using InstallerClean.Helpers;
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
/// bound property and testable as it stands. These two are read back through a method
/// each, apart from the emitter that writes them to the console and to the event log.
/// </summary>
public class CliPendingRebootStringsTests
{
    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public void Every_reason_has_a_stdout_line_of_its_own(string arg)
    {
        var seen = new List<string>();

        foreach (var reason in Enum.GetValues<PendingRebootReason>())
        {
            var line = Program.PendingRebootBlockedMessage(arg, reason, detail: null);

            Assert.False(string.IsNullOrWhiteSpace(line), $"{reason} prints nothing");

            // A reason that shares another's line on purpose is checked against that
            // reason's line and left out of the distinct set.
            if (SharedLine.TryGetValue(reason, out var sharedWith))
            {
                Assert.Equal(Program.PendingRebootBlockedMessage(arg, sharedWith, detail: null), line);
                continue;
            }

            seen.Add(line);
        }

        // DISTINCT, AND IT IS WHAT CATCHES A REASON WITH NO LINE OF ITS OWN. The
        // in-progress file's line is the fallback's text, so a reason added later
        // that falls through to the fallback duplicates it here.
        Assert.Equal(seen.Count, seen.Distinct().Count());
        Assert.Contains(Strings.Cli_PendingRebootBlocked_Other, seen);
    }

    /// <summary>
    /// The reasons whose stdout line is another reason's, each with the reason it
    /// shares. A refused read of Windows Installer's in-progress file prints the
    /// refused-lock line, whose sentence says Windows refused the app permission to
    /// check whether Windows Installer was busy. A Windows Installer service whose
    /// state would not read prints the line of a lock that could not be sampled, whose
    /// sentence says the app could not check whether Windows Installer is busy.
    /// </summary>
    private static readonly Dictionary<PendingRebootReason, PendingRebootReason> SharedLine = new()
    {
        [PendingRebootReason.InstallerInProgressMarkerAccessRefused] = PendingRebootReason.MsiExecuteMutexAccessRefused,
        [PendingRebootReason.InstallerServiceUnreadable] = PendingRebootReason.MsiExecuteMutexUnreadable,
    };

    /// <summary>
    /// The reasons whose Application-channel label is the member's own name, written
    /// out rather than reached through the fallback. Each is stable and greppable and
    /// names what the gate read.
    /// </summary>
    private static readonly PendingRebootReason[] LabelledByName =
    {
        PendingRebootReason.InstallerInProgressMarker,
        PendingRebootReason.InstallerInProgressMarkerAccessRefused,
        PendingRebootReason.InstallerServiceRunning,
        PendingRebootReason.InstallerServiceUnreadable,
    };

    /// <summary>
    /// A refused lock prints the refusal sentence the action services' refusal
    /// prints, for the flag that ran, and never the sentence saying something is
    /// using Windows Installer, which nothing has seen.
    /// </summary>
    [Fact]
    public void A_refused_lock_prints_the_refusal_sentence_for_the_flag_that_ran()
    {
        var delete = Program.PendingRebootBlockedMessage(
            "/d", PendingRebootReason.MsiExecuteMutexAccessRefused, detail: null);
        var move = Program.PendingRebootBlockedMessage(
            "/m", PendingRebootReason.MsiExecuteMutexAccessRefused, detail: null);

        Assert.Equal(Strings.Cli_InstallerLockAccessRefused, delete);
        Assert.Equal(Strings.Cli_MoveInstallerLockAccessRefused, move);
        Assert.NotEqual(Strings.Cli_PendingRebootBlocked_MsiExecuteMutex, delete);
        Assert.NotEqual(Strings.Cli_PendingRebootBlocked_MsiExecuteMutex, move);
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public void A_lock_whose_check_failed_on_a_Windows_error_prints_and_logs_it(string arg)
    {
        var line = Program.PendingRebootBlockedMessage(
            arg, PendingRebootReason.MsiExecuteMutexUnreadable, detail: null, windowsError: 8);
        var logged = MachineContract.English(() => Program.PendingRebootEventLogLine(
            arg, PendingRebootReason.MsiExecuteMutexUnreadable, detail: null, windowsError: 8));

        Assert.Equal(
            string.Format(Strings.Cli_PendingRebootBlocked_MsiExecuteMutexUnreadableWindowsError,
                WindowsErrors.MessageFor(8), "8"),
            line);
        // The number alone in the English channel: the text Windows gives is in the language Windows is shown in.
        Assert.EndsWith(" (Windows error 8)", logged, StringComparison.Ordinal);
        Assert.DoesNotContain(WindowsErrors.MessageFor(8), logged, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_reason_has_an_event_log_label_of_its_own()
    {
        var seen = new List<string>();

        foreach (var reason in Enum.GetValues<PendingRebootReason>())
        {
            var label = MachineContract.English(
                () => Program.PendingRebootEventLogReason(reason));

            Assert.False(string.IsNullOrWhiteSpace(label), $"{reason} logs nothing");
            // The member name is the fallback, so a label equal to it is a reason whose
            // own label was never written. Comparing against the name rather than
            // against a list is what keeps this true for a reason added later. The
            // reasons labelled by their name on purpose are listed, and their label
            // is held to that name.
            if (LabelledByName.Contains(reason))
                Assert.Equal(reason.ToString(), label);
            else
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
            Program.PendingRebootBlockedMessage("/d", unwritten, detail: null));
        Assert.Equal(
            unwritten.ToString(),
            MachineContract.English(() => Program.PendingRebootEventLogReason(unwritten)));
    }

    /// <summary>
    /// The line the Application channel carries, which nothing read back until it had
    /// a method of its own.
    /// </summary>
    [Fact]
    public void No_blocked_line_asserts_a_pending_reboot_over_its_reason()
    {
        foreach (var reason in Enum.GetValues<PendingRebootReason>())
        {
            var line = MachineContract.English(
                () => Program.PendingRebootEventLogLine("/m", reason, detail: null));

            // Only the two queued-rename reasons are a restart waiting to happen. The
            // others are an installer running right now, a suspended transaction, or a
            // check the app was refused or could not make, which is the check saying it
            // cannot answer rather than an answer.
            Assert.DoesNotContain(
                "pending reboot detected", line, StringComparison.OrdinalIgnoreCase);
            // Beside the absence, so the absence is attributable: a line that had
            // stopped naming anything at all would satisfy the assertion above.
            Assert.Contains(
                MachineContract.English(() => Program.PendingRebootEventLogReason(reason)), line);
        }
    }

    [Fact]
    public void The_blocked_line_carries_its_detail_and_no_dangling_space()
    {
        var withPath = MachineContract.English(() => Program.PendingRebootEventLogLine(
            "/m", PendingRebootReason.PendingRenameInCache, @"C:\Windows\Installer\1234.msi"));
        var withNothing = MachineContract.English(() => Program.PendingRebootEventLogLine(
            "/m", PendingRebootReason.PendingRenameUnresolved, detail: null));

        // The separator comes with the detail, so the reason that has one reads as a
        // sentence and the five that do not end where their sentence ends.
        Assert.EndsWith(@". C:\Windows\Installer\1234.msi", withPath);
        Assert.EndsWith(".", withNothing);
        Assert.Equal(withNothing, withNothing.TrimEnd());
    }

    /// <summary>
    /// The one line that takes the resolved path. A null detail is what every other arm
    /// passes, so the format has to survive it rather than printing the placeholder.
    /// </summary>
    [Fact]
    public void The_in_cache_line_spends_its_placeholder_on_the_path_it_was_given()
    {
        var withPath = Program.PendingRebootBlockedMessage(
            "/d", PendingRebootReason.PendingRenameInCache, @"C:\Windows\Installer\1234.msi");
        var withNothing = Program.PendingRebootBlockedMessage(
            "/d", PendingRebootReason.PendingRenameInCache, detail: null);

        Assert.Contains(@"C:\Windows\Installer\1234.msi", withPath);
        Assert.DoesNotContain("{0}", withPath);
        Assert.DoesNotContain("{0}", withNothing);
    }
}
