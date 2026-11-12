using InstallerClean.Services;
using NSubstitute;

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
        var console = Substitute.For<IConsolePrompt>();
        console.ReadLine(Arg.Any<CancellationToken>()).Returns(typed);
        return ConsoleQuestion.Ask(
            console, (yes, no) => $"[{yes}/{no}]", CancellationToken.None,
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
}
