using System.Windows;
using System.Windows.Controls;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class OrphanedFilesWindow : Window
{
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
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (FilesList.Items.Count > 0)
        {
            FilesList.SelectedIndex = 0;
            FilesList.ScrollIntoView(FilesList.Items[0]);
            var container = (ListBoxItem?)FilesList.ItemContainerGenerator
                .ContainerFromIndex(0);
            container?.Focus();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_settingsService is null) return;
        var settings = _settingsService.Load();
        settings.OrphanedWindowSize = new Models.WindowSize { Width = ActualWidth, Height = ActualHeight };
        _settingsService.Save(settings);
    }
}
