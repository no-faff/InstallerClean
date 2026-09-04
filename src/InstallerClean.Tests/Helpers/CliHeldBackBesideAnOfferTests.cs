using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What the command line says about files it did not act on, on a run that DID offer
/// something, driven through the real work method rather than through the strings.
///
/// WHAT EVERY FIXTURE HERE SETS UP, BECAUSE BOTH LINES ARE GATED ON IT. A scan whose
/// offer is not empty, and something held back beside it: the scan's own withholding
/// for the lead line, and a file dropped by the pre-act re-verify for the held-back
/// tally. A run offered nothing takes neither branch and reports through the
/// nothing-offered sentences instead.
///
/// TWO STAGES HOLD FILES BACK AND THEY ARE NOT ONE STAGE. The scan withholds before
/// anything is offered, and the pre-act re-verify drops files out of a batch already
/// offered. A run meets either without the other, they are reported by different
/// lines, and each line is gated on something the other's fixture does not set.
///
/// BOTH SENTENCES BELONG TO A RUN THAT ALSO PRINTED A CHEERFUL ONE. "Found 2 unneeded
/// files" is true on each of these machines and says nothing about the files that went
/// unmentioned, which is the whole reason the lines exist.
/// </summary>
public class CliHeldBackBesideAnOfferTests
{
    private const string OfferA = @"C:\Windows\Installer\offer-a.msi";
    private const string OfferB = @"C:\Windows\Installer\offer-b.msi";
    private const string HeldA = @"C:\Windows\Installer\held-a.msi";
    private const string HeldB = @"C:\Windows\Installer\held-b.msi";

    [Fact]
    public async Task A_scan_withholding_beside_a_live_offer_still_gets_its_own_lead_line()
    {
        // THE MACHINE THE LEAD LINE EXISTS FOR. Its offer is not empty, so the run
        // prints "Found 2 unneeded files" and that sentence is true of the folder it
        // describes; the two files the scan kept back are not in it. This line is the
        // run's only statement of them.
        var (_, stdout) = await Run(Scan(
            offer: 2, withheld: 2,
            split: new WithholdingSplit(DeclaredProductInstalledCount: 2)));

        Assert.Contains(HeldBackLead(Strings.Cli_NothingListedPerFile_Plural, 2),
            stdout, StringComparison.Ordinal);

        // BESIDE THE OFFER RATHER THAN INSTEAD OF IT. The offer was real and was acted
        // on, so its own line is still there and the held-back lead sits under it
        // rather than in its place.
        // Composed the way the run composes it. Cli.FoundOrphans is one flat
        // sentence for every count, inflecting only through the satellite-only
        // overrides, so the form has to be picked by the same rule rather than by
        // reading the neutral value straight.
        Assert.Contains(
            string.Format(
                DisplayHelpers.Pluralise(2, Strings.Cli_FoundOrphans, "Cli.FoundOrphans"),
                DisplayHelpers.FormatCount(2), DisplayHelpers.PluraliseFile(2),
                DisplayHelpers.FormatSize(2048)),
            stdout, StringComparison.Ordinal);

        // The clean line is a statement about the folder and this folder has two files
        // in it that nobody vouched for.
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_wholesale_withholding_beside_a_live_offer_gets_the_wholesale_lead()
    {
        // The same machine as above but for the condition that emptied the walk-derived
        // half, so the pair is held apart here as it is where nothing was offered. One
        // sentence for both would be false of one of them.
        var (_, stdout) = await Run(Scan(
            offer: 2, withheld: 2,
            split: new WithholdingSplit(WholesaleCount: 2)));

        Assert.Contains(HeldBackLead(Strings.Cli_NothingListed_Plural, 2),
            stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(HeldBackLead(Strings.Cli_NothingListedPerFile_Plural, 2),
            stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_run_whose_re_verify_drops_a_file_says_so()
    {
        // THE PRE-ACT RE-VERIFY IS THE OTHER PRODUCER. It hands back what survives and
        // what it dropped, and the held-back line is built from the tally beside them,
        // so a dropped file is the condition this fixture has to create: two offered,
        // one surviving, one dropped with a reason against it.
        var (_, stdout) = await Run(
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
        var (_, stdout) = await Run(
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

    private static string HeldBackLead(string value, int count) =>
        string.Format(value, count, DisplayHelpers.PluraliseFile(count),
            DisplayHelpers.FormatSize(count * 1024L));

    // WHICH LEAD A RUN GETS IS DECIDED BY THE SPLIT AND NOTHING ELSE, so the split is
    // the only thing these fixtures vary for it: ScanResult.Withholding compares the
    // withheld count against WithholdingSplit.WholesaleCount, and reads no flag.
    private static ScanResult Scan(int offer, int withheld, WithholdingSplit split) =>
        new(Files(offer, OfferA, OfferB), Array.Empty<RegisteredPackage>(), 0,
            WithheldFiles: Files(withheld, HeldA, HeldB),
            WithheldBy: split);

    private static OrphanedFile[] Files(int n, string first, string second) =>
        n switch
        {
            0 => Array.Empty<OrphanedFile>(),
            1 => [new OrphanedFile(first, 1024, false, false, false, "unclaimed")],
            _ => [new OrphanedFile(first, 1024, false, false, false, "unclaimed"),
                  new OrphanedFile(second, 1024, false, false, false, "unclaimed")],
        };

    private static async Task<(int ExitCode, string Stdout)> Run(
        ScanResult result, ReverifyResult? reverify = null, DeleteResult? delete = null)
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

        var services = new ServiceCollection()
            .AddSingleton(scan)
            .AddSingleton(reboot)
            .AddSingleton(reverifier)
            .AddSingleton(deleter)
            .AddSingleton(Substitute.For<IMoveFilesService>())
            .AddSingleton(Substitute.For<ISettingsService>())
            .BuildServiceProvider();

        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            var exitCode = await Program.RunWorkAsync(
                "/d", new CliInvocation(CliCommand.Delete, null, null),
                CancellationToken.None, services);
            return (exitCode, buffer.ToString());
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
