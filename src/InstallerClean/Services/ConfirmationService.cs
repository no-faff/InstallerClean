using System.Windows;
using InstallerClean.Resources;

namespace InstallerClean.Services;

public sealed class ConfirmationService : IConfirmationService
{
    public bool ConfirmMove(int fileCount, string sizeDisplay, string destination, bool sameDrive)
    {
        if (Application.Current is null) return false;
        var dialog = new ConfirmMoveWindow(fileCount, sizeDisplay, destination, sameDrive)
        {
            Owner = Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true;
    }

    public bool ConfirmDelete(int fileCount, string sizeDisplay)
    {
        if (Application.Current is null) return false;
        var dialog = new ConfirmDeleteWindow(fileCount, sizeDisplay)
        {
            Owner = Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true;
    }

    public bool ConfirmSendResultLog(string jsonContent)
    {
        if (Application.Current is null) return false;
        var dialog = new ConfirmSendResultLogWindow(jsonContent)
        {
            Owner = Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true;
    }

    public string? AskForMoveDestination(string? currentDestination = null)
    {
        if (Application.Current is null) return null;
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = Strings.FilePicker_ChooseDestinationTitle,
        };

        // Only ever an existing folder, and the check is wrapped because
        // Directory.Exists is the only cheap way to ask: the box holds
        // free text, so it can be a half-typed path, a relative name, a
        // drive that is not there or a UNC share that no longer answers.
        // A start folder the shell cannot resolve is worse than none,
        // so anything short of a confirmed directory falls back to the
        // shell default rather than being passed on.
        var start = currentDestination?.Trim();
        if (!string.IsNullOrEmpty(start))
        {
            try
            {
                if (Directory.Exists(start))
                    dialog.InitialDirectory = start;
            }
            catch (Exception)
            {
                // Unreadable, refused or malformed: no start folder, no dialog.
            }
        }

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
