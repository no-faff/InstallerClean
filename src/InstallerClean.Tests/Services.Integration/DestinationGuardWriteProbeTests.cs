using System.Security.AccessControl;
using System.Security.Principal;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The write probe against folders whose access lists decide what the account can do with
/// a file put in them: a folder it cannot list; a folder it can list and not add to; a
/// folder it can list and add to whose files it can write and not read; one whose files
/// give their bytes and not everything an open for reading needs; one whose files it can
/// read and write and not delete; and one whose files it can also delete. The probe passes
/// the last two, whose files can be read back, and fails the rest, naming the step that
/// refused.
///
/// Each test first holds what Windows lets the account do in that folder, with plain calls
/// and a file made by the probe's own create less DELETE and delete-on-close, so a folder
/// that is not the case under test fails there with its own message before the probe is
/// asked. The probe then runs in a second folder with the same list, which nothing else
/// has touched.
///
/// Each folder carries a protected list naming this account alone, so no entry inherited
/// from the temporary folder's parent, and no group the account's token holds, can supply a
/// right. The folder's list never grants FILE_DELETE_CHILD, which would also satisfy a
/// request to delete a file inside it.
/// </summary>
public sealed class DestinationGuardWriteProbeTests : IDisposable
{
    private readonly string _root = TestTemp.NewFolder("ic-probe-acl-");
    private readonly IDestinationGuard _guard = new HandleDestinationGuard();

    private static readonly string ThisAccountSid = WindowsIdentity.GetCurrent().User!.Value;

    // ERROR_ACCESS_DENIED, which RtlNtStatusToDosError gives for STATUS_ACCESS_DENIED.
    private const int AccessDenied = 5;

    // On the folder: FILE_LIST_DIRECTORY, FILE_ADD_FILE, FILE_READ_EA, FILE_TRAVERSE,
    // FILE_READ_ATTRIBUTES, READ_CONTROL and SYNCHRONIZE. This covers GENERIC_READ, which
    // the probe's open of the folder asks for, and the right to add a file.
    private const string FolderRights = "0x1200AB";

    // The same less FILE_LIST_DIRECTORY, so the probe's open of the folder is refused.
    private const string FolderRightsWithoutList = "0x1200AA";

    // The same as FolderRights less FILE_ADD_FILE, so the folder opens and the probe's
    // create is refused.
    private const string FolderRightsWithoutAdd = "0x1200A9";

    // Inherited by files: FILE_WRITE_DATA, FILE_APPEND_DATA, FILE_WRITE_EA,
    // FILE_WRITE_ATTRIBUTES, READ_CONTROL and SYNCHRONIZE. No FILE_READ_DATA and no DELETE.
    private const string FileRightsWriteOnly = "0x120116";

    // FILE_READ_DATA, FILE_WRITE_DATA, FILE_APPEND_DATA, FILE_READ_EA, FILE_WRITE_EA,
    // FILE_READ_ATTRIBUTES, FILE_WRITE_ATTRIBUTES, READ_CONTROL and SYNCHRONIZE. No DELETE.
    private const string FileRightsReadWrite = "0x12019F";

    // The same less FILE_READ_EA (0x8): a file's bytes can be read, and an open asking for
    // everything an open for reading needs is refused.
    private const string FileRightsBytesWithoutExtendedAttributes = "0x120197";

    // The same with DELETE (0x10000) added.
    private const string FileRightsReadWriteDelete = "0x13019F";

    private static string ListGranting(string folderRights, string fileRights) =>
        $"D:P(A;;{folderRights};;;{ThisAccountSid})(A;OIIO;{fileRights};;;{ThisAccountSid})";

    private static readonly string FullControlOnFolders =
        $"D:P(A;OICI;FA;;;SY)(A;OICI;FA;;;BA)(A;OICI;FA;;;{ThisAccountSid})";

    private static readonly string FullControlOnFiles =
        $"D:P(A;;FA;;;SY)(A;;FA;;;BA)(A;;FA;;;{ThisAccountSid})";

    public void Dispose()
    {
        // The account created each folder and so may rewrite its list, which is how it is
        // let back in to list a folder and delete the files in it. Each folder is reset
        // before its files are listed, and each file's own list is reset too.
        List<string> folders;
        try { folders = Directory.EnumerateDirectories(_root).ToList(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { folders = []; }

        foreach (var folder in folders)
        {
            ResetAccess(folder, isFolder: true);
            try
            {
                foreach (var file in Directory.EnumerateFiles(folder).ToList())
                    ResetAccess(file, isFolder: false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }

        TestTemp.Delete(_root);
    }

    private static void ResetAccess(string path, bool isFolder)
    {
        try
        {
            if (isFolder)
            {
                var security = new DirectorySecurity();
                security.SetSecurityDescriptorSddlForm(FullControlOnFolders, AccessControlSections.Access);
                new DirectoryInfo(path).SetAccessControl(security);
            }
            else
            {
                var security = new FileSecurity();
                security.SetSecurityDescriptorSddlForm(FullControlOnFiles, AccessControlSections.Access);
                new FileInfo(path).SetAccessControl(security);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private string MakeFolder(string accessList)
    {
        var path = Directory.CreateDirectory(Path.Combine(_root, Guid.NewGuid().ToString("N"))).FullName;
        var security = new DirectorySecurity();
        security.SetSecurityDescriptorSddlForm(accessList, AccessControlSections.Access);
        new DirectoryInfo(path).SetAccessControl(security);
        return path;
    }

    private static void AssertTheProbeAsksWhatThisTestAssumes()
    {
        // The create asks for the right to delete and for the file to go as its handle
        // closes, so the probe leaves nothing; the second open asks to read the file's
        // bytes, which the file's own list decides.
        Assert.NotEqual(0u, DirectoryGuard.ProbeAccess & Ntdll.DELETE);
        Assert.NotEqual(0u, DirectoryGuard.ProbeOptions & Ntdll.FILE_DELETE_ON_CLOSE);

        // The second open asks for what GENERIC_READ maps to on a file: FILE_READ_DATA,
        // FILE_READ_EA, FILE_READ_ATTRIBUTES, READ_CONTROL and SYNCHRONIZE.
        Assert.Equal(0x00120089u, DirectoryGuard.ReadBackAccess);
        Assert.NotEqual(0u, DirectoryGuard.ReadBackOptions & Ntdll.FILE_OPEN_REPARSE_POINT);
    }

    private static void AssertTheFolderOpens(string folder)
    {
        // Without this a refusal at the open of the folder would answer false with the same
        // code as a refusal later in the probe, and the assertions below could not tell them apart.
        using var opened = DirectoryGuard.OpenDirectoryFollow(folder, out var failureCode);
        Assert.True(opened is not null, $"the probe's open of the folder was refused (Win32 error {failureCode})");
    }

    private static string CreateFileLikeTheProbeLessDelete(string folder, string leaf)
    {
        // The probe's own create through the folder's handle, with DELETE and
        // FILE_DELETE_ON_CLOSE taken out, so the file stays when the handle closes.
        using var directory = DirectoryGuard.OpenDirectoryFollow(folder, out _);
        Assert.NotNull(directory);
        using var created = DirectoryGuard.OpenChildNoFollow(
            directory, leaf, DirectoryGuard.ProbeAccess & ~Ntdll.DELETE, Ntdll.FILE_CREATE,
            DirectoryGuard.ProbeOptions & ~Ntdll.FILE_DELETE_ON_CLOSE, out var status);
        Assert.True(created is not null,
            "the folder refused the probe's create without DELETE and FILE_DELETE_ON_CLOSE "
            + $"(Win32 error {Ntdll.RtlNtStatusToDosError(status)})");
        return Path.Combine(folder, leaf);
    }

    private static void AssertReadRefused(string file) =>
        AssertRefused(() => File.ReadAllBytes(file), "reading a file in the folder");

    private static void AssertReadAllowed(string file) =>
        AssertAllowed(() => File.ReadAllBytes(file), "reading a file in the folder");

    private static void AssertDeleteRefused(string file)
    {
        AssertRefused(() => File.Delete(file), "deleting a file in the folder");
        Assert.True(File.Exists(file), "a file whose delete was refused is gone");
    }

    private static void AssertRefused(Action act, string what)
    {
        var thrown = Record.Exception(act);
        Assert.True(thrown is UnauthorizedAccessException,
            $"{what} was expected to be refused (got {thrown?.GetType().Name ?? "no exception"})");
    }

    private static void AssertAllowed(Action act, string what)
    {
        var thrown = Record.Exception(act);
        Assert.True(thrown is null, $"{what} was expected to be allowed (got {thrown?.GetType().Name})");
    }

    [Fact]
    public void A_folder_the_account_cannot_list_fails_the_probe_at_the_open_of_the_folder()
    {
        AssertTheProbeAsksWhatThisTestAssumes();
        var accessList = ListGranting(FolderRightsWithoutList, FileRightsWriteOnly);

        // What Windows lets the account do there: its open of the folder is refused.
        var facts = MakeFolder(accessList);
        using (var opened = DirectoryGuard.OpenDirectoryFollow(facts, out var openCode))
        {
            Assert.True(opened is null, "the folder let this account open it for reading, so it is not a folder it cannot list");
            Assert.Equal(AccessDenied, openCode);
        }

        var folder = MakeFolder(accessList);
        Assert.False(_guard.TryProbeWritable(folder, out var failureCode, out var refusal));
        Assert.Equal(AccessDenied, failureCode);
        Assert.Equal(WriteProbeRefusal.FolderOpen, refusal);
    }

    [Fact]
    public void A_folder_the_account_cannot_add_a_file_to_fails_the_probe_at_the_create()
    {
        AssertTheProbeAsksWhatThisTestAssumes();
        var accessList = ListGranting(FolderRightsWithoutAdd, FileRightsReadWriteDelete);

        // What Windows lets the account do there: it can open the folder, and writing a
        // new file into it is refused.
        var facts = MakeFolder(accessList);
        AssertTheFolderOpens(facts);
        AssertRefused(() => File.WriteAllBytes(Path.Combine(facts, "copy.msi"), []), "adding a file to the folder");

        var folder = MakeFolder(accessList);
        Assert.False(_guard.TryProbeWritable(folder, out var failureCode, out var refusal));
        Assert.Equal(AccessDenied, failureCode);
        Assert.Equal(WriteProbeRefusal.CreateRefusedAccess, refusal);
        Assert.Empty(Directory.EnumerateFileSystemEntries(folder));
    }

    [Fact]
    public void A_folder_whose_files_cannot_be_read_fails_the_probe_at_the_read_back_and_is_left_empty()
    {
        AssertTheProbeAsksWhatThisTestAssumes();
        var accessList = ListGranting(FolderRights, FileRightsWriteOnly);

        // What Windows lets the account do there: it can put a file in, and cannot read it or
        // delete it, so discarding a copy there is refused too.
        var facts = MakeFolder(accessList);
        AssertTheFolderOpens(facts);
        var file = CreateFileLikeTheProbeLessDelete(facts, "copy.msi");
        AssertReadRefused(file);
        AssertDeleteRefused(file);
        Assert.False(_guard.TryDiscardCopy(file, out var discardCode));
        Assert.Equal(AccessDenied, discardCode);

        var folder = MakeFolder(accessList);
        Assert.False(_guard.TryProbeWritable(folder, out var failureCode, out var refusal));
        Assert.Equal(AccessDenied, failureCode);
        Assert.Equal(WriteProbeRefusal.ReadBackRefusedAccess, refusal);
        Assert.Empty(Directory.EnumerateFileSystemEntries(folder));
    }

    [Fact]
    public void A_folder_whose_files_give_their_bytes_and_not_the_rest_of_a_read_fails_the_probe_at_the_read_back()
    {
        AssertTheProbeAsksWhatThisTestAssumes();
        var accessList = ListGranting(FolderRights, FileRightsBytesWithoutExtendedAttributes);

        // What Windows lets the account do there: an open asking for a file's bytes alone
        // is granted, and a plain read, which asks for everything an open for reading
        // needs, is refused.
        var facts = MakeFolder(accessList);
        AssertTheFolderOpens(facts);
        var file = CreateFileLikeTheProbeLessDelete(facts, "copy.msi");
        using (var directory = DirectoryGuard.OpenDirectoryFollow(facts, out _))
        {
            Assert.NotNull(directory);
            using var bytesOnly = DirectoryGuard.OpenChildNoFollow(
                directory, "copy.msi", Ntdll.FILE_READ_DATA, Ntdll.FILE_OPEN, DirectoryGuard.ReadBackOptions, out var status);
            Assert.True(bytesOnly is not null,
                $"an open asking for the file's bytes alone was refused (Win32 error {Ntdll.RtlNtStatusToDosError(status)})");
        }
        AssertReadRefused(file);

        var folder = MakeFolder(accessList);
        Assert.False(_guard.TryProbeWritable(folder, out var failureCode, out var refusal));
        Assert.Equal(AccessDenied, failureCode);
        Assert.Equal(WriteProbeRefusal.ReadBackRefusedAccess, refusal);
        Assert.Empty(Directory.EnumerateFileSystemEntries(folder));
    }

    [Fact]
    public void A_folder_whose_files_can_be_read_and_not_deleted_passes_the_probe_and_is_left_empty()
    {
        AssertTheProbeAsksWhatThisTestAssumes();
        var accessList = ListGranting(FolderRights, FileRightsReadWrite);

        // What Windows lets the account do there: it can put a file in and read it, and
        // cannot delete it, so discarding a copy there is refused.
        var facts = MakeFolder(accessList);
        AssertTheFolderOpens(facts);
        var file = CreateFileLikeTheProbeLessDelete(facts, "copy.msi");
        AssertReadAllowed(file);
        AssertDeleteRefused(file);
        Assert.False(_guard.TryDiscardCopy(file, out var discardCode));
        Assert.Equal(AccessDenied, discardCode);

        var folder = MakeFolder(accessList);
        Assert.True(_guard.TryProbeWritable(folder, out var failureCode, out var refusal));
        Assert.Equal(0, failureCode);
        Assert.Equal(WriteProbeRefusal.None, refusal);
        Assert.Empty(Directory.EnumerateFileSystemEntries(folder));
    }

    [Fact]
    public void A_folder_whose_files_can_be_read_and_deleted_passes_the_probe_and_is_left_empty()
    {
        AssertTheProbeAsksWhatThisTestAssumes();
        var accessList = ListGranting(FolderRights, FileRightsReadWriteDelete);

        // What Windows lets the account do there: it can put a file in, read it and delete
        // it, and discarding a copy there removes it.
        var facts = MakeFolder(accessList);
        AssertTheFolderOpens(facts);
        var file = CreateFileLikeTheProbeLessDelete(facts, "copy.msi");
        AssertReadAllowed(file);
        AssertAllowed(() => File.Delete(file), "deleting a file in the folder");
        Assert.False(File.Exists(file));
        var copy = CreateFileLikeTheProbeLessDelete(facts, "copy-2.msi");
        Assert.True(_guard.TryDiscardCopy(copy, out var discardCode));
        Assert.Equal(0, discardCode);
        Assert.False(File.Exists(copy));

        var folder = MakeFolder(accessList);
        Assert.True(_guard.TryProbeWritable(folder, out var failureCode, out var refusal));
        Assert.Equal(0, failureCode);
        Assert.Equal(WriteProbeRefusal.None, refusal);
        Assert.Empty(Directory.EnumerateFileSystemEntries(folder));
    }
}
