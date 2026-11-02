using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IRemovableReverifier"/>: re-runs
/// <see cref="IInstallerQueryService.GetRegisteredPackagesAsync"/>, drops any
/// candidate whose path a currently-registered, non-removable package claims, and
/// judges every candidate no registration names the way the scan judged it.
/// Testable through the same <c>IMsiApi</c> seam the query service uses.
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
/// (<see cref="CachedFileAge"/>). Each step can only keep a file back. The
/// under-lease re-read below asks about registrations alone.
/// </summary>
public sealed class RemovableReverifier : IRemovableReverifier
{
    private readonly IInstallerQueryService _queryService;
    private readonly Interop.IMsiApi _msi;
    private readonly IFileIdentityReader? _fileIds;
    private readonly IDeclaredProductCheck? _declaredProducts;
    private readonly IFileTimesReader? _fileTimes;
    private readonly IPendingRebootService? _installerActivity;
    private readonly TimeProvider _clock;
    private readonly string? _installerFolderOverride;

    /// <summary>
    /// Production constructor. The query service answers the pre-lease pass and the
    /// raw API the under-lease one, two seams rather than one because
    /// <see cref="IInstallerQueryService"/> offers a whole enumeration and nothing
    /// narrower, and an enumeration is kept outside the machine-wide installer lock.
    /// The three readers, the clock and the in-progress signals are the scan's own, for
    /// the steps this pass re-runs on a file no registration names. The signals' read is
    /// also handed back on the result, where a blocked one stops the whole batch in each
    /// host (<see cref="ReverifyResult.InstallerActivity"/>).
    /// </summary>
    public RemovableReverifier(IInstallerQueryService queryService, Interop.IMsiApi msi,
        IFileIdentityReader fileIdentities, IDeclaredProductCheck declaredProducts,
        IFileTimesReader fileTimes, TimeProvider clock, IPendingRebootService installerActivity)
        : this(queryService, msi, fileIdentities, declaredProducts, fileTimes, clock, null,
            installerActivity) { }

    /// <summary>
    /// Test constructor for the tests whose subject is the registrations: no file
    /// is opened, so a file no registration names is judged on the enumeration's
    /// own two legs alone. A pass built this way can only keep back less than one
    /// built with its readers, never more.
    /// </summary>
    internal RemovableReverifier(IInstallerQueryService queryService, Interop.IMsiApi msi)
        : this(queryService, msi, null, null, null, null, null, null) { }

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
    /// which is what each host stops the whole batch on. Null reads nothing: the age check
    /// then judges every file by its times alone, and the result carries no read, so no
    /// host has one to stop on.
    /// </param>
    internal RemovableReverifier(IInstallerQueryService queryService, Interop.IMsiApi msi,
        IFileIdentityReader? fileIdentities, IDeclaredProductCheck? declaredProducts,
        IFileTimesReader? fileTimes, TimeProvider? clock, string? installerFolderOverride,
        IPendingRebootService? installerActivity = null)
    {
        _queryService = queryService;
        _msi = msi;
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
    /// age check's hold nor the hosts' stop on a blocked read has anything to act on. Both
    /// test constructors can leave it off.
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
        // read goes back on the result too, where a blocked one stops the whole batch in
        // each host (ReverifyResult.InstallerActivity).
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

        // The claims naming a surviving path, carried forward so the action
        // service can re-read them under its own hold. Filtered to the survivors
        // rather than passed whole: a dropped path is already out of the batch,
        // and re-reading it under the installer lock would be work done to
        // confirm a decision nothing can act on.
        //
        // WITH THE PAIRINGS THE ENUMERATION FOUND BY ASKING RATHER THAN BY LISTING:
        // each installation that, asked by name, answered that it holds a surviving
        // patch, where its own enumeration produced no claim for it. Such an
        // installation's hold on the patch is as much a part of the offer as a
        // claim's, so it is re-read on the same terms.
        var survivingPaths = new HashSet<string>(surviving, StringComparer.OrdinalIgnoreCase);
        var survivingClaims = query.PatchClaims
            .Concat(query.PairingsHeldByName)
            .Where(c => survivingPaths.Contains(c.LocalPackagePath))
            .ToList();

        // THE SIBLING PAIRINGS, carried forward so the under-lease re-read can apply the
        // per-product condition rather than only the batch's own pairings. The offer
        // rests on a fact about OTHER patches, and a re-verify that does not re-check the
        // fact the offer rests on is not a re-verify.
        //
        // Collected here rather than under the lease because collecting them needs the
        // enumeration this pass has just run, and an enumeration is kept outside the
        // machine-wide installer lock. What crosses into the lock is a list of codes to
        // re-read by key.
        //
        // It includes the surviving claims themselves, a patch's own removability being
        // part of the condition, and it is deduplicated by pairing rather than by patch:
        // one patch registered to three products is three pairings and each answers for
        // its own product.
        var survivingProducts = new HashSet<string>(
            survivingClaims.Select(c => c.ProductCode), StringComparer.OrdinalIgnoreCase);
        var siblingClaims = query.PatchClaims
            .Concat(query.PairingsHeldByName)
            .Concat(query.PairingsOfHoldersWithNoClaims)
            .Where(c => survivingProducts.Contains(c.ProductCode))
            .DistinctBy(c => (c.PatchCode.ToUpperInvariant(), c.ProductCode.ToUpperInvariant(),
                c.UserSid?.ToUpperInvariant(), c.Context))
            .ToList();

        return new ReverifyResult(surviving.AsReadOnly(), dropped.AsReadOnly(), reasons,
            survivingClaims.AsReadOnly(), siblingClaims.AsReadOnly(), sourceRootsGivenUp, sourceWaitCount,
            installerActivity);
    }

    /// <summary>
    /// The cause a non-removable row supports: records that were not read to a
    /// verdict for a row whose verdict was unread or withheld, a live claim for any
    /// other.
    /// </summary>
    private static HeldBackReason CauseOfNonRemovable(RegisteredPackage pkg) =>
        pkg.RemovableWithheld || pkg.VerdictUnreadable
            ? HeldBackReason.RecordsUnreadable
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
        Path.GetExtension(path).Equals(".msp", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    /// <remarks>
    /// IT HAS REAL WORK ON ANY BATCH HOLDING A SUPERSEDED PATCH. Its input is both
    /// halves the rule needs: the claims naming a surviving candidate, and the claims
    /// on every product those name. A surviving superseded patch is named by every
    /// product this scan could read a cached path from, and the path that put it in
    /// the batch is one of those reads, so the first half is non-empty on any batch
    /// containing one. This is the last check standing in front of a permanent
    /// delete.
    ///
    /// IT RE-ASKS BOTH HALVES OF THE RULE THE OFFER RESTS ON. The loop below re-asks
    /// the batch's own pairings, and a path survives it only where every claim on it
    /// comes back as a record that is still there, still readable, still superseded and
    /// still declaring zero; the last two are what <c>IsRemovablePatch</c> answers and
    /// they are half the rule. The pass after it re-asks the OTHER patches registered to
    /// the products those pairings name, which is the other half: a superseded patch is
    /// offered only where the per-product condition positively established that nothing
    /// on any product sharing it could be uninstalled and roll back onto its file. The
    /// offer rests on that fact about other patches, and a re-verify that does not
    /// re-check the fact the offer rests on is not a re-verify.
    ///
    /// THE SECOND HALF IS KEYED READS AND NEVER AN ENUMERATION, which is what makes it
    /// affordable with the machine-wide lease held. The pre-lease pass has already
    /// worked out which pairings to look at, so this asks about records by name, and
    /// every answer is about the record named rather than about the machine, which is
    /// the property an enumeration cannot offer.
    ///
    /// A KEYED READ COMES BACK IN FOUR SHAPES. They are a value, a positive "there is
    /// no such record", a read that failed, and an answer that came back empty without
    /// having failed. The batch's own pairings tell the first three apart. An empty
    /// answer there counts with the reads that failed where the verdict turns on it, an
    /// empty State or an empty Uninstallable beside a superseded State, and beside an
    /// applied or obsoleted State it is a claim, which is how the scan reads the patch's
    /// own row. The sibling reads tell two. A read that failed, an answer that
    /// there is no such record and an empty answer are an inability, which is how the
    /// scan's reading of a product's patch set takes each of them, and a value that is
    /// present and not zero is a live claim on the rollback. Only a clean value lets a
    /// path through, a removable reading for the batch's own pairings and a zero for a
    /// sibling, so what the other shapes decide between is the cause counted for the
    /// path and not whether it is held back.
    ///
    /// THE SET IT RE-READS IS WHAT THE PRE-LEASE ENUMERATION FOUND HOLDING EACH PATCH.
    /// The batch's pairings are its claims on a surviving path, and each installation
    /// that, asked by name, answered that it holds a surviving patch where no claim
    /// recorded it. The siblings are the other claims on the products those name, and,
    /// for an installation found by asking that holds no claim at all, every patch
    /// Windows listed for it. A registration neither found is not re-read here: a patch
    /// on a product holding none of the batch's own, a patch past the end of a list
    /// that did not run to its end, and anything registered after the enumeration.
    ///
    /// SO THIS IS NOT THE SCAN'S OWN CONDITION RE-RUN UNDER THE LEASE, and nothing may
    /// describe it as one. That condition asks every product any of its sources names,
    /// against each product's registered patch set as the registry lists it and
    /// worsened by what the enumeration read. This puts the same two questions about a
    /// bounded set of records.
    ///
    /// THE PRE-LEASE PASS IS THE WIDER CHECK. It re-runs the whole enumeration moments
    /// earlier and applies that condition at its full width, and a path it condemns
    /// never arrives here at all. What it cannot do is run again inside the hold, and
    /// that is what this adds: the same two questions, put about a bounded set of
    /// records, in the window its own enumeration leaves open.
    ///
    /// DO NOT NARROW THIS BACK TO THE BATCH'S OWN PAIRINGS. That re-asks half the rule
    /// the offer rests on and drops the half about other patches. What the sibling
    /// reads cost is on this method's parameter in the interface.
    /// </remarks>
    public UnderLeaseRecheck RecheckUnderLease(UnderLeaseClaims claims)
    {
        var batchClaims = claims.Batch;
        var siblingClaims = claims.Siblings;
        if (batchClaims.Count == 0) return new UnderLeaseRecheck(Array.Empty<string>());

        // Every condemned path's cause, and the paths in the order each was first
        // condemned, which is the order they are handed back in.
        var causes = new Dictionary<string, HeldBackReason>(StringComparer.OrdinalIgnoreCase);
        var condemnedInOrder = new List<string>();

        void Condemn(string path, HeldBackReason reason)
        {
            if (causes.TryGetValue(path, out var already))
            {
                causes[path] = Stronger(already, reason);
                return;
            }

            causes[path] = reason;
            condemnedInOrder.Add(path);
        }

        // A LIVE CLAIM OUTRANKS EVERYTHING ELSE, and a record that has gone outranks a
        // read that did not answer, as a finding outranks an inability. So a path's
        // claims are read on past one that condemns it, until one is a live claim,
        // which nothing can outrank, or until every claim has answered. Which of a
        // path's own claims comes first then decides nothing about its cause. A path that has
        // passed so far is read on for a different reason: every claim on it has to
        // answer, because the one that has moved may be any of them.
        foreach (var claim in batchClaims)
        {
            if (causes.TryGetValue(claim.LocalPackagePath, out var settled)
                && settled == HeldBackReason.Reclaimed)
                continue;

            var context = (Interop.MsiInstallContext)claim.Context;
            var state = InstallerQueryService.GetPatchProperty(
                _msi, claim.PatchCode, claim.ProductCode, claim.UserSid, context,
                Interop.MsiInstallProperty.State);
            var uninstallable = InstallerQueryService.GetPatchProperty(
                _msi, claim.PatchCode, claim.ProductCode, claim.UserSid, context,
                Interop.MsiInstallProperty.Uninstallable);

            var notRegistered = state.NotRegistered || uninstallable.NotRegistered;
            var unreadable = (state.Unreadable && !state.NotRegistered)
                || (uninstallable.Unreadable && !uninstallable.NotRegistered);

            // The order these are asked in IS the judgement; what each cause means
            // is on HeldBackReason, once.
            //
            // Absence first. It condemns, so the two tests below would condemn the
            // same file anyway, and they would name the wrong cause doing it: asked
            // after the removable test, an absent record answers that test with an
            // empty State string and is counted as a reclaim. The outcome is the same
            // either way, and the order is what keeps the cause counted in the opt-in
            // result log true.
            //
            // Unreadable second, so a pairing where one read failed and the other
            // came back absent is reported as the failure it contains. A read that
            // could not be made has not shown the file to be removable, this is the
            // last check standing in front of a permanent delete, and the scan's own
            // rule fails the same way. The pre-lease pass answers a failed read of
            // this same pairing the same way, through the row flag its enumeration
            // sets; what it carries and this does not is a whole enumeration's
            // inherited withholding, which has no counterpart here because this
            // judges one named pairing.
            //
            // An empty answer the verdict turns on is counted with the reads that did
            // not answer, as the pre-lease pass counts it through the row flag
            // (InstallerQueryService.LeavesVerdictUnestablished). An applied or
            // obsoleted pairing is a claim whatever its Uninstallable says.
            var reason =
                notRegistered && !unreadable ? HeldBackReason.RecordsChanged
                : unreadable ? HeldBackReason.RecordsUnreadable
                : InstallerQueryService.IsRemovablePatch(state.Value, uninstallable.Value)
                    ? (HeldBackReason?)null
                : InstallerQueryService.LeavesVerdictUnestablished(state.Value, uninstallable.Value)
                    ? HeldBackReason.RecordsUnreadable
                    : HeldBackReason.Reclaimed;

            if (reason is null) continue;

            Condemn(claim.LocalPackagePath, reason.Value);
        }

        // A PATH ITS OWN PAIRINGS CONDEMNED IS SETTLED BY THEM, and the per-product
        // condition below does not change its cause. The scan is the same: its reading
        // of a product's patch sets takes the verdict away only from a row that still
        // has one, so a row its own pairings left non-removable keeps the cause they
        // gave it.
        var settledByOwnPairings = new HashSet<string>(causes.Keys, StringComparer.OrdinalIgnoreCase);

        // THE PER-PRODUCT CONDITION, RE-READ BY KEY. Everything above re-asks about the
        // batch's own pairings; this re-asks about the OTHER patches on the products
        // those pairings name, which is the fact the offer actually rests on. Without it
        // a sibling patch turning removable between the pre-lease enumeration and this
        // moment would go unseen.
        //
        // Keyed reads and never an enumeration, which is what makes it affordable under
        // the machine-wide lease: the pre-lease pass already worked out exactly which
        // pairings to look at, so this asks about records by name and each answer is
        // about the record named rather than about the machine.
        //
        // ONE PRODUCT'S FAILURE CONDEMNS EVERY BATCH PATH ON THAT PRODUCT, which is the
        // shape of the condition rather than a shortcut: the patch's one cached file is
        // shared by every product holding it, so a rollback on any of them reaches for
        // it.
        //
        // A LIVE CLAIM OUTRANKS AN INABILITY, as it does in the scan's reading of a
        // product's patch set. So a sibling that did not answer does not end the reads
        // on its product: they go on until one sibling is a live claim, which settles
        // the product, or until every sibling has been read, and the product is counted
        // as unread only where none was a claim. A batch path its own pairings passed,
        // registered to several products, is counted as a live claim where any of them
        // gave one. Which sibling, and which product, comes first then decides nothing
        // about the cause.
        var productsAlreadyJudged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sibling in siblingClaims)
        {
            // A product settled by an earlier sibling needs no second look. The verdict
            // is one-way, so re-asking could only cost reads while the lease is held.
            if (!productsAlreadyJudged.Add(sibling.ProductCode)) continue;

            // The cause this product's siblings support, null while every one read so
            // far is a clean zero.
            HeldBackReason? productReason = null;

            foreach (var onThisProduct in siblingClaims)
            {
                if (!string.Equals(onThisProduct.ProductCode, sibling.ProductCode,
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                var siblingContext = (Interop.MsiInstallContext)onThisProduct.Context;
                var siblingUninstallable = InstallerQueryService.GetPatchProperty(
                    _msi, onThisProduct.PatchCode, onThisProduct.ProductCode,
                    onThisProduct.UserSid, siblingContext,
                    Interop.MsiInstallProperty.Uninstallable);

                // A POSITIVE ZERO IS THE ONLY CLEAN ANSWER, and the scan reads a
                // zero the same way. Anything else condemns: a read that failed, an
                // answer that came back empty, and an answer that the installation holds
                // no record of the patch or that its product is not installed. The
                // sibling claims are the pairings the pre-lease re-verify's enumeration
                // listed moments before the lease was taken (UnderLeaseClaims.From),
                // and each is read in its own account and context, so an answer that
                // the patch or the product is not there contradicts that listing. Where
                // the patch or the installation has really gone in between, the batch
                // path is held back all the same.
                if (!siblingUninstallable.Unreadable && siblingUninstallable.Value == "0") continue;

                // A read that failed and an answer that came back empty are an
                // inability, since neither says whether the patch can be uninstalled,
                // and the reads go on. A value that is present and not zero is a live
                // claim on the rollback, and settles the product. The scan reads an
                // answer that came back empty into a product's patch set as
                // unestablished, and the pre-lease pass counts the file it holds for
                // that under RecordsUnreadable where no patch on any product sharing the
                // file is a claim. Change this arm and that reading together, or one answer is
                // counted under two causes depending on which pass met it.
                if (siblingUninstallable.Unreadable || siblingUninstallable.Value.Length == 0)
                {
                    productReason = HeldBackReason.RecordsUnreadable;
                    continue;
                }

                productReason = HeldBackReason.Reclaimed;
                break;
            }

            if (productReason is null) continue;

            // Every batch path registered to this product goes, with the cause the
            // product's siblings support.
            foreach (var batchClaim in batchClaims)
            {
                if (!string.Equals(batchClaim.ProductCode, sibling.ProductCode,
                        StringComparison.OrdinalIgnoreCase))
                    continue;
                if (settledByOwnPairings.Contains(batchClaim.LocalPackagePath)) continue;

                Condemn(batchClaim.LocalPackagePath, productReason.Value);
            }
        }

        var reasons = default(HeldBackReasons);
        foreach (var path in condemnedInOrder)
            reasons = reasons.Plus(causes[path]);

        return new UnderLeaseRecheck(condemnedInOrder.AsReadOnly(), reasons);
    }

    /// <summary>
    /// The stronger of two causes found for one path under the lease: a live claim,
    /// then a registration that has gone, then records not read to a verdict. The
    /// first two are findings and the third is an inability, and a finding outranks
    /// an inability. Only those three are found under the lease.
    /// </summary>
    private static HeldBackReason Stronger(HeldBackReason a, HeldBackReason b) =>
        Rank(a) >= Rank(b) ? a : b;

    private static int Rank(HeldBackReason reason) => reason switch
    {
        HeldBackReason.Reclaimed => 2,
        HeldBackReason.RecordsChanged => 1,
        HeldBackReason.RecordsUnreadable => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason,
            "Only a live claim, a registration that has gone and an unread record are found under the lease."),
    };
}
