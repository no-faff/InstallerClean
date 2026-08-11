using InstallerClean.Interop;
using InstallerClean.Interop.Native;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Services;

/// <summary>
/// Queries the Windows Installer API to build the complete set of registered
/// .msi and .msp files across all installation contexts, with the UserData
/// registry keys read behind it as a second source.
///
/// It asks the filesystem one question and no more: whether a path only the
/// registry claimed is really on the disk, which is what separates an
/// enumeration that came back short from a registry key an uninstall left
/// behind (see the cross-check in <see cref="GetRegisteredPackagesCore"/>). It
/// does not walk the cache folder and does not decide what is orphaned; that is
/// <see cref="IFileSystemScanService"/>'s, off the paths this returns.
/// </summary>
public sealed class InstallerQueryService : IInstallerQueryService
{
    /// <summary>
    /// SID meaning "all users". When passed to MsiEnumProductsEx /
    /// MsiEnumPatchesEx / MsiEnumComponentsEx, the API enumerates across
    /// every user profile on the machine. Requires admin elevation.
    /// </summary>
    private const string AllUsersSid = "S-1-1-0";

    /// <summary>
    /// SIDs are typically ~45 chars (e.g. S-1-5-21-xxx-xxx-xxx-xxxx).
    /// Pre-allocating 256 avoids re-enumerating just to get the SID.
    /// </summary>
    private const int SidBufferLength = 256;

    private readonly IMsiApi _msi;
    private readonly FallbackReader _readFallback;
    private readonly Action<Exception>? _crashLogSink;

    /// <summary>
    /// Reads a cached patch's own declared target products, which is the one
    /// source that does not depend on any enumeration having been complete. See
    /// <see cref="TargetsDeclaredByPatchFile"/>.
    /// </summary>
    private readonly IPackageIdentityReader _identityReader;

    /// <summary>
    /// Reads the registry fallback into <paramref name="claimed"/> and reports
    /// what it saw on the way.
    ///
    /// A seam rather than a direct call because the fallback is one half of the
    /// degraded-sources gate below, and the other half is already drivable
    /// through <see cref="IMsiApi"/>. Without it the gate's condition could not
    /// be reached by a test at all: the real reader opens HKLM directly, so a
    /// test can neither make it fail nor keep it from succeeding, and a rule
    /// about what happens when BOTH sources are short cannot be pinned by
    /// varying only one of them. Production wiring is unchanged; both public
    /// constructors bind the real reader.
    /// </summary>
    internal delegate FallbackRead FallbackReader(Dictionary<string, RegisteredPackage> claimed, CancellationToken ct);

    /// <summary>
    /// What one pass of the registry fallback found.
    /// </summary>
    /// <param name="Failures">
    /// Key reads that failed. Half of the degraded-sources gate: a fallback that
    /// read almost nothing cannot be the recovery a short API enumeration is
    /// allowed to lean on.
    /// </param>
    /// <param name="ProductKeys">
    /// Product subkeys walked under <c>UserData</c>, whether or not the entry
    /// inside carried a package path. It is the app's only independent count of
    /// how many products this machine has, which is what makes an enumeration
    /// that ended early visible at all; see the cross-check in
    /// <see cref="GetRegisteredPackagesCore"/> for what it can and cannot say.
    /// </param>
    /// <param name="UnclaimedProductFiles">
    /// Product entries whose <c>LocalPackage</c> path the API's own loop never
    /// claimed AND whose file is on the disk. One such entry is one installed
    /// product this enumeration did not reach, observed rather than inferred:
    /// see the cross-check in <see cref="GetRegisteredPackagesCore"/> for why
    /// both halves of that sentence are load-bearing.
    /// </param>
    /// <param name="UnclaimedPatchFiles">
    /// The same for patch entries. It carries no product count, a patch entry
    /// naming no product at all, so it can establish only that at least one
    /// product went unreached.
    /// </param>
    /// <param name="NonStringLocalPackageValues">
    /// Registrations whose <c>LocalPackage</c> value was PRESENT and was not a
    /// string, so nothing could be read out of it. A SUBSET of
    /// <see cref="Failures"/> rather than a term beside it, and the overlap is
    /// deliberate: the degraded-sources gate weighs reads that failed, this one
    /// failed, and narrowing that gate is not an instrumentation change's
    /// business. What it is for is the one thing the merged counter cannot say,
    /// namely whether anything on real machines writes that value under a type
    /// other than <c>REG_SZ</c>. Every other contributor to
    /// <see cref="Failures"/> is a thrown exception, so the two are separable by
    /// subtraction and neither has to state a cause for the other's members.
    ///
    /// Nothing writing these keys is obliged to use <c>REG_SZ</c>, and one
    /// machine's 136 of 136 says what that machine holds and nothing about the
    /// population.
    /// </param>
    /// <param name="RegistryProductCodes">
    /// The product codes behind <paramref name="ProductKeys"/>, unpacked out of
    /// the subkey names (see <see cref="UnpackRegistryProductCode"/>). The count
    /// answers how many products the machine has; this answers WHICH, and the
    /// difference is what lets an enumeration that came back short be named rather
    /// than estimated. Short of <paramref name="ProductKeys"/> by any key name
    /// that was not a packed GUID, which is a key naming no product to ask about.
    /// Null where the caller supplied no reader.
    /// </param>
    /// <param name="UnparseableProductKeyNames">
    /// Product subkeys counted in <paramref name="ProductKeys"/> whose name was
    /// not a packed GUID, so no code could be taken from them. The difference
    /// between the two, and the one state where naming products sees LESS than
    /// counting them did: the registry says the machine has this product and
    /// nothing can turn its name into a question. It withholds for that reason,
    /// on the same terms as a code Windows would not answer about.
    /// </param>
    internal readonly record struct FallbackRead(
        int Failures,
        int ProductKeys,
        int UnclaimedProductFiles = 0,
        int UnclaimedPatchFiles = 0,
        int NonStringLocalPackageValues = 0,
        IReadOnlyCollection<string>? RegistryProductCodes = null,
        int UnparseableProductKeyNames = 0);

    /// <summary>
    /// Production constructor: talks to the real msi.dll through
    /// <see cref="MsiApi"/>. Used by the integration tests that run against
    /// the elevated host, and by any caller that resolves the type directly.
    /// </summary>
    public InstallerQueryService() : this(new MsiApi()) { }

    /// <summary>
    /// Seam constructor: DI injects the real <see cref="MsiApi"/>; unit tests
    /// inject a fake so every error path that decides a file's fate can be
    /// driven without an elevated Windows host. Mirrors
    /// <see cref="PendingRebootService"/> taking <c>IRegistryReader</c> /
    /// <c>IMutexProbe</c>.
    /// </summary>
    public InstallerQueryService(IMsiApi msi) : this(msi, ReadRegistryFallback) { }

    /// <summary>
    /// Production constructor for the composed graph: DI supplies both seams.
    /// </summary>
    public InstallerQueryService(IMsiApi msi, IPackageIdentityReader identityReader)
        : this(msi, ReadRegistryFallback, null, identityReader) { }

    /// <summary>
    /// Full seam constructor, for the tests that drive both sources. See
    /// <see cref="FallbackReader"/>.
    /// </summary>
    /// <param name="crashLogSink">
    /// Where the run's budgeted breadcrumbs go; null is crash.log. A seam for
    /// the same reason <see cref="FallbackReader"/> is one: what the budget does
    /// on a machine whose registration refuses every product's patch list is
    /// reachable only by driving it, and driving it against the real sink would
    /// append two dozen entries to the crash log of whatever machine ran the
    /// suite. The registry fallback owns its own budget, being a static this
    /// never reaches.
    /// </param>
    /// <param name="identityReader">
    /// Null in the tests whose subject is the enumeration and the merge, where it
    /// binds a reader that yields nothing. That reads a patch file as having
    /// declared no targets, which is the same as the file being absent and is what
    /// those tests already assume; the tests whose subject IS route B inject one.
    /// </param>
    internal InstallerQueryService(IMsiApi msi, FallbackReader readFallback,
        Action<Exception>? crashLogSink = null, IPackageIdentityReader? identityReader = null)
    {
        _msi = msi;
        _readFallback = readFallback;
        _crashLogSink = crashLogSink;
        _identityReader = identityReader ?? NoPackageIdentity.Instance;
    }

    /// <summary>
    /// A reader that opens nothing and yields nothing, for the constructors that
    /// take no reader. It reports the file as unread rather than as unreadable,
    /// so a test that never meant to exercise route B is not silently made to
    /// withhold by it.
    /// </summary>
    private sealed class NoPackageIdentity : IPackageIdentityReader
    {
        internal static readonly NoPackageIdentity Instance = new();

        public Models.PackageIdentity? Read(string filePath, bool isPatch, out string detail)
        {
            detail = string.Empty;
            return new Models.PackageIdentity(string.Empty, isPatch, Array.Empty<string>());
        }
    }

    /// <inheritdoc />
    public Task<InstallerQueryResult> GetRegisteredPackagesAsync(
        IProgress<ScanProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GetRegisteredPackagesCore(progress, cancellationToken), cancellationToken);
    }

    private InstallerQueryResult GetRegisteredPackagesCore(
        IProgress<ScanProgressUpdate>? progress,
        CancellationToken ct)
    {
        // One entry per LocalPackage path. Every insertion goes through
        // MergeClaim, which carries the whole policy for what a second claim on
        // an already-claimed path does.
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);

        // One entry per CLAIM, deliberately not per path, and it is the merge
        // above that makes the difference load-bearing rather than a stylistic
        // one: MergeClaim keeps a single row per path, so the product code that
        // survives it is whichever product was reached first. Asking that one
        // product about the patch later asks about one of several and cannot see
        // what the others say, which is the exact hazard the act-time re-verify's
        // own remarks describe. Collected here because this loop is the only
        // place all of them exist at once.
        var patchClaims = new List<PatchClaim>();

        progress?.Report(new ScanProgressUpdate(Strings.Status_EnumeratingProducts));

        var (products, unreadableRows) = EnumerateProducts(ct);

        // Installed products this scan could not read in full. A skipped product
        // row is one product whose claims are wholly missing; a skipped patch
        // row, or a LocalPackage value that could not be read, is one product
        // whose claims are short by at least one. All three leave the same hole,
        // a claim that never reached the merge, so all three count the product
        // once. The loop below adds its own; this starts from the rows the
        // product enumeration itself lost.
        var unreadableProducts = unreadableRows;

        // Patches whose State or Uninstallable read failed. Decides nothing; see
        // the increment site for what it measures and why it is worth measuring.
        var unreadablePatchStates = 0;

        progress?.Report(new ScanProgressUpdate(Strings.Status_FoundProducts));

        // Budgeted, because the abandonment breadcrumb is one full entry per
        // product and its trigger is a property of the registration rather than
        // of one product: a SID the enumerator emits and then rejects as input
        // refuses every index for every product recorded under it. Each entry
        // carries a real message and stack trace, so a machine in that state
        // spends crash.log on near-identical copies of one already-recorded
        // condition, which is the very history a report of it would need.
        var abandonedLog = new PerItemFailureLog("Patch enumeration",
            "The product identity in the ones not logged is recorded nowhere else. The user is "
            + "told through the scan summary that something in the records could not be matched "
            + "up, and that notice names nothing and counts nothing.",
            _crashLogSink);

        // The closing entry is owed on every exit: the two gates below both
        // throw, and the both-sources-degraded one in particular fires on
        // exactly the broken registration that makes this storm.
        try
        {
        foreach (var (productCode, userSid, context) in products)
        {
            ct.ThrowIfCancellationRequested();

            // Every way this one product's records can come back short reaches
            // the same count, and reaches it once. The number the user reads is
            // programs, not failures, so one program with a failed package read
            // AND two failed patch rows is one program, exactly as a product
            // whose whole row was skipped is one. Counting failures instead
            // would inflate the notice without telling anyone more.
            var recordsShort = false;

            var productName = GetProductProperty(productCode, userSid, context, MsiInstallProperty.ProductName).Value;
            var localPackage = GetProductProperty(productCode, userSid, context, MsiInstallProperty.LocalPackage);

            // LocalPackage is the one property whose failed read DELETES this
            // product's claim rather than degrading it. An unreadable State
            // leaves patchState 0 and an unreadable Uninstallable leans
            // non-removable, so either still merges a row that says "needed";
            // an unreadable LocalPackage skips the insertion entirely, and the
            // product's "I still have this file" never reaches the merge at all.
            // That is the same information loss as a skipped enumeration row, so
            // it is counted the same way and withholds the same class. Without
            // the count it is worse than a skipped row, because the scan would
            // report itself complete while short of a claim.
            if (localPackage.Unreadable)
            {
                recordsShort = true;
            }
            else if (localPackage.Value.Length > 0)
            {
                // Ticker, not milestone: one of these fires per product,
                // up to hundreds in a few seconds, so the consumer must
                // not feed it to a screen-reader live region.
                progress?.Report(new ScanProgressUpdate(
                    productName.Length > 0 ? productName : productCode, IsMilestone: false));
                MergeClaim(claimed,
                    new RegisteredPackage(NormaliseLocalPackagePath(localPackage.Value), productName, productCode),
                    ClaimSource.InstallerApi);
            }

            var (patches, patchesIncomplete) = EnumeratePatches(productCode, userSid, context, ct, abandonedLog);
            if (patchesIncomplete) recordsShort = true;

            foreach (var (patchCode, patchUserSid, patchContext) in patches)
            {
                ct.ThrowIfCancellationRequested();

                var patchPath = GetPatchProperty(_msi, patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.LocalPackage);

                // The patch-side half of the same loss: this product holds the
                // patch, the row naming it came back, and the path it claims
                // could not be read. A patch is cached once and shared across
                // the products holding it, so the claim just lost may be the
                // Applied one that keeps another product's superseded-looking
                // copy alive.
                if (patchPath.Unreadable)
                {
                    recordsShort = true;
                }
                else if (patchPath.Value.Length > 0)
                {
                    var stateRead = GetPatchProperty(_msi, patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.State);
                    var uninstallableRead = GetPatchProperty(_msi, patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.Uninstallable);
                    var stateStr = stateRead.Value;

                    // Nothing here decides whether a file may be removed, and that
                    // is the 3.0.0 change rather than an omission. Both properties
                    // are still read and neither grants anything: every registered
                    // patch is kept, so what the pair is for now is whether the
                    // records answered at all. A read that failed leaves nothing
                    // established about the registration, which no surface may
                    // describe as a claim, and the count travels beside the flag
                    // because nobody knows how often either read fails on a machine
                    // that is not the one this was measured on.
                    //
                    // BOTH READS SURVIVE, INCLUDING THE ONE NO RULE CONSUMES.
                    // Uninstallable answered the second half of the old removable
                    // rule and answers nothing now, but dropping it would narrow
                    // what counts as "the records did not answer" without saying
                    // so: the merge below prefers a row that established something
                    // over one that did not, and the act-time re-check names its
                    // cause from the same flag, so a half-read record quietly
                    // becoming a fully-read one moves both. It also keeps
                    // UnreadablePatchStates measuring across this release what it
                    // measured before it.
                    var verdictUnreadable = stateRead.Unreadable || uninstallableRead.Unreadable;
                    if (verdictUnreadable) unreadablePatchStates++;

                    // An unparseable State leaves patchState at 0 (not-a-patch),
                    // which is the safe direction on purpose rather than luck: only
                    // a positively read Superseded (2) or Obsoleted (4) labels a row
                    // as one of those, and the label decides nothing but what is
                    // reported. IsRemovable is left at its default and is never set
                    // from here.
                    int.TryParse(stateStr, out var patchState);

                    var claimedPath = NormaliseLocalPackagePath(patchPath.Value);
                    MergeClaim(claimed,
                        new RegisteredPackage(claimedPath, productName, productCode, patchState,
                            VerdictUnreadable: verdictUnreadable),
                        ClaimSource.InstallerApi);
                    // Recorded whatever the verdict was. A claim that is Applied
                    // today is exactly the one that proves a path is still needed
                    // if a later re-read finds it, so filtering to the removable
                    // ones here would throw away the answers worth having.
                    patchClaims.Add(new PatchClaim(
                        claimedPath, patchCode, productCode, patchUserSid, (int)patchContext));
                }
            }

            if (recordsShort) unreadableProducts++;
        }

        progress?.Report(new ScanProgressUpdate(Strings.Status_CheckingRegistry));

        // READ BEFORE THE CONFIRMATION PASS RATHER THAN AFTER IT, because that
        // pass needs the products this enumeration missed and the registry is
        // where their names are. Nothing about the fallback's own answer moves
        // with the order: it claims paths through TryAdd and never displaces a
        // row, its unclaimed-file counts describe what the API LOOP claimed and
        // that loop has finished above, and the confirmation pass puts no new path
        // into the set, only downgrades rows already in it.
        var fallback = _readFallback(claimed, ct);

        // Even a fresh Windows install has OS-level MSI products. Zero
        // here means the database is corrupt or inaccessible; silently
        // reporting "all clear" would be worse than failing.
        if (claimed.Count == 0)
            throw new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty);

        var missed = LocateProductsTheEnumerationMissed(products, fallback.RegistryProductCodes, ct);

        ConfirmRemovableAgainstEveryProduct(claimed, patchClaims, products, missed.Recovered, ct);

        // Both sources degraded at once: refuse the scan outright rather than
        // report a shorter one.
        //
        // THIS GATE PROTECTS THE ORPHAN HALF AND IS THE ONE THING IN THIS REGION
        // THAT KEPT ITS SUBJECT WHEN THE REMOVABLE CLASS WENT. Read it before
        // concluding that unreadableProducts is now a dead term because the
        // withholding below cannot fire: what that count answers here is whether a
        // product's claim on a cached file exists anywhere at all, and a file no
        // source claims is offered as an ORPHAN.
        //
        // A short API enumeration alone is answered by the fallback, because the
        // paths the lost product would have claimed are still reachable: the
        // fallback reads the same UserData keys and contributes them as rows, so
        // the file stays out of the orphan list even though its owner went missing
        // from the API's answer.
        //
        // The moment the fallback is ALSO failing reads, that recovery is no
        // longer established. A product lost from the API whose UserData key was
        // one of the unreadable ones is claimed by neither source, and its cached
        // file is walked, matched against nothing, and offered as an orphan by a
        // scan whose own notice says orphaned files are not affected. The two
        // failures are not independent, either: the same corrupt registration
        // that loses an API row can equally make that product's UserData subtree
        // unreadable, so the backup is likeliest to be missing exactly the
        // product the primary lost. Neither counter can bound what the other
        // lost, so nothing here can be salvaged into a narrower rule.
        //
        // On any healthy machine both counters are zero and this is dead code.
        //
        // Keyed on what the API said about itself, never on the cross-check
        // below: this gate REFUSES, and a refusal must rest on a product the
        // enumeration itself reported it could not read. The cross-check infers
        // a loss from two counts that can differ for innocent reasons, which is
        // sound enough to withhold on and not to refuse on.
        if (unreadableProducts > 0 && fallback.Failures > 0)
            throw new LocalisedInvalidOperationException(Strings.Error_ScanRecordsUnreadable);

        // An enumeration that ends EARLY says nothing about itself: a
        // NoMoreItems at index 3 of 200 sets reachedEnd, leaves unreadableRows
        // at 0, and the scan reports itself complete while 197 products' patch
        // claims never reached the merge. The downgrade-only merge is what stops
        // a patch that is Superseded under one product and Applied under another
        // being offered, and it can only fire for a product the loop reached, so
        // a truncation puts a still-needed patch on the removal list under a
        // scan that believes it is whole.
        //
        // THE QUESTION IS SETTLED BY IDENTITY, ABOVE, AND NOT BY ARITHMETIC HERE.
        // LocateProductsTheEnumerationMissed compares the product codes the
        // registry holds against the codes the enumeration returned, and puts each
        // difference to Windows as a question about that one product. So a
        // truncation is not estimated from how far two totals disagree; the
        // products behind the disagreement are named, and each is either recovered
        // into the questions the confirmation pass asks, or shown not to be
        // installed, or counted in missed.Unresolved because Windows would not
        // say. Only the last of the three withholds anything.
        //
        // WHY A LEFTOVER KEY NOW PROVES NOTHING. A UserData product key outlives
        // a failed or partial uninstall, so the registry legitimately holds more
        // keys than the machine has products, and against a TOTAL that residue is
        // indistinguishable from a truncation: both read as the registry running
        // ahead. That is the whole reason a total ever needed a tolerance, and any
        // tolerance is a guess at how much residue is normal. Asked by name, the
        // same key answers "not installed", which settles it outright and costs
        // nothing, because a product that is not there holds no patches. Residue
        // no longer has to be absorbed, so nothing has to decide how much of it to
        // absorb.
        //
        // AND WHERE THE REGISTRY READ ITSELF FAILS, NOTHING HAS BEEN GIVEN UP.
        // This is the case to check before concluding an inference was safer than
        // a measurement, because a headcount looks as though it would survive it.
        // It does not: ProductKeys is counted from the subkeys the fallback
        // actually walked, so a fallback that failed reports FEWER keys, which
        // shrinks the difference against the enumeration rather than widening it.
        // A total is blinded by exactly the failure that empties the comparison,
        // and blinded quietly, reading as a machine whose two sources agree. What
        // is keyed on that state is the both-sources gate above, which weighs the
        // fallback's own failure count and refuses.
        //
        // What remains here is an OBSERVATION and not an estimate, which is why it
        // stays. The fallback reads the same UserData keys the API read and runs
        // after the whole API loop, so a path it is the FIRST to claim is one no
        // product the loop reached ever named. Its file being on the disk is the
        // other half: a residue key whose product is gone but whose LocalPackage
        // value survives leaves an unclaimed path too, and that population's file
        // is usually not there. It overlaps the comparison on a machine where both
        // fire, and the redundancy is worth its cost: this one sees a lost product
        // through a file on the disk rather than through a code, so it does not
        // depend on any key name being a packed GUID this code can read.
        //
        // A product whose row the API skipped, or whose LocalPackage read failed,
        // has its registry value claimed by the fallback alone, so it is already
        // inside unreadableProducts. Subtracting the whole of that count is
        // deliberately generous (a product short only a patch row contributes no
        // unclaimed path), which can leave the NUMBER low and cannot leave the
        // withholding off: whatever it absorbs, unreadableProducts carries.
        //
        // A patch entry names no product, so it can say only that at least one
        // went unreached. It floors the count rather than adding to it.
        var unclaimedProducts = Math.Max(0, fallback.UnclaimedProductFiles - unreadableProducts);
        var apiNeverClaimed = fallback.UnclaimedPatchFiles > 0
            ? Math.Max(1, unclaimedProducts)
            : unclaimedProducts;

        // Registry products this scan could not settle either way: a code Windows
        // would not answer about, and a key whose name yielded no code to ask
        // with. Two steps of one state, so one figure.
        var unresolvedProducts = missed.Unresolved + fallback.UnparseableProductKeyNames;

        // ADDED rather than weighed against the observation, because the two are
        // not estimates of one quantity: the observation counts products seen to
        // have gone unclaimed, and this counts the ones the question got no answer
        // for. A product RECOVERED by name contributes to neither, which is the
        // whole gain: the gap it would have been part of was closed by asking
        // rather than covered by withholding.
        var withheldProducts = unreadableProducts + apiNeverClaimed + unresolvedProducts;

        progress?.Report(new ScanProgressUpdate(string.Format(
            Helpers.DisplayHelpers.Pluralise(claimed.Count, Strings.Status_RegisteredPackagesFound, "Status.RegisteredPackagesFound"),
            claimed.Count, Helpers.DisplayHelpers.PluralisePackage(claimed.Count))));

        var packages = claimed.Values.ToList();

        // DEAD FROM 3.0.0 AND DELIBERATELY LEFT STANDING. No row reaches here
        // carrying IsRemovable, so the loop runs over no rows and RemovableWithheld
        // is never set on a real scan. It is kept because it is the mechanism the
        // superseded class would need if it were ever offered again, and because
        // deleting the machinery that made that class as safe as it was is a
        // decision about the product rather than a tidy-up.
        //
        // NOT TO BE CONFUSED WITH THE REFUSAL GATE ABOVE, which weighs the same
        // count and is very much alive; see its own note for why.
        //
        // What it did: a scan that lost any claim withheld the whole removable
        // class. "Removable" asserts that NO installed product still needs the
        // file, and a product set known to be short of at least one claim cannot
        // support that assertion for any patch on the machine: the product behind
        // the loss is exactly the one whose "I still have this applied" claim never
        // reached the merge, and a patch is cached once and shared across the
        // products that hold it.
        //
        // Nothing finer is sound. A failed row's product code is undefined (the
        // API documents its output buffers for ERROR_SUCCESS and ERROR_MORE_DATA
        // only) and the loop clears the buffer per iteration, so the missing
        // product's identity is unknowable, and with it the set of patches it
        // could still be holding. A failed LocalPackage read names its product
        // but not the path it would have claimed, which is the half that matters:
        // the lost claim could be on any cached file, so knowing who lost it
        // narrows nothing. Scan-wide is the finest granularity the information
        // supports either way.
        //
        // Only the removable class moves, and the cost is bounded by that: this
        // withholds superseded-patch cleanup, not orphan cleanup, and only on a
        // scan that lost a row or is short against the registry's own count.
        if (withheldProducts > 0)
            for (var i = 0; i < packages.Count; i++)
                if (packages[i].IsRemovable)
                    packages[i] = packages[i] with { IsRemovable = false, RemovableWithheld = true };

        return new InstallerQueryResult(packages.AsReadOnly(), withheldProducts, patchClaims.AsReadOnly(),
            // The tallies rather than the term computed from them: the
            // never-claimed figure is floored and biased low, so it is not the
            // count its name would claim, and it is reproducible from these.
            new EnumerationCensus(
                unreadableProducts,
                unreadableRows,
                fallback.ProductKeys,
                fallback.UnclaimedProductFiles,
                fallback.UnclaimedPatchFiles,
                fallback.NonStringLocalPackageValues,
                unreadablePatchStates,
                products.Count,
                patchClaims.Count,
                packages.Count(p => HasLongLeafStem(p.LocalPackagePath)),
                missed.Recovered.Count,
                // The two halves of unresolvedProducts, apart. The arithmetic
                // above adds them because it needs what could not be settled, and
                // that superordinate is true of both; no narrower sentence is, so
                // nothing that names a cause may carry the sum.
                missed.Unresolved,
                fallback.UnparseableProductKeyNames,
                // Counted off the merged rows rather than at the read site, which
                // is what makes it a different number from the pairing count
                // above: several products' failed reads on one shared patch are
                // one row here and several there.
                packages.Count(p => p.VerdictUnreadable)));
        }
        finally
        {
            abandonedLog.WriteClosingEntry();
        }
    }

    /// <summary>
    /// Which installed products the product enumeration did not return, asked as a
    /// question about named products rather than inferred from two headcounts.
    ///
    /// THE REGISTRY NAMES THE MACHINE'S PRODUCTS AND SO DOES THE ENUMERATION, so a
    /// code the first holds and the second never returned is not evidence that
    /// something was missed; it is the thing that was missed, identified. Each one
    /// is then put to Windows on its own (<see cref="ResolveProductInstance"/>,
    /// which asks about that code and walks no list), and the answer decides which
    /// of three quite different states this is:
    ///
    /// INSTALLED. The enumeration was short and this product is why. It is
    /// recovered into the confirmation pass's ask list, where it answers for the
    /// patches it holds exactly as an enumerated product would. Nothing is withheld
    /// for it, because nothing needed to be: the gap was closed rather than
    /// estimated.
    ///
    /// NOT INSTALLED. A UserData key outliving its product, which is the ordinary
    /// residue of a failed or partial uninstall and the reason a headcount needed a
    /// tolerance in the first place. It establishes nothing and costs nothing. This
    /// is where the difference between comparing names and comparing counts is
    /// worth the most: a count cannot tell this state from the one above, and every
    /// tolerance band that has ever been written here exists to guess at the
    /// proportion of them.
    ///
    /// UNASKABLE. The registry names a product and Windows would not say whether it
    /// is installed. Nothing about the enumeration's completeness can be
    /// established, so the caller withholds; see <paramref name="registryCodes"/>
    /// for the one other way this method reports the same not-knowing.
    /// </summary>
    /// <param name="registryCodes">
    /// Null where no fallback ran, which is not the same as an empty set and must
    /// not read as one: an empty set says the registry holds no product this
    /// enumeration missed, and null says nobody looked. Null yields no recovered
    /// products and no unresolved ones, leaving the caller's other signals to
    /// speak, because a comparison that did not happen may not withhold on its own
    /// silence.
    /// </param>
    /// <returns>
    /// The products to ask alongside the enumerated ones, and how many codes could
    /// not be resolved either way. The second is a count and not a list on purpose:
    /// there is nothing to be done with the identity of a product Windows will not
    /// answer about, and the count is what the withholding needs.
    /// </returns>
    private (List<(string ProductCode, string? Sid, MsiInstallContext Context)> Recovered, int Unresolved)
        LocateProductsTheEnumerationMissed(
            List<(string ProductCode, string? UserSid, MsiInstallContext Context)> products,
            IReadOnlyCollection<string>? registryCodes,
            CancellationToken ct)
    {
        var recovered = new List<(string, string?, MsiInstallContext)>();
        if (registryCodes is null || registryCodes.Count == 0) return (recovered, 0);

        var enumerated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, _, _) in products) enumerated.Add(code);

        // Unbounded by design, and the restraint is deliberate rather than an
        // oversight: one keyed read per code the enumeration did not return, on a
        // set already bounded by the machine's own registry keys, which the
        // fallback has just opened one at a time anyway. Capping it would put back
        // exactly the kind of unjustified number this comparison exists to remove,
        // and the cap would fall on the machines with the most to recover.
        var unresolved = 0;
        foreach (var code in registryCodes)
        {
            ct.ThrowIfCancellationRequested();
            if (enumerated.Contains(code)) continue;

            var resolved = ResolveProductInstance(code);
            if (resolved.Unaskable) { unresolved++; continue; }
            if (resolved.Installed) recovered.Add((code, resolved.Sid, resolved.Context));
        }

        return (recovered, unresolved);
    }

    /// <summary>
    /// Re-establishes every removable verdict by ASKING each enumerated product
    /// about the patch, instead of inferring it from each product's patch list
    /// having come back whole.
    ///
    /// WHAT IT CLOSES, and it is not the mis-spelling class. A cached patch is
    /// claimed once and shared by every product holding it, and the merge is
    /// downgrade-only, so a patch that is Superseded under one product and
    /// Applied under another stays non-removable ONLY IF the Applied row reaches
    /// the merge. That row reaches it through the second product's patch
    /// enumeration, and an enumeration that returns ERROR_NO_MORE_ITEMS early is
    /// indistinguishable from one that finished: <see cref="EnumeratePatches"/>
    /// treats it as a clean end at any index, so nothing is marked incomplete,
    /// no product is counted unreadable, and the scan-wide withholding never
    /// runs.
    ///
    /// NOTHING ELSE CATCHES IT, which is why this exists rather than a counter.
    /// The registry fallback recovers lost PATHS and never lost VERDICTS, and its
    /// unclaimed-patch signal counts only paths it was FIRST to claim, which this
    /// path is not: the first product already claimed it, removable. The product
    /// headcount is untouched because the second product WAS enumerated and only
    /// its patch list was short. And the act-time re-reads cannot see it either,
    /// both of them working from what this enumeration produced: the full
    /// re-verify re-runs the same enumeration, and the under-lease re-read asks
    /// only the claims that were collected, which do not include the one that
    /// never happened.
    ///
    /// THE QUESTION IS KEYED, WHICH IS THE WHOLE POINT. <c>MsiGetPatchInfoEx</c>
    /// takes a patch and a product and walks no list, so a product that holds the
    /// patch answers whether or not its enumeration would have named it. Asking
    /// every enumerated product means the answer does not depend on any
    /// enumeration having been complete.
    ///
    /// WHAT IT COSTS, stated because it is the one thing here that scales with
    /// the machine rather than with the fault: enumerated products multiplied by
    /// removable candidates. Most pairings are settled by a single property read
    /// returning ERROR_UNKNOWN_PATCH, and a machine with nothing removable pays
    /// nothing at all, the method returning before it asks anything.
    ///
    /// The two outcomes use the two meanings the row already has, so this adds no
    /// vocabulary. A product that holds the patch and still needs it makes the row
    /// plainly non-removable, exactly as the merge's own downgrade does. A read
    /// that could not answer makes it non-removable AND withheld, which is the
    /// existing "this scan could not prove it" state, counted and surfaced as such.
    ///
    /// IT RETURNS AT ITS FIRST GUARD FROM 3.0.0, no row carrying a removable
    /// verdict for it to confirm, so it asks nothing and costs nothing. Left
    /// standing for the reason the withholding loop above is: it is what made the
    /// class as safe as it was and what the class would need again. Do not read
    /// its emptiness as evidence that the questions it asks were unnecessary.
    /// </summary>
    /// <param name="recovered">
    /// Products the enumeration never returned and the registry comparison then
    /// found installed (<see cref="LocateProductsTheEnumerationMissed"/>). They are
    /// asked exactly as enumerated products are, which is the point: a product
    /// recovered by name can answer for the patches it holds, where a product
    /// merely inferred from a headcount could only ever have withheld.
    /// </param>
    /// <remarks>
    /// INTERNAL RATHER THAN PRIVATE SO ITS TESTS CAN REACH IT, which is the same
    /// reason <see cref="IsRemovablePatch"/> and <see cref="MergeClaim"/> are.
    /// Nothing reaches it through the enumeration any more, so a test driving the
    /// enumeration can no longer exercise it at all, and the alternative to a seam
    /// was letting the coverage go: preserved-but-untested machinery is preserved
    /// in name only. There is no production switch that re-grants a removable
    /// verdict and there must never be one; a flag in a shipped binary that turns
    /// this class back on is the thing the release exists to prevent.
    /// </remarks>
    internal void ConfirmRemovableAgainstEveryProduct(
        Dictionary<string, RegisteredPackage> claimed,
        List<PatchClaim> patchClaims,
        List<(string ProductCode, string? UserSid, MsiInstallContext Context)> products,
        List<(string ProductCode, string? Sid, MsiInstallContext Context)> recovered,
        CancellationToken ct)
    {
        // EVERY patch code naming a still-removable path, not one per path. The
        // merged row carries no patch code, so the codes come from the claims,
        // and a path can legitimately be named by more than one of them: the
        // claims are collected per claim precisely because several products claim
        // one file, and a corrupt LocalPackage can aim a patch row at a file that
        // is not that patch's at all. Keeping one code per path would confirm one
        // of them and clear the file on its answer.
        var toConfirm = new HashSet<(string Path, string PatchCode)>();
        foreach (var claim in patchClaims)
            if (claimed.TryGetValue(claim.LocalPackagePath, out var row) && row.IsRemovable)
                toConfirm.Add((claim.LocalPackagePath, claim.PatchCode));

        if (toConfirm.Count == 0) return;

        // The pairings the product loop already read. Re-asking them would get the
        // same answer for the same reason, so they are skipped: what this pass is
        // for is the pairings no enumeration produced.
        var alreadyAsked = new HashSet<(string, string)>();
        foreach (var claim in patchClaims)
            alreadyAsked.Add((claim.PatchCode, claim.ProductCode));

        // ROUTE A. Every (patch, product) pairing the API will name when asked
        // about no product in particular, which is the only way to hear about a
        // product the product enumeration never returned. Null where it did not
        // run to a clean end, and that withholds rather than reading as nothing
        // to report: an enumeration that came back empty because it refused,
        // taken as an answer, is the exact fault this whole pass exists to close.
        var holders = EnumeratePatchHoldersAcrossAllProducts(ct);

        foreach (var (path, patchCode) in toConfirm)
        {
            ct.ThrowIfCancellationRequested();

            // A path another code has already settled needs no second pass: the
            // verdict is gone and cannot come back, downgrades being one-way.
            if (!claimed.TryGetValue(path, out var current) || !current.IsRemovable) continue;

            if (holders is null)
            {
                Downgrade(claimed, path, withheld: true);
                continue;
            }

            // The products to put the question to: the ones the enumeration
            // returned, the ones the registry named and the enumeration did not,
            // plus any route A named for this patch, plus any the patch file
            // itself says it targets. They overlap heavily on a healthy machine
            // and are unioned rather than chosen between, because each sees
            // something the others cannot and every one of them can only add a
            // product to ask.
            var toAsk = new List<(string ProductCode, string? Sid, MsiInstallContext Context)>(products);
            toAsk.AddRange(recovered);
            if (holders.TryGetValue(patchCode, out var named)) toAsk.AddRange(named);

            var fromFile = TargetsDeclaredByPatchFile(path, out var fileUnreadable);
            if (fileUnreadable)
            {
                Downgrade(claimed, path, withheld: true);
                continue;
            }
            foreach (var target in fromFile)
            {
                var resolved = ResolveProductInstance(target);
                if (resolved.Unaskable)
                {
                    Downgrade(claimed, path, withheld: true);
                    break;
                }
                if (resolved.Installed)
                    toAsk.Add((target, resolved.Sid, resolved.Context));
            }
            if (!claimed.TryGetValue(path, out current) || !current.IsRemovable) continue;

            foreach (var (productCode, userSid, context) in toAsk)
            {
                if (alreadyAsked.Contains((patchCode, productCode))) continue;

                ct.ThrowIfCancellationRequested();

                // State first and alone where it settles the pairing. A product
                // that does not hold this patch answers ERROR_UNKNOWN_PATCH to
                // the sizing call, so the overwhelming majority of pairings cost
                // one property read and the second is never made.
                var state = GetPatchProperty(_msi, patchCode, productCode, userSid, context,
                    MsiInstallProperty.State);

                // Not registered against this product: a positive answer that
                // this product does not hold the patch, so it says nothing about
                // the verdict either way.
                if (state.NotRegistered) continue;

                if (state.Unreadable)
                {
                    Downgrade(claimed, path, withheld: true);
                    break;
                }

                var uninstallable = GetPatchProperty(_msi, patchCode, productCode, userSid, context,
                    MsiInstallProperty.Uninstallable);
                if (uninstallable.NotRegistered) continue;
                if (uninstallable.Unreadable)
                {
                    Downgrade(claimed, path, withheld: true);
                    break;
                }

                if (!IsRemovablePatch(state.Value, uninstallable.Value))
                {
                    // This product holds the patch and has not shown it
                    // removable, which is the claim the truncated enumeration
                    // would have contributed. Same verdict, reached by asking.
                    Downgrade(claimed, path, withheld: false);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// ROUTE A. Every patch the machine holds, mapped to the products holding it,
    /// by asking the API about no product in particular.
    ///
    /// <c>MsiEnumPatchesEx</c> documents a null <c>szProductCode</c> as "the
    /// patches for all products under the specified context are enumerated", and
    /// hands back the target product's own code, context and SID on every row. So
    /// it is the one call that can name a product the PRODUCT enumeration never
    /// returned, which is the whole reason it is here: a product missing from
    /// that list cannot be asked about a patch, and the pass that confirms a
    /// removable verdict would otherwise be blind to exactly the registration
    /// that would overturn it.
    ///
    /// IT HAS A DOCUMENTED BLIND SPOT AND IT IS WHY THIS IS NOT THE ONLY ROUTE.
    /// In the per-user-unmanaged context, "only patches installed with Windows
    /// Installer version 3.0 are enumerated for users that are not the current
    /// user". A patch applied under another account by an installer older than
    /// that is invisible here, whatever else is working. The patch file's own
    /// declared targets are read alongside for that reason, and the keyed reads
    /// both routes feed carry no such limitation: the administrator group may
    /// query patch data for any product instance and any user on the computer.
    ///
    /// NULL MEANS THE ANSWER IS NOT AVAILABLE AND EVERY REMOVABLE VERDICT IS
    /// WITHHELD, which is deliberate and is the more expensive direction. A short
    /// or refused enumeration read as "no other product holds it" is the fault
    /// this pass exists to close, so nothing here distinguishes a refusal from an
    /// empty machine.
    /// </summary>
    private Dictionary<string, List<(string ProductCode, string? Sid, MsiInstallContext Context)>>?
        EnumeratePatchHoldersAcrossAllProducts(CancellationToken ct)
    {
        var holders = new Dictionary<string, List<(string, string?, MsiInstallContext)>>(
            StringComparer.OrdinalIgnoreCase);
        var patchCode = new char[Msi.GuidBufferLength];
        var targetProductCode = new char[Msi.GuidBufferLength];
        var sidBuffer = new char[SidBufferLength];

        for (uint index = 0; index < MaxPatchIndex; index++)
        {
            ct.ThrowIfCancellationRequested();

            Array.Clear(patchCode);
            Array.Clear(targetProductCode);
            uint sidLength = SidBufferLength;

            var error = _msi.EnumPatches(
                productCode: null,
                userSid: AllUsersSid,
                context: MsiInstallContext.All,
                filter: MsiPatchFilter.All,
                index: index,
                patchCode: patchCode,
                targetProductCode: targetProductCode,
                targetProductContext: out var targetContext,
                targetUserSid: sidBuffer,
                targetUserSidLength: ref sidLength);

            if (error == MsiError.MoreData)
            {
                // The SID did not fit. Documented as the count excluding the
                // terminator, so the retry is that plus one.
                sidLength++;
                sidBuffer = new char[sidLength];
                error = _msi.EnumPatches(
                    productCode: null,
                    userSid: AllUsersSid,
                    context: MsiInstallContext.All,
                    filter: MsiPatchFilter.All,
                    index: index,
                    patchCode: patchCode,
                    targetProductCode: targetProductCode,
                    targetProductContext: out targetContext,
                    targetUserSid: sidBuffer,
                    targetUserSidLength: ref sidLength);
            }

            if (error == MsiError.NoMoreItems) return holders;

            // Every documented failure return lands here: access denied, corrupt
            // configuration, an invalid parameter, an unknown product. None of
            // them is an answer, and a set short by an unknown amount is a veto
            // that does not fire.
            if (error != MsiError.Success) return null;

            var code = BufferToString(patchCode);
            var target = BufferToString(targetProductCode);
            if (code.Length == 0 || target.Length == 0)
            {
                // A success that named nothing. It cannot be used and it cannot
                // be shown to be harmless, so it is treated as the row that was
                // missed rather than skipped.
                return null;
            }

            var safeSidLength = (int)Math.Min(sidLength, (uint)sidBuffer.Length);
            var sid = (targetContext != MsiInstallContext.Machine && safeSidLength > 0)
                ? new string(sidBuffer, 0, safeSidLength)
                : null;

            if (!holders.TryGetValue(code, out var list))
                holders[code] = list = new List<(string, string?, MsiInstallContext)>();
            list.Add((target, sid, targetContext));
        }

        // Ran out of budget rather than reaching the end, so the map is short for
        // the same reason a refusal makes it short.
        return null;
    }

    /// <summary>
    /// ROUTE B. The product codes a cached patch says in its own Template that it
    /// may be applied to.
    ///
    /// It is read from the FILE, so it does not care what any enumeration
    /// returned, which is what makes it the answer to route A's documented blind
    /// spot. Its own limit is the other way round: it names the products the
    /// patch may target rather than the products that hold it, and the evidence
    /// that the Template is complete for this purpose is two files on one
    /// machine, which is the thinnest thing either route stands on.
    /// </summary>
    /// <param name="unreadable">
    /// True where the file exists and would not yield an identity. A patch whose
    /// own declaration cannot be read has not been shown to be unneeded by
    /// anybody, so the caller withholds rather than proceeding on the other two
    /// routes alone. An absent file is not unreadable: there is nothing there to
    /// remove and nothing to withhold.
    /// </param>
    private IReadOnlyList<string> TargetsDeclaredByPatchFile(string path, out bool unreadable)
    {
        unreadable = false;

        // Only a patch has a Template to read. A cached product package is not
        // this route's business and its absence of one is not a failure.
        if (!path.EndsWith(".msp", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<string>();

        // A file that is not there is left to the read below rather than tested
        // for, and the outcome is the same either way: a row that is withheld and
        // whose file has gone, and a row that is removable and whose file has
        // gone, both reach the scan's benign missing-removable count and neither
        // is offered or reported. Testing for it here would put a real-filesystem
        // question inside a decision that has no other, for no difference anybody
        // can observe.
        var identity = _identityReader.Read(path, isPatch: true, out _);
        if (identity is null)
        {
            unreadable = true;
            return Array.Empty<string>();
        }

        return identity.Value.TargetProductCodes;
    }

    /// <summary>
    /// Where one product code is installed, asked about that code alone.
    ///
    /// Route B yields a product code and nothing else, and a keyed patch read
    /// needs the account and context the instance lives in. The filtered product
    /// enumeration answers exactly that for a single code and stops at the first
    /// row, so it is a question about one product rather than a walk of the
    /// machine's list.
    /// </summary>
    /// <returns>
    /// <c>Installed</c> with the instance's account and context; or neither flag,
    /// meaning the code is positively not installed, which is a clean answer
    /// because a product that is not there holds no patches; or
    /// <c>Unaskable</c>, which withholds. Which returns say "not installed" is
    /// <see cref="IsProductNotInstalled"/>'s, and there is more than one of them.
    /// </returns>
    private (bool Installed, bool Unaskable, string? Sid, MsiInstallContext Context)
        ResolveProductInstance(string productCode)
    {
        var installedCode = new char[Msi.GuidBufferLength];
        var sidBuffer = new char[SidBufferLength];
        uint sidLength = SidBufferLength;

        var error = _msi.EnumProducts(
            productCode: productCode,
            userSid: AllUsersSid,
            context: MsiInstallContext.All,
            index: 0,
            installedProductCode: installedCode,
            installedContext: out var context,
            sid: sidBuffer,
            sidLength: ref sidLength);

        if (error == MsiError.MoreData)
        {
            sidLength++;
            sidBuffer = new char[sidLength];
            error = _msi.EnumProducts(
                productCode: productCode,
                userSid: AllUsersSid,
                context: MsiInstallContext.All,
                index: 0,
                installedProductCode: installedCode,
                installedContext: out context,
                sid: sidBuffer,
                sidLength: ref sidLength);
        }

        if (IsProductNotInstalled(error)) return (false, false, null, default);
        if (error != MsiError.Success) return (false, true, null, default);

        var safeSidLength = (int)Math.Min(sidLength, (uint)sidBuffer.Length);
        var sid = (context != MsiInstallContext.Machine && safeSidLength > 0)
            ? new string(sidBuffer, 0, safeSidLength)
            : null;
        return (true, false, sid, context);
    }

    /// <summary>
    /// Takes one path's removable verdict away. <paramref name="withheld"/>
    /// separates the two reasons, because they are not the same thing to have
    /// found out and the flag is what the rest of the app reads to tell them
    /// apart: false is a product's live claim on the file, true is a read that
    /// established nothing.
    /// </summary>
    private static void Downgrade(
        Dictionary<string, RegisteredPackage> claimed, string path, bool withheld)
    {
        if (!claimed.TryGetValue(path, out var row) || !row.IsRemovable) return;
        claimed[path] = row with { IsRemovable = false, RemovableWithheld = withheld };
    }

    /// <summary>
    /// Puts a LocalPackage value into the one spelling the folder walk produces,
    /// before it becomes a claim.
    ///
    /// Orphanhood is decided by string equality between these values and the
    /// paths the walk enumerates, while existence is decided by the filesystem,
    /// so any spelling Windows can hand back and the walk never produces splits
    /// one file into two answers: registered-and-present on this side, and
    /// unclaimed-therefore-orphaned on the other. Doubled separators, forward
    /// slashes, a relative segment, a trailing space or dot, and the <c>\\?\</c>
    /// and <c>\??\</c> prefixes over a drive letter are all such spellings, and
    /// all of them survive into the registry because nothing writing there is
    /// obliged to canonicalise. GetFullPath settles every one; the prefix comes
    /// off first because GetFullPath deliberately leaves a <c>\\?\</c> path
    /// alone, that being the point of the prefix, and reads the <c>\??\</c>
    /// form's leading separator as rooted on whatever drive the process is
    /// running from. Which prefixes come off, and why one is left on, is
    /// <see cref="InstallerCacheHelpers.StripLongPathPrefix"/>'s.
    ///
    /// This is also the string a removable candidate is later moved or deleted
    /// by (FileSystemScanService builds the candidate straight off it), which is
    /// the right direction: the normalised form names the same file and names it
    /// the way the rest of the app spells it.
    ///
    /// TWO SPELLINGS SURVIVE GetFullPath, BECAUSE NEITHER IS DECIDABLE FROM THE
    /// STRING. Windows Installer names the files it caches itself, as short hex
    /// (<c>9f05cba.msi</c>, <c>1e4a2f.msp</c>), so the FILENAME cannot have a
    /// short form that differs; the path also carries the folder, and
    /// <c>Installer</c> is nine characters, so on a volume still creating 8dot3
    /// aliases the folder has a short form of its own and
    /// <c>C:\Windows\INSTAL~1\1a2b3c.msi</c> names an ordinary file a product
    /// still needs. A volume-GUID path is the other, keeping its prefix for the
    /// reason <see cref="InstallerCacheHelpers.StripLongPathPrefix"/> gives.
    /// Neither matches the walk, and the short form is the worse of the two: it
    /// answers true to File.Exists, so the row counts as a registered file found
    /// on disk and the scan's correlation gate reads a healthy machine.
    ///
    /// Both are settled by asking the filesystem what the path really is, which
    /// is what <see cref="InstallerCacheHelpers.TryResolveFinalPath"/> already
    /// does at every containment gate. The only open question was the cost of
    /// asking once per registration, and it is not asked once per registration:
    /// both spellings announce themselves in the string, so
    /// <see cref="NeedsFinalPathResolution"/> decides on a character scan and a
    /// handle is opened only for a path carrying one. A machine holding neither
    /// pays the scan and no I/O.
    ///
    /// THE PREFIX IS NORMALISED BEFORE THE ASK, and that is not tidying. The NT
    /// object form (<c>\??\</c>) and the Win32 escape (<c>\\?\</c>) name the same
    /// object, which is why StripLongPathPrefix takes either off a drive-rooted
    /// path; over a volume GUID neither comes off, and the NT form then has its
    /// leading separator read as rooted on whatever drive the process is running
    /// from. Handing the resolver the Win32 spelling is what stops the resolution
    /// answering about a path assembled out of the running process's location.
    ///
    /// WHAT THIS DOES NOT DO, because a comment claiming a closed hole is worse
    /// than none: a flagged path the kernel declines to expand is kept in the
    /// spelling Windows gave, so its claim still fails to match the walk and its
    /// file is still offered. That is what happened before any resolution existed
    /// and is no worse; what is new is only that the residue is a claim known to
    /// be unspellable rather than one nobody asked about, with nothing downstream
    /// told about it.
    ///
    /// Measured on one elevated machine (Windows 10.0.26200, 2026-08-03): 138
    /// registered paths, every one an ordinary drive path, no tilde-and-digit
    /// anywhere in any of them, and the cache folder had no short name on that
    /// volume. That says how exposed one machine was, and nothing about whether
    /// another holds one.
    /// </summary>
    private static string NormaliseLocalPackagePath(string value)
    {
        try
        {
            var stripped = InstallerCacheHelpers.StripLongPathPrefix(value);

            // The test runs on the stripped value rather than the fully
            // normalised one because GetFullPath destroys the evidence it needs:
            // a prefix it cannot root is folded into an ordinary-looking path,
            // and a trigger that has been normalised away cannot be tested for.
            //
            // Only a proven expansion is taken. A false return means the kernel
            // never expanded this path, so its out value is the same string by
            // another route and using it would dress a guess as an answer.
            if (NeedsFinalPathResolution(stripped)
                && InstallerCacheHelpers.TryResolveFinalPath(ToWin32Prefix(stripped), out var resolved))
            {
                return resolved;
            }

            return Path.GetFullPath(stripped);
        }
        catch
        {
            // A value GetFullPath refuses (an embedded null, a device name, a
            // length past the API's limit) is kept exactly as Windows returned
            // it. It cannot be improved, and dropping the claim would turn an
            // unreadable spelling into an orphaned file.
            return value;
        }
    }

    /// <summary>
    /// Whether a prefix-stripped path carries a spelling only the filesystem can
    /// settle. This is what keeps the resolution off the ordinary path: a handle
    /// is opened for a registration answering true here and for no other.
    ///
    /// A tilde followed by a digit is the 8.3 alias form. A surviving prefix,
    /// either form, is what <see cref="InstallerCacheHelpers.StripLongPathPrefix"/>
    /// leaves on a path with no drive root, which in this position means a
    /// volume-GUID or device path.
    ///
    /// It over-selects deliberately. A long name may legitimately hold a
    /// tilde-and-digit, and a false positive costs one handle open on a path
    /// that resolves to itself; a false negative costs a file.
    /// </summary>
    internal static bool NeedsFinalPathResolution(string path)
    {
        if (path.StartsWith(@"\\?\", StringComparison.Ordinal)
            || path.StartsWith(@"\??\", StringComparison.Ordinal)) return true;

        for (var i = 0; i < path.Length - 1; i++)
        {
            if (path[i] == '~' && char.IsAsciiDigit(path[i + 1])) return true;
        }

        return false;
    }

    /// <summary>
    /// Where a claim on a cached file's path came from. The two sources carry
    /// different authority and <see cref="MergeClaim"/> is the only place that
    /// difference is expressed.
    /// </summary>
    internal enum ClaimSource
    {
        /// <summary>
        /// A product row or a patch row from the Windows Installer API. The API
        /// is authoritative about what a file IS: whose package it is, and, for
        /// a patch, its state under each product that holds it.
        /// </summary>
        InstallerApi,

        /// <summary>
        /// A LocalPackage value read straight out of the UserData registry keys.
        /// Presence-only: it establishes that some registration names the path
        /// and nothing else, having no state to read a verdict from.
        /// </summary>
        RegistryFallback,
    }

    /// <summary>
    /// The single insertion policy for <paramref name="claimed"/>: every claim on
    /// a path runs through here, so what a second claim does is one function
    /// rather than a rule per call site.
    ///
    /// An API claim moves a path towards non-removable and never away from it.
    /// A patch is cached once per code but its State is per product, so one .msp
    /// can be Superseded (removable) under one product and Applied (still
    /// needed) under another, and a corrupt LocalPackage can aim a patch row at
    /// a product's own cached .msi. First-writer-wins decided both on a coin
    /// flip of enumeration order. Under this policy, once anything claims a path
    /// non-removable it stays non-removable, and an existing removable row is
    /// downgraded by a later non-removable claim; the verdict is never upgraded
    /// the other way.
    ///
    /// THE SAME COIN FLIP REACHES THE CAUSE AS WELL AS THE VERDICT, which is why
    /// there is a second rule rather than one. Two non-removable claims on a path
    /// are not necessarily the same finding: one product's Applied claim names the
    /// file, and another product's failed State read names nothing at all. Keeping
    /// whichever the enumeration reached first would make what the app SAYS about
    /// that file depend on enumeration order, which is the fault the rule above
    /// closes for what the app DOES. So a claim that establishes something
    /// displaces a row that establishes nothing, and never the reverse.
    ///
    /// A fallback claim can only ADD a path, never displace the row on one. That
    /// scoping is load-bearing, not a layering preference. The fallback reads the
    /// same UserData keys the API read and runs after the whole API loop, so every
    /// removable patch already has a fallback row waiting for its own path, and
    /// every fallback row is non-removable by construction (RegisteredPackage
    /// defaults IsRemovable to false, and a fallback row has no State to set it
    /// from). Letting a fallback claim downgrade would therefore walk in behind
    /// the API and strip the removable verdict off every superseded patch it had
    /// just correctly identified: superseded-patch detection would return nothing,
    /// on every machine, for as long as the change stood.
    /// </summary>
    /// <returns>
    /// True where this call put a path into <paramref name="claimed"/> that was
    /// not there before. For a fallback claim that is the whole signal the
    /// cross-check in <see cref="GetRegisteredPackagesCore"/> keys on, the
    /// scoping above being what makes it mean anything: the fallback runs after
    /// the whole API loop over the same UserData keys, so a path it is the first
    /// to claim is one the API never claimed rather than one it saw first.
    /// </returns>
    internal static bool MergeClaim(
        Dictionary<string, RegisteredPackage> claimed,
        RegisteredPackage candidate,
        ClaimSource source)
    {
        if (source == ClaimSource.RegistryFallback)
            return claimed.TryAdd(candidate.LocalPackagePath, candidate);

        if (!claimed.TryGetValue(candidate.LocalPackagePath, out var existing))
        {
            claimed[candidate.LocalPackagePath] = candidate;
            return true;
        }

        // Downgrade only: a removable row loses to a later non-removable claim,
        // and nothing else moves.
        if (existing.IsRemovable && !candidate.IsRemovable)
        {
            claimed[candidate.LocalPackagePath] = candidate;
            return false;
        }

        // Both are non-removable and only one of them is a finding. The
        // IsRemovable test is what stops this reading as an upgrade: a removable
        // candidate never displaces anything here, so the row can only move from
        // "nothing was established" to "this product claims it", which is the
        // direction that costs no file and gains a true sentence.
        if (existing.VerdictUnreadable && !candidate.VerdictUnreadable && !candidate.IsRemovable)
            claimed[candidate.LocalPackagePath] = candidate;

        return false;
    }

    /// <summary>
    /// The real registry fallback: every SID subtree under UserData, read into
    /// <paramref name="claimed"/>, returning how many key reads failed.
    ///
    /// Registry64 is pinned explicitly. Registry.LocalMachine resolves to the
    /// process-bitness view, which redirects to WOW6432Node under an x86 process
    /// and silently misses installer-cache entries written by 64-bit installers.
    /// Pinning to Registry64 keeps the fallback path correct regardless of host
    /// bitness.
    ///
    /// The per-SID and per-key try/catch is deliberate and must not be collapsed
    /// back into one outer try: this fallback is the second of the app's two
    /// independent "still needed" sources, and a single try spanning every SID
    /// once let one corrupt subkey or unreadable DACL abandon the entire
    /// remaining fallback, turning every registration only it would have
    /// contributed into an orphan candidate. Scoping the catch to each key read
    /// costs one entry per bad key, never the net.
    /// </summary>
    private static FallbackRead ReadRegistryFallback(
        Dictionary<string, RegisteredPackage> claimed,
        CancellationToken ct)
    {
        var failures = 0;
        var productKeys = 0;
        var unclaimedProductFiles = 0;
        var unclaimedPatchFiles = 0;
        var nonStringValues = 0;
        var unparseableKeyNames = 0;
        var productCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Budgeted, because every catch below sits inside a loop bounded by the
        // machine's registered products and patches, and what fails one key read
        // usually fails the subtree: a DACL or a hive problem across UserData is
        // per-key, not per-machine-once. These are real caught exceptions with a
        // stack trace each, so a patch-heavy machine's storm evicts crash.log
        // faster per entry than the scan's synthesised refusals did.
        var failureLog = new PerItemFailureLog("Registry fallback",
            "These add up to the count the scan weighs against its other source: with the "
            + "product enumeration also short of a record, the scan is refused rather than "
            + "reported. Which keys they were is recorded nowhere else.");

        try
        {
            using var hklm = Microsoft.Win32.RegistryKey.OpenBaseKey(
                Microsoft.Win32.RegistryHive.LocalMachine,
                Microsoft.Win32.RegistryView.Registry64);
            using var udKey = hklm.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData");
            if (udKey is not null)
            {
                foreach (var sidName in udKey.GetSubKeyNames())
                {
                    ct.ThrowIfCancellationRequested();
                    var sidRead = ReadFallbackSid(udKey, sidName, claimed, productCodes, ct, failureLog);
                    failures += sidRead.Failures;
                    productKeys += sidRead.ProductKeys;
                    unclaimedProductFiles += sidRead.UnclaimedProductFiles;
                    unclaimedPatchFiles += sidRead.UnclaimedPatchFiles;
                    nonStringValues += sidRead.NonStringLocalPackageValues;
                    unparseableKeyNames += sidRead.UnparseableProductKeyNames;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Last resort: a failure opening UserData itself or enumerating
            // its SID names (the per-SID reads have their own catches).
            // The crash log preserves a diagnostic trail for reports of
            // missing registered products. Cancellation is excluded:
            // ThrowIfCancellationRequested fires inside this try, so a plain
            // catch would log the user's own Cancel as a fault and swallow the
            // stop the caller is waiting on.
            failures++;
            failureLog.Record(ex, cause: "userdata");
        }
        finally
        {
            // Owed on a cancelled run too, which leaves through the filters above.
            failureLog.WriteClosingEntry();
        }

        return new FallbackRead(failures, productKeys, unclaimedProductFiles, unclaimedPatchFiles,
            nonStringValues, productCodes, unparseableKeyNames);
    }

    /// <summary>
    /// Reads one SID subtree's Products and Patches keys into the fallback set.
    /// Each key read is independently guarded so one corrupt entry costs only
    /// itself; see the try/catch rationale at the call site. Cancellation is
    /// re-thrown, never swallowed.
    ///
    /// Returns how many reads failed. Every catch here logs and continues, which
    /// is right (one bad key must not cost the net) but leaves the caller unable
    /// to tell a clean fallback from one that read almost nothing, and the
    /// caller's other source may be short at the same time. The count is what
    /// makes that state visible; see the gate in GetRegisteredPackagesCore.
    ///
    /// The four catches carry a cause apiece. Two of them read a per-entry key
    /// inside a loop and two read the loop's own parent key, and a subtree
    /// problem hits all four with the same exception type and HRESULT, which is
    /// what the budget keys on. Without the causes the first kind past the
    /// budget would swallow the other three, and "the Products key would not
    /// open" and "one patch's key would not read" are the two ends of a
    /// diagnosis.
    ///
    /// Also reports how many entries named a cached file the API's own loop
    /// never claimed and that is really on the disk. The existence half is
    /// answered here, against the real filesystem, because this is the only
    /// place that knows WHICH paths those are: the merge holds one row per path
    /// and nothing downstream can tell which source first put it there. It costs
    /// a File.Exists per unclaimed path and nothing per claimed one, so on a
    /// machine whose enumeration reached every product it runs nowhere.
    /// </summary>
    private static FallbackRead ReadFallbackSid(
        Microsoft.Win32.RegistryKey udKey,
        string sidName,
        Dictionary<string, RegisteredPackage> claimed,
        HashSet<string> productCodes,
        CancellationToken ct,
        PerItemFailureLog failureLog)
    {
        var failures = 0;
        var productKeys = 0;
        var unclaimedProductFiles = 0;
        var unclaimedPatchFiles = 0;
        var nonStringValues = 0;
        var unparseableKeyNames = 0;

        try
        {
            using var productsKey = udKey.OpenSubKey($@"{sidName}\Products");
            if (productsKey is not null)
            {
                foreach (var prodGuid in productsKey.GetSubKeyNames())
                {
                    ct.ThrowIfCancellationRequested();
                    // Counted from the key list, before anything inside it is
                    // read: a product whose InstallProperties cannot be opened
                    // is still a product this machine has, and the count exists
                    // to be weighed against how many the API enumerated.
                    productKeys++;

                    // Named from the key list for the same reason, and it is the
                    // stronger half: the count can only say the two sources
                    // disagree, where the name says which product the API never
                    // mentioned and can therefore be put to Windows as a question.
                    // Taken before InstallProperties is opened, so a product whose
                    // entry will not read is still a product that can be asked
                    // about.
                    var unpacked = UnpackRegistryProductCode(prodGuid);
                    if (unpacked is not null) productCodes.Add(unpacked);
                    // A key name that is not a packed GUID is the one place the
                    // comparison is blind where a headcount was not: the registry
                    // says this machine has a product and nothing here can turn
                    // the name into a question. Counted, and counted into the
                    // same withholding an unanswerable code reaches, because it
                    // is the same state one step earlier. Skipping it silently
                    // would let a registry this code cannot read look exactly
                    // like a registry that agreed with the enumeration.
                    else unparseableKeyNames++;

                    try
                    {
                        using var ipKey = productsKey.OpenSubKey($@"{prodGuid}\InstallProperties");
                        if (!TryReadLocalPackage(ipKey, out var localPkg))
                        {
                            failures++;
                            // The only way this returns false is a value that was
                            // there and was not a string, so the two counters move
                            // together here and nowhere else: everything else
                            // reaching failures is a thrown exception.
                            nonStringValues++;
                            failureLog.Record(UnreadableLocalPackage(), cause: "product-localpackage");
                        }
                        else if (!string.IsNullOrEmpty(localPkg))
                        {
                            var path = NormaliseLocalPackagePath(localPkg);
                            // Short-circuited on purpose: the disk is asked about
                            // only the paths the API left unclaimed, which on a
                            // whole enumeration is none of them.
                            if (MergeClaim(claimed, new RegisteredPackage(path, "", ""),
                                    ClaimSource.RegistryFallback)
                                && File.Exists(path))
                                unclaimedProductFiles++;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        failures++;
                        failureLog.Record(ex, cause: "product-entry");
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            failures++;
            failureLog.Record(ex, cause: "products-key");
        }

        try
        {
            using var patchesKey = udKey.OpenSubKey($@"{sidName}\Patches");
            if (patchesKey is not null)
            {
                foreach (var patchGuid in patchesKey.GetSubKeyNames())
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        using var patchKey = patchesKey.OpenSubKey(patchGuid);
                        if (!TryReadLocalPackage(patchKey, out var localPkg))
                        {
                            failures++;
                            nonStringValues++;
                            failureLog.Record(UnreadableLocalPackage(), cause: "patch-localpackage");
                        }
                        else if (!string.IsNullOrEmpty(localPkg))
                        {
                            var path = NormaliseLocalPackagePath(localPkg);
                            if (MergeClaim(claimed, new RegisteredPackage(path, "", ""),
                                    ClaimSource.RegistryFallback)
                                && File.Exists(path))
                                unclaimedPatchFiles++;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        failures++;
                        failureLog.Record(ex, cause: "patch-entry");
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            failures++;
            failureLog.Record(ex, cause: "patches-key");
        }

        return new FallbackRead(failures, productKeys, unclaimedProductFiles, unclaimedPatchFiles,
            nonStringValues, null, unparseableKeyNames);
    }

    /// <summary>
    /// Reads a LocalPackage value, separating the two ways it can yield nothing,
    /// because only one of them is a failure and the caller's count is weighed by
    /// the degraded-sources gate.
    ///
    /// A registration with no such value is an ordinary state: an advertised or
    /// partially removed product carries no cached path and there is nothing to
    /// read. A value that is PRESENT and is not a string is a read that failed.
    /// Discarding the second silently through a cast contributes no claim and no
    /// failure, so a subtree of them reads as a fallback that ran cleanly and
    /// found nothing to say, which is the one state the gate exists to tell apart
    /// from a healthy machine.
    ///
    /// Nothing writing these keys is obliged to use REG_SZ. One machine's 136 of
    /// 136 being REG_SZ says what that machine holds and nothing about what the
    /// shape can be.
    /// </summary>
    private static bool TryReadLocalPackage(Microsoft.Win32.RegistryKey? key, out string? path)
    {
        path = null;

        // Absent by structure, then absent by value: neither is a failed read.
        if (key is null) return true;
        var raw = key.GetValue("LocalPackage");
        if (raw is null) return true;

        if (raw is not string value) return false;

        path = value;
        return true;
    }

    /// <summary>
    /// The exception carrying an unreadable LocalPackage into the per-item
    /// failure log. It names no path and no product: the log is read after a
    /// report of missing registered files, and the app runs elevated, so a
    /// registry value from another account's subtree is not something to write
    /// down for a diagnosis that does not need it. The cause string at the call
    /// site says which of the two loops raised it.
    /// </summary>
    private static InvalidDataException UnreadableLocalPackage() =>
        new("A registered LocalPackage value was present and was not a string.");

    /// <summary>
    /// Whether a claimed path's leaf name has more than eight characters before
    /// its extension, so the name itself cannot be an 8dot3 short name.
    ///
    /// The separator search is explicit rather than <c>Path.GetFileName</c>
    /// because this file's own paths are Windows-spelled whatever the host is,
    /// and the framework helper reads a backslash as an ordinary character
    /// anywhere but Windows: the whole path would come back as the leaf, every
    /// row would count, and the number would look like a finding. Nothing here
    /// runs off Windows in production and the counter is not worth a
    /// platform-shaped answer in a test either.
    ///
    /// Eight is the short name's own limit, so this counts the names that cannot
    /// be one and says nothing about whether the volume has generated one
    /// alongside; the two questions are asked separately and answered in the same
    /// report.
    /// </summary>
    internal static bool HasLongLeafStem(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        var lastSeparator = path.LastIndexOfAny(LeafSeparators);
        var leaf = lastSeparator < 0 ? path : path[(lastSeparator + 1)..];

        // Windows takes the LAST dot as the extension separator, so a leaf with
        // several is measured to the last one, and a leaf with none is all stem.
        var lastDot = leaf.LastIndexOf('.');
        var stemLength = lastDot < 0 ? leaf.Length : lastDot;
        return stemLength > 8;
    }

    private static readonly char[] LeafSeparators = ['\\', '/'];

    /// <summary>
    /// A <c>UserData</c> product subkey name turned back into the braced GUID the
    /// Windows Installer API answers in.
    ///
    /// The registry names those keys in the packed form the installer writes: 32
    /// hex characters, no braces and no hyphens, with each of the first three GUID
    /// fields written backwards and the last eight bytes written as swapped pairs.
    /// Unpacking it is what lets a registry product and an enumerated product be
    /// recognised as THE SAME PRODUCT instead of merely counted against each
    /// other, which is the whole difference between naming what an enumeration
    /// missed and estimating how much it missed.
    ///
    /// Null for anything that is not 32 hex characters, and that is not a
    /// tidiness check: the caller turns each of these into a question about a real
    /// machine, and a code invented out of a key name that was never a packed GUID
    /// would be a question about nothing whose answer withholds.
    ///
    /// THE TRANSFORM IS MEASURED, NOT DERIVED. Against the 137 product keys of one
    /// elevated machine (2026-08-08), 136 unpacked to exactly the GUID inside
    /// their own <c>InstallProperties\UninstallString</c>, none disagreed, and the
    /// remaining key carried no UninstallString to check against. One machine's
    /// agreement cannot establish that no other packing exists, which is the
    /// reason an unparseable name refuses rather than guesses.
    /// </summary>
    internal static string? UnpackRegistryProductCode(string packed)
    {
        if (packed.Length != 32) return null;
        foreach (var c in packed)
            if (!char.IsAsciiHexDigit(c)) return null;

        var guid = new char[38];
        guid[0] = '{';
        guid[9] = guid[14] = guid[19] = guid[24] = '-';
        guid[37] = '}';

        CopyReversed(packed, 0, 8, guid, 1);
        CopyReversed(packed, 8, 4, guid, 10);
        CopyReversed(packed, 12, 4, guid, 15);
        CopySwappedPairs(packed, 16, 4, guid, 20);
        CopySwappedPairs(packed, 20, 12, guid, 25);

        return new string(guid);
    }

    /// <summary>One field of the packed form, which is written least-significant first.</summary>
    private static void CopyReversed(string source, int start, int length, char[] target, int at)
    {
        for (var i = 0; i < length; i++) target[at + i] = source[start + length - 1 - i];
    }

    /// <summary>
    /// The packed form's trailing bytes, where the order of the BYTES is kept and
    /// the two hex characters within each are swapped. Reversing the whole run
    /// instead produces a GUID that looks entirely plausible and names a different
    /// product.
    /// </summary>
    private static void CopySwappedPairs(string source, int start, int length, char[] target, int at)
    {
        for (var i = 0; i < length; i += 2)
        {
            target[at + i] = source[start + i + 1];
            target[at + i + 1] = source[start + i];
        }
    }

    /// <summary>
    /// Puts a surviving prefix into the spelling Win32 accepts. Both forms reach
    /// this from a registered value and both name the same object, but only the
    /// <c>\\?\</c> one survives <see cref="Path.GetFullPath(string)"/> intact:
    /// the other's leading separator is read as rooted on the running process's
    /// drive, so the resolver would answer about a path that depends on where the
    /// process was started from. A path with no prefix left is returned as it
    /// arrived, the strip having already dealt with the rooted forms.
    /// </summary>
    private static string ToWin32Prefix(string path) =>
        path.StartsWith(@"\??\", StringComparison.Ordinal)
            ? string.Concat(@"\\?\", path.AsSpan(4))
            : path;

    private const int MaxProductIndex = 10_000;
    private const int MaxConsecutiveNonSuccess = 20;

    /// <summary>
    /// Enumerates every installed product across all contexts. <c>UnreadableRows</c>
    /// counts the rows this loop had to skip (a non-success return, or a Success
    /// that wrote no GUID): each one is an installed product whose patches will
    /// never be enumerated. It is one of the three losses
    /// <see cref="InstallerQueryResult.RecordsIncomplete"/> is built from, the
    /// others being a skipped patch row and an unreadable LocalPackage value.
    /// </summary>
    private (List<(string ProductCode, string? UserSid, MsiInstallContext Context)> Products, int UnreadableRows)
        EnumerateProducts(CancellationToken ct)
    {
        var results = new List<(string, string?, MsiInstallContext)>();
        var productCode = new char[Msi.GuidBufferLength];
        var sidBuffer = new char[SidBufferLength];
        int consecutiveNonSuccess = 0;
        int unreadableRows = 0;
        uint lastError = MsiError.Success;
        bool reachedEnd = false;

        // THE INDEX ADVANCES ON EVERY ITERATION, AND MsiEnumProductsEx DOCUMENTS
        // THAT IT SHOULD NOT: "The index should be incremented, only if the
        // previous call has returned ERROR_SUCCESS." Holding the index across a
        // failed row is unimplementable as stated, because a row that fails
        // permanently would then be retried for ever, so the advance is the
        // deliberate reading and not an oversight.
        //
        // What makes it affordable is that it cannot lose a product silently.
        // Every arm that advances past a non-Success return also increments
        // unreadableRows, which reaches unreadableProducts and withholds the
        // whole removable class for the scan, so a product this loop skips is a
        // product the scan has already declared it did not read. What the caller
        // goes looking for by name is a product lost SILENTLY, and this cannot
        // produce one. Do not "fix" the advance into a retry without replacing
        // that guarantee first.
        for (uint index = 0; index < MaxProductIndex; index++)
        {
            ct.ThrowIfCancellationRequested();

            // Zero the GUID buffer between iterations so a previous
            // call's longer GUID can't leak via BufferToString's null-
            // scan if the next call wrote a shorter string. The MSI
            // API zero-terminates so this is belt-and-braces, but the
            // belt is cheap.
            Array.Clear(productCode);

            // pcchSid is the buffer size in characters including the
            // null terminator on the Win32 input. On Success the API
            // updates it to the count EXCLUDING the terminator. Pass
            // the full SidBufferLength so any plausible SID fits on
            // the first call and the MoreData branch below stays as
            // a safety net.
            uint sidLen = SidBufferLength;

            var error = _msi.EnumProducts(
                productCode: null,
                userSid: AllUsersSid,
                context: MsiInstallContext.All,
                index: index,
                installedProductCode: productCode,
                installedContext: out var installedContext,
                sid: sidBuffer,
                sidLength: ref sidLen);

            if (error == MsiError.MoreData)
            {
                // Defensive only. Real-world SIDs are ~45 chars and
                // the first call passes a 256-char buffer, so this
                // branch isn't exercised in normal use. On MoreData
                // pcchSid carries the SID length EXCLUDING the
                // terminator ("not including the terminating NULL
                // character", MsiEnumProductsExW on pcchSid), and the
                // documented retry size is that count plus one for the
                // null the buffer must also hold.
                sidLen++;
                sidBuffer = new char[sidLen];

                error = _msi.EnumProducts(
                    productCode: null,
                    userSid: AllUsersSid,
                    context: MsiInstallContext.All,
                    index: index,
                    installedProductCode: productCode,
                    installedContext: out installedContext,
                    sid: sidBuffer,
                    sidLength: ref sidLen);
            }

            // Every classification below sits AFTER the retry so it judges
            // whichever call actually produced this row's answer. With the
            // refusal check above the retry, an AccessDenied returned BY the
            // retry would fall through to the tolerated-failure branch and demote
            // the row instead of stopping the scan, which is the one return this
            // loop is not allowed to absorb. The case is near unreachable by
            // contract, since the retry only runs for a SID longer than 256
            // characters, so this is the refusal contract being uniform rather
            // than a failure anybody has met.
            if (error == MsiError.NoMoreItems)
            {
                reachedEnd = true;
                break;
            }

            if (error == MsiError.AccessDenied)
                throw new LocalisedAccessException(Strings.Error_MsiAccessDenied);

            lastError = error;

            if (error == MsiError.Success)
            {
                var code = BufferToString(productCode);
                if (code.Length == 0)
                {
                    // A Success return that wrote no product GUID: the
                    // follow-up GetProductInfo reads would fail quietly and
                    // drop the product's cached file from the registered set,
                    // which is the unsafe direction (a needed file then looks
                    // orphaned). Count it against the tolerance instead of
                    // adding an empty row.
                    consecutiveNonSuccess++;
                    unreadableRows++;
                    if (consecutiveNonSuccess >= MaxConsecutiveNonSuccess)
                        throw new LocalisedInvalidOperationException(
                            string.Format(Strings.Error_MsiNonSuccess, consecutiveNonSuccess, error));
                    continue;
                }

                consecutiveNonSuccess = 0;
                // Clamp sidLen against the buffer length defensively
                // in case the API ever returns a value larger than the
                // buffer accepted (which would be a Win32 bug, but
                // bounding it here means an unbounded read can never
                // reach the managed string constructor).
                var safeSidLen = (int)Math.Min(sidLen, (uint)sidBuffer.Length);
                var sid = (installedContext != MsiInstallContext.Machine && safeSidLen > 0)
                    ? new string(sidBuffer, 0, safeSidLen)
                    : null;
                results.Add((code, sid, installedContext));
            }
            else
            {
                // Scattered per-product failures are tolerated on purpose:
                // this call cannot tell "product has no cached package" from
                // "product unreadable", so a per-product throw would brick the
                // scan over a single bad row. ERROR_BAD_CONFIGURATION is a
                // documented per-row return of MsiEnumProductsEx and the state
                // is reported in the wild (failed-install residue); how often
                // is not known, and no claim about it is needed here, because
                // the tolerance is paid for either way.
                //
                // What pays for it is the demotion in GetRegisteredPackagesCore:
                // a skipped row is safe to skip precisely BECAUSE the removable
                // class is withheld from the scan that skipped it. The two are
                // one mechanism. The registry fallback is not the safety net it
                // reads like: it recovers lost PATHS, never lost VERDICTS. A
                // fallback row has no State to read, so it can supply a path
                // this row would have contributed and can never correct a verdict
                // built without it. FileSystemScanService's correlation gate only
                // catches a total collapse. Only a long RUN of consecutive
                // failures (a wholesale enumeration collapse) throws.
                consecutiveNonSuccess++;
                unreadableRows++;
                if (consecutiveNonSuccess >= MaxConsecutiveNonSuccess)
                    throw new LocalisedInvalidOperationException(
                        string.Format(Strings.Error_MsiNonSuccess, consecutiveNonSuccess, error));
            }
        }

        // Hitting the index cap is not a clean end: the enumeration ran out of
        // budget rather than reporting NoMoreItems, so everything past the cap
        // would be unseen and classified orphaned. Cannot happen on a real
        // machine (nobody has 10,000 products), but if it ever did it falls to
        // the catastrophic side, so fail loudly rather than truncate silently.
        //
        // This carries its own message and must not be merged back into the
        // consecutive-failures one for symmetry. Here the first placeholder is
        // the budget rather than a failure count, and the second is the last
        // row's error, which is Success when every row read cleanly and the
        // list simply never terminated: that string would state two things
        // that are not true of this condition.
        if (!reachedEnd)
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_MsiEnumerationNeverEnded, MaxProductIndex, lastError));

        return (results, unreadableRows);
    }

    /// <summary>
    /// Converts a fixed-size MSI char[] buffer to a managed string by
    /// trimming at the first null terminator. Used for fixed-size GUID
    /// out-buffers where the API doesn't return a length count.
    /// </summary>
    private static string BufferToString(char[] buffer)
    {
        var len = Array.IndexOf(buffer, '\0');
        return len < 0 ? new string(buffer) : new string(buffer, 0, len);
    }

    private const int MaxPatchIndex = 10_000;

    /// <summary>
    /// Enumerates one product's patches. <c>Incomplete</c> reports that at least
    /// one row was skipped, which costs this product's claim on whatever patch
    /// the row named, and reaches the same demotion the product loop's skips do:
    /// the two loops tolerate a bad row identically, so a verdict built through
    /// either is short the same way.
    ///
    /// A sustained run of unreadable rows for ONE product ends that product's
    /// enumeration and returns <c>Incomplete</c>, rather than aborting the whole
    /// scan. The failure is one product's, and the machinery the caller already
    /// runs contains it: the product counts once in the unreadable tally, the
    /// removable class is withheld scan-wide, and the registry fallback still
    /// claims that product's cached files so none are offered. This is the honest
    /// answer to a per-user instance recorded under a SID the enumerator emits but
    /// then rejects as input, where every index refuses identically: the scan
    /// declines to assert a patch list Windows will not hand over, rather than
    /// losing the whole scan over it. Only a machine-level breakdown stays
    /// scan-fatal (the AccessDenied and never-ended-cap throws below).
    /// </summary>
    /// <param name="failureLog">
    /// The run's budget for the abandonment breadcrumb, which is one entry per
    /// product and so unbounded on a machine where the condition is general.
    /// Owned by the caller because the run, not one product, is what it bounds.
    /// </param>
    private (List<(string PatchCode, string? UserSid, MsiInstallContext Context)> Patches, bool Incomplete)
        EnumeratePatches(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        CancellationToken ct,
        PerItemFailureLog failureLog)
    {
        var results = new List<(string, string?, MsiInstallContext)>();
        var patchCode = new char[Msi.GuidBufferLength];
        var targetProductCode = new char[Msi.GuidBufferLength];
        int consecutiveNonSuccess = 0;
        bool incomplete = false;
        uint lastError = MsiError.Success;
        bool reachedEnd = false;

        for (uint index = 0; index < MaxPatchIndex; index++)
        {
            ct.ThrowIfCancellationRequested();

            // Match EnumerateProducts: zero the GUID buffers between
            // iterations so a previous call's longer GUID can't leak via
            // BufferToString's null-scan if the next call wrote a shorter
            // string. The MSI API zero-terminates so this is belt-and-
            // braces; the belt is cheap.
            Array.Clear(patchCode);
            Array.Clear(targetProductCode);

            uint sidLen = 0;

            var error = _msi.EnumPatches(
                productCode: productCode,
                userSid: userSid,
                context: context,
                filter: MsiPatchFilter.All,
                index: index,
                patchCode: patchCode,
                targetProductCode: targetProductCode,
                targetProductContext: out var patchContext,
                targetUserSid: null,
                targetUserSidLength: ref sidLen);

            if (error == MsiError.NoMoreItems)
            {
                reachedEnd = true;
                break;
            }

            if (error == MsiError.AccessDenied)
                // Match the product loop: an API refusal must land on the scan,
                // not on the verdict. Breaking here would yield zero patches for
                // this product without recording the loss, so neither the
                // scan-wide removable withholding nor the both-sources-degraded
                // gate would see it and the scan would report itself complete
                // while short of a claim. That is what separates it from the run
                // of unreadable rows that degrades instead (below): the run
                // returns Incomplete, where a break would return with the flag
                // still false. The scan command's catch routes this to a dialog
                // and to crash.log.
                throw new LocalisedAccessException(Strings.Error_MsiAccessDenied);

            lastError = error;

            if (error == MsiError.Success || error == MsiError.MoreData)
            {
                var code = BufferToString(patchCode);
                if (code.Length == 0)
                {
                    // An empty patch GUID accepted as success would fail the
                    // follow-up GetPatchInfo reads and drop the patch from the
                    // registered set (the unsafe direction). Count it against
                    // the tolerance rather than adding an empty row.
                    consecutiveNonSuccess++;
                    incomplete = true;
                    // A run of empty GUIDs is the same "this product's rows are
                    // unreadable" state as the non-success arm below, and degrades
                    // the same way: end this product's enumeration, mark it
                    // incomplete, leave the scan running (see this method's summary
                    // and the non-success arm for why one product's loss is not the
                    // scan's).
                    if (consecutiveNonSuccess >= MaxConsecutiveNonSuccess)
                    {
                        LogPatchEnumerationAbandoned(productCode, context, userSid, error, index,
                            failureLog, cause: "empty-guid-run");
                        return (results, incomplete);
                    }
                    continue;
                }

                consecutiveNonSuccess = 0;
                results.Add((code, userSid, patchContext));
            }
            else
            {
                consecutiveNonSuccess++;
                incomplete = true;
                // ERROR_UNKNOWN_PRODUCT REACHES HERE AND MUST KEEP REACHING HERE.
                // This call names a product, so unlike the machine-wide one the
                // code can carry its documented meaning ("The product that
                // szProduct specifies is not installed on the computer in the
                // specified contexts"), and read that way it would say this
                // product holds no patches and cost nothing. It is not read that
                // way on purpose. The product came out of the product
                // enumeration moments earlier, so the two answers contradict each
                // other, and the reading that fits both is that the identity or
                // the context this call was given did not round-trip, which is a
                // registration this scan cannot see the patch list of rather than
                // a product with no patches. Taking the absence at face value
                // would drop that product's Applied claims and let a patch it
                // still holds be offered. The contradiction is information, not
                // an answer, so it degrades like any other unreadable row.
                //
                // One product whose patch rows keep coming back unreadable is a
                // per-product loss, not a scan failure: stop enumerating THIS
                // product's patches and return Incomplete so the caller records
                // one unreadable product and carries on. The reason it is safe to
                // stop rather than abort is the same reason a scattered skip is:
                // the removable class is withheld scan-wide the moment any product
                // is short (so no superseded patch is offered on a run that lost a
                // claim), and the registry fallback claims this product's cached
                // .msp/.msi files independently of the API (so none looks
                // orphaned). What that costs is real and deliberate: the
                // withholding is scan-wide, and on a machine whose registration
                // keeps refusing one product's patch list every scan, it is the
                // steady state until the registration itself changes, not a
                // transient. Declining to name a patch list Windows refuses to
                // return beats guessing at one. Orphan cleanup is unaffected. A
                // whole-machine breakdown is a different case and still aborts: the
                // AccessDenied and never-ended-cap throws stay fatal.
                if (consecutiveNonSuccess >= MaxConsecutiveNonSuccess)
                {
                    LogPatchEnumerationAbandoned(productCode, context, userSid, error, index,
                        failureLog, cause: "unreadable-row-run");
                    return (results, incomplete);
                }
            }
        }

        // See EnumerateProducts: hitting the cap is an unterminated
        // enumeration, not a clean end. Fail loudly rather than truncate, and
        // keep its own message for the reason given there.
        if (!reachedEnd)
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_MsiPatchEnumerationNeverEnded, MaxPatchIndex, lastError));

        return (results, incomplete);
    }

    /// <summary>
    /// Records that one product's patch enumeration was abandoned after a full run
    /// of unreadable rows. Dev-facing crash-log breadcrumb only, deliberately not
    /// localised and never surfaced: the user is told through the scan summary's
    /// kept-patches notice that something in the records could not be matched up,
    /// which carries no product identity and no count, whereas diagnosing WHY the
    /// withholding fired needs exactly that identity. Without this line the abandonment leaves no record of which product
    /// triggered it, so a field report can be pinned to a product only by the
    /// reporter running the Windows Installer API by hand. Carries the product
    /// code, its install context and SID (the round-trip that fails when the SID
    /// is one the enumerator emits but rejects), the last error code, and the
    /// index reached.
    /// </summary>
    /// <param name="cause">
    /// Which arm abandoned: a run of rows the API returned as success with an
    /// empty GUID, or a run of non-success returns. Two causes and not one per
    /// product, on purpose. The budget keys on it, so per-product causes would
    /// buy an entry each and leave no budget at all, where these two keep the
    /// distinction that matters: a machine failing one way throughout does not
    /// hide a single product failing the other way at product 400. The entries
    /// themselves carry the product identity, and the first twenty of those are
    /// logged in full whatever their cause.
    /// </param>
    private static void LogPatchEnumerationAbandoned(
        string productCode, MsiInstallContext context, string? userSid, uint lastError, uint index,
        PerItemFailureLog failureLog, string cause) =>
        failureLog.Record(new InvalidOperationException(
            $"Patch enumeration abandoned for product {productCode} (context {context}, SID {userSid ?? "none"}) " +
            $"after {MaxConsecutiveNonSuccess} consecutive unreadable rows; last error code {lastError}, reached index {index}. " +
            "Superseded-patch cleanup is withheld scan-wide; this product's cached files are kept via the registry fallback."),
            cause);

    /// <summary>
    /// One property read's outcome. The returned value alone cannot carry it:
    /// an empty string means both "this record has no such property" and "the
    /// read failed", and for LocalPackage those are opposite facts. A benign
    /// absence is a product that never had a cached package to lose. A failed
    /// read is a product that has one, still needs it, and whose claim on it has
    /// just gone missing from the scan. Both reach the call site as "", so the
    /// call site cannot skip the row on the second the way it safely skips it on
    /// the first unless the outcome travels with the value.
    ///
    /// <paramref name="NotRegistered"/> NARROWS <paramref name="Unreadable"/>
    /// rather than replacing it, and the pairing is deliberate: the read produced
    /// no value, so every caller that only asks "did I get an answer" keeps the
    /// behaviour it has, and the one caller that has to NAME a cause can ask the
    /// narrower question. It is set only for a code documented as meaning the
    /// record itself is not there, which is a positive answer about the machine
    /// and not a failure to read one.
    /// </summary>
    internal readonly record struct PropertyRead(string Value, bool Unreadable, bool NotRegistered = false);

    /// <summary>
    /// The rule that decided, up to v2.3.0, whether a patch's cached .msp was
    /// offered for removal, from its State and Uninstallable values exactly as
    /// <c>MsiGetPatchInfoEx</c> returned them.
    ///
    /// NO CALLER ACTS ON IT FROM 3.0.0. The enumeration does not consult it, and
    /// the two act-time passes that do reach it are handed the patch claims naming
    /// a candidate, of which there are now none. It is kept whole, with its tests,
    /// because it is the rule itself rather than a helper: if the class is ever
    /// offered again this is what it would be offered on, and rebuilding it from
    /// the documentation a second time is how a subtlety gets lost.
    ///
    /// WHAT THE RULE IS WORTH, WHICH IS WHY IT STOPPED BEING USED. The State half
    /// carries real information: Windows has computed that a later patch took over
    /// this one's fixes. It does not say the cached file is spare, and Microsoft's
    /// own words for the two states are "applied to this product instance but is
    /// superseded" and "applied in this product instance but obsolete". The
    /// Uninstallable half was taken for the cautious conjunct and is not one: it
    /// reports whether Windows can UNDO the patch, which its own reference page
    /// gives eight causes for, the commonest being that the patch author never set
    /// the AllowRemoval row. So a positively read "0" says the patch cannot be
    /// rolled back, and nothing whatever about whether anything still reads the
    /// file. Measured against real patches the conjunct behaves as a vendor filter
    /// pointing the wrong way: all 58 patches in Office 2010 SP2 declare
    /// themselves removable and were refused, and three live Adobe patches declare
    /// themselves not removable and were offered.
    ///
    /// Both directions still fail safe and that has not changed. An unparseable
    /// State leaves the parsed value at 0 (not a patch), and only a positively
    /// read "0" for Uninstallable clears the second half.
    /// </summary>
    internal static bool IsRemovablePatch(string stateValue, string uninstallableValue)
    {
        int.TryParse(stateValue, out var patchState);
        return patchState is 2 or 4 && uninstallableValue == "0";
    }

    /// <summary>
    /// The benign returns of an Msi*GetInfoEx property read, as an ALLOWLIST.
    /// ERROR_SUCCESS is a value (or, at zero length, a property present and
    /// empty); ERROR_UNKNOWN_PROPERTY is the answer for a property the record
    /// does not carry, which is what a product or a registered-not-applied patch
    /// with no cached package gives. A probe of 136 products and 2 patches on
    /// Windows 10.0.26200 / msi.dll 5.0.26100.7920 (2026-07-18) established that
    /// the two cases are distinguishable at all: an absent property returned
    /// 1608 rather than a zero-length success, and a product that could not be
    /// read returned 87. No product on that machine genuinely lacked a cached
    /// package, so 1608 was observed for an absent property rather than for a
    /// real absent LocalPackage; both shapes are on this list, so either reading
    /// lands on the benign side.
    ///
    /// The direction matters more than the membership. One machine can show
    /// which codes ARE benign; no machine can enumerate every failure code that
    /// exists, so an unlisted code falls to the unreadable side and withholds.
    /// Inverting this into a list of known-bad codes reinstates the exact fault
    /// it closes: the failure nobody has seen yet would read as an absence and
    /// silently delete a product's claim on a file it still needs.
    /// </summary>
    private static bool IsBenignPropertyRead(uint error) =>
        error is MsiError.Success or MsiError.MoreData or MsiError.UnknownProperty;

    /// <summary>
    /// The returns that positively establish there is no such record, as a second
    /// ALLOWLIST and for the same reason the first one is one: only a code
    /// documented to mean the record is absent may be read as absence, and
    /// everything unlisted stays on the unreadable side and withholds. Inverting
    /// this would let an unseen failure pass as "the registration has gone",
    /// which is the direction that costs a file.
    ///
    /// The membership is exactly what <c>MsiGetPatchInfoEx</c> documents for a
    /// pairing it cannot find, and nothing wider. ERROR_PRODUCT_UNINSTALLED
    /// (1614) reads as though it belongs and is deliberately absent: it is not
    /// among that function's documented returns, and a code added here on how its
    /// name sounds is a guess with a file on the end of it.
    ///
    /// Only the under-lease re-read asks this. The scan's own consumers of
    /// <see cref="PropertyRead"/> read <c>Unreadable</c>, which is still set, so
    /// the direction they fail in is unchanged.
    /// </summary>
    private static bool IsRecordAbsent(uint error) =>
        error is MsiError.UnknownProduct or MsiError.UnknownPatch;

    /// <summary>
    /// The returns of a KEYED <c>MsiEnumProductsEx</c> that positively establish
    /// the product asked about is not installed. A third ALLOWLIST for the reason
    /// the two above are ones: only a code documented to mean absence may be read
    /// as absence, and an unlisted one stays unaskable and withholds.
    ///
    /// ERROR_UNKNOWN_PRODUCT belongs on it because that function's return table
    /// glosses it "The product is not installed on the computer in the specified
    /// context", which is an answer about the machine rather than a failure to
    /// read one. Reading it as a failure to read is the expensive direction here
    /// and not the safe one: the products a cached patch declares as targets are
    /// mostly products the machine does not have, so it would withhold on the
    /// ordinary case rather than on a fault.
    ///
    /// THE SAME CODE IS A FAILURE WHERE THE CALL NAMES NO PRODUCT, AND THAT IS NOT
    /// AN INCONSISTENCY TO TIDY AWAY. <see cref="EnumerateProducts"/> and
    /// <see cref="EnumeratePatchHoldersAcrossAllProducts"/> both pass a null
    /// product code, so there is no product for the code to be reporting absent
    /// and it cannot carry this meaning; both are right to treat it as a row or a
    /// set short by an unknown amount. The meaning is the question's, not the
    /// number's.
    /// </summary>
    private static bool IsProductNotInstalled(uint error) =>
        error is MsiError.NoMoreItems or MsiError.UnknownProduct;

    /// <summary>
    /// Retrieves a product property using the double-call buffer pattern,
    /// reporting whether an empty result is an absence or a failed read (see
    /// <see cref="PropertyRead"/>).
    /// </summary>
    private PropertyRead GetProductProperty(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string propertyName)
    {
        uint bufferLen = 0;

        var error = _msi.GetProductInfo(
            productCode: productCode,
            userSid: userSid,
            context: context,
            property: propertyName,
            value: null,
            valueLength: ref bufferLen);

        if (error != MsiError.Success && error != MsiError.MoreData)
            return new PropertyRead(string.Empty, Unreadable: !IsBenignPropertyRead(error),
                NotRegistered: IsRecordAbsent(error));

        if (bufferLen == 0)
            return new PropertyRead(string.Empty, Unreadable: false);

        bufferLen++; // space for null terminator
        var buffer = new char[bufferLen];

        error = _msi.GetProductInfo(
            productCode: productCode,
            userSid: userSid,
            context: context,
            property: propertyName,
            value: buffer,
            valueLength: ref bufferLen);

        // Only ERROR_SUCCESS is benign on the second call, which is narrower
        // than the allowlist above and deliberately so: the first call has
        // already reported a value of this length, so the record demonstrably
        // carries the property and anything other than success here is a value
        // that exists and could not be read. The allowlist's
        // ERROR_UNKNOWN_PROPERTY arm describes a record that never carried it.
        //
        // Defensive clamp: a successful Msi*GetInfoEx returns bufferLen as the
        // count excluding the terminator and never larger than the input.
        // Math.Min bounds an unbounded read even if the API ever violates that
        // contract.
        return error == MsiError.Success
            ? new PropertyRead(new string(buffer, 0, (int)Math.Min(bufferLen, (uint)buffer.Length)), Unreadable: false)
            : new PropertyRead(string.Empty, Unreadable: true);
    }

    /// <summary>
    /// Retrieves a patch property using the double-call buffer pattern,
    /// reporting whether an empty result is an absence or a failed read (see
    /// <see cref="PropertyRead"/>).
    /// </summary>
    internal static PropertyRead GetPatchProperty(
        IMsiApi msi,
        string patchCode,
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string propertyName)
    {
        uint bufferLen = 0;

        var error = msi.GetPatchInfo(
            patchCode: patchCode,
            productCode: productCode,
            userSid: userSid,
            context: context,
            property: propertyName,
            value: null,
            valueLength: ref bufferLen);

        if (error != MsiError.Success && error != MsiError.MoreData)
            return new PropertyRead(string.Empty, Unreadable: !IsBenignPropertyRead(error),
                NotRegistered: IsRecordAbsent(error));

        if (bufferLen == 0)
            return new PropertyRead(string.Empty, Unreadable: false);

        bufferLen++; // space for null terminator
        var buffer = new char[bufferLen];

        error = msi.GetPatchInfo(
            patchCode: patchCode,
            productCode: productCode,
            userSid: userSid,
            context: context,
            property: propertyName,
            value: buffer,
            valueLength: ref bufferLen);

        // See GetProductProperty: the second call's narrower rule, and the
        // reason for the clamp.
        return error == MsiError.Success
            ? new PropertyRead(new string(buffer, 0, (int)Math.Min(bufferLen, (uint)buffer.Length)), Unreadable: false)
            : new PropertyRead(string.Empty, Unreadable: true);
    }
}
