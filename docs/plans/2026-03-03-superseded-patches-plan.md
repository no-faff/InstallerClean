# Superseded Patches Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Detect superseded/obsoleted Windows Installer patches and offer to remove them alongside orphaned files, solving the Adobe Acrobat bloat problem.

**Architecture:** Add "State" and "Uninstallable" property queries to InstallerQueryService. Superseded non-uninstallable patches join orphaned files in a combined "removable" list with a Reason tag. Remove the Filters UI entirely. Add registry fallback for robustness. Clean empty subdirs after operations.

**Tech Stack:** C# / WPF / .NET 8 / CommunityToolkit.Mvvm / xUnit + Moq

---

### Task 1: Add Reason field to OrphanedFile model

**Files:**
- Modify: `src/SimpleWindowsInstallerCleaner/Models/OrphanedFile.cs`

**Step 1: Add Reason parameter to OrphanedFile record**

```csharp
public record OrphanedFile(
    string FullPath,
    long SizeBytes,
    bool IsPatch,
    string Reason = "Orphaned")
```

All existing callers pass positional args for the first 3 params, so the default
value means no existing code breaks.

**Step 2: Build to verify nothing breaks**

Run: `dotnet build src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj`
Expected: success

**Step 3: Run tests**

Run: `dotnet test src/SimpleWindowsInstallerCleaner.Tests/`
Expected: 24 passing

**Step 4: Commit**

```
git add src/SimpleWindowsInstallerCleaner/Models/OrphanedFile.cs
git commit -m "feat: add Reason field to OrphanedFile model"
```

---

### Task 2: Add PatchState constants and property names to MsiConstants

**Files:**
- Modify: `src/SimpleWindowsInstallerCleaner/Interop/MsiConstants.cs:65-72`

**Step 1: Add State and Uninstallable to MsiInstallProperty**

```csharp
public static class MsiInstallProperty
{
    public const string LocalPackage = "LocalPackage";
    public const string ProductName = "ProductName";
    public const string State = "State";
    public const string Uninstallable = "Uninstallable";
}
```

**Step 2: Build**

Run: `dotnet build src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj`
Expected: success

**Step 3: Commit**

```
git add src/SimpleWindowsInstallerCleaner/Interop/MsiConstants.cs
git commit -m "feat: add State and Uninstallable MSI property constants"
```

---

### Task 3: Query patch state in InstallerQueryService

**Files:**
- Modify: `src/SimpleWindowsInstallerCleaner/Models/RegisteredPackage.cs`
- Modify: `src/SimpleWindowsInstallerCleaner/Services/InstallerQueryService.cs:62-96`

**Step 1: Add PatchState and IsRemovable to RegisteredPackage**

```csharp
public record RegisteredPackage(
    string LocalPackagePath,
    string ProductName,
    string ProductCode,
    int PatchState = 0,
    bool IsRemovable = false);
```

PatchState values: 0 = not a patch (MSI product), 1 = applied, 2 = superseded,
4 = obsoleted. IsRemovable = true when (PatchState is 2 or 4) AND not uninstallable.

**Step 2: Update patch enumeration in InstallerQueryService**

In `GetRegisteredPackagesCore`, after getting `patchPath` (line 84), query state
and uninstallable:

```csharp
foreach (var (patchCode, patchUserSid, patchContext) in patches)
{
    ct.ThrowIfCancellationRequested();

    var patchPath = GetPatchProperty(patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.LocalPackage);

    if (!string.IsNullOrEmpty(patchPath))
    {
        var stateStr = GetPatchProperty(patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.State);
        var uninstallableStr = GetPatchProperty(patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.Uninstallable);

        int.TryParse(stateStr, out var patchState);
        var isSuperseded = patchState is 2 or 4;
        var isUninstallable = uninstallableStr == "1";
        var isRemovable = isSuperseded && !isUninstallable;

        claimed.TryAdd(patchPath, new RegisteredPackage(patchPath, productName, productCode, patchState, isRemovable));
    }
}
```

Product MSIs (not patches) keep the defaults: PatchState=0, IsRemovable=false.

**Step 3: Build and test**

Run: `dotnet build src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj`
Run: `dotnet test src/SimpleWindowsInstallerCleaner.Tests/`
Expected: all pass (existing tests don't mock InstallerQueryService's internals)

**Step 4: Commit**

```
git add src/SimpleWindowsInstallerCleaner/Models/RegisteredPackage.cs
git add src/SimpleWindowsInstallerCleaner/Services/InstallerQueryService.cs
git commit -m "feat: query patch state and uninstallable properties"
```

---

### Task 4: Update FileSystemScanService to combine removable + orphaned

**Files:**
- Modify: `src/SimpleWindowsInstallerCleaner/Models/ScanResult.cs`
- Modify: `src/SimpleWindowsInstallerCleaner/Services/FileSystemScanService.cs`

**Step 1: Update ScanResult**

```csharp
public record ScanResult(
    IReadOnlyList<OrphanedFile> RemovableFiles,
    IReadOnlyList<RegisteredPackage> RegisteredPackages,
    long RegisteredTotalBytes);
```

Rename `OrphanedFiles` to `RemovableFiles`. This list now contains both orphaned
files AND superseded patches (as OrphanedFile records with different Reasons).

**Step 2: Update FileSystemScanService.ScanAsync**

After building the orphans list (line 74), extract removable registered packages
and convert them to OrphanedFile records with Reason="Superseded":

```csharp
// Superseded registered patches that are safe to remove
var removable = new List<OrphanedFile>(orphans); // start with orphans

foreach (var pkg in registered.Where(p => p.IsRemovable))
{
    cancellationToken.ThrowIfCancellationRequested();

    long size = 0;
    try { if (File.Exists(pkg.LocalPackagePath)) size = new FileInfo(pkg.LocalPackagePath).Length; }
    catch { }

    var ext = Path.GetExtension(pkg.LocalPackagePath);
    removable.Add(new OrphanedFile(
        FullPath: pkg.LocalPackagePath,
        SizeBytes: size,
        IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase),
        Reason: "Superseded"));
}

// Filter removable packages out of the registered list for the "still used" count
var stillUsed = registered.Where(p => !p.IsRemovable).ToList().AsReadOnly();
long stillUsedBytes = 0;
foreach (var pkg in stillUsed)
{
    try { if (File.Exists(pkg.LocalPackagePath)) stillUsedBytes += new FileInfo(pkg.LocalPackagePath).Length; }
    catch { }
}

progress?.Report($"Found {removable.Count} {DisplayHelpers.Pluralise(removable.Count, "file", "files")} to clean up.");
return new ScanResult(removable.AsReadOnly(), stillUsed, stillUsedBytes);
```

Remove the earlier `registeredBytes` calculation loop (lines 39-48) since we now
calculate it after filtering.

**Step 3: Fix all references to ScanResult.OrphanedFiles**

Search for `OrphanedFiles` references and rename to `RemovableFiles`:
- `MainViewModel.cs` line 131: `_lastScanResult.OrphanedFiles` → `_lastScanResult.RemovableFiles`
- `MainViewModel.cs` line 454: `_lastScanResult.OrphanedFiles` → `_lastScanResult.RemovableFiles`
- Any test files referencing OrphanedFiles

**Step 4: Build and test**

Run: `dotnet build src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj`
Run: `dotnet test src/SimpleWindowsInstallerCleaner.Tests/`
Expected: all pass

**Step 5: Commit**

```
git commit -m "feat: combine orphaned and superseded files into removable list"
```

---

### Task 5: Remove Filters UI and exclusion pipeline

**Files:**
- Modify: `src/SimpleWindowsInstallerCleaner/MainWindow.xaml:80-90,188-194`
- Modify: `src/SimpleWindowsInstallerCleaner/ViewModels/MainViewModel.cs`
- Modify: `src/SimpleWindowsInstallerCleaner/App.xaml.cs`
- Modify: `src/SimpleWindowsInstallerCleaner/Models/AppSettings.cs`
- Keep (don't delete): `ExclusionService.cs`, `IExclusionService.cs`, `SettingsWindow.xaml`,
  `SettingsViewModel.cs` — keep code in repo but unwire from GUI

**Step 1: Remove ExclusionService from App.xaml.cs startup**

In `OnStartup` (line 69): remove `var exclusionService = new ExclusionService();`
In MainViewModel constructor call (line 73-75): remove `exclusionService` parameter.

In `RunCliAsync` (line 158-164): remove exclusionService, msiInfoService, and the
`ApplyFilters` call. The scan result's RemovableFiles is already the final list.

**Step 2: Remove exclusion from MainViewModel**

- Remove `IExclusionService` field and constructor parameter
- Remove `IMsiFileInfoService` field and constructor parameter (only used for exclusion)
- Remove `_lastFilteredResult` field
- Remove `ExcludedFileCount`, `ExcludedSizeDisplay`, `ExcludedFilterDisplay`,
  `HasExcludedFiles` properties
- Replace all `_lastFilteredResult.Actionable` with `_lastScanResult.RemovableFiles`
- Remove `OpenSettingsCommand` and `OpenSettingsAsync` method
- In `RunScanCoreAsync`: remove the filter application block, update counts directly
  from `_lastScanResult.RemovableFiles`

**Step 3: Update MainWindow.xaml**

Remove the "excluded by filters" row (lines 80-90):
```xml
<!-- Excluded by filters -->
<TextBlock Grid.Row="2" Grid.Column="0" ...
```

Change "files orphaned" to "files to clean up" (line 119):
```xml
<Run Text=" files to clean up" ...
```

Remove Filters button from bottom nav (lines 189-190):
```xml
<Button DockPanel.Dock="Left" Style="{StaticResource GhostPill}"
        Content="Filters" Command="{Binding OpenSettingsCommand}"/>
```

**Step 4: Remove default Acrobat filter from AppSettings**

```csharp
public sealed class AppSettings
{
    public string MoveDestination { get; set; } = string.Empty;
    public List<string> ExclusionFilters { get; set; } = new();
}
```

Remove the `DefaultExclusionFilter` constant and the default list value.

**Step 5: Build and fix any remaining references**

Run: `dotnet build src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj`
Fix any compilation errors from removed properties/commands.

**Step 6: Run tests**

Run: `dotnet test src/SimpleWindowsInstallerCleaner.Tests/`
Some ExclusionService tests may still pass (the service code stays). Fix any
tests that reference removed MainViewModel properties.

**Step 7: Commit**

```
git commit -m "feat: remove Filters UI, exclusion pipeline no longer needed"
```

---

### Task 6: Update OrphanedFilesViewModel and Window

**Files:**
- Modify: `src/SimpleWindowsInstallerCleaner/ViewModels/OrphanedFilesViewModel.cs`
- Modify: `src/SimpleWindowsInstallerCleaner/OrphanedFilesWindow.xaml`

**Step 1: Simplify OrphanedFilesViewModel**

Remove ExcludedFiles entirely — no more excluded section:

```csharp
public partial class OrphanedFilesViewModel : ObservableObject
{
    private readonly IMsiFileInfoService _infoService;
    private readonly Dictionary<string, MsiSummaryInfo?> _cache = new();

    public IReadOnlyList<OrphanedFile> Files { get; }
    public string Summary { get; }

    // ... (SelectedFile, SelectedDetails, Has/Show properties stay the same)

    public OrphanedFilesViewModel(
        IReadOnlyList<OrphanedFile> files,
        IMsiFileInfoService infoService)
    {
        _infoService = infoService;
        Files = files.OrderByDescending(f => f.SizeBytes).ToList();

        var totalSize = DisplayHelpers.FormatSize(files.Sum(f => f.SizeBytes));
        Summary = $"{files.Count} {DisplayHelpers.Pluralise(files.Count, "file", "files")} ({totalSize})";

        if (Files.Count > 0)
            SelectedFile = Files[0];
    }

    // OnSelectedFileChanged stays the same
}
```

Update the caller in `MainViewModel.OpenOrphanedDetails` to pass the new signature.

**Step 2: Update OrphanedFilesWindow.xaml**

Remove the entire excluded section (Grid.Row="1" StackPanel and Grid.Row="2" ListBox).
The left column becomes just one ListBox.

Add a "Reason" column to the list item template:

```xml
<ListBox.ItemTemplate>
    <DataTemplate>
        <Grid Margin="0,1,14,1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="80"/>
                <ColumnDefinition Width="72"/>
            </Grid.ColumnDefinitions>
            <TextBlock Grid.Column="0" Text="{Binding FileName}"
                       Foreground="#f8fafc" FontSize="14"
                       TextTrimming="CharacterEllipsis" ToolTip="{Binding FullPath}"/>
            <TextBlock Grid.Column="1" Text="{Binding Reason}"
                       Foreground="#64748b" FontSize="11"
                       VerticalAlignment="Center"/>
            <TextBlock Grid.Column="2" Text="{Binding SizeDisplay}"
                       Foreground="#cbd5e1" FontSize="12" TextAlignment="Right"/>
        </Grid>
    </DataTemplate>
</ListBox.ItemTemplate>
```

Also add "Reason" to the detail panel (right side) as an extra row at the top.

Update the window title from "Orphaned files" to "Files to clean up" (or similar).

Remove the code-behind GotFocus event handlers (ActionableList_GotFocus,
ExcludedList_GotFocus) — only one list now. Update OrphanedFilesWindow.xaml.cs.

**Step 3: Build and test**

Run: `dotnet build src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj`
Run: `dotnet test src/SimpleWindowsInstallerCleaner.Tests/`

**Step 4: Commit**

```
git commit -m "feat: add Reason column to details window, remove excluded section"
```

---

### Task 7: Update CLI

**Files:**
- Modify: `src/SimpleWindowsInstallerCleaner/App.xaml.cs:119-226`

**Step 1: Simplify RunCliAsync**

Remove exclusionService, msiInfoService, and the ApplyFilters call.
Use scanResult.RemovableFiles directly:

```csharp
Console.WriteLine("Scanning C:\\Windows\\Installer...");
var scanResult = await scanService.ScanAsync();

var count = scanResult.RemovableFiles.Count;
var size = DisplayHelpers.FormatSize(scanResult.RemovableFiles.Sum(f => f.SizeBytes));
Console.WriteLine($"Found {count} {DisplayHelpers.Pluralise(count, "file", "files")} to clean up ({size}).");

if (count == 0)
{
    Console.WriteLine("Nothing to do.");
    Shutdown(0);
    return;
}

var filePaths = scanResult.RemovableFiles.Select(f => f.FullPath).ToList();
```

Update help text: "Delete orphaned files" → "Delete removable files" (or keep
generic: "Clean up files").

**Step 2: Build**

Run: `dotnet build src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj`

**Step 3: Commit**

```
git commit -m "feat: update CLI to use combined removable files list"
```

---

### Task 8: Empty subdirectory cleanup

**Files:**
- Modify: `src/SimpleWindowsInstallerCleaner/Services/MoveFilesService.cs`
- Modify: `src/SimpleWindowsInstallerCleaner/Services/DeleteFilesService.cs`

**Step 1: Write a shared helper**

Create a static method (can go in either service or a helper class):

```csharp
internal static void PruneEmptySubdirectories(string rootFolder)
{
    if (!Directory.Exists(rootFolder)) return;

    foreach (var dir in Directory.EnumerateDirectories(rootFolder, "*", SearchOption.AllDirectories)
        .OrderByDescending(d => d.Length)) // deepest first
    {
        try
        {
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
                Directory.Delete(dir);
        }
        catch { /* skip protected directories */ }
    }
}
```

**Step 2: Call after move/delete operations**

In MoveFilesService.MoveFilesAsync, after the loop and before the return:
```csharp
var installerFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");
PruneEmptySubdirectories(installerFolder);
```

Same in DeleteFilesService.DeleteFilesAsync.

**Step 3: Build and test**

Run: `dotnet build src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj`
Run: `dotnet test src/SimpleWindowsInstallerCleaner.Tests/`
Expected: all pass (tests use temp dirs, not C:\Windows\Installer)

**Step 4: Commit**

```
git commit -m "feat: prune empty subdirectories after move/delete"
```

---

### Task 9: Registry fallback for patch detection

**Files:**
- Modify: `src/SimpleWindowsInstallerCleaner/Services/InstallerQueryService.cs`

**Step 1: Add registry walk as supplementary data source**

After the main enumeration loop in `GetRegisteredPackagesCore`, add a registry
walk that finds any registered packages the API might have missed:

```csharp
// Registry fallback: catch packages the API might miss
progress?.Report("Checking registry for additional packages...");
try
{
    var udKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData");
    if (udKey is not null)
    {
        foreach (var sidName in udKey.GetSubKeyNames())
        {
            ct.ThrowIfCancellationRequested();

            // Products
            var productsKey = udKey.OpenSubKey($@"{sidName}\Products");
            if (productsKey is not null)
            {
                foreach (var prodGuid in productsKey.GetSubKeyNames())
                {
                    var ipKey = productsKey.OpenSubKey($@"{prodGuid}\InstallProperties");
                    var localPkg = ipKey?.GetValue("LocalPackage") as string;
                    if (!string.IsNullOrEmpty(localPkg))
                        claimed.TryAdd(localPkg, new RegisteredPackage(localPkg, "", ""));
                }
            }

            // Patches
            var patchesKey = udKey.OpenSubKey($@"{sidName}\Patches");
            if (patchesKey is not null)
            {
                foreach (var patchGuid in patchesKey.GetSubKeyNames())
                {
                    var patchKey = patchesKey.OpenSubKey(patchGuid);
                    var localPkg = patchKey?.GetValue("LocalPackage") as string;
                    if (!string.IsNullOrEmpty(localPkg))
                        claimed.TryAdd(localPkg, new RegisteredPackage(localPkg, "", ""));
                }
            }
        }
    }
}
catch { /* registry fallback is best-effort */ }
```

Note: `TryAdd` means this only adds entries NOT already found by the API.
These fallback entries have PatchState=0 and IsRemovable=false, so they
are always treated as "still used" — conservative by design.

**Step 2: Build and test**

Run: `dotnet build src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj`
Run: `dotnet test src/SimpleWindowsInstallerCleaner.Tests/`

**Step 3: Commit**

```
git commit -m "feat: registry fallback for more robust package detection"
```

---

### Task 10: Update tests

**Files:**
- Modify: `src/SimpleWindowsInstallerCleaner.Tests/Services/FileSystemScanServiceTests.cs`
- Create: new test for superseded patch detection if needed

**Step 1: Fix any broken tests from ScanResult rename**

Search for `OrphanedFiles` in test files and rename to `RemovableFiles`.

**Step 2: Add test for superseded patches appearing in removable list**

Write a test that verifies: when InstallerQueryService returns a patch with
PatchState=2 and IsRemovable=true, that patch appears in ScanResult.RemovableFiles
with Reason="Superseded".

**Step 3: Add test for registry fallback not marking files as removable**

Write a test that verifies: packages found only via registry fallback have
IsRemovable=false (conservative default).

**Step 4: Run all tests**

Run: `dotnet test src/SimpleWindowsInstallerCleaner.Tests/`
Expected: all pass

**Step 5: Commit**

```
git commit -m "test: update tests for superseded patches and registry fallback"
```

---

### Task 11: Update main window label and explanatory text

**Files:**
- Modify: `src/SimpleWindowsInstallerCleaner/MainWindow.xaml`

**Step 1: Update explanatory text**

Update the help text (line 42-43) to mention superseded patches:

```
C:\Windows\Installer contains cached files used by Windows to repair and update
software. Orphaned files are no longer needed by any installed product. Superseded
patches have been replaced by newer updates. Both are safe to move or delete to
free up space.
```

**Step 2: Build**

Run: `dotnet build src/SimpleWindowsInstallerCleaner/SimpleWindowsInstallerCleaner.csproj`

**Step 3: Final full test run**

Run: `dotnet test src/SimpleWindowsInstallerCleaner.Tests/`
Expected: all pass

**Step 4: Commit**

```
git commit -m "feat: update explanatory text for superseded patches"
```

---

### Task 12: Manual testing and verification

**Steps:**
1. Run the app elevated: `dotnet run --project src/SimpleWindowsInstallerCleaner`
2. Verify scan completes and shows "X files to clean up"
3. Click Details — verify Reason column shows "Orphaned" or "Superseded"
4. Verify Filters button is gone from bottom nav
5. Verify "excluded by filters" line is gone
6. Test CLI: `InstallerClean.exe --help` — verify updated text
7. If Adobe Acrobat is installed, verify superseded patches appear as removable
8. Test Move on a small file, verify empty subdirectory cleanup works

---

## Reminder: Landing page / GitHub README

After implementation, the README and eventual landing page must explain:
- Why Windows doesn't clean these up itself (it tracks the state but doesn't act on it)
- What superseded patches are and why they're safe to remove
- The Adobe story (the #1 cause, 50-273 GB in the wild, nobody else handles it)
- Move-first safety philosophy
- "How do you know the files are safe?" answer
- Real-world numbers from Reddit

Full quotes and lines saved in memory/reddit-research-2026-03-03.md.
