using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// A Move or Delete standing for an older copy, driven through the real work method with
/// a cover (<see cref="IRunCover"/>) the test sets: asked before the batch, it stops a run
/// it does not cover with nothing done, and where it stops covering the run during the batch
/// the batch stops before its next file. Either way the run says how far it got and names
/// no cause, not that it was cancelled.
/// </summary>
/// <remarks>
/// Stdout is read back through <c>Console.SetOut</c>, which is process-global, and the log
/// through <see cref="EventLogRecorder"/>; the assembly disables test parallelisation,
/// which is what makes both safe here.
/// </remarks>
public class CliRunCoverTests
{
    /// <summary>
    /// Temp is fully qualified on either host and outside both forbidden sets, so the
    /// destination gates pass it. Nothing is created: the move service is a substitute.
    /// </summary>
    private static readonly string Destination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-cover-test");

    private const string File1 = @"C:\Windows\Installer\a.msi";
    private const string File2 = @"C:\Windows\Installer\b.msp";

    private static string Lost(string arg, int done, int total) => arg == "/m"
        ? string.Format(Strings.Cli_MutexLostMove, DisplayHelpers.FormatCount(done),
            DisplayHelpers.FormatCount(total), DisplayHelpers.PluraliseFile(total), Destination)
        : string.Format(Strings.Cli_MutexLostDelete, DisplayHelpers.FormatCount(done),
            DisplayHelpers.FormatCount(total), DisplayHelpers.PluraliseFile(total));

    private static string NoWork(string arg) =>
        arg == "/m" ? Strings.Cli_CoverLostNoWorkMove : Strings.Cli_CoverLostNoWorkDelete;

    private static string NoWorkEntry(string arg) =>
        MachineContract.English(() => string.Format(Strings.Cli_EventLogCoverLostNoWork, arg));

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_run_its_cover_does_not_cover_stops_with_nothing_done(string arg)
    {
        var delete = Substitute.For<IDeleteFilesService>();
        var move = Substitute.For<IMoveFilesService>();
        var cover = new Cover { Covered = false };

        var run = await Run(arg, Services(delete, move), CancellationToken.None, cover);

        Assert.Equal(CliExitCode.Transient, run.ExitCode);
        Assert.Contains(NoWork(arg), run.Stdout);
        Assert.DoesNotContain(Strings.Cli_MutexBlocked, run.Stdout);
        Assert.DoesNotContain(Strings.Cli_Cancelled, run.Stdout);
        Assert.Equal((CliEventClass.TransientSkip, NoWorkEntry(arg)),
            Assert.Single(run.Entries, e => CliRunFixtures.IsSummary(e)));
        Assert.Empty(delete.ReceivedCalls());
        Assert.Empty(move.ReceivedCalls());
    }

    [Fact]
    public async Task A_covered_delete_asks_before_each_file_and_runs_its_batch()
    {
        var cover = new Cover();
        var asked = new List<bool>();
        var delete = Delete(call =>
        {
            var mayAct = call.Arg<Func<string, bool>?>()!;
            asked.Add(mayAct(File1));
            asked.Add(mayAct(File2));
            return new DeleteResult(2, Array.Empty<FileOperationError>());
        });

        var run = await Run("/d", Services(delete: delete), CancellationToken.None, cover);

        Assert.Equal(CliExitCode.Ok, run.ExitCode);
        Assert.Equal([true, true], asked);
        Assert.DoesNotContain(Strings.Cli_MutexBlocked, run.Stdout);
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_cover_lost_once_the_batch_has_finished_leaves_the_run_its_ordinary_summary(string arg)
    {
        // Lost fires as the batch returns in full, before anything after it runs: nothing
        // after the batch is cancelled by it, so the run ends as one that went the distance.
        var cover = new Cover();
        var services = arg == "/m"
            ? Services(move: Move(call =>
            {
                cover.LoseIt();
                return new MoveResult(2, Array.Empty<FileOperationError>());
            }))
            : Services(delete: Delete(call =>
            {
                cover.LoseIt();
                return new DeleteResult(2, Array.Empty<FileOperationError>());
            }));

        var run = await Run(arg, services, CancellationToken.None, cover);

        Assert.True(cover.Lost.IsCancellationRequested);
        Assert.Equal(CliExitCode.Ok, run.ExitCode);
        Assert.DoesNotContain(Lost(arg, 2, 2), run.Stdout);
        Assert.DoesNotContain(NoWork(arg), run.Stdout);
        Assert.DoesNotContain(Strings.Cli_Cancelled, run.Stdout);
        var summary = Assert.Single(run.Entries, e => CliRunFixtures.IsSummary(e));
        Assert.Equal(CliContract.ClassifyFileOperation(2, 0).EventClass, summary.Class);
    }

    [Fact]
    public async Task A_delete_whose_cover_is_lost_after_a_file_stops_and_says_how_far_it_got()
    {
        var cover = new Cover();
        var delete = Delete(call =>
        {
            var mayAct = call.Arg<Func<string, bool>?>()!;
            Assert.True(mayAct(File1));
            cover.Covered = false;
            Assert.False(mayAct(File2));
            return new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true);
        });

        var run = await Run("/d", Services(delete: delete), CancellationToken.None, cover);

        Assert.Equal(CliExitCode.Partial, run.ExitCode);
        Assert.Contains(Lost("/d", 1, 2), run.Stdout);
        Assert.DoesNotContain(Strings.Cli_MutexBlocked, run.Stdout);
        Assert.DoesNotContain(Strings.Cli_Cancelled, run.Stdout);
        Assert.Equal(
            (CliEventClass.Partial, MachineContract.English(() => string.Format(
                Strings.Cli_EventLogMutexLostPartial, "/d", 1, 2, DisplayHelpers.PluraliseFile(2)))),
            Assert.Single(run.Entries, e => CliRunFixtures.IsSummary(e)));
    }

    [Fact]
    public async Task A_move_whose_cover_is_lost_after_a_file_says_where_it_moved_them_and_offers_no_undo()
    {
        var cover = new Cover();
        var move = Move(call =>
        {
            var mayAct = call.Arg<Func<string, bool>?>()!;
            Assert.True(mayAct(File1));
            cover.Covered = false;
            Assert.False(mayAct(File2));
            return new MoveResult(1, Array.Empty<FileOperationError>(), Cancelled: true);
        });

        var run = await Run("/m", Services(move: move), CancellationToken.None, cover);

        Assert.Equal(CliExitCode.Partial, run.ExitCode);
        Assert.Contains(Lost("/m", 1, 2), run.Stdout);
        Assert.DoesNotContain(Strings.Cli_MutexBlocked, run.Stdout);
        Assert.DoesNotContain(string.Format(Strings.Cli_MoveCancelledRestoreHint, Destination), run.Stdout);
        Assert.DoesNotContain(Strings.Cli_Cancelled, run.Stdout);
    }

    [Fact]
    public async Task A_delete_whose_cover_is_lost_before_any_file_stops_with_nothing_done()
    {
        var cover = new Cover();
        var delete = Delete(call =>
        {
            cover.Covered = false;
            Assert.False(call.Arg<Func<string, bool>?>()!(File1));
            return new DeleteResult(0, Array.Empty<FileOperationError>(), Cancelled: true);
        });

        var run = await Run("/d", Services(delete: delete), CancellationToken.None, cover);

        Assert.Equal(CliExitCode.Transient, run.ExitCode);
        Assert.Contains(NoWork("/d"), run.Stdout);
        Assert.DoesNotContain(Strings.Cli_MutexBlocked, run.Stdout);
        Assert.Equal((CliEventClass.TransientSkip, NoWorkEntry("/d")),
            Assert.Single(run.Entries, e => CliRunFixtures.IsSummary(e)));
    }

    [Fact]
    public async Task Ctrl_C_during_a_covered_batch_is_still_a_cancellation()
    {
        using var cts = new CancellationTokenSource();
        var cover = new Cover();
        var delete = Delete(_ =>
        {
            cts.Cancel();
            return new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true);
        });

        var run = await Run("/d", Services(delete: delete), cts.Token, cover);

        Assert.Equal(CliExitCode.Partial, run.ExitCode);
        Assert.Contains(Strings.Cli_Cancelled, run.Stdout);
        Assert.DoesNotContain(Strings.Cli_MutexBlocked, run.Stdout);
    }

    private static IDeleteFilesService Delete(Func<NSubstitute.Core.CallInfo, DeleteResult> answer)
    {
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(), Arg.Any<Func<string, bool>?>())
            .Returns(call => answer(call));
        return delete;
    }

    private static IMoveFilesService Move(Func<NSubstitute.Core.CallInfo, MoveResult> answer)
    {
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<string>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(), Arg.Any<Func<string, bool>?>())
            .Returns(call => answer(call));
        return move;
    }

    /// <summary>
    /// A cover the test sets: <see cref="Covered"/> is what every question answers, and
    /// <see cref="LoseIt"/> fires <see cref="Lost"/>.
    /// </summary>
    private sealed class Cover : IRunCover
    {
        private readonly CancellationTokenSource _lost = new();

        public bool Covered { get; set; } = true;

        public bool Covers() => Covered;

        public CancellationToken Lost => _lost.Token;

        public void LoseIt()
        {
            Covered = false;
            _lost.Cancel();
        }
    }

    private sealed record Ran(int ExitCode, string Stdout, IReadOnlyList<(CliEventClass Class, string Text)> Entries);

    private static async Task<Ran> Run(
        string arg, IServiceProvider services, CancellationToken token, IRunCover cover)
    {
        var command = arg == "/m" ? CliCommand.Move : CliCommand.Delete;
        var original = Console.Out;
        using var buffer = new StringWriter();
        using var logState = EventLogRecorder.FreshLogState();
        try
        {
            Console.SetOut(buffer);
            EventLogRecorder.Clear();
            var exitCode = await Program.RunWorkAsync(
                arg, new CliInvocation(command, null, arg == "/m" ? Destination : null), token, services,
                cover: cover);
            return new Ran(exitCode, buffer.ToString(), EventLogRecorder.Entries);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    /// <summary>
    /// The services the work path resolves. The scan offers two files, the
    /// signals read clean and the check before the batch stands over both, so the run reaches the batch.
    /// </summary>
    private static IServiceProvider Services(
        IDeleteFilesService? delete = null, IMoveFilesService? move = null)
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

        var batchCheck = CliRunFixtures.BatchCheckStanding(
            new ReverifyResult(new[] { File1, File2 }, Array.Empty<string>()));

        return CliRunFixtures.Services(scan, reboot: reboot, batchCheck: batchCheck,
            delete: delete, move: move);
    }
}
