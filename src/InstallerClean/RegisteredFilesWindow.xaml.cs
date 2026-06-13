using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class RegisteredFilesWindow : Window
{
    private string? _lastSortProperty;
    private ListSortDirection _lastSortDirection;
    private GridViewColumn? _lastSortColumn;

    public RegisteredFilesWindow(RegisteredFilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // The window always opens at this computed size; it does not
        // remember a previous one. A saved size does not scale with the OS
        // text setting nor adapt to the current screen, so a size kept from
        // one text scale or monitor could reopen too small to read or off
        // another's edge; the computed default is always right for the
        // current setting. 950 x 770 is the 100% default, multiplied by the
        // text-scale factor because the columns and rows inside scale with
        // it, so an unscaled default would overflow into a horizontal
        // scrollbar. 770 lets the longest products' whole details, down to
        // the comment line, read by arrowing down the list without clicking
        // into the details pane to scroll: the products list is capped
        // (MaxHeight 208 scaled, the rows a 680 window showed) so every unit
        // above 680 lands in the patches/details band, and the longest real
        // metadata (a 7-line signing identity plus a 2-line comment) needs
        // about 90 of those units beyond 680. A rarer longer entry still
        // scrolls, as all three panes do. The clamps keep the window inside
        // the work area, as little as ~672 device-independent units of
        // height on a 1080p laptop at 150% display scale.
        var factor = AccessibilitySettings.Current.TextScaleFactor;
        Width = DetailWindowSizing.ClampWidthToWorkArea(
            Application.Current?.MainWindow, preferred: 950 * factor, minimum: MinWidth);
        Height = DetailWindowSizing.ClampHeightToWorkArea(
            Application.Current?.MainWindow, preferred: 770 * factor, minimum: MinHeight);

        Closed += OnClosed;
        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Documents.Hyperlink link && link.NavigateUri is not null)
            UrlLauncher.OpenUrl(link.NavigateUri.AbsoluteUri);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (ProductsList.Items.Count > 0)
        {
            ProductsList.SelectedIndex = 0;
            ProductsList.ScrollIntoView(ProductsList.Items[0]);
            var container = (ListViewItem?)ProductsList.ItemContainerGenerator
                .ContainerFromIndex(0);
            container?.Focus();
        }

        // VM pre-sorts by product name ascending; show the arrow to match.
        _lastSortProperty = nameof(ProductRow.ProductName);
        _lastSortDirection = ListSortDirection.Ascending;
        _lastSortColumn = ColProductName;
        UpdateSortIndicators();

        BuildSeeAlsoLine();
    }

    // Stable README anchor (an explicit <a id="recovery"> before the
    // recovery section) the missing-file note links to. An explicit id
    // rather than a heading-derived slug, so renaming the heading never
    // breaks this link.
    private const string MissingFileRecoveryUrl =
        "https://github.com/no-faff/InstallerClean#recovery";

    /// <summary>
    /// Builds the missing-file note's closing line from a single resx string
    /// whose linked phrase is delimited by <c>[ ]</c>: a prefix Run, one
    /// Hyperlink into the README's recovery section, then a suffix Run.
    /// Holding the sentence in one string (rather than three prefix/link/suffix
    /// keys) lets a translator move the link anywhere in it. A string with no
    /// <c>[ ]</c> pair renders verbatim as a single Run.
    /// </summary>
    private void BuildSeeAlsoLine()
    {
        var raw = Strings.Body_RegisteredMissingFromDisk_SeeAlso;
        SeeAlsoText.Inlines.Clear();

        int open = raw.IndexOf('[');
        int close = open >= 0 ? raw.IndexOf(']', open + 1) : -1;
        if (open < 0 || close < 0)
        {
            SeeAlsoText.Inlines.Add(new Run(raw));
            return;
        }

        var prefix = raw[..open];
        var linkText = raw[(open + 1)..close];
        var suffix = raw[(close + 1)..];

        var link = new Hyperlink(new Run(linkText))
        {
            NavigateUri = new Uri(MissingFileRecoveryUrl),
            Style = (Style)FindResource("SubtleLink"),
        };
        link.Click += Hyperlink_Click;
        // The visible link text is a phrase mid-sentence ("explains this
        // folder"), meaningless on its own when a screen reader tabs onto
        // the link; the automation name carries the self-contained
        // purpose and still contains the visible phrase so voice control
        // can click the on-screen words.
        AutomationProperties.SetName(link, Strings.Automation_RegisteredMissingSeeAlso);

        if (prefix.Length > 0) SeeAlsoText.Inlines.Add(new Run(prefix));
        SeeAlsoText.Inlines.Add(link);
        if (suffix.Length > 0) SeeAlsoText.Inlines.Add(new Run(suffix));
    }

    private (string Plain, GridViewColumn Col)[] SortableColumns => new[]
    {
        (Strings.Field_ProductName, ColProductName),
        (Strings.Field_File,        ColFileName),
        (Strings.Field_Size,        ColSizeBytes),
        (Strings.Field_Patches,     ColPatchCount),
    };

    private void ColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader header || header.Column is null)
            return;

        string? sortProperty = null;
        if (ReferenceEquals(header.Column, ColProductName)) sortProperty = nameof(ProductRow.ProductName);
        else if (ReferenceEquals(header.Column, ColFileName)) sortProperty = nameof(ProductRow.FileName);
        else if (ReferenceEquals(header.Column, ColSizeBytes)) sortProperty = nameof(ProductRow.SizeBytes);
        else if (ReferenceEquals(header.Column, ColPatchCount)) sortProperty = nameof(ProductRow.PatchCount);

        if (sortProperty is null) return;

        var direction = sortProperty == _lastSortProperty && _lastSortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        var view = CollectionViewSource.GetDefaultView(ProductsList.ItemsSource);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(sortProperty, direction));

        _lastSortProperty = sortProperty;
        _lastSortDirection = direction;
        _lastSortColumn = header.Column;
        UpdateSortIndicators();
    }

    private void UpdateSortIndicators()
    {
        var arrow = _lastSortDirection == ListSortDirection.Ascending ? "  \u25B2" : "  \u25BC";
        var sortedName = string.Empty;
        foreach (var (plain, col) in SortableColumns)
        {
            var isSorted = ReferenceEquals(col, _lastSortColumn);
            col.Header = isSorted ? plain + arrow : plain;
            if (isSorted) sortedName = plain;
        }

        var sortStatus = string.Format(
            _lastSortDirection == ListSortDirection.Ascending
                ? Strings.Automation_SortStatus_Ascending
                : Strings.Automation_SortStatus_Descending,
            sortedName);

        // The sorted column's header carries the sort state as its
        // accessible name. A name change on the focused element is
        // announced, so activating a header with Space speaks the new
        // state, and the name override keeps the sort-arrow glyph
        // appended to the visible header text out of speech. ItemStatus on the
        // ListView alone is not enough: it is only surfaced for the
        // element with focus, and during a keyboard sort focus sits on
        // the header. The generated header controls are reached through
        // the visual tree; they exist whenever this runs, because the
        // first call comes from Loaded, after the template is applied.
        foreach (var header in VisualTreeSearch.Descendants<GridViewColumnHeader>(ProductsList))
        {
            if (header.Column is null)
                continue; // the filler header WPF generates past the last column
            var match = SortableColumns.FirstOrDefault(c => ReferenceEquals(c.Col, header.Column));
            if (match.Col is null)
                continue;
            AutomationProperties.SetName(header,
                ReferenceEquals(header.Column, _lastSortColumn) ? sortStatus : match.Plain);
        }

        // Mirrored onto the list as a queryable property; nothing relies
        // on it being announced.
        AutomationProperties.SetItemStatus(ProductsList, sortStatus);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        if (DataContext is IDisposable vm) vm.Dispose();
    }
}
