using InstallerClean.Services;

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
    private readonly string _sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _destDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public MoveFilesServiceTests()
    {
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_destDir);
    }

    [Fact]
    public async Task MoveFilesAsync_moves_file_to_destination_on_real_filesystem()
    {
        var file = Path.Combine(_sourceDir, "test.msi");
        await File.WriteAllTextAsync(file, "content");

        // The source-containment guard requires sources to resolve inside the
        // cache root; point that root at the sandbox so a legitimately in-bounds
        // real file is not refused. The destination is a separate temp folder,
        // outside the sandbox, so its own gates pass.
        var svc = new MoveFilesService(new System.IO.Abstractions.FileSystem(), _sourceDir);
        var results = await svc.MoveFilesAsync(new[] { file }, _destDir);

        Assert.Empty(results.Errors);
        Assert.False(File.Exists(file));
        Assert.True(File.Exists(Path.Combine(_destDir, "test.msi")));
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

        var svc = new MoveFilesService(new System.IO.Abstractions.FileSystem(),
            $@"{unmounted}:\Windows\Installer");
        var result = await svc.MoveFilesAsync(new[] { file }, _destDir);

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
        var decoy = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(decoy);

        // Asked before the batch, not during it: the swap happens inside a
        // progress callback the service calls outside its per-file catch, so a
        // host that refuses the link there would fail this test rather than
        // stand aside from it.
        var probe = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            Directory.CreateSymbolicLink(probe, decoy);
            Directory.Delete(probe);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Directory.Delete(decoy, recursive: true);
            return; // this host cannot create a directory symlink
        }

        var swapped = false;
        var progress = new Helpers.SyncProgress<InstallerClean.Models.OperationProgress>(p =>
        {
            // Fires before each file's move. On the second, put a link to
            // somewhere else where the folder the batch resolved was.
            if (p.CurrentFile != 2 || swapped) return;
            foreach (var moved in Directory.GetFiles(_destDir))
                File.Move(moved, Path.Combine(decoy, Path.GetFileName(moved)));
            Directory.Delete(_destDir);
            Directory.CreateSymbolicLink(_destDir, decoy);
            swapped = true;
        });

        var svc = new MoveFilesService(new System.IO.Abstractions.FileSystem(), _sourceDir);
        try
        {
            var ex = await Assert.ThrowsAsync<MoveAbortedException>(() =>
                svc.MoveFilesAsync(new[] { first, second }, _destDir, progress));

            Assert.Equal(1, ex.Partial.MovedCount);
            Assert.Empty(ex.Partial.Errors);
            Assert.False(File.Exists(first), "The first file moved before the swap was caught");
            Assert.True(File.Exists(second), "The batch stopped rather than writing into the wrong place");
        }
        finally
        {
            if (Directory.Exists(decoy)) Directory.Delete(decoy, recursive: true);
        }
    }

    [Fact]
    public async Task MoveFilesAsync_records_a_batch_that_ran_without_the_installer_mutex()
    {
        // The sibling of DeleteFilesServiceTests' own record test, and the
        // reasoning is that file's: a skipped hold is not a refusal, and it is
        // the one window in which the act-time re-verify's proof can go stale
        // underneath the batch. Real filesystem because the record is a real
        // crash-log write, which is the behaviour under test; the move itself
        // runs against a mock so nothing on disk is touched.
        var logPath = InstallerClean.Helpers.CrashLog.Write(
            new InvalidOperationException("baseline for the move mutex fall-back record"));
        var baseline = new FileInfo(logPath).Length;

        var fs = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        var source = @"C:\Windows\Installer\never-reached.msi";
        fs.AddFile(source, new System.IO.Abstractions.TestingHelpers.MockFileData("payload"));
        fs.AddDirectory(_destDir);
        var mutex = new Tests.Helpers.FakeMutexProbe(Tests.Helpers.FakeMutexProbe.Mode.FallBack);

        var result = await new MoveFilesService(fs, mutex, null)
            .MoveFilesAsync(new[] { source }, _destDir);

        // The fall-back proceeds rather than refusing, and takes no lease.
        Assert.False(result.InstallerBusy);
        Assert.Equal(1, mutex.AcquireAttempts);
        Assert.Equal(0, mutex.Acquired);

        // Read from the baseline on, so a note left by an earlier run cannot
        // pass for this one's. A rotation between the two reads shortens the
        // file, and starting at 0 is then still right.
        using var stream = new System.IO.FileStream(
            logPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        var written = await reader.ReadToEndAsync();
        var appended = written.Length >= baseline ? written[(int)baseline..] : written;
        Assert.Contains("Move ran without the Windows Installer mutex", appended);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDir)) Directory.Delete(_sourceDir, recursive: true);
        if (Directory.Exists(_destDir)) Directory.Delete(_destDir, recursive: true);
    }
}
