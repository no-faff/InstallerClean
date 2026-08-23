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
  // The list separator German uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
];

// Satellite-only .One override(s). NOT in the neutral; appended before </root>.
// check-resx-parity.mjs allows each because its base (the flat key itself) is in
// the neutral. The value's {N} set matches the base key's set.
const OVERRIDES = {
  'Status.RegisteredPackagesFound.One': `{0} registriertes {1} gefunden.`,
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
  // Section.Registered.Products and Automation.Section.Products were removed from
  // this map on 2026-08-21. They left the neutral resx at f49b795b, when the
  // registered-files window stopped having a products group of its own, and stayed
  // here and in all fifteen satellites, so every round regenerated two keys the app
  // cannot use and check-resx-parity reported them as strays in every language.
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
  'Action.BuyMeACuppa': `Spendier mir einen _Kaffee`,
  'Action.LeaveStarOnGitHub': `Einen Stern auf _GitHub hinterlassen`,
  'Action.Licence': `Apache-2.0-Lizenz`,
  'Action.Move': `_Verschieben`,
  'Action.BackupFolderPlaceholder': `Pfad zum Ordner, falls du verschiebst statt zu löschen.`,
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
  'Automation.ConfirmDelete': `Endgültig löschen entfernt die nicht benötigten Dateien. Abbrechen schließt das Fenster, ohne zu löschen.`,
  'Automation.ConfirmMove': `Verschieben legt die nicht benötigten Dateien in den gewählten Zielordner. Abbrechen lässt sie, wo sie sind.`,
  'Automation.SayThanks': `Danke sagen`,
  'Automation.ConfirmSendResultLog': `Senden übermittelt den angezeigten Bericht an No Faff. Abbrechen sendet nichts.`,
  'Automation.CheckForUpdates': `Nach Updates suchen`,
  'Automation.CheckForUpdates.HelpText': `Sucht auf der Release-Seite von github nach einer neueren Version.`,
  'Automation.UpdateAvailable.HelpText': `Öffne die Release-Seite, um die neuere Version herunterzuladen, oder brich ab, um die aktuelle Version zu behalten.`,
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
  'Tooltip.BuyMeACuppa.About': `Das macht durstig!`,
  'Tooltip.CancellingPending': `Abbruch angefordert. InstallerClean wartet, bis der aktuelle Schritt einen Haltepunkt erreicht. Bei starker Datenträgeraktivität oder einem MSI-Datenbankaufruf kann das ein paar Sekunden dauern.`,
  'Tooltip.Close': `Schließen`,
  'Tooltip.LeaveStarOnGitHub.About': `Ein Stern hilft anderen, InstallerClean zu finden.`,
  'Tooltip.Minimise': `Minimieren`,
  'Tooltip.SendResultLog': `Ganz wie du magst, aber ich freue mich darüber. Sendet eine anonyme Zusammenfassung, die mir nur zeigt, ob es funktioniert und wie viel Platz die Leute freigeben. Auf dem nächsten Bildschirm siehst du vor dem Bestätigen, was gesendet wird.`,
  'Tooltip.SendResultLog.NothingFound': `Ganz wie du magst, aber ich freue mich darüber. Sendet eine anonyme Zusammenfassung, die mir nur zeigt, ob es funktioniert. Auf dem nächsten Bildschirm siehst du vor dem Bestätigen, was gesendet wird.`,
  'Tooltip.Move': `Verschiebt die nicht benötigten Dateien in den Sicherungsordner. Lösche diesen Ordner, sobald du sicher bist, dass sie niemand braucht.`,
  'Tooltip.MoveNeedsDestination': `Verschiebt die nicht benötigten Dateien in einen Sicherungsordner. Den wählst du gleich aus. Lösche diesen Ordner, sobald du sicher bist, dass sie niemand braucht.`,
  'Tooltip.Delete': `Löscht die nicht benötigten Dateien endgültig. Sie lassen sich bedenkenlos entfernen, und du bekommst den Platz sofort zurück.`,
  'Tooltip.SigningCertificate': `Antragstellername aus dem eingebetteten Authenticode-Zertifikat. Die Zertifikatskette wurde nicht geprüft.`,

  // Body copy
  'Body.MainExplanation.Lead': `Alle nicht benötigten Dateien unten kannst du [bedenkenlos löschen].`,
  'Body.MainExplanation.Why': `Sie liegen in {InstallerFolder}. InstallerClean fragt Windows nach jedem installierten Programm: Eine Datei wird aufgeführt, wenn kein Programm sie beansprucht ({0}) oder wenn ein neuerer Patch sie ersetzt hat und kein Programm auf sie zurückgehen könnte ({1}).`,
  'Body.MainExplanation.Action': `Verschiebe sie in einen Sicherungsordner deiner Wahl und lösche diesen Ordner, sobald du sicher bist, dass deine Programme sich weiterhin wie gewohnt aktualisieren, reparieren und deinstallieren lassen. Wenn du sie zurück nach {InstallerFolder} legst, ist alles wiederhergestellt. Oder lösche sie jetzt endgültig.`,
  'Body.PendingReboot.MsiExecuteMutex': `Etwas nutzt gerade Windows Installer, etwa ein Windows-Update oder ein Programm, das im Hintergrund installiert wird. Verschieben und Löschen pausieren, solange das läuft, damit InstallerClean {InstallerFolder} nicht anfasst, während sich der Ordner ändert. Danach einmal neu scannen, und sie sind wieder da.`,
  'Body.PendingReboot.InstallerInProgress': `Auf diesem Rechner ist eine frühere Windows-Installer-Transaktion angehalten. Setze diese Installation fort oder mache sie rückgängig (oder starte Windows neu), bevor du {InstallerFolder} aufräumst.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows hat für den nächsten Neustart eine Dateiumbenennung eingeplant, die {InstallerFolder} betrifft. Starte Windows neu, bevor du aufräumst.`,
  'Body.NoFileSelected': `Wähle eine Datei, um Details anzuzeigen.`,
  'Body.NoProductSelected': `Wähle ein Produkt, um Details anzuzeigen.`,
  'Body.NoMetadata': `Keine Metadaten verfügbar.`,
  'Body.RegisteredMissingFromDisk': `Diese Installationsdatei fehlt. Das macht jetzt keine Probleme und wird es auch nicht tun, bis du eines Tages das zugehörige Programm reparieren, aktualisieren oder deinstallieren willst. Dieser Schritt kann dann fehlschlagen, weil Windows nach dieser Datei sucht und sie nicht da ist.&#10;&#10;Um das zu beheben, lade das Installationsprogramm beim Hersteller herunter und führe es über deiner vorhandenen Installation aus (deinstalliere nicht zuerst, denn das Deinstallieren ist selbst ein Schritt, der diese Datei braucht). Nimm nach Möglichkeit genau die Version, die du installiert hast, da Windows eine andere ablehnen kann. Das sollte die Datei wiederherstellen und deine Einstellungen unangetastet lassen, garantiert wird es von Microsoft aber nicht, und dessen eigenes letztes Mittel ist, das Programm neu zu installieren.`,
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
  'Status.Moving': `Nicht benötigte Dateien werden verschoben...`,
  'Status.Deleting': `Nicht benötigte Dateien werden gelöscht...`,
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
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} endgültig gelöscht`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} endgültig gelöscht`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} Datei unangetastet`,
  'Summary.RegisteredStillUsed.Plural': `{0} Dateien unangetastet`,
  'Summary.OrphanedToCleanUp.Singular': `{0} nicht benötigte Datei zum Aufräumen`,
  'Summary.OrphanedToCleanUp.Plural': `{0} nicht benötigte Dateien zum Aufräumen`,
  'Summary.NothingListed.Singular': `Auf diesem PC konnte InstallerClean nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb die eine Datei zurückgehalten, statt sie aufzuführen.`,
  'Summary.NothingListed.Plural': `Auf diesem PC konnte InstallerClean nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb {0} {1} zurückgehalten, statt sie aufzuführen.`,
  'Summary.MissingFromDisk.Singular': `Windows hat einen Eintrag für {0} Datei, die nicht in {InstallerFolder} liegt: {1}. Im Alltag macht das keine Probleme, aber eine Reparatur, ein Update oder eine Deinstallation kann daran scheitern. Öffne Details, um zu erfahren, was zu tun ist.`,
  'Summary.MissingFromDisk.Plural': `Windows hat Einträge für {0} Dateien, die nicht in {InstallerFolder} liegen: {1}. Im Alltag machen sie keine Probleme, aber eine Reparatur, ein Update oder eine Deinstallation kann daran scheitern. Öffne Details, um zu erfahren, was zu tun ist.`,
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

  // 0 = destination path
  'Confirm.MoveDestination': `Verschieben nach:`,
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
  'Error.DestinationInSystemFolder': `Das Ziel {0} liegt unterhalb eines Windows-Systemordners. Wähle einen Pfad außerhalb von %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% und %ProgramData%.`,
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
  'Error.DestinationChangedMidBatch': `InstallerClean konnte den Sicherungsordner nicht mehr bestätigen und hat deshalb angehalten, statt an die falsche Stelle zu schreiben. Prüfe {0}, dann Neu scannen und noch einmal versuchen.`,

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
  'Display.ListSeparator': `, `,
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
  'Cli.GenericError': `Fehler: unerwarteter Ausfall ({0}). Details in {1} geschrieben.`,
  'Cli.GenericError.NoLog': `Fehler: unerwarteter Ausfall ({0}). Das Absturzprotokoll konnte nicht geschrieben werden.`,
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
  'Cli.Help.ExitCodeOk': `  0   Erfolg: Der Lauf hat getan, worum gebeten wurde, ohne Fehler`,
  'Cli.Help.ExitCodeError': `  1   Fehler: nichts verarbeitet (falsche Argumente, falsches Ziel,&#10;       fehlgeschlagener Scan oder jede Datei fehlgeschlagen)`,
  'Cli.Help.ExitCodePartial': `  2   teilweise: einiges verarbeitet, anderes nicht (Fehler oder Strg+C)`,
  'Cli.Help.ExitCodeTransient': `  75  vorübergehend: etwas hat den Lauf blockiert (siehe Meldung)`,
  'Cli.Help.ExitCodeCancelled': `  130 abgebrochen (Strg+C)`,
  'Body.NotScanned.Lead': `Noch nichts gescannt.`,
  'Body.NotScanned.Why': `Klicke auf „Neu scannen“, um {InstallerFolder} nach Installer-Dateien zu durchsuchen, die kein Programm mehr braucht.`,
  'Confirm.MoveSameDrive': `Dieser Ordner liegt auf demselben Laufwerk, der Platz kommt also erst zurück, wenn du ihn löschst. Wähle stattdessen einen Ordner auf einem anderen Laufwerk, wenn du den Platz sofort haben willst.`,
  'Error.ScanCorrelationFailed': `InstallerClean konnte die Windows-Installer-Einträge nicht mit dem Inhalt von {InstallerFolder} abgleichen. Fast nichts, worauf die Einträge zeigen, ist tatsächlich da, und fast nichts, was da ist, wird von einem Eintrag benannt, deshalb ließ sich für keine Datei zeigen, dass sie nicht benötigt wird. Es wurde nichts angeboten und nichts entfernt.`,
  'Error.CandidateOutsideCache': `Diese Datei liegt nicht direkt im Windows-Installer-Ordner; aus Sicherheitsgründen abgelehnt.`,
  'Completion.ReverifySkipped': `{0} {1} an Ort und Stelle belassen, weil die Einträge jetzt beanspruchen, was der Scan markiert hatte.`,
  'Completion.MoveCancelledSummary': `{0} von {1} {2} verschoben, bevor du abgebrochen hast.`,
  'Completion.PermanentDeleteCancelledSummary': `{0} von {1} {2} endgültig gelöscht, bevor du abgebrochen hast.`,
  'Body.PendingReboot.Lead': `Diese Dateien können gerade nicht aufgeräumt werden.`,
  'Cli.TooManyArguments': `Fehler: Unerwartetes zusätzliches Argument '{0}'. Wenn dein Zielordner ein Leerzeichen enthält, setze den ganzen Pfad in Anführungszeichen: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Der Ordner gilt pro Benutzer; geplante Läufe brauchen /m PFAD.`,
  'Completion.ReverifyIncomplete': `{0} {1} an Ort und Stelle belassen, weil die Windows-Installer-Einträge bei der letzten Prüfung nicht vollständig gelesen werden konnten.`,
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
  'Tooltip.MoveSameDrive': `Verschiebt die nicht benötigten Dateien in den Sicherungsordner. Er liegt auf demselben Laufwerk, du bekommst den Platz also erst zurück, wenn du diesen Ordner löschst oder auf ein anderes Laufwerk verschiebst. Das kannst du tun, sobald du sicher bist, dass sie niemand braucht.`,
  'Completion.MoveRestoreHint.Singular': `Die Datei in diesem Ordner ist [bedenkenlos entfernbar], du kannst den Ordner also löschen, wann immer du magst. Bis dahin kannst du sie zurück nach {InstallerFolder} legen, falls ein Programm sie doch einmal braucht (äußerst unwahrscheinlich).`,
  'Completion.MoveRestoreHint.Plural': `Die Dateien in diesem Ordner sind [bedenkenlos entfernbar], du kannst ihn also löschen, wann immer du magst. Bis dahin kannst du sie zurück nach {InstallerFolder} legen, falls ein Programm doch einmal eine davon braucht (äußerst unwahrscheinlich).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `Die Datei in diesem Ordner ist [bedenkenlos entfernbar], du kannst den Ordner also löschen oder auf ein anderes Laufwerk verschieben, wann immer du den Platz wirklich zurückhaben willst. Bis dahin kannst du sie zurück nach {InstallerFolder} legen, falls ein Programm sie doch einmal braucht (äußerst unwahrscheinlich).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `Die Dateien in diesem Ordner sind [bedenkenlos entfernbar], du kannst ihn also löschen oder auf ein anderes Laufwerk verschieben, wann immer du den Platz wirklich zurückhaben willst. Bis dahin kannst du sie zurück nach {InstallerFolder} legen, falls ein Programm doch einmal eine davon braucht (äußerst unwahrscheinlich).`,
  'Confirm.DeletePermanently.Singular': `Diese Datei wird endgültig gelöscht. Sie lässt sich [bedenkenlos löschen], aber wenn du eine Sicherung möchtest, nimm stattdessen die Schaltfläche Verschieben.`,
  'Confirm.DeletePermanently.Plural': `Die Dateien werden endgültig gelöscht. Sie lassen sich [bedenkenlos löschen], aber wenn du eine Sicherung möchtest, nimm stattdessen die Schaltfläche Verschieben.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean konnte Windows nicht dazu bringen, den echten Pfad von {InstallerFolder} aufzulösen, deshalb ließ sich für keine Datei zeigen, dass sie darin liegt, und keine wurde zum Aufräumen angeboten. Dieser Scan hat nichts gefunden, weil diese Prüfung fehlschlug, nicht weil der Ordner sauber ist. Es wurde nichts entfernt.`,
  'Automation.Scroll.ProductDetails': `Produktdetails`,
  'Body.PendingReboot.Other': `Windows Installer hat etwas in Arbeit, deshalb pausieren Verschieben und Löschen. InstallerClean fasst {InstallerFolder} nicht an, während sich der Ordner ändert. Danach einmal neu scannen, und sie sind wieder da.`,
  'Cli.TooManyArgumentsNoPath': `Fehler: unerwartetes zusätzliches Argument '{0}'. /s und /d nehmen keine weiteren Argumente, und pro Lauf ist nur ein Schalter möglich.`,
  'Cli.MissingFromDisk.Singular': `Windows hat einen Eintrag für {0} Datei, die nicht in {InstallerFolder} liegt: {1}. Im Alltag macht das keine Probleme, aber eine Reparatur, ein Update oder eine Deinstallation kann daran scheitern. Das Installationsprogramm dieses Programms noch einmal auszuführen, möglichst in derselben Version, stellt die Datei meist wieder her.`,
  'Cli.MissingFromDisk.Plural': `Windows hat Einträge für {0} Dateien, die nicht in {InstallerFolder} liegen: {1}. Im Alltag machen sie keine Probleme, aber eine Reparatur, ein Update oder eine Deinstallation kann daran scheitern. Das Installationsprogramm des jeweiligen Programms noch einmal auszuführen, möglichst in derselben Version, stellt die Dateien meist wieder her.`,
  'Cli.MoveNotEnoughSpace': `Fehler: nicht genügend Speicherplatz unter {0}. Das Verschieben dieser Dateien braucht {1}, frei sind {2}. Es wurde nichts verschoben.`,
  'Cli.PendingRebootBlocked.Other': `Fehler: Windows Installer hat etwas in Arbeit, deshalb sind /m und /d blockiert. InstallerClean fasst {InstallerFolder} nicht an, während sich der Ordner ändert. Versuche es erneut, sobald es fertig ist.`,
  'Cli.FoundNoOrphans': `Keine nicht benötigten Dateien gefunden.`,
  'Cli.NothingOffered.Singular': `InstallerClean konnte nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb die eine Datei ({2}) zurückgehalten, die es sonst angeboten hätte.`,
  'Cli.NothingOffered.Plural': `InstallerClean konnte nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb alle {0} {1} ({2}) zurückgehalten, die es sonst angeboten hätte.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean konnte den Sicherungsordner nicht mehr bestätigen und hat deshalb angehalten, statt an die falsche Stelle zu schreiben. Prüfe {0} und führe den Befehl dann erneut aus.`,
  'Cli.Help.Summary': `Entfernt .msi-/.msp-Dateien, die kein installiertes Programm mehr braucht.`,
  'Cli.Help.Elevation': `Nur mit Administratorrechten; Windows startet es sonst gar nicht.`,
  'Error.InstallerLockUnavailableTitle': `Nichts gelöscht`,
  'Error.MoveInstallerLockUnavailableTitle': `Nichts verschoben`,
  'Error.InstallerLockUnavailable': `InstallerClean konnte die Sperre nicht übernehmen, mit der Windows Installer verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts gelöscht. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean konnte die Sperre nicht übernehmen, mit der Windows Installer verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts verschoben. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt.`,
  'Cli.InstallerLockUnavailable': `Fehler: InstallerClean konnte die Windows-Installer-Sperre nicht übernehmen, die verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts gelöscht. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt.`,
  'Cli.MoveInstallerLockUnavailable': `Fehler: InstallerClean konnte die Windows-Installer-Sperre nicht übernehmen, die verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts verschoben. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt.`,
  'Completion.ReverifyRecordsChanged': `{0} {1} an Ort und Stelle belassen, weil sich die Windows-Installer-Einträge bis zur letzten Prüfung geändert hatten.`,
  'Summary.RecordsNotMatched': `InstallerClean konnte nicht alles in den Windows-Einträgen zuordnen und hat sie deshalb nicht vollständig gelesen. Die nicht benötigten Dateien oben sind davon unberührt, aber was hier zu Dateien steht, die in {InstallerFolder} fehlen, kann unvollständig sein. Scanne neu, um es noch einmal zu versuchen.`,
  'Cli.RecordsNotMatched': `InstallerClean konnte nicht alles in den Windows-Einträgen zuordnen und hat sie deshalb nicht vollständig gelesen. Das Gefundene ist davon unberührt, aber was hier zu Dateien steht, die in {InstallerFolder} fehlen, kann unvollständig sein. Ein erneuter Lauf findet vielleicht mehr.`,
  'Completion.ReverifyIdentityClaimed': `{0} {1} an Ort und Stelle belassen, weil Windows einen Eintrag zu dem darin genannten Programm hat.`,
  'Completion.ReverifyIdentityUnreadable': `{0} {1} an Ort und Stelle belassen, weil InstallerClean darin kein Programm benannt fand.`,
  'Completion.ReverifyOwnershipUnestablished': `{0} {1} an Ort und Stelle belassen, weil InstallerClean bis zur letzten Prüfung nicht sicher feststellen konnte, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören.`,
  'Completion.NothingRemoved': `Nichts entfernt`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean konnte die Windows-Installer-Einträge nicht mit dem Inhalt von {InstallerFolder} abgleichen. Der Ordner enthält Dateien, aber kein einziger Eintrag zeigt auf irgendetwas darin, deshalb ließ sich für keine Datei zeigen, dass sie nicht benötigt wird. Es wurde nichts angeboten und nichts entfernt.`,
  'Completion.NothingOffered': `Auf diesem PC nichts angeboten`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean konnte nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb die eine Datei ({1}) zurückgehalten, die es sonst angeboten hätte.`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean konnte nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb alle {0} Dateien ({1}) zurückgehalten, die es sonst angeboten hätte.`,
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
