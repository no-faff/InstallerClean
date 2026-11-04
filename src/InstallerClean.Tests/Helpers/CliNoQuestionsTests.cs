using InstallerClean.Cli;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// A run asks nothing more at the console where its own update question went unanswered,
/// and where it was started with <see cref="CliContract.NoQuestionsSwitch"/>, which a copy
/// whose question went unanswered passes to the newer version. The update question and the
/// report offer are each put only where <see cref="Program.MayAsk"/> answers true.
/// </summary>
public class CliNoQuestionsTests
{
    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    [InlineData("/s")]
    public void A_run_started_without_the_switch_asks_where_the_console_can(string command)
    {
        var host = Program.NewUpdateHost(command, CliContract.ParseArguments([command]));

        Assert.False(host.AsksNothingMore);
        Assert.True(Program.MayAsk(consoleCanAsk: true, host));
    }

    [Fact]
    public void A_run_whose_update_question_went_unanswered_asks_nothing_more()
    {
        var host = Program.NewUpdateHost("/s", CliContract.ParseArguments(["/s"]));

        host.NobodyAnswered();

        Assert.True(host.AsksNothingMore);
        Assert.False(Program.MayAsk(consoleCanAsk: true, host));
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    [InlineData("/s")]
    public void A_newer_version_given_the_switch_asks_nothing(string command)
    {
        // The arguments as the newer version reads them once the replace switches and the
        // Ctrl+C event are taken out.
        var host = Program.NewUpdateHost(command,
            CliContract.ParseArguments([command, CliContract.NoUpdateSwitch, CliContract.NoQuestionsSwitch]));

        Assert.True(host.AsksNothingMore);
        Assert.False(Program.MayAsk(consoleCanAsk: true, host));
    }

    [Fact]
    public void A_run_given_the_switch_without_no_update_asks_no_update_question()
    {
        // The update still runs; its question is not put, and the run goes on as a run
        // nobody can answer does.
        var invocation = CliContract.ParseArguments(["/s", CliContract.NoQuestionsSwitch]);
        var host = Program.NewUpdateHost("/s", invocation);

        Assert.False(invocation.SkipUpdate);
        Assert.False(Program.MayAsk(consoleCanAsk: true, host));
    }

    [Fact]
    public void A_console_nobody_can_answer_in_is_asked_nothing_whatever_the_switch()
    {
        var host = Program.NewUpdateHost("/s", CliContract.ParseArguments(["/s"]));

        Assert.False(Program.MayAsk(consoleCanAsk: false, host));
    }
}
