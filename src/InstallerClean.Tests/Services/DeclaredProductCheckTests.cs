using System.Globalization;
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
/// Installer folder or the file. For a patch the same two: a POSITIVE answer that
/// Windows holds no registration of the declared patch, and every registration of it
/// recording a cached copy that is present and is another file, with none of them per
/// user and unmanaged and no source of the patch reaching the Installer folder or the
/// file. Either way, every source list read has to be held in the registry as the API
/// returns it and hold no URL. Every inability keeps the file.
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

        var outcomes = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []);

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

        var outcomes = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []);

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

        var outcomes = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []);

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

        var outcomes = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []);

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

        var outcomes = new DeclaredProductCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []);

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

        var outcomes = new DeclaredProductCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []);

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

        var outcomes = new DeclaredProductCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []);

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

        var outcomes = new DeclaredProductCheck(msi, identities).Screen(new[]
        {
            Patch(@"C:\Windows\Installer\p.msp"),
            Package(@"C:\Windows\Installer\a.msi"),
        }, []);

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

        var outcomes = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []);

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

        var outcomes = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, []);

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

        var outcomes = new DeclaredProductCheck(msi, identities).Screen(new[]
        {
            Package(@"C:\Windows\Installer\gone.msi"),
            Package(@"C:\Windows\Installer\unreadable.msi"),
            Package(@"C:\Windows\Installer\held.msi"),
        }, []);

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

        var outcomes = new DeclaredProductCheck(msi, identities).Screen(new[]
        {
            Package(@"C:\Windows\Installer\v1.msi"),
            Package(@"C:\Windows\Installer\v2.msi"),
            Package(@"C:\Windows\Installer\v3.msi"),
        }, []);

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
            new DeclaredProductCheck(new ScriptedMsiProducts(), new ScriptedPackageIdentities())
                .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, [], cts.Token));
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
        new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Package(Candidate) }, installations ?? [], default, null,
                namesAFileInInstallerFolder ?? InInstallerFolder)[0];

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
    public void A_copy_is_kept_when_one_installation_records_no_package()
    {
        // The per-machine installation records another file; the per-user one records
        // nothing, so the package that installation opens cannot be seen and this copy
        // could be it.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.Installed(ProductA,
            (null, MsiInstallContext.Machine),
            (UserSid, MsiInstallContext.UserManaged));
        f.Msi.RecordsPackage(ProductA, UserSid, MsiInstallContext.UserManaged, "");

        var outcome = ScreenTheCopy(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.True(outcome.Withholds());
    }

    [Fact]
    public void A_copy_is_kept_when_the_only_installation_records_no_package()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsPackage(ProductA, null, MsiInstallContext.Machine, "");

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    [Fact]
    public void A_copy_is_kept_when_the_record_carries_no_package_property_at_all()
    {
        // ERROR_UNKNOWN_PROPERTY is a record that never carried the value. It reads as
        // an empty value rather than a failure, and an empty value names no package.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.PackageReadAnswers(ProductA, null, MsiInstallContext.Machine, MsiError.UnknownProperty);

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
    public void A_copy_is_kept_when_the_copy_itself_will_not_identify()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Files.Answers(Candidate, FileIdentityRead.IdentityUnavailable);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
    }

    // ---- The installation's sources ----
    //
    // When Windows Installer needs a product's original package rather than its
    // cached copy, it looks for the package name in the folders on the product's
    // source list. A copy in the Installer folder that such a source can reach is
    // kept, and so is one whose sources cannot be ruled out. Each keeping test is
    // the fixture above, which lets the copy through, with one thing changed.

    [Fact]
    public void A_copy_is_kept_when_its_product_was_installed_from_the_Installer_folder()
    {
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, "c.msi", InstallerFolder + @"\");

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f));
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
    public void A_copy_is_kept_when_whether_a_source_is_in_the_Installer_folder_is_not_established()
    {
        var f = ACopyBesideTheRecordedPackage();

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, ScreenTheCopy(f, _ => null));
    }

    [Fact]
    public void A_copy_is_kept_when_the_screen_has_no_Installer_folder_to_compare_against()
    {
        var f = ACopyBesideTheRecordedPackage();

        var outcome = new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Package(Candidate) }, [])[0];

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
    public void A_copy_is_kept_when_the_second_source_on_its_list_is_the_Installer_folder()
    {
        // The first source holds no package, so the list's first entry alone would let
        // the copy through. The second is the Installer folder.
        var asked = new List<string>();
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsSources(ProductA, null, MsiInstallContext.Machine, SetupName,
            SetupFolder, InstallerFolder + @"\");

        var outcome = ScreenTheCopy(f, path =>
        {
            asked.Add(path);
            return InInstallerFolder(path);
        });

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.Contains(InstallerFolder + @"\" + SetupName, asked);
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

    [Fact]
    public void A_copy_is_kept_when_the_source_used_last_is_on_no_list()
    {
        // The registry holds it as the API answers it, so only its place keeps the copy.
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.ListProperty(ProductA, isPatch: false, null, MsiInstallContext.Machine,
            MsiInstallProperty.LastUsedSource, InstallerFolder + @"\");

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

        var outcome = new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk)
            .Screen(new[] { Package(Candidate) }, [], default, null, InInstallerFolder)[0];

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

        var outcome = new DeclaredProductCheck(f.Msi, f.Packages)
            .Screen(new[] { Package(Candidate) }, [])[0];

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

        var outcomes = new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Package(Candidate), Package(SecondCopy) }, [], default, null, InInstallerFolder);

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
    public void A_copy_is_kept_when_the_folder_its_product_was_installed_from_is_the_Installer_folder()
    {
        var asked = new List<string>();
        var f = ACopyBesideTheRecordedPackage();
        f.Msi.RecordsInstallSource(ProductA, null, MsiInstallContext.Machine, InstallerFolder + @"\");

        var outcome = ScreenTheCopy(f, path =>
        {
            asked.Add(path);
            return InInstallerFolder(path);
        });

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcome);
        Assert.Contains(InstallerFolder + @"\" + SetupName, asked);
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
    [InlineData(@"\\server\setup\")]
    [InlineData(@"D:\Other")]
    public void A_copy_is_let_through_when_the_folder_its_product_was_installed_from_holds_no_package(string folder)
    {
        // A folder on a drive and one on a network share, and the drive's folder without
        // its closing '\'.
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
    // value each registration records. A copy in the folder that no such value names,
    // and that no source of the patch reaches, is let through only when EVERY
    // registration of the patch records a copy that is present, is another file and
    // declares the same patch. Each test after the first is the fixture with one thing
    // changed, and every one of them keeps the file. The sources have their own tests
    // further down.

    private const string PatchQ = "{33333333-3333-3333-3333-333333333333}";
    private const string PatchR = "{44444444-4444-4444-4444-444444444444}";
    private const string PatchCopy = @"C:\Windows\Installer\copy.msp";
    private const string RecordedPatch = @"C:\Windows\Installer\cached.msp";
    private const string PatchSetupName = "fix.msp";
    private const string PatchSetupPackage = @"D:\Setup\fix.msp";

    /// <summary>
    /// Patch Q, declaring product A as its target, registered against A's one
    /// per-machine installation and recording <see cref="RecordedPatch"/>, with both
    /// files on disk as two different files that both declare patch Q, and applied from
    /// <see cref="PatchSetupPackage"/>, which is no longer there. The machine-wide patch
    /// enumeration lists that registration. Each test changes one thing.
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
        msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, PatchSetupName, SetupFolder);

        var files = new ScriptedFileIdentities();
        files.Opens(PatchCopy, 1);
        files.Opens(RecordedPatch, 2);
        files.Answers(PatchSetupPackage, FileIdentityRead.NamesNothing);

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
        new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Patch(PatchCopy) }, installations ?? [], default, null,
                namesAFileInInstallerFolder ?? InInstallerFolder)[0];

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
        // And the patch's source was looked at, finding no package there.
        Assert.Contains(PatchSetupPackage, f.Files.Reads);
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
    public void A_patch_copy_is_kept_when_the_copy_itself_will_not_identify()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Files.Answers(PatchCopy, FileIdentityRead.IdentityUnavailable);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_a_second_registration_records_no_copy()
    {
        // Product B holds patch Q as well, for a user, and records nothing, so the copy
        // that registration opens cannot be seen and this copy could be it. B is not in
        // the patch's own target list: the machine-wide enumeration is what names it.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.HoldsPatch(PatchQ, ProductB, UserSid, MsiInstallContext.UserManaged);
        f.Msi.RecordsPatchPackage(PatchQ, ProductB, UserSid, MsiInstallContext.UserManaged, "");

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
        f.Msi.RecordsPatchSources(PatchQ, UserSid, MsiInstallContext.UserManaged, PatchSetupName, SetupFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, ScreenThePatchCopy(f));
        Assert.Equal(2, f.Msi.PatchPackageReads.Count);
        Assert.Single(f.Files.Reads, p => p == RecordedPatch);
    }

    [Fact]
    public void A_patch_copy_is_kept_when_a_registration_is_per_user_unmanaged_whatever_its_source_list_holds()
    {
        // The copy that is let through above, with the second registration per user and
        // unmanaged. It records the same present copy, and the patch's source list there,
        // were it read, points only at a folder holding no package. The source list in
        // that context is not read, whichever account it is in, so what that
        // registration could open cannot be ruled out and this copy is kept.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.HoldsPatch(PatchQ, ProductB, UserSid, MsiInstallContext.UserUnmanaged);
        f.Msi.RecordsPatchPackage(PatchQ, ProductB, UserSid, MsiInstallContext.UserUnmanaged, RecordedPatch);
        f.Msi.RecordsPatchSources(PatchQ, UserSid, MsiInstallContext.UserUnmanaged, PatchSetupName, SetupFolder);

        var outcome = ScreenThePatchCopy(f);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, outcome);
        Assert.True(outcome.Withholds());
        // The machine's list was read and the user's was not.
        var machineOnly = new[] { (PatchQ, (string?)null, MsiInstallContext.Machine) };
        Assert.Equal(machineOnly, f.Msi.PatchPackageNameReads);
        Assert.Equal(machineOnly, f.Msi.PatchSourceListWalks);
    }

    [Fact]
    public void A_patch_registration_in_a_user_account_is_read_in_that_account()
    {
        // The only registration is per user. Its copy and the patch's source list are
        // asked for in that account and context, and the fake answers nothing else, so
        // a read in any other place fails the test rather than being answered.
        var packages = new ScriptedPackageIdentities();
        packages.DeclaresPatch(PatchCopy, PatchQ, ProductA);
        packages.DeclaresPatch(RecordedPatch, PatchQ, ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA, (UserSid, MsiInstallContext.UserManaged));
        msi.HoldsPatch(PatchQ, ProductA, UserSid, MsiInstallContext.UserManaged);
        msi.RecordsPatchPackage(PatchQ, ProductA, UserSid, MsiInstallContext.UserManaged, RecordedPatch);
        msi.RecordsPatchSources(PatchQ, UserSid, MsiInstallContext.UserManaged, PatchSetupName, SetupFolder);

        var files = new ScriptedFileIdentities();
        files.Opens(PatchCopy, 1);
        files.Opens(RecordedPatch, 2);
        files.Answers(PatchSetupPackage, FileIdentityRead.NamesNothing);

        var disk = new MockFileSystem();
        disk.AddFile(PatchCopy, new MockFileData(new byte[100]));
        disk.AddFile(RecordedPatch, new MockFileData(new byte[100]));

        var outcome = ScreenThePatchCopy((packages, msi, files, disk));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, outcome);
        Assert.Equal(
            new[] { (PatchQ, ProductA, (string?)UserSid, MsiInstallContext.UserManaged) },
            msi.PatchPackageReads);
        Assert.Equal(new[] { (PatchQ, (string?)UserSid, MsiInstallContext.UserManaged) }, msi.PatchPackageNameReads);
        Assert.Equal(new[] { (PatchQ, (string?)UserSid, MsiInstallContext.UserManaged) }, msi.PatchSourceListWalks);
    }

    // ---- The patch's sources ----
    //
    // A patch has a source list and a package name of its own in each account and
    // context holding a registration of it. A patch copy in the Installer folder that
    // such a source can reach is kept, and so is one whose sources cannot be ruled out.
    // Each keeping test is the patch fixture above, which lets the copy through, with
    // one thing changed.

    [Fact]
    public void A_patch_copy_is_kept_when_its_patch_was_applied_from_it_in_the_Installer_folder()
    {
        // The patch's source is the Installer folder and its package name is this
        // copy's own name, so the source list names this file and nothing else does.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, "copy.msp", InstallerFolder + @"\");

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_its_patch_was_applied_from_another_file_in_the_Installer_folder()
    {
        // A source in the Installer folder keeps every copy of the patch there, not only
        // the one its package name names, and whether or not that file is still there.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, "applied.msp", InstallerFolder + @"\");
        f.Files.Answers(InstallerFolder + @"\applied.msp", FileIdentityRead.NamesNothing);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_a_source_package_is_the_copy_itself()
    {
        // A source outside the folder whose package opens as this file.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Files.Opens(PatchSetupPackage, 1);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_whether_a_source_is_in_the_Installer_folder_is_not_established()
    {
        var f = APatchCopyBesideTheRecordedCopy();

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f, _ => null));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_screen_has_no_Installer_folder_to_compare_against()
    {
        var f = APatchCopyBesideTheRecordedCopy();

        var outcome = new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Patch(PatchCopy) }, [])[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, outcome);
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_source_list_will_not_read()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.PatchSourceListAnswers(PatchQ, null, MsiInstallContext.Machine, MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_source_list_does_not_end()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.PatchSourceListNeverEnds(PatchQ, null, MsiInstallContext.Machine);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_a_source_entry_of_the_patch_is_empty()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, PatchSetupName, "");

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_a_source_entry_of_the_patch_holds_a_variable_that_is_not_set()
    {
        // Read as it stands, the path finds no file and would be skipped.
        const string Variable = "INSTALLERCLEAN_TEST_UNSET_SOURCE";
        Assert.Null(Environment.GetEnvironmentVariable(Variable));
        var entry = $@"%{Variable}%\Setup\";
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, PatchSetupName, entry);
        f.Files.Answers(entry + PatchSetupName, FileIdentityRead.NamesNothing);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_package_name_will_not_read()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.PatchPackageNameAnswers(PatchQ, null, MsiInstallContext.Machine, MsiError.AccessDenied);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_source_list_carries_no_package_name()
    {
        // ERROR_UNKNOWN_PROPERTY is the source list not carrying the property. It reads
        // as an empty value, and an empty name names no package at any source.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.PatchPackageNameAnswers(PatchQ, null, MsiInstallContext.Machine, MsiError.UnknownProperty);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_package_name_is_empty()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, "", SetupFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_a_source_package_of_the_patch_will_not_identify()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Files.Answers(PatchSetupPackage, FileIdentityRead.OpenRefused);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_let_through_when_its_source_package_is_another_file()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Files.Opens(PatchSetupPackage, 9);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, ScreenThePatchCopy(f));
        Assert.Contains(PatchSetupPackage, f.Files.Reads);
    }

    [Fact]
    public void A_patch_copy_is_let_through_when_its_patch_records_no_network_source()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, PatchSetupName);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_let_through_when_both_sources_on_the_patch_s_list_are_ruled_out()
    {
        // The first source holds no patch package and the second holds another file, so
        // the copy is ruled out only by reading the list to its end.
        const string MediaPatch = @"E:\Media\fix.msp";
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, PatchSetupName,
            SetupFolder, MediaFolder);
        f.Files.Opens(MediaPatch, 9);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, ScreenThePatchCopy(f));
        Assert.Contains(MediaPatch, f.Files.Reads);
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_source_list_in_another_account_reaches_the_folder()
    {
        // Patch Q is registered per machine against product A, and against product B for
        // two users, each in the managed per-user context. All three record the same
        // cached copy. The machine's list and the first user's are ruled out; the second
        // user's names a file in the Installer folder, so the copy is kept. The list is
        // read per account, and the first user's answer does not stand for the second's.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.HoldsPatch(PatchQ, ProductB, UserSid, MsiInstallContext.UserManaged);
        f.Msi.RecordsPatchPackage(PatchQ, ProductB, UserSid, MsiInstallContext.UserManaged, RecordedPatch);
        f.Msi.RecordsPatchSources(PatchQ, UserSid, MsiInstallContext.UserManaged, PatchSetupName, SetupFolder);
        f.Msi.HoldsPatch(PatchQ, ProductB, OtherUserSid, MsiInstallContext.UserManaged);
        f.Msi.RecordsPatchPackage(PatchQ, ProductB, OtherUserSid, MsiInstallContext.UserManaged, RecordedPatch);
        f.Msi.RecordsPatchSources(PatchQ, OtherUserSid, MsiInstallContext.UserManaged, "applied.msp",
            InstallerFolder + @"\");

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
        Assert.Contains((PatchQ, (string?)OtherUserSid, MsiInstallContext.UserManaged), f.Msi.PatchSourceListWalks);
    }

    [Fact]
    public void A_patch_copy_is_kept_when_one_account_holds_it_unmanaged_as_well_as_managed()
    {
        // Patch Q is registered per machine against product A, and against product B for
        // one user twice: in the managed per-user context, whose list is read and ruled
        // out, and then in the unmanaged one. The source lists are told apart by context
        // as well as by account, so the second registration is not taken as the first's
        // list already read, and its context keeps the copy.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.HoldsPatch(PatchQ, ProductB, UserSid, MsiInstallContext.UserManaged);
        f.Msi.RecordsPatchPackage(PatchQ, ProductB, UserSid, MsiInstallContext.UserManaged, RecordedPatch);
        f.Msi.RecordsPatchSources(PatchQ, UserSid, MsiInstallContext.UserManaged, PatchSetupName, SetupFolder);
        f.Msi.HoldsPatch(PatchQ, ProductB, UserSid, MsiInstallContext.UserUnmanaged);
        f.Msi.RecordsPatchPackage(PatchQ, ProductB, UserSid, MsiInstallContext.UserUnmanaged, RecordedPatch);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
        Assert.Contains((PatchQ, (string?)UserSid, MsiInstallContext.UserManaged), f.Msi.PatchSourceListWalks);
    }

    [Fact]
    public void A_patch_s_source_list_is_read_once_in_each_account_and_context_holding_it()
    {
        // Patch Q is registered per machine against products A and B, and for one user
        // against A and against B, the second time with the account spelled in lower
        // case. The source-list calls take the patch code, an account and a context and
        // no product, so there are two lists here, each read once.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.HoldsPatch(PatchQ, ProductB, null, MsiInstallContext.Machine);
        f.Msi.RecordsPatchPackage(PatchQ, ProductB, null, MsiInstallContext.Machine, RecordedPatch);
        f.Msi.HoldsPatch(PatchQ, ProductA, UserSid, MsiInstallContext.UserManaged);
        f.Msi.RecordsPatchPackage(PatchQ, ProductA, UserSid, MsiInstallContext.UserManaged, RecordedPatch);
        f.Msi.RecordsPatchSources(PatchQ, UserSid, MsiInstallContext.UserManaged, PatchSetupName, SetupFolder);
        f.Msi.HoldsPatch(PatchQ, ProductB, UserSid.ToLowerInvariant(), MsiInstallContext.UserManaged);
        f.Msi.RecordsPatchPackage(PatchQ, ProductB, UserSid.ToLowerInvariant(), MsiInstallContext.UserManaged,
            RecordedPatch);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, ScreenThePatchCopy(f));
        var lists = new[]
        {
            (PatchQ, (string?)null, MsiInstallContext.Machine),
            (PatchQ, (string?)UserSid, MsiInstallContext.UserManaged),
        };
        Assert.Equal(lists, f.Msi.PatchPackageNameReads);
        Assert.Equal(lists, f.Msi.PatchSourceListWalks);
        Assert.Equal(4, f.Msi.PatchPackageReads.Count);
    }

    [Fact]
    public void A_patch_copy_is_kept_when_a_source_entry_of_the_patch_holds_a_variable_that_is_set()
    {
        const string Variable = "INSTALLERCLEAN_TEST_SET_PATCH_SOURCE";
        var entry = $@"%{Variable}%\";
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, PatchSetupName, entry);
        f.Files.Answers(entry + PatchSetupName, FileIdentityRead.NamesNothing);

        Environment.SetEnvironmentVariable(Variable, @"D:\Setup");
        try
        {
            Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
        }
        finally
        {
            Environment.SetEnvironmentVariable(Variable, null);
        }
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_package_name_names_more_than_a_file()
    {
        const string Name = @"Windows\Installer\fix.msp";
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsPatchSources(PatchQ, null, MsiInstallContext.Machine, Name, SetupFolder);
        f.Files.Answers(SetupFolder + Name, FileIdentityRead.NamesNothing);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    // ---- The registry key holding the patch's list ----
    //
    // The same comparison as a product's, through the same code. These pin the patch's
    // own keys and that each rule reaches a patch.

    private const string PatchQKey =
        @"SOFTWARE\Classes\Installer\Patches\33333333333333333333333333333333\SourceList";

    [Fact]
    public void A_patch_copy_is_let_through_when_the_registry_holds_the_patch_s_list_the_API_returned()
    {
        var f = APatchCopyBesideTheRecordedCopy();

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, ScreenThePatchCopy(f));
        Assert.Equal(
            new[] { PatchQKey, PatchQKey + @"\Net", PatchQKey + @"\URL", PatchQKey + @"\Media" },
            f.Msi.Registry.Reads);
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_list_key_holds_an_entry_past_a_gap()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.Registry.Holds(PatchQKey + @"\Net",
            ExpandSz("1", SetupFolder), ExpandSz("3", InstallerFolder + @"\"));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_list_key_holds_other_text_than_the_API_returned()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.Registry.Holds(PatchQKey + @"\Net", ExpandSz("1", @"D:\Other\"));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_list_key_holds_another_package_name()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.Registry.Holds(PatchQKey,
            Sz(MsiInstallProperty.PackageName, @"Windows\Installer\" + PatchSetupName),
            ExpandSz(MsiInstallProperty.LastUsedSource, "n;1;" + SetupFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_list_key_holds_no_package_name()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.Registry.Holds(PatchQKey, ExpandSz(MsiInstallProperty.LastUsedSource, "n;1;" + SetupFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_list_key_holds_the_package_name_as_a_REG_EXPAND_SZ()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.Registry.Holds(PatchQKey,
            ExpandSz(MsiInstallProperty.PackageName, PatchSetupName),
            ExpandSz(MsiInstallProperty.LastUsedSource, "n;1;" + SetupFolder));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_list_holds_a_URL()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.RecordsUrls(PatchQ, isPatch: true, null, MsiInstallContext.Machine, "http://localhost/fix/");

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_URL_key_holds_an_entry_the_API_did_not_return()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.Registry.Holds(PatchQKey + @"\URL", ExpandSz("1", "file:///C:/Windows/Installer/"));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Theory]
    [InlineData(RegistryKeyPresence.Absent)]
    [InlineData(RegistryKeyPresence.Unreadable)]
    public void A_patch_copy_is_kept_when_the_patch_s_source_list_key_is_not_read(RegistryKeyPresence presence)
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.Registry.Answers(PatchQKey, presence);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_source_used_last_is_on_no_list()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.ListProperty(PatchQ, isPatch: true, null, MsiInstallContext.Machine,
            MsiInstallProperty.LastUsedSource, InstallerFolder + @"\");

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_registry_holds_no_source_used_last_for_the_patch_while_the_API_answers_one()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.Registry.Holds(PatchQKey, Sz(MsiInstallProperty.PackageName, PatchSetupName));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_registry_holds_the_patch_s_source_used_last_otherwise()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.Registry.Holds(PatchQKey,
            Sz(MsiInstallProperty.PackageName, PatchSetupName),
            ExpandSz(MsiInstallProperty.LastUsedSource, @"n;1;C:\Windows\Installer\"));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_list_names_a_media_package_path()
    {
        // The registry's Media key names none, so only the API's answer keeps the copy.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.ListProperty(PatchQ, isPatch: true, null, MsiInstallContext.Machine,
            MsiInstallProperty.MediaPackagePath, "disk1");
        f.Msi.Registry.Holds(PatchQKey + @"\Media", Sz("1", ";"));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void A_patch_copy_is_kept_when_the_patch_s_media_key_holds_a_media_package_path()
    {
        // The API answers none, so only the registry's value keeps the copy.
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.Registry.Holds(PatchQKey + @"\Media", Sz("1", ";"), Sz("MediaPackage", "disk1"));

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, ScreenThePatchCopy(f));
    }

    [Fact]
    public void Without_the_registry_reader_a_patch_copy_is_kept_and_no_key_is_read()
    {
        var f = APatchCopyBesideTheRecordedCopy();

        var outcome = new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk)
            .Screen(new[] { Patch(PatchCopy) }, [], default, null, InInstallerFolder)[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, outcome);
        Assert.Empty(f.Msi.Registry.Reads);
    }

    [Fact]
    public void A_patch_s_list_in_a_user_account_is_read_under_that_account()
    {
        var f = APatchCopyBesideTheRecordedCopy();
        f.Msi.HoldsPatch(PatchQ, ProductB, UserSid, MsiInstallContext.UserManaged);
        f.Msi.RecordsPatchPackage(PatchQ, ProductB, UserSid, MsiInstallContext.UserManaged, RecordedPatch);
        f.Msi.RecordsPatchSources(PatchQ, UserSid, MsiInstallContext.UserManaged, PatchSetupName, SetupFolder);

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, ScreenThePatchCopy(f));
        Assert.Contains(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Managed\S-1-5-21-9-9-9-1001\Installer\Patches\"
            + @"33333333333333333333333333333333\SourceList",
            f.Msi.Registry.Reads);
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

        var outcome = new DeclaredProductCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, [])[0];

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

        var outcome = new DeclaredProductCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, [])[0];

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

        var outcome = new DeclaredProductCheck(msi, packages, new ScriptedFileIdentities(), new MockFileSystem(), msi.Registry)
            .Screen(new[] { Patch(PatchCopy) }, [])[0];

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

        var outcomes = new DeclaredProductCheck(msi, packages, new ScriptedFileIdentities(), new MockFileSystem(), msi.Registry)
            .Screen(new[] { Patch(PatchCopy), Patch(OtherCopy) }, []);

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

        var outcomes = new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Patch(PatchCopy), Patch(SecondCopy) }, [], default, null, InInstallerFolder);

        Assert.All(outcomes, o => Assert.Equal(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, o));
        Assert.Equal(1, f.Msi.PatchEnumerations);
        Assert.Single(f.Msi.Asked);
        Assert.Single(f.Msi.PatchPackageReads);
        Assert.Single(f.Msi.PatchPackageNameReads);
        Assert.Single(f.Msi.PatchSourceListWalks);
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

        var outcomes = new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Patch(PatchCopy), Patch(OtherPatchCopy) }, [], default, null, InInstallerFolder);

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
        // file readers. The copy is kept, and neither the recorded copy, the patch's
        // source list nor any file's identity is read.
        var f = APatchCopyBesideTheRecordedCopy();

        var outcome = new DeclaredProductCheck(f.Msi, f.Packages)
            .Screen(new[] { Patch(PatchCopy) }, [])[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchRegistered, outcome);
        Assert.Empty(f.Msi.PatchPackageReads);
        Assert.Empty(f.Msi.PatchPackageNameReads);
        Assert.Empty(f.Msi.PatchSourceListWalks);
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

        var outcome = new DeclaredProductCheck(new ScriptedMsiProducts(), packages)
            .Screen(new[] { Patch(PatchCopy) }, [])[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcome);
        Assert.True(outcome.Withholds());
        Assert.Equal(new[] { PatchCopy }, packages.PatchReads);
    }

    [Fact]
    public void A_patch_reading_with_an_empty_code_is_kept_back()
    {
        var packages = new ScriptedPackageIdentities();
        packages.Yields(PatchCopy, new PackageIdentity(string.Empty, IsPatch: true, new[] { ProductA }));

        var outcome = new DeclaredProductCheck(new ScriptedMsiProducts(), packages)
            .Screen(new[] { Patch(PatchCopy) }, [])[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcome);
    }

    [Fact]
    public void A_patch_reading_that_comes_back_as_a_product_is_kept_back()
    {
        // The reading names a product, so the only thing stopping it is that it is not
        // marked as a patch.
        var packages = new ScriptedPackageIdentities();
        packages.Yields(PatchCopy, new PackageIdentity(PatchQ, IsPatch: false, new[] { ProductA }));

        var outcome = new DeclaredProductCheck(new ScriptedMsiProducts(), packages)
            .Screen(new[] { Patch(PatchCopy) }, [])[0];

        Assert.Equal(DeclaredProductOutcome.DeclaredPatchUnestablished, outcome);
    }

    [Fact]
    public void A_patch_reading_that_names_no_target_is_kept_back()
    {
        // With no product named, there is no installation to put the keyed question to.
        var packages = new ScriptedPackageIdentities();
        packages.Yields(PatchCopy, new PackageIdentity(PatchQ, IsPatch: true, Array.Empty<string>()));

        var outcome = new DeclaredProductCheck(new ScriptedMsiProducts(), packages)
            .Screen(new[] { Patch(PatchCopy) }, [])[0];

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

        var outcome = new DeclaredProductCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, [])[0];

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

        var outcome = new DeclaredProductCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, [])[0];

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

        var outcomes = new DeclaredProductCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy), Package(ThePackage), Patch(OtherPatchCopy) }, []);

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

        var outcomes = new DeclaredProductCheck(new ScriptedMsiProducts(), packages)
            .Screen(new[] { Patch(PatchCopy) }, [],
                recordRefusal: (ex, cause) => recorded.Add((ex, cause)));

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

        var outcomes = new DeclaredProductCheck(new ScriptedMsiProducts(), packages)
            .Screen(new[] { Patch(EmptyCode), Patch(AsAProduct), Patch(NoTarget) }, [],
                recordRefusal: (ex, _) => recorded.Add(ex));

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

        var outcome = new DeclaredProductCheck(f.Msi, f.Packages, f.Files, f.Disk, f.Msi.Registry)
            .Screen(new[] { Patch(PatchCopy) }, [], default, (ex, _) => recorded.Add(ex), InInstallerFolder)[0];

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
        new(productCode, null, (int)MsiInstallContext.Machine);

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

        var outcome = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, [ListedPerMachine(ProductA)])[0];

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcome);
        Assert.True(outcome.Withholds());
    }

    [Fact]
    public void A_package_whose_product_the_caller_did_not_list_is_answered_not_installed_as_before()
    {
        // The caller listed product B, so an answer that product A is not installed
        // contradicts nothing.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.UnknownProduct);

        var outcome = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, [ListedPerMachine(ProductB)])[0];

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

        var outcome = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") },
                [ListedPerMachine(LetteredProduct.ToLowerInvariant())])[0];

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
            new ListedInstallation(ProductA, UserSid, (int)MsiInstallContext.UserManaged),
        ]);

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcome);
        Assert.Empty(f.Msi.PackageReads);
    }

    [Fact]
    public void A_copy_is_let_through_when_the_answer_holds_every_installation_the_caller_listed()
    {
        // The test above with product A answering both installations. The account is
        // listed in lower case, and is the same account.
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

        var outcome = ScreenTheCopy(f, installations:
        [
            ListedPerMachine(ProductA),
            new ListedInstallation(ProductA, UserSid.ToLowerInvariant(), (int)MsiInstallContext.UserManaged),
        ]);

        Assert.Equal(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, outcome);
        Assert.Equal(2, f.Msi.PackageReads.Count);
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

        var outcome = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") },
                [new ListedInstallation(ProductA, UserSid, (int)MsiInstallContext.UserManaged)])[0];

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

        var outcome = new DeclaredProductCheck(msi, packages)
            .Screen(new[] { Patch(PatchCopy) }, [ListedPerMachine(ProductA)])[0];

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
            new ListedInstallation(ProductA, UserSid, (int)MsiInstallContext.UserManaged),
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

        var outcomes = new DeclaredProductCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, [],
                recordRefusal: (ex, cause) => recorded.Add((ex, cause)));

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

        var outcomes = new DeclaredProductCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, [],
                recordRefusal: (ex, _) => recorded.Add(ex));

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

        new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, [],
                recordRefusal: (ex, _) => recorded.Add(ex));

        Assert.Empty(recorded);
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
        _byPath[path] = new PackageIdentity(productCode, IsPatch: false, Array.Empty<string>());

    /// <summary>The file is a patch declaring its own code and the products it may be applied to.</summary>
    public void DeclaresPatch(string path, string patchCode, params string[] targets) =>
        _byPath[path] = new PackageIdentity(patchCode, IsPatch: true, targets);

    public void Yields(string path, PackageIdentity identity) => _byPath[path] = identity;

    /// <summary>
    /// The file would not give up an identity at all. The note is what the real
    /// reader writes to say WHICH of its refusals this was, and it is the thing the
    /// screen is meant to pass on rather than drop.
    /// </summary>
    public void YieldsNothing(string path, string note = "")
    {
        _byPath[path] = null;
        _notes[path] = note;
    }

    public PackageIdentity? Read(string filePath, bool isPatch, out string detail)
    {
        Reads.Add(filePath);
        if (isPatch) PatchReads.Add(filePath);
        detail = _notes.TryGetValue(filePath, out var note) ? note : string.Empty;
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

    /// <summary>
    /// Answers LocalPackage, PackageName and InstallSource, the three product properties
    /// the check reads, with the real API's two-call shape: a null buffer is answered
    /// with the length, a buffer with the value.
    ///
    /// AN UNSCRIPTED INSTALLATION THROWS. A recorded package that is present and is
    /// another file is the answer that lets a file through, so a fake inventing one
    /// would let a test assert an offer nothing established.
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
                _ => throw new InvalidOperationException(
                    "the declared-product check reads LocalPackage, PackageName and InstallSource, "
                    + $"and was asked for {property}"),
            };

            if (!table.TryGetValue((productCode, userSid, context), out scripted))
                throw new InvalidOperationException(
                    $"the fake was asked for the {property} {productCode} records for {userSid ?? "the machine"} "
                    + $"in {context}, which no test scripted");
        }

        if (value is null && property == MsiInstallProperty.LocalPackage) PackageReads.Add((productCode, userSid, context));
        if (value is null && property == MsiInstallProperty.PackageName) PackageNameReads.Add((productCode, userSid, context));
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

    /// <summary>What reading a patch's PackageName in one account and context returns instead of a value.</summary>
    public void PatchPackageNameAnswers(string patchCode, string? sid, MsiInstallContext context, uint error) =>
        _patchPackageNames[(patchCode, sid, context)] = (error, string.Empty);

    /// <summary>What reading a patch's source list in one account and context returns instead of an entry.</summary>
    public void PatchSourceListAnswers(string patchCode, string? sid, MsiInstallContext context, uint error) =>
        _patchSources[(patchCode, sid, context)] = new SourceList(Array.Empty<string>(), error);

    /// <summary>A patch source list whose every index answers with another folder, and which never ends.</summary>
    public void PatchSourceListNeverEnds(string patchCode, string? sid, MsiInstallContext context) =>
        _patchSources[(patchCode, sid, context)] = new SourceList(new[] { @"D:\Somewhere\" }, Endless: true);

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
    /// shape: PackageName for a patch, a product's being read through
    /// <see cref="GetProductInfo"/>, and for either kind the three
    /// <see cref="ListProperty"/> names. Anything else throws.
    ///
    /// AN UNSCRIPTED PATCH OR LIST THROWS. A package name naming no file at any source is
    /// an answer that lets a patch copy through, so a fake inventing one would let a test
    /// assert an offer nothing established.
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
        if (property == MsiInstallProperty.PackageName)
        {
            if (!isPatch)
                throw new InvalidOperationException(
                    "the declared-product check reads a product's PackageName through MsiGetProductInfoEx, "
                    + "and was asked for it off the source list");

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
/// A scripted <see cref="IRegistryReader"/> for the keys holding source lists and each
/// installation's InstallSource. By default each key holds what the
/// <see cref="ScriptedMsiProducts"/> it belongs to was scripted with, so the registry and
/// the API agree (<see cref="ScriptedMsiProducts.MirroredKey"/>).
/// A key a test scripts here answers as scripted instead, which is how a test makes the
/// two disagree. The scripted keys are matched by their exact spelling, so a test
/// scripting one also pins the path the check reads.
///
/// A PATH NAMING NO SCRIPTED LIST THROWS, and so does every read other than a key's
/// values.
/// </summary>
internal sealed class ScriptedSourceListRegistry : IRegistryReader
{
    private readonly ScriptedMsiProducts _msi;
    private readonly Dictionary<string, RegistryKeyValues> _keys = new(StringComparer.Ordinal);

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

    public RegistryDwordRead LocalMachineDwordValue(string keyPath, string valueName) =>
        throw new InvalidOperationException($"the declared-product check reads no number, and was asked for {keyPath} {valueName}");
}
