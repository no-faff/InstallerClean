using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// Backs the orphaned-files detail window. Holds the displayable file
/// list (sorted largest-first), the currently selected file, and a lazy
/// per-file metadata cache that reads MSI summary information off the
/// UI thread. The cache survives selection cycles so a user clicking
/// back through previously selected files sees instant detail panels.
/// </summary>
public partial class OrphanedFilesViewModel : ObservableObject, IDisposable
{
    private readonly IMsiFileInfoService _infoService;
    private readonly ConcurrentDictionary<string, MsiSummaryInfo?> _cache = new();
    private readonly CancellationTokenSource _lifetimeCts = new();

    public IReadOnlyList<OrphanedFile> Files { get; }
    public string Summary { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(ShowDetails))]
    [NotifyPropertyChangedFor(nameof(ShowNoMetadata))]
    private OrphanedFile? _selectedFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDetails))]
    [NotifyPropertyChangedFor(nameof(ShowNoMetadata))]
    private MsiSummaryInfo? _selectedDetails;

    public bool HasSelection => SelectedFile is not null;
    public bool ShowDetails => SelectedFile is not null && SelectedDetails is not null;
    public bool ShowNoMetadata => SelectedFile is not null && SelectedDetails is null;

    public OrphanedFilesViewModel(
        IReadOnlyList<OrphanedFile> files,
        IMsiFileInfoService infoService)
    {
        _infoService = infoService;
        // Pre-sort by size descending so the largest reclaimable files
        // show first. OrphanedFilesWindow.xaml.cs:Window_Loaded mirrors
        // this initial state into the GridView sort-arrow indicator;
        // changing the OrderBy direction or key here without updating
        // the indicator initialiser produces an arrow that lies about
        // the actual sort order.
        Files = files.OrderByDescending(f => f.SizeBytes).ToList();

        var totalSize = DisplayHelpers.FormatSize(files.Sum(f => f.SizeBytes));
        // Footer mirrors the Reason-column split of removable files: true
        // orphans (never claimed by the API, so not a removable patch), then
        // the two disjoint patch states. The three counts partition Files.
        var orphaned = files.Count(f => !f.IsRemovablePatch);
        var obsoleted = files.Count(f => f.IsObsoleted);
        var superseded = files.Count(f => f.IsRemovablePatch && !f.IsObsoleted);
        Summary = string.Format(Strings.Summary_OrphanedWindow,
            orphaned, superseded, obsoleted, totalSize);

        if (Files.Count > 0)
            SelectedFile = Files[0];
    }

    async partial void OnSelectedFileChanged(OrphanedFile? value)
    {
        if (value is null)
        {
            SelectedDetails = null;
            return;
        }

        if (_cache.TryGetValue(value.FullPath, out var cached))
        {
            SelectedDetails = cached;
            return;
        }

        var ct = _lifetimeCts.Token;
        try
        {
            var info = await Task.Run(() => _infoService.GetSummaryInfo(value.FullPath), ct);

            if (ct.IsCancellationRequested) return;
            if (SelectedFile == value)
            {
                _cache[value.FullPath] = info;
                SelectedDetails = info;
            }
        }
        catch (OperationCanceledException)
        {
            // Window closed; drop the result.
        }
        catch (Exception ex)
        {
            // IMsiFileInfoService is contracted never to throw (the
            // production implementation wraps everything in its own
            // try/catch). The catch logs anything that does break the
            // contract instead of swallowing silently, so a regression
            // surfaces in crash.log rather than as a "no metadata"
            // panel with no diagnostic trail.
            CrashLog.Write(ex);
            if (!ct.IsCancellationRequested && SelectedFile == value)
                SelectedDetails = null;
        }
    }

    public void Dispose()
    {
        _lifetimeCts.Cancel();
        _lifetimeCts.Dispose();
    }
}
