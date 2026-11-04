#!/usr/bin/env node
// Traditional Chinese (zh-Hant) satellite generator for InstallerClean. Built
// on gen-strings-template.mjs like the others; only OUT, ALSO_KEEP and the MAP
// values are this language's. Works FROM THE ENGLISH SOURCE (Strings.resx):
// replaces each key's inner <value>, strips the machine-contract Cli.EventLog*
// keys, keeps the human Cli keys, and self-verifies against the neutral. Output
// is LF, UTF-8.
//
// zh-Hant is the satellite Windows reaches from zh-TW, zh-HK and zh-MO, all of
// which have it as their parent culture. The wording is Taiwan's, the usage
// Microsoft's Taiwan locale writes: 檔案, 資料夾, 磁碟機, 程式, 軟體, 設定.
//
// Chinese plural rule (DisplayHelpers.CategoryFor, case "zh"): PluralCategory
// .Other at every count. Chinese nouns do not inflect for number, so there are
// NO .One/.Few/.Many override keys, and the Plural.* pairs are identical (both
// 檔案 etc). A count takes the measure word 個 before the noun slot, as in
// "{0} 個{1}". The hardcoded .Singular/.Plural sentence pairs are translated on
// both members and come out identical.
//
// Register: 您 throughout, dropped on imperatives, as Windows writes it. Quoted
// names of buttons take 「」. Platform terms are the ones Windows uses in
// Traditional Chinese, Windows Installer's message file (msimsg.dll) included:
// About = 關於, Run as administrator = 以系統管理員身分執行, registry = 登錄,
// patch = 修補程式, package = 封裝, roll back = 復原, restart = 重新啟動,
// removable drive = 抽取式磁碟機, Event Log / Application log / Group Policy =
// 事件記錄檔 / 應用程式記錄檔 / 群組原則.
//
// MAP values are the committed Strings.zh-Hant.resx <value> bytes verbatim: \\
// is one backslash (the paths), \n is a real newline (the multi-line values),
// &#10; is the literal entity where the neutral uses it, {0}/{1} are .NET
// placeholders left verbatim.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.zh-Hant.resx`;

// Universal keeps: keys whose value is the same in every language, the brand names
// and the pure-placeholder announcement string. Their still-English value is NOT a
// miss. Explicit by KEY on purpose: a future brand key then defaults to "flag until
// someone adds it here", never silently passes. Do NOT translate these values. Do
// NOT edit this list per language.
//
// The five size suffixes and the two elapsed suffixes do not belong in this list,
// because they are not universal: French writes To/Go/Mo/Ko/o, Russian and Ukrainian
// write ТБ/ГБ/МБ/КБ/Б and мс/с. Those three carry real
// values in their MAP; the languages that do abbreviate as English does keep them in
// ALSO_KEEP, which is the per-language list. Display.ListSeparator is the same shape,
// for the same reason.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
]);

// Per-language keeps. Traditional Chinese translates every word, so the only
// values left as the English are the unit suffixes below.
const ALSO_KEEP = [
  // The size and elapsed unit suffixes. Traditional Chinese abbreviates them exactly as
  // English does, so there is nothing to translate and nothing to get wrong.
  // A per-language keep rather than a universal one because fr, ru and uk do
  // NOT: French takes To/Go/Mo/Ko/o, Russian and Ukrainian take ТБ/ГБ/МБ/КБ/Б and
  // мс/с, and all three carry real values in their MAP.
  'Display.Size.TB',           // {0} TB
  'Display.Size.GB',           // {0} GB
  'Display.Size.MB',           // {0} MB
  'Display.Size.KB',           // {0} KB
  'Display.Size.B',            // {0} B
  'Display.Elapsed.Ms',        // {0:F0}ms
  'Display.Elapsed.S',         // {0:F1}s
  // The command line's answer letters stay Y and N, as a console in this language
  // conventionally takes them.
  'Cli.UpdateAnswerYes',
  'Cli.UpdateAnswerNo',
];

const MAP = {
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `關於`,
  'Window.Registered.Title': `原樣保留的檔案`,
  'Window.Orphaned.Title': `不需要的檔案，可安全刪除`,
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `修補程式`,
  'Section.Registered.Details': `產品詳細資料`,
  'Section.Backup.Folder': `備份資料夾`,
  'Section.SayThanks': `說聲謝謝`,
  'Field.Reason': `原因`,
  'Field.Author': `作者`,
  'Field.Application': `應用程式`,
  'Field.Title': `標題`,
  'Field.Subject': `主旨`,
  'Field.Keywords': `關鍵字`,
  'Field.SigningCertificate': `簽章憑證`,
  'Field.FileSize': `檔案大小`,
  'Field.Comment': `註解`,
  'Field.ProductName': `產品名稱`,
  'Field.File': `檔案`,
  'Field.Size': `大小`,
  'Field.Patches': `修補程式`,
  'Field.UnknownProductName': `（不明）`,
  'Field.PatchesOnly': `（僅修補程式）`,
  'Field.Missing': `遺失`,
  'Action.About': `關於(_A)`,
  'Action.Copy': `複製`,
  'Action.Cut': `剪下`,
  'Action.Paste': `貼上`,
  'Action.SelectAll': `全選`,
  'Action.Browse': `瀏覽(_B)...`,
  'Action.Cancel': `取消(_C)`,
  'Action.CheckForUpdates': `檢查更新(_U)`,
  'Action.Close': `關閉(_C)`,
  'Action.DeletePermanently': `永久刪除(_D)`,
  'Action.Done': `完成(_D)`,
  'Action.Details': `詳細資料`,
  'Action.LeaveATip': `打賞(_L)`,
  'Action.Donate': `打賞(_L)`,
  'Action.DonateSmall': `打賞`,
  'Action.LeaveStarOnGitHub': `在 GitHub 上給個星星(_S)`,
  'Action.Licence': `Apache 2.0 授權條款`,
  'Action.Move': `移動(_M)`,
  'Action.BackupFolderPlaceholder': `若選擇移動而非刪除，請在此填寫資料夾路徑。`,
  'Action.Rescan': `重新掃描(_R)`,
  'Automation.LeaveATip.About': `打賞`,
  'Automation.Donate': `打賞`,
  'Automation.DonateSmall': `打賞`,
  'Automation.CancelOperation': `取消作業`,
  'Automation.CancelScan': `取消掃描`,
  'Automation.CancelStartupScan': `取消啟動時的掃描`,
  'Automation.Close': `關閉`,
  'Automation.CloseWindow': `關閉視窗`,
  'Automation.CloseResult': `關閉結果並返回主視窗`,
  'Automation.LeaveStarOnGitHub.About': `在 github 上給個星星`,
  'Automation.Minimise': `最小化`,
  'Automation.ConfirmDelete': `永久刪除會移除這些不需要的檔案。取消則會關閉，不刪除任何檔案。`,
  'Automation.ConfirmMove': `移動會將這些不需要的檔案放到所選的目的地資料夾。取消則讓它們留在原處。`,
  'Automation.SayThanks': `說聲謝謝`,
  'Automation.CheckForUpdates': `檢查更新`,
  'Automation.CheckForUpdates.HelpText': `在 github 的發行頁面上檢查是否有較新的版本。`,
  'Automation.Licence.HelpText': `在瀏覽器中開啟 github.com 上的授權條款檔案。`,
  'Automation.Section.BackupFolder': `備份資料夾`,
  'Automation.Section.Patches': `修補程式`,
  'Automation.Section.ProductDetails': `產品詳細資料`,
  'Automation.BackupFolder': `備份資料夾`,
  'Automation.OperationProgress': `作業進度`,
  'Automation.RescanInstaller': `重新掃描 {InstallerFolder}`,
  'Automation.ScanningProgress': `掃描進度`,
  'Automation.StartupScanProgress': `啟動時的掃描進度`,
  'Automation.ViewOrphanedFiles': `詳細資料，不需要的檔案`,
  'Automation.ViewOrphanedFiles.HelpText': `可供清理。`,
  'Automation.ViewRegisteredFiles': `詳細資料，原樣保留的檔案`,
  'Automation.ViewRegisteredFiles.HelpText': `唯讀清單。`,
  'Automation.SortStatus.Ascending': `已依{0}遞增排序`,
  'Automation.SortStatus.Descending': `已依{0}遞減排序`,
  'Automation.Scroll.ScanResults': `掃描結果`,
  'Automation.Scroll.ResultDetails': `結果詳細資料`,
  'Automation.Scroll.FileDetails': `檔案詳細資料`,
  'Automation.Scroll.DialogBody': `對話方塊文字`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `無法處理的檔案`,
  'Automation.RegisteredMissingSeeAlso': `README 中說明了這個資料夾，以及如何復原檔案`,
  'Tooltip.LeaveATip.About': `任何打賞都非常感謝`,
  'Tooltip.CancellingPending': `已要求取消。InstallerClean 正在等候目前的步驟到達可以停下來的地方。在大量 I/O 或 MSI 資料庫呼叫期間，這可能需要幾秒鐘。`,
  'Tooltip.Close': `關閉`,
  'Tooltip.LeaveStarOnGitHub.About': `給個星星，能幫助更多人找到它`,
  'Tooltip.Minimise': `最小化`,
  'Tooltip.Move': `將不需要的檔案移到備份資料夾`,
  'Tooltip.MoveNeedsDestination': `將不需要的檔案移到備份資料夾。接下來您會選擇資料夾。`,
  'Tooltip.Delete': `永久刪除不需要的檔案。如果您想先確認一切正常，請改用移動。`,
  'Tooltip.SigningCertificate': `來自內嵌 Authenticode 憑證的主體名稱。未驗證憑證鏈。`,
  'Body.MainExplanation.Lead': `下列不需要的檔案都可以安全刪除。`,
  'Body.MainExplanation.Why': `它們位於 {InstallerFolder}。InstallerClean 會就每個已安裝的程式詢問 Windows：沒有任何程式認領的檔案會列出（{0}），已被較新的修補程式取代、且沒有任何程式能復原到它的檔案也會列出（{1}）。`,
  'Body.MainExplanation.Action': `將它們移到您選擇的備份資料夾，等您確定程式仍能照常更新和解除安裝後，再刪除那個資料夾。把它們放回 {InstallerFolder} 就能恢復原狀。或者現在就永久刪除。`,
  'Body.PendingReboot.MsiExecuteMutex': `目前有程式正在使用 Windows Installer，例如 Windows Update 或在背景安裝的程式。在這期間移動和刪除會暫停，InstallerClean 不會在 {InstallerFolder} 變動時碰它。等它結束後重新掃描，移動和刪除就會恢復。`,
  'Body.PendingReboot.MsiExecuteMutexAccessRefused': `Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此移動和刪除已暫停，InstallerClean 不會碰 {InstallerFolder}。請重新啟動 Windows 並重新掃描。如果 Windows 仍然拒絕，這台電腦就不是 InstallerClean 能清理的。`,
  'Body.PendingReboot.MsiExecuteMutexUnreadable': `InstallerClean 無法檢查 Windows Installer 是否忙碌，因此移動和刪除已暫停，InstallerClean 不會碰 {InstallerFolder}。請重新啟動 Windows 並重新掃描。如果仍然無法檢查，這台電腦就不是 InstallerClean 能清理的。`,
  'Body.PendingReboot.MsiExecuteMutexUnreadableWindowsError': `InstallerClean 無法檢查 Windows Installer 是否忙碌，因為 Windows 傳回了錯誤：{0}（錯誤 {1}）。移動和刪除已暫停，InstallerClean 不會碰 {InstallerFolder}。請重新啟動 Windows 並重新掃描。如果仍然無法檢查，這台電腦就不是 InstallerClean 能清理的。`,
  'Body.PendingReboot.InstallerInProgress': `這台電腦上有一筆先前的 Windows Installer 交易已暫停。請先繼續或復原那次安裝（或重新啟動 Windows），再清理 {InstallerFolder}。`,
  'Body.PendingReboot.PendingRenameInCache': `Windows 已排定在下次重新啟動時重新命名一個檔案，這會影響 {InstallerFolder}。請先重新啟動 Windows 再清理。`,
  'Body.NoFileSelected': `選取一個檔案以檢視詳細資料。`,
  'Body.NoProductSelected': `選取一個產品以檢視詳細資料。`,
  'Body.NoMetadata': `沒有可用的中繼資料。`,
  'Body.RegisteredMissingFromDisk': `這個安裝程式檔案不見了。它現在不會造成任何問題，要等到有一天您嘗試更新或解除安裝它所屬的程式時才會。到時這個步驟可能會失敗，因為 Windows 會尋找這個檔案，而它不在那裡。\n\n要把它放回去，您需要目前所用版本的安裝程式。請向該程式的開發者取得，然後直接執行它，覆蓋安裝現有的程式。較新的版本不行：新版本必須先移除您現有的版本，而需要這個檔案的正是這個步驟。先解除安裝也行不通，原因相同。這應該會復原這個檔案並保留您的設定，但 Microsoft 並不保證。`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `README 以 Microsoft 自己的原話[說明了這個資料夾]，以及如何復原檔案。`,
  'Body.NoPatches': `（無）`,
  'Reason.Orphaned': `孤立`,
  'Reason.Superseded': `已被取代`,
  'Reason.Obsoleted': `已廢棄`,
  'Status.Scanning': `正在掃描...`,
  'Status.Cancelling': `正在取消...`,
  'Status.StartingScan': `正在開始掃描...`,
  'Status.QueryingApi': `正在向 Windows 查詢已安裝的軟體...`,
  'Status.ScanningCache': `正在掃描安裝程式快取資料夾...`,
  'Status.CheckingRegistry': `正在檢查登錄中的其他封裝...`,
  'Status.RegisteredPackagesFound': `正在比對檔案與已安裝的程式...`,
  'Status.CheckingRemaining': `正在檢查其餘檔案...`,
  'Status.ScanComplete': `掃描完成（{0}）`,
  'Status.FoundUnused': `找到 {0} 個可安全刪除的{1}。`,
  'Status.PreparingDestination': `正在準備目的地資料夾...`,
  'Status.Moving': `正在移動不需要的檔案...`,
  'Status.Deleting': `正在刪除不需要的檔案...`,
  'Status.MoveCancelled.Partial': `移動已取消。已處理 {1} 個{2}中的 {0} 個。`,
  'Status.DeleteCancelled.Partial': `刪除已取消。已處理 {1} 個{2}中的 {0} 個。`,
  'Status.MoveFailed': `{0}。詳細資料請見 {1}。`,
  'Status.MoveFailed.NoLog': `{0}。無法寫入當機記錄檔。`,
  'Status.DeleteFailed': `{0}。詳細資料請見 {1}。`,
  'Status.DeleteFailed.NoLog': `{0}。無法寫入當機記錄檔。`,
  'Status.ScanAccessDenied': `存取被拒。Windows 拒絕了這次掃描。`,
  'Status.ScanStopped': `掃描已停止。`,
  'Status.ScanCancelled': `掃描已取消。`,
  'Status.Ready': `就緒`,
  'Status.ScanFailedDetails': `掃描失敗（{0}）。詳細資料請見 {1}。`,
  'Status.ScanFailedDetails.NoLog': `掃描失敗（{0}）。無法寫入當機記錄檔。`,
  'Completion.AllClean': `乾乾淨淨`,
  'Completion.NothingToCleanUp': `{InstallerFolder} 中沒有需要清理的內容`,
  'Completion.NothingToCleanUpReceipt': `掃描了 {0} 個{1}，費時：{2}`,
  'Completion.Freed': `已釋放 {0}`,
  'Completion.Moved': `已移動 {0}`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `沒有移動任何檔案`,
  'Completion.NothingDeleted': `沒有刪除任何檔案`,
  'Completion.FailedCount.Singular': `{0} 個檔案無法移動。`,
  'Completion.FailedCount.Plural': `{0} 個檔案無法移動。`,
  'Completion.FailedCountDelete.Singular': `{0} 個檔案無法刪除。`,
  'Completion.FailedCountDelete.Plural': `{0} 個檔案無法刪除。`,
  'Completion.MoveSummary.Singular': `已將 {0} 個{1}移到：{2}`,
  'Completion.MoveSummary.Plural': `已將 {0} 個{1}移到：{2}`,
  'Completion.PermanentDeleteSummary.Singular': `已永久刪除 {0} 個{1}`,
  'Completion.PermanentDeleteSummary.Plural': `已永久刪除 {0} 個{1}`,
  'Summary.RegisteredStillUsed.Singular': `{0} 個檔案原樣保留`,
  'Summary.RegisteredStillUsed.Plural': `{0} 個檔案原樣保留`,
  'Summary.OrphanedToCleanUp.Singular': `{0} 個不需要的檔案可清理`,
  'Summary.OrphanedToCleanUp.Plural': `{0} 個不需要的檔案可清理`,
  'Summary.MissingFromDisk.Singular': `Windows 記錄了 {0} 個不在 {InstallerFolder} 中的檔案：{1}。平常使用不會有問題，但該程式的更新或解除安裝可能會失敗。請開啟「詳細資料」了解該怎麼做。`,
  'Summary.MissingFromDisk.Plural': `Windows 記錄了 {0} 個不在 {InstallerFolder} 中的檔案：{1}。平常使用不會有問題，但這些程式的更新或解除安裝可能會失敗。請開啟「詳細資料」了解該怎麼做。`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `另外 {0} 個程式`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `另外 {0} 個程式`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} 個在記錄中沒有標明程式的檔案`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} 個在記錄中沒有標明程式的檔案`,
  'Summary.OperationFiles': `{1} 個{2}中的 {0} 個`,
  'Summary.OrphanedWindow': `{0} 個不需要的{1}（{2}）`,
  'Summary.RegisteredWindow.Singular': `{0} 個檔案原樣保留（{1}）`,
  'Summary.RegisteredWindow.Plural': `{0} 個檔案原樣保留（{1}）`,
  'Confirm.MoveTitle': `要移動 {0} 個{1}（{2}）嗎？`,
  'Confirm.DeleteTitle': `要刪除 {0} 個{1}（{2}）嗎？`,
  'Error.AdminRequiredTitle': `存取被拒`,
  'Error.AdminRequiredBody': `Windows 拒絕讓 InstallerClean 存取，因此它已停止。沒有移除任何內容。\n\nInstallerClean 本來就是以系統管理員身分執行，所以再用這種方式啟動一次也沒有幫助。Windows 沒有進一步說明是什麼拒絕了存取，因此沒有具體可以嘗試的辦法。`,
  'Error.StoppedTitle': `InstallerClean 已停止`,
  'Error.ScanFailedTitle': `掃描失敗`,
  'Error.InstallerDbEmpty': `Windows Installer 的記錄傳回的內容完全是空的：沒有任何一個已安裝的程式或更新認領快取的安裝程式檔案。正常運作的電腦不會出現這種情況（就連剛安裝好的 Windows 也會有一些），所以要不是記錄已損毀，就是無法讀取；而相信這個結果的掃描，會把 {InstallerFolder} 中的每個檔案都誤判為孤立。InstallerClean 因此停了下來。沒有移除任何內容。`,
  'Error.MsiAccessDenied': `Windows Installer 不讓 InstallerClean 列出已安裝的內容。InstallerClean 本來就是以系統管理員身分執行，所以再以系統管理員身分執行一次也不會有任何改變。沒有這份清單，就無法安全地判斷哪些快取檔案仍然需要，因此 InstallerClean 停了下來。沒有移除任何內容。`,
  'Error.MsiNonSuccess': `Windows Installer 無法提供 InstallerClean 一份可讀取的已安裝程式清單：它讀取了 {1} 個{2}，接著有一個項目傳回時無法讀取（錯誤碼 {0}）。InstallerClean 沒有依據只讀到一部分的清單繼續，而是停了下來。沒有移除任何內容。`,
  'Error.InvalidDestinationTitle': `目的地無效`,
  'Error.DestinationWriteFailedTitle': `無法使用這個備份資料夾`,
  'Error.MoveFailedTitle': `移動失敗`,
  'Error.DeleteFailedTitle': `刪除失敗`,
  'Error.SettingNotSavedTitle': `設定未儲存`,
  'Error.SettingNotSavedBody': `無法儲存這項變更。InstallerClean 下次啟動時會回到先前的設定。`,
  'Error.DestinationInsideInstaller': `目的地不能位於 Windows Installer 資料夾內。`,
  'Error.DestinationInSystemFolder': `目的地 {0} 解析後位於 Windows 系統資料夾之下。請選擇 %SystemRoot%、%ProgramFiles%、%ProgramFiles(x86)% 和 %ProgramData% 以外的路徑。`,
  'Error.NotEnoughSpaceTitle': `空間不足`,
  'Error.NotEnoughSpaceBody': `{0} 的空間不夠\n\n需要：{1}\n可用：{2}`,
  'Error.AccessDeniedDestination': `您沒有寫入 {0} 的權限。\n請改用您的使用者設定檔中的資料夾，或您擁有的磁碟機。`,
  'Error.PathTooLong': `路徑 {0} 對 Windows 來說太長了。請選擇較短的路徑。`,
  'Error.DestinationMissing': `資料夾 {0} 不存在，也無法建立。請檢查磁碟機代號或網路路徑。`,
  'Error.IOWriteDestination': `Windows 無法寫入 {0}。\n詳細資料請見 {1}。`,
  'Error.IOWriteDestination.NoLog': `Windows 無法寫入 {0}。無法寫入當機記錄檔。`,
  'Error.WriteDestination': `無法寫入 {0}。\n詳細資料請見 {1}。`,
  'Error.WriteDestination.NoLog': `無法寫入 {0}。無法寫入當機記錄檔。`,
  'Error.MissingSourceFile': `檔案已不存在。`,
  'Error.SourceIsReparsePoint': `來源檔案是符號連結或連接點；基於安全考量已拒絕。`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows 拒絕存取這個檔案；檔案已留在原處。`,
  'Error.AccessDenied.Plural': `Windows 拒絕存取這些檔案；檔案已留在原處。`,
  'Error.FileInUse.Singular': `這個檔案正被另一個程式開啟或鎖定，所以目前無法移除。它已留在原處；請稍後再試。`,
  'Error.FileInUse.Plural': `這些檔案正被另一個程式開啟或鎖定，所以目前無法移除。它們已留在原處；請稍後再試。`,
  'Error.IOFailure.Singular': `Windows 回報了一個檔案錯誤；檔案已留在原處。`,
  'Error.IOFailure.Plural': `Windows 回報了檔案錯誤；這些檔案已留在原處。`,
  'Error.UnknownError.Singular': `這個檔案出了點問題；它已留在原處。`,
  'Error.UnknownError.Plural': `這些檔案出了點問題；它們已留在原處。`,
  'Error.MoveIntoInstaller': `拒絕將檔案移到 Windows Installer 資料夾（目的地：{0}）。`,
  'Error.DestinationNotFullyQualified': `備份資料夾必須是指向某個資料夾的完整路徑，以磁碟機代號或網路共用資料夾開頭（例如 D:\\Backup 或 \\\\server\\backup）。InstallerClean 無法使用這個路徑：{0}`,
  'BrowserLaunch.FailedTitle': `無法開啟您的瀏覽器`,
  'UpdateCheck.Title': `檢查更新`,
  'UpdateCheck.Status.Checking': `正在檢查...`,
  'UpdateCheck.Status.UpToDate': `已是最新版本。`,
  'UpdateCheck.Failed.NetworkUnavailable': `無法連線到 GitHub。請檢查您的網際網路連線，然後再試一次。`,
  'UpdateCheck.Failed.ServerError': `GitHub 傳回了錯誤回應。請過幾分鐘再試一次。`,
  'UpdateCheck.Failed.ResponseParseError': `GitHub 的回應中沒有可辨識的發行版本。請稍後再試，或直接開啟發行頁面。`,
  'UpdateCheck.Failed.Timeout': `檢查逾時。您與 GitHub 的連線可能較慢；請再試一次。`,
  'UpdateCheck.Failed.Unknown': `檢查因不明原因失敗。如果您需要回報這個問題，詳細資料在 {0}。`,
  'BrowserLaunch.ClipboardOk': `連結已在您的剪貼簿中，您可以自己貼上：&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean 也無法把連結複製到剪貼簿，連結在這裡：&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `InstallerClean 已無法確認備份資料夾，因此沒有再繼續。請檢查 {0}，然後重新掃描並再試一次。`,
  'Error.CannotWriteFolder': `無法寫入 {0}。`,
  'Error.DestinationCollision': `備份資料夾中已經有一個名為「{0}」的檔案。`,
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `它已經在執行了。`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `發生未預期的錯誤，InstallerClean 必須關閉。\n\n{0}\n\n詳細資料已寫入：\n{1}`,
  'Startup.UnhandledBody.NoLog': `發生未預期的錯誤，InstallerClean 必須關閉。\n\n{0}\n\n無法寫入當機記錄檔。`,
  'Startup.ErrorTitle': `啟動錯誤`,
  'Startup.FailedToStart': `無法啟動（{0}）。詳細資料已寫入：\n{1}`,
  'Startup.FailedToStart.NoLog': `無法啟動（{0}）。無法寫入當機記錄檔。`,
  'FilePicker.ChooseDestinationTitle': `為要移動的檔案選擇目的地資料夾`,
  'Version.Display': `版本 {0}`,
  'Plural.File.Singular': `檔案`,
  'Plural.File.Plural': `檔案`,
  'Plural.Error.Singular': `錯誤`,
  'Plural.Error.Plural': `錯誤`,
  'Plural.Package.Singular': `封裝`,
  'Plural.Package.Plural': `封裝`,
  'Plural.Product.Singular': `產品`,
  'Plural.Product.Plural': `產品`,
  'Plural.Patch.Singular': `修補程式`,
  'Plural.Patch.Plural': `修補程式`,
  'Display.Size.TB': `{0} TB`,
  'Display.Size.GB': `{0} GB`,
  'Display.Size.MB': `{0} MB`,
  'Display.Size.KB': `{0} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ListSeparator': `、`,
  'Display.SentenceSeparator': `。`,
  'Display.ElapsedLong.LessThanASecond': `不到一秒`,
  'Display.ElapsedLong.Seconds': `{0:F1} 秒`,
  'Cli.UnknownArgument': `錯誤：未知的參數「{0}」`,
  'Cli.Cancelling': `正在取消...`,
  'Cli.Cancelled': `已取消。`,
  'Cli.GenericError': `錯誤：未預期的失敗（{0}）。詳細資料已寫入 {1}。`,
  'Cli.GenericError.NoLog': `錯誤：未預期的失敗（{0}）。無法寫入當機記錄檔。`,
  'Cli.ScanningInstaller': `正在掃描 {InstallerFolder}...`,
  'Cli.FoundOrphans': `找到 {0} 個可清理的不需要的{1}（{2}）。`,
  'Cli.DeletingFiles': `正在刪除 {0} 個不需要的{1}...`,
  'Cli.DeletedFiles': `已永久刪除 {0} 個不需要的{1}。`,
  'Cli.NoMoveDestination': `錯誤：未指定移動目的地。請使用 /m 路徑。（在圖形介面中設定的預設位置是依使用者儲存的，不適用於排定的工作或服務帳戶下的執行。）`,
  'Cli.MoveDestinationInsideInstaller': `錯誤：目的地不能位於 Windows Installer 資料夾內。`,
  'Cli.MoveDestinationRelative': `錯誤：目的地必須是完整路徑。收到的是：{0}`,
  'Cli.MoveDestinationInSystemFolder': `錯誤：目的地 {0} 解析後位於 Windows 系統資料夾之下。請選擇 %SystemRoot%、%ProgramFiles%、%ProgramFiles(x86)% 和 %ProgramData% 以外的路徑。`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `錯誤：目前有程式正在使用 Windows Installer，例如 Windows Update 或在背景安裝的程式。在這期間 /m 和 /d 會被封鎖。請等它結束後再試一次。`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `錯誤：這台電腦上有一筆先前的 Windows Installer 交易已暫停。請先繼續或復原那次安裝（或重新啟動 Windows），再清理 {InstallerFolder}。`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `錯誤：有一項排定在重新開機後執行的檔案作業以 {InstallerFolder} 為目標（{0}）。請先重新啟動 Windows 讓該作業完成，再進行清理。`,
  'Cli.MovingFiles': `正在將 {0} 個不需要的{1}移到 {2}...`,
  'Cli.MovedFiles': `已移動 {0} 個不需要的{1}。`,
  'Cli.MutexBlocked': `另一個 InstallerClean 處理程序正持有單一執行個體鎖定（圖形介面或另一次 CLI 執行）。結束代碼 75（暫時性）；稍後可安全地重試。`,
  'Cli.EventLogUnavailable': `注意：無法寫入事件記錄檔。請檢查應用程式記錄檔的權限或群組原則。`,
  'CrashLog.PrivacyHeader': `# crash.log 記錄 InstallerClean 未處理的例外狀況。\n# 在提高權限執行時，架構的例外狀況訊息可能包含目前工作階段中的\n# 檔案路徑（包括 Windows Installer 查詢所列舉的其他使用者的設定檔）。\n# 更新檢查或結果記錄上傳的網路失敗訊息，可能包含目的地 URL 以及\n# 解析出的 IP 或 Proxy 位址。關於無法讀取的 Windows Installer 記錄\n# 的項目，可能包含 Windows 帳戶 SID（S-1-5-21-...）以及已安裝軟體的\n# 產品代碼。\n# 將這個檔案附加到公開的錯誤報告之前，請先刪除這三類資訊。\n`,
  'Cli.Help.Header': `InstallerClean - 清理 {InstallerFolder}`,
  'Cli.Help.Usage': `用法：`,
  'Cli.Help.Help': `  installerclean-cli --help     顯示這份說明（也接受 /?、-h）`,
  'Cli.Help.Version': `  installerclean-cli --version  顯示版本（也接受 -v）`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         僅掃描 - 列出不需要的檔案`,
  'Cli.Help.Delete': `  installerclean-cli /d         永久刪除不需要的檔案`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         移到已儲存的備份資料夾`,
  'Cli.Help.MovePath': `  installerclean-cli /m 路徑    移到指定的路徑`,
  'Cli.Help.NoteLine1': `installerclean-cli 會佔住命令提示字元直到結束，&#10;因此指令碼或排定的工作可以等候它完成。`,
  'Cli.Help.ExitCodesHeader': `結束代碼：`,
  'Cli.Help.ExitCodeOk': `  0   成功：這次執行完成了要求的事`,
  'Cli.Help.ExitCodeError': `  1   沒有處理任何內容：這次執行失敗或被拒絕`,
  'Cli.Help.ExitCodePartial': `  2   部分：部分已處理，部分未處理（失敗或中途按了 Ctrl+C）`,
  'Cli.Help.ExitCodeTransient': `  75  暫時性：暫時的狀況阻止了這次執行（請見訊息）`,
  'Cli.Help.ExitCodeCancelled': `  130 已取消（Ctrl+C）`,
  'Tooltip.ChangeLanguage': `變更語言。程式會重新啟動。`,
  'Automation.ChangeLanguage': `變更語言`,
  'Automation.ChangeLanguage.HelpText': `程式會重新啟動。`,
  'Body.NotScanned.Lead': `尚未掃描。`,
  'Body.NotScanned.Why': `按下「重新掃描」，在 {InstallerFolder} 中尋找已沒有任何程式需要的安裝程式檔案。`,
  'Confirm.MoveSameDrive': `那個資料夾在同一個磁碟機上，所以要等您刪除它，空間才會回來。如果想立刻拿回空間，請改選另一個磁碟機上的資料夾。`,
  'Error.ScanCorrelationFailed': `InstallerClean 無法將 Windows Installer 的記錄與 {InstallerFolder} 中的內容對應起來。記錄所指向的內容幾乎都不在那裡，而那裡的內容也幾乎都沒有任何記錄提到，因此無法證明任何檔案是不需要的。沒有提供任何檔案，也沒有移除任何內容。`,
  'Error.CandidateOutsideCache': `這個檔案並非直接位於 Windows Installer 資料夾內；基於安全考量已拒絕。`,
  'Completion.MoveCancelledSummary': `在您取消前，已將 {1} 個{2}中的 {0} 個移到 {3}。`,
  'Completion.PermanentDeleteCancelledSummary': `在您取消前，已永久刪除 {1} 個{2}中的 {0} 個。`,
  'Body.PendingReboot.Lead': `這些檔案目前無法清理。`,
  'Cli.TooManyArguments': `錯誤：出現未預期的多餘參數「{0}」。如果移動目的地資料夾的路徑中有空格，請用引號括住整個路徑：/m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `該資料夾依使用者儲存；排定的工作或 SYSTEM 執行需要 /m 路徑。`,
  'Error.ScanRecordsUnreadable': `InstallerClean 能讀取的 Windows Installer 記錄不夠多，無法確定哪些內容仍然需要：已安裝程式的清單傳回時並不完整，而直接從登錄讀取同樣的記錄也發生了錯誤。一個檔案可能只因為提到它的那筆記錄剛好讀取不到，看起來就像是孤立的，因此 InstallerClean 停了下來。沒有移除任何內容。`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer 始終沒有發出已安裝程式清單結束的訊號：InstallerClean 讀取了 {2} 個{3}，然後在 {0} 個項目後放棄（最後的錯誤碼為 {1}）。沒有結尾的清單無法信任，因此 InstallerClean 停了下來。沒有移除任何內容。`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer 始終沒有發出某個程式的修補程式清單結束的訊號：InstallerClean 讀取了 {2} 個{3}，然後在 {0} 個項目後放棄（最後的錯誤碼為 {1}）。沒有結尾的清單無法信任，因此 InstallerClean 停了下來。沒有移除任何內容。`,
  'Completion.DonateAsk': `任何打賞都非常感謝`,
  'About.Link.Guide': `指南與常見問題`,
  'About.Link.ReportProblem': `回報問題`,
  'About.AutoUpdateCheck': `自動檢查更新`,
  'Automation.About.Guide.HelpText': `在瀏覽器中開啟 github 上的 readme。`,
  'Automation.About.ReportProblem.HelpText': `在瀏覽器中開啟 github.com 上的問題追蹤頁面（Issues）。`,
  'Automation.AutoUpdateCheck.HelpText': `勾選後，InstallerClean 每次執行時都會在 github 上檢查是否有較新的版本。`,
  'Tooltip.MoveSameDrive': `將不需要的檔案移到備份資料夾。它與這些檔案在同一個磁碟機上，所以要等您刪除那個資料夾後才會釋放空間。`,
  'Confirm.DeletePermanently.Singular': `這個檔案將被永久刪除。這麼做是安全的，但如果您想留個備份，請改用移動。`,
  'Confirm.DeletePermanently.Plural': `這些檔案將被永久刪除。這麼做是安全的，但如果您想留個備份，請改用移動。`,
  'Error.ScanCacheRootUnresolved': `InstallerClean 無法讓 Windows 解析出 {InstallerFolder} 的真實路徑，因此無法證明任何檔案位於其中，也沒有提供任何檔案供清理。這次掃描什麼都沒找到，是因為那項檢查失敗，而不是因為資料夾很乾淨。沒有移除任何內容。`,
  'Automation.Scroll.ProductDetails': `產品詳細資料`,
  'Body.PendingReboot.Other': `Windows Installer 有作業正在進行，因此移動和刪除已暫停。InstallerClean 不會在 {InstallerFolder} 變動時碰它。等作業結束後重新掃描，移動和刪除就會恢復。`,
  'Cli.TooManyArgumentsNoPath': `錯誤：出現未預期的多餘參數「{0}」。/s 和 /d 不接受其他參數，而且每次執行只能使用一個選項。`,
  'Cli.MissingFromDisk.Singular': `Windows 記錄了 {0} 個不在 {InstallerFolder} 中的檔案：{1}。平常使用不會有問題，但該程式的更新或解除安裝可能會失敗。要把這個檔案放回去，您需要目前所用版本的安裝程式。請向該程式的開發者取得，然後直接執行它，覆蓋安裝現有的程式。較新的版本不行：新版本必須先移除您現有的版本，而需要這個檔案的正是這個步驟。先解除安裝也行不通，原因相同。這應該會復原這個檔案並保留您的設定，但 Microsoft 並不保證。`,
  'Cli.MissingFromDisk.Plural': `Windows 記錄了 {0} 個不在 {InstallerFolder} 中的檔案：{1}。平常使用不會有問題，但這些程式的更新或解除安裝可能會失敗。要把某個檔案放回去，您需要該程式目前所用版本的安裝程式。請向該程式的開發者取得，然後直接執行它，覆蓋安裝現有的程式。較新的版本不行：新版本必須先移除您現有的版本，而需要該檔案的正是這個步驟。先解除安裝也行不通，原因相同。這應該會復原該檔案並保留您的設定，但 Microsoft 並不保證。`,
  'Cli.MoveNotEnoughSpace': `錯誤：{0} 的空間不足。移動這些檔案需要 {1}，而可用空間為 {2}。沒有移動任何檔案。`,
  'Cli.PendingRebootBlocked.Other': `錯誤：Windows Installer 有作業正在進行，因此 /m 和 /d 已被封鎖。InstallerClean 不會在 {InstallerFolder} 變動時碰它。請等它結束後再試一次。`,
  'Cli.ScanSignal.Busy': `掃描開始時，有程式正在使用 Windows Installer，例如 Windows Update 或正在安裝的程式，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。等它結束後，請再執行一次命令。`,
  'Cli.ScanSignal.Suspended': `掃描開始時，這台電腦上有一筆 Windows Installer 交易已暫停，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請繼續或復原那次安裝，或重新啟動 Windows，然後再執行一次命令。`,
  'Cli.ScanSignal.RenameInCache': `掃描開始時，Windows 已排定在下次重新啟動時重新命名一個檔案，這會影響 {InstallerFolder}，因此 InstallerClean 沒有提供該資料夾中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows，然後再執行一次命令。`,
  'Cli.ScanSignal.RenameUnresolved': `掃描開始時，有一項檔案作業已排定在下次重新啟動時執行，而 InstallerClean 無法得知它涉及哪些檔案，因此沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows，然後再執行一次命令。`,
  'Cli.ScanSignal.AccessRefused': `掃描開始時，Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 後再執行一次命令。`,
  'Cli.ScanSignal.RegistryUnreadable': `掃描開始時，InstallerClean 無法讀取它會檢查的一項 Windows 設定，因此無法判斷是否有安裝程式作業正在執行或正在等候重新啟動，於是沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 後再執行一次命令。`,
  'Cli.ScanSignal.LockUnreadable': `掃描開始時，InstallerClean 無法檢查 Windows Installer 是否忙碌，因此沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 後再執行一次命令。`,
  'Cli.ScanSignal.LockUnreadableWindowsError': `掃描開始時，InstallerClean 無法檢查 Windows Installer 是否忙碌，因為 Windows 傳回了錯誤：{0}（錯誤 {1}）。因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 後再執行一次命令。`,
  'Cli.ScanSignal.Other': `掃描開始時，Windows Installer 有作業正在進行或等候中，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。等它結束後，請再執行一次命令。`,
  'Cli.ScanSignal.ClockSetBack': `掃描期間，這台電腦的時鐘倒退或變慢了，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請再執行一次命令。`,
  'Cli.FoundNoOrphans': `沒有找到不需要的檔案。`,
  'Cli.DestinationChangedMidBatch': `InstallerClean 已無法確認備份資料夾，因此沒有再繼續。請檢查 {0}，然後再執行一次命令。`,
  'Cli.Help.Summary': `移除已沒有任何已安裝程式需要的 .msi 和 .msp 快取檔案。`,
  'Cli.Help.Elevation': `需要系統管理員命令提示字元，否則不會掃描。`,
  'Error.InstallerLockUnavailableTitle': `沒有刪除任何檔案`,
  'Error.MoveInstallerLockUnavailableTitle': `沒有移動任何檔案`,
  'Error.InstallerLockUnavailable': `Windows Installer 用一個鎖定來防止兩個程式同時變更已安裝的軟體，而 InstallerClean 無法取得這個鎖定，因此無法排除某個檔案在過程中變成需要的可能，沒有刪除任何檔案。請再試一次，如果一直發生，請重新啟動 Windows。`,
  'Error.MoveInstallerLockUnavailable': `Windows Installer 用一個鎖定來防止兩個程式同時變更已安裝的軟體，而 InstallerClean 無法取得這個鎖定，因此無法排除某個檔案在過程中變成需要的可能，沒有移動任何檔案。請再試一次，如果一直發生，請重新啟動 Windows。`,
  'Cli.InstallerLockUnavailable': `錯誤：Windows Installer 用一個鎖定來防止兩個程式同時變更已安裝的軟體，而 InstallerClean 無法取得這個鎖定，因此無法排除某個檔案在過程中變成需要的可能。沒有刪除任何檔案。請再試一次，如果一直發生，請重新啟動 Windows。`,
  'Cli.MoveInstallerLockUnavailable': `錯誤：Windows Installer 用一個鎖定來防止兩個程式同時變更已安裝的軟體，而 InstallerClean 無法取得這個鎖定，因此無法排除某個檔案在過程中變成需要的可能。沒有移動任何檔案。請再試一次，如果一直發生，請重新啟動 Windows。`,
  'Completion.ReverifyIdentityClaimed': `有 {0} 個{1}留在原處，因為 Windows 有檔案內所標示程式的記錄。`,
  'Completion.ReverifyIdentityUnreadable': `有 {0} 個{1}留在原處，因為 InstallerClean 在檔案內找不到程式名稱。`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean 無法將 Windows Installer 的記錄與 {InstallerFolder} 中的內容對應起來。資料夾裡有檔案，但沒有任何一筆記錄指向其中的任何內容，因此無法證明任何檔案是不需要的。沒有提供任何檔案，也沒有移除任何內容。`,
  'Error.ScanInstallerFolderElsewhere': `InstallerClean 無法掃描這台電腦上的 {InstallerFolder}。Windows 回報該資料夾位於 {0}，而 InstallerClean 只會掃描 Windows 回報位於其原本路徑上的 Installer 資料夾。沒有提供任何檔案，也沒有移除任何內容。`,
  'Completion.NothingOffered': `這台電腦上沒有可提供的檔案`,
  'Completion.HeldBack.Singular': `已保留 {0} 個檔案。掃描認為它不需要，但最後的檢查無法確認這一點。`,
  'Completion.HeldBack.Plural': `已保留 {0} 個檔案。掃描認為它們不需要，但最後的檢查無法確認這一點。`,
  'Body.PendingReboot.PendingRenameUnresolved': `有一項檔案作業已排定在下次重新啟動時執行，而 InstallerClean 無法得知它涉及哪些檔案，因此無法排除這些檔案位於 {InstallerFolder} 的可能。請先重新啟動 Windows 再清理。`,
  'Completion.MoveRestoreHint': `等您確定一切正常後，再刪除那個資料夾。`,
  'Completion.MoveRestoreHintSameDrive': `等您確定一切正常後，再刪除那個資料夾。在那之前，空間不會真正釋放。`,
  'Confirm.MoveDestination.Singular': `這個檔案將移到：`,
  'Confirm.MoveDestination.Plural': `這些檔案將移到：`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `錯誤：有一項檔案作業已排定在下次重新啟動時執行，而 InstallerClean 無法得知它涉及哪些檔案，因此無法排除 {InstallerFolder}。請先重新啟動 Windows 再清理。`,
  'Cli.MoveRestoreHint': `請確認您的程式仍能照常更新和解除安裝，然後刪除 {0}。`,
  'Error.ScanStoppedDetails': `這也會記錄在 {0}。`,
  'Body.PendingReboot.RegistryCheckUnreadable': `InstallerClean 在碰 {InstallerFolder} 之前會檢查幾項 Windows 設定，其中一項無法讀取，因此無法判斷是否有安裝程式作業正在執行或正在等候重新啟動。請重新啟動 Windows 並重新掃描。如果該設定仍然無法讀取，這台電腦就不是 InstallerClean 能清理的。`,
  'Cli.InstallerLockAccessRefused': `錯誤：Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此它無法排除某個檔案在過程中變成需要的可能。沒有刪除任何檔案。`,
  'Cli.MoveCancelledRestoreHint': `要復原很簡單。把它們從 {0} 移回 {InstallerFolder}，一切就會恢復原狀。`,
  'Cli.MoveInstallerLockAccessRefused': `錯誤：Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此它無法排除某個檔案在過程中變成需要的可能。沒有移動任何檔案。`,
  'Cli.PendingRebootBlocked.RegistryCheckUnreadable': `錯誤：InstallerClean 在碰 {InstallerFolder} 之前會檢查幾個登錄值，其中一個無法讀取，因此無法排除有 Windows Installer 作業正在執行或已排定在下次重新啟動時執行。/m 和 /d 已被封鎖。請重新啟動 Windows 後再試一次。如果仍然無法讀取，這台電腦就不是 InstallerClean 能清理的。`,
  'Cli.PendingRebootBlocked.MsiExecuteMutexUnreadable': `錯誤：InstallerClean 無法檢查 Windows Installer 是否忙碌，因此無法排除有安裝正在進行。/m 和 /d 已被封鎖。請重新啟動 Windows 後再試一次。如果檢查仍然失敗，這台電腦就不是 InstallerClean 能清理的。`,
  'Cli.PendingRebootBlocked.MsiExecuteMutexUnreadableWindowsError': `錯誤：InstallerClean 無法檢查 Windows Installer 是否忙碌，因為 Windows 傳回了錯誤：{0}（錯誤 {1}），因此無法排除有安裝正在進行。/m 和 /d 已被封鎖。請重新啟動 Windows 後再試一次。如果檢查仍然失敗，這台電腦就不是 InstallerClean 能清理的。`,
  'Completion.MoveCancelledRestoreHint': `要復原很簡單。把它們移回 {InstallerFolder}，一切就會恢復原狀。`,
  'Error.InstallerLockAccessRefused': `Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此它無法排除某個檔案在過程中變成需要的可能，沒有刪除任何檔案。`,
  'Error.MoveInstallerLockAccessRefused': `Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此它無法排除某個檔案在過程中變成需要的可能，沒有移動任何檔案。`,
  'Error.MoveStoppedTitle': `移動已停止`,
  'Field.NoNamedProduct': `（無程式）`,
  'Summary.RegisteredWindow.Missing.Plural': `遺失 {0} 個`,
  'Summary.RegisteredWindow.Missing.Singular': `遺失 {0} 個`,
  'UpdateCheck.Failed.Unknown.NoLog': `檢查因不明原因失敗。無法寫入當機記錄檔。`,
  'Startup.AdminRightsNeededTitle': `需要系統管理員權限`,
  'Startup.AdminRightsNeededBody': `InstallerClean 需要系統管理員權限才能看到這台電腦上安裝的每個程式，但它目前不是以系統管理員身分執行，所以不會掃描。沒有做任何變更。\n\n請以系統管理員身分重新執行 InstallerClean。`,
  'Cli.AdminRightsNeeded': `InstallerClean 需要系統管理員權限才能看到這台電腦上安裝的每個程式，但它目前不是以系統管理員身分執行，所以不會掃描。沒有做任何變更。請從系統管理員命令提示字元重新執行。`,
  'Error.ScanInstallerFolderNotFound': `Windows 回報這台電腦上沒有 {InstallerFolder}，因此 InstallerClean 已停止。沒有提供任何檔案，也沒有移除任何內容。`,
  'Error.ScanInstallerFolderListFailed': `InstallerClean 嘗試列出 {InstallerFolder} 中的檔案時，Windows 回報了錯誤，因此 InstallerClean 無法取得完整的清單，已停止。沒有提供任何檔案，也沒有移除任何內容。`,
  'Body.RescanNotFinished.Lead': `下次掃描前沒有可顯示的內容。`,
  'Body.RescanNotFinished.Why': `移動或刪除後執行的掃描沒有完成，因此沒有清單可顯示。按下「重新掃描」即可再次掃描 {InstallerFolder}。`,
  'Body.RescanNotFinished.Recorded': `停止的原因已記錄在 {0}。`,
  'Status.MatchingCount': `{0}/{1}`,
  'Status.WaitingForDrive': `正在等候磁碟機 {0} 回應...`,
  'Status.WaitingForPath': `正在等候 {0} 回應...`,
  'Action.StopWaiting': `不再等候，繼續進行(_W)`,
  'Automation.StopWaitingForDrive': `不再等候，繼續進行，磁碟機 {0}`,
  'Automation.StopWaitingForPath': `不再等候，繼續進行，{0}`,
  'Status.StopWaitingLeavesAlone': `仍需與它核對的檔案都會原樣保留。`,
  'Automation.WaitingLineWithStopKey': `{0}按 Alt+{1} 即可不再等候，繼續進行。{2}`,
  'Summary.SourceGivenUp.Drive': `InstallerClean 已不再等候磁碟機 {0}，繼續進行，並將仍需與它核對的檔案都原樣保留。等它恢復正常回應後，請按「重新掃描」。`,
  'Summary.SourceGivenUp.Path': `InstallerClean 已不再等候 {0}，繼續進行，並將仍需與它核對的檔案都原樣保留。等它恢復正常回應後，請按「重新掃描」。`,
  'Summary.SourcesGivenUp': `InstallerClean 已不再等候多個磁碟機或共用資料夾（{0}），繼續進行，並將仍需與其中任何一個核對的檔案都原樣保留。等它們恢復正常回應後，請按「重新掃描」。`,
  'Summary.ScanSignal.Busy': `掃描開始時，有程式正在使用 Windows Installer，例如 Windows Update 或正在安裝的程式，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。等它結束後，請按「重新掃描」。`,
  'Summary.ScanSignal.Suspended': `掃描開始時，這台電腦上有一筆 Windows Installer 交易已暫停，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請繼續或復原那次安裝，或重新啟動 Windows，然後按「重新掃描」。`,
  'Summary.ScanSignal.RenameInCache': `掃描開始時，Windows 已排定在下次重新啟動時重新命名一個檔案，這會影響 {InstallerFolder}，因此 InstallerClean 沒有提供該資料夾中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows，然後按「重新掃描」。`,
  'Summary.ScanSignal.RenameUnresolved': `掃描開始時，有一項檔案作業已排定在下次重新啟動時執行，而 InstallerClean 無法得知它涉及哪些檔案，因此沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows，然後按「重新掃描」。`,
  'Summary.ScanSignal.AccessRefused': `掃描開始時，Windows 拒絕讓 InstallerClean 檢查 Windows Installer 是否忙碌，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 並按「重新掃描」。`,
  'Summary.ScanSignal.RegistryUnreadable': `掃描開始時，InstallerClean 無法讀取它會檢查的一項 Windows 設定，因此無法判斷是否有安裝程式作業正在執行或正在等候重新啟動，於是沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 並按「重新掃描」。`,
  'Summary.ScanSignal.LockUnreadable': `掃描開始時，InstallerClean 無法檢查 Windows Installer 是否忙碌，因此沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 並按「重新掃描」。`,
  'Summary.ScanSignal.LockUnreadableWindowsError': `掃描開始時，InstallerClean 無法檢查 Windows Installer 是否忙碌，因為 Windows 傳回了錯誤：{0}（錯誤 {1}）。因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請重新啟動 Windows 並按「重新掃描」。`,
  'Summary.ScanSignal.Other': `掃描開始時，Windows Installer 有作業正在進行或等候中，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。等它結束後，請按「重新掃描」。`,
  'Summary.ScanSignal.ClockSetBack': `掃描期間，這台電腦的時鐘倒退或變慢了，因此 InstallerClean 沒有提供 {InstallerFolder} 中任何已安裝程式的記錄都沒有提到的檔案，而是把它們保留了下來。請按「重新掃描」。`,
  'Display.DriveName': `磁碟機 {0}`,
  'Cli.SourceGivenUp.Drive': `InstallerClean 已不再等候磁碟機 {0}，並將仍需與它核對的檔案都原樣保留。等它恢復正常回應後，請再執行一次命令。`,
  'Cli.SourceGivenUp.Path': `InstallerClean 已不再等候 {0}，並將仍需與它核對的檔案都原樣保留。等它恢復正常回應後，請再執行一次命令。`,
  'Cli.SourcesGivenUp': `InstallerClean 已不再等候多個磁碟機或共用資料夾（{0}），並將仍需與其中任何一個核對的檔案都原樣保留。等它們恢復正常回應後，請再執行一次命令。`,
  'Action.CloseWithoutDonating': `不打賞，直接關閉(_C)`,
  'Tooltip.Donate': `InstallerClean 花了我大量心血，我其實應該收費，但我希望它保持免費，好幫助越多人越好。您的贊助讓這一切值得，我非常感激。`,
  'Cli.DonateAsk': `InstallerClean 是免費的。如果它幫上了忙，歡迎打賞，非常感謝：{0}`,
  'Completion.SendReport': `傳送匿名報告(_A)`,
  'Completion.ReportPanel.Intro': `報告會把您的結果加到 GitHub 上的[這張圖表]，圖表顯示人們用 InstallerClean 釋放了多少空間。報告也包含以下技術細節，幫助我發現程式錯誤：`,
  'Completion.ReportPanel.Freed': `釋放了多少空間，以及移動或刪除了多少個檔案`,
  'Completion.ReportPanel.Destination': `移動的目的地是同一個磁碟機、另一個磁碟機、抽取式磁碟機，還是網路共用資料夾`,
  'Completion.ReportPanel.Durations': `掃描以及移動或刪除各花了多少時間`,
  'Completion.ReportPanel.InstallerFiles': `每種安裝程式檔案各有多少個，以及它們佔用多少空間`,
  'Completion.ReportPanel.LeftAlone': `有多少個檔案原樣保留，以及原因`,
  'Completion.ReportPanel.Waits': `InstallerClean 等候緩慢的磁碟機或網路共用資料夾的次數，以及不再等候的有幾個`,
  'Completion.ReportPanel.Records': `Windows 記錄了多少個程式和修補程式`,
  'Completion.ReportPanel.ShortNames': `Windows 是否仍會產生舊式的短檔名`,
  'Completion.ReportPanel.Windows': `Windows 是 10 還是 11、是 x64 還是 Arm，以及設定的是哪種語言和哪個國家或地區`,
  'Completion.ReportPanel.AppVersion': `InstallerClean 的版本、取得方式（setup、portable、slim、Scoop、單獨的命令列工具、從原始碼建置，或未知）、報告是由 InstallerClean 還是命令列工具傳送的，以及其顯示的語言`,
  'Completion.ReportPanel.Errors': `所有出錯情況的次數`,
  'Completion.ReportPanel.Closing': `這些內容都是數字或標籤。報告中沒有任何內容能指出您、您的電腦、您的檔案或程式，我也從來看不到您的 IP 位址。`,
  'Automation.ReportInfo': `報告包含哪些內容`,
  'Automation.Scroll.ReportContents': `報告包含哪些內容`,
  'Completion.ReportPanel.SeeReport': `查看傳送的確切內容`,
  'Window.Report.Title': `匿名報告`,
  'Window.Report.Unreadable': `InstallerClean 無法讀取這份報告。`,
  'Automation.Scroll.Waiting': `正在等候磁碟機或共用資料夾`,
  'Completion.RecentChange.Singular': `已保留 1 個檔案（{2}），因為 Windows 顯示它在 InstallerClean 開始掃描後被變更過。如果當時有程式正在安裝或更新，請等它完成後再掃描一次，InstallerClean 會重新檢查它。`,
  'Completion.RecentChange.Plural': `已保留 {0} 個{1}（{2}），因為 Windows 顯示它們在 InstallerClean 開始掃描後被變更過。如果當時有程式正在安裝或更新，請等它完成後再掃描一次，InstallerClean 會重新檢查它們。`,
  'UpdateNotice.Available': `已推出 InstallerClean {0} 版。`,
  'UpdateNotice.Download': `從 GitHub 下載`,
  'Automation.UpdateNotice.Download': `從 github 下載`,
  'UpdateNotice.Recommended': `強烈建議使用最新版本。`,
  'UpdateNotice.UpdateNow': `立即更新`,
  'UpdateNotice.Downloading': `正在下載 InstallerClean {0}...`,
  'UpdateNotice.DownloadFailed': `無法下載更新。`,
  'UpdateNotice.InstallFailed': `無法安裝更新。`,
  'UpdateNotice.ContinueWithoutUpdating': `不更新，直接繼續(_C)`,
  'Cli.UpdatePrompt': `立即更新？強烈建議使用最新版本。[{0}/{1}]`,
  'Cli.UpdateAnswerYes': `Y`,
  'Cli.UpdateAnswerNo': `N`,
  'Cli.UpdateListsOnlyAfterDelete': `新版本會掃描並列出找到的內容，不會刪除任何檔案。如需刪除，請在之後再次執行 /d。`,
  'Cli.UpdateListsOnlyAfterMove': `新版本會掃描並列出找到的內容，不會移動任何檔案。如需移動，請在之後再次執行 /m。`,
  'Cli.UpdateRunningInstead': `改用 InstallerClean {1} 執行 {0}。`,
  'Cli.UpdateDownloadLink': `{0}：{1}`,
  'Cli.Help.UpdateNote': `掃描前，它會檢查是否有較新版本的 InstallerClean。由您親自執行時，&#10;它會在更新前詢問，一分鐘內無人回答則直接更新。在排定的工作或指令碼中&#10;自動執行時，它會直接更新而不詢問。&#10;加上 /noupdate 可略過檢查。`,
  'UpdateNotice.Scoop': `此副本由 Scoop 安裝。若要更新，請關閉 InstallerClean 並執行：`,
  'UpdateNotice.ScoopGlobal': `此副本由 Scoop 為所有使用者安裝。若要更新，請關閉 InstallerClean，以系統管理員身分開啟終端機並執行：`,
  'UpdateNotice.CommandCopied': `已複製`,
  'UpdateNotice.CopyFailed': `無法複製。請選取該命令並手動複製。`,
  'Automation.UpdateNotice.CopyCommand': `複製命令`,
  'Automation.UpdateNotice.ScoopCommand': `Scoop 更新命令`,
  'Cli.UpdateInUse': `InstallerClean 正在使用中，因此現在無法自行更新。`,
  'Error.FolderWillNotGiveFilesBack': `您可以在 {0} 中儲存檔案，但無法再次開啟，因此 InstallerClean 不會將檔案移到那裡。`,
  'UpdateNotice.NeedsNetFramework48': `此版本需要 .NET Framework 4.8。請從 Microsoft 安裝，重新啟動電腦，然後開啟 InstallerClean 進行更新。`,
  'UpdateNotice.CannotInstallAutomatically': `InstallerClean {0} 無法自動安裝。`,
  'UpdateNotice.CopyCannotTell': `InstallerClean 無法判斷自己是否由其安裝程式安裝，因此無法自行更新。`,
  'UpdateNotice.DownloadNetFramework48': `從 Microsoft 下載 .NET Framework 4.8`,
  'Automation.UpdateNotice.DownloadNetFramework48': `從 Microsoft 下載 .NET Framework 4.8`,
  'Cli.UpdateNeedsNetFramework48': `此版本需要 .NET Framework 4.8。請從 Microsoft 安裝，重新啟動電腦，然後再次執行 InstallerClean 進行更新。`,
  'Cli.ReportPrompt': `傳送本次執行的匿名報告？報告已儲存在 {0}。輸入 ? 可查看其內容。[{1}/{2}/?]`,
  'Cli.ReportChartAt': `地址為 {0} 的圖表`,
  'Cli.ReportIntro': `報告會把您的結果加到{0}，圖表顯示人們用 InstallerClean 釋放了多少空間。報告也包含以下技術細節，幫助我發現程式錯誤：`,
  'Cli.ReportInFull': `完整報告列印在下方。`,
  'Cli.ReportSent': `報告已傳送。謝謝。`,
  'Cli.ReportNotSent': `報告未傳送。`,
  'Cli.ReportCouldNotBeSent': `無法傳送報告。報告保存在 {0}。`,
  'Cli.ReportWaiting': `正在傳送較早一次執行留下的待傳送報告。`,
  'Cli.UpdateRunningNewer': `使用 InstallerClean {1} 執行 {0}。執行結束後，它將取代此副本。`,
  'Cli.UpdateNotRun': `無法執行較新的版本。`,
  'UpdateNotice.CheckingDownload': `正在檢查下載的檔案...`,
  'Cli.MutexLostDelete': `已刪除 {1} 個{2}中的 {0} 個後停止：InstallerClean 已無法確定沒有其他 InstallerClean 正在執行操作。請再次執行同一命令。結束代碼 2（部分完成）。`,
  'Cli.MutexLostMove': `已將 {1} 個{2}中的 {0} 個移動到 {3} 後停止：InstallerClean 已無法確定沒有其他 InstallerClean 正在執行操作。請再次執行同一命令。結束代碼 2（部分完成）。`,
  'Cli.CoverLostNoWorkDelete': `在刪除任何檔案之前已停止：InstallerClean 已無法確定沒有其他 InstallerClean 正在執行操作。請再次執行同一命令。結束代碼 75（暫時性）。`,
  'Cli.CoverLostNoWorkMove': `在移動任何檔案之前已停止：InstallerClean 已無法確定沒有其他 InstallerClean 正在執行操作。請再次執行同一命令。結束代碼 75（暫時性）。`,
  'Cli.UpdateNoAnswer': `一分鐘內沒有回答，因此 InstallerClean 會像無人可回答時一樣，不詢問直接更新。`,
  'Cli.ReportNoAnswer': `一分鐘內沒有回答。`,
};

// PARSE CONTROL. About the READING and not about the content, and it exits 2,
// which is a code no ordinary run of this generator can produce: a generator is
// red by intent for the whole gap between a string landing in English and its
// translation round, so its verdict lines and its exit 0 are load-bearing in
// ci.yml and are deliberately untouched here. This says something different from
// "the translation is not done". It says the file could not be read.
//
// BOTH LEGS. raw === 0 catches a file that declares no entry at all, which the
// equality cannot see on its own because 0 === 0 holds. parsed !== raw catches
// entries the reader dropped, which one <comment> moved above its <value> does to
// any regex wanting <value> on the same whitespace run, and the Visual Studio resx
// editor writes that shape. Counted with <data\b so a tab after the tag name is
// not read as an empty file, and neither figure is written down, so a string added
// to the resx cannot make this go stale.
//
// WHY IT IS HERE WHEN THE SELF-CHECK BELOW ALREADY REDDENS. The self-check reaches
// the right verdict through what it happens to compare, not through knowing it
// read anything: with the neutral's attribute order changed, this generator wrote a
// 389-entry file and its own self-check parsed THREE entries out of it, said
// GENERATION HAS ISSUES, and was right for a reason with nothing to do with the
// truth. A tool reasoning over three entries of a 389-entry artefact should say so.
const parseControl = (where, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${where}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this generator cannot show it read.');
  process.exit(2);
};

let text = readFileSync(BASE, 'utf8');
// The transform below reaches every entry through '<data name="', one space and no
// \s+, which is NOT the spelling the self-check's parse() uses further down. A
// control that exercises a pattern the reader does not use proves the file has
// structure and proves nothing about whether this reader can reach it, so the
// source is controlled in its own spelling before a single value is replaced.
parseControl(BASE, text,
  [...text.matchAll(/<data name="([^"]+)"[^>]*>\s*<value>/g)].length);

// Remove ONLY the machine-contract Cli.* <data> elements BY NAME (the
// Cli.EventLog* set bar Cli.EventLogUnavailable).
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
let cliMachineRemoved = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (isMachineCliKey(name)) { cliMachineRemoved++; return ''; } return m; });

// Replace each key's inner <value> from MAP.
const escRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const notApplied = [];
for (const [key, val] of Object.entries(MAP)) {
  const re = new RegExp('(<data name="' + escRe(key) + '"[^>]*>\\s*<value>)([\\s\\S]*?)(</value>)');
  let applied = false;
  text = text.replace(re, (m, p1, p2, p3) => { applied = true; return p1 + val + p3; });
  if (!applied) notApplied.push(key);
}

// Normalise to LF with exactly one trailing newline.
text = text.replace(/\r\n/g, '\n');
if (!text.endsWith('\n')) text += '\n';

writeFileSync(OUT, text, 'utf8');

// ---------------- self-check the written file against the neutral ----------------
const placeholders = (s) => new Set([...s.matchAll(/\{(\d+)(?::[^}]*)?\}/g)].map((p) => p[1]));
const parse = (xml, where) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  parseControl(where, xml, map.size);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'), BASE);
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written, OUT);
const neutralRequired = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

const missingFromMap = neutralRequired.filter((k) => !(k in MAP));
const strayMapKeys = Object.keys(MAP).filter((k) => !neutral.has(k));
const machineLeaked = [...output.keys()].filter(isMachineCliKey);

// The one human-facing Cli.EventLog* key, asserted present rather than left to
// the counts: a predicate that stopped discriminating it takes it out of the
// output AND out of the required set, so every figure above still agrees. The
// MAP substitution notices today only through the order the two run in.
const humanCliStripped = !output.has('Cli.EventLogUnavailable');
const missingFromOutput = neutralRequired.filter((k) => !output.has(k));
const arityMismatch = neutralRequired.filter((k) => {
  if (!output.has(k)) return false;
  const a = placeholders(neutral.get(k)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});
const crlf = (written.match(/\r/g) || []).length;

const alsoKeep = new Set(ALSO_KEEP);
const untranslated = neutralRequired.filter((k) =>
  output.has(k) && output.get(k) === neutral.get(k) && !KEEP_ENGLISH.has(k) && !alsoKeep.has(k));

// Breakdown computed, never pinned: the non-Cli and human-Cli totals both grow with
// every string the app gains, and a hardcoded pair goes stale silently while the
// checked figure beside it stays right.
const nonCliRequired = neutralRequired.filter((k) => !k.startsWith('Cli.')).length;
console.log('translatable <data> in output:', output.size,
  '(expect', neutralRequired.length,
  '=', nonCliRequired, 'non-Cli +', neutralRequired.length - nonCliRequired, 'Cli)');
console.log('machine Cli <data> removed:', cliMachineRemoved, `(expect ${cliMachineExpected})`);
console.log('MAP entries:', Object.keys(MAP).length, '| CRLF:', crlf, '(expect 0)');

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
if (missingFromOutput.length) console.log('!! required key missing from output:', missingFromOutput);
if (arityMismatch.length) console.log('!! placeholder arity differs from neutral:', arityMismatch);
if (machineLeaked.length) console.log('!! machine Cli keys leaked into output:', machineLeaked);
if (humanCliStripped) console.log('!! Cli.EventLogUnavailable stripped: that key is human-facing and must stay');
if (untranslated.length) {
  const show = untranslated.slice(0, 40).join(', ');
  console.log('!! still English (untranslated), ' + untranslated.length + ': ' + show +
    (untranslated.length > 40 ? ', ...and ' + (untranslated.length - 40) + ' more' : ''));
  if (untranslated.length > 50)
    console.log('   (that is most of the file: this is the untranslated template. Translate the MAP values, then a real miss is listed on its own.)');
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  !humanCliStripped &&
  output.size === neutralRequired.length && cliMachineRemoved === cliMachineExpected && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
