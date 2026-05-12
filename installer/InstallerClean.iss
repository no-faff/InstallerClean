; InstallerClean Inno Setup script.
;
; AppId stays constant across versions ("InstallerClean") so Windows
; treats every shipped version as the same product; the uninstall
; entry lives at HKLM\Software\Microsoft\Windows\CurrentVersion\
; Uninstall\InstallerClean_is1. Changing AppId breaks Add/Remove
; Programs continuity across versions.
;
; AppVersion is normally passed by the release script via
; "ISCC.exe /DAppVersion=1.8.0 ...". The #define fallback below is
; for ad-hoc local builds; it tracks the current shipping target so a
; from-source install doesn't claim an older version on the Add/Remove
; Programs entry.
[Setup]
#ifndef AppVersion
  #define AppVersion "1.8.0"
#endif
AppId=InstallerClean
AppName=InstallerClean
AppVersion={#AppVersion}
; Mutex name matches App.xaml.cs and Cli/Program.cs. Setup pauses with
; a "close the running app" prompt when the user upgrades while
; InstallerClean.exe or installerclean-cli.exe is holding it.
AppMutex=Global\InstallerClean_SingleInstance
AppPublisher=No Faff
AppPublisherURL=https://github.com/no-faff/InstallerClean
AppSupportURL=https://github.com/no-faff/InstallerClean/discussions
AppCopyright=Copyright (c) 2026 No Faff
; Win32 VS_FIXEDFILEINFO is a four-part version; AppVersion is three,
; so VersionInfoVersion / VersionInfoProductVersion pad with .0.
VersionInfoVersion={#AppVersion}.0
VersionInfoProductVersion={#AppVersion}.0
VersionInfoProductName=InstallerClean
VersionInfoCompany=No Faff
VersionInfoCopyright=Copyright (c) 2026 No Faff
VersionInfoDescription=InstallerClean Setup
DefaultDirName={autopf}\InstallerClean
DefaultGroupName=InstallerClean
UninstallDisplayIcon={app}\InstallerClean.exe
OutputDir=..\publish
OutputBaseFilename=InstallerClean-setup
Compression=zip
SolidCompression=yes
MinVersion=10.0
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
SetupIconFile=..\src\InstallerClean\Assets\app.ico
WizardStyle=modern dynamic
DisableProgramGroupPage=yes
WizardImageFile=wizard-image.bmp
WizardSmallImageFile=wizard-small.png
WizardSmallImageFileDynamicDark=wizard-small.png
WizardImageAlphaFormat=defined

[Messages]
WelcomeLabel1=Welcome to InstallerClean setup
WelcomeLabel2=This will install InstallerClean on your computer.
FinishedHeadingLabel=Setup complete
FinishedLabel=InstallerClean has been installed on your computer.
ClickFinish=Click Finish to close setup.

[Files]
Source: "..\publish\self-contained\InstallerClean.exe"; DestDir: "{app}"; Flags: ignoreversion
; CLI is a .NET console exe published from src/InstallerClean.Cli;
; ships alongside the GUI so PowerShell and cmd block on the process
; subsystem naturally.
Source: "..\publish\cli\installerclean-cli.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\InstallerClean"; Filename: "{app}\InstallerClean.exe"; IconFilename: "{app}\InstallerClean.exe"
Name: "{group}\Uninstall InstallerClean"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\InstallerClean.exe"; Description: "Launch InstallerClean"; Flags: nowait postinstall skipifsilent shellexec
