namespace InstallerClean.Models;

/// <summary>
/// A cached installer package (.msi or .msp) still registered with the
/// Windows Installer API, i.e. still needed by an installed product.
/// PatchState: 0 = not a patch, 1 = applied, 2 = superseded, 4 = obsoleted.
///
/// RemovableWithheld marks a patch the API called removable on a scan whose
/// product enumeration was incomplete: the verdict was withheld, so IsRemovable
/// reads false and the row is treated as needed. It is a separate flag rather
/// than a plain false because the two are not the same fact downstream. A
/// genuinely needed file missing from disk is the load-bearing alarm signal (an
/// install, uninstall or repair will fail on it); a withheld patch missing from
/// disk is the ordinary end state of a patch Windows itself calls removable, and
/// counting it as the alarm would tell the user their machine has a problem it
/// does not have.
/// </summary>
public record RegisteredPackage(
    string LocalPackagePath,
    string ProductName,
    string ProductCode,
    int PatchState = 0,
    bool IsRemovable = false,
    bool RemovableWithheld = false,
    long FileSizeBytes = 0,
    bool FileExists = true)
{
    /// <summary>
    /// Windows still claims this file, it is gone from disk, and that is a
    /// problem: the one condition the missing-from-disk warning is for. Both
    /// removable and withheld rows are excluded, because for either one the file
    /// having gone is the expected end state and nothing will fail over it.
    /// </summary>
    public bool IsMissingAndNeeded => !FileExists && !IsRemovable && !RemovableWithheld;
}
