namespace InstallerClean.Interop;

/// <summary>
/// Test seam over the four Windows Installer enumeration and property
/// P/Invokes that decide a cached file's fate. Introduced so the verdict
/// logic in <c>InstallerQueryService</c> can be pinned by unit tests with a
/// fake, exactly as <c>PendingRebootService</c> takes <c>IRegistryReader</c>
/// and <c>IMutexProbe</c>. The production implementation (<c>MsiApi</c>) is a
/// thin pass-through to the static <c>Msi</c> P/Invokes; no marshalling or
/// buffer contract changes across this boundary.
/// </summary>
/// <remarks>
/// The signatures mirror the native double-call buffer pattern verbatim: pass
/// a buffer plus a <c>ref</c> character count, read the return code, and on
/// <c>MoreData</c> reallocate and retry. The interface deliberately carries no
/// buffer-management logic of its own, so a fake sees the same call shape the
/// real API does.
/// </remarks>
public interface IMsiApi
{
    /// <summary>Wraps <c>MsiEnumProductsExW</c>. See <c>Msi.MsiEnumProductsEx</c>.</summary>
    uint EnumProducts(
        string? productCode,
        string? userSid,
        MsiInstallContext context,
        uint index,
        char[]? installedProductCode,
        out MsiInstallContext installedContext,
        char[]? sid,
        ref uint sidLength);

    /// <summary>Wraps <c>MsiEnumPatchesExW</c>. See <c>Msi.MsiEnumPatchesEx</c>.</summary>
    uint EnumPatches(
        string? productCode,
        string? userSid,
        MsiInstallContext context,
        MsiPatchFilter filter,
        uint index,
        char[]? patchCode,
        char[]? targetProductCode,
        out MsiInstallContext targetProductContext,
        char[]? targetUserSid,
        ref uint targetUserSidLength);

    /// <summary>Wraps <c>MsiGetProductInfoExW</c>. See <c>Msi.MsiGetProductInfoEx</c>.</summary>
    uint GetProductInfo(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string property,
        char[]? value,
        ref uint valueLength);

    /// <summary>Wraps <c>MsiGetPatchInfoExW</c>. See <c>Msi.MsiGetPatchInfoEx</c>.</summary>
    uint GetPatchInfo(
        string patchCode,
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string property,
        char[]? value,
        ref uint valueLength);
}
