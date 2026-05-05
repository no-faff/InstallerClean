; InstallerClean Inno Setup script.
;
; AppId is intentionally constant across versions ("InstallerClean") so
; Windows treats every shipped version as the same product and the
; uninstall entry stays at HKLM\Software\Microsoft\Windows\CurrentVersion\
; Uninstall\InstallerClean_is1. Do NOT change AppId.
;
; AppVersion is normally provided by the release script via
; "ISCC.exe /DAppVersion=1.7.0 ...". The fallback below is for ad-hoc
; local builds; keep it in sync with the current shipping target so a
; from-source install doesn't claim to be an older version on the user's
; Add/Remove Programs entry.
[Setup]
#ifndef AppVersion
  #define AppVersion "1.7.0"
#endif
AppId=InstallerClean
AppName=InstallerClean
AppVersion={#AppVersion}
AppPublisher=No Faff
AppPublisherURL=https://github.com/no-faff/InstallerClean
AppSupportURL=https://github.com/no-faff/InstallerClean/discussions
AppCopyright=Copyright (c) 2026 No Faff
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
; The CLI is now a real .NET console exe published from
; src/InstallerClean.Cli; it ships alongside the GUI exe so PowerShell
; and cmd block on the process naturally without any launcher fudge.
Source: "..\publish\cli\installerclean-cli.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\InstallerClean"; Filename: "{app}\InstallerClean.exe"; IconFilename: "{app}\InstallerClean.exe"
Name: "{group}\Uninstall InstallerClean"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\InstallerClean.exe"; Description: "Launch InstallerClean"; Flags: nowait postinstall skipifsilent shellexec
