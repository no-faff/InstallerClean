using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for netapi32.dll: this PC's own server service, asked where one of its
/// shares is on its own disks.
/// </summary>
internal static class Netapi32
{
    private const string Library = "netapi32.dll";

    public const int NERR_Success = 0;

    /// <summary>
    /// Gets a share's information from the server service. Called with a null server name,
    /// which is this PC, and level 2 (<see cref="SHARE_INFO_2"/>), which gives the share's
    /// local path and needs administrator rights. The buffer it returns is freed with
    /// <see cref="NetApiBufferFree"/>.
    /// </summary>
    [DllImport(Library, EntryPoint = "NetShareGetInfo", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int NetShareGetInfo(string? servername, string netname, int level, out IntPtr bufptr);

    [DllImport(Library, EntryPoint = "NetApiBufferFree", ExactSpelling = true)]
    public static extern int NetApiBufferFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SHARE_INFO_2
    {
        public IntPtr shi2_netname;
        public uint shi2_type;
        public IntPtr shi2_remark;
        public uint shi2_permissions;
        public uint shi2_max_uses;
        public uint shi2_current_uses;
        public IntPtr shi2_path;
        public IntPtr shi2_passwd;
    }
}
