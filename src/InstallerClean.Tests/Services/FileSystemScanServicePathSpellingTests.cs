using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// How the scan decides that a walked file is the one a registration names, when
/// the registration's recorded path and the walk's path are not the same string.
///
/// THIS IS THE GATE THE WHOLE OFFER RESTS ON. A registration the scan fails to
/// match does not make its file safe to remove; it makes the file a candidate, and
/// the only thing left between it and the user is the identity check. Everything
/// here is about not needing that second chance.
///
/// Two mechanisms, pinned separately because they close different things. The
/// comparer handles case, which is the divergence real machines actually produce.
/// The file-identity reader handles every other spelling at once, which matters
/// because the list of other spellings is somebody's enumeration and nothing says
/// when it is complete.
/// </summary>
public class FileSystemScanServicePathSpellingTests
{
    private const string CacheRoot = @"C:\Windows\Installer";
    private const string Walked = @"C:\Windows\Installer\9f05cba.msi";
    private const string Orphan = @"C:\Windows\Installer\orphan.msi";

    // ---- The comparer, which is what actually carries real machines ----

    [Theory]
    // The three spellings one machine's own records carry for one folder, counted
    // 2026-08-11 across its 138 registrations: 121 upper, 15 mixed, 2 with a
    // lower-case drive letter. Windows Installer records whatever spelling was in
    // force when the product was installed and normalises nothing, so all three
    // are ordinary and none of them is the spelling the folder walk produces.
    [InlineData(@"C:\WINDOWS\Installer\9f05cba.msi")]
    [InlineData(@"C:\Windows\Installer\9f05cba.msi")]
    [InlineData(@"c:\Windows\Installer\9f05cba.msi")]
    [InlineData(@"C:\windows\installer\9F05CBA.MSI")]
    public async Task A_registration_spelled_in_another_case_still_claims_its_file(string registeredAs)
    {
        // On the machine this was measured on, 123 of 138 registrations differ
        // from the walk's spelling by case alone. An ordinal comparison would put
        // every one of those files into the candidate set on a single scan, so
        // this is not a nicety about tidy strings: it is the difference between
        // the app claiming its registrations and the app handing the lot to the
        // identity check to rescue.
        var result = await Scan(
            walkedFiles: new[] { Walked },
            registeredPaths: new[] { registeredAs });

        Assert.Empty(result.RemovableFiles);
    }

    [Fact]
    public async Task A_file_no_registration_names_is_still_offered()
    {
        // The must-fail half of the pair. Without it the case theory above passes
        // just as well against a scan that offers nothing at all, which is the
        // shape a broken gate would have.
        var result = await Scan(
            walkedFiles: new[] { Walked, Orphan },
            registeredPaths: new[] { @"C:\WINDOWS\Installer\9f05cba.msi" });

        var offered = Assert.Single(result.RemovableFiles);
        Assert.Equal(Orphan, offered.FullPath);
    }

    // ---- The file-identity reader, which closes the spellings nobody has seen ----

    [Fact]
    public async Task A_candidate_that_is_the_same_FILE_as_a_registration_is_not_offered()
    {
        // The registration is recorded through a junction, so no comparison of
        // strings can match it, and the app's own resolution does not flag that
        // form. Asking the filesystem which file each path names settles it.
        const string throughAJunction = @"C:\Junction\9f05cba.msi";
        var ids = Identities(
            (throughAJunction, 1),
            (Walked, 1),
            (Orphan, 2));

        var result = await Scan(
            walkedFiles: new[] { Walked, Orphan },
            registeredPaths: new[] { throughAJunction },
            fileIds: ids);

        var offered = Assert.Single(result.RemovableFiles);
        Assert.Equal(Orphan, offered.FullPath);
    }

    [Fact]
    public async Task A_candidate_that_is_a_different_file_is_still_offered()
    {
        // The must-fail control for the mechanism above: a reader that answered
        // "same file" to everything would pass the previous test and empty every
        // offer, and the two look identical from the outside.
        var ids = Identities(
            (@"C:\Elsewhere\other.msi", 7),
            (Orphan, 8));

        var result = await Scan(
            walkedFiles: new[] { Orphan },
            registeredPaths: new[] { @"C:\Elsewhere\other.msi" },
            fileIds: ids);

        Assert.Single(result.RemovableFiles);
    }

    [Fact]
    public async Task A_registration_whose_path_will_not_open_leaves_the_candidate_alone()
    {
        // No identity for the registration means nothing to compare against, and
        // the candidate goes on exactly as it did before this mechanism existed.
        // The failure direction is the whole safety argument for adding it.
        var ids = Identities((Orphan, 3));   // the registration is absent from the map

        var result = await Scan(
            walkedFiles: new[] { Orphan },
            registeredPaths: new[] { @"C:\Gone\missing.msi" },
            fileIds: ids);

        Assert.Single(result.RemovableFiles);
    }

    [Fact]
    public async Task A_candidate_that_will_not_open_stays_a_candidate()
    {
        // The other half of the same argument. A candidate with no readable
        // identity cannot be shown to be a registered file, and an unreadable
        // answer must never be read as "not the same file, therefore offer it"
        // any more than as "the same file, therefore keep it": it leaves the
        // decision where it was.
        var ids = Identities((@"C:\Elsewhere\other.msi", 9));   // the candidate is absent

        var result = await Scan(
            walkedFiles: new[] { Orphan },
            registeredPaths: new[] { @"C:\Elsewhere\other.msi" },
            fileIds: ids);

        Assert.Single(result.RemovableFiles);
    }

    [Fact]
    public async Task No_reader_at_all_leaves_the_scan_exactly_as_it_was()
    {
        // Every other test in the suite constructs the scan without one, so this
        // pins what that means rather than leaving it to be assumed: the string
        // comparison alone, and an offer identical to the one made before any of
        // this existed.
        var result = await Scan(
            walkedFiles: new[] { Orphan },
            registeredPaths: new[] { @"C:\Elsewhere\other.msi" },
            fileIds: null);

        Assert.Single(result.RemovableFiles);
    }

    // ---- Helpers ----

    /// <summary>
    /// A reader answering from a map of path to file number. Two paths carrying
    /// the same number are the same file; a path not in the map has no answer at
    /// all, which is what a handle that will not open produces.
    ///
    /// The volume serial is fixed and the id's high half is zero, so the tests
    /// vary the one field they are about. Both halves being part of the value is
    /// pinned by the type rather than here.
    /// </summary>
    private static IFileIdentityReader Identities(params (string Path, ulong File)[] entries)
    {
        var map = new Dictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, file) in entries) map[path] = file;

        var reader = Substitute.For<IFileIdentityReader>();
        reader.TryRead(Arg.Any<string>(), out Arg.Any<FileIdentity>())
            .Returns(call =>
            {
                var path = (string?)call[0] ?? string.Empty;
                if (!map.TryGetValue(path, out var file))
                {
                    call[1] = default(FileIdentity);
                    return false;
                }
                call[1] = new FileIdentity(VolumeSerialNumber: 1, FileIdLow: file, FileIdHigh: 0);
                return true;
            });
        return reader;
    }

    private static async Task<ScanResult> Scan(
        string[] walkedFiles,
        string[] registeredPaths,
        IFileIdentityReader? fileIds = null)
    {
        var fs = new MockFileSystem();
        fs.AddDirectory(CacheRoot);
        foreach (var path in walkedFiles) fs.AddFile(path, new MockFileData("x"));

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(
                Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new InstallerQueryResult(
                registeredPaths.Select(p => new RegisteredPackage(p, "Product", "{code}")).ToList())));

        return await new FileSystemScanService(
            query, fs, PermissiveIdentityVeto.Instance, null, walkedFiles, CacheRoot, fileIds)
            .ScanAsync();
    }
}
