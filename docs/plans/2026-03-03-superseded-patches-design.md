# Superseded patches + cleanup improvements

Date: 3 March 2026

## Summary

Detect superseded/obsoleted Windows Installer patches and offer to remove them
alongside orphaned files. This solves the Adobe Acrobat problem — the #1 cause
of bloated C:\Windows\Installer folders — which no existing tool handles properly.

Also: remove the Filters UI (no longer needed), clean up empty subdirectories
after operations, and add a registry fallback for more robust detection.

## Background

Windows Installer tracks patch state via MsiGetPatchInfoEx:
- Applied (1) — active, in use. Never touch.
- Superseded (2) — replaced by a newer patch. Removed from the active chain.
- Obsoleted (4) — explicitly marked obsolete by a newer patch.

Superseded/obsoleted patches are not used during repair. Their cached .msp files
sit in C:\Windows\Installer forever. Adobe Acrobat Reader accumulates ~1.1 GB
patches per update, 10+ updates per year. Real-world folder sizes: 50-273 GB.

PatchCleaner and all other tools exclude Adobe entirely. We can now handle it
properly because we can read the actual patch state from the API.

A patch is safe to remove if:
1. State = Superseded (2) or Obsoleted (4)
2. Uninstallable != "1" (no rollback scenario exists)

Adobe patches are never uninstallable by design, so condition 2 is always met.

## Changes

### 1. Easy wins from PatchCleanerPS (do first)

**a) Empty subdirectory cleanup**
After move or delete completes, prune empty subdirectories inside
C:\Windows\Installer. One-way: only deletes directories that are empty.
Add to MoveFilesService and DeleteFilesService.

**b) Registry fallback for patch detection**
Walk HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\<SID>\Patches
and ...\Products to find registered packages directly from the registry. Merge into
the registered set alongside MSI API results. This catches packages the API might
miss (corrupted database, weird edge cases). Extra important for CLI mode where
there's no human reviewing results before action.

### 2. Superseded patch detection

**InstallerQueryService changes:**
- After getting each patch's LocalPackage path, also query "State" property
- If State = "2" (superseded) or "4" (obsoleted), also query "Uninstallable"
- Pass this info through to the RegisteredPackage model

**RegisteredPackage model changes:**
- Add PatchState enum: Applied, Superseded, Obsoleted, NotAPatch
- Add IsRemovable bool: true if (Superseded or Obsoleted) and not Uninstallable
- Keep existing fields (LocalPackagePath, ProductName, ProductCode)

**ScanResult / FileSystemScanService changes:**
- Split registered packages: applied (truly needed) vs removable (superseded)
- Removable registered patches join orphaned files in a combined "safe to remove" list
- Each item tagged with a reason: "Orphaned" or "Superseded"

### 3. Remove Filters UI

- Remove Filters button from bottom nav
- Remove SettingsWindow and SettingsViewModel
- Remove "X files excluded by filters" line from main window
- Remove default "Acrobat" filter from AppSettings
- Keep ExclusionService code in codebase (tested, works) but don't wire it up in GUI
- CLI: remove filter pipeline from RunCliAsync

### 4. Main window UI changes

- "X files orphaned" becomes "X files to clean up" (or similar)
- Filters button gone from bottom nav
- Excluded files line gone
- Everything else stays the same: same Move/Delete buttons, same flow

### 5. Details window changes

- Add "Reason" column: "Orphaned" or "Superseded"
- Be thoughtful with space — resize panels as needed
- Superseded entries should show the product name they belonged to

### 6. CLI changes

- Remove filter/exclusion pipeline
- Update output text: "orphaned" → "to clean up" where appropriate
- Help text updated to reflect superseded patches
- Consider: verbose flag (/v) that lists each file with its reason (nice-to-have)

### 7. Update explanatory text

- Main window help text: update to mention superseded patches
- About window: update description if needed
- SettingsWindow explanation text about Acrobat: removed with the window

## Testing

- Existing 24 tests must still pass
- New tests for:
  - RegisteredPackage with different PatchState values
  - InstallerQueryService returning superseded patches (mock)
  - FileSystemScanService combining orphaned + superseded into one list
  - Registry fallback finding additional registered packages
- Manual testing: run on a machine with Adobe Acrobat installed, verify
  superseded patches are correctly identified

## Risk

Low. We are:
- Adding state queries to patches we already enumerate (same API, new property)
- Only flagging patches as removable when State=Superseded/Obsoleted AND not Uninstallable
- Move-first is still the default
- Registry fallback can only make us MORE conservative (finding more registered files)

The only new risk is a bug in the state query logic incorrectly flagging an applied
patch as removable. Mitigated by: unit tests, the Uninstallable check, and move-first.

## Not in scope

- Detecting Adobe updater retry-loop orphans (Tier 3 from research — future work)
- Verbose CLI flag (nice-to-have, not essential for this change)
- Selective file moving (previously discussed, still not needed)

## Landing page / GitHub README

This feature is the centrepiece of our messaging. Key points to communicate:

1. "Windows knows which patches are superseded. It just doesn't clean them up."
2. "Adobe Acrobat is the #1 cause. PatchCleaner excludes it. We handle it properly."
3. "We don't guess. We read the Windows Installer API — the authority's own records."
4. "Move first, delete later. If something breaks, copy them back."
5. Real-world numbers from Reddit: 50 GB, 176 GB, 219 GB, 273 GB.
6. The question "how do I know the files are safe?" finally has a proper answer.

Full quotes and research saved in memory/reddit-research-2026-03-03.md.
