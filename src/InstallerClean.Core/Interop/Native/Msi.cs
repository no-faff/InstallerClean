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
    /// Fixed buffer size for the szInstalledProductCode / szPatchCode /
    /// szTargetProductCode out-buffers of the MsiEnum*Ex functions. The
    /// Windows Installer API documents these as <c>WCHAR[39]</c>: a
    /// 38-char canonical GUID surface form plus a null terminator.
    /// </summary>
    public const int GuidBufferLength = 39;

    /// <summary>
    /// Enumerates installed products across the user contexts allowed
    /// by <paramref name="dwContext"/>. Returns one product GUID per
    /// call until <see cref="InstallerClean.Interop.MsiError.NoMoreItems"/>.
    /// </summary>
    /// <remarks>
    /// szInstalledProductCode is a fixed 39-char buffer in the native
    /// signature (a GUID + null terminator); the native function takes
    /// no count parameter for it. ConstantElementCount keeps the C#
    /// signature aligned with the 8-parameter native signature.
    /// CountElementName would inject a phantom 9th parameter that the
    /// x64 calling convention passes harmlessly but is undefined
    /// behaviour and crashes on x86.
    /// </remarks>
    [LibraryImport(Library, EntryPoint = "MsiEnumProductsExW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiEnumProductsEx(
        string? szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        uint dwIndex,
        [MarshalUsing(ConstantElementCount = GuidBufferLength)] char[]? szInstalledProductCode,
        out MsiInstallContext pdwInstalledContext,
        [MarshalUsing(CountElementName = nameof(pcchSid))] char[]? szSid,
        ref uint pcchSid);

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
    /// <remarks>
    /// szPatchCode and szTargetProductCode are both fixed 39-char
    /// buffers in the native signature; the native function takes no
    /// count parameter for them. See MsiEnumProductsEx remarks for the
    /// rationale on ConstantElementCount vs CountElementName here.
    /// </remarks>
    [LibraryImport(Library, EntryPoint = "MsiEnumPatchesExW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiEnumPatchesEx(
        string? szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        MsiPatchFilter dwFilter,
        uint dwIndex,
        [MarshalUsing(ConstantElementCount = GuidBufferLength)] char[]? szPatchCode,
        [MarshalUsing(ConstantElementCount = GuidBufferLength)] char[]? szTargetProductCode,
        out MsiInstallContext pdwTargetProductContext,
        [MarshalUsing(CountElementName = nameof(pcchTargetUserSid))] char[]? szTargetUserSid,
        ref uint pcchTargetUserSid);

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
    /// <remarks>
    /// MSIHANDLE is <c>typedef unsigned long MSIHANDLE</c> in msi.h; on
    /// Windows <c>unsigned long</c> is a 4-byte type regardless of
    /// architecture. Using IntPtr (8 bytes on x64) for these handles
    /// is x64-only-by-luck (the lower 32 bits land in the right place
    /// in argument registers) and would crash on x86 where pushing
    /// 8 bytes for a 4-byte argument breaks the stack frame.
    /// </remarks>
    [LibraryImport(Library, EntryPoint = "MsiGetSummaryInformationW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiGetSummaryInformation(
        uint hDatabase,
        string? szDatabasePath,
        uint uiUpdateCount,
        out uint phSummaryInfo);

    /// <summary>
    /// Reads one property out of an open summary-information stream.
    /// The returned <paramref name="puiDataType"/> indicates which of
    /// the value out-params holds the actual data.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiSummaryInfoGetPropertyW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiSummaryInfoGetProperty(
        uint hSummaryInfo,
        uint uiProperty,
        out uint puiDataType,
        out int piValue,
        // The FILETIME* receiver, as its 8 raw bytes. A real target is
        // always supplied rather than NULL: the API writes through the
        // pointer whenever the property's stored type is VT_FILETIME,
        // including when a malformed file declares that type in a slot
        // read for a string.
        out long pftValue,
        [MarshalUsing(CountElementName = nameof(pcchValueBuf))] char[]? szValueBuf,
        ref uint pcchValueBuf);

    /// <summary>
    /// Closes any handle returned by an Msi* function.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiCloseHandle")]
    public static partial uint MsiCloseHandle(uint hAny);

    /// <summary>
    /// Opens an installation package as a database. The handle MUST be closed
    /// via <see cref="MsiCloseHandle"/>.
    /// </summary>
    /// <param name="szPersist">
    /// Declared as a pointer rather than a string because msiquery.h's open modes
    /// are POINTER-VALUED constants, not text: <c>MSIDBOPEN_READONLY</c> is
    /// <c>(LPCTSTR)0</c>, <c>MSIDBOPEN_TRANSACT</c> is <c>(LPCTSTR)1</c>, and so
    /// on up to 4, with <c>MSIDBOPEN_PATCHFILE</c> at 32 as a bit to combine.
    /// Spelling it as a string would make the read-only mode indistinguishable
    /// from a null path argument at the call site and invite somebody to "fix"
    /// it by passing the text "0", which is a pointer into the first page and
    /// not a mode. <see cref="MsiDbOpen.ReadOnly"/> carries the value.
    /// </param>
    [LibraryImport(Library, EntryPoint = "MsiOpenDatabaseW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiOpenDatabase(
        string szDatabasePath,
        IntPtr szPersist,
        out uint phDatabase);

    /// <summary>
    /// Prepares a query against an open database. The view handle MUST be closed
    /// via <see cref="MsiCloseHandle"/>. A query naming a table the database does
    /// not have fails here rather than at execute time.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiDatabaseOpenViewW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiDatabaseOpenView(
        uint hDatabase,
        string szQuery,
        out uint phView);

    /// <summary>
    /// Executes a prepared view. <paramref name="hRecord"/> supplies the query's
    /// parameters and is 0 for a query that has none.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiViewExecute")]
    public static partial uint MsiViewExecute(uint hView, uint hRecord);

    /// <summary>
    /// Fetches the next row of an executed view, returning
    /// <see cref="InstallerClean.Interop.MsiError.NoMoreItems"/> when the result
    /// set is exhausted. Each record handle MUST be closed via
    /// <see cref="MsiCloseHandle"/>.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiViewFetch")]
    public static partial uint MsiViewFetch(uint hView, out uint phRecord);

    /// <summary>
    /// Reads one field of a fetched record as text, using the same double-call
    /// buffer pattern as the property reads above. Fields are 1-based.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MsiRecordGetStringW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint MsiRecordGetString(
        uint hRecord,
        uint iField,
        [MarshalUsing(CountElementName = nameof(pcchValueBuf))] char[]? szValueBuf,
        ref uint pcchValueBuf);
}
