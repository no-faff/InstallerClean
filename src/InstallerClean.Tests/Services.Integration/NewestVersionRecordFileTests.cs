using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// <see cref="NewestVersionRecord.RecordAndSayIfUpdated"/> against a real folder laid out as
/// the account's InstallerClean folder: what a start says, and what it leaves for the next.
/// </summary>
public class NewestVersionRecordFileTests : IDisposable
{
    private static readonly Version Running = new(3, 8, 0);

    // A settings.json as 3.5.0 to 3.7.0 write it: every field of their schema, and no
    // record of a version anywhere.
    private const string EarlierVersionsSettings = """
        {
          "moveDestination": "D:\\moved",
          "hasSentResultLog": true,
          "reportSendPending": false,
          "autoUpdateCheck": true,
          "language": "en-GB"
        }
        """;

    private readonly string _folder = TestTemp.NewFolder("newest-version-");
    private string Record => Path.Combine(_folder, NewestVersionRecord.FileName);
    private string Settings => Path.Combine(_folder, "settings.json");
    private string LastRun => Path.Combine(_folder, "last-run.json");

    public void Dispose() => TestTemp.Delete(_folder);

    private NewestVersionRecord NewRecord() => new(Record, [Settings, LastRun]);

    private bool Start() => NewRecord().RecordAndSayIfUpdated(Running);

    private void CommandLineRun() => NewRecord().RecordFirstRun(Running);

    [Fact]
    public void After_an_earlier_version_wrote_settings_it_says_updated_once()
    {
        File.WriteAllText(Settings, EarlierVersionsSettings);

        Assert.True(Start());
        Assert.False(Start());

        Assert.Equal("3.8.0", File.ReadAllText(Record));
        Assert.Equal(EarlierVersionsSettings, File.ReadAllText(Settings));
    }

    [Fact]
    public void After_an_earlier_version_left_only_its_report_it_says_updated()
    {
        File.WriteAllText(LastRun, "{}");

        Assert.True(Start());
    }

    [Fact]
    public void The_first_run_in_an_account_says_nothing_and_records_this_version()
    {
        Assert.False(Start());

        Assert.Equal("3.8.0", File.ReadAllText(Record));
    }

    [Fact]
    public void An_earlier_version_saving_its_settings_afterwards_does_not_bring_the_line_back()
    {
        File.WriteAllText(Settings, EarlierVersionsSettings);
        Assert.True(Start());

        // An earlier copy run in the same account saves settings.json whole, in its own schema.
        new SettingsService(Settings).TrySave(new AppSettings { Language = "de" });

        Assert.False(Start());
    }

    [Fact]
    public void An_older_record_says_updated_and_is_replaced()
    {
        File.WriteAllText(Record, "3.7.0");

        Assert.True(Start());

        Assert.Equal("3.8.0", File.ReadAllText(Record));
    }

    [Fact]
    public void An_older_copy_run_after_a_newer_one_says_nothing_and_leaves_the_newer_version()
    {
        File.WriteAllText(Record, "3.9.0");

        Assert.False(Start());

        Assert.Equal("3.9.0", File.ReadAllText(Record));
    }

    [Fact]
    public void A_record_holding_no_version_says_nothing_and_is_replaced()
    {
        File.WriteAllText(Settings, EarlierVersionsSettings);
        File.WriteAllText(Record, "not a version");

        Assert.False(Start());

        Assert.Equal("3.8.0", File.ReadAllText(Record));
    }

    [Fact]
    public void A_record_that_cannot_be_read_says_nothing_and_is_left_as_it_is()
    {
        // A lock is the everyday shape of a file that will not read. Real filesystem,
        // because sharing modes are a Windows behaviour.
        File.WriteAllText(Settings, EarlierVersionsSettings);
        File.WriteAllText(Record, "3.7.0");

        using (File.Open(Record, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.False(Start());

        Assert.Equal("3.7.0", File.ReadAllText(Record));
    }

    [Fact]
    public void A_record_that_cannot_be_written_never_says_updated_and_logs_nothing()
    {
        // A folder at the record's name refuses the rename, as a read-only profile would.
        using var crashLog = TestCrashLog.OwnFolder();
        File.WriteAllText(Settings, EarlierVersionsSettings);
        Directory.CreateDirectory(Record);

        Assert.False(Start());
        Assert.False(Start());

        Assert.Equal(string.Empty, TestCrashLog.Text());
    }

    [Fact]
    public void Files_a_start_writes_after_its_read_do_not_count_as_an_earlier_run()
    {
        var record = NewRecord();
        var reading = record.Read(Running);

        // A report this run writes between the read and the record.
        File.WriteAllText(LastRun, "{}");

        Assert.False(record.Record(reading));
        Assert.Equal("3.8.0", File.ReadAllText(Record));
    }

    [Fact]
    public void A_command_line_first_run_records_this_version_so_the_window_says_nothing()
    {
        CommandLineRun();
        // The command line's own report question writes these.
        File.WriteAllText(LastRun, "{}");
        File.WriteAllText(Settings, EarlierVersionsSettings);

        Assert.False(Start());
    }

    [Fact]
    public void A_command_line_run_after_an_earlier_version_leaves_the_line_to_the_window()
    {
        File.WriteAllText(Settings, EarlierVersionsSettings);

        CommandLineRun();

        Assert.False(File.Exists(Record));
        Assert.True(Start());
    }

    [Fact]
    public void A_command_line_run_leaves_an_older_record_for_the_window_to_move_up()
    {
        File.WriteAllText(Record, "3.7.0");

        CommandLineRun();

        Assert.Equal("3.7.0", File.ReadAllText(Record));
        Assert.True(Start());
    }
}
