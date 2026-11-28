using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The accounts whose own lists the product walk reads one at a time where a walk across
/// accounts stops: those with a subkey under <c>Managed</c> or <c>UserData</c>, and every
/// hive loaded under an account's name, its names taken where two listings agree.
/// </summary>
public class RegistryProductListsAccountsTests
{
    private const string WithRecords = "S-1-5-21-100-200-300-1001";
    private const string HiveOnly = "S-1-5-21-100-200-300-2002";

    [Fact]
    public void A_hive_loaded_with_no_records_under_UserData_is_an_account_whose_list_is_walked()
    {
        // The second account's hive is loaded and it has no subtree under UserData. The first
        // is in both sources and is walked once.
        var accounts = RegistryProductLists.AccountsOf(
            [WithRecords, "S-1-5-18"],
            [".DEFAULT", "S-1-5-18", "S-1-5-19", WithRecords, WithRecords + "_Classes", HiveOnly]);

        Assert.Equal(new[] { WithRecords, "S-1-5-19", HiveOnly }, accounts);
    }

    [Fact]
    public void With_no_hive_given_only_the_listed_accounts_are_walked()
    {
        Assert.Equal(new[] { WithRecords }, RegistryProductLists.AccountsOf([WithRecords, "S-1-5-18", "Products"], []));
    }

    [Fact]
    public void Two_listings_that_agree_are_the_hives_with_no_pause()
    {
        var pauses = new List<TimeSpan>();
        var listings = 0;

        var hives = RegistryProductLists.LoadedHives(() => { listings++; return [WithRecords, HiveOnly]; }, pauses.Add);

        Assert.Equal(new[] { WithRecords, HiveOnly }, hives);
        Assert.Equal(2, listings);
        Assert.Empty(pauses);
    }

    [Fact]
    public void A_hive_loading_between_two_listings_is_listed_again_after_a_pause()
    {
        var readings = new Queue<string[]>([[WithRecords], [WithRecords, HiveOnly], [WithRecords, HiveOnly]]);
        var pauses = new List<TimeSpan>();

        var hives = RegistryProductLists.LoadedHives(() => readings.Dequeue(), pauses.Add);

        Assert.Equal(new[] { WithRecords, HiveOnly }, hives);
        Assert.Empty(readings);
        Assert.Equal(RegistryProductLists.HivePauseBefore.Take(1), pauses);
    }

    [Fact]
    public void Listings_that_never_agree_are_a_list_that_would_not_read()
    {
        var listings = 0;
        var pauses = new List<TimeSpan>();

        var hives = RegistryProductLists.LoadedHives(
            () => listings++ % 2 == 0 ? [WithRecords] : [WithRecords, HiveOnly], pauses.Add);

        Assert.Null(hives);
        Assert.Equal(RegistryProductLists.HiveReadings, listings);
        Assert.Equal(RegistryProductLists.HivePauseBefore, pauses);
    }

    [Fact]
    public void A_listing_that_will_not_read_is_a_list_that_would_not_read()
    {
        var readings = new Queue<string[]?>([[WithRecords], null]);

        Assert.Null(RegistryProductLists.LoadedHives(() => readings.Dequeue(), _ => { }));
        Assert.Null(RegistryProductLists.LoadedHives(() => null, _ => { }));
    }
}
