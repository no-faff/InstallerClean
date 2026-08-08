# InstallerClean in Deutsch (German)

The text of InstallerClean's interface and command-line tool in English on the left, with the German translation beside it, grouped by where each line appears in the app. It is here so someone who really knows German can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.de.resx`](../../src/InstallerClean.Core/Resources/Strings.de.resx), so do not edit it by hand. The German translation itself lives in [`gen-strings-de.mjs`](../../scripts/translations/gen-strings-de.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Deutsch |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Über |
| Registered files that should not be deleted | Registrierte Dateien, die nicht gelöscht werden sollten |
| Unneeded files that are safe to delete | Nicht benötigte Dateien, die bedenkenlos gelöscht werden können |

## Section headings

| English | Deutsch |
| --- | --- |
| PRODUCTS | PRODUKTE |
| PATCHES | PATCHES |
| PRODUCT DETAILS | PRODUKTDETAILS |
| BACKUP FOLDER | BACKUP FOLDER |
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
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
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
| Moving unneeded files... | Moving unneeded files... |
| Deleting unneeded files... | Deleting unneeded files... |
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
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Sie liegen in {InstallerFolder} und blieben zurück, als ein Programm deinstalliert wurde ({0}), ein neuerer Patch einen älteren ersetzt hat ({1}) oder der Herausgeber ihn zurückgezogen hat ({2}). InstallerClean listet nur Dateien auf, die Windows selbst als erledigt meldet. |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | Noch nichts gescannt. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Klicke auf „Neu scannen“, um {InstallerFolder} nach Installer-Dateien zu durchsuchen, die kein Programm mehr braucht. |
| These files can't be cleaned up right now. | Diese Dateien können gerade nicht aufgeräumt werden. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. |
| Select a file to view details. | Wähle eine Datei, um Details anzuzeigen. |
| Select a product to view details. | Wähle ein Produkt, um Details anzuzeigen. |
| No metadata available. | Keine Metadaten verfügbar. |
| This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. |
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
| Nothing removed | Nothing removed |
| Nothing to clean up in {InstallerFolder} | Nichts aufzuräumen in {InstallerFolder} |
| Scanned {0} {1} in {2} | {0} {1} in {2} gescannt |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
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
| {0} {1} kept in place, because the records now claim what the scan flagged. | {0} {1} kept in place, because the records now claim what the scan flagged. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} {1} kept in place, because the Windows Installer records had changed by the final check. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} kept in place, because Windows has a record of the program named inside. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} kept in place, because InstallerClean couldn't find a program named inside. |
| Moved {0} of {1} {2} before you cancelled. | {0} von {1} {2} verschoben, bevor du abgebrochen hast. |
| Permanently deleted {0} of {1} {2} before you cancelled. | {0} von {1} {2} endgültig gelöscht, bevor du abgebrochen hast. |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | Freut mich, dass es geholfen hat. Die Kaffeekasse steht bereit, falls dir großzügig zumute ist. |

## Summaries and counts

| English | Deutsch |
| --- | --- |
| {0} file still needed | {0} Datei noch benötigt |
| {0} files still needed | {0} Dateien noch benötigt |
| {0} unneeded file to clean up | {0} nicht benötigte Datei zum Aufräumen |
| {0} unneeded files to clean up | {0} nicht benötigte Dateien zum Aufräumen |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} registrierte Datei fehlt (nicht von InstallerClean gelöscht). Im Moment kein Problem, aber eine spätere Reparatur, Aktualisierung oder Deinstallation dieses Programms könnte fehlschlagen. Öffne Details, um zu erfahren, was zu tun ist. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} registrierte Dateien fehlen (nicht von InstallerClean gelöscht). Im Moment kein Problem, aber eine spätere Reparatur, Aktualisierung oder Deinstallation dieser Programme könnte fehlschlagen. Öffne Details, um zu erfahren, was zu tun ist. |
| InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| {0} of {1} {2} | {0} von {1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} verwaist, {1} ersetzt, {2} veraltet ({3}) |
| {0} registered file that is still needed ({1}) | {0} registrierte Datei, die noch benötigt wird ({1}) |
| {0} registered files that are still needed ({1}) | {0} registrierte Dateien, die noch benötigt werden ({1}) |

## Confirmation dialogs

| English | Deutsch |
| --- | --- |
| Move {0} {1} ({2})? | {0} {1} verschieben ({2})? |
| Files will be moved to: | Die Dateien werden verschoben nach: |
| Delete {0} {1} ({2})? | {0} {1} löschen ({2})? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

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
| InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean konnte nicht genug von den Einträgen von Windows Installer lesen, um sicher zu sein, was noch gebraucht wird: Die Liste der installierten Programme kam unvollständig zurück, und dieselben Einträge direkt aus der Registrierung zu lesen führte ebenfalls zu Fehlern. Eine Datei könnte allein deshalb verwaist wirken, weil der Eintrag, der sie nennt, zu den unlesbaren gehörte, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. |
| Nothing was deleted | Nichts gelöscht |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Invalid destination | Ungültiges Ziel |
| Could not write to destination | Schreiben am Ziel nicht möglich |
| Move failed | Verschieben fehlgeschlagen |
| Delete failed | Löschen fehlgeschlagen |
| Setting not saved | Einstellung nicht gespeichert |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Die Änderung konnte nicht gespeichert werden. Beim nächsten Start verwendet InstallerClean wieder die vorherige Einstellung. |
| The destination cannot be inside the Windows Installer folder. | Das Ziel darf nicht im Windows-Installer-Ordner liegen. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows hat einen Dateifehler gemeldet; die Datei wurde an ihrem Platz belassen. |
| Windows reported file errors; these files were left in place. | Windows hat Dateifehler gemeldet; diese Dateien wurden an ihrem Platz belassen. |
| Something went wrong with this file; it was left in place. | Bei dieser Datei ist etwas schiefgelaufen; sie wurde an ihrem Platz belassen. |
| Something went wrong with these files; they were left in place. | Bei diesen Dateien ist etwas schiefgelaufen; sie wurden an ihrem Platz belassen. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Das Verschieben von Dateien in den Windows-Installer-Ordner wird abgelehnt (Ziel: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

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
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
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
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
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
| Backup folder | Backup folder |
| Products | Produkte |
| Patches | Patches |
| Product details | Produktdetails |
| Backup folder | Backup folder |
| Operation progress | Vorgangsfortschritt |
| Scan {InstallerFolder} again | {InstallerFolder} erneut scannen |
| Scanning progress | Scan-Fortschritt |
| Startup scan progress | Fortschritt des Start-Scans |
| Details, unneeded files | Details, nicht benötigte Dateien |
| Available for cleanup. | Zum Aufräumen verfügbar. |
| Details, registered files | Details, registrierte Dateien |
| Read-only inventory. | Schreibgeschützte Übersicht. |
| Sorted by {0}, ascending | Sortiert nach {0}, aufsteigend |
| Sorted by {0}, descending | Sortiert nach {0}, absteigend |
| Scan results | Scan-Ergebnisse |
| Result details | Ergebnisdetails |
| File details | Dateidetails |
| Product details | Product details |
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
| Error: unknown argument '{0}' | Error: unknown argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Fehler: Unerwartetes zusätzliches Argument '{0}'. Wenn dein Zielordner ein Leerzeichen enthält, setze den ganzen Pfad in Anführungszeichen: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. |
| Cancelling... | Wird abgebrochen... |
| Cancelled. | Abgebrochen. |
| Error: unexpected failure ({0}). Details written to {1}. | Error: unexpected failure ({0}). Details written to {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Error: unexpected failure ({0}). The crash log could not be written. |
| Scanning {InstallerFolder}... | {InstallerFolder} wird gescannt... |
| Found {0} unneeded {1} to clean up ({2}). | Found {0} unneeded {1} to clean up ({2}). |
| Found no unneeded files. | Found no unneeded files. |
| {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. | {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. |
| {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. | {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. |
| InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Deleting {0} unneeded {1}... | Deleting {0} unneeded {1}... |
| Permanently deleted {0} unneeded {1}. | Permanently deleted {0} unneeded {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Fehler: Kein Zielordner zum Verschieben angegeben. Nutze /m PFAD. (Ein in der GUI gesetztes Standardziel gilt pro Benutzer und greift nicht bei geplanten oder Dienstkonto-Läufen.) |
| Error: destination cannot be inside the Windows Installer folder. | Fehler: Das Ziel darf nicht im Windows-Installer-Ordner liegen. |
| Error: destination must be a fully qualified path. Got: {0} | Fehler: Das Ziel muss ein vollständig qualifizierter Pfad sein. Erhalten: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Moving {0} unneeded {1} to {2}... | Moving {0} unneeded {1} to {2}... |
| Moved {0} unneeded {1}. | Moved {0} unneeded {1}. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Ein anderer InstallerClean-Prozess hält die Einzelinstanz-Sperre (die GUI oder ein anderer CLI-Lauf). Exit-Code 75 (vorübergehend); ein späterer Wiederholungsversuch ist sicher. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Hinweis: Das Schreiben in das Ereignisprotokoll ist fehlgeschlagen. Prüfe die Berechtigungen des Anwendungsprotokolls oder die Gruppenrichtlinie. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - {InstallerFolder} aufräumen |
| Removes cached .msi and .msp files that no installed program still needs. | Removes cached .msi and .msp files that no installed program still needs. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Needs an elevated (administrator) prompt; Windows will not start it. |
| Usage: | Verwendung: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Diese Hilfe anzeigen (auch /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Die Version ausgeben (auch -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PFAD    An den angegebenen Pfad |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. |
| Exit codes: | Exit-Codes: |
|   0   success: the run finished with nothing left to do |   0   success: the run finished with nothing left to do |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partial: some processed, some not (a failure or a Ctrl+C part way) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  vorübergehend: etwas hat den Lauf blockiert (siehe Meldung) |
|   130 cancelled (Ctrl+C) |   130 abgebrochen (Strg+C) |
