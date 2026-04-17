using System.Windows;

namespace InstallerClean.Services;

public sealed class DialogService : IDialogService
{
    public void ShowInformation(string message, string caption) =>
        Show(message, caption, MessageBoxImage.Information);

    public void ShowWarning(string message, string caption) =>
        Show(message, caption, MessageBoxImage.Warning);

    public void ShowError(string message, string caption) =>
        Show(message, caption, MessageBoxImage.Error);

    private static void Show(string message, string caption, MessageBoxImage icon)
    {
        // MessageBox requires the UI thread; marshal there if a background caller invokes us.
        var app = Application.Current;
        if (app is null || app.Dispatcher.CheckAccess())
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, icon);
            return;
        }
        app.Dispatcher.Invoke(() =>
            MessageBox.Show(message, caption, MessageBoxButton.OK, icon));
    }
}
