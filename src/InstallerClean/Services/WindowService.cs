using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.ViewModels;

namespace InstallerClean.Services;

/// <summary>
/// Opens the windows the main window offers: the two Details windows, About and
/// the update prompt. Each is owned by the main window and shown with
/// <c>ShowDialog</c>, so none of them can be reached or used on its own.
///
/// That ownership is why each of them carries <c>ShowInTaskbar="False"</c> in its
/// own XAML, as does every Window in the app bar the main one. Without it a
/// window opened here would put a second InstallerClean button in the taskbar and
/// a second entry in Alt+Tab for as long as it was open, offering a switch to a
/// window that can only be reached through the one already there.
/// </summary>
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
