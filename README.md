<p align="center">
  <strong>English</strong> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a>
</p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<p align="center"><em>🎶 What's my line? I'm happy <a href="https://www.youtube.com/watch?v=HM-jHhUZfFI">cleaning Windows</a></em></p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong>A modern, open-source replacement for <a href="https://www.homedev.com.au/free/patchcleaner">PatchCleaner</a>. Safely clean up <code>C:\Windows\Installer</code>, the hidden Windows folder that quietly eats your disk space.</strong></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-blue.svg" alt="Licence: MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/github/v/release/no-faff/InstallerClean" alt="GitHub Release"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/github/downloads/no-faff/InstallerClean/total?cacheSeconds=300" alt="Total downloads"></a>
</p>

![Screenshot of InstallerClean after a successful clean-up: 965 MB freed, 68 files deleted](docs/screenshots/04d-deleted-freed-success.webp)

- **What:** Finds and removes unneeded files from `C:\Windows\Installer`, the hidden folder Windows never cleans up.
- **How much space:** Depends on your software. On my machine it was just shy of 1 GB. An InstallerClean user [reported](https://github.com/no-faff/InstallerClean/issues/12#issuecomment-4395580816) 25 GB. With Adobe Acrobat it can pass 100 GB. It could be nothing at all. The point is that it's quick and costs nothing; whatever can be removed will be gone.
- **Is it safe:** Yes. Only removes files Windows itself says it no longer needs. Delete sends files to the Recycle Bin (or, if the bin isn't available for the drive, lets you choose Move, permanent delete or cancel, it never deletes permanently without asking). Move lets you keep them somewhere safe.
- **Get it:** [Download the latest release](../../releases/latest), run it, done.

---

## The folder nobody tells you about

There's a hidden folder on every Windows PC called `C:\Windows\Installer`. Every time you install software that uses the Windows Installer system, or apply a patch to Microsoft Office, Adobe Acrobat, Visual Studio or any other `.msi`-based application, a copy of that installer or `.msp` patch file goes into this folder. And stays there.

When you uninstall the software, the files stay. When a newer patch replaces an older one, both stay. Windows never cleans them up. Disk Cleanup doesn't touch them. DISM is for a different folder entirely. Over the years, the folder grows: 10 GB, 30 GB, 50 GB. On machines with heavy MSI-using software (Acrobat is a frequent culprit), it can [pass 100 GB](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/).

These aren't temp files that get recreated the moment you close a cleaning tool. They're genuine dead weight: old installers from software you uninstalled years ago and patches that have been replaced three times over. Once they're gone, they don't come back.

**If you're looking for an easy way to free up disk space on Windows, this folder is one of the best places to start.** InstallerClean finds the unneeded files and removes them safely.

[PatchCleaner](https://www.homedev.com.au/free/patchcleaner) has been the go-to tool for this, but it hasn't been updated since March 2016 and it's closed source. InstallerClean is an open-source alternative, with superseded-patch detection (which catches the Acrobat patches PatchCleaner excludes) and a modern UI.

## The search for help

If you've ever searched for help with this folder, you know how it goes. Someone asks how to clean it. They're told to run Disk Cleanup. They try it. It frees up [600 MB of a 180 GB folder](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb). The thread goes quiet.

> *"All of the threads I've found tend to recommend the same things which don't solve the problem, and then go dead."*
>
> ksparks519, r/Windows10

Or they're told not to touch it at all. In one thread, someone with a 60 GB Installer folder was told to ["don't mess with it."](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/) When they asked what they should do instead, the reply was: *"I just told you."*

The standard advice confuses deleting files at random (which genuinely is dangerous) with removing files that Windows itself says it no longer needs (which isn't). InstallerClean does the latter.

If you've searched for help with this before, you've probably already found [PatchCleaner](https://www.homedev.com.au/free/patchcleaner) by [John Crawford](https://www.homedev.com.au/). It's a fantastic app. I downloaded it and it did exactly what it said: freed up a ton of space. The one thing it doesn't handle is Adobe patches; it excludes them by default, and on machines where Adobe is the biggest offender, a lot of removable files get left behind:

> *"I've downloaded Patchcleaner to delete the orphaned .msp files... 29 GB of the files are 'excluded by filters', so Patchcleaner doesn't seem to help."*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/)

InstallerClean detects which patches have been superseded by newer updates and flags them as removable, including the Acrobat patches PatchCleaner excludes.

## What it does

1. **Scans** `C:\Windows\Installer` for `.msi` and `.msp` files
2. **Queries** the Windows Installer API to find which files are still registered
3. **Shows** what's needed and what's not, with sizes
4. **Removes** the unneeded files: delete to the Recycle Bin (if it isn't available for the drive, the app asks before any permanent delete), or move to a folder you choose

No automatic network activity. Two opt-in buttons make a single HTTPS call when clicked: **Check for updates** in About, and **Send summary** on the completion screen. See [What it doesn't do](#what-it-doesnt-do) below for the full detail.

## Screenshots

<p>
  <img src="docs/screenshots/01-initial-scan.webp" alt="Splash screen showing the scan in progress, having found 68 files to clean up" width="900"><br>
  <em>Initial scan. This is very quick.</em>
</p>

<p>
  <img src="docs/screenshots/02-main-window.webp" alt="Main window showing 116 files still used and 68 files to clean up" width="900"><br>
  <em>Results: how much is in use, how much is removable.</em>
</p>

<p>
  <img src="docs/screenshots/03a-details-registered.webp" alt="Registered files window listing installed products and their installer-database metadata" width="900"><br>
  <em>The files still in use, with metadata read from the installer database.</em>
</p>

<p>
  <img src="docs/screenshots/03b-details-unused.webp" alt="Unused files window listing removable .msi files with reasons" width="900"><br>
  <em>The files no longer needed.</em>
</p>

<p>
  <img src="docs/screenshots/04b-Delete-dialogue.webp" alt="Delete confirmation dialog showing 68 files (965 MB) will go to the Recycle Bin" width="900"><br>
  <em>Confirmation before either action. Delete sends to the Recycle Bin; Move puts the files somewhere of your choice.</em>
</p>

<p>
  <img src="docs/screenshots/04d-deleted-freed-success.webp" alt="Success overlay showing 965 MB freed after a delete operation, with 68 files sent to the Recycle Bin" width="900"><br>
  <em>After a successful Delete.</em>
</p>

<p>
  <img src="docs/screenshots/06a-scanned-again-all-clean.webp" alt="All clean overlay shown when nothing is removable on a subsequent scan" width="900"><br>
  <em>After scanning again. Nothing left to clean.</em>
</p>

## How it works

InstallerClean identifies two kinds of unneeded files.

**Orphaned files** are installers and patches left behind after you uninstall software. Windows no longer references them, but the files sit in the folder taking up space.

**Superseded patches** are old `.msp` patches that have been replaced by newer ones. Windows marks them as superseded in its own database but never deletes them. Vendors that ship frequent patches (Acrobat, Office, large dev tools) accumulate superseded ones indefinitely.

To find them, InstallerClean calls the Windows Installer COM interface directly via P/Invoke:

- `MsiEnumProductsEx` to enumerate every installed product
- `MsiEnumPatchesEx` to find all registered patches for each product
- `MsiGetPatchInfoEx` to read patch state (applied, superseded or obsoleted)

Any `.msi` or `.msp` file in `C:\Windows\Installer` that isn't claimed by a registered product is orphaned. Any patch marked as superseded and not required for uninstall is flagged as removable.

If the API returns incomplete data (rare, but it can happen with corrupted installer state), the app falls back to reading the registry. The fallback only adds files to the "still needed" set, never to the "removable" set.

After a Move or Delete completes, empty subfolders inside `C:\Windows\Installer` (the directories the cache leaves behind once their contents are gone) are pruned in the same pass. Reparse points are skipped during the prune so a junction planted inside the cache cannot redirect the cleanup outside it.

## Is it safe?

Yes. InstallerClean queries the same database Windows itself uses to track what's installed. If Windows says a file is no longer needed, the app trusts it; it doesn't guess based on filenames or dates.

**In the app.** Delete sends files to the Recycle Bin. If the Recycle Bin isn't available for that drive (it's been turned off for the drive, or it's full or damaged), InstallerClean doesn't quietly delete the files for good. It stops and lets you choose: move them somewhere safe instead, delete them permanently or cancel. Files are only ever permanently deleted if you explicitly choose that. Move is the extra-safe option: it puts the files in a folder you choose, so you can keep them until you're sure nothing's gone wrong. Nothing is touched until you confirm. If Windows Installer is currently writing to the cache, has a previous transaction suspended or has a queued post-reboot rename targeting the cache, Move and Delete are disabled and the specific reason is shown. The scan, query, move, delete, settings and pending-reboot services are covered by an automated test suite that runs on every commit (see the CI badge above).

**Verifying the binary.** InstallerClean is unsigned. Code-signing certificates cost money annually and I'd rather keep the project free, open and donations-funded.

- SHA-256 hashes for each release are listed on the [releases page](../../releases/latest).
- VirusTotal links for setup, portable, slim and CLI builds are published with each release.
- Source is at [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean) and CI builds and tests every commit (see the green CI badge above).
- [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) tests each release for viruses, spyware and adware.
- [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) tests each submission in a virtual machine and lists it only if it passes their review.

<a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Softpedia certified 100% clean" width="190"></a>

VirusTotal: clean across every engine. Live links in each release's notes so you can re-check.

## If a needed file goes missing

InstallerClean only ever clears files Windows reports as finished with, so it
can't leave a program unable to repair, update or uninstall. Removing files from
`C:\Windows\Installer` by hand, or with a tool that doesn't check the Installer
database first, is a different matter, and it's why the standard advice is to
leave the folder alone. That advice is right, as far as it goes. Here's the
fuller picture, and what to do if a needed file has already gone.

<details>
<summary><strong>About <code>C:\Windows\Installer</code>, and recovering a missing file</strong></summary>

<br>

`C:\Windows\Installer` is the Windows Installer cache. When you install an
MSI-based program or apply a patch, Windows keeps a copy of the installer here
and records, per product, the file it expects to find later. These files aren't
used while the program runs; they're used when Windows repairs, updates or
uninstalls it. Delete one a program still needs and nothing breaks straight
away, which is exactly why it's easy to get away with deleting them and only hit
trouble months later. Microsoft puts it like this:

> "If the installer cache is compromised, you may not immediately see problems
> until you take an action such as uninstalling, repairing, or updating a
> product."

Recovery is not straightforward, and Microsoft is candid about that:

> "If application files are missing from the Windows Installer Cache, ask the
> vendor or support team for the application about the missing files. You must
> follow the procedures or steps recommended by the application vendor to
> restore the files. In some cases, you may have to rebuild the operating system
> and reinstall the application to fix the problem."

> "Windows support engineers cannot help you recover missing application files
> from the Windows Installer cache."

You can't borrow the file from another machine, either:

> "Missing files cannot be copied between computers because the files are
> unique."

In practice, the fix that usually works is to download the affected program's
installer from its maker and run it over your existing install. Don't uninstall
first: uninstalling is itself one of the steps that needs the missing file. Use
the version you currently have installed if you can still get hold of it,
because Windows may reject a different one:

> "The upgrade cannot be installed by the Windows Installer service because the
> program to be upgraded may be missing, or the upgrade may update a different
> version of the program."

This normally restores the file and leaves your settings untouched, but
Microsoft doesn't guarantee it, and its documented last resort is reinstalling
the program, or rebuilding Windows. That's the official position, reported as I
find it. I didn't cause it and I can't improve on Microsoft's own guidance; I'm
simply telling you what it is.

None of this can happen because of InstallerClean. It only ever removes files
Windows itself reports as no longer needed, so the file a future repair, update
or uninstall goes looking for is never one it touched. Microsoft's guidance is at
[Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache).

</details>

## What it doesn't do

- WinSxS (`C:\Windows\WinSxS`) is a different folder with different rules. For that one, run `Dism /Online /Cleanup-Image /StartComponentCleanup` from an elevated prompt.
- No background service, no scheduled task, no auto-clean. The app runs when you launch it.
- The registry is read-only. The app queries the Windows Installer database; it doesn't change it.
- No automatic telemetry, no background network. The app makes no network call until you click one of two buttons. **Check for updates** in About queries GitHub's public releases API on click and tells you whether you have the latest version (single HTTPS GET, identifying string `InstallerClean/<version>`). **Send summary** on the completion screen reads `%LOCALAPPDATA%\NoFaff\InstallerClean\last-run.json` and HTTPS-POSTs it to a No Faff endpoint so I can see whether the run worked. The JSON contains counts and categorical labels only: no file paths, no user names, no machine identifiers, no time-of-day. Clicking opens a confirmation window showing the exact JSON about to be sent; review it there and press Send to confirm, or Cancel to back out. Once per machine: after a successful send the button stays hidden for ever; if the first attempt fails with a transient error the next session re-prompts.
- No bundled extras. No toolbars, no third-party offers, no upsells.
- The only permission asked for beyond launching is Administrator, which is required because `C:\Windows\Installer` is admin-only.

## FAQ

**Will I actually free up GBs of space?** Depends on your machine. A clean Windows 11 install with no extra software has nothing to remove. A long-running developer workstation, or any machine with heavy MSI-based software (Acrobat, Office, LibreOffice, large dev tools), can have tens of GB. Run `installerclean-cli /s` to see exactly what would be removed before you commit.

<!-- reports-stats-start (generated by non-repo-files/refresh-reports-table.mjs; do not hand-edit between these markers) -->
Across the 64 reports people have been kind enough to send in (thanks 🙏) since v1.8.0 added the option:

| Outcome | Share | Smallest | Median | Largest |
|---|---|---|---|---|
| Nothing to remove | 67% | - | - | - |
| Freed space | 33% | 0.2 GB | 20 GB | 327 GB |
<!-- reports-stats-end -->

**Why does it want Administrator?** `C:\Windows\Installer` is owned by SYSTEM and locked down to admins only. Reading the folder, writing to the Installer-database query API and moving or deleting files all require elevation. There's no user-mode path.

**Can I undo a Delete?** Usually, yes. When the Recycle Bin is available for the drive, Delete sends files there and you can restore them from the bin. If the bin isn't available, the app never deletes for good on its own; it offers Move or a permanent delete you confirm. Either way, for a safety net you control, use Move to put the files in a folder you choose and verify nothing breaks before deleting from there.

**Will Windows complain if I remove these files?** No. InstallerClean only ever removes the files Windows itself reports as finished with, so nothing it removes is needed to repair, update or uninstall a program. If a needed file does go missing from `C:\Windows\Installer` by some other means, see [If a needed file goes missing](#if-a-needed-file-goes-missing).

**Why no `Win32_Product` (WMI)?** [`Win32_Product` triggers MSI repair operations on every product during enumeration](https://gregramsey.net/2012/02/20/win32_product-is-evil/), which can take minutes and load the disk hard. InstallerClean calls the Windows Installer COM API directly with no side effects.

**Why not just a PowerShell script?** A short script that calls `MsiEnumPatchesEx` is enough to *list* patches, but the load-bearing parts of InstallerClean are the bits a script glosses over: the orphan-vs-superseded classification, the registry fallback that only ever adds files to the "still needed" set (never to "removable"), the pending-reboot block, the Move-to-elsewhere safety net, the per-file progress with cancellation and the Recycle-Bin-not-permanent-delete default. Edge cases on real heavy-MSI machines (corrupt registrations, junctions inside the cache, products in `HKU\.DEFAULT`, suspended Installer transactions) are easy to mishandle in a one-off script. The `installerclean-cli` is the headless face if scripting is what you want.

**Does it work on Windows 7 or 8?** Untested and not supported. Targets Windows 10 and 11.

**Is it suitable for RMM / mass deployment?** Yes. The CLI exits with distinct codes per outcome (0 success, 2 partial, 1 hard failure, 75 transient, 130 Ctrl+C) so a scheduled task can retry on 75 without conflating it with hard failures. It writes a per-run summary to the Application event log and respects the same single-instance mutex as the GUI. See the Command line section.

## Download

Four builds, choose one:

- **Setup** (`InstallerClean-setup.exe`): a regular Windows installer with the .NET 10 runtime bundled. Adds a Start Menu entry and uninstalls cleanly. Tucked into Programs so it's easy to find six months from now.
- **Portable** (`InstallerClean-portable.exe`): a single self-contained exe with the runtime bundled. No install, no uninstaller. Run it, use it, delete it. Run it again whenever.
- **Slim** (`InstallerClean-slim.exe`): the smallest download. Requires the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) to be installed already (which you have if you have an up-to-date Visual Studio).
- **CLI** (`installerclean-cli.exe`): the command-line version on its own, a single self-contained exe. No install, nothing left on the machine afterwards. Drop it on a client, run a scan or a clean, delete it. Built for scripting, scheduled tasks and mass deployment, where you want the operations without a desktop app on the client. See [Command line](#command-line) for the arguments and exit codes.

Download from the [releases page](../../releases/latest), then run. Windows SmartScreen will say "Unknown publisher". Click **More info** then **Run anyway**. This is normal for unsigned open-source software.

The app scans automatically on startup. Review the results, then click **Delete** or **Move**.

Or install via [Scoop](https://scoop.sh):

```
scoop bucket add no-faff https://github.com/no-faff/scoop-bucket
scoop install installerclean
```

## Compared to PatchCleaner

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| Last updated | 2026 (active) | 3 March 2016 |
| Source code | Open source (MIT) | Closed source |
| Runtime | .NET 10 (self-contained) | .NET + VBScript |
| API | Windows Installer COM (in-process) | Windows Installer COM (out-of-process via VBScript) |
| Superseded patch detection | Yes | No |
| Adobe handling | Detects superseded patches | Excludes by default |
| UI | Dark theme (WPF) | Windows Forms |
| Data collection | None | None |
| Delete safety | Recycle Bin, never a silent permanent delete | Permanent, no Recycle Bin |

> **A note on `Win32_Product`:** The common-but-broken approach for listing installed products is `Win32_Product` (WMI), which [triggers MSI repair operations](https://gregramsey.net/2012/02/20/win32_product-is-evil/) on every product during enumeration. Both InstallerClean and PatchCleaner avoid it. Both use the Windows Installer COM interface. The `WMIProducts.vbs` filename in PatchCleaner's script is misleading; the script uses MSI COM, not WMI.

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) also offers Installer cleanup as part of its System Booster module, but it's a paid tool ($15-25) and the cleanup is one small feature inside a much larger application. InstallerClean is free, focused and open source.

General-purpose system cleaners like [CCleaner](https://www.ccleaner.com/) and [BleachBit](https://www.bleachbit.org/) do not touch `C:\Windows\Installer`. The folder needs Windows Installer API queries to tell registered packages from unused ones, and a generic cleaner that just walked the file tree could break installed apps. InstallerClean is the tool to reach for when that's the folder you actually want cleaned.

## Command line

InstallerClean supports headless operation for scripting and sysadmin use:

```
Usage:
  installerclean-cli --help   Show this help (also accepts /?, -h)
  installerclean-cli --version  Print the version (also accepts -v)
  installerclean-cli /s       Scan only - list removable files
  installerclean-cli /d       Delete removable files (Recycle Bin)
  installerclean-cli /m       Move to saved default location
  installerclean-cli /m PATH  Move to specified path
```

To launch the GUI, run `InstallerClean.exe` (or use the Start-menu shortcut from the setup install).

Run with no argument, or an unrecognised flag, and `installerclean-cli` prints this usage and exits `1`, so a scheduled task that drops its flag fails visibly instead of silently succeeding while doing nothing. An explicit `--help`, `/?` or `-h` prints the same usage and exits `0`.

`/s` is a dry run: it scans, lists what it would remove with filenames and sizes, then exits. Useful for auditing before cleanup. Exit code is `0` on a successful scan, `1` if the scan fails and `130` on Ctrl+C. All files are in `C:\Windows\Installer`.

`/d` and `/m` scan and then act. `/d` sends removable files to the Recycle Bin. `/m` moves them to a folder (either one you specify on the command line, or the default saved from the GUI). Exit codes: `0` for full success, `2` for partial (some files succeeded, some failed), `1` for total failure (scan failed, bad arguments or every file in the batch failed), `75` for a transient condition that blocked the run (the printed message explains which and whether a retry will help), `130` for Ctrl+C.

All of the CLI's output, including error and diagnostic messages, goes to stdout; there is no separate stderr stream. The exit code is the machine-readable signal (and the per-run Application event log entry mirrors it), so a script should key off the exit code rather than parse the text, and `installerclean-cli /s > audit.txt` captures the whole run including any error line.

All three require an elevated (administrator) command prompt. If Group Policy blocks the UAC elevation prompt the process refuses to start and Windows returns error 740 to the parent shell (`$LASTEXITCODE = 740` in PowerShell). `taskkill /pid <pid>` does not fire a graceful cancel; the single-instance mutex is recovered by the next run via the AbandonedMutexException path.

### Why `installerclean-cli` and not `installerclean.exe`?

`InstallerClean.exe` is the WPF GUI; it does not respond to command-line arguments. `installerclean-cli.exe` is a separate console executable that ships in the same install directory and exposes the same scan / move / delete operations to PowerShell, cmd and scheduled tasks. Because it is a real console process, it blocks the prompt until it finishes; redirect or pipe its output as you would any other console exe.

The portable and slim downloads contain only the GUI exe. If you want the command line without the GUI, download `installerclean-cli.exe` from the [releases page](../../releases/latest) and run it directly. The setup installs it alongside the GUI as well.

## Requirements

- Windows 10 or 11
- Administrator privileges (`C:\Windows\Installer` is admin-only)

See [Download](#download) for setup, portable, slim and CLI build options.

## Building from source

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean/InstallerClean.csproj
```

Run the tests:

```
dotnet test src/InstallerClean.Tests/
```

## Contributing

Found a bug or have a suggestion? [Open an issue](../../issues) or start a [discussion](../../discussions). Pull requests welcome. Please run `dotnet test` before submitting.

## Support the project

If InstallerClean helped, consider [supporting No Faff](https://nofaff.netlify.app/support) or leaving a star on GitHub.

## Star history

<a href="https://www.star-history.com/?repos=no-faff%2FInstallerClean&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
   <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
 </picture>
</a>

## Licence

[MIT](LICENSE)

---

🎶 [George Formby - When I'm Cleaning Windows](https://www.youtube.com/watch?v=sfmAeijj5cM). Enjoy!
