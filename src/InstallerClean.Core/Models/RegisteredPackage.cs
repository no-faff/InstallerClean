namespace InstallerClean.Models;

/// <summary>
/// A cached installer package (.msi or .msp) some Windows Installer registration
/// names. PatchState: 0 = not a patch, 1 = applied, 2 = superseded, 4 = obsoleted.
///
/// EVERY ROW IS KEPT. Nothing here decides that a file may be removed, and no
/// verdict on this record puts a file into the offer: the offer is built from the
/// files no registration names at all. A patch Windows reports superseded or
/// obsoleted is a patch Windows still holds, in Microsoft's own words "applied to
/// this product instance but is superseded"
/// (learn.microsoft.com/en-us/windows/win32/msi/patch-state), and it stays on this
/// side of the scan with every other registration.
///
/// <see cref="PatchState"/> IS THE ONLY THING THAT SEPARATES THEM, and it separates
/// them for reporting rather than for acting. What the state says about a file that
/// has GONE from disk is nothing: Windows opens every patch registered to a product
/// whether or not it has been superseded, so a record pointing at an absent file is
/// the same condition whichever state it carries (see
/// <see cref="IsMissingFromDisk"/>).
///
/// RemovableWithheld and VerdictUnreadable both survive from the arrangement where
/// this record could carry a removable verdict. VerdictUnreadable is still written
/// on every scan and still means what it says: a patch's State or Uninstallable
/// read failed, so nothing was established about that registration either way.
/// RemovableWithheld is written only where a removable verdict is taken away, and
/// a verdict is granted again from 3.0.0, so it is set on any scan that withholds
/// one; the code that
/// writes it is kept because it is the machinery the class would need if it ever
/// came back. Nothing may put a row carrying either flag under a sentence that
/// names a claim, because there is no claim to name.
/// </summary>
/// <param name="ProductPatchSetVerdict">
/// Whether anything on any product this registration is registered under could be
/// uninstalled and roll back onto its cached file, across the union of every source
/// that can see those products' patch sets.
///
/// SEPARATE FROM <paramref name="IsRemovable"/> BECAUSE THAT FLAG CONFLATES THREE
/// FACTS AND THIS IS ONLY ONE OF THEM. A row is removable where the state read
/// SUPERSEDED, its own <c>Uninstallable</c> positively read zero, AND this verdict is
/// clean. So a clean verdict cannot be recovered from the flag: an OBSOLETED row on
/// entirely clean products is not removable, because the rule gates on superseded, and
/// is then indistinguishable from a superseded row that failed this very condition.
/// Those two need opposite treatment where a missing file is being judged, which is
/// what this member exists for.
///
/// IT IS STAMPED FOR PATCH ROWS ONLY, meaning a state of 2 or 4, and defaults to
/// <see cref="ProductPatchSet.Unestablished"/> everywhere else. That is not a claim
/// about those other rows: it is the honest default for a question nobody asked of
/// them. Its two consumers only ever read it for a patch row, because a product's own
/// package and an applied patch can be neither offered from the registered set nor
/// called a benign absence.
///
/// DEFAULTING TO UNESTABLISHED RATHER THAN CLEAN IS LOAD-BEARING. A row nobody judged
/// must not read as a row judged safe, and every consumer treats this value's absence
/// of a positive as a reason to act cautiously: to withhold on the offer, and to raise
/// the alarm on a missing file.
/// </param>
public record RegisteredPackage(
    string LocalPackagePath,
    string ProductName,
    string ProductCode,
    int PatchState = 0,
    bool IsRemovable = false,
    bool RemovableWithheld = false,
    bool VerdictUnreadable = false,
    ProductPatchSet ProductPatchSetVerdict = ProductPatchSet.Unestablished,
    long FileSizeBytes = 0,
    bool FileExists = true)
{
    /// <summary>
    /// Windows holds a record naming this file and the file is not there. The one
    /// condition the missing-from-disk report is for, and the whole of it.
    ///
    /// IT USED TO EXCLUDE SUPERSEDED AND OBSOLETED PATCHES AND THAT WAS WRONG. The
    /// exclusion rested on the file having gone being those patches' expected end
    /// state, which rested in turn on the state meaning Windows had finished with
    /// the file. It does not. Microsoft's own Windows Installer engineer, on
    /// Microsoft's setup blog on 16 August 2008: "Windows Installer will always
    /// open every patch registered to a product whether or not it has already been
    /// obsolesced or superseded when opening a product or package handle", and a
    /// cached file that has gone then gives error 1635. Microsoft's currently
    /// maintained article on this exact state (KB 971187) tests for it without any
    /// carve-out: "if the LocalPackage string value or referenced package is
    /// missing, the product is affected".
    ///
    /// SO NO SENTENCE MAY GRADE THE TWO. The consequence is the same, the recovery
    /// step is the same, and the only thing that ever differed is what removed the
    /// file, which no surface may speak to (any tool that removed one, this one
    /// included up to v2.3.0, leaves an identical record). The split survives as
    /// two counts on <see cref="ScanResult"/> so the data keeps it; the copy does
    /// not.
    /// </summary>
    public bool IsMissingFromDisk => !FileExists;

    /// <summary>
    /// Windows reports this registration as a patch it has superseded (2) or
    /// obsoleted (4). Both are sub-states of applied and neither says the cached
    /// file is spare; this is a label on the record, not a verdict on the file.
    /// </summary>
    public bool IsSupersededOrObsoleted => PatchState is 2 or 4;
}
