using System.Diagnostics;
using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.Services;

namespace InstallerClean;

public partial class AboutWindow : Window
{
    private readonly IUpdateCheckService _updateCheckService;

    public AboutWindow(IUpdateCheckService updateCheckService)
    {
        InitializeComponent();
        _updateCheckService = updateCheckService;
        VersionText.Text = DisplayHelpers.GetVersionString();
        this.EnableAltSpaceSystemMenu();
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private async void CheckNowClick(object sender, RoutedEventArgs e)
    {
        CheckNowButton.IsEnabled = false;
        CheckNowButton.Content = "Checking...";
        try
        {
            var result = await _updateCheckService.GetLatestVersionAsync();
            if (result.CheckFailed)
                MessageBox.Show(
                    "Couldn't check for updates. Make sure you're connected to the internet, then try again.",
                    "Check failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (result.LatestVersion is not null)
                MessageBox.Show(
                    $"{result.LatestVersion} is available.\n\ngithub.com/no-faff/InstallerClean/releases",
                    "Update available", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show(
                    "InstallerClean is up to date.",
                    "Up to date", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // Belt-and-braces: async void must not let an exception escape.
            MessageBox.Show(
                $"Couldn't check for updates: {ex.Message}",
                "Check failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            CheckNowButton.IsEnabled = true;
            CheckNowButton.Content = "Check for updates";
        }
    }

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Documents.Hyperlink link && link.NavigateUri is not null)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = link.NavigateUri.AbsoluteUri,
                UseShellExecute = true
            });
        }
    }
}
