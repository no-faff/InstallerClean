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
    private readonly PropertyChangedEventHandler _scanHandler;

    public ChromeViewModel(
        IWindowService windowService,
        IMsiFileInfoService msiInfoService,
        ISettingsService settings,
        ScanViewModel scan)
    {
        _windowService = windowService;
        _msiInfoService = msiInfoService;
        _settings = settings;
        _scan = scan;

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
    }

    public void Dispose() => _scan.PropertyChanged -= _scanHandler;

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
        _settings.Update(s => s.Language = culture);
        _windowService.RelaunchForLanguageChange();
    }
}
