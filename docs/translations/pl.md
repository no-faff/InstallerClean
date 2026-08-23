# InstallerClean in Polski (Polish)

The text of InstallerClean's interface and command-line tool in English on the left, with the Polish translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Polish can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.pl.resx`](../../src/InstallerClean.Core/Resources/Strings.pl.resx), so do not edit it by hand. The Polish translation itself lives in [`gen-strings-pl.mjs`](../../scripts/translations/gen-strings-pl.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Polski |
| --- | --- |
| InstallerClean | InstallerClean |
| About | O programie |
| Files left alone | Pliki pozostawione bez zmian |
| Unneeded files that are safe to delete | Niepotrzebne pliki, które można bezpiecznie usunąć |

## Section headings

| English | Polski |
| --- | --- |
| PATCHES | POPRAWKI |
| PRODUCT DETAILS | SZCZEGÓŁY PRODUKTU |
| BACKUP FOLDER | FOLDER DOCELOWY |
| SAY THANKS | PODZIĘKUJ |

## Buttons and actions

| English | Polski |
| --- | --- |
| _About | _O programie |
| Copy | Kopiuj |
| Cut | Wytnij |
| Paste | Wklej |
| Select all | Zaznacz wszystko |
| _Browse... | Prze_glądaj... |
| _Cancel | _Anuluj |
| Check for _updates | Sprawdź _aktualizacje |
| _Close | _Zamknij |
| _Delete permanently | Usuń _trwale |
| _Done | _Gotowe |
| Details | Szczegóły |
| _Buy me a cuppa | Postaw mi _kawę |
| Leave a _star on GitHub | Zostaw _gwiazdkę na GitHubie |
| Apache 2.0 licence | Licencja Apache 2.0 |
| _Move | _Przenieś |
| Path to folder if you move rather than delete. | Ścieżka do folderu, jeśli przenosisz zamiast usuwać. |
| Open _release page | Otwórz stronę _wydania |
| _Re-scan | _Skanuj ponownie |
| _Scan again | Skanuj _ponownie |
| Send report | Wyślij raport |
| _Send | _Wyślij |

## About window

| English | Polski |
| --- | --- |
| Guide and FAQ | Przewodnik i FAQ |
| Report a problem | Zgłoś problem |
| Check for updates automatically | Automatycznie sprawdzaj aktualizacje |

## Field labels

| English | Polski |
| --- | --- |
| Reason | Powód |
| Author | Autor |
| Application | Aplikacja |
| Title | Tytuł |
| Subject | Temat |
| Keywords | Słowa kluczowe |
| Signing certificate | Certyfikat podpisujący |
| File size | Rozmiar pliku |
| Comment | Komentarz |
| Product name | Nazwa produktu |
| File | Plik |
| Size | Rozmiar |
| Patches | Poprawki |
| (unknown) | (nieznana) |
| (patches only) | (tylko poprawki) |
| missing | brak |

## Status and progress

| English | Polski |
| --- | --- |
| Scanning... | Skanowanie... |
| Cancelling... | Anulowanie... |
| Starting scan... | Rozpoczynanie skanowania... |
| Asking Windows about installed software... | Pytanie Windows o zainstalowane oprogramowanie... |
| Scanning installer cache folder... | Skanowanie folderu pamięci podręcznej instalatora... |
| Enumerating installed products... | Wyliczanie zainstalowanych produktów... |
| Checking registry for additional packages... | Sprawdzanie rejestru w poszukiwaniu dodatkowych pakietów... |
| Found {0} registered {1}. | Znaleziono {0} zarejestrowanych {1}. |
| Scan complete ({0}) | Skanowanie zakończone ({0}) |
| Scanning local packages... | Skanowanie pakietów lokalnych... |
| Found {0} {1} you can safely delete. | Znaleziono {0} {1} do bezpiecznego usunięcia. |
| Preparing destination folder... | Przygotowywanie folderu docelowego... |
| Moving unneeded files... | Przenoszenie niepotrzebnych plików... |
| Deleting unneeded files... | Usuwanie niepotrzebnych plików... |
| Move cancelled. {0} of {1} {2} processed. | Przenoszenie anulowane. Przetworzono {0}/{1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Usuwanie anulowane. Przetworzono {0}/{1} {2}. |
| Move failed ({0}). Details in {1}. | Przenoszenie nie powiodło się ({0}). Szczegóły w {1}. |
| Move failed ({0}). The crash log could not be written. | Przenoszenie nie powiodło się ({0}). Nie udało się zapisać dziennika awarii. |
| Delete failed ({0}). Details in {1}. | Usuwanie nie powiodło się ({0}). Szczegóły w {1}. |
| Delete failed ({0}). The crash log could not be written. | Usuwanie nie powiodło się ({0}). Nie udało się zapisać dziennika awarii. |
| Access denied. Windows refused the scan. | Odmowa dostępu. Windows odmówił skanowania. |
| Scan failed: couldn't read the Windows Installer records. | Skanowanie nie powiodło się: nie udało się odczytać rekordów Windows Installera. |
| Scan cancelled. | Skanowanie anulowane. |
| Ready | Gotowe |
| Scan failed ({0}). Details in {1}. | Skanowanie nie powiodło się ({0}). Szczegóły w {1}. |
| Scan failed ({0}). The crash log could not be written. | Skanowanie nie powiodło się ({0}). Nie udało się zapisać dziennika awarii. |

## Main screen text

| English | Polski |
| --- | --- |
| Any unneeded files below are [safe to delete]. | Wszystkie niepotrzebne pliki poniżej [można bezpiecznie usunąć]. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Leżą w {InstallerFolder}. InstallerClean pyta system Windows o każdy zainstalowany program: plik trafia na listę, gdy żaden program się do niego nie przyznaje ({0}) albo gdy nowsza poprawka go zastąpiła i żaden program nie mógłby do niego wrócić ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update, repair and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Przenieś je do wybranego przez siebie folderu docelowego, a potem usuń ten folder, gdy się przekonasz, że twoje programy nadal normalnie się aktualizują, naprawiają i odinstalowują. Umieszczenie ich z powrotem w {InstallerFolder} przywraca wszystko. Albo usuń je trwale już teraz. |
| Nothing scanned yet. | Jeszcze nic nie przeskanowano. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Naciśnij przycisk Skanuj ponownie, aby przejrzeć {InstallerFolder} w poszukiwaniu plików instalatora, których żaden program już nie potrzebuje. |
| These files can't be cleaned up right now. | Tych plików nie można teraz wyczyścić. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Coś właśnie korzysta z Instalatora Windows, na przykład aktualizacja systemu albo program instalujący się w tle. Przenieś i Usuń są wstrzymane, dopóki to trwa, żeby InstallerClean nie ruszał {InstallerFolder} w trakcie zmian. Gdy się skończy, skanuj ponownie, a wrócą. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Na tym komputerze jest wstrzymana wcześniejsza transakcja Instalatora Windows. Wznów ją lub wycofaj tę instalację (albo uruchom system ponownie), zanim wyczyścisz {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows ma w kolejce na następne uruchomienie zmianę nazwy pliku, która dotyczy {InstallerFolder}. Uruchom system ponownie, zanim wyczyścisz. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Instalator Windows ma coś w toku, więc Przenieś i Usuń są wstrzymane. InstallerClean nie ruszy {InstallerFolder} w trakcie zmian. Gdy się skończy, skanuj ponownie, a wrócą. |
| Select a file to view details. | Wybierz plik, aby zobaczyć szczegóły. |
| Select a product to view details. | Wybierz produkt, aby zobaczyć szczegóły. |
| No metadata available. | Brak dostępnych metadanych. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. | Tego pliku instalatora brakuje. Teraz nie sprawia to żadnych kłopotów i nie będzie sprawiać aż do dnia, w którym spróbujesz naprawić, zaktualizować lub odinstalować program, do którego należy. Ten krok może się wtedy nie powieść, bo Windows szuka tego pliku, a jego nie ma.<br><br>Aby spróbować to naprawić, pobierz instalator tego programu od jego producenta i uruchom go na istniejącej kopii (nie odinstalowuj najpierw: odinstalowanie samo w sobie jest krokiem, któremu ten plik jest potrzebny). Jeśli zdołasz, użyj tej wersji, którą masz zainstalowaną, bo Windows może odrzucić inną. To powinno przywrócić plik i zostawić twoje ustawienia w spokoju, ale Microsoft tego nie gwarantuje, a jego własną ostatecznością jest ponowna instalacja programu. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README [wyjaśnia ten folder] i sposób odzyskania pliku, słowami samego Microsoftu. |
| (none) | (brak) |

## Reasons a file is unneeded

| English | Polski |
| --- | --- |
| Orphaned | Osierocony |
| Superseded | Zastąpiony |
| Obsoleted | Przestarzały |

## Completion screen

| English | Polski |
| --- | --- |
| All clean | Wszystko czyste |
| Nothing removed | Niczego nie usunięto |
| Nothing to clean up in {InstallerFolder} | Nie ma czego czyścić w {InstallerFolder} |
| Scanned {0} {1} in {2} | Przeskanowano {0} {1} w {2} |
| Nothing offered on this PC | Na tym komputerze niczego nie zaproponowano |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({1}) it might otherwise have offered. | InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej, więc zatrzymał jedyny plik ({1}), który inaczej by zaproponował. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} files ({1}) it might otherwise have offered. | InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej, więc zatrzymał wszystkie {0} plików ({1}), które inaczej by zaproponował. |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | Plik w tym folderze [można bezpiecznie usunąć], więc skasuj folder, kiedy zechcesz. Do tego czasu możesz umieścić go z powrotem w {InstallerFolder}, gdyby jakiś program okazał się go potrzebować (skrajnie mało prawdopodobne). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | Pliki w tym folderze [można bezpiecznie usunąć], więc skasuj go, kiedy zechcesz. Do tego czasu możesz umieścić je z powrotem w {InstallerFolder}, gdyby jakiś program okazał się potrzebować któregoś z nich (skrajnie mało prawdopodobne). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | Plik w tym folderze [można bezpiecznie usunąć], więc skasuj folder albo przenieś go na inny dysk, kiedy naprawdę zechcesz odzyskać miejsce. Do tego czasu możesz umieścić go z powrotem w {InstallerFolder}, gdyby jakiś program okazał się go potrzebować (skrajnie mało prawdopodobne). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | Pliki w tym folderze [można bezpiecznie usunąć], więc skasuj go albo przenieś na inny dysk, kiedy naprawdę zechcesz odzyskać miejsce. Do tego czasu możesz umieścić je z powrotem w {InstallerFolder}, gdyby jakiś program okazał się potrzebować któregoś z nich (skrajnie mało prawdopodobne). |
| {0} freed | Zwolniono {0} |
| {0} moved | Przeniesiono {0} |
| Nothing was moved | Niczego nie przeniesiono |
| Nothing was deleted | Niczego nie usunięto |
| {0} of {1} could not be moved. | Nie udało się przenieść {0} pliku z {1}. |
| {0} of {1} could not be moved. | Nie udało się przenieść {0} plików z {1}. |
| {0} of {1} could not be deleted. | Nie udało się usunąć {0} pliku z {1}. |
| {0} of {1} could not be deleted. | Nie udało się usunąć {0} plików z {1}. |
| {0} {1} moved to: {2} | Przeniesiono {0} {1} do: {2} |
| {0} {1} moved to: {2} | Przeniesiono {0} {1} do: {2} |
| {0} {1} kept in place, because the records now claim what the scan flagged. | Pozostawiono na miejscu {0} {1}, ponieważ rekordy przyznają się teraz do tego, co oznaczyło skanowanie. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | Pozostawiono na miejscu {0} {1}, ponieważ do czasu końcowego sprawdzenia rekordy Instalatora Windows się zmieniły. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | Pozostawiono na miejscu {0} {1}, ponieważ przy końcowym sprawdzeniu nie udało się odczytać rekordów Instalatora Windows w całości. |
| {0} {1} kept in place, because by the final check InstallerClean could not be certain which cached files belong to the programs installed here. | Pozostawiono na miejscu {0} {1}, ponieważ do czasu końcowego sprawdzenia InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | Pozostawiono na miejscu {0} {1}, ponieważ Windows ma rekord programu wskazanego w środku. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | Pozostawiono na miejscu {0} {1}, ponieważ InstallerClean nie znalazł w środku nazwy żadnego programu. |
| Moved {0} of {1} {2} before you cancelled. | Przed anulowaniem przeniesiono {0}/{1} {2}. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Przed anulowaniem usunięto trwale {0}/{1} {2}. |
| {0} {1} permanently deleted | Trwale usunięto {0} {1} |
| {0} {1} permanently deleted | Trwale usunięto {0} {1} |
| Glad to help. There's a tip jar if you're feeling kind. | Cieszę się, że pomogło. Jeśli masz dobre serce, jest miejsce na napiwek. |

## Summaries and counts

| English | Polski |
| --- | --- |
| {0} file left alone | {0} plik pozostawiony bez zmian |
| {0} files left alone | {0} plików pozostawionych bez zmian |
| {0} unneeded file to clean up | {0} niepotrzebny plik do wyczyszczenia |
| {0} unneeded files to clean up | {0} niepotrzebnych plików do wyczyszczenia |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Open Details for what to do. | Windows ma rekord dla {0} pliku, którego nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawia to kłopotu, ale naprawa, aktualizacja lub odinstalowanie może się przez niego nie powieść. Otwórz Szczegóły, aby dowiedzieć się, co zrobić. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Open Details for what to do. | Windows ma rekordy dla {0} plików, których nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawiają one kłopotu, ale naprawa, aktualizacja lub odinstalowanie może się przez nie nie powieść. Otwórz Szczegóły, aby dowiedzieć się, co zrobić. |
| {0} other program | jeszcze {0} program |
| {0} other programs | jeszcze {0} programów |
| {0} file with no program named in the records | {0} plik, dla którego rekordy nie wskazują programu |
| {0} files with no program named in the records | {0} plików, dla których rekordy nie wskazują programu |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than listing it. | Na tym komputerze InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej, więc zatrzymał jedyny plik zamiast pokazać go na liście. |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than listing them. | Na tym komputerze InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej, więc zatrzymał {0} {1} zamiast pokazać je na liście. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. The unneeded files above are unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Re-scan to try again. | InstallerClean nie zdołał dopasować wszystkiego, co jest w rekordach Windows, więc nie odczytał ich w całości. Niepotrzebnych plików powyżej to nie dotyczy, ale to, co mówi o plikach brakujących w {InstallerFolder}, może być niepełne. Skanuj ponownie, aby spróbować jeszcze raz. |
| {0} of {1} {2} | {0}/{1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} do wyczyszczenia ({2}) |
| {0} file left alone ({1}) | {0} plik pozostawiony bez zmian ({1}) |
| {0} files left alone ({1}) | {0} plików pozostawionych bez zmian ({1}) |

## Confirmation dialogs

| English | Polski |
| --- | --- |
| Move {0} {1} ({2})? | Przenieść {0} {1} ({2})? |
| Move to: | Przenieś do: |
| Delete {0} {1} ({2})? | Usunąć {0} {1} ({2})? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | Ten plik zostanie trwale usunięty. [Można go bezpiecznie usunąć], ale jeśli chcesz kopię zapasową, użyj zamiast tego przycisku Przenieś. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Pliki zostaną trwale usunięte. [Można je bezpiecznie usunąć], ale jeśli chcesz kopię zapasową, użyj zamiast tego przycisku Przenieś. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Ten folder jest na tym samym dysku, więc miejsce nie wróci, dopóki go nie skasujesz. Wybierz folder na innym dysku, jeśli chcesz mieć miejsce od razu. |

## Error messages

| English | Polski |
| --- | --- |
| Access denied | Odmowa dostępu |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows odmówił InstallerClean dostępu, więc program się zatrzymał. Nic nie zostało usunięte.<br><br>InstallerClean działał już jako administrator, więc ponowne uruchomienie go w ten sposób nic nie da. Windows nie mówi nic więcej o tym, co odmówiło dostępu, więc nie ma nic konkretnego do spróbowania. |
| Couldn't read the Windows Installer records | Nie udało się odczytać rekordów Windows Installera |
| Scan failed | Skanowanie nie powiodło się |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Rekordy Windows Installera wróciły całkowicie puste: ani jeden zainstalowany program ani jedna aktualizacja nie rości sobie prawa do żadnego pliku instalacyjnego w pamięci podręcznej. Na działającym komputerze to się nie zdarza (nawet świeża instalacja Windows ma takie pliki), więc albo rekordy są uszkodzone, albo nie dało się ich odczytać, a skanowanie, które uwierzyłoby w tę odpowiedź, błędnie uznałoby każdy plik w {InstallerFolder} za osierocony. InstallerClean zamiast tego się zatrzymał. Nic nie zostało usunięte. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer nie pozwolił InstallerClean wypisać tego, co jest zainstalowane. InstallerClean działał już jako administrator, więc uruchomienie go ponownie jako administrator niczego nie zmieni. Bez tej listy nie da się bezpiecznie stwierdzić, które pliki w pamięci podręcznej są nadal potrzebne, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer nie zdołał przekazać InstallerClean czytelnej listy zainstalowanych programów: {0} wpisów z rzędu wróciło nieczytelnych (ostatni kod błędu {1}). Zamiast pracować na liście odczytanej tylko częściowo, InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer nigdy nie zasygnalizował końca listy zainstalowanych programów: InstallerClean poddał się po {0} wpisach (ostatni kod błędu {1}). Liście bez końca nie można ufać, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer nigdy nie zasygnalizował końca listy poprawek jednego programu: InstallerClean poddał się po {0} wpisach (ostatni kod błędu {1}). Liście bez końca nie można ufać, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean nie zdołał dopasować rekordów Instalatora Windows do zawartości {InstallerFolder}. Prawie nic z tego, na co wskazują rekordy, tam nie ma, i prawie nic z tego, co tam jest, nie jest wskazane przez żaden rekord, więc o żadnym pliku nie dało się wykazać, że jest niepotrzebny. Niczego nie zaproponowano i niczego nie usunięto. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean nie zdołał dopasować rekordów Instalatora Windows do zawartości {InstallerFolder}. W folderze są pliki, ale ani jeden rekord nie wskazuje niczego w środku, więc o żadnym pliku nie dało się wykazać, że jest niepotrzebny. Niczego nie zaproponowano i niczego nie usunięto. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean nie zdołał odczytać dość rekordów Windows Installera, by mieć pewność, co jest jeszcze potrzebne: lista zainstalowanych programów wróciła niepełna, a odczyt tych samych rekordów prosto z rejestru również napotkał błędy. Plik mógłby wyglądać na osierocony tylko dlatego, że rekord, który go wymienia, był jednym z nieczytelnych, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean nie zdołał skłonić systemu Windows do rozwinięcia prawdziwej ścieżki {InstallerFolder}, więc o żadnym pliku nie dało się wykazać, że jest w środku, i żadnego nie zaproponowano do wyczyszczenia. To skanowanie niczego nie znalazło dlatego, że ta kontrola się nie powiodła, a nie dlatego, że folder jest czysty. Niczego nie usunięto. |
| Nothing was deleted | Niczego nie usunięto |
| Nothing was moved | Niczego nie przeniesiono |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean nie zdołał przejąć blokady, którą Instalator Windows powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy, i niczego nie usunięto. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean nie zdołał przejąć blokady, którą Instalator Windows powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy, i niczego nie przeniesiono. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie. |
| Invalid destination | Nieprawidłowy folder docelowy |
| Could not write to destination | Nie udało się zapisać w folderze docelowym |
| Move failed | Przenoszenie nie powiodło się |
| Delete failed | Usuwanie nie powiodło się |
| Setting not saved | Ustawienie nie zostało zapisane |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Nie udało się zapisać zmiany. Przy następnym uruchomieniu InstallerClean wróci do poprzedniego ustawienia. |
| The destination cannot be inside the Windows Installer folder. | Folder docelowy nie może znajdować się wewnątrz folderu Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Miejsce docelowe {0} rozwija się wewnątrz folderu systemowego Windows. Wybierz ścieżkę poza %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% i %ProgramData%. |
| Not enough space | Za mało miejsca |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Za mało miejsca w {0}<br><br>Wymagane: {1}<br>Dostępne: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | Nie masz uprawnień do zapisu w {0}.<br>Wypróbuj folder w swoim profilu użytkownika lub na własnym dysku. |
| The path {0} is too long for Windows. Pick a shorter path. | Ścieżka {0} jest za długa dla Windows. Wybierz krótszą ścieżkę. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | Folder {0} nie istnieje i nie udało się go utworzyć. Sprawdź literę dysku lub ścieżkę sieciową. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows nie może zapisać w {0}.<br>Szczegóły w {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows nie może zapisać w {0}. Nie udało się zapisać dziennika awarii. |
| Cannot write to {0}.<br>Details in {1}. | Nie można zapisać w {0}.<br>Szczegóły w {1}. |
| Cannot write to {0}. The crash log could not be written. | Nie można zapisać w {0}. Nie udało się zapisać dziennika awarii. |
| File no longer exists. | Plik już nie istnieje. |
| Source file is a symlink or junction; refused for safety. | Plik źródłowy jest dowiązaniem symbolicznym lub złączem (junction); odrzucono ze względów bezpieczeństwa. |
| This file is not directly inside the Windows Installer folder; refused for safety. | Ten plik nie znajduje się bezpośrednio w folderze Windows Installer; odrzucono ze względów bezpieczeństwa. |
| Windows refused access to this file; it was left in place. | Windows odmówił dostępu do tego pliku; został pozostawiony na miejscu. |
| Windows refused access to these files; they were left in place. | Windows odmówił dostępu do tych plików; zostały pozostawione na miejscu. |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | Ten plik jest otwarty lub zablokowany przez inny program, więc na razie nic go nie usunie. Został na miejscu; spróbuj później. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | Te pliki są otwarte lub zablokowane przez inny program, więc na razie nic ich nie usunie. Zostały na miejscu; spróbuj później. |
| Windows reported a file error; the file was left in place. | Windows zgłosił błąd pliku; plik został pozostawiony na miejscu. |
| Windows reported file errors; these files were left in place. | Windows zgłosił błędy plików; te pliki zostały pozostawione na miejscu. |
| Something went wrong with this file; it was left in place. | Coś poszło nie tak z tym plikiem; został pozostawiony na miejscu. |
| Something went wrong with these files; they were left in place. | Coś poszło nie tak z tymi plikami; zostały pozostawione na miejscu. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Odmowa przeniesienia plików do folderu Windows Installer (cel: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Folder docelowy musi być pełną ścieżką do folderu, zaczynającą się od litery dysku albo udziału sieciowego (na przykład D:\Backup albo \\serwer\backup). InstallerClean nie może użyć tej: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean nie mógł już potwierdzić folderu docelowego, więc zatrzymał się, zamiast zapisać w złym miejscu. Sprawdź {0}, potem Skanuj ponownie i spróbuj jeszcze raz. |
| Cannot write to {0}. | Nie można zapisać w {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Nie udało się znaleźć unikalnej nazwy pliku dla „{0}” po 10 000 prób. |

## Update check

| English | Polski |
| --- | --- |
| Check for updates | Sprawdź aktualizacje |
| Checking... | Sprawdzanie... |
| Up to date. | Wszystko aktualne. |
| Version {0} is available. | Dostępna jest wersja {0}. |
| Update available | Dostępna aktualizacja |
| You're running version {0}.<br>Version {1} is available. | Masz zainstalowaną wersję {0}.<br>Dostępna jest wersja {1}. |
| Couldn't reach GitHub. Check your internet connection and try again. | Nie udało się połączyć z GitHub. Sprawdź połączenie internetowe i spróbuj ponownie. |
| GitHub returned an error response. Try again in a few minutes. | GitHub zwrócił odpowiedź z błędem. Spróbuj ponownie za kilka minut. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Odpowiedź GitHub nie zawierała rozpoznawalnego wydania. Spróbuj ponownie później lub otwórz stronę wydań bezpośrednio. |
| The check timed out. Your connection to GitHub may be slow; try again. | Upłynął limit czasu sprawdzania. Połączenie z GitHub może być wolne; spróbuj ponownie. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | Sprawdzanie nie powiodło się z nieznanej przyczyny. Szczegóły są w crash.log, jeśli chcesz to zgłosić. |

## Opening links in your browser

| English | Polski |
| --- | --- |
| Couldn't open your browser | Nie udało się otworzyć przeglądarki |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean nie mógł otworzyć przeglądarki. Link jest w schowku, więc możesz wkleić go samodzielnie:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean nie mógł otworzyć przeglądarki ani skopiować linku do schowka. Oto link:<br><br>{0} |

## Sending the summary

| English | Polski |
| --- | --- |
| Sending... | Wysyłanie... |
| Thanks! Report sent. | Dzięki! Raport wysłany. |
| Sending failed. Try again later. | Wysyłanie nie powiodło się. Spróbuj ponownie później. |
| No report to send. | Brak raportu do wysłania. |
| Send this? | Wysłać to? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | Trafia do nofaff.netlify.app/api/result-log. Nic nie identyfikuje ciebie ani twojego komputera; to po prostu daje mi znać, że InstallerClean działa i [ile miejsca ludzie zwalniają]. |

## Startup and crashes

| English | Polski |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean jest już uruchomiony. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Wystąpił nieoczekiwany błąd i InstallerClean musi się zamknąć.<br><br>{0}<br><br>Szczegóły zapisano w:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Wystąpił nieoczekiwany błąd i InstallerClean musi się zamknąć.<br><br>{0}<br><br>Nie udało się zapisać dziennika awarii. |
| Startup error | Błąd uruchamiania |
| Failed to start ({0}). Details written to:<br>{1} | Nie udało się uruchomić ({0}). Szczegóły zapisano w:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Nie udało się uruchomić ({0}). Nie udało się zapisać dziennika awarii. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log zbiera nieobsłużone wyjątki InstallerClean.<br># Przy podwyższonych uprawnieniach komunikaty wyjątków platformy mogą<br># zawierać ścieżki plików z bieżącej sesji (w tym profile innych<br># użytkowników wyliczone przez zapytania Instalatora Windows).<br># Komunikaty o błędach sieci przy sprawdzaniu aktualizacji lub wysyłce<br># dziennika wyników mogą zawierać docelowy adres URL oraz rozwiązany<br># adres IP albo adres serwera proxy. Wpisy o nieczytelnych rekordach<br># Instalatora Windows mogą zawierać identyfikator SID konta Windows<br># (S-1-5-21-...) i kody produktów zainstalowanego oprogramowania.<br># Usuń wszystkie trzy rodzaje danych, zanim dołączysz ten plik do<br># publicznego zgłoszenia błędu.<br> |

## Tooltips (hover text)

| English | Polski |
| --- | --- |
| It's thirsty work! | Aż zaschło w gardle! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Zażądano anulowania. InstallerClean czeka, aż bieżący krok dojdzie do punktu, w którym może się zatrzymać. Przy intensywnych operacjach wejścia/wyjścia lub zapytaniu do bazy danych MSI może to potrwać kilka sekund. |
| Close | Zamknij |
| A star helps other people find it. | Gwiazdka pomaga innym znaleźć InstallerClean. |
| Minimise | Minimalizuj |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Twoja decyzja, ale będzie miło. Wysyła anonimowe podsumowanie, które po prostu daje mi znać, czy działa i ile miejsca ludzie zwalniają. Na następnym ekranie zobaczysz, co zostanie wysłane, zanim potwierdzisz. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Twoja decyzja, ale będzie miło. Wysyła anonimowe podsumowanie, które po prostu daje mi znać, czy działa. Na następnym ekranie zobaczysz, co zostanie wysłane, zanim potwierdzisz. |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Przenosi niepotrzebne pliki do folderu docelowego. Skasuj ten folder, gdy nabierzesz pewności, że nic ich nie potrzebuje. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Przenosi niepotrzebne pliki do folderu docelowego. Wybierzesz go za chwilę. Skasuj ten folder, gdy nabierzesz pewności, że nic ich nie potrzebuje. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Przenosi niepotrzebne pliki do folderu docelowego. Jest on na tym samym dysku, więc miejsce odzyskasz dopiero po skasowaniu tego folderu albo przeniesieniu go na inny dysk. Możesz to zrobić, gdy nabierzesz pewności, że nic ich nie potrzebuje. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Trwale usuwa niepotrzebne pliki. Można je bezpiecznie skasować, a miejsce odzyskasz od razu. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nazwa podmiotu z osadzonego certyfikatu Authenticode. Łańcuch nie został zweryfikowany. |
| Change language. The program will restart. | Zmień język. Program zostanie ponownie uruchomiony. |

## Screen reader labels

| English | Polski |
| --- | --- |
| Donate | Wesprzyj |
| Buy me a cuppa | Postaw mi kawę |
| Cancel operation | Anuluj operację |
| Cancel scan | Anuluj skanowanie |
| Cancel startup scan | Anuluj skanowanie startowe |
| Close | Zamknij |
| Close window | Zamknij okno |
| Close result and return to main window | Zamknij wynik i wróć do okna głównego |
| Leave a star on github | Zostaw gwiazdkę na githubie |
| Minimise | Minimalizuj |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Usuń trwale usuwa niepotrzebne pliki. Anuluj zamyka okno bez usuwania. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Przenieś umieszcza niepotrzebne pliki w wybranym folderze docelowym. Anuluj zostawia je na miejscu. |
| Say thanks | Podziękuj |
| Send posts the report shown to No Faff. Cancel sends nothing. | Wyślij przekazuje pokazany raport do No Faff. Anuluj nie wysyła niczego. |
| Check for updates | Sprawdź aktualizacje |
| Checks github's releases page for a newer version. | Sprawdza na stronie wydań githuba, czy jest nowsza wersja. |
| Opens the readme on github in your browser. | Otwiera readme na githubie w twojej przeglądarce. |
| Opens the issue tracker on github.com in your browser. | Otwiera listę zgłoszeń (Issues) na github.com w twojej przeglądarce. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Jeśli zaznaczone, InstallerClean przy uruchomieniu sprawdza na githubie, czy jest nowsza wersja. |
| Open the release page to download the newer version, or cancel to keep the current version. | Otwórz stronę wydania, aby pobrać nowszą wersję, lub anuluj, aby zachować bieżącą. |
| Opens the licence file on github.com in your browser. | Otwiera plik licencji na github.com w twojej przeglądarce. |
| Backup folder | Folder docelowy |
| Patches | Poprawki |
| Product details | Szczegóły produktu |
| Backup folder | Folder docelowy |
| Operation progress | Postęp operacji |
| Scan {InstallerFolder} again | Skanuj ponownie {InstallerFolder} |
| Scanning progress | Postęp skanowania |
| Startup scan progress | Postęp skanowania startowego |
| Details, unneeded files | Szczegóły, niepotrzebne pliki |
| Available for cleanup. | Dostępne do wyczyszczenia. |
| Details, files left alone | Szczegóły, pliki pozostawione bez zmian |
| Read-only inventory. | Lista tylko do odczytu. |
| Sorted by {0}, ascending | Posortowano według {0}, rosnąco |
| Sorted by {0}, descending | Posortowano według {0}, malejąco |
| Scan results | Wyniki skanowania |
| Result details | Szczegóły wyniku |
| File details | Szczegóły pliku |
| Product details | Szczegóły produktu |
| Dialog text | Tekst okna dialogowego |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Pliki, których nie udało się przetworzyć |
| Explains this folder, and how to recover a file, in the README | Wyjaśnia ten folder i sposób odzyskania pliku w README |
| Report preview | Podgląd raportu |
| Change language | Zmień język |
| The program will restart. | Program zostanie ponownie uruchomiony. |

## File picker

| English | Polski |
| --- | --- |
| Choose destination folder for moved files | Wybierz folder docelowy dla przeniesionych plików |

## Version

| English | Polski |
| --- | --- |
| Version {0} | Wersja {0} |

## Word forms (singular and plural)

| English | Polski |
| --- | --- |
| file | plik |
| files | plików |
| error | błąd |
| errors | błędów |
| package | pakiet |
| packages | pakietów |
| product | produkt |
| products | produktów |
| patch | poprawka |
| patches | poprawek |

## Sizes and times

| English | Polski |
| --- | --- |
| ,  | ,  |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | mniej niż sekunda |
| {0:F1} seconds | {0:F1} sekundy |

## Command-line tool (installerclean-cli)

| English | Polski |
| --- | --- |
| Error: unknown argument '{0}' | Błąd: nieznany argument „{0}” |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Błąd: nieoczekiwany dodatkowy argument „{0}”. Jeśli ścieżka folderu przenoszenia zawiera spację, ujmij całą ścieżkę w cudzysłów: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Błąd: nieoczekiwany dodatkowy argument „{0}”. /s i /d nie przyjmują dalszych argumentów, a w jednym uruchomieniu można użyć tylko jednego przełącznika. |
| Cancelling... | Anulowanie... |
| Cancelled. | Anulowano. |
| Error: unexpected failure ({0}). Details written to {1}. | Błąd: nieoczekiwana awaria ({0}). Szczegóły zapisano w {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Błąd: nieoczekiwana awaria ({0}). Nie udało się zapisać dziennika awarii. |
| Scanning {InstallerFolder}... | Skanowanie {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Znaleziono {0} niepotrzebnych {1} do wyczyszczenia ({2}). |
| Found no unneeded files. | Nie znaleziono niepotrzebnych plików. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back the one file ({2}) it might otherwise have offered. | InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej, więc zatrzymał jedyny plik ({2}), który inaczej by zaproponował. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back all {0} {1} ({2}) it might otherwise have offered. | InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej, więc zatrzymał wszystkie {0} {1} ({2}), które inaczej by zaproponował. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Running that program's installer again, preferably the same version, usually restores the file. | Windows ma rekord dla {0} pliku, którego nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawia to kłopotu, ale naprawa, aktualizacja lub odinstalowanie może się przez niego nie powieść. Ponowne uruchomienie instalatora tego programu, najlepiej w tej samej wersji, zwykle przywraca plik. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Running each program's installer again, preferably the same version, usually restores the files. | Windows ma rekordy dla {0} plików, których nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawiają one kłopotu, ale naprawa, aktualizacja lub odinstalowanie może się przez nie nie powieść. Ponowne uruchomienie instalatora każdego z tych programów, najlepiej w tej samej wersji, zwykle przywraca pliki. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. What it found is unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Running it again may pick up more. | InstallerClean nie zdołał dopasować wszystkiego, co jest w rekordach Windows, więc nie odczytał ich w całości. Tego, co znalazł, to nie dotyczy, ale to, co mówi o plikach brakujących w {InstallerFolder}, może być niepełne. Ponowne uruchomienie może wykryć więcej. |
| Deleting {0} unneeded {1}... | Trwa usuwanie: {0} niepotrzebnych {1}... |
| Permanently deleted {0} unneeded {1}. | Trwale usunięto {0} niepotrzebnych {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Błąd: nie podano folderu docelowego przenoszenia. Użyj /m ŚCIEŻKA. (Lokalizacja domyślna ustawiona w GUI jest przypisana do użytkownika i nie dotyczy uruchomień zaplanowanych ani na koncie usługi.) |
| Error: destination cannot be inside the Windows Installer folder. | Błąd: folder docelowy nie może znajdować się wewnątrz folderu Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Błąd: folder docelowy musi być pełną ścieżką. Otrzymano: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Błąd: miejsce docelowe {0} rozwija się wewnątrz folderu systemowego Windows. Wybierz ścieżkę poza %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% i %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Błąd: za mało miejsca w {0}. Przeniesienie tych plików wymaga {1}, a wolne jest {2}. Niczego nie przeniesiono. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Błąd: coś właśnie korzysta z Instalatora Windows, na przykład aktualizacja systemu albo program instalujący się w tle. /m i /d są zablokowane, dopóki to trwa. Spróbuj ponownie, gdy się skończy. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Błąd: na tym komputerze jest wstrzymana wcześniejsza transakcja Instalatora Windows. Wznów ją lub wycofaj tę instalację (albo uruchom system ponownie), zanim wyczyścisz {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Błąd: zakolejkowana na po ponownym uruchomieniu operacja na pliku dotyczy {InstallerFolder} ({0}). Uruchom system ponownie, aby ją dokończyć, zanim wyczyścisz. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Błąd: Instalator Windows ma coś w toku, więc /m i /d są zablokowane. InstallerClean nie ruszy {InstallerFolder} w trakcie zmian. Spróbuj ponownie, gdy się skończy. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Błąd: InstallerClean nie zdołał przejąć blokady Instalatora Windows, która powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy. Niczego nie usunięto. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Błąd: InstallerClean nie zdołał przejąć blokady Instalatora Windows, która powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy. Niczego nie przeniesiono. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie. |
| Moving {0} unneeded {1} to {2}... | Trwa przenoszenie: {0} niepotrzebnych {1} do {2}... |
| Moved {0} unneeded {1}. | Przeniesiono {0} niepotrzebnych {1}. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean nie mógł już potwierdzić folderu docelowego, więc zatrzymał się, zamiast zapisać w złym miejscu. Sprawdź {0}, a potem uruchom polecenie ponownie. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Inny proces InstallerClean trzyma blokadę pojedynczej instancji (GUI lub inne uruchomienie CLI). Kod zakończenia 75 (stan przejściowy); można bezpiecznie spróbować ponownie później. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Uwaga: zapis do dziennika zdarzeń nie powiódł się. Sprawdź uprawnienia dziennika „Aplikacja” lub zasady grupy. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - oczyszczanie {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Usuwa pliki .msi i .msp z pamięci podręcznej, zbędne każdemu programowi. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Wymaga wiersza polecenia administratora; inaczej Windows go nie uruchomi. |
| Usage: | Sposób użycia: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help      Pokaż tę pomoc (akceptuje też /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version   Wypisz wersję (akceptuje też -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s          Tylko skanuj - lista niepotrzebnych |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d          Trwale usuń niepotrzebne pliki |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m          Przenieś do zapisanego folderu |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ŚCIEŻKA  Przenieś do wskazanej ścieżki |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blokuje wiersz polecenia aż do końca, więc skrypt<br>albo zadanie zaplanowane może na niego zaczekać. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Folder zapisuje się per użytkownik; zadania wymagają /m ŚCIEŻKA. |
| Exit codes: | Kody zakończenia: |
|   0   success: the run did what it was asked and nothing failed |   0   sukces: zrobił to, o co poproszono, i nic nie zawiodło |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   niepowodzenie: nic nie przetworzono (złe argumenty, złe miejsce<br>       docelowe, nieudane skanowanie albo każdy plik z błędem) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   częściowo: część przetworzona, część nie (błąd albo Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  stan przejściowy: coś zablokowało uruchomienie (zob. komunikat) |
|   130 cancelled (Ctrl+C) |   130 anulowano (Ctrl+C) |
