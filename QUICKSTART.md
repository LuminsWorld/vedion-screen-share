# Quick Start Guide

## Prerequisites

- Windows 10 or later
- .NET 6.0 Runtime (download from https://dotnet.microsoft.com/download)
- Visual Studio 2022 (or VS Code + .NET CLI)

## Build from Source

### Option 1: Visual Studio

1. Open Visual Studio 2022
2. **File → Open → Folder** → Select this directory
3. Wait for project to load
4. **Build → Build Solution** (Ctrl+Shift+B)
5. Run: **Debug → Start Without Debugging** (Ctrl+F5)

### Option 2: Command Line

```bash
cd vedion-screen-share
dotnet build -c Release
dotnet run --configuration Release
```

## First Run

1. **Setup Window** appears
   - Enter WebSocket endpoint URL (e.g., `wss://vedion.example.com/screen`)
   - Click **Generate New** to create encryption key
   - Adjust capture interval and quality as needed
   - Click **Start Sharing**

2. **Tray Icon** appears
   - Right-click to access menu
   - Pause/Resume capture
   - View status
   - Exit

3. **Background Sharing**
   - Window minimizes to tray
   - Frames captured and encrypted automatically
   - Sent to configured endpoint

## Configuration

Edit the settings in the **Setup Window** before starting:

| Setting | Default | Notes |
|---------|---------|-------|
| Endpoint URL | `wss://vedion.example.com/screen` | WebSocket server address |
| Encryption Key | _(generated)_ | Base64-encoded 256-bit key |
| Capture Interval | 1000ms | How often to capture (ms) |
| Image Quality | 75% | JPEG quality (40-95%) |

## Network Setup

The application sends frames to a WebSocket endpoint. You need:

1. **WebSocket server** listening on `wss://your-endpoint.com`
2. **Endpoint must be HTTPS/WSS** (secure)
3. **Certificate pinning** recommended (see Security section below)

### Example Server (Node.js)

```javascript
const WebSocket = require('ws');
const https = require('https');
const fs = require('fs');

const wss = new WebSocket.Server({
  server: https.createServer({
    cert: fs.readFileSync('./cert.pem'),
    key: fs.readFileSync('./key.pem')
  })
});

wss.on('connection', (ws) => {
  console.log('Client connected');
  
  ws.on('message', (message) => {
    const frame = JSON.parse(message);
    console.log(`Received frame ${frame.id}: ${frame.width}x${frame.height}`);
    
    // Send acknowledgement
    ws.send(JSON.stringify({
      id: frame.id,
      status: 'ok',
      message: 'Frame received'
    }));
  });

  ws.on('close', () => console.log('Client disconnected'));
});

const server = https.createServer({
  cert: fs.readFileSync('./cert.pem'),
  key: fs.readFileSync('./key.pem')
});

wss.on('request', (request) => {
  if (request.url === '/screen') {
    console.log('WebSocket screen-share connection');
  }
});

server.listen(8443);
console.log('Server listening on wss://localhost:8443');
```

## Security Checklist

- [ ] Endpoint URL uses `wss://` (not `ws://`)
- [ ] Server certificate is valid and trusted
- [ ] Encryption key is generated fresh on setup
- [ ] Key is stored securely (not in code)
- [ ] Windows Firewall allows outbound HTTPS
- [ ] No proxy interference (or proxy configured correctly)

## Troubleshooting

### "Connection refused"
- Check endpoint URL is correct
- Verify server is running and listening
- Check Windows Firewall outbound rules

### "Invalid encryption key"
- Key must be valid Base64
- Key must be exactly 32 bytes (256 bits) when decoded
- Click **Generate New** to create a valid key

### Frames not sending
- Check network connectivity
- Verify server is responding with acknowledgements
- Check application logs (TODO: add logging)

### High CPU/Memory
- Reduce capture frequency (increase interval)
- Reduce JPEG quality (lower = faster compression)
- Ensure server is processing frames

## Logging (Future)

Add structured logging to `TrayApplication.cs`:

```csharp
private readonly ILogger<TrayApplication> _logger;

// In Start():
_logger.LogInformation("Starting screen capture service");

// In CaptureLoopAsync():
_logger.LogDebug($"Sending frame {packet.Id}, size: {jpegData.Length} bytes");
```

See `Services/LoggingService.cs` (TODO: implement)

## Deployment

### As Windows Service

To run as a system service (admin required):

```bash
sc create VedionScreenShare binPath= "C:\Program Files\Vedion\VedionScreenShare.exe"
sc start VedionScreenShare
```

### As Scheduled Task

Use Task Scheduler to run at user login or on a schedule.

## Next Steps

1. **Set up receiver server** (see Network Setup section)
2. **Test with dummy endpoint** (echo server)
3. **Add certificate pinning** for production (see Security notes)
4. **Deploy to target machine**
5. **Monitor first run** for any issues

## Support

For issues or questions:
- Check logs (add logging.service)
- Verify configuration
- Test network connectivity separately
- Check Windows Firewall/antivirus rules
