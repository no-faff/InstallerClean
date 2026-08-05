# InstallerClean in 简体中文 (Simplified Chinese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Simplified Chinese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Simplified Chinese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.zh-Hans.resx`](../../src/InstallerClean.Core/Resources/Strings.zh-Hans.resx), so do not edit it by hand. The Simplified Chinese translation itself lives in [`gen-strings-zh-Hans.mjs`](../../scripts/translations/gen-strings-zh-Hans.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | 简体中文 |
| --- | --- |
| InstallerClean | InstallerClean |
| About | 关于 |
| Registered files that should not be deleted | 不应删除的已注册文件 |
| Unneeded files that are safe to delete | 不需要的文件，可安全删除 |

## Section headings

| English | 简体中文 |
| --- | --- |
| PRODUCTS | 产品 |
| PATCHES | 补丁 |
| PRODUCT DETAILS | 产品详情 |
| BACKUP FOLDER | BACKUP FOLDER |
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
| _Delete permanently | 永久删除(_D) |
| _Done | 完成(_D) |
| Details | 详情 |
| _Buy me a cuppa | 请我喝杯茶(_B) |
| Leave a _star on GitHub | 在 GitHub 上点个星(_S) |
| Apache 2.0 licence | Apache 2.0 许可证 |
| _Move | 移动(_M) |
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
| Open _release page | 打开发布页面(_R) |
| _Re-scan | 重新扫描(_R) |
| _Scan again | 再次扫描(_S) |
| Send report | 发送报告 |
| _Send | 发送(_S) |

## About window

| English | 简体中文 |
| --- | --- |
| Guide and FAQ | 指南和常见问题 |
| Report a problem | 报告问题 |
| Check for updates automatically | 自动检查更新 |

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
| Moving unneeded files... | Moving unneeded files... |
| Deleting unneeded files... | Deleting unneeded files... |
| Move cancelled. {0} of {1} {2} processed. | 移动已取消。{1} 个{2}中已处理 {0} 个。 |
| Delete cancelled. {0} of {1} {2} processed. | 删除已取消。{1} 个{2}中已处理 {0} 个。 |
| Move failed ({0}). Details in {1}. | 移动失败（{0}）。详情见 {1}。 |
| Move failed ({0}). The crash log could not be written. | 移动失败（{0}）。无法写入崩溃日志。 |
| Delete failed ({0}). Details in {1}. | 删除失败（{0}）。详情见 {1}。 |
| Delete failed ({0}). The crash log could not be written. | 删除失败（{0}）。无法写入崩溃日志。 |
| Access denied. Windows refused the scan. | 访问被拒绝。Windows 拒绝了扫描。 |
| Scan failed: couldn't read the Windows Installer records. | 扫描失败：无法读取 Windows Installer 记录。 |
| Scan cancelled. | 扫描已取消。 |
| Ready | 就绪 |
| Scan failed ({0}). Details in {1}. | 扫描失败（{0}）。详情见 {1}。 |
| Scan failed ({0}). The crash log could not be written. | 扫描失败（{0}）。无法写入崩溃日志。 |

## Main screen text

| English | 简体中文 |
| --- | --- |
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | 它们位于 {InstallerFolder}，是在卸载程序（{0}）、新补丁取代旧补丁（{1}）或发布者撤回补丁（{2}）时遗留下来的。InstallerClean 只会列出 Windows 自己报告为不再需要的文件。 |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | 尚未扫描。 |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | 点击“重新扫描”，在 {InstallerFolder} 中查找没有任何程序仍然需要的安装程序文件。 |
| These files can't be cleaned up right now. | 这些文件现在无法清理。 |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. |
| Select a file to view details. | 选择一个文件以查看详情。 |
| Select a product to view details. | 选择一个产品以查看详情。 |
| No metadata available. | 没有可用的元数据。 |
| This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. |
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
| Nothing to clean up in {InstallerFolder} | {InstallerFolder} 中没有需要清理的内容 |
| Scanned {0} {1} in {2} | 扫描了 {0} 个{1}，用时 {2} |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| {0} freed | 已释放 {0} |
| {0} moved | 已移动 {0} |
| Nothing was moved | 没有移动任何文件 |
| Nothing was deleted | 没有删除任何文件 |
| {0} of {1} could not be moved. | {1} 个文件中有 {0} 个无法移动。 |
| {0} of {1} could not be moved. | {1} 个文件中有 {0} 个无法移动。 |
| {0} of {1} could not be deleted. | {1} 个文件中有 {0} 个无法删除。 |
| {0} of {1} could not be deleted. | {1} 个文件中有 {0} 个无法删除。 |
| {0} {1} moved to: {2} | 已将 {0} 个{1}移动到：{2} |
| {0} {1} moved to: {2} | 已将 {0} 个{1}移动到：{2} |
| {0} {1} kept in place, because a program went back to needing what the scan flagged. | {0} 个{1}已保留在原处，因为这次扫描之后又有程序需要它们了。 |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} {1} kept in place, because the Windows Installer records had changed by the final check. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. |
| Moved {0} of {1} {2} before you cancelled. | 在您取消前，已移动 {1} 个{2}中的 {0} 个。 |
| Permanently deleted {0} of {1} {2} before you cancelled. | 在您取消前，已永久删除 {1} 个{2}中的 {0} 个。 |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | 很高兴帮上忙。您若有心，这里可以打赏。 |

## Summaries and counts

| English | 简体中文 |
| --- | --- |
| {0} file still needed | 仍需要 {0} 个文件 |
| {0} files still needed | 仍需要 {0} 个文件 |
| {0} unneeded file to clean up | {0} 个不需要的文件可清理 |
| {0} unneeded files to clean up | {0} 个不需要的文件可清理 |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | 有 {0} 个已注册文件缺失（并非 InstallerClean 删除）。目前没有问题，但日后修复、更新或卸载该程序时可能会失败。打开“详情”了解该怎么做。 |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | 有 {0} 个已注册文件缺失（并非 InstallerClean 删除）。目前没有问题，但日后修复、更新或卸载这些程序时可能会失败。打开“详情”了解该怎么做。 |
| Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
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
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

## Error messages

| English | 简体中文 |
| --- | --- |
| Access denied | 访问被拒绝 |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows 拒绝了 InstallerClean 的访问，因此已停止。没有删除任何内容。<br><br>InstallerClean 本来就以管理员身份运行，所以再那样启动一次也无济于事。Windows 没有进一步说明是什么拒绝了访问，因此没有具体可以尝试的办法。 |
| Couldn't read the Windows Installer records | 无法读取 Windows Installer 记录 |
| Scan failed | 扫描失败 |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Windows Installer 记录返回的内容完全为空：没有任何一个已安装的程序或更新声称拥有缓存的安装文件。在正常工作的电脑上不会出现这种情况（即使是刚装好的 Windows 也会有一些），所以要么记录已损坏，要么无法读取；而一次相信这个结果的扫描，会把 {InstallerFolder} 中的每个文件都错误地判定为孤立。InstallerClean 没有那样做，而是停了下来。没有删除任何内容。 |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer 不允许 InstallerClean 列出已安装的内容。InstallerClean 本来就以管理员身份运行，所以再以管理员身份运行一次也不会有任何改变。没有这份清单，就无法安全地判断哪些缓存文件仍然需要，因此 InstallerClean 停了下来。没有删除任何内容。 |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer 无法向 InstallerClean 提供一份可读的已安装程序清单：连续 {0} 个条目返回时无法读取（最后的错误代码为 {1}）。InstallerClean 没有基于只读到一半的清单继续，而是停了下来。没有删除任何内容。 |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer 始终没有发出已安装程序清单结束的信号：InstallerClean 在 {0} 个条目后放弃（最后的错误代码为 {1}）。没有尽头的清单无法信任，因此 InstallerClean 停了下来。没有删除任何内容。 |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer 始终没有发出某个程序补丁清单结束的信号：InstallerClean 在 {0} 个条目后放弃（最后的错误代码为 {1}）。没有尽头的清单无法信任，因此 InstallerClean 停了下来。没有删除任何内容。 |
| InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean 未能读取到足够的 Windows Installer 记录，无法确定哪些内容仍然需要：已安装程序的清单返回时并不完整，而直接从注册表读取同样的记录也遇到了错误。一个文件可能仅仅因为指明它的那条记录属于读不到的记录之一，就显得像是孤立的，因此 InstallerClean 停了下来。没有删除任何内容。 |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. |
| Nothing was deleted | 没有删除任何文件 |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Invalid destination | 目标无效 |
| Could not write to destination | 无法写入目标 |
| Move failed | 移动失败 |
| Delete failed | 删除失败 |
| Setting not saved | 设置未保存 |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | 无法保存此更改。下次启动时，InstallerClean 将恢复为之前的设置。 |
| The destination cannot be inside the Windows Installer folder. | 目标不能位于 Windows Installer 文件夹内。 |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
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
| This file is not directly inside the Windows Installer folder; refused for safety. | 此文件不直接位于 Windows Installer 文件夹内；为安全起见已拒绝。 |
| Windows refused access to this file; it was left in place. | Windows 拒绝了对此文件的访问；该文件已留在原处。 |
| Windows refused access to these files; they were left in place. | Windows 拒绝了对这些文件的访问；这些文件已留在原处。 |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows 报告了一个文件错误；该文件已留在原处。 |
| Windows reported file errors; these files were left in place. | Windows 报告了文件错误；这些文件已留在原处。 |
| Something went wrong with this file; it was left in place. | 此文件出了点问题；该文件已留在原处。 |
| Something went wrong with these files; they were left in place. | 这些文件出了点问题；它们已留在原处。 |
| Refusing to move files into the Windows Installer folder (destination: {0}). | 拒绝将文件移动到 Windows Installer 文件夹（目标：{0}）。 |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
| Cannot write to {0}. | 无法写入 {0}。 |
| Could not find a unique filename for '{0}' after 10,000 attempts. | 尝试 10,000 次后仍无法为“{0}”找到唯一的文件名。 |

## Update check

| English | 简体中文 |
| --- | --- |
| Check for updates | 检查更新 |
| Checking... | 正在检查… |
| Up to date. | 已是最新版本。 |
| Version {0} is available. | {0} 版现已推出。 |
| Update available | 有可用更新 |
| You're running version {0}.<br>Version {1} is available. | 您正在运行 {0} 版。<br>{1} 版现已推出。 |
| Couldn't reach GitHub. Check your internet connection and try again. | 无法连接到 GitHub。请检查您的网络连接后重试。 |
| GitHub returned an error response. Try again in a few minutes. | GitHub 返回了错误响应。请过几分钟后重试。 |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | GitHub 的响应中没有可识别的发布版本。请稍后重试，或直接打开发布页面。 |
| The check timed out. Your connection to GitHub may be slow; try again. | 检查超时。您与 GitHub 的连接可能较慢；请重试。 |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | 检查因未知原因失败。如果您需要报告此问题，详情在 crash.log 中。 |

## Opening links in your browser

| English | 简体中文 |
| --- | --- |
| Couldn't open your browser | 无法打开您的浏览器 |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean 无法打开您的浏览器。链接已复制到您的剪贴板，您可以自行粘贴：<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean 无法打开您的浏览器，也无法将链接复制到您的剪贴板。链接如下：<br><br>{0} |

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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

## Tooltips (hover text)

| English | 简体中文 |
| --- | --- |
| It's thirsty work! | 该来杯茶了！ |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | 已请求取消。InstallerClean 正在等待当前步骤到达一个可以停下来的位置。在大量 I/O 操作或 MSI 数据库调用期间，这可能需要几秒钟。 |
| Close | 关闭 |
| A star helps other people find it. | 点个星有助于更多人发现 InstallerClean。 |
| Minimise | 最小化 |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | 由您决定，但非常感谢。会发送一份匿名摘要，只是让我知道它是否正常工作，以及大家释放了多少空间。下一个界面会让您在确认前先看到将要发送的内容。 |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | 由您决定，但非常感谢。会发送一份匿名摘要，只是让我知道它是否正常工作。下一个界面会让您在确认前先看到将要发送的内容。 |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | 来自内嵌 Authenticode 证书的使用者名称。未验证证书链。 |
| Change language. The program will restart. | 更改语言。程序会重启。 |

## Screen reader labels

| English | 简体中文 |
| --- | --- |
| Donate | 捐赠 |
| Buy me a cuppa | 请我喝杯茶 |
| Cancel operation | 取消操作 |
| Cancel scan | 取消扫描 |
| Cancel startup scan | 取消启动扫描 |
| Close | 关闭 |
| Close window | 关闭窗口 |
| Close result and return to main window | 关闭结果并返回主窗口 |
| Leave a star on github | 在 github 上点个星 |
| Minimise | 最小化 |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | 移动会将不需要的文件放入所选的目标文件夹。取消则让它们留在原处。 |
| Say thanks | 道声谢 |
| Send posts the report shown to No Faff. Cancel sends nothing. | 发送会将所示报告提交给 No Faff。取消则不发送任何内容。 |
| Check for updates | 检查更新 |
| Checks github's releases page for a newer version. | 在 github 的发布页面上检查是否有更新版本。 |
| Opens the readme on github in your browser. | 在浏览器中打开 github 上的 readme。 |
| Opens the issue tracker on github.com in your browser. | 在浏览器中打开 github.com 上的问题追踪页面（Issues）。 |
| If ticked, InstallerClean checks github for a newer version when you run it. | 勾选后，InstallerClean 运行时会在 github 上检查是否有更新版本。 |
| Open the release page to download the newer version, or cancel to keep the current version. | 打开发布页面以下载更新版本，或取消以保留当前版本。 |
| Opens the licence file on github.com in your browser. | 在浏览器中打开 github.com 上的许可证文件。 |
| Backup folder | Backup folder |
| Products | 产品 |
| Patches | 补丁 |
| Product details | 产品详情 |
| Backup folder | Backup folder |
| Operation progress | 操作进度 |
| Scan {InstallerFolder} again | 重新扫描 {InstallerFolder} |
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
| Product details | Product details |
| Dialog text | 对话框文本 |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | 无法处理的文件 |
| Explains this folder, and how to recover a file, in the README | 在 README 中解释了这个文件夹，以及如何恢复文件 |
| Report preview | 报告预览 |
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
| Error: unknown argument '{0}' | Error: unknown argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | 错误：出现意外的多余参数“{0}”。如果移动文件夹的路径中含有空格，请给整个路径加上引号：/m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. |
| Cancelling... | 正在取消… |
| Cancelled. | 已取消。 |
| Error: unexpected failure ({0}). Details written to {1}. | Error: unexpected failure ({0}). Details written to {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Error: unexpected failure ({0}). The crash log could not be written. |
| Scanning {InstallerFolder}... | 正在扫描 {InstallerFolder}… |
| Found {0} unneeded {1} to clean up ({2}). | Found {0} unneeded {1} to clean up ({2}). |
| Found no unneeded files. | Found no unneeded files. |
| {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. | {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. |
| {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. | {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. |
| Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Deleting {0} unneeded {1}... | Deleting {0} unneeded {1}... |
| Permanently deleted {0} unneeded {1}. | Permanently deleted {0} unneeded {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | 错误：未指定移动目标位置。请使用 /m 路径。（在 GUI 中设置的默认位置是按用户保存的，不适用于计划任务或服务账户下的运行。） |
| Error: destination cannot be inside the Windows Installer folder. | 错误：目标位置不能位于 Windows Installer 文件夹内。 |
| Error: destination must be a fully qualified path. Got: {0} | 错误：目标位置必须是完整路径。收到：{0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Moving {0} unneeded {1} to {2}... | Moving {0} unneeded {1} to {2}... |
| Moved {0} unneeded {1}. | Moved {0} unneeded {1}. |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | 另一个 InstallerClean 进程正持有单实例锁（GUI 或另一次 CLI 运行）。退出代码 75（暂时性）；稍后可安全重试。 |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | 注意：事件日志写入失败。请检查应用程序日志的权限或组策略。 |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - 清理 {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Removes cached .msi and .msp files that no installed program still needs. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Needs an elevated (administrator) prompt; Windows will not start it. |
| Usage: | 用法： |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     显示此帮助（也接受 /?、-h） |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  显示版本号（也接受 -v） |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m 路径    移动到指定路径 |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. |
| Exit codes: | 退出代码： |
|   0   success: the run finished with nothing left to do |   0   success: the run finished with nothing left to do |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partial: some processed, some not (a failure or a Ctrl+C part way) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  暂时性：临时状况阻止了本次运行（见相关消息） |
|   130 cancelled (Ctrl+C) |   130 已取消（Ctrl+C） |
