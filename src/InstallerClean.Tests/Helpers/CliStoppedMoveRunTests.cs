using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What a Move stopped by one of the service's own destination guards leaves on
/// stdout, driven through the real work method with a service that throws.
///
/// THE ORDER IS THE CLAIM, and it is the same claim a cancelled run makes in
/// CliCancelledRunTests: what the run did, then that it went no further, then what
/// to do about the files it had already moved. The last of the three is the one the
/// reader acts on, and a line telling somebody to check their programs and delete
/// the backup folder reads as the end of a finished run when it stands above the
/// sentence saying the run stopped.
///
/// The two sentences name DIFFERENT FOLDERS and that is deliberate, so each is
/// asserted against its own path: the guard's sentence names the path the run was
/// given, asking the reader to go and look at what they set, and the line under it
/// names where the files actually went.
///
/// STDOUT IS READ BACK THROUGH <c>Console.SetOut</c>, WHICH IS PROCESS-GLOBAL. The
/// assembly disables test parallelisation for a reason of its own, in
/// AssemblyInfo.cs, and that is what makes the capture safe here.
/// </summary>
public class CliStoppedMoveRunTests
{
    /// <summary>
    /// The path the run is given. Temp is fully qualified on either host and outside
    /// both forbidden sets, so the destination gates pass it. Nothing is created and
    /// nothing is written: the move service is a substitute.
    /// </summary>
    private static readonly string Destination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-stopped-test");

    /// <summary>
    /// Where the batch actually put the files, which is what the guard carries and
    /// what the folder-naming line has to print. Deliberately not the path above.
    /// </summary>
    private const string WhereTheFilesWent = @"E:\where-they-really-went";

    private const string File1 = @"C:\Windows\Installer\a.msi";
    private const string File2 = @"C:\Windows\Installer\b.msp";

    private static readonly PatchClaim SurvivingA =
        new(File1, "{AAAA1111-0000-0000-0000-000000000001}", "{PPPP1111-0000-0000-0000-000000000001}", null, 2);
    private static readonly PatchClaim SurvivingB =
        new(File2, "{AAAA1111-0000-0000-0000-000000000002}", "{PPPP1111-0000-0000-0000-000000000001}", null, 2);

    [Fact]
    public async Task A_stopped_move_says_it_went_no_further_before_naming_the_folder_to_delete()
    {
        var move = MoveThatStopsAfter(1);

        var (_, stdout) = await Run(Services(move));

        var moved = string.Format(
            DisplayHelpers.Pluralise(1, Strings.Cli_MovedFiles, "Cli.MovedFiles"),
            1, DisplayHelpers.PluraliseFile(1));
        var stopped = string.Format(Strings.Cli_DestinationChangedMidBatch, Destination);
        var folder = string.Format(Strings.Cli_MoveRestoreHint, WhereTheFilesWent);

        Assert.Contains(moved, stdout);
        Assert.Contains(stopped, stdout);
        Assert.Contains(folder, stdout);

        Assert.True(
            stdout.IndexOf(moved, StringComparison.Ordinal)
                < stdout.IndexOf(stopped, StringComparison.Ordinal),
            "the summary is printed after the sentence saying the run stopped");
        Assert.True(
            stdout.IndexOf(stopped, StringComparison.Ordinal)
                < stdout.IndexOf(folder, StringComparison.Ordinal),
            "the folder to delete is named before the sentence saying the run stopped");
    }

    [Fact]
    public async Task A_stopped_move_that_moved_nothing_names_no_folder_to_delete()
    {
        // The control on the test above, and the gate it pins is the count rather
        // than the stop: a run that put no file in the folder has not made one worth
        // naming. Asserted beside the guard's own sentence, so the absence answers
        // for the count and not for the line having gone.
        var move = MoveThatStopsAfter(0);

        var (_, stdout) = await Run(Services(move));

        Assert.Contains(string.Format(Strings.Cli_DestinationChangedMidBatch, Destination), stdout);
        Assert.DoesNotContain(
            string.Format(Strings.Cli_MoveRestoreHint, WhereTheFilesWent), stdout);
    }

    private static IMoveFilesService MoveThatStopsAfter(int moved)
    {
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns<MoveResult>(_ => throw new MoveAbortedException(
                "stopped", new MoveResult(moved, Array.Empty<FileOperationError>()),
                WhereTheFilesWent, MoveAbortReason.ResolvesElsewhere));
        return move;
    }

    private static IServiceProvider Services(IMoveFilesService move)
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
            .AddSingleton(Substitute.For<IDeleteFilesService>())
            .AddSingleton(move)
            .AddSingleton(Substitute.For<ISettingsService>())
            .BuildServiceProvider();
    }

    private static async Task<(int ExitCode, string Stdout)> Run(IServiceProvider services)
    {
        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            var exitCode = await Program.RunWorkAsync(
                "/m", new CliInvocation(CliCommand.Move, null, Destination),
                CancellationToken.None, services);
            return (exitCode, buffer.ToString());
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
