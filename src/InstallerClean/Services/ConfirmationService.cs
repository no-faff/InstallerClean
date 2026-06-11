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

    public bool ConfirmDelete(int fileCount, string sizeDisplay)
    {
        if (Application.Current is null) return false;
        var dialog = new ConfirmDeleteWindow(fileCount, sizeDisplay)
        {
            Owner = Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true;
    }

    public RecycleUnavailableChoice ConfirmRecycleUnavailable(int fileCount, string sizeDisplay)
    {
        if (Application.Current is null) return RecycleUnavailableChoice.Cancel;
        var dialog = new RecycleUnavailableWindow(fileCount, sizeDisplay)
        {
            Owner = Application.Current.MainWindow,
        };
        // ShowDialog's bool? return is ignored; the three-way answer is
        // carried on the window's Choice property (Cancel by default, so a
        // dismissal that runs no button handler never deletes).
        dialog.ShowDialog();
        return dialog.Choice;
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
}
