# Vedion Screen Share

**Encrypted background screen-sharing for Windows** — Share sensitive information live without other applications intercepting it.

![Windows](https://img.shields.io/badge/Windows-10%2B-blue)
![.NET](https://img.shields.io/badge/.NET-6.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

✅ **Transparent Setup** — Clear consent before activation  
✅ **End-to-End Encryption** — AES-256-CBC per frame  
✅ **Application Isolation** — Other apps can't intercept data  
✅ **System Tray Control** — Pause, resume, exit from tray menu  
✅ **Customizable Capture** — Full screen or specific region  
✅ **Efficient Encoding** — JPEG compression with adjustable quality  
✅ **Secure Transport** — WebSocket over TLS (WSS)  

## How It Works

```
Setup Window → Encrypt Config → Start Sharing → System Tray
    ↓              ↓                  ↓              ↓
User enters   Generate AES-256   Every 1000ms:   • Pause/Resume
endpoint &    encryption key     • Capture       • View status
quality                          • Encrypt       • Exit
                                 • Send
                                     ↓
                            Remote WebSocket Server
                            (receives encrypted frames)
```

## What Gets Protected

| What | Protected? | How |
|------|-----------|-----|
| **Unencrypted frames** | ✅ | Other apps can't read process memory |
| **Network packets** | ✅ | TLS encryption in transit |
| **Encryption key** | ✅ | Separate process, isolated storage |
| **System sees app** | ❌ | App runs visibly (not hidden) |
| **Kernel spying** | ❌ | Kernel can always see everything |

**This is NOT:**
- A way to hide from your OS or antivirus
- Protection against system-level rootkits
- Suitable for bypassing corporate monitoring

**This IS:**
- Protection against other user-level applications
- A transparent tool for secure remote assistance
- Designed for legitimate privacy needs

## Quick Start

### Requirements

- Windows 10 or later
- .NET 6.0 Runtime ([download](https://dotnet.microsoft.com/download/dotnet/6.0))

### Build from Source

```bash
git clone https://github.com/yourusername/vedion-screen-share.git
cd vedion-screen-share

# Build
dotnet build -c Release

# Run
dotnet run --configuration Release
```

Or use Visual Studio 2022:
```
File → Open → Folder → [select project folder]
Ctrl+Shift+B  # Build
Ctrl+F5       # Run
```

### First Run

1. **Setup Window** appears
2. Enter WebSocket endpoint URL:
   ```
   wss://your-server.com:8443/screen
   ```
3. Click **"Generate New"** for encryption key
4. Adjust capture interval (500-5000ms) and quality (40-95%)
5. Click **"Start Sharing"**
6. App minimizes to tray and starts capturing

**Right-click tray icon to:**
- Pause/Resume capture
- View status
- Exit application

## Configuration

### Endpoint URL

The server where frames are sent. Must be:
- **WebSocket Secure** (`wss://`, not `ws://`)
- **HTTPS-compatible** with valid TLS certificate
- **Publicly accessible** or on your network

### Encryption Key

- Generated automatically (click "Generate New")
- Base64-encoded 256-bit key
- **Keep it safe** — you need it to decrypt frames on server
- Can be changed anytime

### Capture Settings

| Setting | Range | Default | Impact |
|---------|-------|---------|--------|
| **Interval** | 500-5000ms | 1000ms | How often to capture (lower = more frames) |
| **Quality** | 40-95% | 75% | JPEG compression (lower = smaller file) |
| **Region** | Full or custom | Full screen | What part of screen to record |

## Architecture

```
SetupWindow (WPF XAML UI)
    ↓
TrayApplication (Orchestrator)
    ├── ScreenCaptureService (GDI+ capture)
    ├── EncryptionService (AES-256-CBC)
    └── NetworkService (WebSocket client)
```

**See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed design.**

## Security

### Encryption

- **Algorithm:** AES-256-CBC
- **Key:** 32 bytes (256 bits), Base64-encoded
- **IV:** Random per frame (sent with packet)
- **Padding:** PKCS7

### Network

- **Protocol:** WebSocket Secure (TLS 1.3+)
- **Certificate validation:** Enabled by default
- **Pinning:** Can be added (see [SECURITY.md](SECURITY.md))

### Process Isolation

- Runs as separate user process
- Other apps cannot read memory or internal state
- Windows Firewall can restrict to specific endpoint
- No shared memory or clipboard usage

**See [SECURITY.md](SECURITY.md) for threat models and hardening tips.**

## Server Setup

### Quick Test Server (Node.js)

```bash
# Install Node.js from https://nodejs.org

mkdir vedion-server
cd vedion-server
npm init -y
npm install ws

# Generate test certificate
openssl req -new -x509 -keyout server-key.pem -out server.pem \
  -days 365 -nodes -subj "/CN=localhost"
```

Create `server.js`:

```javascript
const WebSocket = require('ws');
const https = require('https');
const fs = require('fs');

const wss = new WebSocket.Server({
  server: https.createServer({
    key: fs.readFileSync('./server-key.pem'),
    cert: fs.readFileSync('./server.pem')
  }),
  path: '/screen'
});

wss.on('connection', (ws) => {
  console.log('Client connected');
  
  ws.on('message', (data) => {
    const frame = JSON.parse(data);
    console.log(`Frame: ${frame.width}x${frame.height}`);
    ws.send(JSON.stringify({ id: frame.id, status: 'ok' }));
  });
});

wss.on('connection').server.listen(8443, () => {
  console.log('Server listening on wss://localhost:8443/screen');
});
```

Run: `node server.js`

**For full server example, see [SERVER_EXAMPLE.md](SERVER_EXAMPLE.md).**

## Deployment

### Standalone Executable

```bash
dotnet publish -c Release -r win-x64 --self-contained
# Output: bin/Release/net6.0-windows/win-x64/publish/VedionScreenShare.exe
```

Size: ~60-100 MB (includes .NET runtime)

### To Another Machine

1. Copy `VedionScreenShare.exe` to target machine
2. Ensure Windows 10+ (no .NET install needed for self-contained)
3. Run the executable

### As Windows Service (Future)

```powershell
sc create VedionScreenShare binPath= "C:\Program Files\Vedion\VedionScreenShare.exe"
```

Requires code modifications (see [BUILD_GUIDE.md](BUILD_GUIDE.md)).

## Performance

### System Requirements

| Component | Requirement |
|-----------|-------------|
| **CPU** | 1 GHz+ (uses ~5-15%) |
| **RAM** | 2+ GB (app uses ~50-100MB) |
| **Network** | 1+ Mbps (depends on quality/interval) |
| **OS** | Windows 10/11 |

### Bandwidth Estimate

At **75% quality, 1 frame/sec:**
- Frame size: 500KB - 2MB (depends on screen content)
- Upload: 500KB - 2MB per second
- **Monthly:** ~40-160 GB (adjust interval/quality to reduce)

## Documentation

| File | Purpose |
|------|---------|
| [README.md](README.md) | Feature overview |
| [START_HERE.md](START_HERE.md) | Quick navigation guide |
| [QUICKSTART.md](QUICKSTART.md) | Build and run instructions |
| [BUILD_GUIDE.md](BUILD_GUIDE.md) | Detailed build & deployment |
| [ARCHITECTURE.md](ARCHITECTURE.md) | System design internals |
| [SECURITY.md](SECURITY.md) | Security analysis & hardening |
| [SERVER_EXAMPLE.md](SERVER_EXAMPLE.md) | Example receiver server |

## Troubleshooting

### "Connection refused"
- Server not running or wrong URL
- Check endpoint is correct
- Verify server is listening

### "Invalid encryption key"
- Use "Generate New" in setup
- Key must be valid Base64 and 32 bytes

### "WebSocket security error"
- Certificate issue (self-signed = warning)
- Verify server certificate is trusted

### Application hangs
- Check network connectivity
- Verify server is responding
- Reduce capture frequency if CPU high

**See [BUILD_GUIDE.md](BUILD_GUIDE.md) troubleshooting section.**

## Contributing

Contributions welcome! Areas for enhancement:

- [ ] Configuration persistence
- [ ] Logging system
- [ ] Certificate pinning
- [ ] Delta/diff compression
- [ ] Multiple monitor support
- [ ] Installer (MSI/NSIS)
- [ ] Windows service support
- [ ] UI settings window

See [ARCHITECTURE.md](ARCHITECTURE.md#future-enhancements) for full enhancement list.

## License

MIT License — See [LICENSE](LICENSE)

## Disclaimer

⚠️ **Use responsibly:**
- Only run on systems you own or have permission to use
- Disclose to users that screen recording is active
- Comply with all applicable laws and regulations
- In shared environments, ensure corporate policies allow this

**This is a tool. Like any tool, it can be used responsibly or irresponsibly.**

## Support

- **Issues:** Check [BUILD_GUIDE.md](BUILD_GUIDE.md) troubleshooting first
- **Questions:** See documentation files in repo
- **Contributions:** Pull requests welcome
- **Security Issues:** Please report privately

## Roadmap

**v1.0 (Current)**
- ✅ Basic screen capture
- ✅ AES-256 encryption
- ✅ WebSocket transmission
- ✅ System tray control

**v1.1 (Planned)**
- Configuration persistence
- Logging system
- Settings window
- Better error handling

**v2.0 (Future)**
- Delta compression
- Multiple monitor support
- Windows service mode
- Installer

## Acknowledgments

Built with:
- .NET Foundation (.NET 6.0)
- Microsoft Cryptography APIs
- OpenSSL (testing)

---

**Ready to get started?** → See [START_HERE.md](START_HERE.md)

**Questions about security?** → See [SECURITY.md](SECURITY.md)

**Ready to deploy?** → See [BUILD_GUIDE.md](BUILD_GUIDE.md)
