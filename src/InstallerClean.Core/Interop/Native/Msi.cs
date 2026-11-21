using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for msi.dll (Windows Installer API). All entry
/// points are the Unicode ("W") variants, and every declaration passing
/// text is CharSet.Unicode, so a string and a char[] each reach the
/// function as a pointer to UTF-16 characters, as a W function reads them.
///
/// A char[] buffer is passed as a pointer to its first element, and
/// [Out] copies what the function wrote back into the array wherever
/// the marshaller copies the array rather than pinning it.
///
/// The double-call pattern: pass a buffer plus a ref count of the
/// buffer's character capacity. On success the count is updated to the
/// actual character count (excluding the null terminator). On
/// <see cref="InstallerClean.Interop.MsiError.MoreData"/> the count is
/// updated to the size required and the call should be retried with a
/// larger buffer.
/// </summary>
internal static class Msi
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
    /// no count parameter for it, so the caller allocates it at
    /// <see cref="GuidBufferLength"/> and the declaration has the native
    /// signature's eight parameters exactly. A count parameter added for
    /// the buffer would be a ninth argument the function does not take,
    /// which the x64 calling convention passes harmlessly but is undefined
    /// behaviour and crashes on x86.
    /// </remarks>
    [DllImport(Library, EntryPoint = "MsiEnumProductsExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint MsiEnumProductsEx(
        string? szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        uint dwIndex,
        [Out] char[]? szInstalledProductCode,
        out MsiInstallContext pdwInstalledContext,
        [Out] char[]? szSid,
        ref uint pcchSid);

    /// <summary>
    /// Reads a property (e.g. "ProductName", "LocalPackage") for a
    /// single registered product. <paramref name="pcchValue"/> is the
    /// double-call buffer-size in/out parameter.
    /// </summary>
    [DllImport(Library, EntryPoint = "MsiGetProductInfoExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint MsiGetProductInfoEx(
        string szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        string szProperty,
        [Out] char[]? szValue,
        ref uint pcchValue);

    /// <summary>
    /// Reads one entry of the source list Windows Installer holds for a product or a
    /// patch in one account and context, among the source types
    /// <paramref name="dwOptions"/> names; the same flags say which of the two the code
    /// is. <paramref name="pcchSource"/> is the buffer's size in characters going in and
    /// the entry's length coming out. <paramref name="szUserSid"/> is null for a
    /// per-machine installation.
    /// </summary>
    /// <remarks>
    /// Windows keeps the position of the walk between calls, and a success moves it on,
    /// a call with a null buffer included. So an entry is read in one call with a
    /// buffer. A sizing call followed by a second call at the same index, the way a
    /// property is read, answers ERROR_INVALID_PARAMETER at every index after the
    /// first.
    /// </remarks>
    [DllImport(Library, EntryPoint = "MsiSourceListEnumSourcesW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint MsiSourceListEnumSources(
        string szProductCodeOrPatchCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        uint dwOptions,
        uint dwIndex,
        [Out] char[]? szSource,
        ref uint pcchSource);

    /// <summary>
    /// Reads one property of the source list Windows Installer holds for a product or
    /// a patch in one account and context, <paramref name="dwOptions"/> saying which of
    /// the two the code is. <paramref name="pcchValue"/> is the double-call buffer-size
    /// in/out parameter, as on <see cref="MsiGetProductInfoEx"/>.
    /// <paramref name="szUserSid"/> is null for a per-machine installation.
    /// </summary>
    [DllImport(Library, EntryPoint = "MsiSourceListGetInfoW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint MsiSourceListGetInfo(
        string szProductCodeOrPatchCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        uint dwOptions,
        string szProperty,
        [Out] char[]? szValue,
        ref uint pcchValue);

    /// <summary>
    /// Reads one disk of the media sources Windows Installer holds for a product or a patch
    /// in one account and context, at <paramref name="dwIndex"/>, with its disk ID in
    /// <paramref name="pdwDiskId"/>. Declared for whether there is a disk at an index alone:
    /// the volume label and disk prompt, and their lengths, are passed as null pointers,
    /// which the function accepts for each pair together. <paramref name="szUserSid"/> is null
    /// for a per-machine installation. The walk keeps its position between calls, as
    /// <see cref="MsiSourceListEnumSources"/>'s does, and a call at index 0 starts it again.
    /// </summary>
    [DllImport(Library, EntryPoint = "MsiSourceListEnumMediaDisksW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint MsiSourceListEnumMediaDisks(
        string szProductCodeOrPatchCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        uint dwOptions,
        uint dwIndex,
        out uint pdwDiskId,
        IntPtr szVolumeLabel,
        IntPtr pcchVolumeLabel,
        IntPtr szDiskPrompt,
        IntPtr pcchDiskPrompt);

    /// <summary>
    /// Enumerates patches against a product, returning patch and
    /// product-target GUIDs plus the user SID context.
    /// </summary>
    /// <remarks>
    /// szPatchCode and szTargetProductCode are both fixed 39-char
    /// buffers in the native signature; the native function takes no
    /// count parameter for them, and the declaration has the native
    /// signature's ten parameters exactly. MsiEnumProductsEx's remarks say
    /// what a count parameter added here would do.
    /// </remarks>
    [DllImport(Library, EntryPoint = "MsiEnumPatchesExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint MsiEnumPatchesEx(
        string? szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        MsiPatchFilter dwFilter,
        uint dwIndex,
        [Out] char[]? szPatchCode,
        [Out] char[]? szTargetProductCode,
        out MsiInstallContext pdwTargetProductContext,
        [Out] char[]? szTargetUserSid,
        ref uint pcchTargetUserSid);

    /// <summary>
    /// Reads a property (e.g. "LocalPackage", "State") for a single
    /// patch. <paramref name="pcchValue"/> is the double-call buffer-
    /// size in/out parameter.
    /// </summary>
    [DllImport(Library, EntryPoint = "MsiGetPatchInfoExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint MsiGetPatchInfoEx(
        string szPatchCode,
        string szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        string szProperty,
        [Out] char[]? szValue,
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
    [DllImport(Library, EntryPoint = "MsiGetSummaryInformationW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint MsiGetSummaryInformation(
        uint hDatabase,
        string? szDatabasePath,
        uint uiUpdateCount,
        out uint phSummaryInfo);

    /// <summary>
    /// Reads one property out of an open summary-information stream.
    /// The returned <paramref name="puiDataType"/> indicates which of
    /// the value out-params holds the actual data.
    /// </summary>
    [DllImport(Library, EntryPoint = "MsiSummaryInfoGetPropertyW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint MsiSummaryInfoGetProperty(
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
        [Out] char[]? szValueBuf,
        ref uint pcchValueBuf);

    /// <summary>
    /// Closes any handle returned by an Msi* function.
    /// </summary>
    [DllImport(Library, EntryPoint = "MsiCloseHandle", ExactSpelling = true)]
    public static extern uint MsiCloseHandle(uint hAny);

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
    [DllImport(Library, EntryPoint = "MsiOpenDatabaseW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint MsiOpenDatabase(
        string szDatabasePath,
        IntPtr szPersist,
        out uint phDatabase);

    /// <summary>
    /// Prepares a query against an open database. The view handle MUST be closed
    /// via <see cref="MsiCloseHandle"/>. A query naming a table the database does
    /// not have fails here rather than at execute time.
    /// </summary>
    [DllImport(Library, EntryPoint = "MsiDatabaseOpenViewW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint MsiDatabaseOpenView(
        uint hDatabase,
        string szQuery,
        out uint phView);

    /// <summary>
    /// Executes a prepared view. <paramref name="hRecord"/> supplies the query's
    /// parameters and is 0 for a query that has none.
    /// </summary>
    [DllImport(Library, EntryPoint = "MsiViewExecute", ExactSpelling = true)]
    public static extern uint MsiViewExecute(uint hView, uint hRecord);

    /// <summary>
    /// Fetches the next row of an executed view, returning
    /// <see cref="InstallerClean.Interop.MsiError.NoMoreItems"/> when the result
    /// set is exhausted. Each record handle MUST be closed via
    /// <see cref="MsiCloseHandle"/>.
    /// </summary>
    [DllImport(Library, EntryPoint = "MsiViewFetch", ExactSpelling = true)]
    public static extern uint MsiViewFetch(uint hView, out uint phRecord);

    /// <summary>
    /// Reads one field of a fetched record as text, using the same double-call
    /// buffer pattern as the property reads above. Fields are 1-based.
    /// </summary>
    [DllImport(Library, EntryPoint = "MsiRecordGetStringW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint MsiRecordGetString(
        uint hRecord,
        uint iField,
        [Out] char[]? szValueBuf,
        ref uint pcchValueBuf);
}
