using System.Diagnostics;
using System.Runtime.InteropServices;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// Opens URLs at the desktop shell's IL. An elevated parent would
/// otherwise spawn the browser elevated, so each Donate / Star /
/// Updates click would open a separate Admin browser session with
/// no cookies. Falls back to elevated <c>Process.Start</c> if the
/// shell-token chain fails, logging the reason to crash.log.
/// </summary>
internal static class UnelevatedLauncher
{
    /// <summary>
    /// Opens <paramref name="url"/> in the user's default browser at
    /// medium IL. Falls back to elevated Process.Start if the
    /// shell-token route fails. Errors log and swallow.
    /// </summary>
    public static void OpenUrl(string url)
    {
        try
        {
            if (TryUnelevatedLaunch(url, out var failureReason))
                return;
            CrashLog.Write(new InvalidOperationException(
                "UnelevatedLauncher fell back to elevated Process.Start: " + failureReason));
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

    private static bool TryUnelevatedLaunch(string url, out string failureReason)
    {
        failureReason = string.Empty;

        var shellWindow = User32.GetShellWindow();
        if (shellWindow == IntPtr.Zero)
        {
            failureReason = "GetShellWindow returned 0";
            return false;
        }

        User32.GetWindowThreadProcessId(shellWindow, out var shellPid);
        if (shellPid == 0)
        {
            failureReason = "GetWindowThreadProcessId returned PID 0";
            return false;
        }

        using var shellProcess = Kernel32.OpenProcess(
            Kernel32.PROCESS_QUERY_INFORMATION, inheritHandle: false, shellPid);
        if (shellProcess.IsInvalid)
        {
            failureReason = $"OpenProcess(pid={shellPid}) failed, error {Marshal.GetLastWin32Error()}";
            return false;
        }

        if (!Advapi32.OpenProcessToken(shellProcess,
                Advapi32.TOKEN_DUPLICATE | Advapi32.TOKEN_QUERY,
                out var shellTokenRaw))
        {
            failureReason = $"OpenProcessToken failed, error {Marshal.GetLastWin32Error()}";
            return false;
        }
        using var shellToken = shellTokenRaw;

        if (!Advapi32.DuplicateTokenEx(
                shellToken,
                Advapi32.MAXIMUM_ALLOWED,
                IntPtr.Zero,
                Advapi32.SecurityImpersonationLevel.SecurityImpersonation,
                Advapi32.TokenType.TokenPrimary,
                out var primaryTokenRaw))
        {
            failureReason = $"DuplicateTokenEx failed, error {Marshal.GetLastWin32Error()}";
            return false;
        }
        using var primaryToken = primaryTokenRaw;

        // rundll32 url.dll,FileProtocolHandler is the canonical
        // "open URL with the default handler" shell entry.
        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var rundll32 = Path.Combine(system32, "rundll32.exe");
        // %-encode embedded `"` so the outer quoting can't be closed
        // early by a URL containing one.
        var safeUrl = url.Replace("\"", "%22");
        var commandLine = $"\"{rundll32}\" url.dll,FileProtocolHandler \"{safeUrl}\"";

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
        {
            failureReason = $"CreateProcessWithTokenW failed, error {Marshal.GetLastWin32Error()}";
            return false;
        }

        // Close the returned handles or the kernel objects leak.
        if (pi.hProcess != IntPtr.Zero) Kernel32.CloseHandle(pi.hProcess);
        if (pi.hThread != IntPtr.Zero) Kernel32.CloseHandle(pi.hThread);

        return true;
    }
}
