# InstallerClean in Українська (Ukrainian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Ukrainian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Ukrainian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.uk.resx`](../../src/InstallerClean.Core/Resources/Strings.uk.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | Українська |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Про програму |
| Registered files that should not be deleted | Зареєстровані файли, які не варто видаляти |
| Unneeded files that are safe to delete | Непотрібні файли, які можна безпечно видалити |
| Confirm move | Підтвердження переміщення |
| Confirm delete | Підтвердження видалення |
| Recycle Bin unavailable | Кошик недоступний |

## Section headings

| English | Українська |
| --- | --- |
| PRODUCTS | ПРОДУКТИ |
| PATCHES | ПАТЧІ |
| PRODUCT DETAILS | ДЕТАЛІ ПРОДУКТУ |
| MOVE LOCATION | КУДИ ПЕРЕМІСТИТИ |
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
| _Delete | _Видалити |
| _Delete permanently | Видалити _назавжди |
| _Done | _Готово |
| Details | Деталі |
| _Buy me a cuppa | Пригостіть мене _кавою |
| Leave a _star on GitHub | Лишити зірку на _GitHub |
| Apache 2.0 licence | Ліцензія Apache 2.0 |
| _Move | Пере_містити |
| _Move instead | _Перемістити натомість |
| Path to folder if you Move instead of Delete | Шлях до папки, якщо ви переміщуєте замість видалення |
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
| Patches | Патчі |
| (unknown) | (невідомо) |
| (patches only) | (лише патчі) |
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
| Checking the Recycle Bin... | Перевірка Кошика... |
| Moving {0} {1}... | Переміщення: {0} {1}... |
| Deleting {0} {1}... | Видалення: {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Переміщення скасовано. Опрацьовано {0} з {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Видалення скасовано. Опрацьовано {0} з {1} {2}. |
| Move failed ({0}). Details in {1}. | Не вдалося перемістити ({0}). Деталі у {1}. |
| Move failed ({0}). The crash log could not be written. | Не вдалося перемістити ({0}). Не вдалося записати журнал збоїв. |
| Delete failed ({0}). Details in {1}. | Не вдалося видалити ({0}). Деталі у {1}. |
| Delete failed ({0}). The crash log could not be written. | Не вдалося видалити ({0}). Не вдалося записати журнал збоїв. |
| Access denied. Windows refused the scan. | Відмовлено в доступі. Windows відхилив сканування. |
| Scan failed: couldn't read the Windows Installer records. | Сканування не вдалося: не вдалося прочитати записи інсталятора Windows. |
| Scan cancelled. | Сканування скасовано. |
| Ready | Готово |
| Scan failed ({0}). Details in {1}. | Збій сканування ({0}). Деталі у {1}. |
| Scan failed ({0}). The crash log could not be written. | Збій сканування ({0}). Не вдалося записати журнал збоїв. |

## Main screen text

| English | Українська |
| --- | --- |
| Any unneeded files below are safe to delete. | Будь-які непотрібні файли нижче можна безпечно видалити. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Вони лежать у C:\Windows\Installer, лишившись після видалення програми ({0}), заміни старого патча новішим ({1}) чи відкликання видавцем ({2}). InstallerClean перелічує лише ті файли, які сама Windows позначає як завершені. |
| Delete them to the Recycle Bin, or use Move instead to keep a backup. Putting the files back in C:\Windows\Installer returns you to exactly where you started. | Видаліть їх до Кошика, або скористайтеся натомість функцією «Перемістити», щоб зберегти резервну копію. Якщо повернути файли назад у C:\Windows\Installer, усе стане точно таким, як було. |
| Nothing scanned yet. | Ще нічого не проскановано. |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | Натисніть «Повторити сканування», щоб переглянути C:\Windows\Installer і знайти файли інсталятора, яких уже не потребує жодна програма. |
| These files can't be cleaned up right now. | Ці файли зараз не можна прибрати. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | Щось саме зараз використовує Windows Installer, зазвичай це Windows Update або програма, що встановлюється у фоні. Переміщення та видалення призупинено, доки це триває, тож InstallerClean не чіпатиме кеш інсталятора, поки той змінюється. Коли це завершиться, виконайте повторне сканування, і вони повернуться. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | На цій машині призупинено попередню транзакцію Windows Installer. Поновіть або відкотіть те встановлення (чи перезавантажте Windows), перш ніж очищати кеш. |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows має перейменування файлу, поставлене в чергу на наступне перезавантаження, яке стосується кешу інсталятора. Перезавантажте Windows, перш ніж очищати. |
| Select a file to view details. | Виберіть файл, щоб переглянути деталі. |
| Select a product to view details. | Виберіть продукт, щоб переглянути деталі. |
| No metadata available. | Метадані недоступні. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | Цей файл інсталятора видалено. InstallerClean цього не робив, він ніколи не видаляє файл, який ще потрібен програмі; цей видалило щось інше, перш ніж ви запустили InstallerClean.<br><br>Зараз це не завдає клопоту і не завдаватиме аж до дня, коли ви спробуєте відновити, оновити чи видалити програму, якій він належить. Тоді цей крок може не вдатися, бо Windows шукатиме цей файл, а його там немає.<br><br>Щоб спробувати це виправити, завантажте інсталятор тієї програми від її виробника та запустіть його поверх наявної копії (не видаляйте програму спершу, бо видалення саме по собі є кроком, який потребує цього файлу). Якщо можете дістати ту версію, яку ви встановили, використайте її, бо Windows може відхилити іншу. Зазвичай це відновлює файл, і ваші налаштування звичайно лишаються недоторканими, але Microsoft цього не гарантує, її власний останній засіб — це перевстановлення програми або самої Windows. |
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
| Nothing to clean up in C:\Windows\Installer | У C:\Windows\Installer немає чого прибирати |
| Scanned {0} {1} in {2} | Проскановано {0} {1} за {2} |
| Copy them back to C:\Windows\Installer if anything ever breaks ([extremely unlikely]). | Скопіюйте їх назад у C:\Windows\Installer, якщо раптом щось зламається ([вкрай малоймовірно]). |
| Until then, you can restore them if anything ever breaks ([extremely unlikely]). | А поки що ви можете відновити їх, якщо раптом щось зламається ([вкрай малоймовірно]). |
| Empty it to actually reclaim the space. | Очистіть Кошик, щоб справді звільнити місце. |
| {0} freed | Звільнено {0} |
| {0} cleaned up | Очищено {0} |
| {0} moved | Переміщено {0} |
| Nothing was moved | Нічого не переміщено |
| Nothing was deleted | Нічого не видалено |
| {0} of {1} could not be moved. | Не вдалося перемістити {0} файл з {1}. |
| {0} of {1} could not be moved. | Не вдалося перемістити {0} файлів з {1}. |
| {0} of {1} could not be deleted. | Не вдалося видалити {0} файл з {1}. |
| {0} of {1} could not be deleted. | Не вдалося видалити {0} файлів з {1}. |
| {0} {1} moved to: {2} | {0} {1} переміщено до: {2} |
| {0} {1} moved to: {2} | {0} {1} переміщено до: {2} |
| {0} {1} moved to the Recycle Bin | {0} {1} переміщено до Кошика |
| {0} {1} moved to the Recycle Bin | {0} {1} переміщено до Кошика |
| {0} {1} kept in place, because a program started needing them again after the scan. | {0} {1} залишено на місці: після сканування вони знову знадобилися програмі. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | {0} {1} залишено на місці: під час повторної перевірки не вдалося повністю прочитати записи інсталятора Windows. |
| Moved {0} of {1} {2} before you cancelled. | Переміщено {0} з {1} {2}, перш ніж ви скасували. |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | Переміщено {0} з {1} {2} до Кошика, перш ніж ви скасували. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Безповоротно видалено {0} з {1} {2}, перш ніж ви скасували. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} безповоротно видалено. Він не потрапив до Кошика. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1} безповоротно видалено. Вони не потрапили до Кошика. |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Це нормально, його можна було безпечно прибрати. InstallerClean прибирає лише ті файли, які Windows позначає як завершені, ніколи той, що ще потрібен програмі. У малоймовірному разі, якщо видалення колись лишило програму нездатною відновитися, оновитися чи видалитися, перевстановлення її від виробника зазвичай повертає файл, хоча Microsoft цього не гарантує. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Це нормально, їх можна було безпечно прибрати. InstallerClean прибирає лише ті файли, які Windows позначає як завершені, ніколи той, що ще потрібен програмі. У малоймовірному разі, якщо видалення колись лишило програму нездатною відновитися, оновитися чи видалитися, перевстановлення її від виробника зазвичай повертає файл, хоча Microsoft цього не гарантує. |
| Glad to help. There's a tip jar if you're feeling kind. | Радий, що знадобилося. Якщо ваша ласка, є куди докинути на каву. |

## Recycle Bin unavailable

| English | Українська |
| --- | --- |
| The Recycle Bin isn't available for this drive | Кошик недоступний для цього диска |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Тож цей {1} ({2}) не видалено. Ви можете перемістити його в безпечне місце або видалити назавжди. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Тож ці {0} {1} ({2}) не видалено. Ви можете перемістити їх у безпечне місце або видалити назавжди. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Видаляти його безпечно. InstallerClean прибирає лише ті файли, які Windows позначає як завершені, ніколи той, що ще потрібен програмі, а Кошик — це лише додатковий запобіжник. У малоймовірному разі, якщо видалення колись лишило програму нездатною відновитися, оновитися чи видалитися, перевстановлення її від виробника зазвичай повертає файл, хоча Microsoft цього не гарантує. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Видаляти їх безпечно. InstallerClean прибирає лише ті файли, які Windows позначає як завершені, ніколи той, що ще потрібен програмі, а Кошик — це лише додатковий запобіжник. У малоймовірному разі, якщо видалення колись лишило програму нездатною відновитися, оновитися чи видалитися, перевстановлення її від виробника зазвичай повертає файл, хоча Microsoft цього не гарантує. |

## Summaries and counts

| English | Українська |
| --- | --- |
| {0} file still needed | {0} файл ще потрібен |
| {0} files still needed | {0} файлів ще потрібно |
| {0} unneeded file to clean up | {0} непотрібний файл для очищення |
| {0} unneeded files to clean up | {0} непотрібних файлів для очищення |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} зареєстрований файл відсутній (його не видаляв InstallerClean). Зараз це не завдає клопоту, але в майбутньому відновлення, оновлення чи видалення тієї програми може не вдатися. Відкрийте «Деталі», щоб дізнатися, що робити. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} зареєстрованих файлів відсутні (їх не видаляв InstallerClean). Зараз це не завдає клопоту, але в майбутньому відновлення, оновлення чи видалення тих програм може не вдатися. Відкрийте «Деталі», щоб дізнатися, що робити. |
| {0} installed program could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | Під час цього сканування не вдалося прочитати {0} встановлену програму, тому заміщені патчі залишено на місці. Осиротілих файлів це не стосується. |
| {0} installed programs could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | Під час цього сканування не вдалося прочитати {0} встановлених програм, тому заміщені патчі залишено на місці. Осиротілих файлів це не стосується. |
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
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | Файли буде переміщено до Кошика. Якщо ви хочете зберегти резервні копії, скористайтеся кнопкою «Перемістити». |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | Ця папка на тому самому диску, тож саме переміщення не звільнить місця. Місце повернеться, коли ви видалите з неї файли, або ж можете натомість вибрати папку на іншому диску. |

## Error messages

| English | Українська |
| --- | --- |
| Access denied | Відмовлено в доступі |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows відмовив InstallerClean у доступі, тому роботу було зупинено. Нічого не було видалено.<br><br>InstallerClean уже працював від імені адміністратора, тож запускати його так ще раз не допоможе. Windows не повідомляє нічого більше про те, що саме відмовило в доступі, тож немає нічого конкретного, що варто спробувати. |
| Couldn't read the Windows Installer records | Не вдалося прочитати записи інсталятора Windows |
| Scan failed | Збій сканування |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in C:\Windows\Installer orphaned. InstallerClean stopped instead. Nothing has been removed. | Записи інсталятора Windows повернулися цілком порожніми: жодна встановлена програма й жодне оновлення не заявляє прав на кешований файл інсталятора. На робочому комп'ютері такого не буває (навіть у щойно встановленої Windows такі файли є), тож записи або пошкоджено, або їх не вдалося прочитати, і сканування, яке повірило б такій відповіді, помилково визнало б осиротілим кожен файл у C:\Windows\Installer. Замість цього InstallerClean зупинився. Нічого не було видалено. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer не дозволив InstallerClean перелічити встановлене. InstallerClean уже працював від імені адміністратора, тож запуск від імені адміністратора ще раз нічого не змінить. Без цього списку немає безпечного способу визначити, які кешовані файли ще потрібні, тож InstallerClean зупинився. Нічого не було видалено. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer не зміг надати InstallerClean читабельний список встановлених програм: {0} записів поспіль повернулися нечитабельними (останній код помилки {1}). Замість того щоб працювати зі списком, прочитаним лише частково, InstallerClean зупинився. Нічого не було видалено. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer так і не повідомив про кінець списку встановлених програм: InstallerClean припинив спроби після {0} записів (останній код помилки {1}). Списку без кінця довіряти не можна, тож InstallerClean зупинився. Нічого не було видалено. |
| Windows Installer couldn't give InstallerClean a readable list of one program's patches: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer не зміг надати InstallerClean читабельний список патчів однієї програми: {0} записів поспіль повернулися нечитабельними (останній код помилки {1}). Замість того щоб працювати зі списком, прочитаним лише частково, InstallerClean зупинився. Нічого не було видалено. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer так і не повідомив про кінець списку патчів однієї програми: InstallerClean припинив спроби після {0} записів (останній код помилки {1}). Списку без кінця довіряти не можна, тож InstallerClean зупинився. Нічого не було видалено. |
| InstallerClean couldn't square this scan with the Windows Installer records: every file Windows still lists as needed is missing from C:\Windows\Installer, while the files actually in the folder match nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean не зміг узгодити це сканування із записами інсталятора Windows: кожного файлу, який Windows досі вважає потрібним, немає в C:\Windows\Installer, а файли, що справді лежать у цій папці, не відповідають жодному запису. Жоден справжній комп'ютер так не виглядає, тож це вказує на проблему з читанням записів, а не на файли, які можна безпечно видалити. Для очищення нічого не запропоновано, і нічого не було видалено. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean не зміг прочитати достатньо записів інсталятора Windows, щоб напевно знати, що ще потрібно: список встановлених програм повернувся неповним, а читання тих самих записів прямо з реєстру теж призвело до помилок. Файл міг видаватися осиротілим лише тому, що запис, який його називає, виявився одним із нечитабельних, тож InstallerClean зупинився. Нічого не було видалено. |
| Invalid destination | Недійсне призначення |
| Could not write to destination | Не вдалося записати в призначення |
| Move failed | Не вдалося перемістити |
| Delete failed | Не вдалося видалити |
| The destination cannot be inside the Windows Installer folder. | Призначення не може бути всередині папки Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Призначення {0} вказує всередину системної папки Windows. Виберіть шлях поза %SystemRoot%, %ProgramFiles% та %ProgramData%. |
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
| This file is open or locked by another program, so nothing can move it just now. It was left in place; try again later. | Цей файл відкритий або заблокований іншою програмою, тож зараз його ніщо не може перемістити. Його залишено на місці; спробуйте ще раз пізніше. |
| These files are open or locked by another program, so nothing can move them just now. They were left in place; try again later. | Ці файли відкриті або заблоковані іншою програмою, тож зараз їх ніщо не може перемістити. Їх залишено на місці; спробуйте ще раз пізніше. |
| Windows reported a file error; the file was left in place. | Windows повідомив про помилку файлу; файл залишено на місці. |
| Windows reported file errors; these files were left in place. | Windows повідомив про помилки файлів; ці файли залишено на місці. |
| Something went wrong with this file; it was left in place. | З цим файлом щось пішло не так; його залишено на місці. |
| Something went wrong with these files; they were left in place. | З цими файлами щось пішло не так; їх залишено на місці. |
| Couldn't move this file to the Recycle Bin (error {0}), and InstallerClean can't tell you why from that code. The file was left in place. Try the Move button instead, since it doesn't use the Recycle Bin. | Не вдалося перемістити цей файл до Кошика (помилка {0}), і за цим кодом InstallerClean не може сказати чому. Файл залишено на місці. Спробуйте натомість кнопку «Перемістити», яка не використовує Кошик. |
| Windows refused access even with administrator rights (error {0}), and InstallerClean can't tell whether the problem is the file or the Recycle Bin. The file was left in place. The Move button will work if it's the Recycle Bin, but not if it's the file. | Windows відмовив у доступі навіть із правами адміністратора (помилка {0}), і InstallerClean не може визначити, у чому річ — у файлі чи в Кошику. Файл залишено на місці. Кнопка «Перемістити» допоможе, якщо річ у Кошику, але не допоможе, якщо річ у файлі. |
| This file is open or locked by another program (error {0}), so nothing can remove it just now. It was left in place; try again later. | Цей файл відкритий або заблокований іншою програмою (помилка {0}), тож зараз його ніщо не може видалити. Його залишено на місці; спробуйте ще раз пізніше. |
| Windows deleted this file outright rather than moving it to the Recycle Bin. InstallerClean asked for the Recycle Bin, and Windows did this instead. The file is gone. | Windows видалив цей файл безповоротно, а не перемістив його до Кошика. InstallerClean попросив Кошик, а Windows вчинив інакше. Файла більше немає. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Відмова перемістити файли до папки Windows Installer (призначення: {0}). |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Розташування для переміщення має бути повним шляхом до папки, що починається з літери диска або мережевої папки (наприклад, D:\Backup чи \\server\backup). InstallerClean не може використати такий шлях: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | Папка призначення змінилася, поки тривало переміщення (щось замінило чи перенаправило її), тож InstallerClean зупинився, щоб не записати файли не туди. Перевірте {0}, потім повторіть сканування і спробуйте ще раз. |
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
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | GitHub повернув повідомлення про помилку. Можливо, API випусків обмежує частоту запитів; спробуйте за кілька хвилин. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log фіксує необроблені винятки InstallerClean.<br># Під час підвищення прав повідомлення про винятки фреймворку<br># можуть містити шляхи до файлів із поточного сеансу (зокрема<br># профілі інших користувачів, перелічені запитами Windows<br># Installer). Повідомлення про збій мережі від перевірки оновлень<br># чи надсилання звіту можуть містити URL призначення та<br># розв'язану IP-адресу / адресу проксі. Відредагуйте обидва види<br># даних, перш ніж додавати цей файл до публічного звіту про ваду.<br> |

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
| Move the unneeded files to the Move location. | Перемістити непотрібні файли до вказаного розташування. |
| Move the unneeded files somewhere safe. You'll choose the folder next. | Перемістити непотрібні файли в безпечне місце. Папку ви виберете на наступному кроці. |
| Move the unneeded files to the Recycle Bin. | Перемістити непотрібні файли до Кошика. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Назва суб'єкта з вбудованого сертифіката Authenticode. Ланцюжок не перевірено. |
| Change language. The program will restart. | Змінити мову. Програму буде перезапущено. |

## Screen reader labels

| English | Українська |
| --- | --- |
| Donate | Підтримати |
| Buy me a cuppa (About window) | Пригостіть мене кавою (вікно «Про програму») |
| Cancel operation | Скасувати операцію |
| Cancel scan | Скасувати сканування |
| Cancel startup scan | Скасувати сканування під час запуску |
| Close | Закрити |
| Close window | Закрити вікно |
| Close result and return to main window | Закрити результат і повернутися до головного вікна |
| Leave a star on GitHub (About window) | Лишити зірку на GitHub (вікно «Про програму») |
| Minimise | Згорнути |
| Move all unneeded installer files to the Move location | Перемістити всі непотрібні файли інсталятора до вказаного розташування |
| Move all unneeded installer files to the Recycle Bin | Перемістити всі непотрібні файли інсталятора до Кошика |
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | «Видалити» переміщує непотрібні файли до Кошика. «Скасувати» закриває вікно без видалення. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | «Перемістити» кладе непотрібні файли до обраної папки призначення. «Скасувати» лишає їх там, де вони є. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Виберіть, що робити з непотрібними файлами: перемістити їх у безпечне місце, видалити назавжди або скасувати. |
| Move the unneeded files to a folder you choose | Перемістити непотрібні файли до обраної вами папки |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Видалити непотрібні файли назавжди, бо Кошик недоступний для цього диска |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Надсилається на nofaff.netlify.app. Лише лічильники та позначки. Ви побачите точний вміст перед надсиланням. |
| Say thanks | Подякувати |
| Send posts the report shown to No Faff. Cancel sends nothing. | «Надіслати» надсилає показаний звіт до No Faff. «Скасувати» не надсилає нічого. |
| Check for updates | Перевірити оновлення |
| Checks the GitHub releases API over HTTPS for a newer version. | Перевіряє API випусків GitHub через HTTPS на наявність новішої версії. |
| Opens the guide (README) on github.com in your browser. | Відкриває посібник (README) на github.com у вашому браузері. |
| Opens the issue tracker on github.com in your browser. | Відкриває список проблем (Issues) на github.com у вашому браузері. |
| When ticked, InstallerClean checks GitHub for a newer version when you run it. | Якщо позначено, InstallerClean під час кожного запуску перевіряє на GitHub наявність новішої версії. |
| Open the release page to download the newer version, or cancel to keep the current version. | Відкрийте сторінку випуску, щоб завантажити новішу версію, або скасуйте, щоб лишити поточну версію. |
| Apache 2.0 licence | Ліцензія Apache 2.0 |
| Opens the licence file on github.com in your browser. | Відкриває файл ліцензії на github.com у вашому браузері. |
| Move location | Куди перемістити |
| Products | Продукти |
| Patches | Патчі |
| Product details | Деталі продукту |
| Move location | Куди перемістити |
| Operation progress | Перебіг операції |
| Scan C:\Windows\Installer again | Просканувати C:\Windows\Installer ще раз |
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
| patch | патч |
| patches | патчів |

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
| Unknown argument: '{0}' | Невідомий аргумент: «{0}» |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Помилка: неочікуваний зайвий аргумент «{0}». Якщо в назві папки для переміщення є пробіл, візьміть увесь шлях у лапки: /m "D:\My Backup" |
| Cancelling... | Скасування... |
| Cancelled. | Скасовано. |
| Error: {0}. Details written to {1}. | Помилка: {0}. Деталі записано до {1}. |
| Error: {0}. The crash log could not be written. | Помилка: {0}. Не вдалося записати журнал збоїв. |
| Scanning C:\Windows\Installer... | Сканування C:\Windows\Installer... |
| Found {0} {1} to clean up ({2}). | Знайдено {0} {1} для очищення ({2}). |
| Nothing to do. | Немає чого робити. |
| Deleting {0} {1}... | Видалення: {0} {1}... |
| Deleted {0} {1}. | Видалено {0} {1}. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Помилка: Кошик недоступний для цього тому, тож нічого не видалено. Скористайтеся /m, щоб перемістити файли натомість, або знову ввімкніть Кошик і запустіть ще раз. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Помилка: не вказано розташування для переміщення. Скористайтеся /m ШЛЯХ. (Типове значення, задане в графічному інтерфейсі, діє лише для поточного користувача і не застосовується до запусків за розкладом чи від імені службового облікового запису.) |
| Error: destination cannot be inside the Windows Installer folder. | Помилка: призначення не може бути всередині папки Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Помилка: призначення має бути повним шляхом. Отримано: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Помилка: призначення {0} вказує всередину системної папки Windows. Виберіть шлях поза %SystemRoot%, %ProgramFiles% та %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Помилка: щось саме зараз використовує Windows Installer, зазвичай це Windows Update або програма, що встановлюється у фоні. Переміщення та видалення заблоковано, доки це триває. Спробуйте ще раз, коли воно завершиться. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Помилка: на цій машині призупинено попередню транзакцію Windows Installer. Поновіть або відкотіть те встановлення (чи перезавантажте Windows), перш ніж очищати кеш. |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Помилка: поставлена в чергу після перезавантаження файлова операція націлена на кеш інсталятора ({0}). Перезавантажте Windows, щоб завершити ту операцію, перш ніж очищати. |
| Moving {0} {1} to {2}... | Переміщення: {0} {1} до {2}... |
| Moved {0} {1}. | Переміщено {0} {1}. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Інший процес InstallerClean утримує блокування єдиного екземпляра (графічний інтерфейс чи інший запуск CLI). Вихід 75 (тимчасовий); можна безпечно повторити пізніше. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Примітка: не вдалося записати до журналу подій. Перевірте дозволи журналу «Програма» чи групову політику. |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - очищення C:\Windows\Installer |
| Usage: | Використання: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Показати цю довідку (також приймає /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Вивести версію (також приймає -v) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s         Лише сканувати, перелічити непотрібні файли |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d         Видалити непотрібні файли (Кошик) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m         Перемістити в збережене типове розташування |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ШЛЯХ    Перемістити за вказаним шляхом |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli — це консольний процес, що блокує командний рядок до |
| until it finishes; redirect or pipe its output as you would any | завершення; перенаправляйте чи передавайте його вивід, як у будь-якого |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | іншого консольного exe. Графічний інтерфейс міститься поруч, у InstallerClean.exe. |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | Збережене типове розташування діє лише для поточного користувача; для запусків за розкладом чи від імені SYSTEM потрібно вказати /m ШЛЯХ. |
| Exit codes: | Коди виходу: |
|   0   success: every flagged file was processed |   0   успіх: оброблено кожен позначений файл |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   невдача: нічого не оброблено (хибні аргументи, збій сканування, усі файли зазнали збою) |
|   2   partial: some files processed, some failed |   2   частково: частину файлів оброблено, частину ні |
|   75  transient: a temporary condition blocked the run (see the message) |   75  тимчасова: запуск заблокувала тимчасова умова (див. повідомлення) |
|   130 cancelled (Ctrl+C) |   130 скасовано (Ctrl+C) |
