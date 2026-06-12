using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean;

public partial class AboutWindow : Window
{
    // Cooldown between successive Check-for-updates clicks. Only bites
    // if something is hammering the button: a hand-driven user clicks
    // once and reads the dialog. Five seconds keeps a stuck button or
    // a UI-automation loop from running into GitHub's 60/hour
    // unauthenticated rate-limit on a long enough timescale.
    private static readonly TimeSpan CheckForUpdatesCooldown = TimeSpan.FromSeconds(5);

    private readonly IUpdateCheckService _updateCheckService;
    private CancellationTokenSource? _checkCts;

    public AboutWindow(IUpdateCheckService updateCheckService)
    {
        InitializeComponent();
        _updateCheckService = updateCheckService;
        var version = DisplayHelpers.GetVersionString();
        VersionText.Text = version;
        // The version sits in a read-only TextBox so it can be selected
        // and copied, and an UNNAMED edit control is announced
        // control-type first ("edit, read-only" before the version).
        // Naming the box with its own text puts the content first; there
        // is no visible label to use instead.
        AutomationProperties.SetName(VersionText, version);

        ApplyScaledBounds();
        AccessibilitySettings.Current.PropertyChanged += OnAccessibilitySettingsChanged;
        // A live text-scale increase re-applies the scaled bounds and
        // the shown window grows down and right from a fixed top-left;
        // the nudge brings the Close row back inside the work area.
        // NoResize means SizeChanged only ever fires for that growth.
        SizeChanged += OnWindowSizeChanged;

        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
        // Open with focus on Close (IsCancel, the non-committal control),
        // matching the modals' Cancel-first rule: a visible ring at once,
        // and the first Tab is not left to land on whatever happens to be
        // the first stop. Deferred to Loaded so the visual tree exists
        // when Focus runs.
        Loaded += (_, _) => CloseButton.Focus();
    }

    /// <summary>
    /// 500 x 320 at 100% text scale is the designed box; the slack
    /// between the content's natural height and the box sits in the
    /// layout's star spacer row, which pins the say-thanks block to
    /// the bottom. The box multiplies by the text-scale factor (the
    /// fixed margins and gaps make the content grow sub-linearly, so
    /// it always fits) and clamps to the work area, so the Close row
    /// can never be pushed off screen the way a fixed 320 height cut
    /// it off at 208%. Width and Height are explicit and first
    /// assigned before the window handle exists, the shape every
    /// band-free window in the app uses. Sized to content instead
    /// (SizeToContent="WidthAndHeight" with the scaled box as window
    /// minimums, re-applied from OnSourceInitialized), the window
    /// opened larger than its arranged content by exactly the
    /// standard caption frame, 37 by 14 device-independent units of
    /// unpainted black along the bottom and right edges (observed
    /// 2026-06-13 at 125% monitor scale, custom WindowChrome active).
    /// The constructor resolves the work area against the owner-to-be
    /// (the main window); a live text-scale change re-resolves against
    /// the actual owner.
    /// </summary>
    private void ApplyScaledBounds()
    {
        var reference = Owner ?? Application.Current?.MainWindow;
        var factor = AccessibilitySettings.Current.TextScaleFactor;
        Width = DetailWindowSizing.ClampWidthToWorkArea(reference, 500 * factor, 0);
        Height = DetailWindowSizing.ClampHeightToWorkArea(reference, 320 * factor, 0);
    }

    private void OnAccessibilitySettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(AccessibilitySettings.TextScaleFactor))
            ApplyScaledBounds();
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        => DetailWindowSizing.NudgeIntoWorkArea(this);

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
                // Disabling the focused button orphans keyboard focus.
                // Restore it to the button on re-enable, but only when
                // focus has not moved on to another control meanwhile.
                if (FocusManager.GetFocusedElement(this) is null or AboutWindow)
                    button.Focus();
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
        UrlLauncher.OpenUrl("https://nofaff.netlify.app/support");

    protected override void OnClosed(EventArgs e)
    {
        AccessibilitySettings.Current.PropertyChanged -= OnAccessibilitySettingsChanged;
        SizeChanged -= OnWindowSizeChanged;
        _checkCts?.Cancel();
        _checkCts?.Dispose();
        base.OnClosed(e);
    }
}
