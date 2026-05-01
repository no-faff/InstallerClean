using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for msi.dll (Windows Installer API). All entry
/// points are the Unicode ("W") variants.
///
/// The double-call pattern: pass a buffer plus a ref count of the
/// buffer's character capacity. On success the count is updated to the
/// actual character count (excluding the null terminator). On
/// <see cref="InstallerClean.Interop.MsiError.MoreData"/> the count is
/// updated to the size required and the call should be retried with a
/// larger buffer.
/// </summary>
internal static partial class Msi
{
    private const string Library = "msi.dll";

    /// <summary>
    /// Enumerates installed products across the user contexts allowed
    /// by <paramref name="dwContext"/>. Returns one product GUID per
    /// call until <see cref="InstallerClean.Interop.MsiError.NoMoreItems"/>.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiEnumProductsExW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiEnumProductsEx(
        string? szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        uint dwIndex,
        [MarshalUsing(CountElementName = nameof(cchInstalledProductCode))] char[]? szInstalledProductCode,
        out MsiInstallContext pdwInstalledContext,
        [MarshalUsing(CountElementName = nameof(pcchSid))] char[]? szSid,
        ref uint pcchSid,
        uint cchInstalledProductCode);

    /// <summary>
    /// Reads a property (e.g. "ProductName", "LocalPackage") for a
    /// single registered product. <paramref name="pcchValue"/> is the
    /// double-call buffer-size in/out parameter.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiGetProductInfoExW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiGetProductInfoEx(
        string szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        string szProperty,
        [MarshalUsing(CountElementName = nameof(pcchValue))] char[]? szValue,
        ref uint pcchValue);

    /// <summary>
    /// Enumerates patches against a product, returning patch and
    /// product-target GUIDs plus the user SID context.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiEnumPatchesExW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiEnumPatchesEx(
        string? szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        MsiPatchFilter dwFilter,
        uint dwIndex,
        [MarshalUsing(CountElementName = nameof(cchPatchCode))] char[]? szPatchCode,
        [MarshalUsing(CountElementName = nameof(cchTargetProductCode))] char[]? szTargetProductCode,
        out MsiInstallContext pdwTargetProductContext,
        [MarshalUsing(CountElementName = nameof(pcchTargetUserSid))] char[]? szTargetUserSid,
        ref uint pcchTargetUserSid,
        uint cchPatchCode,
        uint cchTargetProductCode);

    /// <summary>
    /// Reads a property (e.g. "LocalPackage", "State") for a single
    /// patch. <paramref name="pcchValue"/> is the double-call buffer-
    /// size in/out parameter.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiGetPatchInfoExW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiGetPatchInfoEx(
        string szPatchCode,
        string szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        string szProperty,
        [MarshalUsing(CountElementName = nameof(pcchValue))] char[]? szValue,
        ref uint pcchValue);

    /// <summary>
    /// Opens an MSI summary-information stream for a .msi or .msp file.
    /// The returned handle MUST be closed via <see cref="MsiCloseHandle"/>.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiGetSummaryInformationW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiGetSummaryInformation(
        IntPtr hDatabase,
        string? szDatabasePath,
        uint uiUpdateCount,
        out IntPtr phSummaryInfo);

    /// <summary>
    /// Reads one property out of an open summary-information stream.
    /// The returned <paramref name="puiDataType"/> indicates which of
    /// the value out-params holds the actual data.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiSummaryInfoGetPropertyW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiSummaryInfoGetProperty(
        IntPtr hSummaryInfo,
        uint uiProperty,
        out uint puiDataType,
        out int piValue,
        IntPtr pftValue,
        [MarshalUsing(CountElementName = nameof(pcchValueBuf))] char[]? szValueBuf,
        ref uint pcchValueBuf);

    /// <summary>
    /// Closes any handle returned by an Msi* function. Safe to call
    /// with <see cref="IntPtr.Zero"/>; returns success in that case.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiCloseHandle")]
    public static partial uint MsiCloseHandle(IntPtr hAny);
}
