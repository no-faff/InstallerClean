using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.Resources;

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
        // Treated here and nowhere else in this window. The scan diagnoses name
        // the installer cache folder mid-paragraph and, when a scan stops on its
        // own, the crash log's path as well; the first is bound so it cannot break
        // and the second is given break opportunities at its folders. Of the two
        // places the message goes this is the one that gets laid out, so it is the
        // one with a line break to protect; the announced title takes the message
        // as it came. The same split TranslateExtension makes for an automation
        // property. The box is also given the message as it came, so a copy out of
        // it carries the message's own characters and none of the treatment's.
        PlainCopy.SetUntreated(MessageText, message);
        MessageText.Text = InstallerPathText.ForDrawing(message);
        if (kind != MessageKind.Information)
            WarningIcon.Visibility = Visibility.Visible;

        // The title is what a screen reader announces when the dialog opens, and
        // only the title and the focused button are spoken then, so the message
        // rides on it: left in the body alone it would go unheard, which is how
        // the stock message box (which announces its text) behaved. ShowInTaskbar
        // is false and the chrome paints no caption, so the title is never
        // rendered and this is announcement-only, which is why it takes the
        // unbound message rather than the one above it.
        //
        // The stop between the two comes from the resx rather than from here,
        // because the caption ends in no punctuation of its own and the mark that
        // ends a sentence is not the same in every language.
        Title = caption + Strings.Display_SentenceSeparator + message;

        // Sized to content, the card from 360 to 520 scaled. The clamps stop a
        // very large text scale pushing the card past the work area: across, the
        // message wraps into the narrower card, and down, it scrolls and the
        // Close button stays visible.
        this.KeepCardInsideWorkArea(Card, minimumWidth: 360, maximumWidth: 520);

        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
        // Close is the only action, so it takes focus (and Enter and Esc, being
        // IsDefault and IsCancel both). Deferred to Loaded so the visual tree
        // exists when Focus runs.
        Loaded += (_, _) => CloseButton.Focus();
    }

    private void OnClose(object sender, RoutedEventArgs e) => DialogResult = true;
}
