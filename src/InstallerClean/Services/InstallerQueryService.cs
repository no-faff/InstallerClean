using System.Text;
using InstallerClean.Interop;
using InstallerClean.Models;

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
    /// A GUID is 38 chars ({xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}) plus a
    /// null terminator. We allocate 39 to be safe.
    /// </summary>
    private const int GuidBufferLength = 39;

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

        progress?.Report("Enumerating installed products...");

        var products = EnumerateProducts(ct);

        progress?.Report($"Found {products.Count} installed product(s). Scanning local packages...");

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

        progress?.Report("Checking registry for additional packages...");
        try
        {
            using var udKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
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
            // Best effort; logged so a user report about missing products
            // has a diagnostic trail.
            Helpers.CrashLog.Write(ex);
        }

        // Even a fresh Windows install has OS-level MSI products. Zero
        // here means the database is corrupt or inaccessible; silently
        // reporting "all clear" would be worse than failing.
        if (claimed.Count == 0)
            throw new InvalidOperationException(
                "The Windows Installer database appears to be empty or inaccessible. " +
                "This is unusual even on a fresh Windows install and typically means " +
                "the database is corrupt or a third-party tool has cleared it. " +
                "Running 'sfc /scannow' from an elevated prompt usually repairs it.");

        progress?.Report($"Scan complete. {claimed.Count} registered package(s) found.");

        return claimed.Values.ToList().AsReadOnly();
    }

    private const int MaxProductIndex = 10_000;
    private const int MaxConsecutiveNonSuccess = 20;

    private static List<(string ProductCode, string? UserSid, MsiInstallContext Context)> EnumerateProducts(
        CancellationToken ct)
    {
        var results = new List<(string, string?, MsiInstallContext)>();
        var productCode = new StringBuilder(GuidBufferLength);
        var sidBuffer = new StringBuilder(SidBufferLength);
        int consecutiveNonSuccess = 0;

        for (uint index = 0; index < MaxProductIndex; index++)
        {
            ct.ThrowIfCancellationRequested();

            productCode.Clear();
            sidBuffer.Clear();
            uint sidLen = (uint)(SidBufferLength - 1);

            var error = MsiNativeMethods.MsiEnumProductsEx(
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
                throw new UnauthorizedAccessException(
                    "Access denied enumerating installed products. Run as administrator.");

            if (error == MsiError.MoreData)
            {
                // Only the SID dimension varies in this double-call pattern;
                // productCode fits in the fixed GuidBufferLength.
                sidLen++; // null terminator
                sidBuffer.Clear();
                sidBuffer.EnsureCapacity((int)sidLen);

                error = MsiNativeMethods.MsiEnumProductsEx(
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
                var sid = (installedContext != MsiInstallContext.Machine && sidLen > 0)
                    ? sidBuffer.ToString()
                    : null;
                results.Add((productCode.ToString(), sid, installedContext));
            }
            else
            {
                consecutiveNonSuccess++;
                if (consecutiveNonSuccess >= MaxConsecutiveNonSuccess)
                    throw new InvalidOperationException(
                        $"Windows Installer API returned {consecutiveNonSuccess} consecutive non-Success responses (last error {error}). Unable to enumerate products.");
            }
        }

        return results;
    }

    private const int MaxPatchIndex = 10_000;

    private static List<(string PatchCode, string? UserSid, MsiInstallContext Context)> EnumeratePatches(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        CancellationToken ct)
    {
        var results = new List<(string, string?, MsiInstallContext)>();
        var patchCode = new StringBuilder(GuidBufferLength);
        var targetProductCode = new StringBuilder(GuidBufferLength);
        int consecutiveNonSuccess = 0;

        for (uint index = 0; index < MaxPatchIndex; index++)
        {
            ct.ThrowIfCancellationRequested();

            patchCode.Clear();
            targetProductCode.Clear();
            uint sidLen = 0;

            var error = MsiNativeMethods.MsiEnumPatchesEx(
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
                break; // skip patches we can't access

            if (error == MsiError.Success || error == MsiError.MoreData)
            {
                consecutiveNonSuccess = 0;
                results.Add((patchCode.ToString(), userSid, patchContext));
            }
            else
            {
                consecutiveNonSuccess++;
                if (consecutiveNonSuccess >= MaxConsecutiveNonSuccess)
                    break;
            }
        }

        return results;
    }

    /// <summary>
    /// Retrieves a product property using the double-call buffer pattern.
    /// Returns an empty string if the property cannot be read.
    /// </summary>
    private static string GetProductProperty(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string propertyName)
    {
        uint bufferLen = 0;

        var error = MsiNativeMethods.MsiGetProductInfoEx(
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
        var buffer = new StringBuilder((int)bufferLen);

        error = MsiNativeMethods.MsiGetProductInfoEx(
            szProductCode: productCode,
            szUserSid: userSid,
            dwContext: context,
            szProperty: propertyName,
            szValue: buffer,
            pcchValue: ref bufferLen);

        return error == MsiError.Success ? buffer.ToString() : string.Empty;
    }

    /// <summary>
    /// Retrieves a patch property using the double-call buffer pattern.
    /// Returns an empty string if the property cannot be read.
    /// </summary>
    private static string GetPatchProperty(
        string patchCode,
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string propertyName)
    {
        uint bufferLen = 0;

        var error = MsiNativeMethods.MsiGetPatchInfoEx(
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
        var buffer = new StringBuilder((int)bufferLen);

        error = MsiNativeMethods.MsiGetPatchInfoEx(
            szPatchCode: patchCode,
            szProductCode: productCode,
            szUserSid: userSid,
            dwContext: context,
            szProperty: propertyName,
            szValue: buffer,
            pcchValue: ref bufferLen);

        return error == MsiError.Success ? buffer.ToString() : string.Empty;
    }
}
