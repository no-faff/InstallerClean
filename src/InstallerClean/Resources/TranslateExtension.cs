using System;
using System.Globalization;
using System.Resources;
using System.Windows.Markup;

namespace InstallerClean.Resources;

/// <summary>
/// XAML markup extension that resolves a key from <c>Strings.resx</c>.
/// Used as <c>{loc:Translate Window.Main.Title}</c>; the key uses dot
/// notation matching the resx data names. Resolution happens at the
/// current UI culture, so a satellite assembly for a different locale
/// flows through automatically.
/// </summary>
/// <remarks>
/// C# code paths use the strongly-typed <c>Strings</c> class directly
/// for compile-time safety. XAML uses this extension instead, which
/// resolves keys at runtime: a missing key falls back to returning the
/// key itself, so a misspelled <c>{loc:Translate Window.Main.Titel}</c>
/// renders as the literal "Window.Main.Titel" in the UI rather than a
/// blank or a null. Visible typos are easier to fix than silent ones.
/// </remarks>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TranslateExtension : MarkupExtension
{
    // Reuse the auto-generated Strings class's ResourceManager rather
    // than constructing a parallel one. The resx is embedded in
    // InstallerClean.Core; a fresh `new ResourceManager(...,
    // typeof(this).Assembly)` would resolve typeof(this).Assembly to
    // InstallerClean.dll (the WPF host), miss the embedded resources,
    // and return literal keys for every XAML binding.
    private static readonly ResourceManager ResourceManager = Strings.ResourceManager;

    public TranslateExtension() { }

    public TranslateExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string? Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            return string.Empty;

        return ResourceManager.GetString(Key, CultureInfo.CurrentUICulture)
            ?? Key;
    }
}
