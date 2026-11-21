using System.Diagnostics;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// A console program standing in for a program started with no console
/// (<see cref="InstallerClean.Services.DetachedProcess"/>), as the command line's replace
/// is: compiled once per test run by Windows PowerShell, since PowerShell itself ends at
/// once when started without a console. It is a console-subsystem exe, like the command
/// line.
/// </summary>
/// <remarks>
/// Its first three arguments are a folder, what to do and the started event's name less
/// its ID (<see cref="EventPrefix"/>); every argument after them is the program's own. It
/// writes those arguments to arguments.txt in the folder, one to a line, its working folder
/// to folder.txt and its process ID to pid.txt, each under another name first and then
/// renamed, so a file read as soon as it is there is whole. Then, for "set", it sets the
/// started event named by its <see cref="HandOverContract.StartedEventSwitch"/> argument and
/// waits a minute; for "wait" it waits a minute; for "exit" it ends with exit code 1.
/// </remarks>
internal static class DetachedStandIn
{
    private static readonly string PowerShell =
        Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");

    /// <summary>The started event's name with no ID after it.</summary>
    public static string EventPrefix => HandOverContract.StartedEventName(Guid.Empty)[..^32];

    // Windows PowerShell 5.1 compiles C# 5, so the type below stays at C# 5.
    private static readonly string Source = $$"""
        using System;
        using System.Diagnostics;
        using System.IO;
        using System.Threading;
        public static class IcDetachedStandIn
        {
            static void Write(string folder, string name, string[] lines)
            {
                string writing = Path.Combine(folder, name + ".writing");
                File.WriteAllLines(writing, lines);
                File.Move(writing, Path.Combine(folder, name));
            }

            public static int Main(string[] args)
            {
                string folder = args[0];
                string mode = args[1];
                string prefix = args[2];
                string[] own = new string[args.Length - 3];
                Array.Copy(args, 3, own, 0, own.Length);
                Write(folder, "arguments.txt", own);
                Write(folder, "folder.txt", new string[] { Environment.CurrentDirectory });
                Write(folder, "pid.txt", new string[] { Process.GetCurrentProcess().Id.ToString() });
                if (mode == "exit") return 1;
                if (mode == "set")
                {
                    foreach (string argument in own)
                    {
                        if (argument.StartsWith("{{HandOverContract.StartedEventSwitch}}", StringComparison.OrdinalIgnoreCase))
                            EventWaitHandle.OpenExisting(prefix + argument.Substring({{HandOverContract.StartedEventSwitch.Length}})).Set();
                    }
                }
                Thread.Sleep(60000);
                return 0;
            }
        }
        """;

    private static readonly Lazy<string> Compiled = new(() =>
    {
        var folder = TestTemp.NewFolder("ic-detached-stand-in-");
        var source = Path.Combine(folder, "StandIn.cs");
        var exe = Path.Combine(folder, "stand-in.exe");
        File.WriteAllText(source, Source);

        var info = new ProcessStartInfo(PowerShell)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.Arguments = CommandLineArguments.Join(new[]
        {
            // -Command takes the rest of the line as the command, so the paths go inside it,
            // each in single quotes with any quote in it doubled.
            "-NoProfile", "-NonInteractive", "-Command",
            $"$ErrorActionPreference = 'Stop'; Add-Type -OutputType ConsoleApplication -OutputAssembly {Quoted(exe)} -TypeDefinition ([IO.File]::ReadAllText({Quoted(source)}))",
        });
        using var compiler = Process.Start(info)!;
        var output = compiler.StandardOutput.ReadToEndAsync();
        var error = compiler.StandardError.ReadToEndAsync();
        if (!compiler.WaitForExit(120_000))
        {
            try { compiler.Kill(); } catch { }
            throw new InvalidOperationException("Compiling the stand-in did not end in 120 seconds.");
        }
        if (compiler.ExitCode != 0 || !File.Exists(exe))
            throw new InvalidOperationException($"Compiling the stand-in ended with {compiler.ExitCode}: {output.Result.Trim()} {error.Result.Trim()}");
        return exe;
    });

    /// <summary>
    /// <paramref name="text"/> as a PowerShell string that takes it literally: in single
    /// quotes, with each character PowerShell reads as a single quote doubled, the curly
    /// ones included.
    /// </summary>
    private static string Quoted(string text) =>
        "'" + System.Text.RegularExpressions.Regex.Replace(text, "['\u2018\u2019\u201A\u201B]", "$0$0") + "'";

    /// <summary>The stand-in's exe, compiled on first use.</summary>
    public static string Exe => Compiled.Value;

    /// <summary>The three arguments that go before the program's own: where to write, what to do, and the event's prefix.</summary>
    public static string[] Before(string folder, string mode) => [folder, mode, EventPrefix];
}
