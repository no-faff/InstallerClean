using System.Windows;
using System.Windows.Controls;

namespace InstallerClean.Helpers;

/// <summary>
/// Gives a selectable box's copied text back without the invisible characters
/// <see cref="InstallerPathText"/> adds for line breaking. Set <c>Untreated</c> on a
/// TextBox to the text before it was treated, and give the box the treated text to
/// show: its Copy, by Ctrl+C, by the context menu or by a drag, then carries
/// <see cref="InstallerPathText.ForCopying"/> of the selection. A word joiner or a
/// zero-width space that was in the untreated text, as one can be in a folder's name,
/// is copied with the rest. A box showing text no treatment has touched sets nothing,
/// and copies what it shows.
/// </summary>
public static class PlainCopy
{
    public static readonly DependencyProperty UntreatedProperty =
        DependencyProperty.RegisterAttached(
            "Untreated", typeof(string), typeof(PlainCopy),
            new PropertyMetadata(null, OnUntreatedChanged));

    public static string? GetUntreated(DependencyObject element) =>
        (string?)element.GetValue(UntreatedProperty);

    public static void SetUntreated(DependencyObject element, string? value) =>
        element.SetValue(UntreatedProperty, value);

    private static void OnUntreatedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        DataObject.RemoveCopyingHandler(element, OnCopying);
        if (e.NewValue is not null)
            DataObject.AddCopyingHandler(element, OnCopying);
    }

    // The box puts the selection on the data object in each text format before it
    // raises Copying, and the data object replaces a format's data when it is set
    // again, so rewriting every format that holds the selection is the whole of it.
    // Each is set back with conversion allowed, as the box sets it, so a reader asking
    // for another text format is still given one. A format holding anything other than
    // the box's selection is left as it is. Where the box's text is not its untreated text
    // with only those two characters added, the copy is the selection with every one of
    // them taken out.
    private static void OnCopying(object sender, DataObjectCopyingEventArgs e)
    {
        if (sender is not TextBox box || GetUntreated(box) is not { } untreated) return;

        var selected = box.SelectedText;
        var plain = InstallerPathText.ForCopying(box.Text, untreated, box.SelectionStart, box.SelectionLength)
            ?? InstallerPathText.WithoutAddedCharacters(selected);
        var data = e.DataObject;
        foreach (var format in data.GetFormats(autoConvert: false))
        {
            if (data.GetData(format, autoConvert: false) is string text
                && string.Equals(text, selected, StringComparison.Ordinal)
                && !string.Equals(text, plain, StringComparison.Ordinal))
                data.SetData(format, plain, autoConvert: true);
        }
    }
}
