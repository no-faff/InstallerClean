# InstallerClean in Deutsch (German)

The text of InstallerClean's interface and command-line tool in English on the left, with the German translation beside it, grouped by where each line appears in the app. It is here so someone who really knows German can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.de.resx`](../../src/InstallerClean.Core/Resources/Strings.de.resx), so do not edit it by hand. The German translation itself lives in [`gen-strings-de.mjs`](../../scripts/translations/gen-strings-de.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Deutsch |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Über |
| Files left alone | Unangetastete Dateien |
| Unneeded files that are safe to delete | Nicht benötigte Dateien, die bedenkenlos gelöscht werden können |

## Section headings

| English | Deutsch |
| --- | --- |
| PATCHES | PATCHES |
| PRODUCT DETAILS | PRODUKTDETAILS |
| BACKUP FOLDER | SICHERUNGSORDNER |
| SAY THANKS | DANKE SAGEN |

## Buttons and actions

| English | Deutsch |
| --- | --- |
| _About | _Über |
| Copy | Kopieren |
| Cut | Ausschneiden |
| Paste | Einfügen |
| Select all | Alles auswählen |
| _Browse... | _Durchsuchen... |
| _Cancel | _Abbrechen |
| Check for _updates | Nach _Updates suchen |
| _Close | _Schließen |
| _Delete permanently | Endgültig _löschen |
| _Done | _Fertig |
| Details | Details |
| _Buy me a cuppa | Spendier mir einen _Kaffee |
| Leave a _star on GitHub | Einen Stern auf _GitHub hinterlassen |
| Apache 2.0 licence | Apache-2.0-Lizenz |
| _Move | _Verschieben |
| Path to folder if you move rather than delete. | Pfad zum Ordner, falls du verschiebst statt zu löschen. |
| Open _release page | _Release-Seite öffnen |
| _Re-scan | _Neu scannen |
| _Scan again | Erneut _scannen |
| Send report | Bericht senden |
| _Send | _Senden |

## About window

| English | Deutsch |
| --- | --- |
| Guide and FAQ | Anleitung und FAQ |
| Report a problem | Ein Problem melden |
| Check for updates automatically | Automatisch nach Updates suchen |

## Field labels

| English | Deutsch |
| --- | --- |
| Reason | Grund |
| Author | Autor |
| Application | Anwendung |
| Title | Titel |
| Subject | Betreff |
| Keywords | Schlüsselwörter |
| Signing certificate | Signaturzertifikat |
| File size | Dateigröße |
| Comment | Kommentar |
| Product name | Produktname |
| File | Datei |
| Size | Größe |
| Patches | Patches |
| (unknown) | (unbekannt) |
| (patches only) | (nur Patches) |
| missing | fehlt |

## Status and progress

| English | Deutsch |
| --- | --- |
| Scanning... | Scannen... |
| Cancelling... | Wird abgebrochen... |
| Starting scan... | Scan wird gestartet... |
| Asking Windows about installed software... | Windows wird nach installierter Software gefragt... |
| Scanning installer cache folder... | Installer-Cache-Ordner wird gescannt... |
| Enumerating installed products... | Installierte Produkte werden aufgezählt... |
| Checking registry for additional packages... | Registrierung wird nach weiteren Paketen durchsucht... |
| Found {0} registered {1}. | {0} registrierte {1} gefunden. |
| Scan complete ({0}) | Scan abgeschlossen ({0}) |
| Scanning local packages... | Lokale Pakete werden gescannt... |
| Found {0} {1} you can safely delete. | {0} {1} gefunden, die du bedenkenlos löschen kannst. |
| Preparing destination folder... | Zielordner wird vorbereitet... |
| Moving unneeded files... | Nicht benötigte Dateien werden verschoben... |
| Deleting unneeded files... | Nicht benötigte Dateien werden gelöscht... |
| Move cancelled. {0} of {1} {2} processed. | Verschieben abgebrochen. {0} von {1} {2} verarbeitet. |
| Delete cancelled. {0} of {1} {2} processed. | Löschen abgebrochen. {0} von {1} {2} verarbeitet. |
| Move failed ({0}). Details in {1}. | Verschieben fehlgeschlagen ({0}). Details in {1}. |
| Move failed ({0}). The crash log could not be written. | Verschieben fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden. |
| Delete failed ({0}). Details in {1}. | Löschen fehlgeschlagen ({0}). Details in {1}. |
| Delete failed ({0}). The crash log could not be written. | Löschen fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden. |
| Access denied. Windows refused the scan. | Zugriff verweigert. Windows hat den Scan abgelehnt. |
| Scan failed: couldn't read the Windows Installer records. | Scan fehlgeschlagen: Die Einträge von Windows Installer konnten nicht gelesen werden. |
| Scan cancelled. | Scan abgebrochen. |
| Ready | Bereit |
| Scan failed ({0}). Details in {1}. | Scan fehlgeschlagen ({0}). Details in {1}. |
| Scan failed ({0}). The crash log could not be written. | Scan fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden. |

## Main screen text

| English | Deutsch |
| --- | --- |
| Any unneeded files below are [safe to delete]. | Alle nicht benötigten Dateien unten kannst du [bedenkenlos löschen]. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Sie liegen in {InstallerFolder}. InstallerClean fragt Windows nach jedem installierten Programm: Eine Datei wird aufgeführt, wenn kein Programm sie beansprucht ({0}) oder wenn ein neuerer Patch sie ersetzt hat und kein Programm auf sie zurückgehen könnte ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update, repair and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Verschiebe sie in einen Sicherungsordner deiner Wahl und lösche diesen Ordner, sobald du sicher bist, dass deine Programme sich weiterhin wie gewohnt aktualisieren, reparieren und deinstallieren lassen. Wenn du sie zurück nach {InstallerFolder} legst, ist alles wiederhergestellt. Oder lösche sie jetzt endgültig. |
| Nothing scanned yet. | Noch nichts gescannt. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Klicke auf „Neu scannen“, um {InstallerFolder} nach Installer-Dateien zu durchsuchen, die kein Programm mehr braucht. |
| These files can't be cleaned up right now. | Diese Dateien können gerade nicht aufgeräumt werden. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Etwas nutzt gerade Windows Installer, etwa ein Windows-Update oder ein Programm, das im Hintergrund installiert wird. Verschieben und Löschen pausieren, solange das läuft, damit InstallerClean {InstallerFolder} nicht anfasst, während sich der Ordner ändert. Danach einmal neu scannen, und sie sind wieder da. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Auf diesem Rechner ist eine frühere Windows-Installer-Transaktion angehalten. Setze diese Installation fort oder mache sie rückgängig (oder starte Windows neu), bevor du {InstallerFolder} aufräumst. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows hat für den nächsten Neustart eine Dateiumbenennung eingeplant, die {InstallerFolder} betrifft. Starte Windows neu, bevor du aufräumst. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer hat etwas in Arbeit, deshalb pausieren Verschieben und Löschen. InstallerClean fasst {InstallerFolder} nicht an, während sich der Ordner ändert. Danach einmal neu scannen, und sie sind wieder da. |
| Select a file to view details. | Wähle eine Datei, um Details anzuzeigen. |
| Select a product to view details. | Wähle ein Produkt, um Details anzuzeigen. |
| No metadata available. | Keine Metadaten verfügbar. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. | Diese Installationsdatei fehlt. Das macht jetzt keine Probleme und wird es auch nicht tun, bis du eines Tages das zugehörige Programm reparieren, aktualisieren oder deinstallieren willst. Dieser Schritt kann dann fehlschlagen, weil Windows nach dieser Datei sucht und sie nicht da ist.<br><br>Um das zu beheben, lade das Installationsprogramm beim Hersteller herunter und führe es über deiner vorhandenen Installation aus (deinstalliere nicht zuerst, denn das Deinstallieren ist selbst ein Schritt, der diese Datei braucht). Nimm nach Möglichkeit genau die Version, die du installiert hast, da Windows eine andere ablehnen kann. Das sollte die Datei wiederherstellen und deine Einstellungen unangetastet lassen, garantiert wird es von Microsoft aber nicht, und dessen eigenes letztes Mittel ist, das Programm neu zu installieren. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | Das README [erklärt diesen Ordner], und wie sich eine Datei wiederherstellen lässt, mit Microsofts eigenen Worten. |
| (none) | (keine) |

## Reasons a file is unneeded

| English | Deutsch |
| --- | --- |
| Orphaned | Verwaist |
| Superseded | Ersetzt |
| Obsoleted | Veraltet |

## Completion screen

| English | Deutsch |
| --- | --- |
| All clean | Alles sauber |
| Nothing removed | Nichts entfernt |
| Nothing to clean up in {InstallerFolder} | Nichts aufzuräumen in {InstallerFolder} |
| Scanned {0} {1} in {2} | {0} {1} in {2} gescannt |
| Nothing offered on this PC | Auf diesem PC nichts angeboten |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({1}) it might otherwise have offered. | InstallerClean konnte nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb die eine Datei ({1}) zurückgehalten, die es sonst angeboten hätte. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} files ({1}) it might otherwise have offered. | InstallerClean konnte nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb alle {0} Dateien ({1}) zurückgehalten, die es sonst angeboten hätte. |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | Die Datei in diesem Ordner ist [bedenkenlos entfernbar], du kannst den Ordner also löschen, wann immer du magst. Bis dahin kannst du sie zurück nach {InstallerFolder} legen, falls ein Programm sie doch einmal braucht (äußerst unwahrscheinlich). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | Die Dateien in diesem Ordner sind [bedenkenlos entfernbar], du kannst ihn also löschen, wann immer du magst. Bis dahin kannst du sie zurück nach {InstallerFolder} legen, falls ein Programm doch einmal eine davon braucht (äußerst unwahrscheinlich). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | Die Datei in diesem Ordner ist [bedenkenlos entfernbar], du kannst den Ordner also löschen oder auf ein anderes Laufwerk verschieben, wann immer du den Platz wirklich zurückhaben willst. Bis dahin kannst du sie zurück nach {InstallerFolder} legen, falls ein Programm sie doch einmal braucht (äußerst unwahrscheinlich). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | Die Dateien in diesem Ordner sind [bedenkenlos entfernbar], du kannst ihn also löschen oder auf ein anderes Laufwerk verschieben, wann immer du den Platz wirklich zurückhaben willst. Bis dahin kannst du sie zurück nach {InstallerFolder} legen, falls ein Programm doch einmal eine davon braucht (äußerst unwahrscheinlich). |
| {0} freed | {0} freigegeben |
| {0} moved | {0} verschoben |
| Nothing was moved | Nichts verschoben |
| Nothing was deleted | Nichts gelöscht |
| {0} of {1} could not be moved. | {0} Datei von {1} konnte nicht verschoben werden. |
| {0} of {1} could not be moved. | {0} Dateien von {1} konnten nicht verschoben werden. |
| {0} of {1} could not be deleted. | {0} Datei von {1} konnte nicht gelöscht werden. |
| {0} of {1} could not be deleted. | {0} Dateien von {1} konnten nicht gelöscht werden. |
| {0} {1} moved to: {2} | {0} {1} verschoben nach: {2} |
| {0} {1} moved to: {2} | {0} {1} verschoben nach: {2} |
| {0} {1} kept in place, because the records now claim what the scan flagged. | {0} {1} an Ort und Stelle belassen, weil die Einträge jetzt beanspruchen, was der Scan markiert hatte. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} {1} an Ort und Stelle belassen, weil sich die Windows-Installer-Einträge bis zur letzten Prüfung geändert hatten. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} {1} an Ort und Stelle belassen, weil die Windows-Installer-Einträge bei der letzten Prüfung nicht vollständig gelesen werden konnten. |
| {0} {1} kept in place, because by the final check InstallerClean could not be certain which cached files belong to the programs installed here. | {0} {1} an Ort und Stelle belassen, weil InstallerClean bis zur letzten Prüfung nicht sicher feststellen konnte, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} an Ort und Stelle belassen, weil Windows einen Eintrag zu dem darin genannten Programm hat. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} an Ort und Stelle belassen, weil InstallerClean darin kein Programm benannt fand. |
| Moved {0} of {1} {2} before you cancelled. | {0} von {1} {2} verschoben, bevor du abgebrochen hast. |
| Permanently deleted {0} of {1} {2} before you cancelled. | {0} von {1} {2} endgültig gelöscht, bevor du abgebrochen hast. |
| {0} {1} permanently deleted | {0} {1} endgültig gelöscht |
| {0} {1} permanently deleted | {0} {1} endgültig gelöscht |
| Glad to help. There's a tip jar if you're feeling kind. | Freut mich, dass es geholfen hat. Die Kaffeekasse steht bereit, falls dir großzügig zumute ist. |

## Summaries and counts

| English | Deutsch |
| --- | --- |
| {0} file left alone | {0} Datei unangetastet |
| {0} files left alone | {0} Dateien unangetastet |
| {0} unneeded file to clean up | {0} nicht benötigte Datei zum Aufräumen |
| {0} unneeded files to clean up | {0} nicht benötigte Dateien zum Aufräumen |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Open Details for what to do. | Windows hat einen Eintrag für {0} Datei, die nicht in {InstallerFolder} liegt: {1}. Im Alltag macht das keine Probleme, aber eine Reparatur, ein Update oder eine Deinstallation kann daran scheitern. Öffne Details, um zu erfahren, was zu tun ist. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Open Details for what to do. | Windows hat Einträge für {0} Dateien, die nicht in {InstallerFolder} liegen: {1}. Im Alltag machen sie keine Probleme, aber eine Reparatur, ein Update oder eine Deinstallation kann daran scheitern. Öffne Details, um zu erfahren, was zu tun ist. |
| {0} other program | {0} weiteres Programm |
| {0} other programs | {0} weitere Programme |
| {0} file with no program named in the records | {0} Datei, zu der die Einträge kein Programm nennen |
| {0} files with no program named in the records | {0} Dateien, zu denen die Einträge kein Programm nennen |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than listing it. | Auf diesem PC konnte InstallerClean nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb die eine Datei zurückgehalten, statt sie aufzuführen. |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than listing them. | Auf diesem PC konnte InstallerClean nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb {0} {1} zurückgehalten, statt sie aufzuführen. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. The unneeded files above are unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Re-scan to try again. | InstallerClean konnte nicht alles in den Windows-Einträgen zuordnen und hat sie deshalb nicht vollständig gelesen. Die nicht benötigten Dateien oben sind davon unberührt, aber was hier zu Dateien steht, die in {InstallerFolder} fehlen, kann unvollständig sein. Scanne neu, um es noch einmal zu versuchen. |
| {0} of {1} {2} | {0} von {1} {2} |
| {0} unneeded {1} ({2}) | {0} nicht benötigte {1} ({2}) |
| {0} file left alone ({1}) | {0} Datei unangetastet ({1}) |
| {0} files left alone ({1}) | {0} Dateien unangetastet ({1}) |

## Confirmation dialogs

| English | Deutsch |
| --- | --- |
| Move {0} {1} ({2})? | {0} {1} verschieben ({2})? |
| Move to: | Verschieben nach: |
| Delete {0} {1} ({2})? | {0} {1} löschen ({2})? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | Diese Datei wird endgültig gelöscht. Sie lässt sich [bedenkenlos löschen], aber wenn du eine Sicherung möchtest, nimm stattdessen die Schaltfläche Verschieben. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Die Dateien werden endgültig gelöscht. Sie lassen sich [bedenkenlos löschen], aber wenn du eine Sicherung möchtest, nimm stattdessen die Schaltfläche Verschieben. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Dieser Ordner liegt auf demselben Laufwerk, der Platz kommt also erst zurück, wenn du ihn löschst. Wähle stattdessen einen Ordner auf einem anderen Laufwerk, wenn du den Platz sofort haben willst. |

## Error messages

| English | Deutsch |
| --- | --- |
| Access denied | Zugriff verweigert |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows hat InstallerClean den Zugriff verweigert, deshalb hat es abgebrochen. Es wurde nichts entfernt.<br><br>InstallerClean lief bereits als Administrator, es noch einmal so zu starten hilft also nicht. Windows sagt nicht genauer, was den Zugriff verweigert hat, es gibt also nichts Bestimmtes, das du versuchen könntest. |
| Couldn't read the Windows Installer records | Die Einträge von Windows Installer konnten nicht gelesen werden |
| Scan failed | Scan fehlgeschlagen |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Die Einträge von Windows Installer kamen völlig leer zurück: Nicht ein einziges installiertes Programm und kein einziges Update beansprucht eine zwischengespeicherte Installationsdatei. Auf einem funktionierenden System kommt das nicht vor (selbst eine frische Windows-Installation hat welche), also sind die Einträge entweder beschädigt oder sie konnten nicht gelesen werden, und ein Scan, der diese Antwort glaubt, würde jede Datei in {InstallerFolder} fälschlich als verwaist einstufen. InstallerClean hat stattdessen abgebrochen. Es wurde nichts entfernt. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer hat es InstallerClean verweigert, die installierte Software aufzulisten. InstallerClean lief bereits als Administrator, es erneut als Administrator auszuführen ändert also nichts. Ohne diese Liste lässt sich nicht sicher sagen, welche zwischengespeicherten Dateien noch gebraucht werden, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer konnte InstallerClean keine lesbare Liste der installierten Programme geben: {0} Einträge in Folge kamen unlesbar zurück (letzter Fehlercode {1}). Statt mit einer nur teilweise gelesenen Liste zu arbeiten, hat InstallerClean abgebrochen. Es wurde nichts entfernt. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer hat das Ende der Liste der installierten Programme nie signalisiert: InstallerClean hat nach {0} Einträgen aufgegeben (letzter Fehlercode {1}). Einer Liste ohne Ende ist nicht zu trauen, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer hat das Ende der Patch-Liste eines Programms nie signalisiert: InstallerClean hat nach {0} Einträgen aufgegeben (letzter Fehlercode {1}). Einer Liste ohne Ende ist nicht zu trauen, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean konnte die Windows-Installer-Einträge nicht mit dem Inhalt von {InstallerFolder} abgleichen. Fast nichts, worauf die Einträge zeigen, ist tatsächlich da, und fast nichts, was da ist, wird von einem Eintrag benannt, deshalb ließ sich für keine Datei zeigen, dass sie nicht benötigt wird. Es wurde nichts angeboten und nichts entfernt. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean konnte die Windows-Installer-Einträge nicht mit dem Inhalt von {InstallerFolder} abgleichen. Der Ordner enthält Dateien, aber kein einziger Eintrag zeigt auf irgendetwas darin, deshalb ließ sich für keine Datei zeigen, dass sie nicht benötigt wird. Es wurde nichts angeboten und nichts entfernt. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean konnte nicht genug von den Einträgen von Windows Installer lesen, um sicher zu sein, was noch gebraucht wird: Die Liste der installierten Programme kam unvollständig zurück, und dieselben Einträge direkt aus der Registrierung zu lesen führte ebenfalls zu Fehlern. Eine Datei könnte allein deshalb verwaist wirken, weil der Eintrag, der sie nennt, zu den unlesbaren gehörte, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean konnte Windows nicht dazu bringen, den echten Pfad von {InstallerFolder} aufzulösen, deshalb ließ sich für keine Datei zeigen, dass sie darin liegt, und keine wurde zum Aufräumen angeboten. Dieser Scan hat nichts gefunden, weil diese Prüfung fehlschlug, nicht weil der Ordner sauber ist. Es wurde nichts entfernt. |
| Nothing was deleted | Nichts gelöscht |
| Nothing was moved | Nichts verschoben |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean konnte die Sperre nicht übernehmen, mit der Windows Installer verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts gelöscht. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean konnte die Sperre nicht übernehmen, mit der Windows Installer verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts verschoben. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt. |
| Invalid destination | Ungültiges Ziel |
| Could not write to destination | Schreiben am Ziel nicht möglich |
| Move failed | Verschieben fehlgeschlagen |
| Delete failed | Löschen fehlgeschlagen |
| Setting not saved | Einstellung nicht gespeichert |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Die Änderung konnte nicht gespeichert werden. Beim nächsten Start verwendet InstallerClean wieder die vorherige Einstellung. |
| The destination cannot be inside the Windows Installer folder. | Das Ziel darf nicht im Windows-Installer-Ordner liegen. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Das Ziel {0} liegt unterhalb eines Windows-Systemordners. Wähle einen Pfad außerhalb von %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% und %ProgramData%. |
| Not enough space | Nicht genügend Speicherplatz |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Nicht genügend Speicherplatz unter {0}<br><br>Benötigt: {1}<br>Verfügbar: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | Du hast keine Berechtigung, in {0} zu schreiben.<br>Versuch einen Ordner in deinem Benutzerprofil oder auf einem Laufwerk, das dir gehört. |
| The path {0} is too long for Windows. Pick a shorter path. | Der Pfad {0} ist zu lang für Windows. Wähle einen kürzeren Pfad. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | Der Ordner {0} existiert nicht und konnte nicht erstellt werden. Prüfe den Laufwerkbuchstaben oder den Netzwerkpfad. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows kann nicht in {0} schreiben.<br>Details in {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows kann nicht in {0} schreiben. Das Absturzprotokoll konnte nicht geschrieben werden. |
| Cannot write to {0}.<br>Details in {1}. | Schreiben in {0} nicht möglich.<br>Details in {1}. |
| Cannot write to {0}. The crash log could not be written. | Schreiben in {0} nicht möglich. Das Absturzprotokoll konnte nicht geschrieben werden. |
| File no longer exists. | Die Datei existiert nicht mehr. |
| Source file is a symlink or junction; refused for safety. | Die Quelldatei ist ein Symlink oder eine Junction; aus Sicherheitsgründen abgelehnt. |
| This file is not directly inside the Windows Installer folder; refused for safety. | Diese Datei liegt nicht direkt im Windows-Installer-Ordner; aus Sicherheitsgründen abgelehnt. |
| Windows refused access to this file; it was left in place. | Windows hat den Zugriff auf diese Datei verweigert; sie wurde an ihrem Platz belassen. |
| Windows refused access to these files; they were left in place. | Windows hat den Zugriff auf diese Dateien verweigert; sie wurden an ihrem Platz belassen. |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | Diese Datei ist von einem anderen Programm geöffnet oder gesperrt, deshalb kann sie gerade nicht entfernt werden. Sie wurde an Ort und Stelle belassen; versuche es später noch einmal. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | Diese Dateien sind von einem anderen Programm geöffnet oder gesperrt, deshalb können sie gerade nicht entfernt werden. Sie wurden an Ort und Stelle belassen; versuche es später noch einmal. |
| Windows reported a file error; the file was left in place. | Windows hat einen Dateifehler gemeldet; die Datei wurde an ihrem Platz belassen. |
| Windows reported file errors; these files were left in place. | Windows hat Dateifehler gemeldet; diese Dateien wurden an ihrem Platz belassen. |
| Something went wrong with this file; it was left in place. | Bei dieser Datei ist etwas schiefgelaufen; sie wurde an ihrem Platz belassen. |
| Something went wrong with these files; they were left in place. | Bei diesen Dateien ist etwas schiefgelaufen; sie wurden an ihrem Platz belassen. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Das Verschieben von Dateien in den Windows-Installer-Ordner wird abgelehnt (Ziel: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Der Sicherungsordner muss ein vollständiger Pfad zu einem Ordner sein, beginnend mit einem Laufwerksbuchstaben oder einer Netzwerkfreigabe (zum Beispiel D:\Backup oder \\server\backup). InstallerClean kann diesen hier nicht verwenden: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean konnte den Sicherungsordner nicht mehr bestätigen und hat deshalb angehalten, statt an die falsche Stelle zu schreiben. Prüfe {0}, dann Neu scannen und noch einmal versuchen. |
| Cannot write to {0}. | Schreiben in {0} nicht möglich. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Nach 10.000 Versuchen konnte kein eindeutiger Dateiname für '{0}' gefunden werden. |

## Update check

| English | Deutsch |
| --- | --- |
| Check for updates | Nach Updates suchen |
| Checking... | Wird geprüft... |
| Up to date. | Auf dem neuesten Stand. |
| Version {0} is available. | Version {0} ist verfügbar. |
| Update available | Update verfügbar |
| You're running version {0}.<br>Version {1} is available. | Du verwendest Version {0}.<br>Version {1} ist verfügbar. |
| Couldn't reach GitHub. Check your internet connection and try again. | GitHub war nicht erreichbar. Prüfe deine Internetverbindung und versuch es erneut. |
| GitHub returned an error response. Try again in a few minutes. | GitHub hat eine Fehlerantwort zurückgegeben. Versuch es in ein paar Minuten erneut. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Die Antwort von GitHub enthielt keine erkennbare Version. Versuch es später erneut oder öffne die Releases-Seite direkt. |
| The check timed out. Your connection to GitHub may be slow; try again. | Bei der Prüfung wurde das Zeitlimit überschritten. Deine Verbindung zu GitHub ist vielleicht langsam; versuch es erneut. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | Die Prüfung ist aus unbekanntem Grund fehlgeschlagen. Details stehen in crash.log, falls du es melden möchtest. |

## Opening links in your browser

| English | Deutsch |
| --- | --- |
| Couldn't open your browser | Browser konnte nicht geöffnet werden |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean konnte deinen Browser nicht öffnen. Der Link liegt in deiner Zwischenablage, du kannst ihn also selbst einfügen:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean konnte deinen Browser nicht öffnen und den Link auch nicht in die Zwischenablage kopieren. Der Link lautet:<br><br>{0} |

## Sending the summary

| English | Deutsch |
| --- | --- |
| Sending... | Wird gesendet... |
| Thanks! Report sent. | Danke! Bericht gesendet. |
| Sending failed. Try again later. | Senden fehlgeschlagen. Versuch es später erneut. |
| No report to send. | Kein Bericht zum Senden. |
| Send this? | Das senden? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | Es geht an nofaff.netlify.app/api/result-log. Nichts identifiziert dich oder deinen Rechner; es zeigt mir nur, dass InstallerClean funktioniert und [wie viel Platz die Leute freigeben]. |

## Startup and crashes

| English | Deutsch |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean läuft bereits. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Ein unerwarteter Fehler ist aufgetreten und InstallerClean muss geschlossen werden.<br><br>{0}<br><br>Details gespeichert unter:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Ein unerwarteter Fehler ist aufgetreten und InstallerClean muss geschlossen werden.<br><br>{0}<br><br>Das Absturzprotokoll konnte nicht geschrieben werden. |
| Startup error | Startfehler |
| Failed to start ({0}). Details written to:<br>{1} | Start fehlgeschlagen ({0}). Details gespeichert unter:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Start fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log erfasst unbehandelte Ausnahmen von InstallerClean.<br># Mit erhöhten Rechten können die Ausnahmemeldungen des Frameworks<br># Dateipfade aus der laufenden Sitzung enthalten (auch Profile<br># anderer Benutzer, die Windows-Installer-Abfragen aufzählen).<br># Meldungen über Netzwerkfehler bei der Updateprüfung oder beim<br># Senden des Ergebnisprotokolls können die Ziel-URL und die<br># aufgelöste IP- oder Proxyadresse enthalten. Einträge über<br># unlesbare Windows-Installer-Einträge können eine Windows-Konto-SID<br># (S-1-5-21-...) und die Produktcodes installierter Software<br># enthalten.<br># Entferne alle drei Arten von Angaben, bevor du diese Datei an<br># einen öffentlichen Fehlerbericht anhängst.<br> |

## Tooltips (hover text)

| English | Deutsch |
| --- | --- |
| It's thirsty work! | Das macht durstig! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Abbruch angefordert. InstallerClean wartet, bis der aktuelle Schritt einen Haltepunkt erreicht. Bei starker Datenträgeraktivität oder einem MSI-Datenbankaufruf kann das ein paar Sekunden dauern. |
| Close | Schließen |
| A star helps other people find it. | Ein Stern hilft anderen, InstallerClean zu finden. |
| Minimise | Minimieren |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Ganz wie du magst, aber ich freue mich darüber. Sendet eine anonyme Zusammenfassung, die mir nur zeigt, ob es funktioniert und wie viel Platz die Leute freigeben. Auf dem nächsten Bildschirm siehst du vor dem Bestätigen, was gesendet wird. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Ganz wie du magst, aber ich freue mich darüber. Sendet eine anonyme Zusammenfassung, die mir nur zeigt, ob es funktioniert. Auf dem nächsten Bildschirm siehst du vor dem Bestätigen, was gesendet wird. |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Verschiebt die nicht benötigten Dateien in den Sicherungsordner. Lösche diesen Ordner, sobald du sicher bist, dass sie niemand braucht. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Verschiebt die nicht benötigten Dateien in einen Sicherungsordner. Den wählst du gleich aus. Lösche diesen Ordner, sobald du sicher bist, dass sie niemand braucht. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Verschiebt die nicht benötigten Dateien in den Sicherungsordner. Er liegt auf demselben Laufwerk, du bekommst den Platz also erst zurück, wenn du diesen Ordner löschst oder auf ein anderes Laufwerk verschiebst. Das kannst du tun, sobald du sicher bist, dass sie niemand braucht. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Löscht die nicht benötigten Dateien endgültig. Sie lassen sich bedenkenlos entfernen, und du bekommst den Platz sofort zurück. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Antragstellername aus dem eingebetteten Authenticode-Zertifikat. Die Zertifikatskette wurde nicht geprüft. |
| Change language. The program will restart. | Sprache ändern. Das Programm wird neu gestartet. |

## Screen reader labels

| English | Deutsch |
| --- | --- |
| Donate | Spenden |
| Buy me a cuppa | Spendier mir einen Kaffee |
| Cancel operation | Vorgang abbrechen |
| Cancel scan | Scan abbrechen |
| Cancel startup scan | Start-Scan abbrechen |
| Close | Schließen |
| Close window | Fenster schließen |
| Close result and return to main window | Ergebnis schließen und zum Hauptfenster zurückkehren |
| Leave a star on github | Einen Stern auf github hinterlassen |
| Minimise | Minimieren |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Endgültig löschen entfernt die nicht benötigten Dateien. Abbrechen schließt das Fenster, ohne zu löschen. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Verschieben legt die nicht benötigten Dateien in den gewählten Zielordner. Abbrechen lässt sie, wo sie sind. |
| Say thanks | Danke sagen |
| Send posts the report shown to No Faff. Cancel sends nothing. | Senden übermittelt den angezeigten Bericht an No Faff. Abbrechen sendet nichts. |
| Check for updates | Nach Updates suchen |
| Checks github's releases page for a newer version. | Sucht auf der Release-Seite von github nach einer neueren Version. |
| Opens the readme on github in your browser. | Öffnet das readme auf github in deinem Browser. |
| Opens the issue tracker on github.com in your browser. | Öffnet den Issue-Tracker auf github.com in deinem Browser. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Wenn aktiviert, sucht InstallerClean beim Start auf github nach einer neueren Version. |
| Open the release page to download the newer version, or cancel to keep the current version. | Öffne die Release-Seite, um die neuere Version herunterzuladen, oder brich ab, um die aktuelle Version zu behalten. |
| Opens the licence file on github.com in your browser. | Öffnet die Lizenzdatei auf github.com in deinem Browser. |
| Backup folder | Sicherungsordner |
| Patches | Patches |
| Product details | Produktdetails |
| Backup folder | Sicherungsordner |
| Operation progress | Vorgangsfortschritt |
| Scan {InstallerFolder} again | {InstallerFolder} erneut scannen |
| Scanning progress | Scan-Fortschritt |
| Startup scan progress | Fortschritt des Start-Scans |
| Details, unneeded files | Details, nicht benötigte Dateien |
| Available for cleanup. | Zum Aufräumen verfügbar. |
| Details, files left alone | Details, unangetastete Dateien |
| Read-only inventory. | Schreibgeschützte Übersicht. |
| Sorted by {0}, ascending | Sortiert nach {0}, aufsteigend |
| Sorted by {0}, descending | Sortiert nach {0}, absteigend |
| Scan results | Scan-Ergebnisse |
| Result details | Ergebnisdetails |
| File details | Dateidetails |
| Product details | Produktdetails |
| Dialog text | Dialogtext |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Dateien, die nicht verarbeitet werden konnten |
| Explains this folder, and how to recover a file, in the README | Erklärt diesen Ordner, und wie sich eine Datei wiederherstellen lässt, im README |
| Report preview | Vorschau des Berichts |
| Change language | Sprache ändern |
| The program will restart. | Das Programm wird neu gestartet. |

## File picker

| English | Deutsch |
| --- | --- |
| Choose destination folder for moved files | Zielordner für verschobene Dateien wählen |

## Version

| English | Deutsch |
| --- | --- |
| Version {0} | Version {0} |

## Word forms (singular and plural)

| English | Deutsch |
| --- | --- |
| file | Datei |
| files | Dateien |
| error | Fehler |
| errors | Fehler |
| package | Paket |
| packages | Pakete |
| product | Produkt |
| products | Produkte |
| patch | Patch |
| patches | Patches |

## Sizes and times

| English | Deutsch |
| --- | --- |
| ,  | ,  |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | weniger als eine Sekunde |
| {0:F1} seconds | {0:F1} Sekunden |

## Command-line tool (installerclean-cli)

| English | Deutsch |
| --- | --- |
| Error: unknown argument '{0}' | Fehler: unbekanntes Argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Fehler: Unerwartetes zusätzliches Argument '{0}'. Wenn dein Zielordner ein Leerzeichen enthält, setze den ganzen Pfad in Anführungszeichen: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Fehler: unerwartetes zusätzliches Argument '{0}'. /s und /d nehmen keine weiteren Argumente, und pro Lauf ist nur ein Schalter möglich. |
| Cancelling... | Wird abgebrochen... |
| Cancelled. | Abgebrochen. |
| Error: unexpected failure ({0}). Details written to {1}. | Fehler: unerwarteter Ausfall ({0}). Details in {1} geschrieben. |
| Error: unexpected failure ({0}). The crash log could not be written. | Fehler: unerwarteter Ausfall ({0}). Das Absturzprotokoll konnte nicht geschrieben werden. |
| Scanning {InstallerFolder}... | {InstallerFolder} wird gescannt... |
| Found {0} unneeded {1} to clean up ({2}). | {0} nicht benötigte {1} zum Aufräumen gefunden ({2}). |
| Found no unneeded files. | Keine nicht benötigten Dateien gefunden. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back the one file ({2}) it might otherwise have offered. | InstallerClean konnte nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb die eine Datei ({2}) zurückgehalten, die es sonst angeboten hätte. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back all {0} {1} ({2}) it might otherwise have offered. | InstallerClean konnte nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb alle {0} {1} ({2}) zurückgehalten, die es sonst angeboten hätte. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Running that program's installer again, preferably the same version, usually restores the file. | Windows hat einen Eintrag für {0} Datei, die nicht in {InstallerFolder} liegt: {1}. Im Alltag macht das keine Probleme, aber eine Reparatur, ein Update oder eine Deinstallation kann daran scheitern. Das Installationsprogramm dieses Programms noch einmal auszuführen, möglichst in derselben Version, stellt die Datei meist wieder her. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Running each program's installer again, preferably the same version, usually restores the files. | Windows hat Einträge für {0} Dateien, die nicht in {InstallerFolder} liegen: {1}. Im Alltag machen sie keine Probleme, aber eine Reparatur, ein Update oder eine Deinstallation kann daran scheitern. Das Installationsprogramm des jeweiligen Programms noch einmal auszuführen, möglichst in derselben Version, stellt die Dateien meist wieder her. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. What it found is unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Running it again may pick up more. | InstallerClean konnte nicht alles in den Windows-Einträgen zuordnen und hat sie deshalb nicht vollständig gelesen. Das Gefundene ist davon unberührt, aber was hier zu Dateien steht, die in {InstallerFolder} fehlen, kann unvollständig sein. Ein erneuter Lauf findet vielleicht mehr. |
| Deleting {0} unneeded {1}... | {0} nicht benötigte {1} werden gelöscht... |
| Permanently deleted {0} unneeded {1}. | {0} nicht benötigte {1} endgültig gelöscht. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Fehler: Kein Zielordner zum Verschieben angegeben. Nutze /m PFAD. (Ein in der GUI gesetztes Standardziel gilt pro Benutzer und greift nicht bei geplanten oder Dienstkonto-Läufen.) |
| Error: destination cannot be inside the Windows Installer folder. | Fehler: Das Ziel darf nicht im Windows-Installer-Ordner liegen. |
| Error: destination must be a fully qualified path. Got: {0} | Fehler: Das Ziel muss ein vollständig qualifizierter Pfad sein. Erhalten: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Fehler: das Ziel {0} liegt unterhalb eines Windows-Systemordners. Wähle einen Pfad außerhalb von %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% und %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Fehler: nicht genügend Speicherplatz unter {0}. Das Verschieben dieser Dateien braucht {1}, frei sind {2}. Es wurde nichts verschoben. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Fehler: etwas nutzt gerade Windows Installer, etwa ein Windows-Update oder ein Programm, das im Hintergrund installiert wird. /m und /d sind blockiert, solange das läuft. Versuche es erneut, sobald es fertig ist. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Fehler: auf diesem Rechner ist eine frühere Windows-Installer-Transaktion angehalten. Setze diese Installation fort oder mache sie rückgängig (oder starte Windows neu), bevor du {InstallerFolder} aufräumst. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Fehler: ein für den Neustart eingeplanter Dateivorgang betrifft {InstallerFolder} ({0}). Starte Windows neu, damit dieser Vorgang abgeschlossen wird, bevor du aufräumst. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Fehler: Windows Installer hat etwas in Arbeit, deshalb sind /m und /d blockiert. InstallerClean fasst {InstallerFolder} nicht an, während sich der Ordner ändert. Versuche es erneut, sobald es fertig ist. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Fehler: InstallerClean konnte die Windows-Installer-Sperre nicht übernehmen, die verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts gelöscht. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Fehler: InstallerClean konnte die Windows-Installer-Sperre nicht übernehmen, die verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts verschoben. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt. |
| Moving {0} unneeded {1} to {2}... | {0} nicht benötigte {1} werden nach {2} verschoben... |
| Moved {0} unneeded {1}. | {0} nicht benötigte {1} verschoben. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean konnte den Sicherungsordner nicht mehr bestätigen und hat deshalb angehalten, statt an die falsche Stelle zu schreiben. Prüfe {0} und führe den Befehl dann erneut aus. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Ein anderer InstallerClean-Prozess hält die Einzelinstanz-Sperre (die GUI oder ein anderer CLI-Lauf). Exit-Code 75 (vorübergehend); ein späterer Wiederholungsversuch ist sicher. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Hinweis: Das Schreiben in das Ereignisprotokoll ist fehlgeschlagen. Prüfe die Berechtigungen des Anwendungsprotokolls oder die Gruppenrichtlinie. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - {InstallerFolder} aufräumen |
| Removes cached .msi and .msp files that no installed program still needs. | Entfernt .msi-/.msp-Dateien, die kein installiertes Programm mehr braucht. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Nur mit Administratorrechten; Windows startet es sonst gar nicht. |
| Usage: | Verwendung: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Diese Hilfe anzeigen (auch /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Die Version ausgeben (auch -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Nur scannen - nicht benötigte Dateien |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Nicht benötigte Dateien endgültig löschen |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         In den gespeicherten Sicherungsordner |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PFAD    An den angegebenen Pfad |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blockiert die Eingabeaufforderung bis zum Ende,<br>damit ein Skript oder eine geplante Aufgabe darauf warten kann. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Der Ordner gilt pro Benutzer; geplante Läufe brauchen /m PFAD. |
| Exit codes: | Exit-Codes: |
|   0   success: the run did what it was asked and nothing failed |   0   Erfolg: Der Lauf hat getan, worum gebeten wurde, ohne Fehler |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   Fehler: nichts verarbeitet (falsche Argumente, falsches Ziel,<br>       fehlgeschlagener Scan oder jede Datei fehlgeschlagen) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   teilweise: einiges verarbeitet, anderes nicht (Fehler oder Strg+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  vorübergehend: etwas hat den Lauf blockiert (siehe Meldung) |
|   130 cancelled (Ctrl+C) |   130 abgebrochen (Strg+C) |
