# Windows Setup - Complete Guide

Everything is ready to build on Windows. Follow this guide to get it running.

## Requirements Checklist

- [ ] Windows 10 or Windows 11
- [ ] .NET 6.0 SDK (download below)
- [ ] Visual Studio 2022 OR VS Code (optional but recommended)
- [ ] Git (for cloning from GitHub)

## Step 1: Install .NET 6.0 SDK

**Download:** https://dotnet.microsoft.com/download/dotnet/6.0

Click the **"x64"** installer for Windows (not runtime, SDK).

**Verify installation:**
```bash
dotnet --version
```

Should output: `6.0.x`

## Step 2: Clone the Repository

**Option A: GitHub Desktop (Easiest)**
1. Download GitHub Desktop from https://desktop.github.com/
2. Click **Clone a repository**
3. Enter: `YOUR_USERNAME/vedion-screen-share`
4. Click **Clone**

**Option B: Git Command Line**
```bash
git clone https://github.com/YOUR_USERNAME/vedion-screen-share.git
cd vedion-screen-share
```

**Option C: Download ZIP**
1. Go to https://github.com/YOUR_USERNAME/vedion-screen-share
2. Click **Code → Download ZIP**
3. Extract to a folder
4. Open in command prompt/PowerShell

## Step 3: Build the Application

### Option A: Visual Studio 2022 (Recommended)

1. **Install Visual Studio 2022** (Community is free)
   - Download: https://visualstudio.microsoft.com/
   - Select **".NET desktop development"** workload during install

2. **Open Project:**
   - File → Open → Folder
   - Select the `vedion-screen-share` folder
   - Wait for project to load

3. **Build:**
   - Press `Ctrl+Shift+B` or Build → Build Solution
   - Output: `bin/Release/net6.0-windows/VedionScreenShare.exe`

4. **Run:**
   - Press `Ctrl+F5` or Debug → Start Without Debugging
   - Setup window appears

### Option B: Command Line

```bash
# Navigate to project directory
cd vedion-screen-share

# Build in Release mode
dotnet build -c Release

# Run
dotnet run --configuration Release
```

### Option C: PowerShell Script (Automated)

Create `build.ps1`:

```powershell
# Build and run Vedion
Write-Host "Building Vedion Screen Share..." -ForegroundColor Green

dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Running Vedion..." -ForegroundColor Green
dotnet run --configuration Release
```

Run:
```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

## Step 4: First Run Walkthrough

When you run the app, you'll see:

### Setup Window

**Required:**
1. **Endpoint URL** — Where to send frames
   - Example: `wss://localhost:8443/screen` (local test)
   - Or your production server URL

2. **Encryption Key** — AES-256 key
   - Click **"Generate New"** to create one
   - Keep it safe — you need it to decrypt frames

### Optional:

3. **Capture Interval** — How often to capture (default 1000ms)
   - Lower = more frames = more bandwidth
   - Higher = fewer frames = less bandwidth

4. **Image Quality** — JPEG compression (default 75%)
   - Lower = smaller files
   - Higher = better quality but larger files

### Start Sharing

Click **"Start Sharing"** to begin:
- Window closes
- App minimizes to system tray
- Frames start being captured and sent
- Right-click tray icon to pause/resume/exit

## Step 5: Set Up Test Server (Optional)

To test locally without a production server:

### Quick Node.js Server

**1. Install Node.js:**
https://nodejs.org/ (choose LTS)

**2. Create server directory:**
```bash
mkdir vedion-test-server
cd vedion-test-server
npm init -y
npm install ws
```

**3. Create `server.js`:**
```javascript
const WebSocket = require('ws');
const https = require('https');
const fs = require('fs');

const server = https.createServer({
  key: fs.readFileSync('./server-key.pem'),
  cert: fs.readFileSync('./server.pem')
});

const wss = new WebSocket.Server({ server, path: '/screen' });

wss.on('connection', (ws) => {
  console.log('✓ Client connected');
  
  ws.on('message', (data) => {
    try {
      const frame = JSON.parse(data);
      console.log(`Frame received: ${frame.width}x${frame.height}`);
      ws.send(JSON.stringify({ id: frame.id, status: 'ok' }));
    } catch (e) {
      console.error('Error:', e.message);
    }
  });
  
  ws.on('close', () => console.log('✗ Client disconnected'));
});

server.listen(8443, () => {
  console.log('🚀 Server listening on wss://localhost:8443/screen');
});
```

**4. Generate certificate:**

In PowerShell (as Admin):
```powershell
# Using OpenSSL (if installed)
openssl req -new -x509 -keyout server-key.pem -out server.pem `
  -days 365 -nodes -subj "/CN=localhost"

# OR use PowerShell built-in
# (More complex, see SERVER_EXAMPLE.md for details)
```

**5. Run server:**
```bash
node server.js
```

Output:
```
🚀 Server listening on wss://localhost:8443/screen
```

**6. In Vedion setup window:**
- Endpoint: `wss://localhost:8443/screen`
- Click "Start Sharing"
- Server console shows frames being received ✓

## Step 6: Build Standalone Executable

For sharing with others (no .NET install needed on their machine):

```bash
# Build self-contained executable
dotnet publish -c Release -r win-x64 --self-contained

# Output: bin/Release/net6.0-windows/win-x64/publish/VedionScreenShare.exe
# Size: ~60-100 MB (includes .NET runtime)
```

Copy `VedionScreenShare.exe` to any Windows 10+ machine and run it directly.

## Step 7: (Optional) Create Installer

Use NSIS (free, open-source) to create a `.exe` installer:

1. Download NSIS: https://nsis.sourceforge.io/
2. Create `installer.nsi`:
   ```nsis
   ; Simple NSIS installer script
   Name "Vedion Screen Share"
   OutFile "VedionScreenShare-Setup.exe"
   InstallDir "$PROGRAMFILES\Vedion"
   
   Section "Install"
     SetOutPath "$INSTDIR"
     File "bin/Release/net6.0-windows/win-x64/publish/VedionScreenShare.exe"
     CreateDirectory "$SMPROGRAMS\Vedion"
     CreateShortCut "$SMPROGRAMS\Vedion\Vedion Screen Share.lnk" "$INSTDIR\VedionScreenShare.exe"
   SectionEnd
   ```
3. Right-click installer.nsi → Compile NSIS Script
4. Distribute the generated `.exe`

## Production Deployment

### For Your Own Machines

**1. Build release:**
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

**2. Copy executable:**
```bash
# Copy to target machine
Copy-Item "bin/Release/net6.0-windows/win-x64/publish/VedionScreenShare.exe" `
  "C:\Program Files\Vedion\VedionScreenShare.exe"
```

**3. Run on user login (optional):**
```bash
# Add to Startup folder
Copy-Item "VedionScreenShare.exe" `
  "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\"
```

### For Distribution

1. Create installer (see Step 7)
2. Host on GitHub Releases
3. Users download and run installer
4. Runs on first login (or manually)

## Troubleshooting

### "dotnet: command not found"
- .NET 6.0 SDK not installed
- Download from https://dotnet.microsoft.com/download/dotnet/6.0
- Verify: `dotnet --version`

### "Project file not found"
- Make sure you're in the `vedion-screen-share` directory
- Check: `dir VedionScreenShare.csproj` should show the file

### Build fails with "error CS..."
- Check that all files are in correct folders:
  - `Services/` folder should have .cs files
  - `Models/` folder should have .cs files
- Verify .NET version: `dotnet --version`

### "Connection refused" when starting
- Server not running
- Wrong endpoint URL
- Check Windows Firewall allows WebSocket (port 8443 or your custom port)

### Application crashes immediately
- .NET 6.0 runtime not installed
- Try: `dotnet --version`
- If error, reinstall from https://dotnet.microsoft.com/download/dotnet/6.0

### "WebSocket certificate error"
- Self-signed certificate (development only)
- For production, use proper TLS certificate from Let's Encrypt
- See SECURITY.md for certificate pinning

## Performance Tips

**If CPU usage is high:**
- Increase capture interval (1000ms → 2000ms)
- Reduce JPEG quality (75% → 60%)

**If bandwidth is high:**
- Reduce JPEG quality (biggest impact)
- Increase capture interval (fewer frames)
- Capture specific region instead of full screen

**If memory grows:**
- Restart application periodically
- Check logs for errors
- Reduce capture quality/frequency

## Next Steps

✅ **You now have:**
- Full source code
- Working Windows application
- Clear build instructions
- Example server setup

📖 **For more info:**
- [README.md](README.md) — Features overview
- [ARCHITECTURE.md](ARCHITECTURE.md) — How it works
- [SECURITY.md](SECURITY.md) — Security details
- [GITHUB_SETUP.md](GITHUB_SETUP.md) — How to push to GitHub

🚀 **Ready to share?**
- Create GitHub repo (see GITHUB_SETUP.md)
- Add to README which links to this guide
- Share the link with collaborators

## Getting Help

- **Build errors?** — Check .NET version and file locations
- **Runtime errors?** — Check Windows Firewall and network connectivity
- **Security questions?** — See SECURITY.md
- **Deployment?** — See GITHUB_SETUP.md

---

**You're all set!** Time to build and run. 🎯
