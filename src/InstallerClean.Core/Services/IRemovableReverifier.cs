using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Re-checks a set of removal candidates against the Windows Installer API
/// immediately before a Move or Delete acts on them, to catch the one window
/// neither the fresh pending-reboot gate nor the <c>Global\_MSIExecute</c> hold
/// can see: a patch
/// whose state changed AND settled between the scan and the click (a superseded
/// patch reverted to Applied because its superseding patch was uninstalled).
///
/// It re-runs the full classifier (<see cref="IInstallerQueryService"/>) rather
/// than re-querying a single retained product code, because after the
/// shared-patch verdict merge a patch can revert to Applied for a DIFFERENT
/// product than the one whose code survived the merge; re-enumerating is correct
/// across every product for nothing but a few seconds spent before a rare,
/// destructive batch.
///
/// A true orphan can be dropped here too, and that is the second reason the
/// enumeration is full rather than per candidate. A file the API never claimed is
/// not a file it can never claim: an install that wrote its package into the cache
/// before the folder walk reached it, and registered that package after the query
/// had already passed, leaves a file that is an orphan by every measurement the
/// scan made and is claimed by the time the user clicks. Only re-walking the whole
/// registered set finds it, and finding it is the last thing between that file and
/// a permanent delete.
///
/// WHAT THAT DOES NOT ESTABLISH, and no copy built on this may assume: that the
/// claim is new. An install completing inside the session and the scan's own
/// reading having missed a claim that was there all along produce the identical
/// observation, a candidate no query claimed and a re-verify that finds one
/// claiming it, and nothing here can separate them. The second is the very failure
/// this app is being hardened against, so a sentence asserting the first would have
/// the app quietly ruling it out on the one screen where it had just fired. All
/// that is shown is the present state of the records.
/// </summary>
public interface IRemovableReverifier
{
    /// <summary>
    /// Re-enumerates the registered set and splits <paramref name="candidatePaths"/>
    /// into those still safe to remove and those a currently-registered,
    /// non-removable package now claims (which must be dropped from the batch and
    /// reported as skipped). An empty input short-circuits without querying.
    /// Propagates any exception the enumeration raises (an inability to
    /// re-verify must stop the batch, not silently pass it).
    /// </summary>
    Task<ReverifyResult> ReverifyAsync(
        IReadOnlyList<string> candidatePaths,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-reads the given patch claims and returns the paths the re-read has not
    /// left shown to be removable, for the action services to call ONCE THEY HOLD
    /// <c>Global\_MSIExecute</c> and before they touch a file. Not "no longer
    /// removable": one of the three causes is a read that established nothing
    /// either way, and it keeps the file for want of a verdict rather than on one.
    ///
    /// It exists because <see cref="ReverifyAsync"/> cannot be the last word. The
    /// hold is taken inside the action service, so every caller runs the full
    /// re-verify before it, and the window the batch acts across is that
    /// enumeration's whole duration rather than the instant after it. Windows
    /// writes a patch's registration during the execute sequence, and
    /// <c>_MSIExecute</c> is documented as set only while the execute-sequence
    /// tables are being processed, so the write falls inside the phase the mutex
    /// covers.
    ///
    /// What is NOT established, and must not be written here as though it were:
    /// whether an info API can return a registration its own transaction has
    /// written and not yet committed. That answer decides how WIDE the window
    /// this closes really was; it does not decide whether the re-read is worth
    /// taking, which is why this was built without it.
    ///
    /// Synchronous, and that is a requirement rather than a convenience: the
    /// lease must be released by the thread that took it, so the whole hold is one
    /// unbroken synchronous body with no await in it to hop threads.
    ///
    /// WHAT IT COVERS, stated narrowly because the difference matters. It re-asks
    /// about claims that already existed, so it catches a verdict changing on one
    /// of them, which is the reverting superseded patch the full re-verify is for.
    /// It cannot see a claim from a product that held none when the claims were
    /// collected: there is nothing to re-ask about, and only re-walking the whole
    /// registered set would find it. That case is covered up to the full
    /// re-verify's own enumeration and no further. Closing it as well would mean
    /// running that enumeration inside a machine-wide installer lock on every run,
    /// which is a worse trade than the sliver it buys.
    /// </summary>
    /// <param name="claims">
    /// The batch's own pairings and the sibling pairings on the products they name, as
    /// one argument. See <see cref="UnderLeaseClaims"/> for why it is one and not two.
    ///
    /// MEASURED COST, because the ruling that asked for the sibling half forbade
    /// adopting the narrower question without one. The added reads are the patch
    /// registrations of the products the batch touches, so they are bounded by the
    /// batch's own products and never by an enumeration.
    ///
    /// THE FIGURE CARRIES THE DATE IT WAS READ, because it is a fact about one
    /// machine at one moment and not a property of this code. Read out of the
    /// per-product Patches keys under UserData on 2026-08-18, the machine every other
    /// figure here came from held THREE patch registrations across two products, one
    /// product holding two of them and one of those superseded with its Uninstallable
    /// read as zero. So a batch touching that product adds two keyed reads there. This
    /// paragraph previously gave an undated two, one per product and none superseded,
    /// from an earlier reading of the same machine, and nothing in this code changed
    /// between the two: an update landed and the sentence went stale where it stood.
    /// The largest single-product figure captured anywhere in this project is 58, from
    /// Office 2010 SP2. Against any of those, this method already makes two keyed
    /// reads per claim in the batch, and the pre-lease pass runs a whole enumeration
    /// moments earlier outside the lease.
    /// </param>
    UnderLeaseRecheck RecheckUnderLease(UnderLeaseClaims claims);
}

/// <summary>
/// The two claim lists the under-lease re-read needs, carried as ONE argument.
///
/// THAT IS THE WHOLE POINT OF THE TYPE AND IT REPLACED A GUARD. The re-read needs the
/// batch's own pairings and the sibling pairings on the products those name, because
/// the offer rests on a fact about other patches. As two arguments, a caller could
/// supply the first and forget the second and silently receive a weaker check, so this
/// first shipped with a consistency guard that detected the mismatch and refused the
/// batch. The guard was the wrong answer: it could only ever fire on a programming
/// error, and refusing a batch puts a sentence on somebody's screen, so it would have
/// had to name a cause about their machine that had not occurred.
///
/// One argument removes the mistake instead of detecting it, which is the rule this
/// project states as handling beating guarding: establish that a limit cannot simply be
/// removed before designing around it. <see cref="From"/> is how production builds one,
/// out of the pre-lease pass's own result, so the two halves cannot come from different
/// places.
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
/// Why one file was held back. Four states, because they are four different things
/// to have found out: a confirmed positive, an inability, neither, and one that is
/// not about the file at all.
///
/// NOTHING THE USER READS NAMES ANY OF THEM, WHICH IS A CHANGE FROM 3.0.0 AND IS
/// WHY THIS COMMENT NO LONGER ARGUES FOR A PARTITION. The screen and stdout carry
/// one counted sentence naming no cause, on the ground that every file on it was
/// offered by the scan and not confirmed by the check made immediately before
/// acting, which is true of all four by construction. These four survive as
/// COUNTS: they travel in the opt-in result log and are the only place a machine's
/// causes can still be told apart.
///
/// THE FIRST THREE ARE ABOUT THE REGISTRATION THAT NAMES THIS PATH. The fourth is
/// about the machine and is reached without reading anything about the path, which
/// is why it could not fold into any of them while each had a sentence, and why it
/// is still worth counting separately now that none has.
/// </summary>
public enum HeldBackReason
{
    /// <summary>
    /// The records were read and a registered product's live claim names the file,
    /// where the scan's own reading left it removable.
    ///
    /// BOTH LIMBS OF THAT SENTENCE ARE LOAD-BEARING and the first is the one that
    /// was once missing. A patch row whose State or Uninstallable read failed is
    /// non-removable too, and it names no claim at all: it is the row being there
    /// and nothing more. Such a row carries
    /// <see cref="Models.RegisteredPackage.VerdictUnreadable"/> and counts under
    /// <see cref="RecordsUnreadable"/>, so nothing reaches this cause on a read
    /// that did not answer.
    ///
    /// TWO ROUTES REACH IT and the copy has to hold for both. A patch the scan
    /// found superseded or obsoleted whose claim now says needed, back at Applied
    /// or still uninstallable and so needed to roll back with; and a candidate the
    /// scan found no claim on at all, which the re-enumeration finds claimed. The
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
    /// of the answer suggests and is the measurement this state exists for. The
    /// absence code means "no such product in the ACCOUNT AND CONTEXT you asked
    /// in", not "no such product", and the context a claim carries was settled when
    /// the scan collected it. A pairing that moved context between the scan and the
    /// click therefore answers absent while its registration is live, so releasing
    /// on that answer would put a needed file into a permanent delete on the
    /// strength of a question asked the wrong way round.
    /// </summary>
    RecordsChanged,

    /// <summary>
    /// A read failed, so nothing was established either way. It has not shown the
    /// file to be removable, which is what keeps it in place.
    ///
    /// TWO MECHANISMS REACH IT and the sentence is a superordinate over both
    /// rather than a convenience: a patch's own State or Uninstallable read
    /// failing during the re-verify's enumeration
    /// (<see cref="Models.RegisteredPackage.VerdictUnreadable"/>), and the same
    /// pairing's read failing under the installer lease. Both are a keyed property
    /// read of the Windows Installer records that did not answer, which is what the
    /// sentence says, and the merged count does not distinguish them.
    ///
    /// IT REACHED FOUR UNTIL 3.0.0. The other two were the identity re-check's
    /// unaskable state, an account list that would not read and a keyed read
    /// answering outside its documented set, and they folded in here rather than
    /// taking a cause of their own. They went with the check. Anything added later
    /// is held to the same test against the code that builds the set, never against
    /// this list.
    /// </summary>
    RecordsUnreadable,

    /// <summary>
    /// The re-enumeration met a condition under which the scan itself offers no
    /// walk-derived file at all, so a file this batch carries is one the app would
    /// no longer put on a list.
    ///
    /// IT IS ABOUT THE MACHINE AND NOT ABOUT THE FILE, which is what separates it
    /// from the three above and is why it could not fold into any of them. Those
    /// are findings about the registration that names this path: a live claim, a
    /// registration that has gone, a read that failed. This one is reached without
    /// looking at the path at all, which is why it earns a count of its own even
    /// though no sentence names it.
    ///
    /// WHAT REACHES IT is asked of the census where the members live
    /// (<see cref="Models.EnumerationCensus.AnyRecordedPathUnestablished"/> and
    /// <see cref="Models.EnumerationCensus.SecondInstanceNotRuledOut"/>), on the
    /// same rule as the scan's own withholding: a condition added to either is
    /// acted on here without this file being edited. Two different findings reach
    /// it today, which is one reason among several that the copy names no cause at
    /// all.
    ///
    /// IT DROPS THE WALK-DERIVED HALF OF A BATCH AND NOT THE WHOLE OF IT, which
    /// matches what the scan does rather than what the removed version did. The
    /// version taken out in 3.0.0 refused the entire batch, and that was right when
    /// the whole offer was walk-derived; a superseded registration is offered beside
    /// it now, and those rows are judged by product code and are not touched by
    /// either condition, so refusing them here would keep back files the scan would
    /// still offer on the same machine a moment later. A path no registration names
    /// is the walk-derived half, and that is the test used.
    /// </summary>
    OwnershipUnestablished,
}

/// <summary>
/// How many files were held back for each cause. Counts rather than one cause for
/// the set, because a batch can meet more than one.
///
/// THEY ARE INSTRUMENTATION NOW RATHER THAN COPY. The report reads
/// <see cref="Total"/> and nothing else, one sentence naming no cause; these four
/// travel in the opt-in result log, which is the only place the causes can still be
/// told apart on a real machine. This note used to justify them by a sentence being
/// false of four files in five, which was the argument for the partition that
/// replaced them.
///
/// The paths themselves are carried alongside by whichever result holds this. Every
/// producer increments at the point it adds the path, so the two cannot come apart,
/// and
/// <see cref="Total"/> is what a test holds them to.
/// </summary>
public readonly record struct HeldBackReasons(
    int Reclaimed = 0,
    int RecordsChanged = 0,
    int RecordsUnreadable = 0,
    int OwnershipUnestablished = 0)
{
    /// <summary>
    /// Files held back for any cause, and the ONLY member the report reads: the
    /// user's sentence counts this and names nothing. Equals the accompanying path
    /// list's count.
    /// </summary>
    public int Total =>
        Reclaimed + RecordsChanged + RecordsUnreadable + OwnershipUnestablished;

    /// <summary>
    /// This tally with one more file counted against <paramref name="reason"/>.
    ///
    /// EVERY MEMBER IS NAMED AND THE DEFAULT THROWS, which is a change and is the
    /// point of it. The default arm used to be <see cref="HeldBackReason.RecordsUnreadable"/>,
    /// so a cause added to the enum and forgotten here compiled, built green and was
    /// counted and reported as a read that failed: a file held back under a sentence
    /// naming a cause that did not occur, with nothing anywhere to see. This project
    /// has shipped that shape once already, in a rename that would have made every
    /// delete-failure report unreadable. A member added to the enum now fails at the
    /// first file that reaches it, loudly, rather than being absorbed.
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
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason,
            "A held-back cause with no counter. Add it to HeldBackReasons in the "
            + "same edit as the enum member. The report needs nothing: it counts "
            + "Total and names no cause."),
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
            a.OwnershipUnestablished + b.OwnershipUnestablished);
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
/// the input: <see cref="Surviving"/> is still safe to act on, <see cref="Dropped"/>
/// is now claimed by a non-removable registered package and must be skipped.
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
