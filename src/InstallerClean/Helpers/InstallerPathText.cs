using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace InstallerClean.Helpers;

/// <summary>
/// Keeps the literal <c>C:\Windows\Installer</c> whole when the UI draws it, by
/// binding its punctuation seams with U+2060 WORD JOINER as the text goes to a
/// control. Eleven resx keys name the folder; wrapped in a paragraph, the path
/// broke after the <c>C:</c> and carried on <c>\Windows\Installer</c> on the
/// next line, which reads as two things rather than one.
///
/// Why here rather than in the strings: this is presentation, and every resx
/// value in sixteen languages stays exactly as the translators wrote it. It
/// also puts the transform out of the CLI's reach by construction. Three of the
/// eleven keys are console output, one of them machine-read, and an invisible
/// character in a console line is a liability with no upside; the CLI takes its
/// strings from the generated <c>Strings</c> class in Core and never comes past
/// this project's boundary.
///
/// The joined seams, not just the one that was observed to break. Unicode's
/// line-breaking algorithm (UAX #14) allows exactly one break inside the path,
/// between the <c>:</c> (class IS) and the <c>\</c> (class PR, the backslash
/// sharing a code point with the yen sign), everything else being letter to
/// letter or covered by LB24; the reported break was at that exact point, which
/// says WPF's breaker follows a current table rather than an approximation. A
/// joiner at every non-letter seam costs four more invisible characters and
/// makes the path safe against a table that differs anywhere else.
///
/// UNVERIFIED, and it is the whole mechanism: that WPF honours class WJ.
/// U+2060 is the character Unicode defines for this (LB11 forbids a break
/// either side of it), and the app already relies on WPF honouring the
/// neighbouring class ZW, since <c>CompositionParsing.InsertPathWrapPoints</c>
/// adds U+200B to make a long destination path break at its folders. But
/// WPF breaks lines through the unmanaged LineServices layer, not through
/// dotnet/wpf, so no source reading settles it and it needs a look at the
/// running app. Where a TextBlock takes WPF's simple-text fast path instead,
/// the question does not arise: that path has no break opportunity inside the
/// path to suppress.
///
/// Applied wherever this project turns a resource string into text on a
/// control, drawn or spoken: <c>TranslateExtension</c> for everything XAML
/// resolves, the converter below for the main window's intro line, and by hand
/// in the completion overlay's two inline builders, the message dialog and the
/// scan announcer. Drawn and spoken are not separated, because a drawn
/// TextBlock's automation peer reports its Text as its name, so the joiners
/// reach a screen reader through the visible strings whatever is done with the
/// automation-only ones.
/// </summary>
internal static class InstallerPathText
{
    // Not a verbatim string: a lone backslash reads unambiguously in this form,
    // and the escape sequence next to it is the one that once shipped broken.
    private const string InstallerFolder = "C:\\Windows\\Installer";

    // Spelled as an escape rather than typed, so it is visible in the editor
    // and in a diff, and cannot be flattened to a plain space by tooling. The
    // same reason InsertPathWrapPoints spells its zero-width space out.
    private const char WordJoiner = '\u2060';

    /// <summary>
    /// Returns <paramref name="text"/> with every occurrence of the installer
    /// folder bound together, or unchanged when it names no such path (the
    /// overwhelming majority of strings, and the reason for the early exit).
    /// The match ignores case, because a path is case-insensitive on Windows
    /// and a translator could reasonably lower-case it; the matched run is
    /// rebuilt from the text itself, so whatever case it was written in
    /// survives.
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

    // A joiner goes at every seam except letter-to-letter, where no
    // line-breaking algorithm has ever offered a break. In C:\Windows\Installer
    // that is five seams, not four: both sides of the colon, both sides of each
    // backslash, with the colon and the first backslash sharing one.
    private static void AppendBound(StringBuilder joined, ReadOnlySpan<char> path)
    {
        for (int i = 0; i < path.Length; i++)
        {
            if (i > 0 && !(char.IsLetter(path[i - 1]) && char.IsLetter(path[i])))
                joined.Append(WordJoiner);
            joined.Append(path[i]);
        }
    }
}

/// <summary>
/// <see cref="InstallerPathText.KeepWhole"/> for a binding, where the string
/// comes from a view model rather than from the resx at parse time. The main
/// window's intro detail line is the one consumer: it carries the "they sit in
/// C:\Windows\Installer" sentence, the not-yet-scanned prompt and, on a failed
/// scan, either of the two diagnoses that name the folder.
/// </summary>
internal sealed class InstallerPathTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => InstallerPathText.KeepWhole(value as string);

    // One-way only: the joiners are for the screen, and putting them back into
    // a view model would be a data change rather than a rendering one.
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
