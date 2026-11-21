using System.Text;

namespace InstallerClean.Helpers;

/// <summary>
/// A list of arguments written as one command-line string, as .NET 10 writes
/// <c>ProcessStartInfo.ArgumentList</c> for <c>Process.Start</c> on Windows (dotnet/runtime
/// v10.0.0, <c>PasteArguments.cs</c>), so the started program reads each argument back as it
/// was given, by the rules CommandLineToArgvW and the C runtime read a command line with.
/// </summary>
/// <remarks>
/// Set as <c>ProcessStartInfo.Arguments</c>, the string gives the command line .NET 10 gives
/// for the same list: both write the program's path, quoted, and then a space before the
/// arguments, which this string separates by one space each.
/// </remarks>
internal static class CommandLineArguments
{
    private const char Quote = '"';
    private const char Backslash = '\\';

    /// <summary>
    /// <paramref name="arguments"/> as one string, one space between each. An argument that is
    /// not empty and holds no white space and no quote is written as it is. Every other is
    /// written in quotes: a quote inside it as a backslash and the quote, a run of backslashes
    /// before a quote or before the closing quote doubled, and any other backslash as itself.
    /// A null argument is an <see cref="ArgumentNullException"/>, as Process.Start gives for
    /// one in its ArgumentList.
    /// </summary>
    internal static string Join(IEnumerable<string> arguments)
    {
        var line = new StringBuilder();
        foreach (var argument in arguments)
            Append(line, argument);
        return line.ToString();
    }

    /// <summary>
    /// Appends <paramref name="argument"/> to <paramref name="line"/> as one argument, after a
    /// space where the line already holds text. A null argument is an
    /// <see cref="ArgumentNullException"/>, as for <see cref="Join"/>.
    /// </summary>
    internal static void Append(StringBuilder line, string argument)
    {
        if (argument is null) throw new ArgumentNullException("item", "ArgumentList may not contain null.");
        if (line.Length != 0) line.Append(' ');

        if (argument.Length != 0 && ContainsNoWhiteSpaceOrQuotes(argument))
        {
            line.Append(argument);
            return;
        }

        line.Append(Quote);
        var idx = 0;
        while (idx < argument.Length)
        {
            var c = argument[idx++];
            if (c == Backslash)
            {
                var numBackSlash = 1;
                while (idx < argument.Length && argument[idx] == Backslash)
                {
                    idx++;
                    numBackSlash++;
                }

                if (idx == argument.Length)
                {
                    // The closing quote follows.
                    line.Append(Backslash, numBackSlash * 2);
                }
                else if (argument[idx] == Quote)
                {
                    line.Append(Backslash, numBackSlash * 2 + 1);
                    line.Append(Quote);
                    idx++;
                }
                else
                {
                    line.Append(Backslash, numBackSlash);
                }

                continue;
            }

            if (c == Quote)
            {
                // A quote written as a backslash and the quote never follows a closing quote,
                // which readers before and after 2008 take differently.
                line.Append(Backslash);
                line.Append(Quote);
                continue;
            }

            line.Append(c);
        }

        line.Append(Quote);
    }

    private static bool ContainsNoWhiteSpaceOrQuotes(string s)
    {
        foreach (var c in s)
        {
            if (IsWhiteSpace(c) || c == Quote)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Whether <paramref name="c"/> is one of the 25 characters Unicode gives the White_Space
    /// property, the set .NET 10's <c>char.IsWhiteSpace</c> answers true for. Written out so
    /// the quoting does not rest on the character tables of the runtime it runs on.
    /// </summary>
    internal static bool IsWhiteSpace(char c) =>
        c is >= '\u0009' and <= '\u000D'
            or '\u0020'
            or '\u0085'
            or '\u00A0'
            or '\u1680'
            or >= '\u2000' and <= '\u200A'
            or '\u2028'
            or '\u2029'
            or '\u202F'
            or '\u205F'
            or '\u3000';
}
