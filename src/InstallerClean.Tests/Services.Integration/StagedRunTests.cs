using System.Diagnostics;
using System.IO.Pipes;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Tests.Helpers;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// A newer command line running a command from the staging folder for an older copy: the
/// detached start of the replace once the command has ended
/// (<see cref="StagedRun.StartReplace(ReplaceRequest, string)"/>), the start itself
/// (<see cref="DetachedProcess"/>), and the check that the copy it stands for is running
/// (<see cref="StagedRun.HoldCopy"/>), against real processes: a console program compiled
/// for the purpose standing in for the replace (<see cref="DetachedStandIn"/>), and cmd.
/// </summary>
public sealed class StagedRunTests : IDisposable
{
    private readonly string _root = TestTemp.NewFolder("ic-staged-");
    private readonly List<Process> _owned = [];
    private readonly IDisposable _crashLog = TestCrashLog.OwnFolder();

    public void Dispose()
    {
        try
        {
            // A stand-in the test did not take, an assertion having failed first, is ended
            // too, where the process with the ID it wrote still runs from the stand-in's
            // file. Both paths are resolved to the file's final name, so a short name in
            // either is not read as another file.
            if (_owned.Count == 0 && TryReadPid() is { } pid)
            {
                Process? unowned = null;
                try
                {
                    unowned = Process.GetProcessById(pid);
                    if (unowned.MainModule?.FileName is { } image
                        && InstallerCacheHelpers.TryResolveFinalPath(image, out var running)
                        && InstallerCacheHelpers.TryResolveFinalPath(DetachedStandIn.Exe, out var standIn)
                        && string.Equals(running, standIn, StringComparison.OrdinalIgnoreCase))
                    {
                        _owned.Add(unowned);
                        unowned = null;
                    }
                }
                catch { }
                finally
                {
                    unowned?.Dispose();
                }
            }
            foreach (var process in _owned)
            {
                // Waited for, so the stand-in's exe is free to delete once the test run ends.
                try { if (!process.HasExited) { process.Kill(); process.WaitForExit(10_000); } } catch { }
                process.Dispose();
            }
            TestTemp.Delete(_root);
        }
        finally
        {
            _crashLog.Dispose();
        }
    }

    private static readonly string Cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");

    /// <summary>A request standing for a copy at a path in the test's folder, run by process <paramref name="processId"/>.</summary>
    private ReplaceRequest Request(int processId) =>
        new(Path.Combine(_root, "installerclean-cli.exe"), processId, Guid.NewGuid().ToString("N"), ["/s"]);

    /// <summary>The process ID the stand-in wrote to pid.txt in the test's folder, or null where it wrote none.</summary>
    private int? TryReadPid()
    {
        try
        {
            return int.TryParse(File.ReadAllText(Path.Combine(_root, "pid.txt")).Trim(),
                System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var pid) ? pid : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// The process whose ID the stand-in wrote to pid.txt in the test's folder, which the
    /// test then owns, waiting up to 30 seconds for the file.
    /// </summary>
    private Process Owned()
    {
        var clock = Stopwatch.StartNew();
        int? pid;
        while ((pid = TryReadPid()) is null && clock.Elapsed < TimeSpan.FromSeconds(30)) Thread.Sleep(100);
        Assert.True(pid is not null, "The stand-in wrote no process ID within 30 seconds, so it never ran its own code.");
        var process = Process.GetProcessById(pid.Value);
        _owned.Add(process);
        return process;
    }

    [Fact]
    public void The_replace_is_given_the_copys_path_and_process_and_an_event_of_its_own_and_works_in_its_own_folder()
    {
        // A process ID that is not this one, so the test sees the request's ID passed on.
        var request = Request(processId: 4);

        EventLogRecorder.Clear();

        var answer = StagedRun.StartReplace(request, DetachedStandIn.Exe, DetachedStandIn.Before(_root, "set"), TimeSpan.FromSeconds(60));

        Assert.Equal(NewerVersionAnswer.Started, answer);
        Assert.False(Owned().HasExited);
        Assert.Empty(EventLogRecorder.Entries);
        Assert.Equal(ReplaceRead.Asked, HandOverContract.ReadReplaceRequest(File.ReadAllLines(Path.Combine(_root, "arguments.txt")), out var passed));
        Assert.Equal(request.ExePath, passed!.ExePath);
        Assert.Equal(4, passed.ProcessId);
        Assert.NotEqual(request.StartedEvent, passed.StartedEvent);
        Assert.Empty(passed.Command);
        Assert.Equal(Path.GetDirectoryName(DetachedStandIn.Exe), File.ReadAllText(Path.Combine(_root, "folder.txt")).Trim(), ignoreCase: true);
        Assert.Empty(TestCrashLog.Entries());
    }

    [Fact]
    public void A_replace_that_does_not_set_its_event_in_time_is_ended_and_said_in_crash_log()
    {
        EventLogRecorder.Clear();

        var answer = StagedRun.StartReplace(Request(Environment.ProcessId), DetachedStandIn.Exe, DetachedStandIn.Before(_root, "wait"), TimeSpan.FromSeconds(5));

        Assert.Equal(NewerVersionAnswer.NotStarted, answer);
        Assert.Contains("within 5 seconds", TestCrashLog.OnlyEntry());
        Assert.Equal(CliEventClass.UpdateNotInstalledNotice, Assert.Single(EventLogRecorder.Entries).Class);
    }

    [Fact]
    public void A_replace_that_ends_by_itself_without_setting_its_event_leaves_the_notice_to_itself()
    {
        EventLogRecorder.Clear();

        var answer = StagedRun.StartReplace(Request(Environment.ProcessId), DetachedStandIn.Exe, DetachedStandIn.Before(_root, "exit"), TimeSpan.FromSeconds(60));

        Assert.Equal(NewerVersionAnswer.NotStarted, answer);
        Assert.Empty(EventLogRecorder.Entries);
        // The stand-in writes what it was given before it exits, so the end was its own.
        Assert.Equal(ReplaceRead.Asked, HandOverContract.ReadReplaceRequest(File.ReadAllLines(Path.Combine(_root, "arguments.txt")), out _));
    }

    [Fact]
    public void A_replace_that_cannot_be_started_is_tried_again_and_answered_as_not_started()
    {
        var missing = Path.Combine(_root, "installerclean-cli.exe");

        EventLogRecorder.Clear();

        var answer = StagedRun.StartReplace(Request(Environment.ProcessId), missing, [], TimeSpan.FromSeconds(5));

        Assert.Equal(NewerVersionAnswer.NotStarted, answer);
        Assert.Equal(ProcessStartRetry.Attempts, TestCrashLog.FailedStartsOf(missing));
        Assert.Equal(CliEventClass.UpdateNotInstalledNotice, Assert.Single(EventLogRecorder.Entries).Class);
    }

    [Fact]
    public async Task A_detached_program_holds_no_handle_of_the_starting_process()
    {
        // Compiled first where no test has yet: the compile starts programs that inherit
        // handles, and the pipe below is not to be among them.
        var standIn = DetachedStandIn.Exe;
        // An inheritable pipe: a program that inherited the write end would hold it open, and
        // the read below would wait for it to end.
        using var pipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        using var started = DetachedProcess.Start(standIn, DetachedStandIn.Before(_root, "wait"), _root);
        Assert.NotNull(started);
        var child = Owned();

        pipe.DisposeLocalCopyOfClientHandle();
        var read = pipe.ReadAsync(new byte[1]).AsTask();

        Assert.Same(read, await Task.WhenAny(read, Task.Delay(TimeSpan.FromSeconds(5))));
        Assert.Equal(0, await read);
        Assert.False(child.HasExited);
    }

    [Fact]
    public void A_detached_program_works_in_the_folder_it_is_given()
    {
        var folder = Directory.CreateDirectory(Path.Combine(_root, "work here")).FullName;

        using var started = DetachedProcess.Start(Cmd, ["/d", "/c", "cd>where.txt"], folder);

        Assert.NotNull(started);
        Assert.Equal(Kernel32.WAIT_OBJECT_0, HandleWait.One(started!, 30_000, out _));
        Assert.Equal(folder, File.ReadAllText(Path.Combine(folder, "where.txt")).Trim(), ignoreCase: true);
    }

    [Fact]
    public void A_run_stands_for_a_copy_only_where_the_process_runs_from_the_file_at_the_path()
    {
        var own = Environment.ProcessPath!;
        var elsewhere = Path.Combine(_root, "installerclean-cli.exe");
        File.Copy(own, elsewhere);

        using (var held = StagedRun.HoldCopy(new ReplaceRequest(own, Environment.ProcessId, Guid.NewGuid().ToString("N"), ["/s"])))
            Assert.NotNull(held);
        Assert.Empty(TestCrashLog.Entries());
        Assert.Null(StagedRun.HoldCopy(new ReplaceRequest(elsewhere, Environment.ProcessId, Guid.NewGuid().ToString("N"), ["/s"])));
        Assert.Contains("is not the one process", TestCrashLog.OnlyEntry());
    }
}
