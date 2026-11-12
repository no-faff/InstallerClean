using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean;

public partial class SplashWindow : Window
{
    // Where the bar stands is worked out by ScanProgressFill, which holds the
    // band arithmetic and no drawing; this window turns what it returns into
    // motion. See that class for how the scan's milestones and ticker divide the
    // range between the floor and the ceiling.
    private readonly ScanProgressFill _fill = new();

    // How long a step of the fill takes when the caller does not say. The closing
    // step does say, so that the last of the bar arrives as the window goes
    // rather than being cut off by it.
    private static readonly TimeSpan StepEase = Durations.FromMilliseconds(250);

    // Set when the user asks to stop. The scan observes cancellation at its next
    // checkpoint, so updates already in flight arrive after this and would
    // otherwise write a phase message over the line saying the app is stopping.
    private bool _cancelling;

    // The waits the scan reports, shown on the step text.
    private readonly WaitLine _stepWait;

    // The wait whose line is being written to the step text, and the wait the step text
    // shows, with the button that stops it. Only the write putting a wait's own line up
    // sets the second (ShowStep), so every other line written there takes the button away.
    private SourceFolderWait? _waitTakingTheLine;
    private SourceFolderWait? _waitShown;

    // Reads the step text out each time it is written (ShowStep).
    private readonly LiveRegionRaises _liveRegions;

    public event EventHandler? CancelRequested;

    // What the XAML gives Cancel to show and to be called, which a press replaces and
    // the end of the scan puts back (ShowScanEnded).
    private readonly object _cancelContent;
    private readonly string _cancelName;

    public SplashWindow()
    {
        InitializeComponent();
        _cancelContent = CancelButton.Content;
        _cancelName = AutomationProperties.GetName(CancelButton);
        _liveRegions = new LiveRegionRaises(Dispatcher);
        ShowStep(Strings.Status_Scanning);
        _stepWait = new WaitLine(() => AutomationProperties.GetName(StepText), ShowStep);
        VersionText.Text = DisplayHelpers.GetVersionString();

        // The 480 x 320 box is the 100% design. The card sizes its height to
        // content (SizeToContent="Height"); MinHeight holds the designed box,
        // the star spacer rows absorbing the slack while the content is
        // shorter, and the card grows when the content is taller, e.g. a
        // larger OS text scale or a longer-language hero name / step text /
        // Cancel outgrowing 320. A fixed Height clips the bottom-pinned
        // version against the card's edge. MaxHeight caps growth at the work
        // area. The splash opens before any other window exists, so the clamp
        // resolves against the primary monitor's work area (the helper's null
        // fallback). Width is assigned rather than sized to content, and the
        // step text wraps inside it, as a wait naming a long share needs.
        var factor = AccessibilitySettings.Current.TextScaleFactor;
        Width = Math.Min(480 * factor, DetailWindowSizing.WorkAreaWidthLimit(null));
        MinHeight = Math.Min(320 * factor, DetailWindowSizing.WorkAreaHeightLimit(null));
        MaxHeight = DetailWindowSizing.WorkAreaHeightLimit(null);

        // The window grows down from a fixed top-left when a live text-size change
        // enlarges its text, so each change of its size brings it back inside the
        // work area. NoResize means SizeChanged never fires for a user drag-resize.
        SizeChanged += (_, _) => DetailWindowSizing.NudgeIntoWorkArea(this);

        this.SuppressFocusVisualOnDeactivation();
        // Loaded fires after the visual tree is realised, so Focus()
        // on the only focusable element lands on first paint and the
        // keyboard-only user sees a visible focus ring immediately.
        Loaded += (_, _) => CancelButton.Focus();
    }

    public void OnScanProgress(ScanProgressUpdate update)
    {
        if (_cancelling) return;

        // A wait takes the step text, which is the live region, and gives it back when
        // it ends. It is no step of the scan, so the bar and the ticker stay where they
        // are.
        if (update.IsWait)
        {
            _waitTakingTheLine = update.Wait;
            try
            {
                _stepWait.Show(update);
            }
            finally
            {
                _waitTakingTheLine = null;
            }

            return;
        }

        if (update.IsMilestone)
        {
            _stepWait.Forget();
            UpdateStep(update.Message, _fill.AtMilestone());
            return;
        }
        // Ticker: display-only. The step text (the live region) is
        // left alone so the splash does not queue one announcement per
        // update.
        ProductTicker.Text = update.Message;
        AnimateProgress(_fill.AtTicker(update.Position, update.Total));
    }

    // The startup scan has finished, so Cancel has nothing left to stop: it goes out
    // of use, and Esc with it, a disabled IsCancel button taking no access key. The
    // step line stays as the scan left it until ShowReady.
    //
    // The scan can return after Cancel has been pressed: the press can land during its
    // last step, which does not stop for it, and a scan that fails returns too. What
    // the press changed is undone here. The button gets back its label and its name
    // and loses its tooltip and help text, and the bar shows a position again, so
    // nothing on the splash says it is stopping. The press stays recorded
    // (_cancelling), which keeps any progress arriving late off the step text.
    public void ShowScanEnded()
    {
        CancelButton.IsEnabled = false;
        if (_cancelling)
        {
            CancelButton.Content = _cancelContent;
            AutomationProperties.SetName(CancelButton, _cancelName);
            CancelButton.ClearValue(FrameworkElement.ToolTipProperty);
            CancelButton.ClearValue(ToolTipService.ShowOnDisabledProperty);
            CancelButton.ClearValue(AutomationProperties.HelpTextProperty);
            SplashProgress.IsIndeterminate = false;
        }
    }

    // The scan has ended and the update check beside it has answered or given up: the
    // step says "Ready" and the bar eases to full over the time the splash has left.
    public void ShowReady(TimeSpan ease) => UpdateStep(Strings.Status_Ready, 100, ease);

    public void UpdateStep(string message, double progressPercent, TimeSpan? ease = null)
    {
        ShowStep(message);
        // A milestone closes the phase the ticker was narrating; clear
        // it so the last product name does not sit stale beside the next
        // phase's message.
        ProductTicker.Text = string.Empty;
        AnimateProgress(progressPercent, ease);
    }

    // Every write to the step text comes through here. The text drawn takes break
    // opportunities inside a path the line names, as a wait on a share does
    // (InstallerPathText.AllowFolderBreaksInAnyPath), and the name a screen reader
    // speaks is the line as composed, followed, while the stop-waiting button shows, by
    // its key and what it leaves alone (DisplayHelpers.WaitingLineWithStopKey). A
    // TextBlock with a name set speaks the name and not its text, so a write straight to
    // Text would leave the name on the line before. The name is set before the text, so
    // anything reading the name as the text changes reads the new line.
    //
    // Each line is read out once the splash is on screen, after the focus moves to Cancel
    // as it opens (LiveRegionRaises), so a screen reader hears every step, each wait and
    // its end, "Cancelling..." and "Ready". The line written as the window is built, before
    // it shows, is not; the first step after Show is.
    private void ShowStep(string line)
    {
        _waitShown = _waitTakingTheLine;
        ShowStopWaiting();
        AutomationProperties.SetName(
            StepText, _waitShown is null ? line : DisplayHelpers.WaitingLineWithStopKey(line));
        StepText.Text = InstallerPathText.AllowFolderBreaksInAnyPath(line);
        if (IsVisible)
            _liveRegions.Queue(StepText, () => IsVisible);
    }

    // Shows the stop-waiting button and the line under it while the step text names a
    // wait, and takes both away otherwise. A button that has the keyboard focus as it
    // goes hands it to Cancel first, so the focus does not fall to the window; where
    // Cancel is out of use, pressed already or the scan over, the focus goes where WPF
    // puts it.
    private void ShowStopWaiting()
    {
        var shown = _waitShown is not null;
        if (!shown && StopWaitingButton.IsKeyboardFocused && CancelButton.IsEnabled)
            CancelButton.Focus();

        var visibility = shown ? Visibility.Visible : Visibility.Collapsed;
        StopWaitingButton.Visibility = visibility;
        StopWaitingLeavesAlone.Visibility = visibility;
        if (_waitShown is { } named)
            AutomationProperties.SetName(StopWaitingButton, DisplayHelpers.StopWaitingFor(named.Root));
    }

    // Gives up on the drive or share the step text names for the rest of the startup
    // scan. The scan ends the wait at once and tells its end, which puts the step text
    // back and takes the button away.
    private void StopWaitingClick(object sender, RoutedEventArgs e) => _waitShown?.StopWaiting();

    private void AnimateProgress(double progressPercent, TimeSpan? ease = null)
    {
        progressPercent = _fill.At(progressPercent);
        if (AccessibilitySettings.Current.ReduceMotion)
        {
            // Reduced motion: set the value with no easing. Clearing the
            // animation clock first is required because an animation left
            // holding its end value otherwise overrides a plain Value
            // assignment.
            SplashProgress.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, null);
            SplashProgress.Value = progressPercent;
            return;
        }
        // ProgressBar.Value isn't implicitly animated; on a fast scan
        // the bar would jump 0 -> ~95 -> 100 in two frames. Ease each
        // step so the splash feels like a deliberate motion rather than
        // a sequence of instantaneous frames.
        var animation = new DoubleAnimation
        {
            To = progressPercent,
            Duration = ease ?? StepEase,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        SplashProgress.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, animation);
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        _cancelling = true;
        CancelButton.IsEnabled = false;
        CancelButton.Content = Strings.Status_Cancelling;
        // SR-announced name tracks the visible Content swap; a
        // declared-once XAML name drifts from the post-click label and
        // from IsEnabled=false.
        AutomationProperties.SetName(CancelButton, Strings.Status_Cancelling);
        // Tooltip explains the disabled state: cancellation is observed
        // at the next CancellationToken checkpoint, which during a
        // mid-MSI-API call can be several seconds. Without this hint a
        // user sees a frozen "Cancelling..." button and can't tell the
        // app from hung.
        ToolTipService.SetShowOnDisabled(CancelButton, true);
        CancelButton.ToolTip = Strings.Tooltip_CancellingPending;
        // The same sentence as HelpText, from the same value, because a WPF
        // ToolTip opens on hover and never on keyboard focus: without this the
        // one explanation for a button that has just gone dead reaches a mouse
        // user only, and the wait here sits inside an MSI API call and can be
        // the longest in the app.
        AutomationProperties.SetHelpText(CancelButton, Strings.Tooltip_CancellingPending);
        ShowStep(Strings.Status_Cancelling);
        ProductTicker.Text = string.Empty;

        // The length of this wait is set by where the scan happens to be, so the
        // bar stops claiming a position and shows only that the app is working,
        // which is the bar the main window shows for a scan of its own. The
        // animation clock is cleared first because a determinate animation left
        // running holds its end value against the bar it no longer drives.
        SplashProgress.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, null);
        SplashProgress.IsIndeterminate = true;

        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
