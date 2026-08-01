using System.Windows;
using InstallerClean.Resources;

namespace InstallerClean.Helpers;

/// <summary>
/// WPF-host wrapper around <see cref="UnelevatedLauncher.OpenUrl"/>
/// that adds the host-side fallback when the shell-token chain is
/// unavailable. The Core helper returns a result rather than spawning
/// an elevated browser; this class handles the result by copying the
/// URL to the user's clipboard and showing a one-line dialog. The
/// app never spawns an elevated browser, regardless of session state.
/// </summary>
internal static class UrlLauncher
{
    /// <summary>
    /// Starts the launch and returns at once. Every caller is a click handler
    /// on the dispatcher thread and the chain underneath is six cross-process
    /// COM calls into Explorer bounded at ten seconds, so run synchronously it
    /// is up to ten seconds of a window that cannot repaint, which Windows
    /// relabels "(Not Responding)" after five. It is quick while the shell is
    /// healthy and slow exactly when it is not, and the links it serves are
    /// the donate heart, the update link, the About links and the "Is it
    /// safe?" hyperlink: the moments the app is asking to be trusted.
    /// </summary>
    public static void OpenUrl(string url)
    {
        // Captured here rather than read on the worker: Application.Current is
        // a static the worker would race with shutdown for.
        var dispatcher = Application.Current?.Dispatcher;

        Task.Run(() =>
        {
            // Returns a result for every outcome, exceptions included, so
            // there is nothing here for the unobserved-task handler to catch.
            if (UnelevatedLauncher.OpenUrl(url).Launched) return;

            // Only the fallback comes back, and it has to: the clipboard needs
            // an STA thread and the dialog is a window. Dropped if the app is
            // going away, which is a user who clicked a link and closed the
            // window inside the same second.
            if (dispatcher is null || dispatcher.HasShutdownStarted) return;
            dispatcher.InvokeAsync(() => ShowClipboardFallback(url));
        });
    }

    private static void ShowClipboardFallback(string url)
    {
        // Clipboard copy may itself fail on a session without an active
        // window station (Server Core, scheduled tasks, locked sessions).
        // Show the URL in the dialog body either way so the user has a
        // path to it.
        var clipboardOk = TryCopyToClipboard(url);
        var body = clipboardOk
            ? string.Format(Strings.BrowserLaunch_ClipboardOk, url)
            : string.Format(Strings.BrowserLaunch_ClipboardFailed, url);
        // Warning, not information: the user asked for a page and did not get
        // it, and the body carries the URL for them to paste.
        MessageDialog.Show(body, Strings.BrowserLaunch_FailedTitle, MessageKind.Warning);
    }

    private static bool TryCopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
