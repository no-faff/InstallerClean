using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;
using NSubstitute;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The parts of the update from inside the app that touch the disk: the folder the
/// download is written to, the leftovers a later start removes, and the whole of each
/// route through <see cref="AppUpdater"/>, against a temporary folder standing in for
/// %ProgramData%.
/// </summary>
public sealed class UpdateInPlaceTests : IDisposable
{
    private readonly string _root = TestTemp.NewFolder("ic-update-");

    private readonly IDisposable _crashLog = TestCrashLog.OwnFolder();

    /// <summary>The SID of the account running the suite.</summary>
    private static readonly string ThisAccountSid = WindowsIdentity.GetCurrent().User!.Value;

    /// <summary>
    /// The shipping list with the account running the suite added, owned by that account
    /// rather than Administrators. The suite runs without administrator rights, so it
    /// cannot set Administrators as the owner the way the shipping descriptor does, and
    /// could not write into the folder without its own entry.
    /// </summary>
    internal static readonly string AdministratorsAndThisAccount =
        $"O:{ThisAccountSid}D:P(A;OICI;FA;;;SY)(A;OICI;FA;;;BA)(A;OICI;FA;;;{ThisAccountSid})";

    /// <summary>
    /// What <see cref="UpdateStagingFolder.RemoveLeftovers(string, DirectoryGuard.FolderExpectation)"/>
    /// is told to accept for a folder made with <see cref="AdministratorsAndThisAccount"/>:
    /// this account alongside SYSTEM and Administrators.
    /// </summary>
    internal static readonly DirectoryGuard.FolderExpectation ThisAccountExpectation = new(
        new HashSet<string> { ThisAccountSid, DirectoryGuard.SystemSid, DirectoryGuard.AdministratorsSid },
        new HashSet<string> { ThisAccountSid, DirectoryGuard.SystemSid, DirectoryGuard.AdministratorsSid },
        requireProtected: true);

    public void Dispose()
    {
        try
        {
            // A folder made with the shipping descriptor keeps this account out, but its
            // creator owns it and may rewrite its list, which is how it is let back in to
            // be deleted.
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

    internal static byte[] Program(byte fill)
    {
        var bytes = new byte[2048];
        Array.Fill(bytes, fill);
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        return bytes;
    }

    /// <summary>The Core library's own file, which carries a version resource.</summary>
    private static readonly string CoreLibrary = typeof(FileVersionResource).Assembly.Location;

    /// <summary>
    /// The release the Core library's file version names, which a download built by
    /// <see cref="Release"/> carries, as the update check gives a version.
    /// </summary>
    internal static readonly string CoreRelease = FileVersionResource.Read(CoreLibrary) is { } v
        ? UpdateCheckService.NormaliseToBuild(v).ToString()
        : throw new InvalidOperationException("The Core library carries no file version.");

    /// <summary>
    /// A download that carries <see cref="CoreRelease"/>: the Core library's bytes with
    /// <paramref name="tag"/> appended, so two of them differ while the version resource
    /// both carry is the same.
    /// </summary>
    internal static byte[] Release(byte tag) => [.. File.ReadAllBytes(CoreLibrary), .. Enumerable.Repeat(tag, 16)];

    /// <summary>A registry that answers nothing, so .NET Framework 4.8 reads as not installed.</summary>
    internal static readonly IRegistryReader NoRegistry = NSubstitute.Substitute.For<IRegistryReader>();

    /// <summary>An update.json that lists every download with nothing required.</summary>
    internal const string AllowsEveryDownload =
        """{"schema": 1, "downloads": {"setup": {"requires": []}, "portable": {"requires": []}, "slim": {"requires": []}, "cli": {"requires": []}}}""";

    /// <summary>
    /// Answers every download with <paramref name="body"/>, and a release's update.json
    /// with <paramref name="manifest"/>, or 404 Not Found where it is null, as for a
    /// release that has none.
    /// </summary>
    private sealed class OneAnswer(byte[] body, string? manifest = AllowsEveryDownload) : HttpMessageHandler
    {
        /// <summary>How many download requests it has answered.</summary>
        public int Requests { get; private set; }

        /// <summary>How many update.json requests it has answered.</summary>
        public int ManifestRequests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/" + UpdateManifest.FileName, StringComparison.Ordinal))
            {
                ManifestRequests++;
                return Task.FromResult(manifest is null
                    ? new HttpResponseMessage(HttpStatusCode.NotFound)
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(manifest) });
            }
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) });
        }
    }

    [AdministratorFact]
    public void The_staging_folder_is_new_each_time_owned_by_Administrators_and_only_they_and_SYSTEM_hold_any_right()
    {
        // The shipping descriptor itself, which sets Administrators as the owner. Only a
        // process with administrator rights can assign that owner, which is why this runs
        // where the suite holds them.
        var first = UpdateStagingFolder.Create(_root);
        var second = UpdateStagingFolder.Create(_root);
        first.Handle.Dispose();
        second.Handle.Dispose();

        Assert.NotEqual(first.FullPath, second.FullPath);
        Assert.StartsWith(UpdateStagingFolder.NamePrefix, Path.GetFileName(first.FullPath));

        var security = new DirectoryInfo(first.FullPath).GetAccessControl();
        Assert.True(security.AreAccessRulesProtected);
        var administrators = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        Assert.Equal(administrators, security.GetOwner(typeof(SecurityIdentifier)));
        var holders = security.GetAccessRules(true, true, typeof(SecurityIdentifier))
            .Cast<FileSystemAccessRule>()
            .Select(rule => (SecurityIdentifier)rule.IdentityReference)
            .Distinct()
            .ToHashSet();
        var expected = new HashSet<SecurityIdentifier>
        {
            new(WellKnownSidType.LocalSystemSid, null),
            administrators,
        };
        Assert.True(holders.SetEquals(expected), string.Join(", ", holders));
    }

    [Fact]
    public void The_staging_folder_is_not_made_where_the_root_is_missing()
    {
        Assert.Throws<IOException>(() => UpdateStagingFolder.Create(Path.Combine(_root, "missing")));
    }

    [Fact]
    public void Leftovers_in_the_apps_own_folders_go_and_nothing_else_does()
    {
        // A folder the app made, holding a download, left by a process that has ended:
        // cleaned, and the folder with it.
        var ours = Left(UpdateStagingFolder.Create(_root, AdministratorsAndThisAccount));
        File.WriteAllBytes(Path.Combine(ours, "InstallerClean-3.5.0-setup.exe"), Program(1));

        // The app's own folder, but a file that is not a download keeps it: the download
        // goes, the other file and the folder stay.
        var oursWithOtherFile = Left(UpdateStagingFolder.Create(_root, AdministratorsAndThisAccount));
        File.WriteAllBytes(Path.Combine(oursWithOtherFile, "InstallerClean-3.5.0-setup.exe"), Program(1));
        File.WriteAllText(Path.Combine(oursWithOtherFile, "notes.txt"), "kept");

        // A folder whose name is not the download shape, left alone.
        var notOurName = Path.Combine(_root, UpdateStagingFolder.NamePrefix + "something-else");
        Directory.CreateDirectory(notOurName);
        File.WriteAllBytes(Path.Combine(notOurName, "InstallerClean-3.5.0-setup.exe"), Program(2));

        var allGone = UpdateStagingFolder.RemoveLeftovers(_root, ThisAccountExpectation);

        Assert.False(allGone);
        Assert.False(Directory.Exists(ours));
        Assert.False(File.Exists(Path.Combine(oursWithOtherFile, "InstallerClean-3.5.0-setup.exe")));
        Assert.True(File.Exists(Path.Combine(oursWithOtherFile, "notes.txt")));
        Assert.True(File.Exists(Path.Combine(notOurName, "InstallerClean-3.5.0-setup.exe")));
    }

    [Fact]
    public void A_folder_an_update_still_holds_is_left_whole_and_counted_as_not_gone()
    {
        // Held as an update in another process holds it: its download goes nowhere, whatever
        // stage it is at, and the clean-up says a folder of the app's is left.
        var held = UpdateStagingFolder.Create(_root, AdministratorsAndThisAccount);
        var download = Path.Combine(held.FullPath, "InstallerClean-3.5.0-setup.exe");
        try
        {
            File.WriteAllBytes(download, Program(1));

            Assert.False(UpdateStagingFolder.RemoveLeftovers(_root, ThisAccountExpectation));
            Assert.True(File.Exists(download));
        }
        finally
        {
            held.Handle.Dispose();
        }

        // Once the process holding it has ended, the next pass takes it.
        Assert.True(UpdateStagingFolder.RemoveLeftovers(_root, ThisAccountExpectation));
        Assert.False(Directory.Exists(held.FullPath));
    }

    [Fact]
    public void A_held_folder_another_account_made_is_left_and_does_not_keep_the_clean_up_retrying()
    {
        // The download name, held without delete sharing, but made as an ordinary folder: not
        // one the app is waiting on.
        var foreign = Path.Combine(_root, UpdateStagingFolder.NamePrefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(foreign);
        using (var holder = Kernel32.CreateFile(foreign, Kernel32.GENERIC_READ, Kernel32.FILE_SHARE_READ, IntPtr.Zero,
            Kernel32.OPEN_EXISTING, Kernel32.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero))
        {
            Assert.False(holder.IsInvalid);
            Assert.True(UpdateStagingFolder.RemoveLeftovers(_root));
            Assert.True(Directory.Exists(foreign));
        }
    }

    [Fact]
    public void A_held_staging_folder_cannot_be_removed_or_renamed_by_its_path()
    {
        var held = UpdateStagingFolder.Create(_root, AdministratorsAndThisAccount);
        try
        {
            Assert.ThrowsAny<IOException>(() => Directory.Delete(held.FullPath));
            Assert.ThrowsAny<IOException>(() => Directory.Move(held.FullPath, held.FullPath + "-moved"));
            Assert.True(Directory.Exists(held.FullPath));
        }
        finally
        {
            held.Handle.Dispose();
        }
    }

    [Fact]
    public void A_program_starts_with_a_held_staging_folder_as_its_working_folder()
    {
        // Every hand-over starts the setup or the newer version from the staging folder while
        // this process still holds it.
        var held = UpdateStagingFolder.Create(_root, AdministratorsAndThisAccount);
        try
        {
            var info = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/d /c cd")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                WorkingDirectory = held.FullPath,
            };
            using var started = Process.Start(info)!;
            var output = started.StandardOutput.ReadToEnd().Trim();
            Assert.True(started.WaitForExit(30_000));

            Assert.Equal(0, started.ExitCode);
            Assert.Equal(held.FullPath, output, ignoreCase: true);
        }
        finally
        {
            held.Handle.Dispose();
        }
    }

    [Fact]
    public void Removing_a_staging_folder_takes_the_files_named_and_then_the_folder()
    {
        var folder = UpdateStagingFolder.Create(_root, AdministratorsAndThisAccount);
        File.WriteAllBytes(Path.Combine(folder.FullPath, "InstallerClean-3.5.0-setup.exe"), Program(1));
        File.WriteAllBytes(Path.Combine(folder.FullPath, "installerclean-cli.exe"), Program(2));

        Assert.True(folder.TryRemove(["InstallerClean-3.5.0-setup.exe", "installerclean-cli.exe"]));

        Assert.False(Directory.Exists(folder.FullPath));
        Assert.True(folder.Handle.IsClosed);
        // Asked again, it reaches nothing.
        Assert.True(folder.TryRemove(["InstallerClean-3.5.0-setup.exe"]));
    }

    [Fact]
    public void A_staging_folder_with_anything_else_inside_stays_and_a_later_removal_takes_it()
    {
        var folder = UpdateStagingFolder.Create(_root, AdministratorsAndThisAccount);
        var other = Path.Combine(folder.FullPath, "notes.txt");
        File.WriteAllBytes(Path.Combine(folder.FullPath, "InstallerClean-3.5.0-setup.exe"), Program(1));
        File.WriteAllText(other, "kept");

        Assert.False(folder.TryRemove(["InstallerClean-3.5.0-setup.exe"]));
        Assert.False(File.Exists(Path.Combine(folder.FullPath, "InstallerClean-3.5.0-setup.exe")));
        Assert.True(File.Exists(other));

        File.Delete(other);
        Assert.True(folder.TryRemove(["InstallerClean-3.5.0-setup.exe"]));
        Assert.False(Directory.Exists(folder.FullPath));
    }

    [Fact]
    public void A_folder_put_in_the_staging_folders_place_is_never_acted_on()
    {
        var folder = UpdateStagingFolder.Create(_root, AdministratorsAndThisAccount);
        folder.Handle.Dispose();
        Directory.Delete(folder.FullPath);
        Directory.CreateDirectory(folder.FullPath);
        var planted = Path.Combine(folder.FullPath, "InstallerClean-3.5.0-setup.exe");
        File.WriteAllBytes(planted, Program(1));

        // Its own folder has gone, so there is nothing of it left to remove.
        Assert.True(folder.TryRemove(["InstallerClean-3.5.0-setup.exe"]));
        Assert.True(File.Exists(planted));
    }

    /// <summary>
    /// A staging folder as a process that has ended leaves it: made, and its hold let go.
    /// Answers its path.
    /// </summary>
    private static string Left(UpdateStagingFolder folder)
    {
        folder.Handle.Dispose();
        return folder.FullPath;
    }

    [Fact]
    public void A_folder_another_account_could_have_made_is_left_untouched()
    {
        // A folder with the download name and a file of the download shape inside it,
        // but made as an ordinary folder: its owner and list are not the app's own, so
        // the real check leaves it and the file whole. This is the account another user
        // on the machine would act as.
        var foreign = Path.Combine(_root, UpdateStagingFolder.NamePrefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(foreign);
        var planted = Path.Combine(foreign, "InstallerClean-anything.exe");
        File.WriteAllBytes(planted, Program(1));

        var allGone = UpdateStagingFolder.RemoveLeftovers(_root);

        // A folder that is not the app's own is not one the app is waiting on, so it does
        // not keep the clean-up retrying.
        Assert.True(allGone);
        Assert.True(Directory.Exists(foreign));
        Assert.True(File.Exists(planted));
    }

    [Fact]
    public void A_link_in_place_of_a_folder_is_left_and_its_target_is_untouched()
    {
        // A download-named candidate that is a link: the clean-up opens it without
        // following the link, finds a reparse point and leaves it, so a file reached
        // through the link is untouched. Best-effort because creating a link needs a
        // privilege not every host grants.
        var target = Directory.CreateDirectory(Path.Combine(_root, "target")).FullName;
        var decoy = Path.Combine(target, "keep.txt");
        File.WriteAllText(decoy, "untouched");

        var link = Path.Combine(_root, UpdateStagingFolder.NamePrefix + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return; // this host cannot create links; the refusal path is exercised where it can
        }

        var allGone = UpdateStagingFolder.RemoveLeftovers(_root, ThisAccountExpectation);

        // A link is not a folder the app made, so it is left and does not keep the
        // clean-up retrying.
        Assert.True(allGone);
        Assert.True(File.Exists(decoy));
    }

    [Fact]
    public async Task The_portable_route_keeps_the_download_in_its_staging_folder_to_put_at_the_exe_path()
    {
        var exeFolder = Directory.CreateDirectory(Path.Combine(_root, "app")).FullName;
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(exeFolder, "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        var updater = new AppUpdater(AppCopyKind.Portable, exe,
            new UpdateDownloadService(new OneAnswer(Release(2)), null), stagingRoot,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, NoRegistry);

        var prepared = await updater.PrepareAsync(CoreRelease, null, CancellationToken.None);

        Assert.Equal(AppCopy.DownloadFileName(AppCopyKind.Portable, CoreRelease), Path.GetFileName(prepared.FileToStart));
        Assert.StartsWith(stagingRoot, prepared.FileToStart);
        Assert.Equal(exe, prepared.Replaces);
        Assert.Equal(string.Empty, prepared.Arguments);
        Assert.Equal(Release(2), File.ReadAllBytes(prepared.FileToStart));
        Assert.Equal(Program(1), File.ReadAllBytes(exe));
        Assert.Equal(["InstallerClean.exe"], Directory.EnumerateFileSystemEntries(exeFolder).Select(Path.GetFileName));

        prepared.Undo();

        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
        Assert.Equal(Program(1), File.ReadAllBytes(exe));
    }

    [Fact]
    public async Task A_prepared_update_keeps_its_download_from_the_clean_up_until_its_undo_removes_it()
    {
        var exeFolder = Directory.CreateDirectory(Path.Combine(_root, "app")).FullName;
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(exeFolder, "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        var updater = new AppUpdater(AppCopyKind.Portable, exe,
            new UpdateDownloadService(new OneAnswer(Release(2)), null), stagingRoot,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, NoRegistry);

        var prepared = await updater.PrepareAsync(CoreRelease, null, CancellationToken.None);
        Assert.False(UpdateStagingFolder.RemoveLeftovers(stagingRoot, ThisAccountExpectation));
        Assert.True(File.Exists(prepared.FileToStart));

        prepared.Undo();

        Assert.False(File.Exists(prepared.FileToStart));
        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Fact]
    public async Task A_prepared_setup_keeps_its_download_from_the_clean_up_until_its_undo_deletes_it()
    {
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(_root, "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        var updater = new AppUpdater(AppCopyKind.Setup, exe,
            new UpdateDownloadService(new OneAnswer(Release(3)), null), stagingRoot,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, NoRegistry);

        var prepared = await updater.PrepareAsync(CoreRelease, null, CancellationToken.None);
        Assert.False(UpdateStagingFolder.RemoveLeftovers(stagingRoot, ThisAccountExpectation));
        Assert.True(File.Exists(prepared.FileToStart));

        prepared.Undo();

        Assert.False(File.Exists(prepared.FileToStart));
        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Fact]
    public async Task The_setup_route_starts_the_downloaded_setup_with_the_apps_switch()
    {
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(_root, "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        var updater = new AppUpdater(AppCopyKind.Setup, exe,
            new UpdateDownloadService(new OneAnswer(Release(3)), null), stagingRoot,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, NoRegistry);

        var prepared = await updater.PrepareAsync(CoreRelease, null, CancellationToken.None);

        Assert.Equal($"InstallerClean-{CoreRelease}-setup.exe", Path.GetFileName(prepared.FileToStart));
        Assert.StartsWith(stagingRoot, prepared.FileToStart);
        Assert.Contains("/UPDATEFROMAPP", prepared.Arguments);
        Assert.Contains("/SILENT", prepared.Arguments);
        Assert.Equal(Release(3), File.ReadAllBytes(prepared.FileToStart));
        Assert.Equal(Program(1), File.ReadAllBytes(exe));

        prepared.Undo();

        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Fact]
    public async Task The_setup_route_starts_the_downloaded_setup_with_the_arguments_it_was_given()
    {
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(_root, "installerclean-cli.exe");
        File.WriteAllBytes(exe, Program(1));
        var updater = new AppUpdater(AppCopyKind.Setup, exe,
            new UpdateDownloadService(new OneAnswer(Release(3)), null), stagingRoot,
            CommandLineUpdate.SetupArguments, AdministratorsAndThisAccount, NoRegistry);

        var prepared = await updater.PrepareAsync(CoreRelease, null, CancellationToken.None);

        Assert.Equal(CommandLineUpdate.SetupArguments, prepared.Arguments);
        Assert.DoesNotContain("/UPDATEFROMAPP", prepared.Arguments);

        prepared.Undo();
    }

    /// <summary>An updater for the command line's own download in its own folder, the exe it updates and the staging root.</summary>
    private (AppUpdater Updater, string Exe, string StagingRoot) CommandLineDownloadUpdater(
        OneAnswer network)
    {
        var exeFolder = Directory.CreateDirectory(Path.Combine(_root, "tools")).FullName;
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(exeFolder, AppCopy.CommandLineFileName);
        File.WriteAllBytes(exe, Program(1));
        var updater = new AppUpdater(AppCopyKind.CommandLineDownload, exe,
            new UpdateDownloadService(network, null), stagingRoot,
            CommandLineUpdate.SetupArguments, AdministratorsAndThisAccount, NoRegistry);
        return (updater, exe, stagingRoot);
    }

#if DEBUG
    /// <summary>
    /// Runs <paramref name="test"/> with the local download switch set to
    /// <paramref name="path"/> and the local setup switch cleared, and puts both back as
    /// they were.
    /// </summary>
    private static async Task WithLocalDownload(string? path, Func<Task> test)
    {
        var download = Environment.GetEnvironmentVariable(AppUpdater.LocalDownloadVariable);
        var setup = Environment.GetEnvironmentVariable(AppUpdater.LocalSetupVariable);
        try
        {
            Environment.SetEnvironmentVariable(AppUpdater.LocalSetupVariable, null);
            Environment.SetEnvironmentVariable(AppUpdater.LocalDownloadVariable, path);
            await test();
        }
        finally
        {
            Environment.SetEnvironmentVariable(AppUpdater.LocalDownloadVariable, download);
            Environment.SetEnvironmentVariable(AppUpdater.LocalSetupVariable, setup);
        }
    }

    [Fact]
    public async Task In_a_debug_build_the_local_download_switch_stages_that_file_in_place_of_a_download()
    {
        var network = new OneAnswer(Release(2));
        var (updater, exe, stagingRoot) = CommandLineDownloadUpdater(network);
        var local = Path.Combine(_root, "local-newer.exe");
        File.WriteAllBytes(local, Release(4));

        await WithLocalDownload(local, async () =>
        {
            var prepared = await updater.PrepareAsync(CoreRelease, null, CancellationToken.None);

            Assert.Equal(0, network.Requests);
            Assert.Equal(Release(4), File.ReadAllBytes(prepared.FileToStart));
            Assert.StartsWith(stagingRoot, prepared.FileToStart);
            Assert.Equal(Release(4), File.ReadAllBytes(local));
            prepared.Undo();

            // The control: the same update with the switch cleared downloads.
            Environment.SetEnvironmentVariable(AppUpdater.LocalDownloadVariable, null);
            prepared = await updater.PrepareAsync(CoreRelease, null, CancellationToken.None);

            Assert.Equal(1, network.Requests);
            Assert.Equal(Release(2), File.ReadAllBytes(prepared.FileToStart));
            prepared.Undo();
        });
    }

    [Theory]
    [InlineData("not a program")]
    [InlineData("a relative path")]
    [InlineData("a missing file")]
    public async Task In_a_debug_build_a_local_download_the_download_would_refuse_fails_as_a_download(string which)
    {
        var network = new OneAnswer(Program(2));
        var (updater, exe, stagingRoot) = CommandLineDownloadUpdater(network);
        var local = Path.Combine(_root, "local-newer.exe");
        File.WriteAllBytes(local, which == "not a program" ? "PK not a program"u8.ToArray() : Program(4));
        var path = which switch
        {
            "a relative path" => Path.GetFileName(local),
            "a missing file" => local + ".missing",
            _ => local,
        };

        await WithLocalDownload(path, async () =>
        {
            var failure = await Assert.ThrowsAsync<UpdateFailedException>(
                () => updater.PrepareAsync("3.5.0", null, CancellationToken.None));

            Assert.Equal(UpdateFailureStage.Download, failure.Stage);
            Assert.Equal(0, network.Requests);
            NothingBeside(exe);
            Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
        });
    }

    [Fact]
    public async Task In_a_debug_build_the_local_download_switch_leaves_the_setup_route_to_download()
    {
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(_root, "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        var local = Path.Combine(_root, "local-newer.exe");
        File.WriteAllBytes(local, Release(4));
        var network = new OneAnswer(Release(3));
        var updater = new AppUpdater(AppCopyKind.Setup, exe,
            new UpdateDownloadService(network, null), stagingRoot,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, NoRegistry);

        await WithLocalDownload(local, async () =>
        {
            var prepared = await updater.PrepareAsync(CoreRelease, null, CancellationToken.None);

            Assert.Equal(1, network.Requests);
            Assert.Equal(Release(3), File.ReadAllBytes(prepared.FileToStart));
            prepared.Undo();
        });
    }
#endif

    [Theory]
    [InlineData(AppCopyKind.Scoop)]
    [InlineData(AppCopyKind.FromSource)]
    public async Task A_copy_with_no_download_is_refused_before_anything_is_made(AppCopyKind kind)
    {
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(_root, "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        var updater = new AppUpdater(kind, exe,
            new UpdateDownloadService(new OneAnswer(Program(2)), null), stagingRoot,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, NoRegistry);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => updater.PrepareAsync("3.5.0", null, CancellationToken.None));

        Assert.False(AppCopy.CanUpdateItself(updater.Kind));
        Assert.Equal(Program(1), File.ReadAllBytes(exe));
        NothingBeside(exe);
        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Fact]
    public async Task A_failed_download_changes_nothing_and_leaves_no_staging_folder()
    {
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(_root, "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        var updater = new AppUpdater(AppCopyKind.Portable, exe,
            new UpdateDownloadService(new OneAnswer("not a program"u8.ToArray()), null), stagingRoot,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, NoRegistry);

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => updater.PrepareAsync("3.5.0", null, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        Assert.Equal(Program(1), File.ReadAllBytes(exe));
            NothingBeside(exe);
            Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Theory]
    [InlineData(AppCopyKind.Setup)]
    [InlineData(AppCopyKind.Portable)]
    [InlineData(AppCopyKind.CommandLineDownload)]
    public async Task A_release_that_rules_this_copy_out_is_refused_before_anything_is_downloaded(AppCopyKind kind)
    {
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(_root, kind == AppCopyKind.CommandLineDownload ? AppCopy.CommandLineFileName : "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        var key = UpdateManifest.DownloadKey(kind);
        var network = new OneAnswer(Release(2),
            "{\"schema\": 1, \"downloads\": {\"" + key + "\": {\"requires\": [\"netfx-4.8\"]}}}");
        var updater = new AppUpdater(kind, exe, new UpdateDownloadService(network, null), stagingRoot,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, NoRegistry);

        Assert.Equal(UpdateRefusal.NeedsNetFramework48, await updater.CheckRequirementsAsync(CoreRelease, null, CancellationToken.None));
        var refused = await Assert.ThrowsAsync<UpdateRefusedException>(
            () => updater.PrepareAsync(CoreRelease, null, CancellationToken.None));

        Assert.Equal(UpdateRefusal.NeedsNetFramework48, refused.Refusal);
        Assert.Equal(1, network.ManifestRequests);
        Assert.Equal(0, network.Requests);
        Assert.Equal(Program(1), File.ReadAllBytes(exe));
        NothingBeside(exe);
        Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    [Fact]
    public async Task The_releases_file_is_read_once_and_the_PC_is_asked_again_each_time()
    {
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(_root, "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        var network = new OneAnswer(Release(2),
            "{\"schema\": 1, \"downloads\": {\"portable\": {\"requires\": [\"netfx-4.8\"]}}}");
        var registry = NSubstitute.Substitute.For<IRegistryReader>();
        var updater = new AppUpdater(AppCopyKind.Portable, exe, new UpdateDownloadService(network, null), stagingRoot,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, registry);

        Assert.Equal(UpdateRefusal.NeedsNetFramework48, await updater.CheckRequirementsAsync(CoreRelease, null, CancellationToken.None));
        registry.LocalMachineDwordValue(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new RegistryDwordRead(RegistryDwordState.Read, 528040));
        var prepared = await updater.PrepareAsync(CoreRelease, null, CancellationToken.None);

        Assert.Equal(1, network.ManifestRequests);
        Assert.Equal(1, network.Requests);
        prepared.Undo();
    }

    [Fact]
    public async Task A_release_above_the_last_without_a_file_that_has_none_is_refused()
    {
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(_root, "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        var network = new OneAnswer(Release(2), manifest: null);
        var updater = new AppUpdater(AppCopyKind.Portable, exe, new UpdateDownloadService(network, null), stagingRoot,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, NoRegistry);
        var above = $"{UpdateManifest.LastVersionWithoutManifest.Major}.{UpdateManifest.LastVersionWithoutManifest.Minor + 1}.0";
        var atOrBelow = UpdateManifest.LastVersionWithoutManifest.ToString(3);

        Assert.Equal(UpdateRefusal.CannotInstallFromThisCopy, await updater.CheckRequirementsAsync(above, null, CancellationToken.None));
        Assert.Null(await updater.CheckRequirementsAsync(atOrBelow, null, CancellationToken.None));
        Assert.Equal(2, network.ManifestRequests);
    }

    [Fact]
    public async Task A_release_with_no_file_is_asked_again_each_time()
    {
        var exe = Path.Combine(_root, "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        var network = new OneAnswer(Release(2), manifest: null);
        var updater = new AppUpdater(AppCopyKind.Portable, exe, new UpdateDownloadService(network, null), _root,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, NoRegistry);

        await updater.CheckRequirementsAsync("9.9.9", null, CancellationToken.None);
        await updater.CheckRequirementsAsync("9.9.9", null, CancellationToken.None);

        Assert.Equal(2, network.ManifestRequests);
    }

    [Fact]
    public async Task A_body_that_is_not_JSON_fails_as_a_read_and_is_not_kept()
    {
        var exe = Path.Combine(_root, "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        var network = new OneAnswer(Release(2), "<html>Sign in to the network</html>");
        var updater = new AppUpdater(AppCopyKind.Portable, exe, new UpdateDownloadService(network, null), _root,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, NoRegistry);

        for (var i = 0; i < 2; i++)
        {
            var failure = await Assert.ThrowsAsync<UpdateFailedException>(
                () => updater.CheckRequirementsAsync(CoreRelease, null, CancellationToken.None));
            Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        }
        Assert.Equal(2, network.ManifestRequests);
    }

    [Theory]
    [InlineData(AppCopyKind.Setup, true)]
    [InlineData(AppCopyKind.Setup, false)]
    [InlineData(AppCopyKind.Portable, true)]
    [InlineData(AppCopyKind.Portable, false)]
    [InlineData(AppCopyKind.CommandLineDownload, true)]
    [InlineData(AppCopyKind.CommandLineDownload, false)]
    public async Task A_download_that_does_not_carry_the_version_asked_for_is_deleted_and_nothing_is_written_beside_the_exe(
        AppCopyKind kind, bool carriesAnotherVersion)
    {
        var stagingRoot = Directory.CreateDirectory(Path.Combine(_root, "programdata")).FullName;
        var exe = Path.Combine(_root, kind == AppCopyKind.CommandLineDownload ? AppCopy.CommandLineFileName : "InstallerClean.exe");
        File.WriteAllBytes(exe, Program(1));
        // Either the Core library's bytes asked for as the release after the one they carry,
        // or bytes that start as a program and carry no version at all.
        var core = Version.Parse(CoreRelease);
        var asked = $"{core.Major}.{core.Minor + 1}.0";
        var updater = new AppUpdater(kind, exe,
            new UpdateDownloadService(new OneAnswer(carriesAnotherVersion ? Release(2) : Program(2)), null), stagingRoot,
            AppUpdater.WindowSetupArguments, AdministratorsAndThisAccount, NoRegistry);

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => updater.PrepareAsync(asked, null, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Install, failure.Stage);
        Assert.Equal(Program(1), File.ReadAllBytes(exe));
            NothingBeside(exe);
            Assert.Empty(Directory.EnumerateFileSystemEntries(stagingRoot));
    }

    /// <summary>
    /// Asserts that nothing is beside <paramref name="exe"/> under its name with more on the
    /// end, as a file written beside it or a name a replace leaves would be.
    /// </summary>
    private static void NothingBeside(string exe)
    {
        var name = Path.GetFileName(exe) + ".";
        Assert.DoesNotContain(Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(exe)!),
            entry => Path.GetFileName(entry).StartsWith(name, StringComparison.OrdinalIgnoreCase));
    }
}
