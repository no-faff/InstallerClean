namespace System.Text;

/// <summary>
/// The two members of .NET 10's <c>System.Text.Ascii</c> the app calls, which .NET Framework 4.8
/// does not have, each answering as .NET 10's does.
/// </summary>
internal static class Ascii
{
    /// <summary>Whether every character of <paramref name="value"/> is ASCII, U+0000 to U+007F.</summary>
    internal static bool IsValid(ReadOnlySpan<char> value)
    {
        foreach (var c in value)
        {
            if (c > '\x7F') return false;
        }

        return true;
    }

    /// <summary>
    /// Whether the two spans are the same length and every pair of characters is ASCII and equal
    /// once A to Z are folded to a to z. A pair holding any character outside ASCII answers false,
    /// whatever its case.
    /// </summary>
    internal static bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        if (left.Length != right.Length) return false;

        for (var i = 0; i < left.Length; i++)
        {
            uint a = left[i];
            uint b = right[i];

            if ((a | b) > 0x7F) return false;
            if (a == b) continue;

            a |= 0x20;
            if (a - 'a' > 'z' - 'a') return false;
            if (a != (b | 0x20)) return false;
        }

        return true;
    }
}
