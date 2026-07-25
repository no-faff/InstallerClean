using System.Windows;
using InstallerClean.Helpers;

namespace InstallerClean;

/// <summary>
/// The app's own message dialog: one heading, one message, one Close button, on
/// the same dark card as every confirmation. Raised through
/// <see cref="Helpers.MessageDialog"/>, which owns the fallback for the case
/// where this window cannot be built.
/// </summary>
public partial class MessageWindow : Window
{
    public MessageWindow(string message, string caption, MessageKind kind)
    {
        InitializeComponent();

        // Both the body and the announced title below draw from it, and two of
        // the scan diagnoses name C:\Windows\Installer mid-paragraph.
        message = InstallerPathText.KeepWhole(message);

        CaptionText.Text = caption;
        MessageText.Text = message;
        if (kind != MessageKind.Information)
            WarningIcon.Visibility = Visibility.Visible;

        // The title is what a screen reader announces when the dialog opens, and
        // only the title and the focused button are spoken then, so the message
        // rides on it: left in the body alone it would go unheard, which is how
        // the stock message box (which announces its text) behaved. ShowInTaskbar
        // is false and the chrome paints no caption, so the title is never
        // rendered and this is announcement-only.
        Title = caption + ". " + message;

        // Sized to content; the clamp stops a very large text scale pushing the
        // card past the work area, at which point the message scrolls and the
        // Close button stays visible.
        MaxHeight = DetailWindowSizing.WorkAreaHeightLimit(Application.Current?.MainWindow);

        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
        // Close is the only action, so it takes focus (and Enter and Esc, being
        // IsDefault and IsCancel both). Deferred to Loaded so the visual tree
        // exists when Focus runs.
        Loaded += (_, _) => CloseButton.Focus();
    }

    private void OnClose(object sender, RoutedEventArgs e) => DialogResult = true;
}
