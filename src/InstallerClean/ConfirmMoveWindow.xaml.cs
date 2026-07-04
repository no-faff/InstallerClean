using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean;

public partial class ConfirmMoveWindow : Window
{
    public ConfirmMoveWindow(int fileCount, string sizeDisplay, string destination)
    {
        InitializeComponent();
        var label = DisplayHelpers.PluraliseFile(fileCount);
        MessageText.Text = string.Format(Strings.Confirm_MoveTitle, fileCount, label, sizeDisplay);
        DestinationLabel.Text = Strings.Confirm_MoveDestination;
        // Insert a zero-width space after every backslash so a long path wraps at
        // a folder boundary (after ...\Installer\, not inside a folder name). It is
        // spelled with the C# unicode escape; do not paste a literal zero-width
        // character into source (it is invisible and tooling mangles it).
        DestinationText.Text = destination.Replace("\\", "\\\u200B");
        // The window title is what a screen reader announces when a
        // dialog opens; the static "Confirm move" left the question
        // itself, the count and size, unspoken. ShowInTaskbar is false,
        // so the title serves announcements only.
        Title = MessageText.Text;

        // Sized to content; the clamp stops a very large text scale
        // pushing the card past the work area, at which point the
        // destination row scrolls and the action buttons stay visible.
        MaxHeight = DetailWindowSizing.WorkAreaHeightLimit(Application.Current?.MainWindow);

        this.EnableAltSpaceSystemMenu();
        this.SuppressFocusVisualOnDeactivation();
        // Open with focus on Cancel (IsDefault/IsCancel) so a keyboard
        // user gets a visible focus ring at once, mirroring the Confirm
        // Delete dialog. Deferred to Loaded so the visual tree exists
        // when Focus runs.
        Loaded += (_, _) => CancelButton.Focus();
    }

    private void OnMove(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
