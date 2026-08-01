using System.Globalization;
using System.Xml.Linq;

namespace InstallerClean.Tests.Themes;

// A selected row draws a rule along its top and bottom edges, because its fill
// is 1.72:1 against the card behind it and in the Registered products list that
// fill is the only thing saying which row the whole right-hand pane is
// describing.
//
// Two things about it are easy to undo by accident and invisible when you do.
//
// The first is the row HEIGHT. A WPF Border adds its thickness to layout, so the
// obvious way to write this, setting BorderThickness inside the IsSelected
// trigger, makes the selected row 2px taller than its neighbours and the list
// shuffles under the selection as it moves. The thickness is therefore on the
// Border in every state and only the brush changes, with the row's own padding
// reduced by the pixel the border now takes. That is arithmetic, so it is
// checkable here rather than by eye, and nothing else would report it: the XAML
// builds either way and a screenshot of a still list looks identical.
//
// The second is the pair of conditions the arrangement was accepted under: top
// and bottom only, no vertical caps, so the row reads as picked out rather than
// fenced in; and the rule stands down while the row has keyboard focus, because
// the 2px focus ring is already an edge and two boundaries doing one job is
// noise.
public class ListRowBoundaryTests
{
    [Theory]
    // The vertical box each row had before the rule was added: 4,6 padding on
    // the ListView rows and 2,2 on the patch list's, twice over.
    [InlineData("ListViewItem", 12.0)]
    [InlineData("ListBoxItem", 4.0)]
    public void A_row_is_exactly_as_tall_as_it_was_and_the_same_height_in_every_state(
        string itemType, double verticalBoxBefore)
    {
        var border = RowBorder(itemType);
        var padding = Thickness(border, "Padding");
        var thickness = Thickness(border, "BorderThickness");

        var verticalBoxNow = thickness.Top + padding.Top + padding.Bottom + thickness.Bottom;

        Assert.Equal(verticalBoxBefore, verticalBoxNow);

        // The other half of the same claim: a state cannot change the geometry,
        // because no trigger touches either property.
        var geometrySetters = TemplateSetters(itemType)
            .Where(s => (string?)s.Attribute("TargetName") == (string?)border.Attribute(ThemeXaml.Xaml + "Name"))
            .Select(s => (string?)s.Attribute("Property"))
            .Where(p => p is "BorderThickness" or "Padding")
            .ToArray();

        Assert.Empty(geometrySetters);
    }

    [Theory]
    [InlineData("ListViewItem")]
    [InlineData("ListBoxItem")]
    public void The_rule_runs_along_the_top_and_bottom_and_has_no_vertical_caps(string itemType)
    {
        var thickness = Thickness(RowBorder(itemType), "BorderThickness");

        Assert.Equal(0.0, thickness.Left);
        Assert.Equal(0.0, thickness.Right);
        Assert.Equal(1.0, thickness.Top);
        Assert.Equal(1.0, thickness.Bottom);
    }

    [Theory]
    [InlineData("ListViewItem")]
    [InlineData("ListBoxItem")]
    public void Selection_draws_the_rule_and_keyboard_focus_takes_it_away_again(string itemType)
    {
        var triggers = TemplateTriggers(itemType);

        var selection = triggers
            .Single(t => t.Name == ThemeXaml.Presentation + "Trigger"
                      && (string?)t.Attribute("Property") == "IsSelected");
        var drawn = selection.Elements(ThemeXaml.Presentation + "Setter")
            .Single(s => (string?)s.Attribute("Property") == "BorderBrush");

        Assert.Equal("{StaticResource Border.Control}", (string?)drawn.Attribute("Value"));

        var focused = triggers
            .Where(t => t.Name == ThemeXaml.Presentation + "MultiTrigger")
            .Single(t => Conditions(t).SetEquals(new[] { "IsSelected", "IsKeyboardFocused" }));
        var cleared = focused.Elements(ThemeXaml.Presentation + "Setter")
            .Single(s => (string?)s.Attribute("Property") == "BorderBrush");

        Assert.Equal("Transparent", (string?)cleared.Attribute("Value"));

        // Later setters win, so the suppression is only a suppression while it
        // is declared after the selection rule it undoes.
        Assert.True(Array.IndexOf(triggers, focused) > Array.IndexOf(triggers, selection),
            $"{itemType}: the focus MultiTrigger is declared before the IsSelected trigger, "
            + "so a focused row would keep the rule as well as the ring.");
    }

    private static XElement RowBorder(string itemType)
        => ItemTemplate(itemType).Element(ThemeXaml.Presentation + "Border")!;

    private static XElement[] TemplateTriggers(string itemType)
        => ItemTemplate(itemType)
            .Element(ThemeXaml.Presentation + "ControlTemplate.Triggers")!
            .Elements()
            .ToArray();

    /// <summary>Every Setter inside the template's triggers, MultiTriggers included.</summary>
    private static XElement[] TemplateSetters(string itemType)
        => TemplateTriggers(itemType)
            .SelectMany(t => t.Elements(ThemeXaml.Presentation + "Setter"))
            .ToArray();

    /// <summary>
    /// The ControlTemplate of the implicit style for <paramref name="itemType"/>.
    /// Implicit, so the style carries no x:Key; a keyed variant would be a
    /// different role and is not what the app's lists pick up.
    /// </summary>
    private static XElement ItemTemplate(string itemType)
    {
        var components = ThemeXaml.Load("ThemeXaml.Components.xaml");

        return components.Root!.Elements(ThemeXaml.Presentation + "Style")
            .Single(s => (string?)s.Attribute("TargetType") == itemType
                      && s.Attribute(ThemeXaml.Xaml + "Key") is null)
            .Elements(ThemeXaml.Presentation + "Setter")
            .Single(s => (string?)s.Attribute("Property") == "Template")
            .Element(ThemeXaml.Presentation + "Setter.Value")!
            .Element(ThemeXaml.Presentation + "ControlTemplate")!;
    }

    private static HashSet<string> Conditions(XElement multiTrigger)
        => multiTrigger.Element(ThemeXaml.Presentation + "MultiTrigger.Conditions")!
            .Elements(ThemeXaml.Presentation + "Condition")
            .Select(c => (string)c.Attribute("Property")!)
            .ToHashSet();

    /// <summary>
    /// WPF's own Thickness shorthand: one value for all four sides, two for
    /// left/right then top/bottom, four for left, top, right, bottom.
    /// </summary>
    private static (double Left, double Top, double Right, double Bottom) Thickness(
        XElement element, string attribute)
    {
        var raw = (string?)element.Attribute(attribute)
            ?? throw new InvalidOperationException($"The row Border has no {attribute}.");

        var parts = raw.Split(',')
            .Select(p => double.Parse(p.Trim(), CultureInfo.InvariantCulture))
            .ToArray();

        return parts.Length switch
        {
            1 => (parts[0], parts[0], parts[0], parts[0]),
            2 => (parts[0], parts[1], parts[0], parts[1]),
            4 => (parts[0], parts[1], parts[2], parts[3]),
            _ => throw new InvalidOperationException($"'{raw}' is not a Thickness."),
        };
    }
}
