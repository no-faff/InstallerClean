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

    /// <summary>
    /// How long the picker waits to learn whether the Move box names a folder before it
    /// opens with no start folder. A network share that does not answer holds the check
    /// for as long as Windows tries it, far longer than this.
    /// </summary>
    internal static readonly TimeSpan StartFolderBound = Durations.FromSeconds(2);

    public async Task<string?> AskForMoveDestinationAsync(string? currentDestination = null, Func<bool>? stillWanted = null)
    {
        if (Application.Current is null) return null;

        var startFolder = await StartFolderAsync(currentDestination, FrameworkDirectory.Exists, StartFolderBound);

        // The window can have closed during the wait, which leaves no flag behind it, so
        // the main window itself is asked, and no picker comes up once it has gone.
        var main = Application.Current?.MainWindow;
        if (main is null || !main.IsVisible || main.Dispatcher.HasShutdownStarted) return null;
        if (stillWanted is not null && !stillWanted()) return null;

        // The main window owns the picker when none of the app's windows is active.
        var fallbackOwner = new WindowInteropHelper(main).Handle;

        return FolderPicker.Pick(Strings.FilePicker_ChooseDestinationTitle, startFolder, fallbackOwner);
    }

    /// <summary>
    /// <paramref name="text"/>, trimmed, where <paramref name="exists"/> says it names a
    /// folder within <paramref name="bound"/>; otherwise null. Only ever an existing folder:
    /// the Move box holds free text, so it can be a half-typed path, a relative name, a drive
    /// that is not there or a network share that no longer answers, and a start folder the
    /// shell cannot resolve is worse than none. The check runs on a thread of its own, so
    /// the window goes on answering while it runs, and one a share holds for as long as
    /// Windows tries it ties up no thread-pool thread. A check that throws, such as the
    /// OperationCanceledException an aborted operation (error 995) maps to, or one still
    /// running at the bound, gives null; one left running ends on its own thread, and its
    /// failure is read there, so nothing reaches the unobserved-task handler.
    /// </summary>
    internal static async Task<string?> StartFolderAsync(string? text, Func<string, bool> exists, TimeSpan bound)
    {
        var start = text?.Trim();
        if (string.IsNullOrEmpty(start)) return null;

        var check = Task.Factory.StartNew(
            () => exists(start!), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        _ = check.ContinueWith(
            t => _ = t.Exception, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        var finished = await Task.WhenAny(check, FrameworkTask.Delay(bound, CancellationToken.None));
        return finished == check && check.Status == TaskStatus.RanToCompletion && check.Result ? start : null;
    }
}
