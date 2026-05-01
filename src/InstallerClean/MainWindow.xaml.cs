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
        _vm.PropertyChanged += OnViewModelPropertyChanged;
        PreviewKeyDown += OnPreviewKeyDown;
        Closed += OnClosed;
        this.EnableAltSpaceSystemMenu();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _vm.PropertyChanged -= OnViewModelPropertyChanged;
        PreviewKeyDown -= OnPreviewKeyDown;
        Closed -= OnClosed;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsComplete) && _vm.IsComplete)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => CompletionCloseButton.Focus());

        if (e.PropertyName == nameof(MainViewModel.IsOperating) && _vm.IsOperating)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => OperationCancelButton.Focus());

        if (e.PropertyName == nameof(MainViewModel.IsScanning) && _vm.IsScanning)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => ScanCancelButton.Focus());
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        if (_vm.IsOperating && _vm.CancelOperationCommand.CanExecute(null))
        {
            _vm.CancelOperationCommand.Execute(null);
            e.Handled = true;
        }
        else if (_vm.IsScanning && _vm.CancelScanCommand.CanExecute(null))
        {
            _vm.CancelScanCommand.Execute(null);
            e.Handled = true;
        }
        else if (_vm.IsComplete && _vm.DismissCompletionCommand.CanExecute(null))
        {
            _vm.DismissCompletionCommand.Execute(null);
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
