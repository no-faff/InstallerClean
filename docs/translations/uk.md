# InstallerClean in Українська (Ukrainian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Ukrainian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Ukrainian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.uk.resx`](../../src/InstallerClean.Core/Resources/Strings.uk.resx), so do not edit it by hand. The Ukrainian translation itself lives in [`gen-strings-uk.mjs`](../../scripts/translations/gen-strings-uk.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Українська |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Про програму |
| Registered files that should not be deleted | Зареєстровані файли, які не варто видаляти |
| Unneeded files that are safe to delete | Непотрібні файли, які можна безпечно видалити |

## Section headings

| English | Українська |
| --- | --- |
| PRODUCTS | ПРОДУКТИ |
| PATCHES | ВИПРАВЛЕННЯ |
| PRODUCT DETAILS | ДЕТАЛІ ПРОДУКТУ |
| BACKUP FOLDER | BACKUP FOLDER |
| SAY THANKS | ПОДЯКУВАТИ |

## Buttons and actions

| English | Українська |
| --- | --- |
| _About | _Про програму |
| Copy | Копіювати |
| Cut | Вирізати |
| Paste | Вставити |
| Select all | Виділити все |
| _Browse... | _Огляд... |
| _Cancel | _Скасувати |
| Check for _updates | Перевірити о_новлення |
| _Close | _Закрити |
| _Delete permanently | Видалити _назавжди |
| _Done | _Готово |
| Details | Деталі |
| _Buy me a cuppa | Пригостіть мене _кавою |
| Leave a _star on GitHub | Лишити зірку на _GitHub |
| Apache 2.0 licence | Ліцензія Apache 2.0 |
| _Move | Пере_містити |
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
| Open _release page | _Відкрити сторінку випуску |
| _Re-scan | Пов_торити сканування |
| _Scan again | _Сканувати знову |
| Send report | Надіслати звіт |
| _Send | _Надіслати |

## About window

| English | Українська |
| --- | --- |
| Guide and FAQ | Посібник і поширені запитання |
| Report a problem | Повідомити про проблему |
| Check for updates automatically | Автоматично перевіряти оновлення |

## Field labels

| English | Українська |
| --- | --- |
| Reason | Причина |
| Author | Автор |
| Application | Застосунок |
| Title | Назва |
| Subject | Тема |
| Keywords | Ключові слова |
| Signing certificate | Сертифікат підпису |
| File size | Розмір файлу |
| Comment | Коментар |
| Product name | Назва продукту |
| File | Файл |
| Size | Розмір |
| Patches | Виправлення |
| (unknown) | (невідомо) |
| (patches only) | (лише виправлення) |
| missing | відсутній |

## Status and progress

| English | Українська |
| --- | --- |
| Scanning... | Сканування... |
| Cancelling... | Скасування... |
| Starting scan... | Початок сканування... |
| Asking Windows about installed software... | Запит до Windows про встановлені програми... |
| Scanning installer cache folder... | Сканування папки кешу інсталятора... |
| Enumerating installed products... | Перелічення встановлених продуктів... |
| Checking registry for additional packages... | Перевірка реєстру на додаткові пакети... |
| Found {0} registered {1}. | Знайдено {0} зареєстрованих {1}. |
| Scan complete ({0}) | Сканування завершено ({0}) |
| Scanning local packages... | Сканування локальних пакетів... |
| Found {0} {1} you can safely delete. | Знайдено {0} {1} для безпечного видалення. |
| Preparing destination folder... | Підготовка папки призначення... |
| Moving unneeded files... | Moving unneeded files... |
| Deleting unneeded files... | Deleting unneeded files... |
| Move cancelled. {0} of {1} {2} processed. | Переміщення скасовано. Опрацьовано {0} з {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Видалення скасовано. Опрацьовано {0} з {1} {2}. |
| Move failed ({0}). Details in {1}. | Не вдалося перемістити ({0}). Деталі у {1}. |
| Move failed ({0}). The crash log could not be written. | Не вдалося перемістити ({0}). Не вдалося записати журнал збоїв. |
| Delete failed ({0}). Details in {1}. | Не вдалося видалити ({0}). Деталі у {1}. |
| Delete failed ({0}). The crash log could not be written. | Не вдалося видалити ({0}). Не вдалося записати журнал збоїв. |
| Access denied. Windows refused the scan. | Відмовлено в доступі. Windows відхилив сканування. |
| Scan failed: couldn't read the Windows Installer records. | Сканування не вдалося: не вдалося прочитати записи Windows Installer. |
| Scan cancelled. | Сканування скасовано. |
| Ready | Готово |
| Scan failed ({0}). Details in {1}. | Збій сканування ({0}). Деталі у {1}. |
| Scan failed ({0}). The crash log could not be written. | Збій сканування ({0}). Не вдалося записати журнал збоїв. |

## Main screen text

| English | Українська |
| --- | --- |
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Вони лежать у {InstallerFolder}, лишившись після видалення програми ({0}), заміни старого виправлення новішим ({1}) чи відкликання видавцем ({2}). InstallerClean перелічує лише ті файли, які сама Windows позначає як завершені. |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | Ще нічого не проскановано. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Натисніть «Повторити сканування», щоб переглянути {InstallerFolder} і знайти файли інсталятора, яких уже не потребує жодна програма. |
| These files can't be cleaned up right now. | Ці файли зараз не можна прибрати. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. |
| Select a file to view details. | Виберіть файл, щоб переглянути деталі. |
| Select a product to view details. | Виберіть продукт, щоб переглянути деталі. |
| No metadata available. | Метадані недоступні. |
| This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README [пояснює цю папку] і як відновити файл, словами самої Microsoft. |
| (none) | (немає) |

## Reasons a file is unneeded

| English | Українська |
| --- | --- |
| Orphaned | Осиротілий |
| Superseded | Заміщений |
| Obsoleted | Застарілий |

## Completion screen

| English | Українська |
| --- | --- |
| All clean | Усе чисто |
| Nothing removed | Nothing removed |
| Nothing to clean up in {InstallerFolder} | У {InstallerFolder} немає чого прибирати |
| Scanned {0} {1} in {2} | Проскановано {0} {1} за {2} |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| {0} freed | Звільнено {0} |
| {0} moved | Переміщено {0} |
| Nothing was moved | Нічого не переміщено |
| Nothing was deleted | Нічого не видалено |
| {0} of {1} could not be moved. | Не вдалося перемістити {0} файл з {1}. |
| {0} of {1} could not be moved. | Не вдалося перемістити {0} файлів з {1}. |
| {0} of {1} could not be deleted. | Не вдалося видалити {0} файл з {1}. |
| {0} of {1} could not be deleted. | Не вдалося видалити {0} файлів з {1}. |
| {0} {1} moved to: {2} | {0} {1} переміщено до: {2} |
| {0} {1} moved to: {2} | {0} {1} переміщено до: {2} |
| {0} {1} kept in place, because the records now claim what the scan flagged. | {0} {1} kept in place, because the records now claim what the scan flagged. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} {1} kept in place, because the Windows Installer records had changed by the final check. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} kept in place, because Windows has a record of the program named inside. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} kept in place, because InstallerClean couldn't find a program named inside. |
| Moved {0} of {1} {2} before you cancelled. | Переміщено {0} з {1} {2}, перш ніж ви скасували. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Безповоротно видалено {0} з {1} {2}, перш ніж ви скасували. |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | Радий, що знадобилося. Якщо ваша ласка, є куди докинути на каву. |

## Summaries and counts

| English | Українська |
| --- | --- |
| {0} file still needed | {0} файл ще потрібен |
| {0} files still needed | {0} файлів ще потрібно |
| {0} unneeded file to clean up | {0} непотрібний файл для очищення |
| {0} unneeded files to clean up | {0} непотрібних файлів для очищення |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} зареєстрований файл відсутній (його не видаляв InstallerClean). Зараз це не завдає клопоту, але в майбутньому відновлення, оновлення чи видалення тієї програми може не вдатися. Відкрийте «Деталі», щоб дізнатися, що робити. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} зареєстрованих файлів відсутні (їх не видаляв InstallerClean). Зараз це не завдає клопоту, але в майбутньому відновлення, оновлення чи видалення тих програм може не вдатися. Відкрийте «Деталі», щоб дізнатися, що робити. |
| InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| {0} of {1} {2} | {0} з {1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} осиротілих, {1} заміщених, {2} застарілих ({3}) |
| {0} registered file that is still needed ({1}) | {0} зареєстрований файл, який ще потрібен ({1}) |
| {0} registered files that are still needed ({1}) | {0} зареєстрованих файлів, які ще потрібні ({1}) |

## Confirmation dialogs

| English | Українська |
| --- | --- |
| Move {0} {1} ({2})? | Перемістити {0} {1} ({2})? |
| Files will be moved to: | Файли буде переміщено до: |
| Delete {0} {1} ({2})? | Видалити {0} {1} ({2})? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

## Error messages

| English | Українська |
| --- | --- |
| Access denied | Відмовлено в доступі |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows відмовив InstallerClean у доступі, тому роботу було зупинено. Нічого не було видалено.<br><br>InstallerClean уже працював від імені адміністратора, тож запускати його так ще раз не допоможе. Windows не повідомляє нічого більше про те, що саме відмовило в доступі, тож немає нічого конкретного, що варто спробувати. |
| Couldn't read the Windows Installer records | Не вдалося прочитати записи Windows Installer |
| Scan failed | Збій сканування |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Записи Windows Installer повернулися цілком порожніми: жодна встановлена програма й жодне оновлення не заявляє прав на кешований файл інсталятора. На робочому комп'ютері такого не буває (навіть у щойно встановленої Windows такі файли є), тож записи або пошкоджено, або їх не вдалося прочитати, і сканування, яке повірило б такій відповіді, помилково визнало б осиротілим кожен файл у {InstallerFolder}. Замість цього InstallerClean зупинився. Нічого не було видалено. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer не дозволив InstallerClean перелічити встановлене. InstallerClean уже працював від імені адміністратора, тож запуск від імені адміністратора ще раз нічого не змінить. Без цього списку немає безпечного способу визначити, які кешовані файли ще потрібні, тож InstallerClean зупинився. Нічого не було видалено. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer не зміг надати InstallerClean читабельний список встановлених програм: {0} записів поспіль повернулися нечитабельними (останній код помилки {1}). Замість того щоб працювати зі списком, прочитаним лише частково, InstallerClean зупинився. Нічого не було видалено. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer так і не повідомив про кінець списку встановлених програм: InstallerClean припинив спроби після {0} записів (останній код помилки {1}). Списку без кінця довіряти не можна, тож InstallerClean зупинився. Нічого не було видалено. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer так і не повідомив про кінець списку виправлень однієї програми: InstallerClean припинив спроби після {0} записів (останній код помилки {1}). Списку без кінця довіряти не можна, тож InstallerClean зупинився. Нічого не було видалено. |
| InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean не зміг прочитати достатньо записів Windows Installer, щоб напевно знати, що ще потрібно: список встановлених програм повернувся неповним, а читання тих самих записів прямо з реєстру теж призвело до помилок. Файл міг видаватися осиротілим лише тому, що запис, який його називає, виявився одним із нечитабельних, тож InstallerClean зупинився. Нічого не було видалено. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. |
| Nothing was deleted | Нічого не видалено |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Invalid destination | Недійсне призначення |
| Could not write to destination | Не вдалося записати в призначення |
| Move failed | Не вдалося перемістити |
| Delete failed | Не вдалося видалити |
| Setting not saved | Налаштування не збережено |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Не вдалося зберегти зміну. Під час наступного запуску InstallerClean повернеться до попереднього налаштування. |
| The destination cannot be inside the Windows Installer folder. | Призначення не може бути всередині папки Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Not enough space | Недостатньо місця |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Недостатньо місця в {0}<br><br>Потрібно: {1}<br>Доступно: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | У вас немає дозволу на запис у {0}.<br>Спробуйте папку у вашому профілі користувача або на диску, який вам належить. |
| The path {0} is too long for Windows. Pick a shorter path. | Шлях {0} задовгий для Windows. Виберіть коротший шлях. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | Папки {0} не існує, і її не вдалося створити. Перевірте літеру диска або мережевий шлях. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows не може записати в {0}.<br>Деталі у {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows не може записати в {0}. Не вдалося записати журнал збоїв. |
| Cannot write to {0}.<br>Details in {1}. | Не вдається записати в {0}.<br>Деталі у {1}. |
| Cannot write to {0}. The crash log could not be written. | Не вдається записати в {0}. Не вдалося записати журнал збоїв. |
| File no longer exists. | Файл більше не існує. |
| Source file is a symlink or junction; refused for safety. | Вихідний файл є символьним посиланням або junction; відмовлено з міркувань безпеки. |
| This file is not directly inside the Windows Installer folder; refused for safety. | Цей файл не міститься безпосередньо в папці Windows Installer; відмовлено з міркувань безпеки. |
| Windows refused access to this file; it was left in place. | Windows відмовив у доступі до цього файлу; його залишено на місці. |
| Windows refused access to these files; they were left in place. | Windows відмовив у доступі до цих файлів; їх залишено на місці. |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows повідомив про помилку файлу; файл залишено на місці. |
| Windows reported file errors; these files were left in place. | Windows повідомив про помилки файлів; ці файли залишено на місці. |
| Something went wrong with this file; it was left in place. | З цим файлом щось пішло не так; його залишено на місці. |
| Something went wrong with these files; they were left in place. | З цими файлами щось пішло не так; їх залишено на місці. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Відмова перемістити файли до папки Windows Installer (призначення: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
| Cannot write to {0}. | Не вдається записати в {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Не вдалося знайти унікальне ім'я файлу для «{0}» після 10 000 спроб. |

## Update check

| English | Українська |
| --- | --- |
| Check for updates | Перевірити оновлення |
| Checking... | Перевірка... |
| Up to date. | Актуальна версія. |
| Version {0} is available. | Доступна версія {0}. |
| Update available | Доступне оновлення |
| You're running version {0}.<br>Version {1} is available. | Ви використовуєте версію {0}.<br>Доступна версія {1}. |
| Couldn't reach GitHub. Check your internet connection and try again. | Не вдалося зв'язатися з GitHub. Перевірте інтернет-з'єднання та спробуйте ще раз. |
| GitHub returned an error response. Try again in a few minutes. | GitHub повернув повідомлення про помилку. Спробуйте ще раз за кілька хвилин. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Відповідь GitHub не містила розпізнаного випуску. Спробуйте пізніше або відкрийте сторінку випусків напряму. |
| The check timed out. Your connection to GitHub may be slow; try again. | Час перевірки вичерпано. Можливо, ваше з'єднання з GitHub повільне; спробуйте ще раз. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | Перевірка не вдалася з невідомої причини. Деталі у crash.log, якщо вам потрібно про це повідомити. |

## Opening links in your browser

| English | Українська |
| --- | --- |
| Couldn't open your browser | Не вдалося відкрити ваш браузер |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean не зміг відкрити ваш браузер. Посилання скопійовано до буфера обміну, тож ви можете вставити його самі:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean не зміг відкрити ваш браузер і не зміг скопіювати посилання до буфера обміну. Ось воно:<br><br>{0} |

## Sending the summary

| English | Українська |
| --- | --- |
| Sending... | Надсилання... |
| Thanks! Report sent. | Дякую! Звіт надіслано. |
| Sending failed. Try again later. | Не вдалося надіслати. Спробуйте пізніше. |
| No report to send. | Немає звіту для надсилання. |
| Send this? | Надіслати це? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | Надсилається на nofaff.netlify.app/api/result-log. Ніщо не ідентифікує вас чи вашу машину; це лише дає мені знати, що InstallerClean працює і [скільки місця люди звільняють]. |

## Startup and crashes

| English | Українська |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean уже працює. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Сталася неочікувана помилка, і InstallerClean потрібно закрити.<br><br>{0}<br><br>Деталі записано до:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Сталася неочікувана помилка, і InstallerClean потрібно закрити.<br><br>{0}<br><br>Не вдалося записати журнал збоїв. |
| Startup error | Помилка запуску |
| Failed to start ({0}). Details written to:<br>{1} | Не вдалося запустити ({0}). Деталі записано до:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Не вдалося запустити ({0}). Не вдалося записати журнал збоїв. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

## Tooltips (hover text)

| English | Українська |
| --- | --- |
| It's thirsty work! | Робота не з легких, аж у горлі пересохло! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Скасування запитано. InstallerClean чекає, доки поточний крок дійде до точки зупинки. Це може тривати кілька секунд під час інтенсивного вводу-виводу чи звернення до бази даних MSI. |
| Close | Закрити |
| A star helps other people find it. | Зірка допомагає іншим знайти InstallerClean. |
| Minimise | Згорнути |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | На ваш розсуд, але буду вдячний. Надсилає анонімний підсумок, який лише дає мені знати, чи працює програма і скільки місця люди звільняють. На наступному екрані ви побачите, що буде надіслано, перш ніж підтвердити. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | На ваш розсуд, але буду вдячний. Надсилає анонімний підсумок, який лише дає мені знати, чи працює програма. На наступному екрані ви побачите, що буде надіслано, перш ніж підтвердити. |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Назва суб'єкта з вбудованого сертифіката Authenticode. Ланцюжок не перевірено. |
| Change language. The program will restart. | Змінити мову. Програму буде перезапущено. |

## Screen reader labels

| English | Українська |
| --- | --- |
| Donate | Підтримати |
| Buy me a cuppa | Пригостіть мене кавою |
| Cancel operation | Скасувати операцію |
| Cancel scan | Скасувати сканування |
| Cancel startup scan | Скасувати сканування під час запуску |
| Close | Закрити |
| Close window | Закрити вікно |
| Close result and return to main window | Закрити результат і повернутися до головного вікна |
| Leave a star on github | Лишити зірку на github |
| Minimise | Згорнути |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | «Перемістити» кладе непотрібні файли до обраної папки призначення. «Скасувати» лишає їх там, де вони є. |
| Say thanks | Подякувати |
| Send posts the report shown to No Faff. Cancel sends nothing. | «Надіслати» надсилає показаний звіт до No Faff. «Скасувати» не надсилає нічого. |
| Check for updates | Перевірити оновлення |
| Checks github's releases page for a newer version. | Перевіряє на сторінці випусків github, чи є новіша версія. |
| Opens the readme on github in your browser. | Відкриває readme на github у вашому браузері. |
| Opens the issue tracker on github.com in your browser. | Відкриває список проблем (Issues) на github.com у вашому браузері. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Якщо позначено, InstallerClean під час запуску перевіряє на github наявність новішої версії. |
| Open the release page to download the newer version, or cancel to keep the current version. | Відкрийте сторінку випуску, щоб завантажити новішу версію, або скасуйте, щоб лишити поточну версію. |
| Opens the licence file on github.com in your browser. | Відкриває файл ліцензії на github.com у вашому браузері. |
| Backup folder | Backup folder |
| Products | Продукти |
| Patches | Виправлення |
| Product details | Деталі продукту |
| Backup folder | Backup folder |
| Operation progress | Перебіг операції |
| Scan {InstallerFolder} again | Просканувати {InstallerFolder} ще раз |
| Scanning progress | Перебіг сканування |
| Startup scan progress | Перебіг сканування під час запуску |
| Details, unneeded files | Деталі, непотрібні файли |
| Available for cleanup. | Доступні для очищення. |
| Details, registered files | Деталі, зареєстровані файли |
| Read-only inventory. | Лише для перегляду. |
| Sorted by {0}, ascending | Відсортовано за {0}, за зростанням |
| Sorted by {0}, descending | Відсортовано за {0}, за спаданням |
| Scan results | Результати сканування |
| Result details | Деталі результату |
| File details | Деталі файлу |
| Product details | Product details |
| Dialog text | Текст діалогу |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Файли, які не вдалося обробити |
| Explains this folder, and how to recover a file, in the README | Пояснює цю папку і як відновити файл, у README |
| Report preview | Попередній перегляд звіту |
| Change language | Змінити мову |
| The program will restart. | Програму буде перезапущено. |

## File picker

| English | Українська |
| --- | --- |
| Choose destination folder for moved files | Виберіть папку призначення для переміщених файлів |

## Version

| English | Українська |
| --- | --- |
| Version {0} | Версія {0} |

## Word forms (singular and plural)

| English | Українська |
| --- | --- |
| file | файл |
| files | файлів |
| error | помилка |
| errors | помилок |
| package | пакет |
| packages | пакетів |
| product | продукт |
| products | продуктів |
| patch | виправлення |
| patches | виправлень |

## Sizes and times

| English | Українська |
| --- | --- |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | менш ніж секунду |
| {0:F1} seconds | {0:F1} секунди |

## Command-line tool (installerclean-cli)

| English | Українська |
| --- | --- |
| Error: unknown argument '{0}' | Error: unknown argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Помилка: неочікуваний зайвий аргумент «{0}». Якщо в назві папки для переміщення є пробіл, візьміть увесь шлях у лапки: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. |
| Cancelling... | Скасування... |
| Cancelled. | Скасовано. |
| Error: unexpected failure ({0}). Details written to {1}. | Error: unexpected failure ({0}). Details written to {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Error: unexpected failure ({0}). The crash log could not be written. |
| Scanning {InstallerFolder}... | Сканування {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Found {0} unneeded {1} to clean up ({2}). |
| Found no unneeded files. | Found no unneeded files. |
| {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. | {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. |
| {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. | {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. |
| InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Deleting {0} unneeded {1}... | Deleting {0} unneeded {1}... |
| Permanently deleted {0} unneeded {1}. | Permanently deleted {0} unneeded {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Помилка: не вказано розташування для переміщення. Скористайтеся /m ШЛЯХ. (Типове значення, задане в графічному інтерфейсі, діє лише для поточного користувача і не застосовується до запусків за розкладом чи від імені службового облікового запису.) |
| Error: destination cannot be inside the Windows Installer folder. | Помилка: призначення не може бути всередині папки Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Помилка: призначення має бути повним шляхом. Отримано: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Moving {0} unneeded {1} to {2}... | Moving {0} unneeded {1} to {2}... |
| Moved {0} unneeded {1}. | Moved {0} unneeded {1}. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Інший процес InstallerClean утримує блокування єдиного екземпляра (графічний інтерфейс чи інший запуск CLI). Вихід 75 (тимчасовий); можна безпечно повторити пізніше. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Примітка: не вдалося записати до журналу подій. Перевірте дозволи журналу «Програма» чи групову політику. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - очищення {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Removes cached .msi and .msp files that no installed program still needs. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Needs an elevated (administrator) prompt; Windows will not start it. |
| Usage: | Використання: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Показати цю довідку (також приймає /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Вивести версію (також приймає -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ШЛЯХ    Перемістити за вказаним шляхом |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. |
| Exit codes: | Коди виходу: |
|   0   success: the run finished with nothing left to do |   0   success: the run finished with nothing left to do |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partial: some processed, some not (a failure or a Ctrl+C part way) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  тимчасова: запуск заблокувала тимчасова умова (див. повідомлення) |
|   130 cancelled (Ctrl+C) |   130 скасовано (Ctrl+C) |
