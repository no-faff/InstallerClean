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

const ALSO_KEEP = [
  // The list separator Ukrainian uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
];

// Satellite-only CLDR plural overrides (uk). base Singular = "one" (1, 21, 31),
// base Plural = "many" (5+, 11-14, 0); .Few = the 2-4 NOMINATIVE-PLURAL form.
// Status.RegisteredPackagesFound is a flat key: its base value is the "many"
// form, with .One and .Few added (the adjective inflects one/few/many in uk).
const OVERRIDES = {
  // As ru: the counted noun sits with {0}, so the pair reaches 1 (файл) and
  // 5+ (файлів) but not the paucal 2-4 (файли).
  'Completion.FailedCount.Few': `Не вдалося перемістити {0} файли з {1}.`,
  'Completion.FailedCountDelete.Few': `Не вдалося видалити {0} файли з {1}.`,
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

  // Flat key with an inflecting adjective: one / few / (base = many).
  'Status.RegisteredPackagesFound.One': `Знайдено {0} зареєстрований {1}.`,
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
  'Status.RegisteredPackagesFound.Few': `Знайдено {0} зареєстровані {1}.`,
  'Completion.HeldBack.Few': `Затримано {0} файли. Сканування вважало їх непотрібними. Підсумкова перевірка не змогла це підтвердити.`,
  'Cli.MissingFromDisk.Few': `Windows має записи про {0} файли, яких немає в {InstallerFolder}: {1}. У щоденній роботі це не заважає, але оновлення чи видалення цих програм може не виконатися. Щоб повернути файл, вам потрібен інсталятор тієї версії цієї програми, яку ви вже маєте. Візьміть його у виробника програми і запустіть поверх наявної копії. Новіша версія не підійде: їй довелося б спершу видалити ту, що у вас є, а саме цьому крокові й потрібен цей файл. Видалити спершу теж не вийде, з тієї самої причини. Це має відновити файл і залишити ваші налаштування недоторканими, але Microsoft цього не гарантує.`,
  'Summary.SupersededHeldBack.Few': `InstallerClean не зміг упевнено визначити, що {0} заміщені файли більше не потрібні, тож затримав їх.`,
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
  'Action.BuyMeACuppa': `Пригостіть мене _кавою`,
  'Action.LeaveStarOnGitHub': `Лишити зірку на _GitHub`,
  'Action.Licence': `Ліцензія Apache 2.0`,
  'Action.Move': `Пере_містити`,
  'Action.BackupFolderPlaceholder': `Шлях до папки, якщо ви переміщуєте, а не видаляєте.`,
  'Action.OpenReleasePage': `_Відкрити сторінку випуску`,
  'Action.Rescan': `Пов_торити сканування`,
  'Action.ScanAgain': `_Сканувати знову`,
  'Action.SendResultLog': `Надіслати звіт`,
  'Action.SendResultLogConfirm': `_Надіслати`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa': `Підтримати`,
  'Automation.BuyMeACuppa.About': `Пригостіть мене кавою`,
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
  'Automation.ConfirmSendResultLog': `«Надіслати» надсилає показаний звіт до No Faff. «Скасувати» не надсилає нічого.`,
  'Automation.CheckForUpdates': `Перевірити оновлення`,
  'Automation.CheckForUpdates.HelpText': `Перевіряє на сторінці випусків github, чи є новіша версія.`,
  'Automation.UpdateAvailable.HelpText': `Відкрийте сторінку випуску, щоб завантажити новішу версію, або скасуйте, щоб лишити поточну версію.`,
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
  'Tooltip.BuyMeACuppa.About': `Робота не з легких, аж у горлі пересохло!`,
  'Tooltip.CancellingPending': `Скасування запитано. InstallerClean чекає, доки поточний крок дійде до точки зупинки. Це може тривати кілька секунд під час інтенсивного вводу-виводу чи звернення до бази даних MSI.`,
  'Tooltip.Close': `Закрити`,
  'Tooltip.LeaveStarOnGitHub.About': `Зірка допомагає іншим знайти InstallerClean.`,
  'Tooltip.Minimise': `Згорнути`,
  'Tooltip.SendResultLog': `На ваш розсуд, але буду вдячний. Надсилає анонімний підсумок, який лише дає мені знати, чи працює програма і скільки місця люди звільняють. На наступному екрані ви побачите, що буде надіслано, перш ніж підтвердити.`,
  'Tooltip.SendResultLog.NothingFound': `На ваш розсуд, але буду вдячний. Надсилає анонімний підсумок, який лише дає мені знати, чи працює програма. На наступному екрані ви побачите, що буде надіслано, перш ніж підтвердити.`,
  'Tooltip.Move': `Переміщує непотрібні файли до папки резервних копій.`,
  'Tooltip.MoveNeedsDestination': `Переміщує непотрібні файли до папки резервних копій. Ви оберете її наступним кроком.`,
  'Tooltip.Delete': `Назавжди видаляє непотрібні файли. Скористайтеся «Перемістити», якщо хочете спершу переконатися, що все гаразд.`,
  'Tooltip.SigningCertificate': `Назва суб'єкта з вбудованого сертифіката Authenticode. Ланцюжок не перевірено.`,

  // Body copy
  'Body.MainExplanation.Lead': `Будь-які непотрібні файли нижче [можна безпечно видалити].`,
  'Body.MainExplanation.Why': `Вони лежать у {InstallerFolder}. InstallerClean запитує Windows про кожну встановлену програму: файл потрапляє до списку, коли на нього не претендує жодна програма ({0}) або коли його замінило новіше виправлення і жодна програма не змогла б до нього повернутися ({1}).`,
  'Body.MainExplanation.Action': `Перемістіть їх до обраної вами папки резервних копій, а потім видаліть цю папку, коли переконаєтеся, що ваші програми досі оновлюються та видаляються як звичайно. Повернення їх до {InstallerFolder} відновлює все. Або видаліть їх назавжди просто зараз.`,
  'Body.PendingReboot.MsiExecuteMutex': `Зараз щось використовує Windows Installer, наприклад оновлення Windows або програма, що встановлюється у фоні. «Перемістити» і «Видалити» призупинено на цей час, щоб InstallerClean не чіпав {InstallerFolder}, доки вона змінюється. Коли все завершиться, повторіть сканування, і вони повернуться.`,
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
  'Status.EnumeratingProducts': `Перелічення встановлених продуктів...`,
  'Status.CheckingRegistry': `Перевірка реєстру на додаткові пакети...`,

  // 0 = registered package count, 1 = pluralised "package"/"packages"
  'Status.RegisteredPackagesFound': `Знайдено {0} зареєстрованих {1}.`,

  // 0 = elapsed time text (e.g. "1.2s")
  'Status.ScanComplete': `Сканування завершено ({0})`,
  'Status.FoundProducts': `Сканування локальних пакетів...`,

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
  'Status.ScanFailedDb': `Сканування не вдалося: не вдалося прочитати записи Windows Installer.`,
  'Status.ScanCancelled': `Сканування скасовано.`,
  'Status.Done': `Готово`,
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
  'Completion.FailedCount.Singular': `Не вдалося перемістити {0} файл з {1}.`,
  'Completion.FailedCount.Plural': `Не вдалося перемістити {0} файлів з {1}.`,
  'Completion.FailedCountDelete.Singular': `Не вдалося видалити {0} файл з {1}.`,
  'Completion.FailedCountDelete.Plural': `Не вдалося видалити {0} файлів з {1}.`,

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
  'Summary.NothingListed.Singular': `InstallerClean не зміг упевнено визначити, які файли в кеші належать встановленим тут програмам, тож затримав єдиний файл, замість того щоб запропонувати його.`,
  'Summary.NothingListed.Plural': `InstallerClean не зміг упевнено визначити, які файли в кеші належать встановленим тут програмам, тож затримав {0} {1}, замість того щоб запропонувати їх.`,
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
  'Error.InstallerDbUnavailableTitle': `Не вдалося прочитати записи Windows Installer`,
  'Error.ScanFailedTitle': `Збій сканування`,
  'Error.InstallerDbEmpty': `Записи Windows Installer повернулися цілком порожніми: жодна встановлена програма й жодне оновлення не заявляє прав на кешований файл інсталятора. На робочому комп'ютері такого не буває (навіть у щойно встановленої Windows такі файли є), тож записи або пошкоджено, або їх не вдалося прочитати, і сканування, яке повірило б такій відповіді, помилково визнало б осиротілим кожен файл у {InstallerFolder}. Замість цього InstallerClean зупинився. Нічого не було видалено.`,
  'Error.MsiAccessDenied': `Windows Installer не дозволив InstallerClean перелічити встановлене. InstallerClean уже працював від імені адміністратора, тож запуск від імені адміністратора ще раз нічого не змінить. Без цього списку немає безпечного способу визначити, які кешовані файли ще потрібні, тож InstallerClean зупинився. Нічого не було видалено.`,
  'Error.MsiNonSuccess': `Windows Installer не зміг надати InstallerClean читабельний список встановлених програм: він прочитав {2} {3}, а потім {0} записів поспіль повернулися нечитабельними (останній код помилки {1}). Замість того щоб працювати зі списком, прочитаним лише частково, InstallerClean зупинився. Нічого не було видалено.`,
  'Error.InvalidDestinationTitle': `Недійсне призначення`,
  'Error.DestinationWriteFailedTitle': `Не вдалося записати в призначення`,
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
  'UpdateCheck.UpdateAvailable.Title': `Доступне оновлення`,

  // 0 = installed version, 1 = latest version on GitHub
  'UpdateCheck.UpdateAvailable.Body': `Ви використовуєте версію {0}.&#10;Доступна версія {1}.`,
  'UpdateCheck.Failed.NetworkUnavailable': `Не вдалося зв'язатися з GitHub. Перевірте інтернет-з'єднання та спробуйте ще раз.`,
  'UpdateCheck.Failed.ServerError': `GitHub повернув повідомлення про помилку. Спробуйте ще раз за кілька хвилин.`,
  'UpdateCheck.Failed.ResponseParseError': `Відповідь GitHub не містила розпізнаного випуску. Спробуйте пізніше або відкрийте сторінку випусків напряму.`,
  'UpdateCheck.Failed.Timeout': `Час перевірки вичерпано. Можливо, ваше з'єднання з GitHub повільне; спробуйте ще раз.`,
  'UpdateCheck.Failed.Unknown': `Перевірка не вдалася з невідомої причини. Деталі у {0}, якщо вам потрібно про це повідомити.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `The link is on your clipboard, so you can paste it in yourself:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean couldn't copy the link to your clipboard either, so here it is:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `InstallerClean більше не зміг підтвердити папку резервних копій і зупинився. Перевірте {0}, потім «Повторити сканування» і спробуйте ще раз.`,
  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Не вдається записати в {0}.`,

  // 0 = file name
  'Error.DestinationCollision': `Файл з іменем «{0}» уже є в папці резервних копій.`,

  // Result log (post-cleanup diagnostic send)
  'ResultLog.Sending': `Надсилання...`,
  'ResultLog.Sent': `Дякую! Звіт надіслано.`,
  'ResultLog.Failed': `Не вдалося надіслати. Спробуйте пізніше.`,
  'ResultLog.NothingToSend': `Немає звіту для надсилання.`,
  'ConfirmSendResultLog.Title': `Надіслати це?`,
  'ConfirmSendResultLog.Reassurance': `Надсилається на nofaff.netlify.app/api/result-log. Ніщо не ідентифікує вас чи вашу машину; це лише дає мені знати, що InstallerClean працює і [скільки місця люди звільняють].`,
  'Automation.ResultLogPreview': `Попередній перегляд звіту`,

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
  'Display.Size.GB': `{0:F2} ГБ`,
  'Display.Size.MB': `{0:F1} МБ`,
  'Display.Size.KB': `{0:F1} КБ`,
  'Display.Size.B': `{0} Б`,
  'Display.Elapsed.Ms': `{0:F0} мс`,
  'Display.Elapsed.S': `{0:F1} с`,
  'Display.ListSeparator': `, `,
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
  'Cli.Help.ExitCodeOk': `  0   успіх: запуск зробив те, про що просили, і нічого не збоїло`,
  'Cli.Help.ExitCodeError': `  1   збій: нічого не оброблено (хибні аргументи чи призначення,&#10;       невдале сканування або всі файли з помилкою)`,
  'Cli.Help.ExitCodePartial': `  2   частково: щось оброблено, щось ні (збій або Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  тимчасова: запуск заблокувала тимчасова умова (див. повідомлення)`,
  'Cli.Help.ExitCodeCancelled': `  130 скасовано (Ctrl+C)`,
  'Body.NotScanned.Lead': `Ще нічого не проскановано.`,
  'Body.NotScanned.Why': `Натисніть «Повторити сканування», щоб переглянути {InstallerFolder} і знайти файли інсталятора, яких уже не потребує жодна програма.`,
  'Confirm.MoveSameDrive': `Ця папка на тому самому диску, тож місце не повернеться, доки ви її не видалите. Виберіть натомість папку на іншому диску, якщо хочете отримати місце одразу.`,
  'Error.ScanCorrelationFailed': `InstallerClean не зміг зіставити записи Windows Installer із вмістом {InstallerFolder}. Майже нічого з того, на що вказують записи, там немає, і майже нічого з того, що там є, не названо жодним записом, тож про жоден файл не вдалося показати, що він непотрібний. Нічого не запропоновано і нічого не прибрано.`,
  'Error.CandidateOutsideCache': `Цей файл не міститься безпосередньо в папці Windows Installer; відмовлено з міркувань безпеки.`,
  'Completion.MoveCancelledSummary': `Переміщено {0} з {1} {2}, перш ніж ви скасували.`,
  'Completion.PermanentDeleteCancelledSummary': `Безповоротно видалено {0} з {1} {2}, перш ніж ви скасували.`,
  'Body.PendingReboot.Lead': `Ці файли зараз не можна прибрати.`,
  'Cli.TooManyArguments': `Помилка: неочікуваний зайвий аргумент «{0}». Якщо в шляху до папки призначення є пробіл, візьміть увесь шлях у лапки: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Папка своя в кожного користувача; запланованим і SYSTEM: /m ШЛЯХ.`,
  'Error.ScanRecordsUnreadable': `InstallerClean не зміг прочитати достатньо записів Windows Installer, щоб напевно знати, що ще потрібно: список встановлених програм повернувся неповним, а читання тих самих записів прямо з реєстру теж призвело до помилок. Файл міг видаватися осиротілим лише тому, що запис, який його називає, виявився одним із нечитабельних, тож InstallerClean зупинився. Нічого не було видалено.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer так і не повідомив про кінець списку встановлених програм: InstallerClean прочитав {2} {3}, а потім припинив спроби після {0} записів (останній код помилки {1}). Списку без кінця довіряти не можна, тож InstallerClean зупинився. Нічого не було видалено.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer так і не повідомив про кінець списку виправлень однієї програми: InstallerClean прочитав {2} {3}, а потім припинив спроби після {0} записів (останній код помилки {1}). Списку без кінця довіряти не можна, тож InstallerClean зупинився. Нічого не було видалено.`,
  'UpdateCheck.Status.UpdateAvailable': `Доступна версія {0}.`,
  'Completion.DonateAsk': `Радий, що знадобилося. Якщо ваша ласка, є куди докинути на каву.`,
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
  'Cli.FoundNoOrphans': `Непотрібних файлів не знайдено.`,
  'Cli.NothingOffered.Singular': `InstallerClean не зміг упевнено визначити, які файли в кеші належать встановленим тут програмам, тож затримав єдиний файл ({2}), замість того щоб запропонувати його.`,
  'Cli.NothingOffered.Plural': `InstallerClean не зміг упевнено визначити, які файли в кеші належать встановленим тут програмам, тож затримав усі {0} {1} ({2}), замість того щоб запропонувати їх.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean більше не зміг підтвердити папку резервних копій і зупинився. Перевірте {0}, потім запустіть команду ще раз.`,
  'Cli.Help.Summary': `Прибирає .msi і .msp з кешу, не потрібні жодній встановленій програмі.`,
  'Cli.Help.Elevation': `Потрібен командний рядок адміністратора; інакше Windows не запустить.`,
  'Error.InstallerLockUnavailableTitle': `Нічого не видалено`,
  'Error.MoveInstallerLockUnavailableTitle': `Нічого не переміщено`,
  'Error.InstallerLockUnavailable': `InstallerClean не зміг узяти блокування, яким Windows Installer не дає двом програмам одночасно змінювати встановлене ПЗ, тож не зміг виключити, що файл знадобиться на півдорозі, і нічого не видалено. Спробуйте ще раз, а якщо повторюється — перезавантажте Windows.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean не зміг узяти блокування, яким Windows Installer не дає двом програмам одночасно змінювати встановлене ПЗ, тож не зміг виключити, що файл знадобиться на півдорозі, і нічого не переміщено. Спробуйте ще раз, а якщо повторюється — перезавантажте Windows.`,
  'Cli.InstallerLockUnavailable': `Помилка: InstallerClean не зміг узяти блокування Windows Installer, яке не дає двом програмам одночасно змінювати встановлене ПЗ, тож не зміг виключити, що файл знадобиться на півдорозі. Нічого не видалено. Спробуйте ще раз, а якщо повторюється — перезавантажте Windows.`,
  'Cli.MoveInstallerLockUnavailable': `Помилка: InstallerClean не зміг узяти блокування Windows Installer, яке не дає двом програмам одночасно змінювати встановлене ПЗ, тож не зміг виключити, що файл знадобиться на півдорозі. Нічого не переміщено. Спробуйте ще раз, а якщо повторюється — перезавантажте Windows.`,
  'Completion.ReverifyIdentityClaimed': `Залишено на місці {0} {1}, бо Windows має запис про програму, названу всередині.`,
  'Completion.ReverifyIdentityUnreadable': `Залишено на місці {0} {1}, бо InstallerClean не знайшов усередині назви програми.`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean не зміг зіставити записи Windows Installer із вмістом {InstallerFolder}. У папці є файли, але жоден запис не вказує ні на що всередині неї, тож про жоден файл не вдалося показати, що він непотрібний. Нічого не запропоновано і нічого не прибрано.`,
  'Completion.NothingOffered': `На цьому ПК нічого не запропоновано`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean не зміг упевнено визначити, які файли в кеші належать встановленим тут програмам, тож затримав єдиний файл ({2}), замість того щоб запропонувати його.`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean не зміг упевнено визначити, які файли в кеші належать встановленим тут програмам, тож затримав усі {0} {1} ({2}), замість того щоб запропонувати їх.`,
  'Summary.SupersededHeldBack.Singular': `InstallerClean не зміг упевнено визначити, що єдиний заміщений файл більше не потрібен, тож затримав його.`,
  'Summary.SupersededHeldBack.Plural': `InstallerClean не зміг упевнено визначити, що {0} заміщених файлів більше не потрібні, тож затримав їх.`,
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
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `Помилка: файлову операцію поставлено в чергу до наступного перезавантаження, і InstallerClean не може визначити, які файли в ній названо, тож не може виключити {InstallerFolder}. Перезавантажте Windows перед очищенням.`,
  'Cli.MoveRestoreHint': `Переконайтеся, що ваші програми досі оновлюються та видаляються як звичайно, а потім видаліть {0}.`,
  'Error.ScanStoppedDetails': `Це також записується до {0}.`,
  'Cli.NothingListedPerFile.Singular': `InstallerClean не був певен щодо одного зі знайдених ним файлів у кеші, тож затримав саме його ({2}), замість того щоб запропонувати.`,
  'Cli.NothingListedPerFile.Plural': `InstallerClean не був певен щодо деяких зі знайдених ним файлів у кеші, тож затримав {0} {1} ({2}), замість того щоб запропонувати їх.`,
  'Cli.NothingOfferedPerFile.Singular': `InstallerClean не зміг встановити, що знайдений ним файл у кеші не потрібен, тож затримав цей єдиний файл ({2}), замість того щоб запропонувати його.`,
  'Cli.NothingOfferedPerFile.Plural': `InstallerClean про жоден зі знайдених ним файлів у кеші не зміг встановити, що він не потрібен, тож затримав усі {0} {1} ({2}), замість того щоб запропонувати їх.`,
  'Completion.NothingOfferedPerFileBody.Singular': `InstallerClean не зміг встановити, що знайдений ним файл у кеші не потрібен, тож затримав цей єдиний файл ({2}), замість того щоб запропонувати його.`,
  'Completion.NothingOfferedPerFileBody.Plural': `InstallerClean про жоден зі знайдених ним файлів у кеші не зміг встановити, що він не потрібен, тож затримав усі {0} {1} ({2}), замість того щоб запропонувати їх.`,
  'Summary.NothingListedPerFile.Singular': `InstallerClean не був певен щодо одного зі знайдених ним файлів у кеші, тож затримав його, замість того щоб запропонувати.`,
  'Summary.NothingListedPerFile.Plural': `InstallerClean не був певен щодо деяких зі знайдених ним файлів у кеші, тож затримав {0} {1}, замість того щоб запропонувати їх.`,
  'Cli.WithheldReasons.CandidateIdentity': `  Файл у теці не вдалося розпізнати, тож його не вдалося зіставити із записами.`,
  'Cli.WithheldReasons.DeclaredProductInstalled': `  Файл заявляє, що належить програмі, яка досі встановлена, тож він може бути ще потрібен.`,
  'Cli.WithheldReasons.DeclaredProductUnestablished': `  Або файл не вказав, якій програмі він належить, або Windows не відповів про цю програму.`,
  'Cli.WithheldReasons.ScreenUnanswered': `  Перевірка того, яким програмам належать файли, дала відповіді, які не збіглися з переданими їй файлами.`,
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
