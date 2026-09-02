using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What a Ctrl+C leaves on stdout, for both commands, driven through the real
/// work method with a service that reports a partial batch.
///
/// THE ORDER IS PART OF WHAT IS ASSERTED. A run says what it did and then says it
/// was cancelled, so each test pins the position of the two sentences against each
/// other rather than their presence alone: a summary printed after the cancellation
/// reads as a second run.
///
/// A CANCELLED MOVE NAMES THE FOLDER. The files it moved are in the destination
/// and the operator needs the path to go and look, so the line that names it is
/// asserted with the destination in it rather than by its sentence alone.
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
    public async Task A_cancelled_move_says_what_it_moved_and_names_the_backup_folder()
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

        Assert.Equal(CliExitCode.Partial, exitCode);
        Assert.Contains(moved, stdout);
        Assert.Contains(restore, stdout);
        Assert.Contains(Strings.Cli_Cancelled, stdout);
        Assert.True(stdout.IndexOf(moved, StringComparison.Ordinal)
            < stdout.IndexOf(Strings.Cli_Cancelled, StringComparison.Ordinal));
        Assert.True(stdout.IndexOf(restore, StringComparison.Ordinal)
            < stdout.IndexOf(Strings.Cli_Cancelled, StringComparison.Ordinal));
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
