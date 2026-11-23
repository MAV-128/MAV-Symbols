#define MyAppName "MAV Symbols"
#define MyAppVersion "1.0"
#define MyAppPublisher "Andrey Morozov (MAV-128)"
#define MyAppURL "https://github.com/MAV-128/MAV-Symbols"
#define MyAppExeName "MAV Symbols.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE
OutputDir=output
OutputBaseFilename=MAVSymbols-{#MyAppVersion}-Setup
SetupIconFile=Symbols.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=lowest

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "MAV Symbols\bin\Release\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "MAV Symbols\bin\Release\MAV Symbols.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "MAV Symbols\bin\Release\MAV Symbols.exe.manifest"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Ассоциация .txt с приложением (без замены иконки)
Root: HKCU; Subkey: "Software\Classes\.txt"; ValueType: string; ValueName: ""; ValueData: "MAVSymbols.txt"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\MAVSymbols.txt"; ValueType: string; ValueName: ""; ValueData: "Text Document"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\MAVSymbols.txt\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "%SystemRoot%\system32\imageres.dll,-102"
Root: HKCU; Subkey: "Software\Classes\MAVSymbols.txt\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent