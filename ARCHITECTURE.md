# Architecture & Implementation Notes

## System Design

```
┌─────────────────────────────────────────────────────────┐
│                    Windows Application                  │
│                                                          │
│  ┌────────────────┐      ┌───────────────────┐          │
│  │  Setup Window  │─────→│  TrayApplication  │          │
│  │ (WPF, XAML)    │      │ (Orchestrator)    │          │
│  └────────────────┘      └─────────┬─────────┘          │
│                                    │                     │
│              ┌─────────────────────┼─────────────────────┐
│              │                     │                     │
│         ┌────▼─────┐      ┌───────▼──────┐      ┌──────▼──────┐
│         │ Screen   │      │  Encryption  │      │  Network    │
│         │ Capture  │      │  Service     │      │  Service    │
│         │ Service  │      │ (AES-256)    │      │ (WebSocket) │
│         └──────────┘      └──────────────┘      └─────────────┘
│
└──────────────────────────────────────────────────────────┘
                           │
                  [TLS/AES Encrypted]
                           │
        ┌──────────────────▼──────────────────┐
        │    Remote WebSocket Server          │
        │  (wss://endpoint.com/screen)        │
        │                                     │
        │  - Receive encrypted frames         │
        │  - Decrypt (with shared key)        │
        │  - Store or process frames          │
        │  - Send acknowledgements            │
        └─────────────────────────────────────┘
```

## Component Architecture

### 1. **SetupWindow** (WPF UI)
**Purpose:** One-time configuration before the app starts

**Responsibilities:**
- Display disclosure and consent information
- Collect configuration from user:
  - WebSocket endpoint URL
  - Encryption key (or generate new)
  - Capture interval
  - JPEG quality
- Validate inputs
- Return `CaptureConfig` object

**Lifecycle:**
- Shown on app startup
- User clicks "Start Sharing" → config returned, window closes
- App launches `TrayApplication` with config

**Key Files:**
- `SetupWindow.xaml` - UI markup
- `SetupWindow.xaml.cs` - Code-behind

### 2. **TrayApplication** (Main Orchestrator)
**Purpose:** Runs in system tray and coordinates all services

**Responsibilities:**
- Initialize all services (Capture, Encryption, Network)
- Manage the main capture loop
- Handle pause/resume
- Control system tray icon and menu
- Graceful shutdown

**Lifecycle:**
- Created after setup window
- `Start()` - Initializes services and starts capture loop
- Runs in background until user exits
- `Stop()` - Cleanup and shutdown

**Key Methods:**
- `Start()` - Initialize services, connect to server, start capture
- `CaptureLoopAsync()` - Main loop that captures, encrypts, and sends
- `SetupTrayIcon()` - Create system tray UI
- `Stop()` - Cleanup and shutdown

**Key Files:**
- `TrayApplication.cs`

### 3. **ScreenCaptureService**
**Purpose:** Capture screen regions and encode as JPEG

**Responsibilities:**
- Capture full screen or specific region
- Encode frames as JPEG with configurable quality
- Return frame dimensions and data

**Methods:**
- `CaptureScreen()` - Full screen capture
- `CaptureArea(x, y, width, height)` - Specific region

**Implementation Details:**
- Uses `System.Drawing` (GDI+)
- `Graphics.CopyFromScreen()` for pixel-perfect capture
- JPEG encoder with quality parameter (40-100)
- Disposal of resources per frame

**Performance:**
- At 1920x1080, ~100-150ms per capture at 75% quality
- Memory: ~5-10MB per frame (temporary)

**Key Files:**
- `Services/ScreenCaptureService.cs`

### 4. **EncryptionService**
**Purpose:** AES-256 encryption/decryption of frame data

**Responsibilities:**
- Manage encryption key
- Encrypt plaintext frames
- Decrypt received data (if needed)
- Generate new random IVs per frame

**Methods:**
- `Encrypt(byte[] plaintext)` → `(ciphertext, iv)` both Base64
- `Decrypt(base64Ciphertext, base64Iv)` → plaintext bytes
- `GenerateKey()` - Static method to create new 256-bit key

**Implementation Details:**
- AES in CBC mode
- PKCS7 padding
- Random IV per frame (prevents pattern analysis)
- Key is 32 bytes (256 bits), Base64-encoded for storage

**Security Note:**
- IV must be random for each encryption
- Same key/IV pair should never encrypt two different plaintexts
- Our implementation satisfies this (new IV every frame)

**Key Files:**
- `Services/EncryptionService.cs`

### 5. **NetworkService**
**Purpose:** WebSocket communication with remote server

**Responsibilities:**
- Connect to WebSocket endpoint (WSS)
- Send encrypted frame packets
- Listen for server responses
- Handle disconnections and reconnections
- Emit events for connection status

**Methods:**
- `ConnectAsync()` - Connect to server
- `SendFrameAsync(packet)` - Send a single frame
- `DisconnectAsync()` - Graceful disconnect
- `ListenAsync()` - Background task receiving responses

**Events:**
- `OnConnected` - When connection established
- `OnDisconnected` - When connection lost
- `OnError` - When error occurs

**Implementation Details:**
- `ClientWebSocket` for native .NET WebSocket support
- TLS 1.3+ (enforced by Windows)
- Automatic certificate validation (trusted CA)
- Text frames (JSON serialization)
- 64KB receive buffer

**Packet Format:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2026-03-19T05:53:00Z",
  "data": "base64-encrypted-jpeg-data",
  "iv": "base64-initialization-vector",
  "width": 1920,
  "height": 1080,
  "version": "1.0"
}
```

**Key Files:**
- `Services/NetworkService.cs`

### 6. **Models**
**Purpose:** Data structures for configuration and network packets

**Files:**
- `CaptureConfig` - Configuration object
- `CaptureArea` - Optional region definition
- `FramePacket` - Network packet format
- `FrameAck` - Server acknowledgement

## Data Flow

### Capture → Encrypt → Send

```
1. CaptureLoopAsync() runs on interval
   ↓
2. ScreenCaptureService.CaptureScreen()
   Returns: (JPEG bytes, width, height)
   ↓
3. EncryptionService.Encrypt(jpegBytes)
   Returns: (Base64 ciphertext, Base64 IV)
   ↓
4. Build FramePacket
   {
     id: UUID,
     timestamp: ISO-8601,
     data: Base64 ciphertext,
     iv: Base64 IV,
     width/height: dimensions,
     version: "1.0"
   }
   ↓
5. NetworkService.SendFrameAsync(packet)
   Serializes to JSON, sends over WebSocket
   ↓
6. Server receives, decrypts, processes
   ↓
7. Server sends FrameAck
   {
     id: same UUID,
     status: "ok",
     message: optional
   }
```

## Timing & Performance

### Capture Loop Timing

**Default: 1000ms interval**

```
┌─────────────────────────────────────────────────────┐
│                    1000ms Cycle                     │
├─────────────────────────────────────────────────────┤
│ 0ms    : Capture starts                             │
│ 100ms  : Capture complete, Encrypt starts           │
│ 150ms  : Encrypt complete, Send starts              │
│ 200ms  : Send complete (assuming fast network)      │
│ 200-1000ms : Wait for next interval                 │
│ 1000ms : Start next capture                         │
└─────────────────────────────────────────────────────┘
```

**Adjustable via config:**
- Minimum: 500ms
- Maximum: 5000ms
- Slider in 500ms increments

### Network Bandwidth Estimate

**At 75% JPEG quality:**
- Frame size: ~500KB - 2MB (depends on content)
- At 1fps: 500KB - 2MB per second
- At 2fps: 1MB - 4MB per second

**Optimization:**
- Reduce JPEG quality (increases compression)
- Increase capture interval (fewer frames)
- Send only changed regions (future enhancement)

## Error Handling

### Capture Failures
```csharp
catch (Exception ex) {
    ShowNotification($"Capture error: {ex.Message}", Error);
    await Task.Delay(1000); // Back off
    continue; // Retry next iteration
}
```

### Network Failures
- `ConnectAsync()` - Throws, caught in `Start()`
- `SendFrameAsync()` - Throws, caught in capture loop
- Disconnection - Detected in `ListenAsync()`, emits `OnDisconnected` event
- Auto-reconnect - Not implemented yet (TODO)

### Validation
- Encryption key: Must be valid Base64, 32 bytes when decoded
- Endpoint URL: Must be valid URI, preferably `wss://`
- Intervals: Must be 500-5000ms
- JPEG quality: Must be 40-95%

## Threading Model

### UI Thread
- `SetupWindow` - Runs on UI thread
- Event handlers - Run on UI thread

### Background Threads
- `TrayApplication.CaptureLoopAsync()` - Runs on thread pool (Task-based)
- `NetworkService.ListenAsync()` - Runs on thread pool

### Thread Safety
- Services are not thread-safe by default
- `TrayApplication` ensures single capture loop (no concurrent access)
- Cancellation via `CancellationTokenSource`

## Configuration Persistence

**Current:** Configuration is in-memory only (lost on exit)

**Recommended Improvements:**
1. Save config to JSON file:
   ```json
   {
     "endpointUrl": "wss://...",
     "encryptionKey": "...",
     "captureIntervalMs": 1000,
     "jpegQuality": 75,
     "autoStart": true
   }
   ```

2. Store in secure location:
   - Windows: `%APPDATA%\VedionScreenShare\config.json`
   - With DPAPI encryption for key

3. Load on next startup (skip setup window)

4. Allow user to reconfigure from tray menu

**Files to modify:**
- `Models/CaptureConfig.cs` - Add JSON serialization
- `TrayApplication.cs` - Add `SaveConfig()` and `LoadConfig()` methods
- `SetupWindow.xaml.cs` - Check for existing config, load if present

## Future Enhancements

### Short Term (MVP)
- [ ] Configuration persistence (save/load)
- [ ] Proper logging (file + console)
- [ ] Settings window (change config without restart)
- [ ] Better error messages and recovery

### Medium Term
- [ ] Server-side receiver application
- [ ] Reconnection logic for network failures
- [ ] Certificate pinning
- [ ] Windows service support
- [ ] Installer (MSI/NSIS)

### Long Term
- [ ] Delta/diff compression (send only changed regions)
- [ ] Multiple monitor support
- [ ] Adjustable capture area via UI
- [ ] Frame history/playback
- [ ] Authentication (API keys, certificates)
- [ ] Rate limiting and backpressure handling
- [ ] Metrics and monitoring

## Testing Strategy

### Unit Tests
- `EncryptionService` - Encrypt/decrypt roundtrip
- `ScreenCaptureService` - Validate JPEG encoding
- `Models` - Serialization/deserialization

### Integration Tests
- `NetworkService` - WebSocket connection (mock server)
- Full capture loop - Time and validate frames
- Error scenarios - Network failure, invalid config

### Manual Testing
- Real WebSocket server (local and remote)
- Different screen resolutions
- Various JPEG quality settings
- Network throttling
- Long-duration runs

## Build & Deployment

### Build Steps
1. `dotnet build -c Release`
2. Output: `bin/Release/net6.0-windows/VedionScreenShare.exe`
3. Requires: .NET 6.0 Runtime on target machine

### Installer (TODO)
- WiX (MSI) or NSIS installer
- Install to `Program Files\Vedion\`
- Create Start Menu shortcuts
- Register startup options

### Deployment
- Copy EXE to target machine
- Run once to configure
- Optionally add to Startup folder

## Performance Profiling

### CPU Usage
Expected: ~5-15% on modern CPU
- Capture: ~5%
- Encode: ~3-8%
- Encrypt: ~1-2%
- Network I/O: <1%

### Memory Usage
Expected: ~50-100MB
- Process overhead: ~20MB
- Capture buffer: ~10-20MB (temporary per frame)
- Encryption buffer: ~10-20MB (temporary per frame)

### Profiling Tools
- `dotnet-trace` - CPU and memory
- PerfView - ETW tracing
- Jetbrains Rider - Built-in profiler

## Troubleshooting During Development

### Common Issues

**"WebSocket connection refused"**
- Endpoint URL is wrong
- Server not listening
- Firewall blocking

**"Invalid encryption key"**
- Key not valid Base64
- Key not 32 bytes when decoded
- Use `GenerateKey()` to create valid key

**"Application hanging"**
- Check CancellationToken handling
- Ensure capture loop can exit
- Monitor thread pool

**"Memory leak"**
- Check resource disposal in services
- Monitor long-running tests
- Profile with dotnet-trace

## Code Organization

```
vedion-screen-share/
├── App.xaml                    # Application entry point
├── App.xaml.cs
├── SetupWindow.xaml            # Setup UI
├── SetupWindow.xaml.cs
├── TrayApplication.cs          # Main orchestrator
├── Services/
│   ├── ScreenCaptureService.cs
│   ├── EncryptionService.cs
│   └── NetworkService.cs
├── Models/
│   ├── CaptureConfig.cs
│   └── FramePacket.cs
├── VedionScreenShare.csproj    # Project file
├── README.md
├── QUICKSTART.md
├── ARCHITECTURE.md             # This file
└── SECURITY.md
```

## Next: Running & Testing

See `QUICKSTART.md` for build and run instructions.
