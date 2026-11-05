using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Services;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The command line's replace on the setup's copy (<see cref="SetupReplacement"/>): the
/// setup beside the newer exe is checked for the newer version and held, the started event
/// is set only once it has been, the copy is waited for and then InstallerClean, and the
/// setup is run and judged. A copy of the test host stands in for the setup, carrying a real
/// version resource, and the waits and the start are seams.
/// </summary>
public sealed class SetupReplacementTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("ic-setup-replace-").FullName;
    private readonly IDisposable _crashLog = TestCrashLog.OwnFolder();
    private readonly string _staging;
    private readonly string _version;
    private readonly string _setup;
    private readonly ReplaceRequest _request;
    private readonly EventWaitHandle _started;
    private readonly List<(string File, IReadOnlyList<string> Arguments, string Folder)> _starts = [];

    public SetupReplacementTests()
    {
        _staging = Directory.CreateDirectory(Path.Combine(_root, "staging")).FullName;
        var copyFolder = Directory.CreateDirectory(Path.Combine(_root, "Program Files", "InstallerClean")).FullName;
        _version = FileVersionResource.Read(Environment.ProcessPath!)!.ToString(3);
        _setup = Path.Combine(_staging, AppCopy.DownloadFileName(AppCopyKind.Setup, _version)!);
        File.Copy(Environment.ProcessPath!, _setup);
        var id = Guid.NewGuid();
        _started = HandOverContract.CreateStartedEvent(id)!;
        _request = new ReplaceRequest(Path.Combine(copyFolder, AppCopy.CommandLineFileName), 4, id.ToString("N"), []);
    }

    public void Dispose()
    {
        try
        {
            _started.Dispose();
            TestCrashLog.TryDelete(_root);
        }
        finally
        {
            _crashLog.Dispose();
        }
    }

    /// <summary>A handle that waits like the copy's: this process's own.</summary>
    private static SafeProcessHandle OwnProcess() =>
        Kernel32.OpenProcess(Ntdll.SYNCHRONIZE, bInheritHandle: false, (uint)Environment.ProcessId);

    /// <summary>
    /// Seams under which every step succeeds and the setup exits with
    /// <paramref name="exitCode"/>, each overridable, recording each start.
    /// </summary>
    private SetupReplaceSeams Seams(int exitCode = 0) => new(
        ThisExe: Path.Combine(_staging, AppCopy.CommandLineFileName),
        Version: _version,
        InUseBound: TimeSpan.FromSeconds(1),
        OpenCopy: (ReplaceRequest _, out UpdateFailedException? refused) =>
        {
            refused = null;
            return OwnProcess();
        },
        WaitForEnd: (SafeProcessHandle _, TimeSpan _, out int error) =>
        {
            error = 0;
            return true;
        },
        WaitUntilFree: (_, _) => true,
        Start: (file, arguments, folder) =>
        {
            _starts.Add((file, arguments, folder));
            return OwnProcess();
        },
        WaitForExit: (SafeProcessHandle _, out int code, out int error) =>
        {
            code = exitCode;
            error = 0;
            return true;
        },
        CarriesVersion: (string _, string _, out Exception? unread) =>
        {
            unread = null;
            return true;
        });

    [Fact]
    public void A_setup_carrying_this_version_is_run_hidden_from_its_own_folder_and_the_copy_is_installed()
    {
        var outcome = SetupReplacement.Run(_request, Seams(), out var exitCode);

        Assert.Equal(SetupReplaceOutcome.Installed, outcome);
        Assert.Equal(0, exitCode);
        Assert.True(_started.WaitOne(0));
        var start = Assert.Single(_starts);
        Assert.Equal(_setup, start.File);
        Assert.Equal(CommandLineUpdate.SetupArguments.Split(' '), start.Arguments);
        Assert.Equal(_staging, start.Folder);
        Assert.Empty(TestCrashLog.Entries());
    }

    [Fact]
    public void The_setup_is_held_against_change_from_before_the_event_until_it_has_been_started()
    {
        bool? deletable = null;
        bool? writable = null;
        var seams = Seams() with
        {
            Start = (file, arguments, folder) =>
            {
                deletable = Try(() => File.Delete(file));
                writable = Try(() => new FileStream(file, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete).Dispose());
                return OwnProcess();
            },
        };

        Assert.Equal(SetupReplaceOutcome.Installed, SetupReplacement.Run(_request, seams, out _));

        Assert.False(deletable);
        Assert.False(writable);
        // Let go once it has started.
        File.Delete(_setup);

        static bool Try(Action act)
        {
            try
            {
                act();
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    [Fact]
    public void A_setup_carrying_another_version_is_not_run_and_the_copy_is_not_told_it_is_being_replaced()
    {
        var outcome = SetupReplacement.Run(_request, Seams() with { Version = "99.0.0", ThisExe = ThisExeBesideSetupFor("99.0.0") },
            out var exitCode);

        Assert.Equal(SetupReplaceOutcome.NotThisVersion, outcome);
        Assert.Null(exitCode);
        Assert.False(_started.WaitOne(0));
        Assert.Empty(_starts);
        Assert.Contains("does not carry version 99.0.0", TestCrashLog.OnlyEntry());
    }

    /// <summary>A staging folder holding the test host under the setup's name for <paramref name="version"/>, and the exe's path in it.</summary>
    private string ThisExeBesideSetupFor(string version)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_root, "staging-" + version)).FullName;
        File.Copy(Environment.ProcessPath!, Path.Combine(folder, AppCopy.DownloadFileName(AppCopyKind.Setup, version)!));
        return Path.Combine(folder, AppCopy.CommandLineFileName);
    }

    [Fact]
    public void No_setup_beside_the_exe_runs_nothing_and_sets_no_event()
    {
        File.Delete(_setup);

        Assert.Equal(SetupReplaceOutcome.NotStarted, SetupReplacement.Run(_request, Seams(), out _));

        Assert.False(_started.WaitOne(0));
        Assert.Empty(_starts);
        Assert.Contains("could not be opened", TestCrashLog.OnlyEntry());
    }

    [Fact]
    public void A_copy_that_is_not_the_requests_process_runs_nothing_and_sets_no_event()
    {
        var seams = Seams() with
        {
            OpenCopy = (ReplaceRequest _, out UpdateFailedException? refused) =>
            {
                refused = new UpdateFailedException(UpdateFailureStage.Install, "not the copy");
                return null;
            },
        };

        Assert.Equal(SetupReplaceOutcome.NotHandedOver, SetupReplacement.Run(_request, seams, out _));

        Assert.False(_started.WaitOne(0));
        Assert.Empty(_starts);
        Assert.Contains("not the copy", TestCrashLog.OnlyEntry());
    }

    [Fact]
    public void InstallerClean_still_in_use_after_the_wait_runs_no_setup()
    {
        TimeSpan? waited = null;
        string? folder = null;
        var seams = Seams() with
        {
            WaitUntilFree = (copyFolder, bound) =>
            {
                folder = copyFolder;
                waited = bound;
                return false;
            },
        };

        Assert.Equal(SetupReplaceOutcome.InUse, SetupReplacement.Run(_request, seams, out _));

        Assert.True(_started.WaitOne(0));
        Assert.Equal(Path.GetDirectoryName(_request.ExePath), folder);
        Assert.Equal(TimeSpan.FromSeconds(1), waited);
        Assert.Empty(_starts);
        Assert.Contains("still in use", TestCrashLog.OnlyEntry());
    }

    [Fact]
    public void A_setup_that_cannot_be_started_is_not_run()
    {
        var seams = Seams() with { Start = (_, _, _) => null };

        Assert.Equal(SetupReplaceOutcome.NotStarted, SetupReplacement.Run(_request, seams, out var exitCode));

        Assert.Null(exitCode);
        Assert.Contains("could not be started", TestCrashLog.OnlyEntry());
    }

    [Fact]
    public void A_setup_ending_with_another_exit_code_fails_with_that_code()
    {
        Assert.Equal(SetupReplaceOutcome.SetupFailed, SetupReplacement.Run(_request, Seams(exitCode: 5), out var exitCode));

        Assert.Equal(5, exitCode);
        Assert.Contains("exit code 5", TestCrashLog.OnlyEntry());
    }

    [Fact]
    public void A_setup_whose_end_cannot_be_read_fails_with_no_exit_code()
    {
        var seams = Seams() with
        {
            WaitForExit = (SafeProcessHandle _, out int code, out int error) =>
            {
                code = 0;
                error = 6;
                return false;
            },
        };

        Assert.Equal(SetupReplaceOutcome.SetupFailed, SetupReplacement.Run(_request, seams, out var exitCode));

        Assert.Null(exitCode);
        Assert.Contains("failed with error 6", TestCrashLog.OnlyEntry());
    }

    [Fact]
    public void A_setup_that_ends_well_without_putting_this_version_at_the_path_is_not_in_place()
    {
        var seams = Seams() with
        {
            CarriesVersion = (string path, string version, out Exception? unread) =>
            {
                Assert.Equal(_request.ExePath, path);
                Assert.Equal(_version, version);
                unread = null;
                return false;
            },
        };

        Assert.Equal(SetupReplaceOutcome.NotInPlace, SetupReplacement.Run(_request, seams, out var exitCode));

        Assert.Equal(0, exitCode);
        Assert.Contains("without putting version", TestCrashLog.OnlyEntry());
    }

    [Fact]
    public void A_throw_after_the_setup_has_started_is_answered_as_a_failed_setup_and_one_before_as_not_started()
    {
        var after = Seams() with
        {
            WaitForExit = (SafeProcessHandle _, out int code, out int error) => throw new InvalidOperationException("after"),
        };
        var before = Seams() with { WaitUntilFree = (_, _) => throw new InvalidOperationException("before") };

        Assert.Equal(SetupReplaceOutcome.SetupFailed, SetupReplacement.Run(_request, after, out var afterCode));
        Assert.Null(afterCode);
        Assert.Equal(SetupReplaceOutcome.NotStarted, SetupReplacement.Run(_request, before, out _));
    }

    [Fact]
    public void A_process_started_from_the_file_is_found_and_one_started_from_another_file_of_its_name_is_not()
    {
        // A copy of a program under a name no other process has, and a second copy of the
        // same name in another folder that is never started.
        var name = $"ic-run-{Guid.NewGuid():N}.exe";
        var running = Path.Combine(_root, name);
        var elsewhere = Path.Combine(Directory.CreateDirectory(Path.Combine(_root, "elsewhere")).FullName, name);
        File.Copy(Path.Combine(Environment.SystemDirectory, "ping.exe"), running);
        File.Copy(running, elsewhere);

        Assert.False(SetupReplacement.AnotherProcessMayRunFrom(running));

        using (SuspendedProcess.Start(running))
        {
            Assert.True(SetupReplacement.AnotherProcessMayRunFrom(running));
            Assert.False(SetupReplacement.AnotherProcessMayRunFrom(elsewhere));
        }

        Assert.False(SetupReplacement.AnotherProcessMayRunFrom(running));
    }

    /// <summary>
    /// A process started suspended, so it runs none of its own code and stays until it is
    /// ended. Disposing it ends it, waits for that and closes both of its handles, after
    /// which Windows no longer lists it.
    /// </summary>
    private sealed class SuspendedProcess : IDisposable
    {
        private const uint CreateSuspended = 0x00000004;
        private readonly IntPtr _process;
        private readonly IntPtr _thread;

        private SuspendedProcess(IntPtr process, IntPtr thread)
        {
            _process = process;
            _thread = thread;
        }

        public static SuspendedProcess Start(string path)
        {
            var startup = new Kernel32.STARTUPINFO { cb = (uint)Marshal.SizeOf<Kernel32.STARTUPINFO>() };
            var commandLine = $"\"{path}\"\0".ToCharArray();
            if (!Kernel32.CreateProcess(path, commandLine, IntPtr.Zero, IntPtr.Zero, bInheritHandles: false, CreateSuspended,
                    IntPtr.Zero, Path.GetDirectoryName(path)!, in startup, out var started))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            return new SuspendedProcess(started.hProcess, started.hThread);
        }

        public void Dispose()
        {
            Kernel32.TerminateProcess(_process, 1);
            WaitForSingleObject(_process, 10_000);
            CloseHandle(_thread);
            CloseHandle(_process);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
