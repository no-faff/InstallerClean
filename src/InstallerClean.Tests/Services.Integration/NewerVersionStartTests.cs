using System.Diagnostics;
using InstallerClean.Helpers;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The wait a copy updating itself makes for the newer version it started to say its own
/// code is running (<see cref="NewerVersionProcess.AwaitStart(Process, WaitHandle, TimeSpan, WaitHandle?, out int)"/>), and the ending of a newer
/// version that did not say so, against real processes standing in for it: PowerShell
/// setting the event or sleeping, and programs of Windows that end at once. Also the start
/// of a newer version from the folder it was downloaded to, to put itself at a copy's path
/// (<see cref="NewerVersionProcess.StartToReplace(string, string)"/>).
/// </summary>
public sealed class NewerVersionStartTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("ic-start-").FullName;
    private readonly List<Process> _started = [];
    private readonly IDisposable _crashLog = TestCrashLog.OwnFolder();

    public void Dispose()
    {
        try
        {
            foreach (var process in _started)
            {
                try { if (!process.HasExited) process.Kill(); } catch { }
                process.Dispose();
            }
            try { Directory.Delete(_root, recursive: true); } catch { }
        }
        finally
        {
            _crashLog.Dispose();
        }
    }

    private static readonly string PowerShell =
        Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");

    private Process Start(string file, params string[] arguments)
    {
        var info = new ProcessStartInfo(file) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        var process = Process.Start(info)!;
        _started.Add(process);
        return process;
    }

    private Process PowerShellRunning(string script) =>
        Start(PowerShell, "-NoProfile", "-NonInteractive", "-Command", script);

    private static string Sets(Guid id) =>
        $"[System.Threading.EventWaitHandle]::OpenExisting('{HandOverContract.StartedEventName(id)}').Set() | Out-Null";

    private readonly Dictionary<EventWaitHandle, Guid> _eventIds = [];

    private (Guid Id, EventWaitHandle Event) NewEvent()
    {
        var id = Guid.NewGuid();
        var started = HandOverContract.CreateStartedEvent(id)!;
        _eventIds[started] = id;
        return (id, started);
    }

    private string StartedEventNameOf(EventWaitHandle started) => HandOverContract.StartedEventName(_eventIds[started]);

    private const uint EventModifyState = 0x0002;
    private const uint EventAllAccess = 0x1F0003;
    private const int ErrorAccessDenied = 5;

    [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "OpenEventW",
        CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern Microsoft.Win32.SafeHandles.SafeWaitHandle OpenEvent(
        uint dwDesiredAccess, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)] bool bInheritHandle,
        string lpName);

    [Fact]
    public void A_newer_version_that_sets_the_event_is_answered_as_started_and_left_running()
    {
        var (id, started) = NewEvent();
        using var _ = started;
        var newer = PowerShellRunning(Sets(id) + "; Start-Sleep -Seconds 60");

        var answer = NewerVersionProcess.AwaitStart(newer, started, TimeSpan.FromSeconds(60), cancel: null, out var waitError);

        Assert.Equal(NewerVersionStart.Started, answer);
        Assert.Equal(0, waitError);
        Assert.False(newer.HasExited);
    }

    [Fact]
    public void A_newer_version_that_set_the_event_and_has_ended_is_answered_as_started()
    {
        var (id, started) = NewEvent();
        using var _ = started;
        var newer = PowerShellRunning(Sets(id));
        Assert.True(newer.WaitForExit(TimeSpan.FromSeconds(60)));

        var answer = NewerVersionProcess.AwaitStart(newer, started, TimeSpan.FromSeconds(60), cancel: null, out var waitError);

        Assert.Equal(NewerVersionStart.Started, answer);
        Assert.Equal(0, waitError);
    }

    [Fact]
    public void A_newer_version_that_ends_without_setting_the_event_is_answered_at_once_with_its_exit_code()
    {
        var (_, started) = NewEvent();
        using var __ = started;
        var newer = Start(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/c", "exit 7");
        var bound = TimeSpan.FromSeconds(60);
        var clock = Stopwatch.StartNew();

        var answer = NewerVersionProcess.AwaitStart(newer, started, bound, cancel: null, out var waitError);

        Assert.Equal(NewerVersionStart.Ended, answer);
        Assert.True(clock.Elapsed < Durations.Times(bound, 0.5), $"The wait took {clock.Elapsed}.");
        Assert.Contains("exit code 7", NewerVersionProcess.NotStartedFailure(answer, waitError, newer, bound).Message);
        Assert.True(NewerVersionProcess.EndNewerVersion(newer));
    }

    [Fact]
    public void A_newer_version_that_never_sets_the_event_is_ended_once_the_bound_has_passed()
    {
        var (_, started) = NewEvent();
        using var __ = started;
        var newer = PowerShellRunning("Start-Sleep -Seconds 600");

        var answer = NewerVersionProcess.AwaitStart(newer, started, TimeSpan.FromSeconds(2), cancel: null, out _);

        Assert.Equal(NewerVersionStart.TimedOut, answer);
        Assert.False(newer.HasExited);
        Assert.True(NewerVersionProcess.EndNewerVersion(newer));
        Assert.True(newer.HasExited);
    }

    [Fact]
    public void A_newer_version_that_would_set_the_event_after_the_bound_is_ended_before_it_does()
    {
        var (id, started) = NewEvent();
        using var _ = started;
        var newer = PowerShellRunning("Start-Sleep -Seconds 20; " + Sets(id));

        var answer = NewerVersionProcess.AwaitStart(newer, started, TimeSpan.FromSeconds(1), cancel: null, out var waitError);

        Assert.Equal(NewerVersionStart.TimedOut, answer);
        Assert.Equal(0, waitError);
        Assert.True(NewerVersionProcess.EndNewerVersion(newer));
        Assert.True(newer.HasExited);
        Assert.False(started.WaitOne(0));
    }

    [Fact]
    public void Ctrl_C_while_waiting_is_answered_as_cancelled_and_the_newer_version_is_ended()
    {
        var (_, started) = NewEvent();
        using var __ = started;
        using var cancel = new ManualResetEvent(false);
        var newer = PowerShellRunning("Start-Sleep -Seconds 600");
        using var timer = new Timer(_ => cancel.Set(), null, TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);

        var answer = NewerVersionProcess.AwaitStart(newer, started, TimeSpan.FromSeconds(60), cancel, out _);

        Assert.Equal(NewerVersionStart.Cancelled, answer);
        Assert.True(NewerVersionProcess.EndNewerVersion(newer));
        Assert.True(newer.HasExited);
    }

    [Fact]
    public void A_wait_Windows_refuses_is_answered_at_once_with_its_error_code_and_written_by_the_caller_alone()
    {
        var (_, started) = NewEvent();
        using var __ = started;
        // The started event opened again with the right to set it and not to wait on it, so
        // Windows refuses any wait that includes it, and any look at it.
        using var setOnly = new ManualResetEvent(false);
        setOnly.SafeWaitHandle.Dispose();
        setOnly.SafeWaitHandle = OpenEvent(EventModifyState, false, StartedEventNameOf(started));
        Assert.False(setOnly.SafeWaitHandle.IsInvalid);
        var newer = PowerShellRunning("Start-Sleep -Seconds 600");
        var bound = TimeSpan.FromSeconds(30);
        var clock = Stopwatch.StartNew();

        var answer = NewerVersionProcess.AwaitStart(newer, setOnly, bound, cancel: null, out var waitError);

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(10), $"The wait took {clock.Elapsed}.");
        Assert.Equal(NewerVersionStart.WaitFailed, answer);
        Assert.Equal(ErrorAccessDenied, waitError);
        Assert.Empty(TestCrashLog.Text());
        Assert.Contains("failed with error 5", NewerVersionProcess.NotStartedFailure(answer, waitError, newer, bound).Message);
        Assert.True(NewerVersionProcess.EndNewerVersion(newer));
        Assert.True(newer.HasExited);
    }

    [Fact]
    public void The_same_wait_on_the_event_opened_with_every_right_runs_to_its_bound()
    {
        // The control for the refused wait above: the same event, the same kind of newer
        // version, and only the access the event was opened with differs.
        var (_, started) = NewEvent();
        using var __ = started;
        using var full = new ManualResetEvent(false);
        full.SafeWaitHandle.Dispose();
        full.SafeWaitHandle = OpenEvent(EventAllAccess, false, StartedEventNameOf(started));
        Assert.False(full.SafeWaitHandle.IsInvalid);
        var newer = PowerShellRunning("Start-Sleep -Seconds 600");

        var answer = NewerVersionProcess.AwaitStart(newer, full, TimeSpan.FromSeconds(1), cancel: null, out var waitError);

        Assert.Equal(NewerVersionStart.TimedOut, answer);
        Assert.Equal(0, waitError);
        Assert.True(NewerVersionProcess.EndNewerVersion(newer));
    }

    /// <summary>
    /// A PowerShell script standing in for a staged newer version: it writes the arguments
    /// it was given to <paramref name="argumentsFile"/>, one to a line, where that is not
    /// null, then runs <paramref name="then"/>, where <c>$set</c> sets the started event it
    /// was handed.
    /// </summary>
    private string StagedStandIn(string? argumentsFile, string then)
    {
        var prefix = HandOverContract.StartedEventName(Guid.Empty)[..^32];
        var script = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".ps1");
        var writeArguments = argumentsFile is null
            ? string.Empty
            : $"$args | Set-Content -LiteralPath '{argumentsFile}'";
        File.WriteAllText(script, $$"""
            {{writeArguments}}
            $id = ($args | Where-Object { $_ -like '{{HandOverContract.StartedEventSwitch}}*' }).Substring({{HandOverContract.StartedEventSwitch.Length}})
            $set = { [System.Threading.EventWaitHandle]::OpenExisting('{{prefix}}' + $id).Set() | Out-Null }
            {{then}}
            """);
        return script;
    }

    private static string[] RunningScript(string script) => ["-NoProfile", "-NonInteractive", "-File", script];

    /// <summary>The process whose ID the stand-in wrote to <paramref name="pidFile"/>, which the test then owns.</summary>
    private Process Owned(string pidFile)
    {
        var process = Process.GetProcessById(
            int.Parse(File.ReadAllText(pidFile).Trim(), System.Globalization.CultureInfo.InvariantCulture));
        _started.Add(process);
        return process;
    }

    [Fact]
    public void A_staged_newer_version_that_sets_the_event_is_started_with_the_replace_switches_and_left_running()
    {
        var exe = Path.Combine(_root, "InstallerClean.exe");
        var argumentsFile = Path.Combine(_root, "arguments.txt");
        var pidFile = Path.Combine(_root, "pid.txt");
        var script = StagedStandIn(argumentsFile, $"$PID | Set-Content -LiteralPath '{pidFile}'; & $set; Start-Sleep -Seconds 60");

        var answer = NewerVersionProcess.StartToReplace(PowerShell, exe, RunningScript(script), TimeSpan.FromSeconds(60));

        Assert.Equal(NewerVersionAnswer.Started, answer);
        Assert.False(Owned(pidFile).HasExited);
        Assert.Equal(ReplaceRead.Asked, HandOverContract.ReadReplaceRequest(File.ReadAllLines(argumentsFile), out var request));
        Assert.Equal(exe, request!.ExePath);
        Assert.Equal(Environment.ProcessId, request.ProcessId);
        Assert.Empty(request.Command);
        Assert.Empty(TestCrashLog.Entries());
    }

    [Fact]
    public void A_staged_newer_version_that_ends_without_setting_the_event_is_answered_as_not_started()
    {
        var script = StagedStandIn(Path.Combine(_root, "arguments.txt"), "exit 7");

        var answer = NewerVersionProcess.StartToReplace(
            PowerShell, Path.Combine(_root, "InstallerClean.exe"), RunningScript(script), TimeSpan.FromSeconds(60));

        Assert.Equal(NewerVersionAnswer.NotStarted, answer);
        Assert.Contains("exit code 7", TestCrashLog.OnlyEntry());
    }

    [Fact]
    public void A_staged_newer_version_that_does_not_set_the_event_in_time_is_ended_before_the_answer()
    {
        var script = StagedStandIn(argumentsFile: null, "Start-Sleep -Seconds 60");

        var answer = NewerVersionProcess.StartToReplace(
            PowerShell, Path.Combine(_root, "InstallerClean.exe"), RunningScript(script), TimeSpan.FromSeconds(5));

        // The answer and crash.log's one entry together say it was seen to end: one still
        // running a few seconds after Windows accepted the request to end it is answered
        // NotStarted as well, with a second entry saying so.
        Assert.Equal(NewerVersionAnswer.NotStarted, answer);
        Assert.Contains("within 5 seconds", TestCrashLog.OnlyEntry());
    }

    [Fact]
    public void A_staged_newer_version_that_cannot_be_started_is_answered_as_not_started()
    {
        var missing = Path.Combine(_root, "InstallerClean-portable.exe");

        var answer = NewerVersionProcess.StartToReplace(missing, Path.Combine(_root, "InstallerClean.exe"), [], TimeSpan.FromSeconds(5));

        Assert.Equal(NewerVersionAnswer.NotStarted, answer);
        Assert.Equal(ProcessStartRetry.Attempts, TestCrashLog.FailedStartsOf(missing));
    }
}
