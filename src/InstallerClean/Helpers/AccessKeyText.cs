using System.Text;

namespace InstallerClean.Helpers;

/// <summary>
/// Takes the access key out of a label, for a button that shows the same words as
/// another button but must not answer to the same letter.
///
/// WPF reads a label's first single underscore as the marker and the letter after
/// it as the key; a doubled underscore is a literal one and marks nothing. The
/// marker can sit at the start of a word, inside it, or in the parenthesised
/// suffix Japanese, Korean and Chinese labels carry, <c>完全に削除(_D)</c>. In the
/// suffix the whole group goes, since <c>(D)</c> left on the label names a key
/// the button no longer has.
///
/// The result is still read by a presenter that recognises access keys, so a
/// doubled underscore is kept doubled, and any single underscore left after the
/// marker is doubled as well, which draws it as it stands and leaves nothing for
/// WPF to take as a second marker.
/// </summary>
internal static class AccessKeyText
{
    public static string Remove(string? label)
    {
        if (string.IsNullOrEmpty(label))
            return label ?? string.Empty;

        int marker = FirstMarker(label);
        if (marker < 0)
            return label;

        string rest;
        if (IsSuffixGroup(label, marker))
        {
            // The group runs from its opening bracket to its closing one, and the
            // space a language may put in front of it goes with it.
            int start = marker - 1;
            while (start > 0 && char.IsWhiteSpace(label[start - 1]))
                start--;
            rest = label[..start] + label[(marker + 3)..];
        }
        else
        {
            rest = label[..marker] + label[(marker + 1)..];
        }

        return EscapeSingleUnderscores(rest);
    }

    // The first underscore not paired with the one after it, consuming pairs left
    // to right, so "__X" marks nothing and "___X" marks X.
    private static int FirstMarker(string label)
    {
        for (int i = 0; i < label.Length - 1; i++)
        {
            if (label[i] != '_') continue;
            if (label[i + 1] == '_') { i++; continue; }
            return i;
        }
        return -1;
    }

    private static bool IsSuffixGroup(string label, int marker) =>
        marker >= 1
        && marker + 2 < label.Length
        && label[marker - 1] is '(' or '（'
        && label[marker + 2] is ')' or '）';

    private static string EscapeSingleUnderscores(string text)
    {
        if (!text.Contains('_'))
            return text;

        var escaped = new StringBuilder(text.Length + 2);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '_' && i + 1 < text.Length && text[i + 1] == '_')
            {
                escaped.Append("__");
                i++;
            }
            else if (text[i] == '_')
            {
                escaped.Append("__");
            }
            else
            {
                escaped.Append(text[i]);
            }
        }
        return escaped.ToString();
    }
}
