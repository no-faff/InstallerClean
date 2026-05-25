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

    /// <inheritdoc />
    public Task<IReadOnlyList<RegisteredPackage>> GetRegisteredPackagesAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GetRegisteredPackagesCore(progress, cancellationToken), cancellationToken);
    }

    private IReadOnlyList<RegisteredPackage> GetRegisteredPackagesCore(
        IProgress<string>? progress,
        CancellationToken ct)
    {
        // TryAdd on this dictionary means the API enumeration wins over the
        // registry fallback when both report the same path, because the
        // API entry carries product metadata the fallback lacks.
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);

        progress?.Report(Strings.Status_EnumeratingProducts);

        var products = EnumerateProducts(ct);

        progress?.Report(string.Format(Strings.Status_FoundProducts,
            products.Count, Helpers.DisplayHelpers.PluraliseProduct(products.Count)));

        foreach (var (productCode, userSid, context) in products)
        {
            ct.ThrowIfCancellationRequested();

            var productName = GetProductProperty(productCode, userSid, context, MsiInstallProperty.ProductName);
            var localPackage = GetProductProperty(productCode, userSid, context, MsiInstallProperty.LocalPackage);

            if (!string.IsNullOrEmpty(localPackage))
            {
                progress?.Report(productName.Length > 0 ? productName : productCode);
                claimed.TryAdd(localPackage, new RegisteredPackage(localPackage, productName, productCode));
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

                    int.TryParse(stateStr, out var patchState);
                    var isSuperseded = patchState is 2 or 4;
                    var isUninstallable = uninstallableStr == "1";
                    var isRemovable = isSuperseded && !isUninstallable;

                    claimed.TryAdd(patchPath, new RegisteredPackage(patchPath, productName, productCode, patchState, isRemovable));
                }
            }
        }

        progress?.Report(Strings.Status_CheckingRegistry);
        try
        {
            // Registry64 is pinned explicitly. Registry.LocalMachine
            // resolves to the process-bitness view, which redirects to
            // WOW6432Node under an x86 process and silently misses
            // installer-cache entries written by 64-bit installers.
            // Pinning to Registry64 keeps the fallback path correct
            // regardless of host bitness.
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

                    using var productsKey = udKey.OpenSubKey($@"{sidName}\Products");
                    if (productsKey is not null)
                    {
                        foreach (var prodGuid in productsKey.GetSubKeyNames())
                        {
                            using var ipKey = productsKey.OpenSubKey($@"{prodGuid}\InstallProperties");
                            var localPkg = ipKey?.GetValue("LocalPackage") as string;
                            if (!string.IsNullOrEmpty(localPkg))
                                claimed.TryAdd(localPkg, new RegisteredPackage(localPkg, "", ""));
                        }
                    }

                    using var patchesKey = udKey.OpenSubKey($@"{sidName}\Patches");
                    if (patchesKey is not null)
                    {
                        foreach (var patchGuid in patchesKey.GetSubKeyNames())
                        {
                            using var patchKey = patchesKey.OpenSubKey(patchGuid);
                            var localPkg = patchKey?.GetValue("LocalPackage") as string;
                            if (!string.IsNullOrEmpty(localPkg))
                                claimed.TryAdd(localPkg, new RegisteredPackage(localPkg, "", ""));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Best effort. The crash log preserves a diagnostic trail
            // for reports of missing registered products.
            Helpers.CrashLog.Write(ex);
        }

        // Even a fresh Windows install has OS-level MSI products. Zero
        // here means the database is corrupt or inaccessible; silently
        // reporting "all clear" would be worse than failing.
        if (claimed.Count == 0)
            throw new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty);

        progress?.Report(string.Format(Strings.Status_RegistryScanComplete,
            claimed.Count, Helpers.DisplayHelpers.PluralisePackage(claimed.Count)));

        return claimed.Values.ToList().AsReadOnly();
    }

    private const int MaxProductIndex = 10_000;
    private const int MaxConsecutiveNonSuccess = 20;

    private static List<(string ProductCode, string? UserSid, MsiInstallContext Context)> EnumerateProducts(
        CancellationToken ct)
    {
        var results = new List<(string, string?, MsiInstallContext)>();
        var productCode = new char[Msi.GuidBufferLength];
        var sidBuffer = new char[SidBufferLength];
        int consecutiveNonSuccess = 0;

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

            var error = Msi.MsiEnumProductsEx(
                szProductCode: null,
                szUserSid: AllUsersSid,
                dwContext: MsiInstallContext.All,
                dwIndex: index,
                szInstalledProductCode: productCode,
                pdwInstalledContext: out var installedContext,
                szSid: sidBuffer,
                pcchSid: ref sidLen);

            if (error == MsiError.NoMoreItems)
                break;

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

                error = Msi.MsiEnumProductsEx(
                    szProductCode: null,
                    szUserSid: AllUsersSid,
                    dwContext: MsiInstallContext.All,
                    dwIndex: index,
                    szInstalledProductCode: productCode,
                    pdwInstalledContext: out installedContext,
                    szSid: sidBuffer,
                    pcchSid: ref sidLen);
            }

            if (error == MsiError.Success)
            {
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
                results.Add((BufferToString(productCode), sid, installedContext));
            }
            else
            {
                consecutiveNonSuccess++;
                if (consecutiveNonSuccess >= MaxConsecutiveNonSuccess)
                    throw new LocalisedInvalidOperationException(
                        string.Format(Strings.Error_MsiNonSuccess, consecutiveNonSuccess, error));
            }
        }

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

    private static List<(string PatchCode, string? UserSid, MsiInstallContext Context)> EnumeratePatches(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        CancellationToken ct)
    {
        var results = new List<(string, string?, MsiInstallContext)>();
        var patchCode = new char[Msi.GuidBufferLength];
        var targetProductCode = new char[Msi.GuidBufferLength];
        int consecutiveNonSuccess = 0;

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

            var error = Msi.MsiEnumPatchesEx(
                szProductCode: productCode,
                szUserSid: userSid,
                dwContext: context,
                dwFilter: MsiPatchFilter.All,
                dwIndex: index,
                szPatchCode: patchCode,
                szTargetProductCode: targetProductCode,
                pdwTargetProductContext: out var patchContext,
                szTargetUserSid: null,
                pcchTargetUserSid: ref sidLen);

            if (error == MsiError.NoMoreItems)
                break;

            if (error == MsiError.AccessDenied)
                break; // skip patches the API refuses to enumerate

            if (error == MsiError.Success || error == MsiError.MoreData)
            {
                consecutiveNonSuccess = 0;
                results.Add((BufferToString(patchCode), userSid, patchContext));
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
                        string.Format(Strings.Error_MsiNonSuccess, consecutiveNonSuccess, error));
            }
        }

        return results;
    }

    /// <summary>
    /// Retrieves a product property using the double-call buffer
    /// pattern. Returns an empty string if the property cannot be
    /// read.
    /// </summary>
    private static string GetProductProperty(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string propertyName)
    {
        uint bufferLen = 0;

        var error = Msi.MsiGetProductInfoEx(
            szProductCode: productCode,
            szUserSid: userSid,
            dwContext: context,
            szProperty: propertyName,
            szValue: null,
            pcchValue: ref bufferLen);

        if (error != MsiError.Success && error != MsiError.MoreData)
            return string.Empty;

        if (bufferLen == 0)
            return string.Empty;

        bufferLen++; // space for null terminator
        var buffer = new char[bufferLen];

        error = Msi.MsiGetProductInfoEx(
            szProductCode: productCode,
            szUserSid: userSid,
            dwContext: context,
            szProperty: propertyName,
            szValue: buffer,
            pcchValue: ref bufferLen);

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
    private static string GetPatchProperty(
        string patchCode,
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string propertyName)
    {
        uint bufferLen = 0;

        var error = Msi.MsiGetPatchInfoEx(
            szPatchCode: patchCode,
            szProductCode: productCode,
            szUserSid: userSid,
            dwContext: context,
            szProperty: propertyName,
            szValue: null,
            pcchValue: ref bufferLen);

        if (error != MsiError.Success && error != MsiError.MoreData)
            return string.Empty;

        if (bufferLen == 0)
            return string.Empty;

        bufferLen++; // space for null terminator
        var buffer = new char[bufferLen];

        error = Msi.MsiGetPatchInfoEx(
            szPatchCode: patchCode,
            szProductCode: productCode,
            szUserSid: userSid,
            dwContext: context,
            szProperty: propertyName,
            szValue: buffer,
            pcchValue: ref bufferLen);

        // Defensive clamp: a successful Msi*GetInfoEx returns
        // bufferLen as the count excluding the terminator and never
        // larger than the input. Math.Min bounds an unbounded read
        // even if the API ever violates that contract.
        return error == MsiError.Success
            ? new string(buffer, 0, (int)Math.Min(bufferLen, (uint)buffer.Length))
            : string.Empty;
    }
}
