#!/usr/bin/env node
// English-source TEMPLATE for the InstallerClean translation satellites.
// One template, copied per language. It works FROM THE ENGLISH SOURCE so every
// language is translated from the source, never from another translation, and so
// a phrase a translator forgets is caught by the script instead of slipping
// through.
//
// HOW IT WORKS
//   - Structural base: the English neutral `Strings.resx`. Of the `Cli.*`
//     <data> elements only the MACHINE-contract keys are removed BY NAME (the
//     `Cli.EventLog*` set bar `Cli.EventLogUnavailable`): those are Application-
//     channel event-log lines an RMM tool greps for fixed English phrases, forced
//     English at runtime, so a satellite must not carry them. The HUMAN-facing
//     `Cli.*` keys stay and ARE translated, alongside the non-`Cli.` keys. How
//     many that is, this file does not say: the set grows whenever the command
//     line gains an event-log line, and the self-check below derives the figure
//     from the neutral for exactly that reason. A number written here goes stale
//     silently while the checked one beside it stays right, and had.
//   - The `MAP` is seeded with the ENGLISH values, every translatable key (the
//     non-`Cli.` set plus the human `Cli.` set). You replace each English value with its
//     translation. A value you leave
//     unchanged therefore stays ENGLISH, the obvious "not done yet" state, and
//     the self-check FAILS on it (see the untranslated gate below) unless its key
//     is a universal keep (KEEP_ENGLISH) or one your language deliberately keeps
//     (ALSO_KEEP). Nothing half-translated reaches GENERATION OK.
//   - Everything else is preserved byte-for-byte from the neutral: schema,
//     resheaders, the per-key <comment> children (English context, kept), `&#10;`
//     entities, literal newlines, and any leading/trailing spaces in a value.
//     Output is LF, UTF-8.
//
// FOR A NEW LANGUAGE
//   1. Copy this file to scripts/translations/gen-strings-<code>.mjs.
//   2. Set OUT (below) to src/InstallerClean.Core/Resources/Strings.<code>.resx.
//   3. Translate every MAP value into your language. Leave KEEP_ENGLISH values
//      as they are. If your language deliberately keeps an English word identical
//      (e.g. some languages keep "Patch"), add that KEY to ALSO_KEEP.
//   4. Run from the repo root: node scripts/translations/gen-strings-<code>.mjs
//      Chase it to GENERATION OK.
//
// MAP escaping (template literals): \\ is one backslash, \n is a real newline (the
// multi-line values), {0}/{1} are .NET placeholders left verbatim, and &#10; is
// written literally where the neutral uses the XML entity. The backslashes are the
// EXAMPLE paths a couple of values show the user (`D:\\Backup`, `\\\\server\\backup`),
// which a language may localise; the app's own installer folder is never spelled out
// in a value. {InstallerFolder} carries it, a token the app substitutes at runtime and
// never a word: it may move within a sentence, but a language that renders it ships a
// wrong path, which is what scripts/check-cross-key-rules.mjs fails on.
//
// TEMPLATE MODE (this file, OUT left at its default): the MAP is the English source
// rather than a translation, so the untranslated gate would fire on every line and
// mean nothing. The self-check inverts instead: every value must still BE the
// neutral's, and one that is not is a neutral edit this file never took, which the
// next language would then be translated from. A copy that points OUT at a satellite
// is a normal generator again, gate and all. CI runs every generator on every push
// and reads this file's verdict line, so a neutral edit that never reached the MAP is
// caught there as well as by whoever regenerates the satellites.
import { readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
// A new language MUST point OUT at its own file, e.g. `${dir}/Strings.de.resx`.
// The temp-directory default means running this template UNCHANGED can never
// overwrite a shipped resx (it writes a throwaway), and it is also what selects
// template mode in the self-check, so a copy that forgets step 2 fails on its first
// real translation instead of quietly writing somewhere harmless.
// tmpdir() rather than a literal /tmp, because this is run on Windows as well as on
// Linux: Git Bash resolves a bare /tmp against the drive it is started from, giving a
// D:\tmp that exists on no CI runner, and the write then threw before the verdict
// line, taking down the gate that reads it and everything that gate stands in front of.
const TEMPLATE_OUT = join(tmpdir(), 'Strings.template-output.resx');
const OUT = TEMPLATE_OUT;
const IS_TEMPLATE = OUT === TEMPLATE_OUT;

// Universal keeps: keys whose value is the same in every language (brand names,
// the pure-placeholder string, the size/elapsed format strings). Their still-
// English value is NOT a miss. Explicit by KEY on purpose: a future brand/format
// key then defaults to "flag until someone adds it here", never silently passes.
// Do NOT translate these values. Do NOT edit this list per language.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
  'Display.Size.GB',                   // {0:F2} GB
  'Display.Size.MB',                   // {0:F1} MB
  'Display.Size.KB',                   // {0:F1} KB
  'Display.Size.B',                    // {0} B
  'Display.Elapsed.Ms',                // {0:F0}ms
  'Display.Elapsed.S',                 // {0:F1}s
]);

// Per-language keeps: keys whose value YOUR language deliberately keeps identical
// to English (e.g. ['Plural.Patch.Singular'] if your language keeps "patch").
// Empty in the template. Keep it minimal: legitimate keeps are single short
// tokens, never a phrase. The self-check prints this roster so it stays honest.
const ALSO_KEEP = [];

// Satellite-only CLDR plural overrides: keys that do NOT exist in the neutral,
// appended verbatim before </root> and read by name at runtime
// (DisplayHelpers.Pluralise's One/Few/Many branches; an absent one falls back to
// the base). EMPTY in the template. A language whose grammar needs a count form
// the neutral's one/other pair cannot express adds them here BY NAME, value in
// YOUR language (there is no English baseline to translate from). Each value's
// {N} set MUST match its base key's set (base = the <Prefix>.Plural sibling if the
// neutral has one, else the flat key itself), the precondition check-resx-parity.mjs
// also enforces. Example (Spanish):
//   'Status.RegisteredPackagesFound.One': `Se encontró {0} {1} registrado.`,
const OVERRIDES = {};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `About`,
  'Window.Registered.Title': `Registered files that should not be deleted`,
  'Window.Orphaned.Title': `Unneeded files that are safe to delete`,

  // Section headings
  'Section.Registered.Products': `PRODUCTS`,
  'Section.Registered.Patches': `PATCHES`,
  'Section.Registered.Details': `PRODUCT DETAILS`,
  'Section.Backup.Folder': `BACKUP FOLDER`,
  'Section.SayThanks': `SAY THANKS`,

  // Field labels (used in detail panels)
  'Field.Reason': `Reason`,
  'Field.Author': `Author`,
  'Field.Application': `Application`,
  'Field.Title': `Title`,
  'Field.Subject': `Subject`,
  'Field.Keywords': `Keywords`,
  'Field.SigningCertificate': `Signing certificate`,
  'Field.FileSize': `File size`,
  'Field.Comment': `Comment`,
  'Field.ProductName': `Product name`,
  'Field.File': `File`,
  'Field.Size': `Size`,
  'Field.Patches': `Patches`,

  // Placeholder shown for a registered package whose API ProductName is empty.
  'Field.UnknownProductName': `(unknown)`,
  'Field.PatchesOnly': `(patches only)`,
  'Field.Missing': `missing`,

  // Actions (button labels; underscore prefixes are WPF mnemonics)
  'Action.About': `_About`,
  'Action.Copy': `Copy`,
  'Action.Cut': `Cut`,
  'Action.Paste': `Paste`,
  'Action.SelectAll': `Select all`,
  'Action.Browse': `_Browse...`,
  'Action.Cancel': `_Cancel`,
  'Action.CheckForUpdates': `Check for _updates`,
  'Action.Close': `_Close`,
  'Action.DeletePermanently': `_Delete permanently`,
  'Action.Done': `_Done`,
  'Action.Details': `Details`,
  'Action.BuyMeACuppa': `_Buy me a cuppa`,
  'Action.LeaveStarOnGitHub': `Leave a _star on GitHub`,
  'Action.Licence': `Apache 2.0 licence`,
  'Action.Move': `_Move`,
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
  'Action.OpenReleasePage': `Open _release page`,
  'Action.Rescan': `_Re-scan`,
  'Action.ScanAgain': `_Scan again`,
  'Action.SendResultLog': `Send report`,
  'Action.SendResultLogConfirm': `_Send`,
  'About.Link.Guide': `Guide and FAQ`,
  'About.Link.ReportProblem': `Report a problem`,
  'About.AutoUpdateCheck': `Check for updates automatically`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa': `Donate`,
  'Automation.BuyMeACuppa.About': `Buy me a cuppa`,
  'Automation.CancelOperation': `Cancel operation`,
  'Automation.CancelScan': `Cancel scan`,
  'Automation.CancelStartupScan': `Cancel startup scan`,
  'Automation.Close': `Close`,
  'Automation.CloseWindow': `Close window`,
  'Automation.CloseResult': `Close result and return to main window`,
  'Automation.LeaveStarOnGitHub.About': `Leave a star on github`,
  'Automation.Minimise': `Minimise`,
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
  'Automation.ConfirmMove': `Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are.`,
  'Automation.SayThanks': `Say thanks`,
  'Automation.ConfirmSendResultLog': `Send posts the report shown to No Faff. Cancel sends nothing.`,
  'Automation.CheckForUpdates': `Check for updates`,
  'Automation.CheckForUpdates.HelpText': `Checks github's releases page for a newer version.`,
  'Automation.About.Guide.HelpText': `Opens the readme on github in your browser.`,
  'Automation.About.ReportProblem.HelpText': `Opens the issue tracker on github.com in your browser.`,
  'Automation.AutoUpdateCheck.HelpText': `If ticked, InstallerClean checks github for a newer version when you run it.`,
  'Automation.UpdateAvailable.HelpText': `Open the release page to download the newer version, or cancel to keep the current version.`,
  'Automation.Licence.HelpText': `Opens the licence file on github.com in your browser.`,
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `Products`,
  'Automation.Section.Patches': `Patches`,
  'Automation.Section.ProductDetails': `Product details`,
  'Automation.BackupFolder': `Backup folder`,
  'Automation.OperationProgress': `Operation progress`,
  'Automation.RescanInstaller': `Scan {InstallerFolder} again`,
  'Automation.ScanningProgress': `Scanning progress`,
  'Automation.StartupScanProgress': `Startup scan progress`,
  'Automation.ViewOrphanedFiles': `Details, unneeded files`,
  'Automation.ViewOrphanedFiles.HelpText': `Available for cleanup.`,
  'Automation.ViewRegisteredFiles': `Details, registered files`,
  'Automation.ViewRegisteredFiles.HelpText': `Read-only inventory.`,
  'Automation.SortStatus.Ascending': `Sorted by {0}, ascending`,
  'Automation.SortStatus.Descending': `Sorted by {0}, descending`,
  'Automation.Scroll.ScanResults': `Scan results`,
  'Automation.Scroll.ResultDetails': `Result details`,
  'Automation.Scroll.FileDetails': `File details`,
  'Automation.Scroll.ProductDetails': `Product details`,
  'Automation.Scroll.DialogBody': `Dialog text`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `Files that could not be processed`,
  'Automation.RegisteredMissingSeeAlso': `Explains this folder, and how to recover a file, in the README`,

  // Tooltips
  'Tooltip.BuyMeACuppa.About': `It's thirsty work!`,
  'Tooltip.CancellingPending': `Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call.`,
  'Tooltip.Close': `Close`,
  'Tooltip.LeaveStarOnGitHub.About': `A star helps other people find it.`,
  'Tooltip.Minimise': `Minimise`,
  'Tooltip.SendResultLog': `Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm.`,
  'Tooltip.SendResultLog.NothingFound': `Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm.`,
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Subject name from the embedded Authenticode certificate. Not chain-verified.`,

  // Body copy
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with.`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.NotScanned.Lead': `Nothing scanned yet.`,
  'Body.NotScanned.Why': `Press Re-scan to look through {InstallerFolder} for installer files that no program still needs.`,
  'Body.PendingReboot.Lead': `These files can't be cleaned up right now.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.PendingReboot.Other': `Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back.`,
  'Body.NoFileSelected': `Select a file to view details.`,
  'Body.NoProductSelected': `Select a product to view details.`,
  'Body.NoMetadata': `No metadata available.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.&#10;&#10;It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.&#10;&#10;To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `The README [explains this folder], and how to recover a file, in Microsoft's own words.`,
  'Body.NoPatches': `(none)`,

  // Reasons (Reason column in the orphaned-files list)
  'Reason.Orphaned': `Orphaned`,
  'Reason.Superseded': `Superseded`,
  'Reason.Obsoleted': `Obsoleted`,

  // Status / progress text
  'Status.Scanning': `Scanning...`,
  'Status.Cancelling': `Cancelling...`,
  'Status.StartingScan': `Starting scan...`,
  'Status.QueryingApi': `Asking Windows about installed software...`,
  'Status.ScanningCache': `Scanning installer cache folder...`,
  'Status.EnumeratingProducts': `Enumerating installed products...`,
  'Status.CheckingRegistry': `Checking registry for additional packages...`,

  // 0 = registered package count, 1 = pluralised "package"/"packages"
  'Status.RegisteredPackagesFound': `Found {0} registered {1}.`,

  // 0 = elapsed time text (e.g. "1.2s")
  'Status.ScanComplete': `Scan complete ({0})`,
  'Status.FoundProducts': `Scanning local packages...`,

  // 0 = file count, 1 = pluralised noun ("file"/"files")
  'Status.FoundUnused': `Found {0} {1} you can safely delete.`,
  'Status.PreparingDestination': `Preparing destination folder...`,

  // 0 = file count, 1 = pluralised noun
  'Status.Moving': `Moving unneeded files...`,
  'Status.Deleting': `Deleting unneeded files...`,
  'Status.MoveCancelled.Partial': `Move cancelled. {0} of {1} {2} processed.`,
  'Status.DeleteCancelled.Partial': `Delete cancelled. {0} of {1} {2} processed.`,
  'Status.MoveFailed': `Move failed ({0}). Details in {1}.`,
  'Status.MoveFailed.NoLog': `Move failed ({0}). The crash log could not be written.`,
  'Status.DeleteFailed': `Delete failed ({0}). Details in {1}.`,
  'Status.DeleteFailed.NoLog': `Delete failed ({0}). The crash log could not be written.`,
  'Status.ScanAccessDenied': `Access denied. Windows refused the scan.`,
  'Status.ScanFailedDb': `Scan failed: couldn't read the Windows Installer records.`,
  'Status.ScanCancelled': `Scan cancelled.`,
  'Status.Done': `Ready`,
  'Status.ScanFailedDetails': `Scan failed ({0}). Details in {1}.`,
  'Status.ScanFailedDetails.NoLog': `Scan failed ({0}). The crash log could not be written.`,

  // Completion screen
  'Completion.AllClean': `All clean`,
  'Completion.NothingToCleanUp': `Nothing to clean up in {InstallerFolder}`,
  'Completion.NothingToCleanUpReceipt': `Scanned {0} {1} in {2}`,

  // 0 = size freed (e.g. "120.5 MB")
  'Completion.Freed': `{0} freed`,
  'Completion.Moved': `{0} moved`,

  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `Nothing was moved`,
  'Completion.NothingDeleted': `Nothing was deleted`,
  'Completion.FailedCount.Singular': `{0} of {1} could not be moved.`,
  'Completion.FailedCount.Plural': `{0} of {1} could not be moved.`,
  'Completion.FailedCountDelete.Singular': `{0} of {1} could not be deleted.`,
  'Completion.FailedCountDelete.Plural': `{0} of {1} could not be deleted.`,

  // 0 = moved count, 1 = pluralised noun, 2 = destination path
  'Completion.MoveSummary.Singular': `{0} {1} moved to: {2}`,
  'Completion.MoveSummary.Plural': `{0} {1} moved to: {2}`,

  // 0 = deleted count, 1 = pluralised noun
  'Completion.ReverifySkipped': `{0} {1} kept in place, because a program went back to needing what the scan flagged.`,
  'Completion.ReverifyRecordsChanged': `{0} {1} kept in place, because the Windows Installer records had changed by the final check.`,
  'Completion.ReverifyIncomplete': `{0} {1} kept in place, because the Windows Installer records could not be fully read in the final check.`,
  'Completion.MoveCancelledSummary': `Moved {0} of {1} {2} before you cancelled.`,
  'Completion.PermanentDeleteCancelledSummary': `Permanently deleted {0} of {1} {2} before you cancelled.`,

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,
  'Completion.DonateAsk': `Glad to help. There's a tip jar if you're feeling kind.`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} file still needed`,
  'Summary.RegisteredStillUsed.Plural': `{0} files still needed`,
  'Summary.OrphanedToCleanUp.Singular': `{0} unneeded file to clean up`,
  'Summary.OrphanedToCleanUp.Plural': `{0} unneeded files to clean up`,
  'Summary.MissingFromDisk.Singular': `{0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do.`,
  'Summary.MissingFromDisk.Plural': `{0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do.`,
  'Summary.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Summary.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0} of {1} {2}`,

  // Orphaned-window footer: unneeded files split into the three removable causes
  // (true orphans, superseded patches, obsoleted patches). 0 = orphaned count,
  // 1 = superseded count, 2 = obsoleted count, 3 = size display. No trailing
  // noun, so it agrees at any count.
  'Summary.OrphanedWindow': `{0} orphaned, {1} superseded, {2} obsoleted ({3})`,

  // Registered-window footer, split singular/plural so the noun and verb agree at
  // one file ("file ... is" vs "files ... are"). 0 = count, 1 = size display.
  'Summary.RegisteredWindow.Singular': `{0} registered file that is still needed ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} registered files that are still needed ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Move {0} {1} ({2})?`,

  // 0 = destination path
  'Confirm.MoveDestination': `Files will be moved to:`,
  'Confirm.DeleteTitle': `Delete {0} {1} ({2})?`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,

  // Error messages
  'Error.AdminRequiredTitle': `Access denied`,
  'Error.AdminRequiredBody': `Windows refused InstallerClean access, so it stopped. Nothing has been removed.\n\nInstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try.`,
  'Error.InstallerDbUnavailableTitle': `Couldn't read the Windows Installer records`,
  'Error.ScanFailedTitle': `Scan failed`,
  'Error.InstallerDbEmpty': `The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed.`,
  'Error.MsiAccessDenied': `Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed.`,
  'Error.MsiNonSuccess': `Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed.`,
  'Error.ScanCorrelationFailed': `InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed.`,
  'Error.InstallerLockUnavailableTitle': `Nothing was deleted`,
  'Error.InstallerLockUnavailable': `InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
  'Error.ScanRecordsUnreadable': `InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed.`,
  'Error.InvalidDestinationTitle': `Invalid destination`,
  'Error.DestinationWriteFailedTitle': `Could not write to destination`,
  'Error.MoveFailedTitle': `Move failed`,
  'Error.DeleteFailedTitle': `Delete failed`,
  'Error.SettingNotSavedTitle': `Setting not saved`,
  'Error.SettingNotSavedBody': `The change could not be saved. InstallerClean will go back to the previous setting next time it starts.`,
  'Error.DestinationInsideInstaller': `The destination cannot be inside the Windows Installer folder.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Not enough space`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `Not enough space at {0}\n\nRequired: {1}\nAvailable: {2}`,

  // 0 = destination
  'Error.AccessDeniedDestination': `You don't have permission to write to {0}.\nTry a folder in your user profile or on a drive you own.`,
  'Error.PathTooLong': `The path {0} is too long for Windows. Pick a shorter path.`,
  'Error.DestinationMissing': `The folder {0} does not exist and could not be created. Check the drive letter or network path.`,
  'Error.IOWriteDestination': `Windows cannot write to {0}.\nDetails in {1}.`,
  'Error.IOWriteDestination.NoLog': `Windows cannot write to {0}. The crash log could not be written.`,
  'Error.WriteDestination': `Cannot write to {0}.\nDetails in {1}.`,
  'Error.WriteDestination.NoLog': `Cannot write to {0}. The crash log could not be written.`,
  'Error.MissingSourceFile': `File no longer exists.`,
  'Error.SourceIsReparsePoint': `Source file is a symlink or junction; refused for safety.`,
  'Error.CandidateOutsideCache': `This file is not directly inside the Windows Installer folder; refused for safety.`,

  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows refused access to this file; it was left in place.`,
  'Error.AccessDenied.Plural': `Windows refused access to these files; they were left in place.`,
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `Windows reported a file error; the file was left in place.`,
  'Error.IOFailure.Plural': `Windows reported file errors; these files were left in place.`,
  'Error.UnknownError.Singular': `Something went wrong with this file; it was left in place.`,
  'Error.UnknownError.Plural': `Something went wrong with these files; they were left in place.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Refusing to move files into the Windows Installer folder (destination: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\\Backup, or \\\\server\\backup). InstallerClean can't use this one: {0}`,
  'BrowserLaunch.FailedTitle': `Couldn't open your browser`,
  'UpdateCheck.Title': `Check for updates`,
  'UpdateCheck.Status.Checking': `Checking...`,
  'UpdateCheck.Status.UpToDate': `Up to date.`,
  'UpdateCheck.Status.UpdateAvailable': `Version {0} is available.`,
  'UpdateCheck.UpdateAvailable.Title': `Update available`,

  // 0 = installed version, 1 = latest version on GitHub
  'UpdateCheck.UpdateAvailable.Body': `You're running version {0}.&#10;Version {1} is available.`,
  'UpdateCheck.Failed.NetworkUnavailable': `Couldn't reach GitHub. Check your internet connection and try again.`,
  'UpdateCheck.Failed.ServerError': `GitHub returned an error response. Try again in a few minutes.`,
  'UpdateCheck.Failed.ResponseParseError': `GitHub's response did not contain a recognised release. Try again later, or open the releases page directly.`,
  'UpdateCheck.Failed.Timeout': `The check timed out. Your connection to GitHub may be slow; try again.`,
  'UpdateCheck.Failed.Unknown': `The check failed for an unknown reason. Details are in crash.log if you need to report it.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again.`,

  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Cannot write to {0}.`,

  // 0 = file name
  'Error.NoUniqueFilename': `Could not find a unique filename for '{0}' after 10,000 attempts.`,

  // Result log (post-cleanup diagnostic send)
  'ResultLog.Sending': `Sending...`,
  'ResultLog.Sent': `Thanks! Report sent.`,
  'ResultLog.Failed': `Sending failed. Try again later.`,
  'ResultLog.NothingToSend': `No report to send.`,
  'ConfirmSendResultLog.Title': `Send this?`,
  'ConfirmSendResultLog.Reassurance': `It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing].`,
  'Automation.ResultLogPreview': `Report preview`,

  // Single instance / startup / crash
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean is already running.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `An unexpected error occurred and InstallerClean needs to close.\n\n{0}\n\nDetails written to:\n{1}`,
  'Startup.UnhandledBody.NoLog': `An unexpected error occurred and InstallerClean needs to close.\n\n{0}\n\nThe crash log could not be written.`,
  'Startup.ErrorTitle': `Startup error`,
  'Startup.FailedToStart': `Failed to start ({0}). Details written to:\n{1}`,
  'Startup.FailedToStart.NoLog': `Failed to start ({0}). The crash log could not be written.`,

  // File picker
  'FilePicker.ChooseDestinationTitle': `Choose destination folder for moved files`,

  // Version display

  // 0 = major.minor.patch (e.g. "1.5.4")
  'Version.Display': `Version {0}`,
  'Plural.File.Singular': `file`,
  'Plural.File.Plural': `files`,
  'Plural.Error.Singular': `error`,
  'Plural.Error.Plural': `errors`,
  'Plural.Package.Singular': `package`,
  'Plural.Package.Plural': `packages`,
  'Plural.Product.Singular': `product`,
  'Plural.Product.Plural': `products`,
  'Plural.Patch.Singular': `patch`,
  'Plural.Patch.Plural': `patches`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ElapsedLong.LessThanASecond': `less than a second`,
  'Display.ElapsedLong.Seconds': `{0:F1} seconds`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys. The
  // machine-contract Cli.EventLog* keys (bar Cli.EventLogUnavailable) are NOT
  // here and are stripped from the output; they stay English at runtime. In the
  // help lines translate the DESCRIPTION only: keep the command tokens, flags,
  // the {InstallerFolder} token and the exit-code numbers verbatim, keep the
  // leading spaces (the screen is column-aligned for a monospace terminal), and
  // translate the PATH metavariable (it names the argument, as es/pt-BR/it did).
  'Cli.UnknownArgument': `Error: unknown argument '{0}'`,
  'Cli.TooManyArguments': `Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\\My Backup"`,
  'Cli.Cancelling': `Cancelling...`,
  'Cli.Cancelled': `Cancelled.`,
  'Cli.GenericError': `Error: unexpected failure ({0}). Details written to {1}.`,
  'Cli.GenericError.NoLog': `Error: unexpected failure ({0}). The crash log could not be written.`,
  'Cli.ScanningInstaller': `Scanning {InstallerFolder}...`,
  'Cli.FoundOrphans': `Found {0} unneeded {1} to clean up ({2}).`,
  'Cli.FoundNoOrphans': `Found no unneeded files.`,
  'Cli.DeletingFiles': `Deleting {0} unneeded {1}...`,
  'Cli.DeletedFiles': `Permanently deleted {0} unneeded {1}.`,
  'Cli.NoMoveDestination': `Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.)`,
  'Cli.MoveDestinationInsideInstaller': `Error: destination cannot be inside the Windows Installer folder.`,
  'Cli.MoveDestinationRelative': `Error: destination must be a fully qualified path. Got: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning.`,
  'Cli.MovingFiles': `Moving {0} unneeded {1} to {2}...`,
  'Cli.MovedFiles': `Moved {0} unneeded {1}.`,
  'Cli.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again.`,
  'Cli.MutexBlocked': `Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later.`,
  'Cli.EventLogUnavailable': `Note: Event Log writing failed. Check Application log permissions or Group Policy.`,
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
  'Cli.Help.Header': `InstallerClean - clean up {InstallerFolder}`,
  'Cli.Help.Summary': `Removes cached .msi and .msp files that no installed program still needs.`,
  'Cli.Help.Elevation': `Needs an elevated (administrator) prompt; Windows will not start it.`,
  'Cli.Help.Usage': `Usage:`,
  'Cli.Help.Help': `  installerclean-cli --help     Show this help (also accepts /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  Print the version (also accepts -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Move to the saved backup folder`,
  'Cli.Help.MovePath': `  installerclean-cli /m PATH    Move to specified path`,
  'Cli.Help.NoteLine1': `installerclean-cli blocks the prompt until it finishes, so a script or&#10;scheduled task can wait on it.`,
  'Cli.Help.MoveScheduledNote': `That folder is saved per-user; scheduled or SYSTEM runs need /m PATH.`,
  'Cli.Help.ExitCodesHeader': `Exit codes:`,
  'Cli.Help.ExitCodeOk': `  0   success: the run finished with nothing left to do`,
  'Cli.Help.ExitCodeError': `  1   failure: nothing processed (bad arguments, a bad destination, a&#10;       failed scan or every file failed)`,
  'Cli.Help.ExitCodePartial': `  2   partial: some processed, some not (a failure or a Ctrl+C part way)`,
  'Cli.Help.ExitCodeTransient': `  75  transient: a temporary condition blocked the run (see the message)`,
  'Cli.Help.ExitCodeCancelled': `  130 cancelled (Ctrl+C)`,
  'Tooltip.ChangeLanguage': `Change language. The program will restart.`,
  'Automation.ChangeLanguage': `Change language`,
  'Automation.ChangeLanguage.HelpText': `The program will restart.`,
  'Tooltip.MoveSameDrive': `Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Confirm.DeletePermanently.Plural': `Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Cli.TooManyArgumentsNoPath': `Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run.`,
  'Cli.MissingFromDisk.Singular': `{0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it.`,
  'Cli.MissingFromDisk.Plural': `{0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them.`,
  'Cli.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Cli.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Cli.MoveNotEnoughSpace': `Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.Other': `Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes.`,
  'Cli.InstallerLockUnavailable': `Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the machine-contract Cli.* <data> elements BY NAME (the
// Cli.EventLog* set bar Cli.EventLogUnavailable): each is matched non-greedy to
// its own </data>. The human-facing Cli keys are KEPT, and their value is
// replaced from the MAP like any other key. Same predicate as
// scripts/check-resx-parity.mjs. The section comments left orphaned by a removed
// machine key (<!-- CLI output -->, the per-machine-key placeholder notes) are
// left in place deliberately: removing them needs fragile anchors that name
// specific keys, the exact step that broke before. They are harmless XML
// comments. Do NOT reintroduce comment surgery to "tidy" them.
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
let cliMachineRemoved = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (isMachineCliKey(name)) { cliMachineRemoved++; return ''; } return m; });

// Replace each key's inner <value> from MAP. The capture keeps the <data> tag,
// its attributes and the whitespace before <value>; any <comment> child and the
// </data> close sit outside the match. The closing quote anchors the name, so
// Status.MoveFailed never matches Status.MoveFailed.NoLog. A function replacement
// keeps $-sequences in a value from being read as backreferences.
const escRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const notApplied = [];
for (const [key, val] of Object.entries(MAP)) {
  const re = new RegExp('(<data name="' + escRe(key) + '"[^>]*>\\s*<value>)([\\s\\S]*?)(</value>)');
  let applied = false;
  text = text.replace(re, (m, p1, p2, p3) => { applied = true; return p1 + val + p3; });
  if (!applied) notApplied.push(key);
}

// Append the satellite-only override <data> elements before </root>. Values carry
// no XML-special characters (same as the MAP). Empty OVERRIDES means no block, so
// the output is byte-identical to a no-override language (e.g. Korean).
const overrideBlock = Object.entries(OVERRIDES)
  .map(([k, v]) => `  <data name="${k}" xml:space="preserve"><value>${v}</value></data>`)
  .join('\n');
if (overrideBlock) text = text.replace('</root>', overrideBlock + '\n</root>');

// Normalise to LF with exactly one trailing newline.
text = text.replace(/\r\n/g, '\n');
if (!text.endsWith('\n')) text += '\n';

writeFileSync(OUT, text, 'utf8');

// ---------------- self-check the written file against the neutral ----------------
const placeholders = (s) => new Set([...s.matchAll(/\{(\d+)(?::[^}]*)?\}/g)].map((p) => p[1]));
const parse = (xml) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'));
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written);
// Required = everything a satellite must carry: the non-Cli keys plus the
// human-facing Cli keys. The machine Cli keys are the complement; they must be
// absent from the output (isMachineCliKey is defined up in the strip section).
const neutralRequired = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

// Satellite-only overrides: present, and each sharing its base key's {N} set
// (base = the <Prefix>.Plural sibling if the neutral has one, else the flat key).
const overrideKeys = Object.keys(OVERRIDES);
const overrideMissing = overrideKeys.filter((k) => !output.has(k));
const overrideArityMismatch = overrideKeys.filter((k) => {
  if (!output.has(k)) return true;
  const base = k.replace(/\.(?:One|Few|Many)$/, '');
  const ref = neutral.has(`${base}.Plural`) ? `${base}.Plural` : base;
  if (!neutral.has(ref)) return true; // base must exist in the neutral
  const a = placeholders(neutral.get(ref)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});

const missingFromMap = neutralRequired.filter((k) => !(k in MAP));
const strayMapKeys = Object.keys(MAP).filter((k) => !neutral.has(k));
const machineLeaked = [...output.keys()].filter(isMachineCliKey);

// The one human-facing Cli.EventLog* key, asserted present rather than left to
// the counts: a predicate that stopped discriminating it takes it out of the
// output AND out of the required set, so every figure above still agrees. The
// MAP substitution notices today only through the order the two run in.
const humanCliStripped = !output.has('Cli.EventLogUnavailable');
const missingFromOutput = neutralRequired.filter((k) => !output.has(k));
const arityMismatch = neutralRequired.filter((k) => {
  if (!output.has(k)) return false; // already counted by missingFromOutput
  const a = placeholders(neutral.get(k)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});
const crlf = (written.match(/\r/g) || []).length;

// Untranslated-phrase gate (KEY-based, HARD): a value still byte-identical to the
// English neutral is a miss, UNLESS its key is a universal keep or in ALSO_KEEP.
const alsoKeep = new Set(ALSO_KEEP);
const untranslated = neutralRequired.filter((k) =>
  output.has(k) && output.get(k) === neutral.get(k) && !KEEP_ENGLISH.has(k) && !alsoKeep.has(k));

// The same gate turned around for the template, where English is the deliverable:
// a value that is NOT the neutral's is a neutral edit this file never took, and the
// next language would be seeded with the superseded wording. The key-set checks
// above catch an added or removed key; this catches a reworded one. Also empty here
// are the two per-language slots, which a copy would carry into a language that
// never chose them.
const templateDrift = IS_TEMPLATE
  ? neutralRequired.filter((k) => output.has(k) && output.get(k) !== neutral.get(k))
  : [];
const templateLanguageState = IS_TEMPLATE ? [...ALSO_KEEP, ...overrideKeys] : [];

// Breakdown computed, never pinned: the non-Cli and human-Cli totals both grow with
// every string the app gains, and a hardcoded pair goes stale silently while the
// checked figure beside it stays right.
const nonCliRequired = neutralRequired.filter((k) => !k.startsWith('Cli.')).length;
console.log('translatable <data> in output:', output.size,
  '(expect', neutralRequired.length + overrideKeys.length,
  '=', nonCliRequired, 'non-Cli +', neutralRequired.length - nonCliRequired, 'Cli +',
  overrideKeys.length, 'override)');
console.log('machine Cli <data> removed:', cliMachineRemoved, `(expect ${cliMachineExpected})`);
console.log('MAP entries:', Object.keys(MAP).length, '| override keys:', overrideKeys.length, '| CRLF:', crlf, '(expect 0)');

// ALSO_KEEP audit roster, so a lazy "force it green" dump is visible at a glance.
if (alsoKeep.size) {
  console.log('ALSO_KEEP (' + alsoKeep.size + '), kept identical to English:');
  for (const k of alsoKeep) {
    const v = output.get(k);
    const words = v == null ? 0 : v.replace(/\{\d+(?::[^}]*)?\}/g, ' ').trim().split(/\s+/).filter(Boolean).length;
    const suspicious = v != null && (words > 2 || v.length > 24);
    console.log('   ' + (suspicious ? '!! suspicious (longer than a word or two) ' : '') + k + ' = ' + JSON.stringify(v));
  }
}

if (notApplied.length) console.log('!! value not applied (regex miss):', notApplied);
if (missingFromMap.length) console.log('!! in neutral but missing from MAP:', missingFromMap);
if (strayMapKeys.length) console.log('!! in MAP but not in neutral:', strayMapKeys);
if (missingFromOutput.length) console.log('!! required key missing from output:', missingFromOutput);
if (arityMismatch.length) console.log('!! placeholder arity differs from neutral:', arityMismatch);
if (machineLeaked.length) console.log('!! machine Cli keys leaked into output:', machineLeaked);
if (humanCliStripped) console.log('!! Cli.EventLogUnavailable stripped: that key is human-facing and must stay');
if (overrideMissing.length) console.log('!! override key missing from output:', overrideMissing);
if (overrideArityMismatch.length) console.log('!! override arity differs from its base key:', overrideArityMismatch);
if (!IS_TEMPLATE && untranslated.length) {
  const show = untranslated.slice(0, 40).join(', ');
  console.log('!! still English (untranslated), ' + untranslated.length + ': ' + show +
    (untranslated.length > 40 ? ', ...and ' + (untranslated.length - 40) + ' more' : ''));
  if (untranslated.length > 50)
    console.log('   (that is most of the file: OUT still points at the template default, or the MAP is not translated yet.)');
}
if (IS_TEMPLATE) {
  console.log('template mode: MAP values holding the current English:',
    neutralRequired.length - templateDrift.length, 'of', neutralRequired.length);
  if (templateDrift.length) {
    console.log('!! MAP value superseded by a neutral edit, ' + templateDrift.length + ':', templateDrift);
    for (const k of templateDrift)
      console.log('   ' + k + '\n     template: ' + JSON.stringify(output.get(k)) +
        '\n     neutral : ' + JSON.stringify(neutral.get(k)));
  }
  if (templateLanguageState.length)
    console.log('!! per-language state in the template (ALSO_KEEP / OVERRIDES):', templateLanguageState);
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  !humanCliStripped &&
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === cliMachineExpected && crlf === 0;
const ok = structuralOk && (IS_TEMPLATE
  ? !templateDrift.length && !templateLanguageState.length
  : !untranslated.length);
const label = IS_TEMPLATE ? 'TEMPLATE' : 'GENERATION';
console.log(ok ? '\n' + label + ' OK' : '\n' + label + ' HAS ISSUES (see above)');
