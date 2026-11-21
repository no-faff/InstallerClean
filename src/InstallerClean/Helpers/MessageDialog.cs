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
/// Every message goes through here so that none reaches the user as a stock
/// <c>MessageBox</c>, a light-grey Win32 dialog, in an app whose every other
/// surface is a dark card. The stock box is used only as the fallback in
/// <see cref="ShowCore"/>, for when the themed window itself cannot be built.
/// </summary>
internal static class MessageDialog
{
    /// <summary>
    /// Shows the message. Where <paramref name="linkUrl"/> is given, the phrase in
    /// <c>[ ]</c> inside the message is a link to it.
    /// </summary>
    public static void Show(string message, string caption, MessageKind kind, string? linkUrl = null)
    {
        var app = Application.Current;
        if (app is null || app.Dispatcher.CheckAccess())
        {
            ShowCore(message, caption, kind, linkUrl);
            return;
        }
        app.Dispatcher.Invoke(() => ShowCore(message, caption, kind, linkUrl));
    }

    private static void ShowCore(string message, string caption, MessageKind kind, string? linkUrl)
    {
        try
        {
            var dialog = new MessageWindow(message, caption, kind, linkUrl);

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
            // App's crash handlers show their message through here with the app
            // already failing, and a broken theme resource is one of the things
            // that can have failed it. A StaticResource whose runtime type does
            // not match the property it fills throws when WPF loads or applies
            // it, and MessageWindow is built from the same theme. The stock box
            // needs none of the app's own resources, so it still paints. Remove
            // this fallback, or give it anything from the theme, and a failure in
            // the theme shows the user no message at all.
            CrashLog.TryWrite(ex);
            MessageBox.Show(Unbracketed(message, linkUrl), caption, MessageBoxButton.OK, IconFor(kind));
        }
    }

    /// <summary>
    /// The message as the stock box shows it: a link phrase's brackets taken out,
    /// the stock box having no link to put there.
    /// </summary>
    private static string Unbracketed(string message, string? linkUrl) =>
        linkUrl is not null && CompositionParsing.SplitAtBracketedPhrase(message) is { } split
            ? split.Prefix + split.LinkText + split.Suffix
            : message;

    private static MessageBoxImage IconFor(MessageKind kind) => kind switch
    {
        MessageKind.Error => MessageBoxImage.Error,
        MessageKind.Warning => MessageBoxImage.Warning,
        _ => MessageBoxImage.Information,
    };
}
