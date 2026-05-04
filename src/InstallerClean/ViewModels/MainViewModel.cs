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
public partial class MainViewModel : ObservableObject, IDisposable
{
    public ScanViewModel Scan { get; }
    public CleanupViewModel Cleanup { get; }
    public CompletionViewModel Completion { get; }
    public ChromeViewModel Chrome { get; }

    private readonly EventHandler _scanCompletedHandler;

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
        // Closures read Cleanup / Completion at invocation time, after
        // the ctor runs.
        Scan = new ScanViewModel(scanService, rebootService, dialogService,
            isExternallyBlocked: () => Cleanup?.IsOperating == true || Completion?.IsComplete == true);
        Completion = new CompletionViewModel(() => Scan.ScanCommand.ExecuteAsync(null));
        Cleanup = new CleanupViewModel(
            moveService, deleteService, settingsService,
            dialogService, confirmationService, fileSystem,
            Scan, Completion);
        Chrome = new ChromeViewModel(windowService, msiInfoService, Scan);

        // Surface the all-clear overlay when a scan finishes with no
        // orphans. The IsOperating guard depends on Cleanup setting
        // IsOperating=false AFTER the post-operation refresh fires
        // ScanCompleted; reordering that flow would let an all-clear
        // overpaint a Move/Delete summary.
        _scanCompletedHandler = (_, _) =>
        {
            if (Scan.OrphanedFileCount == 0 && !Cleanup.IsOperating)
                Completion.ShowAllClear();
        };
        Scan.ScanCompleted += _scanCompletedHandler;

        // Drive IsMainContentInteractive off the three overlay states.
        // Caption buttons stay enabled regardless: the user must always
        // be able to close the window.
        Scan.PropertyChanged += OnChildPropertyChanged;
        Cleanup.PropertyChanged += OnChildPropertyChanged;
        Completion.PropertyChanged += OnChildPropertyChanged;
    }

    public void Dispose()
    {
        Scan.PropertyChanged -= OnChildPropertyChanged;
        Cleanup.PropertyChanged -= OnChildPropertyChanged;
        Completion.PropertyChanged -= OnChildPropertyChanged;
        Scan.ScanCompleted -= _scanCompletedHandler;
        Chrome.Dispose();
        Cleanup.Dispose();
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
            // Block F5 / Re-scan while a Move/Delete or completion is
            // up so a parallel scan can't race the operation.
            Scan.NotifyExternallyBlockedChanged();
        }
    }
}
