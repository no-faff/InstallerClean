using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.ViewModels;

/// <summary>
/// Completion-screen slice. Holds the heading / summary / restore-hint
/// / errors block shown after a scan-with-no-orphans, a successful
/// move or a successful delete.
///
/// Other slices push state into this VM via the <c>Show*</c> methods;
/// this VM does not pull from them. The dismiss command clears the
/// state; the rescan command requests another scan via the
/// <see cref="RescanRequested"/> event so this VM stays decoupled from
/// the scan service.
/// </summary>
public partial class CompletionViewModel : ObservableObject
{
    [ObservableProperty] private bool _isComplete;
    [ObservableProperty] private string _heading = string.Empty;
    [ObservableProperty] private string _summary = string.Empty;
    [ObservableProperty] private string _restore = string.Empty;
    [ObservableProperty] private string _errors = string.Empty;

    private readonly Func<Task>? _rescanRequested;

    /// <summary>
    /// <paramref name="rescanRequested"/> is an awaitable run-a-scan
    /// hook. Func not a service reference so this VM stays ignorant of
    /// the scan service.
    /// </summary>
    public CompletionViewModel(Func<Task>? rescanRequested = null)
    {
        _rescanRequested = rescanRequested;
    }

    /// <summary>Shows the "All clear" state after a scan finds no orphans.</summary>
    public void ShowAllClear()
    {
        Heading = Strings.Completion_AllClear;
        Summary = Strings.Completion_NothingToCleanUp;
        Restore = string.Empty;
        Errors = string.Empty;
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
        IsComplete = true;
    }

    [RelayCommand]
    private void Dismiss()
    {
        IsComplete = false;
        Errors = string.Empty;
    }

    [RelayCommand]
    private async Task RescanAfterCompletion()
    {
        IsComplete = false;
        Errors = string.Empty;
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
