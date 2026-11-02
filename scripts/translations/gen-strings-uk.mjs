#!/usr/bin/env node
// Ukrainian (uk) satellite generator. Copied from gen-strings-template.mjs.
// Translates from the English neutral Strings.resx. Run from the repo root:
//   node scripts/translations/gen-strings-uk.mjs
// See the template for how it works. Register: formal "ви" (lowercase),
// Ukrainian software-UI convention. Plurals: East Slavic one/few/many; the
// "few" (2-4) form is NOMINATIVE PLURAL ("2 файли"), never the Russian
// genitive singular ("2 файла"). Overrides live in OVERRIDES, not the resx.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.uk.resx`;

// Universal keeps: keys whose value is the same in every language, the brand names
// and the pure-placeholder announcement string. Their still-English value is NOT a
// miss. Explicit by KEY on purpose: a future brand key then defaults to "flag until
// someone adds it here", never silently passes. Do NOT translate these values. Do
// NOT edit this list per language.
//
// The five size suffixes and the two elapsed suffixes do not belong in this list,
// because they are not universal: French writes To/Go/Mo/Ko/o, Russian and Ukrainian
// write ТБ/ГБ/МБ/КБ/Б and мс/с. Those three carry real
// values in their MAP; the languages that do abbreviate as English does keep them in
// ALSO_KEEP, which is the per-language list. Display.ListSeparator is the same shape,
// for the same reason.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
]);

const ALSO_KEEP = [
  // The list separator Ukrainian uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only Japanese and Chinese differ, taking the ideographic
  // comma.
  'Display.ListSeparator',       // ", "
  // The stop that ends a heading before the body is the one English uses. A
  // punctuation mark rather than a word, so there is nothing to translate and
  // nothing to get wrong; only Japanese and Chinese differ, taking the
  // ideographic full stop, which carries no space after it.
  'Display.SentenceSeparator',   // ". "
  // The colon between a link's words and its address on the command line is the
  // one English uses. A punctuation mark rather than a word, so there is nothing to
  // translate and nothing to get wrong; French puts a narrow no-break space before
  // it, and Japanese and Chinese take the full-width colon.
  'Cli.UpdateDownloadLink',      // "{0}: {1}"
];

// Satellite-only CLDR plural overrides (uk). base Singular = "one" (1, 21, 31),
// base Plural = "many" (5+, 11-14, 0); .Few = the 2-4 NOMINATIVE-PLURAL form.
const OVERRIDES = {
  // As ru: the counted noun sits with {0}, so the pair reaches 1 (файл) and
  // 5+ (файлів) but not the paucal 2-4 (файли).
  'Completion.FailedCount.Few': `Не вдалося перемістити {0} файли.`,
  'Completion.FailedCountDelete.Few': `Не вдалося видалити {0} файли.`,
  // Noun-only pairs: few = nominative plural.
  'Plural.File.Few': `файли`,
  'Plural.Error.Few': `помилки`,
  'Plural.Package.Few': `пакети`,
  'Plural.Product.Few': `продукти`,
  'Plural.Patch.Few': `виправлення`,

  // Sentence-level count keys: the 2-4 form (nominative plural noun/adjective).
  'Summary.RegisteredStillUsed.Few': `{0} файли залишено без змін`,
  'Summary.OrphanedToCleanUp.Few': `{0} непотрібні файли для очищення`,
  'Summary.MissingFromDisk.Few': `Windows має записи про {0} файли, яких немає в {InstallerFolder}: {1}. У щоденній роботі це не заважає, але оновлення чи видалення цих програм може не виконатися. Відкрийте «Деталі», щоб дізнатися, що робити.`,
  // 2-4 takes "встановлені програми"; the base Plural key carries the 5+
  // genitive "встановлених програм".
  'Summary.RegisteredWindow.Few': `{0} файли залишено без змін ({1})`,
  // The counted noun is not in this fragment: it sits in the clause this one is
  // joined to, so the adjective is the only word the numeral governs and it takes
  // its own form at 2-4.
  'Summary.RegisteredWindow.Missing.Few': `{0} відсутні`,

  // Flat key with an inflecting adjective: one / few / (base = many).
  'Summary.MissingFromDisk.Unnamed.Few': `{0} файли, для яких у записах не названо програми`,
  'Summary.MissingFromDisk.OtherPrograms.Few': `ще {0} програми`,
  'Cli.FoundOrphans.One': `Знайдено {0} непотрібний {1} для очищення ({2}).`,
  'Cli.FoundOrphans.Few': `Знайдено {0} непотрібні {1} для очищення ({2}).`,
  'Cli.DeletingFiles.One': `Триває видалення: {0} непотрібний {1}...`,
  'Cli.DeletingFiles.Few': `Триває видалення: {0} непотрібні {1}...`,
  'Cli.DeletedFiles.One': `Остаточно видалено {0} непотрібний {1}.`,
  'Cli.DeletedFiles.Few': `Остаточно видалено {0} непотрібні {1}.`,
  'Cli.MovingFiles.One': `Триває переміщення до {2}: {0} непотрібний {1}...`,
  'Cli.MovingFiles.Few': `Триває переміщення до {2}: {0} непотрібні {1}...`,
  'Cli.MovedFiles.One': `Переміщено {0} непотрібний {1}.`,
  'Cli.MovedFiles.Few': `Переміщено {0} непотрібні {1}.`,
  'Completion.HeldBack.Few': `Затримано {0} файли. Сканування вважало їх непотрібними. Підсумкова перевірка не змогла це підтвердити.`,
  'Cli.MissingFromDisk.Few': `Windows має записи про {0} файли, яких немає в {InstallerFolder}: {1}. У щоденній роботі це не заважає, але оновлення чи видалення цих програм може не виконатися. Щоб повернути файл, вам потрібен інсталятор тієї версії цієї програми, яку ви вже маєте. Візьміть його у виробника програми і запустіть поверх наявної копії. Новіша версія не підійде: їй довелося б спершу видалити ту, що у вас є, а саме цьому крокові й потрібен цей файл. Видалити спершу теж не вийде, з тієї самої причини. Це має відновити файл і залишити ваші налаштування недоторканими, але Microsoft цього не гарантує.`,
  'Cli.SupersededHeldBack.Few': `InstallerClean не зміг упевнено визначити, що {0} заміщені файли більше не потрібні, тож затримав їх.`,
};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Про програму`,
  'Window.Registered.Title': `Файли, залишені без змін`,
  'Window.Orphaned.Title': `Непотрібні файли, які можна безпечно видалити`,

  // Section headings
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `ВИПРАВЛЕННЯ`,
  'Section.Registered.Details': `ДЕТАЛІ ПРОДУКТУ`,
  'Section.Backup.Folder': `ПАПКА РЕЗЕРВНИХ КОПІЙ`,
  'Section.SayThanks': `ПОДЯКУВАТИ`,

  // Field labels (used in detail panels)
  'Field.Reason': `Причина`,
  'Field.Author': `Автор`,
  'Field.Application': `Застосунок`,
  'Field.Title': `Назва`,
  'Field.Subject': `Тема`,
  'Field.Keywords': `Ключові слова`,
  'Field.SigningCertificate': `Сертифікат підпису`,
  'Field.FileSize': `Розмір файлу`,
  'Field.Comment': `Коментар`,
  'Field.ProductName': `Назва продукту`,
  'Field.File': `Файл`,
  'Field.Size': `Розмір`,
  'Field.Patches': `Виправлення`,

  // Placeholder shown for a registered package whose API ProductName is empty.
  'Field.UnknownProductName': `(невідомо)`,
  'Field.PatchesOnly': `(лише виправлення)`,
  'Field.Missing': `відсутній`,

  // Actions (button labels; underscore prefixes are WPF mnemonics)
  'Action.About': `_Про програму`,
  'Action.Copy': `Копіювати`,
  'Action.Cut': `Вирізати`,
  'Action.Paste': `Вставити`,
  'Action.SelectAll': `Виділити все`,
  'Action.Browse': `_Огляд...`,
  'Action.Cancel': `_Скасувати`,
  'Action.CheckForUpdates': `Перевірити о_новлення`,
  'Action.Close': `_Закрити`,
  // Delete permanently marks В, the label's own initial, as en-GB marks D. The
  // natural Н is spoken for by Check for updates (Перевірити о_новлення) on the
  // same results view; the rest of that view holds О (Огляд), М (Перемістити),
  // Т (Повторити) and П (Про програму), and this button's other window, the
  // delete confirmation, carries only Cancel on С.
  'Action.DeletePermanently': `_Видалити назавжди`,
  'Action.Done': `_Готово`,
  'Action.Details': `Деталі`,
  'Action.BuyMeACuppa': `_Підтримати`,
  'Action.Donate': `_Підтримати на 3 \$`,
  'Action.DonateSmall': `Підтримати`,
  'Action.LeaveStarOnGitHub': `Лишити зірку на _GitHub`,
  'Action.Licence': `Ліцензія Apache 2.0`,
  'Action.Move': `Пере_містити`,
  'Action.BackupFolderPlaceholder': `Шлях до папки, якщо ви переміщуєте, а не видаляєте.`,
  'Action.Rescan': `Пов_торити сканування`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa.About': `Підтримати`,
  'Automation.Donate': `Підтримати на 3 \$`,
  'Automation.DonateSmall': `Підтримати`,
  'Automation.CancelOperation': `Скасувати операцію`,
  'Automation.CancelScan': `Скасувати сканування`,
  'Automation.CancelStartupScan': `Скасувати сканування під час запуску`,
  'Automation.Close': `Закрити`,
  'Automation.CloseWindow': `Закрити вікно`,
  'Automation.CloseResult': `Закрити результат і повернутися до головного вікна`,
  'Automation.LeaveStarOnGitHub.About': `Лишити зірку на github`,
  'Automation.Minimise': `Згорнути`,
  'Automation.ConfirmDelete': `«Видалити назавжди» прибирає непотрібні файли. «Скасувати» закриває вікно, нічого не видаляючи.`,
  'Automation.ConfirmMove': `«Перемістити» кладе непотрібні файли до обраної папки призначення. «Скасувати» лишає їх там, де вони є.`,
  'Automation.SayThanks': `Подякувати`,
  'Automation.CheckForUpdates': `Перевірити оновлення`,
  'Automation.CheckForUpdates.HelpText': `Перевіряє на сторінці випусків github, чи є новіша версія.`,
  'Automation.Licence.HelpText': `Відкриває файл ліцензії на github.com у вашому браузері.`,
  'Automation.Section.BackupFolder': `Папка резервних копій`,
  'Automation.Section.Patches': `Виправлення`,
  'Automation.Section.ProductDetails': `Деталі продукту`,
  'Automation.BackupFolder': `Папка резервних копій`,
  'Automation.OperationProgress': `Перебіг операції`,
  'Automation.RescanInstaller': `Просканувати {InstallerFolder} ще раз`,
  'Automation.ScanningProgress': `Перебіг сканування`,
  'Automation.StartupScanProgress': `Перебіг сканування під час запуску`,
  'Automation.ViewOrphanedFiles': `Деталі, непотрібні файли`,
  'Automation.ViewOrphanedFiles.HelpText': `Доступні для очищення.`,
  'Automation.ViewRegisteredFiles': `Деталі, файли, залишені без змін`,
  'Automation.ViewRegisteredFiles.HelpText': `Лише для перегляду.`,
  'Automation.SortStatus.Ascending': `Відсортовано за {0}, за зростанням`,
  'Automation.SortStatus.Descending': `Відсортовано за {0}, за спаданням`,
  'Automation.Scroll.ScanResults': `Результати сканування`,
  'Automation.Scroll.ResultDetails': `Деталі результату`,
  'Automation.Scroll.FileDetails': `Деталі файлу`,
  'Automation.Scroll.DialogBody': `Текст діалогу`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `Файли, які не вдалося обробити`,
  'Automation.RegisteredMissingSeeAlso': `Пояснює цю папку і як відновити файл, у README`,

  // Tooltips
  'Tooltip.BuyMeACuppa.About': `Будь-яка, навіть невелика, підтримка дуже цінна`,
  'Tooltip.CancellingPending': `Скасування запитано. InstallerClean чекає, доки поточний крок дійде до точки зупинки. Це може тривати кілька секунд під час інтенсивного вводу-виводу чи звернення до бази даних MSI.`,
  'Tooltip.Close': `Закрити`,
  'Tooltip.LeaveStarOnGitHub.About': `Зірка допомагає іншим знайти InstallerClean`,
  'Tooltip.Minimise': `Згорнути`,
  'Tooltip.Move': `Переміщує непотрібні файли до папки резервних копій`,
  'Tooltip.MoveNeedsDestination': `Переміщує непотрібні файли до папки резервних копій. Ви оберете її наступним кроком.`,
  'Tooltip.Delete': `Назавжди видаляє непотрібні файли. Скористайтеся «Перемістити», якщо хочете спершу переконатися, що все гаразд.`,
  'Tooltip.SigningCertificate': `Назва суб'єкта з вбудованого сертифіката Authenticode. Ланцюжок не перевірено.`,

  // Body copy
  'Body.MainExplanation.Lead': `Будь-які непотрібні файли нижче можна безпечно видалити.`,
  'Body.MainExplanation.Why': `Вони лежать у {InstallerFolder}. InstallerClean запитує Windows про кожну встановлену програму: файл потрапляє до списку, коли на нього не претендує жодна програма ({0}) або коли його замінило новіше виправлення і жодна програма не змогла б до нього повернутися ({1}).`,
  'Body.MainExplanation.Action': `Перемістіть їх до обраної вами папки резервних копій, а потім видаліть цю папку, коли переконаєтеся, що ваші програми досі оновлюються та видаляються як звичайно. Повернення їх до {InstallerFolder} відновлює все. Або видаліть їх назавжди просто зараз.`,
  'Body.PendingReboot.MsiExecuteMutex': `Зараз щось використовує Windows Installer, наприклад оновлення Windows або програма, що встановлюється у фоні. «Перемістити» і «Видалити» призупинено на цей час, щоб InstallerClean не чіпав {InstallerFolder}, доки вона змінюється. Коли все завершиться, повторіть сканування, і вони повернуться.`,
  'Body.PendingReboot.MsiExecuteMutexAccessRefused': `Windows відмовив InstallerClean у праві перевірити, чи зайнятий Windows Installer, тож «Перемістити» і «Видалити» призупинено, а InstallerClean не чіпатиме {InstallerFolder}. Перезавантажте Windows і повторіть сканування. Якщо Windows і далі відмовляє, це не та машина, яку InstallerClean може очистити.`,
  'Body.PendingReboot.MsiExecuteMutexUnreadable': `InstallerClean не зміг перевірити, чи зайнятий Windows Installer, тож «Перемістити» і «Видалити» призупинено, а InstallerClean не чіпатиме {InstallerFolder}. Перезавантажте Windows і повторіть сканування. Якщо перевірити й далі не вдається, це не та машина, яку InstallerClean може очистити.`,
  'Body.PendingReboot.MsiExecuteMutexUnreadableWindowsError': `InstallerClean не зміг перевірити, чи зайнятий Windows Installer, бо Windows повідомив про помилку: {0} (помилка {1}). «Перемістити» і «Видалити» призупинено, а InstallerClean не чіпатиме {InstallerFolder}. Перезавантажте Windows і повторіть сканування. Якщо перевірити й далі не вдається, це не та машина, яку InstallerClean може очистити.`,
  'Body.PendingReboot.InstallerInProgress': `На цьому комп'ютері призупинено попередню транзакцію Windows Installer. Відновіть або скасуйте те встановлення (чи перезавантажте Windows), перш ніж очищати {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows поставив в чергу на наступне перезавантаження перейменування файлу, що стосується {InstallerFolder}. Перезавантажте Windows, перш ніж очищати.`,
  'Body.NoFileSelected': `Виберіть файл, щоб переглянути деталі.`,
  'Body.NoProductSelected': `Виберіть продукт, щоб переглянути деталі.`,
  'Body.NoMetadata': `Метадані недоступні.`,
  'Body.RegisteredMissingFromDisk': `Цього інсталяційного файлу немає. Зараз це не завдає клопоту і не завдаватиме аж до того дня, коли ви спробуєте оновити або видалити програму, якій він належить. Тоді цей крок може не виконатися, бо Windows шукає цей файл і не знаходить його.\n\nЩоб повернути його, вам потрібен інсталятор тієї версії, яку ви вже маєте. Візьміть його у виробника програми і запустіть поверх наявної копії. Новіша версія не підійде: їй довелося б спершу видалити ту, що у вас є, а саме цьому крокові й потрібен цей файл. Видалити спершу теж не вийде, з тієї самої причини. Це має відновити файл і залишити ваші налаштування недоторканими, але Microsoft цього не гарантує.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `README [пояснює цю папку] і як відновити файл, словами самої Microsoft.`,
  'Body.NoPatches': `(немає)`,

  // Reasons (Reason column in the orphaned-files list)
  'Reason.Orphaned': `Осиротілий`,
  'Reason.Superseded': `Заміщений`,
  'Reason.Obsoleted': `Застарілий`,

  // Status / progress text
  'Status.Scanning': `Сканування...`,
  'Status.Cancelling': `Скасування...`,
  'Status.StartingScan': `Початок сканування...`,
  'Status.QueryingApi': `Запит до Windows про встановлені програми...`,
  'Status.ScanningCache': `Сканування папки кешу інсталятора...`,
  'Status.CheckingRegistry': `Перевірка реєстру на додаткові пакети...`,
  'Status.RegisteredPackagesFound': `Зіставлення файлів зі встановленими програмами...`,
  'Status.CheckingRemaining': `Перевірка решти файлів...`,

  // 0 = elapsed time text (e.g. "1.2s")
  'Status.ScanComplete': `Сканування завершено ({0})`,

  // 0 = file count, 1 = pluralised noun ("file"/"files")
  'Status.FoundUnused': `Знайдено {0} {1} для безпечного видалення.`,
  'Status.PreparingDestination': `Підготовка папки призначення...`,

  // 0 = file count, 1 = pluralised noun
  'Status.Moving': `Переміщення непотрібних файлів...`,
  'Status.Deleting': `Видалення непотрібних файлів...`,
  'Status.MoveCancelled.Partial': `Переміщення скасовано. Опрацьовано {0} з {1} {2}.`,
  'Status.DeleteCancelled.Partial': `Видалення скасовано. Опрацьовано {0} з {1} {2}.`,
  'Status.MoveFailed': `{0}. Деталі у {1}.`,
  'Status.MoveFailed.NoLog': `{0}. Не вдалося записати журнал збоїв.`,
  'Status.DeleteFailed': `{0}. Деталі у {1}.`,
  'Status.DeleteFailed.NoLog': `{0}. Не вдалося записати журнал збоїв.`,
  'Status.ScanAccessDenied': `Відмовлено в доступі. Windows відхилив сканування.`,
  'Status.ScanStopped': `Сканування зупинено.`,
  'Status.ScanCancelled': `Сканування скасовано.`,
  'Status.Ready': `Готово`,
  'Status.ScanFailedDetails': `Збій сканування ({0}). Деталі у {1}.`,
  'Status.ScanFailedDetails.NoLog': `Збій сканування ({0}). Не вдалося записати журнал збоїв.`,

  // Completion screen
  'Completion.AllClean': `Усе чисто`,
  'Completion.NothingToCleanUp': `У {InstallerFolder} немає чого прибирати`,
  'Completion.NothingToCleanUpReceipt': `Проскановано {0} {1} за {2}`,

  // 0 = size freed (e.g. "120.5 MB")
  'Completion.Freed': `Звільнено {0}`,
  'Completion.Moved': `Переміщено {0}`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `Нічого не переміщено`,
  'Completion.NothingDeleted': `Нічого не видалено`,
  'Completion.FailedCount.Singular': `Не вдалося перемістити {0} файл.`,
  'Completion.FailedCount.Plural': `Не вдалося перемістити {0} файлів.`,
  'Completion.FailedCountDelete.Singular': `Не вдалося видалити {0} файл.`,
  'Completion.FailedCountDelete.Plural': `Не вдалося видалити {0} файлів.`,

  // 0 = moved count, 1 = pluralised noun, 2 = destination path
  'Completion.MoveSummary.Singular': `{0} {1} переміщено до: {2}`,
  'Completion.MoveSummary.Plural': `{0} {1} переміщено до: {2}`,

  // 0 = deleted count, 1 = pluralised noun

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `Остаточно видалено {0} {1}`,
  'Completion.PermanentDeleteSummary.Plural': `Остаточно видалено {0} {1}`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} файл залишено без змін`,
  'Summary.RegisteredStillUsed.Plural': `{0} файлів залишено без змін`,
  'Summary.OrphanedToCleanUp.Singular': `{0} непотрібний файл для очищення`,
  'Summary.OrphanedToCleanUp.Plural': `{0} непотрібних файлів для очищення`,
  'Summary.MissingFromDisk.Singular': `Windows має запис про {0} файл, якого немає в {InstallerFolder}: {1}. У щоденній роботі це не заважає, але оновлення чи видалення цієї програми може не виконатися. Відкрийте «Деталі», щоб дізнатися, що робити.`,
  'Summary.MissingFromDisk.Plural': `Windows має записи про {0} файлів, яких немає в {InstallerFolder}: {1}. У щоденній роботі це не заважає, але оновлення чи видалення цих програм може не виконатися. Відкрийте «Деталі», щоб дізнатися, що робити.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `ще {0} програма`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `ще {0} програм`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} файл, для якого в записах не названо програми`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} файлів, для яких у записах не названо програми`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0} з {1} {2}`,

  // Orphaned-window footer: unneeded files split into the three removable causes
  // (genitive plural adjectives read at any count; no trailing noun).
  'Summary.OrphanedWindow': `{0} {1} для очищення ({2})`,

  // Registered-window footer, split singular/plural. 0 = count, 1 = size display.
  'Summary.RegisteredWindow.Singular': `{0} файл залишено без змін ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} файлів залишено без змін ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Перемістити {0} {1} ({2})?`,

  'Confirm.DeleteTitle': `Видалити {0} {1} ({2})?`,

  // Error messages
  'Error.AdminRequiredTitle': `Відмовлено в доступі`,
  'Error.AdminRequiredBody': `Windows відмовив InstallerClean у доступі, тому роботу було зупинено. Нічого не було видалено.\n\nInstallerClean уже працював від імені адміністратора, тож запускати його так ще раз не допоможе. Windows не повідомляє нічого більше про те, що саме відмовило в доступі, тож немає нічого конкретного, що варто спробувати.`,
  'Error.StoppedTitle': `InstallerClean зупинився`,
  'Error.ScanFailedTitle': `Збій сканування`,
  'Error.InstallerDbEmpty': `Записи Windows Installer повернулися цілком порожніми: жодна встановлена програма й жодне оновлення не заявляє прав на кешований файл інсталятора. На робочому комп'ютері такого не буває (навіть у щойно встановленої Windows такі файли є), тож записи або пошкоджено, або їх не вдалося прочитати, і сканування, яке повірило б такій відповіді, помилково визнало б осиротілим кожен файл у {InstallerFolder}. Замість цього InstallerClean зупинився. Нічого не було видалено.`,
  'Error.MsiAccessDenied': `Windows Installer не дозволив InstallerClean перелічити встановлене. InstallerClean уже працював від імені адміністратора, тож запуск від імені адміністратора ще раз нічого не змінить. Без цього списку немає безпечного способу визначити, які кешовані файли ще потрібні, тож InstallerClean зупинився. Нічого не було видалено.`,
  'Error.MsiNonSuccess': `Windows Installer не зміг надати InstallerClean читабельний список встановлених програм: він прочитав {1} {2}, а потім один запис повернувся нечитабельним (код помилки {0}). Замість того щоб працювати зі списком, прочитаним лише частково, InstallerClean зупинився. Нічого не було видалено.`,
  'Error.InvalidDestinationTitle': `Недійсне призначення`,
  'Error.DestinationWriteFailedTitle': `Не вдалося використати цю папку резервних копій`,
  'Error.MoveFailedTitle': `Не вдалося перемістити`,
  'Error.DeleteFailedTitle': `Не вдалося видалити`,
  'Error.SettingNotSavedTitle': `Налаштування не збережено`,
  'Error.SettingNotSavedBody': `Не вдалося зберегти зміну. Під час наступного запуску InstallerClean повернеться до попереднього налаштування.`,
  'Error.DestinationInsideInstaller': `Призначення не може бути всередині папки Windows Installer.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `Призначення {0} вказує всередину системної папки Windows. Виберіть шлях поза %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% і %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Недостатньо місця`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `У {0} не вистачає місця\n\nПотрібно: {1}\nДоступно: {2}`,

  // 0 = destination
  'Error.AccessDeniedDestination': `У вас немає дозволу на запис у {0}.\nСпробуйте папку у вашому профілі користувача або на диску, який вам належить.`,
  'Error.PathTooLong': `Шлях {0} задовгий для Windows. Виберіть коротший шлях.`,
  'Error.DestinationMissing': `Папки {0} не існує, і її не вдалося створити. Перевірте літеру диска або мережевий шлях.`,
  'Error.IOWriteDestination': `Windows не може записати в {0}.\nДеталі у {1}.`,
  'Error.IOWriteDestination.NoLog': `Windows не може записати в {0}. Не вдалося записати журнал збоїв.`,
  'Error.WriteDestination': `Не вдається записати в {0}.\nДеталі у {1}.`,
  'Error.WriteDestination.NoLog': `Не вдається записати в {0}. Не вдалося записати журнал збоїв.`,
  'Error.MissingSourceFile': `Файл більше не існує.`,
  'Error.SourceIsReparsePoint': `Вихідний файл є символьним посиланням або junction; відмовлено з міркувань безпеки.`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows відмовив у доступі до цього файлу; його залишено на місці.`,
  'Error.AccessDenied.Plural': `Windows відмовив у доступі до цих файлів; їх залишено на місці.`,
  'Error.FileInUse.Singular': `Цей файл відкрито або заблоковано іншою програмою, тож зараз його нічим не прибрати. Його залишено на місці; спробуйте пізніше.`,
  'Error.FileInUse.Plural': `Ці файли відкрито або заблоковано іншою програмою, тож зараз їх нічим не прибрати. Їх залишено на місці; спробуйте пізніше.`,
  'Error.IOFailure.Singular': `Windows повідомив про помилку файлу; файл залишено на місці.`,
  'Error.IOFailure.Plural': `Windows повідомив про помилки файлів; ці файли залишено на місці.`,
  'Error.UnknownError.Singular': `З цим файлом щось пішло не так; його залишено на місці.`,
  'Error.UnknownError.Plural': `З цими файлами щось пішло не так; їх залишено на місці.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Відмова перемістити файли до папки Windows Installer (призначення: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `Папка резервних копій має бути повним шляхом до папки, що починається з літери диска або мережевого ресурсу (наприклад, D:\\Backup або \\\\server\\backup). InstallerClean не може використати цей: {0}`,
  'BrowserLaunch.FailedTitle': `Не вдалося відкрити ваш браузер`,
  'UpdateCheck.Title': `Перевірити оновлення`,
  'UpdateCheck.Status.Checking': `Перевірка...`,
  'UpdateCheck.Status.UpToDate': `Актуальна версія.`,

  // 0 = installed version, 1 = latest version on GitHub
  'UpdateCheck.Failed.NetworkUnavailable': `Не вдалося зв'язатися з GitHub. Перевірте інтернет-з'єднання та спробуйте ще раз.`,
  'UpdateCheck.Failed.ServerError': `GitHub повернув повідомлення про помилку. Спробуйте ще раз за кілька хвилин.`,
  'UpdateCheck.Failed.ResponseParseError': `Відповідь GitHub не містила розпізнаного випуску. Спробуйте пізніше або відкрийте сторінку випусків напряму.`,
  'UpdateCheck.Failed.Timeout': `Час перевірки вичерпано. Можливо, ваше з'єднання з GitHub повільне; спробуйте ще раз.`,
  'UpdateCheck.Failed.Unknown': `Перевірка не вдалася з невідомої причини. Деталі у {0}, якщо вам потрібно про це повідомити.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `Посилання в буфері обміну, тож ви можете вставити його самі:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean також не зміг скопіювати посилання в буфер обміну, ось воно:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `InstallerClean більше не зміг підтвердити папку резервних копій і зупинився. Перевірте {0}, потім «Повторити сканування» і спробуйте ще раз.`,
  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Не вдається записати в {0}.`,

  // 0 = file name
  'Error.DestinationCollision': `Файл з іменем «{0}» уже є в папці резервних копій.`,

  // Result log (post-cleanup diagnostic send)

  // Single instance / startup / crash
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `Уже працює.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `Сталася неочікувана помилка, і InstallerClean потрібно закрити.\n\n{0}\n\nДеталі записано до:\n{1}`,
  'Startup.UnhandledBody.NoLog': `Сталася неочікувана помилка, і InstallerClean потрібно закрити.\n\n{0}\n\nНе вдалося записати журнал збоїв.`,
  'Startup.ErrorTitle': `Помилка запуску`,
  'Startup.FailedToStart': `Не вдалося запустити ({0}). Деталі записано до:\n{1}`,
  'Startup.FailedToStart.NoLog': `Не вдалося запустити ({0}). Не вдалося записати журнал збоїв.`,

  // File picker
  'FilePicker.ChooseDestinationTitle': `Виберіть папку призначення для переміщених файлів`,

  // Version display

  // 0 = major.minor.patch (e.g. "1.5.4")
  'Version.Display': `Версія {0}`,
  'Plural.File.Singular': `файл`,
  'Plural.File.Plural': `файлів`,
  'Plural.Error.Singular': `помилка`,
  'Plural.Error.Plural': `помилок`,
  'Plural.Package.Singular': `пакет`,
  'Plural.Package.Plural': `пакетів`,
  'Plural.Product.Singular': `продукт`,
  'Plural.Product.Plural': `продуктів`,
  'Plural.Patch.Singular': `виправлення`,
  'Plural.Patch.Plural': `виправлень`,
  'Display.Size.TB': `{0} ТБ`,
  'Display.Size.GB': `{0} ГБ`,
  'Display.Size.MB': `{0} МБ`,
  'Display.Size.KB': `{0} КБ`,
  'Display.Size.B': `{0} Б`,
  'Display.Elapsed.Ms': `{0:F0} мс`,
  'Display.Elapsed.S': `{0:F1} с`,
  'Display.ListSeparator': `, `,
  'Display.SentenceSeparator': `. `,
  'Display.ElapsedLong.LessThanASecond': `менш ніж секунду`,
  'Display.ElapsedLong.Seconds': `{0:F1} секунди`,
  'CrashLog.PrivacyHeader': `# crash.log збирає необроблені винятки InstallerClean.\n# За підвищених прав повідомлення про винятки платформи можуть\n# містити шляхи до файлів поточного сеансу (зокрема профілі інших\n# користувачів, перелічені запитами Windows Installer). Повідомлення\n# про мережеві збої під час перевірки оновлень або надсилання журналу\n# результатів можуть містити URL призначення та розв'язану IP-адресу\n# чи адресу проксі. Записи про нечитані записи Windows Installer\n# можуть містити SID облікового запису Windows (S-1-5-21-...) і коди\n# продуктів встановленого ПЗ.\n# Приберіть усі три види відомостей, перш ніж додавати цей файл до\n# публічного звіту про помилку.\n`,
  'Tooltip.ChangeLanguage': `Змінити мову. Програму буде перезапущено.`,
  'Automation.ChangeLanguage': `Змінити мову`,
  'Automation.ChangeLanguage.HelpText': `Програму буде перезапущено.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  'Cli.UnknownArgument': `Помилка: невідомий аргумент «{0}»`,
  'Cli.Cancelling': `Скасування...`,
  'Cli.Cancelled': `Скасовано.`,
  'Cli.GenericError': `Помилка: неочікуваний збій ({0}). Подробиці записано до {1}.`,
  'Cli.GenericError.NoLog': `Помилка: неочікуваний збій ({0}). Журнал збою записати не вдалося.`,
  'Cli.ScanningInstaller': `Сканування {InstallerFolder}...`,
  'Cli.FoundOrphans': `Знайдено {0} непотрібних {1} для очищення ({2}).`,
  'Cli.DeletingFiles': `Триває видалення: {0} непотрібних {1}...`,
  'Cli.DeletedFiles': `Остаточно видалено {0} непотрібних {1}.`,
  'Cli.NoMoveDestination': `Помилка: не вказано розташування для переміщення. Скористайтеся /m ШЛЯХ. (Типове значення, задане в графічному інтерфейсі, діє лише для поточного користувача і не застосовується до запусків за розкладом чи від імені службового облікового запису.)`,
  'Cli.MoveDestinationInsideInstaller': `Помилка: призначення не може бути всередині папки Windows Installer.`,
  'Cli.MoveDestinationRelative': `Помилка: призначення має бути повним шляхом. Отримано: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Помилка: призначення {0} вказує всередину системної папки Windows. Виберіть шлях поза %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% і %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Помилка: зараз щось використовує Windows Installer, наприклад оновлення Windows або програма, що встановлюється у фоні. /m і /d заблоковано на цей час. Спробуйте ще раз, коли все завершиться.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Помилка: на цьому комп'ютері призупинено попередню транзакцію Windows Installer. Відновіть або скасуйте те встановлення (чи перезавантажте Windows), перш ніж очищати {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Помилка: поставлена в чергу після перезавантаження операція з файлом стосується {InstallerFolder} ({0}). Перезавантажте Windows, щоб завершити цю операцію, перш ніж очищати.`,
  'Cli.MovingFiles': `Триває переміщення до {2}: {0} непотрібних {1}...`,
  'Cli.MovedFiles': `Переміщено {0} непотрібних {1}.`,
  'Cli.MutexBlocked': `Інший процес InstallerClean утримує блокування єдиного екземпляра (графічний інтерфейс чи інший запуск CLI). Вихід 75 (тимчасовий); можна безпечно повторити пізніше.`,
  'Cli.EventLogUnavailable': `Примітка: не вдалося записати до журналу подій. Перевірте дозволи журналу «Програма» чи групову політику.`,
  'Cli.Help.Header': `InstallerClean - очищення {InstallerFolder}`,
  'Cli.Help.Usage': `Використання:`,
  'Cli.Help.Help': `  installerclean-cli --help     Показати цю довідку (також приймає /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  Вивести версію (також приймає -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Лише сканувати - список непотрібних`,
  'Cli.Help.Delete': `  installerclean-cli /d         Видалити непотрібні файли назавжди`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Перемістити до збереженої папки`,
  'Cli.Help.MovePath': `  installerclean-cli /m ШЛЯХ    Перемістити за вказаним шляхом`,
  'Cli.Help.NoteLine1': `installerclean-cli утримує командний рядок до кінця роботи, щоб&#10;скрипт або запланована задача могли на нього зачекати.`,
  'Cli.Help.ExitCodesHeader': `Коди виходу:`,
  'Cli.Help.ExitCodeOk': `  0   успіх: запуск зробив те, про що просили`,
  'Cli.Help.ExitCodeError': `  1   нічого не оброблено: запуск завершився збоєм або був відхилений`,
  'Cli.Help.ExitCodePartial': `  2   частково: щось оброблено, щось ні (збій або Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  тимчасова: запуск заблокувала тимчасова умова (див. повідомлення)`,
  'Cli.Help.ExitCodeCancelled': `  130 скасовано (Ctrl+C)`,
  'Body.NotScanned.Lead': `Ще нічого не проскановано.`,
  'Body.NotScanned.Why': `Натисніть «Повторити сканування», щоб переглянути {InstallerFolder} і знайти файли інсталятора, яких уже не потребує жодна програма.`,
  'Confirm.MoveSameDrive': `Ця папка на тому самому диску, тож місце не повернеться, доки ви її не видалите. Виберіть натомість папку на іншому диску, якщо хочете отримати місце одразу.`,
  'Error.ScanCorrelationFailed': `InstallerClean не зміг зіставити записи Windows Installer із вмістом {InstallerFolder}. Майже нічого з того, на що вказують записи, там немає, і майже нічого з того, що там є, не названо жодним записом, тож про жоден файл не вдалося показати, що він непотрібний. Нічого не запропоновано і нічого не прибрано.`,
  'Error.CandidateOutsideCache': `Цей файл не міститься безпосередньо в папці Windows Installer; відмовлено з міркувань безпеки.`,
  'Completion.MoveCancelledSummary': `Переміщено {0} з {1} {2} до {3}, перш ніж ви скасували.`,
  'Completion.PermanentDeleteCancelledSummary': `Безповоротно видалено {0} з {1} {2}, перш ніж ви скасували.`,
  'Body.PendingReboot.Lead': `Ці файли зараз не можна прибрати.`,
  'Cli.TooManyArguments': `Помилка: неочікуваний зайвий аргумент «{0}». Якщо в шляху до папки призначення є пробіл, візьміть увесь шлях у лапки: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Папка своя в кожного користувача; запланованим і SYSTEM: /m ШЛЯХ.`,
  'Error.ScanRecordsUnreadable': `InstallerClean не зміг прочитати достатньо записів Windows Installer, щоб напевно знати, що ще потрібно: список встановлених програм повернувся неповним, а читання тих самих записів прямо з реєстру теж призвело до помилок. Файл міг видаватися осиротілим лише тому, що запис, який його називає, виявився одним із нечитабельних, тож InstallerClean зупинився. Нічого не було видалено.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer так і не повідомив про кінець списку встановлених програм: InstallerClean прочитав {2} {3}, а потім припинив спроби після {0} записів (останній код помилки {1}). Списку без кінця довіряти не можна, тож InstallerClean зупинився. Нічого не було видалено.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer так і не повідомив про кінець списку виправлень однієї програми: InstallerClean прочитав {2} {3}, а потім припинив спроби після {0} записів (останній код помилки {1}). Списку без кінця довіряти не можна, тож InstallerClean зупинився. Нічого не було видалено.`,
  'Completion.DonateAsk': `Будь-яка, навіть невелика, підтримка дуже цінна`,
  'About.Link.Guide': `Посібник і поширені запитання`,
  'About.Link.ReportProblem': `Повідомити про проблему`,
  'About.AutoUpdateCheck': `Автоматично перевіряти оновлення`,
  'Automation.About.Guide.HelpText': `Відкриває readme на github у вашому браузері.`,
  'Automation.About.ReportProblem.HelpText': `Відкриває список проблем (Issues) на github.com у вашому браузері.`,
  'Automation.AutoUpdateCheck.HelpText': `Якщо позначено, InstallerClean під час запуску перевіряє на github наявність новішої версії.`,
  'Tooltip.MoveSameDrive': `Переміщує непотрібні файли до папки резервних копій. Вона на тому самому диску, тож місце звільниться лише після того, як ви видалите цю папку.`,
  'Confirm.DeletePermanently.Singular': `Цей файл буде видалено назавжди. Це безпечно, але якщо хочете резервну копію, скористайтеся «Перемістити».`,
  'Confirm.DeletePermanently.Plural': `Ці файли буде видалено назавжди. Це безпечно, але якщо хочете резервну копію, скористайтеся «Перемістити».`,
  'Error.ScanCacheRootUnresolved': `InstallerClean не зміг отримати від Windows справжній шлях до {InstallerFolder}, тож про жоден файл не вдалося показати, що він усередині, і жоден не було запропоновано для очищення. Це сканування нічого не знайшло через невдачу тієї перевірки, а не тому, що папка чиста. Нічого не прибрано.`,
  'Automation.Scroll.ProductDetails': `Відомості про продукт`,
  'Body.PendingReboot.Other': `У Windows Installer щось виконується, тому «Перемістити» і «Видалити» призупинено. InstallerClean не чіпатиме {InstallerFolder}, доки вона змінюється. Коли все завершиться, повторіть сканування, і вони повернуться.`,
  'Cli.TooManyArgumentsNoPath': `Помилка: неочікуваний зайвий аргумент «{0}». /s і /d не приймають інших аргументів, і за один запуск можна використати лише один ключ.`,
  'Cli.MissingFromDisk.Singular': `Windows має запис про {0} файл, якого немає в {InstallerFolder}: {1}. У щоденній роботі це не заважає, але оновлення чи видалення цієї програми може не виконатися. Щоб повернути файл, вам потрібен інсталятор тієї версії, яку ви вже маєте. Візьміть його у виробника програми і запустіть поверх наявної копії. Новіша версія не підійде: їй довелося б спершу видалити ту, що у вас є, а саме цьому крокові й потрібен цей файл. Видалити спершу теж не вийде, з тієї самої причини. Це має відновити файл і залишити ваші налаштування недоторканими, але Microsoft цього не гарантує.`,
  'Cli.MissingFromDisk.Plural': `Windows має записи про {0} файлів, яких немає в {InstallerFolder}: {1}. У щоденній роботі це не заважає, але оновлення чи видалення цих програм може не виконатися. Щоб повернути файл, вам потрібен інсталятор тієї версії цієї програми, яку ви вже маєте. Візьміть його у виробника програми і запустіть поверх наявної копії. Новіша версія не підійде: їй довелося б спершу видалити ту, що у вас є, а саме цьому крокові й потрібен цей файл. Видалити спершу теж не вийде, з тієї самої причини. Це має відновити файл і залишити ваші налаштування недоторканими, але Microsoft цього не гарантує.`,
  'Cli.MoveNotEnoughSpace': `Помилка: недостатньо місця в {0}. Для переміщення цих файлів потрібно {1}, а вільно {2}. Нічого не переміщено.`,
  'Cli.PendingRebootBlocked.Other': `Помилка: у Windows Installer щось виконується, тому /m і /d заблоковано. InstallerClean не чіпатиме {InstallerFolder}, доки вона змінюється. Спробуйте ще раз, коли все завершиться.`,
  'Cli.ScanSignal.Busy': `Коли почалося сканування, щось використовувало Windows Installer, наприклад оновлення Windows або програма, що встановлювалася, тож InstallerClean затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Коли це завершиться, запустіть команду ще раз.`,
  'Cli.ScanSignal.Suspended': `Коли почалося сканування, на цьому комп'ютері було призупинено транзакцію Windows Installer, тож InstallerClean затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Відновіть або скасуйте те встановлення чи перезавантажте Windows, а потім запустіть команду ще раз.`,
  'Cli.ScanSignal.RenameInCache': `Коли почалося сканування, Windows уже поставив у чергу на наступне перезавантаження перейменування файлу, що стосується {InstallerFolder}, тож InstallerClean затримав розташовані там файли, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Перезавантажте Windows, а потім запустіть команду ще раз.`,
  'Cli.ScanSignal.RenameUnresolved': `Коли почалося сканування, у черзі до наступного перезавантаження стояла файлова операція, і InstallerClean не зміг визначити, які файли в ній названо, тож він затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Перезавантажте Windows, а потім запустіть команду ще раз.`,
  'Cli.ScanSignal.AccessRefused': `Коли почалося сканування, Windows відмовив InstallerClean у праві перевірити, чи зайнятий Windows Installer, тож він затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Перезавантажте Windows і запустіть команду ще раз.`,
  'Cli.ScanSignal.RegistryUnreadable': `Коли почалося сканування, InstallerClean не зміг прочитати один із параметрів Windows, які перевіряє, тож не міг визначити, чи виконувалася операція встановлювача, чи чекала на перезавантаження, і затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Перезавантажте Windows і запустіть команду ще раз.`,
  'Cli.ScanSignal.LockUnreadable': `Коли почалося сканування, InstallerClean не зміг перевірити, чи зайнятий Windows Installer, тож він затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Перезавантажте Windows і запустіть команду ще раз.`,
  'Cli.ScanSignal.LockUnreadableWindowsError': `Коли почалося сканування, InstallerClean не зміг перевірити, чи зайнятий Windows Installer, бо Windows повідомив про помилку: {0} (помилка {1}). Тож він затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Перезавантажте Windows і запустіть команду ще раз.`,
  'Cli.ScanSignal.Other': `Коли почалося сканування, у Windows Installer щось виконувалося або чекало, тож InstallerClean затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Коли це владнається, запустіть команду ще раз.`,
  'Cli.ScanSignal.ClockSetBack': `Під час сканування годинник цього комп'ютера пішов назад або відстав, тож InstallerClean затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Запустіть команду ще раз.`,
  'Cli.FoundNoOrphans': `Непотрібних файлів не знайдено.`,
  'Cli.NothingOffered.Singular': `InstallerClean не зміг упевнено визначити, які файли в кеші належать встановленим тут програмам, тож затримав єдиний файл ({2}), замість того щоб запропонувати його.`,
  'Cli.NothingOffered.Plural': `InstallerClean не зміг упевнено визначити, які файли в кеші належать встановленим тут програмам, тож затримав {0} {1} ({2}), замість того щоб запропонувати їх.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean більше не зміг підтвердити папку резервних копій і зупинився. Перевірте {0}, потім запустіть команду ще раз.`,
  'Cli.Help.Summary': `Прибирає .msi і .msp з кешу, не потрібні жодній встановленій програмі.`,
  'Cli.Help.Elevation': `Потрібен командний рядок адміністратора; інакше сканування не буде.`,
  'Error.InstallerLockUnavailableTitle': `Нічого не видалено`,
  'Error.MoveInstallerLockUnavailableTitle': `Нічого не переміщено`,
  'Error.InstallerLockUnavailable': `InstallerClean не зміг узяти блокування, яким Windows Installer не дає двом програмам одночасно змінювати встановлене ПЗ, тож не зміг виключити, що файл знадобиться на півдорозі, і нічого не видалено. Спробуйте ще раз, а якщо повторюється — перезавантажте Windows.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean не зміг узяти блокування, яким Windows Installer не дає двом програмам одночасно змінювати встановлене ПЗ, тож не зміг виключити, що файл знадобиться на півдорозі, і нічого не переміщено. Спробуйте ще раз, а якщо повторюється — перезавантажте Windows.`,
  'Cli.InstallerLockUnavailable': `Помилка: InstallerClean не зміг узяти блокування Windows Installer, яке не дає двом програмам одночасно змінювати встановлене ПЗ, тож не зміг виключити, що файл знадобиться на півдорозі. Нічого не видалено. Спробуйте ще раз, а якщо повторюється — перезавантажте Windows.`,
  'Cli.MoveInstallerLockUnavailable': `Помилка: InstallerClean не зміг узяти блокування Windows Installer, яке не дає двом програмам одночасно змінювати встановлене ПЗ, тож не зміг виключити, що файл знадобиться на півдорозі. Нічого не переміщено. Спробуйте ще раз, а якщо повторюється — перезавантажте Windows.`,
  'Completion.ReverifyIdentityClaimed': `Залишено на місці {0} {1}, бо Windows має запис про програму, названу всередині.`,
  'Completion.ReverifyIdentityUnreadable': `Залишено на місці {0} {1}, бо InstallerClean не знайшов усередині назви програми.`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean не зміг зіставити записи Windows Installer із вмістом {InstallerFolder}. У папці є файли, але жоден запис не вказує ні на що всередині неї, тож про жоден файл не вдалося показати, що він непотрібний. Нічого не запропоновано і нічого не прибрано.`,
  'Error.ScanInstallerFolderElsewhere': `InstallerClean не може просканувати {InstallerFolder} на цьому ПК. Windows повідомляє, що ця папка розташована в {0}, а InstallerClean сканує папку Installer, лише якщо Windows вказує для неї її власний шлях. Нічого не запропоновано і нічого не прибрано.`,
  'Completion.NothingOffered': `На цьому ПК нічого не запропоновано`,
  'Cli.SupersededHeldBack.Singular': `InstallerClean не зміг упевнено визначити, що єдиний заміщений файл більше не потрібен, тож затримав його.`,
  'Cli.SupersededHeldBack.Plural': `InstallerClean не зміг упевнено визначити, що {0} заміщених файлів більше не потрібні, тож затримав їх.`,
  'Completion.HeldBack.Singular': `Затримано {0} файл. Сканування вважало його непотрібним. Підсумкова перевірка не змогла це підтвердити.`,
  'Completion.HeldBack.Plural': `Затримано {0} файлів. Сканування вважало їх непотрібними. Підсумкова перевірка не змогла це підтвердити.`,
  'Body.PendingReboot.PendingRenameUnresolved': `Файлову операцію поставлено в чергу до наступного перезавантаження, і InstallerClean не може визначити, які файли в ній названо, тож не може виключити, що вони в {InstallerFolder}. Перезавантажте Windows перед очищенням.`,
  'Completion.MoveRestoreHint': `Видаліть цю папку, коли переконаєтеся, що все гаразд.`,
  'Completion.MoveRestoreHintSameDrive': `Видаліть цю папку, коли переконаєтеся, що все гаразд. Лише після цього місце справді звільниться.`,
  'Confirm.MoveDestination.Singular': `Цей файл буде переміщено до:`,
  'Confirm.MoveDestination.Plural': `Ці файли буде переміщено до:`,
  'Cli.NothingListed.Singular': `InstallerClean не зміг упевнено визначити, які файли в кеші належать встановленим тут програмам, тож затримав єдиний файл ({2}), замість того щоб запропонувати його.`,
  'Cli.NothingListed.Plural': `InstallerClean не зміг упевнено визначити, які файли в кеші належать встановленим тут програмам, тож затримав {0} {1} ({2}), замість того щоб запропонувати їх.`,
  'Cli.WithheldReasons.Header': `Чому впевненості не було:`,
  'Cli.WithheldReasons.RecordedPath': `  Шлях до файлу з власних записів Windows Installer не вдалося розв'язати, тож із ним нічого не вдалося зіставити.`,
  'Cli.WithheldReasons.FileIdentity': `  Файл, про який Windows має запис, не вдалося розпізнати, тож його не вдалося зіставити з тим, що є в теці.`,
  'Cli.WithheldReasons.SecondInstance': `  Програму може бути встановлено на цьому ПК більше одного разу, а записи не можуть сказати, якій копії належить файл.`,
  'Cli.WithheldReasons.RegistryRecordsUnread': `  Частину записів Windows Installer не вдалося прочитати, тож файл може належати програмі, записаній там.`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `Помилка: файлову операцію поставлено в чергу до наступного перезавантаження, і InstallerClean не може визначити, які файли в ній названо, тож не може виключити {InstallerFolder}. Перезавантажте Windows перед очищенням.`,
  'Cli.MoveRestoreHint': `Переконайтеся, що ваші програми досі оновлюються та видаляються як звичайно, а потім видаліть {0}.`,
  'Error.ScanStoppedDetails': `Це також записується до {0}.`,
  'Cli.NothingListedPerFile.Singular': `InstallerClean не зміг встановити, що один файл не потрібен, тож затримав його ({2}), замість того щоб запропонувати.`,
  'Cli.NothingListedPerFile.Plural': `InstallerClean затримав {0} {1} ({2}), замість того щоб запропонувати їх, бо не зміг встановити, що вони не потрібні.`,
  'Cli.NothingOfferedPerFile.Singular': `InstallerClean не зміг встановити, що один файл не потрібен, тож затримав його ({2}), замість того щоб запропонувати.`,
  'Cli.NothingOfferedPerFile.Plural': `InstallerClean затримав {0} {1} ({2}), замість того щоб запропонувати їх, бо не зміг встановити, що вони не потрібні.`,
  'Cli.WithheldReasons.CandidateIdentity': `  Файл у теці не вдалося розпізнати, тож його не вдалося зіставити із записами.`,
  'Cli.WithheldReasons.DeclaredProductUnestablished': `  Або файл не вказав, якій програмі він належить, або Windows не дав чіткої відповіді про цю програму.`,
  'Cli.WithheldReasons.ScreenUnanswered': `  Перевірка того, яким програмам належать файли, дала відповіді, які не збіглися з переданими їй файлами.`,
  'Cli.WithheldReasons.DeclaredPatchUnestablished': `  Або файл виправлення не вказав, яке це виправлення, або не вказав, для яких програм він призначений, або Windows не вказав повністю, які програми мають це виправлення.`,
  'Body.PendingReboot.RegistryCheckUnreadable': `InstallerClean не зміг прочитати один із параметрів Windows, які перевіряє перед тим, як торкатися {InstallerFolder}, тож не може визначити, чи операція встановлювача виконується, чи чекає на перезавантаження. Перезавантажте Windows і повторіть сканування. Якщо параметр і далі не читається, це не та машина, яку InstallerClean може очистити.`,
  'Cli.InstallerLockAccessRefused': `Помилка: Windows відмовив InstallerClean у праві перевірити, чи зайнятий Windows Installer, тож він не зміг виключити, що файл знадобиться в процесі. Нічого не було видалено.`,
  'Cli.MoveCancelledRestoreHint': `Це легко скасувати. Перемістіть їх з {0} назад до {InstallerFolder}, і все повернеться як було.`,
  'Cli.MoveInstallerLockAccessRefused': `Помилка: Windows відмовив InstallerClean у праві перевірити, чи зайнятий Windows Installer, тож він не зміг виключити, що файл знадобиться в процесі. Нічого не було переміщено.`,
  'Cli.PendingRebootBlocked.RegistryCheckUnreadable': `Помилка: InstallerClean не зміг прочитати одне зі значень реєстру, які перевіряє перед тим, як торкатися {InstallerFolder}, тож не може виключити операцію Windows Installer, що виконується зараз або поставлена в чергу до наступного перезавантаження. /m і /d заблоковано. Перезавантажте Windows і спробуйте ще раз. Якщо читання й далі не вдається, це не та машина, яку InstallerClean може очистити.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutexUnreadable': `Помилка: InstallerClean не зміг перевірити, чи зайнятий Windows Installer, тож не може виключити встановлення, що триває. /m і /d заблоковано. Перезавантажте Windows і спробуйте ще раз. Якщо перевірка й далі не вдається, це не та машина, яку InstallerClean може очистити.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutexUnreadableWindowsError': `Помилка: InstallerClean не зміг перевірити, чи зайнятий Windows Installer, бо Windows повідомив про помилку: {0} (помилка {1}), тож не може виключити встановлення, що триває. /m і /d заблоковано. Перезавантажте Windows і спробуйте ще раз. Якщо перевірка й далі не вдається, це не та машина, яку InstallerClean може очистити.`,
  'Completion.MoveCancelledRestoreHint': `Це легко скасувати. Перемістіть їх назад до {InstallerFolder}, і все повернеться як було.`,
  'Error.InstallerLockAccessRefused': `Windows відмовив InstallerClean у праві перевірити, чи зайнятий Windows Installer, тож він не зміг виключити, що файл знадобиться в процесі, і нічого не було видалено.`,
  'Error.MoveInstallerLockAccessRefused': `Windows відмовив InstallerClean у праві перевірити, чи зайнятий Windows Installer, тож він не зміг виключити, що файл знадобиться в процесі, і нічого не було переміщено.`,
  'Error.MoveStoppedTitle': `Переміщення зупинено`,
  'Field.NoNamedProduct': `(немає програми)`,
  'Summary.RegisteredWindow.Missing.Plural': `{0} відсутніх`,
  'Summary.RegisteredWindow.Missing.Singular': `{0} відсутній`,
  'UpdateCheck.Failed.Unknown.NoLog': `Перевірка не вдалася з невідомої причини. Не вдалося записати журнал збоїв.`,
  'Startup.AdminRightsNeededTitle': `Потрібні права адміністратора`,
  'Startup.AdminRightsNeededBody': `InstallerClean потрібні права адміністратора, щоб бачити всі програми, встановлені на цьому ПК, а його запущено не від імені адміністратора, тож сканування не буде. Нічого не змінено.\n\nЗапустіть InstallerClean знову від імені адміністратора.`,
  'Cli.AdminRightsNeeded': `InstallerClean потрібні права адміністратора, щоб бачити всі програми, встановлені на цьому ПК, а його запущено не від імені адміністратора, тож сканування не буде. Нічого не змінено. Запустіть його знову з командного рядка адміністратора.`,
  'Error.ScanInstallerFolderNotFound': `Windows повідомляє, що {InstallerFolder} на цьому ПК не існує, тому InstallerClean зупинився. Нічого не запропоновано і нічого не прибрано.`,
  'Error.ScanInstallerFolderListFailed': `Windows повідомив про помилку, коли InstallerClean намагався отримати список файлів у {InstallerFolder}, тому InstallerClean не зміг отримати повний список і зупинився. Нічого не запропоновано і нічого не прибрано.`,
  'Body.RescanNotFinished.Lead': `До наступного сканування показувати нічого.`,
  'Body.RescanNotFinished.Why': `Сканування, яке виконується після «Перемістити» або «Видалити», не завершилося, тож немає списку, який можна показати. Натисніть «Повторити сканування», щоб знову просканувати {InstallerFolder}.`,
  'Body.RescanNotFinished.Recorded': `Причину зупинки записано до {0}.`,
  'Status.MatchingCount': `{0} з {1}`,
  'Status.WaitingForDrive': `Очікування відповіді від диска {0}...`,
  'Status.WaitingForPath': `Очікування відповіді від {0}...`,
  'Action.StopWaiting': `Продовжити _без очікування`,
  'Automation.StopWaitingForDrive': `Продовжити без очікування, диск {0}`,
  'Automation.StopWaitingForPath': `Продовжити без очікування, {0}`,
  'Status.StopWaitingLeavesAlone': `Будь-який файл, який ще треба звірити з тим, що там є, буде залишено без змін.`,
  'Automation.WaitingLineWithStopKey': `{0} Натисніть Alt+{1}, щоб продовжити без очікування. {2}`,
  'Summary.SourceGivenUp.Drive': `InstallerClean продовжив роботу без диска {0} і залишив без змін усі файли, які ще треба було звірити з ним. Коли він знову відповідатиме нормально, натисніть «Повторити сканування».`,
  'Summary.SourceGivenUp.Path': `InstallerClean продовжив роботу без {0} і залишив без змін усі файли, які ще треба було звірити з тим, що там є. Коли доступ знову стане нормальним, натисніть «Повторити сканування».`,
  'Summary.SourcesGivenUp': `InstallerClean продовжив роботу без кількох дисків або мережевих ресурсів ({0}) і залишив без змін усі файли, які ще треба було звірити з одним із них. Коли вони знову відповідатимуть нормально, натисніть «Повторити сканування».`,
  'Summary.ScanSignal.Busy': `Коли почалося сканування, щось використовувало Windows Installer, наприклад оновлення Windows або програма, що встановлювалася, тож InstallerClean затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Коли це завершиться, натисніть «Повторити сканування».`,
  'Summary.ScanSignal.Suspended': `Коли почалося сканування, на цьому комп'ютері було призупинено транзакцію Windows Installer, тож InstallerClean затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Відновіть або скасуйте те встановлення чи перезавантажте Windows, а потім натисніть «Повторити сканування».`,
  'Summary.ScanSignal.RenameInCache': `Коли почалося сканування, Windows уже поставив у чергу на наступне перезавантаження перейменування файлу, що стосується {InstallerFolder}, тож InstallerClean затримав розташовані там файли, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Перезавантажте Windows, а потім натисніть «Повторити сканування».`,
  'Summary.ScanSignal.RenameUnresolved': `Коли почалося сканування, у черзі до наступного перезавантаження стояла файлова операція, і InstallerClean не зміг визначити, які файли в ній названо, тож він затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Перезавантажте Windows, а потім натисніть «Повторити сканування».`,
  'Summary.ScanSignal.AccessRefused': `Коли почалося сканування, Windows відмовив InstallerClean у праві перевірити, чи зайнятий Windows Installer, тож він затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Перезавантажте Windows і натисніть «Повторити сканування».`,
  'Summary.ScanSignal.RegistryUnreadable': `Коли почалося сканування, InstallerClean не зміг прочитати один із параметрів Windows, які перевіряє, тож не міг визначити, чи виконувалася операція встановлювача, чи чекала на перезавантаження, і затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Перезавантажте Windows і натисніть «Повторити сканування».`,
  'Summary.ScanSignal.LockUnreadable': `Коли почалося сканування, InstallerClean не зміг перевірити, чи зайнятий Windows Installer, тож він затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Перезавантажте Windows і натисніть «Повторити сканування».`,
  'Summary.ScanSignal.LockUnreadableWindowsError': `Коли почалося сканування, InstallerClean не зміг перевірити, чи зайнятий Windows Installer, бо Windows повідомив про помилку: {0} (помилка {1}). Тож він затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Перезавантажте Windows і натисніть «Повторити сканування».`,
  'Summary.ScanSignal.Other': `Коли почалося сканування, у Windows Installer щось виконувалося або чекало, тож InstallerClean затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Коли це владнається, натисніть «Повторити сканування».`,
  'Summary.ScanSignal.ClockSetBack': `Під час сканування годинник цього комп'ютера пішов назад або відстав, тож InstallerClean затримав файли в {InstallerFolder}, які не названо в записах жодної встановленої програми, замість того щоб запропонувати їх. Натисніть «Повторити сканування».`,
  'Display.DriveName': `диск {0}`,
  'Cli.SourceGivenUp.Drive': `InstallerClean перестав чекати на диск {0} і залишив без змін усі файли, які ще треба було звірити з ним. Коли він почне відповідати нормально, запустіть команду ще раз.`,
  'Cli.SourceGivenUp.Path': `InstallerClean перестав чекати на {0} і залишив без змін усі файли, які ще треба було звірити з тим, що там є. Коли доступ стане нормальним, запустіть команду ще раз.`,
  'Cli.SourcesGivenUp': `InstallerClean перестав чекати на кілька дисків або мережевих ресурсів ({0}) і залишив без змін усі файли, які ще треба було звірити з одним із них. Коли вони почнуть відповідати нормально, запустіть команду ще раз.`,
  'Action.CloseWithoutDonating': `_Закрити без підтримки`,
  'Tooltip.Donate': `Насправді за InstallerClean варто було б брати гроші: на нього пішло дуже багато праці. Але я хочу, щоб він залишався безкоштовним і допомагав якомога більшій кількості людей. Ваша підтримка надає всьому цьому сенсу, і я дуже за неї вдячний.`,
  'Cli.DonateAsk': `InstallerClean безкоштовний. Якщо він допоміг, будь ласка, підтримайте його на 3 \$: {0}`,
  'Completion.SendReport': `Надіслати _анонімний звіт`,
  'Completion.ReportPanel.Intro': `Звіт додає ваш результат до [цього графіка] на GitHub, який показує, скільки місця люди звільняють за допомогою InstallerClean. У ньому також є ці технічні відомості, які допомагають мені знаходити помилки:`,
  'Completion.ReportPanel.Freed': `Скільки місця звільнено і скільки файлів переміщено або видалено`,
  'Completion.ReportPanel.Destination': `Чи було місцем призначення «Перемістити» той самий диск, інший диск, знімний диск чи мережевий ресурс`,
  'Completion.ReportPanel.Durations': `Скільки тривали сканування і «Перемістити» або «Видалити»`,
  'Completion.ReportPanel.InstallerFiles': `Скільки файлів інсталятора кожного виду і скільки місця вони займають`,
  'Completion.ReportPanel.LeftAlone': `Скільки файлів залишено без змін і чому`,
  'Completion.ReportPanel.Waits': `Як часто InstallerClean чекав на повільний диск або мережевий ресурс і на скільки з них перестав чекати`,
  'Completion.ReportPanel.Records': `Про скільки програм і виправлень Windows має запис`,
  'Completion.ReportPanel.ShortNames': `Чи досі Windows створює короткі імена файлів старого зразка`,
  'Completion.ReportPanel.Windows': `Яка версія Windows, 10 чи 11, x64 чи Arm, і які в ній встановлено мову та країну або регіон`,
  'Completion.ReportPanel.AppVersion': `Версія InstallerClean, спосіб її отримання (setup, portable, slim, Scoop, окрема програма командного рядка, збірка з вихідного коду або невідомо), звідки надіслано звіт (з InstallerClean чи з програми командного рядка) і мова, якою він відображався`,
  'Completion.ReportPanel.Errors': `Лічильники всього, що пішло не так`,
  'Completion.ReportPanel.Closing': `Кожен із цих пунктів — це число або мітка. Ніщо у звіті не вказує на вас, ваш ПК, ваші файли чи програми, і я ніколи не бачу вашу IP-адресу.`,
  'Automation.ReportInfo': `Що містить звіт`,
  'Automation.Scroll.ReportContents': `Що містить звіт`,
  'Completion.ReportPanel.SeeReport': `Переглянути, що саме надсилається`,
  'Window.Report.Title': `Анонімний звіт`,
  'Window.Report.Unreadable': `InstallerClean не зміг прочитати звіт.`,
  'Automation.Scroll.Waiting': `Очікування диска або мережевого ресурсу`,
  'Completion.RecentChange.Singular': `Затримано один файл ({2}), бо, за даними Windows, його було змінено після того, як InstallerClean почав сканування. Якщо в цей час встановлювалася чи оновлювалася програма, повторіть сканування, коли вона завершить, і InstallerClean перевірить його знову.`,
  'Completion.RecentChange.Plural': `InstallerClean затримав {0} {1} ({2}), бо, за даними Windows, їх було змінено після того, як InstallerClean почав сканування. Якщо в цей час встановлювалася чи оновлювалася програма, повторіть сканування, коли вона завершить, і InstallerClean перевірить їх знову.`,
  'UpdateNotice.Available': `Доступна версія InstallerClean {0}.`,
  'UpdateNotice.Download': `Завантажити з GitHub`,
  'Automation.UpdateNotice.Download': `Завантажити з github`,
  'UpdateNotice.Recommended': `Настійно рекомендуємо використовувати найновішу версію.`,
  'UpdateNotice.UpdateNow': `Оновити зараз`,
  'UpdateNotice.Downloading': `Завантаження InstallerClean {0}...`,
  'UpdateNotice.DownloadFailed': `Не вдалося завантажити оновлення.`,
  'UpdateNotice.InstallFailed': `Не вдалося встановити оновлення.`,
  'UpdateNotice.ContinueWithoutUpdating': `_Продовжити без оновлення`,
  'Cli.UpdatePrompt': `Оновити зараз? Настійно рекомендуємо використовувати найновішу версію. [{0}/{1}]`,
  'Cli.UpdateAnswerYes': `Т`,
  'Cli.UpdateAnswerNo': `Н`,
  'Cli.UpdateListsOnlyAfterDelete': `Нова версія просканує й покаже список знайденого, нічого не видаляючи. Щоб видалити, потім знову запустіть /d.`,
  'Cli.UpdateListsOnlyAfterMove': `Нова версія просканує й покаже список знайденого, нічого не переміщуючи. Щоб перемістити, потім знову запустіть /m.`,
  'Cli.UpdateRunningInstead': `Натомість команду {0} буде виконано в InstallerClean {1}.`,
  'Cli.UpdateDownloadLink': `{0}: {1}`,
  'Cli.Help.UpdateNote': `Перед скануванням перевіряє, чи є новіша версія InstallerClean. Під час&#10;запуску вручну запитує перед оновленням і оновлюється, якщо протягом&#10;хвилини ніхто не відповів. Під час запуску без участі людини, у&#10;запланованій задачі чи скрипті, оновлюється без запитань.&#10;Додайте /noupdate, щоб пропустити перевірку.`,
  'UpdateNotice.Scoop': `Цю копію встановлено через Scoop. Щоб оновити її, закрийте InstallerClean і виконайте:`,
  'UpdateNotice.ScoopGlobal': `Цю копію встановлено через Scoop для всіх користувачів. Щоб оновити її, закрийте InstallerClean, відкрийте термінал від імені адміністратора і виконайте:`,
  'UpdateNotice.CommandCopied': `Скопійовано`,
  'UpdateNotice.CopyFailed': `Не вдалося скопіювати. Виділіть команду й скопіюйте її вручну.`,
  'Automation.UpdateNotice.CopyCommand': `Копіювати команду`,
  'Automation.UpdateNotice.ScoopCommand': `Команда оновлення Scoop`,
  'Cli.UpdateInUse': `InstallerClean використовується, тому зараз не може оновитися.`,
  'Error.FolderWillNotGiveFilesBack': `Ви можете зберігати файли в {0}, але не можете знову їх відкрити, тому InstallerClean не переміщуватиме туди файли.`,
  'UpdateNotice.NeedsNetFramework48': `Для цієї версії потрібен .NET Framework 4.8. Встановіть його з сайту Microsoft, перезавантажте комп'ютер, а потім відкрийте InstallerClean, щоб виконати оновлення.`,
  'UpdateNotice.CannotInstallAutomatically': `InstallerClean {0} не можна встановити автоматично.`,
  'UpdateNotice.CopyCannotTell': `InstallerClean не може визначити, чи встановила його власна програма встановлення, тому не може оновитися.`,
  'UpdateNotice.DownloadNetFramework48': `Завантажити .NET Framework 4.8 з сайту Microsoft`,
  'Automation.UpdateNotice.DownloadNetFramework48': `Завантажити .NET Framework 4.8 з сайту Microsoft`,
  'Cli.UpdateNeedsNetFramework48': `Для цієї версії потрібен .NET Framework 4.8. Встановіть його з сайту Microsoft, перезавантажте комп'ютер, а потім знову запустіть InstallerClean, щоб виконати оновлення.`,
  'Cli.ReportPrompt': `Надіслати анонімний звіт про цей запуск? Його збережено в {0}. Введіть ?, щоб побачити, що в ньому. [{1}/{2}/?]`,
  'Cli.ReportChartAt': `графіка за адресою {0}`,
  'Cli.ReportIntro': `Звіт додає ваш результат до {0}, який показує, скільки місця люди звільняють за допомогою InstallerClean. У ньому також є ці технічні відомості, які допомагають мені знаходити помилки:`,
  'Cli.ReportInFull': `Повний звіт виведено нижче.`,
  'Cli.ReportSent': `Звіт надіслано. Дякую.`,
  'Cli.ReportNotSent': `Звіт не надіслано.`,
  'Cli.ReportCouldNotBeSent': `Не вдалося надіслати звіт. Його збережено в {0}.`,
  'Cli.ReportWaiting': `Надсилається звіт, що чекає на надсилання ще з раннішого запуску.`,
  'Cli.UpdateRunningNewer': `Команду {0} буде виконано в InstallerClean {1}. Після завершення ця версія замінить поточну копію.`,
  'Cli.UpdateNotRun': `Не вдалося запустити новішу версію.`,
  'UpdateNotice.CheckingDownload': `Перевірка завантаженого файлу...`,
  'Cli.MutexLostDelete': `Зупинено після видалення {0} з {1} {2}: InstallerClean більше не міг бути певним, що жоден інший InstallerClean не виконує дій. Запустіть ту саму команду ще раз. Вихід 2 (частково).`,
  'Cli.MutexLostMove': `Зупинено після переміщення {0} з {1} {2} до {3}: InstallerClean більше не міг бути певним, що жоден інший InstallerClean не виконує дій. Запустіть ту саму команду ще раз. Вихід 2 (частково).`,
  'Cli.CoverLostNoWorkDelete': `Зупинено до видалення будь-якого файлу: InstallerClean більше не міг бути певним, що жоден інший InstallerClean не виконує дій. Запустіть ту саму команду ще раз. Вихід 75 (тимчасовий).`,
  'Cli.CoverLostNoWorkMove': `Зупинено до переміщення будь-якого файлу: InstallerClean більше не міг бути певним, що жоден інший InstallerClean не виконує дій. Запустіть ту саму команду ще раз. Вихід 75 (тимчасовий).`,
  'Cli.UpdateNoAnswer': `Відповіді немає вже хвилину, тому InstallerClean оновлюється без запитань, як під час запуску, коли відповідати нікому.`,
  'Cli.ReportNoAnswer': `Відповіді немає вже хвилину.`,
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

const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
let cliMachineRemoved = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (isMachineCliKey(name)) { cliMachineRemoved++; return ''; } return m; });

const escRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const notApplied = [];
for (const [key, val] of Object.entries(MAP)) {
  const re = new RegExp('(<data name="' + escRe(key) + '"[^>]*>\\s*<value>)([\\s\\S]*?)(</value>)');
  let applied = false;
  text = text.replace(re, (m, p1, p2, p3) => { applied = true; return p1 + val + p3; });
  if (!applied) notApplied.push(key);
}

const overrideBlock = Object.entries(OVERRIDES)
  .map(([k, v]) => `  <data name="${k}" xml:space="preserve"><value>${v}</value></data>`)
  .join('\n');
if (overrideBlock) text = text.replace('</root>', overrideBlock + '\n</root>');

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
