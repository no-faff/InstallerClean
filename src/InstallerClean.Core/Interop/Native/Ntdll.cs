using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for ntdll.dll, declared as <see cref="Kernel32"/>'s are, and loaded
/// from System32 by the resolver in <see cref="SystemLibraries"/> like every other
/// library here.
/// </summary>
/// <remarks>
/// Declared because the Win32 CreateFile cannot open a child by a name resolved
/// against an already-open directory handle: it takes a full path and resolves every
/// component of it afresh. <see cref="NtCreateFile"/> takes a leaf name and a
/// RootDirectory handle in its <see cref="OBJECT_ATTRIBUTES"/>, so the kernel resolves
/// the leaf against the directory the handle already names rather than against a path
/// string. A caller that opened the directory, read its owner and list, and found them
/// its own can then reach each child through that handle, and a folder put in the
/// directory's place under its name afterwards is not on the path the kernel walks.
/// </remarks>
internal static class Ntdll
{
    private const string Library = "ntdll.dll";

    /// <summary>
    /// Opens or creates a file or directory. The handle comes back as a raw value the
    /// caller wraps in a <see cref="SafeFileHandle"/>. The return value is an NTSTATUS,
    /// zero or above for success and negative for a failure the caller treats as "not
    /// opened".
    /// </summary>
    [DllImport(Library, EntryPoint = "NtCreateFile", ExactSpelling = true)]
    public static extern int NtCreateFile(
        out IntPtr FileHandle,
        uint DesiredAccess,
        in OBJECT_ATTRIBUTES ObjectAttributes,
        out IO_STATUS_BLOCK IoStatusBlock,
        IntPtr AllocationSize,
        uint FileAttributes,
        uint ShareAccess,
        uint CreateDisposition,
        uint CreateOptions,
        IntPtr EaBuffer,
        uint EaLength);

    /// <summary>The Win32 error a failing NTSTATUS maps to, so a handle open that an
    /// NTSTATUS refused is recorded with a cause the rest of the code reads.</summary>
    [DllImport(Library, EntryPoint = "RtlNtStatusToDosError", ExactSpelling = true)]
    public static extern uint RtlNtStatusToDosError(int Status);

    /// <summary>
    /// A counted UTF-16 string, as ntdll takes names. <see cref="Buffer"/> points at
    /// memory the caller keeps alive and pinned for the call; the two lengths are byte
    /// counts, the terminator excluded.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    /// <summary>
    /// What an ntdll open is told to resolve against. <see cref="RootDirectory"/> is
    /// the directory handle a relative name is resolved from, and
    /// <see cref="ObjectName"/> points at the <see cref="UNICODE_STRING"/> leaf. The
    /// two descriptor pointers are left null here.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct OBJECT_ATTRIBUTES
    {
        public uint Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }

    /// <summary>The result block an ntdll call fills; its fields are not read here.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct IO_STATUS_BLOCK
    {
        public IntPtr StatusOrPointer;
        public IntPtr Information;
    }

    /// <summary>Names match without regard to case, as the file system does.</summary>
    public const uint OBJ_CASE_INSENSITIVE = 0x00000040;

    /// <summary>Open the object where it exists, and fail where it does not.</summary>
    public const uint FILE_OPEN = 0x00000001;

    /// <summary>Create the object, and fail where one already has the name.</summary>
    public const uint FILE_CREATE = 0x00000002;

    /// <summary>Open the object as itself where it is a reparse point, never its target.</summary>
    public const uint FILE_OPEN_REPARSE_POINT = 0x00200000;

    /// <summary>Fail unless the object is a directory.</summary>
    public const uint FILE_DIRECTORY_FILE = 0x00000001;

    /// <summary>Fail where the object is a directory.</summary>
    public const uint FILE_NON_DIRECTORY_FILE = 0x00000040;

    /// <summary>Complete the call before returning, so the handle can be used at once.</summary>
    public const uint FILE_SYNCHRONOUS_IO_NONALERT = 0x00000020;

    /// <summary>Remove the object when the handle that created it closes.</summary>
    public const uint FILE_DELETE_ON_CLOSE = 0x00001000;

    /// <summary>The right to delete, and the right synchronous use needs.</summary>
    public const uint DELETE = 0x00010000;
    public const uint SYNCHRONIZE = 0x00100000;

    /// <summary>The right to write a file's bytes, which the write probe's create takes.</summary>
    public const uint FILE_WRITE_DATA = 0x00000002;

    /// <summary>The right to read a file's bytes.</summary>
    public const uint FILE_READ_DATA = 0x00000001;

    /// <summary>
    /// The rights GENERIC_READ maps to on a file: its bytes, its extended attributes,
    /// its attributes, its security descriptor other than the audit list, and SYNCHRONIZE.
    /// </summary>
    public const uint FILE_GENERIC_READ = 0x00120089;
}
