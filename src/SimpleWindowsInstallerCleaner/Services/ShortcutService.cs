using System.Diagnostics;
using System.IO;

namespace SimpleWindowsInstallerCleaner.Services;

public static class ShortcutService
{
    private const string AppName = "InstallerClean";

    public static void CreateStartMenuShortcut()
    {
        var startMenuFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            AppName + ".lnk");

        CreateShortcut(startMenuFolder);
    }

    public static void CreateDesktopShortcut()
    {
        var desktopFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            AppName + ".lnk");

        CreateShortcut(desktopFolder);
    }

    private static void CreateShortcut(string shortcutPath)
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath)) return;

        // Use PowerShell and WScript.Shell COM to create the .lnk
        var script = $"""
            $ws = New-Object -ComObject WScript.Shell
            $sc = $ws.CreateShortcut('{shortcutPath.Replace("'", "''")}')
            $sc.TargetPath = '{exePath.Replace("'", "''")}'
            $sc.WorkingDirectory = '{Path.GetDirectoryName(exePath)!.Replace("'", "''")}'
            $sc.Description = 'Clean orphaned files from C:\Windows\Installer'
            $sc.Save()
            """;

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -Command \"{script.Replace("\"", "\\\"")}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };

        try
        {
            using var process = Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch
        {
            // Shortcut creation is best-effort
        }
    }
}
