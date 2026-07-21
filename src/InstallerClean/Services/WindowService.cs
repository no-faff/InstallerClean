using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.ViewModels;

namespace InstallerClean.Services;

public sealed class WindowService : IWindowService
{
    private readonly ISettingsService _settingsService;

    public WindowService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void ShowOrphanedDetails(OrphanedFilesViewModel viewModel)
    {
        if (Application.Current is null) return;
        var window = new OrphanedFilesWindow(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
    }

    public void ShowRegisteredDetails(RegisteredFilesViewModel viewModel)
    {
        if (Application.Current is null) return;
        var window = new RegisteredFilesWindow(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
    }

    public void ShowAbout()
    {
        if (Application.Current is null) return;
        var window = new AboutWindow(_settingsService)
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
    }

    public bool ShowUpdateAvailable(string currentVersion, string latestVersion)
    {
        if (Application.Current is null) return false;
        var dialog = new UpdateAvailableWindow(currentVersion, latestVersion)
        {
            Owner = Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true;
    }

    public void CloseMainWindow()
    {
        Application.Current?.MainWindow?.Close();
    }

    public void OpenUrl(string url) => UrlLauncher.OpenUrl(url);

    public void RelaunchForLanguageChange()
        => (Application.Current as App)?.RelaunchForLanguageChange();
}
