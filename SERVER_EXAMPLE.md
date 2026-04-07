# Server Implementation Example

The client application sends encrypted frames to a WebSocket server. Here's how to receive and process them.

## Basic Node.js / Express Server

### Setup

```bash
npm init -y
npm install ws express https crypto
npm install --save-dev nodemon
```

### server.js

```javascript
const WebSocket = require('ws');
const https = require('https');
const fs = require('fs');
const crypto = require('crypto');
const path = require('path');

// Configuration
const PORT = 8443;
const CERT_FILE = './certs/server.pem';
const KEY_FILE = './certs/server-key.pem';
const FRAMES_DIR = './frames';

// Ensure frames directory exists
if (!fs.existsSync(FRAMES_DIR)) {
  fs.mkdirSync(FRAMES_DIR, { recursive: true });
}

// HTTPS Server with WebSocket
const server = https.createServer({
  cert: fs.readFileSync(CERT_FILE),
  key: fs.readFileSync(KEY_FILE)
});

const wss = new WebSocket.Server({ server, path: '/screen' });

// Decryption function (must match client)
function decryptFrame(encryptedData, ivBase64, encryptionKey) {
  const iv = Buffer.from(ivBase64, 'base64');
  const encryptedBuffer = Buffer.from(encryptedData, 'base64');
  const keyBuffer = Buffer.from(encryptionKey, 'base64');

  const decipher = crypto.createDecipheriv('aes-256-cbc', keyBuffer, iv);
  let decrypted = decipher.update(encryptedBuffer);
  decrypted = Buffer.concat([decrypted, decipher.final()]);

  return decrypted;
}

// Connection handler
wss.on('connection', (ws, req) => {
  const clientIp = req.socket.remoteAddress;
  const clientId = crypto.randomBytes(8).toString('hex');

  console.log(`[${new Date().toISOString()}] Client connected: ${clientId} from ${clientIp}`);

  let frameCount = 0;
  let lastFrameTime = Date.now();

  ws.on('message', (data) => {
    try {
      const frame = JSON.parse(data.toString());
      frameCount++;

      // Log frame info
      const now = Date.now();
      const fps = frameCount / ((now - lastFrameTime) / 1000);
      console.log(`[${new Date().toISOString()}] Frame ${frameCount} | ${frame.width}x${frame.height} | FPS: ${fps.toFixed(1)}`);

      // TODO: Decrypt frame with correct key
      // const decrypted = decryptFrame(frame.data, frame.iv, ENCRYPTION_KEY);
      // fs.writeFileSync(`${FRAMES_DIR}/frame-${frameCount}.jpg`, decrypted);

      // Send acknowledgement
      ws.send(JSON.stringify({
        id: frame.id,
        status: 'ok',
        message: 'Frame received'
      }));

    } catch (error) {
      console.error(`[${new Date().toISOString()}] Error processing frame:`, error.message);
      ws.send(JSON.stringify({
        status: 'error',
        message: error.message
      }));
    }
  });

  ws.on('close', () => {
    console.log(`[${new Date().toISOString()}] Client disconnected: ${clientId} (${frameCount} frames received)`);
  });

  ws.on('error', (error) => {
    console.error(`[${new Date().toISOString()}] WebSocket error: ${error.message}`);
  });
});

// Start server
server.listen(PORT, () => {
  console.log(`WebSocket server listening on wss://localhost:${PORT}/screen`);
  console.log('Waiting for client connections...');
});

// Graceful shutdown
process.on('SIGINT', () => {
  console.log('\nShutting down...');
  wss.clients.forEach((client) => {
    client.close();
  });
  server.close(() => {
    console.log('Server closed');
    process.exit(0);
  });
});
```

### package.json

```json
{
  "name": "vedion-server",
  "version": "1.0.0",
  "description": "Receiver server for Vedion screen-share frames",
  "main": "server.js",
  "scripts": {
    "start": "node server.js",
    "dev": "nodemon server.js"
  },
  "dependencies": {
    "ws": "^8.13.0",
    "express": "^4.18.2",
    "https": "^1.0.0",
    "crypto": "^1.0.1"
  },
  "devDependencies": {
    "nodemon": "^3.0.1"
  }
}
```

## Generate Self-Signed Certificate

For testing/development:

```bash
mkdir certs
cd certs

# Generate private key
openssl genrsa -out server-key.pem 2048

# Generate certificate (valid for 365 days)
openssl req -new -x509 -key server-key.pem -out server.pem -days 365 \
  -subj "/C=US/ST=State/L=City/O=Org/CN=localhost"

cd ..
```

For production, use Let's Encrypt or your certificate authority.

## Run Server

```bash
# Development (with auto-reload)
npm run dev

# Production
npm start
```

Output:
```
WebSocket server listening on wss://localhost:8443/screen
Waiting for client connections...
```

## Frame Processing Example

To decrypt and save frames:

```javascript
ws.on('message', (data) => {
  try {
    const frame = JSON.parse(data.toString());
    
    // Decrypt frame
    const encryptionKey = process.env.ENCRYPTION_KEY; // Set this!
    const decrypted = decryptFrame(frame.data, frame.iv, encryptionKey);
    
    // Save JPEG
    const filename = `${FRAMES_DIR}/frame-${Date.now()}-${frame.id}.jpg`;
    fs.writeFileSync(filename, decrypted);
    
    console.log(`Saved: ${filename}`);
    
    // Send acknowledgement
    ws.send(JSON.stringify({
      id: frame.id,
      status: 'ok'
    }));
    
  } catch (error) {
    console.error('Error:', error.message);
  }
});
```

Set encryption key via environment variable:

```bash
export ENCRYPTION_KEY="your-base64-key-here"
npm run dev
```

## Advanced: Frame Buffer & Streaming

To stream frames to a web frontend in real-time:

```javascript
// Store latest frame in memory
let latestFrame = null;
let latestFrameId = null;

ws.on('message', (data) => {
  const frame = JSON.parse(data.toString());
  
  // Decrypt and store
  const encryptionKey = process.env.ENCRYPTION_KEY;
  const decrypted = decryptFrame(frame.data, frame.iv, encryptionKey);
  
  latestFrame = decrypted;
  latestFrameId = frame.id;
  
  // Notify all HTTP clients
  broadcastToClients(frame.id);
  
  // Send ack
  ws.send(JSON.stringify({
    id: frame.id,
    status: 'ok'
  }));
});

// HTTP endpoint for latest frame
express().get('/api/latest-frame', (req, res) => {
  if (!latestFrame) {
    return res.status(404).json({ error: 'No frame available yet' });
  }
  
  res.setHeader('Content-Type', 'image/jpeg');
  res.setHeader('Content-Length', latestFrame.length);
  res.setHeader('X-Frame-Id', latestFrameId);
  res.send(latestFrame);
});
```

## Docker Deployment

```dockerfile
FROM node:18-alpine

WORKDIR /app

COPY package*.json ./
RUN npm install --production

COPY . .
COPY certs/ certs/

ENV ENCRYPTION_KEY=""
ENV PORT=8443

EXPOSE 8443

CMD ["npm", "start"]
```

Build and run:

```bash
docker build -t vedion-server .
docker run -p 8443:8443 -e ENCRYPTION_KEY="your-key" vedion-server
```

## Monitoring & Logging

### Connection Metrics

```javascript
const metrics = {
  totalConnections: 0,
  activeConnections: 0,
  totalFrames: 0,
  totalBytes: 0
};

wss.on('connection', (ws) => {
  metrics.totalConnections++;
  metrics.activeConnections++;

  ws.on('message', (data) => {
    const frame = JSON.parse(data.toString());
    metrics.totalBytes += data.length;
    metrics.totalFrames++;
  });

  ws.on('close', () => {
    metrics.activeConnections--;
  });
});

// Metrics endpoint
express().get('/api/metrics', (req, res) => {
  res.json(metrics);
});
```

### Logging to File

```javascript
const log = (message) => {
  const timestamp = new Date().toISOString();
  const logLine = `[${timestamp}] ${message}\n`;
  fs.appendFileSync('server.log', logLine);
  console.log(logLine);
};
```

## Troubleshooting

### "Certificate verification failed"

**Client side:**
- Update endpoint URL to match certificate CN
- For self-signed: Client needs to trust the certificate
- For production: Use proper TLS certificate

**Server side:**
```bash
# Check certificate validity
openssl x509 -in certs/server.pem -text -noout
```

### "Connection refused"

- Server not running
- Wrong port
- Firewall blocking
- SSL/TLS handshake failure

### "Frames not decrypting"

- Encryption key mismatch (must use same key as client)
- IV corruption during transmission
- Key not valid Base64 or wrong length

Test decryption:

```bash
node -e "
const crypto = require('crypto');
const key = Buffer.from('your-base64-key', 'base64');
console.log('Key length:', key.length, 'bytes (expected 32)');
"
```

## Next Steps

1. Generate certificate (see above)
2. Set `ENCRYPTION_KEY` environment variable
3. Run server: `npm run dev`
4. In client, set endpoint to `wss://your-server:8443/screen`
5. Verify connection in server logs

## Production Checklist

- [ ] Use proper TLS certificate (Let's Encrypt)
- [ ] Enable HTTPS only (no plain HTTP)
- [ ] Implement authentication/authorization
- [ ] Store frames securely (encryption at rest)
- [ ] Implement frame retention policy (auto-delete)
- [ ] Add audit logging
- [ ] Monitor resource usage
- [ ] Set up alerting
- [ ] Implement backpressure handling
- [ ] Rate limiting per client
