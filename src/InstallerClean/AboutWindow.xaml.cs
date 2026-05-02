using System.Diagnostics;
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
        OpenUrl("https://github.com/no-faff/InstallerClean/releases");

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Documents.Hyperlink link && link.NavigateUri is not null)
            OpenUrl(link.NavigateUri.AbsoluteUri);
    }

    private void StarClick(object sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/no-faff/InstallerClean");

    private void DonateClick(object sender, RoutedEventArgs e) =>
        OpenUrl("https://nofaff.netlify.app");

    private static void OpenUrl(string url)
    {
        // Misconfigured URL handler (rare but observed) would otherwise
        // bubble out of a click handler and crash the app. The button
        // is non-essential; silent fail with crash log is the right
        // tradeoff.
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
        }
    }
}
