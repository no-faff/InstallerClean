using System.Linq;
using System.Windows;
using System.Windows.Controls;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.Tests.Themes;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Text copied out of a selectable box is the text before it was drawn: the word
/// joiners and zero-width spaces <see cref="InstallerPathText"/> adds for line
/// breaking stay on screen and do not reach the clipboard, and any the text held
/// before it was drawn do.
///
/// Every comparison here is by code point. xUnit's string equality treats those two
/// characters as nothing, so <c>Assert.Equal</c> on two strings differing only by
/// them passes.
/// </summary>
public class InstallerPathTextCopyingTests
{
    // Spelled as escapes, never typed, for the reason the code under test gives.
    private const char ZeroWidthSpace = '\u200B';
    private const char WordJoiner = '\u2060';

    // A spelled-out log path rather than the host's own, as in
    // InstallerPathTextLogPathTests, so there are separators to break at on every
    // machine.
    private const string LogPath = @"C:\Users\U-ser\AppData\Local\InstallerClean\crash.log";

    [Fact]
    public void A_copy_of_a_drawn_message_is_the_message()
    {
        var message = $"The scan could not read {InstallerCacheHelpers.InstallerFolder}. "
            + $"Nothing has been removed. This is also recorded in {LogPath}.";
        var drawn = InstallerPathText.AllowFolderBreaksIn(InstallerPathText.KeepWhole(message), LogPath);

        // The drawn text carries the characters, or the comparison below proves
        // nothing.
        Assert.True(drawn.Contains(ZeroWidthSpace), "the drawn message carries no zero-width space");
        Assert.True(drawn.Contains(WordJoiner), "the drawn message carries no word joiner");

        AssertSameCodePoints(message, InstallerPathText.ForCopying(drawn, message, 0, drawn.Length)!);
    }

    [Fact]
    public void A_copy_of_a_message_drawn_as_the_message_dialog_draws_it_is_the_message()
    {
        var message = $"The scan could not read {InstallerCacheHelpers.InstallerFolder}. "
            + $"This is also recorded in {CrashLog.LogPath}.";
        var drawn = InstallerPathText.ForDrawing(message);

        AssertSameCodePoints(message, InstallerPathText.ForCopying(drawn, message, 0, drawn.Length)!);
    }

    // A folder's name may hold either character, in its middle or straight after a
    // backslash, where a treatment adds its own.
    [Theory]
    [InlineData(@"C:\Back\u200Bups\IC")]
    [InlineData(@"C:\\u200BBackups\IC")]
    [InlineData(@"C:\Backups\\u2060IC\\u200B")]
    [InlineData(@"\\server\\u200Bshare\IC")]
    public void A_character_the_path_itself_holds_is_copied(string escaped)
    {
        var path = Unescape(escaped);
        var message = $"Moving files to {path}.";
        var drawn = InstallerPathText.AllowFolderBreaksInAnyPath(InstallerPathText.ForDrawing(message));

        Assert.NotEqual(message.Length, drawn.Length);
        AssertSameCodePoints(message, InstallerPathText.ForCopying(drawn, message, 0, drawn.Length)!);

        var logged = $"This is also recorded in {path}.";
        var drawnLog = InstallerPathText.AllowFolderBreaksIn(logged, path);
        AssertSameCodePoints(logged, InstallerPathText.ForCopying(drawnLog, logged, 0, drawnLog.Length)!);
    }

    [Fact]
    public void A_copy_of_part_of_the_text_is_that_part_as_it_was()
    {
        var folder = InstallerCacheHelpers.InstallerFolder;
        var message = $"The scan could not read {folder}.";
        var drawn = InstallerPathText.ForDrawing(message);
        var drawnFolder = InstallerPathText.KeepWhole(folder);
        var at = drawn.IndexOf(drawnFolder, StringComparison.Ordinal);
        Assert.True(at > 0, "the drawn message does not hold the drawn folder");

        AssertSameCodePoints(folder, InstallerPathText.ForCopying(drawn, message, at, drawnFolder.Length)!);
        AssertSameCodePoints("The scan", InstallerPathText.ForCopying(drawn, message, 0, 8)!);
    }

    // The log path's treatment puts a zero-width space after every backslash, so a folder
    // whose name opens with one is drawn with two side by side, the added one first. A
    // selection starting between them starts at the folder's own.
    [Fact]
    public void A_selection_starting_between_an_added_character_and_the_text_s_own_keeps_the_own()
    {
        var path = Unescape(@"C:\\u200BBackups\IC\crash.log");
        var logged = $"Recorded in {path}.";
        var drawn = InstallerPathText.AllowFolderBreaksIn(logged, path);
        var pair = drawn.IndexOf(new string(ZeroWidthSpace, 2), StringComparison.Ordinal);
        Assert.True(pair > 0, "the drawn line holds no pair of zero-width spaces");

        var copied = InstallerPathText.ForCopying(drawn, logged, pair + 1, drawn.Length - pair - 1)!;

        Assert.Equal(ZeroWidthSpace, copied[0]);
        Assert.Equal('B', copied[1]);
    }

    [Fact]
    public void Text_no_treatment_touched_is_copied_as_it_is()
    {
        var text = "scoop update installerclean \u200B";
        Assert.Equal("update", InstallerPathText.ForCopying(text, text, 6, 6));
        AssertSameCodePoints(text, InstallerPathText.ForCopying(text, text, 0, text.Length)!);
    }

    [Fact]
    public void Text_that_is_not_the_untreated_text_drawn_maps_to_nothing()
    {
        Assert.Null(InstallerPathText.ForCopying("abc", "abd", 0, 3));
        Assert.Null(InstallerPathText.ForCopying("ab", "abc", 0, 2));
        Assert.Null(InstallerPathText.ForCopying("ab\u00ADc", "abc", 0, 4));
        Assert.Null(InstallerPathText.ForCopying("abc", "abc", 2, 2));
        Assert.Null(InstallerPathText.ForCopying("abc", "abc", -1, 1));
    }

    // A TextBox puts its selection on the data object in each text format before it
    // raises Copying; the box's handler rewrites each one holding the selection.
    [Fact]
    public void A_box_given_its_untreated_text_copies_the_selection_as_it_was()
    {
        LooseXaml.OnStaThread(() =>
        {
            var path = Unescape(@"C:\Back\u200Bups\IC");
            var message = $"Could not move to {path} or {InstallerCacheHelpers.InstallerFolder}.";
            var box = new TextBox();
            PlainCopy.SetUntreated(box, message);
            box.Text = InstallerPathText.AllowFolderBreaksInAnyPath(InstallerPathText.ForDrawing(message));
            box.SelectAll();

            var data = new DataObject();
            data.SetData(DataFormats.UnicodeText, box.SelectedText, true);
            data.SetData(DataFormats.Text, box.SelectedText, true);
            box.RaiseEvent(new DataObjectCopyingEventArgs(data, isDragDrop: false));

            AssertSameCodePoints(message, (string)data.GetData(DataFormats.UnicodeText, false));
            AssertSameCodePoints(message, (string)data.GetData(DataFormats.Text, false));
        });
    }

    [Fact]
    public void A_box_given_no_untreated_text_copies_what_it_shows()
    {
        LooseXaml.OnStaThread(() =>
        {
            var shown = Unescape(@"C:\Back\u200Bups\IC");
            var box = new TextBox { Text = shown };
            box.SelectAll();

            var data = new DataObject();
            data.SetData(DataFormats.UnicodeText, box.SelectedText, true);
            box.RaiseEvent(new DataObjectCopyingEventArgs(data, isDragDrop: false));

            AssertSameCodePoints(shown, (string)data.GetData(DataFormats.UnicodeText, false));
        });
    }

    // A box whose text does not map onto its untreated text copies its selection with
    // both characters taken out.
    [Fact]
    public void A_box_whose_text_is_not_its_untreated_text_drawn_copies_without_either_character()
    {
        LooseXaml.OnStaThread(() =>
        {
            var message = $"The scan could not read {InstallerCacheHelpers.InstallerFolder}.";
            var box = new TextBox();
            PlainCopy.SetUntreated(box, "another message");
            box.Text = InstallerPathText.ForDrawing(message);
            box.SelectAll();

            var data = new DataObject();
            data.SetData(DataFormats.UnicodeText, box.SelectedText, true);
            box.RaiseEvent(new DataObjectCopyingEventArgs(data, isDragDrop: false));

            AssertSameCodePoints(message, (string)data.GetData(DataFormats.UnicodeText, false));
        });
    }

    // A selectable box shows its text as given, so a box whose text a treatment has
    // touched must be given its untreated text too (PlainCopy.Untreated). In XAML a
    // treatment arrives through loc:Translate or one of the two path converters, and no
    // selectable box takes either. Each file listed holds at least one selectable box.
    [Theory]
    [InlineData("ThemeXaml.MainWindow.xaml")]
    [InlineData("ThemeXaml.MessageWindow.xaml")]
    [InlineData("ThemeXaml.ReportWindow.xaml")]
    [InlineData("ThemeXaml.OrphanedFilesWindow.xaml")]
    [InlineData("ThemeXaml.RegisteredFilesWindow.xaml")]
    [InlineData("ThemeXaml.AboutWindow.xaml")]
    [InlineData("ThemeXaml.ScoopCommandBox.xaml")]
    public void No_selectable_box_is_given_treated_text_in_XAML(string window)
    {
        var boxes = ThemeXaml.Load(window).Descendants(ThemeXaml.Presentation + "TextBox")
            .Where(b => ((string?)b.Attribute("Style") ?? "") is var style
                && (style.Contains("SelectableText") || style.Contains("MetadataValueText")))
            .ToList();
        Assert.True(boxes.Count > 0, $"{window}: no selectable box was found to check");

        var treated = boxes
            .Where(b => ((string?)b.Attribute("Text") ?? "") is var text
                && (text.Contains("loc:Translate") || text.Contains("KeepInstallerPathWhole") || text.Contains("BreakPathsAtFolders")))
            .Select(b => (string?)b.Attribute(ThemeXaml.Xaml + "Name") ?? (string?)b.Attribute("Text"))
            .ToList();

        Assert.True(treated.Count == 0, $"{window}: {string.Join(", ", treated)}");
    }

    // The two characters as \u escapes in the rows above, so neither is typed.
    private static string Unescape(string text) =>
        text.Replace("\\u200B", ZeroWidthSpace.ToString()).Replace("\\u2060", WordJoiner.ToString());

    private static void AssertSameCodePoints(string expected, string actual)
    {
        for (int i = 0; i < Math.Min(expected.Length, actual.Length); i++)
            Assert.True(expected[i] == actual[i],
                $"index {i}: expected U+{(int)expected[i]:X4}, got U+{(int)actual[i]:X4}");
        Assert.True(expected.Length == actual.Length,
            $"length: expected {expected.Length}, got {actual.Length}");
    }
}
