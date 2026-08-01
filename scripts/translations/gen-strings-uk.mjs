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

const ALSO_KEEP = [];

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
  'Summary.RegisteredStillUsed.Few': `{0} файли ще потрібні`,
  'Summary.OrphanedToCleanUp.Few': `{0} непотрібні файли для очищення`,
  'Summary.MissingFromDisk.Few': `{0} зареєстровані файли відсутні (їх не видаляв InstallerClean). Зараз це не завдає клопоту, але в майбутньому відновлення, оновлення чи видалення тих програм може не вдатися. Відкрийте «Деталі», щоб дізнатися, що робити.`,
  // 2-4 takes "встановлені програми"; the base Plural key carries the 5+
  // genitive "встановлених програм".
  'Summary.ProgramsUnreadable.Few': `Під час цього сканування не вдалося прочитати {0} встановлені програми, тому заміщені виправлення залишено на місці. Осиротілих файлів це не стосується.`,
  'Summary.RegisteredWindow.Few': `{0} зареєстровані файли, які ще потрібні ({1})`,

  // Flat key with an inflecting adjective: one / few / (base = many).
  'Status.RegisteredPackagesFound.One': `Знайдено {0} зареєстрований {1}.`,
  'Status.RegisteredPackagesFound.Few': `Знайдено {0} зареєстровані {1}.`,

  // Flat count key: MAP base carries the few/many predicate ("вони ...
  // знадобилися"); .One supplies the n==1 masculine-singular form ("він ...").
  'Completion.ReverifySkipped.One': `{0} {1} залишено на місці: після сканування він знову знадобився програмі.`,
};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Про програму`,
  'Window.Registered.Title': `Зареєстровані файли, які не варто видаляти`,
  'Window.Orphaned.Title': `Непотрібні файли, які можна безпечно видалити`,

  // Section headings
  'Section.Registered.Products': `ПРОДУКТИ`,
  'Section.Registered.Patches': `ВИПРАВЛЕННЯ`,
  'Section.Registered.Details': `ДЕТАЛІ ПРОДУКТУ`,
  'Section.Backup.Folder': `BACKUP FOLDER`,
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
  'Action.DeletePermanently': `Видалити _назавжди`,
  'Action.Done': `_Готово`,
  'Action.Details': `Деталі`,
  'Action.BuyMeACuppa': `Пригостіть мене _кавою`,
  'Action.LeaveStarOnGitHub': `Лишити зірку на _GitHub`,
  'Action.Licence': `Ліцензія Apache 2.0`,
  'Action.Move': `Пере_містити`,
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
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
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
  'Automation.ConfirmMove': `«Перемістити» кладе непотрібні файли до обраної папки призначення. «Скасувати» лишає їх там, де вони є.`,
  'Automation.SayThanks': `Подякувати`,
  'Automation.ConfirmSendResultLog': `«Надіслати» надсилає показаний звіт до No Faff. «Скасувати» не надсилає нічого.`,
  'Automation.CheckForUpdates': `Перевірити оновлення`,
  'Automation.CheckForUpdates.HelpText': `Перевіряє на сторінці випусків github, чи є новіша версія.`,
  'Automation.UpdateAvailable.HelpText': `Відкрийте сторінку випуску, щоб завантажити новішу версію, або скасуйте, щоб лишити поточну версію.`,
  'Automation.Licence.HelpText': `Відкриває файл ліцензії на github.com у вашому браузері.`,
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `Продукти`,
  'Automation.Section.Patches': `Виправлення`,
  'Automation.Section.ProductDetails': `Деталі продукту`,
  'Automation.BackupFolder': `Backup folder`,
  'Automation.OperationProgress': `Перебіг операції`,
  'Automation.RescanInstaller': `Просканувати {InstallerFolder} ще раз`,
  'Automation.ScanningProgress': `Перебіг сканування`,
  'Automation.StartupScanProgress': `Перебіг сканування під час запуску`,
  'Automation.ViewOrphanedFiles': `Деталі, непотрібні файли`,
  'Automation.ViewOrphanedFiles.HelpText': `Доступні для очищення.`,
  'Automation.ViewRegisteredFiles': `Деталі, зареєстровані файли`,
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
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Назва суб'єкта з вбудованого сертифіката Authenticode. Ланцюжок не перевірено.`,

  // Body copy
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `Вони лежать у {InstallerFolder}, лишившись після видалення програми ({0}), заміни старого виправлення новішим ({1}) чи відкликання видавцем ({2}). InstallerClean перелічує лише ті файли, які сама Windows позначає як завершені.`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.NoFileSelected': `Виберіть файл, щоб переглянути деталі.`,
  'Body.NoProductSelected': `Виберіть продукт, щоб переглянути деталі.`,
  'Body.NoMetadata': `Метадані недоступні.`,
  'Body.RegisteredMissingFromDisk': `Цей файл інсталятора видалено. InstallerClean цього не робив, він ніколи не видаляє файл, який ще потрібен програмі; цей видалило щось інше, перш ніж ви запустили InstallerClean.&#10;&#10;Зараз це не завдає клопоту і не завдаватиме аж до дня, коли ви спробуєте відновити, оновити чи видалити програму, якій він належить. Тоді цей крок може не вдатися, бо Windows шукатиме цей файл, а його там немає.&#10;&#10;Щоб спробувати це виправити, завантажте інсталятор тієї програми від її виробника та запустіть його поверх наявної копії (не видаляйте програму спершу, бо видалення саме по собі є кроком, який потребує цього файлу). Якщо можете дістати ту версію, яку ви встановили, використайте її, бо Windows може відхилити іншу. Зазвичай це відновлює файл, і ваші налаштування звичайно лишаються недоторканими, але Microsoft цього не гарантує, її власний останній засіб — це перевстановлення програми або самої Windows.`,
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
  'Status.Moving': `Переміщення: {0} {1}...`,
  'Status.Deleting': `Видалення: {0} {1}...`,
  'Status.MoveCancelled.Partial': `Переміщення скасовано. Опрацьовано {0} з {1} {2}.`,
  'Status.DeleteCancelled.Partial': `Видалення скасовано. Опрацьовано {0} з {1} {2}.`,
  'Status.MoveFailed': `Не вдалося перемістити ({0}). Деталі у {1}.`,
  'Status.MoveFailed.NoLog': `Не вдалося перемістити ({0}). Не вдалося записати журнал збоїв.`,
  'Status.DeleteFailed': `Не вдалося видалити ({0}). Деталі у {1}.`,
  'Status.DeleteFailed.NoLog': `Не вдалося видалити ({0}). Не вдалося записати журнал збоїв.`,
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
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} файл ще потрібен`,
  'Summary.RegisteredStillUsed.Plural': `{0} файлів ще потрібно`,
  'Summary.OrphanedToCleanUp.Singular': `{0} непотрібний файл для очищення`,
  'Summary.OrphanedToCleanUp.Plural': `{0} непотрібних файлів для очищення`,
  'Summary.MissingFromDisk.Singular': `{0} зареєстрований файл відсутній (його не видаляв InstallerClean). Зараз це не завдає клопоту, але в майбутньому відновлення, оновлення чи видалення тієї програми може не вдатися. Відкрийте «Деталі», щоб дізнатися, що робити.`,
  'Summary.MissingFromDisk.Plural': `{0} зареєстрованих файлів відсутні (їх не видаляв InstallerClean). Зараз це не завдає клопоту, але в майбутньому відновлення, оновлення чи видалення тих програм може не вдатися. Відкрийте «Деталі», щоб дізнатися, що робити.`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0} з {1} {2}`,

  // Orphaned-window footer: unneeded files split into the three removable causes
  // (genitive plural adjectives read at any count; no trailing noun).
  'Summary.OrphanedWindow': `{0} осиротілих, {1} заміщених, {2} застарілих ({3})`,

  // Registered-window footer, split singular/plural. 0 = count, 1 = size display.
  'Summary.RegisteredWindow.Singular': `{0} зареєстрований файл, який ще потрібен ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} зареєстрованих файлів, які ще потрібні ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Перемістити {0} {1} ({2})?`,

  // 0 = destination path
  'Confirm.MoveDestination': `Файли буде переміщено до:`,
  'Confirm.DeleteTitle': `Видалити {0} {1} ({2})?`,

  // Error messages
  'Error.AdminRequiredTitle': `Відмовлено в доступі`,
  'Error.AdminRequiredBody': `Windows відмовив InstallerClean у доступі, тому роботу було зупинено. Нічого не було видалено.\n\nInstallerClean уже працював від імені адміністратора, тож запускати його так ще раз не допоможе. Windows не повідомляє нічого більше про те, що саме відмовило в доступі, тож немає нічого конкретного, що варто спробувати.`,
  'Error.InstallerDbUnavailableTitle': `Не вдалося прочитати записи Windows Installer`,
  'Error.ScanFailedTitle': `Збій сканування`,
  'Error.InstallerDbEmpty': `Записи Windows Installer повернулися цілком порожніми: жодна встановлена програма й жодне оновлення не заявляє прав на кешований файл інсталятора. На робочому комп'ютері такого не буває (навіть у щойно встановленої Windows такі файли є), тож записи або пошкоджено, або їх не вдалося прочитати, і сканування, яке повірило б такій відповіді, помилково визнало б осиротілим кожен файл у {InstallerFolder}. Замість цього InstallerClean зупинився. Нічого не було видалено.`,
  'Error.MsiAccessDenied': `Windows Installer не дозволив InstallerClean перелічити встановлене. InstallerClean уже працював від імені адміністратора, тож запуск від імені адміністратора ще раз нічого не змінить. Без цього списку немає безпечного способу визначити, які кешовані файли ще потрібні, тож InstallerClean зупинився. Нічого не було видалено.`,
  'Error.MsiNonSuccess': `Windows Installer не зміг надати InstallerClean читабельний список встановлених програм: {0} записів поспіль повернулися нечитабельними (останній код помилки {1}). Замість того щоб працювати зі списком, прочитаним лише частково, InstallerClean зупинився. Нічого не було видалено.`,
  'Error.InvalidDestinationTitle': `Недійсне призначення`,
  'Error.DestinationWriteFailedTitle': `Не вдалося записати в призначення`,
  'Error.MoveFailedTitle': `Не вдалося перемістити`,
  'Error.DeleteFailedTitle': `Не вдалося видалити`,
  'Error.SettingNotSavedTitle': `Налаштування не збережено`,
  'Error.SettingNotSavedBody': `Не вдалося зберегти зміну. Під час наступного запуску InstallerClean повернеться до попереднього налаштування.`,
  'Error.DestinationInsideInstaller': `Призначення не може бути всередині папки Windows Installer.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `Призначення {0} вказує всередину системної папки Windows. Виберіть шлях поза %SystemRoot%, %ProgramFiles% та %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Недостатньо місця`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `Недостатньо місця в {0}\n\nПотрібно: {1}\nДоступно: {2}`,

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
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `Windows повідомив про помилку файлу; файл залишено на місці.`,
  'Error.IOFailure.Plural': `Windows повідомив про помилки файлів; ці файли залишено на місці.`,
  'Error.UnknownError.Singular': `З цим файлом щось пішло не так; його залишено на місці.`,
  'Error.UnknownError.Plural': `З цими файлами щось пішло не так; їх залишено на місці.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Відмова перемістити файли до папки Windows Installer (призначення: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `Розташування для переміщення має бути повним шляхом до папки, що починається з літери диска або мережевої папки (наприклад, D:\\Backup чи \\\\server\\backup). InstallerClean не може використати такий шлях: {0}`,
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
  'UpdateCheck.Failed.Unknown': `Перевірка не вдалася з невідомої причини. Деталі у crash.log, якщо вам потрібно про це повідомити.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `InstallerClean не зміг відкрити ваш браузер. Посилання скопійовано до буфера обміну, тож ви можете вставити його самі:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean не зміг відкрити ваш браузер і не зміг скопіювати посилання до буфера обміну. Ось воно:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `Папка призначення змінилася, поки тривало переміщення (щось замінило чи перенаправило її), тож InstallerClean зупинився, щоб не записати файли не туди. Перевірте {0}, потім повторіть сканування і спробуйте ще раз.`,

  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Не вдається записати в {0}.`,

  // 0 = file name
  'Error.NoUniqueFilename': `Не вдалося знайти унікальне ім'я файлу для «{0}» після 10 000 спроб.`,

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
  'Startup.AlreadyRunningBody': `InstallerClean уже працює.`,
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
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ElapsedLong.LessThanASecond': `менш ніж секунду`,
  'Display.ElapsedLong.Seconds': `{0:F1} секунди`,
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
  'Tooltip.ChangeLanguage': `Змінити мову. Програму буде перезапущено.`,
  'Automation.ChangeLanguage': `Змінити мову`,
  'Automation.ChangeLanguage.HelpText': `Програму буде перезапущено.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  'Cli.UnknownArgument': `Невідомий аргумент: «{0}»`,
  'Cli.Cancelling': `Скасування...`,
  'Cli.Cancelled': `Скасовано.`,
  'Cli.GenericError': `Помилка: {0}. Деталі записано до {1}.`,
  'Cli.GenericError.NoLog': `Помилка: {0}. Не вдалося записати журнал збоїв.`,
  'Cli.ScanningInstaller': `Сканування {InstallerFolder}...`,
  'Cli.FoundOrphans': `Знайдено {0} {1} для очищення ({2}).`,
  'Cli.NothingToDo': `Немає чого робити.`,
  'Cli.DeletingFiles': `Видалення: {0} {1}...`,
  'Cli.DeletedFiles': `Permanently deleted {0} {1}.`,
  'Cli.NoMoveDestination': `Помилка: не вказано розташування для переміщення. Скористайтеся /m ШЛЯХ. (Типове значення, задане в графічному інтерфейсі, діє лише для поточного користувача і не застосовується до запусків за розкладом чи від імені службового облікового запису.)`,
  'Cli.MoveDestinationInsideInstaller': `Помилка: призначення не може бути всередині папки Windows Installer.`,
  'Cli.MoveDestinationRelative': `Помилка: призначення має бути повним шляхом. Отримано: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Помилка: призначення {0} вказує всередину системної папки Windows. Виберіть шлях поза %SystemRoot%, %ProgramFiles% та %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Помилка: на цій машині призупинено попередню транзакцію Windows Installer. Поновіть або відкотіть те встановлення (чи перезавантажте Windows), перш ніж очищати кеш.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Помилка: поставлена в чергу після перезавантаження файлова операція націлена на кеш інсталятора ({0}). Перезавантажте Windows, щоб завершити ту операцію, перш ніж очищати.`,
  'Cli.MovingFiles': `Переміщення: {0} {1} до {2}...`,
  'Cli.MovedFiles': `Переміщено {0} {1}.`,
  'Cli.MutexBlocked': `Інший процес InstallerClean утримує блокування єдиного екземпляра (графічний інтерфейс чи інший запуск CLI). Вихід 75 (тимчасовий); можна безпечно повторити пізніше.`,
  'Cli.EventLogUnavailable': `Примітка: не вдалося записати до журналу подій. Перевірте дозволи журналу «Програма» чи групову політику.`,
  'Cli.Help.Header': `InstallerClean - очищення {InstallerFolder}`,
  'Cli.Help.Usage': `Використання:`,
  'Cli.Help.Help': `  installerclean-cli --help     Показати цю довідку (також приймає /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  Вивести версію (також приймає -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Перемістити в збережену папку`,
  'Cli.Help.MovePath': `  installerclean-cli /m ШЛЯХ    Перемістити за вказаним шляхом`,
  'Cli.Help.NoteLine1': `installerclean-cli — це консольний процес, що блокує командний рядок до`,
  'Cli.Help.NoteLine2': `завершення; перенаправляйте чи передавайте його вивід, як у будь-якого`,
  'Cli.Help.NoteLine3': `іншого консольного exe. Графічний інтерфейс у InstallerClean.exe.`,
  'Cli.Help.ExitCodesHeader': `Коди виходу:`,
  'Cli.Help.ExitCodeOk': `  0   успіх: оброблено кожен позначений файл`,
  'Cli.Help.ExitCodeError': `  1   невдача: нічого не оброблено (аргументи, сканування або файли)`,
  'Cli.Help.ExitCodePartial': `  2   частково: частину файлів оброблено, частину ні`,
  'Cli.Help.ExitCodeTransient': `  75  тимчасова: запуск заблокувала тимчасова умова (див. повідомлення)`,
  'Cli.Help.ExitCodeCancelled': `  130 скасовано (Ctrl+C)`,
  'Body.NotScanned.Lead': `Ще нічого не проскановано.`,
  'Body.NotScanned.Why': `Натисніть «Повторити сканування», щоб переглянути {InstallerFolder} і знайти файли інсталятора, яких уже не потребує жодна програма.`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,
  'Error.ScanCorrelationFailed': `InstallerClean не зміг узгодити це сканування із записами Windows Installer: кожного файлу, який Windows досі вважає потрібним, немає в {InstallerFolder}, а файли, що справді лежать у цій папці, не відповідають жодному запису. Жоден справжній комп'ютер так не виглядає, тож це вказує на проблему з читанням записів, а не на файли, які можна безпечно видалити. Для очищення нічого не запропоновано, і нічого не було видалено.`,
  'Error.CandidateOutsideCache': `Цей файл не міститься безпосередньо в папці Windows Installer; відмовлено з міркувань безпеки.`,
  'Completion.ReverifySkipped': `{0} {1} залишено на місці: після сканування вони знову знадобилися програмі.`,
  'Completion.MoveCancelledSummary': `Переміщено {0} з {1} {2}, перш ніж ви скасували.`,
  'Completion.PermanentDeleteCancelledSummary': `Безповоротно видалено {0} з {1} {2}, перш ніж ви скасували.`,
  'Body.PendingReboot.Lead': `Ці файли зараз не можна прибрати.`,
  'Cli.TooManyArguments': `Помилка: неочікуваний зайвий аргумент «{0}». Якщо в назві папки для переміщення є пробіл, візьміть увесь шлях у лапки: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Збережена папка своя в кожного користувача; розклад і SYSTEM: /m ШЛЯХ.`,
  'Completion.ReverifyIncomplete': `{0} {1} залишено на місці: під час повторної перевірки не вдалося повністю прочитати записи Windows Installer.`,
  'Summary.ProgramsUnreadable.Singular': `Під час цього сканування не вдалося прочитати {0} встановлену програму, тому заміщені виправлення залишено на місці. Осиротілих файлів це не стосується.`,
  'Summary.ProgramsUnreadable.Plural': `Під час цього сканування не вдалося прочитати {0} встановлених програм, тому заміщені виправлення залишено на місці. Осиротілих файлів це не стосується.`,
  'Error.ScanRecordsUnreadable': `InstallerClean не зміг прочитати достатньо записів Windows Installer, щоб напевно знати, що ще потрібно: список встановлених програм повернувся неповним, а читання тих самих записів прямо з реєстру теж призвело до помилок. Файл міг видаватися осиротілим лише тому, що запис, який його називає, виявився одним із нечитабельних, тож InstallerClean зупинився. Нічого не було видалено.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer так і не повідомив про кінець списку встановлених програм: InstallerClean припинив спроби після {0} записів (останній код помилки {1}). Списку без кінця довіряти не можна, тож InstallerClean зупинився. Нічого не було видалено.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer так і не повідомив про кінець списку виправлень однієї програми: InstallerClean припинив спроби після {0} записів (останній код помилки {1}). Списку без кінця довіряти не можна, тож InstallerClean зупинився. Нічого не було видалено.`,
  'UpdateCheck.Status.UpdateAvailable': `Доступна версія {0}.`,
  'Completion.DonateAsk': `Радий, що знадобилося. Якщо ваша ласка, є куди докинути на каву.`,
  'About.Link.Guide': `Посібник і поширені запитання`,
  'About.Link.ReportProblem': `Повідомити про проблему`,
  'About.AutoUpdateCheck': `Автоматично перевіряти оновлення`,
  'Automation.About.Guide.HelpText': `Відкриває readme на github у вашому браузері.`,
  'Automation.About.ReportProblem.HelpText': `Відкриває список проблем (Issues) на github.com у вашому браузері.`,
  'Automation.AutoUpdateCheck.HelpText': `Якщо позначено, InstallerClean під час запуску перевіряє на github наявність новішої версії.`,
  'Tooltip.MoveSameDrive': `Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Confirm.DeletePermanently.Plural': `Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
};

let text = readFileSync(BASE, 'utf8');

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
console.log('machine Cli <data> removed:', cliMachineRemoved, '(expect 20)');
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
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === 20 && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
