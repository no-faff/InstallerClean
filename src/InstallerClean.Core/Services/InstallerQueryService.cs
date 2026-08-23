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
    /// <param name="ProductPatchSets">
    /// One verdict per product code, from the registry's own per-product patch
    /// list, or null where the caller supplied no reader. See
    /// <see cref="ProductPatchSet"/> for what the three values mean and
    /// <see cref="ReadProductPatchSet"/> for how each is reached.
    ///
    /// IT DECIDES NOTHING YET AND IS SENT NOWHERE YET. The reading lands ahead of
    /// both the rule that consumes it and the payload fields that will carry it, so
    /// that the three can be reviewed and reverted apart. The four counts beside it
    /// reach <see cref="EnumerationCensus"/> and stop there; wiring them to the wire
    /// is a payload change and lands with the rule.
    /// </param>
    /// <param name="ProductPatchKeys">
    /// Products whose <c>Patches</c> key opened. Against
    /// <paramref name="ProductKeys"/> it answers how usual it is for a product to
    /// carry one at all: one machine reads 138 of 139, and a product with no
    /// patches has no reason to carry it.
    /// </param>
    /// <param name="ProductPatchRegistrations">
    /// Patch subkeys seen under those keys, one per (product, patch) registration
    /// rather than per patch. With <paramref name="ProductPatchKeys"/> it is the
    /// shape fact the measured machine is least like: it held five when this was
    /// written on 2026-08-17 and three when its hives were read on 2026-08-18, with
    /// nothing in this code changing in between. The figure is dated because it is
    /// one machine's state at one moment, and an undated one reads as current for
    /// ever.
    /// </param>
    /// <param name="ProductsWithRemovablePatch">
    /// Products where at least one registered patch positively declared itself
    /// removable. THIS IS THE COUNT THAT SAYS WHETHER THE PER-PRODUCT CONDITION
    /// WILL WITHHOLD ANYTHING IN THE FIELD, which no measurement of one machine can
    /// answer.
    /// </param>
    /// <param name="ProductsWithPatchSetUnestablished">
    /// Products whose patch set could not be established at all. The other half of
    /// the same question, and the one that separates "the condition found a reason"
    /// from "the condition could not look".
    /// </param>
    internal readonly record struct FallbackRead(
        int Failures,
        int ProductKeys,
        int UnclaimedProductFiles = 0,
        int UnclaimedPatchFiles = 0,
        int NonStringLocalPackageValues = 0,
        IReadOnlyCollection<string>? RegistryProductCodes = null,
        int UnparseableProductKeyNames = 0,
        IReadOnlyDictionary<string, ProductPatchSet>? ProductPatchSets = null,
        int ProductPatchKeys = 0,
        int ProductPatchRegistrations = 0,
        int ProductsWithRemovablePatch = 0,
        int ProductsWithPatchSetUnestablished = 0,
        PathCensus? Paths = null);

    /// <summary>
    /// Which step of <see cref="NormaliseLocalPackagePath"/> a recorded path was
    /// being put through when it was refused. A marker in scope rather than three
    /// separate try blocks, because that method is on the path every claim takes
    /// and restructuring its control flow to improve a counter is the wrong trade:
    /// the value it hands back on refusal is pinned by a test and must not move.
    /// </summary>
    internal enum NormalisationStage
    {
        /// <summary>
        /// The value carries a character no path can carry, tested before anything
        /// is attempted on it. Its own member rather than part of the expansion
        /// below, because for such a value the expansion is never reached: what it
        /// names is the value's own shape, where the other three name a call that
        /// refused it.
        /// </summary>
        EmbeddedNull,

        /// <summary>Expanding an environment variable.</summary>
        Expansion,

        /// <summary>
        /// Taking the long-path or NT object prefix off, and preparing and putting
        /// the value to the final-path resolver. Both are prefix work on a string;
        /// the resolver itself reports its own failures separately and does not
        /// throw, so what this covers is the preparation around it.
        /// </summary>
        PrefixStrip,

        /// <summary>The closing <see cref="Path.GetFullPath(string)"/> alone.</summary>
        FullPath,
    }

    /// <summary>
    /// How the recorded paths one scan read turned out: how often the final-path
    /// resolver was asked and what it answered, and how often a value could not be
    /// turned into a path at all.
    ///
    /// THE DENOMINATOR TRAVELS WITH THE FIVE OUTCOMES AND WITHOUT IT THEY CANNOT BE
    /// READ. The resolver is asked only for a value carrying a prefix or an 8dot3
    /// alias (<see cref="CarriesFlaggedSpelling"/>), which on most machines is no
    /// value at all. Four of its five failures would then read zero because nothing
    /// asked, which is indistinguishable from zero because nothing failed, and a
    /// receiver would take the second reading. <see cref="ResolverAttempts"/> is
    /// what separates them.
    ///
    /// BOTH GROUPS NOW DECIDE THE OFFER, AND THE OLD NOTE HERE SAID THE OPPOSITE.
    /// It said the resolver's five outcomes were read by nothing and existed to size
    /// a failure before anything was designed around it. That was true for one
    /// release. From 3.0.0 the five withhold exactly as the four normalisation
    /// refusals do, on one rule in one place rather than a second quiet copy of one:
    /// FileSystemScanService withholds the whole walk-derived offer where
    /// <c>EnumerationCensus.AnyRecordedPathUnestablished</c> answers true, and that
    /// property is where every population is added to the question.
    ///
    /// THE ATTEMPTS COUNT IS STILL MEASUREMENT AND NOT A RULE. Nothing withholds on
    /// it. It is what makes the five readable, since a machine flagging no path at
    /// all reports five zeros that are indistinguishable on the wire from five clean
    /// answers.
    /// </summary>
    internal sealed class PathCensus
    {
        /// <summary>
        /// Recorded paths put to the final-path resolver, which from 3.0.0 is every
        /// value that got past the embedded-null test and the expansion.
        /// </summary>
        internal int ResolverAttempts;

        /// <summary>
        /// Of those, the ones carrying a spelling only the filesystem can settle: an
        /// 8dot3 alias, or a prefix the strip left on for want of a drive root.
        ///
        /// IT DECIDES NOTHING AND IS THE ONLY MEMBER HERE THAT NEVER DID. The other
        /// counters record what happened to a value; this records what the value
        /// LOOKED LIKE. It exists because widening the resolver to every path took
        /// the answer away from <see cref="ResolverAttempts"/>, which used to be both
        /// figures at once, and how often these spellings occur on real machines is a
        /// question this project has been trying to answer rather than an incidental.
        /// </summary>
        internal int FlaggedSpellings;

        /// <summary>Of those, the ones it refused outright as not a path.</summary>
        internal int ResolverNotAPath;

        /// <summary>Of those, the ones with no existing ancestor anywhere.</summary>
        internal int ResolverNoExistingAncestor;

        /// <summary>Of those, the ones it could not open a handle on.</summary>
        internal int ResolverOpenRefused;

        /// <summary>Of those, the ones whose final name came back empty.</summary>
        internal int ResolverFinalNameUnavailable;

        /// <summary>Of those, the ones where the attempt threw.</summary>
        internal int ResolverFaulted;

        /// <summary>
        /// Values refused for carrying a character no path can carry, before the
        /// expansion below was attempted on them.
        /// </summary>
        internal int NormalisationRefusedAtEmbeddedNull;

        /// <summary>Values refused while expanding an environment variable.</summary>
        internal int NormalisationRefusedAtExpansion;

        /// <summary>Values refused while taking a prefix off or preparing the resolver ask.</summary>
        internal int NormalisationRefusedAtPrefixStrip;

        /// <summary>Values <see cref="Path.GetFullPath(string)"/> refused.</summary>
        internal int NormalisationRefusedAtFullPath;

        /// <summary>
        /// Every value this scan could not turn into a path, whatever refused it.
        /// Derived rather than tallied, so the parts and the total cannot disagree.
        /// This is the population a claim is kept raw for, and the one the
        /// withholding acts on: a mixed set with four causes, so nothing may state
        /// a single cause for it.
        /// </summary>
        internal int NormalisationRefusedTotal =>
            NormalisationRefusedAtEmbeddedNull
            + NormalisationRefusedAtExpansion
            + NormalisationRefusedAtPrefixStrip
            + NormalisationRefusedAtFullPath;

#if DEBUG
        /// <summary>
        /// The thread that built this census, kept in debug builds only so that the
        /// increments below can be held to it.
        /// </summary>
        private readonly int _owningThread = Environment.CurrentManagedThreadId;
#endif

        /// <summary>
        /// THE ONE THING THAT MAKES THIS TYPE SAFE IS THE CALL GRAPH'S SHAPE, WHICH IS
        /// NOT SOMETHING THE TYPE CAN HOLD ANYBODY TO. Every increment on a census is
        /// a read-modify-write on a plain int field, so two threads incrementing one
        /// census lose counts, and a lost normalisation refusal is a withholding that
        /// does not fire. Today the enumeration is single-threaded by construction of
        /// its entry point: the whole synchronous core runs inside one Task.Run with
        /// no await in it, and this file holds no other concurrency primitive.
        ///
        /// This is what makes the first change to that fail a test rather than report
        /// a smaller number. Debug builds only, which is where the suite runs: a
        /// release build must not acquire a new way to throw on a user's machine for
        /// the sake of an assertion about this project's own code.
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        private void AssertOwningThread()
        {
#if DEBUG
            if (Environment.CurrentManagedThreadId == _owningThread) return;
            throw new InvalidOperationException(
                "A PathCensus was incremented on a thread other than the one that built it. "
                + "The counts are plain int fields with no interlocking, so a parallel "
                + "enumeration loses increments silently, and a lost normalisation refusal "
                + "is a withholding that does not fire. Give each unit of parallel work its "
                + "own census and fold them with Add, which is what the API loop and the "
                + "registry fallback already do.");
#endif
        }

        /// <summary>
        /// One value put to the final-path resolver, counted whether it answers or
        /// not. A method rather than a bare increment at the call site so that the
        /// thread guard covers every counter and not merely the ones a switch reaches.
        /// </summary>
        internal void RecordResolverAttempt()
        {
            AssertOwningThread();
            ResolverAttempts++;
        }

        /// <summary>
        /// One value seen to carry a spelling only the filesystem can settle. A
        /// method rather than a bare increment for the same reason as the attempt
        /// above: the thread guard has to cover every counter.
        /// </summary>
        internal void RecordFlaggedSpelling()
        {
            AssertOwningThread();
            FlaggedSpellings++;
        }

        internal void RecordResolution(PathResolution outcome)
        {
            AssertOwningThread();
            switch (outcome)
            {
                case PathResolution.NotAPath: ResolverNotAPath++; break;
                case PathResolution.NoExistingAncestor: ResolverNoExistingAncestor++; break;
                case PathResolution.OpenRefused: ResolverOpenRefused++; break;
                case PathResolution.FinalNameUnavailable: ResolverFinalNameUnavailable++; break;
                case PathResolution.Faulted: ResolverFaulted++; break;
                    // Resolved is not counted: it is the attempts less the five, and a
                    // stored copy could disagree with them.
            }
        }

        internal void RecordNormalisationRefusal(NormalisationStage stage)
        {
            AssertOwningThread();
            switch (stage)
            {
                case NormalisationStage.EmbeddedNull: NormalisationRefusedAtEmbeddedNull++; break;
                case NormalisationStage.Expansion: NormalisationRefusedAtExpansion++; break;
                case NormalisationStage.PrefixStrip: NormalisationRefusedAtPrefixStrip++; break;
                case NormalisationStage.FullPath: NormalisationRefusedAtFullPath++; break;
            }
        }

        /// <summary>
        /// Folds another scan-half's tallies in. The API loop and the registry
        /// fallback each normalise their own paths and neither can see the other's,
        /// so the census the report carries is the sum.
        /// </summary>
        internal void Add(PathCensus? other)
        {
            AssertOwningThread();
            if (other is null) return;
            ResolverAttempts += other.ResolverAttempts;
            FlaggedSpellings += other.FlaggedSpellings;
            ResolverNotAPath += other.ResolverNotAPath;
            ResolverNoExistingAncestor += other.ResolverNoExistingAncestor;
            ResolverOpenRefused += other.ResolverOpenRefused;
            ResolverFinalNameUnavailable += other.ResolverFinalNameUnavailable;
            ResolverFaulted += other.ResolverFaulted;
            NormalisationRefusedAtEmbeddedNull += other.NormalisationRefusedAtEmbeddedNull;
            NormalisationRefusedAtExpansion += other.NormalisationRefusedAtExpansion;
            NormalisationRefusedAtPrefixStrip += other.NormalisationRefusedAtPrefixStrip;
            NormalisationRefusedAtFullPath += other.NormalisationRefusedAtFullPath;
        }
    }


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

        // Products installed as a second instance of themselves, and the products
        // that would not answer the question. Both decide nothing at all: see
        // EnumerationCensus.InstanceProductCount for what the pair is for and why
        // neither may be read without the other.
        var instanceProducts = 0;
        var instanceTypeUnreadable = 0;

        // THE API's OWN READING OF EACH PRODUCT'S PATCH SET, which is one of the
        // three sources the superseded-patch condition unions. It is built here
        // rather than asked for later because the loop below already reads every
        // patch's Uninstallable for every product it reaches, so the answers are
        // free at this point and a keyed re-read afterwards would ask the same
        // question twice.
        //
        // ONE ENTRY PER PRODUCT, NOT PER PATCH, because the condition is about the
        // product: a rollback on any product holding a superseded patch reaches for
        // that patch's one cached file, so what matters is whether ANY patch on that
        // product can be uninstalled. See ProductPatchSet for the three answers and
        // why two of them withhold for different reasons.
        var apiPatchSets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase);

        // How the recorded paths THIS loop read turned out. The registry fallback
        // keeps its own and the two are added at the census, neither half being able
        // to see the other's.
        var pathCensus = new PathCensus();

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

            // One more keyed property read on a product this loop has already
            // reached, rather than a second enumeration: the walk behind this loop
            // already passes the everyone SID across all three contexts, which is
            // the shape the question needs, so asking here costs one call per
            // product and nothing per machine.
            //
            // IT REACHES NO VERDICT AND MUST NOT ACQUIRE ONE. A failed read does
            // not count as an instance product, does not count as an ordinary one,
            // and above all does not feed recordsShort: the whole class the other
            // reads in this loop withhold for is about a CLAIM that never reached
            // the merge, and this property carries no claim on any file. Counting
            // it there would withhold the removable class over a diagnostic.
            var instanceType = GetProductProperty(productCode, userSid, context, MsiInstallProperty.InstanceType);
            if (instanceType.Unreadable) instanceTypeUnreadable++;
            else if (int.TryParse(instanceType.Value.TrimEnd('\0').Trim(), out var instanceTypeValue)
                     && instanceTypeValue != 0) instanceProducts++;

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
                    new RegisteredPackage(NormaliseLocalPackagePath(localPackage.Value, pathCensus), productName, productCode),
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
                // AND A PATCH WHOSE PATH READS BENIGNLY EMPTY TAKES NEITHER ARM,
                // WHICH IS THE ONE MEASURED REASON THE PER-PRODUCT CONDITION UNIONS
                // THREE SOURCES RATHER THAN TRUSTING THIS LOOP. Present and
                // zero-length is not a read failure, so recordsShort stays false and
                // nothing records the gap; and the whole block below is skipped, so
                // the pairing contributes no claim, no State read and no verdict to
                // its own product's entry in apiPatchSets. The API's view of that
                // product's patch set is then short of a patch, silently, and a
                // product holding one patch that could be uninstalled and one whose
                // path read empty looks from here like a product holding nothing
                // removable. The registry patch-set read and the all-products patch
                // enumeration are what see it, which is why the condition asks all
                // three and takes the worst answer rather than the first.
                else if (patchPath.Value.Length > 0)
                {
                    var stateRead = GetPatchProperty(_msi, patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.State);
                    var uninstallableRead = GetPatchProperty(_msi, patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.Uninstallable);
                    var stateStr = stateRead.Value;

                    // A read that failed leaves nothing established about the
                    // registration, which no surface may describe as a claim, and the
                    // count travels beside the flag because nobody knows how often
                    // either read fails on a machine that is not the one this was
                    // measured on. It also refuses the removable verdict below, both
                    // halves of that rule needing a positive answer.
                    var verdictUnreadable = stateRead.Unreadable || uninstallableRead.Unreadable;
                    if (verdictUnreadable) unreadablePatchStates++;

                    // An unparseable State leaves patchState at 0 (not-a-patch),
                    // which is the safe direction on purpose rather than luck: only
                    // a positively read Superseded (2) or Obsoleted (4) labels a row
                    // as one of those.
                    int.TryParse(stateStr, out var patchState);

                    // THIS PATCH'S CONTRIBUTION TO ITS PRODUCT'S PATCH SET, which is
                    // the API's reading of one of the three sources the superseded
                    // condition unions. Free here: the loop has just read this
                    // pairing's Uninstallable for its own purposes.
                    //
                    // THE ORDER OF THE ARMS IS THE WHOLE OF IT. A read that failed
                    // establishes nothing. A positive "0" is the only clean answer.
                    // An EMPTY value is an inability and not a finding, which is the
                    // arm easiest to get wrong: comparing against "0" alone would read
                    // an absent property as a removable patch, which is a cause stated
                    // for something nobody measured. Anything else present is a
                    // positive finding that something on this product can be
                    // uninstalled.
                    var apiVerdict =
                        stateRead.Unreadable || uninstallableRead.Unreadable
                            ? ProductPatchSet.Unestablished
                        : uninstallableRead.Value == "0" ? ProductPatchSet.AllNonRemovable
                        : uninstallableRead.Value.Length == 0 ? ProductPatchSet.Unestablished
                        : ProductPatchSet.RemovablePatchPresent;
                    apiPatchSets[productCode] = apiPatchSets.TryGetValue(productCode, out var seenApi)
                        ? Worse(seenApi, apiVerdict)
                        : apiVerdict;

                    var claimedPath = NormaliseLocalPackagePath(patchPath.Value, pathCensus);

                    // THE REMOVABLE VERDICT IS GRANTED HERE AND TAKEN AWAY LATER, and
                    // the order is the architecture rather than a convenience. This
                    // half needs only what has just been read; the other half needs
                    // the registry's per-product patch sets, which are read after this
                    // loop finishes. So the verdict is granted provisionally and
                    // JudgeAndWithholdAgainstEveryProductPatchSet removes it, which works because
                    // every path a verdict can travel is downgrade-only: MergeClaim
                    // never upgrades, and Downgrade is one-way. A row that leaves this
                    // loop removable can still be withheld by four separate later
                    // passes and can never be made removable again by any of them.
                    MergeClaim(claimed,
                        new RegisteredPackage(claimedPath, productName, productCode, patchState,
                            IsRemovable: IsRemovablePatch(stateStr, uninstallableRead.Value),
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

        ConfirmRemovableAgainstEveryProduct(claimed, patchClaims, products, missed.Recovered,
            fallback.ProductPatchSets, apiPatchSets, ct);

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

        // LIVE, AND ON NO ACCOUNT TO BE DELETED AS DEAD MACHINERY. This note said
        // the opposite until 3.0.0 restored the superseded offer, and the old
        // wording was true only while that class was not offered: with nothing
        // removable in the result no row reached here, so the loop ran over
        // nothing. Rows reach it again. A superseded row on a machine whose patch
        // sets read clean arrives here still carrying IsRemovable, and this loop
        // is what takes it off the offer when the scan lost a claim.
        //
        // Measured rather than argued: the same machine that offers such a patch
        // withholds it once one product's LocalPackage read fails, which is this
        // loop and nothing else.
        //
        // NOT TO BE CONFUSED WITH THE REFUSAL GATE ABOVE, which weighs the same
        // count and is very much alive; see its own note for why.
        //
        // What it does: a scan that loses any claim withholds the whole removable
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
        //
        // AND IT TOUCHES NOTHING ELSE, WHICH IS A DECISION RATHER THAN THE ABSENCE OF
        // ONE. It ran a second arm until 3.0.0 that cleared the unread-file marker on
        // a row something else had already withheld, so that the missing-files split
        // would treat such a row as unaccounted for. That arm is gone and must not
        // come back by a different name.
        //
        // WHAT THE MARKER MEANS IS WHY. It records that the ONLY reason the row lost
        // its verdict was that the pass reading the patch file could not read it, and
        // the split reads it for one population: rows whose file has GONE. For those
        // the failed read is the read of the very file whose absence is the subject.
        // Nobody can perform it, on any machine, ever, and it fails identically
        // whatever removed the file. Clearing it turned that tautology into a reason
        // to warn, so a run that came up short somewhere ELSE printed an alarm about a
        // file this scan had positively established nothing could reach for.
        //
        // AND THE COUNT IT FIRED ON DOES NOT NAME THAT ROW'S RISK. Its three terms are
        // a read that failed on a product this loop DID return, a product the registry
        // saw and the enumeration did not whose own file is present, and a registry key
        // Windows would not answer about. None of them is "a holder of this patch went
        // unseen", which is the condition that would bear on this file. The count is a
        // proxy for a degraded machine and the arm applied it as a per-file verdict.
        //
        // WHAT ANSWERS THE RESIDUAL IS THE WITHHOLDING ABOVE, AND IT IS UNTOUCHED. The
        // machine may indeed be short of a product that could roll back onto a cached
        // patch, and on this run the app therefore removes no superseded patch at all.
        // That protects every file where protecting one is still possible. The file
        // already gone is not one of them, and printing a sentence about it is not a
        // second line of defence.
        //
        // THE SPLIT IS NOT LEFT WITHOUT A ROUTE TO THIS STATE. A run whose machine-wide
        // patch enumeration did not answer downgrades every removable path with no
        // marker set (see ConfirmRemovableAgainstEveryProduct), so a missing superseded
        // row on such a run reaches the split withheld and unmarked and is reported.
        // That is the run where the app really did fail to establish something about
        // this patch, rather than the run where something else on the machine failed.
        if (withheldProducts > 0)
            for (var i = 0; i < packages.Count; i++)
                if (packages[i].IsRemovable)
                    packages[i] = packages[i] with { IsRemovable = false, RemovableWithheld = true };

        // The run's whole path census: this loop's, plus the fallback's own.
        var paths = new PathCensus();
        paths.Add(pathCensus);
        paths.Add(fallback.Paths);

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
                packages.Count(p => p.VerdictUnreadable),
                instanceProducts,
                instanceTypeUnreadable,
                fallback.ProductPatchKeys,
                fallback.ProductPatchRegistrations,
                fallback.ProductsWithRemovablePatch,
                fallback.ProductsWithPatchSetUnestablished,
                // BOTH HALVES OF THE SCAN, ADDED. The API loop and the registry
                // fallback each normalise the paths they read and neither can see
                // the other's, so a census taken from either alone would report a
                // fraction of the machine as the whole of it. Added here rather
                // than shared as one object through both, so the fallback stays a
                // function of its own inputs.
                paths.ResolverAttempts,
                paths.ResolverNotAPath,
                paths.ResolverNoExistingAncestor,
                paths.ResolverOpenRefused,
                paths.ResolverFinalNameUnavailable,
                paths.ResolverFaulted,
                paths.NormalisationRefusedAtExpansion,
                paths.NormalisationRefusedAtPrefixStrip,
                paths.NormalisationRefusedAtFullPath,
                paths.NormalisationRefusedAtEmbeddedNull,
                paths.FlaggedSpellings));
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

            var resolved = ResolveProductInstance(_msi, code);
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
    /// returning ERROR_UNKNOWN_PATCH. A machine with nothing removable pays for
    /// the machine-wide enumeration and the per-product condition and nothing
    /// else, both of which it needs: the condition's second consumer is the
    /// missing-file split, and a machine with nothing to offer is exactly the
    /// machine where that is the only consumer there is.
    ///
    /// The two outcomes use the two meanings the row already has, so this adds no
    /// vocabulary. A product that holds the patch and still needs it makes the row
    /// plainly non-removable, exactly as the merge's own downgrade does. A read
    /// that could not answer makes it non-removable AND withheld, which is the
    /// existing "this scan could not prove it" state, counted and surfaced as such.
    ///
    /// IT IS THE CONDITION THE SUPERSEDED OFFER RESTS ON, and this note said the
    /// opposite until 3.0.0 restored that offer. While the class was not offered
    /// no row carried a removable verdict for it to confirm, so it returned at its
    /// first guard and cost nothing. Rows reach it again, and a superseded patch
    /// is offered only where this pass has asked every product it knows of and
    /// none of them still holds it. Emptiness here is a machine with nothing
    /// removable, never a mechanism that is not needed.
    ///
    /// AND EMPTINESS NO LONGER RETURNS AT THE TOP, which is a separate statement
    /// and the one most likely to be undone by somebody restoring an obvious
    /// saving. The per-product condition this method hosts is read by the offer
    /// AND by the missing-file split, and on a machine with nothing to offer the
    /// split is its only reader. Returning before it left every patch row at the
    /// type's default, which the split reports, so a missing obsoleted
    /// registration was named or not according to whether an unrelated program
    /// happened to hold an offer-eligible patch that day.
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
        IReadOnlyDictionary<string, ProductPatchSet>? registryPatchSets,
        IReadOnlyDictionary<string, ProductPatchSet> apiPatchSets,
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

        // AN EMPTY WORK LIST USED TO RETURN HERE, and the guard has moved below the
        // per-product pass rather than being deleted. Everything from here to that pass
        // is what the pass needs; everything after it is the per-pairing work, which an
        // empty list really does make pointless.
        //
        // WHY IT COULD NOT STAY. The pass has two consumers and only one of them is the
        // offer. The other is the missing-files split, which reads the verdict for rows
        // whose file has gone, and those two sets are disjoint: a missing file is never
        // offered. So a machine with nothing to offer is precisely a machine where the
        // split is the only reader, and returning here left every row at the type's
        // default of Unestablished, which the split reports.
        //
        // WHAT THAT DID, and it is why this is a fix rather than a tidy. Whether a past
        // user was warned about a file an earlier release removed turned on whether some
        // UNRELATED program on the machine happened to hold an offer-eligible superseded
        // patch that day. Nothing to do with the file, the registration or the risk. The
        // class it moved is exactly one: an obsoleted patch whose Uninstallable read a
        // positive zero, which is precisely what every release up to v2.3.0 offered and
        // removed.
        //
        // AND IT IS CHEAPER THAN THE GUARD MADE IT LOOK. The expensive half of this
        // method is the per-pairing property reads and the patch-file reads, and neither
        // happens on such a machine: the pairing loop is below the moved guard, and the
        // pass reads a patch file only for a row that is still removable, of which there
        // are none. The two patch-set maps are built before this method is called at all.
        // What is left is the machine-wide enumeration below, which reads no file and
        // which every machine that offers anything already pays for on every scan.
        //
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

        // ROUTE B, READ ONCE PER PATH AND SHARED BY BOTH PASSES BELOW. The file names
        // the products it may be applied to, so it answers about a product no
        // enumeration returned, which is what makes it the cover for route A's
        // documented blind spot. It was reaching the per-pairing pass alone.
        //
        // MEMOISED BECAUSE THE READS ARE THE EXPENSIVE PART AND THE ANSWER CANNOT
        // CHANGE WITHIN ONE SCAN. A path named by two patch codes was read twice
        // before this, once per pairing, and both passes now want the same answer.
        // The cache is per call and dies with it, so nothing is carried between
        // scans and no staleness is possible.
        //
        // THE RESOLVE HAPPENS HERE AND NOT AT EITHER CONSUMER, because a declared
        // target is a product code and nothing more, and the two things a caller
        // needs to know about it are decided by the same call: whether it is
        // installed at all, and, if it is, which account and context to ask in. A
        // code the file names and the machine does not hold contributes nothing and
        // is not a failure; a code that could not be asked about withholds.
        var declaredByPath = new Dictionary<string, DeclaredTargets>(StringComparer.OrdinalIgnoreCase);
        DeclaredTargets DeclaredTargetsFor(string patchPath)
        {
            if (declaredByPath.TryGetValue(patchPath, out var already)) return already;

            var declared = TargetsDeclaredByPatchFile(patchPath, out var unreadable);
            var installed = new List<(string ProductCode, string? Sid, MsiInstallContext Context)>();
            var unaskable = false;
            foreach (var target in declared)
            {
                // EVERY TARGET IS RESOLVED EVEN ONCE ONE HAS FAILED, where the pairing
                // pass used to stop at the first. The outcome for the path is the same
                // either way, that path being withheld on the flag below, and reading
                // the rest is what makes one cached answer serve both consumers rather
                // than depending on which of them asked first.
                var resolved = ResolveProductInstance(_msi, target);
                if (resolved.Unaskable) { unaskable = true; continue; }
                if (resolved.Installed) installed.Add((target, resolved.Sid, resolved.Context));
            }

            return declaredByPath[patchPath] = new DeclaredTargets(installed, unreadable, unaskable);
        }

        // THE PER-PRODUCT CONDITION, RUN BEFORE THE PER-PAIRING WORK BELOW because
        // it can settle a path outright and the pairing reads are the expensive
        // half. It asks a different question from everything else in this method:
        // the rest confirms that no product claims this patch as still needed, and
        // this asks whether anything on a product sharing the patch could be
        // uninstalled and reach for its file.
        JudgeAndWithholdAgainstEveryProductPatchSet(
            claimed, patchClaims, holders, registryPatchSets, apiPatchSets,
            DeclaredTargetsFor, ct);

        // NOW the empty work list settles it. Everything below is per-pairing and there
        // are no pairings to ask about; see the note where this guard used to sit.
        if (toConfirm.Count == 0) return;

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

            // Both withholdings are the same shape and were the same shape before
            // this read was shared: a patch whose own declaration will not be read
            // has been shown to be unneeded by nobody, and a product it names that
            // Windows will not answer about is a question left open rather than an
            // answer of no.
            var fromFile = DeclaredTargetsFor(path);
            if (fromFile.Unreadable || fromFile.Unaskable)
            {
                // WHICH OF THE TWO IT WAS IS RECORDED, AND RECORDING IT CHANGES
                // NOTHING HERE. Both still take the verdict away and both still keep
                // the file. The flag is read much later, by the missing-files split,
                // and only ever for a row whose file turned out not to be there.
                //
                // IT HAS TO BE RECORDED RATHER THAN WORKED OUT LATER, because an
                // unread declaration carries two meanings and this is the only place
                // that knows which was met. A file that is THERE and will not give up
                // an identity is the app unable to establish something it could have
                // established. A file that is NOT THERE cannot be read by anybody, so
                // the same withholding is a tautology and says nothing about the
                // machine. This class cannot tell them apart, having no filesystem to
                // ask, and must not guess: FileSystemScanService stamps FileExists
                // against the same filesystem it walks, and the two facts meet there.
                //
                // WHAT IT COST WHILE THEY WERE ONE THING. The app offered a superseded
                // patch, removed it, and the next scan warned that a repair could fail
                // on that very file, because this read had failed for the only reason
                // it could: the file was the one the app had just taken away.
                Downgrade(claimed, path, withheld: true, unreadableFile: fromFile.Unreadable);
                continue;
            }
            toAsk.AddRange(fromFile.Installed);

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
    /// One patch file's route B reading, resolved against the machine, in the form
    /// both consumers of it need.
    ///
    /// THE TWO FLAGS ARE NOT THE SAME FINDING AND NEITHER IS AN EMPTY LIST. A patch
    /// that declares targets none of which are installed yields an empty
    /// <paramref name="Installed"/> with both flags clear, and that is a positive
    /// answer: nothing on this machine holds it, so nothing on this machine can roll
    /// back onto its file. <paramref name="Unreadable"/> is the file declining to say
    /// what it targets, and <paramref name="Unaskable"/> is Windows declining to say
    /// where a declared target lives. Both leave the question open and both withhold.
    /// </summary>
    private readonly record struct DeclaredTargets(
        IReadOnlyList<(string ProductCode, string? Sid, MsiInstallContext Context)> Installed,
        bool Unreadable,
        bool Unaskable);

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
    /// True where the file did not yield an identity, WHICH INCLUDES A FILE THAT IS
    /// NOT THERE. This said the opposite until 2026-08-21, and the sentence it carried
    /// ("an absent file is not unreadable: there is nothing there to remove and nothing
    /// to withhold") described an intention the code has never had: the read below is
    /// the only test, and a path naming no file fails it like any other. A patch whose
    /// own declaration cannot be read has not been shown to be unneeded by anybody, so
    /// the caller withholds rather than proceeding on the other two routes alone, and
    /// it does that for an absent file too.
    ///
    /// THE ABSENT CASE IS SEPARATED BY THE CALLER AND NOT HERE, because separating it
    /// here would need a filesystem this class does not have, and asking the real one
    /// would answer about a different machine from the one the scan is walking.
    /// </param>
    private IReadOnlyList<string> TargetsDeclaredByPatchFile(string path, out bool unreadable)
    {
        unreadable = false;

        // Only a patch has a Template to read. A cached product package is not
        // this route's business and its absence of one is not a failure.
        if (!path.EndsWith(".msp", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<string>();

        // A FILE THAT IS NOT THERE IS LEFT TO THE READ BELOW AND FAILS IT. The
        // paragraph that stood here said the outcome was the same either way and that
        // no difference could be observed, and both halves held until the missing-files
        // split began reading the withheld flag. After that the difference was a
        // warning on the main window naming the program whose patch this app had just
        // correctly removed. Nothing is tested for here still: the caller records WHICH
        // failure this was, and the scan, which is holding the filesystem, decides what
        // it meant.
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
    /// <remarks>
    /// STATIC AND SHARED RATHER THAN COPIED, because
    /// <see cref="DeclaredProductCheck"/> has to put the identical question about
    /// a product code a cached package declared. What is worth sharing is not the
    /// buffer dance: it is <see cref="IsProductNotInstalled"/>, the allowlist that
    /// decides which returns may be read as absence. A second copy of that is a
    /// second place for a return to be added to, or not added to, and the
    /// direction it fails in is a file offered on a question that was never
    /// answered.
    /// </remarks>
    internal static (bool Installed, bool Unaskable, string? Sid, MsiInstallContext Context)
        ResolveProductInstance(IMsiApi msi, string productCode)
    {
        var installedCode = new char[Msi.GuidBufferLength];
        var sidBuffer = new char[SidBufferLength];
        uint sidLength = SidBufferLength;

        var error = msi.EnumProducts(
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
            error = msi.EnumProducts(
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
    /// <param name="unreadableFile">
    /// Records that the read which established nothing was the patch file's own
    /// declaration. A cause and not a second verdict: every caller passing it also
    /// passes <paramref name="withheld"/> true, and the file is kept either way. It
    /// travels because that one cause is a real inability for a file that is present
    /// and a tautology for one that is not, and only a reader holding the filesystem
    /// can say which. See <see cref="RegisteredPackage.WithheldOnUnreadableFile"/>.
    /// </param>
    private static void Downgrade(
        Dictionary<string, RegisteredPackage> claimed, string path, bool withheld,
        bool unreadableFile = false)
    {
        if (!claimed.TryGetValue(path, out var row) || !row.IsRemovable) return;
        claimed[path] = row with
        {
            IsRemovable = false,
            RemovableWithheld = withheld,
            WithheldOnUnreadableFile = unreadableFile,
        };
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
    /// AN ENVIRONMENT-VARIABLE FORM IS ANOTHER SUCH SPELLING AND IS EXPANDED HERE
    /// FROM 3.0.0. A value spelled <c>%SystemRoot%\Installer\1e038.msi</c> is a
    /// claim on a real cached file, and nothing in this application expanded one:
    /// <see cref="CarriesFlaggedSpelling"/> answers false for a <c>%</c>, so the
    /// value fell through to GetFullPath, which completed it from the process's
    /// working directory and produced a well-formed path naming nothing. The claim
    /// then failed to match the walk and the file it meant was offered as
    /// unclaimed, which is the one way a spelling fault puts a needed file in front
    /// of somebody rather than merely mis-filing a row.
    ///
    /// THE DEFECT WAS THAT BEHAVIOUR TURNED ON A REGISTRY VALUE'S TYPE. .NET
    /// expands a <c>REG_EXPAND_SZ</c> as part of reading it, so the registry
    /// fallback already coped with that form without anything here deciding to
    /// (<see cref="TryReadLocalPackage"/>, where it is now explicit). The
    /// <c>REG_SZ</c> value holding the same text was expanded nowhere, and neither
    /// was anything the API side returned. Two registrations naming one location,
    /// one stored expandable and one stored plain, got different answers, and no
    /// comment anywhere said so or meant it.
    ///
    /// WHAT THE EXPANSION DOES TO THE OFFER, ONE LINE PER HALF, because the two
    /// halves reach the list by opposite routes and no one sentence is true of both.
    /// A walked file is offered when no registration names it, so a value that now
    /// resolves takes its file OFF the list: the registration matches, and the file
    /// is claimed and kept. A value that expands to somewhere else either names
    /// nothing, which is exactly the old behaviour, or names some other file, which
    /// is then claimed and kept in its place. A registered superseded patch is on
    /// the list BECAUSE of its registration, and there the expansion can ADD:
    /// <c>%SystemRoot%\Installer\1e038.msi</c> named nothing, so the row read as
    /// missing from disk and the branch that offers it is gated on the file being
    /// there; expanded, the row names the file that is really there and can reach
    /// the offer.
    ///
    /// AND WHAT MAKES THAT SAFE IS NOT THIS METHOD. Such a row is put to the same
    /// per-product condition, the same confirmation pass and the same act-time
    /// re-verify as every other row on the machine. The expansion settles which file
    /// a registration names and settles nothing about whether that file may go, so a
    /// row it repairs arrives at the offer's conditions unprivileged and is judged
    /// there. That is the whole argument and there is nothing in it that a later
    /// release can falsify: this paragraph used to say the expansion could only ever
    /// move a file OFF the offer, which was true only while the superseded class was
    /// not offered at all, and 3.0.0 put that class back.
    ///
    /// HOW OFTEN THE FORM OCCURS IS NOT WHAT MAKES THE HANDLING RIGHT, so no
    /// prevalence finding stands behind it and none is needed. What there is is one
    /// reading: all 296 path values across the three SIDs of one elevated machine
    /// were plain absolute drive paths, zero containing a <c>%</c> (read 2026-08-16;
    /// one machine cannot show that the form never occurs, which is the reason for
    /// handling it rather than waiting to find out).
    ///
    /// AND ONE VALUE IS REFUSED BEFORE THE EXPANSION RUNS AT ALL. A recorded value
    /// carrying an embedded null is never put through it: on Windows that call cuts
    /// the value at the null and returns silently, so a claim that should have been
    /// unspellable becomes a well-formed path naming whatever is left, and nothing
    /// downstream is told. Such a value comes back raw and counted, like every other
    /// this method cannot spell, and the body says why the test has to come first.
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
    /// does at every containment gate. EVERY RECORDED PATH IS ASKED, FROM 3.0.0,
    /// where until then only a path announcing one of the two spellings in its own
    /// characters was.
    ///
    /// THE INVARIANT THAT BUYS IS THE REASON FOR THE CHANGE, and it is worth more
    /// than the spellings it settles. Every claim leaving this method is now EITHER
    /// a location the kernel proved OR one whose failure to resolve has been counted,
    /// and a counted failure withholds the whole walk-derived offer
    /// (<c>EnumerationCensus.AnyRecordedPathUnestablished</c>). There is no third
    /// case. So anything downstream comparing a claim is comparing a proven path,
    /// and a reader asking "could this claim be wrong about where its file is" has
    /// one answer rather than a case analysis.
    ///
    /// THE COST ARGUMENT THAT KEPT THE ASK NARROW IS SPENT, and this is what
    /// replaced it. It said a handle per registration was too much to pay. But
    /// <c>CandidateGuard.CheckSafeToRemove</c> already calls
    /// <see cref="InstallerCacheHelpers.TryResolveFinalPath"/> once per walked
    /// CANDIDATE, which is the same call and the far larger population: that is why
    /// the resolver rents its buffer rather than allocating one, a decision taken
    /// for a folder reaching 800,000 files. Registrations number in the hundreds on
    /// every machine measured. The ask was being kept off the small side of a cost
    /// the large side already pays.
    ///
    /// AND IT REPAIRS A CLAIM, WHICH IS WHAT SEPARATES IT FROM THE IDENTITY MATCH
    /// AND IS WHY BOTH EXIST. <c>FileSystemScanService</c> also reconciles a
    /// differently-spelled registration by opening both sides and comparing file
    /// identity, and that is the more general of the two for the candidate list: it
    /// reconciles any spelling at all, hard links and junctions included. But it
    /// SUBTRACTS from the candidate list and never repairs the claim, so it feeds
    /// nothing else. The correlation gate that refuses a scan outright counts
    /// registrations whose recorded path LEXICALLY names a file in the walked folder
    /// (<c>FileSystemScanService.NamesFileDirectlyIn</c>), and an unsettled spelling
    /// silently withholds its row from that count while the identity pass is
    /// structurally unable to put it back. The missing-from-disk counts and the
    /// registered-files window read the claim the same way. Resolving here is what
    /// makes all of them true.
    ///
    /// THE PREFIX IS NORMALISED BEFORE THE ASK, and that is not tidying. The NT
    /// object form (<c>\??\</c>) and the Win32 escape (<c>\\?\</c>) name the same
    /// object, which is why StripLongPathPrefix takes either off a drive-rooted
    /// path; over a volume GUID neither comes off, and the NT form then has its
    /// leading separator read as rooted on whatever drive the process is running
    /// from. Handing the resolver the Win32 spelling is what stops the resolution
    /// answering about a path assembled out of the running process's location.
    ///
    /// WHAT THIS STILL DOES NOT DO, and what stopped following from it. A flagged
    /// path the kernel declines to RESOLVE is still kept in the spelling Windows
    /// gave, and its claim still fails to match anything the walk produces. That much
    /// is unchanged and cannot be improved here. What no longer follows is that its
    /// file is offered: from 3.0.0 the refusal is counted, and
    /// <c>EnumerationCensus.AnyRecordedPathUnestablished</c> withholds the whole
    /// walk-derived offer on it, because the app cannot say WHICH candidate the
    /// unresolved claim meant and so cannot hold back a narrower set. This paragraph
    /// used to end "and its file is still offered", which was true when it was
    /// written and is the sentence to check against the code rather than against its
    /// own reasoning. (Resolve, not expand: two different operations are in this
    /// method and the one word was doing for both. The kernel resolving a final
    /// path is <see cref="InstallerCacheHelpers.TryResolveFinalPath"/>, which
    /// answers yes or no; expanding an environment variable is the paragraph above,
    /// which has no failure to report. What it does with a variable the machine has
    /// never heard of is pinned by a test rather than asserted here, that being a
    /// property of the platform call and not of this code.)
    ///
    /// Measured on one elevated machine (Windows 10.0.26200, 2026-08-03): 138
    /// registered paths, every one an ordinary drive path, no tilde-and-digit
    /// anywhere in any of them, and the cache folder had no short name on that
    /// volume. That says how exposed one machine was, and nothing about whether
    /// another holds one.
    /// </summary>
    private static string NormaliseLocalPackagePath(string value, PathCensus census)
    {
        // THE NULL IS TESTED BEFORE ANYTHING IS ATTEMPTED ON THE VALUE, AND IT IS THE
        // ONE ORDERING THAT WORKS. On Windows ExpandEnvironmentVariables TRUNCATES a
        // value holding an embedded null and does not throw: measured on CI, where
        // C:\Windows\Installer\bad\0name.msi came back as C:\Windows\Installer\bad,
        // cut at the null. Nothing throws, so the catch below never runs, so no
        // refusal is counted, so the withholding never fires. Putting this test after
        // the expansion would run it against a string the null had gone from.
        //
        // AND THE TRUNCATED VALUE IS THE DANGEROUS HALF, not the missing count. What
        // comes out is a WELL-FORMED PATH, which on a real machine can match a real
        // file: the claim would then be filed against a file that needed no claim
        // while the file the registration meant stays unclaimed. A raw value carrying
        // a null can match nothing, so this test costs the offer nothing it was
        // entitled to.
        //
        // ON THE RAW VALUE, NEVER ON "THE EXPANSION SHORTENED IT". A variable
        // legitimately expands to something shorter than its own name, so a length
        // test would refuse ordinary paths. A path cannot carry a null, so its
        // presence is refusal by definition: exact, and free.
        //
        // IT ALSO TAKES A PLATFORM DIFFERENCE OUT OF THE MECHANISM, which is how this
        // survived. Off Windows the same call returns the value untouched and
        // GetFullPath then throws, so this one input was refused at a different step
        // on each platform, and the harness that could reach it was the one that could
        // not see the fault. A string test behaves the same everywhere, so both
        // platforms now refuse the value here.
        if (value.Contains('\0'))
        {
            census.RecordNormalisationRefusal(NormalisationStage.EmbeddedNull);
            return value;
        }

        // A MARKER IN SCOPE RATHER THAN A TRY BLOCK PER STAGE, and the reason is what
        // this method is: the last thing between a registry value and a claim, whose
        // refusal behaviour is pinned by a test. Splitting the try to sharpen a
        // counter would restructure the control flow of a safety-critical path to
        // improve instrumentation, which is the wrong way round. The marker costs an
        // assignment and the catch reads it.
        //
        // THE FOUR ARE COUNTED APART BECAUSE THEY ARE NOT ONE FINDING. A value
        // carrying a character no path can carry, one the expansion refused, one the
        // prefix work refused and one GetFullPath refused are four different things
        // about a machine, and a single counter named for any one of them would be
        // false of the other three. What they share, and the only thing any sentence
        // may say over all four, is that the recorded path could not be turned into a
        // path.
        var stage = NormalisationStage.Expansion;
        try
        {
            // BEFORE THE PREFIX STRIP, and the order is load-bearing rather than
            // incidental. StripLongPathPrefix takes a prefix off a DRIVE-ROOTED
            // path, and \??\%SystemRoot%\... is not drive-rooted as text, so
            // stripping first leaves the prefix on and hands GetFullPath a string it
            // reads as rooted on whatever drive the process is running from.
            // Expanding first makes it drive-rooted and the strip then works as it
            // always did. On a value holding no % this is the identity, so nothing
            // that reached here before reaches anything different now.
            var expanded = Environment.ExpandEnvironmentVariables(value);

            stage = NormalisationStage.PrefixStrip;
            var stripped = InstallerCacheHelpers.StripLongPathPrefix(expanded);

            // The test runs on the stripped value rather than the fully
            // normalised one because GetFullPath destroys the evidence it needs:
            // a prefix it cannot root is folded into an ordinary-looking path,
            // and a trigger that has been normalised away cannot be tested for.
            //
            // Only a proven expansion is taken. A false return means the kernel
            // never expanded this path, so its out value is the same string by
            // another route and using it would dress a guess as an answer.
            // COUNTED BEFORE THE ASK AND NO LONGER GATING IT. The two spellings
            // announce themselves in the string, and until 3.0.0 that character scan
            // decided whether a handle was opened at all. It decides nothing now:
            // every recorded path is resolved. What the scan still answers is how
            // many of a machine's recorded values carry such a spelling, which is a
            // fact about that machine this project has been trying to size and which
            // the attempts count stopped being able to report the moment the ask
            // widened to everything.
            if (CarriesFlaggedSpelling(stripped)) census.RecordFlaggedSpelling();

            // COUNTED WHETHER IT ANSWERS OR NOT, which is the whole use of the
            // number: the five failures below are meaningless without how many times
            // anything was asked. That mattered most when most machines never asked
            // at all; it still separates a scan that read no registrations from one
            // whose every registration resolved.
            census.RecordResolverAttempt();
            var outcome = InstallerCacheHelpers.ResolveFinalPathOutcome(
                ToWin32Prefix(stripped), out var resolved);
            census.RecordResolution(outcome);

            if (outcome == PathResolution.Resolved) return resolved;

            // ONLY A CLAIM THE RESOLVER REFUSED REACHES THIS, and the refusal has
            // been counted one line above, so the offer is already being withheld by
            // the time this value is used for anything. What it produces is the best
            // spelling available for a claim nothing is going to act on.
            stage = NormalisationStage.FullPath;
            return Path.GetFullPath(stripped);
        }
        catch
        {
            // A value GetFullPath refuses (a device name, a length past the API's
            // limit) is kept exactly as Windows returned it. It cannot be improved,
            // and dropping the claim would turn an unreadable spelling into an
            // orphaned file. The embedded null used to be named here as the third
            // and is refused above instead, because on Windows it never reaches
            // this call: the expansion truncates it away without throwing.
            //
            // AND THE FACT IS NOW CARRIED OUT RATHER THAN ENDING HERE. What leaves
            // this method is a claim that cannot match anything the folder walk
            // produces, so the file it means is offered as unclaimed. Until this
            // release nothing downstream was told, and the count is the first step
            // in finding out how often it happens on a real machine.
            census.RecordNormalisationRefusal(stage);
            return value;
        }
    }

    /// <summary>
    /// Whether a prefix-stripped path carries a spelling only the filesystem can
    /// settle. A tilde followed by a digit is the 8.3 alias form. A surviving
    /// prefix, either form, is what
    /// <see cref="InstallerCacheHelpers.StripLongPathPrefix"/> leaves on a path with
    /// no drive root, which in this position means a volume-GUID or device path.
    ///
    /// IT DECIDES NOTHING. It was the gate on the final-path resolution until 3.0.0,
    /// which is what the old name said, and every recorded path is resolved now
    /// whatever this answers. What it does is COUNT, into
    /// <see cref="PathCensus.FlaggedSpellings"/>, and the reason that survived the
    /// gate is that the two questions came apart when the ask widened.
    /// <see cref="PathCensus.ResolverAttempts"/> used to answer both, being the
    /// number of paths asked about AND therefore the number carrying such a
    /// spelling; it now answers only the first. Losing the second would have been a
    /// measurement quietly disappearing from the only instrument this project has,
    /// and a report that stops being able to answer a question reads exactly like a
    /// machine that has nothing to report.
    ///
    /// It over-selects deliberately, and what that costs has changed with its job. A
    /// long name may legitimately hold a tilde-and-digit; as a gate a false positive
    /// cost one handle on a path that resolved to itself, and as a count it inflates
    /// a figure nothing acts on. A false negative used to cost a file and now costs
    /// nothing at all, the resolution no longer depending on it.
    /// </summary>
    internal static bool CarriesFlaggedSpelling(string path)
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
        var productPatchKeys = 0;
        var productPatchRegistrations = 0;
        var patchSets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase);
        var pathCensus = new PathCensus();

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
                    productPatchKeys += sidRead.ProductPatchKeys;
                    productPatchRegistrations += sidRead.ProductPatchRegistrations;
                    pathCensus.Add(sidRead.Paths);
                    // Worsening merge across SID subtrees: one product code can be
                    // registered under several, and whichever subtree the walk
                    // reached first must not settle a disagreement between them.
                    if (sidRead.ProductPatchSets is not null)
                        foreach (var (code, set) in sidRead.ProductPatchSets)
                            patchSets[code] = patchSets.TryGetValue(code, out var seen)
                                ? Worse(seen, set)
                                : set;
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
            nonStringValues, productCodes, unparseableKeyNames, patchSets,
            productPatchKeys, productPatchRegistrations,
            // Counted off the merged map rather than tallied per SID, for the reason
            // the verdict-unreadable count is: a product registered under two
            // subtrees is one product here and two increments there.
            patchSets.Values.Count(v => v == ProductPatchSet.RemovablePatchPresent),
            patchSets.Values.Count(v => v == ProductPatchSet.Unestablished),
            pathCensus);
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
        // This subtree's own tally of how its recorded paths turned out, folded into
        // the run's by the caller.
        var pathCensus = new PathCensus();
        var failures = 0;
        var productKeys = 0;
        var unclaimedProductFiles = 0;
        var unclaimedPatchFiles = 0;
        var nonStringValues = 0;
        var unparseableKeyNames = 0;
        var productPatchKeys = 0;
        var productPatchRegistrations = 0;
        var patchSets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase);

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

                    // The per-product patch set, read here because this loop is
                    // already standing on the key it lives one level below, so it
                    // costs one OpenSubKey per product and no second walk of
                    // UserData. Its own try/catch and its own cause: a product whose
                    // patch list will not read is a different diagnosis from one
                    // whose InstallProperties will not, and the budget keys on the
                    // cause string.
                    //
                    // Keyed on the UNPACKED code because that is what every caller
                    // holds; a key name that would not unpack has no code to ask
                    // about and is already counted above as withholding for that
                    // reason.
                    if (unpacked is not null)
                    {
                        try
                        {
                            var set = ReadProductPatchSet(productsKey, prodGuid,
                                ref productPatchKeys, ref productPatchRegistrations);
                            patchSets[unpacked] = patchSets.TryGetValue(unpacked, out var existing)
                                ? Worse(existing, set)
                                : set;
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            failures++;
                            // The verdict is written rather than left absent, so a
                            // product whose read threw is on record as unestablished
                            // instead of as a product nobody asked about. An absent
                            // entry and an unestablished one must not be the same
                            // thing to a caller.
                            patchSets[unpacked] = ProductPatchSet.Unestablished;
                            failureLog.Record(ex, cause: "product-patches");
                        }
                    }

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
                            var path = NormaliseLocalPackagePath(localPkg, pathCensus);
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
                            var path = NormaliseLocalPackagePath(localPkg, pathCensus);
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
            nonStringValues, null, unparseableKeyNames, patchSets,
            productPatchKeys, productPatchRegistrations,
            Paths: pathCensus);
    }

    /// <summary>
    /// One product's patch-set verdict, unioned across the sources that can see its
    /// patches. Every source can only ADD a patch to the set, so unioning costs reads
    /// and can only ever withhold more.
    ///
    /// THE REGISTRY IS WHAT MAKES THE SET TRUSTWORTHY AND THE API IS WHAT STOPS IT
    /// RESTING ON ONE READING. The registry side is a key listing, so it has no index
    /// and no early end to be blind to, which is the fault every enumeration of a
    /// patch set shares. The API side is the same pairings the product loop already
    /// read, so it costs nothing and it catches a patch the registry's own key does
    /// not list.
    ///
    /// AN ABSENT REGISTRY ENTRY IS AN INABILITY AND AN ABSENT API ENTRY IS NOT, which
    /// looks inconsistent and is the point. The registry walk visits every product key
    /// on the machine and writes a verdict for each, so a product missing from it is a
    /// product the walk could not account for. The API map is built only from patches
    /// the loop actually read, so a product missing from it is usually a product with
    /// no patches, which is not a failure to establish anything. A product with no
    /// registry entry therefore withholds, and in production that is either a machine
    /// whose <c>UserData</c> would not open at all, which the degraded-sources gate
    /// also sees, or a test that supplied no patch sets.
    /// </summary>
    internal static ProductPatchSet ProductVerdict(
        string productCode,
        IReadOnlyDictionary<string, ProductPatchSet>? registryPatchSets,
        IReadOnlyDictionary<string, ProductPatchSet> apiPatchSets)
    {
        var fromRegistry = registryPatchSets is not null
            && registryPatchSets.TryGetValue(productCode, out var r)
                ? r
                : ProductPatchSet.Unestablished;

        return apiPatchSets.TryGetValue(productCode, out var a)
            ? Worse(fromRegistry, a)
            : fromRegistry;
    }

    /// <summary>
    /// Withholds every still-removable path that any product could roll back onto.
    ///
    /// THE DEFECT THIS CLOSES, MEASURED RATHER THAN REASONED. The rule that decided
    /// this until 3.0.0 read the SUPERSEDED patch's own removability, and the risk
    /// turns on the SUPERSEDING patch's. Uninstalling patch C with the superseded
    /// patches' cached files present rolled a product back one step correctly; with
    /// those files missing it went all the way to the unpatched base, discarded both
    /// patches and reported success, and the log carries Windows looking for the
    /// absent files by name. So removing a superseded patch's cached file can silently
    /// cost somebody a security update, in exactly the operation Microsoft always
    /// named as the reason the file is cached.
    ///
    /// SO THE CONDITION IS ABOUT THE PRODUCT AND ABOUT EVERY PRODUCT. A superseded
    /// patch is cached once and registered once per product it applies to, and its one
    /// file is shared by all of them, so a rollback on ANY of those products reaches
    /// for it. A condition holding only for the product a loop happened to be standing
    /// in would offer a file that a second product's removable patch can still need.
    /// One file was measured carrying four registrations across two products, read on
    /// or before 2026-08-17. What it establishes is that the shape occurs, which no
    /// later reading can take back; how many any machine holds today is a different
    /// question and this figure does not answer it.
    ///
    /// THE PRODUCTS ARE UNIONED TOO, not just the patches. The claims name the
    /// products the enumeration reached; route A names products it never returned;
    /// the patch file's own declared targets name products no enumeration on the
    /// machine has to have mentioned at all. A product any of the three names is a
    /// product the condition has to hold for.
    ///
    /// THE THIRD SOURCE IS THE ONE THAT COVERS ROUTE A'S DOCUMENTED BLIND SPOT, and
    /// it reached the per-pairing pass first and this condition second. While it was
    /// missing here, a product that route A cannot see and that carries no claim for
    /// this path was never put into the set, so its removable patch was never seen,
    /// and this condition could answer AllNonRemovable for a file that product can
    /// still reach for. The per-pairing pass below would then ask that product about
    /// the patch and be told, truthfully, that it holds it and cannot uninstall it,
    /// which is an answer to a different question.
    ///
    /// WHAT IT CANNOT PROMISE, so no copy may say otherwise: the condition is read
    /// here and re-read at act time, and neither is a statement about the future. A
    /// patch that is non-removable today can be replaced tomorrow by one that is not.
    /// That is true of every check this app makes and is not a defect here.
    /// </summary>
    private static void JudgeAndWithholdAgainstEveryProductPatchSet(
        Dictionary<string, RegisteredPackage> claimed,
        List<PatchClaim> patchClaims,
        Dictionary<string, List<(string ProductCode, string? Sid, MsiInstallContext Context)>>? holders,
        IReadOnlyDictionary<string, ProductPatchSet>? registryPatchSets,
        IReadOnlyDictionary<string, ProductPatchSet> apiPatchSets,
        Func<string, DeclaredTargets> declaredTargets,
        CancellationToken ct)
    {
        // The patch codes naming each PATCH path, which is what decides which products
        // the path has to be clean against. Several codes can name one path, and one
        // code can be registered to several products, so both are collected rather than
        // reduced.
        //
        // IT IS EVERY PATCH ROW AND NOT ONLY THE REMOVABLE ONES, which is wider than
        // this pass began as, and the extra rows are the reason. The verdict has two
        // consumers: the offer, which reads it for removable rows, and the missing-file
        // split, which reads it for rows whose file has gone. Those two sets are
        // disjoint, a missing file never being offered, so judging removable rows alone
        // would leave the second consumer with nothing to read.
        //
        // AND IT CANNOT BE NARROWED TO "REMOVABLE PLUS MISSING", which is the obvious
        // saving: this runs in the enumeration, and the enumeration does not know
        // whether a file exists. Existence is established later, against the injected
        // filesystem, in FileSystemScanService. So the narrowest set available here is
        // the one both consumers can draw from, which is the patch rows.
        //
        // A state of 2 or 4 is the test rather than "has a patch claim", because that IS
        // the narrowing: an applied patch and a product's own package can be neither
        // offered from the registered set nor called a benign absence, so nothing ever
        // reads their verdict.
        var codesByPath = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in patchClaims)
        {
            if (!claimed.TryGetValue(claim.LocalPackagePath, out var row)) continue;
            if (row.PatchState is not (2 or 4)) continue;
            if (!codesByPath.TryGetValue(claim.LocalPackagePath, out var codes))
                codesByPath[claim.LocalPackagePath] = codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            codes.Add(claim.PatchCode);
        }

        if (codesByPath.Count == 0) return;

        // The products each path's codes are registered to, from the claims and from
        // route A.
        //
        // THIS NOTE SAID ROUTE A BEING NULL WAS "ALREADY ANSWERED BY THE CALLER, WHICH
        // WITHHOLDS EVERY PATH ON IT", AND THAT WAS TRUE OF ONLY HALF THE ROWS. The
        // caller's downgrade takes a removable verdict away and skips a row that has
        // none, so it never reaches a row that was never removable. An obsoleted
        // registration is exactly that. Those rows read their verdict from this pass and
        // from nowhere else, so a null route A has to be answered here as well; see where
        // the verdict is seeded below.
        var productsByPath = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in patchClaims)
        {
            if (!codesByPath.ContainsKey(claim.LocalPackagePath)) continue;
            if (!productsByPath.TryGetValue(claim.LocalPackagePath, out var set))
                productsByPath[claim.LocalPackagePath] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            set.Add(claim.ProductCode);
        }

        if (holders is not null)
            foreach (var (path, codes) in codesByPath)
                foreach (var code in codes)
                    if (holders.TryGetValue(code, out var named))
                        foreach (var (productCode, _, _) in named)
                            productsByPath[path].Add(productCode);

        foreach (var (path, products) in productsByPath)
        {
            ct.ThrowIfCancellationRequested();

            if (!claimed.TryGetValue(path, out var row)) continue;

            // ROUTE B, UNIONED IN HERE TOO. The two sources above are the claims and
            // route A, and both of them can only name a product some enumeration
            // returned. The patch file is read from disk and does not care what any
            // enumeration said, so it is the one source that can name the product
            // whose removable patch would overturn this verdict and that nothing else
            // on the machine mentions. It was already unioned into the per-pairing
            // pass below for exactly that reason; this condition asked a narrower set
            // than the pass it runs ahead of, and the gap between the two is a file
            // offered because the product that could roll back onto it was never
            // asked.
            //
            // ONLY WHERE A ROW IS STILL REMOVABLE, which is where widening the set can
            // change what the app does. The verdict is also read by the missing-file
            // split, and a file that has gone yields no declaration to read, so
            // widening there would cost a read per obsoleted row and could not alter
            // an answer.
            //
            // ADDING A PRODUCT CAN ONLY EVER WITHHOLD MORE. Worse() takes the worst of
            // the set, so a product joining it can move the verdict away from
            // AllNonRemovable and can never move it back.
            if (row.IsRemovable)
                foreach (var (productCode, _, _) in declaredTargets(path).Installed)
                    products.Add(productCode);

            // THE VERDICT ACROSS EVERY PRODUCT, WORSENED, and never from one of them.
            // The row carries whichever product code survived the claim merge, which is
            // whichever was reached first, so reading the verdict off the row's own code
            // would answer about one product of several and let enumeration order decide
            // what the app says about a file.
            //
            // AND IT STARTS UNESTABLISHED WHERE ROUTE A DID NOT ANSWER, because the set it
            // is about to be worsened across is then short by an unknown amount. Route A
            // is the only source that can name a product no enumeration returned, so
            // losing it does not narrow the product set by a knowable margin: it removes
            // the app's only way of finding out. Reading a clean verdict off what remains
            // is the scan trusting, for the purpose of a claim, exactly the completeness
            // it has just been told it does not have.
            //
            // THE OFFER DOES NOT MOVE BY ONE ROW, and that is why this is safe to do here
            // rather than only at the consumer. The caller already downgrades every
            // removable path when route A returns null, so a removable row is withheld
            // either way and arrives at the split carrying the same two flags; all this
            // changes is which pass got there first. What it DOES change is the row that
            // was never removable, chiefly an obsoleted registration, which the caller's
            // downgrade cannot touch because Downgrade takes a verdict away and there is
            // none to take. Such a row used to keep a positively clean verdict off a
            // product set route A had refused to complete, and the missing-files split
            // then read that as the app having established the absence was harmless.
            //
            // IT IS THE SAME MISTAKE THE SPLIT'S OWN NOTE WARNS ABOUT, arriving where that
            // note was not looking: trusting for the purpose of staying quiet what the
            // scan refused to trust for the purpose of acting.
            var verdict = holders is null
                ? ProductPatchSet.Unestablished
                : ProductPatchSet.AllNonRemovable;
            foreach (var productCode in products)
                verdict = Worse(verdict, ProductVerdict(productCode, registryPatchSets, apiPatchSets));

            claimed[path] = row with { ProductPatchSetVerdict = verdict };

            if (verdict == ProductPatchSet.AllNonRemovable) continue;

            // The downgrade applies to a removable row only, and the guard is not
            // redundant: this pass now judges rows that never carried a verdict, and
            // Downgrade's own contract is that it takes one away. The two causes reach
            // the two words it already has, and they are not the same finding: one is
            // the app having established that something on this product can be
            // uninstalled, the other is the app unable to establish that nothing can.
            // Both keep the file.
            if (!row.IsRemovable) continue;
            Downgrade(claimed, path, withheld: verdict == ProductPatchSet.Unestablished);
        }
    }

    /// <summary>
    /// Reads one product's registered patch set out of
    /// <c>Products\&lt;packed product&gt;\Patches</c> and reduces it to a single
    /// verdict.
    ///
    /// IT IS A LISTING AND NOT AN ENUMERATION, which is the reason this source is
    /// worth having at all. There is no index and no <c>NoMoreItems</c>, so there is
    /// no early-end case to be blind to: the fault every other source of a patch set
    /// shares is that a truncated enumeration is indistinguishable from a complete
    /// one, and a key listing cannot be truncated that way.
    ///
    /// NEVER <c>AllPatches</c>, AND THIS IS THE DECISION MOST LIKELY TO BE UNDONE BY
    /// SOMEBODY TIDYING UP. The same key carries an <c>AllPatches</c>
    /// <c>REG_MULTI_SZ</c> that looks like a ready-made list of exactly this. It is
    /// the EFFECTIVE list and not the registration list: measured against one machine
    /// while that machine still held superseded patches, it agreed with the subkeys
    /// on 137 of 138 products and disagreed about exactly one, the only product
    /// holding superseded patches, by listing the applied patch alone and omitting all
    /// three superseded ones. So anything built on it silently excludes the exact
    /// class this condition exists for.
    ///
    /// THAT MEASUREMENT CANNOT BE RE-TAKEN AND A FRESH CHECK WILL LOOK LIKE IT
    /// REFUTES IT. Re-run over the same machine on 2026-08-17, after its superseded
    /// patches had gone: 148 product keys, 147 carrying a <c>Patches</c> key, 147
    /// carrying <c>AllPatches</c>, and ZERO disagreements. `measured` Of course there
    /// were none, because the disagreement is ABOUT superseded patches and the machine
    /// no longer had any. **A reader who checks this on a machine with no superseded
    /// patch and finds perfect agreement has not disproved anything**, and the whole
    /// value of this paragraph is stopping them concluding otherwise. The live guard is
    /// now a test rather than a machine: see
    /// <c>ProductPatchSetTests.AllPatches_is_not_read_even_when_it_contradicts_the_subkeys</c>,
    /// which plants the disagreement rather than waiting for one.
    ///
    /// <c>Uninstallable</c> IS ACCEPTED ONLY AS AN <c>int</c>, and the strictness is
    /// the safe direction rather than tidiness. A value stored as text, or as a
    /// 64-bit number, is a shape nothing here anticipated, and reading it more
    /// permissively would turn an unanticipated store into a product read as clean,
    /// which is the one direction that puts a file on the list. <c>State</c> is not
    /// read: the condition asks about every registered patch whatever state it
    /// carries, so filtering by state could only ever narrow the set and offer more.
    /// </summary>
    internal static ProductPatchSet ReadProductPatchSet(
        Microsoft.Win32.RegistryKey productsKey,
        string packedProductCode,
        ref int patchKeys,
        ref int patchRegistrations)
    {
        using var patchesKey = productsKey.OpenSubKey($@"{packedProductCode}\Patches");

        // AN ABSENT KEY IS AN ANSWER AND NOT AN INABILITY, and the difference decides
        // whether a file is offered. A product with no Patches key holds no registered
        // patch, so it holds no removable one, so nothing on it can be uninstalled and
        // reach for the file this verdict is being asked about. That is the same
        // sentence AllNonRemovable already carries, arrived at without reading a
        // registration because there are none to read.
        //
        // THE FUNCTION ALREADY SAYS SO ONE BRANCH AWAY. A Patches key that opens and
        // holds no subkeys runs the loop zero times, leaves unestablished false and
        // returns AllNonRemovable at the closing line. An empty patch list and an
        // absent one say the identical thing about the machine, and reporting them
        // differently made the emptier of the two the more suspicious.
        //
        // THE TWO WAYS OF GETTING NOTHING ARE TOLD APART, AND AT THE CALLER RATHER
        // THAN HERE. A key that exists and will not open throws, and the caller's own
        // catch writes Unestablished for that product with its own failure cause. A
        // key that is not there returns null and arrives on this line. So this branch
        // carries the absent case alone and does not have to hedge for the other.
        if (patchesKey is null) return ProductPatchSet.AllNonRemovable;

        // COUNTED WHERE THE KEY OPENED AND NOWHERE ELSE, unchanged by the line above.
        // The count answers how usual it is for a product to carry a Patches key at
        // all, read against ProductKeys, and a product that has no such key has not
        // got one whatever verdict is returned for it. Moving the increment up would
        // make the two counts agree on every machine and stop the pair saying
        // anything. This reading feeds the opt-in report, where a counter that
        // quietly changes meaning is worse than one that is missing.
        patchKeys++;

        var unestablished = false;
        foreach (var patchName in patchesKey.GetSubKeyNames())
        {
            patchRegistrations++;

            using var patchKey = patchesKey.OpenSubKey(patchName);
            if (patchKey is null) { unestablished = true; continue; }

            // A positive zero is the only clean answer. Absent, wrong-typed and
            // anything non-zero all fail the product, and only the last of the three
            // is a finding rather than an inability.
            if (patchKey.GetValue("Uninstallable") is not int uninstallable)
            {
                unestablished = true;
                continue;
            }

            if (uninstallable != 0) return ProductPatchSet.RemovablePatchPresent;
        }

        return unestablished ? ProductPatchSet.Unestablished : ProductPatchSet.AllNonRemovable;
    }

    /// <summary>
    /// Merges two readings of one product code, which happens when the same product
    /// is registered under more than one SID subtree.
    ///
    /// WORSENING ONLY, on the same reasoning as <see cref="MergeClaim"/>: a reading
    /// that finds a removable patch can never be cancelled by one that did not look
    /// there, and two SIDs disagreeing must not be settled by whichever the walk
    /// reached first. A positive finding outranks an inability, and an inability
    /// outranks a clean bill.
    /// </summary>
    internal static ProductPatchSet Worse(ProductPatchSet a, ProductPatchSet b)
    {
        if (a == ProductPatchSet.RemovablePatchPresent || b == ProductPatchSet.RemovablePatchPresent)
            return ProductPatchSet.RemovablePatchPresent;
        if (a == ProductPatchSet.Unestablished || b == ProductPatchSet.Unestablished)
            return ProductPatchSet.Unestablished;
        return ProductPatchSet.AllNonRemovable;
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
    ///
    /// AND TWO TYPES NEVER REACH THE CAST, which is why the presence test below is
    /// not belt and braces over it. Microsoft documents that
    /// <c>RegistryKey.GetValue</c> "does not support reading values of type
    /// REG_NONE or REG_LINK. In both cases, the default value (null) is returned
    /// instead of the actual value." So a value that is PRESENT in either of those
    /// types arrives here as null and, read naively, is indistinguishable from a
    /// registration that simply has no cached path. The claim is dropped, no
    /// failure is counted, and the fallback reports itself as having run cleanly
    /// and found nothing to say, which is the one state the degraded-sources gate
    /// exists to tell apart from a healthy machine. Asking the key which value
    /// names it holds is what separates the two, because the name list is typed
    /// nowhere and carries every value whatever its type.
    ///
    /// AND IT HAS ALWAYS EXPANDED A <c>REG_EXPAND_SZ</c> VALUE WITHOUT ANYBODY
    /// DECIDING TO, which is worth stating because it was the load-bearing half of a
    /// defect nobody had noticed. .NET expands that type as part of the read, so a
    /// registration spelled <c>%SystemRoot%\Installer\...</c> and STORED expandable
    /// came back as a usable path here, while the same text stored as a plain
    /// <c>REG_SZ</c> was expanded nowhere in the application and became a claim on a
    /// location that did not exist. Behaviour turned on the value's registry type
    /// rather than on anything in the code.
    /// <c>NormaliseLocalPackagePath</c> closes that from 3.0.0 by expanding on the
    /// main path, so this read's expansion is now the belt to that brace rather than
    /// the only thing standing between one storage type and a wrong answer.
    /// </summary>
    internal static bool TryReadLocalPackage(Microsoft.Win32.RegistryKey? key, out string? path)
    {
        path = null;

        // Absent by structure: not a failed read.
        if (key is null) return true;

        // RegistryValueOptions.None is the option that selects expansion, and it is
        // passed explicitly rather than left to the default because the expansion is
        // now part of a documented pair with NormaliseLocalPackagePath and a reader
        // has to be able to see it. GetValue(name) delegates to this same overload
        // with this same option, so the call does exactly what it did and only says
        // so; anyone "simplifying" it back has removed the statement and not the
        // behaviour.
        var raw = key.GetValue("LocalPackage", null, Microsoft.Win32.RegistryValueOptions.None);
        if (raw is string value)
        {
            path = value;
            return true;
        }

        // Present and not a string: a read that failed, and the ordinary shape of
        // one (REG_DWORD, REG_BINARY, REG_MULTI_SZ).
        if (raw is not null) return false;

        // Null, which is two different things. Absent by value is an ordinary
        // state; present-but-unreadable is a failure the cast above could never
        // have seen. The name comparison is case-insensitive because registry
        // value names are, so a key holding "localpackage" must not read as a key
        // holding nothing.
        foreach (var name in key.GetValueNames())
        {
            if (string.Equals(name, "LocalPackage", StringComparison.OrdinalIgnoreCase))
                return false;
        }

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
    /// HALF the rule that decides whether a patch's cached .msp is offered, from its
    /// State and Uninstallable values exactly as <c>MsiGetPatchInfoEx</c> returned
    /// them. The other half is
    /// <see cref="JudgeAndWithholdAgainstEveryProductPatchSet"/> and a row this returns true for
    /// is still withheld unless every product sharing the patch passes that.
    /// **Nothing may read this alone as permission to remove a file.**
    ///
    /// SUPERSEDED ONLY FROM 3.0.0, WHERE IT WAS <c>2 or 4</c>. Obsoleted patches come
    /// off the offer for a reason that is not about safety: measured across every
    /// opt-in report this project had received as at 2026-08-17, obsoleted patches had
    /// never been seen on any machine at all, so offering them reclaims nothing, and
    /// nobody has ever manufactured one to test with. The date is part of the claim,
    /// the corpus being one that grows. A class that buys no space and has never been
    /// exercised does not belong on a list whose whole claim is certainty. They are
    /// counted at scan time instead, off the machine rather than off the offer, so the
    /// question of whether anybody has any gets answered without anything appearing on
    /// anyone's list.
    ///
    /// WHAT EACH HALF IS WORTH, because the two are not the same kind of fact. The
    /// State half carries real information: Windows has computed that a later patch
    /// took over this one's fixes. It does not say the cached file is spare, and
    /// Microsoft's own words for the state are "applied to this product instance but
    /// is superseded". The Uninstallable half reports whether Windows can UNDO this
    /// patch, which its own reference page gives eight causes for, the commonest being
    /// that the patch author never set the AllowRemoval row. So a positively read "0"
    /// says this patch cannot be rolled back, and nothing about whether anything still
    /// reads the file.
    ///
    /// AND ON ITS OWN THE CONJUNCT ASKS THE WRONG PATCH. Measured against real
    /// patches it behaves as a vendor filter pointing the wrong way: all 58 patches in
    /// Office 2010 SP2 declare themselves removable and were refused, and three live
    /// Adobe patches declared themselves not removable and were offered. The Office
    /// figure comes from captured data about one product's patch set; the Adobe one is
    /// one machine's state, read on or before 2026-08-17, and that machine held two
    /// Adobe patch registrations when its hives were read on 2026-08-18. Neither
    /// re-reading changes what the pair shows, which is that the declaration tracks
    /// the vendor rather than the risk. The risk turns
    /// on whether the patch that SUPERSEDED this one can be uninstalled, which this
    /// never reads, and that is precisely what the other half was built for.
    ///
    /// Both directions fail safe. An unparseable State leaves the parsed value at 0
    /// (not a patch), and only a positively read "0" for Uninstallable clears the
    /// second test, so an absent or unreadable value refuses.
    /// </summary>
    internal static bool IsRemovablePatch(string stateValue, string uninstallableValue)
    {
        int.TryParse(stateValue, out var patchState);
        return patchState == 2 && uninstallableValue == "0";
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
