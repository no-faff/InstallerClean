using System.Xml.Linq;

namespace InstallerClean.Tests.Themes;

// The dialogs that size to their card take the card's widths from
// DetailWindowSizing.KeepCardInsideWorkArea in code-behind, which holds them to the
// work area. A width written on the card in XAML is replaced there, so it would
// describe a card that is never drawn, and it would come back unheld the moment the
// call went. So the card is named Card, for the code-behind, and carries no width.
public class DialogCardWidthTests
{
    private static readonly XNamespace Xaml = ThemeXaml.Xaml;
    private static readonly XNamespace Presentation = ThemeXaml.Presentation;

    [Theory]
    [InlineData("ThemeXaml.ConfirmDeleteWindow.xaml")]
    [InlineData("ThemeXaml.ConfirmMoveWindow.xaml")]
    [InlineData("ThemeXaml.MessageWindow.xaml")]
    [InlineData("ThemeXaml.ReportWindow.xaml")]
    [InlineData("ThemeXaml.NoticesWindow.xaml")]
    public void The_card_is_the_window_and_carries_no_width_of_its_own(string resource)
    {
        var window = ThemeXaml.Load(resource).Root!;

        Assert.Equal("WidthAndHeight", (string?)window.Attribute("SizeToContent"));

        var card = Assert.Single(window.Elements(), e => e.Name.Namespace == Presentation
            && !e.Name.LocalName.Contains('.'));
        Assert.Equal(Presentation + "Border", card.Name);
        Assert.Equal("Card", (string?)card.Attribute(Xaml + "Name"));

        foreach (var property in new[] { "Width", "MinWidth", "MaxWidth" })
            Assert.True(card.Attribute(property) is null,
                $"The card in {resource} sets {property}, which code-behind sets and holds to the work area.");
    }
}
