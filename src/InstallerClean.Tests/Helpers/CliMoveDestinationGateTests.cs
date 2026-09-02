using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The command line's own destination gates, driven through the real work method
/// with the destination a scheduled task would carry.
///
/// THE REFUSAL IS ASSERTED ON THE SENTENCE THE RUN PRINTS, and the exit code is
/// the weaker half. A destination inside the cache folder sits inside
/// <c>%SystemRoot%</c> as well, so the system-folder gate is entitled to the same
/// path and answers with the same exit code; the two carry a sentence each, and
/// the sentence is what says which gate the run met.
///
/// THE THIRD TEST IS THE CONTROL AND IT IS NOT DECORATION. Both refusals assert
/// that the scan was never reached, and a service that is never called at all
/// satisfies that on its own. The accepted destination takes the same fixture
/// through to a completed move, so what the two refusals show is the gates
/// answering rather than a fixture that goes nowhere.
///
/// STDOUT IS READ BACK THROUGH <c>Console.SetOut</c>, WHICH IS PROCESS-GLOBAL.
/// The assembly disables test parallelisation for a reason of its own, in
/// AssemblyInfo.cs, and that is what makes the capture safe here.
/// </summary>
public class CliMoveDestinationGateTests
{
    /// <summary>
    /// A destination inside the installer cache, and one inside a system folder,
    /// each built from the folder the gate itself asks Windows about, so these are
    /// the same tests on whichever host runs them. The gate ahead of the two takes
    /// only a fully qualified path, and a drive-letter spelling is not one off
    /// Windows while a slash-rooted one is not one on it.
    /// </summary>
    private static readonly string InsideInstallerCache = Path.GetFullPath(
        Path.Combine(InstallerCacheHelpers.InstallerFolder, "installerclean-cli-gate-test"));

    private static readonly string InsideSystemFolder = Path.GetFullPath(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "installerclean-cli-gate-test"));

    /// <summary>
    /// The destination the gates accept, temp being fully qualified on either host
    /// and outside both forbidden sets. Nothing is created and nothing is written:
    /// the move service is a substitute.
    /// </summary>
    private static readonly string AcceptedDestination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-gate-test");

    private const string File1 = @"C:\Windows\Installer\a.msi";
    private const string File2 = @"C:\Windows\Installer\b.msp";

    private static readonly PatchClaim SurvivingA =
        new(File1, "{AAAA1111-0000-0000-0000-000000000001}", "{PPPP1111-0000-0000-0000-000000000001}", null, 2);
    private static readonly PatchClaim SurvivingB =
        new(File2, "{AAAA1111-0000-0000-0000-000000000002}", "{PPPP1111-0000-0000-0000-000000000001}", null, 2);

    [Fact]
    public async Task A_move_destination_inside_the_installer_cache_is_refused_before_the_scan()
    {
        var (services, scan, move) = Fixture();

        var (exitCode, stdout) = await RunMove(InsideInstallerCache, services);

        Assert.Equal(CliExitCode.Error, exitCode);
        Assert.Contains(Strings.Cli_MoveDestinationInsideInstaller, stdout);
        await scan.DidNotReceive().ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
        await move.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_move_destination_inside_a_system_folder_is_refused_before_the_scan()
    {
        var (services, scan, move) = Fixture();

        var (exitCode, stdout) = await RunMove(InsideSystemFolder, services);

        Assert.Equal(CliExitCode.Error, exitCode);
        Assert.Contains(
            string.Format(Strings.Cli_MoveDestinationInSystemFolder, InsideSystemFolder), stdout);
        await scan.DidNotReceive().ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
        await move.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_accepted_destination_takes_the_same_fixture_through_to_the_move()
    {
        var (services, scan, move) = Fixture();

        var (exitCode, _) = await RunMove(AcceptedDestination, services);

        Assert.Equal(CliExitCode.Ok, exitCode);
        await scan.Received(1).ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
        await move.Received(1).MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), AcceptedDestination, Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    // ---- fixtures ----

    /// <summary>
    /// Runs <c>/m</c> against <paramref name="destination"/> with stdout redirected,
    /// and hands back the exit code and what the run wrote. The console goes back
    /// whatever happens.
    /// </summary>
    private static async Task<(int ExitCode, string Stdout)> RunMove(
        string destination, IServiceProvider services)
    {
        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            var exitCode = await Program.RunWorkAsync(
                "/m",
                new CliInvocation(CliCommand.Move, null, destination),
                CancellationToken.None,
                services);
            return (exitCode, buffer.ToString());
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    /// <summary>
    /// The services the work path resolves for a <c>/m</c>. The scan offers two
    /// files, the pending-reboot gate is clean, the re-verify keeps both, and the
    /// move service reports them moved, so a run that gets past the destination
    /// gates completes.
    /// </summary>
    private static (IServiceProvider Services, IFileSystemScanService Scan, IMoveFilesService Move) Fixture()
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

        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));

        var services = new ServiceCollection()
            .AddSingleton(scan)
            .AddSingleton(reboot)
            .AddSingleton(reverifier)
            .AddSingleton(move)
            .AddSingleton(Substitute.For<IDeleteFilesService>())
            .AddSingleton(Substitute.For<ISettingsService>())
            .BuildServiceProvider();

        return (services, scan, move);
    }
}
