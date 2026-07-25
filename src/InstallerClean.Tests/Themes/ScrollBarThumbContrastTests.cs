namespace InstallerClean.Tests.Themes;

// The scrollbar thumb is the one control in the app a user has to FIND before
// they can use it: a button says what it is in words, and WCAG 1.4.11 lets a
// component be identified by its label for exactly that reason, but a thumb is
// nothing but its own fill. So its resting state carries a contrast floor, and
// the floor is what this pins rather than the colour that currently meets it: a
// later nudge to the alpha, to either slate surface, or an inlined colour on the
// Thumb template fails here with the measured number, which is not something the
// XAML, the build or a screenshot can report.
//
// Scoped deliberately to this one token. The other non-text ratios in the theme
// sit below 3:1 by decision, not by oversight (the About checkbox's box, whose
// state is conveyed by a 6.01:1 tick inside it, and the move-destination field,
// which is identified by its label and placeholder), so a sweep asserting 3:1
// across the palette would encode the opposite of what was decided.
public class ScrollBarThumbContrastTests
{
    // WCAG 2.1 SC 1.4.11 Non-text Contrast.
    private const double NonTextFloor = 3.0;

    [Theory]
    [InlineData("Surface.Card")]     // the Details list panes and every modal card
    [InlineData("Surface.Sidebar")]  // the About body and the window background behind it
    public void The_resting_thumb_clears_the_non_text_floor_on_every_surface_it_sits_on(string surface)
    {
        var tokens = ThemeXaml.Load("ThemeXaml.Tokens.xaml");
        var primitives = ThemeXaml.Load("ThemeXaml.Primitives.xaml");

        var thumb = ThemeXaml.ColourLiteral(tokens, primitives, "Scrollbar.Thumb");
        var behind = ThemeXaml.ColourLiteral(tokens, primitives, surface);

        var ratio = ThemeXaml.Contrast(thumb, behind);

        Assert.True(ratio >= NonTextFloor,
            $"Scrollbar.Thumb ({thumb}) is {ratio:0.00}:1 against {surface} ({behind}), " +
            $"under the {NonTextFloor:0.0}:1 WCAG 1.4.11 asks of a control identified by its fill alone.");
    }

    [Fact]
    public void Hover_stays_brighter_than_rest_by_a_margin_a_user_can_see()
    {
        // The rest state was raised towards the floor above, which narrowed the
        // gap to hover; this holds the two apart so a future raise cannot quietly
        // flatten the states into one and leave the bar looking unresponsive.
        var tokens = ThemeXaml.Load("ThemeXaml.Tokens.xaml");
        var primitives = ThemeXaml.Load("ThemeXaml.Primitives.xaml");

        var card = ThemeXaml.ColourLiteral(tokens, primitives, "Surface.Card");
        var rest = ThemeXaml.Contrast(ThemeXaml.ColourLiteral(tokens, primitives, "Scrollbar.Thumb"), card);
        var hover = ThemeXaml.Contrast(ThemeXaml.ColourLiteral(tokens, primitives, "Scrollbar.Thumb.Hover"), card);

        Assert.True(hover >= rest * 1.25,
            $"Hover is {hover:0.00}:1 against Surface.Card and rest {rest:0.00}:1, " +
            "too close for the brightening to register.");
    }

    [Fact]
    public void The_calculation_reproduces_the_ratios_the_theme_states_in_its_own_comments()
    {
        // Calibration, and the reason the numbers above can be trusted: three
        // ratios were measured into Tokens.xaml's comments independently of this
        // code, so if the contrast implementation here is wrong these disagree.
        var tokens = ThemeXaml.Load("ThemeXaml.Tokens.xaml");
        var primitives = ThemeXaml.Load("ThemeXaml.Primitives.xaml");
        var sidebar = ThemeXaml.ColourLiteral(tokens, primitives, "Surface.Sidebar");

        Assert.Equal(3.89, ThemeXaml.Contrast(
            ThemeXaml.ColourLiteral(tokens, primitives, "Decoration.Heart"), sidebar), precision: 2);
        Assert.Equal(4.90, ThemeXaml.Contrast(
            ThemeXaml.ColourLiteral(tokens, primitives, "Link.Rest"), sidebar), precision: 2);
        Assert.Equal(7.34, ThemeXaml.Contrast(
            ThemeXaml.ColourLiteral(tokens, primitives, "Link.Hover"), sidebar), precision: 2);
    }
}
