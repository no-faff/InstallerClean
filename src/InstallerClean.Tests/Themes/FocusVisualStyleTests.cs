using System.Xml.Linq;

namespace InstallerClean.Tests.Themes;

// .NET Framework draws an element's default focus visual from the Windows theme
// alone: KeyboardNavigation looks the default up in the system resources, never in
// the application's, so a style keyed on SystemParameters.FocusVisualStyleKey in the
// app's dictionaries is never drawn. The links and the scrolling regions, whose
// theme styles name no focus visual, carry the app's ring only by naming it.
public class FocusVisualStyleTests
{
    [Theory]
    [InlineData("SubtleLink")]
    [InlineData("BodyScrollRegion")]
    public void The_style_names_the_app_s_flat_ring(string key)
    {
        var style = Style(key);

        Assert.Contains(style.Elements(ThemeXaml.Presentation + "Setter"), setter =>
            (string?)setter.Attribute("Property") == "FocusVisualStyle"
            && (string?)setter.Attribute("Value") == "{StaticResource FlatTextBoxFocusVisual}");
    }

    // Every link and scrolling-region style there is, so one added beside these two
    // cannot leave its ring to the theme.
    [Theory]
    [InlineData("Hyperlink")]
    [InlineData("ScrollViewer")]
    public void Every_style_for_a_link_or_a_scrolling_region_names_a_focus_visual(string targetType)
    {
        var styles = Components().Root!.Elements(ThemeXaml.Presentation + "Style")
            .Where(s => (string?)s.Attribute("TargetType") == targetType)
            .ToList();
        Assert.NotEmpty(styles);

        var unnamed = styles
            .Where(s => !s.Elements(ThemeXaml.Presentation + "Setter")
                .Any(setter => (string?)setter.Attribute("Property") == "FocusVisualStyle"))
            .Select(s => (string?)s.Attribute(ThemeXaml.Xaml + "Key") ?? "(implicit)")
            .ToList();

        Assert.True(unnamed.Count == 0, string.Join(", ", unnamed));
    }

    private static XDocument Components() => ThemeXaml.Load("ThemeXaml.Components.xaml");

    private static XElement Style(string key) =>
        Components().Root!.Elements(ThemeXaml.Presentation + "Style")
            .Single(s => (string?)s.Attribute(ThemeXaml.Xaml + "Key") == key);
}
