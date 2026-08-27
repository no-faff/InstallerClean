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

// Universal keeps: keys whose value is the same in every language, the brand names
// and the pure-placeholder announcement string. Their still-English value is NOT a
// miss. Explicit by KEY on purpose: a future brand key then defaults to "flag until
// someone adds it here", never silently passes. Do NOT translate these values. Do
// NOT edit this list per language.
//
// The four size suffixes and the two elapsed suffixes were in this list until
// 2026-08-26 and do not belong in it, because they are not universal: French writes
// Go/Mo/Ko/o, Russian and Ukrainian write ГБ/МБ/КБ/Б and мс/с. Those three carry real
// values in their MAP; the languages that do abbreviate as English does keep them in
// ALSO_KEEP, which is the per-language list. Display.ListSeparator is the same shape,
// for the same reason.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
]);

// Per-language keeps: Russian has a native rendering for every translatable
// token (patch -> исправление, the term Microsoft's own Russian uses for an
// .msp), so nothing beyond KEEP_ENGLISH stays English.
const ALSO_KEEP = [
  // The list separator Russian uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
];

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `О программе`,
  'Window.Registered.Title': `Файлы, оставленные без изменений`,
  'Window.Orphaned.Title': `Ненужные файлы, которые можно безопасно удалить`,

  // Section headings
  // Section.Registered.Products and Automation.Section.Products were removed from
  // this map on 2026-08-21. They left the neutral resx at f49b795b, when the
  // registered-files window stopped having a products group of its own, and stayed
  // here and in all fifteen satellites, so every round regenerated two keys the app
  // cannot use and check-resx-parity reported them as strays in every language.
  'Section.Registered.Patches': `ИСПРАВЛЕНИЯ`,
  'Section.Registered.Details': `СВЕДЕНИЯ О ПРОДУКТЕ`,
  'Section.Backup.Folder': `ПАПКА РЕЗЕРВНЫХ КОПИЙ`,
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
  'Action.BackupFolderPlaceholder': `Путь к папке, если вы перемещаете, а не удаляете.`,
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
  // The three Cancel names are built on the noun Отмена rather than the verb
  // Отменить, which share only a root: WCAG 2.5.3 (Label in Name) asks that a
  // control's spoken name contain the word drawn on it, so speech input can
  // reach the control by what the user sees. Отмена is what Windows Installer's
  // own Russian puts on that button (msimsg.dll string 18, en-US "Cancel").
  // The alignment is about the WORD and nothing else. Casing is decided
  // separately, by ear, and Automation.CheckForUpdates.HelpText holds the
  // reasoning for the lower-case github that looks like a slip and is not.
  'Automation.CancelOperation': `Отмена операции`,
  'Automation.CancelScan': `Отмена сканирования`,
  'Automation.CancelStartupScan': `Отмена сканирования при запуске`,
  'Automation.Close': `Закрыть`,
  'Automation.CloseWindow': `Закрыть окно`,
  'Automation.CloseResult': `Закрыть результат и вернуться в главное окно`,
  'Automation.LeaveStarOnGitHub.About': `Поставить звезду на github`,
  'Automation.Minimise': `Свернуть`,
  'Automation.ConfirmDelete': `«Удалить безвозвратно» убирает ненужные файлы. «Отмена» закрывает окно, ничего не удаляя.`,
  'Automation.ConfirmMove': `«Переместить» помещает ненужные файлы в выбранную папку назначения. «Отмена» оставляет их на месте.`,
  'Automation.SayThanks': `Поблагодарить`,
  'Automation.ConfirmSendResultLog': `«Отправить» передаёт показанный отчёт в No Faff. «Отмена» не отправляет ничего.`,
  'Automation.CheckForUpdates': `Проверить обновления`,
  'Automation.CheckForUpdates.HelpText': `Проверяет на странице выпусков github, есть ли более новая версия.`,
  'Automation.UpdateAvailable.HelpText': `Откройте страницу выпуска, чтобы скачать более новую версию, или нажмите «Отмена», чтобы оставить текущую.`,
  'Automation.Licence.HelpText': `Открывает файл лицензии на github.com в вашем браузере.`,
  'Automation.Section.BackupFolder': `Папка резервных копий`,
  'Automation.Section.Patches': `Исправления`,
  'Automation.Section.ProductDetails': `Сведения о продукте`,
  'Automation.BackupFolder': `Папка резервных копий`,
  'Automation.OperationProgress': `Ход операции`,
  'Automation.RescanInstaller': `Сканировать {InstallerFolder} заново`,
  'Automation.ScanningProgress': `Ход сканирования`,
  'Automation.StartupScanProgress': `Ход сканирования при запуске`,
  'Automation.ViewOrphanedFiles': `Подробности, ненужные файлы`,
  'Automation.ViewOrphanedFiles.HelpText': `Доступны для очистки.`,
  'Automation.ViewRegisteredFiles': `Подробности, файлы, оставленные без изменений`,
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
  'Tooltip.Move': `Перемещает ненужные файлы в папку резервных копий. Удалите эту папку, когда убедитесь, что они никому не нужны.`,
  'Tooltip.MoveNeedsDestination': `Перемещает ненужные файлы в папку резервных копий. Вы выберете её следующим шагом. Удалите эту папку, когда убедитесь, что они никому не нужны.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to delete, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Имя субъекта из встроенного сертификата Authenticode. Цепочка не проверялась.`,

  // Body copy
  'Body.MainExplanation.Lead': `Любые ненужные файлы ниже [можно безопасно удалить].`,
  'Body.MainExplanation.Why': `Они лежат в {InstallerFolder}. InstallerClean спрашивает Windows о каждой установленной программе: файл попадает в список, когда его не заявляет ни одна программа ({0}) или когда его заменило более новое исправление и ни одна программа не смогла бы к нему откатиться ({1}).`,
  'Body.MainExplanation.Action': `Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. If anything does go wrong, put them back into {InstallerFolder} under the names they had. Or delete them permanently now.`,
  'Body.PendingReboot.MsiExecuteMutex': `Сейчас что-то использует Windows Installer, например обновление Windows или программа, устанавливающаяся в фоне. «Переместить» и «Удалить» приостановлены на это время, чтобы InstallerClean не трогал {InstallerFolder}, пока она меняется. Когда всё закончится, повторите сканирование, и они вернутся.`,
  'Body.PendingReboot.InstallerInProgress': `На этом компьютере приостановлена предыдущая транзакция Windows Installer. Возобновите или откатите эту установку (либо перезагрузите Windows), прежде чем очищать {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows поставил в очередь на следующую перезагрузку переименование файла, которое затрагивает {InstallerFolder}. Перезагрузите Windows, прежде чем очищать.`,
  'Body.NoFileSelected': `Выберите файл, чтобы посмотреть сведения.`,
  'Body.NoProductSelected': `Выберите продукт, чтобы посмотреть сведения.`,
  'Body.NoMetadata': `Метаданные недоступны.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.\n\nTo put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it.`,
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
  'Status.Moving': `Перемещение ненужных файлов...`,
  'Status.Deleting': `Удаление ненужных файлов...`,
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
  'Completion.PermanentDeleteSummary.Singular': `Безвозвратно удалён {0} {1}`,
  'Completion.PermanentDeleteSummary.Plural': `Безвозвратно удалено {0} {1}`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} файл оставлен без изменений`,
  'Summary.RegisteredStillUsed.Plural': `{0} файлов оставлено без изменений`,
  'Summary.OrphanedToCleanUp.Singular': `{0} ненужный файл для очистки`,
  'Summary.OrphanedToCleanUp.Plural': `{0} ненужных файлов для очистки`,
  'Summary.NothingListed.Singular': `На этом ПК InstallerClean не смог с уверенностью определить, каким из установленных здесь программ принадлежат файлы в кэше, поэтому удержал единственный файл, а не показал его в списке.`,
  'Summary.NothingListed.Plural': `На этом ПК InstallerClean не смог с уверенностью определить, каким из установленных здесь программ принадлежат файлы в кэше, поэтому удержал {0} {1}, а не показал их в списке.`,
  'Summary.MissingFromDisk.Singular': `Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do.`,
  'Summary.MissingFromDisk.Plural': `Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `ещё {0} программа`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `ещё {0} программ`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} файл, для которого в записях не названа программа`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} файлов, для которых в записях не названа программа`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0}/{1} {2}`,

  // OrphanedWindow: by-cause tally. 0/1/2 = orphaned/superseded/obsoleted counts,
  // 3 = size. Reason.* stems in the genitive-plural elliptical form (invariant across
  // counts, so one flat form serves all). RegisteredWindow split into Singular/Plural
  // (0 = count, 1 = size); Russian adds .Few in OVERRIDES, and .Plural serves CLDR
  // Many (ru integers never resolve to Other), so no .Many key, matching the GUI's
  // Summary.RegisteredStillUsed shape.
  'Summary.OrphanedWindow': `{0} {1} для очистки ({2})`,
  'Summary.RegisteredWindow.Singular': `{0} файл оставлен без изменений ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} файлов оставлено без изменений ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Переместить {0} {1} ({2})?`,

  // 0 = destination path
  'Confirm.MoveDestination': `Переместить в:`,
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
  'Error.DestinationInSystemFolder': `Назначение {0} разрешается внутри системной папки Windows. Выберите путь вне %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% и %ProgramData%.`,
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
  'Error.FileInUse.Singular': `Этот файл открыт или заблокирован другой программой, поэтому сейчас его ничем не убрать. Он оставлен на месте; попробуйте позже.`,
  'Error.FileInUse.Plural': `Эти файлы открыты или заблокированы другой программой, поэтому сейчас их ничем не убрать. Они оставлены на месте; попробуйте позже.`,
  'Error.IOFailure.Singular': `Windows сообщил об ошибке файла; файл оставлен на месте.`,
  'Error.IOFailure.Plural': `Windows сообщил об ошибках файлов; эти файлы оставлены на месте.`,
  'Error.UnknownError.Singular': `С этим файлом что-то пошло не так; он оставлен на месте.`,
  'Error.UnknownError.Plural': `С этими файлами что-то пошло не так; они оставлены на месте.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Перемещение файлов в папку Windows Installer отклонено (назначение: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `Папка резервных копий должна быть полным путём к папке, начинающимся с буквы диска или сетевого ресурса (например, D:\\Backup или \\\\server\\backup). InstallerClean не может использовать этот: {0}`,
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
  'Error.DestinationChangedMidBatch': `InstallerClean больше не смог подтвердить папку резервных копий и остановился, чтобы не записать не туда. Проверьте {0}, затем «Повторить сканирование» и попробуйте снова.`,
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
  'Display.Size.GB': `{0:F2} ГБ`,
  'Display.Size.MB': `{0:F1} МБ`,
  'Display.Size.KB': `{0:F1} КБ`,
  'Display.Size.B': `{0} Б`,
  'Display.Elapsed.Ms': `{0:F0} мс`,
  'Display.Elapsed.S': `{0:F1} с`,
  'Display.ListSeparator': `, `,
  'Display.ElapsedLong.LessThanASecond': `меньше секунды`,
  'Display.ElapsedLong.Seconds': `{0:F1} секунды`,
  'CrashLog.PrivacyHeader': `# crash.log собирает необработанные исключения InstallerClean.\n# При повышенных правах сообщения исключений платформы могут\n# содержать пути к файлам текущего сеанса (в том числе профили\n# других пользователей, перечисленные запросами Windows Installer).\n# Сообщения о сетевых сбоях при проверке обновлений или отправке\n# журнала результатов могут содержать URL назначения и разрешённый\n# IP-адрес или адрес прокси. Записи о нечитаемых записях Windows\n# Installer могут содержать SID учётной записи Windows\n# (S-1-5-21-...) и коды продуктов установленного ПО.\n# Удалите все три вида сведений, прежде чем прикладывать этот файл\n# к публичному сообщению об ошибке.\n`,
  'Tooltip.ChangeLanguage': `Изменить язык. Программа перезапустится.`,
  'Automation.ChangeLanguage': `Изменить язык`,
  'Automation.ChangeLanguage.HelpText': `Программа перезапустится.`,
  'Body.NotScanned.Lead': `Пока ничего не просканировано.`,
  'Body.NotScanned.Why': `Нажмите «Повторить сканирование», чтобы просмотреть {InstallerFolder} в поисках файлов установщика, которые больше не нужны ни одной программе.`,
  'Confirm.MoveSameDrive': `Эта папка на том же диске, поэтому место не вернётся, пока вы её не удалите. Выберите папку на другом диске, если хотите получить место сразу.`,
  'Error.ScanCorrelationFailed': `InstallerClean не смог сопоставить записи Windows Installer с содержимым {InstallerFolder}. Почти ничего из того, на что указывают записи, там нет, и почти ничто из того, что там есть, не названо ни одной записью, поэтому ни про один файл не удалось показать, что он не нужен. Ничего не предложено и ничего не убрано.`,
  'Error.CandidateOutsideCache': `Этот файл находится не в самой папке Windows Installer; отклонено в целях безопасности.`,
  'Completion.MoveCancelledSummary': `Перемещено {0}/{1} {2} до отмены.`,
  'Completion.PermanentDeleteCancelledSummary': `Удалено безвозвратно {0}/{1} {2} до отмены.`,
  'Body.PendingReboot.Lead': `Эти файлы сейчас нельзя очистить.`,
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
  'Tooltip.MoveSameDrive': `Перемещает ненужные файлы в папку резервных копий. Она на том же диске, поэтому место вернётся только после того, как вы удалите эту папку или переместите её на другой диск. Это можно сделать, когда убедитесь, что они никому не нужны.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to delete], so remove the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to delete], so remove it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to delete], so remove the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to delete], so remove it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `Этот файл будет удалён безвозвратно. Его [можно безопасно удалить], но если хотите резервную копию, воспользуйтесь кнопкой «Переместить».`,
  'Confirm.DeletePermanently.Plural': `These files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean не смог получить от Windows настоящий путь к {InstallerFolder}, поэтому ни про один файл не удалось показать, что он находится внутри, и ни один не был предложен для очистки. Это сканирование ничего не нашло из-за неудачи этой проверки, а не потому, что папка чиста. Ничего не убрано.`,
  'Automation.Scroll.ProductDetails': `Сведения о продукте`,
  'Body.PendingReboot.Other': `У Windows Installer что-то выполняется, поэтому «Переместить» и «Удалить» приостановлены. InstallerClean не будет трогать {InstallerFolder}, пока она меняется. Когда всё закончится, повторите сканирование, и они вернутся.`,
  'Error.InstallerLockUnavailableTitle': `Ничего не удалено`,
  'Error.MoveInstallerLockUnavailableTitle': `Ничего не перемещено`,
  'Error.InstallerLockUnavailable': `InstallerClean не смог взять блокировку, которой Windows Installer не даёт двум программам одновременно менять установленное ПО, поэтому не смог исключить, что файл понадобится на полпути, и ничего не удалено. Попробуйте ещё раз, а если повторяется — перезагрузите Windows.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean не смог взять блокировку, которой Windows Installer не даёт двум программам одновременно менять установленное ПО, поэтому не смог исключить, что файл понадобится на полпути, и ничего не перемещено. Попробуйте ещё раз, а если повторяется — перезагрузите Windows.`,
  'Completion.ReverifyIdentityClaimed': `Оставлено на месте {0} {1}, потому что у Windows есть запись о программе, названной внутри.`,
  'Completion.ReverifyIdentityUnreadable': `Оставлено на месте {0} {1}, потому что InstallerClean не нашёл внутри названия программы.`,
  'Completion.NothingRemoved': `Ничего не убрано`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean не смог сопоставить записи Windows Installer с содержимым {InstallerFolder}. В папке есть файлы, но ни одна запись не указывает ни на что внутри неё, поэтому ни про один файл не удалось показать, что он не нужен. Ничего не предложено и ничего не убрано.`,
  'Completion.NothingOffered': `На этом ПК ничего не предложено`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean не смог с уверенностью определить, каким из установленных здесь программ принадлежат файлы в кэше, поэтому удержал единственный файл ({2}), который иначе предложил бы.`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean не смог с уверенностью определить, каким из установленных здесь программ принадлежат файлы в кэше, поэтому удержал все {0} {1} ({2}), которые иначе предложил бы.`,
  'Summary.SupersededHeldBack.Singular': `On this PC InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back.`,
  'Summary.SupersededHeldBack.Plural': `On this PC InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back.`,
  'Completion.HeldBack.Singular': `{0} file held back. The scan said it was unneeded. The final check didn't agree.`,
  'Completion.HeldBack.Plural': `{0} files held back. The scan said these were unneeded. The final check didn't agree.`,
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
  'Summary.RegisteredStillUsed.Few': `{0} файла оставлено без изменений`,
  'Summary.OrphanedToCleanUp.Few': `{0} ненужных файла для очистки`,
  // Summary.MissingFromDisk.Few was removed in the 3.0.0 round. After the
  // preposition the noun takes the prepositional plural at 2-4 and at 5+ alike
  // ("о 2 файлах", "о 5 файлах"), so the paucal form is the base form and the
  // override said the same thing twice. It had been holding a superseded English
  // sentence, which neither arm of the still-English gate can see: not equal to the
  // current neutral, and its base not equal either.
  // 2-4 takes the accusative-plural "установленные программы"; the base Plural
  // key carries the 5+ genitive "установленных программ".
  'Summary.RegisteredWindow.Few': `{0} файла оставлено без изменений ({1})`,
  // Completion.PermanentDeleteSummary.Few was removed in the 3.0.0 round. The
  // participle is impersonal ("удалено") and does not move between the paucal and
  // the 5+ band, and the noun inflects through Plural.File, so the .Few form came
  // out byte-identical to the base and was the same sentence twice. An absent
  // override falls back to the base, which is the form wanted here.
  // Completion.ReverifyIdentityUnreadable.One was added and removed again in the 3.0.0 round. Its base is
  // one of the two retired identity causes: no code reads it, so nothing passes
  // the prefix to Pluralise and the override could never be selected.
  // CountedStringTests.Every_satellite_override_belongs_to_a_counted_prefix is
  // what says so. The base string itself stays translated, which is the point of
  // keeping those two keys at all.
  // The flat template «Найдено зарегистрированных {1}: {0}.» mis-agrees for counts
  // where the numeral is not adjacent to the noun. .One restores nominative agreement
  // for the One category (1, 21, ...): {1} is nominative singular "пакет" while the
  // baked-in adjective stays genitive plural, so it reads "Найдено 1 зарегистрированный
  // пакет." .Few does the same for 2-4, putting the count beside the noun so the
  // paucal genitive-singular noun agrees ("Найдено 2 зарегистрированных пакета.");
  // Many keeps the flat label form ("Найдено зарегистрированных пакетов: 5.").
  'Status.RegisteredPackagesFound.One': `Найдено {0} зарегистрированный {1}.`,
  'Summary.MissingFromDisk.Unnamed.Few': `{0} файла, для которых в записях не названа программа`,
  'Summary.MissingFromDisk.OtherPrograms.Few': `ещё {0} программы`,
  'Cli.FoundOrphans.One': `Найден {0} ненужный {1} для очистки ({2}).`,
  'Cli.DeletingFiles.One': `Идёт удаление: {0} ненужный {1}...`,
  'Cli.DeletedFiles.One': `Безвозвратно удалён {0} ненужный {1}.`,
  'Cli.MovingFiles.One': `Идёт перемещение в {2}: {0} ненужный {1}...`,
  'Cli.MovedFiles.One': `Перемещён {0} ненужный {1}.`,
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
  'Cli.UnknownArgument': `Ошибка: неизвестный аргумент «{0}»`,
  'Cli.Cancelling': `Отмена...`,
  'Cli.Cancelled': `Отменено.`,
  'Cli.GenericError': `Ошибка: непредвиденный сбой ({0}). Подробности записаны в {1}.`,
  'Cli.GenericError.NoLog': `Ошибка: непредвиденный сбой ({0}). Журнал сбоя записать не удалось.`,
  'Cli.ScanningInstaller': `Сканирование {InstallerFolder}...`,
  'Cli.FoundOrphans': `Найдено {0} ненужных {1} для очистки ({2}).`,
  'Cli.DeletingFiles': `Идёт удаление: {0} ненужных {1}...`,
  'Cli.DeletedFiles': `Безвозвратно удалено {0} ненужных {1}.`,
  'Cli.NoMoveDestination': `Ошибка: не указана папка назначения для перемещения. Используйте /m ПУТЬ. (Значение по умолчанию, заданное в графическом интерфейсе, действует только для текущего пользователя и не применяется при запуске по расписанию или от имени служебной учётной записи.)`,
  'Cli.MoveDestinationInsideInstaller': `Ошибка: папка назначения не может находиться внутри папки Windows Installer.`,
  'Cli.MoveDestinationRelative': `Ошибка: папка назначения должна быть полным путём. Получено: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Ошибка: назначение {0} разрешается внутри системной папки Windows. Выберите путь вне %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% и %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Ошибка: сейчас что-то использует Windows Installer, например обновление Windows или программа, устанавливающаяся в фоне. /m и /d заблокированы на это время. Попробуйте снова, когда всё закончится.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Ошибка: на этом компьютере приостановлена предыдущая транзакция Windows Installer. Возобновите или откатите эту установку (либо перезагрузите Windows), прежде чем очищать {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Ошибка: поставленная в очередь после перезагрузки операция с файлом затрагивает {InstallerFolder} ({0}). Перезагрузите Windows, чтобы завершить эту операцию, прежде чем очищать.`,
  'Cli.MovingFiles': `Идёт перемещение в {2}: {0} ненужных {1}...`,
  'Cli.MovedFiles': `Перемещено {0} ненужных {1}.`,
  'Cli.MutexBlocked': `Другой процесс InstallerClean удерживает блокировку единственного экземпляра (GUI или другой запуск CLI). Код выхода 75 (временное состояние); можно повторить попытку позже.`,
  'Cli.EventLogUnavailable': `Примечание: не удалось выполнить запись в журнал событий. Проверьте разрешения журнала «Приложение» или групповую политику.`,
  // Help screen (column-aligned for a monospace terminal; tokens/flags/paths verbatim)
  'Cli.Help.Header': `InstallerClean - очистка {InstallerFolder}`,
  'Cli.Help.Usage': `Использование:`,
  'Cli.Help.Help': `  installerclean-cli --help       Показать эту справку (также /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version    Показать версию (также -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s           Только сканировать - список ненужных`,
  'Cli.Help.Delete': `  installerclean-cli /d           Безвозвратно удалить ненужные файлы`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m           Переместить в сохранённую папку`,
  'Cli.Help.MovePath': `  installerclean-cli /m ПУТЬ      Переместить в указанный путь`,
  'Cli.Help.NoteLine1': `installerclean-cli удерживает командную строку до конца работы, чтобы&#10;скрипт или запланированная задача могли его дождаться.`,
  'Cli.Help.ExitCodesHeader': `Коды выхода:`,
  'Cli.Help.ExitCodeOk': `  0   успех: запуск сделал то, о чём просили, и ничего не сбоило`,
  'Cli.Help.ExitCodeError': `  1   сбой: ничего не обработано (неверные аргументы или&#10;       назначение, неудачное сканирование или все файлы с ошибкой)`,
  'Cli.Help.ExitCodePartial': `  2   частично: часть обработана, часть нет (сбой или Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  временно: запуск заблокирован временным состоянием (см. сообщение)`,
  'Cli.Help.ExitCodeCancelled': `  130 отменено (Ctrl+C)`,
  'Cli.TooManyArguments': `Ошибка: неожиданный лишний аргумент «{0}». Если в пути к папке назначения есть пробел, возьмите весь путь в кавычки: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Папка своя у каждого пользователя; запланированным и SYSTEM: /m ПУТЬ.`,
  'Cli.TooManyArgumentsNoPath': `Ошибка: непредвиденный лишний аргумент «{0}». /s и /d не принимают других аргументов, и за один запуск можно использовать только один ключ.`,
  'Cli.MissingFromDisk.Singular': `Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This usually restores the file, but Microsoft doesn't guarantee it.`,
  'Cli.MissingFromDisk.Plural': `Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This usually restores the file, but Microsoft doesn't guarantee it.`,
  'Cli.MoveNotEnoughSpace': `Ошибка: недостаточно места в {0}. Для перемещения этих файлов нужно {1}, а свободно {2}. Ничего не перемещено.`,
  'Cli.PendingRebootBlocked.Other': `Ошибка: у Windows Installer что-то выполняется, поэтому /m и /d заблокированы. InstallerClean не будет трогать {InstallerFolder}, пока она меняется. Попробуйте снова, когда всё закончится.`,
  'Cli.FoundNoOrphans': `Ненужных файлов не найдено.`,
  // Added 2026-08-24 by the translation round, holding the English while the
  // wording settles. Russian's command-line block is a separate object, so a
  // key added to the neutral reaches the other fourteen MAPs and never this
  // one, and the key goes missing from the Russian resx rather than merely
  // untranslated. Translate both when the English is ruled.
  'Cli.NothingOffered.Singular': `InstallerClean не смог с уверенностью определить, каким из установленных здесь программ принадлежат файлы в кэше, поэтому удержал единственный файл ({2}), который иначе предложил бы.`,
  'Cli.NothingOffered.Plural': `InstallerClean не смог с уверенностью определить, каким из установленных здесь программ принадлежат файлы в кэше, поэтому удержал все {0} {1} ({2}), которые иначе предложил бы.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean больше не смог подтвердить папку резервных копий и остановился, чтобы не записать не туда. Проверьте {0}, затем запустите команду снова.`,
  'Cli.Help.Summary': `Убирает .msi и .msp из кэша, не нужные ни одной установленной программе.`,
  'Cli.Help.Elevation': `Нужна командная строка администратора; иначе Windows её не запустит.`,
  'Cli.InstallerLockUnavailable': `Ошибка: InstallerClean не смог взять блокировку Windows Installer, которая не даёт двум программам одновременно менять установленное ПО, поэтому не смог исключить, что файл понадобится на полпути. Ничего не удалено. Попробуйте ещё раз, а если повторяется — перезагрузите Windows.`,
  'Cli.MoveInstallerLockUnavailable': `Ошибка: InstallerClean не смог взять блокировку Windows Installer, которая не даёт двум программам одновременно менять установленное ПО, поэтому не смог исключить, что файл понадобится на полпути. Ничего не перемещено. Попробуйте ещё раз, а если повторяется — перезагрузите Windows.`,
  'Cli.SupersededHeldBack.Singular': `On this PC InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back.`,
  'Cli.SupersededHeldBack.Plural': `On this PC InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back.`,
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
