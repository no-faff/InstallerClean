# InstallerClean in Українська (Ukrainian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Ukrainian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Ukrainian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.uk.resx`](../../src/InstallerClean.Core/Resources/Strings.uk.resx), so do not edit it by hand. The Ukrainian translation itself lives in [`gen-strings-uk.mjs`](../../scripts/translations/gen-strings-uk.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Українська |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Про програму |
| Files left alone | Файли, залишені без змін |
| Unneeded files that are safe to delete | Непотрібні файли, які можна безпечно видалити |

## Section headings

| English | Українська |
| --- | --- |
| PATCHES | ВИПРАВЛЕННЯ |
| PRODUCT DETAILS | ДЕТАЛІ ПРОДУКТУ |
| BACKUP FOLDER | ПАПКА ПРИЗНАЧЕННЯ |
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
| _Delete permanently | _Видалити назавжди |
| _Done | _Готово |
| Details | Деталі |
| _Buy me a cuppa | Пригостіть мене _кавою |
| Leave a _star on GitHub | Лишити зірку на _GitHub |
| Apache 2.0 licence | Ліцензія Apache 2.0 |
| _Move | Пере_містити |
| Path to folder if you move rather than delete. | Шлях до папки, якщо ви переміщуєте, а не видаляєте. |
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
| Moving unneeded files... | Переміщення непотрібних файлів... |
| Deleting unneeded files... | Видалення непотрібних файлів... |
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
| Any unneeded files below are [safe to delete]. | Будь-які непотрібні файли нижче [можна безпечно видалити]. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Вони лежать у {InstallerFolder}. InstallerClean запитує Windows про кожну встановлену програму: файл потрапляє до списку, коли на нього не претендує жодна програма ({0}) або коли його замінило новіше виправлення і жодна програма не змогла б до нього повернутися ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update, repair and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Перемістіть їх до вибраної вами папки призначення, а потім видаліть цю папку, коли переконаєтеся, що ваші програми, як і раніше, оновлюються, відновлюються та видаляються. Повернення їх до {InstallerFolder} відновлює все. Або видаліть їх назавжди просто зараз. |
| Nothing scanned yet. | Ще нічого не проскановано. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Натисніть «Повторити сканування», щоб переглянути {InstallerFolder} і знайти файли інсталятора, яких уже не потребує жодна програма. |
| These files can't be cleaned up right now. | Ці файли зараз не можна прибрати. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Зараз щось використовує Windows Installer, наприклад оновлення Windows або програма, що встановлюється у фоні. «Перемістити» і «Видалити» призупинено на цей час, щоб InstallerClean не чіпав {InstallerFolder}, доки вона змінюється. Коли все завершиться, повторіть сканування, і вони повернуться. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | На цьому комп'ютері призупинено попередню транзакцію Windows Installer. Відновіть або скасуйте те встановлення (чи перезавантажте Windows), перш ніж очищати {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows поставила в чергу на наступне перезавантаження перейменування файлу, що стосується {InstallerFolder}. Перезавантажте Windows, перш ніж очищати. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | У Windows Installer щось виконується, тому «Перемістити» і «Видалити» призупинено. InstallerClean не чіпатиме {InstallerFolder}, доки вона змінюється. Коли все завершиться, повторіть сканування, і вони повернуться. |
| Select a file to view details. | Виберіть файл, щоб переглянути деталі. |
| Select a product to view details. | Виберіть продукт, щоб переглянути деталі. |
| No metadata available. | Метадані недоступні. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. | Цього файлу інсталятора немає. Зараз це не створює жодних труднощів і не створюватиме до того дня, коли ви спробуєте відновити, оновити або видалити програму, якій він належить. Тоді цей крок може завершитися невдало, бо Windows шукає цей файл, а його немає.<br><br>Щоб спробувати це виправити, завантажте інсталятор тієї програми в її розробника і запустіть його поверх наявної копії (не видаляйте програму спершу: видалення саме по собі є кроком, якому потрібен цей файл). За змоги візьміть саме ту версію, яку встановлено, бо Windows може відхилити іншу. Це має відновити файл і не зачепити ваші налаштування, але Microsoft цього не гарантує, і її власний останній засіб - перевстановлення програми. |
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
| Nothing removed | Нічого не прибрано |
| Nothing to clean up in {InstallerFolder} | У {InstallerFolder} немає чого прибирати |
| Scanned {0} {1} in {2} | Проскановано {0} {1} за {2} |
| Nothing offered on this PC | На цьому ПК нічого не запропоновано |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({1}) it might otherwise have offered. | InstallerClean не зміг упевнено визначити, яким зі встановлених тут програм належать файли в кеші, тож затримав єдиний файл ({1}), який інакше запропонував би. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} files ({1}) it might otherwise have offered. | InstallerClean не зміг упевнено визначити, яким зі встановлених тут програм належать файли в кеші, тож затримав усі {0} файлів ({1}), які інакше запропонував би. |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | Файл у цій папці [можна безпечно прибрати], тож видаляйте папку коли завгодно. До того часу ви можете повернути його до {InstallerFolder}, якщо якійсь програмі він усе ж знадобиться (украй малоймовірно). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | Файли в цій папці [можна безпечно прибрати], тож видаляйте її коли завгодно. До того часу ви можете повернути їх до {InstallerFolder}, якщо якійсь програмі знадобиться один із них (украй малоймовірно). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | Файл у цій папці [можна безпечно прибрати], тож видаліть папку або перемістіть її на інший диск, коли справді захочете повернути місце. До того часу ви можете повернути його до {InstallerFolder}, якщо якійсь програмі він усе ж знадобиться (украй малоймовірно). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | Файли в цій папці [можна безпечно прибрати], тож видаліть її або перемістіть на інший диск, коли справді захочете повернути місце. До того часу ви можете повернути їх до {InstallerFolder}, якщо якійсь програмі знадобиться один із них (украй малоймовірно). |
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
| {0} {1} kept in place, because the records now claim what the scan flagged. | Залишено на місці {0} {1}, бо записи тепер заявляють те, що позначило сканування. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | Залишено на місці {0} {1}, бо до підсумкової перевірки записи Windows Installer змінилися. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | Залишено на місці {0} {1}, бо під час підсумкової перевірки записи Windows Installer не вдалося прочитати повністю. |
| {0} {1} kept in place, because by the final check InstallerClean could not be certain which cached files belong to the programs installed here. | Залишено на місці {0} {1}, бо до підсумкової перевірки InstallerClean не зміг упевнено визначити, яким зі встановлених тут програм належать файли в кеші. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | Залишено на місці {0} {1}, бо Windows має запис про програму, названу всередині. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | Залишено на місці {0} {1}, бо InstallerClean не знайшов усередині назви програми. |
| Moved {0} of {1} {2} before you cancelled. | Переміщено {0} з {1} {2}, перш ніж ви скасували. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Безповоротно видалено {0} з {1} {2}, перш ніж ви скасували. |
| {0} {1} permanently deleted | Остаточно видалено {0} {1} |
| {0} {1} permanently deleted | Остаточно видалено {0} {1} |
| Glad to help. There's a tip jar if you're feeling kind. | Радий, що знадобилося. Якщо ваша ласка, є куди докинути на каву. |

## Summaries and counts

| English | Українська |
| --- | --- |
| {0} file left alone | {0} файл залишено без змін |
| {0} files left alone | {0} файлів залишено без змін |
| {0} unneeded file to clean up | {0} непотрібний файл для очищення |
| {0} unneeded files to clean up | {0} непотрібних файлів для очищення |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Open Details for what to do. | Windows має запис про {0} файл, якого немає в {InstallerFolder}: {1}. У повсякденній роботі це не заважає, але відновлення, оновлення чи видалення програми через нього може не вдатися. Відкрийте «Деталі», щоб дізнатися, що робити. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Open Details for what to do. | Windows має записи про {0} файлів, яких немає в {InstallerFolder}: {1}. У повсякденній роботі це не заважає, але відновлення, оновлення чи видалення програми через них може не вдатися. Відкрийте «Деталі», щоб дізнатися, що робити. |
| {0} other program | ще {0} програма |
| {0} other programs | ще {0} програм |
| {0} file with no program named in the records | {0} файл, для якого в записах не названо програми |
| {0} files with no program named in the records | {0} файлів, для яких у записах не названо програми |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than listing it. | На цьому ПК InstallerClean не зміг упевнено визначити, яким зі встановлених тут програм належать файли в кеші, тож затримав єдиний файл, а не показав його в списку. |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than listing them. | На цьому ПК InstallerClean не зміг упевнено визначити, яким зі встановлених тут програм належать файли в кеші, тож затримав {0} {1}, а не показав їх у списку. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. The unneeded files above are unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Re-scan to try again. | InstallerClean не зміг зіставити все, що є в записах Windows, тому прочитав їх не повністю. Непотрібних файлів вище це не стосується, але сказане про файли, яких немає в {InstallerFolder}, може бути неповним. Повторіть сканування, щоб спробувати ще раз. |
| {0} of {1} {2} | {0} з {1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} для очищення ({2}) |
| {0} file left alone ({1}) | {0} файл залишено без змін ({1}) |
| {0} files left alone ({1}) | {0} файлів залишено без змін ({1}) |

## Confirmation dialogs

| English | Українська |
| --- | --- |
| Move {0} {1} ({2})? | Перемістити {0} {1} ({2})? |
| Move to: | Перемістити до: |
| Delete {0} {1} ({2})? | Видалити {0} {1} ({2})? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | Цей файл буде видалено назавжди. Його [можна безпечно видалити], але якщо хочете резервну копію, скористайтеся кнопкою «Перемістити». |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Файли буде видалено назавжди. Їх [можна безпечно видалити], але якщо хочете резервну копію, скористайтеся кнопкою «Перемістити». |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Ця папка на тому самому диску, тож місце не повернеться, доки ви її не видалите. Виберіть натомість папку на іншому диску, якщо хочете отримати місце одразу. |

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
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean не зміг зіставити записи Windows Installer із вмістом {InstallerFolder}. Майже нічого з того, на що вказують записи, там немає, і майже нічого з того, що там є, не названо жодним записом, тож про жоден файл не вдалося показати, що він непотрібний. Нічого не запропоновано і нічого не прибрано. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean не зміг зіставити записи Windows Installer із вмістом {InstallerFolder}. У папці є файли, але жоден запис не вказує ні на що всередині неї, тож про жоден файл не вдалося показати, що він непотрібний. Нічого не запропоновано і нічого не прибрано. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean не зміг прочитати достатньо записів Windows Installer, щоб напевно знати, що ще потрібно: список встановлених програм повернувся неповним, а читання тих самих записів прямо з реєстру теж призвело до помилок. Файл міг видаватися осиротілим лише тому, що запис, який його називає, виявився одним із нечитабельних, тож InstallerClean зупинився. Нічого не було видалено. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean не зміг домогтися від Windows розв'язання справжнього шляху до {InstallerFolder}, тож про жоден файл не вдалося показати, що він усередині, і жоден не було запропоновано для очищення. Це сканування нічого не знайшло через невдачу тієї перевірки, а не тому, що папка чиста. Нічого не прибрано. |
| Nothing was deleted | Нічого не видалено |
| Nothing was moved | Нічого не переміщено |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean не зміг узяти блокування, яким Windows Installer не дає двом програмам одночасно змінювати встановлене ПЗ, тож не зміг виключити, що файл знадобиться на півдорозі, і нічого не видалено. Спробуйте ще раз, а якщо повторюється - перезавантажте Windows. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean не зміг узяти блокування, яким Windows Installer не дає двом програмам одночасно змінювати встановлене ПЗ, тож не зміг виключити, що файл знадобиться на півдорозі, і нічого не переміщено. Спробуйте ще раз, а якщо повторюється - перезавантажте Windows. |
| Invalid destination | Недійсне призначення |
| Could not write to destination | Не вдалося записати в призначення |
| Move failed | Не вдалося перемістити |
| Delete failed | Не вдалося видалити |
| Setting not saved | Налаштування не збережено |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Не вдалося зберегти зміну. Під час наступного запуску InstallerClean повернеться до попереднього налаштування. |
| The destination cannot be inside the Windows Installer folder. | Призначення не може бути всередині папки Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Призначення {0} розв'язується всередині системної папки Windows. Виберіть шлях поза %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% і %ProgramData%. |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | Цей файл відкрито або заблоковано іншою програмою, тож зараз його нічим не прибрати. Його залишено на місці; спробуйте пізніше. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | Ці файли відкрито або заблоковано іншою програмою, тож зараз їх нічим не прибрати. Їх залишено на місці; спробуйте пізніше. |
| Windows reported a file error; the file was left in place. | Windows повідомив про помилку файлу; файл залишено на місці. |
| Windows reported file errors; these files were left in place. | Windows повідомив про помилки файлів; ці файли залишено на місці. |
| Something went wrong with this file; it was left in place. | З цим файлом щось пішло не так; його залишено на місці. |
| Something went wrong with these files; they were left in place. | З цими файлами щось пішло не так; їх залишено на місці. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Відмова перемістити файли до папки Windows Installer (призначення: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Папка призначення має бути повним шляхом до папки, що починається з літери диска або мережевого ресурсу (наприклад, D:\Backup або \\server\backup). InstallerClean не може використати цей: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean більше не зміг підтвердити папку призначення і зупинився, щоб не записати не туди. Перевірте {0}, потім «Повторити сканування» і спробуйте ще раз. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log збирає необроблені винятки InstallerClean.<br># За підвищених прав повідомлення про винятки платформи можуть<br># містити шляхи до файлів поточного сеансу (зокрема профілі інших<br># користувачів, перелічені запитами Windows Installer). Повідомлення<br># про мережеві збої під час перевірки оновлень або надсилання журналу<br># результатів можуть містити URL призначення та розв'язану IP-адресу<br># чи адресу проксі. Записи про нечитані записи Windows Installer<br># можуть містити SID облікового запису Windows (S-1-5-21-...) і коди<br># продуктів встановленого ПЗ.<br># Приберіть усі три види відомостей, перш ніж додавати цей файл до<br># публічного звіту про помилку.<br> |

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
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Переміщує непотрібні файли до папки призначення. Видаліть цю папку, коли переконаєтеся, що вони нікому не потрібні. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Переміщує непотрібні файли до папки призначення. Ви виберете її наступним кроком. Видаліть цю папку, коли переконаєтеся, що вони нікому не потрібні. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Переміщує непотрібні файли до папки призначення. Вона на тому самому диску, тож місце повернеться лише після того, як ви видалите цю папку або перемістите її на інший диск. Це можна зробити, коли переконаєтеся, що вони нікому не потрібні. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Видаляє непотрібні файли назавжди. Їх можна безпечно прибрати, і місце повернеться одразу. |
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
| Delete permanently removes the unneeded files. Cancel closes without deleting. | «Видалити назавжди» прибирає непотрібні файли. «Скасувати» закриває вікно, нічого не видаляючи. |
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
| Backup folder | Папка призначення |
| Patches | Виправлення |
| Product details | Деталі продукту |
| Backup folder | Папка призначення |
| Operation progress | Перебіг операції |
| Scan {InstallerFolder} again | Просканувати {InstallerFolder} ще раз |
| Scanning progress | Перебіг сканування |
| Startup scan progress | Перебіг сканування під час запуску |
| Details, unneeded files | Деталі, непотрібні файли |
| Available for cleanup. | Доступні для очищення. |
| Details, files left alone | Деталі, файли, залишені без змін |
| Read-only inventory. | Лише для перегляду. |
| Sorted by {0}, ascending | Відсортовано за {0}, за зростанням |
| Sorted by {0}, descending | Відсортовано за {0}, за спаданням |
| Scan results | Результати сканування |
| Result details | Деталі результату |
| File details | Деталі файлу |
| Product details | Відомості про продукт |
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
| ,  | ,  |
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
| Error: unknown argument '{0}' | Помилка: невідомий аргумент «{0}» |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Помилка: неочікуваний зайвий аргумент «{0}». Якщо в назві папки для переміщення є пробіл, візьміть увесь шлях у лапки: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Помилка: неочікуваний зайвий аргумент «{0}». /s і /d не приймають інших аргументів, і за один запуск можна використати лише один ключ. |
| Cancelling... | Скасування... |
| Cancelled. | Скасовано. |
| Error: unexpected failure ({0}). Details written to {1}. | Помилка: неочікуваний збій ({0}). Подробиці записано до {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Помилка: неочікуваний збій ({0}). Журнал збою записати не вдалося. |
| Scanning {InstallerFolder}... | Сканування {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Знайдено {0} непотрібних {1} для очищення ({2}). |
| Found no unneeded files. | Непотрібних файлів не знайдено. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back the one file ({2}) it might otherwise have offered. | InstallerClean не зміг упевнено визначити, яким зі встановлених тут програм належать файли в кеші, тож затримав єдиний файл ({2}), який інакше запропонував би. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back all {0} {1} ({2}) it might otherwise have offered. | InstallerClean не зміг упевнено визначити, яким зі встановлених тут програм належать файли в кеші, тож затримав усі {0} {1} ({2}), які інакше запропонував би. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Running that program's installer again, preferably the same version, usually restores the file. | Windows має запис про {0} файл, якого немає в {InstallerFolder}: {1}. У повсякденній роботі це не заважає, але відновлення, оновлення чи видалення програми через нього може не вдатися. Повторний запуск інсталятора тієї програми, бажано тієї самої версії, зазвичай повертає файл. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Running each program's installer again, preferably the same version, usually restores the files. | Windows має записи про {0} файлів, яких немає в {InstallerFolder}: {1}. У повсякденній роботі це не заважає, але відновлення, оновлення чи видалення програми через них може не вдатися. Повторний запуск інсталятора кожної програми, бажано тієї самої версії, зазвичай повертає файли. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. What it found is unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Running it again may pick up more. | InstallerClean не зміг зіставити все, що є в записах Windows, тому прочитав їх не повністю. Знайденого це не стосується, але сказане про файли, яких немає в {InstallerFolder}, може бути неповним. Повторний запуск, можливо, знайде більше. |
| Deleting {0} unneeded {1}... | Видалення {0} непотрібних {1}... |
| Permanently deleted {0} unneeded {1}. | Остаточно видалено {0} непотрібних {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Помилка: не вказано розташування для переміщення. Скористайтеся /m ШЛЯХ. (Типове значення, задане в графічному інтерфейсі, діє лише для поточного користувача і не застосовується до запусків за розкладом чи від імені службового облікового запису.) |
| Error: destination cannot be inside the Windows Installer folder. | Помилка: призначення не може бути всередині папки Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Помилка: призначення має бути повним шляхом. Отримано: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Помилка: призначення {0} розв'язується всередині системної папки Windows. Виберіть шлях поза %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% і %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Помилка: недостатньо місця в {0}. Для переміщення цих файлів потрібно {1}, а вільно {2}. Нічого не переміщено. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Помилка: зараз щось використовує Windows Installer, наприклад оновлення Windows або програма, що встановлюється у фоні. /m і /d заблоковано на цей час. Спробуйте ще раз, коли все завершиться. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Помилка: на цьому комп'ютері призупинено попередню транзакцію Windows Installer. Відновіть або скасуйте те встановлення (чи перезавантажте Windows), перш ніж очищати {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Помилка: поставлена в чергу після перезавантаження операція з файлом стосується {InstallerFolder} ({0}). Перезавантажте Windows, щоб завершити цю операцію, перш ніж очищати. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Помилка: у Windows Installer щось виконується, тому /m і /d заблоковано. InstallerClean не чіпатиме {InstallerFolder}, доки вона змінюється. Спробуйте ще раз, коли все завершиться. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Помилка: InstallerClean не зміг узяти блокування Windows Installer, яке не дає двом програмам одночасно змінювати встановлене ПЗ, тож не зміг виключити, що файл знадобиться на півдорозі. Нічого не видалено. Спробуйте ще раз, а якщо повторюється - перезавантажте Windows. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Помилка: InstallerClean не зміг узяти блокування Windows Installer, яке не дає двом програмам одночасно змінювати встановлене ПЗ, тож не зміг виключити, що файл знадобиться на півдорозі. Нічого не переміщено. Спробуйте ще раз, а якщо повторюється - перезавантажте Windows. |
| Moving {0} unneeded {1} to {2}... | Переміщення {0} непотрібних {1} до {2}... |
| Moved {0} unneeded {1}. | Переміщено {0} непотрібних {1}. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean більше не зміг підтвердити папку призначення і зупинився, щоб не записати не туди. Перевірте {0}, потім запустіть команду ще раз. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Інший процес InstallerClean утримує блокування єдиного екземпляра (графічний інтерфейс чи інший запуск CLI). Вихід 75 (тимчасовий); можна безпечно повторити пізніше. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Примітка: не вдалося записати до журналу подій. Перевірте дозволи журналу «Програма» чи групову політику. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - очищення {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Прибирає файли .msi і .msp з кешу, не потрібні жодній програмі. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Потрібен командний рядок адміністратора; інакше Windows не запустить. |
| Usage: | Використання: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Показати цю довідку (також приймає /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Вивести версію (також приймає -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Лише сканувати - список непотрібних |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Видалити непотрібні файли назавжди |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Перемістити до збереженої папки |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ШЛЯХ    Перемістити за вказаним шляхом |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli утримує командний рядок до кінця роботи, щоб<br>скрипт або запланована задача могли на нього зачекати. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Папка зберігається для кожного користувача; задачам потрібен /m ШЛЯХ. |
| Exit codes: | Коди виходу: |
|   0   success: the run did what it was asked and nothing failed |   0   успіх: запуск зробив те, про що просили, і нічого не збоїло |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   збій: нічого не оброблено (хибні аргументи чи призначення,<br>       невдале сканування або всі файли з помилкою) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   частково: щось оброблено, щось ні (збій або Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  тимчасова: запуск заблокувала тимчасова умова (див. повідомлення) |
|   130 cancelled (Ctrl+C) |   130 скасовано (Ctrl+C) |
