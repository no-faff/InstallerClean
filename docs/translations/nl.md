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
| MOVE LOCATION | VERPLAATSLOCATIE |
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
| _Delete | _Verwijderen |
| _Delete permanently | _Definitief verwijderen |
| _Done | _Klaar |
| Details | Details |
| _Buy me a cuppa | Trakteer me op een kop _koffie |
| Leave a _star on GitHub | Geef een s_ter op GitHub |
| Apache 2.0 licence | Apache 2.0-licentie |
| _Move | Ver_plaatsen |
| _Move instead | In plaats daarvan _verplaatsen |
| Path to folder if you Move instead of Delete | Pad naar de map als je kiest voor Verplaatsen in plaats van Verwijderen |
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
| Checking the Recycle Bin... | Prullenbak controleren... |
| Moving {0} {1}... | Bezig met verplaatsen van {0} {1}... |
| Deleting {0} {1}... | Bezig met verwijderen van {0} {1}... |
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
| Any unneeded files below are safe to delete. | Overbodige bestanden hieronder kun je veilig verwijderen. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Ze staan in {InstallerFolder}, achtergebleven toen een programma werd verwijderd ({0}), een nieuwere patch een oudere verving ({1}) of de uitgever hem introk ({2}). InstallerClean toont alleen bestanden waarvan Windows zelf aangeeft dat het ermee klaar is. |
| Delete them to the Recycle Bin, or use Move instead to keep a backup. Putting the files back in {InstallerFolder} returns you to exactly where you started. | Verwijder ze naar de Prullenbak, of gebruik Verplaatsen om een back-up te houden. Zet je de bestanden terug in {InstallerFolder}, dan is alles weer precies zoals het was. |
| Nothing scanned yet. | Nog niets gescand. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Klik op Opnieuw scannen om {InstallerFolder} te doorzoeken op installatiebestanden die geen enkel programma nog nodig heeft. |
| These files can't be cleaned up right now. | Deze bestanden kunnen nu niet worden opgeruimd. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | Er is op dit moment iets met Windows Installer bezig, meestal een Windows-update of een programma dat op de achtergrond wordt geïnstalleerd. Verplaatsen en Verwijderen staan zolang stil, zodat InstallerClean niet aan de installatiecache komt terwijl die verandert. Zodra het klaar is: opnieuw scannen en ze zijn er weer. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Een eerdere Windows Installer-transactie is op deze computer opgeschort. Hervat die installatie of draai haar terug (of herstart Windows) voordat je de cache opschoont. |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows heeft voor de volgende herstart een bestandshernoeming in de wachtrij staan die de installatiecache raakt. Herstart Windows voordat je opschoont. |
| Select a file to view details. | Selecteer een bestand om de details te zien. |
| Select a product to view details. | Selecteer een product om de details te zien. |
| No metadata available. | Geen metadata beschikbaar. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | Dit installatiebestand is verwijderd. Dat heeft InstallerClean niet gedaan, het verwijdert nooit een bestand dat een programma nog nodig heeft; iets anders heeft dit bestand verwijderd voordat je InstallerClean startte.<br><br>Het geeft nu geen problemen, en dat blijft zo tot de dag dat je het programma waar het bij hoort probeert te herstellen, bij te werken of te verwijderen. Die stap kan dan mislukken, omdat Windows dit bestand zoekt en het er niet is.<br><br>Om het te proberen te herstellen: download de installer van dat programma bij de maker en voer hem uit over je bestaande installatie heen (niet eerst deïnstalleren; deïnstalleren is zelf een stap die dit bestand nodig heeft). Gebruik zo mogelijk de versie die je nu hebt geïnstalleerd, want Windows kan een andere weigeren. Meestal is het bestand daarmee terug en blijven je instellingen ongemoeid, maar Microsoft garandeert het niet; zijn eigen laatste redmiddel is het programma opnieuw installeren, of Windows zelf. |
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
| Copy them back to {InstallerFolder} if anything ever breaks ([extremely unlikely]). | Kopieer ze terug naar {InstallerFolder} als er ooit iets stukgaat ([uiterst onwaarschijnlijk]). |
| Until then, you can restore them if anything ever breaks ([extremely unlikely]). | Tot die tijd kun je ze terugzetten als er ooit iets stukgaat ([uiterst onwaarschijnlijk]). |
| Empty it to actually reclaim the space. | Leeg hem om de ruimte echt terug te krijgen. |
| {0} freed | {0} vrijgemaakt |
| {0} cleaned up | {0} opgeruimd |
| {0} moved | {0} verplaatst |
| Nothing was moved | Er is niets verplaatst |
| Nothing was deleted | Er is niets verwijderd |
| {0} of {1} could not be moved. | {0} van {1} kon niet worden verplaatst. |
| {0} of {1} could not be moved. | {0} van {1} konden niet worden verplaatst. |
| {0} of {1} could not be deleted. | {0} van {1} kon niet worden verwijderd. |
| {0} of {1} could not be deleted. | {0} van {1} konden niet worden verwijderd. |
| {0} {1} moved to: {2} | {0} {1} verplaatst naar: {2} |
| {0} {1} moved to: {2} | {0} {1} verplaatst naar: {2} |
| {0} {1} moved to the Recycle Bin | {0} {1} naar de Prullenbak verplaatst |
| {0} {1} moved to the Recycle Bin | {0} {1} naar de Prullenbak verplaatst |
| {0} {1} kept in place, because a program went back to needing what the scan flagged. | {0} {1} behouden, omdat een programma ze na de scan weer nodig bleek te hebben. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | {0} {1} behouden, omdat de Windows Installer-records bij de herhaalde controle niet volledig konden worden gelezen. |
| Moved {0} of {1} {2} before you cancelled. | {0} van {1} {2} verplaatst voordat je annuleerde. |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | {0} van {1} {2} naar de Prullenbak verplaatst voordat je annuleerde. |
| Permanently deleted {0} of {1} {2} before you cancelled. | {0} van {1} {2} definitief verwijderd voordat je annuleerde. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} definitief verwijderd. Het is niet in de Prullenbak beland. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1} definitief verwijderd. Ze zijn niet in de Prullenbak beland. |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Geen zorgen, het kon veilig weg. InstallerClean ruimt alleen bestanden op waarvan Windows aangeeft dat het ermee klaar is, nooit een bestand dat een programma nog nodig heeft. In het onwaarschijnlijke geval dat een programma door een verwijdering ooit niet meer te herstellen, bij te werken of te verwijderen is, zet het programma opnieuw installeren via de maker het bestand meestal terug, al garandeert Microsoft dat niet. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Geen zorgen, ze konden veilig weg. InstallerClean ruimt alleen bestanden op waarvan Windows aangeeft dat het ermee klaar is, nooit een bestand dat een programma nog nodig heeft. In het onwaarschijnlijke geval dat een programma door een verwijdering ooit niet meer te herstellen, bij te werken of te verwijderen is, zet het programma opnieuw installeren via de maker het bestand meestal terug, al garandeert Microsoft dat niet. |
| Glad to help. There's a tip jar if you're feeling kind. | Graag gedaan. Er staat een fooienpot klaar, mocht je je gul voelen. |

## Recycle Bin unavailable

| English | Nederlands |
| --- | --- |
| The Recycle Bin isn't available for this drive | De Prullenbak is niet beschikbaar voor deze schijf |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Dit {1} ({2}) is dus niet verwijderd. Je kunt het naar een veilige plek verplaatsen of het definitief verwijderen. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Deze {0} {1} ({2}) zijn dus niet verwijderd. Je kunt ze naar een veilige plek verplaatsen of ze definitief verwijderen. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Het verwijderen is veilig. InstallerClean ruimt alleen bestanden op waarvan Windows aangeeft dat het ermee klaar is, nooit een bestand dat een programma nog nodig heeft, en de Prullenbak is alleen een extra vangnet. In het onwaarschijnlijke geval dat een programma door een verwijdering ooit niet meer te herstellen, bij te werken of te verwijderen is, zet het programma opnieuw installeren via de maker het bestand meestal terug, al garandeert Microsoft dat niet. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Ze verwijderen is veilig. InstallerClean ruimt alleen bestanden op waarvan Windows aangeeft dat het ermee klaar is, nooit een bestand dat een programma nog nodig heeft, en de Prullenbak is alleen een extra vangnet. In het onwaarschijnlijke geval dat een programma door een verwijdering ooit niet meer te herstellen, bij te werken of te verwijderen is, zet het programma opnieuw installeren via de maker het bestand meestal terug, al garandeert Microsoft dat niet. |

## Summaries and counts

| English | Nederlands |
| --- | --- |
| {0} file still needed | {0} bestand nog nodig |
| {0} files still needed | {0} bestanden nog nodig |
| {0} unneeded file to clean up | {0} overbodig bestand om op te ruimen |
| {0} unneeded files to clean up | {0} overbodige bestanden om op te ruimen |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} geregistreerd bestand ontbreekt (niet door InstallerClean verwijderd). Nu geen probleem, maar een toekomstige herstel-, update- of verwijderactie van dat programma kan mislukken. Open Details voor wat je kunt doen. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} geregistreerde bestanden ontbreken (niet door InstallerClean verwijderd). Nu geen probleem, maar een toekomstige herstel-, update- of verwijderactie van die programma's kan mislukken. Open Details voor wat je kunt doen. |
| {0} installed program could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | {0} geïnstalleerd programma kon tijdens deze scan niet worden gelezen; vervangen patches zijn daarom behouden. Voor verweesde bestanden maakt dit niets uit. |
| {0} installed programs could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | {0} geïnstalleerde programma's konden tijdens deze scan niet worden gelezen; vervangen patches zijn daarom behouden. Voor verweesde bestanden maakt dit niets uit. |
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
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | De bestanden gaan naar de Prullenbak. Wil je back-ups, gebruik dan de knop Verplaatsen. |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | Deze map staat op dezelfde schijf, dus het verplaatsen maakt op zichzelf geen ruimte vrij. De ruimte komt terug zodra je de bestanden daaruit verwijdert, of kies een map op een andere schijf. |

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
| InstallerClean couldn't square this scan with the Windows Installer records: every file Windows still lists as needed is missing from {InstallerFolder}, while the files actually in the folder match nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean kreeg deze scan niet te rijmen met de Windows Installer-records: elk bestand dat Windows nog als nodig aanmerkt, ontbreekt in {InstallerFolder}, terwijl de bestanden die echt in de map staan met niets in de records overeenkomen. Zo ziet geen echte computer eruit, dus dit wijst op een probleem met het lezen van de records, niet op bestanden die je veilig kunt verwijderen. Er is niets voor opruiming aangeboden en er is niets verwijderd. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean kon niet genoeg van de Windows Installer-records lezen om zeker te weten wat er nog nodig is: de lijst met geïnstalleerde programma's kwam te kort terug, en dezelfde records rechtstreeks uit het register lezen leverde ook fouten op. Een bestand kon er verweesd uitzien enkel doordat het record dat het benoemt een van de onleesbare was, dus InstallerClean is gestopt. Er is niets verwijderd. |
| Invalid destination | Ongeldige bestemming |
| Could not write to destination | Kon niet naar de bestemming schrijven |
| Move failed | Verplaatsen mislukt |
| Delete failed | Verwijderen mislukt |
| Setting not saved | Instelling niet opgeslagen |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | De wijziging kon niet worden opgeslagen. InstallerClean gaat bij de volgende start terug naar de vorige instelling. |
| The destination cannot be inside the Windows Installer folder. | De bestemming mag niet in de Windows Installer-map liggen. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | De bestemming {0} komt uit onder een Windows-systeemmap. Kies een pad buiten %SystemRoot%, %ProgramFiles% en %ProgramData%. |
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
| This file is open or locked by another program, so nothing can move it just now. It was left in place; try again later. | Dit bestand is geopend of vergrendeld door een ander programma, dus niets kan het nu verplaatsen. Het is blijven staan; probeer het later opnieuw. |
| These files are open or locked by another program, so nothing can move them just now. They were left in place; try again later. | Deze bestanden zijn geopend of vergrendeld door een ander programma, dus niets kan ze nu verplaatsen. Ze zijn blijven staan; probeer het later opnieuw. |
| Windows reported a file error; the file was left in place. | Windows meldde een bestandsfout; het bestand is blijven staan. |
| Windows reported file errors; these files were left in place. | Windows meldde bestandsfouten; deze bestanden zijn blijven staan. |
| Something went wrong with this file; it was left in place. | Er ging iets mis met dit bestand; het is blijven staan. |
| Something went wrong with these files; they were left in place. | Er ging iets mis met deze bestanden; ze zijn blijven staan. |
| Couldn't move this file to the Recycle Bin (error {0}), and InstallerClean can't tell you why from that code. The file was left in place. Try the Move button instead, since it doesn't use the Recycle Bin. | Kon dit bestand niet naar de Prullenbak verplaatsen (fout {0}), en aan die code kan InstallerClean niet zien waarom. Het bestand is blijven staan. Probeer de knop Verplaatsen; die gebruikt de Prullenbak niet. |
| Windows refused access even with administrator rights (error {0}), and InstallerClean can't tell whether the problem is the file or the Recycle Bin. The file was left in place. The Move button will work if it's the Recycle Bin, but not if it's the file. | Windows weigerde de toegang, zelfs met administratorrechten (fout {0}), en InstallerClean kan niet zien of het aan het bestand of aan de Prullenbak ligt. Het bestand is blijven staan. De knop Verplaatsen werkt als het aan de Prullenbak ligt, maar niet als het aan het bestand ligt. |
| This file is open or locked by another program (error {0}), so nothing can remove it just now. It was left in place; try again later. | Dit bestand is geopend of vergrendeld door een ander programma (fout {0}), dus niets kan het nu weghalen. Het is blijven staan; probeer het later opnieuw. |
| Windows deleted this file outright rather than moving it to the Recycle Bin. InstallerClean asked for the Recycle Bin, and Windows did this instead. The file is gone. | Windows heeft dit bestand meteen definitief verwijderd in plaats van het naar de Prullenbak te verplaatsen. InstallerClean vroeg om de Prullenbak, en Windows deed dit in plaats daarvan. Het bestand is weg. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Weigert bestanden naar de Windows Installer-map te verplaatsen (bestemming: {0}). |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | De verplaatslocatie moet een volledig pad naar een map zijn, beginnend met een stationsletter of een netwerkshare (bijvoorbeeld D:\Backup of \\server\backup). InstallerClean kan hier niets mee: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | De verplaatslocatie veranderde terwijl de bestanden werden verplaatst (iets heeft de map vervangen of omgeleid), dus InstallerClean is gestopt om niet op de verkeerde plek te schrijven. Controleer {0}, scan opnieuw en probeer het nog eens. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log legt onafgevangen fouten van InstallerClean vast.<br># Met verhoogde rechten kunnen de foutmeldingen van het framework<br># bestandspaden uit de lopende sessie bevatten (ook uit profielen van<br># andere gebruikers die Windows Installer-query's doorlopen). Meldingen<br># over netwerkfouten van de updatecontrole of de rapport-POST kunnen de<br># bestemmings-URL en het opgeloste IP-adres of proxyadres bevatten.<br># Haal beide soorten details weg voordat je dit bestand bij een openbaar<br># bugrapport voegt.<br> |

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
| Move the unneeded files to the Move location. | Verplaatst de overbodige bestanden naar de verplaatslocatie. |
| Move the unneeded files somewhere safe. You'll choose the folder next. | Zet de overbodige bestanden op een veilige plek. De map kies je hierna. |
| Move the unneeded files to the Recycle Bin. | Verplaatst de overbodige bestanden naar de Prullenbak. |
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
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | Verwijderen verplaatst de overbodige bestanden naar de Prullenbak. Annuleren sluit het venster zonder te verwijderen. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Verplaatsen zet de overbodige bestanden in de gekozen doelmap. Annuleren laat ze staan waar ze staan. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Kies wat er met de overbodige bestanden moet gebeuren: verplaats ze naar een veilige plek, verwijder ze definitief of annuleer. |
| Move the unneeded files to a folder you choose | De overbodige bestanden verplaatsen naar een map die je zelf kiest |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | De overbodige bestanden definitief verwijderen, omdat de Prullenbak voor deze schijf niet beschikbaar is |
| Say thanks | Zeg bedankt |
| Send posts the report shown to No Faff. Cancel sends nothing. | Verzenden stuurt het getoonde rapport naar No Faff. Annuleren verstuurt niets. |
| Check for updates | Controleren op updates |
| Checks github's releases page for a newer version. | Kijkt op de releasepagina van github of er een nieuwere versie is. |
| Opens the readme on github in your browser. | Opent het readme-bestand op github in je browser. |
| Opens the issue tracker on github.com in your browser. | Opent de issue-tracker op github.com in je browser. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Als dit is aangevinkt, kijkt InstallerClean bij het starten op github of er een nieuwere versie is. |
| Open the release page to download the newer version, or cancel to keep the current version. | Open de releasepagina om de nieuwere versie te downloaden, of annuleer om de huidige versie te houden. |
| Opens the licence file on github.com in your browser. | Opent het licentiebestand op github.com in je browser. |
| Move location | Verplaatslocatie |
| Products | Producten |
| Patches | Patches |
| Product details | Productgegevens |
| Move location | Verplaatslocatie |
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
| Unknown argument: '{0}' | Onbekend argument: '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Fout: onverwacht extra argument '{0}'. Staat er een spatie in je verplaatsingsmap, zet dan aanhalingstekens om het hele pad: /m "D:\My Backup" |
| Cancelling... | Annuleren... |
| Cancelled. | Geannuleerd. |
| Error: {0}. Details written to {1}. | Fout: {0}. Details weggeschreven naar {1}. |
| Error: {0}. The crash log could not be written. | Fout: {0}. Het crashlog kon niet worden geschreven. |
| Scanning {InstallerFolder}... | {InstallerFolder} scannen... |
| Found {0} {1} to clean up ({2}). | {0} {1} gevonden om op te ruimen ({2}). |
| Nothing to do. | Niets te doen. |
| Deleting {0} {1}... | Bezig met verwijderen van {0} {1}... |
| Deleted {0} {1}. | {0} {1} verwijderd. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Fout: de Prullenbak is niet beschikbaar voor dit volume, dus er is niets verwijderd. Gebruik /m om de bestanden te verplaatsen, of schakel de Prullenbak weer in en voer de opdracht opnieuw uit. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Fout: geen verplaatsbestemming opgegeven. Gebruik /m PAD. (Een standaard die in de GUI is ingesteld, geldt per gebruiker en niet voor geplande taken of serviceaccounts.) |
| Error: destination cannot be inside the Windows Installer folder. | Fout: de bestemming mag niet in de Windows Installer-map liggen. |
| Error: destination must be a fully qualified path. Got: {0} | Fout: de bestemming moet een volledig gekwalificeerd pad zijn. Gekregen: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Fout: de bestemming {0} komt uit onder een Windows-systeemmap. Kies een pad buiten %SystemRoot%, %ProgramFiles% en %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Fout: er is op dit moment iets met Windows Installer bezig, meestal een Windows-update of een programma dat op de achtergrond wordt geïnstalleerd. Verplaatsen en Verwijderen zijn zolang geblokkeerd. Probeer het opnieuw zodra het klaar is. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Fout: een eerdere Windows Installer-transactie is op deze computer opgeschort. Hervat die installatie of draai haar terug (of herstart Windows) voordat je de cache opschoont. |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Fout: een bestandsbewerking die na de herstart in de wachtrij staat, richt zich op de installatiecache ({0}). Herstart Windows om die bewerking te voltooien voordat je opschoont. |
| Moving {0} {1} to {2}... | Bezig met verplaatsen van {0} {1} naar {2}... |
| Moved {0} {1}. | {0} {1} verplaatst. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Een ander InstallerClean-proces houdt de single-instance-vergrendeling vast (de GUI of een andere CLI-run). Exit 75 (tijdelijk); later opnieuw proberen kan veilig. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Let op: schrijven naar het gebeurtenislogboek is mislukt. Controleer de rechten op het logboek Toepassing of het groepsbeleid. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - {InstallerFolder} opschonen |
| Usage: | Gebruik: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Deze hulp tonen (ook /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  De versie tonen (ook -v) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s         Alleen scannen - verwijderbare bestanden |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d         Verwijderbare bestanden (Prullenbak) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m         Naar de opgeslagen standaardlocatie |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PAD     Naar het opgegeven pad |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli is een echt consoleproces en blokkeert de prompt |
| until it finishes; redirect or pipe its output as you would any | tot het klaar is; leid de uitvoer om of door (pipe) zoals bij elke |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | andere console-exe. De GUI zit ernaast, in InstallerClean.exe. |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | De opgeslagen standaard is per gebruiker; geplande of SYSTEM-runs: /m PAD. |
| Exit codes: | Afsluitcodes: |
|   0   success: every flagged file was processed |   0   gelukt: elk aangemerkt bestand is verwerkt |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   mislukt: niets verwerkt (foute argumenten, scan of alle bestanden) |
|   2   partial: some files processed, some failed |   2   gedeeltelijk: sommige bestanden verwerkt, sommige mislukt |
|   75  transient: a temporary condition blocked the run (see the message) |   75  tijdelijk: iets blokkeerde de run (zie de melding) |
|   130 cancelled (Ctrl+C) |   130 geannuleerd (Ctrl+C) |
