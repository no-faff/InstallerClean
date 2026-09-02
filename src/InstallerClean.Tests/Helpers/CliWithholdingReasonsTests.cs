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

    // ---- The per-file half of the same list ----
    //
    // WRITTEN OUT IN PARALLEL RATHER THAN SHARED WITH THE FOUR ABOVE. The two mappings
    // answer to different types and a single generic pass over both would have to take
    // its members by reflection, which is what the last test in this file does and is
    // exactly the thing the four above are not doing: they name the enum so a reader
    // sees which set each assertion covers.

    [Fact]
    public void Every_arm_has_a_line_of_its_own()
    {
        var seen = new List<string>();

        foreach (var arm in Enum.GetValues<WithholdingSplitArm>())
        {
            var line = Program.LineFor(arm);

            Assert.False(string.IsNullOrWhiteSpace(line), $"{arm} prints nothing");
            Assert.NotEqual(Strings.Cli_WithheldReasons_Header, line);
            seen.Add(line);
        }

        // Distinct, because the fallback would otherwise satisfy the assertion above
        // for every arm at once and this test would pass over the very gap it exists
        // to close.
        Assert.Equal(seen.Count, seen.Distinct().Count());
    }

    [Fact]
    public void Every_arm_line_is_indented_under_the_heading()
    {
        foreach (var arm in Enum.GetValues<WithholdingSplitArm>())
            Assert.StartsWith("  ", Program.LineFor(arm), StringComparison.Ordinal);
    }

    [Fact]
    public void No_arm_line_carries_a_placeholder()
    {
        // None of them takes a count and none is formatted, so a placeholder here would
        // reach a reader as literal braces. It is also the rule the strings are written
        // to: these say which conditions the run met and attribute no files to any of
        // them, so there is nothing for a number to be.
        foreach (var arm in Enum.GetValues<WithholdingSplitArm>())
            Assert.DoesNotContain("{", Program.LineFor(arm), StringComparison.Ordinal);
    }

    [Fact]
    public void An_arm_with_no_line_of_its_own_still_says_something()
    {
        Assert.Equal(
            Strings.Cli_WithheldReasons_Header,
            Program.LineFor((WithholdingSplitArm)99));
    }

    [Fact]
    public void The_legs_and_the_arms_never_print_the_same_line()
    {
        // ONE HEADING CARRIES BOTH SETS AND A RUN CAN MEET CONDITIONS FROM EACH, so two
        // of them rendering identically would put the same sentence under that caption
        // twice and tell a reader one of the conditions had not been reported.
        var lines = Enum.GetValues<WithholdingLeg>().Select(leg => Program.LineFor(leg))
            .Concat(Enum.GetValues<WithholdingSplitArm>().Select(arm => Program.LineFor(arm)))
            .ToList();

        Assert.Equal(lines.Count, lines.Distinct().Count());
    }

    [Fact]
    public void Every_arm_of_the_split_has_a_line_or_is_the_wholesale_one()
    {
        // THE ONE TEST HERE THAT IS NOT DRIVEN OFF THE ENUM, and that is its whole
        // point: the enum cannot say whether it has kept up with WithholdingSplit. A
        // sixth arm counted into that struct with no member here would get no line, no
        // test above would notice, and the output would be a heading with one fewer
        // reason under it while every total still added up.
        //
        // Read off the struct's own primary constructor, so the set cannot go stale.
        // The suffix is dropped because the members are counts and the enum names the
        // decisions they count.
        var arms = typeof(WithholdingSplit).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First()
            .GetParameters()
            .Select(p => p.Name!)
            .Select(n => n.EndsWith("Count", StringComparison.Ordinal) ? n[..^"Count".Length] : n)
            .ToArray();

        // A set that came back empty would leave the loop below passing over no cases
        // at all, which reads exactly like a clean result. A floor rather than a count,
        // so that adding an arm does not fail this for the wrong reason.
        Assert.True(arms.Length >= 5, "the split's member list came back short");

        var named = Enum.GetNames<WithholdingSplitArm>().ToHashSet(StringComparer.Ordinal);

        foreach (var arm in arms)
        {
            // The wholesale arm is deliberately not among the members: the three leg
            // lines above speak for it, and a line here as well would say the same
            // thing twice about one machine. Spelled out rather than derived, so that
            // renaming that member fails this test rather than quietly widening the
            // exception.
            if (arm == "Wholesale") continue;

            Assert.True(named.Contains(arm),
                $"{arm} is an arm of the split with no line of its own");
        }
    }
}
