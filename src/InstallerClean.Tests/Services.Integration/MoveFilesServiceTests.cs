using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// One real-disk smoke test for MoveFilesService. The unit suite under
/// InstallerClean.Tests.Services covers the full behavioural contract
/// against MockFileSystem; this file exists to catch integration
/// surprises that only show on a real NTFS filesystem (case folding,
/// sharing modes, drive boundaries).
/// </summary>
public class MoveFilesServiceTests : IDisposable
{
    private readonly string _sourceDir = TestTemp.NewFolder("ic-move-source-");
    private readonly string _destDir = TestTemp.NewFolder("ic-move-dest-");


    [Fact]
    public async Task MoveFilesAsync_moves_file_to_destination_on_real_filesystem()
    {
        var file = Path.Combine(_sourceDir, "test.msi");
        await File.WriteAllTextAsync(file, "content");

        // The source-containment guard requires sources to resolve inside the
        // cache root; point that root at the sandbox so a legitimately in-bounds
        // real file is not refused. The destination is a separate temp folder,
        // outside the sandbox, so its own gates pass.
        var svc = new MoveFilesService(new System.IO.Abstractions.FileSystem(), null, null, new HandleInHandFolder(), new HandleDestinationGuard(), _sourceDir);
        var results = await svc.MoveFilesAsync(StandingChecks.Of(file), _destDir);

        Assert.Empty(results.Errors);
        Assert.False(File.Exists(file));
        Assert.True(File.Exists(Path.Combine(_destDir, "test.msi")));
        // The batch's own folder is removed once it is empty, and so is the folder above it.
        Assert.False(Directory.Exists(Path.Combine(_sourceDir, InHandFolders.FolderName)));
    }

    [Fact]
    public async Task MoveFilesAsync_stops_before_the_file_the_hosts_question_turns_down()
    {
        var first = Path.Combine(_sourceDir, "first.msi");
        var second = Path.Combine(_sourceDir, "second.msi");
        await File.WriteAllTextAsync(first, "content");
        await File.WriteAllTextAsync(second, "content");

        // Asked before each file's progress report and as the last thing before it is
        // moved, which is how the command line stops a run the single-instance mutex no
        // longer covers. The file turned down gets no progress line.
        var reported = new List<string>();
        var svc = new MoveFilesService(new System.IO.Abstractions.FileSystem(), null, null, new HandleInHandFolder(), new HandleDestinationGuard(), _sourceDir);
        var result = await svc.MoveFilesAsync(StandingChecks.Of(first, second), _destDir,
            new SyncProgress<OperationProgress>(p => reported.Add(p.CurrentFileName)),
            mayAct: path => path != second);

        Assert.True(result.Cancelled);
        Assert.Equal(1, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.True(File.Exists(Path.Combine(_destDir, "first.msi")));
        Assert.True(File.Exists(second));
        Assert.False(File.Exists(Path.Combine(_destDir, "second.msi")));
        Assert.Equal(["first.msi"], reported);
    }

    [Fact]
    public async Task MoveFilesAsync_keeps_a_file_it_cannot_place_and_does_not_say_where_it_is()
    {
        // The file must still be kept: this is the containment invariant
        // SECURITY.md is written against, and Refused and Unproven both refuse
        // to touch the file. What changes is the sentence, because with a root
        // the kernel never expanded, "this file is not directly inside the
        // Windows Installer folder" states the outcome of a comparison that
        // could not be made.
        var unmounted = Helpers.TestHost.FirstUnmountedDriveLetter();
        if (unmounted is null)
            return; // every letter is in use on this host

        var file = Path.Combine(_sourceDir, "resolves-fine.msi");
        await File.WriteAllTextAsync(file, "content");

        // The batch's own folder is made in the sandbox, which the hand cannot do
        // under a drive nothing is mounted on, so the source checks are what refuse
        // the file here.
        var svc = new MoveFilesService(
            new System.IO.Abstractions.FileSystem(), null, null, new HandInTheSandbox(_sourceDir), new HandleDestinationGuard(),
            $@"{unmounted}:\Windows\Installer");
        var result = await svc.MoveFilesAsync(StandingChecks.Of(file), _destDir);

        Assert.Equal(0, result.MovedCount);
        Assert.True(File.Exists(file), "An unproven verdict keeps the file exactly as a refusal does");
        Assert.IsType<InstallerClean.Models.UnknownError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task MoveFilesAsync_reports_what_it_moved_when_the_destination_is_swapped()
    {
        // The per-iteration re-resolve exists to catch a destination relabelled
        // under the batch, and files have already left C:\Windows\Installer by
        // the time it fires: the user is owed the count and the line saying they
        // can put those files back. Needs a real reparse point, so it is
        // best-effort like the symlink tests next door, a directory symlink
        // needing SeCreateSymbolicLinkPrivilege that not every host grants.
        var first = Path.Combine(_sourceDir, "first.msi");
        var second = Path.Combine(_sourceDir, "second.msi");
        await File.WriteAllTextAsync(first, "one");
        await File.WriteAllTextAsync(second, "two");
        var decoy = TestTemp.NewFolder("ic-move-decoy-");

        // Asked before the batch, not during it: the swap happens inside a
        // progress callback, and a host that refuses the link there would leave
        // this test asserting an abort that had no reason to happen. Probing
        // first is what lets such a host stand aside instead. The callback now
        // runs inside the service's per-file try, so a refusal there would be
        // filed as one file's error rather than thrown, which makes the probe
        // more necessary than it was and not less: without it the failure would
        // arrive as a missing MoveAbortedException with nothing naming the cause.
        var probe = Path.Combine(TestTemp.Run, "ic-move-probe-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateSymbolicLink(probe, decoy);
            Directory.Delete(probe);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TestTemp.Delete(decoy);
            return; // this host cannot create a directory symlink
        }

        var swapped = false;
        var renamedAway = _destDir + "-renamed-away";
        var progress = new Helpers.SyncProgress<InstallerClean.Models.OperationProgress>(p =>
        {
            // Fires before each file's move. On the second, put a link to
            // somewhere else where the folder the batch resolved was. The folder is
            // renamed away and not deleted: the batch holds it open, and a folder
            // deleted while it is open can keep its name until that handle closes,
            // which would refuse the link.
            if (p.CurrentFile != 2 || swapped) return;
            foreach (var moved in Directory.GetFiles(_destDir))
                File.Move(moved, Path.Combine(decoy, Path.GetFileName(moved)));
            Directory.Move(_destDir, renamedAway);
            Directory.CreateSymbolicLink(_destDir, decoy);
            swapped = true;
        });

        // Taken before the batch, because after the swap the folder cannot be
        // asked any more: the name now resolves to the decoy. This is the answer
        // the service captured, so it is what the exception has to be carrying.
        InstallerCacheHelpers.TryResolveFinalPath(_destDir, out var resolvedDest);
        resolvedDest = resolvedDest.TrimEnd(Path.DirectorySeparatorChar);

        var svc = new MoveFilesService(new System.IO.Abstractions.FileSystem(), null, null, new HandleInHandFolder(), new HandleDestinationGuard(), _sourceDir);
        try
        {
            var ex = await Assert.ThrowsAsync<MoveAbortedException>(() =>
                svc.MoveFilesAsync(StandingChecks.Of(first, second), _destDir, progress));

            Assert.Equal(1, ex.Partial.MovedCount);
            Assert.Empty(ex.Partial.Errors);
            Assert.False(File.Exists(first), "The first file moved before the swap was caught");
            Assert.True(File.Exists(second), "The batch stopped rather than writing into the wrong place");
            // Where the file that moved actually went, which is the folder the
            // destination named when the batch started and not the string it was
            // asked about: that name now resolves to the decoy, so a host echoing
            // it would send the user to a folder holding none of their files. The
            // whole of what this exception reports is that the two came apart.
            Assert.Equal(resolvedDest, ex.Destination);
            Assert.NotEqual(decoy, ex.Destination);
            Assert.Equal(MoveAbortReason.ResolvesElsewhere, ex.Reason);
        }
        catch (Exception) when (TestTemp.KeepOnFailure(decoy)) { }
        finally
        {
            TestTemp.Delete(decoy);
            TestTemp.Delete(renamedAway);
        }
    }

    /// <summary>
    /// A hand that makes each batch's folder in a real folder of the test's own and
    /// acts by path, for the one test whose cache root is a drive nothing is mounted on.
    /// </summary>
    private sealed class HandInTheSandbox(string folder) : IInHandFolder
    {
        public IInHandBatch? OpenBatch(string installerFolder, out int failureCode) =>
            new ByPathInHandFolder(new System.IO.Abstractions.FileSystem()).OpenBatch(folder, out failureCode);

        public ILeftInHand? OpenLeft(string installerFolder) =>
            new ByPathInHandFolder(new System.IO.Abstractions.FileSystem()).OpenLeft(folder);
    }

    public void Dispose()
    {
        TestTemp.Delete(_sourceDir);
        TestTemp.Delete(_destDir);
    }
}
