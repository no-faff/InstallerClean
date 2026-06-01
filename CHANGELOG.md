# Changelog

Every change to InstallerClean, logged in full (not just the user-facing highlights). Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- Scan-cancel status now updates the instant Esc is pressed. `CancelScan` set the "Cancelling..." status after cancelling the token; reordering it before the cancel makes the synchronous write land immediately (the scan's own progress reporter only fires on its next callback) and guarantees the scan's later "Scan cancelled." write lands last. On the single UI thread the old order was harmless; the race only showed in a SynchronizationContext-free unit test, where it flaked in CI. No user-visible change on the running app.

## [1.8.2] - 2026-05-27

### Fixed

- Missing-from-disk banner no longer fires for a benign case. A registered patch marked superseded by the MSI database whose file had already been removed (by an older cleaner or a manual sweep) used to count into the same total as a non-removable package gone missing, and the banner text ("Windows refers to installer files that aren't there. Cleaning the cache won't change this") was wrong for the superseded-and-already-gone case. A machine that had been cleaned before could see a permanent banner suggesting the system was broken when it was fine. The two counts are now separate; the banner fires only on the non-removable population.
- CLI single-instance mutex now releases on the acquiring thread. Main acquired the mutex synchronously but the post-await `finally` ran on a thread-pool thread, and `Mutex.ReleaseMutex` throws `ApplicationException` from any thread other than the one that owned it. Main is now sync-over-async around a synchronous mutex acquire/release, so the release runs on the entry thread; previously, the release threw and propagated as an unhandled exception, the process exited, and the next CLI or GUI launch hit the abandoned-mutex recovery path.
- Cleanup view-model now cancels its in-flight operation before disposing the cancellation token source on app shutdown. Closing the window mid-Move or mid-Delete previously surfaced an ObjectDisposedException on the worker; the outer catch wrote to crash.log and the in-progress file operation stopped wherever it was. The worker now sees OperationCanceledException at its next checkpoint and runs through the normal cancellation summary path.
- Result-log write at startup-scan completion survives the dispatcher shutting down mid-await. `OnScanCompleted` is async void; if the user closes the window between scan finish and the result-log POST returning, the await previously tried to resume on the captured dispatcher and the outer catch logged a dispatcher exception to crash.log. ConfigureAwait(false) on the WriteAsync resumes off the dispatcher; the post-await action is plain field writes that do not need it.
- CLI `/m <path>` argument now goes through the same `IsSystemFolderOrChild` guard as the settings-loaded fallback. A stale Scheduled Task argument carrying `/m C:\Windows\System32\Spool` previously cleared only the inside-`C:\Windows\Installer` gate.
- CLI no longer echoes a framework-raised `UnauthorizedAccessException.Message` to stdout. The two production throw sites that carry a resx-sourced safe-to-display message opt in via a new `LocalisedAccessException` sentinel type; a BCL-raised UAE from deep in the framework falls through to the generic crash-log catch with a type-name only.
- CLI mutex-block path no longer prints the GUI's "InstallerClean is already running" dialog body. New `Cli.MutexBlocked` resx names the contending parties (GUI or another CLI run) and points at exit code 75 (transient, safe to retry).
- GUI's Move destination textbox now goes through the same `IsSystemFolderOrChild` gate the CLI uses. A user typing or pasting `C:\Windows\System32\Spool` previously cleared only the inside-`C:\Windows\Installer` gate; with both gates applied, an accidental system-folder destination is refused before any file moves.
- MoveFilesService's five validation throws (not-fully-qualified destination, IsInstallerFolderOrChild race, destination-changed-mid-batch, write-probe failure) now use the `LocalisedInvalidOperationException` / `LocalisedAccessException` sentinel types introduced for the scan service. Both the GUI's CleanupViewModel and the CLI's Program.cs now catch each sentinel and surface the carefully-worded localised message; previously both showed a generic type-name + crash-log breadcrumb.
- CLI now writes an Application-channel EventLog entry on the three hard-error catch paths (LocalisedAccessException, LocalisedInvalidOperationException, generic Exception). The earlier behaviour broke the README/CHANGELOG promise that "each run writes one summary entry" precisely on the failure paths sysadmin tooling cares about most.
- CLI EventLog "X recovered" / "X relocated" lines on partial-failure runs now report actually-moved bytes (computed from the scanned removable files minus the per-file error list). Previously the line reported the scan total, silently overstating fleet-wide capacity-planning telemetry on every error.
- CLI EventLog pending-reboot reason field renders a short human label ("Windows Installer mutex held" etc.) instead of the raw enum identifier. A sysadmin grepping the Application channel reads a phrase, not "MsiExecuteMutexHeld".
- CLI `--help` exit-code line for code 75 trims to 76 characters so a default 80-column cmd.exe window doesn't wrap mid-sentence.
- CLI per-file error block emits "errors:" regardless of count, holding the documented `\d+ errors:` regex contract for RMM scripts on the one-error case.
- CLI Ctrl+C handler guards against a double-fire; the second Ctrl+C while cancellation is already in flight no longer prints "Cancelling..." a second time.
- Bare catch blocks in MutexProbe, RegistryReader, FileSystemScanService (size lookup) and InstallerCacheHelpers (prune) now name the documented expected exception types explicitly so a real memory-pressure failure (OutOfMemoryException, StackOverflowException) propagates rather than being silently absorbed as "no signal" by the surrounding gate.

### Changed

- InstallerClean-portable.exe ships ~135 MB instead of ~62 MB. The single-file LZMA-compressed embedded runtime that produced the smaller earlier shape tripped Microsoft Defender's `Trojan:Win32/Wacatac.B!ml` machine-learning heuristic as a false positive on the v1.8.2 build; the same code lineage cleared 0/70 on v1.8.1. Turning the inner compression off (the dotnet publish `EnableCompressionInSingleFile` flag) cleared every VirusTotal engine. Slim and CLI single-file builds are unaffected and unchanged in size.
- Inno Setup wrapper now uses `Compression=bzip` with `SolidCompression=no`. The previous `Compression=zip` configuration combined with the new uncompressed-payload portable inside picked up a DeepInstinct static-ML false positive on the setup hash; bzip was the only Inno compression algorithm tested that cleared every VirusTotal engine for the v1.8.2 setup.
- Orphans-list Reason column promoted from `Text.Dim` to `Text.Muted` so the load-bearing column that distinguishes Orphaned from Superseded is no longer the lowest text tier on the most semantically critical cell.
- Orphans-list now renders as a ListView + GridView (matching the registered-files window) so screen readers announce each row as column-headed cells. Previously the rows announced as single cells with the three values run together.
- Completion overlay's Done button gains Alt+D access key, matching the Alt mnemonics on the Cancel / Move / Delete / Browse / Rescan / ScanAgain buttons that previously had them.
- Result-log noun aligned across surfaces. The Send-summary button label ("Send summary") was the user-visible truth since v1.8.0, but the screen-reader Automation.Name said "diagnostic log", the failure status said "Didn't work. Never mind.", and the success status said "Result log sent". All three now say "summary"; the failure status says "Sending failed. Try again later."
- About window's Star and Buy-me-a-cuppa buttons carry distinct automation names from the main-window equivalents so a screen-reader element list with About open over Main can tell the rows apart.
- SubtleLink picks up an underline + brighten on keyboard focus matching the existing hover behaviour, so the About window's MIT licence link surfaces the same visual cue to a tabbing keyboard-only user that a mouse hover already shows.
- README gains short notes that `taskkill` bypasses `Console.CancelKeyPress` (the abandoned mutex is recovered by the next CLI run) and that Group Policy denying UAC elevation surfaces as Windows error 740 to the parent shell. Same notes ported to README.fr and README.zh-CN.
- Body explanation paragraph now templates three Reason values (Orphaned, Superseded, Obsoleted) so a translator can edit the column labels in one place and have the body copy follow. The Obsoleted case (PatchState 4, publisher-withdrawn) gets its own clause distinct from Superseded.
- `BrowserLaunchFailed*` resx keys renamed to the `BrowserLaunch.*` dotted-category prefix every other key uses.
- `installerclean-cli.csproj` pins `PublishReadyToRun=false` matching the WPF host so a future SDK feature-band change to the default cannot silently shift the CLI's R2R section count (same AV-signal-stability rationale).
- `installerclean-cli.csproj` carries an ApplicationIcon so the CLI exe paints with the Squeegee in Explorer instead of Windows's generic console-exe icon, matching the GUI sibling in the install directory.
- CLI app.manifest assemblyIdentity bumps to 1.8.2.0 (the GUI manifest was bumped earlier; the CLI was missed). Sigcheck / AppLocker rules pinned to manifest version are now consistent across the two exes.
- Result-log schema bumps to version 2. `supersededCount` now counts only PatchState=Superseded (2); a new `obsoletedCount` field counts PatchState=Obsoleted (4). v1 receivers saw both lumped under `supersededCount`. `OrphanedFile.IsSuperseded` renamed to `IsRemovablePatch` (true for both states); a new `IsObsoleted` flag isolates the obsoleted case.
- `DisplayHelpers.FormatSize` unit suffixes (GB / MB / KB / B) and `FormatElapsed` / `FormatElapsedLong` strings ("ms" / "s" / "less than a second" / "{N.N} seconds") are now sourced from resx instead of hardcoded English. The all-clean overlay receipt and every size display in the app reach a translator now.
- New `Automation.HelpText` entries on the Send-summary button (names the HTTPS endpoint), Check-for-updates button (names the GitHub releases API), and About window's MIT licence link (warns SR users the link opens a browser).
- Move destination TextBox declares `AutomationProperties.IsRequiredForForm` so SR users know it must be filled before Move enables.
- Detail-panel metadata TextBoxes in OrphanedFilesWindow and RegisteredFilesWindow now carry `AutomationProperties.Name` pointing at the visible field label, so SR users hear field names instead of "edit, read only, [value]".
- All-caps section labels (MOVE LOCATION, PRODUCTS, PATCHES, PRODUCT DETAILS) carry mixed-case `AutomationProperties.Name` overrides so Narrator's default verbosity reads them as phrases instead of spelling out individual letters.
- SplashWindow auto-focuses the Cancel button on first frame; keyboard-only users see a focus ring and can press Space without first Tab-finding it. The Cancel button's automation name now syncs with its visible "Cancelling..." label after click.
- AboutWindow's version TextBox is keyboard-reachable again so users can Tab to it and Ctrl+C the version string for a bug report; the previous `IsTabStop="False"` opt-out blocked that.
- Stale-MSI banner and Send-summary status text raise `LiveRegionChanged` explicitly on first reveal, matching the existing fix for the pending-reboot and missing-from-disk banners. WPF's UIA bridge does not re-fire LiveRegionChanged for a Visibility=Collapsed→Visible transition.

## [1.8.1] - 2026-05-13

### Changed

- All-clean completion overlay now uses the same two-tier text hierarchy as the post-Move and post-Delete overlays: the "Nothing to clean up in C:\Windows\Installer" headline renders in body weight (Summary slot), and the "Scanned N products in T" receipt renders smaller and muted (Restore slot). Both lines previously rendered at the same body weight.
- Dropped trailing full stops on every completion-overlay text line (summary, scan receipt, Move and Delete restore hints) so the overlays read consistently as labels rather than mixed sentences and labels.

## [1.8.0] - 2026-05-13

### Added

- Check for updates in About now performs the version check itself rather than opening the releases page. Single HTTPS GET to `api.github.com/repos/no-faff/InstallerClean/releases/latest` on click; UA `InstallerClean/<version>`; 8 s timeout; localised result dialog.
- Send summary on the completion overlay. Writes `%LOCALAPPDATA%\NoFaff\InstallerClean\last-run.json` after every Move, Delete or all-clear; opens a confirmation window showing the exact JSON; POSTs to `https://nofaff.netlify.app/api/result-log` on confirm. Counts and categorical labels only. No paths, no usernames, no machine identifiers, no time-of-day. Once per machine, ever.

### Changed

- "All clear" overlay heading is now "All clean".
- All-clean overlay shows the elapsed scan duration and the count of registered products scanned alongside the all-clear text.
- "{N} cleared" completion heading is now "{N} freed".
- JSON schema field `bytesCleared` renamed to `bytesFreed`; redundant `removableCount` dropped (sum of `orphanedCount` + `supersededCount`).
- "Donate" button in About renamed to "Buy me a cuppa".
- Star and Buy me a cuppa buttons in About picked up Alt+S / Alt+B accelerators; "SAY THANKS" section header above them.
- Umbrella term renamed from "orphaned files" to "unused files" in window titles, screen-reader announcements, Event Log entries and the app description. Per-file Reason values ("Orphaned" / "Superseded") unchanged.
- CLI exit code 75 reserved for transient conditions (GUI is running, Windows Installer transaction pending). The mutex-blocked path writes an Application Event Log entry under source `InstallerClean`. Stdout is UTF-8. A final "Event Log writing failed" note prints on stdout if any audit write failed during the run.
- Installer prompts to close a running InstallerClean before upgrade (`AppMutex=Global\InstallerClean_SingleInstance`). VersionInfo metadata (`VersionInfoVersion`, `ProductName`, `Company`, `Copyright`, `Description`) embedded in `InstallerClean-setup.exe`.

### Fixed

- Tab order in the main window respects visual left-to-right order. The action-row and bottom-nav DockPanels scope `KeyboardNavigation.TabIndex` locally so their values no longer interleave under the parent Grid's default `TabNavigation="Continue"`.
- Triple-click in any TextBox selects all content (class handler on `TextBox.PreviewMouseLeftButtonDownEvent` registered in `App.OnStartup`).
- Focus ring no longer appears on Alt+Tab return. The focused element's `FocusVisualStyle` is swapped to null on cross-process `Window.Deactivated` and restored on the next `PreviewKeyDown`. Logical focus is preserved across the round trip so a mid-edit TextBox keeps its caret position and Ctrl+V continues to paste.
- Screen reader: dynamic status text (scan progress, operation progress, send-summary status) and the pending-reboot and missing-from-disk banners announce on appear (`LiveSetting=Polite`).
- Read-only `SelectableText` metadata fields are keyboard-reachable (`IsTabStop=True`) so a keyboard-only user can Tab to a value and Ctrl+C it.
- Inline link colour bumped to meet WCAG AA contrast.

### Removed

- "Share what you cleared" (browser-mediated, pre-release) replaced by Send summary before tag.
- View last result log link in About (superseded by the confirmation window).

## [1.7.0] - 2026-05-05

### Changed

- Pending-reboot detection rewritten to use three narrow Windows Installer signals instead of four broad pending-reboot signals:
  - `Global\_MSIExecute` mutex is held (Windows Installer is currently writing to the cache).
  - `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\InProgress` key exists (a previous Windows Installer transaction is suspended).
  - A queued post-reboot file rename targets a path under `%SystemRoot%\Installer`.

  The four previously-checked signals (`WindowsUpdate\Auto Update\RebootRequired`, `Component Based Servicing\RebootPending`, `WindowsUpdate\Auto Update\PostRebootReporting`, broad `PendingFileRenameOperations`) had no documented relationship to Windows Installer cache safety.
- Pending-reboot banner copy and CLI message are reason-specific.

### Fixed

- Spurious "Windows is waiting to restart" banner on Windows 11 with no Windows update pending. Closes [#12](https://github.com/no-faff/InstallerClean/issues/12).

## [1.6.0] - 2026-05-05

### Added

- All-clear and completion overlays after scans, Moves and Deletes.
- CLI per-file progress (`[5/100] foo.msi`) on `/d` and `/m`.
- CLI three-state exit codes: 0 success, 1 total failure, 2 partial. 130 reserved for Ctrl+C with no committed work.
- CLI writes one Application event log entry per run under source `InstallerClean`; refuses if the source is pre-mapped to a non-Application log.
- CLI arguments are case-insensitive.
- Pending-reboot detection now blocks Move and Delete in the GUI and CLI (was warning-only in v1.5.3).
- Three-layer design system in the WPF host: Primitives (raw colours), Tokens (semantic roles), Components (control styles).

### Changed

- Runtime moved from .NET 8 LTS to .NET 10 LTS. Slim build now needs the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).
- Codebase split into three projects: `InstallerClean.Core` (headless library), `InstallerClean` (WPF host), `InstallerClean.Cli` (console).
- Browser-opening URL clicks launch at the desktop shell's integrity level via `CreateProcessWithTokenW(rundll32 url.dll,FileProtocolHandler ...)`. Falls back to elevated `Process.Start` if the shell-token chain fails, with the failure logged.
- Path-leak hardening: every dialog and status pill shows exception type name plus crash-log path only; `ex.Message` never reaches the UI. The crash log gets the full detail.
- `CrashLog.TryWrite` returns whether the entry was actually persisted.
- `StorageHelpers.OpenAtomic` is the only sanctioned write entry for elevated writes (`CreateFile` with `FILE_FLAG_OPEN_REPARSE_POINT` plus post-open `GetFileInformationByHandle` reparse-point check). Replaces the previous check-then-write pattern.
- WPF MVVM reorganised: `MainViewModel` composes Scan / Cleanup / Completion / Chrome child VMs, each with its own observable state and commands.
- Dependency injection: services registered in `CoreComposition.cs` (shared with the CLI) and `Composition.cs` (WPF host adds Dialog / Confirmation / Window / MainViewModel). `validateScopes: true`.
- `System.IO.Abstractions.IFileSystem` injected into every file-touching service; security checks (`InstallerCacheHelpers`, `StorageHelpers`) deliberately bypass the mock.
- Localisation reorganised to a single neutral `Strings.resx` (en-GB) plus a hand-managed `Strings.Designer.cs` and a `{loc:Translate Key}` XAML markup extension.
- `MoveFilesService` re-checks `IsInstallerFolderOrChild` after `Directory.CreateDirectory` (closes a TOCTOU window) and refuses sources that are reparse points.
- `FileSystemScanService.ScanAsync` continuation runs off the UI thread (`ConfigureAwait(false)`).
- Settings save uses write-temp-then-rename via `OpenAtomic`.
- `App.xaml.cs` `DispatcherUnhandledException` handler has a re-entry guard.
- `OrphanedFilesViewModel` / `RegisteredFilesViewModel` lazy-load MSI summary metadata off the UI thread; cache survives selection cycles.
- `InstallerQueryService` `MsiEnumProductsExW` / `MsiEnumPatchesExW` use `ConstantElementCount = 39` (the previous `CountElementName` form added a phantom parameter that worked on x64 by ABI luck and would have crashed on x86).
- `MsiFileInfoService` MSIHANDLE marshalled as `uint` (matches `unsigned long MSIHANDLE` in `msi.h`; the previous `IntPtr` was 8 bytes on x64 instead of 4).
- `ShellFileOperations.SendToRecycleBin` rejects paths containing an embedded null (`SHFILEOPSTRUCT.pFrom` is a list-of-strings encoding; an embedded null would cause over-deletion).
- CLI `CancelKeyPress` handler registered before mutex acquisition.
- Browser-opening calls go through a defensive try/catch.
- `MainViewModel`, `CleanupViewModel`, `ChromeViewModel` implement `IDisposable`; container disposes at shutdown.
- CLI generic `UnauthorizedAccessException` catch echoes the resx message on the probe-failure path.
- `Status.FoundProducts` resx parameterises the noun via `PluraliseProduct` (was a literal `(s)`).
- `DeleteFilesService` reports per-file progress before the file-exists check, matching `MoveFilesService`.
- WPF-UI dependency removed; every control style defined in `Themes/Components.xaml`. Default styles for `ToolTip`, `ContextMenu`, `MenuItem`, `ProgressBar` and the focus visual ship in the same file.
- Caption buttons render in Segoe MDL2 Assets (the canonical Windows chrome font); previous Unicode codepoints relied on font fallback that left the maximise / restore swap visually identical.
- Main window: maximise removed. Title-bar double-click, Win+Up and the system menu's Maximize item all intercepted at `WM_SYSCOMMAND`. Detail windows keep default resize and maximise.

### Fixed

- XAML resource type-mismatch crash: default `ToolTip` template set `BorderThickness="{StaticResource Border.Hairline}"` against a `<sys:Double>` resource. WPF resource lookups don't run TypeConverters, so first paint with the default style threw `XamlParseException`. Added parallel `BorderThickness.*` Thickness tokens and used them at every `BorderThickness="{StaticResource ...}"` site.
- F5 (rescan) no longer fires while a Move or Delete operating overlay is up.
- Settings-file lost-update race: typing in the Move destination while a detail window was being resized could clobber the window-size save. `SaveAfterDelayAsync` now reloads before writing.
- Move pre-flight write-probe runs on a worker thread, honours the cancel button, and goes through the injected `IFileSystem`.
- `ResolveFinalPath` produces the right path shape when the existing-ancestor walk lands at a drive root (was producing drive-relative paths like `C:NewFolder\Sub`; cosmetic only, the security check still failed correctly).
- CLI `/m` no longer silently truncates extra positional arguments; trailing spaces in the destination are trimmed; mode-flag-bearing event log lines are parameterised.
- `MoveFilesService` per-file progress advances the counter on missing-source / reparse-point skips.
- `RegisteredFilesViewModel`: products with no `.msi` file (only patches) render a `(patches only)` synthetic main row.
- `ConfirmationService` guards against `Application.Current is null`.
- `App.xaml.cs` `BitmapImage` for window icons is frozen so the same instance is safely shared across windows.
- `PendingRebootService` reads keys via `RegistryView.Registry64`.
- About window's MIT licence Hyperlink shows the underline on hover (was colour-only; fails for users with reduced colour vision).
- Move destination textbox right-click menu uses the dark theme; explicit themed `ContextMenu` with the four standard editing commands.

### Removed

- `Strings.en-GB.resx` satellite (was a 1:1 duplicate of the neutral resx).
- `ISettingsService.Save` overload (void wrapper around `TrySave`); call sites use `_ = TrySave(...)`.

## [1.5.3] - 2026-04-18

### Changed

- About dialog redesign: version, licence and repository metadata in a compact block; Star on GitHub and Donate as labelled actions in the footer alongside Check for updates and Close.
- Inno Setup compression switched from `lzma2/ultra64` to `zip` after `setup.exe` was flagged by DeepInstinct on VirusTotal.
- Scan-complete timer displays milliseconds when under one second (was rounding to "0.0s").

### Fixed

- Keyboard-focus "stuck selected" appearance on About / Details navigation buttons after a modal dialog closed.
- Minor alignment issues in the About dialog.

### Removed

- `UpdateCheckService` (the HTTP-based update check). Check for updates now opens the GitHub releases page in the browser. The slim binary was being flagged by DeepInstinct on VirusTotal; auto-HTTP-on-startup from an elevated process was the leading suspicion at the time.

## [1.5.2] - 2026-04-17

### Added

- Cancellation across the long-running surfaces: startup scan (Cancel button or Esc from splash), rescan (Cancel button on scanning overlay or Esc), the background MSI-metadata read on Registered / Orphaned details cancels on window close.
- Main window is resizable; content stays centred and max-bounded.
- Move destination field is editable (type, paste, or Browse); value persists on focus loss.
- Confirmation dialogs (Move, Delete) can be dragged by their top edge.
- Column sort indicator on the Registered Files window; initial ProductName ascending state shown on open.
- Registered-but-missing-on-disk diagnostic: main window shows a count if the API reports a package whose `LocalPackage` path no longer exists.
- `Application` (PID_APPNAME) and `Keywords` (PID_KEYWORDS) fields in the Orphaned and Registered details panels.
- CLI Event Log entry: each `/s`, `/d` or `/m` run writes one summary entry to the Application event log under source "InstallerClean".
- `installerclean-cli.exe` shipped in the installer (~44 KB static console launcher, source in `cli-launcher/launcher.c`) so CLI usage waits properly when called from PowerShell or cmd.

### Changed

- Move refuses any destination inside `C:\Windows\Installer` at the service layer (not only at the UI).
- Destinations that resolve via junction or symlink into `C:\Windows\Installer` are detected and blocked.
- Delete uses `SHFileOperationW` directly rather than VB's `FileSystem.DeleteFile`, removing a thread-apartment risk where error dialogs could deadlock.
- Large-file Delete warning fires when any single file exceeds 500 MB (in addition to the existing 1 GB-total threshold).
- Completion-screen secondary button changed from "Close" to "Scan again" and triggers a real rescan.
- Large-size delete warning copy clarifies why Windows may bypass the Recycle Bin and points users at Move.
- Move-destination error messages are categorised (no permission, path too long, folder missing, drive error) instead of raw framework messages.
- UNC-share Move destination no longer crashes the free-space check; the check is skipped silently when the destination cannot be measured.
- Move destination is probed once before the per-file copy loop.
- Update-check HTTP response capped at 256 KB.
- Installer product enumeration bails after 20 consecutive API errors; hard 10,000-index cap keeps the loop finite.
- Zero products from the Windows Installer API (corrupt database) produces a targeted error pointing the user at `sfc /scannow` (was a silent "all clear").
- Crash log rotates at 512 KB; timestamps include offset (`zzz`).
- Settings save never throws; disk-full or locked-file shows a warning. Stranded `settings.json.tmp` is cleaned up automatically.
- About window's Check for updates is resilient to any unexpected exception.
- Build is deterministic (`<Deterministic>true</Deterministic>` + `<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>`); same source on the same SDK patch produces a byte-reproducible binary.
- Spacing tokens (`Gap.*`, `GapBottom.*`, `GapRight.*`, `GapLeft.*`).

### Fixed

- Scan failures now write to the crash log.
- Cancel during the last microsecond of an operation no longer throws `ObjectDisposedException`.
- Detail-panel values, version text and error messages remain selectable for right-click Copy.

## [1.5.1] - 2026-04-16

### Added

- Update check displays "Couldn't check for updates" when GitHub is unreachable (was falsely reporting up-to-date).
- Corrupt settings file is renamed to `settings.json.bad`; saved Move destination is recoverable.
- CLI `/d` and `/m` handle Ctrl+C cleanly; prints "Cancelling..." rather than terminating via the default handler.
- CLI `/m` validates the destination is not inside `C:\Windows\Installer` (or a subfolder) before any file move.
- Move destinations longer than 260 characters work without `\\?\` prefix workarounds.
- Unit tests for the update-check service and the installer-folder path validation helper.

### Changed

- All wildcard NuGet dependencies pinned (CommunityToolkit.Mvvm, NSubstitute); transitive dependency lockfile enabled.
- Inno Setup script tightened with explicit `AppId`, `MinVersion=10.0`, `ArchitecturesAllowed=x64compatible`.
- GitHub Actions in CI and CodeQL workflows pinned to commit SHAs.
- Completion screen: pressing Enter closes the window (Close button is `IsDefault`).

### Fixed

- Event-handler leaks on window close on repeated scans; subscriptions unhooked in `OnClosed`.
- Removed an orphaned image asset that was no longer referenced.

## [1.5.0] - 2026-04-04

### Added

- Manual Check for updates button in About; hits the GitHub Releases API on click only.
- Heart Donate icon on the main window (replaces the Ko-fi-shaped button).
- Hover animation on the star and heart icons.
- Dependabot for npm-style dependency PRs.
- CodeQL workflow for automated static analysis.

### Changed

- Donate link now points to `nofaff.netlify.app`.

## [1.4.1] - 2026-03-10

### Added

- 99 tests (was 56): coverage for `InstallerQueryService`, `MsiFileInfoService`, `PendingRebootService` and the model records.
- Project metadata: `Authors`, `Description`, `RepositoryUrl`, `Licence` populated in the assembly info.
- README troubleshooting section, clearer portable-vs-slim guidance, Recycle Bin safety reinforced.

### Changed

- WCAG AA contrast pass: dim text raised from 3.2:1 to 4.7:1; orphaned-files summary brightened.
- Design tokens: ~35 hardcoded colour values replaced with named resources (`Warning`, `Dim`, `Danger`, `Base200`, `Primary`).
- `CommunityToolkit.Mvvm` pinned to 8.4.0 (was `8.*`).

### Removed

- Icon working files removed from tracking (re-added to `.gitignore`).

## [1.4.0] - 2026-03-09

### Added

- GitHub Actions CI: build and test suite on every push and PR.
- 56 tests covering stress conditions, error handling and edge cases.
- `CONTRIBUTING.md` with build instructions, commit conventions and AV-friendly constraints.

### Changed

- Test mocking framework switched from Moq to NSubstitute (Moq's SponsorLink dependency was a concern for a freely-distributed project).

## [1.3.0] - 2026-03-08

### Added

- `installerclean-cli.exe /s`: scan-only CLI mode that lists removable files (filenames + sizes) without taking action. Exit code always 0.

### Changed

- Splash screen shows real scan progress instead of fixed steps.

## [1.2.0] - 2026-03-08

### Added

- Keyboard shortcuts: Alt+M Move, Alt+D Delete, Alt+B Browse, Alt+R Re-scan, Alt+A About, F5 scan, Esc cancel / dismiss / close.
- Focus management: overlays auto-focus their primary button.
- Focus indicators on caption buttons.
- Screen-reader support: accessible labels on icon buttons and progress bars.
- Selectable text: detail-panel values, version text, error messages support right-click Copy.
- Click-to-sort columns in the Registered Files window.
- Window-size persistence on detail windows.
- Inno Setup installer with Start Menu shortcut and Add/Remove Programs entry.

### Changed

- Self-contained exe shrinks from 162 MB to 76 MB.
- Detail-window lists handle large file counts more efficiently.
- Size column sorts numerically (was sorting as text).
- Re-scan shows "Scan complete" feedback even on fast scans.

## [1.1.0] - 2026-03-05

### Added

- Custom `WindowChrome` title bars across all windows; dark theme, app icon, per-window heading.
- Custom caption buttons (minimise, close) styled to match the dark theme; close has a red hover.

### Fixed

- Detail windows auto-select and focus the first item on open (keyboard navigation worked but had no visible target).

## [1.0.0] - 2026-03-04

Initial public release.

### Added

- `C:\Windows\Installer` scan: enumerates `.msi` and `.msp` files and correlates against the Windows Installer API to identify orphans.
- Superseded patch detection (catches the Adobe Acrobat patches PatchCleaner excludes by default).
- Move (to a folder of your choice) or Delete (to the Recycle Bin).
- CLI: `/d` (Delete), `/m` (Move to saved default), `/m PATH` (Move to specified path).
- Self-contained `InstallerClean.exe` (162 MB) and framework-dependent `InstallerClean-framework-dependent.exe` (8 MB, needs .NET 8 Desktop Runtime).
- No data collection.
