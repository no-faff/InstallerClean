using System.Diagnostics;
using System.Runtime.InteropServices;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Reads and closes the console window another process is attached to, through a Windows
/// PowerShell helper of its own: a process can be attached to one console only, so the test
/// process leaves its own alone and the helper detaches from its console and attaches to the
/// other.
/// </summary>
internal static class OtherConsole
{
    private static readonly string PowerShell =
        Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");

    // Windows PowerShell 5.1 compiles C# 5, so the type below stays at C# 5.
    private const string Script = """
        param([string]$Mode, [int]$ProcessId, [string]$Library)
        $ErrorActionPreference = 'Stop'
        if (Test-Path -LiteralPath $Library) { Add-Type -Path $Library; $compiled = $true } else { $compiled = $false }
        if (-not $compiled) {
        Add-Type -OutputAssembly $Library -TypeDefinition @'
        using System;
        using System.Runtime.InteropServices;
        using System.Text;
        public static class IcOtherConsole
        {
            [StructLayout(LayoutKind.Sequential)] struct Coord { public short X; public short Y; }
            [StructLayout(LayoutKind.Sequential)] struct SmallRect { public short Left; public short Top; public short Right; public short Bottom; }
            [StructLayout(LayoutKind.Sequential)] struct BufferInfo { public Coord Size; public Coord Cursor; public short Attributes; public SmallRect Window; public Coord MaxSize; }
            [DllImport("kernel32.dll", SetLastError = true)] static extern bool FreeConsole();
            [DllImport("kernel32.dll", SetLastError = true)] static extern bool AttachConsole(uint processId);
            [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
            [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
            [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetConsoleScreenBufferInfo(IntPtr output, out BufferInfo info);
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] static extern bool ReadConsoleOutputCharacterW(IntPtr output, StringBuilder text, uint length, Coord origin, out uint read);
            [DllImport("user32.dll", SetLastError = true)] static extern bool PostMessageW(IntPtr window, uint message, IntPtr w, IntPtr l);
            [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassNameW(IntPtr window, StringBuilder name, int size);

            public static string Run(string mode, int processId)
            {
                FreeConsole();
                if (!AttachConsole((uint)processId)) return "!attach " + Marshal.GetLastWin32Error();
                try
                {
                    if (mode == "window" || mode == "close")
                    {
                        IntPtr window = GetConsoleWindow();
                        if (window == IntPtr.Zero) return "!nowindow";
                        StringBuilder cls = new StringBuilder(256);
                        GetClassNameW(window, cls, 256);
                        if (mode == "window") return cls.ToString();
                        // Detached first, so the close this posts is not also sent to this helper.
                        FreeConsole();
                        // WM_CLOSE, as the window's Close button sends it.
                        return PostMessageW(window, 0x0010, IntPtr.Zero, IntPtr.Zero) ? cls.ToString() : "!post " + Marshal.GetLastWin32Error();
                    }
                    IntPtr output = CreateFileW("CONOUT$", 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
                    if (output == new IntPtr(-1)) return "!conout " + Marshal.GetLastWin32Error();
                    try
                    {
                        BufferInfo info;
                        if (!GetConsoleScreenBufferInfo(output, out info)) return "!buffer " + Marshal.GetLastWin32Error();
                        StringBuilder all = new StringBuilder();
                        for (int row = 0; row <= info.Cursor.Y; row++)
                        {
                            StringBuilder line = new StringBuilder(info.Size.X);
                            Coord origin = new Coord();
                            origin.Y = (short)row;
                            uint read;
                            if (!ReadConsoleOutputCharacterW(output, line, (uint)info.Size.X, origin, out read)) break;
                            all.AppendLine(line.ToString(0, (int)read).TrimEnd());
                        }
                        return all.ToString();
                    }
                    finally { CloseHandle(output); }
                }
                finally { FreeConsole(); }
            }
        }
        '@
        Add-Type -Path $Library
        }
        [Console]::OutputEncoding = [Text.Encoding]::UTF8
        [Console]::Out.Write([IcOtherConsole]::Run($Mode, $ProcessId))
        """;

    /// <summary>
    /// The script and the library its first run compiles the type into, which later runs load
    /// rather than compile again, in a folder of this run's own.
    /// </summary>
    private static readonly Lazy<(string Script, string Library)> Files = new(() =>
    {
        var folder = TestTemp.NewFolder("ic-other-console-");
        var script = Path.Combine(folder, "OtherConsole.ps1");
        File.WriteAllText(script, Script);
        return (script, Path.Combine(folder, "OtherConsole.dll"));
    });

    /// <summary>
    /// What the helper answers for <paramref name="mode"/>: the text on the console's screen
    /// for "read", the console window's class for "window" and for "close" (which posts
    /// WM_CLOSE to it), or a line starting with "!" naming what failed: a Windows call, or
    /// the helper itself, where it ends with an error, answers nothing or does not end.
    /// </summary>
    private static string Run(string mode, int processId)
    {
        var info = new ProcessStartInfo(PowerShell)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
        };
        var (script, library) = Files.Value;
        info.Arguments = CommandLineArguments.Join(new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script, mode, processId.ToString(), library });
        using var helper = Process.Start(info)!;
        var output = helper.StandardOutput.ReadToEndAsync();
        var error = helper.StandardError.ReadToEndAsync();
        if (!helper.WaitForExit(60_000))
        {
            try { helper.Kill(); } catch { }
            return "!helper did not end in 60 seconds";
        }
        var answer = output.Result;
        if (helper.ExitCode != 0 || answer.Length == 0)
            return $"!helper ended with {helper.ExitCode}: {error.Result.Trim()}";
        return answer;
    }

    /// <summary>Whether <paramref name="answer"/> says the helper failed rather than answered.</summary>
    public static bool Failed(string answer) => answer.StartsWith('!');

    public static string Read(int processId) => Run("read", processId);

    public static string WindowClass(int processId) => Run("window", processId);

    public static string Close(int processId) => Run("close", processId);

    /// <summary>
    /// Starts <paramref name="file"/> under a conhost.exe of its own, with this process's
    /// environment, and answers the program and the conhost. conhost.exe started with a command
    /// line opens a console window of its own for it, where a program started through the shell
    /// can be given a pseudo-console window instead. Answers no program where conhost.exe started none in 30
    /// seconds, with what it found in <paramref name="why"/>.
    /// </summary>
    public static (Process? Program, Process Host) StartInOwnConsole(string file, string arguments, out string why)
    {
        var host = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "conhost.exe"), $"\"{file}\" {arguments}")
        {
            UseShellExecute = true,
        })!;
        var name = Path.GetFileName(file);
        var hostStarted = host.StartTime;
        var seen = "";
        var snapshotFailure = "";
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < TimeSpan.FromSeconds(30))
        {
            var processes = Processes(out var snapshotError);
            if (processes is null) snapshotFailure = $"; the process snapshot failed with error {snapshotError}";
            foreach (var (id, parent, exe) in processes ?? [])
            {
                if (parent != host.Id || !string.Equals(exe, name, StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    var program = Process.GetProcessById(id);
                    // A process whose parent's id was given to this conhost after it ended is older than it.
                    if (program.StartTime < hostStarted) { program.Dispose(); continue; }
                    // Opened now, so the exit code can be read after it ends.
                    _ = program.Handle;
                    why = "";
                    return (program, host);
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    seen = $"; a {name} with id {id} was seen under it and had ended before it could be opened";
                }
            }
            if (host.HasExited) break;
            Thread.Sleep(100);
        }
        why = (host.HasExited ? $"conhost.exe ended with {host.ExitCode} with no {name} under it open" : $"conhost.exe had no {name} under it in 30 seconds")
            + seen + snapshotFailure;
        return (null, host);
    }

    /// <summary>Ends <paramref name="process"/> where it is still running. Never throws.</summary>
    public static void End(Process? process)
    {
        try { if (process is { HasExited: false }) process.Kill(); } catch { }
        process?.Dispose();
    }

    /// <summary>
    /// Every running process's id, its parent's id and its exe's name, from a Toolhelp snapshot,
    /// or null with the Windows error where the snapshot could not be taken or walked to its end.
    /// </summary>
    private static List<(int Id, int Parent, string Exe)>? Processes(out int error)
    {
        error = 0;
        var found = new List<(int, int, string)>();
        var snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snapshot == new IntPtr(-1)) { error = Marshal.GetLastWin32Error(); return null; }
        try
        {
            var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
            for (var more = Process32FirstW(snapshot, ref entry); more; more = Process32NextW(snapshot, ref entry))
                found.Add(((int)entry.ProcessId, (int)entry.ParentProcessId, entry.ExeFile));
            error = Marshal.GetLastWin32Error();
            if (error != ERROR_NO_MORE_FILES) return null;
            error = 0;
        }
        finally
        {
            CloseHandle(snapshot);
        }
        return found;
    }

    private const uint TH32CS_SNAPPROCESS = 0x00000002;
    private const int ERROR_NO_MORE_FILES = 18;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public IntPtr DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int PriorityClassBase;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string ExeFile;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool Process32FirstW(IntPtr snapshot, ref ProcessEntry entry);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool Process32NextW(IntPtr snapshot, ref ProcessEntry entry);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);
}
