# InstallerClean in 简体中文 (Simplified Chinese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Simplified Chinese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Simplified Chinese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.zh-Hans.resx`](../../src/InstallerClean.Core/Resources/Strings.zh-Hans.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | 简体中文 |
| --- | --- |
| InstallerClean | InstallerClean |
| About | 关于 |
| Registered files that should not be deleted | 不应删除的已注册文件 |
| Unneeded files that are safe to delete | 不需要的文件，可安全删除 |
| Confirm move | 确认移动 |
| Confirm delete | 确认删除 |
| Recycle Bin unavailable | 回收站不可用 |

## Section headings

| English | 简体中文 |
| --- | --- |
| PRODUCTS | 产品 |
| PATCHES | 补丁 |
| PRODUCT DETAILS | 产品详情 |
| MOVE LOCATION | 移动位置 |
| SAY THANKS | 道声谢 |

## Buttons and actions

| English | 简体中文 |
| --- | --- |
| _About | 关于(_A) |
| Copy | 复制 |
| Cut | 剪切 |
| Paste | 粘贴 |
| Select all | 全选 |
| _Browse... | 浏览(_B)… |
| _Cancel | 取消(_C) |
| Check for _updates | 检查更新(_U) |
| _Close | 关闭(_C) |
| _Delete | 删除(_D) |
| _Delete permanently | 永久删除(_D) |
| _Done | 完成(_D) |
| Details | 详情 |
| _Buy me a cuppa | 请我喝杯茶(_B) |
| Leave a _star on GitHub | 在 GitHub 上点个星(_S) |
| MIT licence | MIT 许可证 |
| _Move | 移动(_M) |
| _Move instead | 改为移动(_M) |
| Path to folder if you Move instead of Delete | 文件夹路径（若选择移动而非删除） |
| Open _release page | 打开发布页面(_R) |
| _Re-scan | 重新扫描(_R) |
| _Scan again | 再次扫描(_S) |
| Send report | 发送报告 |
| _Send | 发送(_S) |

## Field labels

| English | 简体中文 |
| --- | --- |
| Reason | 原因 |
| Author | 作者 |
| Application | 应用程序 |
| Title | 标题 |
| Subject | 主题 |
| Keywords | 关键字 |
| Signing certificate | 签名证书 |
| File size | 文件大小 |
| Comment | 备注 |
| Product name | 产品名称 |
| File | 文件 |
| Size | 大小 |
| Patches | 补丁 |
| (unknown) | （未知） |
| (patches only) | （仅补丁） |
| missing | 缺失 |

## Status and progress

| English | 简体中文 |
| --- | --- |
| Scanning... | 正在扫描… |
| Cancelling... | 正在取消… |
| Starting scan... | 正在开始扫描… |
| Asking Windows about installed software... | 正在向 Windows 查询已安装的软件… |
| Scanning installer cache folder... | 正在扫描安装程序缓存文件夹… |
| Enumerating installed products... | 正在枚举已安装的产品… |
| Checking registry for additional packages... | 正在检查注册表中的其他程序包… |
| Found {0} registered {1}. | 找到 {0} 个已注册的{1}。 |
| Scan complete ({0}) | 扫描完成（{0}） |
| Scanning local packages... | 正在扫描本地程序包… |
| Found {0} {1} you can safely delete. | 找到 {0} 个{1}，可安全删除。 |
| Preparing destination folder... | 正在准备目标文件夹… |
| Moving {0} {1}... | 正在移动 {0} 个{1}… |
| Deleting {0} {1}... | 正在删除 {0} 个{1}… |
| Move cancelled. {0} of {1} {2} processed. | 移动已取消。{1} 个{2}中已处理 {0} 个。 |
| Delete cancelled. {0} of {1} {2} processed. | 删除已取消。{1} 个{2}中已处理 {0} 个。 |
| Move failed ({0}). Details in {1}. | 移动失败（{0}）。详情见 {1}。 |
| Move failed ({0}). The crash log could not be written. | 移动失败（{0}）。无法写入崩溃日志。 |
| Delete failed ({0}). Details in {1}. | 删除失败（{0}）。详情见 {1}。 |
| Delete failed ({0}). The crash log could not be written. | 删除失败（{0}）。无法写入崩溃日志。 |
| Access denied. Windows refused the scan. | Access denied. Windows refused the scan. |
| Scan failed: installer database unavailable. | 扫描失败：安装程序数据库不可用。 |
| Scan cancelled. | 扫描已取消。 |
| Ready | 就绪 |
| Scan failed ({0}). Details in {1}. | 扫描失败（{0}）。详情见 {1}。 |
| Scan failed ({0}). The crash log could not be written. | 扫描失败（{0}）。无法写入崩溃日志。 |

## Main screen text

| English | 简体中文 |
| --- | --- |
| The unneeded files below are safe to delete. | 下面这些不需要的文件可以安全删除。 |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | 它们位于 C:\Windows\Installer，是在卸载程序（{0}）、新补丁取代旧补丁（{1}）或发布者撤回补丁（{2}）时遗留下来的。InstallerClean 只会列出 Windows 自己报告为已经用完的文件。 |
| Delete them to the Recycle Bin, or use Move instead if you'd rather keep a copy. | 将它们删除到回收站，如果您想保留副本，也可以改用“移动”。 |
| Nothing scanned yet. | Nothing scanned yet. |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | 现在有程序正在使用 Windows Installer，通常是 Windows 更新或某个正在后台安装的程序。在此期间，移动和删除会暂停，这样 InstallerClean 就不会在安装程序缓存发生变化时去动它。等它完成后，重新扫描，这两项操作便会恢复。 |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | 此计算机上有一个先前的 Windows Installer 事务处于挂起状态。请先继续或回滚该安装（或重启 Windows），再清理缓存。 |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows 已排队一项将在下次重启时执行的文件重命名操作，会影响安装程序缓存。请先重启 Windows，再进行清理。 |
| Select a file to view details. | 选择一个文件以查看详情。 |
| Select a product to view details. | 选择一个产品以查看详情。 |
| No metadata available. | 没有可用的元数据。 |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | 这个安装程序文件已被删除。这不是 InstallerClean 干的，它从不删除程序仍然需要的文件；是在您运行 InstallerClean 之前，别的东西删掉了它。<br><br>现在它不会造成任何麻烦，直到有一天您尝试修复、更新或卸载它所属的程序时才会显现。那一步可能会失败，因为 Windows 会去找这个文件，却找不到。<br><br>要尝试修复，请从该程序的厂商处下载它的安装程序，在您现有的安装之上运行一遍（不要先卸载，卸载本身就是一个需要这个文件的步骤）。如果能找到，请使用您已安装的那个版本，因为 Windows 可能会拒绝其他版本。这通常会把文件恢复回来，您的设置一般也不受影响，但 Microsoft 并不保证，它自己的最后手段是重新安装该程序，或重装 Windows 本身。 |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README 用 Microsoft 自己的原话[解释了这个文件夹]，以及如何恢复文件。 |
| (none) | （无） |

## Reasons a file is unneeded

| English | 简体中文 |
| --- | --- |
| Orphaned | 孤立 |
| Superseded | 被取代 |
| Obsoleted | 已废弃 |

## Completion screen

| English | 简体中文 |
| --- | --- |
| All clean | 全部干净 |
| Nothing to clean up in C:\Windows\Installer | C:\Windows\Installer 中没有需要清理的内容 |
| Scanned {0} {1} in {2} | 扫描了 {0} 个{1}，用时 {2} |
| Copy them back if anything breaks ([it won't!]). | 万一出了什么问题，把它们复制回原处（[不会的！]）。 |
| Until then, you can restore them if anything breaks ([it won't!]). | 在那之前，万一出了什么问题，您可以把它们还原（[不会的！]）。 |
| Empty it to actually reclaim the space. | 清空回收站才能真正释放空间。 |
| {0} freed | 已释放 {0} |
| {0} cleaned up | 已清理 {0} |
| {0} moved | 已移动 {0} |
| {0} moved, some files could not be processed | 已移动 {0}，部分文件无法处理 |
| {0} freed, some files could not be processed | 已释放 {0}，部分文件无法处理 |
| {0} cleaned up, some files could not be processed | 已清理 {0}，部分文件无法处理 |
| {0} {1} moved to: {2} | 已将 {0} 个{1}移动到：{2} |
| {0} {1} moved to: {2} | 已将 {0} 个{1}移动到：{2} |
| {0} {1} moved to: {2}. {3} {4} | 已将 {0} 个{1}移动到：{2}。{3} 个{4} |
| {0} {1} moved to: {2}. {3} {4} | 已将 {0} 个{1}移动到：{2}。{3} 个{4} |
| {0} {1} moved to the Recycle Bin | 已将 {0} 个{1}移到回收站 |
| {0} {1} moved to the Recycle Bin | 已将 {0} 个{1}移到回收站 |
| {0} {1} moved to the Recycle Bin. {2} {3} | 已将 {0} 个{1}移到回收站。{2} 个{3} |
| {0} {1} moved to the Recycle Bin. {2} {3} | 已将 {0} 个{1}移到回收站。{2} 个{3} |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | 已永久删除 {0} 个{1}。它没有进入回收站。 |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | 已永久删除 {0} 个{1}。它们没有进入回收站。 |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. {2} {3} | 已永久删除 {0} 个{1}。它没有进入回收站。{2} 个{3} |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. {2} {3} | 已永久删除 {0} 个{1}。它们没有进入回收站。{2} 个{3} |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | 没关系，它本来就可以安全删除。InstallerClean 只清除 Windows 报告为已经用完的文件，绝不会删除程序仍然需要的文件。万一某次删除真的让某个程序无法修复、更新或卸载，从其厂商处重新安装通常就能把文件恢复回来，不过 Microsoft 并不保证这一点。 |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | 没关系，它们本来就可以安全删除。InstallerClean 只清除 Windows 报告为已经用完的文件，绝不会删除程序仍然需要的文件。万一某次删除真的让某个程序无法修复、更新或卸载，从其厂商处重新安装通常就能把文件恢复回来，不过 Microsoft 并不保证这一点。 |

## Recycle Bin unavailable

| English | 简体中文 |
| --- | --- |
| The Recycle Bin isn't available for this drive | 此驱动器的回收站不可用 |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | 所以这个{1}（{2}）还没有被删除。您可以把它移动到安全的位置，或将它永久删除。 |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | 所以这 {0} 个{1}（{2}）还没有被删除。您可以把它们移动到安全的位置，或将它们永久删除。 |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | 删除它是安全的。InstallerClean 只清除 Windows 报告为已经用完的文件，绝不会删除程序仍然需要的文件，回收站只是一道额外的保险。万一某次删除真的让某个程序无法修复、更新或卸载，从其厂商处重新安装通常就能把文件恢复回来，不过 Microsoft 并不保证这一点。 |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | 删除它们是安全的。InstallerClean 只清除 Windows 报告为已经用完的文件，绝不会删除程序仍然需要的文件，回收站只是一道额外的保险。万一某次删除真的让某个程序无法修复、更新或卸载，从其厂商处重新安装通常就能把文件恢复回来，不过 Microsoft 并不保证这一点。 |

## Summaries and counts

| English | 简体中文 |
| --- | --- |
| {0} file still needed | 仍需要 {0} 个文件 |
| {0} files still needed | 仍需要 {0} 个文件 |
| {0} unneeded file to clean up | {0} 个不需要的文件可清理 |
| {0} unneeded files to clean up | {0} 个不需要的文件可清理 |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | 有 {0} 个已注册文件缺失（并非 InstallerClean 删除）。目前没有问题，但日后修复、更新或卸载该程序时可能会失败。打开“详情”了解该怎么做。 |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | 有 {0} 个已注册文件缺失（并非 InstallerClean 删除）。目前没有问题，但日后修复、更新或卸载这些程序时可能会失败。打开“详情”了解该怎么做。 |
| {0} stale MSI entry detected (file already gone from disk; InstallerClean doesn't unregister it). | 检测到 {0} 个过时的 MSI 条目（文件已从磁盘上消失；InstallerClean 不会取消它的注册）。 |
| {0} stale MSI entries detected (files already gone from disk; InstallerClean doesn't unregister them). | 检测到 {0} 个过时的 MSI 条目（文件已从磁盘上消失；InstallerClean 不会取消它们的注册）。 |
| {0} of {1} {2} | {1} 个{2}中的 {0} 个 |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | 孤立 {0} 个，被取代 {1} 个，已废弃 {2} 个（{3}） |
| {0} registered file that is still needed ({1}) | {0} 个仍需要的已注册文件（{1}） |
| {0} registered files that are still needed ({1}) | {0} 个仍需要的已注册文件（{1}） |

## Confirmation dialogs

| English | 简体中文 |
| --- | --- |
| Move {0} {1} ({2})? | 移动 {0} 个{1}（{2}）？ |
| Files will be moved to: | 文件将被移动到： |
| Delete {0} {1} ({2})? | 删除 {0} 个{1}（{2}）？ |
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | 文件将被移到回收站。如果您想要备份副本，请改用“移动”按钮。 |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. |

## Error messages

| English | 简体中文 |
| --- | --- |
| Access denied | Access denied |
| Windows refused InstallerClean access. InstallerClean is already running as administrator, so starting it again that way won't help.<br><br>That leaves two likely causes: security software is holding C:\Windows\Installer, or the folder's permissions have been changed. Pausing the security software and trying again is the quickest one to rule out. | Windows refused InstallerClean access. InstallerClean is already running as administrator, so starting it again that way won't help.<br><br>That leaves two likely causes: security software is holding C:\Windows\Installer, or the folder's permissions have been changed. Pausing the security software and trying again is the quickest one to rule out. |
| Installer database unavailable | 安装程序数据库不可用 |
| Scan failed | 扫描失败 |
| The Windows Installer database appears to be empty or inaccessible. This is unusual even on a fresh Windows install and typically means the database is corrupt or a third-party tool has cleared it. Running 'sfc /scannow' from an elevated prompt usually repairs it. | Windows Installer 数据库似乎为空或无法访问。即便在全新安装的 Windows 上这也很不寻常，通常意味着数据库已损坏，或被某个第三方工具清空了。在提权的命令提示符中运行“sfc /scannow”通常可以修复它。 |
| Windows Installer refused to list the installed products, and InstallerClean is already running as administrator, so running it again won't help. The permissions on Windows's own installer records may have been changed, or security software may be blocking them. Running 'sfc /scannow' from an elevated prompt is worth a try. | Windows Installer refused to list the installed products, and InstallerClean is already running as administrator, so running it again won't help. The permissions on Windows's own installer records may have been changed, or security software may be blocking them. Running 'sfc /scannow' from an elevated prompt is worth a try. |
| Windows Installer refused to list products after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | 在连续 {0} 次失败后，Windows Installer 拒绝列出产品（最后的错误代码为 {1}）。请尝试重启 Windows，或在提权的命令提示符中运行“sfc /scannow”。 |
| Windows Installer refused to list a product's patches after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | 在连续 {0} 次失败后，Windows Installer 拒绝列出某个产品的补丁（最后的错误代码为 {1}）。请尝试重启 Windows，或在提权的命令提示符中运行“sfc /scannow”。 |
| Invalid destination | 目标无效 |
| Could not write to destination | 无法写入目标 |
| Move failed | 移动失败 |
| Delete failed | 删除失败 |
| The destination cannot be inside the Windows Installer folder. | 目标不能位于 Windows Installer 文件夹内。 |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | 目标 {0} 解析后位于 Windows 系统文件夹下。请选择 %SystemRoot%、%ProgramFiles% 和 %ProgramData% 之外的路径。 |
| Not enough space | 空间不足 |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | {0} 上的空间不足<br><br>所需：{1}<br>可用：{2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | 您没有写入 {0} 的权限。<br>请尝试您的用户配置文件中的文件夹，或您拥有的驱动器。 |
| The path {0} is too long for Windows. Pick a shorter path. | 路径 {0} 对 Windows 来说太长了。请选择更短的路径。 |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | 文件夹 {0} 不存在，且无法创建。请检查驱动器盘符或网络路径。 |
| Windows cannot write to {0}.<br>Details in {1}. | Windows 无法写入 {0}。<br>详情见 {1}。 |
| Windows cannot write to {0}. The crash log could not be written. | Windows 无法写入 {0}。无法写入崩溃日志。 |
| Cannot write to {0}.<br>Details in {1}. | 无法写入 {0}。<br>详情见 {1}。 |
| Cannot write to {0}. The crash log could not be written. | 无法写入 {0}。无法写入崩溃日志。 |
| File no longer exists. | 文件已不存在。 |
| Source file is a symlink or junction; refused for safety. | 源文件是符号链接或目录联接；为安全起见已拒绝。 |
| Access denied. | 访问被拒绝。 |
| The operation failed. Try again or restart Windows. | 操作失败。请重试或重启 Windows。 |
| Unknown error. | 未知错误。 |
| Couldn't move this file to the Recycle Bin (error {0}). It may be locked, in use or blocked by Windows. Use the Move button instead. | 无法将此文件移到回收站（错误 {0}）。它可能被锁定、正在使用，或被 Windows 阻止。请改用“移动”按钮。 |
| Windows blocked access to this file, even with administrator rights (error {0}). It is usually an ownership or permissions lock. Use the Move button instead. | 即使拥有管理员权限，Windows 仍阻止了对此文件的访问（错误 {0}）。这通常是所有权或权限锁定。请改用“移动”按钮。 |
| This file is open or locked by another program (error {0}). Close that program, or whatever is scanning it, then try again, or use the Move button instead. | 此文件正被另一个程序打开或锁定（错误 {0}）。请关闭那个程序，或任何正在扫描它的程序，然后重试，或改用“移动”按钮。 |
| The file was permanently deleted because it could not be moved to the Recycle Bin. | 由于无法将该文件移到回收站，它已被永久删除。 |
| Refusing to move files into the Windows Installer folder (destination: {0}). | 拒绝将文件移动到 Windows Installer 文件夹（目标：{0}）。 |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
| Cannot write to {0}. | 无法写入 {0}。 |
| Could not find a unique filename for '{0}' after 10,000 attempts. | 尝试 10,000 次后仍无法为“{0}”找到唯一的文件名。 |

## Update check

| English | 简体中文 |
| --- | --- |
| Check for updates | 检查更新 |
| Checking... | 正在检查… |
| Up to date. | 已是最新版本。 |
| Update available | 有可用更新 |
| You're running version {0}.<br>Version {1} is available. | 您正在运行 {0} 版。<br>{1} 版现已推出。 |
| Couldn't reach GitHub. Check your internet connection and try again. | 无法连接到 GitHub。请检查您的网络连接后重试。 |
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | GitHub 返回了错误响应。发布 API 可能受到了速率限制；请过几分钟后重试。 |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | GitHub 的响应中没有可识别的发布版本。请稍后重试，或直接打开发布页面。 |
| The check timed out. Your connection to GitHub may be slow; try again. | 检查超时。您与 GitHub 的连接可能较慢；请重试。 |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | 检查因未知原因失败。如果您需要报告此问题，详情在 crash.log 中。 |

## Opening links in your browser

| English | 简体中文 |
| --- | --- |
| Couldn't open your browser | 无法打开您的浏览器 |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} |

## Sending the summary

| English | 简体中文 |
| --- | --- |
| Sending... | 正在发送… |
| Thanks! Report sent. | 谢谢！报告已发送。 |
| Sending failed. Try again later. | 发送失败。请稍后重试。 |
| No report to send. | 没有可发送的报告。 |
| Send this? | 把这个发送吗？ |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | 它会发送到 nofaff.netlify.app/api/result-log。没有任何内容能识别您或您的机器；它只是让我知道 InstallerClean 是否正常工作，以及[大家释放了多少空间]。 |

## Startup and crashes

| English | 简体中文 |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean 已在运行。 |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | 发生了意外错误，InstallerClean 需要关闭。<br><br>{0}<br><br>详情已写入：<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | 发生了意外错误，InstallerClean 需要关闭。<br><br>{0}<br><br>无法写入崩溃日志。 |
| Startup error | 启动错误 |
| Failed to start ({0}). Details written to:<br>{1} | 启动失败（{0}）。详情已写入：<br>{1} |
| Failed to start ({0}). The crash log could not be written. | 启动失败（{0}）。无法写入崩溃日志。 |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log 记录 InstallerClean 未处理的异常。<br># 在提权运行时，框架的异常消息可能包含当前会话的文件<br># 路径（包括 Windows Installer 查询所枚举到的其他用户的<br># 配置文件）。来自检查更新或结果日志 POST 的网络失败<br># 消息，可能包含目标 URL 以及解析出的 IP / 代理地址。<br># 在将此文件附加到公开的错误报告之前，请先删除这两类<br># 细节。<br> |

## Tooltips (hover text)

| English | 简体中文 |
| --- | --- |
| Donate | 捐赠 |
| It's thirsty work! | 该来杯茶了！ |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | 已请求取消。InstallerClean 正在等待当前步骤到达一个可以停下来的位置。在大量 I/O 操作或 MSI 数据库调用期间，这可能需要几秒钟。 |
| Close | 关闭 |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | 在 GitHub 上点个星、提交 Issue 或在 Discussions 中发帖。欢迎任何反馈。 |
| or report an Issue or post in Discussions. Any feedback welcome. | 或提交 Issue，或在 Discussions 中发帖。欢迎任何反馈。 |
| Minimise | 最小化 |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | 由您决定，但非常感谢。会发送一份匿名摘要，只是让我知道它是否正常工作，以及大家释放了多少空间。下一个界面会让您在确认前先看到将要发送的内容。 |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | 由您决定，但非常感谢。会发送一份匿名摘要，只是让我知道它是否正常工作。下一个界面会让您在确认前先看到将要发送的内容。 |
| Move the unneeded files to the Move location. | 将不需要的文件移动到移动位置。 |
| Move the unneeded files to the Move location. Choose one first. | 将不需要的文件移动到移动位置。请先选择一个位置。 |
| Move the unneeded files to the Recycle Bin. | 将不需要的文件移到回收站。 |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | 来自内嵌 Authenticode 证书的主题名称。未验证证书链。 |
| Change language. The program will restart. | 更改语言。程序会重启。 |

## Screen reader labels

| English | 简体中文 |
| --- | --- |
| Donate | 捐赠 |
| Buy me a cuppa (About window) | 请我喝杯茶（关于窗口） |
| Cancel operation | 取消操作 |
| Cancel scan | 取消扫描 |
| Cancel startup scan | 取消启动扫描 |
| Close | 关闭 |
| Close window | 关闭窗口 |
| Close result and return to main window | 关闭结果并返回主窗口 |
| Leave a star on GitHub | 在 GitHub 上点个星 |
| Leave a star on GitHub (About window) | 在 GitHub 上点个星（关于窗口） |
| Minimise | 最小化 |
| Move all unneeded installer files to the chosen destination folder | 将所有不需要的安装程序文件移动到所选的目标文件夹 |
| Move all unneeded installer files to the Recycle Bin | 将所有不需要的安装程序文件移到回收站 |
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | 删除会将不需要的文件移到回收站。取消则关闭且不删除。 |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | 移动会将不需要的文件放入所选的目标文件夹。取消则让它们留在原处。 |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | 选择如何处理这些不需要的文件：移动到安全的位置、永久删除或取消。 |
| Move the unneeded files to a folder you choose | 将不需要的文件移动到您选择的文件夹 |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | 由于此驱动器的回收站不可用，永久删除这些不需要的文件 |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | 发送到 nofaff.netlify.app。仅包含计数和标签。发送前您会看到完整的内容。 |
| Say thanks | 道声谢 |
| Send posts the report shown to No Faff. Cancel sends nothing. | 发送会将所示报告提交给 No Faff。取消则不发送任何内容。 |
| Check for updates | 检查更新 |
| Checks the GitHub releases API over HTTPS for a newer version. | 通过 HTTPS 查询 GitHub 发布 API，检查是否有更新版本。 |
| Open the release page to download the newer version, or cancel to keep the current version. | 打开发布页面以下载更新版本，或取消以保留当前版本。 |
| MIT licence | MIT 许可证 |
| Opens the licence file on github.com in your browser. | 在浏览器中打开 github.com 上的许可证文件。 |
| Move location | 移动位置 |
| Products | 产品 |
| Patches | 补丁 |
| Product details | 产品详情 |
| Move destination folder | 移动目标文件夹 |
| Operation progress | 操作进度 |
| Scan C:\Windows\Installer again | 重新扫描 C:\Windows\Installer |
| Scanning progress | 扫描进度 |
| Startup scan progress | 启动扫描进度 |
| Details, unneeded files | 详情，不需要的文件 |
| Available for cleanup. | 可供清理。 |
| Details, registered files | 详情，已注册文件 |
| Read-only inventory. | 只读清单。 |
| Sorted by {0}, ascending | 已按{0}升序排序 |
| Sorted by {0}, descending | 已按{0}降序排序 |
| Scan results | 扫描结果 |
| Result details | 结果详情 |
| File details | 文件详情 |
| Dialog text | 对话框文本 |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | 无法处理的文件 |
| Explains this folder, and how to recover a file, in the README | 在 README 中解释了这个文件夹，以及如何恢复文件 |
| Result log preview | 结果日志预览 |
| Change language | 更改语言 |
| The program will restart. | 程序会重启。 |

## File picker

| English | 简体中文 |
| --- | --- |
| Choose destination folder for moved files | 为移动的文件选择目标文件夹 |

## Version

| English | 简体中文 |
| --- | --- |
| Version {0} | 版本 {0} |

## Word forms (singular and plural)

| English | 简体中文 |
| --- | --- |
| file | 文件 |
| files | 文件 |
| error | 错误 |
| errors | 错误 |
| package | 程序包 |
| packages | 程序包 |
| product | 产品 |
| products | 产品 |
| patch | 补丁 |
| patches | 补丁 |

## Sizes and times

| English | 简体中文 |
| --- | --- |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | 不到一秒 |
| {0:F1} seconds | {0:F1} 秒 |

## Command-line tool (installerclean-cli)

| English | 简体中文 |
| --- | --- |
| Unknown argument: '{0}' | 未知参数：'{0}' |
| Cancelling... | 正在取消… |
| Cancelled. | 已取消。 |
| Error: {0}. Details written to {1}. | 错误：{0}。详情已写入 {1}。 |
| Error: {0}. The crash log could not be written. | 错误：{0}。无法写入崩溃日志。 |
| Scanning C:\Windows\Installer... | 正在扫描 C:\Windows\Installer… |
| Found {0} {1} to clean up ({2}). | 找到 {0} 个{1}，可清理（{2}）。 |
| Nothing to do. | 无需任何操作。 |
| Deleting {0} {1}... | 正在删除 {0} 个{1}… |
| Deleted {0} {1}. | 已删除 {0} 个{1}。 |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | 错误：此驱动器的回收站不可用，因此未删除任何内容。请改用 /m 移动这些文件，或重新启用回收站后再次运行。 |
| Error: no move destination specified. Use /m PATH or set a default in the GUI. | 错误：未指定移动位置。请使用 /m 路径，或在 GUI 中设置默认位置。 |
| Error: destination cannot be inside the Windows Installer folder. | 错误：目标位置不能位于 Windows Installer 文件夹内。 |
| Error: destination must be a fully qualified path. Got: {0} | 错误：目标位置必须是完整路径。收到：{0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | 错误：目标位置 {0} 解析到 Windows 系统文件夹下。请选择 %SystemRoot%、%ProgramFiles% 和 %ProgramData% 之外的路径。 |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | 错误：当前有程序正在使用 Windows Installer，通常是 Windows 更新或正在后台安装的程序。在其运行期间，移动和删除均被阻止。请在它完成后重试。 |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | 错误：本机有一个先前的 Windows Installer 事务处于挂起状态。请在清理缓存前，恢复或回滚该安装（或重启 Windows）。 |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | 错误：有一个排队等待重启后执行的文件操作指向安装程序缓存（{0}）。请先重启 Windows 完成该操作，然后再清理。 |
| Moving {0} {1} to {2}... | 正在将 {0} 个{1}移动到 {2}… |
| Moved {0} {1}. | 已移动 {0} 个{1}。 |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | 另一个 InstallerClean 进程正持有单实例锁（GUI 或另一次 CLI 运行）。退出码 75（暂时性）；稍后可安全重试。 |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | 注意：事件日志写入失败。请检查应用程序日志的权限或组策略。 |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - 清理 C:\Windows\Installer |
| Usage: | 用法： |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     显示此帮助（也接受 /?、-h） |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  显示版本号（也接受 -v） |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s         仅扫描 - 列出不需要的文件 |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d         删除不需要的文件（回收站） |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m         移动到已保存的默认位置 |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m 路径      移动到指定路径 |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli 是一个真正的控制台进程，在运行结束前会一直 |
| until it finishes; redirect or pipe its output as you would any | 占用命令提示符；可像对待其他控制台程序那样重定向或通过管道处理其输出。 |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | GUI 就位于同目录下的 InstallerClean.exe 中。 |
| Exit codes: | 退出码： |
|   0   success: every flagged file was processed |   0   成功：已处理每个被标记的文件 |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   失败：未处理任何文件（参数错误、扫描失败或所有文件均失败） |
|   2   partial: some files processed, some failed |   2   部分完成：部分文件已处理，部分失败 |
|   75  transient: a temporary condition blocked the run (see the message) |   75  暂时性：临时状况阻止了本次运行（见相关消息） |
|   130 cancelled (Ctrl+C) |   130 已取消（Ctrl+C） |
