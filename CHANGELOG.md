# Changelog

All notable changes to InstallerClean. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

## [1.5.4] - Unreleased

A 10-step architectural rebuild plus two ship-readiness review passes (60 + 43 findings, both actioned bar the documented deferrals). Most of v1.5.4 is plumbing rather than user-visible features; the headline behaviour matches v1.5.3.

### Added

- All-clear / completion overlay after a scan with no orphans, and a summary overlay after Move and Delete with bytes recovered, destination and per-file errors grouped by reason.
- CLI per-file progress (`[5/100] foo.msi` per file) so a sysadmin watching a long run can tell it's alive.
- CLI three-state exit codes (0 = full success, 1 = full failure, 2 = partial success with errors). 130 = Ctrl+C with no committed work; partial cancellation returns 2 and writes an Event Log summary.
- CLI writes a single summary entry to the Application event log per run (mode, counts, destination if any, error count). Source name `InstallerClean`; refuses to write if the source is mapped to a non-Application log.
- CLI args are case-insensitive (`/D`, `--HELP`, `/S` all work).
- Maximize / Restore button glyph swaps when the window is maximised, with matching tooltip and AutomationProperties.Name.
- Pending-reboot detection now disables Move and Delete in the GUI and blocks `/d` and `/m` in the CLI (in v1.5.3 it was warning-only).
- Settings file `.bad` rename: a corrupt settings.json is renamed for manual recovery before the loader returns defaults.
- Three-tier design system in the WPF host: Primitives (raw colours) → Tokens (semantic roles) → Components (control styles).

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
- `InstallerQueryService` `MsiEnumProductsExW` and `MsiEnumPatchesExW` stubs use `ConstantElementCount = 39` for the fixed GUID buffers (matches the native signatures exactly; the previous `CountElementName` form added a phantom parameter that worked on x64 by ABI luck and would have crashed on x86).
- `MsiFileInfoService` MSIHANDLE marshalled as `uint` (matches `unsigned long MSIHANDLE` in `msi.h`); the previous `IntPtr` form was 8 bytes on x64 instead of 4.
- `ShellFileOperations.SendToRecycleBin` rejects paths containing an embedded null (`SHFILEOPSTRUCT.pFrom` is a list-of-strings encoding; an embedded null would cause over-deletion).
- The CLI's CancelKeyPress handler is registered before mutex acquisition so a Ctrl+C in that window prints "Cancelling..." rather than terminating via the default handler.
- Browser-opening calls go through a defensive try/catch so a misconfigured URL handler can't crash the app over a Donate click.

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

### Removed

- `Strings.en-GB.resx` satellite (was a 1:1 duplicate of the neutral resx; .NET resource fallback returns the neutral resx for any culture without a satellite, including en-GB).
- `ISettingsService.Save` overload (was a void wrapper around `TrySave`); call sites use `_ = TrySave(...)` so the discard is explicit.

## [1.5.3] - 2026-04-18

About dialog redesign. `UpdateCheckService` removed; "Check for updates" now opens the GitHub releases page in your browser. Inno Setup compression switched from `lzma2/ultra64` to `zip` (cleared a DeepInstinct flag on `setup.exe`).

## [1.5.2]

Splash and rescan cancel. Resizable main window. Editable Move destination. Sort indicators in the registered list. UNC and symlink destination safety. `installerclean-cli.exe`. `IDialogService` abstraction. Deterministic builds. Spacing tokens. Accessibility pass.

## [1.5.1]

Update-check tri-state. `settings.json.bad` backup. Broader pending-reboot detection. `CrashLog`. CLI Ctrl+C handling. Lockfile pinning. Hardened manifest and Inno Setup script.

## [1.5.0]

Manual update check in About. Heart Donate icon. Hover animations. Dependabot and CodeQL.

## [1.4.1]

Colour tokens. WCAG AA contrast pass. 99 tests. Pinned dependency versions. README troubleshooting section.

## [1.4.0]

CI on every commit. NSubstitute test mocks. CONTRIBUTING.md. 56 tests.

## [1.3.0]

`/s` scan-only CLI mode.

## [1.2.0]

Inno Setup installer.

## [1.1.0]

WindowChrome custom title bars.

## [1.0.0]

Initial public release.
