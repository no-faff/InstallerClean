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
///
/// Rows flag as missing on <c>IsMissingFromDisk</c>, which is now the plain
/// question of whether the file is there. It drives the "a future repair could
/// fail" note and the window's opening selection, and it takes every
/// registration: a superseded patch reaches this window like any other row and
/// its file having gone is the same condition, Windows opening every registered
/// patch's cached file whatever state it carries. The property excluded that
/// class until 3.0.0, on a reading Microsoft does not support.
///
/// THIS IS WHERE THE PROGRAMS ARE NAMED. The main window's line says how many
/// files and names the first few; this window has room for all of them, which is
/// what "Open Details for what to do" is sending the reader to.
/// </summary>
public partial class RegisteredFilesViewModel : ObservableObject, IDisposable
{
    private readonly IMsiFileInfoService _infoService;
    private readonly ConcurrentDictionary<string, MsiSummaryInfo?> _cache = new();
    private readonly CancellationTokenSource _lifetimeCts = new();

    public IReadOnlyList<ProductRow> Products { get; }

    /// <summary>
    /// The files the identity pass kept back, which belong to no product row
    /// because no registration names them. Shown as a second group under its own
    /// heading rather than mixed in, because they are a different finding: Windows
    /// answers for the first group and the app could not be sure about this one.
    ///
    /// NO CAUSE IS SHOWN AND NONE MAY BE. Four different findings put a file here
    /// and a machine can carry more than one at once, so any sentence naming a
    /// cause would be false of some of the rows under it.
    /// </summary>
    public IReadOnlyList<UnsureRow> Unsure { get; }

    /// <summary>Hides the whole second group, heading and spacer included, on the
    /// ordinary machine where the pass kept nothing back. The rows it occupies are
    /// Auto-height, so a collapsed group takes none and the window lays out exactly
    /// as it did before the group existed.</summary>
    public bool HasUnsure => Unsure.Count > 0;

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

    /// <param name="unsure">
    /// Every file the identity pass kept back, whatever the cause. Empty on the
    /// machines anybody has measured.
    /// </param>
    public RegisteredFilesViewModel(
        IReadOnlyList<RegisteredPackage> packages,
        long totalBytes,
        IMsiFileInfoService infoService,
        IReadOnlyList<OrphanedFile>? unsure = null)
    {
        _infoService = infoService;

        // Registry-fallback entries share an empty ProductCode; keying on
        // path gives each its own group instead of a single "(unknown)" pile.
        var groups = packages.GroupBy(
            p => string.IsNullOrEmpty(p.ProductCode) ? p.LocalPackagePath : p.ProductCode,
            StringComparer.OrdinalIgnoreCase);

        // Path tiebreaker so the (unknown) cluster orders by path
        // rather than GroupBy-iteration order.
        //
        // This order decides which row the window opens on (see the selection at
        // the end of the constructor), not which order it displays: the window
        // applies its own culture-aware SortDescription at Loaded, so that the
        // sort arrow it paints describes an order the list is actually in. The
        // two need not agree and a change here does not move the display.
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
                    IsMissing: msi.IsMissingFromDisk));

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
                        IsMissing: extra.IsMissingFromDisk));
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
                    IsMissing: items.First().IsMissingFromDisk));
            }
        }

        Products = products;

        Unsure = (unsure ?? Array.Empty<OrphanedFile>())
            .Select(f => new UnsureRow(
                System.IO.Path.GetFileName(f.FullPath),
                DisplayHelpers.FormatSize(f.SizeBytes)))
            .ToList();

        // BOTH GROUPS, because both are on the screen. The line used to say
        // "registered" and count the product rows alone, which was true while they
        // were all the window held; with a second group under it, a figure covering
        // only the first would describe less than the reader can see, and it has to
        // agree with the main window's own left-alone line, which counts both.
        var shownCount = packages.Count + Unsure.Count;
        Summary = string.Format(
            DisplayHelpers.Pluralise(shownCount, Strings.Summary_RegisteredWindow_Singular, Strings.Summary_RegisteredWindow_Plural, "Summary.RegisteredWindow"),
            shownCount,
            DisplayHelpers.FormatSize(totalBytes + (unsure ?? Array.Empty<OrphanedFile>()).Sum(f => f.SizeBytes)));

        // Open on the first product whose installer file is missing from disk,
        // when there is one. The main window's missing-from-disk banner ends
        // "Open Details for what to do", and what to do is the note that only
        // that row's details pane carries. Selecting the top row instead leaves
        // the user hunting for a small amber triangle somewhere in an
        // alphabetical list of every installed product, which is an instruction
        // they cannot follow. Both orders here are by product name, so the missing
        // rows are wherever the alphabet puts them; when there is no missing row,
        // the intent is the top of the list, which the window reads off its own
        // sorted view rather than off this one.
        SelectedProduct = products.FirstOrDefault(p => p.IsMissing) ?? products.FirstOrDefault();
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

/// <summary>
/// One row of the second group: a cached file the identity pass kept back.
///
/// It carries a file name and a size and nothing else, which is the whole of what
/// is known about it. There is no product name because no registration names the
/// file, which is why it is in this group at all, and inventing a placeholder
/// would put a claim on the row that nothing supports.
///
/// NOT SELECTABLE, and that is a decision rather than an omission. The window is a
/// master/detail pair whose detail pane reads a product's summary stream; a row
/// with no product has nothing to put in it, so letting one be selected would
/// blank the pane and leave the reader wondering what they broke. The group is
/// rendered by an ItemsControl, which has no selection to give, so the pane goes
/// on following the products list and cannot be emptied from here.
/// </summary>
/// <param name="AccessibleName">
/// What a screen reader announces for the row, the two columns read as one line,
/// matching how the product rows carry their own.
/// </param>
public sealed record UnsureRow(string FileName, string SizeDisplay)
{
    public string AccessibleName => $"{FileName}, {SizeDisplay}";
}
