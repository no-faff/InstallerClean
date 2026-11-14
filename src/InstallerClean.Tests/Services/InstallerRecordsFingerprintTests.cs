using InstallerClean.Services;
using Xunit;

namespace InstallerClean.Tests.Services;

public class InstallerRecordsFingerprintTests
{
    private const string ProductA = "{11111111-1111-1111-1111-111111111111}";
    private const string ProductB = "{22222222-2222-2222-2222-222222222222}";
    private const string Patch1 = "{33333333-3333-3333-3333-333333333333}";

    /// <summary>Registry lines a test can change between two readings, or a failure to report.</summary>
    private sealed class FakeRegistrySnapshot : IInstallerRegistrySnapshot
    {
        public List<string> Lines { get; } = new() { "key|HKLM\\SOFTWARE\\Classes\\Installer|3|0|100" };
        public string? Failure { get; set; }

        public string? Write(Action<string> sink)
        {
            foreach (var line in Lines)
                sink(line);
            return Failure;
        }
    }

    private static (FakeMsiApi Msi, FakeRegistrySnapshot Registry, InstallerRecordsFingerprint Print) Machine()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct(ProductA);
        msi.AddProduct(ProductB);
        msi.RouteAHolders.Add((Patch1, ProductA));
        msi.SetPatchProperty(Patch1, ProductA, "State", "2");
        msi.SetPatchProperty(Patch1, ProductA, "Uninstallable", "0");
        msi.SetPatchProperty(Patch1, ProductA, "LocalPackage", @"C:\Windows\Installer\1.msp");
        var registry = new FakeRegistrySnapshot();
        return (msi, registry, new InstallerRecordsFingerprint(msi, registry));
    }

    [Fact]
    public void Two_readings_of_an_unchanged_machine_match()
    {
        var (_, _, print) = Machine();

        var first = print.Take();
        var second = print.Take();

        Assert.True(first.Complete, first.Failure);
        Assert.True(first.Matches(second));
    }

    [Fact]
    public void A_patch_registered_to_another_product_between_readings_is_a_difference()
    {
        var (msi, _, print) = Machine();
        var before = print.Take();

        msi.RouteAHolders.Add((Patch1, ProductB));
        msi.SetPatchProperty(Patch1, ProductB, "State", "1");

        Assert.False(before.Matches(print.Take()));
    }

    [Fact]
    public void A_patch_whose_state_changes_between_readings_is_a_difference()
    {
        var (msi, _, print) = Machine();
        var before = print.Take();

        msi.SetPatchProperty(Patch1, ProductA, "State", "1");

        Assert.False(before.Matches(print.Take()));
    }

    [Fact]
    public void A_registry_key_whose_last_write_time_moves_is_a_difference()
    {
        var (_, registry, print) = Machine();
        var before = print.Take();

        registry.Lines[0] = "key|HKLM\\SOFTWARE\\Classes\\Installer|3|0|101";

        Assert.False(before.Matches(print.Take()));
    }

    [Fact]
    public void A_reading_the_registry_half_could_not_finish_matches_nothing_not_even_itself()
    {
        var (_, registry, print) = Machine();
        registry.Failure = "the walk stopped";

        var reading = print.Take();

        Assert.False(reading.Complete);
        Assert.False(reading.Matches(reading));
        Assert.False(reading.Matches(print.Take()));
    }

    [Fact]
    public void A_patch_walk_that_fails_makes_the_reading_incomplete()
    {
        var (msi, _, print) = Machine();
        msi.RouteAResult = 1610;

        var reading = print.Take();

        Assert.False(reading.Complete);
        Assert.Contains("MsiEnumPatchesEx returned 1610", reading.Failure);
    }

    [Fact]
    public void A_registry_half_that_throws_makes_the_reading_incomplete_rather_than_throwing()
    {
        var msi = new FakeMsiApi();
        var print = new InstallerRecordsFingerprint(msi, new ThrowingSnapshot());

        var reading = print.Take();

        Assert.False(reading.Complete);
    }

    private sealed class ThrowingSnapshot : IInstallerRegistrySnapshot
    {
        public string? Write(Action<string> sink) => throw new UnauthorizedAccessException("refused");
    }
}
