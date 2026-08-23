namespace InstallerClean.Models;

/// <summary>
/// A cached installer package (.msi or .msp) some Windows Installer registration
/// names. PatchState: 0 = not a patch, 1 = applied, 2 = superseded, 4 = obsoleted.
///
/// NEARLY EVERY ROW IS KEPT, AND ONE NARROW SHAPE LEAVES FOR THE OFFER. A row is
/// offered only where Windows positively reported the patch SUPERSEDED, its own
/// <c>Uninstallable</c> positively read zero, <see cref="ProductPatchSetVerdict"/>
/// came back clean across every product sharing the patch, the file is on disk and
/// the containment guard passed it. Everything else on this record is kept, and
/// nothing here grants that verdict on its own: see
/// <see cref="Services.IFileSystemScanService"/> for where the row is read and
/// <see cref="ProductPatchSetVerdict"/> for what the flags do and do not establish.
///
/// A SUPERSEDED PATCH IS ONE WINDOWS STILL HOLDS A RECORD OF, in Microsoft's own
/// words "applied to this product instance but is superseded"
/// (learn.microsoft.com/en-us/windows/win32/msi/patch-state). THAT IS WHY THE STATE
/// IS NOT ENOUGH ON ITS OWN and never decides anything by itself. Windows holding a
/// record is exactly the condition under which uninstalling the superseding patch
/// can reach back for the cached file, so the state opens the question and the
/// per-product condition is what answers it.
///
/// <see cref="PatchState"/> SEPARATES THEM FOR REPORTING AND IS PART OF WHAT ACTS.
/// What the state says about a file that has GONE from disk is still nothing:
/// Windows opens every patch registered to a product whether or not it has been
/// superseded, so a record pointing at an absent file is the same condition
/// whichever state it carries (see <see cref="IsMissingFromDisk"/>).
///
/// RemovableWithheld and VerdictUnreadable are both live and neither is machinery
/// kept against a class coming back. VerdictUnreadable means a patch's State or
/// Uninstallable read failed, so nothing was established about that registration
/// either way. RemovableWithheld means a removable verdict was taken away, which is
/// a thing that happens on any scan that withholds one. Nothing may put a row
/// carrying either flag under a sentence that names a claim, because there is no
/// claim to name.
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
/// <param name="WithheldOnUnreadableFile">
/// The row's removable verdict was taken away for one reason and one only: the pass
/// that reads a patch file to ask what products it declares could not read the file the
/// record names. A cause, and nothing besides. It settles nothing on its own and it is
/// not a claim that the file is absent.
///
/// IT EXISTS BECAUSE THAT ONE CAUSE CARRIES TWO MEANINGS AND THE ENUMERATION CANNOT TELL
/// THEM APART. A file that is THERE and will not give up an identity is the app unable to
/// establish something it could have established, and the withholding is a finding worth
/// acting on. A file that is NOT THERE cannot be read by anybody, so the withholding is a
/// tautology and says nothing at all about the machine. The enumeration has no filesystem
/// to ask which it met; the scan has, and stamps <paramref name="FileExists"/> against the
/// same filesystem it walks. So the cause is recorded here and the two meanings are
/// separated at the one place holding both facts, <c>MissingFilesReport.Affected</c>.
///
/// IT MEANS THAT WAS THE ONLY REASON, AND THE ONLY REASON IS WHAT MAKES IT READABLE. A row
/// carrying this flag carries no other withholding cause. That comes from the shape of the
/// downgrade rather than from any bookkeeping: every downgrade is one-way and the first one
/// wins, a row already withheld is skipped, and one call site sets this flag. So no earlier
/// cause can be sitting underneath it and no later one can be added on top.
///
/// AND IT IS A PROPERTY ONE PLACE MAINTAINS RATHER THAN ONE THE TYPE ENFORCES, which is the
/// standing hazard here and the reason this paragraph exists. Anything new that withholds a
/// row has to decide the same question, and getting it wrong is silent in both directions:
/// a cause joined to this one makes the flag overstate what it knows, and a row withheld
/// for something else while this flag stands makes it say an unread file was the whole of
/// it when it was not.
///
/// THE SCAN-WIDE WITHHOLDING USED TO BE THE SECOND PLACE AND NO LONGER IS. It runs after the
/// confirmation loop and only touches rows still carrying <paramref name="IsRemovable"/>, so
/// it passes over a row this flag has already taken; until 3.0.0 it had a second arm that
/// CLEARED the flag on any run that came up short of a product. That arm is gone. What it
/// produced was a warning about a file whose absence the same scan had positively
/// established to be harmless, fired on a count that says something about the machine and
/// nothing about this patch. The run that came up short still withholds the whole removable
/// class, which is where withholding can still change an outcome.
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
    bool FileExists = true,
    bool WithheldOnUnreadableFile = false)
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
