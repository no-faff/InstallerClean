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
/// account and context. The superseded class is withheld on every such key but one under the
/// system account whose lists account for it, every name in them the code of a per-machine
/// program.
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

    // Forty characters: too long to be a packed code.
    private const string TooLong = "ICTRIPZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ";

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
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    /// <summary>
    /// Answers as the production reader does for a patch on the disk, naming
    /// <see cref="Before"/> as the product each patch file declares.
    /// </summary>
    private sealed class DeclaresBefore : IPackageIdentityReader
    {
        public PackageIdentity? Read(string filePath, bool isPatch, out string detail, out PackageReadRefusal refusal)
        {
            detail = string.Empty;
            refusal = PackageReadRefusal.WouldNotRead;
            return isPatch
                ? new PackageIdentity(string.Empty, true, new[] { Before })
                : new PackageIdentity(string.Empty, false, Array.Empty<string>());
        }

        public string? PackageCodeOf(string filePath) => null;

        public bool HoldsNoRevisionNumber(string filePath) => false;
    }

    [Fact]
    public async Task Beside_a_system_key_its_lists_account_for_a_patch_is_offered_unless_the_program_found_by_code_holds_it_applied()
    {
        // The system account's key, accounted for by a per-machine list naming a program the
        // walk left out, which is found by asking its code. The program the walk returned holds
        // patch P superseded. Where the program found by code holds P applied, it is asked
        // about P with every other installation and keeps P's cached file.
        const string Patch = "{77777777-7777-7777-7777-777777777777}";
        const string PatchFile = @"C:\Windows\Installer\published-list-patch.msp";

        async Task<InstallerQueryResult> ScanWith(bool missedHoldsItApplied)
        {
            var msi = Machine();
            msi.AddPatch(Before, Patch, PatchFile, state: "2", uninstallable: "0");
            msi.RouteAHolders.Add((Patch, Before));
            if (missedHoldsItApplied)
            {
                msi.AddPatch(Missed, Patch, PatchFile, state: "1", uninstallable: "0");
                msi.RouteAHolders.Add((Patch, Missed));
            }
            msi.PatchSequenceFor = (_, _) => FakeMsiApi.CannotApply;
            var lists = new FakeProductLists(msi);
            lists.PublishedInstead[(MsiInstallContext.Machine, null)] = [Packed(Before), Packed(Missed)];

            // The registry's reading of every program's patch set, as the fallback gives one for
            // each product key; a program with none would hold back every patch it shares.
            var sets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase)
            {
                [Before] = ProductPatchSet.AllNonRemovable,
                [Missed] = ProductPatchSet.AllNonRemovable,
            };
            return await new InstallerQueryService(msi,
                    (_, _) => new InstallerQueryService.FallbackRead(0, 3,
                        RegistryProductCodes: [Before], UnparseableProductKeyNames: 1,
                        UnparseableProductKeysByAccount: new Dictionary<string, int> { [LocalSystem] = 1 },
                        ProductPatchSets: sets),
                    identityReader: new DeclaresBefore(), productLists: lists)
                .GetRegisteredPackagesAsync();
        }

        var held = await ScanWith(missedHoldsItApplied: true);
        var offered = await ScanWith(missedHoldsItApplied: false);

        Assert.Equal(1, held.Census.UnparseableSystemKeysAccountedFor);
        Assert.Equal(0, held.UnaccountedProductCount);
        Assert.Contains(held.Installations!, i => i.ProductCode == Missed);
        Assert.Contains(held.Packages, p => p.LocalPackagePath == Path.GetFullPath(PatchFile) && !p.IsRemovable);
        Assert.Equal(0, offered.UnaccountedProductCount);
        Assert.Contains(offered.Packages, p => p.LocalPackagePath == Path.GetFullPath(PatchFile) && p.IsRemovable);
    }

    public enum KeyCase
    {
        SystemListsAccountFor, SystemListsNameAPerUserProgram, SystemListsHoldANameThatIsNoCode,
        SystemCodeNotInstalled, SystemCodeUnanswered, SystemListUnread,
        AnotherAccountsListsAccountFor, AnotherAccountsListsEmpty, Unreachable, SystemAndUnreachable,
    }

    [Theory]
    [InlineData(KeyCase.SystemListsAccountFor, 0, 1)]
    [InlineData(KeyCase.SystemListsNameAPerUserProgram, 1, 0)]
    [InlineData(KeyCase.SystemListsHoldANameThatIsNoCode, 1, 0)]
    [InlineData(KeyCase.SystemCodeNotInstalled, 1, 0)]
    [InlineData(KeyCase.SystemCodeUnanswered, 2, 0)]
    [InlineData(KeyCase.SystemListUnread, 1, 0)]
    [InlineData(KeyCase.AnotherAccountsListsAccountFor, 1, 0)]
    [InlineData(KeyCase.AnotherAccountsListsEmpty, 1, 0)]
    [InlineData(KeyCase.Unreachable, 2, 0)]
    [InlineData(KeyCase.SystemAndUnreachable, 1, 1)]
    public async Task Only_a_system_key_its_per_machine_lists_account_for_leaves_the_superseded_class_unwithheld(
        KeyCase plant, int withheld, int systemAccountedFor)
    {
        // The system account's key, accounted for by a per-machine list naming a program the
        // walk left out; the same with SYSTEM's own list naming a program it installed per
        // user; with the per-machine list holding a name that is not a code; with the code it
        // names not installed; with that code unanswered; with the per-machine list unread; a
        // signed-in account's key, accounted for by its own list; the same with that account's
        // lists empty; two keys under an account whose hive is not loaded; and the first and
        // that one together, one key each.
        var msi = Machine();
        var lists = new FakeProductLists(msi);
        var keys = new Dictionary<string, int>();
        var system = plant is KeyCase.SystemListsAccountFor or KeyCase.SystemListsNameAPerUserProgram
            or KeyCase.SystemListsHoldANameThatIsNoCode or KeyCase.SystemCodeNotInstalled
            or KeyCase.SystemCodeUnanswered or KeyCase.SystemListUnread or KeyCase.SystemAndUnreachable;
        if (system)
        {
            lists.PublishedInstead[(MsiInstallContext.Machine, null)] = plant switch
            {
                KeyCase.SystemListsHoldANameThatIsNoCode => [Packed(Before), Packed(Missed), TooLong],
                KeyCase.SystemListUnread => null,
                _ => [Packed(Before), Packed(Missed)],
            };
            keys[LocalSystem] = 1;
        }
        if (plant == KeyCase.SystemCodeNotInstalled) msi.NotInstalled.Add(Missed);
        if (plant == KeyCase.SystemCodeUnanswered) msi.KeyedRowResult[(Missed, 0)] = 1610;
        if (plant == KeyCase.AnotherAccountsListsEmpty)
        {
            lists.PublishedInstead[(MsiInstallContext.UserUnmanaged, Account)] = [];
            keys[Account] = 1;
        }
        if (plant == KeyCase.SystemListsNameAPerUserProgram)
        {
            msi.KeyedInstances[Theirs] = [(LocalSystem, MsiInstallContext.UserUnmanaged)];
            lists.PublishedInstead[(MsiInstallContext.UserUnmanaged, LocalSystem)] = [Packed(Theirs)];
        }
        if (plant == KeyCase.AnotherAccountsListsAccountFor)
        {
            msi.KeyedInstances[Theirs] = [(Account, MsiInstallContext.UserUnmanaged)];
            lists.PublishedInstead[(MsiInstallContext.UserUnmanaged, Account)] = [Packed(Theirs)];
            keys[Account] = 1;
        }
        if (plant is KeyCase.Unreachable or KeyCase.SystemAndUnreachable)
        {
            lists.HiveNotLoaded.Add(Account);
            keys[Account] = plant == KeyCase.Unreachable ? 2 : 1;
        }

        var result = await Scan(msi, lists, keys);

        Assert.Equal(withheld, result.UnaccountedProductCount);
        Assert.Equal(systemAccountedFor, result.Census.UnparseableSystemKeysAccountedFor);
        Assert.Equal(
            result.Census.UnsettledEnumeratedProductCount + result.Census.UnansweredProductCount
                + result.Census.UnparseableProductKeyNames - result.Census.UnparseableSystemKeysAccountedFor,
            result.UnaccountedProductCount);
        Assert.Equal(plant is KeyCase.Unreachable or KeyCase.SystemAndUnreachable or KeyCase.SystemCodeNotInstalled
                or KeyCase.SystemCodeUnanswered or KeyCase.SystemListUnread,
            result.Census.RegistryProductUnaskable);
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
