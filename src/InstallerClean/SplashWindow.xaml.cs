using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using InstallerClean.Helpers;
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
        this.SuppressFocusVisualOnDeactivation();
        // Loaded fires after the visual tree is realised, so Focus()
        // on the only focusable element lands on first paint and the
        // keyboard-only user sees a visible focus ring immediately.
        Loaded += (_, _) => CancelButton.Focus();
    }

    public void OnScanProgress(string message)
    {
        // Asymptote to 95; the closing UpdateStep("Done", 100) finishes the fill.
        _progressMessageCount++;
        var percent = 10 + 85.0 * _progressMessageCount / (_progressMessageCount + 15);
        UpdateStep(message, percent);
    }

    public void UpdateStep(string message, double progressPercent)
    {
        StepText.Text = message;
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
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
