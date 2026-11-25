using InstallerClean.Helpers;
using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services;

/// <summary>
/// A list of installed programs holding an entry whose name is not a program code. Windows
/// Installer's walk of that list stops at the entry, and the walks of the other lists read
/// to their end. The scan walks each list on its own, reads the one that stopped by name,
/// asks Windows about every program named after the entry by code, and reads each
/// installation Windows lists as it reads one the walk returned. It refuses wherever the
/// list it read does not account for the rows the walk returned.
///
/// Every machine here holds one per-machine program before the entry, whose cached package
/// the walk claims, so the scan has a claim to stand on whatever happens after it.
/// </summary>
public class InstallerQueryServiceStoppedListTests
{
    private const uint MoreData = 234;

    private const string Before = "{11111111-1111-1111-1111-111111111111}";
    private const string AfterOne = "{22222222-2222-2222-2222-222222222222}";
    private const string AfterTwo = "{33333333-3333-3333-3333-333333333333}";
    private const string Theirs = "{44444444-4444-4444-4444-444444444444}";
    private const string TheirsAfter = "{55555555-5555-5555-5555-555555555555}";

    private const string Account = "S-1-5-21-100-200-300-1001";
    private const string BeforeFile = @"C:\Windows\Installer\stopped-list-before.msi";
    private const string AfterFile = @"C:\Windows\Installer\stopped-list-after.msi";
    private const string AfterPatch = "{66666666-6666-6666-6666-666666666666}";
    private const string AfterPatchFile = @"C:\Windows\Installer\stopped-list-after.msp";

    // Forty characters and thirty-one: a name too long to be a packed code, and one too short.
    private const string TooLong = "ICTRIPZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ";
    private const string TooShort = "ABCDEF0123456789ABCDEF012345678";

    private static string Packed(string code) => InstallerQueryService.PackRegistryCode(code)!;

    private const uint AccessDenied = 5;

    // A fallback that ran and named no program.
    private static InstallerQueryService.FallbackRead FallbackRan(
        Dictionary<string, RegisteredPackage> claimed, CancellationToken ct) =>
        new(0, 0, RegistryProductCodes: Array.Empty<string>());

    private static InstallerQueryService.FallbackRead NoFallback(
        Dictionary<string, RegisteredPackage> claimed, CancellationToken ct) => new(0, 0);

    private static FakeMsiApi MachineWith(params string[] machineNames)
    {
        var msi = new FakeMsiApi
        {
            ProductLists = [new FakeProductList(MsiInstallContext.Machine, null, machineNames.ToList())],
        };
        msi.SetProductProperty(Before, "LocalPackage", BeforeFile);
        return msi;
    }

    private static async Task<InstallerQueryResult> Scan(FakeMsiApi msi, FakeProductLists? lists = null) =>
        await new InstallerQueryService(msi, FallbackRan, productLists: lists ?? new FakeProductLists(msi))
            .GetRegisteredPackagesAsync();

    private static string[] Codes(InstallerQueryResult result) =>
        result.Installations!.Select(i => i.ProductCode.ToUpperInvariant()).OrderBy(c => c, StringComparer.Ordinal).ToArray();

    private static string Refusal(uint code, int read) =>
        StopMessages.NothingRemoved(string.Format(Strings.Error_MsiNonSuccess, code, read, DisplayHelpers.PluraliseProduct(read)));

    [Fact]
    public async Task A_name_too_long_to_be_a_code_in_the_machine_list_lets_the_scan_ask_about_every_program_after_it()
    {
        var msi = MachineWith(Packed(Before), TooLong, Packed(AfterOne), Packed(AfterTwo));

        var result = await Scan(msi);

        Assert.Equal(new[] { Before, AfterOne, AfterTwo }, Codes(result));
        Assert.Equal(1, result.Census.StoppedProductLists);
        Assert.Equal(1, result.Census.UnnamedProductListEntries);
        Assert.Equal(2, result.Census.StoppedListInstallations);
        Assert.Equal(0, result.Census.RecoveredProductCount);
        Assert.Equal(0, result.Census.SkippedProductRows);
        Assert.Contains(result.Packages, p => p.LocalPackagePath == Path.GetFullPath(BeforeFile));
    }

    [Fact]
    public async Task A_program_after_the_entry_is_read_as_it_is_from_a_list_that_reads_whole()
    {
        // The program after the entry claims its cached package and holds a superseded patch.
        // Every claim the scan makes is the same with the entry in its list and without it.
        static FakeMsiApi WithProgramAfter(FakeMsiApi msi)
        {
            msi.SetProductProperty(AfterOne, "LocalPackage", AfterFile);
            msi.AddPatch(AfterOne, AfterPatch, AfterPatchFile, state: "2", uninstallable: "0");
            return msi;
        }

        var stopped = await Scan(WithProgramAfter(MachineWith(Packed(Before), TooLong, Packed(AfterOne))));
        var whole = await Scan(WithProgramAfter(MachineWith(Packed(Before), Packed(AfterOne))));

        Assert.Equal(1, stopped.Census.StoppedProductLists);
        Assert.Contains(stopped.Packages, p => p.ProductCode == AfterOne && p.PatchState == 0);
        Assert.Contains(stopped.Packages, p => p.ProductCode == AfterOne && p.PatchState == 2);
        Assert.Contains(stopped.PatchClaims, c => c.PatchCode == AfterPatch && c.ProductCode == AfterOne);
        Assert.Equal(
            whole.Packages.OrderBy(p => p.LocalPackagePath, StringComparer.Ordinal),
            stopped.Packages.OrderBy(p => p.LocalPackagePath, StringComparer.Ordinal));
        Assert.Equal(whole.PatchClaims, stopped.PatchClaims);
    }

    [Fact]
    public async Task A_name_too_short_to_be_a_code_settles_the_same_way()
    {
        // Windows answers such an entry with a success that names no program.
        var msi = MachineWith(Packed(Before), TooShort, Packed(AfterOne));

        var result = await Scan(msi);

        Assert.Equal(new[] { Before, AfterOne }, Codes(result));
        Assert.Equal(1, result.Census.StoppedProductLists);
        Assert.Equal(1, result.Census.UnnamedProductListEntries);
    }

    [Fact]
    public async Task A_program_after_the_entry_that_Windows_says_is_not_installed_is_not_taken_as_one()
    {
        var msi = MachineWith(Packed(Before), TooLong, Packed(AfterOne), Packed(AfterTwo));
        msi.NotInstalled.Add(AfterTwo);

        var result = await Scan(msi);

        Assert.Equal(new[] { Before, AfterOne }, Codes(result));
        Assert.Equal(0, result.Census.UnansweredProductCount);
    }

    [Fact]
    public async Task A_program_after_the_entry_that_Windows_will_not_answer_about_is_counted_as_unanswered()
    {
        var msi = MachineWith(Packed(Before), TooLong, Packed(AfterOne));
        msi.KeyedRowResult[(AfterOne, 0)] = 1610;

        var result = await Scan(msi);

        Assert.Equal(1, result.Census.UnansweredProductCount);
        Assert.True(result.Census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task An_entry_in_one_account_s_own_list_stops_only_that_list_and_its_programs_are_asked_about()
    {
        var msi = MachineWith(Packed(Before), Packed(AfterOne));
        msi.ProductLists!.Add(new FakeProductList(MsiInstallContext.UserUnmanaged, Account,
            [Packed(Theirs), TooLong, Packed(TheirsAfter)]));
        msi.KeyedInstances[Theirs] = [(Account, MsiInstallContext.UserUnmanaged)];
        msi.KeyedInstances[TheirsAfter] = [(Account, MsiInstallContext.UserUnmanaged)];

        var result = await Scan(msi);

        Assert.Equal(new[] { Before, AfterOne, Theirs, TheirsAfter }, Codes(result));
        Assert.Contains(result.Installations!, i => i.ProductCode == TheirsAfter
            && i.UserSid == Account && i.Context == (int)MsiInstallContext.UserUnmanaged);
        Assert.Equal(1, result.Census.StoppedProductLists);
        Assert.Equal(1, result.Census.StoppedListInstallations);
        Assert.Equal(0, result.Census.UnsettledEnumeratedProductCount);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_list_read_by_name_whose_codes_do_not_match_the_rows_before_the_stop_refuses_the_scan()
    {
        var msi = MachineWith(Packed(Before), TooLong, Packed(AfterOne));
        var lists = new FakeProductLists(msi);
        lists.NamesInstead[(MsiInstallContext.Machine, null)] = [Packed(AfterOne), TooLong, Packed(Before)];

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Scan(msi, lists));

        Assert.Equal(Refusal(MoreData, 1), ex.Message);
    }

    [Fact]
    public async Task A_list_read_by_name_that_stops_somewhere_else_refuses_the_scan()
    {
        // The rows before the stop match, but the list read by name holds a second program
        // ahead of its first name that is no code, where the walk stopped.
        var msi = MachineWith(Packed(Before), TooLong, Packed(AfterOne));
        var lists = new FakeProductLists(msi);
        lists.NamesInstead[(MsiInstallContext.Machine, null)] = [Packed(Before), Packed(AfterTwo), TooLong];

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Scan(msi, lists));
    }

    [Fact]
    public async Task A_list_that_will_not_read_by_name_refuses_the_scan()
    {
        var msi = MachineWith(Packed(Before), TooLong, Packed(AfterOne));
        var lists = new FakeProductLists(msi);
        lists.NamesInstead[(MsiInstallContext.Machine, null)] = null;

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Scan(msi, lists));

        Assert.Equal(Refusal(MoreData, 1), ex.Message);
    }

    [Fact]
    public async Task A_list_read_by_name_with_no_entry_that_is_not_a_code_refuses_the_scan()
    {
        var msi = MachineWith(Packed(Before), TooLong, Packed(AfterOne));
        var lists = new FakeProductLists(msi);
        lists.NamesInstead[(MsiInstallContext.Machine, null)] = [Packed(Before), Packed(AfterOne)];

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Scan(msi, lists));
    }

    [Fact]
    public async Task With_no_lists_to_read_by_name_the_scan_refuses_as_the_walk_ended()
    {
        var msi = MachineWith(Packed(Before), TooLong, Packed(AfterOne));

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(
            () => new InstallerQueryService(msi, NoFallback).GetRegisteredPackagesAsync());

        Assert.Equal(Refusal(MoreData, 1), ex.Message);
    }

    [Fact]
    public async Task Where_no_registry_fallback_ran_a_stopped_list_is_settled_all_the_same()
    {
        // Every program after the entry is read through what Windows Installer answers, so
        // nothing about it waits on the registry's records.
        var msi = MachineWith(Packed(Before), TooLong, Packed(AfterOne));

        var result = await new InstallerQueryService(msi, NoFallback, productLists: new FakeProductLists(msi))
            .GetRegisteredPackagesAsync();

        Assert.Equal(new[] { Before, AfterOne }, Codes(result));
        Assert.Equal(1, result.Census.StoppedListInstallations);
    }

    [Fact]
    public async Task A_list_Windows_refuses_access_to_when_walked_alone_refuses_with_that_message()
    {
        var msi = MachineWith(Packed(Before), TooLong, Packed(AfterOne));
        msi.ProductListRefusals[(MsiInstallContext.Machine, null)] = AccessDenied;

        // The exception type is what tells refused access from an entry that did not read.
        await Assert.ThrowsAsync<LocalisedAccessException>(() => Scan(msi));
    }

    [Fact]
    public async Task A_walk_across_accounts_that_stops_where_no_account_s_own_walk_stops_refuses_the_scan()
    {
        // The account holding the entry is not among the accounts read, so no list read here
        // accounts for where the walk across accounts stopped.
        var msi = MachineWith(Packed(Before));
        msi.ProductLists!.Add(new FakeProductList(MsiInstallContext.UserUnmanaged, Account,
            [TooLong, Packed(TheirsAfter)]));
        var lists = new FakeProductLists(msi);
        lists.AccountsInstead[MsiInstallContext.UserUnmanaged] = [];

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Scan(msi, lists));
    }

    [Fact]
    public async Task A_list_that_reads_whole_leaves_nothing_to_settle()
    {
        var msi = MachineWith(Packed(Before), Packed(AfterOne));

        var result = await Scan(msi);

        Assert.Equal(new[] { Before, AfterOne }, Codes(result));
        Assert.Equal(0, result.Census.StoppedProductLists);
        Assert.Equal(0, result.Census.UnnamedProductListEntries);
        Assert.Equal(0, result.Census.RecoveredProductCount);
        Assert.Contains(result.Packages, p => p.LocalPackagePath == Path.GetFullPath(BeforeFile));
    }

    [Fact]
    public void A_walk_is_settled_against_its_list_only_where_the_rows_are_its_codes_in_order_up_to_the_first_name_that_is_no_code()
    {
        var rows = new List<(string ProductCode, string? UserSid, MsiInstallContext Context)>
            { (Before, null, MsiInstallContext.Machine) };

        Assert.True(InstallerQueryService.SettleAgainstList(rows, [Packed(Before), TooLong, Packed(AfterOne)], out var codes));
        Assert.Equal(new string?[] { Before, null, AfterOne }, codes);

        Assert.False(InstallerQueryService.SettleAgainstList(rows, [TooLong, Packed(Before)], out _));
        Assert.False(InstallerQueryService.SettleAgainstList(rows, [Packed(AfterOne), TooLong], out _));
        Assert.False(InstallerQueryService.SettleAgainstList(rows, [Packed(Before), Packed(AfterOne)], out _));
        Assert.False(InstallerQueryService.SettleAgainstList(rows, null, out _));
    }
}
