using System.Diagnostics;
using System.Windows;
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
        var window = new AboutWindow
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
    }

    public void CloseMainWindow()
    {
        Application.Current?.MainWindow?.Close();
    }

    public void OpenUrl(string url)
    {
        // Swallow: a misconfigured URL handler is a common cause of
        // Process.Start throw, and the user clicked a non-essential
        // button (Donate / Star / Check for updates). Tearing the app
        // down to surface "no browser" is worse than the silent fail.
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Helpers.CrashLog.Write(ex);
        }
    }
}
