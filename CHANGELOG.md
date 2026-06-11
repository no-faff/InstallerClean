# Changelog

Every change to InstallerClean, logged in full (not just the user-facing highlights). Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

## [1.9.0] - Unreleased

### Added

- InstallerClean honours the Windows reduced-motion setting (Settings > Accessibility > Visual effects > Animation effects), which WPF ignores by default: with animations off, the heart and star hover growth, the scanning sweep and the splash easing all go static. It reacts to the setting live, no restart.
- InstallerClean follows the Windows text-size slider (Settings > Accessibility > Text size), which WPF also ignores on its own: the app's text scales while the title bar, icons and control heights keep their shape, live as the slider moves.
- Sorting the detail windows' columns is keyboard-operable: the headers are focusable tab stops with a visible cue, Space or Enter sorts and flips direction, and the sort state reaches screen readers as "Sorted by Size, descending" instead of an arrow glyph spelled out letter by letter.
- When the Recycle Bin turns out to be unavailable for the drive, Delete offers a choice instead of just stopping: Move (the recommended route, straight into the normal flow), delete permanently or cancel. Nothing is removed without an explicit choice, the dialog states only what is known and never guesses why the bin is off, and the completion screen after a consented permanent delete says plainly that the files did not go to the bin.
- `installerclean-cli.exe` is offered as a download in its own right: one self-contained exe, no install, no .NET runtime needed, the same binary the setup installs. Drop it on a client, scan or clean, delete it. Requested in discussion #23.
- `installerclean-cli --version` (or `-v`) prints the name and Major.Minor.Patch then exits 0, deliberately without the deterministic build's `+<commit>` suffix so a parsed version stays clean.
- A README section, "If a needed file goes missing", explains in Microsoft's own words what the cache is, how a registered file can go missing by means other than InstallerClean and the recovery route: reinstall the program over itself, same version where possible, which usually restores it. Mirrored into French, Spanish and Simplified Chinese; the "Will Windows complain?" FAQ shrinks to a plain no with a pointer at the section.
- The READMEs gain an Accessibility section stating only what is implemented: full keyboard operation including column sorting, Narrator and Voice Access support, announced outcomes, an always-visible focus ring and WCAG AA contrast.
- The all-clean screen now says that about two in three people find nothing unneeded, so a clean result reads as the normal outcome rather than a wasted run. Shown only on a from-scratch all-clean, never after a Move or Delete, so it cannot tell someone who just cleared files that they found nothing.

### Changed

- Delete now uses the `IFileOperation` shell interface instead of `SHFileOperationW`, which could silently permanently delete a file it could not recycle while reporting success. Delete now checks the bin works on the drive before running and reports a file as recycled only when it genuinely reached the bin; when recycling is not possible it stops, the GUI offering the choice above and the CLI refusing with exit 75. Per-file failures now carry the real Windows error code in hex.
- Opt-in reports record the Windows error code behind a failed delete (a per-code count map on each error category), so a real-machine failure is diagnosable. The schema moves to version 3; still counts and categorical labels only.
- A per-file recycle failure now explains its likely cause from the error code: access denied (`0x80070005`) names a permissions lock and suggests Move, a sharing violation (`0x80070020`/`0x80070021`) names a file held open by another program, everything else takes a clearer generic line.
- The main window's results read more clearly: a rule sets the intro apart from the counts, the missing-from-disk note sits as a footnote under the count it belongs to, and "N files still used" becomes "N files still needed", the honest word for a file Windows still expects whether or not it is present.
- A registered file missing from disk is flagged in the Registered Files window: a warning triangle, "missing" in place of the size and, on selection, a note explaining that Windows still expects it and how to try to put it back. It used to look like any other row, "0 B" and an empty pane.
- The missing-file copy is rewritten to be accurate rather than reassuring: recovery means reinstalling the program (matching version safest), it usually works, and nothing InstallerClean removes can cause the situation, since it never removes a file a program still needs. The old copy had promised recovery as a certainty.
- The main window opens on the reassurance: the unneeded files below are safe to delete, the three reasons they exist, and the basis (only files Windows reports as finished with). The count gains the word "unneeded".
- The Registered Files missing-file note links into the README's recovery section, routed through the unelevated launcher like every other in-app link.
- The About cuppa button gains the hover line "It's thirsty work!" in the accent tooltip, and the main window's icon-only cuppa now reads "If it helped, buy me a cup of tea.", its screen-reader name matched so Voice Access agrees with the tooltip.
- The Move tooltips name the box they point at: "Move the unneeded files to the Move location.", the disabled state adding "Choose one first.", so the tooltip and the MOVE LOCATION label visibly mean the same thing. The path box's own tooltip drops "paste one".
- The star tooltips invite more than a star: the main one now reads "Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome.", and the About star button gains an accent tooltip reading on from its visible label.
- The Send-summary tooltips now say what the button does and what happens next: "Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm.", the all-clean variant dropping the space-freed clause.
- The main window's star and heart tooltips sit flush above their buttons, right edge pinned to the button's, so they stay inside the window instead of spilling onto the desktop from the bottom-right corner; they drop below only when there is no room above.
- The send-confirmation window shows the whole report with no scrollbar; the preview box had been capped a line or two shy of the 25 lines every successful report has. The box now sizes to the report, clamped to the screen, so it scrolls only when the report genuinely cannot fit.
- The send-confirmation caption leads with the privacy fact instead of the URL: "Nothing identifies you or your machine; it just lets me know the app is working and how much space people are freeing. It goes to nofaff.netlify.app/api/result-log."
- Both donate buttons open nofaff.netlify.app/support, where Ko-fi and Bitcoin actually live, instead of the bare homepage.
- Every CLI run now leaves an Application-log entry: a pre-work Ctrl+C and a bad flag, which used to exit with no trace, write Warning entries, and a bare argless run is a usage error (exit 1) instead of a silent success, so a misconfigured scheduled task fails visibly. An explicit `--help`, `/?` or `-h` still exits 0.
- Each CLI log entry carries a stable numeric Event ID for its outcome class (1000 completed, 1002 with per-file errors, 2000 transient skip, 4000 hard error), so a non-English consumer can classify by number; the mutex-contention skip moves from Information to Warning, so a "Warning and above" filter catches every run that did not do its job.
- The CLI reports per-file progress synchronously so stdout stays in order; the lines had gone through the thread pool and could print out of order with the closing summary, which matters to an RMM scraping stdout.
- The CLI's single-stream output contract is documented in the READMEs: everything goes to stdout, no separate stderr; the exit code is the machine signal and the event-log entry mirrors it.
- A CLI `/m` run validates its destination before scanning rather than after, so a misconfigured destination fails fast instead of paying for the whole walk first; a bare `/m` with no saved destination now fails on every run, the correct reading of a misconfigured task.
- The CLI `--help` legend for exit 75 is cause-agnostic, "a temporary condition blocked the run", pointing at the printed message for the remedy; the old "try later" advice is wrong for the bin-unavailable cause, which a plain retry never clears.
- The installer requires Windows 10 build 14393 (version 1607), the oldest the .NET 10 Desktop Runtime supports, so too-old machines get a clear setup message instead of a first-launch failure; Inno reads the true build via `RtlGetVersion`.
- The setup installs the MIT licence beside the executables as `LICENSE.txt` (so a double-click opens Notepad), letting a mirror that copies the install directory carry the redistribution terms `pad.xml` requires.
- The removable files are called "unneeded" across the GUI and the CLI scan lines, the honest partner of the registered list's "still needed"; 1.8.2 said "unused", but the axis is needed or not, not used or not.
- The two Details windows are retitled "Unneeded files that are safe to delete" and "Registered files that should not be deleted", in place of "Unused files" and "Registered files", so a screen reader announces what each is for.
- The Registered files window opens as tall as the screen allows (up to 860, clamped to the monitor's work area), since arrowing down the products is how the details get read and a typical product's details did not fit the old fixed 680. The products list keeps its old height, the extra all goes to the details band, and a size set by resizing still wins.
- Two delete-path messages are tightened: a partly-failed consented permanent delete keeps the "did not go to the Recycle Bin" clause and gains a singular form, and the one-file recycle-unavailable dialog reads "So this file ..." rather than "So this 1 file ...".
- The About window's "What it does, and why it's safe" link is removed; it opened the same repository as the Star and licence links one hop away. The pane's recovery link stays.
- The README is published in seven languages: Japanese, Brazilian Portuguese and Russian join English, Simplified Chinese, Spanish and French, the existing three resynced to native quality. Languages chosen by which developer audiences value a native README, not by speaker counts.
- The READMEs are reconciled with the 1.9.0 delete and CLI behaviour: the unconditional "goes to the Recycle Bin, restore from there" promise is softened to the conditional truth, single-sourced in "Is it safe?" so the locales cannot drift, and the CLI exit codes are corrected.
- The delete copy no longer frames Move as the safe option. The safety is in what gets listed (only files Windows reports as finished with), so Delete is the normal action; Move is for anyone who'd rather see for themselves first.
- The PatchCleaner comparison table gains a delete-safety row: InstallerClean never silently permanently deletes, PatchCleaner's delete is permanent with no bin step. Mirrored across the locales.
- The README FAQ gains a table of what recent runs have found, from the opt-in summaries, so a visitor can see the range of space freed on real machines.
- The reports table's figures follow each translation's own conventions (comma decimals, the French, Spanish and Russian space before %, "Go" and "ГБ" units), produced from each file's locale rather than hand-formatted.
- The README opening leads with the honest split: about a third of machines have something to clean (median around 21 GB, some hundreds), two-thirds find nothing, and the scan takes seconds either way. Replaces the per-machine anecdotes.
- The Delete confirmation no longer warns that a large delete may skip the Recycle Bin. The warning (shown since 1.5.2) fired on ordinary deletes: Windows only bypasses the bin for a single file larger than the per-drive quota, not for a large total. The genuine bin-unavailable case keeps its choice dialog.
- The About star and cuppa tooltips appear instantly, matching the main window's.
- The installer-busy banner is reworded from jargon into plain terms: something, usually a Windows Update or a background install, is using Windows Installer, Move and Delete are paused while it runs, Re-scan once it is done. The CLI's matching gate message follows suit, keeping its retry advice, which is right for this cause.
- The Delete confirmation drops the warning triangle by its heading; a delete to the Recycle Bin is the normal action the app is for, not a hazard.

<!-- RELEASE-OPERATOR: if the compressed ~65 MB portable clears VirusTotal at the Stage 2 VT pause, add this Changed bullet; if it flags and the portable stays uncompressed, drop it:
- The portable build returns to the compressed single-file shape, roughly halving the download (about 135 MB to about 65 MB). It had shipped uncompressed since v1.8.2 to clear a Defender machine-learning false positive (`Trojan:Win32/Wacatac.B!ml`) on the compressed runtime bytes; Microsoft retrains on cleared false positives, so the compressed shape is clean again.
-->

### Fixed

- The recycle probe cleans up after itself: the tiny `ic-recycle-probe-*.tmp` it recycles to test the bin used to stay in the bin on success. The shell hands back the exact bin entry a delete creates, so the probe now permanently deletes that one item, addressed by identity, which can only ever remove the probe's own entry; real deletions stay restorable.
- The unneeded-files window's Size column is actually visible. It shipped in 1.8.2, with the list opening sorted by it, biggest first, but the column widths overran the pane with horizontal scrolling disabled, so it sat wholly past the edge; the File column (short hex names) gives up the spare width.
- Three accessibility gaps on the overlays and dialogs: the confirmation and recycle-unavailable dialogs open with focus on Cancel (a visible ring at once, and a reflexive Space lands on the non-committing choice), the recycle-unavailable dialog gains a window-level description of its choice, and the completion outcome is announced to screen readers, which used to hear only "Done". The decorative splash hero and completion icon leave the screen-reader tree.
- The update-available dialog focuses Cancel on open and describes its choice to screen readers, matching the other modals.
- Keyboard focus no longer falls to nowhere after dismissing the completion overlay (it returns to Re-scan) or on a scan that found files (it lands in the move-destination box, the start of the Move flow, never Delete).
- Accessible names match visible labels again (WCAG 2.5.3), so Voice Access "click Delete" or "click Done" works, including on permanent delete; the longer descriptions move to help text, which screen readers still read.
- The MIT licence link's help text lowercases "licence", which Narrator spelled out letter by letter in caps.
- The GUI no longer reports freed space when nothing was deleted; the bin-unavailable case used to show a green "freed N MB" over "0 files deleted" and log the false figure.
- The pending-reboot warning wraps instead of running off the window edge, and the main window sizes itself to its content (now non-resizable), so a tall banner grows the window rather than clipping the buttons.
- Scan-cancel status updates the instant Esc is pressed; the old write order was harmless on the UI thread but flaked a CI unit test. No visible change on the running app.
- The scrollbar groove pages again; the custom template had no track page-buttons, so clicking above or below the thumb did nothing. Every scrollbar in the app shares the template, so all are covered.
- The Browse caption lines up with the Details and Move edges again; it used the ghost button's default 20px side padding where its neighbours use 12px.
- A Move or Delete cancelled before any file was touched leaves the status line blank; only a cancel that interrupted real work reports it ("Move cancelled. 23 of 100 files processed."), and Move and Delete now behave the same (Move used to park a standalone "Move cancelled."). The line is pinned left instead of floating centred.
- A move or delete that crashes outright surfaces a dialog naming the exception type and the crash-log path, matching the other failures; the old inline status line trimmed the path off at its width cap.
- Concurrent settings saves can no longer lose each other's changes: all writers go through one locked load-mutate-save, and the one-shot sent-summary flag writes synchronously. The debounced destination save runs on a pool thread and could interleave with the UI-thread saves, last write winning wholesale.
- The pending-reboot check no longer reports "clean" under memory pressure; its probes had swallowed `OutOfMemoryException`, so a held Windows Installer mutex could read as safe to clean. Such a failure now surfaces as a scan error.
- The recycle engine publishes its work queue safely on weakly-ordered targets (Windows on ARM64) and refuses work after disposal with the `ObjectDisposedException` the delete service already degrades to a per-file error.
- A successful clean-up no longer reports a hard error because the best-effort empty-folder prune afterwards hit a failure in the directory walk itself; any non-cancellation failure in the prune is swallowed, one shared helper covering the GUI and both CLI paths.
- The update-available dialog's title wraps instead of clipping; longer localised titles would have been cut off.
- In-app links open reliably again. The unelevated launcher's `CreateProcessWithTokenW` route needs `SeImpersonatePrivilege`, which some elevated tokens lack, so every link fell back to copying the address to the clipboard. It now drives the running Explorer through the shell-view chain (`IShellWindows` through to `IShellDispatch2.ShellExecute`), opening the user's own browser with no privilege required; the guarantee is unchanged, unelevated or the clipboard fallback, never an elevated browser, absolute http/https only.
- The Delete and recycle-unavailable headings can no longer clip: a horizontal StackPanel gave the text unbounded width so wrapping never engaged; both now use the DockPanel the banners already use, and the card grows to fit.
- The move-destination box no longer pops its tooltip unprompted on window open or alt-tab back (WPF shows tooltips on keyboard focus, and that box is the one the results screen auto-focuses); it shows on hover only, the focus ring unchanged.

### Changed (internal)

- The Delete confirmation chain drops the dead `totalBytes` and `maxSingleFileBytes` parameters left behind by the removed size warning; the signatures carry what the dialog reads, the test call-sites following.
- The P/Invoke the shell-view launcher rework orphaned is removed (`GetShellWindow`, `OpenProcess`, `CloseHandle`, `PROCESS_QUERY_INFORMATION`); a smaller declared import surface is also one less thing for an AV heuristic to weigh.
- The `WarningTooltip` style is renamed `AccentTooltip`; it is the indigo accent on the thanks buttons, never a warning, and the name now matches the `AccentPill` family.
- The `MainViewModelTests` count assertions move to the "unneeded" wording; they had pinned the old "{N} file(s) to clean up" string.
- Dead design tokens, primitives and the unused `LinkButton` style are removed, per the theme's rule that a token earns its place only with a real consumer; comments citing the removed examples are corrected in the same pass.
- A read-only `list-unused-resources.sh` reports resx keys and XAML resources nothing consumes, matching both accessor forms; it only reports, never deletes.
- A `GapTop.Sm` token replaces three literal 8px top margins, the warning-triangle gap moves to the existing `GapRight.Icon` token, and the deliberately off-scale margins gain explaining comments.
- An unused `System.Net` import is dropped from the update-check service.
- The CLI's argument mapping and finished-run classification move into a `CliContract` type in Core, so the exit-code and EventLog contract RMM tooling pins to carries unit-test coverage; `Main` stays a thin Console and Environment shell.
- The delete test suite is rebuilt for the `IFileOperation` engine, unit and integration; the old `SHFileOperation` tests are removed.
- The pending-reboot probe and the settings persists move off the UI thread; the settings tests assert the new locked `Update` call.
- Two strings are made translation-safe: the missing-file note's linked sentence becomes one [ ]-delimited string so a translator can move the link to suit the word order, and the resx header index gains the `RecycleUnavailable.*` entry it was missing.

## [1.8.2] - 2026-05-27

An audit-driven release: a large sweep of correctness fixes (thread affinity, exception handling, path-leak defence on the CLI), an accessibility pass on the orphans list and completion overlay, a result-log schema bump that separates obsoleted from superseded patches, and AV-false-positive work on the portable and setup builds. No single headline feature; the value is the breadth and the receipts behind each fix.

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

The two new opt-in network features (a manual update check and the Send-summary report) were the headline, but the bulk of this release was a deep accessibility pass across every window, a security and AV-heuristic hardening pass, internationalisation, and a correctness sweep, all done with the same one-click-from-elevated caution the rest of the app holds to.

### Added

- Check for updates in About now performs the version check itself rather than opening the releases page. Single HTTPS GET to `api.github.com/repos/no-faff/InstallerClean/releases/latest` on click; UA `InstallerClean/<version>`; 8 s timeout; localised result dialog; a styled "update available" window when behind.
- Send summary on the completion overlay. Writes `%LOCALAPPDATA%\NoFaff\InstallerClean\last-run.json` after every Move, Delete or all-clear; opens a confirmation window showing the exact JSON; POSTs to `https://nofaff.netlify.app/api/result-log` on confirm. Counts and categorical labels only. No paths, no usernames, no machine identifiers, no time-of-day. Once per machine, ever, with a confirm-before-send window and a lifetime lock.
- French (`README.fr.md`) and Simplified Chinese (`README.zh-CN.md`) translations of the README.

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
- Detail-panel rows carry screen-reader context so values are announced with their field names rather than read out bare.
- Keyboard reach and focus order corrected across the detail and About windows; modal windows kept out of the taskbar where they should be.
- Inline link colour bumped to meet WCAG AA contrast.
- Operation progress is cleared on success so the status pill resets cleanly rather than holding the last step's text.
- Splash-screen icon load tolerates a failure rather than taking the window down with it.

### Security and hardening

- Defence-in-depth pass on the network and input boundaries: a cap on the Send-summary request size, a bounded JSON parse depth (`MaxDepth=8`) on the update-check and settings deserialisation, and tightened handling of window-activation process IDs in the focus logic.
- Correctness and hardening sweep across the codebase: tightened symmetry in shared helpers, an AboutWindow close guard, corrected cancellation-token ordering in `ScanAsync`, and receipts on the splash icon load.
- Consolidated the user32 P/Invoke surface into Core and removed the need for `AllowUnsafeBlocks` on the WPF host, reducing the heuristic-AV signal of the host binary.

### Changed (internal)

- Codebase-wide comment and code-quality pass: comments brought to a state-the-contract standard, first-person and devlog-style phrasing removed, XAML literals (corner radii, margins, close-button overhang) replaced with design tokens, and the test suite extended to pin the result-log schema, the update-check user-agent contract, and the JSON parse-depth limits.

### Removed

- "Share what you cleared" (browser-mediated, pre-release) replaced by Send summary before tag.
- View last result log link in About (superseded by the confirmation window).

## [1.7.0] - 2026-05-05

A focused release that rebuilt the pending-reboot subsystem from the ground up: not just changing which signals are checked, but redesigning the service to be documentation-grounded, security-hardened against path tricks, and fully unit-testable behind new abstractions, with a 368-line test suite. It closes a real false-positive reported in the field (issue #12).

### Changed

- Pending-reboot detection rewritten to use three narrow Windows Installer signals instead of four broad pending-reboot signals, each backed by a primary Microsoft source rather than adapted from generic PowerShell snippets:
  - `Global\_MSIExecute` mutex is held (Windows Installer is currently writing to the cache).
  - `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\InProgress` key exists (a previous Windows Installer transaction is suspended).
  - A queued post-reboot file rename targets a path under `%SystemRoot%\Installer`.

  The four previously-checked signals (`WindowsUpdate\Auto Update\RebootRequired`, `Component Based Servicing\RebootPending`, `WindowsUpdate\Auto Update\PostRebootReporting`, broad `PendingFileRenameOperations`) had no documented relationship to Windows Installer cache safety, and the combination produced false positives that blocked legitimate use.
- Service surface redesigned from `bool HasPendingReboot()` to a tri-state `PendingRebootResult Check()` returning Clean or Block(reason, detail), so the banner copy and CLI message can be reason-specific instead of a single generic warning. The result type is constructed through a factory that makes the Block-with-no-reason state unrepresentable, letting every consumer switch be exhaustive without a defensive fallback.
- Pending-reboot banner copy and CLI message are reason-specific.

### Added

- `IRegistryReader` and `IMutexProbe` abstractions, so the pending-reboot logic can be unit-tested against simulated registry and mutex state without touching the real system. Backed by a 368-line unit-test suite covering each signal, the tri-state outcomes, and the path edge cases below. The mutex probe asks for `READ_CONTROL` only, the minimum an existence check needs.

### Fixed

- Spurious "Windows is waiting to restart" banner on Windows 11 with no Windows update pending. Closes [#12](https://github.com/no-faff/InstallerClean/issues/12).
- Pending-rename path matching is now separator-aware and canonicalised with `Path.GetFullPath`, so a traversal entry like `\??\C:\Windows\Installer\..\..` no longer matches and a sibling folder like `C:\Windows\InstallerExtra` no longer false-matches the cache path.

## [1.6.0] - 2026-05-05

The largest engineering release in the project's history. The codebase was split into three projects (Core / WPF GUI / CLI), put behind a dependency-injection container, given an `IFileSystem` boundary so every file-touching service is unit-testable, moved to .NET 10, and had the third-party wpfui dependency removed in favour of an own three-layer design system. The MainViewModel was broken into four child view-models. All of this was driven through a sustained sequence of ship-readiness audits (a 28-finding pass, a 24-finding pass, a 40-plus-finding pass) with the findings actioned rather than deferred. The long list below is the result of that work, not padding.

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

Initial public release. Built from the ground up over months: the scan-and-correlate engine, a safety model that moves rather than deletes, a full WPF application taken through nine rounds of UI redesign, a custom dark theme with bundled Poppins, the superseded-patch detection that is the real advance over PatchCleaner, a console CLI, and the distribution and trust work to ship it. The detail below is the shape of that first release, not a summary of it.

### Added

#### Scan and correlation engine

- `C:\Windows\Installer` scan: enumerates every `.msi` and `.msp` file and correlates each against the Windows Installer API to identify which are still registered and which are orphaned.
- Windows Installer query layer over the `MsiEnum*` / `MsiGet*Info` COM API, using the documented double-call buffer pattern to size each call before reading.
- Superseded patch detection: reads actual patch state (State and Uninstallable properties) to find old patches Windows has replaced but never removed. This catches the Adobe Acrobat patches PatchCleaner excludes by default, the difference between recovering a couple of GB and tens of GB on a machine with heavy MSI software.
- Registry fallback enumeration (`HKLM\...\Installer\UserData`) so the still-needed set is found even where the API under-reports; the fallback only ever adds to "still needed", never to "removable".
- Per-package file statistics gathered up front during the scan so the detail windows never hit disk on the UI thread.
- MSI summary-information reader (title, subject, author, keywords, comments, digital signature) via source-generated P/Invoke, surfaced in the detail windows.
- Empty-subdirectory cleanup: after a Move or Delete, the empty folders the cache leaves behind are pruned in the same pass.
- Hardening against an MSI API access-denied condition that could otherwise spin the enumeration in an infinite loop; SID resolution done in a single enumeration pass rather than re-querying.

#### Safety model

- Move (to a folder of your choice) or Delete (to the Recycle Bin, never a permanent delete by default), the move-don't-delete principle so anything can be restored if it turns out to be needed.
- Confirmation dialogs before both Move and Delete, with the Recycle Bin behaviour spelled out.
- Cancellable operations with structured per-file progress tracking for both Move and Delete.
- Pending-reboot detection so the cache is not cleaned while a Windows Installer transaction is mid-flight.
- Settings persisted as JSON with graceful handling of save failures.
- Restore guidance shown after a Move so the user knows how to put files back.

#### Application and UI

- WPF desktop app requiring elevation via the application manifest (the cache is not readable otherwise).
- Main window as a compact summary: registered count, orphaned count, space recoverable, with the action front and centre.
- Orphaned-files and registered-files detail windows, the latter as a master-detail product/patch layout; detail panels scrollable for long content, first item auto-selected and focused on open, full keyboard navigation and mouse-wheel support.
- Startup splash screen showing scan progress steps, cancellable.
- Completion screen summarising what was done.
- About window, custom delete and move confirmation windows.
- Scan duration shown on completion; fast scans suppress the overlay so the window does not flash.
- Proper pluralisation throughout (no "file(s)").

#### Theme and visual design

- Custom dark theme with a layered design-token resource system, inspired by Upscayl.
- Custom `WindowChrome` title bars across every window, with caption buttons styled to match (close has a red hover).
- Poppins bundled as the body font.
- App icon (the squeegee) set on every window.

#### Command line

- Console CLI: `/d` (Delete), `/m` (Move to saved default), `/m PATH` (Move to a specified path), plus `--help`.

#### Distribution and trust

- Self-contained `InstallerClean.exe` and a framework-dependent build (needs the .NET Desktop Runtime).
- README written for the target user, with sourced forum quotes, an honest space-range claim, and credit to PatchCleaner's author.
- VirusTotal scan published (1/70 on the final release build) and linked from the README.
- No data collection.

### Changed

- Renamed from the working title (Simple Windows Installer Cleaner) to **InstallerClean** ahead of launch.
- The original exclusion-filter feature (substring/summary-info matching to exclude files) was removed once superseded-patch detection made it unnecessary: detecting the real patch state is more correct than asking the user to maintain exclusion rules.
