using System.IO.Abstractions.TestingHelpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

public class FileSystemScanServiceTests
{
    private static RegisteredPackage Registered(string path) =>
        new(path, "Test Product", "{00000000-0000-0000-0000-000000000001}");

    /// <summary>
    /// A patch Windows reports superseded, in the shape the enumeration now
    /// produces: the state on the row and no removable verdict. Building one with
    /// IsRemovable set would pin behaviour against a row the query service cannot
    /// emit.
    /// </summary>
    private static RegisteredPackage Superseded(string path) =>
        new(path, "Test Product", "{00000000-0000-0000-0000-000000000001}", PatchState: 2);

    private static RegisteredPackage Obsoleted(string path) =>
        new(path, "Test Product", "{00000000-0000-0000-0000-000000000001}", PatchState: 4);

    /// <summary>
    /// A superseded patch carrying the withheld flag, which no enumeration sets
    /// now. Kept so the scan is still held to handling such a row rather than to
    /// an assumption that one cannot arrive.
    /// </summary>
    private static RegisteredPackage Withheld(string path) =>
        new(path, "Test Product", "{00000000-0000-0000-0000-000000000001}",
            PatchState: 2, IsRemovable: false, RemovableWithheld: true);

    /// <summary>
    /// A patch whose State or Uninstallable read failed, so no verdict was ever
    /// established for it. Kept like the row above and for the opposite reason:
    /// that one the records called removable, this one they said nothing about.
    /// </summary>
    private static RegisteredPackage Unjudged(string path) =>
        new(path, "Test Product", "{00000000-0000-0000-0000-000000000001}",
            IsRemovable: false, VerdictUnreadable: true);

    private static IInstallerQueryService QueryReturning(InstallerQueryResult result)
    {
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(result);
        return q;
    }

    /// <summary>
    /// A scan of one orphan against <paramref name="present"/> registered files
    /// on disk and <paramref name="missing"/> that Windows still names but are
    /// gone: the two numbers the correlation gate weighs against each other.
    /// </summary>
    private static Task<ScanResult> ScanWithRegisteredSplit(int present, int missing)
    {
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var packages = new List<RegisteredPackage>();
        var fs = new MockFileSystem();
        fs.AddFile(orphan, new MockFileData("x"));

        for (var i = 0; i < present; i++)
        {
            var path = $@"C:\Windows\Installer\present{i}.msi";
            packages.Add(Registered(path));
            fs.AddFile(path, new MockFileData("x"));
        }
        for (var i = 0; i < missing; i++)
            packages.Add(Registered($@"C:\Windows\Installer\gone{i}.msi"));

        var query = QueryReturning(new InstallerQueryResult(packages.AsReadOnly()));
        return new FileSystemScanService(query, fs, new[] { orphan }, null).ScanAsync();
    }

    [Fact]
    public async Task ScanAsync_does_not_let_a_registered_file_outside_the_folder_hold_the_survivor_count_up()
    {
        // The survivor count is what disarms the gate, and it used to count a
        // registered path existing ANYWHERE on disk. Three packages cached under
        // a user profile, which is where Windows Installer caches a per-user
        // unmanaged install, then held it past the absolute bound and disarmed
        // the gate permanently on a machine whose folder correlation is wholly
        // broken. They exist, they are registered, and they say nothing whatever
        // about the folder this scan walked.
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var packages = new List<RegisteredPackage>();
        var fs = new MockFileSystem();
        fs.AddFile(orphan, new MockFileData("x"));

        for (var i = 0; i < 3; i++)
        {
            var elsewhere = $@"C:\Users\someone\AppData\Local\Package Cache\p{i}.msi";
            packages.Add(Registered(elsewhere));
            fs.AddFile(elsewhere, new MockFileData("x"));
        }
        // Enough in-folder registrations that the gate has something to ask
        // about, all of them gone from disk, which is the collapse it is for.
        for (var i = 0; i < 20; i++)
            packages.Add(Registered($@"C:\Windows\Installer\gone{i}.msi"));

        var query = QueryReturning(new InstallerQueryResult(packages.AsReadOnly()));

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() =>
            new FileSystemScanService(query, fs, new[] { orphan }, null).ScanAsync());

        Assert.Equal(InstallerClean.Resources.Strings.Error_ScanCorrelationFailed, ex.Message);
    }

    [Fact]
    public async Task ScanAsync_does_not_weigh_a_missing_file_the_folder_never_held_against_the_folder()
    {
        // The other half of the same mixed measurement. The survivor count asks
        // about registrations naming this folder; the missing count used to take
        // the whole needed set wherever its paths pointed, so absent
        // registrations that were never in the folder counted against a
        // correlation they say nothing about and could never answer back on the
        // survivor side. This machine's folder correlation is perfect, two of two
        // present, and forty needed files registered elsewhere have gone.
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var packages = new List<RegisteredPackage>();
        var fs = new MockFileSystem();
        fs.AddFile(orphan, new MockFileData("x"));

        for (var i = 0; i < 2; i++)
        {
            var here = $@"C:\Windows\Installer\present{i}.msi";
            packages.Add(Registered(here));
            fs.AddFile(here, new MockFileData("x"));
        }
        for (var i = 0; i < 40; i++)
            packages.Add(Registered($@"C:\Users\someone\AppData\Local\Package Cache\gone{i}.msi"));

        var query = QueryReturning(new InstallerQueryResult(packages.AsReadOnly()));
        var result = await new FileSystemScanService(query, fs, new[] { orphan }, null).ScanAsync();

        Assert.Single(result.RemovableFiles);
        // And the banner is untouched, which is the reason the gate got its own
        // counter instead of this one being narrowed: a needed file registered
        // anywhere and now gone is exactly as much of a problem, so the alarm
        // still fires on all forty.
        Assert.Equal(40, result.MissingAffectedCount);
    }

    [Fact]
    public async Task ScanAsync_refuses_when_no_record_names_a_file_in_the_folder_it_walked()
    {
        // The question a survivor count cannot ask. Every registration here names
        // a file on a drive the walk never touched, which is what an image
        // restore that moved the system volume's letter leaves behind, so nothing
        // in the folder can be matched against anything and the whole of it would
        // otherwise be offered. Five missing rows is below the proportional
        // clause's twenty, so the numeric gate cannot see this machine at all.
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var packages = new List<RegisteredPackage>();
        for (var i = 0; i < 5; i++)
            packages.Add(Registered($@"D:\Windows\Installer\needed{i}.msi"));

        var fs = new MockFileSystem();
        fs.AddFile(orphan, new MockFileData("x"));

        var query = QueryReturning(new InstallerQueryResult(packages.AsReadOnly()));

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() =>
            new FileSystemScanService(query, fs, new[] { orphan }, null).ScanAsync());

        Assert.Equal(InstallerClean.Resources.Strings.Error_ScanNoRegisteredFileInFolder, ex.Message);
    }

    [Fact]
    public async Task ScanAsync_still_scans_when_the_records_name_the_folder_and_the_files_are_simply_gone()
    {
        // The control that separates the two machines, and the reason the check
        // above ignores existence. This one had its cache emptied by another
        // tool: not one registered file is on disk either, and every one of them
        // still names the folder, so the comparison worked and the orphans are
        // real. Four missing rows keeps the numeric gate out of it.
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var packages = new List<RegisteredPackage>();
        for (var i = 0; i < 4; i++)
            packages.Add(Registered($@"C:\Windows\Installer\gone{i}.msi"));

        var fs = new MockFileSystem();
        fs.AddFile(orphan, new MockFileData("x"));

        var query = QueryReturning(new InstallerQueryResult(packages.AsReadOnly()));
        var result = await new FileSystemScanService(query, fs, new[] { orphan }, null).ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal(4, result.MissingAffectedCount);
    }

    [Fact]
    public async Task ScanAsync_does_not_refuse_a_registered_set_it_was_handed_none_of()
    {
        // Nothing can be asked of no rows. An empty registered set is the
        // installer database being unreadable, which InstallerQueryService
        // refuses on its own before the scan sees it, and a gate here reporting a
        // mismatch it never measured would be the fault this project keeps
        // naming: a section that could not measure printing the sentence somebody
        // would be glad to see.
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var fs = new MockFileSystem();
        fs.AddFile(orphan, new MockFileData("x"));

        var query = QueryReturning(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));
        var result = await new FileSystemScanService(query, fs, new[] { orphan }, null).ScanAsync();

        Assert.Single(result.RemovableFiles);
    }

    [Fact]
    public async Task ScanAsync_refuses_a_correlation_that_one_surviving_package_would_have_disarmed()
    {
        // The finding: testing for a total collapse meant a single registered
        // file still on disk disarmed the gate entirely, and a mismatch that
        // spares one path in two hundred is the same fault as one that spares
        // none.
        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() =>
            ScanWithRegisteredSplit(present: 1, missing: 30));

        Assert.Equal(InstallerClean.Resources.Strings.Error_ScanCorrelationFailed, ex.Message);
    }

    [Fact]
    public async Task ScanAsync_weighs_a_missing_superseded_registration_in_the_correlation_gate()
    {
        // A DELIBERATE WIDENING, PINNED SO NOBODY LATER READS IT AS AN ACCIDENT.
        // The gate's own comment says both sides of its proportion ask about one
        // population, the registrations naming a file directly in the walked
        // folder. They did not: the survivor side counted every such registration
        // whose file was there, superseded ones included, and the missing side
        // took only the rows carrying no superseded or obsoleted state, on the
        // reading that such a file having gone was its expected end state. That
        // reading is what 3.0.0 removes, so the exclusion goes with it.
        //
        // This machine could not reach the bound before and reaches it now: one
        // registered file present in the folder, twenty absent and all of them
        // superseded, and an offer with something in it. Refusing is the safe
        // direction, and the shape is not one a healthy machine takes.
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        const string present = @"C:\Windows\Installer\present.msi";
        var fs = new MockFileSystem();
        fs.AddFile(orphan, new MockFileData("x"));
        fs.AddFile(present, new MockFileData("x"));

        var packages = new List<RegisteredPackage> { Registered(present) };
        for (var i = 0; i < 20; i++)
            packages.Add(Superseded($@"C:\Windows\Installer\gone{i}.msp"));

        var query = QueryReturning(new InstallerQueryResult(packages.AsReadOnly()));

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() =>
            new FileSystemScanService(query, fs, new[] { orphan }, null).ScanAsync());

        Assert.Equal(InstallerClean.Resources.Strings.Error_ScanCorrelationFailed, ex.Message);
    }

    [Fact]
    public async Task ScanAsync_does_not_refuse_a_machine_that_has_merely_lost_most_of_its_cache()
    {
        // Machines with most of their registered files gone are real: another
        // tool emptying the folder is exactly what the missing-from-disk banner
        // exists for, and refusing that scan would take away a list they can act
        // on. Three survivors is past the absolute bound and stays a scan.
        var result = await ScanWithRegisteredSplit(present: 3, missing: 30);

        Assert.Single(result.RemovableFiles);
        Assert.Equal(30, result.MissingAffectedCount);
    }

    [Fact]
    public async Task ScanAsync_counts_a_superseded_file_on_disk_as_a_registered_file_on_disk()
    {
        // A real run in the result logs sits exactly here: 52 registered files,
        // more missing than registered once the superseded ones are counted, and
        // ten superseded patches found on disk and offered. Those ten are
        // registered paths that resolved to real files, so the two halves of
        // the scan demonstrably line up, and a gate that only looked at the
        // non-removable rows would have refused that machine its scan.
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        const string needed = @"C:\Windows\Installer\needed.msi";
        const string superseded = @"C:\Windows\Installer\superseded.msp";
        var packages = new List<RegisteredPackage> { Registered(needed), Superseded(superseded) };
        for (var i = 0; i < 30; i++)
            packages.Add(Registered($@"C:\Windows\Installer\gone{i}.msi"));

        var fs = new MockFileSystem();
        fs.AddFile(orphan, new MockFileData("x"));
        fs.AddFile(needed, new MockFileData("x"));
        fs.AddFile(superseded, new MockFileData("x"));

        var query = QueryReturning(new InstallerQueryResult(packages.AsReadOnly()));
        var result = await new FileSystemScanService(query, fs, new[] { orphan }, null).ScanAsync();

        Assert.Equal(2, result.RemovableFiles.Count);
        Assert.Equal(30, result.MissingAffectedCount);
    }

    [Fact]
    public async Task ScanAsync_does_not_refuse_a_small_machine_where_two_survivors_are_a_tenth_of_it()
    {
        // The proportional bound doing its own work: two present out of twenty
        // is a fifth of the registrations, which is nothing like a collapse, and
        // without it the absolute bound alone would refuse this scan.
        var result = await ScanWithRegisteredSplit(present: 2, missing: 18);

        Assert.Single(result.RemovableFiles);
    }

    // The proportional clause reduces to 19P < M with P floored at one, so for
    // each P the absolute bound admits, the answer changes between M = 19P and
    // one more than it, and no survivors answers on one survivor's threshold.
    // Both sides of each are pinned here because the clause is written as
    // P * 20 < P + M, which invites being "simplified" into a percentage, and a
    // percentage moves the bound by one without failing anything else. The
    // (0, 1) row is the floor's own: without it a single registered file missing
    // from a machine with one orphan in the folder refused the entire scan.
    [Theory]
    [InlineData(0, 1, false)]
    [InlineData(0, 19, false)]
    [InlineData(0, 20, true)]
    [InlineData(1, 19, false)]
    [InlineData(1, 20, true)]
    [InlineData(2, 38, false)]
    [InlineData(2, 39, true)]
    public async Task The_proportional_bound_is_pinned_on_both_sides(int present, int missing, bool refuses)
    {
        if (!refuses)
        {
            Assert.Single((await ScanWithRegisteredSplit(present, missing)).RemovableFiles);
            return;
        }

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() =>
            ScanWithRegisteredSplit(present, missing));
        Assert.Equal(InstallerClean.Resources.Strings.Error_ScanCorrelationFailed, ex.Message);
    }

    [Fact]
    public async Task ScanAsync_refuses_when_the_root_never_resolved_and_nothing_survived_the_guard()
    {
        // A path on a drive letter nothing is mounted on gives the resolver no
        // existing ancestor to open, which is the degraded root: every candidate
        // is then measured against a spelling rather than a location, every one
        // is refused, and the run would otherwise report the folder as clean.
        var unmounted = Helpers.TestHost.FirstUnmountedDriveLetter();
        if (unmounted is null)
            return; // every letter is in use on this host

        var root = $@"{unmounted}:\Windows\Installer";
        var orphan = $@"{root}\orphan.msi";
        var needed = $@"{root}\needed.msi";
        var query = QueryReturning(new InstallerQueryResult(
            new List<RegisteredPackage> { Registered(needed) }.AsReadOnly()));

        var fs = new MockFileSystem();
        fs.AddFile(orphan, new MockFileData("x"));
        fs.AddFile(needed, new MockFileData("x"));

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() =>
            new FileSystemScanService(query, fs, new[] { orphan }, root).ScanAsync());

        Assert.Equal(InstallerClean.Resources.Strings.Error_ScanCacheRootUnresolved, ex.Message);
    }

    [Fact]
    public async Task ScanAsync_does_not_refuse_when_a_resolved_root_simply_refused_a_candidate()
    {
        // The other half of the pair, and the reason the gate needs both: a
        // refusal against a root the kernel did expand is a real answer about a
        // real file, however many of them there are. %TEMP% stands in for the
        // cache root because it exists, so it resolves; the candidate sits
        // somewhere else, so it is refused.
        var root = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        const string elsewhere = @"C:\Windows\Installer\elsewhere.msi";
        var query = QueryReturning(new InstallerQueryResult(
            new List<RegisteredPackage> { Registered(Path.Combine(root, "needed.msi")) }.AsReadOnly()));

        var fs = new MockFileSystem();
        fs.AddFile(elsewhere, new MockFileData("x"));
        fs.AddFile(Path.Combine(root, "needed.msi"), new MockFileData("x"));

        var result = await new FileSystemScanService(query, fs, new[] { elsewhere }, root).ScanAsync();

        Assert.Empty(result.RemovableFiles);
    }

    [Fact]
    public async Task ScanAsync_never_offers_a_withheld_patch_for_removal()
    {
        const string patch = @"C:\Windows\Installer\withheld.msp";
        var query = QueryReturning(new InstallerQueryResult(
            new List<RegisteredPackage> { Withheld(patch) }.AsReadOnly(), UnaccountedProductCount: 1));

        var fs = new MockFileSystem();
        fs.AddFile(patch, new MockFileData("x"));

        var result = await new FileSystemScanService(query, fs, new[] { patch }, null).ScanAsync();

        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.RegisteredPackages);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task ScanAsync_partitions_the_files_it_keeps_by_what_is_true_of_them()
    {
        // The list the window shows holds three different findings and reads as
        // one. Only the first is a file a registration claims; the second the
        // records called REMOVABLE and this scan would not act on; about the third
        // nothing was established at all. Counted apart, each can be described in
        // words that are true of it; counted together, any sentence naming a need
        // is false of two of them.
        const string claimed = @"C:\Windows\Installer\claimed.msi";
        const string withheld = @"C:\Windows\Installer\withheld.msp";
        const string unjudged = @"C:\Windows\Installer\unjudged.msp";
        var query = QueryReturning(new InstallerQueryResult(
            new List<RegisteredPackage> { Registered(claimed), Withheld(withheld), Unjudged(unjudged) }
                .AsReadOnly(),
            UnaccountedProductCount: 1));

        var fs = new MockFileSystem();
        fs.AddFile(claimed, new MockFileData(new byte[3]));
        fs.AddFile(withheld, new MockFileData(new byte[5]));
        fs.AddFile(unjudged, new MockFileData(new byte[7]));

        var result = await new FileSystemScanService(query, fs, Array.Empty<string>(), null).ScanAsync();

        Assert.Equal(3, result.RegisteredPackages.Count);
        Assert.Equal(1, result.RegisteredClaimedCount);
        Assert.Equal(1, result.RegisteredWithheldCount);
        Assert.Equal(1, result.RegisteredUnjudgedCount);
        // The size travels beside the count and is read with it, so it covers the
        // same files: the whole 15 bytes against a count of one would put the
        // other two files' space behind a sentence about the first.
        Assert.Equal(15, result.RegisteredTotalBytes);
        Assert.Equal(3, result.RegisteredClaimedBytes);
    }

    [Fact]
    public async Task ScanAsync_leaves_no_kept_file_outside_the_three_counts()
    {
        // A partition rather than three tallies that happen to be near the total.
        // Missing files are in it too, because the count on the screen is of rows
        // and not of files on the disk, and a row whose file has gone is still
        // one the window lists.
        var packages = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\a.msi"),
            Registered(@"C:\Windows\Installer\b.msi"),
            Registered(@"C:\Windows\Installer\gone.msi"),
            Withheld(@"C:\Windows\Installer\w1.msp"),
            Withheld(@"C:\Windows\Installer\w2.msp"),
            Unjudged(@"C:\Windows\Installer\u1.msp"),
        };
        var query = QueryReturning(new InstallerQueryResult(packages.AsReadOnly(), UnaccountedProductCount: 1));

        var fs = new MockFileSystem();
        foreach (var p in packages.Where(p => !p.LocalPackagePath.EndsWith(@"\gone.msi", StringComparison.Ordinal)))
            fs.AddFile(p.LocalPackagePath, new MockFileData("x"));

        var result = await new FileSystemScanService(query, fs, Array.Empty<string>(), null).ScanAsync();

        Assert.Equal(6, result.RegisteredPackages.Count);
        Assert.Equal(
            result.RegisteredPackages.Count,
            result.RegisteredClaimedCount + result.RegisteredWithheldCount + result.RegisteredUnjudgedCount);
        Assert.Equal(3, result.RegisteredClaimedCount);
        Assert.Equal(2, result.RegisteredWithheldCount);
        Assert.Equal(1, result.RegisteredUnjudgedCount);
    }

    [Fact]
    public async Task ScanAsync_on_a_machine_that_read_cleanly_claims_every_file_it_keeps()
    {
        // The control the two above need. On an ordinary machine the partition is
        // one population and the other two are empty, so a surface built on the
        // claimed count says exactly what the old one said and the change is
        // invisible to everybody it should be invisible to.
        const string claimed = @"C:\Windows\Installer\claimed.msi";
        var query = QueryReturning(new InstallerQueryResult(
            new List<RegisteredPackage> { Registered(claimed) }.AsReadOnly()));

        var fs = new MockFileSystem();
        fs.AddFile(claimed, new MockFileData(new byte[42]));

        var result = await new FileSystemScanService(query, fs, Array.Empty<string>(), null).ScanAsync();

        Assert.Equal(1, result.RegisteredClaimedCount);
        Assert.Equal(0, result.RegisteredWithheldCount);
        Assert.Equal(0, result.RegisteredUnjudgedCount);
        Assert.Equal(result.RegisteredTotalBytes, result.RegisteredClaimedBytes);
    }

    [Fact]
    public async Task ScanAsync_reports_no_withheld_files_even_where_the_records_were_incomplete()
    {
        // The count is what withholding the removable class cost a run, and no
        // run withholds it: nothing is offered on that verdict, so there is
        // nothing to withhold. It is pinned at zero rather than deleted because
        // the result-log schema carries the field, and a field quietly acquiring a
        // non-zero value again is the shape this asserts against.
        const string present = @"C:\Windows\Installer\withheld-present.msp";
        const string gone = @"C:\Windows\Installer\withheld-gone.msp";
        var query = QueryReturning(new InstallerQueryResult(
            new List<RegisteredPackage> { Withheld(present), Withheld(gone) }.AsReadOnly(),
            UnaccountedProductCount: 2));

        var fs = new MockFileSystem();
        fs.AddFile(present, new MockFileData("x"));

        var result = await new FileSystemScanService(query, fs, Array.Empty<string>(), null).ScanAsync();

        Assert.Equal(0, result.WithheldCount);
        Assert.Equal(2, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task ScanAsync_reports_a_superseded_patch_missing_from_disk()
    {
        // THIS TEST'S CLAIM IS THE REVERSE OF THE ONE IT REPLACES, which asserted
        // that such a row stayed off the missing-files report because the file
        // having gone was its expected end state. Windows opens every registered
        // patch's cached file whether it has been superseded or not, and a missing
        // one gives error 1635, so the record is exactly as much of a problem as
        // any other and the report speaks for it.
        const string gone = @"C:\Windows\Installer\superseded-gone.msp";
        var query = QueryReturning(new InstallerQueryResult(
            new List<RegisteredPackage> { Superseded(gone) }.AsReadOnly()));

        var result = await new FileSystemScanService(
            query, new MockFileSystem(), Array.Empty<string>(), null).ScanAsync();

        Assert.Equal(1, result.MissingUnaffectedCount);
        Assert.Equal(0, result.MissingAffectedCount);
        // The split is data and the total is what both hosts speak.
        Assert.Equal(1, result.MissingFromDiskCount);
    }

    [Fact]
    public async Task ScanAsync_counts_a_missing_file_with_no_patch_state_apart_from_the_superseded_ones()
    {
        // The other side of the split. Same total, different half, and no host
        // says anything different about the two.
        const string gone = @"C:\Windows\Installer\needed-gone.msi";
        var query = QueryReturning(new InstallerQueryResult(
            new List<RegisteredPackage> { Registered(gone) }.AsReadOnly()));

        var result = await new FileSystemScanService(
            query, new MockFileSystem(), Array.Empty<string>(), null).ScanAsync();

        Assert.Equal(1, result.MissingAffectedCount);
        Assert.Equal(0, result.MissingUnaffectedCount);
        Assert.Equal(1, result.MissingFromDiskCount);
    }

    [Fact]
    public async Task ScanAsync_returns_files_not_in_registered_set()
    {
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\aaa.msi"),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fakeFiles = new[]
        {
            @"C:\Windows\Installer\aaa.msi",   // registered, should NOT appear
            @"C:\Windows\Installer\bbb.msi",   // orphaned, should appear
        };

        // The registered file is present on disk, so the correlation gate
        // (every registered package missing while files on disk are orphaned)
        // does not fire on this small scenario.
        var fs = new MockFileSystem();
        fs.AddFile(@"C:\Windows\Installer\aaa.msi", new MockFileData("x"));
        fs.AddFile(@"C:\Windows\Installer\bbb.msi", new MockFileData("x"));

        var svc = new FileSystemScanService(mockQuery, fs, fakeFiles, null);

        var result = await svc.ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal(@"C:\Windows\Installer\bbb.msi", result.RemovableFiles[0].FullPath);
    }

    [Fact]
    public async Task ScanAsync_scans_the_root_only_and_ignores_subdirectory_files()
    {
        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));

        // Only root-level files are candidates. A registered LocalPackage path
        // never sits in a subdirectory, so the API can say nothing about a file
        // there. That puts a payload .msp under $PatchCache$ (the patch engine's
        // baseline copy) and every other subfolder file out of scope.
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"C:\Windows\Installer\bbb.msi"] = new("x"),                                     // root: a candidate
            [@"C:\Windows\Installer\$PatchCache$\Managed\abc\1.0.0\payload.msp"] = new("x"),  // subdir: not
            [@"C:\Windows\Installer\SomeVendor\leftover.msi"] = new("x"),                     // subdir: not
        });

        var svc = new FileSystemScanService(mockQuery, fs);

        var result = await svc.ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal(@"C:\Windows\Installer\bbb.msi", result.RemovableFiles[0].FullPath);
    }

    [Fact]
    public async Task ScanAsync_does_not_offer_a_reparse_point_at_the_cache_root()
    {
        // A symlink or junction sitting at the root, which the walk drops before
        // anything else looks at it: following one would pull an OS file out of
        // System32. The walk used to say this through
        // EnumerationOptions.AttributesToSkip, which MockFileSystem ignores, so
        // this could not be asserted until the test moved into managed code.
        var mockQuery = QueryReturning(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));

        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"C:\Windows\Installer\plain.msi"] = new("x"),
            [@"C:\Windows\Installer\link.msi"] = new("x")
            {
                Attributes = FileAttributes.Normal | FileAttributes.ReparsePoint,
            },
        });

        var result = await new FileSystemScanService(mockQuery, fs).ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal(@"C:\Windows\Installer\plain.msi", result.RemovableFiles[0].FullPath);
    }

    [Fact]
    public async Task ScanAsync_still_offers_hidden_and_system_cache_files()
    {
        // The other half of the same change. Real cache entries sometimes carry
        // Hidden or System, and .NET's default AttributesToSkip is exactly those
        // two, so a walk that took the default would quietly stop offering them.
        var mockQuery = QueryReturning(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));

        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"C:\Windows\Installer\hidden.msi"] = new("x") { Attributes = FileAttributes.Hidden },
            [@"C:\Windows\Installer\system.msp"] = new("x") { Attributes = FileAttributes.System },
        });

        var result = await new FileSystemScanService(mockQuery, fs).ScanAsync();

        Assert.Equal(2, result.RemovableFiles.Count);
    }

    [Fact]
    public async Task ScanAsync_path_comparison_is_case_insensitive()
    {
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\AAA.MSI"),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fakeFiles = new[] { @"C:\Windows\Installer\aaa.msi" };

        var svc = new FileSystemScanService(mockQuery, fakeFiles);
        var result = await svc.ScanAsync();

        Assert.Empty(result.RemovableFiles);
    }

    [Fact]
    public async Task ScanAsync_registered_packages_contains_all_api_packages()
    {
        // One registered package present on disk, one missing: the missing one
        // must still be listed (registered packages are included regardless of
        // disk presence), while the present one keeps the correlation gate from
        // firing on this small scenario.
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\aaa.msi"),  // present
            Registered(@"C:\Windows\Installer\bbb.msi"),  // missing on disk
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fs = new MockFileSystem();
        fs.AddFile(@"C:\Windows\Installer\aaa.msi", new MockFileData(new byte[100]));
        var fakeFiles = new[] { @"C:\Windows\Installer\ccc.msi" }; // orphan

        var svc = new FileSystemScanService(mockQuery, fs, fakeFiles, null);
        var result = await svc.ScanAsync();

        // Both API packages are included, even the one missing from disk.
        Assert.Equal(2, result.RegisteredPackages.Count);
        // Only the present package contributes bytes; the missing one is excluded.
        Assert.Equal(100, result.RegisteredTotalBytes);
        Assert.Equal(1, result.MissingAffectedCount);
        Assert.Single(result.RemovableFiles);
    }

    [Fact]
    public async Task ScanAsync_never_offers_a_superseded_patch()
    {
        // THE CHANGE ITSELF, AND THE TEST THAT REPLACES ITS OPPOSITE. The one it
        // supersedes asserted that a superseded patch appeared in the removable
        // list with Reason "Superseded". Microsoft's own engineer documented in
        // 2008 that Windows opens every patch registered to a product whether or
        // not it has been superseded, and a missing cached file then gives error
        // 1635, so a file Windows holds a live registration for is not this app's
        // to remove whatever state that registration carries.
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\applied.msp"),
            Superseded(@"C:\Windows\Installer\superseded.msp"),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fs = new MockFileSystem();
        fs.AddFile(@"C:\Windows\Installer\applied.msp", new MockFileData(new byte[100]));
        fs.AddFile(@"C:\Windows\Installer\superseded.msp", new MockFileData(new byte[200]));

        var svc = new FileSystemScanService(mockQuery, fs, Array.Empty<string>(), null);
        var result = await svc.ScanAsync();

        Assert.Empty(result.RemovableFiles);
        // Both rows are kept, and the superseded one's bytes are in the kept
        // total: it is a file still sitting in the folder, and a total that left
        // it out would account for less of the folder than is there.
        Assert.Equal(2, result.RegisteredPackages.Count);
        Assert.Equal(300, result.RegisteredTotalBytes);
    }

    [Fact]
    public async Task ScanAsync_never_offers_an_obsoleted_patch_either()
    {
        // Obsoleted (PatchState 4) goes with superseded (2) and NOT because it is
        // rare, which it is: Microsoft's wording for it is "applied in this
        // product instance but obsolete", so it carries the same word as its
        // sibling and the same reasoning reaches it.
        var registered = new List<RegisteredPackage>
        {
            Obsoleted(@"C:\Windows\Installer\obsoleted.msp"),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fs = new MockFileSystem();
        fs.AddFile(@"C:\Windows\Installer\obsoleted.msp", new MockFileData(new byte[150]));

        var svc = new FileSystemScanService(mockQuery, fs, Array.Empty<string>(), null);
        var result = await svc.ScanAsync();

        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.RegisteredPackages);
    }

    [Fact]
    public async Task ScanAsync_counts_the_superseded_patches_it_is_keeping()
    {
        // The two figures nobody had. The field data records superseded patches
        // by count only and never by size, so how much space the class occupies
        // on a real machine has only ever been estimated; these are what a report
        // can carry instead. Files on disk only, the missing ones having no space
        // to give back and their own count already.
        var registered = new List<RegisteredPackage>
        {
            Superseded(@"C:\Windows\Installer\here.msp"),
            Obsoleted(@"C:\Windows\Installer\also-here.msp"),
            Superseded(@"C:\Windows\Installer\gone.msp"),
            Registered(@"C:\Windows\Installer\applied.msi"),
        };

        var fs = new MockFileSystem();
        fs.AddFile(@"C:\Windows\Installer\here.msp", new MockFileData(new byte[200]));
        fs.AddFile(@"C:\Windows\Installer\also-here.msp", new MockFileData(new byte[50]));
        fs.AddFile(@"C:\Windows\Installer\applied.msi", new MockFileData(new byte[10]));

        var query = QueryReturning(new InstallerQueryResult(registered.AsReadOnly()));
        var result = await new FileSystemScanService(query, fs, Array.Empty<string>(), null).ScanAsync();

        Assert.Equal(2, result.RegisteredSupersededCount);
        Assert.Equal(250, result.RegisteredSupersededBytes);
        Assert.Equal(1, result.MissingUnaffectedCount);
    }

    [Fact]
    public async Task ScanAsync_keeps_a_superseded_patch_whose_file_has_gone_in_the_registered_list()
    {
        // Windows lists a patch as superseded and the file is not there. It is
        // reported as a missing registration, and the ROW stays in the registered
        // list, which is the visible half of the change: the window has to be able
        // to name the program it belongs to, and a row taken out of that list to
        // be offered for removal cannot be named there.
        var registered = new List<RegisteredPackage>
        {
            Superseded(@"C:\Windows\Installer\ghost.msp"),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        // Empty MockFileSystem: ghost.msp is registered but not present.
        var fs = new MockFileSystem();

        var svc = new FileSystemScanService(mockQuery, fs, Array.Empty<string>(), null);
        var result = await svc.ScanAsync();

        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.RegisteredPackages);
        Assert.Equal(1, result.MissingUnaffectedCount);
        Assert.Equal(0, result.MissingAffectedCount);
        Assert.Equal(1, result.MissingFromDiskCount);
    }

    [Fact]
    public async Task ScanAsync_counts_a_missing_product_package_on_the_other_side_of_the_split()
    {
        // A registered package with no patch state whose file has gone. Same
        // condition and same total as its superseded neighbour above; only the
        // sub-count differs, and no surface reads the sub-counts.
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\ghost.msi"),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fs = new MockFileSystem();

        var svc = new FileSystemScanService(mockQuery, fs, Array.Empty<string>(), null);
        var result = await svc.ScanAsync();

        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.RegisteredPackages);
        Assert.Equal(1, result.MissingAffectedCount);
        Assert.Equal(0, result.MissingUnaffectedCount);
        Assert.Equal(1, result.MissingFromDiskCount);
    }

    [Fact]
    public async Task ScanAsync_registry_fallback_entries_are_not_removable()
    {
        // Simulate a registry-fallback entry: PatchState=0, IsRemovable=false
        var registered = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\fallback.msi", "", ""),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fakeFiles = Array.Empty<string>();

        var svc = new FileSystemScanService(mockQuery, fakeFiles);
        var result = await svc.ScanAsync();

        // Fallback entries (PatchState=0, IsRemovable=false) stay in registered, not removable
        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.RegisteredPackages);
    }

    [Fact]
    public async Task ScanAsync_handles_10000_orphaned_files()
    {
        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));

        var fakeFiles = Enumerable.Range(0, 10_000)
            .Select(i => $@"C:\Windows\Installer\orphan{i:D5}.msi")
            .ToArray();

        var svc = new FileSystemScanService(mockQuery, fakeFiles);
        var result = await svc.ScanAsync();

        Assert.Equal(10_000, result.RemovableFiles.Count);
        Assert.Empty(result.RegisteredPackages);
    }

    [Fact]
    public async Task ScanAsync_handles_10000_files_with_mixed_registered_and_orphaned()
    {
        // 5000 registered, 5000 orphaned
        var registered = Enumerable.Range(0, 5_000)
            .Select(i => Registered($@"C:\Windows\Installer\reg{i:D5}.msi"))
            .ToList();

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fakeFiles = Enumerable.Range(0, 5_000)
            .Select(i => $@"C:\Windows\Installer\reg{i:D5}.msi")
            .Concat(Enumerable.Range(0, 5_000)
                .Select(i => $@"C:\Windows\Installer\orphan{i:D5}.msi"))
            .ToArray();

        // The 5000 registered packages are present on disk, so the correlation
        // gate does not fire (it would if every registered package were missing).
        var fs = new MockFileSystem();
        for (int i = 0; i < 5_000; i++)
            fs.AddFile($@"C:\Windows\Installer\reg{i:D5}.msi", new MockFileData("x"));

        var svc = new FileSystemScanService(mockQuery, fs, fakeFiles, null);
        var result = await svc.ScanAsync();

        Assert.Equal(5_000, result.RemovableFiles.Count);
        Assert.Equal(5_000, result.RegisteredPackages.Count);
    }

    [Fact]
    public async Task ScanAsync_handles_cancellation_during_large_scan()
    {
        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));

        var fakeFiles = Enumerable.Range(0, 10_000)
            .Select(i => $@"C:\Windows\Installer\orphan{i:D5}.msi")
            .ToArray();

        var cts = new CancellationTokenSource();
        cts.Cancel(); // cancel immediately

        var svc = new FileSystemScanService(mockQuery, fakeFiles);
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => svc.ScanAsync(cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ScanAsync_ignores_non_msi_msp_files()
    {
        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));

        var fakeFiles = new[]
        {
            @"C:\Windows\Installer\legit.msi",
            @"C:\Windows\Installer\patch.msp",
            @"C:\Windows\Installer\readme.txt",
            @"C:\Windows\Installer\data.dat",
            @"C:\Windows\Installer\script.exe",
        };

        var svc = new FileSystemScanService(mockQuery, fakeFiles);
        var result = await svc.ScanAsync();

        Assert.Equal(2, result.RemovableFiles.Count);
        Assert.All(result.RemovableFiles, f =>
            Assert.True(f.FullPath.EndsWith(".msi") || f.FullPath.EndsWith(".msp")));
    }

    [Fact]
    public async Task ScanAsync_query_service_throws_propagates_exception()
    {
        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        var svc = new FileSystemScanService(mockQuery, Array.Empty<string>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.ScanAsync());
    }

    [Fact]
    public async Task ScanAsync_refuses_a_collapsed_correlation()
    {
        // The collapse signature: every registered (non-removable) package is
        // missing from disk while the walk still yields orphan candidates. A
        // path-form mismatch, a collapsed enumeration or the wrong folder all
        // produce it; no healthy machine does. The scan must refuse rather than
        // offer the whole cache for removal.
        var registered = Enumerable.Range(0, 30)
            .Select(i => Registered($@"C:\Windows\Installer\reg{i:D2}.msi"))
            .ToList();

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        // Empty filesystem: none of the registered files exist on disk.
        var fs = new MockFileSystem();
        var orphans = new[] { @"C:\Windows\Installer\o1.msi", @"C:\Windows\Installer\o2.msi" };

        var svc = new FileSystemScanService(mockQuery, fs, orphans, null);

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => svc.ScanAsync());
    }

    [Fact]
    public async Task ScanAsync_classifies_the_orphans_when_the_registered_files_are_on_disk()
    {
        // The gate weighs whether Windows and the folder agree, not how much of
        // the cache is missing: a machine whose registrations resolve to real
        // files is scanned, and its orphans are judged on their own merits.
        var registered = Enumerable.Range(0, 30)
            .Select(i => Registered($@"C:\Windows\Installer\reg{i:D2}.msi"))
            .ToList();

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fs = new MockFileSystem();
        for (var i = 0; i < 28; i++)
            fs.AddFile($@"C:\Windows\Installer\reg{i:D2}.msi", new MockFileData("x"));
        var orphans = new[] { @"C:\Windows\Installer\o1.msi", @"C:\Windows\Installer\o2.msi" };

        var svc = new FileSystemScanService(mockQuery, fs, orphans, null);
        var result = await svc.ScanAsync();

        Assert.Equal(2, result.RemovableFiles.Count);
        Assert.Equal(2, result.MissingAffectedCount);
    }

    [Fact]
    public async Task ScanAsync_carries_the_enumeration_census_through_untouched()
    {
        // The scan does not compute any of these and must not start: they are the
        // enumeration's own measurements and the scan is a courier. Distinct
        // values throughout so a transposition between two fields fails rather
        // than cancelling out.
        var census = new EnumerationCensus(
            UnreadableProducts: 1, SkippedProductRows: 2, RegistryProductKeys: 3,
            UnclaimedProductFiles: 4, UnclaimedPatchFiles: 5,
            NonStringLocalPackageValues: 6, UnreadablePatchStates: 7,
            ProductCount: 8, PatchClaimCount: 9, LongLeafStemCount: 10);
        var query = QueryReturning(new InstallerQueryResult(
            Array.Empty<RegisteredPackage>(), Census: census));

        var result = await new FileSystemScanService(
            query, new MockFileSystem(), Array.Empty<string>(), null).ScanAsync();

        Assert.Equal(census, result.Census);
    }

    [Fact]
    public async Task ScanAsync_reports_the_machines_short_name_policy()
    {
        var probe = Substitute.For<IShortNameCreationProbe>();
        probe.Read().Returns(ShortNameCreationLabels.SystemVolumeOnly);
        var query = QueryReturning(new InstallerQueryResult(Array.Empty<RegisteredPackage>()));

        var result = await new FileSystemScanService(
            query, new MockFileSystem(), probe,
            Array.Empty<string>(), null, null).ScanAsync();

        Assert.Equal(ShortNameCreationLabels.SystemVolumeOnly, result.ShortNameCreation);
    }

    [Fact]
    public async Task A_scan_with_no_probe_reports_the_policy_as_unreadable_rather_than_guessing()
    {
        // The control for the row above, and the one that matters: a default of
        // any real setting would put a figure nobody measured into the one payload
        // that exists to measure, and it would look exactly like a measurement.
        var query = QueryReturning(new InstallerQueryResult(Array.Empty<RegisteredPackage>()));

        var result = await new FileSystemScanService(
            query, new MockFileSystem(), Array.Empty<string>(), null).ScanAsync();

        Assert.Equal(ShortNameCreationLabels.Unreadable, result.ShortNameCreation);
    }
}
