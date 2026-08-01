namespace InstallerClean.Tests.Themes;

// Four controls in the app say what they are with a fill and nothing else: the
// About window's auto-update checkbox, a selected row in each of the two detail
// lists, and the backup-folder box. Those fills run from 1.22:1 to 2.33:1
// against what they are drawn on, and no repaint fixes that, because the theme's
// five opaque structural surfaces span 2.36:1 between the widest pair: there is
// no such thing as a quiet visible fill here, and the next step up the ramp is a
// body-text brightness. A LINE can clear the floor where a fill cannot, being
// drawn against a surface rather than being one, which is what Border.Control
// is for.
//
// So the floor moves onto the boundary token, and this pins it there. The
// failure it catches is silent everywhere else: repoint Border.Control at a
// darker slate, or nudge either surface, and the XAML still builds, every other
// test still passes, and the only symptom is a user who cannot tell which row
// the details pane is describing.
public class ControlBoundaryContrastTests
{
    // WCAG 2.1 SC 1.4.11 Non-text Contrast.
    private const double NonTextFloor = 3.0;

    [Theory]
    // The window behind the checkbox and the backup-folder box.
    [InlineData("Surface.Sidebar")]
    // The card behind a list row, and the fill of the backup-folder box.
    [InlineData("Surface.Card")]
    // A selected row's own fill and the checkbox's off state: the side of the
    // boundary away from the window, and the tightest pairing of the three.
    [InlineData("Action.Standard.Background")]
    public void The_boundary_clears_the_non_text_floor_on_both_sides_of_every_edge_it_draws(string surface)
    {
        var tokens = ThemeXaml.Load("ThemeXaml.Tokens.xaml");
        var primitives = ThemeXaml.Load("ThemeXaml.Primitives.xaml");

        var boundary = ThemeXaml.ColourLiteral(tokens, primitives, "Border.Control");
        var behind = ThemeXaml.ColourLiteral(tokens, primitives, surface);

        var ratio = ThemeXaml.Contrast(boundary, behind);

        Assert.True(ratio >= NonTextFloor,
            $"Border.Control ({boundary}) is {ratio:0.00}:1 against {surface} ({behind}), " +
            $"under the {NonTextFloor:0.0}:1 WCAG 1.4.11 asks of the boundary that identifies a control.");
    }

    [Fact]
    public void The_progress_fill_clears_the_floor_against_its_own_track()
    {
        // A progress indicator's fill against its track is the textbook 1.4.11
        // case, and it is the whole control: the bar carries no label of its
        // own. All three bars (splash, scanning overlay, operating overlay)
        // share one implicit style, so this covers each of them; neither
        // overlay dims what is behind its bar, the card being opaque over the
        // dim and the track opaque over the card.
        var tokens = ThemeXaml.Load("ThemeXaml.Tokens.xaml");
        var primitives = ThemeXaml.Load("ThemeXaml.Primitives.xaml");

        var fill = ThemeXaml.ColourLiteral(tokens, primitives, "Progress.Fill");
        var track = ThemeXaml.ColourLiteral(tokens, primitives, "Surface.Sidebar");

        var ratio = ThemeXaml.Contrast(fill, track);

        Assert.True(ratio >= NonTextFloor,
            $"Progress.Fill ({fill}) is {ratio:0.00}:1 against its track ({track}), " +
            $"under the {NonTextFloor:0.0}:1 floor. The accent fill it used to share sits at 2.33:1.");
    }

    [Fact]
    public void The_progress_fill_is_its_own_role_rather_than_the_accent_the_buttons_wear()
    {
        // The bar deliberately no longer matches the accent buttons, which was
        // compared at true size and accepted rather than overlooked. Pointing
        // Progress.Fill back at the accent would look like tidying two tokens
        // that resolve to different colours into one, and would put the bar
        // back under the floor above.
        var tokens = ThemeXaml.Load("ThemeXaml.Tokens.xaml");
        var primitives = ThemeXaml.Load("ThemeXaml.Primitives.xaml");

        Assert.NotEqual(
            ThemeXaml.ColourLiteral(tokens, primitives, "Action.Accent.Background"),
            ThemeXaml.ColourLiteral(tokens, primitives, "Progress.Fill"));
    }

    [Fact]
    public void The_calculation_still_reproduces_the_ratios_the_theme_states_in_its_own_comments()
    {
        // Calibration against the two figures Tokens.xaml states for the new
        // tokens, measured independently of this code, as
        // ScrollBarThumbContrastTests does for the three older ones.
        var tokens = ThemeXaml.Load("ThemeXaml.Tokens.xaml");
        var primitives = ThemeXaml.Load("ThemeXaml.Primitives.xaml");

        var boundary = ThemeXaml.ColourLiteral(tokens, primitives, "Border.Control");

        Assert.Equal(4.72, ThemeXaml.Contrast(
            boundary, ThemeXaml.ColourLiteral(tokens, primitives, "Surface.Sidebar")), precision: 2);
        Assert.Equal(5.76, ThemeXaml.Contrast(
            boundary, ThemeXaml.ColourLiteral(tokens, primitives, "Surface.Card")), precision: 2);
        Assert.Equal(3.34, ThemeXaml.Contrast(
            boundary, ThemeXaml.ColourLiteral(tokens, primitives, "Action.Standard.Background")), precision: 2);
        Assert.Equal(4.90, ThemeXaml.Contrast(
            ThemeXaml.ColourLiteral(tokens, primitives, "Progress.Fill"),
            ThemeXaml.ColourLiteral(tokens, primitives, "Surface.Sidebar")), precision: 2);
    }
}
