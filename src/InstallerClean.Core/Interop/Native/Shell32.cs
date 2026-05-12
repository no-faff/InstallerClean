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
    /// SHFILEOPSTRUCT for SHFileOperationW. Fully blittable so it
    /// works under <c>DisableRuntimeMarshalling</c>: string fields
    /// are <see cref="IntPtr"/> handles to caller-allocated unmanaged
    /// buffers, and BOOL is an <see cref="int"/>. Callers allocate
    /// the path buffer (<see cref="Marshal.StringToCoTaskMemUni(string)"/>),
    /// ensure double-null termination (the Win32 list-string
    /// convention for pFrom), and free with
    /// <see cref="Marshal.FreeCoTaskMem(IntPtr)"/>.
    /// </summary>
    /// <remarks>
    /// Pack = 8 matches the modern Win32 x64 ABI for SHFILEOPSTRUCT.
    /// shellapi.h wraps the native declaration in
    /// <c>#include &lt;pshpack1.h&gt;</c>, but pshpack1 is a 16-bit-
    /// Windows relic; on x64 natural alignment places pFrom at offset
    /// 16. Pack = 1 places it at offset 12 and produces
    /// AccessViolationException at runtime, because the kernel reads
    /// pFrom from offset 16.
    /// </remarks>
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
