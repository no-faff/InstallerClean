using System.Windows;

namespace InstallerClean.Services;

public sealed class ConfirmationService : IConfirmationService
{
    public bool ConfirmMove(int fileCount, string sizeDisplay, string destination)
    {
        if (Application.Current is null) return false;
        var dialog = new ConfirmMoveWindow(fileCount, sizeDisplay, destination)
        {
            Owner = Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true;
    }

    public bool ConfirmDelete(int fileCount, string sizeDisplay, long totalBytes, long maxSingleFileBytes)
    {
        if (Application.Current is null) return false;
        var dialog = new ConfirmDeleteWindow(fileCount, sizeDisplay, totalBytes, maxSingleFileBytes)
        {
            Owner = Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true;
    }
}
