using System.Globalization;
using System.Reflection;
using System.Xml.Linq;

namespace InstallerClean.Tests.Themes;

// The Details list panes inset their scrollbar tracks from the card ends so
// the thumb never rides into the corner curve, where it would draw outside
// the visible pane (a rounded Border paints only inside its curve while
// ClipToBounds clips at the rectangular layout bounds). That geometry rests
// on two numeric ties XAML cannot express and a build cannot check:
//
//   1. Padding.ScrollBarCardEnds / ...Horizontal must equal Radius.Pill,
//      or the travel stops short of, or crosses into, the curve.
//   2. The scoped ScrollBar margins in the two Details windows must mirror
//      the ListView bleed margin exactly, or the insets measure from the
//      bled edge rather than the true card edge.
//
// Each would drift silently: the XAML stays valid, the app runs, and only a
// close look at a corner at the end of scroll travel would show it. The
// XAML sources are embedded in this assembly (see the csproj) and read as
// plain XML.
public class ScrollBarCardInsetTests
{
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [Fact]
    public void Card_scrollbar_end_insets_equal_the_card_radius()
    {
        var tokens = LoadXaml("ThemeXaml.Tokens.xaml");

        var radius = ParseUniform(ResourceValue(tokens, "Radius.Pill"));

        var vertical = ParseThickness(ResourceValue(tokens, "Padding.ScrollBarCardEnds"));
        Assert.Equal((0, radius, 0, radius), vertical);

        var horizontal = ParseThickness(ResourceValue(tokens, "Padding.ScrollBarCardEndsHorizontal"));
        Assert.Equal((radius, 0, radius, 0), horizontal);
    }

    [Theory]
    [InlineData("ThemeXaml.OrphanedFilesWindow.xaml", "FilesList")]
    [InlineData("ThemeXaml.RegisteredFilesWindow.xaml", "ProductsList")]
    public void Scoped_scrollbar_margins_mirror_the_list_bleed(string resource, string listName)
    {
        var window = LoadXaml(resource);

        var listView = window.Descendants(Presentation + "ListView")
            .Single(e => (string?)e.Attribute(Xaml + "Name") == listName);
        var (bleedLeft, bleedTop, bleedRight, bleedBottom) =
            ParseThickness((string)listView.Attribute("Margin")!);

        var scoped = listView.Element(Presentation + "ListView.Resources")!
            .Elements(Presentation + "Style")
            .Single(s => (string?)s.Attribute("TargetType") == "ScrollBar");

        // The vertical bar's margin undoes the bleed on every side, putting
        // the track back on the card rect the end insets are sized for.
        Assert.Equal(
            (-bleedLeft, -bleedTop, -bleedRight, -bleedBottom),
            ParseThickness(SetterValue(scoped, "Margin")));

        // The horizontal bar lifts out of the bottom bleed only: its bottom
        // must land on the card's bottom edge, whole rather than clipped,
        // and nothing bleeds it at the top.
        var horizontalTrigger = scoped.Element(Presentation + "Style.Triggers")!
            .Elements(Presentation + "Trigger")
            .Single(t => (string?)t.Attribute("Property") == "Orientation"
                      && (string?)t.Attribute("Value") == "Horizontal");
        Assert.Equal((0, 0, 0, -bleedBottom), ParseThickness(SetterValue(horizontalTrigger, "Margin")));
    }

    private static XDocument LoadXaml(string logicalName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"Embedded resource '{logicalName}' is missing.");
        return XDocument.Load(stream);
    }

    private static string ResourceValue(XDocument tokens, string key)
    {
        var element = tokens.Root!.Elements()
            .Single(e => (string?)e.Attribute(Xaml + "Key") == key);
        return element.Value.Trim();
    }

    private static string SetterValue(XElement scope, string property)
        => (string)scope.Elements(Presentation + "Setter")
            .Single(s => (string?)s.Attribute("Property") == property)
            .Attribute("Value")!;

    /// <summary>
    /// A CornerRadius whose four corners must agree, returned as the one
    /// value. Radius tokens are written uniform; a token split into four
    /// unequal corners would invalidate the inset geometry, so that fails
    /// rather than picking a corner.
    /// </summary>
    private static double ParseUniform(string cornerRadius)
        => Assert.Single(Split(cornerRadius).Distinct());

    /// <summary>
    /// Thickness in its 1, 2 or 4 value forms, as left/top/right/bottom.
    /// A tuple rather than System.Windows.Thickness so the assertions
    /// need no WPF type initialisation.
    /// </summary>
    private static (double Left, double Top, double Right, double Bottom) ParseThickness(string thickness)
    {
        var p = Split(thickness);
        return p.Length switch
        {
            1 => (p[0], p[0], p[0], p[0]),
            2 => (p[0], p[1], p[0], p[1]),
            4 => (p[0], p[1], p[2], p[3]),
            _ => throw new FormatException($"'{thickness}' is not a Thickness."),
        };
    }

    private static double[] Split(string value)
        => value.Split(',', StringSplitOptions.TrimEntries)
            .Select(v => double.Parse(v, CultureInfo.InvariantCulture))
            .ToArray();
}
