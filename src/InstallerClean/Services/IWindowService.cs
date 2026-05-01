using InstallerClean.ViewModels;

namespace InstallerClean.Services;

/// <summary>
/// Opens the secondary windows (orphaned details, registered details, about)
/// and closes the main window. Behind an interface so MainViewModel commands
/// don't take a direct dependency on Application.Current.MainWindow or
/// new up Window types, both of which would NRE under xUnit.
/// </summary>
public interface IWindowService
{
    void ShowOrphanedDetails(OrphanedFilesViewModel viewModel);

    void ShowRegisteredDetails(RegisteredFilesViewModel viewModel);

    void ShowAbout();

    void CloseMainWindow();

    void OpenUrl(string url);
}
