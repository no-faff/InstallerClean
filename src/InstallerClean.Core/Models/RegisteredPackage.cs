namespace InstallerClean.Models;

/// <summary>
/// A cached installer package (.msi or .msp) some Windows Installer registration
/// names. PatchState: 0 = not a patch, 1 = applied, 2 = superseded, 4 = obsoleted.
///
/// A ROW IS NOT A STATEMENT THAT THE FILE IS NEEDED, and reading it as one is the
/// mistake the two flags below exist to stop. Most rows do carry a product's live
/// claim, but a row can equally be a patch the API positively called removable on
/// a scan that could not confirm it, or one whose verdict never read at all.
///
/// RemovableWithheld marks the first: the API called it removable and this scan's
/// product enumeration was incomplete, so the verdict was withheld, IsRemovable
/// reads false and the row is kept. It is a separate flag rather than a plain
/// false because the two are not the same fact downstream. A genuinely needed
/// file missing from disk is the load-bearing alarm signal (an install, uninstall
/// or repair will fail on it); a withheld patch missing from disk is the ordinary
/// end state of a patch Windows itself calls removable, and counting it as the
/// alarm would tell the user their machine has a problem it does not have.
///
/// VerdictUnreadable marks the second, and the difference from its neighbour is
/// what the API DID: there, it answered and the scan could not act on the answer;
/// here, it did not answer. A patch's State or Uninstallable read that fails
/// leaves the row non-removable, which is the safe direction and is not in
/// question, but it leaves nothing established about the file either way. Nothing
/// may put such a row under a sentence that names a claim, because there is no
/// claim to name.
/// </summary>
public record RegisteredPackage(
    string LocalPackagePath,
    string ProductName,
    string ProductCode,
    int PatchState = 0,
    bool IsRemovable = false,
    bool RemovableWithheld = false,
    bool VerdictUnreadable = false,
    long FileSizeBytes = 0,
    bool FileExists = true)
{
    /// <summary>
    /// Windows still claims this file, it is gone from disk, and that is a
    /// problem: the one condition the missing-from-disk warning is for. Both
    /// removable and withheld rows are excluded, because for either one the file
    /// having gone is the expected end state and nothing will fail over it.
    ///
    /// A VERDICT THAT NEVER READ IS NOT EXCLUDED, which is the opposite direction
    /// to its sibling flag and is deliberate. A withheld row was positively read
    /// as removable, so its file going is expected; an unread one may be an
    /// applied patch, whose file going is exactly what the warning is for. The
    /// warning says a future repair, update or uninstall COULD fail, which is
    /// true of a file whose state nobody could read, where excluding it would
    /// keep a real problem off the screen to keep a count tidy.
    /// </summary>
    public bool IsMissingAndNeeded => !FileExists && !IsRemovable && !RemovableWithheld;
}
