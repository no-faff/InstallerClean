namespace InstallerClean.Services;

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
    /// Shows the diagnostic-log confirmation dialog. <paramref name="jsonContent"/>
    /// is the literal text the app is about to POST to the No Faff endpoint.
    /// Returns true if the user pressed Send, false if they cancelled or
    /// closed the window.
    /// </summary>
    bool ConfirmSendResultLog(string jsonContent);
}
