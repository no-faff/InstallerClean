using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

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
    [ObservableProperty] private string _errors = string.Empty;

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
        Heading = Strings.Completion_AllClean;
        Summary = string.Format(
            Strings.Completion_NothingToCleanUp,
            installedProductCount,
            DisplayHelpers.PluraliseProduct(installedProductCount),
            DisplayHelpers.FormatElapsedLong(TimeSpan.FromMilliseconds(scanDurationMs)));
        Restore = string.Empty;
        Errors = string.Empty;
        ResultLogStatusMessage = string.Empty;
        LastResultFreedNothing = true;
        IsComplete = true;
    }

    /// <summary>Shows the post-Move summary including any per-file errors.</summary>
    public void ShowMoveSummary(int movedCount, long movedBytes, string destination,
        IReadOnlyList<FileOperationError> errors)
    {
        // Distinct heading on partial-failure paths so a user whose
        // Move only half-completed doesn't see a green "120 MB freed"
        // banner that hides the per-file error list below it.
        Heading = string.Format(
            errors.Count == 0 ? Strings.Completion_Freed : Strings.Completion_PartlyFreed,
            DisplayHelpers.FormatSize(movedBytes));
        var movedLabel = DisplayHelpers.PluraliseFile(movedCount);
        Summary = errors.Count == 0
            ? string.Format(Strings.Completion_MoveSummary, movedCount, movedLabel, destination)
            : string.Format(Strings.Completion_MoveSummaryWithErrors,
                movedCount, movedLabel, destination, errors.Count, DisplayHelpers.PluraliseError(errors.Count));
        Restore = Strings.Completion_MoveRestoreHint;
        Errors = errors.Count > 0 ? FormatErrorBreakdown(errors) : string.Empty;
        ResultLogStatusMessage = string.Empty;
        LastResultFreedNothing = movedBytes <= 0;
        IsComplete = true;
    }

    /// <summary>Shows the post-Delete summary including any per-file errors.</summary>
    public void ShowDeleteSummary(int deletedCount, long deletedBytes,
        IReadOnlyList<FileOperationError> errors)
    {
        Heading = string.Format(
            errors.Count == 0 ? Strings.Completion_Freed : Strings.Completion_PartlyFreed,
            DisplayHelpers.FormatSize(deletedBytes));
        var deletedLabel = DisplayHelpers.PluraliseFile(deletedCount);
        Summary = errors.Count == 0
            ? string.Format(Strings.Completion_DeleteSummary, deletedCount, deletedLabel)
            : string.Format(Strings.Completion_DeleteSummaryWithErrors,
                deletedCount, deletedLabel, errors.Count, DisplayHelpers.PluraliseError(errors.Count));
        Restore = Strings.Completion_DeleteRestoreHint;
        Errors = errors.Count > 0 ? FormatErrorBreakdown(errors) : string.Empty;
        ResultLogStatusMessage = string.Empty;
        LastResultFreedNothing = deletedBytes <= 0;
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
        IsResultLogReady = false;
        ResultLogStatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task RescanAfterCompletion()
    {
        IsComplete = false;
        Errors = string.Empty;
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
    /// Renders the per-file error list shown on the completion screen.
    /// Errors are grouped by category so the user sees "Access denied
    /// (3): a.msi, b.msi, c.msi" rather than a flat list of identical
    /// sentences. Internal so MainViewModelTests can verify the
    /// grouping behaviour without going through the live UI binding.
    /// </summary>
    internal static string FormatErrorBreakdown(IReadOnlyList<FileOperationError> errors)
    {
        if (errors.Count == 0) return string.Empty;

        // Group by runtime type so MissingSourceFile, ShellRefused etc
        // each get their own bucket. Within a bucket, list each file
        // by name; the LocalisedMessage is shown once per category.
        var buckets = errors
            .GroupBy(e => e.GetType())
            .OrderByDescending(g => g.Count());

        var sb = new System.Text.StringBuilder();
        foreach (var bucket in buckets)
        {
            var sample = bucket.First().LocalisedMessage;
            sb.Append(sample).Append(" (").Append(bucket.Count()).Append(')').AppendLine();
            foreach (var err in bucket)
                sb.Append("  ").Append(Path.GetFileName(err.FilePath)).AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}
