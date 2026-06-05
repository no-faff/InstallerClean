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
        this.SuppressFocusVisualOnDeactivation();
        // Open with focus on Cancel (IsCancel, the conservative default) so a
        // keyboard user gets a visible focus ring at once rather than focus on
        // the window itself. Deferred to Loaded so the visual tree exists when
        // Focus runs. Mirrors RecycleUnavailableWindow.
        Loaded += (_, _) => CancelButton.Focus();
    }

    private void OnOpen(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
