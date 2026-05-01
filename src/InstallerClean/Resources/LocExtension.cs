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
/// C# code paths use the auto-generated <c>Strings</c> class directly
/// for compile-time safety. XAML can't reach that class because the
/// MSBuild StronglyTypedClassName task always emits it as
/// <c>internal</c> and <c>{x:Static}</c> requires public; this
/// extension is the XAML-side equivalent.
///
/// On lookup miss the key itself is returned, so a typo is visually
/// obvious in the running UI rather than producing a blank or null.
/// </remarks>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TranslateExtension : MarkupExtension
{
    private static readonly ResourceManager ResourceManager =
        new("InstallerClean.Resources.Strings", typeof(TranslateExtension).Assembly);

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
