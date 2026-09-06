#!/usr/bin/env node
// Simplified Chinese (zh-Hans) satellite generator for InstallerClean. Copied
// from gen-strings-template.mjs (ko new pattern); only OUT and the MAP values
// differ. Works FROM THE ENGLISH SOURCE (Strings.resx): replaces each key's
// inner <value>, strips the machine-contract Cli.EventLog* keys, keeps the 39
// human Cli keys, and self-verifies against the neutral. Output is LF, UTF-8.
//
// Chinese plural rule (DisplayHelpers.CategoryFor, case "zh"): PluralCategory
// .Other at every count. Chinese nouns do not inflect for number, so there are
// NO .One/.Few/.Many override keys, and the Plural.* pairs are identical (both
// 文件 etc). The hardcoded .Singular/.Plural sentence pairs are translated on
// both members and come out identical.
//
// Register: 您 (formal-polite) throughout, dropped on imperatives, matching
// README.zh-CN.md and the Windows Chinese UI convention; warmth from word
// choice, never 你. Platform terms from Windows: About = 关于, Run as
// administrator = 以管理员身份运行, registry = 注册表, Event Log / Application
// log / Group Policy = 事件日志 / 应用程序日志 / 组策略.
//
// MAP values are the committed Strings.zh-Hans.resx <value> bytes verbatim: \\
// is one backslash (the paths), \n is a real newline (the multi-line values),
// &#10; is the literal entity where the neutral uses it, {0}/{1} are .NET
// placeholders left verbatim.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.zh-Hans.resx`;

// Universal keeps: keys whose value is the same in every language, the brand names
// and the pure-placeholder announcement string. Their still-English value is NOT a
// miss. Explicit by KEY on purpose: a future brand key then defaults to "flag until
// someone adds it here", never silently passes. Do NOT translate these values. Do
// NOT edit this list per language.
//
// The four size suffixes and the two elapsed suffixes do not belong in this list,
// because they are not universal: French writes Go/Mo/Ko/o, Russian and Ukrainian
// write ГБ/МБ/КБ/Б and мс/с. Those three carry real
// values in their MAP; the languages that do abbreviate as English does keep them in
// ALSO_KEEP, which is the per-language list. Display.ListSeparator is the same shape,
// for the same reason.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
]);

// Per-language keeps: empty for Simplified Chinese, which translates every
// translatable token (patch -> 补丁), so nothing beyond KEEP_ENGLISH stays English.
const ALSO_KEEP = [
  // The size and elapsed unit suffixes. Simplified Chinese abbreviates them exactly as
  // English does, so there is nothing to translate and nothing to get wrong.
  // A per-language keep rather than a universal one because fr, ru and uk do
  // NOT: French takes Go/Mo/Ko/o, Russian and Ukrainian take ГБ/МБ/КБ/Б and
  // мс/с, and all three carry real values in their MAP.
  'Display.Size.GB',           // {0:F2} GB
  'Display.Size.MB',           // {0:F1} MB
  'Display.Size.KB',           // {0:F1} KB
  'Display.Size.B',            // {0} B
  'Display.Elapsed.Ms',        // {0:F0}ms
  'Display.Elapsed.S',         // {0:F1}s
];

const MAP = {
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `关于`,
  'Window.Registered.Title': `原样保留的文件`,
  'Window.Orphaned.Title': `不需要的文件，可安全删除`,
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `补丁`,
  'Section.Registered.Details': `产品详情`,
  'Section.Backup.Folder': `备份文件夹`,
  'Section.SayThanks': `道声谢`,
  'Field.Reason': `原因`,
  'Field.Author': `作者`,
  'Field.Application': `应用程序`,
  'Field.Title': `标题`,
  'Field.Subject': `主题`,
  'Field.Keywords': `关键字`,
  'Field.SigningCertificate': `签名证书`,
  'Field.FileSize': `文件大小`,
  'Field.Comment': `备注`,
  'Field.ProductName': `产品名称`,
  'Field.File': `文件`,
  'Field.Size': `大小`,
  'Field.Patches': `补丁`,
  'Field.UnknownProductName': `（未知）`,
  'Field.PatchesOnly': `（仅补丁）`,
  'Field.Missing': `缺失`,
  'Action.About': `关于(_A)`,
  'Action.Copy': `复制`,
  'Action.Cut': `剪切`,
  'Action.Paste': `粘贴`,
  'Action.SelectAll': `全选`,
  'Action.Browse': `浏览(_B)…`,
  'Action.Cancel': `取消(_C)`,
  'Action.CheckForUpdates': `检查更新(_U)`,
  'Action.Close': `关闭(_C)`,
  'Action.DeletePermanently': `永久删除(_D)`,
  'Action.Done': `完成(_D)`,
  'Action.Details': `详情`,
  'Action.BuyMeACuppa': `请我喝杯茶(_B)`,
  'Action.LeaveStarOnGitHub': `在 GitHub 上点个星(_S)`,
  'Action.Licence': `Apache 2.0 许可证`,
  'Action.Move': `移动(_M)`,
  'Action.BackupFolderPlaceholder': `若选择移动而非删除，此处填写文件夹路径。`,
  'Action.OpenReleasePage': `打开发布页面(_R)`,
  'Action.Rescan': `重新扫描(_R)`,
  'Action.ScanAgain': `再次扫描(_S)`,
  'Action.SendResultLog': `发送报告`,
  'Action.SendResultLogConfirm': `发送(_S)`,
  'Automation.BuyMeACuppa': `捐赠`,
  'Automation.BuyMeACuppa.About': `请我喝杯茶`,
  'Automation.CancelOperation': `取消操作`,
  'Automation.CancelScan': `取消扫描`,
  'Automation.CancelStartupScan': `取消启动扫描`,
  'Automation.Close': `关闭`,
  'Automation.CloseWindow': `关闭窗口`,
  'Automation.CloseResult': `关闭结果并返回主窗口`,
  'Automation.LeaveStarOnGitHub.About': `在 github 上点个星`,
  'Automation.Minimise': `最小化`,
  'Automation.ConfirmDelete': `永久删除会移除这些不需要的文件。取消则不删除任何内容并关闭。`,
  'Automation.ConfirmMove': `移动会将不需要的文件放入所选的目标文件夹。取消则让它们留在原处。`,
  'Automation.SayThanks': `道声谢`,
  'Automation.ConfirmSendResultLog': `发送会将所示报告提交给 No Faff。取消则不发送任何内容。`,
  'Automation.CheckForUpdates': `检查更新`,
  'Automation.CheckForUpdates.HelpText': `在 github 的发布页面上检查是否有更新版本。`,
  'Automation.UpdateAvailable.HelpText': `打开发布页面以下载更新版本，或取消以保留当前版本。`,
  'Automation.Licence.HelpText': `在浏览器中打开 github.com 上的许可证文件。`,
  'Automation.Section.BackupFolder': `备份文件夹`,
  'Automation.Section.Patches': `补丁`,
  'Automation.Section.ProductDetails': `产品详情`,
  'Automation.BackupFolder': `备份文件夹`,
  'Automation.OperationProgress': `操作进度`,
  'Automation.RescanInstaller': `重新扫描 {InstallerFolder}`,
  'Automation.ScanningProgress': `扫描进度`,
  'Automation.StartupScanProgress': `启动扫描进度`,
  'Automation.ViewOrphanedFiles': `详情，不需要的文件`,
  'Automation.ViewOrphanedFiles.HelpText': `可供清理。`,
  'Automation.ViewRegisteredFiles': `详情，原样保留的文件`,
  'Automation.ViewRegisteredFiles.HelpText': `只读清单。`,
  'Automation.SortStatus.Ascending': `已按{0}升序排序`,
  'Automation.SortStatus.Descending': `已按{0}降序排序`,
  'Automation.Scroll.ScanResults': `扫描结果`,
  'Automation.Scroll.ResultDetails': `结果详情`,
  'Automation.Scroll.FileDetails': `文件详情`,
  'Automation.Scroll.DialogBody': `对话框文本`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `无法处理的文件`,
  'Automation.RegisteredMissingSeeAlso': `在 README 中解释了这个文件夹，以及如何恢复文件`,
  'Tooltip.BuyMeACuppa.About': `该来杯茶了！`,
  'Tooltip.CancellingPending': `已请求取消。InstallerClean 正在等待当前步骤到达一个可以停下来的位置。在大量 I/O 操作或 MSI 数据库调用期间，这可能需要几秒钟。`,
  'Tooltip.Close': `关闭`,
  'Tooltip.LeaveStarOnGitHub.About': `点个星有助于更多人发现 InstallerClean。`,
  'Tooltip.Minimise': `最小化`,
  'Tooltip.SendResultLog': `由您决定，但非常感谢。会发送一份匿名摘要，只是让我知道它是否正常工作，以及大家释放了多少空间。下一个界面会让您在确认前先看到将要发送的内容。`,
  'Tooltip.SendResultLog.NothingFound': `由您决定，但非常感谢。会发送一份匿名摘要，只是让我知道它是否正常工作。下一个界面会让您在确认前先看到将要发送的内容。`,
  'Tooltip.Move': `把不需要的文件移动到备份文件夹。`,
  'Tooltip.MoveNeedsDestination': `把不需要的文件移动到一个备份文件夹。您接下来会选择它。`,
  'Tooltip.Delete': `永久删除不需要的文件。如果您想有机会自己确认一切正常，请改用移动。`,
  'Tooltip.SigningCertificate': `来自内嵌 Authenticode 证书的使用者名称。未验证证书链。`,
  'Body.MainExplanation.Lead': `下面这些不需要的文件都[可以安全删除]。`,
  'Body.MainExplanation.Why': `它们位于 {InstallerFolder} 中。InstallerClean 会就每个已安装的程序询问 Windows：当没有任何程序认领某个文件时（{0}），或者当更新的补丁已经取代了它、并且没有任何程序能够回退到它时（{1}），该文件才会列出。`,
  'Body.MainExplanation.Action': `把它们移动到您选择的备份文件夹，等您确信自己的程序仍能照常更新和卸载时，再删除那个文件夹。把它们放回 {InstallerFolder} 就能恢复原状。或者现在就永久删除。`,
  'Body.PendingReboot.MsiExecuteMutex': `此刻有程序正在使用 Windows Installer，比如 Windows 更新，或者某个正在后台安装的程序。在此期间，移动和删除会暂停，这样 InstallerClean 就不会在 {InstallerFolder} 变动时去碰它。等结束后重新扫描，两者就会恢复。`,
  'Body.PendingReboot.InstallerInProgress': `这台计算机上有一个先前的 Windows Installer 事务处于挂起状态。请先继续或回滚那次安装（或重启 Windows），再清理 {InstallerFolder}。`,
  'Body.PendingReboot.PendingRenameInCache': `Windows 已把一次文件重命名排入下次重启的队列，且会影响 {InstallerFolder}。请先重启 Windows 再清理。`,
  'Body.NoFileSelected': `选择一个文件以查看详情。`,
  'Body.NoProductSelected': `选择一个产品以查看详情。`,
  'Body.NoMetadata': `没有可用的元数据。`,
  'Body.RegisteredMissingFromDisk': `这个安装文件不见了。现在不会造成任何麻烦，直到有一天您尝试更新或卸载它所属的程序为止。到那时这一步可能会失败，因为 Windows 会寻找这个文件而找不到它。\n\n要把它放回去，您需要您当前所用版本的安装程序。请从程序的制作方获取，并在现有副本上运行它。更新的版本不行：新版本必须先移除您现有的版本，而正是这一步需要这个文件。先卸载同样行不通，原因相同。这应当会恢复该文件并保持您的设置不变，但 Microsoft 并不保证。`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `README 用 Microsoft 自己的原话[解释了这个文件夹]，以及如何恢复文件。`,
  'Body.NoPatches': `（无）`,
  'Reason.Orphaned': `孤立`,
  'Reason.Superseded': `被取代`,
  'Reason.Obsoleted': `已废弃`,
  'Status.Scanning': `正在扫描…`,
  'Status.Cancelling': `正在取消…`,
  'Status.StartingScan': `正在开始扫描…`,
  'Status.QueryingApi': `正在向 Windows 查询已安装的软件…`,
  'Status.ScanningCache': `正在扫描安装程序缓存文件夹…`,
  'Status.EnumeratingProducts': `正在枚举已安装的产品…`,
  'Status.CheckingRegistry': `正在检查注册表中的其他程序包…`,
  'Status.RegisteredPackagesFound': `找到 {0} 个已注册的{1}。`,
  'Status.ScanComplete': `扫描完成（{0}）`,
  'Status.FoundProducts': `正在扫描本地程序包…`,
  'Status.FoundUnused': `找到 {0} 个{1}，可安全删除。`,
  'Status.PreparingDestination': `正在准备目标文件夹…`,
  'Status.Moving': `正在移动不需要的文件…`,
  'Status.Deleting': `正在删除不需要的文件…`,
  'Status.MoveCancelled.Partial': `移动已取消。{1} 个{2}中已处理 {0} 个。`,
  'Status.DeleteCancelled.Partial': `删除已取消。{1} 个{2}中已处理 {0} 个。`,
  'Status.MoveFailed': `{0}。详情见 {1}。`,
  'Status.MoveFailed.NoLog': `{0}。无法写入崩溃日志。`,
  'Status.DeleteFailed': `{0}。详情见 {1}。`,
  'Status.DeleteFailed.NoLog': `{0}。无法写入崩溃日志。`,
  'Status.ScanAccessDenied': `访问被拒绝。Windows 拒绝了扫描。`,
  'Status.ScanFailedDb': `扫描失败：无法读取 Windows Installer 记录。`,
  'Status.ScanCancelled': `扫描已取消。`,
  'Status.Done': `就绪`,
  'Status.ScanFailedDetails': `扫描失败（{0}）。详情见 {1}。`,
  'Status.ScanFailedDetails.NoLog': `扫描失败（{0}）。无法写入崩溃日志。`,
  'Completion.AllClean': `全部干净`,
  'Completion.NothingToCleanUp': `{InstallerFolder} 中没有需要清理的内容`,
  'Completion.NothingToCleanUpReceipt': `扫描了 {0} 个{1}，用时 {2}`,
  'Completion.Freed': `已释放 {0}`,
  'Completion.Moved': `已移动 {0}`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `没有移动任何文件`,
  'Completion.NothingDeleted': `没有删除任何文件`,
  'Completion.FailedCount.Singular': `{0} 个文件无法移动。`,
  'Completion.FailedCount.Plural': `{0} 个文件无法移动。`,
  'Completion.FailedCountDelete.Singular': `{0} 个文件无法删除。`,
  'Completion.FailedCountDelete.Plural': `{0} 个文件无法删除。`,
  'Completion.MoveSummary.Singular': `已将 {0} 个{1}移动到：{2}`,
  'Completion.MoveSummary.Plural': `已将 {0} 个{1}移动到：{2}`,
  'Completion.PermanentDeleteSummary.Singular': `已永久删除 {0} 个{1}`,
  'Completion.PermanentDeleteSummary.Plural': `已永久删除 {0} 个{1}`,
  'Summary.RegisteredStillUsed.Singular': `{0} 个文件原样保留`,
  'Summary.RegisteredStillUsed.Plural': `{0} 个文件原样保留`,
  'Summary.OrphanedToCleanUp.Singular': `{0} 个不需要的文件可清理`,
  'Summary.OrphanedToCleanUp.Plural': `{0} 个不需要的文件可清理`,
  'Summary.NothingListed.Singular': `InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供那 1 个文件，而是把它保留了下来。`,
  'Summary.NothingListed.Plural': `InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供 {0} 个{1}，而是把它们保留了下来。`,
  'Summary.MissingFromDisk.Singular': `Windows 有 {0} 个不在 {InstallerFolder} 中的文件的记录：{1}。日常使用不会有问题，但该程序的更新或卸载可能会失败。请打开详情了解该怎么做。`,
  'Summary.MissingFromDisk.Plural': `Windows 有 {0} 个不在 {InstallerFolder} 中的文件的记录：{1}。日常使用不会有问题，但这些程序的更新或卸载可能会失败。请打开详情了解该怎么做。`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `另外 {0} 个程序`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `另外 {0} 个程序`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} 个在记录中没有标明程序的文件`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} 个在记录中没有标明程序的文件`,
  'Summary.OperationFiles': `{1} 个{2}中的 {0} 个`,
  'Summary.OrphanedWindow': `{0} 个不需要的{1}（{2}）`,
  'Summary.RegisteredWindow.Singular': `{0} 个文件原样保留（{1}）`,
  'Summary.RegisteredWindow.Plural': `{0} 个文件原样保留（{1}）`,
  'Confirm.MoveTitle': `移动 {0} 个{1}（{2}）？`,
  'Confirm.DeleteTitle': `删除 {0} 个{1}（{2}）？`,
  'Error.AdminRequiredTitle': `访问被拒绝`,
  'Error.AdminRequiredBody': `Windows 拒绝了 InstallerClean 的访问，因此已停止。没有删除任何内容。\n\nInstallerClean 本来就以管理员身份运行，所以再那样启动一次也无济于事。Windows 没有进一步说明是什么拒绝了访问，因此没有具体可以尝试的办法。`,
  'Error.InstallerDbUnavailableTitle': `无法读取 Windows Installer 记录`,
  'Error.ScanFailedTitle': `扫描失败`,
  'Error.InstallerDbEmpty': `Windows Installer 记录返回的内容完全为空：没有任何一个已安装的程序或更新声称拥有缓存的安装文件。在正常工作的电脑上不会出现这种情况（即使是刚装好的 Windows 也会有一些），所以要么记录已损坏，要么无法读取；而一次相信这个结果的扫描，会把 {InstallerFolder} 中的每个文件都错误地判定为孤立。InstallerClean 没有那样做，而是停了下来。没有删除任何内容。`,
  'Error.MsiAccessDenied': `Windows Installer 不允许 InstallerClean 列出已安装的内容。InstallerClean 本来就以管理员身份运行，所以再以管理员身份运行一次也不会有任何改变。没有这份清单，就无法安全地判断哪些缓存文件仍然需要，因此 InstallerClean 停了下来。没有删除任何内容。`,
  'Error.MsiNonSuccess': `Windows Installer 无法向 InstallerClean 提供一份可读的已安装程序清单：它读取了 {2} {3}，随后连续 {0} 个条目返回时无法读取（最后的错误代码为 {1}）。InstallerClean 没有基于只读到一半的清单继续，而是停了下来。没有删除任何内容。`,
  'Error.InvalidDestinationTitle': `目标无效`,
  'Error.DestinationWriteFailedTitle': `无法使用该备份文件夹`,
  'Error.MoveFailedTitle': `移动失败`,
  'Error.DeleteFailedTitle': `删除失败`,
  'Error.SettingNotSavedTitle': `设置未保存`,
  'Error.SettingNotSavedBody': `无法保存此更改。下次启动时，InstallerClean 将恢复为之前的设置。`,
  'Error.DestinationInsideInstaller': `目标不能位于 Windows Installer 文件夹内。`,
  'Error.DestinationInSystemFolder': `目标 {0} 解析到了 Windows 系统文件夹之下。请选择 %SystemRoot%、%ProgramFiles%、%ProgramFiles(x86)% 和 %ProgramData% 之外的路径。`,
  'Error.NotEnoughSpaceTitle': `空间不足`,
  'Error.NotEnoughSpaceBody': `{0} 上放不下\n\n所需：{1}\n可用：{2}`,
  'Error.AccessDeniedDestination': `您没有写入 {0} 的权限。\n请尝试您的用户配置文件中的文件夹，或您拥有的驱动器。`,
  'Error.PathTooLong': `路径 {0} 对 Windows 来说太长了。请选择更短的路径。`,
  'Error.DestinationMissing': `文件夹 {0} 不存在，且无法创建。请检查驱动器盘符或网络路径。`,
  'Error.IOWriteDestination': `Windows 无法写入 {0}。\n详情见 {1}。`,
  'Error.IOWriteDestination.NoLog': `Windows 无法写入 {0}。无法写入崩溃日志。`,
  'Error.WriteDestination': `无法写入 {0}。\n详情见 {1}。`,
  'Error.WriteDestination.NoLog': `无法写入 {0}。无法写入崩溃日志。`,
  'Error.MissingSourceFile': `文件已不存在。`,
  'Error.SourceIsReparsePoint': `源文件是符号链接或目录联接；为安全起见已拒绝。`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows 拒绝了对此文件的访问；该文件已留在原处。`,
  'Error.AccessDenied.Plural': `Windows 拒绝了对这些文件的访问；这些文件已留在原处。`,
  'Error.FileInUse.Singular': `此文件正被另一个程序打开或锁定，因此暂时无法移除。它已保持原位；请稍后再试。`,
  'Error.FileInUse.Plural': `这些文件正被另一个程序打开或锁定，因此暂时无法移除。它们已保持原位；请稍后再试。`,
  'Error.IOFailure.Singular': `Windows 报告了一个文件错误；该文件已留在原处。`,
  'Error.IOFailure.Plural': `Windows 报告了文件错误；这些文件已留在原处。`,
  'Error.UnknownError.Singular': `此文件出了点问题；该文件已留在原处。`,
  'Error.UnknownError.Plural': `这些文件出了点问题；它们已留在原处。`,
  'Error.MoveIntoInstaller': `拒绝将文件移动到 Windows Installer 文件夹（目标：{0}）。`,
  'Error.DestinationNotFullyQualified': `备份文件夹必须是指向某个文件夹的完整路径，以驱动器盘符或网络共享开头（例如 D:\\Backup，或 \\\\server\\backup）。InstallerClean 无法使用这个：{0}`,
  'BrowserLaunch.FailedTitle': `无法打开您的浏览器`,
  'UpdateCheck.Title': `检查更新`,
  'UpdateCheck.Status.Checking': `正在检查…`,
  'UpdateCheck.Status.UpToDate': `已是最新版本。`,
  'UpdateCheck.UpdateAvailable.Title': `有可用更新`,
  'UpdateCheck.UpdateAvailable.Body': `您正在运行 {0} 版。&#10;{1} 版现已推出。`,
  'UpdateCheck.Failed.NetworkUnavailable': `无法连接到 GitHub。请检查您的网络连接后重试。`,
  'UpdateCheck.Failed.ServerError': `GitHub 返回了错误响应。请过几分钟后重试。`,
  'UpdateCheck.Failed.ResponseParseError': `GitHub 的响应中没有可识别的发布版本。请稍后重试，或直接打开发布页面。`,
  'UpdateCheck.Failed.Timeout': `检查超时。您与 GitHub 的连接可能较慢；请重试。`,
  'UpdateCheck.Failed.Unknown': `检查因未知原因失败。如果您需要报告此问题，详情在 {0} 中。`,
  'BrowserLaunch.ClipboardOk': `链接已复制到剪贴板，您可以自己粘贴：&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean 也无法把链接复制到剪贴板，链接在这里：&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `InstallerClean 已无法确认备份文件夹，因此停了下来。请检查 {0}，然后重新扫描并再试一次。`,
  'Error.CannotWriteFolder': `无法写入 {0}。`,
  'Error.DestinationCollision': `备份文件夹中已经有一个名为“{0}”的文件。`,
  'ResultLog.Sending': `正在发送…`,
  'ResultLog.Sent': `谢谢！报告已发送。`,
  'ResultLog.Failed': `发送失败。请稍后重试。`,
  'ResultLog.NothingToSend': `没有可发送的报告。`,
  'ConfirmSendResultLog.Title': `把这个发送吗？`,
  'ConfirmSendResultLog.Reassurance': `它会发送到 nofaff.netlify.app/api/result-log。没有任何内容能识别您或您的机器；它只是让我知道 InstallerClean 是否正常工作，以及[大家释放了多少空间]。`,
  'Automation.ResultLogPreview': `报告预览`,
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `已在运行。`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `发生了意外错误，InstallerClean 需要关闭。\n\n{0}\n\n详情已写入：\n{1}`,
  'Startup.UnhandledBody.NoLog': `发生了意外错误，InstallerClean 需要关闭。\n\n{0}\n\n无法写入崩溃日志。`,
  'Startup.ErrorTitle': `启动错误`,
  'Startup.FailedToStart': `启动失败（{0}）。详情已写入：\n{1}`,
  'Startup.FailedToStart.NoLog': `启动失败（{0}）。无法写入崩溃日志。`,
  'FilePicker.ChooseDestinationTitle': `为移动的文件选择目标文件夹`,
  'Version.Display': `版本 {0}`,
  'Plural.File.Singular': `文件`,
  'Plural.File.Plural': `文件`,
  'Plural.Error.Singular': `错误`,
  'Plural.Error.Plural': `错误`,
  'Plural.Package.Singular': `程序包`,
  'Plural.Package.Plural': `程序包`,
  'Plural.Product.Singular': `产品`,
  'Plural.Product.Plural': `产品`,
  'Plural.Patch.Singular': `补丁`,
  'Plural.Patch.Plural': `补丁`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ListSeparator': `、`,
  'Display.ElapsedLong.LessThanASecond': `不到一秒`,
  'Display.ElapsedLong.Seconds': `{0:F1} 秒`,
  'Cli.UnknownArgument': `错误：未知参数“{0}”`,
  'Cli.Cancelling': `正在取消…`,
  'Cli.Cancelled': `已取消。`,
  'Cli.GenericError': `错误：意外故障（{0}）。详情已写入 {1}。`,
  'Cli.GenericError.NoLog': `错误：意外故障（{0}）。崩溃日志无法写入。`,
  'Cli.ScanningInstaller': `正在扫描 {InstallerFolder}…`,
  'Cli.FoundOrphans': `找到 {0} 个可清理的、不需要的{1}（{2}）。`,
  'Cli.DeletingFiles': `正在删除 {0} 个不需要的{1}…`,
  'Cli.DeletedFiles': `已永久删除 {0} 个不需要的{1}。`,
  'Cli.NoMoveDestination': `错误：未指定移动目标位置。请使用 /m 路径。（在 GUI 中设置的默认位置是按用户保存的，不适用于计划任务或服务账户下的运行。）`,
  'Cli.MoveDestinationInsideInstaller': `错误：目标位置不能位于 Windows Installer 文件夹内。`,
  'Cli.MoveDestinationRelative': `错误：目标位置必须是完整路径。收到：{0}`,
  'Cli.MoveDestinationInSystemFolder': `错误：目标 {0} 解析到了 Windows 系统文件夹之下。请选择 %SystemRoot%、%ProgramFiles%、%ProgramFiles(x86)% 和 %ProgramData% 之外的路径。`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `错误：此刻有程序正在使用 Windows Installer，比如 Windows 更新，或者某个正在后台安装的程序。在此期间 /m 和 /d 会被阻止。等结束后再试。`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `错误：这台计算机上有一个先前的 Windows Installer 事务处于挂起状态。请先继续或回滚那次安装（或重启 Windows），再清理 {InstallerFolder}。`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `错误：一项排在重启之后的文件操作指向 {InstallerFolder}（{0}）。请先重启 Windows 让该操作完成，再进行清理。`,
  'Cli.MovingFiles': `正在把 {0} 个不需要的{1}移动到 {2}…`,
  'Cli.MovedFiles': `已移动 {0} 个不需要的{1}。`,
  'Cli.MutexBlocked': `另一个 InstallerClean 进程正持有单实例锁（GUI 或另一次 CLI 运行）。退出代码 75（暂时性）；稍后可安全重试。`,
  'Cli.EventLogUnavailable': `注意：事件日志写入失败。请检查应用程序日志的权限或组策略。`,
  'CrashLog.PrivacyHeader': `# crash.log 记录 InstallerClean 未处理的异常。\n# 在提升权限的情况下，框架的异常消息可能包含当前会话中的文件路径\n#（包括 Windows Installer 查询所枚举的其他用户的配置文件）。更新\n# 检查或结果日志上传的网络故障消息，可能包含目标 URL 以及解析出的\n# IP 或代理地址。关于无法读取的 Windows Installer 记录的条目，可能\n# 包含 Windows 账户 SID（S-1-5-21-...）以及已安装软件的产品代码。\n# 把此文件附到公开的错误报告之前，请先删除这三类信息。\n`,
  'Cli.Help.Header': `InstallerClean - 清理 {InstallerFolder}`,
  'Cli.Help.Usage': `用法：`,
  'Cli.Help.Help': `  installerclean-cli --help     显示此帮助（也接受 /?、-h）`,
  'Cli.Help.Version': `  installerclean-cli --version  显示版本号（也接受 -v）`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         仅扫描 - 列出不需要的文件`,
  'Cli.Help.Delete': `  installerclean-cli /d         永久删除不需要的文件`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         移动到已保存的备份文件夹`,
  'Cli.Help.MovePath': `  installerclean-cli /m 路径    移动到指定路径`,
  'Cli.Help.NoteLine1': `installerclean-cli 会占住命令提示符直到结束，因此脚本或计划任务&#10;可以等待它完成。`,
  'Cli.Help.ExitCodesHeader': `退出代码：`,
  'Cli.Help.ExitCodeOk': `  0   成功：本次运行做了要求的事，并且没有任何失败`,
  'Cli.Help.ExitCodeError': `  1   失败：没有处理任何内容（参数或目标有误、扫描失败，&#10;       或者每个文件都失败）`,
  'Cli.Help.ExitCodePartial': `  2   部分：部分已处理，部分未处理（失败或 Ctrl+C）`,
  'Cli.Help.ExitCodeTransient': `  75  暂时性：临时状况阻止了本次运行（见相关消息）`,
  'Cli.Help.ExitCodeCancelled': `  130 已取消（Ctrl+C）`,
  'Tooltip.ChangeLanguage': `更改语言。程序会重启。`,
  'Automation.ChangeLanguage': `更改语言`,
  'Automation.ChangeLanguage.HelpText': `程序会重启。`,
  'Body.NotScanned.Lead': `尚未扫描。`,
  'Body.NotScanned.Why': `点击“重新扫描”，在 {InstallerFolder} 中查找没有任何程序仍然需要的安装程序文件。`,
  'Confirm.MoveSameDrive': `那个文件夹在同一个驱动器上，所以在您删除它之前空间不会回来。如果想立刻拿回空间，请改选另一个驱动器上的文件夹。`,
  'Error.ScanCorrelationFailed': `InstallerClean 未能把 Windows Installer 记录与 {InstallerFolder} 中的内容对应起来。记录所指向的内容几乎都不在那里，而那里的内容几乎都没有被任何记录标明，因此无法证明任何文件是不需要的。没有提供任何内容，也没有移除任何内容。`,
  'Error.CandidateOutsideCache': `此文件不直接位于 Windows Installer 文件夹内；为安全起见已拒绝。`,
  'Completion.MoveCancelledSummary': `在您取消前，已将 {1} 个{2}中的 {0} 个移动到 {3}。`,
  'Completion.PermanentDeleteCancelledSummary': `在您取消前，已永久删除 {1} 个{2}中的 {0} 个。`,
  'Body.PendingReboot.Lead': `这些文件现在无法清理。`,
  'Cli.TooManyArguments': `错误：出现意外的多余参数“{0}”。如果目标文件夹的路径中含有空格，请给整个路径加上引号：/m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `该文件夹按用户保存；计划任务或 SYSTEM 运行需要 /m 路径。`,
  'Error.ScanRecordsUnreadable': `InstallerClean 未能读取到足够的 Windows Installer 记录，无法确定哪些内容仍然需要：已安装程序的清单返回时并不完整，而直接从注册表读取同样的记录也遇到了错误。一个文件可能仅仅因为指明它的那条记录属于读不到的记录之一，就显得像是孤立的，因此 InstallerClean 停了下来。没有删除任何内容。`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer 始终没有发出已安装程序清单结束的信号：InstallerClean 读取了 {2} {3}，随后在 {0} 个条目后放弃（最后的错误代码为 {1}）。没有尽头的清单无法信任，因此 InstallerClean 停了下来。没有删除任何内容。`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer 始终没有发出某个程序补丁清单结束的信号：InstallerClean 读取了 {2} {3}，随后在 {0} 个条目后放弃（最后的错误代码为 {1}）。没有尽头的清单无法信任，因此 InstallerClean 停了下来。没有删除任何内容。`,
  'UpdateCheck.Status.UpdateAvailable': `{0} 版现已推出。`,
  'Completion.DonateAsk': `很高兴帮上忙。您若有心，这里可以打赏。`,
  'About.Link.Guide': `指南和常见问题`,
  'About.Link.ReportProblem': `报告问题`,
  'About.AutoUpdateCheck': `自动检查更新`,
  'Automation.About.Guide.HelpText': `在浏览器中打开 github 上的 readme。`,
  'Automation.About.ReportProblem.HelpText': `在浏览器中打开 github.com 上的问题追踪页面（Issues）。`,
  'Automation.AutoUpdateCheck.HelpText': `勾选后，InstallerClean 运行时会在 github 上检查是否有更新版本。`,
  'Tooltip.MoveSameDrive': `把不需要的文件移动到备份文件夹。它在同一个驱动器上，所以要等您删除那个文件夹后才会释放空间。`,
  'Confirm.DeletePermanently.Singular': `此文件将被永久删除。这么做是安全的，但如果您想要备份，请改用移动。`,
  'Confirm.DeletePermanently.Plural': `这些文件将被永久删除。这么做是安全的，但如果您想要备份，请改用移动。`,
  'Error.ScanCacheRootUnresolved': `InstallerClean 未能让 Windows 解析出 {InstallerFolder} 的真实路径，因此无法证明任何文件位于其中，也没有提供任何文件供清理。这次扫描一无所获是因为那项检查失败，而不是因为文件夹是干净的。没有移除任何内容。`,
  'Automation.Scroll.ProductDetails': `产品详情`,
  'Body.PendingReboot.Other': `Windows Installer 有操作正在进行，因此移动和删除已暂停。InstallerClean 不会在 {InstallerFolder} 变动时去碰它。等结束后重新扫描，两者就会恢复。`,
  'Cli.TooManyArgumentsNoPath': `错误：出现意外的多余参数“{0}”。/s 和 /d 不接受其他参数，每次运行也只能使用一个开关。`,
  'Cli.MissingFromDisk.Singular': `Windows 有 {0} 个不在 {InstallerFolder} 中的文件的记录：{1}。日常使用不会有问题，但该程序的更新或卸载可能会失败。要把该文件放回去，您需要您当前所用版本的安装程序。请从程序的制作方获取，并在现有副本上运行它。更新的版本不行：新版本必须先移除您现有的版本，而正是这一步需要这个文件。先卸载同样行不通，原因相同。这应当会恢复该文件并保持您的设置不变，但 Microsoft 并不保证。`,
  'Cli.MissingFromDisk.Plural': `Windows 有 {0} 个不在 {InstallerFolder} 中的文件的记录：{1}。日常使用不会有问题，但这些程序的更新或卸载可能会失败。要把某个文件放回去，您需要该程序当前所用版本的安装程序。请从程序的制作方获取，并在现有副本上运行它。更新的版本不行：新版本必须先移除您现有的版本，而正是这一步需要该文件。先卸载同样行不通，原因相同。这应当会恢复该文件并保持您的设置不变，但 Microsoft 并不保证。`,
  'Cli.MoveNotEnoughSpace': `错误：{0} 上的空间不足。移动这些文件需要 {1}，而可用空间为 {2}。没有移动任何内容。`,
  'Cli.PendingRebootBlocked.Other': `错误：Windows Installer 有操作正在进行，因此 /m 和 /d 会被阻止。InstallerClean 不会在 {InstallerFolder} 变动时去碰它。等结束后再试。`,
  'Cli.FoundNoOrphans': `未找到不需要的文件。`,
  'Cli.NothingOffered.Singular': `InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供那 1 个文件（{2}），而是把它保留了下来。`,
  'Cli.NothingOffered.Plural': `InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供全部 {0} 个{1}（{2}），而是把它们保留了下来。`,
  'Cli.DestinationChangedMidBatch': `InstallerClean 已无法确认备份文件夹，因此停了下来。请检查 {0}，然后重新运行该命令。`,
  'Cli.Help.Summary': `移除没有任何已安装程序仍然需要的 .msi 和 .msp 缓存文件。`,
  'Cli.Help.Elevation': `需要管理员命令提示符，否则 Windows 不会启动它。`,
  'Error.InstallerLockUnavailableTitle': `没有删除任何文件`,
  'Error.MoveInstallerLockUnavailableTitle': `没有移动任何文件`,
  'Error.InstallerLockUnavailable': `InstallerClean 未能取得 Windows Installer 用来防止两个程序同时更改已安装软件的锁，因此无法排除某个文件在中途变成必需的可能，也没有删除任何内容。请重试，若一直如此请重启 Windows。`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean 未能取得 Windows Installer 用来防止两个程序同时更改已安装软件的锁，因此无法排除某个文件在中途变成必需的可能，也没有移动任何内容。请重试，若一直如此请重启 Windows。`,
  'Cli.InstallerLockUnavailable': `错误：InstallerClean 未能取得防止两个程序同时更改已安装软件的 Windows Installer 锁，因此无法排除某个文件在中途变成必需的可能。没有删除任何内容。请重试，若一直如此请重启 Windows。`,
  'Cli.MoveInstallerLockUnavailable': `错误：InstallerClean 未能取得防止两个程序同时更改已安装软件的 Windows Installer 锁，因此无法排除某个文件在中途变成必需的可能。没有移动任何内容。请重试，若一直如此请重启 Windows。`,
  'Completion.ReverifyIdentityClaimed': `有 {0} 个{1}保持原位，因为 Windows 有文件内所标示程序的记录。`,
  'Completion.ReverifyIdentityUnreadable': `有 {0} 个{1}保持原位，因为 InstallerClean 没能在文件内找到程序名。`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean 未能把 Windows Installer 记录与 {InstallerFolder} 中的内容对应起来。文件夹里有文件，但没有任何一条记录指向其中的任何内容，因此无法证明任何文件是不需要的。没有提供任何内容，也没有移除任何内容。`,
  'Completion.NothingOffered': `在这台电脑上没有提供任何内容`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供那 1 个文件（{2}），而是把它保留了下来。`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供全部 {0} 个{1}（{2}），而是把它们保留了下来。`,
  'Summary.SupersededHeldBack.Singular': `InstallerClean 无法确定唯一那个被取代的文件已不再需要，因此保留了它。`,
  'Summary.SupersededHeldBack.Plural': `InstallerClean 无法确定 {0} 个被取代的文件已不再需要，因此保留了它们。`,
  'Cli.SupersededHeldBack.Singular': `InstallerClean 无法确定唯一那个被取代的文件已不再需要，因此保留了它。`,
  'Cli.SupersededHeldBack.Plural': `InstallerClean 无法确定 {0} 个被取代的文件已不再需要，因此保留了它们。`,
  'Completion.HeldBack.Singular': `已保留 {0} 个文件。扫描认为它不需要，但最终检查无法确认这一点。`,
  'Completion.HeldBack.Plural': `已保留 {0} 个文件。扫描认为它们不需要，但最终检查无法确认这一点。`,
  'Body.PendingReboot.PendingRenameUnresolved': `有一项文件操作已排入下次重启的队列，InstallerClean 无法得知它指名了哪些文件，因此无法排除这些文件位于 {InstallerFolder} 的可能。请在清理前重启 Windows。`,
  'Completion.MoveRestoreHint': `等您确信一切正常时，再删除那个文件夹。`,
  'Completion.MoveRestoreHintSameDrive': `等您确信一切正常时，再删除那个文件夹。在那之前空间不会真正释放。`,
  'Confirm.MoveDestination.Singular': `此文件将移动到：`,
  'Confirm.MoveDestination.Plural': `这些文件将移动到：`,
  'Cli.NothingListed.Singular': `InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供那 1 个文件（{2}），而是把它保留了下来。`,
  'Cli.NothingListed.Plural': `InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供 {0} 个{1}（{2}），而是把它们保留了下来。`,
  'Cli.WithheldReasons.Header': `无法确定的原因：`,
  'Cli.WithheldReasons.RecordedPath': `  Windows Installer 自身记录中的一个文件路径无法解析，因此没有任何内容能与它对应起来。`,
  'Cli.WithheldReasons.FileIdentity': `  无法识别 Windows 有记录的某个文件，因此无法把它与文件夹中的内容对应起来。`,
  'Cli.WithheldReasons.SecondInstance': `  某个程序可能在这台电脑上安装了不止一次，而记录无法说明某个文件属于哪一份。`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `错误：有一项文件操作已排入下次重启的队列，InstallerClean 无法得知它指名了哪些文件，因此无法排除 {InstallerFolder}。请在清理前重启 Windows。`,
  'Cli.MoveRestoreHint': `请确认您的程序仍能照常更新和卸载，然后删除 {0}。`,
  'Error.ScanStoppedDetails': `这也会记录在 {0} 中。`,
  'Cli.NothingListedPerFile.Singular': `InstallerClean 对它找到的缓存文件中的一个没有把握，因此没有提供那一个（{2}），而是把它保留了下来。`,
  'Cli.NothingListedPerFile.Plural': `InstallerClean 对它找到的部分缓存文件没有把握，因此没有提供 {0} 个{1}（{2}），而是把它们保留了下来。`,
  'Cli.NothingOfferedPerFile.Singular': `InstallerClean 无法证实它找到的那个缓存文件是不需要的，因此没有提供那 1 个文件（{2}），而是把它保留了下来。`,
  'Cli.NothingOfferedPerFile.Plural': `InstallerClean 无法证实它找到的缓存文件中有任何一个是不需要的，因此没有提供全部 {0} 个{1}（{2}），而是把它们保留了下来。`,
  'Completion.NothingOfferedPerFileBody.Singular': `InstallerClean 无法证实它找到的那个缓存文件是不需要的，因此没有提供那 1 个文件（{2}），而是把它保留了下来。`,
  'Completion.NothingOfferedPerFileBody.Plural': `InstallerClean 无法证实它找到的缓存文件中有任何一个是不需要的，因此没有提供全部 {0} 个{1}（{2}），而是把它们保留了下来。`,
  'Summary.NothingListedPerFile.Singular': `InstallerClean 对它找到的缓存文件中的一个没有把握，因此没有提供它，而是把它保留了下来。`,
  'Summary.NothingListedPerFile.Plural': `InstallerClean 对它找到的部分缓存文件没有把握，因此没有提供 {0} 个{1}，而是把它们保留了下来。`,
  'Cli.WithheldReasons.CandidateIdentity': `  无法识别文件夹中的某个文件，因此无法把它与记录对应起来。`,
  'Cli.WithheldReasons.DeclaredProductInstalled': `  某个文件声称属于一个仍然安装着的程序，因此可能仍然需要。`,
  'Cli.WithheldReasons.DeclaredProductUnestablished': `  要么某个文件没有说明它属于哪个程序，要么 Windows 没有就该程序作出回答。`,
  'Cli.WithheldReasons.ScreenUnanswered': `  一项关于这些文件属于哪些程序的检查，给出的答案与交给它的文件对不上。`,
  'Body.PendingReboot.RegistryCheckUnreadable': `InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean.`,
  'Cli.InstallerLockAccessRefused': `Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted.`,
  'Cli.MoveCancelledRestoreHint': `It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was.`,
  'Cli.MoveInstallerLockAccessRefused': `Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.RegistryCheckUnreadable': `Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean.`,
  'Completion.MoveCancelledRestoreHint': `It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was.`,
  'Error.InstallerLockAccessRefused': `Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted.`,
  'Error.MoveInstallerLockAccessRefused': `Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved.`,
  'Error.MoveStoppedTitle': `Move stopped`,
  'Field.NoNamedProduct': `(no program)`,
  'Summary.RegisteredWindow.Missing.Plural': `{0} missing`,
  'Summary.RegisteredWindow.Missing.Singular': `{0} missing`,
  'UpdateCheck.Failed.Unknown.NoLog': `The check failed for an unknown reason. The crash log could not be written.`,
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
