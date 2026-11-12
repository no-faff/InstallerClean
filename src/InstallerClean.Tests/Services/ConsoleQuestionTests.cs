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

    /// <summary>A console where <c>typed</c> is the one line read.</summary>
    private sealed class Typing(string typed) : IConsolePrompt
    {
        public void WriteLine(string line) { }

        public void Write(string text) { }

        public void DiscardTypedAhead() { }

        public string? ReadLine(CancellationToken token) => typed;
    }
}
