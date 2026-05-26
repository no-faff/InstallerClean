using System.Runtime.InteropServices;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// Opens URLs at the desktop shell's IL. An elevated parent that calls
/// <c>Process.Start</c> with <c>UseShellExecute=true</c> spawns the
/// browser elevated, which leaves the user without their normal cookies
/// and turns any phishing page typed into the elevated session into a
/// privilege-amplification path. The shell-token chain duplicates the
/// desktop shell's primary token and runs <c>rundll32 url.dll,
/// FileProtocolHandler</c> under it, so the URL opens in the user's
/// normal-IL browser.
/// </summary>
internal static class UnelevatedLauncher
{
    /// <summary>
    /// Result of an <see cref="OpenUrl"/> attempt. <see cref="Launched"/>
    /// is true when the unelevated browser was spawned; otherwise
    /// <see cref="FailureReason"/> describes what failed in the token
    /// chain. Callers fall back to a copy-to-clipboard prompt rather
    /// than launching elevated.
    /// </summary>
    public readonly record struct OpenUrlResult(bool Launched, string FailureReason);

    /// <summary>
    /// Opens <paramref name="url"/> in the user's default browser at
    /// medium IL. Returns a result rather than throwing or falling back
    /// to an elevated launch: the calling host decides what to do when
    /// the unelevated route is unavailable.
    /// </summary>
    public static OpenUrlResult OpenUrl(string url)
    {
        try
        {
            if (TryUnelevatedLaunch(url, out var failureReason))
                return new OpenUrlResult(true, string.Empty);
            CrashLog.Write(new InvalidOperationException(
                "UnelevatedLauncher could not spawn an unelevated browser: " + failureReason));
            return new OpenUrlResult(false, failureReason);
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
            return new OpenUrlResult(false, ex.GetType().Name);
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

        // CreateProcessWithTokenW documents three required rights on
        // the primary token: TOKEN_QUERY | TOKEN_DUPLICATE |
        // TOKEN_ASSIGN_PRIMARY. Requesting MAXIMUM_ALLOWED hands back
        // a token with every right the caller is permitted on the
        // shell's token, which is wider than this call needs.
        const uint createProcessWithTokenRights =
            Advapi32.TOKEN_QUERY | Advapi32.TOKEN_DUPLICATE | Advapi32.TOKEN_ASSIGN_PRIMARY;
        if (!Advapi32.DuplicateTokenEx(
                shellToken,
                createProcessWithTokenRights,
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
