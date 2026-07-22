[Setup]
AppId={{4C6081D2-47D1-4C3B-95C7-97D5D7F2E9A1}
AppName=CAPETF Realtime
AppVersion=0.1.0
AppPublisher=CAPETF
DefaultDirName={autopf}\CAPETF Realtime
DefaultGroupName=CAPETF Realtime
OutputDir=..\..\artifacts
OutputBaseFilename=CAPETF-Realtime-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\CAPETF Realtime"; Filename: "{app}\CAPETF.exe"
Name: "{autodesktop}\CAPETF Realtime"; Filename: "{app}\CAPETF.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Run]
Filename: "{app}\CAPETF.exe"; Description: "Launch CAPETF Realtime"; Flags: nowait postinstall skipifsilent
