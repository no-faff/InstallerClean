using System.Runtime.InteropServices;

namespace InstallerClean.Interop;

/// <summary>
/// Thin wrapper around SHFileOperationW for sending files to the Recycle
/// Bin without any UI. Avoids VB's FileSystem.DeleteFile which expects an
/// STA thread when it shows error dialogs.
/// </summary>
internal static class ShellFileOperations
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszProgressTitle;
    }

    private const uint FO_DELETE = 0x0003;

    private const ushort FOF_SILENT          = 0x0004;
    private const ushort FOF_NOCONFIRMATION  = 0x0010;
    private const ushort FOF_ALLOWUNDO       = 0x0040;
    private const ushort FOF_NOERRORUI       = 0x0400;
    private const ushort FOF_NOCONFIRMMKDIR  = 0x0200;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHFileOperationW")]
    private static extern int SHFileOperationW(ref SHFILEOPSTRUCT lpFileOp);

    /// <summary>
    /// Sends a single file to the Recycle Bin. Returns 0 on success, a
    /// non-zero shell error code otherwise. Never shows UI, never prompts,
    /// thread-safe in any apartment.
    /// </summary>
    internal static int SendToRecycleBin(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path must not be empty", nameof(path));

        // pFrom requires a double-null terminator; the LPWStr marshaller
        // adds one automatically so we only need to append one here.
        var op = new SHFILEOPSTRUCT
        {
            wFunc = FO_DELETE,
            pFrom = path + "\0",
            fFlags = FOF_ALLOWUNDO | FOF_SILENT | FOF_NOCONFIRMATION
                     | FOF_NOERRORUI | FOF_NOCONFIRMMKDIR,
        };
        return SHFileOperationW(ref op);
    }
}
