; InstallerClean Inno Setup script.
;
; AppId stays constant across versions ("InstallerClean") so Windows
; treats every shipped version as the same product; the uninstall
; entry lives at HKLM\Software\Microsoft\Windows\CurrentVersion\
; Uninstall\InstallerClean_is1. Changing AppId breaks Add/Remove
; Programs continuity across versions.
;
; AppVersion is passed in by CI and by the release script, both reading it
; from Directory.Build.props: "ISCC.exe /DAppVersion=X.Y.Z ...". A compile
; without it fails here rather than falling back, and there is deliberately no
; fallback to add: any hand-maintained default tracks the shipping version only
; until the version bump that forgets it, and a stale one produces a setup whose
; Add/Remove Programs entry states a version the binaries inside it are not,
; silently, on the one build nobody checks. There is no correct value to fall
; back TO.
[Setup]
#ifndef AppVersion
  #error AppVersion is not defined. Pass it on the command line, reading the value from Directory.Build.props: ISCC.exe /DAppVersion=X.Y.Z installer\InstallerClean.iss
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
; AppVerName sets the name+version string Inno shows in the wizard title bar; set
; explicitly so it reads "InstallerClean <version>" with no localised
; "version"/"versione" word. Inno Setup 6's default includes that word in every
; language and offers no [Messages] override for it (an "<lang>.NameAndVersion"
; entry is unrecognised and silently ignored). Inno Setup 7 drops the word from
; the default, so on 7.x this directive is redundant but harmless; keeping it
; means the title stays correct on either compiler. The Add/Remove Programs entry
; is named by UninstallDisplayName further down and does not follow this
; directive.
AppVerName=InstallerClean {#AppVersion}
; Mutex name matches App.xaml.cs and Cli/Program.cs. Setup pauses with
; a "close the running app" prompt when the user upgrades while
; InstallerClean.exe or installerclean-cli.exe is holding it.
AppMutex=Global\InstallerClean_SingleInstance
; %LOCALAPPDATA%\NoFaff\InstallerClean\ user data (settings.json,
; last-run.json, settings.json.bad on a corrupt-and-recovered run,
; crash.log, crash.log.old once the log has rotated) survives
; uninstall by design: the saved move destination and the lifetime
; result-log lock carry across upgrades.
; The CLI's Application event-log source, registered on its first run at
; HKLM\SYSTEM\CurrentControlSet\Services\EventLog\Application\InstallerClean,
; survives too, and no [Registry] or [UninstallDelete] entry should be added
; to remove it: Event Viewer resolves an entry's description through its
; source, so deleting the source turns every audit entry the CLI has already
; written into "the description for Event ID ... cannot be found".
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
; The name Windows shows in Apps and Features. Inno's default here is AppVerName,
; which would carry the version into a list that already prints it in a column of
; its own, so the entry would read it twice. Setting the name explicitly leaves
; the wizard title bar alone. It does not change what winget matches on either:
; the published manifest correlates on the product code and carries no display
; name at all.
UninstallDisplayName=InstallerClean
UninstallDisplayIcon={app}\InstallerClean.exe
OutputDir={#PublishDir}
; The version is part of the download's name from 2.2.0 on. A setup exe sitting
; in a Downloads folder months later otherwise has nothing on it to say which
; release it is, and neither does the SHA-256 sidecar published beside it. The
; release pipeline builds the same name from the same version to find this
; file afterwards, so the two have to agree.
OutputBaseFilename=InstallerClean-{#AppVersion}-setup
; Compression=bzip; SolidCompression=no. Every other Inno
; compression combination tested on this project has tripped a
; static-ML false positive on the setup hash: lzma2 trips one
; engine, and zip (with or without SolidCompression=yes) trips one
; of two, which of them depending on the embedded portable's
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
; pad.xml's Distribution_Permissions requires the Apache 2.0 licence text to
; travel alongside any redistributed binary, so Setup installs it too.
; DestName gives the installed copy a .txt extension so a double-click opens it
; in Notepad; a bare "LICENSE" with no extension makes Windows show the "how do
; you want to open this file?" picker instead.
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion
; Same argument, for what is redistributed rather than what is licensed: the GUI
; embeds four Poppins faces under the SIL Open Font License, and this setup
; compiles in four community Inno Setup translations. The DestName trick is the
; LICENSE one and is needed for the same reason.
Source: "..\THIRD-PARTY-NOTICES"; DestDir: "{app}"; DestName: "THIRD-PARTY-NOTICES.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\InstallerClean"; Filename: "{app}\InstallerClean.exe"; IconFilename: "{app}\InstallerClean.exe"
Name: "{group}\{cm:UninstallApp}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\InstallerClean.exe"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent shellexec
