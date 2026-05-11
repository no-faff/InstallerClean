using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean;

public partial class UpdateAvailableWindow : Window
{
    public UpdateAvailableWindow(string currentVersion, string latestVersion)
    {
        InitializeComponent();
        VersionInfo.Text = string.Format(
            Strings.UpdateCheck_UpdateAvailable_Body,
            currentVersion, latestVersion);
        this.EnableAltSpaceSystemMenu();
    }

    private void OnOpen(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
