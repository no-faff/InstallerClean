using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Where a dialog's left or top edge goes when its size changes: moved by half the
/// change so its centre stays put, an edge the change carries out of the work area
/// brought back to it, and an edge already outside it left there. The work area
/// runs from 0 to 1000.
/// </summary>
public class DetailWindowSizingRecentredTests
{
    [Fact]
    public void A_shrinking_window_keeps_its_centre()
    {
        // 400 wide at 300, centre 500; 300 wide keeps 500 at 350.
        Assert.Equal(350.0, DetailWindowSizing.Recentred(300, 400, 300, 0, 1000));
    }

    [Fact]
    public void A_growing_window_keeps_its_centre()
    {
        Assert.Equal(250.0, DetailWindowSizing.Recentred(300, 400, 500, 0, 1000));
    }

    [Fact]
    public void A_far_edge_the_growth_carries_out_is_brought_back()
    {
        // 400 wide at 590 ends at 990; 500 wide about the same centre would end at 1040.
        Assert.Equal(500.0, DetailWindowSizing.Recentred(590, 400, 500, 0, 1000));
    }

    [Fact]
    public void A_near_edge_the_growth_carries_out_is_brought_back()
    {
        Assert.Equal(0.0, DetailWindowSizing.Recentred(10, 400, 500, 0, 1000));
    }

    [Fact]
    public void An_edge_already_outside_the_work_area_stays_outside()
    {
        // Dragged so it starts on the monitor to the left, and on across the far edge.
        Assert.Equal(-250.0, DetailWindowSizing.Recentred(-200, 400, 500, 0, 1000));
        Assert.Equal(850.0, DetailWindowSizing.Recentred(900, 400, 500, 0, 1000));
    }

    [Fact]
    public void A_far_edge_flush_with_the_work_area_or_a_rounding_past_it_is_held_there()
    {
        // 400 high ending at 1000, and at 1000.3, growing to 500.
        Assert.Equal(500.0, DetailWindowSizing.Recentred(600, 400, 500, 0, 1000));
        Assert.Equal(500.0, DetailWindowSizing.Recentred(600.3, 400, 500, 0, 1000));
    }

    [Fact]
    public void A_near_edge_flush_with_the_work_area_or_a_rounding_before_it_is_held_there()
    {
        Assert.Equal(0.0, DetailWindowSizing.Recentred(0, 400, 500, 0, 1000));
        Assert.Equal(0.0, DetailWindowSizing.Recentred(-0.3, 400, 500, 0, 1000));
    }

    [Fact]
    public void A_window_wider_than_the_work_area_keeps_its_near_edge_inside()
    {
        Assert.Equal(0.0, DetailWindowSizing.Recentred(100, 800, 1200, 0, 1000));
    }
}
