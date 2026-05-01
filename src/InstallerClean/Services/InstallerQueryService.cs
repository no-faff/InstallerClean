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

        progress?.Report(Strings.Status_EnumeratingProducts);

        var products = EnumerateProducts(ct);

        progress?.Report(string.Format(Strings.Status_FoundProducts, products.Count));

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
            throw new InvalidOperationException(Strings.Error_InstallerDbEmpty);

        progress?.Report(string.Format(Strings.Status_RegistryScanComplete, claimed.Count));

        return claimed.Values.ToList().AsReadOnly();
    }

    private const int MaxProductIndex = 10_000;
    private const int MaxConsecutiveNonSuccess = 20;

    private static List<(string ProductCode, string? UserSid, MsiInstallContext Context)> EnumerateProducts(
        CancellationToken ct)
    {
        var results = new List<(string, string?, MsiInstallContext)>();
        var productCode = new char[GuidBufferLength];
        var sidBuffer = new char[SidBufferLength];
        int consecutiveNonSuccess = 0;

        for (uint index = 0; index < MaxProductIndex; index++)
        {
            ct.ThrowIfCancellationRequested();

            // pcchSid is the buffer size in characters including the
            // null terminator, per the MsiEnumProductsExW docs. Pass
            // the full SidBufferLength so the SID is reported correctly
            // without needing the MoreData realloc path for any
            // plausible SID size.
            uint sidLen = SidBufferLength;

            var error = Msi.MsiEnumProductsEx(
                szProductCode: null,
                szUserSid: AllUsersSid,
                dwContext: MsiInstallContext.All,
                dwIndex: index,
                szInstalledProductCode: productCode,
                pdwInstalledContext: out var installedContext,
                szSid: sidBuffer,
                pcchSid: ref sidLen,
                cchInstalledProductCode: GuidBufferLength);

            if (error == MsiError.NoMoreItems)
                break;

            if (error == MsiError.AccessDenied)
                throw new UnauthorizedAccessException(Strings.Error_MsiAccessDenied);

            if (error == MsiError.MoreData)
            {
                // Defensive only. Real-world SIDs are ~45 chars and the
                // first call passes a 256-char buffer, so this branch
                // isn't exercised in normal use. Kept as a safety net
                // for any future unusually-long SID format. Only the
                // SID dimension varies; productCode fits in the fixed
                // GuidBufferLength.
                sidLen++; // null terminator
                sidBuffer = new char[sidLen];

                error = Msi.MsiEnumProductsEx(
                    szProductCode: null,
                    szUserSid: AllUsersSid,
                    dwContext: MsiInstallContext.All,
                    dwIndex: index,
                    szInstalledProductCode: productCode,
                    pdwInstalledContext: out installedContext,
                    szSid: sidBuffer,
                    pcchSid: ref sidLen,
                    cchInstalledProductCode: GuidBufferLength);
            }

            if (error == MsiError.Success)
            {
                consecutiveNonSuccess = 0;
                var sid = (installedContext != MsiInstallContext.Machine && sidLen > 0)
                    ? new string(sidBuffer, 0, (int)sidLen)
                    : null;
                results.Add((BufferToString(productCode), sid, installedContext));
            }
            else
            {
                consecutiveNonSuccess++;
                if (consecutiveNonSuccess >= MaxConsecutiveNonSuccess)
                    throw new InvalidOperationException(
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
        var patchCode = new char[GuidBufferLength];
        var targetProductCode = new char[GuidBufferLength];
        int consecutiveNonSuccess = 0;

        for (uint index = 0; index < MaxPatchIndex; index++)
        {
            ct.ThrowIfCancellationRequested();

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
                pcchTargetUserSid: ref sidLen,
                cchPatchCode: GuidBufferLength,
                cchTargetProductCode: GuidBufferLength);

            if (error == MsiError.NoMoreItems)
                break;

            if (error == MsiError.AccessDenied)
                break; // skip patches we can't access

            if (error == MsiError.Success || error == MsiError.MoreData)
            {
                consecutiveNonSuccess = 0;
                results.Add((BufferToString(patchCode), userSid, patchContext));
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

        return error == MsiError.Success
            ? new string(buffer, 0, (int)bufferLen)
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

        return error == MsiError.Success
            ? new string(buffer, 0, (int)bufferLen)
            : string.Empty;
    }
}
