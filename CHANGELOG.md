# Changelog

Every change to InstallerClean, logged in full (not just the user-facing highlights). Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

## [1.9.0] - Unreleased

### Added

- InstallerClean now honours the Windows reduced-motion setting (Settings > Accessibility > Visual effects > Animation effects). With animations off, the heart and star in the bottom navigation no longer grow on hover, the scanning progress bar shows a static full-width fill instead of a moving sweep, and the splash progress bar steps to each stage rather than easing. WPF ignores this setting by default; the app reads it and refreshes live on the system setting-changed message, so toggling it takes effect with no restart. With animations on, nothing about the look changes.
- InstallerClean now follows the Windows text-size setting (Settings > Accessibility > Text size, the "Make text bigger" slider). WPF does not honour it on its own; the app reads the system text-scale factor and applies it to its text before the first window paints, so a larger setting enlarges the app's text while the title bar, icons and control heights keep their shape. It refreshes on the same signal as reduced motion, so moving the slider resizes the text live, with no restart.
- Sorting the columns in the two detail windows is now keyboard-operable. The headers were mouse-only and outside the tab order; they are now focusable tab stops with a visible focus cue, and because a column header is a button underneath, Space or Enter on a focused header sorts and flips direction with no separate sort path. The empty filler header past the last column is skipped. Sort direction had been shown only by an up or down arrow glyph, which a screen reader spells out letter by letter, so each list now exposes the direction as a spoken status ("Sorted by Size, descending") while the glyph stays for sighted users.
- When you delete and the Recycle Bin turns out to be unavailable for the drive, InstallerClean now offers a choice rather than just stopping: move the files somewhere safe (the recommended option, which goes straight into the normal Move flow, opening the folder picker first if you have not set a destination), delete them permanently, or cancel. Nothing is removed unless you choose to delete permanently. That option is honest, these files do not go to the Recycle Bin and cannot be brought back from it, and reassuring without overselling: InstallerClean only ever removes files Windows itself reports as no longer needed, and in the unlikely event you needed one back you would just reinstall that program and you are back to normal, settings and all. The completion screen after a permanent delete says exactly that rather than claiming the files were sent to the Recycle Bin. The dialog states only what is known, that the Recycle Bin is not available for the drive, and never guesses why.
- `installerclean-cli.exe` is now offered as a download in its own right on the releases page, a single self-contained exe that needs no install and no .NET runtime on the target machine. The CLI has shipped inside the setup since it became a separate executable, but the only way to get it was to run the desktop installer on every machine, which defeats the point for scripting, scheduled tasks and mass deployment. Now the one file can be dropped on a client, run for a scan or a clean, and deleted, leaving no desktop app behind. It is the same binary the setup installs, published on its own. Requested in discussion #23.
- `installerclean-cli --version` (or `-v`) prints the executable name and its Major.Minor.Patch version, then exits 0, so a sysadmin can read the deployed build straight from the CLI. It deliberately prints the plain three-part version, not the deterministic build's `+<commit>` informational suffix, so a parsed version stays clean. The flag is documented in the help output and the command-line section of every README.
- A README section, "If a needed file goes missing", explains in Microsoft's own words what the `C:\Windows\Installer` cache is, why a file registered there can go missing by means other than InstallerClean, and how to try to recover it: reinstall the program over your existing copy (without uninstalling first, since uninstalling itself needs the file), using the version you already have where you can, which usually restores it and normally leaves your settings untouched. It keeps the honest caveats, Microsoft guarantees nothing and its own documented last resort is reinstalling the program or Windows, and quotes five Microsoft passages verbatim. The section is mirrored into the French, Spanish and Simplified Chinese READMEs, with the Microsoft quotations left in English. The "Will Windows complain if I remove these files?" FAQ is rewritten from a multi-bullet answer into a short plain one: no, InstallerClean only ever removes the files Windows itself reports as finished with, so nothing it removes is needed to repair, update or uninstall a program, and a file that goes missing by other means is covered by the new section.
- The READMEs gain an Accessibility section, stating only what is implemented: keyboard operation throughout, including keyboard column sorting in the detail windows; Narrator and Voice Access support, where the visible label is the accessible name and a Move or Delete outcome is announced; an always-visible focus ring; and WCAG AA contrast across the dark theme. It is written warm and factual rather than as a conformance certificate, and sits in the same position in every locale so the heading sets stay aligned.
- The all-clean screen now reassures instead of leaving an empty result reading as a wasted run. When a scan finds nothing to remove, a line under the summary says that about two in three people find nothing unneeded, so a clean result is the normal outcome and simply means the Installer folder is already in good order; it adds that the third who do free space clear around 21 GB on the median, some hundreds of GB. It shows only on a from-scratch all-clean, withheld on the rescan that follows a Move or Delete (where the person has just cleaned files rather than come up empty) and never on the Move or Delete summary screens, so it can never tell someone who just cleared files that they found nothing.

### Changed

- Delete now uses the Windows `IFileOperation` shell interface instead of the older `SHFileOperationW`, and guarantees its core promise honestly. The old API could *silently permanently delete* a file it could not recycle (Recycle Bin disabled for the drive, a corrupt `$Recycle.Bin`, or a file larger than the bin allows) while reporting success, so a file the user believed was sent to the Recycle Bin could in fact be gone for good. Delete now checks the Recycle Bin is available for the drive before it runs, and reports a file as recycled only when it genuinely reached the bin. When recycling is not possible it stops rather than destroying anything: the GUI stops and offers a choice of what to do next (see Added), and the CLI refuses with guidance and the transient exit code 75. The per-file outcome now carries the real Windows error code (shown in hex) for any failure.
- Opt-in result-log reports now record the Windows error code behind a failed delete. Each delete error in the report carries a small map of error code to count (the `IFileOperation` HRESULT, in hex), so a delete that fails on a real machine is diagnosable instead of being logged only as a category and a count. The report schema moves to version 3; the receiving endpoint accepts the new code field and the `IFileOperation`-era error categories, and an endpoint that has not yet been updated keeps the report rather than dropping it. As ever the report carries only counts and categorical labels, no paths, names or machine identifiers; an error code is a low-cardinality categorical value of the same kind.
- A delete that fails to recycle a specific file now explains the likely cause instead of one neutral line. The completion-screen message reads the `IFileOperation` HRESULT and tailors it: access denied (`0x80070005`) names a permissions or ownership lock and suggests Move; a sharing or lock violation (`0x80070020`/`0x80070021`) names a file held open by another program; every other code, including the shell copy engine's own `0x8027xxxx` codes that are not publicly enumerated, takes a clearer generic line. It is purely client-side, the code is already in hand, so it needs no telemetry. This is the per-file recycle failure; the separate bin-unavailable case has its own choice dialog (see Added).
- The main window's results read more clearly. A rule now sets the opening explanation apart from the scan counts, and the "registered file missing from disk" note sits as a quiet footnote directly under the count it belongs to rather than as a second wall of body text. The "N files still used" line becomes "N files still needed": a registered file that has gone missing from disk is still counted there because Windows still needs it, it just is not present, so "needed" is the honest word where "used" implied a file that is there and in use.
- A registered file that has gone missing from disk is now flagged in the Registered Files window. It was always listed there and counted among the registered ("still needed") files, but looked like any other row, "0 B" and an empty details pane. Now it shows a warning triangle by the product name, "missing" in place of the size and, when you select it, a note explaining that Windows still expects the file and how to try to put it back.
- The "file missing from disk" copy is rewritten to be accurate rather than to overpromise. The Product Details note, the main-window footnote, the permanent-delete completion hint and the recycle-unavailable reassurance had each promised recovery as a certainty ("put it back, with nothing lost", "all is put right"). Recovering a file deleted from `C:\Windows\Installer` means reinstalling the program from its maker, the matching version is safest because a different one can be refused, and Microsoft's own documented last resort is reinstalling the program or Windows. The copy now says exactly that, keeps the load-bearing hedges ("usually", "normally"), and leads on the real reason the tool is safe: it never removes a file a program still needs, so anything it flags as missing was removed by something else. The Product Details note becomes a single generic explanation shown for every product, with no program name interpolated (a possessive on a name ending in s read wrongly), and now sits top-aligned and scrollable in its pane so the longer explanation reads from the top rather than as a centred block.
- The main window's opening line leads on the reassurance instead of a folder explainer: the unneeded files below are safe to delete, then the three-reason breakdown (a program was uninstalled, a newer patch replaced one or the publisher withdrew it), the basis (InstallerClean only ever lists files Windows reports as finished with), and Move framed as optional (move them somewhere first if you would rather keep a copy). The removable count gains "unneeded", "N unneeded files to clean up", so it matches the intro and reads as clearly removable rather than as just "files".
- The Registered Files missing-file note gains a link into the README: "explains this folder" opens the new recovery section. It routes through the unelevated launcher, so the page opens in the user's own browser at medium integrity rather than as Administrator with no session, the same path every other in-app link uses.
- The About window's Buy-me-a-cuppa button gains a hover line, "It's thirsty work!", in the accent (indigo) tooltip the thanks buttons use rather than the default grey. The button already carries its label, so the tooltip adds a little character instead of repeating it. On the main window the icon-only cuppa, where the tooltip does the labelling, now reads "If it helped, buy me a cup of tea." in place of a bare "Buy me a cuppa", and its screen-reader name follows to "Buy me a cup of tea" so Voice Access matches the spoken tooltip.
- The move-location tooltips read better. The disabled Move button no longer shows its enabled-state text; a state-aware tooltip keeps the explanation and adds the missing step when no destination is set, reverting to the normal text once a path is set. Both button tooltips now name the box they point at, "Move the unneeded files to the Move location.", the disabled one adding "Choose one first.", so the tooltip and the MOVE LOCATION label visibly mean the same thing, a connection the old "the chosen destination folder" left the reader, especially one with little English, to make alone. The path-box tooltip drops "paste one", since anyone who can type a path can already paste.
- The star buttons invite more than a star. The main window's gold-star tooltip now reads "Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome." in place of the bare "Leave a star on GitHub". The About window's star button, whose old "Open the project on GitHub" tooltip only restated its own label, instead carries "or report an Issue or post in Discussions. Any feedback welcome.", reading on from its visible "Leave a star on GitHub" label. The visible labels and screen-reader names are unchanged.
- The Send-summary tooltips are reworded. The old hover text, "An anonymous summary sent to No Faff. Totally optional.", with "Worth sending even when nothing was found." added on the all-clean, named the feature without saying what it does or what happens next. It now reads "Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm.", and the all-clean variant drops the space-freed clause, since a nothing-found run has none to report.
- The main window's star and heart tooltips now sit flush above their buttons with their right edge pinned to the button's, so they stay inside the window. Standard top placement lines a tooltip's left edge up with the button's and grows it rightward, which from the bottom-right corner spilled the heart's tooltip across the window edge onto the desktop, and the star's tooltip used a sideways placement that hung out of the bottom of the window. Each falls back to below its button only when there is no room above. The About window's star and cuppa tooltips, whose buttons sit at the window's left where top placement already lands inside, are unchanged.
- Both donate buttons, the heart on the main window and the cuppa in About, now open nofaff.netlify.app/support, the page that offers Ko-fi and Bitcoin, instead of the bare No Faff homepage, so the button lands where a donation can actually be made.
- Every CLI run now leaves an Application-channel summary entry, closing the paths that used to slip through. A run cancelled before any file was processed (Ctrl+C during the scan or before the first move or delete) exited 130 with no entry; it now writes a Warning entry first. A run with a mistyped or missing flag exited 1 with no trace; it now writes a Warning entry too. And a bare run with no arguments, which used to exit 0 while doing nothing, is now a usage error: it prints the usage and exits 1, so an argless scheduled task fails visibly instead of succeeding silently. An explicit `--help`, `/?` or `-h` still prints usage and exits 0.
- Each CLI Application-log entry carries a stable numeric Event ID for its outcome class (1000 completed, 1002 completed with per-file errors, 2000 transient skip, 4000 hard error), so a non-English consumer can classify a run by number rather than by matching the English summary text. The mutex-contention skip (another InstallerClean already running) moves from Information to Warning, so a "Warning and above" filter now catches every run that did not do its job.
- The CLI reports move and delete progress synchronously, so its stdout stays in order. A console process has no synchronization context, so the per-file "[i/total]" lines had been marshalled through the thread pool and could print out of order with each other and with the closing summary ("Deleted N files."), which an RMM scraping stdout treats as significant; the progress now runs inline on the producing thread. The GUI keeps the dispatcher marshal, since it wants the callback back on the UI thread.
- The CLI's single-stream output contract is now documented in the README command-line section. The CLI writes everything, including error and diagnostic lines, to stdout with no separate stderr: stdout is the audit channel, the exit code is the machine signal and the Application event log entry mirrors it. That was the design all along but undocumented, so a consumer capturing stdout could not know an error line might land in the same stream.
- A CLI `/m` run validates its move destination before scanning, not after. A misconfigured destination (missing, relative or pointing inside `C:\Windows\Installer` or a system folder) used to scan the whole Installer folder first and only then fail, so the walk was paid for every run; the destination is now resolved and gated before the scan, so it fails fast. One consequence: a bare `/m` with no destination set now fails on every run rather than only on the runs that found something to move, which is the correct reading of a misconfigured task.
- The CLI `--help` legend for exit code 75 is now cause-agnostic. A third condition has returned 75 since the recycle rebuild, a delete refused because the Recycle Bin is unavailable for the drive, for which the old "try later" advice is wrong, since a plain retry never clears a bin disabled for the drive. It now says a temporary condition blocked the run and points at the printed message, where the specific remedy already appears.
- The installer now requires Windows 10 version 1607 (build 14393), the oldest release the .NET 10 Desktop Runtime supports. Setup had allowed any Windows 10 build, including ones too old for the runtime, where the app would fail at first launch rather than at install; only those pre-1607 builds, themselves long out of support, are newly blocked, and they get a clear setup message instead of a cryptic runtime failure. Inno reads the true build via `RtlGetVersion`, so the check holds on Windows 10 and 11.
- The setup now installs the MIT licence beside the two executables, as `LICENSE.txt`. `pad.xml`'s redistribution terms require the licence text to travel with any redistributed binary, so a mirror or compilation that copies the install directory carries the terms with it. The installed copy is given a `.txt` extension so a double-click opens it in Notepad rather than the "how do you want to open this file?" picker.
- The removable files are called "unneeded" consistently across the GUI, the honest partner of the registered list's "still needed": a registered file can be needed by Windows yet missing from disk, so the axis is needed or not-needed, not used or not-used. In 1.8.2 these surfaces said "unused" (the term an earlier release chose when it renamed them from "orphaned"), and the clean-up count was a bare "N files to clean up"; the window, the size summaries, the screen-reader labels and the CLI scan lines now all read "unneeded", matching the intro and the count, so the whole app uses one word.
- The two Details windows are retitled in plain descriptive terms rather than bare labels: "Unneeded files that are safe to delete" and "Registered files that should not be deleted", in place of "Unused files" and "Registered files". They are owned, content-sized dialogs, so the longer titles fit, and a screen reader announces what each window is for when it opens.
- The Registered files window opens as tall as the screen sensibly allows. Stepping down the products with the arrow keys is the natural way to read each one's details, and at the fixed 680 height a typical product's detail list did not fit, so each product meant clicking into the details pane and scrolling. With no remembered size the window now opens at up to 860, clamped to the work area of the monitor the app is on: a 1080p laptop at 150% scale has only about 672 units of work area and gets roughly the old height, while a 100% desktop gets the full 860. The products list keeps its old height, every extra unit goes to the patches and product details band below, and a size set by resizing the window is still remembered and still wins.
- Two delete-path messages are tightened. A consented permanent delete that partly fails now keeps the "they did not go to the Recycle Bin" clause its clean-path siblings carry and gains a singular form, so a single-file partial failure no longer reads "1 file permanently deleted. 1 error" with nothing saying the bin was bypassed. And the single-file recycle-unavailable dialog drops a literal count, reading "So this file ..." rather than "So this 1 file ..."; the plurals are unchanged.
- The About window's second link to the repository is removed. The "What it does, and why it's safe" link opened the same place as the Star and the MIT licence links, so with the repository one obvious hop away it earned nothing; the pane's "explains this folder" link into the recovery section stays.
- The README is now published in seven languages. Japanese, Brazilian Portuguese and Russian join English, Simplified Chinese, Spanish and French, and the three existing translations are brought up to native quality and resynced to the current English. Every README carries the seven-language switcher, the inline screenshots and the Reddit quotes translated with a note, while the Microsoft quotes are kept in their English original with the source link. The languages were chosen by which developer audiences would most value a native README, not by raw speaker counts.
- The READMEs are reconciled with the 1.9.0 delete and CLI behaviour. The delete copy now matches the conditional Recycle Bin behaviour, that Delete sends files to the bin and, when the bin is unavailable for the drive, refuses to remove them for good on its own, offering Move or a permanent delete you confirm; the old unconditional "goes to the Recycle Bin, restore from there" promise is softened across the intro, the "Is it safe?" passage and the undo-a-delete FAQ, with Move framed as the extra-safe option rather than the thing that makes it safe. That nuance is now single-sourced in the "Is it safe?" passage so the four locales cannot drift, and the CLI exit codes are corrected (a `/s` scan is 0 on success, 1 on failure and 130 on Ctrl+C, not "always 0").
- The PatchCleaner comparison table gains a delete-safety row: InstallerClean sends deletes to the Recycle Bin and asks before any permanent removal, so it never does a silent permanent delete, whereas PatchCleaner's delete path removes orphaned files permanently with no Recycle Bin step. Mirrored across the locales.
- The README FAQ gains a table of what recent runs have found, drawn from the opt-in summaries people have been kind enough to send in, so a visitor can see the range of space freed on real machines.
- The reports table's figures are localised to each translation instead of kept in a single English format. They follow each language's own conventions: a comma decimal separator in French, Spanish, Russian and Brazilian Portuguese (0,2 rather than 0.2), a space before the percent sign in French, Spanish and Russian (68 %), and the gigabyte unit written "Go" in French and "ГБ" in Russian; French uses the narrow no-break space the rest of that file already uses, while English, Chinese and Japanese keep the form they had. The figures are produced from each file's locale rather than hand-formatted, so the format follows the data. The Spanish and Russian Softpedia badge alt text gains the same percent space, which a screen reader reads aloud.
- The README opening leads with the honest split. The "how much space" bullet now reads that only about a third of machines have unneeded files to clean (median around 21 GB freed, some hundreds of GB), while the other two-thirds find nothing because their Installer folder is already clean and the scan takes seconds either way, in place of the previous per-machine anecdotes (a measured figure, a user's report and the Adobe Acrobat line).
- The Delete confirmation no longer warns that a large delete may skip the Recycle Bin (a warning shown since 1.5.2). It fired whenever the total exceeded 1 GB or any single file 500 MB, but Windows only sends a file to permanent deletion when that one file is larger than the bin's per-drive quota (usually many GB) or there is no room, in which case it evicts older binned items to fit it; the total of a multi-file delete does not push the new files out, so the warning fired on ordinary deletes. The genuine "Recycle Bin unavailable for the drive" case keeps its own choice dialog, and the plain "Files will be sent to the Recycle Bin" line stays.
- The About window's star and Buy-me-a-cuppa tooltips now appear instantly, matching the main-window star and heart that already had no show delay.
- The pending-reboot banner shown while Windows Installer is busy is reworded from jargon into plain terms. The old "Windows Installer is currently processing an installation. Move and Delete are disabled until it finishes." read as a pending-updates warning and implied Move and Delete switch back on by themselves; they do not, the state is only re-checked on a scan. It now reads that something (usually a Windows Update or a background install) is using Windows Installer right now, that Move and Delete are paused so the cache is not touched while it is changing, and that you Re-scan once it is done to bring them back. The CLI's matching gate message had kept the old phrasing and follows suit: "Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes." (retrying is the right advice for this cause, and the CLI has no Re-scan).
- The Delete confirmation no longer shows a warning triangle by its heading. A delete to the Recycle Bin is the normal, safe action InstallerClean is for, not a hazard, so dressing the confirmation with a warning icon was off; the heading now stands on its own.

<!-- RELEASE-OPERATOR: if the compressed ~65 MB portable clears VirusTotal at the Stage 2 VT pause, add this Changed bullet; if it flags and the portable stays uncompressed, drop it:
- The portable build returns to the compressed single-file shape, roughly halving its download size (about 135 MB back down to about 65 MB). It had been published uncompressed since v1.8.2 to clear a Microsoft Defender machine-learning false positive (`Trojan:Win32/Wacatac.B!ml`) that fired on the compressed runtime bytes, never on anything the code does; Microsoft retrains the detector on cleared false positives over time, so the compressed shape is clean again.
-->

### Fixed

- The unneeded-files window's Size column is actually visible. It shipped in 1.8.2's column rework and the list has opened sorted by it, biggest file first, ever since, but the fixed column widths overran the list pane and horizontal scrolling is disabled, so the column sat wholly past the pane edge with no way to reach it. Installer-cache names are short random hex, so the File column gives up the spare width and the Size column lands inside the pane, sort arrow and all.
- Three accessibility gaps on the overlays and choice dialogs are fixed. The Confirm Delete, Confirm Move and recycle-unavailable dialogs opened with focus on the window rather than a control, so a keyboard user saw no focus ring until they Tabbed; each now focuses its Cancel button on open, so the ring shows at once and a reflexive Space lands on the non-committing choice. The recycle-unavailable dialog, the app's highest-consequence decision, gains a window-level description that frames the move, delete-permanently or cancel choice up front. And the completion outcome is now announced: focus moves to Done when the overlay appears, so a screen reader had read only the button and never what happened ("All clean", "120 MB freed", "Moved N files"), and the heading and summary are now live regions raised explicitly on reveal, since the WPF UIA bridge does not re-announce a collapsed-to-visible change on its own. The splash hero and the completion icon, both decorative, are now hidden from the screen-reader tree instead of sitting in it as nameless images.
- The update-available dialog now focuses Cancel on open and carries a window-level description of the choice, matching the other modals; it had opened with focus on the window and no readout, so a keyboard user saw no ring and a screen reader did not state the dialog's purpose.
- Keyboard focus no longer falls to nowhere in two main-window transitions. After the completion overlay was dismissed (Done, Esc or clicking the dimmed background) the focused button was gone and focus fell to the window root, and a startup scan that found unneeded files opened the window with nothing focused. Dismissing now returns focus to the Re-scan button, and the results view focuses the move-destination field, the start of the Move workflow, never the destructive Delete; the same default applies after a manual re-scan finds something.
- Several buttons' accessible names are brought back in line with their visible labels (WCAG 2.5.3, Label in Name). They had set the accessible name to a sentence that did not contain the on-screen word, so a Voice Access or Dragon user saying "click Delete" or "click Done" was not matched, including on the highest-consequence button, permanent delete. The accessible name now falls back to the visible label and the longer description moves to the help text, which Narrator and NVDA still read, so nothing is lost for screen-reader users.
- The MIT licence link's help text lowercases "licence". Narrator at default verbosity spells an all-caps token out letter by letter, so "Opens the LICENCE file" was read "L-I-C-E-N-C-E"; as "licence" it is read as a word. The visible link label is unchanged.
- GUI no longer reports freed space when nothing was deleted. If the Recycle Bin was unavailable, the completion screen previously showed a green "freed N MB" over "0 files deleted" and logged that false figure; it now frees nothing in that case and offers a choice of what to do instead (see Added).
- The pending-reboot warning now wraps and shows its full text. It sat on a single line that ran off the right edge when the message was long, so the end of the guidance (such as "...before cleaning the cache") was cut off; it now wraps like the missing-from-disk warning. The main window is now non-resizable and sizes itself to its content (like the confirmation dialogs), so a tall warning banner makes the window grow to fit rather than clipping the Delete and Move buttons or showing a scrollbar.
- Scan-cancel status now updates the instant Esc is pressed. `CancelScan` set the "Cancelling..." status after cancelling the token; reordering it before the cancel makes the synchronous write land immediately (the scan's own progress reporter only fires on its next callback) and guarantees the scan's later "Scan cancelled." write lands last. On the single UI thread the old order was harmless; the race only showed in a SynchronizationContext-free unit test, where it flaked in CI. No user-visible change on the running app.
- The scrollbar groove pages again. The custom ScrollBar template carried only the thumb, with no repeat-buttons on the track, so clicking the groove above or below the thumb did nothing instead of paging. Transparent, hit-testable page-buttons on both the vertical and horizontal tracks restore paging; the 8px thumb is unchanged. This applies to every scrollbar in the app, which share the one template.
- The Browse button's caption lines up with the Details and Move edges again. It used the ghost button's default 20px side padding while the Details links override to 12px, so its caption sat 8px adrift; matching its horizontal padding to 12px aligns them. Its height and vertical padding are unchanged.
- The operation-status line under MOVE LOCATION is tidied. A Move or Delete you cancel before any file has been touched now leaves the line blank, rather than parking a standalone "Move cancelled." or "Delete cancelled." beside the buttons; only a cancel that interrupted work already under way still reports what it managed ("Move cancelled. 23 of 100 files processed."), and Move and Delete now behave the same. Previously Move left a standalone "Move cancelled." while Delete left nothing, so a short message could also linger from a prior cancel. That remaining message is left-aligned: a `MaxWidth` on the bound text with the default Stretch alignment had centred it in the space beside the buttons, so it floated in from the margin, and `HorizontalAlignment=Left` pins it to the column.
- A move or delete that crashes outright now surfaces a dialog naming the exception type and the crash-log path, matching the pre-flight and validation failures, instead of a body-row status line. That inline line trimmed the crash-log path off at its width cap, and reaching it is rare anyway, since the services collect per-file errors rather than throwing.
- Concurrent settings saves can no longer lose each other's changes. Three places did load, mutate, then save with no lock, and the debounced move-destination save runs on a thread-pool thread while the window-size and sent-summary saves run on the UI thread, so two could interleave and the last write would win wholesale, silently dropping the other change (a typed destination, the sent-summary flag or a saved window size). All four writers now go through a single `Update` call that holds a lock around the load-mutate-save, and the one-shot sent-summary flag is written synchronously so it cannot race.
- The pending-reboot check no longer reports "clean" when it hits memory pressure. Its mutex and registry probes had caught every exception, including `OutOfMemoryException`, so a held Windows Installer mutex under memory pressure could be reported as safe to clean; the catches now exclude `OutOfMemoryException` and `StackOverflowException`, matching the registry and mutex readers, so a real memory-pressure failure surfaces as a scan error rather than a false all-clear.
- The recycle engine publishes its work queue safely and refuses work after disposal. Its fields were read outside the lock without being volatile, so on a weakly-ordered target (Windows on ARM64) a caller could observe the queue before its construction was visible, and a delete racing app shutdown (Alt+F4 mid-delete) could reach a disposed queue and throw the wrong exception. The fields are now volatile, and a call after disposal throws the `ObjectDisposedException` the delete service already degrades to a per-file error.
- A successful clean-up no longer reports a hard error because of the tidy-up afterwards. The empty-subdirectory prune that runs once a delete or move has committed caught per-directory failures but not a failure in the directory walk itself (the Installer folder vanishing mid-walk, or a share violation raised by the enumerator), which would propagate and report exit 1 on a run that had in fact moved or deleted the files. The walk is now wrapped so any non-cancellation failure in the best-effort prune is swallowed, while a requested cancellation still propagates; one shared helper covers the GUI and both CLI paths.
- The update-available dialog's title wraps instead of clipping. The heading had no wrapping while the card is width-capped, so a longer localised title (German, French and Spanish all run longer than "Update available") would have been cut off; it now wraps like the other modal headings.
- In-app links open reliably again. The unelevated URL launcher, used by the recovery link in the Registered Files window and by the GitHub, support, star and update-prompt links in About, opened a URL by duplicating the desktop shell's token and calling `CreateProcessWithTokenW`, which requires `SeImpersonatePrivilege` on the calling token; on an elevated token that did not grant it the call was refused with access-denied (error 5), so every in-app link fell back to copying the address to the clipboard instead of opening. The launcher now drives the already-running Explorer through the shell-view automation chain (`IShellWindows` to `IShellBrowser` to `IShellView` to `IShellFolderViewDual` to `IShellDispatch2.ShellExecute`), so the link opens in the user's medium-integrity browser with no privilege required and works on locked-down machines. The guarantee is unchanged: it opens unelevated or, when any step in the chain is unavailable (no running shell, a refused cross-integrity activation), falls back to the clipboard, and never opens the browser elevated; `ShellExecute` is restricted to absolute http/https URLs as a guard. The now-unused advapi32 token P/Invokes are removed.
- Two confirmation-dialog headings can no longer clip a long heading. The Delete and Recycle-unavailable headings placed the warning icon and the heading in a horizontal StackPanel, which hands the text unbounded width so wrapping never engaged and a long heading ran off the card edge; they now use the icon-docks-left DockPanel the pending-reboot and missing-from-disk banners already use, so the heading wraps and the SizeToContent card grows to fit.
- The move-destination box no longer shows its tooltip unprompted. WPF shows a control's tooltip on keyboard focus for accessibility, and that box is the control the results screen auto-focuses, so its tooltip appeared on window open and again on alt-tab back with no pointer near it; `ToolTipService.ShowsToolTipOnKeyboardFocus` is now false on the box, so it shows on hover only. The keyboard focus ring it shows on focus is unchanged.

### Changed (internal)

- The `WarningTooltip` style is renamed `AccentTooltip`. It is the indigo accent tooltip used on the Buy-me-a-cuppa and Star buttons, never on a warning, so a use-based name misled; `AccentTooltip` names it by what it is and matches the `AccentPill` / `PrimaryPill` / `DangerPill` naming.
- The `MainViewModelTests` clean-up-count assertions are updated to the new "unneeded" wording; they had pinned the old "{N} file(s) to clean up" string.
- Dead design tokens, primitives and the unused `LinkButton` style are removed. `Surface.AppBackground`, `Selection.Background`, several unused `Border` and `BorderThickness` steps, plus the primitives only those consumed, had no consumer anywhere in the app, against the theme's rule that a token earns its place only when a real consumer surfaces; the `LinkButton` style was likewise unused, since only `SubtleLink` is applied. The comments that cited the now-removed examples are corrected in the same pass.
- A read-only `list-unused-resources.sh` reports `Strings.resx` keys and XAML resources that no source file consumes, as a pointer for cleanup. It never edits or deletes, since a key can be retired only once a human confirms nothing builds it at runtime, and it matches a resx key in both forms it can take, the underscored C# accessor and the dotted XAML `{loc:Translate}` form, excluding the generated designer and the build trees.
- A `GapTop.Sm` token replaces three literal 8px top margins (the missing-from-disk banner, the stale-MSI line and the operating-overlay Cancel button), naming the gap like the `GapBottom` family, and the deliberately off-scale operating-overlay margins and the detail windows' negative bleed gain explaining comments. The warning-triangle icon gap in the Registered Files window also moves to the existing `GapRight.Icon` token instead of a matching literal.
- An unused `System.Net` import is dropped from the update-check service; every networking type it uses lives in `System.Net.Http`.
- The CLI's argument-to-command mapping and finished-run classification are extracted into a `CliContract` type in Core, so the exit-code and EventLog contract that RMM tooling and scheduled tasks pin to carries unit-test coverage; the Tests project references Core but not the console executable, so logic left in `Main` could not be tested at all. `Main` stays a thin Console and Environment shell.
- The delete test suite is rebuilt for the `IFileOperation` engine: unit tests over a fake recycle engine (recycle, permanent-delete, failure and recycle-unavailable paths), hex HRESULT rendering, integration tests driving the real recycle engine and the recycle-unavailable completion state; the old `SHFileOperation` tests are removed.
- Two registry and disk hops move off the UI thread so they cannot stall it: the pending-reboot probe (a mutex open plus three registry reads, sampled after the scan) and the settings persists. The settings tests are updated to assert the new `Update` call and its effect rather than the previous prepared-snapshot `TrySave`.
- Two strings are made translation-safe. The missing-file note's "the README explains this folder" sentence, previously split across three keys to hold the link's position, becomes one string whose linked phrase is delimited by [ ] and parsed at load, so a translator can move the link to suit a different word order. The resx header's category index, a translator's map of the file, gains the `RecycleUnavailable.*` entry, the recycle rebuild's choice-dialog strings, that had been missing from it.

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
