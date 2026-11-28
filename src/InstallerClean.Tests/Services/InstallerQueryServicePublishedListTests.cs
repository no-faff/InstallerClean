using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// A product key under <c>UserData</c> whose name is not a program code. Where the registry
/// holds one, the scan reads the published lists that account's programs are listed in and
/// asks Windows about every code they name, so a program the walk left out is recovered and
/// asked as any recovered program is. The walk-derived offer is withheld unless those lists
/// account for the key: every list read, and every code in them settled in its list's own
/// account and context.
/// The superseded class is withheld on every such key.
///
/// Every machine here holds one per-machine program, which the walk returns and whose cached
/// package it claims. The walk returns nothing else: a program a published list names is
/// found only by asking.
/// </summary>
public class InstallerQueryServicePublishedListTests
{
    private const string Before = "{11111111-1111-1111-1111-111111111111}";
    private const string Missed = "{22222222-2222-2222-2222-222222222222}";
    private const string Theirs = "{33333333-3333-3333-3333-333333333333}";
    private const string Account = "S-1-5-21-100-200-300-1001";
    private const string LocalSystem = "S-1-5-18";

    private static string Packed(string code) => InstallerQueryService.PackRegistryCode(code)!;

    private static FakeMsiApi Machine()
    {
        var msi = new FakeMsiApi
        {
            ProductLists = [new FakeProductList(MsiInstallContext.Machine, null, [Packed(Before)])],
        };
        msi.SetProductProperty(Before, "LocalPackage", @"C:\Windows\Installer\published-list-before.msi");
        return msi;
    }

    // A fallback that read the registry's product keys: Before's, and the given number of
    // keys whose name is not a code under each account.
    private static InstallerQueryService.FallbackReader Fallback(Dictionary<string, int>? unparseableByAccount) =>
        (_, _) => new InstallerQueryService.FallbackRead(
            0, 1 + (unparseableByAccount?.Values.Sum() ?? 0),
            RegistryProductCodes: [Before],
            UnparseableProductKeyNames: unparseableByAccount?.Values.Sum() ?? 0,
            UnparseableProductKeysByAccount: unparseableByAccount);

    private static Task<InstallerQueryResult> Scan(FakeMsiApi msi, FakeProductLists lists,
        Dictionary<string, int>? unparseableByAccount) =>
        new InstallerQueryService(msi, Fallback(unparseableByAccount), productLists: lists)
            .GetRegisteredPackagesAsync();

    [Fact]
    public async Task A_key_with_no_code_under_the_system_account_has_every_per_machine_code_asked()
    {
        var msi = Machine();
        var lists = new FakeProductLists(msi);
        lists.PublishedInstead[(MsiInstallContext.Machine, null)] = [Packed(Before), Packed(Missed)];

        var result = await Scan(msi, lists, new() { [LocalSystem] = 1 });

        Assert.Contains(result.Installations!, i => i.ProductCode == Missed);
        Assert.Equal(1, result.Census.PublishedCodesAsked);
        Assert.Equal(1, result.Census.RecoveredProductCount);
        Assert.Equal(0, result.Census.UnparseableProductKeysUnreachable);
        Assert.False(result.Census.RegistryProductUnaskable);
        // The key still withholds the superseded class.
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_key_with_no_code_under_a_signed_in_account_has_its_own_list_asked()
    {
        var msi = Machine();
        msi.KeyedInstances[Theirs] = [(Account, MsiInstallContext.UserUnmanaged)];
        var lists = new FakeProductLists(msi);
        lists.PublishedInstead[(MsiInstallContext.UserUnmanaged, Account)] = [Packed(Theirs)];

        var result = await Scan(msi, lists, new() { [Account] = 1 });

        Assert.Contains(result.Installations!, i => i.ProductCode == Theirs && i.UserSid == Account
            && i.Context == (int)MsiInstallContext.UserUnmanaged);
        Assert.Contains((MsiInstallContext.UserManaged, (string?)Account), lists.PublishedAsked);
        Assert.Contains((MsiInstallContext.UserUnmanaged, (string?)Account), lists.PublishedAsked);
        Assert.Equal(1, result.Census.PublishedCodesAsked);
        Assert.False(result.Census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_key_with_no_code_under_an_account_whose_hive_is_not_loaded_withholds_the_walk_derived_offer()
    {
        // The account's own list is in its hive, so the codes of its programs cannot be read.
        var msi = Machine();
        var lists = new FakeProductLists(msi);
        lists.HiveNotLoaded.Add(Account);

        var result = await Scan(msi, lists, new() { [Account] = 1 });

        Assert.Equal(1, result.Census.UnparseableProductKeysUnreachable);
        Assert.Equal(0, result.Census.PublishedCodesAsked);
        Assert.True(result.Census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_published_list_that_will_not_read_withholds_the_walk_derived_offer()
    {
        var msi = Machine();
        var lists = new FakeProductLists(msi);
        lists.PublishedInstead[(MsiInstallContext.Machine, null)] = null;

        var result = await Scan(msi, lists, new() { [LocalSystem] = 2 });

        Assert.Equal(2, result.Census.UnparseableProductKeysUnreachable);
        Assert.True(result.Census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_published_code_Windows_will_not_answer_about_withholds_the_walk_derived_offer()
    {
        var msi = Machine();
        msi.KeyedRowResult[(Missed, 0)] = 1610;
        var lists = new FakeProductLists(msi);
        lists.PublishedInstead[(MsiInstallContext.Machine, null)] = [Packed(Before), Packed(Missed)];

        var result = await Scan(msi, lists, new() { [LocalSystem] = 1 });

        // Unanswered, and so not found installed: the key is not accounted for either.
        Assert.Equal(1, result.Census.UnansweredProductCount);
        Assert.Equal(1, result.Census.UnparseableProductKeysUnreachable);
        Assert.True(result.Census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_published_code_Windows_lists_no_installation_of_withholds_the_walk_derived_offer()
    {
        // Windows Installer's walks leave out per-user programs that are advertised only, even
        // asked by code, and a program whose records sit under a name that is not its code can
        // be one.
        var msi = Machine();
        msi.NotInstalled.Add(Theirs);
        var lists = new FakeProductLists(msi);
        lists.PublishedInstead[(MsiInstallContext.UserUnmanaged, Account)] = [Packed(Theirs)];

        var result = await Scan(msi, lists, new() { [Account] = 1 });

        Assert.Equal(1, result.Census.PublishedCodesAsked);
        Assert.Equal(1, result.Census.UnparseableProductKeysUnreachable);
        Assert.True(result.Census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_program_SYSTEM_installed_per_user_withholds_the_walk_derived_offer_for_a_key_with_no_code_under_its_account()
    {
        // No walk lists such a program, asked by code or not, so the key could be its.
        var msi = Machine();
        msi.NotInstalled.Add(Theirs);
        var lists = new FakeProductLists(msi);
        lists.PublishedInstead[(MsiInstallContext.UserUnmanaged, LocalSystem)] = [Packed(Theirs)];

        var result = await Scan(msi, lists, new() { [LocalSystem] = 1 });

        Assert.Contains((MsiInstallContext.Machine, (string?)null), lists.PublishedAsked);
        Assert.Contains((MsiInstallContext.UserManaged, (string?)LocalSystem), lists.PublishedAsked);
        Assert.Contains((MsiInstallContext.UserUnmanaged, (string?)LocalSystem), lists.PublishedAsked);
        Assert.Equal(1, result.Census.UnparseableProductKeysUnreachable);
        Assert.True(result.Census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_key_with_no_code_the_count_holds_and_no_account_does_withholds_the_walk_derived_offer()
    {
        var msi = Machine();
        var lists = new FakeProductLists(msi);

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(0, 4,
                    RegistryProductCodes: [Before], UnparseableProductKeyNames: 3,
                    UnparseableProductKeysByAccount: new Dictionary<string, int> { [LocalSystem] = 1 }),
                productLists: lists)
            .GetRegisteredPackagesAsync();

        Assert.Equal(2, result.Census.UnparseableProductKeysUnreachable);
        Assert.True(result.Census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_code_in_an_accounts_own_list_is_not_settled_by_its_per_machine_installation()
    {
        // The walk lists the program per machine, and the account's own list names it too: to
        // Windows Installer those are two installations, and only the per-machine one was asked.
        var msi = Machine();
        msi.ProductLists![0] = new FakeProductList(MsiInstallContext.Machine, null, [Packed(Before), Packed(Theirs)]);
        var lists = new FakeProductLists(msi);
        lists.PublishedInstead[(MsiInstallContext.UserUnmanaged, Account)] = [Packed(Theirs)];

        var result = await Scan(msi, lists, new() { [Account] = 1 });

        Assert.Contains(result.Installations!, i => i.ProductCode == Theirs
            && i.Context == (int)MsiInstallContext.Machine);
        Assert.Equal(1, result.Census.UnparseableProductKeysUnreachable);
        Assert.True(result.Census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_walked_code_whose_own_answer_did_not_settle_does_not_account_for_the_key()
    {
        // The walk lists the account's program, and the keyed ask about it does not read, so
        // which of its installations exist is not established.
        var msi = Machine();
        msi.ProductLists!.Add(new FakeProductList(MsiInstallContext.UserUnmanaged, Account, [Packed(Theirs)]));
        msi.WalkInstances[Theirs] = (Account, MsiInstallContext.UserUnmanaged);
        msi.KeyedRowResult[(Theirs, 0)] = 1610;
        var lists = new FakeProductLists(msi);

        var result = await Scan(msi, lists, new() { [Account] = 1 });

        Assert.Contains(result.Installations!, i => i.ProductCode == Theirs && i.UserSid == Account);
        Assert.Equal(1, result.Census.UnparseableProductKeysUnreachable);
        Assert.True(result.Census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task Where_no_key_lacks_a_code_no_published_list_is_read()
    {
        var msi = Machine();
        var lists = new FakeProductLists(msi);
        lists.PublishedInstead[(MsiInstallContext.Machine, null)] = [Packed(Before), Packed(Missed)];

        var result = await Scan(msi, lists, unparseableByAccount: null);

        Assert.Empty(lists.PublishedAsked);
        Assert.Equal(0, result.Census.PublishedCodesAsked);
        Assert.DoesNotContain(result.Installations!, i => i.ProductCode == Missed);
    }

    [Fact]
    public async Task A_key_with_no_code_whose_account_the_fallback_did_not_record_withholds_the_walk_derived_offer()
    {
        var msi = Machine();
        var lists = new FakeProductLists(msi);

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(0, 2,
                    RegistryProductCodes: [Before], UnparseableProductKeyNames: 1),
                productLists: lists)
            .GetRegisteredPackagesAsync();

        Assert.Empty(lists.PublishedAsked);
        Assert.Equal(1, result.Census.UnparseableProductKeysUnreachable);
        Assert.True(result.Census.RegistryProductUnaskable);
    }
}
