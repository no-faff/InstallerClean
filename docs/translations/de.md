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
| The anonymous report | Der anonyme Bericht |
| InstallerClean couldn't read the report. | InstallerClean konnte den Bericht nicht lesen. |

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
| Carry on _without it | Ohne _Warten fortfahren |
| Check for _updates | Nach _Updates suchen |
| _Close | _Schließen |
| _Close without tipping | _Ohne Trinkgeld schließen |
| _Delete permanently | Endgültig _löschen |
| _Done | _Fertig |
| Details | Details |
| _Leave a tip | _Trinkgeld geben |
| Leave a tip | Trinkgeld geben |
| _Leave a tip | _Trinkgeld geben |
| Leave a _star on GitHub | Einen Stern auf _GitHub hinterlassen |
| Apache 2.0 licence | Apache-2.0-Lizenz |
| _Move | _Verschieben |
| Path to folder if you move rather than delete. | Pfad zum Ordner, falls du verschiebst statt zu löschen. |
| _Re-scan | _Neu scannen |

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
| (no program) | (kein Programm) |
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
| Checking registry for additional packages... | Registrierung wird nach weiteren Paketen durchsucht... |
| Matching files against installed programs... | Dateien werden mit den installierten Programmen abgeglichen... |
| {0} of {1} | {0} von {1} |
| Checking remaining files... | Übrige Dateien werden geprüft... |
| Waiting for drive {0} to respond... | Warten auf eine Antwort von Laufwerk {0}... |
| Waiting for {0} to respond... | Warten auf eine Antwort von {0}... |
| Any file still to be checked against it will be left alone. | Jede Datei, die noch damit abgeglichen werden muss, bleibt unangetastet. |
| Scan complete ({0}) | Scan abgeschlossen ({0}) |
| Found {0} {1} you can safely delete. | {0} {1} gefunden, die du bedenkenlos löschen kannst. |
| Preparing destination folder... | Zielordner wird vorbereitet... |
| Moving unneeded files... | Nicht benötigte Dateien werden verschoben... |
| Deleting unneeded files... | Nicht benötigte Dateien werden gelöscht... |
| Move cancelled. {0} of {1} {2} processed. | Verschieben abgebrochen. {0} von {1} {2} verarbeitet. |
| Delete cancelled. {0} of {1} {2} processed. | Löschen abgebrochen. {0} von {1} {2} verarbeitet. |
| {0}. Details are in {1}. | {0}. Details in {1}. |
| {0}. The crash log could not be written. | {0}. Das Absturzprotokoll konnte nicht geschrieben werden. |
| {0}. Details are in {1}. | {0}. Details in {1}. |
| {0}. The crash log could not be written. | {0}. Das Absturzprotokoll konnte nicht geschrieben werden. |
| Access denied. Windows refused the scan. | Zugriff verweigert. Windows hat den Scan abgelehnt. |
| Scan stopped. | Scan gestoppt. |
| Scan cancelled. | Scan abgebrochen. |
| Ready | Bereit |
| Scan failed ({0}). Details in {1}. | Scan fehlgeschlagen ({0}). Details in {1}. |
| Scan failed ({0}). The crash log could not be written. | Scan fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden. |

## Main screen text

| English | Deutsch |
| --- | --- |
| Any unneeded files below are safe to delete. | Alle nicht benötigten Dateien unten kannst du bedenkenlos löschen. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Sie liegen in {InstallerFolder}. InstallerClean fragt Windows nach jedem installierten Programm: Eine Datei wird aufgeführt, wenn kein Programm sie beansprucht ({0}) oder wenn ein neuerer Patch sie ersetzt hat und kein Programm auf sie zurückgehen könnte ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Verschiebe sie in einen Sicherungsordner deiner Wahl und lösche diesen Ordner, sobald du dich überzeugt hast, dass deine Programme sich weiterhin wie gewohnt aktualisieren und deinstallieren lassen. Wenn du sie zurück nach {InstallerFolder} legst, ist alles wieder da. Oder lösche sie jetzt endgültig. |
| Nothing scanned yet. | Noch nichts gescannt. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Klicke auf „Neu scannen“, um {InstallerFolder} nach Installer-Dateien zu durchsuchen, die kein Programm mehr braucht. |
| Nothing to show until the next scan. | Nichts anzuzeigen bis zum nächsten Scan. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | Der Scan, der nach „Verschieben“ oder „Löschen“ läuft, wurde nicht abgeschlossen, deshalb kann keine Liste angezeigt werden. Klicke auf „Neu scannen“, um {InstallerFolder} erneut zu scannen. |
| What stopped it is recorded in {0}. | Was ihn gestoppt hat, ist in {0} festgehalten. |
| These files can't be cleaned up right now. | Diese Dateien können gerade nicht aufgeräumt werden. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Etwas nutzt gerade Windows Installer, etwa ein Windows-Update oder ein Programm, das im Hintergrund installiert wird. Verschieben und Löschen pausieren, solange das läuft, damit InstallerClean {InstallerFolder} nicht anfasst, während sich der Ordner ändert. Danach einmal neu scannen, und sie sind wieder da. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows hat InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt ist, deshalb pausieren Verschieben und Löschen, und InstallerClean fasst {InstallerFolder} nicht an. Starte Windows neu und wähle Neu scannen. Verweigert Windows es dann immer noch, ist das kein Rechner, den InstallerClean aufräumen kann. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean konnte nicht prüfen, ob Windows Installer beschäftigt ist, deshalb pausieren Verschieben und Löschen, und InstallerClean fasst {InstallerFolder} nicht an. Starte Windows neu und wähle Neu scannen. Kann es dann immer noch nicht prüfen, ist das kein Rechner, den InstallerClean aufräumen kann. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean konnte nicht prüfen, ob Windows Installer beschäftigt ist, weil Windows einen Fehler gemeldet hat: {0} (Fehler {1}). Verschieben und Löschen pausieren, und InstallerClean fasst {InstallerFolder} nicht an. Starte Windows neu und wähle Neu scannen. Kann es dann immer noch nicht prüfen, ist das kein Rechner, den InstallerClean aufräumen kann. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Auf diesem Rechner ist eine frühere Windows-Installer-Transaktion angehalten. Setze diese Installation fort oder mache sie rückgängig (oder starte Windows neu), bevor du {InstallerFolder} aufräumst. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows hat für den nächsten Neustart eine Dateiumbenennung eingeplant, die {InstallerFolder} betrifft. Starte Windows neu, bevor du aufräumst. |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | Für den nächsten Neustart ist ein Dateivorgang vorgemerkt, und InstallerClean kann nicht erkennen, welche Dateien darin genannt werden. Damit lässt sich nicht ausschließen, dass sie in {InstallerFolder} liegen. Starte Windows neu, bevor du aufräumst. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean konnte eine der Windows-Einstellungen nicht lesen, die es prüft, bevor es {InstallerFolder} anfasst, und kann deshalb nicht erkennen, ob gerade ein Installationsvorgang läuft oder auf einen Neustart wartet. Starte Windows neu und wähle Neu scannen. Lässt sich die Einstellung dann immer noch nicht lesen, ist das kein Rechner, den InstallerClean aufräumen kann. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer hat etwas in Arbeit, deshalb pausieren Verschieben und Löschen. InstallerClean fasst {InstallerFolder} nicht an, während sich der Ordner ändert. Danach einmal neu scannen, und sie sind wieder da. |
| Select a file to view details. | Wähle eine Datei, um Details anzuzeigen. |
| Select a product to view details. | Wähle ein Produkt, um Details anzuzeigen. |
| No metadata available. | Keine Metadaten verfügbar. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Diese Installationsdatei fehlt. Das macht im Moment keine Probleme und wird auch keine machen, bis du eines Tages das zugehörige Programm aktualisieren oder deinstallieren willst. Dieser Schritt kann dann fehlschlagen, weil Windows nach dieser Datei sucht und sie nicht findet.<br><br>Um sie zurückzuholen, brauchst du das Installationsprogramm genau der Version, die du bereits hast. Hol es beim Hersteller des Programms und führe es über deiner vorhandenen Installation aus. Eine neuere Version reicht nicht: Sie müsste zuerst die vorhandene entfernen, und genau dieser Schritt braucht diese Datei. Vorher zu deinstallieren funktioniert aus demselben Grund ebenfalls nicht. Das sollte die Datei wiederherstellen und deine Einstellungen unangetastet lassen, garantiert wird es von Microsoft aber nicht. |
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
| Nothing to clean up in {InstallerFolder} | Nichts aufzuräumen in {InstallerFolder} |
| Scanned {0} {1} in {2} | {0} {1} in {2} gescannt |
| Nothing offered on this PC | Auf diesem PC nichts angeboten |
| One file ({2}) was held back because Windows shows it was changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check it again. | Eine Datei ({2}) wurde zurückgehalten, weil Windows anzeigt, dass sie geändert wurde, nachdem InstallerClean mit dem Scannen begonnen hatte. Falls gerade ein Programm installiert oder aktualisiert wurde, scanne erneut, sobald das fertig ist, dann prüft InstallerClean sie noch einmal. |
| {0} {1} ({2}) were held back because Windows shows they were changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check them again. | {0} {1} ({2}) wurden zurückgehalten, weil Windows anzeigt, dass sie geändert wurden, nachdem InstallerClean mit dem Scannen begonnen hatte. Falls gerade ein Programm installiert oder aktualisiert wurde, scanne erneut, sobald das fertig ist, dann prüft InstallerClean sie noch einmal. |
| Delete that folder when you're satisfied all is well. | Lösche diesen Ordner, sobald du dich überzeugt hast, dass alles in Ordnung ist. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | Lösche diesen Ordner, sobald du dich überzeugt hast, dass alles in Ordnung ist. Erst dann wird der Speicherplatz tatsächlich frei. |
| {0} freed | {0} freigegeben |
| {0} moved | {0} verschoben |
| Nothing was moved | Nichts verschoben |
| Nothing was deleted | Nichts gelöscht |
| {0} file could not be moved. | {0} Datei konnte nicht verschoben werden. |
| {0} files could not be moved. | {0} Dateien konnten nicht verschoben werden. |
| {0} file could not be deleted. | {0} Datei konnte nicht gelöscht werden. |
| {0} files could not be deleted. | {0} Dateien konnten nicht gelöscht werden. |
| {0} {1} moved to: {2} | {0} {1} verschoben nach: {2} |
| {0} {1} moved to: {2} | {0} {1} verschoben nach: {2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | {0} Datei zurückgehalten. Der Scan hielt sie für nicht benötigt. Die abschließende Prüfung konnte das nicht bestätigen. |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | {0} Dateien zurückgehalten. Der Scan hielt sie für nicht benötigt. Die abschließende Prüfung konnte das nicht bestätigen. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} an Ort und Stelle belassen, weil Windows einen Eintrag zu dem darin genannten Programm hat. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} an Ort und Stelle belassen, weil InstallerClean darin kein Programm benannt fand. |
| Moved {0} of {1} {2} to {3} before you cancelled. | {0} von {1} {2} nach {3} verschoben, bevor du abgebrochen hast. |
| Permanently deleted {0} of {1} {2} before you cancelled. | {0} von {1} {2} endgültig gelöscht, bevor du abgebrochen hast. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | Das lässt sich leicht rückgängig machen. Verschiebe sie zurück nach {InstallerFolder}, und alles ist wieder wie vorher. |
| {0} {1} permanently deleted | {0} {1} endgültig gelöscht |
| {0} {1} permanently deleted | {0} {1} endgültig gelöscht |
| Any tip is much appreciated | Über jedes Trinkgeld freue ich mich sehr |
| Send _anonymous report | _Anonymen Bericht senden |
| The report adds your result to [this chart] on GitHub, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Der Bericht fügt dein Ergebnis zu [diesem Diagramm] auf GitHub hinzu, das zeigt, wie viel Speicherplatz Menschen mit InstallerClean freigeben. Außerdem enthält er diese technischen Angaben, die mir helfen, Fehler zu finden: |
| How much space was freed, and how many files were moved or deleted | Wie viel Speicherplatz freigegeben und wie viele Dateien verschoben oder gelöscht wurden |
| Whether a Move went to the same drive, another drive, a removable drive or a network share | Ob beim Verschieben das Ziel auf demselben Laufwerk, einem anderen Laufwerk, einem Wechsellaufwerk oder einer Netzwerkfreigabe lag |
| How long the scan and the Move or Delete took | Wie lange der Scan und das Verschieben oder Löschen gedauert haben |
| How many installer files there are of each kind, and how much space they take | Wie viele Installer-Dateien es von jeder Art gibt und wie viel Speicherplatz sie belegen |
| How many files were left alone, and why | Wie viele Dateien unangetastet blieben und warum |
| How often InstallerClean waited for a slow drive or network share, and how many it stopped waiting for | Wie oft InstallerClean auf ein langsames Laufwerk oder eine Netzwerkfreigabe gewartet hat und bei wie vielen es aufgehört hat zu warten |
| How many programs and patches Windows has a record of | Zu wie vielen Programmen und Patches Windows einen Eintrag hat |
| Whether Windows still makes old-style short file names | Ob Windows noch kurze Dateinamen im alten Stil anlegt |
| Whether Windows is 10 or 11 and x64 or Arm, and which language and country or region it's set to | Ob Windows 10 oder 11 und x64 oder Arm ist und auf welche Sprache und welches Land oder welche Region es eingestellt ist |
| InstallerClean's version, how you got it (setup, portable, slim, Scoop, the command-line tool on its own, built from source, or not known), whether the report was sent from InstallerClean or the command-line tool, and the language it was showing | Die Version von InstallerClean, wie du es bekommen hast (Setup, Portable, Slim, Scoop, das Befehlszeilenprogramm allein, selbst aus dem Quellcode gebaut oder unbekannt), ob der Bericht von InstallerClean oder vom Befehlszeilenprogramm gesendet wurde, und die Sprache, in der es angezeigt wurde |
| Counts of anything that went wrong | Wie oft etwas schiefgegangen ist |
| Each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Jede dieser Angaben ist eine Zahl oder eine Bezeichnung. Nichts im Bericht nennt dich, deinen PC, deine Dateien oder deine Programme, und ich sehe deine IP-Adresse nie. |
| See exactly what's sent | Genau sehen, was gesendet wird |

## Summaries and counts

| English | Deutsch |
| --- | --- |
| {0} file left alone | {0} Datei unangetastet |
| {0} files left alone | {0} Dateien unangetastet |
| {0} unneeded file to clean up | {0} nicht benötigte Datei zum Aufräumen |
| {0} unneeded files to clean up | {0} nicht benötigte Dateien zum Aufräumen |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | Windows hat einen Eintrag für {0} Datei, die nicht in {InstallerFolder} liegt: {1}. Im Alltag macht das keine Probleme, aber ein Update oder eine Deinstallation dieses Programms kann fehlschlagen. Unter Details steht, was zu tun ist. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | Windows hat Einträge für {0} Dateien, die nicht in {InstallerFolder} liegen: {1}. Im Alltag macht das keine Probleme, aber ein Update oder eine Deinstallation dieser Programme kann fehlschlagen. Unter Details steht, was zu tun ist. |
| {0} other program | {0} weiteres Programm |
| {0} other programs | {0} weitere Programme |
| {0} file with no program named in the records | {0} Datei, zu der die Einträge kein Programm nennen |
| {0} files with no program named in the records | {0} Dateien, zu denen die Einträge kein Programm nennen |
| InstallerClean carried on without drive {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean hat ohne Laufwerk {0} weitergemacht und jede Datei unangetastet gelassen, die noch damit abgeglichen werden musste. Sobald es wieder normal reagiert, klicke auf „Neu scannen“. |
| InstallerClean carried on without {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean hat ohne {0} weitergemacht und jede Datei unangetastet gelassen, die noch damit abgeglichen werden musste. Sobald der Zugriff darauf wieder normal funktioniert, klicke auf „Neu scannen“. |
| InstallerClean carried on without more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, Re-scan. | InstallerClean hat ohne mehrere Laufwerke oder Freigaben ({0}) weitergemacht und jede Datei unangetastet gelassen, die noch mit einem davon abgeglichen werden musste. Sobald sie wieder normal reagieren, klicke auf „Neu scannen“. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has finished. | Als der Scan begann, nutzte etwas Windows Installer, etwa ein Windows-Update oder ein Programm, das installiert wurde. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Klicke auf „Neu scannen“, sobald das fertig ist. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then Re-scan. | Als der Scan begann, war auf diesem Rechner eine Windows-Installer-Transaktion angehalten. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Setze diese Installation fort oder mache sie rückgängig oder starte Windows neu, und klicke dann auf „Neu scannen“. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Als der Scan begann, hatte Windows für den nächsten Neustart eine Dateiumbenennung eingeplant, die {InstallerFolder} betrifft. Deshalb hat InstallerClean die Dateien dort zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und klicke dann auf „Neu scannen“. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Als der Scan begann, war für den nächsten Neustart ein Dateivorgang vorgemerkt, und InstallerClean konnte nicht erkennen, welche Dateien darin genannt werden. Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und klicke dann auf „Neu scannen“. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Als der Scan begann, hat Windows InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt war. Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und klicke auf „Neu scannen“. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Als der Scan begann, konnte InstallerClean eine der Windows-Einstellungen nicht lesen, die es prüft, und deshalb nicht erkennen, ob ein Installationsvorgang lief oder auf einen Neustart wartete. Daher hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und klicke auf „Neu scannen“. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Als der Scan begann, konnte InstallerClean nicht prüfen, ob Windows Installer beschäftigt war. Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und klicke auf „Neu scannen“. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Als der Scan begann, konnte InstallerClean nicht prüfen, ob Windows Installer beschäftigt war, weil Windows einen Fehler gemeldet hat: {0} (Fehler {1}). Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und klicke auf „Neu scannen“. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has cleared. | Als der Scan begann, hatte Windows Installer etwas in Arbeit oder ausstehend. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Klicke auf „Neu scannen“, sobald das erledigt ist. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan. | Die Uhr dieses PCs ist während des Scans zurückgesprungen oder nachgegangen. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Klicke auf „Neu scannen“. |
| {0} of {1} {2} | {0} von {1} {2} |
| {0} unneeded {1} ({2}) | {0} nicht benötigte {1} ({2}) |
| {0} file left alone ({1}) | {0} Datei unangetastet ({1}) |
| {0} files left alone ({1}) | {0} Dateien unangetastet ({1}) |
| {0} missing | {0} fehlt |
| {0} missing | {0} fehlen |

## Confirmation dialogs

| English | Deutsch |
| --- | --- |
| Move {0} {1} ({2})? | {0} {1} verschieben ({2})? |
| This file will be moved to: | Diese Datei wird verschoben nach: |
| These files will be moved to: | Diese Dateien werden verschoben nach: |
| Delete {0} {1} ({2})? | {0} {1} löschen ({2})? |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Diese Datei wird endgültig gelöscht. Das ist unbedenklich, aber wenn du eine Sicherung möchtest, nimm stattdessen Verschieben. |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Diese Dateien werden endgültig gelöscht. Das ist unbedenklich, aber wenn du eine Sicherung möchtest, nimm stattdessen Verschieben. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Dieser Ordner liegt auf demselben Laufwerk, der Platz kommt also erst zurück, wenn du ihn löschst. Wähle stattdessen einen Ordner auf einem anderen Laufwerk, wenn du den Platz sofort haben willst. |

## Error messages

| English | Deutsch |
| --- | --- |
| This is also recorded in {0}. | Das wird auch in {0} festgehalten. |
| Access denied | Zugriff verweigert |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows hat InstallerClean den Zugriff verweigert, deshalb hat es abgebrochen. Es wurde nichts entfernt.<br><br>InstallerClean lief bereits als Administrator, es noch einmal so zu starten hilft also nicht. Windows sagt nicht genauer, was den Zugriff verweigert hat, es gibt also nichts Bestimmtes, das du versuchen könntest. |
| InstallerClean stopped | InstallerClean gestoppt |
| Scan failed | Scan fehlgeschlagen |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Die Einträge von Windows Installer kamen völlig leer zurück: Nicht ein einziges installiertes Programm und kein einziges Update beansprucht eine zwischengespeicherte Installationsdatei. Auf einem funktionierenden System kommt das nicht vor (selbst eine frische Windows-Installation hat welche), also sind die Einträge entweder beschädigt oder sie konnten nicht gelesen werden, und ein Scan, der diese Antwort glaubt, würde jede Datei in {InstallerFolder} fälschlich als verwaist einstufen. InstallerClean hat stattdessen abgebrochen. Es wurde nichts entfernt. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer hat es InstallerClean verweigert, die installierte Software aufzulisten. InstallerClean lief bereits als Administrator, es erneut als Administrator auszuführen ändert also nichts. Ohne diese Liste lässt sich nicht sicher sagen, welche zwischengespeicherten Dateien noch gebraucht werden, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer konnte InstallerClean keine lesbare Liste der installierten Programme geben: Es hat {1} {2} gelesen, dann kam ein Eintrag unlesbar zurück (Fehlercode {0}). Statt mit einer nur teilweise gelesenen Liste zu arbeiten, hat InstallerClean abgebrochen. Es wurde nichts entfernt. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer hat das Ende der Liste der installierten Programme nie signalisiert: InstallerClean hat {2} {3} gelesen und dann nach {0} Einträgen aufgegeben (letzter Fehlercode {1}). Einer Liste ohne Ende ist nicht zu trauen, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer hat das Ende der Patch-Liste eines Programms nie signalisiert: InstallerClean hat {2} {3} gelesen und dann nach {0} Einträgen aufgegeben (letzter Fehlercode {1}). Einer Liste ohne Ende ist nicht zu trauen, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean konnte die Windows-Installer-Einträge nicht mit dem Inhalt von {InstallerFolder} abgleichen. Fast nichts, worauf die Einträge zeigen, ist tatsächlich da, und fast nichts, was da ist, wird von einem Eintrag benannt, deshalb ließ sich für keine Datei zeigen, dass sie nicht benötigt wird. Es wurde nichts angeboten und nichts entfernt. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean konnte die Windows-Installer-Einträge nicht mit dem Inhalt von {InstallerFolder} abgleichen. Der Ordner enthält Dateien, aber kein einziger Eintrag zeigt auf irgendetwas darin, deshalb ließ sich für keine Datei zeigen, dass sie nicht benötigt wird. Es wurde nichts angeboten und nichts entfernt. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. Nothing has been offered and nothing has been removed. | InstallerClean kann {InstallerFolder} auf diesem PC nicht scannen. Windows meldet diesen Ordner als {0}, und InstallerClean scannt nur einen Installer-Ordner, den Windows unter seinem eigenen Pfad meldet. Es wurde nichts angeboten und nichts entfernt. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. Nothing has been offered and nothing has been removed. | Windows meldet, dass {InstallerFolder} auf diesem PC nicht existiert, deshalb hat InstallerClean abgebrochen. Es wurde nichts angeboten und nichts entfernt. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. Nothing has been offered and nothing has been removed. | Windows hat einen Fehler gemeldet, als InstallerClean versuchte, die Dateien in {InstallerFolder} aufzulisten, deshalb konnte es keine vollständige Liste erhalten und hat abgebrochen. Es wurde nichts angeboten und nichts entfernt. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean konnte nicht genug von den Einträgen von Windows Installer lesen, um sicher zu sein, was noch gebraucht wird: Die Liste der installierten Programme kam unvollständig zurück, und dieselben Einträge direkt aus der Registrierung zu lesen führte ebenfalls zu Fehlern. Eine Datei könnte allein deshalb verwaist wirken, weil der Eintrag, der sie nennt, zu den unlesbaren gehörte, deshalb hat InstallerClean abgebrochen. Es wurde nichts entfernt. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean konnte Windows nicht dazu bringen, den echten Pfad von {InstallerFolder} aufzulösen, deshalb ließ sich für keine Datei zeigen, dass sie darin liegt, und keine wurde zum Aufräumen angeboten. Dieser Scan hat nichts gefunden, weil diese Prüfung fehlschlug, nicht weil der Ordner sauber ist. Es wurde nichts entfernt. |
| Nothing was deleted | Nichts gelöscht |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows hat InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt ist, und damit ließ sich nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts gelöscht. |
| Nothing was moved | Nichts verschoben |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows hat InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt ist, und damit ließ sich nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts verschoben. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean konnte die Sperre nicht übernehmen, mit der Windows Installer verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts gelöscht. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean konnte die Sperre nicht übernehmen, mit der Windows Installer verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts verschoben. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt. |
| Invalid destination | Ungültiges Ziel |
| Move stopped | Verschieben gestoppt |
| Couldn't use that backup folder | Sicherungsordner nicht verwendbar |
| Move failed | Verschieben fehlgeschlagen |
| Delete failed | Löschen fehlgeschlagen |
| Setting not saved | Einstellung nicht gespeichert |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Die Änderung konnte nicht gespeichert werden. Beim nächsten Start verwendet InstallerClean wieder die vorherige Einstellung. |
| The destination cannot be inside the Windows Installer folder. | Das Ziel darf nicht im Windows-Installer-Ordner liegen. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Das Ziel {0} liegt unterhalb eines Windows-Systemordners. Wähle einen Pfad außerhalb von %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% und %ProgramData%. |
| Not enough space | Nicht genügend Speicherplatz |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | Unter {0} ist zu wenig Platz<br><br>Benötigt: {1}<br>Verfügbar: {2} |
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
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean konnte den Sicherungsordner nicht mehr bestätigen und hat deshalb angehalten. Prüfe {0}, dann Neu scannen und noch einmal versuchen. |
| Cannot write to {0}. | Schreiben in {0} nicht möglich. |
| You can save files in {0} but not open them again, so InstallerClean won't move files there. | Du kannst Dateien in {0} speichern, aber nicht wieder öffnen, deshalb verschiebt InstallerClean keine Dateien dorthin. |
| A file called '{0}' is already in the backup folder. | Eine Datei namens '{0}' liegt bereits im Sicherungsordner. |

## Update check

| English | Deutsch |
| --- | --- |
| Check for updates | Nach Updates suchen |
| Checking... | Wird geprüft... |
| Up to date. | Auf dem neuesten Stand. |
| InstallerClean {0} is available. | InstallerClean {0} ist verfügbar. |
| It's strongly recommended to use the latest version. | Es wird dringend empfohlen, die neueste Version zu verwenden. |
| Download it from GitHub | Von GitHub herunterladen |
| Download .NET Framework 4.8 from Microsoft | .NET Framework 4.8 von Microsoft herunterladen |
| Update now | Jetzt aktualisieren |
| This copy was installed by Scoop. To update it, close InstallerClean and run: | Diese Kopie wurde mit Scoop installiert. Um sie zu aktualisieren, schließe InstallerClean und führe Folgendes aus: |
| This copy was installed by Scoop for all users. To update it, close InstallerClean, open a terminal as administrator and run: | Diese Kopie wurde mit Scoop für alle Benutzer installiert. Um sie zu aktualisieren, schließe InstallerClean, öffne ein Terminal als Administrator und führe Folgendes aus: |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean to update. | Diese Version benötigt .NET Framework 4.8. Installiere es von Microsoft, starte den PC neu und öffne dann InstallerClean, um zu aktualisieren. |
| InstallerClean {0} can't be installed automatically. | InstallerClean {0} kann nicht automatisch installiert werden. |
| InstallerClean can't tell whether its setup installed it, so it can't update itself. | InstallerClean kann nicht feststellen, ob es mit seinem Setup installiert wurde, und kann sich deshalb nicht selbst aktualisieren. |
| Copied | Kopiert |
| Couldn't copy it. Select the command and copy it yourself. | Kopieren nicht möglich. Markiere den Befehl und kopiere ihn selbst. |
| _Continue without updating | _Ohne Aktualisierung fortfahren |
| Downloading InstallerClean {0}... | InstallerClean {0} wird heruntergeladen... |
| Checking the download... | Download wird geprüft... |
| The update couldn't be downloaded. | Das Update konnte nicht heruntergeladen werden. |
| The update couldn't be installed. | Das Update konnte nicht installiert werden. |
| Couldn't reach GitHub. Check your internet connection and try again. | GitHub war nicht erreichbar. Prüfe deine Internetverbindung und versuch es erneut. |
| GitHub returned an error response. Try again in a few minutes. | GitHub hat eine Fehlerantwort zurückgegeben. Versuch es in ein paar Minuten erneut. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Die Antwort von GitHub enthielt keine erkennbare Version. Versuch es später erneut oder öffne die Releases-Seite direkt. |
| The check timed out. Your connection to GitHub may be slow; try again. | Bei der Prüfung wurde das Zeitlimit überschritten. Deine Verbindung zu GitHub ist vielleicht langsam; versuch es erneut. |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | Die Prüfung ist aus unbekanntem Grund fehlgeschlagen. Details stehen in {0}, falls du es melden möchtest. |
| The check failed for an unknown reason. The crash log could not be written. | Die Prüfung ist aus unbekanntem Grund fehlgeschlagen. Das Absturzprotokoll konnte nicht geschrieben werden. |

## Opening links in your browser

| English | Deutsch |
| --- | --- |
| Couldn't open your browser | Browser konnte nicht geöffnet werden |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | Der Link ist in der Zwischenablage, du kannst ihn also selbst einfügen:<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean konnte den Link auch nicht in die Zwischenablage kopieren, hier ist er:<br><br>{0} |

## Startup and crashes

| English | Deutsch |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | Es läuft bereits. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Ein unerwarteter Fehler ist aufgetreten und InstallerClean muss geschlossen werden.<br><br>{0}<br><br>Details gespeichert unter:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Ein unerwarteter Fehler ist aufgetreten und InstallerClean muss geschlossen werden.<br><br>{0}<br><br>Das Absturzprotokoll konnte nicht geschrieben werden. |
| Startup error | Startfehler |
| Failed to start ({0}). Details written to:<br>{1} | Start fehlgeschlagen ({0}). Details gespeichert unter:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Start fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden. |
| Administrator rights needed | Administratorrechte erforderlich |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed.<br><br>Run InstallerClean again as administrator. | InstallerClean braucht Administratorrechte, um alle auf diesem PC installierten Programme zu sehen, läuft aber nicht als Administrator und scannt deshalb nicht. Es wurde nichts verändert.<br><br>Führe InstallerClean erneut als Administrator aus. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log erfasst unbehandelte Ausnahmen von InstallerClean.<br># Mit erhöhten Rechten können die Ausnahmemeldungen des Frameworks<br># Dateipfade aus der laufenden Sitzung enthalten (auch Profile<br># anderer Benutzer, die Windows-Installer-Abfragen aufzählen).<br># Meldungen über Netzwerkfehler bei der Updateprüfung oder beim<br># Senden des Ergebnisprotokolls können die Ziel-URL und die<br># aufgelöste IP- oder Proxyadresse enthalten. Einträge über<br># unlesbare Windows-Installer-Einträge können eine Windows-Konto-SID<br># (S-1-5-21-...) und die Produktcodes installierter Software<br># enthalten.<br># Entferne alle drei Arten von Angaben, bevor du diese Datei an<br># einen öffentlichen Fehlerbericht anhängst.<br> |

## Tooltips (hover text)

| English | Deutsch |
| --- | --- |
| Any tip is much appreciated | Über jedes Trinkgeld freue ich mich sehr |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Abbruch angefordert. InstallerClean wartet, bis der aktuelle Schritt einen Haltepunkt erreicht. Bei starker Datenträgeraktivität oder einem MSI-Datenbankaufruf kann das ein paar Sekunden dauern. |
| Close | Schließen |
| I really should charge for InstallerClean - because it's been a huge amount of work - but I want to keep it free so it helps as many people as possible. Your donation makes it worthwhile and is very much appreciated. | Eigentlich sollte ich für InstallerClean Geld verlangen, denn es hat enorm viel Arbeit gekostet, aber ich möchte es kostenlos halten, damit es so vielen Menschen wie möglich hilft. Deine Spende macht die Mühe lohnenswert und wird sehr geschätzt. |
| A star helps other people find it | Ein Stern hilft anderen, InstallerClean zu finden |
| Minimise | Minimieren |
| Move the unneeded files to the backup folder | Verschiebt die nicht benötigten Dateien in den Sicherungsordner |
| Move the unneeded files to a backup folder. You'll choose it next. | Verschiebt die nicht benötigten Dateien in einen Sicherungsordner. Den wählst du gleich aus. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | Verschiebt die nicht benötigten Dateien in den Sicherungsordner. Er liegt auf demselben Laufwerk, der Speicherplatz wird also erst frei, wenn du diesen Ordner löschst. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | Löscht die nicht benötigten Dateien endgültig. Nimm stattdessen Verschieben, wenn du dich erst überzeugen möchtest, dass alles in Ordnung ist. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Antragstellername aus dem eingebetteten Authenticode-Zertifikat. Die Zertifikatskette wurde nicht geprüft. |
| Change language. The program will restart. | Sprache ändern. Das Programm wird neu gestartet. |

## Screen reader labels

| English | Deutsch |
| --- | --- |
| Leave a tip | Trinkgeld geben |
| Leave a tip | Trinkgeld geben |
| Leave a tip | Trinkgeld geben |
| Cancel operation | Vorgang abbrechen |
| Cancel scan | Scan abbrechen |
| Cancel startup scan | Start-Scan abbrechen |
| Carry on without it, drive {0} | Ohne Warten fortfahren, Laufwerk {0} |
| Carry on without it, {0} | Ohne Warten fortfahren, {0} |
| Close | Schließen |
| Close window | Fenster schließen |
| Close result and return to main window | Ergebnis schließen und zum Hauptfenster zurückkehren |
| Leave a star on github | Einen Stern auf github hinterlassen |
| Minimise | Minimieren |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Endgültig löschen entfernt die nicht benötigten Dateien. Abbrechen schließt das Fenster, ohne zu löschen. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Verschieben legt die nicht benötigten Dateien in den gewählten Zielordner. Abbrechen lässt sie, wo sie sind. |
| Say thanks | Danke sagen |
| Check for updates | Nach Updates suchen |
| Checks github's releases page for a newer version. | Sucht auf der Release-Seite von github nach einer neueren Version. |
| Opens the readme on github in your browser. | Öffnet das readme auf github in deinem Browser. |
| Opens the issue tracker on github.com in your browser. | Öffnet den Issue-Tracker auf github.com in deinem Browser. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Wenn aktiviert, sucht InstallerClean beim Start auf github nach einer neueren Version. |
| Download it from github | Von github herunterladen |
| Download .NET Framework 4.8 from Microsoft | .NET Framework 4.8 von Microsoft herunterladen |
| Copy the command | Befehl kopieren |
| Scoop's update command | Aktualisierungsbefehl für Scoop |
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
| Waiting for a drive or share | Warten auf ein Laufwerk oder eine Freigabe |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Dateien, die nicht verarbeitet werden konnten |
| Explains this folder, and how to recover a file, in the README | Erklärt diesen Ordner, und wie sich eine Datei wiederherstellen lässt, im README |
| {0} Press Alt+{1} to carry on without it. {2} | {0} Drücke Alt+{1}, um ohne Warten fortzufahren. {2} |
| What the report holds | Was der Bericht enthält |
| What the report holds | Was der Bericht enthält |
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
| drive {0} | Laufwerk {0} |
| .  | .  |
| {0} TB | {0} TB |
| {0} GB | {0} GB |
| {0} MB | {0} MB |
| {0} KB | {0} KB |
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
| Error: unexpected failure ({0}). Details written to {1}. | Fehler: unerwarteter Absturz ({0}). Details in {1} geschrieben. |
| Error: unexpected failure ({0}). The crash log could not be written. | Fehler: unerwarteter Absturz ({0}). Das Absturzprotokoll konnte nicht geschrieben werden. |
| Scanning {InstallerFolder}... | {InstallerFolder} wird gescannt... |
| Found {0} unneeded {1} to clean up ({2}). | {0} nicht benötigte {1} zum Aufräumen gefunden ({2}). |
| Found no unneeded files. | Keine nicht benötigten Dateien gefunden. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean konnte nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb die eine Datei ({2}) zurückgehalten, statt sie anzubieten. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean konnte nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb {0} {1} ({2}) zurückgehalten, statt sie anzubieten. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean konnte nicht feststellen, dass eine Datei nicht benötigt wird, und hat sie deshalb ({2}) zurückgehalten, statt sie anzubieten. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean konnte nicht feststellen, dass {0} {1} nicht benötigt werden, und hat sie deshalb ({2}) zurückgehalten, statt sie anzubieten. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean konnte nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb die eine Datei ({2}) zurückgehalten, statt sie anzubieten. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean konnte nicht sicher feststellen, welche zwischengespeicherten Dateien zu den hier installierten Programmen gehören, und hat deshalb {0} {1} ({2}) zurückgehalten, statt sie anzubieten. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean konnte nicht feststellen, dass eine Datei nicht benötigt wird, und hat sie deshalb ({2}) zurückgehalten, statt sie anzubieten. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean konnte nicht feststellen, dass {0} {1} nicht benötigt werden, und hat sie deshalb ({2}) zurückgehalten, statt sie anzubieten. |
| Why it couldn't be certain: | Warum keine Gewissheit möglich war: |
|   A file path in Windows Installer's own records wouldn't resolve, so nothing could be matched to it. |   Ein Dateipfad in den eigenen Aufzeichnungen von Windows Installer ließ sich nicht auflösen, sodass ihm nichts zugeordnet werden konnte. |
|   A file Windows has a record of couldn't be identified, so it couldn't be matched to what's in the folder. |   Eine Datei, über die Windows eine Aufzeichnung hat, ließ sich nicht identifizieren und konnte deshalb dem Inhalt des Ordners nicht zugeordnet werden. |
|   A program may be installed more than once on this PC, and the records can't say which copy a file belongs to. |   Ein Programm ist auf diesem PC möglicherweise mehrfach installiert, und die Aufzeichnungen geben nicht her, zu welcher Kopie eine Datei gehört. |
|   Part of Windows Installer's records wouldn't read, so a file could belong to a program recorded there. |   Ein Teil der Aufzeichnungen von Windows Installer ließ sich nicht lesen, sodass eine Datei zu einem dort aufgezeichneten Programm gehören könnte. |
|   A file in the folder couldn't be identified, so it couldn't be matched against the records. |   Eine Datei im Ordner ließ sich nicht identifizieren und konnte deshalb nicht den Aufzeichnungen zugeordnet werden. |
|   Either a file wouldn't say which program it belongs to, or Windows wouldn't give a clear answer about that program. |   Entweder gab eine Datei nicht an, zu welchem Programm sie gehört, oder Windows gab zu diesem Programm keine eindeutige Auskunft. |
|   A check on which programs the files belong to gave answers that didn't line up with the files it was handed. |   Eine Prüfung, zu welchen Programmen die Dateien gehören, lieferte Antworten, die nicht zu den übergebenen Dateien passten. |
|   Either a patch file wouldn't say which patch it is, or it wouldn't say which programs it's for, or Windows wouldn't say in full which programs have that patch. |   Entweder gab eine Patch-Datei nicht an, um welchen Patch es sich handelt, oder sie gab nicht an, für welche Programme sie bestimmt ist, oder Windows gab keine vollständige Auskunft darüber, welche Programme diesen Patch haben. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows hat einen Eintrag für {0} Datei, die nicht in {InstallerFolder} liegt: {1}. Im Alltag macht das keine Probleme, aber ein Update oder eine Deinstallation dieses Programms kann fehlschlagen. Um die Datei zurückzuholen, brauchst du das Installationsprogramm genau der Version, die du bereits hast. Hol es beim Hersteller des Programms und führe es über deiner vorhandenen Installation aus. Eine neuere Version reicht nicht: Sie müsste zuerst die vorhandene entfernen, und genau dieser Schritt braucht diese Datei. Vorher zu deinstallieren funktioniert aus demselben Grund ebenfalls nicht. Das sollte die Datei wiederherstellen und deine Einstellungen unangetastet lassen, garantiert wird es von Microsoft aber nicht. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows hat Einträge für {0} Dateien, die nicht in {InstallerFolder} liegen: {1}. Im Alltag macht das keine Probleme, aber ein Update oder eine Deinstallation dieser Programme kann fehlschlagen. Um eine Datei zurückzuholen, brauchst du das Installationsprogramm genau der Version, die du von diesem Programm bereits hast. Hol es beim Hersteller des Programms und führe es über deiner vorhandenen Installation aus. Eine neuere Version reicht nicht: Sie müsste zuerst die vorhandene entfernen, und genau dieser Schritt braucht die Datei. Vorher zu deinstallieren funktioniert aus demselben Grund ebenfalls nicht. Das sollte die Datei wiederherstellen und deine Einstellungen unangetastet lassen, garantiert wird es von Microsoft aber nicht. |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | InstallerClean konnte nicht sicher feststellen, dass die eine ersetzte Datei nicht mehr gebraucht wird, und hat sie deshalb zurückgehalten. |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | InstallerClean konnte nicht sicher feststellen, dass {0} ersetzte Dateien nicht mehr gebraucht werden, und hat sie deshalb zurückgehalten. |
| InstallerClean stopped waiting for drive {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean hat aufgehört, auf Laufwerk {0} zu warten, und jede Datei unangetastet gelassen, die noch damit abgeglichen werden musste. Sobald es wieder normal reagiert, führe den Befehl erneut aus. |
| InstallerClean stopped waiting for {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean hat aufgehört, auf {0} zu warten, und jede Datei unangetastet gelassen, die noch damit abgeglichen werden musste. Sobald der Zugriff darauf wieder normal funktioniert, führe den Befehl erneut aus. |
| InstallerClean stopped waiting for more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, run the command again. | InstallerClean hat aufgehört, auf mehrere Laufwerke oder Freigaben ({0}) zu warten, und jede Datei unangetastet gelassen, die noch mit einem davon abgeglichen werden musste. Sobald sie wieder normal reagieren, führe den Befehl erneut aus. |
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
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | Fehler: Für den nächsten Neustart ist ein Dateivorgang vorgemerkt, und InstallerClean kann nicht erkennen, welche Dateien darin genannt werden. Damit lässt sich {InstallerFolder} nicht ausschließen. Starte Windows neu, bevor du aufräumst. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Fehler: InstallerClean konnte einen der Registrierungswerte nicht lesen, die es prüft, bevor es {InstallerFolder} anfasst, und kann deshalb einen laufenden oder für den nächsten Neustart vorgemerkten Windows-Installer-Vorgang nicht ausschließen. /m und /d sind blockiert. Starte Windows neu und versuch es erneut. Schlägt das Lesen weiterhin fehl, ist das kein Rechner, den InstallerClean aufräumen kann. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Fehler: InstallerClean konnte nicht prüfen, ob Windows Installer beschäftigt ist, und kann deshalb eine laufende Installation nicht ausschließen. /m und /d sind blockiert. Starte Windows neu und versuch es erneut. Schlägt die Prüfung weiterhin fehl, ist das kein Rechner, den InstallerClean aufräumen kann. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Fehler: InstallerClean konnte nicht prüfen, ob Windows Installer beschäftigt ist, weil Windows einen Fehler gemeldet hat: {0} (Fehler {1}). Deshalb kann es eine laufende Installation nicht ausschließen. /m und /d sind blockiert. Starte Windows neu und versuch es erneut. Schlägt die Prüfung weiterhin fehl, ist das kein Rechner, den InstallerClean aufräumen kann. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Fehler: Windows Installer hat etwas in Arbeit, deshalb sind /m und /d blockiert. InstallerClean fasst {InstallerFolder} nicht an, während sich der Ordner ändert. Versuche es erneut, sobald es fertig ist. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has finished. | Als der Scan begann, nutzte etwas Windows Installer, etwa ein Windows-Update oder ein Programm, das installiert wurde. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Führe den Befehl erneut aus, sobald das fertig ist. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then run it again. | Als der Scan begann, war auf diesem Rechner eine Windows-Installer-Transaktion angehalten. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Setze diese Installation fort oder mache sie rückgängig oder starte Windows neu, und führe den Befehl dann erneut aus. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Als der Scan begann, hatte Windows für den nächsten Neustart eine Dateiumbenennung eingeplant, die {InstallerFolder} betrifft. Deshalb hat InstallerClean die Dateien dort zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und führe den Befehl dann erneut aus. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Als der Scan begann, war für den nächsten Neustart ein Dateivorgang vorgemerkt, und InstallerClean konnte nicht erkennen, welche Dateien darin genannt werden. Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und führe den Befehl dann erneut aus. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Als der Scan begann, hat Windows InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt war. Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und führe den Befehl erneut aus. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Als der Scan begann, konnte InstallerClean eine der Windows-Einstellungen nicht lesen, die es prüft, und deshalb nicht erkennen, ob ein Installationsvorgang lief oder auf einen Neustart wartete. Daher hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und führe den Befehl erneut aus. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Als der Scan begann, konnte InstallerClean nicht prüfen, ob Windows Installer beschäftigt war. Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und führe den Befehl erneut aus. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Als der Scan begann, konnte InstallerClean nicht prüfen, ob Windows Installer beschäftigt war, weil Windows einen Fehler gemeldet hat: {0} (Fehler {1}). Deshalb hat es die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Starte Windows neu und führe den Befehl erneut aus. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has cleared. | Als der Scan begann, hatte Windows Installer etwas in Arbeit oder ausstehend. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Führe den Befehl erneut aus, sobald das erledigt ist. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again. | Die Uhr dieses PCs ist während des Scans zurückgesprungen oder nachgegangen. Deshalb hat InstallerClean die Dateien in {InstallerFolder} zurückgehalten, die in den Einträgen keines installierten Programms genannt werden, statt sie anzubieten. Führe den Befehl erneut aus. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Fehler: InstallerClean konnte die Windows-Installer-Sperre nicht übernehmen, die verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts gelöscht. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Fehler: InstallerClean konnte die Windows-Installer-Sperre nicht übernehmen, die verhindert, dass zwei Programme gleichzeitig installierte Software ändern, und konnte deshalb nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts verschoben. Versuche es noch einmal und starte Windows neu, wenn es weiterhin auftritt. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Fehler: Windows hat InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt ist, und damit ließ sich nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts gelöscht. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Fehler: Windows hat InstallerClean die Berechtigung verweigert zu prüfen, ob Windows Installer beschäftigt ist, und damit ließ sich nicht ausschließen, dass eine Datei mittendrin doch gebraucht wird. Es wurde nichts verschoben. |
| Moving {0} unneeded {1} to {2}... | {0} nicht benötigte {1} werden nach {2} verschoben... |
| Moved {0} unneeded {1}. | {0} nicht benötigte {1} verschoben. |
| Check that your programs still update and uninstall as normal, then delete {0}. | Prüfe, ob deine Programme sich weiterhin wie gewohnt aktualisieren und deinstallieren lassen, und lösche dann {0}. |
| InstallerClean is free. If it helped, a tip is much appreciated: {0} | InstallerClean ist kostenlos. Wenn es dir geholfen hat, freue ich mich sehr über ein Trinkgeld: {0} |
| Update now? It's strongly recommended to use the latest version. [{0}/{1}] | Jetzt aktualisieren? Es wird dringend empfohlen, die neueste Version zu verwenden. [{0}/{1}] |
| Y | J |
| N | N |
| No answer after a minute, so InstallerClean updates without asking, as it does when nobody is there to answer. | Keine Antwort nach einer Minute, also aktualisiert sich InstallerClean ohne zu fragen, wie wenn niemand da ist, der antworten kann. |
| Send an anonymous report of this run? It is saved at {0}. Type ? to see what it holds. [{1}/{2}/?] | Anonymen Bericht über diesen Durchlauf senden? Er ist gespeichert unter {0}. Gib ? ein, um zu sehen, was er enthält. [{1}/{2}/?] |
| the chart at {0} | dem Diagramm unter {0} |
| The report adds your result to {0}, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Der Bericht fügt dein Ergebnis zu {0} hinzu, das zeigt, wie viel Speicherplatz Menschen mit InstallerClean freigeben. Außerdem enthält er diese technischen Angaben, die mir helfen, Fehler zu finden: |
| The full report is printed below. | Der vollständige Bericht steht unten. |
| Report sent. Thank you. | Bericht gesendet. Danke. |
| Report not sent. | Bericht nicht gesendet. |
| No answer after a minute. | Keine Antwort nach einer Minute. |
| The report could not be sent. It is kept in {0}. | Der Bericht konnte nicht gesendet werden. Er wird in {0} aufbewahrt. |
| Sending a report left waiting by an earlier run. | Ein Bericht, der seit einem früheren Durchlauf auf das Senden wartet, wird gesendet. |
| The new version will scan and list what it finds, without deleting anything. To delete, run /d again afterwards. | Die neue Version scannt und listet auf, was sie findet, ohne etwas zu löschen. Zum Löschen führe danach /d erneut aus. |
| The new version will scan and list what it finds, without moving anything. To move, run /m again afterwards. | Die neue Version scannt und listet auf, was sie findet, ohne etwas zu verschieben. Zum Verschieben führe danach /m erneut aus. |
| InstallerClean is in use, so it can't update itself now. | InstallerClean wird gerade verwendet und kann sich deshalb jetzt nicht aktualisieren. |
| Running {0} with InstallerClean {1} instead. | Stattdessen wird {0} mit InstallerClean {1} ausgeführt. |
| The newer version couldn't be run. | Die neuere Version konnte nicht ausgeführt werden. |
| Running {0} with InstallerClean {1}. It takes this copy's place once the run ends. | {0} wird mit InstallerClean {1} ausgeführt. Sobald der Lauf beendet ist, ersetzt diese Version diese Kopie. |
| {0}: {1} | {0}: {1} |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again to update. | Diese Version benötigt .NET Framework 4.8. Installiere es von Microsoft, starte den PC neu und führe InstallerClean dann erneut aus, um zu aktualisieren. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | Das lässt sich leicht rückgängig machen. Verschiebe sie aus {0} zurück nach {InstallerFolder}, und alles ist wieder wie vorher. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean konnte den Sicherungsordner nicht mehr bestätigen und hat deshalb angehalten. Prüfe {0} und führe den Befehl dann erneut aus. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Ein anderer InstallerClean-Prozess hält die Einzelinstanz-Sperre (die GUI oder ein anderer CLI-Lauf). Exit-Code 75 (vorübergehend); ein späterer Wiederholungsversuch ist sicher. |
| Stopped after deleting {0} of {1} {2}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Angehalten nach dem Löschen von {0} von {1} {2}: InstallerClean konnte nicht mehr sicher sein, dass keine andere InstallerClean-Instanz tätig war. Führe denselben Befehl erneut aus. Exit-Code 2 (teilweise). |
| Stopped after moving {0} of {1} {2} to {3}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Angehalten nach dem Verschieben von {0} von {1} {2} nach {3}: InstallerClean konnte nicht mehr sicher sein, dass keine andere InstallerClean-Instanz tätig war. Führe denselben Befehl erneut aus. Exit-Code 2 (teilweise). |
| Stopped before deleting any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Angehalten, bevor eine Datei gelöscht wurde: InstallerClean konnte nicht mehr sicher sein, dass keine andere InstallerClean-Instanz tätig war. Führe denselben Befehl erneut aus. Exit-Code 75 (vorübergehend). |
| Stopped before moving any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Angehalten, bevor eine Datei verschoben wurde: InstallerClean konnte nicht mehr sicher sein, dass keine andere InstallerClean-Instanz tätig war. Führe denselben Befehl erneut aus. Exit-Code 75 (vorübergehend). |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed. Run it again from an administrator prompt. | InstallerClean braucht Administratorrechte, um alle auf diesem PC installierten Programme zu sehen, läuft aber nicht als Administrator und scannt deshalb nicht. Es wurde nichts verändert. Führe es erneut in einer Eingabeaufforderung als Administrator aus. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Hinweis: Das Schreiben in das Ereignisprotokoll ist fehlgeschlagen. Prüfe die Berechtigungen des Anwendungsprotokolls oder die Gruppenrichtlinie. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - {InstallerFolder} aufräumen |
| Removes cached .msi and .msp files that no installed program still needs. | Entfernt .msi-/.msp-Dateien, die kein installiertes Programm mehr braucht. |
| Needs an elevated (administrator) prompt and won't scan without one. | Nur mit Administratorrechten; ohne sie scannt es nicht. |
| Usage: | Verwendung: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Diese Hilfe anzeigen (auch /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Die Version ausgeben (auch -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Nur scannen - nicht benötigte Dateien |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Nicht benötigte Dateien endgültig löschen |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         In den gespeicherten Sicherungsordner |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PFAD    An den angegebenen Pfad |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blockiert die Eingabeaufforderung bis zum Ende,<br>damit ein Skript oder eine geplante Aufgabe darauf warten kann. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Der Ordner gilt pro Benutzer; geplante Läufe und SYSTEM brauchen /m PFAD. |
| Before it scans, it checks for a newer version of InstallerClean.<br>When you run it yourself, it asks before updating, and updates if nobody<br>answers within a minute. When it runs on its own, in a scheduled task or<br>a script, it updates without asking.<br>Add /noupdate to skip the check. | Vor dem Scannen prüft es, ob es eine neuere Version von InstallerClean<br>gibt. Wenn du es selbst startest, fragt es vor dem Aktualisieren und<br>aktualisiert, wenn innerhalb einer Minute niemand antwortet. Läuft es von<br>selbst, in einer geplanten Aufgabe oder einem Skript, aktualisiert es ohne<br>zu fragen. Mit /noupdate wird die Prüfung übersprungen. |
| Exit codes: | Exit-Codes: |
|   0   success: the run did what it was asked |   0   Erfolg: Der Lauf hat getan, worum gebeten wurde |
|   1   nothing processed: the run failed or was refused |   1   nichts verarbeitet: der Lauf schlug fehl oder wurde abgelehnt |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   teilweise: einiges verarbeitet, anderes nicht (Fehler oder Strg+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  vorübergehend: etwas hat den Lauf blockiert (siehe Meldung) |
|   130 cancelled (Ctrl+C) |   130 abgebrochen (Strg+C) |
