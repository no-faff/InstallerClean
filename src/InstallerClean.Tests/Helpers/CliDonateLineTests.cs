using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The command line's tip line: printed last, after a blank line, by a Delete that
/// deleted files and a Move that moved files, and only to a console somebody is
/// watching. Where the run goes on to ask about its report or to send one, the line
/// follows that instead (CliReportCarryTests). Driven through the real work method with
/// substitute services, the watched answer passed in, since standard output is
/// redirected under a test runner.
///
/// A RUN THAT REACHES THE LINE'S GATE IS MADE TWICE, watched and not, and the two exit
/// codes are compared: the line is output and nothing else, so it must not move the
/// code a script reads.
///
/// STDOUT IS READ BACK THROUGH <c>Console.SetOut</c>, WHICH IS PROCESS-GLOBAL. The
/// assembly disables test parallelisation for a reason of its own, in
/// AssemblyInfo.cs, and that is what makes the capture safe here.
/// </summary>
public class CliDonateLineTests
{
    /// <summary>
    /// Temp is fully qualified on either host and outside both forbidden sets, so
    /// the destination gates pass it. Nothing is created and nothing is written: the
    /// move service is a substitute.
    /// </summary>
    private static readonly string Destination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-donate-test");

    private const string File1 = @"C:\Windows\Installer\a.msi";
    private const string File2 = @"C:\Windows\Installer\b.msp";

    private static readonly PatchClaim SurvivingA =
        new(File1, "{AAAA1111-0000-0000-0000-000000000001}", "{PPPP1111-0000-0000-0000-000000000001}", null, 2);
    private static readonly PatchClaim SurvivingB =
        new(File2, "{AAAA1111-0000-0000-0000-000000000002}", "{PPPP1111-0000-0000-0000-000000000001}", null, 2);

    private static string DonateLine => string.Format(Strings.Cli_DonateAsk, SupportLink.Url);

    [Fact]
    public async Task A_delete_that_deleted_files_ends_with_the_tip_line()
    {
        var (exitCode, stdout) = await Run("/d", null, Services(delete: DeleteThatDeletes(2)), watched: true);
        var (unwatchedExitCode, _) = await Run("/d", null, Services(delete: DeleteThatDeletes(2)), watched: false);

        var deleted = string.Format(
            DisplayHelpers.Pluralise(2, Strings.Cli_DeletedFiles, "Cli.DeletedFiles"),
            DisplayHelpers.FormatCount(2), DisplayHelpers.PluraliseFile(2));
        Assert.Contains(deleted, stdout);
        Assert.EndsWith(Environment.NewLine + Environment.NewLine + DonateLine + Environment.NewLine, stdout);
        Assert.True(stdout.IndexOf(deleted, StringComparison.Ordinal)
            < stdout.IndexOf(DonateLine, StringComparison.Ordinal));
        Assert.Equal(CliExitCode.Ok, exitCode);
        Assert.Equal(exitCode, unwatchedExitCode);
    }

    [Fact]
    public async Task The_tip_line_never_reaches_the_application_log()
    {
        EventLogRecorder.Clear();
        var (_, stdout) = await Run("/d", null, Services(delete: DeleteThatDeletes(2)), watched: true);
        var entries = EventLogRecorder.Entries;

        Assert.Contains(DonateLine, stdout);
        // The run's summary entry is there, so the recorder was listening to this run.
        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.DoesNotContain(SupportLink.Url, e.Text));
    }

    [Fact]
    public async Task A_delete_nobody_is_watching_prints_no_tip_line()
    {
        // The control for the test above: the same run, the same services, and only
        // the watched answer differs.
        var (_, stdout) = await Run("/d", null, Services(delete: DeleteThatDeletes(2)), watched: false);

        Assert.Contains(string.Format(
            DisplayHelpers.Pluralise(2, Strings.Cli_DeletedFiles, "Cli.DeletedFiles"),
            DisplayHelpers.FormatCount(2), DisplayHelpers.PluraliseFile(2)), stdout);
        Assert.DoesNotContain(SupportLink.Url, stdout);
    }

    [Fact]
    public async Task A_move_that_moved_files_ends_with_the_tip_line_after_the_restore_hint()
    {
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));

        var (exitCode, stdout) = await Run("/m", Destination, Services(move: move), watched: true);
        var (unwatchedExitCode, _) = await Run("/m", Destination, Services(move: move), watched: false);

        var restore = string.Format(Strings.Cli_MoveRestoreHint, Destination);
        Assert.Contains(restore, stdout);
        Assert.EndsWith(Environment.NewLine + Environment.NewLine + DonateLine + Environment.NewLine, stdout);
        Assert.True(stdout.IndexOf(restore, StringComparison.Ordinal)
            < stdout.IndexOf(DonateLine, StringComparison.Ordinal));
        Assert.Equal(CliExitCode.Ok, exitCode);
        Assert.Equal(exitCode, unwatchedExitCode);
    }

    [Fact]
    public async Task A_delete_that_deleted_nothing_prints_no_tip_line()
    {
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, new FileOperationError[] { new UnknownError(File1), new UnknownError(File2) }));

        var (exitCode, stdout) = await Run("/d", null, Services(delete: delete), watched: true);
        var (unwatchedExitCode, _) = await Run("/d", null, Services(delete: delete), watched: false);

        Assert.DoesNotContain(SupportLink.Url, stdout);
        Assert.Equal(exitCode, unwatchedExitCode);
    }

    [Fact]
    public async Task A_move_the_app_stopped_prints_no_tip_line()
    {
        // Files reached the folder before the stop, so the count alone would ask.
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns<MoveResult>(_ => throw new MoveAbortedException(
                "stopped", new MoveResult(1, Array.Empty<FileOperationError>()),
                Destination, MoveAbortReason.ResolvesElsewhere));

        var (exitCode, stdout) = await Run("/m", Destination, Services(move: move), watched: true);
        var (unwatchedExitCode, _) = await Run("/m", Destination, Services(move: move), watched: false);

        Assert.DoesNotContain(SupportLink.Url, stdout);
        Assert.Equal(exitCode, unwatchedExitCode);
    }

    [Fact]
    public async Task A_cancelled_delete_that_deleted_files_prints_no_tip_line()
    {
        // The cancellation stays the last thing on the screen.
        using var cts = new CancellationTokenSource();
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true);
            });

        var (exitCode, stdout) = await Run("/d", null, Services(delete: delete), watched: true, cts.Token);

        Assert.Contains(Strings.Cli_Cancelled, stdout);
        Assert.DoesNotContain(SupportLink.Url, stdout);
        Assert.Equal(CliExitCode.Partial, exitCode);
    }

    [Fact]
    public async Task A_cancelled_move_that_moved_files_prints_no_tip_line()
    {
        // The undo stays the last thing on the screen.
        using var cts = new CancellationTokenSource();
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new MoveResult(1, Array.Empty<FileOperationError>(), Cancelled: true);
            });

        var (exitCode, stdout) = await Run("/m", Destination, Services(move: move), watched: true, cts.Token);

        Assert.Contains(string.Format(Strings.Cli_MoveCancelledRestoreHint, Destination), stdout);
        Assert.DoesNotContain(SupportLink.Url, stdout);
        Assert.Equal(CliExitCode.Partial, exitCode);
    }

    [Fact]
    public void The_tip_line_prints_the_address_as_given()
    {
        // The address goes in through a slot, so a value that dropped or doubled the
        // slot would print a line with no address, or throw where the run prints it.
        Assert.Contains(SupportLink.Url, DonateLine, StringComparison.Ordinal);
        Assert.Equal(1, DonateLine.Split(SupportLink.Url).Length - 1);
    }

    // ---- fixtures ----

    private static IDeleteFilesService DeleteThatDeletes(int deleted)
    {
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(deleted, Array.Empty<FileOperationError>()));
        return delete;
    }

    private static async Task<(int ExitCode, string Stdout)> Run(
        string arg, string? destination, IServiceProvider services, bool watched,
        CancellationToken token = default)
    {
        var command = arg == "/m" ? CliCommand.Move : CliCommand.Delete;
        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            var exitCode = await Program.RunWorkAsync(
                arg, new CliInvocation(command, null, destination), token, services, () => watched);
            return (exitCode, buffer.ToString());
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    /// <summary>
    /// The services the work path resolves. The scan offers two files, the
    /// pending-reboot gate is clean and the re-verify keeps both, so the run
    /// reaches whichever service the test scripted.
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
