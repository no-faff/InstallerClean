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
    /// Product entries with a cached-package path, under either name in
    /// <see cref="CachedPackageValueNames"/>, that the API's own loop never claimed
    /// AND whose file is on the disk, counted once per entry however many of its
    /// values named one. One such entry is one installed
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
    /// Cached-package values, under either name in
    /// <see cref="CachedPackageValueNames"/>, that were PRESENT and were not a
    /// string, so nothing could be read out of them, one per value. A SUBSET of
    /// <see cref="Failures"/> rather than a term beside it, and the overlap is
    /// deliberate: the degraded-sources gate weighs reads that failed, this one
    /// failed, and narrowing that gate is not an instrumentation change's
    /// business. What it is for is the one thing the merged counter cannot say,
    /// namely whether anything on real machines writes that value under a type
    /// other than <c>REG_SZ</c>. Every other contributor to
    /// <see cref="Failures"/> is a thrown exception, so the two are separable by
    /// subtraction and neither has to state a cause for the other's members.
    ///
    /// Nothing writing these keys is obliged to use <c>REG_SZ</c>.
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
    /// IT DECIDES WHETHER A CACHED PATCH IS OFFERED. This dictionary is
    /// passed to <see cref="ConfirmRemovableAgainstEveryProduct"/>, reaches
    /// <c>JudgeAndWithholdAgainstEveryProductPatchSet</c>, and is read per product by
    /// <see cref="ProductVerdict"/>, which is what stamps the verdict the offer and the
    /// missing-file split both consult. The two counts beside it travel in the opt-in
    /// report through <c>EnumerationCensus</c> and <c>ResultLogEntry</c>.
    ///
    /// DO NOT READ IT AS MACHINERY WAITING TO BE WIRED UP. The registry is the half of
    /// this verdict that has no index and no early end to be blind to, which is the
    /// whole reason it is read at all, and discounting it discards the one source that
    /// cannot be silently truncated.
    /// </param>
    /// <param name="ProductPatchKeys">
    /// Products whose <c>Patches</c> key opened. Against
    /// <paramref name="ProductKeys"/> it answers how usual it is for a product to
    /// carry one at all, a product with no patches having no reason to.
    /// </param>
    /// <param name="ProductPatchRegistrations">
    /// Patch subkeys REGISTERED under those keys, one per (product, patch)
    /// registration rather than per patch, and taken off the key listing rather than
    /// off what the read went on to examine. With
    /// <paramref name="ProductPatchKeys"/> it gives how many patches a machine's
    /// products carry.
    ///
    /// THE COUNT IS OF REGISTRATIONS LISTED, NOT OF REGISTRATIONS EXAMINED. The
    /// per-product read returns at the first patch declaring itself removable, so the
    /// two differ on a product with a removable patch and more than one registration.
    /// A REPORT FROM AN EARLIER SCHEMA CARRIES THE OTHER QUANTITY UNDER THIS NAME: the
    /// two are not comparable and must not be summed, and the envelope's app version
    /// and schema version each separate them.
    /// </param>
    /// <param name="ProductsWithRemovablePatch">
    /// Products where at least one registered patch positively declared itself
    /// removable. THIS IS THE COUNT THAT SAYS ON HOW MANY PRODUCTS THE PER-PRODUCT
    /// CONDITION IS ARMED.
    /// </param>
    /// <param name="ProductsWithPatchSetUnestablished">
    /// Products whose patch set could not be established at all. The other half of
    /// the same question, and the one that separates "the condition found a reason"
    /// from "the condition could not look".
    /// </param>
    /// <param name="Reach">
    /// What this read established about which cached files a product's own patches
    /// could reach for, which is what lets a product recovered by name be judged
    /// against the files it can actually touch instead of against all of them. See
    /// <see cref="EstablishedPatchReach"/>; its default establishes nothing and is
    /// read as "judge this product against every path".
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
        PathCensus? Paths = null,
        EstablishedPatchReach Reach = default);

    /// <summary>
    /// The two registry listings that together say which cached files one product's
    /// patches could reach for: the patch codes a product holds, and the cached path
    /// each of those patch codes records for itself.
    ///
    /// WHAT IT IS FOR. A product the machine-wide enumeration never returned is
    /// recovered by name and has to be judged against the cached patch files it could
    /// roll back onto. Judging it against every one of them is always correct and
    /// keeps back files it demonstrably cannot touch. These two listings are what
    /// make the narrower answer available, and <see cref="MustJudge"/> is the only
    /// place they are read.
    ///
    /// NOT KNOWING IS THE DEFAULT AND IT IS STRUCTURAL. Both members are null on a
    /// default value, and null at any step of <see cref="MustJudge"/> means the
    /// product is judged against the path. So a caller that never fills this in gets
    /// the wide answer rather than a silently narrow one, which is the same discipline
    /// that puts <c>Unestablished</c> at the zero of <see cref="ProductPatchSet"/>.
    ///
    /// AN EMPTY LISTING AND AN ABSENT ONE ARE NOT THE SAME THING AND MUST NEVER BE
    /// MADE ONE. An empty collection says the registry was read and this product holds
    /// no patch, or this patch records no cached file. A null says nobody could
    /// establish either. The first can exclude a path; the second never can.
    /// </summary>
    /// <param name="PatchCodesByProduct">
    /// Per product code, the patch codes its own <c>Patches</c> key lists, or null
    /// where that listing was not established. See <see cref="ReadProductPatchSet"/>,
    /// which takes it on the same call that reduces it to a verdict.
    /// </param>
    /// <param name="CachedPathsByPatchCode">
    /// Per patch code, the cached paths its own registrations record in
    /// <c>LocalPackage</c> or <c>ManagedLocalPackage</c>, or null where any
    /// registration of it yielded none. A patch registered under two SID subtrees, or
    /// recording both values, can record two, so it is a set rather than a path, and a
    /// registration with either value unreadable, or there and empty, leaves the whole
    /// entry null.
    /// </param>
    internal readonly record struct EstablishedPatchReach(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>?>? PatchCodesByProduct = null,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>?>? CachedPathsByPatchCode = null)
    {
        /// <summary>
        /// Whether a product recovered by name has to be judged against a given cached
        /// patch file.
        ///
        /// THE WIDE ANSWER IS THE FIRST STATEMENT RATHER THAN THE LAST. No listing at
        /// all, no entry for this product, or an entry that is null all mean the same
        /// thing: nothing establishes which patches this product holds, so it may hold
        /// any of them, so it is judged against every path exactly as if none of this
        /// existed. Only a listing that positively finished can exclude a path, and it
        /// excludes one only by naming neither the path's own patch codes nor the path.
        ///
        /// WHY A LISTING MAY BE TRUSTED WHERE AN ENUMERATION MAY NOT, which is half of
        /// the safety argument: <c>GetSubKeyNames</c> either returns every name under
        /// the key or throws. There is no index and no early end, so a listing cannot
        /// come back short while looking complete, which is the fault every other
        /// source of a patch set has. See <see cref="ReadProductPatchSet"/>, where that
        /// same property is the reason this source was chosen at all.
        ///
        /// AND THE OTHER HALF IS WHY THE PATH IS ASKED ABOUT TWICE. The codes naming a
        /// path come from the claims, and a claim carries the path one registration
        /// recorded for one patch code. A corrupt <c>LocalPackage</c> can aim a patch row
        /// at a file that is not that patch's, so a product could hold the patch whose
        /// file this really is while the claims name the path under another code, and
        /// the codes alone would then exclude a product that can reach the file. The
        /// second question covers that: every patch code this product holds is asked
        /// where its own cached file is, and a code that records this path, or records
        /// nothing, judges. So the narrowing rests on the product's OWN registrations
        /// rather than on another product's claim being right about which file it
        /// named.
        ///
        /// WHAT IT READS is a product's own registry records of which patches it holds
        /// and where their cached files are, which are the same records the per-product
        /// verdict reads one step later.
        /// </summary>
        internal bool MustJudge(
            string productCode, string path, HashSet<string> patchCodesNamingThePath)
        {
            if (PatchCodesByProduct is null
                || !PatchCodesByProduct.TryGetValue(productCode, out var held)
                || held is null)
                return true;

            foreach (var code in held)
            {
                // The claims say this code is registered against this path, so a
                // rollback of anything on this product can reach for it.
                if (patchCodesNamingThePath.Contains(code)) return true;

                // And where this code's own registration says its cached file is.
                // Absent, unreadable or naming this path: judge. Only a positively
                // read set of paths, none of which is this one, lets the path go.
                if (CachedPathsByPatchCode is null
                    || !CachedPathsByPatchCode.TryGetValue(code, out var cached)
                    || cached is null)
                    return true;

                foreach (var cachedPath in cached)
                    if (string.Equals(cachedPath, path, StringComparison.OrdinalIgnoreCase))
                        return true;
            }

            return false;
        }
    }

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
    /// READ. The resolver is asked about every recorded path the steps before it
    /// could turn into a path, so a scan that asked about none reports five zeros
    /// because nothing was asked, which is indistinguishable from five zeros because
    /// nothing failed, and a receiver would take the second reading.
    /// <see cref="ResolverAttempts"/> is what separates them.
    ///
    /// BOTH GROUPS DECIDE THE OFFER. The resolver's five outcomes withhold exactly as
    /// the four normalisation refusals do, on one rule in one place rather than a
    /// second quiet copy of one:
    /// FileSystemScanService withholds the whole walk-derived offer where
    /// <c>EnumerationCensus.AnyRecordedPathUnestablished</c> answers true, and that
    /// property is where every population is added to the question.
    ///
    /// THE ATTEMPTS COUNT IS MEASUREMENT AND NOT A RULE. Nothing withholds on it. It
    /// is what makes the five readable, since a scan that asked about no path reports
    /// five zeros that are indistinguishable on the wire from five clean answers.
    /// </summary>
    internal sealed class PathCensus
    {
        /// <summary>
        /// Recorded paths put to the final-path resolver, which is every value that
        /// got past the embedded-null test, the expansion and the prefix strip.
        /// </summary>
        internal int ResolverAttempts;

        /// <summary>
        /// Of those, the ones carrying a spelling only the filesystem can settle: an
        /// 8dot3 alias, or a prefix the strip left on for want of a drive root.
        ///
        /// IT DECIDES NOTHING AND IS THE ONLY MEMBER HERE THAT NEVER DID. The other
        /// counters record what happened to a value; this records what the value
        /// LOOKED LIKE. It exists because the resolver is put every path, so
        /// <see cref="ResolverAttempts"/> cannot also say how many of them carried
        /// such a spelling: one counter answering both questions answers neither.
        /// How often these spellings occur on real machines is what this one is for.
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

        var (products, unreadableRows) = EnumerateProducts(ct);

        // Installed products this scan could not read in full. A skipped patch
        // row, or a LocalPackage value that could not be read, is one product
        // whose claims are short by at least one. Both leave the same hole, a
        // claim that never reached the merge, so both count the product once.
        // The loop below adds them. It starts from the product rows the walk
        // passed without reading, which is zero on every walk that returns: the
        // walk refuses the scan on such a row. Seeding from the walk's own count
        // keeps a row it ever passed inside this figure.
        var unreadableProducts = unreadableRows;

        // Patches whose State or Uninstallable read failed. Decides nothing; see
        // the increment site for what it measures and why it is worth measuring.
        var unreadablePatchStates = 0;

        // Products installed as a second instance of themselves, and the products
        // that would not answer the question. NEITHER MAY BE READ WITHOUT THE OTHER,
        // and there is a rule that obeys that rather than a note saying it:
        // EnumerationCensus.SecondInstanceNotRuledOut asks them together and the walk's
        // offer is withheld wholesale on the answer. See that property for what the
        // pair means and InstanceProductCount for what a positive reading rests on.
        //
        // FED FROM TWO PLACES AND NOT ONE. The loop below asks every product the
        // enumeration returned; the pass after it asks every product the enumeration
        // lost that the registry named and Windows confirmed installed. A product the
        // registry names that nothing shows was asked is counted as unanswered or as
        // an unparseable key name, and the rule reads those two counts as well. A product
        // in neither the enumeration nor the registry's product keys is one nothing on
        // this machine can name, which is the limit of the whole scan and not of this
        // rule.
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

        // A SECOND BUDGET, BECAUSE THE CLOSING ENTRY'S LAST SENTENCE IS PER CAUSE AND
        // THESE TWO HAVE DIFFERENT ANSWERS. The one above says the product identity is
        // written down nowhere else; this one is about a file rather than a product and
        // the answer for it is different again. Sharing a budget would put one of the
        // two sentences over entries it is false of.
        //
        // WHAT IT RECORDS IS A DISTINCTION WINDOWS DRAWS AND THE APP OTHERWISE DROPS.
        // Asked to open a patch file's summary stream, Windows answers one code for a
        // path it could not open at all and another for a file it opened and found not
        // to be a package. The second can only mean the file is THERE and will not be
        // read, which is the app unable to establish something it could have
        // established; the first is also what an absent file gives, which is ordinary.
        // Both take the verdict away and keep the file, so nothing acts on the
        // difference. This is where it is written down.
        //
        // THE CAUSE KEY IS THE READER'S OWN DETAIL, WHICH IS WHAT MAKES THE BUDGET
        // WORK FOR THIS. The detail carries the code, so the two answers are two
        // causes: a machine with thousands of absent patch files still logs the first
        // file that was present and unreadable, however late it arrives, instead of
        // losing it behind a storm of the ordinary one.
        var unreadPatchFileLog = new PerItemFailureLog("Patch file read",
            "How many patch files would not read, and which of the two ways, is recorded "
            + "nowhere else: the result log carries no count for it and no surface says "
            + "anything about it. What the user sees is that some superseded files were "
            + "kept back, which names no file and no cause.",
            _crashLogSink);

        // The closing entry is owed on every exit: the two gates below both
        // throw, and the both-sources-degraded one in particular fires on
        // exactly the broken registration that makes this storm.
        try
        {
        // Which product of how many this loop is on. The enumeration above
        // materialises its list before a single product is asked about anything,
        // so the total is settled here and a host can fill a bar in proportion
        // rather than approximate one. It counts products the enumeration
        // returned, which is the loop's own length and not a claim about how many
        // are installed: the products it could not read are recovered by name
        // further down and are counted where that happens.
        var productIndex = 0;

        foreach (var (productCode, userSid, context) in products)
        {
            ct.ThrowIfCancellationRequested();
            productIndex++;

            // Every way this one product's records can come back short reaches
            // the same count, and reaches it once. The number the user reads is
            // programs, not failures, so one program with a failed package read
            // AND two failed patch rows is one program. Counting failures
            // instead would inflate the notice without telling anyone more.
            var recordsShort = false;

            var productName = GetProductProperty(productCode, userSid, context, MsiInstallProperty.ProductName).Value;
            var localPackage = GetProductProperty(productCode, userSid, context, MsiInstallProperty.LocalPackage);

            // Ticker, not milestone: one of these fires per product, up to
            // hundreds in a few seconds, so the consumer must not feed it to a
            // screen-reader live region.
            //
            // Reported for every product the loop reaches, including the ones
            // whose records come back short below, because the position says how
            // far through the list this loop is and every product in the list
            // takes the same turn. Reporting only the products that claim a file
            // would leave the position short of the total by however many did
            // not, and a host filling a bar from it would stop before the end.
            progress?.Report(new ScanProgressUpdate(
                productName.Length > 0 ? productName : productCode,
                IsMilestone: false, Position: productIndex, Total: products.Count));

            // One more keyed property read on a product this loop has already
            // reached, rather than a second enumeration: the walk behind this loop
            // already passes the everyone SID across all three contexts, which is
            // the shape the question needs, so asking here costs one call per
            // product and nothing per machine.
            //
            // IT DOES NOT FEED recordsShort AND MUST NOT START. The class the other
            // reads in this loop withhold for is about a CLAIM that never reached the
            // merge, and this property carries no claim on any file, so counting it
            // there would withhold the superseded class on a fact about the machine
            // rather than on a lost claim. What it DOES feed is a separate rule, and
            // where the two counts are read together is EnumerationCensus.
            switch (ReadInstanceType(productCode, userSid, context))
            {
                case InstanceReading.SecondInstance: instanceProducts++; break;
                case InstanceReading.Unreadable: instanceTypeUnreadable++; break;
            }

            // LocalPackage is the one property whose failed read DELETES this
            // product's claim rather than degrading it. An unreadable State
            // leaves patchState 0 and an unreadable Uninstallable leans
            // non-removable, so either still merges a row that says "needed";
            // an unreadable LocalPackage skips the insertion entirely, and the
            // product's "I still have this file" never reaches the merge at all.
            // So it is counted in unreadableProducts and withholds the removable
            // class. Without the count the scan would report itself complete
            // while short of a claim.
            if (localPackage.Unreadable)
            {
                recordsShort = true;
            }
            else if (localPackage.Value.Length > 0)
            {
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
                // WHICH IS WHY THE PER-PRODUCT CONDITION UNIONS THREE SOURCES
                // RATHER THAN TRUSTING THIS LOOP. Present and
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
                    // count travels beside the flag because how often either read
                    // fails is a fact only the reports can establish. It also refuses
                    // the removable verdict below, both halves of that rule needing a
                    // positive answer.
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

        // THE SAME QUESTION, PUT TO THE PRODUCTS THE ENUMERATION LOST. A product it
        // lost is recovered by name above, through ResolveProductInstances, which asks
        // whether the code is installed and walks no list, so it establishes an account
        // and a context and reads no property at all. This loop puts the InstanceType
        // question to each recovered product in that account and context.
        //
        // ASKED RATHER THAN ASSUMED UNANSWERABLE. A recovered product counted as
        // unreadable instead would empty the offer on exactly the machines the recovery
        // pass exists to rescue. Recovery closes a gap by asking, and this is one more
        // question to the products it recovered.
        //
        // IT COSTS ONE KEYED PROPERTY READ PER RECOVERED PRODUCT, on a set that is
        // empty on a machine whose enumeration came back whole, and it fails in the
        // safe direction by construction: the read that will not answer reaches the
        // unreadable count, which withholds, and a positive reaches the count that
        // withholds for the other reason.
        //
        // The account and context are the ones the recovery established, because a
        // per-user product answers in its own account and nowhere else.
        foreach (var (recoveredCode, recoveredSid, recoveredContext) in missed.Recovered)
        {
            ct.ThrowIfCancellationRequested();
            switch (ReadInstanceType(recoveredCode, recoveredSid, recoveredContext))
            {
                case InstanceReading.SecondInstance: instanceProducts++; break;
                case InstanceReading.Unreadable: instanceTypeUnreadable++; break;
            }
        }

        ConfirmRemovableAgainstEveryProduct(claimed, patchClaims, products, missed.Recovered,
            fallback.Reach, fallback.ProductPatchSets, apiPatchSets, ct, unreadPatchFileLog);

        // Both sources degraded at once: the scan is refused outright rather than
        // reported short.
        //
        // THIS GATE PROTECTS THE WALK HALF. What unreadableProducts answers here is
        // whether a product's claim on a cached file exists anywhere at all, and a
        // file no source claims goes to the folder walk's candidates.
        //
        // A claim the API loop lost is answered by the fallback alone, because the
        // path it names is still reachable: the fallback reads the same UserData
        // keys and contributes them as rows, so the file stays claimed even though
        // the API's read of it failed.
        //
        // With the fallback ALSO failing reads, that is not established: a product
        // whose claim the API lost and whose UserData key was one of the unreadable
        // ones is claimed by neither source, so the scan stops here. The two
        // failures are not independent, either: the same corrupt registration that
        // fails an API read can equally make that product's UserData subtree
        // unreadable, so the backup is likeliest to be missing exactly the claim
        // the primary lost.
        // Neither counter can bound what the other lost, so no narrower rule is
        // sound.
        //
        // On a machine whose records read cleanly both counters are zero and this
        // does not fire.
        //
        // Keyed on what the API said about itself, never on the cross-check
        // below: this gate REFUSES, and a refusal must rest on a product the
        // enumeration itself reported it could not read. The cross-check infers
        // a loss from two counts that can differ for innocent reasons, which is
        // sound enough to withhold on and not to refuse on.
        if (unreadableProducts > 0 && fallback.Failures > 0)
            throw new LocalisedInvalidOperationException(Strings.Error_ScanRecordsUnreadable);

        // An enumeration that ends EARLY says nothing about itself: a
        // NoMoreItems at index 3 of 200 sets reachedEnd and leaves unreadableRows
        // at 0. The downgrade-only merge keeps a patch that is Superseded under one
        // product and Applied under another off the offer only for a product the
        // loop reached, so the products a short enumeration did not reach are found
        // by name wherever the registry names them with a code, and a key whose name
        // yields no code is counted and withholds.
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
        // WHY A LEFTOVER KEY PROVES NOTHING. A UserData product key outlives a
        // failed or partial uninstall, so the registry legitimately holds more keys
        // than the machine has products, and against a TOTAL that residue cannot be
        // told from a truncation: both read as the registry running ahead. Asked by
        // name, the same key answers "not installed", which settles it outright and
        // costs nothing, because a product that is not there holds no patches.
        //
        // AND WHERE THE REGISTRY READ ITSELF FAILS. ProductKeys is counted from the
        // subkeys the fallback actually walked, so a fallback that failed reports
        // FEWER keys, and the names asked about are the codes that side handed over.
        // The failed read is counted in fallback.Failures, which with an unreadable
        // product refuses the scan at the gate above.
        //
        // AND THE GATE ABOVE WEIGHS TWO TERMS, REFUSING WHEN BOTH ARE NON-ZERO,
        // which is worth spelling out beside this because they count different
        // things. fallback.Failures is the registry side's own tally of key reads
        // that failed. unreadableProducts is what the API said about ITSELF:
        // products it returned whose records came back short inside the loop. An
        // enumeration ending on NoMoreItems has said nothing about itself and
        // raises neither term, which is why the products behind a disagreement are
        // named above rather than counted here.
        //
        // What remains here is an OBSERVATION and not an estimate. The fallback reads
        // the same UserData keys the API read and runs after the whole API loop, so a
        // path it is the FIRST to claim is one no product the loop reached ever named.
        // Its file being on the disk is the other half: a residue key whose product is
        // gone but whose cached-package value survives leaves an unclaimed path too,
        // and that population's file is usually not there. It overlaps the comparison
        // on a machine where both fire, and sees a lost product through a file on the
        // disk rather than through a code, so it does not depend on any key name being
        // a packed GUID this code can read.
        //
        // A product whose LocalPackage read failed has its registry value claimed
        // by the fallback alone, so it is already inside unreadableProducts.
        // Subtracting the whole of that count is deliberately generous (a product
        // short only a patch row contributes no unclaimed path), which can leave
        // the NUMBER low and cannot leave the withholding off: whatever it absorbs,
        // unreadableProducts carries.
        //
        // A patch entry names no product, so it can say only that at least one
        // went unreached. It floors the count rather than adding to it.
        var unclaimedProducts = Math.Max(0, fallback.UnclaimedProductFiles - unreadableProducts);
        var apiNeverClaimed = fallback.UnclaimedPatchFiles > 0
            ? Math.Max(1, unclaimedProducts)
            : unclaimedProducts;

        // Registry products this scan could not settle either way: a code Windows
        // would not answer about, and a key whose name yielded no code to ask
        // with. Two steps of one state, so one figure. Nothing shows either was asked
        // its InstanceType, so both also reach EnumerationCensus.SecondInstanceNotRuledOut,
        // which reads them apart through the census below.
        var unresolvedProducts = missed.Unresolved + fallback.UnparseableProductKeyNames;

        // ADDED rather than weighed against the observation, because the two are
        // not estimates of one quantity: the observation counts products seen to
        // have gone unclaimed, and this counts the ones the question got no answer
        // for. A product RECOVERED by name contributes to neither, which is the
        // whole gain: the gap it would have been part of was closed by asking
        // rather than covered by withholding.
        var withheldProducts = unreadableProducts + apiNeverClaimed + unresolvedProducts;

        progress?.Report(new ScanProgressUpdate(Strings.Status_RegisteredPackagesFound));

        var packages = claimed.Values.ToList();

        // LIVE, AND ON NO ACCOUNT TO BE DELETED AS DEAD MACHINERY. A superseded row
        // on a machine whose patch sets read clean arrives here still carrying
        // IsRemovable, and this loop is what takes it off the offer when the scan
        // lost a claim.
        //
        // One product whose LocalPackage read fails is enough to fire it: that
        // product is counted in withheldProducts, and the loop takes every superseded
        // row off the offer.
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
        // Nothing finer is sound. A failed patch row names its product and not its
        // patch (the API documents its output buffers for ERROR_SUCCESS and
        // ERROR_MORE_DATA only, and the loop clears the buffer per iteration), so
        // the patch that product could still be holding is unknowable. A failed
        // LocalPackage read names its product but not the path it would have
        // claimed, which is the half that matters: the lost claim could be on any
        // cached file, so knowing who lost it narrows nothing. Scan-wide is the
        // finest granularity the information supports either way.
        //
        // This loop moves only the removable class, the superseded patches, and only
        // on a scan that lost a claim, found a cached file no product it reached
        // claimed, or could not settle a product the registry names. The walk half is
        // decided elsewhere, on conditions of its own.
        //
        // AND IT TOUCHES NOTHING ELSE, WHICH IS A DECISION RATHER THAN THE ABSENCE OF
        // ONE. A second arm here, clearing the unread-file marker on a row something
        // else has already withheld so that the missing-files split treats such a row
        // as unaccounted for, is not wanted and must not be added under any name.
        //
        // WHAT THE MARKER MEANS IS WHY. It records that the ONLY reason the row lost
        // its verdict was that the pass reading the patch file could not read it, and
        // the split reads it for one population: rows whose file has GONE. For those
        // the failed read is the read of the very file whose absence is the subject.
        // Nobody can perform it, on any machine, ever, and it fails identically
        // whatever removed the file. Clearing it would make that tautology a reason to
        // warn, and a run that came up short somewhere ELSE would print an alarm about
        // a file this scan had positively established nothing could reach for.
        //
        // AND THE COUNT THIS LOOP FIRES ON DOES NOT NAME THAT ROW'S RISK. Its terms are
        // a read that failed on a product this loop DID return, a product the registry
        // saw and the enumeration did not whose own file is present, and a product the
        // registry names that this scan could not settle. None of them is "a holder of
        // this patch went unseen", which is the condition that would bear on this
        // file. The count is a sign of a degraded machine, not a per-file verdict.
        //
        // THE WITHHOLDING ITSELF IS WHAT ANSWERS FOR SUCH A MACHINE: a run that could
        // not account for a product offers no superseded patch at all. A file already
        // gone is not kept by printing a sentence about it.
        //
        // THE SPLIT HAS A ROUTE TO THIS STATE. A run whose machine-wide patch
        // enumeration did not answer downgrades every removable path with no marker
        // set (see ConfirmRemovableAgainstEveryProduct), so a missing superseded row on
        // such a run reaches the split withheld and unmarked and is reported. That run
        // failed to establish something about the patch itself.
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
                paths.FlaggedSpellings,
                fallback.Failures),
            ListedInstallations(products, missed.Recovered));
        }
        finally
        {
            abandonedLog.WriteClosingEntry();
            unreadPatchFileLog.WriteClosingEntry();
        }
    }

    /// <summary>
    /// Every installation this enumeration established, for
    /// <see cref="InstallerQueryResult.Installations"/>: each row the product walk
    /// listed, in walk order, then each installation the recovery by name found.
    /// </summary>
    private static List<ListedInstallation> ListedInstallations(
        List<(string ProductCode, string? UserSid, MsiInstallContext Context)> products,
        List<(string ProductCode, string? Sid, MsiInstallContext Context)> recovered)
    {
        var installations = new List<ListedInstallation>(products.Count + recovered.Count);
        foreach (var (code, sid, context) in products)
            installations.Add(new ListedInstallation(code, sid, (int)context));
        foreach (var (code, sid, context) in recovered)
            installations.Add(new ListedInstallation(code, sid, (int)context));

        return installations;
    }

    /// <summary>
    /// Which installed products the product enumeration did not return, asked as a
    /// question about named products rather than inferred from two headcounts.
    ///
    /// THE REGISTRY NAMES THE MACHINE'S PRODUCTS AND SO DOES THE ENUMERATION, so a
    /// code the first holds and the second never returned is not evidence that
    /// something was missed; it is the thing that was missed, identified. Each one
    /// is then put to Windows on its own (<see cref="ResolveProductInstances"/>,
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
    /// residue of a failed or partial uninstall. It establishes nothing and costs
    /// nothing. This is where comparing names is worth the most: a count cannot tell
    /// this state from the one above.
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

        // Unbounded: one keyed read per code the enumeration did not return, on a set
        // already bounded by the machine's own registry keys, which the fallback has
        // just opened one at a time anyway. A cap would fall on the machines with the
        // most to recover.
        var unresolved = 0;
        foreach (var code in registryCodes)
        {
            ct.ThrowIfCancellationRequested();
            if (enumerated.Contains(code)) continue;

            var resolved = ResolveProductInstances(_msi, code);
            if (resolved.Unaskable) { unresolved++; continue; }
            foreach (var (sid, context) in resolved.Instances)
                recovered.Add((code, sid, context));
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
    /// IT IS THE CONDITION THE SUPERSEDED OFFER RESTS ON. A superseded patch is
    /// offered only where this pass has asked every product it knows of and none of
    /// them still holds it. Emptiness here is a machine with nothing removable, never
    /// a mechanism that is not needed.
    ///
    /// AND AN EMPTY WORK LIST DOES NOT RETURN AT THE TOP, which is a separate
    /// statement and the one most likely to be undone by somebody restoring an
    /// obvious saving. The per-product condition this method hosts is read by the
    /// offer AND by the missing-file split, and on a machine with nothing to offer
    /// the split is its only reader. A return before it leaves every patch row at
    /// the type's default, which the split reports, and a missing obsoleted
    /// registration is then named or not according to whether an unrelated program
    /// happens to hold an offer-eligible patch that day.
    /// </summary>
    /// <param name="recovered">
    /// Products the enumeration never returned and the registry comparison then
    /// found installed (<see cref="LocateProductsTheEnumerationMissed"/>). They are
    /// asked exactly as enumerated products are, which is the point: a product
    /// recovered by name can answer for the patches it holds.
    /// </param>
    /// <param name="reach">
    /// What the registry established about which cached files each product's own
    /// patches record. Read for the recovered products alone, and only to narrow the
    /// paths each is judged against; its default narrows nothing. See
    /// <see cref="EstablishedPatchReach"/>.
    /// </param>
    /// <remarks>
    /// INTERNAL RATHER THAN PRIVATE SO ITS TESTS CAN REACH IT, which is the same
    /// reason <see cref="IsRemovablePatch"/> and <see cref="MergeClaim"/> are. The
    /// patch truncation tests call it directly with a claimed set and its patch
    /// claims, so their assertions turn on this pass rather than on the enumeration
    /// that builds its inputs in production.
    ///
    /// Nothing re-grants a removable verdict this pass takes away, and nothing may:
    /// every path a verdict travels after the API loop is downgrade-only.
    /// </remarks>
    internal void ConfirmRemovableAgainstEveryProduct(
        Dictionary<string, RegisteredPackage> claimed,
        List<PatchClaim> patchClaims,
        List<(string ProductCode, string? UserSid, MsiInstallContext Context)> products,
        List<(string ProductCode, string? Sid, MsiInstallContext Context)> recovered,
        EstablishedPatchReach reach,
        IReadOnlyDictionary<string, ProductPatchSet>? registryPatchSets,
        IReadOnlyDictionary<string, ProductPatchSet> apiPatchSets,
        CancellationToken ct,
        PerItemFailureLog? unreadPatchFileLog = null)
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

        // THE RETURN FOR AN EMPTY WORK LIST IS BELOW THE PER-PRODUCT PASS, NOT HERE.
        // Everything from here to that pass is what the pass needs; everything after
        // it is the per-pairing work, which an empty list really does make pointless.
        //
        // The pass has two consumers and only one of them is the offer. The other is
        // the missing-files split, which reads the verdict for rows whose file has
        // gone, and those two sets are disjoint: a missing file is never offered. So a
        // machine with nothing to offer is precisely a machine where the split is the
        // only reader, and a return here would leave every row at the type's default
        // of Unestablished, which the split reports. Whether a user is warned about a
        // missing file would then turn on whether some UNRELATED program on the
        // machine held an offer-eligible superseded patch that day. The class that
        // moves is an obsoleted patch whose Uninstallable reads a positive zero.
        //
        // ON SUCH A MACHINE THE PASS COSTS LITTLE. The expensive half of this method
        // is the per-pairing property reads and the patch-file reads, and neither
        // happens on such a machine: the pairing loop is below the return, and the
        // pass reads a patch file only for a row that is still removable, of which
        // there are none. The two patch-set maps are built before this method is
        // called at all.
        // What is left is the machine-wide enumeration below, which reads no file and
        // which every machine that offers anything already pays for on every scan.
        //
        // The pairings the product loop already read, keyed by the INSTANCE that
        // answered and not by the product code alone. Re-asking gets the same answer
        // for the same reason only where the same instance is being asked: one product
        // code can be installed for two accounts, or per-machine and per-user at once,
        // and each instance holds its own patch registrations and answers for itself.
        // So the account and the context are part of what makes a pairing already
        // asked, and an instance this loop never reached is asked below rather than
        // taken as answered by another. What this pass is for is the pairings no
        // enumeration produced.
        //
        // The context is the raw API value on both sides. A claim carries it as an int
        // because the models keep no dependency on the interop layer, and the
        // enumerated form is cast to match rather than the claim being widened.
        var alreadyAsked = new HashSet<(string PatchCode, string ProductCode, string? UserSid, int Context)>();
        foreach (var claim in patchClaims)
            alreadyAsked.Add((claim.PatchCode, claim.ProductCode, claim.UserSid, claim.Context));

        // ROUTE A. Every (patch, product) pairing the API will name when asked
        // about no product in particular, which is the only way to hear about a
        // product the product enumeration never returned. Null where it did not
        // run to a clean end, and that withholds rather than reading as nothing
        // to report: the API returns no rows both where it refuses and where it
        // finds nothing, and the null is what tells the two apart.
        var holders = EnumeratePatchHoldersAcrossAllProducts(_msi, ct);

        // ROUTE B, READ ONCE PER PATH AND SHARED BY BOTH PASSES BELOW. The file names
        // the products it may be applied to, so it answers about a product no
        // enumeration returned, one route A cannot see among them.
        //
        // MEMOISED BECAUSE THE READS ARE THE EXPENSIVE PART AND THE ANSWER CANNOT
        // CHANGE WITHIN ONE SCAN. A path named by two patch codes would otherwise be
        // read once per pairing, and both passes want the same answer.
        // The cache is per call and dies with it, so nothing is carried between
        // scans and no staleness is possible.
        //
        // THE RESOLVE HAPPENS HERE AND NOT AT EITHER CONSUMER, because a declared
        // target is a product code and nothing more, and the two things a caller
        // needs to know about it are decided by the same call: whether it is
        // installed at all, and, if it is, which account and context to ask in. A
        // code the file names and the machine does not hold contributes nothing and
        // is not a failure; a code that could not be asked about withholds.
        //
        // AND EVERY ANSWER IS HELD AGAINST THE INSTALLATIONS THIS RUN LISTED. A declared
        // target answered "not installed", or answered with a list short of an
        // installation the product walk or the recovery by name established, withholds
        // as a code that could not be asked about does
        // (HoldsEveryListedInstallation).
        var listed = InstallationsByCode(ListedInstallations(products, recovered));
        var declaredByPath = new Dictionary<string, DeclaredTargets>(StringComparer.OrdinalIgnoreCase);
        DeclaredTargets DeclaredTargetsFor(string patchPath)
        {
            if (declaredByPath.TryGetValue(patchPath, out var already)) return already;

            var declared = TargetsDeclaredByPatchFile(patchPath, out var unreadable, unreadPatchFileLog);
            var installed = new List<(string ProductCode, string? Sid, MsiInstallContext Context)>();
            var unaskable = false;
            foreach (var target in declared)
            {
                // EVERY TARGET IS RESOLVED EVEN ONCE ONE HAS FAILED. Stopping at the
                // first failure gives the same outcome for the path, that path being
                // withheld on the flag below, and reading the rest is what makes one
                // cached answer serve both consumers rather than depending on which
                // of them asked first.
                var resolved = ResolveProductInstances(_msi, target);
                if (resolved.Unaskable || !HoldsEveryListedInstallation(listed, target, resolved.Instances))
                {
                    unaskable = true;
                    continue;
                }

                foreach (var (sid, context) in resolved.Instances)
                    installed.Add((target, sid, context));
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
            claimed, patchClaims, holders, recovered, reach, registryPatchSets, apiPatchSets,
            DeclaredTargetsFor, ct);

        // An empty work list settles it. Everything below is per-pairing and there are
        // no pairings to ask about.
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

            // Both withholdings are the same shape: a patch whose own declaration will
            // not be read has been shown to be unneeded by nobody, and a product it
            // names that Windows will not answer about, or answers about without an
            // installation this run listed, is a question left open rather than an
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
                // SO THIS READ ALONE DOES NOT PUT A SUPERSEDED FILE THAT HAS GONE ON THE
                // MISSING-FILES REPORT. Where its products' patch sets are clean and this
                // is the one reason the row was withheld, MissingFilesReport.Affected
                // reads the failed read as the tautology it is: the file would not read
                // because it has gone.
                Downgrade(claimed, path, withheld: true, unreadableFile: fromFile.Unreadable);
                continue;
            }
            toAsk.AddRange(fromFile.Installed);

            foreach (var (productCode, userSid, context) in toAsk)
            {
                if (alreadyAsked.Contains((patchCode, productCode, userSid, (int)context))) continue;

                ct.ThrowIfCancellationRequested();

                // State first and alone where it settles the pairing. A product
                // that does not hold this patch answers ERROR_UNKNOWN_PATCH to
                // the sizing call, so the overwhelming majority of pairings cost
                // one property read and the second is never made.
                var state = GetPatchProperty(_msi, patchCode, productCode, userSid, context,
                    MsiInstallProperty.State);

                // ONLY AN INSTALLATION ANSWERING THAT IT HOLDS NO RECORD OF THE PATCH IS
                // SKIPPED, AND NOT ONE THE MACHINE-WIDE PATCH ENUMERATION HAS LISTED AS
                // HOLDING IT. From any other installation that answer is a positive one
                // that it does not hold the patch, so it says nothing about the verdict
                // either way. From a listed holder it contradicts the listing, which
                // named the same product, account and context this read is put in, so
                // it withholds with every other read that did not answer.
                //
                // AN ANSWER THAT THE PRODUCT IS NOT INSTALLED IS NEVER SKIPPED. Every
                // installation on this list was listed earlier in this scan, by the
                // product enumeration, the recovery by name, the machine-wide patch
                // enumeration or the resolve of a declared target, so that answer
                // contradicts what the scan established, and it withholds too.
                if (state.PatchNotHeld && !IsListedHolder(named, productCode, userSid, context)) continue;

                if (state.Unreadable)
                {
                    Downgrade(claimed, path, withheld: true);
                    break;
                }

                // NOTHING HERE IS SKIPPED. The State read has just answered, and not
                // that this installation holds no record of the patch, so it holds
                // one, and an answer now that it does not, or that its product is not
                // installed, contradicts the one before it. Both are unreadable as
                // well, and withhold.
                var uninstallable = GetPatchProperty(_msi, patchCode, productCode, userSid, context,
                    MsiInstallProperty.Uninstallable);
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
    /// Whether <paramref name="listed"/>, the installations the machine-wide patch
    /// enumeration named as holding one patch, holds the installation of
    /// <paramref name="productCode"/> in <paramref name="userSid"/> and
    /// <paramref name="context"/>. Codes and accounts are compared without case, the
    /// enumerations each handing back their own spelling; the context is compared
    /// exactly. Null, the enumeration naming no installation for the patch, holds none.
    /// </summary>
    private static bool IsListedHolder(
        List<(string ProductCode, string? Sid, MsiInstallContext Context)>? listed,
        string productCode,
        string? userSid,
        MsiInstallContext context)
    {
        if (listed is null) return false;

        foreach (var holder in listed)
            if (holder.Context == context
                && string.Equals(holder.ProductCode, productCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(holder.Sid, userSid, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
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
    /// THE DOCUMENTED BLIND SPOT IS ON THE SID PARAMETER, SO IT IS NOT THIS ROUTE'S
    /// ALONE. Microsoft prints the limitation on
    /// <c>szUserSid</c>: "When enumerating for a user other than current user, any
    /// patches that were applied in a per-user-unmanaged context using a version less
    /// than Windows Installer version 3.0, are not enumerated". The per-product
    /// <see cref="EnumeratePatches"/> passes that product's own SID and context into
    /// the same export, so it carries the same limitation. DO NOT TAKE THE PER-PRODUCT
    /// LOOP AS THE COMPLETE HALF: <c>MergeClaim</c>'s downgrade-only rule is argued
    /// from that loop producing every product's claims.
    ///
    /// WHAT COVERS BOTH IS THE KEYED READ, AND THAT IS WHY THIS IS NOT THE ONLY ROUTE.
    /// The patch file's own declared targets are read alongside, and the keyed
    /// <c>MsiGetPatchInfoEx</c> reads both routes feed carry no such limitation: the
    /// administrator group may query patch data for any product instance and any user
    /// on the computer.
    ///
    /// NULL MEANS THE ANSWER IS NOT AVAILABLE AND EVERY REMOVABLE VERDICT IS
    /// WITHHELD, which is deliberate and is the more expensive direction. A short
    /// or refused enumeration read as "no other product holds it" is the fault
    /// this pass exists to close, so nothing here distinguishes a refusal from an
    /// empty machine.
    ///
    /// STATIC AND SHARED RATHER THAN COPIED, for the reason
    /// <see cref="ResolveProductInstances"/> is: <see cref="DeclaredProductCheck"/>
    /// finds the registrations of a patch a cached copy declares through the same
    /// walk, and what is worth sharing is which returns end the list and which leave
    /// it short. A second copy of that is a second place for a return to be read as
    /// the end of the list, and a list taken as ended short of its end is missing the
    /// registrations past that point.
    /// </summary>
    internal static Dictionary<string, List<(string ProductCode, string? Sid, MsiInstallContext Context)>>?
        EnumeratePatchHoldersAcrossAllProducts(IMsiApi msi, CancellationToken ct)
    {
        var holders = new Dictionary<string, List<(string, string?, MsiInstallContext)>>(
            StringComparer.OrdinalIgnoreCase);
        var patchCode = new char[Msi.GuidBufferLength];
        var targetProductCode = new char[Msi.GuidBufferLength];

        for (uint index = 0; index < MaxPatchIndex; index++)
        {
            ct.ThrowIfCancellationRequested();

            Array.Clear(patchCode);
            Array.Clear(targetProductCode);

            // Buffer and length are both per row, because the retry below hands
            // back a buffer sized to the row that needed it. Sizing every row
            // from the constant keeps the length this call declares true of the
            // buffer it passes.
            var sidBuffer = new char[SidBufferLength];
            uint sidLength = SidBufferLength;

            var error = msi.EnumPatches(
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
                error = msi.EnumPatches(
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
    /// where a declared target lives, or saying it in an answer that leaves out an
    /// installation this run listed. Both leave the question open and both withhold.
    /// </summary>
    private readonly record struct DeclaredTargets(
        IReadOnlyList<(string ProductCode, string? Sid, MsiInstallContext Context)> Installed,
        bool Unreadable,
        bool Unaskable);

    /// <summary>
    /// ROUTE B. The product codes a cached patch says in its own Template that it
    /// may be applied to.
    ///
    /// It is read from the FILE, so it does not depend on what any enumeration
    /// returned.
    ///
    /// IT NAMES THE PRODUCTS THE PATCH MAY TARGET, NOT THE PRODUCTS THAT HOLD IT.
    /// The per-product condition judges a cached patch file against four sets of
    /// products together: those whose own patch claims name the file, those route A
    /// names as holding one of its patch codes, those the registry comparison
    /// recovered by name, less any whose recorded patches were read and name only
    /// other files, and, for a row still removable, the installed products this route
    /// reads from the file's Template.
    ///
    /// A HOLDER THE TEMPLATE DOES NOT NAME HAS A DOCUMENTED PRODUCER.
    /// <c>MsiApplyPatchW</c> with <c>INSTALLTYPE_SINGLE_INSTANCE</c>: "the installer
    /// applies the patch to the product specified by szInstallPackage. In this case,
    /// other eligible products listed in the patch package are ignored and the
    /// szInstallPackage parameter contains the null-terminated string representing
    /// the product code of the instance to patch." A second INSTANCE of a product
    /// carries a ProductCode of its own, which the patch author had no reason to
    /// list. So the holders of a patch are NOT guaranteed to be a subset of what its
    /// Template names.
    ///
    /// Microsoft documents the Template as required and as "a semicolon-delimited
    /// list of the product codes that can accept the patch", so it is a real list
    /// rather than a hint. A Template naming MORE products than hold the patch adds
    /// each extra product to the judged set, and adding one can only withhold.
    /// </summary>
    /// <param name="unreadable">
    /// True where the file did not yield an identity, WHICH INCLUDES A FILE THAT IS
    /// NOT THERE. The read below is
    /// the only test, and a path naming no file fails it like any other. A patch whose
    /// own declaration cannot be read has not been shown to be unneeded by anybody, so
    /// the caller withholds rather than proceeding on the other two routes alone, and
    /// it does that for an absent file too.
    ///
    /// THE ABSENT CASE IS SEPARATED BY THE CALLER AND NOT HERE, because separating it
    /// here would need a filesystem this class does not have, and asking the real one
    /// would answer about a different machine from the one the scan is walking.
    /// </param>
    private IReadOnlyList<string> TargetsDeclaredByPatchFile(
        string path, out bool unreadable, PerItemFailureLog? failureLog = null)
    {
        unreadable = false;

        // Only a patch has a Template to read. A cached product package is not
        // this route's business and its absence of one is not a failure.
        if (!path.EndsWith(".msp", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<string>();

        // A FILE THAT IS NOT THERE IS LEFT TO THE READ BELOW AND FAILS IT. Nothing is
        // tested for here: the caller records which failure this was, and the scan, which
        // holds the filesystem, decides what it means. Where the patch is superseded or
        // obsoleted, its file has gone and every program sharing it has a clean patch
        // list, the missing-files split reads this failed read as the absence itself and
        // gives no warning for it. A test for the file here would ask the real disk rather
        // than the filesystem the scan walks.
        var identity = _identityReader.Read(path, isPatch: true, out var detail);
        if (identity is null)
        {
            unreadable = true;

            // THE READER'S DETAIL IS KEPT HERE, AS IT IS AT THE PRODUCT SCREEN.
            // It names which of the reader's failures occurred, and for the first of
            // them it carries the code Windows returned, which separates a path that
            // would not open from a file that opened and is not a package. Nothing acts
            // on the difference and nothing should: both withhold and both keep the
            // file. What it changes is whether anybody can tell, from a report, which
            // kind of machine they are looking at.
            //
            // THE PATH IS NOT NAMED, deliberately and for the reason the reader's own
            // contract gives: the app runs elevated, and this is read long after a
            // report about some other file. The class is the diagnostic here; which file
            // it was is not.
            //
            // NULL WHERE THERE IS NO RUN TO BUDGET AGAINST. The budget belongs to the
            // scan that owns the crash log for the run, so it is handed in rather than
            // made here; a test calling this pass directly has no run and writes nothing.
            failureLog?.Record(
                new InvalidOperationException(
                    "A registered patch file did not yield the products it declares, so its "
                    + "own removable verdict is withheld and the file is kept. Reader detail: "
                    + (detail.Length == 0 ? "none given" : detail) + "."),
                cause: detail);

            return Array.Empty<string>();
        }

        return identity.Value.TargetProductCodes;
    }

    /// <summary>
    /// Every installation of one product code, asked about that code alone.
    ///
    /// Route B yields a product code and nothing else, and a keyed patch read
    /// needs the account and context the instance lives in. The filtered product
    /// enumeration answers exactly that for a single code, a row per index until
    /// it reports no more, so it is a question about one product rather than a
    /// walk of the machine's list.
    ///
    /// ONE CODE CAN NAME MORE THAN ONE INSTALLATION, WHICH IS WHY EVERY ROW IS
    /// READ. The same product code is installed per machine and per user at once,
    /// or under two user accounts, and each of those is its own row with its own
    /// account and context. A keyed patch read is put in one account and one
    /// context and answers about that instance alone, so each instance is a
    /// separate place a cached patch can still be needed and all of them are
    /// returned.
    /// </summary>
    /// <returns>
    /// <c>Instances</c>, one per installation, each carrying the account and
    /// context to ask it in; empty with no <c>Unaskable</c>, meaning the code is
    /// positively not installed, which is a clean answer because a product that is
    /// not there holds no patches; or <c>Unaskable</c>, which withholds. Which
    /// returns say "not installed" is <see cref="IsProductNotInstalled"/>'s, and
    /// there is more than one of them.
    /// </returns>
    /// <remarks>
    /// A ROW THIS WALK CANNOT READ MAKES THE WHOLE CODE UNASKABLE RATHER THAN
    /// SHORTENING THE LIST. A list short by an unknown amount is a set of
    /// instances nothing asked about, and no caller can tell it from a machine
    /// holding only the rows it was handed; the answer that withholds is the one
    /// true of both. The index budget ends the same way and for the same reason:
    /// the enumeration ran out of it rather than reporting an end, so what is past
    /// it is unread.
    /// </remarks>
    /// <remarks>
    /// THE "NOT INSTALLED" ANSWER DEPENDS ON THE PROCESS BEING ELEVATED, AND THAT
    /// DEPENDENCY LIVES IN A FILE NOTHING HERE REFERENCES. Both hosts declare
    /// <c>requireAdministrator</c> in their app manifests, and the question is put with
    /// the Everyone SID across all contexts. An administrator may query product and
    /// patch data for any instance and any user on the computer; a caller who may not
    /// is told ERROR_UNKNOWN_PRODUCT about a per-user product belonging to another
    /// account, which this method reads as a positive "the machine does not hold it".
    ///
    /// THAT IS THE ONE PLACE A FALSE NEGATIVE HERE BECOMES A FILE ON THE OFFER. A
    /// product dropped from the per-product condition's set cannot contribute its
    /// removable patch, so a cached patch it could still reach for can be judged clean.
    /// The manifests carry the other half of this note.
    ///
    /// NO GUARD ASSERTS IT AND ONE WOULD NOT HELP MUCH. Checking elevation at startup
    /// is easy and would catch a manifest change, but elevation is not the property
    /// that matters: what matters is whether this call answered completely, and nothing
    /// distinguishes "not installed" from "not visible to you" in the return. A machine
    /// with no per-user product of another account is unaffected either way, and there
    /// is no way to ask whether such a product exists without the visibility in
    /// question.
    /// </remarks>
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
    internal static (IReadOnlyList<(string? Sid, MsiInstallContext Context)> Instances, bool Unaskable)
        ResolveProductInstances(IMsiApi msi, string productCode)
    {
        var installedCode = new char[Msi.GuidBufferLength];
        var instances = new List<(string? Sid, MsiInstallContext Context)>();

        // The same budget the machine-wide enumeration spends, because it is the same
        // API's index and the number is already argued there. Nothing else is capped
        // here: what ends the walk on a real machine is the API saying so.
        for (uint index = 0; index < MaxProductIndex; index++)
        {
            var sidBuffer = new char[SidBufferLength];

            // pcchSid is reset per row. The API overwrites it with the length it
            // wrote, so a row carried forward from the last one would size the next
            // call to whatever the last SID happened to be.
            uint sidLength = SidBufferLength;

            var error = msi.EnumProducts(
                productCode: productCode,
                userSid: AllUsersSid,
                context: MsiInstallContext.All,
                index: index,
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
                    index: index,
                    installedProductCode: installedCode,
                    installedContext: out context,
                    sid: sidBuffer,
                    sidLength: ref sidLength);
            }

            // AT ANY INDEX THIS IS THE END OF THE ROWS, and at the first it is also
            // the machine saying it does not hold the code at all. The two are one
            // return because they are one fact: there is no row here. What separates
            // them is whether anything was collected before it.
            if (IsProductNotInstalled(error)) return (instances, false);
            if (error != MsiError.Success) return (Array.Empty<(string?, MsiInstallContext)>(), true);

            var safeSidLength = (int)Math.Min(sidLength, (uint)sidBuffer.Length);
            var sid = (context != MsiInstallContext.Machine && safeSidLength > 0)
                ? new string(sidBuffer, 0, safeSidLength)
                : null;
            instances.Add((sid, context));
        }

        return (Array.Empty<(string?, MsiInstallContext)>(), true);
    }

    /// <summary>
    /// The installations in <paramref name="installations"/>, grouped by product code
    /// for <see cref="HoldsEveryListedInstallation"/>. Codes are compared without case,
    /// the enumeration and a package's own declaration each handing back their own
    /// spelling of one code.
    /// </summary>
    internal static Dictionary<string, List<(string? Sid, MsiInstallContext Context)>> InstallationsByCode(
        IEnumerable<ListedInstallation> installations)
    {
        var byCode = new Dictionary<string, List<(string? Sid, MsiInstallContext Context)>>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var installation in installations)
        {
            if (!byCode.TryGetValue(installation.ProductCode, out var of))
                byCode[installation.ProductCode] = of = [];
            of.Add((installation.UserSid, (MsiInstallContext)installation.Context));
        }

        return byCode;
    }

    /// <summary>
    /// Whether <paramref name="instances"/>, the answer
    /// <see cref="ResolveProductInstances"/> gave for <paramref name="productCode"/>,
    /// holds every installation of that code in <paramref name="listed"/>, the
    /// installations an enumeration earlier in the same run listed. Accounts are
    /// compared without case and the context exactly.
    ///
    /// FALSE IS A CONTRADICTION AND EVERY CALLER READS IT AS UNASKABLE. The product walk
    /// asks for the installations of every product and the keyed question for those of
    /// one, with the same account and the same contexts, so an answer leaving out an
    /// installation the walk listed contradicts the walk; and an installation the
    /// recovery by name established came from this same question earlier in the run, so
    /// an answer leaving it out contradicts that earlier answer. Either way it is "not
    /// installed" for a product the run established, or a list stopping short of one of
    /// its installations. An installation the answer holds and
    /// <paramref name="listed"/> does not is no contradiction, and is read like any
    /// other.
    /// </summary>
    internal static bool HoldsEveryListedInstallation(
        IReadOnlyDictionary<string, List<(string? Sid, MsiInstallContext Context)>> listed,
        string productCode,
        IReadOnlyList<(string? Sid, MsiInstallContext Context)> instances)
    {
        if (!listed.TryGetValue(productCode, out var ofCode)) return true;

        foreach (var (sid, context) in ofCode)
        {
            var held = false;
            foreach (var instance in instances)
                if (instance.Context == context
                    && string.Equals(instance.Sid, sid, StringComparison.OrdinalIgnoreCase))
                {
                    held = true;
                    break;
                }

            if (!held) return false;
        }

        return true;
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
    /// Puts a recorded cached-package value into the one spelling the folder walk
    /// produces, before it becomes a claim.
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
    /// AN ENVIRONMENT-VARIABLE FORM IS ANOTHER SUCH SPELLING AND IS EXPANDED HERE.
    /// A value spelled <c>%SystemRoot%\Installer\1e038.msi</c> is a claim on a real
    /// cached file, and <see cref="CarriesFlaggedSpelling"/> answers false for a
    /// <c>%</c>, so the expansion is what keeps such a value from reaching GetFullPath,
    /// which completes it from the process's working directory into a well-formed path
    /// naming nothing.
    ///
    /// AND IT MAKES THE ANSWER THE SAME WHATEVER THE VALUE'S REGISTRY TYPE. .NET
    /// expands a <c>REG_EXPAND_SZ</c> as part of reading it
    /// (<see cref="TryReadLocalPackage"/>). A <c>REG_SZ</c> value holding the same
    /// text, and anything the API side returns, is expanded here, so two registrations
    /// naming one location, one stored expandable and one stored plain, get one
    /// answer.
    ///
    /// WHAT THE EXPANSION DOES TO THE OFFER, ONE LINE PER HALF, because the two
    /// halves reach the list by opposite routes and no one sentence is true of both.
    /// A walked file is offered when no registration names it, so a value the
    /// expansion resolves takes its file OFF the list: the registration matches, and
    /// the file is claimed and kept. A value that expands to somewhere else either
    /// names nothing, which is what an unexpanded one does, or names some other file,
    /// which is then claimed and kept in its place. A registered superseded patch is
    /// on the list BECAUSE of its registration, and there the expansion can ADD:
    /// unexpanded, <c>%SystemRoot%\Installer\1e038.msi</c> names nothing, so the row
    /// reads as missing from disk and the branch that offers it is gated on the file
    /// being there; expanded, the row names the file that is really there and can
    /// reach the offer.
    ///
    /// AND WHAT MAKES THAT SAFE IS NOT THIS METHOD. Such a row is put to the same
    /// per-product condition, the same confirmation pass and the same act-time
    /// re-verify as every other row on the machine. The expansion settles which file
    /// a registration names and settles nothing about whether that file may go, so a
    /// row it repairs arrives at the offer's conditions unprivileged and is judged
    /// there, whichever classes the offer holds.
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
    /// TWO SPELLINGS ARE SETTLED ONLY BY THE FILESYSTEM, BECAUSE NEITHER IS
    /// DECIDABLE FROM THE STRING. Windows Installer names the files it caches itself,
    /// as short hex (<c>9f05cba.msi</c>, <c>1e4a2f.msp</c>), so the FILENAME cannot
    /// have a short form that differs; the path also carries the folder, and
    /// <c>Installer</c> is nine characters, so on a volume still creating 8dot3
    /// aliases the folder has a short form of its own and
    /// <c>C:\Windows\INSTAL~1\1a2b3c.msi</c> names an ordinary file a product
    /// still needs. On Windows, GetFullPath expands such a name through
    /// GetLongPathName wherever it exists on disk, which is the filesystem being
    /// asked, and leaves it as written where it does not. A volume-GUID path is the
    /// other, keeping its prefix for the reason
    /// <see cref="InstallerCacheHelpers.StripLongPathPrefix"/> gives, and GetFullPath
    /// returns a prefixed path unchanged. Neither matches the walk as written.
    ///
    /// Both are settled by asking the filesystem what the path really is, which
    /// is what <see cref="InstallerCacheHelpers.TryResolveFinalPath"/> already
    /// does at every containment gate. EVERY RECORDED PATH IS ASKED, and not only
    /// one announcing either spelling in its own characters.
    ///
    /// THE INVARIANT ASKING EVERY PATH BUYS IS WORTH MORE THAN THE SPELLINGS IT
    /// SETTLES. Every claim leaving this method is EITHER a location the kernel
    /// proved OR one whose failure to resolve has been counted.
    /// There is no third case, so a reader asking whether a claim's location was
    /// proved has an answer rather than a case analysis.
    ///
    /// WHAT THE COUNTED HALF THEN BUYS IS THE WALK-DERIVED OFFER, AND THAT HALF
    /// ALONE. A counted failure arms
    /// <c>EnumerationCensus.AnyRecordedPathUnestablished</c>, which keeps back every
    /// candidate the walk found and no registration claims; the registered side of
    /// the scan is decided elsewhere and is not keyed on it. So the surfaces that
    /// read a registration's own recorded path go on reading the string this method
    /// returned, proved or counted: the correlation gate, the missing-from-disk
    /// counts and the registered-files window.
    ///
    /// EACH OF THOSE TAKES AN UNPROVEN SPELLING IN THE DIRECTION THAT KEEPS MORE
    /// BACK, which is why the ask is worth making even though it settles the offer
    /// on one side only. A claim whose spelling names no walked file lowers the
    /// correlation count, which moves the scan towards refusing outright; one whose
    /// file is not found where the claim says raises the missing count, which is a
    /// warning rather than an offer.
    ///
    /// THE ASK COSTS A HANDLE PER REGISTRATION, on the smaller side of a cost the
    /// scan already pays. <c>CandidateGuard.CheckSafeToRemove</c> calls
    /// <see cref="InstallerCacheHelpers.TryResolveFinalPath"/> once per walked
    /// CANDIDATE, which is the same call over the far larger population, and is why
    /// the resolver rents its buffer rather than allocating one.
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
    /// makes all of them true, together with the scan stopping before its walk
    /// wherever the kernel spells the walked folder another way
    /// (<c>FileSystemScanService.SpellsTheSameFolder</c>).
    ///
    /// THE PREFIX IS NORMALISED BEFORE THE ASK, and that is not tidying. The NT
    /// object form (<c>\??\</c>) and the Win32 escape (<c>\\?\</c>) name the same
    /// object, which is why StripLongPathPrefix takes either off a drive-rooted
    /// path; over a volume GUID neither comes off, and the NT form then has its
    /// leading separator read as rooted on whatever drive the process is running
    /// from. Handing the resolver the Win32 spelling is what stops the resolution
    /// answering about a path assembled out of the running process's location.
    ///
    /// A PATH THE KERNEL DECLINES TO RESOLVE is kept in the spelling Windows gave and
    /// matches nothing the walk produces, so the refusal is counted and
    /// <c>EnumerationCensus.AnyRecordedPathUnestablished</c> withholds the whole
    /// walk-derived offer on it: nothing says WHICH candidate the unresolved claim
    /// meant, so no narrower set can be held back. Resolving a final path is
    /// <see cref="InstallerCacheHelpers.TryResolveFinalPath"/>, which answers yes or
    /// no; expanding an environment variable has no failure to report, and what it
    /// does with a variable the machine has never heard of is pinned by a test.
    /// </summary>
    private static string NormaliseLocalPackagePath(string value, PathCensus census)
    {
        // THE NULL IS TESTED BEFORE ANYTHING IS ATTEMPTED ON THE VALUE, AND IT IS THE
        // ONE ORDERING THAT WORKS. On Windows ExpandEnvironmentVariables TRUNCATES a
        // value holding an embedded null and does not throw:
        // C:\Windows\Installer\bad\0name.msi comes back as C:\Windows\Installer\bad,
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
        // IT ALSO TAKES A PLATFORM DIFFERENCE OUT OF THE MECHANISM. Off Windows the
        // same call returns the value untouched and GetFullPath then throws, so
        // without this test the one input would be refused at a different step on
        // each platform. A string test behaves the same everywhere, so both platforms
        // refuse the value here.
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
            // Expanding first makes it drive-rooted, so the strip takes the prefix
            // off. On a value holding no % the expansion returns the value unchanged.
            var expanded = InstallerCacheHelpers.ExpandRecordedPath(value);

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
            // COUNTED BEFORE THE ASK AND NOT GATING IT. The two spellings announce
            // themselves in the string, and this scan decides nothing: the resolver
            // below is put every recorded path whatever the scan says, so a reader
            // taking this line for a gate has it wrong. What it answers is how many
            // of a machine's recorded values carry such a spelling, which the
            // attempts count cannot report while everything is asked.
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
            // orphaned file. An embedded null is not one of them and is refused
            // above instead, because on Windows it never reaches this call: the
            // expansion truncates it away without throwing.
            //
            // AND THE FACT IS CARRIED OUT RATHER THAN ENDING HERE, WHICH IS WHAT
            // KEEPS THE FILE. What leaves this method is a claim that cannot match
            // anything the folder walk produces, so on its own it would leave the
            // file it means unclaimed and on the offer. The refusal recorded on the
            // next line is what stops that: it reaches
            // <c>EnumerationCensus.AnyRecordedPathUnestablished</c>, which withholds
            // the whole walk-derived offer, so no file is offered on the strength of
            // a claim nobody could read.
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
    /// IT DECIDES NOTHING, AND ITS ONE PRODUCTION CALLER FEEDS A COUNTER. Every
    /// recorded path is resolved whatever this answers, so a reader taking it for a
    /// gate on the final-path resolution has it wrong. What it does is COUNT, into
    /// <see cref="PathCensus.FlaggedSpellings"/>, and that is separate from
    /// <see cref="PathCensus.ResolverAttempts"/> because the attempts count is the
    /// number of paths asked about, which with every path asked is not the number
    /// carrying such a spelling. One counter serving both loses the second,
    /// and a report that stops being able to answer a question reads exactly like a
    /// machine that has nothing to report.
    ///
    /// It over-selects deliberately: a long name may legitimately hold a
    /// tilde-and-digit, and as a count a false positive inflates a figure nothing
    /// acts on. A false negative costs nothing at all, the resolution not depending
    /// on it.
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
    /// a product's own cached .msi. The order the enumeration reaches the claims in
    /// decides neither: once anything claims a path non-removable it stays
    /// non-removable, and an existing removable row is downgraded by a later
    /// non-removable claim; the verdict is never upgraded the other way.
    ///
    /// THE CAUSE IS KEPT OUT OF ENUMERATION ORDER AS WELL AS THE VERDICT, which is
    /// why there is a second rule rather than one. Two non-removable claims on a
    /// path are not necessarily the same finding: one product's Applied claim names
    /// the file, and another product's failed State read names nothing at all. So a
    /// claim that establishes something displaces a row that establishes nothing,
    /// and never the reverse, and neither what the app DOES with the file nor what it
    /// SAYS about it turns on which claim the enumeration reached first.
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

        // Downgrade only: a removable row loses to a later non-removable claim, and the
        // whole row goes with the verdict rather than the flag alone. A registration is
        // one product's account of the file, so what a machine ends up with is the
        // account of whichever product last displaced the row, its product name
        // included.
        if (existing.IsRemovable && !candidate.IsRemovable)
        {
            claimed[candidate.LocalPackagePath] = Displace(existing, candidate);
            return false;
        }

        // Both are non-removable and only one of them is a finding. The
        // IsRemovable test is what stops this reading as an upgrade: a removable
        // candidate never displaces anything here, so the row can only move from
        // "nothing was established" to "this product claims it", which is the
        // direction that costs no file and gains a true sentence.
        if (existing.VerdictUnreadable && !candidate.VerdictUnreadable && !candidate.IsRemovable)
            claimed[candidate.LocalPackagePath] = Displace(existing, candidate);

        return false;
    }

    /// <summary>
    /// The row a displacement leaves behind, and the one exception to displacing whole.
    /// Both of <see cref="MergeClaim"/>'s displacements come through here, so the rule
    /// is written once and a third displacement inherits it rather than having to
    /// remember it.
    ///
    /// THE PATCH STATE IS THE ONE FIELD A CLAIM CANNOT TAKE AWAY WITHOUT BRINGING ONE,
    /// and the two halves of that are separate. A claim carrying a state replaces what
    /// was there: one cached patch can be superseded under one product and still applied
    /// under another, and it is the applied reading that has to reach the row, or the row
    /// would say superseded on a machine where a product still holds the patch. A claim
    /// carrying no state leaves the state alone. Zero is what a State the enumeration
    /// could not read, or could not parse, arrives as, and it means not-a-patch to
    /// everything downstream that asks, so writing it over a state Windows gave would put
    /// a reading nobody made in front of one somebody did.
    ///
    /// ZERO IS THE TEST RATHER THAN THE ROW'S UNREADABLE FLAG, WHICH ANSWERS A WIDER
    /// QUESTION. That flag is the OR of a pairing's State read and its Uninstallable
    /// read, so it is set for a claim whose state Windows gave positively and whose
    /// Uninstallable alone would not read; keying on it would discard a reading the
    /// machine had made.
    ///
    /// A PRODUCT'S CLAIM IS THE OTHER SHAPE THAT ARRIVES CARRYING NO STATE, and it is
    /// why this belongs to displacement rather than to the downgrade. A product row is
    /// built from its LocalPackage alone, so its state is zero because nothing read one
    /// rather than because something read nothing, and a corrupt value can aim it at a
    /// patch's cached file. It establishes which product claims the path and nothing
    /// whatever about the patch.
    ///
    /// IT CANNOT PUT A FILE ON THE OFFER. Removability is granted where a row is built
    /// and never afterwards, and both rows reaching either displacement are already
    /// non-removable. Carrying the state forward widens what the later per-product pass
    /// looks at, and that pass only ever withholds.
    /// </summary>
    private static RegisteredPackage Displace(
        RegisteredPackage existing, RegisteredPackage candidate) =>
        candidate.PatchState == 0
            ? candidate with { PatchState = existing.PatchState }
            : candidate;

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
        var patchCodesByProduct = new Dictionary<string, IReadOnlyCollection<string>?>(
            StringComparer.OrdinalIgnoreCase);
        var cachedPathsByPatchCode = new Dictionary<string, IReadOnlyCollection<string>?>(
            StringComparer.OrdinalIgnoreCase);
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

                    // The two listings merge on the same rule and for the same reason:
                    // one subtree's complete listing does not make another subtree's
                    // failed one complete, so a reading that established nothing
                    // anywhere leaves the whole entry establishing nothing.
                    if (sidRead.Reach.PatchCodesByProduct is not null)
                        foreach (var (code, held) in sidRead.Reach.PatchCodesByProduct)
                            patchCodesByProduct[code] =
                                patchCodesByProduct.TryGetValue(code, out var seenHeld)
                                    ? MergeEstablishedNames(seenHeld, held)
                                    : held;

                    if (sidRead.Reach.CachedPathsByPatchCode is not null)
                        foreach (var (code, paths) in sidRead.Reach.CachedPathsByPatchCode)
                            cachedPathsByPatchCode[code] =
                                cachedPathsByPatchCode.TryGetValue(code, out var seenPaths)
                                    ? MergeEstablishedNames(seenPaths, paths)
                                    : paths;
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
            pathCensus,
            new EstablishedPatchReach(patchCodesByProduct, cachedPathsByPatchCode));
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
    internal static FallbackRead ReadFallbackSid(
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
        // The two halves of EstablishedPatchReach for this subtree, filled by the two
        // loops below. Both hold null for anything the read could not establish, and
        // a code absent from either is read the same way by the consumer.
        var patchCodesByProduct = new Dictionary<string, IReadOnlyCollection<string>?>(
            StringComparer.OrdinalIgnoreCase);
        var cachedPathsByPatchCode = new Dictionary<string, IReadOnlyCollection<string>?>(
            StringComparer.OrdinalIgnoreCase);

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
                                ref productPatchKeys, ref productPatchRegistrations,
                                out var heldCodes);
                            patchSets[unpacked] = patchSets.TryGetValue(unpacked, out var existing)
                                ? Worse(existing, set)
                                : set;
                            patchCodesByProduct[unpacked] =
                                patchCodesByProduct.TryGetValue(unpacked, out var seenCodes)
                                    ? MergeEstablishedNames(seenCodes, heldCodes)
                                    : heldCodes;
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
                            // AND THE CODE LIST GOES WITH IT. A read that threw
                            // established nothing about which patches this product
                            // holds, so as far as anything downstream may assume it
                            // holds any of them.
                            patchCodesByProduct[unpacked] = null;
                            failureLog.Record(ex, cause: "product-patches");
                        }
                    }

                    try
                    {
                        using var ipKey = productsKey.OpenSubKey($@"{prodGuid}\InstallProperties");

                        // Both names are read, and each one present is claimed: see
                        // CachedPackageValueNames for which installation writes which.
                        // The product counts once towards the unclaimed files however
                        // many of its values named one, because that figure is
                        // weighed against a count of products.
                        var unclaimedFileHere = false;
                        foreach (var valueName in CachedPackageValueNames)
                        {
                            if (!TryReadLocalPackage(ipKey, valueName, out var localPkg))
                            {
                                failures++;
                                // The only way this returns false is a value that was
                                // there and was not a string, so the two counters move
                                // together here and nowhere else: everything else
                                // reaching failures is a thrown exception.
                                nonStringValues++;
                                failureLog.Record(UnreadableLocalPackage(valueName),
                                    cause: $"product-{valueName.ToLowerInvariant()}");
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
                                    unclaimedFileHere = true;
                            }
                        }

                        if (unclaimedFileHere) unclaimedProductFiles++;
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

                    // WHICH PATCH THIS IS, TAKEN BEFORE ANYTHING INSIDE THE KEY IS
                    // READ, so a registration whose value will not read is still a
                    // registration this code can name and record its not-knowing
                    // against. A name that will not unpack names no patch to record
                    // anything about, and the consumer's own default answers it: a
                    // code it holds no entry for is a code whose cached file is
                    // unestablished.
                    var patchCode = UnpackRegistryProductCode(patchGuid);

                    // THE PATHS THIS PATCH RECORDS FOR ITSELF, one per value name that
                    // holds one, which is what lets a recovered product be judged
                    // against the files its own patches name rather than against every
                    // cached file. Normalised first, because the consumer compares them
                    // against a claimed path and those are normalised too.
                    //
                    // NOT ESTABLISHED UNLESS EVERY VALUE THAT IS THERE NAMES A PATH. A
                    // value that would not read may be recording any path, this one
                    // included, and a value that is there and empty records none, which
                    // EstablishedPatchReach.MustJudge answers by judging against every
                    // path. Either leaves the paths saying nothing, whatever the other
                    // name holds, as a key yielding no path does across account
                    // subtrees in the merge. The flag starts false and only the end of
                    // a read that threw nothing sets it, so every other way through,
                    // the throw included, leaves the paths saying nothing.
                    var recordedPaths = new List<string>(CachedPackageValueNames.Length);
                    var pathsEstablished = false;

                    try
                    {
                        using var patchKey = patchesKey.OpenSubKey(patchGuid);
                        var anyValueNamesNoPath = false;
                        var unclaimedFileHere = false;
                        foreach (var valueName in CachedPackageValueNames)
                        {
                            if (!TryReadLocalPackage(patchKey, valueName, out var localPkg))
                            {
                                failures++;
                                nonStringValues++;
                                anyValueNamesNoPath = true;
                                failureLog.Record(UnreadableLocalPackage(valueName),
                                    cause: $"patch-{valueName.ToLowerInvariant()}");
                            }
                            else if (localPkg is null)
                            {
                                // Not there at all: the key records nothing under this
                                // name, which leaves the other name's answer standing.
                            }
                            else if (localPkg.Length == 0)
                            {
                                anyValueNamesNoPath = true;
                            }
                            else
                            {
                                var path = NormaliseLocalPackagePath(localPkg, pathCensus);
                                recordedPaths.Add(path);
                                if (MergeClaim(claimed, new RegisteredPackage(path, "", ""),
                                        ClaimSource.RegistryFallback)
                                    && File.Exists(path))
                                    unclaimedFileHere = true;
                            }
                        }

                        if (unclaimedFileHere) unclaimedPatchFiles++;
                        pathsEstablished = !anyValueNamesNoPath && recordedPaths.Count > 0;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        failures++;
                        failureLog.Record(ex, cause: "patch-entry");
                    }

                    // RECORDED ON EVERY WAY OUT OF THAT READ AND NOT ONLY ON THE ONE
                    // THAT WORKED, so a registration that threw is on record as
                    // unestablished rather than as a patch nobody asked about. The two
                    // are the same answer to the consumer, and writing it is what keeps
                    // them the same answer if that ever stops being true.
                    if (patchCode is not null)
                    {
                        IReadOnlyCollection<string>? read =
                            pathsEstablished ? recordedPaths : null;
                        cachedPathsByPatchCode[patchCode] =
                            cachedPathsByPatchCode.TryGetValue(patchCode, out var seenPaths)
                                ? MergeEstablishedNames(seenPaths, read)
                                : read;
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
            Paths: pathCensus,
            Reach: new EstablishedPatchReach(patchCodesByProduct, cachedPathsByPatchCode));
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
    /// THE PATCH WHOSE REMOVABILITY COUNTS IS THE SUPERSEDING ONE. A rule reading the
    /// SUPERSEDED patch's own removability asks the wrong patch. Uninstalling patch C
    /// with the superseded patches' cached files present rolls a product back one
    /// step correctly; with those files missing it goes all the way to the unpatched
    /// base, discards both patches and reports success, and the log carries Windows
    /// looking for the absent files by name. So removing a superseded patch's cached
    /// file can silently cost somebody a security update, in exactly the operation
    /// Microsoft always named as the reason the file is cached.
    ///
    /// SO THE CONDITION IS ABOUT THE PRODUCT AND ABOUT EVERY PRODUCT. A superseded
    /// patch is cached once and registered once per product it applies to, and its one
    /// file is shared by all of them, so a rollback on ANY of those products reaches
    /// for it. A condition holding only for the product a loop happened to be standing
    /// in would offer a file that a second product's removable patch can still need,
    /// and a cached patch file can carry several registrations across more than one
    /// product.
    ///
    /// THE PRODUCTS ARE UNIONED TOO, not just the patches. The claims name the
    /// products the enumeration reached; route A names products it never returned;
    /// the patch file's own declared targets name products no enumeration on the
    /// machine has to have mentioned at all; and the fourth source is the products the
    /// enumeration lost and the registry comparison then recovered by name. A product
    /// any of the four names is a product the condition has to hold for.
    ///
    /// THE THIRD SOURCE NAMES A PRODUCT ROUTE A CANNOT SEE AND THAT CARRIES NO CLAIM
    /// FOR THIS PATH. Do not drop it: without it such a product is not in the set, its
    /// removable patch is not seen, and this condition can answer AllNonRemovable for
    /// a file that product can still reach for. The per-pairing pass below asks a
    /// product whether it holds the patch and can uninstall it, which is a different
    /// question and does not stand in for this one.
    ///
    /// THE FOURTH SOURCE IS A PRODUCT THE MACHINE-WIDE ENUMERATION NEVER RETURNED AND
    /// THE REGISTRY COMPARISON RECOVERED BY NAME, and it is handed to this condition as
    /// well as to the per-pairing pass. On a machine holding the same program twice
    /// with a patch applied to the second copy by name, that copy is the product that
    /// could roll back onto the file, and the per-pairing pass asking it whether it
    /// holds the patch answers the other question.
    ///
    /// AND THAT SOURCE IS THE ONE THAT IS NARROWED, which none of the other three is.
    /// The other three name a product BECAUSE of this path: a claim on it, a route A
    /// pairing for one of its codes, or the patch file naming the product as a target.
    /// The recovered products are named by the machine and not by the path, so unioning
    /// them everywhere keeps back every superseded patch on such a machine rather than
    /// the files at risk on it. <see cref="EstablishedPatchReach"/> is where a recovered
    /// product's own registry records are asked which files it could reach for, and
    /// where anything unestablished puts it back into every path.
    ///
    /// IT IS NOT A STATEMENT ABOUT THE FUTURE, and no copy may say it is: the
    /// condition is read here and re-read at act time, and a patch that is
    /// non-removable today can be replaced tomorrow by one that is not.
    /// </summary>
    private static void JudgeAndWithholdAgainstEveryProductPatchSet(
        Dictionary<string, RegisteredPackage> claimed,
        List<PatchClaim> patchClaims,
        Dictionary<string, List<(string ProductCode, string? Sid, MsiInstallContext Context)>>? holders,
        IReadOnlyList<(string ProductCode, string? Sid, MsiInstallContext Context)> recovered,
        EstablishedPatchReach reach,
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
        // A NULL ROUTE A IS ANSWERED HERE AND NOT ONLY BY THE CALLER. The caller's
        // downgrade takes a removable verdict away and skips a row that has none, so it
        // never reaches a row that was never removable. An obsoleted registration is
        // exactly that, and those rows read their verdict from this pass and from
        // nowhere else; see where the verdict is seeded below.
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

        // THE FOURTH SOURCE: THE PRODUCTS THE ENUMERATION LOST AND THE REGISTRY
        // COMPARISON RECOVERED BY NAME. The per-pairing pass below is handed these as
        // well, and the two ask different questions.
        // That one asks a product whether it holds THIS patch and can uninstall it,
        // and is answered truthfully that it cannot. This one asks whether the product
        // holds ANYTHING ELSE that could be uninstalled and roll back onto this file,
        // and a product nobody puts into the set is never asked it at all.
        //
        // JUDGED AGAINST THE FILES ITS OWN RECORDS SAY IT COULD REACH FOR, AND AGAINST
        // EVERY FILE WHEREVER THAT CANNOT BE ESTABLISHED. A product that positively
        // holds neither this patch nor any patch whose cached file this is cannot roll
        // back onto it, and keeping the file from it would keep it back for a reason
        // that is not true of it. MustJudge is where the two cases are told apart and
        // its default is the whole set; nothing here may infer completeness from the
        // shape of what came back.
        foreach (var (path, codes) in codesByPath)
            foreach (var (productCode, _, _) in recovered)
                if (reach.MustJudge(productCode, path, codes))
                    productsByPath[path].Add(productCode);

        foreach (var (path, products) in productsByPath)
        {
            ct.ThrowIfCancellationRequested();

            if (!claimed.TryGetValue(path, out var row)) continue;

            // ROUTE B, UNIONED IN HERE TOO. The sources above are the claims, route A
            // and the products the recovery by name found, and each of them can only
            // name a product something on the machine already lists: an enumeration,
            // or the registry's product keys. The patch file is read from disk and does
            // not care what any of them lists, so it is the one source that can name
            // the product whose removable patch would overturn this verdict and that
            // nothing else on the machine mentions. The per-pairing pass below unions
            // it for the same reason.
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
            // none to take. Such a row would otherwise carry a positively clean verdict
            // off a product set route A had refused to complete, and the missing-files
            // split would read that as the app having established the absence was
            // harmless.
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
    /// the EFFECTIVE list and not the registration list: for a product holding
    /// superseded patches it can list the applied patch alone and omit the superseded
    /// ones its subkeys name. So anything built on it can silently exclude the exact
    /// class this condition exists for.
    ///
    /// A CHECK ON A MACHINE WITH NO SUPERSEDED PATCH CANNOT REFUTE THIS. The
    /// disagreement is ABOUT superseded patches, so on such a machine the two can agree
    /// on every product and that agreement disproves nothing. The guard is a test
    /// rather than a machine: see
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
    /// <param name="patchCodes">
    /// The patch codes this product holds, unpacked out of the same subkey names the
    /// verdict is reduced from, or NULL where that listing was not established.
    ///
    /// TAKEN FROM THE NAME LISTING AND NOT FROM THE LOOP, which is what makes it
    /// complete on a product the loop returns early on. The loop stops at the first
    /// removable patch it finds, so a list built inside it would come back short
    /// exactly on the products that matter most.
    ///
    /// NULL ON EVERY PATH THAT DOES NOT POSITIVELY FINISH, and an empty collection is
    /// not the same answer: empty says this product holds no registered patch, null
    /// says nobody established what it holds. The caller reads the second as "this
    /// product may hold any patch on the machine".
    /// </param>
    internal static ProductPatchSet ReadProductPatchSet(
        Microsoft.Win32.RegistryKey productsKey,
        string packedProductCode,
        ref int patchKeys,
        ref int patchRegistrations,
        out IReadOnlyCollection<string>? patchCodes)
    {
        // NOT ESTABLISHED UNTIL IT IS, and this line rather than a failure path is
        // where that is decided: a path added below that forgets to set it leaves the
        // caller not knowing, which withholds, rather than holding an empty listing,
        // which offers.
        patchCodes = null;

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
        if (patchesKey is null)
        {
            // AND IT IS AN ANSWER ABOUT THE CODE LIST TOO. This product holds no
            // registered patch, so the complete list of the patches it holds is the
            // empty one, which is a positive statement and not the absence of one.
            patchCodes = Array.Empty<string>();
            return ProductPatchSet.AllNonRemovable;
        }

        // COUNTED WHERE THE KEY OPENED AND NOWHERE ELSE, unchanged by the line above.
        // The count answers how usual it is for a product to carry a Patches key at
        // all, read against ProductKeys, and a product that has no such key has not
        // got one whatever verdict is returned for it. Moving the increment up would
        // make the two counts agree on every machine and stop the pair saying
        // anything. This reading feeds the opt-in report, where a counter that
        // quietly changes meaning is worse than one that is missing.
        patchKeys++;

        // THE NAME LISTING IS WHAT EVERY COMPLETE ANSWER HERE COMES OFF, and the
        // loop below is the part that may stop early. Both the code list and the
        // registration count are taken from these names rather than out of that
        // loop, which is what leaves them complete where it returns. A key listing
        // cannot be truncated the way an enumeration can: GetSubKeyNames returns
        // every name under the key or throws, and the caller's catch turns a throw
        // into the null set above.
        var patchNames = patchesKey.GetSubKeyNames();

        // COUNTED OFF THE LISTING RATHER THAN INSIDE THE LOOP, so every registration
        // under the key reaches the figure. The loop returns on the first patch
        // declaring itself removable, so counting inside it would let a product
        // holding fifty-eight registrations contribute one, and would do so on
        // exactly the products that make ProductsWithRemovablePatch non-zero. The two
        // are collected in one walk and published side by side, so that figure would
        // go quiet on the machines it exists to measure, which reads as good news
        // rather than as a fault.
        patchRegistrations += patchNames.Length;

        // A NAME THAT WILL NOT UNPACK LEAVES THE LISTING UNESTABLISHED, because the
        // registry is saying this product holds a patch and nothing here can turn that
        // name into a code to compare. It is the same treatment, in the same
        // direction, that a product key name already gets one level up: what cannot be
        // named cannot be excluded.
        //
        // THE UNPACKING IS THE SAME TRANSFORM WHETHER THE GUID NAMES A PRODUCT OR A
        // PATCH. These keys are named in the packed form the installer writes for any
        // code it puts in a key name, and the reader is named for the caller that
        // needed it first rather than for the only thing it can read.
        var codes = new List<string>(patchNames.Length);
        var everyNameUnpacked = true;
        foreach (var patchName in patchNames)
        {
            var unpackedPatchCode = UnpackRegistryProductCode(patchName);
            if (unpackedPatchCode is null) { everyNameUnpacked = false; break; }
            codes.Add(unpackedPatchCode);
        }

        if (everyNameUnpacked) patchCodes = codes;

        var unestablished = false;
        foreach (var patchName in patchNames)
        {
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

        // THE VALUE THAT PERMITS IS THE ONE NAMED, and the inability is what everything
        // else reduces to. Written that way round, the merge answers a value added to
        // the enum by withholding, and letting one through has to be a deliberate edit
        // here as well as there.
        return a == ProductPatchSet.AllNonRemovable && b == ProductPatchSet.AllNonRemovable
            ? ProductPatchSet.AllNonRemovable
            : ProductPatchSet.Unestablished;
    }

    /// <summary>
    /// Merges two readings of one registry listing, which happens for the same reason
    /// <see cref="Worse"/> exists: one code can be registered under more than one SID
    /// subtree. It serves both listings in
    /// <see cref="EstablishedPatchReach"/>, the patch codes a product holds and the
    /// cached paths a patch records.
    ///
    /// NOT ESTABLISHED WINS, on the same rule the verdicts use: a subtree whose listing
    /// could not be taken is not made complete by another subtree's listing that could.
    /// Only where BOTH readings finished is the union of them a complete statement, and
    /// only a complete statement may exclude anything.
    /// </summary>
    internal static IReadOnlyCollection<string>? MergeEstablishedNames(
        IReadOnlyCollection<string>? a, IReadOnlyCollection<string>? b)
    {
        if (a is null || b is null) return null;
        var union = new HashSet<string>(a, StringComparer.OrdinalIgnoreCase);
        foreach (var name in b) union.Add(name);
        return union;
    }

    /// <summary>
    /// The two values a registration under <c>UserData</c> records its cached package
    /// in: <c>LocalPackage</c>, and <c>ManagedLocalPackage</c>, which is where a
    /// per-user managed installation records it, for a product in its
    /// <c>InstallProperties</c> key and for a patch in the patch's own key. The
    /// fallback reads both and claims whatever either names, so a cached package is
    /// claimed whichever context it was installed in.
    ///
    /// A NAME LEFT OFF THIS LIST IS A CACHED PACKAGE THE FALLBACK NEVER CLAIMS, and
    /// nothing counts the omission, because the value is never asked for. A context
    /// found to record its cached package under a third name is added here.
    /// </summary>
    internal static readonly string[] CachedPackageValueNames = ["LocalPackage", "ManagedLocalPackage"];

    /// <summary>
    /// Reads one of the values in <see cref="CachedPackageValueNames"/>, separating the
    /// two ways it can yield nothing, because only one of them is a failure and the
    /// caller's count is weighed by the degraded-sources gate.
    ///
    /// A registration with no such value is an ordinary state: an advertised or
    /// partially removed product carries no cached path and there is nothing to
    /// read. A value that is PRESENT and is not a string is a read that failed.
    /// Discarding the second silently through a cast contributes no claim and no
    /// failure, so a subtree of them reads as a fallback that ran cleanly and
    /// found nothing to say, which is the one state the gate exists to tell apart
    /// from a healthy machine.
    ///
    /// Nothing writing these keys is obliged to use REG_SZ.
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
    /// AND IT EXPANDS A <c>REG_EXPAND_SZ</c> VALUE. .NET expands that type as part of
    /// the read, so a registration spelled <c>%SystemRoot%\Installer\...</c> and STORED
    /// expandable comes back as a usable path here. The same text stored as a plain
    /// <c>REG_SZ</c> is expanded by <c>NormaliseLocalPackagePath</c>, on the main path,
    /// so both storage types reach the claim as the path they name.
    /// </summary>
    internal static bool TryReadLocalPackage(
        Microsoft.Win32.RegistryKey? key, string valueName, out string? path)
    {
        path = null;

        // Absent by structure: not a failed read.
        if (key is null) return true;

        // RegistryValueOptions.None is the option that selects expansion, and it is
        // passed explicitly rather than left to the default so a reader can see the
        // expansion, which pairs with NormaliseLocalPackagePath. GetValue(name)
        // delegates to this same overload with this same option, so dropping the
        // argument changes what the call says and not what it does.
        var raw = key.GetValue(valueName, null, Microsoft.Win32.RegistryValueOptions.None);
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
            if (string.Equals(name, valueName, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    /// <summary>
    /// The exception carrying an unreadable cached-package value into the per-item
    /// failure log. It names the value and no path and no product: the log is read
    /// after a report of missing registered files, and the app runs elevated, so a
    /// registry value from another account's subtree is not something to write
    /// down for a diagnosis that does not need it. The cause string at the call
    /// site says which of the two loops raised it.
    /// </summary>
    private static InvalidDataException UnreadableLocalPackage(string valueName) =>
        new($"A registered {valueName} value was present and was not a string.");

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
    /// tidiness check: every caller turns a code into a question about a real
    /// machine, and each reads a key name that yields none as something not
    /// established rather than as a code.
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

    /// <summary>
    /// A braced product or patch code written in the packed form Windows Installer names
    /// its registry keys with, the inverse of <see cref="UnpackRegistryProductCode"/>, in
    /// upper case as the installer writes it.
    ///
    /// Null for anything that is not a braced GUID, so a key path built from it names no
    /// key rather than one belonging to something else.
    /// </summary>
    internal static string? PackRegistryCode(string code)
    {
        if (code.Length != 38 || code[0] != '{' || code[37] != '}') return null;
        for (var i = 1; i < 37; i++)
        {
            if (i is 9 or 14 or 19 or 24)
            {
                if (code[i] != '-') return null;
            }
            else if (!char.IsAsciiHexDigit(code[i]))
            {
                return null;
            }
        }

        var text = code.ToUpperInvariant();
        var packed = new char[32];
        CopyReversed(text, 1, 8, packed, 0);
        CopyReversed(text, 10, 4, packed, 8);
        CopyReversed(text, 15, 4, packed, 12);
        CopySwappedPairs(text, 20, 4, packed, 16);
        CopySwappedPairs(text, 25, 12, packed, 20);

        return new string(packed);
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

    /// <summary>
    /// Enumerates every installed product across all contexts, or refuses the scan.
    /// The walk reads rows until Windows reports the end of the list, and a row it
    /// cannot read stops it, the scan then refusing with <c>Error.MsiNonSuccess</c>.
    /// <c>UnreadableRows</c> counts the rows the walk passed without reading, so it is
    /// zero on every walk that returns. It seeds <c>unreadableProducts</c> and the
    /// census carries it as <see cref="EnumerationCensus.SkippedProductRows"/>.
    /// </summary>
    private (List<(string ProductCode, string? UserSid, MsiInstallContext Context)> Products, int UnreadableRows)
        EnumerateProducts(CancellationToken ct)
    {
        var results = new List<(string, string?, MsiInstallContext)>();
        var productCode = new char[Msi.GuidBufferLength];
        int unreadableRows = 0;
        uint lastError = MsiError.Success;
        bool reachedEnd = false;

        // THE INDEX ADVANCES ONLY PAST A ROW THAT READ, which is what
        // MsiEnumProductsEx documents: "The index should be incremented, only if
        // the previous call has returned ERROR_SUCCESS." A row that does not read
        // ends the walk, and the check after the loop refuses the scan on it.
        //
        // Do not turn that stop into a skip. The API documents its output buffers
        // for ERROR_SUCCESS and ERROR_MORE_DATA only, so a row that did not read
        // names no product, and a walk that stepped past it would hand the rest of
        // the scan a list short of a product the walk cannot name. The rows after
        // it would also be asked for at an index the documentation says not to use.
        for (uint index = 0; index < MaxProductIndex; index++)
        {
            ct.ThrowIfCancellationRequested();

            // Zero the GUID buffer between iterations so a previous
            // call's longer GUID can't leak via BufferToString's null-
            // scan if the next call wrote a shorter string. The MSI
            // API zero-terminates so this is belt-and-braces, but the
            // belt is cheap.
            Array.Clear(productCode);

            // Buffer and length are both per row, because the retry
            // below hands back a buffer sized to the row that needed
            // it. Sizing every row from the constant keeps the length
            // this call declares true of the buffer it passes.
            var sidBuffer = new char[SidBufferLength];

            // pcchSid is the buffer size in characters including the
            // null terminator on the Win32 input. On Success the API
            // updates it to the count EXCLUDING the terminator. Pass
            // the full SidBufferLength so any plausible SID fits on
            // the first call.
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
                // MoreData asks for a larger SID buffer. Real-world SIDs are
                // ~45 chars and the first call passes 256, so no ordinary SID
                // needs it. On MoreData pcchSid carries the SID length
                // EXCLUDING the terminator ("not including the terminating NULL
                // character", MsiEnumProductsExW on pcchSid), and the documented
                // retry size is that count plus one for the null the buffer must
                // also hold.
                //
                // Windows can also answer MoreData for a product key whose name
                // is too long to be a packed product code. No SID buffer helps
                // there: the retry answers MoreData again, and the row stops the
                // walk as a row that did not read.
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
            // whichever call produced this row's answer. With the access check
            // above the retry, an AccessDenied returned BY the retry would reach
            // the arm for a row that did not read, and the scan would refuse
            // saying an entry came back unreadable when Windows had refused
            // access.
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
                    // A Success return that wrote no product code is a row that
                    // did not read: there is no code to read the product's
                    // cached package with, so its file would be missing from the
                    // registered set. It stops the walk like any other such row,
                    // and the refusal carries the code the call returned, which
                    // is Success.
                    unreadableRows++;
                    break;
                }

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
                // Any other return is a row that did not read,
                // ERROR_BAD_CONFIGURATION among them, which MsiEnumProductsEx
                // documents as a per-row return.
                unreadableRows++;
                break;
            }
        }

        // The refusal for a row that did not read. It sits after the loop rather
        // than in each arm, so an arm that counted a row and carried on is refused
        // here as well, and the walk cannot return with a row unread. It comes
        // before the check below because a walk stopped by an unread row has not
        // reached the end either, and would otherwise be refused under the message
        // for a list that never ended.
        if (unreadableRows > 0)
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_MsiNonSuccess, lastError,
                    results.Count, Helpers.DisplayHelpers.PluraliseProduct(results.Count)));

        // Hitting the index cap is not a clean end: the enumeration ran out of
        // budget rather than reporting NoMoreItems, so everything past the cap
        // would be unseen and classified orphaned. Cannot happen on a real
        // machine (nobody has 10,000 products), but if it ever did it falls to
        // the catastrophic side, so fail loudly rather than truncate silently.
        //
        // This carries its own message and must not share the one above. Every
        // row before the cap read, so there is no unreadable entry to report, and
        // the error code is the last row's, which is Success whenever the list
        // simply never terminated.
        if (!reachedEnd)
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_MsiEnumerationNeverEnded, MaxProductIndex, lastError,
                    results.Count, Helpers.DisplayHelpers.PluraliseProduct(results.Count)));

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
    private const int MaxConsecutiveNonSuccess = 20;

    /// <summary>
    /// Enumerates one product's patches. <c>Incomplete</c> reports that at least
    /// one row was skipped, which costs this product's claim on whatever patch
    /// the row named. The caller counts the product once in
    /// <c>unreadableProducts</c>, which withholds the removable class.
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
                // one unreadable product and carries on. Nothing is offered on the
                // strength of the rows it did not read: the removable class is
                // withheld scan-wide the moment any product is short (so no
                // superseded patch is offered on a run that lost a claim), and the
                // registry fallback claims this product's cached .msp/.msi files
                // independently of the API (so none looks orphaned). On a machine
                // whose registration refuses one product's patch list on every
                // scan, the removable class is withheld on every scan until the
                // registration changes. Declining to name a patch list Windows
                // refuses to return beats guessing at one. A whole-machine
                // breakdown still aborts: the AccessDenied and never-ended-cap
                // throws stay fatal.
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
                string.Format(Strings.Error_MsiPatchEnumerationNeverEnded, MaxPatchIndex, lastError,
                    results.Count, Helpers.DisplayHelpers.PluralisePatch(results.Count)));

        return (results, incomplete);
    }

    /// <summary>
    /// Records that one product's patch enumeration was abandoned after a full run
    /// of unreadable rows. Dev-facing crash-log breadcrumb only, deliberately not
    /// localised and never surfaced: what the user is told is at most a count of the
    /// superseded files the scan held back, and the count names no product, whereas
    /// diagnosing WHY the withholding fired needs exactly that identity. Without this
    /// line the abandonment leaves no record of which product
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
    ///
    /// <paramref name="PatchNotHeld"/> NARROWS IT AGAIN, AND ONLY THE PATCH READ SETS
    /// IT. <see cref="GetPatchProperty"/> sets it for the one return an installation
    /// gives when it holds no record of the patch asked about; see
    /// <see cref="IsPatchNotHeld"/>. <paramref name="NotRegistered"/> carries that
    /// return and ERROR_UNKNOWN_PRODUCT alike, and the second is an answer about the
    /// installation rather than the patch, so a caller that has already established the
    /// installation is there asks this instead.
    /// </summary>
    internal readonly record struct PropertyRead(
        string Value, bool Unreadable, bool NotRegistered = false, bool PatchNotHeld = false);

    /// <summary>
    /// HALF the rule that decides whether a patch's cached .msp is offered, from its
    /// State and Uninstallable values exactly as <c>MsiGetPatchInfoEx</c> returned
    /// them. The other half is
    /// <see cref="JudgeAndWithholdAgainstEveryProductPatchSet"/> and a row this returns true for
    /// is still withheld unless every product sharing the patch passes that.
    /// **Nothing may read this alone as permission to remove a file.**
    ///
    /// SUPERSEDED ONLY, WHICH IS STATE 2 AND NOT <c>2 or 4</c>. An obsoleted patch is not
    /// offered. It is counted at scan time, off the machine rather than off the offer.
    /// Widening this test to 4 would offer every obsoleted patch that passes the same
    /// tests, under the superseded label.
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
    /// AND ON ITS OWN THE CONJUNCT ASKS THE WRONG PATCH. Against real patches it
    /// behaves as a vendor filter pointing the wrong way: every patch in Office 2010
    /// SP2 declares itself removable, so the conjunct alone refuses all of them, and
    /// Adobe patches can declare themselves not removable, so it alone passes them.
    /// The declaration tracks the vendor rather than the risk. The risk turns on
    /// whether the patch that SUPERSEDED this one can be uninstalled, which this never
    /// reads, and that is what the other half reads.
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
    /// The benign returns of a property read, through <c>MsiGetProductInfoEx</c>,
    /// <c>MsiGetPatchInfoEx</c> or <c>MsiSourceListGetInfo</c>, as an ALLOWLIST.
    /// ERROR_SUCCESS is a value (or, at zero length, a property present and
    /// empty); ERROR_UNKNOWN_PROPERTY is the answer for a property the record
    /// does not carry, which is what a product or a registered-not-applied patch
    /// with no cached package gives. The two cases are distinguishable: an
    /// absent property answers 1608 rather than a zero-length success, and a
    /// product that cannot be read answers a real error, 87 among them. Both
    /// shapes of absence, 1608 and a zero-length success, are on this list, so
    /// either lands on the benign side.
    ///
    /// The direction matters more than the membership. One machine can show
    /// which codes ARE benign; no machine can enumerate every failure code that
    /// exists, so an unlisted code falls to the unreadable side and withholds.
    /// Do not invert this into a list of known-bad codes: the failure nobody has
    /// seen yet would then read as an absence and silently delete a product's
    /// claim on a file it still needs.
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
    /// Only the under-lease re-read of a batch's own pairings asks this, and it holds
    /// the file back on it. The other consumers of <see cref="PropertyRead"/> read
    /// <c>Unreadable</c>, which is set for both codes, <c>PatchNotHeld</c>, which
    /// carries the second alone (<see cref="IsPatchNotHeld"/>), or the value and
    /// nothing else, so none of them takes an answer that the product is not
    /// installed as the record being absent.
    /// </summary>
    private static bool IsRecordAbsent(uint error) =>
        error is MsiError.UnknownProduct or MsiError.UnknownPatch;

    /// <summary>
    /// The one return of <c>MsiGetPatchInfoEx</c> that says the installation the read
    /// named holds no record of the patch: ERROR_UNKNOWN_PATCH. A further ALLOWLIST, for
    /// the reason the ones above are, and narrower than <see cref="IsRecordAbsent"/> by
    /// exactly one code.
    ///
    /// ERROR_UNKNOWN_PRODUCT IS NOT ON IT. Microsoft's return table for the function
    /// glosses that code as the product not being installed on the computer, which is an
    /// answer about the installation rather than about the patch, and an installation
    /// that is there and does not hold the patch answers ERROR_UNKNOWN_PATCH. So a caller
    /// that listed the installation moments earlier and is then told it is not there has
    /// an answer contradicting what the run established, and it stays on the unreadable
    /// side, which withholds.
    /// </summary>
    private static bool IsPatchNotHeld(uint error) => error is MsiError.UnknownPatch;

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
    /// THE SAME CODE IS CLASSIFIED AT SIX OTHER POINTS IN THIS FILE, THREE OF
    /// THEM THE OTHER WAY, AND NOT ONE OF THOSE DISAGREEMENTS IS AN INCONSISTENCY
    /// TO TIDY AWAY. What separates the sites is whether something earlier in the
    /// same run has already established that the product exists, and not whether
    /// the call names it: four of the six name a product or a patch, and none of
    /// them lets the scan act on the code as an absence.
    ///
    /// Nothing has established it here, which is what the paragraph above is
    /// about: the question this call puts is whether the machine holds the code
    /// at all, so a no is the machine answering.
    ///
    /// <see cref="EnumerateProducts"/> and
    /// <see cref="EnumeratePatchHoldersAcrossAllProducts"/> both pass a null
    /// product code, so there is no product for the code to be reporting absent
    /// and it cannot carry this meaning. The first reads it as a row that did not
    /// read and refuses the scan on it; the second reads it as a set short by an
    /// unknown amount.
    ///
    /// <see cref="EnumeratePatches"/> NAMES A PRODUCT AND IS STILL RIGHT TO TREAT
    /// IT AS A FAILURE. That product came out of the product enumeration moments
    /// earlier, so an absence contradicts what the run has already established,
    /// and the reading that fits both is a registration whose patch list this
    /// scan cannot see. What reading it as an absence would cost is written at
    /// that line.
    ///
    /// ONE RETURN IS CLASSIFIED MORE THAN ONCE IN ONE EXPRESSION, ON PURPOSE, AT
    /// <see cref="ReadProductProperty"/>, <see cref="GetPatchProperty"/> AND
    /// <see cref="ReadSourceListProperty"/>, which are the other three points.
    /// <see cref="IsBenignPropertyRead"/> does not carry the code, so the read
    /// is Unreadable, and every consumer that decides anything on the read
    /// withholds on it, the product enumeration's ProductName read being the one
    /// that only names a row and takes the value alone;
    /// <see cref="IsRecordAbsent"/> does carry it, so the same return is
    /// NotRegistered as well, which the under-lease re-read of a batch's own
    /// pairings alone asks and which is a different question: whether a
    /// registration has gone since the scan read it. At
    /// <see cref="GetPatchProperty"/> a third predicate,
    /// <see cref="IsPatchNotHeld"/>, leaves the code off PatchNotHeld, which is the
    /// question a caller that has just listed the installation puts: whether that
    /// installation holds no record of the patch. The predicates read as a
    /// contradiction until that is known. Putting the code on the benign list to
    /// settle them would turn a record that has gone into a readable empty value,
    /// which is the direction that costs a file.
    ///
    /// The meaning is the question's, not the number's.
    /// </summary>
    private static bool IsProductNotInstalled(uint error) =>
        error is MsiError.NoMoreItems or MsiError.UnknownProduct;

    /// <summary>
    /// What one product answered when asked whether it is installed as a second
    /// instance of itself. Three states, because the question has three answers and
    /// collapsing the third into either of the others is the fault this whole rule
    /// exists to avoid: a product that would not answer has NOT been shown to be
    /// ordinary.
    /// </summary>
    private enum InstanceReading
    {
        /// <summary>An ordinary single-instance installation, positively established.</summary>
        Ordinary,

        /// <summary>Installed under an instance transform as a second instance of itself.</summary>
        SecondInstance,

        /// <summary>The question was put and not answered. Neither of the above.</summary>
        Unreadable,
    }

    /// <summary>
    /// Puts the second-instance question to one product.
    ///
    /// ONE COPY OF THE CLASSIFICATION, FOR THE REASON <see cref="IsProductNotInstalled"/>
    /// IS SHARED RATHER THAN COPIED. Two call sites ask it, the product enumeration's own
    /// loop and the products that loop lost and the registry named, and what is worth
    /// sharing is not the property read: it is which readings count as an answer. A second
    /// copy of that is a second place for a spelling to be handled, or not handled, and the
    /// direction it fails in is a machine wrongly reported ordinary.
    ///
    /// A POSITIVE READING IS THE ONLY THING THAT REPORTS <see cref="InstanceReading.SecondInstance"/>.
    /// An absent property is documented as meaning an ordinary installation, and
    /// <see cref="IsBenignPropertyRead"/> already puts ERROR_UNKNOWN_PROPERTY on the benign
    /// side, so a record that never carried the property arrives here as a readable empty
    /// value and is Ordinary rather than Unreadable. That is what keeps the rule off every
    /// machine in the world: a value that will not parse is not a positive either.
    ///
    /// The value is compared as a NUMBER rather than against the string "1", because
    /// nothing documents the spelling the API returns and a machine answering "01" or "1 "
    /// would read as ordinary on a string test.
    /// </summary>
    private InstanceReading ReadInstanceType(string productCode, string? userSid, MsiInstallContext context)
    {
        var read = GetProductProperty(productCode, userSid, context, MsiInstallProperty.InstanceType);
        if (read.Unreadable) return InstanceReading.Unreadable;
        return int.TryParse(read.Value.TrimEnd('\0').Trim(), out var instanceType) && instanceType != 0
            ? InstanceReading.SecondInstance
            : InstanceReading.Ordinary;
    }

    /// <summary>
    /// Retrieves a product property through this service's own API.
    /// See <see cref="ReadProductProperty"/>.
    /// </summary>
    private PropertyRead GetProductProperty(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string propertyName) =>
        ReadProductProperty(_msi, productCode, userSid, context, propertyName);

    /// <summary>
    /// Retrieves a product property using the double-call buffer pattern,
    /// reporting whether an empty result is an absence or a failed read (see
    /// <see cref="PropertyRead"/>).
    ///
    /// STATIC AND SHARED RATHER THAN COPIED, for the reason
    /// <see cref="ResolveProductInstances"/> is: <see cref="DeclaredProductCheck"/>
    /// reads the same property of the same records, and which returns count as an
    /// absence rather than a failed read is decided here once.
    /// </summary>
    internal static PropertyRead ReadProductProperty(
        IMsiApi msi,
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string propertyName)
    {
        uint bufferLen = 0;

        var error = msi.GetProductInfo(
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

        error = msi.GetProductInfo(
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
                NotRegistered: IsRecordAbsent(error), PatchNotHeld: IsPatchNotHeld(error));

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

        // See ReadProductProperty: the second call's narrower rule, and the
        // reason for the clamp.
        return error == MsiError.Success
            ? new PropertyRead(new string(buffer, 0, (int)Math.Min(bufferLen, (uint)buffer.Length)), Unreadable: false)
            : new PropertyRead(string.Empty, Unreadable: true);
    }

    /// <summary>
    /// Retrieves a property of the source list Windows Installer holds for a product or
    /// a patch in one account and context, using the double-call buffer pattern and
    /// reporting whether an empty result is an absence or a failed read (see
    /// <see cref="PropertyRead"/>). <paramref name="codeKind"/> is
    /// <see cref="MsiSourceListOptions.Product"/> or
    /// <see cref="MsiSourceListOptions.Patch"/>, saying which of the two
    /// <paramref name="code"/> is.
    ///
    /// STATIC AND SHARED RATHER THAN COPIED, for the reason
    /// <see cref="ReadProductProperty"/> is: <see cref="DeclaredProductCheck"/> reads a
    /// patch's package name through it, and which returns count as an absence rather
    /// than a failed read is decided here once.
    /// </summary>
    internal static PropertyRead ReadSourceListProperty(
        IMsiApi msi,
        string code,
        string? userSid,
        MsiInstallContext context,
        uint codeKind,
        string propertyName)
    {
        uint bufferLen = 0;

        var error = msi.GetSourceListInfo(
            productCodeOrPatchCode: code,
            userSid: userSid,
            context: context,
            options: codeKind,
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

        error = msi.GetSourceListInfo(
            productCodeOrPatchCode: code,
            userSid: userSid,
            context: context,
            options: codeKind,
            property: propertyName,
            value: buffer,
            valueLength: ref bufferLen);

        // See ReadProductProperty: the second call's narrower rule, and the
        // reason for the clamp.
        return error == MsiError.Success
            ? new PropertyRead(new string(buffer, 0, (int)Math.Min(bufferLen, (uint)buffer.Length)), Unreadable: false)
            : new PropertyRead(string.Empty, Unreadable: true);
    }
}
