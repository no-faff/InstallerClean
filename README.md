[![Licence: MIT](https://img.shields.io/badge/licence-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Windows 10/11](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg)](https://github.com/no-faff/windows-installer-cleaner/releases)
<!-- [![VirusTotal](https://img.shields.io/badge/VirusTotal-0%2F70-brightgreen.svg)](VIRUSTOTAL_URL) -->
<!-- [![GitHub Release](https://img.shields.io/github/v/release/no-faff/windows-installer-cleaner)](https://github.com/no-faff/windows-installer-cleaner/releases/latest) -->

# InstallerClean

Reclaim tens, sometimes hundreds, of gigabytes from `C:\Windows\Installer`.

![Screenshot of InstallerClean](docs/screenshot.png)

## The problem

Windows keeps every `.msi` installer and `.msp` patch it has ever used in `C:\Windows\Installer`. When you update software, the old patches are superseded by newer ones, but Windows never removes them. Over months and years the folder quietly balloons.

The biggest culprits are **Adobe Acrobat** and **Microsoft Office**. Both deliver updates as large `.msp` patch files (often 1 GB+), and Windows retains every single version. A machine with a few years of Office and Acrobat updates can easily have 50+ GB of superseded patches sitting in this one folder.

Real numbers from real people:

- **273 GB** on a single machine (1,133 `.msi` and `.msp` files)
- **180 GB** on a 240 GB drive ("Microsoft don't delete anything")
- **176 GB** dominated by ~1.1 GB Adobe `.msp` patches
- **50 GB** "consistent problem for me for years"

Searching "C:\Windows\Installer safe to delete" leads to the same dead ends: Disk Cleanup ignores the folder. Storage Sense doesn't touch it. DISM is for the component store, not the Installer cache. And the most common suggestion, "don't touch it", isn't much use when your drive is full.

## What it does

1. **Scans** `C:\Windows\Installer` for `.msi` and `.msp` files
2. **Queries the Windows Installer API** to find every file still registered to an installed product
3. **Detects superseded patches**, old `.msp` files replaced by newer versions that Windows kept anyway
4. **Shows a clear summary** of files still needed vs files safe to remove, with sizes
5. **Moves or deletes** to a safe location you choose (recommended) or to the Recycle Bin
6. **Cleans up** empty subdirectories left behind by failed Windows Installer operations

## Superseded patch detection

This is what sets InstallerClean apart.

Most tools only look for orphaned files, installers left behind after software is uninstalled. That misses the bigger problem: **superseded patches**. These are old `.msp` patches that have been replaced by newer cumulative updates. Windows marks them as superseded in its own database (the `State` property in `MsiGetPatchInfoEx`) but never acts on it.

Adobe Acrobat and Microsoft Office are the worst offenders. Each update delivers a large `.msp` patch, and over time dozens of superseded patches accumulate. PatchCleaner excludes Adobe by default, so the single biggest source of waste is the one thing it won't touch.

InstallerClean reads the same Windows Installer API that tracks patch state. If Windows says a patch is superseded and not required for uninstall, we flag it as removable. We don't guess based on filenames or dates. We read the authority's own records.

## Compared to other tools

PatchCleaner served the community well for a decade. InstallerClean picks up where it left off.

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| Last updated | 2026 (active) | 3 March 2016 |
| Source code | Open source (MIT) | Closed source |
| Runtime | .NET 8 | .NET + VBScript |
| API | Windows Installer COM (direct) | WMI (`Win32_Product`) |
| Superseded patch detection | Yes, reads patch State property | No |
| Adobe handling | Detects superseded patches properly | Excludes by default |
| UI | Modern dark theme (WPF) | Windows Forms |
| Data collection | None | None |

> **A note on WMI:** PatchCleaner uses `Win32_Product`, which is known to [trigger MSI repair operations](https://gregramsey.net/2012/02/20/win32_product-is-evil/) during enumeration. InstallerClean calls the Windows Installer COM interface directly with no side effects.

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) also includes an Installer cleanup feature inside its System Booster module, but it's a paid Swiss army knife tool ($15-25) where the cleanup is buried alongside registry cleaning and junk file removal. InstallerClean is free, focused and open source.

## Getting started

1. Download the latest release from the [releases page](../../releases)
2. Run the exe. Windows will prompt for administrator access.
3. The app scans automatically on startup
4. Review the results, then **Move** (recommended) or **Delete**

> **Tip:** If Windows has pending updates, InstallerClean will warn you. Restart and install updates first. A pending update might reference files that appear removable but aren't yet fully registered.

## Command line

InstallerClean supports headless operation for scripting and sysadmin use:

```
InstallerClean — clean up C:\Windows\Installer

Usage:
  InstallerClean.exe          Launch the GUI
  InstallerClean.exe /d       Delete removable files (Recycle Bin)
  InstallerClean.exe /m       Move to saved default location
  InstallerClean.exe /m PATH  Move to specified path
```

Also accepts `--help`, `/?` and `-h`.

## Is it safe?

- **Move first, delete later.** The recommended workflow. Move files to a location you choose, live with it for a week. If nothing breaks, delete them. If something does, copy them back.
- **Delete goes to the Recycle Bin.** Not permanent deletion. You can restore files if needed.
- **No modifications until you confirm.** The app only reads during scanning. Nothing is touched until you explicitly click Move or Delete and confirm.
- **Pending reboot detection.** Warns you if Windows has pending updates that could affect accuracy.
- **Open source.** Every line of code is on GitHub. Read it, audit it, build it yourself.
<!-- - **VirusTotal.** [Clean scan](VIRUSTOTAL_URL), 0/70 detections. -->

## How it works under the hood

**Orphan detection:** `MsiEnumProductsEx` enumerates every installed product across all user contexts. For each product, we collect the local `.msi` package path and enumerate all associated `.msp` patches via `MsiEnumPatchesEx`. Any file in `C:\Windows\Installer` not claimed by a registered product is orphaned.

**Superseded patch detection:** For each registered patch, we call `MsiGetPatchInfoEx` to read the `State` property (1 = Applied, 2 = Superseded, 4 = Obsoleted) and the `Uninstallable` property. Patches that are superseded or obsoleted, and not marked as required for uninstall, are flagged as removable.

**Registry fallback:** If the API returns incomplete data (which can happen with corrupted installer state), we fall back to reading `HKLM\Software\Microsoft\Windows\CurrentVersion\Installer` directly. The fallback is conservative: it only adds files to the "registered" set, never to the "removable" set.

**No WMI:** We never call `Win32_Product`. That WMI class is notorious for triggering MSI consistency checks and repair operations on every installed product during enumeration. We talk to the Windows Installer COM interface directly via P/Invoke. Fast, safe, no side effects.

## Features

- **Move or delete.** Move is the safe default; delete sends to the Recycle Bin
- **Superseded patch detection.** Finds `.msp` patches Windows itself has marked as replaced
- **Detail views.** Inspect individual files with product name, size, reason and digital signature
- **Pending reboot detection.** Warns if scan results might be affected by pending updates
- **Empty subdirectory cleanup.** Removes empty temp folders left by failed Installer operations
- **Command line mode.** `/d`, `/m` and `/m PATH` for scripting and automation
- **No installer needed.** Download, run, done
- **No data collection.** Doesn't phone home, collect data or require an account
- **Dark theme.** Modern UI with the Poppins typeface

## Requirements

- Windows 10 or 11
- Administrator privileges (to access `C:\Windows\Installer`)
- Self-contained, no .NET runtime install needed

## Building from source

```
git clone https://github.com/no-faff/windows-installer-cleaner.git
cd windows-installer-cleaner
dotnet build src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj
```

Run the tests:

```
dotnet test src/SimpleWindowsInstallerCleaner.Tests/
```

## Contributing

Found a bug or have a suggestion? [Open an issue](../../issues). Pull requests are welcome. Please run the tests before submitting.

## Part of the No Faff suite

InstallerClean is part of [No Faff](https://github.com/no-faff), a collection of small, useful Windows utilities. No fuss, no bloat, no accounts.

## Support the project

If InstallerClean saved you some drive space, consider [buying me a cuppa](https://ko-fi.com/nofaff) or leaving a star on GitHub.

## Licence

[MIT](LICENSE)
