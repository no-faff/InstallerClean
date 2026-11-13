using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What a scan that stops on its own leaves the command line doing, for each of the
/// three commands, driven through the real work method with a scan that throws. Every
/// stop the app raises itself reaches the same catch, which prints its message, so one
/// of each of its two exception types stands for all of them here: a listing of the
/// Installer folder the scan could not take whole, and Windows Installer refusing to
/// list what is installed. A stop the framework raises reaches the last catch, which
/// names its type and never prints its message.
///
/// STDOUT IS READ BACK THROUGH <c>Console.SetOut</c>, WHICH IS PROCESS-GLOBAL. The
/// assembly disables test parallelisation for a reason of its own, in
/// AssemblyInfo.cs, and that is what makes the capture safe here.
/// </summary>
public class CliStoppedScanTests
{
    /// <summary>
    /// Temp is fully qualified on either host and outside both forbidden sets, so the
    /// destination gates pass it and a Move reaches the scan. Nothing is created and
    /// nothing is written.
    /// </summary>
    private static readonly string Destination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-stopped-scan-test");

    [Theory]
    [InlineData("/s")]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_scan_that_stops_prints_its_account_and_exits_with_the_error_code(string arg)
    {
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new LocalisedInvalidOperationException(
                Strings.Error_ScanInstallerFolderListFailed, Strings.Error_NothingOfferedNothingRemoved,
                new UnauthorizedAccessException()));
        var delete = Substitute.For<IDeleteFilesService>();
        var move = Substitute.For<IMoveFilesService>();

        var (exitCode, stdout) = await Run(arg, Services(scan, delete, move));

        Assert.Equal(CliExitCode.Error, exitCode);
        Assert.Contains(
            StopMessages.NothingOfferedNothingRemoved(Strings.Error_ScanInstallerFolderListFailed), stdout);
        // Beside the account, so the absence answers for the stop and not for a run
        // that printed nothing at all.
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout);
        Assert.Empty(delete.ReceivedCalls());
        Assert.Empty(move.ReceivedCalls());
    }

    [Theory]
    [InlineData("/s")]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_scan_windows_installer_refuses_prints_its_account_and_exits_with_the_error_code(string arg)
    {
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new LocalisedAccessException(Strings.Error_MsiAccessDenied, Strings.Error_NothingRemoved));
        var delete = Substitute.For<IDeleteFilesService>();
        var move = Substitute.For<IMoveFilesService>();

        var (exitCode, stdout) = await Run(arg, Services(scan, delete, move));

        Assert.Equal(CliExitCode.Error, exitCode);
        Assert.Contains(StopMessages.NothingRemoved(Strings.Error_MsiAccessDenied), stdout);
        Assert.Empty(delete.ReceivedCalls());
        Assert.Empty(move.ReceivedCalls());
    }

    [Theory]
    [InlineData("/s")]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_scan_the_framework_refuses_exits_with_the_error_code_and_never_prints_its_message(string arg)
    {
        // The base type of the app's own access refusal, raised the way the framework
        // raises it, with a path out of another user's profile in its message.
        const string OtherProfilePath = @"C:\Users\someone-else\AppData\Local\private.txt";
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException($"Access to the path '{OtherProfilePath}' is denied."));
        var delete = Substitute.For<IDeleteFilesService>();
        var move = Substitute.For<IMoveFilesService>();

        var (exitCode, stdout) = await Run(arg, Services(scan, delete, move));

        Assert.Equal(CliExitCode.Error, exitCode);
        // The type is named, so the absence answers for a failure that was reported.
        Assert.Contains(nameof(UnauthorizedAccessException), stdout);
        Assert.DoesNotContain(OtherProfilePath, stdout);
        Assert.Empty(delete.ReceivedCalls());
        Assert.Empty(move.ReceivedCalls());
    }

    // ---- fixtures ----

    private static async Task<(int ExitCode, string Stdout)> Run(string arg, IServiceProvider services)
    {
        var invocation = arg switch
        {
            "/m" => new CliInvocation(CliCommand.Move, null, Destination),
            "/d" => new CliInvocation(CliCommand.Delete, null, null),
            _ => new CliInvocation(CliCommand.ScanOnly, null, null),
        };
        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            var exitCode = await Program.RunWorkAsync(arg, invocation, CancellationToken.None, services);
            return (exitCode, buffer.ToString());
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    private static IServiceProvider Services(
        IFileSystemScanService scan, IDeleteFilesService delete, IMoveFilesService move)
    {
        var reboot = Substitute.For<IPendingRebootService>();
        reboot.Check().Returns(PendingRebootResult.Clean);

        return CliRunFixtures.Services(scan, reboot: reboot, delete: delete, move: move);
    }
}
