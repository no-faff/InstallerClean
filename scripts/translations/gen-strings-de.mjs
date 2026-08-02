#!/usr/bin/env node
// German (de) satellite generator for InstallerClean. Copied from
// gen-strings-template.mjs (the full-resx template) and filled with German.
// It works FROM THE ENGLISH SOURCE (Strings.resx): it reads the neutral as the
// structural base, strips ONLY the machine-contract Cli.* keys, replaces the
// inner <value> of every other key from the MAP below, appends the satellite-only
// .One override(s), writes LF/UTF-8 and self-verifies against the neutral.
//
// German plural class: CategoryFor returns One only at n==1, else Other (the
// "default" branch, same selector as es/it). German past participles do NOT
// inflect for number (gefunden / gelöscht / verschoben are identical at 1 and
// many), so the three CLI completion lines es/it overrode (Cli.FoundOrphans,
// Cli.DeletedFiles, Cli.MovedFiles) need no override here. German DOES inflect
// the finite verb (wird/werden) and the attributive adjective
// ("1 registriertes Paket" vs "120 registrierte Pakete"), so there are THREE
// .One overrides: the attributive Status.RegisteredPackagesFound, and the two
// count-bearing command-line PROGRESS lines (Cli.DeletingFiles,
// Cli.MovingFiles), whose base is the werden-form and whose .One is the
// wird-form. Both are routed through DisplayHelpers.Pluralise, so the .One keys
// fire at n==1. The window's own progress headings needed the same pair until
// they stopped carrying a count: with no number in the sentence there is
// nothing for the verb to agree with, and German was the only language that had
// overridden them.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.de.resx`;

// Universal keeps: values identical in every language (brand names, the pure-
// placeholder string, the size/elapsed format strings). Do NOT edit per language.
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

// Per-language keeps: German words that are byte-identical to the English source
// and are the correct, natural German term, so their still-"English" value is not
// a miss. All single tokens: "Patches" (naturalised German plural of der Patch),
// "Details" (das Detail/die Details) and "Version" (das Version-loanword stem).
const ALSO_KEEP = [
  'Section.Registered.Patches',  // PATCHES
  'Field.Patches',               // Patches
  'Automation.Section.Patches',  // Patches
  'Action.Details',              // Details
  'Version.Display',             // Version {0}
];

// Satellite-only .One override(s). NOT in the neutral; appended before </root>.
// check-resx-parity.mjs allows each because its base (the flat key itself) is in
// the neutral. The value's {N} set matches the base key's set.
const OVERRIDES = {
  'Status.RegisteredPackagesFound.One': `{0} registriertes {1} gefunden.`,
  'Cli.DeletingFiles.One': `Deleting {0} unneeded {1}...`,
  'Cli.MovingFiles.One': `Moving {0} unneeded {1} to {2}...`,
};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Über`,
  'Window.Registered.Title': `Registrierte Dateien, die nicht gelöscht werden sollten`,
  'Window.Orphaned.Title': `Nicht benötigte Dateien, die bedenkenlos gelöscht werden können`,

  // Section headings
  'Section.Registered.Products': `PRODUKTE`,
  'Section.Registered.Patches': `PATCHES`,
  'Section.Registered.Details': `PRODUKTDETAILS`,
  'Section.Backup.Folder': `BACKUP FOLDER`,
  'Section.SayThanks': `DANKE SAGEN`,

  // Field labels (used in detail panels)
  'Field.Reason': `Grund`,
  'Field.Author': `Autor`,
  'Field.Application': `Anwendung`,
  'Field.Title': `Titel`,
  'Field.Subject': `Betreff`,
  'Field.Keywords': `Schlüsselwörter`,
  'Field.SigningCertificate': `Signaturzertifikat`,
  'Field.FileSize': `Dateigröße`,
  'Field.Comment': `Kommentar`,
  'Field.ProductName': `Produktname`,
  'Field.File': `Datei`,
  'Field.Size': `Größe`,
  'Field.Patches': `Patches`,

  // Placeholder shown for a registered package whose API ProductName is empty.
  'Field.UnknownProductName': `(unbekannt)`,
  'Field.PatchesOnly': `(nur Patches)`,
  'Field.Missing': `fehlt`,

  // Actions (button labels; underscore prefixes are WPF mnemonics)
  'Action.About': `_Über`,
  'Action.Copy': `Kopieren`,
  'Action.Cut': `Ausschneiden`,
  'Action.Paste': `Einfügen`,
  'Action.SelectAll': `Alles auswählen`,
  'Action.Browse': `_Durchsuchen...`,
  'Action.Cancel': `_Abbrechen`,
  'Action.CheckForUpdates': `Nach _Updates suchen`,
  'Action.Close': `_Schließen`,
  'Action.DeletePermanently': `Endgültig _löschen`,
  'Action.Done': `_Fertig`,
  'Action.Details': `Details`,
  'Action.BuyMeACuppa': `Spendier mir einen _Kaffee`,
  'Action.LeaveStarOnGitHub': `Einen Stern auf _GitHub hinterlassen`,
  'Action.Licence': `Apache-2.0-Lizenz`,
  'Action.Move': `_Verschieben`,
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
  'Action.OpenReleasePage': `_Release-Seite öffnen`,
  'Action.Rescan': `_Neu scannen`,
  'Action.ScanAgain': `Erneut _scannen`,
  'Action.SendResultLog': `Bericht senden`,
  'Action.SendResultLogConfirm': `_Senden`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa': `Spenden`,
  'Automation.BuyMeACuppa.About': `Spendier mir einen Kaffee`,
  'Automation.CancelOperation': `Vorgang abbrechen`,
  'Automation.CancelScan': `Scan abbrechen`,
  'Automation.CancelStartupScan': `Start-Scan abbrechen`,
  'Automation.Close': `Schließen`,
  'Automation.CloseWindow': `Fenster schließen`,
  'Automation.CloseResult': `Ergebnis schließen und zum Hauptfenster zurückkehren`,
  'Automation.LeaveStarOnGitHub.About': `Einen Stern auf github hinterlassen`,
  'Automation.Minimise': `Minimieren`,
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
  'Automation.ConfirmMove': `Verschieben legt die nicht benötigten Dateien in den gewählten Zielordner. Abbrechen lässt sie, wo sie sind.`,
  'Automation.SayThanks': `Danke sagen`,
  'Automation.ConfirmSendResultLog': `Senden übermittelt den angezeigten Bericht an No Faff. Abbrechen sendet nichts.`,
  'Automation.CheckForUpdates': `Nach Updates suchen`,
  'Automation.CheckForUpdates.HelpText': `Sucht auf der Release-Seite von github nach einer neueren Version.`,
  'Automation.UpdateAvailable.HelpText': `Öffne die Release-Seite, um die neuere Version herunterzuladen, oder brich ab, um die aktuelle Version zu behalten.`,
  'Automation.Licence.HelpText': `Öffnet die Lizenzdatei auf github.com in deinem Browser.`,
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `Produkte`,
  'Automation.Section.Patches': `Patches`,
  'Automation.Section.ProductDetails': `Produktdetails`,
  'Automation.BackupFolder': `Backup folder`,
  'Automation.OperationProgress': `Vorgangsfortschritt`,
  'Automation.RescanInstaller': `{InstallerFolder} erneut scannen`,
  'Automation.ScanningProgress': `Scan-Fortschritt`,
  'Automation.StartupScanProgress': `Fortschritt des Start-Scans`,
  'Automation.ViewOrphanedFiles': `Details, nicht benötigte Dateien`,
  'Automation.ViewOrphanedFiles.HelpText': `Zum Aufräumen verfügbar.`,
  'Automation.ViewRegisteredFiles': `Details, registrierte Dateien`,
  'Automation.ViewRegisteredFiles.HelpText': `Schreibgeschützte Übersicht.`,
  'Automation.SortStatus.Ascending': `Sortiert nach {0}, aufsteigend`,
  'Automation.SortStatus.Descending': `Sortiert nach {0}, absteigend`,
  'Automation.Scroll.ScanResults': `Scan-Ergebnisse`,
  'Automation.Scroll.ResultDetails': `Ergebnisdetails`,
  'Automation.Scroll.FileDetails': `Dateidetails`,
  'Automation.Scroll.DialogBody': `Dialogtext`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `Dateien, die nicht verarbeitet werden konnten`,
  'Automation.RegisteredMissingSeeAlso': `Erklärt diesen Ordner, und wie sich eine Datei wiederherstellen lässt, im README`,

  // Tooltips
  'Tooltip.BuyMeACuppa.About': `Das macht durstig!`,
  'Tooltip.CancellingPending': `Abbruch angefordert. InstallerClean wartet, bis der aktuelle Schritt einen Haltepunkt erreicht. Bei starker Datenträgeraktivität oder einem MSI-Datenbankaufruf kann das ein paar Sekunden dauern.`,
  'Tooltip.Close': `Schließen`,
  'Tooltip.LeaveStarOnGitHub.About': `Ein Stern hilft anderen, InstallerClean zu finden.`,
  'Tooltip.Minimise': `Minimieren`,
  'Tooltip.SendResultLog': `Ganz wie du magst, aber ich freue mich darüber. Sendet eine anonyme Zusammenfassung, die mir nur zeigt, ob es funktioniert und wie viel Platz die Leute freigeben. Auf dem nächsten Bildschirm siehst du vor dem Bestätigen, was gesendet wird.`,
  'Tooltip.SendResultLog.NothingFound': `Ganz wie du magst, aber ich freue mich darüber. Sendet eine anonyme Zusammenfassung, die mir nur zeigt, ob es funktioniert. Auf dem nächsten Bildschirm siehst du vor dem Bestätigen, was gesendet wird.`,
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Antragstellername aus dem eingebetteten Authenticode-Zertifikat. Die Zertifikatskette wurde nicht geprüft.`,

  // Body copy
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `Sie liegen in {InstallerFolder} und blieben zurück, als ein Programm deinstalliert wurde ({0}), ein neuerer Patch einen älteren ersetzt hat ({1}) oder der Herausgeber ihn zurückgezogen hat ({2}). InstallerClean listet nur Dateien auf, die Windows selbst als erledigt meldet.`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.NoFileSelected': `Wähle eine Datei, um Details anzuzeigen.`,
  'Body.NoProductSelected': `Wähle ein Produkt, um Details anzuzeigen.`,
  'Body.NoMetadata': `Keine Metadaten verfügbar.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.&#10;&#10;It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.&#10;&#10;To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `Das README [erklärt diesen Ordner], und wie sich eine Datei wiederherstellen lässt, mit Microsofts eigenen Worten.`,
  'Body.NoPatches': `(keine)`,

  // Reasons (Reason column in the orphaned-files list)
  'Reason.Orphaned': `Verwaist`,
  'Reason.Superseded': `Ersetzt`,
  'Reason.Obsoleted': `Veraltet`,

  // Status / progress text
  'Status.Scanning': `Scannen...`,
  'Status.Cancelling': `Wird abgebrochen...`,
  'Status.StartingScan': `Scan wird gestartet...`,
  'Status.QueryingApi': `Windows wird nach installierter Software gefragt...`,
  'Status.ScanningCache': `Installer-Cache-Ordner wird gescannt...`,
  'Status.EnumeratingProducts': `Installierte Produkte werden aufgezählt...`,
  'Status.CheckingRegistry': `Registrierung wird nach weiteren Paketen durchsucht...`,

  // 0 = registered package count, 1 = pluralised "package"/"packages"
  'Status.RegisteredPackagesFound': `{0} registrierte {1} gefunden.`,

  // 0 = elapsed time text (e.g. "1.2s")
  'Status.ScanComplete': `Scan abgeschlossen ({0})`,
  'Status.FoundProducts': `Lokale Pakete werden gescannt...`,

  // 0 = file count, 1 = pluralised noun ("file"/"files")
  'Status.FoundUnused': `{0} {1} gefunden, die du bedenkenlos löschen kannst.`,
  'Status.PreparingDestination': `Zielordner wird vorbereitet...`,

  // 0 = file count, 1 = pluralised noun. Routed through Pluralise: base is the
  // werden-form (n != 1), the .One override carries the wird-form.
  'Status.Moving': `Moving unneeded files...`,
  'Status.Deleting': `Deleting unneeded files...`,
  'Status.MoveCancelled.Partial': `Verschieben abgebrochen. {0} von {1} {2} verarbeitet.`,
  'Status.DeleteCancelled.Partial': `Löschen abgebrochen. {0} von {1} {2} verarbeitet.`,
  'Status.MoveFailed': `Verschieben fehlgeschlagen ({0}). Details in {1}.`,
  'Status.MoveFailed.NoLog': `Verschieben fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden.`,
  'Status.DeleteFailed': `Löschen fehlgeschlagen ({0}). Details in {1}.`,
  'Status.DeleteFailed.NoLog': `Löschen fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden.`,
  'Status.ScanAccessDenied': `Zugriff verweigert. Windows hat den Scan abgelehnt.`,
  'Status.ScanFailedDb': `Scan fehlgeschlagen: Die Einträge von Windows Installer konnten nicht gelesen werden.`,
  'Status.ScanCancelled': `Scan abgebrochen.`,
  'Status.Done': `Bereit`,
  'Status.ScanFailedDetails': `Scan fehlgeschlagen ({0}). Details in {1}.`,
  'Status.ScanFailedDetails.NoLog': `Scan fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden.`,

  // Completion screen
  'Completion.AllClean': `Alles sauber`,
  'Completion.NothingToCleanUp': `Nichts aufzuräumen in {InstallerFolder}`,
  'Completion.NothingToCleanUpReceipt': `{0} {1} in {2} gescannt`,

  // 0 = size freed (e.g. "120.5 MB")
  'Completion.Freed': `{0} freigegeben`,
  'Completion.Moved': `{0} verschoben`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `Nichts verschoben`,
  'Completion.NothingDeleted': `Nichts gelöscht`,
  'Completion.FailedCount.Singular': `{0} Datei von {1} konnte nicht verschoben werden.`,
  'Completion.FailedCount.Plural': `{0} Dateien von {1} konnten nicht verschoben werden.`,
  'Completion.FailedCountDelete.Singular': `{0} Datei von {1} konnte nicht gelöscht werden.`,
  'Completion.FailedCountDelete.Plural': `{0} Dateien von {1} konnten nicht gelöscht werden.`,

  // 0 = moved count, 1 = pluralised noun, 2 = destination path
  'Completion.MoveSummary.Singular': `{0} {1} verschoben nach: {2}`,
  'Completion.MoveSummary.Plural': `{0} {1} verschoben nach: {2}`,

  // 0 = deleted count, 1 = pluralised noun

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} Datei noch benötigt`,
  'Summary.RegisteredStillUsed.Plural': `{0} Dateien noch benötigt`,
  'Summary.OrphanedToCleanUp.Singular': `{0} nicht benötigte Datei zum Aufräumen`,
  'Summary.OrphanedToCleanUp.Plural': `{0} nicht benötigte Dateien zum Aufräumen`,
  'Summary.MissingFromDisk.Singular': `{0} registrierte Datei fehlt (nicht von InstallerClean gelöscht). Im Moment kein Problem, aber eine spätere Reparatur, Aktualisierung oder Deinstallation dieses Programms könnte fehlschlagen. Öffne Details, um zu erfahren, was zu tun ist.`,
  'Summary.MissingFromDisk.Plural': `{0} registrierte Dateien fehlen (nicht von InstallerClean gelöscht). Im Moment kein Problem, aber eine spätere Reparatur, Aktualisierung oder Deinstallation dieser Programme könnte fehlschlagen. Öffne Details, um zu erfahren, was zu tun ist.`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0} von {1} {2}`,

  // Orphaned-window footer. 0 = orphaned count, 1 = superseded count,
  // 2 = obsoleted count, 3 = size display. Predicative adjectives, invariant.
  'Summary.OrphanedWindow': `{0} verwaist, {1} ersetzt, {2} veraltet ({3})`,

  // Registered-window footer, split so noun and verb agree. 0 = count, 1 = size.
  'Summary.RegisteredWindow.Singular': `{0} registrierte Datei, die noch benötigt wird ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} registrierte Dateien, die noch benötigt werden ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `{0} {1} verschieben ({2})?`,

  // 0 = destination path
  'Confirm.MoveDestination': `Die Dateien werden verschoben nach:`,
  'Confirm.DeleteTitle': `{0} {1} löschen ({2})?`,

  // Error messages
  'Error.AdminRequiredTitle': `Zugriff verweigert`,
  'Error.AdminRequiredBody': `Windows hat InstallerClean den Zugriff verweigert, deshalb hat es abgebrochen. Es wurde nichts entfernt.\n\nInstallerClean lief bereits als Administrator, es noch einmal so zu starten hilft also nicht. Windows sagt nicht genauer, was den Zugriff verweigert hat, es gibt also nichts Bestimmtes, das du versuchen könntest.`,
  'Error.InstallerDbUnavailableTitle': `Die Einträge von Windows Installer konnten nicht gelesen werden`,
  'Error.ScanFailedTitle': `Scan fehlgeschlagen`,
  'Error.InstallerDbEmpty': `Die Einträge von Windows Installer kamen völlig leer zurück: Nicht ein einziges installiertes Programm und kein einziges Update beansprucht eine zwischengespeicherte Installationsdatei. Auf einem funktionierenden System kommt das nicht vor (selbst eine frische Windows-Installation hat welche), also sind die Einträge entweder beschädigt oder sie konnten nicht gelesen werden, und ein Scan, der diese Antwort glaubt, würde jede Datei in {InstallerFolder} fälschlich als verwaist einstufen. InstallerClean hat stattdessen abgebrochen. Es wurde nichts entfernt.`,
  'Error.MsiAccessDenied': `Windows Installer hat es InstallerClean verweigert, die installierte Software aufzulisten. InstallerClean lief bereits als Administrator, es erneut als Administrator auszuführen ändert also nichts. Ohne diese Liste lässt sich nicht sicher sagen, welche zwischengespeicherten Dateien noch gebraucht werden, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt.`,
  'Error.MsiNonSuccess': `Windows Installer konnte InstallerClean keine lesbare Liste der installierten Programme geben: {0} Einträge in Folge kamen unlesbar zurück (letzter Fehlercode {1}). Statt mit einer nur teilweise gelesenen Liste zu arbeiten, hat InstallerClean abgebrochen. Es wurde nichts entfernt.`,
  'Error.InvalidDestinationTitle': `Ungültiges Ziel`,
  'Error.DestinationWriteFailedTitle': `Schreiben am Ziel nicht möglich`,
  'Error.MoveFailedTitle': `Verschieben fehlgeschlagen`,
  'Error.DeleteFailedTitle': `Löschen fehlgeschlagen`,
  'Error.SettingNotSavedTitle': `Einstellung nicht gespeichert`,
  'Error.SettingNotSavedBody': `Die Änderung konnte nicht gespeichert werden. Beim nächsten Start verwendet InstallerClean wieder die vorherige Einstellung.`,
  'Error.DestinationInsideInstaller': `Das Ziel darf nicht im Windows-Installer-Ordner liegen.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Nicht genügend Speicherplatz`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `Nicht genügend Speicherplatz unter {0}\n\nBenötigt: {1}\nVerfügbar: {2}`,

  // 0 = destination
  'Error.AccessDeniedDestination': `Du hast keine Berechtigung, in {0} zu schreiben.\nVersuch einen Ordner in deinem Benutzerprofil oder auf einem Laufwerk, das dir gehört.`,
  'Error.PathTooLong': `Der Pfad {0} ist zu lang für Windows. Wähle einen kürzeren Pfad.`,
  'Error.DestinationMissing': `Der Ordner {0} existiert nicht und konnte nicht erstellt werden. Prüfe den Laufwerkbuchstaben oder den Netzwerkpfad.`,
  'Error.IOWriteDestination': `Windows kann nicht in {0} schreiben.\nDetails in {1}.`,
  'Error.IOWriteDestination.NoLog': `Windows kann nicht in {0} schreiben. Das Absturzprotokoll konnte nicht geschrieben werden.`,
  'Error.WriteDestination': `Schreiben in {0} nicht möglich.\nDetails in {1}.`,
  'Error.WriteDestination.NoLog': `Schreiben in {0} nicht möglich. Das Absturzprotokoll konnte nicht geschrieben werden.`,
  'Error.MissingSourceFile': `Die Datei existiert nicht mehr.`,
  'Error.SourceIsReparsePoint': `Die Quelldatei ist ein Symlink oder eine Junction; aus Sicherheitsgründen abgelehnt.`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows hat den Zugriff auf diese Datei verweigert; sie wurde an ihrem Platz belassen.`,
  'Error.AccessDenied.Plural': `Windows hat den Zugriff auf diese Dateien verweigert; sie wurden an ihrem Platz belassen.`,
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `Windows hat einen Dateifehler gemeldet; die Datei wurde an ihrem Platz belassen.`,
  'Error.IOFailure.Plural': `Windows hat Dateifehler gemeldet; diese Dateien wurden an ihrem Platz belassen.`,
  'Error.UnknownError.Singular': `Bei dieser Datei ist etwas schiefgelaufen; sie wurde an ihrem Platz belassen.`,
  'Error.UnknownError.Plural': `Bei diesen Dateien ist etwas schiefgelaufen; sie wurden an ihrem Platz belassen.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Das Verschieben von Dateien in den Windows-Installer-Ordner wird abgelehnt (Ziel: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\\Backup, or \\\\server\\backup). InstallerClean can't use this one: {0}`,
  'BrowserLaunch.FailedTitle': `Browser konnte nicht geöffnet werden`,
  'UpdateCheck.Title': `Nach Updates suchen`,
  'UpdateCheck.Status.Checking': `Wird geprüft...`,
  'UpdateCheck.Status.UpToDate': `Auf dem neuesten Stand.`,
  'UpdateCheck.UpdateAvailable.Title': `Update verfügbar`,

  // 0 = installed version, 1 = latest version on GitHub
  'UpdateCheck.UpdateAvailable.Body': `Du verwendest Version {0}.&#10;Version {1} ist verfügbar.`,
  'UpdateCheck.Failed.NetworkUnavailable': `GitHub war nicht erreichbar. Prüfe deine Internetverbindung und versuch es erneut.`,
  'UpdateCheck.Failed.ServerError': `GitHub hat eine Fehlerantwort zurückgegeben. Versuch es in ein paar Minuten erneut.`,
  'UpdateCheck.Failed.ResponseParseError': `Die Antwort von GitHub enthielt keine erkennbare Version. Versuch es später erneut oder öffne die Releases-Seite direkt.`,
  'UpdateCheck.Failed.Timeout': `Bei der Prüfung wurde das Zeitlimit überschritten. Deine Verbindung zu GitHub ist vielleicht langsam; versuch es erneut.`,
  'UpdateCheck.Failed.Unknown': `Die Prüfung ist aus unbekanntem Grund fehlgeschlagen. Details stehen in crash.log, falls du es melden möchtest.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `InstallerClean konnte deinen Browser nicht öffnen. Der Link liegt in deiner Zwischenablage, du kannst ihn also selbst einfügen:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean konnte deinen Browser nicht öffnen und den Link auch nicht in die Zwischenablage kopieren. Der Link lautet:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again.`,

  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Schreiben in {0} nicht möglich.`,

  // 0 = file name
  'Error.NoUniqueFilename': `Nach 10.000 Versuchen konnte kein eindeutiger Dateiname für '{0}' gefunden werden.`,

  // Result log (post-cleanup diagnostic send)
  'ResultLog.Sending': `Wird gesendet...`,
  'ResultLog.Sent': `Danke! Bericht gesendet.`,
  'ResultLog.Failed': `Senden fehlgeschlagen. Versuch es später erneut.`,
  'ResultLog.NothingToSend': `Kein Bericht zum Senden.`,
  'ConfirmSendResultLog.Title': `Das senden?`,
  'ConfirmSendResultLog.Reassurance': `Es geht an nofaff.netlify.app/api/result-log. Nichts identifiziert dich oder deinen Rechner; es zeigt mir nur, dass InstallerClean funktioniert und [wie viel Platz die Leute freigeben].`,
  'Automation.ResultLogPreview': `Vorschau des Berichts`,

  // Single instance / startup / crash
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean läuft bereits.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `Ein unerwarteter Fehler ist aufgetreten und InstallerClean muss geschlossen werden.\n\n{0}\n\nDetails gespeichert unter:\n{1}`,
  'Startup.UnhandledBody.NoLog': `Ein unerwarteter Fehler ist aufgetreten und InstallerClean muss geschlossen werden.\n\n{0}\n\nDas Absturzprotokoll konnte nicht geschrieben werden.`,
  'Startup.ErrorTitle': `Startfehler`,
  'Startup.FailedToStart': `Start fehlgeschlagen ({0}). Details gespeichert unter:\n{1}`,
  'Startup.FailedToStart.NoLog': `Start fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden.`,

  // File picker
  'FilePicker.ChooseDestinationTitle': `Zielordner für verschobene Dateien wählen`,

  // Version display

  // 0 = major.minor.patch (e.g. "1.5.4")
  'Version.Display': `Version {0}`,
  'Plural.File.Singular': `Datei`,
  'Plural.File.Plural': `Dateien`,
  'Plural.Error.Singular': `Fehler`,
  'Plural.Error.Plural': `Fehler`,
  'Plural.Package.Singular': `Paket`,
  'Plural.Package.Plural': `Pakete`,
  'Plural.Product.Singular': `Produkt`,
  'Plural.Product.Plural': `Produkte`,
  'Plural.Patch.Singular': `Patch`,
  'Plural.Patch.Plural': `Patches`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ElapsedLong.LessThanASecond': `weniger als eine Sekunde`,
  'Display.ElapsedLong.Seconds': `{0:F1} Sekunden`,
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
  'Tooltip.ChangeLanguage': `Sprache ändern. Das Programm wird neu gestartet.`,
  'Automation.ChangeLanguage': `Sprache ändern`,
  'Automation.ChangeLanguage.HelpText': `Das Programm wird neu gestartet.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  'Cli.UnknownArgument': `Error: unknown argument '{0}'`,
  'Cli.Cancelling': `Wird abgebrochen...`,
  'Cli.Cancelled': `Abgebrochen.`,
  'Cli.GenericError': `Error: unexpected failure ({0}). Details written to {1}.`,
  'Cli.GenericError.NoLog': `Error: unexpected failure ({0}). The crash log could not be written.`,
  'Cli.ScanningInstaller': `{InstallerFolder} wird gescannt...`,
  'Cli.FoundOrphans': `Found {0} unneeded {1} to clean up ({2}).`,
  'Cli.DeletingFiles': `Deleting {0} unneeded {1}...`,
  'Cli.DeletedFiles': `Permanently deleted {0} unneeded {1}.`,
  'Cli.NoMoveDestination': `Fehler: Kein Zielordner zum Verschieben angegeben. Nutze /m PFAD. (Ein in der GUI gesetztes Standardziel gilt pro Benutzer und greift nicht bei geplanten oder Dienstkonto-Läufen.)`,
  'Cli.MoveDestinationInsideInstaller': `Fehler: Das Ziel darf nicht im Windows-Installer-Ordner liegen.`,
  'Cli.MoveDestinationRelative': `Fehler: Das Ziel muss ein vollständig qualifizierter Pfad sein. Erhalten: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning.`,
  'Cli.MovingFiles': `Moving {0} unneeded {1} to {2}...`,
  'Cli.MovedFiles': `Moved {0} unneeded {1}.`,
  'Cli.MutexBlocked': `Ein anderer InstallerClean-Prozess hält die Einzelinstanz-Sperre (die GUI oder ein anderer CLI-Lauf). Exit-Code 75 (vorübergehend); ein späterer Wiederholungsversuch ist sicher.`,
  'Cli.EventLogUnavailable': `Hinweis: Das Schreiben in das Ereignisprotokoll ist fehlgeschlagen. Prüfe die Berechtigungen des Anwendungsprotokolls oder die Gruppenrichtlinie.`,
  'Cli.Help.Header': `InstallerClean - {InstallerFolder} aufräumen`,
  'Cli.Help.Usage': `Verwendung:`,
  'Cli.Help.Help': `  installerclean-cli --help     Diese Hilfe anzeigen (auch /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  Die Version ausgeben (auch -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Move to the saved backup folder`,
  'Cli.Help.MovePath': `  installerclean-cli /m PFAD    An den angegebenen Pfad`,
  'Cli.Help.NoteLine1': `installerclean-cli blocks the prompt until it finishes, so a script or&#10;scheduled task can wait on it.`,
  'Cli.Help.ExitCodesHeader': `Exit-Codes:`,
  'Cli.Help.ExitCodeOk': `  0   success: the run finished with nothing left to do`,
  'Cli.Help.ExitCodeError': `  1   failure: nothing processed (bad arguments, a bad destination, a&#10;       failed scan or every file failed)`,
  'Cli.Help.ExitCodePartial': `  2   partial: some processed, some not (a failure or a Ctrl+C part way)`,
  'Cli.Help.ExitCodeTransient': `  75  vorübergehend: etwas hat den Lauf blockiert (siehe Meldung)`,
  'Cli.Help.ExitCodeCancelled': `  130 abgebrochen (Strg+C)`,
  'Body.NotScanned.Lead': `Noch nichts gescannt.`,
  'Body.NotScanned.Why': `Klicke auf „Neu scannen“, um {InstallerFolder} nach Installer-Dateien zu durchsuchen, die kein Programm mehr braucht.`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,
  'Error.ScanCorrelationFailed': `InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed.`,
  'Error.CandidateOutsideCache': `Diese Datei liegt nicht direkt im Windows-Installer-Ordner; aus Sicherheitsgründen abgelehnt.`,
  'Completion.ReverifySkipped': `{0} {1} an Ort und Stelle belassen, weil ein Programm sie nach dem Scan wieder benötigt.`,
  'Completion.MoveCancelledSummary': `{0} von {1} {2} verschoben, bevor du abgebrochen hast.`,
  'Completion.PermanentDeleteCancelledSummary': `{0} von {1} {2} endgültig gelöscht, bevor du abgebrochen hast.`,
  'Body.PendingReboot.Lead': `Diese Dateien können gerade nicht aufgeräumt werden.`,
  'Cli.TooManyArguments': `Fehler: Unerwartetes zusätzliches Argument '{0}'. Wenn dein Zielordner ein Leerzeichen enthält, setze den ganzen Pfad in Anführungszeichen: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `That folder is saved per-user; scheduled or SYSTEM runs need /m PATH.`,
  'Completion.ReverifyIncomplete': `{0} {1} an Ort und Stelle belassen, weil die Einträge von Windows Installer bei der wiederholten Prüfung nicht vollständig gelesen werden konnten.`,
  'Summary.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Summary.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Error.ScanRecordsUnreadable': `InstallerClean konnte nicht genug von den Einträgen von Windows Installer lesen, um sicher zu sein, was noch gebraucht wird: Die Liste der installierten Programme kam unvollständig zurück, und dieselben Einträge direkt aus der Registrierung zu lesen führte ebenfalls zu Fehlern. Eine Datei könnte allein deshalb verwaist wirken, weil der Eintrag, der sie nennt, zu den unlesbaren gehörte, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer hat das Ende der Liste der installierten Programme nie signalisiert: InstallerClean hat nach {0} Einträgen aufgegeben (letzter Fehlercode {1}). Einer Liste ohne Ende ist nicht zu trauen, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer hat das Ende der Patch-Liste eines Programms nie signalisiert: InstallerClean hat nach {0} Einträgen aufgegeben (letzter Fehlercode {1}). Einer Liste ohne Ende ist nicht zu trauen, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt.`,
  'UpdateCheck.Status.UpdateAvailable': `Version {0} ist verfügbar.`,
  'Completion.DonateAsk': `Freut mich, dass es geholfen hat. Die Kaffeekasse steht bereit, falls dir großzügig zumute ist.`,
  'About.Link.Guide': `Anleitung und FAQ`,
  'About.Link.ReportProblem': `Ein Problem melden`,
  'About.AutoUpdateCheck': `Automatisch nach Updates suchen`,
  'Automation.About.Guide.HelpText': `Öffnet das readme auf github in deinem Browser.`,
  'Automation.About.ReportProblem.HelpText': `Öffnet den Issue-Tracker auf github.com in deinem Browser.`,
  'Automation.AutoUpdateCheck.HelpText': `Wenn aktiviert, sucht InstallerClean beim Start auf github nach einer neueren Version.`,
  'Tooltip.MoveSameDrive': `Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Confirm.DeletePermanently.Plural': `Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed.`,
  'Automation.Scroll.ProductDetails': `Product details`,
  'Body.PendingReboot.Other': `Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back.`,
  'Cli.TooManyArgumentsNoPath': `Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run.`,
  'Cli.MissingFromDisk.Singular': `{0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it.`,
  'Cli.MissingFromDisk.Plural': `{0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them.`,
  'Cli.MoveNotEnoughSpace': `Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.Other': `Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes.`,
  'Cli.FoundNoOrphans': `Found no unneeded files.`,
  'Cli.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again.`,
  'Cli.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Cli.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Cli.Help.Summary': `Removes cached .msi and .msp files that no installed program still needs.`,
  'Cli.Help.Elevation': `Needs an elevated (administrator) prompt; Windows will not start it.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the machine-contract Cli.* <data> elements BY NAME (the
// Cli.EventLog* set bar Cli.EventLogUnavailable). The human Cli keys stay and
// are translated from MAP. Same predicate as scripts/check-resx-parity.mjs.
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
let cliMachineRemoved = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (isMachineCliKey(name)) { cliMachineRemoved++; return ''; } return m; });

// Replace each key's inner <value> from MAP. The closing quote anchors the name,
// so Status.MoveFailed never matches Status.MoveFailed.NoLog. A function
// replacement keeps $-sequences in a value from being read as backreferences.
const escRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const notApplied = [];
for (const [key, val] of Object.entries(MAP)) {
  const re = new RegExp('(<data name="' + escRe(key) + '"[^>]*>\\s*<value>)([\\s\\S]*?)(</value>)');
  let applied = false;
  text = text.replace(re, (m, p1, p2, p3) => { applied = true; return p1 + val + p3; });
  if (!applied) notApplied.push(key);
}

// Append the satellite-only .One override <data> elements before </root>.
const overrideBlock = Object.entries(OVERRIDES)
  .map(([k, v]) => `  <data name="${k}" xml:space="preserve"><value>${v}</value></data>`)
  .join('\n') + '\n';
text = text.replace('</root>', overrideBlock + '</root>');

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
// Required = the non-Cli keys plus the human-facing Cli keys.
const neutralRequired = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

// The output also carries the satellite-only .One override key(s). Each must be
// present and share its base key's placeholder set (base = the .Plural sibling if
// it exists, else the flat key itself; mirrors check-resx-parity.mjs).
const overrideKeys = Object.keys(OVERRIDES);
const overrideMissing = overrideKeys.filter((k) => !output.has(k));
const overrideArityMismatch = overrideKeys.filter((k) => {
  if (!output.has(k)) return true;
  const base = k.replace(/\.(?:One|Few|Many)$/, '');
  const ref = neutral.has(`${base}.Plural`) ? `${base}.Plural` : base;
  if (!neutral.has(ref)) return true;
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
if (untranslated.length) {
  const show = untranslated.slice(0, 40).join(', ');
  console.log('!! still English (untranslated), ' + untranslated.length + ': ' + show +
    (untranslated.length > 40 ? ', ...and ' + (untranslated.length - 40) + ' more' : ''));
  if (untranslated.length > 50)
    console.log('   (that is most of the file: this is the untranslated template. Translate the MAP values, then a real miss is listed on its own.)');
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  !humanCliStripped &&
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === cliMachineExpected && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
