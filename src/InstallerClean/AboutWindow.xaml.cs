using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean;

public partial class AboutWindow : Window
{
    // Cooldown between successive Check-for-updates clicks. A hand-
    // driven user clicks once and reads the dialog; the cooldown only
    // bites if something is hammering the button. That pattern
    // (elevated process repeatedly hitting an external endpoint in
    // rapid succession) is what behaviour-based AV classifiers score
    // against as beaconing, and is also what GitHub's unauthenticated
    // 60/hour rate-limit refuses on a long enough timescale. Five
    // seconds breaks the repeat-call shape without affecting any
    // legitimate user.
    private static readonly TimeSpan CheckForUpdatesCooldown = TimeSpan.FromSeconds(5);

    private readonly IUpdateCheckService _updateCheckService;
    private CancellationTokenSource? _checkCts;

    public AboutWindow(IUpdateCheckService updateCheckService)
    {
        InitializeComponent();
        _updateCheckService = updateCheckService;
        VersionText.Text = DisplayHelpers.GetVersionString();
        this.EnableAltSpaceSystemMenu();
        this.ClearFocusOnDeactivation();
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private async void CheckNowClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button) return;

        // Swap-then-dispose. The previous CTS is cancelled to release
        // any waiter inside CheckAsync; disposing it after the swap
        // matches the CleanupViewModel.ScheduleMoveDestinationSave
        // pattern and avoids leaking one CTS per rapid double-click.
        var previous = _checkCts;
        _checkCts = new CancellationTokenSource();
        previous?.Cancel();
        previous?.Dispose();
        var token = _checkCts.Token;

        button.IsEnabled = false;
        Mouse.OverrideCursor = Cursors.Wait;
        CheckStatusText.Text = Strings.UpdateCheck_Status_Checking;
        try
        {
            UpdateCheckResult result;
            try
            {
                result = await _updateCheckService.CheckAsync(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            switch (result)
            {
                case UpToDate:
                    CheckStatusText.Text = Strings.UpdateCheck_Status_UpToDate;
                    break;

                case UpdateAvailable available:
                    var dialog = new UpdateAvailableWindow(available.CurrentVersion, available.LatestVersion)
                    {
                        Owner = this,
                    };
                    if (dialog.ShowDialog() == true)
                        UrlLauncher.OpenUrl(available.ReleaseUrl);
                    CheckStatusText.Text = string.Empty;
                    break;

                case CheckFailed failed:
                    MessageBox.Show(
                        FailureReasonText(failed.ReasonCode),
                        Strings.UpdateCheck_Title,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    CheckStatusText.Text = string.Empty;
                    break;
            }
        }
        finally
        {
            Mouse.OverrideCursor = null;
            try
            {
                await Task.Delay(CheckForUpdatesCooldown, token);
            }
            catch (OperationCanceledException)
            {
            }
            // OnClosed cancels _checkCts; on that path the cooldown
            // exits without touching the closing window's elements.
            if (!token.IsCancellationRequested)
            {
                CheckStatusText.Text = string.Empty;
                button.IsEnabled = true;
            }
        }
    }

    private static string FailureReasonText(UpdateCheckFailureReason reason) => reason switch
    {
        UpdateCheckFailureReason.NetworkUnavailable => Strings.UpdateCheck_Failed_NetworkUnavailable,
        UpdateCheckFailureReason.ServerError => Strings.UpdateCheck_Failed_ServerError,
        UpdateCheckFailureReason.ResponseParseError => Strings.UpdateCheck_Failed_ResponseParseError,
        UpdateCheckFailureReason.Timeout => Strings.UpdateCheck_Failed_Timeout,
        _ => Strings.UpdateCheck_Failed_Unknown,
    };

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Documents.Hyperlink link && link.NavigateUri is not null)
            UrlLauncher.OpenUrl(link.NavigateUri.AbsoluteUri);
    }

    private void StarClick(object sender, RoutedEventArgs e) =>
        UrlLauncher.OpenUrl("https://github.com/no-faff/InstallerClean");

    private void DonateClick(object sender, RoutedEventArgs e) =>
        UrlLauncher.OpenUrl("https://nofaff.netlify.app");

    protected override void OnClosed(EventArgs e)
    {
        _checkCts?.Cancel();
        _checkCts?.Dispose();
        base.OnClosed(e);
    }
}
