using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// Backs the details window's file list. Groups packages by product (so an MSI
/// and its patches show as a single row), sorts alphabetically by product
/// name, and lazy-loads MSI summary metadata for the selected row off the UI
/// thread. The cache survives selection cycles.
///
/// ONE LIST, HOLDING TWO POPULATIONS. The registrations Windows still has a
/// record of, and the files this scan declined to offer. They sat under
/// separate headings until 3.0.0 and now share <see cref="Products"/>, a
/// withheld file arriving as an ordinary row with no product name of its own.
/// The detail pane needs no special case for one, because it reads the FILE
/// rather than the registration: author, title, signing certificate and the
/// rest all come out of the package's own summary stream, which is why the
/// orphaned window can show a certificate for a file no registration claims.
///
/// Rows flag as missing on <c>IsMissingFromDisk</c>, the plain question of
/// whether the file is there. A row's own missing file drives
/// <see cref="ShowMissing"/>, which puts the recovery note in front of the reader
/// where that file is an .msi, and any missing file of the row, a patch's
/// included, drives the warning triangle and the window's opening selection
/// (<see cref="ProductRow.HasMissingFile"/>). The flag grades no patch state and
/// must not start: Windows opens
/// every registered patch's cached file whatever state it carries, so an absent
/// file is the same condition on a superseded registration as on any other.
///
/// WHICH REGISTRATIONS REACH THAT FLAG IS SETTLED BEFORE ANY ROW IS BUILT, at
/// the head of the constructor, and it is the narrower question of whether
/// anything could still reach for the file. The two are separate on purpose:
/// the flag describes a row, and the filter decides whether there is one.
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
    // The note's advice is about a missing .msi: running the installer for the same
    // version puts it back. Nothing establishes that of a missing patch, so a row
    // standing for patches alone, and a row whose only missing file is a patch,
    // show no note.
    public bool ShowMissing => SelectedProduct is { IsMissing: true, IsPatchesOnly: false };
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
        IMsiFileInfoService infoService,
        IReadOnlyList<OrphanedFile>? withheldFiles = null)
    {
        _infoService = infoService;

        // WHAT THE LIST IS BUILT FROM, WHICH IS NARROWER THAN THE SET OF
        // REGISTRATIONS. A registration whose file has gone is a row unless this
        // scan established that nothing can reach for the file. Not established is
        // enough to keep it: the row says what the app could not rule out, never
        // that anything is wrong, so a verdict nobody could take holds the row here
        // exactly as it withholds the offer elsewhere.
        //
        // THE CONDITION IS THE ONE THE OFFER RESTS ON, PUT TO A FILE THAT HAS GONE.
        // MissingFilesReport.Affected carries the whole expression, and the clause
        // doing the work is the per-product verdict: every product sharing the patch
        // was shown to hold no patch that could be uninstalled and roll back onto
        // its cached file. The offer requires that same value before it will put
        // such a file in front of anybody, so the two cannot reach opposite answers
        // on it. Nothing is remembered between runs either, the verdict being
        // re-derived from the machine's own records on every scan, so a product that
        // acquires an uninstallable patch fails the clause next time and the row
        // comes back.
        //
        // NOTHING ABOUT THE FILE PUTS SUCH A ROW HERE, WHICH IS WHY LEAVING IT OUT
        // CONCEALS NOTHING. This list is built from registrations, and a
        // registration outlives the file it names. A file offered because no
        // registration claims it has none to leave behind and is on this screen
        // neither before it goes nor after; a superseded patch offered on its
        // per-product verdict does leave one. Without this the same run would say
        // nothing about the files of the first kind it removed and would flag the
        // second, which describes how the list is assembled rather than anything
        // about the disk.
        //
        // THE MISSING ROWS THAT REMAIN ARE WHAT THIS WINDOW IS FOR AND THEY STAY.
        // The main window's missing-from-disk line ends by sending the reader here
        // for what to do, and what to do is the note on such a row's own detail
        // pane, which the opening selection at the end of this constructor lands
        // on. Drop those rows and that line points at a window holding no answer to
        // the instruction it has just given.
        var listed = packages
            .Where(p => !p.IsMissingFromDisk || MissingFilesReport.Affected(p))
            .ToList();

        // Registry-fallback entries share an empty ProductCode; keying on
        // path gives each its own group instead of a single "(unknown)" pile.
        var groups = listed.GroupBy(
            p => string.IsNullOrEmpty(p.ProductCode) ? p.LocalPackagePath : p.ProductCode,
            StringComparer.OrdinalIgnoreCase);

        // Path tiebreaker so the (unknown) cluster orders by path
        // rather than GroupBy-iteration order.
        //
        // This order decides which row the window opens on (see the selection at
        // the end of the constructor), not which order it displays: the window
        // sorts the view itself at Loaded, collating in its own language, so that
        // the sort arrow it paints describes an order the list is actually in. The
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
                    PathText.GetFileName(p.LocalPackagePath),
                    p.LocalPackagePath,
                    DisplayHelpers.FormatSize(p.FileSizeBytes),
                    IsMissing: p.IsMissingFromDisk))
                .ToList();

            if (msi is null && patches.Count == 0) continue;

            var productName = items.First().ProductName;
            if (string.IsNullOrEmpty(productName)) productName = Strings.Field_UnknownProductName;

            if (msi is not null)
            {
                products.Add(new ProductRow(
                    productName,
                    PathText.GetFileName(msi.LocalPackagePath),
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
                        PathText.GetFileName(extra.LocalPackagePath),
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
                // patch-list entry. The row is missing only where every patch
                // is, its size being their total, and its details are read from
                // a patch that is on the disk.
                var patchBytes = items.Sum(p => p.FileSizeBytes);
                var present = items.FirstOrDefault(p => !p.IsMissingFromDisk) ?? items.First();
                products.Add(new ProductRow(
                    productName,
                    Strings.Field_PatchesOnly,
                    present.LocalPackagePath,
                    DisplayHelpers.FormatSize(patchBytes),
                    patchBytes,
                    patches.Count,
                    patches,
                    IsMissing: items.All(p => p.IsMissingFromDisk),
                    IsPatchesOnly: true));
            }
        }

        // The files this scan declined to offer, in the same list as the
        // registrations rather than under a heading of their own. Nothing marks
        // them out: no column, no indicator and no lookup of a program that might
        // have used the file. The product name is empty, because no registration
        // names the file and there is therefore no product to name, and the cell
        // says exactly that: ProductRow.ProductNameDisplay puts
        // Field.NoNamedProduct in front of the reader, which states the fact
        // rather than leaving a space for them to account for themselves.
        //
        // NOT "(unknown)", which this window uses for a REGISTERED product whose
        // display name did not come back: reusing it here would tell the reader a
        // product had been established when none has.
        //
        // AND THE NAME ITSELF STAYS EMPTY IN THE RECORD. What the cell says is
        // composed for display, so the ordering below goes on reading an empty
        // name and these rows keep their place at the foot of the list.
        //
        // NO CAUSE TRAVELS WITH THESE ROWS AND NONE MAY BE ADDED. More than one
        // condition puts a file here and they are not one thing, so any sentence
        // covering the lot would be false of part of it; see
        // <see cref="ScanResult.WithheldFiles"/>, which lists them.
        //
        // AND THERE IS NO PER-FILE RECORD OF WHICH CAUSE PUT A ROW HERE, on this
        // screen or anywhere else. The opt-in report carries each cause as its own
        // count, so what travels is how many files each decision kept back and
        // never which decision kept back any one of them. A figure about a scan
        // cannot be read down onto a row.
        //
        // No patches and not missing, both being the truth rather than a gap. A
        // file no registration names has no registered patches to list, and the
        // missing flag drives a recovery note about a file WINDOWS HAS A RECORD
        // FOR, which is the one thing these rows are not.
        //
        // THE EMPTY PRODUCT NAME IS WHAT PUTS THESE ROWS AT THE FOOT OF THE
        // DISPLAYED LIST. The window ranks on ProductRow.HasNoNamedProduct ahead
        // of the product name itself; the note that says so in full is beside
        // that sort, in Window_Loaded.
        var withheld = withheldFiles ?? Array.Empty<OrphanedFile>();
        foreach (var file in withheld)
        {
            products.Add(new ProductRow(
                ProductName: string.Empty,
                PathText.GetFileName(file.FullPath),
                file.FullPath,
                DisplayHelpers.FormatSize(file.SizeBytes),
                file.SizeBytes,
                PatchCount: 0,
                Array.Empty<PatchRow>(),
                IsMissing: false));
        }

        Products = products;

        // BOTH POPULATIONS, because both are on the screen. A figure covering only
        // the registrations would describe less than the reader can see, and it has
        // to agree with the main window's own left-alone line, which counts both the
        // same way. The two are one click apart, so a reader comparing them must not
        // find them disagreeing.
        //
        // PACKAGES, NOT ROWS, and that is the part the next editor will get wrong
        // now that the withheld files are rows too. A product registered with three
        // patches is ONE row and FOUR packages, so Products.Count would report a
        // number neither window has ever shown. The count is of files, and it is
        // taken from the two inputs rather than from the list built out of them.
        // AND A REGISTRATION WHOSE FILE IS GONE IS IN NEITHER FIGURE, which is the
        // other half of the same rule: nothing was left alone that is not there, and
        // totalBytes is summed under that same existence test, so the count and the
        // size describe one population. The withheld files are in both, being real
        // files in the folder that this scan did not offer.
        //
        // EVERY ROW ON THE LIST IS SHOWN, THE MISSING ONES INCLUDED, and the count is
        // narrower than the list on purpose. The recovery note lives on a missing row
        // and the missing flag is what puts it there, so a list trimmed to match the count
        // would take away the one screen that says what to do. The clause below says
        // how many of the rows on view are in that state instead, which is a sentence
        // only this window can carry: the main window's line has the same count and no
        // list under it.
        //
        // THE SAME COUNT IS MEANT LITERALLY. Both figures are the missing rows the
        // scan could not establish to be harmless, because the list this method walks
        // has been filtered on the predicate the main window's own count is taken over.
        // The two are one click apart and a reader comparing them must not find them
        // disagreeing.
        var missingCount = listed.Count(p => p.IsMissingFromDisk);
        var shownCount = listed.Count - missingCount + withheld.Count;
        var summary = string.Format(
            DisplayHelpers.Pluralise(shownCount, Strings.Summary_RegisteredWindow_Singular, Strings.Summary_RegisteredWindow_Plural, "Summary.RegisteredWindow"),
            DisplayHelpers.FormatCount(shownCount),
            DisplayHelpers.FormatSize(totalBytes + withheld.Sum(f => f.SizeBytes)));

        // Joined on the shared separator rather than a comma written here, on the same
        // reasoning as MissingFilesReport.Inline: where a clause hangs off a phrase is
        // a per-language question and the answer is a resx value.
        Summary = missingCount == 0
            ? summary
            : summary + Strings.Display_ListSeparator + string.Format(
                DisplayHelpers.Pluralise(missingCount,
                    Strings.Summary_RegisteredWindow_Missing_Singular,
                    Strings.Summary_RegisteredWindow_Missing_Plural,
                    "Summary.RegisteredWindow.Missing"),
                DisplayHelpers.FormatCount(missingCount));

        // Open on the first product with a file missing from disk, its .msi or a
        // patch, when there is one. The main window's missing-from-disk banner ends
        // "Open Details for what to do", and that row's details pane is where the
        // answer is: the note for a missing .msi, and the patch list marking which
        // patch is gone. Selecting the top row instead leaves
        // the user hunting for a small amber triangle somewhere in an
        // alphabetical list of every installed product, which is an instruction
        // they cannot follow. Both orders here rank on product name, so a missing
        // row is wherever that puts it; when there is no missing row, the intent
        // is the top of the list, which the window reads off its own sorted view
        // rather than off this one.
        SelectedProduct = products.FirstOrDefault(p => p.HasMissingFile) ?? products.FirstOrDefault();
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
            CrashLog.TryWrite(ex);
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
