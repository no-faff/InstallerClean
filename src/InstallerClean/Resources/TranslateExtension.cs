using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Markup;
using InstallerClean.Helpers;

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

        // Strings.Get, not a lookup of this class's own: it is the single door
        // every resource read comes through, so the culture and the
        // installer-folder token cannot differ between the XAML half of a screen
        // and the C# half. A second lookup here is also how a parallel
        // ResourceManager gets built by mistake, binding to InstallerClean.dll,
        // missing the resx embedded in Core and returning literal keys.
        var value = Strings.Get(Key);

        // Every XAML-resolved string passes through KeepWhole, which is a no-op
        // unless the value names the installer cache folder and then binds it so
        // it cannot break across two lines. Doing it here rather than per site
        // means a new string naming the folder is covered by writing it. It runs
        // after the resolve above and must: the folder arrives as a token, and
        // there is no path to bind until that is spent.
        //
        // Except where the string is only ever spoken. KeepWhole's word joiners
        // are a line-breaking instruction, and an automation property is never
        // laid out, so there they buy nothing and hand a speech engine four
        // invisible format characters to make of what it will. A drawn string
        // still carries them wherever it is also the spoken one, a TextBlock's
        // peer reporting its Text as its name; this only declines to add them to
        // a string that has no rendering to protect.
        return IsSpokenOnly(serviceProvider) ? value : InstallerPathText.KeepWhole(value);
    }

    // The target property is unavailable inside a Setter or a template, where
    // ProvideValue runs once for a value shared across instances; those keep the
    // transform, which is the behaviour every site had before this split.
    private static bool IsSpokenOnly(IServiceProvider serviceProvider) =>
        serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target
        && target.TargetProperty is DependencyProperty property
        && (property == AutomationProperties.NameProperty
            || property == AutomationProperties.HelpTextProperty);
}
