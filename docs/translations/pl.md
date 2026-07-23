# InstallerClean in Polski (Polish)

The text of InstallerClean's interface and command-line tool in English on the left, with the Polish translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Polish can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.pl.resx`](../../src/InstallerClean.Core/Resources/Strings.pl.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | Polski |
| --- | --- |
| InstallerClean | InstallerClean |
| About | O programie |
| Registered files that should not be deleted | Zarejestrowane pliki, których nie należy usuwać |
| Unneeded files that are safe to delete | Niepotrzebne pliki, które można bezpiecznie usunąć |
| Confirm move | Potwierdzenie przeniesienia |
| Confirm delete | Potwierdzenie usunięcia |
| Recycle Bin unavailable | Kosz niedostępny |

## Section headings

| English | Polski |
| --- | --- |
| PRODUCTS | PRODUKTY |
| PATCHES | POPRAWKI |
| PRODUCT DETAILS | SZCZEGÓŁY PRODUKTU |
| MOVE LOCATION | LOKALIZACJA PRZENOSZENIA |
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
| _Delete | _Usuń |
| _Delete permanently | Usuń _trwale |
| _Done | _Gotowe |
| Details | Szczegóły |
| _Buy me a cuppa | Postaw mi _kawę |
| Leave a _star on GitHub | Zostaw _gwiazdkę na GitHubie |
| Apache 2.0 licence | Licencja Apache 2.0 |
| _Move | _Przenieś |
| _Move instead | _Przenieś zamiast tego |
| Path to folder if you Move instead of Delete | Ścieżka do folderu, jeśli zamiast usuwać wybierzesz Przenieś |
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
| Checking the Recycle Bin... | Sprawdzanie Kosza... |
| Moving {0} {1}... | Przenoszenie: {0} {1}... |
| Deleting {0} {1}... | Usuwanie: {0} {1}... |
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
| Any unneeded files below are safe to delete. | Wszelkie niepotrzebne pliki poniżej można bezpiecznie usunąć. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Leżą w C:\Windows\Installer, pozostawione po odinstalowaniu programu ({0}), gdy nowsza poprawka zastąpiła jedną z nich ({1}) lub gdy wydawca ją wycofał ({2}). InstallerClean wymienia wyłącznie pliki, które sam Windows zgłasza jako już niepotrzebne. |
| Delete them to the Recycle Bin, or use Move instead to keep a backup. Putting the files back in C:\Windows\Installer returns you to exactly where you started. | Usuń je do Kosza albo użyj zamiast tego Przenieś, aby zachować kopię zapasową. Umieszczenie plików z powrotem w C:\Windows\Installer cofa wszystko dokładnie do punktu wyjścia. |
| Nothing scanned yet. | Jeszcze nic nie przeskanowano. |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | Naciśnij przycisk Skanuj ponownie, aby przejrzeć C:\Windows\Installer w poszukiwaniu plików instalatora, których żaden program już nie potrzebuje. |
| These files can't be cleaned up right now. | Tych plików nie można teraz wyczyścić. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | Coś właśnie korzysta z Windows Installer, zwykle aktualizacja Windows albo program instalujący się w tle. Na ten czas Przenieś i Usuń są wstrzymane, aby InstallerClean nie ruszał pamięci podręcznej instalatora, gdy ta się zmienia. Gdy to się zakończy, skanuj ponownie, a wrócą. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Na tym komputerze zawieszona jest wcześniejsza transakcja Windows Installer. Przed wyczyszczeniem pamięci podręcznej dokończ lub wycofaj tamtą instalację (albo uruchom ponownie Windows). |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows ma w kolejce na następne uruchomienie zmianę nazwy pliku, która dotyczy pamięci podręcznej instalatora. Przed czyszczeniem uruchom ponownie Windows. |
| Select a file to view details. | Wybierz plik, aby zobaczyć szczegóły. |
| Select a product to view details. | Wybierz produkt, aby zobaczyć szczegóły. |
| No metadata available. | Brak dostępnych metadanych. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | Ten plik instalatora został usunięty. InstallerClean tego nie zrobił, nigdy nie usuwa pliku, którego program wciąż potrzebuje; ten usunęło coś innego, zanim uruchomiono InstallerClean.<br><br>Na razie nie sprawia to żadnych kłopotów i nie sprawi, dopóki pewnego dnia nie spróbujesz naprawić, zaktualizować lub odinstalować programu, do którego należy. Ten krok może się wtedy nie powieść, bo Windows szuka tego pliku, a jego nie ma.<br><br>Aby spróbować to naprawić, pobierz instalator tego programu od jego producenta i uruchom go na istniejącej kopii (nie odinstalowuj wcześniej, odinstalowanie samo w sobie jest krokiem, który potrzebuje tego pliku). Jeśli możesz, użyj wersji, którą masz zainstalowaną, bo Windows może odrzucić inną. Zwykle przywraca to plik, a twoje ustawienia zazwyczaj pozostają nietknięte, ale Microsoft tego nie gwarantuje, jego własną ostatecznością jest ponowna instalacja programu lub samego Windows. |
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
| Nothing to clean up in C:\Windows\Installer | Nie ma czego czyścić w C:\Windows\Installer |
| Scanned {0} {1} in {2} | Przeskanowano {0} {1} w {2} |
| Copy them back to C:\Windows\Installer if anything ever breaks ([extremely unlikely]). | Skopiuj je z powrotem do C:\Windows\Installer, gdyby coś się kiedykolwiek zepsuło ([skrajnie mało prawdopodobne]). |
| Until then, you can restore them if anything ever breaks ([extremely unlikely]). | Do tego czasu możesz je przywrócić, gdyby coś się kiedykolwiek zepsuło ([skrajnie mało prawdopodobne]). |
| Empty it to actually reclaim the space. | Opróżnij go, aby naprawdę odzyskać miejsce. |
| {0} freed | Zwolniono {0} |
| {0} cleaned up | Wyczyszczono {0} |
| {0} moved | Przeniesiono {0} |
| Nothing was moved | Niczego nie przeniesiono |
| Nothing was deleted | Niczego nie usunięto |
| {0} of {1} could not be moved. | Nie udało się przenieść {0} pliku z {1}. |
| {0} of {1} could not be moved. | Nie udało się przenieść {0} plików z {1}. |
| {0} of {1} could not be deleted. | Nie udało się usunąć {0} pliku z {1}. |
| {0} of {1} could not be deleted. | Nie udało się usunąć {0} plików z {1}. |
| {0} {1} moved to: {2} | Przeniesiono {0} {1} do: {2} |
| {0} {1} moved to: {2} | Przeniesiono {0} {1} do: {2} |
| {0} {1} moved to the Recycle Bin | Przeniesiono {0} {1} do Kosza |
| {0} {1} moved to the Recycle Bin | Przeniesiono {0} {1} do Kosza |
| {0} {1} kept in place, because a program started needing them again after the scan. | Pozostawiono na miejscu {0} {1}, ponieważ po skanowaniu program znów zaczął ich potrzebować. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | Pozostawiono na miejscu {0} {1}, ponieważ przy powtórzeniu sprawdzenia nie udało się w pełni odczytać rekordów Windows Installera. |
| Moved {0} of {1} {2} before you cancelled. | Przed anulowaniem przeniesiono {0}/{1} {2}. |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | Przed anulowaniem przeniesiono {0}/{1} {2} do Kosza. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Przed anulowaniem usunięto trwale {0}/{1} {2}. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | Usunięto trwale {0} {1}. Nie trafił do Kosza. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | Usunięto trwale {0} {1}. Nie trafiło do Kosza. |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | W porządku, można go było bezpiecznie usunąć. InstallerClean usuwa tylko pliki, które Windows zgłasza jako już niepotrzebne, nigdy takiego, którego program wciąż potrzebuje. W mało prawdopodobnym przypadku, gdyby usunięcie kiedykolwiek pozbawiło program możliwości naprawy, aktualizacji lub odinstalowania, ponowna instalacja od producenta zwykle przywraca plik, choć Microsoft tego nie gwarantuje. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | W porządku, można je było bezpiecznie usunąć. InstallerClean usuwa tylko pliki, które Windows zgłasza jako już niepotrzebne, nigdy takiego, którego program wciąż potrzebuje. W mało prawdopodobnym przypadku, gdyby usunięcie kiedykolwiek pozbawiło program możliwości naprawy, aktualizacji lub odinstalowania, ponowna instalacja od producenta zwykle przywraca plik, choć Microsoft tego nie gwarantuje. |
| Glad to help. There's a tip jar if you're feeling kind. | Cieszę się, że pomogło. Jeśli masz dobre serce, jest miejsce na napiwek. |

## Recycle Bin unavailable

| English | Polski |
| --- | --- |
| The Recycle Bin isn't available for this drive | Kosz nie jest dostępny dla tego dysku |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Dlatego ten {1} ({2}) nie został usunięty. Możesz przenieść go w bezpieczne miejsce lub usunąć trwale. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Dlatego te {0} {1} ({2}) nie zostało usuniętych. Możesz przenieść je w bezpieczne miejsce lub usunąć trwale. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Można go bezpiecznie usunąć. InstallerClean usuwa tylko pliki, które Windows zgłasza jako już niepotrzebne, nigdy takiego, którego program wciąż potrzebuje, a Kosz to tylko dodatkowe zabezpieczenie. W mało prawdopodobnym przypadku, gdyby usunięcie kiedykolwiek pozbawiło program możliwości naprawy, aktualizacji lub odinstalowania, ponowna instalacja od producenta zwykle przywraca plik, choć Microsoft tego nie gwarantuje. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Można je bezpiecznie usunąć. InstallerClean usuwa tylko pliki, które Windows zgłasza jako już niepotrzebne, nigdy takiego, którego program wciąż potrzebuje, a Kosz to tylko dodatkowe zabezpieczenie. W mało prawdopodobnym przypadku, gdyby usunięcie kiedykolwiek pozbawiło program możliwości naprawy, aktualizacji lub odinstalowania, ponowna instalacja od producenta zwykle przywraca plik, choć Microsoft tego nie gwarantuje. |

## Summaries and counts

| English | Polski |
| --- | --- |
| {0} file still needed | {0} plik nadal potrzebny |
| {0} files still needed | {0} plików nadal potrzebnych |
| {0} unneeded file to clean up | {0} niepotrzebny plik do wyczyszczenia |
| {0} unneeded files to clean up | {0} niepotrzebnych plików do wyczyszczenia |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | Brakuje {0} zarejestrowanego pliku (nie usunął go InstallerClean). Na razie to nie problem, ale w przyszłości naprawa, aktualizacja lub odinstalowanie tego programu mogą się nie powieść. Otwórz Szczegóły, aby dowiedzieć się, co zrobić. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | Brakuje {0} zarejestrowanych plików (nie usunął ich InstallerClean). Na razie to nie problem, ale w przyszłości naprawa, aktualizacja lub odinstalowanie tych programów mogą się nie powieść. Otwórz Szczegóły, aby dowiedzieć się, co zrobić. |
| {0} installed program could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | Podczas tego skanowania nie udało się odczytać {0} zainstalowanego programu, więc zastąpione poprawki zostały zachowane. Nie dotyczy to plików osieroconych. |
| {0} installed programs could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | Podczas tego skanowania nie udało się odczytać {0} zainstalowanych programów, więc zastąpione poprawki zostały zachowane. Nie dotyczy to plików osieroconych. |
| {0} of {1} {2} | {0}/{1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} osieroconych, {1} zastąpionych, {2} przestarzałych ({3}) |
| {0} registered file that is still needed ({1}) | {0} zarejestrowany plik nadal potrzebny ({1}) |
| {0} registered files that are still needed ({1}) | {0} zarejestrowanych plików nadal potrzebnych ({1}) |

## Confirmation dialogs

| English | Polski |
| --- | --- |
| Move {0} {1} ({2})? | Przenieść {0} {1} ({2})? |
| Files will be moved to: | Pliki zostaną przeniesione do: |
| Delete {0} {1} ({2})? | Usunąć {0} {1} ({2})? |
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | Pliki zostaną przeniesione do Kosza. Jeśli chcesz mieć kopie zapasowe, użyj zamiast tego przycisku Przenieś. |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | Ten folder jest na tym samym dysku, więc samo przeniesienie nie zwolni miejsca. Odzyskasz je, gdy usuniesz z niego pliki, albo możesz zamiast tego wybrać folder na innym dysku. |

## Error messages

| English | Polski |
| --- | --- |
| Access denied | Odmowa dostępu |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows odmówił InstallerClean dostępu, więc program się zatrzymał. Nic nie zostało usunięte.<br><br>InstallerClean działał już jako administrator, więc ponowne uruchomienie go w ten sposób nic nie da. Windows nie mówi nic więcej o tym, co odmówiło dostępu, więc nie ma nic konkretnego do spróbowania. |
| Couldn't read the Windows Installer records | Nie udało się odczytać rekordów Windows Installera |
| Scan failed | Skanowanie nie powiodło się |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in C:\Windows\Installer orphaned. InstallerClean stopped instead. Nothing has been removed. | Rekordy Windows Installera wróciły całkowicie puste: ani jeden zainstalowany program ani jedna aktualizacja nie rości sobie prawa do żadnego pliku instalacyjnego w pamięci podręcznej. Na działającym komputerze to się nie zdarza (nawet świeża instalacja Windows ma takie pliki), więc albo rekordy są uszkodzone, albo nie dało się ich odczytać, a skanowanie, które uwierzyłoby w tę odpowiedź, błędnie uznałoby każdy plik w C:\Windows\Installer za osierocony. InstallerClean zamiast tego się zatrzymał. Nic nie zostało usunięte. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer nie pozwolił InstallerClean wypisać tego, co jest zainstalowane. InstallerClean działał już jako administrator, więc uruchomienie go ponownie jako administrator niczego nie zmieni. Bez tej listy nie da się bezpiecznie stwierdzić, które pliki w pamięci podręcznej są nadal potrzebne, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer nie zdołał przekazać InstallerClean czytelnej listy zainstalowanych programów: {0} wpisów z rzędu wróciło nieczytelnych (ostatni kod błędu {1}). Zamiast pracować na liście odczytanej tylko częściowo, InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer nigdy nie zasygnalizował końca listy zainstalowanych programów: InstallerClean poddał się po {0} wpisach (ostatni kod błędu {1}). Liście bez końca nie można ufać, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer nigdy nie zasygnalizował końca listy poprawek jednego programu: InstallerClean poddał się po {0} wpisach (ostatni kod błędu {1}). Liście bez końca nie można ufać, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| InstallerClean couldn't square this scan with the Windows Installer records: every file Windows still lists as needed is missing from C:\Windows\Installer, while the files actually in the folder match nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean nie zdołał pogodzić tego skanowania z rekordami Windows Installera: każdego pliku, który Windows nadal wymienia jako potrzebny, brakuje w C:\Windows\Installer, a pliki faktycznie leżące w tym folderze nie pasują do żadnego rekordu. Żaden prawdziwy komputer tak nie wygląda, więc wskazuje to na problem z odczytem rekordów, a nie na pliki, które można bezpiecznie usunąć. Nie zaproponowano niczego do wyczyszczenia i nic nie zostało usunięte. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean nie zdołał odczytać dość rekordów Windows Installera, by mieć pewność, co jest jeszcze potrzebne: lista zainstalowanych programów wróciła niepełna, a odczyt tych samych rekordów prosto z rejestru również napotkał błędy. Plik mógłby wyglądać na osierocony tylko dlatego, że rekord, który go wymienia, był jednym z nieczytelnych, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| Invalid destination | Nieprawidłowy folder docelowy |
| Could not write to destination | Nie udało się zapisać w folderze docelowym |
| Move failed | Przenoszenie nie powiodło się |
| Delete failed | Usuwanie nie powiodło się |
| Setting not saved | Ustawienie nie zostało zapisane |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Nie udało się zapisać zmiany. Przy następnym uruchomieniu InstallerClean wróci do poprzedniego ustawienia. |
| The destination cannot be inside the Windows Installer folder. | Folder docelowy nie może znajdować się wewnątrz folderu Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Folder docelowy {0} prowadzi do folderu systemowego Windows. Wybierz ścieżkę poza %SystemRoot%, %ProgramFiles% i %ProgramData%. |
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
| This file is open or locked by another program, so nothing can move it just now. It was left in place; try again later. | Ten plik jest otwarty lub zablokowany przez inny program, więc nic nie może go teraz przenieść. Został pozostawiony na miejscu; spróbuj ponownie później. |
| These files are open or locked by another program, so nothing can move them just now. They were left in place; try again later. | Te pliki są otwarte lub zablokowane przez inny program, więc nic nie może ich teraz przenieść. Zostały pozostawione na miejscu; spróbuj ponownie później. |
| Windows reported a file error; the file was left in place. | Windows zgłosił błąd pliku; plik został pozostawiony na miejscu. |
| Windows reported file errors; these files were left in place. | Windows zgłosił błędy plików; te pliki zostały pozostawione na miejscu. |
| Something went wrong with this file; it was left in place. | Coś poszło nie tak z tym plikiem; został pozostawiony na miejscu. |
| Something went wrong with these files; they were left in place. | Coś poszło nie tak z tymi plikami; zostały pozostawione na miejscu. |
| Couldn't move this file to the Recycle Bin (error {0}), and InstallerClean can't tell you why from that code. The file was left in place. Try the Move button instead, since it doesn't use the Recycle Bin. | Nie udało się przenieść tego pliku do Kosza (błąd {0}), a na podstawie tego kodu InstallerClean nie potrafi powiedzieć dlaczego. Plik został pozostawiony na miejscu. Spróbuj zamiast tego przycisku Przenieś, który nie korzysta z Kosza. |
| Windows refused access even with administrator rights (error {0}), and InstallerClean can't tell whether the problem is the file or the Recycle Bin. The file was left in place. The Move button will work if it's the Recycle Bin, but not if it's the file. | Windows odmówił dostępu nawet z uprawnieniami administratora (błąd {0}), a InstallerClean nie potrafi stwierdzić, czy problemem jest plik, czy Kosz. Plik został pozostawiony na miejscu. Przycisk Przenieś zadziała, jeśli problemem jest Kosz, ale nie wtedy, gdy jest nim plik. |
| This file is open or locked by another program (error {0}), so nothing can remove it just now. It was left in place; try again later. | Ten plik jest otwarty lub zablokowany przez inny program (błąd {0}), więc nic nie może go teraz usunąć. Został pozostawiony na miejscu; spróbuj ponownie później. |
| Windows deleted this file outright rather than moving it to the Recycle Bin. InstallerClean asked for the Recycle Bin, and Windows did this instead. The file is gone. | Windows usunął ten plik trwale, zamiast przenieść go do Kosza. InstallerClean poprosił o Kosz, a Windows zrobił co innego. Pliku już nie ma. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Odmowa przeniesienia plików do folderu Windows Installer (cel: {0}). |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Lokalizacja przenoszenia musi być pełną ścieżką do folderu, zaczynającą się od litery dysku lub udziału sieciowego (na przykład D:\Backup lub \\server\backup). InstallerClean nie może użyć tej ścieżki: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | Lokalizacja przenoszenia zmieniła się w trakcie przenoszenia plików (coś podmieniło lub przekierowało ten folder), więc InstallerClean przerwał, zamiast zapisać w niewłaściwym miejscu. Sprawdź {0}, a następnie użyj przycisku Skanuj ponownie i spróbuj jeszcze raz. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log rejestruje nieobsłużone wyjątki InstallerClean.<br># Przy podwyższonych uprawnieniach komunikaty wyjątków platformy<br># mogą zawierać ścieżki plików z bieżącej sesji (w tym profile<br># innych użytkowników wyliczone przez zapytania Windows Installer).<br># Komunikaty o błędach sieci ze sprawdzania aktualizacji lub<br># wysyłania dziennika wyników mogą zawierać docelowy adres URL<br># oraz rozwiązany adres IP / proxy. Usuń oba rodzaje danych przed<br># dołączeniem tego pliku do publicznego zgłoszenia błędu.<br> |

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
| Move the unneeded files to the Move location. | Przenieś niepotrzebne pliki do lokalizacji przenoszenia. |
| Move the unneeded files somewhere safe. You'll choose the folder next. | Przenieś niepotrzebne pliki w bezpieczne miejsce. Folder wybierzesz w następnym kroku. |
| Move the unneeded files to the Recycle Bin. | Przenieś niepotrzebne pliki do Kosza. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nazwa podmiotu z osadzonego certyfikatu Authenticode. Łańcuch nie został zweryfikowany. |
| Change language. The program will restart. | Zmień język. Program zostanie ponownie uruchomiony. |

## Screen reader labels

| English | Polski |
| --- | --- |
| Donate | Wesprzyj |
| Buy me a cuppa (About window) | Postaw mi kawę (okno O programie) |
| Cancel operation | Anuluj operację |
| Cancel scan | Anuluj skanowanie |
| Cancel startup scan | Anuluj skanowanie startowe |
| Close | Zamknij |
| Close window | Zamknij okno |
| Close result and return to main window | Zamknij wynik i wróć do okna głównego |
| Leave a star on GitHub (About window) | Zostaw gwiazdkę na GitHubie (okno O programie) |
| Minimise | Minimalizuj |
| Move all unneeded installer files to the Move location | Przenieś wszystkie niepotrzebne pliki instalatora do lokalizacji przenoszenia |
| Move all unneeded installer files to the Recycle Bin | Przenieś wszystkie niepotrzebne pliki instalatora do Kosza |
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | Usuń przenosi niepotrzebne pliki do Kosza. Anuluj zamyka okno bez usuwania. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Przenieś umieszcza niepotrzebne pliki w wybranym folderze docelowym. Anuluj zostawia je na miejscu. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Wybierz, co zrobić z niepotrzebnymi plikami: przenieść je w bezpieczne miejsce, usunąć trwale lub anulować. |
| Move the unneeded files to a folder you choose | Przenieś niepotrzebne pliki do wybranego folderu |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Usuń niepotrzebne pliki trwale, ponieważ Kosz jest niedostępny dla tego dysku |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Wysyłane do nofaff.netlify.app. Tylko liczniki i etykiety. Przed wysłaniem zobaczysz dokładną treść. |
| Say thanks | Podziękuj |
| Send posts the report shown to No Faff. Cancel sends nothing. | Wyślij przekazuje pokazany raport do No Faff. Anuluj nie wysyła niczego. |
| Check for updates | Sprawdź aktualizacje |
| Checks GitHub's releases page over HTTPS for a newer version. | Sprawdza na stronie wydań GitHuba przez HTTPS, czy jest nowsza wersja. |
| Opens the guide (README) on github.com in your browser. | Otwiera przewodnik (README) na github.com w twojej przeglądarce. |
| Opens the issue tracker on github.com in your browser. | Otwiera listę zgłoszeń (Issues) na github.com w twojej przeglądarce. |
| When ticked, InstallerClean checks GitHub for a newer version when you run it. | Gdy zaznaczone, InstallerClean sprawdza na GitHubie przy każdym uruchomieniu, czy jest nowsza wersja. |
| Open the release page to download the newer version, or cancel to keep the current version. | Otwórz stronę wydania, aby pobrać nowszą wersję, lub anuluj, aby zachować bieżącą. |
| Apache 2.0 licence | Licencja Apache 2.0 |
| Opens the licence file on github.com in your browser. | Otwiera plik licencji na github.com w twojej przeglądarce. |
| Move location | Lokalizacja przenoszenia |
| Products | Produkty |
| Patches | Poprawki |
| Product details | Szczegóły produktu |
| Move location | Lokalizacja przenoszenia |
| Operation progress | Postęp operacji |
| Scan C:\Windows\Installer again | Skanuj ponownie C:\Windows\Installer |
| Scanning progress | Postęp skanowania |
| Startup scan progress | Postęp skanowania startowego |
| Details, unneeded files | Szczegóły, niepotrzebne pliki |
| Available for cleanup. | Dostępne do wyczyszczenia. |
| Details, registered files | Szczegóły, zarejestrowane pliki |
| Read-only inventory. | Lista tylko do odczytu. |
| Sorted by {0}, ascending | Posortowano według {0}, rosnąco |
| Sorted by {0}, descending | Posortowano według {0}, malejąco |
| Scan results | Wyniki skanowania |
| Result details | Szczegóły wyniku |
| File details | Szczegóły pliku |
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
| Unknown argument: '{0}' | Nieznany argument: „{0}” |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Błąd: nieoczekiwany dodatkowy argument „{0}”. Jeśli ścieżka folderu przenoszenia zawiera spację, ujmij całą ścieżkę w cudzysłów: /m "D:\My Backup" |
| Cancelling... | Anulowanie... |
| Cancelled. | Anulowano. |
| Error: {0}. Details written to {1}. | Błąd: {0}. Szczegóły zapisano w {1}. |
| Error: {0}. The crash log could not be written. | Błąd: {0}. Nie udało się zapisać dziennika awarii. |
| Scanning C:\Windows\Installer... | Skanowanie C:\Windows\Installer... |
| Found {0} {1} to clean up ({2}). | Znaleziono {0} {1} do wyczyszczenia ({2}). |
| Nothing to do. | Nie ma nic do zrobienia. |
| Deleting {0} {1}... | Usuwanie: {0} {1}... |
| Deleted {0} {1}. | Usunięto {0} {1}. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Błąd: Kosz jest niedostępny dla tego woluminu, więc nic nie usunięto. Użyj /m, aby zamiast tego przenieść pliki, albo ponownie włącz Kosz i uruchom jeszcze raz. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Błąd: nie podano folderu docelowego przenoszenia. Użyj /m ŚCIEŻKA. (Lokalizacja domyślna ustawiona w GUI jest przypisana do użytkownika i nie dotyczy uruchomień zaplanowanych ani na koncie usługi.) |
| Error: destination cannot be inside the Windows Installer folder. | Błąd: folder docelowy nie może znajdować się wewnątrz folderu Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Błąd: folder docelowy musi być pełną ścieżką. Otrzymano: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Błąd: folder docelowy {0} prowadzi do folderu systemowego Windows. Wybierz ścieżkę poza %SystemRoot%, %ProgramFiles% i %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Błąd: coś właśnie korzysta z Windows Installer, zwykle aktualizacja Windows albo program instalujący się w tle. Przenoszenie i usuwanie są zablokowane, dopóki to trwa. Spróbuj ponownie, gdy się zakończy. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Błąd: na tym komputerze zawieszona jest wcześniejsza transakcja Windows Installer. Przed wyczyszczeniem pamięci podręcznej dokończ lub wycofaj tamtą instalację (albo uruchom ponownie Windows). |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Błąd: zakolejkowana po ponownym uruchomieniu operacja na pliku dotyczy pamięci podręcznej instalatora ({0}). Przed czyszczeniem uruchom ponownie Windows, aby dokończyć tę operację. |
| Moving {0} {1} to {2}... | Przenoszenie: {0} {1} do {2}... |
| Moved {0} {1}. | Przeniesiono {0} {1}. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Inny proces InstallerClean trzyma blokadę pojedynczej instancji (GUI lub inne uruchomienie CLI). Kod wyjścia 75 (stan przejściowy); można bezpiecznie spróbować ponownie później. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Uwaga: zapis do dziennika zdarzeń nie powiódł się. Sprawdź uprawnienia dziennika „Aplikacja” lub zasady grupy. |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - oczyszczanie C:\Windows\Installer |
| Usage: | Sposób użycia: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help      Pokaż tę pomoc (akceptuje też /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version   Wypisz wersję (akceptuje też -v) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s          Tylko skanowanie - lista niepotrzebnych plików |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d          Usuń niepotrzebne pliki (Kosz) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m          Przenieś do zapisanej lokalizacji domyślnej |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ŚCIEŻKA  Przenieś do wskazanej ścieżki |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli to prawdziwy proces konsolowy i blokuje wiersz poleceń, |
| until it finishes; redirect or pipe its output as you would any | dopóki się nie zakończy; przekieruj lub przekaż potokiem jego wyjście |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | jak każdy inny program konsolowy exe. GUI jest obok, w InstallerClean.exe. |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | Zapisana lokalizacja domyślna jest przypisana do użytkownika; uruchomienia zaplanowane lub na koncie SYSTEM wymagają /m ŚCIEŻKA. |
| Exit codes: | Kody wyjścia: |
|   0   success: every flagged file was processed |   0   sukces: przetworzono każdy oznaczony plik |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   błąd: nic nie przetworzono (złe argumenty, skanowanie nie powiodło się, wszystkie pliki zawiodły) |
|   2   partial: some files processed, some failed |   2   częściowo: część plików przetworzono, część zawiodła |
|   75  transient: a temporary condition blocked the run (see the message) |   75  stan przejściowy: tymczasowy warunek zablokował uruchomienie (zob. komunikat) |
|   130 cancelled (Ctrl+C) |   130 anulowano (Ctrl+C) |
