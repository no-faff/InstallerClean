<p align="center">
  <strong>English</strong> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.ru.md">Русский</a> · <a href="README.es.md">Español</a> · <a href="README.ar.md">العربية</a> · <a href="README.ja.md">日本語</a> · <a href="README.pt-BR.md">Português (BR)</a> · <a href="README.pl.md">Polski</a> · <a href="README.tr.md">Türkçe</a> · <a href="README.ko.md">한국어</a> · <a href="README.fr.md">Français</a> · <a href="README.it.md">Italiano</a> · <a href="README.de.md">Deutsch</a> · <a href="README.id.md">Bahasa Indonesia</a> · <a href="README.vi.md">Tiếng Việt</a> · <a href="README.uk.md">Українська</a> · <a href="README.nl.md">Nederlands</a>
</p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<p align="center"><em>🎶 What's my line? I'm happy <a href="https://www.youtube.com/watch?v=HM-jHhUZfFI">cleaning Windows</a></em></p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong>An open-source tool to safely clean up <code>C:\Windows\Installer</code>, the hidden Windows folder that quietly eats your disk space.</strong></p>

<p align="center"><em>Use it once in a blue moon. Maybe save some space. Move on, feeling clean.</em></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-Apache_2.0-blue.svg" alt="Licence: Apache 2.0"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/badge/release-v2.3.0-blue" alt="GitHub Release"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/downloads-58k-brightgreen" alt="Total downloads"></a>
</p>

![Screenshot of InstallerClean after a successful clean-up: 1.28 GB cleaned up, 68 files moved to the Recycle Bin](docs/screenshots/en/07-success-done.webp)

- **What:** InstallerClean does one thing: it removes unneeded files from `C:\Windows\Installer`, a hidden folder Windows never cleans up. After a nearly instant scan it tells you whether you have any, shows more detail for the curious, and lets you delete them to free up space on your C: drive. You use it once and move on.
- **You might be here because:** You used [WinDirStat](https://github.com/windirstat/windirstat), WizTree or TreeSize, saw `C:\Windows\Installer` taking up a lot of space and didn't know what was in there. InstallerClean is just what you need. It knows what's in those files with random-looking names like `9f05cba.msi` and quickly tells you which ones you can safely delete.
- **How much space:** The (optional and anonymous) reports sent in so far show <!-- reports-freedpct-start -->62%<!-- reports-freedpct-end --> of machines had unneeded files to clean. Of those, the median freed is <!-- reports-median-start -->16.8 GB<!-- reports-median-end --><!-- reports-biggest-start --> - with one machine reclaiming a whopping 462 GB<!-- reports-biggest-end -->. The other <!-- reports-nothingpct-start -->38%<!-- reports-nothingpct-end --> found nothing to remove, which just means their Installer folder was already clean. More detail in the [FAQ](#faq) below.
- **Is it safe:** Yes. It asks the Windows Installer API itself which files are still needed and only ever lists the ones Windows reports as finished with. It's open source (Apache 2.0) and asks nothing about you: no account, no ads, no tracking, no telemetry, nothing running in the background. The only thing it does online by itself is check GitHub for a newer version when you run it, and you can turn that off.
- **Get it:** [Download the latest release](../../releases/latest). Run it; click through [the unknown-publisher warning](#unknown-publisher) and [the admin prompt](#admin). Delete any unneeded files. Done.

## Contents

- [The folder nobody tells you about](#the-folder-nobody-tells-you-about)
- [The search for help](#the-search-for-help)
- [What it does](#what-it-does)
- [Screenshots](#screenshots)
- [How it works](#how-it-works)
- [Is it safe?](#is-it-safe)
- [Code signing policy](#code-signing-policy)
- [If you do have a file missing from C:\Windows\Installer](#recovery)
- [Accessibility](#accessibility)
- [What it doesn't do](#what-it-doesnt-do)
- [FAQ](#faq)
- [Download](#download)
- [Compared to PatchCleaner](#compared-to-patchcleaner)
- [Command line](#command-line)
- [Requirements](#requirements)
- [Building from source](#building-from-source)
- [Contributing](#contributing)
- [Support the project](#support-the-project)
- [Star history](#star-history)
- [Licence](#licence)

---

## The folder nobody tells you about

There's a hidden folder on every Windows PC called `C:\Windows\Installer`. Every time you install software that uses the Windows Installer system, or apply a patch to Microsoft Office, Adobe Acrobat, Visual Studio or any other `.msi`-based application, a copy of that installer or `.msp` patch file goes into this folder - and stays there.

When you uninstall the software, the files stay. When a newer patch replaces an older one, both stay. Windows never cleans them up. Disk Cleanup doesn't touch them. DISM is for a different folder entirely. Over time, the folder grows: 1 GB, 5 GB, 20 GB, 50 GB. On machines with heavy MSI-using software (Acrobat is a frequent culprit), it can [pass 100 GB](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/).

These aren't temp files that come back on their own. They're genuine dead weight: old installers from software you uninstalled years ago and patches that have been replaced several times over. Once they're gone, they don't come back.

**If you're looking for an easy way to free up disk space on Windows, this folder is a good place to start.** InstallerClean finds the unneeded files and removes them safely.

## The search for help

If you've ever searched for help with this folder, you probably know how it goes. Someone with 180 GB in `C:\Windows\Installer` asks how to clean it. They're [told to run Disk Cleanup](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb). They try it. It clears 600 MB, none of it from that folder (because Disk Cleanup doesn't touch `C:\Windows\Installer`). The thread goes quiet.

> *"All of the threads I've found tend to recommend the same things which don't solve the problem, and then go dead."*
>
> [ksparks519, r/Windows10](https://www.reddit.com/r/Windows10/comments/1bt8c5p/anyone_ever_figure_out_giant_installer_folders/)

Or they're told not to touch it at all. In one thread, someone with a 60 GB Installer folder was told to ["don't mess with it."](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/) When they asked what they should do instead, the reply was: *"I just told you."*

The standard advice confuses deleting files at random (which genuinely is dangerous) with removing files that Windows itself says it no longer needs (which isn't). InstallerClean does the latter.

## What it does

1. **Scans** `C:\Windows\Installer` for `.msi` and `.msp` files
2. **Queries** the Windows Installer API to find which files are still registered
3. **Shows** how much you can free and how much is still needed, with optional detail windows listing every file
4. **Removes** the unneeded files: delete permanently, or move to a folder you choose

## Screenshots

<p>
  <img src="docs/screenshots/en/01-initial-scan.webp" alt="Splash screen with the InstallerClean logo while the scan runs" width="900"><br>
  <em>Initial scan. This is very quick.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/en/02-main-window.webp" alt="Main window showing 138 files still needed (2.93 GB) and 68 unneeded files to clean up (1.28 GB), with a move location box and Delete and Move buttons" width="900"><br>
  <em>Results: how much is still needed, how much is removable.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/en/03-details-safe-to-delete.webp" alt="Unneeded files window listing removable .msi files sorted by size, with the reason each is removable and details for the selected file" width="900"><br>
  <em>Details of the files no longer needed.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/en/04-details-registered.webp" alt="Registered files window listing installed products, with installer-database details for the selected product" width="900"><br>
  <em>Details of the files still needed, with metadata read from the installer database.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/en/05-delete-dialog.webp" alt="Delete confirmation asking to delete 68 files (1.28 GB), noting the files will be moved to the Recycle Bin" width="900"><br>
  <em>Confirmation before either action. Delete moves to the Recycle Bin; Move puts the files somewhere of your choice.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/en/06-deleting.webp" alt="Progress overlay while the delete runs: 51 of 68 files done (75%), the file currently being deleted, and a Cancel button" width="900"><br>
  <em>The delete running. Cancel stops it part-way.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/en/07-success-done.webp" alt="Success overlay showing 1.28 GB cleaned up, with 68 files moved to the Recycle Bin" width="900"><br>
  <em>After a successful Delete.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/en/08-scanned-again-all-clean.webp" alt="All clean overlay after scanning again: nothing to clean up in C:\Windows\Installer" width="900"><br>
  <em>After scanning again. Nothing left to clean.</em>
  <br><br>
</p>

## How it works

InstallerClean identifies three kinds of unneeded files.

**Orphaned files** are the `.msi` installers (and any `.msp` patches) left behind after you uninstall software. Windows no longer references them, but the files sit in the folder taking up space.

**Superseded patches** are old `.msp` patches that have been replaced by newer ones. Windows marks them as superseded in its own database but never deletes them. Adobe is why this comes up so often: every Acrobat update ships as a patch against the same original installer rather than as a new installer of its own, so a machine ends up keeping one for every update it has ever had. Office and large dev tools build up the same way, more slowly.

**Obsoleted patches** are `.msp` patches the publisher has withdrawn or deprecated rather than replaced with a newer version. Windows records that state too, and likewise leaves the file in the folder.

To find them, InstallerClean calls the Windows Installer COM interface directly via P/Invoke:

- `MsiEnumProductsEx` to enumerate every installed product
- `MsiEnumPatchesEx` to find registered patches, both per product and across the whole machine
- `MsiGetPatchInfoEx` to read patch state (applied, superseded or obsoleted)
- `MsiGetProductInfoEx` to ask about one specific product code, rather than trusting a list to be complete

A file no registered product claims is a candidate, not a verdict. Before offering one, InstallerClean opens the file itself, reads the product or patch it says it belongs to, and asks Windows about that identity directly. Only a file Windows has no record of at all is offered. If the file won't say what it belongs to, or Windows can't be asked, the file stays where it is.

Superseded and obsoleted patches work the other way round, because there Windows has positively told the app they've been replaced. Each one is confirmed against every installed product before it's offered, so a product that still has the patch applied keeps its copy whatever any single list says.

The app also reads the same records straight from the registry on every scan, as a second, independent source. If either reading comes back incomplete (rare, but it can happen with a corrupted installer state), InstallerClean keeps files back or refuses the scan rather than guess. That second reading only adds files to the "still needed" set, never to the "removable" set.

After a Move or Delete completes, empty subfolders inside `C:\Windows\Installer` (the directories the cache leaves behind once their contents are gone) are pruned in the same pass.

<a id="is-it-safe"></a>
## Is it safe?

Yes. InstallerClean doesn't guess from filenames, dates or sizes. It asks Windows, twice over: once through the Windows Installer API and once by reading the registry directly. And it asks about each file's own identity rather than about where the file sits, so a registration written in a form it doesn't recognise can't make a needed file look removable. Anything it can't get a clear answer about stays where it is. Nothing has been reported broken after <!-- downloads-start -->58,000+<!-- downloads-end --> downloads.

**About Delete and Move.** Delete permanently removes the files. Move takes them out of `C:\Windows\Installer` to a folder you choose; put that folder on another drive and you get the space back on C: straight away and still have the files. Copy them back and you are exactly where you started.

If Windows Installer is currently writing to the cache, has a previous transaction suspended or has a queued post-reboot rename targeting the cache, Move and Delete are disabled and the specific reason is shown.

The scan, query, move, delete, settings and pending-reboot services are covered by an automated test suite that runs on every commit (see the CI badge above).

**Verifying the binary.** InstallerClean is unsigned, but you don't have to trust that it's safe:

- SHA-256 hashes for each release are listed on the [releases page](../../releases/latest).
- VirusTotal: every build is scanned, with the full per-engine results linked on its release page so you can see how each file scored and re-scan it yourself. A false positive that's live when a release goes out is named and explained on that release's page, and the page is updated once the vendor clears it.
- Source is at [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean) and CI builds and tests every commit (see the green CI badge above).
- Release builds are deterministic: the compiler settings make the same source and SDK produce the same bytes, and the release process refuses to tag a version unless the shipped exes were built from a clean tree at exactly that tag. So you can check out the tag, build it yourself and compare hashes with the published ones: the portable and the command-line downloads provably match the public source. The setup is compiled by Inno Setup rather than by the SDK and stamps the build year into itself, so reproducing its hash needs the same Inno version and the same calendar year as well. Match the SDK version first (each release's notes say which it was built with); a different SDK patch produces different bytes, which looks like a mismatch and isn't.
- <!-- downloads-start -->58,000+<!-- downloads-end --> downloads across GitHub, MajorGeeks and Softpedia.
- [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) tests each submission in a virtual machine and lists it only if it passes their review.<br><a href="https://www.majorgeeks.com/files/details/installerclean.html"><img src="docs/badges/majorgeeks-certified.webp" alt="MajorGeeks certified 100% clean" width="263"></a>
- [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) tests each release for viruses, spyware and adware.<br><a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Softpedia certified 100% clean" width="190"></a>

## Code signing policy

InstallerClean has applied to [SignPath Foundation](https://signpath.org) for free code signing, a programme that signs open source software so it stops arriving on your machine from an unknown publisher. The application is pending, so for now the downloads here are unsigned and Windows will warn about them.

If it's approved, releases will carry the line SignPath ask for: free code signing provided by SignPath.io, certificate by SignPath Foundation. The certificate belongs to the foundation rather than to me, because a certificate has to be issued to a legal entity and a one-person project isn't one. It doesn't mean InstallerClean is theirs, or that they're involved in it beyond the signing.

**Roles.** InstallerClean is maintained by one person, me, and I hold all of them:

- Committers and reviewers, meaning who can put code into the project: me. Every pull request is reviewed before it's merged.
- Approvers, meaning who can authorise a release to be signed: me.

**Privacy.** I don't find out anything about you or your files - unless you choose to send the totally optional anonymous report, which just lets me know it's working. No ads, no telemetry. The only other connections are a version check when the app starts (one request to GitHub which you can turn off in the About window) and buttons linking to GitHub and a page where you can donate if you're feeling generous. Full [privacy policy](PRIVACY.md).

<a id="recovery"></a>
## If you do have a file missing from `C:\Windows\Installer`

InstallerClean only removes files Windows has no record of needing. If one has gone missing, InstallerClean spots it and flags it. The steps below are the same whatever the cause, InstallerClean included.

Download that program's installer from its maker and run it over your existing installation; don't uninstall first. Use the version you have now if you can, because Windows may turn down a different one. This should restore the file and leave your settings alone. Re-scan in InstallerClean and the warning will be gone if it worked.

That usually works. What follows is Microsoft's own, fuller account: the official detail, and the harder cases for when it isn't that simple. None of it is InstallerClean's doing, and I can't improve on Microsoft's guidance, so I'm just passing it on.

<details>
<summary>Microsoft's fuller position</summary>

Full guidance: [Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache).

*It may not show up straight away:*
> "If the installer cache is compromised, you may not immediately see problems until you take an action such as uninstalling, repairing, or updating a product."

*The files are unique per machine, so you can't copy one from another PC:*
> "Missing files cannot be copied between computers because the files are unique."

*You can't pull just the file from a backup, either:*
> "To restore the missing files, a full system state restoration is required. It is not possible to replace only the missing files from a previous backup."

*The recommended recovery, and its blunt limits:*
> "If application files are missing from the Windows Installer Cache, ask the vendor or support team for the application about the missing files. You must follow the procedures or steps recommended by the application vendor to restore the files. In some cases, you may have to rebuild the operating system and reinstall the application to fix the problem."
>
> "Windows support engineers cannot help you recover missing application files from the Windows Installer cache."

*Why the same version matters:*
> "The upgrade cannot be installed by the Windows Installer service because the program to be upgraded may be missing, or the upgrade may update a different version of the program."

</details>

If InstallerClean is ever the reason a file is missing, I want to know. [Open an issue](../../issues) and I'll fix it.

## Accessibility

InstallerClean is built to be fully usable from the keyboard and with a screen reader.

- **Keyboard-operable throughout.** Everything the app does is reachable from the keyboard, and the detail-window columns sort from the keyboard too, so nothing here needs a mouse. The title-bar buttons behave like the Windows ones and are reached with Alt+Space or Alt+F4. Keyboard focus stays visible wherever it lands.
- **Narrator and Voice Access.** Every control is labelled, and the visible word on a button is the word that activates it by voice. When a Move or Delete finishes, the outcome is read aloud.
- **Built to be read.** Text meets WCAG AA contrast throughout the dark theme.

If anything here gets in your way, [open an issue](../../issues). Accessibility problems are bugs, not edge cases.

## What it doesn't do

- WinSxS (`C:\Windows\WinSxS`) is a different folder with different rules. For that one, run `Dism /Online /Cleanup-Image /StartComponentCleanup` from an elevated prompt.
- No background service, no scheduled task, no auto-clean. The app runs when you launch it.
- It doesn't change your installed programs or the Windows Installer database, only reads them. The only thing it ever writes to the registry is the one-time event-source registration the command-line tool needs so its runs can appear in the Windows Event Log.
- It makes one kind of connection off its own bat: a quick check of GitHub's releases page for a newer version when you run it, which you can switch off in About. Everything else only happens when you tell it to: the optional anonymous report (just to let me know it's working) and links to the GitHub docs and a donate page, which open in your browser if you click them. It never downloads anything by itself.
- No toolbars, no bundled software, no adware.

## FAQ

<a id="reports-stats"></a>
**Will I actually free up GBs of space?** It depends on your machine. A clean Windows 11 install with no extra software has nothing to remove. A long-running developer workstation, or any machine with heavy MSI-based software (Acrobat, Office, LibreOffice, large dev tools), can have tens of GB. Either way, you'll see exactly how much the moment you run it.

<!-- reports-stats-start (generated; do not hand-edit between these markers) -->
Since v1.8.0 there's been an option to send in a brief anonymous report of the outcome. 208 have come in so far (thanks everyone 🙏) and of the 62% of machines that had something to clean up, the median freed is 16.8 GB. One machine reclaimed a pretty staggering 462 GB. Here's a summary of the results.

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="docs/reports-en-dark.svg" />
    <source media="(prefers-color-scheme: light)" srcset="docs/reports-en-light.svg" />
    <img alt="Bar chart of how many machines had something to clean up and how much they freed" src="docs/reports-en-light.svg" width="800" />
  </picture>
</p>

Sending a report is a totally optional click of a button in the app. Nothing personal is included and it shows you exactly what will be sent, like this:

![Confirmation dialog titled "Send this?" showing the full report that would be sent: app version, Windows version, scan counts, files processed and bytes freed, with no file paths, names or machine IDs, and a note that nothing identifies you or your machine, just whether the app worked and how much space was freed, with Cancel and Send buttons.](docs/screenshots/en/optional-send-report-confirmation-dialog.webp)
<!-- reports-stats-end -->

<a id="admin"></a>

**Why does it want Administrator?** `C:\Windows\Installer` is locked down to administrators. Reading it, querying the Installer database and moving or deleting files all need that, so the app has to run as admin.

<a id="unknown-publisher"></a>

**Why does Windows say "Unknown publisher"?** InstallerClean isn't code-signed, and Windows marks files downloaded from the internet, so on first run SmartScreen usually shows "Windows protected your PC" with the publisher listed as unknown. A paid signing certificate costs money every year and I'd rather keep the app free than pay for one, so I've applied to SignPath Foundation, who sign open source software for nothing (see [Code signing policy](#code-signing-policy)). Until that comes through, click **More info**, then **Run anyway**. It's safe to do: the source code is public, and every release has VirusTotal links and SHA-256 hashes you can check first.

**Can I undo a Delete?** No. Delete removes the files permanently, behind a confirmation. If you want a way back you control, use Move instead: it puts the files in a folder you choose, and you delete them from there whenever you're satisfied (see [Is it safe?](#is-it-safe)).

**Will Windows complain if I remove these files?** No. InstallerClean only ever removes the files Windows itself reports as finished with, so nothing it removes is needed to repair, update or uninstall a program. If a needed file does go missing from `C:\Windows\Installer` by some other means, see [If you do have a file missing from C:\Windows\Installer](#recovery).

**Why no `Win32_Product` (WMI)?** [`Win32_Product` triggers MSI repair operations on every product during enumeration](https://gregramsey.net/2012/02/20/win32_product-is-evil/), which can take minutes and load the disk hard. InstallerClean calls the Windows Installer COM API directly with no side effects.

**Why not just a PowerShell script?** A short script that calls `MsiEnumPatchesEx` is enough to *list* patches, but the load-bearing parts of InstallerClean are the bits a script glosses over: the orphan-vs-superseded classification, the registry fallback that only ever adds files to the "still needed" set (never to "removable"), the pending-reboot block, the Move-to-elsewhere safety net, the per-file progress with cancellation and the identity check that asks Windows about each file's own product or patch code before offering it. Edge cases on real heavy-MSI machines (corrupt registrations, junctions inside the cache, products in `HKU\.DEFAULT`, suspended Installer transactions) are easy to mishandle in a one-off script. The `installerclean-cli` is the headless face if scripting is what you want.

**Does it work on Windows 7 or 8?** Untested and not supported. Targets Windows 10 and 11.

**Is it suitable for RMM / mass deployment?** Yes. The CLI exits with distinct codes per outcome (0 success, 2 partial, 1 hard failure, 75 transient, 130 for a Ctrl+C before any file was processed; a Ctrl+C that lands mid-batch exits 2, since work was committed) so a scheduled task can retry on 75 without conflating it with hard failures. It writes a per-run summary to the Application event log and respects the same single-instance mutex as the GUI. The setup also installs silently with the standard Inno Setup switches (`/SILENT` or `/VERYSILENT`); the post-install launch is skipped on silent installs. See the Command line section.

## Download

Three builds, choose one:

- **Setup** (`InstallerClean-2.3.0-setup.exe`): a regular Windows installer with the .NET 10 runtime bundled. Adds a Start Menu entry and uninstalls cleanly. Tucked into Programs so it's easy to find six months from now.
- **Portable** (`InstallerClean-2.3.0-portable.exe`): a single self-contained exe with the runtime bundled. No install, no uninstaller. Run it, use it, delete it. Run it again whenever.
- **CLI** (`installerclean-cli.exe`): the command-line version on its own, a single self-contained exe. No install, nothing left on the machine afterwards. Drop it on a client, run a scan or a clean, delete it. Built for scripting, scheduled tasks and mass deployment, where you want the operations without a desktop app on the client. See [Command line](#command-line) for the arguments and exit codes.

From 2.2.0 the setup and portable filenames carry their version number, so a downloaded copy always says what it is; the CLI keeps its plain `installerclean-cli.exe` name so scheduled tasks and scripts that point at it keep working across updates.

Download from the [releases page](../../releases/latest), then run. It's unsigned, so Windows shows an "unknown publisher" warning; the [FAQ](#unknown-publisher) explains what you'll see and why it's safe.

The app scans automatically on startup. Review the results, then click **Delete** or **Move**.

Or install via [winget](https://learn.microsoft.com/windows/package-manager/winget/):

```
winget install NoFaff.InstallerClean
```

Or install via [Scoop](https://scoop.sh):

```
scoop install installerclean
```

## Compared to PatchCleaner

If you've searched for this folder before, the tool you'll most likely have found is [PatchCleaner](https://www.homedev.com.au/free/patchcleaner). It's still going strong, but I made InstallerClean because PatchCleaner is closed source, hasn't had an update since March 2016 and, by default, won't touch Adobe products. Its orphan check wrongly flagged Adobe's patches, and removing them broke Adobe's updates, so it leaves all Adobe files alone unless you switch the filter off. On the machines where Adobe is the worst offender, that's most of the space:

> *"I've downloaded Patchcleaner to delete the orphaned .msp files, but apparently this would only free up 250 MB of space. 29 GB of the files are 'excluded by filters', so Patchcleaner doesn't seem to help."*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/)

InstallerClean reads the Windows Installer's own patch records, so rather than hiding every Adobe file behind a blanket filter it can tell which patches Windows has marked superseded, and labels them as exactly that. Here's how the two compare:

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| Last updated | 2026 (active) | 3 March 2016 |
| Source code | Open source (Apache 2.0) | Closed source |
| Runtime | .NET 10 (self-contained) | .NET + VBScript |
| API | Windows Installer COM (in-process) | Windows Installer COM (out-of-process via VBScript) |
| Superseded patch detection | Yes | No |
| Adobe handling | Detects superseded patches | Excludes by default |
| UI | Dark theme (WPF) | Windows Forms |
| Data collection | None | None |
| Delete safety | Permanent, with Move to a folder you choose offered alongside | Permanent, no Recycle Bin |

> **A note on `Win32_Product`:** The common-but-broken approach for listing installed products is `Win32_Product` (WMI), which [triggers MSI repair operations](https://gregramsey.net/2012/02/20/win32_product-is-evil/) on every product during enumeration. Both InstallerClean and PatchCleaner avoid it. Both use the Windows Installer COM interface. The `WMIProducts.vbs` filename in PatchCleaner's script is misleading; the script uses MSI COM, not WMI.

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) also offers Installer cleanup as part of its System Booster module, but it's a paid tool ($15-25) and the cleanup is one small feature inside a much larger application. InstallerClean is free, focused and open source.

General-purpose system cleaners like [CCleaner](https://www.ccleaner.com/) and [BleachBit](https://www.bleachbit.org/) do not touch `C:\Windows\Installer`. The folder needs Windows Installer API queries to tell registered packages from unneeded ones, and a generic cleaner that just walked the file tree could break installed apps. InstallerClean is the tool to reach for when that's the folder you actually want cleaned.

## Command line

InstallerClean supports headless operation for scripting and sysadmin use:

```
Usage:
  installerclean-cli --help     Show this help (also accepts /?, -h)
  installerclean-cli --version  Print the version (also accepts -v)
  installerclean-cli /s         Scan only - list unneeded files
  installerclean-cli /d         Delete unneeded files permanently
  installerclean-cli /m         Move to the saved backup folder
  installerclean-cli /m PATH    Move to specified path
```

To launch the GUI, run `InstallerClean.exe` (or use the Start-menu shortcut from the setup install).

Run with no argument, or an unrecognised flag, and `installerclean-cli` prints this usage and exits `1`, so a scheduled task that drops its flag fails visibly instead of silently succeeding while doing nothing. An explicit `--help`, `/?` or `-h` prints the same usage and exits `0`.

`/s` is a dry run: it scans, lists what it would remove with filenames and sizes, then exits. Useful for auditing before cleanup. Exit code is `0` on a successful scan, `1` if the scan fails and `130` on Ctrl+C. All files are in `C:\Windows\Installer`.

`/d` and `/m` scan and then act. `/d` deletes removable files permanently. `/m` moves them to a folder (either one you specify on the command line, or the default saved from the GUI). That saved default is stored per-user, so a scheduled task running as SYSTEM or a service account won't see it; those runs have to pass the folder explicitly with `/m PATH`. Exit codes: `0` for full success, `2` for partial (some files succeeded, some failed), `1` for total failure (scan failed, bad arguments or every file in the batch failed), `75` for a transient condition that blocked the run (the printed message explains which and whether a retry will help), `130` for a Ctrl+C before any file was processed (a Ctrl+C that lands mid-batch exits `2`, partial, since work was committed).

All of the CLI's output, including error and diagnostic messages, goes to stdout; there is no separate stderr stream. The exit code is the machine-readable signal (and the per-run Application event log entry mirrors it), so a script should key off the exit code rather than parse the text, and `installerclean-cli /s > audit.txt` captures the whole run including any error line.

All three require an elevated (administrator) command prompt. If Group Policy blocks the UAC elevation prompt the process refuses to start and Windows returns error 740 to the parent shell (`$LASTEXITCODE = 740` in PowerShell). `taskkill /pid <pid>` does not fire a graceful cancel; the single-instance mutex is recovered by the next run via the AbandonedMutexException path.

### Scheduling a regular clean

To clean on a schedule, point Task Scheduler at `installerclean-cli`. Run it as SYSTEM or a service account with the highest privileges, so it gets the elevation it needs without an interactive prompt, and give the move destination on the command line, because the per-user default set in the GUI doesn't apply to a SYSTEM or service-account run. For a monthly move to `D:\InstallerBackup`, with a copy of the CLI dropped at `C:\Tools`:

```
schtasks /create /tn "InstallerClean monthly" /tr "C:\Tools\installerclean-cli.exe /m D:\InstallerBackup" /sc monthly /ru SYSTEM /rl highest
```

The task blocks until the run finishes and records the exit code as its Last Run Result, so your RMM can key off the codes above (`0` full success, `2` partial, `75` transient, `1` hard failure) the same way a script would.

Two things about the destination. Nothing empties it: a scheduled `/m` only ever adds to that folder, and a file moved twice picks up a `(1)` in its name, so the folder wants its own clear-out on whatever schedule suits you. And a task running as SYSTEM reaches the network as the machine account, not as you, so a `\\server\share` destination only works if that account has been given rights to it.

### Why `installerclean-cli` and not `installerclean.exe`?

`InstallerClean.exe` is the WPF GUI; it does not respond to command-line arguments. `installerclean-cli.exe` is a separate console executable that ships in the same install directory and exposes the same scan / move / delete operations to PowerShell, cmd and scheduled tasks. Because it is a real console process, it blocks the prompt until it finishes; redirect or pipe its output as you would any other console exe.

The portable download contains only the GUI exe. If you want the command line without the GUI, download `installerclean-cli.exe` from the [releases page](../../releases/latest) and run it directly. The setup installs it alongside the GUI as well.

## Requirements

- Windows 10 (version 1607 / build 14393 or later, the oldest the .NET 10 runtime supports) or Windows 11
- Administrator privileges (`C:\Windows\Installer` is admin-only)

See [Download](#download) for setup, portable and CLI build options.

## Building from source

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean.sln
```

Run the tests:

```
dotnet test src/InstallerClean.Tests/
```

## Contributing

Found a bug or have a suggestion? [Open an issue](../../issues) or start a [discussion](../../discussions). Pull requests welcome. Please run `dotnet test` before submitting.

InstallerClean comes in 16 languages, each covering the whole of it: the app, the installer, the command line and this README. In the app, the installer and the command line, the Japanese and the Dutch were contributed complete by coolvitto and RijckAlex, and the Italian is my own machine translation corrected and approved by bovirus, all three native speakers; the rest are my own machine translations. Every README is my own, in every language. I put a lot of effort into them, but they won't be perfect, and I decided to ship them as they are rather than hold them back until a native speaker could check each one. If you speak English and one of these languages and spot anything that could be improved, I'd be glad to hear it, in an [issue](../../issues/new?template=translation_review.md), a pull request or a [discussion](../../discussions).

## Support the project

If InstallerClean helped, consider [supporting No Faff](https://nofaff.netlify.app/support) or leaving a star on GitHub.

## Star History

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/star-history-dark.svg" />
  <source media="(prefers-color-scheme: light)" srcset="docs/star-history-light.svg" />
  <img alt="Line chart of InstallerClean's GitHub stars over time" src="docs/star-history-light.svg" width="800" />
</picture>

## Licence

[Apache 2.0](LICENSE)

---

🎶 [George Formby - When I'm Cleaning Windows](https://www.youtube.com/watch?v=P183Uo5Ust4). Enjoy!
