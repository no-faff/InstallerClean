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
    ///
    /// It reads Windows Installer's in-progress signals too and hands the read back on
    /// <see cref="ReverifyResult.InstallerActivity"/>. Where that read is blocked the
    /// caller acts on nothing, whatever <see cref="ReverifyResult.Surviving"/> holds.
    /// </summary>
    /// <param name="progress">
    /// Told each wait the declared-product screen makes on a source folder, as the scan
    /// tells its own (<see cref="ScanProgressUpdate.IsWait"/>): a line naming what it is
    /// waiting for once a read has waited about a second, carrying the wait the caller can
    /// stop (<see cref="ScanProgressUpdate.Wait"/>), and an empty one when that wait ends.
    /// Nothing else is reported through it.
    /// </param>
    Task<ReverifyResult> ReverifyAsync(
        IReadOnlyList<string> candidatePaths,
        CancellationToken cancellationToken = default,
        IProgress<ScanProgressUpdate>? progress = null);

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
    /// WHAT IT RE-ASKS. It re-asks about the pairings the full re-verify's enumeration
    /// found holding each surviving patch, so it catches a verdict changing on one of
    /// them, which is the reverting superseded patch the full re-verify is for. A
    /// product that enumeration found holding nothing in the batch gives it nothing to
    /// re-ask about, and that enumeration, moments earlier, is what reads such a
    /// product. Do not move that enumeration inside the hold: it would hold the
    /// machine-wide installer lock for the length of an enumeration on every run.
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
/// Every claim naming a path still in the batch, and every pairing the enumeration found by
/// asking an installation by name that holds one of those paths' patches
/// (<see cref="InstallerQueryResult.PairingsHeldByName"/>). Empty short-circuits the
/// re-read without touching the API, which is the ordinary case: most batches are true
/// orphans, which carry no claim to re-read.
/// </param>
/// <param name="Siblings">
/// Every claim and pairing on any product one of those paths is registered to, including
/// the batch's own, a patch's own removability being part of the condition, with the
/// patches Windows listed for an installation found by asking that holds no claim
/// (<see cref="InstallerQueryResult.PairingsOfHoldersWithNoClaims"/>).
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
    /// Uninstallable read failed, or came back empty where the verdict turns on it, is
    /// non-removable too, and it names no claim at all:
    /// it is the row being there and nothing more. Such a row carries
    /// <see cref="Models.RegisteredPackage.VerdictUnreadable"/> and counts under
    /// <see cref="RecordsUnreadable"/>, so nothing reaches this cause on a read
    /// that did not answer.
    ///
    /// TWO ROUTES REACH IT and what is said of it has to hold for both. A patch the
    /// scan found superseded or obsoleted whose claim now says needed, back at Applied
    /// or still uninstallable and so needed to roll back with, or registered to a
    /// product on which another patch now answers that it can be uninstalled and so
    /// could roll back onto its file; and a candidate the scan found no claim on at
    /// all, which the re-enumeration finds claimed, by its path or by the file its
    /// path names. The second is the one the name flatters: nothing was reclaimed,
    /// because nothing this app saw ever held it, and whether the claim is new is not
    /// something either route can be told apart on (see this file's interface
    /// remarks). What is true of both, and the whole of what may be said, is that the
    /// records claim the file now.
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
    /// rather than a convenience: a patch's own State or Uninstallable read failing, or
    /// coming back empty where the verdict turns on it, during the re-verify's
    /// enumeration (<see cref="Models.RegisteredPackage.VerdictUnreadable"/>); a
    /// superseded patch whose removable verdict that enumeration took away because it
    /// could not establish something the offer needs, such as its products' patch sets,
    /// every installed product or every recorded path
    /// (<see cref="Models.RegisteredPackage.RemovableWithheld"/>); and a read under the
    /// installer lease failing, the same pairing's or the Uninstallable read of a patch
    /// on a product the batch's pairings name. The first reaches it too on an empty
    /// answer the verdict turns on, as the enumeration's does. The second reaches it too
    /// where it answers that the installation holds no record of the patch or that the
    /// product is not installed, or comes back empty. Under the lease the path's own
    /// pairings decide first: a path is counted here where none of them found a live
    /// claim or a registration that has gone and one did not answer, or where they all
    /// passed and the other patches on its products found no live claim and one did not
    /// answer. A walk-derived file whose identity matches a row of either of the first
    /// two kinds is counted here as well, the row deciding the cause. The merged count
    /// does not distinguish them.
    ///
    /// Anything else that reaches it is held to the same test against the code that
    /// builds the set, never against this list.
    /// </summary>
    RecordsUnreadable,

    /// <summary>
    /// The re-enumeration met a condition under which the scan itself offers no
    /// walk-derived file of this file's kind, so a file this batch carries is one the app
    /// would not put on a list now.
    ///
    /// IT IS ABOUT THE MACHINE AND NOT ABOUT THE FILE, which is what separates it
    /// from the three above. Those are findings about the registration that names
    /// this path: a live claim, a registration that has gone, a read that failed.
    /// This one rests on a fact about the machine and on nothing about the file,
    /// which is why it earns a count of its own even though no sentence names it.
    ///
    /// WHAT REACHES IT is <see cref="WithholdingLegs.Any"/>, the expression the scan's own
    /// withholding asks, put to the re-enumeration's census and to the registration side
    /// of the identity comparison this check re-runs, so a leg added there is acted on
    /// here without this file being edited; and the declared-product screen's
    /// <see cref="DeclaredProductOutcome.SecondCopyUnestablished"/>, under which the scan
    /// offers no installation package that verdict reaches. Several different findings
    /// reach it, which is one reason among several that the copy names no cause at all.
    ///
    /// IT DROPS THE WALK-DERIVED HALF OF A BATCH AND NOT THE WHOLE OF IT, as the scan
    /// does. A superseded registration is offered beside the walk-derived files and is
    /// judged by its own row. Where the re-enumeration could not settle a recorded path,
    /// that row comes back withheld and its path is held under
    /// <see cref="RecordsUnreadable"/>; a failed read of the records withholds the row too
    /// and is held the same way. The other legs do not touch the row, so dropping it here on
    /// them would keep back a file the scan would still offer on the same machine a moment
    /// later. A path no registration names is the walk-derived half,
    /// and that is the test used.
    /// </summary>
    OwnershipUnestablished,

    /// <summary>
    /// A check the scan makes on the file itself, made again just before acting, did
    /// not let the file through: the containment guard did not answer Safe, its own
    /// identity would not read or matches only registrations that are still
    /// removable, the declared-product screen kept it on any verdict but
    /// <see cref="DeclaredProductOutcome.SecondCopyUnestablished"/>, or the age check
    /// did not show it last changed before a moment Windows Installer had nothing in
    /// progress.
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
/// <param name="SourceRootsGivenUp">
/// Every drive or share, or path of another form, the declared-product screen stopped
/// reading in this check, with the route it was given up by and how many files the screen
/// kept at it (<see cref="DeclaredProductScreening.RootsGivenUp"/>). Each file it counts is in
/// <see cref="Dropped"/> and counted in <see cref="Reasons"/>, as
/// <see cref="HeldBackReason.FileNotConfirmed"/> or
/// <see cref="HeldBackReason.OwnershipUnestablished"/>. The check gives up its own drives
/// and shares, whatever the scan before it gave up.
/// </param>
/// <param name="SourceWaitCount">
/// How many waits the declared-product screen made in this check, each a read still waiting
/// after about a second, for which a host showing the waits put its waiting line up
/// (<see cref="DeclaredProductScreening.WaitCount"/>). Counted whether or not the check used
/// the screen's answer, and nought where the screen did not run.
/// </param>
/// <param name="InstallerActivity">
/// Windows Installer's in-progress signals as this check read them, just after its clock:
/// the same read, through the same service, as the pending-reboot gate each host makes before
/// the check. Null where the check reads no signals, and where it had no files to check.
///
/// A BLOCKED READ STOPS THE WHOLE BATCH, AND EACH HOST STOPS IT, as each stops on its gate:
/// the window paints the gate's banner and the command line ends as the gate ends it. A
/// caller acting on <see cref="Surviving"/> tests this member first, as both hosts do. The
/// check itself holds only the files no registration names (as
/// <see cref="HeldBackReason.FileNotConfirmed"/>), so a superseded patch in the same batch is
/// still in <see cref="Surviving"/> on a blocked read.
/// </param>
public record ReverifyResult(
    IReadOnlyList<string> Surviving,
    IReadOnlyList<string> Dropped,
    HeldBackReasons Reasons = default,
    IReadOnlyList<PatchClaim>? SurvivingPatchClaims = null,
    IReadOnlyList<PatchClaim>? SiblingPatchClaims = null,
    IReadOnlyList<SourceRootGivenUp>? SourceRootsGivenUp = null,
    int SourceWaitCount = 0,
    PendingRebootResult? InstallerActivity = null)
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

    /// <summary>Never null: an absent list reads as a check that gave nothing up.</summary>
    public IReadOnlyList<SourceRootGivenUp> SourceRootsGivenUp { get; init; }
        = SourceRootsGivenUp ?? Array.Empty<SourceRootGivenUp>();

    /// <summary>
    /// The drives and shares in <see cref="SourceRootsGivenUp"/> at which the screen kept at
    /// least one file, in the order it gave them up, as
    /// <see cref="ScanResult.SourceRootsGivenUpKeepingFiles"/> is for a scan: a root given up
    /// with no file kept at it changed nothing the check decided.
    /// </summary>
    public IReadOnlyList<SourceRootGivenUp> SourceRootsGivenUpKeepingFiles =>
        SourceRootGivenUp.KeepingFiles(SourceRootsGivenUp);

    /// <summary>
    /// Folds the paths an action service held back under the installer mutex into
    /// the pre-act re-verify's own account of what it kept back, so the user is
    /// shown one account of one batch.
    ///
    /// Which side of the mutex a file was condemned on is a fact about how the
    /// check is built rather than about what happened to the file, so the two
    /// producers earn no line of their own. The tallies are ADDED rather than
    /// merged, because the two hold back DIFFERENT files: the one number the user
    /// reads is every file the run held back, and anything that merged rather than
    /// added would under-count it. The count per cause under that number is added
    /// for the same reason, and the opt-in result log is what reads it.
    ///
    /// The SURVIVING claims move with the paths, because those three collections
    /// describe one set of files and have to agree: a path leaving
    /// <c>Surviving</c> for <c>Dropped</c> takes every claim naming it, a patch
    /// held by several products carrying one per product. A claim left behind
    /// would make <c>SurvivingPatchClaims</c> name a file the same result has just
    /// declared held back. <see cref="PatchClaim.LocalPackagePath"/> is normalised
    /// the way the registered rows are, so the one case-insensitive set answers
    /// for both.
    ///
    /// The SIBLING claims do not, and the body says why. They are the other half
    /// of what the under-lease re-read is asked, they are keyed by product rather
    /// than by path, and this is the one place in the app that builds a
    /// <see cref="ReverifyResult"/> out of another one.
    /// </summary>
    public ReverifyResult WithHeldBack(IReadOnlyList<string> heldBack, HeldBackReasons reasons)
    {
        if (heldBack.Count == 0) return this;

        var condemned = new HashSet<string>(heldBack, StringComparer.OrdinalIgnoreCase);
        // A WITH-EXPRESSION RATHER THAN A REBUILD, and that is the whole of why
        // SiblingPatchClaims is still here. A positional form naming only some of the
        // record's members leaves the rest at their defaults, the sibling list among
        // them, which is a batch with no siblings: the exact pairing UnderLeaseClaims
        // exists to make unconstructible. This form carries whatever the author did
        // not name, so a member added to ReverifyResult later arrives here without
        // anybody having to remember it.
        //
        // AND THE SIBLING LIST PASSES THROUGH UNFILTERED, WHICH IS NOT AN OVERSIGHT
        // AND IS THE THING TO GET WRONG HERE. The surviving claims are keyed by
        // PATH, so they leave with their paths, which is what the filter below
        // does. The siblings are keyed by PRODUCT: the re-read walks them by
        // product code and condemns every batch path on a product one of them
        // answers badly for. Filtering them by condemned path would throw away the
        // row that answers for a product a surviving path is still registered to,
        // and so narrow the very check this is restoring.
        //
        // What unfiltered costs, weighed and accepted rather than unnoticed: a
        // folded result carries siblings for products no surviving path is left on,
        // so a re-read handed one would make keyed reads that can condemn nothing,
        // with the machine-wide installer lease held. It is bounded by the batch's
        // own products, the reads are keyed rather than an enumeration, and a
        // narrower list would have to be built by product and never by path. That
        // saving is not worth a second way of getting this wrong.
        return this with
        {
            Surviving = Surviving.Where(p => !condemned.Contains(p)).ToList().AsReadOnly(),
            Dropped = Dropped.Concat(heldBack).ToList().AsReadOnly(),
            Reasons = Reasons + reasons,
            SurvivingPatchClaims = SurvivingPatchClaims
                .Where(c => !condemned.Contains(c.LocalPackagePath)).ToList().AsReadOnly(),
        };
    }
}
