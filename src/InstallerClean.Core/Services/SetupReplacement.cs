using System.Diagnostics;
using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Services;

/// <summary>
/// Puts this version of the command line in place of the setup's copy once that copy has
/// ended, by running the newer setup the older copy downloaded beside this exe
/// (<see cref="HandOverContract.ReadReplaceRequest"/>). The setup writes the window and the
/// command line together, with neither running, and opens nothing afterwards.
/// </summary>
/// <remarks>
/// The setup is held open from before the started event is set until it has been started,
/// shared for reading only, so nothing can delete, rename or change it between the check of
/// its version and its start: what is checked is the file that runs.
/// </remarks>
internal static class SetupReplacement
{
    /// <summary>
    /// How long the setup waits for InstallerClean to close once the copy it replaces has
    /// ended: the single-instance mutex let go, and nothing running from the setup's folder.
    /// The setup itself stops where a process has the mutex open, so it is started only
    /// once nothing does. Where InstallerClean is still in use after this, nothing is run,
    /// and the next run that finds the newer version tries again.
    /// </summary>
    internal static readonly TimeSpan InUseBound = TimeSpan.FromSeconds(60);

    /// <summary>How long a look at the copy at the path waits out a program holding it, once the setup has written it.</summary>
    internal static readonly TimeSpan ReadHoldWait = TimeSpan.FromSeconds(15);

    /// <summary>The window's file name in the setup's folder, as installer/InstallerClean.iss installs it.</summary>
    internal const string WindowFileName = "InstallerClean.exe";

    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Carries out <paramref name="request"/> on the setup's copy and answers how it ended,
    /// with the setup's exit code in <paramref name="exitCode"/> where it ran and ended and the
    /// code was read, and null otherwise. Every failure is written to crash.log. Never throws.
    /// </summary>
    public static SetupReplaceOutcome Run(ReplaceRequest request, out int? exitCode) =>
        Run(request, new SetupReplaceSeams(), out exitCode);

    /// <summary>Test seam: <see cref="Run(ReplaceRequest, out int?)"/>, with <paramref name="seams"/> in place of the real parts.</summary>
    internal static SetupReplaceOutcome Run(ReplaceRequest request, SetupReplaceSeams seams, out int? exitCode)
    {
        exitCode = null;
        var setupStarted = false;
        try
        {
            return RunSteps(request, seams, ref setupStarted, out exitCode);
        }
        catch (Exception ex)
        {
            // Before the setup is started nothing at the path has changed; after it, what the
            // setup did is not known.
            CrashLog.TryWrite(ex);
            return setupStarted ? SetupReplaceOutcome.SetupFailed : SetupReplaceOutcome.NotStarted;
        }
    }

    private static SetupReplaceOutcome RunSteps(
        ReplaceRequest request, SetupReplaceSeams seams, ref bool setupStarted, out int? exitCode)
    {
        exitCode = null;
        var version = seams.Version ?? ThisVersion();
        var thisExe = seams.ThisExe ?? Environment.ProcessPath
            ?? throw new InvalidOperationException("This process has no path, so the setup beside it cannot be found.");
        var setupPath = Path.Combine(Path.GetDirectoryName(thisExe)!, AppCopy.DownloadFileName(AppCopyKind.Setup, version)!);
        var copyFolder = Path.GetDirectoryName(request.ExePath)!;

        using var oldCopy = (seams.OpenCopy ?? ExeReplacement.OpenCopy)(request, out var refused);
        if (oldCopy is null) return Fail(SetupReplaceOutcome.NotHandedOver, refused!);

        FileStream? setup;
        try
        {
            setup = new FileStream(setupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Fail(SetupReplaceOutcome.NotStarted, "The setup beside this exe could not be opened.", ex);
        }
        try
        {
            if (FileVersionResource.Read(setup) is not { } carried || !FileVersionResource.IsRelease(carried, version))
                return Fail(SetupReplaceOutcome.NotThisVersion,
                    $"The setup beside this exe does not carry version {version}, so it was not run.");

            if (!HandOverContract.SignalStarted(request.StartedEvent)) return SetupReplaceOutcome.NotHandedOver;

            // The copy has been told it is being replaced and ends whatever this process does
            // next, so the wait has no bound, as the replace's own has none.
            if (!(seams.WaitForEnd ?? ExeReplacement.WaitForEnd)(oldCopy, ExeReplacement.OldCopyNotice, out var waitError))
                return Fail(SetupReplaceOutcome.OldCopyNotSeenToEnd,
                    $"Waiting for the copy to be replaced to end failed with error {waitError}; the setup was not run.");

            if (!(seams.WaitUntilFree ?? WaitUntilFree)(copyFolder, seams.InUseBound ?? InUseBound))
                return Fail(SetupReplaceOutcome.InUse,
                    $"InstallerClean was still in use {(seams.InUseBound ?? InUseBound).TotalSeconds:0} seconds after the copy to be replaced had ended, so the setup was not run.");

            using var started = (seams.Start ?? DetachedProcess.Start)(
                setupPath, CommandLineUpdate.SetupArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                Path.GetDirectoryName(setupPath)!);
            // The setup has its own handle on its file once it runs, so this one goes.
            setup.Dispose();
            setup = null;
            if (started is null) return Fail(SetupReplaceOutcome.NotStarted, "The setup could not be started.");
            setupStarted = true;

            if (!(seams.WaitForExit ?? WaitForExit)(started, out var ended, out var exitError))
                return Fail(SetupReplaceOutcome.SetupFailed, $"Waiting for the setup to end failed with error {exitError}.");
            exitCode = ended;
            if (ended != 0)
                return Fail(SetupReplaceOutcome.SetupFailed, $"The setup ended with exit code {ended}.");

            if (!(seams.CarriesVersion ?? CarriesVersion)(request.ExePath, version, out var unread))
                return Fail(SetupReplaceOutcome.NotInPlace,
                    $"The setup finished without putting version {version} of the command line in place.", unread);
            return SetupReplaceOutcome.Installed;
        }
        finally
        {
            setup?.Dispose();
        }
    }

    /// <summary>This build's version, as the update check writes one: major, minor and build.</summary>
    internal static string ThisVersion() =>
        (typeof(SetupReplacement).Assembly.GetName().Version ?? new Version(0, 0, 0)).ToString(3);

    private static SetupReplaceOutcome Fail(SetupReplaceOutcome outcome, Exception failure)
    {
        CrashLog.TryWrite(failure);
        return outcome;
    }

    private static SetupReplaceOutcome Fail(SetupReplaceOutcome outcome, string message, Exception? inner = null) =>
        Fail(outcome, new UpdateFailedException(UpdateFailureStage.Install, message, inner));

    /// <summary>
    /// Waits up to <paramref name="bound"/> for InstallerClean to be closed: the
    /// single-instance mutex let go, and no process running from the window or the command
    /// line in <paramref name="folder"/> (<see cref="AnotherProcessMayRunFrom"/>).
    /// The mutex is taken for the look and let go again, its handle closed, before this
    /// answers, since the setup stops while any process has it open. Answers whether
    /// InstallerClean was found closed.
    /// </summary>
    private static bool WaitUntilFree(string folder, TimeSpan bound)
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        while (true)
        {
            var left = bound - Durations.Since(started);
            bool busy;
            using (var instance = new InstanceLock())
            {
                if (!instance.TryTake(left > TimeSpan.Zero ? left : TimeSpan.Zero)) return false;
                try
                {
                    busy = AnotherProcessMayRunFrom(Path.Combine(folder, AppCopy.CommandLineFileName))
                        || AnotherProcessMayRunFrom(Path.Combine(folder, WindowFileName));
                }
                finally
                {
                    instance.Release();
                }
            }
            if (!busy) return true;
            left = bound - Durations.Since(started);
            if (left <= TimeSpan.Zero) return false;
            Thread.Sleep(RetryInterval < left ? RetryInterval : left);
        }
    }

    /// <summary>
    /// Waits for <paramref name="process"/> to end, for as long as it takes, and reads its
    /// exit code. Answers false where Windows refused the wait or the read, with its error in
    /// <paramref name="error"/>.
    /// </summary>
    private static bool WaitForExit(SafeProcessHandle process, out int exitCode, out int error)
    {
        exitCode = 0;
        if (HandleWait.One(process, Kernel32.INFINITE, out error) != Kernel32.WAIT_OBJECT_0) return false;
        if (!Kernel32.GetExitCodeProcess(process, out var code))
        {
            error = Marshal.GetLastWin32Error();
            return false;
        }
        exitCode = (int)code;
        return true;
    }

    /// <summary>
    /// Whether the file at <paramref name="path"/> carries <paramref name="version"/>. The
    /// setup has just written it, and an antivirus can hold a program it has just seen
    /// written while it looks at it, so a read refused with a sharing or a lock violation is
    /// tried again for up to <see cref="ReadHoldWait"/>. Any other failure, or a hold that
    /// outlasts that, answers false with the failure in <paramref name="unread"/>.
    /// </summary>
    private static bool CarriesVersion(string path, string version, out Exception? unread)
    {
        unread = null;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        while (true)
        {
            try
            {
                return FileVersionResource.ReadIfThere(path) is { } carried && FileVersionResource.IsRelease(carried, version);
            }
            catch (IOException ex) when (WindowsErrors.Of(ex) is Kernel32.ERROR_SHARING_VIOLATION or Kernel32.ERROR_LOCK_VIOLATION
                && Durations.Since(started) < ReadHoldWait)
            {
                Thread.Sleep(100);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                unread = ex;
                return false;
            }
        }
    }

    /// <summary>
    /// Whether a process other than this one may be running from <paramref name="exePath"/>.
    /// Only processes whose image has the exe's file name are asked, Windows giving no path
    /// for its own (System, Registry and the like). Each one answers no only where it has
    /// ended since the list was taken, or where its image path opens to a file known to be
    /// another: by the two files' identities, or where a volume gives none, by the two
    /// files being on volumes with different serial numbers. Anything that cannot be read
    /// answers yes. Never throws.
    /// </summary>
    /// <remarks>
    /// This finds a copy that is still starting, which holds no mutex yet, as well as one
    /// that runs without it.
    /// </remarks>
    internal static bool AnotherProcessMayRunFrom(string exePath)
    {
        Process[] named;
        try
        {
            named = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath));
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return true;
        }
        try
        {
            var self = Environment.ProcessId;
            var ours = FileOnDisk.At(exePath);
            var buffer = new char[MaxImagePathLength];
            foreach (var process in named)
            {
                if (process.Id != self && !RunsFromAnotherFile(process.Id, ours, buffer))
                    return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return true;
        }
        finally
        {
            foreach (var process in named) process.Dispose();
        }
    }

    /// <summary>
    /// Whether the process <paramref name="processId"/> has ended, or runs from a file
    /// known to be another than <paramref name="ours"/>. <paramref name="buffer"/> takes
    /// its image path.
    /// </summary>
    private static bool RunsFromAnotherFile(int processId, FileOnDisk ours, char[] buffer)
    {
        using var process = Kernel32.OpenProcess(
            Kernel32.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, (uint)processId);
        if (process.IsInvalid)
            return Marshal.GetLastWin32Error() == Kernel32.ERROR_INVALID_PARAMETER;

        var length = (uint)buffer.Length;
        if (!Kernel32.QueryFullProcessImageName(process, 0, buffer, ref length)) return false;
        return FileOnDisk.At(new string(buffer, 0, (int)length)).IsKnownToBeAnotherThan(ours);
    }

    /// <summary>The longest path Windows gives for a process's image, in characters.</summary>
    private const int MaxImagePathLength = 32768;

    /// <summary>
    /// What tells two files apart: the file's identity, where its volume gives one, and
    /// otherwise the serial number of the volume it is on, read when a comparison needs it.
    /// </summary>
    private readonly record struct FileOnDisk(string Path, FileIdentity? Identity)
    {
        public static FileOnDisk At(string path) =>
            new(path, new FileIdentityReader().ReadOutcome(path, out var identity) == FileIdentityRead.Read
                ? identity
                : null);

        /// <summary>
        /// Whether this is known to be a different file from <paramref name="other"/>: both
        /// identities read and different, or, where either identity is missing, both volume
        /// serial numbers read and different.
        /// </summary>
        public bool IsKnownToBeAnotherThan(FileOnDisk other)
        {
            if (Identity is { } mine && other.Identity is { } theirs) return mine != theirs;
            return VolumeSerialOf(Path) is { } myVolume
                && VolumeSerialOf(other.Path) is { } theirVolume
                && myVolume != theirVolume;
        }

        private static uint? VolumeSerialOf(string path)
        {
            using var handle = StorageHelpers.OpenForMetadata(path, out _);
            return !handle.IsInvalid && Kernel32.GetVolumeSerialNumberByHandle(
                    handle, IntPtr.Zero, 0, out var serial, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0)
                ? serial
                : null;
        }
    }
}

/// <summary>How a replace on the setup's copy (<see cref="SetupReplacement.Run(ReplaceRequest, out int?)"/>) ended.</summary>
internal enum SetupReplaceOutcome
{
    /// <summary>The started event was not set, so the copy carries on, and nothing changed.</summary>
    NotHandedOver,

    /// <summary>The event was set and Windows refused the wait for the copy to end. The setup was not run.</summary>
    OldCopyNotSeenToEnd,

    /// <summary>The setup beside this exe does not carry this version, so it was not run.</summary>
    NotThisVersion,

    /// <summary>InstallerClean was still in use once the wait for it had run out. The setup was not run.</summary>
    InUse,

    /// <summary>The setup could not be opened or started, so nothing ran.</summary>
    NotStarted,

    /// <summary>
    /// The setup ran and ended with an exit code other than 0, or what it ended with is not
    /// known: the wait for it was refused, or something after its start threw.
    /// </summary>
    SetupFailed,

    /// <summary>The setup ended with exit code 0, and the copy at the path does not carry this version.</summary>
    NotInPlace,

    /// <summary>The setup ended with exit code 0, and the copy at the path carries this version.</summary>
    Installed,
}

/// <summary>
/// Test seams for <see cref="SetupReplacement.Run(ReplaceRequest, SetupReplaceSeams, out int?)"/>.
/// Each null member is the real part.
/// </summary>
/// <param name="ThisExe">This exe's path, in place of the process's own, which says where the setup is.</param>
/// <param name="Version">This build's version, in place of the assembly's.</param>
/// <param name="InUseBound">In place of <see cref="SetupReplacement.InUseBound"/>.</param>
/// <param name="OpenCopy">Opens the copy to be replaced, as <see cref="ExeReplacement.OpenCopy"/> does.</param>
/// <param name="WaitForEnd">Waits for the copy to end, as <see cref="ExeReplacement.WaitForEnd"/> does.</param>
/// <param name="WaitUntilFree">Answers whether InstallerClean is closed within the bound, given the setup's folder.</param>
/// <param name="Start">Starts the setup, as <see cref="DetachedProcess.Start(string, IReadOnlyList{string}, string)"/> does.</param>
/// <param name="WaitForExit">Waits for the setup to end: true with its exit code, false with the error.</param>
/// <param name="CarriesVersion">Answers whether the copy at the path carries the version.</param>
internal sealed record SetupReplaceSeams(
    string? ThisExe = null,
    string? Version = null,
    TimeSpan? InUseBound = null,
    OpenCopySeam? OpenCopy = null,
    WaitForEndSeam? WaitForEnd = null,
    Func<string, TimeSpan, bool>? WaitUntilFree = null,
    Func<string, IReadOnlyList<string>, string, SafeProcessHandle?>? Start = null,
    WaitForExitSeam? WaitForExit = null,
    CarriesVersionSeam? CarriesVersion = null);

/// <summary>The shape of <see cref="SetupReplaceSeams.OpenCopy"/>.</summary>
internal delegate SafeProcessHandle? OpenCopySeam(ReplaceRequest request, out UpdateFailedException? refused);

/// <summary>The shape of <see cref="SetupReplaceSeams.WaitForExit"/>.</summary>
internal delegate bool WaitForExitSeam(SafeProcessHandle process, out int exitCode, out int error);

/// <summary>The shape of <see cref="SetupReplaceSeams.CarriesVersion"/>.</summary>
internal delegate bool CarriesVersionSeam(string path, string version, out Exception? unread);
