using System.Xml.Linq;

namespace InstallerClean.Tests.Themes;

// Four controls carry a boundary of their own, and each stands it down while the
// focus ring is drawn, so a control never wears two edges at once.
//
// The trap this guards is one the theme fell into once already, and it is
// invisible in the XAML, in a build and in a screenshot of a keyboard-driven
// walk through the app. IsKeyboardFocused looks like the right condition and is
// not: WPF draws the FocusVisualStyle adorner from
// FrameworkElement.OnGotKeyboardFocus, which calls
// KeyboardNavigation.ShowFocusVisual, which gates on
// `AlwaysShowFocusVisual || IsKeyboardMostRecentInputDevice()`. A mouse click
// takes keyboard focus and draws no ring. Keyed on focus, every one of these
// four loses its edge and gains nothing in its place at the exact moment a
// mouse user picks it, which is the commonest way any of them is used and the
// case the boundary was added for.
//
// FocusRing.IsShowing mirrors that expression, so it is the only condition
// allowed to take an edge away.
public class FocusRingSuppressionTests
{
    private const string Suppressor = "a11y:FocusRing.IsShowing";

    [Fact]
    public void Nothing_in_the_theme_takes_an_edge_away_on_any_other_signal()
    {
        var components = ThemeXaml.Load("ThemeXaml.Components.xaml");

        var suppressions = components.Descendants(ThemeXaml.Presentation + "Setter")
            .Where(s => (string?)s.Attribute("Property") == "BorderBrush"
                     && (string?)s.Attribute("Value") == "Transparent")
            .ToArray();

        // Four: the checkbox's box, both list rows and the backup-folder box.
        Assert.Equal(4, suppressions.Length);

        foreach (var setter in suppressions)
        {
            var conditions = ConditionsOf(setter.Parent!);

            Assert.True(conditions.Contains(Suppressor),
                $"An edge is cleared on [{string.Join(", ", conditions)}] rather than on {Suppressor}. "
                + "A mouse click satisfies IsKeyboardFocused and draws no ring, so that control "
                + "would show neither its own edge nor a ring.");
        }
    }

    [Fact]
    public void The_selection_rule_is_the_only_thing_the_row_suppression_undoes()
    {
        // Paired with the assertion above: the suppressor is allowed to take an
        // edge away and nothing else, so a later hand cannot hang a fill or a
        // thickness off it and reintroduce the jump the row geometry avoids.
        var components = ThemeXaml.Load("ThemeXaml.Components.xaml");

        var setters = components.Descendants(ThemeXaml.Presentation + "MultiTrigger")
            .Concat(components.Descendants(ThemeXaml.Presentation + "Trigger"))
            .Where(t => ConditionsOf(t).Contains(Suppressor))
            .SelectMany(t => t.Elements(ThemeXaml.Presentation + "Setter"))
            .Select(s => (string?)s.Attribute("Property"))
            .Distinct()
            .ToArray();

        Assert.Equal(new[] { "BorderBrush" }, setters);
    }

    /// <summary>
    /// The properties a trigger keys on, whether it is a single-condition
    /// Trigger or a MultiTrigger.
    /// </summary>
    private static HashSet<string> ConditionsOf(XElement trigger)
    {
        if (trigger.Name == ThemeXaml.Presentation + "MultiTrigger")
            return trigger.Element(ThemeXaml.Presentation + "MultiTrigger.Conditions")!
                .Elements(ThemeXaml.Presentation + "Condition")
                .Select(c => (string)c.Attribute("Property")!)
                .ToHashSet();

        var property = (string?)trigger.Attribute("Property");
        return property is null ? new HashSet<string>() : new HashSet<string> { property };
    }
}
