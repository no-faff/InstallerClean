using System.Xml.Linq;

namespace InstallerClean.Tests.Themes;

/// <summary>
/// The order of the three lines under the scan counts, read off the shipped
/// markup. It is in this folder because the instrument is the same one the
/// theme tests use: the window's XAML is embedded as plain bytes and read as
/// XML, which is the only way to hold a tie that is expressed as document
/// order and that no compiler looks at.
///
/// WHY THE ORDER IS A CLAIM RATHER THAN A LAYOUT DETAIL. The two held-back
/// lines are one sentence about two populations, the walk side and the
/// registered side of the same scan. They open on the same words and close on
/// the same verb, so a reader meets them as a pair, and a third line between
/// them reads as the end of one subject and the start of another. The
/// missing-files warning is the only one of the three carrying anything to do,
/// so it goes last, nearest the buttons.
///
/// The announcement follows the same order, and it is set somewhere else
/// entirely: the window raises a live region per property change, so the
/// sequence a screen reader hears is the order ScanViewModel assigns the
/// counts in, not the order of the branches that map them to elements. The
/// second test holds the two together.
/// </summary>
public class ScanFootnoteOrderTests
{
    private const string Walk = "NothingListedText";
    private const string Registered = "SupersededHeldBackText";
    private const string Warning = "MissingFromDiskBannerText";

    [Fact]
    public void The_two_held_back_lines_are_adjacent_and_the_warning_is_last()
    {
        var order = NamedElementOrder(Walk, Registered, Warning);

        Assert.Equal(new[] { Walk, Registered, Warning }, order);
    }

    [Fact]
    public void Each_of_the_three_appears_exactly_once()
    {
        // The order assertion above compares a sequence, so a name appearing
        // twice would still produce a passing prefix. This is what stops that
        // reading, and it is also what makes a rename fail loudly here rather
        // than silently dropping a line out of the comparison.
        var document = ThemeXaml.Load("ThemeXaml.MainWindow.xaml");
        foreach (var name in new[] { Walk, Registered, Warning })
            Assert.Single(document.Descendants(), e => NameOf(e) == name);
    }

    /// <summary>
    /// The three named elements in document order, which for a StackPanel is
    /// the order they are drawn in.
    /// </summary>
    private static string[] NamedElementOrder(params string[] names) =>
        ThemeXaml.Load("ThemeXaml.MainWindow.xaml")
            .Descendants()
            .Select(NameOf)
            .Where(n => n is not null && names.Contains(n))
            .Select(n => n!)
            .ToArray();

    private static string? NameOf(XElement element) =>
        (string?)element.Attribute(ThemeXaml.Xaml + "Name");
}
