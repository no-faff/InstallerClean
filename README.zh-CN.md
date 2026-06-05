<p align="center">
  <a href="README.md">English</a> · <strong>简体中文</strong> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a>
</p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong>一个现代的开源 <a href="https://www.homedev.com.au/free/patchcleaner">PatchCleaner</a> 替代方案。安全清理 <code>C:\Windows\Installer</code>，这个悄悄吞噬您磁盘空间的隐藏 Windows 文件夹。</strong></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-blue.svg" alt="许可证：MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/github/v/release/no-faff/InstallerClean" alt="GitHub 版本"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/github/downloads/no-faff/InstallerClean/total?cacheSeconds=300" alt="总下载量"></a>
</p>

![InstallerClean 完成清理后的截图：释放 965 MB，删除 68 个文件](docs/screenshots/04d-deleted-freed-success.webp)

- **简介：** 在 `C:\Windows\Installer` 中查找并删除不需要的文件，这是 Windows 从不清理的隐藏文件夹。
- **能释放多少空间：** 取决于您的软件。在我的电脑上接近 1 GB。一位 InstallerClean 用户[报告](https://github.com/no-faff/InstallerClean/issues/12#issuecomment-4395580816)清理出 25 GB。安装了 Adobe Acrobat 时可能超过 100 GB。也可能完全没有。关键是它既快又免费；凡是能清理的，都会被清理掉。
- **是否安全：** 是的。仅删除 Windows 自身确认不再需要的文件。删除会将文件发送到回收站（如果回收站对该驱动器不可用，则让您选择移动、永久删除或取消，绝不会在未经询问的情况下永久删除）。移动会让您把文件保存到安全的位置。
- **如何获取：** [下载最新版本](../../releases/latest)，运行，搞定。

---

## 没有人告诉您的文件夹

每台 Windows 电脑上都有一个名为 `C:\Windows\Installer` 的隐藏文件夹。每次您安装基于 Windows Installer 的软件，或为 Microsoft Office、Adobe Acrobat、Visual Studio 或其他任何 `.msi` 应用程序打补丁时，安装程序或 `.msp` 补丁文件的副本都会进入这个文件夹，然后留在那里。

当您卸载软件时，这些文件依然留着。当新补丁取代旧补丁时，两者都留着。Windows 从不清理它们。磁盘清理工具不碰这些文件。DISM 是用于另一个文件夹的工具。年复一年，这个文件夹越来越大：10 GB、30 GB、50 GB。在大量使用 MSI 软件的电脑上（Acrobat 是常见元凶），它可能[超过 100 GB](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/)。

这些不是关闭清理工具后又会重新生成的临时文件。它们是真正的累赘：多年前卸载的软件留下的旧安装程序，还有被替换过三次的补丁。一旦清理掉，它们不会回来。

**如果您正在寻找一种简单的方法来释放 Windows 上的磁盘空间，这个文件夹是最佳的起点之一。** InstallerClean 找到这些不需要的文件并安全地删除它们。

[PatchCleaner](https://www.homedev.com.au/free/patchcleaner) 长期以来是处理这一问题的首选工具，但自 2016 年 3 月以来未再更新，且不是开源的。InstallerClean 是它的开源替代方案，具备被取代补丁的检测能力（能识别 PatchCleaner 排除的 Acrobat 补丁），还有现代的用户界面。

## 寻求帮助

如果您曾经为这个文件夹寻找过帮助，您就知道是怎么回事。有人问怎么清理它。被告知运行磁盘清理。试了一下。在 [180 GB 的文件夹中只释放了 600 MB](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb)。然后讨论就没下文了。

> *"我找到的所有帖子都倾向于推荐同样的方法，这些方法解决不了问题，然后讨论就死了。"*
>
> ksparks519, r/Windows10（英文原帖翻译）

或者被告知完全不要碰它。在一个帖子中，有人因为 60 GB 的 Installer 文件夹收到了 ["不要碰它"](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/) 的回复。当他问应该怎么做时，回答是：*"我刚才不就说过了吗。"*

主流建议把"随机删除文件"（这确实危险）和"删除 Windows 自身确认不再需要的文件"（这并不危险）混为一谈。InstallerClean 做的是后者。

如果您之前找过这方面的帮助，您可能已经发现了 [John Crawford](https://www.homedev.com.au/) 开发的 [PatchCleaner](https://www.homedev.com.au/free/patchcleaner)。这是一款很棒的应用。我下载它，它做了它声称要做的事：释放了大量空间。它唯一不处理的是 Adobe 补丁；它默认排除这些补丁，而在 Adobe 是最大占用源的电脑上，大量可删除的文件就被留下了：

> *"我下载了 PatchCleaner 来删除孤立的 .msp 文件……但 29 GB 的文件都被'过滤器排除'了，所以 PatchCleaner 似乎帮不上忙。"*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/)（英文原帖翻译）

InstallerClean 检测被新版本取代的补丁，并将其标记为可删除，包括 PatchCleaner 排除的 Acrobat 补丁。

## 它做什么

1. **扫描** `C:\Windows\Installer` 中的 `.msi` 和 `.msp` 文件
2. **查询** Windows Installer API 以确定哪些文件仍处于注册状态
3. **显示** 哪些是需要的、哪些是不需要的，并附带文件大小
4. **删除** 不需要的文件：发送到回收站（如果回收站对该驱动器不可用，应用会在任何永久删除前先询问），或移动到您选择的文件夹

无自动网络活动。两个选择性按钮在点击时发起一次 HTTPS 调用：「关于」中的**检查更新**，以及完成屏幕上的**发送摘要**。详情请见下文 [它不做什么](#它不做什么)。

## 截图

<details>
<summary>点击展开</summary>

<br>

<p>
  <img src="docs/screenshots/01-initial-scan.webp" alt="启动画面，显示扫描进行中，已找到 68 个待清理文件" width="900"><br>
  <em>初始扫描，速度很快。</em>
</p>

<p>
  <img src="docs/screenshots/02-main-window.webp" alt="主窗口，显示 116 个仍在使用的文件和 68 个待清理文件" width="900"><br>
  <em>结果：哪些在使用、哪些可清理。</em>
</p>

<p>
  <img src="docs/screenshots/03a-details-registered.webp" alt="已注册文件窗口，列出已安装产品及其安装数据库元数据" width="900"><br>
  <em>仍在使用的文件，附带从安装数据库读取的元数据。</em>
</p>

<p>
  <img src="docs/screenshots/03b-details-unused.webp" alt="未使用文件窗口，列出可删除的 .msi 文件及其原因" width="900"><br>
  <em>不再需要的文件。</em>
</p>

<p>
  <img src="docs/screenshots/04b-Delete-dialogue.webp" alt="删除确认对话框，显示 68 个文件（965 MB）将被发送到回收站" width="900"><br>
  <em>每次操作前都会有确认。删除发送到回收站；移动则把文件放到您选择的位置。</em>
</p>

<p>
  <img src="docs/screenshots/04d-deleted-freed-success.webp" alt="删除操作成功后的提示界面，显示已释放 965 MB" width="900"><br>
  <em>删除成功后的画面。</em>
</p>

<p>
  <img src="docs/screenshots/06a-scanned-again-all-clean.webp" alt="再次扫描后无可清理项的提示界面" width="900"><br>
  <em>再次扫描后，已无可清理。</em>
</p>

</details>

## 工作原理

InstallerClean 识别两类不需要的文件。

**孤立文件** 是卸载软件后留下的安装程序和补丁。Windows 不再引用它们，但这些文件仍占据着文件夹中的空间。

**被取代的补丁** 是被新补丁替换的旧 `.msp` 补丁。Windows 在自己的数据库中将它们标记为已被取代，但从不删除。频繁发布补丁的厂商（Acrobat、Office、大型开发工具）会无限累积被取代的补丁。

为了找到它们，InstallerClean 通过 P/Invoke 直接调用 Windows Installer COM 接口：

- `MsiEnumProductsEx` 枚举每个已安装的产品
- `MsiEnumPatchesEx` 查找每个产品所有已注册的补丁
- `MsiGetPatchInfoEx` 读取补丁状态（已应用、被取代或已废弃）

`C:\Windows\Installer` 中没有被任何已注册产品声明的 `.msi` 或 `.msp` 文件即为孤立文件。任何被标记为已被取代且不需要用于卸载的补丁，都会被标记为可删除。

如果 API 返回的数据不完整（很少见，但在安装程序状态损坏时可能发生），应用会回退到读取注册表。这种回退只会向"仍需保留"的集合中添加文件，绝不会向"可删除"集合添加。

移动或删除完成后，`C:\Windows\Installer` 内的空子文件夹（缓存清空后留下的目录）会在同一次操作中被清理。在此过程中，重新分析点（reparse points）会被跳过，这样植入缓存内的目录联接（junction）就无法把清理引向缓存之外。

## 是否安全？

是的。InstallerClean 查询的是 Windows 自己用来追踪安装内容的同一个数据库。如果 Windows 说某个文件不再需要，应用就相信这一点；它不会根据文件名或日期来猜测。

**应用内部。** 删除会将文件发送到回收站。如果回收站对该驱动器不可用（已为该驱动器关闭，或已满或损坏），InstallerClean 不会悄悄地把文件彻底删除。它会停下来让您选择：把文件移动到安全的地方、永久删除，或者取消。只有在您明确选择时，文件才会被永久删除。移动是更稳妥的选择：它把文件放到您选择的文件夹中，您可以先留着，直到确认没有任何问题。在您确认之前不会动任何文件。如果 Windows Installer 正在写入缓存、有先前的事务被挂起、或有针对缓存的开机后重命名待执行，移动和删除会被禁用，并显示具体的原因。扫描、查询、移动、删除、设置和待重启服务由一套自动化测试覆盖，每次提交都会运行（参见上方的 CI 徽章）。

**验证二进制文件。** InstallerClean 没有数字签名。代码签名证书每年都要花钱，我宁愿让项目保持免费、开源、由捐赠支持。

- 每个版本的 SHA-256 哈希值都列在[发布页面](../../releases/latest)。
- 每个版本都会发布 setup、portable、slim 和 CLI 各构建的 VirusTotal 链接。
- 源代码在 [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean)，CI 会编译并测试每次提交（参见上方绿色 CI 徽章）。
- [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) 对每个版本进行病毒、间谍软件和广告软件检测。
- [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) 在虚拟机中测试每次提交，只有通过审核后才会上架。

<a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Softpedia 认证 100% 干净" width="190"></a>

VirusTotal：所有引擎均为干净。每次发布说明中均提供实时链接，便于您自行复核。

## 如果需要的文件丢失了

InstallerClean 只会清除 Windows 报告为不再需要的文件，因此它不可能让某个程序无法修复、更新或卸载。手动删除 `C:\Windows\Installer` 中的文件，或使用一款不先查询安装数据库的工具来删除，则完全是另一回事，这也正是「不要碰这个文件夹」这一主流建议的由来。这个建议在一定程度上是对的。下面是更完整的来龙去脉，以及万一某个需要的文件已经丢失，该怎么办。

<details>
<summary><strong>关于 <code>C:\Windows\Installer</code>，以及如何找回丢失的文件</strong></summary>

<br>

`C:\Windows\Installer` 是 Windows Installer 缓存。当您安装基于 MSI 的程序或应用补丁时，Windows 会在这里保留一份安装程序的副本，并为每个产品记录下它日后预期会用到的那个文件。这些文件在程序运行期间并不会被用到；只有在 Windows 修复、更新或卸载该程序时才会用到。删掉某个程序仍然需要的文件，当下不会立刻出问题；这正是为什么删除它们往往看似无碍，却要到几个月后才惹上麻烦。Microsoft 是这样说的：

> "If the installer cache is compromised, you may not immediately see problems until you take an action such as uninstalling, repairing, or updating a product."

恢复并不简单，对此 Microsoft 也直言不讳：

> "If application files are missing from the Windows Installer Cache, ask the vendor or support team for the application about the missing files. You must follow the procedures or steps recommended by the application vendor to restore the files. In some cases, you may have to rebuild the operating system and reinstall the application to fix the problem."

> "Windows support engineers cannot help you recover missing application files from the Windows Installer cache."

您也无法从另一台机器借用该文件：

> "Missing files cannot be copied between computers because the files are unique."

实际操作中，通常奏效的办法是从受影响程序的厂商处下载它的安装程序，然后在现有安装之上运行它。不要先卸载：卸载本身就是需要用到那个丢失文件的步骤之一。如果还能找得到，请使用您当前安装的那个版本，因为 Windows 可能会拒绝另一个版本：

> "The upgrade cannot be installed by the Windows Installer service because the program to be upgraded may be missing, or the upgrade may update a different version of the program."

这通常会恢复该文件并且不动您的设置，但 Microsoft 并不保证，而它在文档中给出的最后手段是重新安装该程序，甚至重建 Windows。这就是官方立场，我如实转述。这不是我造成的，我也无法在 Microsoft 自己的指引之上再做改进；我只是把情况如实告诉您。

这一切都不可能因为 InstallerClean 而发生。它只会删除 Windows 自身报告为不再需要的文件，因此日后某次修复、更新或卸载所要找的文件，绝不会是它动过的。Microsoft 的指引见 [Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache)。

</details>

## 它不做什么

- WinSxS（`C:\Windows\WinSxS`）是另一个有不同规则的文件夹。对于那个文件夹，请在管理员命令提示符下运行 `Dism /Online /Cleanup-Image /StartComponentCleanup`。
- 没有后台服务、没有计划任务、没有自动清理。应用只在您启动它时运行。
- 注册表只读。应用查询 Windows Installer 数据库；它不会修改数据库。
- 无自动遥测、无后台网络活动。在您点击两个选择性按钮之一之前，应用不会发出任何网络请求。**检查更新**（位于「关于」窗口）在点击时调用 GitHub 公开的 releases API，并告诉您是否为最新版本（单次 HTTPS GET，标识字符串 `InstallerClean/<版本>`）。**发送摘要**（位于完成屏幕）会读取 `%LOCALAPPDATA%\NoFaff\InstallerClean\last-run.json`，并以 HTTPS POST 的方式发送至 No Faff 的端点，以便我能查看本次运行是否成功。该 JSON 仅包含计数和分类标签：不含任何文件路径、用户名、机器标识符或时间信息。点击后会弹出一个确认窗口，显示即将发送的具体 JSON 内容；查看后按「发送」确认，或按「取消」放弃。每台机器一次：成功发送后该按钮会永久隐藏；如果首次尝试因瞬时错误失败，下一次会话会再次询问。
- 无捆绑附加。无工具栏，无第三方推广，无烦人弹窗。
- 启动之外要求的唯一权限是管理员权限，这是因为 `C:\Windows\Installer` 仅限管理员访问。

## 常见问题

**我真的能释放几 GB 的空间吗？** 取决于您的电脑。一台没有额外软件的全新 Windows 11 安装上没有什么可清理的。一台长期使用的开发工作站，或任何安装了大量基于 MSI 的软件（Acrobat、Office、LibreOffice、大型开发工具）的电脑，可能有几十 GB。运行 `installerclean-cli /s` 可以在执行之前查看具体会删除哪些文件。

**为什么需要管理员权限？** `C:\Windows\Installer` 归 SYSTEM 所有，仅向管理员开放。读取该文件夹、调用 Windows Installer 数据库 API、移动或删除文件，都需要权限提升。没有用户态的路径。

**可以撤销删除吗？** 通常可以。当回收站对该驱动器可用时，删除会把文件发送到回收站，从那里恢复即可。如果回收站不可用，应用不会自行把文件彻底删除；它会提供"移动"或由您确认的永久删除。无论哪种情况，如果想要一个由您掌控的安全网，可以改用"移动"把文件放到您选择的文件夹，确认没有问题后再从那里删除。

**删除这些文件 Windows 会有意见吗？** 不会。InstallerClean 只会删除 Windows 自身报告为不再需要的文件，因此它删除的任何东西都不是修复、更新或卸载某个程序所需要的。如果某个需要的文件确实因为别的原因从 `C:\Windows\Installer` 中消失了，请见[如果需要的文件丢失了](#如果需要的文件丢失了)。

**为什么不用 `Win32_Product`（WMI）？** [`Win32_Product` 在枚举时会触发每个产品的 MSI 修复操作](https://gregramsey.net/2012/02/20/win32_product-is-evil/)，可能耗时数分钟并使磁盘负载剧增。InstallerClean 直接调用 Windows Installer COM API，无副作用。

**为什么不直接用 PowerShell 脚本？** 一段调用 `MsiEnumPatchesEx` 的短脚本足够*列出*补丁，但 InstallerClean 的关键部分恰恰是脚本会一笔带过的内容：孤立与被取代的分类、只往「仍需保留」集合（绝不往「可删除」集合）添加文件的注册表回退、待重启时的阻断、移动至别处的安全网、带取消能力的逐文件进度，以及默认回收站而非永久删除。在真正大量使用 MSI 的电脑上，边界情况（注册损坏、缓存内的目录联接、`HKU\.DEFAULT` 中的产品、被挂起的 Installer 事务）在一次性脚本中很容易处理不当。如果您需要的是脚本能力，`installerclean-cli` 就是无界面版本。

**支持 Windows 7 或 8 吗？** 未测试，不支持。目标平台是 Windows 10 和 11。

**适合 RMM 或大规模部署吗？** 是的。CLI 按结果返回不同的退出码（0 成功、2 部分成功、1 完全失败、75 瞬时状态、130 Ctrl+C），所以计划任务可以在 75 时重试，而不会与硬性失败混淆。每次运行会向应用程序事件日志写入一份摘要，并遵循与图形界面相同的单实例互斥锁。详见命令行部分。

## 下载

四种构建，请选其一：

- **Setup**（`InstallerClean-setup.exe`）：标准的 Windows 安装程序，自带 .NET 10 运行时。在开始菜单中添加快捷方式，可以干净卸载。放在「程序」里，六个月后容易找回。
- **Portable**（`InstallerClean-portable.exe`）：单一自包含可执行文件，自带运行时。无需安装，无需卸载。运行、使用、删除即可。需要时随时再运行。
- **Slim**（`InstallerClean-slim.exe`）：体积最小。需要事先安装 [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)（如果您的 Visual Studio 是最新版本，那就已经装了）。
- **CLI**（`installerclean-cli.exe`）：单独的命令行版本，单一自包含可执行文件。无需安装，用完不留任何痕迹。把它放到客户端机器上，运行一次扫描或清理，然后删除。专为脚本、计划任务和大规模部署而设计，适合需要执行这些操作但不想在客户端安装桌面应用的场景。参数和退出码见[命令行](#命令行)。

从[发布页面](../../releases/latest)下载，然后运行。Windows SmartScreen 会提示"未知发布者"。点击"**更多信息**"然后"**仍要运行**"。这对未签名的开源软件而言是正常情况。

应用启动时会自动扫描。查看结果，然后点击"**删除**"或"**移动**"。

或通过 [Scoop](https://scoop.sh) 安装：

```
scoop bucket add no-faff https://github.com/no-faff/scoop-bucket
scoop install installerclean
```

## 与 PatchCleaner 对比

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| 最近更新 | 2026（持续维护） | 2016 年 3 月 3 日 |
| 源代码 | 开源（MIT） | 闭源 |
| 运行时 | .NET 10（自包含） | .NET + VBScript |
| API | Windows Installer COM（进程内） | Windows Installer COM（通过 VBScript 跨进程） |
| 被取代补丁检测 | 有 | 无 |
| Adobe 处理 | 检测被取代补丁 | 默认排除 |
| 用户界面 | 深色主题（WPF） | Windows Forms |
| 数据收集 | 无 | 无 |
| 删除安全性 | 回收站，绝不悄悄永久删除 | 永久删除，不经回收站 |

> **关于 `Win32_Product` 的说明：** 列出已安装产品的常见但有缺陷的做法是 `Win32_Product`（WMI），它会在枚举时[对每个产品触发 MSI 修复操作](https://gregramsey.net/2012/02/20/win32_product-is-evil/)。InstallerClean 和 PatchCleaner 都避免使用它。两者都使用 Windows Installer COM 接口。PatchCleaner 脚本中的 `WMIProducts.vbs` 文件名具有误导性；该脚本实际使用的是 MSI COM，而非 WMI。

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) 也在其 System Booster 模块中提供 Installer 清理功能，但它是付费工具（15 至 25 美元），清理只是其中的一个小功能。InstallerClean 是免费、专一、开源的。

像 [CCleaner](https://www.ccleaner.com/) 和 [BleachBit](https://www.bleachbit.org/) 这类通用系统清理工具不会处理 `C:\Windows\Installer`。这个文件夹需要 Windows Installer API 查询来区分已注册的包和未使用文件，仅遍历文件树的通用清理工具可能会破坏已安装的应用。当您要清理的就是这个文件夹时，InstallerClean 是合适的工具。

## 命令行

InstallerClean 支持无界面运行，方便编写脚本和系统管理：

```
用法：
  installerclean-cli --help   显示此帮助（也接受 /?、-h）
  installerclean-cli --version  显示版本（也接受 -v）
  installerclean-cli /s       仅扫描，列出可删除的文件
  installerclean-cli /d       删除文件（送至回收站）
  installerclean-cli /m       移动到已保存的默认位置
  installerclean-cli /m PATH  移动到指定路径
```

要启动图形界面，请运行 `InstallerClean.exe`（或使用 setup 安装后的"开始"菜单快捷方式）。

不带参数运行，或使用无法识别的选项时，`installerclean-cli` 会显示此帮助并以代码 `1` 退出，因此丢失了选项的计划任务会显式失败，而不是悄无声息地"成功"却什么都不做。显式指定 `--help`、`/?` 或 `-h` 则显示相同的帮助并以代码 `0` 退出。

`/s` 是空跑模式：扫描、列出将被删除的文件名和大小，然后退出。便于在清理前审核。退出码为 `0` 表示扫描成功，`1` 表示扫描失败，`130` 表示按下 Ctrl+C。所有文件都在 `C:\Windows\Installer` 中。

`/d` 和 `/m` 先扫描后执行。`/d` 将可删除文件发送到回收站。`/m` 将它们移动到一个文件夹（您在命令行指定的、或图形界面保存的默认值）。退出码：`0` 完全成功；`2` 部分成功（部分文件成功，部分失败）；`1` 完全失败（扫描失败、参数错误，或本批次所有文件都失败）；`75` 某个瞬时状态阻止了本次运行（屏幕上的消息会说明是哪种情况、以及重试是否有用）；`130` 用户按下 Ctrl+C。

CLI 的所有输出，包括错误和诊断信息，都写入 stdout；没有单独的 stderr 流。退出码是供机器读取的信号（每次运行的应用程序事件日志条目也会与之对应），因此脚本应根据退出码判断，而不是解析文本，并且 `installerclean-cli /s > audit.txt` 会捕获整个运行过程，包括任何错误行。

三个命令都需要管理员权限的命令提示符。如果组策略阻止 UAC 提升提示，进程将拒绝启动，Windows 会向父 Shell 返回错误码 740（PowerShell 中表现为 `$LASTEXITCODE = 740`）。`taskkill /pid <pid>` 不会触发优雅取消；单实例互斥锁会在下次运行时通过 AbandonedMutexException 路径恢复。

注意：CLI 自身的输出是英文的。上面的描述对应可用的选项。

### 为什么是 `installerclean-cli` 而不是 `installerclean.exe`？

`InstallerClean.exe` 是 WPF 图形界面，不响应命令行参数。`installerclean-cli.exe` 是单独的控制台可执行文件，与图形界面在同一安装目录下，将相同的扫描、移动、删除操作暴露给 PowerShell、cmd 和计划任务。因为它是真正的控制台进程，会阻塞命令提示符直到完成；像其他控制台 exe 一样可以重定向或管道输出。

portable 和 slim 下载只包含图形界面 exe。如果您想要不带图形界面的命令行，请从[发布页面](../../releases/latest)下载 `installerclean-cli.exe` 直接运行。setup 也会把它和图形界面一起安装。

## 系统要求

- Windows 10 或 11
- 管理员权限（`C:\Windows\Installer` 仅限管理员访问）

setup、portable、slim 和 CLI 各构建选项见[下载](#下载)。

## 从源码构建

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean/InstallerClean.csproj
```

运行测试：

```
dotnet test src/InstallerClean.Tests/
```

## 参与贡献

发现 bug 或有建议？[提交 issue](../../issues) 或开启[讨论](../../discussions)。欢迎 pull request。提交前请先运行 `dotnet test`。

## 支持本项目

如果 InstallerClean 帮到了您，欢迎[支持 No Faff](https://nofaff.netlify.app/support)，或在 GitHub 上点亮一颗 star。

## Star 历史

<a href="https://www.star-history.com/?repos=no-faff%2FInstallerClean&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
   <img alt="Star 历史图表" src="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
 </picture>
</a>

## 许可证

[MIT](LICENSE)
