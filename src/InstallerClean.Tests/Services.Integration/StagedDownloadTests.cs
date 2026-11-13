using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;
using static InstallerClean.Tests.Services.Integration.UpdateInPlaceTests;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The downloads an update holds open from their check until they are used or taken away:
/// <see cref="AppUpdater.PrepareAsync"/>'s, which the window starts and the command line copies
/// beside its exe, and the one the command line runs its
/// command from (<see cref="AppUpdater.PrepareStagedRunAsync"/>), the command line's own
/// download and on the setup's copy the setup beside it in one staging folder, against a
/// temporary folder standing in for %ProgramData%.
/// </summary>
public sealed class StagedDownloadTests : IDisposable
{
    private readonly string _root = TestTemp.NewFolder("ic-staged-");
    private readonly string _stagingRoot;
    private readonly string _exe;
    private readonly List<PreparedUpdate> _prepared = [];
    private readonly IDisposable _crashLog = TestCrashLog.OwnFolder();

    public StagedDownloadTests()
    {
        _stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        _exe = Path.Combine(Directory.CreateDirectory(Path.Combine(_root, "app")).FullName, AppCopy.CommandLineFileName);
        File.WriteAllBytes(_exe, Program(1));
    }

    public void Dispose()
    {
        try
        {
            foreach (var prepared in _prepared) prepared.Undo();
            foreach (var folder in Directory.EnumerateDirectories(_root, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var security = new DirectorySecurity();
                    security.SetSecurityDescriptorSddlForm(AdministratorsAndThisAccount);
                    new DirectoryInfo(folder).SetAccessControl(security);
                }
                catch { }
            }
            TestTemp.Delete(_root);
        }
        finally
        {
            _crashLog.Dispose();
        }
    }

    /// <summary>
    /// Answers each download by its file name from <paramref name="bodies"/>, with 404 Not
    /// Found for a name it does not hold, and the release's update.json with
    /// <paramref name="manifest"/>. Records the file names asked for, in order.
    /// </summary>
    private sealed class ByName(Dictionary<string, byte[]> bodies, string manifest = AllowsEveryDownload)
        : HttpMessageHandler
    {
        public List<string> Asked { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var name = Path.GetFileName(request.RequestUri!.AbsolutePath);
            if (name == UpdateManifest.FileName)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(manifest) });
            Asked.Add(name);
            return Task.FromResult(bodies.TryGetValue(name, out var body)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private static string SetupName => AppCopy.DownloadFileName(AppCopyKind.Setup, CoreRelease)!;

    private AppUpdater Updater(
        AppCopyKind kind, HttpMessageHandler network, IRegistryReader? registry = null,
        TimeSpan? holdRetryBound = null, Action<string>? beforeHold = null) =>
        new(kind, _exe, new UpdateDownloadService(network, null), _stagingRoot,
            CommandLineUpdate.SetupArguments, AdministratorsAndThisAccount, registry ?? NoRegistry,
            holdRetryBound, beforeHold);

    private async Task<PreparedUpdate> Prepare(AppUpdater updater, CancellationToken cancellationToken = default)
    {
        var prepared = await updater.PrepareStagedRunAsync(CoreRelease, cancellationToken);
        _prepared.Add(prepared);
        return prepared;
    }

    private void AssertNothingLeft()
    {
        Assert.Empty(Directory.EnumerateFileSystemEntries(_stagingRoot));
    }

    private static void AssertHeld(string path)
    {
        var error = Assert.Throws<IOException>(() => new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite).Dispose());
        Assert.Equal(Kernel32.ERROR_SHARING_VIOLATION, error.HResult & 0xFFFF);
        error = Assert.Throws<IOException>(() => File.Delete(path));
        Assert.Equal(Kernel32.ERROR_SHARING_VIOLATION, error.HResult & 0xFFFF);
        error = Assert.Throws<IOException>(() => File.Move(path, path + ".moved"));
        Assert.Equal(Kernel32.ERROR_SHARING_VIOLATION, error.HResult & 0xFFFF);
    }

    [Fact]
    public async Task A_download_reports_how_far_it_has_got_and_then_that_it_is_being_checked()
    {
        var network = new ByName(new() { ["InstallerClean-portable.exe"] = Release(2) });
        var seen = new List<UpdateProgress>();

        _prepared.Add(await Updater(AppCopyKind.Portable, network)
            .PrepareAsync(CoreRelease, new SyncProgress<UpdateProgress>(seen.Add), CancellationToken.None));

        Assert.True(seen.Count >= 2);
        Assert.Equal(new UpdateProgress(null, Checking: true), seen[^1]);
        Assert.All(seen.GetRange(0, seen.Count - 1), step => Assert.False(step.Checking));
        Assert.Equal(1d, seen[^2].Fraction);
    }

    [Theory]
    [InlineData(AppCopyKind.Setup)]
    [InlineData(AppCopyKind.Portable)]
    [InlineData(AppCopyKind.CommandLineDownload)]
    public async Task A_prepared_download_is_held_from_its_check_until_its_undo(AppCopyKind kind)
    {
        var name = AppCopy.DownloadFileName(kind, CoreRelease)!;
        var network = new ByName(new() { [name] = Release(2) });
        var prepared = await Updater(kind, network).PrepareAsync(CoreRelease, null, CancellationToken.None);
        _prepared.Add(prepared);

        // Nothing can change the bytes between the check and the start.
        Assert.Equal(name, Path.GetFileName(prepared.FileToStart));
        Assert.Equal(Release(2), File.ReadAllBytes(prepared.FileToStart));
        AssertHeld(prepared.FileToStart);
        Assert.False(UpdateStagingFolder.RemoveLeftovers(_stagingRoot, ThisAccountExpectation));

        prepared.Undo();

        Assert.Empty(Directory.EnumerateFileSystemEntries(_stagingRoot));
    }

    [Fact]
    public async Task The_command_lines_download_is_held_in_a_folder_of_its_own_until_its_undo()
    {
        var network = new ByName(new() { [AppCopy.CommandLineFileName] = Release(2) });

        var prepared = await Prepare(Updater(AppCopyKind.CommandLineDownload, network));

        var folder = Assert.Single(Directory.EnumerateDirectories(_stagingRoot));
        Assert.Equal(Path.Combine(folder, AppCopy.CommandLineFileName), prepared.FileToStart);
        Assert.Equal(_exe, prepared.Replaces);
        Assert.Equal(string.Empty, prepared.Arguments);
        Assert.Equal(Release(2), File.ReadAllBytes(prepared.FileToStart));
        AssertHeld(prepared.FileToStart);
        Assert.False(UpdateStagingFolder.RemoveLeftovers(_stagingRoot, ThisAccountExpectation));
        Assert.Equal(Program(1), File.ReadAllBytes(_exe));

        prepared.Undo();

        AssertNothingLeft();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_download_another_process_holds_to_write_for_a_second_is_held_once_it_lets_go(bool stagedRun)
    {
        var network = new ByName(new() { [AppCopy.CommandLineFileName] = Release(2) });
        FileStream? writer = null;
        Task? letGo = null;
        var updater = Updater(AppCopyKind.CommandLineDownload, network, beforeHold: path =>
        {
            writer = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            letGo = Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(_ => writer.Dispose());
        });
        try
        {
            var prepared = stagedRun
                ? await updater.PrepareStagedRunAsync(CoreRelease, CancellationToken.None)
                : await updater.PrepareAsync(CoreRelease, null, CancellationToken.None);
            _prepared.Add(prepared);

            Assert.NotNull(letGo);
            Assert.Equal(Release(2), File.ReadAllBytes(prepared.FileToStart));
            AssertHeld(prepared.FileToStart);
        }
        finally
        {
            if (letGo is not null) await letGo;
            writer?.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_download_another_process_holds_to_write_past_the_bound_fails_with_nothing_left(bool stagedRun)
    {
        var network = new ByName(new() { [AppCopy.CommandLineFileName] = Release(2) });
        FileStream? writer = null;
        var updater = Updater(AppCopyKind.CommandLineDownload, network, holdRetryBound: TimeSpan.FromSeconds(1),
            beforeHold: path => writer = new FileStream(
                path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete));
        try
        {
            var error = await Assert.ThrowsAsync<UpdateFailedException>(() => stagedRun
                ? updater.PrepareStagedRunAsync(CoreRelease, CancellationToken.None)
                : updater.PrepareAsync(CoreRelease, null, CancellationToken.None));

            Assert.Equal(UpdateFailureStage.Install, error.Stage);
            Assert.Equal(Kernel32.ERROR_SHARING_VIOLATION,
                Assert.IsType<System.ComponentModel.Win32Exception>(error.InnerException).NativeErrorCode);
        }
        finally
        {
            writer?.Dispose();
        }
        // The download was deleted while the other process had it open sharing delete, so it
        // goes as that process lets go; the folder is then empty.
        UpdateStagingFolder.RemoveLeftovers(_stagingRoot, ThisAccountExpectation);
        Assert.Empty(Directory.EnumerateFileSystemEntries(_stagingRoot));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task The_holds_last_once_nothing_references_the_prepared_update(bool stagedRun)
    {
        var network = new ByName(new() { [AppCopy.CommandLineFileName] = Release(2) });
        // Undo is never called, so the file stays held for the rest of the test run, and the
        // folder it is in is left for the next run to delete.
        var path = await PrepareAndDrop(Updater(AppCopyKind.CommandLineDownload, network), stagedRun);
        TestTemp.LeaveForRunEnd(Path.GetDirectoryName(path)!);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        AssertHeld(path);
    }

    /// <summary>Prepares a run and answers only its file, keeping no reference to the update.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static async Task<string> PrepareAndDrop(AppUpdater updater, bool stagedRun) =>
        (await (stagedRun
            ? updater.PrepareStagedRunAsync(CoreRelease, CancellationToken.None)
            : updater.PrepareAsync(CoreRelease, null, CancellationToken.None))).FileToStart;

    [Fact]
    public async Task The_setups_copy_holds_its_setup_and_the_command_line_in_one_folder_setup_first()
    {
        var network = new ByName(new() { [SetupName] = Release(3), [AppCopy.CommandLineFileName] = Release(2) });

        var prepared = await Prepare(Updater(AppCopyKind.Setup, network));

        Assert.Equal([SetupName, AppCopy.CommandLineFileName], network.Asked);
        var folder = Assert.Single(Directory.EnumerateDirectories(_stagingRoot));
        Assert.Equal(Path.Combine(folder, AppCopy.CommandLineFileName), prepared.FileToStart);
        Assert.Equal(_exe, prepared.Replaces);
        Assert.Equal(Release(3), File.ReadAllBytes(Path.Combine(folder, SetupName)));
        AssertHeld(Path.Combine(folder, SetupName));
        AssertHeld(prepared.FileToStart);

        prepared.Undo();

        AssertNothingLeft();
    }

    [Fact]
    public async Task A_setup_not_carrying_its_version_fails_before_the_command_line_is_asked_for()
    {
        var network = new ByName(new() { [SetupName] = Program(3), [AppCopy.CommandLineFileName] = Release(2) });

        var error = await Assert.ThrowsAsync<UpdateFailedException>(() => Prepare(Updater(AppCopyKind.Setup, network)));

        Assert.Equal(UpdateFailureStage.Install, error.Stage);
        Assert.Equal([SetupName], network.Asked);
        AssertNothingLeft();
    }

    [Fact]
    public async Task A_command_line_that_fails_to_download_takes_the_setup_staged_before_it_away()
    {
        var network = new ByName(new() { [SetupName] = Release(3) });

        var error = await Assert.ThrowsAsync<UpdateFailedException>(() => Prepare(Updater(AppCopyKind.Setup, network)));

        Assert.Equal(UpdateFailureStage.Download, error.Stage);
        Assert.Equal([SetupName, AppCopy.CommandLineFileName], network.Asked);
        AssertNothingLeft();
    }

    [Theory]
    // The command line's own download is judged by its own entry.
    [InlineData(AppCopyKind.CommandLineDownload, """{"schema": 1, "downloads": {"cli": {"requires": ["something new"]}}}""", UpdateRefusal.CannotInstallFromThisCopy)]
    [InlineData(AppCopyKind.CommandLineDownload, """{"schema": 1, "downloads": {"cli": {"requires": ["netfx-4.8"]}}}""", UpdateRefusal.NeedsNetFramework48)]
    // The setup's copy is judged by the setup's entry and the command line's.
    [InlineData(AppCopyKind.Setup, """{"schema": 1, "downloads": {"setup": {"requires": []}, "cli": {"requires": ["something new"]}}}""", UpdateRefusal.CannotInstallFromThisCopy)]
    [InlineData(AppCopyKind.Setup, """{"schema": 1, "downloads": {"setup": {"requires": []}}}""", UpdateRefusal.CannotInstallFromThisCopy)]
    [InlineData(AppCopyKind.Setup, """{"schema": 1, "downloads": {"setup": {"requires": []}, "cli": {"requires": ["netfx-4.8"]}}}""", UpdateRefusal.NeedsNetFramework48)]
    [InlineData(AppCopyKind.Setup, """{"schema": 1, "downloads": {"setup": {"requires": ["netfx-4.8"]}, "cli": {"requires": []}}}""", UpdateRefusal.NeedsNetFramework48)]
    // Where one entry cannot be installed from this copy, installing .NET would not help.
    [InlineData(AppCopyKind.Setup, """{"schema": 1, "downloads": {"setup": {"requires": ["netfx-4.8"]}, "cli": {"requires": ["something new"]}}}""", UpdateRefusal.CannotInstallFromThisCopy)]
    [InlineData(AppCopyKind.Setup, """{"schema": 1, "downloads": {"setup": {"requires": ["something new"]}, "cli": {"requires": ["netfx-4.8"]}}}""", UpdateRefusal.CannotInstallFromThisCopy)]
    public async Task A_release_that_rules_this_copy_out_is_refused_before_anything_is_downloaded(
        AppCopyKind kind, string manifest, UpdateRefusal expected)
    {
        var network = new ByName(new() { [SetupName] = Release(3), [AppCopy.CommandLineFileName] = Release(2) }, manifest);

        var error = await Assert.ThrowsAsync<UpdateRefusedException>(() => Prepare(Updater(kind, network)));

        Assert.Equal(expected, error.Refusal);
        Assert.Empty(network.Asked);
        AssertNothingLeft();
    }

    [Theory]
    [InlineData(AppCopyKind.Portable)]
    [InlineData(AppCopyKind.Scoop)]
    [InlineData(AppCopyKind.FromSource)]
    [InlineData(AppCopyKind.Undetermined)]
    public async Task Any_other_kind_of_copy_has_no_command_line_to_run_from_a_download(AppCopyKind kind)
    {
        var network = new ByName(new() { [AppCopy.CommandLineFileName] = Release(2) });

        await Assert.ThrowsAsync<InvalidOperationException>(() => Prepare(Updater(kind, network)));

        Assert.Empty(network.Asked);
        AssertNothingLeft();
    }

    [Fact]
    public async Task The_start_up_clean_up_leaves_a_download_written_and_not_yet_held()
    {
        // The clean-up runs in this process between each download's write and its hold, the
        // moment nothing but the folder's hold keeps the file.
        var network = new ByName(new() { [SetupName] = Release(3), [AppCopy.CommandLineFileName] = Release(2) });
        var kept = new List<bool>();
        var updater = Updater(AppCopyKind.Setup, network, beforeHold: path =>
        {
            Assert.False(UpdateStagingFolder.RemoveLeftovers(_stagingRoot, ThisAccountExpectation));
            kept.Add(File.Exists(path));
        });

        var prepared = await Prepare(updater);

        Assert.Equal([true, true], kept);
        Assert.Equal(Release(2), File.ReadAllBytes(prepared.FileToStart));
    }

    [Fact]
    public void The_start_up_clean_up_takes_a_folder_left_under_a_root_that_is_a_junction()
    {
        // A %ProgramData% moved to another drive and reached through a junction. A directory
        // junction needs no privilege, so this always makes one and fails where it cannot.
        var linkedRoot = Path.Combine(_root, "programdata-link");
        var start = new ProcessStartInfo("cmd.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.Arguments = CommandLineArguments.Join(new[] { "/d", "/c", "mklink", "/J", linkedRoot, _stagingRoot });
        using (var mklink = Process.Start(start)!)
        {
            var output = mklink.StandardOutput.ReadToEnd() + mklink.StandardError.ReadToEnd();
            mklink.WaitForExit();
            Assert.True(mklink.ExitCode == 0, $"mklink /J failed: {output}");
        }
        Assert.True(new DirectoryInfo(linkedRoot).Attributes.HasFlag(FileAttributes.ReparsePoint));
        var made = UpdateStagingFolder.Create(linkedRoot, AdministratorsAndThisAccount);
        made.Handle.Dispose();
        File.WriteAllBytes(Path.Combine(made.FullPath, AppCopy.CommandLineFileName), Release(2));
        Assert.Single(Directory.EnumerateDirectories(_stagingRoot));

        Assert.True(UpdateStagingFolder.RemoveLeftovers(linkedRoot, ThisAccountExpectation));

        Assert.Empty(Directory.EnumerateFileSystemEntries(_stagingRoot));
        Assert.True(new DirectoryInfo(linkedRoot).Attributes.HasFlag(FileAttributes.ReparsePoint));
    }

    [Fact]
    public async Task The_start_up_clean_up_leaves_the_folder_a_prepared_update_holds()
    {
        var network = new ByName(new() { [SetupName] = Release(3), [AppCopy.CommandLineFileName] = Release(2) });
        var prepared = await Prepare(Updater(AppCopyKind.Setup, network));
        var folder = Path.GetDirectoryName(prepared.FileToStart)!;
        // The same two names written in a folder nothing holds, which the clean-up takes, so
        // what keeps the prepared ones is the folder's hold and not their names.
        var made = UpdateStagingFolder.Create(_stagingRoot, AdministratorsAndThisAccount);
        made.Handle.Dispose();
        var unheld = made.FullPath;
        File.WriteAllBytes(Path.Combine(unheld, SetupName), Release(3));
        File.WriteAllBytes(Path.Combine(unheld, AppCopy.CommandLineFileName), Release(2));

        UpdateStagingFolder.RemoveLeftovers(_stagingRoot, ThisAccountExpectation);

        Assert.False(Directory.Exists(unheld));
        Assert.Equal(Release(3), File.ReadAllBytes(Path.Combine(folder, SetupName)));
        Assert.Equal(Release(2), File.ReadAllBytes(prepared.FileToStart));
    }

    [Fact]
    public void A_program_held_open_to_read_sharing_only_reading_can_be_started_both_ways()
    {
        var folder = Directory.CreateDirectory(Path.Combine(_root, "held")).FullName;
        var exe = Path.Combine(folder, "stand-in.exe");
        File.Copy(DetachedStandIn.Exe, exe);
        using var hold = new FileStream(exe, FileMode.Open, FileAccess.Read, FileShare.Read);

        // The stand-in's "exit" ends with 1 once it has written its files.
        var attached = Directory.CreateDirectory(Path.Combine(folder, "attached")).FullName;
        var info = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true };
        info.Arguments = CommandLineArguments.Join(DetachedStandIn.Before(attached, "exit"));
        using (var process = Process.Start(info)!)
        {
            try
            {
                Assert.True(process.WaitForExit(60_000));
                Assert.Equal(1, process.ExitCode);
            }
            finally
            {
                try { if (!process.HasExited) { process.Kill(); process.WaitForExit(10_000); } } catch { }
            }
        }
        Assert.True(File.Exists(Path.Combine(attached, "pid.txt")));

        var detached = Directory.CreateDirectory(Path.Combine(folder, "detached")).FullName;
        using var started = DetachedProcess.Start(exe, DetachedStandIn.Before(detached, "exit"), folder);
        Assert.NotNull(started);
        using var ended = new ManualResetEvent(false) { SafeWaitHandle = new(started.DangerousGetHandle(), ownsHandle: false) };
        try
        {
            Assert.True(ended.WaitOne(60_000));
            Assert.True(Kernel32.GetExitCodeProcess(started, out var code));
            Assert.Equal(1u, code);
        }
        finally
        {
            if (!ended.WaitOne(0))
            {
                Kernel32.TerminateProcess(started.DangerousGetHandle(), 1);
                ended.WaitOne(10_000);
            }
        }
        Assert.True(File.Exists(Path.Combine(detached, "pid.txt")));
    }

#if DEBUG
    [Fact]
    public async Task In_a_debug_build_a_cancel_at_the_command_lines_gate_takes_away_the_setup_already_held()
    {
        var local = Path.Combine(_root, "local");
        Directory.CreateDirectory(local);
        var setup = Path.Combine(local, "setup.exe");
        var commandLine = Path.Combine(local, "cli.exe");
        File.WriteAllBytes(setup, Release(3));
        File.WriteAllBytes(commandLine, Release(2));
        var gate = Path.Combine(local, "gate");
        var names = new[] { AppUpdater.LocalSetupVariable, AppUpdater.LocalDownloadVariable, AppUpdater.DownloadGateVariable };
        var before = names.Select(Environment.GetEnvironmentVariable).ToArray();
        using var cancel = new CancellationTokenSource();
        Task? update = null;
        try
        {
            Environment.SetEnvironmentVariable(AppUpdater.LocalSetupVariable, setup);
            Environment.SetEnvironmentVariable(AppUpdater.LocalDownloadVariable, commandLine);
            Environment.SetEnvironmentVariable(AppUpdater.DownloadGateVariable, gate);
            var network = new ByName([]);

            update = Prepare(Updater(AppCopyKind.Setup, network), cancel.Token);
            var deadline = Stopwatch.StartNew();
            while (!File.Exists(gate + AppUpdater.GateReachedSuffix) && deadline.Elapsed < TimeSpan.FromSeconds(30))
                await Task.Delay(50);
            Assert.True(File.Exists(gate + AppUpdater.GateReachedSuffix));
            var staged = Assert.Single(Directory.EnumerateDirectories(_stagingRoot));
            AssertHeld(Path.Combine(staged, SetupName));
            cancel.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => update);
            Assert.Empty(network.Asked);
            AssertNothingLeft();
        }
        finally
        {
            // A test that failed before its cancel still ends the update held at the gate.
            cancel.Cancel();
            if (update is not null) try { await update; } catch { }
            for (var i = 0; i < names.Length; i++) Environment.SetEnvironmentVariable(names[i], before[i]);
        }
    }
#endif
}
