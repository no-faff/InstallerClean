using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean;

public partial class RecycleUnavailableWindow : Window
{
    /// <summary>
    /// The user's choice. Initialised to
    /// <see cref="RecycleUnavailableChoice.Cancel"/> so closing the window
    /// by Esc or the system menu (neither runs a Click handler) is read as
    /// cancel, never as a delete.
    /// </summary>
    public RecycleUnavailableChoice Choice { get; private set; } = RecycleUnavailableChoice.Cancel;

    public RecycleUnavailableWindow(int fileCount, string sizeDisplay)
    {
        InitializeComponent();
        var label = DisplayHelpers.PluraliseFile(fileCount);
        var bodyFormat = DisplayHelpers.Pluralise(fileCount,
            Strings.RecycleUnavailable_Body_Singular,
            Strings.RecycleUnavailable_Body_Plural,
            "RecycleUnavailable.Body");
        BodyText.Text = string.Format(bodyFormat, fileCount, label, sizeDisplay);
        ReassuranceText.Text = DisplayHelpers.Pluralise(fileCount,
            Strings.RecycleUnavailable_Reassurance_Singular,
            Strings.RecycleUnavailable_Reassurance_Plural,
            "RecycleUnavailable.Reassurance");
        // Title carries the file count and size so they are spoken when the
        // dialog opens, matching the Confirm dialogs' Title = on-card text.
        // ShowInTaskbar=false and the custom chrome mean the Title is never
        // rendered, so this is announcement-only, not a visible change.
        Title = BodyText.Text;

        // Sized to content; the clamp stops a very large text scale
        // pushing the card past the work area, at which point the body
        // row scrolls and the three choice buttons stay visible.
        MaxHeight = DetailWindowSizing.WorkAreaHeightLimit(Application.Current?.MainWindow);

        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
        // Open with focus on Cancel (IsDefault/IsCancel, the safe
        // default) so a keyboard user gets a visible focus ring at once
        // and a reflexive Space cannot trigger a permanent delete.
        // Deferred to Loaded so the visual tree exists when Focus runs.
        Loaded += (_, _) => CancelButton.Focus();
    }

    private void OnMove(object sender, RoutedEventArgs e)
    {
        Choice = RecycleUnavailableChoice.MoveInstead;
        DialogResult = true;
    }

    private void OnDeletePermanently(object sender, RoutedEventArgs e)
    {
        Choice = RecycleUnavailableChoice.DeletePermanently;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Choice = RecycleUnavailableChoice.Cancel;
        DialogResult = false;
    }
}
