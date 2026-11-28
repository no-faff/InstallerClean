using InstallerClean.Helpers;
using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IRemovableReverifier"/>: re-runs
/// <see cref="IInstallerQueryService.GetRegisteredPackagesAsync"/>, drops any
/// candidate whose path a currently-registered, non-removable package claims, and
/// judges every candidate no registration names the way the scan judged it.
///
/// The full re-enumeration is the cost of the answer for the question this asks.
/// Most candidates are orphans, and what this re-establishes about an orphan is
/// the ABSENCE of any claim ON ITS PATH: there is no registration to re-read,
/// because the reason the file is a candidate is that no registration names it,
/// and only walking the whole registered set again can re-establish an absence.
/// A per-candidate re-read of the same question would answer nothing at all for
/// every orphan while still reporting itself as a re-verification.
///
/// AN ORPHAN IS THEN PUT TO EVERY OTHER STEP THE SCAN DECIDED IT BY, in the scan's
/// order: the containment guard, the file-identity comparison
/// (<see cref="RegistrationIdentityMatch"/>), the withholding legs
/// (<see cref="WithholdingLegs"/>), the declared-product screen
/// (<see cref="IDeclaredProductCheck"/>) and the age check
/// (<see cref="CachedFileAge"/>). Each step can only keep a file back.
/// </summary>
public sealed class RemovableReverifier : IRemovableReverifier
{
    private readonly IInstallerQueryService _queryService;
    private readonly IFileIdentityReader? _fileIds;
    private readonly IDeclaredProductCheck? _declaredProducts;
    private readonly IFileTimesReader? _fileTimes;
    private readonly IPendingRebootService? _installerActivity;
    private readonly TimeProvider _clock;
    private readonly string? _installerFolderOverride;

    /// <summary>
    /// Production constructor. The three readers, the clock and the in-progress signals
    /// are the scan's own, for the steps this pass re-runs on a file no registration
    /// names. The signals' read is also handed back on the result, where a blocked one
    /// means the check does not stand (<see cref="ReverifyResult.InstallerActivity"/>).
    /// </summary>
    public RemovableReverifier(IInstallerQueryService queryService,
        IFileIdentityReader fileIdentities, IDeclaredProductCheck declaredProducts,
        IFileTimesReader fileTimes, TimeProvider clock, IPendingRebootService installerActivity)
        : this(queryService, fileIdentities, declaredProducts, fileTimes, clock, null, installerActivity) { }

    /// <summary>
    /// Test constructor for the tests whose subject is the registrations: no file
    /// is opened, so a file no registration names is judged on the enumeration's
    /// own two legs alone. A pass built this way can only keep back less than one
    /// built with its readers, never more.
    /// </summary>
    internal RemovableReverifier(IInstallerQueryService queryService)
        : this(queryService, null, null, null, null, null, null) { }

    /// <summary>
    /// Test constructor for the steps that open a file.
    /// </summary>
    /// <param name="fileIdentities">
    /// Null runs no identity comparison. With any of the three readers present, the
    /// containment guard runs first on every file no registration names, against the
    /// real filesystem, as the scan's does.
    /// </param>
    /// <param name="declaredProducts">Null runs no screen.</param>
    /// <param name="fileTimes">Null runs no age check.</param>
    /// <param name="clock">The clock the age check judges against. Null means the system clock.</param>
    /// <param name="installerFolderOverride">
    /// A real folder standing in for <c>C:\Windows\Installer</c>, as the scan's test
    /// constructors take one. The guard still asks the real filesystem.
    /// </param>
    /// <param name="installerActivity">
    /// What this pass reads Windows Installer's in-progress signals through, just after its
    /// clock. A blocked read holds every file no registration names at the age check, and
    /// the read goes back on the result as <see cref="ReverifyResult.InstallerActivity"/>,
    /// which is what <see cref="IBatchCheck"/> throws the check away on. Null reads
    /// nothing: the age check then judges every file by its times alone, and the result
    /// carries no read.
    /// </param>
    internal RemovableReverifier(IInstallerQueryService queryService,
        IFileIdentityReader? fileIdentities, IDeclaredProductCheck? declaredProducts,
        IFileTimesReader? fileTimes, TimeProvider? clock, string? installerFolderOverride,
        IPendingRebootService? installerActivity = null)
    {
        _queryService = queryService;
        _fileIds = fileIdentities;
        _declaredProducts = declaredProducts;
        _fileTimes = fileTimes;
        _installerActivity = installerActivity;
        _clock = clock ?? TimeProvider.System;
        _installerFolderOverride = installerFolderOverride;
    }

    /// <summary>
    /// Whether this pass opens the files no registration names, for the test that
    /// holds the hosts' pass to doing so. Both test constructors can leave it off.
    /// </summary>
    internal bool ChecksFiles =>
        _fileIds is not null && _declaredProducts is not null && _fileTimes is not null;

    /// <summary>
    /// Whether this pass reads Windows Installer's in-progress signals after its clock, for
    /// the test that holds the hosts' pass to reading them: without the read, neither the
    /// age check's hold nor the rule that a check made while a signal is up does not stand
    /// has anything to act on. Both test constructors can leave it off.
    /// </summary>
    internal bool ReadsInstallerActivity => _installerActivity is not null;

    /// <summary>
    /// Whether this pass judges file times against <see cref="FileStampClock"/>, as the
    /// scan does, for the test that holds the hosts' pass to it.
    /// </summary>
    internal bool JudgesOnTheFileStampClock => _clock is FileStampClock;

    public async Task<ReverifyResult> ReverifyAsync(
        IReadOnlyList<string> candidatePaths,
        CancellationToken cancellationToken = default,
        IProgress<ScanProgressUpdate>? progress = null)
    {
        if (candidatePaths.Count == 0)
            return new ReverifyResult(candidatePaths, Array.Empty<string>());

        // The age check's clock, read once and before anything else is read, as the
        // scan reads its own: a file created or changed while this pass runs is later
        // than it. The in-progress signals follow it, for the reason the scan reads its
        // own in that order (FileSystemScanService.ScanAsync), and off the calling thread
        // as the scan reads them, the window awaiting this from its dispatcher. The
        // wall-clock reading taken just before the clock is what the age check measures the
        // wall clock's own advance from, and comes first, as the scan's does. The signals'
        // read goes back on the result too, where a blocked one means the check does not
        // stand (ReverifyResult.InstallerActivity).
        var wallAtStart = CachedFileAge.ReadWallClock(_clock);
        var clock = _clock.GetUtcNow();
        var installerActivity = _installerActivity is null
            ? null
            : await Task.Run(_installerActivity.Check, cancellationToken).ConfigureAwait(false);

        // ConfigureAwait(false): Core has no thread affinity; the caller runs this
        // off the dispatcher (behind the operating overlay), exactly as the scan does.
        var query = await _queryService.GetRegisteredPackagesAsync(null, cancellationToken)
            .ConfigureAwait(false);

        // Every path a currently NON-removable registered package claims, against
        // the cause its own row supports. A reverted patch (Superseded -> Applied)
        // appears here as a live claim; a still-superseded patch is IsRemovable and
        // does not appear at all; a true orphan was never registered.
        //
        // NOT EVERY ROW HERE CARRIES A CLAIM, and telling them apart is the whole
        // of what this map is for. A patch whose State or Uninstallable read failed,
        // or came back empty where the verdict turns on it
        // (InstallerQueryService.LeavesVerdictUnestablished), lands here having
        // established nothing either way: non-removable for want
        // of a verdict rather than on one, so its file is held as records that could
        // not be read, not as a program reclaiming it. The withheld kind is a third,
        // held the same way: a superseded patch whose removable verdict this run took
        // away because it could not establish something the offer needs, not because
        // it found a claim. Every kind can be in one batch, which is why the cause is
        // carried per path and not per run.
        //
        // A STILL-REMOVABLE SUPERSEDED PATCH IS DELIBERATELY NOT IN THIS MAP, and that
        // is the one entry whose absence is the point. The map is what condemns a
        // candidate, so a row that is still removable must stay out of it or the offer
        // would be emptied by the pass that exists to re-check it. A superseded patch
        // whose verdict has MOVED since the scan is non-removable now and is therefore
        // in the map, dropped with a cause, which is exactly the reverting-patch case
        // this whole pass was built for.
        //
        // THE ROW DECIDES THE CAUSE, NOT THE CANDIDATE. A candidate the scan found
        // to be an orphan, whose path a patch row names here with its verdict
        // unread, is reported as records that could not be read, although "a
        // registration names it now" is true of it too. Deciding the cause from
        // what the SCAN saw would mean trusting the reading this pass exists to
        // distrust.
        //
        // A dictionary rather than a set because InstallerQueryResult.Packages is
        // one row per claimed path, so there is a single answer to record for each.
        var nonRemovable = new Dictionary<string, HeldBackReason>(StringComparer.OrdinalIgnoreCase);
        foreach (var pkg in query.Packages)
            if (!pkg.IsRemovable)
                nonRemovable[pkg.LocalPackagePath] = CauseOfNonRemovable(pkg);

        // Every path any registration names, removable or not, which is the test for
        // which half of the batch a candidate came from. A superseded registration
        // reaches the offer FROM this set and is judged by product code and patch
        // code; a walk-derived candidate is one no registration names at all, and it
        // is the half the steps below judge, as the scan judged it. Judging the whole
        // batch that way instead would keep back files the same scan would still offer
        // a moment later.
        var claimedPaths = new HashSet<string>(
            query.Packages.Select(p => p.LocalPackagePath), StringComparer.OrdinalIgnoreCase);

        // The path's own finding first where there is one, because it is the stronger
        // thing to have found out: a live claim on this file says more than anything
        // the steps below find. Nothing the user reads names either, so what the
        // order decides is which counter the file lands in, and the counters are what
        // the opt-in report carries.
        var held = new Dictionary<string, HeldBackReason>(StringComparer.OrdinalIgnoreCase);
        var walkDerived = new List<string>();
        var walkSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in candidatePaths)
        {
            if (nonRemovable.TryGetValue(path, out var reason)) held.TryAdd(path, reason);
            else if (!claimedPaths.Contains(path) && walkSeen.Add(path)) walkDerived.Add(path);
        }

        var (sourceRootsGivenUp, sourceWaitCount) =
            HoldWalkDerivedFilesTheScanWouldHold(
                walkDerived, query, clock, wallAtStart, installerActivity, held, cancellationToken, progress);

        // In the order the batch was handed over, so the two lists read as the batch
        // does.
        var surviving = new List<string>(candidatePaths.Count);
        var dropped = new List<string>();
        var reasons = default(HeldBackReasons);
        foreach (var path in candidatePaths)
        {
            if (held.TryGetValue(path, out var reason))
            {
                dropped.Add(path);
                reasons = reasons.Plus(reason);
            }
            else
            {
                surviving.Add(path);
            }
        }

        return new ReverifyResult(surviving.AsReadOnly(), dropped.AsReadOnly(), reasons,
            sourceRootsGivenUp, sourceWaitCount, installerActivity);
    }

    /// <summary>
    /// The cause a non-removable row supports: a program that does not hold the patch
    /// for a row kept for one, whichever answer keeps it; records that were not read to
    /// a verdict for a row whose verdict was unread or withheld; a live claim for any
    /// other.
    /// </summary>
    private static HeldBackReason CauseOfNonRemovable(RegisteredPackage pkg) =>
        pkg.KeptForLackingProgram != LackingProgramKeep.None ? HeldBackReason.LackingProgram
        : pkg.RemovableWithheld || pkg.VerdictUnreadable ? HeldBackReason.RecordsUnreadable
        : HeldBackReason.Reclaimed;

    /// <summary>
    /// Adds to <paramref name="held"/> every file in <paramref name="walkDerived"/>
    /// the scan would keep back from its offer now, with the cause it is kept under.
    /// The steps are the scan's, in the scan's order, and each one is handed only
    /// what the steps before it let through.
    ///
    /// THE CONTAINMENT GUARD COMES FIRST, because every step after it opens the file
    /// and a path the guard does not answer Safe for is not opened. Such a path is
    /// held rather than left for the action services' own guard, which runs later
    /// against a root of its own and could answer differently.
    ///
    /// WHERE A LEG FIRES, EVERY FILE STILL STANDING IS HELD, as in the scan: each is
    /// already kept on a fact about the machine, and the last two steps have nothing left
    /// to run on.
    ///
    /// It returns every drive or share the declared-product screen gave up and how many waits
    /// it made, and none of either where the screen did not run.
    /// </summary>
    private (IReadOnlyList<SourceRootGivenUp> RootsGivenUp, int WaitCount) HoldWalkDerivedFilesTheScanWouldHold(
        List<string> walkDerived,
        InstallerQueryResult query,
        DateTimeOffset clock,
        WallClockReading wallAtStart,
        PendingRebootResult? installerActivity,
        Dictionary<string, HeldBackReason> held,
        CancellationToken cancellationToken,
        IProgress<ScanProgressUpdate>? progress)
    {
        IReadOnlyList<SourceRootGivenUp> givenUp = [];
        var waitCount = 0;
        if (walkDerived.Count == 0) return (givenUp, waitCount);

        var standing = walkDerived;
        var opensFiles = _fileIds is not null || _declaredProducts is not null || _fileTimes is not null;

        // Resolved once for the pass, as the scan resolves it once for the run.
        var cacheRoot = opensFiles ? InstallerCacheRoot.Resolve(_installerFolderOverride) : null;
        if (cacheRoot is not null)
            standing = Keep(standing, held, path =>
                CandidateGuard.CheckSafeToRemove(path, cacheRoot) == CandidateGuard.RemovalSafety.Safe
                    ? null
                    : HeldBackReason.FileNotConfirmed);

        var registrationReads = default(FileIdentityReadTally);
        if (_fileIds is not null && standing.Count > 0)
        {
            var comparison = RegistrationIdentityMatch.Compare(
                _fileIds, query.Packages, standing, cancellationToken);
            registrationReads = comparison.Registrations;

            var answers = comparison.Answers;
            var index = 0;
            standing = Keep(standing, held, _ =>
            {
                var answer = answers[index++];
                return answer.Verdict switch
                {
                    CandidateIdentityVerdict.Unclaimed => null,
                    CandidateIdentityVerdict.Claimed => CauseOfIdentityClaim(answer.ClaimedBy!),
                    CandidateIdentityVerdict.Unestablished => HeldBackReason.FileNotConfirmed,
                    _ => throw new ArgumentOutOfRangeException(nameof(walkDerived), answer.Verdict,
                        "An identity verdict with no arm here. Name it in the same edit as the enum member."),
                };
            });
        }

        // THE SCAN'S OWN WHOLESALE WITHHOLDING, asked through the expressions the scan
        // asks it through, so a leg added there is acted on here without this file
        // being edited. Where the identity comparison did not run, its tally is zero
        // attempts and the enumeration's two legs answer alone.
        if (WithholdingLegs.Any(query.Census, registrationReads))
            standing = Keep(standing, held, _ => HeldBackReason.OwnershipUnestablished);

        if (_declaredProducts is not null && cacheRoot is not null && standing.Count > 0)
            standing = ScreenByWhatTheyDeclare(
                standing, held, cacheRoot, query.Installations, query.UnlistedProductKeys, cancellationToken, progress, out givenUp, out waitCount);

        // The age check, as the scan runs it: every file held, and no file's times read, where
        // an in-progress signal was set just after the clock, and otherwise every file whose
        // times are not before the clock, and then every file left where the clock went back
        // while the times were read (CachedFileAge.ClockWentBack).
        if (_fileTimes is not null && standing.Count > 0)
        {
            if (installerActivity?.IsBlocked == true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _ = Keep(standing, held, _ => HeldBackReason.FileNotConfirmed);
            }
            else
            {
                standing = Keep(standing, held, path =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var outcome = _fileTimes.ReadOutcome(path, out var times);
                    return CachedFileAge.IsBeforeTheClock(outcome, times, clock)
                        ? null
                        : HeldBackReason.FileNotConfirmed;
                });

                if (standing.Count > 0 && CachedFileAge.ClockWentBack(_clock, wallAtStart))
                    _ = Keep(standing, held, _ => HeldBackReason.FileNotConfirmed);
            }
        }

        return (givenUp, waitCount);
    }

    /// <summary>
    /// Puts <paramref name="standing"/> to the declared-product screen as the scan
    /// does, holding every answer about a product against the installations this pass's
    /// own enumeration listed, and returns what it lets through. A screen that answers
    /// about a different number of files than it was handed has not answered about these
    /// files, so all of them are held.
    ///
    /// <see cref="DeclaredProductOutcome.SecondCopyUnestablished"/> IS HELD AS
    /// <see cref="HeldBackReason.OwnershipUnestablished"/>, being a finding about another
    /// installation on the machine and not about the file. Every other verdict that
    /// withholds is held as <see cref="HeldBackReason.FileNotConfirmed"/>.
    ///
    /// A wait the screen makes on a source folder is reported through
    /// <paramref name="progress"/> as it starts and as it ends, as the scan reports its own,
    /// and every drive or share it gives up comes back in <paramref name="givenUp"/>
    /// (<see cref="DeclaredProductScreening.RootsGivenUp"/>), none where its answer is not
    /// used. How many waits it made comes back in <paramref name="waitCount"/>
    /// (<see cref="DeclaredProductScreening.WaitCount"/>) whether or not its answer is used,
    /// each having been reported.
    /// </summary>
    private List<string> ScreenByWhatTheyDeclare(
        List<string> standing,
        Dictionary<string, HeldBackReason> held,
        InstallerCacheRoot cacheRoot,
        IReadOnlyList<ListedInstallation> installations,
        IReadOnlyList<UnlistedProductKey> unlisted,
        CancellationToken cancellationToken,
        IProgress<ScanProgressUpdate>? progress,
        out IReadOnlyList<SourceRootGivenUp> givenUp,
        out int waitCount)
    {
        givenUp = [];
        waitCount = 0;
        var files = standing
            .Select(path => new OrphanedFile(
                FullPath: path,
                SizeBytes: 0,
                IsPatch: IsPatchFile(path),
                IsRemovablePatch: false,
                IsObsoleted: false,
                Reason: Resources.Strings.Reason_Orphaned))
            .ToList();

        var refusalLog = new PerItemFailureLog("Re-verify",
            "There is no other record of which files these were: a file the screen could not read is held "
            + "back from the batch and nothing else about it is kept.");
        try
        {
            var screening = _declaredProducts!.Screen(
                files, installations, unlisted, cancellationToken, (ex, cause) => refusalLog.Record(ex, cause),
                path => InstallerCacheHelpers.NamesAFileDirectlyInInstallerFolder(path, cacheRoot),
                waitingOn: wait => progress?.Report(ScanProgressUpdate.Waiting(wait)),
                installerFolder: cacheRoot.Proven ? cacheRoot.Resolved : null,
                isTheInstallerFolder: folder => InstallerCacheHelpers.IsTheInstallerFolder(folder, cacheRoot));
            var outcomes = screening.Outcomes;
            waitCount = screening.WaitCount;

            if (outcomes.Count != files.Count)
            {
                foreach (var path in standing) held.TryAdd(path, HeldBackReason.FileNotConfirmed);
                return new List<string>();
            }

            givenUp = screening.RootsGivenUp;
            var index = 0;
            return Keep(standing, held, _ => outcomes[index++] switch
            {
                DeclaredProductOutcome.SecondCopyUnestablished => HeldBackReason.OwnershipUnestablished,
                var outcome when outcome.Withholds() => HeldBackReason.FileNotConfirmed,
                _ => null,
            });
        }
        finally
        {
            refusalLog.WriteClosingEntry();
        }
    }

    /// <summary>
    /// The cause for a walk-derived file whose identity matches a registration: the
    /// cause of the strongest non-removable row among
    /// <paramref name="claimedBy"/>, a live claim before an unread verdict, and
    /// <see cref="HeldBackReason.FileNotConfirmed"/> where every row naming the file
    /// is still removable.
    /// </summary>
    private static HeldBackReason CauseOfIdentityClaim(IReadOnlyList<RegisteredPackage> claimedBy)
    {
        var cause = HeldBackReason.FileNotConfirmed;
        foreach (var row in claimedBy)
        {
            if (row.IsRemovable) continue;
            if (CauseOfNonRemovable(row) == HeldBackReason.Reclaimed) return HeldBackReason.Reclaimed;
            cause = HeldBackReason.RecordsUnreadable;
        }

        return cause;
    }

    /// <summary>
    /// Returns the paths <paramref name="decide"/> answers null for, in order, and
    /// adds every other path to <paramref name="held"/> under the cause it answered.
    /// <paramref name="decide"/> is called once per path, in order.
    /// </summary>
    private static List<string> Keep(
        List<string> paths,
        Dictionary<string, HeldBackReason> held,
        Func<string, HeldBackReason?> decide)
    {
        var kept = new List<string>(paths.Count);
        foreach (var path in paths)
        {
            var reason = decide(path);
            if (reason is null) kept.Add(path);
            else held.TryAdd(path, reason.Value);
        }

        return kept;
    }

    /// <summary>Whether a walk-derived path is a patch file, by its extension as the scan tells it.</summary>
    private static bool IsPatchFile(string path) =>
        PathText.GetExtension(path).Equals(".msp", StringComparison.OrdinalIgnoreCase);
}
