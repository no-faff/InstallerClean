using System.Threading;
using System.Windows;
using System.Windows.Input;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean;

public partial class AboutWindow : Window
{
    private readonly IUpdateCheckService _updateCheckService;
    private readonly IResultLogService _resultLogService;
    private CancellationTokenSource? _checkCts;

    public AboutWindow(IUpdateCheckService updateCheckService, IResultLogService resultLogService)
    {
        InitializeComponent();
        _updateCheckService = updateCheckService;
        _resultLogService = resultLogService;
        VersionText.Text = DisplayHelpers.GetVersionString();
        ViewLastResultLogContainer.Visibility = _resultLogService.HasFreshLog
            ? Visibility.Visible : Visibility.Collapsed;
        this.EnableAltSpaceSystemMenu();
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private async void CheckNowClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button) return;

        // Cancel any in-flight check from a previous click. Only one
        // check is in flight at a time; the second click supersedes
        // the first.
        _checkCts?.Cancel();
        _checkCts = new CancellationTokenSource();
        var token = _checkCts.Token;

        var originalContent = button.Content;
        button.IsEnabled = false;
        Mouse.OverrideCursor = Cursors.Wait;
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
                case UpToDate up:
                    MessageBox.Show(
                        string.Format(Strings.UpdateCheck_UpToDate_Body, up.CurrentVersion),
                        Strings.UpdateCheck_Title,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    break;

                case UpdateAvailable available:
                    var body = string.Format(
                        Strings.UpdateCheck_UpdateAvailable_Body,
                        available.CurrentVersion, available.LatestVersion);
                    var choice = MessageBox.Show(
                        body,
                        Strings.UpdateCheck_UpdateAvailable_Title,
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Information);
                    if (choice == MessageBoxResult.OK)
                        UrlLauncher.OpenUrl(available.ReleaseUrl);
                    break;

                case CheckFailed failed:
                    MessageBox.Show(
                        FailureReasonText(failed.ReasonCode),
                        Strings.UpdateCheck_Title,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    break;
            }
        }
        finally
        {
            Mouse.OverrideCursor = null;
            button.Content = originalContent;
            button.IsEnabled = true;
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

    private void ViewLastResultLogClick(object sender, RoutedEventArgs e)
    {
        // Convert to a file:// URI so url.dll,FileProtocolHandler
        // ShellExecutes via the registered handler for .json. Plain
        // paths can confuse rundll32's argument parser when they
        // contain spaces, the file:// form quotes the path uniformly.
        // Routing through UrlLauncher keeps the editor at medium IL
        // even though InstallerClean is running elevated.
        var uri = new Uri(_resultLogService.LastLogPath).AbsoluteUri;
        UrlLauncher.OpenUrl(uri);
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
