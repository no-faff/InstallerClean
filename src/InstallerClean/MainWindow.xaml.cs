using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsComplete) && _vm.IsComplete)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => CompletionCloseButton.Focus());

        if (e.PropertyName == nameof(MainViewModel.IsOperating) && _vm.IsOperating)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => OperationCancelButton.Focus());
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
        else if (_vm.IsComplete && _vm.DismissCompletionCommand.CanExecute(null))
        {
            _vm.DismissCompletionCommand.Execute(null);
            e.Handled = true;
        }
        else if (!_vm.IsScanning)
        {
            Close();
            e.Handled = true;
        }
    }

    private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
