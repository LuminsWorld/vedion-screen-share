# Vedion Screen Share - Windows Background Assistant

A transparent, encrypted background screen-sharing tool for secure live assistance.

## Features

- **Transparent Setup** — Clear consent window before activation
- **Application-Level Isolation** — Other apps can't see capture/encryption details
- **DXGI Capture** — Efficient per-frame screen capture
- **AES-256 Encryption** — End-to-end encrypted frames
- **Tray Control** — Minimize, pause, configure from system tray
- **Windows-Native** — Uses WPF + CNG for hardware acceleration

## Project Structure

```
vedion-screen-share/
├── SetupWindow.xaml           # Initial configuration UI
├── TrayApplication.cs         # Main tray application
├── Services/
│   ├── ScreenCaptureService.cs      # DXGI screen capture
│   ├── EncryptionService.cs         # AES-256 encryption
│   └── NetworkService.cs             # WebSocket client
├── Models/
│   ├── CaptureConfig.cs             # Configuration data
│   └── FramePacket.cs               # Network frame format
└── App.xaml                         # Application entry point
```

## Prerequisites

- .NET 6.0+ (or .NET Framework 4.8+)
- Visual Studio 2022 or equivalent
- Windows 10+ (for modern DXGI APIs)

## Next Steps

1. Create Visual Studio WPF project
2. Add NuGet packages (see package references below)
3. Copy source files from this scaffold
4. Configure endpoint (where to send frames)
5. Build & deploy

## Security Notes

- All frames encrypted before transmission
- Process isolation via Windows Firewall rules
- Certificate pinning for WebSocket connection
- No clipboard/shared memory usage
