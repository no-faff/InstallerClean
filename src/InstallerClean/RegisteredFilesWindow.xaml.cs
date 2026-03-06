using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class RegisteredFilesWindow : Window
{
    private string? _lastSortProperty;
    private ListSortDirection _lastSortDirection;

    private readonly ISettingsService? _settingsService;

    public RegisteredFilesWindow(RegisteredFilesViewModel viewModel, ISettingsService? settingsService = null)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsService = settingsService;

        var saved = settingsService?.Load().RegisteredWindowSize;
        if (saved is { Width: > 0, Height: > 0 })
        {
            Width = saved.Width;
            Height = saved.Height;
        }

        Closed += OnClosed;
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

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
    }

    private void ColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader header || header.Column is null)
            return;

        var sortProperty = header.Column.Header switch
        {
            "Product name" => "ProductName",
            "File" => "FileName",
            "Size" => "SizeBytes",
            "Patches" => "PatchCount",
            _ => null
        };

        if (sortProperty is null) return;

        var direction = sortProperty == _lastSortProperty && _lastSortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        var view = CollectionViewSource.GetDefaultView(ProductsList.ItemsSource);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(sortProperty, direction));

        _lastSortProperty = sortProperty;
        _lastSortDirection = direction;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_settingsService is null) return;
        var settings = _settingsService.Load();
        settings.RegisteredWindowSize = new Models.WindowSize { Width = ActualWidth, Height = ActualHeight };
        _settingsService.Save(settings);
    }
}
