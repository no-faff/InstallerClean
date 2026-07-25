using System;
using System.Resources;
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

        var value = ResourceManager.GetString(Key, Localisation.UiCulture) ?? Key;

        // Every XAML-resolved string passes through KeepWhole, which is a no-op
        // unless the value names C:\Windows\Installer and then binds it so it
        // cannot break across two lines. Doing it here rather than per site
        // means a new string naming the folder is covered by writing it.
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
