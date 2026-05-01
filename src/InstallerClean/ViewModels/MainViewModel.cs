using CommunityToolkit.Mvvm.ComponentModel;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// Composition root for the main window's view-model graph. Holds the
/// four child view-models (Scan / Cleanup / Completion / Chrome) as
/// public properties for XAML binding, and wires the inter-VM signals
/// that coordinate them:
///
///   - When a scan completes with no orphans, push the "all clear"
///     completion overlay.
///   - When the user clicks the "Scan again" button on the completion
///     overlay, fire the scan VM's Scan command.
///
/// All scan/cleanup/completion/chrome state lives on the child VMs.
/// XAML binds via the corresponding nested property
/// (<c>{Binding Scan.IsScanning}</c>, <c>{Binding Cleanup.MoveDestination}</c>,
/// etc).
/// </summary>
public partial class MainViewModel : ObservableObject
{
    public ScanViewModel Scan { get; }
    public CleanupViewModel Cleanup { get; }
    public CompletionViewModel Completion { get; }
    public ChromeViewModel Chrome { get; }

    public MainViewModel(
        IFileSystemScanService scanService,
        IMoveFilesService moveService,
        IDeleteFilesService deleteService,
        ISettingsService settingsService,
        IPendingRebootService rebootService,
        IMsiFileInfoService msiInfoService,
        IDialogService dialogService,
        IConfirmationService confirmationService,
        IWindowService windowService)
    {
        Scan = new ScanViewModel(scanService, rebootService, dialogService);
        Completion = new CompletionViewModel();
        Cleanup = new CleanupViewModel(
            moveService, deleteService, settingsService,
            dialogService, confirmationService,
            Scan, Completion);
        Chrome = new ChromeViewModel(windowService, msiInfoService, Scan);

        // After every successful scan, if there are no orphans and no
        // operation is in flight, surface the all-clear completion
        // overlay. The IsOperating guard prevents the all-clear from
        // hiding a freshly-painted Move/Delete summary when the
        // post-operation refresh runs.
        Scan.ScanCompleted += (_, _) =>
        {
            if (Scan.OrphanedFileCount == 0 && !Cleanup.IsOperating)
                Completion.ShowAllClear();
        };

        // Completion's "Scan again" button doesn't know about the scan
        // service; route the request through to the scan VM's command
        // and propagate its task so callers (notably tests) can await
        // the resulting scan.
        Completion.RescanRequested = () => Scan.ScanCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Forwarded to <see cref="ScanViewModel.ScanWithProgressAsync"/>
    /// so App.xaml.cs can keep its existing splash-driven startup
    /// scan call site without reaching into the child VM directly.
    /// </summary>
    public Task ScanWithProgressAsync(IProgress<string>? progress, CancellationToken cancellationToken = default) =>
        Scan.ScanWithProgressAsync(progress, cancellationToken);
}
