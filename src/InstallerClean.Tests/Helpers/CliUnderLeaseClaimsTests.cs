using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What the command line hands the last safety check before it acts.
///
/// THE DEFECT THIS EXISTS FOR IS A CALLER PASSING THE EMPTY VALUE. The argument is
/// required, so the compiler proves both hosts pass something; it cannot prove they
/// pass the claims the re-verify produced rather than UnderLeaseClaims.None, and the
/// check returns at its first line for an empty batch. A caller passing None receives
/// a pass from the last thing standing in front of a permanent delete, without
/// anything having been asked, and every test stays green.
///
/// THE WINDOW HAS TWO TESTS FOR THIS AND THE COMMAND LINE HAD NONE.
///
/// AND IT IS DELIBERATELY NOT A TEST OF A HELPER. Extracting the call into a named
/// method and asserting the method does the right thing would pin the method, and the
/// call site would remain free to stop calling it: a guard naming a line it does not
/// hold. So this drives the real work method and reads what the real service was
/// handed.
///
/// The fixture gives the re-verify two claims and one sibling, all distinct, so
/// UnderLeaseClaims.None cannot satisfy the assertion by being equal to what was
/// expected. Both lists are asserted, because From carries two and a fold that dropped
/// one of them is a defect this project has already had.
///
/// AND IT ONLY SPEAKS FOR THE CALL SITES THAT EXIST. A caller written tomorrow is not
/// covered by anything here, which is what check-under-lease-claims.mjs is for: that
/// reads the source and holds every production call to the shape, and in return says
/// nothing about what the value turns out to be. Neither of the two covers the other.
///
/// THE DESTINATION IS Path.GetTempPath() SO THE SAME TEST RUNS ON BOTH HOSTS, and what
/// is host-independent is the thing being asserted: which claims the call site hands
/// over. A Windows path spelling is exercised where Windows runs it.
/// </summary>
public class CliUnderLeaseClaimsTests
{
    /// <summary>
    /// A destination the /m gates accept on whichever host is running. They ask that it
    /// is fully qualified and outside both the installer cache and the system folders,
    /// and "fully qualified" is answered differently on the two platforms: a
    /// drive-letter path is not one off Windows and a slash-rooted path is not one on
    /// it. The temp folder is one on either, so the fixture is the same test on both
    /// rather than a test that only ever runs in one place.
    ///
    /// It is incidental to the assertion, which is about what the move service is
    /// handed and not about where. Nothing is created and nothing is written: the move
    /// service is a substitute.
    /// </summary>
    private static readonly string MoveDestination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-claims-test");

    private const string File1 = @"C:\Windows\Installer\a.msi";
    private const string File2 = @"C:\Windows\Installer\b.msp";

    private static readonly PatchClaim SurvivingA =
        new(File1, "{AAAA1111-0000-0000-0000-000000000001}", "{PPPP1111-0000-0000-0000-000000000001}", null, 2);
    private static readonly PatchClaim SurvivingB =
        new(File2, "{AAAA1111-0000-0000-0000-000000000002}", "{PPPP1111-0000-0000-0000-000000000001}", null, 2);
    private static readonly PatchClaim Sibling =
        new(@"C:\Windows\Installer\c.msp", "{BBBB2222-0000-0000-0000-000000000003}", "{PPPP1111-0000-0000-0000-000000000001}", "S-1-5-21-1", 4);

    [Fact]
    public async Task Delete_is_handed_the_claims_the_reverify_produced()
    {
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(2, Array.Empty<FileOperationError>()));

        await Program.RunWorkAsync("/d", Invocation(CliCommand.Delete), CancellationToken.None,
            Services(delete: delete));

        await delete.Received(1).DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<UnderLeaseClaims>(c =>
                c.Batch.SequenceEqual(new[] { SurvivingA, SurvivingB })
                && c.Siblings.SequenceEqual(new[] { Sibling })),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Move_is_handed_the_claims_the_reverify_produced()
    {
        // The other call site. Pinning one and leaving the other is half the item: they
        // are two callers of the same contract and either can be changed alone.
        var move = Substitute.For<IMoveFilesService>();
        move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));

        await Program.RunWorkAsync("/m", Invocation(CliCommand.Move, MoveDestination),
            CancellationToken.None, Services(move: move));

        await move.Received(1).MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Is<UnderLeaseClaims>(c =>
                c.Batch.SequenceEqual(new[] { SurvivingA, SurvivingB })
                && c.Siblings.SequenceEqual(new[] { Sibling })),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_claims_handed_over_are_not_the_empty_value()
    {
        // The assertion above states what arrived; this states what must not, in the
        // words of the defect. A fixture whose re-verify produced nothing would satisfy
        // the first test against a caller passing None, and this is what says the
        // fixture is not that.
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(2, Array.Empty<FileOperationError>()));

        await Program.RunWorkAsync("/d", Invocation(CliCommand.Delete), CancellationToken.None,
            Services(delete: delete));

        // THE POSITIVE ASSERTION COMES FIRST AND IS NOT DECORATION. DidNotReceive is
        // satisfied by a service that was never called at all, so on its own this test
        // passes over a run that took an early return and never reached the delete.
        // Pinning that the call happened is what makes the line below say something.
        await delete.Received(1).DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());

        await delete.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<UnderLeaseClaims>(c => c.Batch.Count == 0 && c.Siblings.Count == 0),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    // ---- fixtures ----

    private static CliInvocation Invocation(CliCommand command, string? moveDestination = null) =>
        new(command, null, moveDestination);

    /// <summary>
    /// The four services the work path resolves, plus the move service for the /m half.
    /// The scan offers two files, the gate is clean, and the re-verify keeps both and
    /// produces the claims asserted above.
    /// </summary>
    private static IServiceProvider Services(
        IDeleteFilesService? delete = null, IMoveFilesService? move = null)
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
                SiblingPatchClaims: new[] { Sibling }));

        return new ServiceCollection()
            .AddSingleton(scan)
            .AddSingleton(reboot)
            .AddSingleton(reverifier)
            .AddSingleton(delete ?? Substitute.For<IDeleteFilesService>())
            .AddSingleton(move ?? Substitute.For<IMoveFilesService>())
            .AddSingleton(Substitute.For<ISettingsService>())
            .BuildServiceProvider();
    }
}
