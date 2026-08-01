using InstallerClean.Interop;
using InstallerClean.Interop.Native;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Services;

/// <summary>
/// Queries the Windows Installer API to build the complete set of registered
/// .msi and .msp files across all installation contexts. This service only
/// talks to the MSI API. It does not touch the filesystem.
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
    internal readonly record struct FallbackRead(int Failures, int ProductKeys);

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
    internal InstallerQueryService(IMsiApi msi, FallbackReader readFallback,
        Action<Exception>? crashLogSink = null)
    {
        _msi = msi;
        _readFallback = readFallback;
        _crashLogSink = crashLogSink;
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
            + "told through the scan summary that some programs could not be read, and that "
            + "notice names none of them.",
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

                var patchPath = GetPatchProperty(patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.LocalPackage);

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
                    var stateStr = GetPatchProperty(patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.State).Value;
                    var uninstallableStr = GetPatchProperty(patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.Uninstallable).Value;

                    // Unparseable State leaves patchState at 0 (not-a-patch),
                    // so isSuperseded is false and the row is kept: the zero
                    // default is the safe direction on purpose, not luck. Only
                    // a positively read Superseded (2) or Obsoleted (4) makes a
                    // patch a removal candidate.
                    int.TryParse(stateStr, out var patchState);
                    var isSuperseded = patchState is 2 or 4;
                    // Fail safe on Uninstallable: only a positively read "0"
                    // (the patch cannot be uninstalled, so its cached .msp is
                    // dead weight) makes it removable. An unreadable value ("")
                    // must NOT lean removable, because a still-uninstallable
                    // superseded patch needs its .msp to roll back.
                    var isUninstallable = uninstallableStr != "0";
                    var isRemovable = isSuperseded && !isUninstallable;

                    MergeClaim(claimed,
                        new RegisteredPackage(NormaliseLocalPackagePath(patchPath.Value), productName, productCode, patchState, isRemovable),
                        ClaimSource.InstallerApi);
                }
            }

            if (recordsShort) unreadableProducts++;
        }

        progress?.Report(new ScanProgressUpdate(Strings.Status_CheckingRegistry));

        var fallback = _readFallback(claimed, ct);

        // Even a fresh Windows install has OS-level MSI products. Zero
        // here means the database is corrupt or inaccessible; silently
        // reporting "all clear" would be worse than failing.
        if (claimed.Count == 0)
            throw new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty);

        // Both sources degraded at once: refuse the scan outright rather than
        // report a shorter one.
        //
        // Withholding the removable class answers a short API enumeration on its
        // own, because the paths the lost product would have claimed are still
        // reachable: the fallback reads the same UserData keys and contributes
        // them as non-removable rows, so the file stays out of the orphan list
        // even though its owner went missing from the API's answer. That is the
        // whole reason the withholding can say orphan detection is unaffected.
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
        // scan that believes it is whole. Nothing inside the API can see it.
        //
        // The registry walk is the only independent count of how many products
        // this machine has, so a shortfall against it is the signal. It is an
        // inference and not a measurement, which is what the tolerance is for:
        // UserData product keys survive a failed uninstall, so the registry
        // legitimately runs ahead on a healthy machine, and a fifth of a
        // machine's registrations being residue is not the ordinary case where
        // losing a fifth of them to a truncation is one event. Two products'
        // difference is absorbed outright, so a machine with a handful of
        // registrations cannot trip on one stale key.
        //
        // A small truncation stays invisible and it is not made stricter to
        // chase one: firing costs a user their superseded-patch cleanup.
        var productShortfall = fallback.ProductKeys - products.Count;
        var enumerationLooksShort =
            productShortfall > 2 && products.Count * 5 < fallback.ProductKeys * 4;

        // The two counts overlap by exactly unreadableRows and the number the
        // user reads is programs, so the overlap is subtracted rather than
        // added twice. A row the enumeration skipped is already one product in
        // unreadableProducts, and it is missing from products.Count as well, so
        // it is inside productShortfall too: 30 skipped rows out of 100
        // registrations reported as 60 programs unread. Only the shortfall
        // BEYOND the rows the enumeration owned up to is new information.
        var withheldProducts = unreadableProducts
            + (enumerationLooksShort ? Math.Max(0, productShortfall - unreadableRows) : 0);

        progress?.Report(new ScanProgressUpdate(string.Format(
            Helpers.DisplayHelpers.Pluralise(claimed.Count, Strings.Status_RegisteredPackagesFound, "Status.RegisteredPackagesFound"),
            claimed.Count, Helpers.DisplayHelpers.PluralisePackage(claimed.Count))));

        var packages = claimed.Values.ToList();

        // A scan that lost any claim withholds the whole removable class.
        // "Removable" asserts that NO installed product still needs the file, and
        // a product set known to be short of at least one claim cannot support
        // that assertion for any patch on the machine: the product behind the
        // loss is exactly the one whose "I still have this applied" claim never
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

        return new InstallerQueryResult(packages.AsReadOnly(), withheldProducts);
        }
        finally
        {
            abandonedLog.WriteClosingEntry();
        }
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
    /// prefix are all such spellings, and all of them survive into the registry
    /// because nothing writing there is obliged to canonicalise. GetFullPath
    /// settles every one; the prefix is stripped first because GetFullPath
    /// deliberately leaves a <c>\\?\</c> path alone, that being the point of the
    /// prefix.
    ///
    /// This is also the string a removable candidate is later moved or deleted
    /// by (FileSystemScanService builds the candidate straight off it), which is
    /// the right direction: the normalised form names the same file and names it
    /// the way the rest of the app spells it.
    ///
    /// 8.3 short names are the one spelling GetFullPath does not settle, and they
    /// are deliberately not handled, on the strength of where these paths come
    /// from rather than on a cost argument. Windows Installer names the files it
    /// caches itself, as short hex (<c>9f05cba.msi</c>, <c>1e4a2f.msp</c>), so a
    /// path this method receives is 8.3-clean by construction and its short and
    /// long forms cannot differ. The hazard case, a registration holding
    /// <c>ABCDEF~1.MSI</c> against a walk that produced the long name, needs a
    /// long-named file in the cache root, and a long-named file in the cache root
    /// is one Windows Installer did not put there, so nothing registered names
    /// it and orphaned is the correct verdict for it.
    ///
    /// Should that ever be revisited, the expansion needs
    /// GetFinalPathNameByHandle per registered path, an open handle per
    /// registration on every scan. The cheap form is a lexical <c>~</c>-plus-digit
    /// pre-filter deciding which registrations are worth a handle, so the
    /// per-path cost is paid only where the hazard could exist, which on a
    /// healthy machine is nowhere.
    /// </summary>
    private static string NormaliseLocalPackagePath(string value)
    {
        try
        {
            return Path.GetFullPath(InstallerCacheHelpers.StripLongPathPrefix(value));
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
    internal static void MergeClaim(
        Dictionary<string, RegisteredPackage> claimed,
        RegisteredPackage candidate,
        ClaimSource source)
    {
        if (source == ClaimSource.RegistryFallback)
        {
            claimed.TryAdd(candidate.LocalPackagePath, candidate);
            return;
        }

        if (!claimed.TryGetValue(candidate.LocalPackagePath, out var existing))
        {
            claimed[candidate.LocalPackagePath] = candidate;
            return;
        }

        // Downgrade only: a removable row loses to a later non-removable claim,
        // and nothing else moves.
        if (existing.IsRemovable && !candidate.IsRemovable)
            claimed[candidate.LocalPackagePath] = candidate;
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
                    var sidRead = ReadFallbackSid(udKey, sidName, claimed, ct, failureLog);
                    failures += sidRead.Failures;
                    productKeys += sidRead.ProductKeys;
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

        return new FallbackRead(failures, productKeys);
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
    /// </summary>
    private static FallbackRead ReadFallbackSid(
        Microsoft.Win32.RegistryKey udKey,
        string sidName,
        Dictionary<string, RegisteredPackage> claimed,
        CancellationToken ct,
        PerItemFailureLog failureLog)
    {
        var failures = 0;
        var productKeys = 0;

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
                    try
                    {
                        using var ipKey = productsKey.OpenSubKey($@"{prodGuid}\InstallProperties");
                        var localPkg = ipKey?.GetValue("LocalPackage") as string;
                        if (!string.IsNullOrEmpty(localPkg))
                            MergeClaim(claimed,
                                new RegisteredPackage(NormaliseLocalPackagePath(localPkg), "", ""),
                                ClaimSource.RegistryFallback);
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
                        var localPkg = patchKey?.GetValue("LocalPackage") as string;
                        if (!string.IsNullOrEmpty(localPkg))
                            MergeClaim(claimed,
                                new RegisteredPackage(NormaliseLocalPackagePath(localPkg), "", ""),
                                ClaimSource.RegistryFallback);
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

        return new FallbackRead(failures, productKeys);
    }

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
    /// localised and never surfaced: the user is told a program could not be read
    /// through the scan summary's kept-patches notice, which carries no product
    /// identity, whereas diagnosing WHY the withholding fired needs exactly that
    /// identity. Without this line the abandonment leaves no record of which product
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
    /// </summary>
    private readonly record struct PropertyRead(string Value, bool Unreadable);

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
            return new PropertyRead(string.Empty, Unreadable: !IsBenignPropertyRead(error));

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
    private PropertyRead GetPatchProperty(
        string patchCode,
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string propertyName)
    {
        uint bufferLen = 0;

        var error = _msi.GetPatchInfo(
            patchCode: patchCode,
            productCode: productCode,
            userSid: userSid,
            context: context,
            property: propertyName,
            value: null,
            valueLength: ref bufferLen);

        if (error != MsiError.Success && error != MsiError.MoreData)
            return new PropertyRead(string.Empty, Unreadable: !IsBenignPropertyRead(error));

        if (bufferLen == 0)
            return new PropertyRead(string.Empty, Unreadable: false);

        bufferLen++; // space for null terminator
        var buffer = new char[bufferLen];

        error = _msi.GetPatchInfo(
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
