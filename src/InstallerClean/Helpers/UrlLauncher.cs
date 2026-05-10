using System.Windows;
using InstallerClean.Helpers;
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
    public static void OpenUrl(string url)
    {
        var result = UnelevatedLauncher.OpenUrl(url);
        if (result.Launched) return;

        // Clipboard copy may itself fail on a session without an active
        // window station (Server Core, scheduled tasks, locked sessions).
        // Show the URL in the dialog body either way so the user has a
        // path to it.
        var clipboardOk = TryCopyToClipboard(url);
        var body = clipboardOk
            ? string.Format(Strings.BrowserLaunchFailedClipboardOk, url)
            : string.Format(Strings.BrowserLaunchFailedClipboardFailed, url);
        MessageBox.Show(
            body,
            Strings.BrowserLaunchFailedTitle,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
