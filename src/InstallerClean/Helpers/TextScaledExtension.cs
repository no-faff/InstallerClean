using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace InstallerClean.Helpers;

/// <summary>
/// A layout length that tracks the OS text-scale factor:
/// <c>{a11y:TextScaled 400}</c> yields 400 at 100% text size and
/// 400 multiplied by the factor thereafter, updating live when the
/// slider moves. Font sizes scale through the Type.* tokens; this
/// covers the lengths that bound scaled text (card widths, list
/// column widths, text MaxWidths), which would otherwise wrap or
/// clip ever tighter as the text grows while everything around it
/// stays the same size.
/// </summary>
/// <remarks>
/// Provides a one-way binding with an explicit source, so it works on
/// any dependency property of type <see cref="double"/>, including
/// targets outside the visual tree such as
/// <see cref="System.Windows.Controls.GridViewColumn.Width"/>.
/// </remarks>
[MarkupExtensionReturnType(typeof(double))]
public sealed class TextScaledExtension : MarkupExtension
{
    private static readonly ScaleConverter Converter = new();

    /// <summary>The length at 100% text scale.</summary>
    public double Base { get; set; }

    public TextScaledExtension(double @base) => Base = @base;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding(nameof(AccessibilitySettings.TextScaleFactor))
        {
            Source = AccessibilitySettings.Current,
            Mode = BindingMode.OneWay,
            Converter = Converter,
            ConverterParameter = Base,
        }.ProvideValue(serviceProvider);

    private sealed class ScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (double)value * (double)parameter;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
