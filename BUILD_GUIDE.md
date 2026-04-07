# Build & Deployment Guide

## Prerequisites Checklist

- [ ] Windows 10/11
- [ ] .NET 6.0 SDK installed (`dotnet --version`)
- [ ] Visual Studio 2022 OR VS Code + .NET CLI
- [ ] Git (optional, for cloning)

### Install .NET 6.0

**Download:** https://dotnet.microsoft.com/download/dotnet/6.0

**Verify installation:**
```bash
dotnet --version
# Should output: 6.0.xxx
```

## Build from Source

### Option 1: Visual Studio 2022

1. **Open Project**
   - File → Open → Folder
   - Navigate to `vedion-screen-share` directory
   - Wait for project to load

2. **Build Solution**
   - Press `Ctrl+Shift+B` or Build → Build Solution
   - Output: `bin/Release/net6.0-windows/VedionScreenShare.exe`

3. **Run**
   - Press `Ctrl+F5` (Start Without Debugging)
   - Or Debug → Start Without Debugging

### Option 2: Command Line (.NET CLI)

```bash
# Navigate to project directory
cd vedion-screen-share

# Restore packages (if any external dependencies added)
dotnet restore

# Build in Release mode
dotnet build -c Release

# Run the application
dotnet run --configuration Release
```

Output:
```
Microsoft (R) Build Engine version 17.x.x
...
Build succeeded.
```

### Option 3: Command Line (Direct Execution)

After building:

```bash
# Run the built executable directly
.\bin\Release\net6.0-windows\VedionScreenShare.exe
```

## First Run Walkthrough

### 1. Setup Window Appears

You should see:
- "Vedion Screen Share Setup" title
- Disclosure box explaining what the app does
- Configuration fields

### 2. Configure Endpoint

**Test Server (Development):**

If you don't have a server yet, use a test endpoint:
```
wss://echo.websocket.org
```

**Production Server:**

Use the actual server URL. For the included Node.js example:
```
wss://your-server.com:8443/screen
```

### 3. Generate Encryption Key

1. Click **"Generate New"** button
2. A Base64 key is generated (32 bytes, 256-bit)
3. Key appears in the text field
4. **Keep this key safe** — you'll need it for decryption

### 4. Adjust Settings

**Capture Interval** (ms):
- Default: 1000 (1 frame per second)
- Range: 500-5000
- Lower = more frames but higher bandwidth

**Image Quality**:
- Default: 75%
- Range: 40-95%
- Lower = smaller file size, more compression artifacts

### 5. Start Sharing

1. Click **"Start Sharing"**
2. Setup window closes
3. System tray icon appears
4. Frames start being captured and sent

### 6. Monitor in System Tray

Right-click the tray icon:
- **Status** — Shows "Active" or "Paused"
- **Pause/Resume** — Toggle capture
- **Exit** — Stop the application

## Troubleshooting First Run

### "Connection refused"
- Endpoint URL might be wrong
- Server not running
- Check network connectivity

**Fix:**
```bash
# Test connectivity to endpoint
ping your-server.com
```

### "Invalid encryption key"
- Key not valid Base64
- Key not exactly 32 bytes when decoded

**Fix:**
- Click "Generate New" to create a valid key

### "WebSocket connection timeout"
- Server not responding
- Firewall blocking connection

**Fix:**
- Check endpoint URL
- Verify server is running
- Check Windows Firewall settings

### Application crashes on startup
- .NET 6.0 not installed
- Missing Windows components (GDI+, etc.)

**Fix:**
```bash
# Verify .NET is installed
dotnet --version

# Install .NET 6.0 Runtime if needed
```

## Setting Up a Test Server

See `SERVER_EXAMPLE.md` for detailed instructions.

### Quick Start (Node.js)

```bash
# Install Node.js from https://nodejs.org

# Create server directory
mkdir vedion-server
cd vedion-server

# Copy server files from SERVER_EXAMPLE.md
# (or clone from repository)

# Install dependencies
npm install

# Generate self-signed certificate
mkdir certs
openssl req -new -x509 -keyout certs/server-key.pem -out certs/server.pem -days 365 -nodes -subj "/CN=localhost"

# Start server
npm start
```

Server output:
```
WebSocket server listening on wss://localhost:8443/screen
Waiting for client connections...
```

### Connect Client to Local Server

1. In Setup Window, Endpoint URL: `wss://localhost:8443/screen`
2. Windows Firewall will prompt — allow if needed
3. Click "Start Sharing"
4. Server console shows: `Frame 1 | 1920x1080 | FPS: 1.0`

## Building for Distribution

### Create Release Build

```bash
dotnet build -c Release -p:PublishSingleFile=true
```

**Output:** `bin/Release/net6.0-windows/VedionScreenShare.exe`

This is a standalone executable. To use it on another machine:
1. Ensure .NET 6.0 Runtime is installed
2. Copy `VedionScreenShare.exe` to target machine
3. Run the executable

### Create Self-Contained Executable

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

**Output:** `bin/Release/net6.0-windows/win-x64/publish/VedionScreenShare.exe`

This executable includes .NET runtime, no installation required.

**Size:** ~60-100 MB (includes .NET runtime)

### Create Windows Installer (Future)

Use WiX Toolset or NSIS to create `.msi` or `.exe` installer.

**Basic WiX Example:**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Name="Vedion Screen Share" Id="*" UpgradeCode="..." Version="1.0.0.0">
    <Package InstallerVersion="200" Compressed="yes" />
    <Media Id="1" Cabinet="vedion.cab" EmbedCab="yes" />
    
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="Vedion" />
      </Directory>
    </Directory>
    
    <Feature Id="ProductFeature" Title="Vedion Screen Share" Level="1">
      <ComponentRef Id="VedionExe" />
    </Feature>
  </Product>
</Wix>
```

## Deployment Options

### 1. Direct Installation

```bash
# Copy executable to target machine
# Run on user login or via Task Scheduler
```

### 2. Windows Service

```powershell
# As Administrator
sc create VedionScreenShare binPath= "C:\Program Files\Vedion\VedionScreenShare.exe"
sc start VedionScreenShare
```

Note: Requires modifications to app (add `ServiceBase` support)

### 3. Scheduled Task

```powershell
# Run at user login
$trigger = New-ScheduledTaskTrigger -AtLogOn
$action = New-ScheduledTaskAction -Execute "C:\Program Files\Vedion\VedionScreenShare.exe"
Register-ScheduledTask -TaskName "Vedion Screen Share" -Trigger $trigger -Action $action
```

### 4. Docker (Server Only)

See `SERVER_EXAMPLE.md` for Docker instructions.

## Configuration Management

### Save Configuration Between Runs (Future Feature)

Current behavior: Configuration is lost on exit

Recommended: Save config to JSON file

**Location:** `%APPDATA%\VedionScreenShare\config.json`

**Format:**
```json
{
  "endpointUrl": "wss://your-server.com:8443/screen",
  "encryptionKey": "base64-key-here",
  "captureIntervalMs": 1000,
  "jpegQuality": 75,
  "autoStart": true
}
```

**Implementation:** (for future enhancement)

```csharp
// In TrayApplication.cs
private async Task SaveConfigAsync()
{
    string configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VedionScreenShare",
        "config.json"
    );
    
    var json = JsonSerializer.Serialize(_config);
    await File.WriteAllTextAsync(configPath, json);
}

private async Task<CaptureConfig> LoadConfigAsync()
{
    string configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VedionScreenShare",
        "config.json"
    );
    
    if (!File.Exists(configPath))
        return null;
    
    var json = await File.ReadAllTextAsync(configPath);
    return JsonSerializer.Deserialize<CaptureConfig>(json);
}
```

## Performance Optimization

### CPU Usage High?

1. Increase capture interval (1000ms → 2000ms)
2. Reduce JPEG quality (75% → 60%)
3. Profile with `dotnet-trace`

### Memory Growing?

1. Ensure no resource leaks (check `Dispose()` calls)
2. Monitor with `dotnet-trace`
3. Profile with Jetbrains Rider

### Slow Network?

1. Reduce JPEG quality (higher compression)
2. Increase capture interval (fewer frames)
3. Compress frames with ZSTD (future enhancement)

## Monitoring in Production

### Health Check Endpoint (Future)

Add HTTP endpoint for health monitoring:

```csharp
var httpListener = new HttpListener();
httpListener.Prefixes.Add("http://localhost:8000/");
httpListener.Start();

// Respond to GET /health
```

### Metrics Export (Future)

Add Prometheus-style metrics:

```
# HELP vedion_frames_sent Total frames sent
# TYPE vedion_frames_sent counter
vedion_frames_sent 1234

# HELP vedion_bytes_sent Total bytes sent
# TYPE vedion_bytes_sent counter
vedion_bytes_sent 102400000
```

## Debugging

### Enable Console Output

Modify `App.xaml.cs`:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    // Allocate console for debugging
    AllocConsole();
    Console.WriteLine("Vedion Screen Share starting...");
    
    base.OnStartup(e);
    // ... rest of startup code
}

[DllImport("kernel32.dll")]
private static extern bool AllocConsole();
```

### Use Debugger

In Visual Studio:
1. Set breakpoints (click line number)
2. Press F5 to start debugging
3. Step through code with F10/F11
4. Inspect variables in debug window

### View Event Log

```powershell
# View application errors
Get-EventLog -LogName Application -Source "Vedion*"
```

## Next Steps

1. ✅ Build the application
2. ✅ Generate encryption key
3. ✅ Set up test server (or use production endpoint)
4. ✅ Run first test
5. ➡️ **Customize as needed**
   - Add configuration persistence
   - Add logging
   - Add health checks
6. ➡️ **Deploy to target machines**
   - Create installer
   - Test on clean systems
   - Monitor in production

## Support & Resources

- **Project Docs:** See other .md files in this directory
- **.NET Docs:** https://docs.microsoft.com/en-us/dotnet/
- **WebSocket:** https://developer.mozilla.org/en-US/docs/Web/API/WebSocket
- **Cryptography:** https://cryptography.io/
- **OpenSSL:** https://www.openssl.org/

## Common Commands Reference

```bash
# Build
dotnet build -c Release

# Run
dotnet run --configuration Release

# Publish (self-contained)
dotnet publish -c Release -r win-x64 --self-contained

# Test
dotnet test

# Clean
dotnet clean

# Restore packages
dotnet restore
```

Good luck! 🚀
