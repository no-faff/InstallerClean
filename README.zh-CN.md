<p align="center">
  <a href="README.md">English</a> · <strong>简体中文</strong> · <a href="README.es.md">Español</a> · <a href="README.ja.md">日本語</a> · <a href="README.pt-BR.md">Português (BR)</a> · <a href="README.ru.md">Русский</a> · <a href="README.fr.md">Français</a> · <a href="README.it.md">Italiano</a>
</p>

<p align="center"><em>本页面为译文，应用程序的界面目前仅提供英文。</em></p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<p align="center"><em>🎶 What's my line? I'm happy <a href="https://www.youtube.com/watch?v=HM-jHhUZfFI">cleaning Windows</a></em></p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong>一款开源工具，安全清理 <code>C:\Windows\Installer</code>，这个悄悄蚕食您磁盘空间的隐藏 Windows 文件夹。</strong></p>

<p align="center"><em>用上一次。说不定能省点空间。然后随手扔掉。</em></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-blue.svg" alt="许可证：MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/badge/release-v1.9.1-blue" alt="GitHub 版本"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/downloads-22k-brightgreen" alt="总下载量"></a>
</p>

![InstallerClean 成功清理后的截图：已释放 1.28 GB，69 个文件已送进回收站](docs/screenshots/06-freed-success-done.webp)

- **简介：** InstallerClean 只做一件事：清除 `C:\Windows\Installer` 里不需要的文件，这是个 Windows 从不清理的隐藏文件夹。它几乎瞬间扫描完，告诉您有没有这类文件，想细看的还能查看更多细节，并让您删掉它们，给 C: 盘腾出空间。用上一次，就可以不再惦记了。
- **能释放多少空间：** 目前收到的（自愿）报告显示，<!-- reports-freedpct-start -->41%<!-- reports-freedpct-end --> 的机器有需要清理的文件。这些机器里，释放空间的中位数是 <!-- reports-median-start -->22 GB<!-- reports-median-end -->。有几台清出了好几百 GB。我自己是 1.28 GB。其余 <!-- reports-nothingpct-start -->59%<!-- reports-nothingpct-end --> 没找到可清理的，这只是说明它们的 Installer 文件夹本来就是干净的。更多细节见下文的[常见问题](#常见问题)。
- **是否安全：** 是的。它直接问 Windows Installer API 哪些文件还需要，只会列出 Windows 报告为已经用完的那些。它是开源的（MIT），不向您索取任何信息：没有账号、没有广告、没有跟踪、没有遥测，也没有任何东西在后台运行。它从不自行联网。
- **如何获取：** [下载最新版本](../../releases/latest)。运行；点过 [Windows 的警告](#unknown-publisher)和[管理员提示](#admin)。删掉任何不需要的文件。搞定。

## 目录

- [没有人告诉您的文件夹](#没有人告诉您的文件夹)
- [寻求帮助](#寻求帮助)
- [它做什么](#它做什么)
- [截图](#截图)
- [工作原理](#工作原理)
- [是否安全？](#是否安全)
- [万一您真的丢了 C:\Windows\Installer 里的文件](#recovery)
- [无障碍](#无障碍)
- [它不做什么](#它不做什么)
- [常见问题](#常见问题)
- [下载](#下载)
- [与 PatchCleaner 对比](#与-patchcleaner-对比)
- [命令行](#命令行)
- [系统要求](#系统要求)
- [从源码构建](#从源码构建)
- [参与贡献](#参与贡献)
- [支持本项目](#支持本项目)
- [Star 历史](#star-历史)
- [许可证](#许可证)

---

## 没有人告诉您的文件夹

每台 Windows 电脑上都有一个名为 `C:\Windows\Installer` 的隐藏文件夹。每当您安装使用 Windows Installer 机制的软件，或给 Microsoft Office、Adobe Acrobat、Visual Studio 等任何基于 `.msi` 的应用打补丁时，安装程序或 `.msp` 补丁文件的副本就会进入这个文件夹。然后就一直留在那里。

卸载软件时，这些文件还在。新补丁取代旧补丁时，两个都还在。Windows 从不清理它们。磁盘清理碰都不碰。DISM 针对的完全是另一个文件夹。时间一长，文件夹越来越大：1 GB、5 GB、20 GB、50 GB。在大量使用 MSI 软件的电脑上（Acrobat 是常见的元凶），它能[突破 100 GB](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/)。

这些不是那种会自己重新冒出来的临时文件。它们是实打实的累赘：多年前卸载的软件留下的旧安装程序，以及被替换了好几次的补丁。一旦清掉，就不会再回来。

**如果您想找个简单的办法给 Windows 腾出磁盘空间，这个文件夹是个不错的起点。** InstallerClean 找出这些不需要的文件，安全地删掉。

## 寻求帮助

只要您为这个文件夹找过帮助，多半就知道是怎么个套路。有人的 `C:\Windows\Installer` 有 180 GB，问怎么清理，[得到的回答是运行磁盘清理](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb)。一试，清出了 600 MB，但没有一点来自那个文件夹（因为磁盘清理根本不碰 `C:\Windows\Installer`）。然后帖子就没了下文。

> *“我找到的帖子几乎都在翻来覆去地推荐同样那几招，根本解决不了问题，然后就没了动静。”*
>
> [ksparks519, r/Windows10](https://www.reddit.com/r/Windows10/comments/1bt8c5p/anyone_ever_figure_out_giant_installer_folders/)（译自英文原帖）

要么就是干脆叫人别碰它。在一个帖子里，有人的 Installer 文件夹有 60 GB，得到的回复是[“别去动它”](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/)。当他追问那到底该怎么做时，对方的回答是：“我刚才不就说了吗。”

主流的建议把两件事混为一谈：一是随手乱删文件（这确实危险），二是删除 Windows 自己都说不再需要的文件（这其实并不危险）。InstallerClean 做的是后者。

## 它做什么

1. **扫描** `C:\Windows\Installer`，找出其中的 `.msi` 和 `.msp` 文件
2. **查询** Windows Installer API，确定哪些文件仍处于注册状态
3. **显示** 能释放多少、还需要多少，并提供可选的详情窗口逐一列出每个文件
4. **清除** 不需要的文件：删除到回收站，或移动到您选择的文件夹

## 截图

<p>
  <img src="docs/screenshots/01-initial-scan.webp" alt="启动画面，扫描运行时显示 InstallerClean 徽标" width="900"><br>
  <em>初始扫描，非常快。</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/02-main-window.webp" alt="主窗口，显示 120 个文件仍需保留（2.83 GB），69 个不需要的文件可以清理（1.28 GB），还有一个移动位置输入框，以及“删除”和“移动”按钮" width="900"><br>
  <em>结果：还需要多少，能清理多少。</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/03-details-registered.webp" alt="已注册文件窗口，列出已安装的产品，并显示所选产品的安装数据库详情" width="900"><br>
  <em>仍需保留的文件的详情，附带从安装数据库读取的元数据。</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/04-details-safe-to-delete.webp" alt="不需要的文件窗口，按大小排序列出可删除的 .msi 文件，给出每个文件可删除的原因，并显示所选文件的详情" width="900"><br>
  <em>不再需要的文件的详情。</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/05-delete-dialogue.webp" alt="删除确认对话框，询问是否删除 69 个文件（1.28 GB），并说明这些文件将被送进回收站" width="900"><br>
  <em>每次操作前都会确认。删除会送进回收站；移动则把文件放到您选择的位置。</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/06-freed-success-done.webp" alt="删除成功后的提示界面，显示已释放 1.28 GB，69 个文件已送进回收站" width="900"><br>
  <em>一次成功的删除之后。</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/07-scanned-again-all-clean.webp" alt="再次扫描后的“全部干净”提示界面：C:\Windows\Installer 中已无可清理项" width="900"><br>
  <em>再次扫描之后。已经没有什么可清理的了。</em>
  <br><br>
</p>

## 工作原理

InstallerClean 会识别三类不需要的文件。

**孤立文件**是卸载软件后遗留下来的 `.msi` 安装程序（以及任何 `.msp` 补丁）。Windows 不再引用它们，但文件仍留在文件夹里占着空间。

**被取代的补丁**是被更新版本替换掉的旧 `.msp` 补丁。Windows 在自己的数据库里把它们标记为已被取代，却从不删除。频繁发布补丁的厂商（Acrobat、Office、大型开发工具）会无止境地累积这类补丁。

**已废弃的补丁**是发布者撤回或弃用、而非用更新版本替换掉的 `.msp` 补丁。Windows 同样会记录这个状态，也同样把文件留在文件夹里。

为了找出它们，InstallerClean 通过 P/Invoke 直接调用 Windows Installer 的 COM 接口：

- `MsiEnumProductsEx` 枚举每一个已安装的产品
- `MsiEnumPatchesEx` 找出每个产品所有已注册的补丁
- `MsiGetPatchInfoEx` 读取补丁状态（已应用、已被取代或已废弃）

`C:\Windows\Installer` 中任何不被已注册产品认领的 `.msi` 或 `.msp` 文件，都是孤立文件，会被标记为可删除。任何被数据库标记为已被取代或已废弃、且卸载时不需要的补丁，也是如此。

如果 API 返回的数据不完整（少见，但安装程序状态损坏时可能发生），应用会退回到读取注册表。这一回退只会把文件加入“仍需保留”的集合，绝不会加入“可删除”的集合。

一次移动或删除完成后，`C:\Windows\Installer` 内的空子文件夹（缓存内容清空后残留下来的目录）会在同一轮里一并清理。

## 是否安全？

是的。InstallerClean 查询的，正是 Windows 自己用来记录已安装内容的那个 Windows Installer API 数据库。如果 Windows 说某个文件不再需要，应用就信它；它不会根据文件名或日期来猜测。

**关于删除和移动。** InstallerClean 删除的这些文件，永久删掉也是安全的。**删除**会把它们送进回收站（回收站不可用时会有提示）；等您清空回收站，C: 盘上的空间就回来了。

不过，这些文件能否放心删除，您不必只听我说。趁它们还在回收站里，您有机会确认一下：用到这个文件夹的那些应用（Office、Acrobat、Visual Studio 之类）是否还能照常更新和卸载。万一有什么坏了（不会的！），从回收站把文件还原回去就能修好。想格外稳妥，也可以改用**移动**，把文件停放到您选定的文件夹里（当然，如果您是想给 C: 盘腾空间，就该选另一个分区或驱动器上的文件夹）。想恢复原状，把文件复制回 `C:\Windows\Installer` 就行（不过您用不着这么做！）。

如果 Windows Installer 正在写入缓存、有上一笔事务被挂起，或有一项排队等候的、指向该缓存的重启后重命名，移动和删除都会被禁用，并显示具体原因。

扫描、查询、移动、删除、设置和待重启检查这些服务，都有一套自动化测试覆盖，每次提交都会运行（见上方的 CI 徽章）。

**验证二进制文件。** InstallerClean 没有数字签名，所以您不必只凭信任：

- 每个版本的 SHA-256 哈希值都列在[发布页面](../../releases/latest)上。
- VirusTotal：所有引擎检测均为干净。每个版本的发布说明里都有实时链接，方便您自行复查。
- 源代码在 [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean)，CI 会对每次提交进行构建和测试（见上方绿色的 CI 徽章）。
- 在 GitHub、MajorGeeks 和 Softpedia 上累计 <!-- downloads-start -->22k<!-- downloads-end --> 次下载。
- [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) 会在虚拟机中测试每一个提交上来的版本，只有通过审核才会收录。
- [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) 会对每个版本做病毒、间谍软件和广告软件检测。

<a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Softpedia 认证 100% 干净" width="190"></a>

<a id="recovery"></a>
## 万一您真的丢了 `C:\Windows\Installer` 里的文件

InstallerClean 只会删除 Windows 自己报告为不再需要的文件，所以它绝不可能是某个文件丢失的原因。但如果已经有文件丢了，InstallerClean 会发现并标记出来。下面就是解决办法。

从受影响程序的厂商那里下载它的安装程序，然后在您现有的安装之上运行一遍；不要先卸载。如果可以，请使用您现在装的那个版本，因为 Windows 可能会拒绝另一个版本。这通常会把文件放回去，并且不动您的设置。在 InstallerClean 里重新扫描一次，如果成功了，那条警告就会消失。

这办法通常管用。下面是 Microsoft 自己更完整的说法：官方的细节，以及没那么简单时的疑难情形。这些都不是 InstallerClean 造成的，我也无法在 Microsoft 的指引之上做得更好，所以只是如实转达。

<details>
<summary>Microsoft 更完整的说法</summary>

*以下 Microsoft 引文均为英文原文。*

完整指引：[Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache)。

*可能不会立刻显现：*
> "If the installer cache is compromised, you may not immediately see problems until you take an action such as uninstalling, repairing, or updating a product."

*这些文件每台机器各不相同，没法从别的电脑拷一个过来：*
> "Missing files cannot be copied between computers because the files are unique."

*也没法只从备份里单独取回那个文件：*
> "To restore the missing files, a full system state restoration is required. It is not possible to replace only the missing files from a previous backup."

*推荐的恢复办法，以及它直白的局限：*
> "If application files are missing from the Windows Installer Cache, ask the vendor or support team for the application about the missing files. You must follow the procedures or steps recommended by the application vendor to restore the files. In some cases, you may have to rebuild the operating system and reinstall the application to fix the problem."
>
> "Windows support engineers cannot help you recover missing application files from the Windows Installer cache."

*为什么版本要一致：*
> "The upgrade cannot be installed by the Windows Installer service because the program to be upgraded may be missing, or the upgrade may update a different version of the program."

</details>

## 无障碍

InstallerClean 在设计上力求完全能用键盘和屏幕阅读器操作。

- **全程可用键盘操作。** Tab 键能到达每一个控件，详情窗口里的各列也能用键盘排序，所以这里没有任何地方非用鼠标不可。键盘焦点无论落在哪里都保持可见。
- **讲述人与语音访问。** 每个控件都有标签，按钮上看得见的那个词，就是用语音激活它时要说的词。移动或删除完成后，结果会朗读出来。
- **为阅读而打造。** 在整个深色主题中，文字都达到 WCAG AA 对比度标准。

如果这里有任何地方妨碍到您，请[提交 issue](../../issues)。无障碍问题属于 bug，而不是边缘情况。

## 它不做什么

- WinSxS（`C:\Windows\WinSxS`）是另一个文件夹，规则也不同。要清理那个，请在管理员命令提示符里运行 `Dism /Online /Cleanup-Image /StartComponentCleanup`。
- 没有后台服务，没有计划任务，没有自动清理。应用只在您启动它时运行。
- 注册表是只读的。应用查询 Windows Installer 数据库；它不会改动数据库。
- 它只在您主动要求时才联网：手动检查更新；可选的匿名摘要（只是让我知道它在正常工作）；以及指向 GitHub 文档和捐赠页面的链接，您点了才会在浏览器里打开。
- 没有工具栏，没有捆绑软件，没有广告软件。

## 常见问题

**我真能腾出好几 GB 空间吗？** 看您的机器。一台没装额外软件的全新 Windows 11，没有什么可清理的。一台用了很久的开发者工作站，或任何装了大量基于 MSI 的软件（Acrobat、Office、LibreOffice、大型开发工具）的机器，可能有几十 GB。无论是哪种情况，一运行您就能看到具体有多少。

<!-- reports-stats-start (generated by non-repo-files/refresh-reports-table.mjs; do not hand-edit between these markers) -->
自 v1.8.0 加入这个选项以来，热心的用户已经发来 83 份报告（谢谢 🙏），汇总如下：

| 结果 | 占比 | 最小 | 中位数 | 最大 |
|---|---|---|---|---|
| 无可清理 | 59% | - | - | - |
| 已释放空间 | 41% | 0.2 GB | 22 GB | 327 GB |
<!-- reports-stats-end -->

<details>
<summary>这些报告来自可选的“发送摘要”按钮。下面是发送前您会看到的内容。</summary>

![确认对话框，标题为“把这个发送给 No Faff 吗？”，显示将要发送的完整报告：应用版本、Windows 版本、扫描计数、处理的文件数和释放的字节数，不含任何文件路径、名称或机器标识，并附一句说明：没有任何内容能识别您或您的机器，只有应用是否正常工作以及释放了多少空间，下方是“取消”和“发送”按钮。](docs/screenshots/optional-send-summary-confirmation-dialogue.webp)

</details>

<a id="admin"></a>

**为什么需要管理员权限？** `C:\Windows\Installer` 被锁定为仅限管理员访问。读取它、查询 Installer 数据库、移动或删除文件，都需要管理员权限，所以应用必须以管理员身份运行。

<a id="unknown-publisher"></a>

**为什么 Windows 说“未知发布者”？** 因为 InstallerClean 没有代码签名。签名证书每年都要花钱，我宁愿让应用保持免费，也不想为它掏钱。所以您运行它时，Windows SmartScreen 会显示“Windows 已保护你的电脑”。点击**更多信息**，再点**仍要运行**。这么做是安全的：源代码是公开的，每个版本都有 VirusTotal 链接和 SHA-256 哈希值供您事先核对。

**删除能撤销吗？** 通常可以。当该驱动器的回收站可用时，删除会把文件送进回收站，您可以从那里还原。如果回收站不可用，应用绝不会自行把文件彻底删除（见[是否安全？](#是否安全)）。如果您更想要一条自己掌控的退路，移动会把文件放进您选择的文件夹；等您满意了，再从那里删除。

**删除这些文件，Windows 会不会有意见？** 不会。InstallerClean 只会删除 Windows 自己报告为已经用完的文件，所以它删掉的东西，没有一样是修复、更新或卸载某个程序所需要的。如果某个需要的文件确实因为别的原因从 `C:\Windows\Installer` 里消失了，请见[万一您真的丢了 C:\Windows\Installer 里的文件](#recovery)。

**为什么不用 `Win32_Product`（WMI）？** [`Win32_Product` 在枚举时会对每个产品触发 MSI 修复操作](https://gregramsey.net/2012/02/20/win32_product-is-evil/)，可能要花上好几分钟，还会让磁盘满负荷。InstallerClean 直接调用 Windows Installer 的 COM API，没有任何副作用。

**为什么不直接写个 PowerShell 脚本？** 一段调用 `MsiEnumPatchesEx` 的短脚本，足以把补丁*列出来*，但 InstallerClean 真正吃重的部分，恰恰是脚本会一笔带过的地方：孤立与被取代的分类、只往“仍需保留”集合（绝不往“可删除”集合）添加文件的注册表回退、待重启时的阻断、移动到别处的安全网、可随时取消的逐文件进度，以及“默认送回收站、不做永久删除”的默认行为。在真正大量使用 MSI 的机器上，各种边角情况（注册信息损坏、缓存里的目录联接、`HKU\.DEFAULT` 下的产品、被挂起的 Installer 事务）在一次性脚本里很容易处理不当。如果您要的就是脚本能力，`installerclean-cli` 就是它无界面的那一面。

**支持 Windows 7 或 8 吗？** 未经测试，也不支持。目标平台是 Windows 10 和 11。

**适合 RMM 或批量部署吗？** 适合。CLI 会按不同结果返回不同的退出码（0 成功、2 部分成功、1 严重失败、75 临时性问题、130 表示在处理任何文件之前按下了 Ctrl+C；若 Ctrl+C 发生在批处理中途，则返回 2 部分成功，因为已经有改动落地），这样计划任务就能在遇到 75 时重试，而不会把它和严重失败混为一谈。它会向应用程序事件日志写入一份每次运行的摘要，并遵循与图形界面相同的单实例互斥锁。setup 也支持用标准的 Inno Setup 开关（`/SILENT` 或 `/VERYSILENT`）静默安装；静默安装时会跳过安装后的自动启动。详见命令行一节。

## 下载

三种构建，任选其一：

- **Setup**（`InstallerClean-setup.exe`）：标准的 Windows 安装程序，内置 .NET 10 运行时。会在开始菜单里添加条目，也能干净地卸载。安安稳稳待在“程序”列表里，半年后也好找。
- **Portable**（`InstallerClean-portable.exe`）：单个自包含的 exe，运行时已打包在内。无需安装，没有卸载程序。运行、使用、删掉。想用了随时再运行。
- **CLI**（`installerclean-cli.exe`）：单独的命令行版本，一个自包含的 exe。无需安装，用完不在机器上留下任何东西。把它丢到客户端机器上，跑一次扫描或清理，再删掉。专为脚本、计划任务和批量部署而生，适合那种想执行操作、又不想在客户端装桌面应用的场景。参数和退出码见[命令行](#命令行)。

从[发布页面](../../releases/latest)下载，然后运行。它没有签名，所以 Windows 会显示一条“未知发布者”的警告；[常见问题](#unknown-publisher)解释了您会看到什么，以及为什么它是安全的。

应用启动时会自动扫描。查看结果，然后点击**删除**或**移动**。

或者通过 [winget](https://learn.microsoft.com/windows/package-manager/winget/) 安装：

```
winget install NoFaff.InstallerClean
```

或者通过 [Scoop](https://scoop.sh) 安装：

```
scoop bucket add no-faff https://github.com/no-faff/scoop-bucket
scoop install installerclean
```

## 与 PatchCleaner 对比

如果您以前搜过这个文件夹，最可能找到的工具就是 [PatchCleaner](https://www.homedev.com.au/free/patchcleaner)。它至今仍然好用，但我做 InstallerClean，是因为 PatchCleaner 不开源、自 2016 年 3 月起就没再更新，而且默认不碰 Adobe 产品。它的孤立检测把 Adobe 的补丁误判了，删掉那些补丁会弄坏 Adobe 的更新，所以除非您关掉那个过滤器，它会把所有 Adobe 文件都放着不动。而在 Adobe 是最大占用源的机器上，那部分正是大头：

> *“我下载了 PatchCleaner 来删除那些孤立的 .msp 文件，但据说这样只能释放 250 MB 的空间。有 29 GB 的文件被‘过滤器排除’了，所以 PatchCleaner 似乎帮不上忙。”*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/)（译自英文原帖）

InstallerClean 读取的是 Windows Installer 自己的补丁记录，所以它能分辨出哪些 Adobe 补丁是真正被取代了，把那些安全地清掉，无需一刀切的过滤器。两者对比如下：

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| 最近更新 | 2026（活跃维护中） | 2016 年 3 月 3 日 |
| 源代码 | 开源（MIT） | 闭源 |
| 运行时 | .NET 10（自包含） | .NET + VBScript |
| API | Windows Installer COM（进程内） | Windows Installer COM（进程外，经 VBScript） |
| 被取代补丁检测 | 有 | 无 |
| Adobe 处理 | 检测被取代的补丁 | 默认排除 |
| 界面 | 深色主题（WPF） | Windows 窗体 |
| 数据收集 | 无 | 无 |
| 删除安全性 | 回收站。若回收站不可用，会询问：改为移动，还是永久删除 | 永久删除，不经回收站 |

> **关于 `Win32_Product` 的说明：** 列出已安装产品有一种常见却有缺陷的做法，就是用 `Win32_Product`（WMI），它会在枚举时[对每个产品触发 MSI 修复操作](https://gregramsey.net/2012/02/20/win32_product-is-evil/)。InstallerClean 和 PatchCleaner 都避开了它，两者用的都是 Windows Installer COM 接口。PatchCleaner 脚本里的 `WMIProducts.vbs` 这个文件名有误导性；脚本实际用的是 MSI COM，而不是 WMI。

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) 也在它的 System Booster 模块里提供 Installer 清理，但那是一款付费工具（15 到 25 美元），清理只是一个大得多的应用里的一个小功能。InstallerClean 免费、专注、开源。

[CCleaner](https://www.ccleaner.com/) 和 [BleachBit](https://www.bleachbit.org/) 这类通用系统清理工具不会碰 `C:\Windows\Installer`。要把已注册的包和不需要的文件区分开，这个文件夹需要调用 Windows Installer API 来查询，而仅仅遍历文件树的通用清理工具，可能会弄坏已安装的应用。当您真正想清理的就是这个文件夹时，InstallerClean 就是该用的工具。

## 命令行

InstallerClean 支持无界面运行，方便编写脚本和系统管理使用：

```
用法：
  installerclean-cli --help   显示帮助（也接受 /?、-h）
  installerclean-cli --version  显示版本（也接受 -v）
  installerclean-cli /s       仅扫描，列出可删除的文件
  installerclean-cli /d       删除可删除的文件（送回收站）
  installerclean-cli /m       移动到已保存的默认位置
  installerclean-cli /m PATH  移动到指定路径
```

要启动图形界面，请运行 `InstallerClean.exe`（或使用 setup 安装后在开始菜单里的快捷方式）。

不带参数运行，或给出无法识别的选项，`installerclean-cli` 会打印这段用法说明并以 `1` 退出，这样一来，丢了选项的计划任务会明明白白地失败，而不是悄无声息地“成功”却什么都没做。而显式给出 `--help`、`/?` 或 `-h`，则会打印同样的用法说明并以 `0` 退出。

`/s` 是一次试运行：它会扫描、列出将要删除的文件名和大小，然后退出。便于在清理前做一次审查。退出码：扫描成功为 `0`，扫描失败为 `1`，按下 Ctrl+C 为 `130`。所有文件都在 `C:\Windows\Installer` 中。

`/d` 和 `/m` 先扫描再执行。`/d` 把可删除的文件送进回收站。`/m` 把它们移动到一个文件夹（要么是您在命令行上指定的，要么是图形界面里保存的默认位置）。退出码：`0` 表示完全成功，`2` 表示部分成功（部分文件成功，部分失败），`1` 表示彻底失败（扫描失败、参数有误，或本批文件全部失败），`75` 表示有临时性状况挡住了这次运行（打印出来的消息会说明是哪种状况，以及重试是否有用），`130` 表示在处理任何文件之前按下了 Ctrl+C（若 Ctrl+C 发生在批处理中途，则返回 `2` 部分成功，因为已经有改动落地）。

CLI 的所有输出，包括错误和诊断信息，都写到 stdout；没有单独的 stderr 流。退出码才是供机器读取的信号（每次运行写入应用程序事件日志的条目也与它一致），所以脚本应当依据退出码来判断，而不是去解析文本；`installerclean-cli /s > audit.txt` 会把整次运行都捕获下来，包括任何错误行。

这三个命令都需要一个已提权的（管理员）命令提示符。如果组策略拦截了 UAC 提权提示，进程会拒绝启动，Windows 会向父 Shell 返回错误码 740（在 PowerShell 中即 `$LASTEXITCODE = 740`）。`taskkill /pid <pid>` 不会触发优雅取消；单实例互斥锁会由下一次运行通过 AbandonedMutexException 路径恢复。

说明：CLI 自身的输出是英文的，上面的描述对应的是各个可用选项。

### 为什么是 `installerclean-cli` 而不是 `installerclean.exe`？

`InstallerClean.exe` 是 WPF 图形界面，它不响应命令行参数。`installerclean-cli.exe` 是另一个独立的控制台程序，与图形界面装在同一个目录下，把相同的扫描 / 移动 / 删除操作开放给 PowerShell、cmd 和计划任务。因为它是一个真正的控制台进程，会阻塞命令行直到运行结束；可以像对待任何其他控制台 exe 一样，对它的输出做重定向或管道。

portable 下载只包含图形界面 exe。如果您想要不带图形界面的命令行，请从[发布页面](../../releases/latest)下载 `installerclean-cli.exe` 直接运行。setup 也会把它和图形界面一起装上。

## 系统要求

- Windows 10（版本 1607 / 内部版本 14393 或更高，这是 .NET 10 运行时支持的最低版本）或 Windows 11
- 管理员权限（`C:\Windows\Installer` 仅限管理员访问）

setup、portable 和 CLI 各种构建的选项见[下载](#下载)。

## 从源码构建

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean.sln
```

运行测试：

```
dotnet test src/InstallerClean.Tests/
```

## 参与贡献

发现了 bug，或有什么建议？[提交 issue](../../issues) 或发起[讨论](../../discussions)。欢迎 pull request。提交前请先运行 `dotnet test`。

## 支持本项目

如果 InstallerClean 帮到了您，欢迎[支持 No Faff](https://nofaff.netlify.app/support)，或在 GitHub 上点个 star。

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

---

🎶 [George Formby - When I'm Cleaning Windows](https://www.youtube.com/watch?v=sfmAeijj5cM). 点开看看吧！
