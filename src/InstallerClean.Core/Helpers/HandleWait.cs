using System.ComponentModel;
using System.Runtime.InteropServices;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// Waits on one kernel handle with a plain Windows wait, which dispatches nothing meanwhile:
/// on a window's thread no message sent to it and no COM call is handled while it waits,
/// whatever the thread's synchronisation context. A wait through
/// <see cref="WaitHandle.WaitOne(TimeSpan)"/> on such a thread goes through that context,
/// which can dispatch them.
/// </summary>
/// <remarks>
/// A window's thread waiting this way answers nothing for as long as it waits, and Windows
/// marks a window that has not answered for about five seconds as not responding, so a wait
/// on such a thread with a window showing has to be bounded well short of that. With no
/// window showing nothing is marked, though a message sent to a hidden window of the thread
/// still waits until the wait ends.
/// </remarks>
internal static class HandleWait
{
    /// <summary>
    /// Waits up to <paramref name="wait"/> for <paramref name="handle"/>, which
    /// <see cref="Timeout.InfiniteTimeSpan"/> makes a wait with no end, and answers how it
    /// ended. Throws <see cref="ObjectDisposedException"/> where the handle is closed or not
    /// a handle, <see cref="ArgumentOutOfRangeException"/> for a wait below -1 or over
    /// <see cref="int.MaxValue"/> milliseconds, as <see cref="WaitHandle.WaitOne(TimeSpan)"/>
    /// does, <see cref="Win32Exception"/> where Windows refuses the wait, and
    /// <see cref="InvalidOperationException"/> where it answers anything but signalled,
    /// abandoned or timed out.
    /// </summary>
    public static HandleWaitResult One(SafeHandle handle, TimeSpan wait)
    {
        var result = One(handle, Milliseconds(wait), out var error);
        if (result == Kernel32.WAIT_FAILED) throw new Win32Exception(error);
        return result switch
        {
            Kernel32.WAIT_OBJECT_0 => HandleWaitResult.Signalled,
            Kernel32.WAIT_ABANDONED_0 => HandleWaitResult.Abandoned,
            Kernel32.WAIT_TIMEOUT => HandleWaitResult.TimedOut,
            _ => throw new InvalidOperationException($"A wait on one handle answered 0x{result:X}."),
        };
    }

    /// <summary>
    /// Waits up to <paramref name="milliseconds"/> for <paramref name="handle"/> and answers
    /// what WaitForMultipleObjects answered, with the error code in <paramref name="error"/>
    /// where it refused the wait. The handle is kept open for the whole wait. Throws
    /// <see cref="ObjectDisposedException"/> where it is closed or not a handle.
    /// </summary>
    public static uint One(SafeHandle handle, uint milliseconds, out int error)
    {
        // A disposed Mutex hands out its closed handle, and one that never had a handle
        // hands out -1, which Windows reads as this process, so a wait on it would run to
        // its timeout, or for ever, rather than fail.
        if (handle.IsClosed || handle.IsInvalid) throw new ObjectDisposedException(nameof(handle));
        return Raw([handle], milliseconds, out error);
    }

    /// <summary>
    /// Waits up to <paramref name="wait"/> for any of <paramref name="handles"/>, as
    /// <see cref="One(SafeHandle, TimeSpan)"/> does for one, and answers the index of the
    /// handle that ended the wait with how it ended, or <see cref="HandleWaitResult.TimedOut"/>
    /// and -1. Where several are signalled together the lowest index is answered, and only
    /// that handle is acquired. Throws as <see cref="One(SafeHandle, TimeSpan)"/> does.
    /// </summary>
    public static (int Index, HandleWaitResult Result) Any(SafeHandle[] handles, TimeSpan wait)
    {
        var milliseconds = Milliseconds(wait);
        foreach (var handle in handles)
            if (handle.IsClosed || handle.IsInvalid) throw new ObjectDisposedException(nameof(handles));
        var result = Raw(handles, milliseconds, out var error);
        if (result == Kernel32.WAIT_FAILED) throw new Win32Exception(error);
        if (result == Kernel32.WAIT_TIMEOUT) return (-1, HandleWaitResult.TimedOut);
        if (result - Kernel32.WAIT_OBJECT_0 < (uint)handles.Length)
            return ((int)(result - Kernel32.WAIT_OBJECT_0), HandleWaitResult.Signalled);
        if (result - Kernel32.WAIT_ABANDONED_0 < (uint)handles.Length)
            return ((int)(result - Kernel32.WAIT_ABANDONED_0), HandleWaitResult.Abandoned);
        throw new InvalidOperationException($"A wait on {handles.Length} handles answered 0x{result:X}.");
    }

    /// <summary>
    /// Waits up to <paramref name="milliseconds"/> for any of <paramref name="handles"/> and
    /// answers what WaitForMultipleObjects answered, with the error code in
    /// <paramref name="error"/> where it refused the wait (0 for every other answer). Each
    /// handle is kept open for the whole wait.
    /// </summary>
    internal static uint Raw(IReadOnlyList<SafeHandle> handles, uint milliseconds, out int error)
    {
        error = 0;
        var raw = new IntPtr[handles.Count];
        var added = new bool[handles.Count];
        try
        {
            for (var i = 0; i < handles.Count; i++)
            {
                handles[i].DangerousAddRef(ref added[i]);
                raw[i] = handles[i].DangerousGetHandle();
            }
            var result = Kernel32.WaitForMultipleObjects((uint)raw.Length, raw, bWaitAll: false, milliseconds);
            if (result == Kernel32.WAIT_FAILED) error = Marshal.GetLastWin32Error();
            return result;
        }
        finally
        {
            for (var i = 0; i < handles.Count; i++)
                if (added[i]) handles[i].DangerousRelease();
        }
    }

    private static uint Milliseconds(TimeSpan wait)
    {
        var milliseconds = (long)wait.TotalMilliseconds;
        ArgumentOutOfRangeException.ThrowIfLessThan(milliseconds, -1, nameof(wait));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(milliseconds, int.MaxValue, nameof(wait));
        return milliseconds == -1 ? Kernel32.INFINITE : (uint)milliseconds;
    }
}

/// <summary>How a <see cref="HandleWait.One(SafeHandle, TimeSpan)"/> ended.</summary>
internal enum HandleWaitResult
{
    /// <summary>The handle was signalled; for a mutex, the waiting thread now owns it.</summary>
    Signalled,

    /// <summary>A mutex whose last owner ended without releasing it: the waiting thread now owns it.</summary>
    Abandoned,

    /// <summary>The wait ran out.</summary>
    TimedOut,
}
