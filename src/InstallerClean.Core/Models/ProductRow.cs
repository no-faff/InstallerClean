namespace InstallerClean.Models;

/// <summary>
/// A row in the Registered Files details grid. Groups an MSI package with
/// its patches so the UI can sort and display them as a single product.
/// </summary>
public sealed record ProductRow(
    string ProductName,
    string FileName,
    string FullPath,
    string SizeDisplay,
    long SizeBytes,
    int PatchCount,
    IReadOnlyList<PatchRow> Patches,
    // True when the product's representative file is still registered with
    // Windows Installer but absent from disk. Its summary metadata is read
    // from the file, so it is unavailable for a missing row.
    bool IsMissing = false);
