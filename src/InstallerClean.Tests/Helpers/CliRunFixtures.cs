using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What the command-line tests share when they drive the real work method
/// (<see cref="InstallerClean.Cli.Program.RunWorkAsync"/>): the services it resolves, and
/// which of the entries a run writes is its summary.
/// </summary>
internal static class CliRunFixtures
{
    /// <summary>
    /// The services the work method resolves. Each one a test leaves out is a bare
    /// substitute, scripted to nothing, and the settings service always is one.
    /// </summary>
    internal static IServiceProvider Services(
        IFileSystemScanService scan,
        IPendingRebootService? reboot = null,
        IBatchCheck? batchCheck = null,
        IDeleteFilesService? delete = null,
        IMoveFilesService? move = null,
        IFirstRunMark? firstRunMark = null) =>
        new ServiceCollection()
            .AddSingleton(scan)
            .AddSingleton(reboot ?? Substitute.For<IPendingRebootService>())
            .AddSingleton(batchCheck ?? BatchCheckStandingOverEverything())
            .AddSingleton(delete ?? Substitute.For<IDeleteFilesService>())
            .AddSingleton(move ?? Substitute.For<IMoveFilesService>())
            .AddSingleton(Substitute.For<ISettingsService>())
            .AddSingleton(firstRunMark ?? Substitute.For<IFirstRunMark>())
            .BuildServiceProvider();

    /// <summary>
    /// A batch check that stands over exactly the paths it is given and drops none, a new
    /// result for each call.
    /// </summary>
    internal static IBatchCheck BatchCheckStandingOverEverything() =>
        BatchCheckStanding(files => new ReverifyResult(files, Array.Empty<string>()));

    /// <summary>
    /// A batch check that stands with whatever <paramref name="result"/> makes of the paths
    /// it is given, a new result for each call.
    /// </summary>
    internal static IBatchCheck BatchCheckStanding(Func<IReadOnlyList<string>, ReverifyResult> result)
    {
        var batchCheck = Substitute.For<IBatchCheck>();
        batchCheck.RunAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>(),
                Arg.Any<IProgress<BatchCheckProgress>>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(StandingChecks.Stood(result(ci.Arg<IReadOnlyList<string>>()))));
        return batchCheck;
    }

    /// <summary>A batch check that stands with <paramref name="result"/>, a new copy of it for each call.</summary>
    internal static IBatchCheck BatchCheckStanding(ReverifyResult result) => BatchCheckStanding(_ => result);

    /// <summary>A batch check that ends with no check standing, for the reason and signals given.</summary>
    internal static IBatchCheck BatchCheckEnding(BatchEndReason reason, PendingRebootResult? signals = null)
    {
        var batchCheck = Substitute.For<IBatchCheck>();
        batchCheck.RunAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>(),
                Arg.Any<IProgress<BatchCheckProgress>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(StandingChecks.Ended(reason, signals)));
        return batchCheck;
    }

    /// <summary>
    /// The services the work method resolves for a scan returning <paramref name="scan"/>: a
    /// clean read of Windows Installer's signals and a batch check standing over every file the scan offers, so the
    /// run reaches whichever service the test scripted.
    /// </summary>
    internal static IServiceProvider ServicesKeepingTheOffer(
        ScanResult scan, IDeleteFilesService? delete = null, IMoveFilesService? move = null)
    {
        var scanService = Substitute.For<IFileSystemScanService>();
        scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(scan);
        var reboot = Substitute.For<IPendingRebootService>();
        reboot.Check().Returns(PendingRebootResult.Clean);
        return Services(scanService, reboot: reboot, batchCheck: BatchCheckStandingOverEverything(),
            delete: delete, move: move);
    }

    /// <summary>
    /// Whether an entry of <paramref name="entryClass"/> is a run's summary: Ok, Partial,
    /// TransientSkip or HardError, the four outcome classes, each run writing exactly one.
    /// Every other class is a notice.
    /// </summary>
    internal static bool IsSummary(CliEventClass entryClass) =>
        entryClass is CliEventClass.Ok or CliEventClass.Partial
            or CliEventClass.TransientSkip or CliEventClass.HardError;

    /// <inheritdoc cref="IsSummary(CliEventClass)"/>
    internal static bool IsSummary((CliEventClass Class, string Text) entry) => IsSummary(entry.Class);

    /// <summary>
    /// Runs <paramref name="run"/> with an Application log that refuses the entries
    /// <paramref name="refuses"/> picks, as a log Group Policy or a stopped service refuses,
    /// and records the rest in <see cref="EventLogRecorder"/>. The log starts with no entry
    /// refused and the note saying so not yet printed. A refused entry marks the log
    /// unavailable, as a refused write to the real log does.
    /// </summary>
    internal static async Task<T> WithTheLogRefusing<T>(Func<CliEventClass, bool> refuses, Func<Task<T>> run)
    {
        // The sink, the flag and the note's latch are process-global; the assembly disables
        // test parallelisation.
        using var state = EventLogRecorder.FreshLogState();
        EventLogWriter.Sink = (entry, text) =>
        {
            if (refuses(entry)) throw new InvalidOperationException("The log refused the entry.");
            EventLogRecorder.Sink(entry, text);
        };
        return await run();
    }
}
