using System.Runtime.InteropServices;
using System.Threading;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace System;

/// <summary>
/// The two <see cref="Environment"/> members .NET Framework 4.8 does not have, each answering
/// as the .NET 10 member of the same name does, so a call such as
/// <c>Environment.ProcessPath</c> binds here unchanged.
/// </summary>
internal static class FrameworkEnvironment
{
    private static volatile int s_processId;
    private static volatile string? s_processPath;

    extension(Environment)
    {
        /// <summary>The id Windows gives this process, read once and kept.</summary>
        public static int ProcessId
        {
            get
            {
                var processId = s_processId;
                if (processId == 0)
                    s_processId = processId = unchecked((int)Kernel32.GetCurrentProcessId());
                return processId;
            }
        }

        /// <summary>
        /// The full path of this process's exe, as Windows reports the file it was loaded
        /// from, read once and kept for the life of the process, so every read gives the same
        /// path. Null where there is no path, never empty.
        /// </summary>
        public static string? ProcessPath
        {
            get
            {
                var processPath = s_processPath;
                if (processPath is null)
                {
                    Interlocked.CompareExchange(ref s_processPath, ReadProcessPath(), null);
                    processPath = s_processPath!;
                }

                return processPath.Length != 0 ? processPath : null;
            }
        }
    }

    private static string ReadProcessPath() =>
        ReadModulePath(
            buffer => Kernel32.GetModuleFileName(IntPtr.Zero, buffer, (uint)buffer.Length),
            Marshal.GetLastWin32Error);

    /// <summary>
    /// Test seam: the exe's path from <paramref name="getModuleFileName"/>, which writes it
    /// into the buffer it is given and returns its length as GetModuleFileNameW does. The
    /// buffer starts at 260 characters, and while the return fills it the next is twice the
    /// size rounded up to a power of two (1024, 2048 and on), the sizes .NET 10's pooled
    /// buffer comes in, so a path is never cut short. A return of 0 throws the exception for
    /// the error <paramref name="lastError"/> answers.
    /// </summary>
    internal static string ReadModulePath(Func<char[], uint> getModuleFileName, Func<int> lastError)
    {
        var buffer = new char[260];

        uint length;
        while ((length = getModuleFileName(buffer)) >= buffer.Length)
            buffer = new char[NextPowerOfTwo(buffer.Length * 2)];

        if (length == 0)
            throw Win32Marshal.GetExceptionForWin32Error(lastError());

        return new string(buffer, 0, (int)length);
    }

    private static int NextPowerOfTwo(int value)
    {
        var size = 16;
        while (size < value) size <<= 1;
        return size;
    }
}
