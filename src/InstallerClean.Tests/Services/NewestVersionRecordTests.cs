using InstallerClean.Services;
using Found = InstallerClean.Services.NewestVersionRecord.Found;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The two decisions behind the line saying InstallerClean has been updated: whether a
/// start says so, and whether it writes its version, from what it found of the record and
/// of an earlier run in the account. The reads and writes themselves are in
/// Services.Integration/NewestVersionRecordFileTests.
/// </summary>
public class NewestVersionRecordTests
{
    private static readonly Version Running = new(3, 8, 0);

    // The enum is internal, so a public test method takes its member's name.
    private static Found Parse(string found) => (Found)Enum.Parse(typeof(Found), found);

    [Theory]
    [InlineData(nameof(Found.Read), "3.7.0", false, true)]
    [InlineData(nameof(Found.Read), "3.7.9", false, true)]
    [InlineData(nameof(Found.Read), "2.9.12", false, true)]
    [InlineData(nameof(Found.Read), "3.8.0", true, false)]
    [InlineData(nameof(Found.Read), "3.8.0.0", true, false)]
    [InlineData(nameof(Found.Read), "3.8.1", true, false)]
    [InlineData(nameof(Found.Read), "4.0.0", true, false)]
    [InlineData(nameof(Found.Read), "not a version", true, false)]
    [InlineData(nameof(Found.Read), null, true, false)]
    // No record: an earlier version, which recorded none, ran in this account, or this is
    // the account's first run.
    [InlineData(nameof(Found.Absent), null, true, true)]
    [InlineData(nameof(Found.Absent), null, false, false)]
    // A record that would not read says nothing of what ran before.
    [InlineData(nameof(Found.Unreadable), null, true, false)]
    public void SaysUpdated_for_an_older_record_or_none_after_an_earlier_run(
        string found, string? recorded, bool earlierRunInAccount, bool expected)
    {
        Assert.Equal(expected, NewestVersionRecord.SaysUpdated(Parse(found), recorded, earlierRunInAccount, Running));
    }

    [Theory]
    [InlineData(nameof(Found.Absent), null, true)]
    [InlineData(nameof(Found.Read), "3.7.0", true)]
    [InlineData(nameof(Found.Read), "not a version", true)]
    [InlineData(nameof(Found.Read), null, true)]
    [InlineData(nameof(Found.Read), "3.8.0", false)]
    [InlineData(nameof(Found.Read), "3.9.0", false)]
    // Never over a record nobody could read.
    [InlineData(nameof(Found.Unreadable), null, false)]
    public void Records_where_the_record_is_absent_or_older_and_never_over_one_that_would_not_read(
        string found, string? recorded, bool expected)
    {
        Assert.Equal(expected, NewestVersionRecord.Records(Parse(found), recorded, Running));
    }

    [Fact]
    public void A_running_version_given_with_two_parts_compares_as_its_three()
    {
        Assert.False(NewestVersionRecord.SaysUpdated(Found.Read, "3.8.0", false, new Version(3, 8)));
        Assert.True(NewestVersionRecord.SaysUpdated(Found.Read, "3.7.0", false, new Version(3, 8)));
    }

    [Fact]
    public void Running_is_this_build_as_three_numbers()
    {
        var expected = typeof(NewestVersionRecord).Assembly.GetName().Version!;

        var running = NewestVersionRecord.Running();

        Assert.Equal(3, running.ToString().Split('.').Length);
        Assert.Equal(new Version(expected.Major, expected.Minor, Math.Max(expected.Build, 0)), running);
    }
}
