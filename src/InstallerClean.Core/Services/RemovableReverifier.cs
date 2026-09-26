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
    private readonly TimeProvider _clock;
    private readonly string? _installerFolderOverride;

    /// <summary>
    /// Production constructor. The query service answers the pre-lease pass and the
    /// raw API the under-lease one, two seams rather than one because
    /// <see cref="IInstallerQueryService"/> offers a whole enumeration and nothing
    /// narrower, and an enumeration is kept outside the machine-wide installer lock.
    /// The three readers and the clock are the scan's own, for the steps this pass
    /// re-runs on a file no registration names.
    /// </summary>
    public RemovableReverifier(IInstallerQueryService queryService, Interop.IMsiApi msi,
        IFileIdentityReader fileIdentities, IDeclaredProductCheck declaredProducts,
        IFileTimesReader fileTimes, TimeProvider clock)
        : this(queryService, msi, fileIdentities, declaredProducts, fileTimes, clock, null) { }

    /// <summary>
    /// Test constructor for the tests whose subject is the registrations: no file
    /// is opened, so a file no registration names is judged on the enumeration's
    /// own two legs alone. A pass built this way can only keep back less than one
    /// built with its readers, never more.
    /// </summary>
    internal RemovableReverifier(IInstallerQueryService queryService, Interop.IMsiApi msi)
        : this(queryService, msi, null, null, null, null, null) { }

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
    internal RemovableReverifier(IInstallerQueryService queryService, Interop.IMsiApi msi,
        IFileIdentityReader? fileIdentities, IDeclaredProductCheck? declaredProducts,
        IFileTimesReader? fileTimes, TimeProvider? clock, string? installerFolderOverride)
    {
        _queryService = queryService;
        _msi = msi;
        _fileIds = fileIdentities;
        _declaredProducts = declaredProducts;
        _fileTimes = fileTimes;
        _clock = clock ?? TimeProvider.System;
        _installerFolderOverride = installerFolderOverride;
    }

    /// <summary>
    /// Whether this pass opens the files no registration names, for the test that
    /// holds the hosts' pass to doing so. Both test constructors can leave it off.
    /// </summary>
    internal bool ChecksFiles =>
        _fileIds is not null && _declaredProducts is not null && _fileTimes is not null;

    public async Task<ReverifyResult> ReverifyAsync(
        IReadOnlyList<string> candidatePaths,
        CancellationToken cancellationToken = default)
    {
        if (candidatePaths.Count == 0)
            return new ReverifyResult(candidatePaths, Array.Empty<string>());

        // The age check's clock, read once and before anything else is read, as the
        // scan reads its own: a file created or changed while this pass runs is later
        // than it.
        var clock = _clock.GetUtcNow();

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
        // of what this map is for. A patch whose State or Uninstallable read failed
        // lands here having established nothing either way: non-removable for want
        // of a verdict rather than on one, so reporting it as a program reclaiming
        // the file would name a cause that did not occur. The withheld kind is a
        // third: a superseded patch whose product's patch set this run could not
        // establish is non-removable for want of a reading rather than on one. Every
        // kind can be in one batch, which is why the cause is carried per path and
        // not per run.
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

        HoldWalkDerivedFilesTheScanWouldHold(walkDerived, query, clock, held, cancellationToken);

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
        var survivingPaths = new HashSet<string>(surviving, StringComparer.OrdinalIgnoreCase);
        var survivingClaims = query.PatchClaims
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
            .Where(c => survivingProducts.Contains(c.ProductCode))
            .ToList();

        return new ReverifyResult(surviving.AsReadOnly(), dropped.AsReadOnly(), reasons,
            survivingClaims.AsReadOnly(), siblingClaims.AsReadOnly());
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
    /// WHERE A LEG FIRES, EVERY FILE STILL STANDING IS HELD AND THE LAST TWO STEPS DO
    /// NOT RUN, as in the scan: each file is already kept on a fact about the machine.
    /// </summary>
    private void HoldWalkDerivedFilesTheScanWouldHold(
        List<string> walkDerived,
        InstallerQueryResult query,
        DateTimeOffset clock,
        Dictionary<string, HeldBackReason> held,
        CancellationToken cancellationToken)
    {
        if (walkDerived.Count == 0) return;

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

        // THE SCAN'S OWN WHOLESALE WITHHOLDING, asked through the expression the scan
        // asks it through, so a leg added there is acted on here without this file
        // being edited. Where the identity comparison did not run, its tally is zero
        // attempts and the enumeration's two legs answer alone.
        if (WithholdingLegs.Any(query.Census, registrationReads))
        {
            foreach (var path in standing) held.TryAdd(path, HeldBackReason.OwnershipUnestablished);
            return;
        }

        if (_declaredProducts is not null && cacheRoot is not null && standing.Count > 0)
            standing = ScreenByWhatTheyDeclare(standing, held, cacheRoot, cancellationToken);

        if (_fileTimes is not null && standing.Count > 0)
            _ = Keep(standing, held, path =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var outcome = _fileTimes.ReadOutcome(path, out var times);
                return CachedFileAge.Judge(outcome, times, clock) == CachedFileAgeVerdict.ShownADayOld
                    ? null
                    : HeldBackReason.FileNotConfirmed;
            });
    }

    /// <summary>
    /// Puts <paramref name="standing"/> to the declared-product screen as the scan
    /// does, and returns what it lets through. A screen that answers about a different
    /// number of files than it was handed has not answered about these files, so all
    /// of them are held.
    /// </summary>
    private List<string> ScreenByWhatTheyDeclare(
        List<string> standing,
        Dictionary<string, HeldBackReason> held,
        InstallerCacheRoot cacheRoot,
        CancellationToken cancellationToken)
    {
        var files = standing
            .Select(path => new OrphanedFile(
                FullPath: path,
                SizeBytes: 0,
                IsPatch: Path.GetExtension(path).Equals(".msp", StringComparison.OrdinalIgnoreCase),
                IsRemovablePatch: false,
                IsObsoleted: false,
                Reason: Resources.Strings.Reason_Orphaned))
            .ToList();

        var refusalLog = new PerItemFailureLog("Re-verify",
            "There is no other record of which files these were: a file the screen could not read is held "
            + "back from the batch and nothing else about it is kept.");
        try
        {
            var outcomes = _declaredProducts!.Screen(
                files, cancellationToken, (ex, cause) => refusalLog.Record(ex, cause),
                path => InstallerCacheHelpers.NamesAFileDirectlyInInstallerFolder(path, cacheRoot));

            if (outcomes.Count != files.Count)
            {
                foreach (var path in standing) held.TryAdd(path, HeldBackReason.FileNotConfirmed);
                return new List<string>();
            }

            var index = 0;
            return Keep(standing, held, _ =>
                outcomes[index++].Withholds() ? HeldBackReason.FileNotConfirmed : null);
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
    /// having failed. The batch's own pairings tell the first three apart. The sibling
    /// reads tell two: a read that failed and an answer that there is no such record
    /// are both an inability, and any value but a zero is a live claim on the rollback.
    /// In both halves an empty answer counts as a value that holds the path back. Only
    /// a clean value lets a path through, a removable reading for the batch's own
    /// pairings and a zero for a sibling, so what the other shapes decide between is
    /// the cause counted for the path and not whether it is held back.
    ///
    /// THE SET IT RE-READS IS BUILT FROM THE CLAIMS. The batch's pairings are the ones
    /// the pre-lease enumeration recorded as claims, and the siblings are the other
    /// claims on the products those name. A registration that enumeration produced no
    /// claim for is not re-read here: a patch it got no cached path for, a product it
    /// never returned even where the machine-wide patch enumeration names it, a
    /// product holding none of the batch's own patches that only a patch file's
    /// declared targets name, and anything registered after the claims were
    /// collected. The sibling set is built out of the products the surviving claims
    /// name, so a product whose only pairing on a surviving path gave no cached path
    /// is not in it, and nor is any other patch registered to that product.
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

        var heldBack = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reasons = default(HeldBackReasons);


        foreach (var claim in batchClaims)
        {
            // Any one claim turning non-removable settles the path, so once a path
            // is condemned its remaining claims are not queried. A path that has
            // passed so far still is: every claim on it has to answer, because the
            // one that has moved may be any of them.
            if (seen.Contains(claim.LocalPackagePath)) continue;

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
            var reason =
                notRegistered && !unreadable ? HeldBackReason.RecordsChanged
                : unreadable ? HeldBackReason.RecordsUnreadable
                : !InstallerQueryService.IsRemovablePatch(state.Value, uninstallable.Value)
                    ? HeldBackReason.Reclaimed
                    : (HeldBackReason?)null;

            if (reason is null) continue;

            // One cause per path, taken from the claim that condemned it. Where a
            // path's claims disagree the later ones are never asked, so preferring
            // a different cause would mean more property reads with the
            // machine-wide installer lease held. The cause named is true of the
            // file; that a second one also applied is not a defect.
            seen.Add(claim.LocalPackagePath);
            heldBack.Add(claim.LocalPackagePath);
            reasons = reasons.Plus(reason.Value);
        }

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
        var productsAlreadyJudged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sibling in siblingClaims)
        {
            // A product settled by an earlier sibling needs no second look. The verdict
            // is one-way, so re-asking could only cost reads while the lease is held.
            if (!productsAlreadyJudged.Add(sibling.ProductCode)) continue;

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
                // zero the same way. Anything else condemns: a read that failed, a
                // value that is absent, and an answer that the installation holds no
                // record of the patch or that its product is not installed. The
                // sibling claims are the pairings the pre-lease re-verify's enumeration
                // listed moments before the lease was taken (UnderLeaseClaims.From),
                // and each is read in its own account and context, so either of those
                // answers contradicts that listing. Where the patch or the installation
                // has really gone in between, the batch path is held back all the same.
                if (!siblingUninstallable.Unreadable && siblingUninstallable.Value == "0") continue;

                // Every batch path registered to this product goes, with the cause the
                // read supports: a read that failed is an inability, and a patch that
                // answered something other than zero is a live claim on the rollback.
                var siblingReason = siblingUninstallable.Unreadable
                    ? HeldBackReason.RecordsUnreadable
                    : HeldBackReason.Reclaimed;

                foreach (var batchClaim in batchClaims)
                {
                    if (!string.Equals(batchClaim.ProductCode, sibling.ProductCode,
                            StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!seen.Add(batchClaim.LocalPackagePath)) continue;

                    heldBack.Add(batchClaim.LocalPackagePath);
                    reasons = reasons.Plus(siblingReason);
                }

                break;
            }
        }

        return new UnderLeaseRecheck(heldBack.AsReadOnly(), reasons);
    }
}
