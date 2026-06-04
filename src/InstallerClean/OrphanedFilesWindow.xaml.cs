using System.ComponentModel;
using System.Windows;
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
        foreach (var (plain, col) in SortableColumns)
        {
            col.Header = ReferenceEquals(col, _lastSortColumn) ? plain + arrow : plain;
        }
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
