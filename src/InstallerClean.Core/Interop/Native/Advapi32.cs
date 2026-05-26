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

    public const uint TOKEN_ASSIGN_PRIMARY  = 0x0001;
    public const uint TOKEN_DUPLICATE       = 0x0002;
    public const uint TOKEN_QUERY           = 0x0008;
}
