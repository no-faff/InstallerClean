using System.Collections.Generic;
using System.Text;

namespace System;

/// <summary>
/// The <see cref="string"/> members .NET Framework 4.8 does not have, each doing what the .NET 10
/// member of the same name does, so a call written for .NET 10 binds here unchanged. A member
/// .NET Framework does have, such as <c>Split(params char[])</c>, is never reached through this
/// class, as the compiler binds a type's own member first.
/// </summary>
internal static class FrameworkStringExtensions
{
    extension(string text)
    {
        /// <summary>Whether the text is not empty and its first character is <paramref name="value"/>.</summary>
        public bool StartsWith(char value) => text.Length != 0 && text[0] == value;

        /// <summary>Whether the text is not empty and its last character is <paramref name="value"/>.</summary>
        public bool EndsWith(char value) => text.Length != 0 && text[text.Length - 1] == value;

        /// <summary>
        /// Whether the character <paramref name="value"/> occurs in the text, compared ordinally.
        /// Without this member, a call on .NET Framework binds LINQ's Enumerable.Contains, which
        /// answers the same for a string but throws ArgumentNullException rather than
        /// NullReferenceException where the text is null.
        /// </summary>
        public bool Contains(char value)
        {
            if (text is null) throw new NullReferenceException();

            return text.AsSpan().IndexOf(value) >= 0;
        }

        /// <summary>
        /// Whether <paramref name="value"/> occurs in the text under an ordinal comparison, with
        /// case folded for <see cref="StringComparison.OrdinalIgnoreCase"/>. An empty value occurs
        /// in every text.
        /// </summary>
        /// <exception cref="NotSupportedException">A culture comparison, which has no .NET Framework port.</exception>
        public bool Contains(string value, StringComparison comparisonType)
        {
            if (text is null) throw new NullReferenceException();
            if (value is null) throw new ArgumentNullException(nameof(value));

            return OrdinalIndexOf(text, value, 0, IgnoresCase(comparisonType)) >= 0;
        }

        /// <summary>
        /// The first index at or after <paramref name="startIndex"/> where <paramref name="value"/>
        /// occurs in the text under an ordinal comparison, with case folded for
        /// <see cref="StringComparison.OrdinalIgnoreCase"/>, or -1: what .NET 10's
        /// <c>IndexOf(string, int, StringComparison)</c> answers for those two comparisons. An
        /// empty value is found at <paramref name="startIndex"/>. string.IndexOf with a
        /// comparison is banned in the shipped projects and this is called in its place, as
        /// .NET Framework's searches ASCII text by culture rules under OrdinalIgnoreCase.
        /// </summary>
        /// <exception cref="NotSupportedException">A culture comparison, which has no .NET Framework port.</exception>
        public int IndexOfOrdinal(string value, int startIndex, StringComparison comparisonType)
        {
            if (text is null) throw new NullReferenceException();
            if (value is null) throw new ArgumentNullException(nameof(value));
            if ((uint)startIndex > (uint)text.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Index was out of range. Must be non-negative and less than or equal to the size of the collection.");

            return OrdinalIndexOf(text, value, startIndex, IgnoresCase(comparisonType));
        }

        /// <summary><see cref="IndexOfOrdinal(string, int, StringComparison)"/> from the start of the text.</summary>
        public int IndexOfOrdinal(string value, StringComparison comparisonType) =>
            IndexOfOrdinal(text, value, 0, comparisonType);

        /// <summary>
        /// The text with every occurrence of <paramref name="oldValue"/>, found left to right
        /// without overlap under an ordinal comparison (case folded for
        /// <see cref="StringComparison.OrdinalIgnoreCase"/>), replaced by
        /// <paramref name="newValue"/>, or by nothing where that is null. The same instance comes
        /// back where nothing matched.
        /// </summary>
        /// <exception cref="NotSupportedException">A culture comparison, which has no .NET Framework port.</exception>
        public string Replace(string oldValue, string? newValue, StringComparison comparisonType)
        {
            if (text is null) throw new NullReferenceException();
            var ignoreCase = IgnoresCase(comparisonType);
            if (oldValue is null) throw new ArgumentNullException(nameof(oldValue));
            if (oldValue.Length == 0) throw new ArgumentException("The value cannot be an empty string.", nameof(oldValue));

            var at = OrdinalIndexOf(text, oldValue, 0, ignoreCase);
            if (at < 0) return text;

            var result = new StringBuilder(text.Length);
            var from = 0;
            while (at >= 0)
            {
                result.Append(text, from, at - from).Append(newValue);
                from = at + oldValue.Length;
                at = OrdinalIndexOf(text, oldValue, from, ignoreCase);
            }

            return result.Append(text, from, text.Length - from).ToString();
        }

        /// <summary>The text split at each <paramref name="separator"/>, as the overload taking a count does with no limit on it.</summary>
        public string[] Split(char separator, StringSplitOptions options) =>
            SplitAt(text, separator.ToString(), int.MaxValue, options);

        /// <summary>
        /// The text split at each <paramref name="separator"/> into at most
        /// <paramref name="count"/> parts, the last holding the rest of the text, separators
        /// included. <see cref="StringSplitOptions.RemoveEmptyEntries"/> drops empty parts and
        /// TrimEntries (value 2, which .NET Framework's enum does not name) trims white space from
        /// each.
        /// </summary>
        public string[] Split(char separator, int count, StringSplitOptions options = StringSplitOptions.None) =>
            SplitAt(text, separator.ToString(), count, options);

        /// <summary>
        /// The text split at each occurrence of <paramref name="separator"/>, found left to right
        /// without overlap by ordinal comparison. A null or empty separator splits nothing: the
        /// text is the one part.
        /// </summary>
        public string[] Split(string? separator, StringSplitOptions options = StringSplitOptions.None) =>
            SplitAt(text, separator ?? string.Empty, int.MaxValue, options);

        /// <summary>The text split at each occurrence of <paramref name="separator"/> into at most <paramref name="count"/> parts.</summary>
        public string[] Split(string? separator, int count, StringSplitOptions options = StringSplitOptions.None) =>
            SplitAt(text, separator ?? string.Empty, count, options);
    }

    extension(string)
    {
        /// <summary>
        /// Each of <paramref name="values"/> as text, joined with <paramref name="separator"/>
        /// between them. A null value, or one whose text is null, joins as nothing.
        /// </summary>
        public static string Join<T>(char separator, IEnumerable<T> values) =>
            string.Join(separator.ToString(), values);

        /// <summary>The characters of <paramref name="str0"/> followed by those of <paramref name="str1"/>.</summary>
        public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1)
        {
            var length = checked(str0.Length + str1.Length);
            if (length == 0) return string.Empty;

            var buffer = new char[length];
            str0.CopyTo(buffer);
            str1.CopyTo(buffer.AsSpan(str0.Length));
            return new string(buffer);
        }
    }

    /// <summary>TrimEntries, which .NET Framework's StringSplitOptions does not name.</summary>
    private const StringSplitOptions TrimEntries = (StringSplitOptions)2;

    /// <summary>
    /// Whether <paramref name="comparisonType"/> is OrdinalIgnoreCase rather than Ordinal. Contains
    /// checks its value before this and Replace after it, the order .NET 10 checks them in.
    /// </summary>
    /// <exception cref="NotSupportedException">A culture comparison, which has no .NET Framework port.</exception>
    /// <exception cref="ArgumentException">A value that is not a StringComparison.</exception>
    private static bool IgnoresCase(StringComparison comparisonType)
    {
        switch (comparisonType)
        {
            case StringComparison.Ordinal:
                return false;

            case StringComparison.OrdinalIgnoreCase:
                return true;

            case StringComparison.CurrentCulture:
            case StringComparison.CurrentCultureIgnoreCase:
            case StringComparison.InvariantCulture:
            case StringComparison.InvariantCultureIgnoreCase:
                throw new NotSupportedException(
                    $"StringComparison.{comparisonType} is not ported to .NET Framework: only Ordinal and OrdinalIgnoreCase search as .NET 10 does.");

            default:
                throw new ArgumentException("The string comparison type passed in is currently not supported.", nameof(comparisonType));
        }
    }

    /// <summary>
    /// The first index at or after <paramref name="startIndex"/> where <paramref name="value"/>
    /// occurs in <paramref name="text"/>, or -1. Without <paramref name="ignoreCase"/> the
    /// characters are compared as they are. With it, each window is compared by System.Memory's
    /// span Equals under OrdinalIgnoreCase, which folds a to z itself and passes the rest, from
    /// the first character outside ASCII, to string.Compare under OrdinalIgnoreCase. Neither path
    /// goes through string.IndexOf, which on .NET Framework searches ASCII text by culture rules
    /// under OrdinalIgnoreCase.
    /// </summary>
    private static int OrdinalIndexOf(string text, string value, int startIndex, bool ignoreCase)
    {
        if (!ignoreCase)
        {
            var found = text.AsSpan(startIndex).IndexOf(value.AsSpan());
            return found < 0 ? -1 : startIndex + found;
        }

        var source = text.AsSpan();
        var sought = value.AsSpan();
        for (var at = startIndex; at <= source.Length - sought.Length; at++)
        {
            if (source.Slice(at, sought.Length).Equals(sought, StringComparison.OrdinalIgnoreCase))
                return at;
        }

        return -1;
    }

    /// <summary>
    /// .NET 10's split at a single separator, a character or a string: the separators are found
    /// left to right without overlap, at most <paramref name="count"/> parts are made, and the
    /// options trim the parts and drop empty ones.
    /// </summary>
    private static string[] SplitAt(string text, string separator, int count, StringSplitOptions options)
    {
        if (text is null) throw new NullReferenceException();
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be less than zero.");

        if ((options & ~(StringSplitOptions.RemoveEmptyEntries | TrimEntries)) != 0)
            throw new ArgumentException("Value of flags is invalid.", nameof(options));

        if (count <= 1 || text.Length == 0 || separator.Length == 0)
            return SoleValue(text, options, count);

        var separators = new List<int>();
        for (var at = OrdinalIndexOf(text, separator, 0, ignoreCase: false);
             at >= 0;
             at = OrdinalIndexOf(text, separator, at + separator.Length, ignoreCase: false))
        {
            separators.Add(at);
        }

        if (separators.Count == 0) return SoleValue(text, options, count);

        return options == StringSplitOptions.None
            ? SplitWithoutPostProcessing(text, separators, separator.Length, count)
            : SplitWithPostProcessing(text, separators, separator.Length, count, options);
    }

    private static string[] SoleValue(string text, StringSplitOptions options, int count)
    {
        if (count != 0)
        {
            var candidate = (options & TrimEntries) != 0 ? text.Trim() : text;
            if ((options & StringSplitOptions.RemoveEmptyEntries) == 0 || candidate.Length != 0)
                return [candidate];
        }

        return [];
    }

    private static string[] SplitWithoutPostProcessing(string text, List<int> separators, int separatorLength, int count)
    {
        var currIndex = 0;
        var arrIndex = 0;

        count--;
        var numActualReplaces = separators.Count < count ? separators.Count : count;

        var parts = new string[numActualReplaces + 1];

        for (var i = 0; i < numActualReplaces && currIndex < text.Length; i++)
        {
            parts[arrIndex++] = text.Substring(currIndex, separators[i] - currIndex);
            currIndex = separators[i] + separatorLength;
        }

        if (currIndex < text.Length && numActualReplaces >= 0)
            parts[arrIndex] = text.Substring(currIndex);
        else if (arrIndex == numActualReplaces)
            parts[arrIndex] = string.Empty;

        return parts;
    }

    private static string[] SplitWithPostProcessing(
        string text, List<int> separators, int separatorLength, int count, StringSplitOptions options)
    {
        var trim = (options & TrimEntries) != 0;
        var removeEmpty = (options & StringSplitOptions.RemoveEmptyEntries) != 0;
        var numReplaces = separators.Count;

        var maxItems = numReplaces < count ? numReplaces + 1 : count;
        var parts = new string[maxItems];

        var currIndex = 0;
        var arrIndex = 0;
        ReadOnlySpan<char> entry;

        for (var i = 0; i < numReplaces; i++)
        {
            entry = text.AsSpan(currIndex, separators[i] - currIndex);
            if (trim) entry = entry.Trim();
            if (!entry.IsEmpty || !removeEmpty) parts[arrIndex++] = entry.ToString();
            currIndex = separators[i] + separatorLength;

            if (arrIndex == count - 1)
            {
                if (removeEmpty)
                {
                    while (++i < numReplaces)
                    {
                        entry = text.AsSpan(currIndex, separators[i] - currIndex);
                        if (trim) entry = entry.Trim();
                        if (!entry.IsEmpty) break;
                        currIndex = separators[i] + separatorLength;
                    }
                }

                break;
            }
        }

        entry = text.AsSpan(currIndex);
        if (trim) entry = entry.Trim();
        if (!entry.IsEmpty || !removeEmpty) parts[arrIndex++] = entry.ToString();

        Array.Resize(ref parts, arrIndex);
        return parts;
    }
}
