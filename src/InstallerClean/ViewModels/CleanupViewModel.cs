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
    private readonly IDestinationGuard _destinationGuard;
    private readonly ScanViewModel _scan;
    private readonly CompletionViewModel _completion;
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
    /// <see cref="MoveDestinationClassifier.Classify"/> for the result-log label, and two
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
    private readonly Func<bool> _instanceLockLost;

    /// <summary>Read and written only through <see cref="OperationSource"/>.</summary>
    private CancellationTokenSource? _operationSource;
    private CancellationTokenSource? _moveDestinationSaveCts;
    private CancellationTokenSource? _destinationVolumeCts;
    private AppSettings _settings;

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
    internal static readonly TimeSpan MoveDestinationSaveDelay = Durations.FromMilliseconds(400);

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
    // execution gate, and IsMoveOrDeleteRunning is what holds the window's close.
    [ObservableProperty] private bool _isOperating;

    /// <summary>
    /// True from the first line of the Move or Delete command until its last, after
    /// every clean-up it owes and every dialog it shows. This is what holds the
    /// window's close: a close taken before it falls can end the process while the
    /// folder the Move's pre-flight made is still being removed, or while a
    /// confirmation is up.
    ///
    /// It is not <see cref="IsOperationInFlight"/>, which falls in
    /// <see cref="EndOperation"/> ahead of the clean-up on the pre-flight's early
    /// exits, nor <see cref="IsOperating"/>, which is false for the whole of the
    /// pre-flight before its overlay and for the confirmation. Move and Delete are
    /// out of use while it is true, so a second command cannot start inside the
    /// first one's clean-up and have this fall under it.
    /// </summary>
    public bool IsMoveOrDeleteRunning
    {
        get => _isMoveOrDeleteRunning;
        private set
        {
            if (!SetProperty(ref _isMoveOrDeleteRunning, value)) return;
            MoveAllCommand.NotifyCanExecuteChanged();
            DeleteAllCommand.NotifyCanExecuteChanged();
            BrowseDestinationCommand.NotifyCanExecuteChanged();
        }
    }
    private bool _isMoveOrDeleteRunning;

    /// <summary>
    /// True from the first line of a Move or a Delete until it has finished,
    /// pre-flight included, whether or not <see cref="IsOperating"/> ever
    /// raised the overlay. This is what the commands gate on, together with
    /// <see cref="IsMoveOrDeleteRunning"/>, which stays true through the Move's
    /// clean-up after <see cref="EndOperation"/> has cleared this.
    ///
    /// The Move pre-flight is the reason it exists. It hops off the dispatcher
    /// (a Win32 path resolve against a mapped drive can stall for the SMB
    /// timeout, and on the dispatcher that freezes the window), and while it is
    /// awaited the message loop pumps with no overlay up. Without this flag
    /// Delete is still clickable in that window, and a Delete started during a
    /// Move pre-flight would overwrite <see cref="OperationSource"/> while the
    /// Move was still using it, leaving the Move's Cancel button wired to the
    /// Delete's token.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MoveAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseDestinationCommand))]
    private bool _isOperationInFlight;

    /// <summary>
    /// True while Browse works out the folder its picker starts in, which can take up to
    /// <see cref="ConfirmationService.StartFolderBound"/> where the box names a network
    /// share that does not answer. Move, Delete and Scan are out of use while it is
    /// true, so none starts on a destination the picker is about to change.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MoveAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteAllCommand))]
    private bool _isChoosingDestination;
    private string _operationProgress = string.Empty;

    /// <summary>
    /// The heading on the Move or Delete card, which a screen reader reads out
    /// (<see cref="OperationProgressName"/>). Every write sets <see cref="WaitShown"/> to the
    /// wait whose line <see cref="WaitsInTheHeading"/> is writing, which is null for every
    /// other write, whether or not the heading changes. Where it changes, the button is set
    /// after the heading is stored and before the change is raised, so the heading, the
    /// button under it and the name a screen reader speaks change in one step.
    /// </summary>
    public string OperationProgress
    {
        get => _operationProgress;
        set
        {
            if (EqualityComparer<string>.Default.Equals(_operationProgress, value))
            {
                WaitShown = _waitTakingTheLine;
                return;
            }

            OnPropertyChanging();
            OnPropertyChanging(nameof(OperationProgressName));
            _operationProgress = value;
            WaitShown = _waitTakingTheLine;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OperationProgressName));
        }
    }

    [ObservableProperty] private int _operationCurrentFile;
    [ObservableProperty] private int _operationTotalFiles;
    [ObservableProperty] private string _operationCurrentFileName = string.Empty;
    [ObservableProperty] private double _operationProgressPercent;

    /// <summary>
    /// What a screen reader speaks for <see cref="OperationProgress"/>: the heading itself,
    /// and while it is a wait the card's button can stop, the heading followed by the key
    /// that presses the button and what that leaves alone
    /// (<see cref="DisplayHelpers.WaitingLineWithStopKey"/>).
    /// </summary>
    public string OperationProgressName =>
        WaitShown is null ? OperationProgress : DisplayHelpers.WaitingLineWithStopKey(OperationProgress);

    /// <summary>
    /// The wait <see cref="OperationProgress"/> names while it is a waiting line, and null
    /// for every other heading. Every write to the heading sets it: the write putting a
    /// wait's own line up sets it to that wait, and every other write sets it to null, the
    /// write putting back the heading the wait took among them, whatever any of them says.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStopWaiting))]
    [NotifyPropertyChangedFor(nameof(StopWaitingName))]
    [NotifyPropertyChangedFor(nameof(OperationProgressName))]
    [NotifyCanExecuteChangedFor(nameof(StopWaitingCommand))]
    private SourceFolderWait? _waitShown;

    /// <summary>The wait whose line <see cref="WaitsInTheHeading"/> is writing, and null at any other time.</summary>
    private SourceFolderWait? _waitTakingTheLine;

    /// <summary>Whether the card shows the button that stops the wait the heading names.</summary>
    public bool CanStopWaiting => WaitShown is not null;

    /// <summary>
    /// The name a screen reader speaks for that button, naming the drive or share
    /// (<see cref="DisplayHelpers.StopWaitingFor"/>), and empty while there is no button.
    /// </summary>
    public string StopWaitingName => WaitShown is { } wait ? DisplayHelpers.StopWaitingFor(wait.Root) : string.Empty;

    /// <summary>
    /// Gives up on the drive or share the heading names for the rest of the check made
    /// before the batch, or of the scan after it. The check or the scan ends the wait at
    /// once and tells its end, which puts the heading back and takes the button away.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStopWaiting))]
    private void StopWaiting() => WaitShown?.StopWaiting();

    /// <summary>
    /// True while the overlay is up over work that reports no per-file
    /// progress: the re-verify before a batch's first file, and the rescan
    /// after a batch. The bar runs
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
    /// file and each time the batch crosses a tenth, never per file. The
    /// window reads the line out each time it changes, and per-file changes
    /// would queue speech far faster than it can be spoken.
    /// </summary>
    [ObservableProperty] private string _operationProgressAnnouncement = string.Empty;

    /// <summary>Last tenth-of-batch boundary announced; -1 between operations.</summary>
    private int _lastAnnouncedDecile = -1;

    /// <summary>
    /// True from a Cancel press until the rescan after the batch starts or the
    /// operation ends. Takes the overlay's Cancel out of use
    /// (<see cref="CanCancelOperation"/>) and puts the tooltip on it that says a
    /// cancel is under way.
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
        IRemovableReverifier reverifier,
        IDestinationGuard destinationGuard,
        Func<string, bool?>? resolveIsOnCacheVolume = null,
        Func<bool>? instanceLockLost = null)
    {
        _moveService = moveService;
        _deleteService = deleteService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        _confirmationService = confirmationService;
        _fs = fileSystem;
        _destinationGuard = destinationGuard;
        _scan = scan;
        _completion = completion;
        _reverifier = reverifier;
        _resolveIsOnCacheVolume = resolveIsOnCacheVolume ?? MoveDestinationClassifier.AskWhetherOnCacheVolume;
        _instanceLockLost = instanceLockLost ?? (() => false);

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
            e.PropertyName == nameof(ScanViewModel.HasOrphans) ||
            e.PropertyName == nameof(ScanViewModel.HasPendingReboot))
        {
            MoveAllCommand.NotifyCanExecuteChanged();
            DeleteAllCommand.NotifyCanExecuteChanged();
            BrowseDestinationCommand.NotifyCanExecuteChanged();
        }
    }

    public void Dispose()
    {
        _scan.PropertyChanged -= _scanHandler;
        EndOperation();
        FlushMoveDestination();
    }

    /// <summary>
    /// Saves a move destination still waiting out its debounce, now, and stops the
    /// debounced save. Closing the app, the language relaunch and an update's hand-over
    /// included, reaches it through <see cref="Dispose"/>.
    /// </summary>
    public void FlushMoveDestination()
    {
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
    // IT READS AN ANSWER SOMEBODY ELSE FETCHED RATHER THAN ASKING THE QUESTION
    // HERE. This property is recomputed on every keystroke in the box
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
    // THE ANSWER CAN BE BRIEFLY OUT OF DATE, AND NOTHING ACTS ON IT. It is taken
    // once the box settles, so a mount point created or removed while nobody is
    // typing leaves the last answer standing. The confirmation dialog, the
    // free-space refusal and the completion line all re-ask inside the
    // pre-flight at the moment of action. Do not take the volume from the path
    // root instead: a path root cannot tell a mounted volume from a folder, so
    // that answer is wrong for any destination under a volume mounted into a
    // folder.
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
        // itself uses (MoveAllAsync takes MoveDestination.Trim()), and the tooltip
        // must answer for the string the button acts on.

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
            await FrameworkTask.Delay(MoveDestinationSaveDelay, token);
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
    /// The crash-log message written where the debounced save of the destination is refused.
    /// </summary>
    internal const string MoveDestinationSaveRefused =
        "Settings.Update returned false during MoveDestination debounced save.";

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
            await FrameworkTask.Delay(MoveDestinationSaveDelay, token);
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
        // cannot lose the language pick made on the dispatcher, or the report's
        // own saves, to a last-writer-wins rename.
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
                CrashLog.TryWrite(new InvalidOperationException(MoveDestinationSaveRefused));
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

    // Move and Delete are gated on HasPendingReboot, which is true whenever the
    // pending-reboot check blocks, whichever PendingRebootReason it names.
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
    // A disabled control is skipped by Tab entirely, which would take the
    // button, its spoken description and the tooltip explaining what it needs
    // out of reach without a mouse, the one route a screen-reader user does not
    // have.
    // HasOrphans rather than the count alone, because it is false while the scan
    // view model holds no result: after a scan that failed, or the scan after a
    // Move or Delete that did not finish, neither command runs, whatever the
    // layout shows.
    // The single-instance mutex is what keeps the window and a command-line Move or
    // Delete from working on the folder at once. An update that did not hand over
    // can find another process holding it, and until the app has it back neither
    // command runs; asking tries to take it back.
    private bool CanMove() =>
        !_scan.IsScanInFlight && !IsOperationInFlight && !IsMoveOrDeleteRunning
        && !IsChoosingDestination
        && !_scan.HasPendingReboot
        && _scan.HasOrphans
        && !_instanceLockLost();

    private bool CanDelete() =>
        !_scan.IsScanInFlight && !IsOperationInFlight && !IsMoveOrDeleteRunning
        && !IsChoosingDestination
        && !_scan.HasPendingReboot
        && _scan.HasOrphans
        && !_instanceLockLost();

    /// <summary>
    /// Asks Move and Delete again whether they can run, after an update ends without
    /// handing over: the app may not have the single-instance mutex back.
    /// </summary>
    public void NotifyInstanceLockChanged()
    {
        MoveAllCommand.NotifyCanExecuteChanged();
        DeleteAllCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Opens the folder picker at the folder the box names. The picker opens only where
    /// the box still holds, trimmed, the text it held when Browse was pressed: text typed
    /// while the start folder was being worked out stands, and no picker comes up over it.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanBrowse))]
    private async Task BrowseDestinationAsync()
    {
        var asked = MoveDestination?.Trim();
        IsChoosingDestination = true;
        string? chosen;
        try
        {
            chosen = await _confirmationService.AskForMoveDestinationAsync(
                MoveDestination, () => MoveDestination?.Trim() == asked);
        }
        finally
        {
            IsChoosingDestination = false;
        }
        if (chosen is not null) MoveDestination = chosen;
    }

    // A picker answer written while a Move or Delete runs would change the box under it.
    private bool CanBrowse() =>
        !_scan.IsScanInFlight && !IsOperationInFlight && !IsMoveOrDeleteRunning;

    /// <summary>
    /// The source a Cancel press cancels. A Move or Delete installs one as it starts
    /// and the rescan after its batch installs its own; each is let go of once nothing
    /// is left running on it, and the end of the operation takes off whatever is left.
    /// Each change of it tells the Cancel button, which asks
    /// <see cref="CanCancelOperation"/> again only when CanExecuteChanged is raised, so
    /// the button follows the source whatever else changes with it. Setting the value
    /// it already holds tells nobody.
    ///
    /// In the app it is set on the dispatcher only: the button answers
    /// CanExecuteChanged on the thread that raises it, and WPF refuses a control being
    /// changed from any other thread.
    ///
    /// Written by hand and private. The generated observable form is public, which
    /// would put the source within reach of every binding.
    /// </summary>
    private CancellationTokenSource? OperationSource
    {
        get => _operationSource;
        set
        {
            if (ReferenceEquals(_operationSource, value)) return;
            _operationSource = value;
            CancelOperationCommand.NotifyCanExecuteChanged();
        }
    }

    // Live while the operation holds a source to cancel: through the Move's
    // probe, the re-verify and the batch, and through the rescan after the
    // batch, which installs one of its own (RefreshAfterBatchAsync).
    // Everything else an operation does with the overlay up, a refusal's
    // dialog and gate re-check, a failure dialog or the wait for the report,
    // stops for nothing, so Cancel and Esc are out of use there.
    private bool CanCancelOperation() => IsOperating && !IsCancellationRequested && OperationSource is not null;

    [RelayCommand(CanExecute = nameof(CanCancelOperation))]
    private void CancelOperation()
    {
        IsCancellationRequested = true;
        // Every holder takes the source off before it disposes it, so in the app,
        // where all of them run on the dispatcher as this does, this finds a live
        // source or none. Where this read and a holder run on different threads,
        // the source can be disposed between the read and the Cancel, and the
        // catch keeps that from throwing.
        try { OperationSource?.Cancel(); }
        catch (ObjectDisposedException) { }
        // The move/delete loop only repaints OperationProgress on its
        // next iteration. Without a synchronous write the overlay holds
        // "Moving 23 of 100..." for one iteration past the click. The
        // operation loop then clears it, or replaces it with a partial-
        // progress line, once the loop observes the cancellation.
        OperationProgress = Strings.Status_Cancelling;
    }

    /// <summary>
    /// True once the window has been asked to close while a Move or Delete runs
    /// (<see cref="RequestClose"/>). The window holds that close and takes it when
    /// <see cref="IsMoveOrDeleteRunning"/> goes false. From then on the operation
    /// finishes without what only an open window needs: no rescan after the batch, a
    /// running one being cancelled, no wait for the start check, and no report box, the
    /// card it would sit on going with the window unseen. A Move or Delete not yet
    /// started when it is set does not start: the Move ends at its pre-flight, at its
    /// confirmation or at its last check, removing the folders the pre-flight made, and
    /// the Delete ends at its last check.
    /// </summary>
    public bool CloseRequested => _closeRequested.Task.IsCompleted;

    /// <summary>
    /// Completes as <see cref="RequestClose"/> is first called, so a wait can end on it.
    /// <see cref="CloseRequested"/> reads it.
    /// </summary>
    private readonly TaskCompletionSource _closeRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// The window's close, asked for while a Move or Delete runs: records it and
    /// cancels what the operation has running, as a press of Cancel does. That
    /// includes the Move's pre-flight before its overlay is up and the Move while its
    /// confirmation is open, which the button cannot reach, so the pre-flight stops at
    /// its next check and the confirmation, once answered, ends the Move. Where the
    /// user has already pressed Cancel, or what is running stops for nothing, the
    /// close cancels nothing and that step finishes. Called on the dispatcher, as
    /// Cancel is. A second call does nothing.
    /// </summary>
    public void RequestClose()
    {
        if (!_closeRequested.TrySetResult()) return;
        // Neither CancelOperation nor its command's Execute asks whether it may run, so
        // the test is made here, on the source and not on CanCancelOperation: the
        // overlay is down while the Move's pre-flight runs its first 200 ms and while
        // the confirmation is up, so the button is out of use with the source live. The
        // operation lets go of its source once nothing is left to stop on it, and the
        // heading the press writes is the one the pre-flight's reveal shows if it
        // raises the overlay while the close waits.
        if (OperationSource is not null && !IsCancellationRequested) CancelOperation();
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private Task MoveAllAsync() => RunHoldingTheClose(MoveAllCoreAsync);

    /// <summary>
    /// Runs a Move or a Delete with <see cref="IsMoveOrDeleteRunning"/> set from its
    /// first line to its last, so the window's close waits for every exit the command
    /// has, whichever one it takes.
    /// </summary>
    private async Task RunHoldingTheClose(Func<Task> command)
    {
        IsMoveOrDeleteRunning = true;
        try
        {
            await command();
        }
        finally
        {
            IsMoveOrDeleteRunning = false;
        }
    }

    private async Task MoveAllCoreAsync()
    {
        if (_scan.LastScanResult is null) return;

        // Move is offered without a destination typed, so this is where one
        // gets asked for. Cancelling the browser abandons the move and
        // touches nothing; the box is left as it was, empty.
        if (string.IsNullOrWhiteSpace(MoveDestination))
        {
            var chosen = await _confirmationService.AskForMoveDestinationAsync();
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
        // them. Every gate below goes through FullPaths.GetFullPath, which expands a
        // bare "backup" against the process CWD, so the pre-flight would create
        // that folder and write its probe file somewhere the user never named,
        // elevated, and only then would MoveFilesService refuse the batch, after
        // the confirmation dialog. The CWD is wherever the process happened to be
        // started from and nothing in the app constrains it, so the two
        // out-of-bounds gates below catching such a path is luck, not a gate.
        // The same checks in the same order as the CLI's
        // ResolveAndValidateMoveDestination, with the length check first in the
        // pass below, where the scan's files are known; MoveFilesService keeps
        // its own copy at the service boundary for callers that never come
        // through here.
        if (!PathText.IsPathFullyQualified(dest))
        {
            _dialogService.ShowWarning(
                string.Format(Strings.Error_DestinationNotFullyQualified, dest),
                Strings.Error_InvalidDestinationTitle);
            return;
        }

        // A close asked for while the destination browser or the warning above was
        // up starts nothing: the pre-flight creates a folder.
        if (CloseRequested) return;

        // Every touch of the destination happens here, on a worker thread and
        // under one cancellable task: the two path gates, the folder creation
        // and write probe, the drive classification and the free-space query.
        // Each of them resolves or queries a path through Win32, so each can
        // stall for the SMB timeout on a mapped drive or a UNC share that has
        // gone away, and any of them on the dispatcher freezes the window.
        // The operation's source is installed first so the overlay's Cancel
        // button can interrupt the wait. The folders are created through the
        // injected destination guard, as is the write probe, with IFileSystem
        // making whatever the guard leaves, so a test that substitutes both
        // creates and probes nothing on the real disk. The gates and the
        // free-space query read the real disk, so a mock cannot talk its way
        // past a safety check.
        //
        // Nothing here touches view-model state: the verdict comes back as a
        // record and is applied below, on the dispatcher.
        OperationSource = new CancellationTokenSource();
        var probeToken = OperationSource.Token;
        IsOperationInFlight = true;
        // Captured rather than carried back on DestinationPreFlight, because
        // the two catch arms below need it just as much as the returns do and
        // neither of them ever gets a record: a cancel or a write failure part
        // way through the probe abandons the move with the folders already
        // created. The write happens before the first await, and the await is
        // the barrier, so the read after it sees it.
        var createdFolders = new List<string>();
        var batchPaths = preOpScan.RemovableFiles.Select(f => f.FullPath).ToList();
        var probeTask = Task.Run(() =>
        {
            // A destination where a moved file's path would reach MAX_PATH is refused
            // first, before anything reads or creates it. The batch measured is every
            // file the scan offered, as the command line measures it, which holds every
            // file the Move can be handed.
            if (MoveDestinationLength.TooLong(dest, batchPaths))
                return DestinationPreFlight.Rejected(pathTooLong: true);

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

            // The create makes the destination and every missing folder
            // above it, and a move that then never happens should leave none of
            // them behind; a folder that was already there is the user's and is
            // never touched. The create says which folders it made, and the list
            // holds them when a later create throws.
            CreatedFolders.Create(_fs, _destinationGuard, dest, createdFolders);
            probeToken.ThrowIfCancellationRequested();
            // The guard writes a write-test file through a handle opened on the
            // folder, opens it again to read it, and removes it, so a folder
            // another account can write cannot steer the test to a file in
            // another folder, and it leaves nothing behind. A folder that will not
            // take the file, or takes it and will not let it be read back, is not
            // writable for a Move, and the throw here refuses the destination. Where
            // Windows refused access to adding the file or to reading it back, the
            // throw carries the message for that refusal, shown as it stands by the
            // arm below; any other refusal is a write failure.
            if (!_destinationGuard.TryProbeWritable(dest, out var failureCode, out var refusal))
            {
                var cause = new System.ComponentModel.Win32Exception(failureCode);
                throw refusal switch
                {
                    WriteProbeRefusal.ReadBackRefusedAccess => new LocalisedAccessException(
                        string.Format(Strings.Error_FolderWillNotGiveFilesBack, dest), cause),
                    WriteProbeRefusal.CreateRefusedAccess => new LocalisedAccessException(
                        string.Format(Strings.Error_AccessDeniedDestination, dest), cause),
                    _ => new IOException(string.Format(Strings.Error_CannotWriteFolder, dest), cause),
                };
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
                MoveDestinationClassifier.Classify(dest, _resolveIsOnCacheVolume),
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
            // start announced exactly once if the overlay does appear. The delay
            // is not tied to the probe's token, so a close or Cancel that lands
            // inside the 200 ms does not raise the overlay early: it appears, with
            // the cancelling heading, only if the probe is still running when the
            // 200 ms is up.
            if (await Task.WhenAny(probeTask, Task.Delay(200)) != probeTask)
            {
                OperationProgress = IsCancellationRequested
                    ? Strings.Status_Cancelling
                    : Strings.Status_PreparingDestination;
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
            EndOperation();
            await RemoveCreatedFoldersAsync(createdFolders);
            return;
        }
        catch (LocalisedAccessException ex)
        {
            // A refusal of access to the write test adding its file or reading it
            // back. Any other failure arrives at the arm below. Its message is a resx
            // string naming the folder the user typed, so it is shown as it stands,
            // and it is a refusal rather than a fault, so no crash log is written.
            IsOperating = false;
            OperationProgress = string.Empty;
            EndOperation();
            await RemoveCreatedFoldersAsync(createdFolders);
            _dialogService.ShowWarning(ex.Message, Strings.Error_DestinationWriteFailedTitle);
            return;
        }
        catch (Exception ex)
        {
            IsOperating = false;
            OperationProgress = string.Empty;
            // ex.Message stays out of the dialog: path-leak risk under elevation.
            var message = DescribeWriteFailure(dest, ex, () => CrashLog.TryWrite(ex));
            EndOperation();
            await RemoveCreatedFoldersAsync(createdFolders);
            _dialogService.ShowWarning(message, Strings.Error_DestinationWriteFailedTitle);
            return;
        }
        IsOperating = false;
        OperationProgress = string.Empty;

        if (preFlight.PathTooLong)
        {
            EndOperation();
            _dialogService.ShowWarning(
                string.Format(Strings.Error_PathTooLong, dest),
                Strings.Error_InvalidDestinationTitle);
            return;
        }

        if (preFlight.InsideInstallerCache)
        {
            EndOperation();
            _dialogService.ShowWarning(
                Strings.Error_DestinationInsideInstaller,
                Strings.Error_InvalidDestinationTitle);
            return;
        }

        if (preFlight.InsideSystemFolder)
        {
            EndOperation();
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
        // THAT IS TWO VOLUME QUERIES, ONE FOR EACH PATH, AND THE ARGUMENT BELOW
        // COVERS BOTH. The cache side is resolved on every call and must not be
        // kept for the life of the process: it asks about the installer cache,
        // which is a volume of its own wherever one is mounted at
        // C:\Windows\Installer, and a volume mounted there can appear or vanish
        // while the app is open. So the cache side has to earn its place on this
        // thread the way the destination side does.
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
            // The Move goes no further, so the operation ends here.
            EndOperation();
            OperationProgress = string.Empty;
            await RemoveCreatedFoldersAsync(createdFolders);
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
                destinationKind == MoveDestinationKinds.SameDrive) || CloseRequested)
        {
            // User cancelled at the confirmation dialog, or the window was asked to
            // close while it was up, so the operation ends here.
            EndOperation();
            OperationProgress = string.Empty;
            await RemoveCreatedFoldersAsync(createdFolders);
            return;
        }

        // Re-check the pending-reboot gate at the moment of action, after the
        // confirmation and immediately before acting. The scan sampled it once; a
        // user who left the confirmation dialog open while a Windows Installer
        // transaction started would otherwise move files against a live install,
        // the one condition the gate exists to block. Blocked => the banner paints
        // via HasPendingReboot and both commands drop out; refuse without calling
        // the service.
        //
        // Check() throws when a read of Windows Installer's in-progress file fails
        // in a way none of its readings names. Nothing has been touched at this
        // point, so the throw ends here in the same dialog as the unforeseen-failure
        // arm below. The counts on screen still describe the folder, so there is
        // no rescan, and the folders the pre-flight made are removed as they are when
        // the gate blocks.
        bool blocked;
        try
        {
            blocked = await _scan.RecheckPendingRebootAsync();
        }
        catch (Exception ex)
        {
            EndOperation();
            OperationProgress = string.Empty;
            await RemoveCreatedFoldersAsync(createdFolders);
            ShowActionFailed(ex, deleting: false);
            return;
        }
        // A close asked for during the confirmation or this check ends the Move
        // here, as a declined confirmation does: nothing has been placed.
        if (blocked || CloseRequested)
        {
            EndOperation();
            OperationProgress = string.Empty;
            await RemoveCreatedFoldersAsync(createdFolders);
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
            // Re-verify the removable set immediately before acting, behind the
            // overlay: the Windows Installer records are enumerated again, and
            // every file no record names is judged again the way the scan judged
            // it (see IRemovableReverifier). It can fail; a failure must STOP
            // the batch, never act on an un-verified set, so it is surfaced through
            // the scan's own error ladder. A cancellation propagates to the outer
            // OCE catch.
            //
            // A moving bar over no count from here until the batch reports its first
            // file, as on the rescan after a batch: the re-verify reads the records again
            // and re-checks the files, and the service then takes the installer lock and
            // re-reads the batch's claims under it, and nothing in that stretch is a
            // count of the batch. OnOperationProgressUpdate brings the measured bar back.
            IsOperationProgressIndeterminate = true;
            ReverifyResult reverify;
            try
            {
                reverify = await _reverifier.ReverifyAsync(filePaths, OperationSource!.Token, WaitsInTheHeading());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Nothing is left running on the operation's source, so it goes
                // before the clean-up and the dialog.
                ReleaseOperationSource();
                var failure = _scan.DescribeScanFailure(ex);
                OperationProgress = string.Empty;
                // The re-verify runs before the batch, so nothing was placed.
                await RemoveCreatedFoldersAsync(createdFolders);
                if (failure.IsError)
                    _dialogService.ShowError(failure.Message, failure.Title);
                else
                    _dialogService.ShowWarning(failure.Message, failure.Title);
                return;
            }

            // A Windows Installer transaction that started after the gate re-check above
            // and was under way when the re-verify read the in-progress signals stops the
            // whole batch, as that gate stops it: the banner paints and both commands drop
            // out through HasPendingReboot. The re-verify itself holds back only the files no
            // record names on such a read, and a superseded patch in the same batch is
            // stopped here. It comes ahead of the all-held arm below so a batch the
            // re-verify emptied ends here too, under the banner rather than the held-back
            // card. Nothing has been touched, so there is no rescan; the finally below ends
            // the operation and takes the overlay down. Nothing is left running on the
            // operation's source, so it goes first, and Cancel is out of use while the
            // folder the pre-flight made is removed.
            if (reverify.InstallerActivity is { IsBlocked: true } installerActivity)
            {
                ReleaseOperationSource();
                _scan.PendingRebootResult = installerActivity;
                OperationProgress = string.Empty;
                await RemoveCreatedFoldersAsync(createdFolders);
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
                await RemoveCreatedFoldersAsync(createdFolders);
                return;
            }

            var survivingPaths = survivingFiles.Select(f => f.FullPath).ToList();
            var survivingBytes = survivingFiles.Sum(f => f.SizeBytes);

            // The source installed for the pre-flight above runs on through the
            // move, so a single Cancel covers the pre-flight, the re-verify and the
            // move loop.
            var progress = new Progress<OperationProgress>(OnOperationProgressUpdate);
            MoveResult result;
            try
            {
                try
                {
                    result = await _moveService.MoveFilesAsync(survivingPaths, dest,
                        UnderLeaseClaims.From(reverify), progress, OperationSource!.Token);
                }
                finally
                {
                    // The batch is the last step on the operation's source, so it
                    // goes here, ahead of every arm below: the arms that stop for
                    // nothing have no Cancel. On the arms that rescan, this and the
                    // rescan's install run in one dispatcher pass, so the button
                    // is live again before WPF re-checks the keyboard focus, and
                    // keeps it.
                    ReleaseOperationSource();
                }
            }
            catch (MoveAbortedException ex)
            {
                // The destination could no longer be confirmed part-way through,
                // so the service stopped and handed back what it had done. Caught
                // here rather than at the method's own arms so the surviving list
                // and the destination kind are still in scope to report with; the
                // warning over the summary carries the reason. No result-log entry,
                // for the reason recorded on the cancel arm below, and files that
                // moved make this the PC's first run all the same.
                await RefreshAfterBatchAsync();
                if (ex.Partial.MovedCount > 0) _completion.RecordFirstRun();
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
                //
                // The heading goes first: nothing it names runs behind the dialog.
                OperationProgress = string.Empty;
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
                        reverify.WithHeldBack(ex.Partial.HeldBack, ex.Partial.HeldBackReasons));
                }
                return;
            }

            if (result.InstallerBusy)
            {
                // A Windows Installer transaction took Global\_MSIExecute after the
                // act-time gate re-check above passed and before the service
                // acquired it, so the service refused and touched nothing. Re-run
                // the gate to paint the banner, and report no completed operation.
                // The heading goes first on this arm and the two after it, which end
                // in the re-check or a dialog with nothing the heading names running.
                OperationProgress = string.Empty;
                await RecheckAfterLockRefusalAsync(deleting: false);
                // The service refused at the mutex, before its own
                // CreateDestinationFolder, so nothing was placed.
                await RemoveCreatedFoldersAsync(createdFolders);
                return;
            }

            if (result.InstallerLockUnavailable)
            {
                // The service could not take Global\_MSIExecute and nothing was
                // shown to be holding it, so it refused and touched nothing. No
                // gate re-check here, unlike the arm above, and that is the whole
                // reason this is a separate flag rather than a second cause behind
                // the same one: the gate has no account of this condition, its
                // probe reading such a failure as not held, so a re-check would
                // come back clean and leave a refusal with no reason on screen. A
                // dialog carries the reason instead. The service's own acquire has
                // the detail.
                //
                // The destination cleanup matches the arm above, and for the same
                // reason: the service refuses ahead of its own
                // CreateDestinationFolder, so nothing it did needs undoing, and
                // what does is the folder THIS pre-flight made, which would
                // otherwise be left empty behind a batch that never ran.
                OperationProgress = string.Empty;
                _dialogService.ShowWarning(
                    Strings.Error_MoveInstallerLockUnavailable,
                    Strings.Error_MoveInstallerLockUnavailableTitle);
                await RemoveCreatedFoldersAsync(createdFolders);
                return;
            }

            if (result.InstallerLockAccessRefused)
            {
                // The object's security refused the service the rights to open it,
                // so it never learned whether a transaction held it and refused
                // without touching anything. A different sentence from the arm
                // above: the app was not allowed to look, which is not the same as
                // finding the lock busy, and only one of those two is something an
                // administrator can go and change.
                //
                // The dialog says what happened to this click, and the gate is then
                // re-run as the busy arm runs it, because the gate has a reason for
                // this condition and paints a banner of its own for it. A refusal
                // that stands leaves the window saying so with both commands out,
                // rather than offering buttons that will refuse the next click the
                // same way. A refusal that has already gone leaves the window as it
                // was, which is the state the machine is now in.
                OperationProgress = string.Empty;
                _dialogService.ShowWarning(
                    Strings.Error_MoveInstallerLockAccessRefused,
                    Strings.Error_MoveInstallerLockUnavailableTitle);
                await RecheckAfterLockRefusalAsync(deleting: false);
                await RemoveCreatedFoldersAsync(createdFolders);
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
                reverify = reverify.WithHeldBack(result.HeldBack, result.HeldBackReasons);
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
                await RemoveCreatedFoldersAsync(createdFolders);
                return;
            }

            if (result.Cancelled)
            {
                // Cancelled mid-batch: the service returned what completed rather
                // than throwing the tally away. Report the partial on the
                // completion overlay, and write no result-log entry for a
                // cancelled run, which keeps the public reports stats meaning
                // what they mean. Files that did move make this the PC's first run
                // all the same. Only raise the overlay when something actually
                // moved or errored; a cancel that reached no file just clears.
                await RefreshAfterBatchAsync();
                if (result.MovedCount > 0) _completion.RecordFirstRun();
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

            // Re-scans through the helper the other ends of a batch use. The
            // summary card below is filled from the batch's own figures, not the
            // scan's.
            await RefreshAfterBatchAsync();

            // The box goes on before the card is revealed, and the report is written
            // once the card is up, on the PC's first run only. The heading goes
            // before the wait for the start check, as nothing it names is still
            // running.
            OperationProgress = string.Empty;
            var carriesReport = await TakeReportForCardAsync(movedCount);
            _completion.ShowMoveSummary(movedCount, movedBytes, movedDest, result.Errors,
                ClassifySpaceOutcome(destinationKind), reverify);

            if (carriesReport)
            {
                var entry = ResultLogEntry.ForMove(
                    preOpScan, preOpDurationMs,
                    result, movedBytes,
                    operationTimer.ElapsedMilliseconds,
                    destinationKind,
                    // The folded result, so the under-lease re-read's own kept-back
                    // files are counted with the pre-act ones rather than lost:
                    // the two producers keep back DIFFERENT files and the report
                    // owes an account of both. The drives and shares given up and the
                    // waits are the pre-act check's, which the fold carries through.
                    reverify,
                    _completion.ReportRegion,
                    _completion.ReportHost, _completion.ReportCopy);
                await _completion.WriteReportAsync(entry);
            }
            // Completion overlay carries the user-facing summary; the
            // bottom-row pill stays blank once the overlay dismisses.
            OperationProgress = string.Empty;
        }
        catch (OperationCanceledException)
        {
            // A cancel before the worker starts (the token was cancelled between
            // the confirmation and Task.Run) moves nothing; a mid-batch cancel
            // comes back as result.Cancelled above rather than as a throw, and is
            // reported on the overlay there. Nothing reached a file here, so just
            // refresh the counts and clear.
            await RefreshAfterBatchAsync();
            OperationProgress = string.Empty;
            await RemoveCreatedFoldersAsync(createdFolders);
        }
        catch (LocalisedInvalidOperationException ex)
        {
            // Everything reaching here is one of MoveFilesService's destination
            // gates, all of which run before the per-file loop, so no file has
            // moved, the counts on screen are still right, and a rescan would be
            // a full folder walk and API enumeration for nothing. A batch the
            // destination guard stops part-way throws MoveAbortedException
            // instead, caught at the call site where the batch's own tally is
            // still in scope.
            //
            // No RemoveCreatedFoldersAsync either: the gates that reach this
            // arm fire precisely because the destination has just been shown to
            // resolve into C:\Windows\Installer or a system folder, and deleting
            // a directory at a path just proven to land somewhere unexpected is
            // the operation those guards exist to prevent. The service's
            // fully-qualified check is not among them: the window refuses a
            // relative destination above, before the batch is handed over.
            OperationProgress = string.Empty;
            _dialogService.ShowWarning(ex.Message, Strings.Error_InvalidDestinationTitle);
        }
        catch (LocalisedAccessException ex)
        {
            // No refresh here, and that asymmetry is deliberate: every
            // LocalisedAccessException comes from CreateDestinationFolder or
            // ProbeDestinationWriteable, both of which run before the per-file
            // loop starts, so no file has moved and the counts on screen are
            // still correct. That same ordering is why the created folders can
            // be removed here: nothing was placed in them.
            OperationProgress = string.Empty;
            await RemoveCreatedFoldersAsync(createdFolders);
            _dialogService.ShowWarning(ex.Message, Strings.Error_DestinationWriteFailedTitle);
        }
        catch (Exception ex)
        {
            // How far an unforeseen failure got is by definition unknown, so the
            // counts on screen may be describing files that have moved. The two
            // localised arms above are certain nothing had, their exceptions all
            // coming from gates that run ahead of the per-file loop, which is why
            // only they can skip the rescan. The delete path's twin of this arm
            // is certain of just as little, and rescans on the same reasoning.
            // For the same reason the run counts as the PC's first: files may
            // have moved.
            _completion.RecordFirstRun();
            await RefreshAfterBatchAsync();
            // Reaching here is rare: the move service collects per-file errors
            // rather than throwing.
            OperationProgress = string.Empty;
            ShowActionFailed(ex, deleting: false);
        }
        finally
        {
            // EndOperation also clears IsCancellationRequested.
            EndOperation();
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
            // Back to a measured bar for the next operation, which reports
            // per-file progress. The re-verify sets this and it stays set until
            // the batch reports a file, so a re-verify that fails, or a batch
            // that stops before its first file, returns with it set, and
            // RefreshAfterBatchAsync sets it on every path that rescans. This
            // reset clears it on each of them.
            IsOperationProgressIndeterminate = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private Task DeleteAllAsync() => RunHoldingTheClose(DeleteAllCoreAsync);

    private async Task DeleteAllCoreAsync()
    {
        if (_scan.LastScanResult is null) return;

        var removableFiles = _scan.LastScanResult.RemovableFiles;
        var count = removableFiles.Count;
        var sizeDisplay = _scan.OrphanedSizeDisplay;

        if (!_confirmationService.ConfirmDelete(count, sizeDisplay) || CloseRequested) return;

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
        OperationSource = new CancellationTokenSource();
        IsOperationInFlight = true;

        // Re-check the pending-reboot gate at the moment of action: a
        // transaction can start while the user is reading the confirmation
        // dialog. Blocked => paint the banner and refuse. A throw from the check
        // touches nothing and ends in the failure dialog without a rescan, as on
        // the Move path.
        bool blocked;
        try
        {
            blocked = await _scan.RecheckPendingRebootAsync();
        }
        catch (Exception ex)
        {
            EndOperation();
            OperationProgress = string.Empty;
            ShowActionFailed(ex, deleting: true);
            return;
        }
        // A close asked for during this check ends the Delete here, with nothing
        // touched.
        if (blocked || CloseRequested)
        {
            EndOperation();
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
            // Re-verify the removable set immediately before acting, as the Move
            // path does, so nothing the check no longer confirms is deleted. A
            // failure STOPS the batch (surfaced through the scan's error ladder);
            // a cancellation propagates to the outer OCE catch. The bar moves over no
            // count from here until the batch reports its first file, as on the Move
            // path.
            IsOperationProgressIndeterminate = true;
            ReverifyResult reverify;
            try
            {
                reverify = await _reverifier.ReverifyAsync(ctx.FilePaths, OperationSource.Token, WaitsInTheHeading());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // As on the Move path, the source goes before the dialog.
                ReleaseOperationSource();
                var failure = _scan.DescribeScanFailure(ex);
                OperationProgress = string.Empty;
                if (failure.IsError)
                    _dialogService.ShowError(failure.Message, failure.Title);
                else
                    _dialogService.ShowWarning(failure.Message, failure.Title);
                return;
            }

            // A blocked read of the in-progress signals at the re-verify stops the whole
            // batch, ahead of the all-held arm, as on the Move path, its source going first.
            if (reverify.InstallerActivity is { IsBlocked: true } installerActivity)
            {
                ReleaseOperationSource();
                _scan.PendingRebootResult = installerActivity;
                OperationProgress = string.Empty;
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
            DeleteResult result;
            try
            {
                result = await _deleteService.DeleteFilesAsync(
                    survivingPaths, UnderLeaseClaims.From(reverify), progress, OperationSource.Token);
            }
            finally
            {
                // The last step on the operation's source, released as on the
                // Move path.
                ReleaseOperationSource();
            }

            if (result.InstallerBusy)
            {
                // A Windows Installer transaction grabbed Global\_MSIExecute after
                // the act-time gate re-check above passed, so the service refused
                // and touched nothing. Re-run the gate to paint the banner and
                // report no completed operation. The heading goes first here and on
                // the two arms after it, as on the Move path.
                OperationProgress = string.Empty;
                await RecheckAfterLockRefusalAsync(deleting: true);
                return;
            }

            if (result.InstallerLockUnavailable)
            {
                // The service could not take Global\_MSIExecute and nothing was
                // shown to be holding it, so it refused and touched nothing. No
                // gate re-check here, unlike the arm above, and that is the whole
                // reason this is a separate flag rather than a second cause behind
                // the same one: the gate has no account of this condition, its
                // probe reading such a failure as not held, so a re-check would
                // come back clean and leave a refusal with no reason on screen. A
                // dialog carries the reason instead. The service's own acquire has
                // the detail.
                OperationProgress = string.Empty;
                _dialogService.ShowWarning(
                    Strings.Error_InstallerLockUnavailable, Strings.Error_InstallerLockUnavailableTitle);
                return;
            }

            if (result.InstallerLockAccessRefused)
            {
                // The object's security refused the service the rights to open it,
                // so it never learned whether a transaction held it and refused
                // without touching anything. A different sentence from the arm
                // above: the app was not allowed to look, which is not the same as
                // finding the lock busy, and only one of those two is something an
                // administrator can go and change.
                //
                // The dialog says what happened to this click, and the gate is then
                // re-run as the busy arm runs it, because the gate has a reason for
                // this condition and paints a banner of its own for it. A refusal
                // that stands leaves the window saying so with both commands out,
                // rather than offering buttons that will refuse the next click the
                // same way. A refusal that has already gone leaves the window as it
                // was, which is the state the machine is now in.
                OperationProgress = string.Empty;
                _dialogService.ShowWarning(
                    Strings.Error_InstallerLockAccessRefused, Strings.Error_InstallerLockUnavailableTitle);
                await RecheckAfterLockRefusalAsync(deleting: true);
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
                reverify = reverify.WithHeldBack(result.HeldBack, result.HeldBackReasons);
            }

            if (result.HeldBack.Count > 0 && survivingFiles.Count == 0)
            {
                // The under-lease re-read took the WHOLE batch back, so this is
                // the arm above having emptied it rather than a batch that ran.
                // One condition owes one screen and one record, and the pre-lease
                // twin already decided what both are, so this takes them: the
                // all-skipped overlay, and no result-log entry.
                //
                // It returns here rather than falling through: the heading
                // suppression below turns on errors and there are none here, so the
                // summary below would report a completed delete of zero files, and
                // the result log would take a bytesFreed of zero as a run that freed
                // nothing rather than a run that never was.
                await RefreshAfterBatchAsync();
                _completion.ShowReverifyAllSkipped(reverify, deleting: true);
                OperationProgress = string.Empty;
                return;
            }

            if (result.Cancelled)
            {
                // Cancelled mid-batch: report the partial on the completion
                // overlay (no result-log entry for a cancelled run, which counts as
                // the PC's first run where files went). Only raise the overlay when
                // something actually happened.
                await RefreshAfterBatchAsync();
                if (result.DeletedCount > 0) _completion.RecordFirstRun();
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

            // Re-scans through the helper the other ends of a batch use. The
            // summary card below is filled from the batch's own figures, not the
            // scan's.
            await RefreshAfterBatchAsync();

            // As on the Move path: the heading off, the box before the card, the
            // report after it.
            OperationProgress = string.Empty;
            var carriesReport = await TakeReportForCardAsync(deletedCount);
            _completion.ShowDeleteSummary(deletedCount, deletedBytes, result.Errors, reverify);

            if (carriesReport)
            {
                var entry = ResultLogEntry.ForDelete(
                    ctx.PreOpScan, ctx.PreOpDurationMs,
                    result, deletedBytes,
                    operationTimer.ElapsedMilliseconds,
                    // Folded, as on the Move path.
                    reverify,
                    _completion.ReportRegion,
                    _completion.ReportHost, _completion.ReportCopy);
                await _completion.WriteReportAsync(entry);
            }
            OperationProgress = string.Empty;
            return;
        }
        catch (OperationCanceledException)
        {
            // A cancel before the worker starts deletes nothing; a mid-batch
            // cancel returns as result.Cancelled above and is reported on the
            // overlay there. Refresh the counts and clear: this is the path that
            // has to leave the window showing a true count or none, and it is
            // bounded by the same rescan every other path runs, which takes the
            // list off the window where it does not finish.
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
            // offering to act on files a second time. Files may have gone, so the
            // run counts as the PC's first.
            _completion.RecordFirstRun();
            await RefreshAfterBatchAsync();
            OperationProgress = string.Empty;
            ShowActionFailed(ex, deleting: true);
        }
        finally
        {
            // EndOperation also clears IsCancellationRequested.
            EndOperation();
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
            // Back to a measured bar for the next operation, which reports
            // per-file progress. The re-verify sets this and it stays set until
            // the batch reports a file, so a re-verify that fails, or a batch
            // that stops before its first file, returns with it set, and
            // RefreshAfterBatchAsync sets it on every path that rescans. This
            // reset clears it on each of them.
            IsOperationProgressIndeterminate = false;
        }
    }

    /// <summary>
    /// Whether the card a finished Move or Delete is about to reveal carries the
    /// report, taking it where it does (<see cref="CompletionViewModel.TakeReport"/>).
    /// <paramref name="actedOn"/> is how many files the batch moved or deleted.
    ///
    /// Where the window has been asked to close (<see cref="CloseRequested"/>), the
    /// card goes with the window unseen, so it carries no box and no report is
    /// written. The wait for the start check is not begun, or ends as the close
    /// lands, and files moved or deleted make this the PC's first run all the same
    /// (<see cref="CompletionViewModel.RecordFirstRun"/>), as on the cancel arms.
    /// </summary>
    private async Task<bool> TakeReportForCardAsync(int actedOn)
    {
        if (!CloseRequested)
        {
            var reportFree = _completion.ReportIsFreeAsync();
            await Task.WhenAny(reportFree, _closeRequested.Task);
            if (!CloseRequested) return await reportFree && _completion.TakeReport();
            // Left behind as the window closes, and observed all the same, so a
            // failure in it reaches the crash log.
            _ = reportFree.ContinueWith(t => CrashLog.TryWrite(t.Exception!), CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        if (actedOn > 0) _completion.RecordFirstRun();
        return false;
    }

    /// <summary>
    /// Removes the folders the pre-flight created for a Move that then did not go
    /// ahead: the destination and each folder above it that was not there before,
    /// the destination first. The rule, rather than a list that goes stale as paths
    /// are added: every abandon path on which NOTHING WAS PLACED in the destination
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
    /// because the folders that survive are the PRE-FLIGHT's rather than the
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
    /// folders are still left alone. The arm itself carries the reason.
    ///
    /// The list holds only the folders the pre-flight's create made, so a folder
    /// that was already there is never a candidate.
    ///
    /// The removal IS the emptiness test, rather than an Exists or Any check
    /// followed by a delete: Win32 refuses to remove a directory that holds
    /// anything, so a file that arrived between the pre-flight and the cancel
    /// stops the removal at the syscall, with no window in which a check could
    /// pass and the delete then take something with it.
    ///
    /// Off the dispatcher for the same reason every other touch of the
    /// destination is (a stalled network path), and silent on failure because
    /// nothing depends on the folder being gone: it is tidiness, not a
    /// guarantee, and a dialog about a folder the user chose to abandon would
    /// be noise at the end of an operation they cancelled.
    /// </summary>
    private Task RemoveCreatedFoldersAsync(IReadOnlyList<string> createdFolders) =>
        createdFolders.Count == 0
            ? Task.CompletedTask
            // Through the guard, which walks down from the folder that was there
            // before the create, opening each folder the create made from the one
            // above without following a link, and removes the folder where it is
            // empty. Nothing depends on a folder being gone, so a folder that is
            // not empty any more, is gone already or is refused ends the removal
            // without a word.
            : Task.Run(() => CreatedFolders.RemoveWhileEmpty(_fs, _destinationGuard, createdFolders));

    /// <summary>
    /// Re-runs the pending-reboot gate after the service refused at
    /// Global\_MSIExecute, so a refusal that still stands paints the gate's banner
    /// and takes both commands out.
    ///
    /// The gate throws when a read of Windows Installer's in-progress file fails in
    /// a way none of its readings names. The service touched nothing, so the throw
    /// ends here in the failure dialog with no rescan, as it does at the act-time
    /// re-check, and the caller goes on to its own clean-up, which on a Move removes
    /// the folders the pre-flight made. In the arms that show the lock dialog first,
    /// this is a second dialog, and it is the one that reports the failed read.
    /// </summary>
    private async Task RecheckAfterLockRefusalAsync(bool deleting)
    {
        try
        {
            await _scan.RecheckPendingRebootAsync();
        }
        catch (Exception ex)
        {
            ShowActionFailed(ex, deleting);
        }
    }

    /// <summary>
    /// The dialog a Move or Delete ends in when it fails in a way none of its own
    /// arms accounts for, the way every other failure in the app is surfaced: the
    /// exception's type and the crash-log path, never ex.Message, which under
    /// elevation can carry another user's profile path. A dialog rather than the
    /// body-row status, which trims at a width cap and would cut off the log
    /// path, the one actionable thing in a report.
    /// </summary>
    private void ShowActionFailed(Exception ex, bool deleting)
    {
        var crash = CrashLog.TryWrite(ex);
        var typeName = ex.GetType().Name;
        if (deleting)
            _dialogService.ShowWarning(
                crash.Written
                    ? string.Format(Strings.Status_DeleteFailed, typeName, crash.Path)
                    : string.Format(Strings.Status_DeleteFailed_NoLog, typeName),
                Strings.Error_DeleteFailedTitle);
        else
            _dialogService.ShowWarning(
                crash.Written
                    ? string.Format(Strings.Status_MoveFailed, typeName, crash.Path)
                    : string.Format(Strings.Status_MoveFailed_NoLog, typeName),
                Strings.Error_MoveFailedTitle);
    }


    /// <summary>
    /// What one off-thread pass over a Move destination found: whether it is
    /// out of bounds, and, when it is not, what kind of volume it sits on and
    /// how much room is left there. <see cref="PathTooLong"/> is set where the
    /// Move would write a path of MAX_PATH or more. <see cref="Kind"/> and
    /// <see cref="AvailableFreeSpace"/> are only meaningful when no
    /// rejection is set, because the pass returns at the first gate that
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
        long? AvailableFreeSpace,
        bool PathTooLong = false)
    {
        public static DestinationPreFlight Rejected(
            bool insideInstallerCache = false, bool insideSystemFolder = false,
            bool pathTooLong = false) =>
            new(insideInstallerCache, insideSystemFolder, MoveDestinationKinds.Unknown, null, pathTooLong);
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
    /// Ends the current operation: takes <see cref="OperationSource"/> off, cancels
    /// it and disposes it, then clears <see cref="IsCancellationRequested"/> (that
    /// source's UI mirror) and <see cref="IsOperationInFlight"/>. Every exit path of
    /// a Move or a Delete calls this, including the pre-flight's early returns, so
    /// the operation's state is torn down in one place.
    ///
    /// On every path past the re-verify this finds no source left and the flags are
    /// all it clears: the batch lets go of the operation's source as it returns, a
    /// re-verify that fails lets go of it before its dialog, one that finds Windows
    /// Installer busy lets go of it before the banner, and
    /// <see cref="RefreshAfterBatchAsync"/> lets go of the one it installs as it
    /// returns (<see cref="ReleaseOperationSource"/>).
    ///
    /// Order matters on two fronts: the source is taken off before it is disposed,
    /// so nothing reading it afterwards, a Cancel press among them, finds a
    /// disposed source; and it is cancelled before it is disposed, so a
    /// still-running worker on the Dispose-during-shutdown path observes
    /// OperationCanceledException at its next ThrowIfCancellationRequested rather
    /// than ObjectDisposedException.
    ///
    /// The flag is cleared here rather than in each operation's finally
    /// because the Move pre-flight and the gate re-check before either batch
    /// return early without reaching one: a cancelled or failed probe, a
    /// destination refused, not enough space, a declined confirmation, a gate
    /// that blocks or throws. CanCancelOperation reads !IsCancellationRequested,
    /// so clearing it here is what leaves Cancel and Esc live for the next
    /// operation after a Cancel pressed during the probe.
    /// </summary>
    private void EndOperation()
    {
        var cts = OperationSource;
        OperationSource = null;
        cts?.Cancel();
        cts?.Dispose();
        IsCancellationRequested = false;
        IsOperationInFlight = false;
    }

    /// <summary>
    /// Lets go of the operation's cancellation source once nothing is left
    /// running on it, which takes Cancel and Esc out of use
    /// (<see cref="CanCancelOperation"/>) until <see cref="RefreshAfterBatchAsync"/>
    /// installs a source for the rescan, on the paths that rescan. The source is
    /// taken off before it is disposed, as in <see cref="EndOperation"/>, and
    /// taking it off is what tells the button (<see cref="OperationSource"/>).
    /// </summary>
    private void ReleaseOperationSource()
    {
        var cts = OperationSource;
        OperationSource = null;
        cts?.Dispose();
    }

    /// <summary>
    /// The refresh a Move or Delete owes the window at the end of a batch, run
    /// under a FRESH cancellation source with the overlay's Cancel button live
    /// again.
    ///
    /// Every caller arrives with one fact and that fact is the whole contract:
    /// the counts on screen are a statement about the folder as it was before the
    /// batch. How the batch ended does not change that. A run that stopped
    /// part-way may be offering files it has already removed, a run that completed
    /// is describing a batch that is over, and a check that held every file back
    /// acted on nothing but has just found Windows Installer's records disagreeing
    /// with the scan. So each of them re-scans, and a re-scan that does not finish
    /// takes the list off the window rather than leaving the previous scan's
    /// numbers standing (<see cref="ScanViewModel.RefreshAsync"/>).
    ///
    /// The success path runs it as well, with the overlay's heading changed to
    /// the scan's, so a finished delete's overlay says it is scanning rather than
    /// "Deleting..." over "71 of 71" while a scan runs behind it.
    ///
    /// It runs on a source of its own, never the operation's token, and that is
    /// why arming the Cancel button is not simply a matter of passing the token
    /// the caller already holds. On the cancel paths that token is cancelled by
    /// definition and would abandon the refresh at its first checkpoint, leaving
    /// the window with no list at all; and on a success path a Cancel pressed as
    /// the last file finished leaves a cancelled token behind a batch that
    /// reports itself complete. Nor can the refresh run without a token: this
    /// is a full folder walk plus a full API
    /// enumeration on a folder that can hold millions of files, and the user is
    /// watching an overlay that has stopped saying anything. Held behind
    /// a greyed Cancel button with nothing advancing, that wait is the shape
    /// people report as a hang.
    ///
    /// So the button is re-armed rather than left dead: the fresh source and a
    /// cleared IsCancellationRequested are what re-enable it
    /// (<see cref="CanCancelOperation"/>; IsOperating is still true here, and the
    /// overlay's own finally clears it). A second Cancel then stops the refresh
    /// too, and the window drops the list from before the batch rather than
    /// showing it, as it does when the refresh fails
    /// (<see cref="ScanViewModel.RefreshAsync"/>). As the refresh returns it lets
    /// go of its source, and the button goes out of use again for whatever the
    /// caller does next.
    ///
    /// The overlay's numbers go with the heading. They belong to a batch that is
    /// over and the rescan reports no progress of its own, so left in place they
    /// would read as a scan stalled at file 34 of 71 for as long as the walk
    /// takes, and a completed batch's "71 of 71" would make the same claim. The
    /// caller's finally clears them too, but only once the refresh has finished.
    ///
    /// Where the window has been asked to close (<see cref="CloseRequested"/>), no
    /// scan runs and no source is installed. The list from before the batch is
    /// dropped all the same (<see cref="ScanViewModel.DropResultWithoutRescan"/>),
    /// and the window closes as the operation ends. A close that lands while the scan
    /// runs cancels it where Cancel is live (<see cref="RequestClose"/>), and no scan
    /// is reported cancelled there either
    /// (<see cref="ScanViewModel.ClearCancelAsTheWindowCloses"/>).
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

        // The operation's source goes here where the batch has not already let
        // go of it: a re-verify that held every file back, or was cancelled,
        // comes straight here. No Cancel first: nothing still running holds
        // the token.
        ReleaseOperationSource();

        if (CloseRequested)
        {
            // The window is closing, so no scan runs and the overlay keeps its
            // heading. The list from before the batch is dropped all the same, so
            // nothing is left on the window to act on while the close waits its
            // turn on the dispatcher.
            _scan.DropResultWithoutRescan();
            return;
        }

        IsCancellationRequested = false;
        var cts = new CancellationTokenSource();
        OperationSource = cts;
        OperationProgress = Strings.Status_Scanning;
        try
        {
            await _scan.RefreshAsync(cts.Token, WaitsInTheHeading());
        }
        finally
        {
            // Let go of as the rescan returns, because what the caller does
            // next, the card, a dialog or the wait for the report, stops for
            // nothing. Only this refresh's own source, which the view model's
            // Dispose can have taken first.
            if (ReferenceEquals(OperationSource, cts))
                ReleaseOperationSource();
        }

        // The window is closing, so the scan the close cancelled, or the one the user
        // had cancelled before it, is not reported cancelled.
        if (CloseRequested) _scan.ClearCancelAsTheWindowCloses();
    }

    /// <summary>
    /// A reporter for the check before a Move or Delete and the rescan after one, which
    /// shows a wait they report on a source folder (<see cref="ScanProgressUpdate.IsWait"/>)
    /// in the heading, the line a screen reader speaks, with the button that stops it
    /// (<see cref="WaitShown"/>), and puts the heading back when the wait ends, which takes
    /// the button away. The check reports nothing but waits
    /// (<see cref="IRemovableReverifier.ReverifyAsync"/>). The rescan's milestones and its
    /// ticker stop at <see cref="WaitsOnly"/> on the thread that reports them, so none of
    /// them is shown or crosses to the dispatcher. Once Cancel is pressed a wait is not
    /// shown or put back, so "Cancelling..." stays in front of the reader. Made on the
    /// dispatcher, so a wait it is told arrives there.
    /// </summary>
    private IProgress<ScanProgressUpdate> WaitsInTheHeading()
    {
        var heading = new WaitLine(() => OperationProgress, line => OperationProgress = line);
        return new WaitsOnly(new Progress<ScanProgressUpdate>(update =>
        {
            if (IsCancellationRequested) return;
            _waitTakingTheLine = update.Wait;
            try
            {
                heading.Show(update);
            }
            finally
            {
                _waitTakingTheLine = null;
            }
        }));
    }

    private void OnOperationProgressUpdate(OperationProgress p)
    {
        // The batch's first count ends the stretch before it, which the bar spends
        // moving over no count.
        IsOperationProgressIndeterminate = false;
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
    /// Maps a destination-write failure to a localised explanation.
    /// <paramref name="dest"/> is the user's own typed path so echoing
    /// it back is safe; <paramref name="ex"/>.Message is never routed
    /// through this method (path-leak risk under elevation).
    /// <para>
    /// A failure is written to the crash log exactly where its message names the log.
    /// An IOException and any other type call <paramref name="writeCrashLog"/>, and the
    /// message carries the path it returns, or says the log could not be written. A
    /// refusal of access, a path too long and a folder that is not there are explained
    /// whole by their messages, so <paramref name="writeCrashLog"/> is not called for them.
    /// A path too long and a folder that is not there derive from IOException, so their
    /// arms stay ahead of it.
    /// </para>
    /// </summary>
    internal static string DescribeWriteFailure(
        string dest, Exception ex, Func<(string Path, bool Written)> writeCrashLog) => ex switch
    {
        UnauthorizedAccessException =>
            string.Format(Strings.Error_AccessDeniedDestination, dest),
        PathTooLongException =>
            string.Format(Strings.Error_PathTooLong, dest),
        DirectoryNotFoundException =>
            string.Format(Strings.Error_DestinationMissing, dest),
        IOException => NamingCrashLog(
            dest, writeCrashLog, Strings.Error_IOWriteDestination, Strings.Error_IOWriteDestination_NoLog),
        _ => NamingCrashLog(
            dest, writeCrashLog, Strings.Error_WriteDestination, Strings.Error_WriteDestination_NoLog),
    };

    private static string NamingCrashLog(
        string dest, Func<(string Path, bool Written)> writeCrashLog, string withLog, string withoutLog)
    {
        var (path, written) = writeCrashLog();
        return written ? string.Format(withLog, dest, path) : string.Format(withoutLog, dest);
    }
}
