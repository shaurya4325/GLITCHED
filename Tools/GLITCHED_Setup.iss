; GLITCHED installer (Inno Setup 6). Packs Builds\WindowsJudges into Builds\Installer\GLITCHED_Setup.exe.
; Compile: "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" Tools\GLITCHED_Setup.iss

#define AppName "GLITCHED"
#define AppVersion "1.0"
#define Src "..\Builds\WindowsJudges"

[Setup]
AppId={{6F1C2A9E-4B7D-4E3A-9C55-61D0A7E2B3F4}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=DAV Public School, Defence Colony, Dehradun
AppComments=Knowledge is your weapon. CBSE Skill Expo 2026-27.
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; installs for the current user without admin rights; "all users" is offered too
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\Builds\Installer
OutputBaseFilename=GLITCHED_Setup
UninstallDisplayIcon={app}\GLITCHED.exe
Compression=lzma2/max
SolidCompression=yes
DiskSpanning=no
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
Source: "{#Src}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*_BurstDebugInformation_DoNotShip,*.pdb"

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\GLITCHED.exe"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\GLITCHED.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\GLITCHED.exe"; Description: "Play {#AppName} now"; Flags: nowait postinstall skipifsilent
