[Setup]
AppId=InstallerClean
AppName=InstallerClean
AppVersion=1.5.1
AppPublisher=No Faff
AppPublisherURL=https://github.com/no-faff/InstallerClean
AppSupportURL=https://github.com/no-faff/InstallerClean/discussions
AppCopyright=Copyright (c) 2026 No Faff
DefaultDirName={autopf}\InstallerClean
DefaultGroupName=InstallerClean
UninstallDisplayIcon={app}\InstallerClean.exe
OutputDir=..\publish
OutputBaseFilename=InstallerClean-setup
Compression=lzma2/ultra64
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

[Icons]
Name: "{group}\InstallerClean"; Filename: "{app}\InstallerClean.exe"; IconFilename: "{app}\InstallerClean.exe"
Name: "{group}\Uninstall InstallerClean"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\InstallerClean.exe"; Description: "Launch InstallerClean"; Flags: nowait postinstall skipifsilent shellexec
