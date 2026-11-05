using System;
using System.Windows.Markup;
using InstallerClean.Helpers;

namespace InstallerClean.Resources;

/// <summary>
/// <see cref="TranslateExtension"/> with the access key taken out of the result,
/// used as <c>{loc:TranslateWithoutAccessKey Action.DeletePermanently}</c>.
/// </summary>
/// <remarks>
/// The two confirmations' Delete permanently and Move buttons take this. They show
/// the same label as the main window's buttons, and carry no key, so each is reached
/// only by the pointer or by Tab, and the letter that opened the confirmation from
/// the main window does nothing in it. WPF fires an access key on the bare letter,
/// with no Alt, wherever the focused control takes no typing, and that includes
/// Cancel, where the confirmation opens.
///
/// One string, read through the same door as every other label, so a rewording of
/// the main window's button reaches the confirmation by itself and the spoken help
/// naming the button goes on naming it. scripts/check-accelerators.mjs lists every
/// site that uses this.
/// </remarks>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TranslateWithoutAccessKeyExtension : MarkupExtension
{
    public TranslateWithoutAccessKeyExtension() { }

    public TranslateWithoutAccessKeyExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string? Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        AccessKeyText.Remove((string)new TranslateExtension(Key!).ProvideValue(serviceProvider));
}
