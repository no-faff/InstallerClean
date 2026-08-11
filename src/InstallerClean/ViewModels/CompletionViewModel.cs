using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// What a completed Move did to the disk. The completion card needs it in two
/// places that do not answer the same question: the heading verb ("freed" only
/// where the system drive is genuinely emptier) and the body's advice about
/// when the space comes back. A same-drive move is not the only one that frees
/// nothing at that instant, so the two cannot be derived from each other.
/// </summary>
public enum MoveSpaceOutcome
{
    /// <summary>Another volume: the system drive is genuinely emptier now.</summary>
    FreedSpace,

    /// <summary>
    /// The drive the files came from. A rename, so nothing is reclaimed until
    /// the backup folder is deleted or moved off the drive.
    /// </summary>
    SameDrive,

    /// <summary>
    /// A volume the classification could not read. The heading claims no
    /// freed space and the body makes no claim about the drive.
    /// </summary>
    Unclassified,
}

/// <summary>
/// Completion-screen slice. Holds the heading / summary / restore-hint
/// / errors block shown after a scan-with-no-orphans, a successful
/// move or a successful delete. The rescan command runs the
/// <c>rescanRequested</c> constructor delegate so this VM stays
/// ignorant of the scan service. The Send-result button on the same
/// overlay routes through <see cref="IResultLogService"/> and
/// <see cref="IConfirmationService"/>.
///
/// Visibility of the Send button is gated by three independent locks:
///
///   - Lifetime lock: <c>AppSettings.HasSentResultLog</c> on disk.
///     Set to true on a successful POST, never cleared. The flag
///     survives version upgrades and the prompt does not return on
///     a later session. Documented in <see cref="AppSettings.HasSentResultLog"/>.
///
///   - Session lock: <see cref="_promptShownThisSession"/>. The first
///     <see cref="MarkResultLogReady"/> call in a session sets the
///     flag; later calls no-op. Each session offers the prompt at
///     most once.
///
///   - One-shot suppression: <see cref="SuppressNextResultLogPrompt"/>
///     set by <c>RescanAfterCompletion</c>. The all-clear that
///     immediately follows a rescan-from-overlay skips the WriteAsync
///     and MarkResultLogReady call so the rescan's empty result does
///     not overwrite the prior Move/Delete payload in
///     <c>last-run.json</c>.
/// </summary>
public partial class CompletionViewModel : ObservableObject
{
    [ObservableProperty] private bool _isComplete;
    [ObservableProperty] private string _heading = string.Empty;
    [ObservableProperty] private string _summary = string.Empty;
    [ObservableProperty] private string _restore = string.Empty;

    /// <summary>
    /// Move-only: the raw destination path embedded in <see cref="Summary"/>,
    /// so the WPF host can locate it within the formatted sentence and force
    /// it onto its own line (mirroring the move-confirmation dialog's
    /// destination-on-its-own-line treatment). Empty on every other
    /// completion path (delete, all-clear) so the host falls back to
    /// rendering Summary verbatim. Must be set before <see cref="Summary"/>
    /// in <see cref="ShowMoveSummary"/>: the WPF host rebuilds the summary
    /// line's inlines from Summary's PropertyChanged, so this needs its
    /// final value in place before that setter raises. Cleared everywhere
    /// else because the view-model instance is reused across operations.
    /// </summary>
    [ObservableProperty] private string _summaryDestination = string.Empty;

    /// <summary>
    /// The failure count line ("2 of 71 could not be moved."), shown in
    /// the warning colour directly under the heading and empty on every run
    /// that failed at nothing, which collapses the bound TextBlock. It exists
    /// as its own zone because either of the two lines that could carry it
    /// instead merges two messages into one: on the heading, the outcome plus a
    /// trailing "some files could not be processed" clause that clips; on the
    /// summary line, the destination path followed by an error count that reads
    /// as part of the folder name. Cleared everywhere else, because the
    /// view-model instance is reused across operations.
    /// </summary>
    [ObservableProperty] private string _failedCount = string.Empty;

    /// <summary>
    /// Paints the heading in the warning colour instead of the success green.
    /// True only where the operation acted on nothing and had errors, which is
    /// the one outcome the size headings cannot state honestly ("0 B freed" is
    /// a result, not a failure). A partial failure keeps the success heading:
    /// something was genuinely freed, and <see cref="FailedCount"/> carries the
    /// rest. A cancelled run is never a failure, so it keeps it too.
    /// </summary>
    [ObservableProperty] private bool _headingIsWarning;

    [ObservableProperty] private string _errors = string.Empty;

    /// <summary>
    /// Line shown when the act-time re-verify left some candidates out of the
    /// batch, and saying which of its two reasons applied: a program has needed
    /// them again since the scan, or the re-verify could not read the records
    /// well enough to judge. Empty on every path where the re-verify dropped
    /// nothing (the common case), which collapses the bound TextBlock. Set
    /// through the <c>reverify</c> argument of the Show* summary methods and
    /// cleared everywhere else, because the view-model instance is reused across
    /// operations.
    /// </summary>
    [ObservableProperty] private string _skipped = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSendResultLogVisible))]
    private bool _isResultLogReady;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSendResultLogVisible))]
    private bool _isSendingResultLog;

    [ObservableProperty] private string _resultLogStatusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SendResultLogTooltip))]
    private bool _lastResultFreedNothing;

    /// <summary>
    /// Shows the donate heart in the completion card's corner. True only
    /// when the operation actually shifted files, so the ask only ever
    /// follows work done: an all-clear, a run the re-verify held back
    /// entirely, and a Move or Delete that reached no file all leave it
    /// false. It measures the bytes the run moved or deleted, NOT whether
    /// the disk got any emptier, so a same-drive Move earns the heart on
    /// the strength of having done what the user asked, while its heading
    /// still says "moved" rather than "freed" because nothing was
    /// reclaimed. Set from the bytes argument in each Show* method rather
    /// than derived from <see cref="LastResultFreedNothing"/>, which
    /// happens to agree today but answers a different question (it picks
    /// the send-report tooltip's wording) and is free to diverge.
    /// </summary>
    [ObservableProperty] private bool _showDonate;

    private readonly bool _alreadySentBeforeThisSession;
    private bool _resultLogSentThisSession;
    private bool _promptShownThisSession;
    private bool _skipNextResultLogPrompt;
    private bool _sendInFlight;

    /// <summary>
    /// Visible when a fresh log exists for the operation just
    /// completed and the user has not already sent or dismissed one
    /// this session. The lifetime-lock clause is belt-and-braces:
    /// MarkResultLogReady upstream of this getter also gates on the
    /// same flag. The "once per machine, ever" privacy contract is
    /// worth two independent checks.
    /// </summary>
    public bool IsSendResultLogVisible =>
        IsResultLogReady && !_resultLogSentThisSession && !IsSendingResultLog
        && !_alreadySentBeforeThisSession;

    /// <summary>
    /// Trigger property the MainViewModel listens to so it persists the
    /// lifetime lock. Read-side returns true after any click outcome
    /// (the session lock fires on success and failure both), but the
    /// PropertyChanged event is only raised on a Sent outcome, so the
    /// persistence handler only fires for actual transmissions. The
    /// asymmetry is what stops a transient timeout permanently locking
    /// a user out without anything reaching the receiver.
    /// </summary>
    public bool HasSentResultLog => _resultLogSentThisSession;

    /// <summary>
    /// True when the result-log surface is shut down for any reason:
    /// the user already sent in a previous session (lifetime lock from
    /// settings) or the session lock has fired this run. Consumed by
    /// CleanupViewModel and the all-clear handler in MainViewModel to
    /// skip the last-run.json write when nobody will ever send the
    /// file: the disk I/O is otherwise paid on every Move and Delete
    /// for the rest of the session even though the Send button stays
    /// hidden.
    /// </summary>
    public bool IsResultLogLocked =>
        _alreadySentBeforeThisSession || _resultLogSentThisSession;

    /// <summary>
    /// Tooltip text for the Send button. Switches to the "please
    /// send even if nothing was found" variant when the last completion
    /// produced zero bytes freed (all-clear scan, or a Move/Delete that
    /// found nothing to operate on), so the all-clear cohort isn't
    /// silently filtered out of the aggregate.
    /// </summary>
    public string SendResultLogTooltip =>
        LastResultFreedNothing
            ? Strings.Tooltip_SendResultLog_NothingFound
            : Strings.Tooltip_SendResultLog;

    private readonly Func<Task>? _rescanRequested;
    private readonly IResultLogService? _resultLogService;
    private readonly IConfirmationService? _confirmationService;

    /// <summary>
    /// <paramref name="rescanRequested"/> is an awaitable run-a-scan
    /// hook. <paramref name="resultLogService"/> writes, reads and
    /// sends the post-cleanup diagnostic log. <paramref name="confirmationService"/>
    /// shows the modal that lets the user see exactly what would be
    /// sent before pressing Send. <paramref name="hasSentBefore"/> is
    /// the persisted lifetime flag (<see cref="AppSettings.HasSentResultLog"/>)
    /// read once at construction. All services are optional so unit
    /// tests can construct a bare view-model.
    /// </summary>
    public CompletionViewModel(
        Func<Task>? rescanRequested = null,
        IResultLogService? resultLogService = null,
        IConfirmationService? confirmationService = null,
        bool hasSentBefore = false)
    {
        _rescanRequested = rescanRequested;
        _resultLogService = resultLogService;
        _confirmationService = confirmationService;
        _alreadySentBeforeThisSession = hasSentBefore;
    }

    /// <summary>Shows the "All clean" state after a scan finds no orphans.
    /// <paramref name="installedProductCount"/> is the registered-package
    /// count surfaced as the scan receipt; <paramref name="scanDurationMs"/>
    /// is the elapsed scan time. The count and the duration together stop
    /// the all-clean overlay from reading as "did nothing" on a fast
    /// machine where the duration alone shows as a fraction of a second.</summary>
    public void ShowAllClear(int installedProductCount, long scanDurationMs)
    {
        HeadingIsWarning = false;
        Heading = Strings.Completion_AllClean;
        FailedCount = string.Empty;
        SummaryDestination = string.Empty;
        Summary = Strings.Completion_NothingToCleanUp;
        Restore = string.Format(
            Strings.Completion_NothingToCleanUpReceipt,
            installedProductCount,
            DisplayHelpers.PluraliseProduct(installedProductCount),
            DisplayHelpers.FormatElapsedLong(TimeSpan.FromMilliseconds(scanDurationMs)));
        Errors = string.Empty;
        Skipped = string.Empty;
        ResultLogStatusMessage = string.Empty;
        LastResultFreedNothing = true;
        ShowDonate = false;
        IsComplete = true;
    }

    /// <summary>
    /// Shows the state where the scan completed, kept every candidate back, and did
    /// so because this machine installs a product as a second instance of itself.
    ///
    /// SEPARATE FROM <see cref="ShowAllClear"/> AND THAT IS THE WHOLE POINT. Both
    /// end with an empty offer and they are not the same finding: one says the
    /// folder holds nothing to remove, the other says the app could not tell. Until
    /// this existed the second was shown as the first, so a machine with files the
    /// app could not vouch for read as a machine with nothing to vouch for.
    ///
    /// The heading is not a warning colour. Nothing is wrong with the machine and
    /// there is nothing for the user to do, which the body says in as many words.
    /// </summary>
    /// <param name="heldBackCount">
    /// The files that would otherwise have been offered, not everything in the
    /// folder. The folder total is mostly registered files that would never be
    /// offered on any machine, and printing it on the screen that tells somebody
    /// they are getting nothing would imply that much was going spare.
    /// </param>
    public void ShowNothingOffered(int heldBackCount, string heldBackSize,
        int installedProductCount, long scanDurationMs)
    {
        HeadingIsWarning = false;
        Heading = Strings.Completion_NothingOffered;
        FailedCount = string.Empty;
        SummaryDestination = string.Empty;
        Summary = string.Format(
            Strings.Completion_NothingOfferedBody, heldBackCount, heldBackSize);
        Restore = string.Format(
            Strings.Completion_NothingToCleanUpReceipt,
            installedProductCount,
            DisplayHelpers.PluraliseProduct(installedProductCount),
            DisplayHelpers.FormatElapsedLong(TimeSpan.FromMilliseconds(scanDurationMs)));
        Errors = string.Empty;
        Skipped = string.Empty;
        ResultLogStatusMessage = string.Empty;
        LastResultFreedNothing = true;
        ShowDonate = false;
        IsComplete = true;
    }

    /// <summary>
    /// The "N files kept in place" block for a completion overlay, or empty when
    /// the act-time re-verify kept nothing back. Shown alongside the Move/Delete
    /// summary so the totals still add up (acted on + kept = the scan's
    /// candidates).
    ///
    /// The reasons come from the re-verify, never from the count, and there is one
    /// line per cause that occurred rather than one sentence for the batch. A
    /// re-verify that could not read the records keeps files back without any
    /// program having reclaimed them, so a batch that met both causes and reported
    /// either alone would state a cause that did not occur for some of its files.
    /// The ordering and the sentences are Core's
    /// (<see cref="HeldBackReport.Lines"/>), because the command line prints the
    /// same partition and the two must not answer differently for one machine
    /// state.
    ///
    /// Joined the way <see cref="FormatErrorBreakdown"/> joins its own rows, and
    /// read by the same kind of wrapping TextBlock. The all-skipped overlay routes
    /// this through <see cref="Summary"/> instead, whose inlines are composed in
    /// the window's code-behind, and that is where the line breaks are made
    /// explicit.
    /// </summary>
    private static string SkippedText(ReverifyResult? reverify)
    {
        if (reverify is null) return string.Empty;
        return string.Join(Environment.NewLine, HeldBackReport.Lines(reverify.Reasons));
    }

    /// <summary>
    /// The failure count line for a completion overlay, or empty when nothing
    /// failed. The denominator is what the operation TRIED
    /// (<paramref name="actedCount"/> + <paramref name="failedCount"/>), not
    /// the batch it was handed: on a run the user cancelled, the files it never
    /// reached are not files that could not be processed, and on a run that went
    /// the distance the two numbers are the same anyway. So one formula is
    /// honest on both, and the cancelled summary line below still names the
    /// full batch it was working through.
    /// </summary>
    private static string FailedCountText(int actedCount, int failedCount, bool deleting)
    {
        if (failedCount == 0) return string.Empty;

        var (singular, plural, key) = deleting
            ? (Strings.Completion_FailedCountDelete_Singular,
               Strings.Completion_FailedCountDelete_Plural,
               "Completion.FailedCountDelete")
            : (Strings.Completion_FailedCount_Singular,
               Strings.Completion_FailedCount_Plural,
               "Completion.FailedCount");

        return string.Format(
            DisplayHelpers.Pluralise(failedCount, singular, plural, key),
            failedCount, actedCount + failedCount);
    }

    /// <summary>
    /// The line under a Move summary: the backup folder is safe to delete, and
    /// what deleting it does about the space. Picks between four strings on the
    /// two things that vary, whether the folder is on the drive the files came
    /// from and whether it holds one file or several.
    /// </summary>
    private static string MoveRestoreText(int movedCount, MoveSpaceOutcome space) =>
        space == MoveSpaceOutcome.SameDrive
            ? DisplayHelpers.Pluralise(movedCount,
                Strings.Completion_MoveRestoreHintSameDrive_Singular,
                Strings.Completion_MoveRestoreHintSameDrive_Plural,
                "Completion.MoveRestoreHintSameDrive")
            : DisplayHelpers.Pluralise(movedCount,
                Strings.Completion_MoveRestoreHint_Singular,
                Strings.Completion_MoveRestoreHint_Plural,
                "Completion.MoveRestoreHint");

    /// <summary>
    /// Shows the post-Move summary including any per-file errors.
    /// <paramref name="space"/> picks the heading verb and the restore line: a
    /// move to another volume genuinely frees space on the system drive, so it
    /// reads "freed"; a same-volume move is a rename that frees nothing until
    /// the parked folder is deleted, so it reads "moved" and its restore line
    /// says how to get the space back. An unclassifiable destination takes the
    /// claim-less verb and the restore line that names no drive.
    /// </summary>
    public void ShowMoveSummary(int movedCount, long movedBytes, string destination,
        IReadOnlyList<FileOperationError> errors, MoveSpaceOutcome space, ReverifyResult? reverify = null)
    {
        // The heading states one outcome and only one. A partial failure still
        // freed what it freed, so it keeps the size heading and lets the count
        // line below carry the failures; only a Move that got nowhere swaps to
        // the warning heading, because "0 B freed" would report that as a
        // result.
        HeadingIsWarning = errors.Count > 0 && movedCount == 0;
        Heading = HeadingIsWarning
            ? Strings.Completion_NothingMoved
            : string.Format(
                space == MoveSpaceOutcome.FreedSpace ? Strings.Completion_Freed : Strings.Completion_Moved,
                DisplayHelpers.FormatSize(movedBytes));
        FailedCount = FailedCountText(movedCount, errors.Count, deleting: false);
        var movedLabel = DisplayHelpers.PluraliseFile(movedCount);
        // SummaryDestination must be set before Summary: the WPF host rebuilds
        // the summary line's inlines (splitting the sentence at this substring
        // to force the path onto its own line) from Summary's PropertyChanged,
        // so SummaryDestination needs its final value in place before that
        // setter raises.
        SummaryDestination = HeadingIsWarning ? string.Empty : destination;
        // One summary sentence: what moved and where it went. The error count
        // lives in FailedCount now, because appended to this line it read as
        // part of the destination folder's name. Suppressed entirely when
        // nothing moved, because "0 files moved to: D:\Backup" describes files
        // arriving somewhere none of them reached; the destination is still on
        // screen in the backup-folder box behind the overlay.
        Summary = HeadingIsWarning
            ? string.Empty
            : string.Format(DisplayHelpers.Pluralise(movedCount,
                    Strings.Completion_MoveSummary_Singular,
                    Strings.Completion_MoveSummary_Plural,
                    "Completion.MoveSummary"),
                movedCount, movedLabel, destination);
        // No restore line when nothing moved: it is about files sitting in the
        // backup folder, and there are none.
        Restore = HeadingIsWarning ? string.Empty : MoveRestoreText(movedCount, space);
        Errors = errors.Count > 0 ? FormatErrorBreakdown(errors) : string.Empty;
        Skipped = SkippedText(reverify);
        ResultLogStatusMessage = string.Empty;
        LastResultFreedNothing = movedBytes <= 0;
        ShowDonate = movedBytes > 0;
        IsComplete = true;
    }

    /// <summary>
    /// Shows the post-Delete summary including any per-file errors. Two lines
    /// and no third: the heading reads "freed", because a delete reclaims the
    /// disk at the instant it happens, and the summary states what was done.
    ///
    /// <see cref="Restore"/> is deliberately left empty, which is the one thing
    /// about this screen worth defending. The app's claim is not that a delete
    /// can be undone, it is that these files do not need undoing, so a recovery
    /// line here would be the app hedging against the thing it has just said
    /// will not happen. That also leaves the screen with no safety claim on it,
    /// which is why it is the one completion state carrying no link.
    /// </summary>
    public void ShowDeleteSummary(int deletedCount, long deletedBytes,
        IReadOnlyList<FileOperationError> errors, ReverifyResult? reverify = null)
    {
        // See ShowMoveSummary for why a partial failure keeps the size heading
        // and only a delete that reached no file at all swaps to the warning.
        HeadingIsWarning = errors.Count > 0 && deletedCount == 0;
        Heading = HeadingIsWarning
            ? Strings.Completion_NothingDeleted
            : string.Format(Strings.Completion_Freed, DisplayHelpers.FormatSize(deletedBytes));
        FailedCount = FailedCountText(deletedCount, errors.Count, deleting: true);
        SummaryDestination = string.Empty;
        var deletedLabel = DisplayHelpers.PluraliseFile(deletedCount);
        // Suppressed when nothing was deleted: see ShowMoveSummary. "0 files
        // permanently deleted" reports an act that did not happen.
        Summary = HeadingIsWarning
            ? string.Empty
            : string.Format(DisplayHelpers.Pluralise(deletedCount,
                    Strings.Completion_PermanentDeleteSummary_Singular,
                    Strings.Completion_PermanentDeleteSummary_Plural,
                    "Completion.PermanentDeleteSummary"),
                deletedCount, deletedLabel);
        Restore = string.Empty;
        Errors = errors.Count > 0 ? FormatErrorBreakdown(errors) : string.Empty;
        Skipped = SkippedText(reverify);
        ResultLogStatusMessage = string.Empty;
        LastResultFreedNothing = deletedBytes <= 0;
        ShowDonate = deletedBytes > 0;
        IsComplete = true;
    }

    /// <summary>
    /// Shows the completion summary for a Move the user cancelled part-way
    /// through. <paramref name="movedCount"/> of <paramref name="totalCount"/>
    /// files were moved before the stop, and <paramref name="movedBytes"/> is the
    /// size of just those. The heading states what was accomplished, not the
    /// "some files could not be processed" of a partial FAILURE (a cancel is not
    /// one); the summary line says it was a cancel, and the moved files still get
    /// the restore reassurance because they are the ones that reached the
    /// destination.
    /// </summary>
    public void ShowMoveCancelledSummary(int movedCount, int totalCount, long movedBytes,
        IReadOnlyList<FileOperationError> errors, MoveSpaceOutcome space, ReverifyResult? reverify = null)
    {
        HeadingIsWarning = false;
        Heading = string.Format(
            space == MoveSpaceOutcome.FreedSpace ? Strings.Completion_Freed : Strings.Completion_Moved,
            DisplayHelpers.FormatSize(movedBytes));
        FailedCount = FailedCountText(movedCount, errors.Count, deleting: false);
        SummaryDestination = string.Empty;
        Summary = string.Format(
            DisplayHelpers.Pluralise(totalCount, Strings.Completion_MoveCancelledSummary, "Completion.MoveCancelledSummary"),
            movedCount, totalCount, DisplayHelpers.PluraliseFile(totalCount));
        // Same rule as ShowMoveSummary: the restore line is about files sitting
        // in the backup folder, and a cancel that came before the first file
        // moved put none there. Reachable because a cancel with zero moved and
        // a non-zero error count still raises a summary.
        Restore = movedCount == 0 ? string.Empty : MoveRestoreText(movedCount, space);
        Errors = errors.Count > 0 ? FormatErrorBreakdown(errors) : string.Empty;
        Skipped = SkippedText(reverify);
        ResultLogStatusMessage = string.Empty;
        LastResultFreedNothing = movedBytes <= 0;
        ShowDonate = movedBytes > 0;
        IsComplete = true;
    }

    /// <summary>
    /// Shows the completion summary for a Delete the user cancelled part-way
    /// through: <paramref name="deletedCount"/> of <paramref name="totalCount"/>
    /// files were deleted before the stop. The heading states what was
    /// accomplished rather than reading as a failure, because a cancel is not
    /// one, and the screen carries no restore line for the same reason
    /// <see cref="ShowDeleteSummary"/> does not.
    /// </summary>
    public void ShowDeleteCancelledSummary(int deletedCount, int totalCount, long deletedBytes,
        IReadOnlyList<FileOperationError> errors, ReverifyResult? reverify = null)
    {
        HeadingIsWarning = false;
        Heading = string.Format(Strings.Completion_Freed, DisplayHelpers.FormatSize(deletedBytes));
        FailedCount = FailedCountText(deletedCount, errors.Count, deleting: true);
        SummaryDestination = string.Empty;
        Summary = string.Format(
            DisplayHelpers.Pluralise(totalCount, Strings.Completion_PermanentDeleteCancelledSummary, "Completion.PermanentDeleteCancelledSummary"),
            deletedCount, totalCount, DisplayHelpers.PluraliseFile(totalCount));
        Restore = string.Empty;
        Errors = errors.Count > 0 ? FormatErrorBreakdown(errors) : string.Empty;
        Skipped = SkippedText(reverify);
        ResultLogStatusMessage = string.Empty;
        LastResultFreedNothing = deletedBytes <= 0;
        ShowDonate = deletedBytes > 0;
        IsComplete = true;
    }

    /// <summary>
    /// Shows the completion overlay when the act-time re-verify kept EVERY
    /// candidate back, so nothing was moved or deleted. No freed-size heading
    /// (nothing was freed); the summary IS the "N kept in place" message, and it
    /// names the same reason the per-operation line would have.
    /// </summary>
    public void ShowReverifyAllSkipped(ReverifyResult reverify)
    {
        HeadingIsWarning = false;
        // Not Completion_AllClean, which ShowAllClear uses correctly for a machine
        // with nothing to do. Here everything the user confirmed was kept back,
        // which is not the same as there having been nothing to remove, and the
        // screen previously read "All clean" over a summary naming the causes.
        Heading = Strings.Completion_NothingRemoved;
        FailedCount = string.Empty;
        SummaryDestination = string.Empty;
        // ONE CONDITION OVERRIDES THE PARTITION, and only this one, because it is
        // the only one that is not about the files. The machine gained a product
        // installed as a second instance of itself between the scan and the click,
        // so no file in the batch is the one at fault and the per-file causes below
        // have nothing to say about any of them. Every other outcome keeps its line
        // and its count.
        Summary = reverify.InstanceTransformsInUse
            ? Strings.Completion_InstanceRefusal
            : SkippedText(reverify);
        Restore = string.Empty;
        Errors = string.Empty;
        Skipped = string.Empty;
        ResultLogStatusMessage = string.Empty;
        LastResultFreedNothing = true;
        ShowDonate = false;
        IsComplete = true;
    }

    /// <summary>
    /// Marks a fresh result-log as available to send. No-op when the
    /// lifetime lock is set, when the session lock has fired (any click
    /// outcome this run), or when the prompt has already been offered
    /// once this session.
    /// </summary>
    public void MarkResultLogReady()
    {
        if (_alreadySentBeforeThisSession) return;
        if (_resultLogSentThisSession) return;
        if (_promptShownThisSession) return;
        _promptShownThisSession = true;
        IsResultLogReady = true;
    }

    /// <summary>
    /// One-shot flag set by <c>RescanAfterCompletion</c> so the all-clear
    /// that follows a rescan from the completion overlay doesn't
    /// re-write <c>last-run.json</c> with the rescan's empty result.
    /// </summary>
    public void SuppressNextResultLogPrompt() => _skipNextResultLogPrompt = true;

    /// <summary>Reads and clears the one-shot suppression flag.</summary>
    public bool ConsumeSuppressNextResultLogPrompt()
    {
        var s = _skipNextResultLogPrompt;
        _skipNextResultLogPrompt = false;
        return s;
    }

    [RelayCommand]
    private async Task SendResultLogAsync()
    {
        if (_resultLogService is null || _resultLogSentThisSession || _sendInFlight ||
            _alreadySentBeforeThisSession)
            return;

        // _sendInFlight gates re-entry across the modal await, which
        // IsSendingResultLog cannot cover because the latter would
        // flicker the button visible/invisible during the user's
        // confirmation step. Cleared in the finally so a Cancel from
        // the modal restores the ability to click again.
        _sendInFlight = true;
        try
        {
            // Read once; the same bytes feed the modal preview and the
            // POST. Reading from disk twice would let a concurrent
            // writer slip a different payload between the user's review
            // and the wire transmission.
            var jsonContent = await _resultLogService.ReadLastLogAsync().ConfigureAwait(true);
            if (jsonContent is null)
            {
                // File missing, oversize, or unreadable between the
                // post-operation write and the user's click. Treat as
                // a transient failure: session lock hides the button,
                // status takes its place, lifetime lock stays open so
                // the user is re-prompted on the next session.
                CrashLog.TryWrite(new InvalidOperationException(
                    "Send result clicked but last-run.json could not be read for preview."));
                _resultLogSentThisSession = true;
                IsResultLogReady = false;
                // Distinct from the post-POST failure copy: the silent-
                // skip path never opened the modal and never reached
                // the wire. "No log to send" tells the user the app
                // didn't try, vs "Didn't work" which implies it tried
                // and got refused.
                ResultLogStatusMessage = Strings.ResultLog_NothingToSend;
                return;
            }

            if (_confirmationService is { } confirm)
            {
                if (!confirm.ConfirmSendResultLog(jsonContent))
                    return;
            }

            IsSendingResultLog = true;
            ResultLogStatusMessage = Strings.ResultLog_Sending;
            ResultLogSendOutcome outcome;
            try
            {
                outcome = await _resultLogService.SendAsync(jsonContent)
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                // SendAsync documents never-throws, but the contract
                // sits across an assembly boundary. The catch collapses
                // any breach to the same visible failure state as a
                // clean reject so a regression can't ride
                // DispatcherUnhandledException to a process exit.
                //
                // No cancellation branch above this catch, because there is no
                // caller-driven cancel to handle: the send is passed no token,
                // and SendAsync maps its own HttpClient timeout to the Timeout
                // outcome and only rethrows a cancellation when the token it
                // was given was cancelled. A branch for one could not fire, and
                // would describe a send the user can abandon, which this is
                // not.
                CrashLog.TryWrite(ex);
                outcome = ResultLogSendOutcome.Unknown;
            }
            finally
            {
                IsSendingResultLog = false;
            }

            // Session lock flips on any click outcome so the button
            // does not reappear after a transient failure within the
            // same session. The lifetime lock only persists on a
            // successful transmission: a first-ever click that hits
            // a transient timeout (CGNAT blip, Netlify cold start,
            // captive-portal DNS) leaves the lifetime lock unset, so
            // the next session re-prompts rather than locking the
            // machine out with nothing ever reaching the receiver.
            _resultLogSentThisSession = true;
            IsResultLogReady = false;
            ResultLogStatusMessage = outcome == ResultLogSendOutcome.Sent
                ? Strings.ResultLog_Sent
                : Strings.ResultLog_Failed;
            if (outcome == ResultLogSendOutcome.Sent)
                OnPropertyChanged(nameof(HasSentResultLog));
        }
        finally
        {
            _sendInFlight = false;
        }
    }

    [RelayCommand]
    private void Dismiss()
    {
        IsComplete = false;
        Errors = string.Empty;
        FailedCount = string.Empty;
        IsResultLogReady = false;
        ResultLogStatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task RescanAfterCompletion()
    {
        IsComplete = false;
        Errors = string.Empty;
        FailedCount = string.Empty;
        IsResultLogReady = false;
        ResultLogStatusMessage = string.Empty;
        // The next ScanCompleted fires with this rescan in flight; an
        // all-clear that follows it skips the last-run.json overwrite
        // so the prior Move/Delete payload survives across the rescan.
        SuppressNextResultLogPrompt();
        if (_rescanRequested is { } request)
            await request();
    }

    /// <summary>
    /// Renders the per-file error list shown on the completion screen: one
    /// sentence per cause, then each file it applies to on a line of its own
    /// behind a hyphen. Internal so CompletionViewModelTests can verify the
    /// grouping without going through the live UI binding.
    /// </summary>
    internal static string FormatErrorBreakdown(IReadOnlyList<FileOperationError> errors)
    {
        if (errors.Count == 0) return string.Empty;

        // Group by type AND message, not type alone. No category tailors its
        // sentence to the file today, so the two keys agree, but the message is
        // part of the key because a bucket's heading is taken from its first
        // member: a type that ever varies its wording would otherwise print one
        // file's sentence over files that failed differently. Within a bucket,
        // list each file by name; the heading is shown once per bucket.
        var buckets = errors
            .GroupBy(e => (Type: e.GetType(), e.LocalisedMessage))
            .OrderByDescending(g => g.Count());

        var sb = new System.Text.StringBuilder();
        foreach (var bucket in buckets)
        {
            // The bucket's own count picks singular or plural through the resx
            // plural machinery, so an inflecting language gets its .One/.Few/
            // .Many form. The alternative, a "(3)" bracket appended to a
            // singular sentence, is one no language can inflect and reads as a
            // reference number.
            var count = bucket.Count();
            sb.Append(bucket.First().LocalisedGroupHeading(count)).AppendLine();
            // Leading spaces alone vanish in a proportional font (Poppins), so
            // the hyphen is what separates a filename from the sentence above it.
            foreach (var err in bucket)
                sb.Append("- ").Append(Path.GetFileName(err.FilePath)).AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}
