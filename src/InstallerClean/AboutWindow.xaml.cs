using System.Diagnostics;
using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean;

public partial class AboutWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IUpdateCheckService _updateCheckService;
    private readonly AppSettings _settings;

    public AboutWindow(ISettingsService settingsService, IUpdateCheckService updateCheckService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _updateCheckService = updateCheckService;
        _settings = settingsService.Load();
        VersionText.Text = DisplayHelpers.GetVersionString();
        CheckForUpdatesCheckBox.IsChecked = _settings.CheckForUpdates;
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void CheckForUpdatesChanged(object sender, RoutedEventArgs e)
    {
        _settings.CheckForUpdates = CheckForUpdatesCheckBox.IsChecked == true;
        try { _settingsService.Save(_settings); } catch { }
    }

    private async void CheckNowClick(object sender, RoutedEventArgs e)
    {
        CheckNowButton.IsEnabled = false;
        CheckNowButton.Content = "Checking...";
        try
        {
            var latest = await _updateCheckService.GetLatestVersionAsync();
            if (latest is not null)
                MessageBox.Show(
                    $"{latest} is available.\n\ngithub.com/no-faff/InstallerClean/releases",
                    "Update available", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show(
                    "InstallerClean is up to date.",
                    "Up to date", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally
        {
            CheckNowButton.IsEnabled = true;
            CheckNowButton.Content = "Check now";
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
