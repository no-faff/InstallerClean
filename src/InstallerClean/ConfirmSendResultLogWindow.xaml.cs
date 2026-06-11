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
