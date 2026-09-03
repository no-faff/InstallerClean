using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;
using Xunit;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The 8dot3 policy label the opt-in report carries. It decides nothing in the
/// app, so nothing else would notice it drifting; these are what stands between a
/// mislabelled setting and a population figure read backwards for a release.
///
/// The four settings and their meanings are Microsoft's, from the fsutil
/// 8dot3name reference. THE REGISTRY VALUE DISABLES AND THE LABELS ENABLE, so
/// every row here is also a check that the inversion survives.
/// </summary>
public class ShortNameCreationProbeTests
{
    private const string Key = @"SYSTEM\CurrentControlSet\Control\FileSystem";
    private const string Value = "NtfsDisable8dot3NameCreation";

    private static IShortNameCreationProbe Probe(RegistryDwordRead read)
    {
        var registry = Substitute.For<IRegistryReader>();
        registry.LocalMachineDwordValue(Key, Value).Returns(read);
        return new ShortNameCreationProbe(registry);
    }

    [Theory]
    [InlineData(0, ShortNameCreationLabels.AllVolumes)]
    [InlineData(1, ShortNameCreationLabels.NoVolumes)]
    [InlineData(2, ShortNameCreationLabels.PerVolume)]
    [InlineData(3, ShortNameCreationLabels.SystemVolumeOnly)]
    public void The_four_documented_settings_map_to_their_own_label(int setting, string expected)
    {
        Assert.Equal(expected, Probe(new RegistryDwordRead(RegistryDwordState.Read, setting)).Read());
    }

    [Theory]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(99)]
    public void A_setting_outside_the_documented_four_is_unrecognised_not_folded_in(int setting)
    {
        // Folding it into the nearest neighbour would report a policy nobody has,
        // and the whole point of the field is to find out what real machines hold.
        Assert.Equal(ShortNameCreationLabels.Unrecognised,
            Probe(new RegistryDwordRead(RegistryDwordState.Read, setting)).Read());
    }

    [Fact]
    public void The_three_ways_of_having_no_setting_keep_three_labels()
    {
        // A machine never configured, a machine holding something that is not a
        // number, and a read that failed are three different findings. One label
        // for all three would be false of two of them, and the middle one is the
        // finding worth having: it would mean this app's assumptions about how
        // the value is stored do not hold somewhere.
        Assert.Equal(ShortNameCreationLabels.Unset,
            Probe(new RegistryDwordRead(RegistryDwordState.Absent)).Read());
        Assert.Equal(ShortNameCreationLabels.Unrecognised,
            Probe(new RegistryDwordRead(RegistryDwordState.WrongType)).Read());
        Assert.Equal(ShortNameCreationLabels.Unreadable,
            Probe(new RegistryDwordRead(RegistryDwordState.Unreadable)).Read());
    }

    [Fact]
    public void The_state_a_read_carries_when_nobody_set_one_is_unreadable()
    {
        // What holds the ordering that makes this true is stated where the ordering
        // is, on RegistryDwordState itself, and is not repeated here.
        Assert.Equal(RegistryDwordState.Unreadable, default(RegistryDwordRead).State);
    }

    [Fact]
    public void A_read_nobody_set_reaches_the_label_as_unreadable_and_not_as_a_setting()
    {
        // The other half of it: not what the type carries but what the app then says.
        // The switch above names three states and leaves Read on its discard arm, so a
        // default whose state is Read is answered by the setting table and comes out
        // naming a policy the machine was never asked about.
        //
        // ONE ASSERTION, because a substitute nobody scripts answers this call with the
        // same default value: constructing one is the same value by another route
        // rather than a second check. The Probe helper's own scripting is exercised by
        // the tests above, which expect real values back from it.
        Assert.Equal(ShortNameCreationLabels.Unreadable, Probe(default).Read());
    }

    [Fact]
    public void It_reads_the_key_Microsoft_documents_and_no_other()
    {
        // The path is the whole probe. A mistyped one answers Absent on every
        // machine, which is a plausible-looking label and a silent lie, so the
        // call itself is pinned rather than only its mapping.
        var registry = Substitute.For<IRegistryReader>();
        registry.LocalMachineDwordValue(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new RegistryDwordRead(RegistryDwordState.Read, 1));

        new ShortNameCreationProbe(registry).Read();

        registry.Received(1).LocalMachineDwordValue(
            @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisable8dot3NameCreation");
    }

    [Fact]
    public void Every_label_it_can_answer_is_distinct()
    {
        // The report distinguishes seven states and they have to stay seven
        // strings: two accidentally sharing a value would merge two populations
        // into one and nothing downstream could tell.
        string[] labels =
        [
            ShortNameCreationLabels.AllVolumes,
            ShortNameCreationLabels.NoVolumes,
            ShortNameCreationLabels.PerVolume,
            ShortNameCreationLabels.SystemVolumeOnly,
            ShortNameCreationLabels.Unset,
            ShortNameCreationLabels.Unrecognised,
            ShortNameCreationLabels.Unreadable,
        ];

        Assert.Equal(labels.Length, labels.Distinct().Count());
    }
}
