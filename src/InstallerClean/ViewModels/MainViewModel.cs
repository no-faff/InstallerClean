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
///   - A scan completing with no orphans pushes the completion overlay:
///     the all-clear, or the screen saying what the scan held back or
///     carried on without.
///   - The startup scan is shown only once the update check made as the app
///     starts has answered, and behind the card saying a newer version is
///     available where it found one, until Continue without updating.
///   - The Scan command, behind Re-scan and F5, is refused while a Move
///     or a Delete is in flight and while a card is up.
///   - Esc goes to the overlay in front (<see cref="HandleEscape"/>).
///   - Closing the app settles the report box on a card still up.
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

    /// <summary>
    /// Completes once the latest scan that finished has put its card up or settled
    /// that it puts none up. A card on the PC's first run waits for the window's start
    /// check, which can still be reading as the startup scan finishes, so App waits on
    /// this before it builds the window: the startup card is then either up as the
    /// window is constructed, which replays it, or not coming. Where the window opens
    /// with the card saying a newer version is available, this completes only after
    /// Continue without updating, and the window waits on it then.
    /// </summary>
    public Task ScanCardDecided { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// Completes once what the latest scan found may be shown. Completed except between
    /// <see cref="HoldResultsForUpdateCheck"/> and the startup decision
    /// (<see cref="ShowStartupScan"/>) and, where that decision is the card saying a newer
    /// version is available, until Continue without updating. The card a scan puts up,
    /// with its report box, waits for it, so the report and the PC's first-run mark are
    /// never taken while the card is up.
    /// </summary>
    private TaskCompletionSource _resultsMayShow = Released();

    /// <summary>
    /// True once what the latest scan found may be shown, and false while it is held:
    /// whether <see cref="_resultsMayShow"/> has completed.
    /// </summary>
    internal bool ResultsMayShow => _resultsMayShow.Task.IsCompleted;

    private static TaskCompletionSource Released()
    {
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        released.SetResult();
        return released;
    }

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
        IRemovableReverifier reverifier,
        IEarlierRunCheck earlierRunCheck,
        IFirstRunMark firstRunMark,
        IWindowsRegion windowsRegion,
        IDestinationGuard destinationGuard,
        IAppUpdater? appUpdater = null)
    {
        // Closures read Cleanup / Completion at invocation time, after
        // the ctor runs.
        // IsOperationInFlight, not IsOperating: the latter is unset through
        // both of Cleanup's pre-flights, which would leave Re-scan live while
        // a Move or a Delete was already under way.
        Scan = new ScanViewModel(scanService, rebootService, dialogService,
            isExternallyBlocked: () => Cleanup?.IsOperationInFlight == true
                || Completion?.IsComplete == true
                || Chrome?.IsUpdating == true
                || Chrome?.IsOfferingUpdate == true);
        Completion = new CompletionViewModel(
            resultLogService, settingsService, earlierRunCheck,
            firstRunMark, windowsRegion, windowService,
            // Without an updater nothing decided which kind of copy this is.
            copy: appUpdater?.Kind ?? AppCopyKind.Undetermined,
            lastScan: () => Scan.LastScanResult);
        Cleanup = new CleanupViewModel(
            moveService, deleteService, settingsService,
            dialogService, confirmationService, fileSystem,
            Scan, Completion, reverifier,
            instanceLockLost: windowService.IsInstanceLockLost,
            destinationGuard: destinationGuard);
        Chrome = new ChromeViewModel(windowService, msiInfoService, settingsService,
            updateCheckService, dialogService, Scan,
            isBusy: () => IsBusy,
            updater: appUpdater);

        // Surface the completion overlay when a scan finishes with no
        // orphans. Cleanup sets IsOperating=false after the post-
        // operation refresh fires ScanCompleted; that ordering keeps
        // it from overpainting a Move/Delete summary.
        _scanCompletedHandler = OnScanCompleted;
        Scan.ScanCompleted += _scanCompletedHandler;

        // Drive IsMainContentInteractive off the overlay states.
        // The caption buttons themselves remain IsEnabled=true. The
        // window's overlays all span the three grid rows and are drawn
        // over the title bar, so while one is up a click on the minimise
        // or close button lands on the overlay's dim Border. That Border inherits WindowChrome.IsHitTestVisibleInChrome
        // as false, so WindowChrome hands the click to Windows as a click on
        // the window's own title bar and the WPF button never receives it.
        // Esc reaches the overlays through MainWindow.OnPreviewKeyDown and
        // HandleEscape, and Alt+F4 reaches the window's normal SC_CLOSE path
        // through WM_SYSCOMMAND (only SC_MAXIMIZE is intercepted).
        Scan.PropertyChanged += OnChildPropertyChanged;
        Cleanup.PropertyChanged += OnChildPropertyChanged;
        Completion.PropertyChanged += OnChildPropertyChanged;
        Chrome.PropertyChanged += OnChildPropertyChanged;
    }

    public void Dispose()
    {
        Scan.PropertyChanged -= OnChildPropertyChanged;
        Cleanup.PropertyChanged -= OnChildPropertyChanged;
        Completion.PropertyChanged -= OnChildPropertyChanged;
        Chrome.PropertyChanged -= OnChildPropertyChanged;
        Scan.ScanCompleted -= _scanCompletedHandler;
        Chrome.Dispose();
        Cleanup.Dispose();

        // A card still up as the window goes is closed the way the window closed it:
        // its box decides, and the send and its saves get a bounded wait before the
        // process goes.
        Completion.SettleReportOnExit();
    }

    /// <summary>
    /// True iff none of the overlays (scanning, operating, completion, the card saying
    /// a newer version is available, and the card saying an update is downloading) is
    /// showing. Bound to the main-window body's IsEnabled so an active overlay disables
    /// Tab/click on every control behind it.
    /// </summary>
    public bool IsMainContentInteractive =>
        !Scan.IsScanning && !Cleanup.IsOperating && !Completion.IsComplete
        && !Chrome.IsOfferingUpdate && !Chrome.IsUpdating;

    /// <summary>
    /// Holds what the startup scan finds until <see cref="ShowStartupScan"/>. Called by
    /// the App before the scan starts.
    /// </summary>
    public void HoldResultsForUpdateCheck() =>
        _resultsMayShow = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Decides how the startup scan is shown, once the update check made as the app
    /// starts has answered or given up. Where it found a newer version and
    /// <paramref name="mayOfferUpdate"/> holds, the card saying so goes up and the scan
    /// waits behind it for Continue without updating. Otherwise the scan is shown now.
    /// </summary>
    /// <param name="mayOfferUpdate">
    /// False where the startup scan was cancelled, there being nothing to hold, and on
    /// the start that follows a language change, which carries on the session before it.
    /// </param>
    public void ShowStartupScan(bool mayOfferUpdate)
    {
        if (mayOfferUpdate)
            Chrome.OfferUpdate();
        if (!Chrome.IsOfferingUpdate)
            _resultsMayShow.TrySetResult();
    }

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
    /// What Esc does on the main window: the overlay in front takes it, and the return
    /// says whether one did. An idle window takes none. The card saying an update is
    /// downloading is drawn over everything, so it takes Esc first, as Cancel. The card
    /// saying a newer version is available comes next, as Continue without updating; no
    /// other card is up with it.
    ///
    /// The panel saying what the report holds comes before everything, being drawn over
    /// the card it belongs to: Esc closes it and leaves the card up, and the next Esc
    /// closes the card.
    ///
    /// THE FINISHED CARD COMES NEXT, because it is the one in front. A Move or a Delete
    /// puts its card up before the operation has let go, so for that moment the card and
    /// the operating overlay are both up, and the card is drawn over it. Then a running
    /// Move or Delete, whose Esc is Cancel, and then a scan.
    /// </summary>
    public bool HandleEscape()
    {
        if (Chrome.IsUpdating)
        {
            if (Chrome.CancelUpdateCommand.CanExecute(null))
                Chrome.CancelUpdateCommand.Execute(null);
            return true;
        }
        if (Chrome.IsOfferingUpdate)
        {
            Chrome.ContinueWithoutUpdatingCommand.Execute(null);
            return true;
        }
        if (Completion.ReportPanelOpen)
        {
            Completion.ReportPanelOpen = false;
            return true;
        }
        if (Completion.IsComplete && Completion.DismissCommand.CanExecute(null))
        {
            Completion.DismissCommand.Execute(null);
            return true;
        }
        if (Cleanup.IsOperating && Cleanup.CancelOperationCommand.CanExecute(null))
        {
            Cleanup.CancelOperationCommand.Execute(null);
            return true;
        }
        if (Scan.IsScanning && Scan.CancelScanCommand.CanExecute(null))
        {
            Scan.CancelScanCommand.Execute(null);
            return true;
        }
        return false;
    }

    /// <summary>
    /// The main window's opening line. The intro is the only thing that tells the
    /// window's states apart:
    ///
    ///   - the scan after a Move or Delete did not finish: it says so, and that
    ///     there is no list until the next scan,
    ///   - a scan FAILED (the startup scan, or a Re-scan): the tailored error
    ///     under the heading its dialog carries on a Re-scan, with Re-scan
    ///     focused, instead of exiting,
    ///   - nothing scanned yet (the startup scan was cancelled),
    ///   - a scan found files but a Windows Installer operation is in progress, so
    ///     Move and Delete are held: the copy explains the hold rather than telling
    ///     the user to press the dead buttons,
    ///   - a scan found files and they can be acted on (the state the window was
    ///     designed for),
    ///   - a scan found nothing (an all-clear, and the end state of every
    ///     successful clean-up).
    ///
    /// The first two take precedence over not-yet-scanned, because each leaves
    /// <see cref="ScanViewModel.HasScanned"/> false too but must say what happened
    /// rather than "nothing scanned yet", which after a Move or Delete is not true.
    /// The two never hold together: a scan that sets either message clears the
    /// other first. A completed scan gets the same lead at any count, zero
    /// included: "Any unneeded files below" is written to read correctly over an
    /// empty list, and the completion overlay has already announced the result in
    /// its own words by the time this window is read.
    /// </summary>
    public string IntroLead =>
        Scan.HasUnfinishedRefresh ? Strings.Body_RescanNotFinished_Lead
        : Scan.HasScanError ? Scan.LastScanErrorTitle
        : !Scan.HasScanned ? Strings.Body_NotScanned_Lead
        : Scan.HasOrphans && Scan.HasPendingReboot ? Strings.Body_PendingReboot_Lead
        : Strings.Body_MainExplanation_Lead;

    /// <summary>
    /// The muted second line under <see cref="IntroLead"/>. Carries the tailored
    /// message on a failed scan, and the message saying the scan after a Move or
    /// Delete did not finish. Shown for every completed scan, zero files
    /// included: it is the app explaining what it does, and a clean machine is
    /// when a first-time user needs that most. Empty only on a pending-reboot
    /// hold (the banner below carries the specific reason), which collapses the
    /// TextBlock.
    /// </summary>
    public string IntroDetail =>
        Scan.HasUnfinishedRefresh ? Scan.UnfinishedRefreshMessage
        : Scan.HasScanError ? Scan.LastScanError
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
    /// "Scan cancelled." under the intro when the latest scan was cancelled and
    /// no result is on screen: the startup scan, the scan after a Move or Delete,
    /// or a Re-scan over a window that already had none. The user is told why
    /// the window is empty rather than being shown what looks like a clean
    /// machine. Empty (and collapsed) in every other state: while a completed
    /// scan's result is on screen, that result is the answer.
    /// </summary>
    public string IntroNotice =>
        !Scan.HasScanned && Scan.LastScanWasCancelled ? Strings.Status_ScanCancelled : string.Empty;

    /// <summary>
    /// The middle ("why") sentence of the main-window intro. The intro is three
    /// resx keys (<c>Body.MainExplanation.Lead</c> / <c>.Why</c> / <c>.Action</c>)
    /// so each reads at its own text tier; only this one ever interpolated, which
    /// is why only it binds to the view-model.
    ///
    /// THE SENTENCE TAKES TWO ARGUMENTS AND THREE ARE PASSED, AGAINST A WORDING
    /// THAT MIGHT SPEND THE THIRD RATHER THAN FOR ONE THAT DOES. It carries a slot
    /// per Reason label so that a translator edits the column labels in one place
    /// and the copy follows. Two kinds of file reach the list, orphaned and
    /// superseded, so the neutral spends the first two slots and no satellite
    /// spends the third: read out of the neutral and every satellite, not one contains
    /// {2}.
    ///
    /// IT STAYS, AND WHAT IT GUARDS IS THE OTHER DIRECTION. string.Format ignores a
    /// surplus argument and throws on a missing one, so a translator who writes a
    /// three-slot sentence into a satellite gets it rendered, where dropping this
    /// argument would hand them a FormatException on the main window instead. Its
    /// cost is nothing and the cost of removing it is paid by somebody who cannot
    /// see this line.
    /// </summary>
    public string MainExplanationWhyText =>
        string.Format(Strings.Body_MainExplanation_Why,
            Strings.Reason_Orphaned, Strings.Reason_Superseded, Strings.Reason_Obsoleted);

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The three intro lines are computed from the scan's state, so they
        // re-read whenever any of their inputs moves. HasOrphans covers
        // OrphanedFileCount, which raises it, and HasScanError and
        // HasUnfinishedRefresh cover the two messages, whose setters raise them
        // on every change.
        if (e.PropertyName is nameof(ScanViewModel.HasScanned)
            or nameof(ScanViewModel.HasOrphans)
            or nameof(ScanViewModel.LastScanWasCancelled)
            or nameof(ScanViewModel.HasScanError)
            or nameof(ScanViewModel.LastScanErrorTitle)
            or nameof(ScanViewModel.HasUnfinishedRefresh)
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
            e.PropertyName == nameof(CompletionViewModel.IsComplete) ||
            e.PropertyName == nameof(ChromeViewModel.IsOfferingUpdate) ||
            e.PropertyName == nameof(ChromeViewModel.IsUpdating))
        {
            OnPropertyChanged(nameof(IsMainContentInteractive));
            OnPropertyChanged(nameof(IsBusy));
            // Block F5 / Re-scan while a Move/Delete (its pre-flight included),
            // a card or an update is up so a parallel scan can't race it.
            Scan.NotifyExternallyBlockedChanged();
            // Update now and the Details buttons wait for a scan or a Move or
            // Delete to finish.
            Chrome.NotifyBusyChanged();
        }

        // An update that ended without handing over can leave the app without the
        // single-instance mutex, which Move and Delete need.
        if (e.PropertyName == nameof(ChromeViewModel.IsUpdating) && !Chrome.IsUpdating)
            Cleanup.NotifyInstanceLockChanged();

        // Continue without updating: the scan the card stood in front of is shown.
        if (e.PropertyName == nameof(ChromeViewModel.IsOfferingUpdate) && !Chrome.IsOfferingUpdate)
            _resultsMayShow.TrySetResult();
    }

    private void OnScanCompleted(object? sender, EventArgs e)
    {
        var decided = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ScanCardDecided = decided.Task;
        _ = ShowScanCardAsync(decided);
    }

    /// <summary>
    /// The card a scan that offered nothing puts up, and its report where it is the
    /// PC's first. Never throws: a fault is logged, and <paramref name="decided"/>
    /// completes on every path.
    /// </summary>
    private async Task ShowScanCardAsync(TaskCompletionSource decided)
    {
        try
        {
            if (ScanCardResult() is not { } result) return;

            // The card and its report wait until the scan may be shown.
            await _resultsMayShow.Task;
            if (!ReferenceEquals(ScanCardResult(), result)) return;

            // Resumes on the dispatcher, which the card needs: revealing it raises the
            // change the window answers by building the summary's inlines. Where the
            // start check is still reading, this is a real wait, and a Re-scan or the
            // operation after it can land inside it, so the card is put up only where
            // the scan it is for is still the one on the window.
            var reportIsFree = await Completion.ReportIsFreeAsync();
            if (!ReferenceEquals(ScanCardResult(), result)) return;
            var carriesReport = reportIsFree && Completion.TakeReport();

            // TWO SCREENS, and which one is shown turns on a fact the lists cannot
            // carry. An empty offer means EITHER that the scan held nothing back, or
            // held back only files the declared-product-installed and
            // declared-patch-registered arms kept and gave up no drive or share keeping
            // one, which gets the all-clear, OR that it held back any other file, a
            // superseded patch included, or carried on without a drive or share and left
            // a file alone because of it, which gets the screen saying nothing was
            // offered. Telling the
            // second machine there is nothing to clean up in its Installer folder is a
            // claim about that disk the scan never made.
            //
            // THE READING IS THE SCAN'S AND THIS HOST DOES NOT PARTITION ANYTHING TO
            // GET IT, which is the constraint on anything that replaces these lines.
            // More than one decision fills the withheld list, so a host counting that
            // list is inferring one decision's outcome from figures the others also
            // write to: the moment any of their memberships moves, the gate means
            // something different and nothing fails. The scan result answers it where
            // the withholding happens, and the host spends the answer rather than
            // deriving it.
            //
            // THE CHOICE TURNS ON ScanResult.UnsettledHeldBackCount: every file held back
            // except those kept for a program Windows still has installed or for their
            // patch's registrations, together with the superseded patches the scan held
            // back. A file whose times are not before the scan's clock, a file held
            // because an in-progress signal was set or because the clock went back, a
            // file whose times could not be established and a file the containment check
            // refused or could not answer for are in it.
            // HasUnsettledHeldBack is that count above zero. ScanResult.IsAllClear adds
            // the drives and shares given up, a file kept at one of them being outside
            // that count where its own program is installed, and the all-clear is the
            // machine's only where it says so.
            //
            // THE LINE NAMING THOSE DRIVES AND SHARES IS THE ONE THE MAIN WINDOW SHOWS
            // UNDER THE LEFT-ALONE LINE, handed over as it stands, so the card and the
            // window behind it name them in the same words.

            // THE RECEIPT SPENDS THE COUNT THE MAIN WINDOW IS ALREADY SHOWING rather
            // than recounting the scan result here, so the overlay and the line behind
            // it cannot come apart. Dismissing the overlay puts that line in front of
            // the reader, which is close enough for two different numbers to read as a
            // mistake.
            //
            // IT COUNTS FILES IN THE FOLDER AND NOT REGISTRATIONS, which is what the
            // noun on the receipt says. One product registered with three patches is a
            // single product and four cached files, and a registration naming a file
            // the folder does not have adds nothing to a count of what is in it.
            //
            // A HELD-BACK FILE IS INSIDE IT, and that is the true reading rather than
            // an overlap to tidy away. The scan found it, judged it and decided not to
            // offer it, so a receipt leaving it out would understate what was
            // examined, which is the whole of what the receipt is for: an elapsed time
            // on its own reads as though nothing had happened.
            if (result.IsAllClear)
            {
                Completion.ShowAllClear(Scan.RegisteredFileCount, Scan.LastScanDurationMs);
            }
            else
            {
                Completion.ShowNothingOffered(
                    result,
                    Scan.RegisteredFileCount,
                    Scan.LastScanDurationMs,
                    Scan.SourcesGivenUpText);
            }
            decided.TrySetResult();

            if (carriesReport)
                await Completion.WriteReportAsync(
                    ResultLogEntry.ForScanOnly(
                        result, Scan.LastScanDurationMs, Completion.ReportRegion,
                        Completion.ReportHost, Completion.ReportCopy));
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
        }
        finally
        {
            decided.TrySetResult();
        }
    }

    /// <summary>
    /// The result of the scan that finished, where it is one the card is for: it offered
    /// nothing and no Move or Delete is running behind it. Null otherwise. The refresh
    /// after a Move or Delete finishes with <see cref="CleanupViewModel.IsOperating"/>
    /// still true, so its scan puts up no card here and the operation's own card is not
    /// painted over.
    /// </summary>
    private ScanResult? ScanCardResult() =>
        Scan.OrphanedFileCount != 0 || Cleanup.IsOperating ? null : Scan.LastScanResult;
}
