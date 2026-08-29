using InstallerClean.Cli;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The breakdown the command line prints under the withholding, driven from the enum
/// itself rather than from a list a test author kept in step.
///
/// THIS IS THE HALF THAT MAKES THE BREAKDOWN PRINT BY CONSTRUCTION. The gate and the
/// host read one expression, so they cannot disagree about which conditions fired; what
/// that does not settle is whether a condition has anything to say for itself. A leg
/// added to the enum with no line would leave a reader a heading and one fewer reason
/// under it, with nothing failing and the output still looking like an answer.
///
/// A NON-EXHAUSTIVE SWITCH WOULD NOT CATCH IT. Nothing in this repository sets
/// TreatWarningsAsErrors and CI builds without it except on the audit codes, so the
/// compiler's warning is a warning nobody has to fix, and a warning nobody must fix is
/// not a guard.
/// </summary>
public class CliWithholdingReasonsTests
{
    [Fact]
    public void Every_leg_has_a_line_of_its_own()
    {
        var seen = new List<string>();

        foreach (var leg in Enum.GetValues<WithholdingLeg>())
        {
            var line = Program.LineFor(leg);

            Assert.False(string.IsNullOrWhiteSpace(line), $"{leg} prints nothing");
            Assert.NotEqual(Strings.Cli_WithheldReasons_Header, line);
            seen.Add(line);
        }

        // Distinct, because the fallback would otherwise satisfy the assertion above
        // for every leg at once and this test would pass over the very gap it exists
        // to close.
        Assert.Equal(seen.Count, seen.Distinct().Count());
    }

    [Fact]
    public void Every_line_is_indented_under_the_heading()
    {
        // Two leading spaces, as the help block's own list has. The heading is flush
        // and the reasons sit under it, which is what makes them read as a list rather
        // than as three more sentences.
        foreach (var leg in Enum.GetValues<WithholdingLeg>())
            Assert.StartsWith("  ", Program.LineFor(leg), StringComparison.Ordinal);

        Assert.False(Strings.Cli_WithheldReasons_Header.StartsWith(' '));
    }

    [Fact]
    public void No_line_carries_a_placeholder()
    {
        // None of them takes a count and none is formatted, so a placeholder here would
        // reach a reader as literal braces. It is also the rule the strings are written
        // to: these say which conditions the run met and attribute no files to any of
        // them, so there is nothing for a number to be.
        Assert.DoesNotContain("{0}", Strings.Cli_WithheldReasons_Header, StringComparison.Ordinal);

        foreach (var leg in Enum.GetValues<WithholdingLeg>())
            Assert.DoesNotContain("{", Program.LineFor(leg), StringComparison.Ordinal);
    }

    [Fact]
    public void A_leg_with_no_line_of_its_own_still_says_something()
    {
        // Cast past the enum's members deliberately: this is the state a fourth leg
        // would arrive in before anybody wrote its line. The heading is already on
        // screen by then, so the fallback repeats it rather than leaving a blank line
        // under it, which reads as output that failed.
        Assert.Equal(
            Strings.Cli_WithheldReasons_Header,
            Program.LineFor((WithholdingLeg)99));
    }
}
