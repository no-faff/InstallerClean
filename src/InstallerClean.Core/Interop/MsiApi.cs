using InstallerClean.Interop.Native;

namespace InstallerClean.Interop;

/// <summary>
/// Production <see cref="IMsiApi"/>: a thin pass-through to the static
/// <see cref="Msi"/> P/Invoke surface. It holds no state and adds no logic;
/// the double-call buffer contract, the fixed GUID-buffer sizes and the
/// marshalling declarations all live on <see cref="Msi"/>. The one thing it
/// builds is the single-patch list <see cref="DeterminePatchSequence"/> hands
/// the function, whose entry points at the patch file's path, pinned here for
/// the call. Its only reason to exist is to give the verdict logic a mockable
/// seam.
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

    public uint EnumSources(
        string productCodeOrPatchCode,
        string? userSid,
        MsiInstallContext context,
        uint options,
        uint index,
        char[]? source,
        ref uint sourceLength) =>
        Msi.MsiSourceListEnumSources(
            productCodeOrPatchCode, userSid, context, options, index, source, ref sourceLength);

    public uint EnumMediaDisks(
        string productCodeOrPatchCode,
        string? userSid,
        MsiInstallContext context,
        uint options,
        uint index,
        out uint diskId) =>
        Msi.MsiSourceListEnumMediaDisks(
            productCodeOrPatchCode, userSid, context, options, index, out diskId,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

    public uint GetSourceListInfo(
        string productCodeOrPatchCode,
        string? userSid,
        MsiInstallContext context,
        uint options,
        string property,
        char[]? value,
        ref uint valueLength) =>
        Msi.MsiSourceListGetInfo(
            productCodeOrPatchCode, userSid, context, options, property, value, ref valueLength);

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

    public unsafe uint DeterminePatchSequence(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string patchFilePath,
        out uint order,
        out uint status)
    {
        // The path stays pinned for the call: the struct holds a pointer to its
        // characters, which a string keeps null-terminated.
        fixed (char* path = patchFilePath)
        {
            // Both members start at values the function does not give for a patch it has
            // sequenced, so an answer it did not write is not read as one.
            var patches = new[]
            {
                new Msi.PatchSequenceInfo
                {
                    PatchData = (IntPtr)path,
                    PatchDataType = Msi.PatchDataTypePatchFile,
                    Order = Msi.NotInSequence,
                    Status = uint.MaxValue,
                },
            };

            var error = Msi.MsiDeterminePatchSequence(productCode, userSid, context, 1, patches);
            order = patches[0].Order;
            status = patches[0].Status;
            return error;
        }
    }
}
