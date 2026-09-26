using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Re-checks a Move or Delete batch against the machine immediately before the
/// action services act on it, and drops every file the check does not confirm.
///
/// It re-runs the full classifier (<see cref="IInstallerQueryService"/>) rather
/// than re-querying a single retained product code, because after the
/// shared-patch verdict merge a patch can revert to Applied for a DIFFERENT
/// product than the one whose code survived the merge, as a superseded patch does
/// when the patch that superseded it is uninstalled.
///
/// A FILE NO REGISTRATION NAMES IS JUDGED AGAIN THE WAY THE SCAN JUDGED IT. A file
/// the API never claimed is not a file it can never claim, and only re-walking the
/// whole registered set can re-establish that nothing claims it. So the enumeration
/// is full rather than per candidate, and on the files it leaves unclaimed the
/// check re-runs, in the scan's order, every other step the scan decided them by:
/// the containment guard, the file-identity comparison, the withholding legs, the
/// declared-product screen and the age check.
///
/// WHAT A CLAIM FOUND HERE DOES NOT ESTABLISH, and no copy built on this may say:
/// that the claim is new. Nothing here records when a registration was written, so
/// all that is shown is the present state of the records.
/// </summary>
public interface IRemovableReverifier
{
    /// <summary>
    /// Re-enumerates the registered set and splits <paramref name="candidatePaths"/>
    /// into those the check confirms and those it does not, which must be dropped
    /// from the batch and reported as held back. An empty input short-circuits
    /// without querying. Propagates any exception the enumeration raises: an
    /// inability to re-verify stops the batch rather than passing it.
    /// </summary>
    Task<ReverifyResult> ReverifyAsync(
        IReadOnlyList<string> candidatePaths,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-reads the given patch claims and returns the paths the re-read has not
    /// left shown to be removable, for the action services to call ONCE THEY HOLD
    /// <c>Global\_MSIExecute</c> and before they touch a file. The paths are not all
    /// ones whose verdict turned: one of the three causes is a read that established
    /// nothing either way, and it keeps the file for want of a verdict rather than on
    /// one.
    ///
    /// It is the read of the records made closest to the act. The hold is taken
    /// inside the action service, after every caller's full re-verify has finished,
    /// so a verdict that moved while that enumeration ran is read again here,
    /// immediately before the first file is touched.
    ///
    /// Synchronous, and that is a requirement rather than a convenience: the
    /// lease must be released by the thread that took it, so the whole hold is one
    /// unbroken synchronous body with no await in it to hop threads.
    ///
    /// WHAT IT RE-ASKS. It re-asks about claims that existed when the claims were
    /// collected, so it catches a verdict changing on one of them, which is the
    /// reverting superseded patch the full re-verify is for. A product that held no
    /// claim then gives it nothing to re-ask about, and the full re-verify's own
    /// enumeration, moments earlier, is what reads such a product. Do not move that
    /// enumeration inside the hold: it would hold the machine-wide installer lock
    /// for the length of an enumeration on every run.
    /// </summary>
    /// <param name="claims">
    /// The batch's own pairings and the sibling pairings on the products they name, as
    /// one argument. See <see cref="UnderLeaseClaims"/> for why it is one and not two.
    ///
    /// WHAT THE SIBLING HALF COSTS. The added reads are the patch registrations of the
    /// products the batch touches, so they are bounded by the batch's own products and
    /// never by an enumeration: a batch touching a product with two patch registrations
    /// adds two keyed reads there, and one read for each registration on a product
    /// holding more. Against that, this method already makes two keyed reads per claim
    /// in the batch, and the pre-lease pass runs a whole enumeration moments earlier
    /// outside the lease.
    /// </param>
    UnderLeaseRecheck RecheckUnderLease(UnderLeaseClaims claims);
}

/// <summary>
/// The two claim lists the under-lease re-read needs, carried as ONE argument.
///
/// THAT IS THE WHOLE POINT OF THE TYPE. The re-read needs the batch's own pairings and
/// the sibling pairings on the products those name, because the offer rests on a fact
/// about other patches. As one argument, no caller can hand over the first and leave
/// out the second. <see cref="From"/> is how production builds one, out of the
/// pre-lease pass's own result, so the two halves cannot come from different places.
/// Do not split it into two parameters: a caller could then supply one and receive a
/// weaker check with nothing to show for it.
/// </summary>
/// <param name="Batch">
/// Every claim naming a path still in the batch. Empty short-circuits the re-read
/// without touching the API, which is the ordinary case: most batches are true orphans,
/// which carry no claim to re-read.
/// </param>
/// <param name="Siblings">
/// Every claim on any product one of those paths is registered to, including the batch's
/// own claims, a patch's own removability being part of the condition.
/// </param>
public readonly record struct UnderLeaseClaims(
    IReadOnlyList<PatchClaim> Batch,
    IReadOnlyList<PatchClaim> Siblings)
{
    /// <summary>Nothing to re-read, for a caller with no claims at all.</summary>
    public static UnderLeaseClaims None { get; } =
        new(Array.Empty<PatchClaim>(), Array.Empty<PatchClaim>());

    /// <summary>
    /// The pair a pre-lease re-verify produced. The only route production uses, so the
    /// two lists always come from one enumeration and one another.
    /// </summary>
    public static UnderLeaseClaims From(ReverifyResult reverify) =>
        new(reverify.SurvivingPatchClaims, reverify.SiblingPatchClaims);
}

/// <summary>
/// Why one file was held back. Five, because they are five different things to have
/// found out: a confirmed positive, an inability, neither, one about the machine
/// rather than the file, and one about the file itself.
///
/// NOTHING THE USER READS NAMES ANY OF THEM. The screen and stdout carry one
/// counted sentence naming no cause, on the ground that every file on it was
/// offered by the scan and not confirmed by the check made immediately before
/// acting, which is true of every member by construction. The members are COUNTS:
/// they travel in the opt-in result log and are the only place a machine's causes
/// can be told apart.
///
/// THE FIRST THREE ARE ABOUT THE REGISTRATION THAT NAMES THIS PATH. The fourth is
/// about the machine and rests on nothing about the file. The fifth is about the
/// file, read the way the scan read it. Each is counted separately for that
/// reason.
/// </summary>
public enum HeldBackReason
{
    /// <summary>
    /// The records were read and a registered product's live claim names the file,
    /// where the scan's own reading left it removable.
    ///
    /// BOTH LIMBS OF THAT SENTENCE ARE LOAD-BEARING. A patch row whose State or
    /// Uninstallable read failed is non-removable too, and it names no claim at all:
    /// it is the row being there and nothing more. Such a row carries
    /// <see cref="Models.RegisteredPackage.VerdictUnreadable"/> and counts under
    /// <see cref="RecordsUnreadable"/>, so nothing reaches this cause on a read
    /// that did not answer.
    ///
    /// TWO ROUTES REACH IT and what is said of it has to hold for both. A patch the
    /// scan found superseded or obsoleted whose claim now says needed, back at Applied
    /// or still uninstallable and so needed to roll back with; and a candidate the
    /// scan found no claim on at all, which the re-enumeration finds claimed, by its
    /// path or by the file its path names. The
    /// second is the one the name flatters: nothing was reclaimed, because nothing
    /// this app saw ever held it, and whether the claim is new is not something
    /// either route can be told apart on (see this file's interface remarks). What
    /// is true of both, and the whole of what may be said, is that the records
    /// claim the file now.
    /// </summary>
    Reclaimed,

    /// <summary>
    /// The records were read and no longer hold the registration the claim names.
    /// Not a reclaim, because nothing is left to be in any state at all; not an
    /// unreadable record, because the read succeeded.
    ///
    /// It condemns the file rather than releasing it, which is not what the shape
    /// of the answer suggests. The absence code means "no such product in the
    /// ACCOUNT AND CONTEXT you asked in", not "no such product", and the context a
    /// claim carries was settled when the scan collected it. A pairing that moved
    /// context between the scan and the click therefore answers absent while its
    /// registration is live, which is why this answer keeps the file.
    /// </summary>
    RecordsChanged,

    /// <summary>
    /// The records were not read to a verdict on the file, so nothing was
    /// established either way. It has not shown the file to be removable, which is
    /// what keeps it in place.
    ///
    /// THREE MECHANISMS REACH IT and the sentence is a superordinate over all three
    /// rather than a convenience: a patch's own State or Uninstallable read failing
    /// during the re-verify's enumeration
    /// (<see cref="Models.RegisteredPackage.VerdictUnreadable"/>); a superseded patch
    /// whose product's patch set that enumeration could not establish
    /// (<see cref="Models.RegisteredPackage.RemovableWithheld"/>); and a read under the
    /// installer lease failing: the same pairing's, or the Uninstallable read of a
    /// patch on a product the batch's pairings name, where an answer that the
    /// installation holds no record of the patch or that the product is not installed
    /// counts as failing too. A walk-derived file whose identity matches a row of
    /// either of the first two kinds is counted here as well, the row deciding the
    /// cause. The merged count does not distinguish them.
    ///
    /// Anything else that reaches it is held to the same test against the code that
    /// builds the set, never against this list.
    /// </summary>
    RecordsUnreadable,

    /// <summary>
    /// The re-enumeration met a condition under which the scan itself offers no
    /// walk-derived file at all, so a file this batch carries is one the app would
    /// not put on a list now.
    ///
    /// IT IS ABOUT THE MACHINE AND NOT ABOUT THE FILE, which is what separates it
    /// from the three above. Those are findings about the registration that names
    /// this path: a live claim, a registration that has gone, a read that failed.
    /// This one rests on a fact about the machine and on nothing about the file,
    /// which is why it earns a count of its own even though no sentence names it.
    ///
    /// WHAT REACHES IT is <see cref="WithholdingLegs.Any"/>, the expression the
    /// scan's own withholding asks, put to the re-enumeration's census and to the
    /// registration side of the identity comparison this check re-runs: a leg added
    /// there is acted on here without this file being edited. Several different
    /// findings reach it, which is one reason among several that the copy names no
    /// cause at all.
    ///
    /// IT DROPS THE WALK-DERIVED HALF OF A BATCH AND NOT THE WHOLE OF IT, as the scan
    /// does. A superseded registration is offered beside the walk-derived files, and
    /// those rows are judged by product code and are not touched by either condition,
    /// so dropping them here would keep back files the scan would still offer on the
    /// same machine a moment later. A path no registration names is the walk-derived
    /// half, and that is the test used.
    /// </summary>
    OwnershipUnestablished,

    /// <summary>
    /// A check the scan makes on the file itself, made again just before acting, did
    /// not let the file through: the containment guard did not answer Safe, its own
    /// identity would not read or matches only registrations that are still
    /// removable, the declared-product screen kept it, or its age was not shown to be
    /// a day old.
    ///
    /// IT IS ABOUT THE FILE AND NOT ABOUT A REGISTRATION NAMING ITS PATH, which is what
    /// separates it from the first three, and it is about one file where
    /// <see cref="OwnershipUnestablished"/> is about the machine. Only a file whose
    /// path no registration names reaches it: every walk-derived file, and a
    /// superseded patch's file whose registration has gone by the time of the check.
    /// A file a registration still names is judged by that registration and is not
    /// put to these checks here. The scan puts such a file to the containment guard
    /// where its registration is removable and the file is on disk, and puts none to
    /// the other checks.
    /// </summary>
    FileNotConfirmed,
}

/// <summary>
/// How many files were held back for each cause. Counts rather than one cause for
/// the set, because a batch can meet more than one.
///
/// THEY ARE INSTRUMENTATION, NOT COPY. The report reads <see cref="Total"/> and
/// nothing else, one sentence naming no cause; the counts travel in the opt-in result
/// log, which is the only place the causes can be told apart on a real machine.
///
/// The paths themselves are carried alongside by whichever result holds this. Every
/// producer increments at the point it adds the path, so the two cannot come apart,
/// and <see cref="Total"/> is what a test holds them to.
/// </summary>
public readonly record struct HeldBackReasons(
    int Reclaimed = 0,
    int RecordsChanged = 0,
    int RecordsUnreadable = 0,
    int OwnershipUnestablished = 0,
    int FileNotConfirmed = 0)
{
    /// <summary>
    /// Files held back for any cause, and the ONLY member the report reads: the
    /// user's sentence counts this and names nothing. Equals the accompanying path
    /// list's count.
    /// </summary>
    public int Total =>
        Reclaimed + RecordsChanged + RecordsUnreadable + OwnershipUnestablished + FileNotConfirmed;

    /// <summary>
    /// This tally with one more file counted against <paramref name="reason"/>.
    ///
    /// EVERY MEMBER IS NAMED AND THE DEFAULT THROWS. Do not give the default arm a
    /// cause, <see cref="HeldBackReason.RecordsUnreadable"/> least of all: a member
    /// added to the enum and forgotten here would then compile, build green and be
    /// counted and reported as that cause, a file held back under a sentence naming a
    /// cause that did not occur, with nothing anywhere to see. As it is, a member
    /// added to the enum fails at the first file that reaches it, loudly, rather than
    /// being absorbed.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A cause with no counter, which is a defect in this type and not a machine
    /// state: the enum and this switch are edited together or not at all.
    /// </exception>
    public HeldBackReasons Plus(HeldBackReason reason) => reason switch
    {
        HeldBackReason.Reclaimed => this with { Reclaimed = Reclaimed + 1 },
        HeldBackReason.RecordsChanged => this with { RecordsChanged = RecordsChanged + 1 },
        HeldBackReason.RecordsUnreadable => this with { RecordsUnreadable = RecordsUnreadable + 1 },
        HeldBackReason.OwnershipUnestablished =>
            this with { OwnershipUnestablished = OwnershipUnestablished + 1 },
        HeldBackReason.FileNotConfirmed => this with { FileNotConfirmed = FileNotConfirmed + 1 },
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason,
            "A held-back cause with no counter. Add it to HeldBackReasons, and a "
            + "field for it to the opt-in report's operation block, in the same edit "
            + "as the enum member."),
    };

    /// <summary>
    /// Merges two tallies, for the fold that joins what the pre-act re-verify held
    /// back to what the under-lease re-read did. Addition rather than an OR of
    /// flags: the two producers hold back DIFFERENT files, so their counts
    /// accumulate instead of one standing in for both.
    ///
    /// IT IS WHAT MAKES THE RUN'S ONE LINE COUNT THE WHOLE BATCH. Both hosts fold
    /// through this and print once; anything that merged rather than added would
    /// under-count the only number on that line.
    /// </summary>
    public static HeldBackReasons operator +(HeldBackReasons a, HeldBackReasons b) =>
        new(a.Reclaimed + b.Reclaimed,
            a.RecordsChanged + b.RecordsChanged,
            a.RecordsUnreadable + b.RecordsUnreadable,
            a.OwnershipUnestablished + b.OwnershipUnestablished,
            a.FileNotConfirmed + b.FileNotConfirmed);
}

/// <summary>
/// What one under-lease re-read found.
/// </summary>
/// <param name="HeldBack">
/// The paths to drop, for any of the causes in <see cref="HeldBackReason"/>.
/// </param>
/// <param name="Reasons">
/// How many of <paramref name="HeldBack"/> fell to each cause, for exactly the
/// reason <see cref="ReverifyResult.Reasons"/> carries it: the opt-in result log
/// carries the split, and a count per cause is the only way to size what each
/// condition costs in the field. One re-read can meet more than one of them, and
/// a file's own cause is the only thing that is true of it. A read that could not
/// be made has not shown the file to be removable, so it is held back whichever
/// cause it fell to; what it has not shown is that a program wants it back.
/// </param>
public record UnderLeaseRecheck(
    IReadOnlyList<string> HeldBack,
    HeldBackReasons Reasons = default);

/// <summary>
/// Result of a re-verify. <see cref="Surviving"/> + <see cref="Dropped"/> partition
/// the input: <see cref="Surviving"/> is what the check confirmed, and
/// <see cref="Dropped"/> is what it did not and must be skipped.
/// </summary>
/// <param name="Reasons">
/// How many of <see cref="Dropped"/> fell to each cause. Per file rather than per
/// run: an enumeration that could not read every product withholds the removable
/// class, so a single batch can hold both a file a live registered product claims
/// and a file whose verdict was withheld. Nothing shown to a user distinguishes
/// them; the split is kept because the opt-in result log carries it, and because a
/// count per cause is the only way to size what each condition costs in the field.
/// </param>
/// <param name="SurvivingPatchClaims">
/// Every claim naming a path in <see cref="Surviving"/>, for the action service
/// to re-read once it holds the installer mutex
/// (<see cref="IRemovableReverifier.RecheckUnderLease"/>). One entry per
/// claim, not per path, because a patch applied to several products is claimed
/// by each of them and any one of those verdicts can move on its own.
/// </param>
public record ReverifyResult(
    IReadOnlyList<string> Surviving,
    IReadOnlyList<string> Dropped,
    HeldBackReasons Reasons = default,
    IReadOnlyList<PatchClaim>? SurvivingPatchClaims = null,
    IReadOnlyList<PatchClaim>? SiblingPatchClaims = null)
{
    /// <summary>Never null: an absent list reads as nothing to re-read rather than as a fault.</summary>
    public IReadOnlyList<PatchClaim> SurvivingPatchClaims { get; init; }
        = SurvivingPatchClaims ?? Array.Empty<PatchClaim>();

    /// <summary>
    /// Every claim on any product a surviving path is registered to, for the
    /// under-lease re-read to apply the per-product condition rather than only the
    /// batch's own pairings. Includes the surviving claims themselves, a patch's own
    /// removability being part of that condition.
    ///
    /// Never null, on the same terms as the list above.
    /// </summary>
    public IReadOnlyList<PatchClaim> SiblingPatchClaims { get; init; }
        = SiblingPatchClaims ?? Array.Empty<PatchClaim>();
}
