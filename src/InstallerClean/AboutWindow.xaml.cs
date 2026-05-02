using System.Windows;
using InstallerClean.Helpers;

namespace InstallerClean;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = DisplayHelpers.GetVersionString();
        this.EnableAltSpaceSystemMenu();
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void CheckNowClick(object sender, RoutedEventArgs e) =>
        UnelevatedLauncher.OpenUrl("https://github.com/no-faff/InstallerClean/releases");

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Documents.Hyperlink link && link.NavigateUri is not null)
            UnelevatedLauncher.OpenUrl(link.NavigateUri.AbsoluteUri);
    }

    private void StarClick(object sender, RoutedEventArgs e) =>
        UnelevatedLauncher.OpenUrl("https://github.com/no-faff/InstallerClean");

    private void DonateClick(object sender, RoutedEventArgs e) =>
        UnelevatedLauncher.OpenUrl("https://nofaff.netlify.app");
}
