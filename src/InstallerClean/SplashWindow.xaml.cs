using System.Windows;
using System.Windows.Media.Animation;
using InstallerClean.Helpers;

namespace InstallerClean;

public partial class SplashWindow : Window
{
    private int _progressMessageCount;

    public event EventHandler? CancelRequested;

    public SplashWindow()
    {
        InitializeComponent();
        VersionText.Text = DisplayHelpers.GetVersionString();
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

        var container = SplashProgressBorder.Parent as FrameworkElement;
        if (container == null) return;

        container.UpdateLayout();
        var targetWidth = container.ActualWidth * (progressPercent / 100.0);

        var animation = new DoubleAnimation
        {
            To = targetWidth,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        SplashProgressBorder.BeginAnimation(WidthProperty, animation);
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        CancelButton.IsEnabled = false;
        CancelButton.Content = "Cancelling...";
        StepText.Text = "Cancelling...";
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
