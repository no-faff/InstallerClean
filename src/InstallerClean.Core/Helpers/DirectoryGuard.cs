using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using InstallerClean.Interop.Native;
using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// Opens a folder and reaches the children inside it through handles rather than
/// paths, for a folder that sits where another account can add entries. The app runs
/// elevated, so a delete it makes by a path lands wherever the path leads; another
/// account that can write the folder can, between the moment the path is checked and
/// the moment it is used, put a link in the folder's place whose last component carries
/// a child's name and points anywhere. Reached through a handle instead, a child is
/// resolved against the directory the handle already names, so a folder swapped in
/// under the same name afterwards is not on the route the kernel walks.
/// </summary>
/// <remarks>
/// Three steps make that hold. A folder is opened with
/// <see cref="Kernel32.FILE_FLAG_OPEN_REPARSE_POINT"/>, so a link put in its place
/// opens as the link and is refused, never followed. Its owner and list are read off
/// that handle and checked against what the caller expects, so a folder another account
/// made is told apart from the app's own. Each child is opened relative to the folder
/// handle through <see cref="Ntdll.NtCreateFile"/> and removed through its own handle,
/// so no name is resolved against a path a second time.
/// </remarks>
internal static class DirectoryGuard
{
    /// <summary>The SID of SYSTEM, one of the two the app's own folders grant.</summary>
    internal const string SystemSid = "S-1-5-18";

    /// <summary>The SID of the Administrators group, the other.</summary>
    internal const string AdministratorsSid = "S-1-5-32-544";

    /// <summary>
    /// What a folder's owner and list must be for it to count as the app's own: an
    /// owner drawn from <see cref="OwnerSids"/>, a list naming only SIDs from
    /// <see cref="DaclSids"/>, and, where <see cref="RequireProtected"/>, a list sealed
    /// against inheritance. The two sets are SYSTEM and Administrators for a real folder;
    /// a test that cannot create a folder owned by either adds its own account.
    /// </summary>
    internal readonly struct FolderExpectation
    {
        internal FolderExpectation(IReadOnlySet<string> ownerSids, IReadOnlySet<string> daclSids, bool requireProtected)
        {
            OwnerSids = ownerSids;
            DaclSids = daclSids;
            RequireProtected = requireProtected;
        }

        internal IReadOnlySet<string> OwnerSids { get; }
        internal IReadOnlySet<string> DaclSids { get; }
        internal bool RequireProtected { get; }

        /// <summary>A real folder: owned by SYSTEM or Administrators, granting only those two, sealed.</summary>
        internal static FolderExpectation AppOwned { get; } = new(
            new HashSet<string> { SystemSid, AdministratorsSid },
            new HashSet<string> { SystemSid, AdministratorsSid },
            requireProtected: true);
    }

    /// <summary>
    /// Opens <paramref name="path"/> as a directory handle, following a link in its
    /// final component, so a folder that is a volume mount point or a directory junction
    /// resolves to the directory it stands for: a folder the user chose for a Move, as the
    /// Move's own path resolution and the file move do, and the %ProgramData% an update
    /// makes its staging folder in and later cleans, as a folder made by its path would
    /// be. The children of the handle are still
    /// reached without following a link in each child's name, which is what keeps a
    /// child swapped for a link from redirecting an act. Null where the open fails or
    /// what opened is not a directory, with the Win32 cause in
    /// <paramref name="failureCode"/>.
    /// </summary>
    internal static SafeFileHandle? OpenDirectoryFollow(string path, out int failureCode)
    {
        failureCode = 0;
        if (string.IsNullOrEmpty(path)) return null;

        var handle = Kernel32.CreateFile(
            path, Kernel32.GENERIC_READ, Kernel32.FILE_SHARE_ALL, IntPtr.Zero,
            Kernel32.OPEN_EXISTING, Kernel32.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            failureCode = Marshal.GetLastWin32Error();
            return null;
        }

        if (!Kernel32.GetFileInformationByHandle(handle, out var info))
        {
            failureCode = Marshal.GetLastWin32Error();
            handle.Dispose();
            return null;
        }

        // The read succeeded, so the last error says nothing about this path.
        if ((info.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_DIRECTORY) == 0)
        {
            failureCode = Kernel32.ERROR_DIRECTORY;
            handle.Dispose();
            return null;
        }
        return handle;
    }

    /// <summary>
    /// Whether the folder <paramref name="handle"/> names has the owner and list
    /// <paramref name="expected"/> describes. False on any read failure or any
    /// mismatch, so a folder that cannot be vouched for is never acted on.
    /// </summary>
    internal static bool IsExpectedFolder(SafeFileHandle handle, FolderExpectation expected)
    {
        var rc = Advapi32.GetSecurityInfo(
            handle, Advapi32.SE_FILE_OBJECT,
            Advapi32.OWNER_SECURITY_INFORMATION | Advapi32.DACL_SECURITY_INFORMATION,
            out var owner, out _, out var dacl, out _, out var descriptor);
        if (rc != 0) return false;

        try
        {
            var ownerSid = SidToString(owner);
            if (ownerSid is null || !expected.OwnerSids.Contains(ownerSid)) return false;

            if (!Advapi32.GetSecurityDescriptorControl(descriptor, out var control, out _)) return false;
            if ((control & Advapi32.SE_DACL_PRESENT) == 0) return false;
            if (expected.RequireProtected && (control & Advapi32.SE_DACL_PROTECTED) == 0) return false;

            // A null list pointer grants everyone, so it is never the app's own.
            if (dacl == IntPtr.Zero) return false;
            return EveryAceSidIsExpected(dacl, expected.DaclSids);
        }
        finally
        {
            if (descriptor != IntPtr.Zero) Kernel32.LocalFree(descriptor);
        }
    }

    private static unsafe bool EveryAceSidIsExpected(IntPtr dacl, IReadOnlySet<string> allowed)
    {
        // The ACL header carries its entry count in its third field; each entry begins
        // with a four-byte header, then a four-byte mask, then the SID. An entry whose
        // type is neither "allow" nor "deny" has a different shape, so a folder carrying
        // one is not read further and not taken for the app's own. A list with no entries
        // grants nobody and is not how the app's own folders are made, so it is refused.
        ushort aceCount = *(ushort*)((byte*)dacl + 4);
        if (aceCount == 0) return false;
        for (uint i = 0; i < aceCount; i++)
        {
            if (!Advapi32.GetAce(dacl, i, out var ace)) return false;
            var aceType = *(byte*)ace;
            if (aceType > 1) return false;
            var sid = SidToString(ace + 8);
            if (sid is null || !allowed.Contains(sid)) return false;
        }
        return true;
    }

    private static string? SidToString(IntPtr sid)
    {
        if (sid == IntPtr.Zero) return null;
        if (!Advapi32.ConvertSidToStringSid(sid, out var text)) return null;
        try
        {
            return Marshal.PtrToStringUni(text);
        }
        finally
        {
            Kernel32.LocalFree(text);
        }
    }

    /// <summary>
    /// Opens the child <paramref name="leaf"/> inside the directory
    /// <paramref name="directory"/> names, resolved against that handle and never a
    /// path, without following a link in <paramref name="leaf"/>. Null on any failure.
    /// </summary>
    internal static SafeFileHandle? OpenChildNoFollow(
        SafeFileHandle directory, string leaf, uint access, uint disposition, uint options) =>
        OpenChildNoFollow(directory, leaf, access, disposition, options, out _);

    /// <summary>
    /// The same, reporting the NTSTATUS the open returned in <paramref name="status"/>,
    /// so a caller can tell a child that is absent from one it could not act on. Zero
    /// where a handle came back.
    /// </summary>
    internal static SafeFileHandle? OpenChildNoFollow(
        SafeFileHandle directory, string leaf, uint access, uint disposition, uint options, out int status) =>
        OpenChildNoFollow(directory, leaf, access, Kernel32.FILE_SHARE_ALL, disposition, options, IntPtr.Zero, out status);

    /// <summary>
    /// Creates the folder <paramref name="leaf"/> inside <paramref name="directory"/>, with
    /// <paramref name="securityDescriptor"/> as its descriptor, and answers a handle on it
    /// opened in the same call. The handle shares reading and writing but not delete, so
    /// while it is open no handle can be opened on the folder to delete or rename it. It
    /// asks for no delete access itself, because a process started with the folder as its
    /// working folder opens it sharing no delete, and that open would be refused beside a
    /// handle holding delete access. Null where the create fails, a name already there
    /// among the causes, with the NTSTATUS in <paramref name="status"/>.
    /// </summary>
    internal static SafeFileHandle? CreateChildFolderHeld(
        SafeFileHandle directory, string leaf, IntPtr securityDescriptor, out int status) =>
        OpenChildNoFollow(
            directory, leaf, Ntdll.FILE_GENERIC_READ, Kernel32.FILE_SHARE_READ | Kernel32.FILE_SHARE_WRITE,
            Ntdll.FILE_CREATE, Ntdll.FILE_DIRECTORY_FILE, securityDescriptor, out status);

    /// <summary>
    /// Opens the child folder <paramref name="leaf"/> inside <paramref name="directory"/>
    /// to remove it: for listing and reading its owner and list, and with delete access,
    /// resolved against that handle and without following a link in <paramref name="leaf"/>.
    /// Null where it cannot be opened, is not a folder, or is a reparse point.
    /// <paramref name="heldElsewhere"/> is true where the open was refused because another
    /// handle on the folder shares no delete, as <see cref="CreateChildFolderHeld"/>'s does,
    /// and <paramref name="absent"/> where nothing is at the name.
    /// </summary>
    internal static SafeFileHandle? OpenChildFolderToRemove(
        SafeFileHandle directory, string leaf, out bool heldElsewhere, out bool absent)
    {
        var child = OpenChildFolderNoFollow(directory, leaf, Ntdll.FILE_GENERIC_READ | Ntdll.DELETE, out var status);
        heldElsewhere = status == STATUS_SHARING_VIOLATION;
        absent = status == STATUS_OBJECT_NAME_NOT_FOUND;
        return child;
    }

    private const int STATUS_SHARING_VIOLATION = unchecked((int)0xC0000043);

    /// <summary>
    /// Removes the folder <paramref name="folder"/> names, through that handle, where it is
    /// empty, the handle having been opened with delete access. Answers false where
    /// anything is still inside it or Windows refuses.
    /// </summary>
    internal static bool TryRemoveEmptyFolder(SafeFileHandle folder) => DeleteByHandle(folder);

    /// <summary>
    /// The volume serial number and 128-bit file ID of the file or folder
    /// <paramref name="handle"/> names (<see cref="FileIdentity"/>), which tell it from every
    /// other on its volume while it exists, ReFS included, where the 64-bit index
    /// <see cref="Kernel32.GetFileInformationByHandle"/> gives does not. Null where they
    /// cannot be read.
    /// </summary>
    internal static FileIdentity? IdOf(SafeFileHandle handle) =>
        Kernel32.GetFileInformationByHandleEx(
            handle, Kernel32.FileIdInfo, out var info,
            (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<Kernel32.FILE_ID_INFO>())
            ? new FileIdentity(info.VolumeSerialNumber, info.FileIdLow, info.FileIdHigh)
            : null;

    private static unsafe SafeFileHandle? OpenChildNoFollow(
        SafeFileHandle directory, string leaf, uint access, uint share, uint disposition, uint options,
        IntPtr securityDescriptor, out int status)
    {
        status = 0;
        if (string.IsNullOrEmpty(leaf)) return null;

        var refTaken = false;
        try
        {
            directory.DangerousAddRef(ref refTaken);
            var root = directory.DangerousGetHandle();

            fixed (char* buffer = leaf)
            {
                var name = new Ntdll.UNICODE_STRING
                {
                    Length = (ushort)(leaf.Length * sizeof(char)),
                    MaximumLength = (ushort)(leaf.Length * sizeof(char)),
                    Buffer = (IntPtr)buffer,
                };
                var attributes = new Ntdll.OBJECT_ATTRIBUTES
                {
                    Length = (uint)sizeof(Ntdll.OBJECT_ATTRIBUTES),
                    RootDirectory = root,
                    ObjectName = (IntPtr)(&name),
                    Attributes = Ntdll.OBJ_CASE_INSENSITIVE,
                    SecurityDescriptor = securityDescriptor,
                    SecurityQualityOfService = IntPtr.Zero,
                };

                status = Ntdll.NtCreateFile(
                    out var child, access | Ntdll.SYNCHRONIZE, in attributes, out _, IntPtr.Zero,
                    0, share, disposition,
                    options | Ntdll.FILE_SYNCHRONOUS_IO_NONALERT, IntPtr.Zero, 0);
                return status < 0 ? null : new SafeFileHandle(child, ownsHandle: true);
            }
        }
        finally
        {
            if (refTaken) directory.DangerousRelease();
        }
    }

    /// <summary>
    /// Removes the plain file <paramref name="leaf"/> inside <paramref name="directory"/>
    /// through a handle. Leaves, and answers false, where the child cannot be opened,
    /// its attributes cannot be read, or it is a reparse point; removes it and answers
    /// true otherwise.
    /// </summary>
    internal static bool TryRemoveChildFile(SafeFileHandle directory, string leaf)
    {
        using var child = OpenChildNoFollow(
            directory, leaf,
            Ntdll.DELETE | Kernel32.FILE_READ_ATTRIBUTES, Ntdll.FILE_OPEN,
            Ntdll.FILE_OPEN_REPARSE_POINT | Ntdll.FILE_NON_DIRECTORY_FILE);
        if (child is null) return false;

        if (!Kernel32.GetFileInformationByHandle(child, out var info)
            || (info.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_REPARSE_POINT) != 0)
            return false;

        return DeleteByHandle(child);
    }

    /// <summary>
    /// Removes the child folder <paramref name="leaf"/> inside <paramref name="directory"/>
    /// through a handle, where it is empty. Answers false where it cannot be opened or
    /// anything is still inside it.
    /// </summary>
    internal static bool TryRemoveEmptyChildFolder(SafeFileHandle directory, string leaf)
    {
        using var child = OpenChildFolderNoFollow(
            directory, leaf, Ntdll.DELETE | Kernel32.FILE_READ_ATTRIBUTES, out _);
        return child is not null && DeleteByHandle(child);
    }

    /// <summary>
    /// Opens the child folder <paramref name="leaf"/> inside <paramref name="directory"/>
    /// for listing, resolved against that handle and without following a link in
    /// <paramref name="leaf"/>. Null where it cannot be opened, is not a folder, or is a
    /// reparse point.
    /// </summary>
    internal static SafeFileHandle? OpenChildFolderNoFollow(SafeFileHandle directory, string leaf) =>
        OpenChildFolderNoFollow(directory, leaf, Ntdll.FILE_GENERIC_READ, out _);

    /// <summary>
    /// The same, with <paramref name="access"/>, reporting the NTSTATUS the open returned in
    /// <paramref name="status"/>.
    /// </summary>
    private static SafeFileHandle? OpenChildFolderNoFollow(SafeFileHandle directory, string leaf, uint access, out int status)
    {
        var child = OpenChildNoFollow(
            directory, leaf, access, Ntdll.FILE_OPEN,
            Ntdll.FILE_OPEN_REPARSE_POINT | Ntdll.FILE_DIRECTORY_FILE, out status);
        if (child is null) return null;

        if (!Kernel32.GetFileInformationByHandle(child, out var info)
            || (info.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_REPARSE_POINT) != 0)
        {
            child.Dispose();
            return null;
        }
        return child;
    }

    /// <summary>
    /// Removes the folder at the end of <paramref name="names"/>, found by walking
    /// <paramref name="names"/> down from <paramref name="directory"/>: each name is
    /// opened relative to the folder before it, never by a path and never through a link,
    /// and the last is removed through <see cref="TryRemoveEmptyChildFolder"/>. Answers
    /// false, and removes nothing, where any folder on the way is absent, is a reparse
    /// point or is not a folder, or the last has anything in it.
    /// </summary>
    internal static bool TryRemoveEmptyDescendantFolder(SafeFileHandle directory, IReadOnlyList<string> names)
    {
        if (names.Count == 0) return false;

        var held = new List<SafeFileHandle>();
        try
        {
            var current = directory;
            for (var i = 0; i < names.Count - 1; i++)
            {
                var next = OpenChildFolderNoFollow(current, names[i]);
                if (next is null) return false;
                held.Add(next);
                current = next;
            }
            return TryRemoveEmptyChildFolder(current, names[^1]);
        }
        finally
        {
            foreach (var handle in held) handle.Dispose();
        }
    }

    /// <summary>The access the write probe's create asks for: to write the file and to delete it.</summary>
    internal const uint ProbeAccess = Ntdll.FILE_WRITE_DATA | Ntdll.DELETE;

    /// <summary>The options the write probe's create passes: a file, removed as its handle closes.</summary>
    internal const uint ProbeOptions = Ntdll.FILE_NON_DIRECTORY_FILE | Ntdll.FILE_DELETE_ON_CLOSE;

    /// <summary>
    /// The access the write probe's second open asks for: the rights GENERIC_READ maps
    /// to on a file. So the file's bytes are asked for, not its attributes alone, which
    /// any account that can list the folder is granted on every file in it.
    /// </summary>
    internal const uint ReadBackAccess = Ntdll.FILE_GENERIC_READ;

    /// <summary>The options the write probe's second open passes: a file, opened as itself where it is a link.</summary>
    internal const uint ReadBackOptions = Ntdll.FILE_NON_DIRECTORY_FILE | Ntdll.FILE_OPEN_REPARSE_POINT;

    /// <summary>
    /// Whether <paramref name="folder"/> accepts a file and lets it be read back, tested
    /// through a handle opened on the folder. A random-named child is created relative
    /// to that handle, asking for the right to write it and the right to delete it, and
    /// is removed as the handle closes, so the probe leaves nothing behind. While that
    /// handle is open the child is opened a second time for reading, asking for
    /// <see cref="ReadBackAccess"/>.
    /// <para>
    /// The create is granted whatever the new file's inherited list says, because a
    /// create receives the access it asks for once the folder allows the add. The
    /// second open is checked against that list, which is the one a file copied into
    /// the folder from another volume takes. So a folder whose files cannot be read
    /// fails at the second open, and a folder whose files can be read and not deleted
    /// passes.
    /// </para>
    /// False where the folder cannot be opened, the create is refused, or the second
    /// open fails for any cause, with <paramref name="refusal"/> naming the step and
    /// whether Windows refused access there. Only STATUS_ACCESS_DENIED counts as a
    /// refusal of access; any other failure, such as a sharing violation from another
    /// program that opened the new file first, is named as a failure of that step.
    /// </summary>
    internal static bool TryProbeWritable(string folder, out int failureCode, out WriteProbeRefusal refusal)
    {
        refusal = WriteProbeRefusal.None;
        using var dir = OpenDirectoryFollow(folder, out failureCode);
        if (dir is null)
        {
            refusal = WriteProbeRefusal.FolderOpen;
            return false;
        }

        var leaf = Path.GetRandomFileName();
        using var probe = OpenChildNoFollow(
            dir, leaf, ProbeAccess, Ntdll.FILE_CREATE, ProbeOptions, out var status);
        if (probe is null)
        {
            failureCode = (int)Ntdll.RtlNtStatusToDosError(status);
            refusal = IsAccessRefusal(status)
                ? WriteProbeRefusal.CreateRefusedAccess : WriteProbeRefusal.CreateFailed;
            return false;
        }

        using var readBack = OpenChildNoFollow(
            dir, leaf, ReadBackAccess, Ntdll.FILE_OPEN, ReadBackOptions, out status);
        if (readBack is null)
        {
            failureCode = (int)Ntdll.RtlNtStatusToDosError(status);
            refusal = IsAccessRefusal(status)
                ? WriteProbeRefusal.ReadBackRefusedAccess : WriteProbeRefusal.ReadBackFailed;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Whether the NTSTATUS a failed step of the probe returned says Windows refused the
    /// account access. Only STATUS_ACCESS_DENIED does; a sharing violation or any other
    /// failure says nothing about the folder's permissions.
    /// </summary>
    internal static bool IsAccessRefusal(int status) => status == STATUS_ACCESS_DENIED;

    /// <summary>The access the discard's open of the copy asks for: to delete it and to read and write its attributes.</summary>
    internal const uint DiscardAccess = Ntdll.DELETE | Kernel32.FILE_READ_ATTRIBUTES | Kernel32.FILE_WRITE_ATTRIBUTES;

    /// <summary>The options the discard's open passes: a file, opened as itself where it is a link.</summary>
    internal const uint DiscardOptions = Ntdll.FILE_OPEN_REPARSE_POINT | Ntdll.FILE_NON_DIRECTORY_FILE;

    /// <summary>
    /// Removes the plain file <paramref name="leaf"/> inside <paramref name="folder"/>
    /// through a handle, a read-only one included. The system is asked to ignore the
    /// read-only attribute, so the copy goes without its attributes being touched and a
    /// delete that fails leaves it as it was; where the system does not take that form,
    /// the attribute is cleared for the older delete and put back if that delete fails.
    /// Answers true where the file is gone, which includes where it was not there to
    /// begin with, so a copy that was never made is not reported as a copy that would
    /// not go. False where the file is present and could not be removed, is a reparse
    /// point, or is a folder, which is left in place.
    /// </summary>
    internal static bool TryDiscardChildCopy(string folder, string leaf, out int failureCode)
    {
        using var dir = OpenDirectoryFollow(folder, out failureCode);
        if (dir is null) return false;

        using var child = OpenChildNoFollow(
            dir, leaf, DiscardAccess, Ntdll.FILE_OPEN, DiscardOptions, out var status);
        if (child is null)
        {
            if (status == STATUS_OBJECT_NAME_NOT_FOUND || status == STATUS_OBJECT_PATH_NOT_FOUND)
                return true;
            // A folder at the copy's name is recorded as ERROR_DIRECTORY_NOT_SUPPORTED
            // rather than the access-denied code the open's status maps to.
            failureCode = status == STATUS_FILE_IS_A_DIRECTORY
                ? Kernel32.ERROR_DIRECTORY_NOT_SUPPORTED
                : (int)Ntdll.RtlNtStatusToDosError(status);
            return false;
        }

        if (!Kernel32.GetFileInformationByHandle(child, out var info))
        {
            failureCode = Marshal.GetLastWin32Error();
            return false;
        }

        // The read succeeded, so the last error says nothing about this child.
        if ((info.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_REPARSE_POINT) != 0)
        {
            failureCode = Kernel32.ERROR_REPARSE_POINT_ENCOUNTERED;
            return false;
        }

        if (DeleteByHandle(child, info.dwFileAttributes)) return true;

        // The cause the disposition call itself left.
        failureCode = Marshal.GetLastWin32Error();
        return false;
    }

    private static unsafe void SetAttributesByHandle(SafeFileHandle handle, uint attributes)
    {
        // Only the attributes SetFileAttributes can set are written. A FILE_BASIC_INFO
        // FileAttributes of zero leaves the attributes unchanged, so where nothing is
        // left to set, the file is marked normal instead.
        attributes &= FILE_ATTRIBUTE_VALID_SET_FLAGS;
        if (attributes == 0) attributes = FILE_ATTRIBUTE_NORMAL;
        var basic = new Kernel32.FILE_BASIC_INFO { FileAttributes = attributes };
        Kernel32.SetFileBasicInfoByHandle(
            handle, Kernel32.FileBasicInfo, in basic, (uint)sizeof(Kernel32.FILE_BASIC_INFO));
    }

    private const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

    /// <summary>Archive, hidden, normal, not content indexed, offline, read-only, system and temporary.</summary>
    private const uint FILE_ATTRIBUTE_VALID_SET_FLAGS = 0x000031A7;
    private const int STATUS_OBJECT_NAME_NOT_FOUND = unchecked((int)0xC0000034);
    private const int STATUS_OBJECT_PATH_NOT_FOUND = unchecked((int)0xC000003A);
    private const int STATUS_ACCESS_DENIED = unchecked((int)0xC0000022);
    private const int STATUS_FILE_IS_A_DIRECTORY = unchecked((int)0xC00000BA);

    /// <summary>
    /// Whether <paramref name="error"/>, the answer to the POSIX disposition, says the
    /// system or the file system does not take that form, so that the older one is
    /// tried. Every other answer is a refusal.
    /// </summary>
    internal static bool PosixFormIsNotTaken(int error) =>
        error is Kernel32.ERROR_INVALID_PARAMETER or Kernel32.ERROR_NOT_SUPPORTED or Kernel32.ERROR_INVALID_FUNCTION;

    /// <summary>
    /// Deletes the file or empty folder <paramref name="handle"/> names. Where
    /// <paramref name="fileAttributes"/> is given, the file may be read-only and these
    /// are its attributes: for a read-only file the POSIX form asks the system to ignore
    /// the attribute, and the older form has it cleared and put back if the delete fails.
    /// </summary>
    private static unsafe bool DeleteByHandle(SafeFileHandle handle, uint? fileAttributes = null)
    {
        var flags = Kernel32.FILE_DISPOSITION_FLAG_DELETE | Kernel32.FILE_DISPOSITION_FLAG_POSIX_SEMANTICS;
        if (fileAttributes is { } given && (given & FILE_ATTRIBUTE_READONLY) != 0)
            flags |= Kernel32.FILE_DISPOSITION_FLAG_IGNORE_READONLY_ATTRIBUTE;
        var posix = new Kernel32.FILE_DISPOSITION_INFO_EX { Flags = flags };
        if (Kernel32.SetFileDispositionInfoEx(
                handle, Kernel32.FileDispositionInfoEx, in posix, (uint)sizeof(Kernel32.FILE_DISPOSITION_INFO_EX)))
            return true;

        // A Windows build or file system that does not take the POSIX form is
        // answered with one of the codes below, and the older form is tried. Any other
        // answer is a refusal and stays one: the older form takes effect when the last
        // handle closes, so with another program holding the file open it would answer
        // success while the name is still there.
        if (!PosixFormIsNotTaken(Marshal.GetLastWin32Error())) return false;

        return fileAttributes is { } attributes
            ? DeleteClearingReadOnly(handle, attributes)
            : DeleteByPlainForm(handle);
    }

    private static unsafe bool DeleteByPlainForm(SafeFileHandle handle)
    {
        var plain = new Kernel32.FILE_DISPOSITION_INFO { DeleteFile = 1 };
        return Kernel32.SetFileDispositionInfo(
            handle, Kernel32.FileDispositionInfo, in plain, (uint)sizeof(Kernel32.FILE_DISPOSITION_INFO));
    }

    /// <summary>
    /// The older delete of a file whose attributes are <paramref name="attributes"/>,
    /// with the read-only attribute cleared for it and put back if the delete call
    /// fails. The last error after a false answer is the delete's own.
    /// </summary>
    internal static bool DeleteClearingReadOnly(SafeFileHandle handle, uint attributes)
    {
        if ((attributes & FILE_ATTRIBUTE_READONLY) == 0) return DeleteByPlainForm(handle);

        SetAttributesByHandle(handle, attributes & ~FILE_ATTRIBUTE_READONLY);
        if (DeleteByPlainForm(handle)) return true;

        var error = Marshal.GetLastWin32Error();
        // The attributes as they are now, with read-only put back.
        var current = Kernel32.GetFileInformationByHandle(handle, out var info)
            ? info.dwFileAttributes : attributes & ~FILE_ATTRIBUTE_READONLY;
        SetAttributesByHandle(handle, (current & ~FILE_ATTRIBUTE_NORMAL) | FILE_ATTRIBUTE_READONLY);
        Kernel32.SetLastError(unchecked((uint)error));
        return false;
    }
}
