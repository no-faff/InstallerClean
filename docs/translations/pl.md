# InstallerClean in Polski (Polish)

The text of InstallerClean's interface and command-line tool in English on the left, with the Polish translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Polish can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.pl.resx`](../../src/InstallerClean.Core/Resources/Strings.pl.resx), so do not edit it by hand. The Polish translation itself lives in [`gen-strings-pl.mjs`](../../scripts/translations/gen-strings-pl.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Polski |
| --- | --- |
| InstallerClean | InstallerClean |
| About | O programie |
| Registered files that should not be deleted | Zarejestrowane pliki, których nie należy usuwać |
| Unneeded files that are safe to delete | Niepotrzebne pliki, które można bezpiecznie usunąć |

## Section headings

| English | Polski |
| --- | --- |
| PRODUCTS | PRODUKTY |
| PATCHES | POPRAWKI |
| PRODUCT DETAILS | SZCZEGÓŁY PRODUKTU |
| BACKUP FOLDER | BACKUP FOLDER |
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
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
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
| Moving unneeded files... | Moving unneeded files... |
| Deleting unneeded files... | Deleting unneeded files... |
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
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Leżą w {InstallerFolder}, pozostawione po odinstalowaniu programu ({0}), gdy nowsza poprawka zastąpiła jedną z nich ({1}) lub gdy wydawca ją wycofał ({2}). InstallerClean wymienia wyłącznie pliki, które sam Windows zgłasza jako już niepotrzebne. |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | Jeszcze nic nie przeskanowano. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Naciśnij przycisk Skanuj ponownie, aby przejrzeć {InstallerFolder} w poszukiwaniu plików instalatora, których żaden program już nie potrzebuje. |
| These files can't be cleaned up right now. | Tych plików nie można teraz wyczyścić. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. |
| Select a file to view details. | Wybierz plik, aby zobaczyć szczegóły. |
| Select a product to view details. | Wybierz produkt, aby zobaczyć szczegóły. |
| No metadata available. | Brak dostępnych metadanych. |
| This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. |
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
| Nothing to clean up in {InstallerFolder} | Nie ma czego czyścić w {InstallerFolder} |
| Scanned {0} {1} in {2} | Przeskanowano {0} {1} w {2} |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
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
| {0} {1} kept in place, because a program went back to needing what the scan flagged. | Pozostawiono na miejscu {0} {1}, ponieważ po skanowaniu program znów zaczął ich potrzebować. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | Pozostawiono na miejscu {0} {1}, ponieważ przy powtórzeniu sprawdzenia nie udało się w pełni odczytać rekordów Windows Installera. |
| Moved {0} of {1} {2} before you cancelled. | Przed anulowaniem przeniesiono {0}/{1} {2}. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Przed anulowaniem usunięto trwale {0}/{1} {2}. |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | Cieszę się, że pomogło. Jeśli masz dobre serce, jest miejsce na napiwek. |

## Summaries and counts

| English | Polski |
| --- | --- |
| {0} file still needed | {0} plik nadal potrzebny |
| {0} files still needed | {0} plików nadal potrzebnych |
| {0} unneeded file to clean up | {0} niepotrzebny plik do wyczyszczenia |
| {0} unneeded files to clean up | {0} niepotrzebnych plików do wyczyszczenia |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | Brakuje {0} zarejestrowanego pliku (nie usunął go InstallerClean). Na razie to nie problem, ale w przyszłości naprawa, aktualizacja lub odinstalowanie tego programu mogą się nie powieść. Otwórz Szczegóły, aby dowiedzieć się, co zrobić. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | Brakuje {0} zarejestrowanych plików (nie usunął ich InstallerClean). Na razie to nie problem, ale w przyszłości naprawa, aktualizacja lub odinstalowanie tych programów mogą się nie powieść. Otwórz Szczegóły, aby dowiedzieć się, co zrobić. |
| Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
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
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

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
| InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean nie zdołał odczytać dość rekordów Windows Installera, by mieć pewność, co jest jeszcze potrzebne: lista zainstalowanych programów wróciła niepełna, a odczyt tych samych rekordów prosto z rejestru również napotkał błędy. Plik mógłby wyglądać na osierocony tylko dlatego, że rekord, który go wymienia, był jednym z nieczytelnych, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. |
| Nothing was deleted | Niczego nie usunięto |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Invalid destination | Nieprawidłowy folder docelowy |
| Could not write to destination | Nie udało się zapisać w folderze docelowym |
| Move failed | Przenoszenie nie powiodło się |
| Delete failed | Usuwanie nie powiodło się |
| Setting not saved | Ustawienie nie zostało zapisane |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Nie udało się zapisać zmiany. Przy następnym uruchomieniu InstallerClean wróci do poprzedniego ustawienia. |
| The destination cannot be inside the Windows Installer folder. | Folder docelowy nie może znajdować się wewnątrz folderu Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows zgłosił błąd pliku; plik został pozostawiony na miejscu. |
| Windows reported file errors; these files were left in place. | Windows zgłosił błędy plików; te pliki zostały pozostawione na miejscu. |
| Something went wrong with this file; it was left in place. | Coś poszło nie tak z tym plikiem; został pozostawiony na miejscu. |
| Something went wrong with these files; they were left in place. | Coś poszło nie tak z tymi plikami; zostały pozostawione na miejscu. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Odmowa przeniesienia plików do folderu Windows Installer (cel: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

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
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
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
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
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
| Backup folder | Backup folder |
| Products | Produkty |
| Patches | Poprawki |
| Product details | Szczegóły produktu |
| Backup folder | Backup folder |
| Operation progress | Postęp operacji |
| Scan {InstallerFolder} again | Skanuj ponownie {InstallerFolder} |
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
| Product details | Product details |
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
| Error: unknown argument '{0}' | Error: unknown argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Błąd: nieoczekiwany dodatkowy argument „{0}”. Jeśli ścieżka folderu przenoszenia zawiera spację, ujmij całą ścieżkę w cudzysłów: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. |
| Cancelling... | Anulowanie... |
| Cancelled. | Anulowano. |
| Error: unexpected failure ({0}). Details written to {1}. | Error: unexpected failure ({0}). Details written to {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Error: unexpected failure ({0}). The crash log could not be written. |
| Scanning {InstallerFolder}... | Skanowanie {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Found {0} unneeded {1} to clean up ({2}). |
| Found no unneeded files. | Found no unneeded files. |
| {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. | {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. |
| {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. | {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. |
| Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Deleting {0} unneeded {1}... | Deleting {0} unneeded {1}... |
| Permanently deleted {0} unneeded {1}. | Permanently deleted {0} unneeded {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Błąd: nie podano folderu docelowego przenoszenia. Użyj /m ŚCIEŻKA. (Lokalizacja domyślna ustawiona w GUI jest przypisana do użytkownika i nie dotyczy uruchomień zaplanowanych ani na koncie usługi.) |
| Error: destination cannot be inside the Windows Installer folder. | Błąd: folder docelowy nie może znajdować się wewnątrz folderu Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Błąd: folder docelowy musi być pełną ścieżką. Otrzymano: {0} |
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
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Inny proces InstallerClean trzyma blokadę pojedynczej instancji (GUI lub inne uruchomienie CLI). Kod zakończenia 75 (stan przejściowy); można bezpiecznie spróbować ponownie później. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Uwaga: zapis do dziennika zdarzeń nie powiódł się. Sprawdź uprawnienia dziennika „Aplikacja” lub zasady grupy. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - oczyszczanie {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Removes cached .msi and .msp files that no installed program still needs. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Needs an elevated (administrator) prompt; Windows will not start it. |
| Usage: | Sposób użycia: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help      Pokaż tę pomoc (akceptuje też /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version   Wypisz wersję (akceptuje też -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ŚCIEŻKA  Przenieś do wskazanej ścieżki |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. |
| Exit codes: | Kody zakończenia: |
|   0   success: the run finished with nothing left to do |   0   success: the run finished with nothing left to do |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partial: some processed, some not (a failure or a Ctrl+C part way) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  stan przejściowy: coś zablokowało uruchomienie (zob. komunikat) |
|   130 cancelled (Ctrl+C) |   130 anulowano (Ctrl+C) |
