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
        // a folder boundary (after ...\Installer\, not inside a folder name). The
        // insertion is in Core (CompositionParsing) so a test guards the escape.
        DestinationText.Text = CompositionParsing.InsertPathWrapPoints(destination);

        // A same-drive move is a rename: it frees nothing until the user deletes
        // the parked copies themselves. This is the only moment the app knows
        // that and the user is still deciding.
        if (sameDrive)
            SameDriveNote.Visibility = Visibility.Visible;

        // The window title is what a screen reader announces when a dialog
        // opens, and ShowInTaskbar is false under custom chrome, so it serves
        // the announcement and nothing else. It carries the question itself: a
        // title naming only the category leaves the count and the size
        // unspoken. The same-drive note rides
        // along with it: on open, only the title and the focused button are
        // spoken, so a note left in the body alone would never be heard by the
        // user deciding whether to press Move.
        //
        // The destination rides along for the same reason, and it is the fact
        // this dialog exists to confirm: where the files are going was on the
        // card and in no announcement, and the body sits in a scroll region
        // that is not even a tab stop until it overflows, so there was no
        // route to it either. Label then value, the order the card reads, so
        // the join holds in every language. The raw path, not the wrapped
        // DestinationText, whose zero-width spaces are for the line breaker.
        var destinationLine = DestinationLabel.Text + " " + destination;
        Title = sameDrive
            ? MessageText.Text + " " + destinationLine + " " + Strings.Confirm_MoveSameDrive
            : MessageText.Text + " " + destinationLine;

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
