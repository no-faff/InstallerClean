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
    ///
    /// <see cref="IBatchCheck"/> IS WHAT CALLS IT FOR A MOVE OR A DELETE, with a watch on
    /// Windows Installer's records armed first, and nothing else acts on its result: the
    /// result alone says what the records held while they were read, and nothing about
    /// whether they have been written since.
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
}

/// <summary>
/// Why one file was held back. Six, because they are six different things to have
/// found out: a confirmed positive, an inability, neither, one about the machine
/// rather than the file, one about the file itself, and one about a program that does
/// not hold the file's patch.
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
/// file, read the way the scan read it. The sixth is about a program no registration
/// of the file names. Each is counted separately for that reason.
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
    /// path names. For the second, nothing was reclaimed, because nothing this app saw
    /// ever held it, and whether the claim is new is not
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
    /// THESE MECHANISMS REACH IT and the sentence is a superordinate over all of them
    /// rather than a convenience: a patch's own State or Uninstallable read failing, or
    /// coming back empty where the verdict turns on it, during the re-verify's
    /// enumeration (<see cref="Models.RegisteredPackage.VerdictUnreadable"/>); and a
    /// superseded patch whose removable verdict that enumeration took away because it
    /// could not establish something the offer needs, such as its products' patch sets,
    /// every installed product or every recorded path
    /// (<see cref="Models.RegisteredPackage.RemovableWithheld"/>). A walk-derived file
    /// whose identity matches a row of either kind is counted here as well, the row
    /// deciding the cause. The merged count does not distinguish them.
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
    /// could not ask about a product the registry names, or failed a read of the records,
    /// that row comes back withheld and its path is held under
    /// <see cref="RecordsUnreadable"/>. The file-identity leg does not touch the row, so
    /// dropping it here on that leg would keep back a file the scan would still offer on the
    /// same machine a moment later. A path no registration names is the walk-derived half,
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

    /// <summary>
    /// The file is a superseded patch's, and a program the patch is for, which does not
    /// hold it, keeps it: the row the re-enumeration gave the file carries
    /// <see cref="Models.RegisteredPackage.KeptForLackingProgram"/>, whichever of its
    /// answers that is. The scan listed the file, so the program was installed or changed
    /// after the scan, or answered the scan differently.
    ///
    /// IT IS ABOUT A PROGRAM THAT HOLDS NO REGISTRATION OF THE FILE, which is what
    /// separates it from <see cref="Reclaimed"/>: nothing claims the file, and the patch
    /// could still be applied to that program, by an installation that opens it.
    /// </summary>
    LackingProgram,
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
    int FileNotConfirmed = 0,
    int LackingProgram = 0)
{
    /// <summary>
    /// Files held back for any cause, and the ONLY member the report reads: the
    /// user's sentence counts this and names nothing. Equals the accompanying path
    /// list's count.
    /// </summary>
    public int Total =>
        Reclaimed + RecordsChanged + RecordsUnreadable + OwnershipUnestablished + FileNotConfirmed
        + LackingProgram;

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
        HeldBackReason.LackingProgram => this with { LackingProgram = LackingProgram + 1 },
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason,
            "A held-back cause with no counter. Add it to HeldBackReasons, and a "
            + "field for it to the opt-in report's operation block, in the same edit "
            + "as the enum member."),
    };

    /// <summary>
    /// Merges two tallies, for the fold that joins what the check before a batch held
    /// back to what the checks made in the middle of it did. Addition rather than an OR
    /// of flags: the checks hold back DIFFERENT files, so their counts accumulate
    /// instead of one standing in for both.
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
            a.FileNotConfirmed + b.FileNotConfirmed,
            a.LackingProgram + b.LackingProgram);
}

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
/// Windows Installer's in-progress signals as this check read them, just after its clock.
/// Null where the check reads no signals, and where it had no files to check.
///
/// A BLOCKED READ MEANS THE CHECK DOES NOT STAND, and <see cref="IBatchCheck"/>, which is
/// what runs this check for both hosts and both services, throws the whole result away on
/// one. The check itself holds only the files no registration names on such a read (as
/// <see cref="HeldBackReason.FileNotConfirmed"/>), so a superseded patch in the same batch
/// is still in <see cref="Surviving"/>; a caller that acted on <see cref="Surviving"/>
/// without testing this member would act on it.
/// </param>
public record ReverifyResult(
    IReadOnlyList<string> Surviving,
    IReadOnlyList<string> Dropped,
    HeldBackReasons Reasons = default,
    IReadOnlyList<SourceRootGivenUp>? SourceRootsGivenUp = null,
    int SourceWaitCount = 0,
    PendingRebootResult? InstallerActivity = null)
{
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
    /// Folds what the checks made in the middle of a batch came to into the first check's
    /// own account, so the user is shown one account of one batch: the files those checks
    /// dropped leave <see cref="Surviving"/> for <see cref="Dropped"/>, and their causes,
    /// the drives and shares they gave up and the waits they made are added to this
    /// check's.
    ///
    /// Which check a file was dropped by is a fact about how the batch ran and not about
    /// the file, so the later checks earn no line of their own. The tallies are ADDED,
    /// because the checks drop DIFFERENT files: a file a later check drops was confirmed
    /// by every check before it. The one number the user reads is every file the run held
    /// back, and the count per cause under it is what the opt-in result log reads.
    /// </summary>
    public ReverifyResult WithLaterChecks(
        IReadOnlyList<string> heldBack, HeldBackReasons reasons,
        IReadOnlyList<SourceRootGivenUp>? sourceRootsGivenUp = null, int sourceWaitCount = 0)
    {
        var roots = sourceRootsGivenUp ?? Array.Empty<SourceRootGivenUp>();
        if (heldBack.Count == 0 && roots.Count == 0 && sourceWaitCount == 0) return this;

        var condemned = new HashSet<string>(heldBack, StringComparer.OrdinalIgnoreCase);
        // A with-expression, so a member added to ReverifyResult later is carried
        // through without being named here.
        return this with
        {
            Surviving = Surviving.Where(p => !condemned.Contains(p)).ToList().AsReadOnly(),
            Dropped = Dropped.Concat(heldBack).ToList().AsReadOnly(),
            Reasons = Reasons + reasons,
            SourceRootsGivenUp = SourceRootsGivenUp.Concat(roots).ToList().AsReadOnly(),
            SourceWaitCount = SourceWaitCount + sourceWaitCount,
        };
    }
}
