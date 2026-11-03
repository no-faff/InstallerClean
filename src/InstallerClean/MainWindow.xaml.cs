using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly LiveRegionRaises _liveRegions;

    /// <summary>
    /// Whether the pending-reboot line, and the line for files missing from disk, have
    /// appeared since they were last read out, and wait to be read at the next point the
    /// main window is in front: straight after a scan's headline where no card is up, as a
    /// card is closed, or as the Move or Delete card goes where no card follows it. See
    /// <see cref="OnScanPropertyChanged"/>, which sets them.
    /// </summary>
    private bool _pendingRebootToAnnounce;
    private bool _missingFromDiskToAnnounce;

    /// <summary>
    /// Whether the notice saying a newer version is available appeared while something
    /// stood in front of the main window and waits to be read, first, at the next point
    /// the window is in front, as the two warnings above do. Not set where the card a
    /// scan puts up carries the notice, since the card reads it out with its outcome.
    /// See <see cref="OnNewerVersionFound"/>, which sets it.
    /// </summary>
    private bool _updateNoticeToAnnounce;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _vm = viewModel;
        _liveRegions = new LiveRegionRaises(Dispatcher);
        // Each child VM raises its own PropertyChanged stream. Listen
        // on all three so the window can move keyboard focus to the
        // most-relevant Cancel button as overlays appear.
        _vm.Completion.PropertyChanged += OnCompletionPropertyChanged;
        _vm.Cleanup.PropertyChanged += OnCleanupPropertyChanged;
        _vm.Scan.PropertyChanged += OnScanPropertyChanged;
        _vm.Chrome.PropertyChanged += OnChromePropertyChanged;
        _vm.Chrome.NewerVersionFound += OnNewerVersionFound;
        _vm.Chrome.ScoopUpdateCopyAnswered += OnScoopUpdateCopyAnswered;
        _vm.Scan.ScanCompleted += OnScanCompleted;
        PreviewKeyDown += OnPreviewKeyDown;
        PreviewMouseDown += OnPreviewMouseDownOverReportPanel;
        PreviewGotKeyboardFocus += OnPreviewGotKeyboardFocusOverReportPanel;
        Closing += OnClosing;
        Closed += OnClosed;
        // The pair exists only for the update button's focus restore; see
        // _focusBeforeDeactivation.
        Deactivated += OnDeactivatedDuringUpdateCheck;
        Activated += OnActivatedDuringUpdateCheck;

        // The startup scan's two warnings appeared before this window existed, so they
        // are marked to be read here: on closing the card where the scan ended on one,
        // otherwise around the headline the replay below reads. A startup scan that was
        // cancelled or failed has no result, so neither shows.
        _pendingRebootToAnnounce = _vm.Scan.HasPendingReboot;
        _missingFromDiskToAnnounce = _vm.Scan.HasMissingFromDisk;
        // The automatic update check has answered by now. Where it found a newer
        // version the window opens with the card saying so, which says the same two
        // lines. Otherwise the notice at the top is read first, ahead of the replay
        // below, or as the startup card closes where one is up.
        _updateNoticeToAnnounce = _vm.Chrome.HasNewerVersion && !_vm.Chrome.IsOfferingUpdate;
        if (_vm.Chrome.IsOfferingUpdate)
            ShowUpdateOffer();
        else
            ShowStartupScan();

        // Both of the completion card's tip buttons are centred on the card,
        // and each tooltip is centred over its button.
        CompletionDonateToolTip.CustomPopupPlacementCallback = TooltipPlacement.KeptInsideWindow(
            CompletionDonateToolTip, this, ToolTipAnchor.Centre, ToolTipEdgeMargin);
        CompletionDonateFiveToolTip.CustomPopupPlacementCallback = TooltipPlacement.KeptInsideWindow(
            CompletionDonateFiveToolTip, this, ToolTipAnchor.Centre, ToolTipEdgeMargin);

        // The panel the report box's "i" opens. Its words are fixed for the life of
        // the window, a language change relaunching the app, so they are composed
        // once here, and its place is taken again whenever the card or the window
        // around it changes size.
        _reportPanelSpokenText = BuildReportPanel();
        CompletionOverlay.SizeChanged += OnReportPanelSurroundsSizeChanged;
        CompletionCard.SizeChanged += OnReportPanelSurroundsSizeChanged;

        // The completion card's widths at 100% text scale, and the Move or Delete
        // card's MinWidth; see each card in the XAML. Each card's overlay spans the
        // root grid with no margin, so the root grid's width is the card's room, and
        // unlike the overlays it is laid out before a card first shows.
        this.KeepCardInsideRoom(CompletionCard, RootLayout, minimumWidth: 340, maximumWidth: 520);
        this.KeepMinWidthInsideRoom(OperatingCard, RootLayout);

        // Width is explicit, the designed 828 (the content column's 780
        // MaxWidth plus the content margins) multiplied by the
        // text-scale factor; height sizes to content with the root
        // grid's work-area MaxHeight as its ceiling, so at large OS
        // text scales the bottom nav stays above the taskbar. Both are
        // first assigned here, before the window handle exists, the
        // shape every band-free window in the app uses, and no
        // constraint sits on the window itself. Sized to content in both
        // axes instead (SizeToContent="WidthAndHeight" with work-area
        // limits re-applied from OnSourceInitialized), the window opens
        // larger than its arranged content by the standard caption frame
        // under the custom WindowChrome, leaving unpainted black along the
        // bottom and right edges. With no handle yet this resolves against
        // the primary work area, where CenterScreen opens the window
        // anyway; a live text-scale change re-resolves against the actual
        // monitor.
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

    /// <summary>
    /// Shows the startup scan, which ran in the splash before this window existed: as
    /// the window opens, or after Continue without updating where the window opened with
    /// the card saying a newer version is available. The window's PropertyChanged
    /// subscriptions catch changes from their own start only, so what the scan put up
    /// is replayed here.
    ///
    /// Where the startup card is already up, the focus goes into it so Tab lands inside
    /// the overlay (and the overlay's KeyboardNavigation.TabNavigation="Cycle" keeps it
    /// there) rather than starting on a main-window button behind the overlay.
    /// </summary>
    private void ShowStartupScan()
    {
        if (_vm.Completion.IsComplete)
        {
            // The summary has no Text binding, hosting inlines composed in
            // code, so build it now for the overlay already up at
            // construction; the PropertyChanged path that normally builds it
            // never fired for this pre-construction completion (the startup
            // all-clear set during the splash).
            BuildCompletionSummaryLine();
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => CompletionDismissButton().Focus());
            // Reached only from the constructor, with the card up as the window opens:
            // after Continue without updating, the card comes up through
            // OnCompletionPropertyChanged instead. Narrator announces a window opening by
            // its name and its focused control, so the card's outcome lines go in the
            // window's name, in the order the card draws them. The box is not a line and
            // keeps its own raise.
            SpeakWindowAs(DisplayHelpers.SpokenSentences(
                _vm.Completion.Heading, _vm.Completion.FailedCount, _vm.Completion.Summary,
                _vm.Completion.Restore, _vm.Completion.Skipped, _vm.Completion.ScanStartLine));
            if (_vm.Completion.OffersReport)
                AnnounceLiveRegion(CompletionReportBox, () => _vm.Completion.OffersReport);
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
            // window existed, so the window's subscription never saw it;
            // replay the result announcement the same way the all-clear
            // branch replays its raises.
            OnScanCompleted(this, EventArgs.Empty);

            // Cancelled at the splash: there is no scan to announce, and the
            // scanning overlay this window normally re-announces the cancel from
            // was never up inside its lifetime. Say why the window is empty.
            if (!_vm.Scan.HasScanned && _vm.Scan.LastScanWasCancelled)
                Announce(Strings.Status_ScanCancelled);

            // A failed startup scan opens the window with the tailored error in the
            // intro instead of exiting. A newly shown window speaks only its title
            // and the focused Re-scan button, so announce the diagnosis too. Two
            // of the diagnoses name the installer cache folder and the announcer
            // takes them unbound: it is never drawn, so it has no break to protect
            // and a word joiner there would only ever reach a speech engine. The
            // same split TranslateExtension makes for an automation property.
            if (!_vm.Scan.HasScanned && _vm.Scan.HasScanError)
                Announce(_vm.Scan.LastScanError);
        }
    }

    /// <summary>
    /// The card saying a newer version is available, up as the window opens. Narrator
    /// announces a window opening by its name and its focused control, so the card's lines
    /// go in the window's name, Scoop's line and command included on a copy installed by
    /// Scoop, and the focus goes to the card's first step.
    /// </summary>
    private void ShowUpdateOffer()
    {
        SpeakWindowAs(DisplayHelpers.SpokenSentences(
            _vm.Chrome.UpdateNoticeText, _vm.Chrome.UpdateNoticeDetailText,
            _vm.Chrome.ShowsScoopCommand ? _vm.Chrome.ScoopUpdateSpoken : string.Empty));
        Dispatcher.BeginInvoke(DispatcherPriority.Input, FocusUpdateOffer);
    }

    /// <summary>
    /// Focuses the card's first step: Update now, or Copy beside Scoop's update command
    /// where the card has no Update now, or the link where it has neither, or Continue
    /// without updating where it has none of them.
    /// </summary>
    private void FocusUpdateOffer()
    {
        if (!UpdateOfferUpdateNowButton.Focus() && !UpdateOfferScoopBox.FocusCopy()
            && !UpdateOfferDownloadLink.Focus() && !UpdateOfferNetFrameworkLink.Focus())
            UpdateOfferContinueButton.Focus();
    }

    /// <summary>
    /// Names this window, for a screen reader, by <paramref name="name"/> in place of its
    /// title, while the card it opened with is up. The taskbar and the title bar keep the
    /// title. <see cref="SpeakWindowAsItsTitle"/> puts the title back as the card goes.
    /// </summary>
    private void SpeakWindowAs(string name) => AutomationProperties.SetName(this, name);

    private void SpeakWindowAsItsTitle() => ClearValue(AutomationProperties.NameProperty);

    /// <summary>
    /// Continue without updating: the scan the card stood in front of is shown. Where it
    /// offered nothing, its card goes up once the report box has been settled and is read
    /// as any card is (<see cref="OnCompletionPropertyChanged"/>); otherwise what the
    /// window shows is replayed as at the window's opening. The notice at the top is not
    /// read again, the card having just said the same. Never throws.
    /// </summary>
    private async Task ShowScanAfterUpdateOfferAsync()
    {
        try
        {
            SpeakWindowAsItsTitle();
            _updateNoticeToAnnounce = false;
            await _vm.ScanCardDecided;
            if (!_vm.Completion.IsComplete)
                ShowStartupScan();
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
        }
    }

    private void ApplyWorkAreaBounds()
    {
        // Width is assigned directly; SizeToContent is not toggled here. Toggling it
        // to Manual around the Width assignment opens a fresh launch at a large text
        // size narrow, the enlarged text wrapping cramped: the toggle runs in this
        // constructor-time call and disturbs the first content fit. The plain
        // assignment keeps the XAML SizeToContent="Height" and the explicit width
        // intact, which sizes correctly. A text size lowered with the app open is
        // fitted to at the next start.
        RootLayout.MaxHeight = DetailWindowSizing.WorkAreaHeightLimit(this);
        Width = DetailWindowSizing.ClampWidthToWorkArea(
            this, 828 * AccessibilitySettings.Current.TextScaleFactor, 0);

        // The small tip button's tooltip is one sentence and is given the window's width
        // rather than the accent tooltip's wrap width. The large one's is a
        // paragraph and keeps the wrap width.
        TooltipPlacement.WidenToWindow(CompletionDonateToolTip, Width, ToolTipEdgeMargin);
    }

    private void OnAccessibilitySettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(AccessibilitySettings.TextScaleFactor))
            ApplyWorkAreaBounds();
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        => DetailWindowSizing.NudgeIntoWorkArea(this);

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        // IsMoveOrDeleteRunning, which is true from the first line of the Move or
        // Delete command to its last, and not IsOperating, the overlay flag, or
        // IsOperationInFlight, the execution gate. The Move creates the
        // destination folder in its pre-flight, before the overlay is up and while
        // the confirmation is open, and removes it on every exit that stops short
        // of the batch; a close taken before that removal has finished ends the
        // process with the folder left at the path the user typed. The hold is
        // released by OnCleanupPropertyChanged watching this same property go
        // false, so a hold taken on another flag would never be released: nothing
        // raises a change notification the close is waiting on.
        if (!_vm.Cleanup.IsMoveOrDeleteRunning) return;

        // A Move or a Delete is running. Letting this close through kills the
        // worker where it stands: OnExit disposes the container, which cancels
        // the token but waits for nothing, and the batch is on a background
        // thread, so the process exits from under it. A cross-volume move is a
        // copy performed in this process (File.Move passes
        // MOVEFILE_COPY_ALLOWED, and Win32 documents that flag as a CopyFile
        // plus a DeleteFile), so dying mid-file leaves a truncated .msi in the
        // destination folder with the source still in place: a file that looks
        // like a recovery copy and is not one.
        //
        // So: hold the close, cancel the operation, and take the close when the
        // command lets go. Cancelling stops the batch at the next file boundary,
        // so the file being moved right now is finished rather than truncated,
        // and stops the Move before its batch at its next check, removing the
        // folder its pre-flight made. The wait is visible: the operating overlay
        // stays up and says so, and a confirmation already open stays up until it
        // is answered. The view model records the close and finishes the
        // operation without the rescan after the batch, cancelling that rescan
        // where it is already running (CleanupViewModel.RequestClose).
        e.Cancel = true;
        _vm.Cleanup.RequestClose();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _vm.Completion.PropertyChanged -= OnCompletionPropertyChanged;
        _vm.Cleanup.PropertyChanged -= OnCleanupPropertyChanged;
        _vm.Scan.PropertyChanged -= OnScanPropertyChanged;
        _vm.Chrome.PropertyChanged -= OnChromePropertyChanged;
        _vm.Chrome.NewerVersionFound -= OnNewerVersionFound;
        _vm.Chrome.ScoopUpdateCopyAnswered -= OnScoopUpdateCopyAnswered;
        _vm.Scan.ScanCompleted -= OnScanCompleted;
        PreviewKeyDown -= OnPreviewKeyDown;
        PreviewMouseDown -= OnPreviewMouseDownOverReportPanel;
        PreviewGotKeyboardFocus -= OnPreviewGotKeyboardFocusOverReportPanel;
        CompletionOverlay.SizeChanged -= OnReportPanelSurroundsSizeChanged;
        CompletionCard.SizeChanged -= OnReportPanelSurroundsSizeChanged;
        Deactivated -= OnDeactivatedDuringUpdateCheck;
        Activated -= OnActivatedDuringUpdateCheck;
        AccessibilitySettings.Current.PropertyChanged -= OnAccessibilitySettingsChanged;
        SizeChanged -= OnWindowSizeChanged;
        Closing -= OnClosing;
        Closed -= OnClosed;
    }

    /// <summary>
    /// Reads out what a scan the user can see has put on the main window, in the
    /// order it is drawn: the notice saying a newer version is available and the
    /// pending-reboot line, each where it has just appeared, the headline ("12
    /// unneeded files to clean up (3.2 GB)"), the line naming any drive or share the
    /// scan carried on without, and the line for files missing from disk where it has
    /// just appeared. A scan that offered nothing announces through the completion
    /// card instead, and the silent post-operation refresh must stay silent because
    /// the completion outcome is about to speak. In both the two warnings stay marked,
    /// and are read as the card is closed, or as the Move or Delete card goes where no
    /// card follows it. So does the notice.
    ///
    /// The headline, and the line naming the drives and shares, are read after every
    /// scan that shows them rather than only when they change, so a Re-scan that
    /// finds the same files, or meets the same drive again, says so. The two
    /// warnings are read when they appear and not again while they stay as they are.
    /// <see cref="ShowStartupScan"/> replays this method for the startup scan, so that
    /// scan's lines are read too.
    /// </summary>
    private void OnScanCompleted(object? sender, EventArgs e)
    {
        if (_vm.Cleanup.IsOperating || _vm.Completion.IsComplete)
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

        // A completed scan that offered nothing puts its card up after this runs, and
        // the notice is read as that card closes, with the two warnings.
        if (!(_vm.Scan.HasScanned && _vm.Scan.OrphanedFileCount == 0)
            && TakeMark(ref _updateNoticeToAnnounce))
            AnnounceUpdateNotice();
        if (TakeMark(ref _pendingRebootToAnnounce))
            AnnounceLiveRegion(PendingRebootBannerText, () => _vm.Scan.HasPendingReboot);
        if (_vm.Scan.OrphanedFileCount == 0)
        {
            ScanResultAnnouncer.Text = string.Empty;
        }
        else
        {
            Announce(string.Format(Strings.Automation_ScanResultAnnouncement,
                _vm.Scan.OrphanedSummaryText, _vm.Scan.OrphanedSizeDisplay));
            if (_vm.Scan.HasScanSignal)
                AnnounceLiveRegions(ScanSignalText);
            if (_vm.Scan.HasSourcesGivenUp)
                AnnounceLiveRegions(SourcesGivenUpText);
        }
        if (TakeMark(ref _missingFromDiskToAnnounce))
            AnnounceLiveRegion(MissingFromDiskBannerText, () => _vm.Scan.HasMissingFromDisk);
    }

    /// <summary>
    /// Reads out the notice saying a newer version is available and the two warnings
    /// still marked from the last scan, in the order they are drawn, at a point where
    /// the main window is in front again with no headline to put them around: a card
    /// closing, or the Move or Delete card going with no card after it. A line gone by
    /// the time its raise runs is not read.
    /// </summary>
    private void AnnounceMarkedWarnings()
    {
        if (TakeMark(ref _updateNoticeToAnnounce))
            AnnounceUpdateNotice();
        if (TakeMark(ref _pendingRebootToAnnounce))
            AnnounceLiveRegion(PendingRebootBannerText, () => _vm.Scan.HasPendingReboot);
        if (TakeMark(ref _missingFromDiskToAnnounce))
            AnnounceLiveRegion(MissingFromDiskBannerText, () => _vm.Scan.HasMissingFromDisk);
    }

    /// <summary>Returns <paramref name="mark"/> and clears it, so a warning is read once.</summary>
    private static bool TakeMark(ref bool mark)
    {
        var taken = mark;
        mark = false;
        return taken;
    }

    /// <summary>
    /// Speaks why the window has no list, where the scan after a Move or Delete
    /// did not finish. That scan ends behind the operating overlay, so nothing is
    /// announced as the window changes, and once the overlay or the summary card
    /// has gone, focus lands on Re-scan, which a screen reader announces by name
    /// alone. Read after that focus move, for the reason <see cref="LiveRegionRaises"/>
    /// gives. The message goes in as the view model holds it: the announcer is never
    /// drawn, so it takes no break characters.
    /// </summary>
    private void AnnounceUnfinishedRefresh()
    {
        if (!_vm.Scan.HasUnfinishedRefresh) return;
        Announce(_vm.Scan.UnfinishedRefreshMessage);
    }

    /// <summary>
    /// Puts <paramref name="line"/> on the invisible announcer and reads it out, in one
    /// callback at Background priority, after any focus move queued with it (see
    /// <see cref="LiveRegionRaises"/>). It is read whether or not the announcer already
    /// held the same words.
    /// </summary>
    private void Announce(string line) =>
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            ScanResultAnnouncer.Text = line;
            LiveRegionRaises.Raise(ScanResultAnnouncer);
        });

    private void OnCompletionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Summary is set on every Show* path before IsComplete flips, so
        // rebuilding its inlines here means they are in place by the time the
        // IsComplete branch below announces the outcome.
        if (e.PropertyName == nameof(CompletionViewModel.Summary))
            BuildCompletionSummaryLine();

        if (e.PropertyName == nameof(CompletionViewModel.IsComplete) && _vm.Completion.IsComplete)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => CompletionDismissButton().Focus());
            // Focus lands on the button that closes the card, so without an
            // explicit raise a screen reader announces only the button and never
            // the outcome.
            AnnounceCompletionOutcome();
        }

        // Overlay dismissed (Done, Close without tipping, the large tip button, Esc or a
        // click on the dim margin). The focused button is gone, so move focus to a
        // sensible non-destructive control rather than letting it drop to the window
        // root.
        if (e.PropertyName == nameof(CompletionViewModel.IsComplete) && !_vm.Completion.IsComplete)
        {
            SpeakWindowAsItsTitle();
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => RescanButton.Focus());
            AnnounceUnfinishedRefresh();
            // A warning the card's scan put up is read now, after the card. Where the
            // Move or Delete card is closed before the operation has let go, the
            // operating overlay is still up, and the warnings wait for it to go
            // (OnCleanupPropertyChanged).
            if (!_vm.Cleanup.IsOperating)
                AnnounceMarkedWarnings();
        }

        // The box leaves a card still up where its report could not be written, and
        // the "i" and its panel go with it. Focus on any of them would drop to the
        // window root, so it moves to the button that closes the card, which keeps the
        // user inside the card. WPF moves focus off a hidden element on a later pass,
        // so it is still where it was when this runs.
        if (e.PropertyName == nameof(CompletionViewModel.OffersReport)
            && !_vm.Completion.OffersReport && _vm.Completion.IsComplete
            && (CompletionReportLine.IsKeyboardFocusWithin || ReportPanel.IsKeyboardFocusWithin))
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                if (_vm.Completion.IsComplete)
                    CompletionDismissButton().Focus();
            });
        }

        if (e.PropertyName == nameof(CompletionViewModel.ReportPanelOpen))
        {
            if (_vm.Completion.ReportPanelOpen)
            {
                PlaceReportPanel();
                WobbleReportPanel();
                // Focus stays on the "i", so the panel's words are read out rather
                // than reached.
                Announce(_reportPanelSpokenText);
            }
            else if (_vm.Completion.OffersReport && ReportPanel.IsKeyboardFocusWithin)
            {
                // Closed with focus inside it, by Esc on its link: focus goes back to
                // the "i" that opened it rather than dropping to the window root.
                Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
                {
                    if (_vm.Completion.OffersReport)
                        ReportInfoButton.Focus();
                });
            }
        }
    }

    /// <summary>
    /// Reads out the update status line each time its text changes while it shows,
    /// because each of its texts answers a click the user is waiting on:
    /// "Checking..." as the manual check starts, then "Up to date.". Its clearing,
    /// after the cooldown or as a check finds a newer version, collapses it and reads
    /// nothing.
    /// </summary>
    private void OnChromePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChromeViewModel.UpdateStatusText)
            && _vm.Chrome.UpdateStatusText.Length > 0)
            AnnounceLiveRegions(UpdateStatusLineText);

        if (e.PropertyName == nameof(ChromeViewModel.IsUpdating))
            OnUpdatingChanged();

        // The card's line changes from the download to its check while the card is up, and
        // a live region's text changing raises nothing by itself, so the new line is read.
        if (e.PropertyName == nameof(ChromeViewModel.UpdatingText) && _vm.Chrome.IsUpdating)
            AnnounceLiveRegion(UpdatingText, () => _vm.Chrome.IsUpdating);

        if (e.PropertyName == nameof(ChromeViewModel.IsOfferingUpdate) && !_vm.Chrome.IsOfferingUpdate)
            _ = ShowScanAfterUpdateOfferAsync();
    }

    /// <summary>
    /// The card shown while an update downloads. As it appears, the focus moves to its
    /// Cancel and its line is read. As it goes, which it does only where the update was
    /// cancelled, failed or was ruled out by the newer version's release, the focus moves
    /// to Update now, or to the link where Update now has gone, and the line saying what
    /// failed or why the release rules this copy out is read: on the card saying a newer
    /// version is available where that card is behind it, with nothing else read until
    /// that card goes; otherwise on the notice, followed by any warning marked while the
    /// card was up.
    /// </summary>
    private void OnUpdatingChanged()
    {
        if (_vm.Chrome.IsUpdating)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => UpdateCancelButton.Focus());
            AnnounceLiveRegion(UpdatingText, () => _vm.Chrome.IsUpdating);
            return;
        }

        if (_vm.Chrome.IsOfferingUpdate)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, FocusUpdateOffer);
            if (_vm.Chrome.HasUpdateFailure)
                AnnounceLiveRegion(UpdateOfferFailure, () => _vm.Chrome.HasUpdateFailure);
            else if (_vm.Chrome.IsUpdateRefused)
                AnnounceLiveRegion(UpdateOfferDetail, () => _vm.Chrome.IsUpdateRefused);
            return;
        }

        Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (!UpdateNowButton.Focus() && !UpdateNoticeDownloadLink.Focus()
                && !UpdateNoticeNetFrameworkLink.Focus())
                RescanButton.Focus();
        });
        if (_vm.Chrome.HasUpdateFailure)
            AnnounceLiveRegion(UpdateNoticeFailure, () => _vm.Chrome.HasUpdateFailure);
        else if (_vm.Chrome.IsUpdateRefused)
            AnnounceLiveRegion(UpdateNoticeDetail, () => _vm.Chrome.IsUpdateRefused);
        if (!_vm.Cleanup.IsOperating && !_vm.Completion.IsComplete)
            AnnounceMarkedWarnings();
    }

    /// <summary>
    /// Reads out the notice each time a check finds a newer version, which the
    /// automatic check does unprompted and the manual check as the answer to a click,
    /// the notice already showing included. Read at once where nothing stands in front
    /// of it; otherwise marked, and read first at the next point the window is in front.
    /// </summary>
    private void OnNewerVersionFound(object? sender, EventArgs e)
    {
        if (_vm.Completion.IsComplete || _vm.Cleanup.IsOperating || _vm.Scan.IsScanning
            || _vm.Chrome.IsUpdating)
            _updateNoticeToAnnounce = true;
        else
            AnnounceUpdateNotice();
    }

    /// <summary>
    /// Reads out the notice's lines at the top of the main window: its two lines, then on
    /// a copy installed by Scoop the line saying how to update it, whose spoken name
    /// carries the command.
    /// </summary>
    private void AnnounceUpdateNotice()
    {
        AnnounceLiveRegion(UpdateNoticeLead, () => _vm.Chrome.HasNewerVersion);
        AnnounceLiveRegion(UpdateNoticeDetail, () => _vm.Chrome.HasNewerVersion);
        if (_vm.Chrome.ShowsScoopCommand)
            AnnounceLiveRegion(UpdateNoticeScoop, () => _vm.Chrome.ShowsScoopCommand);
    }

    /// <summary>
    /// Answers each press of Copy beside Scoop's update command: "Copied" through the
    /// invisible announcer, which reads a repeat of the same word too, or the line saying
    /// the copy failed, on the card where the card is up and on the notice otherwise. The
    /// button's spoken name stays the same throughout, so its text turning back to Copy
    /// reads nothing.
    /// </summary>
    private void OnScoopUpdateCopyAnswered(object? sender, bool copied)
    {
        if (copied)
            Announce(Strings.UpdateNotice_CommandCopied);
        else if (_vm.Chrome.IsOfferingUpdate)
            AnnounceLiveRegion(UpdateOfferScoopBox.CopyFailureLine, () => _vm.Chrome.HasScoopUpdateCopyFailed);
        else
            AnnounceLiveRegion(UpdateNoticeScoopBox.CopyFailureLine, () => _vm.Chrome.HasScoopUpdateCopyFailed);
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
    /// that lands focus on a control records it: the dismissal of the warning
    /// a failed check shows, an Alt+Tab round trip, whatever put activation
    /// back. That breadth is the point rather than a leak, because in that
    /// span the choice is always WPF's and never the user's, and undoing WPF's
    /// choice is what the restore exists for. The comparison earns its keep at
    /// the other end: focus that comes back to where it already was is not a
    /// choice at all, so it leaves the record alone, and focus the user moved
    /// themselves matches nothing and survives.
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
    /// null or the window itself, and still sitting exactly where the
    /// dismissal of the warning a failed check shows dropped it. A check that
    /// ends "Up to date." or finds a newer version shows no dialog and only
    /// ever takes the first; a check that fails ends in the warning and takes
    /// the second, several seconds after it is dismissed, because the cooldown
    /// outlasts it. Focus the user has since
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
        if (e.PropertyName == nameof(CleanupViewModel.CanStopWaiting) && !_vm.Cleanup.CanStopWaiting)
            HandFocusToCancel(OperationWaitingRegion, OperationCancelButton);

        if (e.PropertyName == nameof(CleanupViewModel.IsMoveOrDeleteRunning)
            && !_vm.Cleanup.IsMoveOrDeleteRunning && _vm.Cleanup.CloseRequested)
        {
            // The close the user asked for during the operation. Deferred to a
            // later dispatcher pass rather than closed from here: this fires
            // inside the command's finally, and tearing the window (and with
            // it the DI container, and with it these view-models) down while
            // that frame is still on the stack is a re-entrancy nobody should
            // have to reason about.
            Dispatcher.BeginInvoke(DispatcherPriority.Background, Close);
            return;
        }

        // Nothing is focused or announced on a window that is closing.
        if (e.PropertyName == nameof(CleanupViewModel.IsOperating) && !_vm.Cleanup.IsOperating
            && !_vm.Cleanup.CloseRequested)
        {
            // The overlay's focused Cancel button collapses with the
            // overlay, and when no completion overlay follows (a
            // cancelled operation, a failure dialog, the Move pre-flight
            // handing over to its confirmation) keyboard focus would drop
            // to the window root: no ring, Tab restarting from the first
            // stop, a screen reader gone quiet. Normal priority, not
            // Input: PropertyChanged fires inside the operation's finally,
            // before the awaiting caller's continuation is posted
            // (DispatcherSynchronizationContext posts at Normal), so
            // same-priority FIFO runs this callback first and a follow-up
            // modal opens with a live focus target in the owner window to
            // restore to on close.
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                if (!_vm.Completion.IsComplete && !_vm.Scan.IsScanning && !_vm.Cleanup.IsOperating)
                {
                    FocusResultsDefault();
                    // A batch that ends with no card can still leave the window
                    // with no list: a cancel before the first file, or a failure
                    // reported in a dialog, followed by a scan that did not finish.
                    AnnounceUnfinishedRefresh();
                    // And a warning the scan after the batch put up, or the
                    // pending-reboot re-check after Windows Installer's lock was
                    // refused, is read here, with no card to wait for.
                    AnnounceMarkedWarnings();
                }
            });
        }

        if (e.PropertyName == nameof(CleanupViewModel.IsOperating) && _vm.Cleanup.IsOperating)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => OperationCancelButton.Focus());
            // Read as the card appears, after the focus move to Cancel, so an
            // operation is first named by its heading and not by a bare file count.
            AnnounceOperationHeading();
        }

        // Every later heading is read as it is written: a wait on a drive or share,
        // the heading put back when the wait ends, "Cancelling..." and the scan after
        // the batch. A blank heading is not read: the heading is blank as the card
        // goes, behind a dialog the operation ends in, and while a finished batch
        // waits to put up its card.
        if (e.PropertyName == nameof(CleanupViewModel.OperationProgress)
            && _vm.Cleanup.OperationProgress.Length > 0 && OperatingCardInFront())
            AnnounceOperationHeading();

        // The count of files done, first file, each tenth of the batch and last file
        // (CleanupViewModel.OperationProgressAnnouncement), read as it is written.
        if (e.PropertyName == nameof(CleanupViewModel.OperationProgressAnnouncement)
            && _vm.Cleanup.OperationProgressAnnouncement.Length > 0 && OperatingCardInFront())
            AnnounceLiveRegion(OperationProgressAnnouncementText, () =>
                OperatingCardInFront() && _vm.Cleanup.ShowOperationProgressDetail
                && _vm.Cleanup.OperationProgressAnnouncement.Length > 0);
    }

    /// <summary>
    /// Whether the Move or Delete card is up with no finished card over it, which is
    /// when its heading and count are read.
    /// </summary>
    private bool OperatingCardInFront() => _vm.Cleanup.IsOperating && !_vm.Completion.IsComplete;

    private void AnnounceOperationHeading() =>
        AnnounceLiveRegion(OperationHeadingText, () =>
            OperatingCardInFront() && _vm.Cleanup.OperationProgress.Length > 0);

    /// <summary>
    /// Moves the keyboard focus to a card's Cancel as the card's wait ends, where the focus is
    /// in the card's waiting region: on the stop-waiting button, or on the region itself,
    /// which takes the focus while it can scroll. As the wait ends the region goes, and WPF
    /// moves focus held by either up to the window. Here and not posted: WPF answers a
    /// focused element going hidden with a focus check posted at Input priority, which
    /// finds Cancel focused and leaves it, where a move posted behind it at Input would run
    /// once the focus had already fallen to the window. A Cancel out of use, pressed already or
    /// with nothing left to stop, takes no focus, so the focus goes where WPF puts it, as it
    /// does when Cancel goes out of use under the focus.
    /// </summary>
    private static void HandFocusToCancel(ScrollViewer waitingRegion, Button cancel)
    {
        if (waitingRegion.IsKeyboardFocusWithin && cancel.IsEnabled)
            cancel.Focus();
    }

    private void OnScanPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When the scan view model drops its result, the list goes from the
        // window and the headline naming it goes with it. The announcer is
        // always rendered and keeps its last text, so browse mode could
        // otherwise land on a count the window no longer shows. Ahead of the
        // guard below, because the scan after a Move or Delete ends behind the
        // operating overlay.
        if (e.PropertyName == nameof(ScanViewModel.HasScanned) && !_vm.Scan.HasScanned)
            ScanResultAnnouncer.Text = string.Empty;

        if (e.PropertyName == nameof(ScanViewModel.IsScanning) && _vm.Scan.IsScanning)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => ScanCancelButton.Focus());
            // Read as the card appears, after the focus move to Cancel.
            AnnounceScanProgress();
        }

        // Every later line is read as it is written while the scan runs: each step,
        // a wait on a drive or share, the step put back when the wait ends, and
        // "Cancelling...". The line written once the scan has ended goes with the card
        // and is not read; a cancelled scan says so on the announcer below.
        if (e.PropertyName == nameof(ScanViewModel.ScanProgress)
            && _vm.Scan.IsScanning && _vm.Scan.IsScanInFlight)
            AnnounceScanProgress();

        if (e.PropertyName == nameof(ScanViewModel.CanStopWaiting) && !_vm.Scan.CanStopWaiting)
            HandFocusToCancel(ScanWaitingRegion, ScanCancelButton);

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

            // The cancelled-scan outcome ("Scan cancelled.") is written to the
            // card's line as the card goes, so it is read on the persistent
            // announcer instead, after the focus move above. Only on a user
            // cancel, and not when an all-clear overlay will speak its own
            // outcome.
            if (_vm.Scan.LastScanWasCancelled && !_vm.Completion.IsComplete)
                Announce(Strings.Status_ScanCancelled);
        }

        // THE PENDING-REBOOT WARNING AND THE WARNING FOR FILES MISSING FROM DISK ARE
        // MARKED HERE AND READ WHERE THEY FALL IN WITH WHAT ELSE IS SPOKEN. A scan sets
        // both before its headline exists and, on a Re-scan ending on a card, before the
        // card is up, so each is marked as it appears and read in drawn order around the
        // headline (OnScanCompleted), or after the card, or as the Move or Delete card
        // goes with no card after it (AnnounceMarkedWarnings). A warning that goes before
        // then is unmarked. Each is marked on a change of value only, so a Re-scan that
        // finds a warning as it was reads nothing more, and one whose count of missing
        // files changes reads the line again.
        //
        // The pending-reboot re-check made at a Move or Delete click runs with no card
        // up and no headline to follow, so the line it puts up is read at once, where it
        // still shows when the raise runs.
        if (e.PropertyName == nameof(ScanViewModel.HasMissingFromDisk))
            _missingFromDiskToAnnounce = _vm.Scan.HasMissingFromDisk;
        if (e.PropertyName == nameof(ScanViewModel.HasPendingReboot))
        {
            if (!_vm.Scan.HasPendingReboot)
                _pendingRebootToAnnounce = false;
            else if (_vm.Scan.IsScanInFlight || _vm.Cleanup.IsOperating || _vm.Completion.IsComplete)
                _pendingRebootToAnnounce = true;
            else
                AnnounceLiveRegion(PendingRebootBannerText, () => _vm.Scan.HasPendingReboot);
        }
    }

    /// <summary>
    /// Reads the scanning card's line, unless the card has gone by the time the raise
    /// runs.
    /// </summary>
    private void AnnounceScanProgress() =>
        AnnounceLiveRegion(ScanProgressText, () => _vm.Scan.IsScanning);

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

    // How close a tooltip may come to either edge of this window.
    private const double ToolTipEdgeMargin = 12;


    /// <summary>
    /// Reads out <paramref name="elements"/> in the order given, each after any focus
    /// move queued with it (see <see cref="LiveRegionRaises"/> for the priority). The
    /// plain update status line is the one assertive caller, so it cuts in rather than
    /// waiting its turn; why that is acceptable there is on the element itself, in
    /// MainWindow.xaml.
    /// </summary>
    private void AnnounceLiveRegions(params FrameworkElement[] elements)
    {
        foreach (var element in elements)
            _liveRegions.Queue(element);
    }

    /// <summary>
    /// Reads out <paramref name="element"/>, unless <paramref name="stillShown"/> answers
    /// false when the raise runs.
    /// </summary>
    private void AnnounceLiveRegion(FrameworkElement element, Func<bool> stillShown) =>
        _liveRegions.Queue(element, stillShown);

    /// <summary>
    /// The completion card's button that closes it: Close without tipping on the
    /// card carrying the large tip button, Done on every other. Read when focus is placed, so
    /// <see cref="CompletionViewModel.AsksForDonation"/> has to be settled before
    /// <see cref="CompletionViewModel.IsComplete"/> reveals the card, as every Show*
    /// method settles it.
    /// </summary>
    private Button CompletionDismissButton() =>
        _vm.Completion.AsksForDonation ? CompletionCloseWithoutDonatingButton : CompletionCloseButton;

    // Every zone of the card that states an outcome, in visual order. Each is
    // assigned before IsComplete reveals the overlay and is not touched while it
    // is up, so this one raise is the only time it is read. The failure count and
    // the kept-back count are empty on a clean run, which collapses the element,
    // and so are the summary, the restore line, the skipped line and the line about
    // the moment the scan began on the cards that carry none.
    //
    // The per-file error list stays unraised, and that is a separate decision:
    // it is a list to read at leisure rather than an outcome, and it is reached
    // in scan mode. The count above it is what has to be spoken.
    //
    // The report box follows the outcome on the card that carries it, which is
    // settled before the card is revealed, so the reader hears that something is
    // sent as the card closes, and whether the box is ticked, without having to
    // Tab past the buttons to find out.
    private void AnnounceCompletionOutcome()
    {
        AnnounceLiveRegions(CompletionHeadingText, CompletionFailedCountText,
            CompletionSummaryText, CompletionRestoreText, CompletionSkippedText,
            CompletionScanStartText);
        if (_vm.Completion.OffersReport)
            AnnounceLiveRegion(CompletionReportBox, () => _vm.Completion.OffersReport);
    }

    /// <summary>
    /// Composes the completion summary line from <see cref="CompletionViewModel.Summary"/>,
    /// forcing the destination path (<see cref="CompletionViewModel.SummaryDestination"/>)
    /// onto its own line at the exact point it substitutes into the formatted
    /// sentence, whatever word order the target language uses (mirrors
    /// ConfirmMoveWindow's destination-on-its-own-line treatment). A value with
    /// no destination (the all-clear's line, the nothing-offered card's lines, either
    /// delete summary) keeps its words as they are, taking only the breaks
    /// <see cref="AddTextWithLineBreaks"/> gives it. Locating the raw substring
    /// rather than a bracket-delimited marker, which is how the windows that carry a
    /// link mark one, is deliberate: the destination is a user-chosen folder path
    /// that could itself contain a literal '[' or ']'.
    ///
    /// The summary can be more than one line. The overlay shown when the act-time
    /// re-check held the WHOLE batch back puts the held-back sentence here, with the
    /// line naming any drive or share that check carried on without under it; the
    /// nothing-offered card puts each of its lines saying what to do here, one to a line;
    /// and
    /// the destination line below is forced onto its own. Every Run therefore goes
    /// through <see cref="AddTextWithLineBreaks"/> rather than straight into
    /// Inlines, so the breaks are ones this method made rather than ones a text
    /// formatter is trusted to find.
    ///
    /// A value with no newline yields exactly one Run and no break.
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

        // THE SPOKEN NAME IS THE SUMMARY AS THE VIEW MODEL HOLDS IT, set here so every
        // build sets it, the startup card's replay included. Every Run below is cut from
        // that same text and the method adds only breaks, so the name carries every word
        // the card draws and none of the characters that exist only for line breaking.
        AutomationProperties.SetName(CompletionSummaryText, _vm.Completion.Summary);

        // The split point (CompositionParsing.SplitAtSubstring) and the
        // destination's break opportunities
        // (InstallerPathText.AllowFolderBreaksInAnyPath) are pure string work
        // with tests of their own; this method only turns the result into WPF
        // inlines.
        if (CompositionParsing.SplitAtSubstring(raw, destination) is not { } split)
        {
            AddTextWithLineBreaks(raw);
            return;
        }

        var wrappedDestination = InstallerPathText.AllowFolderBreaksInAnyPath(destination);

        if (split.Prefix.Length > 0) AddTextWithLineBreaks(split.Prefix);
        CompletionSummaryText.Inlines.Add(new LineBreak());
        CompletionSummaryText.Inlines.Add(new Run(wrappedDestination));
        if (split.Suffix.Length > 0) AddTextWithLineBreaks(split.Suffix);
    }

    /// <summary>
    /// Appends <paramref name="text"/> to the summary's inlines, turning each
    /// newline in it into an explicit <see cref="LineBreak"/>, and giving a path in
    /// each line, such as a share the scan carried on without or the folder a
    /// cancelled Move names mid-sentence, a break opportunity at each of its folders
    /// (<see cref="InstallerPathText.AllowFolderBreaksInAnyPath"/>). The installer
    /// folder, already held whole by the caller, keeps that treatment.
    ///
    /// A TextBlock breaks a line on a newline inside its Text property, but this
    /// TextBlock has no Text binding at all: it is composed from Runs so the
    /// destination path can be forced onto its own line at a position that varies
    /// by language. Emitting the breaks rather than leaving them inside a Run's
    /// text keeps the rendering off an assumption about how the text formatter
    /// treats a control character it was handed.
    ///
    /// Splits on '\n' and trims a trailing '\r' rather than splitting on
    /// Environment.NewLine, so a value joined either way renders the same.
    /// </summary>
    private void AddTextWithLineBreaks(string text)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) CompletionSummaryText.Inlines.Add(new LineBreak());
            CompletionSummaryText.Inlines.Add(
                new Run(InstallerPathText.AllowFolderBreaksInAnyPath(lines[i].TrimEnd('\r'))));
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        // MainViewModel.HandleEscape picks the overlay that takes it. Esc on an idle
        // top-level window must not close the app, so nothing here takes it either.
        if (_vm.HandleEscape())
            e.Handled = true;
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

    // Hand-written endonyms, because the framework's own NativeName is not what
    // a picker wants: it lower-cases where the language does not ("français"),
    // and it renders pt-BR as "português (Brasil)" where every other browser
    // and OS picker shortens the region. Arabic is absent on purpose, being
    // README-only with no satellite resx.
    //
    // This map does NOT decide which languages the menu offers. That comes from
    // SupportedLanguages.CultureNames, the same list the app validates a saved
    // language preference against, so the menu cannot fall out of step with
    // what ships: a language added there appears in the menu, under the
    // framework's NativeName while it has no entry here. CI fails until it has
    // one, because scripts/check-font-coverage.mjs checks each name against
    // the family its language is drawn in.
    private static readonly Dictionary<string, string> Endonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "Bahasa Indonesia",
        ["de"] = "Deutsch",
        ["en-GB"] = "English",
        ["es"] = "Español",
        ["fr"] = "Français",
        ["it"] = "Italiano",
        ["nl"] = "Nederlands",
        ["pl"] = "Polski",
        ["pt-BR"] = "Português (BR)",
        ["vi"] = "Tiếng Việt",
        ["tr"] = "Türkçe",
        ["ru"] = "Русский",
        ["uk"] = "Українська",
        ["ja"] = "日本語",
        ["zh-Hans"] = "简体中文",
        ["zh-Hant"] = "繁體中文",
        ["ko"] = "한국어",
    };

    /// <summary>
    /// Every shipped language paired with the name to show it under, ordered
    /// alphabetically by that name: the conventional picker order, which groups
    /// the non-Latin scripts after the Latin ones. The README's language
    /// switcher uses a different, priority order.
    ///
    /// Sorted with the ordinal comparer so the order does not change with the
    /// displayed language, a picker that reshuffles itself being harder to use
    /// than one that does not.
    /// </summary>
    private static IEnumerable<(string Culture, string Endonym)> LanguageChoices =>
        SupportedLanguages.CultureNames
            .Select(c => (Culture: c, Endonym: Endonyms.TryGetValue(c, out var name)
                ? name
                : CultureInfo.GetCultureInfo(c).NativeName))
            .OrderBy(x => x.Endonym, StringComparer.Ordinal);

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
            // Each name in the family its own language is drawn in, so Русский
            // is in Montserrat and 日本語 in Yu Gothic UI whatever the screen's
            // language. The East Asian families keep the theme family's line
            // spacing, so every row of the menu is the same height.
            header.Children.Add(new TextBlock
            {
                Text = endonym,
                FontFamily = LanguageFonts.For(culture),
                VerticalAlignment = VerticalAlignment.Center,
            });
            var item = new MenuItem
            {
                Header = header,
                Command = _vm.Chrome.SetLanguageCommand,
                CommandParameter = culture,
                // Mark the active language as checked so a screen reader is told
                // which language is current through the UIA Toggle pattern,
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
                    // A separator, not brackets: pt-BR's display name carries its
                    // own parenthetical, and nesting them reads badly aloud. It comes
                    // from the resx because the mark between two items is a
                    // per-language question and this name is read aloud.
                    : $"{endonym}{Strings.Display_ListSeparator}{spokenName}");
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

    // The panel's distance from the window's edges, and from the top of the report
    // line it sits above.
    private const double ReportPanelEdgeMargin = 12;
    private const double ReportPanelGap = 12;

    /// <summary>
    /// The panel's words as one line for the screen reader
    /// (<see cref="ReportPanelText.SpokenLine"/>).
    /// </summary>
    private readonly string _reportPanelSpokenText;

    /// <summary>
    /// Fills the panel: the opening sentence with the chart's name as a link, the list,
    /// the closing line, which its XAML binds, and the link to the report window under
    /// it. Returns the panel's words as one line for the screen reader, made from the
    /// same strings, so what is read and what is drawn cannot drift apart.
    /// </summary>
    private string BuildReportPanel()
    {
        ReportPanelList.ItemsSource = ReportPanelText.Lines;

        var seeReport = new Hyperlink(new Run(Strings.Completion_ReportPanel_SeeReport))
        {
            Style = (Style)FindResource("SubtleLink"),
        };
        seeReport.Click += ReportPanelSeeReport_Click;
        ReportPanelSeeReportText.Inlines.Clear();
        ReportPanelSeeReportText.Inlines.Add(seeReport);

        return ReportPanelText.SpokenLine(BuildReportPanelIntro());
    }

    /// <summary>
    /// Opens the report window through the view model, which waits for the report's
    /// write and reads the file first. A click landing while that is in hand does
    /// nothing.
    /// </summary>
    private void ReportPanelSeeReport_Click(object sender, RoutedEventArgs e)
    {
        var showReport = _vm.Completion.ShowReportCommand;
        if (showReport.CanExecute(null))
            showReport.Execute(null);
    }

    /// <summary>
    /// Composes the panel's opening sentence from
    /// <see cref="Strings.Completion_ReportPanel_Intro"/>, the phrase in <c>[ ]</c>
    /// becoming a link to the chart of results in the README in the displayed language:
    /// a Run before it, the Hyperlink, and a Run after it. The link opens through
    /// <see cref="UrlLauncher"/>, so this elevated process does not start the browser as
    /// Administrator. Returns the sentence as plain text, brackets removed.
    /// </summary>
    private string BuildReportPanelIntro()
    {
        var raw = Strings.Completion_ReportPanel_Intro;
        ReportPanelIntroText.Inlines.Clear();

        // Where the sentence splits around its link is pure string work in Core (see
        // CompositionParsing); this method only builds inlines.
        if (CompositionParsing.SplitAtBracketedPhrase(raw) is not { } split)
        {
            ReportPanelIntroText.Inlines.Add(new Run(raw));
            return raw;
        }

        var plain = split.Prefix + split.LinkText + split.Suffix;

        var link = new Hyperlink(new Run(split.LinkText))
        {
            NavigateUri = new Uri(ReadmeLinks.For("reports-stats", Localisation.UiCulture)),
            Style = (Style)FindResource("SubtleLink"),
        };
        link.Click += ReportPanelLink_Click;
        // The link's words alone ("this chart") say nothing on their own; the whole
        // sentence, already in the displayed language, does.
        AutomationProperties.SetName(link, plain);

        if (split.Prefix.Length > 0) ReportPanelIntroText.Inlines.Add(new Run(split.Prefix));
        ReportPanelIntroText.Inlines.Add(link);
        if (split.Suffix.Length > 0) ReportPanelIntroText.Inlines.Add(new Run(split.Suffix));

        return plain;
    }

    private void ReportPanelLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Hyperlink link && link.NavigateUri is not null)
            UrlLauncher.OpenUrl(link.NavigateUri.AbsoluteUri);
    }

    /// <summary>
    /// Sets the panel's bottom margin so its bottom edge sits <see cref="ReportPanelGap"/>
    /// above the top of the report line. Its other margins keep it
    /// <see cref="ReportPanelEdgeMargin"/> inside the window, and the overlay's grid
    /// measures it inside all four, so where the room above the line is shorter than
    /// the panel its text scrolls rather than running off the window.
    /// </summary>
    private void PlaceReportPanel()
    {
        if (!CompletionReportLine.IsVisible) return;
        var lineTop = CompletionReportLine.TranslatePoint(new Point(0, 0), CompletionOverlay).Y;
        var bottom = Math.Max(ReportPanelEdgeMargin, CompletionOverlay.ActualHeight - lineTop + ReportPanelGap);
        ReportPanel.Margin = new Thickness(ReportPanelEdgeMargin, ReportPanelEdgeMargin, ReportPanelEdgeMargin, bottom);
    }

    private void OnReportPanelSurroundsSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_vm.Completion.ReportPanelOpen)
            PlaceReportPanel();
    }

    /// <summary>
    /// The panel's half-second wobble as it opens: it drops in from 8 above while
    /// fading in over the first third, dips 4 below, and settles, tilting 2 degrees one
    /// way and the other on the way. Each step eases as a CSS "ease" does. Where the
    /// Windows "show animations" setting is off, the panel simply appears.
    /// </summary>
    private void WobbleReportPanel()
    {
        var tilt = new RotateTransform();
        var drop = new TranslateTransform();
        // Tilted first and then moved, so the tilt turns about the panel's own centre.
        ReportPanel.RenderTransform = new TransformGroup { Children = { tilt, drop } };

        if (AccessibilitySettings.Current.ReduceMotion)
        {
            ReportPanel.BeginAnimation(OpacityProperty, null);
            return;
        }

        drop.BeginAnimation(TranslateTransform.YProperty, WobbleSteps(-8, 0, 4, 0));
        tilt.BeginAnimation(RotateTransform.AngleProperty, WobbleSteps(2, -2, 2, 0));
        ReportPanel.BeginAnimation(OpacityProperty, WobbleSteps(0, 1, 1, 1));
    }

    private static readonly TimeSpan WobbleLength = TimeSpan.FromSeconds(0.5);

    // CSS's "ease" timing function, which a CSS animation applies to each step between
    // keyframes.
    private static readonly KeySpline CssEase = new(0.25, 0.1, 0.25, 1);

    /// <summary>
    /// An animation through four values a third of <see cref="WobbleLength"/> apart,
    /// starting from the first.
    /// </summary>
    private static DoubleAnimationUsingKeyFrames WobbleSteps(double start, double third, double twoThirds, double end)
    {
        var steps = new DoubleAnimationUsingKeyFrames { Duration = WobbleLength };
        steps.KeyFrames.Add(new DiscreteDoubleKeyFrame(start, KeyTime.FromPercent(0)));
        steps.KeyFrames.Add(new SplineDoubleKeyFrame(third, KeyTime.FromPercent(1.0 / 3), CssEase));
        steps.KeyFrames.Add(new SplineDoubleKeyFrame(twoThirds, KeyTime.FromPercent(2.0 / 3), CssEase));
        steps.KeyFrames.Add(new SplineDoubleKeyFrame(end, KeyTime.FromPercent(1), CssEase));
        return steps;
    }

    /// <summary>
    /// A click anywhere in the window but the panel and the "i" closes the panel and
    /// does nothing else, so it does not also tick the box, press a button or close the
    /// card. Taken on the way down, before any control under it sees the click. A click
    /// on the "i" is left to the "i", which closes the panel itself.
    /// </summary>
    private void OnPreviewMouseDownOverReportPanel(object sender, MouseButtonEventArgs e)
    {
        if (!_vm.Completion.ReportPanelOpen) return;
        if (e.OriginalSource is DependencyObject source && IsInReportPanelOrItsButton(source)) return;
        _vm.Completion.ReportPanelOpen = false;
        e.Handled = true;
    }

    /// <summary>
    /// Focus moving to a control elsewhere in the window closes the panel. Focus leaving
    /// the window does not, as when the chart's link opens the browser, and it comes back
    /// to where it was when the window is active again.
    /// </summary>
    private void OnPreviewGotKeyboardFocusOverReportPanel(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!_vm.Completion.ReportPanelOpen) return;
        if (e.NewFocus is not DependencyObject focus || ReferenceEquals(focus, this)) return;
        if (IsInReportPanelOrItsButton(focus)) return;
        _vm.Completion.ReportPanelOpen = false;
    }

    /// <summary>
    /// Whether <paramref name="element"/> is the panel, the "i", or anything inside
    /// either, the Runs and the link in the panel's text included, which are not visuals
    /// and are walked up through the logical tree.
    /// </summary>
    private bool IsInReportPanelOrItsButton(DependencyObject element)
    {
        for (var node = element; node is not null;
             node = node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node))
        {
            if (ReferenceEquals(node, ReportPanel) || ReferenceEquals(node, ReportInfoButton))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Click-outside-to-dismiss for the result overlay. Routed via
    /// the dim Border's MouseLeftButtonDown so only a click on the
    /// dim margin triggers it; clicks on the inner content card are
    /// absorbed by their own hit-testing. While the report panel is
    /// open the window takes the click first and closes only the panel.
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
