; ============================================================
;  Vedion Screen Share — Inno Setup 6 Script
;  Output: VedionScreenShare-Setup-v2.0.0.exe  (~70 MB)
;  Build pipeline: run installer\build.ps1
; ============================================================

#define AppName       "Vedion Screen Share"
#define AppVersion    "2.0.0"
#define AppPublisher  "Vedion"
#define AppURL        "https://vedion.cloud"
#define AppExeName    "VedionScreenShare.exe"
#define AppGUID       "{B7A2F3E1-9C4D-4A8B-BE12-3D7F6E5A1C9B}"
#define SourceDir     "..\publish\win-x64"

; ── Setup metadata ────────────────────────────────────────────────────
[Setup]
AppId={#AppGUID}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Installer
VersionInfoProductName={#AppName}

; Install to user folder — no UAC prompt required
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Output
OutputDir=.\output
OutputBaseFilename=VedionScreenShare-Setup-v{#AppVersion}
SetupIconFile=..\Resources\tray.ico

; Compression
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; Wizard appearance
WizardStyle=modern
WizardSizePercent=120
WizardResizable=no
WizardImageFile=wizard_sidebar.bmp
WizardSmallImageFile=wizard_icon.bmp

; Platform
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
; Windows 10 1809+ required

; Uninstall
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
CreateUninstallRegKey=yes

; Don't show "Ready to install" page — go straight to installing
DisableReadyPage=no
DisableReadyMemo=yes

; License (consent disclosure)
LicenseFile=license.rtf

; ── Language ──────────────────────────────────────────────────────────
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

; ── Custom messages ───────────────────────────────────────────────────
[CustomMessages]
english.DisclosureTitle=Screen Capture Disclosure
english.AppDesc=Vedion Screen Share captures your screen on demand and sends it to Discord webhooks or AI providers you configure. It runs in the system tray.%n%nNo data is captured or transmitted without your action.

; ── Install tasks ─────────────────────────────────────────────────────
[Tasks]
Name: "desktopicon";  Description: "Create a &desktop shortcut";     GroupDescription: "Shortcuts:"
Name: "startupentry"; Description: "Start with &Windows (system tray)"; GroupDescription: "Startup:"; Flags: unchecked

; ── Files ─────────────────────────────────────────────────────────────
[Files]
; Single self-contained EXE from dotnet publish
Source: "{#SourceDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; ── Shortcuts ─────────────────────────────────────────────────────────
[Icons]
Name: "{group}\{#AppName}";           Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";     Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"; Tasks: desktopicon

; Startup registry entry (cleaner than startup folder)
[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#AppName}"; \
  ValueData: """{app}\{#AppExeName}"""; \
  Flags: uninsdeletevalue; Tasks: startupentry

; Ensure AppData config folder is created
Root: HKCU; Subkey: "SOFTWARE\{#AppPublisher}\{#AppName}"; \
  ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; \
  Flags: uninsdeletekey

; ── Run after install ─────────────────────────────────────────────────
[Run]
Filename: "{app}\{#AppExeName}"; \
  Description: "&Launch {#AppName} now"; \
  Flags: nowait postinstall skipifsilent shellexec

; ── Kill before uninstall ─────────────────────────────────────────────
[UninstallRun]
Filename: "taskkill.exe"; Parameters: "/F /IM {#AppExeName}"; \
  Flags: runhidden; RunOnceId: "KillVSS"

; ── Pascal script ─────────────────────────────────────────────────────
[Code]

// ── Wizard intro page with disclosure ──────────────────────────────
var
  DisclosurePage: TOutputMsgWizardPage;

procedure InitializeWizard;
begin
  DisclosurePage := CreateOutputMsgPage(
    wpLicense,
    ExpandConstant('{cm:DisclosureTitle}'),
    'Please read before continuing',
    ExpandConstant('{cm:AppDesc}')
  );
end;

// ── Preserve user config on uninstall ──────────────────────────────
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ConfigDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    ConfigDir := ExpandConstant('{localappdata}\VedionScreenShare');
    if DirExists(ConfigDir) then
    begin
      if MsgBox(
        'Do you want to remove your saved settings and license data?' + #13#10 +
        '(' + ConfigDir + ')',
        mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(ConfigDir, True, True, True);
      end;
    end;
  end;
end;

// ── Skip .NET install check (self-contained) ────────────────────────
function InitializeSetup: Boolean;
begin
  Result := True;
end;
