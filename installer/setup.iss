; Vedion Screen Share — Inno Setup Script
; Build: dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained
; Then run this script in Inno Setup Compiler

#define AppName "Vedion Screen Share"
#define AppVersion "2.0.0"
#define AppPublisher "Austin Tessmer"
#define AppURL "https://vedion.cloud/shop"
#define AppExeName "VedionScreenShare.exe"
#define SourceDir "..\publish\win-x64"

[Setup]
AppId={{B7A2F3E1-9C4D-4A8B-BE12-3D7F6E5A1C9B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=.\output
OutputBaseFilename=VedionScreenShare-Setup-v{#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0
; UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon";     Description: "Create a &desktop shortcut";     GroupDescription: "Additional icons:"
Name: "startupentry";    Description: "Launch at &Windows startup";      GroupDescription: "Startup:"; Flags: unchecked

[Files]
; Self-contained publish output — single EXE
Source: "{#SourceDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; If publish is folder (not single file), use:
; Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";                    Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}";          Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";              Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}";              Filename: "{app}\{#AppExeName}"; Tasks: startupentry

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Kill the process before uninstalling
Filename: "taskkill"; Parameters: "/F /IM {#AppExeName}"; Flags: runhidden; RunOnceId: "KillApp"

[Code]
// Preserve user config on uninstall — don't delete AppData
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  // Nothing — config in %AppData%\VedionScreenShare is intentionally left
end;
