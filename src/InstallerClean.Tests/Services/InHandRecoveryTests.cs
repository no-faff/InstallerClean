using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The rules by which a scan puts back what earlier batches left in the app's own folder
/// (<see cref="InHandRecovery"/>), against a hand, signals and source records that answer
/// what each test says. What Windows refuses on real files is the subject of
/// <c>InHandFolderTests</c>, and what the registry holds that of
/// <see cref="RegistrySourceRecordsTests"/>.
/// </summary>
public sealed class InHandRecoveryTests : IDisposable
{
    private const string Installer = @"C:\Windows\Installer";
    private const string BatchA = "0123456789abcdef0123456789abcdef";
    private const string BatchB = "fedcba9876543210fedcba9876543210";

    private readonly ScriptedLeftInHand _hand = new();
    private readonly ScriptedSignals _signals = new();
    private readonly ScriptedSourceRecords _records = new();
    private readonly IDisposable _crashLog = TestCrashLog.OwnFolder();

    public InHandRecoveryTests() => EventLogRecorder.Clear();

    public void Dispose()
    {
        EventLogRecorder.Clear();
        _crashLog.Dispose();
    }

    private InHandRecoveryResult Recover(CancellationToken token = default) =>
        new InHandRecovery(_hand, _signals, _records).RecoverAsync(Installer, token).GetAwaiter().GetResult();

    [Fact]
    public void A_pc_with_no_in_hand_folder_has_nothing_else_read()
    {
        _hand.HasInHandFolder = false;

        var result = Recover();

        Assert.Same(InHandRecoveryResult.None, result);
        Assert.Equal([Installer], _hand.Opened);
        Assert.Equal(0, _signals.BusyReads);
        Assert.Equal(0, _records.ReadCount);
        Assert.Empty(EventLogRecorder.Entries);
        Assert.Equal(string.Empty, TestCrashLog.Text());
    }

    [Fact]
    public void An_in_hand_folder_with_nothing_in_it_has_no_signal_and_no_record_read()
    {
        _hand.Batch(BatchA);

        var result = Recover();

        Assert.Empty(result.PutBack);
        Assert.Equal(0, _signals.BusyReads);
        Assert.Equal(0, _records.ReadCount);
        Assert.Equal(["open", "close"], _hand.Steps);
    }

    [Fact]
    public void A_file_left_goes_back_held_first_then_the_signals_then_the_records_then_the_rename()
    {
        _hand.Batch(BatchA, ("a.msi", 5));
        var order = new List<string>();
        _records.OnRead = () => order.Add($"records after {_signals.BusyReads} busy read(s) and steps [{string.Join(", ", _hand.Steps)}]");

        var result = Recover();

        Assert.Equal([Installer + @"\a.msi"], result.PutBack);
        Assert.Equal(1, _signals.BusyReads);
        Assert.Equal(1, _records.ReadCount);
        // The records were read with the file held, after the one busy read, and before any rename.
        Assert.Equal(["records after 1 busy read(s) and steps [open, hold a.msi]"], order);
        // The hold is let go before the folders are closed, which is what removes them.
        Assert.Equal(["open", "hold a.msi", "put-back a.msi", "let-go a.msi", "close"], _hand.Steps);
    }

    [Fact]
    public void Every_file_left_is_held_before_the_one_busy_read()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).Batch(BatchB, ("b.msp", 7));

        var result = Recover();

        Assert.Equal([Installer + @"\a.msi", Installer + @"\b.msp"], result.PutBack);
        Assert.Equal(1, _signals.BusyReads);
        Assert.Equal(
            ["open", "hold a.msi", "hold b.msp", "put-back a.msi", "put-back b.msp", "let-go a.msi", "let-go b.msp", "close"],
            _hand.Steps);
    }

    [Fact]
    public void A_file_in_use_is_a_running_batchs_and_is_left_without_a_count_or_a_read()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).HoldAnswers("a.msi", HoldOutcome.InUse);

        var result = Recover();

        Assert.Empty(result.PutBack);
        Assert.Equal(0, result.LeftRefused + result.LeftInstallerBusy + result.LeftRecordsUnread + result.LeftAsSource + result.LeftNameTaken);
        Assert.Equal(0, _signals.BusyReads);
        Assert.Equal(0, _records.ReadCount);
        Assert.Equal(["open", "hold a.msi", "close"], _hand.Steps);
        Assert.Equal(string.Empty, TestCrashLog.Text());
    }

    [Fact]
    public void A_file_that_has_gone_since_the_listing_is_not_counted()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).HoldAnswers("a.msi", HoldOutcome.Gone);

        var result = Recover();

        Assert.Empty(result.PutBack);
        Assert.Equal(0, result.LeftRefused);
    }

    [Theory]
    [InlineData(HoldOutcome.Refused)]
    [InlineData(HoldOutcome.NotAPlainFile)]
    public void A_file_that_will_not_open_is_left_and_counted_and_the_others_still_go_back(HoldOutcome outcome)
    {
        _hand.Batch(BatchA, ("a.msi", 5)).Batch(BatchB, ("b.msp", 7)).HoldAnswers("a.msi", outcome);

        var result = Recover();

        Assert.Equal(1, result.LeftRefused);
        Assert.Equal([Installer + @"\b.msp"], result.PutBack);
        Assert.DoesNotContain("put-back a.msi", _hand.Steps);
    }

    [Fact]
    public void While_Windows_Installer_is_busy_nothing_is_renamed_and_no_record_is_read()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).Batch(BatchB, ("b.msp", 7));
        _signals.Busy(PendingRebootResult.Block(PendingRebootReason.InstallerServiceRunning));

        var result = Recover();

        Assert.Empty(result.PutBack);
        Assert.Equal(2, result.LeftInstallerBusy);
        Assert.Single(TestCrashLog.Entries(), e => e.Contains("2 files", StringComparison.Ordinal));
        Assert.Equal(0, _records.ReadCount);
        Assert.Equal(["open", "hold a.msi", "hold b.msp", "let-go a.msi", "let-go b.msp", "close"], _hand.Steps);
        Assert.DoesNotContain(EventLogRecorder.Entries, e => e.Class == CliEventClass.ScanPutBackNotice);
    }

    [Fact]
    public void A_busy_read_that_throws_counts_as_busy()
    {
        _hand.Batch(BatchA, ("a.msi", 5));
        _signals.BusyThrows(new InvalidOperationException("the signals did not read"));

        var result = Recover();

        Assert.Empty(result.PutBack);
        Assert.Equal(1, result.LeftInstallerBusy);
        Assert.DoesNotContain("put-back a.msi", _hand.Steps);
    }

    [Fact]
    public void Where_the_source_records_do_not_all_read_nothing_is_renamed()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).Batch(BatchB, ("b.msp", 7));
        _records.Reads = false;
        // A record that did read names one batch. It decides nothing: the other batch's
        // file is not shown to be named by none.
        _records.Naming(BatchA);

        var result = Recover();

        Assert.Empty(result.PutBack);
        Assert.Equal(2, result.LeftRecordsUnread);
        Assert.Single(TestCrashLog.Entries(), e => e.Contains("2 files", StringComparison.Ordinal));
        Assert.Equal(0, result.LeftAsSource);
        Assert.DoesNotContain(_hand.Steps, s => s.StartsWith("put-back", StringComparison.Ordinal));
    }

    [Fact]
    public void A_file_in_a_folder_a_source_record_names_stays_and_the_rest_go_back()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).Batch(BatchB, ("b.msp", 7));
        _records.Naming(BatchA);

        var result = Recover();

        Assert.Equal(1, result.LeftAsSource);
        Assert.Equal([Installer + @"\b.msp"], result.PutBack);
        Assert.DoesNotContain("put-back a.msi", _hand.Steps);
        // The file that stays is named in the crash log, and the one put back is not.
        var entry = Assert.Single(TestCrashLog.Entries());
        Assert.Contains("a.msi", entry, StringComparison.Ordinal);
        Assert.DoesNotContain("b.msp", entry, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_whose_name_is_taken_stays_and_the_crash_log_names_it()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).PutBackRefused("a.msi", Kernel32.ERROR_ALREADY_EXISTS);

        var result = Recover();

        Assert.Empty(result.PutBack);
        Assert.Equal(1, result.LeftNameTaken);
        Assert.Equal(0, result.LeftRefused);
        var log = TestCrashLog.Text();
        Assert.Contains("a.msi", log, StringComparison.Ordinal);
        Assert.Contains(BatchA, log, StringComparison.Ordinal);
    }

    [Fact]
    public void A_rename_Windows_refuses_for_another_reason_leaves_the_file_counted_apart()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).PutBackRefused("a.msi", Kernel32.ERROR_ACCESS_DENIED);

        var result = Recover();

        Assert.Empty(result.PutBack);
        Assert.Equal(0, result.LeftNameTaken);
        Assert.Equal(1, result.LeftRefused);
        Assert.Contains("a.msi", TestCrashLog.Text(), StringComparison.Ordinal);
    }

    // The names of the files left, for the lines the person reads (PutBackReport).

    [Fact]
    public void Files_left_while_Windows_Installer_is_busy_are_named_for_a_later_scan()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).Batch(BatchB, ("b.msp", 7));
        _signals.Busy(PendingRebootResult.Block(PendingRebootReason.InstallerServiceRunning));

        var result = Recover();

        Assert.Equal(["a.msi", "b.msp"], result.LeftForALaterScan);
        Assert.Empty(result.LeftAsSourceNames);
        Assert.True(result.SaysAnything);
    }

    [Fact]
    public void Files_left_because_the_records_did_not_all_read_are_named_for_a_later_scan()
    {
        _hand.Batch(BatchA, ("a.msi", 5));
        _records.Reads = false;

        var result = Recover();

        Assert.Equal(["a.msi"], result.LeftForALaterScan);
        Assert.Empty(result.LeftAsSourceNames);
    }

    [Fact]
    public void A_file_left_as_a_source_is_named_apart_from_the_files_for_a_later_scan()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).Batch(BatchB, ("b.msp", 7)).PutBackRefused("b.msp", Kernel32.ERROR_ALREADY_EXISTS);
        _records.Naming(BatchA);

        var result = Recover();

        Assert.Equal(["a.msi"], result.LeftAsSourceNames);
        Assert.Equal(["b.msp"], result.LeftForALaterScan);
        Assert.Empty(result.PutBack);
    }

    [Fact]
    public void A_rename_refused_for_another_reason_is_named_for_a_later_scan()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).PutBackRefused("a.msi", Kernel32.ERROR_ACCESS_DENIED);

        var result = Recover();

        Assert.Equal(["a.msi"], result.LeftForALaterScan);
    }

    [Theory]
    [InlineData(HoldOutcome.Refused)]
    [InlineData(HoldOutcome.NotAPlainFile)]
    public void A_file_that_will_not_open_is_named_for_a_later_scan(HoldOutcome outcome)
    {
        _hand.Batch(BatchA, ("a.msi", 5)).HoldAnswers("a.msi", outcome);

        var result = Recover();

        Assert.Equal(["a.msi"], result.LeftForALaterScan);
    }

    [Fact]
    public void The_apps_own_probe_that_will_not_open_is_counted_and_not_named()
    {
        var probe = InHandFolders.NewProbeName();
        _hand.Batch(BatchA, (probe, 0)).HoldAnswers(probe, HoldOutcome.Refused);

        var result = Recover();

        Assert.Equal(1, result.LeftRefused);
        Assert.Empty(result.LeftForALaterScan);
        Assert.False(result.SaysAnything);
    }

    [Fact]
    public void A_file_in_use_is_neither_counted_nor_named()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).HoldAnswers("a.msi", HoldOutcome.InUse);

        var result = Recover();

        Assert.Empty(result.LeftForALaterScan);
        Assert.False(result.SaysAnything);
    }

    [Fact]
    public void Files_put_back_are_not_named_as_left()
    {
        _hand.Batch(BatchA, ("a.msi", 5));

        var result = Recover();

        Assert.Equal([Installer + @"\a.msi"], result.PutBack);
        Assert.Empty(result.LeftForALaterScan);
        Assert.Empty(result.LeftAsSourceNames);
        Assert.True(result.SaysAnything);
    }

    [Fact]
    public void The_apps_own_empty_probe_is_removed_and_never_put_in_the_Installer_folder()
    {
        var probe = InHandFolders.NewProbeName();
        _hand.Batch(BatchA, (probe, 0));

        var result = Recover();

        Assert.Empty(result.PutBack);
        Assert.Equal(["open", "hold " + probe, "delete " + probe, "let-go " + probe, "close"], _hand.Steps);
        // Nothing else was left, so nothing else was read.
        Assert.Equal(0, _signals.BusyReads);
        Assert.Equal(0, _records.ReadCount);
    }

    [Fact]
    public void A_file_with_the_probes_name_that_is_not_empty_is_a_file_like_any_other()
    {
        var name = InHandFolders.NewProbeName();
        _hand.Batch(BatchA, (name, 1));

        var result = Recover();

        Assert.Equal([Installer + @"\" + name], result.PutBack);
        Assert.DoesNotContain("delete " + name, _hand.Steps);
    }

    [Theory]
    [InlineData("InstallerClean-0123456789abcdef0123456789ABCDEF.tmp", true)]
    [InlineData("installerclean-0123456789abcdef0123456789abcdef.TMP", true)]
    [InlineData("InstallerClean-0123456789abcdef0123456789abcde.tmp", false)]
    [InlineData("InstallerClean-0123456789abcdef0123456789abcdeg.tmp", false)]
    [InlineData("InstallerClean-0123456789abcdef0123456789abcdef.msi", false)]
    [InlineData("InstallerClean-copying-0123456789abcdef0123456789abcdef.tmp", false)]
    [InlineData("1a2b3c.msi", false)]
    public void A_probes_name_is_told_from_every_other(string name, bool isProbe) =>
        Assert.Equal(isProbe, InHandFolders.IsProbeName(name));

    [Fact]
    public void A_put_back_is_written_to_the_Application_log_once_naming_each_file()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).Batch(BatchB, ("b.msp", 7));

        Recover();

        var entry = Assert.Single(EventLogRecorder.Entries);
        Assert.Equal(CliEventClass.ScanPutBackNotice, entry.Class);
        Assert.Equal(3008, CliContract.EventIdFor(entry.Class));
        Assert.Equal(System.Diagnostics.EventLogEntryType.Information, CliContract.EntryTypeFor(entry.Class));
        Assert.Contains("a.msi, b.msp", entry.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Files_left_for_a_later_scan_are_written_to_the_Application_log_as_a_warning_naming_each()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).Batch(BatchB, ("b.msp", 7));
        _signals.Busy(PendingRebootResult.Block(PendingRebootReason.InstallerServiceRunning));

        Recover();

        var entry = Assert.Single(EventLogRecorder.Entries);
        Assert.Equal(CliEventClass.ScanLeftInHandNotice, entry.Class);
        Assert.Equal(3009, CliContract.EventIdFor(entry.Class));
        Assert.Equal(System.Diagnostics.EventLogEntryType.Warning, CliContract.EntryTypeFor(entry.Class));
        Assert.Contains("a.msi, b.msp", entry.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Files_left_as_a_source_are_written_to_the_Application_log_as_information_apart_from_the_rest()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).Batch(BatchB, ("b.msp", 7)).PutBackRefused("b.msp", Kernel32.ERROR_ALREADY_EXISTS);
        _records.Naming(BatchA);

        Recover();

        Assert.Equal(2, EventLogRecorder.Entries.Count);
        var later = Assert.Single(EventLogRecorder.Entries, e => e.Class == CliEventClass.ScanLeftInHandNotice);
        Assert.Contains("b.msp", later.Text, StringComparison.Ordinal);
        var source = Assert.Single(EventLogRecorder.Entries, e => e.Class == CliEventClass.ScanLeftAsSourceNotice);
        Assert.Equal(3009, CliContract.EventIdFor(source.Class));
        Assert.Equal(System.Diagnostics.EventLogEntryType.Information, CliContract.EntryTypeFor(source.Class));
        Assert.Contains("a.msi", source.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("b.msp", source.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_scan_that_put_every_file_back_writes_no_entry_for_files_left()
    {
        _hand.Batch(BatchA, ("a.msi", 5));

        Recover();

        Assert.Equal(CliEventClass.ScanPutBackNotice, Assert.Single(EventLogRecorder.Entries).Class);
    }

    [Fact]
    public void The_apps_own_probe_refused_writes_no_entry()
    {
        var probe = InHandFolders.NewProbeName();
        _hand.Batch(BatchA, (probe, 0)).HoldAnswers(probe, HoldOutcome.Refused);

        Recover();

        Assert.Empty(EventLogRecorder.Entries);
    }

    [Fact]
    public void A_cancel_before_the_first_hold_throws_and_touches_nothing()
    {
        _hand.Batch(BatchA, ("a.msi", 5));
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => Recover(cancel.Token));

        Assert.DoesNotContain(_hand.Steps, s => s.StartsWith("hold", StringComparison.Ordinal));
    }

    [Fact]
    public void A_hand_that_throws_part_way_reports_what_was_put_back_and_does_not_throw()
    {
        _hand.Batch(BatchA, ("a.msi", 5)).Batch(BatchB, ("b.msp", 7));
        _records.OnRead = () => throw new IOException("the registry went away");

        var result = Recover();

        Assert.Empty(result.PutBack);
        Assert.Contains("the registry went away", TestCrashLog.Text(), StringComparison.Ordinal);
        // Both holds are let go whatever was thrown.
        Assert.Contains("let-go a.msi", _hand.Steps);
        Assert.Contains("let-go b.msp", _hand.Steps);
    }
}
