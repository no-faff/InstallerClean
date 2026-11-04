using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What the command line says about files it did not act on, on a run that DID offer
/// something, driven through the real work method rather than through the strings.
///
/// TWO STAGES HOLD FILES BACK AND THEY ARE NOT ONE STAGE. The scan withholds before
/// anything is offered, and the pre-act re-verify drops files out of a batch already
/// offered. A run meets either without the other, and they are reported by different
/// lines: the scan's by the count of files left alone, which every scan prints, and the
/// re-verify's by the held-back sentence a Move or Delete prints beside its summary.
///
/// THE COUNT SITS UNDER THE LINE SAYING WHAT WAS FOUND. "Found 2 unneeded files" is true
/// on each of these machines and says nothing about the files that went unmentioned, so
/// the line after it counts them. A /s that lists files prints the count ahead of that
/// line instead, so the list sits straight under it.
/// </summary>
public class CliHeldBackBesideAnOfferTests
{
    private const string OfferA = @"C:\Windows\Installer\offer-a.msi";
    private const string OfferB = @"C:\Windows\Installer\offer-b.msi";
    private const string HeldA = @"C:\Windows\Installer\held-a.msi";
    private const string HeldB = @"C:\Windows\Installer\held-b.msi";

    private static readonly string MoveDestination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-held-back-beside-an-offer-test");

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_delete_or_move_prints_the_count_straight_under_its_found_line(string arg)
    {
        // Two offered, two withheld one at a time, and nine registered files on the disk:
        // eleven files left alone. The clean line is not printed, there being an offer.
        var (_, stdout) = await Run(arg, Scan(
            offer: 2, withheld: 2,
            split: new WithholdingSplit(DeclaredProductUnestablishedCount: 2)));

        Assert.StartsWith(Lines(FoundLine(2, 2048), LeftAlone(11)), AfterScanningLine(stdout),
            StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_scan_that_lists_files_prints_the_count_ahead_of_its_found_line_and_nothing_unindented_after()
    {
        // The rows are indented and a script reading them takes the indented lines after
        // the found line to the end of the output, so nothing may come between that line
        // and the rows.
        var (_, stdout) = await Run("/s", Scan(
            offer: 2, withheld: 2,
            split: new WithholdingSplit(WholesaleCount: 2)));

        var after = AfterScanningLine(stdout);
        Assert.StartsWith(Lines(LeftAlone(11), FoundLine(2, 2048)), after, StringComparison.Ordinal);

        var rows = after[Lines(LeftAlone(11), FoundLine(2, 2048)).Length..]
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, rows.Length);
        Assert.All(rows, row => Assert.StartsWith("  ", row, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("/s")]
    [InlineData("/d")]
    public async Task A_scan_that_withheld_nothing_still_prints_the_count(string arg)
    {
        var (_, stdout) = await Run(arg, Scan(offer: 2, withheld: 0, split: default));

        Assert.Contains(LeftAlone(9), stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_run_whose_re_verify_drops_a_file_says_so()
    {
        // THE PRE-ACT RE-VERIFY IS THE OTHER PRODUCER. It hands back what survives and
        // what it dropped, and the held-back line is built from the tally beside them,
        // so a dropped file is the condition this fixture has to create: two offered,
        // one surviving, one dropped with a reason against it.
        var (_, stdout) = await Run("/d",
            Scan(offer: 2, withheld: 0, split: default),
            reverify: new ReverifyResult(
                new[] { OfferA },
                new[] { OfferB },
                Reasons: new HeldBackReasons(Reclaimed: 1)),
            delete: new DeleteResult(1, Array.Empty<FileOperationError>()));

        Assert.Contains(HeldBackSentence(1), stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_two_producers_are_added_and_reported_as_one_number()
    {
        // The pre-act pass and the service's own re-read hold back DIFFERENT files, and
        // the run prints one sentence for the batch. Taking the later tally instead of
        // adding it would report the second producer's files and lose the first's, so
        // this fixture is the one that has to put a count on both at once.
        var (_, stdout) = await Run("/d",
            Scan(offer: 2, withheld: 0, split: default),
            reverify: new ReverifyResult(
                new[] { OfferA },
                new[] { OfferB },
                Reasons: new HeldBackReasons(Reclaimed: 1)),
            delete: new DeleteResult(0, Array.Empty<FileOperationError>(),
                HeldBack: new[] { OfferA },
                HeldBackReasons: new HeldBackReasons(RecordsChanged: 1)));

        Assert.Contains(HeldBackSentence(2), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(HeldBackSentence(1), stdout, StringComparison.Ordinal);
    }

    // Composed the way the line under test is: the form the count reaches in the
    // displayed language, and the count formatted for that language. Reading the
    // plural key straight answers for English and for any language whose rule has
    // two arms, and differently wherever a Few form covers the count.
    private static string HeldBackSentence(int count) =>
        string.Format(
            DisplayHelpers.Pluralise(count, Strings.Completion_HeldBack_Singular,
                Strings.Completion_HeldBack_Plural, "Completion.HeldBack"),
            DisplayHelpers.FormatCount(count));

    private static string LeftAlone(int count) => LeftAloneReport.Line(count);

    // Composed the way the run composes it. Cli.FoundOrphans is one flat sentence for
    // every count, inflecting only through the satellite-only overrides, so the form has
    // to be picked by the same rule rather than by reading the neutral value straight.
    private static string FoundLine(int count, long bytes) =>
        string.Format(
            DisplayHelpers.Pluralise(count, Strings.Cli_FoundOrphans, "Cli.FoundOrphans"),
            DisplayHelpers.FormatCount(count), DisplayHelpers.PluraliseFile(count),
            DisplayHelpers.FormatSize(bytes));

    // The lines joined the way the console writes them, ending in a line break.
    private static string Lines(params string[] lines) =>
        string.Concat(lines.Select(line => line + Environment.NewLine));

    // Everything after the scanning line, which names the folder and so differs by machine.
    private static string AfterScanningLine(string stdout)
    {
        var at = stdout.IndexOf(Environment.NewLine, StringComparison.Ordinal);
        Assert.True(at >= 0, "the run printed no line at all");
        return stdout[(at + Environment.NewLine.Length)..];
    }

    // WHICH FILES THE COUNT TAKES IN IS DECIDED BY THE REGISTERED ROWS AND THE WITHHELD
    // LIST, so those are the things these fixtures set: nine registered files on the
    // disk, and the withheld files beside them.
    private static ScanResult Scan(
        int offer, int withheld, WithholdingSplit split, bool offerSuperseded = false) =>
        new(Files(offer, OfferA, OfferB, offerSuperseded),
            Enumerable.Range(0, 9)
                .Select(i => new RegisteredPackage($@"C:\Windows\Installer\r{i}.msi", "Product", $"{{{i}}}"))
                .ToArray(),
            0,
            WithheldFiles: Files(withheld, HeldA, HeldB),
            WithheldBy: split);

    /// <param name="superseded">
    /// Registered superseded patches rather than files the walk found.
    /// </param>
    private static OrphanedFile[] Files(int n, string first, string second, bool superseded = false) =>
        n switch
        {
            0 => Array.Empty<OrphanedFile>(),
            1 => [new OrphanedFile(first, 1024, superseded, superseded, false, "unclaimed")],
            _ => [new OrphanedFile(first, 1024, superseded, superseded, false, "unclaimed"),
                  new OrphanedFile(second, 1024, superseded, superseded, false, "unclaimed")],
        };

    private static async Task<(int ExitCode, string Stdout)> Run(
        string arg, ScanResult result, ReverifyResult? reverify = null, DeleteResult? delete = null)
    {
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var reboot = Substitute.For<IPendingRebootService>();
        reboot.Check().Returns(PendingRebootResult.Clean);

        // Surviving defaults to the whole offer, so a test that is not about the
        // re-verify does not silently exercise a drop it never asked for.
        var reverifier = Substitute.For<IRemovableReverifier>();
        reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(reverify ?? new ReverifyResult(
                result.RemovableFiles.Select(f => f.FullPath).ToList(),
                Array.Empty<string>()));

        var deleter = Substitute.For<IDeleteFilesService>();
        deleter.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(delete ?? new DeleteResult(
                result.RemovableFiles.Count, Array.Empty<FileOperationError>()));

        var mover = Substitute.For<IMoveFilesService>();
        mover.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(result.RemovableFiles.Count, Array.Empty<FileOperationError>()));

        var services = CliRunFixtures.Services(
            scan, reboot: reboot, reverifier: reverifier, delete: deleter, move: mover);

        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            var invocation = arg switch
            {
                "/m" => new CliInvocation(CliCommand.Move, null, MoveDestination),
                "/s" => new CliInvocation(CliCommand.ScanOnly, null, null),
                _ => new CliInvocation(CliCommand.Delete, null, null),
            };
            var exitCode = await Program.RunWorkAsync(arg, invocation, CancellationToken.None, services);
            return (exitCode, buffer.ToString());
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
