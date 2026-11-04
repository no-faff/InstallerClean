using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using NSubstitute;
using NSubstitute.Core;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What the command line prints and logs where the scan, or the check made before a Move
/// or Delete, stopped waiting for a drive or share while files still had to be checked
/// against it, driven through the real work method.
///
/// WHAT EVERY FIXTURE HERE SETS UP IS THE PASS'S LIST OF DRIVES AND SHARES GIVEN UP, AND
/// HOW MANY FILES EACH KEPT. The command line names a root only where it kept a file, so a
/// fixture with a root and no kept file is a fixture for silence. The rest of each fixture
/// is the smallest scan, check and action that reach the branch under test.
///
/// THE EVENT LOG IS ASSERTED AS WHOLE ENGLISH SENTENCES. Those entries are machine-read and
/// built in English on every machine, so the words are the contract an RMM matches on.
/// stdout follows the machine's language, so it is asserted through the same helper the
/// run prints from.
/// </summary>
public class CliSourcesGivenUpTests
{
    private const string OfferA = @"C:\Windows\Installer\offer-a.msi";
    private const string OfferB = @"C:\Windows\Installer\offer-b.msi";
    private const string HeldA = @"C:\Windows\Installer\held-a.msi";
    private const string HeldB = @"C:\Windows\Installer\held-b.msi";
    private const string Share = @"\\nas\installers";

    /// <summary>
    /// Temp is fully qualified on either host and outside both forbidden sets, so the /m
    /// destination gates pass it. Nothing is created there: the move service is a
    /// substitute.
    /// </summary>
    private static readonly string Destination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-sources-given-up-test");

    // ---- the scan ----

    [Fact]
    public async Task A_scan_with_nothing_else_to_report_prints_the_count_then_the_line_in_place_of_the_clean_one()
    {
        // THE MACHINE THIS EXISTS FOR. The only file held back was kept for a program
        // Windows still has installed, which the command line reports nothing about, but
        // it was kept because the drive its program's packages are on did not answer. So
        // nothing about this folder was established clean.
        var result = NothingOfferedKeptFor(Given("D:", 1));

        var run = await Run("/s", result);

        Assert.Equal(CliExitCode.Ok, run.ExitCode);
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, run.Stdout, StringComparison.Ordinal);
        // No clean line: the count of files left alone comes straight after the scanning
        // line, and under it the line naming the drive.
        Assert.Equal(LeftAloneReport.Line(result.LeftAloneCount), Lines(run.Stdout)[1]);
        Assert.Equal(
            SourcesGivenUpReport.CommandLine([Given("D:", 1)]),
            Lines(run.Stdout)[2]);
    }

    [Fact]
    public async Task Its_outcome_entry_says_nothing_could_be_offered_and_its_notice_names_the_drive()
    {
        var run = await Run("/s", NothingOfferedKeptFor(Given("D:", 1)));

        Assert.Collection(run.Entries,
            notice => AssertEntry(notice, CliEventClass.SourcesGivenUpNotice,
                "/s mode: InstallerClean stopped waiting for 1 drive or share (drive D:) during the scan "
                + "and left alone the 1 file it still had to check against it."),
            summary => AssertEntry(summary, CliEventClass.Ok,
                "/s mode: nothing could be offered. InstallerClean stopped waiting for 1 drive or share "
                + "and left alone the 1 file it still had to check against it. No action taken."));
    }

    [Fact]
    public async Task More_than_one_drive_or_share_takes_the_plural_and_counts_every_file_once()
    {
        // The files are summed across the roots, each file having been counted at the one
        // root whose give-up stopped its check.
        var run = await Run("/s", NothingOfferedKeptFor(Given("d:", 2), Given(Share, 3)));

        Assert.Collection(run.Entries,
            notice => AssertEntry(notice, CliEventClass.SourcesGivenUpNotice,
                @"/s mode: InstallerClean stopped waiting for 2 drives or shares (drive D:, \\nas\installers) "
                + "during the scan and left alone the 5 files it still had to check against them."),
            summary => AssertEntry(summary, CliEventClass.Ok,
                "/s mode: nothing could be offered. InstallerClean stopped waiting for 2 drives or shares "
                + "and left alone the 5 files it still had to check against them. No action taken."));
        Assert.Contains(
            SourcesGivenUpReport.CommandLine([Given("d:", 2), Given(Share, 3)]),
            run.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task One_drive_keeping_several_files_takes_the_one_form()
    {
        // The form follows the number of drives and shares, and the noun after the file
        // count follows the file count, so the two are chosen apart.
        var run = await Run("/s", NothingOfferedKeptFor(Given("D:", 3)));

        Assert.Collection(run.Entries,
            notice => AssertEntry(notice, CliEventClass.SourcesGivenUpNotice,
                "/s mode: InstallerClean stopped waiting for 1 drive or share (drive D:) during the scan "
                + "and left alone the 3 files it still had to check against it."),
            summary => AssertEntry(summary, CliEventClass.Ok,
                "/s mode: nothing could be offered. InstallerClean stopped waiting for 1 drive or share "
                + "and left alone the 3 files it still had to check against it. No action taken."));
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_delete_or_move_with_nothing_offered_writes_the_same_outcome_under_its_own_flag(string arg)
    {
        // The branch with nothing offered returns before the check for all three flags,
        // so a delete or a move takes this outcome too, opening with its own flag.
        var run = await Run(arg, NothingOfferedKeptFor(Given("D:", 1)));

        Assert.Equal(CliExitCode.Ok, run.ExitCode);
        var summary = Assert.Single(run.Entries, entry => entry.Class == CliEventClass.Ok);
        Assert.Equal(
            $"{arg} mode: nothing could be offered. InstallerClean stopped waiting for 1 drive or share "
            + "and left alone the 1 file it still had to check against it. No action taken.",
            summary.Text);
    }

    [Fact]
    public async Task A_drive_given_up_with_no_file_kept_at_it_changes_nothing()
    {
        // It changed nothing the scan decided, so the machine is reported exactly as a
        // clean one is.
        var run = await Run("/s", NothingOfferedKeptFor(Given("D:", 0)));

        Assert.Contains(Strings.Cli_FoundNoOrphans, run.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Opening(Strings.Cli_SourceGivenUp_Drive), run.Stdout, StringComparison.Ordinal);
        var entry = Assert.Single(run.Entries);
        AssertEntry(entry, CliEventClass.Ok, "/s mode: no unneeded files identified.");
    }

    [Fact]
    public async Task A_scan_with_a_withholding_to_report_prints_the_count_and_then_the_line()
    {
        // Two copies the scan could not tell apart are reported, one of them because the
        // share their packages are on did not answer, and a superseded file was held back
        // too. The count of files left alone comes first, and the line naming the share
        // next.
        var result = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            WithheldCount: 1,
            WithheldFiles: Held(2),
            WithheldBy: new WithholdingSplit(SecondCopyUnestablishedCount: 2),
            SourceRootsGivenUp: [Given(Share, 1)]);

        var run = await Run("/s", result);

        // No clean line: the count of files left alone, then the line naming the share.
        var lines = Lines(run.Stdout);
        Assert.Equal(LeftAloneReport.Line(result.LeftAloneCount), lines[1]);
        Assert.Equal(SourcesGivenUpReport.CommandLine([Given(Share, 1)]), lines[2]);
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, run.Stdout, StringComparison.Ordinal);

        // The outcome counts the files held back, as it does on any such machine, and the
        // notice beside it names the share.
        var summary = Assert.Single(run.Entries, entry => entry.Class == CliEventClass.Ok);
        Assert.StartsWith("/s mode: nothing could be offered. InstallerClean could not establish",
            summary.Text, StringComparison.Ordinal);
        Assert.Single(run.Entries, entry => entry.Class == CliEventClass.SourcesGivenUpNotice);
    }

    [Fact]
    public async Task A_scan_that_offered_files_prints_the_line_ahead_of_its_found_line()
    {
        var result = new ScanResult(
            Offer(2), Array.Empty<RegisteredPackage>(), 0,
            SourceRootsGivenUp: [Given("E:", 1)]);

        var run = await Run("/s", result);

        var lines = Lines(run.Stdout);
        var found = Array.FindIndex(lines, l => l.StartsWith(Opening(Strings.Cli_FoundOrphans), StringComparison.Ordinal));
        Assert.True(found >= 1, run.Stdout);
        Assert.Equal(SourcesGivenUpReport.CommandLine([Given("E:", 1)]), lines[found - 1]);
        Assert.Single(run.Entries, entry => entry.Class == CliEventClass.SourcesGivenUpNotice);
    }

    // ---- the check made before acting ----

    [Fact]
    public async Task On_a_delete_the_check_s_line_follows_the_held_back_line()
    {
        var run = await Run("/d", Offered(2), CheckGivingUp(Given(Share, 1)),
            delete: new DeleteResult(1, Array.Empty<FileOperationError>()));

        AssertUnderTheHeldBackLine(run.Stdout, 1, Given(Share, 1), Opening(Strings.Cli_DeletedFiles));
        Assert.Collection(run.Entries,
            notice => AssertEntry(notice, CliEventClass.SourcesGivenUpNotice,
                @"/d mode: InstallerClean stopped waiting for 1 drive or share (\\nas\installers) during the "
                + "check made before acting and left alone the 1 file it still had to check against it."),
            summary => Assert.Equal(CliEventClass.Ok, summary.Class));
    }

    [Fact]
    public async Task On_a_move_the_check_s_line_follows_the_held_back_line()
    {
        var run = await Run("/m", Offered(2), CheckGivingUp(Given(Share, 1)),
            move: _ => new MoveResult(1, Array.Empty<FileOperationError>()));

        AssertUnderTheHeldBackLine(run.Stdout, 1, Given(Share, 1), Opening(Strings.Cli_MovedFiles));
        Assert.Single(run.Entries, entry => entry.Class == CliEventClass.SourcesGivenUpNotice);
    }

    [Fact]
    public async Task On_a_move_the_service_stopped_the_check_s_line_follows_the_held_back_line()
    {
        var run = await Run("/m", Offered(2), CheckGivingUp(Given(Share, 1)),
            move: _ => throw new MoveAbortedException(
                "stopped", new MoveResult(1, Array.Empty<FileOperationError>()),
                Destination, MoveAbortReason.ResolvesElsewhere));

        AssertUnderTheHeldBackLine(run.Stdout, 1, Given(Share, 1), Opening(Strings.Cli_MovedFiles));
        Assert.Single(run.Entries, entry => entry.Class == CliEventClass.SourcesGivenUpNotice);
    }

    [Fact]
    public async Task A_check_that_held_every_file_back_still_prints_its_line()
    {
        // Nothing is left to delete, and the run still says what it held back and why
        // some of it went unchecked.
        var run = await Run("/d", Offered(2),
            new ReverifyResult(
                Array.Empty<string>(), new[] { OfferA, OfferB },
                Reasons: new HeldBackReasons(FileNotConfirmed: 2),
                SourceRootsGivenUp: [Given("D:", 2)]),
            delete: new DeleteResult(0, Array.Empty<FileOperationError>()));

        AssertUnderTheHeldBackLine(run.Stdout, 2, Given("D:", 2), Opening(Strings.Cli_DeletedFiles));
    }

    [Fact]
    public async Task A_delete_whose_scan_and_check_each_gave_one_up_says_so_for_each_in_order()
    {
        // Two separate waits, which can be on two different drives, so each pass's line
        // and notice is its own.
        var scan = new ScanResult(
            Offer(2), Array.Empty<RegisteredPackage>(), 0,
            SourceRootsGivenUp: [Given("D:", 1)]);

        var run = await Run("/d", scan, CheckGivingUp(Given(Share, 1)),
            delete: new DeleteResult(1, Array.Empty<FileOperationError>()));

        var scanLine = run.Stdout.IndexOf(SourcesGivenUpReport.CommandLine([Given("D:", 1)]), StringComparison.Ordinal);
        var checkLine = run.Stdout.IndexOf(SourcesGivenUpReport.CommandLine([Given(Share, 1)]), StringComparison.Ordinal);
        Assert.True(scanLine >= 0 && checkLine > scanLine, run.Stdout);
        Assert.Collection(run.Entries,
            first => AssertEntry(first, CliEventClass.SourcesGivenUpNotice,
                "/d mode: InstallerClean stopped waiting for 1 drive or share (drive D:) during the scan "
                + "and left alone the 1 file it still had to check against it."),
            second => AssertEntry(second, CliEventClass.SourcesGivenUpNotice,
                @"/d mode: InstallerClean stopped waiting for 1 drive or share (\\nas\installers) during the "
                + "check made before acting and left alone the 1 file it still had to check against it."),
            summary => Assert.Equal(CliEventClass.Ok, summary.Class));
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_run_refused_after_the_check_still_writes_the_check_s_notice_and_prints_neither_line(string arg)
    {
        // The action service refuses the batch for want of the Windows Installer lock. The
        // console says nothing about files held back on that run, as the window shows no
        // card for it, and the Application log still records that the check left files
        // alone, which is true of a run that then touched nothing at all.
        var run = await Run(arg, Offered(2), CheckGivingUp(Given(Share, 1)),
            delete: new DeleteResult(0, Array.Empty<FileOperationError>(), InstallerLockUnavailable: true),
            move: _ => new MoveResult(0, Array.Empty<FileOperationError>(), InstallerLockUnavailable: true));

        Assert.Equal(CliExitCode.Transient, run.ExitCode);
        Assert.DoesNotContain(SourcesGivenUpReport.CommandLine([Given(Share, 1)]), run.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(HeldBackReport.Line(new HeldBackReasons(FileNotConfirmed: 1)), run.Stdout,
            StringComparison.Ordinal);
        Assert.Collection(run.Entries,
            notice => AssertEntry(notice, CliEventClass.SourcesGivenUpNotice,
                $@"{arg} mode: InstallerClean stopped waiting for 1 drive or share (\\nas\installers) during the "
                + "check made before acting and left alone the 1 file it still had to check against it."),
            summary => Assert.Equal(CliEventClass.TransientSkip, summary.Class));
    }

    [Fact]
    public async Task A_check_that_gave_up_a_share_keeping_no_file_says_nothing()
    {
        var run = await Run("/d", Offered(2),
            new ReverifyResult(new[] { OfferA, OfferB }, Array.Empty<string>(),
                SourceRootsGivenUp: [Given(Share, 0)]),
            delete: new DeleteResult(2, Array.Empty<FileOperationError>()));

        Assert.DoesNotContain(Opening(Strings.Cli_SourceGivenUp_Path), run.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(run.Entries, entry => entry.Class == CliEventClass.SourcesGivenUpNotice);
    }

    // ---- fixtures ----

    private static SourceRootGivenUp Given(string root, int filesKept) =>
        new(root, SourceRootGiveUpRoute.NoAnswer, filesKept);

    /// <summary>
    /// A scan that offered nothing and held back one file for each file the roots kept,
    /// every one of them kept for a program Windows still has installed: the reading the
    /// command line reports nothing about, so the roots are all that stand between the run
    /// and the clean line.
    /// </summary>
    private static ScanResult NothingOfferedKeptFor(params SourceRootGivenUp[] roots)
    {
        var kept = Math.Max(1, roots.Sum(root => root.FilesKept));
        return new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            WithheldFiles: Enumerable.Range(0, kept)
                .Select(i => new OrphanedFile($@"C:\Windows\Installer\kept-{i}.msi", 1024, false, false, false, "unclaimed"))
                .ToArray(),
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: kept),
            SourceRootsGivenUp: roots);
    }

    private static ScanResult Offered(int n) =>
        new(Offer(n), Array.Empty<RegisteredPackage>(), 0);

    private static OrphanedFile[] Offer(int n) =>
        new[] { OfferA, OfferB }.Take(n)
            .Select(path => new OrphanedFile(path, 1024, false, false, false, "unclaimed"))
            .ToArray();

    private static OrphanedFile[] Held(int n) =>
        new[] { HeldA, HeldB }.Take(n)
            .Select(path => new OrphanedFile(path, 1024, false, false, false, "unclaimed"))
            .ToArray();

    /// <summary>
    /// A check that holds the second offered file back, kept at <paramref name="root"/>.
    /// </summary>
    private static ReverifyResult CheckGivingUp(SourceRootGivenUp root) =>
        new(new[] { OfferA }, new[] { OfferB },
            Reasons: new HeldBackReasons(FileNotConfirmed: 1),
            SourceRootsGivenUp: [root]);

    // A sentence's words up to its first placeholder, which no name or count changes.
    private static string Opening(string value) => value[..value.IndexOf('{')];

    private static string[] Lines(string stdout) =>
        stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// The check's line is printed straight under the run's held-back line, which counts
    /// <paramref name="heldBackFiles"/> files, and ahead of the line reporting what the batch
    /// did, which opens with <paramref name="doneOpening"/>.
    /// </summary>
    private static void AssertUnderTheHeldBackLine(
        string stdout, int heldBackFiles, SourceRootGivenUp root, string doneOpening)
    {
        var lines = Lines(stdout);
        var heldBack = Array.IndexOf(lines,
            HeldBackReport.Line(new HeldBackReasons(FileNotConfirmed: heldBackFiles)));
        Assert.True(heldBack >= 0, $"No held-back line.\n{stdout}");
        Assert.Equal(SourcesGivenUpReport.CommandLine([root]), lines[heldBack + 1]);
        var done = Array.FindIndex(lines, l => l.StartsWith(doneOpening, StringComparison.Ordinal));
        Assert.True(done > heldBack + 1, $"The line is not ahead of what the batch did.\n{stdout}");
    }

    private static void AssertEntry((CliEventClass Class, string Text) entry, CliEventClass expected, string text)
    {
        Assert.Equal(expected, entry.Class);
        Assert.Equal(text, entry.Text);
    }

    private sealed record RunResult(
        int ExitCode, string Stdout, IReadOnlyList<(CliEventClass Class, string Text)> Entries);

    private static async Task<RunResult> Run(
        string arg, ScanResult result, ReverifyResult? reverify = null,
        DeleteResult? delete = null, Func<CallInfo, MoveResult>? move = null)
    {
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var reboot = Substitute.For<IPendingRebootService>();
        reboot.Check().Returns(PendingRebootResult.Clean);

        // Surviving defaults to the whole offer and gives nothing up, so a test about the
        // scan never meets a check it did not ask for.
        var reverifier = Substitute.For<IRemovableReverifier>();
        reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(reverify ?? new ReverifyResult(
                result.RemovableFiles.Select(f => f.FullPath).ToList(), Array.Empty<string>()));

        var deleter = Substitute.For<IDeleteFilesService>();
        deleter.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(delete ?? new DeleteResult(result.RemovableFiles.Count, Array.Empty<FileOperationError>()));

        var mover = Substitute.For<IMoveFilesService>();
        mover.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(move ?? (_ => new MoveResult(result.RemovableFiles.Count, Array.Empty<FileOperationError>())));

        var services = CliRunFixtures.Services(scan, reboot: reboot, reverifier: reverifier,
            delete: deleter, move: mover);

        var invocation = arg switch
        {
            "/m" => new CliInvocation(CliCommand.Move, null, Destination),
            "/d" => new CliInvocation(CliCommand.Delete, null, null),
            _ => new CliInvocation(CliCommand.ScanOnly, null, null),
        };

        // Console.SetOut and the recorder are process-global; the assembly disables test
        // parallelisation, which is what makes both safe to read back here.
        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            EventLogRecorder.Clear();
            var exitCode = await Program.RunWorkAsync(arg, invocation, CancellationToken.None, services);
            return new RunResult(exitCode, buffer.ToString(), EventLogRecorder.Entries);
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
