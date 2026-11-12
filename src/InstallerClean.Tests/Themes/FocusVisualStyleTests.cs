using System.Xml.Linq;

namespace InstallerClean.Tests.Themes;

// .NET Framework draws an element's default focus visual from the Windows theme
// alone: KeyboardNavigation looks the default up in the system resources, never in
// the application's, so a style keyed on SystemParameters.FocusVisualStyleKey in the
// app's dictionaries is never drawn. An element whose style names no focus visual
// therefore draws the theme's dotted ring, and the theme's own styles for list rows
// name that ring too. Every focusable element names one of the app's visuals, or
// none, so the app draws one ring.
public class FocusVisualStyleTests
{
    // The types the app's XAML uses that take keyboard focus unless told not to.
    private static readonly HashSet<string> FocusableTypes = new(StringComparer.Ordinal)
    {
        "Button", "ToggleButton", "DisclosureButton", "CheckBox", "TextBox",
        "ListView", "ListBox", "ListViewItem", "ListBoxItem", "GridViewColumnHeader",
        "MenuItem", "Hyperlink", "ScrollViewer",
    };

    private static readonly string[] Documents =
    {
        "Components.xaml", "MainWindow.xaml", "AboutWindow.xaml", "ConfirmDeleteWindow.xaml",
        "ConfirmMoveWindow.xaml", "MessageWindow.xaml", "ReportWindow.xaml",
        "OrphanedFilesWindow.xaml", "RegisteredFilesWindow.xaml", "SplashWindow.xaml",
        "ScoopCommandBox.xaml",
    };

    [Theory]
    [InlineData("SubtleLink", "FlatTextBoxFocusVisual")]
    [InlineData("BodyScrollRegion", "FlatTextBoxFocusVisual")]
    [InlineData("SelectableText", "FlatTextBoxFocusVisual")]
    [InlineData("{x:Type ListView}", "FlatTextBoxFocusVisual")]
    [InlineData("{x:Type ListBox}", "FlatTextBoxFocusVisual")]
    [InlineData("{x:Type ListViewItem}", "InsetFocusVisual")]
    [InlineData("{x:Type ListBoxItem}", "InsetFocusVisual")]
    [InlineData("{x:Type GridViewColumnHeader}", "InsetFocusVisual")]
    [InlineData("{x:Type MenuItem}", "InsetFocusVisual")]
    public void The_style_names_the_app_s_ring(string key, string visual)
    {
        var style = StyleIn(Components(), key);

        Assert.NotNull(style);
        Assert.Equal("{StaticResource " + visual + "}", FocusVisualSetter(style!));
    }

    // Every style for a focusable type, in the theme and in each window, names a focus
    // visual itself, takes the focus away, or is based on a style that does.
    [Fact]
    public void Every_style_for_a_focusable_type_names_a_focus_visual()
    {
        var checkedStyles = new List<string>();
        var unnamed = new List<string>();
        foreach (var name in Documents)
        {
            var document = Load(name);
            foreach (var style in document.Descendants(ThemeXaml.Presentation + "Style"))
            {
                var type = TypeName((string?)style.Attribute("TargetType"));
                if (type is null || !FocusableTypes.Contains(type))
                    continue;

                var label = $"{name}: {(string?)style.Attribute(ThemeXaml.Xaml + "Key") ?? type}";
                checkedStyles.Add(label);
                if (!NamesAFocusVisual(style, document))
                    unnamed.Add(label);
            }
        }

        Assert.True(checkedStyles.Count >= 20, string.Join(", ", checkedStyles));
        Assert.True(unnamed.Count == 0, string.Join(", ", unnamed));
    }

    // Every focusable element in a window, and in the theme's own menus, draws through
    // a style that names a focus visual, or names one itself. Elements inside a control
    // template are the template's parts, not the window's, and are left out.
    [Fact]
    public void Every_focusable_element_names_a_focus_visual()
    {
        var checkedElements = 0;
        var unnamed = new List<string>();
        foreach (var name in Documents)
        {
            var document = Load(name);
            foreach (var element in document.Descendants())
            {
                var type = element.Name.LocalName;
                if (type.IndexOf('.') >= 0 || !FocusableTypes.Contains(type)
                    || element.Ancestors(ThemeXaml.Presentation + "ControlTemplate").Any())
                    continue;

                checkedElements++;
                if (!DrawsANamedFocusVisual(element, document))
                    unnamed.Add($"{name}: {type} {(string?)element.Attribute(ThemeXaml.Xaml + "Name")}".TrimEnd());
            }

            // The copy button in the Scoop command box takes its style from each box.
            foreach (var box in document.Descendants().Where(e => e.Name.LocalName == "ScoopCommandBox"))
            {
                checkedElements++;
                var style = Resolve((string?)box.Attribute("CopyButtonStyle"), document);
                if (style is null || !NamesAFocusVisual(style, document))
                    unnamed.Add($"{name}: the copy button of a ScoopCommandBox");
            }
        }

        Assert.True(checkedElements >= 80, $"Only {checkedElements} focusable elements were read.");
        Assert.True(unnamed.Count == 0, string.Join(", ", unnamed));
    }

    private static bool DrawsANamedFocusVisual(XElement element, XDocument document)
    {
        if (element.Attribute("FocusVisualStyle") is not null
            || (string?)element.Attribute("Focusable") == "False")
            return true;

        var styleAttribute = (string?)element.Attribute("Style");
        if (styleAttribute is not null)
        {
            // Bound at the site: the Scoop command box's copy button, checked through
            // each box's CopyButtonStyle.
            if (styleAttribute.StartsWith("{Binding CopyButtonStyle", StringComparison.Ordinal))
                return true;
            return Resolve(styleAttribute, document) is { } keyed && NamesAFocusVisual(keyed, document);
        }

        var local = element.Element(element.Name + ".Style")?.Element(ThemeXaml.Presentation + "Style");
        if (local is not null)
            return NamesAFocusVisual(local, document);

        var implicitStyle = StyleIn(document, "{x:Type " + element.Name.LocalName + "}")
            ?? StyleIn(Components(), "{x:Type " + element.Name.LocalName + "}");
        return implicitStyle is not null && NamesAFocusVisual(implicitStyle, document);
    }

    private static bool NamesAFocusVisual(XElement style, XDocument document)
    {
        for (var current = style; current is not null;)
        {
            var setters = current.Elements(ThemeXaml.Presentation + "Setter").ToList();
            if (setters.Any(s => (string?)s.Attribute("Property") == "FocusVisualStyle")
                || setters.Any(s => (string?)s.Attribute("Property") == "Focusable" && (string?)s.Attribute("Value") == "False"))
                return true;

            var basedOn = (string?)current.Attribute("BasedOn");
            current = basedOn is null ? null : Resolve(basedOn, document);
        }
        return false;
    }

    // A {StaticResource ...} reference to a style, looked up in the document and then
    // in the theme: a key, or {x:Type X} for the implicit style of X.
    private static XElement? Resolve(string? reference, XDocument document)
    {
        if (reference is null || !reference.StartsWith("{StaticResource ", StringComparison.Ordinal))
            return null;
        var key = reference.Substring("{StaticResource ".Length, reference.Length - "{StaticResource ".Length - 1).Trim();
        return StyleIn(document, key) ?? StyleIn(Components(), key);
    }

    private static XElement? StyleIn(XDocument document, string key) =>
        document.Descendants(ThemeXaml.Presentation + "Style").FirstOrDefault(s =>
            key.StartsWith("{x:Type ", StringComparison.Ordinal)
                ? s.Attribute(ThemeXaml.Xaml + "Key") is null
                  && TypeName((string?)s.Attribute("TargetType")) == key.Substring(8, key.Length - 9)
                  && s.Parent?.Name.LocalName is "ResourceDictionary" or "Resources" or "Window.Resources" or "UserControl.Resources"
                : (string?)s.Attribute(ThemeXaml.Xaml + "Key") == key);

    private static string? FocusVisualSetter(XElement style) =>
        (string?)style.Elements(ThemeXaml.Presentation + "Setter")
            .FirstOrDefault(s => (string?)s.Attribute("Property") == "FocusVisualStyle")?.Attribute("Value");

    // "Button", "{x:Type Button}" or "{x:Type chrome:DisclosureButton}" as "Button" or
    // "DisclosureButton".
    private static string? TypeName(string? targetType)
    {
        if (targetType is null)
            return null;
        var name = targetType.StartsWith("{x:Type ", StringComparison.Ordinal)
            ? targetType.Substring(8, targetType.Length - 9).Trim()
            : targetType;
        var colon = name.IndexOf(':');
        return colon < 0 ? name : name.Substring(colon + 1);
    }

    private static XDocument Components() => ThemeXaml.Load("ThemeXaml.Components.xaml");

    private static XDocument Load(string name) => ThemeXaml.Load("ThemeXaml." + name);
}
