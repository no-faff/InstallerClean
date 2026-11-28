using System.ComponentModel;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Resources;

namespace InstallerClean.Services;

/// <summary>
/// Puts back into the Installer folder what earlier batches left in the app's own folder
/// there (<see cref="IInHandFolder"/>). A Move or Delete has at most one file out of its
/// place at a time, so a process that dies in that moment, or a power cut, leaves at most
/// that one file in its batch's folder, whole; and a file Windows would not let a batch
/// put back stays there the same way.
///
/// THE SCAN RUNS IT BEFORE ANYTHING ELSE (<see cref="FileSystemScanService"/>), in whichever
/// process scans, so a file put back is walked and judged by that scan like any other.
/// </summary>
public interface IInHandRecovery
{
    /// <summary>
    /// Puts back what earlier batches left under <paramref name="installerFolder"/> and
    /// answers what it did. Where there is no in-hand folder it answers
    /// <see cref="InHandRecoveryResult.None"/> having read nothing else. It throws only
    /// for a cancel of <paramref name="cancellationToken"/>.
    ///
    /// A CALLER THAT GOES ON TO JUDGE FILES BY THEIR TIMES WAITS FIRST, where anything was
    /// put back (<see cref="InHandPause.AfterAPutBack"/>).
    /// </summary>
    Task<InHandRecoveryResult> RecoverAsync(string installerFolder, CancellationToken cancellationToken);
}

/// <summary>What one run of <see cref="IInHandRecovery.RecoverAsync"/> did.</summary>
/// <param name="PutBack">The files put back, each by its full path in the Installer folder.</param>
/// <param name="LeftAsSource">
/// Files left where they lie because a program or patch was installed from them there
/// (<see cref="ISourceRecords"/>). They stay for good.
/// </param>
/// <param name="LeftNameTaken">Files left because another file now has their name in the Installer folder.</param>
/// <param name="LeftRecordsUnread">Files left for this scan because the source records did not all read.</param>
/// <param name="LeftInstallerBusy">Files left for this scan because Windows Installer was busy.</param>
/// <param name="LeftRefused">
/// Files left because Windows refused the open or the rename for a reason other than the
/// file being in use or its name being taken.
/// </param>
/// <param name="LeftForALaterScan">
/// The names of the files counted in <paramref name="LeftNameTaken"/>,
/// <paramref name="LeftRecordsUnread"/>, <paramref name="LeftInstallerBusy"/> and
/// <paramref name="LeftRefused"/>, each without its folder: files every later scan tries
/// to put back again. The app's own empty file a batch makes to try a rename
/// (<see cref="LeftFile.IsProbe"/>), refused, is counted there and not named here.
/// </param>
/// <param name="LeftAsSourceNames">The names of the files counted in <paramref name="LeftAsSource"/>, each without its folder.</param>
public sealed record InHandRecoveryResult(
    IReadOnlyList<string> PutBack,
    int LeftAsSource = 0,
    int LeftNameTaken = 0,
    int LeftRecordsUnread = 0,
    int LeftInstallerBusy = 0,
    int LeftRefused = 0,
    IReadOnlyList<string>? LeftForALaterScan = null,
    IReadOnlyList<string>? LeftAsSourceNames = null)
{
    /// <summary>Nothing was put back and nothing was left.</summary>
    public static InHandRecoveryResult None { get; } = new(Array.Empty<string>());

    /// <summary>Never null: an absent list reads as no file left for a later scan.</summary>
    public IReadOnlyList<string> LeftForALaterScan { get; init; } = LeftForALaterScan ?? Array.Empty<string>();

    /// <summary>Never null: an absent list reads as no file left as a source.</summary>
    public IReadOnlyList<string> LeftAsSourceNames { get; init; } = LeftAsSourceNames ?? Array.Empty<string>();

    /// <summary>Whether the scan put back or left any file, which its host is then told (<see cref="ScanProgressUpdate.PutBack"/>).</summary>
    public bool SaysAnything => PutBack.Count > 0 || LeftForALaterScan.Count > 0 || LeftAsSourceNames.Count > 0;
}

/// <summary>
/// The app's <see cref="IInHandRecovery"/>.
/// </summary>
/// <remarks>
/// THE ORDER, and each step is there for the one after it:
/// <list type="number">
///   <item>Every file left is held so that nothing else can open it
///   (<see cref="ILeftInHand.Hold"/>). A file that is in use is one a batch running in
///   another process has out of its place at this moment, or one something is reading,
///   and is not counted or touched.</item>
///   <item>With the files held, whether Windows Installer is busy is read once
///   (<see cref="IPendingRebootService.CheckBusy"/>). An installation started from one of
///   these files can be under way with the file closed, and writes its record of where it
///   was installed from as it ends. With the file held nothing new can open it, and with
///   Windows Installer not busy after the hold no such installation is under way, so the
///   records read next are the whole of what names the file.</item>
///   <item>The source records are read (<see cref="ISourceRecords"/>). A file in a folder
///   one of them names is a program's or a patch's source where it lies, and stays.</item>
///   <item>Each other file is renamed back through its hold, replacing nothing.</item>
/// </list>
/// Where Windows Installer is busy, or a source record will not read, every held file is
/// let go where it lies, the crash log says so, and the next scan tries again.
/// </remarks>
internal sealed class InHandRecovery : IInHandRecovery
{
    private readonly IInHandFolder _hand;
    private readonly IPendingRebootService _signals;
    private readonly ISourceRecords _records;

    public InHandRecovery(IInHandFolder hand, IPendingRebootService signals, ISourceRecords records)
    {
        _hand = hand;
        _signals = signals;
        _records = records;
    }

    // Off the caller's thread: the window scans from its dispatcher, and this opens
    // folders and, where anything was left, reads the registry.
    public Task<InHandRecoveryResult> RecoverAsync(string installerFolder, CancellationToken cancellationToken) =>
        Task.Run(() => Recover(installerFolder, cancellationToken), cancellationToken);

    private InHandRecoveryResult Recover(string installerFolder, CancellationToken cancellationToken)
    {
        var putBack = new List<string>();
        var forALaterScan = new List<string>();
        var asSourceNames = new List<string>();
        int asSource = 0, nameTaken = 0, recordsUnread = 0, installerBusy = 0, refused = 0;
        InHandRecoveryResult Result() =>
            new(putBack, asSource, nameTaken, recordsUnread, installerBusy, refused, forALaterScan, asSourceNames);

        try
        {
            // Declared before the list of holds so that it is disposed of after every
            // hold is closed: disposing of it removes the folders that are then empty.
            using var left = _hand.OpenLeft(installerFolder);
            if (left is null) return InHandRecoveryResult.None;

            var held = new List<(LeftFile File, IHeldFile Hold)>();
            try
            {
                foreach (var batch in left.Batches)
                {
                    foreach (var file in batch.Files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var hold = left.Hold(file);
                        switch (hold.Outcome)
                        {
                            case HoldOutcome.Held when file.IsProbe:
                                // The app's own empty file, which was never in the
                                // Installer folder: closing the hold removes it.
                                hold.File!.Delete(out _);
                                hold.File.Dispose();
                                break;
                            case HoldOutcome.Held:
                                held.Add((file, hold.File!));
                                break;
                            case HoldOutcome.InUse:
                            case HoldOutcome.Gone:
                                break;
                            default:
                                refused++;
                                // The app's own empty file was never in the Installer folder,
                                // so it is not one a person is told could not go back there.
                                if (!file.IsProbe) forALaterScan.Add(file.Name);
                                CrashLog.TryWrite(new IOException(
                                    $"A file an earlier Move or Delete left in '{InHandFolders.FolderName}\\{file.Batch}' inside the Installer folder, '{file.Name}', is left where it is: "
                                    + (hold.Outcome == HoldOutcome.NotAPlainFile
                                        ? "it is a link or a folder, which the app does not act on."
                                        : "Windows refused to open it."),
                                    new Win32Exception(hold.WindowsError)));
                                break;
                        }
                    }
                }

                if (held.Count == 0) return Result();

                // Each of the two ways every held file is let go is written to the crash
                // log, once for the scan, with where the files lie: nothing else on the
                // PC says a file is there.
                if (InstallerIsBusy())
                {
                    installerBusy = held.Count;
                    forALaterScan.AddRange(held.Select(h => h.File.Name));
                    CrashLog.TryWrite(new IOException(
                        $"{Left(held.Count)} was not put back by this scan, because Windows Installer was busy. A scan made while it is not puts it back."));
                    return Result();
                }

                var records = _records.NamedBatches(left.Batches, cancellationToken);
                if (!records.Read)
                {
                    recordsUnread = held.Count;
                    forALaterScan.AddRange(held.Select(h => h.File.Name));
                    CrashLog.TryWrite(new IOException(
                        $"{Left(held.Count)} was not put back by this scan, because Windows Installer's records of where programs were installed from did not all read. An account that is signed out, with a program installed for that account alone, is one cause: a scan made while it is signed in can read them."));
                    return Result();
                }

                foreach (var (file, hold) in held)
                {
                    if (records.Names(file.Batch))
                    {
                        asSource++;
                        asSourceNames.Add(file.Name);
                        CrashLog.TryWrite(new IOException(
                            $"A file an earlier Move or Delete left in '{InHandFolders.FolderName}\\{file.Batch}' inside the Installer folder, '{file.Name}', is left where it is: Windows Installer's records name that folder as the place a program or a patch was installed from."));
                    }
                    else if (hold.PutBack(out var code))
                    {
                        putBack.Add(hold.RecordedPath);
                    }
                    else if (code == Kernel32.ERROR_ALREADY_EXISTS)
                    {
                        nameTaken++;
                        forALaterScan.Add(file.Name);
                        CrashLog.TryWrite(new IOException(
                            $"A file an earlier Move or Delete left in '{InHandFolders.FolderName}\\{file.Batch}' inside the Installer folder was not put back: another file now has its name, '{file.Name}', in the Installer folder. It is left where it is."));
                    }
                    else
                    {
                        refused++;
                        forALaterScan.Add(file.Name);
                        CrashLog.TryWrite(new IOException(
                            $"A file an earlier Move or Delete left in '{InHandFolders.FolderName}\\{file.Batch}' inside the Installer folder, '{file.Name}', was not put back: Windows refused the rename. It is left where it is.",
                            new Win32Exception(code)));
                    }
                }
            }
            finally
            {
                foreach (var (_, hold) in held) hold.Dispose();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException and not StackOverflowException)
        {
            // What was put back stays put back and is reported; whatever was not reached
            // lies where it lay, for the next scan.
            CrashLog.TryWrite(ex);
        }
        finally
        {
            // Written here, by whichever process scanned, so that a scan cancelled or
            // stopped after this point has still said what it did.
            Notice(CliEventClass.ScanPutBackNotice, putBack);
            Notice(CliEventClass.ScanLeftInHandNotice, forALaterScan);
            Notice(CliEventClass.ScanLeftAsSourceNotice, asSourceNames);
        }

        return Result();
    }

    /// <summary>
    /// Writes the Application log's entry of <paramref name="kind"/> naming the files in
    /// <paramref name="files"/>, in English, where there is one.
    /// </summary>
    private static void Notice(CliEventClass kind, IReadOnlyCollection<string> files)
    {
        if (files.Count == 0) return;
        var names = PutBackReport.Names(files);
        EventLogWriter.Write(kind, () => MachineCulture.English(() => string.Format(FormatFor(kind), names)));
    }

    /// <summary>The English resource for <paramref name="kind"/>'s entry, read inside the English scope.</summary>
    private static string FormatFor(CliEventClass kind) => kind switch
    {
        CliEventClass.ScanPutBackNotice => Strings.Cli_EventLogScanPutBack,
        CliEventClass.ScanLeftInHandNotice => Strings.Cli_EventLogScanLeftInHand,
        _ => Strings.Cli_EventLogScanLeftAsSource,
    };

    /// <summary>The opening of a crash-log entry about <paramref name="count"/> files let go where they lie.</summary>
    private static string Left(int count) =>
        $"What an earlier Move or Delete left in '{InHandFolders.FolderName}' inside the Installer folder ({count} file{(count == 1 ? string.Empty : "s")})";

    /// <summary>
    /// Whether Windows Installer is busy, a read that could not be made counting as busy:
    /// it has not shown that nothing is under way.
    /// </summary>
    private bool InstallerIsBusy()
    {
        try
        {
            return _signals.CheckBusy().IsBlocked;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            CrashLog.TryWrite(ex);
            return true;
        }
    }
}
