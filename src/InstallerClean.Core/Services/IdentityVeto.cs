using System.Diagnostics;
using InstallerClean.Interop;
using InstallerClean.Interop.Native;
using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IIdentityVeto"/>. Reads each candidate's declared identity
/// through <see cref="IPackageIdentityReader"/>, then puts that identity to
/// Windows through the Installer API.
///
/// THE QUESTION IS KEYED, AND THAT IS THE POINT RATHER THAN AN OPTIMISATION. A
/// mechanism whose job is to notice what other checks missed must not itself have
/// a way of failing silently, and an enumeration has one: a list that ends at row
/// three of two hundred looks exactly like a list that ended because it was
/// finished. Membership of a short list reads as absence. Asking about a named
/// code cannot fail that way, because <c>MsiGetProductInfoEx</c> and
/// <c>MsiGetPatchInfoEx</c> take no index and walk no list; they answer about the
/// record named, or they say there is no such record, and both are useful.
///
/// SOURCES ARE COMPOSED BECAUSE COMPOSING ONLY EVER WITHHOLDS MORE. No single
/// call sees everything. A keyed property read is blind to another account's
/// per-user products unless that account is named, measured on one machine
/// (2026-08-04) as ERROR_UNKNOWN_PRODUCT in every context with the account left
/// out, and ERROR_INVALID_PARAMETER when handed the everyone SID. So the ladder
/// names every account that has registrations, taken from the same registry
/// subtree those registrations live in, and the filtered enumeration runs behind
/// it as a second opinion. Any source saying yes keeps the file; every source has
/// to answer cleanly before one is offered.
/// </summary>
public sealed class IdentityVeto : IIdentityVeto
{
    /// <summary>
    /// Where a per-user registration lives, and therefore the complete list of
    /// accounts worth naming. An account with no subkey here has no registrations
    /// to ask about.
    /// </summary>
    private const string UserDataKey =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData";

    /// <summary>
    /// The advertisement store, keyed by packed product code. A different subtree
    /// from <see cref="UserDataKey"/> and holding different things: this one is
    /// where <see cref="InstanceTypeValue"/> sits, which the installer's own
    /// UserData subtree does not carry (measured on one machine: 0 instance-named
    /// values across 136 InstallProperties keys, and one on each of 123 products
    /// here).
    /// </summary>
    private const string AdvertisedProductsKey = @"SOFTWARE\Classes\Installer\Products";

    /// <summary>
    /// Zero or absent for an ordinary product, one for a product installed with a
    /// multiple-instance transform. See <see cref="MachineUsesInstanceTransforms"/>.
    /// </summary>
    private const string InstanceTypeValue = "InstanceType";

    /// <summary>
    /// The everyone SID, which the two ENUMERATION entry points accept as "across
    /// all accounts" and the two PROPERTY entry points reject outright. The split
    /// is why the ladder exists: the property reads have to be told which account
    /// to look in, one at a time.
    /// </summary>
    private const string AllUsersSid = "S-1-1-0";

    /// <summary>
    /// The SYSTEM account, whose <c>UserData</c> subkey is where per-machine
    /// registrations are kept. It is therefore always in the account list and is
    /// never a per-user account to ask about; see <see cref="BuildAccountLadder"/>
    /// for what including it costs.
    /// </summary>
    private const string MachineAccountSid = "S-1-5-18";

    /// <summary>Matches <c>InstallerQueryService</c>: long enough that the retry never runs.</summary>
    private const int SidBufferLength = 256;

    /// <summary>
    /// The same budget the two enumeration loops in <c>InstallerQueryService</c>
    /// carry, for the same reason: an enumeration that will not terminate must be
    /// bounded by something, and no product has ten thousand patches.
    /// </summary>
    private const int MaxPatchIndex = 10_000;

    /// <summary>
    /// How often the ticker fires. Time-based rather than every Nth candidate,
    /// because the per-candidate cost varies by two orders of magnitude between a
    /// cache hit and a package that has to be opened and parsed, so a fixed stride
    /// is either silent for minutes or floods the dispatcher.
    /// </summary>
    private static readonly TimeSpan TickerInterval = TimeSpan.FromMilliseconds(100);

    private readonly IPackageIdentityReader _reader;
    private readonly IMsiApi _msi;
    private readonly IRegistryReader _registry;
    private readonly Action<Exception>? _crashLogSink;

    /// <summary>Production constructor. DI supplies all three dependencies.</summary>
    public IdentityVeto(IPackageIdentityReader reader, IMsiApi msi, IRegistryReader registry)
        : this(reader, msi, registry, null) { }

    /// <summary>
    /// Seam constructor. <paramref name="crashLogSink"/> is null in production and
    /// is crash.log; tests pass their own, because driving the unreadable-package
    /// branch is the point of half of them and doing it against the real sink
    /// would append entries to the log of whatever machine ran the suite.
    /// </summary>
    internal IdentityVeto(IPackageIdentityReader reader, IMsiApi msi, IRegistryReader registry,
        Action<Exception>? crashLogSink)
    {
        _reader = reader;
        _msi = msi;
        _registry = registry;
        _crashLogSink = crashLogSink;
    }

    /// <inheritdoc />
    public IdentityPassResult Screen(
        IReadOnlyList<IdentityCandidate> candidates,
        IProgress<ScanProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var outcomes = new CandidateIdentityOutcome[candidates.Count];
        if (candidates.Count == 0)
            return new IdentityPassResult(outcomes, 0, 0);

        // Read once for the pass. A ladder that could not be read is not an empty
        // ladder: it means the app does not know which accounts to ask about, so
        // every candidate that gets as far as needing it is unaskable. Null
        // carries that; an empty list would silently mean "asked everybody".
        var ladder = BuildAccountLadder();

        // Also once for the pass, and it is a property of the MACHINE rather than
        // of any candidate: see CandidateIdentityOutcome.InstanceTransformsInUse
        // for what it means and why a positive reading withholds.
        var instanceTransforms = MachineUsesInstanceTransforms();

        // Budgeted, because the conditions that make one package unreadable are
        // usually properties of the machine rather than of one file: a filter
        // driver refusing CreateFile, or an msi.dll that will not prepare the
        // query, refuses every candidate identically. One full crash-log entry
        // each would then be a self-inflicted denial of the log, which is a
        // failure this project has already measured and fixed elsewhere.
        var failureLog = new PerItemFailureLog("Identity check",
            "Which files these were is recorded nowhere else: a file kept back here is left off "
            + "the list offered for removal and nothing else about it is kept. Fewer files are "
            + "offered, never more.",
            _crashLogSink);

        var productVerdicts = new Dictionary<string, CodeVerdict>(StringComparer.Ordinal);
        var patchVerdicts = new Dictionary<string, CodeVerdict>(StringComparer.Ordinal);
        var patchesByProduct = new Dictionary<string, HashSet<string>?>(StringComparer.Ordinal);
        var asked = 0;
        var cacheHits = 0;

        var ticker = Stopwatch.StartNew();
        var lastReport = TimeSpan.Zero;

        try
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var candidate = candidates[i];

                // The first candidate always reports, so a pass that then stalls
                // on one enormous package has still said it started. After that
                // the interval governs.
                if (i == 0 || ticker.Elapsed - lastReport >= TickerInterval)
                {
                    lastReport = ticker.Elapsed;
                    // The file's own name and no label around it, matching the
                    // per-product ticker the enumeration already emits. It carries
                    // no English, which is what lets this ship before anybody has
                    // written the phase a sentence.
                    progress?.Report(new ScanProgressUpdate(
                        Path.GetFileName(candidate.FullPath), IsMilestone: false));
                }

                var identity = _reader.Read(candidate.FullPath, candidate.IsPatch, out var detail);
                if (identity is null)
                {
                    outcomes[i] = CandidateIdentityOutcome.IdentityUnreadable;
                    // The detail is the cause key as well as the message: every
                    // entry here is a synthesised exception of one type carrying
                    // one HRESULT, so without it the budget's novel-cause escape
                    // would fire once and swallow every other kind.
                    failureLog.Record(
                        new InvalidOperationException(
                            $"Candidate kept back, its own identity could not be read: {detail}."),
                        cause: detail);
                    continue;
                }

                if (ladder is null)
                {
                    outcomes[i] = CandidateIdentityOutcome.RecordsUnaskable;
                    continue;
                }

                var value = identity.Value;
                var cache = value.IsPatch ? patchVerdicts : productVerdicts;
                var key = CacheKey(value);
                if (cache.TryGetValue(key, out var verdict))
                {
                    cacheHits++;
                }
                else
                {
                    verdict = value.IsPatch
                        ? AskAboutPatch(value, ladder, patchesByProduct, cancellationToken)
                        : AskAboutProduct(value.Code, ladder);
                    cache[key] = verdict;
                    asked++;
                }

                // The machine reading touches ONE arm and only ever moves it
                // towards keeping the file. A claim stays a claim and an
                // unanswerable question stays unanswerable; what changes is that
                // the single releasing arm stops releasing. That is what keeps
                // this composable with everything else here under the
                // only-ever-withholds invariant, and it is why it could be added
                // without re-arguing any other source.
                outcomes[i] = verdict switch
                {
                    CodeVerdict.Known => CandidateIdentityOutcome.Claimed,
                    CodeVerdict.NotKnown when instanceTransforms
                        => CandidateIdentityOutcome.InstanceTransformsInUse,
                    CodeVerdict.NotKnown => CandidateIdentityOutcome.Unclaimed,
                    _ => CandidateIdentityOutcome.RecordsUnaskable,
                };
            }
        }
        finally
        {
            // Owed on a cancelled pass too, which leaves through here.
            failureLog.WriteClosingEntry();
        }

        return new IdentityPassResult(outcomes, asked, cacheHits);
    }

    /// <summary>
    /// What one identity is cached under. A product code is its own key. A patch
    /// is keyed on its code AND the targets it named, because the verdict is a
    /// function of both: nothing establishes that two files declaring one patch
    /// code must declare the same target list, and a verdict reached through a
    /// shorter list would be a weaker answer reused as a stronger one.
    /// </summary>
    private static string CacheKey(PackageIdentity identity) =>
        identity.IsPatch
            ? string.Concat(identity.Code, " ", string.Join(';', identity.TargetProductCodes))
            : identity.Code;

    /// <summary>What asking Windows about one identity settled.</summary>
    private enum CodeVerdict
    {
        /// <summary>Every source answered and none holds a record for it.</summary>
        NotKnown,

        /// <summary>Some source holds a record for it.</summary>
        Known,

        /// <summary>A source returned something that is not an answer.</summary>
        Unaskable,
    }

    /// <summary>
    /// Every (account, context) pair worth putting a keyed question to, or null
    /// where the account list itself could not be read.
    ///
    /// The per-machine entry passes no account, which is what the API requires:
    /// a named account with the per-machine context is documented as invalid and
    /// was measured returning ERROR_INVALID_PARAMETER. Each account then gets both
    /// per-user contexts, since which of the two a product was installed into is
    /// not knowable from the account name.
    /// </summary>
    private IReadOnlyList<(string? Sid, MsiInstallContext Context)>? BuildAccountLadder()
    {
        var sids = _registry.LocalMachineSubKeyNames(UserDataKey);
        if (sids is null) return null;

        var ladder = new List<(string?, MsiInstallContext)>(1 + sids.Length * 2)
        {
            (null, MsiInstallContext.Machine),
        };
        foreach (var sid in sids)
        {
            // THE MACHINE ACCOUNT IS NOT AN ACCOUNT FOR THIS PURPOSE, and leaving
            // it in empties the offer on every machine. Its key is where
            // per-machine registrations live, so it is always present in this
            // list, and the per-machine context above already covers everything
            // under it. Asked in a per-USER context it is a question that may not
            // be put: "the special SID string S-1-5-18 (system) cannot be used to
            // enumerate products installed as per-machine", and one machine
            // measured ERROR_INVALID_PARAMETER for it in every context, for both
            // keyed entry points.
            //
            // That code is on no allowlist, so it reads as a question that could
            // not be answered, and this rung is reached the moment the per-machine
            // ask says it does not know the product. Left in, every candidate
            // Windows genuinely does not know is withheld and nothing is ever
            // offered.
            //
            // Only this one SID is dropped, and only from the per-user rungs. Any
            // OTHER account answering outside the documented set is a question
            // that really could not be put, and still withholds.
            if (string.Equals(sid, MachineAccountSid, StringComparison.OrdinalIgnoreCase)) continue;

            ladder.Add((sid, MsiInstallContext.UserUnmanaged));
            ladder.Add((sid, MsiInstallContext.UserManaged));
        }
        return ladder;
    }

    /// <summary>
    /// Whether any product on this machine was installed with an instance
    /// transform, which is what makes a negative identity answer stop meaning that
    /// nothing needs the file.
    ///
    /// <c>InstanceType</c> is documented on <c>MsiGetProductInfoEx</c> as "a value
    /// of one (1) indicates a product installed using a multiple instance
    /// transform and the MSINEWINSTANCE property", with a missing value or zero
    /// meaning an ordinary installation. It is also a plain registry value beside
    /// each product in the advertisement store, which is where it is read from
    /// here: the keyed API form would need a product code to ask about, and the
    /// codes worth asking about are exactly the ones this cannot see.
    ///
    /// A POSITIVE READING IS THE ONLY THING THAT ACTS, and everything else leaves
    /// the scan where it was. The store not opening, a product whose value will
    /// not read, a machine registering its instances somewhere this cannot reach:
    /// all of them answer false, and false is what the app already did. So this is
    /// a trigger that can only ever remove files from the offer, never add one,
    /// and its incompleteness costs nothing that was not already being spent.
    ///
    /// THE INCOMPLETENESS IS REAL AND WORTH NAMING RATHER THAN LEAVING TO BE
    /// DISCOVERED. This store is per-machine, so a per-user instance registration
    /// in a user's own hive is not seen; and the store held 123 products on the one
    /// machine measured where the installer's own UserData subtree held 136, so it
    /// is not a complete list of what is installed even per-machine. It is a
    /// signal that the machine does this at all, not a census of what does it.
    /// </summary>
    private bool MachineUsesInstanceTransforms()
    {
        var products = _registry.LocalMachineSubKeyNames(AdvertisedProductsKey);
        if (products is null) return false;

        foreach (var product in products)
        {
            var read = _registry.LocalMachineDwordValue(
                $@"{AdvertisedProductsKey}\{product}", InstanceTypeValue);

            // Read AND non-zero. Absent is the documented shape of an ordinary
            // product, a wrong type is a machine nothing here anticipated, and an
            // unreadable value established nothing; none of the three is a
            // positive, and reading any of them as one would withhold on every
            // machine whose store is not exactly as expected.
            if (read.State == RegistryDwordState.Read && read.Value != 0) return true;
        }

        return false;
    }

    /// <summary>
    /// Whether any installed or advertised product answers to
    /// <paramref name="productCode"/>.
    ///
    /// The ladder is asked first and the filtered enumeration second. Order is not
    /// arbitrary: the ladder's calls are index-free, so a positive from it is the
    /// strongest answer available, and reaching the enumeration at all means every
    /// keyed call has already said no.
    /// </summary>
    private CodeVerdict AskAboutProduct(
        string productCode,
        IReadOnlyList<(string? Sid, MsiInstallContext Context)> ladder)
    {
        foreach (var (sid, context) in ladder)
        {
            uint length = 0;
            var error = _msi.GetProductInfo(
                productCode: productCode,
                userSid: sid,
                context: context,
                property: MsiInstallProperty.ProductName,
                value: null,
                valueLength: ref length);

            if (IsRecordPresent(error)) return CodeVerdict.Known;
            if (error == MsiError.UnknownProduct) continue;
            return CodeVerdict.Unaskable;
        }

        return EnumerationKnowsProduct(productCode);
    }

    /// <summary>
    /// The enumeration asked about ONE product code, which is a different question
    /// from the enumeration the scan already runs: filtered to a single code and
    /// stopped at the first row, it asks whether any account holds this product
    /// rather than walking the machine's whole list.
    ///
    /// It is the belt to the ladder's braces. It catches a registration the account
    /// walk did not name, an account subkey that would not open being the obvious
    /// way that happens, and it is one call.
    /// </summary>
    private CodeVerdict EnumerationKnowsProduct(string productCode)
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
            installedContext: out _,
            sid: sidBuffer,
            sidLength: ref sidLength);

        if (error == MsiError.MoreData)
        {
            // Defensive only, exactly as in the scan's own enumeration: on
            // MoreData the count excludes the terminator, so the retry size is
            // that count plus one. Real SIDs are far inside the first buffer.
            sidLength++;
            sidBuffer = new char[sidLength];
            error = _msi.EnumProducts(
                productCode: productCode,
                userSid: AllUsersSid,
                context: MsiInstallContext.All,
                index: 0,
                installedProductCode: installedCode,
                installedContext: out _,
                sid: sidBuffer,
                sidLength: ref sidLength);
        }

        return error switch
        {
            MsiError.Success => CodeVerdict.Known,
            MsiError.NoMoreItems => CodeVerdict.NotKnown,
            _ => CodeVerdict.Unaskable,
        };
    }

    /// <summary>
    /// Whether any product still holds the patch <paramref name="identity"/>
    /// declares itself to be.
    ///
    /// A patch cannot be asked about on its own. <c>MsiGetPatchInfoEx</c> takes a
    /// product code as a required parameter, so every question about a patch is a
    /// question about a pairing, and the products to pair it with are the ones the
    /// patch's own Template names. A target that is not installed answers cleanly
    /// (it holds no patches, so it does not hold this one), which is why an
    /// uninstalled target is a negative rather than a failure.
    ///
    /// EVERY TARGET HAS TO ANSWER. One that cannot is enough to make the whole
    /// question unanswerable: the patch may be held by exactly that one.
    /// </summary>
    private CodeVerdict AskAboutPatch(
        PackageIdentity identity,
        IReadOnlyList<(string? Sid, MsiInstallContext Context)> ladder,
        Dictionary<string, HashSet<string>?> patchesByProduct,
        CancellationToken cancellationToken)
    {
        foreach (var target in identity.TargetProductCodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var (sid, context) in ladder)
            {
                uint length = 0;
                var error = _msi.GetPatchInfo(
                    patchCode: identity.Code,
                    productCode: target,
                    userSid: sid,
                    context: context,
                    property: MsiInstallProperty.State,
                    value: null,
                    valueLength: ref length);

                if (IsRecordPresent(error)) return CodeVerdict.Known;
                if (error is MsiError.UnknownPatch or MsiError.UnknownProduct) continue;
                return CodeVerdict.Unaskable;
            }

            var applied = PatchCodesOf(target, patchesByProduct, cancellationToken);
            if (applied is null) return CodeVerdict.Unaskable;
            if (applied.Contains(identity.Code)) return CodeVerdict.Known;
        }

        return CodeVerdict.NotKnown;
    }

    /// <summary>
    /// Every patch code registered against one product, across every account, or
    /// null where the enumeration did not run to a clean end.
    ///
    /// The belt to the keyed pairing reads above, and cached per product because a
    /// folder full of one product's patches would otherwise re-enumerate that
    /// product once per file. Read for vetoing only: a code in this set keeps a
    /// file, and a code absent from it clears nothing on its own, the keyed reads
    /// above having already had to answer.
    /// </summary>
    private HashSet<string>? PatchCodesOf(
        string productCode,
        Dictionary<string, HashSet<string>?> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(productCode, out var cached)) return cached;

        var codes = new HashSet<string>(StringComparer.Ordinal);
        var patchCode = new char[Msi.GuidBufferLength];
        var targetCode = new char[Msi.GuidBufferLength];
        HashSet<string>? result = codes;

        for (uint index = 0; index < MaxPatchIndex; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Array.Clear(patchCode);
            Array.Clear(targetCode);
            uint sidLength = 0;

            var error = _msi.EnumPatches(
                productCode: productCode,
                userSid: AllUsersSid,
                context: MsiInstallContext.All,
                filter: MsiPatchFilter.All,
                index: index,
                patchCode: patchCode,
                targetProductCode: targetCode,
                targetProductContext: out _,
                targetUserSid: null,
                targetUserSidLength: ref sidLength);

            // A product with no patches and a product that is not installed both
            // end the list cleanly, and both mean the same thing here: this
            // product does not hold the patch being asked about.
            if (error is MsiError.NoMoreItems or MsiError.UnknownProduct) break;

            if (error is not (MsiError.Success or MsiError.MoreData))
            {
                // Anything else leaves the set short by an unknown amount, and a
                // short veto set is a veto that does not fire. Null rather than a
                // partial set, so the caller cannot mistake one for the other.
                result = null;
                break;
            }

            var code = BufferToString(patchCode);
            if (code.Length > 0) codes.Add(code);

            if (index == MaxPatchIndex - 1)
            {
                // Ran out of budget rather than reaching the end, so the set is
                // short for the same reason and answers the same way.
                result = null;
            }
        }

        cache[productCode] = result;
        return result;
    }

    /// <summary>
    /// The returns that establish a record IS there, as an ALLOWLIST and for the
    /// reason every other allowlist in this codebase is one: one machine can show
    /// which codes mean a record was found, and no machine can enumerate every
    /// code that exists, so anything unlisted has to fall to the side that
    /// withholds.
    ///
    /// ERROR_UNKNOWN_PROPERTY is on it, and that is the entry worth explaining. It
    /// means the record was reached and does not carry the property asked for,
    /// which is a positive statement that the record exists. It is also the exact
    /// code another account's per-user product returns for most properties, so
    /// reading it as an absence would put that product's cached file on the list.
    /// </summary>
    private static bool IsRecordPresent(uint error) =>
        error is MsiError.Success or MsiError.MoreData or MsiError.UnknownProperty;

    /// <summary>
    /// A fixed-size MSI out-buffer as a string, trimmed at the first null. Matches
    /// <c>InstallerQueryService</c>: these buffers carry no count parameter, so the
    /// terminator is the only length there is.
    /// </summary>
    private static string BufferToString(char[] buffer)
    {
        var length = Array.IndexOf(buffer, '\0');
        return length < 0 ? new string(buffer) : new string(buffer, 0, length);
    }
}
