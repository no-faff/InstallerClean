using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean;

public partial class ConfirmMoveWindow : Window
{
    public ConfirmMoveWindow(int fileCount, string sizeDisplay, string destination)
    {
        InitializeComponent();
        var label = DisplayHelpers.PluraliseFile(fileCount);
        MessageText.Text = string.Format(Strings.Confirm_MoveTitle, fileCount, label, sizeDisplay);
        DestinationText.Text = string.Format(Strings.Confirm_MoveDestination, destination);
        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
        // Open with focus on Cancel (IsDefault/IsCancel) so a keyboard
        // user gets a visible focus ring at once, mirroring the Confirm
        // Delete dialog. Deferred to Loaded so the visual tree exists
        // when Focus runs.
        Loaded += (_, _) => CancelButton.Focus();
    }

    private void OnMove(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
