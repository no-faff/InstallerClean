using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// A newer version putting itself at an older copy's path once that copy has ended
/// (<see cref="ExeReplacement"/>), against real files and a real process standing in for
/// the older copy: a copy of Windows PowerShell's exe, run from the path being replaced and
/// sleeping until the test ends it. The newer version is a file of known bytes, given in
/// place of the test process's own exe. Starting the exe at the path afterwards is a seam,
/// answering as a newer version that started, did not, or would not end.
/// </summary>
public sealed class ExeReplacementTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("ic-replace-").FullName;
    private readonly List<Process> _started = [];
    private readonly List<IDisposable> _held = [];
    private readonly IDisposable _crashLog = TestCrashLog.OwnFolder();

    private static readonly string PowerShell =
        Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");

    private static readonly byte[] NewerBytes = Enumerable.Range(0, 64 * 1024).Select(i => (byte)(i * 7)).ToArray();

    public void Dispose()
    {
        try
        {
            foreach (var held in _held) held.Dispose();
            foreach (var process in _started)
            {
                try { if (!process.HasExited) process.Kill(); process.WaitForExit(); } catch { }
                process.Dispose();
            }
            try
            {
                foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(_root, recursive: true);
            }
            catch { }
        }
        finally
        {
            _crashLog.Dispose();
        }
    }

    private string Exe => Path.Combine(_root, "copy", "InstallerClean.exe");
    private string Replacing => Exe + ExeReplacement.ReplacingSuffix;
    private string Backup => Exe + ExeReplacement.BackupSuffix;
    private string Lock => Exe + ExeReplacement.LockSuffix;

    /// <summary>The older copy: PowerShell's exe copied to <see cref="Exe"/>, started from there and sleeping.</summary>
    private Process StartOldCopy()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Exe)!);
        File.Copy(PowerShell, Exe);
        var info = new ProcessStartInfo(Exe) { UseShellExecute = false, CreateNoWindow = true };
        info.Arguments = CommandLineArguments.Join(new[] { "-NoProfile", "-NonInteractive", "-Command", "Start-Sleep -Seconds 300" });
        var process = Process.Start(info)!;
        _started.Add(process);
        return process;
    }

    private string NewerFile()
    {
        var path = Path.Combine(_root, "newer.exe");
        File.WriteAllBytes(path, NewerBytes);
        return path;
    }

    private (ReplaceRequest Request, EventWaitHandle Started) RequestFor(Process oldCopy, string? exePath = null)
    {
        var id = Guid.NewGuid();
        var started = HandOverContract.CreateStartedEvent(id)!;
        return (new ReplaceRequest(exePath ?? Exe, oldCopy.Id, id.ToString("N"), []), started);
    }

    /// <summary>Ends <paramref name="oldCopy"/> once <paramref name="started"/> is set, as an older copy closes once the event is.</summary>
    private static Task EndOnceSet(Process oldCopy, EventWaitHandle started) => Task.Run(() =>
    {
        if (!started.WaitOne(TimeSpan.FromSeconds(60))) return;
        oldCopy.Kill();
        oldCopy.WaitForExit();
    });

    /// <summary>
    /// Denies this account the right to add a file to <paramref name="folder"/>, which Windows
    /// applies to an administrator too, until the test ends.
    /// </summary>
    private void DenyAddingFiles(string folder)
    {
        var info = new DirectoryInfo(folder);
        var deny = new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.CreateFiles, AccessControlType.Deny);
        var security = info.GetAccessControl();
        security.AddAccessRule(deny);
        info.SetAccessControl(security);
        _held.Add(new Undo(() =>
        {
            var again = info.GetAccessControl();
            again.RemoveAccessRule(deny);
            info.SetAccessControl(again);
        }));
        // The rule has to refuse a new file, or this is a test of a folder that takes one.
        Assert.Throws<UnauthorizedAccessException>(() => File.WriteAllText(Path.Combine(folder, "probe.txt"), string.Empty));
    }

    private sealed class Undo(Action undo) : IDisposable
    {
        public void Dispose() => undo();
    }

    /// <summary>Opens <paramref name="path"/> sharing reading alone, which refuses its replacement, until the answer is disposed.</summary>
    private FileStream Hold(string path)
    {
        var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        _held.Add(held);
        return held;
    }

    [Fact]
    public async Task The_newer_version_replaces_the_copy_once_it_has_ended_and_nothing_is_left_beside_it()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: false, new ReplaceSeams(ThisExe: NewerFile()));
        await ending;

        Assert.Equal(ReplaceOutcome.Replaced, outcome);
        Assert.Equal(NewerBytes, File.ReadAllBytes(Exe));
        Assert.False(File.Exists(Replacing));
        Assert.False(File.Exists(Backup));
    }

    [Fact]
    public async Task The_newer_version_is_written_beside_the_copy_before_the_event_is_set()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        byte[]? besideWhenSet = null;
        var ending = Task.Run(() =>
        {
            if (!started.WaitOne(TimeSpan.FromSeconds(60))) return;
            besideWhenSet = File.ReadAllBytes(Replacing);
            oldCopy.Kill();
            oldCopy.WaitForExit();
        });

        ExeReplacement.Run(request, startAtPath: false, new ReplaceSeams(ThisExe: NewerFile()));
        await ending;

        Assert.Equal(NewerBytes, besideWhenSet);
    }

    [Fact]
    public void A_path_that_is_not_the_copys_own_exe_is_refused_with_nothing_written_and_no_event()
    {
        var oldCopy = StartOldCopy();
        var other = Path.Combine(_root, "copy", "Other.exe");
        File.Copy(PowerShell, other);
        var (request, started) = RequestFor(oldCopy, other);
        using var _ = started;

        var outcome = ExeReplacement.Run(request, startAtPath: false, new ReplaceSeams(ThisExe: NewerFile()));

        Assert.Equal(ReplaceOutcome.NotHandedOver, outcome);
        Assert.False(started.WaitOne(0));
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(other));
        Assert.False(File.Exists(other + ExeReplacement.ReplacingSuffix));
        Assert.Contains("is not the one process", TestCrashLog.Text());
    }

    [Fact]
    public async Task A_copy_still_closing_past_the_notice_is_waited_for_and_replaced_once_it_has()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = Task.Run(async () =>
        {
            if (!started.WaitOne(TimeSpan.FromSeconds(60))) return;
            await Task.Delay(TimeSpan.FromSeconds(3));
            oldCopy.Kill();
            await oldCopy.WaitForExitAsync();
        });

        var outcome = ExeReplacement.Run(request, startAtPath: false,
            new ReplaceSeams(ThisExe: NewerFile(), OldCopyNotice: TimeSpan.FromSeconds(1)));
        await ending;

        Assert.Equal(ReplaceOutcome.Replaced, outcome);
        Assert.Equal(NewerBytes, File.ReadAllBytes(Exe));
        Assert.Contains(
            "System.TimeoutException: The copy to be replaced had not closed 1 seconds after it was told to; the replace waits on for it.",
            TestCrashLog.Text());
    }

    [Fact]
    public void A_link_at_the_path_is_refused_with_nothing_written_and_no_event()
    {
        var oldCopy = StartOldCopy();
        var link = Path.Combine(_root, "copy", "Link.exe");
        try
        {
            File.CreateSymbolicLink(link, Exe);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return; // this host cannot create symlinks; CI's runner can
        }
        var (request, started) = RequestFor(oldCopy, link);
        using var _ = started;

        var outcome = ExeReplacement.Run(request, startAtPath: false, new ReplaceSeams(ThisExe: NewerFile()));

        Assert.Equal(ReplaceOutcome.NotHandedOver, outcome);
        Assert.False(started.WaitOne(0));
        Assert.True(new FileInfo(link).Attributes.HasFlag(FileAttributes.ReparsePoint));
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
        Assert.False(File.Exists(link + ExeReplacement.ReplacingSuffix));
        Assert.Contains("is a link", TestCrashLog.Text());
    }

    [Fact]
    public async Task A_read_only_exe_is_replaced_and_the_newer_version_is_read_only_too()
    {
        var oldCopy = StartOldCopy();
        File.SetAttributes(Exe, File.GetAttributes(Exe) | FileAttributes.ReadOnly);
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: false,
            new ReplaceSeams(ThisExe: NewerFile(), MoveRetryBound: TimeSpan.FromSeconds(1)));
        await ending;

        Assert.Equal(ReplaceOutcome.Replaced, outcome);
        Assert.Equal(NewerBytes, File.ReadAllBytes(Exe));
        Assert.True(File.GetAttributes(Exe).HasFlag(FileAttributes.ReadOnly));
        Assert.False(File.Exists(Backup));
    }

    [Fact]
    public async Task A_read_only_exe_replaced_is_read_only_before_the_newer_version_is_started()
    {
        var oldCopy = StartOldCopy();
        File.SetAttributes(Exe, File.GetAttributes(Exe) | FileAttributes.ReadOnly);
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);
        bool? readOnlyWhenStarted = null;

        var outcome = ExeReplacement.Run(request, startAtPath: true, new ReplaceSeams(
            ThisExe: NewerFile(), StartAndAwait: path =>
            {
                readOnlyWhenStarted = File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly);
                return NewerVersionAnswer.Started;
            }));
        await ending;

        Assert.Equal(ReplaceOutcome.Replaced, outcome);
        Assert.True(readOnlyWhenStarted);
    }

    [Fact]
    public async Task A_read_only_exe_that_cannot_be_kept_is_left_read_only_with_nothing_beside_it()
    {
        var oldCopy = StartOldCopy();
        File.SetAttributes(Exe, File.GetAttributes(Exe) | FileAttributes.ReadOnly);
        // A folder at the second name refuses the copy written where the link is refused.
        Directory.CreateDirectory(Backup);
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: true, new ReplaceSeams(
            ThisExe: NewerFile(),
            HardLink: (_, _) => Kernel32.ERROR_INVALID_FUNCTION,
            StartAndAwait: _ => NewerVersionAnswer.Started));
        await ending;

        Assert.Equal(ReplaceOutcome.Refused, outcome);
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
        Assert.True(File.GetAttributes(Exe).HasFlag(FileAttributes.ReadOnly));
        Assert.False(File.Exists(Replacing));
    }

    [Fact]
    public async Task A_read_only_exe_is_read_only_again_where_a_step_of_the_replace_throws()
    {
        var oldCopy = StartOldCopy();
        File.SetAttributes(Exe, File.GetAttributes(Exe) | FileAttributes.ReadOnly);
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: true, new ReplaceSeams(
            ThisExe: NewerFile(), HardLink: (_, _) => throw new InvalidOperationException("thrown by the link")));
        await ending;

        Assert.Equal(ReplaceOutcome.Refused, outcome);
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
        Assert.True(File.GetAttributes(Exe).HasFlag(FileAttributes.ReadOnly));
        Assert.False(File.Exists(Replacing));
        Assert.False(File.Exists(Backup));
        Assert.Contains("thrown by the link", TestCrashLog.Text());
    }

    [Fact]
    public async Task A_step_that_throws_once_the_exe_is_linked_leaves_no_second_name_beside_the_read_only_exe()
    {
        var oldCopy = StartOldCopy();
        File.SetAttributes(Exe, File.GetAttributes(Exe) | FileAttributes.ReadOnly);
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);
        var linked = false;

        var outcome = ExeReplacement.Run(request, startAtPath: true, new ReplaceSeams(
            ThisExe: NewerFile(),
            HardLink: (link, existing) =>
            {
                linked = Kernel32.CreateHardLink(link, existing, IntPtr.Zero);
                throw new InvalidOperationException("thrown once linked");
            }));
        await ending;

        Assert.True(linked);
        Assert.Equal(ReplaceOutcome.Refused, outcome);
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
        Assert.True(File.GetAttributes(Exe).HasFlag(FileAttributes.ReadOnly));
        Assert.False(File.Exists(Backup));
        Assert.False(File.Exists(Replacing));
        Assert.Contains("thrown once linked", TestCrashLog.Text());
    }

    [Fact]
    public async Task A_linked_second_name_that_cannot_be_deleted_goes_later_and_the_exe_stays_read_only()
    {
        var oldCopy = StartOldCopy();
        File.SetAttributes(Exe, File.GetAttributes(Exe) | FileAttributes.ReadOnly);
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);
        var linked = false;

        // The second name is linked and then held, which refuses its delete, and the newer
        // version is held, which refuses the move: a rename needs to delete its source's name.
        var outcome = ExeReplacement.Run(request, startAtPath: false, new ReplaceSeams(
            ThisExe: NewerFile(),
            MoveRetryBound: TimeSpan.FromSeconds(1),
            HardLink: (link, existing) =>
            {
                linked = Kernel32.CreateHardLink(link, existing, IntPtr.Zero);
                if (linked)
                {
                    Hold(link);
                    Hold(Replacing);
                }
                return linked ? 0 : Kernel32.ERROR_INVALID_FUNCTION;
            }));
        await ending;

        Assert.True(linked);
        Assert.Equal(ReplaceOutcome.Refused, outcome);
        Assert.True(File.Exists(Backup));
        Assert.True(File.Exists(Replacing));
        Assert.Equal(NewerBytes, File.ReadAllBytes(Replacing));
        Assert.True(File.GetAttributes(Exe).HasFlag(FileAttributes.ReadOnly));
        // The hold on the move's source is what refuses the move, and the holds on the two
        // names what refuse their deletes: each a sharing violation.
        var violation = Kernel32.ERROR_SHARING_VIOLATION;
        var log = TestCrashLog.Text();
        Assert.Contains($"The newer version could not be moved over the exe; the last try failed with error {violation}.", log);
        Assert.Contains(
            $"The name ending {ExeReplacement.BackupSuffix} beside the exe could not be deleted, with error {violation}, and is left there.", log);
        Assert.Contains(
            $"The name ending {ExeReplacement.ReplacingSuffix} beside the exe could not be deleted, with error {violation}, and is left there.", log);
        foreach (var held in _held) held.Dispose();
        Assert.True(ExeReplacement.TryRemoveLeftovers(Exe));
        Assert.False(File.Exists(Backup));
        Assert.False(File.Exists(Replacing));
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
        Assert.True(File.GetAttributes(Exe).HasFlag(FileAttributes.ReadOnly));
    }

    [Fact]
    public void A_read_only_second_name_left_beside_the_exe_goes()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Exe)!);
        File.WriteAllText(Exe, "exe");
        File.WriteAllText(Backup, "backup");
        File.SetAttributes(Backup, FileAttributes.ReadOnly);

        Assert.True(ExeReplacement.TryRemoveLeftovers(Exe));
        Assert.False(File.Exists(Backup));
        Assert.False(File.GetAttributes(Exe).HasFlag(FileAttributes.ReadOnly));
    }

    [Fact]
    public void Each_earlier_version_a_completed_setup_left_goes_and_nothing_else_does()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Exe)!);
        File.WriteAllText(Exe, "exe");
        var done = new[] { Exe + ".setup-done", Exe + ".setup-done-1", Exe + ".setup-done-12" };
        var kept = new[] { Exe + ".setup-done-", Exe + ".setup-done-x", Exe + ".setup-doner", Exe + ".setup-done.txt" };
        foreach (var path in done.Concat(kept)) File.WriteAllText(path, "left");

        Assert.True(ExeReplacement.TryRemoveLeftovers(Exe));
        foreach (var path in done) Assert.False(File.Exists(path), path);
        foreach (var path in kept) Assert.True(File.Exists(path), path);
    }

    [Fact]
    public void An_earlier_version_a_failed_setup_could_not_put_back_is_kept()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Exe)!);
        File.WriteAllText(Exe, "exe");
        var held = new[] { Exe + ".setup-old", Exe + ".setup-old-1" };
        foreach (var path in held) File.WriteAllText(path, "the only copy of the earlier version");

        Assert.True(ExeReplacement.TryRemoveLeftovers(Exe));
        foreach (var path in held) Assert.True(File.Exists(path), path);
    }

    [Fact]
    public async Task A_read_only_exe_put_back_is_read_only_again()
    {
        var oldCopy = StartOldCopy();
        File.SetAttributes(Exe, File.GetAttributes(Exe) | FileAttributes.ReadOnly);
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: true, new ReplaceSeams(
            ThisExe: NewerFile(), MoveRetryBound: TimeSpan.FromSeconds(1), StartAndAwait: _ => NewerVersionAnswer.NotStarted));
        await ending;

        Assert.Equal(ReplaceOutcome.RolledBack, outcome);
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
        Assert.True(File.GetAttributes(Exe).HasFlag(FileAttributes.ReadOnly));
    }

    [Fact]
    public async Task A_read_only_exe_is_read_only_again_where_the_older_version_cannot_be_put_back()
    {
        var oldCopy = StartOldCopy();
        File.SetAttributes(Exe, File.GetAttributes(Exe) | FileAttributes.ReadOnly);
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: true, new ReplaceSeams(
            ThisExe: NewerFile(), MoveRetryBound: TimeSpan.FromSeconds(1), StartAndAwait: path =>
            {
                Hold(path);
                return NewerVersionAnswer.NotStarted;
            }));
        await ending;

        Assert.Equal(ReplaceOutcome.NotRolledBack, outcome);
        foreach (var held in _held) held.Dispose();
        Assert.Equal(NewerBytes, File.ReadAllBytes(Exe));
        Assert.True(File.GetAttributes(Exe).HasFlag(FileAttributes.ReadOnly));
        Assert.True(File.Exists(Backup));
    }

    [Fact]
    public void A_second_replace_of_the_same_exe_is_refused_while_the_first_holds_its_file()
    {
        var oldCopy = StartOldCopy();
        using var first = new FileStream(Lock, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;

        var outcome = ExeReplacement.Run(request, startAtPath: false,
            new ReplaceSeams(ThisExe: NewerFile(), HoldRetryBound: TimeSpan.FromMilliseconds(300)));

        Assert.Equal(ReplaceOutcome.NotHandedOver, outcome);
        Assert.False(started.WaitOne(0));
        Assert.False(File.Exists(Replacing));
    }

    [Fact]
    public void A_folder_at_the_held_files_name_is_refused_without_waiting_out_the_retries()
    {
        var oldCopy = StartOldCopy();
        Directory.CreateDirectory(Lock);
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var timer = Stopwatch.StartNew();

        var outcome = ExeReplacement.Run(request, startAtPath: false,
            new ReplaceSeams(ThisExe: NewerFile(), HoldRetryBound: TimeSpan.FromSeconds(30)));

        Assert.Equal(ReplaceOutcome.NotHandedOver, outcome);
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10), timer.Elapsed.ToString());
        Assert.False(started.WaitOne(0));
        Assert.True(Directory.Exists(Lock));
        Assert.Contains($"The name ending {ExeReplacement.LockSuffix} is a folder, not a plain file.", TestCrashLog.Text());
    }

    [Fact]
    public void A_link_at_the_held_files_name_is_refused_without_waiting_out_the_retries()
    {
        var oldCopy = StartOldCopy();
        var elsewhere = Directory.CreateDirectory(Path.Combine(_root, "elsewhere")).FullName;
        try
        {
            Directory.CreateSymbolicLink(Lock, elsewhere);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return; // this host cannot create symlinks; CI's runner can
        }
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var timer = Stopwatch.StartNew();

        var outcome = ExeReplacement.Run(request, startAtPath: false,
            new ReplaceSeams(ThisExe: NewerFile(), HoldRetryBound: TimeSpan.FromSeconds(30)));

        Assert.Equal(ReplaceOutcome.NotHandedOver, outcome);
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10), timer.Elapsed.ToString());
        Assert.False(started.WaitOne(0));
        Assert.True(Directory.Exists(elsewhere));
        Assert.Contains($"The name ending {ExeReplacement.LockSuffix} is a reparse point, not a plain file.", TestCrashLog.Text());
    }

    [Fact]
    public void A_replace_started_while_a_clean_up_holds_its_file_past_the_retries_is_not_handed_over()
    {
        var oldCopy = StartOldCopy();
        File.WriteAllText(Backup, "left by an earlier update");
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        ReplaceOutcome? outcome = null;

        var removed = ExeReplacement.TryRemoveLeftovers(Exe, whileHeld: () =>
            outcome = ExeReplacement.Run(request, startAtPath: false,
                new ReplaceSeams(ThisExe: NewerFile(), HoldRetryBound: TimeSpan.FromMilliseconds(300))));

        Assert.True(removed);
        Assert.Equal(ReplaceOutcome.NotHandedOver, outcome);
        Assert.False(started.WaitOne(0));
        Assert.False(File.Exists(Replacing));
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
    }

    [Fact]
    public async Task A_replace_started_while_a_clean_up_holds_its_file_goes_ahead_once_the_clean_up_is_done()
    {
        var oldCopy = StartOldCopy();
        File.WriteAllText(Backup, "left by an earlier update");
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);
        var newer = NewerFile();
        Task<ReplaceOutcome>? replace = null;
        bool? waitingWhileHeld = null;

        var removed = ExeReplacement.TryRemoveLeftovers(Exe, whileHeld: () =>
        {
            replace = Task.Run(() => ExeReplacement.Run(request, startAtPath: false,
                new ReplaceSeams(ThisExe: newer, HoldRetryBound: TimeSpan.FromSeconds(20))));
            Thread.Sleep(TimeSpan.FromSeconds(1));
            waitingWhileHeld = !replace.IsCompleted && !started.WaitOne(0);
        });
        var outcome = await replace!;
        await ending;

        Assert.True(removed);
        Assert.True(waitingWhileHeld);
        Assert.Equal(ReplaceOutcome.Replaced, outcome);
        Assert.Equal(NewerBytes, File.ReadAllBytes(Exe));
        Assert.False(File.Exists(Backup));
    }

    [Fact]
    public void A_replace_that_cannot_make_its_held_file_says_what_Windows_answered()
    {
        var oldCopy = StartOldCopy();
        DenyAddingFiles(Path.GetDirectoryName(Exe)!);
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;

        var outcome = ExeReplacement.Run(request, startAtPath: false,
            new ReplaceSeams(ThisExe: NewerFile(), HoldRetryBound: TimeSpan.FromMilliseconds(300)));

        Assert.Equal(ReplaceOutcome.NotHandedOver, outcome);
        Assert.False(started.WaitOne(0));
        Assert.Contains($"could not be created: error {Kernel32.ERROR_ACCESS_DENIED}.", TestCrashLog.Text());
    }

    [Fact]
    public async Task A_replace_holds_its_file_while_it_runs_and_it_is_gone_once_it_ends()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        bool? removedWhileRunning = null;
        var ending = Task.Run(() =>
        {
            if (!started.WaitOne(TimeSpan.FromSeconds(60))) return;
            removedWhileRunning = ExeReplacement.TryRemoveLeftovers(Exe);
            oldCopy.Kill();
            oldCopy.WaitForExit();
        });

        var outcome = ExeReplacement.Run(request, startAtPath: false, new ReplaceSeams(ThisExe: NewerFile()));
        await ending;

        Assert.Equal(ReplaceOutcome.Replaced, outcome);
        Assert.False(removedWhileRunning);
        Assert.Equal(NewerBytes, File.ReadAllBytes(Exe));
        Assert.False(File.Exists(Lock));
    }

    [Fact]
    public async Task An_exe_held_past_the_retries_is_left_in_place_with_nothing_beside_it()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        Hold(Exe);
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: false,
            new ReplaceSeams(ThisExe: NewerFile(), MoveRetryBound: TimeSpan.FromSeconds(1)));
        await ending;

        Assert.Equal(ReplaceOutcome.Refused, outcome);
        foreach (var held in _held) held.Dispose();
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
        Assert.False(File.Exists(Replacing));
        // Windows answers a move over a file held open with a sharing violation or access denied.
        var log = TestCrashLog.Text();
        Assert.True(
            log.Contains($"failed with error {Kernel32.ERROR_SHARING_VIOLATION}.")
                || log.Contains($"failed with error {Kernel32.ERROR_ACCESS_DENIED}."),
            log);
        // The second name is the exe's own file where the volume links it, so the hold that
        // refused the move can refuse its delete as well. A clean-up made with nothing
        // holding the file and nothing running from it takes it.
        Assert.True(ExeReplacement.TryRemoveLeftovers(Exe));
        Assert.False(File.Exists(Backup));
    }

    [Fact]
    public async Task An_exe_let_go_of_inside_the_retries_is_replaced()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var held = Hold(Exe);
        var ending = Task.Run(async () =>
        {
            if (!started.WaitOne(TimeSpan.FromSeconds(60))) return;
            oldCopy.Kill();
            await oldCopy.WaitForExitAsync();
            await Task.Delay(TimeSpan.FromSeconds(1));
            held.Dispose();
        });

        var outcome = ExeReplacement.Run(request, startAtPath: false,
            new ReplaceSeams(ThisExe: NewerFile(), MoveRetryBound: TimeSpan.FromSeconds(20)));
        await ending;

        Assert.Equal(ReplaceOutcome.Replaced, outcome);
        Assert.Equal(NewerBytes, File.ReadAllBytes(Exe));
    }

    [Fact]
    public async Task Nothing_is_put_at_a_path_left_empty_once_the_copy_has_ended()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var aside = Exe + ".aside";
        // Windows lets a running exe be renamed, so the path is empty as the copy ends.
        var ending = Task.Run(() =>
        {
            if (!started.WaitOne(TimeSpan.FromSeconds(60))) return;
            File.Move(Exe, aside);
            oldCopy.Kill();
            oldCopy.WaitForExit();
        });

        var outcome = ExeReplacement.Run(request, startAtPath: true,
            new ReplaceSeams(ThisExe: NewerFile(), StartAndAwait: _ => throw new InvalidOperationException("started")));
        await ending;

        Assert.Equal(ReplaceOutcome.NothingAtPath, outcome);
        Assert.False(File.Exists(Exe));
        Assert.False(File.Exists(Replacing));
        Assert.False(File.Exists(Backup));
        var log = TestCrashLog.Text();
        Assert.Contains("Nothing was at the exe's path once its copy had ended", log);
        Assert.Contains(nameof(FileNotFoundException), log);
    }

    [Fact]
    public async Task Nothing_is_put_at_a_path_left_empty_as_the_exe_is_kept()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: true, new ReplaceSeams(
            ThisExe: NewerFile(),
            HardLink: (_, existing) =>
            {
                File.Move(existing, existing + ".aside");
                return Kernel32.ERROR_FILE_NOT_FOUND;
            },
            StartAndAwait: _ => throw new InvalidOperationException("started")));
        await ending;

        Assert.Equal(ReplaceOutcome.NothingAtPath, outcome);
        Assert.False(File.Exists(Exe));
        Assert.False(File.Exists(Replacing));
        Assert.False(File.Exists(Backup));
        var log = TestCrashLog.Text();
        Assert.Contains("Nothing was at the exe's path once its copy had ended", log);
        // The failure in hand named the copy's own file, read through the second name's
        // copy, so the reading of the exe that found the path empty goes beside it.
        Assert.Contains(nameof(AggregateException), log);
    }

    [Fact]
    public async Task A_path_left_empty_is_written_to_the_application_log_with_where_to_download_again()
    {
        EventLogRecorder.Clear();
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = Task.Run(() =>
        {
            if (!started.WaitOne(TimeSpan.FromSeconds(60))) return;
            File.Move(Exe, Exe + ".aside");
            oldCopy.Kill();
            oldCopy.WaitForExit();
        });

        var outcome = ExeReplacement.Run(request, startAtPath: false, new ReplaceSeams(ThisExe: NewerFile()));
        await ending;

        Assert.Equal(ReplaceOutcome.NothingAtPath, outcome);
        var entry = Assert.Single(EventLogRecorder.Entries);
        Assert.Equal(CliEventClass.UpdateNotInstalledNotice, entry.Class);
        Assert.Equal(3007, CliContract.EventIdFor(entry.Class));
        Assert.StartsWith("An update to InstallerClean ", entry.Text);
        Assert.EndsWith($"can be downloaded again from {UpdateCheckService.ReleasesPageUrl}.", entry.Text);
    }

    [Fact]
    public async Task A_replace_refused_with_the_exe_still_there_writes_nothing_to_the_application_log()
    {
        EventLogRecorder.Clear();
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        Hold(Exe);
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: false,
            new ReplaceSeams(ThisExe: NewerFile(), MoveRetryBound: TimeSpan.FromSeconds(1)));
        await ending;

        Assert.Equal(ReplaceOutcome.Refused, outcome);
        Assert.Empty(EventLogRecorder.Entries);
    }

    [Fact]
    public void A_step_that_throws_before_the_event_leaves_the_copy_carrying_on_with_nothing_beside_it()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;

        // An empty path is refused by the copy's own open with an exception none of the
        // write's catches take.
        var outcome = ExeReplacement.Run(request, startAtPath: false, new ReplaceSeams(ThisExe: string.Empty));

        Assert.Equal(ReplaceOutcome.NotHandedOver, outcome);
        Assert.False(started.WaitOne(0));
        Assert.False(File.Exists(Replacing));
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
    }

    [Fact]
    public void A_step_that_throws_before_the_copy_is_seen_to_end_answers_that_with_nothing_beside_the_exe()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;

        var outcome = ExeReplacement.Run(request, startAtPath: false, new ReplaceSeams(
            ThisExe: NewerFile(),
            WaitForEnd: (_, _, out _) => throw new InvalidOperationException("thrown by the wait")));

        Assert.Equal(ReplaceOutcome.OldCopyNotSeenToEnd, outcome);
        Assert.True(started.WaitOne(0));
        Assert.False(File.Exists(Replacing));
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
        Assert.Contains("thrown by the wait", TestCrashLog.Text());
    }

    [Fact]
    public void A_wait_refused_with_the_newer_version_held_says_in_crash_log_that_it_is_left_beside_the_exe()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;

        var outcome = ExeReplacement.Run(request, startAtPath: false, new ReplaceSeams(
            ThisExe: NewerFile(),
            WaitForEnd: (_, _, out error) =>
            {
                Hold(Replacing);
                error = Kernel32.ERROR_INVALID_PARAMETER;
                return false;
            }));

        Assert.Equal(ReplaceOutcome.OldCopyNotSeenToEnd, outcome);
        Assert.True(File.Exists(Replacing));
        var log = TestCrashLog.Text();
        Assert.Contains(
            $"The name ending {ExeReplacement.ReplacingSuffix} beside the exe could not be deleted, "
            + $"with error {Kernel32.ERROR_SHARING_VIOLATION}, and is left there.", log);
        Assert.Contains($"Waiting for the copy to be replaced to end failed with error {Kernel32.ERROR_INVALID_PARAMETER}", log);
    }

    [Fact]
    public async Task A_newer_version_that_does_not_start_at_the_path_is_replaced_by_the_older_one_again()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);
        byte[]? atPathWhenStarted = null;

        var outcome = ExeReplacement.Run(request, startAtPath: true, new ReplaceSeams(
            ThisExe: NewerFile(),
            StartAndAwait: path =>
            {
                atPathWhenStarted = File.ReadAllBytes(path);
                return NewerVersionAnswer.NotStarted;
            }));
        await ending;

        Assert.Equal(ReplaceOutcome.RolledBack, outcome);
        Assert.Equal(NewerBytes, atPathWhenStarted);
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
        Assert.False(File.Exists(Replacing));
        Assert.False(File.Exists(Backup));
    }

    [Fact]
    public async Task A_newer_version_that_starts_at_the_path_stays_and_the_older_one_goes()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: true,
            new ReplaceSeams(ThisExe: NewerFile(), StartAndAwait: _ => NewerVersionAnswer.Started));
        await ending;

        Assert.Equal(ReplaceOutcome.Replaced, outcome);
        Assert.Equal(NewerBytes, File.ReadAllBytes(Exe));
        Assert.False(File.Exists(Backup));
    }

    [Fact]
    public async Task A_newer_version_that_will_not_end_is_left_at_the_path_with_the_older_one_kept_beside_it()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: true,
            new ReplaceSeams(ThisExe: NewerFile(), StartAndAwait: _ => NewerVersionAnswer.StillRunning));
        await ending;

        Assert.Equal(ReplaceOutcome.NewerLeftRunning, outcome);
        Assert.Equal(NewerBytes, File.ReadAllBytes(Exe));
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Backup));
        var log = TestCrashLog.Text();
        Assert.Contains("still running", log);
        Assert.Contains(ExeReplacement.BackupSuffix, log);
    }

    [Fact]
    public async Task Where_the_volume_will_not_link_the_exe_a_copy_of_it_is_what_is_put_back()
    {
        var oldCopy = StartOldCopy();
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: true, new ReplaceSeams(
            ThisExe: NewerFile(),
            HardLink: (_, _) => Kernel32.ERROR_INVALID_FUNCTION,
            StartAndAwait: _ => NewerVersionAnswer.NotStarted));
        await ending;

        Assert.Equal(ReplaceOutcome.RolledBack, outcome);
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
        Assert.False(File.Exists(Backup));
        Assert.Contains($"The volume refused the exe a second name with error {Kernel32.ERROR_INVALID_FUNCTION}.", TestCrashLog.Text());
    }

    [Fact]
    public async Task A_plain_file_left_at_the_newer_versions_name_is_written_over()
    {
        var oldCopy = StartOldCopy();
        File.WriteAllText(Replacing, "left by an earlier update");
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;
        var ending = EndOnceSet(oldCopy, started);

        var outcome = ExeReplacement.Run(request, startAtPath: false, new ReplaceSeams(ThisExe: NewerFile()));
        await ending;

        Assert.Equal(ReplaceOutcome.Replaced, outcome);
        Assert.Equal(NewerBytes, File.ReadAllBytes(Exe));
    }

    [Fact]
    public void A_link_at_the_newer_versions_name_is_refused_and_never_followed()
    {
        var oldCopy = StartOldCopy();
        var decoy = Path.Combine(_root, "decoy.txt");
        File.WriteAllText(decoy, "decoy");
        try
        {
            File.CreateSymbolicLink(Replacing, decoy);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return; // this host cannot create symlinks; CI's runner can
        }
        var (request, started) = RequestFor(oldCopy);
        using var _ = started;

        var outcome = ExeReplacement.Run(request, startAtPath: false, new ReplaceSeams(ThisExe: NewerFile()));

        Assert.Equal(ReplaceOutcome.NotHandedOver, outcome);
        Assert.False(started.WaitOne(0));
        Assert.Equal("decoy", File.ReadAllText(decoy));
        Assert.True(new FileInfo(Replacing).Attributes.HasFlag(FileAttributes.ReparsePoint));
        Assert.Equal(File.ReadAllBytes(PowerShell), File.ReadAllBytes(Exe));
    }

    [Fact]
    public void Leftovers_beside_the_exe_go_while_a_file_is_at_the_path()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Exe)!);
        File.WriteAllText(Exe, "exe");
        File.WriteAllText(Replacing, "replacing");
        File.WriteAllText(Backup, "backup");

        Assert.True(ExeReplacement.TryRemoveLeftovers(Exe));
        Assert.False(File.Exists(Replacing));
        Assert.False(File.Exists(Backup));
        Assert.True(File.Exists(Exe));
    }

    [Fact]
    public void Nothing_beside_the_exe_is_nothing_left_in_a_folder_that_takes_no_new_file()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Exe)!);
        File.WriteAllText(Exe, "exe");
        DenyAddingFiles(Path.GetDirectoryName(Exe)!);

        Assert.True(ExeReplacement.TryRemoveLeftovers(Exe));
        Assert.False(File.Exists(Lock));
    }

    [Fact]
    public void Leftovers_beside_the_exe_stay_while_a_replace_holds_its_file()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Exe)!);
        File.WriteAllText(Exe, "exe");
        File.WriteAllText(Replacing, "replacing");
        using var held = new FileStream(Lock, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);

        Assert.False(ExeReplacement.TryRemoveLeftovers(Exe));
        Assert.True(File.Exists(Replacing));
    }

    [Fact]
    public void A_held_file_left_by_a_replace_that_ended_goes_with_the_rest()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Exe)!);
        File.WriteAllText(Exe, "exe");
        File.WriteAllText(Lock, string.Empty);
        File.WriteAllText(Backup, "backup");

        Assert.True(ExeReplacement.TryRemoveLeftovers(Exe));
        Assert.False(File.Exists(Lock));
        Assert.False(File.Exists(Backup));
    }

    [Fact]
    public void Leftovers_beside_the_exe_stay_while_nothing_is_at_the_path()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Exe)!);
        File.WriteAllText(Backup, "backup");

        Assert.False(ExeReplacement.TryRemoveLeftovers(Exe));
        Assert.True(File.Exists(Backup));
    }

    [Fact]
    public void A_link_left_beside_the_exe_is_not_removed_through()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Exe)!);
        File.WriteAllText(Exe, "exe");
        var decoy = Path.Combine(_root, "decoy.txt");
        File.WriteAllText(decoy, "decoy");
        try
        {
            File.CreateSymbolicLink(Backup, decoy);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return; // this host cannot create symlinks; CI's runner can
        }

        Assert.False(ExeReplacement.TryRemoveLeftovers(Exe));
        Assert.True(File.Exists(Backup));
        Assert.Equal("decoy", File.ReadAllText(decoy));
    }
}
