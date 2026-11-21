# InstallerClean in Русский (Russian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Russian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Russian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.ru.resx`](../../src/InstallerClean.Core/Resources/Strings.ru.resx), so do not edit it by hand. The Russian translation itself lives in [`gen-strings-ru.mjs`](../../scripts/translations/gen-strings-ru.mjs).

Where Russian needs a different form of a line for some counts, the form is in a row of its own directly under the line, marked _(one form)_, _(few form)_ or _(many form)_. The app shows it in place of that line at the counts it is written for.

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
| Third-party notices | Уведомления третьих сторон |

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
| Third-party notices | Уведомления третьих сторон |
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
| Any file InstallerClean can't check without it will be left alone. | Any file InstallerClean can't check without it will be left alone. |
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
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed as safe to delete when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Они лежат в {InstallerFolder}. InstallerClean спрашивает Windows о каждой установленной программе: файл попадает в список тех, которые можно безопасно удалить, когда его не заявляет ни одна программа ({0}) или когда его заменило более новое исправление и ни одна программа не смогла бы к нему откатиться ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Переместите их в выбранную вами папку резервных копий, а затем удалите эту папку, когда убедитесь, что ваши программы по-прежнему обновляются и удаляются как обычно. Возвращение их в {InstallerFolder} восстанавливает всё. Или удалите их безвозвратно прямо сейчас. |
| Nothing scanned yet. | Пока ничего не просканировано. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Нажмите «Повторить сканирование», чтобы просмотреть {InstallerFolder} в поисках файлов установщика, которые больше не нужны ни одной программе. |
| Nothing to show until the next scan. | До следующего сканирования показывать нечего. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | Сканирование, которое выполняется после «Переместить» или «Удалить», не завершилось, поэтому нет списка, который можно показать. Нажмите «Повторить сканирование», чтобы снова просканировать {InstallerFolder}. |
| What stopped it is recorded in {0}. | Причина остановки записана в {0}. |
| These files can't be cleaned up right now. | Эти файлы сейчас нельзя очистить. |
| Something is using Windows Installer right now, such as Windows Update or a program being installed or removed. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} until it has finished. Then Re-scan and they come back. | Сейчас что-то использует Windows Installer, например Центр обновления Windows или программа, которую устанавливают или удаляют. «Переместить» и «Удалить» приостановлены на это время, поэтому InstallerClean не будет трогать {InstallerFolder}, пока это не закончится. Затем повторите сканирование, и они вернутся. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. InstallerClean will check again the next time you start it. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. InstallerClean will check again the next time you start it. If Windows still refuses, this isn't a machine InstallerClean can clean. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. InstallerClean will check again the next time you start it. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. InstallerClean will check again the next time you start it. If it still can't check, this isn't a machine InstallerClean can clean. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. InstallerClean will check again the next time you start it. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. InstallerClean will check again the next time you start it. If it still can't check, this isn't a machine InstallerClean can clean. |
| An earlier Windows Installer installation hasn't finished, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. An installation waiting for a restart carries on after it, and InstallerClean will check again the next time you start it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. | An earlier Windows Installer installation hasn't finished, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. An installation waiting for a restart carries on after it, and InstallerClean will check again the next time you start it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. |
| Windows has a change to {InstallerFolder} waiting for the next restart, so Move and Delete are paused. Restart Windows. InstallerClean will check again the next time you start it. | Windows has a change to {InstallerFolder} waiting for the next restart, so Move and Delete are paused. Restart Windows. InstallerClean will check again the next time you start it. |
| Windows has a file change waiting for the next restart, and InstallerClean can't tell which files it affects, so it can't rule out that they're in {InstallerFolder}. Move and Delete are paused. Restart Windows. InstallerClean will check again the next time you start it. | Windows has a file change waiting for the next restart, and InstallerClean can't tell which files it affects, so it can't rule out that they're in {InstallerFolder}. Move and Delete are paused. Restart Windows. InstallerClean will check again the next time you start it. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether Windows Installer is busy or has work waiting for a restart. Move and Delete are paused. Restart Windows. InstallerClean will check again the next time you start it. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether Windows Installer is busy or has work waiting for a restart. Move and Delete are paused. Restart Windows. InstallerClean will check again the next time you start it. If the setting still won't read, this isn't a machine InstallerClean can clean. |
| Windows Installer has something unfinished, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Once it has cleared, Re-scan and they come back. | У Windows Installer есть что-то незавершённое, поэтому «Переместить» и «Удалить» приостановлены, а InstallerClean не будет трогать {InstallerFolder}. Когда это разрешится, повторите сканирование, и они вернутся. |
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
| No files can be deleted | Ни один файл нельзя удалить |
| One file ({2}) was left alone because Windows shows it changed after the scan started. If something was installing or updating, scan again once it has finished. | Один файл ({2}) оставлен без изменений, потому что, по данным Windows, он изменился после начала сканирования. Если что-то устанавливалось или обновлялось, повторите сканирование, когда это закончится. |
| {0} {1} ({2}) were left alone because Windows shows they changed after the scan started. If something was installing or updating, scan again once it has finished. | InstallerClean оставил без изменений {0} {1} ({2}), потому что, по данным Windows, они изменились после начала сканирования. Если что-то устанавливалось или обновлялось, повторите сканирование, когда это закончится. |
| Delete that folder when you're satisfied all is well. | Удалите эту папку, когда убедитесь, что всё в порядке. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | Удалите эту папку, когда убедитесь, что всё в порядке. Только после этого место действительно освободится. |
| {0} freed | Освобождено {0} |
| {0} moved | Перемещено {0} |
| Nothing was moved | Ничего не перемещено |
| Nothing was deleted | Ничего не удалено |
| {0} file could not be moved. | Не удалось переместить {0} файл. |
| {0} files could not be moved. | Не удалось переместить {0} файлов. |
| {0} files could not be moved.<br>_(few form)_ | Не удалось переместить {0} файла. |
| {0} file could not be deleted. | Не удалось удалить {0} файл. |
| {0} files could not be deleted. | Не удалось удалить {0} файлов. |
| {0} files could not be deleted.<br>_(few form)_ | Не удалось удалить {0} файла. |
| {0} {1} moved to: {2} | Перемещено {0} {1} в: {2} |
| {0} {1} moved to: {2} | Перемещено {0} {1} в: {2} |
| {0} file left alone. The scan listed it as safe to delete, but the final check before moving or deleting couldn't confirm it. | {0} file left alone. The scan listed it as safe to delete, but the final check before moving or deleting couldn't confirm it. |
| {0} files left alone. The scan listed them as safe to delete, but the final check before moving or deleting couldn't confirm it. | {0} files left alone. The scan listed them as safe to delete, but the final check before moving or deleting couldn't confirm it. |
| {0} files left alone. The scan listed them as safe to delete, but the final check before moving or deleting couldn't confirm it.<br>_(few form)_ | {0} files left alone. The scan listed them as safe to delete, but the final check before moving or deleting couldn't confirm it. |
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
| InstallerClean's version, how you got it (setup, portable, Scoop, the command-line tool on its own, built from source, or not known), whether the run was in InstallerClean or the command-line tool, and the language it was showing | Версия InstallerClean, способ её получения (setup, portable, Scoop, отдельная программа командной строки, сборка из исходного кода или неизвестно), где выполнялся запуск (в InstallerClean или в программе командной строки) и язык, на котором отображался интерфейс |
| Counts of anything that went wrong | Счётчики всего, что пошло не так |
| A random code made for this report, so that it's only counted once if it's sent more than once | Случайный код, созданный для этого отчёта, чтобы он учитывался только один раз, если будет отправлен несколько раз |
| Apart from that code, each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Кроме этого кода, каждый из этих пунктов — число или метка. Ничто в отчёте не указывает на вас, ваш ПК, ваши файлы или программы, и я никогда не вижу ваш IP-адрес. |
| See exactly what's sent | Посмотреть, что именно отправляется |

## Summaries and counts

| English | Русский |
| --- | --- |
| {0} file left alone | {0} файл оставлен без изменений |
| {0} files left alone | {0} файлов оставлено без изменений |
| {0} files left alone<br>_(few form)_ | {0} файла оставлено без изменений |
| {0} unneeded file to clean up | {0} ненужный файл для очистки |
| {0} unneeded files to clean up | {0} ненужных файлов для очистки |
| {0} unneeded files to clean up<br>_(few form)_ | {0} ненужных файла для очистки |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | У Windows есть запись о {0} файле, которого нет в {InstallerFolder}: {1}. В повседневной работе это не мешает, но обновление или удаление этой программы может не выполниться. Откройте «Подробности», чтобы узнать, что делать. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | У Windows есть записи о {0} файлах, которых нет в {InstallerFolder}: {1}. В повседневной работе это не мешает, но обновление или удаление этих программ может не выполниться. Откройте «Подробности», чтобы узнать, что делать. |
| {0} other program | ещё {0} программа |
| {0} other programs | ещё {0} программ |
| {0} other programs<br>_(few form)_ | ещё {0} программы |
| {0} file with no program named in the records | {0} файл, для которого в записях не названа программа |
| {0} files with no program named in the records | {0} файлов, для которых в записях не названа программа |
| {0} files with no program named in the records<br>_(few form)_ | {0} файла, для которых в записях не названа программа |
| InstallerClean stopped waiting for drive {0}, so it left alone any file it couldn't check without it. Re-scan once it's responding normally. | InstallerClean stopped waiting for drive {0}, so it left alone any file it couldn't check without it. Re-scan once it's responding normally. |
| InstallerClean stopped waiting for {0}, so it left alone any file it couldn't check without it. Re-scan once it's responding normally. | InstallerClean stopped waiting for {0}, so it left alone any file it couldn't check without it. Re-scan once it's responding normally. |
| InstallerClean stopped waiting for more than one drive or share ({0}), so it left alone any file it couldn't check without them. Re-scan once they're responding normally. | InstallerClean stopped waiting for more than one drive or share ({0}), so it left alone any file it couldn't check without them. Re-scan once they're responding normally. |
| Windows Installer was busy when the scan started, for example with Windows Update or a program being installed or removed, so InstallerClean left alone the files it can't check while it's busy. Re-scan once it has finished. | Windows Installer was busy when the scan started, for example with Windows Update or a program being installed or removed, so InstallerClean left alone the files it can't check while it's busy. Re-scan once it has finished. |
| An earlier Windows Installer installation hadn't finished when the scan started, so InstallerClean left alone the files it can't check until it has. Restart Windows. An installation waiting for a restart carries on after it, and InstallerClean will check again the next time you start it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. | An earlier Windows Installer installation hadn't finished when the scan started, so InstallerClean left alone the files it can't check until it has. Restart Windows. An installation waiting for a restart carries on after it, and InstallerClean will check again the next time you start it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. |
| Windows had a change to {InstallerFolder} waiting for the next restart when the scan started, so InstallerClean left alone the files it can't check until that change is made. Restart Windows. InstallerClean will check again the next time you start it. | Windows had a change to {InstallerFolder} waiting for the next restart when the scan started, so InstallerClean left alone the files it can't check until that change is made. Restart Windows. InstallerClean will check again the next time you start it. |
| Windows had a file change waiting for the next restart when the scan started, and InstallerClean couldn't tell which files it affects, so it left alone the files it can't check until that change is made. Restart Windows. InstallerClean will check again the next time you start it. | Windows had a file change waiting for the next restart when the scan started, and InstallerClean couldn't tell which files it affects, so it left alone the files it can't check until that change is made. Restart Windows. InstallerClean will check again the next time you start it. |
| Windows refused InstallerClean permission to check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. | Windows refused InstallerClean permission to check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. |
| InstallerClean couldn't read a Windows setting it checks, so it couldn't tell whether Windows Installer was busy or had work waiting for a restart when the scan started. It left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. | InstallerClean couldn't read a Windows setting it checks, so it couldn't tell whether Windows Installer was busy or had work waiting for a restart when the scan started. It left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. |
| InstallerClean couldn't check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. | InstallerClean couldn't check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. |
| InstallerClean couldn't check whether Windows Installer was busy when the scan started. Windows gave an error: {0} (error {1}). It left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. | InstallerClean couldn't check whether Windows Installer was busy when the scan started. Windows gave an error: {0} (error {1}). It left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. |
| Windows Installer had something unfinished when the scan started, so InstallerClean left alone the files it can't check until it has finished. Re-scan once it has finished. | Windows Installer had something unfinished when the scan started, so InstallerClean left alone the files it can't check until it has finished. Re-scan once it has finished. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean couldn't tell which files had just been added, and left alone every file that could have been. Re-scan. | Во время сканирования часы этого компьютера ушли назад или отстали, поэтому InstallerClean не смог определить, какие файлы были только что добавлены, и оставил без изменений все файлы, которые могли оказаться среди них. Нажмите «Повторить сканирование». |
| {0} of {1} {2} | {0}/{1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} для очистки ({2}) |
| {0} file left alone ({1}) | {0} файл оставлен без изменений ({1}) |
| {0} files left alone ({1}) | {0} файлов оставлено без изменений ({1}) |
| {0} files left alone ({1})<br>_(few form)_ | {0} файла оставлено без изменений ({1}) |
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
| Nothing has been removed. | Ничего не было удалено. |
| No file has been listed as safe to delete and nothing has been removed. | Ни один файл не включён в список тех, которые можно безопасно удалить, и ничего не убрано. |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. | Записи Windows Installer вернулись совершенно пустыми: ни одна установленная программа и ни одно обновление не заявляет прав на кэшированный файл установщика. На работающем компьютере такого не бывает (даже у свежей установки Windows такие файлы есть), значит, записи либо повреждены, либо их не удалось прочитать, и сканирование, поверившее такому ответу, ошибочно сочло бы бесхозным каждый файл в {InstallerFolder}. Вместо этого InstallerClean остановился. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. | Windows Installer не позволил InstallerClean перечислить установленное. InstallerClean уже был запущен от имени администратора, поэтому повторный запуск от имени администратора ничего не изменит. Без этого списка невозможно безопасно определить, какие кэшированные файлы ещё нужны, поэтому InstallerClean остановился. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. | Windows Installer не смог предоставить InstallerClean читаемый список установленных программ: он прочитал {1} {2}, а затем одна запись вернулась нечитаемой (код ошибки {0}). Вместо того чтобы работать с прочитанным лишь частично списком, InstallerClean остановился. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. | Windows Installer так и не сообщил о конце списка установленных программ: InstallerClean прочитал {2} {3}, а затем прекратил попытки после {0} записей (последний код ошибки {1}). Списку без конца доверять нельзя, поэтому InstallerClean остановился. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. | Windows Installer так и не сообщил о конце списка исправлений одной программы: InstallerClean прочитал {2} {3}, а затем прекратил попытки после {0} записей (последний код ошибки {1}). Списку без конца доверять нельзя, поэтому InstallerClean остановился. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. | InstallerClean не смог сопоставить записи Windows Installer с содержимым {InstallerFolder}. Почти ничего из того, на что указывают записи, там нет, и почти ничто из того, что там есть, не названо ни одной записью, поэтому ни про один файл не удалось показать, что он не нужен. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. | InstallerClean не смог сопоставить записи Windows Installer с содержимым {InstallerFolder}. В папке есть файлы, но ни одна запись не указывает ни на что внутри неё, поэтому ни про один файл не удалось показать, что он не нужен. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. | InstallerClean не может просканировать {InstallerFolder} на этом ПК. Windows сообщает, что эта папка находится в {0}, а InstallerClean сканирует папку Installer, только если Windows указывает для неё её собственный путь. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. | Windows сообщает, что {InstallerFolder} на этом ПК не существует, поэтому InstallerClean остановился. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. | Windows сообщил об ошибке, когда InstallerClean попытался получить список файлов в {InstallerFolder}, поэтому InstallerClean не смог получить полный список и остановился. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. | InstallerClean не смог прочитать достаточно записей Windows Installer, чтобы точно знать, что ещё нужно: список установленных программ вернулся неполным, а чтение тех же записей напрямую из реестра тоже привело к ошибкам. Файл мог выглядеть бесхозным лишь потому, что запись, которая его называет, оказалась одной из нечитаемых, поэтому InstallerClean остановился. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was listed as safe to delete. This scan found nothing because that check failed, not because the folder is clean. | InstallerClean не смог получить от Windows настоящий путь к {InstallerFolder}, поэтому ни про один файл не удалось показать, что он находится внутри, и ни один не был включён в список тех, которые можно безопасно удалить. Это сканирование ничего не нашло из-за неудачи этой проверки, а не потому, что папка чиста. |
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
| The update check received an error instead of GitHub's releases page. Try again later, or open [the releases page] in your browser. | The update check received an error instead of GitHub's releases page. Try again later, or open [the releases page] in your browser. |
| The update check couldn't tell which version is the newest from GitHub's answer. Try again later, or open [the releases page] in your browser. | The update check couldn't tell which version is the newest from GitHub's answer. Try again later, or open [the releases page] in your browser. |
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
| InstallerClean won't start because there is a file called {0} beside it, and InstallerClean doesn't come with one. Delete that file, then open InstallerClean again.<br><br>{1} | InstallerClean не запустится, потому что рядом с ним находится файл с именем {0}, а InstallerClean не поставляется с таким файлом. Удалите этот файл, а затем снова запустите InstallerClean.<br><br>{1} |
| InstallerClean needs Windows 10 version 1607 or later, or Windows 11. | Для InstallerClean нужна Windows 10 версии 1607 или новее либо Windows 11. |
| InstallerClean needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean again. | Для InstallerClean нужен .NET Framework 4.8. Установите его с сайта Microsoft, перезагрузите компьютер, а затем снова откройте InstallerClean. |
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
| Shows the licences of the fonts, icons and code InstallerClean includes. | Показывает лицензии шрифтов, значков и кода, входящих в InstallerClean. |
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
| files<br>_(few form)_ | файла |
| error | ошибка |
| errors | ошибок |
| errors<br>_(few form)_ | ошибки |
| package | пакет |
| packages | пакетов |
| packages<br>_(few form)_ | пакета |
| product | продукт |
| products | продуктов |
| products<br>_(few form)_ | продукта |
| patch | исправление |
| patches | исправлений |
| patches<br>_(few form)_ | исправления |

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
| InstallerClean needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again. | Для InstallerClean нужен .NET Framework 4.8. Установите его с сайта Microsoft, перезагрузите компьютер, а затем снова запустите InstallerClean. |
| Error: unknown argument '{0}' | Ошибка: неизвестный аргумент «{0}» |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Ошибка: неожиданный лишний аргумент «{0}». Если в пути к папке назначения есть пробел, возьмите весь путь в кавычки: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Ошибка: непредвиденный лишний аргумент «{0}». /s и /d не принимают других аргументов, и за один запуск можно использовать только один ключ. |
| Cancelling... | Отмена... |
| Cancelled. | Отменено. |
| Error: unexpected failure ({0}). Details written to {1}. | Ошибка: непредвиденный сбой ({0}). Подробности записаны в {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Ошибка: непредвиденный сбой ({0}). Журнал сбоя записать не удалось. |
| Scanning {InstallerFolder}... | Сканирование {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Найдено {0} ненужных {1} для очистки ({2}). |
| Found {0} unneeded {1} to clean up ({2}).<br>_(one form)_ | Найден {0} ненужный {1} для очистки ({2}). |
| No unneeded files identified. | Ненужных файлов не выявлено. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | У Windows есть запись о {0} файле, которого нет в {InstallerFolder}: {1}. В повседневной работе это не мешает, но обновление или удаление этой программы может не выполниться. Чтобы вернуть файл, вам нужен установщик той версии, которая у вас уже есть. Возьмите его у изготовителя программы и запустите поверх имеющейся копии. Более новая версия не подойдёт: ей пришлось бы сначала удалить ту, что у вас есть, а именно этому шагу и нужен этот файл. Удалить сначала тоже не получится, по той же причине. Это должно восстановить файл и оставить ваши настройки нетронутыми, но Microsoft этого не гарантирует. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | У Windows есть записи о {0} файлах, которых нет в {InstallerFolder}: {1}. В повседневной работе это не мешает, но обновление или удаление этих программ может не выполниться. Чтобы вернуть файл, вам нужен установщик той версии этой программы, которая у вас уже есть. Возьмите его у изготовителя программы и запустите поверх имеющейся копии. Более новая версия не подойдёт: ей пришлось бы сначала удалить ту, что у вас есть, а именно этому шагу и нужен этот файл. Удалить сначала тоже не получится, по той же причине. Это должно восстановить файл и оставить ваши настройки нетронутыми, но Microsoft этого не гарантирует. |
| InstallerClean stopped waiting for drive {0}, so it left alone any file it couldn't check without it. Once it's responding normally, run the command again. | InstallerClean stopped waiting for drive {0}, so it left alone any file it couldn't check without it. Once it's responding normally, run the command again. |
| InstallerClean stopped waiting for {0}, so it left alone any file it couldn't check without it. Once it's responding normally, run the command again. | InstallerClean stopped waiting for {0}, so it left alone any file it couldn't check without it. Once it's responding normally, run the command again. |
| InstallerClean stopped waiting for more than one drive or share ({0}), so it left alone any file it couldn't check without them. Once they're responding normally, run the command again. | InstallerClean stopped waiting for more than one drive or share ({0}), so it left alone any file it couldn't check without them. Once they're responding normally, run the command again. |
| Deleting {0} unneeded {1}... | Идёт удаление: {0} ненужных {1}... |
| Deleting {0} unneeded {1}...<br>_(one form)_ | Идёт удаление: {0} ненужный {1}... |
| Permanently deleted {0} unneeded {1}. | Безвозвратно удалено {0} ненужных {1}. |
| Permanently deleted {0} unneeded {1}.<br>_(one form)_ | Безвозвратно удалён {0} ненужный {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Ошибка: не указана папка назначения для перемещения. Используйте /m ПУТЬ. (Значение по умолчанию, заданное в графическом интерфейсе, действует только для текущего пользователя и не применяется при запуске по расписанию или от имени служебной учётной записи.) |
| Error: destination cannot be inside the Windows Installer folder. | Ошибка: папка назначения не может находиться внутри папки Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Ошибка: папка назначения должна быть полным путём. Получено: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Ошибка: назначение {0} разрешается внутри системной папки Windows. Выберите путь вне %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% и %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Ошибка: недостаточно места в {0}. Для перемещения этих файлов нужно {1}, а свободно {2}. Ничего не перемещено. |
| Error: something is using Windows Installer right now, such as Windows Update or a program being installed or removed. /m and /d are blocked while that runs. Try again once it has finished. | Ошибка: сейчас что-то использует Windows Installer, например Центр обновления Windows или программа, которую устанавливают или удаляют. /m и /d заблокированы на это время. Попробуйте снова, когда это закончится. |
| Error: an earlier Windows Installer installation hasn't finished. /m and /d are blocked, so InstallerClean won't touch {InstallerFolder}. Restart Windows, then run the command again. An installation waiting for a restart carries on after it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. | Error: an earlier Windows Installer installation hasn't finished. /m and /d are blocked, so InstallerClean won't touch {InstallerFolder}. Restart Windows, then run the command again. An installation waiting for a restart carries on after it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. |
| Error: Windows has a change to {InstallerFolder} waiting for the next restart ({0}). /m and /d are blocked. Restart Windows, then try again. | Ошибка: Windows ожидает следующей перезагрузки, чтобы внести изменение в {InstallerFolder} ({0}). /m и /d заблокированы. Перезагрузите Windows, а затем попробуйте снова. |
| Error: Windows has a file change waiting for the next restart, and InstallerClean can't tell which files it affects, so it can't rule out that they're in {InstallerFolder}. /m and /d are blocked. Restart Windows, then try again. | Ошибка: Windows ожидает следующей перезагрузки, чтобы изменить какие-то файлы, и InstallerClean не может определить, какие именно, поэтому не может исключить, что они в {InstallerFolder}. /m и /d заблокированы. Перезагрузите Windows, а затем попробуйте снова. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't tell whether Windows Installer is busy or has work waiting for a restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Ошибка: InstallerClean не смог прочитать одно из значений реестра, которые проверяет перед тем, как трогать {InstallerFolder}, поэтому не может определить, занят ли Windows Installer или есть ли у него работа, ожидающая перезагрузки. /m и /d заблокированы. Перезагрузите Windows и попробуйте снова. Если чтение по-прежнему не удаётся, это не та машина, которую InstallerClean может очистить. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Ошибка: InstallerClean не смог проверить, занят ли Windows Installer, поэтому не может исключить идущую установку. /m и /d заблокированы. Перезагрузите Windows и попробуйте снова. Если проверка по-прежнему не удаётся, это не та машина, которую InstallerClean может очистить. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Ошибка: InstallerClean не смог проверить, занят ли Windows Installer, потому что Windows выдал ошибку: {0} (ошибка {1}), поэтому не может исключить идущую установку. /m и /d заблокированы. Перезагрузите Windows и попробуйте снова. Если проверка по-прежнему не удаётся, это не та машина, которую InstallerClean может очистить. |
| Error: Windows Installer has something unfinished, so /m and /d are blocked and InstallerClean won't touch {InstallerFolder}. Try again once it has cleared. | Ошибка: у Windows Installer есть что-то незавершённое, поэтому /m и /d заблокированы, а InstallerClean не будет трогать {InstallerFolder}. Попробуйте снова, когда это разрешится. |
| Windows Installer was busy when the scan started, for example with Windows Update or a program being installed or removed, so InstallerClean left alone the files it can't check while it's busy. Run the command again once it has finished. | Windows Installer was busy when the scan started, for example with Windows Update or a program being installed or removed, so InstallerClean left alone the files it can't check while it's busy. Run the command again once it has finished. |
| An earlier Windows Installer installation hadn't finished when the scan started, so InstallerClean left alone the files it can't check until it has. Restart Windows, then run the command again. An installation waiting for a restart carries on after it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. | An earlier Windows Installer installation hadn't finished when the scan started, so InstallerClean left alone the files it can't check until it has. Restart Windows, then run the command again. An installation waiting for a restart carries on after it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. |
| Windows had a change to {InstallerFolder} waiting for the next restart when the scan started, so InstallerClean left alone the files it can't check until that change is made. Restart Windows, then run the command again. | Windows had a change to {InstallerFolder} waiting for the next restart when the scan started, so InstallerClean left alone the files it can't check until that change is made. Restart Windows, then run the command again. |
| Windows had a file change waiting for the next restart when the scan started, and InstallerClean couldn't tell which files it affects, so it left alone the files it can't check until that change is made. Restart Windows, then run the command again. | Windows had a file change waiting for the next restart when the scan started, and InstallerClean couldn't tell which files it affects, so it left alone the files it can't check until that change is made. Restart Windows, then run the command again. |
| Windows refused InstallerClean permission to check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. | Windows refused InstallerClean permission to check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. |
| InstallerClean couldn't read a Windows setting it checks, so it couldn't tell whether Windows Installer was busy or had work waiting for a restart when the scan started. It left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. | InstallerClean couldn't read a Windows setting it checks, so it couldn't tell whether Windows Installer was busy or had work waiting for a restart when the scan started. It left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. |
| InstallerClean couldn't check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. | InstallerClean couldn't check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. |
| InstallerClean couldn't check whether Windows Installer was busy when the scan started. Windows gave an error: {0} (error {1}). It left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. | InstallerClean couldn't check whether Windows Installer was busy when the scan started. Windows gave an error: {0} (error {1}). It left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. |
| Windows Installer had something unfinished when the scan started, so InstallerClean left alone the files it can't check until it has finished. Run the command again once it has finished. | Windows Installer had something unfinished when the scan started, so InstallerClean left alone the files it can't check until it has finished. Run the command again once it has finished. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean couldn't tell which files had just been added, and left alone every file that could have been. Run the command again. | This PC's clock went back or lost time while the scan was running, so InstallerClean couldn't tell which files had just been added, and left alone every file that could have been. Run the command again. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Ошибка: InstallerClean не смог взять блокировку Windows Installer, которая не даёт двум программам одновременно менять установленное ПО, поэтому не смог исключить, что файл понадобится на полпути. Ничего не удалено. Попробуйте ещё раз, а если повторяется — перезагрузите Windows. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Ошибка: InstallerClean не смог взять блокировку Windows Installer, которая не даёт двум программам одновременно менять установленное ПО, поэтому не смог исключить, что файл понадобится на полпути. Ничего не перемещено. Попробуйте ещё раз, а если повторяется — перезагрузите Windows. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Ошибка: Windows отказал InstallerClean в праве проверить, занят ли Windows Installer, поэтому он не смог исключить, что файл понадобится в процессе. Ничего не было удалено. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Ошибка: Windows отказал InstallerClean в праве проверить, занят ли Windows Installer, поэтому он не смог исключить, что файл понадобится в процессе. Ничего не было перемещено. |
| Moving {0} unneeded {1} to {2}... | Идёт перемещение в {2}: {0} ненужных {1}... |
| Moving {0} unneeded {1} to {2}...<br>_(one form)_ | Идёт перемещение в {2}: {0} ненужный {1}... |
| Moved {0} unneeded {1}. | Перемещено {0} ненужных {1}. |
| Moved {0} unneeded {1}.<br>_(one form)_ | Перемещён {0} ненужный {1}. |
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
| Cancelled while sending the report. It may have arrived anyway. | Отменено во время отправки отчёта. Он всё равно мог дойти. |
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
|   installerclean-cli --notices  Print the third-party notices |   installerclean-cli --notices    Показать уведомления третьих сторон |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s           Только сканировать - список ненужных |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d           Безвозвратно удалить ненужные файлы |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m           Переместить в сохранённую папку |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ПУТЬ      Переместить в указанный путь |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli удерживает командную строку до конца работы, чтобы<br>скрипт или запланированная задача могли его дождаться. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Папка своя у каждого пользователя; запланированным и SYSTEM: /m ПУТЬ. |
| Before it scans, it checks for a newer version of InstallerClean, unless<br>"Check for updates automatically" is turned off for the account it runs<br>as. Where its window is on screen and nothing is piped into or out of it,<br>it asks before updating, and updates if nobody answers within a minute.<br>Anywhere else, such as a scheduled task set to run whether or not anyone<br>is logged on, it updates without asking.<br>Add /noupdate to skip the check. | Перед сканированием проверяет, есть ли более новая версия InstallerClean,<br>если только параметр «Автоматически проверять обновления» не отключён для<br>учётной записи, от имени которой запущена программа. Если её окно открыто<br>на экране, а ввод и вывод не перенаправлены, спрашивает перед обновлением<br>и обновляется, если в течение минуты никто не ответил. Во всех остальных<br>случаях, например в запланированной задаче, настроенной на запуск<br>независимо от того, вошёл ли кто-нибудь в систему, обновляется без<br>вопросов. Добавьте /noupdate, чтобы пропустить проверку. |
| Exit codes: | Коды выхода: |
|   0   success: the run did what it was asked |   0   успех: запуск сделал то, о чём просили |
|   1   nothing processed: the run failed or was refused |   1   ничего не обработано: запуск завершился сбоем или был отклонён |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   частично: часть обработана, часть нет (сбой или Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  временно: запуск заблокирован временным состоянием (см. сообщение) |
|   130 cancelled (Ctrl+C) |   130 отменено (Ctrl+C) |
