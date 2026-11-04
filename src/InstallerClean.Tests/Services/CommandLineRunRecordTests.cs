using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// <see cref="CommandLineRunRecord.ActedOnFiles"/> on lines written out in full: every
/// spelling of each of the four lines that record a run which moved or deleted files,
/// and the lines beside them that do not. The lines the command line writes today are
/// also read back from real runs, in <c>CliFirstRunMarkTests</c>.
/// </summary>
public class CommandLineRunRecordTests
{
    [Theory]
    [InlineData("/d mode: 2 of 2 files deleted permanently, 2.0 KB recovered, 0 errors.")]
    [InlineData("/d mode: 1 of 3 files deleted permanently, 1.0 KB recovered, 2 errors.")]
    [InlineData("/d mode: 5 of 5 files sent to the Recycle Bin, 3.1 GB recovered, 0 errors.")]
    [InlineData(@"/m mode: 77 of 77 files moved to D:\Backup, 2.88 GB relocated, 0 errors.")]
    [InlineData(@"/m mode: 1 of 1 file moved to D:\Backup, 300 B relocated, 0 errors.")]
    [InlineData(@"/m mode stopped: could no longer confirm the destination. 1 of 2 files had already moved to E:\Elsewhere, 100 B relocated, 0 errors.")]
    [InlineData("/d mode cancelled at the console: 1 of 2 files processed before cancellation. See progress output for the per-file detail.")]
    [InlineData("/d mode cancelled: 1 of 2 files processed before cancellation. See progress output for the per-file detail.")]
    [InlineData("/m mode cancelled at the console: 10 of 40 files processed before cancellation. See progress output for the per-file detail.")]
    [InlineData("/d mode interrupted by Ctrl+C: 3 of 9 files processed before cancellation. See progress output for the per-file detail.")]
    [InlineData("/m mode interrupted by Ctrl+C: 1 of 9 files processed before cancellation. See progress output for the per-file detail.")]
    [InlineData("/d mode: 10 of 10 files deleted permanently, 1.0 GB recovered, 0 errors.")]
    [InlineData("Delete mode (/d): 4 of 4 file(s) sent to the Recycle Bin, 1.2 GB recovered, 0 error(s).")]
    [InlineData(@"Move mode (/m): 3 of 5 file(s) moved to D:\Backup, 900 MB relocated, 2 error(s).")]
    public void A_line_recording_files_moved_or_deleted_counts(string entry)
    {
        Assert.True(CommandLineRunRecord.ActedOnFiles(entry));
    }

    [Theory]
    [InlineData("/d mode: 0 of 0 files deleted permanently, 0 B recovered, 0 errors.")]
    [InlineData("/d mode: 0 of 2 files deleted permanently, 0 B recovered, 2 errors.")]
    [InlineData("/d mode: 00 of 2 files sent to the Recycle Bin, 0 B recovered, 2 errors.")]
    [InlineData(@"/m mode: 0 of 2 files moved to D:\Backup, 0 B relocated, 2 errors.")]
    [InlineData(@"/m mode stopped: could no longer confirm the destination. 0 of 2 files had already moved to E:\Elsewhere, 0 B relocated, 0 errors.")]
    [InlineData("/d mode cancelled at the console: 0 of 2 files processed before cancellation. See progress output for the per-file detail.")]
    [InlineData("/d mode cancelled: 0 of 2 files processed before cancellation. See progress output for the per-file detail.")]
    [InlineData("Delete mode (/d): 0 of 4 file(s) sent to the Recycle Bin, 1.2 GB recovered, 4 error(s).")]
    [InlineData(@"Move mode (/m): 0 of 5 file(s) moved to D:\Backup, 900 MB relocated, 5 error(s).")]
    public void A_line_recording_no_file_moved_or_deleted_does_not_count(string entry)
    {
        Assert.False(CommandLineRunRecord.ActedOnFiles(entry));
    }

    [Theory]
    [InlineData("Scan mode (/s): 81 unneeded files found, 4.42 GB. No action taken.")]
    [InlineData("Scan mode (/s): no orphaned files. Installer database has 87 registered package(s).")]
    [InlineData("Scan mode (/d): no orphaned files. Installer database has 87 registered package(s).")]
    [InlineData("Move mode (/m) aborted: no destination specified.")]
    [InlineData(@"Move mode (/m) aborted: destination C:\Windows\Installer\x is inside C:\Windows\Installer.")]
    [InlineData("/s mode: no unneeded files.")]
    [InlineData("/d mode: no unneeded files.")]
    [InlineData("/s mode: no unneeded files identified.")]
    [InlineData("/d mode: no unneeded files identified.")]
    [InlineData("/d mode: nothing could be offered. InstallerClean could not establish that any of the cached files it found are unneeded, so it has held back 12 files. No action taken.")]
    [InlineData("/d mode cancelled at the console before any work was done. No action taken.")]
    [InlineData("/d mode cancelled before any work was done. No action taken.")]
    [InlineData(@"/m mode aborted: destination D:\Full has 1.0 GB free and the move needs 3.0 GB.")]
    [InlineData("/d mode aborted: Windows Installer mutex held.")]
    [InlineData("Run aborted: no argument supplied. No action taken.")]
    [InlineData("")]
    public void Every_other_line_does_not_count(string entry)
    {
        Assert.False(CommandLineRunRecord.ActedOnFiles(entry));
    }

    [Fact]
    public void A_line_is_read_from_its_start_and_not_from_a_path_inside_it()
    {
        // A Move of nothing whose folder name spells out a Delete that deleted files:
        // the Delete's line is matched from the start of the entry only.
        Assert.False(CommandLineRunRecord.ActedOnFiles(
            @"/m mode: 0 of 2 files moved to D:\/d mode: 5 of 5 files deleted permanently, x, 0 B relocated, 2 errors."));
    }
}
