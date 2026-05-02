using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Threading;
using InstallerClean.Helpers;
using InstallerClean.Resources;
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

        // The splash-driven startup scan can complete (and Completion
        // .ShowAllClear() can already have set IsComplete=true) before
        // this window is constructed. The PropertyChanged subscription
        // above only catches state changes from now on, so it doesn't
        // replay the all-clear that already fired. Replay it manually:
        // if the overlay is already up at construction, route focus into
        // it so Tab lands inside the overlay (and the overlay's
        // KeyboardNavigation.TabNavigation="Cycle" keeps it there)
        // rather than starting on a main-window button behind the
        // overlay.
        if (_vm.Completion.IsComplete)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => CompletionCloseButton.Focus());

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
        // Tooltip + automation name update happens in OnStateChanged so
        // double-click-on-title-bar (which bypasses this handler) keeps
        // the labels in sync too.
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    // Title-bar glyph swap matches Windows's own caption buttons.
    private const string MaximizeGlyph = "□";
    private const string RestoreGlyph = "❐";

    /// <summary>
    /// Keeps the caption button's glyph, tooltip and automation name
    /// in sync with the window state regardless of how it was changed.
    /// </summary>
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        var maximised = WindowState == WindowState.Maximized;
        MaximizeButton.Content = maximised ? RestoreGlyph : MaximizeGlyph;
        MaximizeButton.ToolTip = maximised ? Strings.Tooltip_Restore : Strings.Tooltip_Maximise;
        AutomationProperties.SetName(MaximizeButton,
            maximised ? Strings.Tooltip_Restore : Strings.Automation_Maximise);
    }

    /// <summary>
    /// Click-outside-to-dismiss for the result overlay. Routed via
    /// the dim Rectangle's MouseLeftButtonDown so only a click on the
    /// dim margin triggers it; clicks on the inner content card are
    /// absorbed by their own hit-testing.
    /// </summary>
    private void CompletionDimAreaClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm.Completion.IsComplete && _vm.Completion.DismissCommand.CanExecute(null))
        {
            _vm.Completion.DismissCommand.Execute(null);
            e.Handled = true;
        }
    }
}
