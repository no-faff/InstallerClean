using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using InstallerClean.Helpers;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _vm = viewModel;
        // Each child VM raises its own PropertyChanged stream. Listen
        // on all three so the window can move keyboard focus to the
        // most-relevant Cancel button as overlays appear.
        _vm.Completion.PropertyChanged += OnCompletionPropertyChanged;
        _vm.Cleanup.PropertyChanged += OnCleanupPropertyChanged;
        _vm.Scan.PropertyChanged += OnScanPropertyChanged;
        PreviewKeyDown += OnPreviewKeyDown;
        Closed += OnClosed;
        this.EnableAltSpaceSystemMenu();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _vm.Completion.PropertyChanged -= OnCompletionPropertyChanged;
        _vm.Cleanup.PropertyChanged -= OnCleanupPropertyChanged;
        _vm.Scan.PropertyChanged -= OnScanPropertyChanged;
        PreviewKeyDown -= OnPreviewKeyDown;
        Closed -= OnClosed;
    }

    private void OnCompletionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CompletionViewModel.IsComplete) && _vm.Completion.IsComplete)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => CompletionCloseButton.Focus());
    }

    private void OnCleanupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CleanupViewModel.IsOperating) && _vm.Cleanup.IsOperating)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => OperationCancelButton.Focus());
    }

    private void OnScanPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScanViewModel.IsScanning) && _vm.Scan.IsScanning)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => ScanCancelButton.Focus());
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        if (_vm.Cleanup.IsOperating && _vm.Cleanup.CancelOperationCommand.CanExecute(null))
        {
            _vm.Cleanup.CancelOperationCommand.Execute(null);
            e.Handled = true;
        }
        else if (_vm.Scan.IsScanning && _vm.Scan.CancelScanCommand.CanExecute(null))
        {
            _vm.Scan.CancelScanCommand.Execute(null);
            e.Handled = true;
        }
        else if (_vm.Completion.IsComplete && _vm.Completion.DismissCommand.CanExecute(null))
        {
            _vm.Completion.DismissCommand.Execute(null);
            e.Handled = true;
        }
        // No else branch: Esc on an idle top-level window must not close the app.
    }

    private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        MaximizeButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximise";
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
