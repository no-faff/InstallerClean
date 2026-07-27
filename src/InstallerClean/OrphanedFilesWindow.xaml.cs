using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class OrphanedFilesWindow : Window
{
    private string? _lastSortProperty;
    private ListSortDirection _lastSortDirection;
    private GridViewColumn? _lastSortColumn;

    public OrphanedFilesWindow(OrphanedFilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // The window always opens at this computed size; it does not
        // remember a previous one, so the size always suits the current OS
        // text setting and screen (a saved size scales with neither). 1000 x
        // 620 is the 100% default, multiplied by the text-scale factor
        // because the columns and the details pane inside scale with it, so
        // an unscaled default would overflow into a horizontal scrollbar.
        // The clamps keep the window inside the screen's work area.
        var factor = AccessibilitySettings.Current.TextScaleFactor;
        Width = DetailWindowSizing.ClampWidthToWorkArea(
            Application.Current?.MainWindow, preferred: 1000 * factor, minimum: MinWidth);
        Height = DetailWindowSizing.ClampHeightToWorkArea(
            Application.Current?.MainWindow, preferred: 620 * factor, minimum: MinHeight);

        Closed += OnClosed;
        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (FilesList.Items.Count > 0)
        {
            FilesList.SelectedIndex = 0;
            FilesList.ScrollIntoView(FilesList.Items[0]);
            var container = (ListViewItem?)FilesList.ItemContainerGenerator
                .ContainerFromIndex(0);
            container?.Focus();
        }

        // Initial state mirrors OrphanedFilesViewModel.ctor's
        // OrderByDescending(f => f.SizeBytes). Both sites must move
        // together; the cross-reference comment on the VM constructor
        // names this dependency.
        _lastSortProperty = nameof(OrphanedFile.SizeBytes);
        _lastSortDirection = ListSortDirection.Descending;
        _lastSortColumn = ColSize;
        UpdateSortIndicators();
    }

    private (string Plain, GridViewColumn Col)[] SortableColumns => new[]
    {
        (Strings.Field_File,   ColFileName),
        (Strings.Field_Reason, ColReason),
        (Strings.Field_Size,   ColSize),
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
    ///
    /// This list is as long as the cache folder, so the mechanism matters: a
    /// machine with 800,000 files in <c>C:\Windows\Installer</c> puts three
    /// quarters of a million rows behind these headers, and the sort runs on the
    /// UI thread. The obvious speed-up, swapping the property name for a typed
    /// comparer, is a step backwards, and the reason is not visible from here.
    ///
    /// A SortDescription costs one property read per ITEM, not one per
    /// comparison. ListCollectionView.PrepareLocalArray sorts its local
    /// ArrayList through MS.Internal.Data.DataExtensionMethods.Sort, which routes
    /// a SortFieldComparer into SortFieldComparer.SortHelper; that wraps every
    /// item in a CachedValueItem holding the resolved value, so the property path
    /// is evaluated once per row and every comparison reads the cached value.
    /// Any OTHER IComparer, which is what CustomSort takes, falls to the plain
    /// ArrayList.Sort branch with no cache at all. So replacing this with a typed
    /// comparer would trade one read per row for two reads per comparison.
    /// Measured over 776,000 of these rows, that trade loses on the File column:
    /// 1.4 s for the cached shape against 1.8 s for the per-comparison one,
    /// because FileName is a Path.GetFileName over the full path and the comparer
    /// would run it thirty million times rather than 776,000.
    ///
    /// What is left after that is culture-aware collation, which costs the same
    /// whichever mechanism asks for it and is the ordering the app wants (see
    /// RegisteredFilesWindow.ApplySort on why ordinal is not an option). It is
    /// the floor here, not an overhead to engineer away.
    /// </summary>
    private bool SortByColumn(GridViewColumnHeader header)
    {
        if (header.Column is null) return false;

        string? sortProperty = null;
        if (ReferenceEquals(header.Column, ColFileName)) sortProperty = nameof(OrphanedFile.FileName);
        else if (ReferenceEquals(header.Column, ColReason)) sortProperty = nameof(OrphanedFile.Reason);
        else if (ReferenceEquals(header.Column, ColSize)) sortProperty = nameof(OrphanedFile.SizeBytes);

        if (sortProperty is null) return false;

        var direction = sortProperty == _lastSortProperty && _lastSortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        // Deferred, so the pair below is one rebuild rather than two. Every
        // SortDescriptions mutation raises a CollectionChanged that
        // ListCollectionView turns into its own RefreshOrDefer, and Clear raises
        // one even when the collection is already empty, so undeferred this
        // rebuilds the view and resets the list once with no sort at all before
        // rebuilding it again with the wanted one.
        var view = CollectionViewSource.GetDefaultView(FilesList.ItemsSource);
        using (view.DeferRefresh())
        {
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(sortProperty, direction));
        }

        _lastSortProperty = sortProperty;
        _lastSortDirection = direction;
        _lastSortColumn = header.Column;
        UpdateSortIndicators();
        return true;
    }

    private void UpdateSortIndicators()
    {
        var arrow = _lastSortDirection == ListSortDirection.Ascending ? "  ▲" : "  ▼";
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
        // appended to the visible header text out of speech. ItemStatus
        // on the ListView alone is not enough: it is only surfaced for
        // the element with focus, and during a keyboard sort focus sits
        // on the header. The generated header controls are reached
        // through the visual tree; they exist whenever this runs,
        // because the first call comes from Loaded, after the template
        // is applied.
        foreach (var header in VisualTreeSearch.Descendants<GridViewColumnHeader>(FilesList))
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
        AutomationProperties.SetItemStatus(FilesList, sortStatus);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        if (DataContext is IDisposable vm) vm.Dispose();
    }
}
