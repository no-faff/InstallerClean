using System.Windows;
using InstallerClean.Helpers;

namespace InstallerClean;

public partial class ConfirmSendResultLogWindow : Window
{
    public ConfirmSendResultLogWindow(string jsonContent)
    {
        InitializeComponent();
        JsonText.Text = jsonContent;
        this.EnableAltSpaceSystemMenu();
        this.ClearFocusOnDeactivation();
    }

    private void OnSend(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
