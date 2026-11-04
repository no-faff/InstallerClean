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
// the finite verb (wird/werden), so there are TWO .One overrides, both of them
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

// Universal keeps: keys whose value is the same in every language, the brand names
// and the pure-placeholder announcement string. Their still-English value is NOT a
// miss. Explicit by KEY on purpose: a future brand key then defaults to "flag until
// someone adds it here", never silently passes. Do NOT translate these values. Do
// NOT edit this list per language.
//
// The five size suffixes and the two elapsed suffixes do not belong in this list,
// because they are not universal: French writes To/Go/Mo/Ko/o, Russian and Ukrainian
// write ТБ/ГБ/МБ/КБ/Б and мс/с. Those three carry real
// values in their MAP; the languages that do abbreviate as English does keep them in
// ALSO_KEEP, which is the per-language list. Display.ListSeparator is the same shape,
// for the same reason.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
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
  // The list separator German uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only Japanese and Chinese differ, taking the ideographic
  // comma.
  'Display.ListSeparator',       // ", "
  // The stop that ends a heading before the body is the one English uses. A
  // punctuation mark rather than a word, so there is nothing to translate and
  // nothing to get wrong; only Japanese and Chinese differ, taking the
  // ideographic full stop, which carries no space after it.
  'Display.SentenceSeparator',   // ". "
  // The colon between a link's words and its address on the command line is the
  // one English uses. A punctuation mark rather than a word, so there is nothing to
  // translate and nothing to get wrong; French puts a narrow no-break space before
  // it, and Japanese and Chinese take the full-width colon.
  'Cli.UpdateDownloadLink',      // "{0}: {1}"
  // The size and elapsed unit suffixes. German abbreviates them exactly as
  // English does, so there is nothing to translate and nothing to get wrong.
  // A per-language keep rather than a universal one because fr, ru and uk do
  // NOT: French takes To/Go/Mo/Ko/o, Russian and Ukrainian take ТБ/ГБ/МБ/КБ/Б and
  // мс/с, and all three carry real values in their MAP.
  'Display.Size.TB',           // {0} TB
  'Display.Size.GB',           // {0} GB
  'Display.Size.MB',           // {0} MB
  'Display.Size.KB',           // {0} KB
  'Display.Size.B',            // {0} B
  'Display.Elapsed.Ms',        // {0:F0}ms
  'Display.Elapsed.S',         // {0:F1}s
  // The command line's answer letter for no is N, the first letter of this language's
  // word for no.
  'Cli.UpdateAnswerNo',
];

// Satellite-only .One override(s). NOT in the neutral; appended before </root>.
// check-resx-parity.mjs allows each because its base (the flat key itself) is in
// the neutral. The value's {N} set matches the base key's set.
const OVERRIDES = {
  'Cli.DeletingFiles.One': `{0} nicht benötigte {1} wird gelöscht...`,
  'Cli.MovingFiles.One': `{0} nicht benötigte {1} wird nach {2} verschoben...`,
};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Über`,
  'Window.Registered.Title': `Unangetastete Dateien`,
  'Window.Orphaned.Title': `Nicht benötigte Dateien, die bedenkenlos gelöscht werden können`,

  // Section headings
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `PATCHES`,
  'Section.Registered.Details': `PRODUKTDETAILS`,
  'Section.Backup.Folder': `SICHERUNGSORDNER`,
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
  'Action.LeaveATip': `_Trinkgeld geben`,
  'Action.Donate': `_Trinkgeld geben`,
  'Action.DonateSmall': `Trinkgeld geben`,
  'Action.LeaveStarOnGitHub': `Einen Stern auf _GitHub hinterlassen`,
  'Action.Licence': `Apache-2.0-Lizenz`,
  'Action.Move': `_Verschieben`,
  'Action.BackupFolderPlaceholder': `Pfad zum Ordner, falls du verschiebst statt zu löschen.`,
  'Action.Rescan': `_Neu scannen`,

  // Automation names (screen reader / accessibility)
  'Automation.LeaveATip.About': `Trinkgeld geben`,
  'Automation.Donate': `Trinkgeld geben`,
  'Automation.DonateSmall': `Trinkgeld geben`,
  'Automation.CancelOperation': `Vorgang abbrechen`,
  'Automation.CancelScan': `Scan abbrechen`,
  'Automation.CancelStartupScan': `Start-Scan abbrechen`,
  'Automation.Close': `Schließen`,
  'Automation.CloseWindow': `Fenster schließen`,
  'Automation.CloseResult': `Ergebnis schließen und zum Hauptfenster zurückkehren`,
  'Automation.LeaveStarOnGitHub.About': `Einen Stern auf github hinterlassen`,
  'Automation.Minimise': `Minimieren`,
  'Automation.ConfirmDelete': `Endgültig löschen entfernt die nicht benötigten Dateien. Abbrechen schließt das Fenster, ohne zu löschen.`,
  'Automation.ConfirmMove': `Verschieben legt die nicht benötigten Dateien in den gewählten Zielordner. Abbrechen lässt sie, wo sie sind.`,
  'Automation.SayThanks': `Danke sagen`,
  'Automation.CheckForUpdates': `Nach Updates suchen`,
  'Automation.CheckForUpdates.HelpText': `Sucht auf der Release-Seite von github nach einer neueren Version.`,
  'Automation.Licence.HelpText': `Öffnet die Lizenzdatei auf github.com in deinem Browser.`,
  'Automation.Section.BackupFolder': `Sicherungsordner`,
  'Automation.Section.Patches': `Patches`,
  'Automation.Section.ProductDetails': `Produktdetails`,
  'Automation.BackupFolder': `Sicherungsordner`,
  'Automation.OperationProgress': `Vorgangsfortschritt`,
  'Automation.RescanInstaller': `{InstallerFolder} erneut scannen`,
  'Automation.ScanningProgress': `Scan-Fortschritt`,
  'Automation.StartupScanProgress': `Fortschritt des Start-Scans`,
  'Automation.ViewOrphanedFiles': `Details, nicht benötigte Dateien`,
  'Automation.ViewOrphanedFiles.HelpText': `Zum Aufräumen verfügbar.`,
  'Automation.ViewRegisteredFiles': `Details, unangetastete Dateien`,
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
  'Tooltip.LeaveATip.About': `Über jedes Trinkgeld freue ich mich sehr`,
  'Tooltip.CancellingPending': `Abbruch angefordert. InstallerClean wartet, bis der aktuelle Schritt einen Haltepunkt erreicht. Bei starker Datenträgeraktivität oder einem MSI-Datenbankaufruf kann das ein paar Sekunden dauern.`,
  'Tooltip.Close': `Schließen`,
  'Tooltip.LeaveStarOnGitHub.About': `Ein Stern hilft anderen, InstallerClean zu finden`,
  'Tooltip.Minimise': `Minimieren`,
  'Tooltip.Move': `Verschiebt die nicht benötigten Dateien in den Sicherungsordner`,
  'Tooltip.MoveNeedsDestination': `Verschiebt die nicht benötigten Dateien in einen Sicherungsordner. Den wählst du gleich aus.`,
  'Tooltip.Delete': `Löscht die nicht benötigten Dateien endgültig. Nimm stattdessen Verschieben, wenn du dich erst überzeugen möchtest, dass alles in Ordnung ist.`,
  'Tooltip.SigningCertificate': `Antragstellername aus dem eingebetteten Authenticode-Zertifikat. Die Zertifikatskette wurde nicht geprüft.`,

  // Body copy
  'Body.MainExplanation.Lead': `Alle nicht benötigten Dateien unten kannst du bedenkenlos löschen.`,
  'Body.MainExplanation.Why': `Sie liegen in {InstallerFolder}. InstallerClean fragt Windows nach jedem installierten Programm: Eine Datei wird aufgeführt, wenn kein Programm sie beansprucht ({0}) oder wenn ein neuerer Patch sie ersetzt hat und kein Programm auf sie zurückgehen könnte ({1}).`,
  'Body.MainExplanation.Action': `Verschiebe sie in einen Sicherungsordner deiner Wahl und lösche diesen Ordner, sobald du dich überzeugt hast, dass deine Programme sich weiterhin wie gewohnt aktualisieren und deinstallieren lassen. Wenn du sie zurück nach {InstallerFolder} legst, ist alles wieder da. Oder lösche sie jetzt endgültig.`,
  'Body.PendingReboot.MsiExecuteMutex': `Etwas nutzt gerade Windows Installer, etwa ein Windows-Update oder ein Programm, das im Hintergrund installiert wird. Verschieben und Löschen pausieren, solange das läuft, damit InstallerClean {InstallerFolder} nicht anfasst, während sich der Ordner ändert. Danach einmal neu scannen, und sie sind wieder da.`,
  'Body.PendingReboot.MsiExecuteMutexAccessRefused': `Windows hat InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt ist, deshalb pausieren Verschieben und Löschen, und InstallerClean fasst {InstallerFolder} nicht an. Starte Windows neu und wähle Neu scannen. Verweigert Windows es dann immer noch, ist das kein Rechner, den InstallerClean aufräumen kann.`,
  'Body.PendingReboot.MsiExecuteMutexUnreadable': `InstallerClean konnte nicht prüfen, ob Windows Installer beschäftigt ist, deshalb pausieren Verschieben und Löschen, und InstallerClean fasst {InstallerFolder} nicht an. Starte Windows neu und wähle Neu scannen. Kann es dann immer noch nicht prüfen, ist das kein Rechner, den InstallerClean aufräumen kann.`,
  'Body.PendingReboot.MsiExecuteMutexUnreadableWindowsError': `InstallerClean konnte nicht prüfen, ob Windows Installer beschäftigt ist, weil Windows einen Fehler gemeldet hat: {0} (Fehler {1}). Verschieben und Löschen pausieren, und InstallerClean fasst {InstallerFolder} nicht an. Starte Windows neu und wähle Neu scannen. Kann es dann immer noch nicht prüfen, ist das kein Rechner, den InstallerClean aufräumen kann.`,
  'Body.PendingReboot.InstallerInProgress': `Auf diesem Rechner ist eine frühere Windows-Installer-Transaktion angehalten. Setze diese Installation fort oder mache sie rückgängig (oder starte Windows neu), bevor du {InstallerFolder} aufräumst.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows hat für den nächsten Neustart eine Dateiumbenennung eingeplant, die {InstallerFolder} betrifft. Starte Windows neu, bevor du aufräumst.`,
  'Body.NoFileSelected': `Wähle eine Datei, um Details anzuzeigen.`,
  'Body.NoProductSelected': `Wähle ein Produkt, um Details anzuzeigen.`,
  'Body.NoMetadata': `Keine Metadaten verfügbar.`,
  'Body.RegisteredMissingFromDisk': `Diese Installationsdatei fehlt. Das macht im Moment keine Probleme und wird auch keine machen, bis du eines Tages das zugehörige Programm aktualisieren oder deinstallieren willst. Dieser Schritt kann dann fehlschlagen, weil Windows nach dieser Datei sucht und sie nicht findet.\n\nUm sie zurückzuholen, brauchst du das Installationsprogramm genau der Version, die du bereits hast. Hol es beim Hersteller des Programms und führe es über deiner vorhandenen Installation aus. Eine neuere Version reicht nicht: Sie müsste zuerst die vorhandene entfernen, und genau dieser Schritt braucht diese Datei. Vorher zu deinstallieren funktioniert aus demselben Grund ebenfalls nicht. Das sollte die Datei wiederherstellen und deine Einstellungen unangetastet lassen, garantiert wird es von Microsoft aber nicht.`,
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
  'Status.CheckingRegistry': `Registrierung wird nach weiteren Paketen durchsucht...`,
  'Status.RegisteredPackagesFound': `Dateien werden mit den installierten Programmen abgeglichen...`,
  'Status.CheckingRemaining': `Übrige Dateien werden geprüft...`,

  // 0 = elapsed time text (e.g. "1.2s")
  'Status.ScanComplete': `Scan abgeschlossen ({0})`,

  // 0 = file count, 1 = pluralised noun ("file"/"files")
  'Status.FoundUnused': `{0} {1} gefunden, die du bedenkenlos löschen kannst.`,
  'Status.PreparingDestination': `Zielordner wird vorbereitet...`,

  // 0 = file count, 1 = pluralised noun. Routed through Pluralise: base is the
  // werden-form (n != 1), the .One override carries the wird-form.
  'Status.Moving': `Nicht benötigte Dateien werden verschoben...`,
  'Status.Deleting': `Nicht benötigte Dateien werden gelöscht...`,
  'Status.MoveCancelled.Partial': `Verschieben abgebrochen. {0} von {1} {2} verarbeitet.`,
  'Status.DeleteCancelled.Partial': `Löschen abgebrochen. {0} von {1} {2} verarbeitet.`,
  'Status.MoveFailed': `{0}. Details in {1}.`,
  'Status.MoveFailed.NoLog': `{0}. Das Absturzprotokoll konnte nicht geschrieben werden.`,
  'Status.DeleteFailed': `{0}. Details in {1}.`,
  'Status.DeleteFailed.NoLog': `{0}. Das Absturzprotokoll konnte nicht geschrieben werden.`,
  'Status.ScanAccessDenied': `Zugriff verweigert. Windows hat den Scan abgelehnt.`,
  'Status.ScanStopped': `Scan gestoppt.`,
  'Status.ScanCancelled': `Scan abgebrochen.`,
  'Status.Ready': `Bereit`,
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
  'Completion.FailedCount.Singular': `{0} Datei konnte nicht verschoben werden.`,
  'Completion.FailedCount.Plural': `{0} Dateien konnten nicht verschoben werden.`,
  'Completion.FailedCountDelete.Singular': `{0} Datei konnte nicht gelöscht werden.`,
  'Completion.FailedCountDelete.Plural': `{0} Dateien konnten nicht gelöscht werden.`,

  // 0 = moved count, 1 = pluralised noun, 2 = destination path
  'Completion.MoveSummary.Singular': `{0} {1} verschoben nach: {2}`,
  'Completion.MoveSummary.Plural': `{0} {1} verschoben nach: {2}`,

  // 0 = deleted count, 1 = pluralised noun

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} endgültig gelöscht`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} endgültig gelöscht`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} Datei unangetastet`,
  'Summary.RegisteredStillUsed.Plural': `{0} Dateien unangetastet`,
  'Summary.OrphanedToCleanUp.Singular': `{0} nicht benötigte Datei zum Aufräumen`,
  'Summary.OrphanedToCleanUp.Plural': `{0} nicht benötigte Dateien zum Aufräumen`,
  'Summary.MissingFromDisk.Singular': `Windows hat einen Eintrag für {0} Datei, die nicht in {InstallerFolder} liegt: {1}. Im Alltag macht das keine Probleme, aber ein Update oder eine Deinstallation dieses Programms kann fehlschlagen. Unter Details steht, was zu tun ist.`,
  'Summary.MissingFromDisk.Plural': `Windows hat Einträge für {0} Dateien, die nicht in {InstallerFolder} liegen: {1}. Im Alltag macht das keine Probleme, aber ein Update oder eine Deinstallation dieser Programme kann fehlschlagen. Unter Details steht, was zu tun ist.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `{0} weiteres Programm`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `{0} weitere Programme`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} Datei, zu der die Einträge kein Programm nennen`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} Dateien, zu denen die Einträge kein Programm nennen`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0} von {1} {2}`,

  // Orphaned-window footer. 0 = orphaned count, 1 = superseded count,
  // 2 = obsoleted count, 3 = size display. Predicative adjectives, invariant.
  'Summary.OrphanedWindow': `{0} nicht benötigte {1} ({2})`,

  // Registered-window footer, split so noun and verb agree. 0 = count, 1 = size.
  'Summary.RegisteredWindow.Singular': `{0} Datei unangetastet ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} Dateien unangetastet ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `{0} {1} verschieben ({2})?`,

  'Confirm.DeleteTitle': `{0} {1} löschen ({2})?`,

  // Error messages
  'Error.AdminRequiredTitle': `Zugriff verweigert`,
  'Error.AdminRequiredBody': `Windows hat InstallerClean den Zugriff verweigert, deshalb hat es abgebrochen. Es wurde nichts entfernt.\n\nInstallerClean lief bereits als Administrator, es noch einmal so zu starten hilft also nicht. Windows sagt nicht genauer, was den Zugriff verweigert hat, es gibt also nichts Bestimmtes, das du versuchen könntest.`,
  'Error.StoppedTitle': `InstallerClean gestoppt`,
  'Error.ScanFailedTitle': `Scan fehlgeschlagen`,
  'Error.InstallerDbEmpty': `Die Einträge von Windows Installer kamen völlig leer zurück: Nicht ein einziges installiertes Programm und kein einziges Update beansprucht eine zwischengespeicherte Installationsdatei. Auf einem funktionierenden System kommt das nicht vor (selbst eine frische Windows-Installation hat welche), also sind die Einträge entweder beschädigt oder sie konnten nicht gelesen werden, und ein Scan, der diese Antwort glaubt, würde jede Datei in {InstallerFolder} fälschlich als verwaist einstufen. InstallerClean hat stattdessen abgebrochen. Es wurde nichts entfernt.`,
  'Error.MsiAccessDenied': `Windows Installer hat es InstallerClean verweigert, die installierte Software aufzulisten. InstallerClean lief bereits als Administrator, es erneut als Administrator auszuführen ändert also nichts. Ohne diese Liste lässt sich nicht sicher sagen, welche zwischengespeicherten Dateien noch gebraucht werden, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt.`,
  'Error.MsiNonSuccess': `Windows Installer konnte InstallerClean keine lesbare Liste der installierten Programme geben: Es hat {1} {2} gelesen, dann kam ein Eintrag unlesbar zurück (Fehlercode {0}). Statt mit einer nur teilweise gelesenen Liste zu arbeiten, hat InstallerClean abgebrochen. Es wurde nichts entfernt.`,
  'Error.InvalidDestinationTitle': `Ungültiges Ziel`,
  'Error.DestinationWriteFailedTitle': `Sicherungsordner nicht verwendbar`,
  'Error.MoveFailedTitle': `Verschieben fehlgeschlagen`,
  'Error.DeleteFailedTitle': `Löschen fehlgeschlagen`,
  'Error.SettingNotSavedTitle': `Einstellung nicht gespeichert`,
  'Error.SettingNotSavedBody': `Die Änderung konnte nicht gespeichert werden. Beim nächsten Start verwendet InstallerClean wieder die vorherige Einstellung.`,
  'Error.DestinationInsideInstaller': `Das Ziel darf nicht im Windows-Installer-Ordner liegen.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `Das Ziel {0} liegt unterhalb eines Windows-Systemordners. Wähle einen Pfad außerhalb von %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% und %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Nicht genügend Speicherplatz`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `Unter {0} ist zu wenig Platz\n\nBenötigt: {1}\nVerfügbar: {2}`,

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
  'Error.FileInUse.Singular': `Diese Datei ist von einem anderen Programm geöffnet oder gesperrt, deshalb kann sie gerade nicht entfernt werden. Sie wurde an Ort und Stelle belassen; versuche es später noch einmal.`,
  'Error.FileInUse.Plural': `Diese Dateien sind von einem anderen Programm geöffnet oder gesperrt, deshalb können sie gerade nicht entfernt werden. Sie wurden an Ort und Stelle belassen; versuche es später noch einmal.`,
  'Error.IOFailure.Singular': `Windows hat einen Dateifehler gemeldet; die Datei wurde an ihrem Platz belassen.`,
  'Error.IOFailure.Plural': `Windows hat Dateifehler gemeldet; diese Dateien wurden an ihrem Platz belassen.`,
  'Error.UnknownError.Singular': `Bei dieser Datei ist etwas schiefgelaufen; sie wurde an ihrem Platz belassen.`,
  'Error.UnknownError.Plural': `Bei diesen Dateien ist etwas schiefgelaufen; sie wurden an ihrem Platz belassen.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Das Verschieben von Dateien in den Windows-Installer-Ordner wird abgelehnt (Ziel: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `Der Sicherungsordner muss ein vollständiger Pfad zu einem Ordner sein, beginnend mit einem Laufwerksbuchstaben oder einer Netzwerkfreigabe (zum Beispiel D:\\Backup oder \\\\server\\backup). InstallerClean kann diesen hier nicht verwenden: {0}`,
  'BrowserLaunch.FailedTitle': `Browser konnte nicht geöffnet werden`,
  'UpdateCheck.Title': `Nach Updates suchen`,
  'UpdateCheck.Status.Checking': `Wird geprüft...`,
  'UpdateCheck.Status.UpToDate': `Auf dem neuesten Stand.`,

  // 0 = installed version, 1 = latest version on GitHub
  'UpdateCheck.Failed.NetworkUnavailable': `GitHub war nicht erreichbar. Prüfe deine Internetverbindung und versuch es erneut.`,
  'UpdateCheck.Failed.ServerError': `GitHub hat eine Fehlerantwort zurückgegeben. Versuch es in ein paar Minuten erneut.`,
  'UpdateCheck.Failed.ResponseParseError': `Die Antwort von GitHub enthielt keine erkennbare Version. Versuch es später erneut oder öffne die Releases-Seite direkt.`,
  'UpdateCheck.Failed.Timeout': `Bei der Prüfung wurde das Zeitlimit überschritten. Deine Verbindung zu GitHub ist vielleicht langsam; versuch es erneut.`,
  'UpdateCheck.Failed.Unknown': `Die Prüfung ist aus unbekanntem Grund fehlgeschlagen. Details stehen in {0}, falls du es melden möchtest.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `Der Link ist in der Zwischenablage, du kannst ihn also selbst einfügen:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean konnte den Link auch nicht in die Zwischenablage kopieren, hier ist er:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `InstallerClean konnte den Sicherungsordner nicht mehr bestätigen und hat deshalb angehalten. Prüfe {0}, dann Neu scannen und noch einmal versuchen.`,

  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Schreiben in {0} nicht möglich.`,

  // 0 = file name
  'Error.DestinationCollision': `Eine Datei namens '{0}' liegt bereits im Sicherungsordner.`,

  // Result log (post-cleanup diagnostic send)

  // Single instance / startup / crash
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `Es läuft bereits.`,
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
  'Display.Size.TB': `{0} TB`,
  'Display.Size.GB': `{0} GB`,
  'Display.Size.MB': `{0} MB`,
  'Display.Size.KB': `{0} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ListSeparator': `, `,
  'Display.SentenceSeparator': `. `,
  'Display.ElapsedLong.LessThanASecond': `weniger als eine Sekunde`,
  'Display.ElapsedLong.Seconds': `{0:F1} Sekunden`,
  'CrashLog.PrivacyHeader': `# crash.log erfasst unbehandelte Ausnahmen von InstallerClean.\n# Mit erhöhten Rechten können die Ausnahmemeldungen des Frameworks\n# Dateipfade aus der laufenden Sitzung enthalten (auch Profile\n# anderer Benutzer, die Windows-Installer-Abfragen aufzählen).\n# Meldungen über Netzwerkfehler bei der Updateprüfung oder beim\n# Senden des Ergebnisprotokolls können die Ziel-URL und die\n# aufgelöste IP- oder Proxyadresse enthalten. Einträge über\n# unlesbare Windows-Installer-Einträge können eine Windows-Konto-SID\n# (S-1-5-21-...) und die Produktcodes installierter Software\n# enthalten.\n# Entferne alle drei Arten von Angaben, bevor du diese Datei an\n# einen öffentlichen Fehlerbericht anhängst.\n`,
  'Tooltip.ChangeLanguage': `Sprache ändern. Das Programm wird neu gestartet.`,
  'Automation.ChangeLanguage': `Sprache ändern`,
  'Automation.ChangeLanguage.HelpText': `Das Programm wird neu gestartet.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  'Cli.UnknownArgument': `Fehler: unbekanntes Argument '{0}'`,
  'Cli.Cancelling': `Wird abgebrochen...`,
  'Cli.Cancelled': `Abgebrochen.`,
  'Cli.GenericError': `Fehler: unerwarteter Absturz ({0}). Details in {1} geschrieben.`,
  'Cli.GenericError.NoLog': `Fehler: unerwarteter Absturz ({0}). Das Absturzprotokoll konnte nicht geschrieben werden.`,
  'Cli.ScanningInstaller': `{InstallerFolder} wird gescannt...`,
  'Cli.FoundOrphans': `{0} nicht benötigte {1} zum Aufräumen gefunden ({2}).`,
  'Cli.DeletingFiles': `{0} nicht benötigte {1} werden gelöscht...`,
  'Cli.DeletedFiles': `{0} nicht benötigte {1} endgültig gelöscht.`,
  'Cli.NoMoveDestination': `Fehler: Kein Zielordner zum Verschieben angegeben. Nutze /m PFAD. (Ein in der GUI gesetztes Standardziel gilt pro Benutzer und greift nicht bei geplanten oder Dienstkonto-Läufen.)`,
  'Cli.MoveDestinationInsideInstaller': `Fehler: Das Ziel darf nicht im Windows-Installer-Ordner liegen.`,
  'Cli.MoveDestinationRelative': `Fehler: Das Ziel muss ein vollständig qualifizierter Pfad sein. Erhalten: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Fehler: das Ziel {0} liegt unterhalb eines Windows-Systemordners. Wähle einen Pfad außerhalb von %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% und %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Fehler: etwas nutzt gerade Windows Installer, etwa ein Windows-Update oder ein Programm, das im Hintergrund installiert wird. /m und /d sind blockiert, solange das läuft. Versuche es erneut, sobald es fertig ist.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Fehler: auf diesem Rechner ist eine frühere Windows-Installer-Transaktion angehalten. Setze diese Installation fort oder mache sie rückgängig (oder starte Windows neu), bevor du {InstallerFolder} aufräumst.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Fehler: ein für den Neustart eingeplanter Dateivorgang betrifft {InstallerFolder} ({0}). Starte Windows neu, damit dieser Vorgang abgeschlossen wird, bevor du aufräumst.`,
  'Cli.MovingFiles': `{0} nicht benötigte {1} werden nach {2} verschoben...`,
  'Cli.MovedFiles': `{0} nicht benötigte {1} verschoben.`,
  'Cli.MutexBlocked': `Ein anderer InstallerClean-Prozess hält die Einzelinstanz-Sperre (die GUI oder ein anderer CLI-Lauf). Exit-Code 75 (vorübergehend); ein späterer Wiederholungsversuch ist sicher.`,
  'Cli.EventLogUnavailable': `Hinweis: Das Schreiben in das Ereignisprotokoll ist fehlgeschlagen. Prüfe die Berechtigungen des Anwendungsprotokolls oder die Gruppenrichtlinie.`,
  'Cli.Help.Header': `InstallerClean - {InstallerFolder} aufräumen`,
  'Cli.Help.Usage': `Verwendung:`,
  'Cli.Help.Help': `  installerclean-cli --help     Diese Hilfe anzeigen (auch /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  Die Version ausgeben (auch -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Nur scannen - nicht benötigte Dateien`,
  'Cli.Help.Delete': `  installerclean-cli /d         Nicht benötigte Dateien endgültig löschen`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         In den gespeicherten Sicherungsordner`,
  'Cli.Help.MovePath': `  installerclean-cli /m PFAD    An den angegebenen Pfad`,
  'Cli.Help.NoteLine1': `installerclean-cli blockiert die Eingabeaufforderung bis zum Ende,&#10;damit ein Skript oder eine geplante Aufgabe darauf warten kann.`,
  'Cli.Help.ExitCodesHeader': `Exit-Codes:`,
  'Cli.Help.ExitCodeOk': `  0   Erfolg: Der Lauf hat getan, worum gebeten wurde`,
  'Cli.Help.ExitCodeError': `  1   nichts verarbeitet: der Lauf schlug fehl oder wurde abgelehnt`,
  'Cli.Help.ExitCodePartial': `  2   teilweise: einiges verarbeitet, anderes nicht (Fehler oder Strg+C)`,
  'Cli.Help.ExitCodeTransient': `  75  vorübergehend: etwas hat den Lauf blockiert (siehe Meldung)`,
  'Cli.Help.ExitCodeCancelled': `  130 abgebrochen (Strg+C)`,
  'Body.NotScanned.Lead': `Noch nichts gescannt.`,
  'Body.NotScanned.Why': `Klicke auf „Neu scannen“, um {InstallerFolder} nach Installer-Dateien zu durchsuchen, die kein Programm mehr braucht.`,
  'Confirm.MoveSameDrive': `Dieser Ordner liegt auf demselben Laufwerk, der Platz kommt also erst zurück, wenn du ihn löschst. Wähle stattdessen einen Ordner auf einem anderen Laufwerk, wenn du den Platz sofort haben willst.`,
  'Error.ScanCorrelationFailed': `InstallerClean konnte die Windows-Installer-Einträge nicht mit dem Inhalt von {InstallerFolder} abgleichen. Fast nichts, worauf die Einträge zeigen, ist tatsächlich da, und fast nichts, was da ist, wird von einem Eintrag benannt, deshalb ließ sich für keine Datei zeigen, dass sie nicht benötigt wird. Es wurde nichts angeboten und nichts entfernt.`,
  'Error.CandidateOutsideCache': `Diese Datei liegt nicht direkt im Windows-Installer-Ordner; aus Sicherheitsgründen abgelehnt.`,
  'Completion.MoveCancelledSummary': `{0} von {1} {2} nach {3} verschoben, bevor du abgebrochen hast.`,
  'Completion.PermanentDeleteCancelledSummary': `{0} von {1} {2} endgültig gelöscht, bevor du abgebrochen hast.`,
  'Body.PendingReboot.Lead': `Diese Dateien können gerade nicht aufgeräumt werden.`,
  'Cli.TooManyArguments': `Fehler: Unerwartetes zusätzliches Argument '{0}'. Wenn dein Zielordner ein Leerzeichen enthält, setze den ganzen Pfad in Anführungszeichen: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Der Ordner gilt pro Benutzer; geplante Läufe und SYSTEM brauchen /m PFAD.`,
  'Error.ScanRecordsUnreadable': `InstallerClean konnte nicht genug von den Einträgen von Windows Installer lesen, um sicher zu sein, was noch gebraucht wird: Die Liste der installierten Programme kam unvollständig zurück, und dieselben Einträge direkt aus der Registrierung zu lesen führte ebenfalls zu Fehlern. Eine Datei könnte allein deshalb verwaist wirken, weil der Eintrag, der sie nennt, zu den unlesbaren gehörte, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer hat das Ende der Liste der installierten Programme nie signalisiert: InstallerClean hat {2} {3} gelesen und dann nach {0} Einträgen aufgegeben (letzter Fehlercode {1}). Einer Liste ohne Ende ist nicht zu trauen, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer hat das Ende der Patch-Liste eines Programms nie signalisiert: InstallerClean hat {2} {3} gelesen und dann nach {0} Einträgen aufgegeben (letzter Fehlercode {1}). Einer Liste ohne Ende ist nicht zu trauen, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt.`,
  'Completion.DonateAsk': `Über jedes Trinkgeld freue ich mich sehr`,
  'About.Link.Guide': `Anleitung und FAQ`,
  'About.Link.ReportProblem': `Ein Problem melden`,
  'About.AutoUpdateCheck': `Automatisch nach Updates suchen`,
  'Automation.About.Guide.HelpText': `Öffnet das readme auf github in deinem Browser.`,
  'Automation.About.ReportProblem.HelpText': `Öffnet den Issue-Tracker auf github.com in deinem Browser.`,
  'Automation.AutoUpdateCheck.HelpText': `Wenn aktiviert, sucht InstallerClean beim Start auf github nach einer neueren Version.`,
  'Tooltip.MoveSameDrive': `Verschiebt die nicht benötigten Dateien in den Sicherungsordner. Er liegt auf demselben Laufwerk, der Speicherplatz wird also erst frei, wenn du diesen Ordner löschst.`,
  'Confirm.DeletePermanently.Singular': `Diese Datei wird endgültig gelöscht. Das ist unbedenklich, aber wenn du eine Sicherung möchtest, nimm stattdessen Verschieben.`,
  'Confirm.DeletePermanently.Plural': `Diese Dateien werden endgültig gelöscht. Das ist unbedenklich, aber wenn du eine Sicherung möchtest, nimm stattdessen Verschieben.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean konnte Windows nicht dazu bringen, den echten Pfad von {InstallerFolder} aufzulösen, deshalb ließ sich für keine Datei zeigen, dass sie darin liegt, und keine wurde zum Aufräumen angeboten. Dieser Scan hat nichts gefunden, weil diese Prüfung fehlschlug, nicht weil der Ordner sauber ist. Es wurde nichts entfernt.`,
  'Automation.Scroll.ProductDetails': `Produktdetails`,
  'Body.PendingReboot.Other': `Windows Installer hat etwas in Arbeit, deshalb pausieren Verschieben und Löschen. InstallerClean fasst {InstallerFolder} nicht an, während sich der Ordner ändert. Danach einmal neu scannen, und sie sind wieder da.`,
  'Cli.TooManyArgumentsNoPath': `Fehler: unerwartetes zusätzliches Argument '{0}'. /s und /d nehmen keine weiteren Argumente, und pro Lauf ist nur ein Schalter möglich.`,
  'Cli.MissingFromDisk.Singular': `Windows hat einen Eintrag für {0} Datei, die nicht in {InstallerFolder} liegt: {1}. Im Alltag macht das keine Probleme, aber ein Update oder eine Deinstallation dieses Programms kann fehlschlagen. Um die Datei zurückzuholen, brauchst du das Installationsprogramm genau der Version, die du bereits hast. Hol es beim Hersteller des Programms und führe es über deiner vorhandenen Installation aus. Eine neuere Version reicht nicht: Sie müsste zuerst die vorhandene entfernen, und genau dieser Schritt braucht diese Datei. Vorher zu deinstallieren funktioniert aus demselben Grund ebenfalls nicht. Das sollte die Datei wiederherstellen und deine Einstellungen unangetastet lassen, garantiert wird es von Microsoft aber nicht.`,
  'Cli.MissingFromDisk.Plural': `Windows hat Einträge für {0} Dateien, die nicht in {InstallerFolder} liegen: {1}. Im Alltag macht das keine Probleme, aber ein Update oder eine Deinstallation dieser Programme kann fehlschlagen. Um eine Datei zurückzuholen, brauchst du das Installationsprogramm genau der Version, die du von diesem Programm bereits hast. Hol es beim Hersteller des Programms und führe es über deiner vorhandenen Installation aus. Eine neuere Version reicht nicht: Sie müsste zuerst die vorhandene entfernen, und genau dieser Schritt braucht die Datei. Vorher zu deinstallieren funktioniert aus demselben Grund ebenfalls nicht. Das sollte die Datei wiederherstellen und deine Einstellungen unangetastet lassen, garantiert wird es von Microsoft aber nicht.`,
  'Cli.MoveNotEnoughSpace': `Fehler: nicht genügend Speicherplatz unter {0}. Das Verschieben dieser Dateien braucht {1}, frei sind {2}. Es wurde nichts verschoben.`,
  'Cli.PendingRebootBlocked.Other': `Fehler: Windows Installer hat etwas in Arbeit, deshalb sind /m und /d blockiert. InstallerClean fasst {InstallerFolder} nicht an, während sich der Ordner ändert. Versuche es erneut, sobald es fertig ist.`,
  'Cli.ScanSignal.Busy': `Als der Scan begann, nutzte etwas Windows Installer, etwa ein Windows-Update oder ein Programm, das installiert wurde. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Führe den Befehl erneut aus, sobald das fertig ist.`,
  'Cli.ScanSignal.Suspended': `Als der Scan begann, war auf diesem Rechner eine Windows-Installer-Transaktion angehalten. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Setze diese Installation fort oder mache sie rückgängig oder starte Windows neu, und führe den Befehl dann erneut aus.`,
  'Cli.ScanSignal.RenameInCache': `Als der Scan begann, hatte Windows für den nächsten Neustart eine Dateiumbenennung eingeplant, die {InstallerFolder} betrifft. Deshalb hat InstallerClean die Dateien dort zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und führe den Befehl dann erneut aus.`,
  'Cli.ScanSignal.RenameUnresolved': `Als der Scan begann, war für den nächsten Neustart ein Dateivorgang vorgemerkt, und InstallerClean konnte nicht erkennen, welche Dateien darin genannt werden. Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und führe den Befehl dann erneut aus.`,
  'Cli.ScanSignal.AccessRefused': `Als der Scan begann, hat Windows InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt war. Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und führe den Befehl erneut aus.`,
  'Cli.ScanSignal.RegistryUnreadable': `Als der Scan begann, konnte InstallerClean eine der Windows-Einstellungen nicht lesen, die es prüft, und deshalb nicht erkennen, ob ein Installationsvorgang lief oder auf einen Neustart wartete. Daher hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und führe den Befehl erneut aus.`,
  'Cli.ScanSignal.LockUnreadable': `Als der Scan begann, konnte InstallerClean nicht prüfen, ob Windows Installer beschäftigt war. Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und führe den Befehl erneut aus.`,
  'Cli.ScanSignal.LockUnreadableWindowsError': `Als der Scan begann, konnte InstallerClean nicht prüfen, ob Windows Installer beschäftigt war, weil Windows einen Fehler gemeldet hat: {0} (Fehler {1}). Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und führe den Befehl erneut aus.`,
  'Cli.ScanSignal.Other': `Als der Scan begann, hatte Windows Installer etwas in Arbeit oder ausstehend. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Führe den Befehl erneut aus, sobald das erledigt ist.`,
  'Cli.ScanSignal.ClockSetBack': `Die Uhr dieses PCs ist während des Scans zurückgesprungen oder nachgegangen. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Führe den Befehl erneut aus.`,
  'Cli.FoundNoOrphans': `Keine nicht benötigten Dateien gefunden.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean konnte den Sicherungsordner nicht mehr bestätigen und hat deshalb angehalten. Prüfe {0} und führe den Befehl dann erneut aus.`,
  'Cli.Help.Summary': `Entfernt .msi-/.msp-Dateien, die kein installiertes Programm mehr braucht.`,
  'Cli.Help.Elevation': `Nur mit Administratorrechten; ohne sie scannt es nicht.`,
  'Error.InstallerLockUnavailableTitle': `Nichts gelöscht`,
  'Error.MoveInstallerLockUnavailableTitle': `Nichts verschoben`,
  'Error.InstallerLockUnavailable': `InstallerClean konnte die Sperre nicht übernehmen, mit der Windows Installer verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts gelöscht. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean konnte die Sperre nicht übernehmen, mit der Windows Installer verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts verschoben. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt.`,
  'Cli.InstallerLockUnavailable': `Fehler: InstallerClean konnte die Windows-Installer-Sperre nicht übernehmen, die verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts gelöscht. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt.`,
  'Cli.MoveInstallerLockUnavailable': `Fehler: InstallerClean konnte die Windows-Installer-Sperre nicht übernehmen, die verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts verschoben. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt.`,
  'Completion.ReverifyIdentityClaimed': `{0} {1} an Ort und Stelle belassen, weil Windows einen Eintrag zu dem darin genannten Programm hat.`,
  'Completion.ReverifyIdentityUnreadable': `{0} {1} an Ort und Stelle belassen, weil InstallerClean darin kein Programm benannt fand.`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean konnte die Windows-Installer-Einträge nicht mit dem Inhalt von {InstallerFolder} abgleichen. Der Ordner enthält Dateien, aber kein einziger Eintrag zeigt auf irgendetwas darin, deshalb ließ sich für keine Datei zeigen, dass sie nicht benötigt wird. Es wurde nichts angeboten und nichts entfernt.`,
  'Error.ScanInstallerFolderElsewhere': `InstallerClean kann {InstallerFolder} auf diesem PC nicht scannen. Windows meldet diesen Ordner als {0}, und InstallerClean scannt nur einen Installer-Ordner, den Windows unter seinem eigenen Pfad meldet. Es wurde nichts angeboten und nichts entfernt.`,
  'Completion.NothingOffered': `Auf diesem PC nichts angeboten`,
  'Completion.HeldBack.Singular': `{0} Datei zurückgehalten. Der Scan hielt sie für nicht benötigt. Die abschließende Prüfung konnte das nicht bestätigen.`,
  'Completion.HeldBack.Plural': `{0} Dateien zurückgehalten. Der Scan hielt sie für nicht benötigt. Die abschließende Prüfung konnte das nicht bestätigen.`,
  'Body.PendingReboot.PendingRenameUnresolved': `Für den nächsten Neustart ist ein Dateivorgang vorgemerkt, und InstallerClean kann nicht erkennen, welche Dateien darin genannt werden. Damit lässt sich nicht ausschließen, dass sie in {InstallerFolder} liegen. Starte Windows neu, bevor du aufräumst.`,
  'Completion.MoveRestoreHint': `Lösche diesen Ordner, sobald du dich überzeugt hast, dass alles in Ordnung ist.`,
  'Completion.MoveRestoreHintSameDrive': `Lösche diesen Ordner, sobald du dich überzeugt hast, dass alles in Ordnung ist. Erst dann wird der Speicherplatz tatsächlich frei.`,
  'Confirm.MoveDestination.Singular': `Diese Datei wird verschoben nach:`,
  'Confirm.MoveDestination.Plural': `Diese Dateien werden verschoben nach:`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `Fehler: Für den nächsten Neustart ist ein Dateivorgang vorgemerkt, und InstallerClean kann nicht erkennen, welche Dateien darin genannt werden. Damit lässt sich {InstallerFolder} nicht ausschließen. Starte Windows neu, bevor du aufräumst.`,
  'Cli.MoveRestoreHint': `Prüfe, ob deine Programme sich weiterhin wie gewohnt aktualisieren und deinstallieren lassen, und lösche dann {0}.`,
  'Error.ScanStoppedDetails': `Das wird auch in {0} festgehalten.`,
  'Body.PendingReboot.RegistryCheckUnreadable': `InstallerClean konnte eine der Windows-Einstellungen nicht lesen, die es prüft, bevor es {InstallerFolder} anfasst, und kann deshalb nicht erkennen, ob gerade ein Installationsvorgang läuft oder auf einen Neustart wartet. Starte Windows neu und wähle Neu scannen. Lässt sich die Einstellung dann immer noch nicht lesen, ist das kein Rechner, den InstallerClean aufräumen kann.`,
  'Cli.InstallerLockAccessRefused': `Fehler: Windows hat InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt ist, und damit ließ sich nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts gelöscht.`,
  'Cli.MoveCancelledRestoreHint': `Das lässt sich leicht rückgängig machen. Verschiebe sie aus {0} zurück nach {InstallerFolder}, und alles ist wieder wie vorher.`,
  'Cli.MoveInstallerLockAccessRefused': `Fehler: Windows hat InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt ist, und damit ließ sich nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts verschoben.`,
  'Cli.PendingRebootBlocked.RegistryCheckUnreadable': `Fehler: InstallerClean konnte einen der Registrierungswerte nicht lesen, die es prüft, bevor es {InstallerFolder} anfasst, und kann deshalb einen laufenden oder für den nächsten Neustart vorgemerkten Windows-Installer-Vorgang nicht ausschließen. /m und /d sind blockiert. Starte Windows neu und versuch es erneut. Schlägt das Lesen weiterhin fehl, ist das kein Rechner, den InstallerClean aufräumen kann.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutexUnreadable': `Fehler: InstallerClean konnte nicht prüfen, ob Windows Installer beschäftigt ist, und kann deshalb eine laufende Installation nicht ausschließen. /m und /d sind blockiert. Starte Windows neu und versuch es erneut. Schlägt die Prüfung weiterhin fehl, ist das kein Rechner, den InstallerClean aufräumen kann.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutexUnreadableWindowsError': `Fehler: InstallerClean konnte nicht prüfen, ob Windows Installer beschäftigt ist, weil Windows einen Fehler gemeldet hat: {0} (Fehler {1}). Deshalb kann es eine laufende Installation nicht ausschließen. /m und /d sind blockiert. Starte Windows neu und versuch es erneut. Schlägt die Prüfung weiterhin fehl, ist das kein Rechner, den InstallerClean aufräumen kann.`,
  'Completion.MoveCancelledRestoreHint': `Das lässt sich leicht rückgängig machen. Verschiebe sie zurück nach {InstallerFolder}, und alles ist wieder wie vorher.`,
  'Error.InstallerLockAccessRefused': `Windows hat InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt ist, und damit ließ sich nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts gelöscht.`,
  'Error.MoveInstallerLockAccessRefused': `Windows hat InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt ist, und damit ließ sich nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts verschoben.`,
  'Error.MoveStoppedTitle': `Verschieben gestoppt`,
  'Field.NoNamedProduct': `(kein Programm)`,
  'Summary.RegisteredWindow.Missing.Plural': `{0} fehlen`,
  'Summary.RegisteredWindow.Missing.Singular': `{0} fehlt`,
  'UpdateCheck.Failed.Unknown.NoLog': `Die Prüfung ist aus unbekanntem Grund fehlgeschlagen. Das Absturzprotokoll konnte nicht geschrieben werden.`,
  'Startup.AdminRightsNeededTitle': `Administratorrechte erforderlich`,
  'Startup.AdminRightsNeededBody': `InstallerClean braucht Administratorrechte, um alle auf diesem PC installierten Programme zu sehen, läuft aber nicht als Administrator und scannt deshalb nicht. Es wurde nichts verändert.\n\nFühre InstallerClean erneut als Administrator aus.`,
  'Cli.AdminRightsNeeded': `InstallerClean braucht Administratorrechte, um alle auf diesem PC installierten Programme zu sehen, läuft aber nicht als Administrator und scannt deshalb nicht. Es wurde nichts verändert. Führe es erneut in einer Eingabeaufforderung als Administrator aus.`,
  'Error.ScanInstallerFolderNotFound': `Windows meldet, dass {InstallerFolder} auf diesem PC nicht existiert, deshalb hat InstallerClean abgebrochen. Es wurde nichts angeboten und nichts entfernt.`,
  'Error.ScanInstallerFolderListFailed': `Windows hat einen Fehler gemeldet, als InstallerClean versuchte, die Dateien in {InstallerFolder} aufzulisten, deshalb konnte es keine vollständige Liste erhalten und hat abgebrochen. Es wurde nichts angeboten und nichts entfernt.`,
  'Body.RescanNotFinished.Lead': `Nichts anzuzeigen bis zum nächsten Scan.`,
  'Body.RescanNotFinished.Why': `Der Scan, der nach „Verschieben“ oder „Löschen“ läuft, wurde nicht abgeschlossen, deshalb kann keine Liste angezeigt werden. Klicke auf „Neu scannen“, um {InstallerFolder} erneut zu scannen.`,
  'Body.RescanNotFinished.Recorded': `Was ihn gestoppt hat, ist in {0} festgehalten.`,
  'Status.MatchingCount': `{0} von {1}`,
  'Status.WaitingForDrive': `Warten auf eine Antwort von Laufwerk {0}...`,
  'Status.WaitingForPath': `Warten auf eine Antwort von {0}...`,
  'Action.StopWaiting': `Ohne _Warten fortfahren`,
  'Automation.StopWaitingForDrive': `Ohne Warten fortfahren, Laufwerk {0}`,
  'Automation.StopWaitingForPath': `Ohne Warten fortfahren, {0}`,
  'Status.StopWaitingLeavesAlone': `Jede Datei, die noch damit abgeglichen werden muss, bleibt unangetastet.`,
  'Automation.WaitingLineWithStopKey': `{0} Drücke Alt+{1}, um ohne Warten fortzufahren. {2}`,
  'Summary.SourceGivenUp.Drive': `InstallerClean hat ohne Laufwerk {0} weitergemacht und jede Datei unangetastet gelassen, die noch damit abgeglichen werden musste. Sobald es wieder normal reagiert, klicke auf „Neu scannen“.`,
  'Summary.SourceGivenUp.Path': `InstallerClean hat ohne {0} weitergemacht und jede Datei unangetastet gelassen, die noch damit abgeglichen werden musste. Sobald der Zugriff darauf wieder normal funktioniert, klicke auf „Neu scannen“.`,
  'Summary.SourcesGivenUp': `InstallerClean hat ohne mehrere Laufwerke oder Freigaben ({0}) weitergemacht und jede Datei unangetastet gelassen, die noch mit einem davon abgeglichen werden musste. Sobald sie wieder normal reagieren, klicke auf „Neu scannen“.`,
  'Summary.ScanSignal.Busy': `Als der Scan begann, nutzte etwas Windows Installer, etwa ein Windows-Update oder ein Programm, das installiert wurde. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Klicke auf „Neu scannen“, sobald das fertig ist.`,
  'Summary.ScanSignal.Suspended': `Als der Scan begann, war auf diesem Rechner eine Windows-Installer-Transaktion angehalten. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Setze diese Installation fort oder mache sie rückgängig oder starte Windows neu, und klicke dann auf „Neu scannen“.`,
  'Summary.ScanSignal.RenameInCache': `Als der Scan begann, hatte Windows für den nächsten Neustart eine Dateiumbenennung eingeplant, die {InstallerFolder} betrifft. Deshalb hat InstallerClean die Dateien dort zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und klicke dann auf „Neu scannen“.`,
  'Summary.ScanSignal.RenameUnresolved': `Als der Scan begann, war für den nächsten Neustart ein Dateivorgang vorgemerkt, und InstallerClean konnte nicht erkennen, welche Dateien darin genannt werden. Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und klicke dann auf „Neu scannen“.`,
  'Summary.ScanSignal.AccessRefused': `Als der Scan begann, hat Windows InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt war. Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und klicke auf „Neu scannen“.`,
  'Summary.ScanSignal.RegistryUnreadable': `Als der Scan begann, konnte InstallerClean eine der Windows-Einstellungen nicht lesen, die es prüft, und deshalb nicht erkennen, ob ein Installationsvorgang lief oder auf einen Neustart wartete. Daher hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und klicke auf „Neu scannen“.`,
  'Summary.ScanSignal.LockUnreadable': `Als der Scan begann, konnte InstallerClean nicht prüfen, ob Windows Installer beschäftigt war. Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und klicke auf „Neu scannen“.`,
  'Summary.ScanSignal.LockUnreadableWindowsError': `Als der Scan begann, konnte InstallerClean nicht prüfen, ob Windows Installer beschäftigt war, weil Windows einen Fehler gemeldet hat: {0} (Fehler {1}). Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und klicke auf „Neu scannen“.`,
  'Summary.ScanSignal.Other': `Als der Scan begann, hatte Windows Installer etwas in Arbeit oder ausstehend. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Klicke auf „Neu scannen“, sobald das erledigt ist.`,
  'Summary.ScanSignal.ClockSetBack': `Die Uhr dieses PCs ist während des Scans zurückgesprungen oder nachgegangen. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Klicke auf „Neu scannen“.`,
  'Display.DriveName': `Laufwerk {0}`,
  'Cli.SourceGivenUp.Drive': `InstallerClean hat aufgehört, auf Laufwerk {0} zu warten, und jede Datei unangetastet gelassen, die noch damit abgeglichen werden musste. Sobald es wieder normal reagiert, führe den Befehl erneut aus.`,
  'Cli.SourceGivenUp.Path': `InstallerClean hat aufgehört, auf {0} zu warten, und jede Datei unangetastet gelassen, die noch damit abgeglichen werden musste. Sobald der Zugriff darauf wieder normal funktioniert, führe den Befehl erneut aus.`,
  'Cli.SourcesGivenUp': `InstallerClean hat aufgehört, auf mehrere Laufwerke oder Freigaben ({0}) zu warten, und jede Datei unangetastet gelassen, die noch mit einem davon abgeglichen werden musste. Sobald sie wieder normal reagieren, führe den Befehl erneut aus.`,
  'Action.CloseWithoutDonating': `_Ohne Trinkgeld schließen`,
  'Tooltip.Donate': `Eigentlich sollte ich für InstallerClean Geld verlangen, denn es hat enorm viel Arbeit gekostet, aber ich möchte es kostenlos halten, damit es so vielen Menschen wie möglich hilft. Deine Spende macht die Mühe lohnenswert und wird sehr geschätzt.`,
  'Cli.DonateAsk': `InstallerClean ist kostenlos. Wenn es dir geholfen hat, freue ich mich sehr über ein Trinkgeld: {0}`,
  'Completion.SendReport': `_Anonymen Bericht senden`,
  'Completion.ReportPanel.Intro': `Der Bericht fügt dein Ergebnis zu [diesem Diagramm] auf GitHub hinzu, das zeigt, wie viel Speicherplatz Menschen mit InstallerClean freigeben. Außerdem enthält er diese technischen Angaben, die mir helfen, Fehler zu finden:`,
  'Completion.ReportPanel.Freed': `Wie viel Speicherplatz freigegeben und wie viele Dateien verschoben oder gelöscht wurden`,
  'Completion.ReportPanel.Destination': `Ob beim Verschieben das Ziel auf demselben Laufwerk, einem anderen Laufwerk, einem Wechsellaufwerk oder einer Netzwerkfreigabe lag`,
  'Completion.ReportPanel.Durations': `Wie lange der Scan und das Verschieben oder Löschen gedauert haben`,
  'Completion.ReportPanel.InstallerFiles': `Wie viele Installer-Dateien es von jeder Art gibt und wie viel Speicherplatz sie belegen`,
  'Completion.ReportPanel.LeftAlone': `Wie viele Dateien unangetastet blieben und warum`,
  'Completion.ReportPanel.Waits': `Wie oft InstallerClean auf ein langsames Laufwerk oder eine Netzwerkfreigabe gewartet hat und bei wie vielen es aufgehört hat zu warten`,
  'Completion.ReportPanel.Records': `Zu wie vielen Programmen und Patches Windows einen Eintrag hat`,
  'Completion.ReportPanel.ShortNames': `Ob Windows noch kurze Dateinamen im alten Stil anlegt`,
  'Completion.ReportPanel.Windows': `Ob Windows 10 oder 11 und x64 oder Arm ist und auf welche Sprache und welches Land oder welche Region es eingestellt ist`,
  'Completion.ReportPanel.AppVersion': `Die Version von InstallerClean, wie du es bekommen hast (Setup, Portable, Slim, Scoop, das Befehlszeilenprogramm allein, selbst aus dem Quellcode gebaut oder unbekannt), ob der Bericht von InstallerClean oder vom Befehlszeilenprogramm gesendet wurde, und die Sprache, in der es angezeigt wurde`,
  'Completion.ReportPanel.Errors': `Wie oft etwas schiefgegangen ist`,
  'Completion.ReportPanel.Closing': `Jede dieser Angaben ist eine Zahl oder eine Bezeichnung. Nichts im Bericht nennt dich, deinen PC, deine Dateien oder deine Programme, und ich sehe deine IP-Adresse nie.`,
  'Automation.ReportInfo': `Was der Bericht enthält`,
  'Automation.Scroll.ReportContents': `Was der Bericht enthält`,
  'Completion.ReportPanel.SeeReport': `Genau sehen, was gesendet wird`,
  'Window.Report.Title': `Der anonyme Bericht`,
  'Window.Report.Unreadable': `InstallerClean konnte den Bericht nicht lesen.`,
  'Automation.Scroll.Waiting': `Warten auf ein Laufwerk oder eine Freigabe`,
  'Completion.RecentChange.Singular': `Eine Datei ({2}) wurde zurückgehalten, weil Windows anzeigt, dass sie geändert wurde, nachdem InstallerClean mit dem Scannen begonnen hatte. Falls gerade ein Programm installiert oder aktualisiert wurde, scanne erneut, sobald das fertig ist, dann prüft InstallerClean sie noch einmal.`,
  'Completion.RecentChange.Plural': `{0} {1} ({2}) wurden zurückgehalten, weil Windows anzeigt, dass sie geändert wurden, nachdem InstallerClean mit dem Scannen begonnen hatte. Falls gerade ein Programm installiert oder aktualisiert wurde, scanne erneut, sobald das fertig ist, dann prüft InstallerClean sie noch einmal.`,
  'UpdateNotice.Available': `InstallerClean {0} ist verfügbar.`,
  'UpdateNotice.Download': `Von GitHub herunterladen`,
  'Automation.UpdateNotice.Download': `Von github herunterladen`,
  'UpdateNotice.Recommended': `Es wird dringend empfohlen, die neueste Version zu verwenden.`,
  'UpdateNotice.UpdateNow': `Jetzt aktualisieren`,
  'UpdateNotice.Downloading': `InstallerClean {0} wird heruntergeladen...`,
  'UpdateNotice.DownloadFailed': `Das Update konnte nicht heruntergeladen werden.`,
  'UpdateNotice.InstallFailed': `Das Update konnte nicht installiert werden.`,
  'UpdateNotice.ContinueWithoutUpdating': `_Ohne Aktualisierung fortfahren`,
  'Cli.UpdatePrompt': `Jetzt aktualisieren? Es wird dringend empfohlen, die neueste Version zu verwenden. [{0}/{1}]`,
  'Cli.UpdateAnswerYes': `J`,
  'Cli.UpdateAnswerNo': `N`,
  'Cli.UpdateListsOnlyAfterDelete': `Die neue Version scannt und listet auf, was sie findet, ohne etwas zu löschen. Zum Löschen führe danach /d erneut aus.`,
  'Cli.UpdateListsOnlyAfterMove': `Die neue Version scannt und listet auf, was sie findet, ohne etwas zu verschieben. Zum Verschieben führe danach /m erneut aus.`,
  'Cli.UpdateRunningInstead': `Stattdessen wird {0} mit InstallerClean {1} ausgeführt.`,
  'Cli.UpdateDownloadLink': `{0}: {1}`,
  'Cli.Help.UpdateNote': `Vor dem Scannen prüft es, ob es eine neuere Version von InstallerClean&#10;gibt. Wenn du es selbst startest, fragt es vor dem Aktualisieren und&#10;aktualisiert, wenn innerhalb einer Minute niemand antwortet. Läuft es von&#10;selbst, in einer geplanten Aufgabe oder einem Skript, aktualisiert es ohne&#10;zu fragen. Mit /noupdate wird die Prüfung übersprungen.`,
  'UpdateNotice.Scoop': `Diese Kopie wurde mit Scoop installiert. Um sie zu aktualisieren, schließe InstallerClean und führe Folgendes aus:`,
  'UpdateNotice.ScoopGlobal': `Diese Kopie wurde mit Scoop für alle Benutzer installiert. Um sie zu aktualisieren, schließe InstallerClean, öffne ein Terminal als Administrator und führe Folgendes aus:`,
  'UpdateNotice.CommandCopied': `Kopiert`,
  'UpdateNotice.CopyFailed': `Kopieren nicht möglich. Markiere den Befehl und kopiere ihn selbst.`,
  'Automation.UpdateNotice.CopyCommand': `Befehl kopieren`,
  'Automation.UpdateNotice.ScoopCommand': `Aktualisierungsbefehl für Scoop`,
  'Cli.UpdateInUse': `InstallerClean wird gerade verwendet und kann sich deshalb jetzt nicht aktualisieren.`,
  'Error.FolderWillNotGiveFilesBack': `Du kannst Dateien in {0} speichern, aber nicht wieder öffnen, deshalb verschiebt InstallerClean keine Dateien dorthin.`,
  'UpdateNotice.NeedsNetFramework48': `Diese Version benötigt .NET Framework 4.8. Installiere es von Microsoft, starte den PC neu und öffne dann InstallerClean, um zu aktualisieren.`,
  'UpdateNotice.CannotInstallAutomatically': `InstallerClean {0} kann nicht automatisch installiert werden.`,
  'UpdateNotice.CopyCannotTell': `InstallerClean kann nicht feststellen, ob es mit seinem Setup installiert wurde, und kann sich deshalb nicht selbst aktualisieren.`,
  'UpdateNotice.DownloadNetFramework48': `.NET Framework 4.8 von Microsoft herunterladen`,
  'Automation.UpdateNotice.DownloadNetFramework48': `.NET Framework 4.8 von Microsoft herunterladen`,
  'Cli.UpdateNeedsNetFramework48': `Diese Version benötigt .NET Framework 4.8. Installiere es von Microsoft, starte den PC neu und führe InstallerClean dann erneut aus, um zu aktualisieren.`,
  'Cli.ReportPrompt': `Anonymen Bericht über diesen Durchlauf senden? Er ist gespeichert unter {0}. Gib ? ein, um zu sehen, was er enthält. [{1}/{2}/?]`,
  'Cli.ReportChartAt': `dem Diagramm unter {0}`,
  'Cli.ReportIntro': `Der Bericht fügt dein Ergebnis zu {0} hinzu, das zeigt, wie viel Speicherplatz Menschen mit InstallerClean freigeben. Außerdem enthält er diese technischen Angaben, die mir helfen, Fehler zu finden:`,
  'Cli.ReportInFull': `Der vollständige Bericht steht unten.`,
  'Cli.ReportSent': `Bericht gesendet. Danke.`,
  'Cli.ReportNotSent': `Bericht nicht gesendet.`,
  'Cli.ReportCouldNotBeSent': `Der Bericht konnte nicht gesendet werden. Er wird in {0} aufbewahrt.`,
  'Cli.ReportWaiting': `Ein Bericht, der seit einem früheren Durchlauf auf das Senden wartet, wird gesendet.`,
  'Cli.UpdateRunningNewer': `{0} wird mit InstallerClean {1} ausgeführt. Sobald der Lauf beendet ist, ersetzt diese Version diese Kopie.`,
  'Cli.UpdateNotRun': `Die neuere Version konnte nicht ausgeführt werden.`,
  'UpdateNotice.CheckingDownload': `Download wird geprüft...`,
  'Cli.MutexLostDelete': `Angehalten nach dem Löschen von {0} von {1} {2}: InstallerClean konnte nicht mehr sicher sein, dass keine andere InstallerClean-Instanz tätig war. Führe denselben Befehl erneut aus. Exit-Code 2 (teilweise).`,
  'Cli.MutexLostMove': `Angehalten nach dem Verschieben von {0} von {1} {2} nach {3}: InstallerClean konnte nicht mehr sicher sein, dass keine andere InstallerClean-Instanz tätig war. Führe denselben Befehl erneut aus. Exit-Code 2 (teilweise).`,
  'Cli.CoverLostNoWorkDelete': `Angehalten, bevor eine Datei gelöscht wurde: InstallerClean konnte nicht mehr sicher sein, dass keine andere InstallerClean-Instanz tätig war. Führe denselben Befehl erneut aus. Exit-Code 75 (vorübergehend).`,
  'Cli.CoverLostNoWorkMove': `Angehalten, bevor eine Datei verschoben wurde: InstallerClean konnte nicht mehr sicher sein, dass keine andere InstallerClean-Instanz tätig war. Führe denselben Befehl erneut aus. Exit-Code 75 (vorübergehend).`,
  'Cli.UpdateNoAnswer': `Keine Antwort nach einer Minute, also aktualisiert sich InstallerClean ohne zu fragen, wie wenn niemand da ist, der antworten kann.`,
  'Cli.ReportNoAnswer': `Keine Antwort nach einer Minute.`,
};

// PARSE CONTROL. About the READING and not about the content, and it exits 2,
// which is a code no ordinary run of this generator can produce: a generator is
// red by intent for the whole gap between a string landing in English and its
// translation round, so its verdict lines and its exit 0 are load-bearing in
// ci.yml and are deliberately untouched here. This says something different from
// "the translation is not done". It says the file could not be read.
//
// BOTH LEGS. raw === 0 catches a file that declares no entry at all, which the
// equality cannot see on its own because 0 === 0 holds. parsed !== raw catches
// entries the reader dropped, which one <comment> moved above its <value> does to
// any regex wanting <value> on the same whitespace run, and the Visual Studio resx
// editor writes that shape. Counted with <data\b so a tab after the tag name is
// not read as an empty file, and neither figure is written down, so a string added
// to the resx cannot make this go stale.
//
// WHY IT IS HERE WHEN THE SELF-CHECK BELOW ALREADY REDDENS. The self-check reaches
// the right verdict through what it happens to compare, not through knowing it
// read anything: with the neutral's attribute order changed, this generator wrote a
// 389-entry file and its own self-check parsed THREE entries out of it, said
// GENERATION HAS ISSUES, and was right for a reason with nothing to do with the
// truth. A tool reasoning over three entries of a 389-entry artefact should say so.
const parseControl = (where, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${where}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this generator cannot show it read.');
  process.exit(2);
};

let text = readFileSync(BASE, 'utf8');
// The transform below reaches every entry through '<data name="', one space and no
// \s+, which is NOT the spelling the self-check's parse() uses further down. A
// control that exercises a pattern the reader does not use proves the file has
// structure and proves nothing about whether this reader can reach it, so the
// source is controlled in its own spelling before a single value is replaced.
parseControl(BASE, text,
  [...text.matchAll(/<data name="([^"]+)"[^>]*>\s*<value>/g)].length);

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
const parse = (xml, where) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  parseControl(where, xml, map.size);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'), BASE);
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written, OUT);
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
