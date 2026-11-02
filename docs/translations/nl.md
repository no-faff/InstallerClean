# InstallerClean in Nederlands (Dutch)

The text of InstallerClean's interface and command-line tool in English on the left, with the Dutch translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Dutch can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.nl.resx`](../../src/InstallerClean.Core/Resources/Strings.nl.resx), so do not edit it by hand. The Dutch translation itself lives in [`gen-strings-nl.mjs`](../../scripts/translations/gen-strings-nl.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Nederlands |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Over |
| Files left alone | Ongemoeid gelaten bestanden |
| Unneeded files that are safe to delete | Overbodige bestanden die veilig kunnen worden verwijderd |
| The anonymous report | Het anonieme rapport |
| InstallerClean couldn't read the report. | InstallerClean kon het rapport niet lezen. |

## Section headings

| English | Nederlands |
| --- | --- |
| PATCHES | PATCHES |
| PRODUCT DETAILS | PRODUCTGEGEVENS |
| BACKUP FOLDER | BACK-UPMAP |
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
| Carry on _without it | Doorgaan _zonder te wachten |
| Check for _updates | Controleren op _updates |
| _Close | _Sluiten |
| _Close without donating | _Sluiten zonder te doneren |
| _Delete permanently | _Definitief verwijderen |
| _Done | _Klaar |
| Details | Details |
| _Donate | _Doneren |
| _Donate $3 | $ 3 _doneren |
| Donate | Doneren |
| Leave a _star on GitHub | Geef een s_ter op GitHub |
| Apache 2.0 licence | Apache 2.0-licentie |
| _Move | Ver_plaatsen |
| Path to folder if you move rather than delete. | Pad naar de map als je verplaatst in plaats van verwijdert. |
| _Re-scan | Opnieuw _scannen |

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
| (no program) | (geen programma) |
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
| Checking registry for additional packages... | Register controleren op extra pakketten... |
| Matching files against installed programs... | Bestanden vergelijken met de geïnstalleerde programma's... |
| {0} of {1} | {0} van {1} |
| Checking remaining files... | Overige bestanden controleren... |
| Waiting for drive {0} to respond... | Wachten tot schijf {0} reageert... |
| Waiting for {0} to respond... | Wachten tot {0} reageert... |
| Any file still to be checked against it will be left alone. | Elk bestand dat er nog mee vergeleken moet worden, wordt ongemoeid gelaten. |
| Scan complete ({0}) | Scan voltooid ({0}) |
| Found {0} {1} you can safely delete. | Je kunt {0} {1} veilig verwijderen. |
| Preparing destination folder... | Doelmap voorbereiden... |
| Moving unneeded files... | Overbodige bestanden verplaatsen... |
| Deleting unneeded files... | Overbodige bestanden verwijderen... |
| Move cancelled. {0} of {1} {2} processed. | Verplaatsen geannuleerd. {0} van {1} {2} verwerkt. |
| Delete cancelled. {0} of {1} {2} processed. | Verwijderen geannuleerd. {0} van {1} {2} verwerkt. |
| {0}. Details are in {1}. | {0}. Details in {1}. |
| {0}. The crash log could not be written. | {0}. Het crashlog kon niet worden weggeschreven. |
| {0}. Details are in {1}. | {0}. Details in {1}. |
| {0}. The crash log could not be written. | {0}. Het crashlog kon niet worden weggeschreven. |
| Access denied. Windows refused the scan. | Toegang geweigerd. Windows heeft de scan geweigerd. |
| Scan stopped. | Scan gestopt. |
| Scan cancelled. | Scan geannuleerd. |
| Ready | Klaar |
| Scan failed ({0}). Details in {1}. | Scan mislukt ({0}). Details in {1}. |
| Scan failed ({0}). The crash log could not be written. | Scan mislukt ({0}). Het crashlog kon niet worden weggeschreven. |

## Main screen text

| English | Nederlands |
| --- | --- |
| Any unneeded files below are safe to delete. | Overbodige bestanden hieronder zijn veilig te verwijderen. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Ze staan in {InstallerFolder}. InstallerClean vraagt Windows naar elk geïnstalleerd programma: een bestand komt in de lijst wanneer geen enkel programma het opeist ({0}), of wanneer een nieuwere patch het heeft vervangen en geen enkel programma erop kan terugvallen ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Verplaats ze naar een back-upmap die je zelf kiest en verwijder die map zodra je zeker weet dat je programma's nog gewoon bijwerken en verwijderen. Terugzetten in {InstallerFolder} herstelt alles. Of verwijder ze nu definitief. |
| Nothing scanned yet. | Nog niets gescand. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Klik op Opnieuw scannen om {InstallerFolder} te doorzoeken op installatiebestanden die geen enkel programma nog nodig heeft. |
| Nothing to show until the next scan. | Niets te tonen tot de volgende scan. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | De scan die na Verplaatsen of Verwijderen draait, is niet afgerond, dus er is geen lijst om te tonen. Klik op Opnieuw scannen om {InstallerFolder} opnieuw te scannen. |
| What stopped it is recorded in {0}. | Wat de scan heeft gestopt, is vastgelegd in {0}. |
| These files can't be cleaned up right now. | Deze bestanden kunnen op dit moment niet worden opgeruimd. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Er is op dit moment iets bezig met Windows Installer, zoals een Windows-update of een programma dat op de achtergrond installeert. Verplaatsen en Verwijderen staan stil zolang dat loopt, zodat InstallerClean {InstallerFolder} niet aanraakt terwijl daar iets verandert. Zodra het klaar is, klik je op Opnieuw scannen en komen ze terug. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows weigerde InstallerClean toestemming om te controleren of Windows Installer bezig is, dus Verplaatsen en Verwijderen staan stil en InstallerClean raakt {InstallerFolder} niet aan. Start Windows opnieuw op en kies Opnieuw scannen. Als Windows het dan nog steeds weigert, is dit geen machine die InstallerClean kan opruimen. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean kon niet controleren of Windows Installer bezig is, dus Verplaatsen en Verwijderen staan stil en InstallerClean raakt {InstallerFolder} niet aan. Start Windows opnieuw op en kies Opnieuw scannen. Als het dan nog steeds niet kan controleren, is dit geen machine die InstallerClean kan opruimen. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean kon niet controleren of Windows Installer bezig is, omdat Windows een fout gaf: {0} (fout {1}). Verplaatsen en Verwijderen staan stil en InstallerClean raakt {InstallerFolder} niet aan. Start Windows opnieuw op en kies Opnieuw scannen. Als het dan nog steeds niet kan controleren, is dit geen machine die InstallerClean kan opruimen. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Een eerdere Windows Installer-transactie is op deze computer opgeschort. Hervat die installatie of draai haar terug (of herstart Windows) voordat je {InstallerFolder} opruimt. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows heeft voor de volgende herstart een bestandshernoeming in de wachtrij staan die {InstallerFolder} raakt. Herstart Windows voordat je opruimt. |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | Er staat een bestandsbewerking in de wachtrij voor de volgende herstart en InstallerClean kan niet zien welke bestanden die noemt, dus kan het niet uitsluiten dat ze in {InstallerFolder} staan. Start Windows opnieuw op voordat je opruimt. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean kon een van de Windows-instellingen die het controleert voordat het {InstallerFolder} aanraakt niet lezen, en kan dus niet zien of er een installatiebewerking loopt of op een herstart wacht. Start Windows opnieuw op en kies Opnieuw scannen. Als de instelling dan nog steeds niet te lezen is, is dit geen machine die InstallerClean kan opruimen. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer heeft iets lopen, dus Verplaatsen en Verwijderen staan stil. InstallerClean raakt {InstallerFolder} niet aan terwijl daar iets verandert. Zodra het klaar is, klik je op Opnieuw scannen en komen ze terug. |
| Select a file to view details. | Selecteer een bestand om de details te bekijken. |
| Select a product to view details. | Selecteer een product om de details te bekijken. |
| No metadata available. | Geen metadata beschikbaar. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Dit installatiebestand ontbreekt. Dat levert nu geen problemen op, en dat blijft zo tot de dag waarop je het bijbehorende programma wilt bijwerken of verwijderen. Die stap kan dan mislukken, omdat Windows dit bestand zoekt en het er niet is.<br><br>Om het terug te zetten heb je het installatieprogramma nodig van de versie die je al hebt. Haal het bij de maker van het programma en voer het uit over je bestaande installatie heen. Een nieuwere versie volstaat niet: die moet eerst verwijderen wat je hebt, en juist die stap heeft dit bestand nodig. Eerst verwijderen werkt om dezelfde reden ook niet. Dit zou het bestand moeten herstellen en je instellingen ongemoeid moeten laten, maar Microsoft geeft daar geen garantie op. |
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
| Nothing offered on this PC | Niets aangeboden op deze pc |
| One file ({2}) was held back because Windows shows it was changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check it again. | Eén bestand ({2}) is achtergehouden omdat Windows laat zien dat het is gewijzigd nadat InstallerClean met scannen was begonnen. Als er een programma aan het installeren of bijwerken was, scan dan opnieuw zodra het klaar is, dan controleert InstallerClean het nog een keer. |
| {0} {1} ({2}) were held back because Windows shows they were changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check them again. | {0} {1} ({2}) zijn achtergehouden omdat Windows laat zien dat ze zijn gewijzigd nadat InstallerClean met scannen was begonnen. Als er een programma aan het installeren of bijwerken was, scan dan opnieuw zodra het klaar is, dan controleert InstallerClean ze nog een keer. |
| Delete that folder when you're satisfied all is well. | Verwijder die map zodra je zeker weet dat alles goed is. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | Verwijder die map zodra je zeker weet dat alles goed is. Pas dan komt de ruimte echt vrij. |
| {0} freed | {0} vrijgemaakt |
| {0} moved | {0} verplaatst |
| Nothing was moved | Er is niets verplaatst |
| Nothing was deleted | Er is niets verwijderd |
| {0} file could not be moved. | {0} bestand kon niet worden verplaatst. |
| {0} files could not be moved. | {0} bestanden konden niet worden verplaatst. |
| {0} file could not be deleted. | {0} bestand kon niet worden verwijderd. |
| {0} files could not be deleted. | {0} bestanden konden niet worden verwijderd. |
| {0} {1} moved to: {2} | {0} {1} verplaatst naar: {2} |
| {0} {1} moved to: {2} | {0} {1} verplaatst naar: {2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | {0} bestand achtergehouden. De scan noemde het overbodig. De laatste controle kon dat niet bevestigen. |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | {0} bestanden achtergehouden. De scan noemde ze overbodig. De laatste controle kon dat niet bevestigen. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} behouden, omdat Windows een registratie heeft van het programma dat erin genoemd wordt. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} behouden, omdat InstallerClean geen programma kon vinden dat erin genoemd wordt. |
| Moved {0} of {1} {2} to {3} before you cancelled. | {0} van {1} {2} naar {3} verplaatst voordat je annuleerde. |
| Permanently deleted {0} of {1} {2} before you cancelled. | {0} van {1} {2} definitief verwijderd voordat je annuleerde. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | Het is eenvoudig ongedaan te maken. Verplaats ze terug naar {InstallerFolder} en alles is weer zoals het was. |
| {0} {1} permanently deleted | {0} {1} definitief verwijderd |
| {0} {1} permanently deleted | {0} {1} definitief verwijderd |
| Any small donation is much appreciated | Elke kleine donatie wordt zeer gewaardeerd |
| Send _anonymous report | _Anoniem rapport versturen |
| The report adds your result to [this chart] on GitHub, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Het rapport voegt je resultaat toe aan [deze grafiek] op GitHub, die laat zien hoeveel ruimte mensen met InstallerClean vrijmaken. Het bevat ook deze technische gegevens, die me helpen eventuele bugs op te sporen: |
| How much space was freed, and how many files were moved or deleted | Hoeveel ruimte er is vrijgemaakt en hoeveel bestanden er zijn verplaatst of verwijderd |
| Whether a Move went to the same drive, another drive, a removable drive or a network share | Of Verplaatsen naar dezelfde schijf, een andere schijf, een verwisselbare schijf of een netwerkshare ging |
| How long the scan and the Move or Delete took | Hoe lang de scan en Verplaatsen of Verwijderen duurden |
| How many installer files there are of each kind, and how much space they take | Hoeveel installatiebestanden er van elk soort zijn en hoeveel ruimte ze innemen |
| How many files were left alone, and why | Hoeveel bestanden ongemoeid zijn gelaten en waarom |
| How often InstallerClean waited for a slow drive or network share, and how many it stopped waiting for | Hoe vaak InstallerClean op een trage schijf of netwerkshare wachtte, en op hoeveel het is gestopt met wachten |
| How many programs and patches Windows has a record of | Van hoeveel programma's en patches Windows een registratie heeft |
| Whether Windows still makes old-style short file names | Of Windows nog ouderwetse korte bestandsnamen aanmaakt |
| Whether Windows is 10 or 11 and x64 or Arm, and which language and country or region it's set to | Of Windows 10 of 11 is en x64 of Arm, en op welke taal en welk land of welke regio het is ingesteld |
| InstallerClean's version, how you got it (setup, portable, slim, Scoop, the command-line tool on its own, built from source, or not known), whether the report was sent from InstallerClean or the command-line tool, and the language it was showing | De versie van InstallerClean, hoe je het hebt gekregen (setup, portable, slim, Scoop, het opdrachtregelprogramma op zichzelf, zelf gebouwd uit de broncode of onbekend), of het rapport door InstallerClean of door het opdrachtregelprogramma is verstuurd, en de taal waarin het werd weergegeven |
| Counts of anything that went wrong | Tellingen van alles wat er misging |
| Each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Dit zijn allemaal getallen of labels. Niets in het rapport noemt jou, je pc, je bestanden of je programma's, en ik zie je IP-adres nooit. |
| See exactly what's sent | Precies zien wat er wordt verstuurd |

## Summaries and counts

| English | Nederlands |
| --- | --- |
| {0} file left alone | {0} bestand ongemoeid gelaten |
| {0} files left alone | {0} bestanden ongemoeid gelaten |
| {0} unneeded file to clean up | {0} overbodig bestand om op te ruimen |
| {0} unneeded files to clean up | {0} overbodige bestanden om op te ruimen |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | Windows heeft een vermelding van {0} bestand dat niet in {InstallerFolder} staat: {1}. In het dagelijks gebruik levert dat geen problemen op, maar bijwerken of verwijderen van dat programma kan mislukken. Open Details voor wat je kunt doen. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | Windows heeft vermeldingen van {0} bestanden die niet in {InstallerFolder} staan: {1}. In het dagelijks gebruik leveren die geen problemen op, maar bijwerken of verwijderen van die programma's kan mislukken. Open Details voor wat je kunt doen. |
| {0} other program | {0} ander programma |
| {0} other programs | {0} andere programma's |
| {0} file with no program named in the records | {0} bestand waarbij de records geen programma noemen |
| {0} files with no program named in the records | {0} bestanden waarbij de records geen programma noemen |
| InstallerClean carried on without drive {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean is doorgegaan zonder schijf {0} en heeft elk bestand dat er nog mee vergeleken moest worden, ongemoeid gelaten. Klik op Opnieuw scannen zodra de schijf weer normaal reageert. |
| InstallerClean carried on without {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean is doorgegaan zonder {0} en heeft elk bestand dat er nog mee vergeleken moest worden, ongemoeid gelaten. Klik op Opnieuw scannen zodra de toegang weer normaal werkt. |
| InstallerClean carried on without more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, Re-scan. | InstallerClean is doorgegaan zonder meer dan één schijf of share ({0}) en heeft elk bestand dat nog met een daarvan vergeleken moest worden, ongemoeid gelaten. Klik op Opnieuw scannen zodra ze weer normaal reageren. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has finished. | Er was iets bezig met Windows Installer toen de scan begon, zoals een Windows-update of een programma dat installeerde, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Klik op Opnieuw scannen zodra dat klaar is. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then Re-scan. | Er was een Windows Installer-transactie opgeschort op deze computer toen de scan begon, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Hervat die installatie of draai haar terug, of herstart Windows, en klik dan op Opnieuw scannen. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Toen de scan begon, had Windows voor de volgende herstart een bestandshernoeming in de wachtrij staan die {InstallerFolder} raakt, dus heeft InstallerClean de bestanden daar die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Herstart Windows en klik dan op Opnieuw scannen. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Toen de scan begon, stond er een bestandsbewerking in de wachtrij voor de volgende herstart en kon InstallerClean niet zien welke bestanden die noemt, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Herstart Windows en klik dan op Opnieuw scannen. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Toen de scan begon, weigerde Windows InstallerClean toestemming om te controleren of Windows Installer bezig was, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Herstart Windows en klik op Opnieuw scannen. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Toen de scan begon, kon InstallerClean een van de Windows-instellingen die het controleert niet lezen, dus kon het niet zien of er een installatiebewerking liep of op een herstart wachtte, en daarom heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Herstart Windows en klik op Opnieuw scannen. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Toen de scan begon, kon InstallerClean niet controleren of Windows Installer bezig was, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Herstart Windows en klik op Opnieuw scannen. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Toen de scan begon, kon InstallerClean niet controleren of Windows Installer bezig was, omdat Windows een fout gaf: {0} (fout {1}). Daarom heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Herstart Windows en klik op Opnieuw scannen. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has cleared. | Windows Installer had iets lopen of in de wacht staan toen de scan begon, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Klik op Opnieuw scannen zodra dat voorbij is. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan. | De klok van deze pc is tijdens de scan teruggesprongen of achter gaan lopen, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Klik op Opnieuw scannen. |
| {0} of {1} {2} | {0} van {1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} om op te ruimen ({2}) |
| {0} file left alone ({1}) | {0} bestand ongemoeid gelaten ({1}) |
| {0} files left alone ({1}) | {0} bestanden ongemoeid gelaten ({1}) |
| {0} missing | {0} ontbreekt |
| {0} missing | {0} ontbreken |

## Confirmation dialogs

| English | Nederlands |
| --- | --- |
| Move {0} {1} ({2})? | {0} {1} verplaatsen ({2})? |
| This file will be moved to: | Dit bestand gaat naar: |
| These files will be moved to: | Deze bestanden gaan naar: |
| Delete {0} {1} ({2})? | {0} {1} verwijderen ({2})? |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Dit bestand wordt definitief verwijderd. Dat kan veilig, maar wil je een back-up, gebruik dan Verplaatsen. |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Deze bestanden worden definitief verwijderd. Dat kan veilig, maar wil je een back-up, gebruik dan Verplaatsen. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Die map staat op dezelfde schijf, dus de ruimte komt pas vrij als je hem verwijdert. Kies een map op een andere schijf als je de ruimte meteen wilt hebben. |

## Error messages

| English | Nederlands |
| --- | --- |
| This is also recorded in {0}. | Dit wordt ook vastgelegd in {0}. |
| Access denied | Toegang geweigerd |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows heeft InstallerClean de toegang geweigerd, dus het is gestopt. Er is niets verwijderd.<br><br>InstallerClean draaide al als administrator, dus opnieuw starten op die manier helpt niet. Windows zegt er niet bij wat er precies weigerde, dus er valt niets gerichts te proberen. |
| InstallerClean stopped | InstallerClean is gestopt |
| Scan failed | Scan mislukt |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | De Windows Installer-records kwamen volledig leeg terug: geen enkel geïnstalleerd programma of geen enkele update maakt aanspraak op een installatiebestand in de cache. Dat komt op een werkende computer niet voor (zelfs een gloednieuwe Windows-installatie heeft er een paar), dus ofwel zijn de records beschadigd, ofwel konden ze niet worden gelezen, en een scan die dit antwoord zou geloven, zou elk bestand in {InstallerFolder} ten onrechte verweesd noemen. InstallerClean is in plaats daarvan gestopt. Er is niets verwijderd. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer weigerde InstallerClean te laten zien wat er geïnstalleerd is. InstallerClean draaide al als administrator, dus het nogmaals als administrator uitvoeren verandert niets. Zonder die lijst is er geen veilige manier om te bepalen welke bestanden in de cache nog nodig zijn, dus InstallerClean is gestopt. Er is niets verwijderd. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer kon InstallerClean geen leesbare lijst van de geïnstalleerde programma's geven: het las {1} {2} en daarna kwam een vermelding onleesbaar terug (foutcode {0}). In plaats van met een half gelezen lijst te werken is InstallerClean gestopt. Er is niets verwijderd. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer heeft het einde van de lijst met geïnstalleerde programma's nooit gemeld: InstallerClean las {2} {3} en heeft het daarna na {0} vermeldingen opgegeven (laatste foutcode {1}). Een lijst zonder einde is niet te vertrouwen, dus InstallerClean is gestopt. Er is niets verwijderd. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer heeft het einde van de patchlijst van een programma nooit gemeld: InstallerClean las {2} {3} en heeft het daarna na {0} vermeldingen opgegeven (laatste foutcode {1}). Een lijst zonder einde is niet te vertrouwen, dus InstallerClean is gestopt. Er is niets verwijderd. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean kon de Windows Installer-records niet koppelen aan de inhoud van {InstallerFolder}. Bijna niets waarnaar de records verwijzen staat daar werkelijk, en bijna niets van wat daar staat wordt door een record genoemd, waardoor er niets als overbodig kon worden aangemerkt. Er is niets voor opruimen aangeboden en er is niets verwijderd. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean kon de Windows Installer-records niet koppelen aan de inhoud van {InstallerFolder}. De map bevat wel bestanden, maar geen enkel record verwijst naar iets daarin, waardoor er niets als overbodig kon worden aangemerkt. Er is niets voor opruimen aangeboden en er is niets verwijderd. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. Nothing has been offered and nothing has been removed. | InstallerClean kan {InstallerFolder} op deze pc niet scannen. Windows meldt die map als {0}, en InstallerClean scant alleen een Installer-map die Windows op zijn eigen pad meldt. Er is niets voor opruimen aangeboden en er is niets verwijderd. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. Nothing has been offered and nothing has been removed. | Windows meldt dat {InstallerFolder} niet bestaat op deze pc, dus InstallerClean is gestopt. Er is niets voor opruimen aangeboden en er is niets verwijderd. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. Nothing has been offered and nothing has been removed. | Windows meldde een fout toen InstallerClean de bestanden in {InstallerFolder} probeerde op te sommen, dus het kon geen volledige lijst krijgen en is gestopt. Er is niets voor opruimen aangeboden en er is niets verwijderd. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean kon niet genoeg van de Windows Installer-records lezen om zeker te weten wat er nog nodig is: de lijst met geïnstalleerde programma's kwam onvolledig terug, en dezelfde records rechtstreeks uit het register lezen leverde ook fouten op. Een bestand kon er verweesd uitzien enkel doordat het record dat het benoemt een van de onleesbare was, dus InstallerClean is gestopt. Er is niets verwijderd. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean kreeg Windows niet zover het echte pad van {InstallerFolder} te bepalen, dus van geen enkel bestand kon worden aangetoond dat het erin staat en er is niets voor opruimen aangeboden. Deze scan vond niets omdat die controle mislukte, niet omdat de map schoon is. Er is niets verwijderd. |
| Nothing was deleted | Er is niets verwijderd |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows weigerde InstallerClean toestemming om te controleren of Windows Installer bezig was, dus kon het niet uitsluiten dat een bestand halverwege alsnog nodig zou zijn, en er is niets verwijderd. |
| Nothing was moved | Er is niets verplaatst |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows weigerde InstallerClean toestemming om te controleren of Windows Installer bezig was, dus kon het niet uitsluiten dat een bestand halverwege alsnog nodig zou zijn, en er is niets verplaatst. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean kon de vergrendeling niet krijgen waarmee Windows Installer voorkomt dat twee programma's tegelijk geïnstalleerde software wijzigen, en kon dus niet uitsluiten dat een bestand halverwege alsnog nodig werd, dus er is niets verwijderd. Probeer het opnieuw, en herstart Windows als het zich blijft voordoen. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean kon de vergrendeling niet krijgen waarmee Windows Installer voorkomt dat twee programma's tegelijk geïnstalleerde software wijzigen, en kon dus niet uitsluiten dat een bestand halverwege alsnog nodig werd, dus er is niets verplaatst. Probeer het opnieuw, en herstart Windows als het zich blijft voordoen. |
| Invalid destination | Ongeldige bestemming |
| Move stopped | Verplaatsen gestopt |
| Couldn't use that backup folder | Kon die back-upmap niet gebruiken |
| Move failed | Verplaatsen mislukt |
| Delete failed | Verwijderen mislukt |
| Setting not saved | Instelling niet opgeslagen |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | De wijziging kon niet worden opgeslagen. InstallerClean gaat bij de volgende start terug naar de vorige instelling. |
| The destination cannot be inside the Windows Installer folder. | De bestemming mag niet in de Windows Installer-map liggen. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | De bestemming {0} verwijst naar een locatie onder een Windows-systeemmap. Kies een pad buiten %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% en %ProgramData%. |
| Not enough space | Onvoldoende ruimte |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | Er is te weinig plek op {0}<br><br>Vereist: {1}<br>Beschikbaar: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | Je hebt geen schrijfrechten voor {0}.<br>Probeer een map in je gebruikersprofiel of op een schijf die van jou is. |
| The path {0} is too long for Windows. Pick a shorter path. | Het pad {0} is te lang voor Windows. Kies een korter pad. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | De map {0} bestaat niet en kon niet worden aangemaakt. Controleer de stationsletter of het netwerkpad. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows kan niet schrijven naar {0}.<br>Details in {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows kan niet schrijven naar {0}. Het crashlog kon niet worden weggeschreven. |
| Cannot write to {0}.<br>Details in {1}. | Kan niet schrijven naar {0}.<br>Details in {1}. |
| Cannot write to {0}. The crash log could not be written. | Kan niet schrijven naar {0}. Het crashlog kon niet worden weggeschreven. |
| File no longer exists. | Bestand bestaat niet meer. |
| Source file is a symlink or junction; refused for safety. | Bronbestand is een symlink of junction; om veiligheidsredenen geweigerd. |
| This file is not directly inside the Windows Installer folder; refused for safety. | Dit bestand bevindt zich niet direct in de Windows Installer-map; om veiligheidsredenen geweigerd. |
| Windows refused access to this file; it was left in place. | Windows heeft de toegang tot dit bestand geweigerd; het is blijven staan. |
| Windows refused access to these files; they were left in place. | Windows heeft de toegang tot deze bestanden geweigerd; ze zijn blijven staan. |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | Dit bestand is geopend of vergrendeld door een ander programma, dus niets kan het nu verwijderen. Het is blijven staan; probeer het later opnieuw. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | Deze bestanden zijn geopend of vergrendeld door een ander programma, dus niets kan ze nu verwijderen. Ze zijn blijven staan; probeer het later opnieuw. |
| Windows reported a file error; the file was left in place. | Windows heeft een bestandsfout gemeld; het bestand is blijven staan. |
| Windows reported file errors; these files were left in place. | Windows heeft bestandsfouten gemeld; deze bestanden zijn blijven staan. |
| Something went wrong with this file; it was left in place. | Er is iets misgegaan met dit bestand; het is blijven staan. |
| Something went wrong with these files; they were left in place. | Er is iets misgegaan met deze bestanden; ze zijn blijven staan. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Weigert bestanden naar de Windows Installer-map te verplaatsen (bestemming: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | De back-upmap moet een volledig pad naar een map zijn, beginnend met een stationsletter of een netwerkshare (bijvoorbeeld D:\Backup of \\server\backup). InstallerClean kan deze niet gebruiken: {0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean kon de back-upmap niet meer bevestigen en is gestopt. Controleer {0}, klik daarna op Opnieuw scannen en probeer het opnieuw. |
| Cannot write to {0}. | Kan niet schrijven naar {0}. |
| You can save files in {0} but not open them again, so InstallerClean won't move files there. | Je kunt bestanden opslaan in {0}, maar ze niet opnieuw openen, dus InstallerClean verplaatst er geen bestanden naartoe. |
| A file called '{0}' is already in the backup folder. | Er staat al een bestand met de naam '{0}' in de back-upmap. |

## Update check

| English | Nederlands |
| --- | --- |
| Check for updates | Controleren op updates |
| Checking... | Controleren... |
| Up to date. | Je bent bij. |
| InstallerClean {0} is available. | InstallerClean {0} is beschikbaar. |
| It's strongly recommended to use the latest version. | Het wordt sterk aangeraden om de nieuwste versie te gebruiken. |
| Download it from GitHub | Download het van GitHub |
| Download .NET Framework 4.8 from Microsoft | Download .NET Framework 4.8 van Microsoft |
| Update now | Nu bijwerken |
| This copy was installed by Scoop. To update it, close InstallerClean and run: | Deze kopie is met Scoop geïnstalleerd. Bijwerken doe je door InstallerClean te sluiten en dit uit te voeren: |
| This copy was installed by Scoop for all users. To update it, close InstallerClean, open a terminal as administrator and run: | Deze kopie is met Scoop voor alle gebruikers geïnstalleerd. Bijwerken doe je door InstallerClean te sluiten, een terminal als administrator te openen en dit uit te voeren: |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean to update. | Deze versie heeft .NET Framework 4.8 nodig. Installeer het van Microsoft, herstart de pc en open daarna InstallerClean om bij te werken. |
| InstallerClean {0} can't be installed automatically. | InstallerClean {0} kan niet automatisch worden geïnstalleerd. |
| InstallerClean can't tell whether its setup installed it, so it can't update itself. | InstallerClean kan niet vaststellen of het met zijn eigen setup is geïnstalleerd en kan zichzelf dus niet bijwerken. |
| Copied | Gekopieerd |
| Couldn't copy it. Select the command and copy it yourself. | Kopiëren is niet gelukt. Selecteer de opdracht en kopieer hem zelf. |
| _Continue without updating | _Doorgaan zonder bij te werken |
| Downloading InstallerClean {0}... | InstallerClean {0} downloaden... |
| Checking the download... | Download controleren... |
| The update couldn't be downloaded. | De update kon niet worden gedownload. |
| The update couldn't be installed. | De update kon niet worden geïnstalleerd. |
| Couldn't reach GitHub. Check your internet connection and try again. | Kon GitHub niet bereiken. Controleer je internetverbinding en probeer het opnieuw. |
| GitHub returned an error response. Try again in a few minutes. | GitHub heeft een foutmelding teruggestuurd. Probeer het over een paar minuten opnieuw. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | In het antwoord van GitHub was geen release te herkennen. Probeer het later opnieuw, of open de releasepagina rechtstreeks. |
| The check timed out. Your connection to GitHub may be slow; try again. | De controle duurde te lang. Je verbinding met GitHub is mogelijk traag; probeer het opnieuw. |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | De controle is om een onbekende reden mislukt. De details staan in {0}, mocht je het willen melden. |
| The check failed for an unknown reason. The crash log could not be written. | De controle is om een onbekende reden mislukt. Het crashlog kon niet worden weggeschreven. |

## Opening links in your browser

| English | Nederlands |
| --- | --- |
| Couldn't open your browser | Kon je browser niet openen |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | De link staat op je klembord, dus je kunt hem zelf plakken:<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean kon de link ook niet naar je klembord kopiëren, dus hier is hij:<br><br>{0} |

## Startup and crashes

| English | Nederlands |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | Het is al actief. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Er is een onverwachte fout opgetreden en InstallerClean moet worden afgesloten.<br><br>{0}<br><br>Details weggeschreven naar:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Er is een onverwachte fout opgetreden en InstallerClean moet worden afgesloten.<br><br>{0}<br><br>Het crashlog kon niet worden weggeschreven. |
| Startup error | Opstartfout |
| Failed to start ({0}). Details written to:<br>{1} | Starten mislukt ({0}). Details weggeschreven naar:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Starten mislukt ({0}). Het crashlog kon niet worden weggeschreven. |
| Administrator rights needed | Administratorrechten nodig |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed.<br><br>Run InstallerClean again as administrator. | InstallerClean heeft administratorrechten nodig om elk programma te zien dat op deze pc is geïnstalleerd, en draait niet als administrator, dus het scant niet. Er is niets gewijzigd.<br><br>Voer InstallerClean opnieuw uit als administrator. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log legt onafgevangen fouten van InstallerClean vast.<br># Met verhoogde rechten kunnen de foutmeldingen van het framework<br># bestandspaden uit de lopende sessie bevatten (waaronder profielen<br># van andere gebruikers die door Windows Installer-query's zijn<br># opgesomd). Meldingen over netwerkfouten bij de updatecontrole of<br># de POST van het rapport kunnen de bestemmings-URL en het<br># opgeloste IP- of proxyadres bevatten. Regels over onleesbare<br># Windows Installer-records kunnen een Windows-account-SID<br># (S-1-5-21-...) en de productcodes van geïnstalleerde software<br># bevatten.<br># Verwijder alle drie de soorten gegevens voordat je dit bestand<br># aan een openbaar bugrapport toevoegt.<br> |

## Tooltips (hover text)

| English | Nederlands |
| --- | --- |
| Any small donation is much appreciated | Elke kleine donatie wordt zeer gewaardeerd |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Annulering aangevraagd. InstallerClean wacht tot de huidige stap een stoppunt bereikt. Dit kan enkele seconden duren bij zware I/O of een aanroep naar de MSI-database. |
| Close | Sluiten |
| I really should charge for InstallerClean - because it's been a huge amount of work - but I want to keep it free so it helps as many people as possible. Your donation makes it worthwhile and is very much appreciated. | Eigenlijk zou ik geld moeten vragen voor InstallerClean, want het is ontzettend veel werk geweest, maar ik wil het gratis houden zodat het zoveel mogelijk mensen helpt. Jouw donatie maakt het de moeite waard en wordt zeer gewaardeerd. |
| A star helps other people find it | Een ster helpt anderen het te vinden |
| Minimise | Minimaliseren |
| Move the unneeded files to the backup folder | De overbodige bestanden naar de back-upmap verplaatsen |
| Move the unneeded files to a backup folder. You'll choose it next. | De overbodige bestanden naar een back-upmap verplaatsen. Die kies je hierna. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | De overbodige bestanden naar de back-upmap verplaatsen. Die staat op dezelfde schijf, dus de ruimte komt pas vrij als je die map verwijdert. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | De overbodige bestanden definitief verwijderen. Gebruik Verplaatsen als je jezelf eerst wilt overtuigen dat alles goed is. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Naam van het onderwerp uit het ingebedde Authenticode-certificaat. Niet via de certificaatketen geverifieerd. |
| Change language. The program will restart. | Taal wijzigen. Het programma start opnieuw. |

## Screen reader labels

| English | Nederlands |
| --- | --- |
| Donate | Doneren |
| Donate $3 | $ 3 doneren |
| Donate | Doneren |
| Cancel operation | Bewerking annuleren |
| Cancel scan | Scan annuleren |
| Cancel startup scan | Opstartscan annuleren |
| Carry on without it, drive {0} | Doorgaan zonder te wachten, schijf {0} |
| Carry on without it, {0} | Doorgaan zonder te wachten, {0} |
| Close | Sluiten |
| Close window | Venster sluiten |
| Close result and return to main window | Resultaat sluiten en terug naar het hoofdvenster |
| Leave a star on github | Geef een ster op github |
| Minimise | Minimaliseren |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Definitief verwijderen haalt de overbodige bestanden weg. Annuleren sluit het venster zonder iets te verwijderen. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Verplaatsen zet de overbodige bestanden in de gekozen doelmap. Annuleren laat ze waar ze zijn. |
| Say thanks | Zeg bedankt |
| Check for updates | Controleren op updates |
| Checks github's releases page for a newer version. | Kijkt op de releasepagina van github of er een nieuwere versie is. |
| Opens the readme on github in your browser. | Opent het readme-bestand op github in je browser. |
| Opens the issue tracker on github.com in your browser. | Opent de issue-tracker op github.com in je browser. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Als dit is aangevinkt, kijkt InstallerClean bij het starten op github of er een nieuwere versie is. |
| Download it from github | Download het van github |
| Download .NET Framework 4.8 from Microsoft | Download .NET Framework 4.8 van Microsoft |
| Copy the command | De opdracht kopiëren |
| Scoop's update command | Bijwerkopdracht van Scoop |
| Opens the licence file on github.com in your browser. | Opent het licentiebestand op github.com in je browser. |
| Backup folder | Back-upmap |
| Patches | Patches |
| Product details | Productgegevens |
| Backup folder | Back-upmap |
| Operation progress | Voortgang van de bewerking |
| Scan {InstallerFolder} again | {InstallerFolder} opnieuw scannen |
| Scanning progress | Voortgang van de scan |
| Startup scan progress | Voortgang van de opstartscan |
| Details, unneeded files | Details, overbodige bestanden |
| Available for cleanup. | Beschikbaar om op te ruimen. |
| Details, files left alone | Details, ongemoeid gelaten bestanden |
| Read-only inventory. | Alleen-lezen overzicht. |
| Sorted by {0}, ascending | Gesorteerd op {0}, oplopend |
| Sorted by {0}, descending | Gesorteerd op {0}, aflopend |
| Scan results | Scanresultaten |
| Result details | Resultaatdetails |
| File details | Bestandsdetails |
| Product details | Productgegevens |
| Dialog text | Dialoogtekst |
| Waiting for a drive or share | Wachten op een schijf of share |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Bestanden die niet konden worden verwerkt |
| Explains this folder, and how to recover a file, in the README | Legt uit wat deze map is en hoe je een bestand terugzet, in het README-bestand |
| {0} Press Alt+{1} to carry on without it. {2} | {0} Druk op Alt+{1} om door te gaan zonder te wachten. {2} |
| What the report holds | Wat het rapport bevat |
| What the report holds | Wat het rapport bevat |
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
| ,  | ,  |
| drive {0} | schijf {0} |
| .  | .  |
| {0} TB | {0} TB |
| {0} GB | {0} GB |
| {0} MB | {0} MB |
| {0} KB | {0} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | minder dan een seconde |
| {0:F1} seconds | {0:F1} seconden |

## Command-line tool (installerclean-cli)

| English | Nederlands |
| --- | --- |
| Error: unknown argument '{0}' | Fout: onbekend argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Fout: onverwacht extra argument '{0}'. Als de verplaatsingsmap een spatie bevat, zet dan het volledige pad tussen aanhalingstekens: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Fout: onverwacht extra argument '{0}'. /s en /d nemen geen verdere argumenten, en er kan maar één vlag per uitvoering worden gebruikt. |
| Cancelling... | Annuleren... |
| Cancelled. | Geannuleerd. |
| Error: unexpected failure ({0}). Details written to {1}. | Fout: onverwachte crash ({0}). Details weggeschreven naar {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Fout: onverwachte crash ({0}). Het crashlog kon niet worden weggeschreven. |
| Scanning {InstallerFolder}... | {InstallerFolder} scannen... |
| Found {0} unneeded {1} to clean up ({2}). | {0} overbodige {1} gevonden om op te ruimen ({2}). |
| Found no unneeded files. | Geen overbodige bestanden gevonden. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean kon niet met zekerheid vaststellen welke bestanden in de cache bij de hier geïnstalleerde programma's horen, en heeft daarom het ene bestand ({2}) achtergehouden in plaats van het aan te bieden. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean kon niet met zekerheid vaststellen welke bestanden in de cache bij de hier geïnstalleerde programma's horen, en heeft daarom {0} {1} ({2}) achtergehouden in plaats van ze aan te bieden. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean kon niet vaststellen dat één bestand overbodig is, en heeft het daarom ({2}) achtergehouden in plaats van het aan te bieden. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean kon niet vaststellen dat {0} {1} overbodig zijn, en heeft ze daarom ({2}) achtergehouden in plaats van ze aan te bieden. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean kon niet met zekerheid vaststellen welke bestanden in de cache bij de hier geïnstalleerde programma's horen, en heeft daarom het ene bestand ({2}) achtergehouden in plaats van het aan te bieden. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean kon niet met zekerheid vaststellen welke bestanden in de cache bij de hier geïnstalleerde programma's horen, en heeft daarom {0} {1} ({2}) achtergehouden in plaats van ze aan te bieden. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean kon niet vaststellen dat één bestand overbodig is, en heeft het daarom ({2}) achtergehouden in plaats van het aan te bieden. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean kon niet vaststellen dat {0} {1} overbodig zijn, en heeft ze daarom ({2}) achtergehouden in plaats van ze aan te bieden. |
| Why it couldn't be certain: | Waarom die zekerheid er niet was: |
|   A file path in Windows Installer's own records wouldn't resolve, so nothing could be matched to it. |   Een bestandspad uit de eigen administratie van Windows Installer was niet te herleiden, en er kon daarom niets aan worden gekoppeld. |
|   A file Windows has a record of couldn't be identified, so it couldn't be matched to what's in the folder. |   Een bestand waarvan Windows een administratie heeft, was niet te identificeren, en kon daarom niet worden vergeleken met wat er in de map staat. |
|   A program may be installed more than once on this PC, and the records can't say which copy a file belongs to. |   Een programma is mogelijk meer dan één keer op deze pc geïnstalleerd, en de administratie kan niet zeggen bij welke kopie een bestand hoort. |
|   Part of Windows Installer's records wouldn't read, so a file could belong to a program recorded there. |   Een deel van de administratie van Windows Installer was niet te lezen, en een bestand kan daarom bij een daarin vastgelegd programma horen. |
|   A file in the folder couldn't be identified, so it couldn't be matched against the records. |   Een bestand in de map was niet te identificeren, en kon daarom niet met de administratie worden vergeleken. |
|   Either a file wouldn't say which program it belongs to, or Windows wouldn't give a clear answer about that program. |   Ofwel gaf een bestand niet aan bij welk programma het hoort, ofwel gaf Windows geen duidelijk antwoord over dat programma. |
|   A check on which programs the files belong to gave answers that didn't line up with the files it was handed. |   Een controle op bij welke programma's de bestanden horen, gaf antwoorden die niet overeenkwamen met de bestanden die eraan waren doorgegeven. |
|   Either a patch file wouldn't say which patch it is, or it wouldn't say which programs it's for, or Windows wouldn't say in full which programs have that patch. |   Ofwel gaf een patchbestand niet aan om welke patch het gaat, ofwel gaf het niet aan voor welke programma's het bedoeld is, ofwel gaf Windows niet volledig aan welke programma's die patch hebben. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows heeft een vermelding van {0} bestand dat niet in {InstallerFolder} staat: {1}. In het dagelijks gebruik levert dat geen problemen op, maar bijwerken of verwijderen van dat programma kan mislukken. Om het bestand terug te zetten heb je het installatieprogramma nodig van de versie die je al hebt. Haal het bij de maker van het programma en voer het uit over je bestaande installatie heen. Een nieuwere versie volstaat niet: die moet eerst verwijderen wat je hebt, en juist die stap heeft dit bestand nodig. Eerst verwijderen werkt om dezelfde reden ook niet. Dit zou het bestand moeten herstellen en je instellingen ongemoeid moeten laten, maar Microsoft geeft daar geen garantie op. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows heeft vermeldingen van {0} bestanden die niet in {InstallerFolder} staan: {1}. In het dagelijks gebruik leveren die geen problemen op, maar bijwerken of verwijderen van die programma's kan mislukken. Om een bestand terug te zetten heb je het installatieprogramma nodig van de versie die je van dat programma al hebt. Haal het bij de maker van het programma en voer het uit over je bestaande installatie heen. Een nieuwere versie volstaat niet: die moet eerst verwijderen wat je hebt, en juist die stap heeft het bestand nodig. Eerst verwijderen werkt om dezelfde reden ook niet. Dit zou het bestand moeten herstellen en je instellingen ongemoeid moeten laten, maar Microsoft geeft daar geen garantie op. |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | InstallerClean kon niet met zekerheid vaststellen dat het ene vervangen bestand niet meer nodig is, en heeft het daarom achtergehouden. |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | InstallerClean kon niet met zekerheid vaststellen dat {0} vervangen bestanden niet meer nodig zijn, en heeft ze daarom achtergehouden. |
| InstallerClean stopped waiting for drive {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean is gestopt met wachten op schijf {0} en heeft elk bestand dat er nog mee vergeleken moest worden, ongemoeid gelaten. Voer de opdracht opnieuw uit zodra de schijf weer normaal reageert. |
| InstallerClean stopped waiting for {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean is gestopt met wachten op {0} en heeft elk bestand dat er nog mee vergeleken moest worden, ongemoeid gelaten. Voer de opdracht opnieuw uit zodra de toegang weer normaal werkt. |
| InstallerClean stopped waiting for more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, run the command again. | InstallerClean is gestopt met wachten op meer dan één schijf of share ({0}) en heeft elk bestand dat nog met een daarvan vergeleken moest worden, ongemoeid gelaten. Voer de opdracht opnieuw uit zodra ze weer normaal reageren. |
| Deleting {0} unneeded {1}... | {0} overbodige {1} verwijderen... |
| Permanently deleted {0} unneeded {1}. | {0} overbodige {1} definitief verwijderd. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Fout: er is geen bestemming voor het verplaatsen opgegeven. Gebruik /m PAD. (Een standaard die in de GUI is ingesteld, geldt per gebruiker en niet voor geplande taken of serviceaccounts.) |
| Error: destination cannot be inside the Windows Installer folder. | Fout: de bestemming mag niet in de Windows Installer-map liggen. |
| Error: destination must be a fully qualified path. Got: {0} | Fout: de bestemming moet een volledig gekwalificeerd pad zijn. Ontvangen: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Fout: de bestemming {0} komt uit onder een Windows-systeemmap. Kies een pad buiten %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% en %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Fout: onvoldoende ruimte op {0}. Deze bestanden verplaatsen vraagt {1} en er is {2} vrij. Er is niets verplaatst. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Fout: er is op dit moment iets bezig met Windows Installer, zoals een Windows-update of een programma dat op de achtergrond installeert. /m en /d zijn geblokkeerd zolang dat loopt. Probeer het opnieuw zodra het klaar is. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Fout: een eerdere Windows Installer-transactie is op deze computer opgeschort. Hervat die installatie of draai haar terug (of herstart Windows) voordat je {InstallerFolder} opruimt. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Fout: een bestandsbewerking die na de herstart in de wachtrij staat, richt zich op {InstallerFolder} ({0}). Herstart Windows om die bewerking te voltooien voordat je opruimt. |
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | Fout: er staat een bestandsbewerking in de wachtrij voor de volgende herstart en InstallerClean kan niet zien welke bestanden die noemt, dus kan het {InstallerFolder} niet uitsluiten. Start Windows opnieuw op voordat je opruimt. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Fout: InstallerClean kon een van de registerwaarden die het controleert voordat het {InstallerFolder} aanraakt niet lezen, en kan dus een lopende of voor de volgende herstart ingeplande Windows Installer-bewerking niet uitsluiten. /m en /d zijn geblokkeerd. Start Windows opnieuw op en probeer het opnieuw. Als het lezen dan nog steeds mislukt, is dit geen machine die InstallerClean kan opruimen. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Fout: InstallerClean kon niet controleren of Windows Installer bezig is, en kan dus een lopende installatie niet uitsluiten. /m en /d zijn geblokkeerd. Start Windows opnieuw op en probeer het opnieuw. Als de controle dan nog steeds mislukt, is dit geen machine die InstallerClean kan opruimen. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Fout: InstallerClean kon niet controleren of Windows Installer bezig is, omdat Windows een fout gaf: {0} (fout {1}), en kan dus een lopende installatie niet uitsluiten. /m en /d zijn geblokkeerd. Start Windows opnieuw op en probeer het opnieuw. Als de controle dan nog steeds mislukt, is dit geen machine die InstallerClean kan opruimen. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Fout: Windows Installer heeft iets lopen, dus /m en /d zijn geblokkeerd. InstallerClean raakt {InstallerFolder} niet aan terwijl daar iets verandert. Probeer het opnieuw zodra het klaar is. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has finished. | Er was iets bezig met Windows Installer toen de scan begon, zoals een Windows-update of een programma dat installeerde, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Voer de opdracht opnieuw uit zodra dat klaar is. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then run it again. | Er was een Windows Installer-transactie opgeschort op deze computer toen de scan begon, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Hervat die installatie of draai haar terug, of herstart Windows, en voer de opdracht dan opnieuw uit. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Toen de scan begon, had Windows voor de volgende herstart een bestandshernoeming in de wachtrij staan die {InstallerFolder} raakt, dus heeft InstallerClean de bestanden daar die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Herstart Windows en voer de opdracht dan opnieuw uit. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Toen de scan begon, stond er een bestandsbewerking in de wachtrij voor de volgende herstart en kon InstallerClean niet zien welke bestanden die noemt, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Herstart Windows en voer de opdracht dan opnieuw uit. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Toen de scan begon, weigerde Windows InstallerClean toestemming om te controleren of Windows Installer bezig was, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Herstart Windows en voer de opdracht opnieuw uit. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Toen de scan begon, kon InstallerClean een van de Windows-instellingen die het controleert niet lezen, dus kon het niet zien of er een installatiebewerking liep of op een herstart wachtte, en daarom heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Herstart Windows en voer de opdracht opnieuw uit. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Toen de scan begon, kon InstallerClean niet controleren of Windows Installer bezig was, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Herstart Windows en voer de opdracht opnieuw uit. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Toen de scan begon, kon InstallerClean niet controleren of Windows Installer bezig was, omdat Windows een fout gaf: {0} (fout {1}). Daarom heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Herstart Windows en voer de opdracht opnieuw uit. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has cleared. | Windows Installer had iets lopen of in de wacht staan toen de scan begon, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Voer de opdracht opnieuw uit zodra dat voorbij is. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again. | De klok van deze pc is tijdens de scan teruggesprongen of achter gaan lopen, dus heeft InstallerClean de bestanden in {InstallerFolder} die door de records van geen enkel geïnstalleerd programma worden genoemd, achtergehouden in plaats van ze aan te bieden. Voer de opdracht opnieuw uit. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Fout: InstallerClean kon de Windows Installer-vergrendeling niet krijgen die voorkomt dat twee programma's tegelijk geïnstalleerde software wijzigen, en kon dus niet uitsluiten dat een bestand halverwege alsnog nodig werd. Er is niets verwijderd. Probeer het opnieuw, en herstart Windows als het zich blijft voordoen. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Fout: InstallerClean kon de Windows Installer-vergrendeling niet krijgen die voorkomt dat twee programma's tegelijk geïnstalleerde software wijzigen, en kon dus niet uitsluiten dat een bestand halverwege alsnog nodig werd. Er is niets verplaatst. Probeer het opnieuw, en herstart Windows als het zich blijft voordoen. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Fout: Windows weigerde InstallerClean toestemming om te controleren of Windows Installer bezig was, dus kon het niet uitsluiten dat een bestand halverwege alsnog nodig zou zijn. Er is niets verwijderd. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Fout: Windows weigerde InstallerClean toestemming om te controleren of Windows Installer bezig was, dus kon het niet uitsluiten dat een bestand halverwege alsnog nodig zou zijn. Er is niets verplaatst. |
| Moving {0} unneeded {1} to {2}... | {0} overbodige {1} verplaatsen naar {2}... |
| Moved {0} unneeded {1}. | {0} overbodige {1} verplaatst. |
| Check that your programs still update and uninstall as normal, then delete {0}. | Controleer of je programma's nog gewoon bijwerken en verwijderen, en verwijder daarna {0}. |
| InstallerClean is free. If it helped, please donate $3: {0} | InstallerClean is gratis. Heb je er iets aan gehad, doneer dan $ 3: {0} |
| Update now? It's strongly recommended to use the latest version. [{0}/{1}] | Nu bijwerken? Het wordt sterk aangeraden om de nieuwste versie te gebruiken. [{0}/{1}] |
| Y | J |
| N | N |
| No answer after a minute, so InstallerClean updates without asking, as it does when nobody is there to answer. | Na een minuut geen antwoord, dus InstallerClean werkt bij zonder te vragen, zoals wanneer er niemand is om te antwoorden. |
| Send an anonymous report of this run? It is saved at {0}. Type ? to see what it holds. [{1}/{2}/?] | Een anoniem rapport van deze uitvoering versturen? Het is opgeslagen in {0}. Typ ? om te zien wat erin staat. [{1}/{2}/?] |
| the chart at {0} | de grafiek op {0} |
| The report adds your result to {0}, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Het rapport voegt je resultaat toe aan {0}, die laat zien hoeveel ruimte mensen met InstallerClean vrijmaken. Het bevat ook deze technische gegevens, die me helpen eventuele bugs op te sporen: |
| The full report is printed below. | Het volledige rapport staat hieronder. |
| Report sent. Thank you. | Rapport verstuurd. Bedankt. |
| Report not sent. | Rapport niet verstuurd. |
| No answer after a minute. | Na een minuut geen antwoord. |
| The report could not be sent. It is kept in {0}. | Het rapport kon niet worden verstuurd. Het wordt bewaard in {0}. |
| Sending a report left waiting by an earlier run. | Een rapport dat sinds een eerdere uitvoering wacht, wordt nu verstuurd. |
| The new version will scan and list what it finds, without deleting anything. To delete, run /d again afterwards. | De nieuwe versie scant en toont wat hij vindt, zonder iets te verwijderen. Voer daarna /d opnieuw uit om te verwijderen. |
| The new version will scan and list what it finds, without moving anything. To move, run /m again afterwards. | De nieuwe versie scant en toont wat hij vindt, zonder iets te verplaatsen. Voer daarna /m opnieuw uit om te verplaatsen. |
| InstallerClean is in use, so it can't update itself now. | InstallerClean is in gebruik en kan zichzelf nu dus niet bijwerken. |
| Running {0} with InstallerClean {1} instead. | In plaats daarvan wordt {0} uitgevoerd met InstallerClean {1}. |
| The newer version couldn't be run. | De nieuwere versie kon niet worden uitgevoerd. |
| Running {0} with InstallerClean {1}. It takes this copy's place once the run ends. | {0} wordt uitgevoerd met InstallerClean {1}. Na afloop neemt deze versie de plaats van deze kopie in. |
| {0}: {1} | {0}: {1} |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again to update. | Deze versie heeft .NET Framework 4.8 nodig. Installeer het van Microsoft, herstart de pc en voer daarna InstallerClean opnieuw uit om bij te werken. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | Het is eenvoudig ongedaan te maken. Verplaats ze vanuit {0} terug naar {InstallerFolder} en alles is weer zoals het was. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean kon de back-upmap niet meer bevestigen en is gestopt. Controleer {0} en voer de opdracht opnieuw uit. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Een ander InstallerClean-proces houdt de single-instance-vergrendeling vast (de GUI of een andere CLI-uitvoering). Exit 75 (tijdelijk); je kunt het later veilig opnieuw proberen. |
| Stopped after deleting {0} of {1} {2}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Gestopt na het verwijderen van {0} van {1} {2}: InstallerClean kon niet langer zeker zijn dat geen andere InstallerClean bezig was. Voer dezelfde opdracht opnieuw uit. Exit 2 (gedeeltelijk). |
| Stopped after moving {0} of {1} {2} to {3}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Gestopt na het verplaatsen van {0} van {1} {2} naar {3}: InstallerClean kon niet langer zeker zijn dat geen andere InstallerClean bezig was. Voer dezelfde opdracht opnieuw uit. Exit 2 (gedeeltelijk). |
| Stopped before deleting any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Gestopt voordat er een bestand is verwijderd: InstallerClean kon niet langer zeker zijn dat geen andere InstallerClean bezig was. Voer dezelfde opdracht opnieuw uit. Exit 75 (tijdelijk). |
| Stopped before moving any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Gestopt voordat er een bestand is verplaatst: InstallerClean kon niet langer zeker zijn dat geen andere InstallerClean bezig was. Voer dezelfde opdracht opnieuw uit. Exit 75 (tijdelijk). |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed. Run it again from an administrator prompt. | InstallerClean heeft administratorrechten nodig om elk programma te zien dat op deze pc is geïnstalleerd, en draait niet als administrator, dus het scant niet. Er is niets gewijzigd. Voer het opnieuw uit vanuit een prompt als administrator. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Opmerking: het schrijven naar het gebeurtenislogboek is mislukt. Controleer de machtigingen voor het logboek Toepassing of het groepsbeleid. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - {InstallerFolder} opschonen |
| Removes cached .msi and .msp files that no installed program still needs. | Verwijdert .msi/.msp die geen geïnstalleerd programma meer nodig heeft. |
| Needs an elevated (administrator) prompt and won't scan without one. | Vereist een prompt als administrator; anders scant het niet. |
| Usage: | Gebruik: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Deze helptekst weergeven (ook /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  De versie weergeven (ook -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Alleen scannen: overbodige bestanden tonen |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Definitief verwijderen wat overbodig is |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Verplaatsen naar de opgeslagen back-upmap |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PAD     Verplaatsen naar het opgegeven pad |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blokkeert de prompt tot het klaar is, zodat een script<br>of geplande taak erop kan wachten. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Die map geldt per gebruiker; geplande of SYSTEM-taken vereisen /m PAD. |
| Before it scans, it checks for a newer version of InstallerClean.<br>When you run it yourself, it asks before updating, and updates if nobody<br>answers within a minute. When it runs on its own, in a scheduled task or<br>a script, it updates without asking.<br>Add /noupdate to skip the check. | Voor het scannen controleert het of er een nieuwere versie van<br>InstallerClean is. Start je het zelf, dan vraagt het eerst toestemming, en<br>werkt het bij als niemand binnen een minuut antwoordt. Draait het<br>zelfstandig, in een geplande taak of een script, dan werkt het bij zonder<br>te vragen. Voeg /noupdate toe om de controle over te slaan. |
| Exit codes: | Afsluitcodes: |
|   0   success: the run did what it was asked |   0   geslaagd: de uitvoering deed wat gevraagd was |
|   1   nothing processed: the run failed or was refused |   1   niets verwerkt: de uitvoering is mislukt of is geweigerd |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   gedeeltelijk: een deel verwerkt, een deel niet (een fout of Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  tijdelijk: iets tijdelijks blokkeerde de uitvoering (zie de melding) |
|   130 cancelled (Ctrl+C) |   130 geannuleerd (Ctrl+C) |
