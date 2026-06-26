# InstallerClean in Deutsch (German)

The text of InstallerClean's interface and command-line tool in English on the left, with the German translation beside it, grouped by where each line appears in the app. It is here so someone who really knows German can read through the translation and flag anything that doesn't read well. See [Can you help translate InstallerClean?](../../README.de.md#can-you-help-translate-installerclean) for how to suggest a change, whether an issue or a pull request.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.de.resx`](../../src/InstallerClean.Core/Resources/Strings.de.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | Deutsch |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Über |
| Registered files that should not be deleted | Registrierte Dateien, die nicht gelöscht werden sollten |
| Unneeded files that are safe to delete | Nicht benötigte Dateien, die bedenkenlos gelöscht werden können |
| Confirm move | Verschieben bestätigen |
| Confirm delete | Löschen bestätigen |
| Recycle Bin unavailable | Papierkorb nicht verfügbar |

## Section headings

| English | Deutsch |
| --- | --- |
| PRODUCTS | PRODUKTE |
| PATCHES | PATCHES |
| PRODUCT DETAILS | PRODUKTDETAILS |
| MOVE LOCATION | ZIELORT |
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
| _Delete | _Löschen |
| _Delete permanently | Endgültig _löschen |
| _Done | _Fertig |
| Details | Details |
| _Buy me a cuppa | Spendier mir einen _Kaffee |
| Leave a _star on GitHub | Einen Stern auf _GitHub hinterlassen |
| MIT licence | MIT-Lizenz |
| _Move | _Verschieben |
| _Move instead | Stattdessen _verschieben |
| Path to folder if you Move instead of Delete | Ordnerpfad, falls du verschiebst statt löschst |
| Open _release page | _Release-Seite öffnen |
| _Re-scan | _Neu scannen |
| _Scan again | Erneut _scannen |
| Send report | Bericht senden |
| _Send | _Senden |

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
| Moving {0} {1}... | {0} {1} werden verschoben... |
| Deleting {0} {1}... | {0} {1} werden gelöscht... |
| Move cancelled. {0} of {1} {2} processed. | Verschieben abgebrochen. {0} von {1} {2} verarbeitet. |
| Delete cancelled. {0} of {1} {2} processed. | Löschen abgebrochen. {0} von {1} {2} verarbeitet. |
| Move failed ({0}). Details in {1}. | Verschieben fehlgeschlagen ({0}). Details in {1}. |
| Move failed ({0}). The crash log could not be written. | Verschieben fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden. |
| Delete failed ({0}). Details in {1}. | Löschen fehlgeschlagen ({0}). Details in {1}. |
| Delete failed ({0}). The crash log could not be written. | Löschen fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden. |
| Access denied. Run as administrator. | Zugriff verweigert. Als Administrator ausführen. |
| Scan failed: installer database unavailable. | Scan fehlgeschlagen: Installer-Datenbank nicht verfügbar. |
| Scan cancelled. | Scan abgebrochen. |
| Ready | Bereit |
| Scan failed ({0}). Details in {1}. | Scan fehlgeschlagen ({0}). Details in {1}. |
| Scan failed ({0}). The crash log could not be written. | Scan fehlgeschlagen ({0}). Das Absturzprotokoll konnte nicht geschrieben werden. |

## Main screen text

| English | Deutsch |
| --- | --- |
| The unneeded files below are safe to delete. | Die nicht benötigten Dateien unten können bedenkenlos gelöscht werden. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Sie liegen in C:\Windows\Installer und blieben zurück, als ein Programm deinstalliert wurde ({0}), ein neuerer Patch einen älteren ersetzt hat ({1}) oder der Herausgeber ihn zurückgezogen hat ({2}). InstallerClean listet nur Dateien auf, die Windows selbst als erledigt meldet. |
| Delete them to the Recycle Bin, or Move them elsewhere first if you'd rather keep a copy. | Lösche sie in den Papierkorb, oder verschiebe sie zuerst woandershin, wenn du lieber eine Kopie behalten möchtest. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | Gerade benutzt etwas Windows Installer, normalerweise ein Windows-Update oder ein Programm, das im Hintergrund installiert. Verschieben und Löschen pausieren, solange das läuft, damit InstallerClean den Installer-Cache nicht anrührt, während er sich ändert. Wenn es fertig ist, scanne erneut, und sie sind wieder verfügbar. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Eine frühere Windows-Installer-Transaktion ist auf diesem Rechner ausgesetzt. Setze diese Installation fort oder mach sie rückgängig (oder starte Windows neu), bevor du den Cache aufräumst. |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows hat für den nächsten Neustart eine Dateiumbenennung in der Warteschlange, die den Installer-Cache betrifft. Starte Windows neu, bevor du aufräumst. |
| Select a file to view details. | Wähle eine Datei, um Details anzuzeigen. |
| Select a product to view details. | Wähle ein Produkt, um Details anzuzeigen. |
| No metadata available. | Keine Metadaten verfügbar. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | Diese Installer-Datei wurde gelöscht. InstallerClean war es nicht, es entfernt nie eine Datei, die ein Programm noch braucht; etwas anderes hat diese hier gelöscht, bevor du InstallerClean ausgeführt hast.<br><br>Im Moment macht das keine Probleme, und es wird auch keine geben, bis zu dem Tag, an dem du das Programm, zu dem sie gehört, reparieren, aktualisieren oder deinstallieren willst. Dieser Schritt kann dann fehlschlagen, weil Windows nach dieser Datei sucht und sie nicht da ist.<br><br>Um es zu beheben, lade den Installer dieses Programms beim Hersteller herunter und führe ihn über deine vorhandene Installation aus (deinstalliere nicht zuerst, denn das Deinstallieren ist selbst ein Schritt, der diese Datei braucht). Verwende möglichst die Version, die du installiert hast, denn Windows lehnt eine andere unter Umständen ab. Das setzt die Datei in der Regel wieder ein, und deine Einstellungen bleiben normalerweise unangetastet, aber Microsoft garantiert es nicht; sein eigenes letztes Mittel ist, das Programm neu zu installieren, oder Windows selbst. |
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
| Nothing to clean up in C:\Windows\Installer | Nichts aufzuräumen in C:\Windows\Installer |
| Scanned {0} {1} in {2} | {0} {1} in {2} gescannt |
| Copy them back if anything stops working | Kopier sie zurück, falls etwas nicht mehr funktioniert |
| Restore them from the Recycle Bin if anything stops working | Stell sie aus dem Papierkorb wieder her, falls etwas nicht mehr funktioniert |
| {0} freed | {0} freigegeben |
| {0} moved | {0} verschoben |
| {0} moved, some files could not be processed | {0} verschoben, einige Dateien konnten nicht verarbeitet werden |
| {0} freed, some files could not be processed | {0} freigegeben, einige Dateien konnten nicht verarbeitet werden |
| {0} {1} moved to {2} | {0} {1} nach {2} verschoben |
| {0} {1} moved to {2}. {3} {4} | {0} {1} nach {2} verschoben. {3} {4} |
| {0} {1} sent to the Recycle Bin | {0} {1} in den Papierkorb verschoben |
| {0} {1} sent to the Recycle Bin. {2} {3} | {0} {1} in den Papierkorb verschoben. {2} {3} |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} endgültig gelöscht. Sie ist nicht in den Papierkorb gewandert. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1} endgültig gelöscht. Sie sind nicht in den Papierkorb gewandert. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. {2} {3} | {0} {1} endgültig gelöscht. Sie ist nicht in den Papierkorb gewandert. {2} {3} |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. {2} {3} | {0} {1} endgültig gelöscht. Sie sind nicht in den Papierkorb gewandert. {2} {3} |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Das ist in Ordnung, sie konnte bedenkenlos entfernt werden. InstallerClean entfernt nur Dateien, die Windows als erledigt meldet, nie eine, die ein Programm noch braucht. Sollte ein Löschen wider Erwarten je dazu führen, dass sich ein Programm nicht mehr reparieren, aktualisieren oder deinstallieren lässt, setzt eine Neuinstallation beim Hersteller die Datei meist wieder ein, auch wenn Microsoft das nicht garantiert. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Das ist in Ordnung, sie konnten bedenkenlos entfernt werden. InstallerClean entfernt nur Dateien, die Windows als erledigt meldet, nie eine, die ein Programm noch braucht. Sollte ein Löschen wider Erwarten je dazu führen, dass sich ein Programm nicht mehr reparieren, aktualisieren oder deinstallieren lässt, setzt eine Neuinstallation beim Hersteller die Datei meist wieder ein, auch wenn Microsoft das nicht garantiert. |

## Recycle Bin unavailable

| English | Deutsch |
| --- | --- |
| The Recycle Bin isn't available for this drive | Der Papierkorb ist für dieses Laufwerk nicht verfügbar |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Daher wurde diese {1} ({2}) nicht gelöscht. Du kannst sie an einen sicheren Ort verschieben oder endgültig löschen. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Daher wurden diese {0} {1} ({2}) nicht gelöscht. Du kannst sie an einen sicheren Ort verschieben oder endgültig löschen. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Sie zu löschen ist sicher. InstallerClean entfernt nur Dateien, die Windows als erledigt meldet, nie eine, die ein Programm noch braucht, und der Papierkorb ist nur eine zusätzliche Absicherung. Sollte ein Löschen wider Erwarten je dazu führen, dass sich ein Programm nicht mehr reparieren, aktualisieren oder deinstallieren lässt, setzt eine Neuinstallation beim Hersteller die Datei meist wieder ein, auch wenn Microsoft das nicht garantiert. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Sie zu löschen ist sicher. InstallerClean entfernt nur Dateien, die Windows als erledigt meldet, nie eine, die ein Programm noch braucht, und der Papierkorb ist nur eine zusätzliche Absicherung. Sollte ein Löschen wider Erwarten je dazu führen, dass sich ein Programm nicht mehr reparieren, aktualisieren oder deinstallieren lässt, setzt eine Neuinstallation beim Hersteller die Datei meist wieder ein, auch wenn Microsoft das nicht garantiert. |

## Summaries and counts

| English | Deutsch |
| --- | --- |
| {0} file still needed | {0} Datei noch benötigt |
| {0} files still needed | {0} Dateien noch benötigt |
| {0} unneeded file to clean up | {0} nicht benötigte Datei zum Aufräumen |
| {0} unneeded files to clean up | {0} nicht benötigte Dateien zum Aufräumen |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} registrierte Datei fehlt (nicht von InstallerClean gelöscht). Im Moment kein Problem, aber eine spätere Reparatur, Aktualisierung oder Deinstallation dieses Programms könnte fehlschlagen. Öffne Details, um zu erfahren, was zu tun ist. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} registrierte Dateien fehlen (nicht von InstallerClean gelöscht). Im Moment kein Problem, aber eine spätere Reparatur, Aktualisierung oder Deinstallation dieser Programme könnte fehlschlagen. Öffne Details, um zu erfahren, was zu tun ist. |
| {0} stale MSI entry detected (file already gone from disk; InstallerClean doesn't unregister it). | {0} veralteter MSI-Eintrag erkannt (Datei bereits vom Datenträger verschwunden; InstallerClean hebt die Registrierung nicht auf). |
| {0} stale MSI entries detected (files already gone from disk; InstallerClean doesn't unregister them). | {0} veraltete MSI-Einträge erkannt (Dateien bereits vom Datenträger verschwunden; InstallerClean hebt die Registrierung nicht auf). |
| {0} of {1} {2} | {0} von {1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} verwaist, {1} ersetzt, {2} veraltet ({3}) |
| {0} registered file that is still needed ({1}) | {0} registrierte Datei, die noch benötigt wird ({1}) |
| {0} registered files that are still needed ({1}) | {0} registrierte Dateien, die noch benötigt werden ({1}) |

## Confirmation dialogs

| English | Deutsch |
| --- | --- |
| Move {0} {1} ({2})? | {0} {1} verschieben ({2})? |
| Files will be moved to {0}. | Die Dateien werden nach {0} verschoben. |
| Delete {0} {1} ({2})? | {0} {1} löschen ({2})? |
| Files will be sent to the Recycle Bin. If you'd like backup copies, use Move instead. | Die Dateien werden in den Papierkorb verschoben. Wenn du Sicherungskopien möchtest, nutze stattdessen Verschieben. |

## Error messages

| English | Deutsch |
| --- | --- |
| Administrator rights required | Administratorrechte erforderlich |
| InstallerClean requires administrator privileges.<br><br>Please right-click and choose 'Run as administrator'. | InstallerClean benötigt Administratorrechte.<br><br>Klick mit der rechten Maustaste und wähle 'Als Administrator ausführen'. |
| Installer database unavailable | Installer-Datenbank nicht verfügbar |
| Scan failed | Scan fehlgeschlagen |
| The Windows Installer database appears to be empty or inaccessible. This is unusual even on a fresh Windows install and typically means the database is corrupt or a third-party tool has cleared it. Running 'sfc /scannow' from an elevated prompt usually repairs it. | Die Windows-Installer-Datenbank scheint leer oder nicht zugänglich zu sein. Das ist selbst bei einer frischen Windows-Installation ungewöhnlich und bedeutet meist, dass die Datenbank beschädigt ist oder ein Drittanbieter-Tool sie geleert hat. 'sfc /scannow' an einer Eingabeaufforderung mit erhöhten Rechten behebt das in der Regel. |
| Access denied enumerating installed products. Run as administrator. | Zugriff beim Aufzählen der installierten Produkte verweigert. Als Administrator ausführen. |
| Windows Installer refused to list products after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installer hat das Auflisten der Produkte nach {0} aufeinanderfolgenden Fehlern verweigert (letzter Fehlercode {1}). Starte Windows neu oder führe 'sfc /scannow' an einer Eingabeaufforderung mit erhöhten Rechten aus. |
| Invalid destination | Ungültiges Ziel |
| Could not write to destination | Schreiben am Ziel nicht möglich |
| Move failed | Verschieben fehlgeschlagen |
| Delete failed | Löschen fehlgeschlagen |
| The destination cannot be inside the Windows Installer folder. | Das Ziel darf nicht im Windows-Installer-Ordner liegen. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Das Ziel {0} liegt unter einem Windows-Systemordner. Wähle einen Pfad außerhalb von %SystemRoot%, %ProgramFiles% und %ProgramData%. |
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
| Access denied. | Zugriff verweigert. |
| The operation failed. Try again or restart Windows. | Der Vorgang ist fehlgeschlagen. Versuch es erneut oder starte Windows neu. |
| Unknown error. | Unbekannter Fehler. |
| Couldn't send this file to the Recycle Bin (error {0}). It may be locked, in use or blocked by Windows. Move it instead. | Diese Datei konnte nicht in den Papierkorb verschoben werden (Fehler {0}). Sie ist möglicherweise gesperrt, in Benutzung oder von Windows blockiert. Verschieb sie stattdessen. |
| Windows blocked access to this file, even with administrator rights (error {0}). It is usually an ownership or permissions lock. Move it instead. | Windows hat den Zugriff auf diese Datei blockiert, selbst mit Administratorrechten (Fehler {0}). Meist ist es eine Sperre durch Besitzrechte oder Berechtigungen. Verschieb sie stattdessen. |
| This file is open or locked by another program (error {0}). Close that program, or whatever is scanning it, then try again, or Move it instead. | Diese Datei ist von einem anderen Programm geöffnet oder gesperrt (Fehler {0}). Schließe dieses Programm, oder was auch immer sie gerade prüft, und versuch es erneut, oder verschieb sie stattdessen. |
| The file was permanently deleted because it could not be sent to the Recycle Bin. | Die Datei wurde endgültig gelöscht, weil sie nicht in den Papierkorb verschoben werden konnte. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Das Verschieben von Dateien in den Windows-Installer-Ordner wird abgelehnt (Ziel: {0}). |
| Destination must be a fully qualified path (relative paths resolve against the process current directory and are unsafe under elevation): {0} | Das Ziel muss ein vollständig qualifizierter Pfad sein (relative Pfade werden relativ zum aktuellen Verzeichnis des Prozesses aufgelöst und sind bei erhöhten Rechten unsicher): {0} |
| Destination folder canonical path changed mid-batch: {0} | Der kanonische Pfad des Zielordners hat sich während des Vorgangs geändert: {0} |
| Cannot write to {0}. | Schreiben in {0} nicht möglich. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Nach 10.000 Versuchen konnte kein eindeutiger Dateiname für '{0}' gefunden werden. |

## Update check

| English | Deutsch |
| --- | --- |
| Check for updates | Nach Updates suchen |
| Checking... | Wird geprüft... |
| Up to date. | Auf dem neuesten Stand. |
| Update available | Update verfügbar |
| You're running version {0}.<br>Version {1} is available. | Du verwendest Version {0}.<br>Version {1} ist verfügbar. |
| Couldn't reach GitHub. Check your internet connection and try again. | GitHub war nicht erreichbar. Prüfe deine Internetverbindung und versuch es erneut. |
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | GitHub hat eine Fehlerantwort zurückgegeben. Die Releases-API ist möglicherweise ratenbegrenzt; versuch es in ein paar Minuten erneut. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Die Antwort von GitHub enthielt keine erkennbare Version. Versuch es später erneut oder öffne die Releases-Seite direkt. |
| The check timed out. Your connection to GitHub may be slow; try again. | Bei der Prüfung wurde das Zeitlimit überschritten. Deine Verbindung zu GitHub ist vielleicht langsam; versuch es erneut. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | Die Prüfung ist aus unbekanntem Grund fehlgeschlagen. Details stehen in crash.log, falls du es melden möchtest. |

## Opening links in your browser

| English | Deutsch |
| --- | --- |
| Couldn't open your browser | Browser konnte nicht geöffnet werden |
| The link couldn't be opened in your normal-user browser. The URL has been copied to your clipboard so you can open it manually:<br><br>{0} | Der Link konnte nicht im Browser deines normalen Benutzers geöffnet werden. Die URL wurde in die Zwischenablage kopiert, damit du sie manuell öffnen kannst:<br><br>{0} |
| The link couldn't be opened in your normal-user browser, and copying it to the clipboard also failed. The URL is:<br><br>{0} | Der Link konnte nicht im Browser deines normalen Benutzers geöffnet werden, und das Kopieren in die Zwischenablage ist ebenfalls fehlgeschlagen. Die URL lautet:<br><br>{0} |

## Sending the summary

| English | Deutsch |
| --- | --- |
| Sending... | Wird gesendet... |
| Thanks! Report sent. | Danke! Bericht gesendet. |
| Sending failed. Try again later. | Senden fehlgeschlagen. Versuch es später erneut. |
| No report to send. | Kein Bericht zum Senden. |
| Send this to No Faff? | Das an No Faff senden? |
| Nothing identifies you or your machine; it just lets me know InstallerClean's working and how much space people are freeing. It goes to nofaff.netlify.app/api/result-log. | Nichts identifiziert dich oder deinen Rechner; es zeigt mir nur, dass InstallerClean funktioniert und wie viel Platz die Leute freigeben. Es geht an nofaff.netlify.app/api/result-log. |

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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log erfasst unbehandelte Ausnahmen von InstallerClean.<br># Bei erhöhten Rechten können die Ausnahmemeldungen des Frameworks<br># Dateipfade aus der laufenden Sitzung enthalten (einschließlich der<br># von Windows-Installer-Abfragen aufgezählten Profile anderer<br># Benutzer). Netzwerkfehlermeldungen der Update-Prüfung oder des<br># Ergebnisprotokoll-POSTs können die Ziel-URL und die aufgelöste<br># IP-/Proxy-Adresse enthalten. Entferne beide Arten von Details,<br># bevor du diese Datei an einen öffentlichen Fehlerbericht anhängst.<br> |

## Tooltips (hover text)

| English | Deutsch |
| --- | --- |
| If it helped, buy me a cup of tea. | Wenn es geholfen hat, spendier mir eine Tasse Kaffee. |
| It's thirsty work! | Das macht durstig! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Abbruch angefordert. InstallerClean wartet, bis der aktuelle Schritt einen Haltepunkt erreicht. Bei starker Datenträgeraktivität oder einem MSI-Datenbankaufruf kann das ein paar Sekunden dauern. |
| Close | Schließen |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | Hinterlasse einen Stern auf GitHub, melde ein Issue oder schreib in Discussions. Jede Rückmeldung ist willkommen. |
| or report an Issue or post in Discussions. Any feedback welcome. | oder melde ein Issue oder schreib in Discussions. Jede Rückmeldung ist willkommen. |
| Minimise | Minimieren |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Ganz wie du magst, aber ich freue mich darüber. Sendet eine anonyme Zusammenfassung, die mir nur zeigt, ob es funktioniert und wie viel Platz die Leute freigeben. Auf dem nächsten Bildschirm siehst du vor dem Bestätigen, was gesendet wird. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Ganz wie du magst, aber ich freue mich darüber. Sendet eine anonyme Zusammenfassung, die mir nur zeigt, ob es funktioniert. Auf dem nächsten Bildschirm siehst du vor dem Bestätigen, was gesendet wird. |
| Move the unneeded files to the Move location. | Verschiebt die nicht benötigten Dateien an den Zielort. |
| Move the unneeded files to the Move location. Choose one first. | Verschiebt die nicht benötigten Dateien an den Zielort. Wähle zuerst einen aus. |
| Send the unneeded files to the Recycle Bin. | Verschiebt die nicht benötigten Dateien in den Papierkorb. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Antragstellername aus dem eingebetteten Authenticode-Zertifikat. Die Zertifikatskette wurde nicht geprüft. |
| Change language. The program will restart. | Sprache ändern. Das Programm wird neu gestartet. |

## Screen reader labels

| English | Deutsch |
| --- | --- |
| Buy me a cup of tea | Spendier mir eine Tasse Kaffee |
| Buy me a cuppa (About window) | Spendier mir einen Kaffee (Fenster Über) |
| Cancel operation | Vorgang abbrechen |
| Cancel scan | Scan abbrechen |
| Cancel startup scan | Start-Scan abbrechen |
| Close | Schließen |
| Close window | Fenster schließen |
| Close result and return to main window | Ergebnis schließen und zum Hauptfenster zurückkehren |
| Leave a star on GitHub | Einen Stern auf GitHub hinterlassen |
| Leave a star on GitHub (About window) | Einen Stern auf GitHub hinterlassen (Fenster Über) |
| Minimise | Minimieren |
| Move all unneeded installer files to the chosen destination folder | Alle nicht benötigten Installer-Dateien in den gewählten Zielordner verschieben |
| Send all unneeded installer files to the Recycle Bin | Alle nicht benötigten Installer-Dateien in den Papierkorb verschieben |
| Delete sends the unneeded files to the Recycle Bin. Cancel closes without deleting. | Löschen verschiebt die nicht benötigten Dateien in den Papierkorb. Abbrechen schließt, ohne zu löschen. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Verschieben legt die nicht benötigten Dateien in den gewählten Zielordner. Abbrechen lässt sie, wo sie sind. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Wähle, wie mit den nicht benötigten Dateien verfahren werden soll: an einen sicheren Ort verschieben, endgültig löschen oder abbrechen. |
| Move the unneeded files to a folder you choose | Die nicht benötigten Dateien in einen von dir gewählten Ordner verschieben |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Die nicht benötigten Dateien endgültig löschen, weil der Papierkorb für dieses Laufwerk nicht verfügbar ist |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Sendet an nofaff.netlify.app. Nur Zählwerte und Bezeichnungen. Du siehst die genauen Daten vor dem Senden. |
| Say thanks | Danke sagen |
| Send posts the report shown to No Faff. Cancel sends nothing. | Senden übermittelt den angezeigten Bericht an No Faff. Abbrechen sendet nichts. |
| Check for updates | Nach Updates suchen |
| Checks the GitHub releases API over HTTPS for a newer version. | Prüft die GitHub-Releases-API über HTTPS auf eine neuere Version. |
| Open the release page to download the newer version, or cancel to keep the current version. | Öffne die Release-Seite, um die neuere Version herunterzuladen, oder brich ab, um die aktuelle Version zu behalten. |
| MIT licence | MIT-Lizenz |
| Opens the licence file on github.com in your browser. | Öffnet die Lizenzdatei auf github.com in deinem Browser. |
| Move location | Zielort |
| Products | Produkte |
| Patches | Patches |
| Product details | Produktdetails |
| Move destination folder | Zielordner zum Verschieben |
| Operation progress | Vorgangsfortschritt |
| Scan C:\Windows\Installer again | C:\Windows\Installer erneut scannen |
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
| Dialog text | Dialogtext |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Dateien, die nicht verarbeitet werden konnten |
| Explains this folder, and how to recover a file, in the README | Erklärt diesen Ordner, und wie sich eine Datei wiederherstellen lässt, im README |
| Result log preview | Vorschau des Ergebnisprotokolls |
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
| Unknown argument: '{0}' | Unbekanntes Argument: '{0}' |
| Cancelling... | Wird abgebrochen... |
| Cancelled. | Abgebrochen. |
| Error: {0}. Details written to {1}. | Fehler: {0}. Details gespeichert in {1}. |
| Error: {0}. The crash log could not be written. | Fehler: {0}. Das Absturzprotokoll konnte nicht geschrieben werden. |
| Scanning C:\Windows\Installer... | C:\Windows\Installer wird gescannt... |
| Found {0} {1} to clean up ({2}). | {0} {1} zum Aufräumen gefunden ({2}). |
| Nothing to do. | Nichts zu tun. |
| Deleting {0} {1}... | {0} {1} werden gelöscht... |
| Deleted {0} {1}. | {0} {1} gelöscht. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Fehler: Der Papierkorb ist für dieses Volume nicht verfügbar, daher wurde nichts gelöscht. Nutze /m, um die Dateien stattdessen zu verschieben, oder aktiviere den Papierkorb wieder und führe den Vorgang erneut aus. |
| Error: no move destination specified. Use /m PATH or set a default in the GUI. | Fehler: kein Verschiebeziel angegeben. Nutze /m PFAD oder lege in der GUI einen Standard fest. |
| Error: destination cannot be inside the Windows Installer folder. | Fehler: Das Ziel darf nicht im Windows-Installer-Ordner liegen. |
| Error: destination must be a fully qualified path. Got: {0} | Fehler: Das Ziel muss ein vollständig qualifizierter Pfad sein. Erhalten: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Fehler: Das Ziel {0} liegt unter einem Windows-Systemordner. Wähle einen Pfad außerhalb von %SystemRoot%, %ProgramFiles% und %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Fehler: Gerade benutzt etwas Windows Installer, normalerweise ein Windows-Update oder ein Programm, das im Hintergrund installiert. Verschieben und Löschen sind blockiert, solange das läuft. Versuch es erneut, sobald es fertig ist. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Fehler: Eine frühere Windows-Installer-Transaktion ist auf diesem Rechner ausgesetzt. Setze diese Installation fort oder mach sie rückgängig (oder starte Windows neu), bevor du den Cache aufräumst. |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Fehler: Ein für nach dem Neustart eingeplanter Dateivorgang betrifft den Installer-Cache ({0}). Starte Windows neu, um diesen Vorgang abzuschließen, bevor du aufräumst. |
| Moving {0} {1} to {2}... | {0} {1} werden nach {2} verschoben... |
| Moved {0} {1}. | {0} {1} verschoben. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Ein anderer InstallerClean-Prozess hält die Einzelinstanz-Sperre (die GUI oder ein anderer CLI-Lauf). Exit-Code 75 (vorübergehend); ein späterer Wiederholungsversuch ist sicher. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Hinweis: Das Schreiben in das Ereignisprotokoll ist fehlgeschlagen. Prüfe die Berechtigungen des Anwendungsprotokolls oder die Gruppenrichtlinie. |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - C:\Windows\Installer aufräumen |
| Usage: | Verwendung: |
|   installerclean-cli --help   Show this help (also accepts /?, -h) |   installerclean-cli --help     Diese Hilfe anzeigen (akzeptiert auch /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Die Version ausgeben (akzeptiert auch -v) |
|   installerclean-cli /s       Scan only - list removable files |   installerclean-cli /s         Nur scannen - nicht benötigte Dateien auflisten |
|   installerclean-cli /d       Delete removable files (Recycle Bin) |   installerclean-cli /d         Nicht benötigte Dateien löschen (Papierkorb) |
|   installerclean-cli /m       Move to saved default location |   installerclean-cli /m         An den gespeicherten Standardort verschieben |
|   installerclean-cli /m PATH  Move to specified path |   installerclean-cli /m PFAD    An den angegebenen Pfad verschieben |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli ist ein echter Konsolenprozess und blockiert die |
| until it finishes; redirect or pipe its output as you would any | Eingabeaufforderung, bis er fertig ist; leite seine Ausgabe wie bei jeder |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | anderen Konsolen-EXE um oder per Pipe weiter. Die GUI liegt in InstallerClean.exe. |
| Exit codes: | Exit-Codes: |
|   0   success: every flagged file was processed |   0   Erfolg: jede markierte Datei wurde verarbeitet |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   Fehler: nichts verarbeitet (ungültige Argumente, Scan fehlgeschlagen, alle Dateien fehlgeschlagen) |
|   2   partial: some files processed, some failed |   2   teilweise: einige Dateien verarbeitet, einige fehlgeschlagen |
|   75  transient: a temporary condition blocked the run (see the message) |   75  vorübergehend: ein vorübergehender Zustand hat den Lauf blockiert (siehe Meldung) |
|   130 cancelled (Ctrl+C) |   130 abgebrochen (Strg+C) |
