using System.Windows;
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
        SplashProgress.Value = progressPercent;
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        CancelButton.IsEnabled = false;
        CancelButton.Content = Strings.Status_Cancelling;
        StepText.Text = Strings.Status_Cancelling;
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
