using System.ComponentModel;
using System.Runtime.InteropServices;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The production <see cref="FileTimesReader"/> against the real filesystem, in the
/// test run's temp folder, which on the machines the suite runs on is a local NTFS volume.
///
/// THE LAST TWO TESTS HOLD THE AGE CHECK'S FOOTING AS WELL AS THE READER. The check
/// dates a file by the latest of its creation, last-write and change times, so a copy
/// has to come out of each way of making one with at least one of those three its
/// own. Setting a file's older times and copying a file are the two ways these tests
/// can make one.
/// </summary>
public class FileTimesReaderTests
{
    private static readonly DateTime LongAgo = new(2015, 3, 17, 8, 42, 22, DateTimeKind.Utc);

    [Fact]
    public void A_new_file_in_the_temp_folder_reads_with_times_from_now()
    {
        var path = NewFile();
        try
        {
            var before = DateTime.UtcNow.AddMinutes(-5);

            var outcome = new FileTimesReader().ReadOutcome(path, out var times);

            Assert.Equal(FileTimesRead.Read, outcome);
            Assert.True(times.ChangeUtc > before, $"change time {times.ChangeUtc:O}");
            Assert.True(times.CreationUtc > before, $"creation time {times.CreationUtc:O}");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_path_with_nothing_at_it_names_nothing()
    {
        var path = TestTemp.NewFilePath("ic-times-absent-", ".msi");

        Assert.Equal(FileTimesRead.NamesNothing, new FileTimesReader().ReadOutcome(path, out _));
    }

    [Fact]
    public void A_directory_is_not_a_plain_file()
    {
        var path = TestTemp.NewFilePath("ic-times-dir-");
        Directory.CreateDirectory(path);
        try
        {
            Assert.Equal(FileTimesRead.NotAPlainFile, new FileTimesReader().ReadOutcome(path, out _));
        }
        finally
        {
            Directory.Delete(path);
        }
    }

    [Fact]
    public void A_file_given_old_creation_and_last_write_times_is_still_dated_by_a_time_of_its_own()
    {
        var path = NewFile();
        try
        {
            File.SetCreationTimeUtc(path, LongAgo);
            File.SetLastWriteTimeUtc(path, LongAgo);
            var before = DateTime.UtcNow.AddMinutes(-5);

            var outcome = new FileTimesReader().ReadOutcome(path, out var times);

            Assert.Equal(FileTimesRead.Read, outcome);
            Assert.Equal(LongAgo, times.CreationUtc);
            Assert.Equal(LongAgo, times.LastWriteUtc);
            Assert.True(CachedFileAge.Latest(times) > before,
                $"creation {times.CreationUtc:O}, last write {times.LastWriteUtc:O}, change {times.ChangeUtc:O}");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_file_whose_three_times_are_all_set_old_reads_as_old()
    {
        // The control for the tests around it: every other test here expects a recent
        // time, and a reader that answered the present for everything would pass them.
        var path = NewFile();
        try
        {
            SetAllTimes(path, LongAgo);

            var outcome = new FileTimesReader().ReadOutcome(path, out var times);

            Assert.Equal(FileTimesRead.Read, outcome);
            Assert.Equal(new FileTimes(LongAgo, LongAgo, LongAgo), times);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_copy_of_a_file_whose_three_times_are_old_is_dated_by_a_time_of_its_own()
    {
        // File.Copy is CopyFile underneath. The source's three times are all set old
        // first, so whatever the copy takes from its source is old, and a recent
        // latest time can only be one the copy has of its own.
        var source = NewFile();
        var copy = source + ".copy";
        try
        {
            SetAllTimes(source, LongAgo);
            Assert.Equal(FileTimesRead.Read, new FileTimesReader().ReadOutcome(source, out var sourceTimes));
            Assert.Equal(LongAgo, CachedFileAge.Latest(sourceTimes));
            var before = DateTime.UtcNow.AddMinutes(-5);

            File.Copy(source, copy);
            var outcome = new FileTimesReader().ReadOutcome(copy, out var times);

            Assert.Equal(FileTimesRead.Read, outcome);
            Assert.True(CachedFileAge.Latest(times) > before,
                $"creation {times.CreationUtc:O}, last write {times.LastWriteUtc:O}, change {times.ChangeUtc:O}");
        }
        finally
        {
            File.Delete(source);
            if (File.Exists(copy)) File.Delete(copy);
        }
    }

    private static string NewFile()
    {
        var path = TestTemp.NewFilePath("ic-times-", ".msi");
        File.WriteAllBytes(path, new byte[16]);
        return path;
    }

    /// <summary>
    /// Sets the file's creation, last-access, last-write and change times to
    /// <paramref name="at"/> in one call. The change time can only be set this way,
    /// by naming it; the framework's setters leave it to the file system.
    /// </summary>
    private static void SetAllTimes(string path, DateTime at)
    {
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.ReadWrite);
        var time = at.ToFileTimeUtc();
        var info = new FileBasicInfo
        {
            CreationTime = time,
            LastAccessTime = time,
            LastWriteTime = time,
            ChangeTime = time,
            FileAttributes = 0,
        };
        if (!SetFileInformationByHandle(handle, FileBasicInfoClass, ref info,
                (uint)Marshal.SizeOf<FileBasicInfo>()))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    private const int FileBasicInfoClass = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct FileBasicInfo
    {
        public long CreationTime;
        public long LastAccessTime;
        public long LastWriteTime;
        public long ChangeTime;
        public uint FileAttributes;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetFileInformationByHandle(
        Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
        int fileInformationClass,
        ref FileBasicInfo lpFileInformation,
        uint dwBufferSize);
}
