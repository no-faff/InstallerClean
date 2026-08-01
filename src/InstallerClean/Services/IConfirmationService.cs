namespace InstallerClean.Services;

/// <summary>
/// Shows the Move and Delete confirmation dialogs. Extracted behind an
/// interface so ViewModel tests can simulate user confirm/cancel without
/// spawning real Windows.
/// </summary>
public interface IConfirmationService
{
    /// <summary>
    /// Shows the Move confirmation. <paramref name="sameDrive"/> is the caller's
    /// pre-flight verdict on the destination volume: a same-drive move is a
    /// rename, so it frees no space until the user deletes the parked copies,
    /// and the dialog says so. The caller classifies it (off the dispatcher,
    /// once) rather than the dialog re-deriving it.
    /// </summary>
    bool ConfirmMove(int fileCount, string sizeDisplay, string destination, bool sameDrive);

    bool ConfirmDelete(int fileCount, string sizeDisplay);

    /// <summary>
    /// Shows the diagnostic-log confirmation dialog. <paramref name="jsonContent"/>
    /// is the literal text the app is about to POST to the No Faff endpoint.
    /// Returns true if the user pressed Send, false if they cancelled or
    /// closed the window.
    /// </summary>
    bool ConfirmSendResultLog(string jsonContent);

    /// <summary>
    /// Shows the folder browser for the backup folder. Returns the chosen
    /// folder, or <c>null</c> if the user cancelled or there is no host
    /// window to own the dialog.
    ///
    /// <paramref name="currentDestination"/> is whatever the Move box holds,
    /// and the browser opens there when it names a folder that exists. Browse
    /// is most often pressed to change a destination rather than to set the
    /// first one, and starting at the shell default made the user navigate back
    /// to a path already on screen.
    ///
    /// It sits behind this interface for the same reason the confirmations
    /// do: Move asks for a destination when none is set, so without it that
    /// path could only be exercised by opening a real folder browser, which
    /// a test run cannot answer.
    /// </summary>
    string? AskForMoveDestination(string? currentDestination = null);
}
