namespace InstallerClean.Helpers;

/// <summary>
/// The string parsing behind the WPF windows' inline-composition builders: the
/// splitting that turns one formatted sentence into the prefix / body / suffix
/// segments a window renders as separate <c>Run</c>s.
///
/// The window code-behind keeps the WPF inline construction (<c>Run</c>,
/// <c>LineBreak</c>, <c>Hyperlink</c>); the decisions about WHERE a sentence
/// splits live here, in Core, so a unit test reaches them with no UI thread.
/// </summary>
public static class CompositionParsing
{
    /// <summary>
    /// Splits <paramref name="raw"/> at the first occurrence of
    /// <paramref name="substring"/>, returning the text before it (trailing
    /// whitespace trimmed, because the substring is forced onto its own line) and
    /// the text after it. Returns <c>null</c> when <paramref name="substring"/> is
    /// empty or does not occur, so the caller renders <paramref name="raw"/> as a
    /// single <c>Run</c>. The match is ordinal: the substring is a user-chosen
    /// path, compared byte for byte, and could itself contain any character.
    /// </summary>
    public static SubstringSplit? SplitAtSubstring(string raw, string substring)
    {
        if (string.IsNullOrEmpty(substring))
            return null;

        int index = raw.IndexOfOrdinal(substring, StringComparison.Ordinal);
        if (index < 0)
            return null;

        return new SubstringSplit(
            raw[..index].TrimEnd(),
            raw[(index + substring.Length)..]);
    }

    /// <summary>
    /// Splits <paramref name="raw"/> at the first <c>[ ]</c>-delimited phrase,
    /// returning the text before the <c>[</c>, the phrase between the brackets and
    /// the text after the <c>]</c>. Returns <c>null</c> when there is no complete
    /// pair, so the caller renders <paramref name="raw"/> verbatim. The brackets
    /// mark where a translator placed an in-sentence link; holding the whole
    /// sentence in one resx string lets the link sit anywhere the grammar wants it.
    /// </summary>
    public static BracketSplit? SplitAtBracketedPhrase(string raw)
    {
        int open = raw.IndexOf('[');
        int close = open >= 0 ? raw.IndexOf(']', open + 1) : -1;
        if (open < 0 || close < 0)
            return null;

        return new BracketSplit(
            raw[..open],
            raw[(open + 1)..close],
            raw[(close + 1)..]);
    }
}

/// <summary>The two halves of a sentence split at a raw substring.</summary>
public sealed record SubstringSplit(string Prefix, string Suffix);

/// <summary>The three parts of a sentence split at a <c>[ ]</c>-delimited link phrase.</summary>
public sealed record BracketSplit(string Prefix, string LinkText, string Suffix);
