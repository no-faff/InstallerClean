using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The declared-product screen driven through a real scan: the real
/// <see cref="DeclaredProductCheck"/>, the real
/// <see cref="FileSystemScanService"/>, and fakes only at the two seams that
/// reach Windows. What <see cref="DeclaredProductCheckTests"/> pins is the
/// screen's own verdicts; what these pin is that the scan asks it, acts on the
/// answer, and accounts for what it keeps back.
///
/// READ THIS BEFORE COPYING A FIXTURE HERE. The screen is an OPTIONAL
/// collaborator and the scan's test constructor DEFAULTS IT TO NULL, which means
/// a scan built without one screens nothing at all. So an assertion that a file
/// was NOT held back proves nothing on its own: it passes identically against a
/// working screen that let the file through and against a scan that has no screen
/// to speak of. Every "not held back" test below injects a screen, and the same
/// screen is shown holding something else back in the same fixture or in the test
/// beside it. Keep that pairing. It is the difference between a test about this
/// code and a test about nothing.
/// </summary>
public class FileSystemScanServiceDeclaredProductTests
{
    private const string Folder = @"C:\Windows\Installer";
    private const string ProductA = "{11111111-1111-1111-1111-111111111111}";
    private const string ProductB = "{22222222-2222-2222-2222-222222222222}";

    // A source folder outside the Installer folder. The screen resolves a source's
    // package against the real disk, and a package that is not there resolves through
    // the nearest folder that is, so this needs only the C: drive.
    private const string SetupFolder = @"C:\Setup\";

    // ---- The withholding fires ----

    [Fact]
    public async Task A_candidate_whose_own_product_Windows_still_holds_is_not_offered()
    {
        // THE ROUTE THIS EXISTS FOR. No registration names the file, so the path
        // comparison and the file-identity match have both let it through; the
        // package itself says which product it belongs to and Windows still has
        // that product. The screen here is built without its file readers, so it
        // cannot look at what the product records; the two tests after this one
        // give it both readers.
        var identities = new ScriptedPackageIdentities();
        identities.Declares($@"{Folder}\held.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var result = await Scan(new[] { $@"{Folder}\held.msi" }, msi, identities);

        Assert.Empty(result.RemovableFiles);
        var kept = Assert.Single(result.WithheldFiles!);
        Assert.Equal($@"{Folder}\held.msi", kept.FullPath);
    }

    [Fact]
    public async Task A_copy_beside_the_package_its_installed_product_records_is_offered()
    {
        // The pair of the test above, with the same product installed. b.msi is the
        // package product A records, so the scan's own path comparison claims it and
        // it is never a candidate. a.msi declares product A and no record names it.
        // The screen reads the package A records, finds a present file that is not
        // a.msi and declares product A, finds A's source outside the Installer folder
        // with no package there, and lets a.msi through; the two tests below scan the
        // same two files and keep a.msi, one when what A records cannot be seen and one
        // when A was installed from the Installer folder.
        var identities = new ScriptedPackageIdentities();
        identities.Declares($@"{Folder}\a.msi", ProductA);
        identities.Declares($@"{Folder}\b.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.RecordsPackage(ProductA, null, MsiInstallContext.Machine, $@"{Folder}\b.msi");
        msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, "setup.msi", SetupFolder);

        var files = new ScriptedFileIdentities();
        files.Opens($@"{Folder}\a.msi", 1);
        files.Opens($@"{Folder}\b.msi", 2);
        files.Answers(SetupFolder + "setup.msi", FileIdentityRead.NamesNothing);

        var result = await ScanWithRecordedPackage(msi, identities, files);

        var offered = Assert.Single(result.RemovableFiles);
        Assert.Equal($@"{Folder}\a.msi", offered.FullPath);
        Assert.Empty(result.WithheldFiles!);
    }

    [Fact]
    public async Task A_copy_whose_program_was_installed_from_the_Installer_folder_is_kept_by_the_same_screen()
    {
        // The scan above with product A's source in the Installer folder. The screen
        // compares a source against the Installer folder only through what the scan
        // hands it, which is the folder the scan resolved for the run, so a.msi is
        // kept here and offered above only if the scan hands it that.
        var identities = new ScriptedPackageIdentities();
        identities.Declares($@"{Folder}\a.msi", ProductA);
        identities.Declares($@"{Folder}\b.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.RecordsPackage(ProductA, null, MsiInstallContext.Machine, $@"{Folder}\b.msi");
        msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, "c.msi", $@"{Folder}\");

        var files = new ScriptedFileIdentities();
        files.Opens($@"{Folder}\a.msi", 1);
        files.Opens($@"{Folder}\b.msi", 2);

        var result = await ScanWithRecordedPackage(msi, identities, files);

        Assert.Empty(result.RemovableFiles);
        var kept = Assert.Single(result.WithheldFiles!);
        Assert.Equal($@"{Folder}\a.msi", kept.FullPath);
        Assert.Equal(1, result.WithheldBy.DeclaredProductInstalledCount);
    }

    [Fact]
    public void The_scan_the_hosts_build_screens_declared_products()
    {
        // Constructed by hand everywhere else in this file, where the default is no
        // screen. The container is what the hosts use.
        using var services = new ServiceCollection().AddInstallerCleanCore().BuildServiceProvider();

        var scan = Assert.IsType<FileSystemScanService>(services.GetRequiredService<IFileSystemScanService>());

        Assert.True(scan.ScreensDeclaredProducts);
    }

    [Fact]
    public async Task A_copy_whose_installed_product_records_no_package_is_kept_by_the_same_screen()
    {
        // The same scan as the test above, with product A recording no package. The
        // screen cannot see which package A opens, so a.msi could be it.
        var identities = new ScriptedPackageIdentities();
        identities.Declares($@"{Folder}\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.RecordsPackage(ProductA, null, MsiInstallContext.Machine, "");

        var result = await ScanWithRecordedPackage(msi, identities, new ScriptedFileIdentities());

        Assert.Empty(result.RemovableFiles);
        var kept = Assert.Single(result.WithheldFiles!);
        Assert.Equal($@"{Folder}\a.msi", kept.FullPath);
        Assert.Equal(1, result.WithheldBy.DeclaredProductInstalledCount);
        // The file's size goes to the declared-product-installed arm's byte
        // figure, so the held-back sentences, which leave this file out, are left
        // nothing to count.
        Assert.Equal(kept.SizeBytes, result.WithheldDeclaredProductInstalledBytes);
        Assert.Equal(0, result.UnestablishedWithheldCount);
        Assert.Equal(0, result.UnestablishedWithheldBytes);
        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
    }

    /// <summary>
    /// A scan of a folder holding a.msi and b.msi, where b.msi is registered to
    /// product A and a.msi is not registered, with the screen given both file
    /// readers.
    /// </summary>
    private static Task<ScanResult> ScanWithRecordedPackage(
        ScriptedMsiProducts msi,
        ScriptedPackageIdentities identities,
        ScriptedFileIdentities files)
    {
        var fs = FolderHolding($@"{Folder}\a.msi", $@"{Folder}\b.msi");
        var registered = new[] { new RegisteredPackage($@"{Folder}\b.msi", "Product A", ProductA) };

        return new FileSystemScanService(
            QueryReturning(registered), fs, null,
            new[] { $@"{Folder}\a.msi", $@"{Folder}\b.msi" }, null, null,
            new DeclaredProductCheck(msi, identities, files, fs, msi.Registry))
            .ScanAsync();
    }

    [Fact]
    public async Task A_candidate_the_screen_cannot_read_is_not_offered()
    {
        // The other keeping arm, and the one an earlier design of this work got
        // backwards. A file that would not give up an identity has not been shown
        // to be spare by anybody, and "could not read it" is not "nothing claims
        // it".
        var identities = new ScriptedPackageIdentities();
        identities.YieldsNothing($@"{Folder}\unreadable.msi");

        var result = await Scan(new[] { $@"{Folder}\unreadable.msi" },
            new ScriptedMsiProducts(), identities);

        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.WithheldFiles!);
    }

    [Fact]
    public async Task A_candidate_whose_question_Windows_would_not_answer_is_not_offered()
    {
        // The third keeping arm. The code was read and the call failed, so nothing
        // was established about the machine; the file is kept for want of an
        // answer rather than on one.
        var identities = new ScriptedPackageIdentities();
        identities.Declares($@"{Folder}\unaskable.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Answers(ProductA, MsiError.AccessDenied);

        var result = await Scan(new[] { $@"{Folder}\unaskable.msi" }, msi, identities);

        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.WithheldFiles!);
    }

    // ---- The must-miss half, in the same fixture as a must-hit ----

    [Fact]
    public async Task A_candidate_Windows_says_it_does_not_hold_is_still_offered()
    {
        // THE PAIR, IN ONE SCAN. A screen that kept everything back would pass the
        // three tests above and empty the offer on every machine, and from outside
        // that looks exactly like a screen doing its job. Here one file is kept and
        // one is offered by the same screen in the same run, so neither answer can
        // be the fixture's.
        var identities = new ScriptedPackageIdentities();
        identities.Declares($@"{Folder}\held.msi", ProductA);
        identities.Declares($@"{Folder}\gone.msi", ProductB);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.NotInstalled(ProductB, MsiError.UnknownProduct);

        var result = await Scan(new[] { $@"{Folder}\held.msi", $@"{Folder}\gone.msi" },
            msi, identities);

        var offered = Assert.Single(result.RemovableFiles);
        Assert.Equal($@"{Folder}\gone.msi", offered.FullPath);

        var kept = Assert.Single(result.WithheldFiles!);
        Assert.Equal($@"{Folder}\held.msi", kept.FullPath);
    }

    // ---- The installations the scan's own enumeration listed ----

    [Fact]
    public async Task A_candidate_whose_listed_product_Windows_answers_not_installed_is_not_offered()
    {
        // The scan hands the screen the installations its enumeration listed. One folder
        // is scanned twice, with Windows answering that neither product is installed:
        // where the enumeration listed nothing both files are offered, and where it
        // listed product A the file declaring A is kept and the other is offered.
        var identities = new ScriptedPackageIdentities();
        identities.Declares($@"{Folder}\listed.msi", ProductA);
        identities.Declares($@"{Folder}\gone.msi", ProductB);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.UnknownProduct);
        msi.NotInstalled(ProductB, MsiError.UnknownProduct);

        var walked = new[] { $@"{Folder}\listed.msi", $@"{Folder}\gone.msi" };

        var unlisted = await Scan(walked, msi, identities);

        Assert.Equal(walked, unlisted.RemovableFiles.Select(f => f.FullPath));
        Assert.Empty(unlisted.WithheldFiles!);

        var listed = await Scan(walked, msi, identities,
            installations: [new ListedInstallation(ProductA, null, (int)MsiInstallContext.Machine)]);

        var offered = Assert.Single(listed.RemovableFiles);
        Assert.Equal($@"{Folder}\gone.msi", offered.FullPath);
        var kept = Assert.Single(listed.WithheldFiles!);
        Assert.Equal($@"{Folder}\listed.msi", kept.FullPath);
        Assert.Equal(1, listed.WithheldBy.DeclaredProductUnestablishedCount);
    }

    // ---- A patch copy and its patch's registrations ----

    [Fact]
    public async Task A_patch_copy_whose_registration_records_no_copy_is_kept_without_a_notice()
    {
        // copy.msp declares patch Q, which Windows holds registered against product A,
        // and that registration records no cached copy. The screen cannot see which
        // copy the registration opens, so copy.msp could be it. It is treated the way
        // a file kept for an installed program is: counted in its own arm with its
        // size, and left out of the held-back sentences.
        var (msi, identities, files) = APatchCopyBesideTheRecordedCopy();
        msi.RecordsPatchPackage(PatchQ, ProductA, null, MsiInstallContext.Machine, "");

        var result = await ScanWithRecordedPatch(msi, identities, files);

        Assert.Empty(result.RemovableFiles);
        var kept = Assert.Single(result.WithheldFiles!);
        Assert.Equal($@"{Folder}\copy.msp", kept.FullPath);
        Assert.Equal(1, result.WithheldBy.DeclaredPatchRegisteredCount);
        Assert.Equal(result.WithheldFiles!.Count, result.WithheldBy.Total);
        Assert.Equal(kept.SizeBytes, result.WithheldDeclaredPatchRegisteredBytes);
        Assert.Equal(0, result.UnestablishedWithheldCount);
        Assert.Equal(0, result.UnestablishedWithheldBytes);
        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
    }

    [Fact]
    public async Task A_patch_copy_beside_the_copy_its_registration_records_is_offered()
    {
        // The pair of the test above, the same two files and the same screen. The
        // registration records cached.msp, which the scan's own path comparison claims,
        // and the screen finds it present, a different file from copy.msp, and patch Q,
        // and finds Q's source outside the Installer folder with no package there.
        var (msi, identities, files) = APatchCopyBesideTheRecordedCopy();
        msi.RecordsPatchPackage(PatchQ, ProductA, null, MsiInstallContext.Machine, $@"{Folder}\cached.msp");

        var result = await ScanWithRecordedPatch(msi, identities, files);

        var offered = Assert.Single(result.RemovableFiles);
        Assert.Equal($@"{Folder}\copy.msp", offered.FullPath);
        Assert.Empty(result.WithheldFiles!);
        Assert.Contains($@"{Folder}\copy.msp", identities.PatchReads);
    }

    [Fact]
    public async Task A_patch_copy_its_patch_was_applied_from_is_kept_by_the_same_screen()
    {
        // The scan above with patch Q's source in the Installer folder and its package
        // name copy.msp: the patch was applied from the copy in the cache, and nothing
        // but the source list names it. The screen compares a source against the
        // Installer folder only through what the scan hands it, which is the folder the
        // scan resolved for the run, so copy.msp is kept here and offered above only if
        // the scan hands it that.
        var (msi, identities, files) = APatchCopyBesideTheRecordedCopy();
        msi.RecordsPatchPackage(PatchQ, ProductA, null, MsiInstallContext.Machine, $@"{Folder}\cached.msp");
        msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, "copy.msp", $@"{Folder}\");

        var result = await ScanWithRecordedPatch(msi, identities, files);

        Assert.Empty(result.RemovableFiles);
        var kept = Assert.Single(result.WithheldFiles!);
        Assert.Equal($@"{Folder}\copy.msp", kept.FullPath);
        Assert.Equal(1, result.WithheldBy.DeclaredPatchRegisteredCount);
        Assert.Equal(kept.SizeBytes, result.WithheldDeclaredPatchRegisteredBytes);
        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
    }

    [Fact]
    public async Task A_patch_copy_whose_registrations_cannot_be_found_is_kept_and_spoken_of()
    {
        // The scan that offers copy.msp, with the machine-wide patch enumeration refusing
        // at its first row. The screen cannot find the registrations of patch Q, so
        // copy.msp is kept, counted in the patch half's unsettled arm with no byte
        // figure of its own, and among the files the held-back sentences speak of,
        // with a reason line of its own.
        var (msi, identities, files) = APatchCopyBesideTheRecordedCopy();
        msi.RecordsPatchPackage(PatchQ, ProductA, null, MsiInstallContext.Machine, $@"{Folder}\cached.msp");
        msi.PatchEnumerationAnswersAt(0, MsiError.AccessDenied);

        var result = await ScanWithRecordedPatch(msi, identities, files);

        Assert.Empty(result.RemovableFiles);
        var kept = Assert.Single(result.WithheldFiles!);
        Assert.Equal($@"{Folder}\copy.msp", kept.FullPath);
        Assert.Equal(1, result.WithheldBy.DeclaredPatchUnestablishedCount);
        Assert.Equal(result.WithheldFiles!.Count, result.WithheldBy.Total);
        Assert.Equal(0, result.WithheldDeclaredPatchRegisteredBytes);
        Assert.Equal(1, result.UnestablishedWithheldCount);
        Assert.Equal(kept.SizeBytes, result.UnestablishedWithheldBytes);
        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.Equal(new[] { WithholdingSplitArm.DeclaredPatchUnestablished }, result.WithheldBy.ArmsFired);
        Assert.True(result.NamedConditionsCoverEveryHeldBackFile);
    }

    private const string PatchQ = "{33333333-3333-3333-3333-333333333333}";

    /// <summary>
    /// Patch Q, declaring product A as its target, registered against A's one
    /// per-machine installation and applied from fix.msp in a folder outside the
    /// Installer folder, the file no longer being there. copy.msp and cached.msp both
    /// declare Q and open as two different files. What the registration records is each
    /// test's to script.
    /// </summary>
    private static (ScriptedMsiProducts Msi, ScriptedPackageIdentities Identities, ScriptedFileIdentities Files)
        APatchCopyBesideTheRecordedCopy()
    {
        var identities = new ScriptedPackageIdentities();
        identities.DeclaresPatch($@"{Folder}\copy.msp", PatchQ, ProductA);
        identities.DeclaresPatch($@"{Folder}\cached.msp", PatchQ, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.HoldsPatch(PatchQ, ProductA, null, MsiInstallContext.Machine);
        msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, "fix.msp", SetupFolder);

        var files = new ScriptedFileIdentities();
        files.Opens($@"{Folder}\copy.msp", 1);
        files.Opens($@"{Folder}\cached.msp", 2);
        files.Answers(SetupFolder + "fix.msp", FileIdentityRead.NamesNothing);

        return (msi, identities, files);
    }

    /// <summary>
    /// A scan of a folder holding copy.msp and cached.msp, where cached.msp is
    /// registered as a patch applied to product A and copy.msp is not registered, with
    /// the screen given both file readers.
    /// </summary>
    private static Task<ScanResult> ScanWithRecordedPatch(
        ScriptedMsiProducts msi,
        ScriptedPackageIdentities identities,
        ScriptedFileIdentities files)
    {
        var fs = FolderHolding($@"{Folder}\copy.msp", $@"{Folder}\cached.msp");
        var registered = new[] { new RegisteredPackage($@"{Folder}\cached.msp", "Product A", ProductA, PatchState: 1) };

        return new FileSystemScanService(
            QueryReturning(registered), fs, null,
            new[] { $@"{Folder}\copy.msp", $@"{Folder}\cached.msp" }, null, null,
            new DeclaredProductCheck(msi, identities, files, fs, msi.Registry))
            .ScanAsync();
    }

    // ---- The superseded half ----

    [Fact]
    public async Task A_registered_superseded_row_is_never_put_to_the_screen()
    {
        // A superseded patch reaches the offer from the REGISTERED set without ever
        // having been a walk candidate, so the screen does not see it, and this
        // asserts that directly rather than through whatever the superseded branch
        // does with the row. It matters because the screen keeps a registered patch's
        // own cached file: that patch's registrations record that very file. The
        // reader throws on any path no test scripted, so a screen that reached for it
        // would fail the run rather than quietly withhold.
        var registered = new List<RegisteredPackage>
        {
            new($@"{Folder}\superseded.msp", "Test Product", ProductB, PatchState: 2),
        };

        var identities = new ScriptedPackageIdentities();
        identities.Declares($@"{Folder}\held.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var result = await Scan(new[] { $@"{Folder}\held.msi" }, msi, identities, registered);

        Assert.Single(result.WithheldFiles!);
        Assert.Equal(new[] { $@"{Folder}\held.msi" }, identities.Reads);
    }

    // ---- What a scan with no screen does, pinned rather than assumed ----

    [Fact]
    public async Task A_scan_built_without_a_screen_screens_nothing()
    {
        // THIS TEST'S ONLY JOB IS TO FAIL IF THE DEFAULT EVER STOPS BEING NULL.
        // Every test in the suite that is not about this screen omits it, so what
        // the omission means is load-bearing for all of them: it has to leave the
        // offer exactly as it was before the screen existed. A default that
        // injected a screen of any kind would change what those tests measure
        // without changing a line of them.
        var query = QueryReturning(Array.Empty<RegisteredPackage>());
        var fs = FolderHolding($@"{Folder}\a.msi");

        var result = await new FileSystemScanService(
            query, fs, null, new[] { $@"{Folder}\a.msi" }, null, null)
            .ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Empty(result.WithheldFiles!);
    }

    // ---- Accounting ----

    [Fact]
    public async Task Both_lists_come_back_in_walk_order()
    {
        // The withheld list is shown to somebody: it is the second group in the
        // Details window and its bytes are in the main window's left-alone line.
        // The partition is built by walking the candidates once and appending to
        // two lists, and an implementation that removed in place from the back
        // would hand back one of them reversed.
        var identities = new ScriptedPackageIdentities();
        var walked = new List<string>();
        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.NotInstalled(ProductB, MsiError.UnknownProduct);

        for (var i = 0; i < 4; i++)
        {
            var held = $@"{Folder}\held{i}.msi";
            var gone = $@"{Folder}\gone{i}.msi";
            identities.Declares(held, ProductA);
            identities.Declares(gone, ProductB);
            walked.Add(held);
            walked.Add(gone);
        }

        var result = await Scan(walked, msi, identities);

        Assert.Equal(
            new[] { $@"{Folder}\held0.msi", $@"{Folder}\held1.msi", $@"{Folder}\held2.msi", $@"{Folder}\held3.msi" },
            result.WithheldFiles!.Select(f => f.FullPath));
        Assert.Equal(
            new[] { $@"{Folder}\gone0.msi", $@"{Folder}\gone1.msi", $@"{Folder}\gone2.msi", $@"{Folder}\gone3.msi" },
            result.RemovableFiles.Select(f => f.FullPath));
    }

    [Fact]
    public async Task A_kept_candidate_keeps_its_size_so_the_two_summary_lines_still_add_up()
    {
        // A withheld file is in neither the offer nor the registered set, so the
        // main window's two lines would account for less than the folder holds if
        // its bytes did not travel with it. They are read straight off this list.
        var identities = new ScriptedPackageIdentities();
        identities.Declares($@"{Folder}\held.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var fs = new MockFileSystem();
        fs.AddDirectory(Folder);
        fs.AddFile($@"{Folder}\held.msi", new MockFileData(new byte[4096]));

        var result = await new FileSystemScanService(
            QueryReturning(Array.Empty<RegisteredPackage>()), fs, null,
            new[] { $@"{Folder}\held.msi" }, null, null,
            new DeclaredProductCheck(msi, identities))
            .ScanAsync();

        var kept = Assert.Single(result.WithheldFiles!);
        Assert.Equal(4096, kept.SizeBytes);
    }

    [Fact]
    public async Task A_machine_whose_records_hold_an_unspellable_path_keeps_everything_without_screening_it()
    {
        // The two withholdings meet here, and the outcome must be the one the
        // wider rule already reaches. An unspellable claim keeps the whole
        // walk-derived set, so the screen is skipped rather than run and thrown
        // away: both fakes throw on anything unscripted, so this test fails if it
        // runs at all.
        var census = new EnumerationCensus(PathNormalisationRefusedAtEmbeddedNullCount: 1);
        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(
                Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                Array.Empty<RegisteredPackage>(), Census: census));

        var result = await new FileSystemScanService(
            query, FolderHolding($@"{Folder}\a.msi"), null,
            new[] { $@"{Folder}\a.msi" }, null, null,
            new DeclaredProductCheck(new ScriptedMsiProducts(), new ScriptedPackageIdentities()))
            .ScanAsync();

        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.WithheldFiles!);
    }

    // ---- Helpers ----

    private static IInstallerQueryService QueryReturning(
        IReadOnlyList<RegisteredPackage> registered, IReadOnlyList<ListedInstallation>? installations = null)
    {
        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(
                Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered, Installations: installations));
        return query;
    }

    private static MockFileSystem FolderHolding(params string[] paths)
    {
        var fs = new MockFileSystem();
        fs.AddDirectory(Folder);
        foreach (var path in paths) fs.AddFile(path, new MockFileData(new byte[100]));
        return fs;
    }

    /// <summary>
    /// One scan over <paramref name="walked"/>, with the REAL screen driven by the
    /// two scripted seams. Nothing here stubs the check itself: a fake screen
    /// would pin the scan's wiring and leave the thing being wired untested.
    /// </summary>
    private static Task<ScanResult> Scan(
        IEnumerable<string> walked,
        ScriptedMsiProducts msi,
        ScriptedPackageIdentities identities,
        IReadOnlyList<RegisteredPackage>? registered = null,
        IReadOnlyList<ListedInstallation>? installations = null)
    {
        var files = walked.ToArray();
        var fs = FolderHolding(files.Concat(
            (registered ?? Array.Empty<RegisteredPackage>()).Select(p => p.LocalPackagePath)).ToArray());

        return new FileSystemScanService(
            QueryReturning(registered ?? Array.Empty<RegisteredPackage>(), installations), fs, null,
            files, null, null,
            new DeclaredProductCheck(msi, identities))
            .ScanAsync();
    }
}
