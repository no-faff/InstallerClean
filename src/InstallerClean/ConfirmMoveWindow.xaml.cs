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
        this.ClearFocusOnDeactivation();
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
