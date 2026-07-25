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

        CaptionText.Text = caption;
        // Bound here and nowhere else. Two of the scan diagnoses name the
        // installer cache folder mid-paragraph, and of the two places the message
        // goes this is the one that gets laid out, so it is the one with a line
        // break to protect; the announced title takes the message as it came. The
        // same split TranslateExtension makes for an automation property.
        MessageText.Text = InstallerPathText.KeepWhole(message);
        if (kind != MessageKind.Information)
            WarningIcon.Visibility = Visibility.Visible;

        // The title is what a screen reader announces when the dialog opens, and
        // only the title and the focused button are spoken then, so the message
        // rides on it: left in the body alone it would go unheard, which is how
        // the stock message box (which announces its text) behaved. ShowInTaskbar
        // is false and the chrome paints no caption, so the title is never
        // rendered and this is announcement-only, which is why it takes the
        // unbound message rather than the one above it.
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
