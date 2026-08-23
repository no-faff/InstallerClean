#!/usr/bin/env node
// Polish (pl) satellite generator for InstallerClean. Copied from
// gen-strings-template.mjs; only OUT, OVERRIDES and the MAP values changed. Works
// FROM THE ENGLISH SOURCE Strings.resx: strips the machine-contract Cli.* keys
// by name, swaps each remaining <value> for its Polish translation, keeps schema/
// resheaders/<comment> children/&#10; entities/Windows-path backslashes/
// whitespace byte-identical to the neutral, writes LF/UTF-8, then self-checks. Run
// from the repo root: node scripts/translations/gen-strings-pl.mjs
//
// MAP escaping (template literals): \\ is one backslash (the paths), \n is a real
// newline (the multi-line values), {0}/{1} are .NET placeholders left verbatim,
// and &#10; is written literally where the neutral uses the XML entity.
//
// PLURALS: Polish is CLDR one/few/many (One = n==1; Few = n%10 in 2-4 and n%100
// not 12-14; Many = the rest). Slot mapping like Russian: base .Singular = One,
// base .Plural = Many (genitive plural), a .Few override = the 2-4 form. Many
// falls back to .Plural so there are NO .Many overrides. The OVERRIDES block holds
// the 15 satellite-only keys.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = 'src/InstallerClean.Core/Resources/Strings.pl.resx';

// Universal keeps: keys whose value is the same in every language (brand names,
// the pure-placeholder string, the size/elapsed format strings). Their still-
// English value is NOT a miss. Do NOT translate these. Do NOT edit per language.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
  'Display.Size.GB',                   // {0:F2} GB
  'Display.Size.MB',                   // {0:F1} MB
  'Display.Size.KB',                   // {0:F1} KB
  'Display.Size.B',                    // {0} B
  'Display.Elapsed.Ms',                // {0:F0}ms
  'Display.Elapsed.S',                 // {0:F1}s
]);

// Per-language keeps: Polish translates every translatable token (incl. patch ->
// poprawka), so nothing beyond KEEP_ENGLISH stays English.
const ALSO_KEEP = [
  // The list separator Polish uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
];

// Polish CLDR-category overrides (satellite-only, read by name via ResourceManager
// in DisplayHelpers.Pluralise's One/Few/Many branches). Base .Plural carries Many
// (genitive plural), so only .Few is needed for the count split, plus a .One/.Few
// pair for the flat Status.RegisteredPackagesFound whose baked-in adjective agrees
// three ways. MissingFromDisk needs no .Few ("Brakuje" governs genitive so 2-4 and
// 5+ collapse); Reassurance/RestoreHint need none (only the pronoun go/je varies).
// Completion.ReverifySkipped (a flat neutral key) takes a .One only: potrzebować
// governs the genitive, so the pronoun for the kept files is ich at 2-4/5+ and go
// at n==1. Each value's {N} set matches its base key's set.
const OVERRIDES = {
  'Plural.File.Few': `pliki`,
  'Plural.Error.Few': `błędy`,
  'Plural.Package.Few': `pakiety`,
  'Plural.Product.Few': `produkty`,
  'Plural.Patch.Few': `poprawki`,
  'Summary.RegisteredStillUsed.Few': `{0} pliki pozostawione bez zmian`,
  'Summary.OrphanedToCleanUp.Few': `{0} niepotrzebne pliki do wyczyszczenia`,
  'Summary.RegisteredWindow.Few': `{0} pliki pozostawione bez zmian ({1})`,
  // Completion.PermanentDeleteSummary.Few was removed in the 3.0.0 round, for the
  // reason above: "Trwale usunięto" is impersonal and the counted noun comes from
  // Plural.File, so the paucal band needs no sentence of its own.
  // Completion.ReverifySkipped.One was removed in the 3.0.0 round. The Polish
  // reads "Pozostawiono na miejscu", an impersonal form that does not move with
  // the count, and the noun inflects through Plural.File, so the override came out
  // byte-identical to the base. An absent override falls back to the base, which is
  // the form wanted at every count.
  'Status.RegisteredPackagesFound.One': `Znaleziono {0} zarejestrowany {1}.`,
  'Cli.FoundOrphans.One': `Znaleziono {0} niepotrzebny {1} do wyczyszczenia ({2}).`,
  'Cli.FoundOrphans.Few': `Znaleziono {0} niepotrzebne {1} do wyczyszczenia ({2}).`,
  'Cli.DeletingFiles.One': `Trwa usuwanie: {0} niepotrzebny {1}...`,
  'Cli.DeletingFiles.Few': `Trwa usuwanie: {0} niepotrzebne {1}...`,
  'Cli.DeletedFiles.One': `Trwale usunięto {0} niepotrzebny {1}.`,
  'Cli.DeletedFiles.Few': `Trwale usunięto {0} niepotrzebne {1}.`,
  'Cli.MovingFiles.One': `Trwa przenoszenie: {0} niepotrzebny {1} do {2}...`,
  'Cli.MovingFiles.Few': `Trwa przenoszenie: {0} niepotrzebne {1} do {2}...`,
  'Cli.MovedFiles.One': `Przeniesiono {0} niepotrzebny {1}.`,
  'Cli.MovedFiles.Few': `Przeniesiono {0} niepotrzebne {1}.`,
  'Status.RegisteredPackagesFound.Few': `Znaleziono {0} zarejestrowane {1}.`,
};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `O programie`,
  'Window.Registered.Title': `Pliki pozostawione bez zmian`,
  'Window.Orphaned.Title': `Niepotrzebne pliki, które można bezpiecznie usunąć`,

  // Section headings
  // Section.Registered.Products and Automation.Section.Products were removed from
  // this map on 2026-08-21. They left the neutral resx at f49b795b, when the
  // registered-files window stopped having a products group of its own, and stayed
  // here and in all fifteen satellites, so every round regenerated two keys the app
  // cannot use and check-resx-parity reported them as strays in every language.
  'Section.Registered.Patches': `POPRAWKI`,
  'Section.Registered.Details': `SZCZEGÓŁY PRODUKTU`,
  'Section.Backup.Folder': `FOLDER DOCELOWY`,
  'Section.SayThanks': `PODZIĘKUJ`,

  // Field labels (used in detail panels)
  'Field.Reason': `Powód`,
  'Field.Author': `Autor`,
  'Field.Application': `Aplikacja`,
  'Field.Title': `Tytuł`,
  'Field.Subject': `Temat`,
  'Field.Keywords': `Słowa kluczowe`,
  'Field.SigningCertificate': `Certyfikat podpisujący`,
  'Field.FileSize': `Rozmiar pliku`,
  'Field.Comment': `Komentarz`,
  'Field.ProductName': `Nazwa produktu`,
  'Field.File': `Plik`,
  'Field.Size': `Rozmiar`,
  'Field.Patches': `Poprawki`,

  // Placeholder shown for a registered package whose API ProductName is empty.
  'Field.UnknownProductName': `(nieznana)`,
  'Field.PatchesOnly': `(tylko poprawki)`,
  'Field.Missing': `brak`,

  // Actions (button labels; underscore prefixes are WPF mnemonics)
  'Action.About': `_O programie`,
  'Action.Copy': `Kopiuj`,
  'Action.Cut': `Wytnij`,
  'Action.Paste': `Wklej`,
  'Action.SelectAll': `Zaznacz wszystko`,
  'Action.Browse': `Prze_glądaj...`,
  'Action.Cancel': `_Anuluj`,
  'Action.CheckForUpdates': `Sprawdź _aktualizacje`,
  'Action.Close': `_Zamknij`,
  'Action.DeletePermanently': `Usuń _trwale`,
  'Action.Done': `_Gotowe`,
  'Action.Details': `Szczegóły`,
  'Action.BuyMeACuppa': `Postaw mi _kawę`,
  'Action.LeaveStarOnGitHub': `Zostaw _gwiazdkę na GitHubie`,
  'Action.Licence': `Licencja Apache 2.0`,
  'Action.Move': `_Przenieś`,
  'Action.BackupFolderPlaceholder': `Ścieżka do folderu, jeśli przenosisz zamiast usuwać.`,
  'Action.OpenReleasePage': `Otwórz stronę _wydania`,
  'Action.Rescan': `_Skanuj ponownie`,
  'Action.ScanAgain': `Skanuj _ponownie`,
  'Action.SendResultLog': `Wyślij raport`,
  'Action.SendResultLogConfirm': `_Wyślij`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa': `Wesprzyj`,
  'Automation.BuyMeACuppa.About': `Postaw mi kawę`,
  'Automation.CancelOperation': `Anuluj operację`,
  'Automation.CancelScan': `Anuluj skanowanie`,
  'Automation.CancelStartupScan': `Anuluj skanowanie startowe`,
  'Automation.Close': `Zamknij`,
  'Automation.CloseWindow': `Zamknij okno`,
  'Automation.CloseResult': `Zamknij wynik i wróć do okna głównego`,
  'Automation.LeaveStarOnGitHub.About': `Zostaw gwiazdkę na githubie`,
  'Automation.Minimise': `Minimalizuj`,
  'Automation.ConfirmDelete': `Usuń trwale usuwa niepotrzebne pliki. Anuluj zamyka okno bez usuwania.`,
  'Automation.ConfirmMove': `Przenieś umieszcza niepotrzebne pliki w wybranym folderze docelowym. Anuluj zostawia je na miejscu.`,
  'Automation.SayThanks': `Podziękuj`,
  'Automation.ConfirmSendResultLog': `Wyślij przekazuje pokazany raport do No Faff. Anuluj nie wysyła niczego.`,
  'Automation.CheckForUpdates': `Sprawdź aktualizacje`,
  'Automation.CheckForUpdates.HelpText': `Sprawdza na stronie wydań githuba, czy jest nowsza wersja.`,
  'Automation.UpdateAvailable.HelpText': `Otwórz stronę wydania, aby pobrać nowszą wersję, lub anuluj, aby zachować bieżącą.`,
  'Automation.Licence.HelpText': `Otwiera plik licencji na github.com w twojej przeglądarce.`,
  'Automation.Section.BackupFolder': `Folder docelowy`,
  'Automation.Section.Patches': `Poprawki`,
  'Automation.Section.ProductDetails': `Szczegóły produktu`,
  'Automation.BackupFolder': `Folder docelowy`,
  'Automation.OperationProgress': `Postęp operacji`,
  'Automation.RescanInstaller': `Skanuj ponownie {InstallerFolder}`,
  'Automation.ScanningProgress': `Postęp skanowania`,
  'Automation.StartupScanProgress': `Postęp skanowania startowego`,
  'Automation.ViewOrphanedFiles': `Szczegóły, niepotrzebne pliki`,
  'Automation.ViewOrphanedFiles.HelpText': `Dostępne do wyczyszczenia.`,
  'Automation.ViewRegisteredFiles': `Szczegóły, pliki pozostawione bez zmian`,
  'Automation.ViewRegisteredFiles.HelpText': `Lista tylko do odczytu.`,
  'Automation.SortStatus.Ascending': `Posortowano według {0}, rosnąco`,
  'Automation.SortStatus.Descending': `Posortowano według {0}, malejąco`,
  'Automation.Scroll.ScanResults': `Wyniki skanowania`,
  'Automation.Scroll.ResultDetails': `Szczegóły wyniku`,
  'Automation.Scroll.FileDetails': `Szczegóły pliku`,
  'Automation.Scroll.DialogBody': `Tekst okna dialogowego`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `Pliki, których nie udało się przetworzyć`,
  'Automation.RegisteredMissingSeeAlso': `Wyjaśnia ten folder i sposób odzyskania pliku w README`,

  // Tooltips
  'Tooltip.BuyMeACuppa.About': `Aż zaschło w gardle!`,
  'Tooltip.CancellingPending': `Zażądano anulowania. InstallerClean czeka, aż bieżący krok dojdzie do punktu, w którym może się zatrzymać. Przy intensywnych operacjach wejścia/wyjścia lub zapytaniu do bazy danych MSI może to potrwać kilka sekund.`,
  'Tooltip.Close': `Zamknij`,
  'Tooltip.LeaveStarOnGitHub.About': `Gwiazdka pomaga innym znaleźć InstallerClean.`,
  'Tooltip.Minimise': `Minimalizuj`,
  'Tooltip.SendResultLog': `Twoja decyzja, ale będzie miło. Wysyła anonimowe podsumowanie, które po prostu daje mi znać, czy działa i ile miejsca ludzie zwalniają. Na następnym ekranie zobaczysz, co zostanie wysłane, zanim potwierdzisz.`,
  'Tooltip.SendResultLog.NothingFound': `Twoja decyzja, ale będzie miło. Wysyła anonimowe podsumowanie, które po prostu daje mi znać, czy działa. Na następnym ekranie zobaczysz, co zostanie wysłane, zanim potwierdzisz.`,
  'Tooltip.Move': `Przenosi niepotrzebne pliki do folderu docelowego. Skasuj ten folder, gdy nabierzesz pewności, że nic ich nie potrzebuje.`,
  'Tooltip.MoveNeedsDestination': `Przenosi niepotrzebne pliki do folderu docelowego. Wybierzesz go za chwilę. Skasuj ten folder, gdy nabierzesz pewności, że nic ich nie potrzebuje.`,
  'Tooltip.Delete': `Trwale usuwa niepotrzebne pliki. Można je bezpiecznie skasować, a miejsce odzyskasz od razu.`,
  'Tooltip.SigningCertificate': `Nazwa podmiotu z osadzonego certyfikatu Authenticode. Łańcuch nie został zweryfikowany.`,

  // Body copy
  'Body.MainExplanation.Lead': `Wszystkie niepotrzebne pliki poniżej [można bezpiecznie usunąć].`,
  'Body.MainExplanation.Why': `Leżą w {InstallerFolder}. InstallerClean pyta system Windows o każdy zainstalowany program: plik trafia na listę, gdy żaden program się do niego nie przyznaje ({0}) albo gdy nowsza poprawka go zastąpiła i żaden program nie mógłby do niego wrócić ({1}).`,
  'Body.MainExplanation.Action': `Przenieś je do wybranego przez siebie folderu docelowego, a potem usuń ten folder, gdy się przekonasz, że twoje programy nadal normalnie się aktualizują, naprawiają i odinstalowują. Umieszczenie ich z powrotem w {InstallerFolder} przywraca wszystko. Albo usuń je trwale już teraz.`,
  'Body.PendingReboot.MsiExecuteMutex': `Coś właśnie korzysta z Instalatora Windows, na przykład aktualizacja systemu albo program instalujący się w tle. Przenieś i Usuń są wstrzymane, dopóki to trwa, żeby InstallerClean nie ruszał {InstallerFolder} w trakcie zmian. Gdy się skończy, skanuj ponownie, a wrócą.`,
  'Body.PendingReboot.InstallerInProgress': `Na tym komputerze jest wstrzymana wcześniejsza transakcja Instalatora Windows. Wznów ją lub wycofaj tę instalację (albo uruchom system ponownie), zanim wyczyścisz {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows ma w kolejce na następne uruchomienie zmianę nazwy pliku, która dotyczy {InstallerFolder}. Uruchom system ponownie, zanim wyczyścisz.`,
  'Body.NoFileSelected': `Wybierz plik, aby zobaczyć szczegóły.`,
  'Body.NoProductSelected': `Wybierz produkt, aby zobaczyć szczegóły.`,
  'Body.NoMetadata': `Brak dostępnych metadanych.`,
  'Body.RegisteredMissingFromDisk': `Tego pliku instalatora brakuje. Teraz nie sprawia to żadnych kłopotów i nie będzie sprawiać aż do dnia, w którym spróbujesz naprawić, zaktualizować lub odinstalować program, do którego należy. Ten krok może się wtedy nie powieść, bo Windows szuka tego pliku, a jego nie ma.&#10;&#10;Aby spróbować to naprawić, pobierz instalator tego programu od jego producenta i uruchom go na istniejącej kopii (nie odinstalowuj najpierw: odinstalowanie samo w sobie jest krokiem, któremu ten plik jest potrzebny). Jeśli zdołasz, użyj tej wersji, którą masz zainstalowaną, bo Windows może odrzucić inną. To powinno przywrócić plik i zostawić twoje ustawienia w spokoju, ale Microsoft tego nie gwarantuje, a jego własną ostatecznością jest ponowna instalacja programu.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `README [wyjaśnia ten folder] i sposób odzyskania pliku, słowami samego Microsoftu.`,
  'Body.NoPatches': `(brak)`,

  // Reasons (Reason column in the orphaned-files list)
  'Reason.Orphaned': `Osierocony`,
  'Reason.Superseded': `Zastąpiony`,
  'Reason.Obsoleted': `Przestarzały`,

  // Status / progress text
  'Status.Scanning': `Skanowanie...`,
  'Status.Cancelling': `Anulowanie...`,
  'Status.StartingScan': `Rozpoczynanie skanowania...`,
  'Status.QueryingApi': `Pytanie Windows o zainstalowane oprogramowanie...`,
  'Status.ScanningCache': `Skanowanie folderu pamięci podręcznej instalatora...`,
  'Status.EnumeratingProducts': `Wyliczanie zainstalowanych produktów...`,
  'Status.CheckingRegistry': `Sprawdzanie rejestru w poszukiwaniu dodatkowych pakietów...`,

  // 0 = registered package count, 1 = pluralised "package"/"packages"
  'Status.RegisteredPackagesFound': `Znaleziono {0} zarejestrowanych {1}.`,

  // 0 = elapsed time text (e.g. "1.2s")
  'Status.ScanComplete': `Skanowanie zakończone ({0})`,
  'Status.FoundProducts': `Skanowanie pakietów lokalnych...`,

  // 0 = file count, 1 = pluralised noun ("file"/"files")
  'Status.FoundUnused': `Znaleziono {0} {1} do bezpiecznego usunięcia.`,
  'Status.PreparingDestination': `Przygotowywanie folderu docelowego...`,

  // 0 = file count, 1 = pluralised noun
  'Status.Moving': `Przenoszenie niepotrzebnych plików...`,
  'Status.Deleting': `Usuwanie niepotrzebnych plików...`,
  'Status.MoveCancelled.Partial': `Przenoszenie anulowane. Przetworzono {0}/{1} {2}.`,
  'Status.DeleteCancelled.Partial': `Usuwanie anulowane. Przetworzono {0}/{1} {2}.`,
  'Status.MoveFailed': `Przenoszenie nie powiodło się ({0}). Szczegóły w {1}.`,
  'Status.MoveFailed.NoLog': `Przenoszenie nie powiodło się ({0}). Nie udało się zapisać dziennika awarii.`,
  'Status.DeleteFailed': `Usuwanie nie powiodło się ({0}). Szczegóły w {1}.`,
  'Status.DeleteFailed.NoLog': `Usuwanie nie powiodło się ({0}). Nie udało się zapisać dziennika awarii.`,
  'Status.ScanAccessDenied': `Odmowa dostępu. Windows odmówił skanowania.`,
  'Status.ScanFailedDb': `Skanowanie nie powiodło się: nie udało się odczytać rekordów Windows Installera.`,
  'Status.ScanCancelled': `Skanowanie anulowane.`,
  'Status.Done': `Gotowe`,
  'Status.ScanFailedDetails': `Skanowanie nie powiodło się ({0}). Szczegóły w {1}.`,
  'Status.ScanFailedDetails.NoLog': `Skanowanie nie powiodło się ({0}). Nie udało się zapisać dziennika awarii.`,

  // Completion screen
  'Completion.AllClean': `Wszystko czyste`,
  'Completion.NothingToCleanUp': `Nie ma czego czyścić w {InstallerFolder}`,
  'Completion.NothingToCleanUpReceipt': `Przeskanowano {0} {1} w {2}`,

  // 0 = size freed (e.g. "120.5 MB")
  'Completion.Freed': `Zwolniono {0}`,
  'Completion.Moved': `Przeniesiono {0}`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `Niczego nie przeniesiono`,
  'Completion.NothingDeleted': `Niczego nie usunięto`,
  'Completion.FailedCount.Singular': `Nie udało się przenieść {0} pliku z {1}.`,
  'Completion.FailedCount.Plural': `Nie udało się przenieść {0} plików z {1}.`,
  'Completion.FailedCountDelete.Singular': `Nie udało się usunąć {0} pliku z {1}.`,
  'Completion.FailedCountDelete.Plural': `Nie udało się usunąć {0} plików z {1}.`,

  // 0 = moved count, 1 = pluralised noun, 2 = destination path
  'Completion.MoveSummary.Singular': `Przeniesiono {0} {1} do: {2}`,
  'Completion.MoveSummary.Plural': `Przeniesiono {0} {1} do: {2}`,

  // 0 = deleted count, 1 = pluralised noun

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `Trwale usunięto {0} {1}`,
  'Completion.PermanentDeleteSummary.Plural': `Trwale usunięto {0} {1}`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} plik pozostawiony bez zmian`,
  'Summary.RegisteredStillUsed.Plural': `{0} plików pozostawionych bez zmian`,
  'Summary.OrphanedToCleanUp.Singular': `{0} niepotrzebny plik do wyczyszczenia`,
  'Summary.OrphanedToCleanUp.Plural': `{0} niepotrzebnych plików do wyczyszczenia`,
  'Summary.NothingListed.Singular': `Na tym komputerze InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej, więc zatrzymał jedyny plik zamiast pokazać go na liście.`,
  'Summary.NothingListed.Plural': `Na tym komputerze InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej, więc zatrzymał {0} {1} zamiast pokazać je na liście.`,
  'Summary.MissingFromDisk.Singular': `Windows ma rekord dla {0} pliku, którego nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawia to kłopotu, ale naprawa, aktualizacja lub odinstalowanie może się przez niego nie powieść. Otwórz Szczegóły, aby dowiedzieć się, co zrobić.`,
  'Summary.MissingFromDisk.Plural': `Windows ma rekordy dla {0} plików, których nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawiają one kłopotu, ale naprawa, aktualizacja lub odinstalowanie może się przez nie nie powieść. Otwórz Szczegóły, aby dowiedzieć się, co zrobić.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `jeszcze {0} program`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `jeszcze {0} programów`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} plik, dla którego rekordy nie wskazują programu`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} plików, dla których rekordy nie wskazują programu`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0}/{1} {2}`,

  // Orphaned-window footer: removable causes. Reason stems in the genitive-plural
  // elliptical form (invariant across counts). 0/1/2 = counts, 3 = size.
  'Summary.OrphanedWindow': `{0} {1} do wyczyszczenia ({2})`,

  // Registered-window footer; split singular/plural, .Few in OVERRIDES. 0 = count, 1 = size.
  'Summary.RegisteredWindow.Singular': `{0} plik pozostawiony bez zmian ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} plików pozostawionych bez zmian ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Przenieść {0} {1} ({2})?`,

  // 0 = destination path
  'Confirm.MoveDestination': `Przenieś do:`,
  'Confirm.DeleteTitle': `Usunąć {0} {1} ({2})?`,

  // Error messages
  'Error.AdminRequiredTitle': `Odmowa dostępu`,
  'Error.AdminRequiredBody': `Windows odmówił InstallerClean dostępu, więc program się zatrzymał. Nic nie zostało usunięte.\n\nInstallerClean działał już jako administrator, więc ponowne uruchomienie go w ten sposób nic nie da. Windows nie mówi nic więcej o tym, co odmówiło dostępu, więc nie ma nic konkretnego do spróbowania.`,
  'Error.InstallerDbUnavailableTitle': `Nie udało się odczytać rekordów Windows Installera`,
  'Error.ScanFailedTitle': `Skanowanie nie powiodło się`,
  'Error.InstallerDbEmpty': `Rekordy Windows Installera wróciły całkowicie puste: ani jeden zainstalowany program ani jedna aktualizacja nie rości sobie prawa do żadnego pliku instalacyjnego w pamięci podręcznej. Na działającym komputerze to się nie zdarza (nawet świeża instalacja Windows ma takie pliki), więc albo rekordy są uszkodzone, albo nie dało się ich odczytać, a skanowanie, które uwierzyłoby w tę odpowiedź, błędnie uznałoby każdy plik w {InstallerFolder} za osierocony. InstallerClean zamiast tego się zatrzymał. Nic nie zostało usunięte.`,
  'Error.MsiAccessDenied': `Windows Installer nie pozwolił InstallerClean wypisać tego, co jest zainstalowane. InstallerClean działał już jako administrator, więc uruchomienie go ponownie jako administrator niczego nie zmieni. Bez tej listy nie da się bezpiecznie stwierdzić, które pliki w pamięci podręcznej są nadal potrzebne, więc InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
  'Error.MsiNonSuccess': `Windows Installer nie zdołał przekazać InstallerClean czytelnej listy zainstalowanych programów: {0} wpisów z rzędu wróciło nieczytelnych (ostatni kod błędu {1}). Zamiast pracować na liście odczytanej tylko częściowo, InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
  'Error.InvalidDestinationTitle': `Nieprawidłowy folder docelowy`,
  'Error.DestinationWriteFailedTitle': `Nie udało się zapisać w folderze docelowym`,
  'Error.MoveFailedTitle': `Przenoszenie nie powiodło się`,
  'Error.DeleteFailedTitle': `Usuwanie nie powiodło się`,
  'Error.SettingNotSavedTitle': `Ustawienie nie zostało zapisane`,
  'Error.SettingNotSavedBody': `Nie udało się zapisać zmiany. Przy następnym uruchomieniu InstallerClean wróci do poprzedniego ustawienia.`,
  'Error.DestinationInsideInstaller': `Folder docelowy nie może znajdować się wewnątrz folderu Windows Installer.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `Miejsce docelowe {0} rozwija się wewnątrz folderu systemowego Windows. Wybierz ścieżkę poza %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% i %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Za mało miejsca`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `Za mało miejsca w {0}\n\nWymagane: {1}\nDostępne: {2}`,

  // 0 = destination
  'Error.AccessDeniedDestination': `Nie masz uprawnień do zapisu w {0}.\nWypróbuj folder w swoim profilu użytkownika lub na własnym dysku.`,
  'Error.PathTooLong': `Ścieżka {0} jest za długa dla Windows. Wybierz krótszą ścieżkę.`,
  'Error.DestinationMissing': `Folder {0} nie istnieje i nie udało się go utworzyć. Sprawdź literę dysku lub ścieżkę sieciową.`,
  'Error.IOWriteDestination': `Windows nie może zapisać w {0}.\nSzczegóły w {1}.`,
  'Error.IOWriteDestination.NoLog': `Windows nie może zapisać w {0}. Nie udało się zapisać dziennika awarii.`,
  'Error.WriteDestination': `Nie można zapisać w {0}.\nSzczegóły w {1}.`,
  'Error.WriteDestination.NoLog': `Nie można zapisać w {0}. Nie udało się zapisać dziennika awarii.`,
  'Error.MissingSourceFile': `Plik już nie istnieje.`,
  'Error.SourceIsReparsePoint': `Plik źródłowy jest dowiązaniem symbolicznym lub złączem (junction); odrzucono ze względów bezpieczeństwa.`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows odmówił dostępu do tego pliku; został pozostawiony na miejscu.`,
  'Error.AccessDenied.Plural': `Windows odmówił dostępu do tych plików; zostały pozostawione na miejscu.`,
  'Error.FileInUse.Singular': `Ten plik jest otwarty lub zablokowany przez inny program, więc na razie nic go nie usunie. Został na miejscu; spróbuj później.`,
  'Error.FileInUse.Plural': `Te pliki są otwarte lub zablokowane przez inny program, więc na razie nic ich nie usunie. Zostały na miejscu; spróbuj później.`,
  'Error.IOFailure.Singular': `Windows zgłosił błąd pliku; plik został pozostawiony na miejscu.`,
  'Error.IOFailure.Plural': `Windows zgłosił błędy plików; te pliki zostały pozostawione na miejscu.`,
  'Error.UnknownError.Singular': `Coś poszło nie tak z tym plikiem; został pozostawiony na miejscu.`,
  'Error.UnknownError.Plural': `Coś poszło nie tak z tymi plikami; zostały pozostawione na miejscu.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Odmowa przeniesienia plików do folderu Windows Installer (cel: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `Folder docelowy musi być pełną ścieżką do folderu, zaczynającą się od litery dysku albo udziału sieciowego (na przykład D:\\Backup albo \\\\serwer\\backup). InstallerClean nie może użyć tej: {0}`,
  'BrowserLaunch.FailedTitle': `Nie udało się otworzyć przeglądarki`,
  'UpdateCheck.Title': `Sprawdź aktualizacje`,
  'UpdateCheck.Status.Checking': `Sprawdzanie...`,
  'UpdateCheck.Status.UpToDate': `Wszystko aktualne.`,
  'UpdateCheck.UpdateAvailable.Title': `Dostępna aktualizacja`,

  // 0 = installed version, 1 = latest version on GitHub
  'UpdateCheck.UpdateAvailable.Body': `Masz zainstalowaną wersję {0}.&#10;Dostępna jest wersja {1}.`,
  'UpdateCheck.Failed.NetworkUnavailable': `Nie udało się połączyć z GitHub. Sprawdź połączenie internetowe i spróbuj ponownie.`,
  'UpdateCheck.Failed.ServerError': `GitHub zwrócił odpowiedź z błędem. Spróbuj ponownie za kilka minut.`,
  'UpdateCheck.Failed.ResponseParseError': `Odpowiedź GitHub nie zawierała rozpoznawalnego wydania. Spróbuj ponownie później lub otwórz stronę wydań bezpośrednio.`,
  'UpdateCheck.Failed.Timeout': `Upłynął limit czasu sprawdzania. Połączenie z GitHub może być wolne; spróbuj ponownie.`,
  'UpdateCheck.Failed.Unknown': `Sprawdzanie nie powiodło się z nieznanej przyczyny. Szczegóły są w crash.log, jeśli chcesz to zgłosić.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `InstallerClean nie mógł otworzyć przeglądarki. Link jest w schowku, więc możesz wkleić go samodzielnie:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean nie mógł otworzyć przeglądarki ani skopiować linku do schowka. Oto link:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `InstallerClean nie mógł już potwierdzić folderu docelowego, więc zatrzymał się, zamiast zapisać w złym miejscu. Sprawdź {0}, potem Skanuj ponownie i spróbuj jeszcze raz.`,

  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Nie można zapisać w {0}.`,

  // 0 = file name
  'Error.NoUniqueFilename': `Nie udało się znaleźć unikalnej nazwy pliku dla „{0}” po 10 000 prób.`,

  // Result log (post-cleanup diagnostic send)
  'ResultLog.Sending': `Wysyłanie...`,
  'ResultLog.Sent': `Dzięki! Raport wysłany.`,
  'ResultLog.Failed': `Wysyłanie nie powiodło się. Spróbuj ponownie później.`,
  'ResultLog.NothingToSend': `Brak raportu do wysłania.`,
  'ConfirmSendResultLog.Title': `Wysłać to?`,
  'ConfirmSendResultLog.Reassurance': `Trafia do nofaff.netlify.app/api/result-log. Nic nie identyfikuje ciebie ani twojego komputera; to po prostu daje mi znać, że InstallerClean działa i [ile miejsca ludzie zwalniają].`,
  'Automation.ResultLogPreview': `Podgląd raportu`,

  // Single instance / startup / crash
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean jest już uruchomiony.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `Wystąpił nieoczekiwany błąd i InstallerClean musi się zamknąć.\n\n{0}\n\nSzczegóły zapisano w:\n{1}`,
  'Startup.UnhandledBody.NoLog': `Wystąpił nieoczekiwany błąd i InstallerClean musi się zamknąć.\n\n{0}\n\nNie udało się zapisać dziennika awarii.`,
  'Startup.ErrorTitle': `Błąd uruchamiania`,
  'Startup.FailedToStart': `Nie udało się uruchomić ({0}). Szczegóły zapisano w:\n{1}`,
  'Startup.FailedToStart.NoLog': `Nie udało się uruchomić ({0}). Nie udało się zapisać dziennika awarii.`,

  // File picker
  'FilePicker.ChooseDestinationTitle': `Wybierz folder docelowy dla przeniesionych plików`,

  // Version display

  // 0 = major.minor.patch (e.g. "1.5.4")
  'Version.Display': `Wersja {0}`,
  'Plural.File.Singular': `plik`,
  'Plural.File.Plural': `plików`,
  'Plural.Error.Singular': `błąd`,
  'Plural.Error.Plural': `błędów`,
  'Plural.Package.Singular': `pakiet`,
  'Plural.Package.Plural': `pakietów`,
  'Plural.Product.Singular': `produkt`,
  'Plural.Product.Plural': `produktów`,
  'Plural.Patch.Singular': `poprawka`,
  'Plural.Patch.Plural': `poprawek`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ListSeparator': `, `,
  'Display.ElapsedLong.LessThanASecond': `mniej niż sekunda`,
  'Display.ElapsedLong.Seconds': `{0:F1} sekundy`,
  'CrashLog.PrivacyHeader': `# crash.log zbiera nieobsłużone wyjątki InstallerClean.\n# Przy podwyższonych uprawnieniach komunikaty wyjątków platformy mogą\n# zawierać ścieżki plików z bieżącej sesji (w tym profile innych\n# użytkowników wyliczone przez zapytania Instalatora Windows).\n# Komunikaty o błędach sieci przy sprawdzaniu aktualizacji lub wysyłce\n# dziennika wyników mogą zawierać docelowy adres URL oraz rozwiązany\n# adres IP albo adres serwera proxy. Wpisy o nieczytelnych rekordach\n# Instalatora Windows mogą zawierać identyfikator SID konta Windows\n# (S-1-5-21-...) i kody produktów zainstalowanego oprogramowania.\n# Usuń wszystkie trzy rodzaje danych, zanim dołączysz ten plik do\n# publicznego zgłoszenia błędu.\n`,
  'Tooltip.ChangeLanguage': `Zmień język. Program zostanie ponownie uruchomiony.`,
  'Automation.ChangeLanguage': `Zmień język`,
  'Automation.ChangeLanguage.HelpText': `Program zostanie ponownie uruchomiony.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys. The
  // machine-contract Cli.EventLog* keys (bar Cli.EventLogUnavailable) are NOT
  // here and are stripped from the output; they stay English at runtime. In the
  // help lines translate the DESCRIPTION only: keep the command tokens, flags,
  // the {InstallerFolder} token and the exit-code numbers verbatim, keep the
  // leading spaces (the screen is column-aligned for a monospace terminal), and
  // translate the PATH metavariable (ŚCIEŻKA).
  'Cli.UnknownArgument': `Błąd: nieznany argument „{0}”`,
  'Cli.Cancelling': `Anulowanie...`,
  'Cli.Cancelled': `Anulowano.`,
  'Cli.GenericError': `Błąd: nieoczekiwana awaria ({0}). Szczegóły zapisano w {1}.`,
  'Cli.GenericError.NoLog': `Błąd: nieoczekiwana awaria ({0}). Nie udało się zapisać dziennika awarii.`,
  'Cli.ScanningInstaller': `Skanowanie {InstallerFolder}...`,
  'Cli.FoundOrphans': `Znaleziono {0} niepotrzebnych {1} do wyczyszczenia ({2}).`,
  'Cli.DeletingFiles': `Trwa usuwanie: {0} niepotrzebnych {1}...`,
  'Cli.DeletedFiles': `Trwale usunięto {0} niepotrzebnych {1}.`,
  'Cli.NoMoveDestination': `Błąd: nie podano folderu docelowego przenoszenia. Użyj /m ŚCIEŻKA. (Lokalizacja domyślna ustawiona w GUI jest przypisana do użytkownika i nie dotyczy uruchomień zaplanowanych ani na koncie usługi.)`,
  'Cli.MoveDestinationInsideInstaller': `Błąd: folder docelowy nie może znajdować się wewnątrz folderu Windows Installer.`,
  'Cli.MoveDestinationRelative': `Błąd: folder docelowy musi być pełną ścieżką. Otrzymano: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Błąd: miejsce docelowe {0} rozwija się wewnątrz folderu systemowego Windows. Wybierz ścieżkę poza %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% i %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Błąd: coś właśnie korzysta z Instalatora Windows, na przykład aktualizacja systemu albo program instalujący się w tle. /m i /d są zablokowane, dopóki to trwa. Spróbuj ponownie, gdy się skończy.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Błąd: na tym komputerze jest wstrzymana wcześniejsza transakcja Instalatora Windows. Wznów ją lub wycofaj tę instalację (albo uruchom system ponownie), zanim wyczyścisz {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Błąd: zakolejkowana na po ponownym uruchomieniu operacja na pliku dotyczy {InstallerFolder} ({0}). Uruchom system ponownie, aby ją dokończyć, zanim wyczyścisz.`,
  'Cli.MovingFiles': `Trwa przenoszenie: {0} niepotrzebnych {1} do {2}...`,
  'Cli.MovedFiles': `Przeniesiono {0} niepotrzebnych {1}.`,
  'Cli.MutexBlocked': `Inny proces InstallerClean trzyma blokadę pojedynczej instancji (GUI lub inne uruchomienie CLI). Kod zakończenia 75 (stan przejściowy); można bezpiecznie spróbować ponownie później.`,
  'Cli.EventLogUnavailable': `Uwaga: zapis do dziennika zdarzeń nie powiódł się. Sprawdź uprawnienia dziennika „Aplikacja” lub zasady grupy.`,
  'Cli.Help.Header': `InstallerClean - oczyszczanie {InstallerFolder}`,
  'Cli.Help.Usage': `Sposób użycia:`,
  'Cli.Help.Help': `  installerclean-cli --help      Pokaż tę pomoc (akceptuje też /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version   Wypisz wersję (akceptuje też -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s          Tylko skanuj - lista niepotrzebnych`,
  'Cli.Help.Delete': `  installerclean-cli /d          Trwale usuń niepotrzebne pliki`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m          Przenieś do zapisanego folderu`,
  'Cli.Help.MovePath': `  installerclean-cli /m ŚCIEŻKA  Przenieś do wskazanej ścieżki`,
  'Cli.Help.NoteLine1': `installerclean-cli blokuje wiersz polecenia aż do końca, więc skrypt&#10;albo zadanie zaplanowane może na niego zaczekać.`,
  'Cli.Help.ExitCodesHeader': `Kody zakończenia:`,
  'Cli.Help.ExitCodeOk': `  0   sukces: zrobił to, o co poproszono, i nic nie zawiodło`,
  'Cli.Help.ExitCodeError': `  1   niepowodzenie: nic nie przetworzono (złe argumenty, złe miejsce&#10;       docelowe, nieudane skanowanie albo każdy plik z błędem)`,
  'Cli.Help.ExitCodePartial': `  2   częściowo: część przetworzona, część nie (błąd albo Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  stan przejściowy: coś zablokowało uruchomienie (zob. komunikat)`,
  'Cli.Help.ExitCodeCancelled': `  130 anulowano (Ctrl+C)`,
  'Body.NotScanned.Lead': `Jeszcze nic nie przeskanowano.`,
  'Body.NotScanned.Why': `Naciśnij przycisk Skanuj ponownie, aby przejrzeć {InstallerFolder} w poszukiwaniu plików instalatora, których żaden program już nie potrzebuje.`,
  'Confirm.MoveSameDrive': `Ten folder jest na tym samym dysku, więc miejsce nie wróci, dopóki go nie skasujesz. Wybierz folder na innym dysku, jeśli chcesz mieć miejsce od razu.`,
  'Error.ScanCorrelationFailed': `InstallerClean nie zdołał dopasować rekordów Instalatora Windows do zawartości {InstallerFolder}. Prawie nic z tego, na co wskazują rekordy, tam nie ma, i prawie nic z tego, co tam jest, nie jest wskazane przez żaden rekord, więc o żadnym pliku nie dało się wykazać, że jest niepotrzebny. Niczego nie zaproponowano i niczego nie usunięto.`,
  'Error.CandidateOutsideCache': `Ten plik nie znajduje się bezpośrednio w folderze Windows Installer; odrzucono ze względów bezpieczeństwa.`,
  'Completion.ReverifySkipped': `Pozostawiono na miejscu {0} {1}, ponieważ rekordy przyznają się teraz do tego, co oznaczyło skanowanie.`,
  'Completion.MoveCancelledSummary': `Przed anulowaniem przeniesiono {0}/{1} {2}.`,
  'Completion.PermanentDeleteCancelledSummary': `Przed anulowaniem usunięto trwale {0}/{1} {2}.`,
  'Body.PendingReboot.Lead': `Tych plików nie można teraz wyczyścić.`,
  'Cli.TooManyArguments': `Błąd: nieoczekiwany dodatkowy argument „{0}”. Jeśli ścieżka folderu przenoszenia zawiera spację, ujmij całą ścieżkę w cudzysłów: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Folder zapisuje się per użytkownik; zadania wymagają /m ŚCIEŻKA.`,
  'Completion.ReverifyIncomplete': `Pozostawiono na miejscu {0} {1}, ponieważ przy końcowym sprawdzeniu nie udało się odczytać rekordów Instalatora Windows w całości.`,
  'Error.ScanRecordsUnreadable': `InstallerClean nie zdołał odczytać dość rekordów Windows Installera, by mieć pewność, co jest jeszcze potrzebne: lista zainstalowanych programów wróciła niepełna, a odczyt tych samych rekordów prosto z rejestru również napotkał błędy. Plik mógłby wyglądać na osierocony tylko dlatego, że rekord, który go wymienia, był jednym z nieczytelnych, więc InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer nigdy nie zasygnalizował końca listy zainstalowanych programów: InstallerClean poddał się po {0} wpisach (ostatni kod błędu {1}). Liście bez końca nie można ufać, więc InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer nigdy nie zasygnalizował końca listy poprawek jednego programu: InstallerClean poddał się po {0} wpisach (ostatni kod błędu {1}). Liście bez końca nie można ufać, więc InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
  'UpdateCheck.Status.UpdateAvailable': `Dostępna jest wersja {0}.`,
  'Completion.DonateAsk': `Cieszę się, że pomogło. Jeśli masz dobre serce, jest miejsce na napiwek.`,
  'About.Link.Guide': `Przewodnik i FAQ`,
  'About.Link.ReportProblem': `Zgłoś problem`,
  'About.AutoUpdateCheck': `Automatycznie sprawdzaj aktualizacje`,
  'Automation.About.Guide.HelpText': `Otwiera readme na githubie w twojej przeglądarce.`,
  'Automation.About.ReportProblem.HelpText': `Otwiera listę zgłoszeń (Issues) na github.com w twojej przeglądarce.`,
  'Automation.AutoUpdateCheck.HelpText': `Jeśli zaznaczone, InstallerClean przy uruchomieniu sprawdza na githubie, czy jest nowsza wersja.`,
  'Tooltip.MoveSameDrive': `Przenosi niepotrzebne pliki do folderu docelowego. Jest on na tym samym dysku, więc miejsce odzyskasz dopiero po skasowaniu tego folderu albo przeniesieniu go na inny dysk. Możesz to zrobić, gdy nabierzesz pewności, że nic ich nie potrzebuje.`,
  'Completion.MoveRestoreHint.Singular': `Plik w tym folderze [można bezpiecznie usunąć], więc skasuj folder, kiedy zechcesz. Do tego czasu możesz umieścić go z powrotem w {InstallerFolder}, gdyby jakiś program okazał się go potrzebować (skrajnie mało prawdopodobne).`,
  'Completion.MoveRestoreHint.Plural': `Pliki w tym folderze [można bezpiecznie usunąć], więc skasuj go, kiedy zechcesz. Do tego czasu możesz umieścić je z powrotem w {InstallerFolder}, gdyby jakiś program okazał się potrzebować któregoś z nich (skrajnie mało prawdopodobne).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `Plik w tym folderze [można bezpiecznie usunąć], więc skasuj folder albo przenieś go na inny dysk, kiedy naprawdę zechcesz odzyskać miejsce. Do tego czasu możesz umieścić go z powrotem w {InstallerFolder}, gdyby jakiś program okazał się go potrzebować (skrajnie mało prawdopodobne).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `Pliki w tym folderze [można bezpiecznie usunąć], więc skasuj go albo przenieś na inny dysk, kiedy naprawdę zechcesz odzyskać miejsce. Do tego czasu możesz umieścić je z powrotem w {InstallerFolder}, gdyby jakiś program okazał się potrzebować któregoś z nich (skrajnie mało prawdopodobne).`,
  'Confirm.DeletePermanently.Singular': `Ten plik zostanie trwale usunięty. [Można go bezpiecznie usunąć], ale jeśli chcesz kopię zapasową, użyj zamiast tego przycisku Przenieś.`,
  'Confirm.DeletePermanently.Plural': `Pliki zostaną trwale usunięte. [Można je bezpiecznie usunąć], ale jeśli chcesz kopię zapasową, użyj zamiast tego przycisku Przenieś.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean nie zdołał skłonić systemu Windows do rozwinięcia prawdziwej ścieżki {InstallerFolder}, więc o żadnym pliku nie dało się wykazać, że jest w środku, i żadnego nie zaproponowano do wyczyszczenia. To skanowanie niczego nie znalazło dlatego, że ta kontrola się nie powiodła, a nie dlatego, że folder jest czysty. Niczego nie usunięto.`,
  'Automation.Scroll.ProductDetails': `Szczegóły produktu`,
  'Body.PendingReboot.Other': `Instalator Windows ma coś w toku, więc Przenieś i Usuń są wstrzymane. InstallerClean nie ruszy {InstallerFolder} w trakcie zmian. Gdy się skończy, skanuj ponownie, a wrócą.`,
  'Cli.TooManyArgumentsNoPath': `Błąd: nieoczekiwany dodatkowy argument „{0}”. /s i /d nie przyjmują dalszych argumentów, a w jednym uruchomieniu można użyć tylko jednego przełącznika.`,
  'Cli.MissingFromDisk.Singular': `Windows ma rekord dla {0} pliku, którego nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawia to kłopotu, ale naprawa, aktualizacja lub odinstalowanie może się przez niego nie powieść. Ponowne uruchomienie instalatora tego programu, najlepiej w tej samej wersji, zwykle przywraca plik.`,
  'Cli.MissingFromDisk.Plural': `Windows ma rekordy dla {0} plików, których nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawiają one kłopotu, ale naprawa, aktualizacja lub odinstalowanie może się przez nie nie powieść. Ponowne uruchomienie instalatora każdego z tych programów, najlepiej w tej samej wersji, zwykle przywraca pliki.`,
  'Cli.MoveNotEnoughSpace': `Błąd: za mało miejsca w {0}. Przeniesienie tych plików wymaga {1}, a wolne jest {2}. Niczego nie przeniesiono.`,
  'Cli.PendingRebootBlocked.Other': `Błąd: Instalator Windows ma coś w toku, więc /m i /d są zablokowane. InstallerClean nie ruszy {InstallerFolder} w trakcie zmian. Spróbuj ponownie, gdy się skończy.`,
  'Cli.FoundNoOrphans': `Nie znaleziono niepotrzebnych plików.`,
  'Cli.NothingOffered.Singular': `InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej, więc zatrzymał jedyny plik ({2}), który inaczej by zaproponował.`,
  'Cli.NothingOffered.Plural': `InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej, więc zatrzymał wszystkie {0} {1} ({2}), które inaczej by zaproponował.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean nie mógł już potwierdzić folderu docelowego, więc zatrzymał się, zamiast zapisać w złym miejscu. Sprawdź {0}, a potem uruchom polecenie ponownie.`,
  'Cli.Help.Summary': `Usuwa pliki .msi i .msp z pamięci podręcznej, zbędne każdemu programowi.`,
  'Cli.Help.Elevation': `Wymaga wiersza polecenia administratora; inaczej Windows go nie uruchomi.`,
  'Error.InstallerLockUnavailableTitle': `Niczego nie usunięto`,
  'Error.MoveInstallerLockUnavailableTitle': `Niczego nie przeniesiono`,
  'Error.InstallerLockUnavailable': `InstallerClean nie zdołał przejąć blokady, którą Instalator Windows powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy, i niczego nie usunięto. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean nie zdołał przejąć blokady, którą Instalator Windows powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy, i niczego nie przeniesiono. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie.`,
  'Cli.InstallerLockUnavailable': `Błąd: InstallerClean nie zdołał przejąć blokady Instalatora Windows, która powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy. Niczego nie usunięto. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie.`,
  'Cli.MoveInstallerLockUnavailable': `Błąd: InstallerClean nie zdołał przejąć blokady Instalatora Windows, która powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy. Niczego nie przeniesiono. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie.`,
  'Completion.ReverifyRecordsChanged': `Pozostawiono na miejscu {0} {1}, ponieważ do czasu końcowego sprawdzenia rekordy Instalatora Windows się zmieniły.`,
  'Summary.RecordsNotMatched': `InstallerClean nie zdołał dopasować wszystkiego, co jest w rekordach Windows, więc nie odczytał ich w całości. Niepotrzebnych plików powyżej to nie dotyczy, ale to, co mówi o plikach brakujących w {InstallerFolder}, może być niepełne. Skanuj ponownie, aby spróbować jeszcze raz.`,
  'Cli.RecordsNotMatched': `InstallerClean nie zdołał dopasować wszystkiego, co jest w rekordach Windows, więc nie odczytał ich w całości. Tego, co znalazł, to nie dotyczy, ale to, co mówi o plikach brakujących w {InstallerFolder}, może być niepełne. Ponowne uruchomienie może wykryć więcej.`,
  'Completion.ReverifyIdentityClaimed': `Pozostawiono na miejscu {0} {1}, ponieważ Windows ma rekord programu wskazanego w środku.`,
  'Completion.ReverifyIdentityUnreadable': `Pozostawiono na miejscu {0} {1}, ponieważ InstallerClean nie znalazł w środku nazwy żadnego programu.`,
  'Completion.ReverifyOwnershipUnestablished': `Pozostawiono na miejscu {0} {1}, ponieważ do czasu końcowego sprawdzenia InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej.`,
  'Completion.NothingRemoved': `Niczego nie usunięto`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean nie zdołał dopasować rekordów Instalatora Windows do zawartości {InstallerFolder}. W folderze są pliki, ale ani jeden rekord nie wskazuje niczego w środku, więc o żadnym pliku nie dało się wykazać, że jest niepotrzebny. Niczego nie zaproponowano i niczego nie usunięto.`,
  'Completion.NothingOffered': `Na tym komputerze niczego nie zaproponowano`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej, więc zatrzymał jedyny plik ({1}), który inaczej by zaproponował.`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean nie zdołał ustalić z pewnością, do których zainstalowanych tu programów należą pliki w pamięci podręcznej, więc zatrzymał wszystkie {0} plików ({1}), które inaczej by zaproponował.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the machine-contract Cli.* <data> elements BY NAME (the
// Cli.EventLog* set bar Cli.EventLogUnavailable): each is matched non-greedy to
// its own </data>. The human-facing Cli keys are KEPT, and their value is
// replaced from the MAP like any other key. Same predicate as
// scripts/check-resx-parity.mjs.
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
let cliMachineRemoved = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (isMachineCliKey(name)) { cliMachineRemoved++; return ''; } return m; });

// Replace each key's inner <value> from MAP. The capture keeps the <data> tag,
// its attributes and the whitespace before <value>; any <comment> child and the
// </data> close sit outside the match. The closing quote anchors the name, so
// Status.MoveFailed never matches Status.MoveFailed.NoLog. A function replacement
// keeps $-sequences in a value from being read as backreferences.
const escRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const notApplied = [];
for (const [key, val] of Object.entries(MAP)) {
  const re = new RegExp('(<data name="' + escRe(key) + '"[^>]*>\\s*<value>)([\\s\\S]*?)(</value>)');
  let applied = false;
  text = text.replace(re, (m, p1, p2, p3) => { applied = true; return p1 + val + p3; });
  if (!applied) notApplied.push(key);
}

// Append the satellite-only override <data> elements before </root>. Values carry
// no XML-special characters (same as the MAP).
const overrideBlock = Object.entries(OVERRIDES)
  .map(([k, v]) => `  <data name="${k}" xml:space="preserve"><value>${v}</value></data>`)
  .join('\n');
if (overrideBlock) text = text.replace('</root>', overrideBlock + '\n</root>');

// Normalise to LF with exactly one trailing newline.
text = text.replace(/\r\n/g, '\n');
if (!text.endsWith('\n')) text += '\n';

writeFileSync(OUT, text, 'utf8');

// ---------------- self-check the written file against the neutral ----------------
const placeholders = (s) => new Set([...s.matchAll(/\{(\d+)(?::[^}]*)?\}/g)].map((p) => p[1]));
const parse = (xml) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'));
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written);
const neutralRequired = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

const overrideKeys = Object.keys(OVERRIDES);
const overrideMissing = overrideKeys.filter((k) => !output.has(k));
const overrideArityMismatch = overrideKeys.filter((k) => {
  if (!output.has(k)) return true;
  const base = k.replace(/\.(?:One|Few|Many)$/, '');
  const ref = neutral.has(`${base}.Plural`) ? `${base}.Plural` : base;
  if (!neutral.has(ref)) return true;
  const a = placeholders(neutral.get(ref)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});

const missingFromMap = neutralRequired.filter((k) => !(k in MAP));
const strayMapKeys = Object.keys(MAP).filter((k) => !neutral.has(k));
const machineLeaked = [...output.keys()].filter(isMachineCliKey);

// The one human-facing Cli.EventLog* key, asserted present rather than left to
// the counts: a predicate that stopped discriminating it takes it out of the
// output AND out of the required set, so every figure above still agrees. The
// MAP substitution notices today only through the order the two run in.
const humanCliStripped = !output.has('Cli.EventLogUnavailable');
const missingFromOutput = neutralRequired.filter((k) => !output.has(k));
const arityMismatch = neutralRequired.filter((k) => {
  if (!output.has(k)) return false;
  const a = placeholders(neutral.get(k)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});
const crlf = (written.match(/\r/g) || []).length;

const alsoKeep = new Set(ALSO_KEEP);
const untranslated = neutralRequired.filter((k) =>
  output.has(k) && output.get(k) === neutral.get(k) && !KEEP_ENGLISH.has(k) && !alsoKeep.has(k));

// Breakdown computed, never pinned: the non-Cli and human-Cli totals both grow with
// every string the app gains, and a hardcoded pair goes stale silently while the
// checked figure beside it stays right.
const nonCliRequired = neutralRequired.filter((k) => !k.startsWith('Cli.')).length;
console.log('translatable <data> in output:', output.size,
  '(expect', neutralRequired.length + overrideKeys.length,
  '=', nonCliRequired, 'non-Cli +', neutralRequired.length - nonCliRequired, 'Cli +',
  overrideKeys.length, 'override)');
console.log('machine Cli <data> removed:', cliMachineRemoved, `(expect ${cliMachineExpected})`);
console.log('MAP entries:', Object.keys(MAP).length, '| override keys:', overrideKeys.length, '| CRLF:', crlf, '(expect 0)');

if (alsoKeep.size) {
  console.log('ALSO_KEEP (' + alsoKeep.size + '), kept identical to English:');
  for (const k of alsoKeep) {
    const v = output.get(k);
    const words = v == null ? 0 : v.replace(/\{\d+(?::[^}]*)?\}/g, ' ').trim().split(/\s+/).filter(Boolean).length;
    const suspicious = v != null && (words > 2 || v.length > 24);
    console.log('   ' + (suspicious ? '!! suspicious (longer than a word or two) ' : '') + k + ' = ' + JSON.stringify(v));
  }
}

if (notApplied.length) console.log('!! value not applied (regex miss):', notApplied);
if (missingFromMap.length) console.log('!! in neutral but missing from MAP:', missingFromMap);
if (strayMapKeys.length) console.log('!! in MAP but not in neutral:', strayMapKeys);
if (missingFromOutput.length) console.log('!! required key missing from output:', missingFromOutput);
if (arityMismatch.length) console.log('!! placeholder arity differs from neutral:', arityMismatch);
if (machineLeaked.length) console.log('!! machine Cli keys leaked into output:', machineLeaked);
if (humanCliStripped) console.log('!! Cli.EventLogUnavailable stripped: that key is human-facing and must stay');
if (overrideMissing.length) console.log('!! override key missing from output:', overrideMissing);
if (overrideArityMismatch.length) console.log('!! override arity differs from its base key:', overrideArityMismatch);
if (untranslated.length) {
  const show = untranslated.slice(0, 40).join(', ');
  console.log('!! still English (untranslated), ' + untranslated.length + ': ' + show +
    (untranslated.length > 40 ? ', ...and ' + (untranslated.length - 40) + ' more' : ''));
  if (untranslated.length > 50)
    console.log('   (that is most of the file: this is the untranslated template. Translate the MAP values, then a real miss is listed on its own.)');
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  !humanCliStripped &&
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === cliMachineExpected && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
