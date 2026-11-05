using System.ComponentModel;
using System.Runtime.InteropServices;
using InstallerClean.Helpers;

namespace InstallerClean.Interop.Native;

/// <summary>
/// Hands the console control events to one managed handler, which answers whether it took
/// the event. An event it does not take goes on to the routines registered before it, the
/// runtime's own for Ctrl+C and Ctrl+Break among them.
/// </summary>
internal static class ConsoleControl
{
    private static Func<uint, bool>? s_handler;
    private static int s_registered;

    // The routine Windows calls. Windows holds a pointer to it from registration until
    // the process ends, so it is kept in a static field, which the garbage collector never
    // collects; a delegate made at the call and dropped after it would be freed while
    // Windows still calls it.
    private static readonly Kernel32.HandlerRoutine s_routine = Routine;

    /// <summary>
    /// Registers <paramref name="handler"/> for the life of the process. Answers false, with
    /// the reason in crash.log, where Windows refuses it, and false where a handler is
    /// already registered, which stays the one called. Never throws.
    /// </summary>
    public static bool TryRegister(Func<uint, bool> handler)
    {
        if (Interlocked.Exchange(ref s_registered, 1) == 1) return false;
        try
        {
            s_handler = handler;
            if (Kernel32.SetConsoleCtrlHandler(s_routine, add: true)) return true;
            CrashLog.TryWrite(new Win32Exception(Marshal.GetLastWin32Error()));
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
        }
        return false;
    }

    // Called by Windows, where an exception would end the process, so nothing escapes it.
    private static int Routine(uint ctrlType)
    {
        try
        {
            return s_handler?.Invoke(ctrlType) == true ? 1 : 0;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return 0;
        }
    }
}
