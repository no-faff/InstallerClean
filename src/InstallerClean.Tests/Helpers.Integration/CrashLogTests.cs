using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers.Integration;

public class CrashLogTests
{
    [Fact]
    public void The_suite_writes_the_log_under_the_temp_folder()
    {
        // TestCrashLog sets this before any test runs, so no test writes into the log of
        // the PC running the suite.
        Assert.NotNull(CrashLog.FolderForTests);
        Assert.StartsWith(Path.GetTempPath(), CrashLog.LogPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void An_entry_is_written_to_the_folder_it_is_sent_to()
    {
        var folder = Path.Combine(Path.GetTempPath(), "InstallerClean.Tests", Guid.NewGuid().ToString("N"));
        var marker = Guid.NewGuid().ToString("N");
        var suite = CrashLog.FolderForTests;
        CrashLog.FolderForTests = folder;
        try
        {
            var (path, written) = CrashLog.TryWrite(new InvalidOperationException(marker));

            Assert.True(written);
            Assert.Equal(Path.Combine(folder, "crash.log"), path);
            Assert.Contains(marker, File.ReadAllText(path), StringComparison.Ordinal);
        }
        finally
        {
            CrashLog.FolderForTests = suite;
            TestCrashLog.TryDelete(folder);
        }
    }

    [Fact]
    public void An_entry_is_stamped_in_one_shape_whatever_the_culture()
    {
        // A culture whose time separator is '.' and whose calendar is not the Gregorian one,
        // as the user's culture can be: Danish under ICU, Thai with its Buddhist calendar.
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("th-TH").Clone();
        culture.DateTimeFormat.Calendar = new ThaiBuddhistCalendar();
        culture.DateTimeFormat.TimeSeparator = ".";
        var folder = Path.Combine(Path.GetTempPath(), "InstallerClean.Tests", Guid.NewGuid().ToString("N"));
        var suite = CrashLog.FolderForTests;
        var before = CultureInfo.CurrentCulture;
        CrashLog.FolderForTests = folder;
        CultureInfo.CurrentCulture = culture;
        try
        {
            var (path, written) = CrashLog.TryWrite(new InvalidOperationException("stamp"));

            Assert.True(written);
            var stamp = File.ReadAllLines(path).Single(line => line.StartsWith("---- ", StringComparison.Ordinal));
            Assert.Matches(@"^---- 20\d\d-\d\d-\d\d \d\d:\d\d:\d\d [+-]\d\d:\d\d ----$", stamp);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
            CrashLog.FolderForTests = suite;
            TestCrashLog.TryDelete(folder);
        }
    }

    [Fact]
    public void A_folder_whose_delete_is_refused_is_left_without_a_throw()
    {
        // A read-only file makes the delete throw UnauthorizedAccessException on Windows, as a
        // file an antivirus holds can. Elsewhere a file's own mode does not stop its delete and
        // its folder's does, so there the folder is made read-only instead.
        var folder = Path.Combine(Path.GetTempPath(), "InstallerClean.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "crash.log");
        File.WriteAllText(file, "held");
#if NET
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        else
#endif
            File.SetAttributes(file, FileAttributes.ReadOnly);
        try
        {
            Assert.Null(Record.Exception(() => TestCrashLog.TryDelete(folder)));
            Assert.True(File.Exists(file), "The delete was not refused, so the folder went.");
        }
        finally
        {
#if NET
            if (!OperatingSystem.IsWindows())
            {
                if (Directory.Exists(folder))
                    File.SetUnixFileMode(folder,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            else
#endif
            if (File.Exists(file)) File.SetAttributes(file, FileAttributes.Normal);
            TestCrashLog.TryDelete(folder);
        }
    }

    [Fact]
    public void Without_a_folder_set_the_log_is_in_the_app_s_own_folder()
    {
        // Read without writing, so the PC's own log is untouched. The folder is asked for
        // before the suite's log is set aside, so a failure written while it settles goes
        // to the suite's log.
        var folder = AppDataFolder.Location;
        var suite = CrashLog.FolderForTests;
        CrashLog.FolderForTests = null;
        try
        {
            Assert.Equal(Path.Combine(folder, "crash.log"), CrashLog.LogPath);
            Assert.EndsWith(Path.Combine("InstallerClean", "crash.log"), CrashLog.LogPath);
        }
        finally
        {
            CrashLog.FolderForTests = suite;
        }
    }

    [Fact]
    public void A_fresh_log_after_Prepare_is_headed_in_the_language_Prepare_ran_in()
    {
        var first = Path.Combine(Path.GetTempPath(), "InstallerClean.Tests", Guid.NewGuid().ToString("N"));
        var second = Path.Combine(Path.GetTempPath(), "InstallerClean.Tests", Guid.NewGuid().ToString("N"));
        var suite = CrashLog.FolderForTests;
        try
        {
            Localisation.SetPickedLanguage(CultureInfo.GetCultureInfo("fr"));
            var french = FirstLine(Strings.CrashLog_PrivacyHeader);
            CrashLog.FolderForTests = first;
            CrashLog.Prepare();
            Localisation.Reset();
            var english = FirstLine(Strings.CrashLog_PrivacyHeader);
            Assert.NotEqual(french, english);

            var (path, written) = CrashLog.TryWrite(new InvalidOperationException("after Prepare"));
            Assert.True(written);
            Assert.Equal(french, FirstLine(File.ReadAllText(path)));

            // With the kept header dropped, a write builds its own in the language of the moment.
            CrashLog.ForgetPreparedHeader();
            CrashLog.FolderForTests = second;
            (path, written) = CrashLog.TryWrite(new InvalidOperationException("no header kept"));
            Assert.True(written);
            Assert.Equal(english, FirstLine(File.ReadAllText(path)));
        }
        finally
        {
            Localisation.Reset();
            CrashLog.ForgetPreparedHeader();
            CrashLog.FolderForTests = suite;
            TestCrashLog.TryDelete(first);
            TestCrashLog.TryDelete(second);
        }
    }

    private static string FirstLine(string text) => text.Split('\n')[0].TrimEnd('\r');

    [Fact]
    public void TryWrite_never_throws_even_when_exception_serialisation_is_unusual()
    {
        var nested = new Exception("outer", new Exception("inner", new Exception("innermost")));

        var ex = Record.Exception(() => CrashLog.TryWrite(nested));

        Assert.Null(ex);
    }
}
