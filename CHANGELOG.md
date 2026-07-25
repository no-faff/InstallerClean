# Changelog

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

## [2.3.0] - Unreleased

### Added

- RijckAlex contributed, entirely unprompted, a complete Dutch translation of InstallerClean (#54): every window, dialog, tooltip, button and screen-reader label, plus the command-line tool's text. Wiring it into the app took a pass over every string for the je/u register and for Dutch count agreement rather than a calque of the English, then the installer's Dutch wizard text and a full Dutch README to go with it. InstallerClean can now be displayed in Dutch, its sixteenth language, applied automatically on a Dutch-language Windows or picked from the main-window language menu. Where the languages stand:

  | Natively done | Live machine-translated | README only \* |
  | --- | --- | --- |
  | English · Italian · Japanese · Dutch | Simplified Chinese · Russian · Spanish · Brazilian Portuguese · Polish · Turkish · Korean · French · German · Indonesian · Vietnamese · Ukrainian | Arabic |

  \* README only means the README is translated while the interface stays English. Arabic is here on that basis, because a right-to-left interface is a separate piece of work I might tackle later.

### Changed

- The installer's Italian wizard text is refreshed to Inno Setup's current official Italian translation (6.5.0+), which bovirus maintains upstream and flagged as out of date (#53). InstallerClean now carries that file itself rather than using whichever version the compiler happens to have, so the current wording ships every time. Three other installer languages were already carried this way, Simplified Chinese, Vietnamese and Indonesian, though for a different reason: Inno Setup does not ship those at all.
- The About window's licence link no longer carries a separate screen-reader name duplicating its visible text: the link's own text is the name, matching the window's other links, so the spoken words can never drift from the shown words in any language.
- The three small 28px buttons (the language globe, the completion screen's donate heart and the splash screen's Cancel) now share one named style instead of each assembling the same geometry inline, so the small height and its matching focus ring can never drift apart. No visual change.
- The About window's Close button takes the accent colour, the same reading that gives the completion screen's Done its fill: the one finishing action a window leads with. It was a grey pill that did not quite read as a button.
- Four more buttons take the accent colour with it: Send on the report confirmation, the button that opens the releases page when an update is found, and Close on both Details windows. Each is the one thing its window is asking you to do, and each was a grey that read as hesitant rather than quiet. The main window's Move keeps the grey on purpose, because it is meant to sit second to Delete, and so does the Close on a warning, where an inviting button would sit badly with bad news.
- The delete confirmation's own Delete button is no longer red. The app tells you these files are safe to remove and the dialog tells you they go to the Recycle Bin, so a red button there was arguing with both. Red is now kept for the one delete that genuinely cannot be undone, the permanent delete offered when the Recycle Bin is unavailable.
- Four things a screen reader reads out now write GitHub in lower case, the descriptions of "Check for updates", the automatic update-check tick box and the About window's guide link, plus the name of the "Leave a star on GitHub" button, because with the capital a speech engine breaks the word there and spells the first half, so it arrives as "G. I. T. hub". Every visible label is untouched, the protocol name has gone from the update description and the brackets from "the guide (README)", and all fifteen translations were rewritten to match.
- The screen-reader names of the About window's two thank-you buttons no longer end with "(About window)", which was there to tell them apart from a star and a cuppa the main window has not had for some time. It was being read out to somebody who could only be in that window to hear it.

### Fixed

- A single installed program whose patch list Windows refuses to return no longer stops the whole scan. This can happen with an unusual but valid registration (a per-user program recorded under a system account, as some corporate DisplayLink deployments are), where Windows rejects every entry in that one program's patch list: the scan now sets that program aside and keeps all of its cached files, reporting it through the existing "N installed programs could not be read during this scan, so superseded patches have been kept" notice, instead of failing with "Scan failed" and cleaning nothing. Orphaned-file cleanup, the app's main job, is unaffected.
- Tabbing from "Check for updates" to the language globe no longer takes two presses. The update-available link, hidden until a check finds a newer version, still sat in the keyboard's tab order while invisible, so the first press landed on nothing; it is now skipped until it is actually shown.
- The keyboard focus ring on links now has the same weight and breathing room as on every other control, rather than the thinner, tighter outline links used to draw.
- The About window's "Buy me a cuppa" button no longer disappears under the Close button in the languages whose labels run long, Dutch and German among them. Close now has its own space in the row, and the two thank-you buttons take a second line where they need one.
- Column headers in both Details windows now sort the list when you press Space or Enter on one, not only when you click it. A focused header was throwing the keystroke away unless the mouse pointer happened to be resting on that same header, so sorting from the keyboard did nothing at all.
- `C:\Windows\Installer` stays on one line wherever the app shows it, instead of breaking after the "C:" and carrying the rest onto the next line. Dutch is where it showed up, but where a line breaks depends on the words around it, so any language could land on it.
- The arrow that shows which column a Details window is sorted by no longer runs out of room. In the languages with a longer word for size the heading filled the column and pushed the arrow out, so Italian lost it altogether and Vietnamese showed half of it. The size and patch-count columns are wider now, taking the space from the file and product-name columns, which had it going spare.
- The scrollbar handle in both Details windows no longer runs past the pane's rounded corners onto the window background, where at each end of the scroll it left the panel it belongs to. The registered-files patch list did the same thing and is fixed with it, as is the horizontal scrollbar that appears at very large Windows text sizes, which sat half below the panel's bottom edge.
- Delete, Move and Send report showed one explanation on hover and read a differently worded one out to a screen reader; each now uses a single piece of text for both. Move gains a detail with it: when no destination is set, its spoken description says a folder browser opens first.
- The About window's "Leave a star" and "Buy me a cuppa" buttons explained themselves in a tooltip and said nothing to a screen reader, so the reason for either ask reached only the people who could see it. Both now read out what the tooltip shows.
- The note that a signing certificate is the name a file claims rather than a verified one sat in a tooltip in both Details windows, so it never reached a screen reader. It is now part of what the certificate value reads out.
- The main window's "Re-scan" button had no spoken description, while "Scan again" on the finished screen, which runs the same scan, had one. Both explain themselves now.
- The warning shown when the Recycle Bin is unavailable was read out from halfway through its own sentence, opening on "So these files haven't been deleted" with nothing before it. It now leads with the heading, so the first thing said is that the bin could not be used.
- The confirmation shown before a move read out the file count and size but not the destination folder, the one thing it exists to confirm, and there was no way to reach that folder name from the keyboard either. It now reads the destination as part of the confirmation.
- The version number in the About window was read out twice by a screen reader, once as the label and once as the content. It is read once now, introduced by the app name above it.
- Invisible characters that keep `C:\Windows\Installer` on one line are no longer put into text that is only ever spoken and never drawn, where they did nothing and left a speech engine to make sense of them.

## [2.2.0] - 2026-07-23

### Added

- The app now checks for a newer version by itself: once per run, started the moment the app opens, as a single request to GitHub's releases page. If one exists, a quiet "Version X is available." link appears in the bottom bar and opens that release's page in your normal browser. Nothing is downloaded, nothing interrupts you, and an up-to-date result or a failed check says nothing at all.
- A "Check for updates automatically" checkbox in the About window governs the automatic check. On by default; a change applies from the next launch, and with it off the app makes no network request unless you press a button that makes one. It is the app's first checkbox, so there is now a house CheckBox style built from the existing theme tokens.
- A "Check for updates" button on the main window's bottom bar, with a status line beside it for "Checking...", "Up to date." and the update link. The check no longer hides behind About, where the button read as credits rather than updates.
- The About window links to the guide (the README, opened in the app's display language and landing on the document itself rather than the repository page the star button already opens) and to the issue tracker ("Report a problem"). The README, which is the app's actual manual, was previously only reachable via the star button.
- A small donate heart in the corner of the completion screen, shown only after a clean-up that actually did something. Its tooltip does the asking, quietly.
- The per-file safety check that runs before any Move or Delete now refuses anything that is not directly in the top level of `C:\Windows\Installer`, enforcing outright the "never acts inside subfolders" promise SECURITY.md makes. The scan has not offered such a file since 2.1.0 stopped looking inside subfolders; now nothing downstream could act on one either, and the refusal message says "not directly inside" precisely, in all fifteen languages.
- Release downloads carry the version in the filename from this release (`InstallerClean-2.2.0-setup.exe`, `InstallerClean-2.2.0-portable.exe`), so a copy on a mirror, a USB stick or a support thread says what it is. The command-line tool deliberately keeps its unversioned `installerclean-cli.exe` name: scheduled tasks and scripts hold a path to it, and a versioned name would break them all on every update.

### Changed

- Every string this release adds or rewords is translated in all fourteen languages: the About window's guide and "Report a problem" links, the automatic-update checkbox, the update status line on the bottom bar, the donate ask on the completion screen, the warning shown when a setting cannot be saved, and the names and help a screen reader reads out on each of them. The star button's tooltip in About is reworded to say what a star actually does. A non-English user sees all of it in their own language instead of falling back to English. Five languages had their keyboard access keys re-picked along the way so every control on the main window keeps a unique Alt shortcut.
- The bottom bar slims to Re-scan, About, Check for updates and the language globe: the star and heart buttons are gone. The star lives on in About; the donate ask moved to the completion screen, where it follows an actual result instead of sitting in the window chrome.
- The About window lost its update button and inline status (moved to the main window) and gained the links block and the updates checkbox, with the licence line grouped into the links as one block.
- The star pill's tooltip now says what a star does ("A star helps other people find it.") instead of doubling as the route to Issues and Discussions, which "Report a problem" covers properly. The button's label already says GitHub, so the tooltip does not say it again.
- A comments pass covered everything that changed since 2.1.0, the same discipline as the 2.1.0 whole-tree pass: every comment added or changed was checked as a claim against the code, and the unchanged comments around changed code with it. Thirteen were brought back in line and phrasing that dated itself was replaced with the reasons themselves; the pass also caught the access-key check defect fixed below.
- The indigo tooltips, on the About window's star and cuppa buttons and the completion screen's new heart, now draw a hairline border in the card colour. Where one opens across the equally indigo Done button they used to read as one shape; the border keeps an edge between them.

### Fixed

- The update check reads the version from GitHub's releases page rather than its REST API. That API refuses anonymous requests once 60 an hour have come from the same address, counted across every program behind it, so on an office connection, a mobile network or a commercial VPN the check could report an error with nothing wrong at either end.
- The access-key check now reads a doubled underscore the way the app renders it, as a literal underscore rather than a key marker. No current label uses one; the first that did would have made the check report a key that isn't there or hide a clash that is. Its failure message also now gives advice a contributor can follow.
- The optional anonymous report can say "failed" again when a clean-up achieved nothing. Since the act-time re-check arrived in 2.1.0, a run where that check kept some files back and every remaining file then failed was misreported as "partial" with zero files processed.

## [2.1.0] - 2026-07-20

### Changed

- Every string this release adds or rewords is translated in all fourteen languages: the new error and status messages, the not-yet-scanned and pending-reboot main-window states, the same-drive move warning, the cancelled-operation summaries, the reworded stale-patch and access-denied wording, the rewritten scan and per-file failure messages, the rebuilt completion screen's headings and count line, the main window's opening line and its new sentence on how to undo a clean-up, the reworded reassurance after a Move or a Delete (including the linked phrase inside it, which each language places where its own grammar wants the link), the Move button's tooltip and the names a screen reader reads out on the Move box, the Move button and the report preview, the line shown while the app waits on a slow Recycle Bin, and the About window's licence label, which now names Apache 2.0 in each language's own wording for a licence. A non-English user sees them in their own language instead of falling back to English.
- Two messages are reworded: the access-denied error now names the Windows Installer records plainly, and the line reporting files kept back during a clean-up now says a program started needing them again after the scan, rather than reading as though it had needed them all along.
- The line reporting files kept back during a clean-up now has a second version, for when the check before the clean-up could not fully read the Windows Installer records. It used to give the one reason it knew, that a program had started needing the files again, which in this case had not happened: the app was naming a cause instead of saying it could not tell.
- The installer's "InstallerClean is still running" prompt, shown by Setup and by the uninstaller when the app is open, now names the two processes that can hold the lock (`InstallerClean.exe` and `installerclean-cli.exe`) and points the user at Task Manager, in all fifteen languages. The single-instance mutex is shared by the app and the command-line tool, so Setup or an uninstall can correctly find InstallerClean "running" with nothing on screen (a command-line or scheduled-task run holds the same mutex); Inno Setup's default text only says to close all instances, which leaves the user no way to find an invisible one.
- Every error, warning and crash message now appears in InstallerClean's own dialog, on the same dark card as the Move and Delete confirmations, rather than a stock grey Windows message box with no owner window. The moments the app looked least like itself were the moments it had just failed while running as administrator inside `C:\Windows\Installer`, which is exactly when it should look like the app you trusted. The message can still be selected and copied, so the crash-log path and the link on the clipboard are as reachable as they were.
- Six error messages have been rewritten. Three told you to run InstallerClean as administrator, which it always already is: it cannot start at all unless you accept the Windows prompt, so an access-denied never means "you are not an administrator", and you were handed the one instruction you had already followed. They now say only that Windows refused; the one about the Windows Installer records goes on to say that running as administrator again will not change anything. The other three explained the app's internals instead of what happened: a relative Move location talked about the process working directory, a destination swapped mid-move reported a "canonical path changed mid-batch" and a link that could not be opened blamed "your normal-user browser", which is not a thing anyone outside the source code has a name for.
- The messages shown when a scan cannot finish now say only what InstallerClean actually knows. They had been recommending `sfc /scannow` in five places, and blaming security software, changed permissions, a corrupt database or an ownership lock: none of that was ever observed, and `sfc /scannow` repairs protected system files rather than the registry data these messages are about. Each now states the condition that was detected, says plainly where Windows gives no reason to pass on and ends by telling you whether anything has been removed, which is the sentence that matters when a dialog appears over a list of your files.
- The four messages shown for a file that could not be cleaned up now differ in what they suggest, because the failures behind them differ. A file another program holds open cannot be moved either, so that one no longer points at the Move button and says to try again later instead; a refusal that could be coming from either the file or the Recycle Bin now says so, Move being a help in only one of those cases; and the case InstallerClean genuinely cannot explain says that, rather than guessing at a lock. Each of the four now also says the file was left in place.
- The message shown when Windows deletes a file outright instead of moving it to the Recycle Bin now says that Windows did it, and that the file is gone. InstallerClean asks for the Recycle Bin on every delete, and when Windows reports success while nothing has actually arrived in the bin, the file is recorded as an error rather than counted as deleted, so anyone reading this message never agreed to lose it. The old wording, that the file was permanently deleted because it could not be moved to the Recycle Bin, never said who had done the deleting, which left it reading as though InstallerClean might have.
- The heading on the screen shown after a Move or Delete now states one outcome and wraps instead of clipping. It used to carry the outcome and a failure notice in a single line ("1.28 GB freed, some files could not be processed"), which ran off both ends of the card in English and had further to run in longer languages.
- How many files failed now has its own line, in the warning colour, directly under that heading. It had been bolted onto the end of the line naming the folder your files were moved to, where it read as part of the folder's name.
- A Move or Delete that got nowhere now says "Nothing was moved" or "Nothing was deleted", in the warning colour, instead of "0 B freed". It also no longer offers a destination, a restore hint or an invitation to empty the Recycle Bin, all of which describe files arriving somewhere none of them reached.
- The list of files that could not be cleaned up now introduces each group with a properly worded sentence and indents the filenames under it with a hyphen. The count used to be a bracketed "(2)" appended to a sentence written for one file, which no language could inflect and which read as a reference number, and the filenames were indented with spaces that are invisible in the app's font. The box is also twice as tall, since at its old height a single group heading and one filename filled it.
- A file another program is holding open is now reported as exactly that when a Move cannot shift it, rather than as a generic Windows file error. The Delete side has always told the two apart; Move had been treating a held-open file and a disk fault as the same thing, so the one failure you can fix by closing the program looked like the one you cannot.
- Per-file failures during a Move or Delete are now recorded in `crash.log`. The app had been categorising the failure for the screen and discarding everything else, so a Move that failed left nothing to diagnose it by afterwards. A batch logs its failures in full up to twenty, then keeps logging any cause it has not already seen and counts the repeats, so a run where every file fails for one reason cannot bury the rest of the log under thousands of copies of it.
- The Move confirmation now tells you when the folder you have picked is on the same drive as the files. That move is a rename: nothing is freed until you delete the copies from the folder yourself, and InstallerClean is a free-space tool that knew the answer and said nothing. Choosing `C:\Backup` gave you "1.28 GB moved" and no more free space than before.
- Launching InstallerClean while it is already running now brings the running window to the front. It used to say "InstallerClean is already running" and leave you to find it, which is no help at all when the window is minimised or behind the browser. The message is still shown for the case with no window to raise: a command-line or scheduled run holds the same lock.
- The window a second launch brings forward is the one running in your own Windows session. On a machine with fast user switching, a terminal server or a reconnected remote desktop, a copy running in someone else's session sits on a desktop you cannot see, so a launch from yours gets the "already running" message rather than a raise nobody would witness. The single-instance lock is a separate thing and still covers the whole machine, because the folder it protects is shared by every session.
- An installed program's claim on its own cached installer can no longer be lost to a damaged patch record that points at the same file. Which way it went had come down to the order Windows happened to list the two programs in: read one way the file was correctly kept, read the other it was offered for removal, and the same machine could go either way.
- A scan that cannot read every installed program's Windows Installer records now keeps all superseded patches rather than offering them, and says so on the summary. Whether a superseded patch is still needed is a question about every installed program that might hold it, so a scan that could not ask them all has no business answering it; finding orphaned files, which is what the app is mainly for, is unaffected and the summary says that too.
- A cached installer is no longer offered for removal because Windows failed to say which program owns it. Asking Windows where a program's installer is cached can fail on a damaged registration, and a failed answer was indistinguishable from a program that simply has no cached installer: either way the program's claim on the file went missing, and the file could be offered for removal by a scan that reported itself complete. The two are now told apart, and a scan that meets the first keeps every superseded patch back and says so on the summary, exactly as it already did when Windows failed to list a program at all.
- A scan whose two halves do not add up is now refused rather than acted on. If everything Windows still lists has gone from the cache folder while every file in the folder is one Windows has never heard of, the cross-check has broken, not found a folder full of removable files, because no working machine looks like that. Rather than offer the whole folder for removal on a reading it cannot trust, InstallerClean stops and says the scan could not be relied on.
- A scan is also refused when both of the app's readings of the Windows Installer records come back short at once. InstallerClean reads those records twice, once by asking Windows and once straight from the registry, and the second reading is what keeps a program's cached installer off the orphaned list when the first one loses that program. If both are hitting errors, that recovery can no longer be counted on, and neither reading can say what the other missed. The app now stops and says the records could not be read, rather than offering a shorter list it cannot stand behind.
- Two scan failure messages no longer report a failure that never happened. If Windows Installer never signals the end of the list of installed programs, or of one program's patches, InstallerClean stops rather than work from a list with no end, but it borrowed the wording of a different failure to say so: that the list had been abandoned after 10,000 consecutive failures, with a last error code of 0, which is the code for success. Each of the two now has its own message, describing the list that never ended.
- InstallerClean now looks only at the top level of `C:\Windows\Installer`, not inside its subfolders. Windows records where it cached an installer only at the top level, so a file in a subfolder is one Windows was never asked about and InstallerClean has no basis to call unneeded. Files that a third-party installer had dropped in a subfolder, which earlier versions would have offered for removal, are now left alone; the patch engine's `$PatchCache$` subfolder, which earlier versions already skipped by name, is covered by this as a matter of course rather than by a special case.
- Links inside a sentence are now underlined at rest, not only when the pointer is over them or the keyboard has moved to them. Set apart by their colour alone, they were below the contrast a link needs to be distinguishable, so a reader with reduced colour vision could not tell they were links; the underline is a cue that does not depend on colour.
- Links now brighten when the pointer is over them, or when the keyboard reaches them. Hovering one had changed nothing: the hover colour and the resting colour were the same shade of indigo, having met when the resting one was lightened to give links the contrast they need to be read comfortably. Links rest at that same colour still, and now brighten to a lighter one.
- Renamed the theme tokens that named something other than what they did, and corrected the comments describing them. The radius named for the card shaped the dialog boxes while the card itself used a different one; the thickness named standard was used by nothing but focus rings; a fill documented as being for badges and dividers was the title bar's pressed button and nothing else; and two reds wore the name of the shade next to them. Nothing on screen changes: every name now points at the value it always pointed at.
- On the main window, Delete is now the emphasised button and Move the quieter one, the reverse of before, and Delete sits on the right of the pair, where both confirmation dialogs already put their main button. InstallerClean only ever offers files that are safe to remove, and only ever to the Recycle Bin, so deleting them is the main thing the app is for; Move is there for anyone who would rather set a copy aside first. Delete is still the first of the two Tab reaches, so the keyboard now arrives at the right-hand button before the left one, and neither is the reflexive-Enter default.
- A comments pass covered every source, test and tooling file: each comment found asserting something the code does not do was brought back in line (the code was right in every case), here-and-now phrasing and internal reference numbers were replaced with the reasons themselves, and receipts stay where a claim needs one, down to the exact Win32 and Windows Installer contract wording. It also caught one test asserting a superseded ordering claim and one guard whose failure message needed rewording.
- InstallerClean is now licensed under Apache 2.0 rather than MIT, everywhere the licence is named: the LICENSE file, the About window's licence link, all sixteen READMEs, the store metadata and the package manifests. Nothing changes about the app being free and open source; Apache 2.0 additionally spells out that no rights to the InstallerClean name are granted and that a modified redistribution must say it was changed.
- The window listing files that are safe to delete opens taller, so the longest details a real file carries, the ten-line signing identity on an Adobe patch, now fit the right-hand pane without scrolling it. A rarer, longer entry still scrolls, as every pane does.
- The note about old patch registrations whose files have already gone reads in plain language now. It used to say "3 stale MSI entries detected (files already gone from disk; InstallerClean doesn't unregister them)", developer vocabulary on a window meant for everyone; it now says Windows still lists them, that this is harmless and that there is nothing you need to do.
- The command-line tool's `--help`, and the error it shows when `/m` is given no path, now explain that a default move folder saved in the app is a per-user setting. A `/m` with no path falls back to that saved default, which a scheduled task or a service account such as SYSTEM does not have, so a run under one of those fails every time; the old message told you to set a default in the app, which does not help there. Both now say to pass the path explicitly.
- The README's command-line section now carries a worked Task Scheduler example for running a clean on a schedule: one runnable `schtasks` line that invokes `installerclean-cli` under SYSTEM with an explicit move destination, the form an unattended run needs.
- The screenshot galleries, in all sixteen READMEs, now include the delete itself running: the progress overlay with its count, the file in hand and the Cancel button, over the dimmed main window. The gallery used to jump from the confirmation dialog straight to the finished result, so the one screen showing that a clean-up can be stopped part-way was the one screen missing.
- Every push now compiles the installer, bundling the same self-contained command-line tool the released setup carries rather than the lighter build that needs the .NET Desktop Runtime installed. The setup is built by a single tool, on one machine, at release time, so a mistake in the installer script, or in one of the three community language files bundled with it, could only be found by the person about to ship it, on the day they were shipping it. It is now built and checked on every change.
- A known-vulnerable package now fails the build. The dependency audit that has run on every push since 1.5.2 could not fail anything: the command it used reports a vulnerability and still exits successfully, and each of the four checks was marked "carry on regardless" on top of that, so the audit only ever spoke to whoever went looking in the log. It is now a hard stop, and it covers the packages InstallerClean's own dependencies pull in, which is where a vulnerability actually arrives.
- Dependabot's NuGet update PRs now regenerate all four `packages.lock.json` files on the update branch before CI checks them. Dependabot rewrites only the lock of the project whose package it bumped, but a package that lives in the shared Core library is recorded in the GUI, CLI and test locks too, so leaving those three behind left them contradicting the bump and failed the locked-mode restore CI runs, turning an automatic dependency update into a red build that had to be finished by hand.
- After that lock-file regeneration commits to a Dependabot update branch, CI and the CodeQL analysis are now triggered again on the new commit, so the required checks actually run against it before the update can merge automatically. GitHub starts no workflow run for a commit a workflow itself pushed, so those checks had nothing to run against on the regenerated commit and the update would otherwise have waited on them for good.
- The CodeQL security scan now runs the extended query set alongside its default one, trading more findings to look through for higher recall. That is the right side of the trade for an app that runs elevated and reaches COM, native Windows APIs and the registry.
- The `winget` package now carries the exact registry key Windows records InstallerClean under, so `winget upgrade` matches an installed copy by that key rather than by reading a display name that has the version number inside it. Anyone who installed through winget gets a more reliable upgrade; nothing about the app itself changes.
- Recorded, in the installer script, that the command-line tool's Windows event-log source is left behind on uninstall, and why removing it would be wrong: Event Viewer renders an entry's description through its source, so deleting the source would turn every audit entry the tool has ever written into "the description cannot be found".
- The check that verifies every translated resource file against the English original now also inspects the placeholder count of the optional extra plural forms some languages add. A language whose grammar needs a count form English does not have (Russian's separate two-to-four form, for one) supplies it as an entry that lives only in that language, which the check did not look inside, so one referring to a number the sentence never provides would have thrown at the moment that count came up rather than being caught here. It is now validated like every other translated string.
- A resource string that no code or screen references any more is now flagged on every push, rather than sitting on in the English file and its fourteen translations as dead weight. Two are kept on purpose (the part-way-through Move and Delete status lines, between uses since that copy moved to the completion screen), and the check knows them; any other unused string fails it.
- The installer's list of languages and the app's list of languages are now checked against each other on every push. Nothing connected the two before, so the fifteen matched only by hand and a sixteenth language added to the app but forgotten in the installer, or the reverse, would have shipped mismatched. The check also confirms each installer language carries its full set of welcome, finished and "still running" wizard messages, so none can drop back to English part-way through setup.
- Added a check that flags any translation still carrying the English text for a string meant to be translated. When a string is added or its English reworded, it stands in English across all fourteen languages until the per-language translation pass reaches it; the check surfaces exactly those keys on every push, so that pass cannot be quietly skipped before a release. A short list of keys every language keeps in English on purpose (the product name, size and time units, a handful of loanwords, the machine-read command-line log lines) is exempt.
- Added a check that no French string uses a plain space before ! ? : ; where French typography needs a narrow no-break space. That narrow space is easily lost to an ordinary one when a line is edited, which reads wrong and can drop the punctuation to the start of the next line; the check catches it on every push.
- The pass that clears out empty leftover directories in `C:\Windows\Installer` after a Move or a Delete now goes through the same filesystem layer as everything else that touches your files. The app does exactly what it did before; the difference is that the test suite no longer reaches past that layer and prunes the real `C:\Windows\Installer` on whichever machine is running the tests, which it had been doing on every run. The pass now has tests of its own, against an in-memory filesystem, covering the nested-tree collapse and the cancelled case.
- The rule that decides what happens when two Windows Installer records name the same cached file is now one piece of code with tests over every combination, rather than three places that each did their own thing. One of those combinations is load-bearing and had only a comment guarding it: the registry sweep that backs up the main query reads the same records the query just read, so allowing it to overrule a verdict would have quietly switched off superseded-patch detection entirely while every test still passed. A test now fails if anyone does.
- The tests covering the Windows Installer query no longer read the registry of whatever machine is running them. They had always supplied the Windows Installer half of the query themselves while letting the registry half read the real machine, which was harmless while that half could only add to the answer. It decides more than that now, so the tests supply both halves and a result no longer depends on the machine the tests happen to run on.
- The tool that flags a changed string for re-translation used to sign off by saying it had logged the change in the pending list. It never has: that list is written by hand, and the sign-off was telling whoever ran it that a step they still owed was already done. It now says what it does and does not do.
- That same tool now routes a new command-line string into the Russian generator's own command-line list. Russian is the one language whose generator strips every command-line key out of the English base and rebuilds them from a list of its own, so a new key added the ordinary way was stripped straight back out, never reached the Russian file and failed the parity check with nothing on the face of it to say why.
- Picking a language explicitly, rather than letting InstallerClean follow the Windows display language, is now covered by tests: the strings it resolves, the number formatting it applies and the plural form it selects. All fifteen languages travel that path when chosen from the language menu, and none of it was tested before.
- Improved code comments. The settings lock, the pending-reboot gate and three service contracts each described behaviour the code does not have: the lock was justified by a window-size setting removed back when the Details windows stopped remembering their size; the reboot gate was described as checking only where a queued file rename comes from when it checks where one goes as well; and three methods promised they never throw while all three deliberately report a cancellation by throwing. The note above one error message promised a second placeholder for the underlying Windows error message, which the string does not have and must never have: read as the contract by a translator or a contributor, it invited exactly the leak the app is built to avoid, because an error message from an elevated process can carry a path out of another user's profile. Separately, the summary on the helper that builds InstallerClean's in-app README links listed two of the three page anchors it actually builds, understating an invariant a later reader or translator would rely on.
- Corrected a misleading comment in both application manifests. It claimed the version stamped in the manifest tracks the one in `Directory.Build.props`. Nothing enforces that, and the value has no effect either way: Windows ignores the manifest identity for trust, and InstallerClean reports its version from the assembly. The comment now says the value is inert.
- Removed four interface identifiers from the shell interop layer that nothing used.
- Removed a cancellation path from the opt-in report send that could never run, along with the comment describing it: the send is given no way to be cancelled, and the one it claimed to handle cannot happen. A timed-out send was already reported as a timeout and still is.
- The two per-user install-context flags in the Windows Installer interop layer had been given each other's values: `msi.h` numbers the managed context 1 and the unmanaged one 2, which reads backwards, and they had been written the way round the names suggest. Nothing reads either of them by name, so the app has always behaved correctly, but the first piece of code to test for one would have been wrong inside the part of InstallerClean that works out whether a cached file is still registered.
- The logic that decides whether a cached file is still registered with Windows Installer now has unit tests. The four Windows Installer calls it depends on sit behind a seam a test can stand in for, so the failure paths that classify a file (a refused list, an identifier that comes back empty, a run of failures, the merge of a patch's verdicts across several installed programs) are checked on every build. None of them could be tested at all before, which is how several of the edge cases fixed below went unnoticed.
- The completion screen's states are now under test: each heading, the count line on a full run and on a cancelled one, the lines dropped when a run achieved nothing, the pluralised group headings, the hyphen-indented filenames and the reset that leaves none of it showing on the next operation. The end-to-end Move test pins the same shape from the scan through to the screen, and the crash-log budget for per-file failures is covered through a stand-in sink, so pinning it cannot append two dozen entries to the real log on whatever machine runs the tests.
- Added a single-string form of the internal count-formatting helper for the cases where one sentence covers both "one" and "many". Several places passed the same string to it twice, which reads like a slip and invited being tidied down to a single argument, which would have quietly dropped the per-language singular and plural handling. The new form takes one string and cannot be misused that way, and the seven places that passed one twice, five in the command-line tool and two on the main window, now take it.
- The logic that wraps a long folder path neatly, by marking each backslash with an invisible character, and that splits the confirmation and completion sentences around the destination and the "is it safe?" link, moved out of the four windows that used it into a Core helper with tests of its own. The screens look and behave exactly as before; the difference is that the wrap marker, which once shipped written out as six literal characters in a path, and every split point, are now covered on every build. There was no way to test any of it while it sat inside a window's constructor.
- Corrected two more comments. A build-file note claimed InstallerClean produces no translation satellites "because no other culture has a resx", which stopped being true when the fourteen satellites shipped in 2.0.0; it now describes the real set and how a fifteenth would be added. The note on the command-line tool's event-log writer excused disclosing a path as "the calling user's own input"; it now says honestly that the hard-error entry also carries the crash-log path, which embeds the elevating account's profile name, and why that trade is accepted for an unattended run.
- The theme's two main button colours are now named for how they look rather than for a rank: the slate fill was called "primary" and the louder indigo one "secondary", so the names told a reader the opposite of what shipped. They are now Standard and Accent, and which button a screen leads with is settled at that button alone, so moving the emphasis between two buttons can no longer leave a colour name contradicting the screen. Nothing on screen changes.
- Every push now checks the app's theme resources: that every colour, style and spacing value a screen asks for is one that exists, and that every value defined is used by something. A name nothing defines compiles perfectly cleanly and fails only when a user opens that particular window, so a renamed value with a single screen left behind could reach a release with no build, test or check before it able to notice.
- Every push now checks that the list of names the code uses to reach the English text is in step with the English text itself. That list is generated by hand from the same file, so the two can drift, and a name left behind after its text is removed still compiles: the app then shows the name itself, `Body.MainExplanation.Lead`, where the sentence should be. It was the last pair in the project able to drift without a check, and it is checked before the translation gate rather than after, since that gate is deliberately red for the whole window between an English string landing and its translation round, and a red gate hides everything behind it.

### Fixed

- The safety-net retry for an oversized user SID during product enumeration now sizes its buffer to Windows Installer's documented contract, one character more than the reported length, which excludes the null terminator. The old exact-size retry was one character short and would itself have been refused; real SIDs never come near the first call's buffer, so the branch had never fired.
- Closing InstallerClean in the middle of a move or a delete no longer cuts the job off part-way through a file. The window closed instantly and the process went with it, while the work was still running: a move to another drive is a copy, so it could leave a half-written file sitting in your destination folder, looking like a good copy of something that is still in the cache. Alt+F4, the X and the system menu now stop the batch cleanly, finish the file in hand and close the window once it has let go.
- InstallerClean now checks that no Windows Installer operation is running at the moment you press Move or Delete, not only when it last scanned. The gate that holds a clean-up back while an install is in flight was sampled once per scan, so an install that began while the window sat open, or while a confirmation dialog was up, was invisible and the clean-up could run alongside a live installer transaction, the one thing the gate exists to prevent. It now re-checks immediately before acting, and in the rare instant where an install takes the installer lock just as the operation begins, the operation stops before it touches anything and shows the warning rather than a result. The command-line tool already re-checked at that point.
- Immediately before a Move or Delete, InstallerClean re-checks each file it is about to remove against Windows once more and leaves out any a program has started needing again since the scan. This closes the last gap the in-progress check cannot see: a patch that was superseded when you scanned but has since had its replacement removed, so Windows wants the older one back. Any files kept back are reported on the completion screen, and if the re-check itself cannot be run, the operation stops rather than act on files it could not confirm.
- Cancelling a Move or a Delete part-way through now shows you a summary of what it did before it stopped, on the same completion screen a finished run uses, rather than a one-line status that dropped the list of any files it could not process. It reads "Moved 3 of 10 files before you cancelled", carries any per-file errors, and, because the run did not finish, is left out of the opt-in report so the shared figures keep meaning completed clean-ups. A cancelled permanent delete says so plainly and never claims the Recycle Bin, which those files did not reach.
- Switching language while a Move or Delete is being prepared no longer interrupts it. Changing language restarts the app, which ends a running operation the same way closing the window mid-move would; it was already blocked while the operation's overlay was on screen, but not during the brief moment beforehand while the destination is checked. That gap is closed: the switch is now refused for the whole operation, from one place that knows whether any scan or clean-up is in flight.
- The Cancel button and Esc no longer go dead for the rest of the session after you cancel a move while it is preparing the destination folder. Cancelling there left InstallerClean believing a cancellation was still outstanding, so the next Move or Delete opened with its Cancel button greyed out and Esc doing nothing. Cancelling during that step is also honoured properly now: it used to be possible to cancel, be asked to confirm the move anyway and only then have it stop.
- Confirming a delete no longer freezes the window while the Recycle Bin is checked. InstallerClean tests the Bin by actually recycling a throwaway file through the Windows shell, which is slowest in exactly the case the test exists for (a Bin that is switched off, full or broken), and it was doing it on the thread that draws the window, so everything sat dead between the confirmation dialog closing and the delete starting.
- A move destination on a mapped drive or a network share that has gone away no longer freezes the window. Every check InstallerClean makes on a destination (that it is not inside the Installer cache or a Windows system folder, that it can be created and written to, which drive it is on, how much room is left) can sit for the full network timeout, and three of the five were running on the drawing thread. They now all run behind the cancellable overlay, so a dead share leaves you with a window you can still use and a Cancel that works.
- Move and Delete are no longer clickable during the first fraction of a second of a scan. The scanning overlay only appears if a scan runs longer than 200 milliseconds, and the flag that raises it was also the one disabling the two buttons, so for that moment a delete could start against the previous scan's results while a fresh scan walked the same folder, leaving the counts on screen to whichever of the two finished last.
- A move destination typed or picked in the last fraction of a second before the window closes is now kept. The save deliberately waits for you to stop typing, and closing inside that wait threw the edit away rather than writing it, so the box came back empty (or holding the old folder) next time. Switching language, which restarts the app, lost the same edit.
- Two parts of InstallerClean writing to the crash log at the same moment no longer overwrite each other's entry. Entries are appended in a single step now, so the log keeps all of them. It is the only trail there is when something goes wrong, and several parts of the app do nothing but leave a note in it.
- Clicking Send on the opt-in report no longer pauses the window while the "already sent" mark is recorded, which on a redirected or roaming Windows profile is a write to a slow disk. It is written in the background now, and closing the app waits for it, so a close straight after the click cannot lose it and ask again next time.
- InstallerClean can no longer hang on exit waiting for the Recycle Bin. If the Windows shell stops answering (a wedged shell add-in, a drive that has gone away), the app now gives it ten seconds, records the fact in the crash log and exits anyway, instead of leaving a process with no window that only Task Manager can end. Shutting down mid-delete also reports the Bin being gone as the plain "it is gone" error the delete already knows how to handle, rather than a failure it does not.
- After a Move, the completion screen shows the destination folder as you typed it, `D:\Backup`, instead of mangling it at every separator, `D:\u200BBackup`. Long paths are marked at each backslash with an invisible character so the line wraps neatly, and the marker was being written out as six literal characters instead. This is the one screen that tells you where your files went, and the path it shows is the one you would type to get them back, so it has to be the real path. Present since 2.0.0.
- Two parts of the app that could not be tested at all now are. The first writes the opt-in report to disk, and its size limit, its all-or-nothing write and its tidying up of half-written files had no test because it could only ever write to one fixed place in your own Windows profile; it can now be pointed at a scratch folder, and all four behaviours are covered. The second is the lock InstallerClean takes so a Windows install starting mid-clean-up waits rather than writing to the folder underneath it. Only a stand-in had ever been tested, so the one answer the real thing has to get right, whether something else already holds the lock, had never been checked against a real one. Both now run on every push.
- The Browse button now opens the folder browser at the folder the Move box already names, rather than at the Windows default every time. Browse is pressed to change a destination more often than to set the first one, so it had been making you navigate back to a path that was on screen in front of you. A box that is empty, half-typed or naming a folder that is no longer there still starts at the default.
- The fourteen public translation review pages are now checked against the app's own text on every push. Those pages are generated from the translation files and are what the project asks native speakers to read through, but nothing verified they had been regenerated, so they could go on showing lines the app no longer has, which is exactly what happened before this release. A push that changes a translation without refreshing them now fails, naming each page and how far out of step it is.
- Building the installer by hand without telling it which version it is building now fails outright, instead of stamping whatever version number was last typed into the script. That fallback had to be updated by hand at every release and its own comment claimed it tracked the current release, which it had already stopped doing; a setup built from it would have told Add/Remove Programs a version its contents were not. The official builds have always passed the real version in, read from the one file that holds it.
- The line saying how many files a clean-up could not handle no longer reads "1 of 1 files could not be moved" when a single file fails. The word "files" agreed with the wrong one of the two numbers in the sentence, so it was always plural however few files there were. It now says "1 of 1 could not be moved", with no noun to disagree; the same applies to the Delete version of the line. Fourteen languages carry their own version of this sentence and every one of them was right already and keeps its wording: seven put the noun beside the number that governs the form, and in the other seven the noun does not inflect for number at all.
- A Move that does not go ahead no longer leaves an empty folder behind at the location you typed. InstallerClean creates that folder and writes a test file into it before asking you to confirm, so it can tell you there and then whether the folder is writable and whether it is on the same drive as the files. Every way the move can then stop used to leave the folder sitting there, before the confirmation (cancelling at it, cancelling while the folder is still being checked, the folder turning out not to be writable, there not being enough room on the drive, a Windows install starting while the confirmation was open) and after it (the last-moment re-check failing, that re-check keeping every file back, a cancel before the first file, a Windows install taking the installer lock, and the move service's own write check failing). All ten now remove it again, and only ever when InstallerClean created it that moment and nothing has appeared in it since. The test file written to check the folder is writable is also cleared away when you cancel in the instant between it being written and removed, which used to leave both the file and, because a folder with something in it is not removed, the folder around it. A folder that already existed is never touched, and a stop that happens once files are moving leaves the folder alone, since by then it holds them.
- Removed a total size that a delete carried around and never used. It was computed before the last-moment re-check that can hold files back, so it counted files a delete might not touch; every size the app actually reports is worked out afterwards, from the files that survived that check. Nothing on screen changes, and there is now no wrong number sitting there for a later change to pick up by mistake.
- Typing in the Move box no longer leaves stray cancellation entries in `crash.log`. Each keystroke cancels the pending save of the one before it, and a keystroke landing in the instant between a save's wait finishing and the save itself starting turned that cancellation into an error the app never looked at, which Windows then reported into the log at some arbitrary later moment. Nothing was ever lost, and the log now says so by staying quiet.
- Switching language no longer risks the restarted app opening on the previous move folder. InstallerClean waits a fraction of a second after you stop typing in the Move box before saving it, and a language switch restarts the app, so a folder typed or picked in that fraction was still unsaved when the new copy started reading the settings back. The old copy finishes writing before the new one starts now, rather than the two racing.
- A Move that stops part-way with an error now updates the file count and size on the main window, instead of leaving the figures from before it started. The two ways a Move can stop like that, the destination folder being swapped for something else while the files are going into it and any failure the app cannot account for, both left the window claiming files were still in `C:\Windows\Installer` that had already left it, and the list of them showing the same. It corrected itself on the next scan.
- The window no longer sits still with both buttons greyed while InstallerClean checks the Recycle Bin after you confirm a Delete. That check writes a file, sends it to the bin and clears it again, which is quick on a healthy bin and slow on a sick one, the only case it exists to catch; on a slow one the app now says what it is waiting for, and offers Cancel, after the same fifth of a second the scan and the Move check already wait before showing anything. Cancel stops the delete happening rather than the check itself, which Windows gives no way to interrupt.
- A Move or Delete you cancelled before it got to its first file no longer tells you where to find the files or how to put them back. Cancel a clean-up after the first few files have failed and the summary would invite you to empty the Recycle Bin, restore from it, or copy files back out of the folder you had chosen, all about files that never arrived anywhere. It now says none of that, the same as a Move or Delete that failed outright already did. The heading still reads as it did, because stopping something yourself is not a failure.
- Cancelling a scan no longer writes a crash-log entry. The scan caught its own cancellation and recorded it as though it were a fault, so in the log a scan you stopped looked exactly like a scan that broke.
- A failure in the silent rescan that runs after a Move or a Delete now leaves a crash-log entry. That rescan is what refreshes the counts on the completion screen, so when it fails the screen keeps the counts from before the operation; it previously failed with no trace at all, which left a report of "the counts were wrong after it cleaned up" with nothing to work from.
- The command-line tool no longer abandons a run because it could not set its output encoding. It pins its output to UTF-8 as its first act, so that text in a language with non-ASCII characters survives being redirected to a file, and Windows refuses that on a process launched with no console at all, which some remote-management agents and service wrappers do. The refusal was ending the run before it scanned anything, every run, for the sake of an encoding whose only job is to make output nobody is reading look right. It is now carried on past.
- A failure in the command-line tool before it begins scanning, a name or permission clash acquiring the lock the tool shares with the app, now leaves the same trail as any other failure: a crash-log entry, one Windows Application event-log entry and the tool's documented error exit code. It used to fall through to the runtime, which printed the raw exception, message and stack trace, to the error stream and exited with a code no monitoring tool recognises. Running as administrator, that raw message can carry a path out of another user's profile, so a scheduled task captured it to disk with no record of what actually happened.
- The "N.N seconds" scan time on the all-clear screen now takes its decimal separator from the language InstallerClean is displaying, as every other figure in the app already did, rather than from the Windows regional setting. The two agree on every path the app takes today, so nothing changes on screen; it was the one figure that could have shown a French screen an English decimal point.
- A move destination given as a plain folder name rather than a full path, `backup` instead of `D:\Backup`, is now turned down before InstallerClean touches anything. The move itself was always refused, but only after you had confirmed it, and by then InstallerClean had created the folder and written a test file into it, wherever `backup` happened to point relative to the place the app was started from. The refusal now comes first, so nothing is created anywhere, which is how the command-line tool has always done it.
- A scan that Windows Installer stops answering while it is listing an installed program's patches now reports a patch failure, rather than "Windows Installer refused to list products", which pointed whoever read the message, or was shown it, at the wrong part of Windows entirely. The scan asks for each product's patches separately from the product list, and only the product list had a message of its own; the new one is carried in all fifteen languages.
- A settings or opt-in report write that fails no longer strands a temporary file in InstallerClean's own folder. Both are written to a temporary file and then renamed into place, so a disk that filled up or a profile that was locked at the wrong instant left the half-written file behind, and every later attempt made a fresh one under a new name, so nothing ever cleared them up. Both writes now tidy up after themselves on every way out.
- While a Windows Installer operation is in progress, the main window no longer tells you to move or delete files with the buttons to do so greyed out. It already showed a banner explaining the hold, but the lead above it still read "The unneeded files below are safe to delete" and the line beneath still said to move or delete them, above dead buttons. The lead now says the files can't be cleaned up right now, the instruction to act is removed, and both come back when the operation finishes and you Re-scan.
- The main window's lead now reads "Any unneeded files below are safe to delete" rather than "The unneeded files below", so the same sentence reads correctly whatever a scan finds, a count of zero included.
- A startup scan that fails now opens the main window with the reason shown and Re-scan ready, instead of vanishing. It used to hand the failure to the launch code, which, because the "access denied" reached it as an ordinary Windows access error, told an already-elevated user to run as administrator and then exited, so a diagnosis like "your installer database looks empty" flashed by and the app was gone. The window now stays, shows the tailored message where the scan summary would go and puts the focus on Re-scan. A Re-scan that fails and the startup scan now describe the failure the same way, from one place, rather than each having its own wording.
- Cancelling the scan InstallerClean runs at startup no longer leaves you looking at a clean bill of health for a scan that never ran. The window painted "0 unneeded files to clean up" and "0 files still needed", which is exactly what a genuinely clean machine looks like, with nothing anywhere saying the scan had been cancelled. It now says nothing has been scanned yet, points at Re-scan and shows no counts until there are some.
- The Details window opened by the "registered file is missing" warning now opens on the file that is missing. The warning ends "Open Details for what to do", and what to do is a note that only that file's row carries, but the window opened on the first row of an alphabetical list of every installed program and left you to hunt for a small amber triangle somewhere in it. The missing one is now selected, scrolled to and focused when the window opens.
- The `--version` line of `installerclean-cli --help` now lines up with the rest of the help screen. It was the one ragged line in the only screen the command-line tool prints for a person to read, in seven of the fifteen languages.
- The manifest the download sites read (`pad.xml`) pointed at a screenshot that is not there. Giving every language its own screenshots in 2.0.0 moved the file, and the manifest kept the old address, so every site that has refreshed its listing since has fetched a dead image for the one field that decides what the listing looks like.
- The same manifest still described InstallerClean as an English-only program, six weeks after it started shipping in fifteen languages. It now lists them.
- The same manifest has never told those sites what changed in a release. The line was meant to be written from the release notes and was looking for a heading style the notes have never used, so it has fallen back to the words "See release notes." on every release since the file was added. It now carries a real summary, and a release with nothing to say there stops rather than shipping the placeholder.
- The same manifest was the only place in the project spelling the studio "No faff". The installer, the executables' own file properties and the Add/Remove Programs entry all say "No Faff", so the publisher name a download site printed did not match the one Windows shows.
- Several of the translated interfaces had lost a word that carried part of what a message meant. The Spanish and Brazilian Portuguese count of files you can safely delete no longer drops its leading "Found"; the Brazilian Portuguese completion line that says where files went reads "moved to" again, and now differs for one file and for many rather than the identical clipped "in" it used for both; the Ukrainian failure line in `--help` says all files failed rather than just "all files", and a note further down reads as a full sentence again instead of trailing off without a verb; the Turkish completion badge says space was freed rather than opened, and the Turkish delete-safety reassurance finishes the "never" it had left hanging; and the Indonesian explanation of what InstallerClean lists uses a verb that can properly take an object.
- Punctuation in four languages now follows each one's own conventions. French uses its narrow no-break space before the "!" in the two "you can copy them back" hints, as it already did in its other exclamations; Russian restores the dash its "X is Y" sentence needs before "это" in a command-line note; Japanese drops a stray space and swaps full-width brackets for the half-width ones the rest of the Japanese uses, across three window summaries and a pending-reboot message; and Chinese quotes an unknown command-line argument in its own full-width quotation marks rather than straight ASCII ones.
- The Indonesian tooltip on the tip-jar button now keeps the formal register the rest of the app uses, in place of a colloquial line that stood out against it.
- The Japanese wording for a file another program is holding open now sits in the register the rest of the Japanese uses. Its three in-use sentences rendered "nothing can move it just now" with a bookish emphatic no interface writer would reach for.
- The column headings in the Orphaned and Registered file lists now brighten their text, not just their background, while a heading has the keyboard focus. Those lists let you sort by moving to a heading and pressing it without a mouse, and the focused heading was the one place a heading kept a faint grey that dropped below the contrast the text needs to stay readable, on the very control the app went out of its way to make focusable.
- The globe language menu now tells a screen reader which of the fifteen languages is the current one, marking it as the selected item. It was shown only by a small tick beside the name, a symbol a screen reader cannot read out, so someone using one was never told which language they were already in.
- The main window no longer mentions old patch registrations Windows keeps after their files have gone. Nothing could be done about them and nothing needed to be.
- The globe language menu now reads out each language's name in the language the app is running in, after its own name. A screen reader speaks only the scripts it has a voice installed for, so an English-voiced one reached Русский, Українська, 日本語, 简体中文 and 한국어 and said nothing at all for them, falling back to "item 12" and whether it was ticked. Every entry now reads as, for example, "Deutsch, German", and on a French install "Deutsch, allemand". The names come from Windows itself, so they follow whichever of the fifteen languages you are in, and the menu looks exactly as it did.
- The globe language menu now reads out the language names. Each row sets the name beside a tick column so the fifteen line up, and a row built that way leaves a screen reader with no name to announce, so it read out only the position in the list and whether that entry was the current one. The names themselves, the whole point of the menu, were never spoken.
- The Move box and the Move button now describe themselves to a screen reader in the words that are on screen. The box was announced as the "Move destination folder" and the button's spoken help sent files to "the chosen destination folder", both left from wording the app dropped when the label above the box became MOVE LOCATION, so a screen reader and the button's own tooltip had two different names for the same folder.
- The preview box in the send-report dialog is announced as the report preview. It had been the "result log preview", the name the feature goes by inside the code, which is not what the button, the dialog or the messages after a send have ever called it.
- The reassurance after a clean-up no longer promises that nothing will break. It read "you can restore them if anything breaks (it won't!)", which guaranteed something about your whole machine when what InstallerClean can actually vouch for is narrower: that it removed nothing any installed program still needs. It now reads "if anything ever breaks (extremely unlikely)", which is a confident claim rather than a promise, and still links to the reasoning behind it.
- After a Move, the reassurance now says where to copy the files back to. It said only "copy them back", which assumed you knew the folder they came from; it now names `C:\Windows\Installer`. The Delete version deliberately still doesn't name it, because restoring from the Recycle Bin puts each file back on its own.
- The main window now says how to undo a clean-up, and calls what Move leaves you a backup rather than a copy, which is what it is. Putting the files back in `C:\Windows\Installer` returns you to exactly where you started, and that holds for both buttons because a Recycle Bin restore and a move back both put the file at the path Windows recorded for it; it had been written down only in the README, so anyone who arrived from a download site was left to assume the operation was one-way.
- The Move button now works without a folder typed in first: pressing it asks where to put the files, then moves them. It used to be greyed out until the box was filled, and a greyed button is skipped by the Tab key entirely, so anyone working through the window by keyboard never reached Move, never heard it described and never got the tooltip explaining what it was waiting for. Its tooltip now says a folder browser is coming rather than asking for one to be set first.
- The hint under MOVE LOCATION, explaining that the box takes a folder if you Move rather than Delete, is now read out by screen readers. It is drawn inside the box's own styling, which put it out of reach of a screen reader, so the one line explaining why that box exists was the one line a blind user could not get. The box also no longer announces itself as a required field, which it never was.
- A patch shared by several installed programs, no longer needed by one but still in use by another, is no longer offered for removal on the strength of the order Windows happened to list the programs in. The same patch is cached once and shared, and InstallerClean kept whichever program's verdict it read first, so a file another program still needed could reach the cleanup list on what amounted to a coin toss. A file any installed program still uses is now kept.
- A superseded patch whose "can this be rolled back" flag Windows would not report is now kept rather than offered for removal. A failed read of that flag used to count as "safe to remove", which is the wrong way to fail: a patch that can still be rolled back needs its cached file, so the unknown case now leaves the file alone.
- A single unreadable entry in InstallerClean's second, registry-based list of still-needed files no longer discards the rest of that list. One corrupt key, or one folder whose permissions had been changed, used to abandon the whole fallback, and every file only that list would have vouched for was then treated as unneeded. Each entry is read on its own now, so one bad entry costs only itself.
- A scan now stops and reports a patch failure when Windows Installer refuses to list an installed program's patches, instead of treating that program's cached patch files as unneeded. The product list already stopped on that refusal; the patch list quietly skipped it, so a program whose patches could not be listed had its cached updates offered for cleanup.
- An enumeration row that comes back marked successful but carries no product or patch identifier is now counted as a failure rather than kept as a real entry. An empty identifier fails the follow-up reads and would drop that file from the list of still-needed ones, so the file it stood for would look unneeded; treating the empty row as a failure keeps it out of the reckoning instead.
- A Windows Installer enumeration that never reports its end now stops with an error rather than treating everything past its cutoff as unneeded. No real machine has enough installed programs or patches to reach the cutoff, but if one ever did, stopping is the safe outcome and silently classifying the remainder as removable is not.
- InstallerClean now reads what is in `C:\Windows\Installer` before it asks Windows which of those files are still needed, instead of the other way round. A program that finished installing in the fraction of a second between the two steps could otherwise have its freshly cached, still-needed file listed as unneeded; reading the folder first means a file cached after that point is simply not on the list.
- Every file InstallerClean is about to move or delete is now checked, at the moment it acts, that it really sits inside `C:\Windows\Installer`. The place files are moved to was already refused six different ways if it pointed back into the cache or a system folder; the file being moved or deleted was only ever in bounds as a by-product of how the list was built. A corrupt Windows Installer record that pointed a superseded patch's file somewhere else on the disk could have made that file a target; it is now refused, both when the list is built and again at the point of the move or delete, and this is checked against the real folder so nothing can spoof it.
- Delete now refuses a file that is a symlink or junction, exactly as Move already did. InstallerClean's own safety notes said both refused them, but only Move actually did; recycling a symlink removes the link rather than what it points at, so following one out of the cache is now refused on the delete path too.
- Move and Delete now hold the Windows Installer lock (`_MSIExecute`), the same one Windows takes during an install, for as long as they run. An install that starts while InstallerClean is working now waits for it to finish rather than running against the cache at the same moment, and an install already holding that lock when you press Move or Delete makes InstallerClean stop before it touches anything. This turns the pending-install check from a single glance at the start of a scan into a lock held across the whole operation.
- The command-line tool no longer reports a word from your own move folder as an unknown argument. An unquoted path with a space in it, `/m D:\My Backup`, reaches the tool split into separate words, so it was right to refuse the command rather than move files to `D:\My`, but it named `Backup` as though you had mistyped a flag. It now says an unexpected extra argument turned up and shows the quoted form to use.
- A `/d` or `/m` run cancelled with Ctrl+C still writes the same one-line event-log summary and returns the same exit code as before, now that a cancelled Move or Delete hands back what it finished rather than throwing it away. The tool reports a cancelled run from the one place the changed service would otherwise have bypassed, so nothing a scheduled task reads about a stopped run has moved.
- A `/d` or `/m` run now stops with its transient exit code, and the matching event-log entry, if a Windows Installer transaction takes the installer lock in the instant between its start-of-run check and the operation itself, rather than acting against a live install. It reports this exactly as it already reports a blocked start-of-run check.
- The command-line tool now runs the same last-moment re-check the app does before a `/d` or `/m` acts, printing how many files it kept back because a program has started needing them again since the scan. If the re-check itself cannot be run, the run stops rather than act on files it could not confirm.
- A file Windows records under an unusual spelling of its path is now matched against what is actually on disk. The still-needed list was compared letter by letter with the folder listing, so a record written with a doubled backslash, a forward slash or the long-path prefix never matched, and the same physical file was counted as still needed on one side and offered as unneeded on the other. Every recorded path is now put into one form before anything is compared. Short 8.3 names ("PROGRA~1") are the one spelling still not expanded.
- Move and Delete now refuse a file whose attributes Windows will not let them read, rather than treating it as an ordinary file. The check that keeps the app from following a symlink out of the cache answered "not a symlink" when it could not look at all, so an unreadable file went ahead to the move or the delete. It is now left in place and reported as an error, and it is not called a symlink, because that is not what an unreadable file has been shown to be.
- The same refusal now applies to a file whose real location on disk cannot be worked out. The check that every file acted on sits inside `C:\Windows\Installer` expands symlinks and mapped drives first; if that expansion fails, the check was comparing the path as it was written rather than where it leads. Being unable to prove a file is inside the cache now stops it being touched, exactly as being outside does, and a scan drops such a file from the list it offers under a `crash.log` note saying its symlink status or location could not be read, rather than naming a cause the check has not shown.
- Delete now checks the Recycle Bin is available for the drive before it takes the machine-wide Windows Installer lock rather than after. The check writes nothing and can refuse the whole batch, so it has no business running inside a lock that makes every installer on the machine wait.
- Fixed a Windows Installer refusal being tolerated instead of stopping the scan, in the one case where it arrived from the retry that asks for a bigger buffer rather than from the first attempt. Nothing has ever hit it (the retry only runs for an unusually long account identifier), but a refusal is a refusal wherever it comes back from.
- Improved code comments: the machine-wide cost of holding the Windows Installer lock is now recorded beside both places that take it, along with why closing the window during an operation waits on the flag it does; why the re-check before a clean-up re-reads everything rather than just the files in hand; and the reasoning behind each of the safety changes above.

## [2.0.1] - 2026-07-07

### Fixed

- The completion screen no longer shows the Recycle-Bin-only "Empty it to actually reclaim the space." line after an all-clear rescan, a Move, or a permanent delete that followed a Recycle-Bin delete earlier in the same session. The 2.0.0 reword that added that line cleared it after setting the restore line's text rather than before, on three of the four completion paths; the on-screen text is rebuilt at the moment the restore line changes, so it picked up the previous operation's leftover hint instead of a blank one. All four paths now clear it first, matching the one that already did.

## [2.0.0] - 2026-07-06

### Added

- coolvitto contributed, entirely unprompted, a complete Japanese translation of InstallerClean, written from scratch (#41): every window, dialog, tooltip, button and screen-reader label, and the command-line tool's text besides. InstallerClean can now be displayed in Japanese, applied automatically on a Japanese-language Windows or picked from the main-window language menu.
- InstallerClean's interface and command-line tool have been translated into twelve more languages, all live in the app: Simplified Chinese, Russian, Spanish, Brazilian Portuguese, Korean, French, German, Polish, Turkish, Indonesian, Vietnamese and Ukrainian. They are machine translations, openly labelled as such, shipped rather than held back until a native speaker could check each one. Each was done from the English with its own grammar handled rather than calqued: French carries full French typography (the narrow no-break space before `:` `;` `?` `!` and `%`, and guillemets); German, Russian, Polish and Ukrainian agree their counts, Polish and Ukrainian each on their own one/few/many rule and Ukrainian's "few" in the nominative plural rather than Russian's genitive singular; Turkish, Indonesian and Vietnamese leave the count uninflected; and each takes the register and the Windows terms (the Recycle Bin, the Event Log and the rest) its own platform uses. Where the languages stand:

  | Natively done | Live machine-translated | README only \* |
  | --- | --- | --- |
  | English · Italian · Japanese | Simplified Chinese · Russian · Spanish · Brazilian Portuguese · Polish · Turkish · Korean · French · German · Indonesian · Vietnamese · Ukrainian | Arabic |

  \* README only means the README is translated while the interface stays English. Arabic is here on that basis, because a right-to-left interface is a separate piece of work.

- The command-line tool (`installerclean-cli`) now follows the Windows display language for its human-facing output, the help, progress and error text, in all fifteen languages. The machine-readable parts stay English on every machine, by design: the Application event-log entries that monitoring (RMM) tools match on, and the scriptable "N errors:" line. On a Windows language InstallerClean does not ship, it falls back to English. The Japanese command-line text is coolvitto's (#41); the Italian is built on the draft bovirus contributed (#34).
- The installer's wizard now runs in all fifteen languages: twelve use Inno Setup's own official translations, and Simplified Chinese, Vietnamese and Indonesian use community translations bundled with InstallerClean, matched to the installer engine's version.
- Each machine-translated language now has a generated review table (English beside the translation, grouped by where it appears in the app) so a bilingual reader can spot anything that reads wrong, and a "Translation review" issue template to report it. Italian and Japanese, both already native-reviewed or native-written, get their own tables too.

### Changed

- The plural engine now selects the count form by the display language's Unicode CLDR category (one/few/many/other) rather than a fixed singular-vs-plural split, so a language that needs more than two forms renders its counts correctly, for example Russian's 2-4 "few" form now shows as "2 файла", not "2 файлов". A language supplies its extra forms as optional Plural.<Noun>.Few/.Many satellite keys, read by name so they touch no other language.
- The "N registered files" count lines now agree at a count of 1 in Italian ("1 file registrato", not "1 file registrati"), Spanish and Russian ("Найдено 1 зарегистрированный пакет."), where the adjective previously stayed plural regardless. A count of 1 there is effectively unreachable (a machine has dozens of registered files), but the agreement is now correct on principle, via an optional per-language one-form override key that leaves the neutral strings and the other languages untouched.
- The Move and Delete progress lines now route through the plural engine as well, so a language can give them count-correct forms where its grammar needs them (German shows "1 Datei wird verschoben" against "5 Dateien werden verschoben"); a language without a special form for them is unchanged.
- After a delete, the completion screen now reads "X cleaned up" rather than "X freed" (a Recycle-Bin delete reclaims nothing until the bin is emptied), leads the restore paragraph with a new "Empty it to actually reclaim the space." sentence, and ends it with an "it won't!" link to the README's "Is it safe?" section. The main window widened from 720 to 828 and the completion card from 420 to 520 to fit the longer lines.
- Delete's messages now say files are "moved to the Recycle Bin" rather than "sent", matching what Windows itself says. Where a message also offers the separate Move feature (the delete confirmation and the recycle-failure errors), it recommends "the Move button" by name, so "moved" and Move cannot read as one action.
- The completion summaries (moved and deleted, with and without errors) are now plural-aware: each is a singular/plural pair picked by the count, so the verb agrees at one file in the languages that inflect it (Italian shows "Spostato" for one file and "Spostati" for more).
- Two small refinements to the Italian interface text: the post-scan count line now describes the unneeded files as ones you can delete rather than ones to delete, and the delete summary now names the move into the Recycle Bin (matching its with-errors counterpart). Suggested by bovirus (#39).
- The opt-in result send-back is now called a "report" rather than a "summary" on the button (now "Send report"), the sent / nothing-to-send messages and the confirmation dialog's screen-reader text, in English and in Italian ("rapporto"). The descriptive tooltip deliberately keeps "summary" ("riepilogo"): a second word for the same thing reads clearer. It also matches what the README has always called these. Suggested by bovirus (#34).
- The language picker's tooltip and screen-reader label now read "Change language. The program will restart." rather than "Change the display language. InstallerClean restarts to apply it.": the Windows-specific "display language" wording, which bovirus flagged as unclear (#34), is dropped (this picker only sets InstallerClean's own language, not the OS one), and the restart note is shortened. English and Italian.
- The main-window language menu now offers all fifteen languages, listed by their endonyms in alphabetical order. It also reads the active language along the same fallback chain the translations resolve through, so a Simplified Chinese Windows (zh-CN) sees its tick on 简体中文 and can pick English; the previous exact-or-two-letter match reported English there while the interface rendered Chinese.
- The two links the app offers into the README, the completion screen's "it won't!" reassurance and the registered-files window's recovery note, now open the README in the language the app is displaying; every translated README carries matching section anchors.
- Three messages that referred to the program as "the app" now name InstallerClean: the administrator-required error, the cancel-in-progress tooltip and the send-report confirmation. English and Italian.
- The main window now leads with the unneeded (orphaned) count directly under the opening explanation, the quieter still-needed (registered) count below it, and the Move and Delete actions on their own beneath the separator; previously the unneeded count sat in the action zone beside Move and Delete while the still-needed count stood alone above. The two counts now sit together with their order matching their emphasis, the loud one first; only the order changed, the wording, counts and behaviour are unchanged. Suggested by bovirus (#39).
- The post-scan status line now reads "Found N files you can safely delete." in place of "Found N files to clean up.", stating plainly that the unneeded files are safe to delete and the choice is yours. This carries into the English source the softer sense bovirus introduced for the Italian (#39).
- The orphaned-files window's footer now breaks its count down by cause, "69 orphaned, 0 superseded, 0 obsoleted (1.28 GB)" in place of "69 unneeded files (1.28 GB)", naming the three removable categories from the window's Reason column so the line reinforces what each unneeded file is.
- The registered-files window's footer now reads "N registered files that are still needed (size)" rather than "N registered files (size)", reinforcing that "registered" means still needed (the flip side of the main window's "unneeded"); it reads "1 registered file that is still needed" at a count of one.
- The startup splash's final step now reads "Ready" rather than "Done", so the app-is-loaded state no longer shares the one word "Done" with the button that dismisses the post-clean completion screen.
- The post-clean completion screen's dismiss button reads "Fatto" in Italian rather than "Completato", naming the action you take rather than restating that the operation finished. bovirus flagged the old label (#39).
- The command-line tool's warning shown when it cannot write its Event Log entry now refers to the Application "log" rather than the Application "channel", matching the name Windows and the Event Viewer give that log.
- The Ukrainian main window's Alt access keys were reassigned so that no two controls share a letter.
- The Ukrainian texts now write their "X — це Y" constructions with the dash Ukrainian orthography requires, in the app strings and the README.
- Every satellite translation is now guarded on every push: the resx parity check (key presence, stray keys and placeholder arity) runs in CI, and the compiled-resource parity tests cover all fourteen satellites rather than only Italian and Japanese. CI also smoke-publishes the command-line tool in the self-contained single-file shape that actually ships.
- Dependabot no longer proposes bumping `Microsoft.NET.ILLink.Tasks` on its own: a bump there and nowhere else regenerates one lock file at a mismatched version, which fails every other locked-mode restore with NU1004.
- Code comments that still described the command-line tool as pinned to English were brought in line with the CLI localisation.
- The two file-details windows now left-align their column headers, so each heading sits above the values in its column instead of centred over them; the numeric size and patch columns right-align their headers to sit over the figures. The keyboard column-sort and its sort-arrow indicator are unchanged.
- The Size and Patches figures in both details windows now sit right-aligned under their right-aligned headers, rather than left-aligned under them.
- The orphaned-files details window is a little wider (its right-hand metadata panel gains the extra width while the file list keeps its size), so a long value such as a signing certificate has more room to wrap cleanly where a language's field labels run long and leave the value column cramped. Every value field in both details windows now wraps a long value rather than clipping it, closing a gap on the reason and file-size fields.
- The language menu now lists English in its alphabetical place among the other languages rather than pinned to the top; the tick still marks the current language.
- The move-confirmation dialog now shows the destination folder on its own line under "Files will be moved to:", and a long path wraps at its folder separators, so the path no longer breaks awkwardly mid-sentence.
- The completion screen's post-Move summary now puts the destination folder on its own line the same way, with German, Korean, Turkish and Japanese's sentences restructured so nothing is stranded on the line after the break.
- The main-window intro's action line now reads "or use Move instead" in place of "or Move them elsewhere first", saying the same thing more plainly.
- The main-window donate button's tooltip and screen-reader label now read "Donate" rather than "If it helped, buy me a cup of tea"; the About window keeps the cup-of-tea wording.
- Every translated README now shows the app running in its own language, rather than the previous shared English screenshots with translated captions.
- The send-report dialog's title is now just "Send this?" (the JSON below it is the actual disclosure), the line naming where the report goes now opens the paragraph instead of trailing it, and "how much space people are freeing" links to the FAQ answer showing what other people have actually freed.

## [1.9.2] - 2026-06-21

### Changed

- The installer window title and the Add/Remove Programs entry now read "InstallerClean 1.9.2" rather than "InstallerClean version 1.9.2" (and, in Italian, no longer "InstallerClean versione 1.9.2"): the word between the name and the version is dropped by setting the installer's `AppVerName` explicitly, which Inno Setup 7 also does by default. Suggested by bovirus (#36).
- The installer's file sources and output directory reference the `..\publish` folder through a single `#define` rather than repeating the literal path, so they cannot drift apart. Suggested by bovirus (#29).
- The installer's language, message and custom-message definitions move into their own `InstallerClean_Languages.iss` file, pulled in with `#include`, so the main script stays focused on install logic and each language added later touches only that one file. Suggested by bovirus (#36).

## [1.9.1] - 2026-06-20

### Added

- InstallerClean's interface is available in Italian: every window, dialog, tooltip, button and screen-reader label is translated, chosen from the main-window language menu or applied automatically on an Italian-language Windows. The command line stays English by design. Italian wording reviewed and corrected by bovirus (#32).
- The display language can be chosen from a globe in the main window: the menu lists the available languages in their own names (English, Italiano) with the current one ticked, and picking another saves it and restarts InstallerClean into it. With none chosen it follows the Windows display language.
- The Windows installer is offered in Italian: setup shows a language dialog and runs in Italian or English. Italian wording contributed by bovirus (#30).

### Changed

- On startup the saved display-language preference (or the Windows display language when none is set) is applied before any window paints, so the whole app, the splash included, opens in one language and one number format.
- The command-line tool pins itself to English regardless of the Windows display language, keeping its stdout summary lines and English event-log entries in the stable shape monitoring tools match on; file sizes still format to the local region.
- Every text size in the app is a step larger (body, captions, headings and the splash all moved up a notch); the previous sizes were a little small. The layout and proportions are unchanged; only the type size grew.
- The main window's opening explanation now reads in three tiers: the reassurance that the unneeded files are safe to delete leads, prominent; the detail about why they accumulate sits quieter beneath it; and the Delete or Move line follows at body weight. It was one muted block before, so the safety line sat dimmer than the file count under it.
- The delete confirmation reads "If you'd like backup copies, use Move instead." in place of "If you want ...", a touch more courteous for the same advice.
- The installer's copyright notice takes its year from the build date rather than a hardcoded 2026, and drops the duplicate word "Copyright" (the file-properties field is already labelled that). Suggested by bovirus (#33).
- The installer defines the publisher name and the repository URL once each and reuses them across the setup directives, rather than repeating the literals, so they cannot drift apart. Suggested by bovirus (#33).
- The setup re-detects its language each run to match the Windows UI language (`UsePreviousLanguage=no`) instead of reusing the previous install's pick, so a setup language added in a later version becomes the default for an upgrading user whose Windows matches it; the dialog still lists every language. Suggested by bovirus (#33).
- The opt-in send-summary window is slightly narrower, so the privacy line under the report wraps cleanly to two lines with the result-log address kept whole, rather than the window stretching wider than its content needs.

### Fixed

- On the language menu, pressing Enter on the language already on screen (the ticked one) now closes the menu, the same as clicking it does; before, the keyboard left the menu open where the mouse dismissed it.
- The language menu now closes when the globe is clicked a second time, so the globe toggles its own menu instead of leaving it open.
- The keyboard focus ring on the still-needed row's Details button is no longer clipped on its right and bottom edges. That button sits in the scrolling results area, whose viewport was cropping the part of the ring that extends just outside the button.
- The window title-bar text no longer clips at large Windows text sizes or with long titles. It was the one chrome element still scaling with the text-size slider while the caption bar stayed a fixed height; the titles now use a fixed size, matching the caption buttons.
- The splash sizes to its content so its version line no longer clips. The enlarged base text had grown the splash past its fixed height, leaving the bottom-pinned version straddling the card's bottom edge where the transparent window did not paint it.

## [1.9.0] - 2026-06-15

### Added

- InstallerClean honours the Windows reduced-motion setting (Settings > Accessibility > Visual effects > Animation effects), which WPF ignores by default: with animations off, the heart and star hover growth, the scanning sweep and the splash easing all go static. It reacts to the setting live, no restart.
- InstallerClean follows the Windows text-size slider (Settings > Accessibility > Text size), which WPF also ignores on its own: the app's text scales while the title bar, icons and control heights keep their shape, live as the slider moves.
- Sorting the detail windows' columns is keyboard-operable: the headers are focusable tab stops with a visible cue, Space or Enter sorts and flips direction, and the sort state reaches screen readers as "Sorted by Size, descending" instead of an arrow glyph spelled out letter by letter.
- When the Recycle Bin turns out to be unavailable for the drive, Delete offers a choice instead of just stopping: Move (the recommended route, straight into the normal flow), delete permanently or cancel. Nothing is removed without an explicit choice, the dialog states only what is known and never guesses why the bin is off, and the completion screen after a consented permanent delete says plainly that the files did not go to the bin.
- `installerclean-cli.exe` is offered as a download in its own right: one self-contained exe, no install, no .NET runtime needed, the same binary the setup installs. Drop it on a client, scan or clean, delete it. Requested in discussion #23.
- `installerclean-cli --version` (or `-v`) prints the name and Major.Minor.Patch then exits 0, deliberately without the deterministic build's `+<commit>` suffix so a parsed version stays clean.

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
- The move-location box names what to put there. Its placeholder reads "Path to folder if you Move instead of Delete" rather than the bare "Move destination..." that only echoed the MOVE LOCATION label above it, so it tells a first-timer the box wants a folder (not a file) and why it is there. The box's hover tooltip is dropped as redundant, and the placeholder now renders inside the field so it begins exactly where the typed text and caret do, rather than an overlay sibling guessing the caret offset.
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
- The Registered files window opens tall enough to read a product's whole details, down to the comment line, by arrowing through the products list rather than clicking into the details pane to scroll: the products list keeps its row count and the extra height feeds the details, sized to the longest entries (a rarer longer one still scrolls). Both Details windows clamp their opening size to the monitor's work area so they open fully on screen, and always open at that computed size: it scales with the OS text setting and fits the current screen, which a remembered past size did not, so neither window now persists a size between opens.
- Two delete-path messages are tightened: a partly-failed consented permanent delete keeps the "did not go to the Recycle Bin" clause and gains a singular form, and the one-file recycle-unavailable dialog reads "So this file ..." rather than "So this 1 file ...".
- The About window's "What it does, and why it's safe" link is removed; it opened the same repository as the Star and licence links one hop away. The pane's recovery link stays.
- The delete copy no longer frames Move as the safe option. The safety is in what gets listed (only files Windows reports as finished with), so Delete is the normal action; Move is for anyone who'd rather see for themselves first.
- The PatchCleaner comparison table gains a delete-safety row: InstallerClean never silently permanently deletes, PatchCleaner's delete is permanent with no bin step. Mirrored across the locales.
- The Delete confirmation no longer warns that a large delete may skip the Recycle Bin. The warning (shown since 1.5.2) fired on ordinary deletes: Windows only bypasses the bin for a single file larger than the per-drive quota, not for a large total. The genuine bin-unavailable case keeps its choice dialog.
- The About star and cuppa tooltips appear instantly, matching the main window's.
- The installer-busy banner is reworded from jargon into plain terms: something, usually a Windows Update or a background install, is using Windows Installer, Move and Delete are paused while it runs, Re-scan once it is done. The CLI's matching gate message follows suit, keeping its retry advice, which is right for this cause.
- The Delete confirmation drops the warning triangle by its heading; a delete to the Recycle Bin is the normal action the app is for, not a hazard.
- The post-Move heading claims only what happened: "{N} moved" for a same-volume move, which frees nothing until the parked folder is deleted, and "{N} freed" only when the move left the drive. Delete keeps "freed".
- The README documents the setup's silent install (`/SILENT`, `/VERYSILENT`); the installer already skipped its post-install launch on silent runs. Asked for in discussion #26.
- `pad.xml`'s descriptions stop selling Move as the restore path, matching the README's framing: deleting what the app lists is safe, and Move is for keeping a copy.
- The portable build returns to the compressed single-file shape, roughly halving the download (about 135 MB to about 65 MB). It had shipped uncompressed since v1.8.2 to clear a Microsoft Defender machine-learning false positive on the compressed runtime bytes; this release's compressed build was scanned on every VirusTotal engine before shipping and came back clean, so the smaller download is back.

### Fixed

- The recycle probe cleans up after itself: the tiny `ic-recycle-probe-*.tmp` it recycles to test the bin used to stay in the bin on success. The shell hands back the exact bin entry a delete creates, so the probe now permanently deletes that one item, addressed by identity, which can only ever remove the probe's own entry; real deletions stay restorable.
- The unneeded-files window's Size column is actually visible. It shipped in 1.8.2, with the list opening sorted by it, biggest first, but the column widths overran the pane with horizontal scrolling disabled, so it sat wholly past the edge; the File column (short hex names) gives up the spare width.
- Three accessibility gaps on the overlays and dialogs: the confirmation and recycle-unavailable dialogs open with focus on Cancel (a visible ring at once, and a reflexive Space lands on the non-committing choice), the recycle-unavailable dialog describes its three-way choice to screen readers, and the completion outcome is announced to screen readers, which used to hear only "Done". The decorative splash hero, completion icon and window title-bar icons leave the screen-reader tree.
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
- Nothing under `$PatchCache$` is offered for removal. The cache walk recursed into the patch engine's baseline-copy subtree, whose contents are unknown to the API, so a payload `.msi`/`.msp` cached there read as orphaned even though a later delta patch may still want it.
- A move to a folder on the system drive is no longer refused for lack of free space. A same-volume move is a rename and consumes none, yet the check demanded the batch's full size free, refusing exactly the nearly-full machines the app exists for; the check still guards cross-volume moves.
- A delete that fails for mixed reasons shows each cause over its own files; the error list had grouped by category and printed the first file's sentence over the whole group.
- A queued post-reboot rename INTO the installer cache now triggers the safety gate; the destination form Session Manager writes for a replace-existing rename (a leading `!` before the NT prefix) slipped the path match.
- A hung Explorer can no longer hang the app on a link click: the unelevated launcher bounds its wait at ten seconds and falls back to copying the address to the clipboard.
- A product registered in more than one context (per machine plus per user) lists every cached `.msi`, not just the first, so the Registered window's rows agree with its summary count.
- The documented build command is the whole solution, not the GUI project; a single-project build cannot catch a CLI-breaking change, since the tests do not reference the CLI.
- Five stale comments and labels corrected: the resx help-flags note, a misplaced format-argument note, the modal-card padding consumer list, the dark-chrome Windows-version contract and a CodeQL step name.
- The installer-busy gate no longer reads the lingering Windows Installer service as an active install. The probe tested whether the `Global\_MSIExecute` mutex existed, but the service idles for several minutes after its last job and can keep the object alive unheld, so Move and Delete could stay blocked long after an install finished; the probe now takes a zero-timeout acquire and immediate release, which measures ownership itself.
- The move-destination box no longer offers a folder drop it can never receive: Windows refuses drags from Explorer into an elevated window (verified on a real machine, the cursor shows not-allowed), so the dead drop handlers are removed.
- The move-destination watermark starts where typed text starts; it sat a few pixels left of the caret's leftmost position.
- Buttons grow with the Windows text-size setting instead of clipping their labels; every pill's fixed height (and the destination box's) became a minimum, so at 100% nothing changes.
- The main window stays usable at large text sizes: it widens with its text within the screen's work area, never grows taller than it, and when height runs out the explanation scrolls while the counts, Delete, Move and the bottom row stay visible. The count lines wrap rather than truncate when width is tight.
- The completion overlay pins its buttons and scrolls its summary text once the window cannot grow further, so Done, Send summary and Scan again survive any text size.
- The About window sizes to its content with the text scale rather than a fixed 500 by 320 box, which cut off its Close button at 208%; the splash box scales the same way.
- The confirmation cards (Delete, Move, send-summary, recycle-unavailable, update) widen with the text scale, cap at the screen's work area and scroll their body text when the cap binds; the recycle-unavailable choices wrap onto a second line rather than pushing Cancel off the card.
- The two Details windows scale their columns, panes and default size with the text setting, and a horizontal scrollbar appears when the scaled columns no longer fit instead of leaving them unreachable.
- Tooltips widen with the text scale rather than wrapping into ever narrower columns.
- A window that grows on a live text-size change moves itself back inside the work area when the growth would push its buttons under the taskbar.
- Screen readers reliably hear the outcome of a Move or Delete. The outcome announcement used to be queued just before the focus move to Done, whose own announcement cancels pending speech, so it was usually swallowed; it now follows the focus event. The restore hint (where the files went) is announced with it.
- The all-clean result of the startup scan is announced. It is set before the main window exists, so the normal announcement path never ran and a screen reader heard only "Done button" on the most common outcome of all.
- A scan that finds unneeded files announces its headline count and size; it used to end with focus in the destination box and the numbers unspoken.
- The start of a Move or Delete is announced ("Deleting 213 files..."); the overlay used to appear silently, a bare file count being the first thing spoken.
- That start announcement is spoken exactly once. The heading text is now set before the overlay appears, so on any operation after a session's first the text change can no longer be announced a second time alongside the deliberate announcement.
- The scan progress no longer floods a screen reader. It announced every installed product's name, up to hundreds in a few seconds, on the splash and the scan overlay; the product names now tick by on their own display-only line beneath the announced milestone text.
- The operating overlay's "X of Y files" line is announced at most once per tenth of the batch (first and last file always) instead of once per file; the visible counter still updates per file.
- The Details windows' rows read to screen readers as their visible columns ("Microsoft Edge, 1ab2c.msi, 98.3 MB, 2 patches", with "missing" in place of the size when the file is gone); they used to read as an internal value dump, full path and flags included.
- The confirmation dialogs read out what they are asking. The Delete and Move titles now carry the question with its count and size ("Delete 213 files (9.13 GB)?"), the send-confirmation title is its on-card question, and each dialog's description is spoken from the focused Cancel button; the old window-level description sat on an element screen readers never read.
- Tab no longer stops on unnamed scroll panes: the scrollable text regions join the tab order only when there is genuinely something to scroll, and then with a name ("Scan results", "Dialog text") and arrow-key scrolling.
- Keyboard focus no longer drops to nowhere when an operation ends without a result (cancelled, failed or refused because the bin is unavailable), after consenting to Send summary, or while the update check disables its button; the About and send-confirmation windows open with focus on their non-committal button like the other dialogs.
- The About version line and the completion overlay's error list announce their content instead of a bare "edit, read-only", and the missing-file note's link speaks its full purpose rather than the mid-sentence fragment "explains this folder".
- A warning banner that appears during a Move or Delete (something starting to use Windows Installer mid-run) no longer queues its paragraph ahead of the outcome announcement; it stays on screen to read afterwards.

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
- CI restores with the lock files enforced (`RestoreLockedMode`), so a perturbed `packages.lock.json` fails with a clear NU1004 instead of whatever it would have broken downstream.
- A `.gitattributes` normalises every text file to LF at the git boundary; the working tree is shared across two machines via an NTFS mount that intermittently flips edited files to CRLF, and the flips now stop at staging whichever machine staged them.
- The Dependabot auto-merge workflow's one mutable action tag is SHA-pinned like every other action; the job holds write permissions, so a moved tag was an arbitrary-code path onto main.
- CodeQL analyses the whole solution; the CLI host sat outside the analysed graph.
- The `packages.lock.json` churn is fixed at the root. The win-x64 sections existed only after a `-r win-x64` publish restore and every plain restore stripped them out again, so the locks flip-flopped between commands and machines; the RID is now declared in `Directory.Build.props` and the publish-injected ILLink task package referenced explicitly, so every restore computes one identical graph, verified by running both the plain build and the full self-contained publish in locked mode against the regenerated locks.

## [1.8.2] - 2026-05-27

An audit-driven release: a large sweep of correctness fixes (thread affinity, exception handling, path-leak defence on the CLI), an accessibility pass on the orphans list and completion overlay, a result-log schema bump that separates obsoleted from superseded patches, and AV-false-positive work on the portable and setup builds. No single headline feature; the value is the breadth and the receipts behind each fix.

### Added

- Stale MSI registration entries (registered with Windows yet absent from disk and not removable) surface as a diagnostic info line on the main window.

### Fixed

- Missing-from-disk banner no longer fires for a benign case. A registered patch marked superseded whose file was already gone (an older cleaner, a manual sweep) counted into the same total as a needed package gone missing, so a previously-cleaned machine could see a permanent banner suggesting something was broken when it was fine. The two counts are now separate; the banner fires only on the non-removable population.
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
- The setup wizard gains matching light and dark billboards with branding text; dark mode previously showed a gap where the image should be.
- A cross-thread race on the settings save closed; typed exception sentinels are caught before their base types so a stuck "Sending..." status clears.
- Screen-reader announcement flooding reduced on busy transitions.
- Operation progress also clears on the free-space refusal and confirm-cancel paths.
- Browser-launch URL handling hardened in the unelevated launcher.

### Changed

- InstallerClean-portable.exe ships ~135 MB instead of ~62 MB. The single-file LZMA-compressed embedded runtime that produced the smaller earlier shape tripped Microsoft Defender's machine-learning heuristic as a false positive on the v1.8.2 build; the same code lineage cleared 0/70 on v1.8.1. Turning the inner compression off (the dotnet publish `EnableCompressionInSingleFile` flag) cleared every VirusTotal engine. Slim and CLI single-file builds are unaffected and unchanged in size.
- Inno Setup wrapper now uses `Compression=bzip` with `SolidCompression=no`. The previous `Compression=zip` configuration combined with the new uncompressed-payload portable inside picked up a DeepInstinct static-ML false positive on the setup hash; bzip was the only Inno compression algorithm tested that cleared every VirusTotal engine for the v1.8.2 setup.
- Orphans-list Reason column promoted from `Text.Dim` to `Text.Muted` so the load-bearing column that distinguishes Orphaned from Superseded is no longer the lowest text tier on the most semantically critical cell.
- Orphans-list now renders as a ListView + GridView (matching the registered-files window) so screen readers announce each row as column-headed cells, and its columns click-sort like the registered window's. Previously the rows announced as single cells with the three values run together.
- Completion overlay's Done button gains Alt+D access key, matching the Alt mnemonics on the Cancel / Move / Delete / Browse / Rescan / ScanAgain buttons that previously had them.
- Result-log noun aligned across surfaces. The Send-summary button label ("Send summary") was the user-visible truth since v1.8.0, but the screen-reader Automation.Name said "diagnostic log", the failure status said "Didn't work. Never mind.", and the success status said "Result log sent". All three now say "summary"; the failure status says "Sending failed. Try again later."
- About window's Star and Buy-me-a-cuppa buttons carry distinct automation names from the main-window equivalents so a screen-reader element list with About open over Main can tell the rows apart.
- SubtleLink picks up an underline + brighten on keyboard focus matching the existing hover behaviour, so the About window's MIT licence link surfaces the same visual cue to a tabbing keyboard-only user that a mouse hover already shows.
- Body explanation paragraph now templates three Reason values (Orphaned, Superseded, Obsoleted) so a translator can edit the column labels in one place and have the body copy follow. The Obsoleted case (PatchState 4, publisher-withdrawn) gets its own clause distinct from Superseded.
- `BrowserLaunchFailed*` resx keys renamed to the `BrowserLaunch.*` dotted-category prefix every other key uses.
- `installerclean-cli.csproj` pins `PublishReadyToRun=false` matching the WPF host so a future SDK feature-band change to the default cannot silently shift the CLI's R2R section count (the same reason the runtime packages are held still: it changes the shipped bytes, and a new binary is a fresh scan result).
- `installerclean-cli.csproj` carries an ApplicationIcon so the CLI exe paints with the Squeegee in Explorer instead of the generic Windows console-exe icon, matching the GUI sibling in the install directory.
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
- Decorative window chrome hidden from the UIA tree via a custom automation peer.
- Post-1.8.1 NuGet bumps reverted to keep the shipped bytes as close as possible to the last build that scanned clean, and Dependabot now ignores patch bumps on the three runtime packages that travel in the binary closure for the same reason: a patch-level change should not re-roll a scan result between releases. No flag in the project's history has been traced to a dependency bump.
- `pad.xml` drops the 32-bit Windows 10 support claim.
- Spacing tokens tidied: horizontal-only `GapX.*` Thickness tokens added, five unused tokens deleted.
- The crash log's privacy header moves into resx; the `Summary` string splits singular and plural forms instead of straddling a placeholder.
- Comment passes across the release's new code: contract style, stale anchors and unfounded attributions removed; a dead result-log parameter dropped.
- CI runners pinned to windows-2025.

## [1.8.1] - 2026-05-13

### Changed

- All-clean completion overlay now uses the same two-tier text hierarchy as the post-Move and post-Delete overlays: the "Nothing to clean up in C:\Windows\Installer" headline renders in body weight (Summary slot), and the "Scanned N products in T" receipt renders smaller and muted (Restore slot). Both lines previously rendered at the same body weight.
- Dropped trailing full stops on every completion-overlay text line (summary, scan receipt, Move and Delete restore hints) so the overlays read consistently as labels rather than mixed sentences and labels.
- A PAD 4.0 file (`pad.xml`) added at the repo root for Softpedia's automatic listing refresh.

## [1.8.0] - 2026-05-13

The two new opt-in network features (a manual update check and the Send-summary report) were the headline, but the bulk of this release was a deep accessibility pass across every window, a security and AV-heuristic hardening pass, internationalisation, and a correctness sweep, all done with the same one-click-from-elevated caution the rest of the app holds to.

### Added

- Check for updates in About now performs the version check itself rather than opening the releases page. Single HTTPS GET to `api.github.com/repos/no-faff/InstallerClean/releases/latest` on click; UA `InstallerClean/<version>`; 8 s timeout; localised result dialog; a styled "update available" window when behind.
- Send summary on the completion overlay. Writes `%LOCALAPPDATA%\NoFaff\InstallerClean\last-run.json` after every Move, Delete or all-clear; opens a confirmation window showing the exact JSON; POSTs to `https://nofaff.netlify.app/api/result-log` on confirm. Counts and categorical labels only. No paths, no usernames, no machine identifiers, no time-of-day. Once per machine, ever, with a confirm-before-send window and a lifetime lock.

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
- Action buttons reordered Delete before Move, and the body copy disambiguates which files "the files listed below" are.
- The CHANGELOG is backfilled with the full release history (v1.0.0 through v1.5.3), then rewritten in the terse Keep-a-Changelog house style.
- Pre-ship copy sweeps across the resx (wording, tense, consistency).
- Dependabot bumps grouped to cut PR noise, with patch and minor bumps auto-merged in CI.

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
- About window layout reworked; an underscore artefact in its text removed.

### Security and hardening

- Defence-in-depth pass on the network and input boundaries: a cap on the Send-summary request size, a bounded JSON parse depth (`MaxDepth=8`) on the update-check and settings deserialisation, and tightened handling of window-activation process IDs in the focus logic.
- Correctness and hardening sweep across the codebase: tightened symmetry in shared helpers, an AboutWindow close guard, corrected cancellation-token ordering in `ScanAsync`, and receipts on the splash icon load.
- Consolidated the user32 P/Invoke surface into Core and removed the need for `AllowUnsafeBlocks` on the WPF host, reducing the heuristic-AV signal of the host binary.

### Changed (internal)

- Codebase-wide comment and code-quality pass: comments brought to a state-the-contract standard, XAML literals (corner radii, margins, close-button overhang) replaced with design tokens, and the test suite extended to pin the result-log schema, the update-check user-agent contract, and the JSON parse-depth limits.

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
- Keyboard focus rings across the app, tuned per button geometry, with screen-reader HelpText on the destructive actions.
- CI builds and smoke-publishes the CLI, audits each project for vulnerable packages and Dependabot covers all four projects.
- GitHub community-standards files (code of conduct, security policy), the contact email later redacted from both.
- A 512px app icon for listings.

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
- The SDK is pinned with `rollForward: latestPatch` so releases build on a known band.

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
- `SHFILEOPSTRUCT` packing reverted to `Pack=8`: `Pack=1` mis-aligns the x64 struct and crashes in the kernel.
- Scan-failure dialog title is generic rather than database-specific.
- Caption buttons stay out of the keyboard focus path.
- Move and Delete tooltips read correctly in both enabled states; Esc paints "Cancelling..." immediately.
- Keyboard-focus visuals normalised on the pill buttons and the completion overlay's Close button.
- The indeterminate progress bar's storyboard reworked after the wpfui removal.

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

- `UpdateCheckService` (the HTTP-based update check). Check for updates now opens the GitHub releases page in the browser. The setup was being flagged by DeepInstinct on VirusTotal; auto-HTTP-on-startup from an elevated process was the leading suspicion at the time.

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
- Scoop install documented, served from the no-faff/scoop-bucket.
- A 32x32 icon for directory-listing submissions.

### Changed

- All wildcard NuGet dependencies pinned (CommunityToolkit.Mvvm, NSubstitute); transitive dependency lockfile enabled; application manifest hardened.
- Inno Setup script tightened with explicit `AppId`, `MinVersion=10.0`, `ArchitecturesAllowed=x64compatible`.
- GitHub Actions in CI and CodeQL workflows pinned to commit SHAs, and the actions bumped by Dependabot (codeql-action 4, setup-dotnet 5, checkout 6).
- Completion screen: pressing Enter closes the window (Close button is `IsDefault`).
- Comment tidy across the services; a vendor-specific historical reference removed.

### Fixed

- Event-handler leaks on window close on repeated scans; subscriptions unhooked in `OnClosed`.
- Removed an orphaned image asset that was no longer referenced.
- A test that asserted record properties rather than service behaviour removed as misleading.

## [1.5.0] - 2026-04-04

### Added

- Manual Check for updates button in About; hits the GitHub Releases API on click only.
- Heart Donate icon on the main window (replaces the Ko-fi-shaped button).
- Hover animation on the star and heart icons.
- Dependabot for npm-style dependency PRs.
- CodeQL workflow for automated static analysis.

### Changed

- Donate link now points to `nofaff.netlify.app`.
- An automatic startup update check (with an opt-out toggle) was added and removed within the release: DeepInstinct flagged the build on VirusTotal and the startup network call from an elevated process was the leading suspicion, so the check shipped manual-only.

## [1.4.1] - 2026-03-10

### Added

- 99 tests (was 56): coverage for `InstallerQueryService`, `MsiFileInfoService`, `PendingRebootService` and the model records.
- Project metadata: `Authors`, `Description`, `RepositoryUrl`, `Licence` populated in the assembly info.

### Changed

- WCAG AA contrast pass: dim text raised from 3.2:1 to 4.7:1; orphaned-files summary brightened.
- Design tokens: ~35 hardcoded colour values replaced with named resources (`Warning`, `Dim`, `Danger`, `Base200`, `Primary`).
- `CommunityToolkit.Mvvm` pinned to 8.4.0 (was `8.*`).
- Em dashes replaced with plain dashes and full stops across the UI and docs.
- CONTRIBUTING noted the suspicion that the `Func<>` testability pattern triggers AV heuristics.

### Removed

- Icon working files removed from tracking (re-added to `.gitignore`).

## [1.4.0] - 2026-03-09

### Added

- GitHub Actions CI: build and test suite on every push and PR.
- 56 tests covering stress conditions, error handling and edge cases.
- `CONTRIBUTING.md` with build instructions, commit conventions and AV-friendly constraints.

### Changed

- Test mocking framework switched from Moq to NSubstitute (Moq's SponsorLink dependency was a concern for a freely-distributed project).
- A `Func<>`-delegate testability seam for `MainViewModel` was added and reverted within the release, on a suspicion that the pattern trips AV heuristics, not a reproduction.

## [1.3.0] - 2026-03-08

### Added

- `installerclean-cli.exe /s`: scan-only CLI mode that lists removable files (filenames + sizes) without taking action. Exit code always 0.
- Tests for `DisplayHelpers` (FormatSize, Pluralise) and `OrphanedFilesViewModel`.

### Changed

- Splash screen shows real scan progress instead of fixed steps.
- Code cleanup: verbose XML docs stripped, dead code removed, stale tracked docs cleaned up.

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
- The framework-dependent build is named "slim" (was "portable-requires-dotnet8").
- The caption buttons drop their tooltips.

### Fixed

- Access-key underscores no longer show as literal text in labels.
- GitHub links updated from the old repository name.
- The Start Menu shortcut carries an explicit app icon.

## [1.1.0] - 2026-03-05

### Added

- Custom `WindowChrome` title bars across all windows; dark theme, app icon, per-window heading.
- Custom caption buttons (minimise, close) styled to match the dark theme; close has a red hover.

### Changed


### Fixed

- Detail windows auto-select and focus the first item on open (keyboard navigation worked but had no visible target).

## [1.0.0] - 2026-03-04

Initial public release. Built from the ground up over months and 164 commits: the scan-and-correlate engine, a safety model that moves rather than deletes, a full WPF application taken through nine rounds of UI redesign, a custom dark theme with bundled Poppins, the superseded-patch detection that is the real advance over PatchCleaner, a console CLI, and the distribution and trust work to ship it. The detail below is the shape of that first release, not a summary of it.

### Added

#### Scan and correlation engine

- `C:\Windows\Installer` scan: enumerates every `.msi` and `.msp` file and correlates each against the Windows Installer API to identify which are still registered and which are orphaned.
- Windows Installer query layer over the `MsiEnum*` / `MsiGet*Info` COM API, using the documented double-call buffer pattern to size each call before reading.
- Superseded patch detection: reads the patch state from the Windows Installer API (the State and Uninstallable properties) to find patches Windows has replaced or withdrawn but never deleted, and lists them as Superseded or Obsoleted rather than as orphaned files.
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
- Version shown in the main window title bar.
- The full digital-signature subject shown in the detail panels, not just the CN.
- First-run prompt offering a Start Menu shortcut, with a desktop one optional.
- A "Send feedback" link in About opening GitHub Discussions.
- GitHub star and Ko-fi donate icons in the bottom nav.
- Proper pluralisation throughout (no "file(s)"), and British English throughout.

#### Theme and visual design

- Custom dark theme with a layered design-token resource system, inspired by Upscayl.
- Custom `WindowChrome` title bars across every window, with caption buttons styled to match (close has a red hover).
- Poppins bundled as the body font.
- App icon (the squeegee) set on every window.

#### Command line

- Console CLI: `/d` (Delete), `/m` (Move to saved default), `/m PATH` (Move to a specified path), plus `--help`.

#### Engineering

- xUnit test suite grown alongside the code from the first scaffold: scan, move, delete, settings, view-model and cancellation coverage, refreshed at every architecture change.

#### Distribution and trust

- Self-contained `InstallerClean.exe` and a framework-dependent build (needs the .NET Desktop Runtime).
- VirusTotal scan published (1/70 on the final release build) and linked from the README.
- No data collection.

### Changed

- Renamed from the working title (Simple Windows Installer Cleaner) to **InstallerClean** ahead of launch.
- The original exclusion-filter feature (substring/summary-info matching to exclude files) was removed once superseded-patch detection made it unnecessary: detecting the real patch state is more correct than asking the user to maintain exclusion rules.
- An early light-and-dark pass that followed the Windows system theme was cut before launch; the app shipped with one deliberate dark look.
