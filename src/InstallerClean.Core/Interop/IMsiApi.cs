namespace InstallerClean.Interop;

/// <summary>
/// Test seam over the Windows Installer enumeration and property P/Invokes that
/// decide a cached file's fate. Introduced so the verdict
/// logic in <c>InstallerQueryService</c> can be pinned by unit tests with a
/// fake, exactly as <c>PendingRebootService</c> takes <c>IRegistryReader</c>
/// and <c>IMutexProbe</c>. The production implementation (<c>MsiApi</c>) is a
/// thin pass-through to the static <c>Msi</c> P/Invokes; no marshalling or
/// buffer contract changes across this boundary, except that
/// <see cref="DeterminePatchSequence"/> takes the patch file's path as a string
/// and the implementation pins it for the function.
/// </summary>
/// <remarks>
/// The signatures of the enumeration and property calls mirror the native
/// double-call buffer pattern verbatim: pass a buffer plus a <c>ref</c>
/// character count, read the return code, and on <c>MoreData</c> reallocate and
/// retry. The interface deliberately carries no buffer-management logic of its
/// own, so a fake sees the same call shape the real API does.
/// <see cref="DeterminePatchSequence"/> takes no buffer: it puts one patch to the
/// function and hands back the two numbers the function writes for it.
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

    /// <summary>
    /// Wraps <c>MsiSourceListEnumSourcesW</c>. See <c>Msi.MsiSourceListEnumSources</c>.
    /// </summary>
    uint EnumSources(
        string productCodeOrPatchCode,
        string? userSid,
        MsiInstallContext context,
        uint options,
        uint index,
        char[]? source,
        ref uint sourceLength);

    /// <summary>
    /// Wraps <c>MsiSourceListEnumMediaDisksW</c>, asking for the disk ID alone. See
    /// <c>Msi.MsiSourceListEnumMediaDisks</c>.
    /// </summary>
    uint EnumMediaDisks(
        string productCodeOrPatchCode,
        string? userSid,
        MsiInstallContext context,
        uint options,
        uint index,
        out uint diskId);

    /// <summary>
    /// Wraps <c>MsiSourceListGetInfoW</c>. See <c>Msi.MsiSourceListGetInfo</c>.
    /// </summary>
    uint GetSourceListInfo(
        string productCodeOrPatchCode,
        string? userSid,
        MsiInstallContext context,
        uint options,
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

    /// <summary>
    /// Wraps <c>MsiDeterminePatchSequenceW</c> for one patch given as its file: whether
    /// Windows Installer would apply the patch at <paramref name="patchFilePath"/> to
    /// the product installed in <paramref name="userSid"/> and
    /// <paramref name="context"/>. <paramref name="order"/> and
    /// <paramref name="status"/> are the two members the function writes for the patch,
    /// and mean nothing where the return is not success. See
    /// <c>Msi.MsiDeterminePatchSequence</c>.
    /// </summary>
    uint DeterminePatchSequence(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string patchFilePath,
        out uint order,
        out uint status);
}
