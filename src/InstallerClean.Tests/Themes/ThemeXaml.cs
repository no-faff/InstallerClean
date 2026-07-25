using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace InstallerClean.Tests.Themes;

/// <summary>
/// Reads the theme's own XAML sources, which are embedded in this assembly as
/// plain bytes (see the csproj). Shared by the tests in this folder so each one
/// asserts a tie rather than re-implementing the lookup that finds it.
/// </summary>
internal static class ThemeXaml
{
    internal static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
    internal static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    internal static XDocument Load(string logicalName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"Embedded resource '{logicalName}' is missing.");
        return XDocument.Load(stream);
    }

    /// <summary>A top-level resource's element text, trimmed (numeric tokens).</summary>
    internal static string ResourceValue(XDocument document, string key)
        => ResourceElement(document, key).Value.Trim();

    internal static XElement ResourceElement(XDocument document, string key)
        => document.Root!.Elements().Single(e => (string?)e.Attribute(Xaml + "Key") == key);

    /// <summary>
    /// The <c>#aarrggbb</c> or <c>#rrggbb</c> literal a colour resource resolves
    /// to, following one <c>{StaticResource}</c> hop from a brush in Tokens.xaml
    /// to the atom in Primitives.xaml. Anything else, an inline literal on the
    /// brush or a second hop, fails rather than being followed: the layering
    /// rule is that a token names exactly one primitive.
    /// </summary>
    internal static string ColourLiteral(XDocument tokens, XDocument primitives, string tokenKey)
    {
        var colour = (string?)ResourceElement(tokens, tokenKey).Attribute("Color")
            ?? throw new InvalidOperationException($"'{tokenKey}' has no Color attribute.");

        var reference = Regex.Match(colour, @"^\{StaticResource\s+(?<key>[^}]+)\}$");
        if (!reference.Success)
            throw new InvalidOperationException(
                $"'{tokenKey}' does not resolve to a primitive: Color=\"{colour}\".");

        var literal = ResourceValue(primitives, reference.Groups["key"].Value.Trim());
        if (!literal.StartsWith('#'))
            throw new InvalidOperationException(
                $"'{tokenKey}' resolves to '{literal}', which is not a colour literal.");

        return literal;
    }

    /// <summary>
    /// WCAG 2.1 relative-luminance contrast between two opaque colours, and
    /// composition of a translucent one over an opaque one, so a token drawn at
    /// less than full opacity can be measured against what is behind it. The
    /// implementation reproduces the three ratios Tokens.xaml states in its own
    /// comments (Decoration.Heart 3.89:1, Link.Rest 4.90:1, Link.Hover 7.34:1),
    /// which is what makes the numbers here comparable with the theme's.
    /// </summary>
    internal static double Contrast(string foreground, string background)
    {
        var (fr, fg, fb) = Composite(foreground, background);
        var (br, bg, bb) = Rgb(background);
        var lighter = Math.Max(Luminance(fr, fg, fb), Luminance(br, bg, bb));
        var darker = Math.Min(Luminance(fr, fg, fb), Luminance(br, bg, bb));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static (double R, double G, double B) Composite(string foreground, string background)
    {
        var alpha = Alpha(foreground);
        if (alpha >= 1.0)
            return Rgb(foreground);

        var (fr, fg, fb) = Rgb(foreground);
        var (br, bg, bb) = Rgb(background);
        return (alpha * fr + (1 - alpha) * br,
                alpha * fg + (1 - alpha) * bg,
                alpha * fb + (1 - alpha) * bb);
    }

    private static double Alpha(string colour)
    {
        var digits = colour.TrimStart('#');
        return digits.Length == 8 ? Byte(digits, 0) / 255.0 : 1.0;
    }

    private static (double R, double G, double B) Rgb(string colour)
    {
        var digits = colour.TrimStart('#');
        var offset = digits.Length == 8 ? 2 : 0;
        return (Byte(digits, offset), Byte(digits, offset + 2), Byte(digits, offset + 4));
    }

    private static double Byte(string digits, int offset)
        => int.Parse(digits.Substring(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private static double Luminance(double r, double g, double b)
        => 0.2126 * Channel(r) + 0.7152 * Channel(g) + 0.0722 * Channel(b);

    private static double Channel(double value)
    {
        var v = value / 255.0;
        return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }
}
