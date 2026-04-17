using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

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
        Files = files.OrderByDescending(f => f.SizeBytes).ToList();

        var totalSize = DisplayHelpers.FormatSize(files.Sum(f => f.SizeBytes));
        Summary = $"{files.Count} {DisplayHelpers.Pluralise(files.Count, "file", "files")} ({totalSize})";

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
            // Window closed mid-read; drop the result silently.
        }
        catch
        {
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
