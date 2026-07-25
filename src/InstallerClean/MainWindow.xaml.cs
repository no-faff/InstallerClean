using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;
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
        _vm.Chrome.PropertyChanged += OnChromePropertyChanged;
        _vm.Scan.ScanCompleted += OnScanCompleted;
        PreviewKeyDown += OnPreviewKeyDown;
        Closing += OnClosing;
        Closed += OnClosed;
        // The pair exists only for the update button's focus restore; see
        // _focusBeforeDeactivation.
        Deactivated += OnDeactivatedDuringUpdateCheck;
        Activated += OnActivatedDuringUpdateCheck;

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
            // Neither the summary nor the restore line has a Text binding
            // (each hosts inlines composed in code), so build both now for
            // the overlay already up at construction; the PropertyChanged
            // path that normally builds them never fired for this
            // pre-construction completion (the startup all-clear set during
            // the splash).
            BuildCompletionSummaryLine();
            BuildCompletionRestoreLine();
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
        {
            // The startup scan runs during the splash, so it has usually
            // finished by the time this window is built. When it found orphans
            // (no all-clear overlay) land focus on the results default rather
            // than leaving the bare window root unfocused, so a keyboard user
            // has a visible focus ring on open.
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => FocusResultsDefault());
            // Its ScanCompleted fired during the splash, before this
            // window existed, so the subscription above never saw it;
            // replay the result announcement the same way the all-clear
            // branch replays its raises.
            OnScanCompleted(this, EventArgs.Empty);

            // Cancelled at the splash: there is no scan to announce, and the
            // scanning overlay this window normally re-announces the cancel from
            // was never up inside its lifetime. Say why the window is empty.
            // Background, below the focus move queued above, so the focus
            // announcement does not cancel this polite one (see
            // AnnounceLiveRegions for the priority contract).
            if (!_vm.Scan.HasScanned && _vm.Scan.LastScanWasCancelled)
                Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                    ScanResultAnnouncer.Text = Strings.Status_ScanCancelled);

            // A failed startup scan opens the window with the tailored error in the
            // intro instead of exiting. A newly shown window speaks only its title
            // and the focused Re-scan button, so announce the diagnosis too.
            if (!_vm.Scan.HasScanned && _vm.Scan.HasScanError)
                Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                    ScanResultAnnouncer.Text = InstallerPathText.KeepWhole(_vm.Scan.LastScanError));
        }

        CompletionDonateToolTip.CustomPopupPlacementCallback = PlaceAboveRightAligned;

        // Width is explicit, the designed 828 (the content column's 780
        // MaxWidth plus the content margins) multiplied by the
        // text-scale factor; height sizes to content with the root
        // grid's work-area MaxHeight as its ceiling, so at large OS
        // text scales the bottom nav stays above the taskbar. Both are
        // first assigned here, before the window handle exists, the
        // shape every band-free window in the app uses, and no
        // constraint sits on the window itself. Sized to content in
        // both axes instead
        // (SizeToContent="WidthAndHeight" with work-area limits
        // re-applied from OnSourceInitialized), the window opened
        // larger than its arranged content by exactly the standard
        // caption frame, 37 by 14 device-independent units of unpainted
        // black along the bottom and right edges (observed 2026-06-13
        // at 125% monitor scale, custom WindowChrome active). With no
        // handle yet this resolves against the primary work area, where
        // CenterScreen opens the window anyway; a live text-scale
        // change re-resolves against the actual monitor.
        ApplyWorkAreaBounds();
        AccessibilitySettings.Current.PropertyChanged += OnAccessibilitySettingsChanged;

        // A live text-scale increase re-applies the scaled bounds and
        // the shown window grows down and right from its fixed
        // top-left, which can push the action rows off the work area
        // even though the size clamps hold. NoResize means SizeChanged
        // only ever fires for that growth, never a user drag-resize.
        SizeChanged += OnWindowSizeChanged;

        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
    }

    private void ApplyWorkAreaBounds()
    {
        // Width is assigned directly; SizeToContent is not toggled here. Toggling it
        // to Manual around the Width assignment opens a fresh high-text-scale
        // launch NARROW, the enlarged text wrapping cramped (observed
        // 2026-06-13 at ~199%): the toggle runs in this constructor-time call
        // and disturbs the first content fit. The plain assignment keeps the
        // XAML SizeToContent="Height" and the explicit width intact, which
        // sizes correctly. The trade-off is no live shrink when the text size
        // is lowered with the app already open; a restart re-fits, which is
        // acceptable.
        RootLayout.MaxHeight = DetailWindowSizing.WorkAreaHeightLimit(this);
        Width = DetailWindowSizing.ClampWidthToWorkArea(
            this, 828 * AccessibilitySettings.Current.TextScaleFactor, 0);
    }

    private void OnAccessibilitySettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(AccessibilitySettings.TextScaleFactor))
            ApplyWorkAreaBounds();
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        => DetailWindowSizing.NudgeIntoWorkArea(this);

    /// <summary>
    /// True once a close has been requested during a Move or a Delete: the
    /// close is held, the operation cancelled, and the window closed for real
    /// when the operation lets go.
    /// </summary>
    private bool _closeHeldForOperation;

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        // IsOperating, the overlay flag, and not IsOperationInFlight, the
        // execution gate the two destructive commands use. That is a contract,
        // not the near-miss it looks like. The hold is released by
        // OnCleanupPropertyChanged watching this same property go false, so a
        // hold taken on the other flag would never be released: nothing raises a
        // change notification the close is waiting on. And the window where the
        // two disagree costs nothing, because it is before any file is touched:
        // IsOperationInFlight covers the pre-flights, both of which set
        // IsOperating immediately before calling the service, so every moment a
        // file is being moved or recycled is inside this flag.
        if (!_vm.Cleanup.IsOperating) return;

        // A Move or a Delete is running. Letting this close through kills the
        // worker where it stands: OnExit disposes the container, which cancels
        // the token but waits for nothing, and the batch is on a background
        // thread, so the process exits from under it. A cross-volume move is a
        // copy performed in this process (File.Move passes
        // MOVEFILE_COPY_ALLOWED, and Win32 documents that flag as a CopyFile
        // plus a DeleteFile), so dying mid-file leaves a truncated .msi in the
        // destination folder with the source still in place: a file that looks
        // like a recovery copy and is not one, in an app whose whole promise is
        // that it never leaves you worse off.
        //
        // So: hold the close, cancel the batch, and take the close when the
        // operation lets go. Cancelling stops it at the next file boundary, so
        // the file being moved right now is finished rather than truncated. The
        // wait is visible: the operating overlay stays up and says so.
        e.Cancel = true;
        if (_closeHeldForOperation) return;
        _closeHeldForOperation = true;
        if (_vm.Cleanup.CancelOperationCommand.CanExecute(null))
            _vm.Cleanup.CancelOperationCommand.Execute(null);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _vm.Completion.PropertyChanged -= OnCompletionPropertyChanged;
        _vm.Cleanup.PropertyChanged -= OnCleanupPropertyChanged;
        _vm.Scan.PropertyChanged -= OnScanPropertyChanged;
        _vm.Chrome.PropertyChanged -= OnChromePropertyChanged;
        _vm.Scan.ScanCompleted -= OnScanCompleted;
        PreviewKeyDown -= OnPreviewKeyDown;
        Deactivated -= OnDeactivatedDuringUpdateCheck;
        Activated -= OnActivatedDuringUpdateCheck;
        AccessibilitySettings.Current.PropertyChanged -= OnAccessibilitySettingsChanged;
        SizeChanged -= OnWindowSizeChanged;
        Closing -= OnClosing;
        Closed -= OnClosed;
    }

    /// <summary>
    /// Announces the headline result of a user-visible scan that found
    /// files ("12 unneeded files to clean up (3.2 GB)"). The all-clear
    /// path announces through the completion overlay instead, and the
    /// silent post-operation refresh must stay silent because the
    /// completion outcome is about to speak. Writing the announcer's
    /// text is itself what fires the UIA bridge's text-change
    /// announcement; an explicit raise on top would queue the same line
    /// twice. A repeat scan with identical counts sets an equal string,
    /// which raises no event and stays unannounced; the "Scan complete"
    /// milestone still speaks then.
    /// </summary>
    private void OnScanCompleted(object? sender, EventArgs e)
    {
        if (_vm.Cleanup.IsOperating || _vm.Completion.IsComplete || _vm.Scan.OrphanedFileCount == 0)
        {
            // Clear any prior found-files headline so scan-mode / Inspect
            // navigation cannot land on a stale "N unneeded files to clean
            // up" after a clean-up or an all-clear, where the visible counts
            // now read zero. The element is always rendered (Opacity 0), so
            // it persists its last text until overwritten; the sibling
            // progress twin is reset the same way in the operation finally.
            ScanResultAnnouncer.Text = string.Empty;
            return;
        }
        // Clear synchronously before the Background re-set so a manual
        // Re-scan that finds the SAME files still re-announces: a live region
        // raises no event when assigned the text it already holds, so without
        // the clear a re-scan with an unchanged count would speak nothing and
        // the user, who deliberately asked to scan again, would hear only the
        // window. Clear-then-set guarantees a text change either way; the
        // empty value speaks nothing, the headline that follows speaks once.
        ScanResultAnnouncer.Text = string.Empty;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            ScanResultAnnouncer.Text = string.Format(Strings.Automation_ScanResultAnnouncement,
                _vm.Scan.OrphanedSummaryText, _vm.Scan.OrphanedSizeDisplay));
    }

    private void OnCompletionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Summary and Restore are both set on every Show* path before
        // IsComplete flips, so rebuilding their inlines here means they are
        // in place by the time the IsComplete branch below announces the
        // outcome.
        if (e.PropertyName == nameof(CompletionViewModel.Summary))
            BuildCompletionSummaryLine();

        if (e.PropertyName == nameof(CompletionViewModel.Restore))
            BuildCompletionRestoreLine();

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

        // The Send-summary button collapses the moment the user consents
        // (the modal closes and IsSendingResultLog hides it) or the
        // silent no-log-to-send path hides it; WPF's focus restore after
        // the confirm modal then has no target and keyboard focus drops
        // to the window root. Done is the landing that keeps the user
        // inside the overlay. Dismissal paths are excluded because they
        // clear IsComplete before the visibility recomputes.
        if (e.PropertyName == nameof(CompletionViewModel.IsSendResultLogVisible)
            && !_vm.Completion.IsSendResultLogVisible && _vm.Completion.IsComplete)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                if (_vm.Completion.IsComplete)
                    CompletionCloseButton.Focus();
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

    /// <summary>
    /// Whether the plain update-status line is in the rendered tree. The UIA
    /// bridge announces a text change inside an already-rendered subtree by
    /// itself but never a Collapsed-to-Visible reveal, so the reveal is the
    /// one transition the window has to raise, and raising the rest as well
    /// would speak them twice.
    /// </summary>
    private bool _updateStatusLineShown;

    /// <summary>
    /// Both halves of the update status line start life Collapsed, and each
    /// appears by a DataTrigger rather than by its text changing, which is
    /// the reveal the UIA bridge does not announce (the same gap the
    /// pending-reboot banner and the result-log status line each carry an
    /// explicit raise for). Both halves need it, for opposite reasons: the
    /// automatic check's find lands unprompted with focus wherever the user
    /// left it, and the manual check's "Checking..." is the answer to a click
    /// the user is actively waiting on, which without it goes unspoken.
    /// </summary>
    private void OnChromePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChromeViewModel.HasUpdateLink) && _vm.Chrome.HasUpdateLink)
        {
            AnnounceLiveRegions(UpdateLinkText);
            return;
        }

        if (e.PropertyName != nameof(ChromeViewModel.UpdateStatusText)) return;

        // Mirrors the plain line's own visibility triggers: it shows when
        // there is text and no link to show instead.
        var shown = !_vm.Chrome.HasUpdateLink && _vm.Chrome.UpdateStatusText.Length > 0;
        if (shown && !_updateStatusLineShown)
            AnnounceLiveRegions(UpdateStatusLineText);
        _updateStatusLineShown = shown;
    }

    /// <summary>
    /// Whether the update button held keyboard focus when the manual check
    /// took it away. The command disallows concurrent execution and its own
    /// execution covers the cooldown, so the button is disabled from the
    /// press until several seconds after the answer, and WPF drops focus to
    /// the window root when the focused element is disabled.
    /// </summary>
    private bool _restoreFocusToUpdateButton;

    /// <summary>
    /// Keyboard focus as it stood when this window last lost activation
    /// during a manual check, and, when reactivation put focus somewhere
    /// else, where it was put. Both handlers run only while the restore
    /// window is open, which is the same span in which the pressed button
    /// sits disabled and focus is therefore orphaned, so any reactivation
    /// that lands focus on a control records it: the update dialog's
    /// dismissal, an Alt+Tab round trip, whatever put activation back. That
    /// breadth is the point rather than a leak, because in that span the
    /// choice is always WPF's and never the user's, and undoing WPF's choice
    /// is what the restore exists for. The comparison earns its keep at the
    /// other end: focus that comes back to where it already was is not a
    /// choice at all, so it leaves the record alone, and focus the user
    /// moved themselves matches nothing and survives.
    /// </summary>
    private IInputElement? _focusBeforeDeactivation;
    private IInputElement? _focusPlacedOnReactivation;

    private void OnDeactivatedDuringUpdateCheck(object? sender, EventArgs e)
    {
        if (!_restoreFocusToUpdateButton) return;
        _focusBeforeDeactivation = FocusManager.GetFocusedElement(this);
    }

    // Read a dispatcher pass later than the event: WPF restores focus off
    // the same activation, and reading from the handler would see the state
    // before the restore rather than after it. Background so it also follows
    // anything the app itself queues at Input from an activation.
    //
    // Records only a move, never a stay, so a later Alt+Tab out and back does
    // not erase what the dialog did: the cooldown outlasts the dialog by
    // several seconds and there is time for one.
    private void OnActivatedDuringUpdateCheck(object? sender, EventArgs e)
    {
        if (!_restoreFocusToUpdateButton) return;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            var focused = FocusManager.GetFocusedElement(this);
            if (!ReferenceEquals(focused, _focusBeforeDeactivation))
                _focusPlacedOnReactivation = focused;
        });
    }

    // ButtonBase raises Click before it invokes the command, so this reads
    // the focus state from before the disable. A press made while focus sat
    // somewhere else orphans nothing, and pulling focus across the bar when
    // the button comes back would be the bug.
    private void UpdateCheckButton_Click(object sender, RoutedEventArgs e)
    {
        _restoreFocusToUpdateButton = UpdateCheckButton.IsKeyboardFocused;
        _focusBeforeDeactivation = null;
        _focusPlacedOnReactivation = null;
    }

    /// <summary>
    /// Hands the button its focus back once the check and the cooldown are
    /// done, so a keyboard user who pressed Enter on it is not left ringless
    /// with Tab restarting from the first stop. Restores in two cases, both
    /// of them focus the user did not choose: orphaned, which reads back as
    /// null or the window itself, and still sitting exactly where the update
    /// dialog's dismissal dropped it. A check that ends "Up to date." shows
    /// no dialog and only ever takes the first; a check that finds something
    /// ends in the dialog and takes the second, several seconds after it is
    /// dismissed, because the cooldown outlasts it. Focus the user has since
    /// moved elsewhere matches neither and is left alone. The guard on the
    /// disable being this button's own matters as well: the whole bottom bar
    /// is disabled while an overlay is up, and that path has its own focus
    /// destination.
    /// </summary>
    private void UpdateCheckButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true || !_restoreFocusToUpdateButton) return;
        _restoreFocusToUpdateButton = false;
        var focused = FocusManager.GetFocusedElement(this);
        var placedByDialog = _focusPlacedOnReactivation is not null
                             && ReferenceEquals(focused, _focusPlacedOnReactivation);
        _focusBeforeDeactivation = null;
        _focusPlacedOnReactivation = null;
        if (focused is null or MainWindow || placedByDialog)
            UpdateCheckButton.Focus();
    }

    private void OnCleanupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CleanupViewModel.IsOperating) && !_vm.Cleanup.IsOperating
            && _closeHeldForOperation)
        {
            // The close the user asked for during the operation. Deferred to a
            // later dispatcher pass rather than closed from here: this fires
            // inside the operation's finally, and tearing the window (and with
            // it the DI container, and with it these view-models) down while
            // that frame is still on the stack is a re-entrancy nobody should
            // have to reason about.
            Dispatcher.BeginInvoke(DispatcherPriority.Background, Close);
            return;
        }

        if (e.PropertyName == nameof(CleanupViewModel.IsOperating) && !_vm.Cleanup.IsOperating)
        {
            // The overlay's focused Cancel button collapses with the
            // overlay, and when no completion overlay follows (a
            // cancelled operation, a failure dialog, the bin-unavailable
            // refusal) keyboard focus would drop to the window root:
            // no ring, Tab restarting from the first stop, a screen
            // reader gone quiet. Normal priority, not Input:
            // PropertyChanged fires inside the operation's finally,
            // before the awaiting caller's continuation is posted
            // (DispatcherSynchronizationContext posts at Normal), so
            // same-priority FIFO runs this callback first and a
            // follow-up modal (the bin-unavailable choice) opens with a
            // live focus target in the owner window to restore to on
            // close.
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                if (!_vm.Completion.IsComplete && !_vm.Scan.IsScanning && !_vm.Cleanup.IsOperating)
                    FocusResultsDefault();
            });
        }

        if (e.PropertyName == nameof(CleanupViewModel.IsOperating) && _vm.Cleanup.IsOperating)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => OperationCancelButton.Focus());
            // The heading is already bound when IsOperating flips (the
            // view-model assigns it first) and the UIA bridge does not
            // announce a Collapsed-to-Visible reveal, so without this
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
        {
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

            // The cancelled-scan outcome ("Scan cancelled.") is set on the
            // overlay's status line as the overlay collapses, in the same
            // dispatcher frame as the focus move above; the focus move's own
            // name announcement cancels that pending polite speech, so it
            // goes unspoken. Re-announce it on the persistent announcer at
            // Background, below the Input focus, so it survives. Only on a
            // user cancel, and not when an all-clear overlay will speak its
            // own outcome.
            if (_vm.Scan.LastScanWasCancelled && !_vm.Completion.IsComplete)
                Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                    ScanResultAnnouncer.Text = Strings.Status_ScanCancelled);
        }

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
        if (e.PropertyName == nameof(ScanViewModel.HasUnreadableProducts) && _vm.Scan.HasUnreadableProducts)
            AnnounceLiveRegions(ProgramsUnreadableText);
    }

    // Routes focus to the move-destination field, the entry point of the Move
    // workflow, for the results-shown state that appears with no overlay (the
    // startup scan or a manual re-scan that found orphans). Never the
    // destructive Delete.
    //
    // The field is only on screen when the scan found something to move, and
    // Focus() cannot land on a collapsed control, so the two states without an
    // action zone (nothing found, nothing scanned yet) fall back to Re-scan,
    // which is what those states offer. Both calls returning false is the
    // overlay case: Focus() also fails on a disabled control, and the bottom nav
    // is disabled with the rest of the body while an overlay is up, so this
    // stays the no-op it has always been there.
    private void FocusResultsDefault()
    {
        if (!MoveDestinationInput.Focus())
            RescanButton.Focus();
    }

    // PlacementMode.Top aligns the tooltip's left edge with the target's and
    // grows rightward, so a tooltip on a control near the right of the window
    // runs off it (popups respect screen edges, not window edges), and no mode
    // in the enum aligns right edges. This pins the tooltip's right edge to the
    // target's, flush above. The completion card's donate heart needs it: the
    // heart sits at the card's right edge and its wrapped two-line tooltip is
    // wider than the gap from there to the window edge. The second candidate
    // (flush below) is taken by WPF only when there is no room above, e.g. the
    // window dragged to the top of the screen.
    private static CustomPopupPlacement[] PlaceAboveRightAligned(Size popupSize, Size targetSize, Point offset) =>
    [
        new CustomPopupPlacement(new Point(targetSize.Width - popupSize.Width, -popupSize.Height), PopupPrimaryAxis.Horizontal),
        new CustomPopupPlacement(new Point(targetSize.Width - popupSize.Width, targetSize.Height), PopupPrimaryAxis.Horizontal)
    ];

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
    /// same drain and a polite item speaks once the focus announcement
    /// finishes. Background still runs after data binding and render, so
    /// the peers carry the final text. The plain update status line is the
    /// one assertive caller, so it cuts in rather than waiting its turn; why
    /// that is acceptable there is on the element itself, in MainWindow.xaml.
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
    // restore line carries the second half of the outcome (how to reclaim
    // the space and undo it after a Delete, where the files went after a
    // Move, the scan receipt on an all-clear).
    private void AnnounceCompletionOutcome() =>
        AnnounceLiveRegions(CompletionHeadingText, CompletionSummaryText, CompletionRestoreText);

    // Stable README anchor (an explicit <a id="is-it-safe"> before the
    // "Is it safe?" section of every README, so rewording a heading never
    // breaks the link). The post-Move / post-Delete restore hints each carry a
    // bracket-delimited phrase that links here, the reasoning behind the claim
    // that a cleaned file was safe to remove; the URL targets the README in the
    // displayed language.
    private static string SafetyUrl => ReadmeLinks.For("is-it-safe", Localisation.UiCulture);

    /// <summary>
    /// Composes the completion summary line from <see cref="CompletionViewModel.Summary"/>,
    /// forcing the destination path (<see cref="CompletionViewModel.SummaryDestination"/>)
    /// onto its own line at the exact point it substitutes into the formatted
    /// sentence, whatever word order the target language uses (mirrors
    /// ConfirmMoveWindow's destination-on-its-own-line treatment). A value with
    /// no destination (the all-clear receipt, the delete-to-Recycle-Bin
    /// summary) renders verbatim as a single Run. Locating the raw substring
    /// rather than a bracket-delimited marker (as <see cref="BuildCompletionRestoreLine"/>
    /// uses for its hyperlink) is deliberate: the destination is a user-chosen
    /// folder path that could itself contain a literal '[' or ']'.
    /// </summary>
    private void BuildCompletionSummaryLine()
    {
        // Bound before the split, not per Run: KeepWhole only ever inserts
        // between the installer path's own characters, so the destination
        // search below still finds its substring. A destination cannot be
        // inside C:\Windows\Installer, MoveFilesService refusing one that
        // resolves there, so the two can never overlap.
        var raw = InstallerPathText.KeepWhole(_vm.Completion.Summary);
        var destination = _vm.Completion.SummaryDestination;
        CompletionSummaryText.Inlines.Clear();

        // The split point and the path-wrap-point insertion are both pure string
        // work, extracted to Core so a test can pin them (the shipped zero-width
        // escape bug lived here and had no test); this method only turns the
        // result into WPF inlines. See CompositionParsing.
        if (CompositionParsing.SplitAtSubstring(raw, destination) is not { } split)
        {
            CompletionSummaryText.Inlines.Add(new Run(raw));
            return;
        }

        var wrappedDestination = CompositionParsing.InsertPathWrapPoints(destination);

        if (split.Prefix.Length > 0) CompletionSummaryText.Inlines.Add(new Run(split.Prefix));
        CompletionSummaryText.Inlines.Add(new LineBreak());
        CompletionSummaryText.Inlines.Add(new Run(wrappedDestination));
        if (split.Suffix.Length > 0) CompletionSummaryText.Inlines.Add(new Run(split.Suffix));
    }

    /// <summary>
    /// Composes the completion restore line from <see cref="CompletionViewModel.Restore"/>,
    /// rendering a phrase delimited by <c>[ ]</c> as a hyperlink into the
    /// README's "Is it safe?" section: a prefix Run, the Hyperlink, then a
    /// suffix Run. A value with no <c>[ ]</c> pair (the all-clear receipt, the
    /// permanent-delete reassurance) renders verbatim as a single Run. Mirrors
    /// <see cref="RegisteredFilesWindow"/>'s BuildSeeAlsoLine; the URL opens
    /// through <see cref="UrlLauncher"/> so this elevated process does not
    /// launch the browser as Administrator.
    /// </summary>
    private void BuildCompletionRestoreLine()
    {
        // SpaceHint (the recycle-delete-only "Empty it to actually reclaim
        // the space.") is folded in as a leading sentence rather than its own
        // TextBlock, so the whole thing wraps as one flowing paragraph. Two
        // separately-wrapped TextBlocks each break independently, which could
        // strand a short trailing phrase (French's "sera pas le cas !") alone
        // on its own line even when the pane had room to carry it up.
        var spaceHint = _vm.Completion.SpaceHint;
        var raw = spaceHint.Length > 0 ? $"{spaceHint} {_vm.Completion.Restore}" : _vm.Completion.Restore;
        // The post-Move hint names C:\Windows\Installer. Bound before the
        // split: KeepWhole inserts only between the path's own characters, so
        // the [ ] pair the split looks for is untouched.
        raw = InstallerPathText.KeepWhole(raw);
        CompletionRestoreText.Inlines.Clear();

        // Where the sentence splits around its [ ]-delimited link is pure string
        // work in Core (see CompositionParsing); this method only builds inlines.
        if (CompositionParsing.SplitAtBracketedPhrase(raw) is not { } split)
        {
            CompletionRestoreText.Inlines.Add(new Run(raw));
            return;
        }

        var link = new Hyperlink(new Run(split.LinkText))
        {
            NavigateUri = new Uri(SafetyUrl),
            Style = (Style)FindResource("SubtleLink"),
        };
        link.Click += Hyperlink_Click;
        // The bracketed phrase is a parenthetical aside, not a destination, so
        // it says nothing to a screen reader focused on the link alone; the
        // whole restore sentence (brackets removed) is the self-contained
        // accessible name, already in the user's language.
        AutomationProperties.SetName(link, split.Prefix + split.LinkText + split.Suffix);

        if (split.Prefix.Length > 0) CompletionRestoreText.Inlines.Add(new Run(split.Prefix));
        CompletionRestoreText.Inlines.Add(link);
        if (split.Suffix.Length > 0) CompletionRestoreText.Inlines.Add(new Run(split.Suffix));
    }

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Hyperlink link && link.NavigateUri is not null)
            UrlLauncher.OpenUrl(link.NavigateUri.AbsoluteUri);
    }

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

    // The bottom-bar globe opens this language menu. Endonyms are literal
    // (a language is shown in its own name, as browsers and Windows do);
    // the displayed language is ticked with a Segoe MDL2 check glyph drawn by
    // an explicit TextBlock, because the shared MenuItem template renders
    // only the Header (no icon column) and the app-wide implicit TextBlock
    // style would otherwise impose the bundled body font on the glyph.
    // Placement=Top opens the menu upward, clear of the taskbar under a
    // button at the bottom of the window. Rebuilt per click so the tick
    // always reflects the displayed language.
    private static readonly System.Windows.Media.FontFamily SegoeMdl2 = new("Segoe MDL2 Assets");

    // Listed alphabetically by each language's own name, the conventional picker
    // order (so the non-Latin scripts group after the Latin names). The README
    // language-switcher uses a different, priority order. Arabic is README-only
    // (no satellite resx), so it is not offered here.
    private static readonly (string Culture, string Endonym)[] LanguageChoices =
    {
        ("id", "Bahasa Indonesia"),
        ("de", "Deutsch"),
        ("en-GB", "English"),
        ("es", "Español"),
        ("fr", "Français"),
        ("it", "Italiano"),
        ("nl", "Nederlands"),
        ("pl", "Polski"),
        ("pt-BR", "Português (BR)"),
        ("vi", "Tiếng Việt"),
        ("tr", "Türkçe"),
        ("ru", "Русский"),
        ("uk", "Українська"),
        ("ja", "日本語"),
        ("zh-Hans", "简体中文"),
        ("ko", "한국어"),
    };

    // Tracks whether the globe's menu is open, so a second click on the globe
    // closes it instead of reopening it. A ContextMenu auto-dismisses on the
    // mouse-down of an outside click but raises Closed on a later dispatcher
    // pass, so at the globe's Click (the mouse-up of that same gesture) this
    // flag is still set and the click is read as the toggle-off; a deliberate
    // later click finds it already cleared, Closed having run long before the
    // next human click. (A close-time timestamp does the opposite and fails:
    // Closed has not fired yet at the Click, so the menu reopens in a flicker.)
    private bool _languageMenuOpen;

    private void LanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (_languageMenuOpen)
        {
            _languageMenuOpen = false;
            return;
        }

        var active = SupportedLanguages.Active(Localisation.UiCulture);
        var menu = new ContextMenu
        {
            PlacementTarget = button,
            Placement = PlacementMode.Top,
        };
        menu.Closed += (_, _) => _languageMenuOpen = false;

        MenuItem? activeItem = null;
        foreach (var (culture, endonym) in LanguageChoices)
        {
            var isActive = string.Equals(culture, active, StringComparison.OrdinalIgnoreCase);
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(new TextBlock
            {
                Text = isActive ? "\uE73E" : string.Empty,
                FontFamily = SegoeMdl2,
                FontSize = 12,
                Width = 20,
                VerticalAlignment = VerticalAlignment.Center,
            });
            header.Children.Add(new TextBlock
            {
                Text = endonym,
                VerticalAlignment = VerticalAlignment.Center,
            });
            var item = new MenuItem
            {
                Header = header,
                Command = _vm.Chrome.SetLanguageCommand,
                CommandParameter = culture,
                // Mark the active language as checked so a screen reader is told
                // which of the fifteen is current through the UIA Toggle pattern,
                // rather than only by the tick glyph in the header, which is a
                // Segoe MDL2 private-use codepoint with no accessible text. The
                // app's MenuItem template (Components.xaml) draws no check mark of
                // its own, so IsChecked reaches UIA without changing what the menu
                // looks like; the glyph stays the sighted cue.
                IsCheckable = true,
                IsChecked = isActive,
            };
            // A MenuItem takes its spoken name from a string Header, and the
            // header above is a StackPanel, which leaves the item unnamed: a
            // screen reader then has only the list position and the checked
            // state to read out, never the language. An explicit
            // AutomationProperties.Name is consulted ahead of any header
            // fallback, so the name is spoken whatever the header holds.
            //
            // The endonym alone is not enough. A speech engine renders only the
            // scripts it has a voice for, so an English-voiced Narrator reaching
            // Русский, Українська, 日本語, 简体中文 or 한국어 says nothing for the
            // name and falls back to "item 12" and the checked state, which is
            // indistinguishable from the unnamed bug above. Appending the
            // culture's display name fixes that without touching the visible
            // menu: it comes from ICU already translated into the running UI
            // language, so an English UI hears "Russian", a French one "russe"
            // and a Japanese one "ロシア語" - always a string the voice for that
            // UI can pronounce, and never a resx key to keep in step.
            var spokenName = new CultureInfo(culture).DisplayName;
            AutomationProperties.SetName(item,
                // Skip the suffix where it would repeat the endonym: exact for a
                // language listed in its own UI ("日本語"), prefix for one whose
                // display name only adds a region ("English (United Kingdom)").
                // Case-insensitively, because several languages lowercase their
                // own name where the endonym capitalises it, and "Français,
                // français" is the same word twice.
                spokenName.StartsWith(endonym, StringComparison.CurrentCultureIgnoreCase)
                    ? endonym
                    // A comma, not brackets: pt-BR's display name carries its own
                    // parenthetical, and nesting them reads badly aloud.
                    : $"{endonym}, {spokenName}");
            // Close on invoke regardless of what the command does. Picking the
            // displayed language is a no-op command (no relaunch), and a
            // keyboard Enter on that ticked item does not dismiss the menu on
            // its own the way a mouse click does; closing here makes both routes
            // consistent. For a real change the relaunch tears the menu down
            // anyway, so this is harmless there.
            item.Click += (_, _) => menu.IsOpen = false;
            if (isActive) activeItem = item;
            menu.Items.Add(item);
        }

        // A ContextMenu opened via IsOpen puts keyboard focus nowhere, so the
        // first Enter falls through and does nothing until an arrow key moves
        // focus onto an item. Focusing the displayed language (the ticked item)
        // once the menu is up makes the first Enter act on it immediately, and
        // mirrors a mouse open showing the current choice highlighted. Deferred
        // to a later dispatcher pass because the item is not yet focusable from
        // inside the Opened handler itself.
        menu.Opened += (_, _) =>
        {
            _languageMenuOpen = true;
            button.Dispatcher.BeginInvoke(DispatcherPriority.Input,
                new Action(() => activeItem?.Focus()));
        };
        menu.IsOpen = true;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(hwnd)?.AddHook(SuppressMaximize);
    }

    // The main window's centred-column layout caps its width (780
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
    /// the dim Border's MouseLeftButtonDown so only a click on the
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
