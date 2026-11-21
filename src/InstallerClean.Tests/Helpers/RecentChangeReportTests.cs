using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The sentence both hosts print about the files a scan held back because their times
/// are not before the moment it began, as rendered.
/// </summary>
public class RecentChangeReportTests
{
    private static readonly CultureInfo British = CultureInfo.GetCultureInfo("en-GB");

    private const long Bytes = 3 * 1_048_576;

    private static ScanResult Holding(int count, long bytes) =>
        new(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            WithheldBy: new WithholdingSplit(NotBeforeTheClockCount: count),
            WithheldNotBeforeTheClockBytes: bytes,
            InstallerActivityAtScan: PendingRebootResult.Clean);

    // ---- When there is no sentence ----

    [Fact]
    public void No_scan_and_no_file_held_for_its_times_produce_no_sentence()
    {
        Assert.Equal(string.Empty, RecentChangeReport.Line(null));
        Assert.Equal(string.Empty, RecentChangeReport.Line(Holding(0, 0)));
    }

    [Theory]
    [InlineData(PendingRebootReason.MsiExecuteMutexHeld)]
    [InlineData(PendingRebootReason.PendingRenameInCache)]
    [InlineData(PendingRebootReason.RegistryCheckUnreadable)]
    public void Files_held_because_a_signal_was_set_get_no_sentence(PendingRebootReason reason)
    {
        // Those files are held whatever their times, and the signal's own line speaks
        // for them, so the sentence about times would be false of them.
        var scan = new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            WithheldBy: new WithholdingSplit(ScanSignalCount: 3),
            InstallerActivityAtScan: PendingRebootResult.Block(reason));

        Assert.Equal(string.Empty, RecentChangeReport.Line(scan));
        Assert.False(scan.HasRecentChangeLine);
    }

    [Fact]
    public void A_scan_that_read_no_signals_still_gets_the_sentence()
    {
        // A result built with no reading is a scan with no reader, and its files were
        // held for their times.
        var scan = new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            WithheldBy: new WithholdingSplit(NotBeforeTheClockCount: 2),
            WithheldNotBeforeTheClockBytes: Bytes);

        Assert.NotEqual(string.Empty, RecentChangeReport.Line(scan));
    }

    // ---- The sentence ----

    [Fact]
    public void Several_files_give_the_count_and_the_size()
    {
        using var scope = new LocalisationScope(British, British);

        var line = RecentChangeReport.Line(Holding(3, Bytes));

        Assert.Equal(
            $"3 files ({DisplayHelpers.FormatSize(Bytes)}) were left alone because Windows shows they were "
            + "changed after InstallerClean started scanning. If a program was installing or updating, "
            + "scan again once it has finished and InstallerClean will check them again.",
            line);
    }

    [Fact]
    public void One_file_takes_the_sentence_naming_its_size_alone()
    {
        using var scope = new LocalisationScope(British, British);

        var line = RecentChangeReport.Line(Holding(1, Bytes));

        Assert.Equal(
            $"One file ({DisplayHelpers.FormatSize(Bytes)}) was left alone because Windows shows it was "
            + "changed after InstallerClean started scanning. If a program was installing or updating, "
            + "scan again once it has finished and InstallerClean will check it again.",
            line);
    }

    private sealed class LocalisationScope : IDisposable
    {
        public LocalisationScope(CultureInfo uiCulture, CultureInfo formatCulture) =>
            Localisation.Set(uiCulture, formatCulture);

        public void Dispose() => Localisation.Reset();
    }
}
