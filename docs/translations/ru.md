# InstallerClean UI in Русский (Russian)

The text of InstallerClean's interface in English on the left, with the Russian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Russian can read through the translation and flag anything that doesn't read well. See [Can you help translate the GUI?](../../README.ru.md#can-you-help-translate-the-gui) for how to suggest a change, whether an issue or a pull request.

A few lines (the app name, version and file-size formats) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.ru.resx`](../../src/InstallerClean.Core/Resources/Strings.ru.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

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
| _About | _О программе |
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
| _Move instead | _Переместить |
| Path to folder if you Move instead of Delete | Путь к папке, если вы выберете «Переместить» вместо «Удалить» |
| Open _release page | Открыть страницу _выпуска |
| _Re-scan | _Повторить сканирование |
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
| Found {0} {1} to clean up. | Найдено {0} {1} для очистки. |
| Preparing destination folder... | Подготовка папки назначения... |
| Moving {0} {1}... | Перемещение: {0} {1}... |
| Deleting {0} {1}... | Удаление: {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Перемещение отменено. Обработано {0}/{1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Удаление отменено. Обработано {0}/{1} {2}. |
| Move failed ({0}). Details in {1}. | Не удалось переместить ({0}). Подробности в {1}. |
| Move failed ({0}). The crash log could not be written. | Не удалось переместить ({0}). Не удалось записать журнал сбоев. |
| Delete failed ({0}). Details in {1}. | Не удалось удалить ({0}). Подробности в {1}. |
| Delete failed ({0}). The crash log could not be written. | Не удалось удалить ({0}). Не удалось записать журнал сбоев. |
| Access denied. Run as administrator. | Доступ запрещён. Запустите от имени администратора. |
| Scan failed: installer database unavailable. | Сбой сканирования: база данных установщика недоступна. |
| Scan cancelled. | Сканирование отменено. |
| Done | Готово |
| Scan failed ({0}). Details in {1}. | Сбой сканирования ({0}). Подробности в {1}. |
| Scan failed ({0}). The crash log could not be written. | Сбой сканирования ({0}). Не удалось записать журнал сбоев. |

## Main screen text

| English | Русский |
| --- | --- |
| The unneeded files below are safe to delete. | Ненужные файлы ниже можно безопасно удалить. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Они лежат в C:\Windows\Installer и остаются после того, как программа была удалена ({0}), более новый патч заменил один из них ({1}) или издатель его отозвал ({2}). InstallerClean всегда перечисляет только те файлы, которые сам Windows объявляет отработавшими. |
| Delete them to the Recycle Bin, or Move them elsewhere first if you'd rather keep a copy. | Удалите их в Корзину или сначала переместите в другое место, если хотите сохранить копию. |
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
| Copy them back if anything stops working | Скопируйте их обратно, если что-то перестанет работать |
| Restore them from the Recycle Bin if anything stops working | Восстановите их из Корзины, если что-то перестанет работать |
| {0} freed | Освобождено {0} |
| {0} moved | Перемещено {0} |
| {0} moved, some files could not be processed | Перемещено {0}, некоторые файлы не удалось обработать |
| {0} freed, some files could not be processed | Освобождено {0}, некоторые файлы не удалось обработать |
| {0} {1} moved to {2} | Перемещено {0} {1} в {2} |
| {0} {1} moved to {2}. {3} {4} | Перемещено {0} {1} в {2}. {3} {4} |
| {0} {1} sent to the Recycle Bin | Отправлено {0} {1} в Корзину |
| {0} {1} sent to the Recycle Bin. {2} {3} | Отправлено {0} {1} в Корзину. {2} {3} |
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
| {0} files still needed | {0} файлов ещё нужно |
| {0} unneeded file to clean up | {0} ненужный файл для очистки |
| {0} unneeded files to clean up | {0} ненужных файлов для очистки |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | Отсутствует {0} зарегистрированный файл (InstallerClean его не удалял). Сейчас это не доставляет хлопот, но в будущем восстановление, обновление или удаление той программы может не выполниться. Откройте «Подробности», чтобы узнать, что делать. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | Отсутствует {0} зарегистрированных файлов (InstallerClean их не удалял). Сейчас это не доставляет хлопот, но в будущем восстановление, обновление или удаление тех программ может не выполниться. Откройте «Подробности», чтобы узнать, что делать. |
| {0} stale MSI entry detected (file already gone from disk; InstallerClean doesn't unregister it). | Обнаружена {0} устаревшая запись MSI (файл уже исчез с диска; InstallerClean её не удаляет из реестра). |
| {0} stale MSI entries detected (files already gone from disk; InstallerClean doesn't unregister them). | Обнаружено {0} устаревших записей MSI (файлы уже исчезли с диска; InstallerClean их не удаляет из реестра). |
| {0} of {1} {2} | {0}/{1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} для очистки ({2}) |
| {0} registered {1} ({2}) | {0} {1} ещё нужно ({2}) |

## Confirmation dialogs

| English | Русский |
| --- | --- |
| Move {0} {1} ({2})? | Переместить {0} {1} ({2})? |
| Files will be moved to {0}. | Файлы будут перемещены в {0}. |
| Delete {0} {1} ({2})? | Удалить {0} {1} ({2})? |
| Files will be sent to the Recycle Bin. If you'd like backup copies, use Move instead. | Файлы будут отправлены в Корзину. Если хотите сделать резервные копии, воспользуйтесь «Переместить». |

## Error messages

| English | Русский |
| --- | --- |
| Administrator rights required | Требуются права администратора |
| This app requires administrator privileges.<br><br>Please right-click and choose 'Run as administrator'. | Этому приложению нужны права администратора.<br><br>Щёлкните правой кнопкой мыши и выберите «Запуск от имени администратора». |
| Installer database unavailable | База данных установщика недоступна |
| Scan failed | Сбой сканирования |
| The Windows Installer database appears to be empty or inaccessible. This is unusual even on a fresh Windows install and typically means the database is corrupt or a third-party tool has cleared it. Running 'sfc /scannow' from an elevated prompt usually repairs it. | База данных Windows Installer оказалась пустой или недоступной. Это необычно даже для только что установленной Windows и обычно означает, что база данных повреждена или её очистил сторонний инструмент. Обычно её восстанавливает команда «sfc /scannow», запущенная из командной строки с повышенными правами. |
| Access denied enumerating installed products. Run as administrator. | Доступ запрещён при перечислении установленных продуктов. Запустите от имени администратора. |
| Windows Installer refused to list products after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installer отказался перечислять продукты после {0} последовательных сбоев (последний код ошибки {1}). Попробуйте перезагрузить Windows или запустите «sfc /scannow» из командной строки с повышенными правами. |
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
| Access denied. | Доступ запрещён. |
| The operation failed. Try again or restart Windows. | Операция не выполнена. Повторите попытку или перезагрузите Windows. |
| Unknown error. | Неизвестная ошибка. |
| Couldn't send this file to the Recycle Bin (error {0}). It may be locked, in use or blocked by Windows. Move it instead. | Не удалось отправить этот файл в Корзину (ошибка {0}). Возможно, он заблокирован, используется или его блокирует Windows. Воспользуйтесь «Переместить». |
| Windows blocked access to this file, even with administrator rights (error {0}). It is usually an ownership or permissions lock. Move it instead. | Windows заблокировал доступ к этому файлу даже с правами администратора (ошибка {0}). Обычно это блокировка из-за владельца или разрешений. Воспользуйтесь «Переместить». |
| This file is open or locked by another program (error {0}). Close that program, or whatever is scanning it, then try again, or Move it instead. | Этот файл открыт или заблокирован другой программой (ошибка {0}). Закройте ту программу или то, что его сканирует, затем повторите попытку либо воспользуйтесь «Переместить». |
| The file was permanently deleted because it could not be sent to the Recycle Bin. | Файл был удалён безвозвратно, потому что его не удалось отправить в Корзину. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Перемещение файлов в папку Windows Installer отклонено (назначение: {0}). |
| Destination must be a fully qualified path (relative paths resolve against the process current directory and are unsafe under elevation): {0} | Папка назначения должна быть полным путём (относительные пути разрешаются относительно текущего каталога процесса и небезопасны при работе с повышенными правами): {0} |
| Destination folder canonical path changed mid-batch: {0} | Канонический путь папки назначения изменился во время операции: {0} |
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
| The link couldn't be opened in your normal-user browser. The URL has been copied to your clipboard so you can open it manually:<br><br>{0} | Не удалось открыть ссылку в браузере обычного пользователя. URL-адрес скопирован в буфер обмена, чтобы вы могли открыть его вручную:<br><br>{0} |
| The link couldn't be opened in your normal-user browser, and copying it to the clipboard also failed. The URL is:<br><br>{0} | Не удалось открыть ссылку в браузере обычного пользователя, и скопировать её в буфер обмена тоже не вышло. URL-адрес:<br><br>{0} |

## Sending the summary

| English | Русский |
| --- | --- |
| Sending... | Отправка... |
| Thanks! Report sent. | Спасибо! Отчёт отправлен. |
| Sending failed. Try again later. | Не удалось отправить. Повторите попытку позже. |
| No report to send. | Нет отчёта для отправки. |
| Send this to No Faff? | Отправить это в No Faff? |
| Nothing identifies you or your machine; it just lets me know the app is working and how much space people are freeing. It goes to nofaff.netlify.app/api/result-log. | Ничто не идентифицирует вас или ваш компьютер; это просто даёт мне знать, что приложение работает и сколько места люди освобождают. Отправляется на nofaff.netlify.app/api/result-log. |

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
| If it helped, buy me a cup of tea. | Если пригодилось, угостите меня чашкой чая. |
| It's thirsty work! | В горле пересохло! |
| Cancellation requested. The app is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Запрошена отмена. Приложение ждёт, когда текущий шаг дойдёт до точки остановки. Это может занять несколько секунд при интенсивном вводе-выводе или обращении к базе данных MSI. |
| Close | Закрыть |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | Поставьте звезду на GitHub, сообщите о проблеме (Issue) или напишите в обсуждениях (Discussions). Любые отзывы приветствуются. |
| or report an Issue or post in Discussions. Any feedback welcome. | или сообщите о проблеме (Issue), или напишите в обсуждениях (Discussions). Любые отзывы приветствуются. |
| Minimise | Свернуть |
| Up to you but appreciated. Sends an anonymous report that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | На ваше усмотрение, но будет приятно. Отправляет анонимный отчёт, который просто даёт мне знать, работает ли приложение и сколько места люди освобождают. На следующем экране вы увидите, что именно будет отправлено, прежде чем подтвердить. |
| Up to you but appreciated. Sends an anonymous report that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | На ваше усмотрение, но будет приятно. Отправляет анонимный отчёт, который просто даёт мне знать, работает ли приложение. На следующем экране вы увидите, что именно будет отправлено, прежде чем подтвердить. |
| Move the unneeded files to the Move location. | Переместить ненужные файлы в папку для перемещения. |
| Move the unneeded files to the Move location. Choose one first. | Переместить ненужные файлы в папку для перемещения. Сначала выберите её. |
| Send the unneeded files to the Recycle Bin. | Отправить ненужные файлы в Корзину. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Имя субъекта из встроенного сертификата Authenticode. Цепочка не проверялась. |
| Change the display language. InstallerClean restarts to apply it. | Изменить язык интерфейса. Для применения InstallerClean перезапустится. |

## Screen reader labels

| English | Русский |
| --- | --- |
| Buy me a cup of tea | Угостить меня чашкой чая |
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
| Send all unneeded installer files to the Recycle Bin | Отправить все ненужные файлы установщика в Корзину |
| Delete sends the unneeded files to the Recycle Bin. Cancel closes without deleting. | «Удалить» отправляет ненужные файлы в Корзину. «Отмена» закрывает окно без удаления. |
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
| Change the display language | Изменить язык интерфейса |
| InstallerClean restarts to apply it. | Для применения InstallerClean перезапустится. |

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
