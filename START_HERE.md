# 🚀 Vedion Screen Share - Start Here

Welcome! This is a complete scaffold for a transparent, encrypted background screen-sharing application for Windows.

## What You Have

A fully-structured C# WPF application that:
- ✅ Captures your screen in the background
- ✅ Encrypts frames with AES-256
- ✅ Sends encrypted data to a remote WebSocket server
- ✅ Runs quietly in the system tray
- ✅ Requires explicit setup and consent before starting

**Not a surveillance tool.** Not hidden from your OS. Just a secure, transparent way to share sensitive screen content without other applications intercepting it.

## Quick Navigation

### 📖 Read These First
1. **[README.md](README.md)** — Overview and features
2. **[QUICKSTART.md](QUICKSTART.md)** — How to build and run
3. **[BUILD_GUIDE.md](BUILD_GUIDE.md)** — Detailed build instructions

### 🏗️ Understand the Architecture
- **[ARCHITECTURE.md](ARCHITECTURE.md)** — System design, components, data flow
- **[SECURITY.md](SECURITY.md)** — How encryption works, threat models

### 🔧 Set Up Server
- **[SERVER_EXAMPLE.md](SERVER_EXAMPLE.md)** — Example Node.js receiver

## 5-Minute Quick Start

### 1. **Build the Application**

```bash
cd vedion-screen-share
dotnet build -c Release
```

Or use Visual Studio: **File → Open Folder** → select this directory → **Ctrl+Shift+B**

### 2. **Run It**

```bash
dotnet run --configuration Release
```

Or in Visual Studio: **Ctrl+F5**

### 3. **Setup Window Appears**

- Endpoint URL: `wss://your-server.com/screen` (or test endpoint)
- Click **"Generate New"** for encryption key
- Adjust capture frequency and quality if needed
- Click **"Start Sharing"**

### 4. **You're Done**

- App minimizes to system tray
- Frames are captured and encrypted automatically
- Right-click tray icon to pause/resume or exit

## Project Structure

```
vedion-screen-share/
├── Models/                      # Data structures
│   ├── CaptureConfig.cs        # Configuration object
│   └── FramePacket.cs          # Network packet format
│
├── Services/                    # Core functionality
│   ├── ScreenCaptureService.cs # Screen capture (DXGI)
│   ├── EncryptionService.cs    # AES-256 encryption
│   └── NetworkService.cs        # WebSocket communication
│
├── App.xaml(.cs)               # Application entry point
├── SetupWindow.xaml(.cs)       # Setup UI
├── TrayApplication.cs          # Main orchestrator
│
├── VedionScreenShare.csproj    # Project file
├── README.md                    # Feature overview
├── QUICKSTART.md               # Build & run
├── BUILD_GUIDE.md              # Detailed build guide
├── ARCHITECTURE.md             # System design
├── SECURITY.md                 # Security details
├── SERVER_EXAMPLE.md           # Example receiver
└── START_HERE.md               # This file
```

## Key Technologies

| Component | Technology |
|-----------|-----------|
| **UI** | WPF (Windows Presentation Foundation) |
| **Screen Capture** | GDI+ (`System.Drawing`) |
| **Encryption** | AES-256-CBC (`System.Security.Cryptography`) |
| **Networking** | WebSocket (`System.Net.WebSockets`) |
| **Language** | C# (.NET 6.0) |

## How It Works (90-Second Version)

```
┌─────────────────────────────────────────────┐
│ 1. User runs app → Setup Window appears    │
│ 2. User enters server endpoint + encrypts   │
│    with generated AES-256 key              │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 3. App starts in background                 │
│    • Every 1000ms: capture screen           │
│    • Encrypt JPEG frame with AES-256        │
│    • Send to remote WebSocket server        │
│    • Server receives encrypted data         │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 4. Other apps on your machine:              │
│    • Can see the app exists                 │
│    • Cannot read the encryption key         │
│    • Cannot read unencrypted frames         │
│    • Cannot read network packet contents    │
│      (encrypted with TLS + AES-256)         │
└─────────────────────────────────────────────┘
```

## Security Guarantees

✅ **Encryption:** AES-256-CBC per frame, random IV
✅ **Network:** WebSocket Secure (TLS 1.3+)
✅ **Process:** Runs as separate process, isolated from other apps
✅ **Transparency:** Setup wizard clearly discloses what's happening

⚠️ **Not Protected Against:**
- Kernel rootkits or admin-level spying
- Physical screen capture devices
- Compromised server endpoint
- The system OS itself (it can always see everything)

## Customization

Everything is modular. You can:

- **Change capture interval** — Currently 500-5000ms slider
- **Adjust JPEG quality** — Currently 40-95% slider
- **Add region-based capture** — `CaptureArea` is already defined
- **Save configuration** — Add JSON persistence
- **Add authentication** — Modify `NetworkService`
- **Implement delta compression** — Compress only changed pixels
- **Add logging** — Create `LoggingService`

See **[ARCHITECTURE.md](ARCHITECTURE.md)** for enhancement suggestions.

## Next Steps

### If You Want to Run It Now
1. Read **[QUICKSTART.md](QUICKSTART.md)**
2. Build: `dotnet build -c Release`
3. Run: `dotnet run --configuration Release`

### If You Want to Understand It First
1. Read **[ARCHITECTURE.md](ARCHITECTURE.md)**
2. Look at the code (it's well-commented)
3. Then build and run

### If You Want to Deploy It
1. Set up a WebSocket server (see **[SERVER_EXAMPLE.md](SERVER_EXAMPLE.md)**)
2. Build a release executable
3. Copy to target machines
4. Test on a clean Windows system

### If You Want to Harden It
1. Read **[SECURITY.md](SECURITY.md)**
2. Implement certificate pinning
3. Add key rotation
4. Enable comprehensive logging
5. Run security audit

## Troubleshooting

### "Connection refused"
- Server not running or wrong URL
- See **[QUICKSTART.md](QUICKSTART.md)** troubleshooting section

### "Invalid encryption key"
- Use **Generate New** button in setup window
- See **[BUILD_GUIDE.md](BUILD_GUIDE.md)** troubleshooting

### Build errors
- Ensure .NET 6.0 is installed: `dotnet --version`
- See **[BUILD_GUIDE.md](BUILD_GUIDE.md)** for detailed instructions

### Security questions
- Read **[SECURITY.md](SECURITY.md)** thoroughly
- It covers threat models, what's protected, what's not

## File Manifest

### Documentation
- `README.md` — Features and overview
- `START_HERE.md` — This file
- `QUICKSTART.md` — Quick start guide
- `BUILD_GUIDE.md` — Build and deployment
- `ARCHITECTURE.md` — System design and internals
- `SECURITY.md` — Security analysis
- `SERVER_EXAMPLE.md` — Example receiver application

### Source Code
- `App.xaml` / `App.xaml.cs` — Entry point
- `SetupWindow.xaml` / `SetupWindow.xaml.cs` — Setup UI
- `TrayApplication.cs` — Main application logic
- `Services/*.cs` — Core services (capture, encrypt, network)
- `Models/*.cs` — Data models
- `VedionScreenShare.csproj` — Project configuration

## License & Attribution

This is a scaffold provided as-is. Modify and use as needed.

**Built with:**
- .NET Foundation (C#, WPF)
- Microsoft Cryptography APIs
- OpenSSL (for test certificates)

## Questions?

- **How do I...?** — Check the docs in this directory
- **Where's the bug?** — Read ARCHITECTURE.md for known limitations
- **How do I change X?** — Look at the code, it's modular and documented
- **Is this secure?** — Read SECURITY.md thoroughly

## Ready?

1. **Next:** Read [QUICKSTART.md](QUICKSTART.md)
2. **Then:** `dotnet build -c Release`
3. **Finally:** `dotnet run --configuration Release`

Good luck! 🚀

---

**Last Updated:** March 2026
**Version:** 1.0.0 (Scaffold)
**Status:** Ready for development and customization
