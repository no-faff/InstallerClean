using System.Text;

namespace System.IO
{
    /// <summary>
    /// Whether a name matches a search pattern, as .NET 10's folder listings decide it on
    /// Windows (dotnet/runtime v10.0.0, <c>FileSystemName.cs</c>): the pattern's <c>*</c> and
    /// <c>?</c> are read as Win32 reads them, and only the entry's own name is matched, never
    /// its 8.3 short name.
    /// </summary>
    internal static class FrameworkFileSystemName
    {
        // Changes '*' and '?' into the DOS wildcards '<', '>' and '"' so matching follows Win32.
        internal static string TranslateWin32Expression(string? expression)
        {
            if (string.IsNullOrEmpty(expression) || expression == "*" || expression == "*.*")
                return "*";

            bool modified = false;
            StringBuilder sb = new StringBuilder(32);
            int length = expression!.Length;
            for (int i = 0; i < length; i++)
            {
                char c = expression[i];
                switch (c)
                {
                    case '.':
                        modified = true;
                        if (i >= 1 && i == length - 1 && expression[i - 1] == '*')
                        {
                            sb[sb.Length - 1] = '<'; // DOS_STAR (ends in *.)
                        }
                        else if (i < length - 1 && (expression[i + 1] == '?' || expression[i + 1] == '*'))
                        {
                            sb.Append('\"'); // DOS_DOT
                        }
                        else
                        {
                            sb.Append('.');
                        }
                        break;
                    case '?':
                        modified = true;
                        sb.Append('>'); // DOS_QM
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return modified ? sb.ToString() : expression;
        }

        // Matches with the extended (DOS) wildcards '*', '?', '<', '>' and '"'; backslash escapes.
        internal static bool MatchesWin32Expression(ReadOnlySpan<char> expression, ReadOnlySpan<char> name, bool ignoreCase = true)
        {
            return MatchPattern(expression, name, ignoreCase, useExtendedWildcards: true);
        }

        private static bool ContainsWildcard(ReadOnlySpan<char> span, bool useExtendedWildcards)
        {
            for (int i = 0; i < span.Length; i++)
            {
                char c = span[i];
                if (c == '*' || c == '?' || (useExtendedWildcards && (c == '\"' || c == '<' || c == '>')))
                    return true;
            }
            return false;
        }

        // Simple upper-case ranges for code points above U+FFFF: first, last, delta (to add).
        // Taken from .NET 10's casing of supplementary characters (Deseret, Osage, Vithkuqi, Old Hungarian,
        // Warang Citi, Medefaidrin and Adlam).
        private static readonly int[] s_astralUpper =
        {
            0x10428, 0x1044F, -40, 0x104D8, 0x104FB, -40, 0x10597, 0x105A1, -39, 0x105A3, 0x105B1, -39,
            0x105B3, 0x105B9, -39, 0x105BB, 0x105BC, -39, 0x10CC0, 0x10CF2, -64, 0x10D70, 0x10D85, -32,
            0x118C0, 0x118DF, -32, 0x16E60, 0x16E7F, -32, 0x1E922, 0x1E943, -34,
        };

        // The upper case used for ordinal ignore-case comparison. A non-ASCII character never folds onto
        // an ASCII one (.NET keeps U+017F and U+0131 apart from 'S' and 'I').
        private static char OrdinalUpper(char c)
        {
            char u = char.ToUpperInvariant(c);
            return (c >= 0x80 && u < 0x80) ? c : u;
        }

        private static int OrdinalUpperScalar(int scalar)
        {
            for (int i = 0; i < s_astralUpper.Length; i += 3)
            {
                if (scalar >= s_astralUpper[i] && scalar <= s_astralUpper[i + 1])
                    return scalar + s_astralUpper[i + 2];
            }
            return scalar;
        }

        // Stands in for ReadOnlySpan<char>.EndsWith(value, Ordinal or OrdinalIgnoreCase). The ignore-case
        // walk follows .NET's ordinal comparison: a valid surrogate pair is compared as one scalar, and a
        // pair never equals a lone character.
        private static bool EndsWithOrdinal(ReadOnlySpan<char> name, ReadOnlySpan<char> suffix, bool ignoreCase)
        {
            if (suffix.Length > name.Length)
                return false;
            int offset = name.Length - suffix.Length;
            if (!ignoreCase)
            {
                for (int i = 0; i < suffix.Length; i++)
                {
                    if (name[offset + i] != suffix[i])
                        return false;
                }
                return true;
            }

            int length = suffix.Length;
            int index = 0;
            while (index < length)
            {
                char a = name[offset + index];
                char b = suffix[index];
                bool pairA = char.IsHighSurrogate(a) && index < length - 1 && char.IsLowSurrogate(name[offset + index + 1]);
                bool pairB = char.IsHighSurrogate(b) && index < length - 1 && char.IsLowSurrogate(suffix[index + 1]);
                if (pairA != pairB)
                    return false;
                if (pairA)
                {
                    char lowA = name[offset + index + 1];
                    char lowB = suffix[index + 1];
                    if (!(a == b && lowA == lowB))
                    {
                        int scalarA = char.ConvertToUtf32(a, lowA);
                        int scalarB = char.ConvertToUtf32(b, lowB);
                        if (OrdinalUpperScalar(scalarA) != OrdinalUpperScalar(scalarB))
                            return false;
                    }
                    index += 2;
                    continue;
                }
                if (a != b && OrdinalUpper(a) != OrdinalUpper(b))
                    return false;
                index++;
            }
            return true;
        }

        private static bool MatchPattern(ReadOnlySpan<char> expression, ReadOnlySpan<char> name, bool ignoreCase, bool useExtendedWildcards)
        {
            // The idea behind the algorithm is pretty simple. We keep track of all possible locations
            // in the regular expression that are matching the name. When the name has been exhausted,
            // if one of the locations in the expression is also just exhausted, the name is in the
            // language defined by the regular expression.

            if (expression.Length == 0 || name.Length == 0)
                return false;

            if (expression[0] == '*')
            {
                // Just * matches everything
                if (expression.Length == 1)
                    return true;

                ReadOnlySpan<char> expressionEnd = expression.Slice(1);

                // [MS - FSA] 2.1.4.4 Algorithm for Determining if a FileName Is in an Expression
                // https://msdn.microsoft.com/en-us/library/ff469270.aspx
                bool hasWildcards = ContainsWildcard(expressionEnd, useExtendedWildcards);
                if (!hasWildcards)
                {
                    // Handle the special case of a single starting *, which essentially means "ends with"

                    // If the name doesn't have enough characters to match the remaining expression, it can't be a match.
                    if (name.Length < expressionEnd.Length)
                        return false;

                    // See if we end with the expression
                    return EndsWithOrdinal(name, expressionEnd, ignoreCase);
                }
            }

            int nameOffset = 0;
            int expressionOffset;

            int priorMatch;
            int currentMatch;
            int priorMatchCount;
            int matchCount = 1;

            char nameChar = '\0';
            char expressionChar;

            scoped Span<int> temp = default;
            Span<int> currentMatches = stackalloc int[16];
            Span<int> priorMatches = stackalloc int[16];
            priorMatches[0] = 0;

            int maxState = expression.Length * 2;
            int currentState;
            bool nameFinished = false;

            //  Walk through the name string, picking off characters.  We go one
            //  character beyond the end because some wild cards are able to match
            //  zero characters beyond the end of the string.
            //
            //  With each new name character we determine a new set of states that
            //  match the name so far.  We use two arrays that we swap back and forth
            //  for this purpose.  One array lists the possible expression states for
            //  all name characters up to but not including the current one, and other
            //  array is used to build up the list of states considering the current
            //  name character as well.  The arrays are then switched and the process
            //  repeated.
            //
            //  There is not a one-to-one correspondence between state number and
            //  offset into the expression. State numbering is not continuous.
            //  This allows a simple conversion between state number and expression
            //  offset.  Each character in the expression can represent one or two
            //  states.  * and DOS_STAR generate two states: expressionOffset * 2 and
            //  expressionOffset * 2 + 1.  All other expression characters can produce
            //  only a single state.  Thus expressionOffset = currentState / 2.

            while (!nameFinished)
            {
                if (nameOffset < name.Length)
                {
                    // Not at the end of the name. Grab the current character and move the offset forward.
                    nameChar = name[nameOffset++];
                }
                else
                {
                    // At the end of the name. If the expression is exhausted, exit.
                    if (priorMatches[matchCount - 1] == maxState)
                        break;

                    nameFinished = true;
                }

                // Now, for each of the previous stored expression matches, see what
                // we can do with this name character.
                priorMatch = 0;
                currentMatch = 0;
                priorMatchCount = 0;

                while (priorMatch < matchCount)
                {
                    // We have to carry on our expression analysis as far as possible for each
                    // character of name, so we loop here until the expression stops matching.

                    expressionOffset = (priorMatches[priorMatch++] + 1) / 2;

                    while (expressionOffset < expression.Length)
                    {
                        currentState = expressionOffset * 2;
                        expressionChar = expression[expressionOffset];

                        // We may be about to exhaust the local space for matches,
                        // so we have to reallocate if this is the case.
                        if (currentMatch >= currentMatches.Length - 2)
                        {
                            int newSize = currentMatches.Length * 2;
                            temp = new int[newSize];
                            currentMatches.CopyTo(temp);
                            currentMatches = temp;

                            temp = new int[newSize];
                            priorMatches.CopyTo(temp);
                            priorMatches = temp;
                        }

                        if (expressionChar == '*')
                        {
                            // '*' matches any character zero or more times.
                            goto MatchZeroOrMore;
                        }
                        else if (useExtendedWildcards && expressionChar == '<')
                        {
                            // '<' (DOS_STAR) matches any character except '.' zero or more times.

                            // If we are at a period, determine if we are allowed to
                            // consume it, i.e. make sure it is not the last one.

                            bool notLastPeriod = false;
                            if (!nameFinished && nameChar == '.')
                            {
                                for (int offset = nameOffset; offset < name.Length; offset++)
                                {
                                    if (name[offset] == '.')
                                    {
                                        notLastPeriod = true;
                                        break;
                                    }
                                }
                            }

                            if (nameFinished || nameChar != '.' || notLastPeriod)
                            {
                                goto MatchZeroOrMore;
                            }
                            else
                            {
                                // We are at a period.  We can only match zero
                                // characters (i.e. the epsilon transition).
                                goto MatchZero;
                            }
                        }
                        else
                        {
                            // The remaining expression characters all match by consuming a character,
                            // so we need to force the expression and state forward.
                            currentState += 2;

                            if (useExtendedWildcards && expressionChar == '>')
                            {
                                // '>' (DOS_QM) is the most complicated. If the name is finished,
                                // we can match zero characters. If this name is a '.', we
                                // don't match, but look at the next expression.  Otherwise
                                // we match a single character.
                                if (nameFinished || nameChar == '.')
                                    goto NextExpressionCharacter;

                                currentMatches[currentMatch++] = currentState;
                                goto ExpressionFinished;
                            }
                            else if (useExtendedWildcards && expressionChar == '"')
                            {
                                // A '"' (DOS_DOT) can match either a period, or zero characters
                                // beyond the end of name.
                                if (nameFinished)
                                {
                                    goto NextExpressionCharacter;
                                }
                                else if (nameChar == '.')
                                {
                                    currentMatches[currentMatch++] = currentState;
                                }
                                goto ExpressionFinished;
                            }
                            else
                            {
                                if (expressionChar == '\\')
                                {
                                    // Escape character, try to move the expression forward again and match literally.
                                    if (++expressionOffset == expression.Length)
                                    {
                                        currentMatches[currentMatch++] = maxState;
                                        goto ExpressionFinished;
                                    }

                                    currentState = expressionOffset * 2 + 2;
                                    expressionChar = expression[expressionOffset];
                                }

                                // From this point on a name character is required to even
                                // continue, let alone make a match.
                                if (nameFinished) goto ExpressionFinished;

                                if (expressionChar == '?')
                                {
                                    // If this expression was a '?' we can match it once.
                                    currentMatches[currentMatch++] = currentState;
                                }
                                else if (ignoreCase
                                    ? char.ToUpperInvariant(expressionChar) == char.ToUpperInvariant(nameChar)
                                    : expressionChar == nameChar)
                                {
                                    // Matched a non-wildcard character
                                    currentMatches[currentMatch++] = currentState;
                                }

                                goto ExpressionFinished;
                            }
                        }

                        MatchZeroOrMore:
                            currentMatches[currentMatch++] = currentState;
                        MatchZero:
                            currentMatches[currentMatch++] = currentState + 1;
                        NextExpressionCharacter:
                            if (++expressionOffset == expression.Length)
                                currentMatches[currentMatch++] = maxState;
                    } // while (expressionOffset < expression.Length)

                    ExpressionFinished:

                    // Prevent duplication in the destination array.
                    //
                    // Each of the arrays is monotonically increasing and non-duplicating, thus we skip
                    // over any source element in the source array if we just added the same element to
                    // the destination array. This guarantees non-duplication in the destination array.

                    if ((priorMatch < matchCount) && (priorMatchCount < currentMatch))
                    {
                        while (priorMatchCount < currentMatch)
                        {
                            int previousLength = priorMatches.Length;
                            while ((priorMatch < previousLength) && (priorMatches[priorMatch] < currentMatches[priorMatchCount]))
                            {
                                priorMatch++;
                            }
                            priorMatchCount++;
                        }
                    }
                } // while (sourceCount < matchesCount)

                // If we found no matches in the just finished iteration it's time to bail.
                if (currentMatch == 0)
                    return false;

                // Swap the meaning the two arrays
                temp = priorMatches;
                priorMatches = currentMatches;
                currentMatches = temp;

                matchCount = currentMatch;
            } // while (!nameFinished)

            currentState = priorMatches[matchCount - 1];

            return currentState == maxState;
        }
    }
}
