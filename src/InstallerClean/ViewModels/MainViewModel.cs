using System.ComponentModel;
using System.IO.Abstractions;
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
        IWindowService windowService,
        IFileSystem fileSystem)
    {
        Scan = new ScanViewModel(scanService, rebootService, dialogService);
        Completion = new CompletionViewModel();
        Cleanup = new CleanupViewModel(
            moveService, deleteService, settingsService,
            dialogService, confirmationService, fileSystem,
            Scan, Completion);
        Chrome = new ChromeViewModel(windowService, msiInfoService, Scan);

        // After every successful scan, if there are no orphans and no
        // operation is in flight, surface the all-clear completion
        // overlay. The IsOperating guard prevents the all-clear from
        // hiding a freshly-painted Move/Delete summary when the
        // post-operation refresh runs.
        //
        // TIMING DEPENDENCY: this guard relies on Cleanup setting
        // IsOperating=false in its finally block AFTER the post-
        // operation RefreshAsync raises ScanCompleted. If a future
        // change reorders Cleanup's finally so IsOperating clears
        // BEFORE RefreshAsync runs, this handler would fire while
        // IsOperating is already false, painting all-clear over the
        // completion summary. Don't move IsOperating=false above the
        // refresh await without rethinking this guard.
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

        // IsMainContentInteractive is a derived bool the main-window XAML
        // binds to IsEnabled on the body content + bottom nav so neither
        // mouse nor keyboard can reach those buttons while any of the
        // three full-window overlays (scanning, operating, completion)
        // is up. Caption buttons (Min / Max / Close) stay enabled
        // because the user must always be able to close the window.
        Scan.PropertyChanged += OnChildPropertyChanged;
        Cleanup.PropertyChanged += OnChildPropertyChanged;
        Completion.PropertyChanged += OnChildPropertyChanged;
    }

    /// <summary>
    /// True iff none of the three overlays (scanning, operating,
    /// completion) is showing. Bound to the main-window body's
    /// IsEnabled so an active overlay disables Tab/click on every
    /// control behind it.
    /// </summary>
    public bool IsMainContentInteractive =>
        !Scan.IsScanning && !Cleanup.IsOperating && !Completion.IsComplete;

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScanViewModel.IsScanning) ||
            e.PropertyName == nameof(CleanupViewModel.IsOperating) ||
            e.PropertyName == nameof(CompletionViewModel.IsComplete))
        {
            OnPropertyChanged(nameof(IsMainContentInteractive));

            // While a Move/Delete or a completion overlay is up, block
            // the user-driven Scan command (the F5 keybinding and any
            // other Re-scan / Scan-again click). Without this, F5
            // pressed during a Move starts a parallel scan that races
            // the in-flight file operation, and the Scanning overlay
            // paints over the Operating overlay.
            Scan.IsExternallyBlocked = Cleanup.IsOperating || Completion.IsComplete;
        }
    }
}
