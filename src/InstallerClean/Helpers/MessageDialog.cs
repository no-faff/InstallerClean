using System.Windows;

namespace InstallerClean.Helpers;

/// <summary>What the message is: it picks the icon, nothing else.</summary>
public enum MessageKind
{
    /// <summary>Nothing has gone wrong. No icon.</summary>
    Information,

    /// <summary>The user's action could not be completed. Amber triangle.</summary>
    Warning,

    /// <summary>The app failed. Amber triangle; see MessageWindow.xaml on why
    /// this is not a separate colour.</summary>
    Error,
}

/// <summary>
/// The single entry point for every message the app shows: raises the themed
/// <see cref="MessageWindow"/> on the UI thread, owned by the main window when
/// there is one.
///
/// Every one of these used to be a stock <c>MessageBox</c>: a light-grey Win32
/// dialog with no owner, in an app whose every other surface is a dark card. So
/// the moments the app looked least like itself were the moments it had just
/// failed while running elevated inside C:\Windows\Installer, which is exactly
/// when a nervous user needs it to look like the app they trusted. (The delete
/// confirmation was moved off MessageBox for that reason before v1 shipped; the
/// error surface never got the same treatment.)
/// </summary>
internal static class MessageDialog
{
    public static void Show(string message, string caption, MessageKind kind)
    {
        var app = Application.Current;
        if (app is null || app.Dispatcher.CheckAccess())
        {
            ShowCore(message, caption, kind);
            return;
        }
        app.Dispatcher.Invoke(() => ShowCore(message, caption, kind));
    }

    private static void ShowCore(string message, string caption, MessageKind kind)
    {
        try
        {
            var dialog = new MessageWindow(message, caption, kind);

            // Owner only once the main window is real: assigning a Window that
            // has not been shown throws, and the startup failures and the
            // already-running exit all raise a message before it exists. With no
            // owner, CenterOwner would place the dialog at the desktop origin.
            var owner = Application.Current?.MainWindow;
            if (owner is { IsLoaded: true } && !ReferenceEquals(owner, dialog))
                dialog.Owner = owner;
            else
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            // The last-resort crash handler shows a message when the app is
            // already broken, and a theme resource is one of the things that can
            // have broken it (a StaticResource whose runtime type does not match
            // the consuming property has taken this app down before). Building a
            // themed window then throws from inside the handler and the user gets
            // no dialog at all, just a hard exit. The stock box needs none of the
            // app's own resources, so it still paints.
            CrashLog.TryWrite(ex);
            MessageBox.Show(message, caption, MessageBoxButton.OK, IconFor(kind));
        }
    }

    private static MessageBoxImage IconFor(MessageKind kind) => kind switch
    {
        MessageKind.Error => MessageBoxImage.Error,
        MessageKind.Warning => MessageBoxImage.Warning,
        _ => MessageBoxImage.Information,
    };
}
