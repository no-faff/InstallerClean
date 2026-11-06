using System.Windows;

namespace InstallerClean.Helpers;

/// <summary>
/// Gives a selectable box's copied text back without the invisible characters
/// <see cref="InstallerPathText"/> adds for line breaking. Set <c>IsEnabled</c> on
/// a TextBox and its Copy, by Ctrl+C, by the context menu or by a drag, carries
/// <see cref="InstallerPathText.ForCopying"/> of each piece of text it would have
/// carried. The <c>SelectableText</c> style sets it, so every box built on that
/// style copies plain text whatever it is given to show.
/// </summary>
public static class PlainCopy
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(PlainCopy),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        DataObject.RemoveCopyingHandler(element, OnCopying);
        if ((bool)e.NewValue)
            DataObject.AddCopyingHandler(element, OnCopying);
    }

    // The box puts the selection on the data object in each text format before it
    // raises Copying, and the data object replaces a format's data when it is set
    // again, so rewriting every format that holds a string is the whole of it. Each
    // is set back with conversion allowed, as the box sets it, so a reader asking
    // for another text format is still given one.
    private static void OnCopying(object sender, DataObjectCopyingEventArgs e)
    {
        var data = e.DataObject;
        foreach (var format in data.GetFormats(autoConvert: false))
        {
            if (data.GetData(format, autoConvert: false) is not string text) continue;
            var plain = InstallerPathText.ForCopying(text);
            if (!ReferenceEquals(plain, text))
                data.SetData(format, plain, autoConvert: true);
        }
    }
}
