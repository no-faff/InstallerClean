using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Resources;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Services;

/// <summary>
/// Puts this exe, a newer version a copy downloaded into the staging folder, at that
/// copy's path once the copy has ended (<see cref="HandOverContract.ReadReplaceRequest"/>).
/// </summary>
/// <remarks>
/// <para>
/// The newer version is written beside the copy's exe under the exe's name with
/// <see cref="ReplacingSuffix"/> on the end, and once the copy has ended it is moved over
/// the exe in one step, so the path holds the old file or the new one at every instant and
/// no file anything runs from is ever renamed. Before that move the old file is given a
/// second name, the exe's name with <see cref="BackupSuffix"/> on the end, which is what is
/// moved back where the newer version does not start.
/// </para>
/// <para>
/// Everything that can fail while the copy is still running, the check that the path is
/// the copy's own and the write beside it, comes before the started event is set. Where
/// one fails, the event stays unset, the copy is still running, and nothing at its path
/// has changed.
/// </para>
/// <para>
/// For as long as it runs, the replace holds a file named for the exe with
/// <see cref="LockSuffix"/> on the end, open and shared with nothing, and Windows deletes
/// it as the handle closes, however the process ends. <see cref="TryRemoveLeftovers(string)"/>
/// holds the same file while it deletes, so the two never run at once.
/// </para>
/// <para>
/// The steps after the copy has ended run in this version's code, the newer one, so a
/// later release can change them for every copy that updates to it.
/// </para>
/// </remarks>
internal static class ExeReplacement
{
    /// <summary>What the newer version's name carries while it waits beside the exe it replaces.</summary>
    public const string ReplacingSuffix = ".replacing";

    /// <summary>What the exe's second name carries while the newer version is put in its place.</summary>
    public const string BackupSuffix = ".backup";

    /// <summary>What the name of the file a replace holds while it runs carries.</summary>
    public const string LockSuffix = ".updating";

    /// <summary>
    /// What the setup renames a file beside the exe to where it is still running, cannot
    /// be deleted and is not needed: an earlier version once an install has completed,
    /// or a newer one a failed install left (installer/InstallerClean.iss, DoneSuffix).
    /// It is followed by nothing or by a hyphen and a number where that name was taken.
    /// A file the setup holds aside while it writes has another name, since it can be the
    /// only copy of an earlier version, and is never deleted here.
    /// </summary>
    public const string SetupDoneSuffix = ".setup-done";

    /// <summary>
    /// How long the copy is waited for before crash.log says the replace is still waiting
    /// for it to close. The wait goes on after that.
    /// </summary>
    internal static readonly TimeSpan OldCopyNotice = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How long a move refused because the file is held is tried again. An antivirus can
    /// hold a program it has just seen written or ended while it looks at it, and a file
    /// held that way answers a move with a sharing violation, a lock violation or access denied.
    /// </summary>
    internal static readonly TimeSpan MoveRetryBound = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long a replace tries again to hold the file named with <see cref="LockSuffix"/>
    /// where it cannot. <see cref="TryRemoveLeftovers(string)"/> holds that file for as long as its
    /// deletes take, and a replace starting meanwhile takes it once they are done.
    /// </summary>
    internal static readonly TimeSpan HoldRetryBound = TimeSpan.FromSeconds(2);

    private static readonly TimeSpan FirstRetryInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan LongestRetryInterval = TimeSpan.FromSeconds(2);

    /// <summary>The longest path Windows gives for a process's image, in characters.</summary>
    private const int MaxImagePathLength = 32768;

    /// <summary>
    /// Carries out <paramref name="request"/> and answers how it ended. Where
    /// <paramref name="startAtPath"/>, the exe at the path is started once the newer
    /// version is there and waited for until it says its own code is running, and where it
    /// does not, the old file is put back. Every failure is written to crash.log. Never throws.
    /// A command <paramref name="request"/> carries is not run here: the caller runs it, or
    /// refuses the request.
    /// </summary>
    public static ReplaceOutcome Run(ReplaceRequest request, bool startAtPath) =>
        Run(request, startAtPath, new ReplaceSeams());

    /// <summary>
    /// Test seam: <see cref="Run(ReplaceRequest, bool)"/>, with <paramref name="seams"/> in
    /// place of the real parts. Where the answer is <see cref="ReplaceOutcome.NothingAtPath"/>,
    /// the Application log says so too, after crash.log.
    /// </summary>
    internal static ReplaceOutcome Run(ReplaceRequest request, bool startAtPath, ReplaceSeams seams)
    {
        ReplaceOutcome outcome;
        try
        {
            outcome = RunSteps(request, startAtPath, seams);
        }
        catch (Exception ex)
        {
            // Only the steps before the hold throw past RunSteps, and none of them sets the
            // started event, so the copy carries on.
            CrashLog.TryWrite(ex);
            outcome = ReplaceOutcome.NotHandedOver;
        }
        if (outcome == ReplaceOutcome.NothingAtPath) WriteNothingAtPathNotice();
        return outcome;
    }

    /// <summary>
    /// Writes the Application-log notice that this version was not installed because nothing
    /// was at the path once the copy had ended, in English whatever the display language.
    /// </summary>
    private static void WriteNothingAtPathNotice()
    {
        var version = typeof(ExeReplacement).Assembly.GetName().Version ?? new Version(0, 0, 0);
        EventLogWriter.Write(CliEventClass.UpdateNotInstalledNotice, () => MachineCulture.English(() =>
            string.Format(CultureInfo.InvariantCulture, Strings.Cli_EventLogUpdateNothingAtPath,
                version.ToString(3), UpdateCheckService.ReleasesPageUrl)));
    }

    private static ReplaceOutcome RunSteps(ReplaceRequest request, bool startAtPath, ReplaceSeams seams)
    {
        var exePath = request.ExePath;
        var replacing = exePath + ReplacingSuffix;

        using var oldCopy = OpenCopy(request, out var refused);
        if (oldCopy is null)
        {
            CrashLog.TryWrite(refused!);
            return ReplaceOutcome.NotHandedOver;
        }

        using var held = TryHold(exePath, seams.HoldRetryBound ?? HoldRetryBound, out var holdFailure);
        if (held is null)
            return Fail(ReplaceOutcome.NotHandedOver,
                $"The file ending {LockSuffix}, which a replace or a clean-up of this exe holds while it runs, could not be held.",
                holdFailure);

        var state = new ReplaceState();
        try
        {
            if (!TryWriteNew(replacing, seams.ThisExe ?? Environment.ProcessPath, out var writeFailure))
                return Fail(ReplaceOutcome.NotHandedOver, "The newer version could not be written beside the exe.", writeFailure);
            return HandOverAndReplace(request, oldCopy, startAtPath, seams, state);
        }
        catch (Exception ex)
        {
            // Inside the hold, so what is taken back here is this replace's own.
            CrashLog.TryWrite(ex);
            return AfterAThrow(exePath, state);
        }
    }

    /// <summary>
    /// Opens the process <paramref name="request"/> names, for waiting on it and reading its
    /// image, and answers its handle where the file at the request's path is a plain file
    /// that process runs from. Otherwise answers null, with what was found in
    /// <paramref name="refused"/> for crash.log, and nothing opened is left open.
    /// </summary>
    internal static SafeProcessHandle? OpenCopy(ReplaceRequest request, out UpdateFailedException? refused)
    {
        var exePath = request.ExePath;
        var process = Kernel32.OpenProcess(
            Ntdll.SYNCHRONIZE | Kernel32.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, (uint)request.ProcessId);
        if (process.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            process.Dispose();
            refused = Refusal($"The copy to be replaced, process {request.ProcessId}, could not be opened: error {error}.");
            return null;
        }
        try
        {
            refused = !TryReadAttributes(exePath, out var attributes, out var readFailure)
                ? Refusal("The file to be replaced could not be read.", readFailure)
                // A link at the path would pass the check below as the file it leads to, and the
                // move replaces a link rather than following it, so the file it leads to would keep
                // the older version.
                : (attributes & FileAttributes.ReparsePoint) != 0
                    ? Refusal("The file to be replaced is a link.")
                    : !IsImageOf(process, exePath)
                        ? Refusal($"The file to be replaced is not the one process {request.ProcessId} runs from.")
                        : null;
        }
        catch
        {
            process.Dispose();
            throw;
        }
        if (refused is null) return process;
        process.Dispose();
        return null;

        static UpdateFailedException Refusal(string message, Exception? inner = null) =>
            new(UpdateFailureStage.Install, message, inner);
    }

    /// <summary>
    /// Takes back what a replace that threw had done, by how far it had gone, and answers
    /// what that leaves: before the started event, <see cref="ReplaceOutcome.NotHandedOver"/>;
    /// before the copy was seen to end, <see cref="ReplaceOutcome.OldCopyNotSeenToEnd"/>;
    /// before the move, what <see cref="TakeBackBeforeTheMove"/> finds; after it, or where
    /// taking back throws too, <see cref="ReplaceOutcome.Failed"/>. Never throws.
    /// </summary>
    private static ReplaceOutcome AfterAThrow(string exePath, ReplaceState state)
    {
        try
        {
            if (!state.Signalled || !state.OldCopyEnded)
            {
                DeleteOrSayWhy(exePath + ReplacingSuffix);
                return state.Signalled ? ReplaceOutcome.OldCopyNotSeenToEnd : ReplaceOutcome.NotHandedOver;
            }
            if (!state.Moved)
                return TakeBackBeforeTheMove(exePath, state.ReadOnlyTakenOff, out var gone)
                    ? Fail(ReplaceOutcome.NothingAtPath, NothingAtPathMessage, gone)
                    : ReplaceOutcome.Refused;
            if (state.ReadOnlyTakenOff) PutReadOnlyBack(exePath);
            return ReplaceOutcome.Failed;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return ReplaceOutcome.Failed;
        }
    }

    /// <summary>How far a replace has gone, for what is taken back where a step throws.</summary>
    private sealed class ReplaceState
    {
        /// <summary>The started event has been set, so the copy is closing.</summary>
        public bool Signalled;

        /// <summary>The copy has been seen to end.</summary>
        public bool OldCopyEnded;

        /// <summary>The exe's read-only attribute has been taken off for the replace.</summary>
        public bool ReadOnlyTakenOff;

        /// <summary>The newer version has been moved over the exe.</summary>
        public bool Moved;
    }

    /// <summary>
    /// The steps of <see cref="Run(ReplaceRequest, bool, ReplaceSeams)"/> once the newer
    /// version is beside the exe: the started event, the wait for <paramref name="oldCopy"/>,
    /// the second name, the move, and where <paramref name="startAtPath"/>, the start and the
    /// putting back, with how far it has gone in <paramref name="state"/>.
    /// </summary>
    private static ReplaceOutcome HandOverAndReplace(
        ReplaceRequest request, SafeProcessHandle oldCopy, bool startAtPath, ReplaceSeams seams, ReplaceState state)
    {
        var exePath = request.ExePath;
        var replacing = exePath + ReplacingSuffix;
        var backup = exePath + BackupSuffix;

        if (!HandOverContract.SignalStarted(request.StartedEvent))
        {
            DeleteOrSayWhy(replacing);
            return ReplaceOutcome.NotHandedOver;
        }
        state.Signalled = true;

        // The copy has been told to close and closes whatever this process does next, so the
        // wait has no bound: a copy that takes long to close is still replaced once it has.
        if (!(seams.WaitForEnd ?? WaitForEnd)(oldCopy, seams.OldCopyNotice ?? OldCopyNotice, out var waitError))
        {
            DeleteOrSayWhy(replacing);
            return Fail(ReplaceOutcome.OldCopyNotSeenToEnd,
                $"Waiting for the copy to be replaced to end failed with error {waitError}; nothing was replaced.");
        }
        state.OldCopyEnded = true;

        if (!TryReadAttributes(exePath, out var attributes, out var readFailure))
            return NotMoved("The exe could not be read once its copy had ended.", readFailure, aboutTheExe: true);
        // A move over a read-only file is refused, so the attribute comes off the exe for the
        // replace. It goes back on whichever version is at the path straight after each move,
        // so the path is read-only again before anything is started from it.
        var readOnly = (attributes & FileAttributes.ReadOnly) != 0;
        if (readOnly)
        {
            if (!TrySetReadOnly(exePath, on: false, out var attributeFailure))
                return NotMoved("The exe's read-only attribute could not be taken off.", attributeFailure, aboutTheExe: true);
            state.ReadOnlyTakenOff = true;
        }

        var kept = TryMakeBackup(exePath, backup, seams.HardLink ?? HardLink, out var linkError, out var backupFailure);
        var linkNote = linkError == 0 ? string.Empty : $" The volume refused the exe a second name with error {linkError}.";
        if (!kept)
            return NotMoved("The exe could not be kept before it was replaced." + linkNote, backupFailure, aboutTheExe: false);

        var retryBound = seams.MoveRetryBound ?? MoveRetryBound;
        if (!TryMoveOver(replacing, exePath, retryBound, out var moveError))
            return NotMoved(
                $"The newer version could not be moved over the exe; the last try failed with error {moveError}." + linkNote,
                null, aboutTheExe: false);
        state.Moved = true;
        if (readOnly) PutReadOnlyBack(exePath);

        if (!startAtPath)
        {
            TryDeletePlainFile(backup);
            return ReplaceOutcome.Replaced;
        }

        switch ((seams.StartAndAwait ?? StartAndAwait)(exePath))
        {
            case NewerVersionAnswer.Started:
                TryDeletePlainFile(backup);
                return ReplaceOutcome.Replaced;
            case NewerVersionAnswer.StillRunning:
                return Fail(ReplaceOutcome.NewerLeftRunning,
                    "The newer version at the exe's path did not say it had started and could not be ended, so it was left "
                    + $"there, still running, and the older version was left beside it under the name ending {BackupSuffix}."
                    + linkNote);
        }

        const string notPutBack = "The newer version at the exe's path did not start, and the older version could not be put back; ";
        string leftAsItIs = "The newer version is at the path, not running, and the older version is beside it under the name "
            + $"ending {BackupSuffix}." + linkNote;
        if (readOnly && !TrySetReadOnly(exePath, on: false, out var newerAttributeFailure))
            return Fail(ReplaceOutcome.NotRolledBack,
                notPutBack + "the newer version's read-only attribute could not be taken off. " + leftAsItIs, newerAttributeFailure);
        if (TryMoveOver(backup, exePath, retryBound, out var backError))
        {
            if (readOnly) PutReadOnlyBack(exePath);
            return Fail(ReplaceOutcome.RolledBack,
                "The newer version at the exe's path did not start, so the older version was put back there." + linkNote);
        }
        if (readOnly) PutReadOnlyBack(exePath);
        return Fail(ReplaceOutcome.NotRolledBack, notPutBack + $"the last try failed with error {backError}. " + leftAsItIs);

        // A stop once the copy has ended and before the move. The exe is where it was, or
        // Windows says nothing is at the path. Which of the two is read from the exe itself,
        // the failure in hand being able to name another file, the second name among them.
        // Where aboutTheExe and the failure in hand already says the exe is not there, it is all
        // crash.log needs; otherwise it goes beside the answer for the exe.
        ReplaceOutcome NotMoved(string message, Exception? failure, bool aboutTheExe)
        {
            if (!TakeBackBeforeTheMove(exePath, state.ReadOnlyTakenOff, out var gone))
                return Fail(ReplaceOutcome.Refused, message, failure);
            var inner = failure is null ? gone
                : aboutTheExe && IsNothingThere(failure) ? failure
                : new AggregateException(gone!, failure);
            return Fail(ReplaceOutcome.NothingAtPath, $"{NothingAtPathMessage} The step that stopped the replace: {message}", inner);
        }
    }

    /// <summary>
    /// Deletes the newer version waiting beside <paramref name="exePath"/> and the exe's
    /// second name, and answers whether Windows says nothing is at the path, with its answer
    /// in <paramref name="gone"/>. Where something is, and <paramref name="readOnly"/>, puts
    /// the read-only attribute back on the exe.
    /// </summary>
    private static bool TakeBackBeforeTheMove(string exePath, bool readOnly, out Exception? gone)
    {
        DeleteOrSayWhy(exePath + ReplacingSuffix);
        DeleteOrSayWhy(exePath + BackupSuffix);
        if (IsNothingAt(exePath, out gone)) return true;
        gone = null;
        if (readOnly) PutReadOnlyBack(exePath);
        return false;
    }

    /// <summary>What crash.log says where nothing is at the exe's path once its copy has ended.</summary>
    private const string NothingAtPathMessage =
        "Nothing was at the exe's path once its copy had ended, so the newer version was not put there.";

    /// <summary>
    /// Whether <paramref name="failure"/> is Windows answering that a file, its folder or its
    /// drive is not there: errors 2, 3 and 15.
    /// </summary>
    private static bool IsNothingThere(Exception? failure) =>
        failure is FileNotFoundException or DirectoryNotFoundException or DriveNotFoundException;

    /// <summary>
    /// Whether Windows answers that nothing is at <paramref name="exePath"/>
    /// (<see cref="IsNothingThere"/>), with its answer in <paramref name="failure"/>.
    /// </summary>
    private static bool IsNothingAt(string exePath, out Exception? failure) =>
        !TryReadAttributes(exePath, out _, out failure) && IsNothingThere(failure);

    /// <summary>
    /// Holds the file named for <paramref name="exePath"/> with <see cref="LockSuffix"/> on
    /// the end (<see cref="StorageHelpers.CreateHeld"/>), and answers its handle; null where
    /// it cannot, with the last failure in <paramref name="failure"/>. A plain file at the
    /// name is one Windows had no chance to delete, as where the PC lost power, and is deleted
    /// first; one somebody holds cannot be. Each failure is tried again, a little later each
    /// time, for up to <paramref name="bound"/>, except a folder or a reparse point at the
    /// name, which is refused at once.
    /// </summary>
    private static SafeFileHandle? TryHold(string exePath, TimeSpan bound, out Exception? failure)
    {
        var path = exePath + LockSuffix;
        SafeFileHandle? held = null;
        Exception? last = null;
        var taken = Retry(bound, () =>
        {
            try
            {
                // A folder or a link at the name is refused at once; a plain file is deleted.
                if (TryReadAttributes(path, out var found, out _))
                {
                    if ((found & FileAttributes.ReparsePoint) != 0) throw new NotAPlainFileException(path, "a reparse point");
                    if ((found & FileAttributes.Directory) != 0) throw new NotAPlainFileException(path, "a folder");
                    DeletePlainFile(path);
                }
                held = StorageHelpers.CreateHeld(path, out var error);
                if (held is not null) return RetryStep.Done;
                if (error == 0) throw new NotAPlainFileException(path, "a reparse point");
                last = new IOException($"The file ending {LockSuffix} could not be created: error {error}.");
                return RetryStep.Again;
            }
            catch (NotAPlainFileException ex)
            {
                last = ex;
                return RetryStep.Stop;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                last = ex;
                return RetryStep.Again;
            }
        });
        failure = taken ? null : last;
        return taken ? held : null;
    }

    /// <summary>What one try of <see cref="Retry"/> answered.</summary>
    private enum RetryStep
    {
        /// <summary>It succeeded.</summary>
        Done,

        /// <summary>It failed, and may be tried again.</summary>
        Again,

        /// <summary>It failed, and trying again cannot change that.</summary>
        Stop,
    }

    /// <summary>
    /// Calls <paramref name="attempt"/> until it answers <see cref="RetryStep.Done"/> or
    /// <see cref="RetryStep.Stop"/>, or until <paramref name="bound"/> has passed since the
    /// first call, waiting a little longer between calls each time. Answers whether it
    /// answered <see cref="RetryStep.Done"/>.
    /// </summary>
    private static bool Retry(TimeSpan bound, Func<RetryStep> attempt)
    {
        var started = Stopwatch.GetTimestamp();
        var interval = FirstRetryInterval;
        while (true)
        {
            var step = attempt();
            if (step != RetryStep.Again) return step == RetryStep.Done;
            var left = bound - Durations.Since(started);
            if (left <= TimeSpan.Zero) return false;
            Thread.Sleep(interval < left ? interval : left);
            var doubled = Durations.Times(interval, 2);
            interval = doubled < LongestRetryInterval ? doubled : LongestRetryInterval;
        }
    }

    /// <summary>Reads <paramref name="path"/>'s attributes, and answers whether it could, with what failed in <paramref name="failure"/>.</summary>
    private static bool TryReadAttributes(string path, out FileAttributes attributes, out Exception? failure)
    {
        failure = null;
        try
        {
            attributes = File.GetAttributes(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            attributes = default;
            failure = ex;
            return false;
        }
    }

    /// <summary>Sets <paramref name="path"/>'s attributes, and answers whether it did, with what failed in <paramref name="failure"/>.</summary>
    private static bool TrySetAttributes(string path, FileAttributes attributes, out Exception? failure)
    {
        failure = null;
        try
        {
            File.SetAttributes(path, attributes);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failure = ex;
            return false;
        }
    }

    /// <summary>
    /// Puts the read-only attribute on <paramref name="path"/> where <paramref name="on"/>,
    /// and takes it off otherwise, and answers whether it did, with what failed in
    /// <paramref name="failure"/>.
    /// </summary>
    private static bool TrySetReadOnly(string path, bool on, out Exception? failure) =>
        TryReadAttributes(path, out var attributes, out failure)
        && TrySetAttributes(
            path, on ? attributes | FileAttributes.ReadOnly : attributes & ~FileAttributes.ReadOnly, out failure);

    /// <summary>
    /// Puts the read-only attribute on whatever is at <paramref name="exePath"/>. A failure
    /// is written to crash.log.
    /// </summary>
    private static void PutReadOnlyBack(string exePath)
    {
        if (!TrySetReadOnly(exePath, on: true, out var failure))
            CrashLog.TryWrite(new UpdateFailedException(UpdateFailureStage.Install,
                "The read-only attribute could not be put back on the exe.", failure));
    }

    /// <summary>Writes <paramref name="message"/> to crash.log, with <paramref name="inner"/> inside it, and answers <paramref name="outcome"/>.</summary>
    private static ReplaceOutcome Fail(ReplaceOutcome outcome, string message, Exception? inner = null)
    {
        CrashLog.TryWrite(new UpdateFailedException(UpdateFailureStage.Install, message, inner));
        return outcome;
    }

    /// <summary>
    /// Whether <paramref name="exePath"/> is the file the process <paramref name="process"/>
    /// runs from: the two files' identities, where both can be read, and otherwise the two
    /// full paths, compared ignoring case.
    /// </summary>
    private static bool IsImageOf(SafeProcessHandle process, string exePath)
    {
        var buffer = new char[MaxImagePathLength];
        var length = (uint)buffer.Length;
        if (!Kernel32.QueryFullProcessImageName(process, 0, buffer, ref length)) return false;
        var image = new string(buffer, 0, (int)length);

        var reader = new FileIdentityReader();
        if (reader.ReadOutcome(image, out var imageIdentity) == FileIdentityRead.Read
            && reader.ReadOutcome(exePath, out var exeIdentity) == FileIdentityRead.Read)
            return imageIdentity == exeIdentity;
        return string.Equals(Path.GetFullPath(image), Path.GetFullPath(exePath), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Copies <paramref name="source"/> to <paramref name="path"/>, a file created new
    /// through <see cref="StorageHelpers.OpenAtomic"/> in the exe's folder, so it takes
    /// that folder's inherited permissions, and flushed to the disk. A plain file left at
    /// that name is deleted first; a reparse point there is refused and never followed.
    /// Answers whether it was written, with what failed in <paramref name="failure"/>,
    /// having left nothing of its own at the name.
    /// </summary>
    private static bool TryWriteNew(string path, string? source, out Exception? failure)
    {
        failure = null;
        var created = false;
        try
        {
            if (source is null) throw new IOException("This process has no path to copy.");
            if (File.Exists(path)) DeletePlainFile(path);
            using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var handle = StorageHelpers.OpenAtomic(path, FileAccess.Write, StorageHelpers.AtomicOpenMode.CreateNew)
                ?? throw new IOException($"The file ending {Path.GetExtension(path)} could not be created.");
            created = true;
            using var output = new FileStream(handle, FileAccess.Write, bufferSize: 0);
            input.CopyTo(output);
            output.Flush(flushToDisk: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (created) TryDeletePlainFile(path);
            failure = ex;
            return false;
        }
    }

    /// <summary>
    /// Gives the exe at <paramref name="exePath"/> the second name <paramref name="backup"/>
    /// through <paramref name="hardLink"/>, or where the volume will not link it, a copy
    /// written the way <see cref="TryWriteNew"/> writes one, with the link's error code in
    /// <paramref name="linkError"/> (0 where the link was made). A plain file left at that
    /// name is deleted first.
    /// </summary>
    private static bool TryMakeBackup(
        string exePath, string backup, Func<string, string, int> hardLink, out int linkError, out Exception? failure)
    {
        failure = null;
        linkError = 0;
        try
        {
            if (File.Exists(backup)) DeletePlainFile(backup);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failure = ex;
            return false;
        }
        linkError = hardLink(backup, exePath);
        return linkError == 0 || TryWriteNew(backup, exePath, out failure);
    }

    /// <summary>Gives <paramref name="existing"/> the second name <paramref name="link"/>, and answers 0, or the error Windows refused it with.</summary>
    private static int HardLink(string link, string existing) =>
        Kernel32.CreateHardLink(link, existing, IntPtr.Zero) ? 0 : Marshal.GetLastWin32Error();

    /// <summary>
    /// Moves <paramref name="from"/> over <paramref name="to"/> in one step, written through
    /// to the disk, and tries again for up to <paramref name="bound"/> while the move is
    /// refused with a sharing violation, a lock violation or access denied, waiting a little
    /// longer each time. Answers whether it moved, with the last refusal's error code in
    /// <paramref name="error"/>.
    /// </summary>
    private static bool TryMoveOver(string from, string to, TimeSpan bound, out int error)
    {
        var last = 0;
        var moved = Retry(bound, () =>
        {
            if (Kernel32.MoveFileEx(from, to, Kernel32.MOVEFILE_REPLACE_EXISTING | Kernel32.MOVEFILE_WRITE_THROUGH))
                return RetryStep.Done;
            last = Marshal.GetLastWin32Error();
            return last is Kernel32.ERROR_ACCESS_DENIED or Kernel32.ERROR_SHARING_VIOLATION or Kernel32.ERROR_LOCK_VIOLATION
                ? RetryStep.Again
                : RetryStep.Stop;
        });
        error = moved ? 0 : last;
        return moved;
    }

    /// <summary>
    /// Waits for <paramref name="process"/> to end, for as long as it takes, writing to
    /// crash.log once <paramref name="notice"/> has passed that it is still waiting. Answers
    /// true once it has ended, and false where Windows refused the wait, with its error code
    /// in <paramref name="error"/>.
    /// </summary>
    internal static bool WaitForEnd(SafeProcessHandle process, TimeSpan notice, out int error)
    {
        var milliseconds = (uint)Math.Clamp(notice.TotalMilliseconds, 0, uint.MaxValue - 1);
        var result = HandleWait.One(process, milliseconds, out error);
        if (result == Kernel32.WAIT_TIMEOUT)
        {
            CrashLog.TryWrite(new TimeoutException(
                $"The copy to be replaced had not closed {notice.TotalSeconds:0} seconds after it was told to; the replace waits on for it."));
            result = HandleWait.One(process, Kernel32.INFINITE, out error);
        }
        return result == Kernel32.WAIT_OBJECT_0;
    }

    /// <summary>
    /// Starts the exe at <paramref name="exePath"/> with a started event and waits up to
    /// <see cref="HandOverContract.StartBound"/> for it to say its own code is running. One
    /// that does not is ended. Answers <see cref="NewerVersionAnswer.Started"/>, or
    /// <see cref="NewerVersionAnswer.NotStarted"/> where it could not be started, did not say so
    /// and has been ended, or no event could be made to see it by, or
    /// <see cref="NewerVersionAnswer.StillRunning"/> where it did not say so and Windows would not
    /// end it. What failed is written to crash.log.
    /// </summary>
    private static NewerVersionAnswer StartAndAwait(string exePath) =>
        NewerVersionProcess.StartAndAwait(
            exePath, id => [HandOverContract.StartedEventArgument(id)], HandOverContract.StartBound,
            interval => Thread.Sleep(interval));

    /// <summary>
    /// Starts the exe at <paramref name="exePath"/>, the older version put back or left in
    /// place, with <see cref="HandOverContract.UpdateFailedSwitch"/>, so it opens saying the
    /// update couldn't be installed. A start that fails is written to crash.log. Never throws.
    /// </summary>
    public static void StartWithUpdateFailed(string exePath)
    {
        try
        {
            var info = new ProcessStartInfo(exePath)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty,
            };
            info.ArgumentList.Add(HandOverContract.UpdateFailedSwitch);
            ProcessStartRetry.Start(info, _ => true, interval => Thread.Sleep(interval)).Dispose();
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
        }
    }

    /// <summary>
    /// Deletes what a replace left beside <paramref name="exePath"/>: the newer version that
    /// waited there and the older version's second name, each only where it is a plain file,
    /// and only while a file is at the path; and each copy of the exe the setup renamed to
    /// a done name, as not needed (<see cref="SetupDoneSuffix"/>). Where any of those or
    /// the file a replace holds (<see cref="LockSuffix"/>) is beside the exe, it holds that file while it deletes, so
    /// it runs only where no replace of the exe is running, and no replace starts until it
    /// is done. It tries once, so a caller that is to ride out a replace's hold tries
    /// again. Answers whether nothing is left. Never throws.
    /// </summary>
    public static bool TryRemoveLeftovers(string exePath) => TryRemoveLeftovers(exePath, whileHeld: null);

    /// <summary>
    /// Test seam: <see cref="TryRemoveLeftovers(string)"/>, running <paramref name="whileHeld"/>
    /// while the file is held and before anything is deleted.
    /// </summary>
    internal static bool TryRemoveLeftovers(string exePath, Action? whileHeld)
    {
        try
        {
            if (!File.Exists(exePath)) return false;
            var replacing = exePath + ReplacingSuffix;
            var backup = exePath + BackupSuffix;
            // A folder that cannot be listed answers that something may be left, with
            // nothing written to crash.log, since the window asks again many times at
            // every start.
            var setupDone = SetupDoneCopies(exePath);
            var replaceLeft = File.Exists(replacing) || File.Exists(backup) || File.Exists(exePath + LockSuffix);
            if (!replaceLeft && setupDone is not { Count: > 0 }) return setupDone is not null;
            using var held = TryHold(exePath, TimeSpan.Zero, out _);
            if (held is null) return false;
            whileHeld?.Invoke();
            // The second name can be a link of the exe, one file with its attributes. A system
            // that cannot delete it while leaving the read-only attribute alone clears that
            // attribute for the delete, so it is put back on the exe here.
            var readOnly = TryReadAttributes(exePath, out var attributes, out _) && (attributes & FileAttributes.ReadOnly) != 0;
            var removed = TryDeletePlainFile(replacing) & TryDeletePlainFile(backup) & setupDone is not null;
            foreach (var setupCopy in setupDone ?? []) removed &= TryDeletePlainFile(setupCopy);
            if (readOnly && TryReadAttributes(exePath, out attributes, out _) && (attributes & FileAttributes.ReadOnly) == 0)
                PutReadOnlyBack(exePath);
            return removed;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return false;
        }
    }

    /// <summary>
    /// The names beside <paramref name="exePath"/> under which the setup left a copy of the
    /// exe that is not needed: the exe's name with <see cref="SetupDoneSuffix"/> on the end, alone or
    /// followed by a hyphen and a number. Answers null where the folder cannot be listed.
    /// </summary>
    internal static List<string>? SetupDoneCopies(string exePath)
    {
        var folder = Path.GetDirectoryName(exePath)!;
        var stem = Path.GetFileName(exePath) + SetupDoneSuffix;
        var found = new List<string>();
        try
        {
            foreach (var path in Directory.EnumerateFiles(folder, stem + "*"))
            {
                var rest = Path.GetFileName(path)[stem.Length..];
                if (rest.Length == 0 || (rest.Length > 1 && rest[0] == '-' && rest[1..].All(char.IsAsciiDigit)))
                    found.Add(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
        return found;
    }

    /// <summary>Deletes <paramref name="path"/> where it is a plain file, and answers whether nothing is left there.</summary>
    private static bool TryDeletePlainFile(string path) => TryDelete(path, out _);

    /// <summary>
    /// Deletes <paramref name="path"/> where it is a plain file, and where something is left
    /// there, writes to crash.log that it is left and the error Windows gave.
    /// </summary>
    private static void DeleteOrSayWhy(string path)
    {
        if (!TryDelete(path, out var error))
            CrashLog.TryWrite(new IOException(
                $"The name ending {Path.GetExtension(path)} beside the exe could not be deleted, with error {error}, and is left there."));
    }

    /// <summary>
    /// Deletes <paramref name="path"/> where it is a plain file, through
    /// <see cref="DirectoryGuard.TryDiscardChildCopy"/>: a link is never followed, and a
    /// read-only file goes without its attributes being changed where the system allows it,
    /// so the exe's second name, where it is a link, goes without touching the exe. Answers
    /// whether nothing is there now, its folder included, with the error Windows gave in
    /// <paramref name="error"/> otherwise.
    /// </summary>
    private static bool TryDelete(string path, out int error)
    {
        if (DirectoryGuard.TryDiscardChildCopy(Path.GetDirectoryName(path)!, Path.GetFileName(path), out error))
        {
            error = 0;
            return true;
        }
        return error is Kernel32.ERROR_FILE_NOT_FOUND or Kernel32.ERROR_PATH_NOT_FOUND;
    }

    /// <summary>
    /// Deletes <paramref name="path"/> where it is a plain file, as <see cref="TryDelete"/>
    /// does, and throws an <see cref="IOException"/> where it is a folder or a reparse point,
    /// which this never follows, or where Windows refuses the delete, naming its error.
    /// </summary>
    private static void DeletePlainFile(string path)
    {
        if (TryDelete(path, out var error)) return;
        throw error switch
        {
            Kernel32.ERROR_REPARSE_POINT_ENCOUNTERED => new NotAPlainFileException(path, "a reparse point"),
            Kernel32.ERROR_DIRECTORY_NOT_SUPPORTED => new NotAPlainFileException(path, "a folder"),
            _ => new IOException($"The file ending {Path.GetExtension(path)} could not be deleted: error {error}."),
        };
    }

    /// <summary>
    /// Something other than a plain file at a name a replace writes, holds or deletes: a
    /// folder, or a reparse point, which it never follows.
    /// </summary>
    private sealed class NotAPlainFileException(string path, string found)
        : IOException($"The name ending {Path.GetExtension(path)} is {found}, not a plain file.");
}

/// <summary>How a replace (<see cref="ExeReplacement.Run(ReplaceRequest, bool)"/>) ended.</summary>
internal enum ReplaceOutcome
{
    /// <summary>
    /// The started event was not set, so the copy carries on, and nothing at its path changed.
    /// </summary>
    NotHandedOver,

    /// <summary>The event was set and Windows refused the wait for the copy to end. Nothing at its path changed.</summary>
    OldCopyNotSeenToEnd,

    /// <summary>The copy has ended, and its exe is at the path still: the newer version could not be put there.</summary>
    Refused,

    /// <summary>
    /// The copy has ended and nothing is at its path. The newer version was not put there,
    /// and nothing was started.
    /// </summary>
    NothingAtPath,

    /// <summary>The newer version is at the path, and where it was started, said it had started.</summary>
    Replaced,

    /// <summary>The newer version at the path did not start, and the older version is back there.</summary>
    RolledBack,

    /// <summary>
    /// The newer version at the path did not say it had started and is still running; the
    /// older version is beside it under its second name.
    /// </summary>
    NewerLeftRunning,

    /// <summary>
    /// The newer version at the path did not start, and the older version, beside it under
    /// its second name, could not be put back.
    /// </summary>
    NotRolledBack,

    /// <summary>
    /// Something the steps did not expect threw where what is at the path is not
    /// established: once the newer version had been moved over the exe, or while what
    /// the replace had done was being taken back. It is in crash.log.
    /// </summary>
    Failed,
}

/// <summary>
/// Test seams for <see cref="ExeReplacement.Run(ReplaceRequest, bool, ReplaceSeams)"/>. Each null
/// member is the real part.
/// </summary>
/// <param name="ThisExe">The file copied as the newer version, in place of this process's own.</param>
/// <param name="OldCopyNotice">In place of <see cref="ExeReplacement.OldCopyNotice"/>.</param>
/// <param name="MoveRetryBound">In place of <see cref="ExeReplacement.MoveRetryBound"/>.</param>
/// <param name="HoldRetryBound">In place of <see cref="ExeReplacement.HoldRetryBound"/>.</param>
/// <param name="HardLink">Makes the second name, answering 0, or the error it was refused with.</param>
/// <param name="StartAndAwait">Starts the exe at the path and answers what it did.</param>
/// <param name="WaitForEnd">
/// Waits for the copy to end, as the real wait does: true once it has, false with the
/// error where the wait was refused.
/// </param>
internal sealed record ReplaceSeams(
    string? ThisExe = null,
    TimeSpan? OldCopyNotice = null,
    TimeSpan? MoveRetryBound = null,
    TimeSpan? HoldRetryBound = null,
    Func<string, string, int>? HardLink = null,
    Func<string, NewerVersionAnswer>? StartAndAwait = null,
    WaitForEndSeam? WaitForEnd = null);

/// <summary>The shape of <see cref="ReplaceSeams.WaitForEnd"/>.</summary>
internal delegate bool WaitForEndSeam(SafeProcessHandle process, TimeSpan notice, out int error);
