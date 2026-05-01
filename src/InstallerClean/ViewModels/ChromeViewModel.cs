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
/// window is currently summarising. The details commands' CanExecute
/// reflects whether a scan has completed at all, so the buttons are
/// disabled (greyed out via the standard pill IsEnabled trigger) until
/// the user has data to view.
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

        // Surface scan-complete signals to the details commands so the
        // Details buttons enable as soon as the first scan finishes.
        // We listen on HasScanned (an observable property) rather than
        // LastScanResult (a plain auto-property that never raises
        // PropertyChanged), so HasScanned is the single trigger.
        //
        // LIFETIME CONTRACT: this subscription is intentionally never
        // unhooked. Both VMs are constructed by MainViewModel and share
        // its lifetime; MainViewModel is a singleton resolved exactly
        // once via Composition.cs and dies with the process. If a
        // future test or feature ever creates throwaway MainViewModel
        // instances around a longer-lived ScanViewModel (for example
        // by hoisting Scan into a separate DI singleton), convert this
        // to a named handler stored on a field and detach it in an
        // IDisposable.Dispose. The handler does not capture mutable
        // state, only `this`. Mirrors the same contract on
        // CleanupViewModel.
        _scan.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ScanViewModel.HasScanned))
            {
                OpenOrphanedDetailsCommand.NotifyCanExecuteChanged();
                OpenRegisteredDetailsCommand.NotifyCanExecuteChanged();
            }
        };
    }

    private bool HasScanResult => _scan.LastScanResult is not null;

    [RelayCommand(CanExecute = nameof(HasScanResult))]
    private void OpenOrphanedDetails()
    {
        if (_scan.LastScanResult is null) return;

        var viewModel = new OrphanedFilesViewModel(
            _scan.LastScanResult.RemovableFiles,
            _msiInfoService);

        _windowService.ShowOrphanedDetails(viewModel);
    }

    [RelayCommand(CanExecute = nameof(HasScanResult))]
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
