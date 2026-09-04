using System.Windows;
using System.Windows.Automation;
using System.Windows.Documents;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean;

public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow(int fileCount, string sizeDisplay)
    {
        InitializeComponent();
        var label = DisplayHelpers.PluraliseFile(fileCount);
        MessageText.Text = string.Format(
            Strings.Confirm_DeleteTitle, DisplayHelpers.FormatCount(fileCount), label, sizeDisplay);
        var body = BuildBodyLine(fileCount);
        // The window title is what a screen reader announces when a dialog
        // opens, and ShowInTaskbar is false under custom chrome, so it serves
        // the announcement and nothing else. It carries the question itself: a
        // title naming only the category leaves the count and the size unspoken.
        //
        // The body rides along with it, as it does on the sibling modals: on
        // open, only the title and the focused button are spoken, so a line left
        // in the body alone would go unheard. Here that line is the one being
        // consented to. It says the deletion is permanent and offers Move as the
        // way to keep a backup, which is the choice still open to the person
        // the dialog is asking.
        Title = MessageText.Text + " " + body;

        // Sized to content; the clamp stops a very large text scale
        // pushing the card past the work area, at which point the body
        // row scrolls and the action buttons stay visible.
        MaxHeight = DetailWindowSizing.WorkAreaHeightLimit(Application.Current?.MainWindow);

        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
        // Open with focus on Cancel (IsDefault/IsCancel, the safe
        // default) so a keyboard user gets a visible focus ring at once
        // and a reflexive Space cannot delete. Deferred to Loaded so the
        // visual tree exists when Focus runs.
        Loaded += (_, _) => CancelButton.Focus();
    }

    /// <summary>
    /// Composes the body from whichever count form DisplayHelpers.Pluralise
    /// picks. A value with no <c>[ ]</c> pair renders verbatim as a single
    /// Run; a value carrying one renders as a prefix Run, a Hyperlink into the
    /// README's "Is it safe?" section, then a suffix Run. The value alone
    /// decides, which is why the split runs over whatever it is handed rather
    /// than over a list of keys, and the resx entry says why no language's
    /// sentence here is meant to carry a pair. Mirrors the main window's
    /// BuildCompletionRestoreLine, including the URL going through
    /// <see cref="UrlLauncher"/> so this elevated process does not launch the
    /// browser as Administrator.
    ///
    /// Returns the same sentence as plain text, brackets removed, for the
    /// window title to announce. Returned rather than recomputed there so the
    /// spoken line and the painted one cannot drift apart, which is how
    /// ConfirmSendResultLogWindow composes its own title.
    /// </summary>
    private string BuildBodyLine(int fileCount)
    {
        var raw = DisplayHelpers.Pluralise(fileCount,
            Strings.Confirm_DeletePermanently_Singular,
            Strings.Confirm_DeletePermanently_Plural,
            "Confirm.DeletePermanently");
        BodyText.Inlines.Clear();

        if (CompositionParsing.SplitAtBracketedPhrase(raw) is not { } split)
        {
            BodyText.Inlines.Add(new Run(raw));
            return raw;
        }

        var plain = split.Prefix + split.LinkText + split.Suffix;

        var link = new Hyperlink(new Run(split.LinkText))
        {
            NavigateUri = new Uri(ReadmeLinks.For("is-it-safe", Localisation.UiCulture)),
            Style = (Style)FindResource("SubtleLink"),
        };
        link.Click += (_, _) => UrlLauncher.OpenUrl(link.NavigateUri.AbsoluteUri);
        // The bracketed phrase is part of the sentence rather than a
        // destination, so the whole sentence with the brackets removed is the
        // link's accessible name.
        AutomationProperties.SetName(link, plain);

        if (split.Prefix.Length > 0) BodyText.Inlines.Add(new Run(split.Prefix));
        BodyText.Inlines.Add(link);
        if (split.Suffix.Length > 0) BodyText.Inlines.Add(new Run(split.Suffix));

        return plain;
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
