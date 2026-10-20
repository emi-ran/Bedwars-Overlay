#define MyAppName "Bedwars Overlay"
#define MyAppVersion GetEnv("BEDWARS_OVERLAY_VERSION")
#if MyAppVersion == ""
#define MyAppVersion "1.4.4"
#endif
#define MyAppPublisher "emi-ran"
#define MyAppURL "https://github.com/emi-ran/Bedwars-Overlay"
#define MyAppExeName "BedwarsOverlay.App.exe"
#define MyPublishDir "..\publish\BedwarsOverlay-win-x64"

[Setup]
AppId={{E1DD0211-B24A-4550-8C3F-98D2F3614F1D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases/latest
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\publish
OutputBaseFilename=BedwarsOverlaySetup-{#MyAppVersion}-win-x64
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
SetupIconFile=..\BedwarsOverlay.App\Assets\bedwars-overlay-icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
