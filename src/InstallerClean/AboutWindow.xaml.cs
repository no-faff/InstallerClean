using System.ComponentModel;
using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean;

public partial class AboutWindow : Window
{
    private readonly ISettingsService _settings;

    // Set only while the auto-update box is being put back to what disk
    // holds, so that assignment's own Checked/Unchecked raise is not read as
    // a second click.
    private bool _revertingAutoUpdateCheck;

    public AboutWindow(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        VersionText.Text = DisplayHelpers.GetVersionString();

        // Resolved here, not in XAML: each supported language has its own
        // README file, and the link has to follow the one on screen.
        GuideLink.NavigateUri = new Uri(ReadmeLinks.Home(Localisation.UiCulture));

        // Painted from disk, then subscribed. Assigning IsChecked raises
        // Checked, so a handler already attached in XAML would write the
        // setting straight back on every open, and Update is a load, a temp
        // write and a rename against a file that can sit on a roaming or
        // OneDrive-redirected profile. Reading the file here rather than
        // reusing a startup snapshot keeps the box honest if the setting has
        // moved since.
        AutoUpdateCheckBox.IsChecked = _settings.Load().AutoUpdateCheck;
        AutoUpdateCheckBox.Checked += AutoUpdateCheckChanged;
        AutoUpdateCheckBox.Unchecked += AutoUpdateCheckChanged;

        ApplyScaledBounds();
        AccessibilitySettings.Current.PropertyChanged += OnAccessibilitySettingsChanged;
        // SizeChanged fires when the height-sized window grows: a live
        // text-scale increase re-applies the bounds and the content grows
        // with the larger type. The window grows down from a fixed
        // top-left, and the nudge brings the Close row back inside the
        // work area. NoResize means SizeChanged never fires for a user
        // drag-resize.
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
    /// The About box sizes its height to its content
    /// (SizeToContent="Height", set in XAML) so a taller content set grows
    /// the window instead of overflowing it; a fixed height clips the
    /// say-thanks and Close rows off the bottom. MinHeight holds the
    /// designed 500 x 400 box at 100% text
    /// scale; while the content is shorter than the box the layout's star
    /// spacer row takes up the slack and pins the say-thanks block to the
    /// bottom. MaxHeight caps growth at the work area so a large OS text
    /// scale cannot push the box past the screen (a fixed height cuts the
    /// Close row off this way at 208%). The box grows sub-linearly with
    /// text scale because the margins and gaps are fixed. Width is
    /// explicit; Height itself is never assigned, which would re-pin the
    /// fixed box.
    ///
    /// Height-only is deliberate. SizeToContent="WidthAndHeight" with this
    /// custom WindowChrome opened the window larger than its arranged
    /// content by exactly the caption frame, 37 by 14 device-independent
    /// units of unpainted black along the bottom and right edges (observed
    /// 2026-06-13 at 125% monitor scale); MainWindow sizes its height the
    /// same way and for the same reason.
    ///
    /// The constructor resolves the work area against the owner-to-be (the
    /// main window); a live text-scale change re-resolves against the
    /// actual owner.
    /// </summary>
    private void ApplyScaledBounds()
    {
        var reference = Owner ?? Application.Current?.MainWindow;
        var factor = AccessibilitySettings.Current.TextScaleFactor;
        Width = DetailWindowSizing.ClampWidthToWorkArea(reference, 500 * factor, 0);
        // MinHeight can never exceed MaxHeight: ClampHeightToWorkArea caps
        // the scaled box at the same work-area limit MaxHeight uses.
        MinHeight = DetailWindowSizing.ClampHeightToWorkArea(reference, 400 * factor, 0);
        MaxHeight = DetailWindowSizing.WorkAreaHeightLimit(reference);
    }

    private void OnAccessibilitySettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(AccessibilitySettings.TextScaleFactor))
            ApplyScaledBounds();
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        => DetailWindowSizing.NudgeIntoWorkArea(this);

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// Persists the auto-update switch. One handler for Checked and
    /// Unchecked because the two differ only in the value they write.
    ///
    /// Update never throws and returns false instead, which the language
    /// pick discards and the move destination's debounced save records only
    /// in crash.log: for those, a lost write costs a preference the user can
    /// see is wrong and set again. This one is different in kind. It is the
    /// app's only consent control over its only unprompted network call, so a
    /// box that shows unticked while the file on disk still says ticked has
    /// the app opening a socket at every launch on the strength of a setting
    /// the user believes they revoked.
    /// A refused write is reachable without anything being broken: a
    /// redirected or read-only profile is enough, and SettingsService
    /// deliberately refuses a settings path that resolves somewhere else.
    /// So the box is put back to what the file actually holds and the
    /// failure is said out loud.
    /// </summary>
    private void AutoUpdateCheckChanged(object sender, RoutedEventArgs e)
    {
        if (_revertingAutoUpdateCheck) return;
        if (_settings.Update(s => s.AutoUpdateCheck = AutoUpdateCheckBox.IsChecked == true))
            return;

        // Re-read rather than invert the click: what the next launch will do
        // is whatever Load returns now, which is the file if it is readable
        // and the defaults if it is not, and neither is derivable from here.
        // The assignment raises Checked/Unchecked again, so the guard is what
        // stops the revert from being taken for a fresh change and writing
        // back into the failure.
        _revertingAutoUpdateCheck = true;
        try
        {
            AutoUpdateCheckBox.IsChecked = _settings.Load().AutoUpdateCheck;
        }
        finally
        {
            _revertingAutoUpdateCheck = false;
        }

        MessageDialog.Show(
            Strings.Error_SettingNotSavedBody,
            Strings.Error_SettingNotSavedTitle,
            MessageKind.Warning);
    }

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
        base.OnClosed(e);
    }
}
