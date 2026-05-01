using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for shell32.dll. Currently only used for the
/// Recycle-Bin file-operation API.
/// </summary>
internal static partial class Shell32
{
    private const string Library = "shell32.dll";

    /// <summary>
    /// Performs a high-level shell file operation (copy, move, delete,
    /// rename). With <see cref="FO_DELETE"/> + <see cref="FOF_ALLOWUNDO"/>
    /// it sends the file to the Recycle Bin without UI.
    /// </summary>
    /// <remarks>
    /// SHFileOperationW is documented as superseded by IFileOperation,
    /// but IFileOperation requires apartment-state COM init and an STA
    /// thread. For a single-shot Recycle-Bin send from any thread the
    /// older API stays the right tool.
    /// </remarks>
    [LibraryImport(Library, EntryPoint = "SHFileOperationW")]
    public static partial int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

    /// <summary>
    /// SHFILEOPSTRUCT for SHFileOperationW. The struct is fully
    /// blittable so it works under <c>DisableRuntimeMarshalling</c>:
    /// string fields become <see cref="IntPtr"/> handles to caller-
    /// allocated unmanaged buffers, and the BOOL field becomes an
    /// <see cref="int"/> (Win32 BOOL is a 32-bit integer).
    ///
    /// Callers must allocate the path buffer themselves, e.g. via
    /// <see cref="Marshal.StringToCoTaskMemUni(string)"/>, ensure it
    /// ends with a double null (the Win32 contract for pFrom), and
    /// free it with <see cref="Marshal.FreeCoTaskMem(IntPtr)"/>
    /// after the call.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public IntPtr pFrom;
        public IntPtr pTo;
        public ushort fFlags;
        public int fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public IntPtr lpszProgressTitle;
    }

    /// <summary>wFunc value for delete (sends to Recycle Bin when
    /// combined with <see cref="FOF_ALLOWUNDO"/>).</summary>
    public const uint FO_DELETE = 0x0003;

    public const ushort FOF_SILENT          = 0x0004;
    public const ushort FOF_NOCONFIRMATION  = 0x0010;
    public const ushort FOF_ALLOWUNDO       = 0x0040;
    public const ushort FOF_NOERRORUI       = 0x0400;
    public const ushort FOF_NOCONFIRMMKDIR  = 0x0200;
}
