using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;
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

    // ---- The withholding fires ----

    [Fact]
    public async Task A_candidate_whose_own_product_Windows_still_holds_is_not_offered()
    {
        // THE ROUTE THIS EXISTS FOR. No registration names the file, so the path
        // comparison and the file-identity match have both let it through; the
        // package itself says which product it belongs to and Windows still has
        // that product.
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

    // ---- The restriction ----

    [Fact]
    public async Task A_patch_candidate_is_offered_beside_a_package_the_screen_keeps()
    {
        // THE CONTROL ON THE PRODUCT-PACKAGE RESTRICTION, and the fixture is
        // hostile on purpose: the identity reader would throw if the patch were
        // read at all, so the test can only pass if the screen never touched it.
        // The package beside it is kept by the same screen in the same run, so the
        // patch being offered is the restriction working and not the screen being
        // absent.
        //
        // Asked of a patch, the keeping arm is true of every registered superseded
        // patch on every machine, which is the whole class the offer's other half
        // is made of.
        var identities = new ScriptedPackageIdentities();
        identities.Declares($@"{Folder}\held.msi", ProductA);   // the patch is deliberately unscripted

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var result = await Scan(new[] { $@"{Folder}\orphan.msp", $@"{Folder}\held.msi" },
            msi, identities);

        var offered = Assert.Single(result.RemovableFiles);
        Assert.Equal($@"{Folder}\orphan.msp", offered.FullPath);
        Assert.Single(result.WithheldFiles!);
        Assert.DoesNotContain($@"{Folder}\orphan.msp", identities.Reads);
    }

    [Fact]
    public async Task A_registered_superseded_row_is_never_put_to_the_screen()
    {
        // The same restriction from the side that matters most. A superseded patch
        // reaches the offer from the REGISTERED set without ever having been a
        // walk candidate, so the screen should not see it, and this asserts that
        // directly rather than through whatever the superseded branch currently
        // does with the row. The reader throws on any path no test scripted, so a
        // screen that reached for it would fail the run rather than quietly
        // withhold.
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

    private static IInstallerQueryService QueryReturning(IReadOnlyList<RegisteredPackage> registered)
    {
        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(
                Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered));
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
        IReadOnlyList<RegisteredPackage>? registered = null)
    {
        var files = walked.ToArray();
        var fs = FolderHolding(files.Concat(
            (registered ?? Array.Empty<RegisteredPackage>()).Select(p => p.LocalPackagePath)).ToArray());

        return new FileSystemScanService(
            QueryReturning(registered ?? Array.Empty<RegisteredPackage>()), fs, null,
            files, null, null,
            new DeclaredProductCheck(msi, identities))
            .ScanAsync();
    }
}
