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
/// ignorant of the scan service. The "send result log" button on
/// the same overlay routes through <see cref="IResultLogService"/>.
/// </summary>
public partial class CompletionViewModel : ObservableObject
{
    [ObservableProperty] private bool _isComplete;
    [ObservableProperty] private string _heading = string.Empty;
    [ObservableProperty] private string _summary = string.Empty;
    [ObservableProperty] private string _restore = string.Empty;
    [ObservableProperty] private string _errors = string.Empty;

    [ObservableProperty] private bool _isResultLogReady;
    [ObservableProperty] private bool _isSendingResultLog;
    [ObservableProperty] private string _resultLogStatusMessage = string.Empty;

    private bool _resultLogSentThisSession;
    private bool _skipNextResultLogPrompt;

    /// <summary>
    /// Visible when a fresh log exists for the operation just
    /// completed and the user has not already sent one this session.
    /// Once sent, the button is replaced by an inline "Thanks!"
    /// message rather than re-shown for the next operation.
    /// </summary>
    public bool IsSendResultLogVisible =>
        IsResultLogReady && !_resultLogSentThisSession && !IsSendingResultLog;

    /// <summary>True after the user has successfully sent a log this session.</summary>
    public bool HasSentResultLog => _resultLogSentThisSession;

    partial void OnIsResultLogReadyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSendResultLogVisible));
    }

    partial void OnIsSendingResultLogChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSendResultLogVisible));
    }

    private readonly Func<Task>? _rescanRequested;
    private readonly IResultLogService? _resultLogService;

    /// <summary>
    /// <paramref name="rescanRequested"/> is an awaitable run-a-scan
    /// hook. <paramref name="resultLogService"/> writes and sends the
    /// post-cleanup diagnostic log when the user clicks the Send
    /// button. Both are optional so unit tests can construct a bare
    /// view-model.
    /// </summary>
    public CompletionViewModel(
        Func<Task>? rescanRequested = null,
        IResultLogService? resultLogService = null)
    {
        _rescanRequested = rescanRequested;
        _resultLogService = resultLogService;
    }

    /// <summary>Shows the "All clear" state after a scan finds no orphans.</summary>
    public void ShowAllClear()
    {
        Heading = Strings.Completion_AllClear;
        Summary = Strings.Completion_NothingToCleanUp;
        Restore = string.Empty;
        Errors = string.Empty;
        ResultLogStatusMessage = string.Empty;
        IsComplete = true;
    }

    /// <summary>Shows the post-Move summary including any per-file errors.</summary>
    public void ShowMoveSummary(int movedCount, long movedBytes, string destination,
        IReadOnlyList<FileOperationError> errors)
    {
        // Distinct heading on partial-failure paths so a user whose
        // Move only half-completed doesn't see a green "120 MB cleared"
        // banner that hides the per-file error list below it.
        Heading = string.Format(
            errors.Count == 0 ? Strings.Completion_Cleared : Strings.Completion_PartlyCleared,
            DisplayHelpers.FormatSize(movedBytes));
        var movedLabel = DisplayHelpers.PluraliseFile(movedCount);
        Summary = errors.Count == 0
            ? string.Format(Strings.Completion_MoveSummary, movedCount, movedLabel, destination)
            : string.Format(Strings.Completion_MoveSummaryWithErrors,
                movedCount, movedLabel, destination, errors.Count, DisplayHelpers.PluraliseError(errors.Count));
        Restore = Strings.Completion_MoveRestoreHint;
        Errors = errors.Count > 0 ? FormatErrorBreakdown(errors) : string.Empty;
        ResultLogStatusMessage = string.Empty;
        IsComplete = true;
    }

    /// <summary>Shows the post-Delete summary including any per-file errors.</summary>
    public void ShowDeleteSummary(int deletedCount, long deletedBytes,
        IReadOnlyList<FileOperationError> errors)
    {
        Heading = string.Format(
            errors.Count == 0 ? Strings.Completion_Cleared : Strings.Completion_PartlyCleared,
            DisplayHelpers.FormatSize(deletedBytes));
        var deletedLabel = DisplayHelpers.PluraliseFile(deletedCount);
        Summary = errors.Count == 0
            ? string.Format(Strings.Completion_DeleteSummary, deletedCount, deletedLabel)
            : string.Format(Strings.Completion_DeleteSummaryWithErrors,
                deletedCount, deletedLabel, errors.Count, DisplayHelpers.PluraliseError(errors.Count));
        Restore = Strings.Completion_DeleteRestoreHint;
        Errors = errors.Count > 0 ? FormatErrorBreakdown(errors) : string.Empty;
        ResultLogStatusMessage = string.Empty;
        IsComplete = true;
    }

    /// <summary>
    /// Marks a fresh result-log file as available for the user to
    /// send. Called by the operation pipeline after the JSON has been
    /// written to disk. A no-op once the user has already sent a log
    /// this session.
    /// </summary>
    public void MarkResultLogReady()
    {
        if (_resultLogSentThisSession) return;
        IsResultLogReady = true;
    }

    /// <summary>
    /// One-shot flag set by <c>RescanAfterCompletion</c> so the all-clear
    /// that follows a rescan from the completion overlay doesn't re-prompt
    /// the user with the Send button they have just declined to use.
    /// Consumed by the next <see cref="ConsumeSuppressNextResultLogPrompt"/>
    /// call.
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
        if (_resultLogService is null || _resultLogSentThisSession) return;

        IsSendingResultLog = true;
        ResultLogStatusMessage = Strings.ResultLog_Sending;
        try
        {
            var outcome = await _resultLogService.SendAsync();
            if (outcome == ResultLogSendOutcome.Sent)
            {
                _resultLogSentThisSession = true;
                IsResultLogReady = false;
                ResultLogStatusMessage = Strings.ResultLog_Sent;
                OnPropertyChanged(nameof(HasSentResultLog));
                OnPropertyChanged(nameof(IsSendResultLogVisible));
            }
            else
            {
                ResultLogStatusMessage = OutcomeMessage(outcome);
            }
        }
        finally
        {
            IsSendingResultLog = false;
        }
    }

    private static string OutcomeMessage(ResultLogSendOutcome outcome) => outcome switch
    {
        ResultLogSendOutcome.NetworkUnavailable => Strings.ResultLog_NetworkUnavailable,
        ResultLogSendOutcome.Timeout => Strings.ResultLog_Timeout,
        ResultLogSendOutcome.ServerError => Strings.ResultLog_ServerError,
        ResultLogSendOutcome.NoLogToSend => Strings.ResultLog_NoLogToSend,
        _ => Strings.ResultLog_Unknown,
    };

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
        // The next ScanCompleted will run with this rescan in flight; an
        // all-clear that follows must not re-show the Send button the
        // user has just dismissed by choosing to rescan.
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
