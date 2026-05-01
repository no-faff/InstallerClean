namespace InstallerClean.Models;

/// <summary>
/// The output of a single <c>FileSystemScanService</c> run. The whole UI
/// state derives from this record: the orphan list, the registered list,
/// the size totals on the main screen, and the discrepancy banner are
/// all functions of these fields.
/// </summary>
/// <param name="RemovableFiles">
/// Files in <c>C:\Windows\Installer</c> that the API does not claim plus
/// patches that the API marks superseded or obsoleted. Safe to move or
/// recycle.
/// </param>
/// <param name="RegisteredPackages">
/// Every <c>LocalPackage</c> path the API still claims, with metadata
/// for the registered-files window. Drives both the registered list and
/// the totals on the main screen.
/// </param>
/// <param name="RegisteredTotalBytes">
/// Sum of <see cref="RegisteredPackage.FileSizeBytes"/> across
/// <see cref="RegisteredPackages"/> where the file actually exists on
/// disk. Excludes <see cref="MissingFromDiskCount"/> entries so the
/// total never includes non-existent files.
/// </param>
/// <param name="MissingFromDiskCount">
/// Packages the API still claims but whose <c>LocalPackage</c> file is
/// missing on disk. Surfaced as an info banner: a non-zero count usually
/// means a previous third-party cleaner removed files Windows still
/// expects to be there.
/// </param>
public record ScanResult(
    IReadOnlyList<OrphanedFile> RemovableFiles,
    IReadOnlyList<RegisteredPackage> RegisteredPackages,
    long RegisteredTotalBytes,
    int MissingFromDiskCount = 0);
