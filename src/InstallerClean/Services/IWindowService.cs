using InstallerClean.ViewModels;

namespace InstallerClean.Services;

/// <summary>
/// Opens the secondary windows (orphaned details, registered details,
/// about) and closes the main window. The interface keeps MainViewModel
/// free of a direct dependency on Application.Current.MainWindow and on
/// Window constructors, both of which NRE under xUnit.
/// </summary>
public interface IWindowService
{
    void ShowOrphanedDetails(OrphanedFilesViewModel viewModel);

    void ShowRegisteredDetails(RegisteredFilesViewModel viewModel);

    void ShowAbout();

    /// <summary>
    /// Shows the update-available dialog for a check that found a newer
    /// version. Returns true when the user chose to open the release
    /// page; the caller launches the URL, keeping the window layer free
    /// of the launcher and the view-model free of Window construction.
    /// </summary>
    bool ShowUpdateAvailable(string currentVersion, string latestVersion);

    void CloseMainWindow();

    void OpenUrl(string url);

    /// <summary>
    /// Relaunches the app so a newly chosen display-language preference
    /// takes effect. The resx strings resolve once when each window is
    /// built, so a culture swapped at runtime does not reach text already
    /// painted; a fresh process is the reliable way to apply it.
    /// </summary>
    void RelaunchForLanguageChange();
}
