namespace InstallerClean.Models;

/// <summary>
/// The output of a single <c>FileSystemScanService</c> run. The whole UI
/// state derives from this record: the orphan list, the registered list,
/// the size totals on the main screen, and the discrepancy banner are
/// all functions of these fields.
/// </summary>
/// <param name="RemovableFiles">
/// Files in <c>C:\Windows\Installer</c> that the API does not claim plus
/// patches that the API marks superseded or obsoleted and whose file is
/// still on disk. Safe to move or recycle. Superseded entries whose
/// underlying file is already gone count against
/// <see cref="MissingFromDiskCount"/> rather than appearing here, because
/// a Move or Delete would fail with MissingSourceFile.
/// </param>
/// <param name="RegisteredPackages">
/// <c>LocalPackage</c> paths the API still claims that aren't marked
/// superseded or obsoleted. Superseded patches go into
/// <see cref="RemovableFiles"/> instead. Drives the registered list
/// and the totals on the main screen. On a scan with a non-zero
/// <see cref="UnreadableProductCount"/> the superseded patches are in here as
/// well, carrying <c>RemovableWithheld</c>: that scan is keeping them, so it
/// counts them among the files it is keeping.
/// </param>
/// <param name="RegisteredTotalBytes">
/// Sum of <see cref="RegisteredPackage.FileSizeBytes"/> across
/// <see cref="RegisteredPackages"/> where the file actually exists on
/// disk. Excludes <see cref="MissingFromDiskCount"/> entries so the
/// total never includes non-existent files.
/// </param>
/// <param name="MissingNonRemovableCount">
/// Packages the API still treats as in-use but whose <c>LocalPackage</c>
/// file is missing on disk. A non-zero value is the load-bearing signal
/// for the missing-from-disk banner: it means another tool removed
/// files Windows still references and a future install / uninstall /
/// patch will fail when it goes looking for them.
/// </param>
/// <param name="MissingRemovableCount">
/// Packages the API has marked superseded or obsoleted whose file is
/// already gone from disk. Benign: Windows considers these removable,
/// the file has already been removed, the entry is just leftover MSI
/// registration. Counted separately from
/// <see cref="MissingNonRemovableCount"/> so the banner only fires for
/// the actionable case. A superseded package whose verdict this scan withheld
/// counts here too: the file having gone is the same expected end state either
/// way, and only this scan's verdict was withheld.
/// </param>
/// <param name="UnreadableProductCount">
/// Installed products whose Windows Installer records this scan could not fully
/// read. Non-zero means the scan withheld every superseded-patch verdict, so
/// <see cref="RemovableFiles"/> carries orphans only; the summary says so. Zero
/// on any healthy machine, which is why nothing else keys off it.
/// </param>
public record ScanResult(
    IReadOnlyList<OrphanedFile> RemovableFiles,
    IReadOnlyList<RegisteredPackage> RegisteredPackages,
    long RegisteredTotalBytes,
    int MissingNonRemovableCount = 0,
    int MissingRemovableCount = 0,
    int UnreadableProductCount = 0)
{
    /// <summary>Total registered packages missing on disk; sum of the two sub-counts.</summary>
    public int MissingFromDiskCount => MissingNonRemovableCount + MissingRemovableCount;
}
