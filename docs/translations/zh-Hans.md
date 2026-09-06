# InstallerClean in 简体中文 (Simplified Chinese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Simplified Chinese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Simplified Chinese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.zh-Hans.resx`](../../src/InstallerClean.Core/Resources/Strings.zh-Hans.resx), so do not edit it by hand. The Simplified Chinese translation itself lives in [`gen-strings-zh-Hans.mjs`](../../scripts/translations/gen-strings-zh-Hans.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | 简体中文 |
| --- | --- |
| InstallerClean | InstallerClean |
| About | 关于 |
| Files left alone | 原样保留的文件 |
| Unneeded files that are safe to delete | 不需要的文件，可安全删除 |

## Section headings

| English | 简体中文 |
| --- | --- |
| PATCHES | 补丁 |
| PRODUCT DETAILS | 产品详情 |
| BACKUP FOLDER | 备份文件夹 |
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
| Path to folder if you move rather than delete. | 若选择移动而非删除，此处填写文件夹路径。 |
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
| (no program) | （无程序） |
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
| Moving unneeded files... | 正在移动不需要的文件… |
| Deleting unneeded files... | 正在删除不需要的文件… |
| Move cancelled. {0} of {1} {2} processed. | 移动已取消。{1} 个{2}中已处理 {0} 个。 |
| Delete cancelled. {0} of {1} {2} processed. | 删除已取消。{1} 个{2}中已处理 {0} 个。 |
| {0}. Details are in {1}. | {0}。详情见 {1}。 |
| {0}. The crash log could not be written. | {0}。无法写入崩溃日志。 |
| {0}. Details are in {1}. | {0}。详情见 {1}。 |
| {0}. The crash log could not be written. | {0}。无法写入崩溃日志。 |
| Access denied. Windows refused the scan. | 访问被拒绝。Windows 拒绝了扫描。 |
| Scan failed: couldn't read the Windows Installer records. | 扫描失败：无法读取 Windows Installer 记录。 |
| Scan cancelled. | 扫描已取消。 |
| Ready | 就绪 |
| Scan failed ({0}). Details in {1}. | 扫描失败（{0}）。详情见 {1}。 |
| Scan failed ({0}). The crash log could not be written. | 扫描失败（{0}）。无法写入崩溃日志。 |

## Main screen text

| English | 简体中文 |
| --- | --- |
| Any unneeded files below are [safe to delete]. | 下面这些不需要的文件都[可以安全删除]。 |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | 它们位于 {InstallerFolder} 中。InstallerClean 会就每个已安装的程序询问 Windows：当没有任何程序认领某个文件时（{0}），或者当更新的补丁已经取代了它、并且没有任何程序能够回退到它时（{1}），该文件才会列出。 |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | 把它们移动到您选择的备份文件夹，等您确信自己的程序仍能照常更新和卸载时，再删除那个文件夹。把它们放回 {InstallerFolder} 就能恢复原状。或者现在就永久删除。 |
| Nothing scanned yet. | 尚未扫描。 |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | 点击“重新扫描”，在 {InstallerFolder} 中查找没有任何程序仍然需要的安装程序文件。 |
| These files can't be cleaned up right now. | 这些文件现在无法清理。 |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | 此刻有程序正在使用 Windows Installer，比如 Windows 更新，或者某个正在后台安装的程序。在此期间，移动和删除会暂停，这样 InstallerClean 就不会在 {InstallerFolder} 变动时去碰它。等结束后重新扫描，两者就会恢复。 |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | 这台计算机上有一个先前的 Windows Installer 事务处于挂起状态。请先继续或回滚那次安装（或重启 Windows），再清理 {InstallerFolder}。 |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows 已把一次文件重命名排入下次重启的队列，且会影响 {InstallerFolder}。请先重启 Windows 再清理。 |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | 有一项文件操作已排入下次重启的队列，InstallerClean 无法得知它指名了哪些文件，因此无法排除这些文件位于 {InstallerFolder} 的可能。请在清理前重启 Windows。 |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean 无法读取它在碰 {InstallerFolder} 之前会检查的一项 Windows 设置，因此无法判断是否有安装程序操作正在进行或正在等待重启。请重启 Windows 并重新扫描。如果该设置仍然无法读取，这台机器就不是 InstallerClean 能清理的。 |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer 有操作正在进行，因此移动和删除已暂停。InstallerClean 不会在 {InstallerFolder} 变动时去碰它。等结束后重新扫描，两者就会恢复。 |
| Select a file to view details. | 选择一个文件以查看详情。 |
| Select a product to view details. | 选择一个产品以查看详情。 |
| No metadata available. | 没有可用的元数据。 |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | 这个安装文件不见了。现在不会造成任何麻烦，直到有一天您尝试更新或卸载它所属的程序为止。到那时这一步可能会失败，因为 Windows 会寻找这个文件而找不到它。<br><br>要把它放回去，您需要您当前所用版本的安装程序。请从程序的制作方获取，并在现有副本上运行它。更新的版本不行：新版本必须先移除您现有的版本，而正是这一步需要这个文件。先卸载同样行不通，原因相同。这应当会恢复该文件并保持您的设置不变，但 Microsoft 并不保证。 |
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
| Nothing offered on this PC | 在这台电脑上没有提供任何内容 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供那 1 个文件（{2}），而是把它保留了下来。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} {1} ({2}) rather than offering them. | InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供全部 {0} 个{1}（{2}），而是把它们保留了下来。 |
| InstallerClean couldn't establish that the cached file it found is unneeded, so it has held back the one file ({2}) rather than offering it. | InstallerClean 无法证实它找到的那个缓存文件是不需要的，因此没有提供那 1 个文件（{2}），而是把它保留了下来。 |
| InstallerClean couldn't establish that any of the cached files it found are unneeded, so it has held back all {0} {1} ({2}) rather than offering them. | InstallerClean 无法证实它找到的缓存文件中有任何一个是不需要的，因此没有提供全部 {0} 个{1}（{2}），而是把它们保留了下来。 |
| Delete that folder when you're satisfied all is well. | 等您确信一切正常时，再删除那个文件夹。 |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | 等您确信一切正常时，再删除那个文件夹。在那之前空间不会真正释放。 |
| {0} freed | 已释放 {0} |
| {0} moved | 已移动 {0} |
| Nothing was moved | 没有移动任何文件 |
| Nothing was deleted | 没有删除任何文件 |
| {0} file could not be moved. | {0} 个文件无法移动。 |
| {0} files could not be moved. | {0} 个文件无法移动。 |
| {0} file could not be deleted. | {0} 个文件无法删除。 |
| {0} files could not be deleted. | {0} 个文件无法删除。 |
| {0} {1} moved to: {2} | 已将 {0} 个{1}移动到：{2} |
| {0} {1} moved to: {2} | 已将 {0} 个{1}移动到：{2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | 已保留 {0} 个文件。扫描认为它不需要，但最终检查无法确认这一点。 |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | 已保留 {0} 个文件。扫描认为它们不需要，但最终检查无法确认这一点。 |
| {0} {1} kept in place, because Windows has a record of the program named inside. | 有 {0} 个{1}保持原位，因为 Windows 有文件内所标示程序的记录。 |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | 有 {0} 个{1}保持原位，因为 InstallerClean 没能在文件内找到程序名。 |
| Moved {0} of {1} {2} to {3} before you cancelled. | 在您取消前，已将 {1} 个{2}中的 {0} 个移动到 {3}。 |
| Permanently deleted {0} of {1} {2} before you cancelled. | 在您取消前，已永久删除 {1} 个{2}中的 {0} 个。 |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | 撤销很简单。把它们移回 {InstallerFolder}，一切就会恢复原样。 |
| {0} {1} permanently deleted | 已永久删除 {0} 个{1} |
| {0} {1} permanently deleted | 已永久删除 {0} 个{1} |
| Glad to help. There's a tip jar if you're feeling kind. | 很高兴帮上忙。您若有心，这里可以打赏。 |

## Summaries and counts

| English | 简体中文 |
| --- | --- |
| {0} file left alone | {0} 个文件原样保留 |
| {0} files left alone | {0} 个文件原样保留 |
| {0} unneeded file to clean up | {0} 个不需要的文件可清理 |
| {0} unneeded files to clean up | {0} 个不需要的文件可清理 |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | Windows 有 {0} 个不在 {InstallerFolder} 中的文件的记录：{1}。日常使用不会有问题，但该程序的更新或卸载可能会失败。请打开详情了解该怎么做。 |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | Windows 有 {0} 个不在 {InstallerFolder} 中的文件的记录：{1}。日常使用不会有问题，但这些程序的更新或卸载可能会失败。请打开详情了解该怎么做。 |
| {0} other program | 另外 {0} 个程序 |
| {0} other programs | 另外 {0} 个程序 |
| {0} file with no program named in the records | {0} 个在记录中没有标明程序的文件 |
| {0} files with no program named in the records | {0} 个在记录中没有标明程序的文件 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than offering it. | InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供那 1 个文件，而是把它保留了下来。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than offering them. | InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供 {0} 个{1}，而是把它们保留了下来。 |
| InstallerClean couldn't be certain about one of the cached files it found, so it has held that one back rather than offering it. | InstallerClean 对它找到的缓存文件中的一个没有把握，因此没有提供它，而是把它保留了下来。 |
| InstallerClean couldn't be certain about some of the cached files it found, so it has held back {0} {1} rather than offering them. | InstallerClean 对它找到的部分缓存文件没有把握，因此没有提供 {0} 个{1}，而是把它们保留了下来。 |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | InstallerClean 无法确定唯一那个被取代的文件已不再需要，因此保留了它。 |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | InstallerClean 无法确定 {0} 个被取代的文件已不再需要，因此保留了它们。 |
| {0} of {1} {2} | {1} 个{2}中的 {0} 个 |
| {0} unneeded {1} ({2}) | {0} 个不需要的{1}（{2}） |
| {0} file left alone ({1}) | {0} 个文件原样保留（{1}） |
| {0} files left alone ({1}) | {0} 个文件原样保留（{1}） |
| {0} missing | 缺失 {0} 个 |
| {0} missing | 缺失 {0} 个 |

## Confirmation dialogs

| English | 简体中文 |
| --- | --- |
| Move {0} {1} ({2})? | 移动 {0} 个{1}（{2}）？ |
| This file will be moved to: | 此文件将移动到： |
| These files will be moved to: | 这些文件将移动到： |
| Delete {0} {1} ({2})? | 删除 {0} 个{1}（{2}）？ |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | 此文件将被永久删除。这么做是安全的，但如果您想要备份，请改用移动。 |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | 这些文件将被永久删除。这么做是安全的，但如果您想要备份，请改用移动。 |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | 那个文件夹在同一个驱动器上，所以在您删除它之前空间不会回来。如果想立刻拿回空间，请改选另一个驱动器上的文件夹。 |

## Error messages

| English | 简体中文 |
| --- | --- |
| This is also recorded in {0}. | 这也会记录在 {0} 中。 |
| Access denied | 访问被拒绝 |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows 拒绝了 InstallerClean 的访问，因此已停止。没有删除任何内容。<br><br>InstallerClean 本来就以管理员身份运行，所以再那样启动一次也无济于事。Windows 没有进一步说明是什么拒绝了访问，因此没有具体可以尝试的办法。 |
| Couldn't read the Windows Installer records | 无法读取 Windows Installer 记录 |
| Scan failed | 扫描失败 |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Windows Installer 记录返回的内容完全为空：没有任何一个已安装的程序或更新声称拥有缓存的安装文件。在正常工作的电脑上不会出现这种情况（即使是刚装好的 Windows 也会有一些），所以要么记录已损坏，要么无法读取；而一次相信这个结果的扫描，会把 {InstallerFolder} 中的每个文件都错误地判定为孤立。InstallerClean 没有那样做，而是停了下来。没有删除任何内容。 |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer 不允许 InstallerClean 列出已安装的内容。InstallerClean 本来就以管理员身份运行，所以再以管理员身份运行一次也不会有任何改变。没有这份清单，就无法安全地判断哪些缓存文件仍然需要，因此 InstallerClean 停了下来。没有删除任何内容。 |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {2} {3}, then {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer 无法向 InstallerClean 提供一份可读的已安装程序清单：它读取了 {2} {3}，随后连续 {0} 个条目返回时无法读取（最后的错误代码为 {1}）。InstallerClean 没有基于只读到一半的清单继续，而是停了下来。没有删除任何内容。 |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer 始终没有发出已安装程序清单结束的信号：InstallerClean 读取了 {2} {3}，随后在 {0} 个条目后放弃（最后的错误代码为 {1}）。没有尽头的清单无法信任，因此 InstallerClean 停了下来。没有删除任何内容。 |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer 始终没有发出某个程序补丁清单结束的信号：InstallerClean 读取了 {2} {3}，随后在 {0} 个条目后放弃（最后的错误代码为 {1}）。没有尽头的清单无法信任，因此 InstallerClean 停了下来。没有删除任何内容。 |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean 未能把 Windows Installer 记录与 {InstallerFolder} 中的内容对应起来。记录所指向的内容几乎都不在那里，而那里的内容几乎都没有被任何记录标明，因此无法证明任何文件是不需要的。没有提供任何内容，也没有移除任何内容。 |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean 未能把 Windows Installer 记录与 {InstallerFolder} 中的内容对应起来。文件夹里有文件，但没有任何一条记录指向其中的任何内容，因此无法证明任何文件是不需要的。没有提供任何内容，也没有移除任何内容。 |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean 未能读取到足够的 Windows Installer 记录，无法确定哪些内容仍然需要：已安装程序的清单返回时并不完整，而直接从注册表读取同样的记录也遇到了错误。一个文件可能仅仅因为指明它的那条记录属于读不到的记录之一，就显得像是孤立的，因此 InstallerClean 停了下来。没有删除任何内容。 |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean 未能让 Windows 解析出 {InstallerFolder} 的真实路径，因此无法证明任何文件位于其中，也没有提供任何文件供清理。这次扫描一无所获是因为那项检查失败，而不是因为文件夹是干净的。没有移除任何内容。 |
| Nothing was deleted | 没有删除任何文件 |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows 拒绝了 InstallerClean 检查 Windows Installer 是否正忙的权限，因此它无法排除某个文件在中途变得需要，没有删除任何文件。 |
| Nothing was moved | 没有移动任何文件 |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows 拒绝了 InstallerClean 检查 Windows Installer 是否正忙的权限，因此它无法排除某个文件在中途变得需要，没有移动任何文件。 |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean 未能取得 Windows Installer 用来防止两个程序同时更改已安装软件的锁，因此无法排除某个文件在中途变成必需的可能，也没有删除任何内容。请重试，若一直如此请重启 Windows。 |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean 未能取得 Windows Installer 用来防止两个程序同时更改已安装软件的锁，因此无法排除某个文件在中途变成必需的可能，也没有移动任何内容。请重试，若一直如此请重启 Windows。 |
| Invalid destination | 目标无效 |
| Move stopped | 移动已停止 |
| Couldn't use that backup folder | 无法使用该备份文件夹 |
| Move failed | 移动失败 |
| Delete failed | 删除失败 |
| Setting not saved | 设置未保存 |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | 无法保存此更改。下次启动时，InstallerClean 将恢复为之前的设置。 |
| The destination cannot be inside the Windows Installer folder. | 目标不能位于 Windows Installer 文件夹内。 |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | 目标 {0} 解析到了 Windows 系统文件夹之下。请选择 %SystemRoot%、%ProgramFiles%、%ProgramFiles(x86)% 和 %ProgramData% 之外的路径。 |
| Not enough space | 空间不足 |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | {0} 上放不下<br><br>所需：{1}<br>可用：{2} |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | 此文件正被另一个程序打开或锁定，因此暂时无法移除。它已保持原位；请稍后再试。 |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | 这些文件正被另一个程序打开或锁定，因此暂时无法移除。它们已保持原位；请稍后再试。 |
| Windows reported a file error; the file was left in place. | Windows 报告了一个文件错误；该文件已留在原处。 |
| Windows reported file errors; these files were left in place. | Windows 报告了文件错误；这些文件已留在原处。 |
| Something went wrong with this file; it was left in place. | 此文件出了点问题；该文件已留在原处。 |
| Something went wrong with these files; they were left in place. | 这些文件出了点问题；它们已留在原处。 |
| Refusing to move files into the Windows Installer folder (destination: {0}). | 拒绝将文件移动到 Windows Installer 文件夹（目标：{0}）。 |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | 备份文件夹必须是指向某个文件夹的完整路径，以驱动器盘符或网络共享开头（例如 D:\Backup，或 \\server\backup）。InstallerClean 无法使用这个：{0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean 已无法确认备份文件夹，因此停了下来。请检查 {0}，然后重新扫描并再试一次。 |
| Cannot write to {0}. | 无法写入 {0}。 |
| A file called '{0}' is already in the backup folder. | 备份文件夹中已经有一个名为“{0}”的文件。 |

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
| The check failed for an unknown reason. Details are in {0} if you need to report it. | 检查因未知原因失败。如果您需要报告此问题，详情在 {0} 中。 |
| The check failed for an unknown reason. The crash log could not be written. | 检查因未知原因失败。无法写入崩溃日志。 |

## Opening links in your browser

| English | 简体中文 |
| --- | --- |
| Couldn't open your browser | 无法打开您的浏览器 |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | 链接已复制到剪贴板，您可以自己粘贴：<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean 也无法把链接复制到剪贴板，链接在这里：<br><br>{0} |

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
| It's already running. | 已在运行。 |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | 发生了意外错误，InstallerClean 需要关闭。<br><br>{0}<br><br>详情已写入：<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | 发生了意外错误，InstallerClean 需要关闭。<br><br>{0}<br><br>无法写入崩溃日志。 |
| Startup error | 启动错误 |
| Failed to start ({0}). Details written to:<br>{1} | 启动失败（{0}）。详情已写入：<br>{1} |
| Failed to start ({0}). The crash log could not be written. | 启动失败（{0}）。无法写入崩溃日志。 |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log 记录 InstallerClean 未处理的异常。<br># 在提升权限的情况下，框架的异常消息可能包含当前会话中的文件路径<br>#（包括 Windows Installer 查询所枚举的其他用户的配置文件）。更新<br># 检查或结果日志上传的网络故障消息，可能包含目标 URL 以及解析出的<br># IP 或代理地址。关于无法读取的 Windows Installer 记录的条目，可能<br># 包含 Windows 账户 SID（S-1-5-21-...）以及已安装软件的产品代码。<br># 把此文件附到公开的错误报告之前，请先删除这三类信息。<br> |

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
| Move the unneeded files to the backup folder. | 把不需要的文件移动到备份文件夹。 |
| Move the unneeded files to a backup folder. You'll choose it next. | 把不需要的文件移动到一个备份文件夹。您接下来会选择它。 |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | 把不需要的文件移动到备份文件夹。它在同一个驱动器上，所以要等您删除那个文件夹后才会释放空间。 |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | 永久删除不需要的文件。如果您想有机会自己确认一切正常，请改用移动。 |
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
| Delete permanently removes the unneeded files. Cancel closes without deleting. | 永久删除会移除这些不需要的文件。取消则不删除任何内容并关闭。 |
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
| Backup folder | 备份文件夹 |
| Patches | 补丁 |
| Product details | 产品详情 |
| Backup folder | 备份文件夹 |
| Operation progress | 操作进度 |
| Scan {InstallerFolder} again | 重新扫描 {InstallerFolder} |
| Scanning progress | 扫描进度 |
| Startup scan progress | 启动扫描进度 |
| Details, unneeded files | 详情，不需要的文件 |
| Available for cleanup. | 可供清理。 |
| Details, files left alone | 详情，原样保留的文件 |
| Read-only inventory. | 只读清单。 |
| Sorted by {0}, ascending | 已按{0}升序排序 |
| Sorted by {0}, descending | 已按{0}降序排序 |
| Scan results | 扫描结果 |
| Result details | 结果详情 |
| File details | 文件详情 |
| Product details | 产品详情 |
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
| ,  | 、 |
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
| Error: unknown argument '{0}' | 错误：未知参数“{0}” |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | 错误：出现意外的多余参数“{0}”。如果目标文件夹的路径中含有空格，请给整个路径加上引号：/m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | 错误：出现意外的多余参数“{0}”。/s 和 /d 不接受其他参数，每次运行也只能使用一个开关。 |
| Cancelling... | 正在取消… |
| Cancelled. | 已取消。 |
| Error: unexpected failure ({0}). Details written to {1}. | 错误：意外故障（{0}）。详情已写入 {1}。 |
| Error: unexpected failure ({0}). The crash log could not be written. | 错误：意外故障（{0}）。崩溃日志无法写入。 |
| Scanning {InstallerFolder}... | 正在扫描 {InstallerFolder}… |
| Found {0} unneeded {1} to clean up ({2}). | 找到 {0} 个可清理的、不需要的{1}（{2}）。 |
| Found no unneeded files. | 未找到不需要的文件。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供那 1 个文件（{2}），而是把它保留了下来。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} {1} ({2}) rather than offering them. | InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供全部 {0} 个{1}（{2}），而是把它们保留了下来。 |
| InstallerClean couldn't establish that the cached file it found is unneeded, so it has held back the one file ({2}) rather than offering it. | InstallerClean 无法证实它找到的那个缓存文件是不需要的，因此没有提供那 1 个文件（{2}），而是把它保留了下来。 |
| InstallerClean couldn't establish that any of the cached files it found are unneeded, so it has held back all {0} {1} ({2}) rather than offering them. | InstallerClean 无法证实它找到的缓存文件中有任何一个是不需要的，因此没有提供全部 {0} 个{1}（{2}），而是把它们保留了下来。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供那 1 个文件（{2}），而是把它保留了下来。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean 无法确定缓存中的哪些文件属于这里安装的程序，因此没有提供 {0} 个{1}（{2}），而是把它们保留了下来。 |
| InstallerClean couldn't be certain about one of the cached files it found, so it has held that one back ({2}) rather than offering it. | InstallerClean 对它找到的缓存文件中的一个没有把握，因此没有提供那一个（{2}），而是把它保留了下来。 |
| InstallerClean couldn't be certain about some of the cached files it found, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean 对它找到的部分缓存文件没有把握，因此没有提供 {0} 个{1}（{2}），而是把它们保留了下来。 |
| Why it couldn't be certain: | 无法确定的原因： |
|   A file path in Windows Installer's own records wouldn't resolve, so nothing could be matched to it. |   Windows Installer 自身记录中的一个文件路径无法解析，因此没有任何内容能与它对应起来。 |
|   A file Windows has a record of couldn't be identified, so it couldn't be matched to what's in the folder. |   无法识别 Windows 有记录的某个文件，因此无法把它与文件夹中的内容对应起来。 |
|   A program may be installed more than once on this PC, and the records can't say which copy a file belongs to. |   某个程序可能在这台电脑上安装了不止一次，而记录无法说明某个文件属于哪一份。 |
|   A file in the folder couldn't be identified, so it couldn't be matched against the records. |   无法识别文件夹中的某个文件，因此无法把它与记录对应起来。 |
|   A file says it belongs to a program that is still installed, so it may still be needed. |   某个文件声称属于一个仍然安装着的程序，因此可能仍然需要。 |
|   Either a file wouldn't say which program it belongs to, or Windows wouldn't answer about that program. |   要么某个文件没有说明它属于哪个程序，要么 Windows 没有就该程序作出回答。 |
|   A check on which programs the files belong to gave answers that didn't line up with the files it was handed. |   一项关于这些文件属于哪些程序的检查，给出的答案与交给它的文件对不上。 |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows 有 {0} 个不在 {InstallerFolder} 中的文件的记录：{1}。日常使用不会有问题，但该程序的更新或卸载可能会失败。要把该文件放回去，您需要您当前所用版本的安装程序。请从程序的制作方获取，并在现有副本上运行它。更新的版本不行：新版本必须先移除您现有的版本，而正是这一步需要这个文件。先卸载同样行不通，原因相同。这应当会恢复该文件并保持您的设置不变，但 Microsoft 并不保证。 |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows 有 {0} 个不在 {InstallerFolder} 中的文件的记录：{1}。日常使用不会有问题，但这些程序的更新或卸载可能会失败。要把某个文件放回去，您需要该程序当前所用版本的安装程序。请从程序的制作方获取，并在现有副本上运行它。更新的版本不行：新版本必须先移除您现有的版本，而正是这一步需要该文件。先卸载同样行不通，原因相同。这应当会恢复该文件并保持您的设置不变，但 Microsoft 并不保证。 |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | InstallerClean 无法确定唯一那个被取代的文件已不再需要，因此保留了它。 |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | InstallerClean 无法确定 {0} 个被取代的文件已不再需要，因此保留了它们。 |
| Deleting {0} unneeded {1}... | 正在删除 {0} 个不需要的{1}… |
| Permanently deleted {0} unneeded {1}. | 已永久删除 {0} 个不需要的{1}。 |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | 错误：未指定移动目标位置。请使用 /m 路径。（在 GUI 中设置的默认位置是按用户保存的，不适用于计划任务或服务账户下的运行。） |
| Error: destination cannot be inside the Windows Installer folder. | 错误：目标位置不能位于 Windows Installer 文件夹内。 |
| Error: destination must be a fully qualified path. Got: {0} | 错误：目标位置必须是完整路径。收到：{0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | 错误：目标 {0} 解析到了 Windows 系统文件夹之下。请选择 %SystemRoot%、%ProgramFiles%、%ProgramFiles(x86)% 和 %ProgramData% 之外的路径。 |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | 错误：{0} 上的空间不足。移动这些文件需要 {1}，而可用空间为 {2}。没有移动任何内容。 |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | 错误：此刻有程序正在使用 Windows Installer，比如 Windows 更新，或者某个正在后台安装的程序。在此期间 /m 和 /d 会被阻止。等结束后再试。 |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | 错误：这台计算机上有一个先前的 Windows Installer 事务处于挂起状态。请先继续或回滚那次安装（或重启 Windows），再清理 {InstallerFolder}。 |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | 错误：一项排在重启之后的文件操作指向 {InstallerFolder}（{0}）。请先重启 Windows 让该操作完成，再进行清理。 |
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | 错误：有一项文件操作已排入下次重启的队列，InstallerClean 无法得知它指名了哪些文件，因此无法排除 {InstallerFolder}。请在清理前重启 Windows。 |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | 错误：InstallerClean 无法读取它在碰 {InstallerFolder} 之前会检查的一项注册表值，因此无法排除有 Windows Installer 操作正在进行或已排入下次重启的队列。/m 和 /d 会被阻止。请重启 Windows 后再试。如果仍然读不到，这台机器就不是 InstallerClean 能清理的。 |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | 错误：Windows Installer 有操作正在进行，因此 /m 和 /d 会被阻止。InstallerClean 不会在 {InstallerFolder} 变动时去碰它。等结束后再试。 |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | 错误：InstallerClean 未能取得防止两个程序同时更改已安装软件的 Windows Installer 锁，因此无法排除某个文件在中途变成必需的可能。没有删除任何内容。请重试，若一直如此请重启 Windows。 |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | 错误：InstallerClean 未能取得防止两个程序同时更改已安装软件的 Windows Installer 锁，因此无法排除某个文件在中途变成必需的可能。没有移动任何内容。请重试，若一直如此请重启 Windows。 |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | 错误：Windows 拒绝了 InstallerClean 检查 Windows Installer 是否正忙的权限，因此它无法排除某个文件在中途变得需要。没有删除任何文件。 |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | 错误：Windows 拒绝了 InstallerClean 检查 Windows Installer 是否正忙的权限，因此它无法排除某个文件在中途变得需要。没有移动任何文件。 |
| Moving {0} unneeded {1} to {2}... | 正在把 {0} 个不需要的{1}移动到 {2}… |
| Moved {0} unneeded {1}. | 已移动 {0} 个不需要的{1}。 |
| Check that your programs still update and uninstall as normal, then delete {0}. | 请确认您的程序仍能照常更新和卸载，然后删除 {0}。 |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | 撤销很简单。把它们从 {0} 移回 {InstallerFolder}，一切就会恢复原样。 |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean 已无法确认备份文件夹，因此停了下来。请检查 {0}，然后重新运行该命令。 |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | 另一个 InstallerClean 进程正持有单实例锁（GUI 或另一次 CLI 运行）。退出代码 75（暂时性）；稍后可安全重试。 |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | 注意：事件日志写入失败。请检查应用程序日志的权限或组策略。 |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - 清理 {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | 移除没有任何已安装程序仍然需要的 .msi 和 .msp 缓存文件。 |
| Needs an elevated (administrator) prompt; Windows will not start it. | 需要管理员命令提示符，否则 Windows 不会启动它。 |
| Usage: | 用法： |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     显示此帮助（也接受 /?、-h） |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  显示版本号（也接受 -v） |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         仅扫描 - 列出不需要的文件 |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         永久删除不需要的文件 |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         移动到已保存的备份文件夹 |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m 路径    移动到指定路径 |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli 会占住命令提示符直到结束，因此脚本或计划任务<br>可以等待它完成。 |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | 该文件夹按用户保存；计划任务或 SYSTEM 运行需要 /m 路径。 |
| Exit codes: | 退出代码： |
|   0   success: the run did what it was asked and nothing failed |   0   成功：本次运行做了要求的事，并且没有任何失败 |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   失败：没有处理任何内容（参数或目标有误、扫描失败，<br>       或者每个文件都失败） |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   部分：部分已处理，部分未处理（失败或 Ctrl+C） |
|   75  transient: a temporary condition blocked the run (see the message) |   75  暂时性：临时状况阻止了本次运行（见相关消息） |
|   130 cancelled (Ctrl+C) |   130 已取消（Ctrl+C） |
