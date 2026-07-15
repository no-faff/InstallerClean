using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Helpers;
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
public partial class ChromeViewModel : ObservableObject, IDisposable
{
    private readonly IWindowService _windowService;
    private readonly IMsiFileInfoService _msiInfoService;
    private readonly ISettingsService _settings;
    private readonly ScanViewModel _scan;
    private readonly Func<bool> _isBusy;
    private readonly PropertyChangedEventHandler _scanHandler;

    public ChromeViewModel(
        IWindowService windowService,
        IMsiFileInfoService msiInfoService,
        ISettingsService settings,
        ScanViewModel scan,
        Func<bool>? isBusy = null)
    {
        _windowService = windowService;
        _msiInfoService = msiInfoService;
        _settings = settings;
        _scan = scan;
        // MainViewModel.IsBusy: a scan or a Move/Delete in flight. Null in a bare
        // test construction (never busy).
        _isBusy = isBusy ?? (() => false);

        // Re-evaluate the Details buttons when a scan finishes.
        // HasScanned is observable; LastScanResult is a plain auto-
        // property and won't raise PropertyChanged. Held as a field
        // so Dispose can unhook it; the singleton container disposes
        // this VM on shutdown.
        _scanHandler = OnScanPropertyChanged;
        _scan.PropertyChanged += _scanHandler;
    }

    private void OnScanPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScanViewModel.HasScanned))
        {
            OpenOrphanedDetailsCommand.NotifyCanExecuteChanged();
            OpenRegisteredDetailsCommand.NotifyCanExecuteChanged();
        }
        // A clean-up empties the orphan list without a fresh HasScanned raise
        // (the post-operation refresh only moves the counts), so the orphaned
        // Details button needs this one to grey out behind the completion
        // overlay rather than sit live over an empty list.
        else if (e.PropertyName == nameof(ScanViewModel.HasOrphans))
        {
            OpenOrphanedDetailsCommand.NotifyCanExecuteChanged();
        }
    }

    public void Dispose() => _scan.PropertyChanged -= _scanHandler;

    private bool HasScanResult => _scan.LastScanResult is not null;

    // The orphaned-files window lists what the scan found. With nothing found
    // there is nothing to list, and a live button onto an empty list is a dead
    // end; the registered window, which always has content after a scan, keeps
    // the plain has-a-result gate.
    private bool HasOrphansToShow => HasScanResult && _scan.HasOrphans;

    [RelayCommand(CanExecute = nameof(HasOrphansToShow))]
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
    private void Donate() => _windowService.OpenUrl("https://nofaff.netlify.app/support");

    [RelayCommand]
    private void CloseApp() => _windowService.CloseMainWindow();

    // Invoked by the bottom-bar language menu with a culture name
    // ("en-GB", "it"). Re-picking the language already on screen is a no-op,
    // so it does not pointlessly restart; the comparison is against the
    // DISPLAYED language (SupportedLanguages.Active), not the saved setting
    // (which this write changes) nor an explicit override alone (a default
    // install follows the OS with no override, yet still shows a language).
    // A real change is saved and applied by a relaunch, because the resx
    // strings resolve once when each window is built and do not re-read a
    // culture swapped at runtime.
    [RelayCommand]
    private void SetLanguage(string? culture)
    {
        if (string.Equals(culture, SupportedLanguages.Active(Localisation.UiCulture), StringComparison.OrdinalIgnoreCase))
            return;
        // Refuse a relaunch while a scan or a Move/Delete is in flight: the
        // relaunch tears the process down and the child cannot inherit the running
        // operation, so it would end exactly like closing the window mid-move. The
        // bottom-nav globe is already disabled while the scanning or operating
        // overlay is up, but a Move/Delete pre-flight runs with the overlay not yet
        // shown, so this is the belt that covers that window.
        if (_isBusy())
            return;
        _settings.Update(s => s.Language = culture);
        _windowService.RelaunchForLanguageChange();
    }
}
