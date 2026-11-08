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
; The single-instance mutex, named by AppMutex and by the wait in [Code]. It matches
; InstanceLock.Name in src/InstallerClean.Core/Services/InstanceLock.cs, and
; scripts/check-instance-lock-name.mjs holds the two together.
#define AppMutexName "Global\InstallerClean_SingleInstance"
; The mutex Setup holds while it runs, named by SetupMutex, so a second setup does
; not start while one is renaming and writing the files in {app}.
#define SetupMutexName "Global\InstallerClean_Setup"
; What [Code] adds to the name of each file it holds aside while Setup writes. The
; window and the command line never delete a file under this name, which can be the
; only copy of an earlier version; Setup moves it back, or deletes it once an install
; has completed, and the uninstaller deletes it.
#define HeldSuffix ".setup-old"
; What [Code] renames a file to where it is still running and cannot be deleted, and
; is not needed: an earlier version once an install has completed, or a newer one a
; failed install left. It matches ExeReplacement.SetupDoneSuffix in
; src/InstallerClean.Core/Services/ExeReplacement.cs: the window and the command line
; delete a file under this name beside their own exe as they start, Setup deletes
; every one once an install has completed, and scripts/check-setup-held-copies.mjs
; holds the two names together.
#define DoneSuffix ".setup-done"
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
; Setup pauses with a "close the running app" prompt while the mutex's name can
; be opened, which it can while the window or a command-line Delete or Move has it
; open, held or not. A command-line scan never opens it, so Setup can meet one
; running from {app}; [Code] holds each file aside before it is written for that
; reason (HoldAllAside below). Started by an update, from the window or the
; command line, Setup first waits for the app to close (InitializeSetup below).
AppMutex={#AppMutexName}
SetupMutex={#SetupMutexName}
; Setup does not close programs holding the files it replaces: a running
; command-line scan would be ended part way through, and [Code] renames each file
; aside instead, which Windows allows while it runs.
CloseApplications=no
; %LOCALAPPDATA%\InstallerClean\ user data (settings.json,
; last-run.json, settings.json.bad on a corrupt-and-recovered run,
; crash.log, crash.log.old once the log has rotated) survives
; uninstall by design: the saved move destination, and settings.json's
; record of whether this account's report has gone or is still waiting
; to go, carry across upgrades.
; The CLI's Application event-log source, registered on its first run at
; HKLM\SYSTEM\CurrentControlSet\Services\EventLog\Application\InstallerClean,
; survives too, and no [Registry] or [UninstallDelete] entry should be added
; to remove it: Event Viewer resolves an entry's description through its
; source, so deleting the source turns every audit entry the CLI has already
; written into "the description for Event ID ... cannot be found".
; The first-run mark, the value FirstRunRecorded under
; HKLM\SOFTWARE\NoFaff\InstallerClean, survives too, and no [Registry] or
; [UninstallDelete] entry should be added for it: removed, it would make a
; reinstall read as the PC's first run.
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
; The version is part of the download's name, so a setup exe sitting in a
; Downloads folder months later says which release it is before anybody opens
; its properties. The release pipeline builds the same name from the same
; version to find this file afterwards, so the two have to agree.
OutputBaseFilename=InstallerClean-{#AppVersion}-setup
; Compression=bzip; SolidCompression=no. Static antivirus models score a setup's
; hash differently under each compression setting, and lzma2 and zip draw false
; positives on this one more often than bzip does. Changing either line means
; scanning the setup again.
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
; The uninstaller is signed only by the release workflow, which defines
; UninstallerSignatureDir. With SignedUninstaller=yes and no SignTool, the
; compiler writes the uninstaller it is about to embed into that folder as
; uninst-<Inno Setup version>-<hash>.e32 and stops, unless a signed copy of that
; exact file is already there. The workflow compiles once to get the file, has it
; signed with the executables, puts the signed copy back and compiles again. The
; hash is of the file's contents, so the second compile stops the same way if
; anything that goes into the uninstaller changed in between. An installed
; unins000.exe then carries the setup's publisher, and that is the name Windows
; shows when uninstalling asks for administrator rights. Setup also writes the
; uninstaller's messages to unins000.msg beside it, because they cannot be added
; to a signed file. Every other build leaves the name undefined and embeds an
; unsigned uninstaller.
#ifdef UninstallerSignatureDir
SignedUninstaller=yes
SignedUninstallerDir={#UninstallerSignatureDir}
#endif

; Language, message and custom-message definitions live in their own file so the
; main script stays on install logic; each added language touches only that file.
; It is UTF-8 (it carries text in many languages); see its header.
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
; embeds the fonts and icons THIRD-PARTY-NOTICES names, each under its own
; licence, InstallerClean.Core carries the code it names from the .NET runtime,
; and this setup compiles in the wizard translations carried under
; installer\Languages. The DestName trick is the LICENSE one and is needed for
; the same reason.
Source: "..\THIRD-PARTY-NOTICES"; DestDir: "{app}"; DestName: "THIRD-PARTY-NOTICES.txt"; Flags: ignoreversion

[UninstallDelete]
; A file [Code] renamed aside and left: an earlier one still running when the
; install completed or a failed install could not move back, or a newer one
; still running after a failed install.
Type: files; Name: "{app}\*{#HeldSuffix}"
Type: files; Name: "{app}\*{#HeldSuffix}-*"
Type: files; Name: "{app}\*{#DoneSuffix}"
Type: files; Name: "{app}\*{#DoneSuffix}-*"

[Icons]
Name: "{group}\InstallerClean"; Filename: "{app}\InstallerClean.exe"; IconFilename: "{app}\InstallerClean.exe"
Name: "{group}\{cm:UninstallApp}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\InstallerClean.exe"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent shellexec
; The app's Update now starts this setup with /SILENT and /UPDATEFROMAPP and then
; closes, and the app opens again once the files are in place. skipifsilent
; skips the entry above on every silent install, winget's and Chocolatey's
; included, so this one does the opening, and only where UpdatingFromApp finds the
; app's own switch on the command line.
Filename: "{app}\InstallerClean.exe"; Flags: nowait shellexec; Check: UpdatingFromApp

[Code]
// Whether Setup's command line carries Switch. Setup takes no meaning from a switch
// it does not know and hands it to this code through ParamStr.
function HasSwitch(const Switch: String): Boolean;
var
  I: Integer;
begin
  Result := False;
  for I := 1 to ParamCount do
    if CompareText(ParamStr(I), Switch) = 0 then
    begin
      Result := True;
      Exit;
    end;
end;

// Whether the setup was started by the app's Update now, which passes
// /UPDATEFROMAPP. Every version of the app passes it to every later
// setup, so it keeps this meaning.
function UpdatingFromApp: Boolean;
begin
  Result := HasSwitch('/UPDATEFROMAPP');
end;

// Whether the setup was started by an update of the command line, which passes
// /UPDATEFROMCLI. The command line passes it to the setup of the version it updates
// to, so it keeps this meaning. It opens nothing: nobody may be watching, and the
// [Run] entry that opens the app checks UpdatingFromApp alone.
function UpdatingFromCommandLine: Boolean;
begin
  Result := HasSwitch('/UPDATEFROMCLI');
end;

// Started by an update, Setup waits for every copy of the app to close before it
// checks AppMutex, for up to 30 seconds: the copy that starts Setup, or the one it
// stands for, may still be closing, and its handle to the mutex stays open until it
// closes it as it exits. Setup calls InitializeSetup before its own AppMutex check,
// which then asks for InstallerClean to be closed where the mutex is still open, and
// fails the setup with nothing written where /SUPPRESSMSGBOXES answers that box.
function InitializeSetup: Boolean;
var
  Waited: Integer;
begin
  Result := True;
  if not (UpdatingFromApp or UpdatingFromCommandLine) then Exit;
  Waited := 0;
  while CheckForMutexes('{#AppMutexName}') and (Waited < 30000) do
  begin
    Sleep(250);
    Waited := Waited + 250;
  end;
  if Waited > 0 then
    Log(Format('Waited %d ms for InstallerClean to close.', [Waited]));
end;

// Before any file is written, HoldAllAside renames each file [Files] writes to {app}
// aside, and Setup writes every one as new. Inno Setup replaces a file in place by
// deleting it and moving its own copy in, and Windows refuses to delete a running
// program, such as a command-line scan, which holds no mutex; it allows renaming one.
// A failed setup undoes only what it added, so a file it had replaced in place would
// stay the newer version beside the older ones; a file renamed aside is moved back
// instead. Where one file cannot be renamed, the others are moved back and Setup stops
// before writing anything. Where the install fails, Setup deletes what it wrote and
// DeinitializeSetup moves each file back; where it completes, each file held aside is
// deleted, or moved back where Setup wrote nothing in its place, and one still running
// is renamed to a done name, which the copy at the path deletes at its next start, as
// do the next completed install and the uninstaller. scripts/check-setup-held-copies.mjs
// fails where AppFile leaves out a file [Files] writes to {app}.
var
  HeldFrom: array of String;
  HeldAt: array of String;
  HeldSkipped: array of Boolean;
  HeldFailed: String;
  CancelClicked: Boolean;
  Installed: Boolean;

// The Ith file [Files] writes to {app}, by its name there, counting from 0, and ''
// past the last. AppFileCount counts them.
function AppFile(I: Integer): String;
begin
  case I of
    0: Result := 'InstallerClean.exe';
    1: Result := 'installerclean-cli.exe';
    2: Result := 'LICENSE.txt';
    3: Result := 'THIRD-PARTY-NOTICES.txt';
  else
    Result := '';
  end;
end;

function AppFileCount: Integer;
begin
  Result := 0;
  while AppFile(Result) <> '' do Result := Result + 1;
end;

// MoveFileW and DeleteFileW from kernel32.dll, so DLLGetLastError answers the error
// each one set.
function MoveFileW(ExistingName, NewName: String): Boolean;
  external 'MoveFileW@kernel32.dll stdcall';
function DeleteFileW(Name: String): Boolean;
  external 'DeleteFileW@kernel32.dll stdcall';

const
  ERROR_ACCESS_DENIED = 5;
  ERROR_SHARING_VIOLATION = 32;
  ERROR_ALREADY_EXISTS = 183;

// The Nth name with Suffix a file at Path can be renamed to.
function AsideName(const Path, Suffix: String; N: Integer): String;
begin
  if N = 0 then
    Result := Path + Suffix
  else
    Result := Path + Suffix + '-' + IntToStr(N);
end;

// The first name with Suffix for Path with nothing at it, or '' where all are taken. A
// held name already in use can be the only copy of an earlier file, left by a setup
// that could not move it back, so a name is never reused.
function FreeAsideName(const Path, Suffix: String): String;
var
  N: Integer;
begin
  Result := '';
  for N := 0 to 99 do
    if not FileOrDirExists(AsideName(Path, Suffix, N)) then
    begin
      Result := AsideName(Path, Suffix, N);
      Exit;
    end;
end;

// Renames Path to a free name made of Base and Suffix and answers it, or '' with the
// reason in the setup log. A rename refused while another program has the file open
// without sharing its delete, as an antivirus can, is tried again for up to 5 seconds;
// a name taken meanwhile is passed over for the next free one.
function RenameAside(const Path, Base, Suffix: String): String;
var
  Held: String;
  Tried, Error: Integer;
begin
  Result := '';
  Tried := 0;
  while True do
  begin
    Held := FreeAsideName(Base, Suffix);
    if Held = '' then
    begin
      Log(Format('No free name ending %s for %s.', [Suffix, Path]));
      Exit;
    end;
    if MoveFileW(Path, Held) then
    begin
      Result := Held;
      Exit;
    end;
    Error := DLLGetLastError;
    if ((Error <> ERROR_SHARING_VIOLATION) and (Error <> ERROR_ACCESS_DENIED) and (Error <> ERROR_ALREADY_EXISTS))
      or (Tried >= 20) then
    begin
      Log(Format('Could not rename %s to %s: %s', [Path, Held, SysErrorMessage(Error)]));
      Exit;
    end;
    if Error <> ERROR_ALREADY_EXISTS then Sleep(250);
    Tried := Tried + 1;
  end;
end;

// Renames the file Name in {app} aside, where there is one. Answers False where it is
// there and could not be renamed.
function HoldAside(const Name: String): Boolean;
var
  Path, Held: String;
  I: Integer;
begin
  Result := True;
  Path := ExpandConstant('{app}\') + Name;
  if not FileExists(Path) then Exit;
  Held := RenameAside(Path, Path, '{#HeldSuffix}');
  if Held = '' then
  begin
    HeldFailed := Path;
    Result := False;
    Exit;
  end;
  Log(Format('Held %s aside as %s.', [Path, Held]));
  I := GetArrayLength(HeldFrom);
  SetArrayLength(HeldFrom, I + 1);
  SetArrayLength(HeldAt, I + 1);
  SetArrayLength(HeldSkipped, I + 1);
  HeldFrom[I] := Path;
  HeldAt[I] := Held;
  HeldSkipped[I] := False;
end;

// Holds aside every file [Files] writes to {app}, stopping at the first that cannot be.
// Each name stays empty from its rename until Setup writes the file: a command-line run
// started then finds no exe there, and the earlier file waits under its held name,
// which stays until a later setup writes the file and deletes it, or the uninstaller
// does, where this one is cut off.
function HoldAllAside: Boolean;
var
  I: Integer;
begin
  Result := True;
  for I := 0 to AppFileCount - 1 do
  begin
    Result := HoldAside(AppFile(I));
    if not Result then Exit;
  end;
end;

// Moves each file held aside back to its name. Whatever Setup left at the name is
// deleted, or where it is running, renamed aside itself, since the name is free only
// once it moves. Every failure is written to the setup log with the error Windows gave.
procedure PutHeldBack;
var
  I, Error: Integer;
  Left: String;
begin
  for I := GetArrayLength(HeldAt) - 1 downto 0 do
  begin
    if FileExists(HeldFrom[I]) and not DeleteFileW(HeldFrom[I]) then
    begin
      Error := DLLGetLastError;
      Left := RenameAside(HeldFrom[I], HeldFrom[I], '{#DoneSuffix}');
      if Left = '' then
      begin
        Log(Format('Could not delete %s: %s. The earlier file is left as %s.', [
          HeldFrom[I], SysErrorMessage(Error), HeldAt[I]]));
        Continue;
      end;
      Log(Format('Could not delete %s: %s. It was renamed to %s.', [HeldFrom[I], SysErrorMessage(Error), Left]));
    end;
    if MoveFileW(HeldAt[I], HeldFrom[I]) then
      Log(Format('Put %s back as %s.', [HeldAt[I], HeldFrom[I]]))
    else
      Log(Format('Could not rename %s back to %s: %s. The earlier file is left as %s.', [
        HeldAt[I], HeldFrom[I], SysErrorMessage(DLLGetLastError), HeldAt[I]]));
  end;
  SetArrayLength(HeldFrom, 0);
  SetArrayLength(HeldAt, 0);
  SetArrayLength(HeldSkipped, 0);
end;

// Deletes every file under a held or a done name for the file Name in {app}, other than
// those this setup renamed aside and could not delete, where this install wrote a file
// at that name. Where it wrote none there, a held file may be the only copy, so all stay.
procedure DeleteLeftAside(const Name: String);
var
  Path, Aside: String;
  I, N, S: Integer;
  Ours: Boolean;
begin
  Path := ExpandConstant('{app}\') + Name;
  if not FileExists(Path) then Exit;
  for I := 0 to GetArrayLength(HeldFrom) - 1 do
    if HeldSkipped[I] and (CompareText(HeldFrom[I], Path) = 0) then Exit;
  for S := 0 to 1 do
    for N := 0 to 99 do
    begin
      if S = 0 then
        Aside := AsideName(Path, '{#HeldSuffix}', N)
      else
        Aside := AsideName(Path, '{#DoneSuffix}', N);
      if not FileExists(Aside) then Continue;
      Ours := False;
      for I := 0 to GetArrayLength(HeldAt) - 1 do
        if CompareText(HeldAt[I], Aside) = 0 then Ours := True;
      if not Ours and DeleteFileW(Aside) then Log(Format('Deleted %s, left by an earlier setup.', [Aside]));
    end;
end;

// Setup's progress window has a Cancel button on the silent routes too. The question
// whether to stop is asked here rather than by Setup, so that only a setup the person
// stopped by answering yes is kept from being reported to the window as a failed
// update. Setup calls this without asking only for a cancel after it has failed to
// prepare the install, which is reported as the failed update it is.
procedure CancelButtonClick(CurPageID: Integer; var Cancel, Confirm: Boolean);
begin
  if Confirm then
  begin
    Cancel := ExitSetupMsgBox;
    Confirm := False;
    if Cancel then CancelClicked := True;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  I: Integer;
  Done: String;
begin
  if CurStep = ssInstall then
  begin
    if not HoldAllAside then
    begin
      // Nothing has been written yet, so putting back what moved leaves {app} as it was.
      PutHeldBack;
      // The message Setup shows where it cannot replace a file, in the wizard's language.
      SuppressibleMsgBox(SetupMessage(msgErrorReplacingExistingFile) + #13#10 + HeldFailed, mbError, MB_OK, IDOK);
      Abort;
    end;
  end
  else if CurStep = ssPostInstall then
  begin
    Installed := True;
    for I := 0 to GetArrayLength(HeldAt) - 1 do
      // A file whose write was skipped, by Ignore in Setup's error box, has nothing
      // newer at its name, so the earlier one goes back there.
      if not FileExists(HeldFrom[I]) then
      begin
        HeldSkipped[I] := True;
        if MoveFileW(HeldAt[I], HeldFrom[I]) then
          Log(Format('Put %s back as %s: nothing was written in its place.', [HeldAt[I], HeldFrom[I]]))
        else
          Log(Format('Could not rename %s back to %s: %s.', [HeldAt[I], HeldFrom[I], SysErrorMessage(DLLGetLastError)]));
      end
      else if not DeleteFileW(HeldAt[I]) then
      begin
        Log(Format('Could not delete %s: %s.', [HeldAt[I], SysErrorMessage(DLLGetLastError)]));
        Done := RenameAside(HeldAt[I], HeldFrom[I], '{#DoneSuffix}');
        if Done <> '' then
        begin
          Log(Format('Renamed %s to %s, for the next start.', [HeldAt[I], Done]));
          HeldAt[I] := Done;
        end;
      end;
    // Where this install wrote a file at a name, every file an earlier setup left under
    // that name's held or done names is from before it and not needed. One still
    // running stays, and DeleteLeftAside keeps all of them where it wrote none.
    for I := 0 to AppFileCount - 1 do
      DeleteLeftAside(AppFile(I));
  end;
end;

// Called however Setup ends, after a failed install has been rolled back.
procedure DeinitializeSetup;
var
  ErrorCode: Integer;
  App, Args: String;
begin
  if Installed then Exit;
  PutHeldBack;
  // Update now closes the window before this setup starts, and the [Run] entry that
  // opens it again runs only after a completed install, so where the install did not
  // complete the window is opened here, as whatever is now at its path, with the
  // switch that has it say the update failed (HandOverContract.UpdateFailedSwitch),
  // unless the person cancelled the setup. A version that does not read the switch
  // opens as it always does.
  if UpdatingFromApp then
  begin
    // {app} is set only once Setup has read the earlier install's folder, so a setup
    // that stopped before then, at its AppMutex check, opens nothing and says so.
    try
      App := ExpandConstant('{app}\InstallerClean.exe');
    except
      App := '';
    end;
    if CancelClicked then Args := '' else Args := '/updatefailed';
    if App = '' then
      Log('The install folder was not known yet, so the app was not opened.')
    else if not FileExists(App) then
      Log(Format('%s is not there, so the app was not opened.', [App]))
    else if not ShellExec('', App, Args, '', SW_SHOWNORMAL, ewNoWait, ErrorCode) then
      Log(Format('Could not open %s: %s', [App, SysErrorMessage(ErrorCode)]));
  end;
end;
