using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// The folder an update's download is written to before it runs: a new folder
/// straight under %ProgramData%, which only Administrators and SYSTEM can change.
/// </summary>
/// <remarks>
/// The app runs elevated, and it runs the file it downloads, so the file must sit
/// where a process without administrator rights cannot swap it between the write and
/// the start. %ProgramData% lets any user create a folder in it and keeps them out of
/// folders others made, so the folder is given its own descriptor as it is created,
/// in the same call, rather than a moment later. That descriptor is protected, so
/// nothing from %ProgramData% is inherited, and everything written inside inherits it.
/// A name that already exists fails the create, so a folder somebody else made can
/// never be taken for this one. Keep it straight under %ProgramData%: a folder deeper
/// down is only as safe as every folder above it, and any user can make those first.
/// The folder is held from the same call that makes it (<see cref="Handle"/>), sharing no
/// delete, so no other process can remove or rename it while this one has it, and the
/// start-up clean-up, in this process or another, leaves it and everything in it alone. Whatever
/// this process removes from it goes through a handle on the folder that is shown to be
/// the one it made, never by the path.
/// </remarks>
public sealed class UpdateStagingFolder
{
    /// <summary>What every staging folder's name starts with, before 32 hex digits.</summary>
    internal const string NamePrefix = "InstallerClean-update-";

    /// <summary>
    /// Administrators as the owner, full control for SYSTEM and for Administrators on
    /// the folder and inherited by every file and folder inside it, and nothing for
    /// anybody else. "P" protects the list from the parent's inheritable entries. The
    /// owner is fixed to Administrators, rather than left to the creating account's
    /// default, so that a folder the app made is told from one another account made by
    /// an owner no account without administrator rights can set.
    /// </summary>
    internal const string AdministratorsOnly = "O:BAD:P(A;OICI;FA;;;SY)(A;OICI;FA;;;BA)";

    private readonly string _root;
    private readonly string _leaf;
    private readonly FileIdentity _id;
    private bool _gone;

    private UpdateStagingFolder(string root, string leaf, SafeFileHandle handle, FileIdentity id)
    {
        _root = root;
        _leaf = leaf;
        _id = id;
        FullPath = Path.Combine(root, leaf);
        Handle = handle;
    }

    /// <summary>The folder's full path.</summary>
    public string FullPath { get; }

    /// <summary>
    /// The handle on the folder, opened as it was made, sharing reading and writing but not
    /// delete (<see cref="DirectoryGuard.CreateChildFolderHeld"/>). Held until
    /// <see cref="TryRemove"/> lets it go, or until the process ends.
    /// </summary>
    internal SafeFileHandle Handle { get; }

    /// <summary>
    /// The folder under the PC's %ProgramData%, the one the app uses.
    /// </summary>
    public static string DefaultRoot =>
        KnownFolders.GetPath(Environment.SpecialFolder.CommonApplicationData);

    /// <summary>
    /// Creates a new staging folder under <paramref name="root"/>, named
    /// <see cref="NamePrefix"/> and a new GUID, and holds it (<see cref="Handle"/>). Throws
    /// <see cref="Win32Exception"/> where Windows refuses the descriptor, the root or the
    /// folder, the name already being taken among them, and <see cref="IOException"/> where
    /// the root cannot be found.
    /// </summary>
    public static UpdateStagingFolder Create(string root) => Create(root, AdministratorsOnly);

    /// <summary>
    /// Test seam: the same, with the descriptor given as SDDL. The suite runs without
    /// administrator rights, so a test that writes into the folder adds its own
    /// account to <see cref="AdministratorsOnly"/>.
    /// </summary>
    internal static UpdateStagingFolder Create(string root, string descriptorSddl)
    {
        if (!Directory.Exists(root))
            throw new IOException("The folder to create the update's folder in does not exist.");

        var leaf = NamePrefix + Guids.NewGuid().ToString("N");

        // The root is opened as the path names it, a link included, as a folder made by its
        // path would be; the folder is then made relative to that handle.
        using var rootHandle = DirectoryGuard.OpenDirectoryFollow(root, out var rootError)
            ?? throw new Win32Exception(rootError);

        if (!Advapi32.ConvertStringSecurityDescriptorToSecurityDescriptor(
                descriptorSddl, Advapi32.SDDL_REVISION_1, out var descriptor, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            var handle = DirectoryGuard.CreateChildFolderHeld(rootHandle, leaf, descriptor, out var status)
                ?? throw new Win32Exception((int)Ntdll.RtlNtStatusToDosError(status));
            if (DirectoryGuard.IdOf(handle) is not { } id)
            {
                var error = Marshal.GetLastWin32Error();
                handle.Dispose();
                throw new Win32Exception(error);
            }
            return new UpdateStagingFolder(root, leaf, handle, id);
        }
        finally
        {
            Kernel32.LocalFree(descriptor);
        }
    }

    /// <summary>
    /// Lets go of <see cref="Handle"/>, then removes the files named <paramref name="leaves"/>
    /// from this folder and the folder where it is then empty, and answers whether the folder
    /// is gone. The folder is opened again by its name with delete access, relative to a
    /// handle on the root and without following a link, and acted on only where it is the
    /// folder this made, the same volume and file ID; each file is removed through a handle
    /// relative to it. Where anything is left, a file another process holds open say, it can
    /// be called again, and otherwise a later start's clean-up
    /// (<see cref="RemoveLeftovers(string)"/>) takes the folder. Never throws.
    /// </summary>
    internal bool TryRemove(IEnumerable<string> leaves)
    {
        try
        {
            if (_gone) return true;
            Handle.Dispose();

            using var rootHandle = DirectoryGuard.OpenDirectoryFollow(_root, out _);
            if (rootHandle is null) return false;
            using var folder = DirectoryGuard.OpenChildFolderToRemove(rootHandle, _leaf, out _, out var absent);
            // Nothing at the name, or another folder there, means the one this made has gone.
            // Where the folder will not open for another cause, or its ID cannot be read, it is
            // left for a later try or the clean-up.
            if (absent) return _gone = true;
            if (folder is null || DirectoryGuard.IdOf(folder) is not { } id) return false;
            if (id != _id) return _gone = true;

            foreach (var leaf in leaves)
                DirectoryGuard.TryRemoveChildFile(folder, leaf);
            return _gone = DirectoryGuard.TryRemoveEmptyFolder(folder);
        }
        catch
        {
            // Nothing here is worth a failed undo.
            return false;
        }
    }

    /// <summary>
    /// Removes the staging folders earlier updates left under <paramref name="root"/>,
    /// as the setup's leaves one: the setup is still running from it when the app
    /// closes, and still holds its own file open as it opens the app again. Answers
    /// whether none of this app's folders is left, so the caller can try again while
    /// that setup finishes. Never throws.
    /// </summary>
    /// <remarks>
    /// Any user can create a folder with this name in %ProgramData%, so nothing found
    /// is trusted and nothing is reached by its path across a check. The root is opened
    /// as a handle, as the path names it, a link included, as <see cref="Create(string)"/>
    /// opens it, so a %ProgramData% that is a junction to another drive is cleaned where
    /// the folders were made. Each candidate folder is opened relative to it, with delete
    /// access and without following a link in its name; a folder is cleaned only where
    /// its owner and list are the app's own, as <see cref="Create(string)"/> sets them, so
    /// a folder another account made is left whole. A folder an update still holds
    /// (<see cref="Handle"/>), in this process or another, refuses that open, and is left whole and counted
    /// as not gone. Inside a folder that is the app's own, the files are listed by the
    /// folder's path, which only Administrators and SYSTEM can change, and each is removed
    /// through a handle opened relative to the folder's, and only where its name has the
    /// downloads' shape, "InstallerClean-" to ".exe", and it is a plain file; the folder
    /// itself goes, through its own handle, only once it is empty.
    /// </remarks>
    public static bool RemoveLeftovers(string root) =>
        RemoveLeftovers(root, DirectoryGuard.FolderExpectation.AppOwned);

    /// <summary>
    /// Test seam: the same, against the owner and list a test's folder carries. The
    /// suite runs without administrator rights, so it cannot create a folder owned by
    /// Administrators or SYSTEM and names its own account in <paramref name="expected"/>.
    /// </summary>
    internal static bool RemoveLeftovers(string root, DirectoryGuard.FolderExpectation expected)
    {
        try
        {
            if (!Directory.Exists(root)) return true;

            using var rootHandle = DirectoryGuard.OpenDirectoryFollow(root, out _);
            if (rootHandle is null) return false;

            var allGone = true;
            foreach (var folder in Directory.EnumerateDirectories(root, NamePrefix + "*"))
            {
                var leaf = Path.GetFileName(folder);
                try
                {
                    if (!IsOurName(leaf)) continue;

                    using var folderHandle = DirectoryGuard.OpenChildFolderToRemove(rootHandle, leaf, out var heldElsewhere, out _);
                    if (heldElsewhere)
                    {
                        // Held where it shares no delete. Where it is the app's own, an update is
                        // using it, in this process or another, so it is not this pass's to take
                        // and is counted as left; a folder another account made and holds is not one the
                        // app is waiting on.
                        using var look = DirectoryGuard.OpenChildFolderNoFollow(rootHandle, leaf);
                        if (look is not null && DirectoryGuard.IsExpectedFolder(look, expected))
                            allGone = false;
                        continue;
                    }
                    if (folderHandle is null || !DirectoryGuard.IsExpectedFolder(folderHandle, expected))
                        continue;

                    var cleared = true;
                    foreach (var file in Directory.EnumerateFiles(folder, "InstallerClean-*.exe"))
                    {
                        if (!DirectoryGuard.TryRemoveChildFile(folderHandle, Path.GetFileName(file)))
                            cleared = false;
                    }

                    if (!cleared || !DirectoryGuard.TryRemoveEmptyFolder(folderHandle))
                        allGone = false;
                }
                catch
                {
                    // One folder that will not go does not stop the rest.
                    allGone = false;
                }
            }
            return allGone;
        }
        catch
        {
            // Nothing here is worth a failed start.
            return false;
        }
    }

    private static bool IsOurName(string name) =>
        name.Length == NamePrefix.Length + 32
        && name.StartsWith(NamePrefix, StringComparison.OrdinalIgnoreCase)
        && name.AsSpan(NamePrefix.Length).IndexOfAnyExcept("0123456789abcdefABCDEF") < 0;
}
