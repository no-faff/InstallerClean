using System.Text;
using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The guard every run's output goes through (<see cref="ConsoleGuard"/>): a write that
/// fails loses what it was writing and never ends the run, so a run's one outcome entry and
/// its exit code are the ones its work earns. Driven on its own, through the real work method
/// with a failing console behind the guard as Main puts it there, and through Main.
///
/// WHAT EVERY RUN FIXTURE SETS UP is a scan offering two files and a re-verify that keeps
/// both, with a clean pending-reboot gate unless the test blocks it, so the run reaches
/// whichever service the test scripted. The console fails at the line the test names, or at
/// every write from the scanning line on.
/// </summary>
public class CliConsoleGuardTests
{
    /// <summary>
    /// Temp is fully qualified on either host and outside both forbidden sets, so the
    /// destination gates pass it. Nothing is created and nothing is written: the move
    /// service is a substitute.
    /// </summary>
    private static readonly string Destination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-guard-test");

    private const string File1 = @"C:\Windows\Installer\a.msi";
    private const string File2 = @"C:\Windows\Installer\b.msp";

    private static readonly PatchClaim SurvivingA =
        new(File1, "{AAAA1111-0000-0000-0000-000000000001}", "{PPPP1111-0000-0000-0000-000000000001}", null, 2);
    private static readonly PatchClaim SurvivingB =
        new(File2, "{AAAA1111-0000-0000-0000-000000000002}", "{PPPP1111-0000-0000-0000-000000000001}", null, 2);

    private static readonly string NewLine = Environment.NewLine;

    // ---- The guard ----

    [Fact]
    public void Every_write_reaches_the_console_while_it_takes_them()
    {
        var console = new StringWriter();
        var guard = new ConsoleGuard(console);

        guard.Write("a");
        guard.Write('b');
        guard.WriteLine("c");
        guard.WriteLine();

        Assert.Equal("abc" + NewLine + NewLine, console.ToString());
        Assert.False(guard.Failed);
    }

    [Fact]
    public void A_write_that_fails_loses_its_own_text_and_the_next_write_starts_a_line_of_its_own()
    {
        var console = new ConsoleFailingAt("second");
        var guard = new ConsoleGuard(console);

        guard.WriteLine("first");
        var thrown = Record.Exception(() =>
        {
            guard.WriteLine("second");
            guard.WriteLine("third");
            guard.Write('x');
            guard.Flush();
        });

        Assert.Null(thrown);
        Assert.True(guard.Failed);
        Assert.Equal(1, console.Refused);
        Assert.Equal("first" + NewLine + NewLine + "third" + NewLine + "x", console.ToString());
    }

    [Theory]
    [InlineData("a character")]
    [InlineData("a character array")]
    [InlineData("part of a character array")]
    [InlineData("a string builder")]
    [InlineData("a bool")]
    [InlineData("an int")]
    [InlineData("a uint")]
    [InlineData("a long")]
    [InlineData("a ulong")]
    [InlineData("a float")]
    [InlineData("a double")]
    [InlineData("a decimal")]
    public void A_write_that_fails_in_any_form_leaves_one_blank_line_in_place_of_its_own(string form)
    {
        var console = new ConsoleFailingAt(form == "a bool" ? "True" : "2");
        var guard = new ConsoleGuard(console);

        guard.WriteLine("first");
        var thrown = Record.Exception(() =>
        {
            switch (form)
            {
                case "a character": guard.WriteLine('2'); break;
                case "a character array": guard.WriteLine(['2']); break;
                case "part of a character array": guard.WriteLine(['1', '2', '3'], 1, 1); break;
                case "a string builder": guard.WriteLine(new StringBuilder("2")); break;
                case "a bool": guard.WriteLine(true); break;
                case "an int": guard.WriteLine(2); break;
                case "a uint": guard.WriteLine(2u); break;
                case "a long": guard.WriteLine(2L); break;
                case "a ulong": guard.WriteLine(2UL); break;
                case "a float": guard.WriteLine(2f); break;
                case "a double": guard.WriteLine(2d); break;
                default: guard.WriteLine(2m); break;
            }
            guard.WriteLine("third");
        });

        Assert.Null(thrown);
        Assert.Equal(1, console.Refused);
        Assert.Equal("first" + NewLine + NewLine + "third" + NewLine, console.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_string_builder_goes_out_as_one_write_and_a_failure_in_it_writes_none_of_it(bool asALine)
    {
        // TextWriter writes a string builder as the one string its ToString gives, so a
        // failure anywhere in the text refuses all of it, however many chunks it holds.
        var builder = new StringBuilder(4).Append("abcd").Append("2xyz").Append("efgh");
        var console = new ConsoleFailingAt("2");
        var guard = new ConsoleGuard(console);

        guard.WriteLine("first");
        var thrown = Record.Exception(() =>
        {
            if (asALine) guard.WriteLine(builder);
            else guard.Write(builder);
            guard.WriteLine("third");
        });

        Assert.Null(thrown);
        Assert.Equal(1, console.Refused);
        Assert.Equal("first" + NewLine + NewLine + "third" + NewLine, console.ToString());
    }

    [Fact]
    public void A_line_a_failed_write_cuts_short_is_not_joined_by_the_next_line_out()
    {
        // The writer the console gives a run: a StreamWriter with the console's 256-character
        // buffer, flushing after every write, behind TextWriter.Synchronized. The first line
        // goes to the disk in two pieces. The disk takes the first piece and refuses the second,
        // then refuses the line break the guard puts ahead of the next line, then has room.
        var disk = new DiskRefusingWrites(2, 3);
        var console = TextWriter.Synchronized(
            new StreamWriter(disk, new UTF8Encoding(false), 256, leaveOpen: true) { AutoFlush = true });
        var guard = new ConsoleGuard(console);

        var thrown = Record.Exception(() =>
        {
            guard.WriteLine(new string('a', 300));
            guard.WriteLine("lost");
            guard.WriteLine("last");
        });

        Assert.Null(thrown);
        Assert.True(guard.Failed);
        Assert.Equal(5, disk.Writes);
        Assert.Equal(
            new string('a', 256) + NewLine + "last" + NewLine,
            Encoding.UTF8.GetString(disk.ToArray()));
    }

    [Fact]
    public void A_failed_write_goes_to_the_crash_log_once()
    {
        using var _ = TestCrashLog.OwnFolder();
        var console = new ConsoleFailingAt("");
        var guard = new ConsoleGuard(console);

        guard.WriteLine("first");
        guard.WriteLine("second");

        Assert.True(console.Refused == 2, "The second write's line break never reached the console.");
        Assert.Equal(1, TestCrashLog.Text().Split(ConsoleFailingAt.Message).Length - 1);
    }

    [Fact]
    public void A_write_to_a_pipe_whose_reader_has_gone_is_no_failure_and_nothing_is_recorded()
    {
        // The console's writer over a pipe that takes the first line and then refuses every
        // write with ERROR_PIPE_NOT_CONNECTED, as .NET Framework's console stream throws it.
        using var _ = TestCrashLog.OwnFolder();
        var pipe = new PipeReaderGoneAfter(1);
        var console = TextWriter.Synchronized(
            new StreamWriter(pipe, new UTF8Encoding(false), 256, leaveOpen: true) { AutoFlush = true });
        var guard = new ConsoleGuard(console);

        var thrown = Record.Exception(() =>
        {
            guard.WriteLine("first");
            guard.WriteLine(new string('a', 300));
            guard.Write('x');
            guard.Flush();
        });

        Assert.Null(thrown);
        Assert.False(guard.Failed);
        Assert.True(pipe.Writes > 2, "The writes after the reader went never reached the pipe.");
        Assert.Equal("first" + NewLine, Encoding.UTF8.GetString(pipe.ToArray()));
        Assert.Equal("", TestCrashLog.Text());
    }

    [Theory]
    [InlineData("a character")]
    [InlineData("a line break")]
    [InlineData("characters from an array")]
    [InlineData("a string builder")]
    public void A_console_failing_at_an_empty_text_refuses_every_write(string write)
    {
        var console = new ConsoleFailingAt("");

        var thrown = Record.Exception(() =>
        {
            switch (write)
            {
                case "a character": console.Write('x'); break;
                case "a line break": console.WriteLine(); break;
                case "a string builder": console.Write(new StringBuilder("xy")); break;
                default: console.Write(['x', 'y'], 0, 2); break;
            }
        });

        Assert.IsType<IOException>(thrown);
        Assert.Equal(1, console.Refused);
        Assert.Equal(string.Empty, console.ToString());
    }

    // ---- Runs whose output fails ----

    [Fact]
    public async Task A_delete_whose_output_fails_after_the_batch_is_logged_and_exits_as_that_delete()
    {
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(2, Array.Empty<FileOperationError>()));

        var run = await Run("/d", Services(delete: delete), FailsAt(Strings.Cli_DeletedFiles));

        Assert.True(run.Refused > 0, "The console refused no write, so it never failed after the batch.");
        Assert.Equal(CliExitCode.Ok, run.ExitCode);
        var summary = Assert.Single(run.Entries, CliRunFixtures.IsSummary);
        Assert.Equal(CliEventClass.Ok, summary.Class);
        Assert.Equal(MachineContract.English(() => string.Format(Strings.Cli_EventLogDeleteSummary,
            "/d", 2, 2, DisplayHelpers.PluraliseFile(2), DisplayHelpers.FormatSizeForMachine(300),
            0, DisplayHelpers.PluraliseError(0))), summary.Text);
    }

    [Fact]
    public async Task A_move_whose_output_fails_after_the_batch_is_logged_and_exits_as_that_move()
    {
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));

        var run = await Run("/m", Services(move: move), FailsAt(Strings.Cli_MovedFiles));

        Assert.True(run.Refused > 0, "The console refused no write, so it never failed after the batch.");
        Assert.Equal(CliExitCode.Ok, run.ExitCode);
        var summary = Assert.Single(run.Entries, CliRunFixtures.IsSummary);
        Assert.Equal(CliEventClass.Ok, summary.Class);
        Assert.Equal(MachineContract.English(() => string.Format(Strings.Cli_EventLogMoveSummary,
            "/m", 2, 2, DisplayHelpers.PluraliseFile(2), Destination,
            DisplayHelpers.FormatSizeForMachine(300), 0, DisplayHelpers.PluraliseError(0))), summary.Text);
    }

    [Theory]
    [InlineData("the line counting what was deleted")]
    [InlineData("the line saying the run was cancelled")]
    public async Task A_cancelled_delete_whose_output_fails_is_logged_and_exits_as_that_cancellation(string failsAt)
    {
        using var cts = new CancellationTokenSource();
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true);
            });

        var run = await Run("/d", Services(delete: delete),
            failsAt == "the line saying the run was cancelled"
                ? FailsAt(Strings.Cli_Cancelled)
                : FailsAt(Strings.Cli_DeletedFiles),
            cts.Token);

        Assert.True(run.Refused > 0, $"The console refused no write, so it never failed at {failsAt}.");
        Assert.Equal(CliExitCode.Partial, run.ExitCode);
        var summary = Assert.Single(run.Entries, CliRunFixtures.IsSummary);
        Assert.Equal(CliEventClass.Partial, summary.Class);
    }

    [Fact]
    public async Task A_stopped_move_whose_output_fails_is_logged_and_exits_as_that_stopped_move()
    {
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns<MoveResult>(_ => throw new MoveAbortedException(
                "stopped", new MoveResult(1, Array.Empty<FileOperationError>()),
                Destination, MoveAbortReason.ResolvesElsewhere));
        var stopped = CliContract.ClassifyAbortedMove(1);

        var run = await Run("/m", Services(move: move), FailsAt(Strings.Cli_MovedFiles));

        Assert.True(run.Refused > 0, "The console refused no write, so it never failed after the stop.");
        Assert.Equal(stopped.ExitCode, run.ExitCode);
        var summary = Assert.Single(run.Entries, CliRunFixtures.IsSummary);
        Assert.Equal(stopped.EventClass, summary.Class);
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_delete_or_move_whose_output_fails_from_its_first_line_still_acts_and_exits_as_its_batch(string arg)
    {
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(2, Array.Empty<FileOperationError>()));
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));

        // Every write fails, the scanning line first.
        var run = await Run(arg, Services(delete: delete, move: move), new ConsoleFailingAt(""));

        Assert.True(run.Refused > 0, "The console refused no write.");
        string[] both = [File1, File2];
        if (arg == "/d")
            await delete.Received(1).DeleteFilesAsync(Arg.Is<IEnumerable<string>>(f => f.SequenceEqual(both)),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        else
            await move.Received(1).MoveFilesAsync(Arg.Is<IEnumerable<string>>(f => f.SequenceEqual(both)),
                Destination, Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>());
        Assert.Equal(CliExitCode.Ok, run.ExitCode);
        var summary = Assert.Single(run.Entries, CliRunFixtures.IsSummary);
        Assert.Equal(CliEventClass.Ok, summary.Class);
        Assert.Equal(arg == "/d"
            ? MachineContract.English(() => string.Format(Strings.Cli_EventLogDeleteSummary,
                "/d", 2, 2, DisplayHelpers.PluraliseFile(2), DisplayHelpers.FormatSizeForMachine(300),
                0, DisplayHelpers.PluraliseError(0)))
            : MachineContract.English(() => string.Format(Strings.Cli_EventLogMoveSummary,
                "/m", 2, 2, DisplayHelpers.PluraliseFile(2), Destination,
                DisplayHelpers.FormatSizeForMachine(300), 0, DisplayHelpers.PluraliseError(0))),
            summary.Text);
    }

    [Fact]
    public async Task A_run_refused_for_an_install_in_progress_keeps_its_come_back_later_code_when_its_output_fails()
    {
        var reboot = Substitute.For<IPendingRebootService>();
        reboot.Check().Returns(PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld));

        // Every write fails, the scanning line first, so nothing this run prints gets out.
        var run = await Run("/d", Services(reboot: reboot), new ConsoleFailingAt(""));

        Assert.True(run.Refused > 0, "The console refused no write.");
        Assert.Equal(CliExitCode.Transient, run.ExitCode);
        var summary = Assert.Single(run.Entries, CliRunFixtures.IsSummary);
        Assert.Equal(CliEventClass.TransientSkip, summary.Class);
    }

    // ---- A refusal at the start ----

    [Fact]
    public void A_refusal_prints_its_message_on_a_line_and_gives_the_error_code()
    {
        var error = new StringWriter();

        Assert.Equal(CliExitCode.Error, Program.Refuse("refused here", error));
        Assert.Equal("refused here" + NewLine, error.ToString());
    }

    [Fact]
    public void A_refusal_whose_error_stream_refuses_every_write_still_gives_the_error_code()
    {
        using var _ = TestCrashLog.OwnFolder();
        var error = new ConsoleFailingAt("");

        Assert.Equal(CliExitCode.Error, Program.Refuse("refused here", error));
        Assert.True(error.Refused > 0, "The error stream was never written to.");
    }

    // ---- Main ----

    [Theory]
    [InlineData("--version")]
    [InlineData("--help")]
    public void A_help_or_version_request_whose_output_fails_exits_1_and_writes_no_entry(string arg)
    {
        // Through Main, which puts the output behind the guard. A failed write that got past
        // the guard would reach Main's catch-all, which writes a HardError entry.
        var console = new ConsoleFailingAt("");
        var original = Console.Out;
        try
        {
            Console.SetOut(console);
            EventLogRecorder.Clear();

            var exitCode = Program.Main([arg]);

            Assert.True(console.Refused > 0, "The console refused no write.");
            Assert.Equal(CliExitCode.Error, exitCode);
            Assert.Empty(EventLogRecorder.Entries);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("--help")]
    public void A_help_or_version_request_whose_output_is_written_exits_0(string arg)
    {
        var console = new StringWriter();
        var original = Console.Out;
        try
        {
            Console.SetOut(console);
            EventLogRecorder.Clear();

            var exitCode = Program.Main([arg]);

            Assert.NotEqual(string.Empty, console.ToString());
            Assert.Equal(CliExitCode.Ok, exitCode);
            Assert.Empty(EventLogRecorder.Entries);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Fact]
    public void A_replace_switch_in_a_request_not_well_formed_is_refused_as_an_argument_and_sets_no_event()
    {
        var id = Guid.NewGuid();
        using var started = HandOverContract.CreateStartedEvent(id)!;
        var console = new StringWriter();
        var original = Console.Out;
        using var crashLog = TestCrashLog.OwnFolder();
        try
        {
            Console.SetOut(console);
            EventLogRecorder.Clear();

            // Without /afterprocess:, so the three are not all there. A scan, which changes
            // nothing were the refusal ever to fail.
            var exitCode = Program.Main(["/s", "/replacecopy:C:\\x.exe", HandOverContract.StartedEventArgument(id)]);

            Assert.NotEqual(CliExitCode.Ok, exitCode);
            Assert.Contains("/replacecopy:C:\\x.exe", console.ToString());
            Assert.Contains(EventLogRecorder.Entries, entry => entry.Class == CliEventClass.HardError);
            Assert.False(started.WaitOne(0));
            Assert.Contains(HandOverContract.ReplaceNotWellFormedRefusal, TestCrashLog.Text());
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Fact]
    public void A_request_to_run_a_command_for_a_copy_its_process_does_not_run_from_runs_nothing_and_sets_no_event()
    {
        var id = Guid.NewGuid();
        using var started = HandOverContract.CreateStartedEvent(id)!;
        var console = new StringWriter();
        var original = Console.Out;
        using var crashLog = TestCrashLog.OwnFolder();
        var replaces = 0;
        var startReplace = Program.StartReplace;
        try
        {
            Console.SetOut(console);
            EventLogRecorder.Clear();
            Program.StartReplace = _ => replaces++;

            // A path this process does not run from. A scan beside the three, which changes
            // nothing were the refusal ever to fail.
            string[] args = [.. HandOverContract.ReplaceArguments(@"C:\x.exe", Environment.ProcessId, id), "/s"];
            var exitCode = Program.Main(args);

            Assert.Equal(CliExitCode.Error, exitCode);
            Assert.Equal("", console.ToString());
            Assert.Empty(EventLogRecorder.Entries);
            Assert.False(started.WaitOne(0));
            Assert.Equal(0, replaces);
            Assert.Contains(HandOverContract.NotTheCopyRefusal, TestCrashLog.Text());
        }
        finally
        {
            Program.StartReplace = startReplace;
            Console.SetOut(original);
        }
    }

    [Theory]
    [InlineData("--version", CliExitCode.Ok)]
    [InlineData("/nosuchswitch", CliExitCode.Error)]
    public void A_command_run_for_the_copy_this_process_runs_from_sets_the_event_ends_with_its_own_code_and_then_starts_the_replace(
        string command, int expected)
    {
        var id = Guid.NewGuid();
        using var started = HandOverContract.CreateStartedEvent(id)!;
        var console = new StringWriter();
        var original = Console.Out;
        using var crashLog = TestCrashLog.OwnFolder();
        List<(ReplaceRequest Request, bool EventSet)> replaces = [];
        var startReplace = Program.StartReplace;
        try
        {
            Console.SetOut(console);
            EventLogRecorder.Clear();
            Program.StartReplace = request => replaces.Add((request, started.WaitOne(0)));

            string[] args = [command, .. HandOverContract.ReplaceArguments(Environment.ProcessPath!, Environment.ProcessId, id)];
            var exitCode = Program.Main(args);

            Assert.Equal(expected, exitCode);
            Assert.NotEqual("", console.ToString());
            var replace = Assert.Single(replaces);
            Assert.True(replace.EventSet);
            Assert.Equal(Environment.ProcessPath, replace.Request.ExePath);
            Assert.Equal(Environment.ProcessId, replace.Request.ProcessId);
            Assert.Equal([command], replace.Request.Command);
        }
        finally
        {
            Program.StartReplace = startReplace;
            Console.SetOut(original);
        }
    }

    // ---- fixtures ----

    /// <summary>A console that fails at the first write carrying the words of <paramref name="value"/> up to its first placeholder.</summary>
    private static ConsoleFailingAt FailsAt(string value) =>
        new(value.Contains('{') ? value[..value.IndexOf('{')] : value);

    private sealed record RunResult(
        int ExitCode, int Refused, IReadOnlyList<(CliEventClass Class, string Text)> Entries);

    /// <summary>
    /// Runs the real work method with <paramref name="console"/> behind the guard, as Main
    /// puts the console there.
    /// </summary>
    private static async Task<RunResult> Run(
        string arg, IServiceProvider services, ConsoleFailingAt console, CancellationToken token = default)
    {
        var invocation = arg == "/m"
            ? new CliInvocation(CliCommand.Move, null, Destination)
            : new CliInvocation(CliCommand.Delete, null, null);

        // Console.SetOut and the recorder are process-global; the assembly disables test
        // parallelisation, which is what makes both safe to read back here.
        var original = Console.Out;
        try
        {
            var guard = new ConsoleGuard(console);
            Console.SetOut(guard);
            EventLogRecorder.Clear();
            var exitCode = await Program.RunWorkAsync(arg, invocation, token, services, () => false, guard);
            return new RunResult(exitCode, console.Refused, EventLogRecorder.Entries);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    /// <summary>
    /// The services the work path resolves: a scan offering two files of 100 and 200 bytes,
    /// the pending-reboot gate <paramref name="reboot"/> or a clean one, and a re-verify
    /// keeping both.
    /// </summary>
    private static IServiceProvider Services(
        IDeleteFilesService? delete = null, IMoveFilesService? move = null,
        IPendingRebootService? reboot = null)
    {
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                new[]
                {
                    new OrphanedFile(File1, 100, IsPatch: false, IsRemovablePatch: false,
                        IsObsoleted: false, Reason: "unclaimed"),
                    new OrphanedFile(File2, 200, IsPatch: true, IsRemovablePatch: true,
                        IsObsoleted: false, Reason: "superseded"),
                },
                Array.Empty<RegisteredPackage>(),
                RegisteredTotalBytes: 0));

        if (reboot is null)
        {
            reboot = Substitute.For<IPendingRebootService>();
            reboot.Check().Returns(PendingRebootResult.Clean);
        }

        var reverifier = Substitute.For<IRemovableReverifier>();
        reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ReverifyResult(
                new[] { File1, File2 },
                Array.Empty<string>(),
                SurvivingPatchClaims: new[] { SurvivingA, SurvivingB },
                SiblingPatchClaims: Array.Empty<PatchClaim>()));

        return CliRunFixtures.Services(scan, reboot: reboot, reverifier: reverifier,
            delete: delete, move: move);
    }
}

/// <summary>
/// A disk under a redirected standard output that refuses the stream writes whose numbers,
/// counted from one, are in <c>refused</c>, as a full disk does, and takes the rest.
/// <see cref="Writes"/> counts every write it was asked for.
/// </summary>
internal sealed class DiskRefusingWrites(params int[] refused) : MemoryStream
{
    public int Writes { get; private set; }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (refused.Contains(++Writes))
            throw new IOException(ConsoleFailingAt.Message);
        base.Write(buffer, offset, count);
    }
}

/// <summary>
/// A pipe that takes the first <c>taken</c> writes and refuses every later one as .NET
/// Framework's console stream refuses a write to a pipe with no reader: an IOException
/// carrying ERROR_PIPE_NOT_CONNECTED's HRESULT.
/// </summary>
internal sealed class PipeReaderGoneAfter(int taken) : MemoryStream
{
    public int Writes { get; private set; }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (++Writes > taken)
            throw new IOException("No process is on the other end of the pipe.", unchecked((int)0x800700E9));
        base.Write(buffer, offset, count);
    }
}

/// <summary>
/// A console that takes every write until one carrying <c>text</c>, and throws on that one,
/// as a write to a redirected standard output on a full disk does. An empty
/// <c>text</c> refuses every write it is given: a string, a character, characters from an
/// array, a string builder and a line break alike. <see cref="Refused"/> counts the writes
/// it threw on.
/// </summary>
/// <remarks>
/// On .NET Framework, TextWriter has no span or string builder Write: a string builder
/// reaches Write of an object, which writes its text as one string, so every write comes
/// down to the overloads refused here. A null builder writes nothing and so is not refused.
/// </remarks>
internal sealed class ConsoleFailingAt(string text) : StringWriter
{
    /// <summary>The message of the exception every refused write throws.</summary>
    internal const string Message = "There is not enough space on the disk.";

    public int Refused { get; private set; }

    public override void Write(string? value)
    {
        Refuse(value ?? string.Empty);
        base.Write(value);
    }

    public override void Write(char value)
    {
        Refuse(value.ToString());
        base.Write(value);
    }

    // A line break with no text reaches here, through TextWriter.WriteLine().
    public override void Write(char[] buffer, int index, int count)
    {
        Refuse(new string(buffer, index, count));
        base.Write(buffer, index, count);
    }

    private void Refuse(string value)
    {
        if (!value.Contains(text, StringComparison.Ordinal)) return;
        Refused++;
        throw new IOException(Message);
    }

    public override void WriteLine(string? value)
    {
        Write(value);
        base.WriteLine();
    }
}
