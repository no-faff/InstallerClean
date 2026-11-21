using System.Reflection;
using System.Xml.Linq;

namespace InstallerClean.Tests.Themes;

// A StaticResource is looked up once, as the XAML holding it is read, so the key it
// names has to be defined above it: in an earlier element of the same file, in a file
// that file merges, or in the application's resources a window reads after them. A key
// defined further down the same file is not found when the XAML is read whole and
// eagerly, which XamlReader does, and a test parsing the theme that way then fails on
// the first style naming it. A typed key the app never defines names the Windows
// theme's own style for that type and is not looked for here.
public class StaticResourceOrderTests
{
    private static readonly string[] ThemeFiles = { "Primitives.xaml", "Tokens.xaml", "Components.xaml" };

    private static readonly string[] Windows = Assembly.GetExecutingAssembly()
        .GetManifestResourceNames()
        .Where(n => n.StartsWith("ThemeXaml.", StringComparison.Ordinal)
            && n.EndsWith(".xaml", StringComparison.Ordinal))
        .Select(n => n.Substring("ThemeXaml.".Length))
        .Where(n => !ThemeFiles.Contains(n))
        .OrderBy(n => n, StringComparer.Ordinal)
        .ToArray();

    [Fact]
    public void Every_theme_key_is_defined_above_the_first_StaticResource_naming_it()
    {
        var available = new HashSet<string>(StringComparer.Ordinal);
        var allThemeKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in ThemeFiles)
            allThemeKeys.UnionWith(KeysIn(ThemeXaml.Load("ThemeXaml." + file).Root!));

        var late = new List<string>();
        foreach (var file in ThemeFiles)
            late.AddRange(Walk(ThemeXaml.Load("ThemeXaml." + file).Root!, file, available, allThemeKeys));

        Assert.True(late.Count == 0, "Named before it is defined:\n" + string.Join("\n", late));
    }

    // Every page the app compiles is read here: its built pages are listed from the app's
    // own resources, so a window added to the app and not embedded for these tests fails.
    [Fact]
    public void Every_page_the_app_builds_is_read()
    {
        var resources = typeof(App).Assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith(".g.resources", StringComparison.Ordinal));
        using var stream = typeof(App).Assembly.GetManifestResourceStream(resources)!;
        using var reader = new System.Resources.ResourceReader(stream);
        var built = reader.Cast<System.Collections.DictionaryEntry>()
            .Select(e => (string)e.Key)
            .Where(k => k.EndsWith(".baml", StringComparison.Ordinal))
            .Select(k => Path.GetFileNameWithoutExtension(k))
            .Where(n => n != "app")
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        var read = ThemeFiles.Concat(Windows)
            .Select(n => Path.GetFileNameWithoutExtension(n).ToLowerInvariant())
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(built);
        Assert.Equal(built, read);
    }

    [Fact]
    public void Every_window_key_is_defined_above_the_first_StaticResource_naming_it()
    {

        var themeKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in ThemeFiles)
            themeKeys.UnionWith(KeysIn(ThemeXaml.Load("ThemeXaml." + file).Root!));

        var late = new List<string>();
        foreach (var window in Windows)
        {
            var root = ThemeXaml.Load("ThemeXaml." + window).Root!;
            var known = new HashSet<string>(themeKeys, StringComparer.Ordinal);
            known.UnionWith(KeysIn(root));
            late.AddRange(Walk(root, window, new HashSet<string>(themeKeys, StringComparer.Ordinal), known));
        }

        Assert.True(late.Count == 0, "Named before it is defined:\n" + string.Join("\n", late));
    }

    [Fact]
    public void The_walk_reads_setters_and_based_on_styles_in_the_theme()
    {
        var references = ThemeXaml.Load("ThemeXaml.Components.xaml").Root!
            .DescendantsAndSelf()
            .SelectMany(e => e.Attributes())
            .SelectMany(a => References(a.Value))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("FlatTextBoxFocusVisual", references);
        Assert.Contains("SelectableText", references);
        Assert.Contains("{x:Type GridViewColumnHeader}", references);
    }

    [Fact]
    public void A_key_defined_below_its_first_use_is_reported()
    {
        var root = XElement.Parse(
            "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\""
            + " xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">"
            + "<Style x:Key=\"Early\"><Setter Property=\"FocusVisualStyle\" Value=\"{StaticResource Late}\"/></Style>"
            + "<Style x:Key=\"Late\"/>"
            + "<Style x:Key=\"After\" BasedOn=\"{StaticResource Early}\"/>"
            + "<Style x:Key=\"Based\" BasedOn=\"{StaticResource Base}\"/>"
            + "<Style x:Key=\"Base\"/>"
            + "<Style x:Key=\"Scoped\"><Style.Resources><Style x:Key=\"Inner\"/></Style.Resources>"
            + "<Setter Property=\"Tag\" Value=\"{StaticResource Inner}\"/></Style>"
            + "<Style x:Key=\"Outside\" BasedOn=\"{StaticResource Inner}\"/>"
            + "</ResourceDictionary>");
        var known = KeysIn(root);

        var late = Walk(root, "test.xaml", new HashSet<string>(StringComparer.Ordinal), known);

        Assert.Equal(new[]
        {
            "test.xaml: Late, in Setter",
            "test.xaml: Base, in Style",
            "test.xaml: Inner, in Style",
        }, late);
    }

    [Theory]
    [InlineData("{StaticResource Card}", "Card")]
    [InlineData("{StaticResource  Card }", "Card")]
    [InlineData("{StaticResource ResourceKey=Card}", "Card")]
    [InlineData("{StaticResource {x:Type ScrollBar}}", "{x:Type ScrollBar}")]
    [InlineData("{Binding Path=A, Converter={StaticResource Conv}}", "Conv")]
    [InlineData("{DynamicResource Card}", null)]
    [InlineData("{StaticResourceX Card}", null)]
    public void A_reference_is_read_out_of_its_markup_extension(string value, string? expected)
    {
        var found = References(value).ToArray();

        Assert.Equal(expected is null ? Array.Empty<string>() : new[] { expected }, found);
    }

    /// <summary>
    /// Walks <paramref name="root"/> in document order. Each StaticResource on an element
    /// is checked against the keys whose elements have already closed in the element's own
    /// scope or one around it, and a key joins the scope of the nearest element around it
    /// that is not a property element, so a key in a <c>Style.Resources</c> or a
    /// <c>ListView.Resources</c> is seen inside that style or list only. The root's own keys
    /// join <paramref name="available"/>, which the next file of a theme reads. A name
    /// found nowhere in <paramref name="known"/> that is a typed key belongs to the Windows
    /// theme and is skipped; any other name found nowhere is reported as well.
    /// </summary>
    private static List<string> Walk(XElement root, string file, HashSet<string> available, HashSet<string> known)
    {
        var late = new List<string>();
        var scopes = new List<HashSet<string>> { available };
        Visit(root, isRoot: true);
        return late;

        void Visit(XElement element, bool isRoot)
        {
            foreach (var reference in element.Attributes().SelectMany(a => References(a.Value)))
            {
                if (scopes.Any(scope => scope.Contains(reference)))
                    continue;
                if (!known.Contains(reference) && reference.StartsWith("{x:Type ", StringComparison.Ordinal))
                    continue;
                late.Add($"{file}: {reference}, in {element.Name.LocalName}");
            }

            // A property element (Style.Resources, Setter.Value) opens no scope of its own:
            // what it holds belongs to the element it sets. The root's scope is the one handed in.
            var opensScope = !isRoot && element.Name.LocalName.IndexOf('.') < 0;
            if (opensScope)
                scopes.Add(new HashSet<string>(StringComparer.Ordinal));
            foreach (var child in element.Elements())
                Visit(child, isRoot: false);
            if (opensScope)
                scopes.RemoveAt(scopes.Count - 1);

            var key = KeyOf(element);
            if (key is not null)
                scopes[scopes.Count - 1].Add(key);
        }
    }

    private static HashSet<string> KeysIn(XElement root)
        => root.DescendantsAndSelf().Select(KeyOf).OfType<string>().ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// An element's resource key: its x:Key, or for a Style with none, the type it
    /// targets, written as the {x:Type} a reference to it names.
    /// </summary>
    private static string? KeyOf(XElement element)
    {
        var key = (string?)element.Attribute(ThemeXaml.Xaml + "Key");
        if (key is not null)
            return Normalise(key);
        if (element.Name != ThemeXaml.Presentation + "Style")
            return null;
        var target = (string?)element.Attribute("TargetType");
        if (target is null)
            return null;
        target = target.Trim();
        if (target.StartsWith("{x:Type ", StringComparison.Ordinal) && target.EndsWith("}", StringComparison.Ordinal))
            target = target.Substring("{x:Type ".Length, target.Length - "{x:Type ".Length - 1).Trim();
        return "{x:Type " + target + "}";
    }

    /// <summary>Every key a StaticResource names inside an attribute value.</summary>
    private static IEnumerable<string> References(string value)
    {
        const string Marker = "{StaticResource";
        var at = 0;
        while ((at = value.IndexOf(Marker, at, StringComparison.Ordinal)) >= 0)
        {
            var i = at + Marker.Length;
            at = i;
            if (i >= value.Length || (value[i] != ' ' && value[i] != '}'))
                continue;
            while (i < value.Length && value[i] == ' ')
                i++;
            if (string.CompareOrdinal(value, i, "ResourceKey=", 0, "ResourceKey=".Length) == 0)
                i += "ResourceKey=".Length;

            int end;
            if (i < value.Length && value[i] == '{')
            {
                var depth = 0;
                end = i;
                do
                {
                    if (value[end] == '{') depth++;
                    else if (value[end] == '}') depth--;
                    end++;
                }
                while (end < value.Length && depth > 0);
            }
            else
            {
                end = i;
                while (end < value.Length && value[end] != '}' && value[end] != ',' && value[end] != ' ')
                    end++;
            }

            if (end > i)
                yield return Normalise(value.Substring(i, end - i));
        }
    }

    /// <summary>A typed key with its inner spacing made single, so two spellings compare.</summary>
    private static string Normalise(string key)
    {
        key = key.Trim();
        if (!key.StartsWith("{x:Type", StringComparison.Ordinal))
            return key;
        var inner = key.Substring("{x:Type".Length).TrimEnd('}').Trim();
        return "{x:Type " + inner + "}";
    }
}
