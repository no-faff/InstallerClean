#!/usr/bin/env node
// Simplified Chinese (zh-Hans) satellite generator for InstallerClean. Copied
// from gen-strings-template.mjs (ko new pattern); only OUT and the MAP values
// differ. Works FROM THE ENGLISH SOURCE (Strings.resx): replaces each key's
// inner <value>, strips the 20 machine-contract Cli.EventLog* keys, keeps the 39
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
// choice, never 你. Platform terms from Windows: Recycle Bin = 回收站, About =
// 关于, Run as administrator = 以管理员身份运行, registry = 注册表, Event Log /
// Application log / Group Policy = 事件日志 / 应用程序日志 / 组策略.
//
// MAP values are the committed Strings.zh-Hans.resx <value> bytes verbatim: \\
// is one backslash (the paths), \n is a real newline (the multi-line values),
// &#10; is the literal entity where the neutral uses it, {0}/{1} are .NET
// placeholders left verbatim.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.zh-Hans.resx`;

// Universal keeps: keys whose value is the same in every language (brand names,
// the pure-placeholder string, the size/elapsed format strings). Their still-
// English value is NOT a miss. Do NOT translate these values. Do NOT edit this
// list per language.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
  'Display.Size.GB',                   // {0:F2} GB
  'Display.Size.MB',                   // {0:F1} MB
  'Display.Size.KB',                   // {0:F1} KB
  'Display.Size.B',                    // {0} B
  'Display.Elapsed.Ms',                // {0:F0}ms
  'Display.Elapsed.S',                 // {0:F1}s
]);

// Per-language keeps: empty for Simplified Chinese, which translates every
// translatable token (patch -> 补丁), so nothing beyond KEEP_ENGLISH stays English.
const ALSO_KEEP = [];

const MAP = {
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `关于`,
  'Window.Registered.Title': `不应删除的已注册文件`,
  'Window.Orphaned.Title': `不需要的文件，可安全删除`,
  'Section.Registered.Products': `产品`,
  'Section.Registered.Patches': `补丁`,
  'Section.Registered.Details': `产品详情`,
  'Section.Backup.Folder': `BACKUP FOLDER`,
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
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
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
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
  'Automation.ConfirmMove': `移动会将不需要的文件放入所选的目标文件夹。取消则让它们留在原处。`,
  'Automation.SayThanks': `道声谢`,
  'Automation.ConfirmSendResultLog': `发送会将所示报告提交给 No Faff。取消则不发送任何内容。`,
  'Automation.CheckForUpdates': `检查更新`,
  'Automation.CheckForUpdates.HelpText': `在 github 的发布页面上检查是否有更新版本。`,
  'Automation.UpdateAvailable.HelpText': `打开发布页面以下载更新版本，或取消以保留当前版本。`,
  'Automation.Licence.HelpText': `在浏览器中打开 github.com 上的许可证文件。`,
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `产品`,
  'Automation.Section.Patches': `补丁`,
  'Automation.Section.ProductDetails': `产品详情`,
  'Automation.BackupFolder': `Backup folder`,
  'Automation.OperationProgress': `操作进度`,
  'Automation.RescanInstaller': `重新扫描 {InstallerFolder}`,
  'Automation.ScanningProgress': `扫描进度`,
  'Automation.StartupScanProgress': `启动扫描进度`,
  'Automation.ViewOrphanedFiles': `详情，不需要的文件`,
  'Automation.ViewOrphanedFiles.HelpText': `可供清理。`,
  'Automation.ViewRegisteredFiles': `详情，已注册文件`,
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
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `来自内嵌 Authenticode 证书的使用者名称。未验证证书链。`,
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `它们位于 {InstallerFolder}，是在卸载程序（{0}）、新补丁取代旧补丁（{1}）或发布者撤回补丁（{2}）时遗留下来的。InstallerClean 只会列出 Windows 自己报告为不再需要的文件。`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.NoFileSelected': `选择一个文件以查看详情。`,
  'Body.NoProductSelected': `选择一个产品以查看详情。`,
  'Body.NoMetadata': `没有可用的元数据。`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.&#10;&#10;It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.&#10;&#10;To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself.`,
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
  'Status.Moving': `Moving files...`,
  'Status.Deleting': `Deleting files...`,
  'Status.MoveCancelled.Partial': `移动已取消。{1} 个{2}中已处理 {0} 个。`,
  'Status.DeleteCancelled.Partial': `删除已取消。{1} 个{2}中已处理 {0} 个。`,
  'Status.MoveFailed': `移动失败（{0}）。详情见 {1}。`,
  'Status.MoveFailed.NoLog': `移动失败（{0}）。无法写入崩溃日志。`,
  'Status.DeleteFailed': `删除失败（{0}）。详情见 {1}。`,
  'Status.DeleteFailed.NoLog': `删除失败（{0}）。无法写入崩溃日志。`,
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
  'Completion.FailedCount.Singular': `{1} 个文件中有 {0} 个无法移动。`,
  'Completion.FailedCount.Plural': `{1} 个文件中有 {0} 个无法移动。`,
  'Completion.FailedCountDelete.Singular': `{1} 个文件中有 {0} 个无法删除。`,
  'Completion.FailedCountDelete.Plural': `{1} 个文件中有 {0} 个无法删除。`,
  'Completion.MoveSummary.Singular': `已将 {0} 个{1}移动到：{2}`,
  'Completion.MoveSummary.Plural': `已将 {0} 个{1}移动到：{2}`,
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,
  'Summary.RegisteredStillUsed.Singular': `仍需要 {0} 个文件`,
  'Summary.RegisteredStillUsed.Plural': `仍需要 {0} 个文件`,
  'Summary.OrphanedToCleanUp.Singular': `{0} 个不需要的文件可清理`,
  'Summary.OrphanedToCleanUp.Plural': `{0} 个不需要的文件可清理`,
  'Summary.MissingFromDisk.Singular': `有 {0} 个已注册文件缺失（并非 InstallerClean 删除）。目前没有问题，但日后修复、更新或卸载该程序时可能会失败。打开“详情”了解该怎么做。`,
  'Summary.MissingFromDisk.Plural': `有 {0} 个已注册文件缺失（并非 InstallerClean 删除）。目前没有问题，但日后修复、更新或卸载这些程序时可能会失败。打开“详情”了解该怎么做。`,
  'Summary.OperationFiles': `{1} 个{2}中的 {0} 个`,
  'Summary.OrphanedWindow': `孤立 {0} 个，被取代 {1} 个，已废弃 {2} 个（{3}）`,
  'Summary.RegisteredWindow.Singular': `{0} 个仍需要的已注册文件（{1}）`,
  'Summary.RegisteredWindow.Plural': `{0} 个仍需要的已注册文件（{1}）`,
  'Confirm.MoveTitle': `移动 {0} 个{1}（{2}）？`,
  'Confirm.MoveDestination': `文件将被移动到：`,
  'Confirm.DeleteTitle': `删除 {0} 个{1}（{2}）？`,
  'Error.AdminRequiredTitle': `访问被拒绝`,
  'Error.AdminRequiredBody': `Windows 拒绝了 InstallerClean 的访问，因此已停止。没有删除任何内容。\n\nInstallerClean 本来就以管理员身份运行，所以再那样启动一次也无济于事。Windows 没有进一步说明是什么拒绝了访问，因此没有具体可以尝试的办法。`,
  'Error.InstallerDbUnavailableTitle': `无法读取 Windows Installer 记录`,
  'Error.ScanFailedTitle': `扫描失败`,
  'Error.InstallerDbEmpty': `Windows Installer 记录返回的内容完全为空：没有任何一个已安装的程序或更新声称拥有缓存的安装文件。在正常工作的电脑上不会出现这种情况（即使是刚装好的 Windows 也会有一些），所以要么记录已损坏，要么无法读取；而一次相信这个结果的扫描，会把 {InstallerFolder} 中的每个文件都错误地判定为孤立。InstallerClean 没有那样做，而是停了下来。没有删除任何内容。`,
  'Error.MsiAccessDenied': `Windows Installer 不允许 InstallerClean 列出已安装的内容。InstallerClean 本来就以管理员身份运行，所以再以管理员身份运行一次也不会有任何改变。没有这份清单，就无法安全地判断哪些缓存文件仍然需要，因此 InstallerClean 停了下来。没有删除任何内容。`,
  'Error.MsiNonSuccess': `Windows Installer 无法向 InstallerClean 提供一份可读的已安装程序清单：连续 {0} 个条目返回时无法读取（最后的错误代码为 {1}）。InstallerClean 没有基于只读到一半的清单继续，而是停了下来。没有删除任何内容。`,
  'Error.InvalidDestinationTitle': `目标无效`,
  'Error.DestinationWriteFailedTitle': `无法写入目标`,
  'Error.MoveFailedTitle': `移动失败`,
  'Error.DeleteFailedTitle': `删除失败`,
  'Error.SettingNotSavedTitle': `设置未保存`,
  'Error.SettingNotSavedBody': `无法保存此更改。下次启动时，InstallerClean 将恢复为之前的设置。`,
  'Error.DestinationInsideInstaller': `目标不能位于 Windows Installer 文件夹内。`,
  'Error.DestinationInSystemFolder': `目标 {0} 解析后位于 Windows 系统文件夹下。请选择 %SystemRoot%、%ProgramFiles% 和 %ProgramData% 之外的路径。`,
  'Error.NotEnoughSpaceTitle': `空间不足`,
  'Error.NotEnoughSpaceBody': `{0} 上的空间不足\n\n所需：{1}\n可用：{2}`,
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
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `Windows 报告了一个文件错误；该文件已留在原处。`,
  'Error.IOFailure.Plural': `Windows 报告了文件错误；这些文件已留在原处。`,
  'Error.UnknownError.Singular': `此文件出了点问题；该文件已留在原处。`,
  'Error.UnknownError.Plural': `这些文件出了点问题；它们已留在原处。`,
  'Error.MoveIntoInstaller': `拒绝将文件移动到 Windows Installer 文件夹（目标：{0}）。`,
  'Error.DestinationNotFullyQualified': `The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\\Backup, or \\\\server\\backup). InstallerClean can't use this one: {0}`,
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
  'UpdateCheck.Failed.Unknown': `检查因未知原因失败。如果您需要报告此问题，详情在 crash.log 中。`,
  'BrowserLaunch.ClipboardOk': `InstallerClean 无法打开您的浏览器。链接已复制到您的剪贴板，您可以自行粘贴：&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean 无法打开您的浏览器，也无法将链接复制到您的剪贴板。链接如下：&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again.`,
  'Error.CannotWriteFolder': `无法写入 {0}。`,
  'Error.NoUniqueFilename': `尝试 10,000 次后仍无法为“{0}”找到唯一的文件名。`,
  'ResultLog.Sending': `正在发送…`,
  'ResultLog.Sent': `谢谢！报告已发送。`,
  'ResultLog.Failed': `发送失败。请稍后重试。`,
  'ResultLog.NothingToSend': `没有可发送的报告。`,
  'ConfirmSendResultLog.Title': `把这个发送吗？`,
  'ConfirmSendResultLog.Reassurance': `它会发送到 nofaff.netlify.app/api/result-log。没有任何内容能识别您或您的机器；它只是让我知道 InstallerClean 是否正常工作，以及[大家释放了多少空间]。`,
  'Automation.ResultLogPreview': `报告预览`,
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean 已在运行。`,
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
  'Display.ElapsedLong.LessThanASecond': `不到一秒`,
  'Display.ElapsedLong.Seconds': `{0:F1} 秒`,
  'Cli.UnknownArgument': `未知参数：“{0}”`,
  'Cli.Cancelling': `正在取消…`,
  'Cli.Cancelled': `已取消。`,
  'Cli.GenericError': `错误：{0}。详情已写入 {1}。`,
  'Cli.GenericError.NoLog': `错误：{0}。无法写入崩溃日志。`,
  'Cli.ScanningInstaller': `正在扫描 {InstallerFolder}…`,
  'Cli.FoundOrphans': `找到 {0} 个{1}，可清理（{2}）。`,
  'Cli.NothingToDo': `无需任何操作。`,
  'Cli.DeletingFiles': `正在删除 {0} 个{1}…`,
  'Cli.DeletedFiles': `Permanently deleted {0} {1}.`,
  'Cli.NoMoveDestination': `错误：未指定移动目标位置。请使用 /m 路径。（在 GUI 中设置的默认位置是按用户保存的，不适用于计划任务或服务账户下的运行。）`,
  'Cli.MoveDestinationInsideInstaller': `错误：目标位置不能位于 Windows Installer 文件夹内。`,
  'Cli.MoveDestinationRelative': `错误：目标位置必须是完整路径。收到：{0}`,
  'Cli.MoveDestinationInSystemFolder': `错误：目标位置 {0} 解析到 Windows 系统文件夹下。请选择 %SystemRoot%、%ProgramFiles% 和 %ProgramData% 之外的路径。`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning.`,
  'Cli.MovingFiles': `正在将 {0} 个{1}移动到 {2}…`,
  'Cli.MovedFiles': `已移动 {0} 个{1}。`,
  'Cli.MutexBlocked': `另一个 InstallerClean 进程正持有单实例锁（GUI 或另一次 CLI 运行）。退出代码 75（暂时性）；稍后可安全重试。`,
  'Cli.EventLogUnavailable': `注意：事件日志写入失败。请检查应用程序日志的权限或组策略。`,
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
  'Cli.Help.Header': `InstallerClean - 清理 {InstallerFolder}`,
  'Cli.Help.Usage': `用法：`,
  'Cli.Help.Help': `  installerclean-cli --help     显示此帮助（也接受 /?、-h）`,
  'Cli.Help.Version': `  installerclean-cli --version  显示版本号（也接受 -v）`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Move to the saved backup folder`,
  'Cli.Help.MovePath': `  installerclean-cli /m 路径    移动到指定路径`,
  'Cli.Help.NoteLine1': `installerclean-cli 是一个真正的控制台进程，在运行结束前会一直`,
  'Cli.Help.NoteLine2': `占用命令提示符；可像对待其他控制台程序那样重定向或通过管道处理其输出。`,
  'Cli.Help.NoteLine3': `GUI 就位于同目录下的 InstallerClean.exe 中。`,
  'Cli.Help.ExitCodesHeader': `退出代码：`,
  'Cli.Help.ExitCodeOk': `  0   成功：已处理每个被标记的文件`,
  'Cli.Help.ExitCodeError': `  1   失败：未处理任何文件（参数错误、扫描失败或所有文件均失败）`,
  'Cli.Help.ExitCodePartial': `  2   部分完成：部分文件已处理，部分失败`,
  'Cli.Help.ExitCodeTransient': `  75  暂时性：临时状况阻止了本次运行（见相关消息）`,
  'Cli.Help.ExitCodeCancelled': `  130 已取消（Ctrl+C）`,
  'Tooltip.ChangeLanguage': `更改语言。程序会重启。`,
  'Automation.ChangeLanguage': `更改语言`,
  'Automation.ChangeLanguage.HelpText': `程序会重启。`,
  'Body.NotScanned.Lead': `尚未扫描。`,
  'Body.NotScanned.Why': `点击“重新扫描”，在 {InstallerFolder} 中查找没有任何程序仍然需要的安装程序文件。`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,
  'Error.ScanCorrelationFailed': `InstallerClean 无法把这次扫描与 Windows Installer 记录对上：Windows 仍然列为需要的每个文件都不在 {InstallerFolder} 中，而文件夹里实际存在的文件又与任何记录都对不上。真实的电脑不会是这个样子，所以这说明读取记录时出了问题，而不是有文件可以安全删除。没有列出任何可清理的内容，也没有删除任何内容。`,
  'Error.CandidateOutsideCache': `此文件不直接位于 Windows Installer 文件夹内；为安全起见已拒绝。`,
  'Completion.ReverifySkipped': `{0} 个{1}已保留在原处，因为这次扫描之后又有程序需要它们了。`,
  'Completion.MoveCancelledSummary': `在您取消前，已移动 {1} 个{2}中的 {0} 个。`,
  'Completion.PermanentDeleteCancelledSummary': `在您取消前，已永久删除 {1} 个{2}中的 {0} 个。`,
  'Body.PendingReboot.Lead': `这些文件现在无法清理。`,
  'Cli.TooManyArguments': `错误：出现意外的多余参数“{0}”。如果移动文件夹的路径中含有空格，请给整个路径加上引号：/m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `保存的默认位置按用户存储；计划任务或 SYSTEM 账户运行需使用 /m 路径。`,
  'Completion.ReverifyIncomplete': `{0} 个{1}已保留在原处，因为重新检查时无法完整读取 Windows Installer 记录。`,
  'Summary.ProgramsUnreadable.Singular': `本次扫描无法读取 {0} 个已安装的程序，因此被取代的补丁已保留。孤立文件不受影响。`,
  'Summary.ProgramsUnreadable.Plural': `本次扫描无法读取 {0} 个已安装的程序，因此被取代的补丁已保留。孤立文件不受影响。`,
  'Error.ScanRecordsUnreadable': `InstallerClean 未能读取到足够的 Windows Installer 记录，无法确定哪些内容仍然需要：已安装程序的清单返回时并不完整，而直接从注册表读取同样的记录也遇到了错误。一个文件可能仅仅因为指明它的那条记录属于读不到的记录之一，就显得像是孤立的，因此 InstallerClean 停了下来。没有删除任何内容。`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer 始终没有发出已安装程序清单结束的信号：InstallerClean 在 {0} 个条目后放弃（最后的错误代码为 {1}）。没有尽头的清单无法信任，因此 InstallerClean 停了下来。没有删除任何内容。`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer 始终没有发出某个程序补丁清单结束的信号：InstallerClean 在 {0} 个条目后放弃（最后的错误代码为 {1}）。没有尽头的清单无法信任，因此 InstallerClean 停了下来。没有删除任何内容。`,
  'UpdateCheck.Status.UpdateAvailable': `{0} 版现已推出。`,
  'Completion.DonateAsk': `很高兴帮上忙。您若有心，这里可以打赏。`,
  'About.Link.Guide': `指南和常见问题`,
  'About.Link.ReportProblem': `报告问题`,
  'About.AutoUpdateCheck': `自动检查更新`,
  'Automation.About.Guide.HelpText': `在浏览器中打开 github 上的 readme。`,
  'Automation.About.ReportProblem.HelpText': `在浏览器中打开 github.com 上的问题追踪页面（Issues）。`,
  'Automation.AutoUpdateCheck.HelpText': `勾选后，InstallerClean 运行时会在 github 上检查是否有更新版本。`,
  'Tooltip.MoveSameDrive': `Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Confirm.DeletePermanently.Plural': `Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the 20 machine-contract Cli.* <data> elements BY NAME (the
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
const parse = (xml) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'));
const written = readFileSync(OUT, 'utf8');
const output = parse(written);
const neutralRequired = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

const missingFromMap = neutralRequired.filter((k) => !(k in MAP));
const strayMapKeys = Object.keys(MAP).filter((k) => !neutral.has(k));
const machineLeaked = [...output.keys()].filter(isMachineCliKey);
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
console.log('machine Cli <data> removed:', cliMachineRemoved, '(expect 20)');
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
if (untranslated.length) {
  const show = untranslated.slice(0, 40).join(', ');
  console.log('!! still English (untranslated), ' + untranslated.length + ': ' + show +
    (untranslated.length > 40 ? ', ...and ' + (untranslated.length - 40) + ' more' : ''));
  if (untranslated.length > 50)
    console.log('   (that is most of the file: this is the untranslated template. Translate the MAP values, then a real miss is listed on its own.)');
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  output.size === neutralRequired.length && cliMachineRemoved === 20 && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
