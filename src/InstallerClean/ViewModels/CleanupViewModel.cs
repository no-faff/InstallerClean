using System.ComponentModel;
using System.IO.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// Cleanup slice: the move-destination input plus the Move and Delete
/// commands and their progress overlay. Reads orphans from
/// <see cref="ScanViewModel.LastScanResult"/>, pushes outcomes into
/// <see cref="CompletionViewModel"/>, and asks the scan VM to refresh once
/// the worker has returned, however the batch ended.
/// </summary>
public partial class CleanupViewModel : ObservableObject, IDisposable
{
    private readonly IMoveFilesService _moveService;
    private readonly IDeleteFilesService _deleteService;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly IConfirmationService _confirmationService;
    private readonly IFileSystem _fs;
    private readonly ScanViewModel _scan;
    private readonly CompletionViewModel _completion;
    private readonly IResultLogService _resultLogService;
    private readonly IRemovableReverifier _reverifier;
    private readonly PropertyChangedEventHandler _scanHandler;

    /// <summary>
    /// How this view model asks whether a folder is on the volume the installer
    /// cache is on. <see cref="MoveSpaceCheck.ResolveIsOnInstallerCacheDrive"/>
    /// unless a caller hands over something else, which nothing in the app does.
    /// </summary>
    /// <remarks>
    /// ONE FIELD BECAUSE THERE IS ONE QUESTION. The class asks it twice, from
    /// <see cref="ResolveDestinationVolumeAsync"/> for the tooltip and from
    /// <see cref="ClassifyMoveDestination"/> for the result-log label, and two
    /// routes to one answer are two things to keep in step.
    ///
    /// It answers about a real volume, so what a folder is on cannot be arranged
    /// from a test: that needs a second disk, and the timing needs an answer that
    /// arrives when the test says rather than when Windows is ready. Handing the
    /// resolve in is what settles both. It changes where the answer comes from
    /// and nothing about what is done with it: the same debounce, the same hop
    /// off the dispatcher and the same check that the answer is still wanted.
    /// </remarks>
    private readonly Func<string, bool?> _resolveIsOnCacheVolume;

    private CancellationTokenSource? _operationCts;
    private CancellationTokenSource? _moveDestinationSaveCts;
    private CancellationTokenSource? _destinationVolumeCts;
    private AppSettings _settings;

    /// <summary>
    /// The volume question as the app asks it, and the value both readings fall
    /// back to. A wrapper rather than the method itself because
    /// <see cref="MoveSpaceCheck.ResolveIsOnInstallerCacheDrive"/> carries an
    /// optional second parameter, which no delegate of this shape can bind.
    /// </summary>
    private static bool? AskWhetherOnCacheVolume(string destination) =>
        MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(destination);

    /// <summary>
    /// Debounce window for write-back of MoveDestination edits. Each
    /// keystroke cancels the previous pending save; the save runs only
    /// if the user stops typing for this long. 400ms is roughly half a
    /// comfortable keystroke interval.
    /// </summary>
    /// <remarks>
    /// Exposed as <c>internal</c> so MainViewModelTests can wait on this
    /// value plus a small margin instead of hardcoding 700 ms (which
    /// drifts silently if the constant is ever tuned).
    /// </remarks>
    internal static readonly TimeSpan MoveDestinationSaveDelay = TimeSpan.FromMilliseconds(400);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MoveButtonTooltip))]
    private string _moveDestination = string.Empty;

    /// <summary>
    /// Whether the folder named in the box is on the volume the installer cache
    /// is on: true, false, or null where Windows has not answered. Read by
    /// <see cref="MoveButtonTooltip"/> and by nothing else.
    /// </summary>
    /// <remarks>
    /// NULL IS ITS OWN STATE AND NOT A SHADE OF FALSE, because two different
    /// things arrive here knowing nothing. The box changing clears the flag
    /// before anything is asked, and the volume question itself can come back
    /// unanswered on a machine where Windows will not resolve one of the two
    /// paths. Tooltip.Move covers both and says nothing about any volume, so
    /// neither state asserts anything.
    ///
    /// FALSE IS RESERVED FOR WINDOWS HAVING SAID THE FOLDER IS SOMEWHERE ELSE,
    /// and the tooltip happens to word that state and the two unknown ones
    /// alike. The distinction is kept anyway, because a reader meeting a plain
    /// bool here would take false for "another volume" and write the next claim
    /// on it. <see cref="MoveSpaceCheck.ResolveIsOnInstallerCacheDrive"/> is
    /// what keeps the three apart, and its bool sibling folds them together,
    /// which is right for a caller that acts on the answer and wrong for one
    /// that says something about it.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MoveButtonTooltip))]
    private bool? _destinationIsOnCacheVolume;

    // Reveals the operating overlay. It is not the "work is underway" flag:
    // it goes false during the confirm-dialog window between the pre-flight
    // and the move call (so the modal owns the foreground state), and it is set
    // only after 200 ms during the Move pre-flight. IsOperationInFlight is the
    // execution gate.
    [ObservableProperty] private bool _isOperating;

    /// <summary>
    /// True from the first line of a Move or a Delete until it has finished,
    /// pre-flight included, whether or not <see cref="IsOperating"/> ever
    /// raised the overlay. This is what the commands gate on.
    ///
    /// The Move pre-flight is the reason it exists. It hops off the dispatcher
    /// (a Win32 path resolve against a mapped drive can stall for the SMB
    /// timeout, and on the dispatcher that freezes the window), and while it is
    /// awaited the message loop pumps with no overlay up. Without this flag
    /// Delete is still clickable in that window, and a Delete started during a
    /// Move pre-flight would overwrite <see cref="_operationCts"/> while the
    /// Move was still using it, leaving the Move's Cancel button wired to the
    /// Delete's token.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MoveAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteAllCommand))]
    private bool _isOperationInFlight;
    [ObservableProperty] private string _operationProgress = string.Empty;
    [ObservableProperty] private int _operationCurrentFile;
    [ObservableProperty] private int _operationTotalFiles;
    [ObservableProperty] private string _operationCurrentFileName = string.Empty;
    [ObservableProperty] private double _operationProgressPercent;

    /// <summary>
    /// True while the overlay is up over work that reports no per-file
    /// progress, which today is the rescan after a cancel. The bar runs
    /// indeterminate and <see cref="ShowOperationProgressDetail"/> takes the
    /// count row and the filename off, because a bar and a count are a claim
    /// about how far through something is and there is no answer to give.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOperationProgressDetail))]
    private bool _isOperationProgressIndeterminate;

    /// <summary>
    /// Whether the overlay shows its "X of Y files", percentage and filename
    /// block. Positive so the XAML binds it through BooleanToVisibilityConverter
    /// like every other visibility in the window; there is no inverting
    /// converter and this is one property rather than one converter.
    /// </summary>
    public bool ShowOperationProgressDetail => !IsOperationProgressIndeterminate;

    /// <summary>
    /// Throttled copy of <see cref="OperationProgressDetail"/> for the
    /// screen-reader live region: updated on the first file, the last
    /// file and each time the batch crosses a tenth, never per file. A
    /// live region announces every text change, and per-file changes
    /// queue speech far faster than it can be spoken.
    /// </summary>
    [ObservableProperty] private string _operationProgressAnnouncement = string.Empty;

    /// <summary>Last tenth-of-batch boundary announced; -1 between operations.</summary>
    private int _lastAnnouncedDecile = -1;

    /// <summary>
    /// True between a Cancel click and the worker's next
    /// CancellationToken checkpoint. Gates the overlay Cancel button's
    /// IsEnabled binding and the disabled-state tooltip.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelOperationCommand))]
    private bool _isCancellationRequested;

    public CleanupViewModel(
        IMoveFilesService moveService,
        IDeleteFilesService deleteService,
        ISettingsService settingsService,
        IDialogService dialogService,
        IConfirmationService confirmationService,
        IFileSystem fileSystem,
        ScanViewModel scan,
        CompletionViewModel completion,
        IResultLogService resultLogService,
        IRemovableReverifier reverifier,
        Func<string, bool?>? resolveIsOnCacheVolume = null)
    {
        _moveService = moveService;
        _deleteService = deleteService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        _confirmationService = confirmationService;
        _fs = fileSystem;
        _scan = scan;
        _completion = completion;
        _resultLogService = resultLogService;
        _reverifier = reverifier;
        _resolveIsOnCacheVolume = resolveIsOnCacheVolume ?? AskWhetherOnCacheVolume;

        _settings = settingsService.Load();
        MoveDestination = _settings.MoveDestination;

        // Re-evaluate Move/Delete CanExecute when the upstream scan
        // VM's state changes. Held as a field so Dispose can unhook it;
        // the singleton container disposes this VM on shutdown.
        _scanHandler = OnScanPropertyChanged;
        _scan.PropertyChanged += _scanHandler;
    }

    private void OnScanPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScanViewModel.IsScanInFlight) ||
            e.PropertyName == nameof(ScanViewModel.OrphanedFileCount) ||
            e.PropertyName == nameof(ScanViewModel.HasPendingReboot))
        {
            MoveAllCommand.NotifyCanExecuteChanged();
            DeleteAllCommand.NotifyCanExecuteChanged();
        }
    }

    public void Dispose()
    {
        _scan.PropertyChanged -= _scanHandler;
        DisposeOperationCts();
        var saveCts = _moveDestinationSaveCts;
        _moveDestinationSaveCts = null;
        saveCts?.Cancel();
        saveCts?.Dispose();

        // A non-null CTS means an edit is still waiting out its debounce: the
        // field is nulled once a save lands. Cancelling alone throws that edit
        // away, so a destination typed or picked within 400 ms of the window
        // closing is never persisted and the box comes back empty (or holding
        // the old path) next session. The language switch relaunches through
        // this same Dispose, so it loses the same edit.
        //
        // Flushing here is safe: the debounced save's own continuation is
        // posted to a dispatcher that is about to stop pumping, so it would not
        // run anyway, and SettingsService.Update takes its own lock, reloads,
        // applies and saves atomically, so it cannot race the write just
        // cancelled. Update touches no view-model state, so a late write
        // landing after this VM is gone is not a hazard either.
        if (saveCts is not null)
            _settingsService.Update(s => s.MoveDestination = _settings.MoveDestination);
    }

    // Move and Delete gate on IsOperationInFlight, which is already set when
    // IsOperating flips; of the commands, only Cancel gates on IsOperating.
    partial void OnIsOperatingChanged(bool value) =>
        CancelOperationCommand.NotifyCanExecuteChanged();

    // The Move button works from any of the three states, so this says what
    // pressing it will do rather than naming a missing step: with no folder set
    // it warns that a browser opens first, so the browser is expected rather
    // than a surprise, and with a folder Windows has placed on this volume it
    // says when the space actually comes back.
    //
    // The same-drive state earns a string of its own because its outcome is not
    // what a reader expects of a move: nothing on the disk is reclaimed by it. A
    // hover is where that belongs, before the button is pressed and while the
    // destination can still be changed, and it is not the Move confirmation's
    // same-drive warning, which says a related thing once the choice has been
    // made.
    //
    // IT ASKED THE QUESTION HERE UNTIL 3.0.0 AND NOW READS AN ANSWER SOMEBODY
    // ELSE FETCHED. This property is recomputed on every keystroke in the box
    // (NotifyPropertyChangedFor on MoveDestination) and it runs on the
    // dispatcher, and the only call that can settle which volume a folder is on
    // validates a remote path over the network. On a half-typed \\server, or on
    // a mapped letter whose share has gone away, that is the SMB stall this
    // class already refuses to take on the dispatcher for the Move pre-flight.
    // So the call happens on a background hop and lands in
    // DestinationIsOnCacheVolume, and this property only reads a flag.
    //
    // ONLY THE SAME-DRIVE STATE SAYS ANYTHING ABOUT SPACE, AND THE TEST IS
    // AGAINST true RATHER THAN AGAINST FALSENESS. Windows placing the folder on
    // this volume is the one answer that earns that sentence; a folder placed
    // elsewhere, a question still in flight and a question Windows will not
    // answer all take the plain wording, which claims nothing about any volume
    // and is true of all three. Where the space goes is explained on the main
    // window above the buttons.
    //
    // WHAT THAT COSTS, SET AGAINST WHAT IT REPLACED, because a reader meeting
    // an async flag and a staleness window should see the trade rather than
    // half of it. The answer here can be briefly out of date: it is taken once
    // the box settles, so a mount point created or removed while nobody is
    // typing leaves the last answer standing. The answer it replaced was not
    // stale, it was WRONG, and permanently so on that machine, because a path
    // root cannot tell a mounted volume from a folder. And nothing acts on this
    // one: the confirmation dialog, the free-space refusal and the completion
    // line all re-ask inside the pre-flight at the moment of action.
    public string MoveButtonTooltip =>
        string.IsNullOrWhiteSpace(MoveDestination) ? Strings.Tooltip_MoveNeedsDestination
        : DestinationIsOnCacheVolume == true ? Strings.Tooltip_MoveSameDrive
        : Strings.Tooltip_Move;

    partial void OnMoveDestinationChanged(string value)
    {
        // ABOVE THE EARLY RETURN BELOW, DELIBERATELY, AND THE CONSTRUCTOR IS
        // WHY. It restores the saved destination through this property, and on
        // that pass the new value already equals settings, so the return fires
        // and anything scheduled after it would never run for a folder the user
        // had chosen in an earlier session. The tooltip would then never say
        // same-drive for the commonest destination in the app.
        //
        // The flag goes null first and unconditionally: it describes a path,
        // and the path has just changed, so it has nothing to say until the
        // resolve lands. Tooltip.Move covers that window and says nothing about
        // any volume, so nothing is asserted while it stands.
        //
        // Resolved from the TRIMMED value, because that is the string the Move
        // itself uses (MoveAllAsync takes MoveDestination.Trim()), and a tooltip
        // answering for a different string from the one the button acts on is
        // the whole class of fault being fixed here.

        // settings.json holds the trimmed string so a reader (CLI /m,
        // next session start) gets a normalised path. The TextBox
        // binding keeps the typed value mid-session.
        var normalised = value?.Trim() ?? string.Empty;

        DestinationIsOnCacheVolume = null;
        ScheduleDestinationVolumeResolve(normalised);

        if (string.Equals(_settings.MoveDestination, normalised, StringComparison.Ordinal))
            return;

        _settings.MoveDestination = normalised;
        ScheduleMoveDestinationSave();
    }

    /// <summary>
    /// Starts the background resolve of which volume
    /// <paramref name="destination"/> is on, cancelling any resolve still in
    /// flight for the path this one replaces.
    /// </summary>
    /// <remarks>
    /// The same debounce as the settings write-back, and the same constant: a
    /// typist should fire one resolve when they stop, not one per character, and
    /// a resolve is two volume queries rather than the one it was before the
    /// cache side started asking. Sharing <see cref="MoveDestinationSaveDelay"/>
    /// rather than adding a second constant keeps the two from drifting apart
    /// for no reason anybody could later name.
    ///
    /// THE CANCEL IS THE PART THAT MATTERS, more than the debounce. A resolve
    /// in flight belongs to the path that started it; if it landed after the box
    /// had moved on it would publish an answer about a folder nobody is looking
    /// at. Cancelling here and re-checking the token on the way back are the two
    /// halves of that, and the second is needed as well because a token cancelled
    /// after the delay has already elapsed does not stop the work.
    /// </remarks>
    private void ScheduleDestinationVolumeResolve(string? destination)
    {
        var previous = _destinationVolumeCts;
        _destinationVolumeCts = null;
        previous?.Cancel();
        previous?.Dispose();

        // An empty box needs no answer: MoveButtonTooltip tests it first and
        // never reaches the flag. Cancelling above is what the clear case is
        // really for, so a resolve started for the path just deleted cannot
        // land on the emptied box.
        if (string.IsNullOrWhiteSpace(destination)) return;

        var cts = new CancellationTokenSource();
        _destinationVolumeCts = cts;
        _ = ResolveDestinationVolumeAsync(destination, cts.Token);
    }

    private async Task ResolveDestinationVolumeAsync(string destination, CancellationToken token)
    {
        try
        {
            await Task.Delay(MoveDestinationSaveDelay, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // OFF THE DISPATCHER, AND THAT IS THE WHOLE POINT OF THIS METHOD. The
        // resolve is MoveSpaceCheck.ResolveIsOnInstallerCacheDrive, which reaches
        // GetVolumePathName, and Win32 documents that as validating a remote path
        // over the network; IsRemotePath turns away the shapes that say so in
        // their spelling, and a mapped drive letter does not. ConfigureAwait(true)
        // to come back and publish on the thread the binding is read from.
        //
        // No token on the Task.Run: the debounce is the cancellable part and it
        // is over, the body is two volume queries and a token cannot abandon a
        // Win32 call once it has started, and the token check below is what
        // decides whether the answer is still wanted.
        // The three-state answer, so that Windows declining the question is not
        // published as "somewhere else". The tooltip says something about this
        // rather than acting on it, and the two refusals are not the same thing
        // to a sentence a person reads.
        var onCacheVolume = await Task
            .Run(() => _resolveIsOnCacheVolume(destination))
            .ConfigureAwait(true);

        // Publish only if this resolve is still the current one. A keystroke
        // landing during the query installs a fresh CTS, and its own resolve
        // will answer for the path now in the box.
        if (_destinationVolumeCts is not { } current || current.Token != token) return;

        // PUT THE SOURCE AWAY FIRST AND PUBLISH LAST. The publish is what
        // raises PropertyChanged, so it is the moment anything else gets to
        // look at this view model. Doing it after the clear-up means what
        // they find is a resolve that has finished rather than one still
        // halfway through putting itself away, and the field is already
        // empty for whatever reads it next.
        current.Dispose();
        _destinationVolumeCts = null;
        DestinationIsOnCacheVolume = onCacheVolume;
    }

    /// <summary>
    /// Debounced write-back. Each call cancels the previous pending
    /// save so a typist doesn't fire a save per character; the actual
    /// save fires after <see cref="MoveDestinationSaveDelay"/>.
    /// </summary>
    private void ScheduleMoveDestinationSave()
    {
        var previous = _moveDestinationSaveCts;
        var cts = new CancellationTokenSource();
        _moveDestinationSaveCts = cts;
        previous?.Cancel();
        previous?.Dispose();

        _ = SaveAfterDelayAsync(cts.Token);
    }

    private async Task SaveAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(MoveDestinationSaveDelay, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Reload-then-merge happens off the dispatcher because Load()
        // and TrySave() are disk hops (OneDrive-redirected or network-
        // roaming profiles bite hardest). The current MoveDestination
        // is captured on the dispatcher BEFORE the worker starts, so a
        // keystroke landing during the disk hop writes into
        // _settings.MoveDestination on the dispatcher and gets picked
        // up by the next debounce cycle without disturbing this save.
        // The _settings field is NOT reassigned. Swapping in a freshly loaded
        // instance orphans the one the dispatcher is writing into, losing any
        // keystroke that landed between the worker's snapshot read and the
        // swap. SettingsService.Update reloads, applies the captured destination
        // and saves atomically under its own lock, so this thread-pool debounce
        // cannot lose the result-log lifetime lock or the language pick that run
        // on the dispatcher to a last-writer-wins rename.
        //
        // No token on this Task.Run, deliberately. The debounce is the
        // cancellable part and it is already over; the body has nothing to
        // interrupt (one Update call, which serialises on its own lock) and
        // nothing to gain by being skipped.
        var destinationSnapshot = _settings.MoveDestination;
        await Task.Run(() =>
        {
            if (!_settingsService.Update(s => s.MoveDestination = destinationSnapshot))
            {
                // Update returns false on disk-full / read-only-profile /
                // path-redirection failure (it never throws). Without a
                // breadcrumb a user report of "my destination reset between
                // sessions" has no trail back to the failed write.
                CrashLog.TryWrite(new InvalidOperationException(
                    "Settings.Update returned false during MoveDestination debounced save."));
            }
        }).ConfigureAwait(true);

        // Dispose the type-once-and-stop case (every other path is
        // covered by the next schedule call replacing the field).
        // Token equality skips disposal if a fresh keystroke already
        // installed a new CTS during the await.
        if (_moveDestinationSaveCts is { } current && current.Token == token)
        {
            _moveDestinationSaveCts = null;
            current.Dispose();
        }
    }

    // Move and Delete are gated on HasPendingReboot for three reasons:
    // an MSI is in flight, a previous transaction is suspended, or a queued
    // post-reboot rename targets the cache (see IPendingRebootService).
    // The banner is informational only; the CanExecute predicate is what
    // stops a click from reaching the service.
    // Both gates are execution flags, not the overlay flags: IsScanning is only
    // set once a scan passes 200 ms, and IsOperating is unset through both
    // pre-flights, so reading either here leaves a window in which the other
    // destructive command is still clickable.
    // Deliberately identical to CanDelete: Move does NOT require a
    // destination to be typed first. A Move with an empty box asks where to
    // put the files and carries on, so gating the button on the box would
    // hide the action from anyone moving through the window by keyboard.
    // A disabled control is skipped by Tab entirely, so the button, its
    // spoken description and the tooltip explaining what it needs were all
    // unreachable without a mouse, which is the one route a screen-reader
    // user does not have.
    private bool CanMove() =>
        !_scan.IsScanInFlight && !IsOperationInFlight
        && !_scan.HasPendingReboot
        && _scan.OrphanedFileCount > 0;

    private bool CanDelete() =>
        !_scan.IsScanInFlight && !IsOperationInFlight
        && !_scan.HasPendingReboot
        && _scan.OrphanedFileCount > 0;

    [RelayCommand]
    private void BrowseDestination()
    {
        var chosen = _confirmationService.AskForMoveDestination(MoveDestination);
        if (chosen is not null) MoveDestination = chosen;
    }

    private bool CanCancelOperation() => IsOperating && !IsCancellationRequested;

    [RelayCommand(CanExecute = nameof(CanCancelOperation))]
    private void CancelOperation()
    {
        IsCancellationRequested = true;
        // Races the finally block that disposes _operationCts;
        // ObjectDisposedException here means the operation finished
        // before the click reached the dispatcher.
        try { _operationCts?.Cancel(); }
        catch (ObjectDisposedException) { }
        // The move/delete loop only repaints OperationProgress on its
        // next iteration. Without a synchronous write the overlay holds
        // "Moving 23 of 100..." for one iteration past the click. The
        // operation loop then clears it, or replaces it with a partial-
        // progress line, once the loop observes the cancellation.
        OperationProgress = Strings.Status_Cancelling;
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private async Task MoveAllAsync()
    {
        if (_scan.LastScanResult is null) return;

        // Move is offered without a destination typed, so this is where one
        // gets asked for. Cancelling the browser abandons the move and
        // touches nothing; the box is left as it was, empty.
        if (string.IsNullOrWhiteSpace(MoveDestination))
        {
            var chosen = _confirmationService.AskForMoveDestination();
            if (chosen is null) return;
            MoveDestination = chosen;
        }

        // Capture pre-operation scan state for the result-log entry.
        // RefreshAsync below replaces _scan.LastScanResult with the
        // post-move state, so the diagnostic record needs the pre-move
        // result captured here.
        var preOpScan = _scan.LastScanResult;
        var preOpDurationMs = _scan.LastScanDurationMs;

        var dest = MoveDestination.Trim();

        // Relative destinations are refused before anything resolves or creates
        // them. Every gate below goes through Path.GetFullPath, which expands a
        // bare "backup" against the process CWD, so the pre-flight would create
        // that folder and write its probe file somewhere the user never named,
        // elevated, and only then would MoveFilesService refuse the batch, after
        // the confirmation dialog. The CWD is wherever the process happened to be
        // started from and nothing in the app constrains it, so the two
        // out-of-bounds gates below catching such a path is luck, not a gate.
        // Same three checks in the same order as the CLI's
        // ResolveAndValidateMoveDestination; MoveFilesService keeps its own copy
        // at the service boundary for callers that never come through here.
        if (!Path.IsPathFullyQualified(dest))
        {
            _dialogService.ShowWarning(
                string.Format(Strings.Error_DestinationNotFullyQualified, dest),
                Strings.Error_InvalidDestinationTitle);
            return;
        }

        // Every touch of the destination happens here, on a worker thread and
        // under one cancellable task: the two path gates, the CreateDirectory
        // and write probe, the drive classification and the free-space query.
        // Each of them resolves or queries a path through Win32, so each can
        // stall for the SMB timeout on a mapped drive or a UNC share that has
        // gone away, and any of them on the dispatcher freezes the window.
        // The CTS is created first so the overlay's Cancel button can
        // interrupt the wait. The probe goes through IFileSystem so
        // MockFileSystem-backed tests don't hit real disk; the gates and the
        // free-space query deliberately do not (a mock must not be able to
        // talk its way past a safety check).
        //
        // Nothing here touches view-model state: the verdict comes back as a
        // record and is applied below, on the dispatcher.
        _operationCts = new CancellationTokenSource();
        var probeToken = _operationCts.Token;
        IsOperationInFlight = true;
        // Captured rather than carried back on DestinationPreFlight, because
        // the two catch arms below need it just as much as the returns do and
        // neither of them ever gets a record: a cancel or a write failure part
        // way through the probe abandons the move with the folder already
        // created. The write happens before the first await, and the await is
        // the barrier, so the read after it sees it.
        var createdDestination = false;
        var probeTask = Task.Run(() =>
        {
            // Never let files move back inside C:\Windows\Installer.
            // ResolveFinalPath inside IsInstallerFolderOrChild expands
            // junctions so a reparse-point destination cannot smuggle the
            // batch into the cache folder. Both gates run before the probe
            // creates anything.
            if (InstallerCacheHelpers.IsInstallerFolderOrChild(dest))
                return DestinationPreFlight.Rejected(insideInstallerCache: true);

            // %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and
            // %ProgramData% sit on the Win32 DLL search path and the SxS
            // resolution path: a process searching those paths trusts a
            // planted file at load time.
            if (InstallerCacheHelpers.IsSystemFolderOrChild(dest))
                return DestinationPreFlight.Rejected(insideSystemFolder: true);

            // Asked before the create, because after it the answer is always
            // yes. A destination this pass conjured up and a move that then
            // never happens should leave nothing behind; one that was already
            // there is the user's folder and is never touched. Set before
            // CreateDirectory so a throw from the create cannot leave the flag
            // saying no about a folder that did appear.
            createdDestination = !_fs.Directory.Exists(dest);
            _fs.Directory.CreateDirectory(dest);
            probeToken.ThrowIfCancellationRequested();
            var probe = _fs.Path.Combine(dest, _fs.Path.GetRandomFileName());
            _fs.File.WriteAllBytes(probe, Array.Empty<byte>());
            try
            {
                probeToken.ThrowIfCancellationRequested();
                // The delete stays inside the try so a failure to remove the
                // probe still refuses the destination: a folder that takes a
                // file and will not give it back is not writable for a Move.
                _fs.File.Delete(probe);
            }
            catch (OperationCanceledException)
            {
                // Cancelled between the write and the delete. The probe has to
                // go on this path too, because RemoveCreatedDestinationAsync
                // removes the folder non-recursively as its emptiness test, so
                // a probe left here strands the folder along with it. Silent
                // because the cancel is the outcome being reported.
                try { _fs.File.Delete(probe); } catch { }
                throw;
            }
            probeToken.ThrowIfCancellationRequested();

            // A same-volume move is a rename and consumes no space, so the
            // free-space check would otherwise refuse exactly the nearly-full
            // drive holding the installer cache, which is the machine this app
            // exists for; the caller applies the check only when the move really
            // copies. The cache's drive rather than the system drive, because
            // that is the volume MoveSpaceCheck compares a destination against,
            // and the two part wherever a volume is mounted at the cache folder,
            // which is the case this arm exists for.
            return new DestinationPreFlight(
                false, false,
                ClassifyMoveDestination(dest, _resolveIsOnCacheVolume),
                StorageHelpers.GetAvailableFreeSpace(dest));
        }, probeToken);

        DestinationPreFlight preFlight;
        try
        {
            // Reveal the operating overlay only if the probe is slow, the way
            // the scan waits 200 ms before showing its overlay. A local
            // destination probes in well under that, so the overlay (and a
            // screen reader's "Preparing destination folder...") never flashes
            // in the instant before the confirm dialog; a UNC share that
            // stalls on the SMB timeout still gets the overlay and its
            // cancellable Cancel button. Heading before IsOperating keeps the
            // start announced exactly once if the overlay does appear.
            if (await Task.WhenAny(probeTask, Task.Delay(200, probeToken)) != probeTask)
            {
                OperationProgress = Strings.Status_PreparingDestination;
                IsOperating = true;
            }
            preFlight = await probeTask;
            // A Cancel clicked while the probe was in flight can lose the race
            // with a probe that completes anyway (the token is only checked
            // between the probe's filesystem calls). Without this the click is
            // honoured too late: the confirmation dialog opens, and only the
            // move that follows fails on the already-cancelled token.
            probeToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            IsOperating = false;
            OperationProgress = string.Empty;
            DisposeOperationCts();
            if (createdDestination) await RemoveCreatedDestinationAsync(dest);
            return;
        }
        catch (Exception ex)
        {
            IsOperating = false;
            OperationProgress = string.Empty;
            // ex.Message stays out of the dialog: path-leak risk under elevation.
            var crash = CrashLog.TryWrite(ex);
            DisposeOperationCts();
            if (createdDestination) await RemoveCreatedDestinationAsync(dest);
            _dialogService.ShowWarning(
                DescribeWriteFailure(dest, ex, crash.Path, crash.Written),
                Strings.Error_DestinationWriteFailedTitle);
            return;
        }
        IsOperating = false;
        OperationProgress = string.Empty;

        if (preFlight.InsideInstallerCache)
        {
            DisposeOperationCts();
            _dialogService.ShowWarning(
                Strings.Error_DestinationInsideInstaller,
                Strings.Error_InvalidDestinationTitle);
            return;
        }

        if (preFlight.InsideSystemFolder)
        {
            DisposeOperationCts();
            _dialogService.ShowWarning(
                string.Format(Strings.Error_DestinationInSystemFolder, dest),
                Strings.Error_InvalidDestinationTitle);
            return;
        }

        var removableFiles = _scan.LastScanResult.RemovableFiles;
        var filePaths = removableFiles.Select(f => f.FullPath).ToList();
        var count = filePaths.Count;
        var totalBytes = removableFiles.Sum(f => f.SizeBytes);
        var sizeDisplay = _scan.OrphanedSizeDisplay;

        var destinationKind = preFlight.Kind;

        // Free-space check, decided in Core so the command line refuses the same
        // moves this does. The measurement is the pre-flight's, taken off the
        // dispatcher with the rest of the destination work; what MoveSpaceCheck
        // adds is which measurements are worth acting on.
        //
        // IT ASKS ITS OWN SAME-VOLUME QUESTION HERE, ON THIS THREAD, and that is
        // deliberate rather than an oversight in a class that otherwise keeps
        // volume work off it. Handing it the answer the pre-flight already has
        // would mean a second way of reaching that verdict, and the two hosts
        // agreeing about it is the reason the rule lives in Core at all.
        //
        // THAT IS TWO VOLUME QUERIES NOW AND IT WAS ONE, SO THE ARGUMENT BELOW
        // COVERS TWO PATHS AND USED TO COVER ONE. The cache side of the question
        // was a value resolved once for the life of the process, on the ground
        // that the volume running Windows cannot be remounted underneath it. It
        // asked about the Windows system directory rather than the installer
        // cache, which is a different volume wherever one is mounted at
        // C:\Windows\Installer, and the ground went with the subject when that
        // was corrected: a volume mounted there is precisely the thing that can
        // appear or vanish while the app is open. So the cache side is a real
        // call per operation now and has to earn its place on this thread the
        // way the destination side does.
        //
        // WHAT HAS ALREADY HAPPENED TO EACH PATH BEFORE THIS LINE RUNS, which is
        // the argument, rather than a claim that neither of them can stall. The
        // destination: the pre-flight above created this folder, wrote a file
        // into it and deleted that file, so a destination that could stall has
        // already stalled, behind the overlay and its working Cancel, and
        // nothing awaits between there and here. The cache folder: CanMove needs
        // a scan result with files in it, so the scan has walked that folder
        // before this button could be pressed, on its own background thread with
        // its own cancel.
        //
        // AND READ Kernel32.GetVolumePathName's PROHIBITION WITH THE CLAUSE IT
        // HANGS OFF, because it is absolute read alone. It says no caller may
        // make this call on the dispatcher; the sentence it follows from says
        // why, which is that a remote path is validated over the network and a
        // share that is not answering costs an SMB timeout. The cache path is
        // the Windows directory with Installer under it, so it can be neither a
        // share nor a mapped letter and nothing it does is a round trip. The
        // destination can be a mapped letter, which is local in shape and
        // remote in fact and which IsRemotePath does not turn away, so the
        // guard is not what makes this side safe: what makes it safe is that
        // the pre-flight has just written a file to that same path. The rule
        // stands as written. The claim here is that these two paths sit outside
        // its reason, not that the reason is loose.
        if (MoveSpaceCheck.RefusalFreeSpace(dest, totalBytes, preFlight.AvailableFreeSpace)
            is long free)
        {
            // Pre-flight CTS no longer needed; dispose before returning.
            DisposeOperationCts();
            OperationProgress = string.Empty;
            if (createdDestination) await RemoveCreatedDestinationAsync(dest);
            _dialogService.ShowWarning(
                string.Format(Strings.Error_NotEnoughSpaceBody,
                    dest,
                    DisplayHelpers.FormatSize(totalBytes),
                    DisplayHelpers.FormatSize(free)),
                Strings.Error_NotEnoughSpaceTitle);
            return;
        }

        // Use the captured `dest` consistently from here through the
        // move call. Reading MoveDestination live would re-read whatever
        // is in the textbox at that instant; if the user managed to
        // change it between the IsInstallerFolderOrChild validation and
        // here, the validated path and the moved-to path would diverge.
        // The pre-flight already classified the volume, so the confirmation can
        // tell the user the one thing this app exists to be right about: a move
        // to the drive the files are already on frees nothing until the copies
        // parked there are deleted.
        if (!_confirmationService.ConfirmMove(count, sizeDisplay, dest,
                destinationKind == MoveDestinationKinds.SameDrive))
        {
            // User cancelled at the confirmation dialog. The pre-flight
            // CTS is no longer needed; dispose it before returning.
            DisposeOperationCts();
            OperationProgress = string.Empty;
            if (createdDestination) await RemoveCreatedDestinationAsync(dest);
            return;
        }

        // Re-check the pending-reboot gate at the moment of action, after the
        // confirmation and immediately before acting. The scan sampled it once; a
        // user who left the confirmation dialog open while a Windows Installer
        // transaction started would otherwise move files against a live install,
        // the one condition the gate exists to block. Blocked => the banner paints
        // via HasPendingReboot and both commands drop out; refuse without calling
        // the service.
        if (await _scan.RecheckPendingRebootAsync())
        {
            DisposeOperationCts();
            OperationProgress = string.Empty;
            if (createdDestination) await RemoveCreatedDestinationAsync(dest);
            return;
        }

        // Heading before IsOperating: a heading assigned after the reveal
        // can be spoken twice (see OperationHeadingText in MainWindow.xaml).
        // It names no count, so it needs neither formatting nor a plural form;
        // the "X of Y" line beneath it is what counts, and it counts the batch
        // the re-verify actually handed over rather than the one the scan found.
        OperationProgress = Strings.Status_Moving;
        IsOperating = true;

        // Started with the overlay and read where the report is built, so the
        // figure is the whole wait the user sat through: the pre-act re-verify,
        // the batch itself and the post-batch refresh. Narrower readings were
        // available and none of them is the question, which is whether moving a
        // few thousand files is a pleasant thing to do.
        var operationTimer = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Re-verify the removable set against the API immediately before
            // acting, behind the overlay. This closes the one window neither the
            // fresh gate nor the mutex hold can see: a patch whose state changed
            // AND settled between the scan and the click (a superseded patch
            // reverted to Applied because its superseding patch was uninstalled).
            // It re-runs the enumeration (a beat) and can fail; a failure must STOP
            // the batch, never act on an un-verified set, so it is surfaced through
            // the scan's own error ladder. A cancellation propagates to the outer
            // OCE catch.
            ReverifyResult reverify;
            try
            {
                reverify = await _reverifier.ReverifyAsync(filePaths, _operationCts!.Token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var failure = _scan.DescribeScanFailure(ex);
                OperationProgress = string.Empty;
                // The re-verify runs before the batch, so nothing was placed.
                if (createdDestination) await RemoveCreatedDestinationAsync(dest);
                if (failure.IsError)
                    _dialogService.ShowError(failure.Message, failure.Title);
                else
                    _dialogService.ShowWarning(failure.Message, failure.Title);
                return;
            }

            var survivingSet = new HashSet<string>(reverify.Surviving, StringComparer.OrdinalIgnoreCase);
            var survivingFiles = removableFiles.Where(f => survivingSet.Contains(f.FullPath)).ToList();

            if (survivingFiles.Count == 0)
            {
                // The re-verify kept every candidate back. Act on nothing and
                // report it, with the re-verify's own reason for keeping them.
                await RefreshAfterBatchAsync();
                _completion.ShowReverifyAllSkipped(reverify, deleting: false);
                OperationProgress = string.Empty;
                if (createdDestination) await RemoveCreatedDestinationAsync(dest);
                return;
            }

            var survivingPaths = survivingFiles.Select(f => f.FullPath).ToList();
            var survivingBytes = survivingFiles.Sum(f => f.SizeBytes);

            // _operationCts was created in the pre-flight block above; reuse it
            // through the move so a single Cancel signal covers the pre-flight, the
            // re-verify and the move loop.
            var progress = new Progress<OperationProgress>(OnOperationProgressUpdate);
            MoveResult result;
            try
            {
                result = await _moveService.MoveFilesAsync(survivingPaths, dest,
                    UnderLeaseClaims.From(reverify), progress, _operationCts!.Token);
            }
            catch (MoveAbortedException ex)
            {
                // The destination could no longer be confirmed part-way through,
                // so the service stopped and handed back what it had done. Caught
                // here rather
                // than at the method's own arms so the surviving list and the
                // destination kind are still in scope to report with; the warning
                // over the summary carries the reason. No result-log entry, for
                // the reason recorded on the cancel arm below.
                await RefreshAfterBatchAsync();
                // Same fold as the main path: a batch the destination guard
                // stopped still owes an account of anything the under-lease re-read
                // took back before it started.
                var abortReclaimed = new HashSet<string>(ex.Partial.HeldBack, StringComparer.OrdinalIgnoreCase);
                var abortSurviving = ex.Partial.HeldBack.Count == 0
                    ? survivingFiles
                    : survivingFiles.Where(f => !abortReclaimed.Contains(f.FullPath)).ToList();
                // Its own heading, and not the one the other four destination
                // messages carry: the guard behind this exception has two
                // conditions and a share that dropped or an access rule that closed
                // leaves the folder where the user put it. Nothing selects on which
                // fired, so the heading says what the batch did.
                //
                // IT GOES UP BEFORE THE SUMMARY CARD AND THE ORDER IS LOAD-BEARING.
                // Revealing the card raises IsComplete, and the window answers that
                // by queueing a focus move and the card's outcome announcement onto
                // the dispatcher rather than running them where they are raised. A
                // modal opened in the same stretch is what drains that queue, so the
                // focus would land on a control sitting behind the dialog and the
                // outcome would be spoken while the dialog held focus. Showing the
                // dialog first leaves the reveal a clear queue: the reader dismisses
                // the warning, the card appears, and its focus move and its
                // announcement run with nothing over them. The card's own priorities
                // are what order those two against each other, in the window.
                _dialogService.ShowWarning(ex.Message, Strings.Error_MoveStoppedTitle);
                if (ex.Partial.MovedCount > 0 || ex.Partial.Errors.Count > 0)
                {
                    // The service's destination, never `dest`. On the condition
                    // where the destination resolves somewhere else the two are
                    // different folders, so the summary line must name the one the
                    // files are in, and the line under it names the same folder
                    // again in the sentence sending the reader to check it.
                    _completion.ShowMoveStoppedSummary(ex.Partial.MovedCount,
                        CompletedBytes(abortSurviving, ex.Partial.MovedCount, ex.Partial.Errors),
                        ex.Destination, ex.Partial.Errors, ClassifySpaceOutcome(destinationKind),
                        FoldHeldBack(reverify, ex.Partial.HeldBack, ex.Partial.HeldBackReasons));
                }
                OperationProgress = string.Empty;
                return;
            }

            if (result.InstallerBusy)
            {
                // A Windows Installer transaction grabbed Global\_MSIExecute in the
                // sub-millisecond gap between the act-time gate re-check above
                // passing and the service acquiring the mutex, so the service
                // refused and touched nothing. Re-run the gate, which now reports
                // the held mutex, to paint the banner, and report no completed
                // operation.
                await _scan.RecheckPendingRebootAsync();
                OperationProgress = string.Empty;
                // The service refused at the mutex, before its own
                // CreateDestinationFolder, so nothing was placed.
                if (createdDestination) await RemoveCreatedDestinationAsync(dest);
                return;
            }

            if (result.InstallerLockUnavailable)
            {
                // The service could not take Global\_MSIExecute and nothing was
                // shown to be holding it, so it refused and touched nothing. No
                // gate re-check here, unlike the arm above, and that is the whole
                // reason this is a separate flag rather than a second cause behind
                // the same one: the gate is no account of this condition whichever
                // way it answers, since it can come back clean and paint nothing,
                // leaving a refusal with no reason on screen, and on a DACL it
                // reports held and would paint a banner asserting an install
                // nothing has shown. A dialog carries the reason instead. The
                // service's own acquire has the detail.
                //
                // The destination cleanup matches the arm above, and for the same
                // reason: the service refuses ahead of its own
                // CreateDestinationFolder, so nothing it did needs undoing, and
                // what does is the folder THIS pre-flight made, which would
                // otherwise be left empty behind a batch that never ran.
                _dialogService.ShowWarning(
                    Strings.Error_MoveInstallerLockUnavailable,
                    Strings.Error_MoveInstallerLockUnavailableTitle);
                OperationProgress = string.Empty;
                if (createdDestination) await RemoveCreatedDestinationAsync(dest);
                return;
            }

            if (result.InstallerLockAccessRefused)
            {
                // The object's security refused the service the rights to open it,
                // so it never learned whether a transaction held it and refused
                // without touching anything. Same handling as the arm above and a
                // different sentence: the app was not allowed to look, which is not
                // the same as finding the lock busy, and only one of those two is
                // something an administrator can go and change.
                _dialogService.ShowWarning(
                    Strings.Error_MoveInstallerLockAccessRefused,
                    Strings.Error_MoveInstallerLockUnavailableTitle);
                OperationProgress = string.Empty;
                if (createdDestination) await RemoveCreatedDestinationAsync(dest);
                return;
            }

            if (result.HeldBack.Count > 0)
            {
                // The service's own re-read, taken under the installer mutex, took
                // these back. Nothing touched them, so they leave the batch's
                // account of itself entirely: the file list and the byte total
                // both shrink, and the re-verify's kept-back tally grows by the
                // same files. Ahead of every arm below, because each of them
                // reports one or both.
                var reclaimed = new HashSet<string>(result.HeldBack, StringComparer.OrdinalIgnoreCase);
                survivingFiles = survivingFiles.Where(f => !reclaimed.Contains(f.FullPath)).ToList();
                survivingBytes = survivingFiles.Sum(f => f.SizeBytes);
                reverify = FoldHeldBack(reverify, result.HeldBack, result.HeldBackReasons);
            }

            if (result.HeldBack.Count > 0 && survivingFiles.Count == 0)
            {
                // The under-lease re-read took the WHOLE batch back. See the
                // matching arm on the delete path for why this takes the pre-lease
                // twin's screen and its silence on the result log; Move owes a
                // third thing as well, and it is the reason the service is careful
                // to return before creating anything. The destination folder the
                // PRE-FLIGHT made is still there, and nothing was ever put in it.
                await RefreshAfterBatchAsync();
                _completion.ShowReverifyAllSkipped(reverify, deleting: false);
                OperationProgress = string.Empty;
                if (createdDestination) await RemoveCreatedDestinationAsync(dest);
                return;
            }

            if (result.Cancelled)
            {
                // Cancelled mid-batch: the service returned what completed rather
                // than throwing the tally away. Report the partial on the
                // completion overlay, and write no result-log entry for a
                // cancelled run (the owner's decision keeps the public reports
                // stats meaning what they mean). Only raise the overlay when
                // something actually moved or errored; a cancel that reached no
                // file just clears.
                await RefreshAfterBatchAsync();
                if (result.MovedCount > 0 || result.Errors.Count > 0)
                {
                    _completion.ShowMoveCancelledSummary(
                        result.MovedCount, survivingFiles.Count,
                        CompletedBytes(survivingFiles, result.MovedCount, result.Errors),
                        dest, result.Errors, ClassifySpaceOutcome(destinationKind), reverify);
                }
                OperationProgress = string.Empty;
                return;
            }

            var movedCount = result.MovedCount;
            var movedDest = dest;
            var errorCount = result.Errors.Count;

            long movedBytes;
            if (errorCount == 0)
                movedBytes = survivingBytes;
            else
            {
                var errorPaths = new HashSet<string>(result.Errors.Select(e => e.FilePath), StringComparer.OrdinalIgnoreCase);
                movedBytes = survivingFiles.Where(f => !errorPaths.Contains(f.FullPath)).Sum(f => f.SizeBytes);
            }

            // Refresh through the scan VM so the registered/orphaned counts
            // update before the completion overlay reads them, on the same helper
            // every other end of a batch uses.
            await RefreshAfterBatchAsync();

            _completion.ShowMoveSummary(movedCount, movedBytes, movedDest, result.Errors,
                ClassifySpaceOutcome(destinationKind), reverify);

            // Skip the last-run.json write once the result-log surface
            // is locked. Nothing will ever read the file from this point
            // on: the Send button stays hidden for the rest of the
            // session via the session lock, and the next session checks
            // the persisted lifetime lock at startup.
            if (!_completion.IsResultLogLocked)
            {
                var entry = ResultLogEntry.ForMove(
                    preOpScan, preOpDurationMs,
                    result, movedBytes,
                    operationTimer.ElapsedMilliseconds,
                    destinationKind,
                    // The folded tally, so the under-lease re-read's own kept-back
                    // files are counted with the pre-act ones rather than lost:
                    // the two producers keep back DIFFERENT files and the report
                    // owes an account of both.
                    reverify.Reasons);
                if (await _resultLogService.WriteAsync(entry).ConfigureAwait(true))
                    _completion.MarkResultLogReady();
            }
            // Completion overlay carries the user-facing summary; the
            // bottom-row pill stays blank once the overlay dismisses.
            OperationProgress = string.Empty;
        }
        catch (OperationCanceledException)
        {
            // A cancel before the worker starts (the token was cancelled between
            // the confirmation and Task.Run) moves nothing; a mid-batch cancel now
            // comes back as result.Cancelled above rather than as a throw, and is
            // reported on the overlay there. Nothing reached a file here, so just
            // refresh the counts and clear.
            await RefreshAfterBatchAsync();
            OperationProgress = string.Empty;
            if (createdDestination) await RemoveCreatedDestinationAsync(dest);
        }
        catch (LocalisedInvalidOperationException ex)
        {
            // Everything reaching here is one of MoveFilesService's destination
            // gates, all of which run before the per-file loop, so no file has
            // moved and the counts on screen are still right. The mid-batch
            // abort landed here once and no longer does, being a
            // MoveAbortedException caught at the call site where the batch's own
            // tally is still in scope; a rescan here would now be a full folder
            // walk and API enumeration for nothing.
            //
            // No RemoveCreatedDestinationAsync either, and that is a ruling
            // rather than an oversight: the gates that reach this arm fire
            // precisely because the destination has just been shown to resolve
            // into C:\Windows\Installer or a system folder, and deleting a
            // directory at a path just proven to land somewhere unexpected is
            // the operation those guards exist to prevent. The service's
            // fully-qualified check is not among them: the window refuses a
            // relative destination above, before the batch is handed over.
            _dialogService.ShowWarning(ex.Message, Strings.Error_InvalidDestinationTitle);
            OperationProgress = string.Empty;
        }
        catch (LocalisedAccessException ex)
        {
            // No refresh here, and that asymmetry is deliberate: every
            // LocalisedAccessException comes from CreateDestinationFolder or
            // ProbeDestinationWriteable, both of which run before the per-file
            // loop starts, so no file has moved and the counts on screen are
            // still correct. That same ordering is why the created folder can
            // be removed here: nothing was placed in it.
            if (createdDestination) await RemoveCreatedDestinationAsync(dest);
            _dialogService.ShowWarning(ex.Message, Strings.Error_DestinationWriteFailedTitle);
            OperationProgress = string.Empty;
        }
        catch (Exception ex)
        {
            // How far an unforeseen failure got is by definition unknown, so the
            // counts on screen may be describing files that have moved. The two
            // localised arms above are certain nothing had, their exceptions all
            // coming from gates that run ahead of the per-file loop, which is why
            // only they can skip the rescan. The delete path's twin of this arm
            // is certain of just as little, and rescans on the same reasoning.
            await RefreshAfterBatchAsync();
            // A mid-move crash is surfaced the way every other failure in the
            // app is: a dialog naming the exception type and the crash-log
            // path, never ex.Message (it can carry another user's profile path
            // under elevation). It is not left to the body-row status, which
            // trims at a width cap and would cut the log path off, and the log
            // path is the one actionable thing for a report. Reaching here is
            // rare: the move service collects per-file errors rather than throwing.
            var crash = CrashLog.TryWrite(ex);
            var typeName = ex.GetType().Name;
            OperationProgress = string.Empty;
            _dialogService.ShowWarning(
                crash.Written
                    ? string.Format(Strings.Status_MoveFailed, typeName, crash.Path)
                    : string.Format(Strings.Status_MoveFailed_NoLog, typeName),
                Strings.Error_MoveFailedTitle);
        }
        finally
        {
            // DisposeOperationCts also clears IsCancellationRequested.
            DisposeOperationCts();
            IsOperating = false;
            OperationProgressPercent = 0;
            // Stale-state reset: a cancel-then-rerun cycle would otherwise
            // briefly show the previous run's last filename and "X of Y"
            // line through the next operation's first progress callback.
            OperationCurrentFile = 0;
            OperationTotalFiles = 0;
            OperationCurrentFileName = string.Empty;
            OperationProgressAnnouncement = string.Empty;
            _lastAnnouncedDecile = -1;
            // Back to a measured bar: RefreshAfterBatchAsync sets this on
            // every path that stops a batch part-way, and the next operation
            // reports per-file progress.
            IsOperationProgressIndeterminate = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAllAsync()
    {
        if (_scan.LastScanResult is null) return;

        var removableFiles = _scan.LastScanResult.RemovableFiles;
        var count = removableFiles.Count;
        var sizeDisplay = _scan.OrphanedSizeDisplay;

        if (!_confirmationService.ConfirmDelete(count, sizeDisplay)) return;

        // Snapshot the pre-operation scan state once, after the confirm so a
        // cancel costs nothing. RefreshAsync (inside RunDeleteAsync) replaces
        // _scan.LastScanResult with the post-delete state, so the diagnostic
        // record needs the pre-delete result captured here.
        var ctx = new DeleteContext(
            removableFiles,
            removableFiles.Select(f => f.FullPath).ToList(),
            count,
            _scan.LastScanResult,
            _scan.LastScanDurationMs);

        await RunDeleteAsync(ctx);
    }

    /// <summary>
    /// Runs the delete and reports it on the completion overlay. Owns the
    /// operating overlay, the cancellation source and the per-run state reset.
    /// </summary>
    private async Task RunDeleteAsync(DeleteContext ctx)
    {
        _operationCts = new CancellationTokenSource();
        IsOperationInFlight = true;

        // Re-check the pending-reboot gate at the moment of action: a
        // transaction can start while the user is reading the confirmation
        // dialog. Blocked => paint the banner and refuse.
        if (await _scan.RecheckPendingRebootAsync())
        {
            DisposeOperationCts();
            OperationProgress = string.Empty;
            return;
        }

        // Heading before IsOperating: see the matching comment in the Move
        // path for why it carries no count.
        OperationProgress = Strings.Status_Deleting;
        IsOperating = true;

        // Same span as the Move path's timer, for the same reason.
        var operationTimer = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Re-verify the removable set against the API immediately before
            // acting, as the Move path does: a patch reverted to Applied between
            // the scan and the click must not be deleted. A failure STOPS the
            // batch (surfaced through the scan's error ladder); a cancellation
            // propagates to the outer OCE catch.
            ReverifyResult reverify;
            try
            {
                reverify = await _reverifier.ReverifyAsync(ctx.FilePaths, _operationCts.Token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var failure = _scan.DescribeScanFailure(ex);
                OperationProgress = string.Empty;
                if (failure.IsError)
                    _dialogService.ShowError(failure.Message, failure.Title);
                else
                    _dialogService.ShowWarning(failure.Message, failure.Title);
                return;
            }

            var survivingSet = new HashSet<string>(reverify.Surviving, StringComparer.OrdinalIgnoreCase);
            var survivingFiles = ctx.RemovableFiles.Where(f => survivingSet.Contains(f.FullPath)).ToList();

            if (survivingFiles.Count == 0)
            {
                // The re-verify kept every candidate back. Act on nothing and
                // report it, with the re-verify's own reason for keeping them.
                await RefreshAfterBatchAsync();
                _completion.ShowReverifyAllSkipped(reverify, deleting: true);
                OperationProgress = string.Empty;
                return;
            }

            var survivingPaths = survivingFiles.Select(f => f.FullPath).ToList();
            var survivingBytes = survivingFiles.Sum(f => f.SizeBytes);

            var progress = new Progress<OperationProgress>(OnOperationProgressUpdate);
            var result = await _deleteService.DeleteFilesAsync(
                survivingPaths, UnderLeaseClaims.From(reverify), progress, _operationCts.Token);

            if (result.InstallerBusy)
            {
                // A Windows Installer transaction grabbed Global\_MSIExecute after
                // the act-time gate re-check above passed, so the service refused
                // and touched nothing. Re-run the gate to paint the banner and
                // report no completed operation.
                await _scan.RecheckPendingRebootAsync();
                OperationProgress = string.Empty;
                return;
            }

            if (result.InstallerLockUnavailable)
            {
                // The service could not take Global\_MSIExecute and nothing was
                // shown to be holding it, so it refused and touched nothing. No
                // gate re-check here, unlike the arm above, and that is the whole
                // reason this is a separate flag rather than a second cause behind
                // the same one: the gate is no account of this condition whichever
                // way it answers, since it can come back clean and paint nothing,
                // leaving a refusal with no reason on screen, and on a DACL it
                // reports held and would paint a banner asserting an install
                // nothing has shown. A dialog carries the reason instead. The
                // service's own acquire has the detail.
                _dialogService.ShowWarning(
                    Strings.Error_InstallerLockUnavailable, Strings.Error_InstallerLockUnavailableTitle);
                OperationProgress = string.Empty;
                return;
            }

            if (result.InstallerLockAccessRefused)
            {
                // The object's security refused the service the rights to open it,
                // so it never learned whether a transaction held it and refused
                // without touching anything. Same handling as the arm above and a
                // different sentence: the app was not allowed to look, which is not
                // the same as finding the lock busy, and only one of those two is
                // something an administrator can go and change.
                _dialogService.ShowWarning(
                    Strings.Error_InstallerLockAccessRefused, Strings.Error_InstallerLockUnavailableTitle);
                OperationProgress = string.Empty;
                return;
            }

            if (result.HeldBack.Count > 0)
            {
                // The service's own re-read, taken under the installer mutex, took
                // these back. Nothing touched them, so they leave the batch's
                // account of itself entirely, exactly as on the Move path.
                var reclaimed = new HashSet<string>(result.HeldBack, StringComparer.OrdinalIgnoreCase);
                survivingFiles = survivingFiles.Where(f => !reclaimed.Contains(f.FullPath)).ToList();
                survivingBytes = survivingFiles.Sum(f => f.SizeBytes);
                reverify = FoldHeldBack(reverify, result.HeldBack, result.HeldBackReasons);
            }

            if (result.HeldBack.Count > 0 && survivingFiles.Count == 0)
            {
                // The under-lease re-read took the WHOLE batch back, so this is
                // the arm above having emptied it rather than a batch that ran.
                // One condition owes one screen and one record, and the pre-lease
                // twin already decided what both are, so this takes them: the
                // all-skipped overlay, and no result-log entry.
                //
                // Falling through instead reported a completed operation of zero.
                // The heading suppression below turns on errors, and there are
                // none here, so the summary printed "0 files permanently deleted"
                // against its own comment saying that reports an act that did not
                // happen, and a bytesFreed of zero reached the result log, where
                // it is a run that freed nothing rather than a run that never was.
                await RefreshAfterBatchAsync();
                _completion.ShowReverifyAllSkipped(reverify, deleting: true);
                OperationProgress = string.Empty;
                return;
            }

            if (result.Cancelled)
            {
                // Cancelled mid-batch: report the partial on the completion
                // overlay (no result-log entry for a cancelled run). Only raise
                // the overlay when something actually happened.
                await RefreshAfterBatchAsync();
                if (result.DeletedCount > 0 || result.Errors.Count > 0)
                {
                    var cancelledBytes = CompletedBytes(survivingFiles, result.DeletedCount, result.Errors);
                    _completion.ShowDeleteCancelledSummary(
                        result.DeletedCount, survivingFiles.Count, cancelledBytes, result.Errors, reverify);
                }
                OperationProgress = string.Empty;
                return;
            }

            var deletedCount = result.DeletedCount;
            var errorCount = result.Errors.Count;

            long deletedBytes;
            if (errorCount == 0)
                deletedBytes = survivingBytes;
            else
            {
                var errorPaths = new HashSet<string>(result.Errors.Select(e => e.FilePath), StringComparer.OrdinalIgnoreCase);
                deletedBytes = survivingFiles.Where(f => !errorPaths.Contains(f.FullPath)).Sum(f => f.SizeBytes);
            }

            // Same helper and the same reasoning as the Move path's own
            // post-batch refresh.
            await RefreshAfterBatchAsync();

            _completion.ShowDeleteSummary(deletedCount, deletedBytes, result.Errors, reverify);

            // Same lock-aware gate as MoveAllAsync: skip the write once the
            // result-log surface is closed for the rest of the session and
            // across future sessions.
            if (!_completion.IsResultLogLocked)
            {
                var entry = ResultLogEntry.ForDelete(
                    ctx.PreOpScan, ctx.PreOpDurationMs,
                    result, deletedBytes,
                    operationTimer.ElapsedMilliseconds,
                    reverify.Reasons);
                if (await _resultLogService.WriteAsync(entry).ConfigureAwait(true))
                    _completion.MarkResultLogReady();
            }
            OperationProgress = string.Empty;
            return;
        }
        catch (OperationCanceledException)
        {
            // A cancel before the worker starts deletes nothing; a mid-batch
            // cancel returns as result.Cancelled above and is reported on the
            // overlay there. Refresh the counts and clear: this is the path that
            // has to leave the window showing a true count, and it is bounded by
            // the same rescan every other path runs.
            await RefreshAfterBatchAsync();
            OperationProgress = string.Empty;
        }
        catch (Exception ex)
        {
            // How far an unforeseen failure got is by definition unknown, so the
            // counts on screen may be describing files that have gone. The two
            // arms above rescan for the same reason on strictly less: a cancel
            // stops at a file boundary, and this arm cannot say even that much.
            // Deletes are permanent, so a stale count here is the window
            // offering to act on files a second time.
            await RefreshAfterBatchAsync();
            // A dialog naming the exception type and the crash-log path, never
            // ex.Message (under elevation it can carry another user's profile
            // path) and never the body-row status, which trims at a width cap
            // and would cut off the log path, the one actionable thing in a
            // report.
            var crash = CrashLog.TryWrite(ex);
            var typeName = ex.GetType().Name;
            OperationProgress = string.Empty;
            _dialogService.ShowWarning(
                crash.Written
                    ? string.Format(Strings.Status_DeleteFailed, typeName, crash.Path)
                    : string.Format(Strings.Status_DeleteFailed_NoLog, typeName),
                Strings.Error_DeleteFailedTitle);
        }
        finally
        {
            // DisposeOperationCts also clears IsCancellationRequested.
            DisposeOperationCts();
            IsOperating = false;
            OperationProgressPercent = 0;
            // Stale-state reset: a cancel-then-rerun cycle would otherwise
            // briefly show the previous run's last filename and "X of Y"
            // line through the next operation's first progress callback.
            OperationCurrentFile = 0;
            OperationTotalFiles = 0;
            OperationCurrentFileName = string.Empty;
            OperationProgressAnnouncement = string.Empty;
            _lastAnnouncedDecile = -1;
            // Back to a measured bar: RefreshAfterBatchAsync sets this on
            // every path that stops a batch part-way, and the next operation
            // reports per-file progress.
            IsOperationProgressIndeterminate = false;
        }
    }

    /// <summary>
    /// Folds the paths an action service held back under the installer mutex into
    /// the pre-act re-verify's own account of what it kept back, so the user is
    /// shown one account of one batch.
    ///
    /// Which side of the mutex a file was condemned on is a fact about how the
    /// check is built rather than about what happened to the file, so the two
    /// producers earn no line of their own. The tallies are ADDED rather than
    /// merged, because the two hold back DIFFERENT files: the one number the user
    /// reads is every file the run held back, and anything that merged rather than
    /// added would under-count it.
    ///
    /// THAT ARGUMENT USED TO BE ABOUT CAUSES rather than about the total, adding
    /// keeping one line per cause carrying its own count. There is one line now and
    /// it names no cause. The per-cause counts survive underneath, for the opt-in
    /// result log, and adding is what keeps those right as well.
    ///
    /// The SURVIVING claims move with the paths, because those three collections
    /// describe one set of files and have to agree: a path leaving
    /// <c>Surviving</c> for <c>Dropped</c> takes every claim naming it, a patch
    /// held by several products carrying one per product. A claim left behind
    /// would make <c>SurvivingPatchClaims</c> name a file the same result has just
    /// declared held back. <see cref="PatchClaim.LocalPackagePath"/> is normalised
    /// the way the registered rows are, so the one case-insensitive set answers
    /// for both.
    ///
    /// The SIBLING claims do not, and the body says why. They are the other half
    /// of what the under-lease re-read is asked, they are keyed by product rather
    /// than by path, and this is the one place in the app that builds a
    /// <see cref="ReverifyResult"/> out of another one.
    /// </summary>
    internal static ReverifyResult FoldHeldBack(
        ReverifyResult reverify, IReadOnlyList<string> heldBack, HeldBackReasons reasons)
    {
        if (heldBack.Count == 0) return reverify;

        var condemned = new HashSet<string>(heldBack, StringComparer.OrdinalIgnoreCase);
        // A with-expression rather than a rebuild, and that is the whole of why
        // SiblingPatchClaims is still here. The positional form named four of the
        // record's five members, the fifth defaulted to empty, and the result was a
        // batch with no siblings: the exact pairing UnderLeaseClaims was created to
        // make unconstructible, and whose detector was removed on the ground that
        // it had become so. This form carries whatever the author did not name, so
        // a member added to ReverifyResult later arrives here without anybody
        // having to remember it. The fault stops being available rather than being
        // fixed once.
        //
        // AND THE SIBLING LIST PASSES THROUGH UNFILTERED, WHICH IS NOT AN OVERSIGHT
        // AND IS THE THING TO GET WRONG HERE. The surviving claims are keyed by
        // PATH, so they leave with their paths, which is what the filter below
        // does. The siblings are keyed by PRODUCT: the re-read walks them by
        // product code and condemns every batch path on a product one of them
        // answers badly for. Filtering them by condemned path would throw away the
        // row that answers for a product a surviving path is still registered to,
        // and so narrow the very check this is restoring.
        //
        // What unfiltered costs, weighed and accepted rather than unnoticed: a
        // folded result carries siblings for products no surviving path is left on,
        // so a re-read handed one would make keyed reads that can condemn nothing,
        // with the machine-wide installer lease held. It is bounded by the batch's
        // own products, the reads are keyed rather than an enumeration, and a
        // narrower list would have to be built by product and never by path. That
        // saving is not worth a second way of getting this wrong.
        return reverify with
        {
            Surviving = reverify.Surviving.Where(p => !condemned.Contains(p)).ToList().AsReadOnly(),
            Dropped = reverify.Dropped.Concat(heldBack).ToList().AsReadOnly(),
            Reasons = reverify.Reasons + reasons,
            SurvivingPatchClaims = reverify.SurvivingPatchClaims
                .Where(c => !condemned.Contains(c.LocalPackagePath)).ToList().AsReadOnly(),
        };
    }

    /// <summary>
    /// Removes a destination folder the pre-flight created for a Move that then
    /// did not go ahead. The rule, rather than a list that goes stale as paths
    /// are added: every abandon path on which NOTHING WAS PLACED in the folder
    /// calls this, both before the confirmation (probe cancel, probe write
    /// failure, free-space refusal, confirmation decline, act-time
    /// pending-reboot refusal) and after it (re-verify failure, either half of
    /// the re-verify keeping every candidate back, a cancel before the first
    /// file, the service refusing at the Global\_MSIExecute mutex, and the
    /// service's own write-probe failure). The last two are safe because the
    /// service refuses ahead of its CreateDestinationFolder and its per-file loop
    /// respectively.
    ///
    /// "Either half" is load-bearing and was once only the first half. The
    /// pre-lease pass keeping everything back never reaches the service at all;
    /// the under-lease re-read keeping everything back reaches it and comes
    /// straight back out, the service returning before its own
    /// CreateDestinationFolder precisely so that a batch it empties leaves no
    /// folder behind. That care is undone one layer up if this is not called,
    /// because the folder that survives is the PRE-FLIGHT's rather than the
    /// service's.
    ///
    /// The paths that deliberately do NOT call this are the ones where a file
    /// may already have arrived: Error_DestinationChangedMidBatch, thrown from
    /// inside the per-file loop, and the unaccounted-for catch that can fire at
    /// any point in the batch. Removing a folder there could take a moved file
    /// with it, which is the one outcome worse than a stray empty folder.
    ///
    /// One path does not call this and is not one of those, so the rule above
    /// does not reach it and reading from the rule alone would make it look like
    /// a defect: the destination-gate arm, where nothing was placed and the
    /// folder is still left alone. The arm itself carries the reason.
    ///
    /// The caller checks its createdDestination local first, so a folder that
    /// was already there is never a candidate.
    ///
    /// The non-recursive Delete IS the emptiness test, rather than an Exists or
    /// Any check followed by a delete: Win32 refuses to remove a directory that
    /// holds anything, so a file that arrived between the pre-flight and the
    /// cancel stops the removal at the syscall, with no window in which a check
    /// could pass and the delete then take something with it.
    ///
    /// Off the dispatcher for the same reason every other touch of the
    /// destination is (a stalled network path), and silent on failure because
    /// nothing depends on the folder being gone: it is tidiness, not a
    /// guarantee, and a dialog about a folder the user chose to abandon would
    /// be noise at the end of an operation they cancelled.
    /// </summary>
    private Task RemoveCreatedDestinationAsync(string destination) => Task.Run(() =>
    {
        try
        {
            _fs.Directory.Delete(destination);
        }
        catch (Exception)
        {
            // Not empty any more, gone already, or refused: all fine, all leave
            // the user exactly where a pre-2.1.0 cancel did.
        }
    });


    /// <summary>
    /// What one off-thread pass over a Move destination found: whether it is
    /// out of bounds, and, when it is not, what kind of volume it sits on and
    /// how much room is left there. <see cref="Kind"/> and
    /// <see cref="AvailableFreeSpace"/> are only meaningful when neither
    /// rejection flag is set, because the pass returns at the first gate that
    /// refuses and never touches the destination after that.
    ///
    /// Whether the pass created the destination folder is deliberately NOT on
    /// this record: the two arms that catch a cancel or a write failure part
    /// way through the probe need that answer and never receive a record at
    /// all. MoveAllAsync keeps it in a local the probe writes to instead.
    /// </summary>
    private sealed record DestinationPreFlight(
        bool InsideInstallerCache,
        bool InsideSystemFolder,
        string Kind,
        long? AvailableFreeSpace)
    {
        public static DestinationPreFlight Rejected(
            bool insideInstallerCache = false, bool insideSystemFolder = false) =>
            new(insideInstallerCache, insideSystemFolder, MoveDestinationKinds.Unknown, null);
    }

    /// <summary>
    /// Snapshot of the scan state and file list a delete pass needs, captured
    /// before the first delete so the permanent-delete retry and the
    /// result-log entry both read the pre-operation values (RefreshAsync
    /// replaces <see cref="ScanViewModel.LastScanResult"/> on success).
    /// </summary>
    private sealed record DeleteContext(
        IReadOnlyList<OrphanedFile> RemovableFiles,
        IReadOnlyList<string> FilePaths,
        int Count,
        ScanResult PreOpScan,
        long PreOpDurationMs);

    /// <summary>
    /// Bytes of the files a cancelled batch actually completed. The action
    /// services process their input in order and stop at the cancel point, so the
    /// files they reached are the first (<paramref name="completedCount"/> plus
    /// the errors); of those, the ones that did not error are what moved or
    /// deleted. The exact figure on a cancel rests on that ordered processing,
    /// which both <see cref="IMoveFilesService"/> and <see cref="IDeleteFilesService"/>
    /// do; the success path can lean on "no errors means every file completed"
    /// instead, which a cancel cannot.
    /// </summary>
    private static long CompletedBytes(IReadOnlyList<OrphanedFile> files, int completedCount,
        IReadOnlyList<FileOperationError> errors)
    {
        var reached = completedCount + errors.Count;
        if (errors.Count == 0)
            return files.Take(reached).Sum(f => f.SizeBytes);

        var errorPaths = new HashSet<string>(errors.Select(e => e.FilePath), StringComparer.OrdinalIgnoreCase);
        return files.Take(reached).Where(f => !errorPaths.Contains(f.FullPath)).Sum(f => f.SizeBytes);
    }

    /// <summary>
    /// Localised "{current} of {total} files" line shown beneath the
    /// progress bar in the operating overlay. Recomputed from
    /// CurrentFile/TotalFiles via the partial-changed hooks below so
    /// XAML can bind to a single property and never assemble the line
    /// from concatenated <c>&lt;Run&gt;</c> literals.
    /// </summary>
    public string OperationProgressDetail =>
        string.Format(Strings.Summary_OperationFiles,
            DisplayHelpers.FormatCount(OperationCurrentFile),
            DisplayHelpers.FormatCount(OperationTotalFiles),
            DisplayHelpers.PluraliseFile(OperationTotalFiles));

    partial void OnOperationCurrentFileChanged(int value) =>
        OnPropertyChanged(nameof(OperationProgressDetail));

    partial void OnOperationTotalFilesChanged(int value) =>
        OnPropertyChanged(nameof(OperationProgressDetail));

    /// <summary>
    /// Ends the current operation: cancel-then-null-then-dispose
    /// <see cref="_operationCts"/>, then clear <see cref="IsCancellationRequested"/>
    /// (that CTS's UI mirror) and <see cref="IsOperationInFlight"/>. Every exit
    /// path of a Move or a Delete calls this, including the pre-flight's early
    /// returns, so the operation's state is torn down in one place.
    ///
    /// <see cref="RefreshAfterBatchAsync"/> is the one caller that hands
    /// over a source of its own: it disposes the operation's and installs a
    /// fresh one for the rescan, then clears it again, so on every path that
    /// runs it this finds the field already null and the flags are all it
    /// clears.
    ///
    /// Order matters on two fronts: the null happens before Dispose so a
    /// concurrent CancelOperationCommand reading the field sees no CTS
    /// and no-ops instead of racing the dispose; the Cancel happens
    /// first so a still-running worker on the Dispose-during-shutdown
    /// path observes OperationCanceledException at its next
    /// ThrowIfCancellationRequested rather than ObjectDisposedException.
    /// Cancel on a completed CTS (the success-path callers below) is a
    /// no-op.
    ///
    /// The flag is cleared here rather than in each operation's finally
    /// because the Move pre-flight has four early returns that never reach
    /// one (cancelled probe, failed probe, not enough space, declined
    /// confirmation). A Cancel clicked during the probe left the flag set
    /// on those paths, and CanCancelOperation reads !IsCancellationRequested,
    /// so the Cancel button and Esc stayed dead for the whole of the next
    /// operation.
    /// </summary>
    private void DisposeOperationCts()
    {
        var cts = _operationCts;
        _operationCts = null;
        cts?.Cancel();
        cts?.Dispose();
        IsCancellationRequested = false;
        IsOperationInFlight = false;
    }

    /// <summary>
    /// The refresh every Move or Delete owes the window once its worker has
    /// returned, run under a FRESH cancellation source with the overlay's Cancel
    /// button live again.
    ///
    /// Every caller arrives with one fact and that fact is the whole contract:
    /// the worker has returned, so the counts on screen are a statement about a
    /// folder as it was before the batch. How the batch ended does not change
    /// that. A run that stopped part-way may be offering files it has already
    /// removed; a run that completed is describing a batch that is over, and a
    /// re-verify that kept everything back acted on nothing but has just learned
    /// the API disagrees with the scan. None of the three can leave the previous
    /// scan's numbers standing.
    ///
    /// The success path is the one that looks like it does not belong and is the
    /// reason this covers every path rather than the stopped ones. Its overlay
    /// read "Deleting..." over "71 of 71" while the delete was finished and a
    /// scan was running, which is not a lesser version of the problem below: it
    /// is a false sentence on the most travelled path in the app, and it went
    /// unremarked for as long as it did because it looks exactly like the pause
    /// at the end of an operation.
    ///
    /// It cannot reuse the operation's own token, and that is why arming the
    /// Cancel button is not simply a matter of passing the token the caller
    /// already holds. On the cancel paths it is cancelled by definition and would
    /// abandon the refresh at its first checkpoint, leaving the window showing
    /// counts from before the batch; on the unforeseen-failure path the caller's
    /// finally cancels it moments later, which does the same thing from mid-walk;
    /// and on a success path a Cancel pressed as the last file finished leaves a
    /// cancelled token behind a batch that reports itself complete. Nor can the
    /// refresh run without a token: this is a full folder walk plus a full API
    /// enumeration on a folder that can hold millions of files, and the user is
    /// watching an overlay that has stopped saying anything. Held behind
    /// a greyed Cancel button with nothing advancing, that wait is the shape
    /// people report as a hang.
    ///
    /// So the button is re-armed rather than left dead: clearing
    /// IsCancellationRequested is what re-enables it (IsOperating is still true
    /// here, and the overlay's own finally clears both). A second Cancel then
    /// stops the refresh too, and the counts stay stale until the next scan,
    /// which is what RefreshAsync already does on any other failure.
    ///
    /// The overlay's numbers go with the heading, and that is the point rather
    /// than tidiness: they belong to a batch that is over, the rescan reports no
    /// progress of its own, and left in place they read as a scan stalled at file
    /// 34 of 71 for as long as the walk takes, which on the folders this exists
    /// for is the whole point. A completed batch's "71 of 71" is the same claim
    /// with a rounder number on it. The caller's finally clears them too, and far
    /// too late: it runs after the refresh has finished.
    /// </summary>
    private async Task RefreshAfterBatchAsync()
    {
        OperationCurrentFile = 0;
        OperationTotalFiles = 0;
        OperationCurrentFileName = string.Empty;
        OperationProgressAnnouncement = string.Empty;
        _lastAnnouncedDecile = -1;
        OperationProgressPercent = 0;
        // An indeterminate bar over no count, matching the scanning overlay:
        // "0 of 0 files" against a 0% bar would swap one wrong statement for
        // another, both of them claims about progress nothing here can measure.
        IsOperationProgressIndeterminate = true;

        // The operation's own source is finished with, and this is the one path
        // that takes the field away from DisposeOperationCts before it runs.
        // Dropping it undisposed cost nothing at run time and made the teardown
        // invariant false, which the next person to reason from it would pay
        // for. No Cancel first: the worker has returned, so nothing holds the
        // token, and it is either cancelled already or about to be by the
        // caller's finally.
        var finished = _operationCts;
        _operationCts = null;
        finished?.Dispose();

        var cts = new CancellationTokenSource();
        _operationCts = cts;
        IsCancellationRequested = false;
        OperationProgress = Strings.Status_Scanning;
        try
        {
            await _scan.RefreshAsync(cts.Token);
        }
        finally
        {
            // Cleared here rather than left to the caller's finally so a second
            // Cancel arriving after the refresh returns cannot cancel a source
            // the next operation is about to install.
            if (ReferenceEquals(_operationCts, cts))
            {
                _operationCts = null;
                cts.Dispose();
            }
        }
    }

    private void OnOperationProgressUpdate(OperationProgress p)
    {
        OperationCurrentFile = p.CurrentFile;
        OperationTotalFiles = p.TotalFiles;
        OperationCurrentFileName = p.CurrentFileName;
        OperationProgressPercent = p.TotalFiles > 0
            ? (double)p.CurrentFile / p.TotalFiles * 100
            : 0;
        // Heading stays at the original "Moving N files..." / "Deleting
        // N files..." action verb for the operation's duration; the
        // DockPanel below the bar shows the live count via
        // OperationProgressDetail.

        var decile = p.TotalFiles > 0 ? p.CurrentFile * 10 / p.TotalFiles : 0;
        if (p.CurrentFile == 1 || p.CurrentFile == p.TotalFiles || decile != _lastAnnouncedDecile)
        {
            _lastAnnouncedDecile = decile;
            OperationProgressAnnouncement = OperationProgressDetail;
        }
    }

    /// <summary>
    /// Turns the diagnostic-log classification into the two things the
    /// completion card needs: whether to claim freed space, and whether to tell
    /// the user how to get it back. A UNC share counts as freeing space, the
    /// volume the cached files came off being genuinely emptier once they are on
    /// it; only an unreadable destination declines to say either way.
    ///
    /// THAT VOLUME IS THE SYSTEM DRIVE ON AN ORDINARY MACHINE AND IS NOT NAMED
    /// AS ONE HERE. The files come off whatever volume holds
    /// <c>C:\Windows\Installer</c>, and Windows lets a volume be mounted there.
    /// Nothing in the mapping below turns on which volume it is, so naming one
    /// bought nothing and was wrong on the machine where the two differ. The
    /// mapping itself is unchanged: what a same-volume move's bytes count as is
    /// settled and is not in question here.
    /// </summary>
    internal static MoveSpaceOutcome ClassifySpaceOutcome(string destinationKind) => destinationKind switch
    {
        MoveDestinationKinds.DifferentFixedDrive or
        MoveDestinationKinds.RemovableDrive or
        MoveDestinationKinds.UncShare => MoveSpaceOutcome.FreedSpace,
        MoveDestinationKinds.SameDrive => MoveSpaceOutcome.SameDrive,
        _ => MoveSpaceOutcome.Unclassified,
    };

    /// <summary>
    /// Classifies the move destination for the result log. Returns one of
    /// <see cref="MoveDestinationKinds"/>: <c>uncShare</c> for a share,
    /// <c>sameDrive</c> when the destination is on the volume the Installer
    /// cache is on, <c>removableDrive</c> or <c>differentFixedDrive</c> for
    /// another volume by what is mounted there, and <c>unknown</c> for anything
    /// that could not be established.
    /// </summary>
    /// <remarks>
    /// ASKED ABOUT THE VOLUME AND NOT THE LETTER, and it did ask about the
    /// letter until 3.0.0. It took Path.GetPathRoot and handed that to
    /// DriveInfo, so a destination under a volume mounted into a folder on C:
    /// was classified by whatever C: is: a removable disk mounted at
    /// <c>C:\Data</c> was logged as a fixed drive. No sentence the user reads
    /// changed, because ClassifySpaceOutcome maps fixed, removable and share
    /// alike to "space was freed". What was wrong was the field itself, in the
    /// result log and in the opt-in report, and a measurement that is quietly
    /// wrong is worse than one that is missing: it is the only instrument this
    /// project has for what happens on real machines, and it reads as an answer.
    ///
    /// DriveInfo cannot be asked the right question. It is built from a
    /// drive-letter root, and a mounted folder has no letter, so
    /// StorageHelpers.GetDriveKind takes the mount point instead.
    ///
    /// THREE VOLUME QUERIES RATHER THAN TWO, ON PURPOSE, AND IT WAS TWO RATHER
    /// THAN ONE BEFORE THE CACHE SIDE STARTED ASKING. The same-drive answer comes
    /// from MoveSpaceCheck so that this window and the command line cannot answer
    /// it differently, which is the reason that decision lives in Core at all.
    /// Two of the three are its own, one for the cache folder and one for the
    /// destination; threading the destination's mount point out of it to save
    /// the duplicate below would buy back one call per Move and put that back at
    /// risk. This runs once per operation, inside the pre-flight, off the
    /// dispatcher.
    /// </remarks>
    /// <param name="dest">The folder the move would write into.</param>
    /// <param name="resolveIsOnCacheVolume">
    /// The same-drive question, taken rather than reached for: the caller in this
    /// class hands over <see cref="_resolveIsOnCacheVolume"/>, so the label written
    /// to the result log and the answer behind the tooltip come from one place.
    /// Omitted, it is <see cref="AskWhetherOnCacheVolume"/>, which is what the app
    /// itself asks, so a call site that leaves it out gets the app's own answer.
    /// </param>
    internal static string ClassifyMoveDestination(
        string dest, Func<string, bool?>? resolveIsOnCacheVolume = null)
    {
        if (string.IsNullOrWhiteSpace(dest)) return MoveDestinationKinds.Unknown;

        // Answered from the spelling, before anything is asked of the network.
        // The same test MoveSpaceCheck uses, so the two cannot disagree about
        // what counts as a share.
        if (StorageHelpers.IsRemotePath(dest)) return MoveDestinationKinds.UncShare;

        // Three-state for the same reason the tooltip is: this value goes into
        // the result log and the opt-in report, and it is read as an answer. A
        // volume question Windows would not settle is unknown, not another
        // drive, and calling it another drive is what would let the completion
        // card head a move with a freed count it had not established.
        var onCacheVolume = (resolveIsOnCacheVolume ?? AskWhetherOnCacheVolume)(dest);
        if (onCacheVolume is null) return MoveDestinationKinds.Unknown;
        if (onCacheVolume.Value) return MoveDestinationKinds.SameDrive;

        try
        {
            var volume = StorageHelpers.GetVolumeMountPoint(Path.GetFullPath(dest));
            if (volume is null) return MoveDestinationKinds.Unknown;

            return StorageHelpers.GetDriveKind(volume) switch
            {
                DriveType.Fixed => MoveDestinationKinds.DifferentFixedDrive,
                DriveType.Removable => MoveDestinationKinds.RemovableDrive,
                // A mapped drive letter reaches here rather than the spelling
                // test above, its path being local in shape and remote in fact.
                DriveType.Network => MoveDestinationKinds.UncShare,
                _ => MoveDestinationKinds.Unknown,
            };
        }
        catch
        {
            return MoveDestinationKinds.Unknown;
        }
    }

    /// <summary>
    /// Maps a destination-write failure to a localised explanation.
    /// <paramref name="dest"/> is the user's own typed path so echoing
    /// it back is safe; <paramref name="ex"/>.Message is never routed
    /// through this method (path-leak risk under elevation).
    /// </summary>
    internal static string DescribeWriteFailure(string dest, Exception ex, string logPath, bool logWritten) => ex switch
    {
        UnauthorizedAccessException =>
            string.Format(Strings.Error_AccessDeniedDestination, dest),
        PathTooLongException =>
            string.Format(Strings.Error_PathTooLong, dest),
        DirectoryNotFoundException =>
            string.Format(Strings.Error_DestinationMissing, dest),
        IOException => logWritten
            ? string.Format(Strings.Error_IOWriteDestination, dest, logPath)
            : string.Format(Strings.Error_IOWriteDestination_NoLog, dest),
        _ => logWritten
            ? string.Format(Strings.Error_WriteDestination, dest, logPath)
            : string.Format(Strings.Error_WriteDestination_NoLog, dest),
    };
}
