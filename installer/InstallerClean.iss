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

#define MyAppName "InstallerClean"
#define MyAppAuthor "No Faff"
#define MyAppPublisherURL "https://github.com/no-faff/InstallerClean"
#define MyAppSupportURL "https://github.com/no-faff/InstallerClean/discussions"
#define CurrentYear GetDateTimeString('yyyy','','')
#define SourcePath "..\publish\cli"
#define AppVersion() GetVersionComponents(SourcePath + "\InstallerClean.exe", Local[0], Local[1], Local[2], Local[3]), str(Local[0]) + "." + str(Local[1]) + "." + str(Local[2])

[Setup]
#ifndef AppVersion
  #define AppVersion "1.9.0"
#endif

AppId={#MyAppName}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppVerName={#MyAppName} {#AppVersion}

; Mutex name matches App.xaml.cs and Cli/Program.cs. Setup pauses with
; a "close the running app" prompt when the user upgrades while
; InstallerClean.exe or installerclean-cli.exe is holding it.
AppMutex=Global\InstallerClean_SingleInstance
; %LOCALAPPDATA%\NoFaff\InstallerClean\ user data (settings.json,
; last-run.json, settings.json.bad on a corrupt-and-recovered run,
; crash.log) survives uninstall by design: the saved move destination
; and the lifetime result-log lock carry across upgrades.
AppPublisher={#MyAppAuthor}
AppPublisherURL=https://github.com/no-faff/InstallerClean
AppSupportURL=https://github.com/no-faff/InstallerClean/discussions
AppCopyright=(c) {#CurrentYear} {#MyAppAuthor}
; Win32 VS_FIXEDFILEINFO is a four-part version; AppVersion is three,
; so VersionInfoVersion / VersionInfoProductVersion pad with .0.

VersionInfoVersion={#AppVersion}
VersionInfoProductVersion={#AppVersion}
VersionInfoProductName={#MyAppName}
VersionInfoCompany={#MyAppAuthor}
VersionInfoCopyright=(c) {#CurrentYear} {#MyAppAuthor}
VersionInfoDescription={#MyAppName} Setup

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppName}.exe
OutputDir=..\publish
OutputBaseFilename={#MyAppName}-setup
; Compression=bzip; SolidCompression=no. Every other Inno
; compression combination tested on this project has tripped a
; static-ML false positive on the setup hash: lzma2 trips
; DeepInstinct, zip (with or without SolidCompression=yes) trips
; Arctic Wolf or DeepInstinct depending on the embedded portable's
; runtime compression. bzip cleared every VirusTotal engine.
Compression=bzip
SolidCompression=no
; The .NET 10 Desktop Runtime's oldest supported Windows release is
; Windows 10 version 1607 (build 14393), so an older build is blocked here
; with a clear message instead of failing cryptically at first launch. Inno
; Setup reads the true build via RtlGetVersion, which is not subject to the
; GetVersionEx compatibility cap, so the build-level floor is enforced on
; Windows 10 and 11.
MinVersion=10.0.14393
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
SetupIconFile=..\src\InstallerClean\Assets\app.ico
DisableProgramGroupPage=yes

WizardStyle=modern dynamic
WizardImageFile=wizard-image.bmp
WizardImageFileDynamicDark=wizard-image-dark.bmp
WizardSmallImageFile=wizard-small.png
WizardSmallImageFileDynamicDark=wizard-small.png
WizardImageAlphaFormat=defined

ShowLanguageDialog=yes
UsePreviousLanguage=no
LanguageDetectionMethod=uilanguage

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[CustomMessages]
english.WelcomeLabel1=Welcome to {#MyAppName} setup
english.WelcomeLabel2=This will install {#MyAppName} on your computer.
english.FinishedHeadingLabel=Setup complete
english.FinishedLabel={#MyAppName} has been installed on your computer.
english.ClickFinish=Click Finish to close setup.
english.UninstallApp=Uninstall {#MyAppName}
english.LaunchApp=Launch {#MyAppName}

italian.WelcomeLabel1=Benvenuto nell'installazione di {#MyAppName}
italian.WelcomeLabel2=Questo installerà {#MyAppName} nel computer.
italian.FinishedHeadingLabel=Installazione completata
italian.FinishedLabel={#MyAppName} è stato installato nel computer.
italian.ClickFinish=Per chiudere l'installazione seleziona 'Fine'.
italian.UninstallApp=Disinstalla {#MyAppName}
italian.LaunchApp=Esegui {#MyAppName}

[Files]
Source: "{#SourcePath}\InstallerClean.exe"; DestDir: "{app}"; Flags: ignoreversion
; CLI is a .NET console exe published from src/InstallerClean.Cli;
; ships alongside the GUI so PowerShell and cmd block on the process
; subsystem naturally.
Source: "{#SourcePath}\installerclean-cli.exe"; DestDir: "{app}"; Flags: ignoreversion
; pad.xml's Distribution_Permissions requires the MIT licence text to
; travel alongside any redistributed binary, so Setup installs it too.
; DestName gives the installed copy a .txt extension so a double-click opens it
; in Notepad; a bare "LICENSE" with no extension makes Windows show the "how do
; you want to open this file?" picker instead.
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\InstallerClean.exe"; IconFilename: "{app}\InstallerClean.exe"
Name: "{group}\{cm:UninstallApp}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\InstallerClean.exe"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent shellexec
