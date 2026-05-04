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

    void CloseMainWindow();

    void OpenUrl(string url);
}
