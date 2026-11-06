namespace System;

/// <summary>
/// The span member .NET Framework 4.8's System.Memory does not have, answering as the .NET 10
/// member of the same name does, so a call written for .NET 10 binds here unchanged.
/// </summary>
internal static class FrameworkMemoryExtensions
{
    /// <summary>
    /// The index of the first character of <paramref name="span"/> that is not one of
    /// <paramref name="values"/>, or -1 where there is none. With no values, every character
    /// qualifies, so the answer is 0 for a span that is not empty.
    /// </summary>
    internal static int IndexOfAnyExcept(this ReadOnlySpan<char> span, ReadOnlySpan<char> values)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (values.IndexOf(span[i]) < 0) return i;
        }

        return -1;
    }
}
