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
; The published binaries and the setup output all sit under ..\publish;
; defined once here so the [Files] sources and OutputDir cannot drift apart.
#define PublishDir "..\publish"
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
OutputDir={#PublishDir}
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

; Language, message and custom-message definitions live in their own file so the
; main script stays on install logic; each added language touches only that file.
; It keeps a UTF-8 BOM (it carries accented strings); see its header.
#include "InstallerClean_Languages.iss"

[Files]
Source: "{#PublishDir}\self-contained\InstallerClean.exe"; DestDir: "{app}"; Flags: ignoreversion
; CLI is a .NET console exe published from src/InstallerClean.Cli;
; ships alongside the GUI so PowerShell and cmd block on the process
; subsystem naturally.
Source: "{#PublishDir}\cli\installerclean-cli.exe"; DestDir: "{app}"; Flags: ignoreversion
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
