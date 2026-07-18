# InstallerClean in Русский (Russian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Russian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Russian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.ru.resx`](../../src/InstallerClean.Core/Resources/Strings.ru.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | Русский |
| --- | --- |
| InstallerClean | InstallerClean |
| About | О программе |
| Registered files that should not be deleted | Зарегистрированные файлы, которые не стоит удалять |
| Unneeded files that are safe to delete | Ненужные файлы, которые можно безопасно удалить |
| Confirm move | Подтверждение перемещения |
| Confirm delete | Подтверждение удаления |
| Recycle Bin unavailable | Корзина недоступна |

## Section headings

| English | Русский |
| --- | --- |
| PRODUCTS | ПРОДУКТЫ |
| PATCHES | ПАТЧИ |
| PRODUCT DETAILS | СВЕДЕНИЯ О ПРОДУКТЕ |
| MOVE LOCATION | ПАПКА ДЛЯ ПЕРЕМЕЩЕНИЯ |
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
| Check for _updates | Проверить _обновления |
| _Close | _Закрыть |
| _Delete | _Удалить |
| _Delete permanently | _Удалить безвозвратно |
| _Done | _Готово |
| Details | Подробности |
| _Buy me a cuppa | _Угостить чаем |
| Leave a _star on GitHub | Поставить з_везду на GitHub |
| MIT licence | Лицензия MIT |
| _Move | _Переместить |
| _Move instead | _Переместить вместо этого |
| Path to folder if you Move instead of Delete | Путь к папке, если вы выберете «Переместить» вместо «Удалить» |
| Open _release page | Открыть страницу _выпуска |
| _Re-scan | По_вторить сканирование |
| _Scan again | _Сканировать снова |
| Send report | Отправить отчёт |
| _Send | От_править |

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
| Patches | Патчи |
| (unknown) | (неизвестно) |
| (patches only) | (только патчи) |
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
| Moving {0} {1}... | Перемещение: {0} {1}... |
| Deleting {0} {1}... | Удаление: {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Перемещение отменено. Обработано {0}/{1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Удаление отменено. Обработано {0}/{1} {2}. |
| Move failed ({0}). Details in {1}. | Не удалось переместить ({0}). Подробности в {1}. |
| Move failed ({0}). The crash log could not be written. | Не удалось переместить ({0}). Не удалось записать журнал сбоев. |
| Delete failed ({0}). Details in {1}. | Не удалось удалить ({0}). Подробности в {1}. |
| Delete failed ({0}). The crash log could not be written. | Не удалось удалить ({0}). Не удалось записать журнал сбоев. |
| Access denied. Windows refused the scan. | Доступ запрещён. Windows отклонил сканирование. |
| Scan failed: couldn't read the Windows Installer records. | Сканирование не удалось: не удалось прочитать записи установщика Windows. |
| Scan cancelled. | Сканирование отменено. |
| Ready | Готово |
| Scan failed ({0}). Details in {1}. | Сбой сканирования ({0}). Подробности в {1}. |
| Scan failed ({0}). The crash log could not be written. | Сбой сканирования ({0}). Не удалось записать журнал сбоев. |

## Main screen text

| English | Русский |
| --- | --- |
| The unneeded files below are safe to delete. | Ненужные файлы ниже можно безопасно удалить. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Они лежат в C:\Windows\Installer и остаются после того, как программа была удалена ({0}), более новый патч заменил один из них ({1}) или издатель его отозвал ({2}). InstallerClean всегда перечисляет только те файлы, которые сам Windows объявляет отработавшими. |
| Delete them to the Recycle Bin, or use Move instead if you'd rather keep a copy. | Удалите их в Корзину, или используйте вместо этого функцию «Переместить», если хотите сохранить копию. |
| Nothing scanned yet. | Пока ничего не просканировано. |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | Нажмите «Повторить сканирование», чтобы просмотреть C:\Windows\Installer в поисках файлов установщика, которые больше не нужны ни одной программе. |
| These files can't be cleaned up right now. | Эти файлы сейчас нельзя очистить. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | Прямо сейчас что-то использует Windows Installer, обычно это обновление Windows или программа, устанавливающаяся в фоне. Пока это происходит, «Переместить» и «Удалить» приостановлены, чтобы InstallerClean не трогал кэш установки, пока тот меняется. Когда всё завершится, выполните повторное сканирование, и они снова станут доступны. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | На этом компьютере приостановлена предыдущая транзакция Windows Installer. Прежде чем очищать кэш, продолжите или откатите ту установку (либо перезагрузите Windows). |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows поставил в очередь на следующую перезагрузку переименование файла, затрагивающее кэш установки. Прежде чем очищать, перезагрузите Windows. |
| Select a file to view details. | Выберите файл, чтобы посмотреть сведения. |
| Select a product to view details. | Выберите продукт, чтобы посмотреть сведения. |
| No metadata available. | Метаданные недоступны. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | Этот файл установщика был удалён. InstallerClean тут ни при чём: он никогда не удаляет файл, который ещё нужен программе; этот файл удалило что-то другое ещё до того, как вы запустили InstallerClean.<br><br>Сейчас это не доставляет хлопот и не будет, пока в один прекрасный день вы не попробуете восстановить, обновить или удалить программу, которой он принадлежит. Тогда этот шаг может не выполниться, потому что Windows ищет этот файл и не находит его.<br><br>Чтобы попробовать это исправить, скачайте установщик той программы у её разработчика и запустите его поверх имеющейся копии (не удаляйте программу заранее: удаление само по себе шаг, которому нужен этот файл). По возможности возьмите ту версию, что установлена сейчас, потому что Windows может отклонить другую. Обычно это возвращает файл на место, и ваши настройки, как правило, остаются нетронутыми, но Microsoft этого не гарантирует: её собственное последнее средство — переустановка программы или самой Windows. |
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
| Nothing to clean up in C:\Windows\Installer | В C:\Windows\Installer нечего очищать |
| Scanned {0} {1} in {2} | Просканировано {0} {1} за {2} |
| Copy them back if anything breaks ([it won't!]). | Скопируйте их обратно, если что-то сломается ([а оно не сломается!]). |
| Until then, you can restore them if anything breaks ([it won't!]). | А пока их можно восстановить, если что-то сломается ([а оно не сломается!]). |
| Empty it to actually reclaim the space. | Очистите Корзину, чтобы действительно освободить место. |
| {0} freed | Освобождено {0} |
| {0} cleaned up | Очищено {0} |
| {0} moved | Перемещено {0} |
| {0} moved, some files could not be processed | Перемещено {0}, некоторые файлы не удалось обработать |
| {0} freed, some files could not be processed | Освобождено {0}, некоторые файлы не удалось обработать |
| {0} cleaned up, some files could not be processed | Очищено {0}, некоторые файлы не удалось обработать |
| {0} {1} moved to: {2} | Перемещено {0} {1} в: {2} |
| {0} {1} moved to: {2} | Перемещено {0} {1} в: {2} |
| {0} {1} moved to: {2}. {3} {4} | Перемещено {0} {1} в: {2}. {3} {4} |
| {0} {1} moved to: {2}. {3} {4} | Перемещено {0} {1} в: {2}. {3} {4} |
| {0} {1} moved to the Recycle Bin | Перемещено {0} {1} в Корзину |
| {0} {1} moved to the Recycle Bin | Перемещено {0} {1} в Корзину |
| {0} {1} moved to the Recycle Bin. {2} {3} | Перемещено {0} {1} в Корзину. {2} {3} |
| {0} {1} moved to the Recycle Bin. {2} {3} | Перемещено {0} {1} в Корзину. {2} {3} |
| {0} {1} kept in place, because a program started needing them again after the scan. | Оставлено {0} {1} на месте: после сканирования они снова понадобились программе. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | Оставлено {0} {1} на месте: при повторной проверке не удалось полностью прочитать записи установщика Windows. |
| Moved {0} of {1} {2} before you cancelled. | Перемещено {0}/{1} {2} до отмены. |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | Перемещено {0}/{1} {2} в Корзину до отмены. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Удалено безвозвратно {0}/{1} {2} до отмены. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} удалён безвозвратно. Он не попал в Корзину. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1} удалено безвозвратно. Они не попали в Корзину. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. {2} {3} | {0} {1} удалён безвозвратно. Он не попал в Корзину. {2} {3} |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. {2} {3} | {0} {1} удалено безвозвратно. Они не попали в Корзину. {2} {3} |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Это нормально, его можно было безопасно удалить. InstallerClean очищает только те файлы, которые Windows объявляет отработавшими, и никогда тот, что ещё нужен программе. В маловероятном случае, если удаление когда-нибудь лишит программу возможности восстановления, обновления или удаления, переустановка её у разработчика обычно возвращает файл, хотя Microsoft этого не гарантирует. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Это нормально, их можно было безопасно удалить. InstallerClean очищает только те файлы, которые Windows объявляет отработавшими, и никогда тот, что ещё нужен программе. В маловероятном случае, если удаление когда-нибудь лишит программу возможности восстановления, обновления или удаления, переустановка её у разработчика обычно возвращает файл, хотя Microsoft этого не гарантирует. |

## Recycle Bin unavailable

| English | Русский |
| --- | --- |
| The Recycle Bin isn't available for this drive | Корзина недоступна для этого диска |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Поэтому этот {1} ({2}) не был удалён. Вы можете переместить его в надёжное место или удалить безвозвратно. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Поэтому эти {0} {1} ({2}) не были удалены. Вы можете переместить их в надёжное место или удалить безвозвратно. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Удалять его безопасно. InstallerClean очищает только те файлы, которые Windows объявляет отработавшими, и никогда тот, что ещё нужен программе, а Корзина — лишь дополнительная подстраховка. В маловероятном случае, если удаление когда-нибудь лишит программу возможности восстановления, обновления или удаления, переустановка её у разработчика обычно возвращает файл, хотя Microsoft этого не гарантирует. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Удалять их безопасно. InstallerClean очищает только те файлы, которые Windows объявляет отработавшими, и никогда тот, что ещё нужен программе, а Корзина — лишь дополнительная подстраховка. В маловероятном случае, если удаление когда-нибудь лишит программу возможности восстановления, обновления или удаления, переустановка её у разработчика обычно возвращает файл, хотя Microsoft этого не гарантирует. |

## Summaries and counts

| English | Русский |
| --- | --- |
| {0} file still needed | {0} файл ещё нужен |
| {0} files still needed | {0} файлов ещё нужны |
| {0} unneeded file to clean up | {0} ненужный файл для очистки |
| {0} unneeded files to clean up | {0} ненужных файлов для очистки |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | Отсутствует {0} зарегистрированный файл (InstallerClean его не удалял). Сейчас это не доставляет хлопот, но в будущем восстановление, обновление или удаление той программы может не выполниться. Откройте «Подробности», чтобы узнать, что делать. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | Отсутствует {0} зарегистрированных файлов (InstallerClean их не удалял). Сейчас это не доставляет хлопот, но в будущем восстановление, обновление или удаление тех программ может не выполниться. Откройте «Подробности», чтобы узнать, что делать. |
| Windows still lists {0} old patch whose file is already gone from disk. That's harmless, and there's nothing you need to do. | В списке Windows всё ещё есть {0} старый патч, файла которого уже нет на диске. Это не страшно, и делать ничего не нужно. |
| Windows still lists {0} old patches whose files are already gone from disk. That's harmless, and there's nothing you need to do. | В списке Windows всё ещё есть {0} старых патчей, файлов которых уже нет на диске. Это не страшно, и делать ничего не нужно. |
| {0} installed program could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | При этом сканировании не удалось прочитать {0} установленную программу, поэтому замещённые патчи оставлены на месте. Бесхозных файлов это не касается. |
| {0} installed programs could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | При этом сканировании не удалось прочитать {0} установленных программ, поэтому замещённые патчи оставлены на месте. Бесхозных файлов это не касается. |
| {0} of {1} {2} | {0}/{1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} бесхозных, {1} замещённых, {2} устаревших ({3}) |
| {0} registered file that is still needed ({1}) | {0} зарегистрированный файл ещё нужен ({1}) |
| {0} registered files that are still needed ({1}) | {0} зарегистрированных файлов ещё нужны ({1}) |

## Confirmation dialogs

| English | Русский |
| --- | --- |
| Move {0} {1} ({2})? | Переместить {0} {1} ({2})? |
| Files will be moved to: | Файлы будут перемещены в: |
| Delete {0} {1} ({2})? | Удалить {0} {1} ({2})? |
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | Файлы будут перемещены в Корзину. Если хотите сделать резервные копии, воспользуйтесь кнопкой «Переместить». |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | Эта папка находится на том же диске, поэтому само по себе перемещение места не освободит. Оно вернётся, когда вы удалите из неё файлы, либо вместо этого можно выбрать папку на другом диске. |

## Error messages

| English | Русский |
| --- | --- |
| Access denied | Доступ запрещён |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows отказал InstallerClean в доступе, поэтому работа была остановлена. Ничего не было удалено.<br><br>InstallerClean уже был запущен от имени администратора, поэтому повторный запуск таким же образом не поможет. Windows не сообщает ничего больше о том, что именно отказало в доступе, поэтому пробовать что-то конкретное бессмысленно. |
| Couldn't read the Windows Installer records | Не удалось прочитать записи установщика Windows |
| Scan failed | Сбой сканирования |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in C:\Windows\Installer orphaned. InstallerClean stopped instead. Nothing has been removed. | Записи установщика Windows вернулись совершенно пустыми: ни одна установленная программа и ни одно обновление не заявляет прав на кэшированный файл установщика. На работающем компьютере такого не бывает (даже у свежей установки Windows такие файлы есть), значит, записи либо повреждены, либо их не удалось прочитать, и сканирование, поверившее такому ответу, ошибочно сочло бы бесхозным каждый файл в C:\Windows\Installer. Вместо этого InstallerClean остановился. Ничего не было удалено. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer не позволил InstallerClean перечислить установленное. InstallerClean уже был запущен от имени администратора, поэтому повторный запуск от имени администратора ничего не изменит. Без этого списка невозможно безопасно определить, какие кэшированные файлы ещё нужны, поэтому InstallerClean остановился. Ничего не было удалено. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer не смог предоставить InstallerClean читаемый список установленных программ: {0} записей подряд вернулись нечитаемыми (последний код ошибки {1}). Вместо того чтобы работать с прочитанным лишь частично списком, InstallerClean остановился. Ничего не было удалено. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer так и не сообщил о конце списка установленных программ: InstallerClean прекратил попытки после {0} записей (последний код ошибки {1}). Списку без конца доверять нельзя, поэтому InstallerClean остановился. Ничего не было удалено. |
| Windows Installer couldn't give InstallerClean a readable list of one program's patches: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer не смог предоставить InstallerClean читаемый список патчей одной программы: {0} записей подряд вернулись нечитаемыми (последний код ошибки {1}). Вместо того чтобы работать с прочитанным лишь частично списком, InstallerClean остановился. Ничего не было удалено. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer так и не сообщил о конце списка патчей одной программы: InstallerClean прекратил попытки после {0} записей (последний код ошибки {1}). Списку без конца доверять нельзя, поэтому InstallerClean остановился. Ничего не было удалено. |
| InstallerClean couldn't square this scan with the Windows Installer records: every file Windows still lists as needed is missing from C:\Windows\Installer, while the files actually in the folder match nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean не смог сопоставить это сканирование с записями установщика Windows: каждый файл, который Windows всё ещё числит нужным, отсутствует в C:\Windows\Installer, а файлы, реально лежащие в этой папке, не соответствуют ни одной записи. Ни один настоящий компьютер так не выглядит, поэтому это указывает на проблему с чтением записей, а не на файлы, которые можно безопасно удалить. Для очистки ничего не предложено, и ничего не было удалено. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean не смог прочитать достаточно записей установщика Windows, чтобы точно знать, что ещё нужно: список установленных программ вернулся неполным, а чтение тех же записей напрямую из реестра тоже привело к ошибкам. Файл мог выглядеть бесхозным лишь потому, что запись, которая его называет, оказалась одной из нечитаемых, поэтому InstallerClean остановился. Ничего не было удалено. |
| Invalid destination | Недопустимая папка назначения |
| Could not write to destination | Не удалось записать в папку назначения |
| Move failed | Сбой перемещения |
| Delete failed | Сбой удаления |
| The destination cannot be inside the Windows Installer folder. | Папка назначения не может находиться внутри папки Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Папка назначения {0} ведёт в системную папку Windows. Выберите путь за пределами %SystemRoot%, %ProgramFiles% и %ProgramData%. |
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
| This file is not inside the Windows Installer folder; refused for safety. | Этот файл находится не в папке Windows Installer; отклонено в целях безопасности. |
| Access denied. | Доступ запрещён. |
| Windows reported a file error; the file was left in place. | Windows сообщил об ошибке файла; файл оставлен на месте. |
| Unknown error. | Неизвестная ошибка. |
| Couldn't move this file to the Recycle Bin (error {0}), and InstallerClean can't tell you why from that code. The file was left in place. Try the Move button instead, since it doesn't use the Recycle Bin. | Не удалось переместить этот файл в Корзину (ошибка {0}), и по этому коду InstallerClean не может сказать почему. Файл оставлен на месте. Попробуйте вместо этого кнопку «Переместить», которая не использует Корзину. |
| Windows refused access even with administrator rights (error {0}), and InstallerClean can't tell whether the problem is the file or the Recycle Bin. The file was left in place. The Move button will work if it's the Recycle Bin, but not if it's the file. | Windows отказал в доступе даже с правами администратора (ошибка {0}), и InstallerClean не может определить, в чём дело — в файле или в Корзине. Файл оставлен на месте. Кнопка «Переместить» поможет, если дело в Корзине, но не поможет, если дело в файле. |
| This file is open or locked by another program (error {0}), so nothing can remove it just now. It was left in place; try again later. | Этот файл открыт или заблокирован другой программой (ошибка {0}), поэтому сейчас его ничто не может удалить. Он оставлен на месте; повторите попытку позже. |
| The file was permanently deleted because it could not be moved to the Recycle Bin. | Файл был удалён безвозвратно, потому что его не удалось переместить в Корзину. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Перемещение файлов в папку Windows Installer отклонено (назначение: {0}). |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Папка для перемещения должна быть полным путём к папке, начинающимся с буквы диска или сетевой папки (например, D:\Backup или \\server\backup). InstallerClean не может использовать такой путь: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | Папка для перемещения изменилась, пока шло перемещение файлов (что-то заменило или перенаправило эту папку), поэтому InstallerClean остановился, чтобы не записать данные не туда. Проверьте {0}, затем выполните повторное сканирование и попробуйте снова. |
| Cannot write to {0}. | Не удаётся выполнить запись в {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Не удалось подобрать уникальное имя файла для «{0}» после 10 000 попыток. |

## Update check

| English | Русский |
| --- | --- |
| Check for updates | Проверка обновлений |
| Checking... | Проверка... |
| Up to date. | Установлена последняя версия. |
| Update available | Доступно обновление |
| You're running version {0}.<br>Version {1} is available. | У вас установлена версия {0}.<br>Доступна версия {1}. |
| Couldn't reach GitHub. Check your internet connection and try again. | Не удалось подключиться к GitHub. Проверьте подключение к интернету и повторите попытку. |
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | GitHub вернул ответ с ошибкой. Возможно, для API выпусков действует ограничение частоты запросов; повторите попытку через несколько минут. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log фиксирует необработанные исключения InstallerClean.<br># При работе с повышенными правами сообщения исключений платформы<br># могут содержать пути к файлам текущего сеанса (включая профили<br># других пользователей, перечисленные запросами Windows Installer).<br># Сообщения о сетевых сбоях при проверке обновлений или отправке<br># журнала результатов могут содержать целевой URL-адрес и<br># разрешённый IP- или прокси-адрес. Удалите оба вида данных,<br># прежде чем прикладывать этот файл к публичному отчёту об ошибке.<br> |

## Tooltips (hover text)

| English | Русский |
| --- | --- |
| Donate | Поддержать |
| It's thirsty work! | В горле пересохло! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Запрошена отмена. InstallerClean ждёт, когда текущий шаг дойдёт до точки остановки. Это может занять несколько секунд при интенсивном вводе-выводе или обращении к базе данных MSI. |
| Close | Закрыть |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | Поставьте звезду на GitHub, сообщите о проблеме (Issue) или напишите в обсуждениях (Discussions). Любые отзывы приветствуются. |
| or report an Issue or post in Discussions. Any feedback welcome. | или сообщите о проблеме (Issue), или напишите в обсуждениях (Discussions). Любые отзывы приветствуются. |
| Minimise | Свернуть |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | На ваше усмотрение, но будет приятно. Отправляет анонимную сводку, которая просто даёт мне знать, работает ли приложение и сколько места люди освобождают. На следующем экране вы увидите, что именно будет отправлено, прежде чем подтвердить. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | На ваше усмотрение, но будет приятно. Отправляет анонимную сводку, которая просто даёт мне знать, работает ли приложение. На следующем экране вы увидите, что именно будет отправлено, прежде чем подтвердить. |
| Move the unneeded files to the Move location. | Переместить ненужные файлы в папку для перемещения. |
| Move the unneeded files to the Move location. Choose one first. | Переместить ненужные файлы в папку для перемещения. Сначала выберите её. |
| Move the unneeded files to the Recycle Bin. | Переместить ненужные файлы в Корзину. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Имя субъекта из встроенного сертификата Authenticode. Цепочка не проверялась. |
| Change language. The program will restart. | Изменить язык. Программа перезапустится. |

## Screen reader labels

| English | Русский |
| --- | --- |
| Donate | Поддержать |
| Buy me a cuppa (About window) | Угостить чаем (окно «О программе») |
| Cancel operation | Отменить операцию |
| Cancel scan | Отменить сканирование |
| Cancel startup scan | Отменить сканирование при запуске |
| Close | Закрыть |
| Close window | Закрыть окно |
| Close result and return to main window | Закрыть результат и вернуться в главное окно |
| Leave a star on GitHub | Поставить звезду на GitHub |
| Leave a star on GitHub (About window) | Поставить звезду на GitHub (окно «О программе») |
| Minimise | Свернуть |
| Move all unneeded installer files to the chosen destination folder | Переместить все ненужные файлы установщика в выбранную папку назначения |
| Move all unneeded installer files to the Recycle Bin | Переместить все ненужные файлы установщика в Корзину |
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | «Удалить» перемещает ненужные файлы в Корзину. «Отмена» закрывает окно без удаления. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | «Переместить» помещает ненужные файлы в выбранную папку назначения. «Отмена» оставляет их на месте. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Выберите, что сделать с ненужными файлами: переместить в надёжное место, удалить безвозвратно или отменить. |
| Move the unneeded files to a folder you choose | Переместить ненужные файлы в выбранную вами папку |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Удалить ненужные файлы безвозвратно, потому что Корзина недоступна для этого диска |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Отправляется на nofaff.netlify.app. Только счётчики и метки. Перед отправкой вы увидите точное содержимое. |
| Say thanks | Поблагодарить |
| Send posts the report shown to No Faff. Cancel sends nothing. | «Отправить» передаёт показанный отчёт в No Faff. «Отмена» не отправляет ничего. |
| Check for updates | Проверить обновления |
| Checks the GitHub releases API over HTTPS for a newer version. | Проверяет наличие более новой версии через API выпусков GitHub по HTTPS. |
| Open the release page to download the newer version, or cancel to keep the current version. | Откройте страницу выпуска, чтобы скачать более новую версию, или нажмите «Отмена», чтобы оставить текущую. |
| MIT licence | Лицензия MIT |
| Opens the licence file on github.com in your browser. | Открывает файл лицензии на github.com в вашем браузере. |
| Move location | Папка для перемещения |
| Products | Продукты |
| Patches | Патчи |
| Product details | Сведения о продукте |
| Move destination folder | Папка назначения для перемещения |
| Operation progress | Ход операции |
| Scan C:\Windows\Installer again | Сканировать C:\Windows\Installer заново |
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
| Dialog text | Текст диалогового окна |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Файлы, которые не удалось обработать |
| Explains this folder, and how to recover a file, in the README | Объясняет эту папку и то, как восстановить файл, в README |
| Result log preview | Предпросмотр журнала результатов |
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
| patch | патч |
| patches | патчей |

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
| Unknown argument: '{0}' | Неизвестный аргумент: «{0}» |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Ошибка: неожиданный лишний аргумент «{0}». Если в пути к папке для перемещения есть пробел, возьмите весь путь в кавычки: /m "D:\My Backup" |
| Cancelling... | Отмена... |
| Cancelled. | Отменено. |
| Error: {0}. Details written to {1}. | Ошибка: {0}. Подробности записаны в {1}. |
| Error: {0}. The crash log could not be written. | Ошибка: {0}. Не удалось записать журнал сбоев. |
| Scanning C:\Windows\Installer... | Сканирование C:\Windows\Installer... |
| Found {0} {1} to clean up ({2}). | Найдено {0} {1} для очистки ({2}). |
| Nothing to do. | Делать нечего. |
| Deleting {0} {1}... | Удаление: {0} {1}... |
| Deleted {0} {1}. | Удалено {0} {1}. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Ошибка: Корзина недоступна для этого диска, поэтому ничего не удалено. Воспользуйтесь /m, чтобы переместить файлы, либо снова включите Корзину и запустите ещё раз. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Ошибка: не указана папка назначения для перемещения. Используйте /m ПУТЬ. (Значение по умолчанию, заданное в графическом интерфейсе, действует только для текущего пользователя и не применяется при запуске по расписанию или от имени служебной учётной записи.) |
| Error: destination cannot be inside the Windows Installer folder. | Ошибка: папка назначения не может находиться внутри папки Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Ошибка: папка назначения должна быть полным путём. Получено: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Ошибка: папка назначения {0} ведёт в системную папку Windows. Выберите путь за пределами %SystemRoot%, %ProgramFiles% и %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Ошибка: прямо сейчас что-то использует Windows Installer, обычно это обновление Windows или программа, устанавливающаяся в фоне. Перемещение и удаление заблокированы, пока это происходит. Повторите попытку, когда всё завершится. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Ошибка: на этом компьютере приостановлена предыдущая транзакция Windows Installer. Прежде чем очищать кэш, продолжите или откатите ту установку (либо перезагрузите Windows). |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Ошибка: операция с файлом, поставленная в очередь на время после перезагрузки, затрагивает кэш установки ({0}). Прежде чем очищать, перезагрузите Windows, чтобы завершить эту операцию. |
| Moving {0} {1} to {2}... | Перемещение: {0} {1} в {2}... |
| Moved {0} {1}. | Перемещено {0} {1}. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Другой процесс InstallerClean удерживает блокировку единственного экземпляра (GUI или другой запуск CLI). Код выхода 75 (временное состояние); можно повторить попытку позже. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Примечание: не удалось выполнить запись в журнал событий. Проверьте разрешения журнала «Приложение» или групповую политику. |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - очистка C:\Windows\Installer |
| Usage: | Использование: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help       Показать эту справку (также принимает /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version    Показать версию (также принимает -v) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s           Только сканирование - список ненужных файлов |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d           Удалить ненужные файлы (Корзина) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m           Переместить в сохранённую папку по умолчанию |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ПУТЬ      Переместить в указанный путь |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli — это настоящий консольный процесс, он блокирует |
| until it finishes; redirect or pipe its output as you would any | командную строку до завершения; перенаправляйте или передавайте его вывод |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | по конвейеру, как у любого консольного exe. GUI рядом, в InstallerClean.exe. |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | Сохранённое значение по умолчанию задаётся отдельно для каждого пользователя; для запусков по расписанию или от имени SYSTEM нужно указывать /m ПУТЬ. |
| Exit codes: | Коды выхода: |
|   0   success: every flagged file was processed |   0   успех: обработаны все отмеченные файлы |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   ошибка: ничего не обработано (неверные аргументы, сбой сканирования, все файлы с ошибкой) |
|   2   partial: some files processed, some failed |   2   частично: часть файлов обработана, часть с ошибкой |
|   75  transient: a temporary condition blocked the run (see the message) |   75  временно: запуск заблокирован временным состоянием (см. сообщение) |
|   130 cancelled (Ctrl+C) |   130 отменено (Ctrl+C) |
