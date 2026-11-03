# InstallerClean in 繁體中文 (Traditional Chinese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Traditional Chinese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Traditional Chinese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.zh-Hant.resx`](../../src/InstallerClean.Core/Resources/Strings.zh-Hant.resx), so do not edit it by hand. The Traditional Chinese translation itself lives in [`gen-strings-zh-Hant.mjs`](../../scripts/translations/gen-strings-zh-Hant.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | 繁體中文 |
| --- | --- |
| InstallerClean | InstallerClean |
| About | 關於 |
| Files left alone | 原樣保留的檔案 |
| Unneeded files that are safe to delete | 不需要的檔案，可安全刪除 |
| The anonymous report | 匿名報告 |
| InstallerClean couldn't read the report. | InstallerClean 無法讀取這份報告。 |

## Section headings

| English | 繁體中文 |
| --- | --- |
| PATCHES | 修補程式 |
| PRODUCT DETAILS | 產品詳細資料 |
| BACKUP FOLDER | 備份資料夾 |
| SAY THANKS | 說聲謝謝 |

## Buttons and actions

| English | 繁體中文 |
| --- | --- |
| _About | 關於(_A) |
| Copy | 複製 |
| Cut | 剪下 |
| Paste | 貼上 |
| Select all | 全選 |
| _Browse... | 瀏覽(_B)... |
| _Cancel | 取消(_C) |
| Carry on _without it | 不再等候，繼續進行(_W) |
| Check for _updates | 檢查更新(_U) |
| _Close | 關閉(_C) |
| _Close without tipping | 不打賞，直接關閉(_C) |
| _Delete permanently | 永久刪除(_D) |
| _Done | 完成(_D) |
| Details | 詳細資料 |
| _Leave a tip | 打賞(_L) |
| Leave a tip | 打賞 |
| _Leave a tip | 打賞(_L) |
| Leave a _star on GitHub | 在 GitHub 上給個星星(_S) |
| Apache 2.0 licence | Apache 2.0 授權條款 |
| _Move | 移動(_M) |
| Path to folder if you move rather than delete. | 若選擇移動而非刪除，請在此填寫資料夾路徑。 |
| _Re-scan | 重新掃描(_R) |

## About window

| English | 繁體中文 |
| --- | --- |
| Guide and FAQ | 指南與常見問題 |
| Report a problem | 回報問題 |
| Check for updates automatically | 自動檢查更新 |

## Field labels

| English | 繁體中文 |
| --- | --- |
| Reason | 原因 |
| Author | 作者 |
| Application | 應用程式 |
| Title | 標題 |
| Subject | 主旨 |
| Keywords | 關鍵字 |
| Signing certificate | 簽章憑證 |
| File size | 檔案大小 |
| Comment | 註解 |
| Product name | 產品名稱 |
| File | 檔案 |
| Size | 大小 |
| Patches | 修補程式 |
| (unknown) | （不明） |
| (no program) | （無程式） |
| (patches only) | （僅修補程式） |
| missing | 遺失 |

## Status and progress

| English | 繁體中文 |
| --- | --- |
| Scanning... | 正在掃描... |
| Cancelling... | 正在取消... |
| Starting scan... | 正在開始掃描... |
| Asking Windows about installed software... | 正在向 Windows 查詢已安裝的軟體... |
| Scanning installer cache folder... | 正在掃描安裝程式快取資料夾... |
| Checking registry for additional packages... | 正在檢查登錄中的其他封裝... |
| Matching files against installed programs... | 正在比對檔案與已安裝的程式... |
| {0} of {1} | {0}/{1} |
| Checking remaining files... | 正在檢查其餘檔案... |
| Waiting for drive {0} to respond... | 正在等候磁碟機 {0} 回應... |
| Waiting for {0} to respond... | 正在等候 {0} 回應... |
| Any file still to be checked against it will be left alone. | 仍需與它核對的檔案都會原樣保留。 |
| Scan complete ({0}) | 掃描完成（{0}） |
| Found {0} {1} you can safely delete. | 找到 {0} 個可安全刪除的{1}。 |
| Preparing destination folder... | 正在準備目的地資料夾... |
| Moving unneeded files... | 正在移動不需要的檔案... |
| Deleting unneeded files... | 正在刪除不需要的檔案... |
| Move cancelled. {0} of {1} {2} processed. | 移動已取消。已處理 {1} 個{2}中的 {0} 個。 |
| Delete cancelled. {0} of {1} {2} processed. | 刪除已取消。已處理 {1} 個{2}中的 {0} 個。 |
| {0}. Details are in {1}. | {0}。詳細資料請見 {1}。 |
| {0}. The crash log could not be written. | {0}。無法寫入當機記錄檔。 |
| {0}. Details are in {1}. | {0}。詳細資料請見 {1}。 |
| {0}. The crash log could not be written. | {0}。無法寫入當機記錄檔。 |
| Access denied. Windows refused the scan. | 存取被拒。Windows 拒絕了這次掃描。 |
| Scan stopped. | 掃描已停止。 |
| Scan cancelled. | 掃描已取消。 |
| Ready | 就緒 |
| Scan failed ({0}). Details in {1}. | 掃描失敗（{0}）。詳細資料請見 {1}。 |
| Scan failed ({0}). The crash log could not be written. | 掃描失敗（{0}）。無法寫入當機記錄檔。 |

## Main screen text

| English | 繁體中文 |
| --- | --- |
| Any unneeded files below are safe to delete. | 下列不需要的檔案都可以安全刪除。 |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | 它們位於 {InstallerFolder}。InstallerClean 會就每個已安裝的程式詢問 Windows：沒有任何程式認領的檔案會列出（{0}），已被較新的修補程式取代、且沒有任何程式能復原到它的檔案也會列出（{1}）。 |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | 將它們移到您選擇的備份資料夾，等您確定程式仍能照常更新和解除安裝後，再刪除那個資料夾。把它們放回 {InstallerFolder} 就能恢復原狀。或者現在就永久刪除。 |
| Nothing scanned yet. | 尚未掃描。 |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | 按下「重新掃描」，在 {InstallerFolder} 中尋找已沒有任何程式需要的安裝程式檔案。 |
| Nothing to show until the next scan. | 下次掃描前沒有可顯示的內容。 |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | 移動或刪除後執行的掃描沒有完成，因此沒有清單可顯示。按下「重新掃描」即可再次掃描 {InstallerFolder}。 |
| What stopped it is recorded in {0}. | 停止的原因已記錄在 {0}。 |
| These files can't be cleaned up right now. | 這些檔案目前無法清理。 |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | 目前有程式正在使用 Windows Installer，例如 Windows Update 或在背景安裝的程式。在這期間移動和刪除會暫停，InstallerClean 不會在 {InstallerFolder} 變動時碰它。等它結束後重新掃描，移動和刪除就會恢復。 |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此移動和刪除已暫停，InstallerClean 不會碰 {InstallerFolder}。請重新啟動 Windows 並重新掃描。如果 Windows 仍然拒絕，這台電腦就不是 InstallerClean 能清理的。 |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean 無法檢查 Windows Installer 是否忙碌，因此移動和刪除已暫停，InstallerClean 不會碰 {InstallerFolder}。請重新啟動 Windows 並重新掃描。如果仍然無法檢查，這台電腦就不是 InstallerClean 能清理的。 |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean 無法檢查 Windows Installer 是否忙碌，因為 Windows 傳回了錯誤：{0}（錯誤 {1}）。移動和刪除已暫停，InstallerClean 不會碰 {InstallerFolder}。請重新啟動 Windows 並重新掃描。如果仍然無法檢查，這台電腦就不是 InstallerClean 能清理的。 |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | 這台電腦上有一筆先前的 Windows Installer 交易已暫停。請先繼續或復原那次安裝（或重新啟動 Windows），再清理 {InstallerFolder}。 |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows 已排定在下次重新啟動時重新命名一個檔案，這會影響 {InstallerFolder}。請先重新啟動 Windows 再清理。 |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | 有一項檔案作業已排定在下次重新啟動時執行，而 InstallerClean 無法得知它涉及哪些檔案，因此無法排除這些檔案位於 {InstallerFolder} 的可能。請先重新啟動 Windows 再清理。 |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean 在碰 {InstallerFolder} 之前會檢查幾項 Windows 設定，其中一項無法讀取，因此無法判斷是否有安裝程式作業正在執行或正在等候重新啟動。請重新啟動 Windows 並重新掃描。如果該設定仍然無法讀取，這台電腦就不是 InstallerClean 能清理的。 |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer 有作業正在進行，因此移動和刪除已暫停。InstallerClean 不會在 {InstallerFolder} 變動時碰它。等作業結束後重新掃描，移動和刪除就會恢復。 |
| Select a file to view details. | 選取一個檔案以檢視詳細資料。 |
| Select a product to view details. | 選取一個產品以檢視詳細資料。 |
| No metadata available. | 沒有可用的中繼資料。 |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | 這個安裝程式檔案不見了。它現在不會造成任何問題，要等到有一天您嘗試更新或解除安裝它所屬的程式時才會。到時這個步驟可能會失敗，因為 Windows 會尋找這個檔案，而它不在那裡。<br><br>要把它放回去，您需要目前所用版本的安裝程式。請向該程式的開發者取得，然後直接執行它，覆蓋安裝現有的程式。較新的版本不行：新版本必須先移除您現有的版本，而需要這個檔案的正是這個步驟。先解除安裝也行不通，原因相同。這應該會復原這個檔案並保留您的設定，但 Microsoft 並不保證。 |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README 以 Microsoft 自己的原話[說明了這個資料夾]，以及如何復原檔案。 |
| (none) | （無） |

## Reasons a file is unneeded

| English | 繁體中文 |
| --- | --- |
| Orphaned | 孤立 |
| Superseded | 已被取代 |
| Obsoleted | 已廢棄 |

## Completion screen

| English | 繁體中文 |
| --- | --- |
| All clean | 乾乾淨淨 |
| Nothing to clean up in {InstallerFolder} | {InstallerFolder} 中沒有需要清理的內容 |
| Scanned {0} {1} in {2} | 掃描了 {0} 個{1}，費時：{2} |
| Nothing offered on this PC | 這台電腦上沒有可提供的檔案 |
| One file ({2}) was held back because Windows shows it was changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check it again. | 已保留 1 個檔案（{2}），因為 Windows 顯示它在 InstallerClean 開始掃描後被變更過。如果當時有程式正在安裝或更新，請等它完成後再掃描一次，InstallerClean 會重新檢查它。 |
| {0} {1} ({2}) were held back because Windows shows they were changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check them again. | 已保留 {0} 個{1}（{2}），因為 Windows 顯示它們在 InstallerClean 開始掃描後被變更過。如果當時有程式正在安裝或更新，請等它完成後再掃描一次，InstallerClean 會重新檢查它們。 |
| Delete that folder when you're satisfied all is well. | 等您確定一切正常後，再刪除那個資料夾。 |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | 等您確定一切正常後，再刪除那個資料夾。在那之前，空間不會真正釋放。 |
| {0} freed | 已釋放 {0} |
| {0} moved | 已移動 {0} |
| Nothing was moved | 沒有移動任何檔案 |
| Nothing was deleted | 沒有刪除任何檔案 |
| {0} file could not be moved. | {0} 個檔案無法移動。 |
| {0} files could not be moved. | {0} 個檔案無法移動。 |
| {0} file could not be deleted. | {0} 個檔案無法刪除。 |
| {0} files could not be deleted. | {0} 個檔案無法刪除。 |
| {0} {1} moved to: {2} | 已將 {0} 個{1}移到：{2} |
| {0} {1} moved to: {2} | 已將 {0} 個{1}移到：{2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | 已保留 {0} 個檔案。掃描認為它不需要，但最後的檢查無法確認這一點。 |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | 已保留 {0} 個檔案。掃描認為它們不需要，但最後的檢查無法確認這一點。 |
| {0} {1} kept in place, because Windows has a record of the program named inside. | 有 {0} 個{1}留在原處，因為 Windows 有檔案內所標示程式的記錄。 |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | 有 {0} 個{1}留在原處，因為 InstallerClean 在檔案內找不到程式名稱。 |
| Moved {0} of {1} {2} to {3} before you cancelled. | 在您取消前，已將 {1} 個{2}中的 {0} 個移到 {3}。 |
| Permanently deleted {0} of {1} {2} before you cancelled. | 在您取消前，已永久刪除 {1} 個{2}中的 {0} 個。 |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | 要復原很簡單。把它們移回 {InstallerFolder}，一切就會恢復原狀。 |
| {0} {1} permanently deleted | 已永久刪除 {0} 個{1} |
| {0} {1} permanently deleted | 已永久刪除 {0} 個{1} |
| Any tip is much appreciated | 任何打賞都非常感謝 |
| Send _anonymous report | 傳送匿名報告(_A) |
| The report adds your result to [this chart] on GitHub, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | 報告會把您的結果加到 GitHub 上的[這張圖表]，圖表顯示人們用 InstallerClean 釋放了多少空間。報告也包含以下技術細節，幫助我發現程式錯誤： |
| How much space was freed, and how many files were moved or deleted | 釋放了多少空間，以及移動或刪除了多少個檔案 |
| Whether a Move went to the same drive, another drive, a removable drive or a network share | 移動的目的地是同一個磁碟機、另一個磁碟機、抽取式磁碟機，還是網路共用資料夾 |
| How long the scan and the Move or Delete took | 掃描以及移動或刪除各花了多少時間 |
| How many installer files there are of each kind, and how much space they take | 每種安裝程式檔案各有多少個，以及它們佔用多少空間 |
| How many files were left alone, and why | 有多少個檔案原樣保留，以及原因 |
| How often InstallerClean waited for a slow drive or network share, and how many it stopped waiting for | InstallerClean 等候緩慢的磁碟機或網路共用資料夾的次數，以及不再等候的有幾個 |
| How many programs and patches Windows has a record of | Windows 記錄了多少個程式和修補程式 |
| Whether Windows still makes old-style short file names | Windows 是否仍會產生舊式的短檔名 |
| Whether Windows is 10 or 11 and x64 or Arm, and which language and country or region it's set to | Windows 是 10 還是 11、是 x64 還是 Arm，以及設定的是哪種語言和哪個國家或地區 |
| InstallerClean's version, how you got it (setup, portable, slim, Scoop, the command-line tool on its own, built from source, or not known), whether the report was sent from InstallerClean or the command-line tool, and the language it was showing | InstallerClean 的版本、取得方式（setup、portable、slim、Scoop、單獨的命令列工具、從原始碼建置，或未知）、報告是由 InstallerClean 還是命令列工具傳送的，以及其顯示的語言 |
| Counts of anything that went wrong | 所有出錯情況的次數 |
| Each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | 這些內容都是數字或標籤。報告中沒有任何內容能指出您、您的電腦、您的檔案或程式，我也從來看不到您的 IP 位址。 |
| See exactly what's sent | 查看傳送的確切內容 |

## Summaries and counts

| English | 繁體中文 |
| --- | --- |
| {0} file left alone | {0} 個檔案原樣保留 |
| {0} files left alone | {0} 個檔案原樣保留 |
| {0} unneeded file to clean up | {0} 個不需要的檔案可清理 |
| {0} unneeded files to clean up | {0} 個不需要的檔案可清理 |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | Windows 記錄了 {0} 個不在 {InstallerFolder} 中的檔案：{1}。平常使用不會有問題，但該程式的更新或解除安裝可能會失敗。請開啟「詳細資料」了解該怎麼做。 |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | Windows 記錄了 {0} 個不在 {InstallerFolder} 中的檔案：{1}。平常使用不會有問題，但這些程式的更新或解除安裝可能會失敗。請開啟「詳細資料」了解該怎麼做。 |
| {0} other program | 另外 {0} 個程式 |
| {0} other programs | 另外 {0} 個程式 |
| {0} file with no program named in the records | {0} 個在記錄中沒有標明程式的檔案 |
| {0} files with no program named in the records | {0} 個在記錄中沒有標明程式的檔案 |
| InstallerClean carried on without drive {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean 已不再等候磁碟機 {0}，繼續進行，並將仍需與它核對的檔案都原樣保留。等它恢復正常回應後，請按「重新掃描」。 |
| InstallerClean carried on without {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean 已不再等候 {0}，繼續進行，並將仍需與它核對的檔案都原樣保留。等它恢復正常回應後，請按「重新掃描」。 |
| InstallerClean carried on without more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, Re-scan. | InstallerClean 已不再等候多個磁碟機或共用資料夾（{0}），繼續進行，並將仍需與其中任何一個核對的檔案都原樣保留。等它們恢復正常回應後，請按「重新掃描」。 |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has finished. | 掃描開始時，有程式正在使用 Windows Installer，例如 Windows Update 或正在安裝的程式，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。等它結束後，請按「重新掃描」。 |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then Re-scan. | 掃描開始時，這台電腦上有一筆 Windows Installer 交易已暫停，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請繼續或復原那次安裝，或重新啟動 Windows，然後按「重新掃描」。 |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | 掃描開始時，Windows 已排定在下次重新啟動時重新命名一個檔案，這會影響 {InstallerFolder}，因此 InstallerClean 沒有提供該資料夾中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows，然後按「重新掃描」。 |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | 掃描開始時，有一項檔案作業已排定在下次重新啟動時執行，而 InstallerClean 無法得知它涉及哪些檔案，因此沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows，然後按「重新掃描」。 |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | 掃描開始時，Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 並按「重新掃描」。 |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | 掃描開始時，InstallerClean 無法讀取它會檢查的一項 Windows 設定，因此無法判斷是否有安裝程式作業正在執行或正在等候重新啟動，於是沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 並按「重新掃描」。 |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | 掃描開始時，InstallerClean 無法檢查 Windows Installer 是否忙碌，因此沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 並按「重新掃描」。 |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | 掃描開始時，InstallerClean 無法檢查 Windows Installer 是否忙碌，因為 Windows 傳回了錯誤：{0}（錯誤 {1}）。因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 並按「重新掃描」。 |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has cleared. | 掃描開始時，Windows Installer 有作業正在進行或等候中，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。等它結束後，請按「重新掃描」。 |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan. | 掃描期間，這台電腦的時鐘倒退或變慢了，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請按「重新掃描」。 |
| {0} of {1} {2} | {1} 個{2}中的 {0} 個 |
| {0} unneeded {1} ({2}) | {0} 個不需要的{1}（{2}） |
| {0} file left alone ({1}) | {0} 個檔案原樣保留（{1}） |
| {0} files left alone ({1}) | {0} 個檔案原樣保留（{1}） |
| {0} missing | 遺失 {0} 個 |
| {0} missing | 遺失 {0} 個 |

## Confirmation dialogs

| English | 繁體中文 |
| --- | --- |
| Move {0} {1} ({2})? | 要移動 {0} 個{1}（{2}）嗎？ |
| This file will be moved to: | 這個檔案將移到： |
| These files will be moved to: | 這些檔案將移到： |
| Delete {0} {1} ({2})? | 要刪除 {0} 個{1}（{2}）嗎？ |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | 這個檔案將被永久刪除。這麼做是安全的，但如果您想留個備份，請改用移動。 |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | 這些檔案將被永久刪除。這麼做是安全的，但如果您想留個備份，請改用移動。 |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | 那個資料夾在同一個磁碟機上，所以要等您刪除它，空間才會回來。如果想立刻拿回空間，請改選另一個磁碟機上的資料夾。 |

## Error messages

| English | 繁體中文 |
| --- | --- |
| This is also recorded in {0}. | 這也會記錄在 {0}。 |
| Access denied | 存取被拒 |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows 拒絕讓 InstallerClean 存取，因此它已停止。沒有移除任何內容。<br><br>InstallerClean 本來就是以系統管理員身分執行，所以再用這種方式啟動一次也沒有幫助。Windows 沒有進一步說明是什麼拒絕了存取，因此沒有具體可以嘗試的辦法。 |
| InstallerClean stopped | InstallerClean 已停止 |
| Scan failed | 掃描失敗 |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Windows Installer 的記錄傳回的內容完全是空的：沒有任何一個已安裝的程式或更新認領快取的安裝程式檔案。正常運作的電腦不會出現這種情況（就連剛安裝好的 Windows 也會有一些），所以要不是記錄已損毀，就是無法讀取；而相信這個結果的掃描，會把 {InstallerFolder} 中的每個檔案都誤判為孤立。InstallerClean 因此停了下來。沒有移除任何內容。 |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer 不讓 InstallerClean 列出已安裝的內容。InstallerClean 本來就是以系統管理員身分執行，所以再以系統管理員身分執行一次也不會有任何改變。沒有這份清單，就無法安全地判斷哪些快取檔案仍然需要，因此 InstallerClean 停了下來。沒有移除任何內容。 |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer 無法提供 InstallerClean 一份可讀取的已安裝程式清單：它讀取了 {1} 個{2}，接著有一個項目傳回時無法讀取（錯誤碼 {0}）。InstallerClean 沒有依據只讀到一部分的清單繼續，而是停了下來。沒有移除任何內容。 |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer 始終沒有發出已安裝程式清單結束的訊號：InstallerClean 讀取了 {2} 個{3}，然後在 {0} 個項目後放棄（最後的錯誤碼為 {1}）。沒有結尾的清單無法信任，因此 InstallerClean 停了下來。沒有移除任何內容。 |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer 始終沒有發出某個程式的修補程式清單結束的訊號：InstallerClean 讀取了 {2} 個{3}，然後在 {0} 個項目後放棄（最後的錯誤碼為 {1}）。沒有結尾的清單無法信任，因此 InstallerClean 停了下來。沒有移除任何內容。 |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean 無法將 Windows Installer 的記錄與 {InstallerFolder} 中的內容對應起來。記錄所指向的內容幾乎都不在那裡，而那裡的內容也幾乎都沒有任何記錄提到，因此無法證明任何檔案是不需要的。沒有提供任何檔案，也沒有移除任何內容。 |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean 無法將 Windows Installer 的記錄與 {InstallerFolder} 中的內容對應起來。資料夾裡有檔案，但沒有任何一筆記錄指向其中的任何內容，因此無法證明任何檔案是不需要的。沒有提供任何檔案，也沒有移除任何內容。 |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. Nothing has been offered and nothing has been removed. | InstallerClean 無法掃描這台電腦上的 {InstallerFolder}。Windows 回報該資料夾位於 {0}，而 InstallerClean 只會掃描 Windows 回報位於其原本路徑上的 Installer 資料夾。沒有提供任何檔案，也沒有移除任何內容。 |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. Nothing has been offered and nothing has been removed. | Windows 回報這台電腦上沒有 {InstallerFolder}，因此 InstallerClean 已停止。沒有提供任何檔案，也沒有移除任何內容。 |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. Nothing has been offered and nothing has been removed. | InstallerClean 嘗試列出 {InstallerFolder} 中的檔案時，Windows 回報了錯誤，因此 InstallerClean 無法取得完整的清單，已停止。沒有提供任何檔案，也沒有移除任何內容。 |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean 能讀取的 Windows Installer 記錄不夠多，無法確定哪些內容仍然需要：已安裝程式的清單傳回時並不完整，而直接從登錄讀取同樣的記錄也發生了錯誤。一個檔案可能只因為提到它的那筆記錄剛好讀取不到，看起來就像是孤立的，因此 InstallerClean 停了下來。沒有移除任何內容。 |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean 無法讓 Windows 解析出 {InstallerFolder} 的真實路徑，因此無法證明任何檔案位於其中，也沒有提供任何檔案供清理。這次掃描什麼都沒找到，是因為那項檢查失敗，而不是因為資料夾很乾淨。沒有移除任何內容。 |
| Nothing was deleted | 沒有刪除任何檔案 |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此它無法排除某個檔案在過程中變成需要的可能，沒有刪除任何檔案。 |
| Nothing was moved | 沒有移動任何檔案 |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此它無法排除某個檔案在過程中變成需要的可能，沒有移動任何檔案。 |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | Windows Installer 用一個鎖定來防止兩個程式同時變更已安裝的軟體，而 InstallerClean 無法取得這個鎖定，因此無法排除某個檔案在過程中變成需要的可能，沒有刪除任何檔案。請再試一次，如果一直發生，請重新啟動 Windows。 |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | Windows Installer 用一個鎖定來防止兩個程式同時變更已安裝的軟體，而 InstallerClean 無法取得這個鎖定，因此無法排除某個檔案在過程中變成需要的可能，沒有移動任何檔案。請再試一次，如果一直發生，請重新啟動 Windows。 |
| Invalid destination | 目的地無效 |
| Move stopped | 移動已停止 |
| Couldn't use that backup folder | 無法使用這個備份資料夾 |
| Move failed | 移動失敗 |
| Delete failed | 刪除失敗 |
| Setting not saved | 設定未儲存 |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | 無法儲存這項變更。InstallerClean 下次啟動時會回到先前的設定。 |
| The destination cannot be inside the Windows Installer folder. | 目的地不能位於 Windows Installer 資料夾內。 |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | 目的地 {0} 解析後位於 Windows 系統資料夾之下。請選擇 %SystemRoot%、%ProgramFiles%、%ProgramFiles(x86)% 和 %ProgramData% 以外的路徑。 |
| Not enough space | 空間不足 |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | {0} 的空間不夠<br><br>需要：{1}<br>可用：{2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | 您沒有寫入 {0} 的權限。<br>請改用您的使用者設定檔中的資料夾，或您擁有的磁碟機。 |
| The path {0} is too long for Windows. Pick a shorter path. | 路徑 {0} 對 Windows 來說太長了。請選擇較短的路徑。 |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | 資料夾 {0} 不存在，也無法建立。請檢查磁碟機代號或網路路徑。 |
| Windows cannot write to {0}.<br>Details in {1}. | Windows 無法寫入 {0}。<br>詳細資料請見 {1}。 |
| Windows cannot write to {0}. The crash log could not be written. | Windows 無法寫入 {0}。無法寫入當機記錄檔。 |
| Cannot write to {0}.<br>Details in {1}. | 無法寫入 {0}。<br>詳細資料請見 {1}。 |
| Cannot write to {0}. The crash log could not be written. | 無法寫入 {0}。無法寫入當機記錄檔。 |
| File no longer exists. | 檔案已不存在。 |
| Source file is a symlink or junction; refused for safety. | 來源檔案是符號連結或連接點；基於安全考量已拒絕。 |
| This file is not directly inside the Windows Installer folder; refused for safety. | 這個檔案並非直接位於 Windows Installer 資料夾內；基於安全考量已拒絕。 |
| Windows refused access to this file; it was left in place. | Windows 拒絕存取這個檔案；檔案已留在原處。 |
| Windows refused access to these files; they were left in place. | Windows 拒絕存取這些檔案；檔案已留在原處。 |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | 這個檔案正被另一個程式開啟或鎖定，所以目前無法移除。它已留在原處；請稍後再試。 |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | 這些檔案正被另一個程式開啟或鎖定，所以目前無法移除。它們已留在原處；請稍後再試。 |
| Windows reported a file error; the file was left in place. | Windows 回報了一個檔案錯誤；檔案已留在原處。 |
| Windows reported file errors; these files were left in place. | Windows 回報了檔案錯誤；這些檔案已留在原處。 |
| Something went wrong with this file; it was left in place. | 這個檔案出了點問題；它已留在原處。 |
| Something went wrong with these files; they were left in place. | 這些檔案出了點問題；它們已留在原處。 |
| Refusing to move files into the Windows Installer folder (destination: {0}). | 拒絕將檔案移到 Windows Installer 資料夾（目的地：{0}）。 |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | 備份資料夾必須是指向某個資料夾的完整路徑，以磁碟機代號或網路共用資料夾開頭（例如 D:\Backup 或 \\server\backup）。InstallerClean 無法使用這個路徑：{0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean 已無法確認備份資料夾，因此沒有再繼續。請檢查 {0}，然後重新掃描並再試一次。 |
| Cannot write to {0}. | 無法寫入 {0}。 |
| You can save files in {0} but not open them again, so InstallerClean won't move files there. | 您可以在 {0} 中儲存檔案，但無法再次開啟，因此 InstallerClean 不會將檔案移到那裡。 |
| A file called '{0}' is already in the backup folder. | 備份資料夾中已經有一個名為「{0}」的檔案。 |

## Update check

| English | 繁體中文 |
| --- | --- |
| Check for updates | 檢查更新 |
| Checking... | 正在檢查... |
| Up to date. | 已是最新版本。 |
| InstallerClean {0} is available. | 已推出 InstallerClean {0} 版。 |
| It's strongly recommended to use the latest version. | 強烈建議使用最新版本。 |
| Download it from GitHub | 從 GitHub 下載 |
| Download .NET Framework 4.8 from Microsoft | 從 Microsoft 下載 .NET Framework 4.8 |
| Update now | 立即更新 |
| This copy was installed by Scoop. To update it, close InstallerClean and run: | 此副本由 Scoop 安裝。若要更新，請關閉 InstallerClean 並執行： |
| This copy was installed by Scoop for all users. To update it, close InstallerClean, open a terminal as administrator and run: | 此副本由 Scoop 為所有使用者安裝。若要更新，請關閉 InstallerClean，以系統管理員身分開啟終端機並執行： |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean to update. | 此版本需要 .NET Framework 4.8。請從 Microsoft 安裝，重新啟動電腦，然後開啟 InstallerClean 進行更新。 |
| InstallerClean {0} can't be installed automatically. | InstallerClean {0} 無法自動安裝。 |
| InstallerClean can't tell whether its setup installed it, so it can't update itself. | InstallerClean 無法判斷自己是否由其安裝程式安裝，因此無法自行更新。 |
| Copied | 已複製 |
| Couldn't copy it. Select the command and copy it yourself. | 無法複製。請選取該命令並手動複製。 |
| _Continue without updating | 不更新，直接繼續(_C) |
| Downloading InstallerClean {0}... | 正在下載 InstallerClean {0}... |
| Checking the download... | 正在檢查下載的檔案... |
| The update couldn't be downloaded. | 無法下載更新。 |
| The update couldn't be installed. | 無法安裝更新。 |
| Couldn't reach GitHub. Check your internet connection and try again. | 無法連線到 GitHub。請檢查您的網際網路連線，然後再試一次。 |
| GitHub returned an error response. Try again in a few minutes. | GitHub 傳回了錯誤回應。請過幾分鐘再試一次。 |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | GitHub 的回應中沒有可辨識的發行版本。請稍後再試，或直接開啟發行頁面。 |
| The check timed out. Your connection to GitHub may be slow; try again. | 檢查逾時。您與 GitHub 的連線可能較慢；請再試一次。 |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | 檢查因不明原因失敗。如果您需要回報這個問題，詳細資料在 {0}。 |
| The check failed for an unknown reason. The crash log could not be written. | 檢查因不明原因失敗。無法寫入當機記錄檔。 |

## Opening links in your browser

| English | 繁體中文 |
| --- | --- |
| Couldn't open your browser | 無法開啟您的瀏覽器 |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | 連結已在您的剪貼簿中，您可以自己貼上：<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean 也無法把連結複製到剪貼簿，連結在這裡：<br><br>{0} |

## Startup and crashes

| English | 繁體中文 |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | 它已經在執行了。 |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | 發生未預期的錯誤，InstallerClean 必須關閉。<br><br>{0}<br><br>詳細資料已寫入：<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | 發生未預期的錯誤，InstallerClean 必須關閉。<br><br>{0}<br><br>無法寫入當機記錄檔。 |
| Startup error | 啟動錯誤 |
| Failed to start ({0}). Details written to:<br>{1} | 無法啟動（{0}）。詳細資料已寫入：<br>{1} |
| Failed to start ({0}). The crash log could not be written. | 無法啟動（{0}）。無法寫入當機記錄檔。 |
| Administrator rights needed | 需要系統管理員權限 |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed.<br><br>Run InstallerClean again as administrator. | InstallerClean 需要系統管理員權限才能看到這台電腦上安裝的每個程式，但它目前不是以系統管理員身分執行，所以不會掃描。沒有做任何變更。<br><br>請以系統管理員身分重新執行 InstallerClean。 |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log 記錄 InstallerClean 未處理的例外狀況。<br># 在提高權限執行時，架構的例外狀況訊息可能包含目前工作階段中的<br># 檔案路徑（包括 Windows Installer 查詢所列舉的其他使用者的設定檔）。<br># 更新檢查或結果記錄上傳的網路失敗訊息，可能包含目的地 URL 以及<br># 解析出的 IP 或 Proxy 位址。關於無法讀取的 Windows Installer 記錄<br># 的項目，可能包含 Windows 帳戶 SID（S-1-5-21-...）以及已安裝軟體的<br># 產品代碼。<br># 將這個檔案附加到公開的錯誤報告之前，請先刪除這三類資訊。<br> |

## Tooltips (hover text)

| English | 繁體中文 |
| --- | --- |
| Any tip is much appreciated | 任何打賞都非常感謝 |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | 已要求取消。InstallerClean 正在等候目前的步驟到達可以停下來的地方。在大量 I/O 或 MSI 資料庫呼叫期間，這可能需要幾秒鐘。 |
| Close | 關閉 |
| I really should charge for InstallerClean - because it's been a huge amount of work - but I want to keep it free so it helps as many people as possible. Your donation makes it worthwhile and is very much appreciated. | InstallerClean 花了我大量心血，我其實應該收費，但我希望它保持免費，好幫助越多人越好。您的贊助讓這一切值得，我非常感激。 |
| A star helps other people find it | 給個星星，能幫助更多人找到它 |
| Minimise | 最小化 |
| Move the unneeded files to the backup folder | 將不需要的檔案移到備份資料夾 |
| Move the unneeded files to a backup folder. You'll choose it next. | 將不需要的檔案移到備份資料夾。接下來您會選擇資料夾。 |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | 將不需要的檔案移到備份資料夾。它與這些檔案在同一個磁碟機上，所以要等您刪除那個資料夾後才會釋放空間。 |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | 永久刪除不需要的檔案。如果您想先確認一切正常，請改用移動。 |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | 來自內嵌 Authenticode 憑證的主體名稱。未驗證憑證鏈。 |
| Change language. The program will restart. | 變更語言。程式會重新啟動。 |

## Screen reader labels

| English | 繁體中文 |
| --- | --- |
| Leave a tip | 打賞 |
| Leave a tip | 打賞 |
| Leave a tip | 打賞 |
| Cancel operation | 取消作業 |
| Cancel scan | 取消掃描 |
| Cancel startup scan | 取消啟動時的掃描 |
| Carry on without it, drive {0} | 不再等候，繼續進行，磁碟機 {0} |
| Carry on without it, {0} | 不再等候，繼續進行，{0} |
| Close | 關閉 |
| Close window | 關閉視窗 |
| Close result and return to main window | 關閉結果並返回主視窗 |
| Leave a star on github | 在 github 上給個星星 |
| Minimise | 最小化 |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | 永久刪除會移除這些不需要的檔案。取消則會關閉，不刪除任何檔案。 |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | 移動會將這些不需要的檔案放到所選的目的地資料夾。取消則讓它們留在原處。 |
| Say thanks | 說聲謝謝 |
| Check for updates | 檢查更新 |
| Checks github's releases page for a newer version. | 在 github 的發行頁面上檢查是否有較新的版本。 |
| Opens the readme on github in your browser. | 在瀏覽器中開啟 github 上的 readme。 |
| Opens the issue tracker on github.com in your browser. | 在瀏覽器中開啟 github.com 上的問題追蹤頁面（Issues）。 |
| If ticked, InstallerClean checks github for a newer version when you run it. | 勾選後，InstallerClean 每次執行時都會在 github 上檢查是否有較新的版本。 |
| Download it from github | 從 github 下載 |
| Download .NET Framework 4.8 from Microsoft | 從 Microsoft 下載 .NET Framework 4.8 |
| Copy the command | 複製命令 |
| Scoop's update command | Scoop 更新命令 |
| Opens the licence file on github.com in your browser. | 在瀏覽器中開啟 github.com 上的授權條款檔案。 |
| Backup folder | 備份資料夾 |
| Patches | 修補程式 |
| Product details | 產品詳細資料 |
| Backup folder | 備份資料夾 |
| Operation progress | 作業進度 |
| Scan {InstallerFolder} again | 重新掃描 {InstallerFolder} |
| Scanning progress | 掃描進度 |
| Startup scan progress | 啟動時的掃描進度 |
| Details, unneeded files | 詳細資料，不需要的檔案 |
| Available for cleanup. | 可供清理。 |
| Details, files left alone | 詳細資料，原樣保留的檔案 |
| Read-only inventory. | 唯讀清單。 |
| Sorted by {0}, ascending | 已依{0}遞增排序 |
| Sorted by {0}, descending | 已依{0}遞減排序 |
| Scan results | 掃描結果 |
| Result details | 結果詳細資料 |
| File details | 檔案詳細資料 |
| Product details | 產品詳細資料 |
| Dialog text | 對話方塊文字 |
| Waiting for a drive or share | 正在等候磁碟機或共用資料夾 |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | 無法處理的檔案 |
| Explains this folder, and how to recover a file, in the README | README 中說明了這個資料夾，以及如何復原檔案 |
| {0} Press Alt+{1} to carry on without it. {2} | {0}按 Alt+{1} 即可不再等候，繼續進行。{2} |
| What the report holds | 報告包含哪些內容 |
| What the report holds | 報告包含哪些內容 |
| Change language | 變更語言 |
| The program will restart. | 程式會重新啟動。 |

## File picker

| English | 繁體中文 |
| --- | --- |
| Choose destination folder for moved files | 為要移動的檔案選擇目的地資料夾 |

## Version

| English | 繁體中文 |
| --- | --- |
| Version {0} | 版本 {0} |

## Word forms (singular and plural)

| English | 繁體中文 |
| --- | --- |
| file | 檔案 |
| files | 檔案 |
| error | 錯誤 |
| errors | 錯誤 |
| package | 封裝 |
| packages | 封裝 |
| product | 產品 |
| products | 產品 |
| patch | 修補程式 |
| patches | 修補程式 |

## Sizes and times

| English | 繁體中文 |
| --- | --- |
| ,  | 、 |
| drive {0} | 磁碟機 {0} |
| .  | 。 |
| {0} TB | {0} TB |
| {0} GB | {0} GB |
| {0} MB | {0} MB |
| {0} KB | {0} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | 不到一秒 |
| {0:F1} seconds | {0:F1} 秒 |

## Command-line tool (installerclean-cli)

| English | 繁體中文 |
| --- | --- |
| Error: unknown argument '{0}' | 錯誤：未知的參數「{0}」 |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | 錯誤：出現未預期的多餘參數「{0}」。如果移動目的地資料夾的路徑中有空格，請用引號括住整個路徑：/m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | 錯誤：出現未預期的多餘參數「{0}」。/s 和 /d 不接受其他參數，而且每次執行只能使用一個選項。 |
| Cancelling... | 正在取消... |
| Cancelled. | 已取消。 |
| Error: unexpected failure ({0}). Details written to {1}. | 錯誤：未預期的失敗（{0}）。詳細資料已寫入 {1}。 |
| Error: unexpected failure ({0}). The crash log could not be written. | 錯誤：未預期的失敗（{0}）。無法寫入當機記錄檔。 |
| Scanning {InstallerFolder}... | 正在掃描 {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | 找到 {0} 個可清理的不需要的{1}（{2}）。 |
| Found no unneeded files. | 沒有找到不需要的檔案。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean 無法確定快取中的哪些檔案屬於這裡安裝的程式，因此沒有提供那 1 個檔案（{2}），而是把它保留了下來。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean 無法確定快取中的哪些檔案屬於這裡安裝的程式，因此沒有提供 {0} 個{1}（{2}），而是把它們保留了下來。 |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean 無法證實 1 個檔案是不需要的，因此沒有提供它（{2}），而是把它保留了下來。 |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean 無法證實 {0} 個{1}是不需要的，因此沒有提供它們（{2}），而是把它們保留了下來。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean 無法確定快取中的哪些檔案屬於這裡安裝的程式，因此沒有提供那 1 個檔案（{2}），而是把它保留了下來。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean 無法確定快取中的哪些檔案屬於這裡安裝的程式，因此沒有提供 {0} 個{1}（{2}），而是把它們保留了下來。 |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean 無法證實 1 個檔案是不需要的，因此沒有提供它（{2}），而是把它保留了下來。 |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean 無法證實 {0} 個{1}是不需要的，因此沒有提供它們（{2}），而是把它們保留了下來。 |
| Why it couldn't be certain: | 無法確定的原因： |
|   A file path in Windows Installer's own records wouldn't resolve, so nothing could be matched to it. |   Windows Installer 自己的記錄中有一個檔案路徑無法解析，因此沒有任何內容能與它對應。 |
|   A file Windows has a record of couldn't be identified, so it couldn't be matched to what's in the folder. |   Windows 有記錄的某個檔案無法辨識，因此無法與資料夾中的內容對應。 |
|   A program may be installed more than once on this PC, and the records can't say which copy a file belongs to. |   某個程式可能在這台電腦上安裝了不只一次，而記錄無法說明某個檔案屬於哪一份。 |
|   Part of Windows Installer's records wouldn't read, so a file could belong to a program recorded there. |   Windows Installer 記錄的一部分無法讀取，因此某個檔案可能屬於記錄在其中的某個程式。 |
|   A file in the folder couldn't be identified, so it couldn't be matched against the records. |   資料夾中的某個檔案無法辨識，因此無法與記錄對應。 |
|   Either a file wouldn't say which program it belongs to, or Windows wouldn't give a clear answer about that program. |   可能是某個檔案沒有說明它屬於哪個程式，也可能是 Windows 對該程式沒有給出明確的答覆。 |
|   A check on which programs the files belong to gave answers that didn't line up with the files it was handed. |   一項檢查這些檔案屬於哪些程式的作業，給出的答覆與交給它的檔案對不上。 |
|   Either a patch file wouldn't say which patch it is, or it wouldn't say which programs it's for, or Windows wouldn't say in full which programs have that patch. |   可能是某個修補程式檔案沒有說明它是哪個修補程式，或沒有說明它適用於哪些程式，也可能是 Windows 沒有完整說明哪些程式套用了該修補程式。 |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows 記錄了 {0} 個不在 {InstallerFolder} 中的檔案：{1}。平常使用不會有問題，但該程式的更新或解除安裝可能會失敗。要把這個檔案放回去，您需要目前所用版本的安裝程式。請向該程式的開發者取得，然後直接執行它，覆蓋安裝現有的程式。較新的版本不行：新版本必須先移除您現有的版本，而需要這個檔案的正是這個步驟。先解除安裝也行不通，原因相同。這應該會復原這個檔案並保留您的設定，但 Microsoft 並不保證。 |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows 記錄了 {0} 個不在 {InstallerFolder} 中的檔案：{1}。平常使用不會有問題，但這些程式的更新或解除安裝可能會失敗。要把某個檔案放回去，您需要該程式目前所用版本的安裝程式。請向該程式的開發者取得，然後直接執行它，覆蓋安裝現有的程式。較新的版本不行：新版本必須先移除您現有的版本，而需要該檔案的正是這個步驟。先解除安裝也行不通，原因相同。這應該會復原該檔案並保留您的設定，但 Microsoft 並不保證。 |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | InstallerClean 無法確定那一個已被取代的檔案已不再需要，因此保留了它。 |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | InstallerClean 無法確定 {0} 個已被取代的檔案已不再需要，因此保留了它們。 |
| InstallerClean stopped waiting for drive {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean 已不再等候磁碟機 {0}，並將仍需與它核對的檔案都原樣保留。等它恢復正常回應後，請再執行一次命令。 |
| InstallerClean stopped waiting for {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean 已不再等候 {0}，並將仍需與它核對的檔案都原樣保留。等它恢復正常回應後，請再執行一次命令。 |
| InstallerClean stopped waiting for more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, run the command again. | InstallerClean 已不再等候多個磁碟機或共用資料夾（{0}），並將仍需與其中任何一個核對的檔案都原樣保留。等它們恢復正常回應後，請再執行一次命令。 |
| Deleting {0} unneeded {1}... | 正在刪除 {0} 個不需要的{1}... |
| Permanently deleted {0} unneeded {1}. | 已永久刪除 {0} 個不需要的{1}。 |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | 錯誤：未指定移動目的地。請使用 /m 路徑。（在圖形介面中設定的預設位置是依使用者儲存的，不適用於排定的工作或服務帳戶下的執行。） |
| Error: destination cannot be inside the Windows Installer folder. | 錯誤：目的地不能位於 Windows Installer 資料夾內。 |
| Error: destination must be a fully qualified path. Got: {0} | 錯誤：目的地必須是完整路徑。收到的是：{0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | 錯誤：目的地 {0} 解析後位於 Windows 系統資料夾之下。請選擇 %SystemRoot%、%ProgramFiles%、%ProgramFiles(x86)% 和 %ProgramData% 以外的路徑。 |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | 錯誤：{0} 的空間不足。移動這些檔案需要 {1}，而可用空間為 {2}。沒有移動任何檔案。 |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | 錯誤：目前有程式正在使用 Windows Installer，例如 Windows Update 或在背景安裝的程式。在這期間 /m 和 /d 會被封鎖。請等它結束後再試一次。 |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | 錯誤：這台電腦上有一筆先前的 Windows Installer 交易已暫停。請先繼續或復原那次安裝（或重新啟動 Windows），再清理 {InstallerFolder}。 |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | 錯誤：有一項排定在重新開機後執行的檔案作業以 {InstallerFolder} 為目標（{0}）。請先重新啟動 Windows 讓該作業完成，再進行清理。 |
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | 錯誤：有一項檔案作業已排定在下次重新啟動時執行，而 InstallerClean 無法得知它涉及哪些檔案，因此無法排除 {InstallerFolder}。請先重新啟動 Windows 再清理。 |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | 錯誤：InstallerClean 在碰 {InstallerFolder} 之前會檢查幾個登錄值，其中一個無法讀取，因此無法排除有 Windows Installer 作業正在執行或已排定在下次重新啟動時執行。/m 和 /d 已被封鎖。請重新啟動 Windows 後再試一次。如果仍然無法讀取，這台電腦就不是 InstallerClean 能清理的。 |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | 錯誤：InstallerClean 無法檢查 Windows Installer 是否忙碌，因此無法排除有安裝正在進行。/m 和 /d 已被封鎖。請重新啟動 Windows 後再試一次。如果檢查仍然失敗，這台電腦就不是 InstallerClean 能清理的。 |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | 錯誤：InstallerClean 無法檢查 Windows Installer 是否忙碌，因為 Windows 傳回了錯誤：{0}（錯誤 {1}），因此無法排除有安裝正在進行。/m 和 /d 已被封鎖。請重新啟動 Windows 後再試一次。如果檢查仍然失敗，這台電腦就不是 InstallerClean 能清理的。 |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | 錯誤：Windows Installer 有作業正在進行，因此 /m 和 /d 已被封鎖。InstallerClean 不會在 {InstallerFolder} 變動時碰它。請等它結束後再試一次。 |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has finished. | 掃描開始時，有程式正在使用 Windows Installer，例如 Windows Update 或正在安裝的程式，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。等它結束後，請再執行一次命令。 |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then run it again. | 掃描開始時，這台電腦上有一筆 Windows Installer 交易已暫停，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請繼續或復原那次安裝，或重新啟動 Windows，然後再執行一次命令。 |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then run it again. | 掃描開始時，Windows 已排定在下次重新啟動時重新命名一個檔案，這會影響 {InstallerFolder}，因此 InstallerClean 沒有提供該資料夾中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows，然後再執行一次命令。 |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then run it again. | 掃描開始時，有一項檔案作業已排定在下次重新啟動時執行，而 InstallerClean 無法得知它涉及哪些檔案，因此沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows，然後再執行一次命令。 |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | 掃描開始時，Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 後再執行一次命令。 |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | 掃描開始時，InstallerClean 無法讀取它會檢查的一項 Windows 設定，因此無法判斷是否有安裝程式作業正在執行或正在等候重新啟動，於是沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 後再執行一次命令。 |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | 掃描開始時，InstallerClean 無法檢查 Windows Installer 是否忙碌，因此沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 後再執行一次命令。 |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | 掃描開始時，InstallerClean 無法檢查 Windows Installer 是否忙碌，因為 Windows 傳回了錯誤：{0}（錯誤 {1}）。因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 後再執行一次命令。 |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has cleared. | 掃描開始時，Windows Installer 有作業正在進行或等候中，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。等它結束後，請再執行一次命令。 |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again. | 掃描期間，這台電腦的時鐘倒退或變慢了，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請再執行一次命令。 |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | 錯誤：Windows Installer 用一個鎖定來防止兩個程式同時變更已安裝的軟體，而 InstallerClean 無法取得這個鎖定，因此無法排除某個檔案在過程中變成需要的可能。沒有刪除任何檔案。請再試一次，如果一直發生，請重新啟動 Windows。 |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | 錯誤：Windows Installer 用一個鎖定來防止兩個程式同時變更已安裝的軟體，而 InstallerClean 無法取得這個鎖定，因此無法排除某個檔案在過程中變成需要的可能。沒有移動任何檔案。請再試一次，如果一直發生，請重新啟動 Windows。 |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | 錯誤：Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此它無法排除某個檔案在過程中變成需要的可能。沒有刪除任何檔案。 |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | 錯誤：Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此它無法排除某個檔案在過程中變成需要的可能。沒有移動任何檔案。 |
| Moving {0} unneeded {1} to {2}... | 正在將 {0} 個不需要的{1}移到 {2}... |
| Moved {0} unneeded {1}. | 已移動 {0} 個不需要的{1}。 |
| Check that your programs still update and uninstall as normal, then delete {0}. | 請確認您的程式仍能照常更新和解除安裝，然後刪除 {0}。 |
| InstallerClean is free. If it helped, a tip is much appreciated: {0} | InstallerClean 是免費的。如果它幫上了忙，歡迎打賞，非常感謝：{0} |
| Update now? It's strongly recommended to use the latest version. [{0}/{1}] | 立即更新？強烈建議使用最新版本。[{0}/{1}] |
| Y | Y |
| N | N |
| No answer after a minute, so InstallerClean updates without asking, as it does when nobody is there to answer. | 一分鐘內沒有回答，因此 InstallerClean 會像無人可回答時一樣，不詢問直接更新。 |
| Send an anonymous report of this run? It is saved at {0}. Type ? to see what it holds. [{1}/{2}/?] | 傳送本次執行的匿名報告？報告已儲存在 {0}。輸入 ? 可查看其內容。[{1}/{2}/?] |
| the chart at {0} | 地址為 {0} 的圖表 |
| The report adds your result to {0}, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | 報告會把您的結果加到{0}，圖表顯示人們用 InstallerClean 釋放了多少空間。報告也包含以下技術細節，幫助我發現程式錯誤： |
| The full report is printed below. | 完整報告列印在下方。 |
| Report sent. Thank you. | 報告已傳送。謝謝。 |
| Report not sent. | 報告未傳送。 |
| No answer after a minute. | 一分鐘內沒有回答。 |
| The report could not be sent. It is kept in {0}. | 無法傳送報告。報告保存在 {0}。 |
| Sending a report left waiting by an earlier run. | 正在傳送較早一次執行留下的待傳送報告。 |
| The new version will scan and list what it finds, without deleting anything. To delete, run /d again afterwards. | 新版本會掃描並列出找到的內容，不會刪除任何檔案。如需刪除，請在之後再次執行 /d。 |
| The new version will scan and list what it finds, without moving anything. To move, run /m again afterwards. | 新版本會掃描並列出找到的內容，不會移動任何檔案。如需移動，請在之後再次執行 /m。 |
| InstallerClean is in use, so it can't update itself now. | InstallerClean 正在使用中，因此現在無法自行更新。 |
| Running {0} with InstallerClean {1} instead. | 改用 InstallerClean {1} 執行 {0}。 |
| The newer version couldn't be run. | 無法執行較新的版本。 |
| Running {0} with InstallerClean {1}. It takes this copy's place once the run ends. | 使用 InstallerClean {1} 執行 {0}。執行結束後，它將取代此副本。 |
| {0}: {1} | {0}：{1} |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again to update. | 此版本需要 .NET Framework 4.8。請從 Microsoft 安裝，重新啟動電腦，然後再次執行 InstallerClean 進行更新。 |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | 要復原很簡單。把它們從 {0} 移回 {InstallerFolder}，一切就會恢復原狀。 |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean 已無法確認備份資料夾，因此沒有再繼續。請檢查 {0}，然後再執行一次命令。 |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | 另一個 InstallerClean 處理程序正持有單一執行個體鎖定（圖形介面或另一次 CLI 執行）。結束代碼 75（暫時性）；稍後可安全地重試。 |
| Stopped after deleting {0} of {1} {2}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | 已刪除 {1} 個{2}中的 {0} 個後停止：InstallerClean 已無法確定沒有其他 InstallerClean 正在執行操作。請再次執行同一命令。結束代碼 2（部分完成）。 |
| Stopped after moving {0} of {1} {2} to {3}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | 已將 {1} 個{2}中的 {0} 個移動到 {3} 後停止：InstallerClean 已無法確定沒有其他 InstallerClean 正在執行操作。請再次執行同一命令。結束代碼 2（部分完成）。 |
| Stopped before deleting any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | 在刪除任何檔案之前已停止：InstallerClean 已無法確定沒有其他 InstallerClean 正在執行操作。請再次執行同一命令。結束代碼 75（暫時性）。 |
| Stopped before moving any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | 在移動任何檔案之前已停止：InstallerClean 已無法確定沒有其他 InstallerClean 正在執行操作。請再次執行同一命令。結束代碼 75（暫時性）。 |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed. Run it again from an administrator prompt. | InstallerClean 需要系統管理員權限才能看到這台電腦上安裝的每個程式，但它目前不是以系統管理員身分執行，所以不會掃描。沒有做任何變更。請從系統管理員命令提示字元重新執行。 |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | 注意：無法寫入事件記錄檔。請檢查應用程式記錄檔的權限或群組原則。 |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - 清理 {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | 移除已沒有任何已安裝程式需要的 .msi 和 .msp 快取檔案。 |
| Needs an elevated (administrator) prompt and won't scan without one. | 需要系統管理員命令提示字元，否則不會掃描。 |
| Usage: | 用法： |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     顯示這份說明（也接受 /?、-h） |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  顯示版本（也接受 -v） |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         僅掃描 - 列出不需要的檔案 |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         永久刪除不需要的檔案 |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         移到已儲存的備份資料夾 |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m 路徑    移到指定的路徑 |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli 會佔住命令提示字元直到結束，<br>因此指令碼或排定的工作可以等候它完成。 |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | 該資料夾依使用者儲存；排定的工作或 SYSTEM 執行需要 /m 路徑。 |
| Before it scans, it checks for a newer version of InstallerClean.<br>When you run it yourself, it asks before updating, and updates if nobody<br>answers within a minute. When it runs on its own, in a scheduled task or<br>a script, it updates without asking.<br>Add /noupdate to skip the check. | 掃描前，它會檢查是否有較新版本的 InstallerClean。由您親自執行時，<br>它會在更新前詢問，一分鐘內無人回答則直接更新。在排定的工作或指令碼中<br>自動執行時，它會直接更新而不詢問。<br>加上 /noupdate 可略過檢查。 |
| Exit codes: | 結束代碼： |
|   0   success: the run did what it was asked |   0   成功：這次執行完成了要求的事 |
|   1   nothing processed: the run failed or was refused |   1   沒有處理任何內容：這次執行失敗或被拒絕 |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   部分：部分已處理，部分未處理（失敗或中途按了 Ctrl+C） |
|   75  transient: a temporary condition blocked the run (see the message) |   75  暫時性：暫時的狀況阻止了這次執行（請見訊息） |
|   130 cancelled (Ctrl+C) |   130 已取消（Ctrl+C） |
