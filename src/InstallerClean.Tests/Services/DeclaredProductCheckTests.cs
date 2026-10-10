using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The scan's screen of what a cached file says it is: the product an installation
/// package declares it belongs to, or the patch a patch file declares it is, put to
/// Windows.
///
/// IT IS THE ONLY THING IN THE TREE THAT STARTS AT THE FILE. Every other way the
/// scan decides a cached file is spare starts at a registration and looks for the
/// file it names, so where the records hold no usable path there is nothing for any
/// of them to work from. That is the class this reaches, and the tests below are
/// about the direction it fails in. For a package two answers let a file through: a
/// POSITIVE answer that Windows does not hold the declared product, and every
/// installation of that product recording a package that is present and is another
/// file, with none of them per user and unmanaged and no source of it reaching the
/// file. Every source list read has to be held in the registry as the API returns it and
/// hold no URL. For a patch the same two: a POSITIVE answer that Windows holds no
/// registration of the declared patch, and every registration of it recording a cached
/// copy that is present and is another file. Every inability keeps the file.
///
/// THE FAKES THROW ON ANYTHING NO TEST SCRIPTED, which is the point of them rather
/// than strictness. A fake answering an unscripted question with a plausible default
/// is how a test comes to pass without ever reaching its own subject, and the
/// questions here have a permissive answer each: "Windows does not hold that
/// product", "no product holds that patch" and "there was nothing to read". Any of
/// them as a default would let a test assert the file was offered while proving
/// nothing about why.
/// </summary>
public class DeclaredProductCheckTests
{
    private const string ProductA = "{11111111-1111-1111-1111-111111111111}";
    private const string ProductB = "{22222222-2222-2222-2222-222222222222}";

    private static OrphanedFile Package(string path) =>
        new(path, 100, IsPatch: false, IsRemovablePatch: false, IsObsoleted: false, Reason: "orphaned");

    private static OrphanedFile Patch(string path) =>
        new(path, 100, IsPatch: true, IsRemovablePatch: false, IsObsoleted: false, Reason: "orphaned");

    /// <summary>
    /// The check as the tests here build it, with the two parts that ask the machine
    /// scripted: every drive letter answers as a fixed drive (<see cref="FixedDrive"/>), and
    /// every candidate's folder entry holds the last component of its path alone
    /// (<see cref="NameOnly"/>). No test reads the drives or the Installer folder of the
    /// machine it runs on; a test that sets its own options sets these two as well.
    /// </summary>
    private static DeclaredProductCheck ScriptedCheck(
        IMsiApi msi,
        IPackageIdentityReader identityReader,
        IFileIdentityReader? fileIdentities = null,
        IFileSystem? fileSystem = null,
        IRegistryReader? registry = null,
        IRunningAccount? runningAccount = null) =>
        new(msi, identityReader, fileIdentities, fileSystem, registry, runningAccount)
        {
            DriveKindOf = FixedDrive,
            NamesInFolderOf = NameOnly,
        };

    /// <summary>Every drive letter as a fixed drive.</summary>
    private static DriveType FixedDrive(string drive) => DriveType.Fixed;

    /// <summary>A folder entry holding the last component of the candidate's path alone.</summary>
    private static IReadOnlyList<string>? NameOnly(string path) => [path[(path.LastIndexOf('\\') + 1)..]];

    // ---- The keeping arm: Windows still holds the declared product ----

    [Fact]
    public void A_package_whose_declared_product_Windows_holds_is_kept_back()
    {
        // THE WHOLE POINT OF THE CHECK. Nothing registered names this file, so
        // every other mechanism in the scan has already let it through; the file
        // itself says which product it belongs to, and Windows still has that
        // product. Built without the file readers, the check cannot look at what
        // the product records, which is the section further down.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var outcomes = ScriptedCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []).Outcomes;

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcomes[0]);
        Assert.True(outcomes[0].Withholds());
    }

    // ---- The one permitting arm, which is the must-miss control for the above ----

    [Fact]
    public void A_package_Windows_says_it_does_not_hold_is_left_where_it_was()
    {
        // The must-miss half. Without it the test above passes just as well
        // against a check that keeps every file back, which is a check that has
        // emptied the offer and looks identical from the outside.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.UnknownProduct);

        var outcomes = ScriptedCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []).Outcomes;

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcomes[0]);
        Assert.False(outcomes[0].Withholds());
    }

    [Fact]
    public void NoMoreItems_is_the_other_return_that_means_the_product_is_not_there()
    {
        // Two returns are allowed to mean absence and the code has to accept both,
        // so pinning only the obvious one would leave the second free to be
        // dropped: a keyed enumeration that runs out of rows has answered, and
        // reading that as an inability would keep every file on every machine.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.NoMoreItems);

        var outcomes = ScriptedCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []).Outcomes;

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcomes[0]);
    }

    // ---- Every inability keeps the file, which is the half easiest to get wrong ----

    [Fact]
    public void A_return_that_is_not_on_the_absence_allowlist_keeps_the_file()
    {
        // THE ARM THAT DECIDES WHETHER THIS CHECK IS WORTH HAVING. A call that
        // could not be made has not shown the product to be absent. Treating any
        // non-success as "no product" would offer the file on the strength of a
        // question that was never really put, which is the exact collapse the
        // check exists to prevent, and it would look like a working check.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Answers(ProductA, MsiError.AccessDenied);

        var outcomes = ScriptedCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []).Outcomes;

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcomes[0]);
        Assert.True(outcomes[0].Withholds());
    }

    [Fact]
    public void A_package_that_will_not_yield_an_identity_is_kept_back()
    {
        // A file that would not open, a database with no Property table, a
        // ProductCode that is not a GUID: the reader reports all of them as
        // nothing to ask about, and none of them is evidence that the file is
        // spare. This is the outcome an earlier design of this work got backwards.
        var identities = new ScriptedPackageIdentities();
        identities.YieldsNothing(@"C:\Windows\Installer\a.msi");

        var outcomes = ScriptedCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []).Outcomes;

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcomes[0]);
        Assert.True(outcomes[0].Withholds());
    }

    [Fact]
    public void A_reading_that_yields_an_empty_code_is_kept_back()
    {
        // Not the same shape as the test above and not redundant with it. A
        // do-nothing reader hands back an identity carrying no code rather than a
        // null, which is a value that would reach a keyed enumeration and be
        // answered about nothing.
        var identities = new ScriptedPackageIdentities();
        identities.Yields(@"C:\Windows\Installer\a.msi",
            new PackageIdentity(string.Empty, IsPatch: false, Array.Empty<string>()));

        var outcomes = ScriptedCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []).Outcomes;

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcomes[0]);
    }

    [Fact]
    public void A_reading_that_comes_back_marked_as_a_patch_is_kept_back()
    {
        // The product reading was asked for and something else came back. A patch
        // code put to a keyed PRODUCT enumeration is a question about nothing, and
        // the answer would be an absence that means only that the wrong thing was
        // asked.
        var identities = new ScriptedPackageIdentities();
        identities.Yields(@"C:\Windows\Installer\a.msi",
            new PackageIdentity(ProductA, IsPatch: true, new[] { ProductB }));

        var outcomes = ScriptedCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []).Outcomes;

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcomes[0]);
    }

    // ---- A patch and a package in one pass ----

    [Fact]
    public void A_patch_and_a_package_in_one_pass_are_each_screened_on_their_own_terms()
    {
        // Same list, same pass, opposite outcomes. The package declares an installed
        // product and is kept; the patch declares a patch no product holds and is let
        // through. Each verdict answers its own file, and the patch is read as a patch.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);
        identities.DeclaresPatch(@"C:\Windows\Installer\p.msp", PatchQ, ProductB);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.NotInstalled(ProductB, MsiError.UnknownProduct);
        msi.HoldsNoPatches();

        var outcomes = ScriptedCheck(msi, identities).Screen(new[]
        {
            Patch(@"C:\Windows\Installer\p.msp"),
            Package(@"C:\Windows\Installer\a.msi"),
        }, []).Outcomes;

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchNotRegistered, outcomes[0]);
        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcomes[1]);
        Assert.Equal(new[] { @"C:\Windows\Installer\p.msp" }, identities.PatchReads);
    }

    // ---- A product code the machine holds more than once ----

    [Fact]
    public void A_package_whose_declared_product_is_installed_twice_is_kept_back()
    {
        // One code, two installations: per machine and for a user at once. Built
        // without the file readers, the screen asks only whether the machine holds the
        // code the file declares, so the answer is the same as for one installation,
        // and this pins that a walk over several rows still reaches it.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA,
            (null, MsiInstallContext.Machine),
            ("S-1-5-21-9-9-9-1001", MsiInstallContext.UserUnmanaged));

        var outcomes = ScriptedCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []).Outcomes;

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcomes[0]);
        Assert.True(outcomes[0].Withholds());
    }

    [Fact]
    public void A_package_whose_declared_product_has_a_row_Windows_will_not_read_is_kept_back()
    {
        // The first row says the machine holds the code and the second will not
        // answer. The file is kept back as unestablished rather than on the first
        // row's word, because what the rest of the scan does with the answer is put
        // keyed questions to each instance, and one of them is missing.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA,
            (null, MsiInstallContext.Machine),
            ("S-1-5-21-9-9-9-1001", MsiInstallContext.UserUnmanaged));
        msi.AnswersAtRow(ProductA, index: 1, MsiError.AccessDenied);

        var outcomes = ScriptedCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []).Outcomes;

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcomes[0]);
        Assert.True(outcomes[0].Withholds());
    }

    // ---- The pass's own contract ----

    [Fact]
    public void The_verdicts_line_up_with_the_candidates_they_answer()
    {
        // Positional, so a caller reads verdict i as candidate i's. Three
        // candidates with three different answers, deliberately not in the order
        // the enum declares them, so a check that returned a fixed sequence or
        // sorted its output would fail here.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\gone.msi", ProductA);
        identities.YieldsNothing(@"C:\Windows\Installer\unreadable.msi");
        identities.Declares(@"C:\Windows\Installer\held.msi", ProductB);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.UnknownProduct);
        msi.Installed(ProductB);

        var outcomes = ScriptedCheck(msi, identities).Screen(new[]
        {
            Package(@"C:\Windows\Installer\gone.msi"),
            Package(@"C:\Windows\Installer\unreadable.msi"),
            Package(@"C:\Windows\Installer\held.msi"),
        }, []).Outcomes;

        Assert.Equal(new[]
        {
            DeclaredProductOutcome.DeclaredProductNotInstalled,
            DeclaredProductOutcome.Unestablished,
            DeclaredProductOutcome.DeclaredProductInstalled,
        }, outcomes);
    }

    [Fact]
    public void One_product_code_is_put_to_Windows_once_however_many_files_declare_it()
    {
        // A folder holding six cached packages of one program declares one product
        // code six times. The cache is what keeps the pass proportional to the
        // number of PRODUCTS rather than to the number of files, and it must not
        // change any verdict: all three files here get the same answer.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\v1.msi", ProductA);
        identities.Declares(@"C:\Windows\Installer\v2.msi", ProductA);
        identities.Declares(@"C:\Windows\Installer\v3.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var outcomes = ScriptedCheck(msi, identities).Screen(new[]
        {
            Package(@"C:\Windows\Installer\v1.msi"),
            Package(@"C:\Windows\Installer\v2.msi"),
            Package(@"C:\Windows\Installer\v3.msi"),
        }, []).Outcomes;

        Assert.All(outcomes, o => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, o));
        Assert.Equal(new[] { ProductA }, msi.Asked);
        // Every file is still opened: two packages declaring one code is the
        // ordinary case, and the only way to know a file declares that code is to
        // read it.
        Assert.Equal(3, identities.Reads.Count);
    }

    [Fact]
    public void A_cancelled_scan_stops_the_pass()
    {
        // The pass opens a database per candidate, so on a folder of any size it
        // is the part of a scan a user is most likely to cancel during.
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            ScriptedCheck(new ScriptedMsiProducts(), new ScriptedPackageIdentities())
                .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, [], cts.Token).Outcomes);
    }

    // ---- An installed product whose recorded package is another file ----
    //
    // Windows Installer opens a product's cached package through the LocalPackage
    // value each installation records. A copy in the folder that no such value names,
    // and that no source reaches, is let through, and only when EVERY installation's
    // recorded package is present and is another file. Each test after the first is
    // one way an installation's package can fail to be seen, and every one of them
    // keeps the file. The sources have their own tests further down.

    private const string Candidate = @"C:\Windows\Installer\a.msi";
    private const string Recorded = @"C:\Windows\Installer\b.msi";
    private const string UserSid = "S-1-5-21-9-9-9-1001";
    private const string OtherUserSid = "S-1-5-21-9-9-9-1002";
    private const string InstallerFolder = @"C:\Windows\Installer";
    private const string SetupFolder = @"D:\Setup\";
    private const string SetupName = "setup.msi";
    private const string SetupPackage = @"D:\Setup\setup.msi";

    /// <summary>
    /// The scan's answer to whether a path names a file directly in the Installer
    /// folder, on the spelling alone, which is all a scripted path has.
    /// </summary>
    private static bool? InInstallerFolder(string path) =>
        path.StartsWith(InstallerFolder + @"\", StringComparison.OrdinalIgnoreCase)
        && path.IndexOf('\\', InstallerFolder.Length + 1) < 0;

    /// <summary>
    /// Product A installed once per machine, recording <see cref="Recorded"/>, with
    /// both files on disk as two different files that both declare product A, and
    /// installed from <see cref="SetupPackage"/>, which is no longer there. Each test
    /// changes one thing.
    /// </summary>
    private static (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk) ACopyBesideTheRecordedPackage()
    {
        var packages = new ScriptedPackageIdentities();
        packages.Declares(Candidate, ProductA);
        packages.Declares(Recorded, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.RecordsPackage(ProductA, null, MsiInstallContext.Machine, Recorded);
        msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName, SetupFolder);

        var files = new ScriptedFileIdentities();
        files.Opens(Candidate, 1);
        files.Opens(Recorded, 2);
        files.Answers(SetupPackage, FileIdentityRead.NamesNothing);

        var disk = new MockFileSystem();
        disk.AddFile(Candidate, new MockFileData(new byte[100]));
        disk.AddFile(Recorded, new MockFileData(new byte[100]));

        return (packages, msi, files, disk);
    }

    private static DeclaredProductOutcome ScreenTheCopy(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk) f,
        Func<string, bool?>? namesAFileInInstallerFolder = null,
        IReadOnlyList<ListedInstallation>? installations = null) =>
        ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Package(Candidate) }, installations ?? [], default, null,
                namesAFileInInstallerFolder ?? InInstallerFolder).Outcomes[0];

    [Fact]
    public void A_copy_beside_the_package_its_installed_product_records_is_let_through()
    {
        var f = ACopyBesideTheRecordedPackage();

        var outcome = ScreenTheCopy(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.False(outcome.Withholds());
        // Both files were identified, which is what the verdict rests on.
        Assert.Contains(Recorded, f.Files.Reads);
        Assert.Contains(Candidate, f.Files.Reads);
    }

    [Fact]
    public void A_copy_is_let_through_when_every_installation_records_another_present_package()
    {
        const string UsersPackage = @"C:\Windows\Installer\c.msi";
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Installed(ProductA,
            (null, MsiInstallContext.Machine),
            (UserSid, MsiInstallContext.UserManaged));
        f.Msi.RecordsPackage(ProductA, UserSid, MsiInstallContext.UserManaged, UsersPackage);
        f.Msi.RecordsSources(ProductA, UserSid, MsiInstallContext.UserManaged, SetupName, SetupFolder);
        f.Packages.Declares(UsersPackage, ProductA);
        f.Files.Opens(UsersPackage, 3);
        f.Disk.AddFile(UsersPackage, new MockFileData(new byte[100]));

        var outcome = ScreenTheCopy(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.Equal(2, f.Msi.PackageReads.Count);
        Assert.Equal(2, f.Msi.SourceListWalks.Count);
    }

    [Fact]
    public void A_copy_is_kept_when_an_installation_is_per_user_unmanaged_whatever_its_source_list_holds()
    {
        // The copy that is let through above, with the second installation per user and
        // unmanaged. It records another present package declaring the product, and its
        // source list, were it read, points only at a folder holding no package. The
        // source list in that context is not read, whichever account it is in, so the
        // package that installation opens cannot be ruled out and this copy is kept.
        const string UsersPackage = @"C:\Windows\Installer\c.msi";
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Installed(ProductA,
            (null, MsiInstallContext.Machine),
            (UserSid, MsiInstallContext.UserUnmanaged));
        f.Msi.RecordsPackage(ProductA, UserSid, MsiInstallContext.UserUnmanaged, UsersPackage);
        f.Msi.RecordsSources(ProductA, UserSid, MsiInstallContext.UserUnmanaged, SetupName, SetupFolder);
        f.Packages.Declares(UsersPackage, ProductA);
        f.Files.Opens(UsersPackage, 3);
        f.Disk.AddFile(UsersPackage, new MockFileData(new byte[100]));

        var outcome = ScreenTheCopy(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.True(outcome.Withholds());
        // The machine's list was read and the user's was not.
        var machineOnly = new[] { (ProductA, (string?)null, MsiInstallContext.Machine) };
        Assert.Equal(machineOnly, f.Msi.PackageNameReads);
        Assert.Equal(machineOnly, f.Msi.SourceListWalks);
    }

    [Fact]
    public void A_copy_is_kept_when_one_installation_records_no_package_and_its_sources_cannot_be_ruled_out()
    {
        // The per-machine installation records another file; the per-user one records
        // nothing and its package name will not read, so the package that installation
        // opens cannot be seen and this copy could be it.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Installed(ProductA,
            (null, MsiInstallContext.Machine),
            (UserSid, MsiInstallContext.UserManaged));
        f.Msi.RecordsPackage(ProductA, UserSid, MsiInstallContext.UserManaged, "");
        f.Msi.Registry.Holds(InstallPropertiesKey("11111111111111111111111111111111", UserSid));
        f.Msi.PackageNameAnswers(ProductA, UserSid, MsiInstallContext.UserManaged, MsiError.AccessDenied);

        var outcome = ScreenTheCopy(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.True(outcome.Withholds());
    }

    [Fact]
    public void A_copy_is_let_through_when_one_installation_records_no_package_and_its_sources_name_another_file()
    {
        // The must-miss half of the test above: the per-user installation's sources name a
        // package that is no longer there, which is all it can open.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Installed(ProductA,
            (null, MsiInstallContext.Machine),
            (UserSid, MsiInstallContext.UserManaged));
        f.Msi.RecordsPackage(ProductA, UserSid, MsiInstallContext.UserManaged, "");
        f.Msi.RecordsSources(ProductA, UserSid, MsiInstallContext.UserManaged, SetupName, SetupFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
    }

    /// <summary>
    /// Product A's only installation records no package: an empty value, or ERROR_UNKNOWN_PROPERTY,
    /// a record that never carried the value, which reads as an empty value rather than a failure.
    /// </summary>
    private static void RecordsNoPackage(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk) f,
        uint answer)
    {
        if (answer == MsiError.Success) f.Msi.RecordsPackage(ProductA, null, MsiInstallContext.Machine, "");
        else f.Msi.PackageReadAnswers(ProductA, null, MsiInstallContext.Machine, answer);
    }

    [Theory]
    [InlineData(MsiError.Success)]
    [InlineData(MsiError.UnknownProperty)]
    public void A_copy_is_let_through_when_the_only_installation_records_no_package_and_its_sources_name_another_file(
        uint answer)
    {
        // An installation recording no package opens what its sources name, here a package
        // that is no longer there.
        var f = ACopyBesideTheRecordedPackage();
        RecordsNoPackage(f, answer);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
        Assert.Contains(SetupPackage, f.Files.Reads);
    }

    [Theory]
    [InlineData(MsiError.Success)]
    [InlineData(MsiError.UnknownProperty)]
    public void A_copy_is_kept_when_the_only_installation_records_no_package_and_its_sources_cannot_be_ruled_out(
        uint answer)
    {
        var f = ACopyBesideTheRecordedPackage();
        RecordsNoPackage(f, answer);
        f.Msi.SourceListAnswers(ProductA, null, MsiInstallContext.Machine, MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_recorded_package_will_not_read()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.PackageReadAnswers(ProductA, null, MsiInstallContext.Machine, MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_recorded_package_is_not_on_disk()
    {
        // The identity fake still answers for the recorded path, so this is decided by
        // the file being absent and not by an identity that would not read.
        var f = ACopyBesideTheRecordedPackage();
        f.Disk.RemoveFile(Recorded);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_recorded_package_path_names_a_folder()
    {
        // A folder opens to an identity like a file does, so without the file test a
        // value naming a folder would read as another package.
        const string AFolder = @"C:\Windows\Installer\{11111111-1111-1111-1111-111111111111}";
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsPackage(ProductA, null, MsiInstallContext.Machine, AFolder);
        f.Files.Opens(AFolder, 4);
        f.Disk.AddDirectory(AFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_recorded_package_opens_as_the_copy_itself()
    {
        // The record names this very file under another spelling: a short name, a
        // long-path prefix, a link. The two paths open to one file ID.
        const string ShortName = @"C:\Windows\Installer\A~1.MSI";
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsPackage(ProductA, null, MsiInstallContext.Machine, ShortName);
        f.Packages.Declares(ShortName, ProductA);
        f.Files.Opens(ShortName, 1);
        f.Disk.AddFile(ShortName, new MockFileData(new byte[100]));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_recorded_package_will_not_identify()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Files.Answers(Recorded, FileIdentityRead.OpenRefused);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_recorded_package_declares_another_product()
    {
        // The Windows Installer record names a present file, and that file is not
        // product A's package, so the record shows nothing about where A's package is.
        var f = ACopyBesideTheRecordedPackage();
        f.Packages.Declares(Recorded, ProductB);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_recorded_package_reads_as_a_patch()
    {
        // The recorded file carries product A's code and reads as a patch, so it is not
        // product A's installation package, and the record shows nothing about where
        // that package is.
        var f = ACopyBesideTheRecordedPackage();
        f.Packages.Yields(Recorded, new PackageIdentity(ProductA, IsPatch: true, Array.Empty<string>()));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_recorded_package_declares_nothing()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Packages.YieldsNothing(Recorded, "no Property table");

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_as_unidentified_when_the_copy_itself_will_not_identify()
    {
        // Every package the installation opens was seen and identified, so what the
        // comparison lacks is the copy's own identity, and the verdict says that rather
        // than that the program is installed.
        var f = ACopyBesideTheRecordedPackage();
        f.Files.Answers(Candidate, FileIdentityRead.IdentityUnavailable);

        var outcome = ScreenTheCopy(f);

        Assert.Equal(DeclaredProductOutcome.CandidateIdentityUnestablished, outcome);
        Assert.True(outcome.Withholds());
    }

    [Fact]
    public void A_copy_gone_by_the_time_its_identity_is_read_is_kept_as_unidentified()
    {
        // The copy declares product A and then names nothing when the check reads its
        // identity, as a file removed between the two reads does. Nothing at the path
        // shows it to be a different file from the package A records, so it is kept,
        // under the verdict for a copy whose identity did not read.
        var f = ACopyBesideTheRecordedPackage();
        f.Files.Answers(Candidate, FileIdentityRead.NamesNothing);

        var outcome = ScreenTheCopy(f);

        Assert.Equal(DeclaredProductOutcome.CandidateIdentityUnestablished, outcome);
        Assert.True(outcome.Withholds());
    }

    // ---- The installation's sources ----
    //
    // When Windows Installer needs a product's original package rather than its
    // cached copy, it looks for the package name in the folders on the product's
    // source list. A copy in the Installer folder that such a source can reach is
    // kept, and so is one whose sources cannot be ruled out. Each keeping test is
    // the fixture above, which lets the copy through, with one thing changed.

    [Fact]
    public void A_source_in_the_Installer_folder_keeps_the_copy_it_opens_as_and_lets_another_copy_through()
    {
        // Product A installed from c.msi in the Installer folder, which is the second copy.
        // The first copy is another file and goes on to the rest of the check. The
        // Installer-folder test is not asked about a package on a local drive.
        var asked = new List<string>();
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, "c.msi", InstallerFolder + @"\");
        AddSecondCandidate(f);

        var outcomes = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen([Package(Candidate), Package(SecondCandidate)], [], default, null, path =>
            {
                asked.Add(path);
                return InInstallerFolder(path);
            }).Outcomes;

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, DeclaredProductOutcome.DeclaredProductInstalled },
            outcomes);
        Assert.Empty(asked);
    }

    [Theory]
    [InlineData(@"C:\WINDOWS\Installer\", "a.msi")]
    [InlineData(@"C:\WINDOWS\INSTAL~1\", "a.msi")]
    [InlineData(@"\\?\C:\Windows\Installer\", "a.msi")]
    [InlineData(@"C:\Windows\Installer\", "A~1.MSI")]
    [InlineData(@"C:\Windows\Installer\", "c.msi")]
    [InlineData(@"D:\Linked\", "a.msi")]
    [InlineData(@"D:\Setup\", "c.msi")]
    public void A_package_on_a_local_drive_that_opens_as_the_copy_keeps_it_however_its_folder_and_name_are_spelled(
        string folder, string packageName)
    {
        // The Installer folder in capitals, by its short name and through the long-path
        // prefix; the copy by its short name; a name in the Installer folder that opens as
        // the copy through a link; a folder elsewhere linked to the Installer folder; and a
        // file elsewhere linked to the copy. The Installer-folder test answers that each
        // package is in it, and the file the package opens as decides.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, packageName, folder);
        f.Files.Opens(folder + packageName, 1);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f, _ => true));
        Assert.Contains(folder + packageName, f.Files.Reads);
    }

    [Fact]
    public void A_source_in_the_Installer_folder_holding_no_file_by_its_package_name_keeps_no_copy()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, "c.msi", InstallerFolder + @"\");
        f.Files.Answers(InstallerFolder + @"\c.msi", FileIdentityRead.NamesNothing);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData(FileIdentityRead.OpenRefused)]
    [InlineData(FileIdentityRead.IdentityUnavailable)]
    [InlineData(FileIdentityRead.Faulted)]
    [InlineData(FileIdentityRead.NotAPath)]
    public void Every_copy_is_kept_when_the_package_in_the_Installer_folder_its_product_was_installed_from_will_not_identify(
        FileIdentityRead outcome)
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, "c.msi", InstallerFolder + @"\");
        AddSecondCandidate(f);
        f.Files.Answers(SecondCandidate, outcome);

        var outcomes = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen([Package(Candidate), Package(SecondCandidate)], [], default, null, InInstallerFolder).Outcomes;

        Assert.All(outcomes, o => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, o));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void A_package_on_a_local_drive_is_compared_by_its_identity_whatever_the_Installer_folder_test_would_answer(
        bool? answer)
    {
        var asked = new List<string>();
        var f = ACopyBesideTheRecordedPackage();

        var outcome = ScreenTheCopy(f, path =>
        {
            asked.Add(path);
            return answer;
        });

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.Empty(asked);
    }

    [Fact]
    public void A_copy_is_kept_when_a_source_package_is_the_copy_itself()
    {
        // A source outside the folder whose package opens as this file.
        var f = ACopyBesideTheRecordedPackage();
        f.Files.Opens(SetupPackage, 1);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_screen_has_no_Installer_folder_to_compare_against()
    {
        var f = ACopyBesideTheRecordedPackage();

        var outcome = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Package(Candidate) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
    }

    [Fact]
    public void A_copy_is_kept_when_the_source_list_will_not_read()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.SourceListAnswers(ProductA, null, MsiInstallContext.Machine, MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_source_list_does_not_end()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.SourceListNeverEnds(ProductA, null, MsiInstallContext.Machine);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_a_source_entry_is_empty()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName, "");

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_a_source_entry_holds_a_variable_that_is_not_set()
    {
        // Read as it stands, the path finds no file and would be skipped.
        const string Variable = "INSTALLERCLEAN_TEST_UNSET_SOURCE";
        Assert.Null(Environment.GetEnvironmentVariable(Variable));
        var entry = $@"%{Variable}%\Setup\";
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName, entry);
        f.Files.Answers(entry + SetupName, FileIdentityRead.NamesNothing);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_package_name_will_not_read()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.PackageNameAnswers(ProductA, null, MsiInstallContext.Machine, MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_package_name_is_empty()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, "", SetupFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_a_source_package_will_not_identify()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Files.Answers(SetupPackage, FileIdentityRead.OpenRefused);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_let_through_when_its_source_package_is_another_file()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Files.Opens(SetupPackage, 9);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
        Assert.Contains(SetupPackage, f.Files.Reads);
    }

    [Fact]
    public void A_copy_is_let_through_when_its_product_records_no_network_source()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
    }

    // ---- A source list of more than one entry ----
    //
    // The list is read one entry per call, in the order Windows lists them, to the end
    // Windows reports. The fake keeps the position of the walk between calls as Windows
    // does, so once a call at an index past the first has succeeded, a second call at
    // that index is refused, as it is on Windows.

    private const string MediaFolder = @"E:\Media\";
    private const string MediaPackage = @"E:\Media\setup.msi";

    [Fact]
    public void A_copy_is_let_through_when_both_sources_on_its_list_are_ruled_out()
    {
        // The first source holds no package and the second holds another file, so the
        // copy is ruled out only by reading the list to its end.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName, SetupFolder, MediaFolder);
        f.Files.Opens(MediaPackage, 9);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
        Assert.Contains(MediaPackage, f.Files.Reads);
    }

    [Fact]
    public void A_second_source_in_the_Installer_folder_keeps_the_copy_it_opens_as_and_lets_another_copy_through()
    {
        // The first source holds no package, so the list's first entry alone would let both
        // copies through. The second is the Installer folder, whose package is the second
        // copy.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, "c.msi",
            SetupFolder, InstallerFolder + @"\");
        f.Files.Answers(SetupFolder + "c.msi", FileIdentityRead.NamesNothing);
        AddSecondCandidate(f);

        var outcomes = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen([Package(Candidate), Package(SecondCandidate)], [], default, null, InInstallerFolder).Outcomes;

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, DeclaredProductOutcome.DeclaredProductInstalled },
            outcomes);
        Assert.Contains(SetupFolder + "c.msi", f.Files.Reads);
    }

    [Fact]
    public void A_copy_is_kept_when_an_entry_after_the_first_will_not_read()
    {
        // The first entry reads and holds no package. The second answers
        // ERROR_INVALID_PARAMETER, which is not the end of the list, so what the rest
        // of the list holds is unread.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName, SetupFolder, MediaFolder);
        f.Msi.SourceListEntryAnswers(ProductA, null, MsiInstallContext.Machine, 1,
            ScriptedMsiProducts.InvalidParameter);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_a_source_entry_is_longer_than_any_path()
    {
        // An entry longer than the longest path the Windows API takes does not fit the
        // buffer it is read into. Nothing is at the package it names, so only its length
        // keeps the copy.
        var entry = @"D:\" + new string('a', 32_765) + @"\";
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName, entry);
        f.Files.Answers(entry + SetupName, FileIdentityRead.NamesNothing);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_a_source_entry_holds_a_variable_that_is_set()
    {
        // The variable is set and the folder it names holds no package, so only the
        // variable keeps the copy.
        const string Variable = "INSTALLERCLEAN_TEST_SET_SOURCE";
        var entry = $@"%{Variable}%\";
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName, entry);
        f.Files.Answers(entry + SetupName, FileIdentityRead.NamesNothing);

        Environment.SetEnvironmentVariable(Variable, @"D:\Setup");
        try
        {
            Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
        }
        finally
        {
            Environment.SetEnvironmentVariable(Variable, null);
        }
    }

    [Theory]
    [InlineData(@"..\Installer\")]
    [InlineData(@"Other\")]
    [InlineData(@"D:Other\")]
    [InlineData(@"\Other\")]
    [InlineData("D:/Other/")]
    [InlineData("file:///D:/Other/")]
    public void A_copy_is_kept_when_a_source_entry_starts_neither_with_a_drive_letter_a_colon_and_a_backslash_nor_with_two_backslashes(
        string entry)
    {
        // Each is the list's second entry and names no package the file reader knows of, so
        // only its form keeps the copy, and no package on the list is read.
        var package = (entry.EndsWith('\\') ? entry : entry + @"\") + SetupName;
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName, SetupFolder, entry);
        f.Files.Answers(package, FileIdentityRead.NamesNothing);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
        Assert.DoesNotContain(SetupPackage, f.Files.Reads);
    }

    [Theory]
    [InlineData(@"Windows\Installer\setup.msi")]
    [InlineData("../setup.msi")]
    [InlineData("C:setup.msi")]
    [InlineData("%SETUP%.msi")]
    public void A_copy_is_kept_when_the_package_name_names_more_than_a_file(string packageName)
    {
        // The source folder holds no package under any of these names, so only the name
        // keeps the copy.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, packageName, SetupFolder);
        f.Files.Answers(SetupFolder + packageName, FileIdentityRead.NamesNothing);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    // ---- The registry key holding the list ----
    //
    // Every list the API returns is compared with the registry key that holds it, and so
    // are the package name and the source used last. The fake's registry holds by default
    // what the fake's API was scripted with, as Windows holds it and in the same value
    // types, so the fixture lets the copy through, which the first test pins with the keys
    // it reads. Each test after it changes one thing on one side, and every keeping test
    // is kept by that change alone.

    private const string ProductAKey =
        @"SOFTWARE\Classes\Installer\Products\11111111111111111111111111111111\SourceList";

    private const string ProductAProperties =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\"
        + @"11111111111111111111111111111111\InstallProperties";

    /// <summary>
    /// The InstallProperties key of an installation, by its code in the packed form the registry
    /// holds it under and the account subtree it is in, <c>S-1-5-18</c> per machine.
    /// </summary>
    private static string InstallPropertiesKey(string packedCode, string account = "S-1-5-18") =>
        $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\{account}\Products\{packedCode}\InstallProperties";

    /// <summary>A REG_SZ value, the type a source list's package name and media entries have.</summary>
    private static RegistryValue Sz(string name, string text) => new(name, RegistryValueKind.String, text);

    /// <summary>A REG_EXPAND_SZ value, the type a network entry and the source used last have.</summary>
    private static RegistryValue ExpandSz(string name, string text) => new(name, RegistryValueKind.ExpandString, text);

    /// <summary>A REG_DWORD value, which reads with no text.</summary>
    private static RegistryValue Dword(string name) => new(name, RegistryValueKind.DWord, null);

    [Fact]
    public void A_copy_is_let_through_when_the_registry_holds_the_list_the_API_returned()
    {
        var f = ACopyBesideTheRecordedPackage();

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
        Assert.Equal(
            new[]
            {
                ProductAKey, ProductAKey + @"\Net", ProductAKey + @"\URL", ProductAKey + @"\Media", ProductAProperties,
            },
            f.Msi.Registry.Reads);
    }

    [Fact]
    public void A_copy_is_let_through_when_the_list_s_keys_are_written_out_value_by_value_as_Windows_holds_them()
    {
        // Every key written out rather than left to the fake: the package name and the
        // media entry a REG_SZ, the network entry and the source used last a
        // REG_EXPAND_SZ, and no URL key.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey,
            Sz(MsiInstallProperty.PackageName, SetupName),
            ExpandSz(MsiInstallProperty.LastUsedSource, "n;1;" + SetupFolder));
        f.Msi.Registry.Holds(ProductAKey + @"\Net", ExpandSz("1", SetupFolder));
        f.Msi.Registry.Answers(ProductAKey + @"\URL", RegistryKeyPresence.Absent);
        f.Msi.Registry.Holds(ProductAKey + @"\Media", Sz("1", ";"));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData("other.msi")]
    [InlineData("SETUP.MSI")]
    [InlineData("")]
    [InlineData(@"Windows\Installer\setup.msi")]
    [InlineData("Windows/setup.msi")]
    [InlineData("C:setup.msi")]
    [InlineData("%SETUP%.msi")]
    public void A_copy_is_kept_when_the_list_s_key_holds_another_package_name(string stored)
    {
        // The API answers the fixture's own name, so only the name the key holds keeps
        // the copy.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey,
            Sz(MsiInstallProperty.PackageName, stored),
            ExpandSz(MsiInstallProperty.LastUsedSource, "n;1;" + SetupFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_list_s_key_holds_no_package_name()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey, ExpandSz(MsiInstallProperty.LastUsedSource, "n;1;" + SetupFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_list_s_key_holds_the_package_name_as_a_REG_EXPAND_SZ()
    {
        // The API's own text, so only the type keeps the copy.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey,
            ExpandSz(MsiInstallProperty.PackageName, SetupName),
            ExpandSz(MsiInstallProperty.LastUsedSource, "n;1;" + SetupFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_list_s_key_holds_the_package_name_as_a_value_of_another_type()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey,
            Dword(MsiInstallProperty.PackageName),
            ExpandSz(MsiInstallProperty.LastUsedSource, "n;1;" + SetupFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_list_s_key_holds_an_entry_past_a_gap()
    {
        // The API returned the first entry. The key also holds the Installer folder,
        // numbered 3, which the API's walk does not reach.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey + @"\Net",
            ExpandSz("1", SetupFolder), ExpandSz("3", InstallerFolder + @"\"));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_list_s_key_holds_one_entry_more_than_the_API_returned()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey + @"\Net",
            ExpandSz("1", SetupFolder), ExpandSz("2", MediaFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_list_s_key_holds_one_entry_fewer_than_the_API_returned()
    {
        // The same two entries let the copy through when the key holds both.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName, SetupFolder, MediaFolder);
        f.Files.Opens(MediaPackage, 9);
        f.Msi.Registry.Holds(ProductAKey + @"\Net", ExpandSz("1", SetupFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData("2")]
    [InlineData("01")]
    [InlineData("")]
    public void A_copy_is_kept_when_the_list_s_one_value_is_named_anything_but_1(string name)
    {
        // The empty name is the key's default value.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey + @"\Net", ExpandSz(name, SetupFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData(@"D:\Other\")]
    [InlineData(@"d:\setup\")]
    [InlineData(@"D:\Setup")]
    [InlineData(null)]
    public void A_copy_is_kept_when_the_list_s_key_holds_other_text_than_the_API_returned(string? text)
    {
        // Null is a value of a type other than a string.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey + @"\Net", text is null ? Dword("1") : ExpandSz("1", text));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData(RegistryKeyPresence.Absent)]
    [InlineData(RegistryKeyPresence.Unreadable)]
    public void A_copy_is_kept_when_the_source_list_key_is_not_read(RegistryKeyPresence presence)
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Answers(ProductAKey, presence);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData(RegistryKeyPresence.Absent)]
    [InlineData(RegistryKeyPresence.Unreadable)]
    public void A_copy_is_kept_when_the_network_key_is_not_read_while_the_API_returned_an_entry(
        RegistryKeyPresence presence)
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Answers(ProductAKey + @"\Net", presence);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_let_through_when_the_network_key_is_empty_and_the_API_returned_nothing()
    {
        // Beside the product recording no network source, where the key is not there at
        // all.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName);
        f.Msi.Registry.Holds(ProductAKey + @"\Net");

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData("http://localhost/setup/")]
    [InlineData("file:///D:/Other/")]
    [InlineData("ftp://server/setup/")]
    public void A_copy_is_kept_when_its_list_holds_a_URL(string url)
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsUrls(ProductA, isPatch: false, null, MsiInstallContext.Machine, url);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_its_URL_entries_will_not_read()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.UrlListAnswers(ProductA, isPatch: false, null, MsiInstallContext.Machine, MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_URL_key_holds_an_entry_the_API_did_not_return()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey + @"\URL", ExpandSz("2", "file:///C:/Windows/Installer/"));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData(MsiInstallProperty.LastUsedSource)]
    [InlineData(MsiInstallProperty.LastUsedType)]
    [InlineData(MsiInstallProperty.MediaPackagePath)]
    public void A_copy_is_kept_when_a_property_of_its_list_will_not_read(string property)
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.ListPropertyAnswers(ProductA, isPatch: false, null, MsiInstallContext.Machine, property,
            MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_neither_property_of_the_source_used_last_will_read()
    {
        // The key holds no source used last, which is how a list with none reads, so only
        // the two failed reads keep the copy.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.ListPropertyAnswers(ProductA, isPatch: false, null, MsiInstallContext.Machine,
            MsiInstallProperty.LastUsedSource, MsiError.AccessDenied);
        f.Msi.ListPropertyAnswers(ProductA, isPatch: false, null, MsiInstallContext.Machine,
            MsiInstallProperty.LastUsedType, MsiError.AccessDenied);
        f.Msi.Registry.Holds(ProductAKey, Sz(MsiInstallProperty.PackageName, SetupName));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData("u", "http://localhost/setup/")]
    [InlineData("m", @"E:\")]
    [InlineData("u", SetupFolder)]
    [InlineData("x", SetupFolder)]
    public void A_copy_is_kept_when_the_source_used_last_is_not_a_network_source(string type, string source)
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.ListProperty(ProductA, isPatch: false, null, MsiInstallContext.Machine,
            MsiInstallProperty.LastUsedType, type);
        f.Msi.ListProperty(ProductA, isPatch: false, null, MsiInstallContext.Machine,
            MsiInstallProperty.LastUsedSource, source);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_let_through_when_there_is_no_source_used_last()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.ListProperty(ProductA, isPatch: false, null, MsiInstallContext.Machine,
            MsiInstallProperty.LastUsedSource, "");
        f.Msi.ListProperty(ProductA, isPatch: false, null, MsiInstallContext.Machine,
            MsiInstallProperty.LastUsedType, "");

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
    }

    // ---- The folder used last, compared like a folder on the list ----
    //
    // The source used last is a network source, and its folder is not looked for on the
    // list. Its package is compared as a list entry's is, so each test below sets it to a
    // folder the list does not hold, OtherFolder unless it says otherwise, and scripts the
    // file that folder's package name opens where the test reads it.

    /// <summary>The fixture with <paramref name="folder"/> as the source used last.</summary>
    private static (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk) ACopyWithTheSourceUsedLast(string folder)
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.ListProperty(ProductA, isPatch: false, null, MsiInstallContext.Machine,
            MsiInstallProperty.LastUsedSource, folder);
        return f;
    }

    [Fact]
    public void A_copy_is_let_through_when_the_package_in_the_folder_used_last_is_another_file()
    {
        var f = ACopyWithTheSourceUsedLast(OtherFolder);
        f.Files.Opens(OtherPackage, 9);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
        Assert.Contains(OtherPackage, f.Files.Reads);
    }

    [Fact]
    public void A_copy_is_let_through_when_the_folder_used_last_holds_no_package()
    {
        var f = ACopyWithTheSourceUsedLast(OtherFolder);
        f.Files.Answers(OtherPackage, FileIdentityRead.NamesNothing);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
        Assert.Contains(OtherPackage, f.Files.Reads);
    }

    [Fact]
    public void A_copy_is_kept_when_the_package_in_the_folder_used_last_is_the_copy_itself()
    {
        var f = ACopyWithTheSourceUsedLast(OtherFolder);
        f.Files.Opens(OtherPackage, 1);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_package_in_the_folder_used_last_will_not_identify()
    {
        var f = ACopyWithTheSourceUsedLast(OtherFolder);
        f.Files.Answers(OtherPackage, FileIdentityRead.OpenRefused);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData(@"%TEMP%\Setup\")]
    [InlineData("D:\\Set\0up\\")]
    [InlineData(@"Setup\")]
    [InlineData(@"D:Setup\")]
    public void A_copy_is_kept_when_the_folder_used_last_is_one_the_check_does_not_compare(string folder)
    {
        // A folder naming an environment variable, one holding a null, a relative folder
        // and one relative to a drive's current folder. The package in none of them is
        // scripted, and the fake's file reader throws on a path no test scripted, so the
        // copy is kept without any of them being read.
        var f = ACopyWithTheSourceUsedLast(folder);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void The_Installer_folder_as_the_folder_used_last_keeps_the_copy_it_opens_as()
    {
        // The package there is setup.msi, a second copy of product A; the first copy is
        // another file. On a local drive the package is compared by identity alone, as a
        // list entry naming the Installer folder is.
        const string Named = @"C:\Windows\Installer\setup.msi";
        var f = ACopyWithTheSourceUsedLast(InstallerFolder + @"\");
        f.Packages.Declares(Named, ProductA);
        f.Files.Opens(Named, 5);
        f.Disk.AddFile(Named, new MockFileData(new byte[100]));

        var outcomes = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen([Package(Candidate), Package(Named)], [], default, null, InInstallerFolder).Outcomes;

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, DeclaredProductOutcome.DeclaredProductInstalled },
            outcomes);
    }

    [Fact]
    public void A_package_in_a_network_folder_used_last_named_otherwise_than_the_copy_is_not_read()
    {
        var f = ACopyWithTheSourceUsedLast(NasFolder);
        f.Files.Answers(NasFolder + SetupName, FileIdentityRead.OpenRefused);

        var outcome = CheckBesideASource(f).Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.DoesNotContain(NasFolder + SetupName, f.Files.Reads);
    }

    [Fact]
    public void A_copy_is_kept_when_the_package_in_a_network_folder_used_last_carries_its_name_and_is_the_copy()
    {
        // The package name is the copy's own, so the package on the share is read for it.
        const string CopysName = "a.msi";
        var f = ACopyWithTheSourceUsedLast(NasFolder);
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, CopysName, SetupFolder);
        f.Files.Answers(SetupFolder + CopysName, FileIdentityRead.NamesNothing);
        f.Files.Opens(NasFolder + CopysName, 1);

        var outcome = CheckBesideASource(f).Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.Contains(NasFolder + CopysName, f.Files.Reads);
    }

    // A record of the Office 16 Click-to-Run Extensibility Component in which the list's
    // one network entry and the InstallSource spell the folder in mixed case and the
    // source used last spells the same folder in lower case. An Office update can leave
    // the earlier build's copy of its package in the Installer folder.

    private const string Extensibility = "{90160000-008C-0000-1000-0000000FF1CE}";

    private const string ExtensibilitySourceList =
        @"SOFTWARE\Classes\Installer\Products\00006109C80000000100000000F01FEC\SourceList";

    private const string IntegrationFolder = @"C:\Program Files\Microsoft Office\root\Integration\";
    private const string IntegrationFolderUsedLast = @"c:\program files\microsoft office\root\integration\";
    private const string IntegrationPackageName = "C2RInt.16.msi";

    [Fact]
    public void An_older_copy_of_Offices_Extensibility_Component_is_let_through_with_its_folder_used_last_in_lower_case()
    {
        const string OlderCopy = @"C:\Windows\Installer\55749af.msi";
        const string Cached = @"C:\Windows\Installer\12a583.msi";

        var packages = new ScriptedPackageIdentities();
        packages.Declares(OlderCopy, Extensibility);
        packages.Declares(Cached, Extensibility);

        var msi = new ScriptedMsiProducts();
        msi.Installed(Extensibility);
        msi.RecordsPackage(Extensibility, null, MsiInstallContext.Machine, Cached);
        msi.RecordsSources(Extensibility, null, MsiInstallContext.Machine, IntegrationPackageName, IntegrationFolder);
        msi.ListProperty(Extensibility, isPatch: false, null, MsiInstallContext.Machine,
            MsiInstallProperty.LastUsedSource, IntegrationFolderUsedLast);

        // The list's keys value by value, in the types Windows writes them.
        msi.Registry.Holds(ExtensibilitySourceList,
            Sz(MsiInstallProperty.PackageName, IntegrationPackageName),
            ExpandSz(MsiInstallProperty.LastUsedSource, "n;1;" + IntegrationFolderUsedLast));
        msi.Registry.Holds(ExtensibilitySourceList + @"\Net", ExpandSz("1", IntegrationFolder));
        msi.Registry.Answers(ExtensibilitySourceList + @"\URL", RegistryKeyPresence.Absent);
        msi.Registry.Holds(ExtensibilitySourceList + @"\Media",
            Sz("DiskPrompt", "Office 16 Click-to-Run Extensibility Component"), Sz("1", "OFFICE16;1"));

        // The package in the integration folder is present under either spelling, and is
        // neither the older copy nor the cached package.
        var files = new ScriptedFileIdentities();
        files.Opens(OlderCopy, 1);
        files.Opens(Cached, 2);
        files.Opens(IntegrationFolder + IntegrationPackageName, 3);

        var disk = new MockFileSystem();
        disk.AddFile(OlderCopy, new MockFileData(new byte[100]));
        disk.AddFile(Cached, new MockFileData(new byte[100]));

        var outcome = ScriptedCheck(msi, packages, files, disk, msi.Registry)
            .Screen([Package(OlderCopy)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.Contains(IntegrationFolderUsedLast + IntegrationPackageName, files.Reads);
        Assert.Contains(IntegrationFolder + IntegrationPackageName, files.Reads);
    }

    [Theory]
    [InlineData(@"n;1;C:\Windows\Installer\")]
    [InlineData(@"u;1;D:\Setup\")]
    [InlineData("n;1")]
    [InlineData("n")]
    [InlineData(null)]
    public void A_copy_is_kept_when_the_registry_holds_the_source_used_last_otherwise_than_the_API_answers(
        string? stored)
    {
        // The API answers the fixture's own entry, of type "n". Null is a value of a type
        // other than a string.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey,
            Sz(MsiInstallProperty.PackageName, SetupName),
            stored is null
                ? Dword(MsiInstallProperty.LastUsedSource)
                : ExpandSz(MsiInstallProperty.LastUsedSource, stored));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_copy_is_kept_when_the_registry_holds_no_source_used_last_while_the_API_answers_one(bool emptyValue)
    {
        // The API answers the fixture's own entry, which is on the list, so only what the
        // key holds keeps the copy: no LastUsedSource value, or an empty one.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey, emptyValue
            ? new[] { Sz(MsiInstallProperty.PackageName, SetupName), ExpandSz(MsiInstallProperty.LastUsedSource, "") }
            : new[] { Sz(MsiInstallProperty.PackageName, SetupName) });

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_registry_holds_a_source_used_last_the_API_answers_as_none()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.ListProperty(ProductA, isPatch: false, null, MsiInstallContext.Machine,
            MsiInstallProperty.LastUsedSource, "");
        f.Msi.ListProperty(ProductA, isPatch: false, null, MsiInstallContext.Machine,
            MsiInstallProperty.LastUsedType, "");
        f.Msi.Registry.Holds(ProductAKey,
            Sz(MsiInstallProperty.PackageName, SetupName),
            ExpandSz(MsiInstallProperty.LastUsedSource, "n;1;" + SetupFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_API_answers_a_media_package_path()
    {
        // The registry's Media key names none, so only the API's answer keeps the copy.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.ListProperty(ProductA, isPatch: false, null, MsiInstallContext.Machine,
            MsiInstallProperty.MediaPackagePath, "disk1");
        f.Msi.Registry.Holds(ProductAKey + @"\Media", Sz("1", ";"));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData("disk1")]
    [InlineData(null)]
    public void A_copy_is_kept_when_the_media_key_holds_a_media_package_path(string? text)
    {
        // The API answers none, so only the registry's value keeps the copy. Null is a
        // value of a type other than a string.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAKey + @"\Media",
            Sz("1", ";"), text is null ? Dword("MediaPackage") : Sz("MediaPackage", text));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_media_key_will_not_read()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Answers(ProductAKey + @"\Media", RegistryKeyPresence.Unreadable);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_let_through_when_there_is_no_media_key()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Answers(ProductAKey + @"\Media", RegistryKeyPresence.Absent);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
    }

    [Fact]
    public void A_per_user_managed_installation_s_list_is_read_under_its_account()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Installed(ProductA, (UserSid, MsiInstallContext.UserManaged));
        f.Msi.RecordsPackage(ProductA, UserSid, MsiInstallContext.UserManaged, Recorded);
        f.Msi.RecordsSources(ProductA, UserSid, MsiInstallContext.UserManaged, SetupName, SetupFolder);

        const string Key =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Managed\S-1-5-21-9-9-9-1001\Installer\Products\"
            + @"11111111111111111111111111111111\SourceList";
        const string Properties =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-21-9-9-9-1001\Products\"
            + @"11111111111111111111111111111111\InstallProperties";
        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
        Assert.Equal(new[] { Key, Key + @"\Net", Key + @"\URL", Key + @"\Media", Properties }, f.Msi.Registry.Reads);
    }

    [Theory]
    [InlineData(@"S-1-5-21-9-9-9-1001\..\..")]
    [InlineData("s-1-5-21-9-9-9-1001")]
    [InlineData("S-")]
    public void A_copy_is_kept_when_its_account_will_not_make_a_key_name(string sid)
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Installed(ProductA, (sid, MsiInstallContext.UserManaged));
        f.Msi.RecordsPackage(ProductA, sid, MsiInstallContext.UserManaged, Recorded);
        f.Msi.RecordsSources(ProductA, sid, MsiInstallContext.UserManaged, SetupName, SetupFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
        Assert.Empty(f.Msi.Registry.Reads);
    }

    [Fact]
    public void Without_the_registry_reader_a_copy_is_kept_and_no_key_is_read()
    {
        var f = ACopyBesideTheRecordedPackage();

        var outcome = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk)
            .Screen(new[] { Package(Candidate) }, [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.Empty(f.Msi.Registry.Reads);
    }

    [Fact]
    public void The_composition_root_gives_the_check_its_registry_reader()
    {
        using var services = new ServiceCollection().AddInstallerCleanCore().BuildServiceProvider();

        var check = Assert.IsType<DeclaredProductCheck>(services.GetRequiredService<IDeclaredProductCheck>());

        Assert.True(check.ReadsSourceListKeys);
    }

    [Fact]
    public void Without_the_file_readers_no_recorded_package_is_read()
    {
        // The same fixture as the tests above, which scripts every read, handed to a
        // check built without its two file readers. Nothing may be read: the
        // assertions below are that the package reads and the identity reads never
        // happened.
        var f = ACopyBesideTheRecordedPackage();

        var outcome = ScriptedCheck(f.Msi, f.Packages)
            .Screen(new[] { Package(Candidate) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.Empty(f.Msi.PackageReads);
        Assert.Empty(f.Files.Reads);
    }

    [Fact]
    public void Two_copies_of_one_product_ask_Windows_once_and_are_each_compared()
    {
        const string SecondCopy = @"C:\Windows\Installer\a2.msi";
        var f = ACopyBesideTheRecordedPackage();
        f.Packages.Declares(SecondCopy, ProductA);
        f.Files.Opens(SecondCopy, 5);
        f.Disk.AddFile(SecondCopy, new MockFileData(new byte[100]));

        var outcomes = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Package(Candidate), Package(SecondCopy) }, [], default, null, InInstallerFolder).Outcomes;

        Assert.All(outcomes, o => Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, o));
        Assert.Single(f.Msi.Asked);
        Assert.Single(f.Msi.PackageReads);
        Assert.Contains(Candidate, f.Files.Reads);
        Assert.Contains(SecondCopy, f.Files.Reads);
    }

    [Fact]
    public void The_composition_root_gives_the_check_both_file_readers()
    {
        // Constructed by hand everywhere else in this file. Without both readers the
        // check keeps every copy of an installed product, and nothing on any screen
        // would show that it had stopped comparing.
        using var services = new ServiceCollection().AddInstallerCleanCore().BuildServiceProvider();

        var check = Assert.IsType<DeclaredProductCheck>(services.GetRequiredService<IDeclaredProductCheck>());

        Assert.True(check.ComparesRecordedPackages);
    }

    // ---- The folder the installation was installed from ----
    //
    // Each installation of a product records the folder its package was installed from
    // as its InstallSource, and the check compares that folder as one more source
    // whether or not the list still holds it, read through the API and from the
    // installation's InstallProperties key. In the fixture it is the list's own first
    // entry, which is how Windows Installer records it at install, and the copy is let
    // through. Each test below changes one thing and leaves the list holding the
    // fixture's entry alone, so every keeping test is kept by the InstallSource.

    private const string OtherFolder = @"D:\Other\";
    private const string OtherPackage = @"D:\Other\setup.msi";

    [Fact]
    public void The_Installer_folder_as_the_folder_its_product_was_installed_from_keeps_the_copy_it_opens_as()
    {
        // The package there is setup.msi, a second copy of product A; the first copy is
        // another file.
        const string Named = @"C:\Windows\Installer\setup.msi";
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsInstallSource(ProductA, null, MsiInstallContext.Machine, InstallerFolder + @"\");
        f.Packages.Declares(Named, ProductA);
        f.Files.Opens(Named, 5);
        f.Disk.AddFile(Named, new MockFileData(new byte[100]));

        var outcomes = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen([Package(Candidate), Package(Named)], [], default, null, InInstallerFolder).Outcomes;

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, DeclaredProductOutcome.DeclaredProductInstalled },
            outcomes);
    }

    [Fact]
    public void A_copy_is_kept_when_the_package_in_the_folder_its_product_was_installed_from_is_the_copy_itself()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsInstallSource(ProductA, null, MsiInstallContext.Machine, OtherFolder);
        f.Files.Opens(OtherPackage, 1);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_package_in_the_folder_its_product_was_installed_from_will_not_identify()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsInstallSource(ProductA, null, MsiInstallContext.Machine, OtherFolder);
        f.Files.Answers(OtherPackage, FileIdentityRead.OpenRefused);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_let_through_when_the_package_in_the_folder_its_product_was_installed_from_is_another_file()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsInstallSource(ProductA, null, MsiInstallContext.Machine, OtherFolder);
        f.Files.Opens(OtherPackage, 9);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
        Assert.Contains(OtherPackage, f.Files.Reads);
    }

    [Theory]
    [InlineData(OtherFolder)]
    [InlineData(@"D:\Other")]
    public void A_copy_is_let_through_when_the_folder_its_product_was_installed_from_holds_no_package(string folder)
    {
        // A folder on a drive, and the same folder without its closing '\'. A folder on
        // the network is read by name, in the tests at the end.
        var package = (folder.EndsWith('\\') ? folder : folder + @"\") + SetupName;
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsInstallSource(ProductA, null, MsiInstallContext.Machine, folder);
        f.Files.Answers(package, FileIdentityRead.NamesNothing);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
        Assert.Contains(package, f.Files.Reads);
    }

    [Fact]
    public void The_package_in_the_folder_its_product_was_installed_from_is_read_once_where_the_list_holds_the_folder()
    {
        var f = ACopyBesideTheRecordedPackage();

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
        Assert.Single(f.Files.Reads, path => path == SetupPackage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_copy_is_let_through_when_the_installation_records_no_InstallSource(bool asAnEmptyValue)
    {
        // The API answers an empty value, or that the record does not carry the property,
        // and the key holds no InstallSource.
        var f = ACopyBesideTheRecordedPackage();
        if (asAnEmptyValue)
            f.Msi.RecordsInstallSource(ProductA, null, MsiInstallContext.Machine, "");
        else
            f.Msi.InstallSourceAnswers(ProductA, null, MsiInstallContext.Machine, MsiError.UnknownProperty);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, ScreenTheCopy(f));
        Assert.Contains(ProductAProperties, f.Msi.Registry.Reads);
    }

    [Fact]
    public void A_copy_is_kept_when_the_InstallSource_will_not_read()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.InstallSourceAnswers(ProductA, null, MsiInstallContext.Machine, MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData(RegistryKeyPresence.Absent)]
    [InlineData(RegistryKeyPresence.Unreadable)]
    public void A_copy_is_kept_when_the_InstallProperties_key_is_not_read(RegistryKeyPresence presence)
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Answers(ProductAProperties, presence);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData(OtherFolder)]
    [InlineData(@"d:\setup\")]
    [InlineData(@"D:\Setup")]
    [InlineData("")]
    public void A_copy_is_kept_when_the_InstallProperties_key_holds_another_InstallSource(string stored)
    {
        // The API answers the fixture's own folder.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAProperties, Sz(MsiInstallProperty.InstallSource, stored));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_InstallProperties_key_holds_no_InstallSource_while_the_API_answers_one()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAProperties);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_InstallProperties_key_holds_the_InstallSource_as_a_REG_EXPAND_SZ()
    {
        // The API's own text, so only the type keeps the copy.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAProperties, ExpandSz(MsiInstallProperty.InstallSource, SetupFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_InstallProperties_key_holds_the_InstallSource_as_a_value_of_another_type()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Registry.Holds(ProductAProperties, Dword(MsiInstallProperty.InstallSource));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_InstallProperties_key_holds_an_InstallSource_the_API_answers_as_none()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.InstallSourceAnswers(ProductA, null, MsiInstallContext.Machine, MsiError.UnknownProperty);
        f.Msi.Registry.Holds(ProductAProperties, Sz(MsiInstallProperty.InstallSource, SetupFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData(@"D:\%SETUP%\")]
    [InlineData("D:\\Set\0up\\")]
    public void A_copy_is_kept_when_the_folder_its_product_was_installed_from_holds_a_variable_or_a_null(string folder)
    {
        // Each is a folder on a drive holding no package, so only the '%' or the null keeps
        // the copy.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsInstallSource(ProductA, null, MsiInstallContext.Machine, folder);
        f.Files.Answers(folder + SetupName, FileIdentityRead.NamesNothing);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Theory]
    [InlineData("http://localhost/setup/")]
    [InlineData("file:///D:/Other/")]
    [InlineData(@"Other\")]
    [InlineData(@"D:Other\")]
    [InlineData(@"\Other\")]
    [InlineData("D:/Other/")]
    public void A_copy_is_kept_when_the_folder_its_product_was_installed_from_starts_neither_with_a_drive_letter_a_colon_and_a_backslash_nor_with_two_backslashes(
        string folder)
    {
        // Each names no package the file reader knows of, so only its form keeps the copy.
        var package = (folder.EndsWith('\\') ? folder : folder + @"\") + SetupName;
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsInstallSource(ProductA, null, MsiInstallContext.Machine, folder);
        f.Files.Answers(package, FileIdentityRead.NamesNothing);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    // ---- A patch copy and its patch's registrations ----
    //
    // A cached patch declares its own patch code and the products it may be applied to.
    // Windows Installer opens a registered patch's cached copy through the LocalPackage
    // value each registration records. A copy in the folder that no such value names is
    // let through only when EVERY registration of the patch records a copy that is
    // present, is another file and declares the same patch. Each test after the first is
    // the fixture with one thing changed.

    private const string PatchQ = "{33333333-3333-3333-3333-333333333333}";
    private const string PatchR = "{44444444-4444-4444-4444-444444444444}";
    private const string PatchCopy = @"C:\Windows\Installer\copy.msp";
    private const string RecordedPatch = @"C:\Windows\Installer\cached.msp";

    /// <summary>
    /// Patch Q, declaring product A as its target, registered against A's one
    /// per-machine installation and recording <see cref="RecordedPatch"/>, with both
    /// files on disk as two different files that both declare patch Q. The machine-wide
    /// patch enumeration lists that registration. Nothing about the patch's own source
    /// list is scripted, and the fake throws on a read nothing scripted, so a test using
    /// this fixture fails if the check reads that list. Each test changes one thing.
    /// </summary>
    private static (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk) APatchCopyBesideTheRecordedCopy()
    {
        var packages = new ScriptedPackageIdentities();
        packages.DeclaresPatch(PatchCopy, PatchQ, ProductA);
        packages.DeclaresPatch(RecordedPatch, PatchQ, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.HoldsPatch(PatchQ, ProductA, null, MsiInstallContext.Machine);
        msi.RecordsPatchPackage(PatchQ, ProductA, null, MsiInstallContext.Machine, RecordedPatch);

        var files = new ScriptedFileIdentities();
        files.Opens(PatchCopy, 1);
        files.Opens(RecordedPatch, 2);

        var disk = new MockFileSystem();
        disk.AddFile(PatchCopy, new MockFileData(new byte[100]));
        disk.AddFile(RecordedPatch, new MockFileData(new byte[100]));

        return (packages, msi, files, disk);
    }

    private static DeclaredProductOutcome ScreenThePatchCopy(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk) f,
        Func<string, bool?>? namesAFileInInstallerFolder = null,
        IReadOnlyList<ListedInstallation>? installations = null) =>
        ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Patch(PatchCopy) }, installations ?? [], default, null,
                namesAFileInInstallerFolder ?? InInstallerFolder).Outcomes[0];

    [Fact]
    public void A_patch_copy_beside_the_copy_its_registration_records_is_let_through()
    {
        var f = APatchCopyBesideTheRecordedCopy();

        var outcome = ScreenThePatchCopy(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, outcome);
        Assert.False(outcome.Withholds());
        // Both files were identified and both were read as patches, which is what the
        // verdict rests on.
        Assert.Contains(RecordedPatch, f.Files.Reads);
        Assert.Contains(PatchCopy, f.Files.Reads);
        Assert.Equal(new[] { PatchCopy, RecordedPatch }, f.Packages.PatchReads);
        // The registration the machine-wide enumeration listed is read for its copy and
        // not asked about again.
        Assert.Empty(f.Msi.PatchStateReads);
    }

    [Fact]
    public void A_patch_copy_is_kept_when_its_registration_records_no_copy()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchPackage(PatchQ, ProductA, null, MsiInstallContext.Machine, "");

        var outcome = ScreenThePatchCopy(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, outcome);
        Assert.True(outcome.Withholds());
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_registration_carries_no_package_property_at_all()
    {
        // ERROR_UNKNOWN_PROPERTY is a record that does not carry the value. It reads as
        // an empty value, and an empty value names no copy.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.PatchPackageReadAnswers(PatchQ, ProductA, null, MsiInstallContext.Machine, MsiError.UnknownProperty);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_recorded_copy_will_not_read()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.PatchPackageReadAnswers(PatchQ, ProductA, null, MsiInstallContext.Machine, MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_a_listed_registration_answers_that_the_patch_is_not_there()
    {
        // The enumeration listed the registration and the keyed read of its copy answers
        // that the patch is not registered against that product. The two answers
        // disagree, so which copy that registration records is not known.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.PatchPackageReadAnswers(PatchQ, ProductA, null, MsiInstallContext.Machine, MsiError.UnknownPatch);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_recorded_copy_is_not_on_disk()
    {
        // The identity fake still answers for the recorded path, so this is decided by
        // the file being absent and not by an identity that would not read.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Disk.RemoveFile(RecordedPatch);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_recorded_copy_path_names_a_folder()
    {
        // A folder opens to an identity like a file does, so without the file test a
        // value naming a folder would read as another copy.
        const string AFolder = @"C:\Windows\Installer\{33333333-3333-3333-3333-333333333333}";
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchPackage(PatchQ, ProductA, null, MsiInstallContext.Machine, AFolder);
        f.Files.Opens(AFolder, 4);
        f.Disk.AddDirectory(AFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_recorded_copy_opens_as_the_copy_itself()
    {
        // The registration names this very file under another spelling: a short name, a
        // long-path prefix, a link. The two paths open to one file ID.
        const string ShortName = @"C:\Windows\Installer\COPY~1.MSP";
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchPackage(PatchQ, ProductA, null, MsiInstallContext.Machine, ShortName);
        f.Packages.DeclaresPatch(ShortName, PatchQ, ProductA);
        f.Files.Opens(ShortName, 1);
        f.Disk.AddFile(ShortName, new MockFileData(new byte[100]));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_recorded_copy_will_not_identify()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Files.Answers(RecordedPatch, FileIdentityRead.OpenRefused);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_recorded_copy_declares_another_patch()
    {
        // The registration names a present file, and that file is not patch Q, so the
        // record shows nothing about where Q's cached copy is.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Packages.DeclaresPatch(RecordedPatch, PatchR, ProductA);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_recorded_copy_declares_nothing()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Packages.YieldsNothing(RecordedPatch, "patch summary stream would not open");

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_recorded_copy_reads_as_something_other_than_a_patch()
    {
        // The reading carries patch Q's code and is not marked as a patch, so it is not
        // the reading that was asked for and shows nothing about the file.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Packages.Yields(RecordedPatch, new PackageIdentity(PatchQ, IsPatch: false, Array.Empty<string>()));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_as_unidentified_when_the_copy_itself_will_not_identify()
    {
        // As for an installation package: every copy the registration opens was seen and
        // identified, and it is the copy's own identity that did not read.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Files.Answers(PatchCopy, FileIdentityRead.IdentityUnavailable);

        var outcome = ScreenThePatchCopy(f);

        Assert.Equal(DeclaredProductOutcome.CandidateIdentityUnestablished, outcome);
        Assert.True(outcome.Withholds());
    }

    [Theory]
    [InlineData(MsiInstallContext.UserManaged)]
    [InlineData(MsiInstallContext.UserUnmanaged)]
    public void A_patch_copy_is_kept_when_a_second_registration_records_no_copy(MsiInstallContext context)
    {
        // Product B holds patch Q as well, for a user, and records nothing, so the copy
        // that registration opens cannot be seen and this copy could be it. B is not in
        // the patch's own target list: the machine-wide enumeration is what names it.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.HoldsPatch(PatchQ, ProductB, UserSid, context);
        f.Msi.RecordsPatchPackage(PatchQ, ProductB, UserSid, context, "");

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_let_through_when_every_registration_records_the_same_present_copy()
    {
        // A patch is cached once and shared by the products holding it, so two
        // registrations name one file. Each registration's value is read and the file
        // they name is identified once.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.HoldsPatch(PatchQ, ProductB, UserSid, MsiInstallContext.UserManaged);
        f.Msi.RecordsPatchPackage(PatchQ, ProductB, UserSid, MsiInstallContext.UserManaged, RecordedPatch);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, ScreenThePatchCopy(f));
        Assert.Equal(2, f.Msi.PatchPackageReads.Count);
        Assert.Single(f.Files.Reads, p => p == RecordedPatch);
    }

    [Fact]
    public void A_patch_copy_is_let_through_when_a_registration_is_per_user_unmanaged()
    {
        // The copy that is let through above, with the second registration per user and
        // unmanaged. That registration's cached copy is read in its own account and
        // context like any other's, and it is the same present copy.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.HoldsPatch(PatchQ, ProductB, UserSid, MsiInstallContext.UserUnmanaged);
        f.Msi.RecordsPatchPackage(PatchQ, ProductB, UserSid, MsiInstallContext.UserUnmanaged, RecordedPatch);

        var outcome = ScreenThePatchCopy(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, outcome);
        Assert.False(outcome.Withholds());
        Assert.Contains((PatchQ, ProductB, (string?)UserSid, MsiInstallContext.UserUnmanaged), f.Msi.PatchPackageReads);
    }

    [Fact]
    public void A_patch_registration_in_a_user_account_is_read_in_that_account()
    {
        // The only registration is per user. Its copy is asked for in that account and
        // context, and the fake answers nothing else, so a read in any other place fails
        // the test rather than being answered.
        var packages = new ScriptedPackageIdentities();
        packages.DeclaresPatch(PatchCopy, PatchQ, ProductA);
        packages.DeclaresPatch(RecordedPatch, PatchQ, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA, (UserSid, MsiInstallContext.UserManaged));
        msi.HoldsPatch(PatchQ, ProductA, UserSid, MsiInstallContext.UserManaged);
        msi.RecordsPatchPackage(PatchQ, ProductA, UserSid, MsiInstallContext.UserManaged, RecordedPatch);

        var files = new ScriptedFileIdentities();
        files.Opens(PatchCopy, 1);
        files.Opens(RecordedPatch, 2);

        var disk = new MockFileSystem();
        disk.AddFile(PatchCopy, new MockFileData(new byte[100]));
        disk.AddFile(RecordedPatch, new MockFileData(new byte[100]));

        var outcome = ScreenThePatchCopy((packages, msi, files, disk));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, outcome);
        Assert.Equal(
            new[] { (PatchQ, ProductA, (string?)UserSid, MsiInstallContext.UserManaged) },
            msi.PatchPackageReads);
    }

    // ---- The patch's own source list ----
    //
    // A patch has a source list of its own. Microsoft documents that Windows Installer
    // needs the patch's source for an installation or a reinstallation when the cached
    // copy is missing. A registration whose cached copy is not there already keeps the
    // file, so the check does not read that list. Each test gives the check a reason a
    // source list would keep the copy, and the copy is let through.

    [Fact]
    public void A_patch_copy_is_let_through_when_its_patch_was_applied_from_it_in_the_Installer_folder()
    {
        // The patch's source is the Installer folder and its package name is this copy's
        // own name, so the source list names this file and no registration does.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, "copy.msp", InstallerFolder + @"\");

        var outcome = ScreenThePatchCopy(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, outcome);
        Assert.False(outcome.Withholds());
        Assert.Empty(f.Msi.PatchPackageNameReads);
        Assert.Empty(f.Msi.PatchSourceListWalks);
        Assert.Empty(f.Msi.Registry.Reads);
    }

    [Fact]
    public void A_patch_copy_is_let_through_when_the_screen_has_no_Installer_folder_to_compare_against()
    {
        // The Installer folder is what a source is compared against, and no source of the
        // patch is compared.
        var f = APatchCopyBesideTheRecordedCopy();

        var outcome = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Patch(PatchCopy) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, outcome);
    }

    [Fact]
    public void Without_the_registry_reader_a_patch_copy_is_let_through_and_no_key_is_read()
    {
        // The registry reader is what a source list is checked against, and no source list
        // of the patch is read.
        var f = APatchCopyBesideTheRecordedCopy();

        var outcome = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk)
            .Screen(new[] { Patch(PatchCopy) }, [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, outcome);
        Assert.Empty(f.Msi.Registry.Reads);
    }

    // ---- Finding the registrations ----
    //
    // Two ways, unioned. The machine-wide patch enumeration lists every registration it
    // will name. The keyed question puts the patch to each installation of each product
    // the patch declares it may be applied to, which reaches an installation the
    // enumeration does not list. Either can only add a registration.

    [Fact]
    public void A_patch_Windows_holds_no_registration_of_is_left_where_it_was()
    {
        // The must-miss half. Product A is installed and answers that patch Q is not
        // registered against it, and the enumeration lists nothing.
        var packages = new ScriptedPackageIdentities();
        packages.DeclaresPatch(PatchCopy, PatchQ, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.HoldsNoPatches();
        msi.PatchStateAnswers(PatchQ, ProductA, null, MsiInstallContext.Machine, MsiError.UnknownPatch);

        var outcome = ScriptedCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchNotRegistered, outcome);
        Assert.False(outcome.Withholds());
        Assert.Single(msi.PatchStateReads);
    }

    [Fact]
    public void A_patch_whose_target_products_are_not_installed_is_left_where_it_was()
    {
        // No installation of product A to ask, so the keyed question is not put at all.
        var packages = new ScriptedPackageIdentities();
        packages.DeclaresPatch(PatchCopy, PatchQ, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.UnknownProduct);
        msi.HoldsNoPatches();

        var outcome = ScriptedCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchNotRegistered, outcome);
        Assert.Empty(msi.PatchStateReads);
    }

    [Fact]
    public void A_registration_only_the_keyed_question_finds_keeps_the_copy_like_any_other()
    {
        // The enumeration lists nothing and product A answers that patch Q is registered
        // against it, recording no copy. The test above is this one with A answering the
        // other way.
        var packages = new ScriptedPackageIdentities();
        packages.DeclaresPatch(PatchCopy, PatchQ, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.HoldsNoPatches();
        msi.PatchState(PatchQ, ProductA, null, MsiInstallContext.Machine, "1");
        msi.RecordsPatchPackage(PatchQ, ProductA, null, MsiInstallContext.Machine, "");

        var outcome = ScriptedCheck(msi, packages, new ScriptedFileIdentities(), new MockFileSystem(), msi.Registry)
            .Screen(new[] { Patch(PatchCopy) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, outcome);
        Assert.True(outcome.Withholds());
    }

    [Fact]
    public void Copies_declaring_one_patch_for_different_products_are_each_asked_about_their_own()
    {
        // Two files carrying patch Q's code with different target lists. The keyed
        // question for the first finds nothing on product A; the second's target, product
        // B, holds the patch and records no copy. Each file is answered from its own
        // target list.
        const string OtherCopy = @"C:\Windows\Installer\other.msp";
        var packages = new ScriptedPackageIdentities();
        packages.DeclaresPatch(PatchCopy, PatchQ, ProductA);
        packages.DeclaresPatch(OtherCopy, PatchQ, ProductB);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.Installed(ProductB);
        msi.HoldsNoPatches();
        msi.PatchStateAnswers(PatchQ, ProductA, null, MsiInstallContext.Machine, MsiError.UnknownPatch);
        msi.PatchState(PatchQ, ProductB, null, MsiInstallContext.Machine, "1");
        msi.RecordsPatchPackage(PatchQ, ProductB, null, MsiInstallContext.Machine, "");

        var outcomes = ScriptedCheck(msi, packages, new ScriptedFileIdentities(), new MockFileSystem(), msi.Registry)
            .Screen(new[] { Patch(PatchCopy), Patch(OtherCopy) }, []).Outcomes;

        Assert.Equal(new[]
        {
            DeclaredProductOutcome.DeclaredPatchNotRegistered,
            DeclaredProductOutcome.DeclaredPatchRegistered,
        }, outcomes);
    }

    [Fact]
    public void Two_copies_of_one_patch_ask_Windows_once_and_are_each_compared()
    {
        const string SecondCopy = @"C:\Windows\Installer\copy2.msp";
        var f = APatchCopyBesideTheRecordedCopy();
        f.Packages.DeclaresPatch(SecondCopy, PatchQ, ProductA);
        f.Files.Opens(SecondCopy, 5);
        f.Disk.AddFile(SecondCopy, new MockFileData(new byte[100]));

        var outcomes = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Patch(PatchCopy), Patch(SecondCopy) }, [], default, null, InInstallerFolder).Outcomes;

        Assert.All(outcomes, o => Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, o));
        Assert.Equal(1, f.Msi.PatchEnumerations);
        Assert.Single(f.Msi.Asked);
        Assert.Single(f.Msi.PatchPackageReads);
        Assert.Contains(PatchCopy, f.Files.Reads);
        Assert.Contains(SecondCopy, f.Files.Reads);
    }

    [Fact]
    public void One_pass_walks_the_machine_wide_patch_enumeration_once_for_every_patch()
    {
        // Two different patches. The enumeration lists every patch on the machine, so the
        // second patch is answered from the same walk.
        const string OtherPatchCopy = @"C:\Windows\Installer\r.msp";
        var f = APatchCopyBesideTheRecordedCopy();
        f.Packages.DeclaresPatch(OtherPatchCopy, PatchR, ProductB);
        f.Msi.NotInstalled(ProductB, MsiError.UnknownProduct);

        var outcomes = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Patch(PatchCopy), Patch(OtherPatchCopy) }, [], default, null, InInstallerFolder).Outcomes;

        Assert.Equal(new[]
        {
            DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile,
            DeclaredProductOutcome.DeclaredPatchNotRegistered,
        }, outcomes);
        Assert.Equal(1, f.Msi.PatchEnumerations);
    }

    [Fact]
    public void Without_the_file_readers_a_registered_patch_s_copy_is_kept_and_nothing_is_read()
    {
        // The fixture that lets the copy through, handed to a check built without its two
        // file readers. The copy is kept, and neither the recorded copy nor any file's
        // identity is read.
        var f = APatchCopyBesideTheRecordedCopy();

        var outcome = ScriptedCheck(f.Msi, f.Packages)
            .Screen(new[] { Patch(PatchCopy) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, outcome);
        Assert.Empty(f.Msi.PatchPackageReads);
        Assert.Empty(f.Files.Reads);
    }

    // ---- A patch whose registrations the check cannot establish ----
    //
    // Each of these is the file failing to give the check a patch code and the products
    // to put it to, or Windows failing to answer one of the questions that find the
    // patch's registrations. Every one of them keeps the file, under the patch half's
    // own verdict rather than the product half's.

    [Fact]
    public void A_patch_that_will_not_yield_its_code_is_kept_back()
    {
        var packages = new ScriptedPackageIdentities();
        packages.YieldsNothing(PatchCopy, "patch summary stream would not open (1627)");

        var outcome = ScriptedCheck(new ScriptedMsiProducts(), packages)
            .Screen(new[] { Patch(PatchCopy) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcome);
        Assert.True(outcome.Withholds());
        Assert.Equal(new[] { PatchCopy }, packages.PatchReads);
    }

    [Fact]
    public void A_patch_reading_with_an_empty_code_is_kept_back()
    {
        var packages = new ScriptedPackageIdentities();
        packages.Yields(PatchCopy, new PackageIdentity(string.Empty, IsPatch: true, new[] { ProductA }));

        var outcome = ScriptedCheck(new ScriptedMsiProducts(), packages)
            .Screen(new[] { Patch(PatchCopy) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcome);
    }

    [Fact]
    public void A_patch_reading_that_comes_back_as_a_product_is_kept_back()
    {
        // The reading names a product, so the only thing stopping it is that it is not
        // marked as a patch.
        var packages = new ScriptedPackageIdentities();
        packages.Yields(PatchCopy, new PackageIdentity(PatchQ, IsPatch: false, new[] { ProductA }));

        var outcome = ScriptedCheck(new ScriptedMsiProducts(), packages)
            .Screen(new[] { Patch(PatchCopy) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcome);
    }

    [Fact]
    public void A_patch_reading_that_names_no_target_is_kept_back()
    {
        // With no product named, there is no installation to put the keyed question to.
        var packages = new ScriptedPackageIdentities();
        packages.Yields(PatchCopy, new PackageIdentity(PatchQ, IsPatch: true, Array.Empty<string>()));

        var outcome = ScriptedCheck(new ScriptedMsiProducts(), packages)
            .Screen(new[] { Patch(PatchCopy) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcome);
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_enumeration_will_not_start()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.PatchEnumerationAnswersAt(0, MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_enumeration_stops_part_way()
    {
        // One row, then a return that is not the end of the list: what lies past it is
        // unread.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.PatchEnumerationAnswersAt(1, MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_enumeration_does_not_end()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.PatchEnumerationNeverEnds(PatchR, ProductB);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_a_target_s_installations_will_not_list()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.Answers(ProductA, MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_keyed_question_is_not_answered()
    {
        var packages = new ScriptedPackageIdentities();
        packages.DeclaresPatch(PatchCopy, PatchQ, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.HoldsNoPatches();
        msi.PatchStateAnswers(PatchQ, ProductA, null, MsiInstallContext.Machine, MsiError.AccessDenied);

        var outcome = ScriptedCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcome);
    }

    [Fact]
    public void A_patch_copy_is_kept_when_a_listed_installation_answers_that_its_product_is_not_installed()
    {
        // The keyed product enumeration lists product A's one installation, and the keyed
        // patch read put to that same installation answers that the product is not
        // installed. That contradicts the listing, so nothing about the patch has been
        // established. A_patch_Windows_holds_no_registration_of_is_left_where_it_was is
        // this fixture with the installation answering that it holds no record of the
        // patch, which is the answer that lets the copy through.
        var packages = new ScriptedPackageIdentities();
        packages.DeclaresPatch(PatchCopy, PatchQ, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.HoldsNoPatches();
        msi.PatchStateAnswers(PatchQ, ProductA, null, MsiInstallContext.Machine, MsiError.UnknownProduct);

        var outcome = ScriptedCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, []).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcome);
        Assert.True(outcome.Withholds());
        Assert.Single(msi.PatchStateReads);
    }

    [Fact]
    public void A_patch_enumeration_that_fails_keeps_every_patch_copy_on_the_pass()
    {
        // Two copies declaring different patches for different products. The enumeration
        // is walked once for the pass, and its failure keeps both, each without the keyed
        // question being put. A package in the same pass is screened on its own terms.
        const string OtherPatchCopy = @"C:\Windows\Installer\r.msp";
        const string ThePackage = @"C:\Windows\Installer\a.msi";
        var packages = new ScriptedPackageIdentities();
        packages.DeclaresPatch(PatchCopy, PatchQ, ProductA);
        packages.DeclaresPatch(OtherPatchCopy, PatchR, ProductB);
        packages.Declares(ThePackage, ProductB);

        var msi = new ScriptedMsiProducts();
        msi.PatchEnumerationAnswersAt(0, MsiError.AccessDenied);
        msi.NotInstalled(ProductB, MsiError.UnknownProduct);

        var outcomes = ScriptedCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy), Package(ThePackage), Patch(OtherPatchCopy) }, []).Outcomes;

        Assert.Equal(new[]
        {
            DeclaredProductOutcome.DeclaredPatchUnestablished,
            DeclaredProductOutcome.DeclaredProductNotInstalled,
            DeclaredProductOutcome.DeclaredPatchUnestablished,
        }, outcomes);
        Assert.Equal(1, msi.PatchEnumerations);
        Assert.Empty(msi.PatchStateReads);
    }

    [Fact]
    public void A_patch_that_yields_no_identity_hands_on_the_reader_s_own_note()
    {
        var packages = new ScriptedPackageIdentities();
        packages.YieldsNothing(PatchCopy, note: "patch names no target product");

        var recorded = new List<(Exception Ex, string Cause)>();

        var outcomes = ScriptedCheck(new ScriptedMsiProducts(), packages)
            .Screen(new[] { Patch(PatchCopy) }, [],
                recordRefusal: (ex, cause) => recorded.Add((ex, cause))).Outcomes;

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcomes[0]);

        var only = Assert.Single(recorded);
        Assert.Equal("patch names no target product", only.Cause);
        Assert.Contains("patch names no target product", only.Ex.Message, StringComparison.Ordinal);

        // No path, for the reason the product half's test gives.
        Assert.DoesNotContain("copy.msp", only.Ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_patch_reading_that_answers_but_answers_nothing_useful_records_nothing()
    {
        // The three readings that stop the patch half without a null. Each is an answer
        // the reader gave, so it wrote no note about it and a log entry saying the file
        // "did not yield the patch code and target products it declares" would be untrue
        // of a file that yielded a reading.
        const string EmptyCode = @"C:\Windows\Installer\e.msp";
        const string AsAProduct = @"C:\Windows\Installer\p.msp";
        const string NoTarget = @"C:\Windows\Installer\n.msp";
        var packages = new ScriptedPackageIdentities();
        packages.Yields(EmptyCode, new PackageIdentity(string.Empty, IsPatch: true, new[] { ProductA }));
        packages.Yields(AsAProduct, new PackageIdentity(PatchQ, IsPatch: false, new[] { ProductA }));
        packages.Yields(NoTarget, new PackageIdentity(PatchQ, IsPatch: true, Array.Empty<string>()));

        var recorded = new List<Exception>();

        var outcomes = ScriptedCheck(new ScriptedMsiProducts(), packages)
            .Screen(new[] { Patch(EmptyCode), Patch(AsAProduct), Patch(NoTarget) }, [],
                recordRefusal: (ex, _) => recorded.Add(ex)).Outcomes;

        Assert.All(outcomes, o => Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, o));
        Assert.Empty(recorded);
    }

    [Fact]
    public void A_patch_that_yields_an_identity_records_nothing()
    {
        // The must-miss control for the two tests above. A screen that recorded on every
        // patch would satisfy the first while saying nothing about the refusal path.
        var f = APatchCopyBesideTheRecordedCopy();
        var recorded = new List<Exception>();

        var outcome = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Patch(PatchCopy) }, [], default, (ex, _) => recorded.Add(ex), InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, outcome);
        Assert.Empty(recorded);
    }

    // ---- Every answer about a product, held against the caller's own enumeration ----
    //
    // The caller has listed the installations of every product before the screen runs,
    // and the screen asks Windows for the installations of one product at a time. An
    // answer leaving out an installation the caller listed keeps the file: "not
    // installed" for a listed product, and a list short of a listed installation, for a
    // package's own product and for a product a patch names. Each keeping test has a
    // test beside it where the same answer agrees with the list and the file is let
    // through, so neither verdict can be the fixture's.

    /// <summary>A code whose hex digits are letters, so that its spelling has a case.</summary>
    private const string LetteredProduct = "{AAAABBBB-CCCC-DDDD-EEEE-FFFFAAAABBBB}";

    private static ListedInstallation ListedPerMachine(string productCode) =>
        new(productCode, null, (int)MsiInstallContext.Machine, SecondCopyNotRuledOut: false);

    [Theory]
    [InlineData(MsiError.NoMoreItems)]
    [InlineData(MsiError.UnknownProduct)]
    public void A_package_whose_listed_product_is_answered_not_installed_is_kept_back(uint absence)
    {
        // NoMoreItems_is_the_other_return_that_means_the_product_is_not_there and
        // A_package_Windows_says_it_does_not_hold_is_left_where_it_was are this answer
        // for a product nothing listed.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, absence);

        var outcome = ScriptedCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, [ListedPerMachine(ProductA)]).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcome);
        Assert.True(outcome.Withholds());
    }

    [Fact]
    public void A_package_whose_product_the_caller_did_not_list_is_answered_not_installed_as_before()
    {
        // The caller listed product B, so an answer that product A is not installed
        // contradicts nothing. Built without its file readers, the check reads no cached
        // package, so product B's own record is what shows it to be no second copy.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.UnknownProduct);
        msi.AnswersItsOwnRecord(ProductB, null, MsiInstallContext.Machine);

        var outcome = ScriptedCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, [ListedPerMachine(ProductB)]).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcome);
        Assert.False(outcome.Withholds());
    }

    [Fact]
    public void A_listed_code_spelled_in_another_case_is_held_against_the_answer_all_the_same()
    {
        // The caller hands back its own spelling of a code and the reader its own, so the
        // listed code here is in lower case and the file declares it in upper case.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", LetteredProduct);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(LetteredProduct, MsiError.NoMoreItems);

        var outcome = ScriptedCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") },
                [ListedPerMachine(LetteredProduct.ToLowerInvariant())]).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcome);
    }

    [Fact]
    public void A_copy_is_kept_when_the_answer_leaves_out_an_installation_the_caller_listed()
    {
        // Product A answers with its one per-machine installation, and the caller listed a
        // per-user one besides. That installation's package is not read, and the copy
        // could be it.
        var f = ACopyBesideTheRecordedPackage();

        var outcome = ScreenTheCopy(f, installations:
        [
            ListedPerMachine(ProductA),
            new ListedInstallation(ProductA, UserSid, (int)MsiInstallContext.UserManaged, SecondCopyNotRuledOut: false),
        ]);

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcome);
        Assert.Empty(f.Msi.PackageReads);
    }

    [Fact]
    public void A_copy_is_let_through_when_the_answer_holds_every_installation_the_caller_listed()
    {
        // The test above with product A answering both installations. The account is
        // listed in lower case, and is the same account. Each listed installation's cached
        // package is read once under the listed spelling, to see which code it declares,
        // and once under the answered spelling, to be compared with the copy.
        const string UsersPackage = @"C:\Windows\Installer\c.msi";
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Installed(ProductA,
            (null, MsiInstallContext.Machine),
            (UserSid, MsiInstallContext.UserManaged));
        f.Msi.RecordsPackage(ProductA, UserSid, MsiInstallContext.UserManaged, UsersPackage);
        f.Msi.RecordsPackage(ProductA, UserSid.ToLowerInvariant(), MsiInstallContext.UserManaged, UsersPackage);
        f.Msi.RecordsSources(ProductA, UserSid, MsiInstallContext.UserManaged, SetupName, SetupFolder);
        f.Packages.Declares(UsersPackage, ProductA);
        f.Files.Opens(UsersPackage, 3);
        f.Disk.AddFile(UsersPackage, new MockFileData(new byte[100]));

        var outcome = ScreenTheCopy(f, installations:
        [
            ListedPerMachine(ProductA),
            new ListedInstallation(ProductA, UserSid.ToLowerInvariant(), (int)MsiInstallContext.UserManaged,
                SecondCopyNotRuledOut: false),
        ]);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.Equal(4, f.Msi.PackageReads.Count);
    }

    [Fact]
    public void An_installation_listed_in_one_context_is_not_found_in_an_answer_in_another()
    {
        // One account, listed per user and managed, answered per user and unmanaged. An
        // installation in that context keeps the file on its own, as
        // DeclaredProductInstalled; this answer is the other verdict.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA, (UserSid, MsiInstallContext.UserUnmanaged));

        var outcome = ScriptedCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") },
                [new ListedInstallation(ProductA, UserSid, (int)MsiInstallContext.UserManaged, SecondCopyNotRuledOut: false)]).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcome);
    }

    [Fact]
    public void A_patch_whose_listed_target_is_answered_not_installed_is_kept_back()
    {
        // A_patch_whose_target_products_are_not_installed_is_left_where_it_was with the
        // caller having listed product A.
        var packages = new ScriptedPackageIdentities();
        packages.DeclaresPatch(PatchCopy, PatchQ, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.UnknownProduct);
        msi.HoldsNoPatches();

        var outcome = ScriptedCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, [ListedPerMachine(ProductA)]).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcome);
        Assert.True(outcome.Withholds());
        Assert.Empty(msi.PatchStateReads);
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_answer_about_its_target_leaves_out_an_installation_the_caller_listed()
    {
        var f = APatchCopyBesideTheRecordedCopy();

        var outcome = ScreenThePatchCopy(f, installations:
        [
            ListedPerMachine(ProductA),
            new ListedInstallation(ProductA, UserSid, (int)MsiInstallContext.UserManaged, SecondCopyNotRuledOut: false),
        ]);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcome);
    }

    [Fact]
    public void A_patch_copy_is_let_through_when_the_answer_about_its_target_holds_every_installation_the_caller_listed()
    {
        var f = APatchCopyBesideTheRecordedCopy();

        var outcome = ScreenThePatchCopy(f, installations: [ListedPerMachine(ProductA)]);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, outcome);
        Assert.False(outcome.Withholds());
    }

    // ---- Every installation the caller listed, asked about the patch ----
    //
    // A patch's Template names the products that can accept it, and the machine-wide
    // enumeration need not list every registration. Product B below is an installation
    // the caller listed that neither names: the keyed read put to it is the only way to
    // hear that it holds the patch.

    /// <summary>
    /// Patch Q, naming product A, which answers that it holds no record of Q. The
    /// machine-wide enumeration lists nothing. The caller listed A and B.
    /// </summary>
    private static (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi)
        APatchOnlyAnUnnamedInstallationCanHold()
    {
        var packages = new ScriptedPackageIdentities();
        packages.DeclaresPatch(PatchCopy, PatchQ, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);
        msi.HoldsNoPatches();
        msi.PatchStateAnswers(PatchQ, ProductA, null, MsiInstallContext.Machine, MsiError.UnknownPatch);

        return (packages, msi);
    }

    private static readonly ListedInstallation[] ListedAAndB =
        [ListedPerMachine(ProductA), ListedPerMachine(ProductB)];

    [Fact]
    public void A_listed_installation_the_patch_does_not_name_is_asked_and_its_registration_keeps_the_copy()
    {
        var (packages, msi) = APatchOnlyAnUnnamedInstallationCanHold();
        msi.PatchState(PatchQ, ProductB, null, MsiInstallContext.Machine, "1");

        var outcome = ScriptedCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, ListedAAndB).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, outcome);
        Assert.True(outcome.Withholds());
    }

    [Fact]
    public void The_same_installation_holding_no_record_of_the_patch_leaves_the_copy_where_it_was()
    {
        var (packages, msi) = APatchOnlyAnUnnamedInstallationCanHold();
        msi.PatchStateAnswers(PatchQ, ProductB, null, MsiInstallContext.Machine, MsiError.UnknownPatch);

        var outcome = ScriptedCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, ListedAAndB).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchNotRegistered, outcome);
        Assert.Contains((PatchQ, ProductB, (string?)null, MsiInstallContext.Machine), msi.PatchStateReads);
    }

    [Fact]
    public void The_same_installation_giving_no_answer_keeps_the_copy()
    {
        var (packages, msi) = APatchOnlyAnUnnamedInstallationCanHold();
        msi.PatchStateAnswers(PatchQ, ProductB, null, MsiInstallContext.Machine, 1610);

        var outcome = ScriptedCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, ListedAAndB).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcome);
    }

    [Fact]
    public void Copies_of_one_patch_naming_different_products_ask_each_installation_once()
    {
        // Two target lists put two questions about the patch, and each installation's
        // answer about it is the same whichever list asks.
        const string SecondCopy = @"C:\Windows\Installer\copy2.msp";
        var (packages, msi) = APatchOnlyAnUnnamedInstallationCanHold();
        packages.DeclaresPatch(SecondCopy, PatchQ, ProductA, ProductB);
        msi.Installed(ProductB);
        msi.PatchStateAnswers(PatchQ, ProductB, null, MsiInstallContext.Machine, MsiError.UnknownPatch);

        var outcomes = ScriptedCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy), Patch(SecondCopy) }, ListedAAndB).Outcomes;

        Assert.All(outcomes, o => Assert.Equal(DeclaredProductOutcome.DeclaredPatchNotRegistered, o));
        Assert.Equal(2, msi.PatchStateReads.Count);
    }

    [Fact]
    public void Where_a_registration_already_keeps_every_copy_the_other_installations_are_not_asked()
    {
        // The registration the enumeration lists records no copy, so every copy of the
        // patch is kept whatever else holds it. B is left unscripted, and a read put to it
        // would throw.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchPackage(PatchQ, ProductA, null, MsiInstallContext.Machine, "");

        var outcome = ScreenThePatchCopy(f, installations: ListedAAndB);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, outcome);
        Assert.Empty(f.Msi.PatchStateReads);
    }

    // ---- What the outcomes mean, pinned over the whole enum ----

    [Fact]
    public void Exactly_four_outcomes_let_a_file_through_and_an_unset_verdict_does_not()
    {
        // The rule is written as "anything but these four" so that a member added
        // later withholds rather than silently not withholding. This pins the
        // permitting set by name, so adding one that permits has to be a
        // deliberate edit here as well as there.
        var permitting = Enum.GetValues<DeclaredProductOutcome>()
            .Where(o => !o.Withholds())
            .ToArray();

        Assert.Equal(
            new[]
            {
                DeclaredProductOutcome.DeclaredProductNotInstalled,
                DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile,
                DeclaredProductOutcome.DeclaredPatchNotRegistered,
                DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile,
            },
            permitting);

        // And the value a verdict nobody set carries. An array of these is
        // allocated before anything fills it, so the zero has to keep the file.
        Assert.True(default(DeclaredProductOutcome).Withholds());
    }

    [Fact]
    public void A_file_that_yields_no_identity_hands_on_the_reader_s_own_note()
    {
        var identities = new ScriptedPackageIdentities();
        identities.YieldsNothing(@"C:\Windows\Installer\a.msi", note: "no Property table");

        var recorded = new List<(Exception Ex, string Cause)>();

        var outcomes = ScriptedCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, [],
                recordRefusal: (ex, cause) => recorded.Add((ex, cause))).Outcomes;

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcomes[0]);

        var only = Assert.Single(recorded);
        Assert.Equal("no Property table", only.Cause);
        Assert.Contains("no Property table", only.Ex.Message, StringComparison.Ordinal);

        // The path is not named, for the reason the reader's own contract gives: the
        // app runs elevated and this is read long after a report about another file.
        Assert.DoesNotContain(@"a.msi", only.Ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_reading_that_answers_but_answers_nothing_useful_records_nothing()
    {
        // THE DISTINCTION THE ARM'S COMMENT IS ABOUT, and without this nothing held it.
        // Three refusals share one arm. Only the first is the reader failing; the other
        // two are answers it gave, so it wrote no note about them and a log entry saying
        // the file "did not yield the product code it declares" would be untrue of a file
        // that yielded one. Moving the record outside the null test passes every other
        // test in this file.
        var identities = new ScriptedPackageIdentities();
        identities.Yields(@"C:\Windows\Installer\a.msi",
            new PackageIdentity(string.Empty, IsPatch: false, Array.Empty<string>()));

        var recorded = new List<Exception>();

        var outcomes = ScriptedCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, [],
                recordRefusal: (ex, _) => recorded.Add(ex)).Outcomes;

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcomes[0]);
        Assert.Empty(recorded);
    }

    [Fact]
    public void A_file_that_yields_an_identity_records_nothing()
    {
        // The must-miss control for the test above. A screen that recorded on every
        // candidate would satisfy it while saying nothing about the refusal path.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var recorded = new List<Exception>();

        ScriptedCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, [],
                recordRefusal: (ex, _) => recorded.Add(ex));

        Assert.Empty(recorded);
    }

    // ---- An installation registered under a code its cached package does not declare ----
    //
    // A second copy of a program, installed under an instance transform, is registered
    // under the product code the transform produced, while the original package it was
    // installed from declares the base code, and so can the package cached for it. That
    // original can be a file in the Installer folder that the copy's source list names. A
    // copy declaring a code is therefore put to every installation whose cached package
    // declares that code as well as to the installations of the code itself, and each of
    // those installations is read by the code it is registered under.

    private const string SecondCopy = "{33333333-3333-3333-3333-333333333333}";
    private const string SecondCopysPackage = @"C:\Windows\Installer\copy.msi";

    /// <summary>
    /// Product A is installed nowhere under its own code. One installation, in the
    /// context given and another user's account where the context has one, is registered
    /// as <see cref="SecondCopy"/>, and the package cached for it declares product A. The
    /// candidate declares product A. Each test changes one thing.
    ///
    /// THE INSTALLATION IS LISTED AS RULED OUT AS A SECOND COPY unless
    /// <paramref name="marked"/> says otherwise, as the scan lists one whose InstanceType
    /// read as ordinary. Another user's per-user installation can read that way, its
    /// InstanceType answering as absent to this process.
    /// </summary>
    private static (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk, ListedInstallation[] Listed)
        ACopyBesideASecondCopy(MsiInstallContext context = MsiInstallContext.UserUnmanaged, bool marked = false)
    {
        var sid = context == MsiInstallContext.Machine ? null : OtherUserSid;

        var packages = new ScriptedPackageIdentities();
        packages.Declares(Candidate, ProductA);
        packages.Declares(SecondCopysPackage, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.UnknownProduct);
        msi.RecordsPackage(SecondCopy, sid, context, SecondCopysPackage);

        var files = new ScriptedFileIdentities();
        files.Opens(Candidate, 1);
        files.Opens(SecondCopysPackage, 2);

        var disk = new MockFileSystem();
        disk.AddFile(Candidate, new MockFileData(new byte[100]));
        disk.AddFile(SecondCopysPackage, new MockFileData(new byte[100]));

        return (packages, msi, files, disk, [new ListedInstallation(SecondCopy, sid, (int)context, marked)]);
    }

    private static IReadOnlyList<DeclaredProductOutcome> ScreenBesideTheSecondCopy(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk, ListedInstallation[] Listed) f,
        OrphanedFile[]? candidates = null,
        Action<Exception, string>? recordRefusal = null,
        IRunningAccount? account = null) =>
        ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, account)
            .Screen(candidates ?? [Package(Candidate)], f.Listed, default, recordRefusal, InInstallerFolder).Outcomes;

    /// <summary>A running account as a test names it.</summary>
    private sealed class Account(string? sid) : IRunningAccount
    {
        public string? Sid => sid;
    }

    /// <summary>The account the second copy's fixture puts a per-user installation in.</summary>
    private static readonly IRunningAccount TheOwner = new Account(OtherUserSid);

    /// <summary>An account other than the one the fixture puts a per-user installation in.</summary>
    private static readonly IRunningAccount SomebodyElse = new Account(UserSid);

    [Fact]
    public void A_copy_declaring_the_code_another_users_second_copy_caches_is_kept()
    {
        // THE SHAPE THIS SECTION IS FOR. Nothing is installed under product A, so the
        // code the candidate declares has no installation of its own, and the second copy
        // that was installed from it is registered under another code. The copy is per
        // user and unmanaged, whose source list is not read, so the candidate is kept.
        var f = ACopyBesideASecondCopy();

        var outcome = ScreenBesideTheSecondCopy(f)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.True(outcome.Withholds());
        // Read by the code it is registered under, and never by the code its package
        // declares. Inside this context the verdict is the same either way, so the reads
        // are what show which code was used.
        Assert.Contains((SecondCopy, (string?)OtherUserSid, MsiInstallContext.UserUnmanaged), f.Msi.PackageReads);
        Assert.DoesNotContain(f.Msi.PackageReads, read => read.ProductCode == ProductA);
    }

    [Fact]
    public void A_copy_is_let_through_where_the_other_installations_cached_package_declares_its_own_code()
    {
        // The must-miss half of the test above: the same installation, whose cached
        // package declares the code it is registered under. It is no second copy of
        // product A, and product A is not installed.
        var f = ACopyBesideASecondCopy();
        f.Packages.Declares(SecondCopysPackage, SecondCopy);

        var outcome = ScreenBesideTheSecondCopy(f)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcome);
        Assert.False(outcome.Withholds());
    }

    [Fact]
    public void A_copy_declaring_a_code_no_installation_caches_is_let_through_beside_a_second_copy()
    {
        // The second copy is put to the files declaring product A and to no others, so a
        // machine holding one keeps its offer of everything else.
        const string OtherCandidate = @"C:\Windows\Installer\b2.msi";
        var f = ACopyBesideASecondCopy();
        f.Packages.Declares(OtherCandidate, ProductB);
        f.Msi.NotInstalled(ProductB, MsiError.UnknownProduct);

        var outcomes = ScreenBesideTheSecondCopy(f, [Package(Candidate), Package(OtherCandidate)]);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcomes[0]);
        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcomes[1]);
    }

    [Fact]
    public void A_second_copy_in_any_context_is_asked_about_its_sources_by_the_code_it_is_registered_under()
    {
        // A per-machine second copy installed from the candidate, in the Installer folder.
        // Its source list is read, by its own code, and its package there is the candidate,
        // so the candidate is kept. The fake answers no read of product A, so a source read
        // by the declared code fails the test rather than passing it.
        var f = ACopyBesideASecondCopy(MsiInstallContext.Machine);
        f.Msi.RecordsSources(SecondCopy, null, MsiInstallContext.Machine, "a.msi", InstallerFolder + @"\");

        var outcome = ScreenBesideTheSecondCopy(f)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.Equal(new[] { (SecondCopy, (string?)null, MsiInstallContext.Machine) }, f.Msi.PackageNameReads);
    }

    [Fact]
    public void A_per_machine_second_copy_installed_from_elsewhere_lets_the_copy_through()
    {
        // The must-miss half of the test above: the second copy was installed from a
        // folder that no longer holds its package, and its cached package is another
        // file, so nothing it opens is the candidate.
        var f = ACopyBesideASecondCopy(MsiInstallContext.Machine);
        f.Msi.RecordsSources(SecondCopy, null, MsiInstallContext.Machine, SetupName, SetupFolder);
        f.Files.Answers(SetupPackage, FileIdentityRead.NamesNothing);

        var outcome = ScreenBesideTheSecondCopy(f)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.False(outcome.Withholds());
    }

    [Fact]
    public void An_installation_listed_in_another_case_is_not_taken_for_a_second_copy_of_itself()
    {
        // The code the cached package declares comes back in the reader's own spelling
        // and the listed code in the caller's. Compared as text, an ordinary installation
        // would look registered under a code its package does not declare, and would be
        // asked about a second time under the listed spelling, which the fake does not
        // answer for its sources.
        var f = ACopyBesideASecondCopy(MsiInstallContext.Machine);
        f.Packages.Declares(Candidate, LetteredProduct);
        f.Packages.Declares(SecondCopysPackage, LetteredProduct);
        f.Msi.Installed(LetteredProduct);
        f.Msi.RecordsPackage(LetteredProduct, null, MsiInstallContext.Machine, SecondCopysPackage);
        f.Msi.RecordsPackage(LetteredProduct.ToLowerInvariant(), null, MsiInstallContext.Machine, SecondCopysPackage);
        f.Msi.RecordsSources(LetteredProduct, null, MsiInstallContext.Machine, SetupName, SetupFolder);
        f.Files.Answers(SetupPackage, FileIdentityRead.NamesNothing);
        f = f with { Listed = [ListedPerMachine(LetteredProduct.ToLowerInvariant())] };

        var outcome = ScreenBesideTheSecondCopy(f)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
    }

    /// <summary>
    /// The ways an installation's cached package can fail to say what it declares. NoIdentity
    /// is a file the reader could not read; MalformedCode is one it read and found declaring
    /// no product code it could use, a ProductCode that is not a well-formed GUID among them.
    /// </summary>
    public enum CachedPackageFault { ReadFails, NamesNoPackage, NotAFile, NoIdentity, APatch, NoCode, MalformedCode }

    private static void Break(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk, ListedInstallation[] Listed) f,
        CachedPackageFault fault, string? sid, MsiInstallContext context)
    {
        switch (fault)
        {
            case CachedPackageFault.ReadFails:
                f.Msi.PackageReadAnswers(SecondCopy, sid, context, MsiError.AccessDenied);
                break;
            case CachedPackageFault.NamesNoPackage:
                f.Msi.RecordsPackage(SecondCopy, sid, context, string.Empty);
                break;
            case CachedPackageFault.NotAFile:
                f.Disk.RemoveFile(SecondCopysPackage);
                break;
            case CachedPackageFault.NoIdentity:
                f.Packages.YieldsNothing(SecondCopysPackage);
                break;
            case CachedPackageFault.APatch:
                f.Packages.DeclaresPatch(SecondCopysPackage, "{44444444-4444-4444-4444-444444444444}", ProductA);
                break;
            case CachedPackageFault.NoCode:
                f.Packages.Yields(SecondCopysPackage, new PackageIdentity(string.Empty, IsPatch: false, []));
                break;
            case CachedPackageFault.MalformedCode:
                f.Packages.DeclaresNoCode(SecondCopysPackage);
                break;
        }
    }

    [Theory]
    [InlineData(CachedPackageFault.ReadFails)]
    [InlineData(CachedPackageFault.NamesNoPackage)]
    [InlineData(CachedPackageFault.NotAFile)]
    [InlineData(CachedPackageFault.NoIdentity)]
    [InlineData(CachedPackageFault.APatch)]
    [InlineData(CachedPackageFault.NoCode)]
    [InlineData(CachedPackageFault.MalformedCode)]
    public void Every_installation_package_is_kept_while_another_accounts_cached_package_does_not_say_what_it_declares(
        CachedPackageFault fault)
    {
        // That installation could be a second copy of any program, and the candidate its
        // original package, so no candidate can be put to it or ruled out. It belongs to
        // an account other than the one this process runs as, so its own record is not
        // asked. The second candidate declares a code no installation holds, and is kept
        // all the same. The refusal is recorded once for the pass, not once per file.
        const string OtherCandidate = @"C:\Windows\Installer\b2.msi";
        var f = ACopyBesideASecondCopy();
        f.Packages.Declares(OtherCandidate, ProductB);
        f.Msi.NotInstalled(ProductB, MsiError.UnknownProduct);
        Break(f, fault, OtherUserSid, MsiInstallContext.UserUnmanaged);
        var recorded = new List<Exception>();

        var outcomes = ScreenBesideTheSecondCopy(f, [Package(Candidate), Package(OtherCandidate)],
            (ex, _) => recorded.Add(ex), SomebodyElse);

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome));
        Assert.Single(recorded);
        Assert.Empty(f.Msi.RecordReads);
    }

    [Theory]
    [InlineData(CachedPackageFault.NoIdentity, "the cached package would not read")]
    [InlineData(CachedPackageFault.MalformedCode, "the cached package declares no product code")]
    public void A_cached_package_the_reader_wrote_no_note_about_is_logged_by_what_the_reader_answered(
        CachedPackageFault fault, string cause)
    {
        var f = ACopyBesideASecondCopy();
        Break(f, fault, OtherUserSid, MsiInstallContext.UserUnmanaged);
        var recorded = new List<string>();

        ScreenBesideTheSecondCopy(f, recordRefusal: (_, detail) => recorded.Add(detail), account: SomebodyElse);

        Assert.Equal(new[] { cause }, recorded);
    }

    [Theory]
    [InlineData(CachedPackageFault.ReadFails)]
    [InlineData(CachedPackageFault.NamesNoPackage)]
    [InlineData(CachedPackageFault.NotAFile)]
    [InlineData(CachedPackageFault.NoIdentity)]
    [InlineData(CachedPackageFault.APatch)]
    [InlineData(CachedPackageFault.NoCode)]
    [InlineData(CachedPackageFault.MalformedCode)]
    public void A_per_machine_cached_package_that_does_not_say_what_it_declares_keeps_nothing_where_its_record_shows_an_ordinary_installation(
        CachedPackageFault fault)
    {
        // The must-miss half of the theory above: the same faults on a per-machine
        // installation whose record answers, with a package code and an ordinary
        // InstanceType. No account is compared for a per-machine installation, so it is
        // the same whether the check knows the account it runs as or not.
        foreach (var account in new[] { TheOwner, null })
        {
            var f = ACopyBesideASecondCopy(MsiInstallContext.Machine);
            Break(f, fault, null, MsiInstallContext.Machine);
            f.Msi.AnswersItsOwnRecord(SecondCopy, null, MsiInstallContext.Machine);
            var recorded = new List<Exception>();

            var outcome = ScreenBesideTheSecondCopy(f, recordRefusal: (ex, _) => recorded.Add(ex),
                account: account)[0];

            Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcome);
            Assert.Empty(recorded);
        }
    }

    // ---- The installation's own record, where its cached package does not read ----
    //
    // Each test below breaks the cached package of the second copy's installation the
    // same way, so nothing links it, and changes one thing about what its own record
    // answers or whose it is.

    /// <summary>
    /// The second copy's fixture in the context given, its cached package unread, the
    /// installation belonging to <see cref="TheOwner"/> where the context has an account.
    /// </summary>
    private static (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk, ListedInstallation[] Listed)
        AnUnreadCachedPackage(MsiInstallContext context = MsiInstallContext.UserUnmanaged)
    {
        var f = ACopyBesideASecondCopy(context);
        Break(f, CachedPackageFault.ReadFails, f.Listed[0].UserSid, context);
        return f;
    }

    [Fact]
    public void The_running_accounts_own_installation_whose_record_reads_ordinary_keeps_nothing()
    {
        var f = AnUnreadCachedPackage();
        f.Msi.AnswersItsOwnRecord(SecondCopy, OtherUserSid, MsiInstallContext.UserUnmanaged);
        var recorded = new List<Exception>();

        var outcome = ScreenBesideTheSecondCopy(f, recordRefusal: (ex, _) => recorded.Add(ex),
            account: TheOwner)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcome);
        Assert.Empty(recorded);
        // Read by the code the installation is registered under, in its own account and
        // context.
        Assert.Equal(
            new[]
            {
                (MsiInstallProperty.PackageCode, SecondCopy, (string?)OtherUserSid, MsiInstallContext.UserUnmanaged),
                (MsiInstallProperty.InstanceType, SecondCopy, (string?)OtherUserSid, MsiInstallContext.UserUnmanaged),
            },
            f.Msi.RecordReads);
    }

    [Fact]
    public void A_record_without_an_InstanceType_value_is_an_ordinary_installation()
    {
        var f = AnUnreadCachedPackage();
        f.Msi.AnswersItsOwnRecord(SecondCopy, OtherUserSid, MsiInstallContext.UserUnmanaged, instanceType: null);

        var outcome = ScreenBesideTheSecondCopy(f, account: TheOwner)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcome);
    }

    [Fact]
    public void The_account_is_compared_without_regard_to_case()
    {
        var f = AnUnreadCachedPackage();
        f.Msi.AnswersItsOwnRecord(SecondCopy, OtherUserSid, MsiInstallContext.UserUnmanaged);

        var outcome = ScreenBesideTheSecondCopy(f, account: new Account(OtherUserSid.ToLowerInvariant()))[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcome);
    }

    [Fact]
    public void A_record_showing_a_second_copy_keeps_every_installation_package()
    {
        var f = AnUnreadCachedPackage();
        f.Msi.AnswersItsOwnRecord(SecondCopy, OtherUserSid, MsiInstallContext.UserUnmanaged, instanceType: "1");

        var outcome = ScreenBesideTheSecondCopy(f, account: TheOwner)[0];

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome);
    }

    [Fact]
    public void An_InstanceType_that_will_not_read_keeps_every_installation_package()
    {
        var f = AnUnreadCachedPackage();
        f.Msi.AnswersItsOwnRecord(SecondCopy, OtherUserSid, MsiInstallContext.UserUnmanaged);
        f.Msi.InstanceTypeAnswers(SecondCopy, OtherUserSid, MsiInstallContext.UserUnmanaged, MsiError.AccessDenied);

        var outcome = ScreenBesideTheSecondCopy(f, account: TheOwner)[0];

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome);
    }

    [Theory]
    [InlineData(MsiError.UnknownProperty, "")]
    [InlineData(MsiError.AccessDenied, "")]
    [InlineData(MsiError.Success, "")]
    public void A_package_code_that_does_not_come_back_as_a_value_keeps_every_installation_package(
        uint error, string value)
    {
        // ERROR_UNKNOWN_PROPERTY is what a record that did not answer gives, and it reads
        // as an empty value, as an InstanceType the record does not carry does.
        var f = AnUnreadCachedPackage();
        f.Msi.AnswersItsOwnRecord(SecondCopy, OtherUserSid, MsiInstallContext.UserUnmanaged);
        f.Msi.PackageCodeAnswers(SecondCopy, OtherUserSid, MsiInstallContext.UserUnmanaged, error, value);

        var outcome = ScreenBesideTheSecondCopy(f, account: TheOwner)[0];

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome);
    }

    [Fact]
    public void Without_the_running_account_a_per_user_installation_keeps_every_installation_package()
    {
        var f = AnUnreadCachedPackage();
        f.Msi.AnswersItsOwnRecord(SecondCopy, OtherUserSid, MsiInstallContext.UserUnmanaged);

        var withNoCheckAccount = ScreenBesideTheSecondCopy(f)[0];
        var withAnAccountNotRead = ScreenBesideTheSecondCopy(f, account: new Account(null))[0];

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, withNoCheckAccount);
        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, withAnAccountNotRead);
        Assert.Empty(f.Msi.RecordReads);
    }

    [Fact]
    public void A_per_user_managed_installation_is_held_to_its_account_as_well()
    {
        // Its own record answering is not enough where it belongs to another account.
        var ownAccount = AnUnreadCachedPackage(MsiInstallContext.UserManaged);
        ownAccount.Msi.AnswersItsOwnRecord(SecondCopy, OtherUserSid, MsiInstallContext.UserManaged);
        var otherAccount = AnUnreadCachedPackage(MsiInstallContext.UserManaged);
        otherAccount.Msi.AnswersItsOwnRecord(SecondCopy, OtherUserSid, MsiInstallContext.UserManaged);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled,
            ScreenBesideTheSecondCopy(ownAccount, account: TheOwner)[0]);
        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished,
            ScreenBesideTheSecondCopy(otherAccount, account: SomebodyElse)[0]);
    }

    [Fact]
    public void A_per_machine_installation_whose_record_does_not_answer_keeps_every_installation_package()
    {
        var f = AnUnreadCachedPackage(MsiInstallContext.Machine);
        f.Msi.AnswersItsOwnRecord(SecondCopy, null, MsiInstallContext.Machine);
        f.Msi.PackageCodeAnswers(SecondCopy, null, MsiInstallContext.Machine, MsiError.UnknownProperty);

        var outcome = ScreenBesideTheSecondCopy(f, account: TheOwner)[0];

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome);
    }

    // ---- What the pass counts of the cached packages and records it read ----
    //
    // The census the screening carries, which the report sends. Each test reads the census
    // of one pass over the second copy's fixture.

    private static CachedPackageCensus CensusBesideTheSecondCopy(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk, ListedInstallation[] Listed) f,
        IRunningAccount? account = null) =>
        ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, account)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder).CachedPackages;

    private static CachedPackageCensus Census(
        int read = 1, int pathUnreadable = 0, int noneRecorded = 0, int notThere = 0, int wouldNotRead = 0,
        int noProductCode = 0, int anotherAccount = 0, int packageCodeUnanswered = 0,
        int instanceTypeNotOrdinary = 0, int perMachine = 0, int released = 0,
        int unruled = 0, int unseenPathUnreadable = 0, int unseenNoneRecorded = 0, int unseenNotThere = 0,
        int unseenWouldNotIdentify = 0, int unseenWouldNotRead = 0, int unseenNoProductCode = 0,
        int unseenPerUserUnmanaged = 0, int unseenSourcesGivenUp = 0, int unseenSourceNotRuledOut = 0,
        int unseenPerMachine = 0, int unseenByName = 0, int opensNoPackage = 0, int releasedBySources = 0,
        int keptOtherAnswer = 0, int keptRegistryDisagrees = 0, int keptSourcesNotRuledOut = 0) =>
        new(read, pathUnreadable, noneRecorded, notThere, wouldNotRead, noProductCode,
            anotherAccount, packageCodeUnanswered, instanceTypeNotOrdinary, perMachine, released,
            unruled, unseenPathUnreadable, unseenNoneRecorded, unseenNotThere, unseenWouldNotIdentify,
            unseenWouldNotRead, unseenNoProductCode, unseenPerUserUnmanaged, unseenSourcesGivenUp,
            unseenSourceNotRuledOut, unseenPerMachine, unseenByName, opensNoPackage, releasedBySources,
            keptOtherAnswer, keptRegistryDisagrees, keptSourcesNotRuledOut);

    [Theory]
    [InlineData(CachedPackageFault.ReadFails)]
    [InlineData(CachedPackageFault.NamesNoPackage)]
    [InlineData(CachedPackageFault.NotAFile)]
    [InlineData(CachedPackageFault.NoIdentity)]
    [InlineData(CachedPackageFault.APatch)]
    [InlineData(CachedPackageFault.NoCode)]
    [InlineData(CachedPackageFault.MalformedCode)]
    public void An_installation_keeping_every_installation_package_is_counted_by_what_its_cached_package_gave(
        CachedPackageFault fault)
    {
        // Another account's installation, whose record is not read. It is counted once by what
        // its cached package gave and once by why its record did not settle it.
        var f = ACopyBesideASecondCopy();
        Break(f, fault, OtherUserSid, MsiInstallContext.UserUnmanaged);

        var census = CensusBesideTheSecondCopy(f, SomebodyElse);

        var expected = fault switch
        {
            CachedPackageFault.ReadFails => Census(pathUnreadable: 1, anotherAccount: 1),
            CachedPackageFault.NamesNoPackage => Census(noneRecorded: 1, anotherAccount: 1, keptSourcesNotRuledOut: 1),
            CachedPackageFault.NotAFile => Census(notThere: 1, anotherAccount: 1),
            CachedPackageFault.NoIdentity => Census(wouldNotRead: 1, anotherAccount: 1),
            _ => Census(noProductCode: 1, anotherAccount: 1),
        };
        Assert.Equal(expected, census);
    }

    [Fact]
    public void A_per_machine_installation_whose_package_code_does_not_answer_is_counted_as_per_machine()
    {
        var f = AnUnreadCachedPackage(MsiInstallContext.Machine);
        f.Msi.AnswersItsOwnRecord(SecondCopy, null, MsiInstallContext.Machine);
        f.Msi.PackageCodeAnswers(SecondCopy, null, MsiInstallContext.Machine, MsiError.UnknownProperty);

        Assert.Equal(
            Census(pathUnreadable: 1, packageCodeUnanswered: 1, perMachine: 1),
            CensusBesideTheSecondCopy(f, TheOwner));
    }

    [Fact]
    public void The_running_accounts_own_installation_read_as_a_second_instance_is_counted_by_its_InstanceType()
    {
        var f = AnUnreadCachedPackage();
        f.Msi.AnswersItsOwnRecord(SecondCopy, OtherUserSid, MsiInstallContext.UserUnmanaged, instanceType: "1");

        Assert.Equal(
            Census(pathUnreadable: 1, instanceTypeNotOrdinary: 1),
            CensusBesideTheSecondCopy(f, TheOwner));
    }

    [Fact]
    public void An_InstanceType_that_will_not_read_is_counted_with_one_read_as_a_second_instance()
    {
        var f = AnUnreadCachedPackage();
        f.Msi.AnswersItsOwnRecord(SecondCopy, OtherUserSid, MsiInstallContext.UserUnmanaged);
        f.Msi.InstanceTypeAnswers(SecondCopy, OtherUserSid, MsiInstallContext.UserUnmanaged, MsiError.AccessDenied);

        Assert.Equal(
            Census(pathUnreadable: 1, instanceTypeNotOrdinary: 1),
            CensusBesideTheSecondCopy(f, TheOwner));
    }

    [Fact]
    public void An_installation_whose_record_shows_it_ordinary_is_counted_as_keeping_nothing()
    {
        var f = AnUnreadCachedPackage(MsiInstallContext.Machine);
        f.Msi.AnswersItsOwnRecord(SecondCopy, null, MsiInstallContext.Machine);

        Assert.Equal(Census(released: 1), CensusBesideTheSecondCopy(f, TheOwner));
    }

    [Fact]
    public void An_installation_whose_cached_package_declares_a_code_is_counted_as_read_and_nothing_else()
    {
        // Linked under the code its cached package declares, so it keeps nothing.
        var f = ACopyBesideASecondCopy();

        Assert.Equal(Census(), CensusBesideTheSecondCopy(f, SomebodyElse));
    }

    [Fact]
    public void Every_listed_installation_is_counted_once_in_a_pass_of_many_candidates()
    {
        // The installations are read once for the pass, whichever candidate reaches them
        // first, so a second candidate adds nothing to the counts. The second candidate
        // declares a product that is not installed and not listed, so it reaches the
        // installations after the first, and the hold they set keeps it.
        const string OtherCandidate = @"C:\Windows\Installer\b2.msi";
        const string ProductD = "{77777777-7777-7777-7777-777777777777}";
        var f = ACopyBesideASecondCopy();
        f.Packages.Declares(OtherCandidate, ProductD);
        f.Msi.NotInstalled(ProductD, MsiError.UnknownProduct);
        f.Msi.RecordsPackage(ProductB, null, MsiInstallContext.Machine, string.Empty);
        f.Msi.PackageCodeAnswers(ProductB, null, MsiInstallContext.Machine, MsiError.UnknownProperty);
        f.Msi.InstanceTypeAnswers(ProductB, null, MsiInstallContext.Machine, MsiError.UnknownProperty);
        f.Msi.Registry.Holds(InstallPropertiesKey("22222222222222222222222222222222"));
        f.Msi.PackageNameAnswers(ProductB, null, MsiInstallContext.Machine, MsiError.UnknownProduct);
        f = f with { Listed = [.. f.Listed, ListedPerMachine(ProductB)] };

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[1]);
        Assert.Equal(Census(read: 2, noneRecorded: 1, packageCodeUnanswered: 1, perMachine: 1, keptOtherAnswer: 1),
            screening.CachedPackages);
        Assert.Single(f.Msi.RecordReads,
            read => read == (MsiInstallProperty.PackageCode, ProductB, (string?)null, MsiInstallContext.Machine));
    }

    [Fact]
    public void A_pass_in_which_no_installation_package_reaches_the_installations_counts_nothing()
    {
        // The only candidate yields no product code of its own, so the pass never asks about
        // a product and never reads an installation's cached package. A census of zeros then
        // says the step did not run, where one read installation says it ran.
        var f = ACopyBesideASecondCopy();
        f.Packages.YieldsNothing(Candidate);

        Assert.Equal(CachedPackageCensus.None, CensusBesideTheSecondCopy(f, SomebodyElse));
        Assert.Empty(f.Msi.PackageReads);
    }

    [Fact]
    public void A_check_without_its_file_readers_counts_no_installation()
    {
        // It reads no cached package, so it has no reason to give for one, and it counts the
        // installation nowhere rather than under a reason it did not meet. Every installation
        // package is kept all the same.
        var f = ACopyBesideASecondCopy();

        var screening = ScriptedCheck(f.Msi, f.Packages, runningAccount: SomebodyElse)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(CachedPackageCensus.None, screening.CachedPackages);
        Assert.True(screening.Outcomes[0].Withholds());
    }

    // ---- An installation with no package to open ----
    //
    // Office Click-to-Run can register Office's features with Windows Installer as a product
    // of its own, per machine, recording no cached package, no package code and no source
    // list. Windows Installer has nothing to open for it, so it sets no hold. Where any one
    // of the answers that show it changes, its sources are read instead, and as they cannot be
    // ruled out, it keeps every installation package.
    // The tests after the first change those answers one at a time, or set the registration
    // beside a copy declaring its own code, beside other installations, or beside a source
    // list that appears or goes while the pass runs.

    /// <summary>The product code Office Click-to-Run registers Office's features under.</summary>
    private const string OfficeFeatures = "{9AC08E99-230B-47e8-9721-4577B7F124EA}";

    /// <summary>
    /// Its <c>SourceList</c> key, the code in the packed form the registry holds it under.
    /// </summary>
    private const string OfficeFeaturesSourceList =
        @"SOFTWARE\Classes\Installer\Products\99E80CA9B0328E74791254777B1F42AE\SourceList";

    /// <summary>Its <c>InstallProperties</c> key.</summary>
    private const string OfficeFeaturesInstallProperties =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\"
        + @"99E80CA9B0328E74791254777B1F42AE\InstallProperties";

    /// <summary>
    /// The candidate declares product A, which is not installed. The one installation listed
    /// is Office's feature registration, per machine, answering as Windows answers for it: no
    /// LocalPackage or InstallSource (ERROR_UNKNOWN_PROPERTY), an empty PackageCode and
    /// InstanceType, its package name ERROR_UNKNOWN_PRODUCT as a product property and
    /// ERROR_BAD_CONFIGURATION off the source list, and neither its InstallProperties key nor
    /// its SourceList key there. Each test starts from it.
    /// </summary>
    private static (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk, ListedInstallation[] Listed)
        ACopyBesideOfficesFeatureRegistration(bool marked = false)
    {
        var packages = new ScriptedPackageIdentities();
        packages.Declares(Candidate, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.UnknownProduct);
        AnswersAsOfficesFeatureRegistration(msi, OfficeFeatures);
        msi.Registry.Answers(OfficeFeaturesSourceList, RegistryKeyPresence.Absent);
        msi.Registry.Answers(OfficeFeaturesInstallProperties, RegistryKeyPresence.Absent);

        var files = new ScriptedFileIdentities();
        files.Opens(Candidate, 1);

        var disk = new MockFileSystem();
        disk.AddFile(Candidate, new MockFileData(new byte[100]));

        return (packages, msi, files, disk,
            [new ListedInstallation(OfficeFeatures, null, (int)MsiInstallContext.Machine, marked)]);
    }

    /// <summary>
    /// The API's answers for Office's feature registration, per machine, under
    /// <paramref name="code"/>. The registry keys are scripted by the caller.
    /// </summary>
    private static void AnswersAsOfficesFeatureRegistration(ScriptedMsiProducts msi, string code)
    {
        msi.PackageReadAnswers(code, null, MsiInstallContext.Machine, MsiError.UnknownProperty);
        msi.PackageCodeAnswers(code, null, MsiInstallContext.Machine, MsiError.Success);
        msi.InstanceTypeAnswers(code, null, MsiInstallContext.Machine, MsiError.Success);
        msi.PackageNameAnswers(code, null, MsiInstallContext.Machine, MsiError.UnknownProduct);
        msi.InstallSourceAnswers(code, null, MsiInstallContext.Machine, MsiError.UnknownProperty);
        msi.SourceListPackageNameAnswers(code, null, MsiInstallContext.Machine, MsiError.BadConfiguration);
    }

    [Fact]
    public void Offices_feature_registration_keeps_nothing()
    {
        var f = ACopyBesideOfficesFeatureRegistration();
        var recorded = new List<Exception>();

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, (ex, _) => recorded.Add(ex), InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, screening.Outcomes[0]);
        Assert.Empty(recorded);
        Assert.Equal(Census(opensNoPackage: 1), screening.CachedPackages);
        // Read by the API and at both keys, the keys by the spelling the registry holds.
        Assert.Equal(new[] { (OfficeFeatures, (string?)null, MsiInstallContext.Machine) },
            f.Msi.SourceListPackageNameReads);
        Assert.Contains(OfficeFeaturesSourceList, f.Msi.Registry.Reads);
        Assert.Contains(OfficeFeaturesInstallProperties, f.Msi.Registry.Reads);
    }

    /// <summary>One answer about Office's feature registration that does not show it has no package to open.</summary>
    public enum NoPackageFault
    {
        InstallPropertiesThere,
        InstallPropertiesUnreadable,
        SourceListThere,
        SourceListUnreadable,
        PackageNameUnknownProduct,
        PackageNameAccessDenied,
        PackageNameEmpty,
        PackageNameRead,
        LocalPackageUnreadable,
    }

    private static void Break(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk, ListedInstallation[] Listed) f,
        NoPackageFault fault)
    {
        const MsiInstallContext Machine = MsiInstallContext.Machine;
        switch (fault)
        {
            case NoPackageFault.InstallPropertiesThere:
                f.Msi.Registry.Holds(OfficeFeaturesInstallProperties,
                    new RegistryValue(MsiInstallProperty.LocalPackage, RegistryValueKind.String,
                        @"C:\Windows\Installer\gone.msi"));
                break;
            case NoPackageFault.InstallPropertiesUnreadable:
                f.Msi.Registry.Answers(OfficeFeaturesInstallProperties, RegistryKeyPresence.Unreadable);
                break;
            case NoPackageFault.SourceListThere:
                f.Msi.Registry.Holds(OfficeFeaturesSourceList);
                break;
            case NoPackageFault.SourceListUnreadable:
                f.Msi.Registry.Answers(OfficeFeaturesSourceList, RegistryKeyPresence.Unreadable);
                break;
            case NoPackageFault.PackageNameUnknownProduct:
                f.Msi.SourceListPackageNameAnswers(OfficeFeatures, null, Machine, MsiError.UnknownProduct);
                break;
            case NoPackageFault.PackageNameAccessDenied:
                f.Msi.SourceListPackageNameAnswers(OfficeFeatures, null, Machine, MsiError.AccessDenied);
                break;
            case NoPackageFault.PackageNameEmpty:
                f.Msi.SourceListPackageNameAnswers(OfficeFeatures, null, Machine, MsiError.Success);
                break;
            case NoPackageFault.PackageNameRead:
                // A package name that reads as a value, which is the answer a source list gives.
                f.Msi.RecordsSources(OfficeFeatures, null, Machine, SetupName, SetupFolder);
                f.Msi.SourceListPackageNameAnswers(OfficeFeatures, null, Machine, MsiError.MoreData);
                break;
            case NoPackageFault.LocalPackageUnreadable:
                f.Msi.PackageReadAnswers(OfficeFeatures, null, Machine, MsiError.AccessDenied);
                break;
        }
    }

    [Theory]
    [InlineData(NoPackageFault.InstallPropertiesThere)]
    [InlineData(NoPackageFault.InstallPropertiesUnreadable)]
    [InlineData(NoPackageFault.SourceListThere)]
    [InlineData(NoPackageFault.SourceListUnreadable)]
    [InlineData(NoPackageFault.PackageNameUnknownProduct)]
    [InlineData(NoPackageFault.PackageNameAccessDenied)]
    [InlineData(NoPackageFault.PackageNameEmpty)]
    [InlineData(NoPackageFault.PackageNameRead)]
    [InlineData(NoPackageFault.LocalPackageUnreadable)]
    public void A_registration_not_shown_to_have_no_package_to_open_keeps_every_installation_package(
        NoPackageFault fault)
    {
        var f = ACopyBesideOfficesFeatureRegistration();
        Break(f, fault);
        var recorded = new List<Exception>();

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, (ex, _) => recorded.Add(ex), InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[0]);
        Assert.Single(recorded);
        // Each is counted by the answer that kept it: one that shows a source list, or none
        // where the registry disagrees or the API answers otherwise. Its sources are read all
        // the same and cannot be ruled out.
        var keptBy = fault switch
        {
            NoPackageFault.PackageNameUnknownProduct or NoPackageFault.PackageNameAccessDenied =>
                Census(noneRecorded: 1, packageCodeUnanswered: 1, perMachine: 1, keptOtherAnswer: 1),
            NoPackageFault.PackageNameEmpty or NoPackageFault.PackageNameRead =>
                Census(noneRecorded: 1, packageCodeUnanswered: 1, perMachine: 1, keptSourcesNotRuledOut: 1),
            NoPackageFault.LocalPackageUnreadable => Census(pathUnreadable: 1, packageCodeUnanswered: 1, perMachine: 1),
            _ => Census(noneRecorded: 1, packageCodeUnanswered: 1, perMachine: 1, keptRegistryDisagrees: 1),
        };
        Assert.Equal(keptBy, screening.CachedPackages);
    }

    [Fact]
    public void A_per_user_registration_answering_the_same_keeps_every_installation_package()
    {
        // Per user and managed, in the running account, so its own record is read. Whether it
        // has a source list is not asked, and its sources do not read.
        const MsiInstallContext Managed = MsiInstallContext.UserManaged;
        var f = ACopyBesideOfficesFeatureRegistration();
        f.Msi.PackageReadAnswers(OfficeFeatures, OtherUserSid, Managed, MsiError.UnknownProperty);
        f.Msi.PackageCodeAnswers(OfficeFeatures, OtherUserSid, Managed, MsiError.Success);
        f.Msi.Registry.Holds(InstallPropertiesKey("99E80CA9B0328E74791254777B1F42AE", OtherUserSid));
        f.Msi.PackageNameAnswers(OfficeFeatures, OtherUserSid, Managed, MsiError.UnknownProduct);
        f.Msi.SourceListPackageNameAnswers(OfficeFeatures, OtherUserSid, Managed, MsiError.BadConfiguration);
        f = f with { Listed = [new ListedInstallation(OfficeFeatures, OtherUserSid, (int)Managed, false)] };

        var outcome = ScreenBesideTheSecondCopy(f, account: TheOwner)[0];

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome);
        Assert.Empty(f.Msi.SourceListPackageNameReads);
    }

    [Fact]
    public void Without_the_registry_reader_the_registration_keeps_every_installation_package()
    {
        var f = ACopyBesideOfficesFeatureRegistration();

        var outcome = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, runningAccount: TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome);
    }

    [Fact]
    public void Every_such_registration_on_the_machine_has_to_show_it_has_no_package_to_open()
    {
        // Two registrations of the same shape keep nothing between them; one of them with its
        // SourceList key there keeps every installation package.
        const string Other = "{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}";
        const string OtherPacked = "AAAAAAAABBBBCCCCDDDDEEEEEEEEEEEE";
        foreach (var otherHasNoSources in new[] { true, false })
        {
            var f = ACopyBesideOfficesFeatureRegistration();
            AnswersAsOfficesFeatureRegistration(f.Msi, Other);
            f.Msi.Registry.Answers(
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\{OtherPacked}\InstallProperties",
                RegistryKeyPresence.Absent);
            if (otherHasNoSources)
                f.Msi.Registry.Answers($@"SOFTWARE\Classes\Installer\Products\{OtherPacked}\SourceList",
                    RegistryKeyPresence.Absent);
            else
                f.Msi.Registry.Holds($@"SOFTWARE\Classes\Installer\Products\{OtherPacked}\SourceList");
            f = f with { Listed = [.. f.Listed, ListedPerMachine(Other)] };

            var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
                .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder);

            Assert.Equal(
                otherHasNoSources
                    ? DeclaredProductOutcome.DeclaredProductNotInstalled
                    : DeclaredProductOutcome.SecondCopyUnestablished,
                screening.Outcomes[0]);
            Assert.Equal(
                otherHasNoSources
                    ? Census(read: 2, opensNoPackage: 2)
                    : Census(read: 2, noneRecorded: 1, packageCodeUnanswered: 1, perMachine: 1, opensNoPackage: 1,
                        keptRegistryDisagrees: 1),
                screening.CachedPackages);
        }
    }

    [Fact]
    public void A_copy_declaring_the_registrations_own_code_is_let_through()
    {
        // The registration answers for the code the candidate declares, and opens nothing it
        // could be, so the candidate goes on to everything else.
        var f = ACopyBesideOfficesFeatureRegistration();
        f.Packages.Declares(Candidate, OfficeFeatures);
        f.Msi.Installed(OfficeFeatures);

        var outcome = ScreenBesideTheSecondCopy(f, account: TheOwner)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.False(outcome.Withholds());
    }

    [Fact]
    public void A_copy_declaring_the_registrations_code_is_kept_where_its_source_list_is_there()
    {
        // The must-miss half of the test above.
        var f = ACopyBesideOfficesFeatureRegistration();
        f.Packages.Declares(Candidate, OfficeFeatures);
        f.Msi.Installed(OfficeFeatures);
        f.Msi.Registry.Holds(OfficeFeaturesSourceList);

        var outcome = ScreenBesideTheSecondCopy(f, account: TheOwner)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void An_ordinary_registration_with_no_package_to_open_is_counted_as_one(bool noSources)
    {
        // The registration's record reads as ordinary, so it sets no hold either way. With no
        // package to open it lets the copy declaring its code through and is counted as having
        // none; with its source list there it keeps the copy and is counted as ordinary.
        var f = ACopyBesideOfficesFeatureRegistration();
        f.Packages.Declares(Candidate, OfficeFeatures);
        f.Msi.Installed(OfficeFeatures);
        f.Msi.AnswersItsOwnRecord(OfficeFeatures, null, MsiInstallContext.Machine);
        if (!noSources) f.Msi.Registry.Holds(OfficeFeaturesSourceList);

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(
            noSources
                ? DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile
                : DeclaredProductOutcome.DeclaredProductInstalled,
            screening.Outcomes[0]);
        Assert.Equal(noSources ? Census(opensNoPackage: 1) : Census(released: 1), screening.CachedPackages);
    }

    [Fact]
    public void Every_other_installation_of_the_registrations_code_is_still_read()
    {
        // A second installation of the same code, per user and unmanaged, whose source list
        // is not read, so the candidate is kept for it whatever the registration opens.
        const string OthersPackage = @"C:\Windows\Installer\other.msi";
        var f = ACopyBesideOfficesFeatureRegistration();
        f.Packages.Declares(Candidate, OfficeFeatures);
        f.Packages.Declares(OthersPackage, OfficeFeatures);
        f.Msi.Installed(OfficeFeatures, (null, MsiInstallContext.Machine), (OtherUserSid, MsiInstallContext.UserUnmanaged));
        f.Msi.RecordsPackage(OfficeFeatures, OtherUserSid, MsiInstallContext.UserUnmanaged, OthersPackage);
        f.Files.Opens(OthersPackage, 2);
        f.Disk.AddFile(OthersPackage, new MockFileData(new byte[100]));
        f = f with
        {
            Listed = [.. f.Listed, new ListedInstallation(OfficeFeatures, OtherUserSid, (int)MsiInstallContext.UserUnmanaged, false)],
        };

        var outcome = ScreenBesideTheSecondCopy(f, account: TheOwner)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.Contains((OfficeFeatures, (string?)OtherUserSid, MsiInstallContext.UserUnmanaged), f.Msi.PackageReads);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void An_installation_not_ruled_out_as_a_second_copy_opens_nothing_where_it_has_no_package_to_open(
        bool noSources)
    {
        // Its record reads as ordinary, so it sets no hold, and it is listed as not ruled out
        // as a second copy, so its packages are read for every candidate.
        var f = ACopyBesideOfficesFeatureRegistration(marked: true);
        f.Msi.AnswersItsOwnRecord(OfficeFeatures, null, MsiInstallContext.Machine);
        if (!noSources) f.Msi.Registry.Holds(OfficeFeaturesSourceList);

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(
            noSources ? DeclaredProductOutcome.DeclaredProductNotInstalled : DeclaredProductOutcome.SecondCopyUnestablished,
            screening.Outcomes[0]);
        Assert.Equal(
            noSources
                ? Census(opensNoPackage: 1, unruled: 1)
                : Census(released: 1, unruled: 1, unseenNoneRecorded: 1, unseenPerMachine: 1),
            screening.CachedPackages);
    }

    [Fact]
    public void A_source_list_found_after_the_hold_was_settled_keeps_the_copy_declaring_the_code()
    {
        // The registration has no source list when the hold's step asks and has one when the
        // step reading the installations of the candidate's own code asks.
        var f = ACopyBesideOfficesFeatureRegistration();
        f.Packages.Declares(Candidate, OfficeFeatures);
        f.Msi.Installed(OfficeFeatures);
        f.Msi.SourceListPackageNameAnswersInTurn(OfficeFeatures, null, MsiInstallContext.Machine,
            MsiError.BadConfiguration, MsiError.Success);

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, screening.Outcomes[0]);
        Assert.Equal(Census(opensNoPackage: 1), screening.CachedPackages);
        Assert.Equal(2, f.Msi.SourceListPackageNameReads.Count);
    }

    [Fact]
    public void A_source_list_found_after_the_hold_was_settled_keeps_the_copies_beside_a_possible_second_copy()
    {
        // The same, where the registration is not ruled out as a second copy and its packages
        // are read for the candidate after the hold's step.
        var f = ACopyBesideOfficesFeatureRegistration(marked: true);
        f.Msi.SourceListPackageNameAnswersInTurn(OfficeFeatures, null, MsiInstallContext.Machine,
            MsiError.BadConfiguration, MsiError.Success);

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[0]);
        Assert.Equal(Census(opensNoPackage: 1, unruled: 1, unseenNoneRecorded: 1, unseenPerMachine: 1),
            screening.CachedPackages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_package_found_to_open_stands_for_the_rest_of_the_pass(bool declaredInUpperCase)
    {
        // The registration's record reads as ordinary. The hold's step finds a package for it
        // to open, and the step reading the installations of the candidate's own code takes
        // that answer without asking, in either spelling of the code, though an ask would now
        // find none.
        var f = ACopyBesideOfficesFeatureRegistration();
        f.Msi.AnswersItsOwnRecord(OfficeFeatures, null, MsiInstallContext.Machine);
        f.Msi.SourceListPackageNameAnswersInTurn(OfficeFeatures, null, MsiInstallContext.Machine,
            MsiError.Success, MsiError.BadConfiguration);
        var declared = OfficeFeatures;
        if (declaredInUpperCase)
        {
            declared = OfficeFeatures.ToUpperInvariant();
            Assert.NotEqual(OfficeFeatures, declared);
            AnswersAsOfficesFeatureRegistration(f.Msi, declared);
        }
        f.Packages.Declares(Candidate, declared);
        f.Msi.Installed(declared);

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, screening.Outcomes[0]);
        Assert.Equal(Census(released: 1), screening.CachedPackages);
        Assert.Contains((declared, (string?)null, MsiInstallContext.Machine), f.Msi.PackageReads);
        Assert.Equal(new[] { (OfficeFeatures, (string?)null, MsiInstallContext.Machine) },
            f.Msi.SourceListPackageNameReads);
    }

    [Fact]
    public void The_composition_root_gives_the_check_the_running_account()
    {
        using var services = new ServiceCollection().AddInstallerCleanCore().BuildServiceProvider();

        var check = Assert.IsType<DeclaredProductCheck>(services.GetRequiredService<IDeclaredProductCheck>());

        Assert.True(check.KnowsTheRunningAccount);
    }

    [Fact]
    public void An_InstallProperties_key_holding_no_cached_package_shows_there_is_none_as_an_absent_key_does()
    {
        var f = ACopyBesideOfficesFeatureRegistration();
        f.Msi.Registry.Holds(OfficeFeaturesInstallProperties,
            new RegistryValue("DisplayName", RegistryValueKind.String, "Office"));

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, screening.Outcomes[0]);
        Assert.Equal(Census(opensNoPackage: 1), screening.CachedPackages);
    }

    [Theory]
    [InlineData("LocalPackage")]
    [InlineData("ManagedLocalPackage")]
    [InlineData("localpackage")]
    public void An_InstallProperties_key_holding_a_cached_package_keeps_every_installation_package(string valueName)
    {
        // The API answers that the registration records no cached package and the registry
        // holds one, so the two do not agree that it has none.
        var f = ACopyBesideOfficesFeatureRegistration();
        f.Msi.Registry.Holds(OfficeFeaturesInstallProperties,
            new RegistryValue(valueName, RegistryValueKind.String, @"C:\Windows\Installer\gone.msi"));

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[0]);
        Assert.Equal(Census(noneRecorded: 1, packageCodeUnanswered: 1, perMachine: 1, keptRegistryDisagrees: 1),
            screening.CachedPackages);
    }

    [Theory]
    [InlineData(NoPackageFault.PackageNameUnknownProduct,
        "the installation records no cached package, and Windows Installer answered 1605 when asked its "
        + "source list's package name")]
    [InlineData(NoPackageFault.SourceListThere,
        "the installation records no cached package, and the registry holds a cached package or a source "
        + "list where Windows Installer answers that it has none, or a key would not read")]
    [InlineData(NoPackageFault.PackageNameRead,
        "the installation records no cached package, and a source it names could not be ruled out")]
    public void A_registration_with_no_cached_package_is_logged_by_the_answer_that_kept_it(
        NoPackageFault fault, string detail)
    {
        var f = ACopyBesideOfficesFeatureRegistration();
        Break(f, fault);
        var recorded = new List<string>();

        ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, (_, note) => recorded.Add(note), InInstallerFolder);

        Assert.Equal(new[] { detail }, recorded);
    }

    // ---- An installation with no cached package, judged by its sources ----
    //
    // An installation that records no cached package, and is not shown to have no package to
    // open, can open only what its source list names, so its sources are read once its
    // InstallProperties key shows it records none as well. Where every package they name is
    // seen, the installation sets no hold, and every installation package is compared with
    // those packages instead. Where one cannot be ruled out, or the key holds a cached package
    // or will not read, the installation keeps every installation package. Each test starts
    // from the fixture below and changes one thing.

    /// <summary>The code the installation recording no cached package is registered under.</summary>
    private const string NoCachedPackage = "{99999999-9999-9999-9999-999999999999}";

    /// <summary>A second installation of the same shape, registered under another code.</summary>
    private const string AnotherWithNoCachedPackage = "{BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB}";

    /// <summary>A URL entry, which no source list read here can rule out.</summary>
    private const string SetupUrl = "https://example.com/setup/";

    /// <summary>
    /// The candidate declares product A, which is not installed. The one installation listed is
    /// registered per machine under <see cref="NoCachedPackage"/>, records no cached package
    /// (ERROR_UNKNOWN_PROPERTY) and answers an empty PackageCode, so its record does not show an
    /// ordinary installation. It was installed from <see cref="SetupPackage"/>, which is no
    /// longer there. Its source list and its InstallProperties key are held in the registry as
    /// the API answers them, the key holding its InstallSource and no cached package.
    /// </summary>
    private static (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk, ListedInstallation[] Listed)
        ACopyBesideAnInstallationWithNoCachedPackage(bool marked = false)
    {
        var packages = new ScriptedPackageIdentities();
        packages.Declares(Candidate, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.UnknownProduct);
        RecordsNoCachedPackage(msi, NoCachedPackage);

        var files = new ScriptedFileIdentities();
        files.Opens(Candidate, 1);
        files.Answers(SetupPackage, FileIdentityRead.NamesNothing);

        var disk = new MockFileSystem();
        disk.AddFile(Candidate, new MockFileData(new byte[100]));

        return (packages, msi, files, disk,
            [new ListedInstallation(NoCachedPackage, null, (int)MsiInstallContext.Machine, marked)]);
    }

    /// <summary>
    /// The answers of a per-machine installation under <paramref name="code"/> that records no
    /// cached package, answers an empty PackageCode and InstanceType, and was installed from
    /// <see cref="SetupFolder"/>.
    /// </summary>
    private static void RecordsNoCachedPackage(ScriptedMsiProducts msi, string code)
    {
        const MsiInstallContext Machine = MsiInstallContext.Machine;
        msi.PackageReadAnswers(code, null, Machine, MsiError.UnknownProperty);
        msi.PackageCodeAnswers(code, null, Machine, MsiError.Success);
        msi.InstanceTypeAnswers(code, null, Machine, MsiError.Success);
        msi.RecordsSources(code, null, Machine, SetupName, SetupFolder);
    }

    private static DeclaredProductScreening ScreenBesideIt(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk, ListedInstallation[] Listed) f,
        OrphanedFile[]? candidates = null,
        Action<Exception, string>? recordRefusal = null) =>
        ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen(candidates ?? [Package(Candidate)], f.Listed, default, recordRefusal, InInstallerFolder);

    [Fact]
    public void An_installation_with_no_cached_package_whose_sources_are_all_seen_keeps_nothing()
    {
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        var recorded = new List<Exception>();

        var screening = ScreenBesideIt(f, recordRefusal: (ex, _) => recorded.Add(ex));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, screening.Outcomes[0]);
        Assert.Empty(recorded);
        Assert.Equal(Census(releasedBySources: 1), screening.CachedPackages);
        Assert.Contains(SetupPackage, f.Files.Reads);
    }

    [Fact]
    public void A_copy_its_source_in_the_Installer_folder_opens_as_is_kept_and_another_copy_let_through()
    {
        // Installed from a.msi in the Installer folder, which is the candidate. The other
        // candidate declares another product and is another file, so it goes on to the rest of
        // the check.
        const string OtherCandidate = @"C:\Windows\Installer\b2.msi";
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        f.Msi.RecordsSources(NoCachedPackage, null, MsiInstallContext.Machine, CandidateName, InstallerFolder + @"\");
        f.Packages.Declares(OtherCandidate, ProductB);
        f.Msi.NotInstalled(ProductB, MsiError.UnknownProduct);
        f.Files.Opens(OtherCandidate, 2);
        f.Disk.AddFile(OtherCandidate, new MockFileData(new byte[100]));

        var screening = ScreenBesideIt(f, [Package(Candidate), Package(OtherCandidate)]);

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductInstalled, DeclaredProductOutcome.DeclaredProductNotInstalled },
            screening.Outcomes);
        Assert.Equal(Census(releasedBySources: 1), screening.CachedPackages);
    }

    [Fact]
    public void An_installation_with_no_cached_package_and_a_source_not_ruled_out_keeps_every_installation_package()
    {
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        f.Msi.RecordsUrls(NoCachedPackage, isPatch: false, null, MsiInstallContext.Machine, SetupUrl);
        var recorded = new List<Exception>();

        var screening = ScreenBesideIt(f, recordRefusal: (ex, _) => recorded.Add(ex));

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[0]);
        Assert.Single(recorded);
        Assert.Equal(
            Census(noneRecorded: 1, packageCodeUnanswered: 1, perMachine: 1, keptSourcesNotRuledOut: 1),
            screening.CachedPackages);
    }

    [Fact]
    public void An_ordinary_installation_with_no_cached_package_keeps_nothing_and_its_sources_are_not_read()
    {
        // Its record shows an ordinary installation, so it sets no hold, and a URL on its list
        // would keep every installation package if its sources were read for the hold.
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        f.Msi.AnswersItsOwnRecord(NoCachedPackage, null, MsiInstallContext.Machine);
        f.Msi.RecordsUrls(NoCachedPackage, isPatch: false, null, MsiInstallContext.Machine, SetupUrl);

        var screening = ScreenBesideIt(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, screening.Outcomes[0]);
        Assert.Equal(Census(released: 1), screening.CachedPackages);
        Assert.Empty(f.Msi.PackageNameReads);
    }

    [Fact]
    public void Every_installation_with_no_cached_package_is_counted_by_what_its_sources_showed()
    {
        // The first installation's sources are all seen and the second's hold a URL, so the
        // second keeps every installation package and both are counted.
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        RecordsNoCachedPackage(f.Msi, AnotherWithNoCachedPackage);
        f.Msi.RecordsUrls(AnotherWithNoCachedPackage, isPatch: false, null, MsiInstallContext.Machine, SetupUrl);
        f = f with { Listed = [.. f.Listed, ListedPerMachine(AnotherWithNoCachedPackage)] };

        var screening = ScreenBesideIt(f);

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[0]);
        Assert.Equal(
            Census(read: 2, noneRecorded: 1, packageCodeUnanswered: 1, perMachine: 1, releasedBySources: 1,
                keptSourcesNotRuledOut: 1),
            screening.CachedPackages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_hold_set_at_a_drive_given_up_counts_its_files_there_only_where_nothing_else_set_it(
        bool heldOtherwiseToo)
    {
        // The installation's source package on drive D: does not answer within the time limit.
        // Where a second installation, read after it, keeps every installation package for a
        // URL on its list, the file is kept whatever the drive answers, so it is not counted
        // towards the drive.
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        f.Files.Opens(SetupPackage, 9);
        if (heldOtherwiseToo)
        {
            RecordsNoCachedPackage(f.Msi, AnotherWithNoCachedPackage);
            f.Msi.RecordsUrls(AnotherWithNoCachedPackage, isPatch: false, null, MsiInstallContext.Machine, SetupUrl);
            f = f with { Listed = [.. f.Listed, ListedPerMachine(AnotherWithNoCachedPackage)] };
        }
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SetupPackage, HeldFor);

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry, TheOwner)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[0]);
        Assert.Equal([new("D:", SourceRootGiveUpRoute.NoAnswer, heldOtherwiseToo ? 0 : 1)], GivenUp(screening));
    }

    [Fact]
    public void A_copy_declaring_the_code_of_an_installation_with_no_cached_package_is_let_through_where_its_sources_are_another_file()
    {
        // The candidate declares the installation's own code. Its record reads ordinary, so the
        // installation sets no hold, and the candidate is compared with what its sources name.
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        f.Packages.Declares(Candidate, NoCachedPackage);
        f.Msi.Installed(NoCachedPackage);
        f.Msi.AnswersItsOwnRecord(NoCachedPackage, null, MsiInstallContext.Machine);
        f.Files.Opens(SetupPackage, 9);

        var outcome = ScreenBesideIt(f).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.Contains(SetupPackage, f.Files.Reads);
    }

    [Fact]
    public void A_copy_declaring_the_code_of_an_installation_with_no_cached_package_is_kept_where_its_source_is_the_copy()
    {
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        f.Packages.Declares(Candidate, NoCachedPackage);
        f.Msi.Installed(NoCachedPackage);
        f.Msi.AnswersItsOwnRecord(NoCachedPackage, null, MsiInstallContext.Machine);
        f.Msi.RecordsSources(NoCachedPackage, null, MsiInstallContext.Machine, CandidateName, InstallerFolder + @"\");

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenBesideIt(f).Outcomes[0]);
    }

    [Fact]
    public void A_copy_declaring_the_code_of_an_installation_with_no_cached_package_is_kept_where_a_source_is_not_ruled_out()
    {
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        f.Packages.Declares(Candidate, NoCachedPackage);
        f.Msi.Installed(NoCachedPackage);
        f.Msi.AnswersItsOwnRecord(NoCachedPackage, null, MsiInstallContext.Machine);
        f.Msi.RecordsUrls(NoCachedPackage, isPatch: false, null, MsiInstallContext.Machine, SetupUrl);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenBesideIt(f).Outcomes[0]);
    }

    [Fact]
    public void A_possible_second_copy_with_no_cached_package_opens_what_its_sources_name()
    {
        // Its record reads ordinary, so it sets no hold, and it is listed as not ruled out as a
        // second copy, so its packages are read for every candidate.
        var f = ACopyBesideAnInstallationWithNoCachedPackage(marked: true);
        f.Msi.AnswersItsOwnRecord(NoCachedPackage, null, MsiInstallContext.Machine);

        var screening = ScreenBesideIt(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, screening.Outcomes[0]);
        Assert.Equal(Census(released: 1, unruled: 1), screening.CachedPackages);
    }

    [Fact]
    public void A_possible_second_copy_with_no_cached_package_keeps_the_copy_its_source_opens_as()
    {
        var f = ACopyBesideAnInstallationWithNoCachedPackage(marked: true);
        f.Msi.AnswersItsOwnRecord(NoCachedPackage, null, MsiInstallContext.Machine);
        f.Msi.RecordsSources(NoCachedPackage, null, MsiInstallContext.Machine, CandidateName, InstallerFolder + @"\");

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenBesideIt(f).Outcomes[0]);
    }

    [Fact]
    public void A_possible_second_copy_with_no_cached_package_and_a_source_not_ruled_out_keeps_every_installation_package()
    {
        var f = ACopyBesideAnInstallationWithNoCachedPackage(marked: true);
        f.Msi.AnswersItsOwnRecord(NoCachedPackage, null, MsiInstallContext.Machine);
        f.Msi.RecordsUrls(NoCachedPackage, isPatch: false, null, MsiInstallContext.Machine, SetupUrl);

        var screening = ScreenBesideIt(f);

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[0]);
        Assert.Equal(Census(released: 1, unruled: 1, unseenNoneRecorded: 1, unseenPerMachine: 1),
            screening.CachedPackages);
    }

    [Fact]
    public void An_installation_with_no_cached_package_read_by_both_steps_keeps_the_copy_its_source_opens_as()
    {
        // Its record does not show an ordinary installation and it is listed as not ruled out
        // as a second copy, so its sources are read for the hold and again as a second copy's.
        var f = ACopyBesideAnInstallationWithNoCachedPackage(marked: true);
        f.Msi.RecordsSources(NoCachedPackage, null, MsiInstallContext.Machine, CandidateName, InstallerFolder + @"\");

        var screening = ScreenBesideIt(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, screening.Outcomes[0]);
        Assert.Equal(Census(releasedBySources: 1, unruled: 1), screening.CachedPackages);
    }

    /// <summary>The InstallProperties key of the per-machine installation under <see cref="NoCachedPackage"/>.</summary>
    private const string NoCachedPackageProperties =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\"
        + @"99999999999999999999999999999999\InstallProperties";

    /// <summary>
    /// The InstallProperties key of the installation under <see cref="NoCachedPackage"/> per user and
    /// managed in <see cref="UserSid"/>'s account, which is not the account the check runs as.
    /// </summary>
    private const string NoCachedPackageUsersProperties =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\" + UserSid + @"\Products\"
        + @"99999999999999999999999999999999\InstallProperties";

    /// <summary>What an installation's InstallProperties key is scripted to show against an empty LocalPackage.</summary>
    public enum PropertiesFault
    {
        /// <summary>The key holds a cached package beside its InstallSource.</summary>
        HoldsACachedPackage,

        /// <summary>The key will not read.</summary>
        WillNotRead,
    }

    /// <summary>
    /// The InstallProperties key at <paramref name="key"/> shows <paramref name="fault"/>. A key
    /// holding a cached package holds the installation's InstallSource as the API answers it too, so
    /// the InstallSource agrees and the cached package is all that differs.
    /// </summary>
    private static void PropertiesShow(ScriptedSourceListRegistry registry, string key, PropertiesFault fault,
        string valueName = MsiInstallProperty.LocalPackage)
    {
        if (fault == PropertiesFault.WillNotRead)
        {
            registry.Answers(key, RegistryKeyPresence.Unreadable);
            return;
        }

        registry.Holds(key,
            new RegistryValue(MsiInstallProperty.InstallSource, RegistryValueKind.String, SetupFolder),
            new RegistryValue(valueName, RegistryValueKind.String, @"C:\Windows\Installer\gone.msi"));
    }

    [Theory]
    [InlineData("LocalPackage")]
    [InlineData("ManagedLocalPackage")]
    [InlineData("localpackage")]
    public void An_installation_with_no_cached_package_whose_key_holds_one_keeps_every_installation_package(
        string valueName)
    {
        // Windows Installer answers that it records no cached package and its InstallProperties
        // key holds one, so the two do not agree that it records none, and its sources are not
        // read.
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        PropertiesShow(f.Msi.Registry, NoCachedPackageProperties, PropertiesFault.HoldsACachedPackage, valueName);

        var screening = ScreenBesideIt(f);

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[0]);
        Assert.Equal(
            Census(noneRecorded: 1, packageCodeUnanswered: 1, perMachine: 1, keptRegistryDisagrees: 1),
            screening.CachedPackages);
        Assert.Empty(f.Msi.PackageNameReads);
    }

    [Fact]
    public void An_installation_with_no_cached_package_whose_key_will_not_read_keeps_every_installation_package()
    {
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        PropertiesShow(f.Msi.Registry, NoCachedPackageProperties, PropertiesFault.WillNotRead);
        var recorded = new List<string>();

        var screening = ScreenBesideIt(f, recordRefusal: (_, note) => recorded.Add(note));

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[0]);
        Assert.Equal(
            Census(noneRecorded: 1, packageCodeUnanswered: 1, perMachine: 1, keptRegistryDisagrees: 1),
            screening.CachedPackages);
        Assert.Equal(
            new[]
            {
                "the installation records no cached package, and the registry holds a cached package or a source "
                + "list where Windows Installer answers that it has none, or a key would not read",
            },
            recorded);
        Assert.Empty(f.Msi.PackageNameReads);
    }

    [Theory]
    [InlineData(PropertiesFault.HoldsACachedPackage)]
    [InlineData(PropertiesFault.WillNotRead)]
    public void A_copy_declaring_the_code_of_an_installation_with_no_cached_package_is_kept_where_its_key_does_not_agree(
        PropertiesFault fault)
    {
        // The must-hit pair of the copy let through where its sources are another file: the same
        // installation, with its InstallProperties key not showing that it records no cached package.
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        f.Packages.Declares(Candidate, NoCachedPackage);
        f.Msi.Installed(NoCachedPackage);
        f.Msi.AnswersItsOwnRecord(NoCachedPackage, null, MsiInstallContext.Machine);
        f.Files.Opens(SetupPackage, 9);
        PropertiesShow(f.Msi.Registry, NoCachedPackageProperties, fault);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenBesideIt(f).Outcomes[0]);
        Assert.Empty(f.Msi.PackageNameReads);
    }

    [Theory]
    [InlineData(PropertiesFault.HoldsACachedPackage)]
    [InlineData(PropertiesFault.WillNotRead)]
    public void A_possible_second_copy_with_no_cached_package_whose_key_does_not_agree_keeps_every_installation_package(
        PropertiesFault fault)
    {
        var f = ACopyBesideAnInstallationWithNoCachedPackage(marked: true);
        f.Msi.AnswersItsOwnRecord(NoCachedPackage, null, MsiInstallContext.Machine);
        PropertiesShow(f.Msi.Registry, NoCachedPackageProperties, fault);

        var screening = ScreenBesideIt(f);

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[0]);
        Assert.Equal(Census(released: 1, unruled: 1, unseenNoneRecorded: 1, unseenPerMachine: 1),
            screening.CachedPackages);
        Assert.Empty(f.Msi.PackageNameReads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Another_accounts_installation_with_no_cached_package_is_judged_by_its_sources_only_where_its_key_holds_none(
        bool keyHoldsOne)
    {
        // Per user and managed, in an account the check does not run as, so its own record is not
        // read. Its InstallProperties key is in that account's part of the machine's registry and
        // holds a ManagedLocalPackage where it records a cached package. Its sources name a package
        // that is no longer there.
        const MsiInstallContext Managed = MsiInstallContext.UserManaged;
        var f = ACopyBesideAnInstallationWithNoCachedPackage();
        f.Msi.PackageReadAnswers(NoCachedPackage, UserSid, Managed, MsiError.UnknownProperty);
        f.Msi.RecordsSources(NoCachedPackage, UserSid, Managed, SetupName, SetupFolder);
        f = f with { Listed = [new ListedInstallation(NoCachedPackage, UserSid, (int)Managed, false)] };
        if (keyHoldsOne)
            PropertiesShow(f.Msi.Registry, NoCachedPackageUsersProperties, PropertiesFault.HoldsACachedPackage,
                "ManagedLocalPackage");

        var screening = ScreenBesideIt(f);

        Assert.Equal(
            keyHoldsOne ? DeclaredProductOutcome.SecondCopyUnestablished : DeclaredProductOutcome.DeclaredProductNotInstalled,
            screening.Outcomes[0]);
        Assert.Equal(
            keyHoldsOne ? Census(noneRecorded: 1, anotherAccount: 1, keptRegistryDisagrees: 1) : Census(releasedBySources: 1),
            screening.CachedPackages);
    }

    // ---- An installation the caller could not rule out as a second copy ----
    //
    // The caller lists every installation with whether its InstanceType read as an
    // ordinary installation. One that did not read that way could be a second copy of
    // any program, installed from a package declaring no code the check can link it by,
    // so every candidate installation package is compared with every package it opens:
    // its cached package and the package in each folder its sources name. A candidate
    // that opens as one of them is kept, one shown to be another file from all of them
    // keeps the answer its own code gave, and where they cannot all be seen every
    // candidate that answer would let through is kept.

    private const string OtherCandidate = @"C:\Windows\Installer\b2.msi";

    /// <summary>
    /// The second copy's fixture listed as not ruled out as a second copy, in the context
    /// given. Its cached package declares the code it is registered under, so nothing
    /// links it to product A, and it was installed from <see cref="SetupPackage"/>, which
    /// is no longer there. <see cref="Candidate"/> declares product A and
    /// <see cref="OtherCandidate"/> product B, and neither product is installed. Each test
    /// changes one thing.
    /// </summary>
    private static (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk, ListedInstallation[] Listed)
        AMarkedSecondCopy(MsiInstallContext context = MsiInstallContext.Machine)
    {
        var f = ACopyBesideASecondCopy(context, marked: true);
        f.Packages.Declares(SecondCopysPackage, SecondCopy);
        f.Msi.RecordsSources(SecondCopy, f.Listed[0].UserSid, context, SetupName, SetupFolder);
        f.Files.Answers(SetupPackage, FileIdentityRead.NamesNothing);

        f.Packages.Declares(OtherCandidate, ProductB);
        f.Msi.NotInstalled(ProductB, MsiError.UnknownProduct);
        f.Files.Opens(OtherCandidate, 3);
        f.Disk.AddFile(OtherCandidate, new MockFileData(new byte[100]));
        return f;
    }

    [Theory]
    [InlineData(MsiInstallContext.Machine)]
    [InlineData(MsiInstallContext.UserManaged)]
    public void A_candidate_beside_a_second_copy_installed_from_elsewhere_is_let_through(MsiInstallContext context)
    {
        var f = AMarkedSecondCopy(context);

        var outcomes = ScreenBesideTheSecondCopy(f, [Package(Candidate), Package(OtherCandidate)]);

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductNotInstalled, DeclaredProductOutcome.DeclaredProductNotInstalled },
            outcomes);
        // The second copy's packages were read, by the code it is registered under, in
        // its own account and context, and both candidates were compared with them.
        Assert.Equal(new[] { (SecondCopy, f.Listed[0].UserSid, context) }, f.Msi.PackageNameReads);
        Assert.Contains(SetupPackage, f.Files.Reads);
        Assert.Contains(Candidate, f.Files.Reads);
        Assert.Contains(OtherCandidate, f.Files.Reads);
    }

    [Fact]
    public void A_second_copy_whose_cached_package_declares_the_code_it_was_installed_from_is_compared_all_the_same()
    {
        // The cached package declares product A and not the code the copy is registered
        // under. The candidate declaring product A is compared with the copy's packages
        // through that code as well, and neither candidate is the copy's package.
        var f = AMarkedSecondCopy();
        f.Packages.Declares(SecondCopysPackage, ProductA);

        var outcomes = ScreenBesideTheSecondCopy(f, [Package(Candidate), Package(OtherCandidate)]);

        Assert.Equal(
            new[]
            {
                DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile,
                DeclaredProductOutcome.DeclaredProductNotInstalled,
            },
            outcomes);
    }

    [Fact]
    public void A_candidate_that_opens_as_a_second_copys_cached_package_is_kept()
    {
        var f = AMarkedSecondCopy();
        f.Files.Opens(OtherCandidate, 2);

        var outcomes = ScreenBesideTheSecondCopy(f, [Package(Candidate), Package(OtherCandidate)]);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcomes[0]);
        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcomes[1]);
    }

    [Fact]
    public void A_candidate_that_opens_as_a_second_copys_source_package_is_kept()
    {
        var f = AMarkedSecondCopy();
        f.Files.Opens(SetupPackage, 3);

        var outcomes = ScreenBesideTheSecondCopy(f, [Package(Candidate), Package(OtherCandidate)]);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcomes[0]);
        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcomes[1]);
    }

    [Fact]
    public void A_second_copy_installed_from_the_Installer_folder_keeps_the_installation_package_it_opens_as()
    {
        // Installed from c.msi in the Installer folder, a third candidate. The other two are
        // other files and keep the answers their own codes gave.
        const string Named = @"C:\Windows\Installer\c.msi";
        var f = AMarkedSecondCopy();
        f.Msi.RecordsSources(SecondCopy, null, MsiInstallContext.Machine, "c.msi", InstallerFolder + @"\");
        f.Packages.Declares(Named, ProductA);
        f.Files.Opens(Named, 5);
        f.Disk.AddFile(Named, new MockFileData(new byte[100]));

        var outcomes = ScreenBesideTheSecondCopy(f, [Package(Candidate), Package(OtherCandidate), Package(Named)]);

        Assert.Equal(
            new[]
            {
                DeclaredProductOutcome.DeclaredProductNotInstalled,
                DeclaredProductOutcome.DeclaredProductNotInstalled,
                DeclaredProductOutcome.DeclaredProductInstalled,
            },
            outcomes);
    }

    [Fact]
    public void Every_installation_package_is_kept_beside_a_per_user_unmanaged_second_copy()
    {
        // Its source list is not read in that context, in any account.
        var f = AMarkedSecondCopy(MsiInstallContext.UserUnmanaged);

        var outcomes = ScreenBesideTheSecondCopy(f, [Package(Candidate), Package(OtherCandidate)]);

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome));
        Assert.Empty(f.Msi.PackageNameReads);
    }

    [Theory]
    [InlineData(CachedPackageFault.ReadFails)]
    [InlineData(CachedPackageFault.NotAFile)]
    [InlineData(CachedPackageFault.NoIdentity)]
    [InlineData(CachedPackageFault.APatch)]
    [InlineData(CachedPackageFault.NoCode)]
    [InlineData(CachedPackageFault.MalformedCode)]
    public void Every_installation_package_is_kept_beside_a_second_copy_whose_cached_package_does_not_say_what_it_declares(
        CachedPackageFault fault)
    {
        // The installation's own record answers as an ordinary installation, so its
        // cached package failing to read keeps nothing through the links. The caller
        // listed it as not ruled out as a second copy, and that is what keeps every file.
        // One recording no cached package opens what its sources name (the test after this).
        var f = AMarkedSecondCopy();
        Break(f, fault, null, MsiInstallContext.Machine);
        f.Msi.AnswersItsOwnRecord(SecondCopy, null, MsiInstallContext.Machine);

        var outcomes = ScreenBesideTheSecondCopy(f, [Package(Candidate), Package(OtherCandidate)]);

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome));
    }

    [Theory]
    [InlineData(MsiInstallContext.Machine)]
    [InlineData(MsiInstallContext.UserManaged)]
    public void A_second_copy_recording_no_cached_package_opens_what_its_sources_name(MsiInstallContext context)
    {
        // Its own record answers as an ordinary installation, so it sets no hold, and its
        // sources name a package that is no longer there, so every candidate goes on.
        var f = AMarkedSecondCopy(context);
        var sid = f.Listed[0].UserSid;
        Break(f, CachedPackageFault.NamesNoPackage, sid, context);
        f.Msi.AnswersItsOwnRecord(SecondCopy, sid, context);

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductNotInstalled, DeclaredProductOutcome.DeclaredProductNotInstalled },
            screening.Outcomes);
        Assert.Equal(Census(released: 1, unruled: 1), screening.CachedPackages);
        Assert.Contains(SetupPackage, f.Files.Reads);
    }

    [Fact]
    public void A_check_built_without_its_file_readers_keeps_every_installation_package_beside_a_second_copy()
    {
        // Such a check cannot see what the second copy opens. Its own record answers as
        // an ordinary installation, so the links keep nothing.
        var f = AMarkedSecondCopy();
        f.Msi.AnswersItsOwnRecord(SecondCopy, null, MsiInstallContext.Machine);

        var outcomes = ScriptedCheck(f.Msi, f.Packages)
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed).Outcomes;

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome));
        Assert.Empty(f.Files.Reads);
    }

    [Fact]
    public void A_candidate_whose_product_caches_another_file_is_kept_where_a_second_copy_opens_it()
    {
        // Product A is installed and caches another file, which alone lets the candidate
        // through. The second copy's cached package is the candidate.
        var f = AMarkedSecondCopy();
        AlsoInstallProductA(f);
        f.Files.Opens(Candidate, 2);

        var outcome = ScreenBesideTheSecondCopy(f)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
    }

    [Fact]
    public void A_candidate_whose_product_caches_another_file_is_let_through_beside_a_second_copy_opening_other_files()
    {
        // The must-miss half of the test above. The candidate is compared with its own
        // product's packages and the second copy's in one read of its identity.
        var f = AMarkedSecondCopy();
        AlsoInstallProductA(f);

        var outcome = ScreenBesideTheSecondCopy(f)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.Single(f.Files.Reads, read => read == Candidate);
        Assert.Contains((SecondCopy, (string?)null, MsiInstallContext.Machine), f.Msi.PackageNameReads);
    }

    /// <summary>
    /// Product A installed once per machine, caching <see cref="Recorded"/>, a file of its
    /// own declaring product A, and installed from <see cref="SetupPackage"/>.
    /// </summary>
    private static void AlsoInstallProductA(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk, ListedInstallation[] Listed) f)
    {
        f.Msi.Installed(ProductA);
        f.Msi.RecordsPackage(ProductA, null, MsiInstallContext.Machine, Recorded);
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName, SetupFolder);
        f.Packages.Declares(Recorded, ProductA);
        f.Files.Opens(Recorded, 5);
        f.Disk.AddFile(Recorded, new MockFileData(new byte[100]));
    }

    [Fact]
    public void The_second_of_two_second_copies_can_be_the_one_that_opens_the_candidate()
    {
        // Every such installation is read, and the first opening other files does not
        // settle the candidate.
        const string ThirdCopy = "{66666666-6666-6666-6666-666666666666}";
        const string ThirdCopysPackage = @"C:\Windows\Installer\copy3.msi";
        var f = AMarkedSecondCopy();
        f.Msi.RecordsPackage(ThirdCopy, null, MsiInstallContext.Machine, ThirdCopysPackage);
        f.Msi.RecordsSources(ThirdCopy, null, MsiInstallContext.Machine, SetupName, OtherFolder);
        f.Packages.Declares(ThirdCopysPackage, ThirdCopy);
        f.Files.Opens(ThirdCopysPackage, 6);
        f.Files.Opens(OtherPackage, 1);
        f.Disk.AddFile(ThirdCopysPackage, new MockFileData(new byte[100]));
        f = f with { Listed = [.. f.Listed, new ListedInstallation(ThirdCopy, null, (int)MsiInstallContext.Machine, true)] };

        var outcomes = ScreenBesideTheSecondCopy(f, [Package(Candidate), Package(OtherCandidate)]);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcomes[0]);
        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcomes[1]);
    }

    [Fact]
    public void A_candidate_its_own_product_keeps_stays_kept_for_that_where_a_second_copys_packages_cannot_be_seen()
    {
        // Product A is installed, records no cached package and its package name will not
        // read, so the candidate is kept on its own product's answer whatever the second copy
        // opens.
        var f = AMarkedSecondCopy();
        f.Msi.Installed(ProductA);
        f.Msi.RecordsPackage(ProductA, null, MsiInstallContext.Machine, string.Empty);
        f.Msi.Registry.Holds(ProductAProperties);
        f.Msi.PackageNameAnswers(ProductA, null, MsiInstallContext.Machine, MsiError.AccessDenied);
        f.Msi.RecordsSources(SecondCopy, null, MsiInstallContext.Machine, SetupName, OtherFolder);
        f.Files.Answers(OtherPackage, FileIdentityRead.OpenRefused);

        var outcome = ScreenBesideTheSecondCopy(f)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
    }

    [Fact]
    public void A_candidate_its_own_product_would_let_through_is_kept_where_a_second_copys_packages_cannot_be_seen()
    {
        // The other half of the test above: product A caches another file, which alone
        // lets the candidate through. The second copy's source package will not identify.
        var f = AMarkedSecondCopy();
        AlsoInstallProductA(f);
        f.Msi.RecordsSources(SecondCopy, null, MsiInstallContext.Machine, SetupName, OtherFolder);
        f.Files.Answers(OtherPackage, FileIdentityRead.OpenRefused);

        var outcome = ScreenBesideTheSecondCopy(f)[0];

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome);
    }

    [Fact]
    public void A_candidate_its_own_product_opens_is_kept_as_installed_where_a_second_copys_packages_cannot_be_seen()
    {
        // Product A's cached package is the candidate, which keeps it whatever the second
        // copy opens.
        var f = AMarkedSecondCopy();
        AlsoInstallProductA(f);
        f.Files.Opens(Recorded, 1);
        f.Msi.RecordsSources(SecondCopy, null, MsiInstallContext.Machine, SetupName, OtherFolder);
        f.Files.Answers(OtherPackage, FileIdentityRead.OpenRefused);

        var outcome = ScreenBesideTheSecondCopy(f)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
    }

    [Fact]
    public void A_second_copys_packages_are_read_once_for_every_candidate_in_the_pass()
    {
        const string ThirdCandidate = @"C:\Windows\Installer\a3.msi";
        var f = AMarkedSecondCopy();
        f.Packages.Declares(ThirdCandidate, ProductA);
        f.Files.Opens(ThirdCandidate, 7);
        f.Disk.AddFile(ThirdCandidate, new MockFileData(new byte[100]));

        var outcomes = ScreenBesideTheSecondCopy(f,
            [Package(Candidate), Package(OtherCandidate), Package(ThirdCandidate)]);

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcome));
        Assert.Equal(new[] { (SecondCopy, (string?)null, MsiInstallContext.Machine) }, f.Msi.PackageNameReads);
        Assert.Single(f.Files.Reads, read => read == SetupPackage);
    }

    // ---- What the pass counts of the second copies' packages ----
    //
    // Read only where no installation sets the hold through its cached package and record.
    // The read stops at the first installation whose packages cannot all be seen, and the
    // census says which step stopped it and whether that installation is per-machine.

    [Fact]
    public void A_second_copy_whose_packages_are_all_seen_is_counted_as_checked_and_nothing_else()
    {
        var f = AMarkedSecondCopy();

        var census = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed, default, null, InInstallerFolder)
            .CachedPackages;

        Assert.Equal(Census(unruled: 1), census);
    }

    [Theory]
    [InlineData(CachedPackageFault.ReadFails)]
    [InlineData(CachedPackageFault.NotAFile)]
    [InlineData(CachedPackageFault.NoIdentity)]
    [InlineData(CachedPackageFault.APatch)]
    [InlineData(CachedPackageFault.NoCode)]
    [InlineData(CachedPackageFault.MalformedCode)]
    public void A_second_copy_whose_cached_package_cannot_be_seen_is_counted_by_what_its_cached_package_gave(
        CachedPackageFault fault)
    {
        // Its own record shows an ordinary installation, so it does not set the hold through
        // the links, and the read of its packages stops at its cached package. One recording no
        // cached package is read on to its sources, and its count is under "An installation
        // with no cached package, judged by its sources".
        var f = AMarkedSecondCopy();
        Break(f, fault, null, MsiInstallContext.Machine);
        f.Msi.AnswersItsOwnRecord(SecondCopy, null, MsiInstallContext.Machine);

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed, default, null, InInstallerFolder);

        Assert.All(screening.Outcomes, outcome => Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome));
        Assert.Equal(
            fault switch
            {
                CachedPackageFault.ReadFails => Census(released: 1, unruled: 1, unseenPathUnreadable: 1, unseenPerMachine: 1),
                CachedPackageFault.NotAFile => Census(released: 1, unruled: 1, unseenNotThere: 1, unseenPerMachine: 1),
                CachedPackageFault.NoIdentity => Census(released: 1, unruled: 1, unseenWouldNotRead: 1, unseenPerMachine: 1),
                _ => Census(released: 1, unruled: 1, unseenNoProductCode: 1, unseenPerMachine: 1),
            },
            screening.CachedPackages);
    }

    [Fact]
    public void A_second_copy_is_counted_as_a_source_not_ruled_out_by_a_check_with_no_registry_reader()
    {
        // Such a check reads the cached package and cannot read the sources, so every
        // installation package is kept and the step that stopped it is counted.
        var f = AMarkedSecondCopy();

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, registry: null, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[0]);
        Assert.Equal(Census(unruled: 1, unseenSourceNotRuledOut: 1, unseenPerMachine: 1), screening.CachedPackages);
    }

    [Fact]
    public void A_second_copy_is_counted_as_a_source_not_ruled_out_by_a_screen_with_no_Installer_folder_test()
    {
        // The sources are not read without the test of whether a package could be a file in the
        // Installer folder, so every installation package is kept and the step that stopped it is
        // counted.
        var f = AMarkedSecondCopy();

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, namesAFileInInstallerFolder: null);

        Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, screening.Outcomes[0]);
        Assert.Equal(Census(unruled: 1, unseenSourceNotRuledOut: 1, unseenPerMachine: 1), screening.CachedPackages);
    }

    [Fact]
    public void A_second_copy_whose_cached_package_will_not_identify_is_counted_as_that()
    {
        // The links read what the cached package declares and not its volume and file ID, so
        // the cached package declares a code there, and the read of its packages stops at
        // the identity.
        var f = AMarkedSecondCopy();
        f.Files.Answers(SecondCopysPackage, FileIdentityRead.OpenRefused);

        var census = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder).CachedPackages;

        Assert.Equal(Census(unruled: 1, unseenWouldNotIdentify: 1, unseenPerMachine: 1), census);
    }

    [Fact]
    public void A_per_user_unmanaged_second_copy_is_counted_as_that_and_not_as_per_machine()
    {
        var f = AMarkedSecondCopy(MsiInstallContext.UserUnmanaged);

        var census = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, SomebodyElse)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder).CachedPackages;

        Assert.Equal(Census(unruled: 1, unseenPerUserUnmanaged: 1), census);
    }

    [Fact]
    public void A_per_user_managed_second_copy_whose_source_package_will_not_identify_is_not_counted_as_per_machine()
    {
        var f = AMarkedSecondCopy(MsiInstallContext.UserManaged);
        f.Msi.RecordsSources(SecondCopy, OtherUserSid, MsiInstallContext.UserManaged, SetupName, OtherFolder);
        f.Files.Answers(OtherPackage, FileIdentityRead.OpenRefused);

        var census = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, SomebodyElse)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder).CachedPackages;

        Assert.Equal(Census(unruled: 1, unseenSourceNotRuledOut: 1), census);
    }

    [Fact]
    public void A_second_copy_whose_source_package_will_not_identify_is_counted_as_a_source_not_ruled_out()
    {
        var f = AMarkedSecondCopy();
        f.Msi.RecordsSources(SecondCopy, null, MsiInstallContext.Machine, SetupName, OtherFolder);
        f.Files.Answers(OtherPackage, FileIdentityRead.OpenRefused);

        var census = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder).CachedPackages;

        Assert.Equal(Census(unruled: 1, unseenSourceNotRuledOut: 1, unseenPerMachine: 1), census);
    }

    [Fact]
    public void A_second_copy_whose_source_package_does_not_answer_is_counted_as_a_source_given_up()
    {
        var f = AMarkedSecondCopy();
        f.Files.Opens(SetupPackage, 9);
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SetupPackage, HeldFor);

        var census = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed, default, null, InInstallerFolder)
            .CachedPackages;

        Assert.Equal(Census(unruled: 1, unseenSourcesGivenUp: 1, unseenPerMachine: 1), census);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    public void The_read_stops_at_the_first_second_copy_whose_packages_cannot_be_seen(bool unseenFirst, int checkedCount)
    {
        // A second per-machine copy whose source package will not identify, listed after the
        // first copy, whose packages are all seen, or before it. The read stops at it either
        // way, so the first copy is checked only where it comes first.
        const string ThirdCopy = "{66666666-6666-6666-6666-666666666666}";
        const string ThirdCopysPackage = @"C:\Windows\Installer\copy3.msi";
        var f = AMarkedSecondCopy();
        f.Msi.RecordsPackage(ThirdCopy, null, MsiInstallContext.Machine, ThirdCopysPackage);
        f.Msi.RecordsSources(ThirdCopy, null, MsiInstallContext.Machine, SetupName, OtherFolder);
        f.Packages.Declares(ThirdCopysPackage, ThirdCopy);
        f.Files.Opens(ThirdCopysPackage, 6);
        f.Files.Answers(OtherPackage, FileIdentityRead.OpenRefused);
        f.Disk.AddFile(ThirdCopysPackage, new MockFileData(new byte[100]));
        var third = new ListedInstallation(ThirdCopy, null, (int)MsiInstallContext.Machine, true);
        f = f with { Listed = unseenFirst ? [third, .. f.Listed] : [.. f.Listed, third] };

        var census = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder).CachedPackages;

        Assert.Equal(Census(read: 2, unruled: checkedCount, unseenSourceNotRuledOut: 1, unseenPerMachine: 1), census);
    }

    [Fact]
    public void The_second_copies_are_counted_once_in_a_pass_of_many_candidates()
    {
        var f = AMarkedSecondCopy();
        f.Msi.RecordsSources(SecondCopy, null, MsiInstallContext.Machine, SetupName, OtherFolder);
        f.Files.Answers(OtherPackage, FileIdentityRead.OpenRefused);

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed, default, null, InInstallerFolder);

        Assert.All(screening.Outcomes, outcome => Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome));
        Assert.Equal(Census(unruled: 1, unseenSourceNotRuledOut: 1, unseenPerMachine: 1), screening.CachedPackages);
    }

    [Fact]
    public void No_second_copy_is_read_where_an_installation_sets_the_hold_through_its_cached_package_and_record()
    {
        // The per-machine copy's cached package path will not read and its PackageCode does
        // not answer, so the links hold every candidate and its packages are not read again.
        var f = AMarkedSecondCopy();
        Break(f, CachedPackageFault.ReadFails, null, MsiInstallContext.Machine);
        f.Msi.PackageCodeAnswers(SecondCopy, null, MsiInstallContext.Machine, MsiError.UnknownProperty);

        var census = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry, TheOwner)
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder).CachedPackages;

        Assert.Equal(Census(pathUnreadable: 1, packageCodeUnanswered: 1, perMachine: 1), census);
        Assert.Empty(f.Msi.PackageNameReads);
    }

    [Fact]
    public void A_file_kept_for_a_second_copys_package_on_the_network_is_counted_and_the_file_it_is_not_named_as_is_not()
    {
        var f = AMarkedSecondCopy();
        f.Msi.RecordsSources(SecondCopy, null, MsiInstallContext.Machine, CandidateName, ShareFolder);
        f.Files.Answers(SharePackage, FileIdentityRead.OpenRefused);

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            { DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(
            new[] { DeclaredProductOutcome.SecondCopyUnestablished, DeclaredProductOutcome.DeclaredProductNotInstalled },
            screening.Outcomes);
        Assert.Equal(Census(unruled: 1, unseenByName: 1), screening.CachedPackages);
    }

    // ---- A source package that does not answer in time ----
    //
    // A source folder can be on a server that does not answer, where an open waits until
    // Windows gives up on it. The check waits for each source package's read up to its
    // time limit, and a read that has not answered by then keeps the copy as a read that
    // failed does. A drive or a share where one read has not answered, or has answered
    // false only after a long wait, is not read again in the pass. Each test holds a read
    // back for longer than the limit, and the answer it gives once released is the one that
    // lets the copy through. A package on a share carries the name of the candidate it is
    // read for, a package of another name there not being read for it (the tests after
    // these).

    /// <summary>The time limit the tests below give the check.</summary>
    private static readonly TimeSpan ShortLimit = TimeSpan.FromMilliseconds(100);

    /// <summary>How long a held read is held for at most, far past <see cref="ShortLimit"/>.</summary>
    private static readonly TimeSpan HeldFor = TimeSpan.FromSeconds(5);

    private const string ShareFolder = @"\\nas\share\a\";
    private const string CandidateName = "a.msi";
    private const string SharePackage = @"\\nas\share\a\a.msi";

    [Fact]
    public void A_copy_is_kept_when_a_source_package_of_its_product_does_not_answer_within_the_time_limit()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, CandidateName, ShareFolder);
        f.Files.Opens(SharePackage, 9);
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SharePackage, HeldFor);

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var outcome = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.True(clock.Elapsed < HeldFor, $"the screen took {clock.Elapsed}");
        Assert.Contains(SharePackage, files.Started);
    }

    [Fact]
    public void A_copy_is_let_through_when_a_source_package_answers_within_the_time_limit()
    {
        // The read is held back, and for less than the limit.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, CandidateName, ShareFolder);
        f.Files.Opens(SharePackage, 9);
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SharePackage, TimeSpan.FromMilliseconds(200));

        var outcome = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = HeldFor, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.Contains(SharePackage, f.Files.Reads);
    }

    [Fact]
    public void A_copy_is_kept_when_whether_a_source_is_in_the_Installer_folder_does_not_answer_within_the_time_limit()
    {
        // The answer, once given, is that the source is outside the folder. The package's
        // identity is not read.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, CandidateName, ShareFolder);
        f.Files.Opens(SharePackage, 9);
        using var released = new ManualResetEventSlim();

        bool? HeldInInstallerFolder(string path)
        {
            released.Wait(HeldFor);
            return InInstallerFolder(path);
        }

        var outcome = new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate)], [], default, null, HeldInInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.DoesNotContain(SharePackage, f.Files.Reads);
        released.Set();
    }

    [Fact]
    public void Every_installation_package_is_kept_beside_a_second_copy_whose_source_package_does_not_answer_within_the_time_limit()
    {
        // The second copy's package is in a local folder, so it is read for every candidate.
        var f = AMarkedSecondCopy();
        f.Files.Opens(SetupPackage, 9);
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SetupPackage, HeldFor);

        var outcomes = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed, default, null, InInstallerFolder).Outcomes;

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome));
        Assert.Single(files.Started, read => read == SetupPackage);
    }

    [Fact]
    public void Cancelling_the_pass_ends_the_wait_for_a_source_package()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, CandidateName, ShareFolder);
        f.Files.Opens(SharePackage, 9);
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SharePackage, HeldFor);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
                { SourceFolderTimeLimit = TimeSpan.FromMinutes(1), DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
                .Screen([Package(Candidate)], [], cts.Token, null, InInstallerFolder).Outcomes);

        Assert.True(clock.Elapsed < HeldFor, $"the screen took {clock.Elapsed}");
    }

    /// <summary>
    /// Product A installed from <paramref name="heldFolder"/>, whose package is held past the
    /// limit, and product B installed from <paramref name="otherFolder"/>, whose package
    /// opens as another file. <see cref="Candidate"/> declares A and
    /// <see cref="OtherCandidate"/> B, each package carries the name of the candidate
    /// declaring its product, and B's candidate is let through only where B's source is
    /// read.
    /// </summary>
    private static ((ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk) F, HeldFileIdentities Held, string OtherPackage)
        TwoProductsBesideAHeldSource(string heldFolder, string otherFolder)
    {
        const string OtherRecorded = @"C:\Windows\Installer\b3.msi";
        const string OtherName = "b2.msi";
        var heldPackage = heldFolder + CandidateName;
        var otherPackage = otherFolder + OtherName;
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, CandidateName, heldFolder);
        f.Files.Opens(heldPackage, 9);
        f.Msi.Installed(ProductB);
        f.Msi.RecordsPackage(ProductB, null, MsiInstallContext.Machine, OtherRecorded);
        f.Msi.RecordsSources(ProductB, null, MsiInstallContext.Machine, OtherName, otherFolder);
        f.Packages.Declares(OtherCandidate, ProductB);
        f.Packages.Declares(OtherRecorded, ProductB);
        f.Files.Opens(OtherCandidate, 3);
        f.Files.Opens(OtherRecorded, 4);
        f.Files.Opens(otherPackage, 10);
        f.Disk.AddFile(OtherCandidate, new MockFileData(new byte[100]));
        f.Disk.AddFile(OtherRecorded, new MockFileData(new byte[100]));
        var held = new HeldFileIdentities(f.Files);
        held.Holds(heldPackage, HeldFor);
        return (f, held, otherPackage);
    }

    private static IReadOnlyList<DeclaredProductOutcome> ScreenBothCandidates(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk) f,
        HeldFileIdentities files,
        Func<string, DriveType> driveKind) =>
        new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = driveKind, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder).Outcomes;

    /// <summary>How long a read can take to answer false before its root is given up, in the tests of a slow answer.</summary>
    private static readonly TimeSpan SlowFailure = TimeSpan.FromMilliseconds(500);

    /// <summary>A read held for longer than <see cref="SlowFailure"/> and far less than <see cref="HeldFor"/>.</summary>
    private static readonly TimeSpan SlowAnswer = TimeSpan.FromSeconds(1);

    /// <summary>
    /// <see cref="ScreenBothCandidates"/> with the time limit at <see cref="HeldFor"/>, so a
    /// held read answers within it, and a read answering false after <see cref="SlowFailure"/>
    /// giving its root up.
    /// </summary>
    private static IReadOnlyList<DeclaredProductOutcome> ScreenBothCandidatesAnsweringSlowly(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk) f,
        HeldFileIdentities files) =>
        new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderTimeLimit = HeldFor,
                SourceFolderSlowFailure = SlowFailure,
                DriveKindOf = FixedDrive,
                NamesInFolderOf = NameOnly,
            }
            .Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder).Outcomes;

    [Theory]
    [InlineData(@"\\nas\share\b\", false)]
    [InlineData(@"\\NAS\SHARE\b\", false)]
    [InlineData(@"\\nas\other\", true)]
    public void A_source_package_under_a_share_that_has_not_answered_is_kept_for_the_rest_of_the_pass_without_being_read(
        string otherFolder, bool read)
    {
        // A share is on the network by its spelling, so no drive is asked about.
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(ShareFolder, otherFolder);
        using var _ = files;

        var outcomes = ScreenBothCandidates(f, files,
            drive => throw new InvalidOperationException($"the check asked about drive {drive}"));

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcomes[0]);
        Assert.Equal(
            read ? DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile : DeclaredProductOutcome.DeclaredProductInstalled,
            outcomes[1]);
        Assert.Equal(read, files.Calls.Contains(otherPackage));
    }

    [Theory]
    [InlineData(DriveType.Fixed)]
    [InlineData(DriveType.Removable)]
    [InlineData(DriveType.CDRom)]
    [InlineData(DriveType.Ram)]
    public void A_source_package_under_a_local_drive_that_has_not_answered_is_kept_for_the_rest_of_the_pass_without_being_read(
        DriveType driveKind)
    {
        // The second program's folder is on the same drive, its letter in lower case.
        var asked = new ConcurrentQueue<string>();
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"d:\Setup\b\");
        using var _ = files;

        var outcomes = ScreenBothCandidates(f, files, drive =>
        {
            asked.Enqueue(drive);
            return driveKind;
        });

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.DoesNotContain(otherPackage, files.Calls);
        Assert.Equal(new[] { "D:" }, asked);
    }

    [Fact]
    public void A_source_package_on_another_drive_is_read_as_usual_after_a_drive_has_not_answered()
    {
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"E:\Setup\b\");
        using var _ = files;

        var outcomes = ScreenBothCandidates(f, files, FixedDrive);

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductInstalled, DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile },
            outcomes);
        Assert.Contains(otherPackage, files.Calls);
    }

    [Fact]
    public void A_local_drive_kept_for_the_rest_of_one_pass_is_read_again_in_the_next()
    {
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"D:\Setup\b\");
        using var _ = files;
        var check = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly };

        check.Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder);
        Assert.DoesNotContain(otherPackage, files.Calls);

        var outcome = check.Screen([Package(OtherCandidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.Contains(otherPackage, files.Calls);
    }

    [Fact]
    public void A_drive_that_has_not_answered_for_a_second_copy_is_not_read_for_a_program_later_in_the_pass()
    {
        // The second copy was installed from D:\Setup\, where its package is held, and
        // product C from D:\Setup\c\, where its package opens as a file other than its copy
        // and its cached package. The second copy's package is read first, for the candidate
        // declaring product B, which is not installed.
        const string ProductC = "{44444444-4444-4444-4444-444444444444}";
        const string CopyOfC = @"C:\Windows\Installer\c3.msi";
        const string CachedC = @"C:\Windows\Installer\c4.msi";
        const string PackageOfC = @"D:\Setup\c\c2.msi";
        var f = AMarkedSecondCopy();
        f.Msi.Installed(ProductC);
        f.Msi.RecordsPackage(ProductC, null, MsiInstallContext.Machine, CachedC);
        f.Msi.RecordsSources(ProductC, null, MsiInstallContext.Machine, "c2.msi", @"D:\Setup\c\");
        f.Packages.Declares(CopyOfC, ProductC);
        f.Packages.Declares(CachedC, ProductC);
        f.Files.Opens(CopyOfC, 11);
        f.Files.Opens(CachedC, 12);
        f.Files.Opens(PackageOfC, 13);
        f.Disk.AddFile(CopyOfC, new MockFileData(new byte[100]));
        f.Disk.AddFile(CachedC, new MockFileData(new byte[100]));
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SetupPackage, HeldFor);

        var outcomes = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(OtherCandidate), Package(CopyOfC)], f.Listed, default, null, InInstallerFolder).Outcomes;

        Assert.Equal(
            new[] { DeclaredProductOutcome.SecondCopyUnestablished, DeclaredProductOutcome.DeclaredProductInstalled },
            outcomes);
        Assert.Single(files.Started, read => read == SetupPackage);
        Assert.DoesNotContain(PackageOfC, files.Calls);
    }

    [Theory]
    [InlineData(@"D:\Setup\a\", @"D:\Setup\b\")]
    [InlineData(@"\\nas\share\a\", @"\\nas\share\b\")]
    public void A_root_whose_source_package_is_refused_only_after_a_long_wait_is_kept_for_the_rest_of_the_pass_without_being_read(
        string heldFolder, string otherFolder)
    {
        // The held package answers, within the time limit, that it will not open.
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(heldFolder, otherFolder);
        using var _ = files;
        f.Files.Answers(heldFolder + CandidateName, FileIdentityRead.OpenRefused);
        files.Holds(heldFolder + CandidateName, SlowAnswer);

        var outcomes = ScreenBothCandidatesAnsweringSlowly(f, files);

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.DoesNotContain(otherPackage, files.Calls);
    }

    [Theory]
    [InlineData(@"D:\Setup\a\", @"D:\Setup\b\")]
    [InlineData(@"\\nas\share\a\", @"\\nas\share\b\")]
    public void A_root_whose_source_package_is_refused_at_once_still_has_its_next_source_package_read(
        string heldFolder, string otherFolder)
    {
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(heldFolder, otherFolder);
        using var _ = files;
        f.Files.Answers(heldFolder + CandidateName, FileIdentityRead.OpenRefused);
        files.Holds(heldFolder + CandidateName, TimeSpan.Zero);

        var outcomes = ScreenBothCandidatesAnsweringSlowly(f, files);

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductInstalled, DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile },
            outcomes);
        Assert.Contains(otherPackage, files.Calls);
    }

    [Theory]
    [InlineData(@"D:\Setup\a\", @"D:\Setup\b\")]
    [InlineData(@"\\nas\share\a\", @"\\nas\share\b\")]
    public void A_root_whose_source_package_opens_after_a_long_wait_still_has_its_next_source_package_read(
        string heldFolder, string otherFolder)
    {
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(heldFolder, otherFolder);
        using var _ = files;
        files.Holds(heldFolder + CandidateName, SlowAnswer);

        var outcomes = ScreenBothCandidatesAnsweringSlowly(f, files);

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome));
        Assert.Contains(otherPackage, files.Calls);
    }

    // ---- A root that answers slowly ----
    //
    // A read that opens its package within the time limit leaves its root to be read until
    // the reads under it add up past the check's budget for one root. So every package under
    // a root slow to answer each read is read, one after another, until then, and each wait
    // is told to the caller.

    /// <summary>How long a read can take before the check tells its caller it is waiting, in the tests below.</summary>
    private static readonly TimeSpan WaitThreshold = TimeSpan.FromMilliseconds(200);

    /// <summary>A read held for longer than <see cref="WaitThreshold"/> and far less than <see cref="HeldFor"/>.</summary>
    private static readonly TimeSpan SlowRead = TimeSpan.FromMilliseconds(600);

    /// <summary>
    /// How long a source package read takes on the check's clock in the tests below: longer than
    /// the check's own slow-failure bar, and long enough that six reads come to more than its
    /// budget for reads that fail. Neither applies to a read that opens. Twelve reads come to its
    /// budget for every read under one root, which is not past it. The time limit is waited out
    /// in real time by the thread that waits, so this clock does not bring a read any nearer to
    /// it.
    /// </summary>
    private static readonly TimeSpan ReadOnTheClock = TimeSpan.FromSeconds(25);

    /// <summary>
    /// <paramref name="count"/> programs, each installed from a folder of its own under
    /// <paramref name="folder"/> (<see cref="ProgramsInstalledFromFoldersUnder(IReadOnlyList{string})"/>).
    /// </summary>
    private static ((ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk) F, string[] Copies, string[] SourcePackages)
        ProgramsInstalledFromFoldersUnder(string folder, int count) =>
        ProgramsInstalledFromFoldersUnder([.. Enumerable.Repeat(folder, count)]);

    /// <summary>
    /// One program for each of <paramref name="folders"/>, installed once per machine from a
    /// folder of its own under that one, and each with a copy in the Installer folder beside
    /// its cached package. A program's package carries the name of its copy and opens as a
    /// third file, so the copy is let through only where that package is read. Returns the
    /// copies and the packages, in the order of the programs.
    /// </summary>
    private static ((ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk) F, string[] Copies, string[] SourcePackages)
        ProgramsInstalledFromFoldersUnder(IReadOnlyList<string> folders)
    {
        var count = folders.Count;
        var packages = new ScriptedPackageIdentities();
        var msi = new ScriptedMsiProducts();
        var files = new ScriptedFileIdentities();
        var disk = new MockFileSystem();
        var copies = new string[count];
        var sourcePackages = new string[count];

        for (var n = 0; n < count; n++)
        {
            var code = $"{{{n + 1:D8}-AAAA-AAAA-AAAA-AAAAAAAAAAAA}}";
            var name = $"p{n}.msi";
            var copy = $@"C:\Windows\Installer\{name}";
            var cached = $@"C:\Windows\Installer\cached{n}.msi";
            var programFolder = $@"{folders[n]}p{n}\";
            copies[n] = copy;
            sourcePackages[n] = programFolder + name;

            msi.Installed(code);
            msi.RecordsPackage(code, null, MsiInstallContext.Machine, cached);
            msi.RecordsSources(code, null, MsiInstallContext.Machine, name, programFolder);
            packages.Declares(copy, code);
            packages.Declares(cached, code);
            files.Opens(copy, (ulong)(100 + 3 * n));
            files.Opens(cached, (ulong)(101 + 3 * n));
            files.Opens(sourcePackages[n], (ulong)(102 + 3 * n));
            disk.AddFile(copy, new MockFileData(new byte[100]));
            disk.AddFile(cached, new MockFileData(new byte[100]));
        }

        return ((packages, msi, files, disk), copies, sourcePackages);
    }

    [Theory]
    [InlineData(@"D:\Setup\", "D:")]
    [InlineData(@"\\nas\share\", @"\\nas\share")]
    public void Every_source_package_under_a_root_slow_to_answer_each_read_is_read_within_its_budget_and_each_wait_is_told_and_counted(
        string folder, string root)
    {
        // Each read is held for SlowRead, so it is a wait the caller is told of, and takes
        // ReadOnTheClock on the clock the check times reads by, so the six come to two and a
        // half minutes, within the check's budget for one root. The time limit is the check's
        // own.
        const int Programs = 6;
        var (f, copies, sourcePackages) = ProgramsInstalledFromFoldersUnder(folder, Programs);
        using var files = new HeldFileIdentities(f.Files);
        foreach (var package in sourcePackages) files.Holds(package, SlowRead);
        var clock = new SteppedClock();
        files.TakesOnTheClock(clock, ReadOnTheClock);
        var told = new List<string?>();

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderWaitThreshold = WaitThreshold,
                Clock = clock,
                DriveKindOf = FixedDrive,
                NamesInFolderOf = NameOnly,
            }
            .Screen([.. copies.Select(Package)], [], default, null, InInstallerFolder, waitingOn: wait => told.Add(wait?.Root));

        Assert.All(screening.Outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome));
        Assert.Equal(sourcePackages, files.Started);
        Assert.Equal(Enumerable.Repeat(new[] { root, null }, Programs).SelectMany(wait => wait), told);
        Assert.Equal(Programs, screening.WaitCount);
    }

    // ---- A root whose reads fail ----
    //
    // A package read that answers false adds the time it took to its root's total of reads
    // that fail, however short, and once that passes the check's budget for them no later
    // package under the root is read in the pass. A read that finds no file there adds nothing
    // to it. Every read here answers at once and takes the time the test gives it on the
    // check's clock alone, so the tests run on the check's own slow-failure bar and budgets.

    /// <summary>
    /// How long a source package read that does not open takes on the check's clock in the
    /// tests below: shorter than the check's own slow-failure bar.
    /// </summary>
    private static readonly TimeSpan UnopenedReadOnTheClock = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The programs <see cref="ProgramsInstalledFromFoldersUnder(IReadOnlyList{string})"/>
    /// builds for <paramref name="folders"/>, every one of their packages refusing at once to
    /// open, and each read of one moving the returned clock on by <paramref name="each"/>.
    /// </summary>
    private static ((ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk) F, HeldFileIdentities Files, SteppedClock Clock,
        string[] Copies, string[] SourcePackages)
        ProgramsWhosePackagesAreRefused(IReadOnlyList<string> folders, TimeSpan each) =>
        ProgramsWhosePackagesAnswer(folders, FileIdentityRead.OpenRefused, each);

    /// <summary>
    /// <see cref="ProgramsWhosePackagesOpen"/>, every one of the packages answering
    /// <paramref name="answer"/> instead.
    /// </summary>
    private static ((ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk) F, HeldFileIdentities Files, SteppedClock Clock,
        string[] Copies, string[] SourcePackages)
        ProgramsWhosePackagesAnswer(IReadOnlyList<string> folders, FileIdentityRead answer, TimeSpan each)
    {
        var programs = ProgramsWhosePackagesOpen(folders, each);
        foreach (var package in programs.SourcePackages) programs.F.Files.Answers(package, answer);
        return programs;
    }

    /// <summary>
    /// The programs <see cref="ProgramsInstalledFromFoldersUnder(IReadOnlyList{string})"/>
    /// builds for <paramref name="folders"/>, every one of their packages opening at once, and
    /// each read of one moving the returned clock on by <paramref name="each"/>.
    /// </summary>
    private static ((ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk) F, HeldFileIdentities Files, SteppedClock Clock,
        string[] Copies, string[] SourcePackages)
        ProgramsWhosePackagesOpen(IReadOnlyList<string> folders, TimeSpan each)
    {
        var (f, copies, sourcePackages) = ProgramsInstalledFromFoldersUnder(folders);
        var files = new HeldFileIdentities(f.Files);
        foreach (var package in sourcePackages) files.Holds(package, TimeSpan.Zero);

        var clock = new SteppedClock();
        files.TakesOnTheClock(clock, each);
        return (f, files, clock, copies, sourcePackages);
    }

    /// <summary>
    /// The check timing its reads on <paramref name="clock"/>, with its own time limit,
    /// threshold, slow-failure bar and budgets.
    /// </summary>
    private static DeclaredProductCheck CheckOnTheClock(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk) f,
        HeldFileIdentities files,
        SteppedClock clock) =>
        new(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { Clock = clock, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly };

    private static IReadOnlyList<DeclaredProductOutcome> ScreenEvery(DeclaredProductCheck check, IEnumerable<string> copies) =>
        check.Screen([.. copies.Select(Package)], [], default, null, InInstallerFolder).Outcomes;

    [Theory]
    [InlineData(@"D:\Setup\")]
    [InlineData(@"\\nas\share\")]
    public void A_root_whose_refused_reads_add_up_past_a_minute_is_kept_for_the_rest_of_the_pass_without_being_read(
        string folder)
    {
        // Twenty reads of three seconds come to a minute, which is not past it. The
        // twenty-first takes the root past, and no package under it is read after that.
        const int Programs = 24, Reads = 21;
        var (f, files, clock, copies, sourcePackages) =
            ProgramsWhosePackagesAreRefused([.. Enumerable.Repeat(folder, Programs)], UnopenedReadOnTheClock);
        using var _ = files;

        var outcomes = ScreenEvery(CheckOnTheClock(f, files, clock), copies);

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.Equal(sourcePackages[..Reads], files.Started);
    }

    [Theory]
    [InlineData(@"D:\Setup\")]
    [InlineData(@"\\nas\share\")]
    public void Refused_reads_too_quick_to_be_waits_add_up_the_same_way(string folder)
    {
        // Each read takes half a second on the check's clock, less than its threshold for a
        // wait. A hundred and twenty come to a minute, which is not past it, and the hundred
        // and twenty-first takes the root past.
        const int Programs = 130, Reads = 121;
        var (f, files, clock, copies, sourcePackages) =
            ProgramsWhosePackagesAreRefused([.. Enumerable.Repeat(folder, Programs)], TimeSpan.FromMilliseconds(500));
        using var _ = files;

        var outcomes = ScreenEvery(CheckOnTheClock(f, files, clock), copies);

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.Equal(sourcePackages[..Reads], files.Started);
    }

    [Theory]
    [InlineData(@"D:\Setup\")]
    [InlineData(@"\\nas\share\")]
    public void Reads_that_find_no_file_do_not_count_towards_the_budget_for_reads_that_fail(string folder)
    {
        // Each read finds no package and takes three seconds, and the twenty-four come to
        // seventy-two seconds, past the budget for reads that fail and within the one for every
        // read.
        const int Programs = 24;
        var (f, files, clock, copies, sourcePackages) = ProgramsWhosePackagesAnswer(
            [.. Enumerable.Repeat(folder, Programs)], FileIdentityRead.NamesNothing, UnopenedReadOnTheClock);
        using var _ = files;

        var outcomes = ScreenEvery(CheckOnTheClock(f, files, clock), copies);

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome));
        Assert.Equal(sourcePackages, files.Started);
    }

    [Fact]
    public void Two_roots_add_up_their_refused_reads_apart()
    {
        // D: is given up at its twenty-first read. E:'s twenty reads come to a minute, which
        // is not past it, and the total for D: and E: together is.
        string[] folders = [.. Enumerable.Repeat(@"D:\Setup\", 24), .. Enumerable.Repeat(@"E:\Setup\", 20)];
        var (f, files, clock, copies, sourcePackages) = ProgramsWhosePackagesAreRefused(folders, UnopenedReadOnTheClock);
        using var _ = files;

        ScreenEvery(CheckOnTheClock(f, files, clock), copies);

        Assert.Equal(sourcePackages[..21].Concat(sourcePackages[24..]), files.Started);
    }

    [Fact]
    public void A_root_given_up_for_its_refused_reads_in_one_pass_is_read_again_in_the_next()
    {
        var (f, files, clock, copies, sourcePackages) =
            ProgramsWhosePackagesAreRefused([.. Enumerable.Repeat(@"D:\Setup\", 24)], UnopenedReadOnTheClock);
        using var _ = files;
        var check = CheckOnTheClock(f, files, clock);

        ScreenEvery(check, copies);
        Assert.Equal(sourcePackages[..21], files.Started);

        ScreenEvery(check, copies[21..]);

        Assert.Equal(sourcePackages, files.Started);
    }

    // ---- A root whose reads add up ----
    //
    // Every read under a root adds the time it took to a second total, whatever it answered
    // and however short, the read asking a drive its kind among them, and once that passes the
    // check's budget for one root no later package under the root is read in the pass. The
    // read that takes the total past still has its answer used. Every read here answers at
    // once and takes the time the test gives it on the check's clock alone, so the tests run on
    // the check's own budgets.

    [Theory]
    [InlineData(@"D:\Setup\")]
    [InlineData(@"\\nas\share\")]
    public void A_root_whose_reads_add_up_past_five_minutes_is_kept_for_the_rest_of_the_pass_without_being_read(
        string folder)
    {
        // Every package opens. Twelve reads of twenty-five seconds come to five minutes, which
        // is not past it. The thirteenth takes the root past and its copy is still let through,
        // and no package under the root is read after it.
        const int Programs = 16, Reads = 13;
        var (f, files, clock, copies, sourcePackages) =
            ProgramsWhosePackagesOpen([.. Enumerable.Repeat(folder, Programs)], ReadOnTheClock);
        using var _ = files;

        var outcomes = ScreenEvery(CheckOnTheClock(f, files, clock), copies);

        Assert.Equal(sourcePackages[..Reads], files.Started);
        Assert.All(outcomes.Take(Reads),
            outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome));
        Assert.All(outcomes.Skip(Reads), outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
    }

    [Theory]
    [InlineData(@"D:\Setup\")]
    [InlineData(@"\\nas\share\")]
    public void Reads_too_quick_to_be_waits_add_up_towards_five_minutes_the_same_way(string folder)
    {
        // Each read opens its package and takes half a second on the check's clock, less than
        // its threshold for a wait. Six hundred come to five minutes, which is not past it, and
        // the six hundred and first takes the root past.
        const int Programs = 610, Reads = 601;
        var (f, files, clock, copies, sourcePackages) =
            ProgramsWhosePackagesOpen([.. Enumerable.Repeat(folder, Programs)], TimeSpan.FromMilliseconds(500));
        using var _ = files;

        var outcomes = ScreenEvery(CheckOnTheClock(f, files, clock), copies);

        Assert.Equal(sourcePackages[..Reads], files.Started);
        Assert.All(outcomes.Skip(Reads), outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
    }

    [Theory]
    [InlineData(@"D:\Setup\")]
    [InlineData(@"\\nas\share\")]
    public void Reads_that_fail_or_find_no_file_add_up_towards_five_minutes_too(string folder)
    {
        // Every read takes four seconds. The first fourteen packages refuse to open, fifty-six
        // seconds in all, which is within the budget for reads that fail, and the rest are not
        // there. Seventy-five reads come to five minutes, which is not past it, and the
        // seventy-sixth takes the root past.
        const int Programs = 100, Refused = 14, Reads = 76;
        var (f, files, clock, copies, sourcePackages) = ProgramsWhosePackagesAnswer(
            [.. Enumerable.Repeat(folder, Programs)], FileIdentityRead.NamesNothing, TimeSpan.FromSeconds(4));
        using var _ = files;
        foreach (var package in sourcePackages[..Refused]) f.Files.Answers(package, FileIdentityRead.OpenRefused);

        var outcomes = ScreenEvery(CheckOnTheClock(f, files, clock), copies);

        Assert.Equal(sourcePackages[..Reads], files.Started);
        Assert.All(outcomes.Take(Refused), outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.All(outcomes.Take(Reads).Skip(Refused),
            outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome));
        Assert.All(outcomes.Skip(Reads), outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
    }

    [Fact]
    public void The_read_asking_a_drive_its_kind_counts_towards_five_minutes_with_the_package_reads()
    {
        // The drive takes twenty-five seconds on the check's clock to say what kind it is, as
        // each package read does. With eleven package reads that comes to five minutes, and the
        // twelfth takes the drive past.
        const int Programs = 16, Reads = 12;
        var (f, files, clock, copies, sourcePackages) =
            ProgramsWhosePackagesOpen([.. Enumerable.Repeat(@"D:\Setup\", Programs)], ReadOnTheClock);
        using var _ = files;
        var check = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                Clock = clock,
                DriveKindOf = _ =>
                {
                    clock.Advance(ReadOnTheClock);
                    return DriveType.Fixed;
                },
                NamesInFolderOf = NameOnly,
            };

        ScreenEvery(check, copies);

        Assert.Equal(sourcePackages[..Reads], files.Started);
    }

    [Fact]
    public void Two_roots_add_up_their_reads_apart()
    {
        // D: is given up at its thirteenth read. E:'s twelve reads come to five minutes, which
        // is not past it, and the total for D: and E: together is.
        string[] folders = [.. Enumerable.Repeat(@"D:\Setup\", 16), .. Enumerable.Repeat(@"E:\Setup\", 12)];
        var (f, files, clock, copies, sourcePackages) = ProgramsWhosePackagesOpen(folders, ReadOnTheClock);
        using var _ = files;

        ScreenEvery(CheckOnTheClock(f, files, clock), copies);

        Assert.Equal(sourcePackages[..13].Concat(sourcePackages[16..]), files.Started);
    }

    [Fact]
    public void A_root_given_up_for_the_time_its_reads_took_in_one_pass_is_read_again_in_the_next()
    {
        var (f, files, clock, copies, sourcePackages) =
            ProgramsWhosePackagesOpen([.. Enumerable.Repeat(@"D:\Setup\", 16)], ReadOnTheClock);
        using var _ = files;
        var check = CheckOnTheClock(f, files, clock);

        ScreenEvery(check, copies);
        Assert.Equal(sourcePackages[..13], files.Started);

        var outcomes = ScreenEvery(check, copies[13..]);

        Assert.Equal(sourcePackages, files.Started);
        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome));
    }

    // ---- What the check tells its caller while a read waits ----
    //
    // A read still waiting once it has taken longer than the check's threshold is told to
    // the caller with the root it is under, and null is told when the wait ends, whether
    // the read answered or the root was given up. A read that answers sooner is told
    // nothing. The caller is told on the thread the pass runs on, and the pass counts each
    // wait it tells (DeclaredProductScreening.WaitCount).

    /// <summary>
    /// The check over two programs, A's package held as the test says, with its threshold at
    /// <see cref="WaitThreshold"/> and its time limit at <paramref name="limit"/>, telling
    /// <paramref name="told"/> each wait.
    /// </summary>
    private static DeclaredProductScreening ScreenTellingWaits(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk) f,
        HeldFileIdentities files,
        TimeSpan limit,
        List<string?> told,
        CancellationToken cancellationToken = default) =>
        new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderTimeLimit = limit,
                SourceFolderWaitThreshold = WaitThreshold,
                DriveKindOf = FixedDrive,
                NamesInFolderOf = NameOnly,
            }
            .Screen([Package(Candidate), Package(OtherCandidate)], [], cancellationToken, null, InInstallerFolder,
                waitingOn: wait => told.Add(wait?.Root));

    [Theory]
    [InlineData(@"D:\Setup\a\", @"D:\Setup\b\", "D:")]
    [InlineData(@"\\nas\share\a\", @"\\nas\share\b\", @"\\nas\share")]
    public void A_read_that_waits_past_the_threshold_is_told_with_its_root_and_then_its_end_and_counted(
        string heldFolder, string otherFolder, string root)
    {
        var (f, files, _) = TwoProductsBesideAHeldSource(heldFolder, otherFolder);
        using var held = files;
        files.Holds(heldFolder + CandidateName, SlowRead);
        var told = new List<string?>();

        var screening = ScreenTellingWaits(f, files, HeldFor, told);

        Assert.All(screening.Outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome));
        Assert.Equal(new[] { root, null }, told);
        Assert.Equal(1, screening.WaitCount);
    }

    [Fact]
    public void A_read_that_answers_within_the_threshold_is_not_told_or_counted()
    {
        var (f, files, _) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"D:\Setup\b\");
        using var held = files;
        files.Holds(@"D:\Setup\a\" + CandidateName, TimeSpan.Zero);
        var told = new List<string?>();

        var screening = ScreenTellingWaits(f, files, HeldFor, told);

        Assert.Empty(told);
        Assert.Equal(0, screening.WaitCount);
    }

    [Fact]
    public void A_read_that_does_not_answer_is_told_and_its_end_is_told_when_its_drive_is_given_up()
    {
        // The limit is past the threshold and well short of the hold. The second program's
        // package is under the drive given up, so it is not read and nothing more is told.
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"D:\Setup\b\");
        using var held = files;
        var told = new List<string?>();

        var screening = ScreenTellingWaits(f, files, SlowRead, told);

        Assert.All(screening.Outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.Equal(new[] { "D:", null }, told);
        Assert.Equal(1, screening.WaitCount);
        Assert.DoesNotContain(otherPackage, files.Calls);
    }

    [Fact]
    public void A_wait_that_cancelling_ends_is_told_and_its_end_is_not()
    {
        var (f, files, _) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"D:\Setup\b\");
        using var held = files;
        var told = new List<string?>();
        using var cts = new CancellationTokenSource(SlowRead);

        Assert.ThrowsAny<OperationCanceledException>(
            () => ScreenTellingWaits(f, files, TimeSpan.FromMinutes(1), told, cts.Token));

        Assert.Equal(new[] { "D:" }, told);
    }

    [Fact]
    public void A_wait_for_a_drive_to_say_what_kind_it_is_is_told_like_a_read_of_a_package()
    {
        var (f, files, _) = TwoProductsBesideAHeldSource(@"Z:\Setup\a\", @"Z:\Setup\b\");
        using var held = files;
        files.Holds(@"Z:\Setup\a\" + CandidateName, TimeSpan.Zero);
        var told = new List<string?>();

        new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderTimeLimit = HeldFor,
                SourceFolderWaitThreshold = WaitThreshold,
                DriveKindOf = _ =>
                {
                    Thread.Sleep(SlowRead);
                    return DriveType.Fixed;
                },
                NamesInFolderOf = NameOnly,
            }
            .Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder,
                waitingOn: wait => told.Add(wait?.Root));

        Assert.Equal(new[] { "Z:", null }, told);
    }

    [Fact]
    public void A_drive_Windows_reports_as_a_network_drive_is_kept_for_the_rest_of_the_pass_like_a_share()
    {
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(@"Z:\Setup\a\", @"z:\Setup\b\");
        using var _ = files;

        var outcomes = ScreenBothCandidates(f, files, _ => DriveType.Network);

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.DoesNotContain(otherPackage, files.Calls);
    }

    [Fact]
    public void A_drive_whose_kind_does_not_answer_within_the_time_limit_is_kept_for_the_rest_of_the_pass_without_being_read()
    {
        using var released = new ManualResetEventSlim();
        var asked = new ConcurrentQueue<string>();
        var (f, files, _) = TwoProductsBesideAHeldSource(@"Z:\Setup\a\", @"Z:\Setup\b\");
        using var held = files;

        var outcomes = ScreenBothCandidates(f, files, drive =>
        {
            asked.Enqueue(drive);
            released.Wait(HeldFor);
            return DriveType.Fixed;
        });

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.DoesNotContain(files.Calls, read => read.StartsWith(@"Z:\", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(new[] { "Z:" }, asked);
        released.Set();
    }

    [Fact]
    public void A_share_that_has_not_answered_is_read_again_in_the_next_pass()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, CandidateName, ShareFolder);
        f.Files.Opens(SharePackage, 9);
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SharePackage, HeldFor);
        var check = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly };

        check.Screen([Package(Candidate)], [], default, null, InInstallerFolder);
        check.Screen([Package(Candidate)], [], default, null, InInstallerFolder);

        Assert.Equal(2, files.Started.Count(read => read == SharePackage));
    }

    // ---- Stopping a wait ----
    //
    // The caller can stop waiting for the drive or share of a wait it was told of. A wait
    // there in progress ends at once. The root is then given up for the rest of the pass, as
    // it is where a read has not answered within the time limit: what depended on the read is
    // kept, and no later read under the root is started in the pass. A read that has answered
    // keeps its answer. Each pass has its own stops, and a stop does not change what cancelling
    // does. The root is listed with the others the pass gave up, as stopped, in the spelling of
    // the wait the caller stopped, and every file kept where its check stopped at a read the
    // stop refused or ended before it answered counts towards it. The time limit here is a
    // minute and each read is held for at most HeldFor, so a pass ending sooner than that has
    // had its wait stopped.

    /// <summary>
    /// The check over two programs, A's package held as the test says, with its threshold at
    /// <see cref="WaitThreshold"/> and its time limit at a minute, telling
    /// <paramref name="waitingOn"/> each wait and <paramref name="candidateReached"/> each
    /// candidate reached.
    /// </summary>
    private static DeclaredProductScreening ScreenStoppingWaits(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk) f,
        HeldFileIdentities files,
        Action<SourceFolderWait?> waitingOn,
        Action<int>? candidateReached = null) =>
        new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderTimeLimit = TimeSpan.FromMinutes(1),
                SourceFolderWaitThreshold = WaitThreshold,
                DriveKindOf = FixedDrive,
                NamesInFolderOf = NameOnly,
            }
            .Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder,
                candidateReached, waitingOn);

    [Theory]
    [InlineData(@"D:\Setup\a\", @"d:\Setup\b\", "D:")]
    [InlineData(@"\\nas\share\a\", @"\\NAS\SHARE\b\", @"\\nas\share")]
    public void A_wait_stopped_as_it_is_told_ends_at_once_and_its_root_is_kept_for_the_rest_of_the_pass_without_being_read(
        string heldFolder, string otherFolder, string root)
    {
        // The stop is made inside the call telling the wait, on the pass's own thread, before
        // the wait it names has begun. The second program's folder is under the same root,
        // spelled in another case.
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(heldFolder, otherFolder);
        using var held = files;
        var told = new List<string?>();

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var screening = ScreenStoppingWaits(f, files, wait =>
        {
            told.Add(wait?.Root);
            wait?.StopWaiting();
        });

        Assert.True(clock.Elapsed < HeldFor, $"the screen took {clock.Elapsed}");
        Assert.All(screening.Outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.Equal(new[] { root, null }, told);
        Assert.DoesNotContain(otherPackage, files.Calls);
        Assert.Equal([new(root, SourceRootGiveUpRoute.StoppedWaiting, 2)], GivenUp(screening));

        // The wait stopped was still told, so it counts, and the read not started after it
        // makes none.
        Assert.Equal(1, screening.WaitCount);
    }

    [Fact]
    public async Task A_wait_stopped_from_another_thread_ends_at_once()
    {
        // The caller is handed the wait on the pass's thread and stops it from a thread of its
        // own a moment later, as a window does, once the wait has begun.
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"D:\Setup\b\");
        using var held = files;
        var told = new List<string?>();
        Task? stopping = null;

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var screening = ScreenStoppingWaits(f, files, wait =>
        {
            told.Add(wait?.Root);
            if (wait is not null)
                stopping = Task.Run(async () =>
                {
                    await Task.Delay(WaitThreshold);
                    wait.StopWaiting();
                });
        });

        Assert.True(clock.Elapsed < HeldFor, $"the screen took {clock.Elapsed}");
        Assert.NotNull(stopping);
        await stopping;
        Assert.All(screening.Outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.Equal(new[] { "D:", null }, told);
        Assert.DoesNotContain(otherPackage, files.Calls);
        Assert.Equal([new("D:", SourceRootGiveUpRoute.StoppedWaiting, 2)], GivenUp(screening));
    }

    [Fact]
    public void A_stop_gives_up_only_the_root_it_names()
    {
        // The second program's folder is on another drive, so its package is read and its copy
        // let through.
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"E:\Setup\b\");
        using var held = files;

        var screening = ScreenStoppingWaits(f, files, wait => wait?.StopWaiting());

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductInstalled, DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile },
            screening.Outcomes);
        Assert.Contains(otherPackage, files.Calls);
        Assert.Equal([new("D:", SourceRootGiveUpRoute.StoppedWaiting, 1)], GivenUp(screening));
    }

    [Fact]
    public void Every_installation_package_is_kept_beside_a_second_copy_whose_source_the_caller_stopped_waiting_for()
    {
        // The second copy's package is in a local folder, so it is read for every candidate.
        var f = AMarkedSecondCopy();
        f.Files.Opens(SetupPackage, 9);
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SetupPackage, HeldFor);

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderTimeLimit = TimeSpan.FromMinutes(1),
                SourceFolderWaitThreshold = WaitThreshold,
                DriveKindOf = FixedDrive,
                NamesInFolderOf = NameOnly,
            }
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed, default, null, InInstallerFolder,
                waitingOn: wait => wait?.StopWaiting());

        Assert.True(clock.Elapsed < HeldFor, $"the screen took {clock.Elapsed}");
        Assert.All(screening.Outcomes, outcome => Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome));
        Assert.Single(files.Started, read => read == SetupPackage);
        Assert.Equal([new("D:", SourceRootGiveUpRoute.StoppedWaiting, 2)], GivenUp(screening));
    }

    [Fact]
    public void A_wait_for_a_drive_to_say_what_kind_it_is_can_be_stopped_and_nothing_under_the_drive_is_read()
    {
        using var released = new ManualResetEventSlim();
        var (f, files, _) = TwoProductsBesideAHeldSource(@"Z:\Setup\a\", @"Z:\Setup\b\");
        using var held = files;
        var told = new List<string?>();

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderTimeLimit = TimeSpan.FromMinutes(1),
                SourceFolderWaitThreshold = WaitThreshold,
                DriveKindOf = _ =>
                {
                    released.Wait(HeldFor);
                    return DriveType.Fixed;
                },
                NamesInFolderOf = NameOnly,
            }
            .Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder,
                waitingOn: wait =>
                {
                    told.Add(wait?.Root);
                    wait?.StopWaiting();
                });

        Assert.True(clock.Elapsed < HeldFor, $"the screen took {clock.Elapsed}");
        Assert.All(screening.Outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.Equal(new[] { "Z:", null }, told);
        Assert.DoesNotContain(files.Calls, read => read.StartsWith(@"Z:\", StringComparison.OrdinalIgnoreCase));
        Assert.Equal([new("Z:", SourceRootGiveUpRoute.StoppedWaiting, 2)], GivenUp(screening));
        released.Set();
    }

    [Fact]
    public void A_stop_made_as_a_wait_ends_leaves_the_read_its_answer_and_keeps_its_root_for_the_rest_of_the_pass()
    {
        // A's package answers after a wait. The stop is made in the call telling the wait's
        // end, once the read has answered, with the wait the caller was handed as it began.
        // A's copy is let through on that answer, so only B's copy counts towards the root.
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"D:\Setup\b\");
        using var held = files;
        files.Holds(@"D:\Setup\a\" + CandidateName, SlowRead);
        SourceFolderWait? shown = null;
        var told = new List<string?>();

        var screening = ScreenStoppingWaits(f, files, wait =>
        {
            told.Add(wait?.Root);
            if (wait is null) shown?.StopWaiting();
            else shown = wait;
        });

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, DeclaredProductOutcome.DeclaredProductInstalled },
            screening.Outcomes);
        Assert.Equal(new[] { "D:", null }, told);
        Assert.DoesNotContain(otherPackage, files.Calls);
        Assert.Equal([new("D:", SourceRootGiveUpRoute.StoppedWaiting, 1)], GivenUp(screening));
    }

    [Theory]
    [InlineData(true, 1000, 500, 60_000, 300_000, false, SourceRootGiveUpRoute.SlowFailure, 2)]
    [InlineData(true, 1000, 500, 60_000, 300_000, true, SourceRootGiveUpRoute.StoppedWaiting, 2)]
    [InlineData(true, 600, 5_000, 200, 300_000, false, SourceRootGiveUpRoute.FailedReadsAddUp, 1)]
    [InlineData(true, 600, 5_000, 200, 300_000, true, SourceRootGiveUpRoute.StoppedWaiting, 1)]
    [InlineData(false, 600, 5_000, 60_000, 200, false, SourceRootGiveUpRoute.ReadsAddUp, 1)]
    [InlineData(false, 600, 5_000, 60_000, 200, true, SourceRootGiveUpRoute.StoppedWaiting, 1)]
    public void A_root_whose_read_saw_the_stop_is_listed_as_stopped_whatever_else_that_read_met(
        bool refused, int heldMs, int slowFailureMs, int failedBudgetMs, int readBudgetMs, bool stop,
        SourceRootGiveUpRoute route, int kept)
    {
        // A's package is held past the threshold and answers within the limit: refused after a
        // second, past the slow-failure bar, so A's copy counts too; refused sooner, taking the
        // failed reads past their budget; or opening, taking all the reads past theirs. A row
        // with no stop shows the route the read meets on its own. A row with one makes it in
        // the call telling the wait's end, once the read has answered. B's copy is not read.
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"D:\Setup\b\");
        using var held = files;
        if (refused) f.Files.Answers(@"D:\Setup\a\" + CandidateName, FileIdentityRead.OpenRefused);
        files.Holds(@"D:\Setup\a\" + CandidateName, TimeSpan.FromMilliseconds(heldMs));
        SourceFolderWait? shown = null;

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderTimeLimit = HeldFor,
                SourceFolderWaitThreshold = WaitThreshold,
                SourceFolderSlowFailure = TimeSpan.FromMilliseconds(slowFailureMs),
                SourceFolderFailedWaitBudget = TimeSpan.FromMilliseconds(failedBudgetMs),
                SourceFolderReadBudget = TimeSpan.FromMilliseconds(readBudgetMs),
                DriveKindOf = FixedDrive,
                NamesInFolderOf = NameOnly,
            }
            .Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder,
                waitingOn: wait =>
                {
                    if (wait is not null) shown = wait;
                    else if (stop) shown?.StopWaiting();
                });

        Assert.NotNull(shown);
        Assert.DoesNotContain(otherPackage, files.Calls);
        Assert.Equal([new("D:", route, kept)], GivenUp(screening));
    }

    [Fact]
    public void A_stop_made_between_reads_keeps_the_next_read_under_its_root_from_starting()
    {
        // A's package answers after a wait. The stop is made, with the wait the caller was
        // handed for it, as the pass reaches the second candidate, before B's package is read.
        // B's folder spells the drive in lower case, and the root is listed as the wait the
        // caller stopped spelled it.
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"d:\Setup\b\");
        using var held = files;
        files.Holds(@"D:\Setup\a\" + CandidateName, SlowRead);
        SourceFolderWait? shown = null;
        var told = new List<string?>();

        var screening = ScreenStoppingWaits(f, files,
            wait =>
            {
                told.Add(wait?.Root);
                shown ??= wait;
            },
            reached =>
            {
                if (reached == 2) shown!.StopWaiting();
            });

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, DeclaredProductOutcome.DeclaredProductInstalled },
            screening.Outcomes);
        Assert.Equal(new[] { "D:", null }, told);
        Assert.DoesNotContain(otherPackage, files.Calls);
        Assert.Equal([new("D:", SourceRootGiveUpRoute.StoppedWaiting, 1)], GivenUp(screening));
    }

    [Fact]
    public void A_stop_made_as_a_later_read_under_its_root_starts_ends_that_read_at_once_and_tells_nothing_of_it()
    {
        // Both packages are under D: and each is held past the threshold, a second here. The
        // stop is made, with the wait the caller was handed for A's read, as B's read starts,
        // so it lands inside B's threshold, and B's read is timed from its start.
        var threshold = TimeSpan.FromSeconds(1);
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"D:\Setup\b\");
        using var held = files;
        files.Holds(@"D:\Setup\a\" + CandidateName, threshold + SlowRead);
        files.Holds(otherPackage, HeldFor);
        SourceFolderWait? shown = null;
        System.Diagnostics.Stopwatch? sinceBStarted = null;
        files.OnStart(otherPackage, () =>
        {
            sinceBStarted = System.Diagnostics.Stopwatch.StartNew();
            shown!.StopWaiting();
        });
        var told = new List<string?>();

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderTimeLimit = TimeSpan.FromMinutes(1),
                SourceFolderWaitThreshold = threshold,
                DriveKindOf = FixedDrive,
                NamesInFolderOf = NameOnly,
            }
            .Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder,
                waitingOn: wait =>
                {
                    told.Add(wait?.Root);
                    shown ??= wait;
                });

        Assert.NotNull(sinceBStarted);
        Assert.True(sinceBStarted.Elapsed < threshold / 2, $"B's read went on for {sinceBStarted.Elapsed} after it started");
        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, DeclaredProductOutcome.DeclaredProductInstalled },
            screening.Outcomes);
        Assert.Equal(new[] { "D:", null }, told);
        Assert.Equal([new("D:", SourceRootGiveUpRoute.StoppedWaiting, 1)], GivenUp(screening));
    }

    [Fact]
    public void A_stop_made_once_its_pass_has_ended_changes_nothing_in_the_next()
    {
        var (f, files, otherPackage) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"D:\Setup\b\");
        using var held = files;
        files.Holds(@"D:\Setup\a\" + CandidateName, SlowRead);
        var check = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderTimeLimit = HeldFor,
                SourceFolderWaitThreshold = WaitThreshold,
                DriveKindOf = FixedDrive,
                NamesInFolderOf = NameOnly,
            };
        SourceFolderWait? shown = null;

        check.Screen([Package(Candidate)], [], default, null, InInstallerFolder, waitingOn: wait => shown ??= wait);
        Assert.NotNull(shown);
        shown.StopWaiting();
        var screening = check.Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder);

        Assert.All(screening.Outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome));
        Assert.Contains(otherPackage, files.Calls);
        Assert.Empty(screening.RootsGivenUp);
    }

    // ---- The drives and shares a pass gives up ----
    //
    // Every root the pass gives up comes back once, in the order it was given up, with how it
    // came to be and how many files the pass kept at it: files whose check stopped at a read
    // the pass refused for the root, because the root had been given up, the caller's stop
    // refused it or ended it before it answered, the read did not answer within the time
    // limit, or the read answered false only after a long wait. A read whose own answer was
    // used counts nothing, and neither does a file its own comparison keeps. The tests of a
    // root the caller stopped waiting for are under "Stopping a wait".

    /// <summary>The roots a screening gave up, for comparing with what a test expects.</summary>
    private static SourceRootGivenUp[] GivenUp(DeclaredProductScreening screening) => [.. screening.RootsGivenUp];

    [Fact]
    public void A_share_whose_package_does_not_answer_is_given_up_with_the_copy_kept_at_it()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, CandidateName, ShareFolder);
        f.Files.Opens(SharePackage, 9);
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SharePackage, HeldFor);

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate)], [], default, null, InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, screening.Outcomes[0]);
        Assert.Equal([new(@"\\nas\share", SourceRootGiveUpRoute.NoAnswer, 1)], GivenUp(screening));
    }

    [Fact]
    public void Every_copy_of_a_program_whose_source_does_not_answer_is_counted_towards_its_drive()
    {
        // Two copies declare product A, whose source package on drive D: does not answer. The
        // answer about product A is asked once and kept for both copies.
        const string AnotherCopy = @"C:\Windows\Installer\a3.msi";
        var f = ACopyBesideTheRecordedPackage();
        f.Packages.Declares(AnotherCopy, ProductA);
        f.Files.Opens(AnotherCopy, 7);
        f.Disk.AddFile(AnotherCopy, new MockFileData(new byte[100]));
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SetupPackage, HeldFor);

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate), Package(AnotherCopy)], [], default, null, InInstallerFolder);

        Assert.All(screening.Outcomes,
            outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.Equal([new("D:", SourceRootGiveUpRoute.NoAnswer, 2)], GivenUp(screening));
        Assert.Single(files.Started, read => read == SetupPackage);
    }

    [Theory]
    [InlineData(@"D:\Setup\a\", @"D:\Setup\b\", "D:")]
    [InlineData(@"\\nas\share\a\", @"\\nas\share\b\", @"\\nas\share")]
    public void A_root_given_up_for_a_read_refused_only_after_a_long_wait_counts_that_copy_and_the_one_after_it(
        string heldFolder, string otherFolder, string root)
    {
        var (f, files, _) = TwoProductsBesideAHeldSource(heldFolder, otherFolder);
        using var held = files;
        f.Files.Answers(heldFolder + CandidateName, FileIdentityRead.OpenRefused);
        files.Holds(heldFolder + CandidateName, SlowAnswer);

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderTimeLimit = HeldFor,
                SourceFolderSlowFailure = SlowFailure,
                DriveKindOf = FixedDrive,
                NamesInFolderOf = NameOnly,
            }
            .Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder);

        Assert.Equal([new(root, SourceRootGiveUpRoute.SlowFailure, 2)], GivenUp(screening));
    }

    [Fact]
    public void A_read_refused_at_once_is_its_own_answer_and_gives_nothing_up()
    {
        var (f, files, _) = TwoProductsBesideAHeldSource(@"D:\Setup\a\", @"D:\Setup\b\");
        using var held = files;
        f.Files.Answers(@"D:\Setup\a\" + CandidateName, FileIdentityRead.OpenRefused);
        files.Holds(@"D:\Setup\a\" + CandidateName, TimeSpan.Zero);

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderTimeLimit = HeldFor,
                SourceFolderSlowFailure = SlowFailure,
                DriveKindOf = FixedDrive,
                NamesInFolderOf = NameOnly,
            }
            .Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, screening.Outcomes[0]);
        Assert.Empty(screening.RootsGivenUp);
    }

    [Theory]
    [InlineData(@"D:\Setup\", "D:", 24, 3)]
    [InlineData(@"\\nas\share\", @"\\nas\share", 24, 3)]
    [InlineData(@"D:\Setup\", "D:", 21, 0)]
    [InlineData(@"\\nas\share\", @"\\nas\share", 21, 0)]
    public void A_root_whose_refused_reads_add_up_past_a_minute_counts_only_the_copies_left_unread(
        string folder, string root, int programs, int kept)
    {
        // Twenty-one reads of three seconds take the root past a minute. The twenty-first is the
        // read's own answer, and only a program after it is refused for the root.
        var (f, files, clock, copies, _) =
            ProgramsWhosePackagesAreRefused([.. Enumerable.Repeat(folder, programs)], UnopenedReadOnTheClock);
        using var held = files;

        var screening = CheckOnTheClock(f, files, clock)
            .Screen([.. copies.Select(Package)], [], default, null, InInstallerFolder);

        Assert.Equal([new(root, SourceRootGiveUpRoute.FailedReadsAddUp, kept)], GivenUp(screening));
    }

    [Theory]
    [InlineData(@"D:\Setup\", "D:", 16, 3)]
    [InlineData(@"\\nas\share\", @"\\nas\share", 16, 3)]
    [InlineData(@"D:\Setup\", "D:", 13, 0)]
    [InlineData(@"\\nas\share\", @"\\nas\share", 13, 0)]
    public void A_root_whose_reads_add_up_past_five_minutes_counts_only_the_copies_left_unread(
        string folder, string root, int programs, int kept)
    {
        // Thirteen reads of twenty-five seconds take the root past five minutes. The thirteenth
        // opens its package and its copy is let through.
        var (f, files, clock, copies, _) =
            ProgramsWhosePackagesOpen([.. Enumerable.Repeat(folder, programs)], ReadOnTheClock);
        using var held = files;

        var screening = CheckOnTheClock(f, files, clock)
            .Screen([.. copies.Select(Package)], [], default, null, InInstallerFolder);

        Assert.Equal([new(root, SourceRootGiveUpRoute.ReadsAddUp, kept)], GivenUp(screening));
    }

    [Fact]
    public void A_read_refused_after_a_long_wait_that_also_takes_its_root_past_its_budget_gives_it_up_as_a_slow_failure()
    {
        // The first read is refused after twenty-five seconds on the check's clock, past the
        // slow-failure bar, and the budget for every read under the root is twenty seconds.
        var (f, files, clock, copies, _) =
            ProgramsWhosePackagesAreRefused([.. Enumerable.Repeat(@"D:\Setup\", 3)], ReadOnTheClock);
        using var held = files;

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                Clock = clock,
                SourceFolderReadBudget = TimeSpan.FromSeconds(20),
                DriveKindOf = FixedDrive,
                NamesInFolderOf = NameOnly,
            }
            .Screen([.. copies.Select(Package)], [], default, null, InInstallerFolder);

        Assert.Equal([new("D:", SourceRootGiveUpRoute.SlowFailure, 3)], GivenUp(screening));
    }

    [Fact]
    public void A_read_that_takes_both_totals_past_their_budgets_gives_its_root_up_for_the_reads_that_failed()
    {
        // Every read is refused after three seconds on the check's clock, and both budgets are
        // a minute, so the twenty-first read takes both totals past at once.
        var (f, files, clock, copies, _) =
            ProgramsWhosePackagesAreRefused([.. Enumerable.Repeat(@"D:\Setup\", 24)], UnopenedReadOnTheClock);
        using var held = files;

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                Clock = clock,
                SourceFolderReadBudget = TimeSpan.FromMinutes(1),
                DriveKindOf = FixedDrive,
                NamesInFolderOf = NameOnly,
            }
            .Screen([.. copies.Select(Package)], [], default, null, InInstallerFolder);

        Assert.Equal([new("D:", SourceRootGiveUpRoute.FailedReadsAddUp, 3)], GivenUp(screening));
    }

    [Fact]
    public void A_drive_whose_kind_does_not_answer_is_given_up_with_every_copy_kept_at_it()
    {
        using var released = new ManualResetEventSlim();
        var (f, files, _) = TwoProductsBesideAHeldSource(@"Z:\Setup\a\", @"Z:\Setup\b\");
        using var held = files;

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            {
                SourceFolderTimeLimit = ShortLimit,
                DriveKindOf = _ =>
                {
                    released.Wait(HeldFor);
                    return DriveType.Fixed;
                },
                NamesInFolderOf = NameOnly,
            }
            .Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder);

        Assert.Equal([new("Z:", SourceRootGiveUpRoute.NoAnswer, 2)], GivenUp(screening));
        released.Set();
    }

    [Fact]
    public void A_root_is_given_up_once_under_the_spelling_it_was_first_given_up_under()
    {
        // The second program's folder is the same share spelled in capitals.
        var (f, files, _) = TwoProductsBesideAHeldSource(ShareFolder, @"\\NAS\SHARE\b\");
        using var held = files;

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate), Package(OtherCandidate)], [], default, null, InInstallerFolder);

        Assert.Equal([new(@"\\nas\share", SourceRootGiveUpRoute.NoAnswer, 2)], GivenUp(screening));
    }

    [Fact]
    public void Roots_are_given_back_in_the_order_the_pass_gave_them_up()
    {
        var (f, copies, sourcePackages) = ProgramsInstalledFromFoldersUnder([@"\\one\apps\", @"\\two\apps\"]);
        using var files = new HeldFileIdentities(f.Files);
        foreach (var package in sourcePackages) files.Holds(package, HeldFor);

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([.. copies.Select(Package)], [], default, null, InInstallerFolder);

        Assert.Equal(
            [
                new(@"\\one\apps", SourceRootGiveUpRoute.NoAnswer, 1),
                new(@"\\two\apps", SourceRootGiveUpRoute.NoAnswer, 1),
            ],
            GivenUp(screening));
    }

    [Fact]
    public void Every_copy_kept_beside_a_second_copy_whose_source_does_not_answer_is_counted_towards_its_drive()
    {
        // The second copies' packages are read once, for the first copy, and their answer is
        // kept for the pass.
        var f = AMarkedSecondCopy();
        f.Files.Opens(SetupPackage, 9);
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SetupPackage, HeldFor);

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed, default, null, InInstallerFolder);

        Assert.All(screening.Outcomes,
            outcome => Assert.Equal(DeclaredProductOutcome.SecondCopyUnestablished, outcome));
        Assert.Equal([new("D:", SourceRootGiveUpRoute.NoAnswer, 2)], GivenUp(screening));
    }

    [Theory]
    [InlineData(false, DeclaredProductOutcome.SecondCopyUnestablished, 1)]
    [InlineData(true, DeclaredProductOutcome.DeclaredProductInstalled, 0)]
    public void A_copy_its_own_program_keeps_is_not_counted_towards_a_second_copys_drive(
        bool opensAsTheCachedPackage, DeclaredProductOutcome expected, int kept)
    {
        // Product A is installed too and its own source on D: finds no file, so its answer
        // alone would let the copy through, unless the copy opens as product A's cached
        // package. The second copy was installed from D:\Other, which does not answer.
        var f = AMarkedSecondCopy();
        AlsoInstallProductA(f);
        if (opensAsTheCachedPackage) f.Files.Opens(Recorded, 1);
        f.Msi.RecordsSources(SecondCopy, null, MsiInstallContext.Machine, SetupName, OtherFolder);
        f.Files.Opens(OtherPackage, 9);
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(OtherPackage, HeldFor);

        var screening = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate)], f.Listed, default, null, InInstallerFolder);

        Assert.Equal(expected, screening.Outcomes[0]);
        Assert.Equal([new("D:", SourceRootGiveUpRoute.NoAnswer, kept)], GivenUp(screening));
    }

    [Fact]
    public void A_pass_that_reads_every_source_it_needs_gives_nothing_up()
    {
        var f = ACopyBesideTheRecordedPackage();

        var screening = ScriptedCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen([Package(Candidate)], [], default, null, InInstallerFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, screening.Outcomes[0]);
        Assert.Empty(screening.RootsGivenUp);
    }

    // ---- A package in a folder on the network ----
    //
    // Windows Installer looks in a source folder for the file named by the package name
    // and for no other. A package in a folder on the network, a share or a drive Windows
    // reports as a network drive, is read only for a candidate whose name or short name
    // the package name could be, and a package in any other folder is read for every
    // candidate. While Windows is set to follow a symbolic link reached through a network
    // path, every package is read for every candidate. Each package here will not
    // identify, so a read of it keeps the copy, and a package not read lets it through.

    private const string NasFolder = @"\\nas\apps\";
    private const string SecondCandidate = @"C:\Windows\Installer\c.msi";

    /// <summary>
    /// <see cref="ACopyBesideTheRecordedPackage"/> with product A installed from
    /// <paramref name="folder"/> alone, under the package name
    /// <paramref name="packageName"/>, whose package will not identify.
    /// </summary>
    private static ((ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
        ScriptedFileIdentities Files, MockFileSystem Disk) F, string Package)
        ACopyBesideASource(string folder, string packageName)
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, packageName, folder);
        var package = folder + packageName;
        f.Files.Answers(package, FileIdentityRead.OpenRefused);
        return (f, package);
    }

    /// <summary>A second copy of product A in the Installer folder, <see cref="SecondCandidate"/>.</summary>
    private static void AddSecondCandidate(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk) f)
    {
        f.Packages.Declares(SecondCandidate, ProductA);
        f.Files.Opens(SecondCandidate, 5);
        f.Disk.AddFile(SecondCandidate, new MockFileData(new byte[100]));
    }

    /// <summary>
    /// The check over <paramref name="f"/>, with every drive letter answering
    /// <paramref name="driveKind"/> and each candidate's folder entry holding what
    /// <paramref name="namesInFolder"/> gives, or its name alone.
    /// </summary>
    private static DeclaredProductCheck CheckBesideASource(
        (ScriptedPackageIdentities Packages, ScriptedMsiProducts Msi,
            ScriptedFileIdentities Files, MockFileSystem Disk) f,
        DriveType driveKind = DriveType.Fixed,
        Func<string, IReadOnlyList<string>?>? namesInFolder = null) =>
        new(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
        {
            DriveKindOf = _ => driveKind,
            NamesInFolderOf = namesInFolder ?? NameOnly,
        };

    [Theory]
    [InlineData(@"\\nas\apps\", DriveType.Fixed)]
    [InlineData(@"\\?\UNC\nas\apps\", DriveType.Fixed)]
    [InlineData(@"\\.\UNC\nas\apps\", DriveType.Fixed)]
    [InlineData(@"Z:\apps\", DriveType.Network)]
    public void A_package_on_the_network_named_otherwise_than_the_copy_is_not_read_and_the_copy_is_let_through(
        string folder, DriveType driveKind)
    {
        var (f, package) = ACopyBesideASource(folder, SetupName);

        var outcome = CheckBesideASource(f, driveKind)
            .Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.DoesNotContain(package, f.Files.Reads);
    }

    [Fact]
    public void A_package_in_the_network_folder_a_program_was_installed_from_named_otherwise_is_not_read()
    {
        // The folder is the InstallSource alone, and the list holds a local folder, whose
        // package is read.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsInstallSource(ProductA, null, MsiInstallContext.Machine, NasFolder);
        f.Files.Answers(NasFolder + SetupName, FileIdentityRead.OpenRefused);

        var outcome = CheckBesideASource(f).Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.DoesNotContain(NasFolder + SetupName, f.Files.Reads);
        Assert.Contains(SetupPackage, f.Files.Reads);
    }

    [Fact]
    public void A_second_copys_package_on_the_network_is_not_read_for_candidates_named_otherwise()
    {
        var f = AMarkedSecondCopy();
        f.Msi.RecordsSources(SecondCopy, null, MsiInstallContext.Machine, SetupName, NasFolder);
        f.Files.Answers(NasFolder + SetupName, FileIdentityRead.OpenRefused);

        var outcomes = new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            { DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed, default, null, InInstallerFolder).Outcomes;

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcome));
        Assert.DoesNotContain(NasFolder + SetupName, f.Files.Reads);
    }

    [Fact]
    public void A_package_on_the_network_that_will_not_identify_keeps_only_the_copy_it_is_named_as()
    {
        var (f, package) = ACopyBesideASource(NasFolder, "c.msi");
        AddSecondCandidate(f);

        var outcomes = CheckBesideASource(f)
            .Screen([Package(Candidate), Package(SecondCandidate)], [], default, null, InInstallerFolder).Outcomes;

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, DeclaredProductOutcome.DeclaredProductInstalled },
            outcomes);
        Assert.Single(f.Files.Reads, read => read == package);
    }

    [Fact]
    public void A_second_copys_package_on_the_network_that_does_not_answer_keeps_only_the_candidate_it_is_named_as()
    {
        var f = AMarkedSecondCopy();
        f.Msi.RecordsSources(SecondCopy, null, MsiInstallContext.Machine, CandidateName, ShareFolder);
        f.Files.Opens(SharePackage, 9);
        using var files = new HeldFileIdentities(f.Files);
        files.Holds(SharePackage, HeldFor);

        var outcomes = new DeclaredProductCheck(f.Msi, f.Packages, files, f.Disk, f.Msi.Registry)
            { SourceFolderTimeLimit = ShortLimit, DriveKindOf = FixedDrive, NamesInFolderOf = NameOnly }
            .Screen([Package(Candidate), Package(OtherCandidate)], f.Listed, default, null, InInstallerFolder).Outcomes;

        Assert.Equal(
            new[] { DeclaredProductOutcome.SecondCopyUnestablished, DeclaredProductOutcome.DeclaredProductNotInstalled },
            outcomes);
        Assert.Single(files.Started, read => read == SharePackage);
    }

    [Fact]
    public void A_share_onto_the_Installer_folder_keeps_the_copy_its_package_name_names_and_lets_another_copy_through()
    {
        // The Installer folder reached through this PC's admin share counts as the folder
        // itself, as the scan's comparison by folder identity answers.
        const string AdminShare = @"\\localhost\C$\Windows\Installer\";
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, "c.msi", AdminShare);
        AddSecondCandidate(f);
        var asked = new List<string>();

        var outcomes = CheckBesideASource(f).Screen(
            [Package(Candidate), Package(SecondCandidate)], [], default, null, path =>
            {
                asked.Add(path);
                return path.StartsWith(AdminShare, StringComparison.OrdinalIgnoreCase) ? true : InInstallerFolder(path);
            }).Outcomes;

        Assert.Equal(
            new[] { DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, DeclaredProductOutcome.DeclaredProductInstalled },
            outcomes);
        Assert.Equal(new[] { AdminShare + "c.msi" }, asked);
    }

    [Theory]
    [InlineData(@"\\localhost\C$\Windows\Installer\", DriveType.Fixed, true)]
    [InlineData(@"Z:\", DriveType.Network, true)]
    [InlineData(@"Z:\", DriveType.Unknown, false)]
    [InlineData(@"Z:\", DriveType.NoRootDirectory, false)]
    [InlineData(@"\\?\GLOBALROOT\Device\Mup\localhost\C$\Windows\Installer\", DriveType.Fixed, false)]
    public void Every_copy_is_kept_when_a_package_read_for_every_copy_and_not_on_a_local_drive_is_in_the_Installer_folder(
        string folder, DriveType driveKind, bool remoteLinksFollowed)
    {
        // A share onto the Installer folder, and a network drive, while Windows may follow a
        // link reached through a network path; a drive Windows reports as neither local nor on
        // the network; and a path through the network redirector's device. Each package is read
        // for every copy, is in the Installer folder as the scan's test answers, and opens as
        // the second copy. The first copy is kept as well, and the package is not opened.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, "c.msi", folder);
        AddSecondCandidate(f);
        f.Files.Opens(folder + "c.msi", 5);
        if (remoteLinksFollowed)
            f.Msi.Registry.HoldsLinkSetting(ScriptedSourceListRegistry.LinkSettingsKey,
                ScriptedSourceListRegistry.RemoteToLocal, new RegistryDwordRead(RegistryDwordState.Read, 1));

        var outcomes = CheckBesideASource(f, driveKind).Screen(
            [Package(Candidate), Package(SecondCandidate)], [], default, null,
            path => path.StartsWith(folder, StringComparison.OrdinalIgnoreCase) ? true : InInstallerFolder(path)).Outcomes;

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome));
        Assert.DoesNotContain(folder + "c.msi", f.Files.Reads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_copy_is_kept_when_whether_a_package_on_the_network_is_in_the_Installer_folder_is_not_established(
        bool remoteLinksFollowed)
    {
        // The package is read for this copy by its name, or for every copy while Windows may
        // follow a link reached through a network path. It would open as another file.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, CandidateName, ShareFolder);
        f.Files.Opens(SharePackage, 9);
        if (remoteLinksFollowed)
            f.Msi.Registry.HoldsLinkSetting(ScriptedSourceListRegistry.LinkSettingsKey,
                ScriptedSourceListRegistry.RemoteToLocal, new RegistryDwordRead(RegistryDwordState.Read, 1));

        var outcome = CheckBesideASource(f).Screen([Package(Candidate)], [], default, null, _ => null).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.DoesNotContain(SharePackage, f.Files.Reads);
    }

    [Theory]
    [InlineData("a.msi::$DATA")]
    [InlineData(@"C:\Windows\Installer\a.msi")]
    [InlineData(@"sub\..\a.msi")]
    [InlineData("sub/../a.msi")]
    [InlineData("a.msi\0x")]
    public void A_package_name_holding_a_colon_a_separator_or_a_null_keeps_the_copy_whatever_network_folder_it_is_in(
        string packageName)
    {
        // A stream of the copy, a rooted path to it, a relative path ending in its name, and
        // its name cut short by a null.
        var (f, package) = ACopyBesideASource(NasFolder, packageName);

        var outcome = CheckBesideASource(f).Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.DoesNotContain(package, f.Files.Reads);
    }

    [Theory]
    [InlineData("a.msi")]
    [InlineData("A.MSI")]
    [InlineData("a.msi.")]
    [InlineData("a.msi ")]
    [InlineData("a.msi . .")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("...")]
    [InlineData("a.ms\u0131")]
    public void A_package_on_the_network_whose_name_could_be_the_copys_is_read(string packageName)
    {
        // The copy's own name in any case, with trailing dots or spaces, a name of dots
        // alone, and a name holding a letter outside ASCII.
        var (f, package) = ACopyBesideASource(NasFolder, packageName);

        var outcome = CheckBesideASource(f).Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.Contains(package, f.Files.Reads);
    }

    [Theory]
    [InlineData("setup.msi", "SETUP.MSI")]
    [InlineData("a~1.msi", "A~1.MSI")]
    public void A_package_on_the_network_named_as_the_copys_short_name_is_read(string packageName, string shortName)
    {
        // A short name given by hand need not carry a '~'.
        var (f, package) = ACopyBesideASource(NasFolder, packageName);

        var outcome = CheckBesideASource(f, namesInFolder: _ => [CandidateName, shortName])
            .Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.Contains(package, f.Files.Reads);
    }

    [Fact]
    public void Every_package_on_the_network_is_read_for_a_copy_whose_folder_entry_does_not_read()
    {
        var (f, package) = ACopyBesideASource(NasFolder, SetupName);

        var outcome = CheckBesideASource(f, namesInFolder: _ => null)
            .Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.Contains(package, f.Files.Reads);
    }

    [Fact]
    public void A_package_on_the_network_is_read_once_in_a_pass_for_every_copy_it_could_be()
    {
        // Neither copy's folder entry reads, so either could carry the package's name as its
        // short name.
        var (f, package) = ACopyBesideASource(NasFolder, SetupName);
        f.Files.Opens(package, 9);
        AddSecondCandidate(f);

        var outcomes = CheckBesideASource(f, namesInFolder: _ => null)
            .Screen([Package(Candidate), Package(SecondCandidate)], [], default, null, InInstallerFolder).Outcomes;

        Assert.All(outcomes, outcome => Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome));
        Assert.Single(f.Files.Reads, read => read == package);
    }

    [Theory]
    [InlineData(DriveType.Fixed)]
    [InlineData(DriveType.Removable)]
    [InlineData(DriveType.Unknown)]
    [InlineData(DriveType.NoRootDirectory)]
    public void A_package_on_a_drive_Windows_does_not_report_as_a_network_drive_is_read_whatever_its_name(
        DriveType driveKind)
    {
        var (f, package) = ACopyBesideASource(@"Z:\apps\", SetupName);

        var outcome = CheckBesideASource(f, driveKind)
            .Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.Contains(package, f.Files.Reads);
    }

    [Theory]
    [InlineData(ScriptedSourceListRegistry.LinkSettingsKey, ScriptedSourceListRegistry.RemoteToLocal, RegistryDwordState.Read, 1)]
    [InlineData(ScriptedSourceListRegistry.LinkSettingsKey, ScriptedSourceListRegistry.RemoteToRemote, RegistryDwordState.Read, 1)]
    [InlineData(ScriptedSourceListRegistry.LinkPolicyKey, ScriptedSourceListRegistry.RemoteToLocal, RegistryDwordState.Read, 1)]
    [InlineData(ScriptedSourceListRegistry.LinkPolicyKey, ScriptedSourceListRegistry.RemoteToRemote, RegistryDwordState.Read, 1)]
    [InlineData(ScriptedSourceListRegistry.LinkSettingsKey, ScriptedSourceListRegistry.RemoteToLocal, RegistryDwordState.Read, 2)]
    [InlineData(ScriptedSourceListRegistry.LinkSettingsKey, ScriptedSourceListRegistry.RemoteToLocal, RegistryDwordState.WrongType, 0)]
    [InlineData(ScriptedSourceListRegistry.LinkPolicyKey, ScriptedSourceListRegistry.RemoteToRemote, RegistryDwordState.Unreadable, 0)]
    public void Every_package_on_the_network_is_read_while_Windows_may_follow_a_link_reached_through_a_network_path(
        string key, string value, RegistryDwordState state, int number)
    {
        var (f, package) = ACopyBesideASource(NasFolder, SetupName);
        f.Msi.Registry.HoldsLinkSetting(key, value, new RegistryDwordRead(state, number));

        var outcome = CheckBesideASource(f).Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.Contains(package, f.Files.Reads);
    }

    [Theory]
    [InlineData(ScriptedSourceListRegistry.LinkSettingsKey, ScriptedSourceListRegistry.RemoteToLocal)]
    [InlineData(ScriptedSourceListRegistry.LinkPolicyKey, ScriptedSourceListRegistry.RemoteToRemote)]
    public void A_link_setting_holding_0_leaves_a_package_on_the_network_read_by_name(string key, string value)
    {
        var (f, package) = ACopyBesideASource(NasFolder, SetupName);
        f.Msi.Registry.HoldsLinkSetting(key, value, new RegistryDwordRead(RegistryDwordState.Read, 0));

        var outcome = CheckBesideASource(f).Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.DoesNotContain(package, f.Files.Reads);
    }

    [Fact]
    public void The_link_settings_are_read_once_in_a_pass()
    {
        const string MoreFolder = @"\\nas\more\";
        var (f, _) = ACopyBesideASource(NasFolder, SetupName);
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName, NasFolder, MoreFolder);

        CheckBesideASource(f).Screen([Package(Candidate)], [], default, null, InInstallerFolder);

        Assert.Equal(
            new[]
            {
                (ScriptedSourceListRegistry.LinkSettingsKey, ScriptedSourceListRegistry.RemoteToLocal),
                (ScriptedSourceListRegistry.LinkSettingsKey, ScriptedSourceListRegistry.RemoteToRemote),
                (ScriptedSourceListRegistry.LinkPolicyKey, ScriptedSourceListRegistry.RemoteToLocal),
                (ScriptedSourceListRegistry.LinkPolicyKey, ScriptedSourceListRegistry.RemoteToRemote),
            },
            f.Msi.Registry.LinkSettingReads);
    }

    [Fact]
    public void No_link_setting_is_read_where_no_package_is_on_the_network()
    {
        var f = ACopyBesideTheRecordedPackage();

        var outcome = CheckBesideASource(f).Screen([Package(Candidate)], [], default, null, InInstallerFolder).Outcomes[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.Empty(f.Msi.Registry.LinkSettingReads);
    }
}

/// <summary>
/// A scripted <see cref="IPackageIdentityReader"/>. Shared with
/// <see cref="FileSystemScanServiceDeclaredProductTests"/>, which drives the real
/// check through the real scan.
///
/// AN UNSCRIPTED PATH THROWS RATHER THAN YIELDING NOTHING. "Nothing to read" is
/// one of the two answers under test and it is the one that keeps a file, so a
/// fake handing it back by default would let a test assert a withholding that the
/// fixture, not the code, produced.
/// </summary>
internal sealed class ScriptedPackageIdentities : IPackageIdentityReader
{
    private readonly Dictionary<string, PackageIdentity?> _byPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _notes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every path this reader was asked about, in order.</summary>
    public List<string> Reads { get; } = new();

    /// <summary>
    /// Every path this reader was asked to take the patch reading of, in order. The real
    /// reader opens a patch's summary stream and an installation package's database, and
    /// a patch read the other way yields nothing, so which reading was asked for is part
    /// of what a test pins.
    /// </summary>
    public List<string> PatchReads { get; } = new();

    public void Declares(string path, string productCode) =>
        Yields(path, new PackageIdentity(productCode, IsPatch: false, Array.Empty<string>()));

    /// <summary>The file is a patch declaring its own code and the products it may be applied to.</summary>
    public void DeclaresPatch(string path, string patchCode, params string[] targets) =>
        Yields(path, new PackageIdentity(patchCode, IsPatch: true, targets));

    public void Yields(string path, PackageIdentity identity)
    {
        _byPath[path] = identity;
        _notes.Remove(path);
        _declaresNoCode.Remove(path);
    }

    /// <summary>
    /// The file would not give up an identity at all. The note is what the real
    /// reader writes to say WHICH of its refusals this was, and it is the thing the
    /// screen is meant to pass on rather than drop.
    /// </summary>
    public void YieldsNothing(string path, string note = "")
    {
        _byPath[path] = null;
        _notes[path] = note;
        _declaresNoCode.Remove(path);
    }

    /// <summary>
    /// The file read and declares no code the reader could use, as the real reader answers for
    /// a ProductCode that is missing, empty or not a well-formed GUID.
    /// </summary>
    public void DeclaresNoCode(string path)
    {
        _byPath[path] = null;
        _notes.Remove(path);
        _declaresNoCode.Add(path);
    }

    private readonly HashSet<string> _declaresNoCode = new(StringComparer.OrdinalIgnoreCase);

    public PackageIdentity? Read(string filePath, bool isPatch, out string detail, out PackageReadRefusal refusal)
    {
        Reads.Add(filePath);
        if (isPatch) PatchReads.Add(filePath);
        detail = _notes.TryGetValue(filePath, out var note) ? note : string.Empty;
        refusal = _declaresNoCode.Contains(filePath) ? PackageReadRefusal.DeclaresNoCode : PackageReadRefusal.WouldNotRead;
        if (!_byPath.TryGetValue(filePath, out var identity))
            throw new InvalidOperationException(
                $"the fake reader was asked to read {filePath}, which no test scripted");
        return identity;
    }
}

/// <summary>
/// A scripted <see cref="IMsiApi"/> answering the questions this area asks: the keyed
/// product enumeration, the LocalPackage, PackageName and InstallSource each
/// installation records, each installation's network source list, the machine-wide
/// patch enumeration, the State and LocalPackage a patch records against each product
/// holding it, and the package name and network source list a patch has in each
/// account and context.
///
/// AN UNSCRIPTED CODE THROWS, for the reason the reader's does: "Windows does not
/// hold that product" is the single answer that lets a file through, so a fake
/// giving it by default would let a test assert an offer nothing established. The
/// patch questions throw on anything unscripted for the same reason: "no registration
/// of that patch" is the answer that lets a patch copy through.
/// </summary>
internal sealed class ScriptedMsiProducts : IMsiApi
{
    public ScriptedMsiProducts() => Registry = new ScriptedSourceListRegistry(this);

    /// <summary>
    /// The registry keys holding the source lists this API was scripted with, which hold
    /// by default what the API answers.
    /// </summary>
    public ScriptedSourceListRegistry Registry { get; }

    private readonly Dictionary<string, uint> _answers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string? Sid, MsiInstallContext Context)[]> _instances =
        new(StringComparer.Ordinal);
    private readonly Dictionary<(string ProductCode, uint Index), uint> _rowAnswers = new();

    /// <summary>
    /// Every product code this API was asked about, in order, once each. Recorded at
    /// index 0, because the walk over a code's rows is one question about one code.
    /// </summary>
    public List<string> Asked { get; } = new();

    /// <summary>
    /// Every keyed row this API answered, ending row included, so a test can pin that
    /// the walk stops where the rows do rather than at a number.
    /// </summary>
    public int Rows { get; private set; }

    /// <summary>
    /// Installed as one ordinary per-machine instance, which is what a fixture with
    /// nothing to say about instances means.
    /// </summary>
    public void Installed(string productCode) =>
        Installed(productCode, (null, MsiInstallContext.Machine));

    /// <summary>
    /// Installed as the instances given, in enumeration order. One product code can
    /// name more than one installation, per machine and per user at once or under two
    /// accounts, and each is its own row with its own account and context.
    /// </summary>
    public void Installed(string productCode, params (string? Sid, MsiInstallContext Context)[] instances)
    {
        _answers[productCode] = MsiError.Success;
        _instances[productCode] = instances;
    }

    /// <summary>
    /// What one ROW of a code's enumeration returns, keyed by index. It wins over the
    /// per-code answer, which is what builds a walk that reads an instance and then
    /// meets a return it cannot read.
    /// </summary>
    public void AnswersAtRow(string productCode, uint index, uint error) =>
        _rowAnswers[(productCode, index)] = error;

    /// <param name="absence">
    /// Which of the returns that mean absence to give. Named by the caller rather
    /// than picked here, because which returns are allowed to carry that meaning
    /// is the thing under test.
    /// </param>
    public void NotInstalled(string productCode, uint absence) => _answers[productCode] = absence;

    public void Answers(string productCode, uint error) => _answers[productCode] = error;

    public uint EnumProducts(string? productCode, string? userSid, MsiInstallContext context, uint index,
        char[]? installedProductCode, out MsiInstallContext installedContext, char[]? sid, ref uint sidLength)
    {
        installedContext = MsiInstallContext.Machine;

        if (productCode is null)
            throw new InvalidOperationException(
                "the fake was asked to walk every product; this area only asks keyed questions");

        Rows++;
        if (index == 0) Asked.Add(productCode);

        if (!_answers.TryGetValue(productCode, out var result))
            throw new InvalidOperationException(
                $"the fake was asked about {productCode}, which no test scripted");

        if (_rowAnswers.TryGetValue((productCode, index), out var row)) return row;
        if (result != MsiError.Success) return result;

        // A code scripted to succeed with nothing said about instances is one ordinary
        // per-machine instance, which is what Installed(code) means and what every
        // fixture that never mentions them is describing.
        var instances = _instances.TryGetValue(productCode, out var scripted)
            ? scripted
            : new[] { ((string?)null, MsiInstallContext.Machine) };
        if (index >= instances.Length) return MsiError.NoMoreItems;

        // The buffer is written on success because the real API does, and a fake that
        // leaves it empty is a fake with a shape the code has never met. The caller
        // reads the SID back only outside the machine context, which is the rule the
        // real API's own output follows.
        if (installedProductCode is not null)
            for (var i = 0; i < productCode.Length && i < installedProductCode.Length - 1; i++)
                installedProductCode[i] = productCode[i];

        var (instanceSid, instanceContext) = instances[(int)index];
        installedContext = instanceContext;
        if (instanceSid is not null && sid is not null)
        {
            for (var i = 0; i < instanceSid.Length && i < sid.Length; i++) sid[i] = instanceSid[i];
            sidLength = (uint)instanceSid.Length;
        }

        return MsiError.Success;
    }

    private readonly List<(string PatchCode, string ProductCode, string? Sid, MsiInstallContext Context)> _patchRows = new();
    private readonly Dictionary<uint, uint> _patchRowAnswers = new();
    private bool _patchRowsScripted;
    private bool _patchRowsEndless;

    /// <summary>
    /// How many times the machine-wide patch enumeration was started, counted at its
    /// first index, so a test can pin that one pass walks it once.
    /// </summary>
    public int PatchEnumerations { get; private set; }

    /// <summary>
    /// One row of the machine-wide patch enumeration: the patch is registered against
    /// the product in that account and context. Rows come back in the order scripted.
    /// </summary>
    public void HoldsPatch(string patchCode, string productCode, string? sid, MsiInstallContext context)
    {
        _patchRowsScripted = true;
        _patchRows.Add((patchCode, productCode, sid, context));
    }

    /// <summary>The machine-wide patch enumeration ends at once, naming no registration.</summary>
    public void HoldsNoPatches() => _patchRowsScripted = true;

    /// <summary>What one index of the machine-wide patch enumeration returns instead of a row.</summary>
    public void PatchEnumerationAnswersAt(uint index, uint error)
    {
        _patchRowsScripted = true;
        _patchRowAnswers[index] = error;
    }

    /// <summary>A machine-wide patch enumeration whose every index answers with one row, and which never ends.</summary>
    public void PatchEnumerationNeverEnds(string patchCode, string productCode)
    {
        _patchRowsScripted = true;
        _patchRowsEndless = true;
        _patchRows.Add((patchCode, productCode, null, MsiInstallContext.Machine));
    }

    /// <summary>
    /// Answers the machine-wide patch enumeration, the one call made with no product
    /// code, one scripted row per index and <see cref="MsiError.NoMoreItems"/> past the
    /// last. Anything narrower is a question this area does not ask, and throws.
    ///
    /// AN UNSCRIPTED ENUMERATION THROWS. A machine holding no patch registration is an
    /// answer that lets a patch copy through, so a fake giving it by default would let a
    /// test assert an offer nothing established.
    /// </summary>
    public uint EnumPatches(string? productCode, string? userSid, MsiInstallContext context, MsiPatchFilter filter,
        uint index, char[]? patchCode, char[]? targetProductCode, out MsiInstallContext targetProductContext,
        char[]? targetUserSid, ref uint targetUserSidLength)
    {
        targetProductContext = MsiInstallContext.Machine;

        if (productCode is not null || userSid != "S-1-1-0"
            || context != MsiInstallContext.All || filter != MsiPatchFilter.All)
            throw new InvalidOperationException(
                "the declared-product check enumerates every patch of every product in every account, "
                + $"and was asked for {productCode ?? "every product"} as {userSid ?? "the current user"} "
                + $"in {context} with filter {filter}");

        if (!_patchRowsScripted)
            throw new InvalidOperationException(
                "the fake was asked for the machine's patch registrations, which no test scripted");

        if (index == 0) PatchEnumerations++;

        if (_patchRowAnswers.TryGetValue(index, out var error)) return error;
        if (!_patchRowsEndless && index >= _patchRows.Count) return MsiError.NoMoreItems;

        var (rowPatch, rowProduct, rowSid, rowContext) = _patchRows[_patchRowsEndless ? 0 : (int)index];

        // Both GUID buffers and the account are written, because the real API writes
        // them and the caller reads the account back only outside the machine context.
        if (patchCode is not null)
            for (var i = 0; i < rowPatch.Length && i < patchCode.Length - 1; i++) patchCode[i] = rowPatch[i];
        if (targetProductCode is not null)
            for (var i = 0; i < rowProduct.Length && i < targetProductCode.Length - 1; i++)
                targetProductCode[i] = rowProduct[i];

        targetProductContext = rowContext;
        if (rowSid is not null && targetUserSid is not null)
        {
            for (var i = 0; i < rowSid.Length && i < targetUserSid.Length; i++) targetUserSid[i] = rowSid[i];
            targetUserSidLength = (uint)rowSid.Length;
        }
        else targetUserSidLength = 0;

        return MsiError.Success;
    }

    private readonly Dictionary<(string ProductCode, string? Sid, MsiInstallContext Context), (uint Error, string Value)>
        _localPackages = new();

    /// <summary>Every LocalPackage read this API answered, in order.</summary>
    public List<(string ProductCode, string? Sid, MsiInstallContext Context)> PackageReads { get; } = new();

    /// <summary>
    /// The LocalPackage value one installation of a product records. An empty value is
    /// a record that names no package, which the real API returns for a record that
    /// never carried the property.
    /// </summary>
    public void RecordsPackage(string productCode, string? sid, MsiInstallContext context, string localPackage) =>
        _localPackages[(productCode, sid, context)] = (MsiError.Success, localPackage);

    /// <summary>What reading one installation's LocalPackage returns instead of a value.</summary>
    public void PackageReadAnswers(string productCode, string? sid, MsiInstallContext context, uint error) =>
        _localPackages[(productCode, sid, context)] = (error, string.Empty);

    private readonly Dictionary<(string ProductCode, string? Sid, MsiInstallContext Context), (uint Error, string Value)>
        _packageNames = new();

    /// <summary>
    /// One network source list as a test scripts it: the folders in list order, what
    /// every index answers instead where the list will not read, whether it never ends,
    /// and one index that answers an error of its own where the rest read.
    /// </summary>
    private sealed record SourceList(
        string[] Folders,
        uint Error = MsiError.Success,
        bool Endless = false,
        uint? FailingIndex = null,
        uint FailingError = MsiError.Success);

    private readonly Dictionary<(string ProductCode, string? Sid, MsiInstallContext Context), SourceList>
        _sources = new();

    /// <summary>Every product PackageName read this API answered, in order.</summary>
    public List<(string ProductCode, string? Sid, MsiInstallContext Context)> PackageNameReads { get; } = new();

    /// <summary>
    /// Every walk of a product's source list this API started, in order, recorded at
    /// the call at index 0 that carries a buffer.
    /// </summary>
    public List<(string ProductCode, string? Sid, MsiInstallContext Context)> SourceListWalks { get; } = new();

    /// <summary>
    /// The package name and the network source folders one installation records, in
    /// list order.
    /// </summary>
    public void RecordsSources(string productCode, string? sid, MsiInstallContext context,
        string packageName, params string[] folders)
    {
        _packageNames[(productCode, sid, context)] = (MsiError.Success, packageName);
        _sources[(productCode, sid, context)] = new SourceList(folders);
        _urlSources[(productCode, sid, context)] = new SourceList(Array.Empty<string>());
    }

    /// <summary>What reading one installation's PackageName returns instead of a value.</summary>
    public void PackageNameAnswers(string productCode, string? sid, MsiInstallContext context, uint error) =>
        _packageNames[(productCode, sid, context)] = (error, string.Empty);

    private readonly Dictionary<(string ProductCode, string? Sid, MsiInstallContext Context), uint>
        _sourceListPackageNames = new();

    /// <summary>Every product PackageName read off the source list this API answered, in order.</summary>
    public List<(string ProductCode, string? Sid, MsiInstallContext Context)> SourceListPackageNameReads { get; } = new();

    /// <summary>
    /// What reading one installation's PackageName off its source list returns, in place of
    /// what <see cref="GetProductInfo"/> answers for it.
    /// </summary>
    public void SourceListPackageNameAnswers(string productCode, string? sid, MsiInstallContext context, uint error) =>
        _sourceListPackageNames[(productCode, sid, context)] = error;

    private readonly Dictionary<(string ProductCode, string? Sid, MsiInstallContext Context), Queue<uint>>
        _sourceListPackageNameTurns = new();

    /// <summary>
    /// What successive reads of one installation's PackageName off its source list return, one
    /// answer per read in the order given, the last repeating: a source list that appears or
    /// goes while a pass runs. It wins over <see cref="SourceListPackageNameAnswers"/>.
    /// </summary>
    public void SourceListPackageNameAnswersInTurn(string productCode, string? sid, MsiInstallContext context,
        params uint[] errors) =>
        _sourceListPackageNameTurns[(productCode, sid, context)] = new Queue<uint>(errors);

    /// <summary>What reading one installation's source list returns instead of an entry.</summary>
    public void SourceListAnswers(string productCode, string? sid, MsiInstallContext context, uint error) =>
        _sources[(productCode, sid, context)] = new SourceList(Array.Empty<string>(), error);

    /// <summary>
    /// What one index of an installation's scripted source list returns instead of its
    /// entry. The entries before it read.
    /// </summary>
    public void SourceListEntryAnswers(string productCode, string? sid, MsiInstallContext context,
        uint index, uint error) =>
        _sources[(productCode, sid, context)] = _sources[(productCode, sid, context)] with
        {
            FailingIndex = index,
            FailingError = error,
        };

    /// <summary>A source list whose every index answers with another folder, and which never ends.</summary>
    public void SourceListNeverEnds(string productCode, string? sid, MsiInstallContext context) =>
        _sources[(productCode, sid, context)] = new SourceList(new[] { @"D:\Somewhere\" }, Endless: true);

    private readonly Dictionary<(string ProductCode, string? Sid, MsiInstallContext Context), (uint Error, string Value)>
        _installSources = new();

    /// <summary>
    /// The InstallSource one installation records, in place of what its scripted source
    /// list gives: the list's first network entry, or none where it has no network entry.
    /// </summary>
    public void RecordsInstallSource(string productCode, string? sid, MsiInstallContext context, string folder) =>
        _installSources[(productCode, sid, context)] = (MsiError.Success, folder);

    /// <summary>What reading one installation's InstallSource returns instead of a value.</summary>
    public void InstallSourceAnswers(string productCode, string? sid, MsiInstallContext context, uint error) =>
        _installSources[(productCode, sid, context)] = (error, string.Empty);

    /// <summary>
    /// The answer for one installation's InstallSource: as scripted, or else the first
    /// network entry of its scripted list. AN UNSCRIPTED LIST THROWS, for the reason every
    /// other read here does.
    /// </summary>
    private (uint Error, string Value) InstallSourceFor(string productCode, string? sid, MsiInstallContext context)
    {
        if (_installSources.TryGetValue((productCode, sid, context), out var scripted)) return scripted;

        if (!_sources.TryGetValue((productCode, sid, context), out var list))
            throw new InvalidOperationException(
                $"the fake was asked for the InstallSource {productCode} records for {sid ?? "the machine"} "
                + $"in {context}, whose source list no test scripted");

        return (MsiError.Success, list.Folders.Length > 0 ? list.Folders[0] : string.Empty);
    }

    private readonly Dictionary<(string ProductCode, string? Sid, MsiInstallContext Context), (uint Error, string Value)>
        _packageCodes = new();

    private readonly Dictionary<(string ProductCode, string? Sid, MsiInstallContext Context), (uint Error, string Value)>
        _instanceTypes = new();

    /// <summary>Every PackageCode and InstanceType read this API answered, in order, with the property.</summary>
    public List<(string Property, string ProductCode, string? Sid, MsiInstallContext Context)> RecordReads { get; } = new();

    /// <summary>
    /// The record one installation keeps of itself answers, with a package code and the
    /// InstanceType given. A null InstanceType is a record that does not carry the value,
    /// which the real API answers with ERROR_UNKNOWN_PROPERTY.
    /// </summary>
    public void AnswersItsOwnRecord(string productCode, string? sid, MsiInstallContext context,
        string? instanceType = "0")
    {
        _packageCodes[(productCode, sid, context)] = (MsiError.Success, "{55555555-5555-5555-5555-555555555555}");
        _instanceTypes[(productCode, sid, context)] = instanceType is null
            ? (MsiError.UnknownProperty, string.Empty)
            : (MsiError.Success, instanceType);
    }

    /// <summary>What reading one installation's PackageCode returns instead of a package code.</summary>
    public void PackageCodeAnswers(string productCode, string? sid, MsiInstallContext context, uint error,
        string value = "") =>
        _packageCodes[(productCode, sid, context)] = (error, value);

    /// <summary>What reading one installation's InstanceType returns instead of a value.</summary>
    public void InstanceTypeAnswers(string productCode, string? sid, MsiInstallContext context, uint error) =>
        _instanceTypes[(productCode, sid, context)] = (error, string.Empty);

    /// <summary>
    /// Answers LocalPackage, PackageName, InstallSource, PackageCode and InstanceType, the
    /// product properties the check reads, with the real API's two-call shape: a null
    /// buffer is answered with the length, a buffer with the value.
    ///
    /// AN UNSCRIPTED INSTALLATION THROWS. A recorded package that is present and is
    /// another file is the answer that lets a file through, so a fake inventing one
    /// would let a test assert an offer nothing established. So is a record answering
    /// that an installation is ordinary.
    /// </summary>
    public uint GetProductInfo(string productCode, string? userSid, MsiInstallContext context, string property,
        char[]? value, ref uint valueLength)
    {
        (uint Error, string Value) scripted;
        if (property == MsiInstallProperty.InstallSource)
        {
            scripted = InstallSourceFor(productCode, userSid, context);
        }
        else
        {
            var table = property switch
            {
                MsiInstallProperty.LocalPackage => _localPackages,
                MsiInstallProperty.PackageName => _packageNames,
                MsiInstallProperty.PackageCode => _packageCodes,
                MsiInstallProperty.InstanceType => _instanceTypes,
                _ => throw new InvalidOperationException(
                    "the declared-product check reads LocalPackage, PackageName, InstallSource, PackageCode "
                    + $"and InstanceType, and was asked for {property}"),
            };

            if (!table.TryGetValue((productCode, userSid, context), out scripted))
                throw new InvalidOperationException(
                    $"the fake was asked for the {property} {productCode} records for {userSid ?? "the machine"} "
                    + $"in {context}, which no test scripted");
        }

        if (value is null && property == MsiInstallProperty.LocalPackage) PackageReads.Add((productCode, userSid, context));
        if (value is null && property == MsiInstallProperty.PackageName) PackageNameReads.Add((productCode, userSid, context));
        if (value is null && property is MsiInstallProperty.PackageCode or MsiInstallProperty.InstanceType)
            RecordReads.Add((property, productCode, userSid, context));
        if (scripted.Error != MsiError.Success) return scripted.Error;

        if (value is not null)
            for (var i = 0; i < scripted.Value.Length && i < value.Length; i++) value[i] = scripted.Value[i];
        valueLength = (uint)scripted.Value.Length;
        return MsiError.Success;
    }

    private readonly Dictionary<(string PatchCode, string? Sid, MsiInstallContext Context), (uint Error, string Value)>
        _patchPackageNames = new();

    private readonly Dictionary<(string PatchCode, string? Sid, MsiInstallContext Context), SourceList>
        _patchSources = new();

    /// <summary>Every patch PackageName read this API answered, in order.</summary>
    public List<(string PatchCode, string? Sid, MsiInstallContext Context)> PatchPackageNameReads { get; } = new();

    /// <summary>
    /// Every walk of a patch's source list this API started, in order, recorded at the
    /// call at index 0 that carries a buffer.
    /// </summary>
    public List<(string PatchCode, string? Sid, MsiInstallContext Context)> PatchSourceListWalks { get; } = new();

    /// <summary>
    /// The package name and the network source folders a patch has in one account and
    /// context, in list order.
    /// </summary>
    public void RecordsPatchSources(string patchCode, string? sid, MsiInstallContext context,
        string packageName, params string[] folders)
    {
        _patchPackageNames[(patchCode, sid, context)] = (MsiError.Success, packageName);
        _patchSources[(patchCode, sid, context)] = new SourceList(folders);
        _patchUrlSources[(patchCode, sid, context)] = new SourceList(Array.Empty<string>());
    }

    private readonly Dictionary<(string Code, string? Sid, MsiInstallContext Context), SourceList>
        _urlSources = new();

    private readonly Dictionary<(string Code, string? Sid, MsiInstallContext Context), SourceList>
        _patchUrlSources = new();

    /// <summary>
    /// The URL entries on an installation's or a patch's source list, in list order. A
    /// list scripted with <see cref="RecordsSources"/> or <see cref="RecordsPatchSources"/>
    /// holds none until this says otherwise.
    /// </summary>
    public void RecordsUrls(string code, bool isPatch, string? sid, MsiInstallContext context, params string[] urls) =>
        (isPatch ? _patchUrlSources : _urlSources)[(code, sid, context)] = new SourceList(urls);

    /// <summary>What reading an installation's or a patch's URL entries returns instead of an entry.</summary>
    public void UrlListAnswers(string code, bool isPatch, string? sid, MsiInstallContext context, uint error) =>
        (isPatch ? _patchUrlSources : _urlSources)[(code, sid, context)] =
            new SourceList(Array.Empty<string>(), error);

    private readonly Dictionary<(string Code, bool IsPatch, string? Sid, MsiInstallContext Context, string Property),
        (uint Error, string Value)> _listProperties = new();

    /// <summary>
    /// What an installation's or a patch's source list answers for
    /// <see cref="MsiInstallProperty.LastUsedSource"/>,
    /// <see cref="MsiInstallProperty.LastUsedType"/> or
    /// <see cref="MsiInstallProperty.MediaPackagePath"/>, in place of what the scripted
    /// list gives: its first network entry as the source used last, of type "n", no source
    /// used last where it has no network entry, and no media package path.
    /// </summary>
    public void ListProperty(string code, bool isPatch, string? sid, MsiInstallContext context,
        string property, string value) =>
        _listProperties[(code, isPatch, sid, context, property)] = (MsiError.Success, value);

    /// <summary>What reading one of those three properties returns instead of a value.</summary>
    public void ListPropertyAnswers(string code, bool isPatch, string? sid, MsiInstallContext context,
        string property, uint error) =>
        _listProperties[(code, isPatch, sid, context, property)] = (error, string.Empty);

    /// <summary>
    /// The answer for one of the three properties <see cref="ListProperty"/> names.
    /// AN UNSCRIPTED LIST THROWS, for the reason every other read here does.
    /// </summary>
    private (uint Error, string Value) ListPropertyOf(string code, bool isPatch, string? sid,
        MsiInstallContext context, string property)
    {
        if (_listProperties.TryGetValue((code, isPatch, sid, context, property), out var scripted)) return scripted;

        if (!(isPatch ? _patchSources : _sources).TryGetValue((code, sid, context), out var list))
            throw new InvalidOperationException(
                $"the fake was asked for the {property} of {(isPatch ? "patch" : "product")} {code} "
                + $"for {sid ?? "the machine"} in {context}, whose source list no test scripted");

        var first = list.Folders.Length > 0 ? list.Folders[0] : string.Empty;
        return property switch
        {
            MsiInstallProperty.LastUsedSource => (MsiError.Success, first),
            MsiInstallProperty.LastUsedType => (MsiError.Success, first.Length > 0 ? "n" : string.Empty),
            MsiInstallProperty.MediaPackagePath => (MsiError.Success, string.Empty),
            _ => throw new InvalidOperationException($"the fake scripts no source-list property {property}"),
        };
    }

    /// <summary>
    /// A <c>SourceList</c> key, or its <c>Net</c>, <c>URL</c> or <c>Media</c> key, per
    /// machine or per user and managed.
    /// </summary>
    private static readonly Regex SourceListKey = new(
        @"^SOFTWARE\\(?:Classes\\Installer|Microsoft\\Windows\\CurrentVersion\\Installer\\Managed\\(?<sid>S-[0-9-]+)\\Installer)"
        + @"\\(?<kind>Products|Patches)\\(?<packed>[0-9A-F]{32})\\SourceList(?:\\(?<key>Net|URL|Media))?$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// An installation's <c>InstallProperties</c> key, per machine under <c>S-1-5-18</c>
    /// or per user and managed under the account.
    /// </summary>
    private static readonly Regex InstallPropertiesKey = new(
        @"^SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\(?<sid>S-[0-9-]+)"
        + @"\\Products\\(?<packed>[0-9A-F]{32})\\InstallProperties$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// What the registry key at <paramref name="path"/> holds for the list this API was
    /// scripted with, as Windows holds it: the package name as a REG_SZ; the source used
    /// last as a REG_EXPAND_SZ holding its type, the index 1 and its text, each followed
    /// by a ';' but the last; each network entry as a REG_EXPAND_SZ named by its number
    /// from 1, and each URL entry the same way, a key with none being absent; and a
    /// <c>Media</c> key holding the REG_SZ "1" and the media package path where there is
    /// one. An installation's <c>InstallProperties</c> key holds its InstallSource as a
    /// REG_SZ where it has one. A path that names no scripted list throws.
    /// </summary>
    internal RegistryKeyValues MirroredKey(string path)
    {
        var properties = InstallPropertiesKey.Match(path);
        if (properties.Success)
        {
            var account = properties.Groups["sid"].Value;
            var (installSid, installContext) = account == "S-1-5-18"
                ? ((string?)null, MsiInstallContext.Machine)
                : (account, MsiInstallContext.UserManaged);
            var installSource = InstallSourceFor(
                UnpackedForTheFake(properties.Groups["packed"].Value), installSid, installContext);

            return new RegistryKeyValues(RegistryKeyPresence.Present,
                installSource.Error == MsiError.Success && installSource.Value.Length > 0
                    ? new[] { new RegistryValue(MsiInstallProperty.InstallSource, RegistryValueKind.String, installSource.Value) }
                    : Array.Empty<RegistryValue>());
        }

        var match = SourceListKey.Match(path);
        if (!match.Success)
            throw new InvalidOperationException(
                $"the fake registry was asked for {path}, which is neither a source-list key nor an InstallProperties key");

        var isPatch = match.Groups["kind"].Value == "Patches";
        var sid = match.Groups["sid"].Success ? match.Groups["sid"].Value : null;
        var context = sid is null ? MsiInstallContext.Machine : MsiInstallContext.UserManaged;
        var code = UnpackedForTheFake(match.Groups["packed"].Value);

        if (!(isPatch ? _patchSources : _sources).TryGetValue((code, sid, context), out var list))
            throw new InvalidOperationException(
                $"the fake registry was asked for {path}, whose list no test scripted");

        switch (match.Groups["key"].Value)
        {
            case "Net":
                return Numbered(list.Folders);
            case "URL":
                return Numbered((isPatch ? _patchUrlSources : _urlSources).TryGetValue((code, sid, context), out var urls)
                    ? urls.Folders
                    : Array.Empty<string>());
            case "Media":
            {
                var media = new List<RegistryValue> { new("1", RegistryValueKind.String, ";") };
                var packagePath = ListPropertyOf(code, isPatch, sid, context, MsiInstallProperty.MediaPackagePath);
                if (packagePath.Error == MsiError.Success && packagePath.Value.Length > 0)
                    media.Add(new RegistryValue("MediaPackage", RegistryValueKind.String, packagePath.Value));
                return new RegistryKeyValues(RegistryKeyPresence.Present, media);
            }
            default:
            {
                var values = new List<RegistryValue>();
                if ((isPatch ? _patchPackageNames : _packageNames).TryGetValue((code, sid, context), out var name)
                    && name.Error == MsiError.Success)
                    values.Add(new RegistryValue(MsiInstallProperty.PackageName, RegistryValueKind.String, name.Value));

                var source = ListPropertyOf(code, isPatch, sid, context, MsiInstallProperty.LastUsedSource);
                var type = ListPropertyOf(code, isPatch, sid, context, MsiInstallProperty.LastUsedType);
                if (source.Error == MsiError.Success && type.Error == MsiError.Success && source.Value.Length > 0)
                    values.Add(new RegistryValue(MsiInstallProperty.LastUsedSource, RegistryValueKind.ExpandString,
                        $"{type.Value};1;{source.Value}"));
                return new RegistryKeyValues(RegistryKeyPresence.Present, values);
            }
        }
    }

    private static RegistryKeyValues Numbered(string[] entries) =>
        entries.Length == 0
            ? new RegistryKeyValues(RegistryKeyPresence.Absent)
            : new RegistryKeyValues(RegistryKeyPresence.Present,
                entries.Select((entry, i) => new RegistryValue(
                        (i + 1).ToString(CultureInfo.InvariantCulture), RegistryValueKind.ExpandString, entry))
                    .ToArray());

    /// <summary>
    /// A packed code turned back into its braced GUID through the GUID's bytes: each pair
    /// of characters is one byte in the GUID's own byte order, written low digit first.
    /// Worked out apart from the production code's field-by-field form, so the two are
    /// not one mistake made twice.
    /// </summary>
    private static string UnpackedForTheFake(string packed)
    {
        var bytes = new byte[16];
        for (var i = 0; i < 16; i++)
            bytes[i] = byte.Parse(string.Concat(packed[2 * i + 1], packed[2 * i]),
                NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new Guid(bytes).ToString("B").ToUpperInvariant();
    }

    /// <summary>ERROR_INVALID_PARAMETER, which a source-list call out of sequence answers.</summary>
    internal const uint InvalidParameter = 87;

    /// <summary>
    /// Where the source-list walk stands between calls: besides 0, the one index the
    /// next call may ask for.
    /// </summary>
    private uint _sourcePosition;

    /// <summary>
    /// Answers a network or a URL source list the way Windows does, one entry per index
    /// and <see cref="MsiError.NoMoreItems"/> past the last: a product's when the options
    /// say the code is a product code, and a patch's when they say it is a patch code.
    /// Each kind is looked up only among what was scripted for that kind, so a product
    /// code asked about as a patch, or a patch code as a product, throws. Only a network
    /// walk is recorded in <see cref="SourceListWalks"/> or
    /// <see cref="PatchSourceListWalks"/>.
    ///
    /// IT KEEPS THE POSITION OF THE WALK BETWEEN CALLS. A call at index 0 starts the
    /// walk again, a call at the position is answered, and a call at any other index
    /// answers ERROR_INVALID_PARAMETER. A success moves the position on by one, a call
    /// with a null buffer included. A buffer too small for the entry and its terminator
    /// answers <see cref="MsiError.MoreData"/> with the entry's length and leaves the
    /// position where it was.
    ///
    /// AN UNSCRIPTED INSTALLATION THROWS. An empty list is an answer that lets a file
    /// through, so a fake giving one by default would let a test assert an offer
    /// nothing established.
    /// </summary>
    public uint EnumSources(string productCodeOrPatchCode, string? userSid, MsiInstallContext context, uint options,
        uint index, char[]? source, ref uint sourceLength)
    {
        var (table, kind) = options switch
        {
            MsiSourceListOptions.Product | MsiSourceListOptions.Network => (_sources, "product"),
            MsiSourceListOptions.Patch | MsiSourceListOptions.Network => (_patchSources, "patch"),
            MsiSourceListOptions.Product | MsiSourceListOptions.Url => (_urlSources, "product URL"),
            MsiSourceListOptions.Patch | MsiSourceListOptions.Url => (_patchUrlSources, "patch URL"),
            _ => throw new InvalidOperationException(
                "the declared-product check reads a product's or a patch's network and URL sources, "
                + $"and was asked with options {options}"),
        };

        if (!table.TryGetValue((productCodeOrPatchCode, userSid, context), out var scripted))
            throw new InvalidOperationException(
                $"the fake was asked for the sources of {kind} {productCodeOrPatchCode} "
                + $"for {userSid ?? "the machine"} in {context}, which no test scripted");

        if (index == 0)
        {
            _sourcePosition = 0;
            if (source is not null && kind == "product")
                SourceListWalks.Add((productCodeOrPatchCode, userSid, context));
            else if (source is not null && kind == "patch")
                PatchSourceListWalks.Add((productCodeOrPatchCode, userSid, context));
        }
        else if (index != _sourcePosition)
        {
            return InvalidParameter;
        }

        if (scripted.Error != MsiError.Success) return scripted.Error;
        if (index == scripted.FailingIndex) return scripted.FailingError;
        if (!scripted.Endless && index >= scripted.Folders.Length) return MsiError.NoMoreItems;

        var folder = scripted.Folders[scripted.Endless ? 0 : (int)index];
        if (source is not null)
        {
            if (sourceLength <= (uint)folder.Length)
            {
                sourceLength = (uint)folder.Length;
                return MsiError.MoreData;
            }

            folder.CopyTo(0, source, 0, folder.Length);
            source[folder.Length] = '\0';
        }

        sourceLength = (uint)folder.Length;
        _sourcePosition = index + 1;
        return MsiError.Success;
    }

    /// <summary>
    /// Answers the source-list properties the check reads, with the real API's two-call
    /// shape: PackageName for a patch, and for a product as scripted with
    /// <see cref="SourceListPackageNameAnswers"/> or else as <see cref="GetProductInfo"/>
    /// answers it; and for either kind the three <see cref="ListProperty"/> names. Anything
    /// else throws.
    ///
    /// AN UNSCRIPTED PATCH OR LIST THROWS. A package name naming no file at any source is
    /// an answer that lets a patch copy through, so a fake inventing one would let a test
    /// assert an offer nothing established. A product whose package name nothing scripted
    /// answers ERROR_UNKNOWN_PRODUCT, which keeps the file.
    /// </summary>
    public uint GetSourceListInfo(string productCodeOrPatchCode, string? userSid, MsiInstallContext context,
        uint options, string property, char[]? value, ref uint valueLength)
    {
        var isPatch = options switch
        {
            MsiSourceListOptions.Patch => true,
            MsiSourceListOptions.Product => false,
            _ => throw new InvalidOperationException(
                $"the declared-product check reads a source-list property with a product or a patch code, "
                + $"and was asked with options {options}"),
        };

        (uint Error, string Value) scripted;
        if (property == MsiInstallProperty.PackageName && !isPatch)
        {
            var key = (productCodeOrPatchCode, userSid, context);
            if (value is null) SourceListPackageNameReads.Add(key);
            scripted = _sourceListPackageNameTurns.TryGetValue(key, out var turns)
                    ? (turns.Count > 1 ? turns.Dequeue() : turns.Peek(), string.Empty)
                : _sourceListPackageNames.TryGetValue(key, out var error) ? (error, string.Empty)
                : _packageNames.TryGetValue(key, out var name) ? name
                : (MsiError.UnknownProduct, string.Empty);
        }
        else if (property == MsiInstallProperty.PackageName)
        {
            if (!_patchPackageNames.TryGetValue((productCodeOrPatchCode, userSid, context), out scripted))
                throw new InvalidOperationException(
                    $"the fake was asked for the PackageName patch {productCodeOrPatchCode} has "
                    + $"for {userSid ?? "the machine"} in {context}, which no test scripted");

            if (value is null) PatchPackageNameReads.Add((productCodeOrPatchCode, userSid, context));
        }
        else if (property is MsiInstallProperty.LastUsedSource or MsiInstallProperty.LastUsedType
                 or MsiInstallProperty.MediaPackagePath)
        {
            scripted = ListPropertyOf(productCodeOrPatchCode, isPatch, userSid, context, property);
        }
        else
        {
            throw new InvalidOperationException(
                $"the declared-product check reads no source-list property {property}");
        }

        if (scripted.Error != MsiError.Success) return scripted.Error;

        if (value is not null)
            for (var i = 0; i < scripted.Value.Length && i < value.Length; i++) value[i] = scripted.Value[i];
        valueLength = (uint)scripted.Value.Length;
        return MsiError.Success;
    }

    private readonly Dictionary<(string PatchCode, string ProductCode, string? Sid, MsiInstallContext Context), (uint Error, string Value)>
        _patchStates = new();

    private readonly Dictionary<(string PatchCode, string ProductCode, string? Sid, MsiInstallContext Context), (uint Error, string Value)>
        _patchPackages = new();

    /// <summary>Every patch State read this API answered, in order.</summary>
    public List<(string PatchCode, string ProductCode, string? Sid, MsiInstallContext Context)> PatchStateReads { get; } = new();

    /// <summary>Every patch LocalPackage read this API answered, in order.</summary>
    public List<(string PatchCode, string ProductCode, string? Sid, MsiInstallContext Context)> PatchPackageReads { get; } = new();

    /// <summary>
    /// The State a patch has against one installation of a product: a value where the
    /// patch is registered against it.
    /// </summary>
    public void PatchState(string patchCode, string productCode, string? sid, MsiInstallContext context, string state) =>
        _patchStates[(patchCode, productCode, sid, context)] = (MsiError.Success, state);

    /// <summary>
    /// What reading a patch's State against one installation returns instead of a value.
    /// <see cref="MsiError.UnknownPatch"/> is the answer for an installation the patch is
    /// not registered against.
    /// </summary>
    public void PatchStateAnswers(string patchCode, string productCode, string? sid, MsiInstallContext context, uint error) =>
        _patchStates[(patchCode, productCode, sid, context)] = (error, string.Empty);

    /// <summary>
    /// The LocalPackage value one registration of a patch records. An empty value is a
    /// registration that names no cached copy.
    /// </summary>
    public void RecordsPatchPackage(string patchCode, string productCode, string? sid, MsiInstallContext context,
        string localPackage) =>
        _patchPackages[(patchCode, productCode, sid, context)] = (MsiError.Success, localPackage);

    /// <summary>What reading one registration's LocalPackage returns instead of a value.</summary>
    public void PatchPackageReadAnswers(string patchCode, string productCode, string? sid, MsiInstallContext context,
        uint error) =>
        _patchPackages[(patchCode, productCode, sid, context)] = (error, string.Empty);

    /// <summary>
    /// Answers State and LocalPackage, the two patch properties the check reads, with the
    /// real API's two-call shape: a null buffer is answered with the length, a buffer
    /// with the value.
    ///
    /// AN UNSCRIPTED PAIRING THROWS. "The patch is not registered against this product"
    /// is an answer that lets a patch copy through, and a recorded copy that is present
    /// and is another file is the other, so a fake inventing either would let a test
    /// assert an offer nothing established.
    /// </summary>
    public uint GetPatchInfo(string patchCode, string productCode, string? userSid, MsiInstallContext context,
        string property, char[]? value, ref uint valueLength)
    {
        var (table, reads) = property switch
        {
            MsiInstallProperty.State => (_patchStates, PatchStateReads),
            MsiInstallProperty.LocalPackage => (_patchPackages, PatchPackageReads),
            _ => throw new InvalidOperationException(
                $"the declared-product check reads a patch's State and LocalPackage, and was asked for {property}"),
        };

        if (!table.TryGetValue((patchCode, productCode, userSid, context), out var scripted))
            throw new InvalidOperationException(
                $"the fake was asked for the {property} patch {patchCode} records against {productCode} "
                + $"for {userSid ?? "the machine"} in {context}, which no test scripted");

        if (value is null) reads.Add((patchCode, productCode, userSid, context));
        if (scripted.Error != MsiError.Success) return scripted.Error;

        if (value is not null)
            for (var i = 0; i < scripted.Value.Length && i < value.Length; i++) value[i] = scripted.Value[i];
        valueLength = (uint)scripted.Value.Length;
        return MsiError.Success;
    }
}

/// <summary>
/// A scripted <see cref="IFileIdentityReader"/>, standing in for the volume and file
/// ID a path opens.
///
/// AN UNSCRIPTED PATH THROWS. "A different file from the candidate" is the answer
/// that lets a file through, and a fake handing out fresh identities by default would
/// make every recorded package look like another file.
/// </summary>
internal sealed class ScriptedFileIdentities : IFileIdentityReader
{
    private readonly Dictionary<string, (FileIdentityRead Outcome, FileIdentity Identity)> _byPath =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every path this reader was asked about, in order.</summary>
    public List<string> Reads { get; } = new();

    /// <summary>
    /// The path opens as the file numbered <paramref name="fileId"/>. Two paths given
    /// the same number are one file under two spellings.
    /// </summary>
    public void Opens(string path, ulong fileId) =>
        _byPath[path] = (FileIdentityRead.Read, new FileIdentity(1, fileId, 0));

    public void Answers(string path, FileIdentityRead outcome) => _byPath[path] = (outcome, default);

    public FileIdentityRead ReadOutcome(string path, out FileIdentity identity)
    {
        Reads.Add(path);
        if (!_byPath.TryGetValue(path, out var scripted))
            throw new InvalidOperationException(
                $"the fake identity reader was asked about {path}, which no test scripted");
        identity = scripted.Identity;
        return scripted.Outcome;
    }
}

/// <summary>
/// A <see cref="ScriptedFileIdentities"/> whose answer for the paths a test holds comes only
/// once the test releases it or the hold runs out, standing in for a source folder on a
/// server that is slow to answer. Disposing it releases every read still held, and a read
/// released after its test has finished answers into a fixture nothing reads any more.
/// </summary>
internal sealed class HeldFileIdentities(ScriptedFileIdentities answers) : IFileIdentityReader, IDisposable
{
    private readonly ConcurrentDictionary<string, TimeSpan> _holds = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Action> _onStart = new(StringComparer.OrdinalIgnoreCase);
    private readonly ManualResetEventSlim _released = new();
    private (SteppedClock Clock, TimeSpan Each)? _onTheClock;

    /// <summary>Every path a read was started for, held or not, in order.</summary>
    public ConcurrentQueue<string> Calls { get; } = new();

    /// <summary>Every held path a read was started for, in order.</summary>
    public ConcurrentQueue<string> Started { get; } = new();

    /// <summary>A read of <paramref name="path"/> answers after <paramref name="hold"/>, or on release.</summary>
    public void Holds(string path, TimeSpan hold) => _holds[path] = hold;

    /// <summary>Every held read moves <paramref name="clock"/> on by <paramref name="each"/> as it starts.</summary>
    public void TakesOnTheClock(SteppedClock clock, TimeSpan each) => _onTheClock = (clock, each);

    /// <summary>A read of <paramref name="path"/> runs <paramref name="action"/> as it starts, on the read's own thread.</summary>
    public void OnStart(string path, Action action) => _onStart[path] = action;

    public FileIdentityRead ReadOutcome(string path, out FileIdentity identity)
    {
        Calls.Enqueue(path);
        if (_onStart.TryGetValue(path, out var onStart)) onStart();
        if (_holds.TryGetValue(path, out var hold))
        {
            Started.Enqueue(path);
            if (_onTheClock is { } onTheClock) onTheClock.Clock.Advance(onTheClock.Each);
            _released.Wait(hold);
        }

        return answers.ReadOutcome(path, out identity);
    }

    public void Dispose() => _released.Set();
}

/// <summary>
/// A clock that stands still until a test moves it on, for a check that reads how long a
/// read took off <see cref="DeclaredProductCheck.Clock"/>.
/// </summary>
internal sealed class SteppedClock : TimeProvider
{
    private long _ticks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => Interlocked.Read(ref _ticks);

    /// <summary>Moves the clock on by <paramref name="by"/>.</summary>
    public void Advance(TimeSpan by) => Interlocked.Add(ref _ticks, by.Ticks);
}

/// <summary>
/// A scripted <see cref="IRegistryReader"/> for the keys holding source lists and each
/// installation's InstallSource. By default each key holds what the
/// <see cref="ScriptedMsiProducts"/> it belongs to was scripted with, so the registry and
/// the API agree (<see cref="ScriptedMsiProducts.MirroredKey"/>).
/// A key a test scripts here answers as scripted instead, which is how a test makes the
/// two disagree. The scripted keys are matched by their exact spelling, so a test
/// scripting one also pins the path the check reads.
///
/// A PATH NAMING NO SCRIPTED LIST THROWS, and so does every read other than a key's
/// values and the four symbolic link settings, which are not there unless a test scripts
/// them (<see cref="HoldsLinkSetting"/>).
/// </summary>
internal sealed class ScriptedSourceListRegistry : IRegistryReader
{
    private readonly ScriptedMsiProducts _msi;
    private readonly Dictionary<string, RegistryKeyValues> _keys = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Key, string Value), RegistryDwordRead> _linkSettings = new();

    /// <summary>This PC's own symbolic link settings.</summary>
    public const string LinkSettingsKey = @"SYSTEM\CurrentControlSet\Control\FileSystem";

    /// <summary>The policy's symbolic link settings.</summary>
    public const string LinkPolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\Filesystems\NTFS";

    /// <summary>The setting that turns on following a link on a network path to this PC.</summary>
    public const string RemoteToLocal = "SymlinkRemoteToLocalEvaluation";

    /// <summary>The setting that turns on following a link on a network path to another network path.</summary>
    public const string RemoteToRemote = "SymlinkRemoteToRemoteEvaluation";

    /// <summary>Every symbolic link setting this reader was asked for, in order.</summary>
    public List<(string Key, string Value)> LinkSettingReads { get; } = new();

    /// <summary>One of the four symbolic link settings reads as <paramref name="read"/>.</summary>
    public void HoldsLinkSetting(string keyPath, string valueName, RegistryDwordRead read) =>
        _linkSettings[(keyPath, valueName)] = read;

    internal ScriptedSourceListRegistry(ScriptedMsiProducts msi) => _msi = msi;

    /// <summary>Every key path this reader was asked for, in order, spelled as asked.</summary>
    public List<string> Reads { get; } = new();

    /// <summary>The key is there and holds these values.</summary>
    public void Holds(string keyPath, params RegistryValue[] values) =>
        _keys[keyPath] = new RegistryKeyValues(RegistryKeyPresence.Present, values);

    /// <summary>The key is not there, or will not read.</summary>
    public void Answers(string keyPath, RegistryKeyPresence presence) =>
        _keys[keyPath] = new RegistryKeyValues(presence);

    public RegistryKeyValues LocalMachineValues(string keyPath)
    {
        Reads.Add(keyPath);
        return _keys.TryGetValue(keyPath, out var scripted) ? scripted : _msi.MirroredKey(keyPath);
    }

    public RegistryKeyPresence LocalMachineKeyPresence(string relativePath) =>
        throw new InvalidOperationException($"the declared-product check reads values, and was asked whether {relativePath} is there");

    public RegistryMultiStringRead LocalMachineMultiStringValue(string keyPath, string valueName) =>
        throw new InvalidOperationException($"the declared-product check reads no string array, and was asked for {keyPath} {valueName}");

    public RegistryDwordRead LocalMachineDwordValue(string keyPath, string valueName)
    {
        if (keyPath is not (LinkSettingsKey or LinkPolicyKey) || valueName is not (RemoteToLocal or RemoteToRemote))
            throw new InvalidOperationException(
                $"the declared-product check reads no number but the symbolic link settings, and was asked for {keyPath} {valueName}");

        LinkSettingReads.Add((keyPath, valueName));
        return _linkSettings.TryGetValue((keyPath, valueName), out var scripted)
            ? scripted
            : new RegistryDwordRead(RegistryDwordState.Absent);
    }
}
