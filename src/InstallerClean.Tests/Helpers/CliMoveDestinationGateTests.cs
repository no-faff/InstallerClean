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
/// THE CONTROL IS NOT DECORATION. Every refusal here asserts that the scan was
/// never reached, and a service that is never called at all satisfies that on
/// its own. The accepted destination takes the same fixture through to a
/// completed move, so what the refusals show is the gates answering rather than
/// a fixture that goes nowhere.
///
/// THE CACHE FOLDER TAKES A CASE OF ITS OWN because the gate's predicate admits
/// the folder as well as anything under it, and a fixture built by joining a
/// child name onto it exercises one of those two. The event-log line for that
/// gate is held here as well: it carries the path the run was given, which the
/// sentence on stdout does not, so it is the only record of where a refused run
/// was pointed.
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
    /// The cache folder itself, the predicate's other arm. Through
    /// <c>GetFullPath</c> for the same reason the two above go through it: the
    /// gate ahead of this one takes only a fully qualified path, and the folder's
    /// own spelling is not one on every host.
    /// </summary>
    private static readonly string TheCacheFolderItself =
        Path.GetFullPath(InstallerCacheHelpers.InstallerFolder);

    /// <summary>
    /// A destination that reaches the gate without looking as though it should:
    /// an ordinary folder that Windows has been told to stand for the cache folder,
    /// so following it lands inside the cache while the text of the path says
    /// nothing about it. The line prints what the caller typed, which is this.
    /// </summary>
    private const string LinkedIntoTheCache = @"D:\backup";

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
    public async Task The_cache_folder_itself_is_refused_before_the_scan()
    {
        // The case above is a path UNDER the folder. This one is the folder, which
        // the predicate takes through its other arm. It is neither empty nor
        // relative, so the two gates ahead of this one pass it and the installer
        // gate is the first that can answer.
        var (services, scan, move) = Fixture();

        var (exitCode, stdout) = await RunMove(TheCacheFolderItself, services);

        Assert.Equal(CliExitCode.Error, exitCode);
        Assert.Contains(Strings.Cli_MoveDestinationInsideInstaller, stdout);
        await scan.DidNotReceive().ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
        await move.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The three destinations that reach the installer gate, which is more than the
    /// one shape the name suggests.
    /// </summary>
    /// <remarks>
    /// A path under the folder is the ordinary one. The folder ITSELF is admitted
    /// too, by the predicate's other arm. And a path that shares nothing with the
    /// folder's name reaches it when the folder it points at is reached through a
    /// junction: Windows lets a folder anywhere on the machine stand for another
    /// one, the gate follows that link before it decides, and the line prints the
    /// path the caller typed rather than the one it led to.
    /// </remarks>
    public static TheoryData<string> DestinationsTheGateRefuses() => new()
    {
        InsideInstallerCache,
        TheCacheFolderItself,
        LinkedIntoTheCache,
    };

    [Theory]
    [MemberData(nameof(DestinationsTheGateRefuses))]
    public void The_installer_gate_event_log_line_names_the_mode_the_path_and_the_folder(
        string destination)
    {
        // Read through the en-GB scope the emit site wraps it in, so what is
        // asserted is what a machine reads.
        var line = MachineContract.English(
            () => Program.MoveDestinationInsideInstallerEventLogLine("/m", destination));

        // Which run and which path, neither of which the sentence on stdout carries.
        Assert.StartsWith("/m mode aborted:", line, StringComparison.Ordinal);
        Assert.Contains(destination, line, StringComparison.Ordinal);
        // And the folder in words. A machine whose Windows sits on another drive has
        // its cache somewhere other than C:, and the third destination above shares
        // no text with the folder at all, so a spelled path answers for one machine
        // and one destination shape rather than for the ones this line is written on.
        Assert.Contains("the Windows Installer folder", line, StringComparison.Ordinal);
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
