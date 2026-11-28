using InstallerClean.Services;
using InstallerClean.Tests.Helpers;
using Microsoft.Win32;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The source records as <see cref="RegistrySourceRecords"/> reads them from a registry a
/// test writes (<see cref="RegistryTree"/>): which keys it reads, what it takes as naming
/// a batch's folder, and when it answers that the records were not all read.
/// </summary>
public sealed class RegistrySourceRecordsTests
{
    private const string Classes = @"SOFTWARE\Classes\Installer";
    private const string Installer = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer";
    private const string Managed = Installer + @"\Managed";
    private const string UserData = Installer + @"\UserData";
    private const string HiveInstaller = @"Software\Microsoft\Installer";

    private const string System = "S-1-5-18";
    private const string Ann = "S-1-5-21-1-2-3-1001";
    private const string Bob = "S-1-5-21-1-2-3-1002";

    private const string Product = "00000000000000000000000000000001";
    private const string Patch = "00000000000000000000000000000002";

    private const string BatchA = "0123456789abcdef0123456789abcdef";
    private const string BatchB = "fedcba9876543210fedcba9876543210";
    private const string InBatchA = @"C:\Windows\Installer\InstallerClean-in-hand\" + BatchA + @"\";

    private readonly RegistryTree _registry = new RegistryTree().Hive(".DEFAULT").Hive(System).Hive(Ann).Hive(Ann + "_Classes");

    private static readonly LeftBatch[] Batches =
    [
        new(BatchA, "012345~1", [new LeftFile(BatchA, "a.msi", 5)]),
        new(BatchB, null, [new LeftFile(BatchB, "b.msp", 7)]),
    ];

    private SourceRecordsRead Read() =>
        new RegistrySourceRecords(_registry, () => _registry.LoadedHives).NamedBatches(Batches, CancellationToken.None);

    private static (string, string)[] Text(string name, string text) => [(name, text)];

    [Fact]
    public void A_registry_with_no_Windows_Installer_keys_reads_and_names_nothing()
    {
        var read = Read();

        Assert.True(read.Read);
        Assert.Empty(read.Named!);
    }

    [Fact]
    public void Records_that_name_other_folders_read_and_name_no_batch()
    {
        _registry
            .Key($@"{Classes}\Products\{Product}\SourceList", ("PackageName", "setup.msi"), ("LastUsedSource", @"n;1;C:\Downloads\"))
            .Key($@"{Classes}\Products\{Product}\SourceList\Net", ("1", @"C:\Downloads\"))
            .Key($@"{UserData}\{System}\Products\{Product}\InstallProperties",
                ("InstallSource", @"C:\Downloads\"), ("LocalPackage", @"C:\Windows\Installer\1a2b3c.msi"));

        var read = Read();

        Assert.True(read.Read);
        Assert.Empty(read.Named!);
    }

    public static TheoryData<string, string, string> PlacesASourceIsRecorded => new()
    {
        // The whole PC's programs and patches.
        { $@"{Classes}\Products\{Product}\SourceList\Net", "1", InBatchA },
        { $@"{Classes}\Products\{Product}\SourceList", "LastUsedSource", "n;1;" + InBatchA },
        { $@"{Classes}\Patches\{Patch}\SourceList\Net", "1", InBatchA },
        { $@"{Classes}\Patches\{Patch}\SourceList", "LastUsedSource", "n;1;" + InBatchA },
        // An account's managed programs and patches.
        { $@"{Managed}\{Bob}\Installer\Products\{Product}\SourceList\Net", "1", InBatchA },
        { $@"{Managed}\{Bob}\Installer\Patches\{Patch}\SourceList\Net", "1", InBatchA },
        // A signed-in account's own.
        { RegistryTree.InHive(Ann, $@"{HiveInstaller}\Products\{Product}\SourceList\Net"), "1", InBatchA },
        { RegistryTree.InHive(Ann, $@"{HiveInstaller}\Patches\{Patch}\SourceList"), "LastUsedSource", "n;1;" + InBatchA },
        // The folder a program was installed from, for the PC and for an account.
        { $@"{UserData}\{System}\Products\{Product}\InstallProperties", "InstallSource", InBatchA },
        { $@"{UserData}\{Ann}\Products\{Product}\InstallProperties", "InstallSource", InBatchA },
        // A value of another name, and a key further down a source list.
        { $@"{Classes}\Products\{Product}\SourceList", "SomethingElse", InBatchA },
        { $@"{Classes}\Products\{Product}\SourceList\URL\Deeper", "1", InBatchA },
    };

    [Theory]
    [MemberData(nameof(PlacesASourceIsRecorded))]
    public void A_source_recorded_in_any_of_the_places_names_the_batch(string key, string value, string text)
    {
        _registry.Key(key, Text(value, text));

        var read = Read();

        Assert.True(read.Read);
        Assert.Equal([BatchA], read.Named!);
        Assert.True(read.Names(BatchA));
        Assert.False(read.Names(BatchB));
    }

    [Theory]
    // The folder's name in another case, and reached another way round.
    [InlineData(@"c:\windows\installer\installerclean-in-hand\0123456789ABCDEF0123456789ABCDEF")]
    [InlineData(@"\\localhost\C$\Windows\Installer\InstallerClean-in-hand\0123456789abcdef0123456789abcdef\")]
    [InlineData(@"\\?\C:\Windows\Installer\InstallerClean-in-hand\0123456789abcdef0123456789abcdef\a.msi")]
    [InlineData("file:///C:/Windows/Installer/InstallerClean-in-hand/0123456789abcdef0123456789abcdef/")]
    // Its short name, as a whole component.
    [InlineData(@"C:\WINDOWS\INSTAL~1\INSTAL~1\012345~1\")]
    [InlineData(@"n;1;C:\WINDOWS\INSTAL~1\INSTAL~1\012345~1")]
    [InlineData(@"C:\Windows\Installer\InstallerClean-in-hand\012345~1\a.msi")]
    public void The_folders_name_or_its_short_name_names_the_batch_however_the_path_is_spelled(string text)
    {
        _registry.Key($@"{Classes}\Products\{Product}\SourceList\Net", Text("1", text));

        Assert.Equal([BatchA], Read().Named!);
    }

    [Theory]
    // The short name inside a longer component is another folder's name.
    [InlineData(@"C:\Stuff\X012345~1\")]
    [InlineData(@"C:\Stuff\012345~10\")]
    // The in-hand folder itself, and the Installer folder, name no batch.
    [InlineData(@"C:\Windows\Installer\InstallerClean-in-hand\")]
    [InlineData(@"C:\Windows\Installer\")]
    // A batch folder that is not one of those left.
    [InlineData(@"C:\Windows\Installer\InstallerClean-in-hand\00000000000000000000000000000000\")]
    public void Text_that_does_not_hold_the_name_names_nothing(string text)
    {
        _registry.Key($@"{Classes}\Products\{Product}\SourceList\Net", Text("1", text));

        var read = Read();

        Assert.True(read.Read);
        Assert.Empty(read.Named!);
    }

    [Fact]
    public void A_value_naming_the_folder_through_an_environment_variable_names_the_batch()
    {
        Environment.SetEnvironmentVariable("IC_TEST_SOURCE", InBatchA);
        try
        {
            _registry.Key(
                $@"{Classes}\Products\{Product}\SourceList\Net",
                [new RegistryValue("1", RegistryValueKind.ExpandString, "%IC_TEST_SOURCE%")]);

            Assert.Equal([BatchA], Read().Named!);
        }
        finally
        {
            Environment.SetEnvironmentVariable("IC_TEST_SOURCE", null);
        }
    }

    [Fact]
    public void Two_batches_are_each_named_by_their_own_record()
    {
        _registry
            .Key($@"{Classes}\Products\{Product}\SourceList\Net", Text("1", InBatchA))
            .Key($@"{UserData}\{System}\Products\{Product}\InstallProperties",
                Text("InstallSource", @"D:\InstallerClean-in-hand\" + BatchB));

        Assert.Equal([BatchA, BatchB], Read().Named!.OrderBy(n => n, StringComparer.Ordinal));
    }

    public static TheoryData<string> KeysThatMustRead => new()
    {
        $@"{Classes}\Products",
        $@"{Classes}\Patches",
        $@"{Classes}\Products\{Product}\SourceList",
        $@"{Classes}\Products\{Product}\SourceList\Net",
        Managed,
        $@"{Managed}\{Bob}\Installer\Products",
        $@"{Managed}\{Bob}\Installer\Patches\{Patch}\SourceList",
        RegistryTree.InHive(Ann, $@"{HiveInstaller}\Products"),
        RegistryTree.InHive(Ann, $@"{HiveInstaller}\Products\{Product}\SourceList\Net"),
        UserData,
        $@"{UserData}\{System}\Products",
        $@"{UserData}\{System}\Products\{Product}\InstallProperties",
        $@"{UserData}\{Ann}\Products\{Product}\InstallProperties",
    };

    [Theory]
    [MemberData(nameof(KeysThatMustRead))]
    public void A_key_that_is_there_and_will_not_read_leaves_the_records_unread(string key)
    {
        // Every key on the way to each is there, so the read reaches it.
        _registry
            .Key($@"{Classes}\Products\{Product}\SourceList\Net")
            .Key($@"{Managed}\{Bob}\Installer\Patches\{Patch}\SourceList")
            .Key(RegistryTree.InHive(Ann, $@"{HiveInstaller}\Products\{Product}\SourceList\Net"))
            .Key($@"{UserData}\{System}\Products\{Product}\InstallProperties")
            .Key($@"{UserData}\{Ann}\Products\{Product}\InstallProperties")
            .WillNotRead(key);

        var read = Read();

        Assert.False(read.Read);
        Assert.Contains(key, _registry.Reads);
    }

    [Fact]
    public void A_list_of_loaded_hives_that_will_not_read_leaves_the_records_unread()
    {
        var read = new RegistrySourceRecords(_registry, () => null).NamedBatches(Batches, CancellationToken.None);

        Assert.False(read.Read);
    }

    [Fact]
    public void A_signed_out_account_with_a_program_of_its_own_leaves_the_records_unread()
    {
        // Bob's hive is not loaded, and his program is under UserData and not under his
        // Managed key, so its source list is in that hive.
        _registry.Key($@"{UserData}\{Bob}\Products\{Product}\InstallProperties", Text("InstallSource", @"C:\Downloads\"));

        Assert.False(Read().Read);
    }

    [Fact]
    public void A_signed_out_account_with_a_patch_of_its_own_leaves_the_records_unread()
    {
        _registry.Key($@"{UserData}\{Bob}\Patches\{Patch}");

        Assert.False(Read().Read);
    }

    [Fact]
    public void A_signed_out_account_whose_programs_and_patches_are_all_managed_reads()
    {
        _registry
            .Key($@"{UserData}\{Bob}\Products\{Product}\InstallProperties", Text("InstallSource", @"C:\Downloads\"))
            .Key($@"{UserData}\{Bob}\Patches\{Patch}")
            .Key($@"{Managed}\{Bob}\Installer\Products\{Product}\SourceList", Text("LastUsedSource", @"n;1;C:\Downloads\"))
            .Key($@"{Managed}\{Bob}\Installer\Patches\{Patch}\SourceList", Text("LastUsedSource", @"n;1;C:\Downloads\"));

        var read = Read();

        Assert.True(read.Read);
        Assert.Empty(read.Named!);
    }

    [Fact]
    public void A_signed_out_account_with_nothing_under_UserData_reads()
    {
        _registry.Key($@"{UserData}\{Bob}");

        Assert.True(Read().Read);
    }

    [Fact]
    public void A_signed_in_accounts_own_program_reads_from_its_hive()
    {
        _registry
            .Key($@"{UserData}\{Ann}\Products\{Product}\InstallProperties", Text("InstallSource", @"C:\Downloads\"))
            .Key(RegistryTree.InHive(Ann, $@"{HiveInstaller}\Products\{Product}\SourceList\Net"), Text("1", @"C:\Downloads\"));

        var read = Read();

        Assert.True(read.Read);
        Assert.Contains(RegistryTree.InHive(Ann, $@"{HiveInstaller}\Products\{Product}\SourceList\Net"), _registry.Reads);
    }

    [Fact]
    public void A_hive_unloaded_while_the_records_are_read_leaves_them_unread()
    {
        _registry.Key($@"{UserData}\{Ann}\Products\{Product}\InstallProperties", Text("InstallSource", @"C:\Downloads\"));
        var listed = _registry.LoadedHives;
        _registry.Unload(Ann);

        // The hive was loaded when the hives were listed and has gone since.
        var read = new RegistrySourceRecords(_registry, () => listed).NamedBatches(Batches, CancellationToken.None);

        Assert.False(read.Read);
    }

    [Fact]
    public void A_cancel_stops_the_read()
    {
        _registry.Key($@"{Classes}\Products\{Product}\SourceList\Net", Text("1", @"C:\Downloads\"));
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() =>
            new RegistrySourceRecords(_registry, () => _registry.LoadedHives).NamedBatches(Batches, cancel.Token));
    }

    [Fact]
    public void The_zero_of_the_answer_is_records_that_were_not_read()
    {
        Assert.False(default(SourceRecordsRead).Read);
        Assert.False(default(SourceRecordsRead).Names(BatchA));
    }
}
