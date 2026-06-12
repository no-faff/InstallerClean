using System.Windows;
using InstallerClean.Helpers;

namespace InstallerClean;

public partial class ConfirmSendResultLogWindow : Window
{
    public ConfirmSendResultLogWindow(string jsonContent)
    {
        InitializeComponent();
        JsonText.Text = jsonContent;

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

    private void OnSend(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
