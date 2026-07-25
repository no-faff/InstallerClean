using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
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
        // Sort before selecting, not after painting the arrow: the view's order
        // is what decides the index the bound selection sits at, and the scroll
        // and the container focus below are both taken from that index.
        ApplySort(nameof(ProductRow.ProductName), ListSortDirection.Ascending, ColProductName);

        if (ProductsList.Items.Count > 0)
        {
            // The row to open on is the view-model's when it flagged one missing
            // from disk: the main window's banner ends "Open Details for what to
            // do", and what to do is the note only that row's details pane
            // carries, so overriding the selection would send the user who
            // followed the banner somewhere else. Otherwise the view model's
            // intent was simply the top row, and which row that is belongs to the
            // view's order rather than to the pre-sort's, so it is read as index 0
            // rather than trusted to be where the pre-sort left it. The Max guards
            // a selection the list has not resolved.
            var index = ProductsList.SelectedItem is ProductRow { IsMissing: true }
                ? Math.Max(ProductsList.SelectedIndex, 0)
                : 0;
            ProductsList.SelectedIndex = index;
            ProductsList.ScrollIntoView(ProductsList.Items[index]);
            // The list virtualises, so a row below the fold has no container
            // until the scroll above realises it, and the layout pass that does
            // so has not run yet.
            ProductsList.UpdateLayout();
            var container = (ListViewItem?)ProductsList.ItemContainerGenerator
                .ContainerFromIndex(index);
            container?.Focus();
        }

        BuildSeeAlsoLine();
    }

    /// <summary>
    /// Puts the view in the given order and repaints the indicators to match.
    /// The order the window opens in comes through here too, which is the point:
    /// the arrow used to be painted over the view model's pre-sort without a
    /// SortDescription behind it, and the two orders are not the same one. The
    /// pre-sort is ordinal (<see cref="StringComparer.OrdinalIgnoreCase"/>),
    /// which orders by UTF-16 code unit and so files every accented or
    /// non-Latin product name after Z; a SortDescription compares
    /// culture-aware, which interleaves them with their base letters. So on any
    /// machine with a product name outside plain ASCII, and there are plenty,
    /// the arrow claimed an order the list was not in and the first click on
    /// Product name silently reordered the rows instead of reversing them.
    ///
    /// Every sort ends on the full path, which is unique per row, so the order
    /// is total: rows sharing a product name (one product code registered with
    /// several caches), a size or a patch count resolve the same way every time
    /// rather than however WPF's comparer happened to leave them. That is also
    /// where the view model's own path tiebreaker went.
    /// </summary>
    private void ApplySort(string sortProperty, ListSortDirection direction, GridViewColumn column)
    {
        var view = CollectionViewSource.GetDefaultView(ProductsList.ItemsSource);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(sortProperty, direction));
        view.SortDescriptions.Add(
            new SortDescription(nameof(ProductRow.FullPath), ListSortDirection.Ascending));

        _lastSortProperty = sortProperty;
        _lastSortDirection = direction;
        _lastSortColumn = column;
        UpdateSortIndicators();
    }

    // Stable README anchor (an explicit <a id="recovery"> before the
    // recovery section of every README) the missing-file note links to. An
    // explicit id rather than a heading-derived slug, so renaming the heading
    // never breaks this link; the URL targets the README in the displayed
    // language.
    private static string MissingFileRecoveryUrl =>
        ReadmeLinks.For("recovery", Localisation.UiCulture);

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

        // Where the sentence splits around its [ ]-delimited link is pure string
        // work in Core (see CompositionParsing); this method only builds inlines.
        if (CompositionParsing.SplitAtBracketedPhrase(raw) is not { } split)
        {
            SeeAlsoText.Inlines.Add(new Run(raw));
            return;
        }

        var link = new Hyperlink(new Run(split.LinkText))
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

        if (split.Prefix.Length > 0) SeeAlsoText.Inlines.Add(new Run(split.Prefix));
        SeeAlsoText.Inlines.Add(link);
        if (split.Suffix.Length > 0) SeeAlsoText.Inlines.Add(new Run(split.Suffix));
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
        if (e.OriginalSource is GridViewColumnHeader header)
            SortByColumn(header);
    }

    /// <summary>
    /// Space and Enter on a focused column header, which WPF does not wire up on
    /// its own. GridViewColumnHeader.OnClick raises the Click event only for an
    /// access-key or automation invoke, or while the mouse pointer sits inside
    /// the header's own bounds (dotnet/wpf, GridViewColumnHeader.cs, guarding on
    /// <c>IsAccessKeyOrAutomation || !IsMouseOutside()</c>), so ButtonBase's own
    /// Space and Enter handling reaches OnClick and has its Click dropped there
    /// with the pointer anywhere else on screen. The framework is consistent
    /// about it rather than broken: a header is not focusable by default, so it
    /// has no keyboard path to wire. The app's header style makes it focusable,
    /// which leaves this the missing half.
    ///
    /// Preview, not the bubbling KeyDown: ButtonBase marks both keys handled at
    /// the header, so a handler on the list would never be called. Focus stays on
    /// the header afterwards, where the mouse path would have forwarded it to the
    /// list, because UpdateSortIndicators announces the new sort state by
    /// renaming the header that has focus.
    /// </summary>
    private void ColumnHeader_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Space or Key.Enter)) return;
        if (Keyboard.Modifiers != ModifierKeys.None) return;
        if (e.OriginalSource is not GridViewColumnHeader header) return;

        if (SortByColumn(header))
            e.Handled = true;
    }

    /// <summary>
    /// Sorts by the clicked or activated column, toggling to descending when it
    /// is already the ascending sort. Returns false for a header with no sortable
    /// column behind it, which is the filler header WPF generates past the last
    /// column.
    /// </summary>
    private bool SortByColumn(GridViewColumnHeader header)
    {
        if (header.Column is null) return false;

        string? sortProperty = null;
        if (ReferenceEquals(header.Column, ColProductName)) sortProperty = nameof(ProductRow.ProductName);
        else if (ReferenceEquals(header.Column, ColFileName)) sortProperty = nameof(ProductRow.FileName);
        else if (ReferenceEquals(header.Column, ColSizeBytes)) sortProperty = nameof(ProductRow.SizeBytes);
        else if (ReferenceEquals(header.Column, ColPatchCount)) sortProperty = nameof(ProductRow.PatchCount);

        if (sortProperty is null) return false;

        var direction = sortProperty == _lastSortProperty && _lastSortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        ApplySort(sortProperty, direction, header.Column);
        return true;
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
        // announced, so activating a header with Space or Enter speaks the
        // new state, and the name override keeps the sort-arrow glyph
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
