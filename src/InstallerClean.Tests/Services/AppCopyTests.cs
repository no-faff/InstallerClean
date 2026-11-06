using Microsoft.Win32;
using NSubstitute;
using InstallerClean.Helpers;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Which kind of copy is running, decided from facts handed in, and the download
/// address each kind updates from.
/// </summary>
public class AppCopyTests
{
    private const string ProgramFilesFolder = @"C:\Program Files\InstallerClean";
    private const string ScoopFolder = @"C:\Users\someone\scoop\apps\installerclean\3.4.0";
    private const string DownloadsFolder = @"C:\Users\someone\Downloads";

    private static Func<string, bool> FilesPresent(params string[] paths)
    {
        var present = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);
        return present.Contains;
    }

    private static readonly Func<string, bool> NoFiles = _ => false;

    /// <summary>
    /// The setup's record as <see cref="AppCopy.ReadSetupRecord"/> reads a key holding
    /// <paramref name="installLocation"/>, or an absent key where it is null.
    /// </summary>
    private static SetupRecord RecordOf(string? installLocation) =>
        AppCopy.ReadSetupRecord(Registry(installLocation is null
            ? new RegistryKeyValues(RegistryKeyPresence.Absent)
            : new RegistryKeyValues(RegistryKeyPresence.Present,
                [new RegistryValue("InstallLocation", RegistryValueKind.String, installLocation)])));

    private static IRegistryReader Registry(RegistryKeyValues read)
    {
        var registry = Substitute.For<IRegistryReader>();
        registry.LocalMachineValues(AppCopy.SetupUninstallKeyPath).Returns(read);
        return registry;
    }

    [Theory]
    [InlineData("names this folder")]
    [InlineData("none")]
    [InlineData("cannot tell")]
    public void A_build_that_is_not_a_single_file_is_from_source_whatever_else_holds(string which)
    {
        var record = which switch
        {
            "names this folder" => RecordOf(ProgramFilesFolder + @"\"),
            "none" => SetupRecord.None,
            _ => SetupRecord.CannotTell,
        };

        Assert.Equal(AppCopyKind.FromSource,
            AppCopy.Decide(isSingleFile: false, isSelfContained: true, ProgramFilesFolder, record, NoFiles));
        Assert.Equal(AppCopyKind.FromSource,
            AppCopy.DecideCommandLine(isSingleFile: false, ProgramFilesFolder, record));
    }

    [Theory]
    [InlineData(@"C:\Program Files\InstallerClean\")]
    [InlineData(@"C:\Program Files\InstallerClean")]
    [InlineData(@"c:\program files\installerclean\")]
    public void A_copy_in_the_folder_the_setup_recorded_is_the_setups(string installLocation)
    {
        var kind = AppCopy.Decide(
            isSingleFile: true, isSelfContained: true, ProgramFilesFolder, RecordOf(installLocation), NoFiles);

        Assert.Equal(AppCopyKind.Setup, kind);
    }

    [Fact]
    public void The_portable_download_beside_a_setup_install_elsewhere_is_the_portable()
    {
        // The setup installs the very file the portable download is, so only the
        // folder tells them apart.
        var kind = AppCopy.Decide(
            isSingleFile: true, isSelfContained: true, DownloadsFolder,
            RecordOf(ProgramFilesFolder + @"\"), NoFiles);

        Assert.Equal(AppCopyKind.Portable, kind);
    }

    [Theory]
    [InlineData(ProgramFilesFolder)]
    [InlineData(DownloadsFolder)]
    public void A_copy_on_a_PC_whose_setup_record_cannot_tell_is_undetermined(string exeFolder)
    {
        foreach (var selfContained in new[] { true, false })
            Assert.Equal(AppCopyKind.Undetermined, AppCopy.Decide(
                isSingleFile: true, selfContained, exeFolder, SetupRecord.CannotTell, NoFiles));
    }

    [Fact]
    public void A_copy_beside_Scoops_files_is_Scoops_where_the_setup_record_cannot_tell()
    {
        var kind = AppCopy.Decide(
            isSingleFile: true, isSelfContained: true, ScoopFolder, SetupRecord.CannotTell,
            FilesPresent(Path.Combine(ScoopFolder, "scoop-manifest.json"), Path.Combine(ScoopFolder, "scoop-install.json")));

        Assert.Equal(AppCopyKind.Scoop, kind);
    }

    [Theory]
    [InlineData(ProgramFilesFolder)]
    [InlineData(DownloadsFolder)]
    public void A_command_line_on_a_PC_whose_setup_record_cannot_tell_is_undetermined(string exeFolder) =>
        Assert.Equal(AppCopyKind.Undetermined,
            AppCopy.DecideCommandLine(isSingleFile: true, exeFolder, SetupRecord.CannotTell));


    [Theory]
    [InlineData("scoop-manifest.json", "scoop-install.json")]
    [InlineData("manifest.json", "install.json")]
    public void A_copy_beside_both_of_Scoops_files_is_Scoops(string manifest, string install)
    {
        var kind = AppCopy.Decide(
            isSingleFile: true, isSelfContained: true, ScoopFolder, RecordOf(null),
            FilesPresent(Path.Combine(ScoopFolder, manifest), Path.Combine(ScoopFolder, install)));

        Assert.Equal(AppCopyKind.Scoop, kind);
    }

    [Theory]
    [InlineData("scoop-manifest.json")]
    [InlineData("install.json")]
    public void One_of_Scoops_files_alone_does_not_make_a_Scoop_copy(string only)
    {
        var kind = AppCopy.Decide(
            isSingleFile: true, isSelfContained: true, ScoopFolder, RecordOf(null),
            FilesPresent(Path.Combine(ScoopFolder, only)));

        Assert.Equal(AppCopyKind.Portable, kind);
    }

    [Theory]
    [InlineData(true, AppCopyKind.Portable)]
    [InlineData(false, AppCopyKind.Slim)]
    public void Otherwise_the_build_tells_the_portable_from_the_slim(bool selfContained, AppCopyKind expected)
    {
        var kind = AppCopy.Decide(
            isSingleFile: true, selfContained, DownloadsFolder, RecordOf(null), NoFiles);

        Assert.Equal(expected, kind);
    }

    [Theory]
    [InlineData(AppCopyKind.Setup, "https://github.com/no-faff/InstallerClean/releases/download/v3.5.0/InstallerClean-3.5.0-setup.exe")]
    [InlineData(AppCopyKind.Portable, "https://github.com/no-faff/InstallerClean/releases/download/v3.5.0/InstallerClean-portable.exe")]
    [InlineData(AppCopyKind.Slim, "https://github.com/no-faff/InstallerClean/releases/download/v3.5.0/InstallerClean-slim.exe")]
    [InlineData(AppCopyKind.CommandLineDownload, "https://github.com/no-faff/InstallerClean/releases/download/v3.5.0/installerclean-cli.exe")]
    public void Each_kind_downloads_its_own_file_from_the_releases_tag(AppCopyKind kind, string expected)
    {
        Assert.Equal(new Uri(expected), AppCopy.DownloadAddress(kind, "3.5.0"));
        Assert.True(AppCopy.CanUpdateItself(kind));
    }

    [Fact]
    public void The_command_lines_own_download_updates_from_the_unversioned_file_in_the_releases_tag()
    {
        Assert.Equal(
            new Uri("https://github.com/no-faff/InstallerClean/releases/download/v3.5.0/installerclean-cli.exe"),
            AppCopy.DownloadAddress(AppCopyKind.CommandLineDownload, "3.5.0"));
        Assert.True(AppCopy.CanUpdateItself(AppCopyKind.CommandLineDownload));
    }

    [Fact]
    public void Only_the_setups_file_name_carries_the_version()
    {
        Assert.Equal("InstallerClean-3.5.0-setup.exe", AppCopy.DownloadFileName(AppCopyKind.Setup, "3.5.0"));
        Assert.Equal("InstallerClean-3.6.0-setup.exe", AppCopy.DownloadFileName(AppCopyKind.Setup, "3.6.0"));
        foreach (var kind in new[] { AppCopyKind.Portable, AppCopyKind.Slim, AppCopyKind.CommandLineDownload })
        {
            var name = AppCopy.DownloadFileName(kind, "3.5.0");
            Assert.NotNull(name);
            Assert.DoesNotContain("3.5.0", name);
            Assert.Equal(name, AppCopy.DownloadFileName(kind, "3.6.0"));
        }
    }

    [Theory]
    [InlineData(@"C:\Program Files\InstallerClean\")]
    [InlineData(@"c:\program files\installerclean")]
    public void A_command_line_in_the_folder_the_setup_recorded_is_the_setups(string installLocation)
    {
        Assert.Equal(AppCopyKind.Setup,
            AppCopy.DecideCommandLine(isSingleFile: true, ProgramFilesFolder, RecordOf(installLocation)));
    }

    [Theory]
    [InlineData(@"C:\Program Files\InstallerClean\")]
    [InlineData(null)]
    public void A_command_line_anywhere_else_is_its_own_download(string? installLocation)
    {
        // Scoop installs only the window, so even a folder holding Scoop's files is not a
        // Scoop copy of the command line.
        Assert.Equal(AppCopyKind.CommandLineDownload,
            AppCopy.DecideCommandLine(isSingleFile: true, ScoopFolder, RecordOf(installLocation)));
        Assert.Equal(AppCopyKind.CommandLineDownload,
            AppCopy.DecideCommandLine(isSingleFile: true, DownloadsFolder, RecordOf(installLocation)));
    }

    [Fact]
    public void The_window_is_never_taken_for_the_command_lines_own_download()
    {
        foreach (var selfContained in new[] { true, false })
        foreach (var folder in new[] { ProgramFilesFolder, ScoopFolder, DownloadsFolder })
            Assert.NotEqual(AppCopyKind.CommandLineDownload,
                AppCopy.Decide(true, selfContained, folder, RecordOf(ProgramFilesFolder), NoFiles));
    }

    [Fact]
    public void Scoops_copy_has_no_download_and_is_updated_by_Scoop()
    {
        Assert.Null(AppCopy.DownloadAddress(AppCopyKind.Scoop, "3.5.0"));
        Assert.Null(AppCopy.DownloadFileName(AppCopyKind.Scoop, "3.5.0"));
        Assert.False(AppCopy.CanUpdateItself(AppCopyKind.Scoop));
        Assert.Equal("scoop update installerclean", AppCopy.ScoopUpdateCommand);
        foreach (var kind in Enum.GetValues<AppCopyKind>())
            Assert.Equal(kind == AppCopyKind.Scoop, AppCopy.UpdatedByScoop(kind));
    }

    [Fact]
    public void A_build_from_source_has_no_download_and_cannot_update_itself()
    {
        Assert.Null(AppCopy.DownloadAddress(AppCopyKind.FromSource, "3.5.0"));
        Assert.Null(AppCopy.DownloadFileName(AppCopyKind.FromSource, "3.5.0"));
        Assert.False(AppCopy.CanUpdateItself(AppCopyKind.FromSource));
    }

    [Fact]
    public void The_setups_record_names_the_folder_in_its_uninstall_key()
    {
        var registry = Registry(new RegistryKeyValues(
            RegistryKeyPresence.Present,
            [
                new RegistryValue("DisplayVersion", RegistryValueKind.String, "3.4.0"),
                new RegistryValue("InstallLocation", RegistryValueKind.String, ProgramFilesFolder + @"\"),
            ]));

        var record = AppCopy.ReadSetupRecord(registry);

        Assert.Equal(SetupRecordState.Folder, record.State);
        Assert.Equal(PathText.TrimEndingDirectorySeparator(Path.GetFullPath(ProgramFilesFolder + @"\")), record.Folder);
    }

    [Fact]
    public void There_is_no_record_where_the_key_is_absent() =>
        Assert.Equal(SetupRecord.None,
            AppCopy.ReadSetupRecord(Registry(new RegistryKeyValues(RegistryKeyPresence.Absent))));

    [Fact]
    public void The_record_cannot_tell_where_the_key_is_unreadable() =>
        Assert.Equal(SetupRecord.CannotTell,
            AppCopy.ReadSetupRecord(Registry(new RegistryKeyValues(RegistryKeyPresence.Unreadable))));

    [Theory]
    [InlineData("missing")]
    [InlineData("blank")]
    [InlineData("not text")]
    [InlineData("not a path")]
    [InlineData("relative")]
    [InlineData("drive-relative")]
    public void The_record_cannot_tell_where_the_key_has_no_usable_location(string which)
    {
        RegistryValue[] values = which switch
        {
            "missing" => [new RegistryValue("DisplayVersion", RegistryValueKind.String, "3.4.0")],
            "blank" => [new RegistryValue("InstallLocation", RegistryValueKind.String, "  ")],
            "not text" => [new RegistryValue("InstallLocation", RegistryValueKind.DWord, null)],
            "not a path" => [new RegistryValue("InstallLocation", RegistryValueKind.String, "\0not a path")],
            "relative" => [new RegistryValue("InstallLocation", RegistryValueKind.String, "InstallerClean")],
            _ => [new RegistryValue("InstallLocation", RegistryValueKind.String, "C:InstallerClean")],
        };

        var record = AppCopy.ReadSetupRecord(Registry(new RegistryKeyValues(RegistryKeyPresence.Present, values)));

        Assert.Equal(SetupRecord.CannotTell, record);
    }

    [Fact]
    public void An_undetermined_copy_has_no_download_and_cannot_update_itself()
    {
        Assert.Null(AppCopy.DownloadAddress(AppCopyKind.Undetermined, "3.5.0"));
        Assert.Null(AppCopy.DownloadFileName(AppCopyKind.Undetermined, "3.5.0"));
        Assert.False(AppCopy.CanUpdateItself(AppCopyKind.Undetermined));
        Assert.False(AppCopy.UpdatedByScoop(AppCopyKind.Undetermined));
    }

    [Fact]
    public void Only_the_four_kinds_with_a_download_update_themselves()
    {
        var updating = Enum.GetValues<AppCopyKind>().Where(AppCopy.CanUpdateItself).ToHashSet();

        Assert.Equal(new HashSet<AppCopyKind>
            {
                AppCopyKind.Setup, AppCopyKind.Portable, AppCopyKind.Slim, AppCopyKind.CommandLineDownload,
            },
            updating);
        foreach (var kind in Enum.GetValues<AppCopyKind>())
            Assert.Equal(updating.Contains(kind), AppCopy.DownloadFileName(kind, "3.5.0") is not null);
    }
}
