using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// Backs the registered-files detail window. Groups packages by product
/// (so an MSI and its patches show as a single row), sorts alphabetically
/// by product name, and lazy-loads MSI summary metadata for the
/// selected row off the UI thread. The cache survives selection cycles.
/// </summary>
public partial class RegisteredFilesViewModel : ObservableObject, IDisposable
{
    private readonly IMsiFileInfoService _infoService;
    private readonly ConcurrentDictionary<string, MsiSummaryInfo?> _cache = new();
    private readonly CancellationTokenSource _lifetimeCts = new();

    public IReadOnlyList<ProductRow> Products { get; }
    public string Summary { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(ShowDetails))]
    [NotifyPropertyChangedFor(nameof(ShowNoMetadata))]
    [NotifyPropertyChangedFor(nameof(SelectedPatches))]
    [NotifyPropertyChangedFor(nameof(HasPatches))]
    private ProductRow? _selectedProduct;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDetails))]
    [NotifyPropertyChangedFor(nameof(ShowNoMetadata))]
    private MsiSummaryInfo? _selectedDetails;

    public bool HasSelection => SelectedProduct is not null;
    public bool HasPatches => SelectedProduct is not null && SelectedProduct.Patches.Count > 0;
    public bool ShowDetails => SelectedProduct is not null && SelectedDetails is not null;
    public bool ShowNoMetadata => SelectedProduct is not null && SelectedDetails is null;
    public IReadOnlyList<PatchRow> SelectedPatches => SelectedProduct?.Patches ?? Array.Empty<PatchRow>();

    public RegisteredFilesViewModel(
        IReadOnlyList<RegisteredPackage> packages,
        long totalBytes,
        IMsiFileInfoService infoService)
    {
        _infoService = infoService;

        // Registry-fallback entries share an empty ProductCode; keying on
        // path gives each its own group instead of a single "(unknown)" pile.
        var groups = packages.GroupBy(
            p => string.IsNullOrEmpty(p.ProductCode) ? p.LocalPackagePath : p.ProductCode,
            StringComparer.OrdinalIgnoreCase);

        // Path tiebreaker so the (unknown) cluster orders by path
        // rather than GroupBy-iteration order.
        var products = new List<ProductRow>();
        foreach (var group in groups
            .OrderBy(g => g.First().ProductName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(g => g.First().LocalPackagePath, StringComparer.OrdinalIgnoreCase))
        {
            var items = group.ToList();

            var msi = items.FirstOrDefault(p =>
                p.LocalPackagePath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));

            var patches = items
                .Where(p => p.LocalPackagePath.EndsWith(".msp", StringComparison.OrdinalIgnoreCase))
                .Select(p => new PatchRow(
                    Path.GetFileName(p.LocalPackagePath),
                    p.LocalPackagePath,
                    DisplayHelpers.FormatSize(p.FileSizeBytes)))
                .ToList();

            if (msi is null && patches.Count == 0) continue;

            var productName = items.First().ProductName;
            if (string.IsNullOrEmpty(productName)) productName = Strings.Field_UnknownProductName;

            var representative = msi ?? items.First();

            products.Add(new ProductRow(
                productName,
                Path.GetFileName(representative.LocalPackagePath),
                representative.LocalPackagePath,
                DisplayHelpers.FormatSize(representative.FileSizeBytes),
                representative.FileSizeBytes,
                patches.Count,
                patches));
        }

        Products = products;
        Summary = string.Format(Strings.Summary_RegisteredWindow,
            packages.Count,
            DisplayHelpers.PluraliseFile(packages.Count),
            DisplayHelpers.FormatSize(totalBytes));

        if (Products.Count > 0)
            SelectedProduct = Products[0];
    }

    async partial void OnSelectedProductChanged(ProductRow? value)
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
            if (SelectedProduct == value)
            {
                _cache[value.FullPath] = info;
                SelectedDetails = info;
            }
        }
        catch (OperationCanceledException)
        {
            // Window closed; drop the result.
        }
        catch
        {
            if (!ct.IsCancellationRequested && SelectedProduct == value)
                SelectedDetails = null;
        }
    }

    public void Dispose()
    {
        _lifetimeCts.Cancel();
        _lifetimeCts.Dispose();
    }
}
