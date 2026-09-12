using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

public class TooltipPlacementTests
{
    private const double Margin = 12;

    [Fact]
    public void A_box_with_room_where_it_wants_to_be_is_left_alone()
    {
        Assert.Equal(200, TooltipPlacement.LeftInsideWindow(
            preferredLeft: 200, popupWidth: 180, windowWidth: 500, margin: Margin));
    }

    [Fact]
    public void A_box_that_would_run_off_the_right_moves_only_as_far_as_it_has_to()
    {
        // 300 wide, wanting to start at 254, in a 500 window: its right edge
        // would land at 554. It comes back to 188, where the right edge is
        // exactly the margin in from the window's own edge, and no further.
        Assert.Equal(188, TooltipPlacement.LeftInsideWindow(
            preferredLeft: 254, popupWidth: 300, windowWidth: 500, margin: Margin));
    }

    [Fact]
    public void A_box_that_would_start_before_the_left_margin_is_pushed_in()
    {
        Assert.Equal(Margin, TooltipPlacement.LeftInsideWindow(
            preferredLeft: -188, popupWidth: 300, windowWidth: 500, margin: Margin));
    }

    [Fact]
    public void A_box_wider_than_the_window_keeps_the_left_margin()
    {
        // Nothing can hold both margins here, and the reader starts on the left.
        Assert.Equal(Margin, TooltipPlacement.LeftInsideWindow(
            preferredLeft: 100, popupWidth: 900, windowWidth: 500, margin: Margin));
    }

    [Fact]
    public void No_box_that_can_fit_is_ever_left_outside_the_window()
    {
        // The About window is a fixed 500 before the text scale multiplies it,
        // its tooltips wrap at 280 and carry 20 of padding, and the control they
        // hang on can sit anywhere along the row or drop to a second one. This
        // walks past every position and every width either of those can produce,
        // rather than pinning the sixteen a particular set of strings happens to
        // give today, which would stop being the real ones the moment one moved.
        var failures = new List<string>();
        for (var windowWidth = 400.0; windowWidth <= 1200; windowWidth += 50)
        for (var popupWidth = 40.0; popupWidth <= 320; popupWidth += 20)
        for (var preferredLeft = -200.0; preferredLeft <= windowWidth + 200; preferredLeft += 10)
        {
            var left = TooltipPlacement.LeftInsideWindow(
                preferredLeft, popupWidth, windowWidth, Margin);
            if (popupWidth + 2 * Margin > windowWidth) continue;
            if (left < Margin || left + popupWidth > windowWidth - Margin)
                failures.Add($"w={windowWidth} box={popupWidth} wanted={preferredLeft} -> {left}");
        }

        Assert.Empty(failures);
    }
}
