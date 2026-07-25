using System.Xml.Linq;

namespace InstallerClean.Tests.Themes;

// Selection has to survive the pointer. Both list-item templates declare an
// IsSelected trigger and an IsMouseOver trigger against the same Background, and
// WPF resolves that overlap by declaration order alone: with hover last, moving
// the pointer over a selected row replaced the selection fill with the hover fill
// and the selection appeared to vanish under the cursor. Every list in the app
// that a keyboard or screen-reader user navigates by selection is one of these
// two.
//
// Worth a test rather than a comment because the failure mode is silent in every
// other channel. Reordering the two triggers, or dropping the MultiTrigger that
// gives the overlap its own fill, leaves valid XAML that builds clean, passes
// every other test and only shows up as a row that flickers back to unselected
// when the mouse crosses it. Trigger order is also the exact kind of thing a
// later tidy-up reflows without noticing.
public class ListItemSelectionTriggerTests
{
    [Theory]
    [InlineData("ListViewItem")]  // the Orphaned grid and the Registered products grid
    [InlineData("ListBoxItem")]   // the Registered window's patch list
    public void Selection_is_declared_after_hover_so_a_selected_row_keeps_its_fill(string itemType)
    {
        var triggers = TemplateTriggers(itemType);

        var hover = IndexOfTrigger(triggers, "IsMouseOver");
        var selected = IndexOfTrigger(triggers, "IsSelected");

        Assert.True(selected > hover,
            $"{itemType}: IsSelected is declared at {selected} and IsMouseOver at {hover}. "
            + "The later setter wins, so hovering a selected row would repaint it with the hover fill.");
    }

    [Theory]
    [InlineData("ListViewItem")]
    [InlineData("ListBoxItem")]
    public void Selected_and_hovered_has_a_fill_of_its_own(string itemType)
    {
        // Order alone would leave a selected row inert under the pointer, which
        // reads as the row having stopped responding. The overlap therefore takes
        // the selection fill's own hover step, the same relationship the CheckBox
        // template uses for checked-and-hovered.
        var multi = TemplateTriggers(itemType)
            .Where(t => t.Name == ThemeXaml.Presentation + "MultiTrigger")
            .Single(t => Conditions(t).SetEquals(new[] { "IsSelected", "IsMouseOver" }));

        var setter = multi.Elements(ThemeXaml.Presentation + "Setter")
            .Single(s => (string?)s.Attribute("Property") == "Background");

        Assert.Equal("{StaticResource Action.Standard.Hover}", (string?)setter.Attribute("Value"));

        var selectionFill = TemplateTriggers(itemType)
            .Single(t => (string?)t.Attribute("Property") == "IsSelected")
            .Elements(ThemeXaml.Presentation + "Setter")
            .Single(s => (string?)s.Attribute("Property") == "Background");

        Assert.Equal("{StaticResource Action.Standard.Background}", (string?)selectionFill.Attribute("Value"));
    }

    /// <summary>
    /// The ControlTemplate.Triggers children of the implicit style for
    /// <paramref name="itemType"/>, in declaration order. Implicit, so the style
    /// carries no x:Key; a keyed variant would be a different role and is not
    /// what the app's lists pick up.
    /// </summary>
    private static XElement[] TemplateTriggers(string itemType)
    {
        var components = ThemeXaml.Load("ThemeXaml.Components.xaml");

        var style = components.Root!.Elements(ThemeXaml.Presentation + "Style")
            .Single(s => (string?)s.Attribute("TargetType") == itemType
                      && s.Attribute(ThemeXaml.Xaml + "Key") is null);

        return style.Elements(ThemeXaml.Presentation + "Setter")
            .Single(s => (string?)s.Attribute("Property") == "Template")
            .Element(ThemeXaml.Presentation + "Setter.Value")!
            .Element(ThemeXaml.Presentation + "ControlTemplate")!
            .Element(ThemeXaml.Presentation + "ControlTemplate.Triggers")!
            .Elements()
            .ToArray();
    }

    private static int IndexOfTrigger(XElement[] triggers, string property)
        => Assert.Single(triggers
            .Select((t, i) => (Trigger: t, Index: i))
            .Where(x => x.Trigger.Name == ThemeXaml.Presentation + "Trigger"
                     && (string?)x.Trigger.Attribute("Property") == property)
            .Select(x => x.Index));

    private static HashSet<string> Conditions(XElement multiTrigger)
        => multiTrigger.Element(ThemeXaml.Presentation + "MultiTrigger.Conditions")!
            .Elements(ThemeXaml.Presentation + "Condition")
            .Select(c => (string)c.Attribute("Property")!)
            .ToHashSet();
}
