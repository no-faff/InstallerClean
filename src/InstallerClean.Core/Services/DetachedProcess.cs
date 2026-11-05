using System.ComponentModel;
using System.Text;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Services;

/// <summary>
/// Starts a program that outlives the process starting it and holds nothing of that
/// process's: no console, no Ctrl+C, no standard handle and no other inherited handle,
/// and where the job that process is in allows it, outside that job.
/// </summary>
/// <remarks>
/// A program started with standard handles holds them for as long as it runs. Task
/// Scheduler and RMM tools read a task's output through a pipe and count the task as running
/// until every handle to that pipe is closed, so a program that holds one keeps a finished
/// task waiting on it. A program sharing the console is ended with it and gets its Ctrl+C. A
/// program in the caller's job ends where that job is ended. So the start asks for
/// none of the three: handles are not inherited, no standard handles are named, the program
/// is started detached from the console, and it asks to leave the job, which Windows refuses
/// with access denied where the job does not allow it, and then it is started inside the job.
/// </remarks>
internal static class DetachedProcess
{
    /// <summary>
    /// Starts <paramref name="file"/> with <paramref name="arguments"/>, working in
    /// <paramref name="workingDirectory"/>, and answers its process handle, or null where
    /// every try failed. A start that fails is tried <see cref="ProcessStartRetry.Attempts"/>
    /// times, <see cref="ProcessStartRetry.Interval"/> apart, and each failure is written to
    /// crash.log. Never throws.
    /// </summary>
    public static SafeProcessHandle? Start(string file, IReadOnlyList<string> arguments, string workingDirectory) =>
        Start(file, arguments, workingDirectory, CreateProcess, Thread.Sleep);

    /// <summary>
    /// One call to start a program: true with the started process's handles, or false with
    /// the error Windows gave.
    /// </summary>
    internal delegate bool StartCall(
        string file, char[] commandLine, uint flags, string workingDirectory,
        out Kernel32.PROCESS_INFORMATION started, out int error);

    /// <summary>
    /// Test seam: <see cref="Start(string, IReadOnlyList{string}, string)"/> with
    /// <paramref name="start"/> in place of CreateProcess and <paramref name="sleep"/>
    /// between tries.
    /// </summary>
    internal static SafeProcessHandle? Start(
        string file, IReadOnlyList<string> arguments, string workingDirectory, StartCall start, Action<TimeSpan> sleep)
    {
        try
        {
            var commandLine = CommandLine(file, arguments);
            for (var attempt = 1; ; attempt++)
            {
                // Leaving the job is asked for on every try, and a refusal with access denied
                // is answered by a start inside it at once. Access denied can also be an
                // antivirus holding a program it has just seen written, which the next try
                // waits out, so one refusal does not stop the next try asking again.
                var flags = Kernel32.DETACHED_PROCESS | Kernel32.CREATE_BREAKAWAY_FROM_JOB;
                if (!Try(flags, out var started, out var error) && error == Kernel32.ERROR_ACCESS_DENIED)
                    Try(Kernel32.DETACHED_PROCESS, out started, out error);
                if (started is not null) return started;
                CrashLog.TryWrite(new Win32Exception(error, $"Starting {file} failed with error {error}."));
                if (attempt >= ProcessStartRetry.Attempts) return null;
                sleep(ProcessStartRetry.Interval);
            }

            bool Try(uint flags, out SafeProcessHandle? started, out int error)
            {
                // CreateProcess may write into the command line it is given, so each try has its own.
                if (!start(file, (char[])commandLine.Clone(), flags, workingDirectory, out var info, out error))
                {
                    started = null;
                    return false;
                }
                new SafeWaitHandle(info.hThread, ownsHandle: true).Dispose();
                started = new SafeProcessHandle(info.hProcess, ownsHandle: true);
                return true;
            }
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return null;
        }
    }

    private static bool CreateProcess(
        string file, char[] commandLine, uint flags, string workingDirectory,
        out Kernel32.PROCESS_INFORMATION started, out int error)
    {
        var startup = new Kernel32.STARTUPINFO { cb = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Kernel32.STARTUPINFO>() };
        if (Kernel32.CreateProcess(file, commandLine, IntPtr.Zero, IntPtr.Zero, bInheritHandles: false, flags,
                IntPtr.Zero, workingDirectory, in startup, out started))
        {
            error = 0;
            return true;
        }
        error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
        return false;
    }

    /// <summary>
    /// The command line that starts <paramref name="file"/> with <paramref name="arguments"/>,
    /// ending in a null: the file's path in quotes, then each argument quoted where it is
    /// empty or holds white space or a quote, so the program reads each back as it was given,
    /// by the rules CommandLineToArgvW and the C runtime read a command line with.
    /// </summary>
    internal static char[] CommandLine(string file, IReadOnlyList<string> arguments)
    {
        var line = new StringBuilder().Append('"').Append(file).Append('"');
        foreach (var argument in arguments)
        {
            line.Append(' ');
            AppendArgument(line, argument);
        }
        return line.Append('\0').ToString().ToCharArray();
    }

    /// <summary>
    /// Appends <paramref name="argument"/> as one argument. Inside quotes, a run of
    /// backslashes is doubled where a quote or the closing quote follows it, and a quote is
    /// written as a backslash and the quote; any other backslash stands for itself.
    /// </summary>
    private static void AppendArgument(StringBuilder line, string argument)
    {
        if (argument.Length > 0 && !argument.Any(c => char.IsWhiteSpace(c) || c == '"'))
        {
            line.Append(argument);
            return;
        }
        line.Append('"');
        var i = 0;
        while (i < argument.Length)
        {
            var backslashes = 0;
            while (i < argument.Length && argument[i] == '\\')
            {
                backslashes++;
                i++;
            }
            if (i == argument.Length)
            {
                line.Append('\\', backslashes * 2);
                break;
            }
            if (argument[i] == '"')
                line.Append('\\', backslashes * 2 + 1).Append('"');
            else
                line.Append('\\', backslashes).Append(argument[i]);
            i++;
        }
        line.Append('"');
    }
}
