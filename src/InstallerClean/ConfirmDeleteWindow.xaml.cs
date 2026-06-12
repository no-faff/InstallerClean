using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean;

public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow(int fileCount, string sizeDisplay)
    {
        InitializeComponent();
        var label = DisplayHelpers.PluraliseFile(fileCount);
        MessageText.Text = string.Format(Strings.Confirm_DeleteTitle, fileCount, label, sizeDisplay);
        // The window title is what a screen reader announces when a
        // dialog opens; the static "Confirm delete" left the question
        // itself, the count and size, unspoken. ShowInTaskbar is false,
        // so the title serves announcements only.
        Title = MessageText.Text;

        // Sized to content; the clamp stops a very large text scale
        // pushing the card past the work area, at which point the body
        // row scrolls and the action buttons stay visible.
        MaxHeight = DetailWindowSizing.WorkAreaHeightLimit(Application.Current?.MainWindow);

        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
        // Open with focus on Cancel (IsDefault/IsCancel, the safe
        // default) so a keyboard user gets a visible focus ring at once
        // and a reflexive Space cannot delete. Deferred to Loaded so the
        // visual tree exists when Focus runs.
        Loaded += (_, _) => CancelButton.Focus();
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
