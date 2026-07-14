using System.Windows;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean;

public partial class ConfirmMoveWindow : Window
{
    /// <param name="sameDrive">
    /// True when the destination sits on the drive the files are already on. The
    /// caller has classified it in its pre-flight, so the dialog is told rather
    /// than working it out: resolving a drive is a Win32 hop, and this runs on
    /// the dispatcher.
    /// </param>
    public ConfirmMoveWindow(int fileCount, string sizeDisplay, string destination, bool sameDrive)
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

        // A same-drive move is a rename: it frees nothing until the user deletes
        // the parked copies themselves. This is the only moment the app knows
        // that and the user is still deciding.
        if (sameDrive)
            SameDriveNote.Visibility = Visibility.Visible;

        // The window title is what a screen reader announces when a
        // dialog opens; the static "Confirm move" left the question
        // itself, the count and size, unspoken. ShowInTaskbar is false,
        // so the title serves announcements only. The same-drive note rides
        // along with it: on open, only the title and the focused button are
        // spoken, so a note left in the body alone would never be heard by the
        // user deciding whether to press Move.
        Title = sameDrive
            ? MessageText.Text + " " + Strings.Confirm_MoveSameDrive
            : MessageText.Text;

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
