# InstallerClean in Русский (Russian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Russian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Russian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.ru.resx`](../../src/InstallerClean.Core/Resources/Strings.ru.resx), so do not edit it by hand. The Russian translation itself lives in [`gen-strings-ru.mjs`](../../scripts/translations/gen-strings-ru.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Русский |
| --- | --- |
| InstallerClean | InstallerClean |
| About | О программе |
| Files left alone | Файлы, оставленные без изменений |
| Unneeded files that are safe to delete | Ненужные файлы, которые можно безопасно удалить |
| The anonymous report | Анонимный отчёт |
| InstallerClean couldn't read the report. | InstallerClean не смог прочитать отчёт. |

## Section headings

| English | Русский |
| --- | --- |
| PATCHES | ИСПРАВЛЕНИЯ |
| PRODUCT DETAILS | СВЕДЕНИЯ О ПРОДУКТЕ |
| BACKUP FOLDER | ПАПКА РЕЗЕРВНЫХ КОПИЙ |
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
| Carry on _without it | Продолжить _без ожидания |
| Check for _updates | Проверить о_бновления |
| _Close | _Закрыть |
| _Close without tipping | _Закрыть без чаевых |
| _Delete permanently | _Удалить безвозвратно |
| _Done | _Готово |
| Details | Подробности |
| _Leave a tip | _Оставить чаевые |
| Leave a tip | Оставить чаевые |
| _Leave a tip | _Оставить чаевые |
| Leave a _star on GitHub | Поставить з_везду на GitHub |
| Apache 2.0 licence | Лицензия Apache 2.0 |
| _Move | _Переместить |
| Path to folder if you move rather than delete. | Путь к папке, если вы перемещаете, а не удаляете. |
| _Re-scan | По_вторить сканирование |

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
| (no program) | (нет программы) |
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
| Checking registry for additional packages... | Проверка реестра на дополнительные пакеты... |
| Matching files against installed programs... | Сопоставление файлов с установленными программами... |
| {0} of {1} | {0}/{1} |
| Checking remaining files... | Проверка остальных файлов... |
| Waiting for drive {0} to respond... | Ожидание ответа от диска {0}... |
| Waiting for {0} to respond... | Ожидание ответа от {0}... |
| Any file still to be checked against it will be left alone. | Любой файл, который ещё нужно сверить с тем, что там находится, будет оставлен без изменений. |
| Scan complete ({0}) | Сканирование завершено ({0}) |
| Found {0} {1} you can safely delete. | Найдено {0} {1} для безопасного удаления. |
| Preparing destination folder... | Подготовка папки назначения... |
| Moving unneeded files... | Перемещение ненужных файлов... |
| Deleting unneeded files... | Удаление ненужных файлов... |
| Move cancelled. {0} of {1} {2} processed. | Перемещение отменено. Обработано {0}/{1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Удаление отменено. Обработано {0}/{1} {2}. |
| {0}. Details are in {1}. | {0}. Подробности в {1}. |
| {0}. The crash log could not be written. | {0}. Не удалось записать журнал сбоев. |
| {0}. Details are in {1}. | {0}. Подробности в {1}. |
| {0}. The crash log could not be written. | {0}. Не удалось записать журнал сбоев. |
| Access denied. Windows refused the scan. | Доступ запрещён. Windows отклонил сканирование. |
| Scan stopped. | Сканирование остановлено. |
| Scan cancelled. | Сканирование отменено. |
| Ready | Готово |
| Scan failed ({0}). Details in {1}. | Сбой сканирования ({0}). Подробности в {1}. |
| Scan failed ({0}). The crash log could not be written. | Сбой сканирования ({0}). Не удалось записать журнал сбоев. |

## Main screen text

| English | Русский |
| --- | --- |
| Any unneeded files below are safe to delete. | Любые ненужные файлы ниже можно безопасно удалить. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Они лежат в {InstallerFolder}. InstallerClean спрашивает Windows о каждой установленной программе: файл попадает в список, когда его не заявляет ни одна программа ({0}) или когда его заменило более новое исправление и ни одна программа не смогла бы к нему откатиться ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Переместите их в выбранную вами папку резервных копий, а затем удалите эту папку, когда убедитесь, что ваши программы по-прежнему обновляются и удаляются как обычно. Возвращение их в {InstallerFolder} восстанавливает всё. Или удалите их безвозвратно прямо сейчас. |
| Nothing scanned yet. | Пока ничего не просканировано. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Нажмите «Повторить сканирование», чтобы просмотреть {InstallerFolder} в поисках файлов установщика, которые больше не нужны ни одной программе. |
| Nothing to show until the next scan. | До следующего сканирования показывать нечего. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | Сканирование, которое выполняется после «Переместить» или «Удалить», не завершилось, поэтому нет списка, который можно показать. Нажмите «Повторить сканирование», чтобы снова просканировать {InstallerFolder}. |
| What stopped it is recorded in {0}. | Причина остановки записана в {0}. |
| These files can't be cleaned up right now. | Эти файлы сейчас нельзя очистить. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Сейчас что-то использует Windows Installer, например обновление Windows или программа, устанавливающаяся в фоне. «Переместить» и «Удалить» приостановлены на это время, чтобы InstallerClean не трогал {InstallerFolder}, пока она меняется. Когда всё закончится, повторите сканирование, и они вернутся. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows отказал InstallerClean в праве проверить, занят ли Windows Installer, поэтому «Переместить» и «Удалить» приостановлены, а InstallerClean не будет трогать {InstallerFolder}. Перезагрузите Windows и повторите сканирование. Если Windows по-прежнему отказывает, это не та машина, которую InstallerClean может очистить. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean не смог проверить, занят ли Windows Installer, поэтому «Переместить» и «Удалить» приостановлены, а InstallerClean не будет трогать {InstallerFolder}. Перезагрузите Windows и повторите сканирование. Если проверить по-прежнему не удаётся, это не та машина, которую InstallerClean может очистить. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean не смог проверить, занят ли Windows Installer, потому что Windows выдал ошибку: {0} (ошибка {1}). «Переместить» и «Удалить» приостановлены, а InstallerClean не будет трогать {InstallerFolder}. Перезагрузите Windows и повторите сканирование. Если проверить по-прежнему не удаётся, это не та машина, которую InstallerClean может очистить. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | На этом компьютере приостановлена предыдущая транзакция Windows Installer. Возобновите или откатите эту установку (либо перезагрузите Windows), прежде чем очищать {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows поставил в очередь на следующую перезагрузку переименование файла, которое затрагивает {InstallerFolder}. Перезагрузите Windows, прежде чем очищать. |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | Файловая операция поставлена в очередь до следующей перезагрузки, и InstallerClean не может определить, какие файлы в ней названы, поэтому не может исключить, что они в {InstallerFolder}. Перезагрузите Windows перед очисткой. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean не смог прочитать один из параметров Windows, которые проверяет перед тем, как трогать {InstallerFolder}, поэтому не может определить, выполняется ли операция установщика или ожидает перезагрузки. Перезагрузите Windows и повторите сканирование. Если параметр по-прежнему не читается, это не та машина, которую InstallerClean может очистить. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | У Windows Installer что-то выполняется, поэтому «Переместить» и «Удалить» приостановлены. InstallerClean не будет трогать {InstallerFolder}, пока она меняется. Когда всё закончится, повторите сканирование, и они вернутся. |
| Select a file to view details. | Выберите файл, чтобы посмотреть сведения. |
| Select a product to view details. | Выберите продукт, чтобы посмотреть сведения. |
| No metadata available. | Метаданные недоступны. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Этот установочный файл отсутствует. Сейчас это не доставляет хлопот и не будет доставлять до того дня, когда вы попробуете обновить или удалить программу, которой он принадлежит. Тогда этот шаг может не выполниться, потому что Windows ищет этот файл и не находит его.<br><br>Чтобы вернуть его, вам нужен установщик той версии, которая у вас уже есть. Возьмите его у изготовителя программы и запустите поверх имеющейся копии. Более новая версия не подойдёт: ей пришлось бы сначала удалить ту, что у вас есть, а именно этому шагу и нужен этот файл. Удалить сначала тоже не получится, по той же причине. Это должно восстановить файл и оставить ваши настройки нетронутыми, но Microsoft этого не гарантирует. |
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
| Nothing to clean up in {InstallerFolder} | В {InstallerFolder} нечего очищать |
| Scanned {0} {1} in {2} | Просканировано {0} {1} за {2} |
| Nothing offered on this PC | На этом ПК ничего не предложено |
| One file ({2}) was held back because Windows shows it was changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check it again. | Удержан один файл ({2}), потому что, по данным Windows, он был изменён после того, как InstallerClean начал сканирование. Если в это время устанавливалась или обновлялась программа, повторите сканирование, когда она закончит, и InstallerClean проверит его снова. |
| {0} {1} ({2}) were held back because Windows shows they were changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check them again. | InstallerClean удержал {0} {1} ({2}), потому что, по данным Windows, они были изменены после того, как InstallerClean начал сканирование. Если в это время устанавливалась или обновлялась программа, повторите сканирование, когда она закончит, и InstallerClean проверит их снова. |
| Delete that folder when you're satisfied all is well. | Удалите эту папку, когда убедитесь, что всё в порядке. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | Удалите эту папку, когда убедитесь, что всё в порядке. Только после этого место действительно освободится. |
| {0} freed | Освобождено {0} |
| {0} moved | Перемещено {0} |
| Nothing was moved | Ничего не перемещено |
| Nothing was deleted | Ничего не удалено |
| {0} file could not be moved. | Не удалось переместить {0} файл. |
| {0} files could not be moved. | Не удалось переместить {0} файлов. |
| {0} file could not be deleted. | Не удалось удалить {0} файл. |
| {0} files could not be deleted. | Не удалось удалить {0} файлов. |
| {0} {1} moved to: {2} | Перемещено {0} {1} в: {2} |
| {0} {1} moved to: {2} | Перемещено {0} {1} в: {2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | Задержан {0} файл. Сканирование сочло его ненужным. Итоговая проверка не смогла это подтвердить. |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | Задержано {0} файлов. Сканирование сочло их ненужными. Итоговая проверка не смогла это подтвердить. |
| Moved {0} of {1} {2} to {3} before you cancelled. | Перемещено {0}/{1} {2} в {3} до отмены. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Удалено безвозвратно {0}/{1} {2} до отмены. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | Это легко отменить. Переместите их обратно в {InstallerFolder}, и всё вернётся как было. |
| {0} {1} permanently deleted | Безвозвратно удалён {0} {1} |
| {0} {1} permanently deleted | Безвозвратно удалено {0} {1} |
| Any tip is much appreciated | Любые чаевые очень ценны |
| Send _anonymous report | Отправить _анонимный отчёт |
| The report adds your result to [this chart] on GitHub, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Отчёт добавляет ваш результат в [этот график] на GitHub, который показывает, сколько места люди освобождают с помощью InstallerClean. В нём есть и эти технические сведения, которые помогают мне находить ошибки: |
| How much space was freed, and how many files were moved or deleted | Сколько места освобождено и сколько файлов перемещено или удалено |
| Whether a Move went to the same drive, another drive, a removable drive or a network share | Был ли целью «Переместить» тот же диск, другой диск, съёмный диск или сетевой ресурс |
| How long the scan and the Move or Delete took | Сколько длились сканирование и «Переместить» или «Удалить» |
| How many installer files there are of each kind, and how much space they take | Сколько файлов установщика каждого вида и сколько места они занимают |
| How many files were left alone, and why | Сколько файлов оставлено без изменений и почему |
| How often InstallerClean waited for a slow drive or network share, and how many it stopped waiting for | Как часто InstallerClean ждал медленный диск или сетевой ресурс и скольких из них он перестал ждать |
| How many programs and patches Windows has a record of | О скольких программах и исправлениях у Windows есть запись |
| Whether Windows still makes old-style short file names | Создаёт ли Windows по-прежнему короткие имена файлов старого образца |
| Whether Windows is 10 or 11 and x64 or Arm, and which language and country or region it's set to | Какая версия Windows, 10 или 11, x64 или Arm, и какие в ней заданы язык и страна или регион |
| InstallerClean's version, how you got it (setup, portable, slim, Scoop, the command-line tool on its own, built from source, or not known), whether the report was sent from InstallerClean or the command-line tool, and the language it was showing | Версия InstallerClean, способ её получения (setup, portable, slim, Scoop, отдельная программа командной строки, сборка из исходного кода или неизвестно), откуда отправлен отчёт (из InstallerClean или из программы командной строки) и язык, на котором он отображался |
| Counts of anything that went wrong | Счётчики всего, что пошло не так |
| Each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Каждый из этих пунктов — число или метка. Ничто в отчёте не указывает на вас, ваш ПК, ваши файлы или программы, и я никогда не вижу ваш IP-адрес. |
| See exactly what's sent | Посмотреть, что именно отправляется |

## Summaries and counts

| English | Русский |
| --- | --- |
| {0} file left alone | {0} файл оставлен без изменений |
| {0} files left alone | {0} файлов оставлено без изменений |
| {0} unneeded file to clean up | {0} ненужный файл для очистки |
| {0} unneeded files to clean up | {0} ненужных файлов для очистки |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | У Windows есть запись о {0} файле, которого нет в {InstallerFolder}: {1}. В повседневной работе это не мешает, но обновление или удаление этой программы может не выполниться. Откройте «Подробности», чтобы узнать, что делать. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | У Windows есть записи о {0} файлах, которых нет в {InstallerFolder}: {1}. В повседневной работе это не мешает, но обновление или удаление этих программ может не выполниться. Откройте «Подробности», чтобы узнать, что делать. |
| {0} other program | ещё {0} программа |
| {0} other programs | ещё {0} программ |
| {0} file with no program named in the records | {0} файл, для которого в записях не названа программа |
| {0} files with no program named in the records | {0} файлов, для которых в записях не названа программа |
| InstallerClean carried on without drive {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean продолжил работу без диска {0} и оставил без изменений все файлы, которые ещё нужно было сверить с ним. Когда он снова будет отвечать нормально, нажмите «Повторить сканирование». |
| InstallerClean carried on without {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean продолжил работу без {0} и оставил без изменений все файлы, которые ещё нужно было сверить с тем, что там находится. Когда доступ снова станет нормальным, нажмите «Повторить сканирование». |
| InstallerClean carried on without more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, Re-scan. | InstallerClean продолжил работу без нескольких дисков или сетевых ресурсов ({0}) и оставил без изменений все файлы, которые ещё нужно было сверить с одним из них. Когда они снова будут отвечать нормально, нажмите «Повторить сканирование». |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has finished. | Когда началось сканирование, что-то использовало Windows Installer, например обновление Windows или устанавливающаяся программа, поэтому InstallerClean удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Когда это закончится, нажмите «Повторить сканирование». |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then Re-scan. | Когда началось сканирование, на этом компьютере была приостановлена транзакция Windows Installer, поэтому InstallerClean удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Возобновите или откатите эту установку либо перезагрузите Windows, а затем нажмите «Повторить сканирование». |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Когда началось сканирование, Windows уже поставил в очередь на следующую перезагрузку переименование файла, которое затрагивает {InstallerFolder}, поэтому InstallerClean удержал находящиеся там файлы, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Перезагрузите Windows, а затем нажмите «Повторить сканирование». |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Когда началось сканирование, в очереди до следующей перезагрузки стояла файловая операция, и InstallerClean не смог определить, какие файлы в ней названы, поэтому он удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Перезагрузите Windows, а затем нажмите «Повторить сканирование». |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Когда началось сканирование, Windows отказал InstallerClean в праве проверить, занят ли Windows Installer, поэтому он удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Перезагрузите Windows и нажмите «Повторить сканирование». |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Когда началось сканирование, InstallerClean не смог прочитать один из параметров Windows, которые проверяет, поэтому не мог определить, выполнялась ли операция установщика или ожидала перезагрузки, и удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Перезагрузите Windows и нажмите «Повторить сканирование». |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Когда началось сканирование, InstallerClean не смог проверить, занят ли Windows Installer, поэтому он удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Перезагрузите Windows и нажмите «Повторить сканирование». |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Когда началось сканирование, InstallerClean не смог проверить, занят ли Windows Installer, потому что Windows выдал ошибку: {0} (ошибка {1}). Поэтому он удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Перезагрузите Windows и нажмите «Повторить сканирование». |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has cleared. | Когда началось сканирование, у Windows Installer что-то выполнялось или ожидало, поэтому InstallerClean удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Когда это разрешится, нажмите «Повторить сканирование». |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan. | Во время сканирования часы этого компьютера ушли назад или отстали, поэтому InstallerClean удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Нажмите «Повторить сканирование». |
| {0} of {1} {2} | {0}/{1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} для очистки ({2}) |
| {0} file left alone ({1}) | {0} файл оставлен без изменений ({1}) |
| {0} files left alone ({1}) | {0} файлов оставлено без изменений ({1}) |
| {0} missing | {0} отсутствует |
| {0} missing | {0} отсутствуют |

## Confirmation dialogs

| English | Русский |
| --- | --- |
| Move {0} {1} ({2})? | Переместить {0} {1} ({2})? |
| This file will be moved to: | Этот файл будет перемещён в: |
| These files will be moved to: | Эти файлы будут перемещены в: |
| Delete {0} {1} ({2})? | Удалить {0} {1} ({2})? |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Этот файл будет удалён безвозвратно. Это безопасно, но если хотите резервную копию, воспользуйтесь «Переместить». |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Эти файлы будут удалены безвозвратно. Это безопасно, но если хотите резервную копию, воспользуйтесь «Переместить». |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Эта папка на том же диске, поэтому место не вернётся, пока вы её не удалите. Выберите папку на другом диске, если хотите получить место сразу. |

## Error messages

| English | Русский |
| --- | --- |
| This is also recorded in {0}. | Это также записывается в {0}. |
| Access denied | Доступ запрещён |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows отказал InstallerClean в доступе, поэтому работа была остановлена. Ничего не было удалено.<br><br>InstallerClean уже был запущен от имени администратора, поэтому повторный запуск таким же образом не поможет. Windows не сообщает ничего больше о том, что именно отказало в доступе, поэтому пробовать что-то конкретное бессмысленно. |
| InstallerClean stopped | InstallerClean остановился |
| Scan failed | Сбой сканирования |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Записи Windows Installer вернулись совершенно пустыми: ни одна установленная программа и ни одно обновление не заявляет прав на кэшированный файл установщика. На работающем компьютере такого не бывает (даже у свежей установки Windows такие файлы есть), значит, записи либо повреждены, либо их не удалось прочитать, и сканирование, поверившее такому ответу, ошибочно сочло бы бесхозным каждый файл в {InstallerFolder}. Вместо этого InstallerClean остановился. Ничего не было удалено. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer не позволил InstallerClean перечислить установленное. InstallerClean уже был запущен от имени администратора, поэтому повторный запуск от имени администратора ничего не изменит. Без этого списка невозможно безопасно определить, какие кэшированные файлы ещё нужны, поэтому InstallerClean остановился. Ничего не было удалено. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer не смог предоставить InstallerClean читаемый список установленных программ: он прочитал {1} {2}, а затем одна запись вернулась нечитаемой (код ошибки {0}). Вместо того чтобы работать с прочитанным лишь частично списком, InstallerClean остановился. Ничего не было удалено. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer так и не сообщил о конце списка установленных программ: InstallerClean прочитал {2} {3}, а затем прекратил попытки после {0} записей (последний код ошибки {1}). Списку без конца доверять нельзя, поэтому InstallerClean остановился. Ничего не было удалено. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer так и не сообщил о конце списка исправлений одной программы: InstallerClean прочитал {2} {3}, а затем прекратил попытки после {0} записей (последний код ошибки {1}). Списку без конца доверять нельзя, поэтому InstallerClean остановился. Ничего не было удалено. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean не смог сопоставить записи Windows Installer с содержимым {InstallerFolder}. Почти ничего из того, на что указывают записи, там нет, и почти ничто из того, что там есть, не названо ни одной записью, поэтому ни про один файл не удалось показать, что он не нужен. Ничего не предложено и ничего не убрано. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean не смог сопоставить записи Windows Installer с содержимым {InstallerFolder}. В папке есть файлы, но ни одна запись не указывает ни на что внутри неё, поэтому ни про один файл не удалось показать, что он не нужен. Ничего не предложено и ничего не убрано. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. Nothing has been offered and nothing has been removed. | InstallerClean не может просканировать {InstallerFolder} на этом ПК. Windows сообщает, что эта папка находится в {0}, а InstallerClean сканирует папку Installer, только если Windows указывает для неё её собственный путь. Ничего не предложено и ничего не убрано. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. Nothing has been offered and nothing has been removed. | Windows сообщает, что {InstallerFolder} на этом ПК не существует, поэтому InstallerClean остановился. Ничего не предложено и ничего не убрано. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. Nothing has been offered and nothing has been removed. | Windows сообщил об ошибке, когда InstallerClean попытался получить список файлов в {InstallerFolder}, поэтому InstallerClean не смог получить полный список и остановился. Ничего не предложено и ничего не убрано. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean не смог прочитать достаточно записей Windows Installer, чтобы точно знать, что ещё нужно: список установленных программ вернулся неполным, а чтение тех же записей напрямую из реестра тоже привело к ошибкам. Файл мог выглядеть бесхозным лишь потому, что запись, которая его называет, оказалась одной из нечитаемых, поэтому InstallerClean остановился. Ничего не было удалено. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean не смог получить от Windows настоящий путь к {InstallerFolder}, поэтому ни про один файл не удалось показать, что он находится внутри, и ни один не был предложен для очистки. Это сканирование ничего не нашло из-за неудачи этой проверки, а не потому, что папка чиста. Ничего не убрано. |
| Nothing was deleted | Ничего не удалено |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows отказал InstallerClean в праве проверить, занят ли Windows Installer, поэтому он не смог исключить, что файл понадобится в процессе, и ничего не было удалено. |
| Nothing was moved | Ничего не перемещено |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows отказал InstallerClean в праве проверить, занят ли Windows Installer, поэтому он не смог исключить, что файл понадобится в процессе, и ничего не было перемещено. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean не смог взять блокировку, которой Windows Installer не даёт двум программам одновременно менять установленное ПО, поэтому не смог исключить, что файл понадобится на полпути, и ничего не удалено. Попробуйте ещё раз, а если повторяется — перезагрузите Windows. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean не смог взять блокировку, которой Windows Installer не даёт двум программам одновременно менять установленное ПО, поэтому не смог исключить, что файл понадобится на полпути, и ничего не перемещено. Попробуйте ещё раз, а если повторяется — перезагрузите Windows. |
| Invalid destination | Недопустимая папка назначения |
| Move stopped | Перемещение остановлено |
| Couldn't use that backup folder | Не удалось использовать эту папку резервных копий |
| Move failed | Сбой перемещения |
| Delete failed | Сбой удаления |
| Setting not saved | Настройка не сохранена |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Не удалось сохранить изменение. При следующем запуске InstallerClean вернётся к прежней настройке. |
| The destination cannot be inside the Windows Installer folder. | Папка назначения не может находиться внутри папки Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Назначение {0} разрешается внутри системной папки Windows. Выберите путь вне %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% и %ProgramData%. |
| Not enough space | Недостаточно места |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | В {0} не хватает места<br><br>Требуется: {1}<br>Доступно: {2} |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | Этот файл открыт или заблокирован другой программой, поэтому сейчас его ничем не убрать. Он оставлен на месте; попробуйте позже. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | Эти файлы открыты или заблокированы другой программой, поэтому сейчас их ничем не убрать. Они оставлены на месте; попробуйте позже. |
| Windows reported a file error; the file was left in place. | Windows сообщил об ошибке файла; файл оставлен на месте. |
| Windows reported file errors; these files were left in place. | Windows сообщил об ошибках файлов; эти файлы оставлены на месте. |
| Something went wrong with this file; it was left in place. | С этим файлом что-то пошло не так; он оставлен на месте. |
| Something went wrong with these files; they were left in place. | С этими файлами что-то пошло не так; они оставлены на месте. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Перемещение файлов в папку Windows Installer отклонено (назначение: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Папка резервных копий должна быть полным путём к папке, начинающимся с буквы диска или сетевого ресурса (например, D:\Backup или \\server\backup). InstallerClean не может использовать этот: {0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean больше не смог подтвердить папку резервных копий и остановился. Проверьте {0}, затем «Повторить сканирование» и попробуйте снова. |
| Cannot write to {0}. | Не удаётся выполнить запись в {0}. |
| You can save files in {0} but not open them again, so InstallerClean won't move files there. | Вы можете сохранять файлы в {0}, но не можете снова открыть их, поэтому InstallerClean не будет перемещать туда файлы. |
| A file called '{0}' is already in the backup folder. | Файл с именем «{0}» уже есть в папке резервных копий. |

## Update check

| English | Русский |
| --- | --- |
| Check for updates | Проверка обновлений |
| Checking... | Проверка... |
| Up to date. | Установлена последняя версия. |
| InstallerClean {0} is available. | Доступна версия InstallerClean {0}. |
| It's strongly recommended to use the latest version. | Настоятельно рекомендуется использовать последнюю версию. |
| Download it from GitHub | Скачать с GitHub |
| Download .NET Framework 4.8 from Microsoft | Скачать .NET Framework 4.8 с сайта Microsoft |
| Update now | Обновить сейчас |
| This copy was installed by Scoop. To update it, close InstallerClean and run: | Эта копия установлена через Scoop. Чтобы обновить её, закройте InstallerClean и выполните: |
| This copy was installed by Scoop for all users. To update it, close InstallerClean, open a terminal as administrator and run: | Эта копия установлена через Scoop для всех пользователей. Чтобы обновить её, закройте InstallerClean, откройте терминал от имени администратора и выполните: |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean to update. | Для этой версии нужен .NET Framework 4.8. Установите его с сайта Microsoft, перезагрузите компьютер, а затем откройте InstallerClean, чтобы выполнить обновление. |
| InstallerClean {0} can't be installed automatically. | InstallerClean {0} не может быть установлен автоматически. |
| InstallerClean can't tell whether its setup installed it, so it can't update itself. | InstallerClean не может определить, установлен ли он своим установщиком, поэтому не может обновиться. |
| Copied | Скопировано |
| Couldn't copy it. Select the command and copy it yourself. | Не удалось скопировать. Выделите команду и скопируйте её вручную. |
| _Continue without updating | _Продолжить без обновления |
| Downloading InstallerClean {0}... | Скачивание InstallerClean {0}... |
| Checking the download... | Проверка скачанного файла... |
| The update couldn't be downloaded. | Не удалось скачать обновление. |
| The update couldn't be installed. | Не удалось установить обновление. |
| Couldn't reach GitHub. Check your internet connection and try again. | Не удалось подключиться к GitHub. Проверьте подключение к интернету и повторите попытку. |
| GitHub returned an error response. Try again in a few minutes. | GitHub вернул ответ с ошибкой. Повторите попытку через несколько минут. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | В ответе GitHub не оказалось распознаваемого выпуска. Повторите попытку позже или откройте страницу выпусков напрямую. |
| The check timed out. Your connection to GitHub may be slow; try again. | Время ожидания проверки истекло. Возможно, соединение с GitHub медленное; повторите попытку. |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | Проверка не удалась по неизвестной причине. Подробности в {0}, если нужно сообщить об этом. |
| The check failed for an unknown reason. The crash log could not be written. | Проверка не удалась по неизвестной причине. Не удалось записать журнал сбоев. |

## Opening links in your browser

| English | Русский |
| --- | --- |
| Couldn't open your browser | Не удалось открыть браузер |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | Ссылка в буфере обмена, так что вы можете вставить её сами:<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean также не смог скопировать ссылку в буфер обмена, вот она:<br><br>{0} |

## Startup and crashes

| English | Русский |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | Уже запущен. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Произошла непредвиденная ошибка, и InstallerClean необходимо закрыть.<br><br>{0}<br><br>Подробности записаны в:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Произошла непредвиденная ошибка, и InstallerClean необходимо закрыть.<br><br>{0}<br><br>Не удалось записать журнал сбоев. |
| Startup error | Ошибка запуска |
| Failed to start ({0}). Details written to:<br>{1} | Не удалось запустить ({0}). Подробности записаны в:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Не удалось запустить ({0}). Не удалось записать журнал сбоев. |
| Administrator rights needed | Нужны права администратора |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed.<br><br>Run InstallerClean again as administrator. | InstallerClean нужны права администратора, чтобы видеть все программы, установленные на этом ПК, а он запущен не от имени администратора, поэтому сканировать не будет. Ничего не изменено.<br><br>Запустите InstallerClean снова от имени администратора. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log собирает необработанные исключения InstallerClean.<br># При повышенных правах сообщения исключений платформы могут<br># содержать пути к файлам текущего сеанса (в том числе профили<br># других пользователей, перечисленные запросами Windows Installer).<br># Сообщения о сетевых сбоях при проверке обновлений или отправке<br># журнала результатов могут содержать URL назначения и разрешённый<br># IP-адрес или адрес прокси. Записи о нечитаемых записях Windows<br># Installer могут содержать SID учётной записи Windows<br># (S-1-5-21-...) и коды продуктов установленного ПО.<br># Удалите все три вида сведений, прежде чем прикладывать этот файл<br># к публичному сообщению об ошибке.<br> |

## Tooltips (hover text)

| English | Русский |
| --- | --- |
| Any tip is much appreciated | Любые чаевые очень ценны |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Запрошена отмена. InstallerClean ждёт, когда текущий шаг дойдёт до точки остановки. Это может занять несколько секунд при интенсивном вводе-выводе или обращении к базе данных MSI. |
| Close | Закрыть |
| I really should charge for InstallerClean - because it's been a huge amount of work - but I want to keep it free so it helps as many people as possible. Your donation makes it worthwhile and is very much appreciated. | Вообще-то за InstallerClean стоило бы брать деньги: на него ушло огромное количество труда. Но я хочу, чтобы он оставался бесплатным и помогал как можно большему числу людей. Ваша поддержка придаёт всему этому смысл, и я очень за неё благодарен. |
| A star helps other people find it | Звезда помогает другим найти InstallerClean |
| Minimise | Свернуть |
| Move the unneeded files to the backup folder | Перемещает ненужные файлы в папку резервных копий |
| Move the unneeded files to a backup folder. You'll choose it next. | Перемещает ненужные файлы в папку резервных копий. Вы выберете её следующим шагом. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | Перемещает ненужные файлы в папку резервных копий. Она на том же диске, поэтому место освободится только после того, как вы удалите эту папку. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | Безвозвратно удаляет ненужные файлы. Воспользуйтесь «Переместить», если хотите сначала убедиться, что всё в порядке. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Имя субъекта из встроенного сертификата Authenticode. Цепочка не проверялась. |
| Change language. The program will restart. | Изменить язык. Программа перезапустится. |

## Screen reader labels

| English | Русский |
| --- | --- |
| Leave a tip | Оставить чаевые |
| Leave a tip | Оставить чаевые |
| Leave a tip | Оставить чаевые |
| Cancel operation | Отмена операции |
| Cancel scan | Отмена сканирования |
| Cancel startup scan | Отмена сканирования при запуске |
| Carry on without it, drive {0} | Продолжить без ожидания, диск {0} |
| Carry on without it, {0} | Продолжить без ожидания, {0} |
| Close | Закрыть |
| Close window | Закрыть окно |
| Close result and return to main window | Закрыть результат и вернуться в главное окно |
| Leave a star on github | Поставить звезду на github |
| Minimise | Свернуть |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | «Удалить безвозвратно» убирает ненужные файлы. «Отмена» закрывает окно, ничего не удаляя. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | «Переместить» помещает ненужные файлы в выбранную папку назначения. «Отмена» оставляет их на месте. |
| Say thanks | Поблагодарить |
| Check for updates | Проверить обновления |
| Checks github's releases page for a newer version. | Проверяет на странице выпусков github, есть ли более новая версия. |
| Opens the readme on github in your browser. | Открывает readme на github в вашем браузере. |
| Opens the issue tracker on github.com in your browser. | Открывает список проблем (Issues) на github.com в вашем браузере. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Если флажок установлен, InstallerClean при запуске проверяет на github наличие более новой версии. |
| Download it from github | Скачать с github |
| Download .NET Framework 4.8 from Microsoft | Скачать .NET Framework 4.8 с сайта Microsoft |
| Copy the command | Копировать команду |
| Scoop's update command | Команда обновления Scoop |
| Opens the licence file on github.com in your browser. | Открывает файл лицензии на github.com в вашем браузере. |
| Backup folder | Папка резервных копий |
| Patches | Исправления |
| Product details | Сведения о продукте |
| Backup folder | Папка резервных копий |
| Operation progress | Ход операции |
| Scan {InstallerFolder} again | Сканировать {InstallerFolder} заново |
| Scanning progress | Ход сканирования |
| Startup scan progress | Ход сканирования при запуске |
| Details, unneeded files | Подробности, ненужные файлы |
| Available for cleanup. | Доступны для очистки. |
| Details, files left alone | Подробности, файлы, оставленные без изменений |
| Read-only inventory. | Список только для чтения. |
| Sorted by {0}, ascending | Сортировка по столбцу «{0}», по возрастанию |
| Sorted by {0}, descending | Сортировка по столбцу «{0}», по убыванию |
| Scan results | Результаты сканирования |
| Result details | Сведения о результате |
| File details | Сведения о файле |
| Product details | Сведения о продукте |
| Dialog text | Текст диалогового окна |
| Waiting for a drive or share | Ожидание диска или сетевого ресурса |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Файлы, которые не удалось обработать |
| Explains this folder, and how to recover a file, in the README | Объясняет эту папку и то, как восстановить файл, в README |
| {0} Press Alt+{1} to carry on without it. {2} | {0} Нажмите Alt+{1}, чтобы продолжить без ожидания. {2} |
| What the report holds | Что содержит отчёт |
| What the report holds | Что содержит отчёт |
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
| ,  | ,  |
| drive {0} | диск {0} |
| .  | .  |
| {0} TB | {0} ТБ |
| {0} GB | {0} ГБ |
| {0} MB | {0} МБ |
| {0} KB | {0} КБ |
| {0} B | {0} Б |
| {0:F0}ms | {0:F0} мс |
| {0:F1}s | {0:F1} с |
| less than a second | меньше секунды |
| {0:F1} seconds | {0:F1} секунды |

## Command-line tool (installerclean-cli)

| English | Русский |
| --- | --- |
| Error: unknown argument '{0}' | Ошибка: неизвестный аргумент «{0}» |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Ошибка: неожиданный лишний аргумент «{0}». Если в пути к папке назначения есть пробел, возьмите весь путь в кавычки: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Ошибка: непредвиденный лишний аргумент «{0}». /s и /d не принимают других аргументов, и за один запуск можно использовать только один ключ. |
| Cancelling... | Отмена... |
| Cancelled. | Отменено. |
| Error: unexpected failure ({0}). Details written to {1}. | Ошибка: непредвиденный сбой ({0}). Подробности записаны в {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Ошибка: непредвиденный сбой ({0}). Журнал сбоя записать не удалось. |
| Scanning {InstallerFolder}... | Сканирование {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Найдено {0} ненужных {1} для очистки ({2}). |
| No unneeded files identified. | Ненужных файлов не выявлено. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | У Windows есть запись о {0} файле, которого нет в {InstallerFolder}: {1}. В повседневной работе это не мешает, но обновление или удаление этой программы может не выполниться. Чтобы вернуть файл, вам нужен установщик той версии, которая у вас уже есть. Возьмите его у изготовителя программы и запустите поверх имеющейся копии. Более новая версия не подойдёт: ей пришлось бы сначала удалить ту, что у вас есть, а именно этому шагу и нужен этот файл. Удалить сначала тоже не получится, по той же причине. Это должно восстановить файл и оставить ваши настройки нетронутыми, но Microsoft этого не гарантирует. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | У Windows есть записи о {0} файлах, которых нет в {InstallerFolder}: {1}. В повседневной работе это не мешает, но обновление или удаление этих программ может не выполниться. Чтобы вернуть файл, вам нужен установщик той версии этой программы, которая у вас уже есть. Возьмите его у изготовителя программы и запустите поверх имеющейся копии. Более новая версия не подойдёт: ей пришлось бы сначала удалить ту, что у вас есть, а именно этому шагу и нужен этот файл. Удалить сначала тоже не получится, по той же причине. Это должно восстановить файл и оставить ваши настройки нетронутыми, но Microsoft этого не гарантирует. |
| InstallerClean stopped waiting for drive {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean перестал ждать диск {0} и оставил без изменений все файлы, которые ещё нужно было сверить с ним. Когда он начнёт отвечать нормально, запустите команду снова. |
| InstallerClean stopped waiting for {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean перестал ждать {0} и оставил без изменений все файлы, которые ещё нужно было сверить с тем, что там находится. Когда доступ станет нормальным, запустите команду снова. |
| InstallerClean stopped waiting for more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, run the command again. | InstallerClean перестал ждать несколько дисков или сетевых ресурсов ({0}) и оставил без изменений все файлы, которые ещё нужно было сверить с одним из них. Когда они начнут отвечать нормально, запустите команду снова. |
| Deleting {0} unneeded {1}... | Идёт удаление: {0} ненужных {1}... |
| Permanently deleted {0} unneeded {1}. | Безвозвратно удалено {0} ненужных {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Ошибка: не указана папка назначения для перемещения. Используйте /m ПУТЬ. (Значение по умолчанию, заданное в графическом интерфейсе, действует только для текущего пользователя и не применяется при запуске по расписанию или от имени служебной учётной записи.) |
| Error: destination cannot be inside the Windows Installer folder. | Ошибка: папка назначения не может находиться внутри папки Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Ошибка: папка назначения должна быть полным путём. Получено: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Ошибка: назначение {0} разрешается внутри системной папки Windows. Выберите путь вне %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% и %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Ошибка: недостаточно места в {0}. Для перемещения этих файлов нужно {1}, а свободно {2}. Ничего не перемещено. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Ошибка: сейчас что-то использует Windows Installer, например обновление Windows или программа, устанавливающаяся в фоне. /m и /d заблокированы на это время. Попробуйте снова, когда всё закончится. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Ошибка: на этом компьютере приостановлена предыдущая транзакция Windows Installer. Возобновите или откатите эту установку (либо перезагрузите Windows), прежде чем очищать {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Ошибка: поставленная в очередь после перезагрузки операция с файлом затрагивает {InstallerFolder} ({0}). Перезагрузите Windows, чтобы завершить эту операцию, прежде чем очищать. |
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | Ошибка: файловая операция поставлена в очередь до следующей перезагрузки, и InstallerClean не может определить, какие файлы в ней названы, поэтому не может исключить {InstallerFolder}. Перезагрузите Windows перед очисткой. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Ошибка: InstallerClean не смог прочитать одно из значений реестра, которые проверяет перед тем, как трогать {InstallerFolder}, поэтому не может исключить операцию Windows Installer, выполняющуюся сейчас или поставленную в очередь до следующей перезагрузки. /m и /d заблокированы. Перезагрузите Windows и попробуйте снова. Если чтение по-прежнему не удаётся, это не та машина, которую InstallerClean может очистить. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Ошибка: InstallerClean не смог проверить, занят ли Windows Installer, поэтому не может исключить идущую установку. /m и /d заблокированы. Перезагрузите Windows и попробуйте снова. Если проверка по-прежнему не удаётся, это не та машина, которую InstallerClean может очистить. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Ошибка: InstallerClean не смог проверить, занят ли Windows Installer, потому что Windows выдал ошибку: {0} (ошибка {1}), поэтому не может исключить идущую установку. /m и /d заблокированы. Перезагрузите Windows и попробуйте снова. Если проверка по-прежнему не удаётся, это не та машина, которую InstallerClean может очистить. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Ошибка: у Windows Installer что-то выполняется, поэтому /m и /d заблокированы. InstallerClean не будет трогать {InstallerFolder}, пока она меняется. Попробуйте снова, когда всё закончится. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has finished. | Когда началось сканирование, что-то использовало Windows Installer, например обновление Windows или устанавливающаяся программа, поэтому InstallerClean удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Когда это закончится, запустите команду снова. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then run it again. | Когда началось сканирование, на этом компьютере была приостановлена транзакция Windows Installer, поэтому InstallerClean удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Возобновите или откатите эту установку либо перезагрузите Windows, а затем запустите команду снова. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Когда началось сканирование, Windows уже поставил в очередь на следующую перезагрузку переименование файла, которое затрагивает {InstallerFolder}, поэтому InstallerClean удержал находящиеся там файлы, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Перезагрузите Windows, а затем запустите команду снова. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Когда началось сканирование, в очереди до следующей перезагрузки стояла файловая операция, и InstallerClean не смог определить, какие файлы в ней названы, поэтому он удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Перезагрузите Windows, а затем запустите команду снова. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Когда началось сканирование, Windows отказал InstallerClean в праве проверить, занят ли Windows Installer, поэтому он удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Перезагрузите Windows и запустите команду снова. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Когда началось сканирование, InstallerClean не смог прочитать один из параметров Windows, которые проверяет, поэтому не мог определить, выполнялась ли операция установщика или ожидала перезагрузки, и удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Перезагрузите Windows и запустите команду снова. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Когда началось сканирование, InstallerClean не смог проверить, занят ли Windows Installer, поэтому он удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Перезагрузите Windows и запустите команду снова. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Когда началось сканирование, InstallerClean не смог проверить, занят ли Windows Installer, потому что Windows выдал ошибку: {0} (ошибка {1}). Поэтому он удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Перезагрузите Windows и запустите команду снова. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has cleared. | Когда началось сканирование, у Windows Installer что-то выполнялось или ожидало, поэтому InstallerClean удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Когда это разрешится, запустите команду снова. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again. | Во время сканирования часы этого компьютера ушли назад или отстали, поэтому InstallerClean удержал файлы в {InstallerFolder}, которые не названы в записях ни одной установленной программы, вместо того чтобы предложить их. Запустите команду снова. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Ошибка: InstallerClean не смог взять блокировку Windows Installer, которая не даёт двум программам одновременно менять установленное ПО, поэтому не смог исключить, что файл понадобится на полпути. Ничего не удалено. Попробуйте ещё раз, а если повторяется — перезагрузите Windows. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Ошибка: InstallerClean не смог взять блокировку Windows Installer, которая не даёт двум программам одновременно менять установленное ПО, поэтому не смог исключить, что файл понадобится на полпути. Ничего не перемещено. Попробуйте ещё раз, а если повторяется — перезагрузите Windows. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Ошибка: Windows отказал InstallerClean в праве проверить, занят ли Windows Installer, поэтому он не смог исключить, что файл понадобится в процессе. Ничего не было удалено. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Ошибка: Windows отказал InstallerClean в праве проверить, занят ли Windows Installer, поэтому он не смог исключить, что файл понадобится в процессе. Ничего не было перемещено. |
| Moving {0} unneeded {1} to {2}... | Идёт перемещение в {2}: {0} ненужных {1}... |
| Moved {0} unneeded {1}. | Перемещено {0} ненужных {1}. |
| Check that your programs still update and uninstall as normal, then delete {0}. | Убедитесь, что ваши программы по-прежнему обновляются и удаляются как обычно, а затем удалите {0}. |
| InstallerClean is free. If it helped, a tip is much appreciated: {0} | InstallerClean бесплатен. Если он помог, чаевые будут очень кстати: {0} |
| Update now? It's strongly recommended to use the latest version. [{0}/{1}] | Обновить сейчас? Настоятельно рекомендуется использовать последнюю версию. [{0}/{1}] |
| Y | Д |
| N | Н |
| No answer after a minute, so InstallerClean updates without asking, as it does when nobody is there to answer. | Ответа нет уже минуту, поэтому InstallerClean обновляется без вопросов, как при запуске, когда отвечать некому. |
| Send an anonymous report of this run? It is saved at {0}. Type ? to see what it holds. [{1}/{2}/?] | Отправить анонимный отчёт об этом запуске? Он сохранён в {0}. Введите ?, чтобы увидеть, что в нём. [{1}/{2}/?] |
| the chart at {0} | график по адресу {0} |
| The report adds your result to {0}, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Отчёт добавляет ваш результат в {0}, который показывает, сколько места люди освобождают с помощью InstallerClean. В нём есть и эти технические сведения, которые помогают мне находить ошибки: |
| The full report is printed below. | Полный отчёт выведен ниже. |
| Report sent. Thank you. | Отчёт отправлен. Спасибо. |
| Report not sent. | Отчёт не отправлен. |
| No answer after a minute. | Ответа нет уже минуту. |
| The report could not be sent. It is kept in {0}. | Не удалось отправить отчёт. Он хранится в {0}. |
| Sending a report left waiting by an earlier run. | Отправляется отчёт, ожидающий отправки ещё с более раннего запуска. |
| The new version will scan and list what it finds, without deleting anything. To delete, run /d again afterwards. | Новая версия просканирует и покажет список найденного, ничего не удаляя. Чтобы удалить, затем снова запустите /d. |
| The new version will scan and list what it finds, without moving anything. To move, run /m again afterwards. | Новая версия просканирует и покажет список найденного, ничего не перемещая. Чтобы переместить, затем снова запустите /m. |
| InstallerClean is in use, so it can't update itself now. | InstallerClean используется, поэтому сейчас не может обновиться. |
| Running {0} with InstallerClean {1} instead. | Вместо этого команда {0} будет выполнена в InstallerClean {1}. |
| The newer version couldn't be run. | Не удалось запустить более новую версию. |
| Running {0} with InstallerClean {1}. It takes this copy's place once the run ends. | Команда {0} будет выполнена в InstallerClean {1}. После завершения эта версия заменит текущую копию. |
| {0}: {1} | {0}: {1} |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again to update. | Для этой версии нужен .NET Framework 4.8. Установите его с сайта Microsoft, перезагрузите компьютер, а затем снова запустите InstallerClean, чтобы выполнить обновление. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | Это легко отменить. Переместите их из {0} обратно в {InstallerFolder}, и всё вернётся как было. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean больше не смог подтвердить папку резервных копий и остановился. Проверьте {0}, затем запустите команду снова. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Другой процесс InstallerClean удерживает блокировку единственного экземпляра (GUI или другой запуск CLI). Код выхода 75 (временное состояние); можно повторить попытку позже. |
| Stopped after deleting {0} of {1} {2}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Остановлено после удаления {0}/{1} {2}: InstallerClean больше не мог быть уверен, что никакой другой InstallerClean не выполняет действий. Запустите ту же команду ещё раз. Код выхода 2 (частично). |
| Stopped after moving {0} of {1} {2} to {3}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Остановлено после перемещения {0}/{1} {2} в {3}: InstallerClean больше не мог быть уверен, что никакой другой InstallerClean не выполняет действий. Запустите ту же команду ещё раз. Код выхода 2 (частично). |
| Stopped before deleting any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Остановлено до удаления каких-либо файлов: InstallerClean больше не мог быть уверен, что никакой другой InstallerClean не выполняет действий. Запустите ту же команду ещё раз. Код выхода 75 (временное состояние). |
| Stopped before moving any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Остановлено до перемещения каких-либо файлов: InstallerClean больше не мог быть уверен, что никакой другой InstallerClean не выполняет действий. Запустите ту же команду ещё раз. Код выхода 75 (временное состояние). |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed. Run it again from an administrator prompt. | InstallerClean нужны права администратора, чтобы видеть все программы, установленные на этом ПК, а он запущен не от имени администратора, поэтому сканировать не будет. Ничего не изменено. Запустите его снова из командной строки администратора. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Примечание: не удалось выполнить запись в журнал событий. Проверьте разрешения журнала «Приложение» или групповую политику. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - очистка {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Убирает .msi и .msp из кэша, не нужные ни одной установленной программе. |
| Needs an elevated (administrator) prompt and won't scan without one. | Нужна командная строка администратора; без неё сканирования не будет. |
| Usage: | Использование: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help       Показать эту справку (также /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version    Показать версию (также -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s           Только сканировать - список ненужных |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d           Безвозвратно удалить ненужные файлы |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m           Переместить в сохранённую папку |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ПУТЬ      Переместить в указанный путь |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli удерживает командную строку до конца работы, чтобы<br>скрипт или запланированная задача могли его дождаться. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Папка своя у каждого пользователя; запланированным и SYSTEM: /m ПУТЬ. |
| Before it scans, it checks for a newer version of InstallerClean.<br>When you run it yourself, it asks before updating, and updates if nobody<br>answers within a minute. When it runs on its own, in a scheduled task or<br>a script, it updates without asking.<br>Add /noupdate to skip the check. | Перед сканированием проверяет, есть ли более новая версия InstallerClean.<br>При запуске вручную спрашивает перед обновлением и обновляется, если в<br>течение минуты никто не ответил. При запуске без участия человека, в<br>запланированной задаче или скрипте, обновляется без вопросов.<br>Добавьте /noupdate, чтобы пропустить проверку. |
| Exit codes: | Коды выхода: |
|   0   success: the run did what it was asked |   0   успех: запуск сделал то, о чём просили |
|   1   nothing processed: the run failed or was refused |   1   ничего не обработано: запуск завершился сбоем или был отклонён |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   частично: часть обработана, часть нет (сбой или Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  временно: запуск заблокирован временным состоянием (см. сообщение) |
|   130 cancelled (Ctrl+C) |   130 отменено (Ctrl+C) |
