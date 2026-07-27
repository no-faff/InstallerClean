#!/usr/bin/env node
// Polish (pl) satellite generator for InstallerClean. Copied from
// gen-strings-template.mjs; only OUT, OVERRIDES and the MAP values changed. Works
// FROM THE ENGLISH SOURCE Strings.resx: strips the 21 machine-contract Cli.* keys
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
const ALSO_KEEP = [];

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
  'Summary.RegisteredStillUsed.Few': `{0} pliki nadal potrzebne`,
  'Summary.OrphanedToCleanUp.Few': `{0} niepotrzebne pliki do wyczyszczenia`,
  'Summary.RegisteredWindow.Few': `{0} zarejestrowane pliki nadal potrzebne ({1})`,
  'Completion.PermanentDeleteSummary.Few': `Usunięto trwale {0} {1}. Nie trafiły do Kosza.`,
  'Completion.ReverifySkipped.One': `Pozostawiono na miejscu {0} {1}, ponieważ po skanowaniu program znów zaczął go potrzebować.`,
  'RecycleUnavailable.Body.Few': `Dlatego te {0} {1} ({2}) nie zostały usunięte. Możesz przenieść je w bezpieczne miejsce lub usunąć trwale.`,
  'Status.RegisteredPackagesFound.One': `Znaleziono {0} zarejestrowany {1}.`,
  'Status.RegisteredPackagesFound.Few': `Znaleziono {0} zarejestrowane {1}.`,
};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `O programie`,
  'Window.Registered.Title': `Zarejestrowane pliki, których nie należy usuwać`,
  'Window.Orphaned.Title': `Niepotrzebne pliki, które można bezpiecznie usunąć`,

  // Section headings
  'Section.Registered.Products': `PRODUKTY`,
  'Section.Registered.Patches': `POPRAWKI`,
  'Section.Registered.Details': `SZCZEGÓŁY PRODUKTU`,
  'Section.Move.Location': `LOKALIZACJA PRZENOSZENIA`,
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
  'Action.Delete': `_Usuń`,
  'Action.DeletePermanently': `Usuń _trwale`,
  'Action.Done': `_Gotowe`,
  'Action.Details': `Szczegóły`,
  'Action.BuyMeACuppa': `Postaw mi _kawę`,
  'Action.LeaveStarOnGitHub': `Zostaw _gwiazdkę na GitHubie`,
  'Action.Licence': `Licencja Apache 2.0`,
  'Action.Move': `_Przenieś`,
  'Action.MoveInstead': `_Przenieś zamiast tego`,
  'Action.MoveDestinationPlaceholder': `Ścieżka do folderu, jeśli zamiast usuwać wybierzesz Przenieś`,
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
  'Automation.ConfirmDelete': `Usuń przenosi niepotrzebne pliki do Kosza. Anuluj zamyka okno bez usuwania.`,
  'Automation.ConfirmMove': `Przenieś umieszcza niepotrzebne pliki w wybranym folderze docelowym. Anuluj zostawia je na miejscu.`,
  'Automation.RecycleUnavailable': `Wybierz, co zrobić z niepotrzebnymi plikami: przenieść je w bezpieczne miejsce, usunąć trwale lub anulować.`,
  'Automation.RecycleUnavailableMove': `Przenieś niepotrzebne pliki do wybranego folderu`,
  'Automation.RecycleUnavailableDeletePermanently': `Usuń niepotrzebne pliki trwale, ponieważ Kosz jest niedostępny dla tego dysku`,
  'Automation.SayThanks': `Podziękuj`,
  'Automation.ConfirmSendResultLog': `Wyślij przekazuje pokazany raport do No Faff. Anuluj nie wysyła niczego.`,
  'Automation.CheckForUpdates': `Sprawdź aktualizacje`,
  'Automation.CheckForUpdates.HelpText': `Sprawdza na stronie wydań githuba, czy jest nowsza wersja.`,
  'Automation.UpdateAvailable.HelpText': `Otwórz stronę wydania, aby pobrać nowszą wersję, lub anuluj, aby zachować bieżącą.`,
  'Automation.Licence.HelpText': `Otwiera plik licencji na github.com w twojej przeglądarce.`,
  'Automation.Section.MoveLocation': `Lokalizacja przenoszenia`,
  'Automation.Section.Products': `Produkty`,
  'Automation.Section.Patches': `Poprawki`,
  'Automation.Section.ProductDetails': `Szczegóły produktu`,
  'Automation.MoveDestinationFolder': `Lokalizacja przenoszenia`,
  'Automation.OperationProgress': `Postęp operacji`,
  'Automation.RescanInstaller': `Skanuj ponownie {InstallerFolder}`,
  'Automation.ScanningProgress': `Postęp skanowania`,
  'Automation.StartupScanProgress': `Postęp skanowania startowego`,
  'Automation.ViewOrphanedFiles': `Szczegóły, niepotrzebne pliki`,
  'Automation.ViewOrphanedFiles.HelpText': `Dostępne do wyczyszczenia.`,
  'Automation.ViewRegisteredFiles': `Szczegóły, zarejestrowane pliki`,
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
  'Tooltip.Move': `Przenieś niepotrzebne pliki do lokalizacji przenoszenia.`,
  'Tooltip.MoveNeedsDestination': `Przenieś niepotrzebne pliki w bezpieczne miejsce. Folder wybierzesz w następnym kroku.`,
  'Tooltip.Delete': `Przenieś niepotrzebne pliki do Kosza.`,
  'Tooltip.SigningCertificate': `Nazwa podmiotu z osadzonego certyfikatu Authenticode. Łańcuch nie został zweryfikowany.`,

  // Body copy
  'Body.MainExplanation.Lead': `Wszelkie niepotrzebne pliki poniżej można bezpiecznie usunąć.`,
  'Body.MainExplanation.Why': `Leżą w {InstallerFolder}, pozostawione po odinstalowaniu programu ({0}), gdy nowsza poprawka zastąpiła jedną z nich ({1}) lub gdy wydawca ją wycofał ({2}). InstallerClean wymienia wyłącznie pliki, które sam Windows zgłasza jako już niepotrzebne.`,
  'Body.MainExplanation.Action': `Usuń je do Kosza albo użyj zamiast tego Przenieś, aby zachować kopię zapasową. Umieszczenie plików z powrotem w {InstallerFolder} cofa wszystko dokładnie do punktu wyjścia.`,
  'Body.PendingReboot.MsiExecuteMutex': `Coś właśnie korzysta z Windows Installer, zwykle aktualizacja Windows albo program instalujący się w tle. Na ten czas Przenieś i Usuń są wstrzymane, aby InstallerClean nie ruszał pamięci podręcznej instalatora, gdy ta się zmienia. Gdy to się zakończy, skanuj ponownie, a wrócą.`,
  'Body.PendingReboot.InstallerInProgress': `Na tym komputerze zawieszona jest wcześniejsza transakcja Windows Installer. Przed wyczyszczeniem pamięci podręcznej dokończ lub wycofaj tamtą instalację (albo uruchom ponownie Windows).`,
  'Body.PendingReboot.PendingRenameInCache': `Windows ma w kolejce na następne uruchomienie zmianę nazwy pliku, która dotyczy pamięci podręcznej instalatora. Przed czyszczeniem uruchom ponownie Windows.`,
  'Body.NoFileSelected': `Wybierz plik, aby zobaczyć szczegóły.`,
  'Body.NoProductSelected': `Wybierz produkt, aby zobaczyć szczegóły.`,
  'Body.NoMetadata': `Brak dostępnych metadanych.`,
  'Body.RegisteredMissingFromDisk': `Ten plik instalatora został usunięty. InstallerClean tego nie zrobił, nigdy nie usuwa pliku, którego program wciąż potrzebuje; ten usunęło coś innego, zanim uruchomiono InstallerClean.&#10;&#10;Na razie nie sprawia to żadnych kłopotów i nie sprawi, dopóki pewnego dnia nie spróbujesz naprawić, zaktualizować lub odinstalować programu, do którego należy. Ten krok może się wtedy nie powieść, bo Windows szuka tego pliku, a jego nie ma.&#10;&#10;Aby spróbować to naprawić, pobierz instalator tego programu od jego producenta i uruchom go na istniejącej kopii (nie odinstalowuj wcześniej, odinstalowanie samo w sobie jest krokiem, który potrzebuje tego pliku). Jeśli możesz, użyj wersji, którą masz zainstalowaną, bo Windows może odrzucić inną. Zwykle przywraca to plik, a twoje ustawienia zazwyczaj pozostają nietknięte, ale Microsoft tego nie gwarantuje, jego własną ostatecznością jest ponowna instalacja programu lub samego Windows.`,
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
  'Status.Moving': `Przenoszenie: {0} {1}...`,
  'Status.Deleting': `Usuwanie: {0} {1}...`,
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
  'Completion.MoveRestoreHint': `Skopiuj je z powrotem do {InstallerFolder}, gdyby coś się kiedykolwiek zepsuło ([skrajnie mało prawdopodobne]).`,
  'Completion.DeleteRestoreHint': `Do tego czasu możesz je przywrócić, gdyby coś się kiedykolwiek zepsuło ([skrajnie mało prawdopodobne]).`,

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
  'Completion.DeleteSummary.Singular': `Przeniesiono {0} {1} do Kosza`,
  'Completion.DeleteSummary.Plural': `Przeniesiono {0} {1} do Kosza`,

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `Usunięto trwale {0} {1}. Nie trafił do Kosza.`,
  'Completion.PermanentDeleteSummary.Plural': `Usunięto trwale {0} {1}. Nie trafiło do Kosza.`,
  'Completion.PermanentDeleteRestoreHint.Singular': `W porządku, można go było bezpiecznie usunąć. InstallerClean usuwa tylko pliki, które Windows zgłasza jako już niepotrzebne, nigdy takiego, którego program wciąż potrzebuje. W mało prawdopodobnym przypadku, gdyby usunięcie kiedykolwiek pozbawiło program możliwości naprawy, aktualizacji lub odinstalowania, ponowna instalacja od producenta zwykle przywraca plik, choć Microsoft tego nie gwarantuje.`,
  'Completion.PermanentDeleteRestoreHint.Plural': `W porządku, można je było bezpiecznie usunąć. InstallerClean usuwa tylko pliki, które Windows zgłasza jako już niepotrzebne, nigdy takiego, którego program wciąż potrzebuje. W mało prawdopodobnym przypadku, gdyby usunięcie kiedykolwiek pozbawiło program możliwości naprawy, aktualizacji lub odinstalowania, ponowna instalacja od producenta zwykle przywraca plik, choć Microsoft tego nie gwarantuje.`,
  'RecycleUnavailable.Heading': `Kosz nie jest dostępny dla tego dysku`,
  'RecycleUnavailable.Body.Singular': `Dlatego ten {1} ({2}) nie został usunięty. Możesz przenieść go w bezpieczne miejsce lub usunąć trwale.`,
  'RecycleUnavailable.Body.Plural': `Dlatego te {0} {1} ({2}) nie zostało usuniętych. Możesz przenieść je w bezpieczne miejsce lub usunąć trwale.`,
  'RecycleUnavailable.Reassurance.Singular': `Można go bezpiecznie usunąć. InstallerClean usuwa tylko pliki, które Windows zgłasza jako już niepotrzebne, nigdy takiego, którego program wciąż potrzebuje, a Kosz to tylko dodatkowe zabezpieczenie. W mało prawdopodobnym przypadku, gdyby usunięcie kiedykolwiek pozbawiło program możliwości naprawy, aktualizacji lub odinstalowania, ponowna instalacja od producenta zwykle przywraca plik, choć Microsoft tego nie gwarantuje.`,
  'RecycleUnavailable.Reassurance.Plural': `Można je bezpiecznie usunąć. InstallerClean usuwa tylko pliki, które Windows zgłasza jako już niepotrzebne, nigdy takiego, którego program wciąż potrzebuje, a Kosz to tylko dodatkowe zabezpieczenie. W mało prawdopodobnym przypadku, gdyby usunięcie kiedykolwiek pozbawiło program możliwości naprawy, aktualizacji lub odinstalowania, ponowna instalacja od producenta zwykle przywraca plik, choć Microsoft tego nie gwarantuje.`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} plik nadal potrzebny`,
  'Summary.RegisteredStillUsed.Plural': `{0} plików nadal potrzebnych`,
  'Summary.OrphanedToCleanUp.Singular': `{0} niepotrzebny plik do wyczyszczenia`,
  'Summary.OrphanedToCleanUp.Plural': `{0} niepotrzebnych plików do wyczyszczenia`,
  'Summary.MissingFromDisk.Singular': `Brakuje {0} zarejestrowanego pliku (nie usunął go InstallerClean). Na razie to nie problem, ale w przyszłości naprawa, aktualizacja lub odinstalowanie tego programu mogą się nie powieść. Otwórz Szczegóły, aby dowiedzieć się, co zrobić.`,
  'Summary.MissingFromDisk.Plural': `Brakuje {0} zarejestrowanych plików (nie usunął ich InstallerClean). Na razie to nie problem, ale w przyszłości naprawa, aktualizacja lub odinstalowanie tych programów mogą się nie powieść. Otwórz Szczegóły, aby dowiedzieć się, co zrobić.`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0}/{1} {2}`,

  // Orphaned-window footer: removable causes. Reason stems in the genitive-plural
  // elliptical form (invariant across counts). 0/1/2 = counts, 3 = size.
  'Summary.OrphanedWindow': `{0} osieroconych, {1} zastąpionych, {2} przestarzałych ({3})`,

  // Registered-window footer; split singular/plural, .Few in OVERRIDES. 0 = count, 1 = size.
  'Summary.RegisteredWindow.Singular': `{0} zarejestrowany plik nadal potrzebny ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} zarejestrowanych plików nadal potrzebnych ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Przenieść {0} {1} ({2})?`,

  // 0 = destination path
  'Confirm.MoveDestination': `Pliki zostaną przeniesione do:`,
  'Confirm.DeleteTitle': `Usunąć {0} {1} ({2})?`,
  'Confirm.DeleteToRecycleBin': `Pliki zostaną przeniesione do Kosza. Jeśli chcesz mieć kopie zapasowe, użyj zamiast tego przycisku Przenieś.`,

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
  'Error.DestinationInSystemFolder': `Folder docelowy {0} prowadzi do folderu systemowego Windows. Wybierz ścieżkę poza %SystemRoot%, %ProgramFiles% i %ProgramData%.`,
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
  'Error.FileInUse.Singular': `Ten plik jest otwarty lub zablokowany przez inny program, więc nic nie może go teraz przenieść. Został pozostawiony na miejscu; spróbuj ponownie później.`,
  'Error.FileInUse.Plural': `Te pliki są otwarte lub zablokowane przez inny program, więc nic nie może ich teraz przenieść. Zostały pozostawione na miejscu; spróbuj ponownie później.`,
  'Error.IOFailure.Singular': `Windows zgłosił błąd pliku; plik został pozostawiony na miejscu.`,
  'Error.IOFailure.Plural': `Windows zgłosił błędy plików; te pliki zostały pozostawione na miejscu.`,
  'Error.UnknownError.Singular': `Coś poszło nie tak z tym plikiem; został pozostawiony na miejscu.`,
  'Error.UnknownError.Plural': `Coś poszło nie tak z tymi plikami; zostały pozostawione na miejscu.`,

  // 0 = shell error code
  'Error.ShellRecycleFailed': `Nie udało się przenieść tego pliku do Kosza (błąd {0}), a na podstawie tego kodu InstallerClean nie potrafi powiedzieć dlaczego. Plik został pozostawiony na miejscu. Spróbuj zamiast tego przycisku Przenieś, który nie korzysta z Kosza.`,
  'Error.RecycleAccessDenied': `Windows odmówił dostępu nawet z uprawnieniami administratora (błąd {0}), a InstallerClean nie potrafi stwierdzić, czy problemem jest plik, czy Kosz. Plik został pozostawiony na miejscu. Przycisk Przenieś zadziała, jeśli problemem jest Kosz, ale nie wtedy, gdy jest nim plik.`,
  'Error.RecycleInUse': `Ten plik jest otwarty lub zablokowany przez inny program (błąd {0}), więc nic nie może go teraz usunąć. Został pozostawiony na miejscu; spróbuj ponownie później.`,
  'Error.DeletedNotRecycled': `Windows usunął ten plik trwale, zamiast przenieść go do Kosza. InstallerClean poprosił o Kosz, a Windows zrobił co innego. Pliku już nie ma.`,

  // 0 = destination
  'Error.MoveIntoInstaller': `Odmowa przeniesienia plików do folderu Windows Installer (cel: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `Lokalizacja przenoszenia musi być pełną ścieżką do folderu, zaczynającą się od litery dysku lub udziału sieciowego (na przykład D:\\Backup lub \\\\server\\backup). InstallerClean nie może użyć tej ścieżki: {0}`,
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
  'Error.DestinationChangedMidBatch': `Lokalizacja przenoszenia zmieniła się w trakcie przenoszenia plików (coś podmieniło lub przekierowało ten folder), więc InstallerClean przerwał, zamiast zapisać w niewłaściwym miejscu. Sprawdź {0}, a następnie użyj przycisku Skanuj ponownie i spróbuj jeszcze raz.`,

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
  'Display.ElapsedLong.LessThanASecond': `mniej niż sekunda`,
  'Display.ElapsedLong.Seconds': `{0:F1} sekundy`,
  'CrashLog.PrivacyHeader': `# crash.log rejestruje nieobsłużone wyjątki InstallerClean.\n# Przy podwyższonych uprawnieniach komunikaty wyjątków platformy\n# mogą zawierać ścieżki plików z bieżącej sesji (w tym profile\n# innych użytkowników wyliczone przez zapytania Windows Installer).\n# Komunikaty o błędach sieci ze sprawdzania aktualizacji lub\n# wysyłania dziennika wyników mogą zawierać docelowy adres URL\n# oraz rozwiązany adres IP / proxy. Usuń oba rodzaje danych przed\n# dołączeniem tego pliku do publicznego zgłoszenia błędu.\n`,
  'Tooltip.ChangeLanguage': `Zmień język. Program zostanie ponownie uruchomiony.`,
  'Automation.ChangeLanguage': `Zmień język`,
  'Automation.ChangeLanguage.HelpText': `Program zostanie ponownie uruchomiony.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys. The
  // 21 machine-contract Cli.EventLog* keys (bar Cli.EventLogUnavailable) are NOT
  // here and are stripped from the output; they stay English at runtime. In the
  // help lines translate the DESCRIPTION only: keep the command tokens, flags,
  // the {InstallerFolder} token and the exit-code numbers verbatim, keep the
  // leading spaces (the screen is column-aligned for a monospace terminal), and
  // translate the PATH metavariable (ŚCIEŻKA).
  'Cli.UnknownArgument': `Nieznany argument: „{0}”`,
  'Cli.Cancelling': `Anulowanie...`,
  'Cli.Cancelled': `Anulowano.`,
  'Cli.GenericError': `Błąd: {0}. Szczegóły zapisano w {1}.`,
  'Cli.GenericError.NoLog': `Błąd: {0}. Nie udało się zapisać dziennika awarii.`,
  'Cli.ScanningInstaller': `Skanowanie {InstallerFolder}...`,
  'Cli.FoundOrphans': `Znaleziono {0} {1} do wyczyszczenia ({2}).`,
  'Cli.NothingToDo': `Nie ma nic do zrobienia.`,
  'Cli.DeletingFiles': `Usuwanie: {0} {1}...`,
  'Cli.DeletedFiles': `Usunięto {0} {1}.`,
  'Cli.RecycleUnavailable': `Błąd: Kosz jest niedostępny dla tego woluminu, więc nic nie usunięto. Użyj /m, aby zamiast tego przenieść pliki, albo ponownie włącz Kosz i uruchom jeszcze raz.`,
  'Cli.NoMoveDestination': `Błąd: nie podano folderu docelowego przenoszenia. Użyj /m ŚCIEŻKA. (Lokalizacja domyślna ustawiona w GUI jest przypisana do użytkownika i nie dotyczy uruchomień zaplanowanych ani na koncie usługi.)`,
  'Cli.MoveDestinationInsideInstaller': `Błąd: folder docelowy nie może znajdować się wewnątrz folderu Windows Installer.`,
  'Cli.MoveDestinationRelative': `Błąd: folder docelowy musi być pełną ścieżką. Otrzymano: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Błąd: folder docelowy {0} prowadzi do folderu systemowego Windows. Wybierz ścieżkę poza %SystemRoot%, %ProgramFiles% i %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Błąd: coś właśnie korzysta z Windows Installer, zwykle aktualizacja Windows albo program instalujący się w tle. Przenoszenie i usuwanie są zablokowane, dopóki to trwa. Spróbuj ponownie, gdy się zakończy.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Błąd: na tym komputerze zawieszona jest wcześniejsza transakcja Windows Installer. Przed wyczyszczeniem pamięci podręcznej dokończ lub wycofaj tamtą instalację (albo uruchom ponownie Windows).`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Błąd: zakolejkowana po ponownym uruchomieniu operacja na pliku dotyczy pamięci podręcznej instalatora ({0}). Przed czyszczeniem uruchom ponownie Windows, aby dokończyć tę operację.`,
  'Cli.MovingFiles': `Przenoszenie: {0} {1} do {2}...`,
  'Cli.MovedFiles': `Przeniesiono {0} {1}.`,
  'Cli.MutexBlocked': `Inny proces InstallerClean trzyma blokadę pojedynczej instancji (GUI lub inne uruchomienie CLI). Kod zakończenia 75 (stan przejściowy); można bezpiecznie spróbować ponownie później.`,
  'Cli.EventLogUnavailable': `Uwaga: zapis do dziennika zdarzeń nie powiódł się. Sprawdź uprawnienia dziennika „Aplikacja” lub zasady grupy.`,
  'Cli.Help.Header': `InstallerClean - oczyszczanie {InstallerFolder}`,
  'Cli.Help.Usage': `Sposób użycia:`,
  'Cli.Help.Help': `  installerclean-cli --help      Pokaż tę pomoc (akceptuje też /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version   Wypisz wersję (akceptuje też -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s          Tylko skanowanie - niepotrzebne pliki`,
  'Cli.Help.Delete': `  installerclean-cli /d          Usuń niepotrzebne pliki (Kosz)`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m          Przenieś do zapisanej lokalizacji`,
  'Cli.Help.MovePath': `  installerclean-cli /m ŚCIEŻKA  Przenieś do wskazanej ścieżki`,
  'Cli.Help.NoteLine1': `installerclean-cli to prawdziwy proces konsolowy i blokuje wiersz poleceń,`,
  'Cli.Help.NoteLine2': `dopóki się nie zakończy; przekieruj lub przekaż potokiem jego wyjście`,
  'Cli.Help.NoteLine3': `jak każdy inny program konsolowy exe. GUI jest obok, w InstallerClean.exe.`,
  'Cli.Help.ExitCodesHeader': `Kody zakończenia:`,
  'Cli.Help.ExitCodeOk': `  0   sukces: przetworzono każdy oznaczony plik`,
  'Cli.Help.ExitCodeError': `  1   błąd: nic nie przetworzono (złe argumenty, skanowanie lub pliki)`,
  'Cli.Help.ExitCodePartial': `  2   częściowo: część plików przetworzono, część zawiodła`,
  'Cli.Help.ExitCodeTransient': `  75  stan przejściowy: coś zablokowało uruchomienie (zob. komunikat)`,
  'Cli.Help.ExitCodeCancelled': `  130 anulowano (Ctrl+C)`,
  'Completion.CleanedUp': `Wyczyszczono {0}`,
  'Completion.DeleteSpaceHint': `Opróżnij go, aby naprawdę odzyskać miejsce.`,
  'Body.NotScanned.Lead': `Jeszcze nic nie przeskanowano.`,
  'Body.NotScanned.Why': `Naciśnij przycisk Skanuj ponownie, aby przejrzeć {InstallerFolder} w poszukiwaniu plików instalatora, których żaden program już nie potrzebuje.`,
  'Confirm.MoveSameDrive': `Ten folder jest na tym samym dysku, więc samo przeniesienie nie zwolni miejsca. Odzyskasz je, gdy usuniesz z niego pliki, albo możesz zamiast tego wybrać folder na innym dysku.`,
  'Error.ScanCorrelationFailed': `InstallerClean nie zdołał pogodzić tego skanowania z rekordami Windows Installera: każdego pliku, który Windows nadal wymienia jako potrzebny, brakuje w {InstallerFolder}, a pliki faktycznie leżące w tym folderze nie pasują do żadnego rekordu. Żaden prawdziwy komputer tak nie wygląda, więc wskazuje to na problem z odczytem rekordów, a nie na pliki, które można bezpiecznie usunąć. Nie zaproponowano niczego do wyczyszczenia i nic nie zostało usunięte.`,
  'Error.CandidateOutsideCache': `Ten plik nie znajduje się bezpośrednio w folderze Windows Installer; odrzucono ze względów bezpieczeństwa.`,
  'Completion.ReverifySkipped': `Pozostawiono na miejscu {0} {1}, ponieważ po skanowaniu program znów zaczął ich potrzebować.`,
  'Completion.MoveCancelledSummary': `Przed anulowaniem przeniesiono {0}/{1} {2}.`,
  'Completion.DeleteCancelledSummary': `Przed anulowaniem przeniesiono {0}/{1} {2} do Kosza.`,
  'Completion.PermanentDeleteCancelledSummary': `Przed anulowaniem usunięto trwale {0}/{1} {2}.`,
  'Body.PendingReboot.Lead': `Tych plików nie można teraz wyczyścić.`,
  'Cli.TooManyArguments': `Błąd: nieoczekiwany dodatkowy argument „{0}”. Jeśli ścieżka folderu przenoszenia zawiera spację, ujmij całą ścieżkę w cudzysłów: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Zapisana lokalizacja zależy od użytkownika; zadania i SYSTEM: /m ŚCIEŻKA.`,
  'Completion.ReverifyIncomplete': `Pozostawiono na miejscu {0} {1}, ponieważ przy powtórzeniu sprawdzenia nie udało się w pełni odczytać rekordów Windows Installera.`,
  'Summary.ProgramsUnreadable.Singular': `Podczas tego skanowania nie udało się odczytać {0} zainstalowanego programu, więc zastąpione poprawki zostały zachowane. Nie dotyczy to plików osieroconych.`,
  'Summary.ProgramsUnreadable.Plural': `Podczas tego skanowania nie udało się odczytać {0} zainstalowanych programów, więc zastąpione poprawki zostały zachowane. Nie dotyczy to plików osieroconych.`,
  'Error.ScanRecordsUnreadable': `InstallerClean nie zdołał odczytać dość rekordów Windows Installera, by mieć pewność, co jest jeszcze potrzebne: lista zainstalowanych programów wróciła niepełna, a odczyt tych samych rekordów prosto z rejestru również napotkał błędy. Plik mógłby wyglądać na osierocony tylko dlatego, że rekord, który go wymienia, był jednym z nieczytelnych, więc InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer nigdy nie zasygnalizował końca listy zainstalowanych programów: InstallerClean poddał się po {0} wpisach (ostatni kod błędu {1}). Liście bez końca nie można ufać, więc InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer nigdy nie zasygnalizował końca listy poprawek jednego programu: InstallerClean poddał się po {0} wpisach (ostatni kod błędu {1}). Liście bez końca nie można ufać, więc InstallerClean się zatrzymał. Nic nie zostało usunięte.`,
  'Status.CheckingRecycleBin': `Sprawdzanie Kosza...`,
  'UpdateCheck.Status.UpdateAvailable': `Dostępna jest wersja {0}.`,
  'Completion.DonateAsk': `Cieszę się, że pomogło. Jeśli masz dobre serce, jest miejsce na napiwek.`,
  'About.Link.Guide': `Przewodnik i FAQ`,
  'About.Link.ReportProblem': `Zgłoś problem`,
  'About.AutoUpdateCheck': `Automatycznie sprawdzaj aktualizacje`,
  'Automation.About.Guide.HelpText': `Otwiera readme na githubie w twojej przeglądarce.`,
  'Automation.About.ReportProblem.HelpText': `Otwiera listę zgłoszeń (Issues) na github.com w twojej przeglądarce.`,
  'Automation.AutoUpdateCheck.HelpText': `Jeśli zaznaczone, InstallerClean przy uruchomieniu sprawdza na githubie, czy jest nowsza wersja.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the 21 machine-contract Cli.* <data> elements BY NAME (the
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
console.log('machine Cli <data> removed:', cliMachineRemoved, '(expect 21)');
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
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === 21 && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
