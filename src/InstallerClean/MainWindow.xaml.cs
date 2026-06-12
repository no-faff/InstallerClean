using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
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
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => CompletionCloseButton.Focus());
            // The overlay was never revealed inside this window's lifetime
            // (the startup all-clear is set during the splash, before
            // construction), so the PropertyChanged raise path never runs
            // for it, and a newly shown window announces only its title
            // and the focused control. Without these raises the most
            // common outcome of all, the startup scan's "All clean", is
            // never spoken.
            AnnounceCompletionOutcome();
        }
        else if (!_vm.Scan.IsScanning)
            // The startup scan runs during the splash, so it has usually
            // finished by the time this window is built. When it found orphans
            // (no all-clear overlay) land focus on the results default rather
            // than leaving the bare window root unfocused, so a keyboard user
            // has a visible focus ring on open.
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => FocusResultsDefault());

        StarToolTip.CustomPopupPlacementCallback = PlaceAboveRightAligned;
        HeartToolTip.CustomPopupPlacementCallback = PlaceAboveRightAligned;

        // SizeToContent must never outgrow the screen: at large OS text
        // scales the content wants more height than the work area has,
        // and without the clamp the bottom nav renders under the taskbar.
        // No handle exists yet, so this resolves against the primary
        // work area; OnSourceInitialized re-applies it for the actual
        // monitor.
        ApplyWorkAreaBounds();

        // A live text-scale increase grows a shown SizeToContent window
        // down and right from its fixed top-left, which can push the
        // action rows off the work area even though the size clamps
        // hold. NoResize means SizeChanged only ever fires for that
        // content-driven growth, never a user drag-resize.
        SizeChanged += OnWindowSizeChanged;

        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
    }

    private void ApplyWorkAreaBounds()
    {
        MaxWidth = DetailWindowSizing.WorkAreaWidthLimit(this);
        MaxHeight = DetailWindowSizing.WorkAreaHeightLimit(this);
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        => DetailWindowSizing.NudgeIntoWorkArea(this);

    // PlacementMode.Top aligns the tooltip's left edge with the
    // target's and grows rightward, so on the bottom-right corner
    // icons the tooltip crosses the window edge onto the desktop
    // (popups respect screen edges, not window edges), and no mode in
    // the enum aligns right edges. This pins the tooltip's right edge
    // to the button's right edge, flush above, which keeps it inside
    // the window and matches the About window's pair, whose buttons
    // sit at the window's left where plain Top placement already
    // lands inside. The second candidate (flush below) is taken by
    // WPF only when there is no room above, e.g. the window dragged
    // to the top of the screen.
    private static CustomPopupPlacement[] PlaceAboveRightAligned(Size popupSize, Size targetSize, Point offset) =>
    [
        new CustomPopupPlacement(new Point(targetSize.Width - popupSize.Width, -popupSize.Height), PopupPrimaryAxis.Horizontal),
        new CustomPopupPlacement(new Point(targetSize.Width - popupSize.Width, targetSize.Height), PopupPrimaryAxis.Horizontal)
    ];

    private void OnClosed(object? sender, EventArgs e)
    {
        _vm.Completion.PropertyChanged -= OnCompletionPropertyChanged;
        _vm.Cleanup.PropertyChanged -= OnCleanupPropertyChanged;
        _vm.Scan.PropertyChanged -= OnScanPropertyChanged;
        PreviewKeyDown -= OnPreviewKeyDown;
        SizeChanged -= OnWindowSizeChanged;
        Closed -= OnClosed;
    }

    private void OnCompletionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CompletionViewModel.IsComplete) && _vm.Completion.IsComplete)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => CompletionCloseButton.Focus());
            // Focus lands on Done, so without an explicit raise a screen
            // reader announces only the button and never the outcome. The
            // heading, summary and restore lines are plain TextBlocks
            // revealed from Visibility=Collapsed, which the WPF UIA
            // bridge does not re-announce on its own (the same gap the
            // banners work around).
            AnnounceCompletionOutcome();
        }

        if (e.PropertyName == nameof(CompletionViewModel.IsComplete) && !_vm.Completion.IsComplete)
        {
            // Overlay dismissed (Done / Esc / click-dim). The focused button is
            // gone, so move focus to a sensible non-destructive control rather
            // than letting it drop to the window root. RescanAfterCompletion
            // also clears IsComplete but immediately starts a scan; the
            // IsScanning guard defers to the scanning overlay's own focus then.
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                if (!_vm.Scan.IsScanning)
                    RescanButton.Focus();
            });
        }

        // ResultLogStatusMessage transitions empty -> non-empty on the
        // first "Sending..." reveal. WPF's UIA bridge does not re-fire
        // LiveRegionChanged for the Visibility=Collapsed→Visible
        // DataTrigger that gates the TextBlock, so without an explicit
        // raise the SR stays silent precisely when the user has just
        // consented to a network call and wants confirmation. Later
        // text changes (Sending -> Sent / Failed) fire LiveRegionChanged
        // through the bridge normally because the TextBlock is already
        // in the rendered tree by then.
        if (e.PropertyName == nameof(CompletionViewModel.ResultLogStatusMessage)
            && !string.IsNullOrEmpty(_vm.Completion.ResultLogStatusMessage))
        {
            AnnounceLiveRegions(ResultLogStatusText);
        }
    }

    private void OnCleanupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CleanupViewModel.IsOperating) && _vm.Cleanup.IsOperating)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => OperationCancelButton.Focus());
            // The view-model assigns the "Moving/Deleting N files..." /
            // "Preparing destination folder..." heading after IsOperating
            // flips, so the overlay reveals with stale text and the UIA
            // bridge announces neither the Collapsed-to-Visible reveal nor
            // a text change landing before the first render. Without the
            // raise the first thing spoken about an operation is a bare
            // file count.
            AnnounceLiveRegions(OperationHeadingText);
        }
    }

    private void OnScanPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScanViewModel.IsScanning) && _vm.Scan.IsScanning)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => ScanCancelButton.Focus());

        if (e.PropertyName == nameof(ScanViewModel.IsScanning) && !_vm.Scan.IsScanning)
            // A scan just finished. If it found orphans (no all-clear overlay)
            // the scanning overlay's cancel-button focus is gone, so route focus
            // to the results default. If it found nothing the all-clear overlay
            // is up and OnCompletionPropertyChanged focuses Done; the IsComplete
            // guard skips this path then.
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                if (!_vm.Completion.IsComplete)
                    FocusResultsDefault();
            });

        // WPF's UIA bridge does not re-fire LiveRegionChanged for a
        // Visibility=Collapsed→Visible transition; the bridge only
        // announces text changes inside an already-rendered subtree, so
        // a banner's appearance needs an explicit raise. Skipped while an
        // operation or its completion overlay owns the foreground: the
        // post-operation silent refresh can flip a banner while it sits
        // behind the overlay, and its paragraph of text would queue ahead
        // of the completion outcome; the banner stays on screen for
        // scan-mode reading once the overlay dismisses.
        if (_vm.Cleanup.IsOperating || _vm.Completion.IsComplete)
            return;
        if (e.PropertyName == nameof(ScanViewModel.HasPendingReboot) && _vm.Scan.HasPendingReboot)
            AnnounceLiveRegions(PendingRebootBannerText);
        if (e.PropertyName == nameof(ScanViewModel.HasMissingFromDisk) && _vm.Scan.HasMissingFromDisk)
            AnnounceLiveRegions(MissingFromDiskBannerText);
        if (e.PropertyName == nameof(ScanViewModel.HasStaleMsiEntries) && _vm.Scan.HasStaleMsiEntries)
            AnnounceLiveRegions(StaleMsiEntriesText);
    }

    // Routes focus to the move-destination field, the entry point of the Move
    // workflow, for the results-shown state that appears with no overlay (the
    // startup scan or a manual re-scan that found orphans). Never the
    // destructive Delete. A no-op when an overlay has disabled the main content,
    // because Focus() cannot land on a disabled control.
    private void FocusResultsDefault() => MoveDestinationInput.Focus();

    /// <summary>
    /// Queues LiveRegionChanged raises for <paramref name="elements"/> at
    /// Background priority. The priority is the contract: dispatcher
    /// priorities are serviced highest value first, and Loaded (6)
    /// outranks Input (5), so a raise queued at Loaded lands BEFORE a
    /// focus move queued at Input, and the focus announcement then
    /// cancels the queued polite speech (NVDA documents
    /// cancel-on-focus; Narrator behaves the same in practice, which is
    /// how the completion outcome went unheard). Background (4) sits
    /// below Input, so the raises follow every focus event queued in the
    /// same drain and the polite items speak once the focus announcement
    /// finishes. Background still runs after data binding and render, so
    /// the peers carry the final text.
    /// </summary>
    private void AnnounceLiveRegions(params FrameworkElement[] elements)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            foreach (var element in elements)
                RaiseLiveRegionChanged(element);
        });
    }

    // The error list stays unraised: the summary already speaks the
    // error count, and the per-file breakdown is scan-mode reading. The
    // restore line carries the second half of the outcome (where the
    // files went after a Move or Delete; the scan receipt on an
    // all-clear).
    private void AnnounceCompletionOutcome() =>
        AnnounceLiveRegions(CompletionHeadingText, CompletionSummaryText, CompletionRestoreText);

    private static void RaiseLiveRegionChanged(FrameworkElement element)
    {
        var peer = UIElementAutomationPeer.FromElement(element) ?? UIElementAutomationPeer.CreatePeerForElement(element);
        peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
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

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(hwnd)?.AddHook(SuppressMaximize);
        // The handle exists now, so the limits resolve against the
        // monitor actually hosting the window rather than the primary.
        ApplyWorkAreaBounds();
    }

    // The main window's centred-column layout caps its width (672
    // content units, text-scaled) and does not fill a maximised
    // viewport: the content stays in the middle
    // with the dark sidebar surface around it. The custom chrome
    // therefore offers Minimise and Close only, but title-bar
    // double-click, Win+Up and the system menu's Maximize item still
    // dispatch SC_MAXIMIZE through WM_SYSCOMMAND. Silencing the
    // command at the message pump removes those paths to the
    // misshapen state. The low four bits of wParam are reserved for
    // menu state (WM_SYSCOMMAND documentation), so the mask 0xFFF0
    // is required before comparing the command code.
    private static IntPtr SuppressMaximize(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_SYSCOMMAND = 0x0112;
        const int SC_MAXIMIZE = 0xF030;
        if (msg == WM_SYSCOMMAND && (wParam.ToInt32() & 0xFFF0) == SC_MAXIMIZE)
            handled = true;
        return IntPtr.Zero;
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
