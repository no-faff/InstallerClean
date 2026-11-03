using InstallerClean.Helpers;
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
    /// SID meaning "all users". Passed to MsiEnumProductsEx or MsiEnumPatchesEx, it
    /// asks for enumeration across all users, which needs admin elevation. What that
    /// leaves out is read from the registry (<see cref="UnlistedEntries"/>).
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
    /// <param name="UnclaimedProductFileCodes">
    /// One member per product entry with a cached-package path, under either name in
    /// <see cref="CachedPackageValueNames"/>, that the API's own loop never claimed
    /// AND whose file is on the disk, however many of its values named one. Each
    /// member is the product code unpacked out of the entry's key name, or null where
    /// the name is not a packed GUID. <see cref="GetRegisteredPackagesCore"/> reads the
    /// code to decide whether the entry is a program the scan has already settled.
    /// </param>
    /// <param name="UnclaimedPatchFileCodes">
    /// The same for patch entries, each member the patch code unpacked out of the
    /// entry's key name, or null where it will not unpack. A patch entry names no
    /// product; the code is what lets the scan ask whether a program it can question
    /// records holding the patch.
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
    /// <param name="PackageRecords">
    /// Every cached-package path the read found recorded, one member per value that
    /// named one, whether or not the API's own loop had already claimed it. What
    /// <see cref="WithholdOnRegistryPackageRecords"/> reads to let a registry record
    /// take a removable verdict away, which a fallback claim merged into the set cannot
    /// do. Null where the caller supplied no reader, which takes nothing away.
    /// </param>
    /// <param name="PatchListings">
    /// The patch codes each product key's own <c>Patches</c> key lists, by the account
    /// subtree the product key sits under and its code. Only a <c>Patches</c> key that is
    /// there and whose listing was established is in it: a product key with no
    /// <c>Patches</c> key is not, nor is a listing that threw or that holds a name that
    /// is no code, nor a product key whose own name is no code. Null where the caller
    /// supplied no reader, which establishes no listing anywhere.
    /// </param>
    /// <param name="ProductEntries">
    /// One member per product key under <c>UserData</c> whose name is a code, by the
    /// account subtree it sits under. Null where the caller supplied no reader.
    /// </param>
    internal readonly record struct FallbackRead(
        int Failures,
        int ProductKeys,
        IReadOnlyList<string?>? UnclaimedProductFileCodes = null,
        IReadOnlyList<string?>? UnclaimedPatchFileCodes = null,
        int NonStringLocalPackageValues = 0,
        IReadOnlyCollection<string>? RegistryProductCodes = null,
        int UnparseableProductKeyNames = 0,
        IReadOnlyDictionary<string, ProductPatchSet>? ProductPatchSets = null,
        int ProductPatchKeys = 0,
        int ProductPatchRegistrations = 0,
        int ProductsWithRemovablePatch = 0,
        int ProductsWithPatchSetUnestablished = 0,
        PathCensus? Paths = null,
        EstablishedPatchReach Reach = default,
        IReadOnlyList<RegistryPackageRecord>? PackageRecords = null,
        IReadOnlyDictionary<AccountCode, IReadOnlyCollection<string>>? PatchListings = null,
        IReadOnlyList<RegistryProductEntry>? ProductEntries = null)
    {
        /// <summary>
        /// Product entries naming a cached file the API's own loop never claimed and
        /// that is on the disk: the count of <see cref="UnclaimedProductFileCodes"/>, so
        /// the tally the report sends and the entries the scan decides on are one list.
        /// </summary>
        internal int UnclaimedProductFiles => UnclaimedProductFileCodes?.Count ?? 0;

        /// <summary>The same for patch entries, off <see cref="UnclaimedPatchFileCodes"/>.</summary>
        internal int UnclaimedPatchFiles => UnclaimedPatchFileCodes?.Count ?? 0;
    }

    /// <summary>
    /// One cached-package path a registration under <c>UserData</c> records, as the
    /// registry fallback read it.
    /// </summary>
    /// <param name="Path">The recorded value, normalised as every claim is.</param>
    /// <param name="IsPatch">
    /// True where the value is a patch's own package record, under
    /// <c>Patches\&lt;packed patch&gt;</c>; false where it is a product's, under
    /// <c>Products\&lt;packed product&gt;\InstallProperties</c>.
    /// </param>
    /// <param name="Code">
    /// The patch or product code unpacked out of the key name the value sits under, or
    /// null where that name is not a packed GUID.
    /// </param>
    /// <param name="Account">
    /// The name of the account subtree under <c>UserData</c> the value was read from, as
    /// the registry spells it. Null on a record built by anything but the fallback.
    /// </param>
    internal readonly record struct RegistryPackageRecord(
        string Path, bool IsPatch, string? Code, string? Account = null);

    /// <summary>
    /// One product key under <c>UserData</c>, as the registry fallback read it.
    /// </summary>
    /// <param name="Account">
    /// The name of the account subtree the key sits under, as the registry spells it.
    /// </param>
    /// <param name="Code">The product code unpacked out of the key's name.</param>
    /// <param name="PatchCodes">
    /// The patch codes the key's own <c>Patches</c> key lists, empty where it has no such
    /// key, and null where that listing was not established.
    /// </param>
    /// <param name="HasInstallProperties">
    /// Whether the key has an <c>InstallProperties</c> key. It stays true where that read
    /// fails, which is counted as a failed read and withholds on its own.
    /// </param>
    internal readonly record struct RegistryProductEntry(
        string Account, string Code, IReadOnlyCollection<string>? PatchCodes, bool HasInstallProperties = true);

    /// <summary>
    /// A product or patch code under one account subtree of <c>UserData</c>. Both halves
    /// compare without case, the registry and the Windows Installer API each handing back
    /// their own spelling.
    /// </summary>
    internal readonly record struct AccountCode(string Account, string Code)
    {
        public bool Equals(AccountCode other) =>
            string.Equals(Account, other.Account, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Code, other.Code, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(Account),
            StringComparer.OrdinalIgnoreCase.GetHashCode(Code));
    }

    /// <summary>
    /// An installation that answered, asked by a patch's code, that it holds the patch and
    /// has not shown it removable, with the state it holds the patch in (0 where that
    /// state did not parse as a number).
    /// </summary>
    private readonly record struct HeldByAsking(
        int State, string PatchCode, string ProductCode, string? Sid, MsiInstallContext Context);

    /// <summary>
    /// One patch on one installation: the patch, the product, and the account and context
    /// the installation is in. The codes and the account compare without case; the
    /// context compares exactly.
    /// </summary>
    private readonly record struct Pairing(
        string PatchCode, string ProductCode, string? Sid, MsiInstallContext Context)
    {
        public bool Equals(Pairing other) =>
            Context == other.Context
            && string.Equals(PatchCode, other.PatchCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(ProductCode, other.ProductCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Sid, other.Sid, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(PatchCode),
            StringComparer.OrdinalIgnoreCase.GetHashCode(ProductCode),
            Sid is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Sid),
            Context);
    }

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
    /// being put through when it was refused. The embedded-null test counts its own
    /// refusal. Every step after it sets the marker as it starts, inside one try block
    /// whose catch counts the refusal against the step the marker names. Whichever step
    /// refuses, the method hands back the value exactly as recorded.
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
    /// <c>EnumerationCensus.AnyRecordedPathUnestablished</c> answers true, this
    /// service's scan-wide withholding takes every superseded row off the offer on the
    /// same answer, and that property is where every population is added to the
    /// question.
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
        /// IT DECIDES NOTHING. Every other counter here records what happened to a
        /// value; this one records what the value LOOKED LIKE. It exists because the
        /// resolver is put every path, so <see cref="ResolverAttempts"/> cannot also say
        /// how many of them carried such a spelling: one counter answering both
        /// questions answers neither. How often these spellings occur on real machines
        /// is what this one is for.
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
        /// Every increment on a census happens on the thread that built it. The counters
        /// are plain int fields with no interlocking, and the normalisation refusals among
        /// them decide what the scan withholds, so each count has to be exact. The
        /// enumeration builds and fills its censuses inside the one synchronous core that
        /// <c>Task.Run</c> starts. Work split across threads needs a census for each part,
        /// folded with <see cref="Add"/> on one thread afterwards.
        ///
        /// In a Debug build, which is where the suite runs, this throws when a count is
        /// changed on any other thread, by an increment or by <see cref="Add"/>, so a
        /// change that breaks the rule fails a test. A Release build compiles the call
        /// out.
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

        public Models.PackageIdentity? Read(
            string filePath, bool isPatch, out string detail, out PackageReadRefusal refusal)
        {
            detail = string.Empty;
            refusal = PackageReadRefusal.WouldNotRead;
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

        // The same installations, each with which of its reads came back short, so the
        // registry's own records can be asked afterwards whether they hold what each lost
        // (RegistryHoldsWhatWasLost).
        var shortInstallations = new List<ShortInstallation>();

        // Patches whose State or Uninstallable read failed, or came back empty where
        // the pairing's verdict turns on it. Decides nothing; see
        // the increment site for what it measures and why it is worth measuring.
        var unreadablePatchStates = 0;

        // Products installed as a second instance of themselves, and the products
        // that would not answer the question. NEITHER MAY BE READ WITHOUT THE OTHER: a
        // machine reading no second instance while some reads failed has not been shown
        // to hold none. Neither count decides anything. What acts on each reading is the
        // mark the installation carries into the declared-product check
        // (ListedInstallation.SecondCopyNotRuledOut), and a read that failed marks it as
        // a positive reading does. See InstanceProductCount for what a positive reading
        // rests on.
        //
        // FED FROM TWO PLACES AND NOT ONE. The loop below asks every product the
        // enumeration returned; the pass after it asks every product the enumeration
        // lost that the registry named and Windows confirmed installed. A product the
        // registry names that nothing shows was asked is counted as unanswered or as
        // an unparseable key name, and EnumerationCensus.RegistryProductUnaskable
        // withholds the walk-derived offer on those two counts. A product in neither the
        // enumeration nor the registry's product keys is one nothing on this machine can
        // name, which is the limit of the whole scan and not of this rule.
        var instanceProducts = 0;
        var instanceTypeUnreadable = 0;

        // Every installation this enumeration established, for
        // InstallerQueryResult.Installations: each row the product walk listed, in walk
        // order, then each installation the recovery by name found, each marked by its
        // own InstanceType reading.
        var listed = new List<ListedInstallation>(products.Count);

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
        // of one product: an account the patch enumeration refuses as input refuses
        // every index for every product recorded under it. Each entry
        // carries a real message and stack trace, so a machine in that state
        // spends crash.log on near-identical copies of one already-recorded
        // condition, which is the very history a report of it would need.
        var abandonedLog = new PerItemFailureLog("Patch enumeration",
            "The product identity in the ones not logged is recorded nowhere else. Nothing the "
            + "user sees, and nothing the opt-in report carries, says which product's patch list "
            + "was abandoned.",
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
            + "nowhere else. None of those files is offered. Nothing the user sees says that a "
            + "file would not read, and nor does the opt-in report.",
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
            // the same count, and reaches it once. The count is of programs, not
            // failures, so one program with a failed package read AND two failed
            // patch rows is one program. Counting failures instead would inflate
            // the Application-log entry and the opt-in report's figure without
            // telling anyone more.
            var recordsShort = false;
            var packageLost = false;
            List<(string PatchCode, string? Sid, MsiInstallContext Context)>? lostPatchRecords = null;

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
            // IT DOES NOT FEED recordsShort AND MUST NOT START. What the other reads in
            // this loop record there is a CLAIM that never reached the merge, and this
            // property carries no claim on any file, so counting it there would treat a
            // fact about the machine as a lost claim. What it DOES feed is a separate rule, and
            // where the two counts are read together is EnumerationCensus.
            //
            // THE ROW IS LISTED HERE, WITH ITS OWN READING. What the declared-product
            // check does with an installation that is not shown to be ordinary turns on
            // this one answer, so the row carries the answer it was given rather than
            // having it looked up again later by code, account and context.
            var instanceReading = ReadInstanceType(_msi, productCode, userSid, context);
            switch (instanceReading)
            {
                case InstanceReading.SecondInstance: instanceProducts++; break;
                case InstanceReading.Unreadable: instanceTypeUnreadable++; break;
            }

            listed.Add(new ListedInstallation(productCode, userSid, (int)context,
                SecondCopyNotRuledOut: instanceReading != InstanceReading.Ordinary));

            // LocalPackage is the one property whose failed read DELETES this
            // product's claim rather than degrading it. An unreadable State or
            // Uninstallable still merges a row, kept and marked unread;
            // an unreadable LocalPackage skips the insertion entirely, and the
            // product's "I still have this file" never reaches the merge at all.
            // So it is counted in unreadableProducts, and the registry's own
            // package record for this installation's account is asked for after
            // the loop, the removable class being withheld where there is none
            // (RegistryHoldsWhatWasLost). Without the count the scan would report
            // itself complete while short of a claim.
            if (localPackage.Unreadable)
            {
                recordsShort = true;
                packageLost = true;
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
                    (lostPatchRecords ??= []).Add((patchCode, patchUserSid, patchContext));
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
                    // registration, which no surface may describe as a claim, and nor
                    // does an empty answer where the verdict turns on it
                    // (LeavesVerdictUnestablished). The count travels beside the flag
                    // because how often a machine cannot answer either question is a
                    // fact only the reports can establish. Neither can pass the
                    // removable verdict below, both halves of that rule needing a
                    // positive answer.
                    var verdictUnreadable = stateRead.Unreadable || uninstallableRead.Unreadable
                        || LeavesVerdictUnestablished(stateRead.Value, uninstallableRead.Value);
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

            if (recordsShort)
            {
                unreadableProducts++;
                shortInstallations.Add(new ShortInstallation(productCode, userSid, context,
                    packageLost, patchesIncomplete, patches.ConvertAll(p => p.PatchCode),
                    lostPatchRecords ?? []));
            }
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
        // ASKED RATHER THAN ASSUMED UNANSWERABLE. A recovered product marked unasked
        // would have every installation package compared with the packages it opens, and
        // kept wherever those cannot all be seen, on exactly the machines the recovery
        // pass exists to rescue. Recovery closes a gap by asking, and this is one more
        // question to the products it recovered.
        //
        // IT COSTS ONE KEYED PROPERTY READ PER RECOVERED PRODUCT, on a set that is
        // empty on a machine whose enumeration came back whole, and it fails in the
        // safe direction by construction: a read that will not answer marks the
        // installation as a positive reading does.
        //
        // The account and context are the ones the recovery established, because a
        // per-user product answers in its own account and nowhere else.
        foreach (var (recoveredCode, recoveredSid, recoveredContext) in missed.Recovered)
        {
            ct.ThrowIfCancellationRequested();
            var instanceReading = ReadInstanceType(_msi, recoveredCode, recoveredSid, recoveredContext);
            switch (instanceReading)
            {
                case InstanceReading.SecondInstance: instanceProducts++; break;
                case InstanceReading.Unreadable: instanceTypeUnreadable++; break;
            }

            listed.Add(new ListedInstallation(recoveredCode, recoveredSid, (int)recoveredContext,
                SecondCopyNotRuledOut: instanceReading != InstanceReading.Ordinary));
        }

        WithholdOnRegistryPackageRecords(claimed, patchClaims, fallback.PackageRecords);

        var heldByName = new List<PatchClaim>();
        ConfirmRemovableAgainstEveryProduct(claimed, patchClaims, products, missed.Recovered,
            fallback.Reach, fallback.ProductPatchSets, apiPatchSets, ct, unreadPatchFileLog,
            heldByName);

        // After the confirmation pass, so a row an installation it asked holds stays that
        // installation's claim, and a key's listing takes the verdict only from rows still removable.
        var unlistedEntries = UnlistedEntries(fallback.ProductEntries, products.Concat(missed.Recovered));
        WithholdOnUnlistedEntries(
            claimed, patchClaims, fallback.PackageRecords, unlistedEntries, fallback.Failures > 0);

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
        // LocateProductsTheEnumerationMissed puts every product code the registry
        // holds to Windows as a question about that one product, and compares the
        // installations the answer lists with the ones the enumeration listed. So a
        // truncation is not estimated from how far two totals disagree; the
        // installations behind the disagreement are named, and each is either
        // recovered into the questions the confirmation pass asks, or listed under no
        // account, or left open because Windows would not say, which is counted in
        // missed.Unresolved for a code the enumeration never returned and recorded in
        // missed.UnsettledEnumerated for one it did. Only the last of these withholds,
        // on the terms below.
        //
        // WHY A LEFTOVER KEY PROVES NOTHING. A UserData product key outlives a
        // failed or partial uninstall, so the registry legitimately holds more keys
        // than the machine has products, and against a TOTAL that residue cannot be
        // told from a truncation: both read as the registry running ahead. Asked by
        // name, the same key lists no installation, which settles that it is no
        // truncation, and nothing is recovered from it. A key the walks leave out need
        // not be residue (UnlistedEntries), so what its own listing names is held back
        // (WithholdOnUnlistedEntries).
        //
        // AND WHERE THE REGISTRY READ ITSELF FAILS. ProductKeys is counted from the
        // subkeys the fallback actually walked, so a fallback that failed reports
        // FEWER keys, and the names asked about are the codes that side handed over.
        // The failed read is counted in fallback.Failures, which withholds every removable
        // row and, through the census, the walk-derived offer, and which with an unreadable
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
        // A CACHED FILE THE REGISTRY NAMES AND THE API LOOP NEVER CLAIMED DECIDES NOTHING
        // ON ITS OWN, because every product entry behind one is accounted for by name.
        // The fallback reads the same UserData keys the API read and runs after the whole
        // API loop, so a path it is the first to claim is one no installation the loop
        // reached ever named. The recovery above then asks about every code the registry
        // names: the code is one the enumeration returned and asked again, one the
        // recovery found installed and that is asked like any other, one no walk lists an
        // installation of, whose key keeps what its own listing names
        // (WithholdOnUnlistedEntries), or one Windows would not answer about, and a key
        // whose name is no code is counted where it is read. Those last two withhold below
        // on their own.
        //
        // THE ONE ENTRY THAT STILL WITHHOLDS is under a code the enumeration returned whose
        // own keyed answer did not settle, because an installation of it that nothing lists
        // may be what the entry records, and no question in this scan is put to that
        // installation.
        //
        // A PATCH ENTRY NAMES NO PRODUCT, so it is attributed through the registry's own
        // listing of which patches each product holds. Where the listing of a product this
        // scan asks by name names the patch, that product is put the question about every
        // superseded patch in the confirmation pass, and the entry adds nothing. Anywhere
        // else no product this scan asks is recorded as holding the patch, and every
        // superseded patch is withheld. A listing that could not be read names nothing. So
        // does the listing of a code whose own keyed answer did not settle: the listing is
        // merged across accounts, so what it names may be the unasked installation's. A
        // code no installation is listed under is not added: its program is asked nothing.
        var namedPatches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void AddListing(string productCode)
        {
            if (fallback.Reach.PatchCodesByProduct is { } byProduct
                && byProduct.TryGetValue(productCode, out var held) && held is not null)
                foreach (var patch in held) namedPatches.Add(patch);
        }
        foreach (var (code, _, _) in products)
            if (!missed.UnsettledEnumerated.Contains(code)) AddListing(code);
        foreach (var (code, _, _) in missed.Recovered) AddListing(code);

        var unattributedPatchFiles = 0;
        foreach (var patchCode in fallback.UnclaimedPatchFileCodes ?? [])
            if (patchCode is null || !namedPatches.Contains(patchCode)) unattributedPatchFiles++;

        // THE PRODUCTS THIS SCAN COULD NOT SETTLE, and three kinds that cannot overlap:
        // codes the enumeration returned, codes it did not, and keys with no code at all.
        //
        // The first is taken per code, so a product meeting two of its terms counts once.
        // It holds each code with an installation whose records came back short where the
        // registry does not hold, under that installation's own account, what the failed
        // read would have returned (RegistryHoldsWhatWasLost); each code whose own keyed
        // answer did not settle and whose entry names an unclaimed file; and any product
        // row the walk passed without reading, which no walk that returns has.
        //
        // AN INSTALLATION WHOSE LOST RECORDS THE REGISTRY DOES HOLD IS NOT IN IT. The
        // fallback read those records and claimed each path, and any of them can take a
        // superseded patch's verdict away (WithholdOnRegistryPackageRecords). What
        // such an installation holds or could uninstall is asked by name, one superseded
        // patch at a time, in the judging pass and the per-pairing pass.
        var unsettledEnumerated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var installation in shortInstallations)
            if (!RegistryHoldsWhatWasLost(installation, fallback))
                unsettledEnumerated.Add(installation.ProductCode);
        foreach (var code in fallback.UnclaimedProductFileCodes ?? [])
            if (code is not null && missed.UnsettledEnumerated.Contains(code))
                unsettledEnumerated.Add(code);
        var unsettledEnumeratedProducts = unsettledEnumerated.Count + unreadableRows;

        // The other two: a code Windows would not answer about, and a key whose name
        // yielded no code to ask with. Nothing shows either was asked its InstanceType,
        // so both also reach EnumerationCensus.RegistryProductUnaskable, which reads them
        // apart through the census below.
        var unresolvedProducts = missed.Unresolved + fallback.UnparseableProductKeyNames;

        var withheldProducts = unsettledEnumeratedProducts + unresolvedProducts;

        progress?.Report(new ScanProgressUpdate(Strings.Status_RegisteredPackagesFound));

        var packages = claimed.Values.ToList();

        // The run's whole path census: this loop's, plus the fallback's own. Taken
        // before the withholding below, which asks it whether any recorded path went
        // unsettled; nothing that withholding changes is counted in it.
        var paths = new PathCensus();
        paths.Add(pathCensus);
        paths.Add(fallback.Paths);

        var census = new EnumerationCensus(
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
            // Installations of codes the enumeration never returned, which is what this
            // count has always meant. The recovery's other installations, of codes it
            // did return, are counted apart at the end.
            missed.Recovered.Count - missed.RecoveredOfEnumerated,
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
            fallback.Failures,
            unsettledEnumeratedProducts,
            missed.RecoveredOfEnumerated,
            unattributedPatchFiles);

        // LIVE, AND ON NO ACCOUNT TO BE DELETED AS DEAD MACHINERY. A superseded row
        // on a machine whose patch sets read clean arrives here still carrying
        // IsRemovable, and this loop is what takes it off the offer when the scan
        // could not settle a product, met a cached patch file it could not attribute to
        // a product it asks, or met a recorded path it could not settle.
        //
        // One product whose lost claim is on record nowhere is enough to fire it:
        // that product is counted in withheldProducts, and the loop takes every
        // superseded row off the offer. So is one recorded path the scan could not
        // settle.
        //
        // NOT TO BE CONFUSED WITH THE REFUSAL GATE ABOVE, which weighs a wider count
        // of the same products and is very much alive; see its own note for why.
        //
        // What it does: a scan with a claim it cannot place withholds the whole
        // removable class. "Removable" asserts that NO installed product still needs
        // the file, and a claim that could be on any cached file cannot support that
        // assertion for any patch on the machine, a patch being cached once and shared
        // across the products that hold it.
        //
        // A PRODUCT WHOSE RECORDS CAME BACK SHORT FIRES IT ONLY WHERE THE REGISTRY DOES
        // NOT HOLD WHAT ITS FAILED READS WOULD HAVE RETURNED, under that installation's
        // own account (RegistryHoldsWhatWasLost). A failed patch row names its product
        // and not its patch (the API documents its output buffers for ERROR_SUCCESS and
        // ERROR_MORE_DATA only, and the loop clears the buffer per iteration), and a
        // failed LocalPackage read names its product but not the path it would have
        // claimed. So neither says which file the lost claim was on, and where the
        // registry does not hold the record either, nothing does. Where it does, the
        // fallback has claimed that record's path, WithholdOnRegistryPackageRecords reads
        // it against every superseded patch, and the product itself is asked by each
        // superseded patch's code: the per-pairing pass asks whether it holds the patch
        // still needed, and the judging pass puts it into the patch's product set
        // wherever it answers that it holds the patch at all.
        //
        // A RECORDED PATH THE SCAN COULD NOT SETTLE WITHHOLDS THE CLASS AS WELL, for
        // the reason it withholds the walk-derived offer: nothing says which file the
        // claim it came from names. Claims meet on a row by their normalised path, so
        // a claim kept in a spelling nothing resolves need not land on the row for the
        // file it means, and where it does not, the claim never reaches that row. That
        // claim can be a second registration of a superseded patch, holding it applied
        // under another product, or any other registration aimed at the patch's file.
        // The per-pairing pass asks every installation it knows of about the patch
        // itself, so a second registration of that patch, held by an installation the
        // scan listed, is answered there wherever its claim landed. A registration of
        // anything else aimed at the file, another patch's or a product's own package,
        // reaches the file only by landing on its row: the per-pairing pass asks about
        // this patch and the per-product condition about patch sets. The claim names a
        // file the scan cannot place, so it can be any of them, and scan-wide is again
        // the finest granularity there is.
        //
        // IT IS ASKED THROUGH EnumerationCensus.AnyRecordedPathUnestablished, the
        // property the walk-derived withholding asks, so a population added to the
        // census reaches both halves of the offer. A row it withholds carries
        // WithheldOnRecordedPathUnestablished, whether or not the unaccounted-products
        // condition held too, and the opt-in report counts those rows.
        //
        // This loop moves only the removable class, the superseded patches, and only
        // on a scan that could not settle a product, found a cached patch file no
        // product it asks is recorded as holding, or could not settle a recorded path.
        // The walk half is decided elsewhere, on conditions of its own, the last of
        // these among them.
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
        // AND THE CONDITIONS THIS LOOP FIRES ON DO NOT NAME THAT ROW'S RISK. They are a
        // product the enumeration DID return whose lost records the registry does not
        // hold, a product it returned whose installations the keyed ask did not settle
        // and whose registry entry names a cached file the enumeration never claimed that
        // is on the disk, a product the registry names that this scan could not settle,
        // and a cached patch file no product the scan asks is recorded as holding. None
        // of them is "a holder of this patch went unseen", which is the condition that
        // would bear on this file. They are signs of a degraded machine, not a per-file
        // verdict.
        //
        // A RECORDED PATH THE SCAN COULD NOT SETTLE CAN BE THAT HOLDER, and the split
        // still needs nothing from this loop, because the holder's registration reaches
        // the split through the row it lands on. Where the superseded file has gone, the
        // registration kept in the unsettled spelling names that same absent file, or
        // names nothing, so its row reads missing as well, and the split reports a
        // missing row that is not a superseded or obsoleted patch whatever its verdict.
        // The warning names that holder's program through that row.
        //
        // THE WITHHOLDING ITSELF IS WHAT ANSWERS FOR SUCH A MACHINE: a run that could
        // not settle a product, attribute a cached patch file or settle a recorded path
        // offers no superseded patch at all. A file already gone is not kept by printing a
        // sentence about it.
        //
        // A WITHHELD ROW WITH NO MARKER IS REPORTED BY THE SPLIT WHERE ITS FILE HAS
        // GONE. Every row this loop withholds is one. So is every removable path on a
        // run whose machine-wide patch enumeration did not answer, which downgrades
        // them with no marker set (see ConfirmRemovableAgainstEveryProduct), that run
        // having failed to establish something about the patch itself. A superseded
        // file that read cleanly when the pass above opened it, and had gone by the
        // time the scan looked for it on the disk, is one way such a row reaches the
        // split from this loop.
        var pathUnestablished = census.AnyRecordedPathUnestablished;
        if (withheldProducts > 0 || unattributedPatchFiles > 0 || pathUnestablished)
            for (var i = 0; i < packages.Count; i++)
                if (packages[i].IsRemovable)
                    packages[i] = packages[i] with
                    {
                        IsRemovable = false,
                        RemovableWithheld = true,
                        WithheldOnRecordedPathUnestablished = pathUnestablished,
                        WithheldScanWide = true,
                    };

        var pairingsHeldByName = PairingsStillOnOffer(heldByName, packages, patchClaims);

        return new InstallerQueryResult(packages.AsReadOnly(), withheldProducts, patchClaims.AsReadOnly(),
            census, listed,
            pairingsHeldByName,
            PairingsOfHoldersWithNoClaims(pairingsHeldByName, patchClaims, ct, abandonedLog),
            UnlistedProductKeysOf(unlistedEntries, fallback.PackageRecords));
        }
        finally
        {
            abandonedLog.WriteClosingEntry();
            unreadPatchFileLog.WriteClosingEntry();
        }
    }

    /// <summary>
    /// The pairings the per-pairing pass read as holding a patch it could offer, kept
    /// where the path is still removable once every pass has run and the pairing is not
    /// already one of the enumeration's own claims. On a machine whose enumeration
    /// reached every holder of every patch this is empty: each such pairing is a claim.
    /// </summary>
    private static IReadOnlyList<PatchClaim> PairingsStillOnOffer(
        List<PatchClaim> heldByName, List<RegisteredPackage> packages, List<PatchClaim> patchClaims)
    {
        var removable = new HashSet<string>(
            packages.Where(p => p.IsRemovable).Select(p => p.LocalPackagePath), StringComparer.OrdinalIgnoreCase);
        var claims = new HashSet<(string Path, Pairing Pairing)>(
            patchClaims.Select(c => (c.LocalPackagePath.ToUpperInvariant(), PairingOf(c))));

        var kept = new List<PatchClaim>();
        var seen = new HashSet<(string Path, Pairing Pairing)>();
        foreach (var held in heldByName)
        {
            if (!removable.Contains(held.LocalPackagePath)) continue;
            var key = (held.LocalPackagePath.ToUpperInvariant(), PairingOf(held));
            if (claims.Contains(key) || !seen.Add(key)) continue;
            kept.Add(held);
        }

        return kept.AsReadOnly();
    }

    private static Pairing PairingOf(PatchClaim claim) =>
        new(claim.PatchCode, claim.ProductCode, claim.UserSid, (MsiInstallContext)claim.Context);

    /// <summary>
    /// Every patch Windows lists for each installation in <paramref name="heldByName"/>
    /// that holds none of the enumeration's own claims, one pairing per patch, for the
    /// check made under the lease just before a Move or Delete to read as that
    /// installation's other patches. The enumeration lists another installation's
    /// patches as it lists them, as claims.
    ///
    /// THEY CARRY NO PATH. The check reads only whether each can be uninstalled, and
    /// nothing reads a path off one.
    ///
    /// A LIST THAT DID NOT RUN TO ITS END CARRIES WHAT IT LISTED, and one Windows refused
    /// outright, or that never ended, carries nothing and does not refuse the scan. The
    /// offer rests on the judging pass, which reads each such installation's patch set as
    /// the registry lists it in full; these pairings are what the check under the lease
    /// re-reads in the window after it.
    /// </summary>
    private IReadOnlyList<PatchClaim> PairingsOfHoldersWithNoClaims(
        IReadOnlyList<PatchClaim> heldByName,
        List<PatchClaim> patchClaims,
        CancellationToken ct,
        PerItemFailureLog failureLog)
    {
        var withClaims = new HashSet<Pairing>(patchClaims.Select(c => PairingOf(c) with { PatchCode = string.Empty }));
        var asked = new HashSet<Pairing>();
        var pairings = new List<PatchClaim>();

        foreach (var held in heldByName)
        {
            var installation = PairingOf(held) with { PatchCode = string.Empty };
            if (withClaims.Contains(installation) || !asked.Add(installation)) continue;

            List<(string PatchCode, string? UserSid, MsiInstallContext Context)> patches;
            try
            {
                (patches, _) = EnumeratePatches(held.ProductCode, held.UserSid,
                    (MsiInstallContext)held.Context, ct, failureLog);
            }
            catch (Exception ex) when (ex is LocalisedAccessException or LocalisedInvalidOperationException)
            {
                continue;
            }

            foreach (var (patchCode, patchSid, patchContext) in patches)
                pairings.Add(new PatchClaim(string.Empty, patchCode, held.ProductCode, patchSid, (int)patchContext));
        }

        return pairings.AsReadOnly();
    }

    /// <summary>
    /// One installation the product walk listed whose records came back short, and which
    /// of its reads did.
    /// </summary>
    /// <param name="PackageLost">Its own <c>LocalPackage</c> read failed.</param>
    /// <param name="PatchListShort">Its patch enumeration did not run to a clean end.</param>
    /// <param name="PatchCodesListed">
    /// The code of every patch its patch enumeration did return, whether or not the list
    /// ran to its end.
    /// </param>
    /// <param name="LostPatchRecords">
    /// Each patch whose <c>LocalPackage</c> read failed, with the account and context the
    /// patch enumeration listed it in.
    /// </param>
    private sealed record ShortInstallation(
        string ProductCode,
        string? Sid,
        MsiInstallContext Context,
        bool PackageLost,
        bool PatchListShort,
        IReadOnlyList<string> PatchCodesListed,
        IReadOnlyList<(string PatchCode, string? Sid, MsiInstallContext Context)> LostPatchRecords);

    /// <summary>
    /// Whether the registry holds, under the installation's own account subtree, what
    /// each of its failed reads would have returned: a package record for the product
    /// where its <c>LocalPackage</c> read failed; where its patch enumeration came back
    /// short, a <c>Patches</c> key whose listing was established and names every patch
    /// the enumeration did return; and a package record for each patch whose
    /// <c>LocalPackage</c> read failed.
    ///
    /// WHERE IT DOES, WHAT THE INSTALLATION LOST IS ON RECORD. Windows Installer finds a
    /// cached package through the path recorded for it, and the fallback reads and claims
    /// every such record under every account, so a lost claim whose record is there has
    /// reached the claimed set, and <see cref="WithholdOnRegistryPackageRecords"/> reads
    /// it against every superseded patch. A patch the listing names with no record of its
    /// own has no cached path for Windows either. And the registry's listing is what the
    /// per-product condition reads this product's patch set from where the enumeration's
    /// own reading is short.
    ///
    /// A LISTING THAT LEAVES OUT A PATCH THE ENUMERATION RETURNED IS NOT THE WHOLE
    /// REGISTRATION, and nor is a product key with no <c>Patches</c> key, which says the
    /// installation holds no patch where Windows has just returned one. Either answers no.
    ///
    /// WHERE IT DOES NOT, THE LOST CLAIM COULD NAME ANY FILE, and nothing in this scan
    /// says which. A record or a listing under ANOTHER account does not answer for this
    /// one: it may be another installation's. An account <see cref="UserDataAccount"/>
    /// cannot name, and a result with no registry reader, answer no.
    ///
    /// A value that is there and is not a string is a failed fallback read, and with a
    /// short installation that refuses the scan before this is asked, so a record absent
    /// here is absent from the registry.
    /// </summary>
    private static bool RegistryHoldsWhatWasLost(ShortInstallation installation, FallbackRead fallback)
    {
        var account = UserDataAccount(installation.Sid, installation.Context);
        if (account is null) return false;

        if (installation.PackageLost
            && !HoldsPackageRecord(fallback.PackageRecords, account, isPatch: false, installation.ProductCode))
            return false;

        if (installation.PatchListShort)
        {
            if (fallback.PatchListings is null
                || !fallback.PatchListings.TryGetValue(new AccountCode(account, installation.ProductCode),
                    out var listed))
                return false;

            foreach (var patchCode in installation.PatchCodesListed)
                if (!listed.Contains(patchCode, StringComparer.OrdinalIgnoreCase))
                    return false;
        }

        foreach (var (patchCode, patchSid, patchContext) in installation.LostPatchRecords)
        {
            var patchAccount = UserDataAccount(patchSid, patchContext);
            if (patchAccount is null
                || !HoldsPackageRecord(fallback.PackageRecords, patchAccount, isPatch: true, patchCode))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Whether <paramref name="records"/> holds a package record for
    /// <paramref name="code"/> under <paramref name="account"/>, of the kind
    /// <paramref name="isPatch"/> names. Every record the fallback keeps names a path, so
    /// one that is there and empty is not among them.
    /// </summary>
    private static bool HoldsPackageRecord(
        IReadOnlyList<RegistryPackageRecord>? records, string account, bool isPatch, string code)
    {
        if (records is null) return false;

        foreach (var record in records)
            if (record.IsPatch == isPatch
                && string.Equals(record.Code, code, StringComparison.OrdinalIgnoreCase)
                && string.Equals(record.Account, account, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    /// <summary>
    /// Takes the removable verdict off a row that a registry package record names under
    /// anything other than the patch the row is. A product's own package record naming
    /// the path does it, and so does a patch's package record whose code is none of the
    /// codes the enumeration's claims name for the path, or whose key name yields no
    /// code. The row is downgraded as a claim, with withheld false, which is what
    /// <see cref="MergeClaim"/> does with the same record read through the API.
    ///
    /// IT IS WHAT LETS A REGISTRY RECORD KEEP A FILE. A fallback claim merged into
    /// <paramref name="claimed"/> only adds a path and never displaces the row already
    /// on it (see <see cref="MergeClaim"/>), so without this a record the API failed to
    /// read, or never reached, would leave the row as the API loop left it. Windows
    /// Installer finds a product's cached package and a patch's cached package through
    /// the path recorded for each, and the fallback reads every such record under every
    /// account, so a record naming the file is a claim on it whichever source read it.
    ///
    /// A patch's own records naming its own file, under one account or several, change
    /// nothing, and on a machine whose registrations are sound neither shape occurs.
    /// Every patch claim on the path counts, not only the removable ones, so a second
    /// registration of the same patch whose claim read cleanly still names it.
    ///
    /// AND SUCH A RECORD MARKS A SUPERSEDED OR OBSOLETED ROW FOR THE MISSING-FILES WARNING,
    /// removable or not (<see cref="Hold"/>). The record brings no patch state, so the row
    /// still reads superseded, and without the mark a file a product's package record names
    /// would read as a harmless absence once it had gone.
    /// </summary>
    private static void WithholdOnRegistryPackageRecords(
        Dictionary<string, RegisteredPackage> claimed,
        List<PatchClaim> patchClaims,
        IReadOnlyList<RegistryPackageRecord>? records)
    {
        if (records is null || records.Count == 0) return;

        Dictionary<string, HashSet<string>>? codesByPath = null;
        foreach (var record in records)
        {
            if (!claimed.TryGetValue(record.Path, out var row)
                || !(row.IsRemovable || row.IsSupersededOrObsoleted)) continue;

            if (record.IsPatch && record.Code is not null)
            {
                if (codesByPath is null)
                {
                    codesByPath = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var claim in patchClaims)
                    {
                        if (!codesByPath.TryGetValue(claim.LocalPackagePath, out var codes))
                            codesByPath[claim.LocalPackagePath] = codes =
                                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        codes.Add(claim.PatchCode);
                    }
                }

                if (codesByPath.TryGetValue(record.Path, out var named) && named.Contains(record.Code))
                    continue;
            }

            Downgrade(claimed, record.Path, withheld: false);
            claimed[record.Path] = Hold(claimed[record.Path], claimState: 0);
        }
    }

    /// <summary>
    /// The product keys under <c>UserData</c> that no installation in
    /// <paramref name="installations"/> accounts for: each key whose account subtree and code
    /// no listed installation maps to (<see cref="UserDataAccount"/>).
    ///
    /// THE WALKS DO NOT LIST EVERY PROGRAM THE REGISTRY HOLDS. A program installed per user by
    /// SYSTEM has its key under <c>S-1-5-18</c>, with a cached package and a <c>Patches</c>
    /// listing, and neither the product walk nor the keyed walk lists it, whichever elevated
    /// account asks, SYSTEM included, and a keyed read in the account <c>S-1-5-18</c> answers
    /// ERROR_INVALID_PARAMETER. Microsoft documents a second such gap: "The enumeration skips
    /// products that are advertised only (such as products not installed) in the
    /// per-user-unmanaged context when enumerating across all users or a user other than the
    /// current user." A key an uninstall left behind is in this list too, and nothing in the
    /// walks tells the two apart.
    ///
    /// BY ACCOUNT AND CODE, NOT BY CODE. The walks can list a code under one account while a
    /// key under another holds a program of its own, and a comparison of codes would read
    /// that key as accounted for.
    /// </summary>
    internal static IReadOnlyList<RegistryProductEntry> UnlistedEntries(
        IReadOnlyList<RegistryProductEntry>? entries,
        IEnumerable<(string ProductCode, string? Sid, MsiInstallContext Context)> installations)
    {
        if (entries is null || entries.Count == 0) return [];

        var listed = new HashSet<AccountCode>();
        foreach (var (code, sid, context) in installations)
            if (UserDataAccount(sid, context) is { } account)
                listed.Add(new AccountCode(account, code));

        var unlisted = new List<RegistryProductEntry>();
        foreach (var entry in entries)
            if (!listed.Contains(new AccountCode(entry.Account, entry.Code)))
                unlisted.Add(entry);

        return unlisted;
    }

    /// <summary>
    /// <paramref name="unlisted"/> as the declared-product screen is handed it: each key with
    /// the cached-package paths <paramref name="records"/> holds for its own account and code.
    /// Two accounts can hold one code, so a record of another account's key is not its.
    /// </summary>
    internal static IReadOnlyList<UnlistedProductKey> UnlistedProductKeysOf(
        IReadOnlyList<RegistryProductEntry> unlisted,
        IReadOnlyList<RegistryPackageRecord>? records)
    {
        if (unlisted.Count == 0) return [];

        var paths = new Dictionary<AccountCode, List<string>>();
        foreach (var record in records ?? [])
        {
            if (record.IsPatch || record.Code is null || record.Account is null) continue;

            var key = new AccountCode(record.Account, record.Code);
            if (!paths.TryGetValue(key, out var held)) paths[key] = held = [];
            held.Add(record.Path);
        }

        return unlisted
            .Select(entry => new UnlistedProductKey(
                entry.Account, entry.Code, entry.PatchCodes,
                paths.TryGetValue(new AccountCode(entry.Account, entry.Code), out var held) ? held : [],
                entry.HasInstallProperties))
            .ToList();
    }

    /// <summary>
    /// Takes the removable verdict off every row of a patch an unlisted entry's own listing
    /// names, whichever path a claim or a package record gives for the patch, and marks every
    /// superseded or obsoleted row among them for the missing-files warning
    /// (<see cref="Hold"/>).
    ///
    /// THE REGISTRY KEEPS THE FILE AND NOTHING HERE RELEASES ONE. No walk lists such a program,
    /// so no pass in this scan asks it about the patch, and a row it holds is withheld, the
    /// state "this scan could not prove it". The <c>State</c> its listing records is not read:
    /// a registry value can hold a file back and cannot show that a file may go. This pass does
    /// not separate a program from a key an uninstall left behind, so a leftover key keeps what
    /// its listing names.
    ///
    /// The patch's rows are found through a claim and through a package record alike, so a row
    /// either one names for the patch is held, and the listing holds with no record of the
    /// patch's own. Leave either out and a row only the other names stays removable.
    ///
    /// A LISTING NOT ESTABLISHED MAY NAME ANY PATCH, and with
    /// <paramref name="registryReadFailed"/> a key that would not read may be an unlisted
    /// program's, whose record nothing else holds. So either withholds every removable row,
    /// scan wide, and marks every superseded or obsoleted row.
    ///
    /// A ROW WHOSE VERDICT A LISTING TOOK IS MARKED
    /// (<see cref="RegisteredPackage.WithheldOnUnlistedKey"/>), and the report counts those rows.
    /// A failed read alone names no key and marks none.
    /// </summary>
    private static void WithholdOnUnlistedEntries(
        Dictionary<string, RegisteredPackage> claimed,
        List<PatchClaim> patchClaims,
        IReadOnlyList<RegistryPackageRecord>? records,
        IReadOnlyList<RegistryProductEntry> unlisted,
        bool registryReadFailed)
    {
        var listingNotEstablished = false;
        var named = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in unlisted)
        {
            if (entry.PatchCodes is null) listingNotEstablished = true;
            else named.UnionWith(entry.PatchCodes);
        }

        var namedPaths = new HashSet<string>(claimed.Comparer);
        foreach (var claim in patchClaims)
            if (named.Contains(claim.PatchCode)) namedPaths.Add(claim.LocalPackagePath);

        foreach (var record in records ?? [])
            if (record.IsPatch && record.Code is not null && named.Contains(record.Code))
                namedPaths.Add(record.Path);

        if (listingNotEstablished || registryReadFailed)
        {
            foreach (var path in claimed.Keys.ToList())
            {
                var removable = claimed[path].IsRemovable;
                Downgrade(claimed, path, withheld: true, scanWide: true);
                claimed[path] = Hold(claimed[path], claimState: 0);
                if (removable && (listingNotEstablished || namedPaths.Contains(path)))
                    claimed[path] = claimed[path] with { WithheldOnUnlistedKey = true };
            }

            return;
        }

        foreach (var path in namedPaths)
        {
            if (!claimed.TryGetValue(path, out var row)) continue;
            Downgrade(claimed, path, withheld: true);
            claimed[path] = Hold(claimed[path], claimState: 0);
            if (row.IsRemovable) claimed[path] = claimed[path] with { WithheldOnUnlistedKey = true };
        }
    }

    /// <summary>
    /// Which installations of the products the registry names the product enumeration
    /// did not list, asked as a question about named products rather than inferred from
    /// two headcounts.
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
    /// NOT LISTED. The answer lists no installation. That is a UserData key outliving its
    /// product, the ordinary residue of a failed or partial uninstall, or a program the
    /// walks leave out (<see cref="UnlistedEntries"/>), and nothing here tells the two
    /// apart. Nothing is recovered, and what the key's own listing names is held back
    /// (<see cref="WithholdOnUnlistedEntries"/>). This is where comparing names is worth
    /// the most: a count cannot tell this state from the one above.
    ///
    /// UNASKABLE. The registry names a product and Windows would not say whether it
    /// is installed. Nothing about the enumeration's completeness can be
    /// established, so the caller withholds; see <paramref name="registryCodes"/>
    /// for the one other way this method reports the same not-knowing.
    ///
    /// A CODE THE ENUMERATION DID RETURN IS ASKED TOO, because the registry keeps a
    /// product's entry under each account it is installed in, and the enumeration
    /// can list one installation of a code and not another. Every installation the
    /// answer lists that the enumeration did not is recovered on the same terms as
    /// above. An answer that will not come, or that leaves out an installation the
    /// enumeration listed, is recorded against the code in
    /// <see cref="MissedProducts.UnsettledEnumerated"/> and not counted as unasked:
    /// the enumeration did list the product, so the caller keeps the product's own
    /// installation in every question it asks, and withholds on what is still
    /// unaccounted for under that code.
    /// </summary>
    /// <param name="registryCodes">
    /// Null where no fallback ran, which is not the same as an empty set and must
    /// not read as one: an empty set says the registry holds no product this
    /// enumeration missed, and null says nobody looked. Null yields no recovered
    /// products and no unresolved ones, leaving the caller's other signals to
    /// speak, because a comparison that did not happen may not withhold on its own
    /// silence.
    /// </param>
    private MissedProducts LocateProductsTheEnumerationMissed(
        List<(string ProductCode, string? UserSid, MsiInstallContext Context)> products,
        IReadOnlyCollection<string>? registryCodes,
        CancellationToken ct)
    {
        var recovered = new List<(string, string?, MsiInstallContext)>();
        var unsettled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (registryCodes is null || registryCodes.Count == 0) return new(recovered, 0, 0, unsettled);

        var enumerated = InstallationsByCode(products);

        // Unbounded: one keyed read per code the registry names, a set already
        // bounded by the machine's own registry keys, which the fallback has just
        // opened one at a time anyway. A cap would fall on the machines with the
        // most to recover.
        var unresolved = 0;
        var recoveredOfEnumerated = 0;
        foreach (var code in registryCodes)
        {
            ct.ThrowIfCancellationRequested();

            var resolved = ResolveProductInstances(_msi, code);
            if (!enumerated.TryGetValue(code, out var listed))
            {
                if (resolved.Unaskable) { unresolved++; continue; }
                foreach (var (sid, context) in resolved.Instances)
                    recovered.Add((code, sid, context));
                continue;
            }

            if (resolved.Unaskable || !HoldsEveryListedInstallation(enumerated, code, resolved.Instances))
            {
                unsettled.Add(code);
                continue;
            }

            foreach (var (sid, context) in resolved.Instances)
            {
                if (listed.Exists(l => l.Context == context
                        && string.Equals(l.Sid, sid, StringComparison.OrdinalIgnoreCase)))
                    continue;
                recovered.Add((code, sid, context));
                recoveredOfEnumerated++;
            }
        }

        return new(recovered, recoveredOfEnumerated, unresolved, unsettled);
    }

    /// <summary>
    /// What <see cref="LocateProductsTheEnumerationMissed"/> established.
    /// </summary>
    /// <param name="Recovered">
    /// Every installation the keyed ask found that the enumeration did not list, each
    /// with the account and context to ask it in, whether or not the enumeration
    /// returned its code elsewhere. The confirmation pass asks each one exactly as it
    /// asks an enumerated installation.
    /// </param>
    /// <param name="RecoveredOfEnumerated">
    /// How many of <paramref name="Recovered"/> are installations of a code the
    /// enumeration did return, under an account or context it did not list them in.
    /// </param>
    /// <param name="Unresolved">
    /// Codes the enumeration never returned that Windows would not say were installed
    /// or not. A count and not a list: there is nothing to be done with the identity of
    /// a product Windows will not answer about, and the count is what the withholding
    /// needs.
    /// </param>
    /// <param name="UnsettledEnumerated">
    /// Codes the enumeration returned whose own keyed answer would not come, or left out
    /// an installation the enumeration listed, so which installations of them exist is
    /// not established.
    /// </param>
    private readonly record struct MissedProducts(
        List<(string ProductCode, string? Sid, MsiInstallContext Context)> Recovered,
        int RecoveredOfEnumerated,
        int Unresolved,
        HashSet<string> UnsettledEnumerated);

    /// <summary>
    /// Re-establishes every removable verdict by ASKING each enumerated product
    /// about the patch, instead of inferring it from each product's patch list
    /// having come back whole.
    ///
    /// A PATCH LIST THAT ENDS EARLY IS ONE THING IT CLOSES. A cached patch is
    /// claimed once and shared by every product holding it, and the merge is
    /// downgrade-only, so a patch that is Superseded under one product and
    /// Applied under another stays non-removable ONLY IF the Applied row reaches
    /// the merge. That row reaches it through the second product's patch
    /// enumeration, and an enumeration that returns ERROR_NO_MORE_ITEMS early is
    /// indistinguishable from one that finished: <see cref="EnumeratePatches"/>
    /// treats it as a clean end at any index, so nothing is marked incomplete,
    /// no product is counted unreadable, and nothing the truncation leaves behind
    /// fires the scan-wide withholding.
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
    /// enumeration having been complete, and asking each of them, the ones whose
    /// patch rows the product loop has already read included, means it does not
    /// depend on which path any record names either.
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
    /// Installations the enumeration never listed that the registry comparison then
    /// found installed (<see cref="LocateProductsTheEnumerationMissed"/>), of products
    /// it never returned and of products it returned under another account. They are
    /// asked exactly as enumerated installations are, which is the point: an
    /// installation recovered by name can answer for the patches it holds.
    /// </param>
    /// <param name="reach">
    /// What the registry established about which cached files each product's own
    /// patches record. Read for the recovered products alone, and only to narrow the
    /// paths each is judged against; its default narrows nothing. See
    /// <see cref="EstablishedPatchReach"/>.
    /// </param>
    /// <param name="heldByName">
    /// Where to record each pairing this pass read as holding a patch it could offer: an
    /// installation that answered, asked by name, that it holds the patch superseded and
    /// declaring zero. Null records nothing. The caller keeps the ones whose path is still
    /// removable at the end of the scan, for the check made under the lease just before a
    /// Move or Delete.
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
        PerItemFailureLog? unreadPatchFileLog = null,
        ICollection<PatchClaim>? heldByName = null)
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

        // ROUTE A. Every (patch, product) pairing the API will name when asked
        // about no product in particular, which is the only way to hear about a
        // product the product enumeration never returned. Null where it did not
        // run to a clean end, and that withholds rather than reading as nothing
        // to report: the API returns no rows both where it refuses and where it
        // finds nothing, and the null is what tells the two apart.
        var holders = EnumeratePatchHoldersAcrossAllProducts(_msi, ct);

        // ONE STATE READ PER PAIRING, SHARED BY BOTH PASSES BELOW. The judging pass asks
        // every listed installation about the codes naming each still-removable path, and
        // the per-pairing pass asks the same installations about the same codes, so the
        // answer is kept rather than read twice. Per call, like the cache below it.
        var stateReads = new Dictionary<Pairing, PropertyRead>();
        PropertyRead StateOf(string patchCode, string productCode, string? sid, MsiInstallContext context)
        {
            var key = new Pairing(patchCode, productCode, sid, context);
            if (stateReads.TryGetValue(key, out var read)) return read;
            return stateReads[key] = GetPatchProperty(_msi, patchCode, productCode, sid, context,
                MsiInstallProperty.State);
        }

        // ONE WAY OF ASKING, SHARED BY THE PER-PAIRING PASS AND BY THE PASS OVER ROWS NO
        // OTHER PASS ASKS ABOUT. Every installation the enumeration returned, every one the
        // recovery by name found, every holder route A names for the code, and any
        // installation passed in, is asked whether it holds the patch. The answer is
        // whether any of them did not answer, and the installations that hold the patch and
        // have not shown it removable. The asking stops at the first that did not answer.
        //
        // AN INSTALLATION THAT DID NOT ANSWER SETTLES IT, WHATEVER ANY OTHER INSTALLATION
        // SAYS: the per-pairing pass withholds the path, and the pass over rows no other
        // asks about marks the row. A claim does not settle it: the reads go on past one,
        // and a claim counts only where every installation asked has answered. Which
        // installation is asked first then decides nothing.
        //
        // Do not let a claim end the reads. The missing-files split reads a row kept on a
        // claim as one whose verdict this pass established, which holds only where every
        // installation holding the patch answered.
        (bool Unanswered, List<HeldByAsking>? Holds) AskEveryInstallation(
            string path,
            string patchCode,
            IReadOnlyList<(string ProductCode, string? Sid, MsiInstallContext Context)> alsoAsk,
            bool recordHeld)
        {
            var toAsk = new List<(string ProductCode, string? Sid, MsiInstallContext Context)>(products);
            toAsk.AddRange(recovered);
            holders!.TryGetValue(patchCode, out var named);
            if (named is not null) toAsk.AddRange(named);
            toAsk.AddRange(alsoAsk);

            List<HeldByAsking>? holds = null;
            foreach (var (productCode, userSid, context) in toAsk)
            {
                ct.ThrowIfCancellationRequested();

                // State first and alone where it settles the pairing. A product
                // that does not hold this patch answers ERROR_UNKNOWN_PATCH to
                // the sizing call, so the overwhelming majority of pairings cost
                // one property read and the second is never made.
                var state = StateOf(patchCode, productCode, userSid, context);

                // ONLY AN INSTALLATION ANSWERING THAT IT HOLDS NO RECORD OF THE PATCH IS
                // SKIPPED, AND NOT ONE THE MACHINE-WIDE PATCH ENUMERATION HAS LISTED AS
                // HOLDING IT. From any other installation that answer is a positive one
                // that it does not hold the patch, so it says nothing about the verdict
                // either way. From a listed holder it contradicts the listing, which
                // named the same product, account and context this read is put in, so
                // it counts with every other read that did not answer.
                //
                // AN ANSWER THAT THE PRODUCT IS NOT INSTALLED IS NEVER SKIPPED. Every
                // installation on this list was listed earlier in this scan, by the
                // product enumeration, the recovery by name, the machine-wide patch
                // enumeration or the resolve of a declared target, so that answer
                // contradicts what the scan established, and it counts with them too.
                if (state.PatchNotHeld && !IsListedHolder(named, productCode, userSid, context)) continue;

                if (state.Unreadable) return (true, holds);

                // NOTHING HERE IS SKIPPED. The State read has just answered, and not
                // that this installation holds no record of the patch, so it holds
                // one, and an answer now that it does not, or that its product is not
                // installed, contradicts the one before it. Both are unreadable as
                // well.
                var uninstallable = GetPatchProperty(_msi, patchCode, productCode, userSid, context,
                    MsiInstallProperty.Uninstallable);

                // An empty answer the verdict turns on did not answer either, and counts
                // with the reads that failed rather than as a claim
                // (LeavesVerdictUnestablished). An installation where the patch is
                // applied or obsoleted claims it below, whatever its Uninstallable says.
                if (uninstallable.Unreadable || LeavesVerdictUnestablished(state.Value, uninstallable.Value))
                    return (true, holds);

                // This product holds the patch and has not shown it removable, which is
                // the claim the truncated enumeration would have contributed. Same
                // verdict, reached by asking.
                if (!IsRemovablePatch(state.Value, uninstallable.Value))
                {
                    int.TryParse(state.Value, out var heldState);
                    (holds ??= []).Add(new HeldByAsking(heldState, patchCode, productCode, userSid, context));
                }
                else if (recordHeld)
                    heldByName?.Add(new PatchClaim(path, patchCode, productCode, userSid, (int)context));
            }

            return (false, holds);
        }

        // The same question under every code naming one path, for a path whose own file
        // cannot say which installations to ask about. Every code is asked before anything
        // is decided, so which code the work list reaches first decides nothing. Codes are
        // taken in one fixed order.
        (bool Unanswered, List<HeldByAsking>? Holds) AskAboutEveryCode(string path, IEnumerable<string> codes)
        {
            List<HeldByAsking>? holds = null;
            foreach (var code in codes.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
            {
                var (unanswered, found) = AskEveryInstallation(path, code, [], recordHeld: true);
                if (found is not null) (holds ??= []).AddRange(found);
                if (unanswered) return (true, holds);
            }

            return (false, holds);
        }

        // What the installations that hold the patch say to the missing-files warning
        // (Hold), the product's name read for one whose reading is stronger than the row's.
        //
        // ONLY AN INSTALLATION WHOSE OWN CLAIM NEVER REACHED THE MERGE. One whose claim did is
        // on a row already, this one or the row its recorded path lands on, and that row
        // carries its reading: carried here as well, one file would be counted twice.
        var enumeratedClaims = new HashSet<Pairing>(patchClaims.Select(PairingOf));
        RegisteredPackage HoldEach(RegisteredPackage row, List<HeldByAsking> holds)
        {
            foreach (var held in holds)
            {
                if (enumeratedClaims.Contains(new Pairing(held.PatchCode, held.ProductCode, held.Sid, held.Context)))
                    continue;

                var name = ReadingRank(held.State) > ReadingRank(row.PatchState)
                    ? GetProductProperty(held.ProductCode, held.Sid, held.Context, MsiInstallProperty.ProductName).Value
                    : null;
                row = Hold(row, held.State, held.ProductCode, name);
            }

            return row;
        }

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
        // code the file names that no walk lists an installation of contributes nothing
        // here and is not a failure, what the registry holds for it being read apart
        // (WithholdOnUnlistedEntries); a code that could not be asked about withholds.
        //
        // AND EVERY ANSWER IS HELD AGAINST THE INSTALLATIONS THIS RUN LISTED. A declared
        // target answered "not installed", or answered with a list short of an
        // installation the product walk or the recovery by name established, withholds
        // as a code that could not be asked about does
        // (HoldsEveryListedInstallation).
        var listed = InstallationsByCode(products.Concat(recovered));
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
            claimed, patchClaims, holders, products, recovered, reach, registryPatchSets, apiPatchSets,
            DeclaredTargetsFor, (patchCode, productCode, sid, context) =>
                !StateOf(patchCode, productCode, sid, context).PatchNotHeld,
            ct);

        // THE ROWS THE PER-PAIRING PASS NEVER ASKS ABOUT, ASKED FOR THE MISSING-FILES
        // WARNING'S SAKE. A superseded patch that is not removable, or an obsoleted one, is
        // on no work list, so an installation holding it applied whose own claim never
        // reached the merge is heard nowhere else, and the warning would read the file's
        // absence as harmless. So every row the warning would exempt
        // (MissingFilesReport.AbsenceShownHarmless) is put to the same installations under
        // every code naming it. An installation that holds the patch carries its reading
        // onto the row where that reading is the stronger (Hold), and one that did not
        // answer marks the row, so the warning reports it. The judging pass above has
        // already put every installation answering for such a row into its product set.
        //
        // IT ASKS WITHOUT KNOWING WHETHER THE FILE IS THERE. Existence is stamped later,
        // against the filesystem the scan walks, and a second reading of it here could
        // disagree with that stamp: one that read "present" for a file the stamp calls
        // gone would leave that row unasked, and nothing would say so. Every row the
        // warning could exempt is asked instead, which is a superset of the ones whose
        // file has gone. What it costs is those rows, times their codes, times the
        // installations asked, one State read each, shared with the passes around it.
        //
        // Only where route A answered: where it did not, every patch row's verdict is
        // Unestablished and none of them is exempt.
        if (holders is not null)
        {
            var codesByRow = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var claim in patchClaims)
            {
                if (!claimed.TryGetValue(claim.LocalPackagePath, out var row)
                    || row.IsRemovable || !MissingFilesReport.AbsenceShownHarmless(row)) continue;
                if (!codesByRow.TryGetValue(claim.LocalPackagePath, out var codes))
                    codesByRow[claim.LocalPackagePath] = codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                codes.Add(claim.PatchCode);
            }

            foreach (var (path, codes) in codesByRow)
            {
                ct.ThrowIfCancellationRequested();

                List<HeldByAsking>? holds = null;
                var unanswered = false;
                foreach (var code in codes.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
                {
                    var (noAnswer, found) = AskEveryInstallation(path, code, [], recordHeld: false);
                    if (found is not null) (holds ??= []).AddRange(found);
                    if (noAnswer)
                    {
                        unanswered = true;
                        break;
                    }
                }

                if (unanswered) claimed[path] = claimed[path] with { OtherHoldNotRuledOut = true };
                else if (holds is not null) claimed[path] = HoldEach(claimed[path], holds);
            }
        }

        // An empty work list settles it. Everything below is per-pairing and there are
        // no pairings to ask about.
        if (toConfirm.Count == 0) return;

        // The codes naming each path on the work list, for a path whose own file cannot
        // say which installations to ask about and so is asked about under all of them.
        var codesByPath = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, code) in toConfirm)
        {
            if (!codesByPath.TryGetValue(path, out var codes)) codesByPath[path] = codes = [];
            codes.Add(code);
        }

        foreach (var (path, patchCode) in toConfirm)
        {
            ct.ThrowIfCancellationRequested();

            // A path another code has already settled needs no second pass: the
            // verdict is gone and cannot come back, downgrades being one-way.
            if (!claimed.TryGetValue(path, out var current) || !current.IsRemovable) continue;

            if (holders is null)
            {
                Downgrade(claimed, path, withheld: true, scanWide: true);
                continue;
            }

            // The products to put the question to: the ones the enumeration returned,
            // the ones the registry named and the enumeration did not, plus any route A
            // named for this patch, plus any the patch file itself says it targets
            // (AskEveryInstallation). They overlap heavily on a healthy machine and are
            // unioned rather than chosen between, because each sees something the others
            // cannot and every one of them can only add a product to ask.
            //
            // EVERY INSTALLATION ON THE LIST IS ASKED, THE ONES WHOSE PATCH ROWS THE
            // PRODUCT LOOP HAS ALREADY READ INCLUDED. The loop's reading of a pairing
            // reaches this row through the merge only where that pairing's recorded path
            // normalises to this row's path, and a recorded path normalising to any other
            // lands on a row of its own. Asking every installation here means the answer
            // does not depend on which path any record names. Skipping the pairings the
            // loop has read would take each one's answer as already on this row, which
            // holds only where its recorded path normalises to this one.
            //
            // A product the patch file names that Windows will not answer about, or
            // answers about without an installation this run listed, is a question left
            // open rather than an answer of no, and withholds the path with no cause
            // recorded. So where the file has gone by the time the scan stamps whether it
            // is there, the missing-files warning counts it.
            var fromFile = DeclaredTargetsFor(path);
            if (fromFile.Unaskable)
            {
                Downgrade(claimed, path, withheld: true);
                continue;
            }

            // A PATCH FILE THAT WILL NOT READ IS ASKED ABOUT ALL THE SAME, under every code
            // naming it, before anything is decided. A file that has gone never reads, and
            // an installation holding the patch whose own claim never reached the merge
            // answers by the patch's code whatever its enumeration did, which is what says
            // the file is still needed. Every code is asked because the work list reaches
            // the path under one of them first, and which one that is must decide nothing.
            var (unanswered, holds) = fromFile.Unreadable
                ? AskAboutEveryCode(path, codesByPath[path])
                : AskEveryInstallation(path, patchCode, fromFile.Installed, recordHeld: true);

            if (unanswered)
            {
                Downgrade(claimed, path, withheld: true);
            }
            else if (holds is not null)
            {
                Downgrade(claimed, path, withheld: false);
                claimed[path] = HoldEach(claimed[path], holds);
            }
            else if (fromFile.Unreadable)
            {
                // EVERY INSTALLATION ANSWERED AND NONE HOLDS THE PATCH, so the unread file
                // is the one reason left, and it is recorded as that. Recording it changes
                // nothing here: the verdict still goes and the file is still kept. The
                // flag is read much later, by the missing-files split, and only ever for a
                // row whose file turned out not to be there.
                //
                // IT HAS TO BE RECORDED RATHER THAN WORKED OUT LATER, because an unread
                // declaration carries two meanings and this is the only place that knows
                // which was met. A file that is THERE and will not give up an identity is
                // the app unable to establish something it could have established. A file
                // that is NOT THERE cannot be read by anybody, so the same withholding is a
                // tautology and says nothing about the machine. Which it met is not decided
                // here: FileSystemScanService stamps FileExists once, against the same
                // filesystem it walks, and the two facts meet there. A second reading of
                // existence here could disagree with that stamp.
                //
                // SO A SUPERSEDED FILE THAT HAS GONE, WITHHELD FOR THIS ALONE, IS JUDGED ON
                // ITS VERDICT. Where its products' patch sets are clean,
                // MissingFilesReport.Affected reads the failed read as the tautology it
                // is: the file would not read because it has gone.
                Downgrade(claimed, path, withheld: true, unreadableFile: true);
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
    /// on the computer. A keyed read is put to an installation a walk listed, and a key
    /// the walks leave out is read apart (<see cref="WithholdOnUnlistedEntries"/>).
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
    /// none of whose declared targets the walks list yields an empty
    /// <paramref name="Installed"/> with both flags clear, and that is a positive answer
    /// about every installation Windows lists: none of them is a target of it. A key the
    /// walks leave out is read apart (<see cref="WithholdOnUnlistedEntries"/>).
    /// <paramref name="Unreadable"/> is the file declining to say
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

        // EVERY PATH HANDED TO THIS IS READ, WHATEVER THE FILE IS CALLED. It is asked
        // only about rows still carrying the removable verdict, which only a superseded
        // patch's registration is granted, so the registration makes the file a patch's
        // and its name decides nothing. A file that does not read as a patch withholds
        // like any other file that does not read.
        //
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
    /// context to ask it in; empty with no <c>Unaskable</c>, meaning Windows lists no
    /// installation of the code, which is a clean answer about what can be asked and no
    /// answer about what the registry holds (<see cref="UnlistedEntries"/>); or
    /// <c>Unaskable</c>, which withholds. Which returns end the list is
    /// <see cref="IsProductNotInstalled"/>'s, and there is more than one of them.
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
    /// THE ANSWER IS AN ADMINISTRATOR'S. The question is put with the Everyone SID across
    /// all contexts, which Microsoft documents as needing administrator privileges, and
    /// both hosts refuse to scan in a process without them
    /// (<see cref="Helpers.AdministratorRights"/>). ERROR_ACCESS_DENIED, the return
    /// Microsoft documents for a caller without the rights, is not on
    /// <see cref="IsProductNotInstalled"/>'s list, so it makes the code unaskable.
    ///
    /// AN ANSWER LISTING NOTHING IS NOT AN ANSWER THAT NO ACCOUNT HOLDS THE PROGRAM. An
    /// administrator's walk leaves some programs out (<see cref="UnlistedEntries"/>), and a
    /// caller reading such an answer as nothing being there has to read what the registry
    /// holds for that code beside it, as <see cref="WithholdOnUnlistedEntries"/> does. A
    /// new caller runs behind the administrator check too.
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
        IEnumerable<(string ProductCode, string? Sid, MsiInstallContext Context)> installations)
    {
        var byCode = new Dictionary<string, List<(string? Sid, MsiInstallContext Context)>>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var (code, sid, context) in installations)
        {
            if (!byCode.TryGetValue(code, out var of))
                byCode[code] = of = [];
            of.Add((sid, context));
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
    /// <param name="scanWide">
    /// Records that what established nothing was a condition on the whole machine and not
    /// this file's own patch. Like
    /// <paramref name="unreadableFile"/>, every caller passing it also passes
    /// <paramref name="withheld"/> true. See <see cref="RegisteredPackage.WithheldScanWide"/>.
    /// </param>
    private static void Downgrade(
        Dictionary<string, RegisteredPackage> claimed, string path, bool withheld,
        bool unreadableFile = false, bool scanWide = false)
    {
        if (!claimed.TryGetValue(path, out var row) || !row.IsRemovable) return;
        claimed[path] = row with
        {
            IsRemovable = false,
            RemovableWithheld = withheld,
            WithheldOnUnreadableFile = unreadableFile,
            WithheldScanWide = scanWide,
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
    /// WHAT THE COUNTED HALF THEN BUYS IS THE WHOLE OFFER. A counted failure arms
    /// <c>EnumerationCensus.AnyRecordedPathUnestablished</c>, which keeps back every
    /// candidate the walk found and no registration claims, and every superseded row
    /// still on the offer when <see cref="GetRegisteredPackagesCore"/> reaches its
    /// scan-wide withholding. Neither is narrowed to the claim that failed, because
    /// nothing says which file it names. The census leaves the string this method
    /// returned as it is, proved or counted, and the surfaces that read a
    /// registration's own recorded path go on reading it: the correlation gate, the
    /// missing-from-disk counts and the registered-files window.
    ///
    /// EACH OF THOSE TAKES AN UNPROVEN SPELLING IN THE DIRECTION THAT KEEPS MORE
    /// BACK. A claim whose spelling names no walked file lowers the correlation
    /// count, which moves the scan towards refusing outright; one whose file is not
    /// found where the claim says raises the missing count, which is a warning
    /// rather than an offer.
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
    /// A PATH THE RESOLVER DOES NOT SETTLE is kept as GetFullPath spells it, or exactly
    /// as recorded where GetFullPath refuses it as well, and neither need be how the walk
    /// spells the file it names, so the refusal is counted and
    /// <c>EnumerationCensus.AnyRecordedPathUnestablished</c> withholds the whole
    /// walk-derived offer and every superseded row on it: nothing says WHICH file the
    /// unresolved claim meant, so no narrower set can be held back. A final path is
    /// resolved by <see cref="InstallerCacheHelpers.ResolveFinalPathOutcome(string, out string)"/>,
    /// which names the outcome it reached. The catch counts an exception from the
    /// expansion, the prefix strip or GetFullPath against the step it was thrown in. A
    /// variable that is not set in this process's environment is left in the value as
    /// written, '%' signs and all.
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

        // A MARKER IN SCOPE RATHER THAN A TRY BLOCK PER STAGE. Each step sets it as it
        // starts, and the one catch below counts the refusal against the step it names
        // and hands back the value exactly as recorded, whichever step threw. The marker
        // costs an assignment and changes nothing about what a refusal returns.
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
            // COUNTED BEFORE THE ASK AND NOT GATING IT. The two spellings announce
            // themselves in the string, and this scan decides nothing: the resolver
            // below is put every recorded path whatever the scan says, so a reader
            // taking this line for a gate has it wrong. What it answers is how many
            // of a machine's recorded values carry such a spelling, which the
            // attempts count cannot report while everything is asked.
            if (CarriesFlaggedSpelling(stripped)) census.RecordFlaggedSpelling();

            // COUNTED WHETHER IT ANSWERS OR NOT, which is the whole use of the
            // number: the five failures below are meaningless without how many times
            // anything was asked. It separates a scan that read no registrations from
            // one whose every registration resolved.
            census.RecordResolverAttempt();

            // THE RESOLVER ANSWERS EVERY FAILURE AS ONE OF ITS OWN OUTCOMES AND DOES NOT
            // THROW, as its summary sets out, so what it finds on disk is counted with
            // its outcomes and never reaches the catch below. The prefix-strip step is
            // still the one marked here, so a throw from the resolver would be counted as
            // a refusal of that step.
            var outcome = InstallerCacheHelpers.ResolveFinalPathOutcome(
                ToWin32Prefix(stripped), out var resolved);
            census.RecordResolution(outcome);

            // Only a resolved outcome's path is taken. On any other, the out value is
            // not a path the filesystem proved, and using it would dress a guess as an
            // answer.
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
            // A value GetFullPath refuses (one of nothing but spaces, a length past the
            // API's limit) is kept exactly as Windows returned it. It cannot be improved,
            // and dropping the claim would turn an unreadable spelling into an
            // orphaned file. An embedded null is not one of them and is refused
            // above instead, because on Windows it never reaches this call: the
            // expansion truncates it away without throwing.
            //
            // AND THE FACT IS CARRIED OUT RATHER THAN ENDING HERE, WHICH IS WHAT
            // KEEPS THE FILE. What leaves this method is a claim that cannot match
            // anything the folder walk produces or meet the row of the file it means,
            // so on its own it would leave that file unclaimed and on the offer, or
            // leave a superseded row naming it on the offer. The refusal recorded on
            // the next line is what stops that: it reaches
            // <c>EnumerationCensus.AnyRecordedPathUnestablished</c>, which withholds
            // the whole walk-derived offer and every superseded row, so nothing is
            // offered while a claim nobody could read stands.
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
    /// THE CAUSE IS KEPT OUT OF ENUMERATION ORDER AS WELL AS THE VERDICT. Two
    /// non-removable claims on a path are not necessarily the same finding: one
    /// product's Applied claim names the file, and another product's failed State read
    /// names nothing at all. So the row reads unjudged only where no claim that keeps
    /// the file read cleanly, whichever claim's account it carries
    /// (<see cref="Merge"/>), and neither what the app DOES with the file nor what it
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

        claimed[candidate.LocalPackagePath] = Merge(existing, candidate);
        return false;
    }

    /// <summary>
    /// The row two API claims on one path leave, and the same row whichever of them the
    /// enumeration reached first, so the same row for any number of claims in any order.
    /// A registration is one product's account of the file, so the row is one claim's
    /// account, its product name included, with four facts taken across both.
    ///
    /// THE ACCOUNT IS THE CLAIM THAT COMES FIRST IN ONE FIXED ORDER (<see cref="Account"/>):
    /// a claim that is not removable, then one whose state reads as applied
    /// (<see cref="ReadingRank"/>), then the product code and the product name. So a
    /// non-removable claim takes the verdict off a removable row with its whole account,
    /// and the row names a product whose state reads as applied wherever one does.
    ///
    /// THE ROW READS UNJUDGED ONLY WHERE NO CLAIM ON IT ESTABLISHED ANYTHING: where some
    /// claim's reads failed and no claim that is not removable read cleanly. A claim that
    /// read cleanly and keeps the file is a live claim on it, whichever claim's account the
    /// row carries, and a removable claim establishes nothing about the file's being kept.
    ///
    /// THE STATE IS THE STRONGEST READING EITHER CLAIM GAVE (<see cref="ReadingRank"/>),
    /// whichever claim's account the row carries. One cached patch can be superseded under
    /// one product and still applied under another, and it is the stronger reading that
    /// says the file is still needed. A claim carrying no state, a product's own package
    /// record or a patch registration whose State would not read, never replaces a reading:
    /// zero is what a State that did not read, or did not parse, arrives as, and it means
    /// not-a-patch to everything downstream that asks.
    ///
    /// THE ROW IS REMOVABLE ONLY WHERE BOTH CLAIMS ARE, so no order of claims can put a file
    /// on the offer that either claim alone keeps. Removability is granted where a claim is
    /// built and never afterwards.
    ///
    /// AND A SUPERSEDED OR OBSOLETED ROW THAT ANY CLAIM CARRYING NO STATE HAS NAMED is marked
    /// <see cref="RegisteredPackage.OtherHoldNotRuledOut"/>: the claim with no state brought
    /// nothing to put on the row, and it still names the file.
    /// </summary>
    private static RegisteredPackage Merge(RegisteredPackage existing, RegisteredPackage candidate)
    {
        static bool Establishes(RegisteredPackage claim) => !claim.IsRemovable && !claim.VerdictUnreadable;

        var account = Account(candidate, existing) > 0 ? candidate : existing;
        var state = ReadingOrder(candidate.PatchState, existing.PatchState) > 0
            ? candidate.PatchState
            : existing.PatchState;

        var row = account with
        {
            PatchState = state,
            IsRemovable = existing.IsRemovable && candidate.IsRemovable,
            VerdictUnreadable = !(Establishes(existing) || Establishes(candidate))
                && (existing.VerdictUnreadable || candidate.VerdictUnreadable),
        };

        return row with
        {
            OtherHoldNotRuledOut = row.IsSupersededOrObsoleted
                && (existing.OtherHoldNotRuledOut || candidate.OtherHoldNotRuledOut
                    || existing.PatchState == 0 || candidate.PatchState == 0),
        };
    }

    /// <summary>
    /// Whether <paramref name="a"/>'s account of a file comes before <paramref name="b"/>'s:
    /// positive where it does, negative where it does not, and zero only for two accounts
    /// that are the same in every field it reads.
    ///
    /// EVERY KEY IS ONE <see cref="Merge"/> LEAVES ON THE ROW AS IT FOUND IT ON THE ACCOUNT,
    /// which is what makes the row the same in any order: the row is removable only where no
    /// claim is not, it reads applied only where some claim does and that claim then comes
    /// first, and its code and name are its account's own. Do not add as a key the reading
    /// rank below applied, or whether the reads established anything. Merge takes the row's
    /// state and its unreadable flag across all the claims rather than from its account, so
    /// a key on either would compare one claim's field with another's, and the order of the
    /// claims would decide the account again.
    /// </summary>
    private static int Account(RegisteredPackage a, RegisteredPackage b)
    {
        var order = (!a.IsRemovable).CompareTo(!b.IsRemovable);
        if (order != 0) return order;

        order = (ReadingRank(a.PatchState) == AppliedRank).CompareTo(ReadingRank(b.PatchState) == AppliedRank);
        if (order != 0) return order;

        // A lower code or name comes first, and this is only to settle a tie the same way
        // every time.
        order = -StringComparer.OrdinalIgnoreCase.Compare(a.ProductCode, b.ProductCode);
        if (order != 0) return order;

        order = -StringComparer.Ordinal.Compare(a.ProductCode, b.ProductCode);
        if (order != 0) return order;

        order = -StringComparer.OrdinalIgnoreCase.Compare(a.ProductName, b.ProductName);
        if (order != 0) return order;

        return -StringComparer.Ordinal.Compare(a.ProductName, b.ProductName);
    }

    /// <summary>
    /// Which of two patch states is the stronger reading: the higher
    /// <see cref="ReadingRank"/>, and between two states of one rank, the higher number, so
    /// the choice is the same whichever is met first.
    /// </summary>
    private static int ReadingOrder(int a, int b)
    {
        var order = ReadingRank(a).CompareTo(ReadingRank(b));
        return order != 0 ? order : a.CompareTo(b);
    }

    /// <summary>
    /// How strongly a patch state says a product may still need the cached file, for
    /// choosing between two readings of one file: none (0), superseded (2), obsoleted (4),
    /// and any other state, which is read as strongly as applied. That last is the direction
    /// that keeps the missing-files warning: a state that is neither superseded nor
    /// obsoleted takes a row out of the one exemption the warning grants.
    /// </summary>
    private static int ReadingRank(int patchState) => patchState switch
    {
        0 => 0,
        2 => 1,
        4 => 2,
        _ => AppliedRank,
    };

    private const int AppliedRank = 3;

    /// <summary>
    /// What one more claim on a superseded or obsoleted row's file says to the missing-files
    /// warning, where the claim is not a row of its own: a registry package record, the
    /// listing of a product key no installation accounts for, a failed read of the registry
    /// records that may have held such a listing, or an installation that answered, asked by
    /// the patch's code, that it holds the patch. A
    /// stronger reading than the row's (<see cref="ReadingRank"/>) replaces the row's state,
    /// and the row takes the claimant's product code and name where the name read. No state
    /// at all marks the row <see cref="RegisteredPackage.OtherHoldNotRuledOut"/>. Anything
    /// else, and any claim on a row that is not a superseded or obsoleted patch, changes
    /// nothing.
    ///
    /// IT NEVER TOUCHES THE REMOVABLE VERDICT. Every caller has already decided that, and
    /// every row this changes is one the caller has made non-removable or found so. What
    /// it changes is read by the missing-files warning, by the counts of superseded and
    /// obsoleted registrations and as the name the row is listed under, and by nothing that
    /// offers a file.
    /// </summary>
    private static RegisteredPackage Hold(
        RegisteredPackage row, int claimState, string? claimantCode = null, string? claimantName = null)
    {
        if (!row.IsSupersededOrObsoleted) return row;
        if (claimState == 0) return row with { OtherHoldNotRuledOut = true };
        if (ReadingRank(claimState) <= ReadingRank(row.PatchState)) return row;

        return string.IsNullOrEmpty(claimantName) || claimantCode is null
            ? row with { PatchState = claimState }
            : row with { PatchState = claimState, ProductCode = claimantCode, ProductName = claimantName };
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
    /// into one outer try: this fallback is the second of the app's two independent
    /// "still needed" sources, and a single try spanning every SID would let one
    /// corrupt subkey or unreadable DACL abandon the entire remaining fallback,
    /// turning every registration only it would have contributed into an orphan
    /// candidate. Scoping the catch to each key read costs one entry per bad key,
    /// never the net.
    ///
    /// EVERY PATH THE REGISTRY RECORDS IS CLAIMED, ON EVERY SCAN, WHATEVER THE API
    /// ENUMERATION RETURNED. While a Windows Installer session is installing a product,
    /// the product's LocalPackage value can be in UserData before the API lists the
    /// product with it, so an enumeration that ran to a clean end can be short of a
    /// cached copy the registry already names. The claim here is what keeps that copy off
    /// the folder walk's candidates. Read only where the API reported itself short, this
    /// fallback would leave such a copy unclaimed, and nothing after it would name it.
    /// </summary>
    private static FallbackRead ReadRegistryFallback(
        Dictionary<string, RegisteredPackage> claimed,
        CancellationToken ct)
    {
        var failures = 0;
        var productKeys = 0;
        var unclaimedProductFileCodes = new List<string?>();
        var unclaimedPatchFileCodes = new List<string?>();
        var packageRecords = new List<RegistryPackageRecord>();
        var patchListings = new Dictionary<AccountCode, IReadOnlyCollection<string>>();
        var productEntries = new List<RegistryProductEntry>();
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
                    if (sidRead.UnclaimedProductFileCodes is not null)
                        unclaimedProductFileCodes.AddRange(sidRead.UnclaimedProductFileCodes);
                    if (sidRead.UnclaimedPatchFileCodes is not null)
                        unclaimedPatchFileCodes.AddRange(sidRead.UnclaimedPatchFileCodes);
                    if (sidRead.PackageRecords is not null)
                        packageRecords.AddRange(sidRead.PackageRecords);
                    // Keyed by account, so no two subtrees share a key and nothing
                    // is merged.
                    if (sidRead.PatchListings is not null)
                        foreach (var (key, listed) in sidRead.PatchListings)
                            patchListings[key] = listed;
                    if (sidRead.ProductEntries is not null)
                        productEntries.AddRange(sidRead.ProductEntries);
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

        return new FallbackRead(failures, productKeys, unclaimedProductFileCodes, unclaimedPatchFileCodes,
            nonStringValues, productCodes, unparseableKeyNames, patchSets,
            productPatchKeys, productPatchRegistrations,
            // Counted off the merged map rather than tallied per SID, for the reason
            // the verdict-unreadable count is: a product registered under two
            // subtrees is one product here and two increments there.
            patchSets.Values.Count(v => v == ProductPatchSet.RemovablePatchPresent),
            patchSets.Values.Count(v => v == ProductPatchSet.Unestablished),
            pathCensus,
            new EstablishedPatchReach(patchCodesByProduct, cachedPathsByPatchCode),
            packageRecords,
            patchListings,
            productEntries);
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
        var unclaimedProductFileCodes = new List<string?>();
        var unclaimedPatchFileCodes = new List<string?>();
        var packageRecords = new List<RegistryPackageRecord>();
        var patchListings = new Dictionary<AccountCode, IReadOnlyCollection<string>>();
        var productEntries = new List<RegistryProductEntry>();
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
                    IReadOnlyCollection<string>? entryCodes = null;
                    var hasInstallProperties = true;
                    if (unpacked is not null)
                    {
                        try
                        {
                            var set = ReadProductPatchSet(productsKey, prodGuid,
                                ref productPatchKeys, ref productPatchRegistrations,
                                out var heldCodes, out var patchesKeyPresent);
                            entryCodes = heldCodes;
                            patchSets[unpacked] = patchSets.TryGetValue(unpacked, out var existing)
                                ? Worse(existing, set)
                                : set;
                            patchCodesByProduct[unpacked] =
                                patchCodesByProduct.TryGetValue(unpacked, out var seenCodes)
                                    ? MergeEstablishedNames(seenCodes, heldCodes)
                                    : heldCodes;
                            if (patchesKeyPresent && heldCodes is not null)
                                patchListings[new AccountCode(sidName, unpacked)] = heldCodes;
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
                        hasInstallProperties = ipKey is not null;

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
                                packageRecords.Add(new RegistryPackageRecord(path, IsPatch: false, unpacked, sidName));
                                // Short-circuited on purpose: the disk is asked about
                                // only the paths the API left unclaimed, which is none
                                // of them where the API listed every product the
                                // registry records.
                                if (MergeClaim(claimed, new RegisteredPackage(path, "", ""),
                                        ClaimSource.RegistryFallback)
                                    && File.Exists(path))
                                    unclaimedFileHere = true;
                            }
                        }

                        if (unclaimedFileHere) unclaimedProductFileCodes.Add(unpacked);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        failures++;
                        failureLog.Record(ex, cause: "product-entry");
                    }

                    if (unpacked is not null)
                        productEntries.Add(
                            new RegistryProductEntry(sidName, unpacked, entryCodes, hasInstallProperties));
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
                                packageRecords.Add(new RegistryPackageRecord(path, IsPatch: true, patchCode, sidName));
                                if (MergeClaim(claimed, new RegisteredPackage(path, "", ""),
                                        ClaimSource.RegistryFallback)
                                    && File.Exists(path))
                                    unclaimedFileHere = true;
                            }
                        }

                        if (unclaimedFileHere) unclaimedPatchFileCodes.Add(patchCode);
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

        return new FallbackRead(failures, productKeys, unclaimedProductFileCodes, unclaimedPatchFileCodes,
            nonStringValues, null, unparseableKeyNames, patchSets,
            productPatchKeys, productPatchRegistrations,
            Paths: pathCensus,
            Reach: new EstablishedPatchReach(patchCodesByProduct, cachedPathsByPatchCode),
            PackageRecords: packageRecords,
            PatchListings: patchListings,
            ProductEntries: productEntries);
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
        IReadOnlyList<(string ProductCode, string? Sid, MsiInstallContext Context)> enumerated,
        IReadOnlyList<(string ProductCode, string? Sid, MsiInstallContext Context)> recovered,
        EstablishedPatchReach reach,
        IReadOnlyDictionary<string, ProductPatchSet>? registryPatchSets,
        IReadOnlyDictionary<string, ProductPatchSet> apiPatchSets,
        Func<string, DeclaredTargets> declaredTargets,
        Func<string, string, string?, MsiInstallContext, bool> answersHoldingPatch,
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
        // AND IT IS NOT NARROWED TO "REMOVABLE PLUS MISSING", which is the obvious
        // saving: existence is stamped once, later, by FileSystemScanService against the
        // filesystem it walks, and a second reading of it here could disagree with that
        // stamp, leaving a row whose file has gone unjudged. So the set judged here is
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

            // AND EVERY INSTALLATION THIS SCAN LISTED, ASKED BY NAME, WHEREVER IT ANSWERS
            // ANYTHING BUT THAT IT HOLDS NO RECORD OF ONE OF THE PATH'S PATCHES. The claims
            // name a product only where its enumeration reached the patch, and an
            // enumeration can end early without saying so. A product that still holds the
            // patch answers by its code whatever its enumeration did, so this is what
            // puts it into the set when its own claim on the file never reached the merge.
            // An answer that did not come, or that the product is not installed, puts it
            // in too: only a positive "no record" leaves it out.
            //
            // WHERE A ROW IS STILL REMOVABLE, AND WHERE THE MISSING-FILES WARNING WOULD
            // EXEMPT IT (MissingFilesReport.AbsenceHarmlessIfJudgedClean). Those are the rows
            // the per-pairing pass, or the pass after this one over rows nothing else asks
            // about, puts to the same installations, so every answer read here is one read
            // anyway, and the reads are shared. For the second kind, an installation found
            // this way holding the patch in any state can hold something else that could be
            // uninstalled and roll back onto the file, so it joins the set the verdict is
            // taken across.
            //
            // A PRODUCT WHOSE CACHED FILE FOR ANOTHER PATCH IS THIS ONE is not found by
            // asking about this path's patches. Its registry record for that patch names
            // this file, and WithholdOnRegistryPackageRecords has already taken the verdict
            // away on it; where the registry holds no such record for a product whose
            // records came back short, the scan-wide withholding holds every row.
            if (row.IsRemovable
                || (holders is not null && MissingFilesReport.AbsenceHarmlessIfJudgedClean(row)))
                foreach (var installations in new[] { enumerated, recovered })
                    foreach (var (productCode, sid, context) in installations)
                    {
                        if (products.Contains(productCode)) continue;
                        foreach (var patchCode in codesByPath[path])
                            if (answersHoldingPatch(patchCode, productCode, sid, context))
                            {
                                products.Add(productCode);
                                break;
                            }
                    }

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
            // removable path when route A returns null, so a removable row is kept either
            // way, and where no product's removable patch claims it, it arrives at the
            // split carrying the same flags whichever pass got there first. What it DOES
            // change is the row that was never removable, chiefly an obsoleted
            // registration, which the caller's downgrade cannot touch because Downgrade
            // takes a verdict away and there is none to take. Such a row would otherwise
            // carry a positively clean verdict off a product set route A had refused to
            // complete, and the missing-files split would read that as the app having
            // established the absence was harmless.
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
            //
            // A ROW WITHHELD WHILE ROUTE A DID NOT ANSWER IS WITHHELD SCAN-WIDE, since
            // every removable row on such a run starts from Unestablished. One a
            // product's removable patch downgrades is a claim and is not.
            if (!row.IsRemovable) continue;
            var withheld = verdict == ProductPatchSet.Unestablished;
            Downgrade(claimed, path, withheld, scanWide: withheld && holders is null);
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
    /// <param name="patchesKeyPresent">
    /// Whether the product key has a <c>Patches</c> key. The verdict and
    /// <paramref name="patchCodes"/> read an absent one as a product holding no registered
    /// patch. The caller keeps a product's own listing, for an installation whose patch
    /// enumeration came back short, only where this is true: Windows returned patch rows
    /// for that installation, and an absent key says it holds none.
    /// </param>
    internal static ProductPatchSet ReadProductPatchSet(
        Microsoft.Win32.RegistryKey productsKey,
        string packedProductCode,
        ref int patchKeys,
        ref int patchRegistrations,
        out IReadOnlyCollection<string>? patchCodes,
        out bool patchesKeyPresent)
    {
        // NOT ESTABLISHED UNTIL IT IS, and this line rather than a failure path is
        // where that is decided: a path added below that forgets to set it leaves the
        // caller not knowing, which withholds, rather than holding an empty listing,
        // which offers.
        patchCodes = null;
        patchesKeyPresent = false;

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
        // absent one say the identical thing about the machine, so the verdict and the
        // code list are the same for both, and only patchesKeyPresent tells them apart.
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

        patchesKeyPresent = true;

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
    /// The name of the account subtree under <c>UserData</c> that holds the records of an
    /// installation in <paramref name="sid"/> and <paramref name="context"/>:
    /// <c>S-1-5-18</c> per machine, and the account itself per user, managed or not.
    ///
    /// BOTH PER-USER CONTEXTS KEEP THEIR RECORDS UNDER THE USER'S OWN SUBTREE, which is in
    /// HKLM and stays readable while the user is signed out. The product's
    /// <c>InstallProperties</c> and its <c>Patches</c> listing sit at the same paths in
    /// both, and the name of the cached-package value differs
    /// (<see cref="CachedPackageValueNames"/>). A per-user unmanaged installation's
    /// advertised product key and source list are in the user's own hive instead, and
    /// nothing reading this subtree reads either.
    ///
    /// NULL FOR ANY OTHER SHAPE, per machine with an account and per user without one,
    /// and every caller reads a null as an account whose records are not established. An
    /// account is taken only as 'S-' followed by digits and hyphens
    /// (<see cref="IsAccount"/>), so no account names a key outside its own.
    /// </summary>
    internal static string? UserDataAccount(string? sid, MsiInstallContext context) =>
        context == MsiInstallContext.Machine && sid is null ? "S-1-5-18"
        : (context is MsiInstallContext.UserManaged or MsiInstallContext.UserUnmanaged) && IsAccount(sid) ? sid
        : null;

    /// <summary>Whether <paramref name="sid"/> is 'S-' followed by digits and hyphens.</summary>
    internal static bool IsAccount(string? sid)
    {
        if (sid is null || sid.Length < 3 || !sid.StartsWith("S-", StringComparison.Ordinal)) return false;
        for (var i = 2; i < sid.Length; i++)
            if (!char.IsAsciiDigit(sid[i]) && sid[i] != '-') return false;

        return true;
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
    /// Enumerates the products Windows Installer lists across all contexts, or refuses the scan.
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
    /// the row named. The product loop counts the product once in
    /// <c>unreadableProducts</c>, and withholds the removable class unless the
    /// registry holds a patch listing for that installation's own account naming
    /// every patch this returned (<see cref="RegistryHoldsWhatWasLost"/>).
    ///
    /// A sustained run of unreadable rows for ONE product ends that product's
    /// enumeration and returns <c>Incomplete</c>, rather than aborting the whole
    /// scan. The failure is one product's, and the machinery the product loop
    /// runs contains it: the product counts once in the unreadable tally, the
    /// registry fallback still claims that product's cached files so none are
    /// offered, and each superseded patch is either asked about by name or withheld
    /// with the rest of the removable class. So where every index of a per-user
    /// installation refuses identically, the scan
    /// declines to assert a patch list Windows will not hand over, rather than
    /// losing the whole scan over it. The AccessDenied and never-ended-cap throws
    /// below are a machine-level breakdown, and the product loop lets them refuse
    /// the scan. <see cref="PairingsOfHoldersWithNoClaims"/> catches them, the
    /// offer resting on what the judging pass has already read.
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
                // strength of the rows it did not read. The registry fallback claims
                // this product's cached .msp/.msi files independently of the API, so
                // none looks orphaned. Each superseded patch is put to this product
                // by the patch's own code, so one it still holds is kept whatever this
                // list missed, and unless the registry holds a patch listing for this
                // installation's own account naming every patch this list returned,
                // the removable class is withheld scan-wide. Declining to name a patch
                // list Windows refuses to return beats guessing at one. A whole-machine
                // breakdown throws instead: see the AccessDenied arm above and the
                // never-ended cap below.
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
    /// Records that one installation's patch enumeration was abandoned after a full run
    /// of unreadable rows, in the product loop or while listing the patches of an
    /// installation found holding a patch by name (<see cref="PairingsOfHoldersWithNoClaims"/>).
    /// Dev-facing crash-log breadcrumb only, deliberately not localised and never
    /// surfaced. Nothing the user sees and nothing the opt-in report carries says which
    /// product's patch list was abandoned, and diagnosing a superseded file held back
    /// needs exactly that identity. This entry is the one record of which product it
    /// was; without it, pinning a report to a product takes somebody running the Windows
    /// Installer API by hand on that machine. Carries the product code, its install
    /// context and SID (the round-trip that fails when the SID is one the enumerator
    /// emits but rejects), the last error code, and the index reached.
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
            $"after {MaxConsecutiveNonSuccess} consecutive unreadable rows; last error code {lastError}, reached index {index}."),
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
    /// HALF the rule that decides whether a patch's cached file is offered, from its
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
    /// Whether a State and an Uninstallable value that both read, for one pairing of a
    /// patch and a product, still leave that pairing's own verdict unestablished. An
    /// empty value is present and empty or not recorded at all, and it answers nothing:
    /// an empty State does not say whether the patch is applied or superseded, and an
    /// empty Uninstallable beside a superseded State does not say whether the patch can
    /// be rolled back, which is what the removable verdict turns on. Either is counted as
    /// a read that did not answer, never as a claim on the file.
    ///
    /// AN EMPTY UNINSTALLABLE BESIDE ANY OTHER STATE DECIDES NOTHING, and the pairing
    /// stays a claim. An applied or obsoleted patch holds its cached file whether or not
    /// it can be uninstalled, so its State alone keeps the file.
    ///
    /// WHAT TURNS ON THIS IS WHICH COUNT A KEPT FILE IS IN, NEVER WHETHER IT IS KEPT.
    /// Neither shape can pass <see cref="IsRemovablePatch"/>, so a pairing answering
    /// either is kept whatever this says. A product's patch set reads an empty Uninstallable as unestablished whatever
    /// the State beside it, and does not come through here.
    /// </summary>
    internal static bool LeavesVerdictUnestablished(string stateValue, string uninstallableValue) =>
        stateValue.Length == 0
        || (int.TryParse(stateValue, out var patchState) && patchState == 2 && uninstallableValue.Length == 0);

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
    /// Windows Installer lists no further installation of the product asked about. A
    /// third ALLOWLIST for the reason
    /// the two above are ones: only a code documented to mean absence may be read
    /// as absence, and an unlisted one stays unaskable and withholds.
    ///
    /// ERROR_UNKNOWN_PRODUCT belongs on it because that function's return table
    /// glosses it "The product is not installed on the computer in the specified
    /// context", which is an answer rather than a failure to
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
    /// about: the question this call puts is whether Windows Installer lists the code
    /// at all, so a no is Windows Installer answering. It answers for what it lists, and
    /// what it leaves out is read from the registry (<see cref="UnlistedEntries"/>).
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
    internal enum InstanceReading
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
    /// IS SHARED RATHER THAN COPIED. Three call sites ask it, the product enumeration's own
    /// loop, the products that loop lost and the registry named, and
    /// <see cref="DeclaredProductCheck"/>, and what is worth sharing is not the property
    /// read: it is which readings count as an answer. A second copy of that is a second
    /// place for a spelling to be handled, or not handled, and the direction it fails in is
    /// a machine wrongly reported ordinary.
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
    internal static InstanceReading ReadInstanceType(
        IMsiApi msi, string productCode, string? userSid, MsiInstallContext context)
    {
        var read = ReadProductProperty(msi, productCode, userSid, context, MsiInstallProperty.InstanceType);
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
    /// <see cref="ReadProductProperty"/> is: <see cref="DeclaredProductCheck"/> reads the
    /// source used last, its type and the media package path of a product's source list
    /// through it, and which returns count as an absence rather than a failed read is
    /// decided here once.
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
