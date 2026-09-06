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

// Universal keeps: keys whose value is the same in every language, the brand names
// and the pure-placeholder announcement string. Their still-English value is NOT a
// miss. Explicit by KEY on purpose: a future brand key then defaults to "flag until
// someone adds it here", never silently passes. Do NOT translate these values. Do
// NOT edit this list per language.
//
// The four size suffixes and the two elapsed suffixes do not belong in this list,
// because they are not universal: French writes Go/Mo/Ko/o, Russian and Ukrainian
// write ГБ/МБ/КБ/Б and мс/с. Those three carry real
// values in their MAP; the languages that do abbreviate as English does keep them in
// ALSO_KEEP, which is the per-language list. Display.ListSeparator is the same shape,
// for the same reason.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
]);

// Per-language keeps: Polish translates every translatable token (incl. patch ->
// poprawka), so nothing beyond KEEP_ENGLISH stays English.
const ALSO_KEEP = [
  // The list separator Polish uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
  // The size and elapsed unit suffixes. Polish abbreviates them exactly as
  // English does, so there is nothing to translate and nothing to get wrong.
  // A per-language keep rather than a universal one because fr, ru and uk do
  // NOT: French takes Go/Mo/Ko/o, Russian and Ukrainian take ГБ/МБ/КБ/Б and
  // мс/с, and all three carry real values in their MAP.
  'Display.Size.GB',           // {0:F2} GB
  'Display.Size.MB',           // {0:F1} MB
  'Display.Size.KB',           // {0:F1} KB
  'Display.Size.B',            // {0} B
  'Display.Elapsed.Ms',        // {0:F0}ms
  'Display.Elapsed.S',         // {0:F1}s
];

// Polish CLDR-category overrides (satellite-only, read by name via ResourceManager
// in DisplayHelpers.Pluralise's One/Few/Many branches). Base .Plural carries Many
// (genitive plural), so only .Few is needed for the count split, plus a .One/.Few
// pair for the flat Status.RegisteredPackagesFound whose baked-in adjective agrees
// three ways. MissingFromDisk needs no .Few ("Brakuje" governs genitive so 2-4 and
// 5+ collapse); Reassurance/RestoreHint need none (only the pronoun go/je varies).
// Each value's {N} set matches its base key's set.
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
  'Status.RegisteredPackagesFound.One': `Znaleziono {0} zarejestrowany {1}.`,
  'Summary.MissingFromDisk.Unnamed.Few': `{0} pliki, dla których rekordy nie wskazują programu`,
  'Summary.MissingFromDisk.OtherPrograms.Few': `jeszcze {0} programy`,
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
  'Completion.HeldBack.Few': `Zatrzymano {0} pliki. Skanowanie uznało je za niepotrzebne. Końcowa kontrola nie mogła tego potwierdzić.`,
  'Summary.SupersededHeldBack.Few': `InstallerClean nie zdołał ustalić z pewnością, że {0} zastąpione pliki nie są już potrzebne, więc je zatrzymał.`,
  'Cli.SupersededHeldBack.Few': `InstallerClean nie zdołał ustalić z pewnością, że {0} zastąpione pliki nie są już potrzebne, więc je zatrzymał.`,
};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `O programie`,
  'Window.Registered.Title': `Pliki pozostawione bez zmian`,
  'Window.Orphaned.Title': `Niepotrzebne pliki, które można bezpiecznie usunąć`,

  // Section headings
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `POPRAWKI`,
  'Section.Registered.Details': `SZCZEGÓŁY PRODUKTU`,
  'Section.Backup.Folder': `FOLDER KOPII ZAPASOWEJ`,
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
  'Automation.Section.BackupFolder': `Folder kopii zapasowej`,
  'Automation.Section.Patches': `Poprawki`,
  'Automation.Section.ProductDetails': `Szczegóły produktu`,
  'Automation.BackupFolder': `Folder kopii zapasowej`,
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
  'Tooltip.Move': `Przenosi niepotrzebne pliki do folderu kopii zapasowej.`,
  'Tooltip.MoveNeedsDestination': `Przenosi niepotrzebne pliki do folderu kopii zapasowej. Wybierzesz go za chwilę.`,
  'Tooltip.Delete': `Trwale usuwa niepotrzebne pliki. Użyj zamiast tego Przenieś, jeśli chcesz mieć okazję upewnić się, że wszystko jest w porządku.`,
  'Tooltip.SigningCertificate': `Nazwa podmiotu z osadzonego certyfikatu Authenticode. Łańcuch nie został zweryfikowany.`,

  // Body copy
  'Body.MainExplanation.Lead': `Wszystkie niepotrzebne pliki poniżej [można bezpiecznie usunąć].`,
  'Body.MainExplanation.Why': `Leżą w {InstallerFolder}. InstallerClean pyta system Windows o każdy zainstalowany program: plik trafia na listę, gdy żaden program się do niego nie przyznaje ({0}) albo gdy nowsza poprawka go zastąpiła i żaden program nie mógłby do niego wrócić ({1}).`,
  'Body.MainExplanation.Action': `Przenieś je do wybranego przez siebie folderu kopii zapasowej, a potem skasuj ten folder, gdy nabierzesz pewności, że twoje programy nadal normalnie się aktualizują i odinstalowują. Przełożenie ich z powrotem do {InstallerFolder} przywraca wszystko. Albo usuń je trwale już teraz.`,
  'Body.PendingReboot.MsiExecuteMutex': `Coś właśnie korzysta z Instalatora Windows, na przykład aktualizacja systemu albo program instalujący się w tle. Przenieś i Usuń są wstrzymane, dopóki to trwa, żeby InstallerClean nie ruszał {InstallerFolder} w trakcie zmian. Gdy się skończy, skanuj ponownie, a wrócą.`,
  'Body.PendingReboot.InstallerInProgress': `Na tym komputerze jest wstrzymana wcześniejsza transakcja Instalatora Windows. Wznów ją lub wycofaj tę instalację (albo uruchom system ponownie), zanim wyczyścisz {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows ma w kolejce na następne uruchomienie zmianę nazwy pliku, która dotyczy {InstallerFolder}. Uruchom system ponownie, zanim wyczyścisz.`,
  'Body.NoFileSelected': `Wybierz plik, aby zobaczyć szczegóły.`,
  'Body.NoProductSelected': `Wybierz produkt, aby zobaczyć szczegóły.`,
  'Body.NoMetadata': `Brak dostępnych metadanych.`,
  'Body.RegisteredMissingFromDisk': `Brakuje tego pliku instalacyjnego. Teraz nie sprawia to kłopotu i nie będzie go sprawiać aż do dnia, w którym spróbujesz zaktualizować lub odinstalować program, do którego należy. Ten krok może się wtedy nie udać, bo Windows szuka tego pliku, a jego nie ma.\n\nAby go przywrócić, potrzebujesz instalatora tej wersji, którą już masz. Zdobądź go od producenta programu i uruchom na istniejącej kopii. Nowsza wersja nie wystarczy: musiałaby najpierw usunąć tę, którą masz, a to właśnie ten krok potrzebuje tego pliku. Odinstalowanie najpierw też nie zadziała, z tego samego powodu. To powinno przywrócić plik i zostawić twoje ustawienia nietknięte, ale Microsoft tego nie gwarantuje.`,
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
  'Status.MoveFailed': `{0}. Szczegóły w {1}.`,
  'Status.MoveFailed.NoLog': `{0}. Nie udało się zapisać dziennika awarii.`,
  'Status.DeleteFailed': `{0}. Szczegóły w {1}.`,
  'Status.DeleteFailed.NoLog': `{0}. Nie udało się zapisać dziennika awarii.`,
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
  'Summary.NothingListed.Singular': `InstallerClean nie zdołał ustalić z pewnością, które pliki w pamięci podręcznej należą do zainstalowanych tu programów, więc zatrzymał ten jeden plik, zamiast go zaproponować.`,
  'Summary.NothingListed.Plural': `InstallerClean nie zdołał ustalić z pewnością, które pliki w pamięci podręcznej należą do zainstalowanych tu programów, więc zatrzymał {0} {1}, zamiast je zaproponować.`,
  'Summary.MissingFromDisk.Singular': `Windows ma rekord dla {0} pliku, którego nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawia to kłopotu, ale aktualizacja lub odinstalowanie tego programu może się nie udać. Otwórz Szczegóły, aby dowiedzieć się, co zrobić.`,
  'Summary.MissingFromDisk.Plural': `Windows ma rekordy dla {0} plików, których nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawia to kłopotu, ale aktualizacja lub odinstalowanie tych programów może się nie udać. Otwórz Szczegóły, aby dowiedzieć się, co zrobić.`,
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

  'Confirm.DeleteTitle': `Usunąć {0} {1} ({2})?`,

  // Error messages
  'Error.AdminRequiredTitle': `Odmowa dostępu`,
  'Error.AdminRequiredBody': `Windows odmówił InstallerClean dostępu, więc program się zatrzymał. Nic nie zostało usunięte.\n\nInstallerClean działał już jako administrator, więc ponowne uruchomienie go w ten sposób nic nie da. Windows nie mówi nic więcej o tym, co odmówiło dostępu, więc nie ma nic konkretnego do spróbowania.`,
  'Error.InstallerDbUnavailableTitle': `Nie udało się odczytać rekordów Windows Installera`,
  'Error.ScanFailedTitle': `Skanowanie nie powiodło się`,
  'Error.InstallerDbEmpty': `Rekordy Windows Installera wróciły całkowicie puste: ani jeden zainstalowany program ani jedna aktualizacja nie rości sobie prawa do żadnego pliku instalacyjnego w pamięci podręcznej. Na działającym komputerze to się nie zdarza (nawet świeża instalacja Windows ma takie pliki), więc albo rekordy są uszkodzone, albo nie dało się ich odczytać, a skanowanie, które uwierzyłoby w tę odpowiedź, błędnie uznałoby każdy plik w {InstallerFolder} za osierocony. InstallerClean zamiast tego się zatrzymał. Nic nie zostało usunięte.`,
  'Error.MsiAccessDenied': `Windows Installer nie pozwolił InstallerClean wypisać tego, co jest zainstalowane. InstallerClean działał już jako administrator, więc uruchomienie go ponownie jako administrator niczego nie zmieni. Bez tej listy nie da się bezpiecznie stwierdzić, które pliki w pamięci podręcznej są nadal potrzebne, więc InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
  'Error.MsiNonSuccess': `Windows Installer nie zdołał przekazać InstallerClean czytelnej listy zainstalowanych programów: odczytał {2} {3}, a potem {0} wpisów z rzędu wróciło nieczytelnych (ostatni kod błędu {1}). Zamiast pracować na liście odczytanej tylko częściowo, InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
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
  'Error.DestinationNotFullyQualified': `Folder kopii zapasowej musi być pełną ścieżką do folderu, zaczynającą się od litery dysku albo udziału sieciowego (na przykład D:\\Backup albo \\\\serwer\\backup). InstallerClean nie może użyć tej: {0}`,
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
  'BrowserLaunch.ClipboardOk': `The link is on your clipboard, so you can paste it in yourself:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean couldn't copy the link to your clipboard either, so here it is:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `InstallerClean nie mógł już potwierdzić folderu kopii zapasowej, więc zatrzymał się, zamiast zapisać w złym miejscu. Sprawdź {0}, potem Skanuj ponownie i spróbuj jeszcze raz.`,
  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Nie można zapisać w {0}.`,

  // 0 = file name
  'Error.DestinationCollision': `Plik o nazwie „{0}” już jest w folderze kopii zapasowej.`,

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
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Błąd: operacja na pliku zakolejkowana na czas po ponownym uruchomieniu dotyczy {InstallerFolder} ({0}). Uruchom system ponownie, aby ją dokończyć, zanim wyczyścisz.`,
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
  'Completion.MoveCancelledSummary': `Przed anulowaniem przeniesiono {0}/{1} {2}.`,
  'Completion.PermanentDeleteCancelledSummary': `Przed anulowaniem usunięto trwale {0}/{1} {2}.`,
  'Body.PendingReboot.Lead': `Tych plików nie można teraz wyczyścić.`,
  'Cli.TooManyArguments': `Błąd: nieoczekiwany dodatkowy argument „{0}”. Jeśli ścieżka folderu docelowego zawiera spację, ujmij całą ścieżkę w cudzysłów: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Folder dla każdego użytkownika; zaplanowane lub SYSTEM: /m ŚCIEŻKA.`,
  'Error.ScanRecordsUnreadable': `InstallerClean nie zdołał odczytać dość rekordów Windows Installera, by mieć pewność, co jest jeszcze potrzebne: lista zainstalowanych programów wróciła niepełna, a odczyt tych samych rekordów prosto z rejestru również napotkał błędy. Plik mógłby wyglądać na osierocony tylko dlatego, że rekord, który go wymienia, był jednym z nieczytelnych, więc InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer nigdy nie zasygnalizował końca listy zainstalowanych programów: InstallerClean odczytał {2} {3}, a potem poddał się po {0} wpisach (ostatni kod błędu {1}). Liście bez końca nie można ufać, więc InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer nigdy nie zasygnalizował końca listy poprawek jednego programu: InstallerClean odczytał {2} {3}, a potem poddał się po {0} wpisach (ostatni kod błędu {1}). Liście bez końca nie można ufać, więc InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
  'UpdateCheck.Status.UpdateAvailable': `Dostępna jest wersja {0}.`,
  'Completion.DonateAsk': `Cieszę się, że pomogło. Jeśli masz dobre serce, jest miejsce na napiwek.`,
  'About.Link.Guide': `Przewodnik i FAQ`,
  'About.Link.ReportProblem': `Zgłoś problem`,
  'About.AutoUpdateCheck': `Automatycznie sprawdzaj aktualizacje`,
  'Automation.About.Guide.HelpText': `Otwiera readme na githubie w twojej przeglądarce.`,
  'Automation.About.ReportProblem.HelpText': `Otwiera listę zgłoszeń (Issues) na github.com w twojej przeglądarce.`,
  'Automation.AutoUpdateCheck.HelpText': `Jeśli zaznaczone, InstallerClean przy uruchomieniu sprawdza na githubie, czy jest nowsza wersja.`,
  'Tooltip.MoveSameDrive': `Przenosi niepotrzebne pliki do folderu kopii zapasowej. Jest on na tym samym dysku, więc miejsce odzyskasz dopiero po skasowaniu tego folderu.`,
  'Confirm.DeletePermanently.Singular': `Ten plik zostanie trwale usunięty. Można to zrobić bezpiecznie, ale jeśli chcesz kopię zapasową, użyj zamiast tego Przenieś.`,
  'Confirm.DeletePermanently.Plural': `Te pliki zostaną trwale usunięte. Można to zrobić bezpiecznie, ale jeśli chcesz kopię zapasową, użyj zamiast tego Przenieś.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean nie zdołał skłonić systemu Windows do rozwinięcia prawdziwej ścieżki {InstallerFolder}, więc o żadnym pliku nie dało się wykazać, że jest w środku, i żadnego nie zaproponowano do wyczyszczenia. To skanowanie niczego nie znalazło dlatego, że ta kontrola się nie powiodła, a nie dlatego, że folder jest czysty. Niczego nie usunięto.`,
  'Automation.Scroll.ProductDetails': `Szczegóły produktu`,
  'Body.PendingReboot.Other': `Instalator Windows ma coś w toku, więc Przenieś i Usuń są wstrzymane. InstallerClean nie ruszy {InstallerFolder} w trakcie zmian. Gdy się skończy, skanuj ponownie, a wrócą.`,
  'Cli.TooManyArgumentsNoPath': `Błąd: nieoczekiwany dodatkowy argument „{0}”. /s i /d nie przyjmują dalszych argumentów, a w jednym uruchomieniu można użyć tylko jednego przełącznika.`,
  'Cli.MissingFromDisk.Singular': `Windows ma rekord dla {0} pliku, którego nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawia to kłopotu, ale aktualizacja lub odinstalowanie tego programu może się nie udać. Aby przywrócić plik, potrzebujesz instalatora tej wersji, którą już masz. Zdobądź go od producenta programu i uruchom na istniejącej kopii. Nowsza wersja nie wystarczy: musiałaby najpierw usunąć tę, którą masz, a to właśnie ten krok potrzebuje tego pliku. Odinstalowanie najpierw też nie zadziała, z tego samego powodu. To powinno przywrócić plik i zostawić twoje ustawienia nietknięte, ale Microsoft tego nie gwarantuje.`,
  'Cli.MissingFromDisk.Plural': `Windows ma rekordy dla {0} plików, których nie ma w {InstallerFolder}: {1}. Na co dzień nie sprawia to kłopotu, ale aktualizacja lub odinstalowanie tych programów może się nie udać. Aby przywrócić plik, potrzebujesz instalatora tej wersji tego programu, którą już masz. Zdobądź go od producenta programu i uruchom na istniejącej kopii. Nowsza wersja nie wystarczy: musiałaby najpierw usunąć tę, którą masz, a to właśnie ten krok potrzebuje tego pliku. Odinstalowanie najpierw też nie zadziała, z tego samego powodu. To powinno przywrócić plik i zostawić twoje ustawienia nietknięte, ale Microsoft tego nie gwarantuje.`,
  'Cli.MoveNotEnoughSpace': `Błąd: za mało miejsca w {0}. Przeniesienie tych plików wymaga {1}, a wolne jest {2}. Niczego nie przeniesiono.`,
  'Cli.PendingRebootBlocked.Other': `Błąd: Instalator Windows ma coś w toku, więc /m i /d są zablokowane. InstallerClean nie ruszy {InstallerFolder} w trakcie zmian. Spróbuj ponownie, gdy się skończy.`,
  'Cli.FoundNoOrphans': `Nie znaleziono niepotrzebnych plików.`,
  'Cli.NothingOffered.Singular': `InstallerClean nie zdołał ustalić z pewnością, które pliki w pamięci podręcznej należą do zainstalowanych tu programów, więc zatrzymał ten jeden plik ({2}), zamiast go zaproponować.`,
  'Cli.NothingOffered.Plural': `InstallerClean nie zdołał ustalić z pewnością, które pliki w pamięci podręcznej należą do zainstalowanych tu programów, więc zatrzymał wszystkie {0} {1} ({2}), zamiast je zaproponować.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean nie mógł już potwierdzić folderu kopii zapasowej, więc zatrzymał się, zamiast zapisać w złym miejscu. Sprawdź {0}, a potem uruchom polecenie ponownie.`,
  'Cli.Help.Summary': `Usuwa .msi i .msp z cache, zbędne każdemu zainstalowanemu programowi.`,
  'Cli.Help.Elevation': `Wymaga wiersza polecenia administratora; inaczej Windows go nie uruchomi.`,
  'Error.InstallerLockUnavailableTitle': `Niczego nie usunięto`,
  'Error.MoveInstallerLockUnavailableTitle': `Niczego nie przeniesiono`,
  'Error.InstallerLockUnavailable': `InstallerClean nie zdołał przejąć blokady, którą Instalator Windows powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy, i niczego nie usunięto. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean nie zdołał przejąć blokady, którą Instalator Windows powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy, i niczego nie przeniesiono. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie.`,
  'Cli.InstallerLockUnavailable': `Błąd: InstallerClean nie zdołał przejąć blokady Instalatora Windows, która powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy. Niczego nie usunięto. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie.`,
  'Cli.MoveInstallerLockUnavailable': `Błąd: InstallerClean nie zdołał przejąć blokady Instalatora Windows, która powstrzymuje dwa programy przed jednoczesną zmianą zainstalowanego oprogramowania, więc nie mógł wykluczyć, że plik stanie się potrzebny w połowie pracy. Niczego nie przeniesiono. Spróbuj ponownie, a jeśli to się powtarza, uruchom system ponownie.`,
  'Completion.ReverifyIdentityClaimed': `Pozostawiono na miejscu {0} {1}, ponieważ Windows ma rekord programu wskazanego w środku.`,
  'Completion.ReverifyIdentityUnreadable': `Pozostawiono na miejscu {0} {1}, ponieważ InstallerClean nie znalazł w środku nazwy żadnego programu.`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean nie zdołał dopasować rekordów Instalatora Windows do zawartości {InstallerFolder}. W folderze są pliki, ale ani jeden rekord nie wskazuje niczego w środku, więc o żadnym pliku nie dało się wykazać, że jest niepotrzebny. Niczego nie zaproponowano i niczego nie usunięto.`,
  'Completion.NothingOffered': `Na tym komputerze niczego nie zaproponowano`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean nie zdołał ustalić z pewnością, które pliki w pamięci podręcznej należą do zainstalowanych tu programów, więc zatrzymał ten jeden plik ({2}), zamiast go zaproponować.`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean nie zdołał ustalić z pewnością, które pliki w pamięci podręcznej należą do zainstalowanych tu programów, więc zatrzymał wszystkie {0} {1} ({2}), zamiast je zaproponować.`,
  'Summary.SupersededHeldBack.Singular': `InstallerClean nie zdołał ustalić z pewnością, że jedyny zastąpiony plik nie jest już potrzebny, więc go zatrzymał.`,
  'Summary.SupersededHeldBack.Plural': `InstallerClean nie zdołał ustalić z pewnością, że {0} zastąpionych plików nie jest już potrzebnych, więc je zatrzymał.`,
  'Cli.SupersededHeldBack.Singular': `InstallerClean nie zdołał ustalić z pewnością, że jedyny zastąpiony plik nie jest już potrzebny, więc go zatrzymał.`,
  'Cli.SupersededHeldBack.Plural': `InstallerClean nie zdołał ustalić z pewnością, że {0} zastąpionych plików nie jest już potrzebnych, więc je zatrzymał.`,
  'Completion.HeldBack.Singular': `Zatrzymano {0} plik. Skanowanie uznało go za niepotrzebny. Końcowa kontrola nie mogła tego potwierdzić.`,
  'Completion.HeldBack.Plural': `Zatrzymano {0} plików. Skanowanie uznało je za niepotrzebne. Końcowa kontrola nie mogła tego potwierdzić.`,
  'Body.PendingReboot.PendingRenameUnresolved': `Operacja na plikach czeka w kolejce na następny restart, a InstallerClean nie potrafi ustalić, których plików dotyczy, więc nie może wykluczyć, że są w {InstallerFolder}. Uruchom Windows ponownie przed czyszczeniem.`,
  'Completion.MoveRestoreHint': `Skasuj ten folder, gdy nabierzesz pewności, że wszystko jest w porządku.`,
  'Completion.MoveRestoreHintSameDrive': `Skasuj ten folder, gdy nabierzesz pewności, że wszystko jest w porządku. Dopiero wtedy naprawdę odzyskasz miejsce.`,
  'Confirm.MoveDestination.Singular': `Ten plik zostanie przeniesiony do:`,
  'Confirm.MoveDestination.Plural': `Te pliki zostaną przeniesione do:`,
  'Cli.NothingListed.Singular': `InstallerClean nie zdołał ustalić z pewnością, które pliki w pamięci podręcznej należą do zainstalowanych tu programów, więc zatrzymał ten jeden plik ({2}), zamiast go zaproponować.`,
  'Cli.NothingListed.Plural': `InstallerClean nie zdołał ustalić z pewnością, które pliki w pamięci podręcznej należą do zainstalowanych tu programów, więc zatrzymał {0} {1} ({2}), zamiast je zaproponować.`,
  'Cli.WithheldReasons.Header': `Dlaczego nie było pewności:`,
  'Cli.WithheldReasons.RecordedPath': `  Nie udało się rozwiązać ścieżki pliku z własnych rejestrów Windows Installer, więc nie dało się do niej niczego dopasować.`,
  'Cli.WithheldReasons.FileIdentity': `  Nie udało się zidentyfikować pliku, o którym Windows ma zapis, więc nie dało się go dopasować do zawartości folderu.`,
  'Cli.WithheldReasons.SecondInstance': `  Program może być zainstalowany na tym komputerze więcej niż raz, a rejestry nie potrafią powiedzieć, do której kopii należy plik.`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `Błąd: operacja na plikach czeka w kolejce na następny restart, a InstallerClean nie potrafi ustalić, których plików dotyczy, więc nie może wykluczyć {InstallerFolder}. Uruchom Windows ponownie przed czyszczeniem.`,
  'Cli.MoveRestoreHint': `Sprawdź, czy twoje programy nadal normalnie się aktualizują i odinstalowują, a potem skasuj {0}.`,
  'Error.ScanStoppedDetails': `Jest to zapisywane także w {0}.`,
  'Cli.NothingListedPerFile.Singular': `InstallerClean nie miał pewności co do jednego ze znalezionych plików w pamięci podręcznej, więc zatrzymał ten jeden ({2}), zamiast go zaproponować.`,
  'Cli.NothingListedPerFile.Plural': `InstallerClean nie miał pewności co do niektórych ze znalezionych plików w pamięci podręcznej, więc zatrzymał {0} {1} ({2}), zamiast je zaproponować.`,
  'Cli.NothingOfferedPerFile.Singular': `InstallerClean nie zdołał ustalić, że znaleziony plik w pamięci podręcznej jest niepotrzebny, więc zatrzymał ten jeden plik ({2}), zamiast go zaproponować.`,
  'Cli.NothingOfferedPerFile.Plural': `InstallerClean o żadnym ze znalezionych plików w pamięci podręcznej nie zdołał ustalić, że jest niepotrzebny, więc zatrzymał wszystkie {0} {1} ({2}), zamiast je zaproponować.`,
  'Completion.NothingOfferedPerFileBody.Singular': `InstallerClean nie zdołał ustalić, że znaleziony plik w pamięci podręcznej jest niepotrzebny, więc zatrzymał ten jeden plik ({2}), zamiast go zaproponować.`,
  'Completion.NothingOfferedPerFileBody.Plural': `InstallerClean o żadnym ze znalezionych plików w pamięci podręcznej nie zdołał ustalić, że jest niepotrzebny, więc zatrzymał wszystkie {0} {1} ({2}), zamiast je zaproponować.`,
  'Summary.NothingListedPerFile.Singular': `InstallerClean nie miał pewności co do jednego ze znalezionych plików w pamięci podręcznej, więc go zatrzymał, zamiast go zaproponować.`,
  'Summary.NothingListedPerFile.Plural': `InstallerClean nie miał pewności co do niektórych ze znalezionych plików w pamięci podręcznej, więc zatrzymał {0} {1}, zamiast je zaproponować.`,
  'Cli.WithheldReasons.CandidateIdentity': `  Nie udało się zidentyfikować pliku w folderze, więc nie dało się go dopasować do rejestrów.`,
  'Cli.WithheldReasons.DeclaredProductInstalled': `  Plik podaje, że należy do programu, który jest nadal zainstalowany, więc może być jeszcze potrzebny.`,
  'Cli.WithheldReasons.DeclaredProductUnestablished': `  Albo plik nie podał, do którego programu należy, albo Windows nie udzielił odpowiedzi na temat tego programu.`,
  'Cli.WithheldReasons.ScreenUnanswered': `  Sprawdzenie, do których programów należą pliki, dało odpowiedzi, które nie zgadzały się z przekazanymi mu plikami.`,
  'Body.PendingReboot.RegistryCheckUnreadable': `InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean.`,
  'Cli.InstallerLockAccessRefused': `Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted.`,
  'Cli.MoveCancelledRestoreHint': `It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was.`,
  'Cli.MoveInstallerLockAccessRefused': `Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.RegistryCheckUnreadable': `Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean.`,
  'Completion.MoveCancelledRestoreHint': `It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was.`,
  'Error.InstallerLockAccessRefused': `Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted.`,
  'Error.MoveInstallerLockAccessRefused': `Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved.`,
  'Error.MoveStoppedTitle': `Move stopped`,
  'Field.NoNamedProduct': `(no program)`,
  'Summary.RegisteredWindow.Missing.Plural': `{0} missing`,
  'Summary.RegisteredWindow.Missing.Singular': `{0} missing`,
  'UpdateCheck.Failed.Unknown.NoLog': `The check failed for an unknown reason. The crash log could not be written.`,
};

// PARSE CONTROL. About the READING and not about the content, and it exits 2,
// which is a code no ordinary run of this generator can produce: a generator is
// red by intent for the whole gap between a string landing in English and its
// translation round, so its verdict lines and its exit 0 are load-bearing in
// ci.yml and are deliberately untouched here. This says something different from
// "the translation is not done". It says the file could not be read.
//
// BOTH LEGS. raw === 0 catches a file that declares no entry at all, which the
// equality cannot see on its own because 0 === 0 holds. parsed !== raw catches
// entries the reader dropped, which one <comment> moved above its <value> does to
// any regex wanting <value> on the same whitespace run, and the Visual Studio resx
// editor writes that shape. Counted with <data\b so a tab after the tag name is
// not read as an empty file, and neither figure is written down, so a string added
// to the resx cannot make this go stale.
//
// WHY IT IS HERE WHEN THE SELF-CHECK BELOW ALREADY REDDENS. The self-check reaches
// the right verdict through what it happens to compare, not through knowing it
// read anything: with the neutral's attribute order changed, this generator wrote a
// 389-entry file and its own self-check parsed THREE entries out of it, said
// GENERATION HAS ISSUES, and was right for a reason with nothing to do with the
// truth. A tool reasoning over three entries of a 389-entry artefact should say so.
const parseControl = (where, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${where}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this generator cannot show it read.');
  process.exit(2);
};

let text = readFileSync(BASE, 'utf8');
// The transform below reaches every entry through '<data name="', one space and no
// \s+, which is NOT the spelling the self-check's parse() uses further down. A
// control that exercises a pattern the reader does not use proves the file has
// structure and proves nothing about whether this reader can reach it, so the
// source is controlled in its own spelling before a single value is replaced.
parseControl(BASE, text,
  [...text.matchAll(/<data name="([^"]+)"[^>]*>\s*<value>/g)].length);

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
const parse = (xml, where) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  parseControl(where, xml, map.size);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'), BASE);
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written, OUT);
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
