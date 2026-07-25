using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Markup;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Resources;

/// <summary>
/// The XAML string resolver, and specifically its one branch: a value bound for
/// <c>C:\Windows\Installer</c> when it will be drawn, left alone when it will
/// only ever be spoken. The word joiner is a line-breaking instruction and an
/// automation property is never laid out, so putting them there hands a speech
/// engine four invisible format characters for nothing.
///
/// Assertable without a UI thread: the extension resolves a resource and
/// inspects the target property, neither of which needs a Dispatcher. Whether a
/// speech engine would then have stumbled on the joiners is not assertable at
/// all and is the reason this errs the other way.
///
/// The word joiner is referenced as <c>(char)0x2060</c> rather than as a literal
/// or an in-string escape, for the reason
/// <see cref="Helpers.InstallerPathTextTests"/> gives.
/// </summary>
public class TranslateExtensionTests
{
    private const char Wj = (char)0x2060;

    // A key whose value names the installer folder in all sixteen languages, so
    // the assertions below hold whatever culture the test run resolves against.
    private const string SpokenPathKey = "Automation.RescanInstaller";
    private const string DrawnPathKey = "Body.MainExplanation.Action";

    /// <summary>
    /// The two services a markup extension is handed at parse time, of which
    /// this one reads only the target property. A null target models the Setter
    /// and template case, where the value is shared across instances and WPF
    /// supplies no target property.
    /// </summary>
    private sealed class ParseContext(object? targetProperty) : IServiceProvider, IProvideValueTarget
    {
        public object? TargetObject => null;
        public object? TargetProperty => targetProperty;
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IProvideValueTarget) ? this : null;
    }

    private static string Resolve(string key, object? targetProperty) =>
        (string)new TranslateExtension(key).ProvideValue(new ParseContext(targetProperty));

    [Fact]
    public void An_automation_name_carries_no_joiners()
    {
        Assert.DoesNotContain(Wj, Resolve(SpokenPathKey, AutomationProperties.NameProperty));
    }

    [Fact]
    public void An_automation_help_text_carries_no_joiners()
    {
        Assert.DoesNotContain(Wj, Resolve(SpokenPathKey, AutomationProperties.HelpTextProperty));
    }

    [Fact]
    public void Drawn_text_still_carries_the_joiners()
    {
        var drawn = Resolve(DrawnPathKey, TextBlock.TextProperty);

        Assert.Contains(Wj, drawn);
    }

    [Fact]
    public void The_spoken_value_is_the_drawn_one_less_its_joiners()
    {
        var drawn = Resolve(SpokenPathKey, TextBlock.TextProperty);
        var spoken = Resolve(SpokenPathKey, AutomationProperties.NameProperty);

        Assert.Contains(Wj, drawn);
        Assert.Equal(spoken, new string(drawn.Where(c => c != Wj).ToArray()));
    }

    [Fact]
    public void A_shared_value_with_no_target_property_keeps_the_joiners()
    {
        // A Setter or a template resolves once for every instance, so WPF hands
        // over no target property. That path is unchanged from before the split.
        Assert.Contains(Wj, Resolve(DrawnPathKey, targetProperty: null));
    }

    [Fact]
    public void A_string_naming_no_installer_folder_is_untouched_either_way()
    {
        Assert.Equal(
            Resolve("Automation.Minimise", TextBlock.TextProperty),
            Resolve("Automation.Minimise", AutomationProperties.NameProperty));
        Assert.DoesNotContain(Wj, Resolve("Automation.Minimise", TextBlock.TextProperty));
    }

    [Fact]
    public void A_missing_key_still_renders_as_itself()
    {
        // The documented fallback: a misspelled key shows on screen as the key
        // rather than as a blank, so the typo is visible. Asserted on both
        // branches because the resolve now happens before they diverge.
        Assert.Equal("Window.Main.Titel", Resolve("Window.Main.Titel", TextBlock.TextProperty));
        Assert.Equal("Window.Main.Titel", Resolve("Window.Main.Titel", AutomationProperties.NameProperty));
    }

    [Fact]
    public void An_empty_key_resolves_to_empty()
    {
        Assert.Equal(string.Empty,
            (string)new TranslateExtension().ProvideValue(new ParseContext(TextBlock.TextProperty)));
    }
}
