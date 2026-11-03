using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;
using Microsoft.Win32;
using Xunit;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The last step of <see cref="DeclaredProductCheck.Screen"/>: a file the check would let
/// through, put to the product keys no installation accounts for.
///
/// EVERY FIXTURE SETS UP THE REAL SHAPE. The key is handed in, the keyed walk answers
/// ERROR_NO_MORE_ITEMS for its code, and every registry read the check makes is scripted,
/// so the outcome rests on what the key holds and not on a default of the fake.
/// </summary>
public class DeclaredProductCheckUnlistedKeyTests
{
    private const string System = "S-1-5-18";
    private const string User = "S-1-5-21-9-9-9-1001";
    private const string KeyCode = "{11111111-1111-1111-1111-111111111111}";
    private const string OtherCode = "{22222222-2222-2222-2222-222222222222}";
    private const string ThirdCode = "{33333333-3333-3333-3333-333333333333}";
    private const string PatchCode = "{44444444-4444-4444-4444-444444444444}";
    private const string Folder = @"C:\Windows\Installer\";
    private const string First = @"C:\Windows\Installer\a.msi";
    private const string Second = @"C:\Windows\Installer\c.msi";
    private const string Cached = @"C:\Windows\Installer\b.msi";
    private const string Spare = @"C:\Windows\Installer\s.msp";
    private const string Setup = @"D:\Setup\";
    private const string SetupName = "setup.msi";
    private const string SetupPackage = @"D:\Setup\setup.msi";
    private const string Share = @"\\server\share\";

    private static OrphanedFile Package(string path) =>
        new(path, 100, IsPatch: false, IsRemovablePatch: false, IsObsoleted: false, Reason: "orphaned");

    private static OrphanedFile Patch(string path) =>
        new(path, 100, IsPatch: true, IsRemovablePatch: false, IsObsoleted: false, Reason: "orphaned");

    private static bool? InInstallerFolder(string path) =>
        path.StartsWith(Folder, StringComparison.OrdinalIgnoreCase)
        && path.IndexOf('\\', Folder.Length) < 0;

    private static string Packed(string code) => InstallerQueryService.PackRegistryCode(code)!;

    private static string MachineList(string code) => $@"SOFTWARE\Classes\Installer\Products\{Packed(code)}\SourceList";

    private static string HiveList(string account, string code) =>
        ScriptedSourceListRegistry.InHive(account, $@"Software\Microsoft\Installer\Products\{Packed(code)}\SourceList");

    private static string ManagedList(string account, string code) =>
        $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Managed\{account}\Installer\Products\{Packed(code)}\SourceList";

    private static string Properties(string account, string code) =>
        $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\{account}\Products\{Packed(code)}\InstallProperties";

    private static UnlistedProductKey Installed(string account = System, string code = KeyCode, params string[] cached) =>
        new(account, code, [], cached, HasInstallProperties: true);

    private static UnlistedProductKey NoProperties(string account = System, string code = KeyCode) =>
        new(account, code, [], [], HasInstallProperties: false);

    /// <summary>
    /// A PC where <see cref="First"/> and <see cref="Second"/> are two different files declaring
    /// <see cref="OtherCode"/> and <see cref="ThirdCode"/>, neither of which Windows lists, and
    /// the key's account has its hive loaded and no source list anywhere.
    /// </summary>
    private sealed class Pc
    {
        public ScriptedPackageIdentities Packages { get; } = new();
        public ScriptedMsiProducts Msi { get; } = new();
        public ScriptedFileIdentities Files { get; } = new();
        public MockFileSystem Disk { get; } = new();
        public ScriptedSourceListRegistry Registry => Msi.Registry;

        /// <summary>The test for the Installer folder, which the scan hands every pass.</summary>
        public Func<string, bool?> IsTheInstallerFolder { get; set; } = folder => folder == Folder;

        public Pc(string account = System)
        {
            Packages.Declares(First, OtherCode);
            Packages.Declares(Second, ThirdCode);
            Msi.NotInstalled(OtherCode, MsiError.NoMoreItems);
            Msi.NotInstalled(ThirdCode, MsiError.NoMoreItems);
            Msi.NotInstalled(KeyCode, MsiError.NoMoreItems);
            Files.Opens(First, 1);
            Files.Opens(Second, 2);
            Disk.AddFile(First, new MockFileData(new byte[100]));
            Disk.AddFile(Second, new MockFileData(new byte[100]));

            Registry.HiveIs(account, RegistryKeyPresence.Present);
            if (account == System) Registry.Answers(MachineList(KeyCode), RegistryKeyPresence.Absent);
            Registry.Answers(HiveList(account, KeyCode), RegistryKeyPresence.Absent);
            Registry.Answers(ManagedList(account, KeyCode), RegistryKeyPresence.Absent);
            Registry.Answers(Properties(account, KeyCode), RegistryKeyPresence.Absent);
        }

        /// <summary>The source list at <paramref name="list"/>, as a real one reads.</summary>
        public void HasList(string list, string packageName, string folder)
        {
            Registry.Holds(
                list,
                new RegistryValue("PackageName", RegistryValueKind.String, packageName),
                new RegistryValue("LastUsedSource", RegistryValueKind.ExpandString, "n;1;" + folder));
            Registry.Holds(list + @"\Net", new RegistryValue("1", RegistryValueKind.ExpandString, folder));
            Registry.Answers(list + @"\URL", RegistryKeyPresence.Absent);
            Registry.Holds(list + @"\Media", new RegistryValue("1", RegistryValueKind.String, ";"));
        }

        /// <summary>The key's <c>InstallProperties</c> key, recording this <c>InstallSource</c>.</summary>
        public void InstalledFrom(
            string folder, string account = System, RegistryValueKind kind = RegistryValueKind.String) =>
            Registry.Holds(Properties(account, KeyCode), new RegistryValue("InstallSource", kind, folder));

        public void CachedPackageDeclares(string code)
        {
            Packages.Declares(Cached, code);
            Files.Opens(Cached, 3);
        }

        public DeclaredProductOutcome[] Screen(IReadOnlyList<UnlistedProductKey> keys, params OrphanedFile[] files) =>
            ScreeningOf(keys, files).Outcomes.ToArray();

        public DeclaredProductScreening ScreeningOf(
            IReadOnlyList<UnlistedProductKey> keys, OrphanedFile[] files, Action<Exception, string>? recordRefusal = null) =>
            new DeclaredProductCheck(Msi, Packages, Files, Disk, Registry, null)
            {
                DriveKindOf = _ => DriveType.Fixed,
                NamesInFolderOf = path => [path[(path.LastIndexOf('\\') + 1)..]],
            }.Screen(files, [], keys, default, recordRefusal, InInstallerFolder, isTheInstallerFolder: IsTheInstallerFolder);
    }

    private const DeclaredProductOutcome LetThrough = DeclaredProductOutcome.DeclaredProductNotInstalled;

    // ---------------------------------------------------------------- with no keys

    [Fact]
    public void With_no_key_every_file_is_let_through_and_no_source_is_read()
    {
        var pc = new Pc();

        Assert.Equal([LetThrough, LetThrough], pc.Screen([], Package(First), Package(Second)));
        Assert.Empty(pc.Registry.Reads);
    }

    [Fact]
    public void A_pass_of_patches_reads_no_key_source()
    {
        var pc = new Pc();
        pc.Packages.DeclaresPatch(Spare, PatchCode, OtherCode);
        pc.Msi.HoldsNoPatches();

        Assert.Equal(
            [DeclaredProductOutcome.DeclaredPatchNotRegistered],
            pc.Screen([Installed()], Patch(Spare)));
        Assert.DoesNotContain(MachineList(KeyCode), pc.Registry.Reads);
    }

    [Fact]
    public void A_keys_sources_are_read_once_however_many_files_are_let_through()
    {
        var pc = new Pc();

        Assert.Equal([LetThrough, LetThrough], pc.Screen([Installed()], Package(First), Package(Second)));
        Assert.Single(pc.Registry.Reads, MachineList(KeyCode));
    }

    // ---------------------------------------------------------------- a key with InstallProperties

    [Fact]
    public void A_file_declaring_a_keys_code_is_kept_and_another_is_let_through()
    {
        var pc = new Pc();
        pc.Packages.Declares(First, KeyCode);

        Assert.Equal(
            [DeclaredProductOutcome.Unestablished, LetThrough],
            pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_file_declaring_what_a_keys_cached_package_declares_is_kept()
    {
        var pc = new Pc();
        pc.CachedPackageDeclares(OtherCode);

        Assert.Equal(
            [DeclaredProductOutcome.Unestablished, LetThrough],
            pc.Screen([Installed(System, KeyCode, Cached)], Package(First), Package(Second)));
    }

    [Fact]
    public void A_code_listed_under_one_account_with_a_second_key_under_another_keeps_the_file()
    {
        // The listed installation alone lets the file through as another file than its cached
        // package. The key under the other account is what keeps it.
        var pc = new Pc(User);
        pc.Packages.Declares(First, KeyCode);
        pc.CachedPackageDeclares(KeyCode);
        pc.Disk.AddFile(Cached, new MockFileData(new byte[100]));
        pc.Msi.Installed(KeyCode);
        pc.Msi.RecordsPackage(KeyCode, null, MsiInstallContext.Machine, Cached);
        pc.Msi.RecordsSources(KeyCode, null, MsiInstallContext.Machine, SetupName, Setup);
        pc.Files.Answers(SetupPackage, FileIdentityRead.NamesNothing);

        Assert.Equal(
            DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile,
            pc.Screen([], Package(First))[0]);
        Assert.Equal(
            DeclaredProductOutcome.Unestablished,
            pc.Screen([Installed(User)], Package(First))[0]);
    }

    [Fact]
    public void A_file_opening_as_the_package_at_a_keys_source_is_kept_per_machine_and_another_is_let_through()
    {
        var pc = new Pc();
        pc.HasList(MachineList(KeyCode), SetupName, Setup);
        pc.Files.Opens(SetupPackage, 1);
        pc.Files.Answers(Folder + SetupName, FileIdentityRead.NamesNothing);

        Assert.Equal(
            [DeclaredProductOutcome.Unestablished, LetThrough],
            pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_file_opening_as_the_package_at_a_keys_source_is_kept_per_user()
    {
        var pc = new Pc(User);
        pc.HasList(HiveList(User, KeyCode), SetupName, Setup);
        pc.Files.Opens(SetupPackage, 1);
        pc.Files.Answers(Folder + SetupName, FileIdentityRead.NamesNothing);

        Assert.Equal(
            [DeclaredProductOutcome.Unestablished, LetThrough],
            pc.Screen([Installed(User)], Package(First), Package(Second)));
    }

    [Fact]
    public void A_cached_package_that_does_not_say_keeps_every_file_as_a_second_copy()
    {
        var pc = new Pc();
        pc.Files.Answers(Cached, FileIdentityRead.NamesNothing);

        Assert.Equal(
            [DeclaredProductOutcome.SecondCopyUnestablished, DeclaredProductOutcome.SecondCopyUnestablished],
            pc.Screen([Installed(System, KeyCode, Cached)], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_recording_a_cached_package_whose_sources_will_not_read_keeps_every_file()
    {
        var pc = new Pc();
        pc.CachedPackageDeclares(ThirdCode);
        pc.Packages.Declares(Second, OtherCode);
        pc.Registry.Answers(MachineList(KeyCode), RegistryKeyPresence.Unreadable);

        Assert.Equal(
            [DeclaredProductOutcome.SecondCopyUnestablished, DeclaredProductOutcome.SecondCopyUnestablished],
            pc.Screen([Installed(System, KeyCode, Cached)], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_recording_no_cached_package_whose_sources_will_not_read_keeps_only_a_file_declaring_its_code()
    {
        var pc = new Pc();
        pc.Packages.Declares(First, KeyCode);
        pc.Registry.Answers(MachineList(KeyCode), RegistryKeyPresence.Unreadable);

        Assert.Equal(
            [DeclaredProductOutcome.Unestablished, LetThrough],
            pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_keeping_every_file_is_logged_once_for_the_pass_with_why()
    {
        var pc = new Pc();
        pc.Files.Answers(Cached, FileIdentityRead.NamesNothing);
        var recorded = new List<(Exception Error, string Detail)>();

        var check = pc.ScreeningOf(
            [Installed(System, KeyCode, Cached)], [Package(First), Package(Second)],
            (ex, detail) => recorded.Add((ex, detail)));

        Assert.Equal(
            [DeclaredProductOutcome.SecondCopyUnestablished, DeclaredProductOutcome.SecondCopyUnestablished],
            check.Outcomes);
        var entry = Assert.Single(recorded);
        Assert.Equal("the cached package is not a file that is there", entry.Detail);
        Assert.Contains("every installation package is kept rather than offered", entry.Error.Message);
        Assert.DoesNotContain(Cached, entry.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_key_whose_sources_will_not_read_is_logged_as_that()
    {
        var pc = new Pc();
        pc.CachedPackageDeclares(ThirdCode);
        pc.Packages.Declares(Second, OtherCode);
        pc.Registry.Answers(MachineList(KeyCode), RegistryKeyPresence.Unreadable);
        var recorded = new List<string>();

        pc.ScreeningOf(
            [Installed(System, KeyCode, Cached)], [Package(First), Package(Second)],
            (_, detail) => recorded.Add(detail));

        Assert.Equal(["a source list could not be read, its key not reading or its account's hive not loaded"], recorded);
    }

    [Fact]
    public void A_key_whose_cached_package_will_not_read_is_logged_with_the_readers_detail()
    {
        var pc = new Pc();
        pc.Files.Opens(Cached, 3);
        pc.Packages.YieldsNothing(Cached, "the Property table would not open");
        var recorded = new List<string>();

        pc.ScreeningOf(
            [Installed(System, KeyCode, Cached)], [Package(First), Package(Second)],
            (_, detail) => recorded.Add(detail));

        Assert.Equal(["the Property table would not open"], recorded);
    }

    [Fact]
    public void A_key_whose_cached_package_declares_no_code_is_logged_as_that()
    {
        var pc = new Pc();
        pc.Files.Opens(Cached, 3);
        pc.Packages.DeclaresNoCode(Cached);
        var recorded = new List<string>();

        pc.ScreeningOf(
            [Installed(System, KeyCode, Cached)], [Package(First), Package(Second)],
            (_, detail) => recorded.Add(detail));

        Assert.Equal(["the cached package declares no product code"], recorded);
    }

    [Fact]
    public void A_key_whose_InstallProperties_will_not_read_is_logged_as_that()
    {
        var pc = new Pc();
        pc.Registry.Answers(Properties(System, KeyCode), RegistryKeyPresence.Unreadable);
        var recorded = new List<string>();

        var check = pc.ScreeningOf([Installed()], [Package(First), Package(Second)], (_, detail) => recorded.Add(detail));

        Assert.Equal(
            [DeclaredProductOutcome.SecondCopyUnestablished, DeclaredProductOutcome.SecondCopyUnestablished],
            check.Outcomes);
        Assert.Equal(
            ["the key's InstallProperties key would not read, or holds a cached package value the scan read no "
             + "path from"],
            recorded);
    }

    [Fact]
    public void A_key_recording_no_cached_package_installed_from_the_Installer_folder_is_logged_as_that()
    {
        var pc = new Pc();
        pc.InstalledFrom(Folder);
        var recorded = new List<string>();

        pc.ScreeningOf([Installed()], [Package(First), Package(Second)], (_, detail) => recorded.Add(detail));

        Assert.Equal(
            ["the key records no cached package, no source list that read names its package, and the folder it "
             + "was installed from is the Installer folder"],
            recorded);
    }

    [Fact]
    public void A_key_with_no_list_installed_from_the_Installer_folder_is_logged_as_that()
    {
        var pc = new Pc();
        pc.CachedPackageDeclares(ThirdCode);
        pc.Packages.Declares(Second, OtherCode);
        pc.InstalledFrom(Folder);
        var recorded = new List<string>();

        pc.ScreeningOf(
            [Installed(System, KeyCode, Cached)], [Package(First), Package(Second)],
            (_, detail) => recorded.Add(detail));

        Assert.Equal(
            ["the key has no source list, and the folder it was installed from is the Installer folder"], recorded);
    }

    [Fact]
    public void A_key_with_no_list_whose_install_folder_the_test_cannot_answer_is_logged_as_not_ruled_out()
    {
        var pc = new Pc();
        pc.CachedPackageDeclares(ThirdCode);
        pc.Packages.Declares(Second, OtherCode);
        pc.InstalledFrom(Setup);
        pc.IsTheInstallerFolder = _ => null;
        var recorded = new List<string>();

        pc.ScreeningOf(
            [Installed(System, KeyCode, Cached)], [Package(First), Package(Second)],
            (_, detail) => recorded.Add(detail));

        Assert.Equal(
            ["the key has no source list, and the folder it was installed from could not be ruled out as the "
             + "Installer folder"],
            recorded);
    }

    [Fact]
    public void A_key_recording_a_cached_package_whose_InstallProperties_will_not_read_is_logged_as_that()
    {
        var pc = new Pc();
        pc.CachedPackageDeclares(ThirdCode);
        pc.Packages.Declares(Second, OtherCode);
        pc.Registry.Answers(Properties(System, KeyCode), RegistryKeyPresence.Unreadable);
        var recorded = new List<string>();

        pc.ScreeningOf(
            [Installed(System, KeyCode, Cached)], [Package(First), Package(Second)],
            (_, detail) => recorded.Add(detail));

        Assert.Equal(["the key's InstallProperties key would not read"], recorded);
    }

    [Fact]
    public void A_key_whose_InstallSource_names_a_variable_is_logged_as_a_form_not_compared()
    {
        var pc = new Pc();
        pc.CachedPackageDeclares(ThirdCode);
        pc.Packages.Declares(Second, OtherCode);
        pc.InstalledFrom(@"%SystemRoot%\Installer\");
        var recorded = new List<string>();

        pc.ScreeningOf(
            [Installed(System, KeyCode, Cached)], [Package(First), Package(Second)],
            (_, detail) => recorded.Add(detail));

        Assert.Equal(["the key's InstallSource is in a form the check does not compare"], recorded);
    }

    [Fact]
    public void A_key_whose_list_holds_a_folder_naming_a_variable_is_logged_as_a_form_not_compared()
    {
        var pc = new Pc();
        pc.CachedPackageDeclares(ThirdCode);
        pc.Packages.Declares(Second, OtherCode);
        pc.HasList(MachineList(KeyCode), SetupName, @"%SystemRoot%\Installer\");
        var recorded = new List<string>();

        pc.ScreeningOf(
            [Installed(System, KeyCode, Cached)], [Package(First), Package(Second)],
            (_, detail) => recorded.Add(detail));

        Assert.Equal(["a source list holds a value in a form the check does not compare"], recorded);
    }

    [Fact]
    public void A_cached_package_that_is_there_and_will_not_identify_is_logged_as_that()
    {
        var pc = new Pc();
        pc.Files.Answers(Cached, FileIdentityRead.OpenRefused);
        var recorded = new List<string>();

        pc.ScreeningOf(
            [Installed(System, KeyCode, Cached)], [Package(First), Package(Second)],
            (_, detail) => recorded.Add(detail));

        Assert.Equal(["the cached package could not be identified"], recorded);
    }

    [Fact]
    public void An_Installer_folder_hold_on_an_expanded_InstallSource_names_no_source_read()
    {
        var pc = new Pc();
        pc.InstalledFrom(Folder, kind: RegistryValueKind.ExpandString);
        var recorded = new List<string>();

        pc.ScreeningOf([Installed()], [Package(First), Package(Second)], (_, detail) => recorded.Add(detail));

        Assert.Equal(
            ["the key records no cached package, no source list that read names its package, and the folder it "
             + "was installed from is the Installer folder"],
            recorded);
    }

    [Fact]
    public void An_Installer_folder_hold_after_a_list_that_would_not_read_names_both()
    {
        var pc = new Pc();
        pc.InstalledFrom(Folder);
        pc.Registry.Answers(MachineList(KeyCode), RegistryKeyPresence.Unreadable);
        var recorded = new List<string>();

        pc.ScreeningOf([Installed()], [Package(First), Package(Second)], (_, detail) => recorded.Add(detail));

        Assert.Equal(
            ["the key records no cached package, no source list that read names its package, and the folder it "
             + "was installed from is the Installer folder; also, a source list could not be read, its key not "
             + "reading or its account's hive not loaded"],
            recorded);
    }

    [Fact]
    public void A_key_that_shows_its_package_logs_nothing()
    {
        var pc = new Pc();
        pc.CachedPackageDeclares(ThirdCode);
        pc.Packages.Declares(Second, OtherCode);
        var recorded = new List<string>();

        pc.ScreeningOf(
            [Installed(System, KeyCode, Cached)], [Package(First), Package(Second)],
            (_, detail) => recorded.Add(detail));

        Assert.Empty(recorded);
    }

    [Fact]
    public void An_account_whose_hive_is_not_loaded_cannot_have_its_sources_read()
    {
        var pc = new Pc(User);
        pc.Registry.HiveIs(User, RegistryKeyPresence.Absent);
        pc.CachedPackageDeclares(ThirdCode);
        pc.Packages.Declares(Second, OtherCode);

        Assert.Equal(
            [DeclaredProductOutcome.SecondCopyUnestablished, DeclaredProductOutcome.SecondCopyUnestablished],
            pc.Screen([Installed(User, KeyCode, Cached)], Package(First), Package(Second)));
        Assert.DoesNotContain(HiveList(User, KeyCode), pc.Registry.Reads);
    }

    [Fact]
    public void An_unloaded_hive_holds_nothing_for_a_key_recording_no_cached_package()
    {
        var pc = new Pc(User);
        pc.Registry.HiveIs(User, RegistryKeyPresence.Absent);

        Assert.Equal([LetThrough, LetThrough], pc.Screen([Installed(User)], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_with_no_list_and_an_InstallSource_in_the_Installer_folder_holds_when_it_records_a_cached_package()
    {
        var pc = new Pc();
        pc.CachedPackageDeclares(ThirdCode);
        pc.Packages.Declares(Second, OtherCode);
        pc.InstalledFrom(Folder);

        var check = pc.ScreeningOf([Installed(System, KeyCode, Cached)], [Package(First), Package(Second)]);

        Assert.Equal(
            [DeclaredProductOutcome.SecondCopyUnestablished, DeclaredProductOutcome.SecondCopyUnestablished],
            check.Outcomes);
    }

    // ---------------------------------------------------------------- recording no cached package

    private static readonly DeclaredProductOutcome[] EveryFileKept =
        [DeclaredProductOutcome.SecondCopyUnestablished, DeclaredProductOutcome.SecondCopyUnestablished];

    private static readonly DeclaredProductOutcome[] NothingKept = [LetThrough, LetThrough];

    [Fact]
    public void A_key_installed_from_the_Installer_folder_with_no_list_keeps_every_file()
    {
        var pc = new Pc();
        pc.InstalledFrom(Folder);

        var check = pc.ScreeningOf([Installed()], [Package(First), Package(Second)]);

        Assert.Equal(EveryFileKept, check.Outcomes);
        Assert.Empty(check.RootsGivenUp);
    }

    [Fact]
    public void A_key_installed_from_the_Installer_folder_whose_list_will_not_read_keeps_every_file()
    {
        var pc = new Pc();
        pc.InstalledFrom(Folder);
        pc.Registry.Answers(MachineList(KeyCode), RegistryKeyPresence.Unreadable);

        Assert.Equal(EveryFileKept, pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_installed_from_the_Installer_folder_whose_list_holds_a_name_no_file_can_carry_keeps_every_file()
    {
        var pc = new Pc();
        pc.InstalledFrom(Folder);
        pc.HasList(MachineList(KeyCode), "...", Setup);

        Assert.Equal(EveryFileKept, pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_installed_from_the_Installer_folder_under_an_unloaded_hive_keeps_every_file()
    {
        var pc = new Pc(User);
        pc.Registry.HiveIs(User, RegistryKeyPresence.Absent);
        pc.InstalledFrom(Folder, User);

        Assert.Equal(EveryFileKept, pc.Screen([Installed(User)], Package(First), Package(Second)));
    }

    [Fact]
    public void A_file_declaring_the_code_of_a_key_installed_from_the_Installer_folder_is_kept_for_that_code()
    {
        var pc = new Pc();
        pc.Packages.Declares(First, KeyCode);
        pc.InstalledFrom(Folder);

        Assert.Equal(
            [DeclaredProductOutcome.Unestablished, DeclaredProductOutcome.SecondCopyUnestablished],
            pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_installed_from_the_Installer_folder_whose_list_names_a_package_keeps_that_package_alone()
    {
        var pc = new Pc();
        pc.InstalledFrom(Folder);
        pc.HasList(MachineList(KeyCode), "a.msi", Setup);
        pc.Files.Answers(Setup + "a.msi", FileIdentityRead.NamesNothing);

        Assert.Equal(
            [DeclaredProductOutcome.Unestablished, LetThrough],
            pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_installed_from_another_folder_keeps_nothing()
    {
        var pc = new Pc();
        pc.InstalledFrom(Setup);

        Assert.Equal(NothingKept, pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_whose_InstallSource_the_folder_test_does_not_answer_for_keeps_nothing()
    {
        var pc = new Pc { IsTheInstallerFolder = _ => null };
        pc.InstalledFrom(Folder);

        Assert.Equal(NothingKept, pc.Screen([Installed()], Package(First), Package(Second)));
    }

    /// <summary>A variable of this test's own, so the expansion does not rest on the host.</summary>
    private const string RootVariable = "IC_UNLISTED_KEY_TEST_ROOT";

    private static DeclaredProductOutcome[] ScreenExpanding(RegistryValueKind kind, string installSource)
    {
        Environment.SetEnvironmentVariable(RootVariable, @"C:\Windows");
        try
        {
            var pc = new Pc();
            pc.InstalledFrom(installSource, kind: kind);
            return pc.Screen([Installed()], Package(First), Package(Second));
        }
        finally
        {
            Environment.SetEnvironmentVariable(RootVariable, null);
        }
    }

    [Theory]
    [InlineData(RegistryValueKind.String, "%" + RootVariable + @"%\Installer\")]
    [InlineData(RegistryValueKind.ExpandString, "%" + RootVariable + @"%\Installer\")]
    [InlineData(RegistryValueKind.ExpandString, Folder)]
    public void A_key_whose_InstallSource_expands_to_the_Installer_folder_keeps_every_file(
        RegistryValueKind kind, string installSource) =>
        Assert.Equal(EveryFileKept, ScreenExpanding(kind, installSource));

    [Theory]
    [InlineData(RegistryValueKind.String, "%" + RootVariable + @"%\Other\")]
    [InlineData(RegistryValueKind.ExpandString, Setup)]
    [InlineData(RegistryValueKind.String, @"%IC_UNLISTED_KEY_TEST_NOT_SET%\Installer\")]
    public void A_key_whose_InstallSource_does_not_expand_to_the_Installer_folder_keeps_nothing(
        RegistryValueKind kind, string installSource) =>
        Assert.Equal(NothingKept, ScreenExpanding(kind, installSource));

    [Fact]
    public void A_key_whose_InstallSource_expands_to_the_Installer_folder_keeps_the_package_its_list_names()
    {
        Environment.SetEnvironmentVariable(RootVariable, @"C:\Windows");
        try
        {
            var pc = new Pc();
            pc.InstalledFrom("%" + RootVariable + @"%\Installer\");
            pc.HasList(MachineList(KeyCode), "a.msi", Setup);
            pc.Registry.Answers(MachineList(KeyCode) + @"\Media", RegistryKeyPresence.Absent);
            pc.Files.Answers(Setup + "a.msi", FileIdentityRead.NamesNothing);

            Assert.Equal(
                [DeclaredProductOutcome.Unestablished, LetThrough],
                pc.Screen([Installed()], Package(First), Package(Second)));
        }
        finally
        {
            Environment.SetEnvironmentVariable(RootVariable, null);
        }
    }

    [Fact]
    public void An_InstallSource_expanding_to_no_drive_or_share_is_not_put_to_the_folder_test()
    {
        var pc = new Pc { IsTheInstallerFolder = _ => true };
        pc.InstalledFrom(@"Installer\", kind: RegistryValueKind.ExpandString);

        Assert.Equal(NothingKept, pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_recording_a_cached_package_with_a_list_that_will_not_read_asks_nothing_about_its_InstallSource()
    {
        var asked = 0;
        var pc = new Pc
        {
            IsTheInstallerFolder = _ =>
            {
                asked++;
                return false;
            },
        };
        pc.CachedPackageDeclares(ThirdCode);
        pc.Packages.Declares(Second, OtherCode);
        pc.InstalledFrom(Setup);
        pc.HasList(MachineList(KeyCode), "...", Setup);

        Assert.Equal(EveryFileKept, pc.Screen([Installed(System, KeyCode, Cached)], Package(First), Package(Second)));
        Assert.Equal(0, asked);
    }

    [Fact]
    public void A_key_with_no_InstallProperties_keeps_nothing_for_an_InstallSource()
    {
        var pc = new Pc();
        pc.InstalledFrom(Folder);

        Assert.Equal(NothingKept, pc.Screen([NoProperties()], Package(First), Package(Second)));
    }

    [Theory]
    [InlineData("LocalPackage", RegistryValueKind.String, "")]
    [InlineData("ManagedLocalPackage", RegistryValueKind.String, "")]
    [InlineData("LocalPackage", RegistryValueKind.DWord, null)]
    public void A_key_holding_a_cached_package_value_with_no_path_in_it_keeps_every_file(
        string name, RegistryValueKind kind, string? text)
    {
        var pc = new Pc();
        pc.Registry.Holds(Properties(System, KeyCode), new RegistryValue(name, kind, text));

        Assert.Equal(EveryFileKept, pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_whose_InstallProperties_will_not_read_keeps_every_file()
    {
        var pc = new Pc();
        pc.Registry.Answers(Properties(System, KeyCode), RegistryKeyPresence.Unreadable);

        Assert.Equal(EveryFileKept, pc.Screen([Installed()], Package(First), Package(Second)));
    }

    // ---------------------------------------------------------------- names, asked of the filesystem

    [Theory]
    [InlineData("安装.msi")]
    [InlineData("установка.msi")]
    [InlineData("SETUP~1.MSI")]
    [InlineData(SetupName)]
    public void A_package_name_opening_nothing_beside_a_file_keeps_nothing_whatever_it_spells(string name)
    {
        // Every real list has a Media entry, so the name reaches the check. A name outside
        // ASCII could be any file by a comparison of names, and only the filesystem can say.
        var pc = new Pc();
        pc.HasList(MachineList(KeyCode), name, Setup);
        pc.Files.Answers(Setup + name, FileIdentityRead.NamesNothing);
        pc.Files.Answers(Folder + name, FileIdentityRead.NamesNothing);

        Assert.Equal([LetThrough, LetThrough], pc.Screen([Installed()], Package(First), Package(Second)));
        Assert.Contains(Folder + name, pc.Files.Reads);
    }

    [Fact]
    public void A_key_recording_a_cached_package_is_not_held_class_wide_by_a_name_outside_ASCII()
    {
        const string name = "安装.msi";
        var pc = new Pc();
        pc.CachedPackageDeclares(KeyCode);
        pc.HasList(MachineList(KeyCode), name, Setup);
        pc.Files.Answers(Setup + name, FileIdentityRead.NamesNothing);
        pc.Files.Answers(Folder + name, FileIdentityRead.NamesNothing);

        Assert.Equal(
            [LetThrough, LetThrough],
            pc.Screen([Installed(System, KeyCode, Cached)], Package(First), Package(Second)));
    }

    [Fact]
    public void A_file_carrying_a_package_name_is_kept_and_another_is_let_through()
    {
        const string name = "安装.msi";
        var pc = new Pc();
        pc.HasList(MachineList(KeyCode), name, Setup);
        pc.Files.Answers(Setup + name, FileIdentityRead.NamesNothing);
        pc.Files.Opens(Folder + name, 2);

        Assert.Equal(
            [LetThrough, DeclaredProductOutcome.Unestablished],
            pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_name_the_filesystem_will_not_settle_keeps_each_file_it_was_asked_for_alone()
    {
        const string name = "安装.msi";
        var pc = new Pc();
        pc.HasList(MachineList(KeyCode), name, Setup);
        pc.Files.Answers(Setup + name, FileIdentityRead.NamesNothing);
        pc.Files.Answers(Folder + name, FileIdentityRead.OpenRefused);

        var kept = pc.Screen([Installed()], Package(First));

        Assert.Equal([DeclaredProductOutcome.Unestablished], kept);
    }

    [Fact]
    public void A_remote_folder_on_a_list_keeps_nothing_beyond_the_file_carrying_its_name()
    {
        const string name = "安装.msi";
        var pc = new Pc();
        pc.HasList(MachineList(KeyCode), name, Share);
        pc.Files.Answers(Folder + name, FileIdentityRead.NamesNothing);

        Assert.Equal([LetThrough, LetThrough], pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_folder_that_does_not_settle_leaves_the_next_folder_read_and_keeps_a_file_carrying_the_name()
    {
        // No cached package, so nothing is held class-wide, and what the key names still keeps
        // a file: the package in the second folder by identity, and the file carrying the name.
        const string name = "pkg.msi";
        var pc = new Pc();
        pc.Registry.Holds(
            MachineList(KeyCode),
            new RegistryValue("PackageName", RegistryValueKind.String, name));
        pc.Registry.Holds(
            MachineList(KeyCode) + @"\Net",
            new RegistryValue("1", RegistryValueKind.ExpandString, @"D:\One\"),
            new RegistryValue("2", RegistryValueKind.ExpandString, Setup));
        pc.Registry.Answers(MachineList(KeyCode) + @"\URL", RegistryKeyPresence.Absent);
        pc.Registry.Answers(MachineList(KeyCode) + @"\Media", RegistryKeyPresence.Absent);
        pc.Files.Answers(@"D:\One\" + name, FileIdentityRead.OpenRefused);
        pc.Files.Opens(Setup + name, 1);
        pc.Files.Opens(Folder + name, 2);

        Assert.Equal(
            [DeclaredProductOutcome.Unestablished, DeclaredProductOutcome.Unestablished],
            pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void An_InstallSource_in_a_form_not_compared_leaves_the_keys_list_read()
    {
        var pc = new Pc();
        pc.Registry.Holds(
            Properties(System, KeyCode),
            new RegistryValue("InstallSource", RegistryValueKind.String, "%TEMP%"));
        pc.HasList(MachineList(KeyCode), SetupName, Setup);
        pc.Files.Opens(SetupPackage, 1);
        pc.Files.Answers(Folder + SetupName, FileIdentityRead.NamesNothing);

        Assert.Equal(
            [DeclaredProductOutcome.Unestablished, LetThrough],
            pc.Screen([Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_package_name_is_read_once_beside_a_folder_of_files()
    {
        var pc = new Pc();
        pc.HasList(MachineList(KeyCode), SetupName, Setup);
        pc.Files.Answers(SetupPackage, FileIdentityRead.NamesNothing);
        pc.Files.Answers(Folder + SetupName, FileIdentityRead.NamesNothing);

        pc.Screen([Installed()], Package(First), Package(Second));

        Assert.Single(pc.Files.Reads, Folder + SetupName);
    }

    // A name no file can carry is refused as a form, so no read of a file's folder and that
    // name can keep every file.
    [Theory]
    [InlineData("...")]
    [InlineData(". .")]
    [InlineData("se*up.msi")]
    [InlineData("se?up.msi")]
    [InlineData("se<up.msi")]
    [InlineData("se|up.msi")]
    [InlineData("a/b.msi")]
    [InlineData("%TEMP%.msi")]
    public void A_name_no_file_can_carry_holds_nothing_for_a_key_recording_no_cached_package(string name)
    {
        var pc = new Pc();
        pc.Registry.Holds(MachineList(KeyCode), new RegistryValue("PackageName", RegistryValueKind.String, name));

        Assert.Equal([LetThrough, LetThrough], pc.Screen([Installed()], Package(First), Package(Second)));
        Assert.DoesNotContain(Folder + name, pc.Files.Reads);
    }

    [Theory]
    [InlineData("...")]
    [InlineData("se*up.msi")]
    public void A_name_no_file_can_carry_is_a_list_that_will_not_read_for_a_key_recording_a_cached_package(string name)
    {
        var pc = new Pc();
        pc.CachedPackageDeclares(ThirdCode);
        pc.Packages.Declares(Second, OtherCode);
        pc.Registry.Holds(MachineList(KeyCode), new RegistryValue("PackageName", RegistryValueKind.String, name));

        Assert.Equal(
            [DeclaredProductOutcome.SecondCopyUnestablished, DeclaredProductOutcome.SecondCopyUnestablished],
            pc.Screen([Installed(System, KeyCode, Cached)], Package(First), Package(Second)));
    }

    // ---------------------------------------------------------------- a key with no InstallProperties

    [Fact]
    public void A_file_declaring_the_code_of_a_key_holding_Features_alone_is_let_through()
    {
        var pc = new Pc();
        pc.Packages.Declares(First, KeyCode);

        Assert.Equal(
            [LetThrough, LetThrough],
            pc.Screen([NoProperties()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_with_no_InstallProperties_keeps_the_package_at_a_local_folder_on_its_list_by_identity()
    {
        var pc = new Pc();
        pc.HasList(HiveList(System, KeyCode), SetupName, Folder);
        pc.Files.Opens(Folder + SetupName, 1);

        Assert.Equal(
            [DeclaredProductOutcome.Unestablished, LetThrough],
            pc.Screen([NoProperties()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_with_no_InstallProperties_keeps_nothing_by_a_name()
    {
        const string name = "安装.msi";
        var pc = new Pc();
        pc.HasList(HiveList(System, KeyCode), name, Setup);
        pc.Files.Answers(Setup + name, FileIdentityRead.NamesNothing);

        Assert.Equal([LetThrough, LetThrough], pc.Screen([NoProperties()], Package(First), Package(Second)));
        Assert.DoesNotContain(Folder + name, pc.Files.Reads);
    }

    [Fact]
    public void A_key_with_no_InstallProperties_keeps_nothing_where_its_list_will_not_read()
    {
        var pc = new Pc();
        pc.Registry.Answers(HiveList(System, KeyCode), RegistryKeyPresence.Unreadable);

        Assert.Equal([LetThrough, LetThrough], pc.Screen([NoProperties()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_with_no_InstallProperties_keeps_nothing_where_the_read_of_its_package_does_not_settle()
    {
        var pc = new Pc();
        pc.HasList(HiveList(System, KeyCode), SetupName, Setup);
        pc.Files.Answers(SetupPackage, FileIdentityRead.OpenRefused);

        Assert.Equal([LetThrough, LetThrough], pc.Screen([NoProperties()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_key_with_no_InstallProperties_reads_its_package_by_the_rules_of_every_source_read()
    {
        // Only the source read asks a volume of a path that gives no file id.
        var pc = new Pc();
        pc.HasList(HiveList(System, KeyCode), SetupName, Setup);
        pc.Files.Answers(SetupPackage, FileIdentityRead.IdentityUnavailable);

        pc.Screen([NoProperties()], Package(First));

        Assert.Contains(SetupPackage, pc.Files.VolumeReads);
    }

    [Fact]
    public void A_key_with_no_InstallProperties_keeps_nothing_at_a_remote_folder()
    {
        var pc = new Pc();
        pc.HasList(HiveList(System, KeyCode), SetupName, Share);

        Assert.Equal([LetThrough, LetThrough], pc.Screen([NoProperties()], Package(First), Package(Second)));
        Assert.DoesNotContain(Share + SetupName, pc.Files.Reads);
    }

    [Fact]
    public void A_key_with_no_InstallProperties_under_an_unloaded_hive_keeps_nothing()
    {
        var pc = new Pc(User);
        pc.Registry.HiveIs(User, RegistryKeyPresence.Absent);

        Assert.Equal([LetThrough, LetThrough], pc.Screen([NoProperties(User)], Package(First), Package(Second)));
    }

    // ---------------------------------------------------------------- the spare patch

    private static Pc PcWithASparePatch()
    {
        var pc = new Pc();
        pc.Packages.DeclaresPatch(Spare, PatchCode, OtherCode);
        pc.Msi.HoldsNoPatches();
        return pc;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_spare_patch_a_keys_listing_names_is_kept_with_or_without_InstallProperties(bool properties)
    {
        var pc = PcWithASparePatch();
        var key = new UnlistedProductKey(System, KeyCode, [PatchCode.ToLowerInvariant()], [], properties);

        Assert.Equal([DeclaredProductOutcome.DeclaredPatchUnestablished], pc.Screen([key], Patch(Spare)));
    }

    [Fact]
    public void A_spare_patch_is_kept_where_a_keys_listing_was_not_established()
    {
        var pc = PcWithASparePatch();
        var key = new UnlistedProductKey(System, KeyCode, null, [], false);

        Assert.Equal([DeclaredProductOutcome.DeclaredPatchUnestablished], pc.Screen([key], Patch(Spare)));
    }

    [Fact]
    public void A_listing_not_naming_a_spare_patch_adds_nothing()
    {
        var pc = PcWithASparePatch();
        var key = new UnlistedProductKey(System, KeyCode, [OtherCode], [], true);

        Assert.Equal([DeclaredProductOutcome.DeclaredPatchNotRegistered], pc.Screen([key], Patch(Spare)));
    }

    // ---------------------------------------------------------------- what the report counts

    private const string SecondKey = "{55555555-5555-5555-5555-555555555555}";

    private static UnlistedKeyHolds HoldsOf(Pc pc, IReadOnlyList<UnlistedProductKey> keys, params OrphanedFile[] files) =>
        pc.ScreeningOf(keys, files).UnlistedHolds;

    [Fact]
    public void A_pass_with_no_key_counts_nothing()
    {
        Assert.Equal(UnlistedKeyHolds.None, HoldsOf(new Pc(), [], Package(First), Package(Second)));
    }

    [Fact]
    public void A_file_declaring_a_keys_code_counts_for_a_key_with_InstallProperties()
    {
        var pc = new Pc();
        pc.Packages.Declares(First, KeyCode);

        Assert.Equal(new UnlistedKeyHolds(1, 0), HoldsOf(pc, [Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void Every_file_a_key_keeps_counts_once()
    {
        var pc = new Pc();
        pc.InstalledFrom(Folder);

        Assert.Equal(new UnlistedKeyHolds(2, 0), HoldsOf(pc, [Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_file_reached_by_a_keys_package_name_counts_for_a_key_with_InstallProperties()
    {
        var pc = new Pc();
        pc.HasList(MachineList(KeyCode), "a.msi", Setup);
        pc.Files.Answers(Setup + "a.msi", FileIdentityRead.NamesNothing);

        Assert.Equal(new UnlistedKeyHolds(1, 0), HoldsOf(pc, [Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_file_whose_identity_will_not_read_counts_for_the_kind_of_key_comparing_it()
    {
        var pc = new Pc();
        pc.HasList(MachineList(KeyCode), SetupName, Setup);
        pc.Files.Opens(SetupPackage, 1);
        pc.Files.Answers(Folder + SetupName, FileIdentityRead.NamesNothing);
        pc.Files.Answers(First, FileIdentityRead.OpenRefused);

        Assert.Equal(new UnlistedKeyHolds(0, 1), HoldsOf(pc, [NoProperties()], Package(First), Package(Second)));
        Assert.Equal(new UnlistedKeyHolds(1, 0), HoldsOf(pc, [Installed()], Package(First), Package(Second)));
    }

    [Fact]
    public void The_package_at_a_key_with_no_InstallProperties_counts_for_that_kind()
    {
        var pc = new Pc();
        pc.HasList(MachineList(KeyCode), SetupName, Setup);
        pc.Files.Opens(SetupPackage, 1);

        Assert.Equal(new UnlistedKeyHolds(0, 1), HoldsOf(pc, [NoProperties()], Package(First), Package(Second)));
    }

    [Fact]
    public void A_file_kept_for_both_kinds_of_key_counts_for_a_key_with_InstallProperties()
    {
        var pc = new Pc();
        pc.HasList(MachineList(KeyCode), SetupName, Setup);
        pc.HasList(MachineList(SecondKey), SetupName, Setup);
        pc.Registry.Answers(HiveList(System, SecondKey), RegistryKeyPresence.Absent);
        pc.Registry.Answers(ManagedList(System, SecondKey), RegistryKeyPresence.Absent);
        pc.Files.Opens(SetupPackage, 1);
        pc.Files.Answers(Folder + SetupName, FileIdentityRead.NamesNothing);

        Assert.Equal(
            new UnlistedKeyHolds(1, 0),
            HoldsOf(pc, [Installed(), NoProperties(System, SecondKey)], Package(First), Package(Second)));
    }

    [Theory]
    [InlineData(true, 1, 0)]
    [InlineData(false, 0, 1)]
    public void A_spare_patch_a_listing_names_counts_for_that_keys_kind(bool properties, int with, int without)
    {
        var key = new UnlistedProductKey(System, KeyCode, [PatchCode], [], properties);

        Assert.Equal(new UnlistedKeyHolds(with, without), HoldsOf(PcWithASparePatch(), [key], Patch(Spare)));
    }

    [Theory]
    [InlineData(true, 1, 0)]
    [InlineData(false, 0, 1)]
    public void A_spare_patch_a_listing_not_established_may_name_counts_for_that_keys_kind(
        bool properties, int with, int without)
    {
        var key = new UnlistedProductKey(System, KeyCode, null, [], properties);

        Assert.Equal(new UnlistedKeyHolds(with, without), HoldsOf(PcWithASparePatch(), [key], Patch(Spare)));
    }

    [Fact]
    public void A_spare_patch_both_kinds_of_key_name_counts_for_a_key_with_InstallProperties()
    {
        UnlistedProductKey[] keys =
        [
            new(System, SecondKey, [PatchCode], [], false),
            new(System, KeyCode, [PatchCode], [], true),
        ];

        Assert.Equal(new UnlistedKeyHolds(1, 0), HoldsOf(PcWithASparePatch(), keys, Patch(Spare)));
    }

    [Fact]
    public void A_listing_not_naming_a_spare_patch_counts_nothing()
    {
        var key = new UnlistedProductKey(System, KeyCode, [OtherCode], [], true);

        Assert.Equal(UnlistedKeyHolds.None, HoldsOf(PcWithASparePatch(), [key], Patch(Spare)));
    }

    [Fact]
    public void A_patch_Windows_holds_a_registration_of_keeps_that_answer_whatever_a_listing_says()
    {
        var pc = new Pc();
        pc.Packages.DeclaresPatch(Spare, PatchCode, OtherCode);
        pc.Msi.HoldsPatch(PatchCode, OtherCode, null, MsiInstallContext.Machine);
        pc.Msi.RecordsPatchPackage(PatchCode, OtherCode, null, MsiInstallContext.Machine, Spare);
        pc.Files.Opens(Spare, 9);
        var key = new UnlistedProductKey(System, KeyCode, [PatchCode], [], true);

        Assert.Equal([DeclaredProductOutcome.DeclaredPatchRegistered], pc.Screen([key], Patch(Spare)));
    }
}
