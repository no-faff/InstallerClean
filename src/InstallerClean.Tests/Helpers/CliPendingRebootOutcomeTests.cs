using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What a held run RETURNS, and the event class declared beside its exit code, driven
/// from the enum itself.
///
/// CliPendingRebootStringsTests walks the same enum twice, once per string surface, so
/// a reason added without its sentence or without its label is a red test. The emitter
/// reads the exit code and the entry class off the reason as well, in one expression,
/// and a member it does not name falls to the arm that promises a caller nothing about
/// coming back. So a reason added later still takes both without anybody choosing them,
/// and the two string surfaces give no cover here: a member can carry a sentence and a
/// label and still have had no thought given to what a scheduler should do about it.
///
/// THE TABLE IS THE DECISION AND THE WALK IS THE GATE. One row per reason; a member
/// with no row fails below, and writing its row is the choice being made.
///
/// HOW EACH COLUMN IS HELD. Both are read back from a real run driven through the work
/// method: the exit code it returns, and the class of the one entry it writes, as the
/// suite's recorder receives it. The event class is also held against the exit code
/// declared beside it: its Event ID has to sit in the band that code's entries go out in.
/// </summary>
public class CliPendingRebootOutcomeTests
{
    private const string OfferA = @"C:\Windows\Installer\offer-a.msi";
    private const string OfferB = @"C:\Windows\Installer\offer-b.msi";

    private static readonly Dictionary<PendingRebootReason, (int Exit, CliEventClass Class)> Declared = new()
    {
        [PendingRebootReason.MsiExecuteMutexHeld]     = (CliExitCode.Transient, CliEventClass.TransientSkip),
        [PendingRebootReason.MsiExecuteMutexAccessRefused] = (CliExitCode.Error, CliEventClass.HardError),
        [PendingRebootReason.InstallerInProgress]     = (CliExitCode.Transient, CliEventClass.TransientSkip),
        [PendingRebootReason.PendingRenameInCache]    = (CliExitCode.Transient, CliEventClass.TransientSkip),
        [PendingRebootReason.PendingRenameUnresolved] = (CliExitCode.Transient, CliEventClass.TransientSkip),
        [PendingRebootReason.RegistryCheckUnreadable] = (CliExitCode.Error, CliEventClass.HardError),
        [PendingRebootReason.InstallerInProgressMarker] = (CliExitCode.Transient, CliEventClass.TransientSkip),
        [PendingRebootReason.InstallerInProgressMarkerAccessRefused] = (CliExitCode.Error, CliEventClass.HardError),
        [PendingRebootReason.MsiExecuteMutexUnreadable] = (CliExitCode.Error, CliEventClass.HardError),
        [PendingRebootReason.InstallerServiceRunning] = (CliExitCode.Transient, CliEventClass.TransientSkip),
        [PendingRebootReason.InstallerServiceUnreadable] = (CliExitCode.Error, CliEventClass.HardError),
    };

    [Fact]
    public void Every_reason_has_a_row_in_the_table()
    {
        var undeclared = Enum.GetValues<PendingRebootReason>()
            .Where(reason => !Declared.ContainsKey(reason))
            .ToList();

        // Named rather than counted, so the failure says which member needs the
        // decision rather than only that one does.
        Assert.True(undeclared.Count == 0,
            "No exit code or event class has been chosen for: " + string.Join(", ", undeclared));
    }

    /// <summary>
    /// The band an exit code's entry belongs in, taken from the pairs the host writes:
    /// Ok and Partial go out with a 1000-band class, Transient and Cancelled with the
    /// 2000, Error with the 4000.
    ///
    /// EVERY MEMBER OF CliExitCode IS HERE AND NOT ONLY THE ONE THE TABLE USES, so a
    /// row that declares a different code is held rather than passed over by a check
    /// shaped around the single value in front of it today.
    /// </summary>
    private static readonly Dictionary<int, int> BandForExit = new()
    {
        [CliExitCode.Ok] = 1000,
        [CliExitCode.Partial] = 1000,
        [CliExitCode.Transient] = 2000,
        [CliExitCode.Cancelled] = 2000,
        [CliExitCode.Error] = 4000,
    };

    [Fact]
    public void Every_row_declares_an_event_class_whose_band_matches_its_exit_code()
    {
        // Each row's event class has to have its Event ID in the band BandForExit gives
        // the exit code beside it, so the class column cannot hold a value its own row's
        // exit code contradicts.
        var wrong = new List<string>();

        foreach (var (reason, declared) in Declared)
        {
            if (!BandForExit.TryGetValue(declared.Exit, out var band))
            {
                wrong.Add($"{reason}: exit {declared.Exit} is in no band this test knows");
                continue;
            }

            var id = CliContract.EventIdFor(declared.Class);
            if (id / 1000 * 1000 != band)
                wrong.Add($"{reason}: exit {declared.Exit} belongs in the {band} band "
                    + $"and {declared.Class} is Event ID {id}");
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }

    [Fact]
    public async Task A_held_run_exits_with_the_code_and_writes_the_class_declared_for_its_reason()
    {
        var wrong = new List<string>();

        foreach (var reason in Enum.GetValues<PendingRebootReason>())
        {
            if (!Declared.TryGetValue(reason, out var expected)) continue;

            var (exit, _, entries) = await Run("/d", PendingRebootResult.Block(reason));
            if (exit != expected.Exit) wrong.Add($"{reason}: expected exit {expected.Exit}, got {exit}");
            // A held run writes its summary and nothing beside it.
            if (entries.Count != 1)
                wrong.Add($"{reason}: expected one entry, got {entries.Count}");
            else if (entries[0].Class != expected.Class)
                wrong.Add($"{reason}: expected {expected.Class}, got {entries[0].Class}");
        }

        // Every reason is walked before anything is asserted, so one wrong code does
        // not hide the rest of the table behind it.
        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }

    [Fact]
    public async Task A_reason_the_check_has_never_met_still_holds_the_run()
    {
        // Cast past the enum's members deliberately, the way the strings tests do:
        // this is the state a new reason arrives in. Whatever else is undecided, the
        // run must not go on to touch the folder, so the code is asserted to be a
        // holding one rather than to be any particular value.
        const PendingRebootReason unwritten = (PendingRebootReason)99;

        var (exit, _, _) = await Run("/d", PendingRebootResult.Block(unwritten));

        Assert.NotEqual(CliExitCode.Ok, exit);
        Assert.NotEqual(CliExitCode.Partial, exit);
    }

    /// <summary>
    /// A check that ends on a blocked read of Windows Installer's signals refuses the run
    /// with that reason's own sentence, exit code and entry, for every reason, and neither
    /// action service is reached. Both files are offered, so the run that goes on to act is
    /// one where nothing but the read stops it.
    /// </summary>
    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_check_that_ends_on_a_blocked_signal_read_ends_the_run_without_acting(string arg)
    {
        var wrong = new List<string>();

        foreach (var reason in Enum.GetValues<PendingRebootReason>())
        {
            var sentence = Program.PendingRebootBlockedMessage(arg, reason, detail: null);
            var run = await RunToTheEnd(arg, PendingRebootResult.Block(reason));

            if (run.ExitCode != Declared[reason].Exit)
                wrong.Add($"{reason}: exit {run.ExitCode}, expected {Declared[reason].Exit}");
            if (!run.Stdout.Contains(sentence))
                wrong.Add($"{reason}: output lacks the reason's sentence");
            if (run.Entries.Count != 1 || run.Entries[0].Class != Declared[reason].Class)
                wrong.Add($"{reason}: the entry is not the one declared");
            if (run.Acted)
                wrong.Add($"{reason}: an action service was called");
        }

        // The control: a check that stands goes on to the action service.
        var clean = await RunToTheEnd(arg, signals: null);
        if (!clean.Acted) wrong.Add("a check that stood did not reach the action service");

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }

    /// <summary>
    /// A move destination fully qualified on either host and outside both forbidden
    /// sets, so the destination gates pass it. Nothing is created there: the move
    /// service is a substitute.
    /// </summary>
    private static readonly string Destination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-pending-reboot-outcome-test");

    /// <summary>
    /// Runs <paramref name="arg"/> to its end against a batch check that ends on
    /// <paramref name="signals"/>, as a check does that cannot wait out a blocked read of
    /// Windows Installer's signals. Returns the exit code, stdout and the entries the run
    /// wrote.
    /// </summary>
    private static async Task<(int ExitCode, string Stdout, IReadOnlyList<(CliEventClass Class, string Text)> Entries)> Run(
        string arg, PendingRebootResult signals)
    {
        var (exitCode, stdout, entries, _) = await RunToTheEnd(arg, signals);
        return (exitCode, stdout, entries);
    }

    /// <summary>
    /// <see cref="Run"/>, with a check that stands over both files where
    /// <paramref name="signals"/> is null, and whether the run reached either action
    /// service.
    /// </summary>
    private static async Task<(int ExitCode, string Stdout, IReadOnlyList<(CliEventClass Class, string Text)> Entries, bool Acted)> RunToTheEnd(
        string arg, PendingRebootResult? signals)
    {
        // The offer has to be non-empty or the run returns on "nothing to do" before
        // ever reaching the check.
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                [new OrphanedFile(OfferA, 1024, false, false, false, "unclaimed"),
                 new OrphanedFile(OfferB, 1024, false, false, false, "unclaimed")],
                Array.Empty<RegisteredPackage>(), 0));

        // A check that stands keeps both files, so a run it lets through reaches the
        // action service with a batch to act on.
        var batchCheck = signals is null
            ? CliRunFixtures.BatchCheckStandingOverEverything()
            : CliRunFixtures.BatchCheckEnding(BatchEndReason.SignalWaitCannotClear, signals);

        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, Array.Empty<FileOperationError>()));

        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new MoveResult(0, Array.Empty<FileOperationError>()));

        var services = CliRunFixtures.Services(scan, batchCheck: batchCheck,
            delete: delete, move: move);

        var invocation = arg == "/m"
            ? new CliInvocation(CliCommand.Move, null, Destination)
            : new CliInvocation(CliCommand.Delete, null, null);

        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            EventLogRecorder.Clear();
            var exitCode = await Program.RunWorkAsync(arg, invocation, CancellationToken.None, services);
            var acted = delete.ReceivedCalls().Any() || move.ReceivedCalls().Any();
            return (exitCode, buffer.ToString(), EventLogRecorder.Entries, acted);
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
