using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// Opens URLs at the desktop shell's IL. An elevated parent would
/// otherwise spawn the browser elevated, so each Donate / Star /
/// Updates click would open a separate Admin browser session with
/// no cookies.
/// </summary>
internal static class UnelevatedLauncher
{
    /// <summary>
    /// Opens <paramref name="url"/> in the user's default browser at
    /// medium IL. Falls back to elevated Process.Start if the shell-token
    /// route fails. Errors log and swallow.
    /// </summary>
    public static void OpenUrl(string url)
    {
        try
        {
            if (TryUnelevatedLaunch(url))
                return;
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
        }
    }

    private static bool TryUnelevatedLaunch(string url)
    {
        var shellWindow = User32.GetShellWindow();
        if (shellWindow == IntPtr.Zero) return false;

        User32.GetWindowThreadProcessId(shellWindow, out var shellPid);
        if (shellPid == 0) return false;

        using var shellProcess = Kernel32.OpenProcess(
            Kernel32.PROCESS_QUERY_INFORMATION, inheritHandle: false, shellPid);
        if (shellProcess.IsInvalid) return false;

        if (!Advapi32.OpenProcessToken(shellProcess,
                Advapi32.TOKEN_DUPLICATE | Advapi32.TOKEN_QUERY,
                out var shellTokenRaw))
            return false;

        using var shellToken = shellTokenRaw;

        if (!Advapi32.DuplicateTokenEx(
                shellToken,
                Advapi32.MAXIMUM_ALLOWED,
                IntPtr.Zero,
                Advapi32.SecurityImpersonationLevel.SecurityImpersonation,
                Advapi32.TokenType.TokenPrimary,
                out var primaryTokenRaw))
            return false;

        using var primaryToken = primaryTokenRaw;

        // rundll32 url.dll,FileProtocolHandler is the canonical
        // "open URL with the default handler" shell entry.
        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var rundll32 = Path.Combine(system32, "rundll32.exe");
        // Quote the URL: defensive against & / | in command-line parsing.
        var commandLine = $"\"{rundll32}\" url.dll,FileProtocolHandler \"{url}\"";

        var si = new Advapi32.STARTUPINFO
        {
            cb = (uint)Marshal.SizeOf<Advapi32.STARTUPINFO>(),
        };

        if (!Advapi32.CreateProcessWithTokenW(
                primaryToken,
                logonFlags: 0,
                applicationName: rundll32,
                commandLine: commandLine,
                creationFlags: 0,
                environment: IntPtr.Zero,
                currentDirectory: null,
                startupInfo: ref si,
                processInformation: out var pi))
            return false;

        // Close the returned handles; otherwise the kernel objects leak.
        if (pi.hProcess != IntPtr.Zero)
            new SafeProcessHandle(pi.hProcess, ownsHandle: true).Dispose();
        if (pi.hThread != IntPtr.Zero)
            new SafeFileHandle(pi.hThread, ownsHandle: true).Dispose();

        return true;
    }
}
