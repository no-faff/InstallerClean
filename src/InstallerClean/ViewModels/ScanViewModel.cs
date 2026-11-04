using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// Scanning slice of the main window's state. Owns the scan command,
/// the displayed registered/orphaned counts, the pending-reboot
/// warning, the missing-from-disk warning, the line naming the drives and
/// shares given up and a reference to the last scan result.
///
/// Other slices (CleanupViewModel, ChromeViewModel) read
/// <see cref="LastScanResult"/> rather than calling the scan service
/// themselves so the cached result stays the single source of truth.
/// </summary>
public partial class ScanViewModel : ObservableObject
{
    private readonly IFileSystemScanService _scanService;
    private readonly IPendingRebootService _rebootService;
    private readonly IDialogService _dialogService;
    private readonly Func<bool> _isExternallyBlocked;

    private CancellationTokenSource? _scanCts;

    /// <summary>
    /// Reveals the scanning overlay. Deliberately not the gate on any command:
    /// see <see cref="IsScanInFlight"/>.
    /// </summary>
    [ObservableProperty] private bool _isScanning;

    /// <summary>
    /// True from the first line of every scan (the Scan command, the splash
    /// startup scan and the silent post-operation refresh) until it ends.
    ///
    /// <see cref="IsScanning"/> cannot serve as this gate: it is an
    /// overlay-reveal flag, set only once a scan outlives the 200 ms delay,
    /// and never at all by the silent refresh. It would leave the Move and
    /// Delete buttons live through the first 200 ms of a scan, which is long
    /// enough to start a destructive batch against the previous scan's result
    /// while a fresh scan walks the same folder, and to leave two scans writing
    /// <see cref="LastScanResult"/> with no ordering between them.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private bool _isScanInFlight;

    private string _scanProgress = string.Empty;

    /// <summary>
    /// The line on the scanning card that a screen reader reads out
    /// (<see cref="ScanProgressName"/>). Every write sets <see cref="WaitShown"/> to the wait
    /// whose line <see cref="ShowWait"/> is writing, which is null for every other write,
    /// whether or not the line changes. Where it changes, the button is set after the line
    /// is stored and before the change is raised, so the line, the button under it and the
    /// name a screen reader speaks change in one step.
    /// </summary>
    public string ScanProgress
    {
        get => _scanProgress;
        set
        {
            if (EqualityComparer<string>.Default.Equals(_scanProgress, value))
            {
                WaitShown = _waitTakingTheLine;
                return;
            }

            OnPropertyChanging();
            OnPropertyChanging(nameof(ScanProgressName));
            _scanProgress = value;
            WaitShown = _waitTakingTheLine;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ScanProgressName));
        }
    }

    /// <summary>
    /// What a screen reader speaks for <see cref="ScanProgress"/>: the line itself, and
    /// while it is a wait the scanning card's button can stop, the line followed by the key
    /// that presses the button and what that leaves alone
    /// (<see cref="DisplayHelpers.WaitingLineWithStopKey"/>).
    /// </summary>
    public string ScanProgressName =>
        WaitShown is null ? ScanProgress : DisplayHelpers.WaitingLineWithStopKey(ScanProgress);

    /// <summary>
    /// The wait <see cref="ScanProgress"/> names while it is a waiting line, and null for
    /// every other line. Every write to the line sets it: the write putting a wait's own line
    /// up sets it to that wait, and every other write sets it to null, the write putting back
    /// the line the wait took among them, whatever any of them says.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStopWaiting))]
    [NotifyPropertyChangedFor(nameof(StopWaitingName))]
    [NotifyPropertyChangedFor(nameof(ScanProgressName))]
    [NotifyCanExecuteChangedFor(nameof(StopWaitingCommand))]
    private SourceFolderWait? _waitShown;

    /// <summary>The wait whose line <see cref="ShowWait"/> is writing, and null at any other time.</summary>
    private SourceFolderWait? _waitTakingTheLine;

    /// <summary>Whether the scanning card shows the button that stops the wait the line names.</summary>
    public bool CanStopWaiting => WaitShown is not null;

    /// <summary>
    /// The name a screen reader speaks for that button, naming the drive or share
    /// (<see cref="DisplayHelpers.StopWaitingFor"/>), and empty while there is no button.
    /// </summary>
    public string StopWaitingName => WaitShown is { } wait ? DisplayHelpers.StopWaitingFor(wait.Root) : string.Empty;

    /// <summary>
    /// Gives up on the drive or share the line names for the rest of this scan. The scan
    /// ends the wait at once and tells its end, which puts the line back and takes the
    /// button away.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStopWaiting))]
    private void StopWaiting() => WaitShown?.StopWaiting();

    /// <summary>
    /// Ticker line under the scan overlay's milestone text: a count of the
    /// folder's files as the scan lists them, the name of each installed
    /// program as it asks Windows about it, then a count of the files as
    /// it matches them. Display-only: the window never reads it out, because
    /// the ticker updates once per program and many times while the files
    /// are counted, which can run to hundreds of updates in a few seconds,
    /// and reading each would queue speech far faster than it can be spoken.
    /// </summary>
    [ObservableProperty] private string _scanTicker = string.Empty;

    /// <summary>
    /// True while the view model holds the result of a scan that completed. False
    /// before the first one, which is a state the user reaches by cancelling the
    /// startup scan, and false again after a scan that stops or fails, after the
    /// user cancels the scan that follows a Move or Delete, and where that scan does
    /// not run because the window is closing (<see cref="DropResultWithoutRescan"/>).
    /// The main window has to say so rather than paint a zeroed scan result, or an
    /// earlier one.
    ///
    /// A Re-scan the user cancels leaves it as it was. An earlier result stays on
    /// screen: that scan completed, and nothing has acted on its result since.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOrphans))]
    private bool _hasScanned;

    [ObservableProperty] private int _registeredFileCount;
    [ObservableProperty] private string _registeredSizeDisplay = string.Empty;
    [ObservableProperty] private int _orphanedFileCount;
    [ObservableProperty] private string _orphanedSizeDisplay = string.Empty;

    /// <summary>
    /// Last pending-reboot probe result. Null until a scan completes, and null
    /// again at each ending <see cref="HasScanned"/> lists, the banner going with
    /// the list it sits over.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingReboot))]
    [NotifyPropertyChangedFor(nameof(PendingRebootBannerText))]
    private PendingRebootResult? _pendingRebootResult;

    /// <summary>True when the last probe returned Block.</summary>
    public bool HasPendingReboot => PendingRebootResult?.IsBlocked == true;

    /// <summary>
    /// Localised banner text for the current Block reason; empty otherwise.
    ///
    /// The last arm is what a reason with no line of its own gets, and it is a
    /// string rather than a throw because of where this is read from: WPF calls
    /// it through a binding and swallows anything it throws as a binding error,
    /// so the user would meet a blank banner over greyed buttons with nothing on
    /// screen saying why. A generic sentence that is true of the whole family
    /// beats that. It is unreachable while every member of the enum is handled
    /// above, and a member added without a banner is a failing test rather than a
    /// silent gap because Every_reason_has_a_banner_of_its_own walks the enum,
    /// and A_reason_with_no_banner_of_its_own_still_says_something covers this
    /// arm (ScanViewModelPendingRebootTests).
    ///
    /// THE SAME SENTENCE IS ALSO THE IN-PROGRESS FILE'S OWN, named in its arm
    /// above: it says Windows Installer has something in progress, which is what
    /// that file records. A member added later without a banner falls through to
    /// the same text and meets the test's distinctness check, which the file's arm
    /// keeps armed.
    /// </summary>
    public string PendingRebootBannerText => PendingRebootResult?.Reason switch
    {
        PendingRebootReason.MsiExecuteMutexHeld => Strings.Body_PendingReboot_MsiExecuteMutex,
        PendingRebootReason.MsiExecuteMutexAccessRefused => Strings.Body_PendingReboot_MsiExecuteMutexAccessRefused,
        PendingRebootReason.InstallerInProgress => Strings.Body_PendingReboot_InstallerInProgress,
        PendingRebootReason.PendingRenameInCache => Strings.Body_PendingReboot_PendingRenameInCache,
        PendingRebootReason.PendingRenameUnresolved => Strings.Body_PendingReboot_PendingRenameUnresolved,
        PendingRebootReason.RegistryCheckUnreadable => Strings.Body_PendingReboot_RegistryCheckUnreadable,
        PendingRebootReason.InstallerInProgressMarker => Strings.Body_PendingReboot_Other,
        // Windows refused the read of the in-progress file: the refused-permission
        // sentence is as true of that read as of the mutex's.
        PendingRebootReason.InstallerInProgressMarkerAccessRefused => Strings.Body_PendingReboot_MsiExecuteMutexAccessRefused,
        // Given with the Windows error the check failed on, where it failed on one.
        PendingRebootReason.MsiExecuteMutexUnreadable => WindowsErrors.Sentence(
            PendingRebootResult.WindowsError,
            Strings.Body_PendingReboot_MsiExecuteMutexUnreadable,
            Strings.Body_PendingReboot_MsiExecuteMutexUnreadableWindowsError),
        null => string.Empty,
        _ => Strings.Body_PendingReboot_Other,
    };

    /// <summary>
    /// The registrations this scan found naming a file that is not on disk AND whose
    /// absence it could not establish to be harmless. Drives the missing-files line, and
    /// it is the count the line prints as well as the condition that fires it, so the two
    /// cannot disagree.
    ///
    /// IT IS A HALF, AND THE AXIS IS A CONJUNCTION. A missing registration is left out
    /// only where its patch state is superseded or obsoleted AND every product sharing the
    /// patch was shown to hold no patch that could be uninstalled and roll back onto the
    /// file. The state alone does not make an absence harmless, Windows opening every
    /// registered patch's cached file whichever state it carries. Counting every missing
    /// registration would warn about files this app removed after establishing exactly
    /// that condition.
    ///
    /// <see cref="MissingFilesReport.Affected"/> is that expression, named once, and the
    /// programs this line names come off the same predicate. The full total still travels
    /// in the scan result and in the report payload, where a public chart reads it with no
    /// version gate.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMissingFromDisk))]
    [NotifyPropertyChangedFor(nameof(MissingFromDiskSummaryText))]
    private int _missingFromDiskCount;

    /// <summary>
    /// The programs those files belong to, as the one phrase the line names them
    /// in, already capped and joined by <see cref="MissingFilesReport"/> so the
    /// window and the command line say the same thing. Empty when there are none.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MissingFromDiskSummaryText))]
    private string _missingFromDiskPrograms = string.Empty;

    /// <summary>
    /// The line naming the drives and shares this scan gave up on while files still had
    /// to be checked against them (<see cref="SourcesGivenUpReport.WindowLine"/>), shown
    /// under the left-alone line, whose count takes those files in. Empty where the scan
    /// gave none up keeping a file, which collapses the line.
    ///
    /// THE FINISHED CARD SPENDS THIS SAME TEXT where the scan offered nothing, so the card
    /// and the line behind it name the same drives and shares in the same words.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSourcesGivenUp))]
    private string _sourcesGivenUpText = string.Empty;

    /// <summary>Whether <see cref="SourcesGivenUpText"/> has a line to show.</summary>
    public bool HasSourcesGivenUp => SourcesGivenUpText.Length > 0;

    /// <summary>
    /// The line saying the scan held back every file in the folder no record names,
    /// because one of Windows Installer's in-progress signals was set when it read them
    /// just after its clock, or because the system clock went back while it read the files'
    /// times (<see cref="ScanSignalReport.WindowLine(ScanResult)"/>), shown under the
    /// left-alone line, whose count takes those files in. Empty where neither held a file,
    /// which collapses the line.
    ///
    /// IT DOES NOT TOUCH MOVE AND DELETE. Those follow the read made after the scan
    /// (<see cref="PendingRebootResult"/>), as the command line's /d and /m follow theirs,
    /// and the files this line speaks of are not on the list either way. Where that later
    /// read is set too, the warning banner says so as well, and both are true.
    ///
    /// The finished card reads the same line off the same result where the scan offered
    /// nothing (<see cref="CompletionViewModel.ShowNothingOffered"/>).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScanSignal))]
    private string _scanSignalText = string.Empty;

    /// <summary>Whether <see cref="ScanSignalText"/> has a line to show.</summary>
    public bool HasScanSignal => ScanSignalText.Length > 0;

    /// <summary>
    /// The result the main window is showing, from the most recent scan that
    /// completed. Null until the first scan completes, and null again after a scan
    /// that stops or fails and after the user cancels the scan that follows a Move
    /// or Delete (see <see cref="HasScanned"/>), so Move and Delete have no files
    /// to act on then and the Details windows no list to open. Otherwise the same
    /// instance until the next scan replaces it.
    /// </summary>
    public ScanResult? LastScanResult { get; private set; }

    /// <summary>
    /// Wall-clock duration of the most recent user-visible scan, in
    /// milliseconds. Set by <c>ScanAsync</c> and <c>ScanWithProgressAsync</c>;
    /// not overwritten by <c>RefreshAsync</c> so the result-log entry
    /// built after a Move or Delete reports the duration of the scan
    /// that surfaced the orphans, not the silent post-operation refresh.
    /// </summary>
    public long LastScanDurationMs { get; private set; }

    /// <summary>
    /// Raised after every successful scan completes, including the
    /// initial startup scan. Subscribers can read
    /// <see cref="LastScanResult"/> at this point.
    /// </summary>
    public event EventHandler? ScanCompleted;

    public ScanViewModel(
        IFileSystemScanService scanService,
        IPendingRebootService rebootService,
        IDialogService dialogService,
        Func<bool>? isExternallyBlocked = null)
    {
        _scanService = scanService;
        _rebootService = rebootService;
        _dialogService = dialogService;
        _isExternallyBlocked = isExternallyBlocked ?? (() => false);
        _statusWait = new WaitLine(() => ScanProgress, line => ScanProgress = line);
    }

    /// <summary>
    /// Tells the Scan command to re-evaluate its CanExecute. MainViewModel
    /// calls this when the externally-blocked predicate's inputs change
    /// (Cleanup.IsOperationInFlight or Completion.IsComplete).
    /// </summary>
    public void NotifyExternallyBlockedChanged() =>
        ScanCommand.NotifyCanExecuteChanged();

    public string RegisteredSummaryText => LeftAloneReport.Line(RegisteredFileCount);

    public string OrphanedSummaryText =>
        string.Format(
            DisplayHelpers.Pluralise(OrphanedFileCount,
                Strings.Summary_OrphanedToCleanUp_Singular,
                Strings.Summary_OrphanedToCleanUp_Plural,
                "Summary.OrphanedToCleanUp"),
            DisplayHelpers.FormatCount(OrphanedFileCount));

    /// <summary>
    /// True when the last scan found files to clean up. Move, Delete and the
    /// Details button beside the unneeded count are enabled only while this
    /// holds, and the window's pending-reboot lead
    /// (<see cref="Strings.Body_PendingReboot_Lead"/>) shows only while it holds.
    /// </summary>
    public bool HasOrphans => HasScanned && OrphanedFileCount > 0;

    public bool HasMissingFromDisk => MissingFromDiskCount > 0;

    public string MissingFromDiskSummaryText =>
        string.Format(
            DisplayHelpers.Pluralise(MissingFromDiskCount,
                Strings.Summary_MissingFromDisk_Singular,
                Strings.Summary_MissingFromDisk_Plural,
                "Summary.MissingFromDisk"),
            DisplayHelpers.FormatCount(MissingFromDiskCount), MissingFromDiskPrograms);

    partial void OnRegisteredFileCountChanged(int value) =>
        OnPropertyChanged(nameof(RegisteredSummaryText));

    partial void OnOrphanedFileCountChanged(int value)
    {
        OnPropertyChanged(nameof(OrphanedSummaryText));
        OnPropertyChanged(nameof(HasOrphans));
    }

    /// <summary>
    /// Runs the scan service and updates this VM's display fields.
    /// Used by the user-driven Scan command and by the splash startup
    /// scan. Does not raise <see cref="ScanCompleted"/>; that fires
    /// from <see cref="ScanAsync"/> and
    /// <see cref="ScanWithProgressAsync"/> after their respective
    /// success paths.
    /// </summary>
    private async Task RunScanCoreAsync(IProgress<ScanProgressUpdate>? progress, CancellationToken cancellationToken = default)
    {
        // Set before the first await, so it is already true when the caller's
        // command returns to the dispatcher: every scan entry point routes
        // through here, so this is the one place the gate can be complete.
        IsScanInFlight = true;
        try
        {
            // Compute everything off the call results before touching any
            // observable property, so a throw or a cancel leaves every property
            // as this method found it. Which endings keep that earlier result on
            // screen is the callers' decision, each in its own catch.
            var result = await _scanService.ScanAsync(progress, cancellationToken);
            // Sample reboot after the scan; ordering matters. An MSI install
            // starting mid-scan could flip the _MSIExecute mutex, and
            // probing first would miss it.
            var pendingRebootResult = await Task.Run(() => _rebootService.Check(), cancellationToken);

            // THE LEFT-ALONE COUNT AND THE SIZE BESIDE IT ARE THE RESULT'S OWN
            // (ScanResult.LeftAloneCount and LeftAloneTotalBytes), over the same two
            // populations, and the command line prints the count from the same property.
            //
            // The Details window's own header counts the same two populations, off the
            // same lists, because the two are one click apart and a reader comparing them
            // must not find them disagreeing.
            var registeredCount = result.LeftAloneCount;
            var registeredSize = DisplayHelpers.FormatSize(result.LeftAloneTotalBytes);
            var orphanedCount = result.RemovableFiles.Count;
            var orphanedSize = DisplayHelpers.FormatSize(result.RemovableFiles.Sum(f => f.SizeBytes));
            // Built here with the rest of the display state rather than in the
            // property, which is read on every binding refresh: it walks the whole
            // registered set and groups it, and the answer only changes when a
            // scan does.
            var missingPrograms = MissingFilesReport.Inline(
                MissingFilesReport.Products(result.RegisteredPackages));
            var sourcesGivenUp = SourcesGivenUpReport.WindowLine(result.SourceRootsGivenUpKeepingFiles);
            var scanSignal = ScanSignalReport.WindowLine(result);

            PendingRebootResult = pendingRebootResult;
            LastScanResult = result;
            RegisteredFileCount = registeredCount;
            RegisteredSizeDisplay = registeredSize;
            OrphanedFileCount = orphanedCount;
            OrphanedSizeDisplay = orphanedSize;
            // THE AFFECTED HALF, NOT THE SUM. The banner fires where something could
            // still reach for a file that is gone, so a registration whose absence the
            // app positively established to be harmless is not in the count and its
            // program is not named. Both come off the same predicate in
            // MissingFilesReport, and the sum still travels in the report payload, where
            // a public chart reads it with no version gate.
            MissingFromDiskCount = result.MissingAffectedCount;
            MissingFromDiskPrograms = missingPrograms;
            SourcesGivenUpText = sourcesGivenUp;
            ScanSignalText = scanSignal;
            HasScanned = true;
        }
        finally
        {
            IsScanInFlight = false;
        }
    }

    /// <summary>
    /// User-driven scan command. Shows the scan overlay if the scan
    /// takes longer than 200ms, surfaces admin / DB / unknown errors
    /// to the dialog service, and updates <see cref="ScanProgress"/>
    /// throughout.
    /// </summary>
    private bool CanScan() => !IsScanInFlight && !_isExternallyBlocked();

    /// <summary>
    /// True when the most recent scan ended because the user cancelled it
    /// (rather than completing or failing). The view reads this when the
    /// scanning overlay collapses to re-announce "Scan cancelled." past the
    /// focus move that would otherwise swallow it, and the main window's
    /// states with no result read it to say why there is nothing on screen.
    /// Reset at the start of every scan, and where the window closes around the scan
    /// after a Move or Delete, whether that scan does not run
    /// (<see cref="DropResultWithoutRescan"/>) or runs as the close lands
    /// (<see cref="ClearCancelAsTheWindowCloses"/>).
    ///
    /// Observable, not a plain property: the startup scan is the one that gets
    /// cancelled in practice, and it sets this without ever setting
    /// <see cref="HasScanned"/>, so nothing else raises for the window to
    /// re-read it.
    /// </summary>
    public bool LastScanWasCancelled
    {
        get => _lastScanWasCancelled;
        private set => SetProperty(ref _lastScanWasCancelled, value);
    }

    private bool _lastScanWasCancelled;

    /// <summary>
    /// Tailored, safe-to-show message for the most recent scan that FAILED. Empty
    /// until the first failure and cleared by a scan that completes. A Re-scan the
    /// user cancels leaves it where it was, along with everything else on screen,
    /// and the scan after a Move or Delete clears it as it starts, carrying a
    /// message of its own (<see cref="UnfinishedRefreshMessage"/>). Both the
    /// user-driven Scan command and the startup scan set it
    /// through the one error ladder (<see cref="DescribeScanFailure"/>); the main
    /// window shows it in place of the not-yet-scanned copy, with Re-scan focused,
    /// so a failed startup scan opens the window with the diagnosis rather than
    /// exiting.
    /// </summary>
    public string LastScanError
    {
        get => _lastScanError;
        private set
        {
            if (SetProperty(ref _lastScanError, value))
                OnPropertyChanged(nameof(HasScanError));
        }
    }

    private string _lastScanError = string.Empty;

    /// <summary>True when the last scan failed and its message is on screen.</summary>
    public bool HasScanError => LastScanError.Length > 0;

    /// <summary>
    /// The heading for <see cref="LastScanError"/>, the one its dialog carries on
    /// a Re-scan, so the main window and the dialog name the failure alike.
    /// Meaningful only while <see cref="HasScanError"/> is true. The window
    /// re-reads its heading when this changes as well as when the message does,
    /// so the two can be set in either order.
    /// </summary>
    public string LastScanErrorTitle
    {
        get => _lastScanErrorTitle;
        private set => SetProperty(ref _lastScanErrorTitle, value);
    }

    private string _lastScanErrorTitle = string.Empty;

    /// <summary>
    /// What the main window says in place of a list when the scan after a Move or
    /// Delete ended without a result: stopped, failed or cancelled, or did not run
    /// because the window is closing. Empty otherwise. That scan clears it as it
    /// starts and sets it if it ends without a result, a Re-scan that completes or
    /// fails clears it, and a Re-scan the user cancels leaves it where it was, with
    /// the rest of the window.
    ///
    /// It is its own message rather than the failure's, which is what
    /// <see cref="LastScanError"/> carries after the other two scans. The account a
    /// scan gives when it stops, or when Windows refuses it, says that nothing has
    /// been removed. That is true of the scan, and it reads as false straight after
    /// a Move or Delete that has moved or deleted files. So the window says only
    /// that this scan did not finish, whatever ended it, and the account goes to
    /// the crash log, whose path the message names where the write succeeded. A
    /// Re-scan gives the account in full if the cause is still there.
    /// </summary>
    public string UnfinishedRefreshMessage
    {
        get => _unfinishedRefreshMessage;
        private set
        {
            if (SetProperty(ref _unfinishedRefreshMessage, value))
                OnPropertyChanged(nameof(HasUnfinishedRefresh));
        }
    }

    private string _unfinishedRefreshMessage = string.Empty;

    /// <summary>True when the scan after a Move or Delete ended without a result.</summary>
    public bool HasUnfinishedRefresh => UnfinishedRefreshMessage.Length > 0;

    /// <summary>
    /// Takes the last result off the view model, at each ending
    /// <see cref="HasScanned"/> lists. Everything the main window draws from a
    /// result goes with it: both counts, the missing-files line, the line naming
    /// the drives and shares given up and the pending-reboot banner. So do the
    /// commands that act on one, Move and Delete having no files to act on and
    /// the Details windows no list to open.
    ///
    /// <see cref="LastScanResult"/> goes first and <see cref="HasScanned"/> last.
    /// The result raises nothing when it changes, and the Details commands re-ask
    /// whether they can run when HasScanned does, so HasScanned going false while
    /// the result was still here would leave both buttons live over it.
    /// </summary>
    private void DropResult()
    {
        LastScanResult = null;
        RegisteredFileCount = 0;
        RegisteredSizeDisplay = string.Empty;
        OrphanedFileCount = 0;
        OrphanedSizeDisplay = string.Empty;
        MissingFromDiskCount = 0;
        MissingFromDiskPrograms = string.Empty;
        SourcesGivenUpText = string.Empty;
        ScanSignalText = string.Empty;
        PendingRebootResult = null;
        HasScanned = false;
    }

    /// <summary>
    /// Takes the result off the view model where the scan after a Move or Delete does
    /// not run, because the window is closing. The result goes as it goes when that
    /// scan ends without one (<see cref="RefreshAsync"/>), and the window says that
    /// scan did not finish. Nothing failed, so the message names no crash log, and no
    /// scan is reported cancelled: <see cref="LastScanWasCancelled"/> is cleared, as a
    /// Re-scan the user cancelled earlier leaves it set over the list it kept.
    /// </summary>
    public void DropResultWithoutRescan()
    {
        LastScanWasCancelled = false;
        UnfinishedRefreshMessage = Strings.Body_RescanNotFinished_Why;
        DropResult();
    }

    /// <summary>
    /// Clears <see cref="LastScanWasCancelled"/> where the window is closing as the scan
    /// after a Move or Delete ends, so no scan is reported cancelled, as where the close
    /// lands before that scan (<see cref="DropResultWithoutRescan"/>). Everything else
    /// the scan left stands: the result it found, or the message saying it did not
    /// finish, which names the crash log where the scan failed and the log was written
    /// (<see cref="RefreshAsync"/>).
    /// </summary>
    public void ClearCancelAsTheWindowCloses() => LastScanWasCancelled = false;

    /// <summary>
    /// The scan's one error ladder: maps a scan (or act-time re-verify) failure to
    /// the message, dialog title and overlay status line the user should see, so
    /// the user-driven Scan command, the startup scan and the re-verify all
    /// diagnose a failure the same way instead of each inventing its own. The
    /// two arms that write the crash log name its path in the message they return,
    /// and this is called once per failure, so neither writes twice.
    /// <see cref="OperationCanceledException"/> is handled by its own catch and
    /// never reaches here.
    /// </summary>
    internal ScanFailure DescribeScanFailure(Exception ex) => ex switch
    {
        // LocalisedAccessException before UnauthorizedAccessException: it derives
        // from it and carries a precise, safe-to-echo resx message naming what
        // Windows refused, where the BCL type only earns the general message that
        // Windows refused access.
        LocalisedAccessException =>
            new(ex.Message, Strings.Error_AdminRequiredTitle, IsError: false, Strings.Status_ScanAccessDenied),
        UnauthorizedAccessException =>
            new(Strings.Error_AdminRequiredBody, Strings.Error_AdminRequiredTitle, IsError: false, Strings.Status_ScanAccessDenied),
        LocalisedInvalidOperationException => DescribeDeliberateStop(ex),
        _ => DescribeUnexpectedScanFailure(ex),
    };

    /// <summary>
    /// A stop the app decided on for itself, at a scan or at the check made just
    /// before a Move or Delete. The stops have different causes, and the message is a
    /// full account of this one, built from the app's own strings, so it is safe to
    /// show. The heading and the status line are the same for every stop and name no
    /// cause.
    ///
    /// IT GOES TO THE CRASH LOG AS WELL, WHICH IS THE WHOLE REASON THIS ARM IS A
    /// METHOD. The account is shown and then gone: on the overlay it lasts as long
    /// as the window, and at the two act-time callers it is a dialog the reader
    /// dismisses. Writing it leaves a file that can be attached to a report and read
    /// back afterwards. The command line reaches the same condition through its own
    /// catch and writes the reason to the Windows event log.
    ///
    /// The closing sentence is added only where the write succeeded, following the
    /// generic branch's two forms: a message naming a file that was never written
    /// sends somebody looking for it.
    /// </summary>
    private static ScanFailure DescribeDeliberateStop(Exception ex)
    {
        var crash = CrashLog.TryWrite(ex);
        var message = crash.Written
            ? ex.Message + Environment.NewLine + Environment.NewLine
                + string.Format(Strings.Error_ScanStoppedDetails, crash.Path)
            : ex.Message;
        return new ScanFailure(message, Strings.Error_StoppedTitle,
            IsError: true, Strings.Status_ScanStopped);
    }

    private static ScanFailure DescribeUnexpectedScanFailure(Exception ex)
    {
        // ex.Message never reaches UI: type name plus log path only, because a
        // framework message from an elevated process can carry a path out of
        // another user's profile.
        var crash = CrashLog.TryWrite(ex);
        var typeName = ex.GetType().Name;
        // THE PATH GOES IN RAW AND THE BREAK OPPORTUNITIES ARE ADDED WHERE IT IS
        // DRAWN. This string reaches four surfaces: the intro line, through
        // InstallerPathTextConverter; on a Re-scan, the error box's body, through
        // InstallerPathText.ForDrawing, and the scanning card's line under that box,
        // through AnyPathTextConverter, which gives the path the same treatment; and
        // the scan announcer, which is never drawn and takes it unbound. Inserting
        // them here would hand a speech engine invisible format characters for a
        // layout that does not exist.
        var message = crash.Written
            ? string.Format(Strings.Status_ScanFailedDetails, typeName, crash.Path)
            : string.Format(Strings.Status_ScanFailedDetails_NoLog, typeName);
        return new ScanFailure(message, Strings.Error_ScanFailedTitle, IsError: true, message);
    }

    /// <summary>One rung of the scan error ladder: what to show and how.</summary>
    internal readonly record struct ScanFailure(string Message, string Title, bool IsError, string StatusLine);

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        // The two messages stay until this scan has an outcome of its own. A
        // cancel leaves the window as it was before the click, whichever state
        // that was.
        LastScanWasCancelled = false;
        _statusWait.Forget();
        ScanProgress = Strings.Status_StartingScan;
        ScanTicker = string.Empty;
        var sw = Stopwatch.StartNew();
        var cts = new CancellationTokenSource();
        _scanCts = cts;

        try
        {
            // Throttled on the same terms as the startup scan's: the ticker fires
            // once per installed program and many times while the files are
            // counted, and each update that gets through crosses to the
            // dispatcher and replaces the overlay's second line.
            var progress = new ThrottledScanProgress(
                new Progress<ScanProgressUpdate>(ApplyProgressUpdate));
            var scanTask = RunScanCoreAsync(progress, cts.Token);
            if (await Task.WhenAny(scanTask, Task.Delay(200, cts.Token)) != scanTask)
                IsScanning = true;
            await scanTask;

            sw.Stop();
            LastScanDurationMs = sw.ElapsedMilliseconds;
            LastScanError = string.Empty;
            UnfinishedRefreshMessage = string.Empty;
            ScanProgress = string.Format(Strings.Status_ScanComplete, DisplayHelpers.FormatElapsed(sw.Elapsed));
            ScanCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            LastScanWasCancelled = true;
            ScanProgress = Strings.Status_ScanCancelled;
        }
        catch (Exception ex)
        {
            // One error ladder, shared with the startup scan (which shows the
            // message inline in the window rather than a modal) and the act-time
            // re-verify. LastScanError is set on every path so the main window can
            // reflect a failed Re-scan the same way it reflects a failed startup
            // scan; the modal is the immediate feedback for the explicit click.
            //
            // The earlier scan's result goes, so the window under the modal is the
            // one a failed startup scan opens: the message, Re-scan, and nothing
            // from a list this scan did not produce. A cancel, in the arm above,
            // keeps it.
            var failure = DescribeScanFailure(ex);
            UnfinishedRefreshMessage = string.Empty;
            LastScanErrorTitle = failure.Title;
            LastScanError = failure.Message;
            DropResult();
            ScanProgress = failure.StatusLine;
            if (failure.IsError)
                _dialogService.ShowError(failure.Message, failure.Title);
            else
                _dialogService.ShowWarning(failure.Message, failure.Title);
        }
        finally
        {
            // Capture, take off, dispose: a CancelScanCommand reading
            // _scanCts afterwards finds no source to cancel.
            var local = _scanCts;
            _scanCts = null;
            local?.Dispose();
            IsScanning = false;
        }
    }

    /// <summary>
    /// Routes one scan progress update to the overlay's two lines:
    /// milestones to the announced status text, the ticker to the
    /// display-only line beneath it. A milestone also clears the ticker
    /// so the last program name or count does not sit stale beside the
    /// next phase's message.
    /// </summary>
    private void ApplyProgressUpdate(ScanProgressUpdate update)
    {
        // A wait takes the announced status text and gives it back when it ends, leaving
        // the ticker as it was. Once Cancel is pressed a wait is neither shown nor put
        // back, so "Cancelling..." stays in front of the reader.
        if (update.IsWait)
        {
            if (_scanCts?.IsCancellationRequested != true) ShowWait(update);
            return;
        }

        if (update.IsMilestone)
        {
            _statusWait.Forget();
            ScanProgress = update.Message;
            ScanTicker = string.Empty;
        }
        else
        {
            ScanTicker = update.Message;
        }
    }

    /// <summary>The waits the scan reports, shown on <see cref="ScanProgress"/>.</summary>
    private readonly WaitLine _statusWait;

    /// <summary>
    /// Shows <paramref name="update"/> on <see cref="ScanProgress"/>, the wait it carries
    /// going up with its line, and the end of a wait taking the line and its button back.
    /// </summary>
    private void ShowWait(ScanProgressUpdate update)
    {
        _waitTakingTheLine = update.Wait;
        try
        {
            _statusWait.Show(update);
        }
        finally
        {
            _waitTakingTheLine = null;
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        // Set the status before cancelling. The synchronous write updates
        // the overlay the instant Esc is pressed (the ScanAsync progress
        // reporter only fires on its next callback). Ordering it before
        // _scanCts.Cancel() also guarantees ScanAsync's own
        // "Scan cancelled." write lands after this one: cancelling first
        // leaves a window where the scan can complete and write
        // "Scan cancelled." before this line overwrites it with
        // "Cancelling...", harmless on the single UI thread but a race in
        // a SynchronizationContext-free unit test.
        ScanProgress = Strings.Status_Cancelling;
        try { _scanCts?.Cancel(); }
        catch (ObjectDisposedException) { /* scan already finished */ }
    }

    /// <summary>
    /// Splash-driven startup scan. Caller controls the progress
    /// reporter (it pipes to the splash UI) and the cancellation token
    /// (it ties to the splash Cancel button). Raises
    /// <see cref="ScanCompleted"/> on success so MainViewModel can
    /// trigger the all-clear path if appropriate.
    /// </summary>
    public async Task ScanWithProgressAsync(IProgress<ScanProgressUpdate>? progress, CancellationToken cancellationToken = default)
    {
        LastScanWasCancelled = false;
        LastScanError = string.Empty;
        UnfinishedRefreshMessage = string.Empty;
        var sw = Stopwatch.StartNew();
        try
        {
            await RunScanCoreAsync(progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancelling the splash leaves the app with no scan at all, and the
            // main window says so. Recording it here (the caller lets the
            // cancellation through to close the splash) is what lets that window
            // say why, rather than showing a bare "nothing scanned yet" to a user
            // who knows perfectly well they pressed Cancel.
            LastScanWasCancelled = true;
            throw;
        }
        catch (Exception ex)
        {
            // A failed startup scan opens the main window in an error state rather
            // than exiting: record the tailored message through the one ladder,
            // shared with the Scan command, so the window's not-yet-scanned state
            // shows it with Re-scan focused. Do NOT rethrow: the exception then
            // propagates to App.OnStartup, which tells an already-elevated user to
            // run as administrator and exits. An app that diagnoses "your
            // installer database is empty" and then vanishes is strictly worse than
            // one that says it and offers Re-scan.
            //
            // No scan has completed before this one, so there is nothing to drop,
            // and the call is here so that every scan that fails ends the same way.
            var failure = DescribeScanFailure(ex);
            LastScanErrorTitle = failure.Title;
            LastScanError = failure.Message;
            DropResult();
            return;
        }
        sw.Stop();
        LastScanDurationMs = sw.ElapsedMilliseconds;
        ScanProgress = string.Format(Strings.Status_ScanComplete, DisplayHelpers.FormatElapsed(sw.Elapsed));
        ScanCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Re-checks the pending-reboot gate at the moment of action, off the
    /// dispatcher, and updates <see cref="PendingRebootResult"/> so the banner
    /// paints and the Move/Delete commands drop out through the existing
    /// <see cref="HasPendingReboot"/> wiring. Returns true when the gate now
    /// blocks. The scan samples the gate once; a Windows Installer transaction
    /// that starts after that sample, hours later if the window sits open, is
    /// invisible until this runs immediately before a Move or Delete acts.
    /// Mirrors the scan's own off-dispatcher Check() so a registry or mutex read
    /// cannot stall the UI thread.
    /// </summary>
    public async Task<bool> RecheckPendingRebootAsync()
    {
        var result = await Task.Run(() => _rebootService.Check());
        PendingRebootResult = result;
        return result.IsBlocked;
    }

    /// <summary>
    /// The silent scan Cleanup runs at the end of a Move or Delete. Skips the scan
    /// overlay (IsScanning stays false) so the operating overlay can stay visible
    /// until its own finally block clears it.
    ///
    /// <paramref name="cancellationToken"/> is what makes it interruptible, and
    /// it is not optional in practice: this is a full folder walk plus a full
    /// API enumeration on a folder that can hold millions of files, and the
    /// caller runs it behind an overlay the user has usually just pressed
    /// Cancel on. Without a token every checkpoint inside the scan is unreachable
    /// and the wait reads as a hang.
    ///
    /// It never throws, because its callers run it behind the operating overlay
    /// and go on to report their batch whatever it does. A scan that ends without
    /// a result, by a failure or by the user's cancel, drops the result on screen
    /// instead: that list describes the folder as it stood before the batch, so it
    /// is not a list the window may show or Move and Delete may act on. The
    /// summary card, where the batch shows one, is filled from the batch's own
    /// tally and is unaffected. The window says the scan did not finish, in
    /// <see cref="UnfinishedRefreshMessage"/>.
    /// </summary>
    /// <param name="progress">
    /// Told what the scan reports, for a caller that shows its waits on source folders
    /// (<see cref="ScanProgressUpdate.IsWait"/>) behind its own overlay.
    /// </param>
    public async Task RefreshAsync(
        CancellationToken cancellationToken = default, IProgress<ScanProgressUpdate>? progress = null)
    {
        LastScanWasCancelled = false;
        LastScanError = string.Empty;
        UnfinishedRefreshMessage = string.Empty;
        try
        {
            await RunScanCoreAsync(progress, cancellationToken);
            ScanCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // Not written to crash.log, unlike the failure below, because the
            // user asked for it. LastScanWasCancelled puts "Scan cancelled." under
            // the message, as it does in the window a cancelled startup scan
            // leaves, except where the window is closing
            // (ClearCancelAsTheWindowCloses).
            UnfinishedRefreshMessage = Strings.Body_RescanNotFinished_Why;
            LastScanWasCancelled = true;
            DropResult();
        }
        catch (Exception ex)
        {
            // The account goes to crash.log and the window names the file, in the
            // two forms DescribeDeliberateStop uses: a message naming a file that
            // was never written sends somebody looking for it.
            var crash = CrashLog.TryWrite(ex);
            UnfinishedRefreshMessage = crash.Written
                ? Strings.Body_RescanNotFinished_Why + Environment.NewLine + Environment.NewLine
                    + string.Format(Strings.Body_RescanNotFinished_Recorded, crash.Path)
                : Strings.Body_RescanNotFinished_Why;
            DropResult();
        }
    }
}
