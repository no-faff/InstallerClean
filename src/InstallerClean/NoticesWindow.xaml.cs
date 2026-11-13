using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean;

/// <summary>
/// The window the About window's "Third-party notices" link opens: a heading, the text of
/// THIRD-PARTY-NOTICES as the build embedded it (<see cref="ThirdPartyNotices"/>), and one
/// Close button, on the same dark card as the app's other dialogs.
/// </summary>
public partial class NoticesWindow : Window
{
    public NoticesWindow()
    {
        InitializeComponent();

        // The title is what a screen reader announces when the dialog opens; ShowInTaskbar
        // is false and the chrome paints no caption, so it is never drawn. The notices
        // themselves are reached with Tab.
        Title = Strings.Window_Notices_Title;
        NoticesText.Text = ThirdPartyNotices.Read();

        // A fixed width, 700 scaled, held to the work area: wide enough at 100% text
        // scale for the file's 79-column lines in the fixed-width face, so its tables and
        // indented licences keep their columns.
        this.KeepCardInsideWorkArea(Card, minimumWidth: 700, maximumWidth: 700);

        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
        // Close is the only action, so it takes focus (and Enter and Esc, being IsDefault
        // and IsCancel both). Deferred to Loaded so the visual tree exists when Focus runs.
        Loaded += (_, _) => CloseButton.Focus();
    }

    private void OnClose(object sender, RoutedEventArgs e) => DialogResult = true;
}
