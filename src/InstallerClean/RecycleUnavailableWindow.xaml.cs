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
            Strings.RecycleUnavailable_Body_Plural);
        BodyText.Text = string.Format(bodyFormat, fileCount, label, sizeDisplay);
        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
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
