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
    [NotifyPropertyChangedFor(nameof(ShowMissing))]
    [NotifyPropertyChangedFor(nameof(SelectedMissingNote))]
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
    public bool ShowMissing => SelectedProduct?.IsMissing == true;
    // A missing file has no summary stream to read (the file is gone), so the
    // "no metadata" panel stands down in favour of the missing notice.
    public bool ShowNoMetadata => SelectedProduct is not null && SelectedDetails is null && !ShowMissing;
    // Identical for every product: the recovery advice does not vary by app,
    // and naming the product would force a possessive that breaks on names
    // ending in s. So the note is one generic string, shown whenever the
    // selected product's installer file is missing from disk.
    public string SelectedMissingNote => Strings.Body_RegisteredMissingFromDisk;
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

            if (msi is not null)
            {
                products.Add(new ProductRow(
                    productName,
                    Path.GetFileName(msi.LocalPackagePath),
                    msi.LocalPackagePath,
                    DisplayHelpers.FormatSize(msi.FileSizeBytes),
                    msi.FileSizeBytes,
                    patches.Count,
                    patches,
                    IsMissing: !msi.FileExists));

                // One ProductCode can be registered with several .msi
                // caches (a product installed per machine AND per user
                // shares the code across contexts). Each extra cache is
                // counted in the window summary, so each gets its own
                // row; the patches stay on the first row only, since
                // they too are keyed by the shared code and would
                // double-list otherwise.
                foreach (var extra in items.Where(p =>
                    p.LocalPackagePath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)
                    && !ReferenceEquals(p, msi)))
                {
                    products.Add(new ProductRow(
                        productName,
                        Path.GetFileName(extra.LocalPackagePath),
                        extra.LocalPackagePath,
                        DisplayHelpers.FormatSize(extra.FileSizeBytes),
                        extra.FileSizeBytes,
                        PatchCount: 0,
                        new List<PatchRow>(),
                        IsMissing: !extra.FileExists));
                }
            }
            else
            {
                // No .msi for this product - render a synthetic main row
                // showing the patch total so the first patch isn't
                // duplicated as both the product line AND the first
                // patch-list entry.
                var patchBytes = items.Sum(p => p.FileSizeBytes);
                products.Add(new ProductRow(
                    productName,
                    Strings.Field_PatchesOnly,
                    items.First().LocalPackagePath,
                    DisplayHelpers.FormatSize(patchBytes),
                    patchBytes,
                    patches.Count,
                    patches,
                    IsMissing: !items.First().FileExists));
            }
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

        if (value.IsMissing)
        {
            // The file is gone, so there is no summary stream to read; the
            // missing notice replaces the metadata panel via ShowMissing.
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
        catch (Exception ex)
        {
            // IMsiFileInfoService is contracted never to throw (the
            // production implementation wraps everything in its own
            // try/catch). The catch logs anything that does break the
            // contract instead of swallowing silently, so a regression
            // surfaces in crash.log rather than as a "no metadata"
            // panel with no diagnostic trail.
            CrashLog.Write(ex);
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
