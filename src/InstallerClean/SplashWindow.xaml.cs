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
    private int _progressMessageCount;

    public event EventHandler? CancelRequested;

    public SplashWindow()
    {
        InitializeComponent();
        VersionText.Text = DisplayHelpers.GetVersionString();

        // The 480 x 320 box is the 100% design; at larger OS text
        // scales the hero name, step text and Cancel outgrow it, so
        // the whole box scales with the factor (the star spacer rows
        // absorb any slack). The splash opens before any other window
        // exists, so the clamp resolves against the primary monitor's
        // work area, the helper's null fallback.
        var factor = AccessibilitySettings.Current.TextScaleFactor;
        Width = Math.Min(480 * factor, DetailWindowSizing.WorkAreaWidthLimit(null));
        Height = Math.Min(320 * factor, DetailWindowSizing.WorkAreaHeightLimit(null));

        this.SuppressFocusVisualOnDeactivation();
        // Loaded fires after the visual tree is realised, so Focus()
        // on the only focusable element lands on first paint and the
        // keyboard-only user sees a visible focus ring immediately.
        Loaded += (_, _) => CancelButton.Focus();
    }

    public void OnScanProgress(ScanProgressUpdate update)
    {
        // Asymptote to 95; every update, milestone or ticker, advances
        // the bar so the fill tracks the enumeration. The closing
        // UpdateStep("Done", 100) finishes the fill.
        _progressMessageCount++;
        var percent = 10 + 85.0 * _progressMessageCount / (_progressMessageCount + 15);
        if (update.IsMilestone)
        {
            UpdateStep(update.Message, percent);
            return;
        }
        // Ticker: per-product, display-only. The step text (the live
        // region) is left alone so the splash does not queue one
        // announcement per installed product.
        ProductTicker.Text = update.Message;
        AnimateProgress(percent);
    }

    public void UpdateStep(string message, double progressPercent)
    {
        StepText.Text = message;
        // A milestone closes the phase the ticker was narrating; clear
        // it so the last product name does not sit stale beside the next
        // phase's message.
        ProductTicker.Text = string.Empty;
        AnimateProgress(progressPercent);
    }

    private void AnimateProgress(double progressPercent)
    {
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
        // step over 250ms so the splash feels like a deliberate motion
        // rather than a sequence of instantaneous frames.
        var animation = new DoubleAnimation
        {
            To = progressPercent,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        SplashProgress.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, animation);
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
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
        StepText.Text = Strings.Status_Cancelling;
        ProductTicker.Text = string.Empty;
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
