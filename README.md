[![Licence: MIT](https://img.shields.io/badge/licence-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Windows 10/11](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg)](https://github.com/no-faff/windows-installer-cleaner/releases)
<!-- [![VirusTotal](https://img.shields.io/badge/VirusTotal-0%2F70-brightgreen.svg)](VIRUSTOTAL_URL) -->
<!-- [![GitHub Release](https://img.shields.io/github/v/release/no-faff/windows-installer-cleaner)](https://github.com/no-faff/windows-installer-cleaner/releases/latest) -->

# InstallerClean

Find and remove unneeded `.msi` and `.msp` files from `C:\Windows\Installer`.

![Screenshot of InstallerClean](docs/screenshot.png)

## What it does

Windows caches every installer and patch it uses in `C:\Windows\Installer`. When you uninstall software or apply newer updates, the old files stay behind. Over time the folder grows, sometimes to tens or hundreds of gigabytes.

InstallerClean scans the folder, asks the Windows Installer API which files are still needed, and lets you remove the ones that aren't. That's it.

1. **Scan** `C:\Windows\Installer` for `.msi` and `.msp` files
2. **Query** the Windows Installer API to find which files are still registered
3. **Show** what's needed and what's not, with sizes
4. **Remove** the unneeded files (delete or move to a location you choose)

## Why it exists

If you've ever found `C:\Windows\Installer` eating your disk space, you've probably been told to "just run Disk Cleanup" (doesn't touch this folder), "use DISM" (wrong tool, that's for the component store), or simply "don't touch it". None of that helps when your drive is full.

[PatchCleaner](https://www.homedev.com.au/free/patchcleaner) solved this problem for years, but it hasn't been updated since 3 March 2016. It relies on VBScript and WMI, it's closed source, and it occasionally gets flagged by antivirus software.

InstallerClean is a modern, open-source replacement. Same job, built for today.

<!-- TODO: Real-world examples section
     Add sourced quotes from Reddit threads and Microsoft forum posts,
     with links to the original threads and screenshots where available.
     The user has bookmarked URLs to provide. -->

## How it identifies files

**Orphaned files:** When you uninstall software, the `.msi` installer (and any `.msp` patches) often stays behind in `C:\Windows\Installer`. The Windows Installer API no longer references it, but the file remains. These are safe to remove.

**Superseded patches:** When a newer patch replaces an older one, Windows marks the old patch as superseded in its own database but never deletes it. InstallerClean reads this status (the `State` property from `MsiGetPatchInfoEx`) and flags these as removable too. This is something PatchCleaner doesn't do. It's particularly useful for Adobe Acrobat, which delivers large patches and accumulates superseded ones over time. PatchCleaner excludes Adobe by default, so those files never get cleaned.

## Compared to other tools

PatchCleaner served the community well for a decade. InstallerClean picks up where it left off.

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| Last updated | 2026 (active) | 3 March 2016 |
| Source code | Open source (MIT) | Closed source |
| Runtime | .NET 8 | .NET + VBScript |
| API | Windows Installer COM (direct) | WMI (`Win32_Product`) |
| Superseded patch detection | Yes | No |
| Adobe handling | Detects superseded patches | Excludes by default |
| UI | Modern dark theme (WPF) | Windows Forms |
| Data collection | None | None |

> **A note on WMI:** PatchCleaner uses `Win32_Product`, which is known to [trigger MSI repair operations](https://gregramsey.net/2012/02/20/win32_product-is-evil/) during enumeration. InstallerClean calls the Windows Installer COM interface directly with no side effects.

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) also has an Installer cleanup feature buried inside its System Booster module, but it's a paid tool ($15-25) and the cleanup is one small part of a much larger application. InstallerClean is free, focused and open source.

## Getting started

1. Download the latest release from the [releases page](../../releases)
2. Run the exe. Windows will prompt for administrator access.
3. The app scans automatically on startup
4. Review the results, then click **Delete** or **Move**

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

We query the Windows Installer API, the same database Windows itself uses to track what's installed. If Windows says a file is no longer needed, we trust it. We don't guess based on filenames or dates.

- **Delete** sends files to the Recycle Bin, so you can restore them if needed.
- **Move** copies files to a location you choose first, if you prefer to be cautious.
- Nothing is touched until you explicitly click Delete or Move and confirm.
- The app warns you if Windows has pending updates that could affect accuracy.
- Every line of code is on GitHub. Read it, audit it, build it yourself.

<!-- - [VirusTotal scan](VIRUSTOTAL_URL): 0/70 detections. -->

## Under the hood

InstallerClean calls the Windows Installer COM interface directly via P/Invoke:

- `MsiEnumProductsEx` to enumerate every installed product across all user contexts
- `MsiEnumPatchesEx` to find all registered patches for each product
- `MsiGetPatchInfoEx` to read patch state (applied, superseded or obsoleted)

Any `.msi` or `.msp` file in `C:\Windows\Installer` that isn't claimed by a registered product is orphaned. Any patch marked as superseded and not required for uninstall is flagged as removable.

If the API returns incomplete data (which can happen with corrupted installer state), we fall back to reading the registry (`HKLM\Software\Microsoft\Windows\CurrentVersion\Installer`). The fallback is conservative: it only adds files to the "still needed" set, never to the "removable" set.

We never call `Win32_Product`. That WMI class triggers MSI consistency checks on every installed product during enumeration.

## Features

- **Delete or move.** Delete sends to the Recycle Bin. Move lets you keep files somewhere safe.
- **Superseded patch detection.** Finds patches that Windows itself has marked as replaced.
- **Detail views.** Inspect individual files with product name, size, reason and digital signature.
- **Pending reboot detection.** Warns if pending updates might affect scan results.
- **Command line mode.** `/d`, `/m` and `/m PATH` for scripting and automation.
- **No installer needed.** Download, run, done.
- **No data collection.** Doesn't phone home, collect data or require an account.

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

If InstallerClean helped, consider [buying me a cuppa](https://ko-fi.com/nofaff) or leaving a star on GitHub.

## Licence

[MIT](LICENSE)
