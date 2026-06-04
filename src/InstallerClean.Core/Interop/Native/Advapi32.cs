using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for advapi32.dll. Used only by
/// <see cref="Helpers.UnelevatedLauncher"/>.
/// </summary>
internal static partial class Advapi32
{
    private const string Library = "advapi32.dll";

    [LibraryImport(Library, EntryPoint = "OpenProcessToken", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool OpenProcessToken(
        SafeProcessHandle processHandle,
        uint desiredAccess,
        out SafeAccessTokenHandle tokenHandle);

    /// <summary>
    /// Converts an impersonation token into a primary token for
    /// <see cref="CreateProcessWithTokenW"/>.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "DuplicateTokenEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DuplicateTokenEx(
        SafeAccessTokenHandle existingToken,
        uint desiredAccess,
        IntPtr tokenAttributes,
        SecurityImpersonationLevel impersonationLevel,
        TokenType tokenType,
        out SafeAccessTokenHandle newToken);

    /// <summary>
    /// Spawns a process under <paramref name="token"/>. Requires the
    /// caller to hold SE_IMPERSONATE_NAME (elevated processes do).
    /// </summary>
    [LibraryImport(Library, EntryPoint = "CreateProcessWithTokenW", SetLastError = true,
                   StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CreateProcessWithTokenW(
        SafeAccessTokenHandle token,
        uint logonFlags,
        string applicationName,
        string commandLine,
        uint creationFlags,
        IntPtr environment,
        string? currentDirectory,
        ref STARTUPINFO startupInfo,
        out PROCESS_INFORMATION processInformation);

    /// <summary>
    /// Resolves a privilege name such as <c>SeImpersonatePrivilege</c> to
    /// its locally unique identifier for <see cref="AdjustTokenPrivileges"/>.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "LookupPrivilegeValueW", SetLastError = true,
                   StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool LookupPrivilegeValueW(string? systemName, string name, out LUID luid);

    /// <summary>
    /// Enables or disables privileges in an access token. Returns true even
    /// when it could not assign every requested privilege: the caller must
    /// read <see cref="Marshal.GetLastWin32Error"/> for ERROR_NOT_ALL_ASSIGNED
    /// to learn whether the privilege was actually granted.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "AdjustTokenPrivileges", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AdjustTokenPrivileges(
        SafeAccessTokenHandle token,
        [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
        ref TOKEN_PRIVILEGES newState,
        uint bufferLength,
        IntPtr previousState,
        IntPtr returnLength);

    // CharSet is omitted because [assembly: DisableRuntimeMarshalling]
    // ignores it for managed structs; every string-shaped field below
    // is already IntPtr. A managed string field added here would not
    // get auto-marshalled by the attribute either, so the cue would
    // mislead more than it helps.
    [StructLayout(LayoutKind.Sequential)]
    public struct STARTUPINFO
    {
        public uint cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public ushort wShowWindow;
        public ushort cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    // TOKEN_PRIVILEGES is natively a count followed by that many
    // LUID_AND_ATTRIBUTES entries. Flattening the single entry inline is
    // valid only because the one caller ever sets exactly one privilege:
    // with PrivilegeCount = 1 the inline Luid and Attributes occupy the same
    // bytes a one-element trailing array would, and the struct stays
    // blittable under [assembly: DisableRuntimeMarshalling].
    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    public enum SecurityImpersonationLevel
    {
        SecurityAnonymous,
        SecurityIdentification,
        SecurityImpersonation,
        SecurityDelegation,
    }

    public enum TokenType
    {
        TokenPrimary = 1,
        TokenImpersonation,
    }

    public const uint TOKEN_ASSIGN_PRIMARY    = 0x0001;
    public const uint TOKEN_DUPLICATE         = 0x0002;
    public const uint TOKEN_QUERY             = 0x0008;
    public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;

    public const uint SE_PRIVILEGE_ENABLED = 0x0002;
}
