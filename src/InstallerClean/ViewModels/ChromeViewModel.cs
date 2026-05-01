using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// Window-management slice. Holds the commands wired to the chrome
/// (About / Donate / Star / Close) and the two "open the details
/// window" commands that take the user from the main scan summary
/// into the registered- or orphaned-files detail windows.
///
/// Reads <see cref="ScanViewModel.LastScanResult"/> for the details
/// commands so the detail windows always show the same scan the main
/// window is currently summarising.
/// </summary>
public partial class ChromeViewModel : ObservableObject
{
    private readonly IWindowService _windowService;
    private readonly IMsiFileInfoService _msiInfoService;
    private readonly ScanViewModel _scan;

    public ChromeViewModel(
        IWindowService windowService,
        IMsiFileInfoService msiInfoService,
        ScanViewModel scan)
    {
        _windowService = windowService;
        _msiInfoService = msiInfoService;
        _scan = scan;
    }

    [RelayCommand]
    private void OpenOrphanedDetails()
    {
        if (_scan.LastScanResult is null) return;

        var viewModel = new OrphanedFilesViewModel(
            _scan.LastScanResult.RemovableFiles,
            _msiInfoService);

        _windowService.ShowOrphanedDetails(viewModel);
    }

    [RelayCommand]
    private void OpenRegisteredDetails()
    {
        if (_scan.LastScanResult is null) return;

        var viewModel = new RegisteredFilesViewModel(
            _scan.LastScanResult.RegisteredPackages,
            _scan.LastScanResult.RegisteredTotalBytes,
            _msiInfoService);

        _windowService.ShowRegisteredDetails(viewModel);
    }

    [RelayCommand]
    private void ShowAbout() => _windowService.ShowAbout();

    [RelayCommand]
    private void StarOnGitHub() => _windowService.OpenUrl("https://github.com/no-faff/InstallerClean");

    [RelayCommand]
    private void Donate() => _windowService.OpenUrl("https://nofaff.netlify.app");

    [RelayCommand]
    private void CloseApp() => _windowService.CloseMainWindow();
}
