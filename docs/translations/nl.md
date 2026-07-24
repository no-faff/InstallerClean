# InstallerClean in Français (French)

The text of InstallerClean's interface and command-line tool in English on the left, with the Dutch translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Dutch can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.fr.resx`](../../src/InstallerClean.Core/Resources/Strings.fr.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | Nederlands |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Over |
| Registered files that should not be deleted | Geregistreerde bestanden die niet mogen worden verwijderd |
| Unneeded files that are safe to delete | Overbodige bestanden die veilig kunnen worden verwijderd |
| Confirm move | Verplaatsing bevestigen |
| Confirm delete | Verwijdering bevestigen |
| Recycle Bin unavailable | Prullenbak niet beschikbaar |

## Section headings

| English | Nederlands |
| --- | --- |
| PRODUCTS | PRODUCTEN |
| PATCHES | PATCHES |
| PRODUCT DETAILS | PRODUCTGEGEVENS |
| MOVE LOCATION | LOCATIE VERPLAATSEN |
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
| Check for _updates | Controleer op _updates |
| _Close | _Sluiten |
| _Delete | _Verwijderen |
| _Delete permanently |  _Definitief verwijderen |
| _Done | _Klaar |
| Details | Details |
| _Buy me a cuppa | Trakteer me op een _koffie |
| Leave a _star on GitHub | Geef een _ster op GitHub |
| Apache 2.0 licence | Apache 2.0-licentie |
| _Move | _Verplaatsen |
| _Move instead | _Verplaats in plaats van |
| Path to folder if you Move instead of Delete | Path naar de map als je het bestand Verplaatst in plaats van Verwijdert |
| Open _release page | Open de _versie-pagina |
| _Re-scan | _Opnieuw scannen |
| _Scan again | _Scan opnieuw |
| Send report | Rapport verzenden  |
| _Send | _Verzenden |

## Field labels

| English | Nederlands |
| --- | --- |
| Reason | Reden |
| Author | Auteur |
| Application | Toepassing |
| Title | Titel |
| Subject | Onderwerp |
| Keywords | Trefwoorden |
| Signing certificate | Handtekening certificaat |
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
| Scanning installer cache folder... | De installatiecache wordt gescand... |
| Enumerating installed products... | Geïnstalleerde producten opsommen... |
| Checking registry for additional packages... | Het register controleren op extra pakketten... |
| Found {0} registered {1}. | Gevonden {0} geregistreerd {1}. |
| Scan complete ({0}) | Scan voltooid ({0}) |
| Scanning local packages... | Scannen van lokale pakketten... |
| Found {0} {1} you can safely delete. | Gevonden {0} {1}; u kunt deze veilig verwijderen. |
| Preparing destination folder... | Bestemmingsmap voorbereiden... |
| Checking the Recycle Bin... | Controleren prullenbak... |
| Moving {0} {1}... | Verplaatsen {0} {1}... |
| Deleting {0} {1}... | Verwijderen {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Bewerking geannuleerd. {0} van {1} {2} verwerkt. |
| Delete cancelled. {0} of {1} {2} processed. | Verwijdering geannuleerd. {0} van {1} {2} verwerkt. |
| Move failed ({0}). Details in {1}. | Verplaatsing mislukt ({0}). Details in {1}. |
| Move failed ({0}). The crash log could not be written. | Verplaatsing mislukt ({0}). Het crashlogboek kon niet worden opgeslagen. |
| Delete failed ({0}). Details in {1}. | Verwijderen mislukt ({0}). Details in {1}. |
| Delete failed ({0}). The crash log could not be written. | Verwijderen mislukt ({0}). Het crashlogboek kon niet worden opgeslagen. |
| Access denied. Windows refused the scan. | Toegang geweigerd. Windows heeft de scan geweigerd. |
| Scan failed: couldn't read the Windows Installer records. | Scan mislukt: de Windows-installatiegegevens konden niet worden gelezen. |
| Scan cancelled. | Scan geannuleerd. |
| Ready | Klaar |
| Scan failed ({0}). Details in {1}. | Scan mislukt ({0}). Details in {1}. |
| Scan failed ({0}). The crash log could not be written. | Scan mislukt ({0}). Het bestand crash.log kon niet worden opgeslagen. |

## Main screen text

| English | Nederlands |
| --- | --- |
| Any unneeded files below are safe to delete. | Overtollige bestanden hieronder kunnen veilig worden verwijderd. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Ze bevinden zich in C:\Windows\Installer en zijn achtergebleven toen een programma werd verwijderd ({0}), door een nieuwere patch werden vervangen ({1}) of door de uitgever werden teruggetrokken ({2}). InstallerClean geeft uitsluitend bestanden weer waarvan Windows zelf aangeeft dat ze niet meer nodig zijn. |
| Delete them to the Recycle Bin, or use Move instead to keep a backup. Putting the files back in C:\Windows\Installer returns you to exactly where you started. | Verplaats ze naar de Prullenbak, of gebruik in plaats daarvan de optie ‘Verplaatsen’ om een back-up te bewaren. Als je de bestanden terugzet in C:\Windows\Installer, staat alles weer precies zoals het was. |
| Nothing scanned yet. | Nog niets gescand. |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | Klik op ‘Opnieuw scannen’ om de map C:\Windows\Installer te doorzoeken op installatiebestanden die door geen enkel programma meer worden gebruikt. |
| These files can't be cleaned up right now. | Deze bestanden kunnen op dit moment niet worden opgeruimd. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | Er is op dit moment iets bezig met Windows Installer, meestal een Windows Update of een programma dat op de achtergrond wordt geïnstalleerd. De functies ‘Verplaatsen’ en ‘Verwijderen’ worden tijdens dit proces gepauzeerd, zodat InstallerClean de installatiecache niet aanraakt terwijl deze wordt gewijzigd. Zodra het proces is voltooid, kun je ‘Opnieuw scannen’ kiezen, waarna de functies weer beschikbaar zijn. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Op deze computer is een eerdere Windows Installer-transactie uitgesteld. Hervat of draai die installatie terug (of start Windows opnieuw op) voordat u de cache opschoont. |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows heeft een opdracht om een bestand te hernoemen in de wachtrij staan voor de volgende herstart, wat gevolgen heeft voor de cache van het installatieprogramma. Start Windows opnieuw op voordat u gaat opschonen. |
| Select a file to view details. | Selecteer een bestand om de details te bekijken. |
| Select a product to view details. | Selecteer een product om de details te bekijken. |
| No metadata available. | Geen metadata beschikbaar. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | Dit installatiebestand is verwijderd. Dit is niet door InstallerClean gebeurd; dat programma verwijdert nooit een bestand dat een programma nog nodig heeft. Iets anders heeft dit bestand verwijderd voordat je InstallerClean uitvoerde.<br><br>Het levert nu geen problemen op, en dat zal ook zo blijven totdat je het programma waartoe het behoort probeert te herstellen, bij te werken of te verwijderen. Die stap kan dan mislukken, omdat Windows naar dit bestand zoekt en het er niet is. <br><br>Om dit te verhelpen, download je het installatieprogramma van dat programma bij de maker en voer je het uit over je bestaande exemplaar heen (verwijder het programma niet eerst; het verwijderen is zelf een stap waarvoor dit bestand nodig is). Gebruik de versie die je hebt geïnstalleerd als je die kunt vinden, aangezien Windows een andere versie mogelijk afwijst. Dit herstelt het bestand meestal, en je instellingen blijven normaal gesproken ongewijzigd, maar Microsoft garandeert dit niet; hun eigen laatste redmiddel is het opnieuw installeren van het programma of van Windows zelf. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README [legt deze map uit], en hoe u een bestand kunt herstellen, in Microsofts eigen woorden. |
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
| Nothing to clean up in C:\Windows\Installer | Niets op te ruimen in C:\Windows\Installer |
| Scanned {0} {1} in {2} | Gescand {0} {1} in {2} |
| Copy them back to C:\Windows\Installer if anything ever breaks ([extremely unlikely]). | Kopieer ze terug naar C:\Windows\Installer als er ooit iets misgaat ([uiterst onwaarschijnlijk]). |
| Until then, you can restore them if anything ever breaks ([extremely unlikely]). | Tot die tijd kun je ze terugzetten indien er ooit iets misgaat ([uiterst onwaarschijnlijk]). |
| Empty it to actually reclaim the space. | Maak leeg om de ruimte effectief terug te nemen. |
| {0} freed | {0} vrijgemaakt |
| {0} cleaned up | {0} opgeruimd |
| {0} moved | {0} verplaatst |
| Nothing was moved | Er is niets verplaatst |
| Nothing was deleted | Er is niets verwijderd |
| {0} of {1} could not be moved. | {0} van {1} kon niet verplaatst worden. |
| {0} of {1} could not be moved. | {0} van {1} konden niet worden verplaatst. |
| {0} of {1} could not be deleted. | {0} van {1} kon niet verwijderd worden. |
| {0} of {1} could not be deleted. | {0} van {1} konden niet worden verwijderd. |
| {0} {1} moved to: {2} | {0} {1} verplaatst naar: {2} |
| {0} {1} moved to: {2} | {0} {1} verplaatst naar: {2} |
| {0} {1} moved to the Recycle Bin | {0} {1} verplaatst naar de Prullenbak |
| {0} {1} moved to the Recycle Bin | {0} {1} verplaatst naar de Prullenbak |
| {0} {1} kept in place, because a program started needing them again after the scan. | {0} {1} zijn behouden, omdat een programma ze na de scan weer nodig had. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | {0} {1} behouden, omdat de Windows Installer-records bij herhaling van de controle niet volledig konden worden gelezen. |
| Moved {0} of {1} {2} before you cancelled. | Verplaatst {0} van {1} {2} voordat je annuleerde. |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | Verplaatst {0} van {1} {2} naar de Prullenbak voordat je de bewerking annuleerde. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Definitief verwijderd {0} van {1} {2} voordat je de bewerking annuleerde. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} definitief verwijderd. Het is niet naar de Prullenbak verplaatst. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1}  definitief verwijderd. Ze zijn niet naar de Prullenbak verplaatst. |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Geen probleem, je kon het gerust verwijderen. InstallerClean verwijdert alleen bestanden waarvan Windows aangeeft dat ze niet meer nodig zijn, nooit bestanden die een programma nog nodig heeft. In het onwaarschijnlijke geval dat een programma door het verwijderen niet meer kan worden gerepareerd, bijgewerkt of verwijderd, wordt het bestand meestal hersteld door het programma opnieuw te installeren via de website van de maker, hoewel Microsoft dit niet garandeert. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Geen probleem, die bestanden konden veilig worden verwijderd. InstallerClean verwijdert alleen bestanden waarvan Windows aangeeft dat ze niet meer nodig zijn; nooit bestanden die een programma nog nodig heeft. In het onwaarschijnlijke geval dat een programma door het verwijderen van een bestand niet meer kan worden gerepareerd, bijgewerkt of verwijderd, wordt het bestand meestal hersteld door het programma opnieuw te installeren via de website van de fabrikant, hoewel Microsoft dit niet garandeert. |

## Recycle Bin unavailable

| English | Nederlands |
| --- | --- |
| The Recycle Bin isn't available for this drive | De Prullenbak is niet beschikbaar voor deze schijf. |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Dit {1} ({2}) is dus nog niet verwijderd. Je kunt het naar een veilige plek verplaatsen of definitief verwijderen. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Deze {0} {1} ({2}) zijn dus niet verwijderd. Je kunt ze naar een veilige plek verplaatsen of ze definitief verwijderen. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Verwijderen is veilig. InstallerClean verwijdert alleen bestanden waarvan Windows aangeeft dat ze niet meer nodig zijn; het verwijdert nooit bestanden die een programma nog nodig heeft. De Prullenbak dient bovendien alleen als extra beveiliging. In het onwaarschijnlijke geval dat een programma door het verwijderen van een bestand niet meer kan worden gerepareerd, bijgewerkt of verwijderd, wordt het bestand meestal hersteld door het programma opnieuw te installeren via de website van de fabrikant, hoewel Microsoft hiervoor geen garantie biedt. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Ze verwijderen is veilig. InstallerClean verwijdert alleen bestanden waarvan Windows aangeeft dat ze niet meer nodig zijn; het verwijdert nooit bestanden die een programma nog nodig heeft, en de Prullenbak dient slechts als extra beveiliging. In het onwaarschijnlijke geval dat een programma door het verwijderen van een bestand niet meer kan worden gerepareerd, bijgewerkt of verwijderd, wordt het bestand meestal hersteld door het programma opnieuw te installeren via de website van de fabrikant, hoewel Microsoft hiervoor geen garantie biedt. |

## Summaries and counts

| English | Nederlands |
| --- | --- |
| {0} file still needed | {0} bestand nog steeds nodig |
| {0} files still needed | {0} bestanden nog steeds nodig |
| {0} unneeded file to clean up | {0} overbodig bestand dat moet worden verwijderd |
| {0} unneeded files to clean up | {0} overbodige bestanden die moeten worden verwijderd |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} Geregistreerd bestand ontbreekt (niet verwijderd door InstallerClean). Op dit moment levert dit geen problemen op, maar een toekomstige reparatie, update of verwijdering van dat programma zou kunnen mislukken. Open ‘Details’ voor meer informatie over wat je kunt doen. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} Geregistreerde bestanden ontbreken (niet verwijderd door InstallerClean). Op dit moment levert dit geen problemen op, maar een toekomstige reparatie, update of verwijdering van dat programma zou kunnen mislukken. Open ‘Details’ voor meer informatie over wat je kunt doen. |
| {0} installed program could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | {0} Het geïnstalleerde programma kon tijdens deze scan niet worden gelezen, dus zijn verouderde patches behouden. Dit heeft geen invloed op verweesde bestanden. |
| {0} installed programs could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | {0} Geïnstalleerde programma's konden tijdens deze scan niet worden gelezen, dus zijn verouderde patches behouden. Dit heeft geen invloed op verweesde bestanden. |
| {0} of {1} {2} | {0} van {1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} verweesd, {1} vervangen, {2} verouderd ({3}) |
| {0} registered file that is still needed ({1}) | {0} {0} geregistreerd bestand dat nog steeds nodig is ({1}) |
| {0} registered files that are still needed ({1}) | {0} {0} geregistreerde bestanden die nog steeds nodig zijn ({1}) |

## Confirmation dialogs

| English | Nederlands |
| --- | --- |
| Move {0} {1} ({2})? | Verplaats {0} {1} ({2}) ? |
| Files will be moved to: | Bestanden worden verplaatst naar: |
| Delete {0} {1} ({2})? | Verwijderen {0} {1} ({2})? |
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | Bestanden worden naar de Prullenbak verplaatst. Als u back-ups wilt maken, gebruik dan in plaats daarvan de knop ‘Verplaatsen’. |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | Deze map staat op dezelfde schijf, dus het verplaatsen alleen levert geen extra ruimte op. Je krijgt de ruimte pas terug als je de bestanden uit de map verwijdert, of je kunt in plaats daarvan een map op een andere schijf kiezen. |

## Error messages

| English | Nederlands |
| --- | --- |
| Access denied | Toegang geweigerd |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows heeft InstallerClean de toegang geweigerd, waardoor het programma is gestopt. Er is niets verwijderd.<br><br>InstallerClean draaide al als beheerder, dus het opnieuw starten op die manier zal niet helpen. Windows geeft geen verdere uitleg over wat precies is geweigerd, dus er is niets specifieks dat je kunt proberen. |
| Couldn't read the Windows Installer records | Windows Installer-records konden niet worden gelezen |
| Scan failed | Scannen mislukt |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in C:\Windows\Installer orphaned. InstallerClean stopped instead. Nothing has been removed. | De Windows Installer-records bleken volledig leeg te zijn: er is geen enkel geïnstalleerd programma of update dat een in de cache opgeslagen installatiebestand claimt. Dat komt op een goed werkende computer niet voor (zelfs een gloednieuwe Windows-installatie heeft er een paar), dus ofwel zijn de records beschadigd, ofwel konden ze niet worden gelezen. Een scan die dit als antwoord zou aannemen, zou ten onrechte elk bestand in C:\Windows\Installer als ‘verweesd’ bestempelen. InstallerClean is in plaats daarvan gestopt. Er is niets verwijderd. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer stond niet toe dat InstallerClean een overzicht gaf van de geïnstalleerde programma’s. InstallerClean draaide al als beheerder, dus het nogmaals uitvoeren als beheerder zou niets veranderen. Zonder dat overzicht is er geen veilige manier om te bepalen welke bestanden in de cache nog nodig zijn, dus is InstallerClean gestopt. Er is niets verwijderd. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer kon InstallerClean geen leesbare lijst met geïnstalleerde programma's verstrekken: {0} vermeldingen op rij bleken onleesbaar (laatste foutcode {1}). In plaats van verder te werken op basis van een gedeeltelijk leesbare lijst, is InstallerClean gestopt. Er is niets verwijderd. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer heeft nooit aangegeven dat de lijst met geïnstalleerde programma's was voltooid: InstallerClean heeft het na {0} vermeldingen opgegeven (laatste foutcode {1}). Een lijst zonder einde is onbetrouwbaar, dus is InstallerClean gestopt. Er is niets verwijderd. |
| Windows Installer couldn't give InstallerClean a readable list of one program's patches: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer kon InstallerClean geen leesbare lijst met patches voor één programma verstrekken: {0} vermeldingen op rij bleken onleesbaar (laatste foutcode {1}). In plaats van verder te werken op basis van een gedeeltelijk leesbare lijst, is InstallerClean gestopt. Er is niets verwijderd. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer heeft nooit aangegeven dat de patchlijst van een programma was voltooid: InstallerClean heeft het na {0} vermeldingen opgegeven (laatste foutcode {1}). Een lijst zonder einde is onbetrouwbaar, dus is InstallerClean gestopt. Er is niets verwijderd. |
| InstallerClean couldn't square this scan with the Windows Installer records: every file Windows still lists as needed is missing from C:\Windows\Installer, while the files actually in the folder match nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean kon deze scan niet afstemmen met de Windows Installer-records: alle bestanden die Windows nog steeds als noodzakelijk vermeldt, ontbreken in C:\Windows\Installer, terwijl de bestanden die daadwerkelijk in de map staan, niet overeenkomen met iets in de records. Geen enkele echte computer ziet er zo uit, dus dit wijst op een probleem bij het lezen van de records, en niet op bestanden die je veilig kunt verwijderen. Er is niets voorgesteld om op te ruimen en er is niets verwijderd. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean kon onvoldoende gegevens uit de Windows Installer-records lezen om met zekerheid vast te stellen wat er nog nodig is: de lijst met geïnstalleerde programma’s bleek onvolledig, en ook bij het rechtstreeks uit het register lezen van dezelfde records traden er fouten op. Een bestand kon als ‘verweesd’ worden aangemerkt, louter omdat het bijbehorende record tot de onleesbare records behoorde, waardoor InstallerClean werd gestopt. Er is niets verwijderd. |
| Invalid destination | Ongeldige bestemming |
| Could not write to destination | Kon niet naar de bestemming schrijven |
| Move failed | Verplaatsing mislukt |
| Delete failed | Verwijdering mislukt |
| The destination cannot be inside the Windows Installer folder. | Bestemming mag niet in de map van Windows Installer liggen. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | De bestemming {0} verwijst naar een Windows-systeemmap. Kies een pad buiten %SystemRoot%, %ProgramFiles% en %ProgramData%. |
| Not enough space | Te weinig ruimte |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Onvoldoende ruimte bij {0}<br><br>Vereist: {1}<br>Beschikbaar: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | Je hebt geen schrijfrechten voor {0}.<br>Probeer een map in je gebruikersprofiel of op een schijf waarvan je de eigenaar bent. |
| The path {0} is too long for Windows. Pick a shorter path. | Het path {0} is te lang voor Windows. Kies een korter path. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | De map {0} bestaat niet en kon niet worden aangemaakt. Controleer de stationsletter of het netwerkpath. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows kan niet schrijven naar {0}.<br>Details in {1}.<br>Détails dans {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows kan niet naar {0} schrijven. Het crashlogboek kon niet worden opgeslagen. |
| Cannot write to {0}.<br>Details in {1}. | Kan niet schrijven naar {0}.<br>Details in {1}. |
| Cannot write to {0}. The crash log could not be written. | Windows kan niet naar {0} schrijven. Het crashlogboek kon niet worden opgeslagen. |
| File no longer exists. | Bestand bestaat niet meer. |
| Source file is a symlink or junction; refused for safety. | Bronbestand is een symlink of junction; om veiligheidsredenen geweigerd. |
| This file is not inside the Windows Installer folder; refused for safety. | Dit bestand bevindt zich niet in de Windows Installer-map; om veiligheidsredenen geweigerd. |
| Windows refused access to this file; it was left in place. | Windows heeft de toegang tot dit bestand geweigerd; het is op zijn plaats gelaten. |
| Windows refused access to these files; they were left in place. | Windows weigerde toegang tot deze bestanden; ze zijn op hun plaats gelaten. |
| This file is open or locked by another program, so nothing can move it just now. It was left in place; try again later. | Dit bestand is geopend of vergrendeld door een ander programma, dus het kan op dit moment niet worden verplaatst. Het is op zijn plaats gelaten; probeer het later nog eens. |
| These files are open or locked by another program, so nothing can move them just now. They were left in place; try again later. | Deze bestanden zijn geopend of vergrendeld door een ander programma, dus ze kunnen op dit moment niet worden verplaatst. Ze zijn op hun plaats gelaten; probeer het later nog eens. |
| Windows reported a file error; the file was left in place. | Windows heeft een bestandsfout gemeld; het bestand is op zijn plaats gebleven. |
| Windows reported file errors; these files were left in place. | Windows heeft bestandsfouten gemeld; deze bestanden zijn op hun plaats gelaten. |
| Something went wrong with this file; it was left in place. | Er is iets misgegaan met dit bestand; het is op zijn plaats achtergelaten. |
| Something went wrong with these files; they were left in place. | Er is iets misgegaan met deze bestanden; ze zijn op hun plaats achtergelaten. |
| Couldn't move this file to the Recycle Bin (error {0}), and InstallerClean can't tell you why from that code. The file was left in place. Try the Move button instead, since it doesn't use the Recycle Bin. | Dit bestand kon niet naar de Prullenbak worden verplaatst (fout {0}), en InstallerClean kan aan de hand van die foutcode niet vaststellen waarom. Het bestand is op zijn plaats gelaten. Probeer in plaats daarvan de knop ‘Verplaatsen’, aangezien deze geen gebruik maakt van de Prullenbak. |
| Windows refused access even with administrator rights (error {0}), and InstallerClean can't tell whether the problem is the file or the Recycle Bin. The file was left in place. The Move button will work if it's the Recycle Bin, but not if it's the file. | Windows weigerde toegang, zelfs met beheerdersrechten (fout {0}), en InstallerClean kan niet vaststellen of het probleem bij het bestand of bij de Prullenbak ligt. Het bestand is op zijn plaats gelaten. De knop ‘Verplaatsen’ werkt als het om de Prullenbak gaat, maar niet als het om het bestand gaat. |
| This file is open or locked by another program (error {0}), so nothing can remove it just now. It was left in place; try again later. | Dit bestand is geopend of vergrendeld door een ander programma (fout {0}), dus het kan op dit moment niet worden verwijderd. Het is op zijn plaats gelaten; probeer het later nog eens. |
| Windows deleted this file outright rather than moving it to the Recycle Bin. InstallerClean asked for the Recycle Bin, and Windows did this instead. The file is gone. | Windows heeft dit bestand direct verwijderd in plaats van het naar de Prullenbak te verplaatsen. InstallerClean vroeg om de Prullenbak, maar Windows heeft het bestand in plaats daarvan verwijderd. Het bestand is verdwenen. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Weigert bestanden naar de Windows Installer-map te verplaatsen (bestemming: {0}). |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | De locatie voor ‘Verplaatsen’ moet een volledig pad naar een map zijn, beginnend met een stationsletter of een netwerkshare (bijvoorbeeld D:\Backup of \\server\backup). InstallerClean kan deze niet gebruiken: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | De bestemming is tijdens het verplaatsen van de bestanden gewijzigd (de map is vervangen of er is een nieuwe bestemming ingesteld), waardoor InstallerClean is gestopt om te voorkomen dat er naar de verkeerde locatie zou worden geschreven. Vink {0} aan, voer vervolgens een nieuwe scan uit en probeer het opnieuw. |
| Cannot write to {0}. | Kan niet schrijven naar {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Na 10.000 pogingen kon er geen unieke bestandsnaam voor ‘{0}’ worden gevonden. |

## Update check

| English | Nederlands |
| --- | --- |
| Check for updates | Controleer op updates |
| Checking... | Controleren... |
| Up to date. | Bijgewerkt. |
| Update available | Update beschikbaar |
| You're running version {0}.<br>Version {1} is available. | Je gebruikt versie {0}.<br>Versie {1} is beschikbaar. |
| Couldn't reach GitHub. Check your internet connection and try again. | Kan GitHub niet bereiken. Controleer je internetverbinding en probeer het opnieuw. |
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | GitHub heeft een foutmelding teruggestuurd. De Releases-API is mogelijk onderworpen aan een limiet op het aantal verzoeken; probeer het over een paar minuten opnieuw. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Het antwoord van GitHub bevatte geen herkende release. Probeer het later nog eens, of ga rechtstreeks naar de pagina met releases. |
| The check timed out. Your connection to GitHub may be slow; try again. | De controle is verlopen. Je verbinding met GitHub is mogelijk traag; probeer het nog eens. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | De controle is om een onbekende reden mislukt. De details staan in crash.log, mocht je dit willen melden. |

## Opening links in your browser

| English | Nederlands |
| --- | --- |
| Couldn't open your browser | Kon je browser niet openen |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean kon je browser niet openen. De link staat op je klembord, dus je kunt deze zelf plakken:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean kon je browser niet openen en kon de link ook niet naar je klembord kopiëren. De link is:<br><br>{0} |

## Sending the summary

| English | Nederlands |
| --- | --- |
| Sending... | Verzenden... |
| Thanks! Report sent. | Bedankt! Rapport verzonden. |
| Sending failed. Try again later. | Verzenden is mislukt. Probeer het later nog eens. |
| No report to send. | Geen rapport om te versturen. |
| Send this? | Dit versturen? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | Het wordt verzonden naar nofaff.netlify.app/api/result-log. Er wordt niets verstrekt waarmee jij of je computer kan worden geïdentificeerd; het laat me alleen weten dat InstallerClean werkt en [hoeveel ruimte mensen vrijmaken]. |

## Startup and crashes

| English | Nederlands |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean is al actief. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Er is een onverwachte fout opgetreden en InstallerClean moet worden afgesloten.<br><br>{0}<br><br>Details opgeslagen in:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Er is een onverwachte fout opgetreden en InstallerClean moet worden afgesloten.<br><br>{0}<br><br>Het crashlogboek kon niet worden opgeslagen. |
| Startup error | Opstartfout |
| Failed to start ({0}). Details written to:<br>{1} | Het starten van ({0}) is mislukt. Details zijn opgeslagen in: <br>{1} |
| Failed to start ({0}). The crash log could not be written. | Het starten van ({0}) is mislukt. Het crashlogboek kon niet worden opgeslagen. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log registreert niet-afgehandelde uitzonderingen van InstallerClean.<br># Bij verhoogde rechten kunnen de uitzonderingsberichten van het framework<br># bestandspaden uit de actieve sessie bevatten (inclusief profielen van andere gebruikers<br># die door Windows Installer-query’s worden opgesomd). Foutmeldingen over netwerkstoringen<br># afkomstig van de updatecontrole of de POST-verzoek voor het resultatenlogboek kunnen<br># de bestemmings-URL en het omgezette IP-adres of proxyadres bevatten.<br># Verwijder beide soorten details voordat u dit bestand bijvoegt bij een<br># openbaar bugrapport.<br> |

## Tooltips (hover text)

| English | Nederlands |
| --- | --- |
| Donate | Doneer |
| It's thirsty work! | Het is dorstig werk! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Annulering aangevraagd. InstallerClean wacht tot de huidige stap een stoppunt bereikt. Dit kan enkele seconden duren bij intensieve I/O-activiteit of een MSI-database-aanroep. |
| Close | Sluiten |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | Geef een sterretje op GitHub, meld een probleem of plaats een bericht in de discussies. Alle feedback is welkom. |
| or report an Issue or post in Discussions. Any feedback welcome. | of meld een probleem of plaats een bericht in de discussies. Alle feedback is welkom. |
| Minimise | Minimaliseren |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Dat is aan jou, maar ik zou het op prijs stellen. Er wordt een anonieme samenvatting verstuurd waarin ik alleen te zien krijg of het werkt en hoeveel ruimte mensen vrijmaken. Op het volgende scherm kun je zien wat er wordt verstuurd voordat je het bevestigt. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Dat is aan jou, maar ik zou het op prijs stellen. Er wordt een anonieme samenvatting verzonden waarmee ik alleen kan zien of het werkt. Op het volgende scherm kun je zien wat er wordt verzonden voordat je het bevestigt. |
| Move the unneeded files to the Move location. | Verplaats de overbodige bestanden naar de Verplaats-locatie. |
| Move the unneeded files somewhere safe. You'll choose the folder next. | Zet de overbodige bestanden op een veilige plek. Vervolgens kies je de map. |
| Move the unneeded files to the Recycle Bin. | Verplaats de overbodige bestanden naar de Prullenbak. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Naam van het onderwerp uit het ingebedde Authenticode-certificaat. Niet via de certificaatketen geverifieerd. |
| Change language. The program will restart. | Taal wijzigen. Het programma wordt opnieuw opgestart. |

## Screen reader labels

| English | Nederlands |
| --- | --- |
| Donate | Doneer |
| Buy me a cuppa (About window) | Trakteer me op een kopje koffie (venster 'Over') |
| Cancel operation | Bewerking annuleren |
| Cancel scan | Scan annuleren |
| Cancel startup scan | Opstartscan annuleren |
| Close | Sluiten |
| Close window | Venster sluiten |
| Close result and return to main window | Sluit het resultaat en ga terug naar het hoofdvenster |
| Leave a star on GitHub | Geef een ster op GitHub |
| Leave a star on GitHub (About window) | Geef een ster op GitHub (venster ‘Over’) |
| Minimise | Minimaliseren |
| Move all unneeded installer files to the Move location | Verplaats alle overbodige installatiebestanden naar de Verplaats locatie |
| Move all unneeded installer files to the Recycle Bin | Verplaats alle overbodige installatiebestanden naar de Prullenbak |
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | Met ‘Verwijderen’ worden de overbodige bestanden naar de Prullenbak verplaatst. Met ‘Annuleren’ wordt het venster gesloten zonder de bestanden te verwijderen. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Met ‘Verplaatsen’ worden de overbodige bestanden naar de gekozen bestemmingsmap verplaatst. Met ‘Annuleren’ blijven ze waar ze zijn. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Kies hoe je met de overbodige bestanden wilt omgaan: verplaats ze naar een veilige locatie, verwijder ze definitief of annuleer de actie. |
| Move the unneeded files to a folder you choose | Verplaats de overbodige bestanden naar een map naar keuze |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Verwijder de overbodige bestanden definitief, aangezien de Prullenbak voor dit station niet beschikbaar is |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Berichten naar nofaff.netlify.app. Alleen tellingen en labels. Je ziet de exacte inhoud voordat je het bericht verstuurt. |
| Say thanks | Zeg bedankt |
| Send posts the report shown to No Faff. Cancel sends nothing. | Stuur het weergegeven rapport naar No Faff. Als je op ‘Annuleren’ klikt, wordt er niets verzonden. |
| Check for updates | Controleer op updates |
| Checks the GitHub releases API over HTTPS for a newer version. | Controleert via HTTPS of er een nieuwere versie beschikbaar is via de GitHub Releases API. |
| Open the release page to download the newer version, or cancel to keep the current version. | Open de releasepagina om de nieuwere versie te downloaden, of annuleer om de huidige versie te behouden. |
| Apache 2.0 licence | Apache 2.0-licentie |
| Opens the licence file on github.com in your browser. | Opent het licentiebestand op github.com in je browser. |
| Move location | Locatie verplaatsen |
| Products | Producten |
| Patches | Patches |
| Product details | Productgegevens |
| Move location | Locatie verplaatsen |
| Operation progress | Voortgang bewerking |
| Scan C:\Windows\Installer again | Scan C:\Windows\Installer opnieuw |
| Scanning progress | Voortgang scannen |
| Startup scan progress | Voortgang startup-scan |
| Details, unneeded files | Details, overbodige bestanden |
| Available for cleanup. | Beschikbaar voor opruimen. |
| Details, registered files | Details, geregistreerde bestanden |
| Read-only inventory. | Alleen-lezen inventaris. |
| Sorted by {0}, ascending | Gesorteerd op {0}, oplopend |
| Sorted by {0}, descending | Gesorteerd op {0}, aflopend |
| Scan results | Scanresultaten |
| Result details | Resultaatdetails |
| File details | Bestandsdetails |
| Dialog text | Dialoogtekst |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Bestanden die niet konden worden verwerkt |
| Explains this folder, and how to recover a file, in the README | Legt deze map uit, en hoe je een bestand kunt herstellen, in het README-bestand |
| Report preview | Voorbeeldrapport |
| Change language | Taal wijzigen |
| The program will restart. | Het programma wordt opnieuw gestart. |

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
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Fout: onverwacht extra argument ‘{0}’. Als de verplaatsingsmap een spatie bevat, zet dan het volledige pad tussen aanhalingstekens: /m “D:\My Backup” |
| Cancelling... | Annuleren... |
| Cancelled. | Geannuleerd. |
| Error: {0}. Details written to {1}. | Fout: {0}. Details opgeslagen in {1}. |
| Error: {0}. The crash log could not be written. | Fout: {0}. Het crashlogboek kon niet worden opgeslagen. |
| Scanning C:\Windows\Installer... | C:\Windows\Installer wordt gescand... |
| Found {0} {1} to clean up ({2}). | Gevonden: {0} {1} moet worden opgeruimd ({2}). |
| Nothing to do. | Niks te doen. |
| Deleting {0} {1}... | Verwijderen {0} {1}... |
| Deleted {0} {1}. | Verwijderd {0} {1}. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Fout: de Prullenbak is niet beschikbaar voor dit volume, dus er is niets verwijderd. Gebruik in plaats daarvan /m om de bestanden te verplaatsen, of schakel de Prullenbak opnieuw in en voer de opdracht opnieuw uit. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Fout: er is geen bestemming voor het verplaatsen opgegeven. Gebruik /m PATH. (Een standaardinstelling in de GUI geldt per gebruiker en is niet van toepassing op geplande taken of taken die via een serviceaccount worden uitgevoerd.) |
| Error: destination cannot be inside the Windows Installer folder. | Fout: de bestemming mag zich niet in de Windows Installer-map bevinden. |
| Error: destination must be a fully qualified path. Got: {0} | Fout: de bestemming moet een volledig gekwalificeerd pad zijn. Ontvangen: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Fout: de bestemming {0} bevindt zich in een Windows-systeemmap. Kies een pad buiten %SystemRoot%, %ProgramFiles% en %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Fout: Windows Installer wordt momenteel door iets gebruikt, meestal door Windows Update of een programma dat op de achtergrond wordt geïnstalleerd. De functies ‘Verplaatsen’ en ‘Verwijderen’ zijn geblokkeerd zolang dit proces loopt. Probeer het opnieuw zodra het proces is voltooid. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Fout: een eerdere Windows Installer-transactie is op deze computer opgeschort. Hervat of draai die installatie terug (of start Windows opnieuw op) voordat u de cache opschoont. |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Fout: er staat een bestandsbewerking in de wachtrij die na het opnieuw opstarten moet worden uitgevoerd en die betrekking heeft op de cache van het installatieprogramma ({0}). Start Windows opnieuw op om die bewerking te voltooien voordat u de cache opschoont. |
| Moving {0} {1} to {2}... | Verplaatsen van {0} {1} naar {2}... |
| Moved {0} {1}. | Verplaatst {0} {1}. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Een ander InstallerClean-proces heeft de ‘single-instance’-vergrendeling in bezit (via de GUI of een andere CLI-uitvoering). Exitcode 75 (tijdelijk); je kunt het later veilig opnieuw proberen. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Opmerking: Het schrijven naar het gebeurtenissenlogboek is mislukt. Controleer de machtigingen voor het Applicatielogboek of het Groepsbeleid. |
| InstallerClean - clean up C:\Windows\Installer | IInstallerClean - C:\Windows\Installer opschonen |
| Usage: | Gebruik: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Deze helptekst weergeven (accepteert ook /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  De versie weergeven (accepteert ook -v) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s         Alleen scannen - lijst verwijderbare bestanden |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d         Verwijder verwijderbare bestanden (Prullenbak) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m        Verplaats naar de opgeslagen standaardlocatie |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PATH    Verplaats naar het opgegeven pad |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli is een echt consoleproces en blokkeert de prompt |
| until it finishes; redirect or pipe its output as you would any | totdat deze gereed is; leid de uitvoer om zoals je dat met elke andere zou doen |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | een ander exe-bestand van de console. De GUI bevindt zich in het naastgelegen bestand InstallerClean.exe. |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | De opgeslagen standaardinstelling geldt per gebruiker; voor geplande of SYSTEM-uitvoeringen is /m PATH vereist. |
| Exit codes: | Uitgangscodes: |
|   0   success: every flagged file was processed |   0   succes: alle gemarkeerde bestanden zijn verwerkt |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   fout: niets verwerkt (ongeldige argumenten, scan mislukt, alle bestanden mislukt) |
|   2   partial: some files processed, some failed |   2   gedeeltelijk: sommige bestanden zijn verwerkt, bij andere is het mislukt |
|   75  transient: a temporary condition blocked the run (see the message) |   75  tijdelijk: een tijdelijke storing heeft de uitvoering onderbroken (zie het bericht) |
|   130 cancelled (Ctrl+C) |   130 geannuleerd (Ctrl+C) |
