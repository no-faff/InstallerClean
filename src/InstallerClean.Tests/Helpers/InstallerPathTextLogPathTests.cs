using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The crash log's path, made to break at its folders in the one sentence telling
/// somebody where to find it.
///
/// It gets the opposite treatment from the cache folder beside it, and length is
/// why: that one is held whole, and this one runs through a user profile, so
/// holding it whole would push it off the pane rather than wrap it. The drive seam
/// still gets the joiner, that being the one break UAX #14 allows inside a path.
///
/// The transform belongs to the drawing layer. The string it works on also reaches
/// the scan announcer, which is never laid out and takes its values unbound, so a
/// view model doing this would hand a speech engine invisible characters for a
/// layout that does not exist.
/// </summary>
public class InstallerPathTextLogPathTests
{
    // Spelled as escapes, never typed. A literal is invisible in the editor and in
    // a diff and is liable to be flattened by tooling, which is the same reason the
    // code under test spells its own out.
    private const char ZeroWidthSpace = '\u200B';
    private const char WordJoiner = '\u2060';

    // A path rather than the host's own. The log lives under the profile, so off
    // Windows it carries no backslash and no drive letter, and a test driven off the
    // real value there counts zero separators against zero insertions and passes
    // whatever the method does. Measured: with the transform disabled outright,
    // every assertion here still held.
    private const string Path = @"C:\Users\U-ser\AppData\Local\NoFaff\InstallerClean\crash.log";

    private static string Treat(string text) =>
        InstallerPathText.AllowFolderBreaksIn(text, Path);

    [Fact]
    public void Every_folder_boundary_gains_a_break_opportunity()
    {
        var result = Treat($"This is also recorded in {Path}.");

        // Written out rather than derived from the path, so a transform that also
        // miscounted could not satisfy it.
        Assert.Equal(7, Path.Count(c => c == '\\'));
        Assert.Equal(7, result.Count(c => c == ZeroWidthSpace));

        // Rebuilt rather than reformatted, so stripping the additions gives the
        // original back exactly.
        Assert.Equal($"This is also recorded in {Path}.",
            new string(result.Where(c => c != ZeroWidthSpace && c != WordJoiner).ToArray()));

        // WHERE, not just how many. A transform appending all seven to the end of the
        // sentence satisfies both assertions above, and a break opportunity is only
        // worth anything at the seam it belongs to.
        for (int i = 0; i < result.Length; i++)
            if (result[i] == ZeroWidthSpace)
                Assert.True(i > 0 && result[i - 1] == '\\',
                    $"a break opportunity at index {i} follows '{(i > 0 ? result[i - 1] : ' ')}' rather than a separator");
    }

    [Fact]
    public void The_drive_seam_is_bound_so_a_narrow_pane_cannot_strand_the_drive_letter()
    {
        var result = Treat(Path);

        // The colon-to-backslash boundary is the one break the algorithm allows
        // inside a path, so it is the one seam wanting a prohibition rather than an
        // opportunity. Exactly one joiner: the folder separators take the other
        // treatment.
        Assert.Equal(WordJoiner, result[result.IndexOf(':') + 1]);
        Assert.Equal(1, result.Count(c => c == WordJoiner));
    }

    [Fact]
    public void A_sentence_naming_no_path_is_handed_back_unchanged()
    {
        // The must-miss control. Almost every string the converter sees is this one,
        // and a transform that fired on all of them would satisfy the two above.
        const string plain = "The Windows Installer records came back completely empty.";
        Assert.Equal(plain, Treat(plain));
        Assert.Equal(string.Empty, Treat(string.Empty));
    }

    [Fact]
    public void The_real_log_path_is_what_the_drawing_layer_actually_asks_about()
    {
        // The one thing the tests above cannot say, because they supply their own
        // path: that the public entry point looks for the log's real location rather
        // than something else.
        //
        // IT BITES ON WINDOWS AND IS INERT ANYWHERE ELSE, which is worth knowing
        // before reading a pass here as evidence. Where the log sits outside a
        // Windows profile its path holds no separator, so both sides of this are the
        // identity function and agree whatever the method does.
        var sentence = $"This is also recorded in {CrashLog.LogPath}.";
        Assert.Equal(
            InstallerPathText.AllowFolderBreaksIn(sentence, CrashLog.LogPath),
            InstallerPathText.AllowFolderBreaksInLogPath(sentence));
    }
}
