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

    /// <summary>
    /// The off-dispatcher write of the result-log lifetime lock, held so
    /// <see cref="Dispose"/> can wait for it. Clicking Send can be the last
    /// thing a user does before closing the window, and a write lost to the
    /// process exiting would re-prompt them next session on a machine that has
    /// already sent.
    /// </summary>
    private Task _lifetimeLockSave = Task.CompletedTask;

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
        IResultLogService resultLogService,
        IUpdateCheckService updateCheckService,
        IRemovableReverifier reverifier)
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
        // IsOperationInFlight, not IsOperating: the latter is unset through
        // both of Cleanup's pre-flights, which would leave Re-scan live while
        // a Move or a Delete was already under way.
        Scan = new ScanViewModel(scanService, rebootService, dialogService,
            isExternallyBlocked: () => Cleanup?.IsOperationInFlight == true || Completion?.IsComplete == true);
        Completion = new CompletionViewModel(
            rescanRequested: () => Scan.ScanCommand.ExecuteAsync(null),
            resultLogService: resultLogService,
            confirmationService: confirmationService,
            hasSentBefore: _hasSentResultLogBefore);
        Cleanup = new CleanupViewModel(
            moveService, deleteService, settingsService,
            dialogService, confirmationService, fileSystem,
            Scan, Completion, resultLogService, reverifier);
        Chrome = new ChromeViewModel(windowService, msiInfoService, settingsService,
            updateCheckService, dialogService, Scan,
            isBusy: () => IsBusy);

        // Surface the all-clear overlay when a scan finishes with no
        // orphans. Cleanup sets IsOperating=false after the post-
        // operation refresh fires ScanCompleted; that ordering keeps
        // an all-clear from overpainting a Move/Delete summary.
        _scanCompletedHandler = OnScanCompleted;
        Scan.ScanCompleted += _scanCompletedHandler;

        // Drive IsMainContentInteractive off the three overlay states.
        // The caption buttons themselves remain IsEnabled=true, but the
        // scanning and operating overlays span all three grid rows so
        // their dim Border absorbs clicks on the title bar. Esc is
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

        // Let the lifetime-lock write finish before the process goes. Bounded,
        // because a settings.json on a wedged network profile must not be able
        // to hold the app open on exit; the write is a few hundred bytes, so
        // the bound is only ever reached by a disk that has stopped answering,
        // and losing the lock then costs one extra prompt next session.
        try { _lifetimeLockSave.Wait(TimeSpan.FromSeconds(5)); }
        catch (Exception ex) { CrashLog.TryWrite(ex); }
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
    /// The single source of truth for "a scan or a destructive operation is in
    /// flight", owned here rather than inferred at each call site from the two
    /// execution flags (<see cref="ScanViewModel.IsScanInFlight"/> and
    /// <see cref="CleanupViewModel.IsOperationInFlight"/>). The language switch
    /// consults it to refuse a relaunch mid-operation, which the disabled bottom
    /// nav alone misses during a Move/Delete pre-flight (that runs before the
    /// operating overlay is shown). The command CanExecute predicates gate on the
    /// same two flags this unifies, and the window's close-hold uses the narrower
    /// <see cref="CleanupViewModel.IsOperating"/> deliberately: it is the
    /// finish-the-current-file flag, and a read-only scan needs no such hold.
    /// </summary>
    public bool IsBusy => Scan.IsScanInFlight || Cleanup.IsOperationInFlight;

    /// <summary>
    /// The main window's opening line. The intro is the only thing that tells the
    /// window's states apart:
    ///
    ///   - a scan FAILED (the startup scan, or a Re-scan): the tailored error,
    ///     with Re-scan focused, instead of exiting,
    ///   - nothing scanned yet (the startup scan was cancelled),
    ///   - a scan found files but a Windows Installer operation is in progress, so
    ///     Move and Delete are held: the copy explains the hold rather than telling
    ///     the user to press the dead buttons,
    ///   - a scan found files and they can be acted on (the state the window was
    ///     designed for),
    ///   - a scan found nothing (an all-clear, and the end state of every
    ///     successful clean-up).
    ///
    /// The failed state takes precedence over not-yet-scanned, because a failed
    /// scan leaves <see cref="ScanViewModel.HasScanned"/> false too but must say
    /// what went wrong rather than "nothing scanned yet". A completed scan gets
    /// the same lead at any count, zero included: "Any unneeded files below" is
    /// written to read correctly over an empty list, and the all-clear overlay
    /// has already announced the result in its own words by the time this
    /// window is read.
    /// </summary>
    public string IntroLead =>
        Scan.HasScanError ? Strings.Error_ScanFailedTitle
        : !Scan.HasScanned ? Strings.Body_NotScanned_Lead
        : Scan.HasOrphans && Scan.HasPendingReboot ? Strings.Body_PendingReboot_Lead
        : Strings.Body_MainExplanation_Lead;

    /// <summary>
    /// The muted second line under <see cref="IntroLead"/>. Carries the tailored
    /// message on a failed scan. Shown for every completed scan, zero files
    /// included: it is the app explaining what it does, and a clean machine is
    /// when a first-time user needs that most. Empty only on a pending-reboot
    /// hold (the banner below carries the specific reason), which collapses the
    /// TextBlock.
    /// </summary>
    public string IntroDetail =>
        Scan.HasScanError ? Scan.LastScanError
        : !Scan.HasScanned ? Strings.Body_NotScanned_Why
        : Scan.HasOrphans && Scan.HasPendingReboot ? string.Empty
        : MainExplanationWhyText;

    /// <summary>
    /// Whether to show the third intro tier, the line naming the two actions.
    /// True for every completed scan except during a Windows Installer
    /// hold: the buttons are held then, so an instruction to press them is
    /// removed (the pending-reboot banner and lead explain the hold). At zero
    /// files the line stays: "them" reads with the implied "if there are any".
    /// </summary>
    public bool ShowMainAction => Scan.HasScanned && !Scan.HasScanError && !Scan.HasPendingReboot;

    /// <summary>
    /// Whether the separator, the backup-folder box and the two action buttons
    /// are in the window. True for every completed scan, zero files included:
    /// at zero the buttons disable through their commands, and during a Windows
    /// Installer hold the zone stays while the banner explains the greyed
    /// buttons.
    /// </summary>
    public bool ShowActionZone => Scan.HasScanned && !Scan.HasScanError;

    /// <summary>
    /// "Scan cancelled." under the not-yet-scanned intro, so a user who
    /// cancelled the startup scan is told why the window is empty rather than
    /// being shown what looks like a clean machine. Empty (and collapsed) in
    /// every other state: once a scan has completed, its result is the answer.
    /// </summary>
    public string IntroNotice =>
        !Scan.HasScanned && Scan.LastScanWasCancelled ? Strings.Status_ScanCancelled : string.Empty;

    /// <summary>
    /// The middle ("why") sentence of the main-window intro, with the three
    /// Reason values formatted in. The intro is three resx keys
    /// (<c>Body.MainExplanation.Lead</c> / <c>.Why</c> / <c>.Action</c>) so each
    /// reads at its own text tier; only this one interpolates, so only it binds
    /// to the view-model. The <c>{0}</c>, <c>{1}</c> and <c>{2}</c> slots carry
    /// Reason.Orphaned, Reason.Superseded and Reason.Obsoleted so a translator
    /// edits the column labels in one place and the copy follows. Obsoleted
    /// (PatchState 4) is publisher-withdrawn rather than newer-patch-replaced;
    /// the copy distinguishes both.
    /// </summary>
    public string MainExplanationWhyText =>
        string.Format(Strings.Body_MainExplanation_Why,
            Strings.Reason_Orphaned, Strings.Reason_Superseded, Strings.Reason_Obsoleted);

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The three intro lines are computed from the scan's state, so they
        // re-read whenever any of the three inputs moves. HasOrphans covers
        // OrphanedFileCount, which raises it.
        if (e.PropertyName is nameof(ScanViewModel.HasScanned)
            or nameof(ScanViewModel.HasOrphans)
            or nameof(ScanViewModel.LastScanWasCancelled)
            or nameof(ScanViewModel.HasScanError)
            or nameof(ScanViewModel.HasPendingReboot))
        {
            OnPropertyChanged(nameof(IntroLead));
            OnPropertyChanged(nameof(IntroDetail));
            OnPropertyChanged(nameof(IntroNotice));
            OnPropertyChanged(nameof(ShowMainAction));
            OnPropertyChanged(nameof(ShowActionZone));
        }

        if (e.PropertyName == nameof(ScanViewModel.IsScanning) ||
            e.PropertyName == nameof(ScanViewModel.IsScanInFlight) ||
            e.PropertyName == nameof(CleanupViewModel.IsOperating) ||
            e.PropertyName == nameof(CleanupViewModel.IsOperationInFlight) ||
            e.PropertyName == nameof(CompletionViewModel.IsComplete))
        {
            OnPropertyChanged(nameof(IsMainContentInteractive));
            OnPropertyChanged(nameof(IsBusy));
            // Block F5 / Re-scan while a Move/Delete (its pre-flight included)
            // or a completion is up so a parallel scan can't race the operation.
            Scan.NotifyExternallyBlockedChanged();
        }
        else if (e.PropertyName == nameof(CompletionViewModel.HasSentResultLog) &&
                 Completion.HasSentResultLog)
        {
            // Persist the lifetime lock after a successful send. Off the
            // dispatcher, like the debounced destination save and for the same
            // reason: Update is a load-then-save round trip to settings.json,
            // which is a disk hop (OneDrive-redirected and network-roaming
            // profiles bite hardest), and it can additionally block on the
            // service's own gate behind that very save. This fires on a user
            // click, so the stall would land on a visible interaction.
            //
            // Update serialises against the debounced save under that gate, so
            // neither write clobbers the other, and it never throws (TrySave
            // returns false instead), so the task cannot fault unobserved.
            // Best-effort: a failed save just shows the prompt one extra time
            // next session, self-correcting on the next successful send.
            _lifetimeLockSave = Task.Run(() => _settingsService.Update(s => s.HasSentResultLog = true));
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
