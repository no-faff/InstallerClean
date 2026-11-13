using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;

using AccountCode = InstallerClean.Services.InstallerQueryService.AccountCode;
using EstablishedPatchReach = InstallerClean.Services.InstallerQueryService.EstablishedPatchReach;
using RegistryPackageRecord = InstallerClean.Services.InstallerQueryService.RegistryPackageRecord;
using RegistryProductEntry = InstallerClean.Services.InstallerQueryService.RegistryProductEntry;

namespace InstallerClean.Tests.Services;

/// <summary>
/// What a program the product walk listed, and whose records came back short, does to the
/// superseded offer.
///
/// THE MACHINE, IN PLAIN WORDS. One program holds one superseded patch, which nothing else
/// on the machine can reach, so on its own the patch is offered. A second program is listed
/// beside it, and one of its reads fails: its package record, one of its patches' package
/// records, or its patch list, which returns what it can and is then abandoned. The
/// registry fallback is handed in finished, holding whatever records and listings a test
/// gives it. The second program is per machine, or per user and unmanaged under one user's
/// account.
///
/// WHERE ITS OWN ACCOUNT'S REGISTRY HOLDS WHAT THE FAILED READ WOULD HAVE RETURNED, the
/// program is asked about each superseded patch by name and its records are read against
/// each file, and nothing is held back on the whole machine for it. Anywhere else, every
/// superseded patch is.
/// </summary>
public class InstallerQueryServiceShortProgramTests
{
    private const uint BadConfiguration = 1610;
    private const uint AccessDenied = 5;

    private const string Listed = "{AAAAAAAA-0000-0000-0000-00000000000A}";
    private const string Short = "{BBBBBBBB-0000-0000-0000-00000000000B}";
    private const string Recovered = "{EEEEEEEE-0000-0000-0000-00000000000E}";
    private const string Superseded = "{CCCCCCCC-0000-0000-0000-00000000000C}";
    private const string OtherPatch = "{DDDDDDDD-0000-0000-0000-00000000000D}";
    private const string ThirdPatch = "{FFFFFFFF-0000-0000-0000-00000000000F}";

    private const string EndlessPatch = FakeMsiApi.EndlessPatch;

    private const string SupersededFile = @"C:\Windows\Installer\short-program-superseded.msp";
    private const string OtherPatchFile = @"C:\Windows\Installer\short-program-other.msp";
    private const string ListedFile = @"C:\Windows\Installer\short-program-listed.msi";
    private const string ShortFile = @"C:\Windows\Installer\short-program-short.msi";

    /// <summary>The account a per-machine installation's records sit under.</summary>
    private const string MachineAccount = "S-1-5-18";

    /// <summary>A user's account, which is not a per-machine installation's own.</summary>
    private const string OtherAccount = "S-1-5-21-1111111111-2222222222-3333333333-1001";

    /// <summary>
    /// The account a per-user unmanaged <see cref="Short"/> is installed under, a user other
    /// than <see cref="OtherAccount"/>.
    /// </summary>
    private const string OwnerAccount = "S-1-5-21-1111111111-2222222222-3333333333-1002";

    // ---- Whether the registry holds what the failed read would have returned ----

    [Theory]
    [InlineData(MachineAccount, 0)]
    [InlineData(null, 1)]
    [InlineData(OtherAccount, 1)]
    public async Task A_programs_failed_package_read_holds_every_superseded_patch_unless_its_own_account_records_the_package(
        string? recordedUnder, int unsettled)
    {
        var msi = Machine();
        msi.ProductPropertyResult[(Short, "LocalPackage")] = BadConfiguration;

        var result = await Scan(msi, Fallback(records: recordedUnder is null
            ? []
            : [new(Path.GetFullPath(ShortFile), IsPatch: false, Short, recordedUnder)]));

        AssertCounted(result, unsettled);
    }

    [Theory]
    [InlineData(MachineAccount, 0)]
    [InlineData(null, 1)]
    [InlineData(OtherAccount, 1)]
    public async Task A_patchs_failed_package_read_holds_every_superseded_patch_unless_its_own_account_records_the_package(
        string? recordedUnder, int unsettled)
    {
        var msi = Machine();
        msi.AddPatch(Short, OtherPatch, OtherPatchFile, state: "1", uninstallable: "0");
        msi.PatchPropertyResult[(OtherPatch, Short, "LocalPackage")] = BadConfiguration;

        var result = await Scan(msi, Fallback(records: recordedUnder is null
            ? []
            : [new(Path.GetFullPath(OtherPatchFile), IsPatch: true, OtherPatch, recordedUnder)]));

        AssertCounted(result, unsettled);
    }

    /// <summary>
    /// The patch list returns <see cref="OtherPatch"/> and is then abandoned. The listing is
    /// the one the fallback read under <paramref name="listedUnder"/>, or none: a product key
    /// with no <c>Patches</c> key yields no listing, and that is how the second case is
    /// built.
    /// </summary>
    [Theory]
    [InlineData(MachineAccount, new[] { OtherPatch, ThirdPatch }, 0)]
    [InlineData(null, null, 1)]
    [InlineData(MachineAccount, new[] { ThirdPatch }, 1)]
    [InlineData(OtherAccount, new[] { OtherPatch, ThirdPatch }, 1)]
    public async Task A_patch_list_that_came_back_short_holds_every_superseded_patch_unless_its_own_accounts_listing_names_every_patch_it_returned(
        string? listedUnder, string[]? listing, int unsettled)
    {
        var msi = Machine();
        msi.AddPatch(Short, OtherPatch, OtherPatchFile, state: "1", uninstallable: "0");
        msi.PatchRowsFailFrom[Short] = 1;

        var result = await Scan(msi, Fallback(listings: listedUnder is null
            ? new()
            : new() { [new AccountCode(listedUnder, Short)] = listing! }));

        AssertCounted(result, unsettled);
    }

    [Theory]
    [InlineData(null, MsiInstallContext.Machine, MachineAccount)]
    [InlineData(OtherAccount, MsiInstallContext.Machine, null)]
    [InlineData(OtherAccount, MsiInstallContext.UserManaged, OtherAccount)]
    [InlineData(null, MsiInstallContext.UserManaged, null)]
    [InlineData(@"S-1-5-21-1\..\S-1-5-18", MsiInstallContext.UserManaged, null)]
    [InlineData(OtherAccount, MsiInstallContext.UserUnmanaged, OtherAccount)]
    [InlineData(null, MsiInstallContext.UserUnmanaged, null)]
    [InlineData(@"S-1-5-21-1\..\S-1-5-18", MsiInstallContext.UserUnmanaged, null)]
    public void An_installations_records_are_read_only_under_the_account_it_is_installed_in(
        string? sid, MsiInstallContext context, string? account)
    {
        // Per machine is the local system's subtree, and per user, managed or not, is the
        // user's own. Per machine with an account and per user without one have no
        // subtree whose records answer for them.
        Assert.Equal(account, InstallerQueryService.UserDataAccount(sid, context));
    }

    // ---- A per-user unmanaged program, whose records answer from its own account ----

    [Theory]
    [InlineData(OwnerAccount, 0)]
    [InlineData(OtherAccount, 1)]
    [InlineData(MachineAccount, 1)]
    [InlineData(null, 1)]
    public async Task A_per_user_programs_failed_package_read_holds_every_superseded_patch_unless_its_own_account_records_the_package(
        string? recordedUnder, int unsettled)
    {
        var msi = PerUserMachine();
        msi.ProductPropertyResult[(Short, "LocalPackage")] = BadConfiguration;

        var result = await Scan(msi, Fallback(
            records: recordedUnder is null
                ? []
                : [new(Path.GetFullPath(ShortFile), IsPatch: false, Short, recordedUnder)]));

        AssertCounted(result, unsettled);
    }

    /// <summary>
    /// The patch whose package read fails is listed in the program's own account and
    /// context, so its record answers under <see cref="OwnerAccount"/> and under no other.
    /// </summary>
    [Theory]
    [InlineData(OwnerAccount, 0)]
    [InlineData(OtherAccount, 1)]
    [InlineData(MachineAccount, 1)]
    [InlineData(null, 1)]
    public async Task A_per_user_programs_failed_patch_package_read_holds_every_superseded_patch_unless_its_own_account_records_the_package(
        string? recordedUnder, int unsettled)
    {
        var msi = PerUserMachine();
        msi.AddPatch(Short, OtherPatch, OtherPatchFile, state: "1", uninstallable: "0");
        msi.PatchPropertyResult[(OtherPatch, Short, "LocalPackage")] = BadConfiguration;

        var result = await Scan(msi, Fallback(
            records: recordedUnder is null
                ? []
                : [new(Path.GetFullPath(OtherPatchFile), IsPatch: true, OtherPatch, recordedUnder)]));

        AssertCounted(result, unsettled);
    }

    [Theory]
    [InlineData(OwnerAccount, 0)]
    [InlineData(OtherAccount, 1)]
    [InlineData(MachineAccount, 1)]
    [InlineData(null, 1)]
    public async Task A_per_user_programs_short_patch_list_holds_every_superseded_patch_unless_its_own_accounts_listing_names_every_patch_it_returned(
        string? listedUnder, int unsettled)
    {
        var msi = PerUserMachine();
        msi.AddPatch(Short, OtherPatch, OtherPatchFile, state: "1", uninstallable: "0");
        msi.PatchRowsFailFrom[Short] = 1;

        var result = await Scan(msi, Fallback(
            listings: listedUnder is null
                ? new()
                : new() { [new AccountCode(listedUnder, Short)] = [OtherPatch, ThirdPatch] }));

        AssertCounted(result, unsettled);
    }

    [Fact]
    public async Task A_short_per_user_program_holding_no_record_of_the_patch_is_asked_in_its_own_account_and_leaves_it_offered()
    {
        var msi = PerUserMachine();
        msi.AddPatch(Short, OtherPatch, OtherPatchFile, state: "1", uninstallable: "0");
        msi.PatchRowsFailFrom[Short] = 1;

        var result = await Scan(msi, Fallback(
            listings: new() { [new AccountCode(OwnerAccount, Short)] = [OtherPatch] }));

        Assert.Equal(0, result.UnaccountedProductCount);
        Assert.Contains(msi.PatchInfoReads, r => r.PatchCode == Superseded && r.ProductCode == Short
            && r.Sid == OwnerAccount && r.Context == MsiInstallContext.UserUnmanaged && r.Property == "State");
        AssertOffered(Row(result));
    }

    [Fact]
    public async Task A_short_per_user_program_holding_the_patch_applied_keeps_it_as_a_claim()
    {
        var msi = PerUserMachine();
        msi.AddPatch(Short, OtherPatch, OtherPatchFile, state: "1", uninstallable: "0");
        msi.PatchRowsFailFrom[Short] = 1;
        msi.SetPatchProperty(Superseded, Short, "State", "1");
        msi.SetPatchProperty(Superseded, Short, "Uninstallable", "0");

        var result = await Scan(msi, Fallback(
            listings: new() { [new AccountCode(OwnerAccount, Short)] = [OtherPatch, Superseded] }));

        AssertKeptOnAClaim(Row(result));
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_per_user_program_whose_own_account_records_what_it_lost_holds_every_superseded_patch_where_Windows_will_not_list_it_again_and_its_entry_names_an_unclaimed_file()
    {
        // Its own account records the package its read lost, and asked by its code the
        // program will not answer while its registry entry names a cached file the
        // enumeration never claimed.
        var msi = PerUserMachine();
        msi.ProductPropertyResult[(Short, "LocalPackage")] = BadConfiguration;
        msi.KeyedRowResult[(Short, 0)] = BadConfiguration;

        var result = await Scan(msi, Fallback(
            records: [new(Path.GetFullPath(ShortFile), IsPatch: false, Short, OwnerAccount)],
            unclaimedProductFiles: [Short]));

        AssertCounted(result, unsettled: 1);
    }

    // ---- A short program whose own account's registry holds what it lost ----

    [Fact]
    public async Task A_short_program_holding_no_record_of_the_patch_leaves_it_offered()
    {
        var result = await Scan(ShortPatchList(), ShortProgramsListing());

        AssertOffered(Row(result));
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_short_program_holding_the_patch_applied_keeps_it_as_a_claim()
    {
        // Its patch list never reached the patch. Asked by the patch's code, it answers
        // that the patch is applied.
        var msi = ShortPatchList();
        msi.SetPatchProperty(Superseded, Short, "State", "1");
        msi.SetPatchProperty(Superseded, Short, "Uninstallable", "0");

        var result = await Scan(msi, ShortProgramsListing(Superseded));

        AssertKeptOnAClaim(Row(result));
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_short_program_that_will_not_answer_about_the_patch_holds_that_file_alone()
    {
        var msi = ShortPatchList();
        msi.SetPatchProperty(Superseded, Short, "Uninstallable", "0");
        msi.PatchPropertyResult[(Superseded, Short, "State")] = BadConfiguration;

        var result = await Scan(msi, ShortProgramsListing(Superseded));

        var row = Row(result);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.False(row.WithheldScanWide);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_short_program_holding_the_patch_superseded_beside_one_it_can_uninstall_keeps_it()
    {
        // Asked by name, it answers that the patch is superseded and declares zero, which
        // on its own would let the file go. What keeps it is that the program holds a patch
        // that can be uninstalled, and a rollback on it can reach for the shared file. Its
        // answer is the only thing that puts it among the programs the file is judged
        // against: no claim names it on this file.
        var msi = Machine();
        msi.AddPatch(Short, OtherPatch, OtherPatchFile, state: "1", uninstallable: "1");
        msi.PatchRowsFailFrom[Short] = 1;
        msi.SetPatchProperty(Superseded, Short, "State", "2");
        msi.SetPatchProperty(Superseded, Short, "Uninstallable", "0");

        var result = await Scan(msi, Fallback(
            listings: new() { [new AccountCode(MachineAccount, Short)] = [OtherPatch, Superseded] },
            removablePatchOn: Short));

        var row = Row(result);
        Assert.Equal(ProductPatchSet.RemovablePatchPresent, row.ProductPatchSetVerdict);
        AssertKeptOnAClaim(row);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_short_programs_package_record_naming_the_patchs_file_keeps_it_as_a_claim()
    {
        var msi = Machine();
        msi.ProductPropertyResult[(Short, "LocalPackage")] = BadConfiguration;

        var result = await Scan(msi, Fallback(
            records: [new(Path.GetFullPath(SupersededFile), IsPatch: false, Short, MachineAccount)]));

        AssertKeptOnAClaim(Row(result));
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task Another_patchs_package_record_naming_the_patchs_file_keeps_it_as_a_claim()
    {
        // The short program's own read of its other patch's package record failed. The
        // registry's copy of that record names the superseded patch's file.
        var msi = Machine();
        msi.AddPatch(Short, OtherPatch, OtherPatchFile, state: "1", uninstallable: "0");
        msi.PatchPropertyResult[(OtherPatch, Short, "LocalPackage")] = BadConfiguration;

        var result = await Scan(msi, Fallback(
            records: [new(Path.GetFullPath(SupersededFile), IsPatch: true, OtherPatch, MachineAccount)]));

        AssertKeptOnAClaim(Row(result));
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task The_patchs_own_package_records_under_two_accounts_leave_it_offered()
    {
        // The short program holds the patch superseded, and its read of the patch's
        // package record failed. The registry records the patch's own file under two
        // accounts, the program's own among them.
        var msi = Machine();
        msi.AddPatch(Short, Superseded, SupersededFile, state: "2", uninstallable: "0");
        msi.PatchPropertyResult[(Superseded, Short, "LocalPackage")] = BadConfiguration;

        var result = await Scan(msi, Fallback(
            records:
            [
                new(Path.GetFullPath(SupersededFile), IsPatch: true, Superseded, MachineAccount),
                new(Path.GetFullPath(SupersededFile), IsPatch: true, Superseded, OtherAccount),
            ]));

        AssertOffered(Row(result));
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    // ---- An installation found holding the patch by name, for the check made under the lease ----

    [Fact]
    public async Task A_recovered_program_found_holding_the_patch_carries_its_pairing_and_its_other_patches()
    {
        var msi = RecoveredHolder();

        var result = await Scan(msi, Fallback(registryCodes: [Listed, Short, Recovered]));

        AssertOffered(Row(result));
        var held = Assert.Single(result.PairingsHeldByName);
        Assert.Equal((Superseded, Recovered), (held.PatchCode, held.ProductCode));
        Assert.Equal(
            new[] { Superseded, OtherPatch },
            result.PairingsOfHoldersWithNoClaims
                .Where(p => p.ProductCode == Recovered)
                .Select(p => p.PatchCode)
                .Order(StringComparer.Ordinal));
        Assert.All(result.PairingsOfHoldersWithNoClaims, p => Assert.Equal(Recovered, p.ProductCode));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_patch_list_that_refuses_for_that_program_leaves_the_scan_standing(bool neverEnds)
    {
        // Access refused, or a list that never ends, which in the product walk would refuse
        // the scan. Here it carries none of the program's other patches.
        var msi = RecoveredHolder();
        if (neverEnds) msi.NeverEndPatchesFor = Recovered;
        else msi.PatchEnumResult[Recovered] = AccessDenied;

        var result = await Scan(msi, Fallback(registryCodes: [Listed, Short, Recovered]));

        Assert.Contains(Recovered, msi.PatchEnumerationsStarted);
        Assert.Single(result.PairingsHeldByName, p => p.ProductCode == Recovered);
        Assert.Empty(result.PairingsOfHoldersWithNoClaims);
    }

    // ---- A recovered program's patches, as Windows Installer lists them ----

    [Fact]
    public async Task A_recovered_program_Windows_says_holds_an_uninstallable_patch_holds_the_superseded_one()
    {
        // The registry reads the program as holding nothing that could be uninstalled.
        // Windows Installer, asked for the program's own patches, answers that one can be,
        // and that patch could roll back onto the superseded patch's file.
        var msi = RecoveredHolder(otherPatchUninstallable: "1");

        var result = await Scan(msi, Fallback(registryCodes: [Listed, Short, Recovered]));

        // Kept on the answer, not withheld: a withheld row would mean the walk's answer
        // had been read as no answer.
        AssertKeptOnAClaim(Row(result));
    }

    // Every scan below reads the recovered program's product key under its own account:
    // a key under any other account is an entry no listed installation accounts for, which
    // holds the superseded patch back on its own (WithholdOnUnlistedEntries) and would pass
    // a row whatever the rule under test did. The account half is tested on the helpers.

    /// <summary>
    /// Windows refuses the patch list before returning a row. The registry's reading of
    /// the program, which here holds nothing that could be uninstalled, stands on the
    /// program's own product key, whether that key lists the program's patches or has no
    /// <c>Patches</c> key.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_recovered_programs_patch_list_Windows_refuses_before_any_row_leaves_the_registrys_reading_of_its_own_product_key_standing(
        bool keyListsPatches)
    {
        var msi = RecoveredHolder(otherPatchUninstallable: "1");
        msi.PatchEnumResult[Recovered] = AccessDenied;

        var result = await Scan(msi, RecoveredProgramsKey(MachineAccount,
            keyListsPatches ? [Superseded, OtherPatch] : null));

        AssertOffered(Row(result));
    }

    /// <summary>
    /// The same refusal where the registry read no product key for the program: nothing
    /// answers for it, and the superseded patch is held back.
    /// </summary>
    [Fact]
    public async Task A_recovered_programs_patch_list_Windows_refuses_holds_the_superseded_one_where_the_registry_read_no_key_of_its_own()
    {
        var msi = RecoveredHolder(otherPatchUninstallable: "1");
        msi.PatchEnumResult[Recovered] = AccessDenied;

        var result = await Scan(msi, Fallback(registryCodes: [Listed, Short, Recovered]));

        AssertOfferedOrWithheld(Row(result), withheld: true);
    }

    /// <summary>
    /// Windows returns <see cref="Superseded"/> and then refuses the rest of the list. The
    /// registry's reading stands only where the program's own listing names the row
    /// returned.
    /// </summary>
    [Theory]
    [InlineData(new[] { Superseded, OtherPatch }, false)]
    [InlineData(new[] { OtherPatch }, true)]
    public async Task A_recovered_programs_patch_list_Windows_refuses_after_a_row_holds_the_superseded_one_unless_its_own_listing_names_that_row(
        string[] listing, bool withheld)
    {
        var msi = RecoveredHolder(otherPatchUninstallable: "1");
        msi.PatchRowResult[(Recovered, 1)] = AccessDenied;

        var result = await Scan(msi, RecoveredProgramsKey(MachineAccount, listing));

        AssertOfferedOrWithheld(Row(result), withheld);
    }

    /// <summary>
    /// A patch list that never ends, returning <see cref="EndlessPatch"/> at every index,
    /// and one abandoned after a run of rows that will not read, from its second row or
    /// from its first. The registry's reading stands only where the program's own listing
    /// names every patch Windows returned, and a list that met a row it could not read
    /// needs a listing that names at least one patch however few rows it named.
    /// </summary>
    [Theory]
    [InlineData(-1, new[] { Superseded, OtherPatch, EndlessPatch }, false)]
    [InlineData(-1, new[] { Superseded, OtherPatch }, true)]
    [InlineData(1, new[] { Superseded, OtherPatch }, false)]
    [InlineData(1, new[] { OtherPatch, ThirdPatch }, true)]
    [InlineData(0, new[] { Superseded, OtherPatch }, false)]
    [InlineData(0, new string[0], true)]
    [InlineData(0, null, true)]
    public async Task A_recovered_programs_patch_list_that_is_not_whole_holds_the_superseded_one_unless_its_own_listing_names_every_patch_returned(
        int rowsFailFrom, string[]? listing, bool withheld)
    {
        var msi = RecoveredHolder(otherPatchUninstallable: "1");
        if (rowsFailFrom < 0) msi.NeverEndPatchesFor = Recovered;
        else msi.PatchRowsFailFrom[Recovered] = (uint)rowsFailFrom;

        var result = await Scan(msi, RecoveredProgramsKey(MachineAccount, listing));

        AssertOfferedOrWithheld(Row(result), withheld);
    }

    /// <summary>
    /// Windows Installer lists <see cref="OtherPatch"/> and gives no Uninstallable for it,
    /// failing or answering empty. The registry's reading covers the patch where the
    /// program's own listing names it, and the program holds the superseded patch back
    /// where that listing leaves the patch out or the program's key has no
    /// <c>Patches</c> key.
    /// </summary>
    [Theory]
    [InlineData(true, new[] { Superseded, OtherPatch }, false)]
    [InlineData(false, new[] { Superseded, OtherPatch }, false)]
    [InlineData(true, new[] { Superseded }, true)]
    [InlineData(false, new[] { Superseded }, true)]
    [InlineData(true, null, true)]
    [InlineData(false, null, true)]
    public async Task A_recovered_programs_patch_whose_uninstallable_Windows_will_not_give_holds_the_superseded_one_unless_its_own_listing_names_that_patch(
        bool readFails, string[]? listing, bool withheld)
    {
        var msi = RecoveredHolder(otherPatchUninstallable: readFails ? "1" : "");
        if (readFails) msi.PatchPropertyResult[(OtherPatch, Recovered, "Uninstallable")] = BadConfiguration;

        var result = await Scan(msi, RecoveredProgramsKey(MachineAccount, listing));

        AssertOfferedOrWithheld(Row(result), withheld);
    }

    /// <summary>
    /// The same, with <see cref="Recovered"/> installed per user and unmanaged under
    /// <see cref="OwnerAccount"/>, whose product key sits under that account.
    /// </summary>
    [Theory]
    [InlineData(new[] { Superseded, OtherPatch }, false)]
    [InlineData(new[] { Superseded }, true)]
    public async Task A_per_user_recovered_programs_patch_whose_uninstallable_Windows_will_not_give_is_answered_by_the_listing_under_its_own_account(
        string[] listing, bool withheld)
    {
        var msi = RecoveredHolder(otherPatchUninstallable: "1");
        msi.KeyedInstances[Recovered] = [(OwnerAccount, MsiInstallContext.UserUnmanaged)];
        msi.PatchPropertyResult[(OtherPatch, Recovered, "Uninstallable")] = BadConfiguration;

        var result = await Scan(msi, RecoveredProgramsKey(OwnerAccount, listing));

        AssertOfferedOrWithheld(Row(result), withheld);
    }

    [Theory]
    [InlineData(MachineAccount, true)]
    [InlineData("s-1-5-18", true)]
    [InlineData(OtherAccount, false)]
    [InlineData(null, false)]
    public void Only_the_listing_and_the_product_key_under_the_installations_own_account_answer_for_it(
        string? account, bool answers)
    {
        var listings = new Dictionary<AccountCode, IReadOnlyCollection<string>>
        {
            [new AccountCode(MachineAccount, Recovered)] = [OtherPatch],
        };
        RegistryProductEntry[] entries = [new(MachineAccount, Recovered, [OtherPatch])];

        Assert.Equal(answers, InstallerQueryService.OwnListing(listings, account, Recovered) is not null);
        Assert.Equal(answers, InstallerQueryService.ReadOwnProductKey(entries, account, Recovered));
    }

    [Theory]
    [InlineData(new[] { OtherPatch, ThirdPatch }, new[] { OtherPatch }, true)]
    [InlineData(new[] { OtherPatch }, new[] { "{dddddddd-0000-0000-0000-00000000000d}" }, true)]
    [InlineData(new[] { OtherPatch }, new[] { OtherPatch, ThirdPatch }, false)]
    [InlineData(new string[0], new string[0], true)]
    [InlineData(null, new string[0], false)]
    public void A_listing_names_every_patch_only_where_it_is_established_and_holds_each_one(
        string[]? listed, string[] patchCodes, bool namesEvery) =>
        Assert.Equal(namesEvery, InstallerQueryService.NamesEvery(listed, patchCodes));

    // ---- The machine and the fallback ----

    /// <summary>
    /// <see cref="Listed"/> holding <see cref="Superseded"/> superseded and declaring zero,
    /// which nothing on the machine can roll back onto, and <see cref="Short"/> listed beside
    /// it holding nothing a test has not given it.
    /// </summary>
    private static FakeMsiApi Machine()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct(Listed);
        msi.SetProductProperty(Listed, "LocalPackage", ListedFile);
        msi.SetProductProperty(Listed, "ProductName", "A Program");
        msi.AddPatch(Listed, Superseded, SupersededFile, state: "2", uninstallable: "0");
        msi.AddProduct(Short);
        msi.SetProductProperty(Short, "LocalPackage", ShortFile);
        msi.SetProductProperty(Short, "ProductName", "A Short Program");
        return msi;
    }

    /// <summary>
    /// The same machine, with <see cref="Short"/>'s patch list returning
    /// <see cref="OtherPatch"/> and then abandoned.
    /// </summary>
    private static FakeMsiApi ShortPatchList()
    {
        var msi = Machine();
        msi.AddPatch(Short, OtherPatch, OtherPatchFile, state: "1", uninstallable: "0");
        msi.PatchRowsFailFrom[Short] = 1;
        return msi;
    }

    /// <summary>
    /// The same machine, with <see cref="Short"/> installed per user and unmanaged under
    /// <see cref="OwnerAccount"/>, as the product walk lists it and as the keyed query
    /// answers for it.
    /// </summary>
    private static FakeMsiApi PerUserMachine()
    {
        var msi = Machine();
        msi.WalkInstances[Short] = (OwnerAccount, MsiInstallContext.UserUnmanaged);
        msi.KeyedInstances[Short] = [(OwnerAccount, MsiInstallContext.UserUnmanaged)];
        return msi;
    }

    /// <summary>
    /// A fallback whose listing of <see cref="Short"/>'s patches, under its own account,
    /// names what its list returned and <paramref name="alsoHeld"/>.
    /// </summary>
    private static InstallerQueryService.FallbackRead ShortProgramsListing(params string[] alsoHeld) =>
        Fallback(listings: new() { [new AccountCode(MachineAccount, Short)] = [OtherPatch, .. alsoHeld] });

    /// <summary>
    /// <see cref="Recovered"/>, a program the walk never listed, holding
    /// <see cref="Superseded"/> superseded and declaring zero, and <see cref="OtherPatch"/>,
    /// declaring <paramref name="otherPatchUninstallable"/>.
    /// It holds no claim, and asked by name it answers that it holds the patch.
    /// </summary>
    private static FakeMsiApi RecoveredHolder(string otherPatchUninstallable = "0")
    {
        var msi = Machine();
        msi.AddPatch(Recovered, Superseded, SupersededFile, state: "2", uninstallable: "0");
        msi.AddPatch(Recovered, OtherPatch, OtherPatchFile, state: "1", uninstallable: otherPatchUninstallable);
        return msi;
    }

    /// <summary>
    /// The registry fallback's reading. Every code in <paramref name="registryCodes"/> has
    /// an established patch set holding nothing removable, except
    /// <paramref name="removablePatchOn"/>, so only what a test adds can hold the patch back.
    /// <paramref name="listings"/> are the own-account patch listings the reader established,
    /// and <paramref name="unclaimedProductFiles"/> the codes whose entry names a cached file on
    /// the disk that the enumeration never claimed. <paramref name="productEntries"/> are the
    /// product keys the reader read, and <paramref name="heldCodes"/> replaces the patch codes
    /// the reach holds for each code it names.
    /// </summary>
    private static InstallerQueryService.FallbackRead Fallback(
        string[]? registryCodes = null,
        Dictionary<AccountCode, IReadOnlyCollection<string>>? listings = null,
        RegistryPackageRecord[]? records = null,
        string? removablePatchOn = null,
        string?[]? unclaimedProductFiles = null,
        RegistryProductEntry[]? productEntries = null,
        Dictionary<string, IReadOnlyCollection<string>>? heldCodes = null)
    {
        registryCodes ??= [Listed, Short];
        var patchSets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase);
        var held = new Dictionary<string, IReadOnlyCollection<string>?>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in registryCodes)
        {
            patchSets[code] = code == removablePatchOn
                ? ProductPatchSet.RemovablePatchPresent
                : ProductPatchSet.AllNonRemovable;
            held[code] = code == Listed ? [Superseded] : null;
        }
        foreach (var (code, codes) in heldCodes ?? [])
            held[code] = codes;

        return new InstallerQueryService.FallbackRead(
            0, registryCodes.Length,
            UnclaimedProductFileCodes: unclaimedProductFiles,
            RegistryProductCodes: registryCodes,
            ProductPatchSets: patchSets,
            Reach: new EstablishedPatchReach(held,
                new Dictionary<string, IReadOnlyCollection<string>?>(StringComparer.OrdinalIgnoreCase)
                {
                    [Superseded] = [Path.GetFullPath(SupersededFile)],
                }),
            PackageRecords: records,
            PatchListings: listings,
            ProductEntries: productEntries);
    }

    /// <summary>
    /// A fallback reading <see cref="Recovered"/> as holding nothing that could be
    /// uninstalled, from its product key under <paramref name="keyUnder"/>, whose
    /// <c>Patches</c> key lists <paramref name="listing"/>, or is not there where
    /// <paramref name="listing"/> is null. The patch codes the reach holds for
    /// <see cref="Recovered"/> are that same listing, as the reader builds both from one
    /// read.
    /// </summary>
    private static InstallerQueryService.FallbackRead RecoveredProgramsKey(
        string keyUnder, string[]? listing) =>
        Fallback(
            registryCodes: [Listed, Short, Recovered],
            listings: listing is null
                ? new()
                : new() { [new AccountCode(keyUnder, Recovered)] = listing },
            productEntries: [new(keyUnder, Recovered, listing ?? [])],
            heldCodes: new() { [Recovered] = listing ?? [] });

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

    /// <summary>
    /// The short program is counted as a program whose records came back short whatever
    /// the registry holds, and as a program the scan could not check in
    /// <paramref name="unsettled"/>.
    /// </summary>
    private static void AssertCounted(InstallerQueryResult result, int unsettled)
    {
        Assert.Equal(1, result.Census.UnreadableProducts);
        Assert.Equal(unsettled, result.Census.UnsettledEnumeratedProductCount);
        Assert.Equal(unsettled, result.UnaccountedProductCount);
    }

    private static void AssertOffered(RegisteredPackage row)
    {
        Assert.True(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    /// <summary>
    /// The row is offered, or withheld for its program alone: a withheld row is not a
    /// claim and is not held back on the whole machine.
    /// </summary>
    private static void AssertOfferedOrWithheld(RegisteredPackage row, bool withheld)
    {
        if (!withheld)
        {
            AssertOffered(row);
            return;
        }

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
