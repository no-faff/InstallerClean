using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// Two opposite treatments of a path as the text goes to a control, and both are
/// about where a line is allowed to break.
///
/// The installer cache folder's path is held whole, its punctuation seams bound
/// with U+2060 WORD JOINER. Wrapped in a paragraph, the path broke after the
/// drive's <c>C:</c> and carried on <c>\Windows\Installer</c> on the next line,
/// which reads as two things rather than one.
///
/// The crash log's path gets the reverse, U+200B at its folder boundaries so that
/// it breaks there rather than wherever it lands. That one runs through a user
/// profile, so holding it whole would push it off the pane instead of wrapping it.
/// Its drive seam still takes the joiner, that being the one break UAX #14 allows
/// inside a path. <see cref="ForDrawing"/> applies the pair, because a sentence can
/// name either path and the scan diagnoses name both.
///
/// Why here rather than in the strings: this is presentation, and every resx
/// value in sixteen languages stays exactly as the translators wrote it. It
/// also puts the transform out of the CLI's reach by construction. Some of the
/// strings naming the folder are console output, one of them machine-read, and
/// an invisible character in a console line is a liability with no upside; the
/// CLI takes its
/// strings from the generated <c>Strings</c> class in Core and never comes past
/// this project's boundary.
///
/// Every seam a break can fall at, not just the one that was observed to break.
/// Unicode's line-breaking algorithm (UAX #14) allows exactly one break inside
/// the path, between the <c>:</c> (class IS) and the <c>\</c> (class PR, the
/// backslash sharing a code point with the yen sign), everything else being
/// letter to letter or covered by LB24; the reported break was at that exact
/// point, which says WPF's breaker follows a current table rather than an
/// approximation. Binding both sides of each backslash as well costs three more
/// invisible characters and makes the path safe against a table that differs
/// anywhere else. <see cref="AppendBound"/> has the seam list, and why the
/// drive letter's own colon is left out of it.
///
/// VERIFIED, and it is the whole mechanism: WPF honours class WJ. Observed in
/// the running app in Dutch, where the paragraph carrying the path breaks
/// immediately BEFORE <c>C:\Windows\Installer</c> and takes the whole path onto
/// the next line, leaving room at the end of the line above for the <c>C:</c> a
/// breaker following the table alone would have put there. The break
/// opportunity existed and was declined, which is LB11 (no break either side of
/// U+2060) being honoured. The app already relied on the neighbouring class ZW,
/// since <c>CompositionParsing.InsertPathWrapPoints</c> adds U+200B to make a
/// long destination path break at its folders.
///
/// No source reading could have settled that, and the same reading settles the
/// question it raises. WPF formats every line with <c>LineFlags.None</c>
/// (TextFormatterImp) and takes the breaking classes themselves from an
/// unmanaged LineServices callback with no managed counterpart, so the table is
/// out of reach. But it is also not language-tagged: the two culture-flavoured
/// break knobs WPF declares, BreakClassWide and BreakClassStrict, are never
/// set, and the one culture-sensitive break input it does supply is the
/// hyphenator, which needs IsHyphenationEnabled. So the tag
/// <c>App.OnStartup</c> puts on every element cannot move a break either way,
/// and what was seen under one language holds under all sixteen. Where a
/// TextBlock takes WPF's simple-text fast path instead, the question does not
/// arise: that path has no break opportunity inside the path to suppress.
///
/// Applied wherever this project turns a resource string into text that gets
/// drawn: <c>TranslateExtension</c> for everything XAML resolves, the converter
/// below for the main window's intro line, and by hand in the completion
/// overlay's two inline builders and the message dialog's body. A drawn string
/// keeps its joiners even where it is also the spoken one, a TextBlock's
/// automation peer reporting its Text as its name. A string that is only ever
/// spoken does not get them, having no layout to protect and nothing to hand a
/// speech engine but invisible format characters: the message dialog's title
/// and the main window's invisible scan announcer are the two hand-written
/// sites on that side of the line, and <c>TranslateExtension</c> draws that
/// same line for every automation property XAML resolves.
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
    // and in a diff, and cannot be flattened to a plain space by tooling. The
    // same reason InsertPathWrapPoints spells its zero-width space out.
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
    /// <c>CompositionParsing.InsertPathWrapPoints</c> does the backslash half for
    /// the destination path in two windows that build their text in code-behind.
    /// This is the same idea at the binding, where it can also close the drive seam.
    /// </summary>
    public static string AllowFolderBreaksInLogPath(string? text) =>
        AllowFolderBreaksIn(text, LogPath);

    /// <summary>
    /// The whole of the work above, with the path to look for handed in.
    ///
    /// SPLIT OUT SO IT CAN BE TESTED AGAINST A PATH RATHER THAN AGAINST THE HOST'S.
    /// The log lives under the profile, so on a machine that is not Windows it holds
    /// no backslash and no drive letter at all, and a test driven off the real value
    /// there counts zero separators against zero insertions and passes whatever the
    /// method does.
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
    /// THE PAIR ITSELF, and there are two: the converter below and the message
    /// dialog's body. When they each composed their own, a change to the first left
    /// the second behind. The other drawn surfaces call <see cref="KeepWhole"/>
    /// directly and are right to, no log path reaching them.
    /// </summary>
    public static string ForDrawing(string? text) =>
        AllowFolderBreaksInLogPath(KeepWhole(text));
}

/// <summary>
/// <see cref="ForDrawing"/> for a binding, where the string comes from a view
/// model rather than from the resx at parse time. The main window's intro detail
/// line is the one consumer: it carries the "they sit in" sentence that names the
/// cache folder, the not-yet-scanned prompt and, on a failed scan, either of the
/// two diagnoses, which name the cache folder and the log's path.
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
