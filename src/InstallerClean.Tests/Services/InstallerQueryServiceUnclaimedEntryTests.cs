using InstallerClean.Helpers;
using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;

using EstablishedPatchReach = InstallerClean.Services.InstallerQueryService.EstablishedPatchReach;
using RegistryPackageRecord = InstallerClean.Services.InstallerQueryService.RegistryPackageRecord;
using RegistryProductEntry = InstallerClean.Services.InstallerQueryService.RegistryProductEntry;

namespace InstallerClean.Tests.Services;

/// <summary>
/// What a registry entry the product enumeration never reached does to the superseded
/// offer, and what a registry package record naming a superseded patch's file does to it.
///
/// THE MACHINE, IN PLAIN WORDS. One program is installed and holds one superseded patch,
/// which nothing else on the machine can reach, so on its own the patch is offered. Each
/// test adds one thing the registry says and the enumeration did not: a cached file no
/// installation the enumeration listed claimed, a patch registration the same, or a
/// package record naming the patch's file under something other than the patch.
///
/// THEY RUN THE WHOLE ENUMERATION, because what is asserted is whether the file reaches
/// the offer, and the recovery by name, the confirmation pass and the scan-wide
/// withholding all sit between the registry read and that answer. The fallback is handed
/// in finished, as the real reader would return it: the codes of the entries it was the
/// first to claim, and the package records it read.
/// </summary>
public class InstallerQueryServiceUnclaimedEntryTests
{
    private const uint BadConfiguration = 1610;

    private const string Listed = "{AAAAAAAA-0000-0000-0000-00000000000A}";
    private const string Recovered = "{BBBBBBBB-0000-0000-0000-00000000000B}";
    private const string Leftover = "{EEEEEEEE-0000-0000-0000-00000000000E}";
    private const string Superseded = "{CCCCCCCC-0000-0000-0000-00000000000C}";
    private const string OtherPatch = "{DDDDDDDD-0000-0000-0000-00000000000D}";

    private const string Unlisted = "{F1F1F1F1-0000-0000-0000-0000000000F1}";

    private const string MachineAccount = "S-1-5-18";
    private const string UserAccount = "S-1-5-21-1-2-3-1001";

    private const string SupersededFile = @"C:\Windows\Installer\unclaimed-entry-superseded.msp";
    private const string ProductFile = @"C:\Windows\Installer\unclaimed-entry-product.msi";
    private const string UnlistedFile = @"C:\Windows\Installer\unclaimed-entry-unlisted.msi";

    // ---- A cached file a product entry names ----

    [Fact]
    public async Task A_recovered_program_whose_package_is_on_the_disk_leaves_the_patch_offered()
    {
        // The recovery finds the program and it is asked about the patch like any other.
        // Its own package is on the disk and the enumeration never claimed it, because the
        // enumeration never reached it, and that file is the recovery's to account for.
        var msi = Machine();

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed, Recovered],
            unclaimedProductFiles: [Recovered]));

        AssertOffered(Row(result));
        Assert.Equal(0, result.UnaccountedProductCount);
        Assert.Equal(1, result.Census.RecoveredProductCount);
        Assert.Equal(1, result.Census.UnclaimedProductFiles);
    }

    [Fact]
    public async Task A_key_an_uninstall_left_behind_with_its_package_on_the_disk_decides_nothing()
    {
        var msi = Machine();
        msi.NotInstalled.Add(Leftover);

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed, Leftover],
            unclaimedProductFiles: [Leftover]));

        AssertOffered(Row(result));
        Assert.Equal(0, result.UnaccountedProductCount);
        Assert.Equal(0, result.Census.RecoveredProductCount);
    }

    [Fact]
    public async Task A_second_installation_of_a_listed_program_is_recovered_and_asked_about_the_patch()
    {
        // The enumeration lists the program once. Asked about the program by its code,
        // Windows lists a second installation under a user's account, and that
        // installation is put the question about the patch in its own account.
        const string userSid = "S-1-5-21-1-2-3-1001";
        var msi = Machine();
        msi.KeyedInstances[Listed] =
            [(null, MsiInstallContext.Machine), (userSid, MsiInstallContext.UserUnmanaged)];

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed],
            unclaimedProductFiles: [Listed]));

        Assert.Contains(msi.PatchInfoReads, r => r.PatchCode == Superseded && r.ProductCode == Listed
            && r.Sid == userSid && r.Context == MsiInstallContext.UserUnmanaged && r.Property == "State");
        Assert.Equal(1, result.Census.RecoveredEnumeratedInstallationCount);
        Assert.Equal(0, result.Census.RecoveredProductCount);
        Assert.Equal(2, result.Installations.Count);
        AssertOffered(Row(result));
    }

    [Fact]
    public async Task A_second_installation_whose_InstanceType_will_not_read_counts_as_unreadable()
    {
        // The fake answers a property read whatever the account, so the enumerated
        // installation's read fails as well. One is the listed installation's; the second
        // is only there if the recovered installation was asked.
        var msi = Machine();
        msi.KeyedInstances[Listed] =
            [(null, MsiInstallContext.Machine), ("S-1-5-21-1-2-3-1001", MsiInstallContext.UserUnmanaged)];
        msi.ProductPropertyResult[(Listed, "InstanceType")] = BadConfiguration;

        var result = await Scan(msi, Fallback(registryCodes: [Listed]));

        Assert.Equal(2, result.Census.InstanceTypeUnreadableCount);
        // Both installations are listed, each marked by the reading that failed for it.
        var listed = result.Installations.Where(i => i.ProductCode == Listed).ToList();
        Assert.Equal(2, listed.Count);
        Assert.All(listed, i => Assert.True(i.SecondCopyNotRuledOut));
        Assert.False(result.Census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_listed_program_Windows_will_not_list_again_holds_every_superseded_patch_where_its_entry_names_an_unclaimed_file()
    {
        // Asked by its code, the program the enumeration listed will not answer, so an
        // installation of it nothing lists may be what the unclaimed file belongs to.
        var msi = Machine();
        msi.KeyedRowResult[(Listed, 0)] = BadConfiguration;

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed],
            unclaimedProductFiles: [Listed]));

        AssertWithheldScanWide(Row(result));
        Assert.Equal(1, result.UnaccountedProductCount);
        Assert.Equal(1, result.Census.UnsettledEnumeratedProductCount);
        // It is not a code Windows would not answer about, which would hold the whole
        // walk-derived offer as well.
        Assert.Equal(0, result.Census.UnansweredProductCount);
        Assert.False(result.Census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_listed_program_Windows_will_not_list_again_holds_nothing_where_no_unclaimed_file_names_it()
    {
        var msi = Machine();
        msi.KeyedRowResult[(Listed, 0)] = BadConfiguration;

        var result = await Scan(msi, Fallback(registryCodes: [Listed]));

        AssertOffered(Row(result));
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_program_whose_records_came_back_short_counts_once_however_many_terms_it_meets()
    {
        // Its package read fails, so its registry entry's file is the fallback's to claim,
        // and Windows will not list it again either: one code, counted once.
        var msi = Machine();
        msi.ProductPropertyResult[(Listed, "LocalPackage")] = BadConfiguration;
        msi.KeyedRowResult[(Listed, 0)] = BadConfiguration;

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed],
            unclaimedProductFiles: [Listed]));

        AssertWithheldScanWide(Row(result));
        Assert.Equal(1, result.UnaccountedProductCount);
        Assert.Equal(1, result.Census.UnreadableProducts);
    }

    // ---- A cached file a patch entry names ----

    [Fact]
    public async Task An_unclaimed_patch_entry_a_listed_programs_listing_names_leaves_the_patch_offered()
    {
        var msi = Machine();

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed],
            unclaimedPatchFiles: [OtherPatch],
            listings: new() { [Listed] = [Superseded, OtherPatch] }));

        AssertOffered(Row(result));
        Assert.Equal(0, result.Census.UnattributedPatchFileCount);
    }

    [Fact]
    public async Task Unclaimed_patch_entries_no_listing_names_hold_every_superseded_patch_and_count_one_each()
    {
        var msi = Machine();

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed],
            unclaimedPatchFiles: [OtherPatch, "{FFFFFFFF-0000-0000-0000-00000000000F}", null],
            listings: new() { [Listed] = [Superseded] }));

        AssertWithheldScanWide(Row(result));
        Assert.Equal(3, result.Census.UnattributedPatchFileCount);
        // Files and not programs, so the program figure stays where it was.
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task An_unclaimed_patch_entry_is_held_where_the_listing_that_might_name_it_would_not_read()
    {
        var msi = Machine();

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed],
            unclaimedPatchFiles: [OtherPatch],
            listings: new() { [Listed] = null }));

        AssertWithheldScanWide(Row(result));
        Assert.Equal(1, result.Census.UnattributedPatchFileCount);
    }

    [Fact]
    public async Task A_listing_of_a_program_Windows_will_not_list_again_names_no_unclaimed_patch_entry()
    {
        // The listing is merged across accounts, so what it names may be the installation
        // nobody asks. No unclaimed product file names the program here, so only the
        // patch entry can hold anything.
        var msi = Machine();
        msi.KeyedRowResult[(Listed, 0)] = BadConfiguration;

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed],
            unclaimedPatchFiles: [OtherPatch],
            listings: new() { [Listed] = [Superseded, OtherPatch] }));

        AssertWithheldScanWide(Row(result));
        Assert.Equal(1, result.Census.UnattributedPatchFileCount);
    }

    // ---- A registry package record naming the patch's file ----

    [Fact]
    public async Task A_programs_package_record_naming_the_patchs_file_keeps_it_as_a_claim()
    {
        var msi = Machine();

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed],
            records: [new(Path.GetFullPath(SupersededFile), IsPatch: false, Listed)]));

        AssertKeptOnAClaim(Row(result));
    }

    [Fact]
    public async Task A_recovered_programs_package_record_naming_the_patchs_file_keeps_it_as_a_claim()
    {
        var msi = Machine();

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed, Recovered],
            records: [new(Path.GetFullPath(SupersededFile), IsPatch: false, Recovered)]));

        Assert.Equal(1, result.Census.RecoveredProductCount);
        AssertKeptOnAClaim(Row(result));
    }

    [Fact]
    public async Task Another_patchs_package_record_naming_the_patchs_file_keeps_it_as_a_claim()
    {
        var msi = Machine();

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed],
            records: [new(Path.GetFullPath(SupersededFile), IsPatch: true, OtherPatch)]));

        AssertKeptOnAClaim(Row(result));
    }

    [Fact]
    public async Task The_patchs_own_package_records_under_two_accounts_leave_it_offered()
    {
        var msi = Machine();

        var result = await Scan(msi, Fallback(
            registryCodes: [Listed],
            records:
            [
                new(Path.GetFullPath(SupersededFile), IsPatch: true, Superseded),
                new(Path.GetFullPath(SupersededFile), IsPatch: true, Superseded),
            ]));

        AssertOffered(Row(result));
    }

    // ---- A product key no listed installation accounts for ----
    //
    // Windows lists no installation of the program and its keyed walk ends at once. The
    // registry holds its product key under an account subtree, the key's Patches listing,
    // its cached package and the patch's own record, which is the shape a program installed
    // per user by SYSTEM leaves. No walk lists it, so no pass in the scan asks it anything, and
    // what the registry says can keep a file and never release one.

    [Fact]
    public void An_unlisted_key_is_handed_on_with_its_own_accounts_cached_package_and_what_its_properties_key_shows()
    {
        const string own = @"C:\Windows\Installer\own.msi";
        const string other = @"C:\Windows\Installer\other.msi";

        var keys = InstallerQueryService.UnlistedProductKeysOf(
            [
                new RegistryProductEntry(MachineAccount, Unlisted, [Superseded]),
                new RegistryProductEntry(UserAccount, Unlisted, [], HasInstallProperties: false),
            ],
            [
                new(own, IsPatch: false, Unlisted, MachineAccount),
                new(other, IsPatch: false, Unlisted, "S-1-5-21-1-2-3-1002"),
                new(@"C:\Windows\Installer\p.msp", IsPatch: true, Unlisted, MachineAccount),
            ]);

        Assert.Equal(2, keys.Count);
        Assert.Equal([own], keys[0].CachedPackages);
        Assert.True(keys[0].HasInstallProperties);
        Assert.Equal([Superseded], keys[0].PatchCodes);
        Assert.Empty(keys[1].CachedPackages);
        Assert.False(keys[1].HasInstallProperties);
    }

    [Fact]
    public async Task A_patch_a_program_Windows_does_not_list_holds_by_its_registry_listing_is_held_back()
    {
        var msi = MachineWithAnUnlistedProgram();

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, [Superseded]),
            records:
            [
                new(Path.GetFullPath(SupersededFile), IsPatch: true, Superseded, MachineAccount),
                new(Path.GetFullPath(UnlistedFile), IsPatch: false, Unlisted, MachineAccount),
            ],
            unclaimedProductFiles: [Unlisted]));

        AssertHeldBackOnItsOwn(Row(result));
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task The_hold_needs_no_record_of_the_patch_only_the_programs_listing()
    {
        // The registry holds the unlisted program's listing and no record of the patch's own.
        // The row comes from the listed program's claim on the patch, and the listing holds it.
        var msi = MachineWithAnUnlistedProgram();

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, [Superseded])));

        AssertHeldBackOnItsOwn(Row(result));
    }

    [Fact]
    public async Task A_patch_a_keys_listing_names_is_marked_as_held_for_a_key()
    {
        var msi = MachineWithAnUnlistedProgram();

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, [Superseded])));

        Assert.True(Row(result).WithheldOnUnlistedKey);
    }

    [Fact]
    public async Task A_patch_a_listing_not_established_may_name_is_marked_as_held_for_a_key()
    {
        var msi = MachineWithAnUnlistedProgram();

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, null)));

        Assert.True(Row(result).WithheldOnUnlistedKey);
    }

    [Fact]
    public async Task A_patch_a_failed_read_holds_back_is_not_marked_as_held_for_a_key()
    {
        var msi = MachineWithAnUnlistedProgram();

        var result = await Scan(msi, UnlistedProgramFallback() with { Failures = 1 });

        AssertWithheldScanWide(Row(result));
        Assert.False(Row(result).WithheldOnUnlistedKey);
    }

    [Fact]
    public async Task A_patch_a_listing_names_with_no_removable_verdict_to_take_is_not_marked()
    {
        const string AppliedFile = @"C:\Windows\Installer\unclaimed-entry-applied.msp";
        var msi = MachineWithAnUnlistedProgram();
        msi.AddPatch(Listed, OtherPatch, AppliedFile, state: "1", uninstallable: "0");

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, [OtherPatch])));

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath.EndsWith(
            Path.GetFileName(AppliedFile), StringComparison.OrdinalIgnoreCase));
        Assert.False(row.IsRemovable);
        Assert.False(row.WithheldOnUnlistedKey);
    }

    [Fact]
    public async Task A_patch_a_listing_names_is_marked_on_a_scan_whose_registry_read_failed_too()
    {
        var msi = MachineWithAnUnlistedProgram();

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, [Superseded])) with { Failures = 1 });

        AssertWithheldScanWide(Row(result));
        Assert.True(Row(result).WithheldOnUnlistedKey);
    }

    [Fact]
    public async Task A_patch_an_installation_holds_stays_a_claim_where_a_keys_listing_names_it_too()
    {
        // The recovery lists a second program, and asked about the patch by its code it
        // answers that it holds it applied. That answer is a claim on the file. The listing
        // of a key no installation accounts for names the same patch, and it does not turn
        // the claim into a hold.
        var msi = MachineWithAnUnlistedProgram();
        msi.SetPatchProperty(Superseded, Recovered, "State", "1");
        msi.SetPatchProperty(Superseded, Recovered, "LocalPackage", SupersededFile);

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, [Superseded]),
            new RegistryProductEntry(MachineAccount, Recovered, [Superseded])));

        Assert.Equal(1, result.Census.RecoveredProductCount);
        AssertKeptOnAClaim(Row(result));
        Assert.False(Row(result).WithheldOnUnlistedKey);
    }

    [Fact]
    public async Task A_listing_not_naming_the_patch_marks_nothing()
    {
        var msi = MachineWithAnUnlistedProgram();

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, [OtherPatch])));

        Assert.False(Row(result).WithheldOnUnlistedKey);
    }

    [Fact]
    public async Task A_second_key_of_a_code_Windows_lists_under_another_account_holds_the_patch_back()
    {
        // Windows lists this code per user under an ordinary account, so the code is not
        // missing from anything, and the key under another subtree is what no installation
        // accounts for.
        var msi = Machine();
        ListUnderTheUsersAccount(msi);

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, [Superseded]),
            new RegistryProductEntry(UserAccount, Unlisted, [])));

        AssertHeldBackOnItsOwn(Row(result));
    }

    [Fact]
    public async Task The_listing_of_a_listed_installation_holds_nothing_under_its_own_account()
    {
        // The per-user installation's own key lists the patch, and Windows lists that
        // installation, so it is asked and its answer decides. It answers that it holds no
        // record of the patch.
        var msi = Machine();
        ListUnderTheUsersAccount(msi);

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(UserAccount, Unlisted, [Superseded])));

        AssertOffered(Row(result));
    }

    [Fact]
    public async Task An_installation_the_recovery_finds_accounts_for_its_own_key()
    {
        var msi = Machine();
        msi.KeyedInstances[Unlisted] = [(UserAccount, MsiInstallContext.UserUnmanaged)];

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(UserAccount, Unlisted, [Superseded])));

        Assert.Equal(1, result.Census.RecoveredProductCount);
        AssertOffered(Row(result));
    }

    [Fact]
    public async Task A_program_Windows_does_not_list_holds_nothing_its_listing_does_not_name()
    {
        var msi = MachineWithAnUnlistedProgram();

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, [OtherPatch]),
            records: [new(Path.GetFullPath(UnlistedFile), IsPatch: false, Unlisted, MachineAccount)],
            unclaimedProductFiles: [Unlisted]));

        AssertOffered(Row(result));
    }

    [Fact]
    public async Task A_key_with_no_Patches_key_holds_nothing()
    {
        var msi = MachineWithAnUnlistedProgram();

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, [])));

        AssertOffered(Row(result));
    }

    [Theory]
    [InlineData("2")]
    [InlineData("4")]
    public async Task A_row_the_hold_finds_superseded_or_obsoleted_is_not_a_harmless_absence(string state)
    {
        // An obsoleted row is never removable, so nothing withholds it, and it is what the
        // missing-files warning would exempt.
        var msi = new FakeMsiApi();
        msi.AddProduct(Listed);
        msi.SetProductProperty(Listed, "LocalPackage", ProductFile);
        msi.AddPatch(Listed, Superseded, SupersededFile, state, uninstallable: "0");
        msi.KeyedRowResult[(Unlisted, 0)] = MsiError.NoMoreItems;

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, [Superseded])));

        Assert.False(MissingFilesReport.AbsenceShownHarmless(Row(result)));
    }

    [Fact]
    public async Task A_listing_that_did_not_read_holds_every_superseded_patch_back()
    {
        var msi = MachineWithAnUnlistedProgram();

        var result = await Scan(msi, UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, null)));

        AssertWithheldScanWide(Row(result));
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_failed_read_of_the_registry_records_holds_every_superseded_patch_back()
    {
        // The key that would not read may be the only record of a program the walks leave
        // out, so nothing says which patches that program holds.
        var msi = MachineWithAnUnlistedProgram();

        var result = await Scan(msi, UnlistedProgramFallback() with { Failures = 1 });

        AssertWithheldScanWide(Row(result));
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task The_check_before_a_Move_or_Delete_drops_a_patch_such_a_program_holds()
    {
        // Every Move and Delete, from the window and the command line, passes through this
        // re-verify, which runs the enumeration again. The check under the lease re-reads
        // only pairings the scan found, and this holder is not one.
        var msi = MachineWithAnUnlistedProgram();
        var fallback = UnlistedProgramFallback(
            new RegistryProductEntry(MachineAccount, Unlisted, [Superseded]));
        var path = Row(await Scan(msi, fallback)).LocalPackagePath;
        var reverifier = new RemovableReverifier(new InstallerQueryService(msi, (_, _) => fallback), msi);

        var result = await reverifier.ReverifyAsync([path]);

        Assert.Empty(result.Surviving);
        Assert.Equal([path], result.Dropped);
        Assert.Equal(1, result.Reasons.RecordsUnreadable);
    }

    [Fact]
    public async Task A_listing_Windows_could_answer_for_is_not_read_as_a_program_it_cannot()
    {
        // Control: the listed program's own key names the patch it holds superseded.
        var msi = Machine();

        var result = await Scan(msi, UnlistedProgramFallback());

        AssertOffered(Row(result));
    }

    // ---- Which withholding took the row ----

    [Fact]
    public async Task A_row_withheld_by_its_own_patchs_check_does_not_carry_the_scan_wide_flag()
    {
        // A second listed program will not say whether it holds the patch, so the
        // confirmation pass withholds this one file, and nothing about the machine as a
        // whole withholds anything.
        const string asked = "{99999999-0000-0000-0000-000000000009}";
        var msi = Machine();
        msi.AddProduct(asked);
        msi.PatchPropertyResult[(Superseded, asked, "State")] = BadConfiguration;

        var result = await Scan(msi, Fallback(registryCodes: [Listed]));

        var row = Row(result);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.False(row.WithheldScanWide);
    }

    // ---- The machine and the fallback ----

    /// <summary>
    /// One listed program holding one superseded patch that nothing can uninstall and roll
    /// back onto, which is a patch the scan offers.
    /// </summary>
    private static FakeMsiApi Machine()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct(Listed);
        msi.SetProductProperty(Listed, "LocalPackage", ProductFile);
        msi.SetProductProperty(Listed, "ProductName", "A Program");
        msi.AddPatch(Listed, Superseded, SupersededFile, state: "2", uninstallable: "0");
        return msi;
    }

    /// <summary>
    /// <see cref="Machine"/> with a program Windows does not list: the keyed walk for its
    /// code ends at its first row, the return the real walk gives for such a program.
    /// </summary>
    private static FakeMsiApi MachineWithAnUnlistedProgram()
    {
        var msi = Machine();
        msi.KeyedRowResult[(Unlisted, 0)] = MsiError.NoMoreItems;
        return msi;
    }

    /// <summary>
    /// Has the walk list the second program per user under the ordinary account, and the
    /// keyed walk answer for that installation.
    /// </summary>
    private static void ListUnderTheUsersAccount(FakeMsiApi msi)
    {
        msi.AddProduct(Unlisted);
        msi.WalkInstances[Unlisted] = (UserAccount, MsiInstallContext.UserUnmanaged);
        msi.KeyedInstances[Unlisted] = [(UserAccount, MsiInstallContext.UserUnmanaged)];
    }

    /// <summary>
    /// <see cref="Fallback"/> with the listed program's own key under the machine's
    /// subtree, naming the superseded patch, and each of <paramref name="entries"/>. Their
    /// codes are named to the registry comparison and their listings are the merged listing,
    /// as the real reader hands them over.
    /// </summary>
    private static InstallerQueryService.FallbackRead UnlistedProgramFallback(
        params RegistryProductEntry[] entries) =>
        UnlistedProgramFallback(entries, null, null);

    private static InstallerQueryService.FallbackRead UnlistedProgramFallback(
        RegistryProductEntry entry, RegistryPackageRecord[]? records = null, string?[]? unclaimedProductFiles = null) =>
        UnlistedProgramFallback([entry], records, unclaimedProductFiles);

    private static InstallerQueryService.FallbackRead UnlistedProgramFallback(
        RegistryProductEntry[] entries, RegistryPackageRecord[]? records, string?[]? unclaimedProductFiles)
    {
        var codes = new List<string> { Listed };
        var listings = new Dictionary<string, IReadOnlyCollection<string>?>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (!codes.Contains(entry.Code)) codes.Add(entry.Code);
            listings[entry.Code] = listings.TryGetValue(entry.Code, out var seen)
                ? InstallerQueryService.MergeEstablishedNames(seen, entry.PatchCodes)
                : entry.PatchCodes;
        }

        return Fallback([.. codes], unclaimedProductFiles, listings: listings, records: records) with
        {
            ProductEntries = [new RegistryProductEntry(MachineAccount, Listed, [Superseded]), .. entries],
        };
    }

    /// <summary>
    /// The registry fallback's reading. Every code in <paramref name="registryCodes"/> has
    /// an established patch set holding nothing removable, so the per-product condition
    /// settles clean and only what a test adds can hold the patch back.
    /// <paramref name="listings"/> is each program's listing of the patches it holds; a
    /// program left out of it is read as holding only the superseded patch.
    /// </summary>
    private static InstallerQueryService.FallbackRead Fallback(
        string[] registryCodes,
        string?[]? unclaimedProductFiles = null,
        string?[]? unclaimedPatchFiles = null,
        Dictionary<string, IReadOnlyCollection<string>?>? listings = null,
        RegistryPackageRecord[]? records = null)
    {
        var patchSets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase);
        var held = new Dictionary<string, IReadOnlyCollection<string>?>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in registryCodes)
        {
            patchSets[code] = ProductPatchSet.AllNonRemovable;
            held[code] = code == Listed ? [Superseded] : [];
        }
        if (listings is not null)
            foreach (var (code, patches) in listings) held[code] = patches;

        return new InstallerQueryService.FallbackRead(
            0, registryCodes.Length,
            UnclaimedProductFileCodes: unclaimedProductFiles,
            UnclaimedPatchFileCodes: unclaimedPatchFiles,
            RegistryProductCodes: registryCodes,
            ProductPatchSets: patchSets,
            Reach: new EstablishedPatchReach(held,
                new Dictionary<string, IReadOnlyCollection<string>?>(StringComparer.OrdinalIgnoreCase)
                {
                    [Superseded] = [Path.GetFullPath(SupersededFile)],
                }),
            PackageRecords: records);
    }

    private static async Task<InstallerQueryResult> Scan(FakeMsiApi msi, InstallerQueryService.FallbackRead fallback) =>
        await new InstallerQueryService(msi, (_, _) => fallback).GetRegisteredPackagesAsync();

    /// <summary>
    /// The superseded patch's row, found by its file name. A package record is handed in
    /// as the reader hands it over, normalised, and the row carries the spelling the
    /// normalisation settled on.
    /// </summary>
    private static RegisteredPackage Row(InstallerQueryResult result) =>
        Assert.Single(result.Packages, r => r.LocalPackagePath.EndsWith(
            Path.GetFileName(SupersededFile), StringComparison.OrdinalIgnoreCase));

    private static void AssertOffered(RegisteredPackage row)
    {
        Assert.True(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    private static void AssertWithheldScanWide(RegisteredPackage row)
    {
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.True(row.WithheldScanWide);
    }

    private static void AssertHeldBackOnItsOwn(RegisteredPackage row)
    {
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.False(row.WithheldScanWide);
    }

    private static void AssertKeptOnAClaim(RegisteredPackage row)
    {
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
        Assert.False(row.WithheldScanWide);
    }
}
