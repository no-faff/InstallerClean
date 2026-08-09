# InstallerClean in Русский (Russian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Russian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Russian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.ru.resx`](../../src/InstallerClean.Core/Resources/Strings.ru.resx), so do not edit it by hand. The Russian translation itself lives in [`gen-strings-ru.mjs`](../../scripts/translations/gen-strings-ru.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Русский |
| --- | --- |
| InstallerClean | InstallerClean |
| About | О программе |
| Registered files that should not be deleted | Зарегистрированные файлы, которые не стоит удалять |
| Unneeded files that are safe to delete | Ненужные файлы, которые можно безопасно удалить |

## Section headings

| English | Русский |
| --- | --- |
| PRODUCTS | ПРОДУКТЫ |
| PATCHES | ИСПРАВЛЕНИЯ |
| PRODUCT DETAILS | СВЕДЕНИЯ О ПРОДУКТЕ |
| BACKUP FOLDER | BACKUP FOLDER |
| SAY THANKS | ПОБЛАГОДАРИТЬ |

## Buttons and actions

| English | Русский |
| --- | --- |
| _About | О п_рограмме |
| Copy | Копировать |
| Cut | Вырезать |
| Paste | Вставить |
| Select all | Выделить всё |
| _Browse... | _Обзор... |
| _Cancel | _Отмена |
| Check for _updates | Проверить о_бновления |
| _Close | _Закрыть |
| _Delete permanently | _Удалить безвозвратно |
| _Done | _Готово |
| Details | Подробности |
| _Buy me a cuppa | _Угостите меня чаем |
| Leave a _star on GitHub | Поставить з_везду на GitHub |
| Apache 2.0 licence | Лицензия Apache 2.0 |
| _Move | _Переместить |
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
| Open _release page | Открыть страницу _выпуска |
| _Re-scan | По_вторить сканирование |
| _Scan again | _Сканировать снова |
| Send report | Отправить отчёт |
| _Send | От_править |

## About window

| English | Русский |
| --- | --- |
| Guide and FAQ | Руководство и частые вопросы |
| Report a problem | Сообщить о проблеме |
| Check for updates automatically | Автоматически проверять обновления |

## Field labels

| English | Русский |
| --- | --- |
| Reason | Причина |
| Author | Автор |
| Application | Приложение |
| Title | Название |
| Subject | Тема |
| Keywords | Ключевые слова |
| Signing certificate | Сертификат подписи |
| File size | Размер файла |
| Comment | Комментарий |
| Product name | Название продукта |
| File | Файл |
| Size | Размер |
| Patches | Исправления |
| (unknown) | (неизвестно) |
| (patches only) | (только исправления) |
| missing | отсутствует |

## Status and progress

| English | Русский |
| --- | --- |
| Scanning... | Сканирование... |
| Cancelling... | Отмена... |
| Starting scan... | Запуск сканирования... |
| Asking Windows about installed software... | Запрос к Windows об установленном ПО... |
| Scanning installer cache folder... | Сканирование папки кэша установки... |
| Enumerating installed products... | Перечисление установленных продуктов... |
| Checking registry for additional packages... | Проверка реестра на дополнительные пакеты... |
| Found {0} registered {1}. | Найдено зарегистрированных {1}: {0}. |
| Scan complete ({0}) | Сканирование завершено ({0}) |
| Scanning local packages... | Сканирование локальных пакетов... |
| Found {0} {1} you can safely delete. | Найдено {0} {1} для безопасного удаления. |
| Preparing destination folder... | Подготовка папки назначения... |
| Moving unneeded files... | Moving unneeded files... |
| Deleting unneeded files... | Deleting unneeded files... |
| Move cancelled. {0} of {1} {2} processed. | Перемещение отменено. Обработано {0}/{1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Удаление отменено. Обработано {0}/{1} {2}. |
| Move failed ({0}). Details in {1}. | Не удалось переместить ({0}). Подробности в {1}. |
| Move failed ({0}). The crash log could not be written. | Не удалось переместить ({0}). Не удалось записать журнал сбоев. |
| Delete failed ({0}). Details in {1}. | Не удалось удалить ({0}). Подробности в {1}. |
| Delete failed ({0}). The crash log could not be written. | Не удалось удалить ({0}). Не удалось записать журнал сбоев. |
| Access denied. Windows refused the scan. | Доступ запрещён. Windows отклонил сканирование. |
| Scan failed: couldn't read the Windows Installer records. | Сканирование не удалось: не удалось прочитать записи Windows Installer. |
| Scan cancelled. | Сканирование отменено. |
| Ready | Готово |
| Scan failed ({0}). Details in {1}. | Сбой сканирования ({0}). Подробности в {1}. |
| Scan failed ({0}). The crash log could not be written. | Сбой сканирования ({0}). Не удалось записать журнал сбоев. |

## Main screen text

| English | Русский |
| --- | --- |
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Они лежат в {InstallerFolder} и остаются после того, как программа была удалена ({0}), более новое исправление заменило одно из них ({1}) или издатель его отозвал ({2}). InstallerClean всегда перечисляет только те файлы, которые сам Windows объявляет отработавшими. |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | Пока ничего не просканировано. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Нажмите «Повторить сканирование», чтобы просмотреть {InstallerFolder} в поисках файлов установщика, которые больше не нужны ни одной программе. |
| These files can't be cleaned up right now. | Эти файлы сейчас нельзя очистить. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. |
| Select a file to view details. | Выберите файл, чтобы посмотреть сведения. |
| Select a product to view details. | Выберите продукт, чтобы посмотреть сведения. |
| No metadata available. | Метаданные недоступны. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. | This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README [объясняет эту папку] и то, как восстановить файл, словами самой Microsoft. |
| (none) | (нет) |

## Reasons a file is unneeded

| English | Русский |
| --- | --- |
| Orphaned | Бесхозный |
| Superseded | Замещённый |
| Obsoleted | Устаревший |

## Completion screen

| English | Русский |
| --- | --- |
| All clean | Всё чисто |
| Nothing removed | Nothing removed |
| Nothing to clean up in {InstallerFolder} | В {InstallerFolder} нечего очищать |
| Scanned {0} {1} in {2} | Просканировано {0} {1} за {2} |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| {0} freed | Освобождено {0} |
| {0} moved | Перемещено {0} |
| Nothing was moved | Ничего не перемещено |
| Nothing was deleted | Ничего не удалено |
| {0} of {1} could not be moved. | Не удалось переместить {0} файл из {1}. |
| {0} of {1} could not be moved. | Не удалось переместить {0} файлов из {1}. |
| {0} of {1} could not be deleted. | Не удалось удалить {0} файл из {1}. |
| {0} of {1} could not be deleted. | Не удалось удалить {0} файлов из {1}. |
| {0} {1} moved to: {2} | Перемещено {0} {1} в: {2} |
| {0} {1} moved to: {2} | Перемещено {0} {1} в: {2} |
| {0} {1} kept in place, because the records now claim what the scan flagged. | {0} {1} kept in place, because the records now claim what the scan flagged. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} {1} kept in place, because the Windows Installer records had changed by the final check. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} kept in place, because Windows has a record of the program named inside. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} kept in place, because InstallerClean couldn't find a program named inside. |
| Moved {0} of {1} {2} before you cancelled. | Перемещено {0}/{1} {2} до отмены. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Удалено безвозвратно {0}/{1} {2} до отмены. |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | Рад, что пригодилось. Если захочется проявить щедрость, есть куда оставить на чай. |

## Summaries and counts

| English | Русский |
| --- | --- |
| {0} file left alone | {0} file left alone |
| {0} files left alone | {0} files left alone |
| {0} unneeded file to clean up | {0} ненужный файл для очистки |
| {0} unneeded files to clean up | {0} ненужных файлов для очистки |
| {0} registered file is missing. No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} registered file is missing. No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. |
| {0} registered files are missing. No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} registered files are missing. No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. |
| InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| {0} of {1} {2} | {0}/{1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} бесхозных, {1} замещённых, {2} устаревших ({3}) |
| {0} registered file left alone ({1}) | {0} registered file left alone ({1}) |
| {0} registered files left alone ({1}) | {0} registered files left alone ({1}) |

## Confirmation dialogs

| English | Русский |
| --- | --- |
| Move {0} {1} ({2})? | Переместить {0} {1} ({2})? |
| Move to: | Move to: |
| Delete {0} {1} ({2})? | Удалить {0} {1} ({2})? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

## Error messages

| English | Русский |
| --- | --- |
| Access denied | Доступ запрещён |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows отказал InstallerClean в доступе, поэтому работа была остановлена. Ничего не было удалено.<br><br>InstallerClean уже был запущен от имени администратора, поэтому повторный запуск таким же образом не поможет. Windows не сообщает ничего больше о том, что именно отказало в доступе, поэтому пробовать что-то конкретное бессмысленно. |
| Couldn't read the Windows Installer records | Не удалось прочитать записи Windows Installer |
| Scan failed | Сбой сканирования |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Записи Windows Installer вернулись совершенно пустыми: ни одна установленная программа и ни одно обновление не заявляет прав на кэшированный файл установщика. На работающем компьютере такого не бывает (даже у свежей установки Windows такие файлы есть), значит, записи либо повреждены, либо их не удалось прочитать, и сканирование, поверившее такому ответу, ошибочно сочло бы бесхозным каждый файл в {InstallerFolder}. Вместо этого InstallerClean остановился. Ничего не было удалено. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer не позволил InstallerClean перечислить установленное. InstallerClean уже был запущен от имени администратора, поэтому повторный запуск от имени администратора ничего не изменит. Без этого списка невозможно безопасно определить, какие кэшированные файлы ещё нужны, поэтому InstallerClean остановился. Ничего не было удалено. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer не смог предоставить InstallerClean читаемый список установленных программ: {0} записей подряд вернулись нечитаемыми (последний код ошибки {1}). Вместо того чтобы работать с прочитанным лишь частично списком, InstallerClean остановился. Ничего не было удалено. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer так и не сообщил о конце списка установленных программ: InstallerClean прекратил попытки после {0} записей (последний код ошибки {1}). Списку без конца доверять нельзя, поэтому InstallerClean остановился. Ничего не было удалено. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer так и не сообщил о конце списка исправлений одной программы: InstallerClean прекратил попытки после {0} записей (последний код ошибки {1}). Списку без конца доверять нельзя, поэтому InstallerClean остановился. Ничего не было удалено. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean не смог прочитать достаточно записей Windows Installer, чтобы точно знать, что ещё нужно: список установленных программ вернулся неполным, а чтение тех же записей напрямую из реестра тоже привело к ошибкам. Файл мог выглядеть бесхозным лишь потому, что запись, которая его называет, оказалась одной из нечитаемых, поэтому InstallerClean остановился. Ничего не было удалено. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. |
| Nothing was deleted | Ничего не удалено |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Invalid destination | Недопустимая папка назначения |
| Could not write to destination | Не удалось записать в папку назначения |
| Move failed | Сбой перемещения |
| Delete failed | Сбой удаления |
| Setting not saved | Настройка не сохранена |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Не удалось сохранить изменение. При следующем запуске InstallerClean вернётся к прежней настройке. |
| The destination cannot be inside the Windows Installer folder. | Папка назначения не может находиться внутри папки Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Not enough space | Недостаточно места |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Недостаточно места в {0}<br><br>Требуется: {1}<br>Доступно: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | У вас нет прав на запись в {0}.<br>Попробуйте папку в своём профиле пользователя или на диске, которым владеете. |
| The path {0} is too long for Windows. Pick a shorter path. | Путь {0} слишком длинный для Windows. Выберите путь покороче. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | Папка {0} не существует, и её не удалось создать. Проверьте букву диска или сетевой путь. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows не может выполнить запись в {0}.<br>Подробности в {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows не может выполнить запись в {0}. Не удалось записать журнал сбоев. |
| Cannot write to {0}.<br>Details in {1}. | Не удаётся выполнить запись в {0}.<br>Подробности в {1}. |
| Cannot write to {0}. The crash log could not be written. | Не удаётся выполнить запись в {0}. Не удалось записать журнал сбоев. |
| File no longer exists. | Файл больше не существует. |
| Source file is a symlink or junction; refused for safety. | Исходный файл является символической ссылкой или точкой соединения; отклонено в целях безопасности. |
| This file is not directly inside the Windows Installer folder; refused for safety. | Этот файл находится не в самой папке Windows Installer; отклонено в целях безопасности. |
| Windows refused access to this file; it was left in place. | Windows отказал в доступе к этому файлу; он оставлен на месте. |
| Windows refused access to these files; they were left in place. | Windows отказал в доступе к этим файлам; они оставлены на месте. |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows сообщил об ошибке файла; файл оставлен на месте. |
| Windows reported file errors; these files were left in place. | Windows сообщил об ошибках файлов; эти файлы оставлены на месте. |
| Something went wrong with this file; it was left in place. | С этим файлом что-то пошло не так; он оставлен на месте. |
| Something went wrong with these files; they were left in place. | С этими файлами что-то пошло не так; они оставлены на месте. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Перемещение файлов в папку Windows Installer отклонено (назначение: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
| Cannot write to {0}. | Не удаётся выполнить запись в {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Не удалось подобрать уникальное имя файла для «{0}» после 10 000 попыток. |

## Update check

| English | Русский |
| --- | --- |
| Check for updates | Проверка обновлений |
| Checking... | Проверка... |
| Up to date. | Установлена последняя версия. |
| Version {0} is available. | Доступна версия {0}. |
| Update available | Доступно обновление |
| You're running version {0}.<br>Version {1} is available. | У вас установлена версия {0}.<br>Доступна версия {1}. |
| Couldn't reach GitHub. Check your internet connection and try again. | Не удалось подключиться к GitHub. Проверьте подключение к интернету и повторите попытку. |
| GitHub returned an error response. Try again in a few minutes. | GitHub вернул ответ с ошибкой. Повторите попытку через несколько минут. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | В ответе GitHub не оказалось распознаваемого выпуска. Повторите попытку позже или откройте страницу выпусков напрямую. |
| The check timed out. Your connection to GitHub may be slow; try again. | Время ожидания проверки истекло. Возможно, соединение с GitHub медленное; повторите попытку. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | Проверка не удалась по неизвестной причине. Подробности в crash.log, если нужно сообщить об этом. |

## Opening links in your browser

| English | Русский |
| --- | --- |
| Couldn't open your browser | Не удалось открыть браузер |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean не удалось открыть браузер. Ссылка скопирована в буфер обмена, так что можете вставить её сами:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean не удалось открыть браузер, и скопировать ссылку в буфер обмена тоже не удалось. Вот ссылка:<br><br>{0} |

## Sending the summary

| English | Русский |
| --- | --- |
| Sending... | Отправка... |
| Thanks! Report sent. | Спасибо! Отчёт отправлен. |
| Sending failed. Try again later. | Не удалось отправить. Повторите попытку позже. |
| No report to send. | Нет отчёта для отправки. |
| Send this? | Отправить это? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | Отправляется на nofaff.netlify.app/api/result-log. Ничто не идентифицирует вас или ваш компьютер; это просто даёт мне знать, что InstallerClean работает и [сколько места люди освобождают]. |

## Startup and crashes

| English | Русский |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean уже запущен. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Произошла непредвиденная ошибка, и InstallerClean необходимо закрыть.<br><br>{0}<br><br>Подробности записаны в:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Произошла непредвиденная ошибка, и InstallerClean необходимо закрыть.<br><br>{0}<br><br>Не удалось записать журнал сбоев. |
| Startup error | Ошибка запуска |
| Failed to start ({0}). Details written to:<br>{1} | Не удалось запустить ({0}). Подробности записаны в:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Не удалось запустить ({0}). Не удалось записать журнал сбоев. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

## Tooltips (hover text)

| English | Русский |
| --- | --- |
| It's thirsty work! | В горле пересохло! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Запрошена отмена. InstallerClean ждёт, когда текущий шаг дойдёт до точки остановки. Это может занять несколько секунд при интенсивном вводе-выводе или обращении к базе данных MSI. |
| Close | Закрыть |
| A star helps other people find it. | Звезда помогает другим найти InstallerClean. |
| Minimise | Свернуть |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | На ваше усмотрение, но будет приятно. Отправляет анонимную сводку, которая просто даёт мне знать, работает ли приложение и сколько места люди освобождают. На следующем экране вы увидите, что именно будет отправлено, прежде чем подтвердить. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | На ваше усмотрение, но будет приятно. Отправляет анонимную сводку, которая просто даёт мне знать, работает ли приложение. На следующем экране вы увидите, что именно будет отправлено, прежде чем подтвердить. |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Имя субъекта из встроенного сертификата Authenticode. Цепочка не проверялась. |
| Change language. The program will restart. | Изменить язык. Программа перезапустится. |

## Screen reader labels

| English | Русский |
| --- | --- |
| Donate | Поддержать |
| Buy me a cuppa | Угостите меня чаем |
| Cancel operation | Отмена операции |
| Cancel scan | Отмена сканирования |
| Cancel startup scan | Отмена сканирования при запуске |
| Close | Закрыть |
| Close window | Закрыть окно |
| Close result and return to main window | Закрыть результат и вернуться в главное окно |
| Leave a star on github | Поставить звезду на github |
| Minimise | Свернуть |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | «Переместить» помещает ненужные файлы в выбранную папку назначения. «Отмена» оставляет их на месте. |
| Say thanks | Поблагодарить |
| Send posts the report shown to No Faff. Cancel sends nothing. | «Отправить» передаёт показанный отчёт в No Faff. «Отмена» не отправляет ничего. |
| Check for updates | Проверить обновления |
| Checks github's releases page for a newer version. | Проверяет на странице выпусков github, есть ли более новая версия. |
| Opens the readme on github in your browser. | Открывает readme на github в вашем браузере. |
| Opens the issue tracker on github.com in your browser. | Открывает список проблем (Issues) на github.com в вашем браузере. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Если флажок установлен, InstallerClean при запуске проверяет на github наличие более новой версии. |
| Open the release page to download the newer version, or cancel to keep the current version. | Откройте страницу выпуска, чтобы скачать более новую версию, или нажмите «Отмена», чтобы оставить текущую. |
| Opens the licence file on github.com in your browser. | Открывает файл лицензии на github.com в вашем браузере. |
| Backup folder | Backup folder |
| Products | Продукты |
| Patches | Исправления |
| Product details | Сведения о продукте |
| Backup folder | Backup folder |
| Operation progress | Ход операции |
| Scan {InstallerFolder} again | Сканировать {InstallerFolder} заново |
| Scanning progress | Ход сканирования |
| Startup scan progress | Ход сканирования при запуске |
| Details, unneeded files | Подробности, ненужные файлы |
| Available for cleanup. | Доступны для очистки. |
| Details, registered files | Подробности, зарегистрированные файлы |
| Read-only inventory. | Список только для чтения. |
| Sorted by {0}, ascending | Сортировка по столбцу «{0}», по возрастанию |
| Sorted by {0}, descending | Сортировка по столбцу «{0}», по убыванию |
| Scan results | Результаты сканирования |
| Result details | Сведения о результате |
| File details | Сведения о файле |
| Product details | Product details |
| Dialog text | Текст диалогового окна |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Файлы, которые не удалось обработать |
| Explains this folder, and how to recover a file, in the README | Объясняет эту папку и то, как восстановить файл, в README |
| Report preview | Предпросмотр отчёта |
| Change language | Изменить язык |
| The program will restart. | Программа перезапустится. |

## File picker

| English | Русский |
| --- | --- |
| Choose destination folder for moved files | Выберите папку назначения для перемещённых файлов |

## Version

| English | Русский |
| --- | --- |
| Version {0} | Версия {0} |

## Word forms (singular and plural)

| English | Русский |
| --- | --- |
| file | файл |
| files | файлов |
| error | ошибка |
| errors | ошибок |
| package | пакет |
| packages | пакетов |
| product | продукт |
| products | продуктов |
| patch | исправление |
| patches | исправлений |

## Sizes and times

| English | Русский |
| --- | --- |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | меньше секунды |
| {0:F1} seconds | {0:F1} секунды |

## Command-line tool (installerclean-cli)

| English | Русский |
| --- | --- |
| Error: unknown argument '{0}' | Error: unknown argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Ошибка: неожиданный лишний аргумент «{0}». Если в пути к папке для перемещения есть пробел, возьмите весь путь в кавычки: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. |
| Cancelling... | Отмена... |
| Cancelled. | Отменено. |
| Error: unexpected failure ({0}). Details written to {1}. | Error: unexpected failure ({0}). Details written to {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Error: unexpected failure ({0}). The crash log could not be written. |
| Scanning {InstallerFolder}... | Сканирование {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Found {0} unneeded {1} to clean up ({2}). |
| Found no unneeded files. | Found no unneeded files. |
| {0} registered file is missing from {InstallerFolder}. No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, preferably the same version, should restore it. | {0} registered file is missing from {InstallerFolder}. No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, preferably the same version, should restore it. |
| {0} registered files are missing from {InstallerFolder}. No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, preferably the same version, should restore them. | {0} registered files are missing from {InstallerFolder}. No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, preferably the same version, should restore them. |
| InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Deleting {0} unneeded {1}... | Deleting {0} unneeded {1}... |
| Permanently deleted {0} unneeded {1}. | Permanently deleted {0} unneeded {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Ошибка: не указана папка назначения для перемещения. Используйте /m ПУТЬ. (Значение по умолчанию, заданное в графическом интерфейсе, действует только для текущего пользователя и не применяется при запуске по расписанию или от имени служебной учётной записи.) |
| Error: destination cannot be inside the Windows Installer folder. | Ошибка: папка назначения не может находиться внутри папки Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Ошибка: папка назначения должна быть полным путём. Получено: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Moving {0} unneeded {1} to {2}... | Moving {0} unneeded {1} to {2}... |
| Moved {0} unneeded {1}. | Moved {0} unneeded {1}. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Другой процесс InstallerClean удерживает блокировку единственного экземпляра (GUI или другой запуск CLI). Код выхода 75 (временное состояние); можно повторить попытку позже. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Примечание: не удалось выполнить запись в журнал событий. Проверьте разрешения журнала «Приложение» или групповую политику. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - очистка {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Removes cached .msi and .msp files that no installed program still needs. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Needs an elevated (administrator) prompt; Windows will not start it. |
| Usage: | Использование: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help       Показать эту справку (также /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version    Показать версию (также -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ПУТЬ      Переместить в указанный путь |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. |
| Exit codes: | Коды выхода: |
|   0   success: the run finished with nothing left to do |   0   success: the run finished with nothing left to do |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partial: some processed, some not (a failure or a Ctrl+C part way) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  временно: запуск заблокирован временным состоянием (см. сообщение) |
|   130 cancelled (Ctrl+C) |   130 отменено (Ctrl+C) |
