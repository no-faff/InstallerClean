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
| The anonymous report | Anonimowy raport |
| InstallerClean couldn't read the report. | InstallerClean nie zdołał odczytać raportu. |

## Section headings

| English | Polski |
| --- | --- |
| PATCHES | POPRAWKI |
| PRODUCT DETAILS | SZCZEGÓŁY PRODUKTU |
| BACKUP FOLDER | FOLDER KOPII ZAPASOWEJ |
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
| Carry on _without it | Kontynuuj _bez czekania |
| Check for _updates | Sprawdź _aktualizacje |
| _Close | _Zamknij |
| _Close without tipping | _Zamknij bez napiwku |
| _Delete permanently | Usuń _trwale |
| _Done | _Gotowe |
| Details | Szczegóły |
| _Leave a tip | Zostaw _napiwek |
| Leave a tip | Zostaw napiwek |
| _Leave a tip | Zostaw _napiwek |
| Leave a _star on GitHub | Zostaw _gwiazdkę na GitHubie |
| Apache 2.0 licence | Licencja Apache 2.0 |
| _Move | _Przenieś |
| Path to folder if you move rather than delete. | Ścieżka do folderu, jeśli przenosisz zamiast usuwać. |
| _Re-scan | _Skanuj ponownie |

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
| (no program) | (brak programu) |
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
| Checking registry for additional packages... | Sprawdzanie rejestru w poszukiwaniu dodatkowych pakietów... |
| Matching files against installed programs... | Porównywanie plików z zainstalowanymi programami... |
| {0} of {1} | {0}/{1} |
| Checking remaining files... | Sprawdzanie pozostałych plików... |
| Waiting for drive {0} to respond... | Oczekiwanie na odpowiedź dysku {0}... |
| Waiting for {0} to respond... | Oczekiwanie na odpowiedź od {0}... |
| Any file still to be checked against it will be left alone. | Każdy plik, którego sprawdzenie jeszcze od tego zależy, zostanie pozostawiony bez zmian. |
| Scan complete ({0}) | Skanowanie zakończone ({0}) |
| Found {0} {1} you can safely delete. | Znaleziono {0} {1} do bezpiecznego usunięcia. |
| Preparing destination folder... | Przygotowywanie folderu docelowego... |
| Moving unneeded files... | Przenoszenie niepotrzebnych plików... |
| Deleting unneeded files... | Usuwanie niepotrzebnych plików... |
| Move cancelled. {0} of {1} {2} processed. | Przenoszenie anulowane. Przetworzono {0}/{1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Usuwanie anulowane. Przetworzono {0}/{1} {2}. |
| {0}. Details are in {1}. | {0}. Szczegóły w {1}. |
| {0}. The crash log could not be written. | {0}. Nie udało się zapisać dziennika awarii. |
| {0}. Details are in {1}. | {0}. Szczegóły w {1}. |
| {0}. The crash log could not be written. | {0}. Nie udało się zapisać dziennika awarii. |
| Access denied. Windows refused the scan. | Odmowa dostępu. Windows odmówił skanowania. |
| Scan stopped. | Skanowanie zatrzymane. |
| Scan cancelled. | Skanowanie anulowane. |
| Ready | Gotowe |
| Scan failed ({0}). Details in {1}. | Skanowanie nie powiodło się ({0}). Szczegóły w {1}. |
| Scan failed ({0}). The crash log could not be written. | Skanowanie nie powiodło się ({0}). Nie udało się zapisać dziennika awarii. |

## Main screen text

| English | Polski |
| --- | --- |
| Any unneeded files below are safe to delete. | Wszystkie niepotrzebne pliki poniżej można bezpiecznie usunąć. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Leżą w {InstallerFolder}. InstallerClean pyta system Windows o każdy zainstalowany program: plik trafia na listę, gdy żaden program się do niego nie przyznaje ({0}) albo gdy nowsza poprawka go zastąpiła i żaden program nie mógłby do niego wrócić ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Przenieś je do wybranego przez siebie folderu kopii zapasowej, a potem skasuj ten folder, gdy nabierzesz pewności, że twoje programy nadal normalnie się aktualizują i odinstalowują. Przełożenie ich z powrotem do {InstallerFolder} przywraca wszystko. Albo usuń je trwale już teraz. |
| Nothing scanned yet. | Jeszcze nic nie przeskanowano. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Naciśnij przycisk Skanuj ponownie, aby przejrzeć {InstallerFolder} w poszukiwaniu plików instalatora, których żaden program już nie potrzebuje. |
| Nothing to show until the next scan. | Nic do pokazania do następnego skanowania. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | Skanowanie uruchamiane po operacji Przenieś lub Usuń nie zakończyło się, więc nie ma listy do pokazania. Naciśnij przycisk Skanuj ponownie, aby jeszcze raz przeskanować {InstallerFolder}. |
| What stopped it is recorded in {0}. | Przyczyna zatrzymania jest zapisana w {0}. |
| These files can't be cleaned up right now. | Tych plików nie można teraz wyczyścić. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Coś właśnie korzysta z Instalatora Windows, na przykład aktualizacja systemu albo program instalujący się w tle. Przenieś i Usuń są wstrzymane, dopóki to trwa, żeby InstallerClean nie ruszał {InstallerFolder} w trakcie zmian. Gdy się skończy, skanuj ponownie, a wrócą. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows odmówił InstallerClean uprawnień do sprawdzenia, czy Instalator Windows jest zajęty, więc Przenieś i Usuń są wstrzymane, a InstallerClean nie ruszy {InstallerFolder}. Uruchom Windows ponownie i skanuj ponownie. Jeśli Windows nadal odmawia, to nie jest maszyna, którą InstallerClean może wyczyścić. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean nie zdołał sprawdzić, czy Instalator Windows jest zajęty, więc Przenieś i Usuń są wstrzymane, a InstallerClean nie ruszy {InstallerFolder}. Uruchom Windows ponownie i skanuj ponownie. Jeśli nadal nie może tego sprawdzić, to nie jest maszyna, którą InstallerClean może wyczyścić. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean nie zdołał sprawdzić, czy Instalator Windows jest zajęty, ponieważ Windows zgłosił błąd: {0} (błąd {1}). Przenieś i Usuń są wstrzymane, a InstallerClean nie ruszy {InstallerFolder}. Uruchom Windows ponownie i skanuj ponownie. Jeśli nadal nie może tego sprawdzić, to nie jest maszyna, którą InstallerClean może wyczyścić. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Na tym komputerze jest wstrzymana wcześniejsza transakcja Instalatora Windows. Wznów ją lub wycofaj tę instalację (albo uruchom system ponownie), zanim wyczyścisz {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows ma w kolejce na następne uruchomienie zmianę nazwy pliku, która dotyczy {InstallerFolder}. Uruchom system ponownie, zanim wyczyścisz. |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | Operacja na plikach czeka w kolejce na następny restart, a InstallerClean nie potrafi ustalić, których plików dotyczy, więc nie może wykluczyć, że są w {InstallerFolder}. Uruchom Windows ponownie przed czyszczeniem. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean nie zdołał odczytać jednego z ustawień Windows, które sprawdza przed dotknięciem {InstallerFolder}, więc nie potrafi ustalić, czy operacja instalatora jest w toku, czy czeka na ponowne uruchomienie. Uruchom Windows ponownie i skanuj ponownie. Jeśli ustawienie nadal się nie odczytuje, to nie jest maszyna, którą InstallerClean może wyczyścić. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Instalator Windows ma coś w toku, więc Przenieś i Usuń są wstrzymane. InstallerClean nie ruszy {InstallerFolder} w trakcie zmian. Gdy się skończy, skanuj ponownie, a wrócą. |
| Select a file to view details. | Wybierz plik, aby zobaczyć szczegóły. |
| Select a product to view details. | Wybierz produkt, aby zobaczyć szczegóły. |
| No metadata available. | Brak dostępnych metadanych. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Brakuje tego pliku instalacyjnego. Teraz nie sprawia to kłopotu i nie będzie go sprawiać aż do dnia, w którym spróbujesz zaktualizować lub odinstalować program, do którego należy. Ten krok może się wtedy nie udać, bo Windows szuka tego pliku, a jego nie ma.<br><br>Aby go przywrócić, potrzebujesz instalatora tej wersji, którą już masz. Zdobądź go od producenta programu i uruchom na istniejącej kopii. Nowsza wersja nie wystarczy: musiałaby najpierw usunąć tę, którą masz, a to właśnie ten krok potrzebuje tego pliku. Odinstalowanie najpierw też nie zadziała, z tego samego powodu. To powinno przywrócić plik i zostawić twoje ustawienia nietknięte, ale Microsoft tego nie gwarantuje. |
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
| Nothing offered on this PC | Na tym komputerze niczego nie zaproponowano |
| One file ({2}) was held back because Windows shows it was changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check it again. | Zatrzymano jeden plik ({2}), ponieważ Windows pokazuje, że został zmieniony po rozpoczęciu skanowania przez InstallerClean. Jeśli jakiś program się instalował lub aktualizował, przeskanuj ponownie, gdy skończy, a InstallerClean sprawdzi go jeszcze raz. |
| {0} {1} ({2}) were held back because Windows shows they were changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check them again. | Zatrzymano {0} {1} ({2}), ponieważ Windows pokazuje, że zostały zmienione po rozpoczęciu skanowania przez InstallerClean. Jeśli jakiś program się instalował lub aktualizował, przeskanuj ponownie, gdy skończy, a InstallerClean sprawdzi je jeszcze raz. |
| Delete that folder when you're satisfied all is well. | Skasuj ten folder, gdy nabierzesz pewności, że wszystko jest w porządku. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | Skasuj ten folder, gdy nabierzesz pewności, że wszystko jest w porządku. Dopiero wtedy naprawdę odzyskasz miejsce. |
| {0} freed | Zwolniono {0} |
| {0} moved | Przeniesiono {0} |
| Nothing was moved | Niczego nie przeniesiono |
| Nothing was deleted | Niczego nie usunięto |
| {0} file could not be moved. | Nie udało się przenieść {0} pliku. |
| {0} files could not be moved. | Nie udało się przenieść {0} plików. |
| {0} file could not be deleted. | Nie udało się usunąć {0} pliku. |
| {0} files could not be deleted. | Nie udało się usunąć {0} plików. |
| {0} {1} moved to: {2} | Przeniesiono {0} {1} do: {2} |
| {0} {1} moved to: {2} | Przeniesiono {0} {1} do: {2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | Zatrzymano {0} plik. Skanowanie uznało go za niepotrzebny. Końcowa kontrola nie mogła tego potwierdzić. |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | Zatrzymano {0} plików. Skanowanie uznało je za niepotrzebne. Końcowa kontrola nie mogła tego potwierdzić. |
| Moved {0} of {1} {2} to {3} before you cancelled. | Przed anulowaniem przeniesiono {0}/{1} {2} do {3}. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Przed anulowaniem usunięto trwale {0}/{1} {2}. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | Łatwo to cofnąć. Przenieś je z powrotem do {InstallerFolder}, a wszystko wróci do stanu sprzed. |
| {0} {1} permanently deleted | Trwale usunięto {0} {1} |
| {0} {1} permanently deleted | Trwale usunięto {0} {1} |
| Any tip is much appreciated | Każdy napiwek jest bardzo mile widziany |
| Send _anonymous report | Wyślij _anonimowy raport |
| The report adds your result to [this chart] on GitHub, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Raport dodaje twój wynik do [tego wykresu] na GitHubie, który pokazuje, ile miejsca ludzie odzyskują dzięki InstallerClean. Zawiera też te dane techniczne, które pomagają mi wyłapywać błędy: |
| How much space was freed, and how many files were moved or deleted | Ile miejsca zwolniono i ile plików przeniesiono lub usunięto |
| Whether a Move went to the same drive, another drive, a removable drive or a network share | Czy operacja Przenieś trafiła na ten sam dysk, inny dysk, dysk wymienny czy udział sieciowy |
| How long the scan and the Move or Delete took | Jak długo trwało skanowanie oraz operacja Przenieś lub Usuń |
| How many installer files there are of each kind, and how much space they take | Ile jest plików instalatora każdego rodzaju i ile zajmują miejsca |
| How many files were left alone, and why | Ile plików pozostawiono bez zmian i dlaczego |
| How often InstallerClean waited for a slow drive or network share, and how many it stopped waiting for | Jak często InstallerClean czekał na wolny dysk lub udział sieciowy i na ile z nich przestał czekać |
| How many programs and patches Windows has a record of | O ilu programach i poprawkach Windows ma zapis |
| Whether Windows still makes old-style short file names | Czy Windows nadal tworzy krótkie nazwy plików w starym stylu |
| Whether Windows is 10 or 11 and x64 or Arm, and which language and country or region it's set to | Czy Windows to wersja 10 czy 11, x64 czy Arm, i jaki ma ustawiony język oraz kraj lub region |
| InstallerClean's version, how you got it (setup, portable, slim, Scoop, the command-line tool on its own, built from source, or not known), whether the report was sent from InstallerClean or the command-line tool, and the language it was showing | Wersja InstallerClean, sposób jego uzyskania (setup, portable, slim, Scoop, samo narzędzie wiersza poleceń, kompilacja ze źródeł lub nieznany), informacja, czy raport wysłał InstallerClean, czy narzędzie wiersza poleceń, oraz język, w którym był wyświetlany |
| Counts of anything that went wrong | Liczniki wszystkiego, co poszło nie tak |
| Each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Każda z tych rzeczy jest liczbą albo etykietą. Nic w raporcie nie wskazuje ciebie, twojego komputera, twoich plików ani programów, a twojego adresu IP nigdy nie widzę. |
| See exactly what's sent | Zobacz dokładnie, co jest wysyłane |

## Summaries and counts

| English | Polski |
| --- | --- |
| {0} file left alone | {0} plik pozostawiony bez zmian |
| {0} files left alone | {0} plików pozostawionych bez zmian |
| {0} unneeded file to clean up | {0} niepotrzebny plik do wyczyszczenia |
| {0} unneeded files to clean up | {0} niepotrzebnych plików do wyczyszczenia |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | Windows ma rekord dla {0} pliku, którego nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawia to kłopotu, ale aktualizacja lub odinstalowanie tego programu może się nie udać. Otwórz Szczegóły, aby dowiedzieć się, co zrobić. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | Windows ma rekordy dla {0} plików, których nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawia to kłopotu, ale aktualizacja lub odinstalowanie tych programów może się nie udać. Otwórz Szczegóły, aby dowiedzieć się, co zrobić. |
| {0} other program | jeszcze {0} program |
| {0} other programs | jeszcze {0} programów |
| {0} file with no program named in the records | {0} plik, dla którego rekordy nie wskazują programu |
| {0} files with no program named in the records | {0} plików, dla których rekordy nie wskazują programu |
| InstallerClean carried on without drive {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean kontynuował bez dysku {0} i pozostawił bez zmian każdy plik, który trzeba było jeszcze z nim porównać. Gdy dysk znów zacznie normalnie odpowiadać, naciśnij przycisk Skanuj ponownie. |
| InstallerClean carried on without {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean kontynuował bez {0} i pozostawił bez zmian każdy plik, którego sprawdzenie jeszcze od tego zależało. Gdy dostęp znów zacznie działać normalnie, naciśnij przycisk Skanuj ponownie. |
| InstallerClean carried on without more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, Re-scan. | InstallerClean kontynuował bez kilku dysków lub udziałów ({0}) i pozostawił bez zmian każdy plik, który trzeba było jeszcze porównać z jednym z nich. Gdy znów zaczną normalnie odpowiadać, naciśnij przycisk Skanuj ponownie. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has finished. | Gdy rozpoczęło się skanowanie, coś korzystało z Instalatora Windows, na przykład aktualizacja systemu albo instalujący się program, więc InstallerClean zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Gdy to się skończy, naciśnij przycisk Skanuj ponownie. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then Re-scan. | Gdy rozpoczęło się skanowanie, na tym komputerze była wstrzymana transakcja Instalatora Windows, więc InstallerClean zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Wznów lub wycofaj tę instalację albo uruchom system ponownie, a potem naciśnij przycisk Skanuj ponownie. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Gdy rozpoczęło się skanowanie, Windows miał w kolejce na następne uruchomienie zmianę nazwy pliku, która dotyczy {InstallerFolder}, więc InstallerClean zatrzymał znajdujące się tam pliki, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom system ponownie, a potem naciśnij przycisk Skanuj ponownie. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Gdy rozpoczęło się skanowanie, operacja na plikach czekała w kolejce na następny restart, a InstallerClean nie potrafił ustalić, których plików dotyczy, więc zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom Windows ponownie, a potem naciśnij przycisk Skanuj ponownie. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Gdy rozpoczęło się skanowanie, Windows odmówił InstallerClean uprawnień do sprawdzenia, czy Instalator Windows jest zajęty, więc zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom Windows ponownie i naciśnij przycisk Skanuj ponownie. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Gdy rozpoczęło się skanowanie, InstallerClean nie zdołał odczytać jednego z ustawień Windows, które sprawdza, więc nie potrafił ustalić, czy operacja instalatora jest w toku, czy czeka na ponowne uruchomienie, i zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom Windows ponownie i naciśnij przycisk Skanuj ponownie. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Gdy rozpoczęło się skanowanie, InstallerClean nie zdołał sprawdzić, czy Instalator Windows jest zajęty, więc zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom Windows ponownie i naciśnij przycisk Skanuj ponownie. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Gdy rozpoczęło się skanowanie, InstallerClean nie zdołał sprawdzić, czy Instalator Windows jest zajęty, ponieważ Windows zgłosił błąd: {0} (błąd {1}). Dlatego zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom Windows ponownie i naciśnij przycisk Skanuj ponownie. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has cleared. | Gdy rozpoczęło się skanowanie, Instalator Windows miał coś w toku lub w oczekiwaniu, więc InstallerClean zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Gdy to się wyjaśni, naciśnij przycisk Skanuj ponownie. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan. | Zegar tego komputera cofnął się lub spóźnił się w trakcie skanowania, więc InstallerClean zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Naciśnij przycisk Skanuj ponownie. |
| {0} of {1} {2} | {0}/{1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} do wyczyszczenia ({2}) |
| {0} file left alone ({1}) | {0} plik pozostawiony bez zmian ({1}) |
| {0} files left alone ({1}) | {0} plików pozostawionych bez zmian ({1}) |
| {0} missing | {0} brakujący |
| {0} missing | {0} brakujących |

## Confirmation dialogs

| English | Polski |
| --- | --- |
| Move {0} {1} ({2})? | Przenieść {0} {1} ({2})? |
| This file will be moved to: | Ten plik zostanie przeniesiony do: |
| These files will be moved to: | Te pliki zostaną przeniesione do: |
| Delete {0} {1} ({2})? | Usunąć {0} {1} ({2})? |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Ten plik zostanie trwale usunięty. Można to zrobić bezpiecznie, ale jeśli chcesz kopię zapasową, użyj zamiast tego Przenieś. |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Te pliki zostaną trwale usunięte. Można to zrobić bezpiecznie, ale jeśli chcesz kopię zapasową, użyj zamiast tego Przenieś. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Ten folder jest na tym samym dysku, więc miejsce nie wróci, dopóki go nie skasujesz. Wybierz folder na innym dysku, jeśli chcesz mieć miejsce od razu. |

## Error messages

| English | Polski |
| --- | --- |
| This is also recorded in {0}. | Jest to zapisywane także w {0}. |
| Access denied | Odmowa dostępu |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows odmówił InstallerClean dostępu, więc program się zatrzymał. Nic nie zostało usunięte.<br><br>InstallerClean działał już jako administrator, więc ponowne uruchomienie go w ten sposób nic nie da. Windows nie mówi nic więcej o tym, co odmówiło dostępu, więc nie ma nic konkretnego do spróbowania. |
| InstallerClean stopped | InstallerClean się zatrzymał |
| Scan failed | Skanowanie nie powiodło się |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Rekordy Windows Installera wróciły całkowicie puste: ani jeden zainstalowany program ani jedna aktualizacja nie rości sobie prawa do żadnego pliku instalacyjnego w pamięci podręcznej. Na działającym komputerze to się nie zdarza (nawet świeża instalacja Windows ma takie pliki), więc albo rekordy są uszkodzone, albo nie dało się ich odczytać, a skanowanie, które uwierzyłoby w tę odpowiedź, błędnie uznałoby każdy plik w {InstallerFolder} za osierocony. InstallerClean zamiast tego się zatrzymał. Nic nie zostało usunięte. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer nie pozwolił InstallerClean wypisać tego, co jest zainstalowane. InstallerClean działał już jako administrator, więc uruchomienie go ponownie jako administrator niczego nie zmieni. Bez tej listy nie da się bezpiecznie stwierdzić, które pliki w pamięci podręcznej są nadal potrzebne, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer nie zdołał przekazać InstallerClean czytelnej listy zainstalowanych programów: odczytał {1} {2}, a potem jeden wpis wrócił nieczytelny (kod błędu {0}). Zamiast pracować na liście odczytanej tylko częściowo, InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer nigdy nie zasygnalizował końca listy zainstalowanych programów: InstallerClean odczytał {2} {3}, a potem poddał się po {0} wpisach (ostatni kod błędu {1}). Liście bez końca nie można ufać, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer nigdy nie zasygnalizował końca listy poprawek jednego programu: InstallerClean odczytał {2} {3}, a potem poddał się po {0} wpisach (ostatni kod błędu {1}). Liście bez końca nie można ufać, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean nie zdołał dopasować rekordów Instalatora Windows do zawartości {InstallerFolder}. Prawie nic z tego, na co wskazują rekordy, tam nie ma, i prawie nic z tego, co tam jest, nie jest wskazane przez żaden rekord, więc o żadnym pliku nie dało się wykazać, że jest niepotrzebny. Niczego nie zaproponowano i niczego nie usunięto. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean nie zdołał dopasować rekordów Instalatora Windows do zawartości {InstallerFolder}. W folderze są pliki, ale ani jeden rekord nie wskazuje niczego w środku, więc o żadnym pliku nie dało się wykazać, że jest niepotrzebny. Niczego nie zaproponowano i niczego nie usunięto. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. Nothing has been offered and nothing has been removed. | InstallerClean nie może przeskanować {InstallerFolder} na tym komputerze. Windows podaje ten folder jako {0}, a InstallerClean skanuje tylko folder Installer, który Windows podaje pod jego własną ścieżką. Niczego nie zaproponowano i niczego nie usunięto. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. Nothing has been offered and nothing has been removed. | Windows zgłasza, że {InstallerFolder} nie istnieje na tym komputerze, więc InstallerClean się zatrzymał. Niczego nie zaproponowano i niczego nie usunięto. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. Nothing has been offered and nothing has been removed. | Windows zgłosił błąd, gdy InstallerClean próbował pobrać listę plików w {InstallerFolder}, więc nie udało mu się uzyskać pełnej listy i się zatrzymał. Niczego nie zaproponowano i niczego nie usunięto. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean nie zdołał odczytać dość rekordów Windows Installera, by mieć pewność, co jest jeszcze potrzebne: lista zainstalowanych programów wróciła niepełna, a odczyt tych samych rekordów prosto z rejestru również napotkał błędy. Plik mógłby wyglądać na osierocony tylko dlatego, że rekord, który go wymienia, był jednym z nieczytelnych, więc InstallerClean się zatrzymał. Nic nie zostało usunięte. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean nie zdołał skłonić systemu Windows do rozwinięcia prawdziwej ścieżki {InstallerFolder}, więc o żadnym pliku nie dało się wykazać, że jest w środku, i żadnego nie zaproponowano do wyczyszczenia. To skanowanie niczego nie znalazło dlatego, że ta kontrola się nie powiodła, a nie dlatego, że folder jest czysty. Niczego nie usunięto. |
| Nothing was deleted | Niczego nie usunięto |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows odmówił InstallerClean uprawnień do sprawdzenia, czy Instalator Windows jest zajęty, więc nie mógł wykluczyć, że plik stanie się potrzebny w trakcie, i nic nie zostało usunięte. |
| Nothing was moved | Niczego nie przeniesiono |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows odmówił InstallerClean uprawnień do sprawdzenia, czy Instalator Windows jest zajęty, więc nie mógł wykluczyć, że plik stanie się potrzebny w trakcie, i nic nie zostało przeniesione. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean nie zdołał przejąć blokady, którą Instalator Windows powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy, i niczego nie usunięto. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean nie zdołał przejąć blokady, którą Instalator Windows powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy, i niczego nie przeniesiono. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie. |
| Invalid destination | Nieprawidłowy folder docelowy |
| Move stopped | Przenoszenie zatrzymane |
| Couldn't use that backup folder | Nie udało się użyć tego folderu kopii zapasowej |
| Move failed | Przenoszenie nie powiodło się |
| Delete failed | Usuwanie nie powiodło się |
| Setting not saved | Ustawienie nie zostało zapisane |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Nie udało się zapisać zmiany. Przy następnym uruchomieniu InstallerClean wróci do poprzedniego ustawienia. |
| The destination cannot be inside the Windows Installer folder. | Folder docelowy nie może znajdować się wewnątrz folderu Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Miejsce docelowe {0} rozwija się wewnątrz folderu systemowego Windows. Wybierz ścieżkę poza %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% i %ProgramData%. |
| Not enough space | Za mało miejsca |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | W {0} brakuje miejsca<br><br>Wymagane: {1}<br>Dostępne: {2} |
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
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Folder kopii zapasowej musi być pełną ścieżką do folderu, zaczynającą się od litery dysku albo udziału sieciowego (na przykład D:\Backup albo \\serwer\backup). InstallerClean nie może użyć tej: {0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean nie mógł już potwierdzić folderu kopii zapasowej, więc zatrzymał się. Sprawdź {0}, potem Skanuj ponownie i spróbuj jeszcze raz. |
| Cannot write to {0}. | Nie można zapisać w {0}. |
| You can save files in {0} but not open them again, so InstallerClean won't move files there. | Możesz zapisywać pliki w {0}, ale nie możesz ich ponownie otworzyć, więc InstallerClean nie przeniesie tam plików. |
| A file called '{0}' is already in the backup folder. | Plik o nazwie „{0}” już jest w folderze kopii zapasowej. |

## Update check

| English | Polski |
| --- | --- |
| Check for updates | Sprawdź aktualizacje |
| Checking... | Sprawdzanie... |
| Up to date. | Wszystko aktualne. |
| InstallerClean {0} is available. | Dostępna jest wersja InstallerClean {0}. |
| It's strongly recommended to use the latest version. | Zdecydowanie zaleca się korzystanie z najnowszej wersji. |
| Download it from GitHub | Pobierz z GitHuba |
| Download .NET Framework 4.8 from Microsoft | Pobierz .NET Framework 4.8 ze strony Microsoftu |
| Update now | Aktualizuj teraz |
| This copy was installed by Scoop. To update it, close InstallerClean and run: | Ta kopia została zainstalowana przez Scoop. Aby ją zaktualizować, zamknij InstallerClean i uruchom: |
| This copy was installed by Scoop for all users. To update it, close InstallerClean, open a terminal as administrator and run: | Ta kopia została zainstalowana przez Scoop dla wszystkich użytkowników. Aby ją zaktualizować, zamknij InstallerClean, otwórz terminal jako administrator i uruchom: |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean to update. | Ta wersja wymaga .NET Framework 4.8. Zainstaluj go ze strony Microsoftu, uruchom ponownie komputer, a potem otwórz InstallerClean, aby zaktualizować. |
| InstallerClean {0} can't be installed automatically. | Nie można automatycznie zainstalować InstallerClean {0}. |
| InstallerClean can't tell whether its setup installed it, so it can't update itself. | InstallerClean nie może ustalić, czy został zainstalowany przez swój instalator, więc nie może się zaktualizować. |
| Copied | Skopiowano |
| Couldn't copy it. Select the command and copy it yourself. | Nie udało się skopiować. Zaznacz polecenie i skopiuj je samodzielnie. |
| _Continue without updating | _Kontynuuj bez aktualizacji |
| Downloading InstallerClean {0}... | Pobieranie InstallerClean {0}... |
| Checking the download... | Sprawdzanie pobranego pliku... |
| The update couldn't be downloaded. | Nie udało się pobrać aktualizacji. |
| The update couldn't be installed. | Nie udało się zainstalować aktualizacji. |
| Couldn't reach GitHub. Check your internet connection and try again. | Nie udało się połączyć z GitHub. Sprawdź połączenie internetowe i spróbuj ponownie. |
| GitHub returned an error response. Try again in a few minutes. | GitHub zwrócił odpowiedź z błędem. Spróbuj ponownie za kilka minut. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Odpowiedź GitHub nie zawierała rozpoznawalnego wydania. Spróbuj ponownie później lub otwórz stronę wydań bezpośrednio. |
| The check timed out. Your connection to GitHub may be slow; try again. | Upłynął limit czasu sprawdzania. Połączenie z GitHub może być wolne; spróbuj ponownie. |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | Sprawdzanie nie powiodło się z nieznanej przyczyny. Szczegóły są w {0}, jeśli chcesz to zgłosić. |
| The check failed for an unknown reason. The crash log could not be written. | Sprawdzanie nie powiodło się z nieznanej przyczyny. Nie udało się zapisać dziennika awarii. |

## Opening links in your browser

| English | Polski |
| --- | --- |
| Couldn't open your browser | Nie udało się otworzyć przeglądarki |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | Link jest w schowku, więc możesz go wkleić samodzielnie:<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean nie zdołał też skopiować linku do schowka, więc oto on:<br><br>{0} |

## Startup and crashes

| English | Polski |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | Jest już uruchomiony. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Wystąpił nieoczekiwany błąd i InstallerClean musi się zamknąć.<br><br>{0}<br><br>Szczegóły zapisano w:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Wystąpił nieoczekiwany błąd i InstallerClean musi się zamknąć.<br><br>{0}<br><br>Nie udało się zapisać dziennika awarii. |
| Startup error | Błąd uruchamiania |
| Failed to start ({0}). Details written to:<br>{1} | Nie udało się uruchomić ({0}). Szczegóły zapisano w:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Nie udało się uruchomić ({0}). Nie udało się zapisać dziennika awarii. |
| Administrator rights needed | Wymagane uprawnienia administratora |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed.<br><br>Run InstallerClean again as administrator. | InstallerClean potrzebuje uprawnień administratora, żeby widzieć każdy program zainstalowany na tym komputerze, a nie działa jako administrator, więc nie przeprowadzi skanowania. Nic nie zostało zmienione.<br><br>Uruchom InstallerClean ponownie jako administrator. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log zbiera nieobsłużone wyjątki InstallerClean.<br># Przy podwyższonych uprawnieniach komunikaty wyjątków platformy mogą<br># zawierać ścieżki plików z bieżącej sesji (w tym profile innych<br># użytkowników wyliczone przez zapytania Instalatora Windows).<br># Komunikaty o błędach sieci przy sprawdzaniu aktualizacji lub wysyłce<br># dziennika wyników mogą zawierać docelowy adres URL oraz rozwiązany<br># adres IP albo adres serwera proxy. Wpisy o nieczytelnych rekordach<br># Instalatora Windows mogą zawierać identyfikator SID konta Windows<br># (S-1-5-21-...) i kody produktów zainstalowanego oprogramowania.<br># Usuń wszystkie trzy rodzaje danych, zanim dołączysz ten plik do<br># publicznego zgłoszenia błędu.<br> |

## Tooltips (hover text)

| English | Polski |
| --- | --- |
| Any tip is much appreciated | Każdy napiwek jest bardzo mile widziany |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Zażądano anulowania. InstallerClean czeka, aż bieżący krok dojdzie do punktu, w którym może się zatrzymać. Przy intensywnych operacjach wejścia/wyjścia lub zapytaniu do bazy danych MSI może to potrwać kilka sekund. |
| Close | Zamknij |
| I really should charge for InstallerClean - because it's been a huge amount of work - but I want to keep it free so it helps as many people as possible. Your donation makes it worthwhile and is very much appreciated. | Naprawdę powinienem pobierać opłatę za InstallerClean, bo kosztował mnie ogrom pracy, ale chcę, żeby pozostał darmowy i pomagał jak największej liczbie osób. Twoje wsparcie sprawia, że było warto, i jest bardzo doceniane. |
| A star helps other people find it | Gwiazdka pomaga innym znaleźć InstallerClean |
| Minimise | Minimalizuj |
| Move the unneeded files to the backup folder | Przenosi niepotrzebne pliki do folderu kopii zapasowej |
| Move the unneeded files to a backup folder. You'll choose it next. | Przenosi niepotrzebne pliki do folderu kopii zapasowej. Wybierzesz go za chwilę. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | Przenosi niepotrzebne pliki do folderu kopii zapasowej. Jest on na tym samym dysku, więc miejsce odzyskasz dopiero po skasowaniu tego folderu. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | Trwale usuwa niepotrzebne pliki. Użyj zamiast tego Przenieś, jeśli chcesz mieć okazję upewnić się, że wszystko jest w porządku. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nazwa podmiotu z osadzonego certyfikatu Authenticode. Łańcuch nie został zweryfikowany. |
| Change language. The program will restart. | Zmień język. Program zostanie ponownie uruchomiony. |

## Screen reader labels

| English | Polski |
| --- | --- |
| Leave a tip | Zostaw napiwek |
| Leave a tip | Zostaw napiwek |
| Leave a tip | Zostaw napiwek |
| Cancel operation | Anuluj operację |
| Cancel scan | Anuluj skanowanie |
| Cancel startup scan | Anuluj skanowanie startowe |
| Carry on without it, drive {0} | Kontynuuj bez czekania, dysk {0} |
| Carry on without it, {0} | Kontynuuj bez czekania, {0} |
| Close | Zamknij |
| Close window | Zamknij okno |
| Close result and return to main window | Zamknij wynik i wróć do okna głównego |
| Leave a star on github | Zostaw gwiazdkę na githubie |
| Minimise | Minimalizuj |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Usuń trwale usuwa niepotrzebne pliki. Anuluj zamyka okno bez usuwania. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Przenieś umieszcza niepotrzebne pliki w wybranym folderze docelowym. Anuluj zostawia je na miejscu. |
| Say thanks | Podziękuj |
| Check for updates | Sprawdź aktualizacje |
| Checks github's releases page for a newer version. | Sprawdza na stronie wydań githuba, czy jest nowsza wersja. |
| Opens the readme on github in your browser. | Otwiera readme na githubie w twojej przeglądarce. |
| Opens the issue tracker on github.com in your browser. | Otwiera listę zgłoszeń (Issues) na github.com w twojej przeglądarce. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Jeśli zaznaczone, InstallerClean przy uruchomieniu sprawdza na githubie, czy jest nowsza wersja. |
| Download it from github | Pobierz z githuba |
| Download .NET Framework 4.8 from Microsoft | Pobierz .NET Framework 4.8 ze strony Microsoftu |
| Copy the command | Kopiuj polecenie |
| Scoop's update command | Polecenie aktualizacji Scoop |
| Opens the licence file on github.com in your browser. | Otwiera plik licencji na github.com w twojej przeglądarce. |
| Backup folder | Folder kopii zapasowej |
| Patches | Poprawki |
| Product details | Szczegóły produktu |
| Backup folder | Folder kopii zapasowej |
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
| Waiting for a drive or share | Oczekiwanie na dysk lub udział |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Pliki, których nie udało się przetworzyć |
| Explains this folder, and how to recover a file, in the README | Wyjaśnia ten folder i sposób odzyskania pliku w README |
| {0} Press Alt+{1} to carry on without it. {2} | {0} Naciśnij Alt+{1}, aby kontynuować bez czekania. {2} |
| What the report holds | Co zawiera raport |
| What the report holds | Co zawiera raport |
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
| drive {0} | dysk {0} |
| .  | .  |
| {0} TB | {0} TB |
| {0} GB | {0} GB |
| {0} MB | {0} MB |
| {0} KB | {0} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | mniej niż sekunda |
| {0:F1} seconds | {0:F1} sekundy |

## Command-line tool (installerclean-cli)

| English | Polski |
| --- | --- |
| Error: unknown argument '{0}' | Błąd: nieznany argument „{0}” |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Błąd: nieoczekiwany dodatkowy argument „{0}”. Jeśli ścieżka folderu docelowego zawiera spację, ujmij całą ścieżkę w cudzysłów: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Błąd: nieoczekiwany dodatkowy argument „{0}”. /s i /d nie przyjmują dalszych argumentów, a w jednym uruchomieniu można użyć tylko jednego przełącznika. |
| Cancelling... | Anulowanie... |
| Cancelled. | Anulowano. |
| Error: unexpected failure ({0}). Details written to {1}. | Błąd: nieoczekiwana awaria ({0}). Szczegóły zapisano w {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Błąd: nieoczekiwana awaria ({0}). Nie udało się zapisać dziennika awarii. |
| Scanning {InstallerFolder}... | Skanowanie {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Znaleziono {0} niepotrzebnych {1} do wyczyszczenia ({2}). |
| No unneeded files identified. | Nie zidentyfikowano niepotrzebnych plików. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows ma rekord dla {0} pliku, którego nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawia to kłopotu, ale aktualizacja lub odinstalowanie tego programu może się nie udać. Aby przywrócić plik, potrzebujesz instalatora tej wersji, którą już masz. Zdobądź go od producenta programu i uruchom na istniejącej kopii. Nowsza wersja nie wystarczy: musiałaby najpierw usunąć tę, którą masz, a to właśnie ten krok potrzebuje tego pliku. Odinstalowanie najpierw też nie zadziała, z tego samego powodu. To powinno przywrócić plik i zostawić twoje ustawienia nietknięte, ale Microsoft tego nie gwarantuje. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows ma rekordy dla {0} plików, których nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawia to kłopotu, ale aktualizacja lub odinstalowanie tych programów może się nie udać. Aby przywrócić plik, potrzebujesz instalatora tej wersji tego programu, którą już masz. Zdobądź go od producenta programu i uruchom na istniejącej kopii. Nowsza wersja nie wystarczy: musiałaby najpierw usunąć tę, którą masz, a to właśnie ten krok potrzebuje tego pliku. Odinstalowanie najpierw też nie zadziała, z tego samego powodu. To powinno przywrócić plik i zostawić twoje ustawienia nietknięte, ale Microsoft tego nie gwarantuje. |
| InstallerClean stopped waiting for drive {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean przestał czekać na dysk {0} i pozostawił bez zmian każdy plik, który trzeba było jeszcze z nim porównać. Gdy dysk znów zacznie normalnie odpowiadać, uruchom polecenie ponownie. |
| InstallerClean stopped waiting for {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean przestał czekać na {0} i pozostawił bez zmian każdy plik, którego sprawdzenie jeszcze od tego zależało. Gdy dostęp znów zacznie działać normalnie, uruchom polecenie ponownie. |
| InstallerClean stopped waiting for more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, run the command again. | InstallerClean przestał czekać na kilka dysków lub udziałów ({0}) i pozostawił bez zmian każdy plik, który trzeba było jeszcze porównać z jednym z nich. Gdy znów zaczną normalnie odpowiadać, uruchom polecenie ponownie. |
| Deleting {0} unneeded {1}... | Trwa usuwanie: {0} niepotrzebnych {1}... |
| Permanently deleted {0} unneeded {1}. | Trwale usunięto {0} niepotrzebnych {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Błąd: nie podano folderu docelowego przenoszenia. Użyj /m ŚCIEŻKA. (Lokalizacja domyślna ustawiona w GUI jest przypisana do użytkownika i nie dotyczy uruchomień zaplanowanych ani na koncie usługi.) |
| Error: destination cannot be inside the Windows Installer folder. | Błąd: folder docelowy nie może znajdować się wewnątrz folderu Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Błąd: folder docelowy musi być pełną ścieżką. Otrzymano: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Błąd: miejsce docelowe {0} rozwija się wewnątrz folderu systemowego Windows. Wybierz ścieżkę poza %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% i %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Błąd: za mało miejsca w {0}. Przeniesienie tych plików wymaga {1}, a wolne jest {2}. Niczego nie przeniesiono. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Błąd: coś właśnie korzysta z Instalatora Windows, na przykład aktualizacja systemu albo program instalujący się w tle. /m i /d są zablokowane, dopóki to trwa. Spróbuj ponownie, gdy się skończy. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Błąd: na tym komputerze jest wstrzymana wcześniejsza transakcja Instalatora Windows. Wznów ją lub wycofaj tę instalację (albo uruchom system ponownie), zanim wyczyścisz {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Błąd: operacja na pliku zakolejkowana na czas po ponownym uruchomieniu dotyczy {InstallerFolder} ({0}). Uruchom system ponownie, aby ją dokończyć, zanim wyczyścisz. |
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | Błąd: operacja na plikach czeka w kolejce na następny restart, a InstallerClean nie potrafi ustalić, których plików dotyczy, więc nie może wykluczyć {InstallerFolder}. Uruchom Windows ponownie przed czyszczeniem. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Błąd: InstallerClean nie zdołał odczytać jednej z wartości rejestru, które sprawdza przed dotknięciem {InstallerFolder}, więc nie może wykluczyć operacji Instalatora Windows w toku ani zakolejkowanej do następnego uruchomienia. /m i /d są zablokowane. Uruchom Windows ponownie i spróbuj jeszcze raz. Jeśli odczyt nadal się nie udaje, to nie jest maszyna, którą InstallerClean może wyczyścić. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Błąd: InstallerClean nie zdołał sprawdzić, czy Instalator Windows jest zajęty, więc nie może wykluczyć trwającej instalacji. /m i /d są zablokowane. Uruchom Windows ponownie i spróbuj jeszcze raz. Jeśli sprawdzenie nadal się nie udaje, to nie jest maszyna, którą InstallerClean może wyczyścić. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Błąd: InstallerClean nie zdołał sprawdzić, czy Instalator Windows jest zajęty, ponieważ Windows zgłosił błąd: {0} (błąd {1}), więc nie może wykluczyć trwającej instalacji. /m i /d są zablokowane. Uruchom Windows ponownie i spróbuj jeszcze raz. Jeśli sprawdzenie nadal się nie udaje, to nie jest maszyna, którą InstallerClean może wyczyścić. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Błąd: Instalator Windows ma coś w toku, więc /m i /d są zablokowane. InstallerClean nie ruszy {InstallerFolder} w trakcie zmian. Spróbuj ponownie, gdy się skończy. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has finished. | Gdy rozpoczęło się skanowanie, coś korzystało z Instalatora Windows, na przykład aktualizacja systemu albo instalujący się program, więc InstallerClean zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Gdy to się skończy, uruchom polecenie ponownie. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then run it again. | Gdy rozpoczęło się skanowanie, na tym komputerze była wstrzymana transakcja Instalatora Windows, więc InstallerClean zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Wznów lub wycofaj tę instalację albo uruchom system ponownie, a potem uruchom polecenie ponownie. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Gdy rozpoczęło się skanowanie, Windows miał w kolejce na następne uruchomienie zmianę nazwy pliku, która dotyczy {InstallerFolder}, więc InstallerClean zatrzymał znajdujące się tam pliki, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom system ponownie, a potem uruchom polecenie ponownie. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Gdy rozpoczęło się skanowanie, operacja na plikach czekała w kolejce na następny restart, a InstallerClean nie potrafił ustalić, których plików dotyczy, więc zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom Windows ponownie, a potem uruchom polecenie ponownie. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Gdy rozpoczęło się skanowanie, Windows odmówił InstallerClean uprawnień do sprawdzenia, czy Instalator Windows jest zajęty, więc zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom Windows ponownie i uruchom polecenie jeszcze raz. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Gdy rozpoczęło się skanowanie, InstallerClean nie zdołał odczytać jednego z ustawień Windows, które sprawdza, więc nie potrafił ustalić, czy operacja instalatora jest w toku, czy czeka na ponowne uruchomienie, i zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom Windows ponownie i uruchom polecenie jeszcze raz. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Gdy rozpoczęło się skanowanie, InstallerClean nie zdołał sprawdzić, czy Instalator Windows jest zajęty, więc zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom Windows ponownie i uruchom polecenie jeszcze raz. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Gdy rozpoczęło się skanowanie, InstallerClean nie zdołał sprawdzić, czy Instalator Windows jest zajęty, ponieważ Windows zgłosił błąd: {0} (błąd {1}). Dlatego zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom Windows ponownie i uruchom polecenie jeszcze raz. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has cleared. | Gdy rozpoczęło się skanowanie, Instalator Windows miał coś w toku lub w oczekiwaniu, więc InstallerClean zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Gdy to się wyjaśni, uruchom polecenie ponownie. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again. | Zegar tego komputera cofnął się lub spóźnił się w trakcie skanowania, więc InstallerClean zatrzymał pliki w {InstallerFolder}, których nie wskazują rekordy żadnego zainstalowanego programu, zamiast je zaproponować. Uruchom polecenie ponownie. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Błąd: InstallerClean nie zdołał przejąć blokady Instalatora Windows, która powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy. Niczego nie usunięto. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Błąd: InstallerClean nie zdołał przejąć blokady Instalatora Windows, która powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy. Niczego nie przeniesiono. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Błąd: Windows odmówił InstallerClean uprawnień do sprawdzenia, czy Instalator Windows jest zajęty, więc nie mógł wykluczyć, że plik stanie się potrzebny w trakcie. Nic nie zostało usunięte. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Błąd: Windows odmówił InstallerClean uprawnień do sprawdzenia, czy Instalator Windows jest zajęty, więc nie mógł wykluczyć, że plik stanie się potrzebny w trakcie. Nic nie zostało przeniesione. |
| Moving {0} unneeded {1} to {2}... | Trwa przenoszenie: {0} niepotrzebnych {1} do {2}... |
| Moved {0} unneeded {1}. | Przeniesiono {0} niepotrzebnych {1}. |
| Check that your programs still update and uninstall as normal, then delete {0}. | Sprawdź, czy twoje programy nadal normalnie się aktualizują i odinstalowują, a potem skasuj {0}. |
| InstallerClean is free. If it helped, a tip is much appreciated: {0} | InstallerClean jest darmowy. Jeśli ci pomógł, napiwek będzie bardzo mile widziany: {0} |
| Update now? It's strongly recommended to use the latest version. [{0}/{1}] | Zaktualizować teraz? Zdecydowanie zaleca się korzystanie z najnowszej wersji. [{0}/{1}] |
| Y | T |
| N | N |
| No answer after a minute, so InstallerClean updates without asking, as it does when nobody is there to answer. | Brak odpowiedzi po minucie, więc InstallerClean aktualizuje się bez pytania, tak jak wtedy, gdy nie ma kto odpowiedzieć. |
| Send an anonymous report of this run? It is saved at {0}. Type ? to see what it holds. [{1}/{2}/?] | Wysłać anonimowy raport z tego uruchomienia? Jest zapisany w {0}. Wpisz ?, aby zobaczyć, co zawiera. [{1}/{2}/?] |
| the chart at {0} | wykresu pod adresem {0} |
| The report adds your result to {0}, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Raport dodaje twój wynik do {0}, który pokazuje, ile miejsca ludzie odzyskują dzięki InstallerClean. Zawiera też te dane techniczne, które pomagają mi wyłapywać błędy: |
| The full report is printed below. | Pełny raport wyświetlono poniżej. |
| Report sent. Thank you. | Raport wysłany. Dziękuję. |
| Report not sent. | Raport nie został wysłany. |
| No answer after a minute. | Brak odpowiedzi po minucie. |
| The report could not be sent. It is kept in {0}. | Nie udało się wysłać raportu. Jest przechowywany w {0}. |
| Sending a report left waiting by an earlier run. | Wysyłanie raportu oczekującego od wcześniejszego uruchomienia. |
| The new version will scan and list what it finds, without deleting anything. To delete, run /d again afterwards. | Nowa wersja przeskanuje i wyświetli listę tego, co znajdzie, niczego nie usuwając. Aby usunąć, uruchom potem ponownie /d. |
| The new version will scan and list what it finds, without moving anything. To move, run /m again afterwards. | Nowa wersja przeskanuje i wyświetli listę tego, co znajdzie, niczego nie przenosząc. Aby przenieść, uruchom potem ponownie /m. |
| InstallerClean is in use, so it can't update itself now. | InstallerClean jest w użyciu, więc nie może się teraz zaktualizować. |
| Running {0} with InstallerClean {1} instead. | Zamiast tego polecenie {0} zostanie uruchomione w wersji InstallerClean {1}. |
| The newer version couldn't be run. | Nie udało się uruchomić nowszej wersji. |
| Running {0} with InstallerClean {1}. It takes this copy's place once the run ends. | Polecenie {0} zostanie uruchomione w wersji InstallerClean {1}. Po zakończeniu ta wersja zastąpi tę kopię. |
| {0}: {1} | {0}: {1} |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again to update. | Ta wersja wymaga .NET Framework 4.8. Zainstaluj go ze strony Microsoftu, uruchom ponownie komputer, a potem znów uruchom InstallerClean, aby zaktualizować. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | Łatwo to cofnąć. Przenieś je z {0} z powrotem do {InstallerFolder}, a wszystko wróci do stanu sprzed. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean nie mógł już potwierdzić folderu kopii zapasowej, więc zatrzymał się. Sprawdź {0}, a potem uruchom polecenie ponownie. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Inny proces InstallerClean trzyma blokadę pojedynczej instancji (GUI lub inne uruchomienie CLI). Kod zakończenia 75 (stan przejściowy); można bezpiecznie spróbować ponownie później. |
| Stopped after deleting {0} of {1} {2}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Zatrzymano po usunięciu {0}/{1} {2}: InstallerClean nie mógł już mieć pewności, że żaden inny InstallerClean nie działa. Uruchom to samo polecenie ponownie. Kod zakończenia 2 (częściowo). |
| Stopped after moving {0} of {1} {2} to {3}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Zatrzymano po przeniesieniu {0}/{1} {2} do {3}: InstallerClean nie mógł już mieć pewności, że żaden inny InstallerClean nie działa. Uruchom to samo polecenie ponownie. Kod zakończenia 2 (częściowo). |
| Stopped before deleting any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Zatrzymano przed usunięciem jakiegokolwiek pliku: InstallerClean nie mógł już mieć pewności, że żaden inny InstallerClean nie działa. Uruchom to samo polecenie ponownie. Kod zakończenia 75 (stan przejściowy). |
| Stopped before moving any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Zatrzymano przed przeniesieniem jakiegokolwiek pliku: InstallerClean nie mógł już mieć pewności, że żaden inny InstallerClean nie działa. Uruchom to samo polecenie ponownie. Kod zakończenia 75 (stan przejściowy). |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed. Run it again from an administrator prompt. | InstallerClean potrzebuje uprawnień administratora, żeby widzieć każdy program zainstalowany na tym komputerze, a nie działa jako administrator, więc nie przeprowadzi skanowania. Nic nie zostało zmienione. Uruchom go ponownie z wiersza polecenia administratora. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Uwaga: zapis do dziennika zdarzeń nie powiódł się. Sprawdź uprawnienia dziennika „Aplikacja” lub zasady grupy. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - oczyszczanie {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Usuwa .msi i .msp z cache, zbędne każdemu zainstalowanemu programowi. |
| Needs an elevated (administrator) prompt and won't scan without one. | Wymaga wiersza polecenia administratora; inaczej nie skanuje. |
| Usage: | Sposób użycia: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help      Pokaż tę pomoc (akceptuje też /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version   Wypisz wersję (akceptuje też -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s          Tylko skanuj - lista niepotrzebnych |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d          Trwale usuń niepotrzebne pliki |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m          Przenieś do zapisanego folderu |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ŚCIEŻKA  Przenieś do wskazanej ścieżki |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blokuje wiersz polecenia aż do końca, więc skrypt<br>albo zadanie zaplanowane może na niego zaczekać. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Folder dla każdego użytkownika; zaplanowane lub SYSTEM: /m ŚCIEŻKA. |
| Before it scans, it checks for a newer version of InstallerClean.<br>When you run it yourself, it asks before updating, and updates if nobody<br>answers within a minute. When it runs on its own, in a scheduled task or<br>a script, it updates without asking.<br>Add /noupdate to skip the check. | Przed skanowaniem sprawdza, czy jest nowsza wersja InstallerClean.<br>Uruchomiony ręcznie pyta przed aktualizacją i aktualizuje się, jeśli nikt<br>nie odpowie w ciągu minuty. Uruchomiony samodzielnie, w zadaniu<br>zaplanowanym lub skrypcie, aktualizuje się bez pytania. Dodaj /noupdate,<br>aby pominąć sprawdzanie. |
| Exit codes: | Kody zakończenia: |
|   0   success: the run did what it was asked |   0   sukces: zrobił to, o co poproszono |
|   1   nothing processed: the run failed or was refused |   1   nic nie przetworzono: uruchomienie zawiodło albo zostało odrzucone |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   częściowo: część przetworzona, część nie (błąd albo Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  stan przejściowy: coś zablokowało uruchomienie (zob. komunikat) |
|   130 cancelled (Ctrl+C) |   130 anulowano (Ctrl+C) |
