using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The command line sets the PC's first-run mark after a Delete that deleted a file and
/// a Move that moved one, the cancelled and the stopped runs included, and where the
/// service fails with an unforeseen exception before its count comes back. It leaves the
/// mark alone after a scan, after a run that moved or deleted nothing, after a refusal
/// or a cancellation the service raises before its first file, and after a failure met
/// before any batch was handed over. Driven through the real work method with
/// substitute services; the mark is a substitute too, so nothing is written to the
/// registry.
///
/// EACH TEST ALSO READS THE RUN'S OWN APPLICATION-LOG ENTRIES BACK through the window's
/// reader, which has to count them exactly where the run set the mark, with one
/// exception: a batch whose count never came back sets the mark while its entry, the
/// failure line, names no count.
///
/// EVERY RUN THAT LEAVES THE MARK ALONE HAS A TWIN HERE THAT SETS IT, built from the same
/// fixtures with one thing changed: the count the service hands back, the files the
/// check before acting keeps, the step the failure is met in, the kind of failure the
/// service raises, or, for the scan, the command.
/// </summary>
public class CliFirstRunMarkTests
{
    /// <summary>
    /// Temp is fully qualified on either host and outside both forbidden sets, so the
    /// destination gates pass it. Nothing is created: the move service is a substitute.
    /// </summary>
    private static readonly string Destination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-first-run-test");

    private const string File1 = @"C:\Windows\Installer\a.msi";
    private const string File2 = @"C:\Windows\Installer\b.msp";

    [Fact]
    public async Task A_delete_that_deleted_files_sets_the_mark()
    {
        var mark = Substitute.For<IFirstRunMark>();

        var exitCode = await Run("/d", null, Services(mark, delete: Delete(new DeleteResult(2, NoErrors))));

        Assert.Equal(CliExitCode.Ok, exitCode);
        mark.Received(1).Set();
        AssertTheLogRecordsIt(actedOnFiles: true);
    }

    [Fact]
    public async Task A_delete_in_which_every_file_failed_leaves_the_mark()
    {
        var mark = Substitute.For<IFirstRunMark>();
        var failed = new DeleteResult(0, new FileOperationError[] { new UnknownError(File1), new UnknownError(File2) });

        var exitCode = await Run("/d", null, Services(mark, delete: Delete(failed)));

        Assert.Equal(CliExitCode.Error, exitCode);
        mark.DidNotReceive().Set();
        AssertTheLogRecordsIt(actedOnFiles: false);
    }

    [Fact]
    public async Task A_delete_where_the_check_before_it_took_every_file_back_leaves_the_mark()
    {
        var mark = Substitute.For<IFirstRunMark>();
        var delete = Delete(new DeleteResult(2, NoErrors));
        var batchCheck = CliRunFixtures.BatchCheckStanding(
            new ReverifyResult(Array.Empty<string>(), new[] { File1, File2 }));

        var exitCode = await Run("/d", null, Services(mark, delete: delete, batchCheck: batchCheck));

        Assert.Equal(CliExitCode.Ok, exitCode);
        // The run finished without asking the service, so the count it went on is nought.
        await delete.DidNotReceive().DeleteFilesAsync(
            Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        mark.DidNotReceive().Set();
        AssertTheLogRecordsIt(actedOnFiles: false);
    }

    [Fact]
    public async Task A_cancelled_delete_that_deleted_a_file_sets_the_mark()
    {
        using var cts = new CancellationTokenSource();
        var mark = Substitute.For<IFirstRunMark>();
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new DeleteResult(1, NoErrors, Cancelled: true);
            });

        var exitCode = await Run("/d", null, Services(mark, delete: delete), cts.Token);

        Assert.Equal(CliExitCode.Partial, exitCode);
        mark.Received(1).Set();
        AssertTheLogRecordsIt(actedOnFiles: true);
    }

    [Fact]
    public async Task A_delete_cancelled_before_its_first_file_leaves_the_mark()
    {
        using var cts = new CancellationTokenSource();
        var mark = Substitute.For<IFirstRunMark>();
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new DeleteResult(0, NoErrors, Cancelled: true);
            });

        var exitCode = await Run("/d", null, Services(mark, delete: delete), cts.Token);

        Assert.Equal(CliExitCode.Cancelled, exitCode);
        mark.DidNotReceive().Set();
        AssertTheLogRecordsIt(actedOnFiles: false);
    }

    [Fact]
    public async Task A_move_that_moved_files_sets_the_mark()
    {
        var mark = Substitute.For<IFirstRunMark>();

        var exitCode = await Run("/m", Destination, Services(mark, move: Move(new MoveResult(2, NoErrors))));

        Assert.Equal(CliExitCode.Ok, exitCode);
        mark.Received(1).Set();
        AssertTheLogRecordsIt(actedOnFiles: true);
    }

    [Fact]
    public async Task A_move_in_which_every_file_failed_leaves_the_mark()
    {
        var mark = Substitute.For<IFirstRunMark>();
        var failed = new MoveResult(0, new FileOperationError[] { new UnknownError(File1), new UnknownError(File2) });

        var exitCode = await Run("/m", Destination, Services(mark, move: Move(failed)));

        Assert.Equal(CliExitCode.Error, exitCode);
        mark.DidNotReceive().Set();
        AssertTheLogRecordsIt(actedOnFiles: false);
    }

    [Fact]
    public async Task A_cancelled_move_that_moved_a_file_sets_the_mark()
    {
        using var cts = new CancellationTokenSource();
        var mark = Substitute.For<IFirstRunMark>();
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new MoveResult(1, NoErrors, Cancelled: true);
            });

        var exitCode = await Run("/m", Destination, Services(mark, move: move), cts.Token);

        Assert.Equal(CliExitCode.Partial, exitCode);
        mark.Received(1).Set();
        AssertTheLogRecordsIt(actedOnFiles: true);
    }

    [Fact]
    public async Task A_move_the_app_stopped_after_moving_a_file_sets_the_mark()
    {
        var mark = Substitute.For<IFirstRunMark>();

        var exitCode = await Run("/m", Destination, Services(mark, move: StoppedMove(moved: 1)));

        Assert.Equal(CliExitCode.Partial, exitCode);
        mark.Received(1).Set();
        AssertTheLogRecordsIt(actedOnFiles: true);
    }

    [Fact]
    public async Task A_move_the_app_stopped_before_moving_a_file_leaves_the_mark()
    {
        var mark = Substitute.For<IFirstRunMark>();

        var exitCode = await Run("/m", Destination, Services(mark, move: StoppedMove(moved: 0)));

        Assert.Equal(CliExitCode.Error, exitCode);
        mark.DidNotReceive().Set();
        AssertTheLogRecordsIt(actedOnFiles: false);
    }

    /// <summary>
    /// Failures no named catch reports, among them the base types of the app's own
    /// refusals, raised the way the framework raises them.
    /// </summary>
    public static TheoryData<string> UnforeseenFailures => new() { "invalid operation", "access refused" };

    private static Exception Unforeseen(string failure) => failure switch
    {
        "invalid operation" => new InvalidOperationException("planted"),
        _ => new UnauthorizedAccessException("planted"),
    };

    [Theory]
    [MemberData(nameof(UnforeseenFailures))]
    public async Task A_delete_whose_service_fails_before_its_count_comes_back_sets_the_mark(string failure)
    {
        var mark = Substitute.For<IFirstRunMark>();
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns<DeleteResult>(_ => throw Unforeseen(failure));

        var exitCode = await Run("/d", null, Services(mark, delete: delete));

        Assert.Equal(CliExitCode.Error, exitCode);
        mark.Received(1).Set();
        // The run's entry is the failure line, which names no count.
        AssertTheLogRecordsIt(actedOnFiles: false);
    }

    [Theory]
    [MemberData(nameof(UnforeseenFailures))]
    public async Task A_move_whose_service_fails_before_its_count_comes_back_sets_the_mark(string failure)
    {
        var mark = Substitute.For<IFirstRunMark>();
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns<MoveResult>(_ => throw Unforeseen(failure));

        var exitCode = await Run("/m", Destination, Services(mark, move: move));

        Assert.Equal(CliExitCode.Error, exitCode);
        mark.Received(1).Set();
        // The run's entry is the failure line, which names no count.
        AssertTheLogRecordsIt(actedOnFiles: false);
    }

    public static TheoryData<string, string> RefusalsBeforeTheFirstFile => new()
    {
        { "/m", "refused" }, { "/m", "unwritable" }, { "/m", "cancelled" }, { "/m", "task cancelled" },
        { "/d", "refused" }, { "/d", "unwritable" }, { "/d", "cancelled" }, { "/d", "task cancelled" },
    };

    [Theory]
    [MemberData(nameof(RefusalsBeforeTheFirstFile))]
    public async Task A_batch_the_service_refuses_or_cancels_before_its_first_file_leaves_the_mark(
        string arg, string refusal)
    {
        // The app's own refusals and a cancellation, which the services raise ahead of
        // any file. Their twins are the unforeseen failures above, which set the mark.
        Exception thrown = refusal switch
        {
            "refused" => new LocalisedInvalidOperationException("refused"),
            "unwritable" => new LocalisedAccessException("unwritable"),
            "cancelled" => new OperationCanceledException(),
            _ => new TaskCanceledException(),
        };
        var mark = Substitute.For<IFirstRunMark>();
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns<DeleteResult>(_ => throw thrown);
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns<MoveResult>(_ => throw thrown);

        await Run(arg, arg == "/m" ? Destination : null, Services(mark, delete: delete, move: move));

        mark.DidNotReceive().Set();
        AssertTheLogRecordsIt(actedOnFiles: false);
    }

    [Fact]
    public async Task A_delete_that_fails_in_the_check_before_acting_leaves_the_mark()
    {
        // The same failure, met one step earlier, before the batch is handed over.
        var mark = Substitute.For<IFirstRunMark>();
        var delete = Delete(new DeleteResult(2, NoErrors));
        var batchCheck = Substitute.For<IBatchCheck>();
        batchCheck.RunAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>(),
                Arg.Any<IProgress<BatchCheckProgress>>(), Arg.Any<CancellationToken>())
            .Returns<BatchCheckResult>(_ => throw new InvalidOperationException("planted"));

        var exitCode = await Run("/d", null, Services(mark, delete: delete, batchCheck: batchCheck));

        Assert.Equal(CliExitCode.Error, exitCode);
        await delete.DidNotReceive().DeleteFilesAsync(
            Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        mark.DidNotReceive().Set();
        AssertTheLogRecordsIt(actedOnFiles: false);
    }

    [Fact]
    public async Task A_scan_leaves_the_mark()
    {
        var mark = Substitute.For<IFirstRunMark>();

        var exitCode = await Run("/s", null, Services(mark));

        // The scan offered both files and listed them, which is as far as /s goes.
        Assert.Equal(CliExitCode.Ok, exitCode);
        mark.DidNotReceive().Set();
        AssertTheLogRecordsIt(actedOnFiles: false);
    }

    // ---- fixtures ----

    /// <summary>
    /// The window's start check reads the command line's entries back from the log
    /// (<see cref="CommandLineRunRecord"/>), so it has to count a run's entries where
    /// the run's service counted a file moved or deleted and nowhere else, and an entry
    /// it counts has to carry an Event ID its query asks the log for. The entries are
    /// the ones this run wrote, as the recorder took them.
    /// </summary>
    private static void AssertTheLogRecordsIt(bool actedOnFiles)
    {
        var entries = EventLogRecorder.Entries;
        Assert.NotEmpty(entries);
        var counted = entries.Where(e => CommandLineRunRecord.ActedOnFiles(e.Text)).ToList();
        Assert.Equal(actedOnFiles, counted.Count > 0);
        Assert.All(counted, e => Assert.Contains(e.Class, new[] { CliEventClass.Ok, CliEventClass.Partial }));
    }

    private static readonly FileOperationError[] NoErrors = Array.Empty<FileOperationError>();

    private static IDeleteFilesService Delete(DeleteResult result)
    {
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(result);
        return delete;
    }

    private static IMoveFilesService Move(MoveResult result)
    {
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(result);
        return move;
    }

    /// <summary>A Move one of the service's destination guards stopped part way.</summary>
    private static IMoveFilesService StoppedMove(int moved)
    {
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns<MoveResult>(_ => throw new MoveAbortedException(
                "stopped", new MoveResult(moved, NoErrors),
                Destination, MoveAbortReason.ResolvesElsewhere));
        return move;
    }

    private static async Task<int> Run(
        string arg, string? destination, IServiceProvider services, CancellationToken token = default)
    {
        var command = arg switch
        {
            "/s" => CliCommand.ScanOnly,
            "/m" => CliCommand.Move,
            _ => CliCommand.Delete,
        };
        // Standard output is swapped for a buffer so the run's lines stay out of the test
        // runner's own output; nothing here reads them.
        EventLogRecorder.Clear();
        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            return await Program.RunWorkAsync(
                arg, new CliInvocation(command, null, destination), token, services, () => false);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    /// <summary>
    /// The services the work path resolves. The scan offers two files, the
    /// signals read clean and, unless a test passes its own, the check
    /// before acting stands over both, so the run reaches whichever service the test scripted.
    /// </summary>
    private static IServiceProvider Services(
        IFirstRunMark mark, IDeleteFilesService? delete = null, IMoveFilesService? move = null,
        IBatchCheck? batchCheck = null)
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

        var reboot = Substitute.For<IPendingRebootService>();
        reboot.Check().Returns(PendingRebootResult.Clean);

        batchCheck ??= CliRunFixtures.BatchCheckStanding(
            new ReverifyResult(new[] { File1, File2 }, Array.Empty<string>()));

        return CliRunFixtures.Services(scan, reboot: reboot, batchCheck: batchCheck,
            delete: delete, move: move, firstRunMark: mark);
    }
}
