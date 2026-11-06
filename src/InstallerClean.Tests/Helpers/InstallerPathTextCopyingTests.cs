using System.Collections;
using System.Globalization;
using System.Resources;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Text copied out of a selectable box is the text before it was drawn: the word
/// joiners and zero-width spaces <see cref="InstallerPathText"/> adds for line
/// breaking stay on screen and do not reach the clipboard.
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

        AssertSameCodePoints(message, InstallerPathText.ForCopying(drawn));
    }

    [Fact]
    public void A_copy_of_a_message_drawn_as_the_message_dialog_draws_it_is_the_message()
    {
        var message = $"The scan could not read {InstallerCacheHelpers.InstallerFolder}. "
            + $"This is also recorded in {CrashLog.LogPath}.";

        AssertSameCodePoints(message, InstallerPathText.ForCopying(InstallerPathText.ForDrawing(message)));
    }

    [Fact]
    public void Text_carrying_neither_character_comes_back_as_it_is()
    {
        const string text = @"scoop update installerclean";
        Assert.Same(text, InstallerPathText.ForCopying(text));
        Assert.Equal(string.Empty, InstallerPathText.ForCopying(null));
    }

    public static TheoryData<string> Cultures()
    {
        var data = new TheoryData<string> { "" };
        foreach (var name in SupportedLanguages.CultureNames) data.Add(name);
        return data;
    }

    // What lets the copy drop every one of the two characters: none of them was in
    // the text before it was drawn, in any string the app ships.
    [Theory]
    [MemberData(nameof(Cultures))]
    public void No_shipped_string_carries_either_character(string cultureName)
    {
        var culture = cultureName.Length == 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(cultureName);
        var set = Strings.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
        Assert.NotNull(set);

        var read = 0;
        var faults = new List<string>();
        foreach (DictionaryEntry entry in set!)
        {
            if (entry.Value is not string value) continue;
            read++;
            if (value.Contains(ZeroWidthSpace) || value.Contains(WordJoiner))
                faults.Add((string)entry.Key);
        }

        Assert.True(read > 0, $"{cultureName}: no string was read");
        Assert.True(faults.Count == 0, $"{cultureName}: {string.Join(", ", faults)}");
    }

    private static void AssertSameCodePoints(string expected, string actual)
    {
        for (int i = 0; i < Math.Min(expected.Length, actual.Length); i++)
            Assert.True(expected[i] == actual[i],
                $"index {i}: expected U+{(int)expected[i]:X4}, got U+{(int)actual[i]:X4}");
        Assert.True(expected.Length == actual.Length,
            $"length: expected {expected.Length}, got {actual.Length}");
    }
}
