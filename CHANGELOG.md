# Changelog

All notable changes to InstallerClean. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

## [1.8.0] - Unreleased

Two opt-in buttons that, on a single click each, talk to the network. There is no timer, no startup hook, no other call site for either feature: every byte the app sends is the direct consequence of a button press.

### Added

- **Check for updates** in About now performs the version check itself rather than opening the releases page in the browser. On click, the app issues a single HTTPS GET to `api.github.com/repos/no-faff/InstallerClean/releases/latest`, identifies as `InstallerClean/<version>`, parses `tag_name`, and shows one of three localised dialogs: up to date, update available (with a button to open the release page), or check failed with a reason. 8-second timeout. Cancellation on the second click within the same session.
- **Send result log to No Faff** on the completion screen. After every Move, Delete or scan-with-no-orphans, the app writes a small JSON file (`%LOCALAPPDATA%\NoFaff\InstallerClean\last-run.json`) summarising the operation in counts and categorical labels only: schema version, app version, OS string, scan duration, registered/removable/orphaned/superseded/missing counts, pending-reboot label, operation kind, outcome, files processed, files failed, bytes cleared, error buckets and (for moves) destination kind. No file paths, no user names, no machine identifiers, no time-of-day. The button is one-shot per session and is suppressed when the all-clear comes from the Re-scan-after-completion path so the user isn't re-prompted for a result they have just declined to send. After a successful POST, the button is replaced by a "Thanks!" status line. The full schema can be inspected from About via "View last result log", which opens the JSON file in the user's default editor at medium IL.
- About surfaces a "View last result log" link next to the MIT licence link, visible only when a result log file exists on disk.

### Changed

- The completion-screen "Share what you cleared" button (browser-mediated cleared-bytes share) was replaced before release by Send result log. The button does the network call directly rather than handing the value off through a browser confirmation page; the JSON content is what gets sent and the file can be opened from About to inspect first.

## [1.7.0] - 2026-05-05

Pending-reboot detection rewritten to fix spurious "Windows is waiting to restart" banners on machines with no actual Windows update pending. Closes [#12](https://github.com/no-faff/InstallerClean/issues/12).

### Changed

- The pending-reboot gate now checks three narrow Windows Installer signals instead of the previous four broad pending-reboot signals:
  - The `Global\_MSIExecute` mutex is held (Windows Installer is currently writing to the cache).
  - The `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\InProgress` key exists (a previous Windows Installer transaction is suspended).
  - A queued post-reboot file rename targets a path under `%SystemRoot%\Installer` (a pending operation will touch the cache we are about to clean).

  Three of the four previously-checked signals (`WindowsUpdate\Auto Update\RebootRequired`, `Component Based Servicing\RebootPending`, `WindowsUpdate\Auto Update\PostRebootReporting`) had no documented relationship to Windows Installer cache safety; the fourth (`PendingFileRenameOperations`) was checked too coarsely and fired on any non-empty entry regardless of contents.
- Banner copy and CLI message are reason-specific, so the user can tell which signal is firing and what to do about it.

### Fixed

- Spurious "Windows is waiting to restart" banner on Windows 11 with no Windows update pending. The previous detection fired on any non-empty `PendingFileRenameOperations`, which includes routine non-Windows-Update entries (Microsoft Defender platform updates, Microsoft Edge updates, Steam game launcher files, anti-cheat installers, etc). Closes [#12](https://github.com/no-faff/InstallerClean/issues/12).

## [1.6.0] - 2026-05-05

The first release since v1.5.3. Most of the work is structural: the codebase is split into three projects, the WPF host no longer depends on a third-party theme library, the Windows Installer P/Invoke surface has been audited end to end, and the runtime has moved from .NET 8 LTS to .NET 10 LTS. The day-to-day flow (scan, review, move or recycle) is unchanged; the user-visible additions and fixes are listed below.

### Added

- All-clear and completion overlays. After a scan with no orphans, "All clear" with a Scan again button. After a successful Move or Delete, the bytes recovered, the destination, and a per-file error breakdown grouped by cause.
- CLI per-file progress (`[5/100] foo.msi` per line) on `/d` and `/m` so a sysadmin can tell a slow run isn't hung.
- CLI three-state exit codes: 0 = full success, 1 = total failure (bad args, scan failed, every file failed), 2 = partial (some files processed, some failed). 130 is reserved for `Ctrl+C` with no committed work; cancellation after partial work returns 2.
- CLI writes a single summary entry to the Application event log per run under source `InstallerClean`. The writer refuses to run if the source has been pre-mapped to a non-Application log.
- CLI arguments are case-insensitive (`/D`, `--HELP`, `/S`).
- Pending-reboot detection now disables Move and Delete in the GUI and blocks `/d` and `/m` in the CLI; in v1.5.3 it was warning-only.
- Three-layer design system in the WPF host: Primitives (raw colours), Tokens (semantic roles), Components (control styles).

### Changed

- URL-open buttons (Donate, Star, Check for updates, MIT licence) launch the browser at the desktop shell's integrity level via `CreateProcessWithTokenW(rundll32 url.dll,FileProtocolHandler ...)`. Previously the browser could open as Administrator on a freshly-booted machine with no browser running, with no cookies and no logged-in state. Falls back to elevated `Process.Start` if the shell-token chain fails (no interactive desktop, etc), with the failure logged.
- Path-leak hardening: framework exception messages can include paths from another user's profile under elevation. Every dialog and status pill now shows only the exception type name plus the crash-log path; `ex.Message` never reaches the UI. The crash log gets the full detail.
- `CrashLog.TryWrite` returns whether the entry was actually persisted, so dialogs don't claim "details written to X" when the write itself failed (symlinked log file, read-only profile).
- `StorageHelpers.OpenAtomic` is the only sanctioned write entry for elevated writes. `CreateFile` with `FILE_FLAG_OPEN_REPARSE_POINT` plus a post-open `GetFileInformationByHandle` reparse-point check, returning a handle only if the final component is a real file. Replaces the previous check-then-write pattern.
- WPF MVVM reorganised: `MainViewModel` now composes Scan / Cleanup / Completion / Chrome child VMs, each with its own observable state and commands.
- Dependency injection: services are registered in `CoreComposition.cs` (shared with the CLI) and `Composition.cs` (WPF host adds Dialog / Confirmation / Window / MainViewModel). `validateScopes: true`.
- `System.IO.Abstractions.IFileSystem` injected into every file-touching service so unit tests run against `MockFileSystem`. Security checks (`InstallerCacheHelpers`, `StorageHelpers`) deliberately use the real filesystem so a mock can't bypass them.
- Localisation reorganised: a single neutral `Strings.resx` (en-GB) plus a hand-managed `Strings.Designer.cs` and a XAML `{loc:Translate Key}` markup extension. No satellite resx.
- `MoveFilesService` re-checks `IsInstallerFolderOrChild` after `Directory.CreateDirectory` (closes a TOCTOU window) and refuses sources that are reparse points.
- `FileSystemScanService.ScanAsync` continuation runs off the UI thread (`ConfigureAwait(false)`).
- Settings save uses write-temp-then-rename for atomicity. The temp file is created via `OpenAtomic` so a symlink at the temp path can't redirect the write.
- `App.xaml.cs` `DispatcherUnhandledException` handler has a re-entry guard so a second exception fired during the dialog pump can't stack dialogs.
- `OrphanedFilesViewModel` and `RegisteredFilesViewModel` lazy-load MSI summary metadata off the UI thread; the cache survives selection cycles.
- Runtime moved from .NET 8 LTS to .NET 10 LTS. The slim build now requires the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0). Setup and portable builds bundle the runtime so nothing else is needed. End-of-life pushed from November 2026 (.NET 8) to November 2028 (.NET 10).
- `InstallerQueryService` `MsiEnumProductsExW` and `MsiEnumPatchesExW` stubs use `ConstantElementCount = 39` for the fixed GUID buffers (matches the native signatures exactly; the previous `CountElementName` form added a phantom parameter that worked on x64 by ABI luck and would have crashed on x86).
- `MsiFileInfoService` MSIHANDLE marshalled as `uint` (matches `unsigned long MSIHANDLE` in `msi.h`); the previous `IntPtr` form was 8 bytes on x64 instead of 4.
- `ShellFileOperations.SendToRecycleBin` rejects paths containing an embedded null (`SHFILEOPSTRUCT.pFrom` is a list-of-strings encoding; an embedded null would cause over-deletion).
- The CLI's CancelKeyPress handler is registered before mutex acquisition so a Ctrl+C in that window prints "Cancelling..." rather than terminating via the default handler.
- Browser-opening calls go through a defensive try/catch so a misconfigured URL handler can't crash the app over a Donate click.
- `MainViewModel`, `CleanupViewModel` and `ChromeViewModel` implement `IDisposable`. The singleton container disposes them at process shutdown so PropertyChanged subscriptions on `ScanViewModel` are unhooked rather than relying on the process-lifetime invariant.
- CLI generic catch for `UnauthorizedAccessException` now echoes the resx-sourced exception message, so a probe-failure path on a read-only destination shows the right reason instead of "administrator privileges required" (the message is safe under elevation; both throw sites use bounded, resx-sourced messages).
- `Status.FoundProducts` resx now parameterises the noun via `PluraliseProduct`, replacing the literal "(s)" plural.
- `DeleteFilesService` reports per-file progress before the file-exists check so a missing source still advances the counter, matching `MoveFilesService`.
- WPF-UI dependency removed. Every control style is now defined locally in `Themes/Components.xaml`. Default styles for `ToolTip`, `ContextMenu`, `MenuItem`, `ProgressBar` and the focus visual are written in the same file. Smaller dependency surface, no inherited theming surprises, no third-party update churn to track.
- Caption buttons render in Segoe MDL2 Assets, the canonical Windows chrome font. Previous Unicode codepoints were resolved by the WPF font fallback chain from a body font that does not include them, which left the maximise / restore swap visually identical and depended on whatever font Windows fell back to.
- Main window: maximise removed. The 720-wide centred-column layout does not fill a maximised viewport, so the chrome offers Minimise and Close only. Title-bar double-click, Win+Up and the system menu's Maximize item are all intercepted at WM_SYSCOMMAND so the misshapen state is not reachable. Detail windows (Orphaned files, Registered files) keep default Windows resize and maximise.

### Fixed

- XAML resource type-mismatch crash. The default `ToolTip` template set `BorderThickness="{StaticResource Border.Hairline}"` against a `<sys:Double>` resource. WPF resource lookups don't run TypeConverters, so first paint of a tooltip with the default style crashed with `XamlParseException`. Fixed by adding parallel `BorderThickness.*` Thickness tokens and using them at every `BorderThickness="{StaticResource ...}"` site.
- F5 (rescan) no longer fires while a Move or Delete operating overlay is up.
- Settings file lost-update race: typing in the Move destination while a detail window was being resized could clobber the window-size save. `SaveAfterDelayAsync` now reloads before writing.
- Pre-flight write-probe in Move runs on a worker thread, honours the cancel button, and goes through the injected `IFileSystem` rather than hitting real disk directly.
- `ResolveFinalPath` produces the right path shape when the existing-ancestor walk lands at a drive root (e.g. `C:\`); previously it produced drive-relative paths like `C:NewFolder\Sub` (cosmetic only; security check still failed correctly by accident).
- CLI `/m` no longer silently truncates extra positional arguments; trailing spaces in the destination path are trimmed; mode-flag-bearing event log lines are parameterised so a future call from a different mode can't print the wrong flag.
- `MoveFilesService` per-file progress reports advance the counter even on missing-source / reparse-point skips (no more 5 → 7 jumps).
- `RegisteredFilesViewModel`: products with no `.msi` file (only patches) render a `(patches only)` synthetic main row; the first patch no longer appears once as the product line and once in the patch sub-list.
- `ConfirmationService` guards against `Application.Current is null` like `WindowService` does.
- `App.xaml.cs` BitmapImage for window icons is frozen so the same instance is safely shared across windows.
- `PendingRebootService` reads keys via `RegistryView.Registry64` for parity with `InstallerQueryService`.
- About window's MIT licence Hyperlink shows the underline on hover (was colour-only, fails for users with reduced colour vision).
- Move destination textbox right-click menu uses the dark theme. Default WPF builds the textbox context menu outside the implicit-style scope of `Application.Resources`, which rendered the system light-themed shell menu in an otherwise dark UI; the textbox style now sets an explicit themed `ContextMenu` with the four standard editing commands.

### Removed

- `Strings.en-GB.resx` satellite (was a 1:1 duplicate of the neutral resx; .NET resource fallback returns the neutral resx for any culture without a satellite, including en-GB).
- `ISettingsService.Save` overload (was a void wrapper around `TrySave`); call sites use `_ = TrySave(...)` so the discard is explicit.

## [1.5.3] - 2026-04-18

About dialog redesign and small UX polish.

### Changed

- About dialog: version, licence and repository metadata now share a compact block, with Star on GitHub and Donate as labelled actions in the footer alongside Check for updates and Close.
- Inno Setup compression switched from `lzma2/ultra64` to `zip` (cleared a DeepInstinct heuristic flag on `setup.exe`'s hash).
- Scan-complete timer displays milliseconds when the scan takes under one second, instead of rounding to "0.0s".

### Fixed

- Keyboard-focus "stuck selected" appearance on About / Details navigation buttons after a modal dialog closed (the focused-button foreground colour persisted after the modal dismissed).
- Minor alignment issues in the About dialog.

### Removed

- `UpdateCheckService` (the HTTP-based update check). "Check for updates" in About now opens the GitHub releases page in your browser. Auto HTTP from an elevated app on startup was triggering DeepInstinct's C2 heuristic.

## [1.5.2] - 2026-04-17

Substantial release: every code path that touches the Move destination, the Recycle Bin, the splash, or the running scan was reviewed and either tightened or made cancellable. The CLI gains a real console launcher, the main window becomes resizable, and the Move destination becomes editable.

### Added

- Cancellation across the long-running surfaces: the initial startup scan can be cancelled from the splash screen (Cancel button or Esc); rescan can be cancelled (Cancel button on the scanning overlay or Esc); the background MSI-metadata read triggered by selecting a file in Registered or Orphaned details is cancelled when the window closes.
- Main window is resizable. Content stays centred and max-bounded so a large monitor doesn't stretch the layout.
- Move destination field is editable. Type, paste or use Browse; the value persists on focus loss.
- Confirmation dialogs (Move, Delete) can be dragged by their top edge.
- Column sort indicator (arrow showing direction) on the Registered Files window, with the initial ProductName ascending state shown on open.
- Registered-but-missing-on-disk diagnostic: if the Windows Installer API reports a package at a path that no longer exists, the main window shows a warning with the count.
- Two extra fields surfaced in the Orphaned and Registered details panels: `Application` (PID_APPNAME) and `Keywords` (PID_KEYWORDS), alongside the existing Author, Title, Subject, Digital signature and Comment.
- CLI Event Log entry: each `/s`, `/d` or `/m` run writes one summary entry to the Application event log under source "InstallerClean" (Task Scheduler / unattended deployments where stdout is not captured).
- `installerclean-cli.exe` shipped in the installer: a small console launcher (~44 KB, static, no runtime dependency, source in `cli-launcher/launcher.c`) so CLI usage waits properly when called from PowerShell or cmd.

### Changed

- Move refuses any destination inside `C:\Windows\Installer` at the service layer, not only at the UI. A future entry point that forgot to validate cannot route Move back into the cache.
- Destinations that resolve via junction or symlink into `C:\Windows\Installer` are detected and blocked.
- Delete uses `SHFileOperationW` directly rather than VB's `FileSystem.DeleteFile`, removing a thread-apartment risk where error dialogs could deadlock.
- The large-file warning on Delete also fires when any single file exceeds 500 MB, not only when the total exceeds 1 GB. Windows can silently bypass the Recycle Bin for individual large files too.
- Completion screen secondary button is now "Scan again" and triggers a real rescan (was "Close" with no rescan).
- Large-size delete warning copy is clearer about why Windows may bypass the Recycle Bin and points users at Move for guaranteed backups.
- Error messages on unusable Move destinations are categorised (no permission, path too long, folder missing, drive error) rather than showing a raw framework message.
- Move destination on a UNC share (`\\server\share`) no longer crashes the free-space check; the check is skipped silently when the destination cannot be measured.
- Move destination is probed once before the per-file copy loop. A read-only or unwritable destination produces one clean error rather than per-file failures for every file in the batch.
- Update-check HTTP response capped at 256 KB so a misbehaving or compromised endpoint cannot flood memory.
- Installer product enumeration bails after 20 consecutive API errors instead of spinning. A hard 10,000-index cap keeps the loop finite.
- Zero products from the Windows Installer API (corrupt installer database) produces a targeted error pointing the user at `sfc /scannow`, rather than a silent "all clear" scan.
- Crash log rotates at 512 KB and timestamps include offset (`zzz`) so cross-timezone sharing is unambiguous.
- Settings save never throws: a disk-full or locked-file situation shows a friendly warning instead of crashing, and a stranded `settings.json.tmp` is cleaned up automatically.
- About window's Check for updates is resilient to any unexpected exception from the update service.
- Build is now deterministic (`<Deterministic>true</Deterministic>` and `<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>`) so the same source on the same SDK patch produces a byte-reproducible binary.
- Spacing tokens introduced (`Gap.*`, `GapBottom.*`, `GapRight.*`, `GapLeft.*`) so margins reference semantic names rather than numeric literals.

### Fixed

- Scan failures now write to the crash log so details are available for diagnosis.
- Cancel during the last microsecond of an operation no longer throws `ObjectDisposedException`.
- Detail panel values, version text and error messages remain selectable for right-click Copy.

## [1.5.1] - 2026-04-16

Hardening pass. Update-check error handling, settings-file recoverability, CLI Ctrl+C, long path support, and a sweep of supply-chain pins.

### Added

- Update check now displays "Couldn't check for updates" when GitHub is unreachable, instead of falsely reporting up-to-date.
- Corrupt settings file is renamed to `settings.json.bad` instead of being silently discarded; the user's saved Move destination is recoverable.
- CLI `/d` and `/m` accept Ctrl+C cleanly and print "Cancelling..." rather than terminating via the default handler.
- CLI `/m` validates that the destination is not inside `C:\Windows\Installer` (or a subfolder) before any file move starts.
- Move destinations longer than 260 characters work without `\\?\` prefix workarounds.
- Unit tests covering the update check service and the installer-folder path validation helper.

### Changed

- All wildcard NuGet dependencies (CommunityToolkit.Mvvm, NSubstitute) pinned to specific versions; transitive dependency lockfile enabled so future builds cannot pick up an unexpected upstream version.
- Inno Setup script tightened with explicit `AppId`, `MinVersion=10.0` and `ArchitecturesAllowed=x64compatible`.
- All GitHub Actions in CI and CodeQL workflows pinned to commit SHAs instead of floating tags.
- Completion screen: pressing Enter closes the window (Close button is now `IsDefault`).

### Fixed

- Event-handler leaks on window close on repeated scans. Subscriptions are unhooked in `OnClosed`.
- Removed an orphaned image asset that was no longer referenced.

## [1.5.0] - 2026-04-04

About-window enhancements and a security-tooling pass.

### Added

- Manual "Check for updates" button in the About window. Hits the GitHub Releases API on click only; no auto-check on startup.
- Heart Donate icon on the main window (replaces the Ko-fi-shaped button).
- Hover animation on the star and heart icons.
- Dependabot for npm-style dependency PRs.
- CodeQL workflow for automated static analysis.

### Changed

- Donate link now points to `nofaff.netlify.app`.

## [1.4.1] - 2026-03-10

Accessibility, contrast and dependency-hygiene pass.

### Added

- 99 tests (was 56). New coverage for `InstallerQueryService`, `MsiFileInfoService`, `PendingRebootService` and the model records.
- Project metadata: `Authors`, `Description`, `RepositoryUrl` and `Licence` populated in the assembly info.
- README troubleshooting section, clearer portable-vs-slim guidance, Recycle Bin safety reinforced in Getting Started.

### Changed

- WCAG AA contrast pass: dim text (metadata labels, placeholders) raised from 3.2:1 to 4.7:1. Orphaned-files summary brightened so it stands out as the primary call to action.
- Design tokens: ~35 hardcoded colour values replaced with named resources (`Warning`, `Dim`, `Danger`, `Base200`, `Primary`).
- `CommunityToolkit.Mvvm` pinned to 8.4.0 (was `8.*`).

### Removed

- Icon working files removed from tracking (re-added to `.gitignore`).

## [1.4.0] - 2026-03-09

Continuous-integration foundation and a test-suite refresh.

### Added

- GitHub Actions CI: build and run the test suite on every push and PR.
- 56 tests covering stress conditions, error handling and edge cases.
- `CONTRIBUTING.md` with build instructions, commit conventions and AV-friendly constraints.

### Changed

- Test mocking framework switched from Moq to NSubstitute (Moq's SponsorLink dependency was a concern for a freely-distributed project).

## [1.3.0] - 2026-03-08

CLI scan-only mode and progress polish.

### Added

- `installerclean-cli.exe /s`: scan-only CLI mode that lists removable files (filenames + sizes) without taking action. Exit code is always 0. Useful for auditing and scripting.

### Changed

- Splash screen shows real scan progress instead of fixed steps.

## [1.2.0] - 2026-03-08

Keyboard support, accessibility and the first installer.

### Added

- Keyboard shortcuts: Alt+M Move, Alt+D Delete, Alt+B Browse, Alt+R Re-scan, Alt+A About, F5 to scan, Esc to cancel operations / dismiss overlays / close the window.
- Focus management: overlays auto-focus their primary button so keyboard users can act immediately.
- Focus indicators on caption buttons (visual feedback when keyboard-focused).
- Screen reader support: icon buttons and progress bars have accessible labels.
- Selectable text: detail panel values, version text and error messages support right-click Copy.
- Click-to-sort columns in the Registered Files window (header click toggles ascending/descending).
- Window size persistence: detail windows remember their size across sessions.
- Inno Setup installer with Start Menu shortcut and an Add/Remove Programs entry.

### Changed

- Self-contained exe shrinks from 162 MB to 76 MB.
- Detail-window lists handle large file counts more efficiently.
- Size column sorts numerically (was sorting as text).
- Re-scan shows a "Scan complete" feedback message even on fast scans.

## [1.1.0] - 2026-03-05

Custom dark-theme chrome and a focus fix.

### Added

- Custom `WindowChrome` title bars across all windows: dark theme, app icon and heading per title bar.
- Custom caption buttons (minimise and close) styled to match the dark theme; close button has a red hover.

### Fixed

- Detail windows now auto-select and focus the first item on open (keyboard navigation worked but had no visible target).

## [1.0.0] - 2026-03-04

Initial public release.

### Added

- `C:\Windows\Installer` scan: enumerates `.msi` and `.msp` files and correlates against the Windows Installer API to identify orphans.
- Superseded patch detection: catches the Adobe Acrobat patches PatchCleaner excludes by default.
- Move (to a folder of your choice) or Delete (to the Recycle Bin) the orphaned files.
- CLI mode: `/d` (Delete), `/m` (Move to saved default), `/m PATH` (Move to specified path).
- No installer required at first launch: download `InstallerClean.exe` (162 MB self-contained) or `InstallerClean-framework-dependent.exe` (8 MB, requires .NET 8 Desktop Runtime).
- No data collection.
