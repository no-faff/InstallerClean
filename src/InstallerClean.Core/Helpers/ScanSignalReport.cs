using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// The line saying why a scan held back every file from the folder walk, where one of
/// Windows Installer's in-progress signals was set when it read them just after its
/// clock (<see cref="ScanResult.InstallerActivityAtScan"/>): one sentence per reason the
/// gate's banners tell apart, in a window form and a command-line form, each ending with
/// what to do there: Re-scan or running the command again, or restarting Windows where a
/// restart clears the signal. And the line in
/// the same place where the system clock went back while the scan read the files' times
/// (<see cref="WithholdingSplit.ClockSetBackCount"/>), which never meets a signal's line:
/// where a signal was set, no file's times are read.
///
/// It lives in Core for the reason <see cref="HeldBackReport"/> does, and both forms come
/// from one grouping so the two hosts cannot come to say different things about one scan.
///
/// IT SPEAKS OF THE MOMENT THE SCAN STARTED, IN THE PAST TENSE, because the signal may
/// have cleared by the time the line is read. Whether it is still set is the separate read
/// after the scan, which the window's warning banner and the command line's refusal speak
/// for, so a reader can meet both lines about one scan and both are true.
/// </summary>
internal static class ScanSignalReport
{
    /// <summary>
    /// The window's line for <paramref name="scan"/>, or empty where there is no scan or
    /// it held no file for a signal (<see cref="HeldFor"/>) or for the clock going back.
    /// </summary>
    internal static string WindowLine(ScanResult? scan) =>
        HeldFor(scan) is { } signal ? WindowLine(signal)
        : scan is { WithheldBy.ClockSetBackCount: > 0 } ? Strings.Summary_ScanSignal_ClockSetBack
        : string.Empty;

    /// <summary>
    /// The command line's line for <paramref name="scan"/>, or empty where it held no file
    /// for a signal (<see cref="HeldFor"/>) or for the clock going back.
    /// </summary>
    internal static string CommandLine(ScanResult scan) =>
        HeldFor(scan) is { } signal ? CommandLine(signal)
        : scan.WithheldBy.ClockSetBackCount > 0 ? Strings.Cli_ScanSignal_ClockSetBack
        : string.Empty;

    /// <summary>
    /// The reading of the signals <paramref name="scan"/> held files for, or null where it
    /// held none.
    ///
    /// A SIGNAL SET WITH NO FILE HELD FOR IT GETS NO LINE. Where no file from the walk
    /// reached the age check, the scan found the signal and held nothing on it, and a line
    /// saying it held back the files no record names would be false.
    /// </summary>
    internal static PendingRebootResult? HeldFor(ScanResult? scan) =>
        scan is { WithheldBy.ScanSignalCount: > 0, InstallerActivityAtScan: { IsBlocked: true, Reason: not null } signal }
            ? signal
            : null;

    /// <summary>The window's line for a signal set at the scan's start.</summary>
    internal static string WindowLine(PendingRebootReason reason) =>
        WindowLine(PendingRebootResult.Block(reason));

    /// <summary>The command line's line for a signal set at the scan's start.</summary>
    internal static string CommandLine(PendingRebootReason reason) =>
        CommandLine(PendingRebootResult.Block(reason));

    /// <summary>
    /// The window's line for <paramref name="signal"/>. A lock whose check failed on a
    /// Windows error is given with that error (<see cref="PendingRebootResult.WindowsError"/>).
    /// </summary>
    internal static string WindowLine(PendingRebootResult signal) => GroupOf(signal.Reason) switch
    {
        Group.Busy => Strings.Summary_ScanSignal_Busy,
        Group.Suspended => Strings.Summary_ScanSignal_Suspended,
        Group.RenameInCache => Strings.Summary_ScanSignal_RenameInCache,
        Group.RenameUnresolved => Strings.Summary_ScanSignal_RenameUnresolved,
        Group.AccessRefused => Strings.Summary_ScanSignal_AccessRefused,
        Group.RegistryUnreadable => Strings.Summary_ScanSignal_RegistryUnreadable,
        Group.LockUnreadable => WindowsErrors.Sentence(signal.WindowsError,
            Strings.Summary_ScanSignal_LockUnreadable, Strings.Summary_ScanSignal_LockUnreadableWindowsError),
        _ => Strings.Summary_ScanSignal_Other,
    };

    /// <summary>The command line's line for <paramref name="signal"/>, as <see cref="WindowLine(PendingRebootResult)"/>.</summary>
    internal static string CommandLine(PendingRebootResult signal) => GroupOf(signal.Reason) switch
    {
        Group.Busy => Strings.Cli_ScanSignal_Busy,
        Group.Suspended => Strings.Cli_ScanSignal_Suspended,
        Group.RenameInCache => Strings.Cli_ScanSignal_RenameInCache,
        Group.RenameUnresolved => Strings.Cli_ScanSignal_RenameUnresolved,
        Group.AccessRefused => Strings.Cli_ScanSignal_AccessRefused,
        Group.RegistryUnreadable => Strings.Cli_ScanSignal_RegistryUnreadable,
        Group.LockUnreadable => WindowsErrors.Sentence(signal.WindowsError,
            Strings.Cli_ScanSignal_LockUnreadable, Strings.Cli_ScanSignal_LockUnreadableWindowsError),
        _ => Strings.Cli_ScanSignal_Other,
    };

    /// <summary>
    /// Which sentence a reason takes: one per reason the gate's banners tell apart, so
    /// each says what the gate's own sentence for it says, in the past tense. The two
    /// refusals share one, as their banners do. A reason with no sentence of its own takes
    /// <see cref="Group.Other"/>, whose sentence is true of the whole family, so a reason
    /// added later is spoken of rather than reported as a crash.
    ///
    /// WINDOWS INSTALLER'S IN-PROGRESS FILE TAKES THE OTHER SENTENCE, as the gate's banner
    /// and its command-line line give it: the file records that an installation has not
    /// finished, which is not the same as something using Windows Installer at that
    /// moment, and a file left behind says the first and not the second.
    /// </summary>
    private static Group GroupOf(PendingRebootReason? reason) => reason switch
    {
        PendingRebootReason.MsiExecuteMutexHeld => Group.Busy,
        PendingRebootReason.InstallerInProgress => Group.Suspended,
        PendingRebootReason.PendingRenameInCache => Group.RenameInCache,
        PendingRebootReason.PendingRenameUnresolved => Group.RenameUnresolved,
        PendingRebootReason.MsiExecuteMutexAccessRefused
            or PendingRebootReason.InstallerInProgressMarkerAccessRefused => Group.AccessRefused,
        PendingRebootReason.RegistryCheckUnreadable => Group.RegistryUnreadable,
        PendingRebootReason.MsiExecuteMutexUnreadable => Group.LockUnreadable,
        _ => Group.Other,
    };

    private enum Group
    {
        Other,
        Busy,
        Suspended,
        RenameInCache,
        RenameUnresolved,
        AccessRefused,
        RegistryUnreadable,
        LockUnreadable,
    }
}
