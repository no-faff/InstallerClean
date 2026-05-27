using System.ComponentModel;
using System.IO.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// Composition root for the main window's view-model graph. Holds the
/// four child view-models (Scan / Cleanup / Completion / Chrome) as
/// public properties for XAML binding, and wires the inter-VM signals
/// that coordinate them:
///
///   - A scan completing with no orphans pushes the "all clear"
///     completion overlay.
///   - The "Scan again" command on the completion overlay invokes
///     the Scan VM's Scan command via the rescan delegate.
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
    private readonly IResultLogService _resultLogService;
    private readonly ISettingsService _settingsService;
    private readonly bool _hasSentResultLogBefore;

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
        IFileSystem fileSystem,
        IResultLogService resultLogService)
    {
        _resultLogService = resultLogService;
        _settingsService = settingsService;
        // Snapshot the lifetime lock once at construction. The settings
        // service is also read inside CleanupViewModel for MoveDestination,
        // but those two reads can't race: this snapshot covers HasSentResultLog
        // for the whole MainViewModel lifetime, and writes go through the
        // persistence callback below.
        _hasSentResultLogBefore = settingsService.Load().HasSentResultLog;

        // Closures read Cleanup / Completion at invocation time, after
        // the ctor runs.
        Scan = new ScanViewModel(scanService, rebootService, dialogService,
            isExternallyBlocked: () => Cleanup?.IsOperating == true || Completion?.IsComplete == true);
        Completion = new CompletionViewModel(
            rescanRequested: () => Scan.ScanCommand.ExecuteAsync(null),
            resultLogService: resultLogService,
            confirmationService: confirmationService,
            hasSentBefore: _hasSentResultLogBefore);
        Cleanup = new CleanupViewModel(
            moveService, deleteService, settingsService,
            dialogService, confirmationService, fileSystem,
            Scan, Completion, resultLogService);
        Chrome = new ChromeViewModel(windowService, msiInfoService, Scan);

        // Surface the all-clear overlay when a scan finishes with no
        // orphans. Cleanup sets IsOperating=false after the post-
        // operation refresh fires ScanCompleted; that ordering keeps
        // an all-clear from overpainting a Move/Delete summary.
        _scanCompletedHandler = OnScanCompleted;
        Scan.ScanCompleted += _scanCompletedHandler;

        // Drive IsMainContentInteractive off the three overlay states.
        // The caption buttons themselves remain IsEnabled=true, but the
        // scanning and operating overlays span all four grid rows so
        // their dim Rectangle absorbs clicks on the title bar. Esc is
        // wired through MainWindow.OnPreviewKeyDown for each overlay,
        // and Alt+F4 reaches the window's normal SC_CLOSE path through
        // WM_SYSCOMMAND (only SC_MAXIMIZE is intercepted).
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

    /// <summary>
    /// Body explanation paragraph with the three Reason values formatted
    /// in. The resx template carries <c>{0}</c>, <c>{1}</c> and <c>{2}</c>
    /// for Reason.Orphaned, Reason.Superseded and Reason.Obsoleted so a
    /// translator can edit the column labels in one place and have the
    /// body copy follow. Obsoleted (PatchState 4) is publisher-withdrawn
    /// rather than newer-patch-replaced; the body distinguishes both.
    /// </summary>
    public string MainExplanationText =>
        string.Format(Strings.Body_MainExplanation,
            Strings.Reason_Orphaned, Strings.Reason_Superseded, Strings.Reason_Obsoleted);

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
        else if (e.PropertyName == nameof(CompletionViewModel.HasSentResultLog) &&
                 Completion.HasSentResultLog)
        {
            // Persist the lifetime lock immediately after a successful
            // send. TrySave is best-effort; a failed save means the
            // user might see the prompt one extra time on a future
            // session, which is harmless and self-correcting on the
            // next successful save.
            var snapshot = _settingsService.Load();
            snapshot.HasSentResultLog = true;
            _settingsService.TrySave(snapshot);
        }
    }

    private async void OnScanCompleted(object? sender, EventArgs e)
    {
        // async void: WriteAsync documents never-throws, but the
        // contract sits across an assembly boundary. The outer
        // try/catch keeps any breach of that contract from riding
        // DispatcherUnhandledException to a process exit.
        try
        {
            // The suppression flag is consumed up front so a rescan
            // that returns orphans (rather than another all-clear)
            // still resets the one-shot for the next MarkResultLogReady
            // call.
            var suppress = Completion.ConsumeSuppressNextResultLogPrompt();

            if (Scan.OrphanedFileCount != 0 || Cleanup.IsOperating || Scan.LastScanResult is not { } result)
                return;

            Completion.ShowAllClear(result.RegisteredPackages.Count, Scan.LastScanDurationMs);

            if (suppress) return;
            // Either lock (the prior-session persisted flag or the
            // in-session click flag) hides the Send button for the
            // rest of the user's time on this machine. Writing
            // last-run.json on this path produces a file with no
            // consumer. CleanupViewModel's Move and Delete paths read
            // the same property.
            if (Completion.IsResultLogLocked) return;

            // WriteAsync returns false on disk-full / locked-file /
            // read-only-profile failure; the Send button stays hidden
            // rather than overpainting a dialog on the all-clear summary.
            //
            // ConfigureAwait(false) drops the I/O continuation off the
            // dispatcher so a window-close mid-await doesn't throw on
            // resumption. MarkResultLogReady() fires PropertyChanged
            // for IsResultLogReady and IsSendResultLogVisible; binding
            // subscribers (Send button Visibility, CanExecuteChanged
            // routed through CommandManager.InvalidateRequerySuggested)
            // are dispatcher-affined. Marshal that single field-write
            // back to the dispatcher so the property change actually
            // updates the UI; without the marshal the cross-thread
            // PropertyChanged throws InvalidOperationException, the
            // outer catch swallows it, and the Send button silently
            // never appears on a happy-path startup-scan.
            var entry = ResultLogEntry.ForScanOnly(
                result, Scan.LastScanDurationMs, Scan.PendingRebootLabel);
            var written = await _resultLogService.WriteAsync(entry).ConfigureAwait(false);
            if (written)
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher is { HasShutdownStarted: false })
                    await dispatcher.InvokeAsync(Completion.MarkResultLogReady);
                else
                    // No live dispatcher: either the window has closed (the
                    // outer catch will absorb any binding-side InvalidOperation
                    // Exception), or this is a unit test with Application.Current
                    // null. In both cases a direct call is the right behaviour:
                    // production fails closed, tests observe the property change.
                    Completion.MarkResultLogReady();
            }
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
        }
    }
}
