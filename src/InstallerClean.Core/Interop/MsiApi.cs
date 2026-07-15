using InstallerClean.Interop.Native;

namespace InstallerClean.Interop;

/// <summary>
/// Production <see cref="IMsiApi"/>: a thin pass-through to the static
/// <see cref="Msi"/> P/Invoke surface. It holds no state and adds no logic;
/// the double-call buffer contract, the fixed GUID-buffer sizes and the
/// [LibraryImport] marshalling all live on <see cref="Msi"/>. Its only reason
/// to exist is to give the verdict logic a mockable seam.
/// </summary>
internal sealed class MsiApi : IMsiApi
{
    public uint EnumProducts(
        string? productCode,
        string? userSid,
        MsiInstallContext context,
        uint index,
        char[]? installedProductCode,
        out MsiInstallContext installedContext,
        char[]? sid,
        ref uint sidLength) =>
        Msi.MsiEnumProductsEx(
            productCode, userSid, context, index,
            installedProductCode, out installedContext, sid, ref sidLength);

    public uint EnumPatches(
        string? productCode,
        string? userSid,
        MsiInstallContext context,
        MsiPatchFilter filter,
        uint index,
        char[]? patchCode,
        char[]? targetProductCode,
        out MsiInstallContext targetProductContext,
        char[]? targetUserSid,
        ref uint targetUserSidLength) =>
        Msi.MsiEnumPatchesEx(
            productCode, userSid, context, filter, index,
            patchCode, targetProductCode, out targetProductContext,
            targetUserSid, ref targetUserSidLength);

    public uint GetProductInfo(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string property,
        char[]? value,
        ref uint valueLength) =>
        Msi.MsiGetProductInfoEx(
            productCode, userSid, context, property, value, ref valueLength);

    public uint GetPatchInfo(
        string patchCode,
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string property,
        char[]? value,
        ref uint valueLength) =>
        Msi.MsiGetPatchInfoEx(
            patchCode, productCode, userSid, context, property, value, ref valueLength);
}
