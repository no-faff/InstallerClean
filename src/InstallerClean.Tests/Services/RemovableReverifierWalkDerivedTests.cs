using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The check made just before a Move or Delete, on the files no registration names:
/// the containment guard, the file-identity comparison, the withholding legs, the
/// declared-product screen and the age check, run again in the scan's order.
///
/// A HOLD ALONE COULD BE THE GUARD'S. The guard asks the real filesystem, and a path it
/// does not answer Safe for is held under the same cause as the screen's and the age
/// check's holds. So every test holding a file under that cause carries a file beside
/// it that survives, or shows the step after the guard was handed the file: either
/// shows the guard answered Safe for the folder these paths are in.
/// </summary>
public class RemovableReverifierWalkDerivedTests
{
    private const string Folder = @"C:\Windows\Installer";
    private const string Product = "{00000000-0000-0000-0000-000000000001}";
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Old = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static IInstallerQueryService Query(EnumerationCensus census, params RegisteredPackage[] pkgs)
    {
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(pkgs.ToList().AsReadOnly(), Census: census));
        return q;
    }

    private static IInstallerQueryService Query(params RegisteredPackage[] pkgs) => Query(default, pkgs);

    private static RemovableReverifier Reverifier(
        IInstallerQueryService query, IFileIdentityReader ids, IDeclaredProductCheck screen, IFileTimesReader times) =>
        new(query, Substitute.For<IMsiApi>(), ids, screen, times, new FixedClock(Now), null);

    /// <summary>
    /// A screen that keeps the files named in <paramref name="kept"/> and lets every
    /// other file through.
    /// </summary>
    private static IDeclaredProductCheck Screen(params string[] kept)
    {
        var screen = Substitute.For<IDeclaredProductCheck>();
        screen.Screen(Arg.Any<IReadOnlyList<OrphanedFile>>(), Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception, string>?>(), Arg.Any<Func<string, bool?>?>())
            .Returns(call => call.ArgAt<IReadOnlyList<OrphanedFile>>(0)
                .Select(f => kept.Contains(f.FullPath, StringComparer.OrdinalIgnoreCase)
                    ? DeclaredProductOutcome.DeclaredProductInstalled
                    : DeclaredProductOutcome.DeclaredProductNotInstalled)
                .ToList());
        return screen;
    }

    /// <summary>The paths the screen was handed, over every call.</summary>
    private static List<string> Screened(IDeclaredProductCheck screen) =>
        screen.ReceivedCalls()
            .SelectMany(c => ((IReadOnlyList<OrphanedFile>)c.GetArguments()[0]!).Select(f => f.FullPath))
            .ToList();

    private static RegisteredPackage Live(string path) => new(path, "Product", Product);

    private static ScriptedFileTimes OldTimes(params string[] paths)
    {
        var times = new ScriptedFileTimes();
        foreach (var path in paths) times.Reads(path, Old, Old, Old);
        return times;
    }

    // ---- Every step lets the file through ----

    [Fact]
    public async Task A_file_every_step_confirms_survives_and_every_step_was_asked()
    {
        const string orphan = Folder + @"\orphan.msi";
        const string registered = Folder + @"\registered.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(registered, 1);
        ids.Opens(orphan, 2);
        var screen = Screen();
        var times = OldTimes(orphan);

        var result = await Reverifier(Query(Live(registered)), ids, screen, times)
            .ReverifyAsync(new[] { orphan });

        Assert.Equal(new[] { orphan }, result.Surviving);
        Assert.Empty(result.Dropped);
        Assert.Equal(0, result.Reasons.Total);
        Assert.Contains(orphan, ids.Reads);
        Assert.Equal(new[] { orphan }, Screened(screen));
        Assert.Equal(new[] { orphan }, times.Asked);
    }

    // ---- The screen ----

    [Fact]
    public async Task A_file_the_screen_keeps_is_held_back_and_the_file_beside_it_is_not()
    {
        const string kept = Folder + @"\kept.msi";
        const string spare = Folder + @"\spare.msi";
        const string registered = Folder + @"\registered.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(registered, 1);
        ids.Opens(kept, 2);
        ids.Opens(spare, 3);

        var result = await Reverifier(Query(Live(registered)), ids, Screen(kept), OldTimes(spare))
            .ReverifyAsync(new[] { kept, spare });

        Assert.Equal(new[] { spare }, result.Surviving);
        Assert.Equal(new[] { kept }, result.Dropped);
        Assert.Equal(new HeldBackReasons(FileNotConfirmed: 1), result.Reasons);
    }

    [Fact]
    public async Task A_screen_that_answers_about_a_different_number_of_files_holds_every_one()
    {
        const string a = Folder + @"\a.msi";
        const string b = Folder + @"\b.msi";
        const string registered = Folder + @"\registered.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(registered, 1);
        ids.Opens(a, 2);
        ids.Opens(b, 3);
        var screen = Substitute.For<IDeclaredProductCheck>();
        screen.Screen(Arg.Any<IReadOnlyList<OrphanedFile>>(), Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception, string>?>(), Arg.Any<Func<string, bool?>?>())
            .Returns(new[] { DeclaredProductOutcome.DeclaredProductNotInstalled });

        var result = await Reverifier(Query(Live(registered)), ids, screen, OldTimes(a, b))
            .ReverifyAsync(new[] { a, b });

        // Both reached the screen, so the guard let both through.
        Assert.Equal(new[] { a, b }, Screened(screen));
        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { a, b }, result.Dropped);
        Assert.Equal(new HeldBackReasons(FileNotConfirmed: 2), result.Reasons);
    }

    [Fact]
    public async Task The_screen_is_handed_each_file_as_the_scan_hands_it()
    {
        const string package = Folder + @"\package.msi";
        const string patch = Folder + @"\patch.MSP";
        const string registered = Folder + @"\registered.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(registered, 1);
        ids.Opens(package, 2);
        ids.Opens(patch, 3);
        var screen = Screen();

        await Reverifier(Query(Live(registered)), ids, screen, OldTimes(package, patch))
            .ReverifyAsync(new[] { package, patch });

        var handed = (IReadOnlyList<OrphanedFile>)screen.ReceivedCalls().Single().GetArguments()[0]!;
        Assert.Equal(new[] { false, true }, handed.Select(f => f.IsPatch));
        Assert.All(handed, f => Assert.False(f.IsRemovablePatch));
    }

    // ---- The age check ----

    [Fact]
    public async Task A_file_not_shown_a_day_old_at_the_check_is_held_back()
    {
        const string fresh = Folder + @"\fresh.msi";
        const string unread = Folder + @"\unread.msi";
        const string spare = Folder + @"\spare.msi";
        const string registered = Folder + @"\registered.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(registered, 1);
        ids.Opens(fresh, 2);
        ids.Opens(unread, 3);
        ids.Opens(spare, 4);
        var times = OldTimes(spare);
        var recent = Now.UtcDateTime.AddHours(-12);
        times.Reads(fresh, Old, Old, recent);
        times.Answers(unread, FileTimesRead.OpenRefused);

        var result = await Reverifier(Query(Live(registered)), ids, Screen(), times)
            .ReverifyAsync(new[] { fresh, unread, spare });

        Assert.Equal(new[] { spare }, result.Surviving);
        Assert.Equal(new[] { fresh, unread }, result.Dropped);
        Assert.Equal(new HeldBackReasons(FileNotConfirmed: 2), result.Reasons);
    }

    // ---- The identity comparison ----

    [Fact]
    public async Task A_file_a_live_registration_names_under_another_spelling_is_held_as_reclaimed()
    {
        const string walked = Folder + @"\named.msi";
        const string spelled = @"C:\Windows\INSTAL~1\named.msi";
        const string spare = Folder + @"\spare.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(spelled, 1);
        ids.Opens(walked, 1);
        ids.Opens(spare, 2);

        var result = await Reverifier(Query(Live(spelled)), ids, Screen(), OldTimes(spare))
            .ReverifyAsync(new[] { walked, spare });

        Assert.Equal(new[] { spare }, result.Surviving);
        Assert.Equal(new[] { walked }, result.Dropped);
        Assert.Equal(new HeldBackReasons(Reclaimed: 1), result.Reasons);
    }

    [Fact]
    public async Task A_file_a_registration_with_an_unread_verdict_names_is_held_as_unreadable()
    {
        const string walked = Folder + @"\named.msp";
        const string spelled = @"C:\Windows\INSTAL~1\named.msp";
        const string spare = Folder + @"\spare.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(spelled, 1);
        ids.Opens(walked, 1);
        ids.Opens(spare, 2);
        var unread = new RegisteredPackage(spelled, "Product", Product, VerdictUnreadable: true);

        var result = await Reverifier(Query(unread), ids, Screen(), OldTimes(spare))
            .ReverifyAsync(new[] { walked, spare });

        Assert.Equal(new[] { walked }, result.Dropped);
        Assert.Equal(new HeldBackReasons(RecordsUnreadable: 1), result.Reasons);
    }

    [Fact]
    public async Task A_file_a_still_removable_registration_names_under_another_spelling_is_held_as_not_confirmed()
    {
        // Not Reclaimed: a registration that is still removable is not a live claim.
        const string walked = Folder + @"\named.msp";
        const string spelled = @"C:\Windows\INSTAL~1\named.msp";
        const string spare = Folder + @"\spare.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(spelled, 1);
        ids.Opens(walked, 1);
        ids.Opens(spare, 2);
        var superseded = new RegisteredPackage(spelled, "Product", Product, PatchState: 2, IsRemovable: true);

        var result = await Reverifier(Query(superseded), ids, Screen(), OldTimes(spare))
            .ReverifyAsync(new[] { walked, spare });

        Assert.Equal(new[] { spare }, result.Surviving);
        Assert.Equal(new[] { walked }, result.Dropped);
        Assert.Equal(new HeldBackReasons(FileNotConfirmed: 1), result.Reasons);
    }

    [Fact]
    public async Task A_file_whose_own_identity_will_not_read_is_held_alone()
    {
        const string refused = Folder + @"\refused.msi";
        const string spare = Folder + @"\spare.msi";
        const string registered = Folder + @"\registered.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(registered, 1);
        ids.Answers(refused, FileIdentityRead.OpenRefused);
        ids.Opens(spare, 2);
        var screen = Screen();

        var result = await Reverifier(Query(Live(registered)), ids, screen, OldTimes(spare))
            .ReverifyAsync(new[] { refused, spare });

        Assert.Equal(new[] { spare }, result.Surviving);
        Assert.Equal(new[] { refused }, result.Dropped);
        Assert.Equal(new HeldBackReasons(FileNotConfirmed: 1), result.Reasons);
        Assert.Equal(new[] { spare }, Screened(screen));
    }

    // ---- The legs ----

    [Fact]
    public async Task A_registration_that_will_not_identify_holds_the_walk_derived_half_and_nothing_else()
    {
        const string unidentified = Folder + @"\unidentified.msi";
        const string superseded = Folder + @"\superseded.msp";
        const string a = Folder + @"\a.msi";
        const string b = Folder + @"\b.msi";
        var ids = new ScriptedFileIdentities();
        ids.Answers(unidentified, FileIdentityRead.OpenRefused);
        ids.Opens(superseded, 1);
        ids.Opens(a, 2);
        ids.Opens(b, 3);
        var screen = Screen();
        var times = new ScriptedFileTimes();
        var removable = new RegisteredPackage(superseded, "Product", Product, PatchState: 2, IsRemovable: true);

        var result = await Reverifier(Query(Live(unidentified), removable), ids, screen, times)
            .ReverifyAsync(new[] { a, superseded, b });

        Assert.Equal(new[] { superseded }, result.Surviving);
        Assert.Equal(new[] { a, b }, result.Dropped);
        Assert.Equal(new HeldBackReasons(OwnershipUnestablished: 2), result.Reasons);
        Assert.Empty(Screened(screen));
        Assert.Empty(times.Asked);
    }

    [Fact]
    public async Task The_enumerations_own_legs_hold_the_walk_derived_half_before_the_screen_runs()
    {
        const string orphan = Folder + @"\orphan.msi";
        const string registered = Folder + @"\registered.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(registered, 1);
        ids.Opens(orphan, 2);
        var screen = Screen();

        var result = await Reverifier(
                Query(new EnumerationCensus(InstanceProductCount: 1), Live(registered)),
                ids, screen, new ScriptedFileTimes())
            .ReverifyAsync(new[] { orphan });

        Assert.Equal(new[] { orphan }, result.Dropped);
        Assert.Equal(new HeldBackReasons(OwnershipUnestablished: 1), result.Reasons);
        Assert.Empty(Screened(screen));
    }

    // ---- What is and is not put to these steps ----

    [Fact]
    public async Task A_superseded_file_the_records_still_name_is_put_to_none_of_the_file_steps()
    {
        const string superseded = Folder + @"\superseded.msp";
        const string orphan = Folder + @"\orphan.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(superseded, 1);
        ids.Opens(orphan, 2);
        var screen = Screen();
        var times = OldTimes(orphan);
        var removable = new RegisteredPackage(superseded, "Product", Product, PatchState: 2, IsRemovable: true);

        var result = await Reverifier(Query(removable), ids, screen, times)
            .ReverifyAsync(new[] { superseded, orphan });

        Assert.Equal(new[] { superseded, orphan }, result.Surviving);
        Assert.Equal(new[] { orphan }, Screened(screen));
        Assert.Equal(new[] { orphan }, times.Asked);
    }

    [Fact]
    public async Task A_batch_with_no_walk_derived_file_opens_no_file()
    {
        const string superseded = Folder + @"\superseded.msp";
        var ids = new ScriptedFileIdentities();
        var screen = Screen();
        var times = new ScriptedFileTimes();
        var removable = new RegisteredPackage(superseded, "Product", Product, PatchState: 2, IsRemovable: true);

        var result = await Reverifier(Query(removable), ids, screen, times)
            .ReverifyAsync(new[] { superseded });

        Assert.Equal(new[] { superseded }, result.Surviving);
        Assert.Empty(ids.Reads);
        Assert.Empty(Screened(screen));
        Assert.Empty(times.Asked);
    }

    [Fact]
    public async Task A_path_the_guard_does_not_answer_safe_for_is_held_and_not_opened()
    {
        // The fake identity reader throws on a path nobody scripted, so opening the
        // outside path would fail this test rather than pass it.
        const string outside = @"C:\Setup\outside.msi";
        const string spare = Folder + @"\spare.msi";
        const string registered = Folder + @"\registered.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(registered, 1);
        ids.Opens(spare, 2);
        var screen = Screen();

        var result = await Reverifier(Query(Live(registered)), ids, screen, OldTimes(spare))
            .ReverifyAsync(new[] { outside, spare });

        Assert.Equal(new[] { spare }, result.Surviving);
        Assert.Equal(new[] { outside }, result.Dropped);
        Assert.Equal(new HeldBackReasons(FileNotConfirmed: 1), result.Reasons);
        Assert.DoesNotContain(outside, ids.Reads);
        Assert.Equal(new[] { spare }, Screened(screen));
    }

    [Fact]
    public async Task A_path_the_guard_cannot_settle_is_held_and_not_opened()
    {
        // A path holding a null: the guard cannot read its attributes and answers
        // Unproven rather than Refused, on any host. The services ask the guard again
        // later, and a second answer can differ from the first, so the pass holds the
        // path itself rather than leaving it in the batch.
        const string unsettled = Folder + "\\bad\0name.msi";
        const string spare = Folder + @"\spare.msi";
        const string registered = Folder + @"\registered.msi";
        var ids = new ScriptedFileIdentities();
        ids.Opens(registered, 1);
        ids.Opens(spare, 2);
        var screen = Screen();

        var result = await Reverifier(Query(Live(registered)), ids, screen, OldTimes(spare))
            .ReverifyAsync(new[] { unsettled, spare });

        Assert.Equal(new[] { spare }, result.Surviving);
        Assert.Equal(new[] { unsettled }, result.Dropped);
        Assert.Equal(new HeldBackReasons(FileNotConfirmed: 1), result.Reasons);
        Assert.DoesNotContain(unsettled, ids.Reads);
        Assert.Equal(new[] { spare }, Screened(screen));
    }

    [Fact]
    public void The_pass_the_hosts_build_opens_the_files_no_registration_names()
    {
        // Constructed by hand everywhere else, where the readers can be left off. The
        // container is what the hosts use.
        using var services = new ServiceCollection().AddInstallerCleanCore().BuildServiceProvider();

        var reverifier = Assert.IsType<RemovableReverifier>(services.GetRequiredService<IRemovableReverifier>());

        Assert.True(reverifier.ChecksFiles);
    }
}
