using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.ViewModels;

namespace InstallerClean.Services;

public sealed class WindowService : IWindowService
{
    private readonly ISettingsService _settingsService;
    private readonly IUpdateCheckService _updateCheckService;
    private readonly IResultLogService _resultLogService;

    public WindowService(
        ISettingsService settingsService,
        IUpdateCheckService updateCheckService,
        IResultLogService resultLogService)
    {
        _settingsService = settingsService;
        _updateCheckService = updateCheckService;
        _resultLogService = resultLogService;
    }

    public void ShowOrphanedDetails(OrphanedFilesViewModel viewModel)
    {
        if (Application.Current is null) return;
        var window = new OrphanedFilesWindow(viewModel, _settingsService)
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
    }

    public void ShowRegisteredDetails(RegisteredFilesViewModel viewModel)
    {
        if (Application.Current is null) return;
        var window = new RegisteredFilesWindow(viewModel, _settingsService)
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
    }

    public void ShowAbout()
    {
        if (Application.Current is null) return;
        var window = new AboutWindow(_updateCheckService, _resultLogService)
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
