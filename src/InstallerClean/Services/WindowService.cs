using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.ViewModels;

namespace InstallerClean.Services;

public sealed class WindowService : IWindowService
{
    private readonly IUpdateCheckService _updateCheckService;
    private readonly ISettingsService _settingsService;

    public WindowService(IUpdateCheckService updateCheckService, ISettingsService settingsService)
    {
        _updateCheckService = updateCheckService;
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
        var window = new AboutWindow(_updateCheckService, _settingsService)
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
    }

    public void CloseMainWindow()
    {
        Application.Current?.MainWindow?.Close();
    }

    public void OpenUrl(string url) => UrlLauncher.OpenUrl(url);
}
