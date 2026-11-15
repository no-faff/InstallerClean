using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// <see cref="ConsoleQuestion.Ask"/> reads an answer as starting with this language's yes or
/// no letter as .NET 10 matches it in the user's culture, through the ICU Windows ships. The
/// suite runs in English, whose letters are y and n.
/// </summary>
public class ConsoleQuestionTests
{
    private static ConsoleAnswer Answering(string typed)
    {
        return ConsoleQuestion.Ask(
            new Typing(typed), (yes, no) => $"[{yes}/{no}]", CancellationToken.None,
            TimeSpan.Zero, TimeSpan.FromMinutes(1), out _);
    }

    [Theory]
    [InlineData("y", "Yes")]
    [InlineData("Yes", "Yes")]
    [InlineData("", "Yes")]
    [InlineData("no", "No")]
    [InlineData("N", "No")]
    [InlineData("-y", "Other")]
    [InlineData("maybe", "Other")]
    public void An_answer_is_read_by_its_first_letter_as_NET_10_matches_it(string typed, string expected)
    {
        Assert.Equal(expected, Answering(typed).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("y")]
    [InlineData(null)]
    public void A_Ctrl_C_whose_handler_runs_after_the_read_has_ended_never_answers_yes(string? line)
    {
        // The read ends first, with Enter, a typed yes or no line, and the Ctrl+C handler
        // cancels the token a moment later, as Windows runs it on a thread of its own.
        using var cancel = new CancellationTokenSource();
        var console = new Ending(line, () => cancel.CancelAfter(TimeSpan.FromMilliseconds(50)));

        var answer = ConsoleQuestion.Ask(
            console, (yes, no) => $"[{yes}/{no}]", cancel.Token,
            TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1), out _);

        Assert.Equal(ConsoleAnswer.Cancelled, answer);
    }

    [Theory]
    [InlineData("")]
    [InlineData("y")]
    public void A_yes_with_no_Ctrl_C_is_yes_once_the_bound_has_passed(string line)
    {
        using var cancel = new CancellationTokenSource();

        var answer = ConsoleQuestion.Ask(
            new Typing(line), (yes, no) => $"[{yes}/{no}]", cancel.Token,
            TimeSpan.FromMilliseconds(50), TimeSpan.FromMinutes(1), out _);

        Assert.Equal(ConsoleAnswer.Yes, answer);
    }

    [Fact]
    public void A_no_does_not_wait_for_the_bound()
    {
        var started = DateTime.UtcNow;

        var answer = ConsoleQuestion.Ask(
            new Typing("n"), (yes, no) => $"[{yes}/{no}]", CancellationToken.None,
            TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1), out _);

        Assert.Equal(ConsoleAnswer.No, answer);
        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(5));
    }

    /// <summary>A console whose one read answers <c>line</c> and runs <c>after</c> as it returns.</summary>
    private sealed class Ending(string? line, Action after) : IConsolePrompt
    {
        public void WriteLine(string text) { }

        public void Write(string text) { }

        public void DiscardTypedAhead() { }

        public string? ReadLine(CancellationToken token)
        {
            after();
            return line;
        }
    }

    /// <summary>A console where <c>typed</c> is the one line read.</summary>
    private sealed class Typing(string typed) : IConsolePrompt
    {
        public void WriteLine(string line) { }

        public void Write(string text) { }

        public void DiscardTypedAhead() { }

        public string? ReadLine(CancellationToken token) => typed;
    }
}
