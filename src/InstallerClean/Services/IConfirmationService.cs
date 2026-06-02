namespace InstallerClean.Services;

/// <summary>
/// The user's answer to the recycle-unavailable choice dialog.
/// </summary>
public enum RecycleUnavailableChoice
{
    /// <summary>
    /// Do nothing. The default, so dismissing the dialog by Esc or its
    /// system menu (neither runs a button handler) is treated as cancel
    /// and never deletes.
    /// </summary>
    Cancel,

    /// <summary>Route into the Move flow instead of deleting.</summary>
    MoveInstead,

    /// <summary>
    /// Delete the files permanently, with the user's explicit consent,
    /// because the Recycle Bin is unavailable for their volume.
    /// </summary>
    DeletePermanently,
}

/// <summary>
/// Shows the Move and Delete confirmation dialogs. Extracted behind an
/// interface so ViewModel tests can simulate user confirm/cancel without
/// spawning real Windows.
/// </summary>
public interface IConfirmationService
{
    bool ConfirmMove(int fileCount, string sizeDisplay, string destination);

    bool ConfirmDelete(int fileCount, string sizeDisplay, long totalBytes, long maxSingleFileBytes);

    /// <summary>
    /// Shows the choice offered when Delete finds the Recycle Bin
    /// unavailable for the files' volume: Move instead (the safe path),
    /// delete permanently with consent, or cancel. Nothing has been
    /// deleted when this is shown. Returns
    /// <see cref="RecycleUnavailableChoice.Cancel"/> if there is no host
    /// window to own the dialog.
    /// </summary>
    RecycleUnavailableChoice ConfirmRecycleUnavailable(int fileCount, string sizeDisplay);

    /// <summary>
    /// Shows the diagnostic-log confirmation dialog. <paramref name="jsonContent"/>
    /// is the literal text the app is about to POST to the No Faff endpoint.
    /// Returns true if the user pressed Send, false if they cancelled or
    /// closed the window.
    /// </summary>
    bool ConfirmSendResultLog(string jsonContent);
}
