# InstallerClean in Nederlands (Dutch)

The text of InstallerClean's interface and command-line tool in English on the left, with the Dutch translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Dutch can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.nl.resx`](../../src/InstallerClean.Core/Resources/Strings.nl.resx), so do not edit it by hand. The Dutch translation itself lives in [`gen-strings-nl.mjs`](../../scripts/translations/gen-strings-nl.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Nederlands |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Over |
| Registered files that should not be deleted | Geregistreerde bestanden die niet mogen worden verwijderd |
| Unneeded files that are safe to delete | Overbodige bestanden die veilig kunnen worden verwijderd |

## Section headings

| English | Nederlands |
| --- | --- |
| PRODUCTS | PRODUCTEN |
| PATCHES | PATCHES |
| PRODUCT DETAILS | PRODUCTGEGEVENS |
| BACKUP FOLDER | BACKUP FOLDER |
| SAY THANKS | ZEG BEDANKT |

## Buttons and actions

| English | Nederlands |
| --- | --- |
| _About | _Over |
| Copy | Kopiëren |
| Cut | Knippen |
| Paste | Plakken |
| Select all | Alles selecteren |
| _Browse... | _Bladeren... |
| _Cancel | _Annuleren |
| Check for _updates | Controleren op _updates |
| _Close | _Sluiten |
| _Delete permanently | _Definitief verwijderen |
| _Done | _Klaar |
| Details | Details |
| _Buy me a cuppa | Trakteer me op een kop _koffie |
| Leave a _star on GitHub | Geef een s_ter op GitHub |
| Apache 2.0 licence | Apache 2.0-licentie |
| _Move | Ver_plaatsen |
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
| Open _release page | _Releasepagina openen |
| _Re-scan | Opnieuw _scannen |
| _Scan again | _Opnieuw scannen |
| Send report | Rapport verzenden |
| _Send | _Verzenden |

## About window

| English | Nederlands |
| --- | --- |
| Guide and FAQ | Handleiding en FAQ |
| Report a problem | Een probleem melden |
| Check for updates automatically | Automatisch controleren op updates |

## Field labels

| English | Nederlands |
| --- | --- |
| Reason | Reden |
| Author | Auteur |
| Application | Toepassing |
| Title | Titel |
| Subject | Onderwerp |
| Keywords | Trefwoorden |
| Signing certificate | Ondertekeningscertificaat |
| File size | Bestandsgrootte |
| Comment | Opmerking |
| Product name | Productnaam |
| File | Bestand |
| Size | Grootte |
| Patches | Patches |
| (unknown) | (onbekend) |
| (patches only) | (alleen patches) |
| missing | ontbreekt |

## Status and progress

| English | Nederlands |
| --- | --- |
| Scanning... | Scannen... |
| Cancelling... | Annuleren... |
| Starting scan... | Scan starten... |
| Asking Windows about installed software... | Windows vragen naar geïnstalleerde software... |
| Scanning installer cache folder... | Installatiecache scannen... |
| Enumerating installed products... | Geïnstalleerde producten doorlopen... |
| Checking registry for additional packages... | Register controleren op extra pakketten... |
| Found {0} registered {1}. | {0} geregistreerde {1} gevonden. |
| Scan complete ({0}) | Scan voltooid ({0}) |
| Scanning local packages... | Lokale pakketten scannen... |
| Found {0} {1} you can safely delete. | Je kunt {0} {1} veilig verwijderen. |
| Preparing destination folder... | Doelmap voorbereiden... |
| Moving unneeded files... | Moving unneeded files... |
| Deleting unneeded files... | Deleting unneeded files... |
| Move cancelled. {0} of {1} {2} processed. | Verplaatsen geannuleerd. {0} van {1} {2} verwerkt. |
| Delete cancelled. {0} of {1} {2} processed. | Verwijderen geannuleerd. {0} van {1} {2} verwerkt. |
| Move failed ({0}). Details in {1}. | Verplaatsen mislukt ({0}). Details in {1}. |
| Move failed ({0}). The crash log could not be written. | Verplaatsen mislukt ({0}). Het crashlog kon niet worden geschreven. |
| Delete failed ({0}). Details in {1}. | Verwijderen mislukt ({0}). Details in {1}. |
| Delete failed ({0}). The crash log could not be written. | Verwijderen mislukt ({0}). Het crashlog kon niet worden geschreven. |
| Access denied. Windows refused the scan. | Toegang geweigerd. Windows heeft de scan geweigerd. |
| Scan failed: couldn't read the Windows Installer records. | Scan mislukt: de Windows Installer-records konden niet worden gelezen. |
| Scan cancelled. | Scan geannuleerd. |
| Ready | Gereed |
| Scan failed ({0}). Details in {1}. | Scan mislukt ({0}). Details in {1}. |
| Scan failed ({0}). The crash log could not be written. | Scan mislukt ({0}). Het crashlog kon niet worden geschreven. |

## Main screen text

| English | Nederlands |
| --- | --- |
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Ze staan in {InstallerFolder}, achtergebleven toen een programma werd verwijderd ({0}), een nieuwere patch een oudere verving ({1}) of de uitgever hem introk ({2}). InstallerClean toont alleen bestanden waarvan Windows zelf aangeeft dat het ermee klaar is. |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | Nog niets gescand. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Klik op Opnieuw scannen om {InstallerFolder} te doorzoeken op installatiebestanden die geen enkel programma nog nodig heeft. |
| These files can't be cleaned up right now. | Deze bestanden kunnen nu niet worden opgeruimd. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. |
| Select a file to view details. | Selecteer een bestand om de details te zien. |
| Select a product to view details. | Selecteer een product om de details te zien. |
| No metadata available. | Geen metadata beschikbaar. |
| This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | Het README-bestand [legt uit wat deze map is], en hoe je een bestand terugzet, in Microsofts eigen woorden. |
| (none) | (geen) |

## Reasons a file is unneeded

| English | Nederlands |
| --- | --- |
| Orphaned | Verweesd |
| Superseded | Vervangen |
| Obsoleted | Verouderd |

## Completion screen

| English | Nederlands |
| --- | --- |
| All clean | Alles schoon |
| Nothing to clean up in {InstallerFolder} | Niets op te ruimen in {InstallerFolder} |
| Scanned {0} {1} in {2} | {0} {1} gescand in {2} |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| {0} freed | {0} vrijgemaakt |
| {0} moved | {0} verplaatst |
| Nothing was moved | Er is niets verplaatst |
| Nothing was deleted | Er is niets verwijderd |
| {0} of {1} could not be moved. | {0} van {1} kon niet worden verplaatst. |
| {0} of {1} could not be moved. | {0} van {1} konden niet worden verplaatst. |
| {0} of {1} could not be deleted. | {0} van {1} kon niet worden verwijderd. |
| {0} of {1} could not be deleted. | {0} van {1} konden niet worden verwijderd. |
| {0} {1} moved to: {2} | {0} {1} verplaatst naar: {2} |
| {0} {1} moved to: {2} | {0} {1} verplaatst naar: {2} |
| {0} {1} kept in place, because a program went back to needing what the scan flagged. | {0} {1} behouden, omdat een programma ze na de scan weer nodig bleek te hebben. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} {1} kept in place, because the Windows Installer records had changed by the final check. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. |
| Moved {0} of {1} {2} before you cancelled. | {0} van {1} {2} verplaatst voordat je annuleerde. |
| Permanently deleted {0} of {1} {2} before you cancelled. | {0} van {1} {2} definitief verwijderd voordat je annuleerde. |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | Graag gedaan. Er staat een fooienpot klaar, mocht je je gul voelen. |

## Summaries and counts

| English | Nederlands |
| --- | --- |
| {0} file still needed | {0} bestand nog nodig |
| {0} files still needed | {0} bestanden nog nodig |
| {0} unneeded file to clean up | {0} overbodig bestand om op te ruimen |
| {0} unneeded files to clean up | {0} overbodige bestanden om op te ruimen |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} geregistreerd bestand ontbreekt (niet door InstallerClean verwijderd). Nu geen probleem, maar een toekomstige herstel-, update- of verwijderactie van dat programma kan mislukken. Open Details voor wat je kunt doen. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} geregistreerde bestanden ontbreken (niet door InstallerClean verwijderd). Nu geen probleem, maar een toekomstige herstel-, update- of verwijderactie van die programma's kan mislukken. Open Details voor wat je kunt doen. |
| Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| {0} of {1} {2} | {0} van {1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} verweesd, {1} vervangen, {2} verouderd ({3}) |
| {0} registered file that is still needed ({1}) | {0} geregistreerd bestand dat nog nodig is ({1}) |
| {0} registered files that are still needed ({1}) | {0} geregistreerde bestanden die nog nodig zijn ({1}) |

## Confirmation dialogs

| English | Nederlands |
| --- | --- |
| Move {0} {1} ({2})? | {0} {1} verplaatsen ({2})? |
| Files will be moved to: | De bestanden gaan naar: |
| Delete {0} {1} ({2})? | {0} {1} verwijderen ({2})? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

## Error messages

| English | Nederlands |
| --- | --- |
| Access denied | Toegang geweigerd |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows heeft InstallerClean de toegang geweigerd, dus het is gestopt. Er is niets verwijderd.<br><br>InstallerClean draaide al als administrator, dus opnieuw starten op die manier helpt niet. Windows zegt er niet bij wat er precies weigerde, dus er valt niets gerichts te proberen. |
| Couldn't read the Windows Installer records | Kon de Windows Installer-records niet lezen |
| Scan failed | Scan mislukt |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | De Windows Installer-records kwamen volledig leeg terug: geen enkel geïnstalleerd programma of geen enkele update maakt aanspraak op een installatiebestand in de cache. Dat komt op een werkende computer niet voor (zelfs een verse Windows-installatie heeft er een paar), dus of de records zijn beschadigd, of ze konden niet worden gelezen, en een scan die dit antwoord zou geloven, zou elk bestand in {InstallerFolder} ten onrechte verweesd noemen. InstallerClean is daarom gestopt. Er is niets verwijderd. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer weigerde InstallerClean te laten zien wat er geïnstalleerd is. InstallerClean draaide al als administrator, dus het nogmaals als administrator uitvoeren verandert niets. Zonder die lijst is er geen veilige manier om te bepalen welke bestanden in de cache nog nodig zijn, dus InstallerClean is gestopt. Er is niets verwijderd. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer kon InstallerClean geen leesbare lijst van de geïnstalleerde programma's geven: {0} vermeldingen op rij kwamen onleesbaar terug (laatste foutcode {1}). In plaats van met een half gelezen lijst te werken is InstallerClean gestopt. Er is niets verwijderd. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer heeft het einde van de lijst met geïnstalleerde programma's nooit gemeld: InstallerClean heeft het na {0} vermeldingen opgegeven (laatste foutcode {1}). Een lijst zonder einde is niet te vertrouwen, dus InstallerClean is gestopt. Er is niets verwijderd. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer heeft het einde van de patchlijst van één programma nooit gemeld: InstallerClean heeft het na {0} vermeldingen opgegeven (laatste foutcode {1}). Een lijst zonder einde is niet te vertrouwen, dus InstallerClean is gestopt. Er is niets verwijderd. |
| InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean kon niet genoeg van de Windows Installer-records lezen om zeker te weten wat er nog nodig is: de lijst met geïnstalleerde programma's kwam te kort terug, en dezelfde records rechtstreeks uit het register lezen leverde ook fouten op. Een bestand kon er verweesd uitzien enkel doordat het record dat het benoemt een van de onleesbare was, dus InstallerClean is gestopt. Er is niets verwijderd. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. |
| Nothing was deleted | Er is niets verwijderd |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Invalid destination | Ongeldige bestemming |
| Could not write to destination | Kon niet naar de bestemming schrijven |
| Move failed | Verplaatsen mislukt |
| Delete failed | Verwijderen mislukt |
| Setting not saved | Instelling niet opgeslagen |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | De wijziging kon niet worden opgeslagen. InstallerClean gaat bij de volgende start terug naar de vorige instelling. |
| The destination cannot be inside the Windows Installer folder. | De bestemming mag niet in de Windows Installer-map liggen. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Not enough space | Onvoldoende ruimte |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Onvoldoende ruimte op {0}<br><br>Nodig: {1}<br>Beschikbaar: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | Je hebt geen rechten om naar {0} te schrijven.<br>Probeer een map in je gebruikersprofiel of op een schijf die van jou is. |
| The path {0} is too long for Windows. Pick a shorter path. | Het pad {0} is te lang voor Windows. Kies een korter pad. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | De map {0} bestaat niet en kon niet worden aangemaakt. Controleer de stationsletter of het netwerkpad. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows kan niet schrijven naar {0}.<br>Details in {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows kan niet schrijven naar {0}. Het crashlog kon niet worden geschreven. |
| Cannot write to {0}.<br>Details in {1}. | Kan niet schrijven naar {0}.<br>Details in {1}. |
| Cannot write to {0}. The crash log could not be written. | Kan niet schrijven naar {0}. Het crashlog kon niet worden geschreven. |
| File no longer exists. | Bestand bestaat niet meer. |
| Source file is a symlink or junction; refused for safety. | Bronbestand is een symlink of junction; voor de veiligheid geweigerd. |
| This file is not directly inside the Windows Installer folder; refused for safety. | Dit bestand staat niet direct in de Windows Installer-map; voor de veiligheid geweigerd. |
| Windows refused access to this file; it was left in place. | Windows heeft de toegang tot dit bestand geweigerd; het is blijven staan. |
| Windows refused access to these files; they were left in place. | Windows heeft de toegang tot deze bestanden geweigerd; ze zijn blijven staan. |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows meldde een bestandsfout; het bestand is blijven staan. |
| Windows reported file errors; these files were left in place. | Windows meldde bestandsfouten; deze bestanden zijn blijven staan. |
| Something went wrong with this file; it was left in place. | Er ging iets mis met dit bestand; het is blijven staan. |
| Something went wrong with these files; they were left in place. | Er ging iets mis met deze bestanden; ze zijn blijven staan. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Weigert bestanden naar de Windows Installer-map te verplaatsen (bestemming: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
| Cannot write to {0}. | Kan niet schrijven naar {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Kon na 10.000 pogingen geen unieke bestandsnaam vinden voor '{0}'. |

## Update check

| English | Nederlands |
| --- | --- |
| Check for updates | Controleren op updates |
| Checking... | Controleren... |
| Up to date. | Je bent bij. |
| Version {0} is available. | Versie {0} is beschikbaar. |
| Update available | Update beschikbaar |
| You're running version {0}.<br>Version {1} is available. | Je draait versie {0}.<br>Versie {1} is beschikbaar. |
| Couldn't reach GitHub. Check your internet connection and try again. | Kon GitHub niet bereiken. Controleer je internetverbinding en probeer het opnieuw. |
| GitHub returned an error response. Try again in a few minutes. | GitHub gaf een foutmelding terug. Probeer het over een paar minuten opnieuw. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | In het antwoord van GitHub was geen release te herkennen. Probeer het later opnieuw, of open de releasepagina rechtstreeks. |
| The check timed out. Your connection to GitHub may be slow; try again. | De controle duurde te lang. Je verbinding met GitHub is misschien traag; probeer het opnieuw. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | De controle is om een onbekende reden mislukt. De details staan in crash.log, mocht je het willen melden. |

## Opening links in your browser

| English | Nederlands |
| --- | --- |
| Couldn't open your browser | Kon je browser niet openen |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean kon je browser niet openen. De link staat op je klembord, dus je kunt hem zelf plakken:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean kon je browser niet openen, en kon de link ook niet naar je klembord kopiëren. De link is:<br><br>{0} |

## Sending the summary

| English | Nederlands |
| --- | --- |
| Sending... | Verzenden... |
| Thanks! Report sent. | Bedankt! Rapport verzonden. |
| Sending failed. Try again later. | Verzenden mislukt. Probeer het later opnieuw. |
| No report to send. | Geen rapport om te verzenden. |
| Send this? | Dit versturen? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | Het gaat naar nofaff.netlify.app/api/result-log. Niets identificeert jou of je computer; het laat me alleen weten dat InstallerClean werkt en [hoeveel ruimte mensen vrijmaken]. |

## Startup and crashes

| English | Nederlands |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean is al actief. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Er is een onverwachte fout opgetreden en InstallerClean moet afsluiten.<br><br>{0}<br><br>Details weggeschreven naar:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Er is een onverwachte fout opgetreden en InstallerClean moet afsluiten.<br><br>{0}<br><br>Het crashlog kon niet worden geschreven. |
| Startup error | Opstartfout |
| Failed to start ({0}). Details written to:<br>{1} | Starten mislukt ({0}). Details weggeschreven naar:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Starten mislukt ({0}). Het crashlog kon niet worden geschreven. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

## Tooltips (hover text)

| English | Nederlands |
| --- | --- |
| It's thirsty work! | Het is dorstig werk! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Annulering aangevraagd. InstallerClean wacht tot de huidige stap een stoppunt bereikt. Dit kan enkele seconden duren bij zware I/O of een aanroep naar de MSI-database. |
| Close | Sluiten |
| A star helps other people find it. | Een ster helpt anderen het te vinden. |
| Minimise | Minimaliseren |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Dat is aan jou, maar ik zou het op prijs stellen. Er wordt een anonieme samenvatting verstuurd die me alleen laat weten of het werkt en hoeveel ruimte mensen vrijmaken. Op het volgende scherm zie je precies wat er wordt verstuurd voordat je bevestigt. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Dat is aan jou, maar ik zou het op prijs stellen. Er wordt een anonieme samenvatting verstuurd die me alleen laat weten of het werkt. Op het volgende scherm zie je precies wat er wordt verstuurd voordat je bevestigt. |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Naam van het onderwerp uit het ingesloten Authenticode-certificaat. Niet gecontroleerd via de certificaatketen. |
| Change language. The program will restart. | Taal wijzigen. Het programma start opnieuw. |

## Screen reader labels

| English | Nederlands |
| --- | --- |
| Donate | Doneren |
| Buy me a cuppa | Trakteer me op een kop koffie |
| Cancel operation | Bewerking annuleren |
| Cancel scan | Scan annuleren |
| Cancel startup scan | Opstartscan annuleren |
| Close | Sluiten |
| Close window | Venster sluiten |
| Close result and return to main window | Resultaat sluiten en terug naar het hoofdvenster |
| Leave a star on github | Geef een ster op github |
| Minimise | Minimaliseren |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Verplaatsen zet de overbodige bestanden in de gekozen doelmap. Annuleren laat ze staan waar ze staan. |
| Say thanks | Zeg bedankt |
| Send posts the report shown to No Faff. Cancel sends nothing. | Verzenden stuurt het getoonde rapport naar No Faff. Annuleren verstuurt niets. |
| Check for updates | Controleren op updates |
| Checks github's releases page for a newer version. | Kijkt op de releasepagina van github of er een nieuwere versie is. |
| Opens the readme on github in your browser. | Opent het readme-bestand op github in je browser. |
| Opens the issue tracker on github.com in your browser. | Opent de issue-tracker op github.com in je browser. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Als dit is aangevinkt, kijkt InstallerClean bij het starten op github of er een nieuwere versie is. |
| Open the release page to download the newer version, or cancel to keep the current version. | Open de releasepagina om de nieuwere versie te downloaden, of annuleer om de huidige versie te houden. |
| Opens the licence file on github.com in your browser. | Opent het licentiebestand op github.com in je browser. |
| Backup folder | Backup folder |
| Products | Producten |
| Patches | Patches |
| Product details | Productgegevens |
| Backup folder | Backup folder |
| Operation progress | Voortgang van de bewerking |
| Scan {InstallerFolder} again | {InstallerFolder} opnieuw scannen |
| Scanning progress | Voortgang van de scan |
| Startup scan progress | Voortgang van de opstartscan |
| Details, unneeded files | Details, overbodige bestanden |
| Available for cleanup. | Beschikbaar om op te ruimen. |
| Details, registered files | Details, geregistreerde bestanden |
| Read-only inventory. | Alleen-lezen overzicht. |
| Sorted by {0}, ascending | Gesorteerd op {0}, oplopend |
| Sorted by {0}, descending | Gesorteerd op {0}, aflopend |
| Scan results | Scanresultaten |
| Result details | Resultaatdetails |
| File details | Bestandsdetails |
| Product details | Product details |
| Dialog text | Dialoogtekst |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Bestanden die niet konden worden verwerkt |
| Explains this folder, and how to recover a file, in the README | Legt uit wat deze map is en hoe je een bestand terugzet, in het README-bestand |
| Report preview | Rapportvoorbeeld |
| Change language | Taal wijzigen |
| The program will restart. | Het programma start opnieuw. |

## File picker

| English | Nederlands |
| --- | --- |
| Choose destination folder for moved files | Kies de doelmap voor de verplaatste bestanden |

## Version

| English | Nederlands |
| --- | --- |
| Version {0} | Versie {0} |

## Word forms (singular and plural)

| English | Nederlands |
| --- | --- |
| file | bestand |
| files | bestanden |
| error | fout |
| errors | fouten |
| package | pakket |
| packages | pakketten |
| product | product |
| products | producten |
| patch | patch |
| patches | patches |

## Sizes and times

| English | Nederlands |
| --- | --- |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | minder dan een seconde |
| {0:F1} seconds | {0:F1} seconden |

## Command-line tool (installerclean-cli)

| English | Nederlands |
| --- | --- |
| Error: unknown argument '{0}' | Error: unknown argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Fout: onverwacht extra argument '{0}'. Staat er een spatie in je verplaatsingsmap, zet dan aanhalingstekens om het hele pad: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. |
| Cancelling... | Annuleren... |
| Cancelled. | Geannuleerd. |
| Error: unexpected failure ({0}). Details written to {1}. | Error: unexpected failure ({0}). Details written to {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Error: unexpected failure ({0}). The crash log could not be written. |
| Scanning {InstallerFolder}... | {InstallerFolder} scannen... |
| Found {0} unneeded {1} to clean up ({2}). | Found {0} unneeded {1} to clean up ({2}). |
| Found no unneeded files. | Found no unneeded files. |
| {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. | {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. |
| {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. | {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. |
| Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Deleting {0} unneeded {1}... | Deleting {0} unneeded {1}... |
| Permanently deleted {0} unneeded {1}. | Permanently deleted {0} unneeded {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Fout: geen verplaatsbestemming opgegeven. Gebruik /m PAD. (Een standaard die in de GUI is ingesteld, geldt per gebruiker en niet voor geplande taken of serviceaccounts.) |
| Error: destination cannot be inside the Windows Installer folder. | Fout: de bestemming mag niet in de Windows Installer-map liggen. |
| Error: destination must be a fully qualified path. Got: {0} | Fout: de bestemming moet een volledig gekwalificeerd pad zijn. Gekregen: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Moving {0} unneeded {1} to {2}... | Moving {0} unneeded {1} to {2}... |
| Moved {0} unneeded {1}. | Moved {0} unneeded {1}. |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Een ander InstallerClean-proces houdt de single-instance-vergrendeling vast (de GUI of een andere CLI-run). Exit 75 (tijdelijk); later opnieuw proberen kan veilig. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Let op: schrijven naar het gebeurtenislogboek is mislukt. Controleer de rechten op het logboek Toepassing of het groepsbeleid. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - {InstallerFolder} opschonen |
| Removes cached .msi and .msp files that no installed program still needs. | Removes cached .msi and .msp files that no installed program still needs. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Needs an elevated (administrator) prompt; Windows will not start it. |
| Usage: | Gebruik: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Deze hulp tonen (ook /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  De versie tonen (ook -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PAD     Naar het opgegeven pad |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. |
| Exit codes: | Afsluitcodes: |
|   0   success: the run finished with nothing left to do |   0   success: the run finished with nothing left to do |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partial: some processed, some not (a failure or a Ctrl+C part way) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  tijdelijk: iets blokkeerde de run (zie de melding) |
|   130 cancelled (Ctrl+C) |   130 geannuleerd (Ctrl+C) |
