using System.Windows;
using System.Windows.Automation;
using System.Windows.Documents;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean;

public partial class ConfirmSendResultLogWindow : Window
{
    // Stable README anchor (an explicit <a id="reports-stats"> right before
    // the "Will I actually free up GBs of space?" FAQ answer of every
    // README, so rewording the question never breaks the link). The
    // Reassurance line's "[how much space people are freeing]" phrase links
    // here; the URL targets the README in the displayed language.
    private static string ReportsStatsUrl => ReadmeLinks.For("reports-stats", Localisation.UiCulture);

    public ConfirmSendResultLogWindow(string jsonContent)
    {
        InitializeComponent();
        JsonText.Text = jsonContent;
        BuildReassuranceLine();

        // Sized to content, so the whole report is visible with no
        // scrollbar; the clamp stops an error-heavy report or a very
        // large text scale pushing the window past the work area, and
        // the JSON box scrolls only once the clamp binds.
        MaxHeight = DetailWindowSizing.WorkAreaHeightLimit(Application.Current?.MainWindow);

        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
        // Open with focus on Cancel so a keyboard user gets a visible
        // ring at once, matching the other modals. Send stays IsDefault
        // (the action is not destructive), and Enter on the focused
        // Cancel still activates Cancel, so a reflexive Enter dismisses.
        // Deferred to Loaded so the visual tree exists when Focus runs.
        Loaded += (_, _) => CancelButton.Focus();
    }

    /// <summary>
    /// Composes the Reassurance line from <see cref="Strings.ConfirmSendResultLog_Reassurance"/>,
    /// rendering the <c>[ ]</c>-delimited phrase as a Hyperlink into the
    /// README's reports-stats FAQ answer: a prefix Run, the Hyperlink, then a
    /// suffix Run. Mirrors MainWindow's BuildCompletionRestoreLine; the URL
    /// opens through <see cref="UrlLauncher"/> so this elevated process does
    /// not launch the browser as Administrator.
    /// </summary>
    private void BuildReassuranceLine()
    {
        var raw = Strings.ConfirmSendResultLog_Reassurance;
        ReassuranceText.Inlines.Clear();

        // Where the sentence splits around its [ ]-delimited link is pure string
        // work in Core (see CompositionParsing); this method only builds inlines.
        if (CompositionParsing.SplitAtBracketedPhrase(raw) is not { } split)
        {
            ReassuranceText.Inlines.Add(new Run(raw));
            return;
        }

        var link = new Hyperlink(new Run(split.LinkText))
        {
            NavigateUri = new Uri(ReportsStatsUrl),
            Style = (Style)FindResource("SubtleLink"),
        };
        link.Click += Hyperlink_Click;
        // The link text alone ("how much space people are freeing") is not a
        // self-contained accessible name; the whole sentence (brackets
        // removed) is, already in the user's language.
        AutomationProperties.SetName(link, split.Prefix + split.LinkText + split.Suffix);

        if (split.Prefix.Length > 0) ReassuranceText.Inlines.Add(new Run(split.Prefix));
        ReassuranceText.Inlines.Add(link);
        if (split.Suffix.Length > 0) ReassuranceText.Inlines.Add(new Run(split.Suffix));
    }

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Hyperlink link && link.NavigateUri is not null)
            UrlLauncher.OpenUrl(link.NavigateUri.AbsoluteUri);
    }

    private void OnSend(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
