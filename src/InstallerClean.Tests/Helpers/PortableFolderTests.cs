using InstallerClean.Helpers;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// <see cref="PortableFolder.Find(string[], string, Func{string, bool})"/>: the marker beside the
/// exe of the copy a process stands for makes that exe's folder the app's own, and
/// <see cref="AppDataFolder.InUse"/> leaves the setup's copy in %LOCALAPPDATA%.
/// </summary>
public class PortableFolderTests
{
    private const string Exe = @"D:\Tools\InstallerClean\InstallerClean.exe";

    private static Func<string, bool> Files(params string[] present) =>
        path => present.Contains(path, StringComparer.OrdinalIgnoreCase);

    [Theory]
    [InlineData(@"D:\Tools\InstallerClean\InstallerClean-portable.txt")]
    [InlineData(@"D:\Tools\InstallerClean\InstallerClean-portable")]
    [InlineData(@"D:\Tools\InstallerClean\installerclean-PORTABLE.TXT")]
    public void The_marker_beside_the_exe_makes_its_folder_the_apps_own(string marker)
    {
        Assert.Equal(@"D:\Tools\InstallerClean", PortableFolder.Find([], Exe, Files(marker)));
    }

    [Theory]
    [InlineData(@"D:\Tools\InstallerClean\portable.txt")]
    [InlineData(@"D:\Tools\InstallerClean-portable.txt")]
    [InlineData(@"D:\Tools\InstallerClean\data\InstallerClean-portable.txt")]
    public void Without_the_marker_beside_the_exe_there_is_no_portable_folder(string elsewhere)
    {
        Assert.Null(PortableFolder.Find([], Exe, Files(elsewhere)));
    }

    [Fact]
    public void A_newer_version_replacing_a_copy_keeps_to_the_copys_folder()
    {
        // Running from its staging folder, with the copy it replaces named by the switch.
        string[] args = [@"/replacecopy:D:\Tools\InstallerClean\InstallerClean.exe", "/afterprocess:42"];
        var staged = @"C:\ProgramData\InstallerClean-update-0123\InstallerClean.exe";

        Assert.Equal(@"D:\Tools\InstallerClean",
            PortableFolder.Find(args, staged, Files(@"D:\Tools\InstallerClean\InstallerClean-portable.txt")));
        Assert.Null(PortableFolder.Find(args, staged, Files(@"C:\ProgramData\InstallerClean-update-0123\InstallerClean-portable.txt")));
    }

    [Theory]
    [InlineData(@"/replacecopy:InstallerClean.exe")]
    [InlineData(@"/replacecopy:")]
    public void A_switch_without_a_full_path_names_no_copy_and_the_running_exe_is_used(string sw)
    {
        Assert.Equal(@"D:\Tools\InstallerClean",
            PortableFolder.Find([sw], Exe, Files(@"D:\Tools\InstallerClean\InstallerClean-portable.txt")));
    }

    [Fact]
    public void Two_switches_name_no_copy()
    {
        string[] args = [@"/replacecopy:E:\A\InstallerClean.exe", @"/replacecopy:E:\B\InstallerClean.exe"];

        Assert.Null(PortableFolder.Find(args, Exe, Files(@"E:\A\InstallerClean-portable.txt")));
    }

    [Fact]
    public void A_copy_at_a_drives_root_keeps_the_roots_separator()
    {
        Assert.Equal(@"E:\", PortableFolder.Find([], @"E:\InstallerClean.exe", Files(@"E:\InstallerClean-portable.txt")));
    }

    [Theory]
    [InlineData(@"C:\Program Files\InstallerClean\")]
    [InlineData(@"C:\Program Files\InstallerClean")]
    [InlineData(@"c:\program files\installerclean")]
    public void The_setups_copy_keeps_its_files_in_local_app_data_whatever_is_beside_its_exe(string recorded)
    {
        Assert.Null(AppDataFolder.InUse(@"C:\Program Files\InstallerClean", () => SetupRecord.At(recorded)));
    }

    [Fact]
    public void A_copy_the_setups_record_cannot_rule_out_keeps_its_files_in_local_app_data()
    {
        Assert.Null(AppDataFolder.InUse(@"D:\Tools", () => SetupRecord.CannotTell));
    }

    [Fact]
    public void A_copy_elsewhere_than_the_setups_folder_keeps_its_files_beside_its_exe()
    {
        Assert.Equal(@"D:\Tools", AppDataFolder.InUse(@"D:\Tools", () => SetupRecord.At(@"C:\Program Files\InstallerClean\")));
        Assert.Equal(@"D:\Tools", AppDataFolder.InUse(@"D:\Tools\", () => SetupRecord.None));
        Assert.Null(AppDataFolder.InUse(null, () => SetupRecord.None));
    }
}
