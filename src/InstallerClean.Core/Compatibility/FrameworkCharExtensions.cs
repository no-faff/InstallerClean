namespace System;

/// <summary>
/// The ASCII tests on <see cref="char"/> that .NET Framework 4.8 does not have, each answering as
/// the .NET 10 member of the same name does, so a call such as <c>char.IsAsciiDigit(c)</c> binds
/// here unchanged.
/// </summary>
internal static class FrameworkCharExtensions
{
    extension(char)
    {
        /// <summary>Whether <paramref name="c"/> is a letter A to Z or a to z.</summary>
        public static bool IsAsciiLetter(char c) => (uint)((c | 0x20) - 'a') <= 'z' - 'a';

        /// <summary>Whether <paramref name="c"/> is a letter a to z.</summary>
        public static bool IsAsciiLetterLower(char c) => (uint)(c - 'a') <= 'z' - 'a';

        /// <summary>Whether <paramref name="c"/> is a letter A to Z.</summary>
        public static bool IsAsciiLetterUpper(char c) => (uint)(c - 'A') <= 'Z' - 'A';

        /// <summary>Whether <paramref name="c"/> is a digit 0 to 9.</summary>
        public static bool IsAsciiDigit(char c) => (uint)(c - '0') <= '9' - '0';

        /// <summary>Whether <paramref name="c"/> is a digit 0 to 9 or a letter A to F or a to f.</summary>
        public static bool IsAsciiHexDigit(char c) =>
            (uint)(c - '0') <= '9' - '0' || (uint)((c | 0x20) - 'a') <= 'f' - 'a';
    }
}
