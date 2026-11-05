using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// Three treatments of a path as the text goes to a control, all about where a
/// line is allowed to break.
///
/// The installer cache folder's path is held whole, its punctuation seams bound
/// with U+2060 WORD JOINER. The joiners keep a wrapped path from breaking after
/// the drive's <c>C:</c> and carrying on <c>\Windows\Installer</c> on the next
/// line, which reads as two things rather than one.
///
/// The crash log's path gets the reverse, U+200B at its folder boundaries so that
/// it breaks there rather than wherever it lands. That one runs through a user
/// profile, so holding it whole would push it off the pane instead of wrapping it.
/// Its drive seam still takes the joiner, that being the one break UAX #14 allows
/// inside a path. <see cref="ForDrawing"/> applies the pair, because a sentence can
/// name either path and the scan diagnoses name both.
///
/// The line saying what a scan is waiting for, the line naming a drive or share
/// the app carried on without, and the folder a Move sends files to, get the
/// third, <see cref="AllowFolderBreaksInAnyPath"/>. None of those paths is known
/// here: a share, drive or path in either line sits where each language's grammar
/// puts it, and a destination is whatever folder was chosen. So the path is found
/// by its backslashes rather than matched.
///
/// Why here rather than in the strings: this is presentation, and every resx
/// value in every language stays exactly as the translators wrote it. It
/// also puts the transform out of the CLI's reach by construction. Some of the
/// strings naming the folder are console output, one of them machine-read, and
/// an invisible character in a console line is a liability with no upside; the
/// CLI takes its strings from the generated <c>Strings</c> class in Core and
/// never comes past this project's boundary.
///
/// Every seam a break can fall at is bound. Unicode's line-breaking algorithm
/// (UAX #14) allows exactly one break inside the path, between the <c>:</c>
/// (class IS) and the <c>\</c> (class PR, the backslash sharing a code point with
/// the yen sign), everything else being letter to letter or covered by LB24, and
/// WPF's line breaker offers that break. Binding both sides of each backslash as
/// well costs three more invisible characters and makes the path safe against a
/// table that differs anywhere else. <see cref="AppendBound"/> has the seam list,
/// and why the drive letter's own colon is left out of it.
///
/// WPF honours class WJ: no break either side of U+2060 (LB11). So a paragraph
/// that runs out of room at the path breaks before <c>C:\Windows\Installer</c>
/// and carries the whole path onto the next line, rather than leaving <c>C:</c>
/// at the end of the line above. The app relies on WPF honouring class ZW as
/// well, adding U+200B to make a long path break at its folders.
///
/// WPF's line breaking is not language-tagged. Its breaking classes come from an
/// unmanaged LineServices callback; of the line flags it declares, the two
/// culture-flavoured ones, BreakClassWide and BreakClassStrict, are never set,
/// TextFormatterImp formatting a line with <c>LineFlags.None</c>; and the one
/// culture-sensitive break input it supplies is the hyphenator, which needs
/// IsHyphenationEnabled. So the language tag <c>App.OnStartup</c> puts on every
/// element cannot move a break, and a break falls the same way in every
/// language. A TextBlock on WPF's simple-text fast path has no break opportunity
/// inside the path to suppress.
///
/// Applied wherever this project turns a resource string into text that gets
/// drawn: <c>TranslateExtension</c> for everything XAML resolves,
/// <see cref="InstallerPathTextConverter"/> for the main window's bound body lines,
/// and by hand in the completion overlay's summary builder and the message
/// dialog's body. <see cref="AnyPathTextConverter"/> applies all three, the third
/// after the other two. The third alone is applied by <c>SplashWindow</c>'s step
/// line, to the destination by the Move confirmation, and to every line of the
/// finished card's summary. A drawn string keeps its joiners even where it is also
/// the spoken one, a TextBlock's automation peer reporting its Text as its name,
/// except on the lines showing a wait, the lines naming a drive or share carried on
/// without and the finished card's summary: each sets its screen-reader name to the
/// text as composed, so what a path in it takes reaches the layout and not the
/// speech.
/// A string that is only ever spoken does not get them, having no layout to
/// protect and nothing to hand a speech engine but invisible format characters:
/// the message dialog's title and the main window's invisible scan announcer are
/// the two hand-written sites on that side of the line, and
/// <c>TranslateExtension</c> draws that same line for every automation property
/// XAML resolves.
/// </summary>
internal static class InstallerPathText
{
    // The resolved folder, not a literal, and the same one the scan uses: a
    // hardcoded C: match target would find nothing to bind on a machine whose
    // Windows lives elsewhere, so the path would break there and only there.
    private static readonly string InstallerFolder = InstallerCacheHelpers.InstallerFolder;

    // The crash log's own path, resolved for the same reason the folder above is.
    private static readonly string LogPath = CrashLog.LogPath;

    // Spelled out for the same reason as the joiner below it. A break opportunity
    // rather than a prohibition: this path is long enough that holding it whole
    // would push it off the pane instead of wrapping it.
    private const char ZeroWidthSpace = '\u200B';

    // Spelled as an escape rather than typed, so it is visible in the editor
    // and in a diff, and cannot be flattened to a plain space by tooling.
    private const char WordJoiner = '\u2060';

    /// <summary>
    /// Returns <paramref name="text"/> with the crash log's path made to break at
    /// its folders rather than wherever it happens to land, or unchanged when it
    /// names no such path.
    ///
    /// THE OPPOSITE TREATMENT FROM THE ONE ABOVE, AND THE LENGTH IS WHY. The cache
    /// folder is short enough to hold whole; this path runs through a user profile
    /// and holding it whole would push it off the pane rather than wrap it. So the
    /// backslashes gain break opportunities and the drive seam gains the joiner,
    /// which is the one break UAX #14 allows inside a path and the one this project
    /// observed happening. Without it a narrow pane can still leave <c>C:</c> alone
    /// at the end of a line.
    ///
    /// <see cref="AllowFolderBreaksInAnyPath"/> does the backslash half for a path
    /// it finds by its backslashes rather than matches, a waiting line's share and
    /// a Move's destination. This one matches its path, and so can also close the
    /// drive seam.
    /// </summary>
    public static string AllowFolderBreaksInLogPath(string? text) =>
        AllowFolderBreaksIn(text, LogPath);

    /// <summary>
    /// The whole of the work above, with the path to look for handed in.
    ///
    /// SPLIT OUT SO IT CAN BE TESTED AGAINST A PATH RATHER THAN AGAINST THE HOST'S.
    /// The log lives under the profile, so on a machine that is not Windows the real
    /// value holds no backslash and no drive letter at all, and there is nothing in
    /// it for an assertion to count. This overload is what lets a caller hand in a
    /// path that has both.
    /// </summary>
    internal static string AllowFolderBreaksIn(string? text, string path)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(path))
            return text ?? string.Empty;

        int at = text.IndexOf(path, StringComparison.OrdinalIgnoreCase);
        if (at < 0)
            return text;

        var matched = text.AsSpan(at, path.Length);
        var built = new StringBuilder(text.Length + path.Length);
        built.Append(text, 0, at);

        for (int i = 0; i < matched.Length; i++)
        {
            built.Append(matched[i]);
            if (matched[i] == '\\')
                built.Append(ZeroWidthSpace);
            else if (matched[i] == ':' && i + 1 < matched.Length && matched[i + 1] == '\\')
                built.Append(WordJoiner);
        }

        built.Append(text, at + path.Length, text.Length - at - path.Length);
        return built.ToString();
    }

    /// <summary>
    /// Returns <paramref name="text"/> with every occurrence of the installer
    /// folder bound together, or unchanged when it names no such path (the
    /// overwhelming majority of strings, and the reason for the early exit).
    /// The match ignores case, because a path is case-insensitive on Windows
    /// and the folder can reach a sentence from somewhere other than the token
    /// substitution, in whatever case that source wrote it; the matched run is
    /// rebuilt from the text itself, so the case it arrived in survives.
    /// </summary>
    public static string KeepWhole(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        int at = text.IndexOf(InstallerFolder, StringComparison.OrdinalIgnoreCase);
        if (at < 0)
            return text;

        var joined = new StringBuilder(text.Length + 8);
        int copiedTo = 0;
        while (at >= 0)
        {
            joined.Append(text, copiedTo, at - copiedTo);
            AppendBound(joined, text.AsSpan(at, InstallerFolder.Length));
            copiedTo = at + InstallerFolder.Length;
            at = text.IndexOf(InstallerFolder, copiedTo, StringComparison.OrdinalIgnoreCase);
        }

        joined.Append(text, copiedTo, text.Length - copiedTo);
        return joined.ToString();
    }

    // A joiner goes either side of each backslash: four seams, the drive
    // colon's included, the seam after it being the same position as the one
    // before the first backslash. Those are the only places in
    // C:\Windows\Installer a line breaker will offer a break. Not between the
    // drive letter and its colon, because UAX #14 forbids a break before ':'
    // regardless, so a joiner there is one more invisible character for a
    // screen reader and a braille display to carry for nothing.
    private static void AppendBound(StringBuilder joined, ReadOnlySpan<char> path)
    {
        for (int i = 0; i < path.Length; i++)
        {
            if (i > 0 && (path[i - 1] is ':' or '\\' || path[i] == '\\'))
                joined.Append(WordJoiner);
            joined.Append(path[i]);
        }
    }
    /// <summary>
    /// Both treatments, for a string that is about to be laid out.
    ///
    /// THE TWO ARE OPPOSITES AND BOTH ARE NEEDED. The cache folder is short and gets
    /// bound so it cannot break at all; the crash log's path runs through a user
    /// profile and gets break opportunities at its folders instead, or it would push
    /// the line off the pane rather than wrap it. A sentence can name either, and the
    /// scan diagnoses name both.
    ///
    /// EVERY SURFACE THAT CAN DRAW A CRASH-LOG PATH CALLS THIS RATHER THAN COMPOSING
    /// THE PAIR ITSELF, and there are three: the two converters below and the message
    /// dialog's body. A surface composing the pair for itself is one a change here
    /// does not reach. No log path reaches the other drawn surfaces, which call
    /// <see cref="KeepWhole"/> or <see cref="AllowFolderBreaksInAnyPath"/> directly.
    /// </summary>
    public static string ForDrawing(string? text) =>
        AllowFolderBreaksInLogPath(KeepWhole(text));

    /// <summary>
    /// Returns <paramref name="text"/> with every word joiner and zero-width space
    /// taken out, for text leaving a selectable box on the clipboard or in a drag.
    /// Those two characters are the only ones the treatments here add, and no string
    /// the app ships carries either, so this gives back the text as it was before it
    /// was drawn: a copied path is the path, and pastes into Run or Explorer as one.
    /// <see cref="PlainCopy"/> applies it.
    /// </summary>
    public static string ForCopying(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;
        if (text.IndexOf(WordJoiner) < 0 && text.IndexOf(ZeroWidthSpace) < 0)
            return text;

        var plain = new StringBuilder(text.Length);
        foreach (var c in text)
            if (c != WordJoiner && c != ZeroWidthSpace)
                plain.Append(c);
        return plain.ToString();
    }

    /// <summary>
    /// Returns <paramref name="text"/> with a break opportunity after each backslash
    /// inside a path it names, and a word joiner between two backslashes, wherever in
    /// the line the path sits. The line saying what a scan is waiting for takes it,
    /// naming a share, a drive or a path where each language's word order puts it, as
    /// does the line naming a drive or share the app carried on without, and so does
    /// the folder a Move sends files to, which is a path on its own.
    ///
    /// A share's name opens with two backslashes. Unicode's line-breaking rules allow a
    /// break between two backslashes, and the joiner is there to refuse it; neither of
    /// the pair takes a break after it, so nothing here offers a break inside
    /// <c>\\server</c> and <c>\\server\share</c> can break before <c>share</c>. A
    /// backslash opening a path, at the start of the line or after a space, takes no
    /// break after it either. A line naming no path comes back unchanged, and so does a
    /// line this has already been through. A backslash with a joiner or a zero-width
    /// space beside it takes no break opportunity, so a path <see cref="ForDrawing"/>
    /// has treated keeps that treatment.
    /// </summary>
    public static string AllowFolderBreaksInAnyPath(string? text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('\\'))
            return text ?? string.Empty;

        var built = new StringBuilder(text.Length + 8);
        for (int i = 0; i < text.Length; i++)
        {
            built.Append(text[i]);
            if (text[i] != '\\' || i + 1 == text.Length)
                continue;

            var next = text[i + 1];
            if (next == '\\')
                built.Append(WordJoiner);
            else if (i > 0 && IsSpelledInAPath(text[i - 1]) && IsSpelledInAPath(next))
                built.Append(ZeroWidthSpace);
        }

        return built.ToString();
    }

    // A backslash takes a break after it only where the character on each side of it is
    // spelled in a path: neither a backslash, nor a break character this class adds, nor
    // white space.
    private static bool IsSpelledInAPath(char c) =>
        c is not ('\\' or ZeroWidthSpace or WordJoiner) && !char.IsWhiteSpace(c);
}

/// <summary>
/// <see cref="InstallerPathText.ForDrawing"/> for a binding, where the string comes
/// from a view model rather than from the resx at parse time. The consumers are the
/// main window's bound body lines: the intro lead, the intro detail, the completion
/// restore line, the footnote saying why the scan held back the files no record names,
/// and the finished card's lines saying why a scan held files back for something about
/// the scan itself. The detail is the one that reaches every treatment
/// here, carrying the "they sit in" sentence that names the cache folder, the
/// not-yet-scanned prompt and, on a failed scan, either of the two diagnoses,
/// which name the cache folder and the log's path.
/// </summary>
internal sealed class InstallerPathTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => InstallerPathText.ForDrawing(value as string);

    // One-way only: the joiners are for the screen, and putting them back into
    // a view model would be a data change rather than a rendering one.
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// <see cref="InstallerPathText.ForDrawing"/> and then
/// <see cref="InstallerPathText.AllowFolderBreaksInAnyPath"/>, for a binding. The
/// consumers are the main window's two lines that can show what a scan is waiting
/// for, the scanning card's progress line and the heading over a Move or Delete, and
/// its two lines that can name a drive or share the app carried on without, the one
/// under the left-alone count and the finished card's held-back line. On a failed
/// Re-scan the scanning card's line, left up under the error box, shows the diagnosis
/// naming the crash log's path, and draws that path as the intro line does. Each line
/// binds its screen-reader name to the same value without this converter.
/// </summary>
internal sealed class AnyPathTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => InstallerPathText.AllowFolderBreaksInAnyPath(InstallerPathText.ForDrawing(value as string));

    // One-way only, for the reason the converter above gives.
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
