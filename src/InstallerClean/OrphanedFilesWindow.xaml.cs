using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
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

    private readonly ISettingsService? _settingsService;

    public OrphanedFilesWindow(OrphanedFilesViewModel viewModel, ISettingsService? settingsService = null)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsService = settingsService;

        var saved = settingsService?.Load().OrphanedWindowSize;
        if (saved is { Width: > 0, Height: > 0 })
        {
            Width = saved.Width;
            Height = saved.Height;
        }
        else
        {
            // 920 x 540 is the 100% default; both multiply by the OS
            // text-scale factor because the columns and the details
            // pane inside scale with it, so an unscaled default would
            // open with the list overflowing into a horizontal
            // scrollbar. The clamps keep the window inside the
            // screen's work area.
            var factor = AccessibilitySettings.Current.TextScaleFactor;
            Width = DetailWindowSizing.ClampWidthToWorkArea(
                Application.Current?.MainWindow, preferred: 920 * factor, minimum: MinWidth);
            Height = DetailWindowSizing.ClampHeightToWorkArea(
                Application.Current?.MainWindow, preferred: 540 * factor, minimum: MinHeight);
        }

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
        if (e.OriginalSource is not GridViewColumnHeader header || header.Column is null)
            return;

        string? sortProperty = null;
        if (ReferenceEquals(header.Column, ColFileName)) sortProperty = nameof(OrphanedFile.FileName);
        else if (ReferenceEquals(header.Column, ColReason)) sortProperty = nameof(OrphanedFile.Reason);
        else if (ReferenceEquals(header.Column, ColSize)) sortProperty = nameof(OrphanedFile.SizeBytes);

        if (sortProperty is null) return;

        var direction = sortProperty == _lastSortProperty && _lastSortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        var view = CollectionViewSource.GetDefaultView(FilesList.ItemsSource);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(sortProperty, direction));

        _lastSortProperty = sortProperty;
        _lastSortDirection = direction;
        _lastSortColumn = header.Column;
        UpdateSortIndicators();
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
        // announced, so activating a header with Space speaks the new
        // state, and the name override keeps the sort-arrow glyph
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
        if (_settingsService is null) return;
        _ = _settingsService.Update(s =>
            s.OrphanedWindowSize = new Models.WindowSize { Width = ActualWidth, Height = ActualHeight });
    }
}
