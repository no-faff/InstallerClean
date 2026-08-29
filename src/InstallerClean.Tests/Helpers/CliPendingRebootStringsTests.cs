using InstallerClean.Cli;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The two things the command line says when a run is held, driven from the enum
/// itself rather than from a list a test author kept in step.
///
/// Both fail quietly rather than loudly if a reason is ever added without them. The
/// stdout sentence falls back to a generic line that is true of the whole family, so
/// an operator reads something plausible and never learns that the specific reason
/// went unwritten. The Application-channel label falls back to the enum member's own
/// name, which is greppable but is not the phrase anybody's monitoring is matching on.
/// Enumerating the enum is what turns either into a failing build.
///
/// ScanViewModelPendingRebootTests does this job for the window's banner, which is a
/// bound property and testable as it stands. These two needed a method each to read
/// them back, the emitter that used to hold them writing to the console and to the
/// event log in the same breath.
/// </summary>
public class CliPendingRebootStringsTests
{
    [Fact]
    public void Every_reason_has_a_stdout_line_of_its_own()
    {
        var seen = new List<string>();

        foreach (var reason in Enum.GetValues<PendingRebootReason>())
        {
            var line = Program.PendingRebootBlockedMessage(reason, detail: null);

            Assert.False(string.IsNullOrWhiteSpace(line), $"{reason} prints nothing");
            Assert.NotEqual(Strings.Cli_PendingRebootBlocked_Other, line);
            seen.Add(line);
        }

        // Distinct, because the fallback would otherwise satisfy the assertion above
        // for every reason at once and this test would pass over the very gap it
        // exists to close.
        Assert.Equal(seen.Count, seen.Distinct().Count());
    }

    [Fact]
    public void Every_reason_has_an_event_log_label_of_its_own()
    {
        var seen = new List<string>();

        foreach (var reason in Enum.GetValues<PendingRebootReason>())
        {
            var label = Program.PendingRebootEventLogReason(reason);

            Assert.False(string.IsNullOrWhiteSpace(label), $"{reason} logs nothing");
            // The member name is the fallback, so a label equal to it is a reason whose
            // own label was never written. Comparing against the name rather than
            // against a list is what keeps this true for a reason added later.
            Assert.NotEqual(reason.ToString(), label);
            seen.Add(label);
        }

        Assert.Equal(seen.Count, seen.Distinct().Count());
    }

    [Fact]
    public void A_reason_with_no_line_of_its_own_still_says_something()
    {
        // Cast past the enum's members deliberately: this is the state a new reason
        // would arrive in before anyone wrote its two strings. The run is held either
        // way, so what matters is that the operator reads a sentence and the channel
        // carries a name, rather than a blank or a crash.
        const PendingRebootReason unwritten = (PendingRebootReason)99;

        Assert.Equal(
            Strings.Cli_PendingRebootBlocked_Other,
            Program.PendingRebootBlockedMessage(unwritten, detail: null));
        Assert.Equal(unwritten.ToString(), Program.PendingRebootEventLogReason(unwritten));
    }

    /// <summary>
    /// The one line that takes the resolved path. A null detail is what every other arm
    /// passes, so the format has to survive it rather than printing the placeholder.
    /// </summary>
    [Fact]
    public void The_in_cache_line_spends_its_placeholder_on_the_path_it_was_given()
    {
        var withPath = Program.PendingRebootBlockedMessage(
            PendingRebootReason.PendingRenameInCache, @"C:\Windows\Installer\1234.msi");
        var withNothing = Program.PendingRebootBlockedMessage(
            PendingRebootReason.PendingRenameInCache, detail: null);

        Assert.Contains(@"C:\Windows\Installer\1234.msi", withPath);
        Assert.DoesNotContain("{0}", withPath);
        Assert.DoesNotContain("{0}", withNothing);
    }
}
