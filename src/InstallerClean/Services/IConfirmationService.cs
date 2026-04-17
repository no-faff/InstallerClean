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
}
