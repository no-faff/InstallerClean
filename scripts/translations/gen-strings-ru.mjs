#!/usr/bin/env node
// Russian (ru) satellite generator for InstallerClean. Copied from
// gen-strings-template.mjs; only OUT and the MAP values changed. Works FROM THE
// ENGLISH SOURCE Strings.resx: removes every Cli.* key by name, swaps each
// remaining <value> for its Russian translation, keeps schema/resheaders/
// <comment> children/&#10; entities/Windows-path backslashes/whitespace
// byte-identical to the neutral, writes LF/UTF-8, then self-checks. Run from the
// repo root: node scripts/translations/gen-strings-ru.mjs
//
// MAP escaping (template literals): \\ is one backslash (the paths), \n is a real
// newline (the multi-line values), {0}/{1} are .NET placeholders left verbatim,
// and &#10; is written literally where the neutral uses the XML entity.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = 'src/InstallerClean.Core/Resources/Strings.ru.resx';

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

// Per-language keeps: Russian has a native rendering for every translatable
// token (patch -> исправление, the term Microsoft's own Russian uses for an
// .msp), so nothing beyond KEEP_ENGLISH stays English.
const ALSO_KEEP = [];

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `О программе`,
  'Window.Registered.Title': `Зарегистрированные файлы, которые не стоит удалять`,
  'Window.Orphaned.Title': `Ненужные файлы, которые можно безопасно удалить`,

  // Section headings
  'Section.Registered.Products': `ПРОДУКТЫ`,
  'Section.Registered.Patches': `ИСПРАВЛЕНИЯ`,
  'Section.Registered.Details': `СВЕДЕНИЯ О ПРОДУКТЕ`,
  'Section.Backup.Folder': `BACKUP FOLDER`,
  'Section.SayThanks': `ПОБЛАГОДАРИТЬ`,

  // Field labels (used in detail panels)
  'Field.Reason': `Причина`,
  'Field.Author': `Автор`,
  'Field.Application': `Приложение`,
  'Field.Title': `Название`,
  'Field.Subject': `Тема`,
  'Field.Keywords': `Ключевые слова`,
  'Field.SigningCertificate': `Сертификат подписи`,
  'Field.FileSize': `Размер файла`,
  'Field.Comment': `Комментарий`,
  'Field.ProductName': `Название продукта`,
  'Field.File': `Файл`,
  'Field.Size': `Размер`,
  'Field.Patches': `Исправления`,

  // Placeholder shown for a registered package whose API ProductName is empty.
  'Field.UnknownProductName': `(неизвестно)`,
  'Field.PatchesOnly': `(только исправления)`,
  'Field.Missing': `отсутствует`,

  // Actions (button labels; underscore prefixes are WPF mnemonics)
  // About takes Alt+Р (not the natural Alt+О): О collides with Browse (Обзор) in the
  // results view and with Cancel (Отмена) in the operating view, both co-visible with
  // About, and Cancel must keep its conventional Alt+О, so About is the one that moves.
  'Action.About': `О п_рограмме`,
  'Action.Copy': `Копировать`,
  'Action.Cut': `Вырезать`,
  'Action.Paste': `Вставить`,
  'Action.SelectAll': `Выделить всё`,
  'Action.Browse': `_Обзор...`,
  'Action.Cancel': `_Отмена`,
  'Action.CheckForUpdates': `Проверить о_бновления`,
  'Action.Close': `_Закрыть`,
  'Action.DeletePermanently': `_Удалить безвозвратно`,
  'Action.Done': `_Готово`,
  'Action.Details': `Подробности`,
  'Action.BuyMeACuppa': `_Угостите меня чаем`,
  'Action.LeaveStarOnGitHub': `Поставить з_везду на GitHub`,
  'Action.Licence': `Лицензия Apache 2.0`,
  'Action.Move': `_Переместить`,
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
  'Action.OpenReleasePage': `Открыть страницу _выпуска`,
  // Rescan takes Alt+в (not the natural Alt+П): П collides with Move (Переместить) in the
  // results view, where Move is the primary action and keeps П; С is taken by ScanAgain
  // (co-visible at completion), so Rescan moves to в in По_вторить.
  'Action.Rescan': `По_вторить сканирование`,
  'Action.ScanAgain': `_Сканировать снова`,
  'Action.SendResultLog': `Отправить отчёт`,
  'Action.SendResultLogConfirm': `От_править`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa': `Поддержать`,
  'Automation.BuyMeACuppa.About': `Угостите меня чаем`,
  'Automation.CancelOperation': `Отменить операцию`,
  'Automation.CancelScan': `Отменить сканирование`,
  'Automation.CancelStartupScan': `Отменить сканирование при запуске`,
  'Automation.Close': `Закрыть`,
  'Automation.CloseWindow': `Закрыть окно`,
  'Automation.CloseResult': `Закрыть результат и вернуться в главное окно`,
  'Automation.LeaveStarOnGitHub.About': `Поставить звезду на github`,
  'Automation.Minimise': `Свернуть`,
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
  'Automation.ConfirmMove': `«Переместить» помещает ненужные файлы в выбранную папку назначения. «Отмена» оставляет их на месте.`,
  'Automation.SayThanks': `Поблагодарить`,
  'Automation.ConfirmSendResultLog': `«Отправить» передаёт показанный отчёт в No Faff. «Отмена» не отправляет ничего.`,
  'Automation.CheckForUpdates': `Проверить обновления`,
  'Automation.CheckForUpdates.HelpText': `Проверяет на странице выпусков github, есть ли более новая версия.`,
  'Automation.UpdateAvailable.HelpText': `Откройте страницу выпуска, чтобы скачать более новую версию, или нажмите «Отмена», чтобы оставить текущую.`,
  'Automation.Licence.HelpText': `Открывает файл лицензии на github.com в вашем браузере.`,
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `Продукты`,
  'Automation.Section.Patches': `Исправления`,
  'Automation.Section.ProductDetails': `Сведения о продукте`,
  'Automation.BackupFolder': `Backup folder`,
  'Automation.OperationProgress': `Ход операции`,
  'Automation.RescanInstaller': `Сканировать {InstallerFolder} заново`,
  'Automation.ScanningProgress': `Ход сканирования`,
  'Automation.StartupScanProgress': `Ход сканирования при запуске`,
  'Automation.ViewOrphanedFiles': `Подробности, ненужные файлы`,
  'Automation.ViewOrphanedFiles.HelpText': `Доступны для очистки.`,
  'Automation.ViewRegisteredFiles': `Подробности, зарегистрированные файлы`,
  'Automation.ViewRegisteredFiles.HelpText': `Список только для чтения.`,
  'Automation.SortStatus.Ascending': `Сортировка по столбцу «{0}», по возрастанию`,
  'Automation.SortStatus.Descending': `Сортировка по столбцу «{0}», по убыванию`,
  'Automation.Scroll.ScanResults': `Результаты сканирования`,
  'Automation.Scroll.ResultDetails': `Сведения о результате`,
  'Automation.Scroll.FileDetails': `Сведения о файле`,
  'Automation.Scroll.DialogBody': `Текст диалогового окна`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `Файлы, которые не удалось обработать`,
  'Automation.RegisteredMissingSeeAlso': `Объясняет эту папку и то, как восстановить файл, в README`,

  // Tooltips
  'Tooltip.BuyMeACuppa.About': `В горле пересохло!`,
  'Tooltip.CancellingPending': `Запрошена отмена. InstallerClean ждёт, когда текущий шаг дойдёт до точки остановки. Это может занять несколько секунд при интенсивном вводе-выводе или обращении к базе данных MSI.`,
  'Tooltip.Close': `Закрыть`,
  'Tooltip.LeaveStarOnGitHub.About': `Звезда помогает другим найти InstallerClean.`,
  'Tooltip.Minimise': `Свернуть`,
  'Tooltip.SendResultLog': `На ваше усмотрение, но будет приятно. Отправляет анонимную сводку, которая просто даёт мне знать, работает ли приложение и сколько места люди освобождают. На следующем экране вы увидите, что именно будет отправлено, прежде чем подтвердить.`,
  'Tooltip.SendResultLog.NothingFound': `На ваше усмотрение, но будет приятно. Отправляет анонимную сводку, которая просто даёт мне знать, работает ли приложение. На следующем экране вы увидите, что именно будет отправлено, прежде чем подтвердить.`,
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Имя субъекта из встроенного сертификата Authenticode. Цепочка не проверялась.`,

  // Body copy
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `Они лежат в {InstallerFolder} и остаются после того, как программа была удалена ({0}), более новое исправление заменило одно из них ({1}) или издатель его отозвал ({2}). InstallerClean всегда перечисляет только те файлы, которые сам Windows объявляет отработавшими.`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.NoFileSelected': `Выберите файл, чтобы посмотреть сведения.`,
  'Body.NoProductSelected': `Выберите продукт, чтобы посмотреть сведения.`,
  'Body.NoMetadata': `Метаданные недоступны.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.&#10;&#10;It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.&#10;&#10;To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `README [объясняет эту папку] и то, как восстановить файл, словами самой Microsoft.`,
  'Body.NoPatches': `(нет)`,

  // Reasons (Reason column in the orphaned-files list)
  'Reason.Orphaned': `Бесхозный`,
  'Reason.Superseded': `Замещённый`,
  'Reason.Obsoleted': `Устаревший`,

  // Status / progress text
  'Status.Scanning': `Сканирование...`,
  'Status.Cancelling': `Отмена...`,
  'Status.StartingScan': `Запуск сканирования...`,
  'Status.QueryingApi': `Запрос к Windows об установленном ПО...`,
  'Status.ScanningCache': `Сканирование папки кэша установки...`,
  'Status.EnumeratingProducts': `Перечисление установленных продуктов...`,
  'Status.CheckingRegistry': `Проверка реестра на дополнительные пакеты...`,

  // 0 = registered package count, 1 = pluralised "package"/"packages"
  'Status.RegisteredPackagesFound': `Найдено зарегистрированных {1}: {0}.`,

  // 0 = elapsed time text (e.g. "1.2s")
  'Status.ScanComplete': `Сканирование завершено ({0})`,
  'Status.FoundProducts': `Сканирование локальных пакетов...`,

  // 0 = file count, 1 = pluralised noun ("file"/"files")
  'Status.FoundUnused': `Найдено {0} {1} для безопасного удаления.`,
  'Status.PreparingDestination': `Подготовка папки назначения...`,

  // 0 = file count, 1 = pluralised noun
  'Status.Moving': `Moving unneeded files...`,
  'Status.Deleting': `Deleting unneeded files...`,
  'Status.MoveCancelled.Partial': `Перемещение отменено. Обработано {0}/{1} {2}.`,
  'Status.DeleteCancelled.Partial': `Удаление отменено. Обработано {0}/{1} {2}.`,
  'Status.MoveFailed': `Не удалось переместить ({0}). Подробности в {1}.`,
  'Status.MoveFailed.NoLog': `Не удалось переместить ({0}). Не удалось записать журнал сбоев.`,
  'Status.DeleteFailed': `Не удалось удалить ({0}). Подробности в {1}.`,
  'Status.DeleteFailed.NoLog': `Не удалось удалить ({0}). Не удалось записать журнал сбоев.`,
  'Status.ScanAccessDenied': `Доступ запрещён. Windows отклонил сканирование.`,
  'Status.ScanFailedDb': `Сканирование не удалось: не удалось прочитать записи Windows Installer.`,
  'Status.ScanCancelled': `Сканирование отменено.`,
  'Status.Done': `Готово`,
  'Status.ScanFailedDetails': `Сбой сканирования ({0}). Подробности в {1}.`,
  'Status.ScanFailedDetails.NoLog': `Сбой сканирования ({0}). Не удалось записать журнал сбоев.`,

  // Completion screen
  'Completion.AllClean': `Всё чисто`,
  'Completion.NothingToCleanUp': `В {InstallerFolder} нечего очищать`,
  'Completion.NothingToCleanUpReceipt': `Просканировано {0} {1} за {2}`,

  // 0 = size freed (e.g. "120.5 MB")
  'Completion.Freed': `Освобождено {0}`,
  'Completion.Moved': `Перемещено {0}`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `Ничего не перемещено`,
  'Completion.NothingDeleted': `Ничего не удалено`,
  'Completion.FailedCount.Singular': `Не удалось переместить {0} файл из {1}.`,
  'Completion.FailedCount.Plural': `Не удалось переместить {0} файлов из {1}.`,
  'Completion.FailedCountDelete.Singular': `Не удалось удалить {0} файл из {1}.`,
  'Completion.FailedCountDelete.Plural': `Не удалось удалить {0} файлов из {1}.`,

  // 0 = moved count, 1 = pluralised noun, 2 = destination path
  'Completion.MoveSummary.Singular': `Перемещено {0} {1} в: {2}`,
  'Completion.MoveSummary.Plural': `Перемещено {0} {1} в: {2}`,

  // 0 = deleted count, 1 = pluralised noun

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} файл ещё нужен`,
  'Summary.RegisteredStillUsed.Plural': `{0} файлов ещё нужны`,
  'Summary.OrphanedToCleanUp.Singular': `{0} ненужный файл для очистки`,
  'Summary.OrphanedToCleanUp.Plural': `{0} ненужных файлов для очистки`,
  'Summary.MissingFromDisk.Singular': `Отсутствует {0} зарегистрированный файл (InstallerClean его не удалял). Сейчас это не доставляет хлопот, но в будущем восстановление, обновление или удаление той программы может не выполниться. Откройте «Подробности», чтобы узнать, что делать.`,
  'Summary.MissingFromDisk.Plural': `Отсутствует {0} зарегистрированных файлов (InstallerClean их не удалял). Сейчас это не доставляет хлопот, но в будущем восстановление, обновление или удаление тех программ может не выполниться. Откройте «Подробности», чтобы узнать, что делать.`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0}/{1} {2}`,

  // OrphanedWindow: by-cause tally. 0/1/2 = orphaned/superseded/obsoleted counts,
  // 3 = size. Reason.* stems in the genitive-plural elliptical form (invariant across
  // counts, so one flat form serves all). RegisteredWindow split into Singular/Plural
  // (0 = count, 1 = size); Russian adds .Few in OVERRIDES, and .Plural serves CLDR
  // Many (ru integers never resolve to Other), so no .Many key, matching the GUI's
  // Summary.RegisteredStillUsed shape.
  'Summary.OrphanedWindow': `{0} бесхозных, {1} замещённых, {2} устаревших ({3})`,
  'Summary.RegisteredWindow.Singular': `{0} зарегистрированный файл ещё нужен ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} зарегистрированных файлов ещё нужны ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Переместить {0} {1} ({2})?`,

  // 0 = destination path
  'Confirm.MoveDestination': `Файлы будут перемещены в:`,
  'Confirm.DeleteTitle': `Удалить {0} {1} ({2})?`,

  // Error messages
  'Error.AdminRequiredTitle': `Доступ запрещён`,
  'Error.AdminRequiredBody': `Windows отказал InstallerClean в доступе, поэтому работа была остановлена. Ничего не было удалено.\n\nInstallerClean уже был запущен от имени администратора, поэтому повторный запуск таким же образом не поможет. Windows не сообщает ничего больше о том, что именно отказало в доступе, поэтому пробовать что-то конкретное бессмысленно.`,
  'Error.InstallerDbUnavailableTitle': `Не удалось прочитать записи Windows Installer`,
  'Error.ScanFailedTitle': `Сбой сканирования`,
  'Error.InstallerDbEmpty': `Записи Windows Installer вернулись совершенно пустыми: ни одна установленная программа и ни одно обновление не заявляет прав на кэшированный файл установщика. На работающем компьютере такого не бывает (даже у свежей установки Windows такие файлы есть), значит, записи либо повреждены, либо их не удалось прочитать, и сканирование, поверившее такому ответу, ошибочно сочло бы бесхозным каждый файл в {InstallerFolder}. Вместо этого InstallerClean остановился. Ничего не было удалено.`,
  'Error.MsiAccessDenied': `Windows Installer не позволил InstallerClean перечислить установленное. InstallerClean уже был запущен от имени администратора, поэтому повторный запуск от имени администратора ничего не изменит. Без этого списка невозможно безопасно определить, какие кэшированные файлы ещё нужны, поэтому InstallerClean остановился. Ничего не было удалено.`,
  'Error.MsiNonSuccess': `Windows Installer не смог предоставить InstallerClean читаемый список установленных программ: {0} записей подряд вернулись нечитаемыми (последний код ошибки {1}). Вместо того чтобы работать с прочитанным лишь частично списком, InstallerClean остановился. Ничего не было удалено.`,
  'Error.InvalidDestinationTitle': `Недопустимая папка назначения`,
  'Error.DestinationWriteFailedTitle': `Не удалось записать в папку назначения`,
  'Error.MoveFailedTitle': `Сбой перемещения`,
  'Error.DeleteFailedTitle': `Сбой удаления`,
  'Error.SettingNotSavedTitle': `Настройка не сохранена`,
  'Error.SettingNotSavedBody': `Не удалось сохранить изменение. При следующем запуске InstallerClean вернётся к прежней настройке.`,
  'Error.DestinationInsideInstaller': `Папка назначения не может находиться внутри папки Windows Installer.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Недостаточно места`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `Недостаточно места в {0}\n\nТребуется: {1}\nДоступно: {2}`,

  // 0 = destination
  'Error.AccessDeniedDestination': `У вас нет прав на запись в {0}.\nПопробуйте папку в своём профиле пользователя или на диске, которым владеете.`,
  'Error.PathTooLong': `Путь {0} слишком длинный для Windows. Выберите путь покороче.`,
  'Error.DestinationMissing': `Папка {0} не существует, и её не удалось создать. Проверьте букву диска или сетевой путь.`,
  'Error.IOWriteDestination': `Windows не может выполнить запись в {0}.\nПодробности в {1}.`,
  'Error.IOWriteDestination.NoLog': `Windows не может выполнить запись в {0}. Не удалось записать журнал сбоев.`,
  'Error.WriteDestination': `Не удаётся выполнить запись в {0}.\nПодробности в {1}.`,
  'Error.WriteDestination.NoLog': `Не удаётся выполнить запись в {0}. Не удалось записать журнал сбоев.`,
  'Error.MissingSourceFile': `Файл больше не существует.`,
  'Error.SourceIsReparsePoint': `Исходный файл является символической ссылкой или точкой соединения; отклонено в целях безопасности.`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows отказал в доступе к этому файлу; он оставлен на месте.`,
  'Error.AccessDenied.Plural': `Windows отказал в доступе к этим файлам; они оставлены на месте.`,
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `Windows сообщил об ошибке файла; файл оставлен на месте.`,
  'Error.IOFailure.Plural': `Windows сообщил об ошибках файлов; эти файлы оставлены на месте.`,
  'Error.UnknownError.Singular': `С этим файлом что-то пошло не так; он оставлен на месте.`,
  'Error.UnknownError.Plural': `С этими файлами что-то пошло не так; они оставлены на месте.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Перемещение файлов в папку Windows Installer отклонено (назначение: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\\Backup, or \\\\server\\backup). InstallerClean can't use this one: {0}`,
  'BrowserLaunch.FailedTitle': `Не удалось открыть браузер`,
  'UpdateCheck.Title': `Проверка обновлений`,
  'UpdateCheck.Status.Checking': `Проверка...`,
  'UpdateCheck.Status.UpToDate': `Установлена последняя версия.`,
  'UpdateCheck.UpdateAvailable.Title': `Доступно обновление`,

  // 0 = installed version, 1 = latest version on GitHub
  'UpdateCheck.UpdateAvailable.Body': `У вас установлена версия {0}.&#10;Доступна версия {1}.`,
  'UpdateCheck.Failed.NetworkUnavailable': `Не удалось подключиться к GitHub. Проверьте подключение к интернету и повторите попытку.`,
  'UpdateCheck.Failed.ServerError': `GitHub вернул ответ с ошибкой. Повторите попытку через несколько минут.`,
  'UpdateCheck.Failed.ResponseParseError': `В ответе GitHub не оказалось распознаваемого выпуска. Повторите попытку позже или откройте страницу выпусков напрямую.`,
  'UpdateCheck.Failed.Timeout': `Время ожидания проверки истекло. Возможно, соединение с GitHub медленное; повторите попытку.`,
  'UpdateCheck.Failed.Unknown': `Проверка не удалась по неизвестной причине. Подробности в crash.log, если нужно сообщить об этом.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `InstallerClean не удалось открыть браузер. Ссылка скопирована в буфер обмена, так что можете вставить её сами:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean не удалось открыть браузер, и скопировать ссылку в буфер обмена тоже не удалось. Вот ссылка:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again.`,

  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Не удаётся выполнить запись в {0}.`,

  // 0 = file name
  'Error.NoUniqueFilename': `Не удалось подобрать уникальное имя файла для «{0}» после 10 000 попыток.`,

  // Result log (post-cleanup diagnostic send)
  'ResultLog.Sending': `Отправка...`,
  'ResultLog.Sent': `Спасибо! Отчёт отправлен.`,
  'ResultLog.Failed': `Не удалось отправить. Повторите попытку позже.`,
  'ResultLog.NothingToSend': `Нет отчёта для отправки.`,
  'ConfirmSendResultLog.Title': `Отправить это?`,
  'ConfirmSendResultLog.Reassurance': `Отправляется на nofaff.netlify.app/api/result-log. Ничто не идентифицирует вас или ваш компьютер; это просто даёт мне знать, что InstallerClean работает и [сколько места люди освобождают].`,
  'Automation.ResultLogPreview': `Предпросмотр отчёта`,

  // Single instance / startup / crash
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean уже запущен.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `Произошла непредвиденная ошибка, и InstallerClean необходимо закрыть.\n\n{0}\n\nПодробности записаны в:\n{1}`,
  'Startup.UnhandledBody.NoLog': `Произошла непредвиденная ошибка, и InstallerClean необходимо закрыть.\n\n{0}\n\nНе удалось записать журнал сбоев.`,
  'Startup.ErrorTitle': `Ошибка запуска`,
  'Startup.FailedToStart': `Не удалось запустить ({0}). Подробности записаны в:\n{1}`,
  'Startup.FailedToStart.NoLog': `Не удалось запустить ({0}). Не удалось записать журнал сбоев.`,

  // File picker
  'FilePicker.ChooseDestinationTitle': `Выберите папку назначения для перемещённых файлов`,

  // Version display

  // 0 = major.minor.patch (e.g. "1.5.4")
  'Version.Display': `Версия {0}`,
  'Plural.File.Singular': `файл`,
  'Plural.File.Plural': `файлов`,
  'Plural.Error.Singular': `ошибка`,
  'Plural.Error.Plural': `ошибок`,
  'Plural.Package.Singular': `пакет`,
  'Plural.Package.Plural': `пакетов`,
  'Plural.Product.Singular': `продукт`,
  'Plural.Product.Plural': `продуктов`,
  'Plural.Patch.Singular': `исправление`,
  'Plural.Patch.Plural': `исправлений`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ElapsedLong.LessThanASecond': `меньше секунды`,
  'Display.ElapsedLong.Seconds': `{0:F1} секунды`,
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
  'Tooltip.ChangeLanguage': `Изменить язык. Программа перезапустится.`,
  'Automation.ChangeLanguage': `Изменить язык`,
  'Automation.ChangeLanguage.HelpText': `Программа перезапустится.`,
  'Body.NotScanned.Lead': `Пока ничего не просканировано.`,
  'Body.NotScanned.Why': `Нажмите «Повторить сканирование», чтобы просмотреть {InstallerFolder} в поисках файлов установщика, которые больше не нужны ни одной программе.`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,
  'Error.ScanCorrelationFailed': `InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed.`,
  'Error.CandidateOutsideCache': `Этот файл находится не в самой папке Windows Installer; отклонено в целях безопасности.`,
  'Completion.ReverifySkipped': `Оставлено {0} {1} на месте: после сканирования они снова понадобились программе.`,
  'Completion.MoveCancelledSummary': `Перемещено {0}/{1} {2} до отмены.`,
  'Completion.PermanentDeleteCancelledSummary': `Удалено безвозвратно {0}/{1} {2} до отмены.`,
  'Body.PendingReboot.Lead': `Эти файлы сейчас нельзя очистить.`,
  'Completion.ReverifyIncomplete': `{0} {1} kept in place, because the Windows Installer records could not be fully read in the final check.`,
  'Summary.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Summary.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Error.ScanRecordsUnreadable': `InstallerClean не смог прочитать достаточно записей Windows Installer, чтобы точно знать, что ещё нужно: список установленных программ вернулся неполным, а чтение тех же записей напрямую из реестра тоже привело к ошибкам. Файл мог выглядеть бесхозным лишь потому, что запись, которая его называет, оказалась одной из нечитаемых, поэтому InstallerClean остановился. Ничего не было удалено.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer так и не сообщил о конце списка установленных программ: InstallerClean прекратил попытки после {0} записей (последний код ошибки {1}). Списку без конца доверять нельзя, поэтому InstallerClean остановился. Ничего не было удалено.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer так и не сообщил о конце списка исправлений одной программы: InstallerClean прекратил попытки после {0} записей (последний код ошибки {1}). Списку без конца доверять нельзя, поэтому InstallerClean остановился. Ничего не было удалено.`,
  'UpdateCheck.Status.UpdateAvailable': `Доступна версия {0}.`,
  'Completion.DonateAsk': `Рад, что пригодилось. Если захочется проявить щедрость, есть куда оставить на чай.`,
  'About.Link.Guide': `Руководство и частые вопросы`,
  'About.Link.ReportProblem': `Сообщить о проблеме`,
  'About.AutoUpdateCheck': `Автоматически проверять обновления`,
  'Automation.About.Guide.HelpText': `Открывает readme на github в вашем браузере.`,
  'Automation.About.ReportProblem.HelpText': `Открывает список проблем (Issues) на github.com в вашем браузере.`,
  'Automation.AutoUpdateCheck.HelpText': `Если флажок установлен, InstallerClean при запуске проверяет на github наличие более новой версии.`,
  'Tooltip.MoveSameDrive': `Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Confirm.DeletePermanently.Plural': `Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed.`,
  'Automation.Scroll.ProductDetails': `Product details`,
  'Body.PendingReboot.Other': `Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back.`,
  'Error.InstallerLockUnavailableTitle': `Ничего не удалено`,
  'Error.InstallerLockUnavailable': `InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
  'Completion.ReverifyRecordsChanged': `{0} {1} kept in place, because the Windows Installer records had changed by the final check.`,
};

// Russian CLDR-category overrides beyond the neutral one/other split. They do NOT
// exist in the neutral resx, so they are appended as satellite-only keys, read by
// name via the ResourceManager (DisplayHelpers.Pluralise's One/Few/Many branches).
// check-resx-parity.mjs allows each because its base (the <Prefix>.Plural sibling,
// or the flat key itself) is in the neutral. The five noun fragments take the
// genitive singular (the nominative-numeral 2-4 form, e.g. "2 файла"); the
// sentence keys carry the 2-4 plural agreement (participle/predicate/pronoun) the
// Plural slot (now 5+ only, impersonal) does not; two .One overrides restore the
// singular agreement a single-item count needs (n ending in 1 but 11); and a paucal
// .Few puts the count beside the noun in a template whose flat form detaches them.
// Each value's {N} set matches its base key's set.
const OVERRIDES = {
  // The counted noun sits with {0}, which is what selects the form, so the
  // pair reaches 1 (файл) and 5+ (файлов) but not the paucal 2-4 (файла).
  'Completion.FailedCount.Few': `Не удалось переместить {0} файла из {1}.`,
  'Completion.FailedCountDelete.Few': `Не удалось удалить {0} файла из {1}.`,
  'Plural.File.Few': `файла`,
  'Plural.Package.Few': `пакета`,
  'Plural.Product.Few': `продукта`,
  'Plural.Error.Few': `ошибки`,
  'Plural.Patch.Few': `исправления`,
  'Summary.RegisteredStillUsed.Few': `{0} файла ещё нужны`,
  'Summary.OrphanedToCleanUp.Few': `{0} ненужных файла для очистки`,
  'Summary.MissingFromDisk.Few': `Отсутствует {0} зарегистрированных файла (InstallerClean их не удалял). Сейчас это не доставляет хлопот, но в будущем восстановление, обновление или удаление тех программ может не выполниться. Откройте «Подробности», чтобы узнать, что делать.`,
  // 2-4 takes the accusative-plural "установленные программы"; the base Plural
  // key carries the 5+ genitive "установленных программ".
  'Summary.ProgramsUnreadable.Few': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Summary.RegisteredWindow.Few': `{0} зарегистрированных файла ещё нужны ({1})`,
  'Completion.PermanentDeleteSummary.Few': `{0} {1} permanently deleted`,
  // ReverifySkipped's flat base carries the 2-4/5+ agreement ("оставлено ... они
  // ... понадобились"); .One restores the singular-masculine agreement a single
  // file needs ("Оставлен 1 файл ... он ... понадобился").
  'Completion.ReverifySkipped.One': `Оставлен {0} {1} на месте: после сканирования он снова понадобился программе.`,
  // As ReverifySkipped.One: the base's "Оставлено" is the impersonal form that
  // 2-4 and 5+ both take; n==1 needs the masculine singular "Оставлен".
  'Completion.ReverifyIncomplete.One': `{0} {1} kept in place, because the Windows Installer records could not be fully read in the final check.`,
  // The flat template «Найдено зарегистрированных {1}: {0}.» mis-agrees for counts
  // where the numeral is not adjacent to the noun. .One restores nominative agreement
  // for the One category (1, 21, ...): {1} is nominative singular "пакет" while the
  // baked-in adjective stays genitive plural, so it reads "Найдено 1 зарегистрированный
  // пакет." .Few does the same for 2-4, putting the count beside the noun so the
  // paucal genitive-singular noun agrees ("Найдено 2 зарегистрированных пакета.");
  // Many keeps the flat label form ("Найдено зарегистрированных пакетов: 5.").
  'Status.RegisteredPackagesFound.One': `Найдено {0} зарегистрированный {1}.`,
  'Status.RegisteredPackagesFound.Few': `Найдено {0} зарегистрированных {1}.`,
};

// The human-facing CLI keys (progress, argument/path errors, the pending-reboot
// sentences, the --help screen, the count lines). The machine-read Cli.EventLog*
// keys (every EventLog* bar EventLogUnavailable) are deliberately OMITTED: they are
// forced English at the emit site so an RMM/monitoring grep matches a fixed phrase
// regardless of OS language, so they must not appear in a satellite. Appended as a
// block before </root>, like OVERRIDES.
//
// Count lines (FoundOrphans/DeletedFiles/MovedFiles) use the impersonal verb-first
// frame (Найдено/Удалено/Перемещено), which is invariant across the CLDR One/Few/Many
// categories, so they need NO .One/.Few/.Many override: the {1} noun still inflects
// via Plural.File.* (файл/файла/файлов). This matches the GUI's own impersonal result
// summaries (Completion.DeleteSummary/Moved) and is the idiomatic Russian for a count
// report. Help screen: command tokens, flags, paths and units stay verbatim; only the
// descriptions are translated; columns are space-aligned for a monospace terminal
// (PATH -> ПУТЬ keeps the same width).
const CLI = {
  'Cli.UnknownArgument': `Error: unknown argument '{0}'`,
  'Cli.Cancelling': `Отмена...`,
  'Cli.Cancelled': `Отменено.`,
  'Cli.GenericError': `Error: unexpected failure ({0}). Details written to {1}.`,
  'Cli.GenericError.NoLog': `Error: unexpected failure ({0}). The crash log could not be written.`,
  'Cli.ScanningInstaller': `Сканирование {InstallerFolder}...`,
  'Cli.FoundOrphans': `Found {0} unneeded {1} to clean up ({2}).`,
  'Cli.DeletingFiles': `Deleting {0} unneeded {1}...`,
  'Cli.DeletedFiles': `Permanently deleted {0} unneeded {1}.`,
  'Cli.NoMoveDestination': `Ошибка: не указана папка назначения для перемещения. Используйте /m ПУТЬ. (Значение по умолчанию, заданное в графическом интерфейсе, действует только для текущего пользователя и не применяется при запуске по расписанию или от имени служебной учётной записи.)`,
  'Cli.MoveDestinationInsideInstaller': `Ошибка: папка назначения не может находиться внутри папки Windows Installer.`,
  'Cli.MoveDestinationRelative': `Ошибка: папка назначения должна быть полным путём. Получено: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning.`,
  'Cli.MovingFiles': `Moving {0} unneeded {1} to {2}...`,
  'Cli.MovedFiles': `Moved {0} unneeded {1}.`,
  'Cli.MutexBlocked': `Другой процесс InstallerClean удерживает блокировку единственного экземпляра (GUI или другой запуск CLI). Код выхода 75 (временное состояние); можно повторить попытку позже.`,
  'Cli.EventLogUnavailable': `Примечание: не удалось выполнить запись в журнал событий. Проверьте разрешения журнала «Приложение» или групповую политику.`,
  // Help screen (column-aligned for a monospace terminal; tokens/flags/paths verbatim)
  'Cli.Help.Header': `InstallerClean - очистка {InstallerFolder}`,
  'Cli.Help.Usage': `Использование:`,
  'Cli.Help.Help': `  installerclean-cli --help       Показать эту справку (также /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version    Показать версию (также -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Move to the saved backup folder`,
  'Cli.Help.MovePath': `  installerclean-cli /m ПУТЬ      Переместить в указанный путь`,
  'Cli.Help.NoteLine1': `installerclean-cli blocks the prompt until it finishes, so a script or&#10;scheduled task can wait on it.`,
  'Cli.Help.ExitCodesHeader': `Коды выхода:`,
  'Cli.Help.ExitCodeOk': `  0   success: the run finished with nothing left to do`,
  'Cli.Help.ExitCodeError': `  1   failure: nothing processed (bad arguments, a bad destination, a&#10;       failed scan or every file failed)`,
  'Cli.Help.ExitCodePartial': `  2   partial: some processed, some not (a failure or a Ctrl+C part way)`,
  'Cli.Help.ExitCodeTransient': `  75  временно: запуск заблокирован временным состоянием (см. сообщение)`,
  'Cli.Help.ExitCodeCancelled': `  130 отменено (Ctrl+C)`,
  'Cli.TooManyArguments': `Ошибка: неожиданный лишний аргумент «{0}». Если в пути к папке для перемещения есть пробел, возьмите весь путь в кавычки: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `That folder is saved per-user; scheduled or SYSTEM runs need /m PATH.`,
  'Cli.TooManyArgumentsNoPath': `Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run.`,
  'Cli.MissingFromDisk.Singular': `{0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it.`,
  'Cli.MissingFromDisk.Plural': `{0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them.`,
  'Cli.MoveNotEnoughSpace': `Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.Other': `Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes.`,
  'Cli.FoundNoOrphans': `Found no unneeded files.`,
  'Cli.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again.`,
  'Cli.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Cli.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Cli.Help.Summary': `Removes cached .msi and .msp files that no installed program still needs.`,
  'Cli.Help.Elevation': `Needs an elevated (administrator) prompt; Windows will not start it.`,
  'Cli.InstallerLockUnavailable': `Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove every Cli.* <data> element BY NAME: one per-element match, non-greedy
// to its own </data>, so it works regardless of how the Cli keys are grouped.
let cliRemoved = 0;
text = text.replace(/[^\S\n]*<data name="Cli\.[^"]*"[\s\S]*?<\/data>\n?/g, () => { cliRemoved++; return ''; });

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

// Append the satellite-only CLDR-override <data> elements before </root>. Their
// values carry no XML-special characters, so they are written verbatim like the MAP.
const overrideBlock = '\n  <!-- Russian CLDR-category overrides (.One/.Few); satellite-only, read by name via ResourceManager -->\n' +
  Object.entries(OVERRIDES).map(([k, v]) => `  <data name="${k}" xml:space="preserve"><value>${v}</value></data>`).join('\n') + '\n';
text = text.replace('</root>', overrideBlock + '</root>');

// Append the human CLI keys before </root>, same verbatim emission as OVERRIDES
// (no CLI value carries an XML-special character: guillemets not <>, no &). The
// machine Cli.EventLog* keys are not in CLI, so they stay out of the satellite.
const cliBlock = '\n  <!-- Human-facing CLI keys; the machine Cli.EventLog* keys are forced English at the emit site and omitted -->\n' +
  Object.entries(CLI).map(([k, v]) => `  <data name="${k}" xml:space="preserve"><value>${v}</value></data>`).join('\n') + '\n';
text = text.replace('</root>', cliBlock + '</root>');

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
const neutralNonCli = [...neutral.keys()].filter((k) => !k.startsWith('Cli.'));

const missingFromMap = neutralNonCli.filter((k) => !(k in MAP));
const strayMapKeys = Object.keys(MAP).filter((k) => !neutral.has(k));
const missingFromOutput = neutralNonCli.filter((k) => !output.has(k));
const arityMismatch = neutralNonCli.filter((k) => {
  if (!output.has(k)) return false; // already counted by missingFromOutput
  const a = placeholders(neutral.get(k)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});
const crlf = (written.match(/\r/g) || []).length;

// Untranslated-phrase gate (KEY-based, HARD): a value still byte-identical to the
// English neutral is a miss, UNLESS its key is a universal keep or in ALSO_KEEP.
const alsoKeep = new Set(ALSO_KEEP);
const untranslated = neutralNonCli.filter((k) =>
  output.has(k) && output.get(k) === neutral.get(k) && !KEEP_ENGLISH.has(k) && !alsoKeep.has(k));

// The output carries the neutral non-Cli keys PLUS the satellite-only override keys
// (.One/.Few). Each must be present and share its base key's placeholder set, where
// the base is the <Prefix>.Plural sibling if it exists, else the flat key itself
// (mirroring check-resx-parity.mjs's base resolution).
const overrideKeys = Object.keys(OVERRIDES);
const overrideMissing = overrideKeys.filter((k) => !output.has(k));
const overrideArityMismatch = overrideKeys.filter((k) => {
  if (!output.has(k)) return true;
  const base = k.replace(/\.(?:One|Few|Many)$/, '');
  const ref = neutral.has(`${base}.Plural`) ? `${base}.Plural` : base;
  if (!neutral.has(ref)) return true; // base must exist in the neutral (the parity precondition)
  const a = placeholders(neutral.get(ref)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});

// CLI surface: the satellite ships the human Cli.* keys and OMITS the machine
// Cli.EventLog* keys (every EventLog* bar EventLogUnavailable), which stay English at
// the emit site for RMM greps. Mirror check-resx-parity.mjs's split. The CLI map must
// hold exactly the human keys (no machine key, no stray), every human key must reach
// the output, no machine key may leak in, each must match its neutral arity, and none
// may sit byte-identical to the English (an untranslated miss).
const isMachineCli = (k) => k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
const isHumanCli = (k) => k.startsWith('Cli.') && !isMachineCli(k);
const neutralHumanCli = [...neutral.keys()].filter(isHumanCli);
// Derived, never pinned: this file strips every Cli.* element and re-adds the
// human ones from CLI, so what it removes is however many the neutral holds.
// A literal here goes stale the moment the command line gains a string, and it
// asserts nothing about what was actually stripped.
const cliExpected = [...neutral.keys()].filter((k) => k.startsWith('Cli.')).length;
const cliKeys = Object.keys(CLI);
const cliStrayMap = cliKeys.filter((k) => !neutral.has(k) || isMachineCli(k));
const cliMissingFromMap = neutralHumanCli.filter((k) => !(k in CLI));
const cliMissingFromOutput = neutralHumanCli.filter((k) => !output.has(k));
const cliMachineLeaked = [...output.keys()].filter(isMachineCli);

// The one human-facing Cli.EventLog* key, asserted present rather than left to
// the counts: a predicate that stopped discriminating it takes it out of the
// output AND out of the required set, so every figure above still agrees. The
// MAP substitution notices today only through the order the two run in.
const humanCliStripped = !output.has('Cli.EventLogUnavailable');
const cliArityMismatch = cliKeys.filter((k) => {
  if (!output.has(k) || !neutral.has(k)) return false; // counted by the stray/missing checks
  const a = placeholders(neutral.get(k)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});
const cliUntranslated = neutralHumanCli.filter((k) => output.has(k) && output.get(k) === neutral.get(k));

console.log('<data> in output:', output.size, '(expect', (neutralNonCli.length + overrideKeys.length + cliKeys.length) + ')');
console.log('Cli <data> removed:', cliRemoved, `(expect ${cliExpected})`);
console.log('MAP entries:', Object.keys(MAP).length, '| override keys:', overrideKeys.length, '| human Cli keys:', cliKeys.length, `(expect ${neutralHumanCli.length}) | CRLF:`, crlf, '(expect 0)');

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
if (missingFromOutput.length) console.log('!! neutral key missing from output:', missingFromOutput);
if (arityMismatch.length) console.log('!! placeholder arity differs from neutral:', arityMismatch);
if (overrideMissing.length) console.log('!! override key missing from output:', overrideMissing);
if (overrideArityMismatch.length) console.log('!! override arity differs from its base key:', overrideArityMismatch);
if (cliStrayMap.length) console.log('!! CLI map key not a human neutral key:', cliStrayMap);
if (cliMissingFromMap.length) console.log('!! human Cli key missing from CLI map:', cliMissingFromMap);
if (cliMissingFromOutput.length) console.log('!! human Cli key missing from output:', cliMissingFromOutput);
if (cliMachineLeaked.length) console.log('!! machine Cli.EventLog* key leaked into output:', cliMachineLeaked);
if (humanCliStripped) console.log('!! Cli.EventLogUnavailable stripped: that key is human-facing and must stay');
if (cliArityMismatch.length) console.log('!! Cli placeholder arity differs from neutral:', cliArityMismatch);
if (cliUntranslated.length) console.log('!! Cli key still English (untranslated):', cliUntranslated);
if (untranslated.length) {
  const show = untranslated.slice(0, 40).join(', ');
  console.log('!! still English (untranslated), ' + untranslated.length + ': ' + show +
    (untranslated.length > 40 ? ', ...and ' + (untranslated.length - 40) + ' more' : ''));
  if (untranslated.length > 50)
    console.log('   (that is most of the file: this is the untranslated template. Translate the MAP values, then a real miss is listed on its own.)');
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length &&
  !overrideMissing.length && !overrideArityMismatch.length &&
  !cliStrayMap.length && !cliMissingFromMap.length && !cliMissingFromOutput.length &&
  !cliMachineLeaked.length && !humanCliStripped &&
  !cliArityMismatch.length && !cliUntranslated.length &&
  output.size === neutralNonCli.length + overrideKeys.length + cliKeys.length &&
  cliRemoved === cliExpected && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
