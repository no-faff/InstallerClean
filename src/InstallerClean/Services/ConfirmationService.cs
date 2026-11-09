using System.Windows;
using System.Windows.Interop;
using InstallerClean.Helpers;
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

    public string? AskForMoveDestination(string? currentDestination = null)
    {
        if (Application.Current is null) return null;

        // Only ever an existing folder. The box holds free text, so it can be a
        // half-typed path, a relative name, a drive that is not there or a UNC
        // share that no longer answers. A start folder the shell cannot resolve is
        // worse than none, so anything short of a confirmed folder falls back to the
        // shell default rather than being passed on. FrameworkDirectory.Exists answers
        // false for a path that cannot be read, is refused or is malformed, and the
        // catch takes what it lets through, such as the OperationCanceledException an
        // aborted operation (error 995) maps to: the dialog then opens with no start
        // folder.
        string? startFolder = null;
        var start = currentDestination?.Trim();
        if (!string.IsNullOrEmpty(start))
        {
            try
            {
                if (FrameworkDirectory.Exists(start))
                    startFolder = start;
            }
            catch (Exception)
            {
            }
        }

        // The main window owns the picker when none of the app's windows is active.
        var main = Application.Current.MainWindow;
        var fallbackOwner = main is null ? IntPtr.Zero : new WindowInteropHelper(main).Handle;

        return FolderPicker.Pick(Strings.FilePicker_ChooseDestinationTitle, startFolder, fallbackOwner);
    }
}
