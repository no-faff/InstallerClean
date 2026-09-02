using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The split of why a scan kept each file back, driven through real scans rather
/// than built by hand.
///
/// THE POINT IS THE COMPLETENESS ASSERTION AND NOT THE INDIVIDUAL COUNTS. Three
/// decisions put a file on the withheld list, and the five counts are a partition of
/// it. A partition stays one until somebody adds a branch, and a sixth arm
/// arriving later would appear in none of the five while the list grew underneath
/// them: five counts that no longer sum to the list are the only thing that says so.
/// Every test here asserts the sum as well as its own arm, so a fixture reaching a
/// new decision fails whichever arm it was written for.
///
/// EACH ARM IS REACHED ON ITS OWN, WHICH IS WHAT THE DECISIONS MAKE POSSIBLE. The
/// wholesale arm skips the per-file screen entirely, so no one scan can exercise all
/// five, and a fixture claiming to would be describing a machine that cannot exist.
/// </summary>
public class WithholdingSplitTests
{
    private const string Folder = @"C:\Windows\Installer";
    private const string ProductA = "{11111111-1111-1111-1111-111111111111}";

    /// <summary>
    /// The one thing every test here asserts beside its own arm: the five account for
    /// the list exactly. Named rather than inlined so a test that forgets it is
    /// visible as a test that does not call it.
    /// </summary>
    private static void AssertPartitions(ScanResult result)
    {
        Assert.Equal(result.WithheldFiles?.Count ?? 0, result.WithheldBy.Total);
    }

    [Fact]
    public async Task A_candidate_the_identity_read_gave_up_on_is_counted_as_its_own_arm()
    {
        // The registration identifies cleanly, so the wholesale arm does not fire and
        // this candidate is kept back on its own account. A fixture whose registration
        // also failed would withhold everything at once and this arm would read zero
        // while the test still passed, which is the shape this pairing avoids.
        var result = await Scan(
            walked: new[] { $@"{Folder}\a.msi", $@"{Folder}\gaveup.msi" },
            registered: new[] { $@"{Folder}\a.msi" },
            identities: Reader(
                ($@"{Folder}\a.msi", FileIdentityRead.Read),
                ($@"{Folder}\gaveup.msi", FileIdentityRead.OpenRefused)));

        Assert.Equal(1, result.WithheldBy.IdentityUnestablishedCount);
        Assert.Equal(0, result.WithheldBy.WholesaleCount);
        AssertPartitions(result);
    }

    [Fact]
    public async Task A_walk_offer_withheld_in_one_go_is_counted_as_its_own_arm()
    {
        // The census says a product may be installed more than once, which is one of
        // the three conditions that keep the whole walk-derived offer back. The screen
        // is never reached on this path, so its two arms must read zero.
        var result = await Scan(
            walked: new[] { $@"{Folder}\a.msi", $@"{Folder}\b.msi" },
            registered: Array.Empty<string>(),
            census: new EnumerationCensus(InstanceProductCount: 1));

        Assert.True(result.WalkOfferWithheldWholesale);
        Assert.Equal(2, result.WithheldBy.WholesaleCount);
        Assert.Equal(0, result.WithheldBy.DeclaredProductInstalledCount);
        Assert.Equal(0, result.WithheldBy.DeclaredProductUnestablishedCount);
        AssertPartitions(result);
    }

    [Fact]
    public async Task The_screens_two_withholding_verdicts_are_counted_apart()
    {
        // Both arms in one scan, because the screen answers per file and these two
        // answers are opposite findings: one is Windows positively holding the
        // product, the other is nothing having been settled at all. A single count
        // over the pair would state a cause that is false of half of it.
        var identities = new ScriptedPackageIdentities();
        identities.Declares($@"{Folder}\held.msi", ProductA);
        // The second file has to be scripted as yielding nothing rather than left
        // unscripted. An unscripted path throws by design, so the arm this test is
        // named for would never be reached; YieldsNothing is the reader answering
        // that it had nothing to read, which is the answer the Unestablished count
        // is about.
        identities.YieldsNothing($@"{Folder}\unreadable.msi");

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var result = await Scan(
            walked: new[] { $@"{Folder}\held.msi", $@"{Folder}\unreadable.msi" },
            registered: Array.Empty<string>(),
            screen: new DeclaredProductCheck(msi, identities));

        Assert.Equal(1, result.WithheldBy.DeclaredProductInstalledCount);
        Assert.Equal(1, result.WithheldBy.DeclaredProductUnestablishedCount);
        Assert.Equal(0, result.WithheldBy.WholesaleCount);
        AssertPartitions(result);
    }

    [Fact]
    public async Task A_screen_that_answered_about_a_different_number_of_files_is_counted_apart()
    {
        // A screen answering a different number of candidates than it was handed has
        // answered about none of them, so every candidate is kept and no verdict in it
        // may be read positionally. It is a third fact rather than a member of either
        // verdict above: filing it under one of them would attach a cause the screen
        // never reached.
        var screen = Substitute.For<IDeclaredProductCheck>();
        screen.Screen(Arg.Any<IReadOnlyList<OrphanedFile>>(),
                Arg.Any<CancellationToken>(), Arg.Any<Action<Exception, string>?>())
            .Returns(new[] { DeclaredProductOutcome.DeclaredProductNotInstalled });

        var result = await Scan(
            walked: new[] { $@"{Folder}\a.msi", $@"{Folder}\b.msi", $@"{Folder}\c.msi" },
            registered: Array.Empty<string>(),
            screen: screen);

        Assert.Equal(3, result.WithheldBy.ScreenUnansweredCount);
        Assert.Equal(0, result.WithheldBy.DeclaredProductInstalledCount);
        Assert.Equal(0, result.WithheldBy.DeclaredProductUnestablishedCount);
        AssertPartitions(result);
    }

    [Fact]
    public async Task A_scan_that_withheld_nothing_reports_five_zeroes()
    {
        // The state the great majority of machines are in, and the one a partition can
        // satisfy by accident: every count is zero and so is the list, so the sum
        // agrees for the wrong reason. It is asserted anyway, because a split that
        // reported a figure on a machine that kept nothing back would be wrong in a
        // way the other tests here cannot see.
        var result = await Scan(
            walked: new[] { $@"{Folder}\a.msi" },
            registered: Array.Empty<string>());

        Assert.Empty(result.WithheldFiles!);
        Assert.Equal(default, result.WithheldBy);
        Assert.Equal(0, result.WithheldBy.Total);
        AssertPartitions(result);
    }

    [Fact]
    public async Task Two_arms_firing_on_one_scan_still_account_for_the_list()
    {
        // The identity arm takes one file and the screen takes another, in the same
        // pass. It is the case a per-arm test cannot reach and the one where a shared
        // counter or a double count would show: each file is on the list once and is
        // counted under one arm only.
        var identities = new ScriptedPackageIdentities();
        identities.Declares($@"{Folder}\held.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var result = await Scan(
            walked: new[] { $@"{Folder}\a.msi", $@"{Folder}\gaveup.msi", $@"{Folder}\held.msi" },
            registered: new[] { $@"{Folder}\a.msi" },
            identities: Reader(
                ($@"{Folder}\a.msi", FileIdentityRead.Read),
                ($@"{Folder}\gaveup.msi", FileIdentityRead.OpenRefused),
                ($@"{Folder}\held.msi", FileIdentityRead.Read)),
            screen: new DeclaredProductCheck(msi, identities));

        Assert.Equal(1, result.WithheldBy.IdentityUnestablishedCount);
        Assert.Equal(1, result.WithheldBy.DeclaredProductInstalledCount);
        Assert.Equal(2, result.WithheldFiles!.Count);
        AssertPartitions(result);
    }

    // ---- fixtures ----

    /// <summary>
    /// An identity reader answering from a map, with anything unnamed refused. The
    /// refusal is deliberate: a fixture that quietly read every unnamed file cleanly
    /// would let a file the scan never asked about pass as identified.
    /// </summary>
    private static IFileIdentityReader Reader(params (string Path, FileIdentityRead Answer)[] answers)
    {
        var map = answers.ToDictionary(a => a.Path, a => a.Answer, StringComparer.OrdinalIgnoreCase);
        var ids = map.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

        var reader = Substitute.For<IFileIdentityReader>();
        reader.ReadOutcome(Arg.Any<string>(), out Arg.Any<FileIdentity>())
            .Returns(call =>
            {
                var path = (string?)call[0] ?? string.Empty;
                var answer = map.TryGetValue(path, out var a) ? a : FileIdentityRead.OpenRefused;

                // Only a clean read fills the identity, which is the production
                // contract: a fake filling it on a failure would let a caller that
                // read the value without checking the outcome pass on a fixture.
                call[1] = answer == FileIdentityRead.Read
                    ? new FileIdentity(VolumeSerialNumber: 1, FileIdLow: (ulong)(ids.IndexOf(path) + 1), FileIdHigh: 0)
                    : default(FileIdentity);
                return answer;
            });
        return reader;
    }

    private static Task<ScanResult> Scan(
        string[] walked,
        string[] registered,
        IFileIdentityReader? identities = null,
        IDeclaredProductCheck? screen = null,
        EnumerationCensus census = default)
    {
        var fs = new MockFileSystem();
        fs.AddDirectory(Folder);
        foreach (var path in walked.Concat(registered).Distinct())
            fs.AddFile(path, new MockFileData(new byte[100]));

        var packages = registered
            .Select(p => new RegisteredPackage(ProductName: "P", ProductCode: ProductA, LocalPackagePath: p))
            .ToList();

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(
                Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(packages.AsReadOnly(), Census: census));

        return new FileSystemScanService(query, fs, null, walked, null, identities, screen)
            .ScanAsync();
    }
}
