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
  #define AppVersion "1.9.1"
#endif
; Copyright year comes from the build clock (compile-time) so the
; notice never goes stale.
#define CurrentYear GetDateTimeString('yyyy','','')
; The publisher name and repo URL each appear in several directives below;
; defined once here so they cannot drift apart.
#define MyCompany "No Faff"
#define MyRepoUrl "https://github.com/no-faff/InstallerClean"
AppId=InstallerClean
AppName=InstallerClean
AppVersion={#AppVersion}
; Mutex name matches App.xaml.cs and Cli/Program.cs. Setup pauses with
; a "close the running app" prompt when the user upgrades while
; InstallerClean.exe or installerclean-cli.exe is holding it.
AppMutex=Global\InstallerClean_SingleInstance
; %LOCALAPPDATA%\NoFaff\InstallerClean\ user data (settings.json,
; last-run.json, settings.json.bad on a corrupt-and-recovered run,
; crash.log) survives uninstall by design: the saved move destination
; and the lifetime result-log lock carry across upgrades.
AppPublisher={#MyCompany}
AppPublisherURL={#MyRepoUrl}
AppSupportURL={#MyRepoUrl}/discussions
AppCopyright=(c) {#CurrentYear} {#MyCompany}
; Win32 VS_FIXEDFILEINFO is a four-part version; AppVersion is three,
; so VersionInfoVersion / VersionInfoProductVersion pad with .0.
VersionInfoVersion={#AppVersion}.0
VersionInfoProductVersion={#AppVersion}.0
VersionInfoProductName=InstallerClean
VersionInfoCompany={#MyCompany}
VersionInfoCopyright=(c) {#CurrentYear} {#MyCompany}
VersionInfoDescription=InstallerClean Setup
DefaultDirName={autopf}\InstallerClean
DefaultGroupName=InstallerClean
UninstallDisplayIcon={app}\InstallerClean.exe
OutputDir=..\publish
OutputBaseFilename=InstallerClean-setup
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
WizardStyle=modern dynamic
DisableProgramGroupPage=yes
WizardImageFile=wizard-image.bmp
WizardImageFileDynamicDark=wizard-image-dark.bmp
WizardSmallImageFile=wizard-small.png
WizardSmallImageFileDynamicDark=wizard-small.png
WizardImageAlphaFormat=defined
ShowLanguageDialog=yes
; Re-detect the wizard language each run rather than reusing the previous
; install's pick, so a language added in a later version becomes the default
; for an upgrading user whose OS matches it; the dialog still lists them all.
UsePreviousLanguage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

; Welcome/Finished are standard Inno [Messages], overridden per language with
; the language-name prefix. They must live here, NOT in [CustomMessages]:
; [CustomMessages] entries are only reachable via {cm:Name} and would not
; override the wizard's own text. Italian.isl supplies the rest of the wizard.
[Messages]
english.WelcomeLabel1=Welcome to InstallerClean setup
english.WelcomeLabel2=This will install InstallerClean on your computer.
english.FinishedHeadingLabel=Setup complete
english.FinishedLabel=InstallerClean has been installed on your computer.
english.ClickFinish=Click Finish to close setup.
italian.WelcomeLabel1=Benvenuto nell'installazione di InstallerClean
italian.WelcomeLabel2=Questo installerà InstallerClean nel computer.
italian.FinishedHeadingLabel=Installazione completata
italian.FinishedLabel=InstallerClean è stato installato nel computer.
italian.ClickFinish=Per chiudere l'installazione seleziona 'Fine'.

[CustomMessages]
english.UninstallApp=Uninstall InstallerClean
english.LaunchApp=Launch InstallerClean
italian.UninstallApp=Disinstalla InstallerClean
italian.LaunchApp=Esegui InstallerClean

[Files]
Source: "..\publish\self-contained\InstallerClean.exe"; DestDir: "{app}"; Flags: ignoreversion
; CLI is a .NET console exe published from src/InstallerClean.Cli;
; ships alongside the GUI so PowerShell and cmd block on the process
; subsystem naturally.
Source: "..\publish\cli\installerclean-cli.exe"; DestDir: "{app}"; Flags: ignoreversion
; pad.xml's Distribution_Permissions requires the MIT licence text to
; travel alongside any redistributed binary, so Setup installs it too.
; DestName gives the installed copy a .txt extension so a double-click opens it
; in Notepad; a bare "LICENSE" with no extension makes Windows show the "how do
; you want to open this file?" picker instead.
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\InstallerClean"; Filename: "{app}\InstallerClean.exe"; IconFilename: "{app}\InstallerClean.exe"
Name: "{group}\{cm:UninstallApp}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\InstallerClean.exe"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent shellexec
