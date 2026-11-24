using System.Windows;
using System.Windows.Controls;
using InstallerClean.Helpers;
using InstallerClean.Tests.Themes;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The widths a card over a window's content is held to by
/// <see cref="DetailWindowSizing.KeepCardInsideRoom"/> and
/// <see cref="DetailWindowSizing.KeepMinWidthInsideRoom"/>, read off the card after
/// WPF lays its room out: taken at once from a room already laid out, and taken
/// again when the room's width changes. Each card keeps 24 clear on either side, as
/// the main window's cards do.
/// </summary>
public class DetailWindowSizingKeepInsideRoomTests
{
    private const double Margins = 48;

    [Fact]
    public void A_card_registered_on_a_room_already_laid_out_takes_its_widths_at_once()
    {
        LooseXaml.OnStaThread(() =>
        {
            var (window, room, card) = Build();
            LayOut(room, 400);

            window.KeepCardInsideRoom(card, room, minimumWidth: 340, maximumWidth: 520);

            AssertCardWidths(card, 400);
        });
    }

    [Fact]
    public void A_card_takes_its_widths_again_when_its_room_narrows()
    {
        LooseXaml.OnStaThread(() =>
        {
            var (window, room, card) = Build();
            window.KeepCardInsideRoom(card, room, minimumWidth: 340, maximumWidth: 520);

            LayOut(room, 2000);
            AssertCardWidths(card, 2000);

            LayOut(room, 400);
            AssertCardWidths(card, 400);
        });
    }

    [Fact]
    public void A_minimum_registered_on_a_room_already_laid_out_is_held_to_it_at_once()
    {
        LooseXaml.OnStaThread(() =>
        {
            var (window, room, card) = Build();
            card.MinWidth = 260;
            LayOut(room, 300);

            window.KeepMinWidthInsideRoom(card, room);

            Assert.Equal(252.0, card.MinWidth);
        });
    }

    [Fact]
    public void A_minimum_is_held_to_its_room_again_when_the_room_narrows_and_widens()
    {
        LooseXaml.OnStaThread(() =>
        {
            var (window, room, card) = Build();
            card.MinWidth = 260;
            window.KeepMinWidthInsideRoom(card, room);

            LayOut(room, 300);
            Assert.Equal(252.0, card.MinWidth);

            LayOut(room, 1000);
            Assert.Equal(260.0, card.MinWidth);
        });
    }

    private static (Window Window, Grid Room, Border Card) Build()
    {
        var card = new Border { Margin = new Thickness(Margins / 2) };
        var room = new Grid();
        room.Children.Add(card);
        return (new Window(), room, card);
    }

    private static void LayOut(FrameworkElement room, double width)
    {
        room.Measure(new Size(width, 500));
        room.Arrange(new Rect(0, 0, width, 500));
        room.UpdateLayout();
    }

    private static void AssertCardWidths(FrameworkElement card, double roomWidth)
    {
        var expected = DetailWindowSizing.CardWidths(340, 520,
            AccessibilitySettings.Current.TextScaleFactor, DetailWindowSizing.RoomLimit(roomWidth, Margins));
        Assert.Equal(expected, (card.MinWidth, card.MaxWidth));
    }
}
