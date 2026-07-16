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
    public InstallerQueryService(IMsiApi msi) => _msi = msi;

    /// <inheritdoc />
    public Task<IReadOnlyList<RegisteredPackage>> GetRegisteredPackagesAsync(
        IProgress<ScanProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GetRegisteredPackagesCore(progress, cancellationToken), cancellationToken);
    }

    private IReadOnlyList<RegisteredPackage> GetRegisteredPackagesCore(
        IProgress<ScanProgressUpdate>? progress,
        CancellationToken ct)
    {
        // One entry per LocalPackage path. Every insertion goes through
        // MergeClaim, which carries the whole policy for what a second claim on
        // an already-claimed path does.
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);

        progress?.Report(new ScanProgressUpdate(Strings.Status_EnumeratingProducts));

        var products = EnumerateProducts(ct);

        progress?.Report(new ScanProgressUpdate(Strings.Status_FoundProducts));

        foreach (var (productCode, userSid, context) in products)
        {
            ct.ThrowIfCancellationRequested();

            var productName = GetProductProperty(productCode, userSid, context, MsiInstallProperty.ProductName);
            var localPackage = GetProductProperty(productCode, userSid, context, MsiInstallProperty.LocalPackage);

            if (!string.IsNullOrEmpty(localPackage))
            {
                // Ticker, not milestone: one of these fires per product,
                // up to hundreds in a few seconds, so the consumer must
                // not feed it to a screen-reader live region.
                progress?.Report(new ScanProgressUpdate(
                    productName.Length > 0 ? productName : productCode, IsMilestone: false));
                MergeClaim(claimed,
                    new RegisteredPackage(localPackage, productName, productCode),
                    ClaimSource.InstallerApi);
            }

            var patches = EnumeratePatches(productCode, userSid, context, ct);

            foreach (var (patchCode, patchUserSid, patchContext) in patches)
            {
                ct.ThrowIfCancellationRequested();

                var patchPath = GetPatchProperty(patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.LocalPackage);

                if (!string.IsNullOrEmpty(patchPath))
                {
                    var stateStr = GetPatchProperty(patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.State);
                    var uninstallableStr = GetPatchProperty(patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.Uninstallable);

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
                        new RegisteredPackage(patchPath, productName, productCode, patchState, isRemovable),
                        ClaimSource.InstallerApi);
                }
            }
        }

        progress?.Report(new ScanProgressUpdate(Strings.Status_CheckingRegistry));

        // Registry64 is pinned explicitly. Registry.LocalMachine resolves to
        // the process-bitness view, which redirects to WOW6432Node under an
        // x86 process and silently misses installer-cache entries written by
        // 64-bit installers. Pinning to Registry64 keeps the fallback path
        // correct regardless of host bitness.
        //
        // The per-SID and per-key try/catch below is deliberate and must not
        // be collapsed back into one outer try: this fallback is the second of
        // the app's two independent "still needed" sources, and a single try
        // spanning every SID once let one corrupt subkey or unreadable DACL
        // abandon the entire remaining fallback, turning every registration
        // only it would have contributed into an orphan candidate. Scoping the
        // catch to each key read costs one entry per bad key, never the net.
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
                    ReadFallbackSid(udKey, sidName, claimed, ct);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Last resort: a failure opening UserData itself or enumerating
            // its SID names (the per-SID reads have their own catches below).
            // The crash log preserves a diagnostic trail for reports of
            // missing registered products. Cancellation is excluded:
            // ThrowIfCancellationRequested fires inside this try, so a plain
            // catch would log the user's own Cancel as a fault and swallow the
            // stop the caller is waiting on.
            Helpers.CrashLog.Write(ex);
        }

        // Even a fresh Windows install has OS-level MSI products. Zero
        // here means the database is corrupt or inaccessible; silently
        // reporting "all clear" would be worse than failing.
        if (claimed.Count == 0)
            throw new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty);

        progress?.Report(new ScanProgressUpdate(string.Format(
            Helpers.DisplayHelpers.Pluralise(claimed.Count, Strings.Status_RegisteredPackagesFound, "Status.RegisteredPackagesFound"),
            claimed.Count, Helpers.DisplayHelpers.PluralisePackage(claimed.Count))));

        return claimed.Values.ToList().AsReadOnly();
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
    /// Reads one SID subtree's Products and Patches keys into the fallback set.
    /// Each key read is independently guarded so one corrupt entry costs only
    /// itself; see the try/catch rationale at the call site. Cancellation is
    /// re-thrown, never swallowed.
    /// </summary>
    private static void ReadFallbackSid(
        Microsoft.Win32.RegistryKey udKey,
        string sidName,
        Dictionary<string, RegisteredPackage> claimed,
        CancellationToken ct)
    {
        try
        {
            using var productsKey = udKey.OpenSubKey($@"{sidName}\Products");
            if (productsKey is not null)
            {
                foreach (var prodGuid in productsKey.GetSubKeyNames())
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        using var ipKey = productsKey.OpenSubKey($@"{prodGuid}\InstallProperties");
                        var localPkg = ipKey?.GetValue("LocalPackage") as string;
                        if (!string.IsNullOrEmpty(localPkg))
                            MergeClaim(claimed, new RegisteredPackage(localPkg, "", ""),
                                ClaimSource.RegistryFallback);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        Helpers.CrashLog.Write(ex);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Helpers.CrashLog.Write(ex);
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
                            MergeClaim(claimed, new RegisteredPackage(localPkg, "", ""),
                                ClaimSource.RegistryFallback);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        Helpers.CrashLog.Write(ex);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Helpers.CrashLog.Write(ex);
        }
    }

    private const int MaxProductIndex = 10_000;
    private const int MaxConsecutiveNonSuccess = 20;

    private List<(string ProductCode, string? UserSid, MsiInstallContext Context)> EnumerateProducts(
        CancellationToken ct)
    {
        var results = new List<(string, string?, MsiInstallContext)>();
        var productCode = new char[Msi.GuidBufferLength];
        var sidBuffer = new char[SidBufferLength];
        int consecutiveNonSuccess = 0;
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

            if (error == MsiError.NoMoreItems)
            {
                reachedEnd = true;
                break;
            }

            if (error == MsiError.AccessDenied)
                throw new LocalisedAccessException(Strings.Error_MsiAccessDenied);

            if (error == MsiError.MoreData)
            {
                // Defensive only. Real-world SIDs are ~45 chars and
                // the first call passes a 256-char buffer, so this
                // branch isn't exercised in normal use. On MoreData
                // pcchSid carries the size required INCLUDING the
                // terminator; the retry allocates exactly that size and
                // passes the same value back as the new buffer size.
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
                // "product unreadable", and ERROR_BAD_CONFIGURATION residue is
                // common on exactly the machines this app serves, so a
                // per-product throw would brick the scan for the people who
                // most need it. The protection is layered elsewhere: the
                // registry fallback covers the paths a scattered failure loses,
                // and FileSystemScanService's correlation gate refuses the scan
                // if the correlation as a whole has collapsed. Only a long RUN
                // of consecutive failures (a wholesale enumeration collapse)
                // throws. Do not "fix" this into a per-product hard failure.
                consecutiveNonSuccess++;
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
        if (!reachedEnd)
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_MsiNonSuccess, MaxProductIndex, lastError));

        return results;
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

    private List<(string PatchCode, string? UserSid, MsiInstallContext Context)> EnumeratePatches(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        CancellationToken ct)
    {
        var results = new List<(string, string?, MsiInstallContext)>();
        var patchCode = new char[Msi.GuidBufferLength];
        var targetProductCode = new char[Msi.GuidBufferLength];
        int consecutiveNonSuccess = 0;
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
                // not on the verdict. Breaking here silently yielded zero
                // patches for this product, and its cached .msp files were then
                // presented as orphaned. The scan command's catch routes this
                // to a dialog and to crash.log.
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
                    if (consecutiveNonSuccess >= MaxConsecutiveNonSuccess)
                        throw new LocalisedInvalidOperationException(
                            string.Format(Strings.Error_MsiPatchNonSuccess, consecutiveNonSuccess, error));
                    continue;
                }

                consecutiveNonSuccess = 0;
                results.Add((code, userSid, patchContext));
            }
            else
            {
                consecutiveNonSuccess++;
                // Match EnumerateProducts: throw rather than silently
                // truncate. A patch enumeration that returns a few real
                // entries then collapses to non-success would otherwise
                // leave real-but-superseded patches missing from the
                // result set, classifying them as orphaned and offering
                // them for cleanup. Throwing surfaces the API failure
                // to the caller (the scan command's catch routes it
                // to a dialog and to crash.log).
                if (consecutiveNonSuccess >= MaxConsecutiveNonSuccess)
                    throw new LocalisedInvalidOperationException(
                        string.Format(Strings.Error_MsiPatchNonSuccess, consecutiveNonSuccess, error));
            }
        }

        // See EnumerateProducts: hitting the cap is an unterminated
        // enumeration, not a clean end. Fail loudly rather than truncate.
        if (!reachedEnd)
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_MsiPatchNonSuccess, MaxPatchIndex, lastError));

        return results;
    }

    /// <summary>
    /// Retrieves a product property using the double-call buffer
    /// pattern. Returns an empty string if the property cannot be
    /// read.
    /// </summary>
    private string GetProductProperty(
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
            return string.Empty;

        if (bufferLen == 0)
            return string.Empty;

        bufferLen++; // space for null terminator
        var buffer = new char[bufferLen];

        error = _msi.GetProductInfo(
            productCode: productCode,
            userSid: userSid,
            context: context,
            property: propertyName,
            value: buffer,
            valueLength: ref bufferLen);

        // Defensive clamp: a successful Msi*GetInfoEx returns
        // bufferLen as the count excluding the terminator and never
        // larger than the input. Math.Min bounds an unbounded read
        // even if the API ever violates that contract.
        return error == MsiError.Success
            ? new string(buffer, 0, (int)Math.Min(bufferLen, (uint)buffer.Length))
            : string.Empty;
    }

    /// <summary>
    /// Retrieves a patch property using the double-call buffer
    /// pattern. Returns an empty string if the property cannot be
    /// read.
    /// </summary>
    private string GetPatchProperty(
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
            return string.Empty;

        if (bufferLen == 0)
            return string.Empty;

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

        // Defensive clamp: a successful Msi*GetInfoEx returns
        // bufferLen as the count excluding the terminator and never
        // larger than the input. Math.Min bounds an unbounded read
        // even if the API ever violates that contract.
        return error == MsiError.Success
            ? new string(buffer, 0, (int)Math.Min(bufferLen, (uint)buffer.Length))
            : string.Empty;
    }
}
