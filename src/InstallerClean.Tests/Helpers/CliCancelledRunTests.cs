using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What a cancelled run leaves on stdout, for both commands, driven through the
/// real work method with a service that reports a partial batch, and the two lines
/// such a run writes to the Application channel.
///
/// THE ORDER IS PART OF WHAT IS ASSERTED. A run says what it did and then says it
/// was cancelled, so each test pins the position of the two sentences against each
/// other rather than their presence alone: a summary printed after the cancellation
/// reads as a second run.
///
/// A CANCELLED MOVE CARRIES THE UNDO AND NOT THE KEEP-THE-MOVE LINE, and the two
/// halves are asserted together. The keep-the-move sentence asks the reader to check
/// their programs and then delete the backup, which is what to do after a run that
/// went the distance; its absence is pinned against the moved line's presence, so it
/// answers for the cancel rather than for the zero-count gate that also suppresses
/// it. The undo sentence takes its place, and its POSITION is asserted too: after the
/// cancellation, so the reader learns the run stopped before being told what to do.
///
/// STDOUT IS READ BACK THROUGH <c>Console.SetOut</c>, WHICH IS PROCESS-GLOBAL. The
/// assembly disables test parallelisation for a reason of its own, in
/// AssemblyInfo.cs, and that is what makes the capture safe here.
/// </summary>
public class CliCancelledRunTests
{
    /// <summary>
    /// Temp is fully qualified on either host and outside both forbidden sets, so
    /// the destination gates pass it and these are the same tests wherever they
    /// run. Nothing is created and nothing is written: the move service is a
    /// substitute.
    /// </summary>
    private static readonly string Destination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-cancelled-test");

    private const string File1 = @"C:\Windows\Installer\a.msi";
    private const string File2 = @"C:\Windows\Installer\b.msp";

    private static readonly PatchClaim SurvivingA =
        new(File1, "{AAAA1111-0000-0000-0000-000000000001}", "{PPPP1111-0000-0000-0000-000000000001}", null, 2);
    private static readonly PatchClaim SurvivingB =
        new(File2, "{AAAA1111-0000-0000-0000-000000000002}", "{PPPP1111-0000-0000-0000-000000000001}", null, 2);

    [Fact]
    public async Task A_cancelled_move_says_what_it_moved_and_then_how_to_undo_it()
    {
        // The Ctrl+C arrives while the service is working, which is the sequence
        // the host is written for: the token is cancelled and the service hands
        // back what it managed rather than throwing.
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

        var (exitCode, stdout) = await Run("/m", Destination, Services(move: move), cts.Token);

        var moved = string.Format(
            DisplayHelpers.Pluralise(1, Strings.Cli_MovedFiles, "Cli.MovedFiles"),
            1, DisplayHelpers.PluraliseFile(1));
        var restore = string.Format(Strings.Cli_MoveRestoreHint, Destination);

        var undo = string.Format(Strings.Cli_MoveCancelledRestoreHint, Destination);

        Assert.Equal(CliExitCode.Partial, exitCode);
        Assert.Contains(moved, stdout);
        Assert.Contains(Strings.Cli_Cancelled, stdout);
        Assert.Contains(undo, stdout);
        // THE ORDER IS THE CLAIM. What ran, then that it stopped, then how to put it
        // back: a reader is told the run was cancelled before being told what to do
        // about it, and the undo is the last thing on the screen because it is what
        // they act on.
        Assert.True(stdout.IndexOf(moved, StringComparison.Ordinal)
            < stdout.IndexOf(Strings.Cli_Cancelled, StringComparison.Ordinal));
        Assert.True(stdout.IndexOf(Strings.Cli_Cancelled, StringComparison.Ordinal)
            < stdout.IndexOf(undo, StringComparison.Ordinal));
        // A file did move, which is the other gate on the keep-the-move line, so its
        // absence answers for the cancel and for nothing else.
        Assert.DoesNotContain(restore, stdout);
    }

    [Fact]
    public async Task A_cancelled_move_that_moved_nothing_offers_no_undo()
    {
        // Nothing reached the destination, so there is nothing to put back and the
        // sentence would name a folder the run never wrote to. This is the control
        // that keeps the test above answering for the cancel rather than for the
        // mere presence of a destination on the command line: the same /m, the same
        // folder, the same cancel, and no undo line.
        using var cts = new CancellationTokenSource();
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new MoveResult(0, Array.Empty<FileOperationError>(), Cancelled: true);
            });

        var (_, stdout) = await Run("/m", Destination, Services(move: move), cts.Token);

        Assert.Contains(Strings.Cli_Cancelled, stdout);
        Assert.DoesNotContain(
            string.Format(Strings.Cli_MoveCancelledRestoreHint, Destination), stdout);
    }

    [Fact]
    public async Task A_cancelled_delete_offers_no_undo_line_at_all()
    {
        // The undo is a Move sentence and a Delete has no undo, so the line is gated
        // on a destination this path never sets. Held here because the two commands
        // share one cancellation handler, which is where a line meant for one of them
        // reaches the other.
        using var cts = new CancellationTokenSource();
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true);
            });

        var (_, stdout) = await Run("/d", null, Services(delete: delete), cts.Token);

        Assert.Contains(Strings.Cli_Cancelled, stdout);
        Assert.DoesNotContain("It's simple to undo", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_move_that_was_not_cancelled_does_carry_the_keep_the_move_line()
    {
        // The control on the test above: the same run without the cancel prints the
        // line, so its absence there is the cancel and not the sentence having gone.
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => new MoveResult(1, Array.Empty<FileOperationError>(), Cancelled: false));

        var (_, stdout) = await Run("/m", Destination, Services(move: move), CancellationToken.None);

        Assert.Contains(string.Format(Strings.Cli_MoveRestoreHint, Destination), stdout);
        Assert.DoesNotContain(Strings.Cli_Cancelled, stdout);
    }

    [Fact]
    public async Task A_cancelled_delete_says_how_many_files_it_deleted()
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

        var (exitCode, stdout) = await Run("/d", null, Services(delete: delete), cts.Token);

        var deleted = string.Format(
            DisplayHelpers.Pluralise(1, Strings.Cli_DeletedFiles, "Cli.DeletedFiles"),
            1, DisplayHelpers.PluraliseFile(1));

        Assert.Equal(CliExitCode.Partial, exitCode);
        Assert.Contains(deleted, stdout);
        Assert.Contains(Strings.Cli_Cancelled, stdout);
        Assert.True(stdout.IndexOf(deleted, StringComparison.Ordinal)
            < stdout.IndexOf(Strings.Cli_Cancelled, StringComparison.Ordinal));
    }

    [Fact]
    public void The_cancelled_audit_lines_say_where_the_cancellation_came_from()
    {
        // Console.CancelKeyPress is raised for either control key and the handler
        // never reads which one it was, so neither line is in a position to name
        // one. Composed here exactly as the two write sites compose them, so a
        // slot the value stopped spelling would throw here rather than in the
        // Application channel.
        var partial = MachineContract.English(() => string.Format(
            Strings.Cli_EventLogCancelledPartial,
            "/m", 1, 2, DisplayHelpers.PluraliseFile(2)));
        var noWork = MachineContract.English(
            () => string.Format(Strings.Cli_EventLogCancelledNoWork, "/d"));

        foreach (var line in new[] { partial, noWork })
        {
            Assert.DoesNotContain("Ctrl+", line, StringComparison.OrdinalIgnoreCase);
            // Beside the absence, so the absence is attributable: a line that had
            // stopped saying how the run ended would satisfy the assertion above
            // on its own.
            Assert.Contains("cancelled at the console", line, StringComparison.Ordinal);
        }
    }

    // ---- fixtures ----

    private static async Task<(int ExitCode, string Stdout)> Run(
        string arg, string? destination, IServiceProvider services, CancellationToken token)
    {
        var command = arg == "/m" ? CliCommand.Move : CliCommand.Delete;
        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            var exitCode = await Program.RunWorkAsync(
                arg, new CliInvocation(command, null, destination), token, services);
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

        return new ServiceCollection()
            .AddSingleton(scan)
            .AddSingleton(reboot)
            .AddSingleton(reverifier)
            .AddSingleton(delete ?? Substitute.For<IDeleteFilesService>())
            .AddSingleton(move ?? Substitute.For<IMoveFilesService>())
            .AddSingleton(Substitute.For<ISettingsService>())
            .BuildServiceProvider();
    }
}
