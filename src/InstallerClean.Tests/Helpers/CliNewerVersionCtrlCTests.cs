using InstallerClean.Cli;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Who says the run is cancelling while the update starts the newer version. From the
/// moment it is being started with the event that passes it Ctrl+C, the newer version
/// says so, and where that start then fails, this run says the line it held back. The
/// console is read back through <c>Console.SetOut</c>, which is process-wide; the
/// assembly runs no tests in parallel, for a reason of its own in AssemblyInfo.cs.
/// </summary>
public class CliNewerVersionCtrlCTests
{
    [Fact]
    public void This_run_says_it_is_cancelling_until_the_newer_version_is_being_started()
    {
        var host = new ConsoleUpdateHost("/s");

        Assert.True(host.TakeCancellingLine());
        host.NewerVersionAnswersCtrlC(true);
        Assert.False(host.TakeCancellingLine());
    }

    [Fact]
    public void A_start_that_fails_after_a_Ctrl_C_held_back_says_it_is_cancelling()
    {
        var host = new ConsoleUpdateHost("/s");
        host.NewerVersionAnswersCtrlC(true);
        Assert.False(host.TakeCancellingLine());

        var said = Said(host.NewerVersionNotStarted);

        Assert.Equal(new[] { "", Strings.Cli_Cancelling }, said);
        Assert.True(host.TakeCancellingLine());
    }

    [Fact]
    public void A_start_that_fails_with_no_Ctrl_C_held_back_says_nothing()
    {
        var host = new ConsoleUpdateHost("/s");
        host.NewerVersionAnswersCtrlC(true);

        var said = Said(host.NewerVersionNotStarted);

        Assert.Empty(said);
        Assert.True(host.TakeCancellingLine());
    }

    private static string[] Said(Action action)
    {
        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            action();
        }
        finally
        {
            Console.SetOut(original);
        }
        return buffer.ToString().Split(Environment.NewLine).Slice(..^1);
    }
}
