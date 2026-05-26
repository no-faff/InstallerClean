using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

public class InstallerCacheHelpersTests
{
    private static readonly string InstallerFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");

    [Fact]
    public void IsInstallerFolderOrChild_rejects_exact_folder()
    {
        Assert.True(InstallerCacheHelpers.IsInstallerFolderOrChild(InstallerFolder));
    }

    [Fact]
    public void IsInstallerFolderOrChild_rejects_with_trailing_separator()
    {
        Assert.True(InstallerCacheHelpers.IsInstallerFolderOrChild(InstallerFolder + @"\"));
    }

    [Fact]
    public void IsInstallerFolderOrChild_rejects_case_insensitive()
    {
        Assert.True(InstallerCacheHelpers.IsInstallerFolderOrChild(InstallerFolder.ToUpperInvariant()));
    }

    [Fact]
    public void IsInstallerFolderOrChild_rejects_subdirectory()
    {
        var sub = Path.Combine(InstallerFolder, "cleanup");
        Assert.True(InstallerCacheHelpers.IsInstallerFolderOrChild(sub));
    }

    [Fact]
    public void IsInstallerFolderOrChild_rejects_nested_subdirectory()
    {
        var sub = Path.Combine(InstallerFolder, "a", "b", "c");
        Assert.True(InstallerCacheHelpers.IsInstallerFolderOrChild(sub));
    }

    [Fact]
    public void IsInstallerFolderOrChild_allows_sibling_folder()
    {
        var sibling = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
        Assert.False(InstallerCacheHelpers.IsInstallerFolderOrChild(sibling));
    }

    [Fact]
    public void IsInstallerFolderOrChild_allows_unrelated_path()
    {
        Assert.False(InstallerCacheHelpers.IsInstallerFolderOrChild(@"D:\Backup\Installer"));
    }

    [Fact]
    public void IsInstallerFolderOrChild_no_false_positive_on_prefix_match()
    {
        // "InstallerXyz" should not match just because it starts with "Installer"
        var notChild = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows), "InstallerBackup");
        Assert.False(InstallerCacheHelpers.IsInstallerFolderOrChild(notChild));
    }

    private static readonly string WindowsFolder =
        Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    private static readonly string ProgramFilesFolder =
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    private static readonly string ProgramFilesX86Folder =
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    private static readonly string ProgramDataFolder =
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

    [Fact]
    public void IsSystemFolderOrChild_rejects_windows_folder()
    {
        Assert.True(InstallerCacheHelpers.IsSystemFolderOrChild(WindowsFolder));
    }

    [Fact]
    public void IsSystemFolderOrChild_rejects_windows_subdirectory()
    {
        Assert.True(InstallerCacheHelpers.IsSystemFolderOrChild(
            Path.Combine(WindowsFolder, "System32", "Spool")));
    }

    [Fact]
    public void IsSystemFolderOrChild_rejects_with_trailing_separator()
    {
        Assert.True(InstallerCacheHelpers.IsSystemFolderOrChild(WindowsFolder + @"\"));
    }

    [Fact]
    public void IsSystemFolderOrChild_rejects_case_insensitive()
    {
        Assert.True(InstallerCacheHelpers.IsSystemFolderOrChild(WindowsFolder.ToUpperInvariant()));
    }

    [Fact]
    public void IsSystemFolderOrChild_rejects_program_files()
    {
        Assert.True(InstallerCacheHelpers.IsSystemFolderOrChild(ProgramFilesFolder));
    }

    [Fact]
    public void IsSystemFolderOrChild_rejects_program_files_subdirectory()
    {
        Assert.True(InstallerCacheHelpers.IsSystemFolderOrChild(
            Path.Combine(ProgramFilesFolder, "Some Vendor")));
    }

    [Fact]
    public void IsSystemFolderOrChild_rejects_program_files_x86()
    {
        // On 32-bit Windows ProgramFilesX86 returns the same path as
        // ProgramFiles; both are still system roots and either way
        // the gate should fire.
        Assert.True(InstallerCacheHelpers.IsSystemFolderOrChild(ProgramFilesX86Folder));
    }

    [Fact]
    public void IsSystemFolderOrChild_rejects_program_data()
    {
        Assert.True(InstallerCacheHelpers.IsSystemFolderOrChild(ProgramDataFolder));
    }

    [Fact]
    public void IsSystemFolderOrChild_rejects_program_data_subdirectory()
    {
        Assert.True(InstallerCacheHelpers.IsSystemFolderOrChild(
            Path.Combine(ProgramDataFolder, "Microsoft", "Crypto")));
    }

    [Fact]
    public void IsSystemFolderOrChild_allows_user_profile_path()
    {
        var userPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Backups");
        Assert.False(InstallerCacheHelpers.IsSystemFolderOrChild(userPath));
    }

    [Fact]
    public void IsSystemFolderOrChild_allows_unrelated_drive_path()
    {
        Assert.False(InstallerCacheHelpers.IsSystemFolderOrChild(@"D:\Backup\InstallerClean"));
    }

    [Fact]
    public void IsSystemFolderOrChild_no_false_positive_on_prefix_match()
    {
        // "C:\WindowsBackup" should not match just because it starts with "C:\Windows"
        var notChild = WindowsFolder + "Backup";
        Assert.False(InstallerCacheHelpers.IsSystemFolderOrChild(notChild));
    }

    [Fact]
    public void IsSystemFolderOrChild_handles_empty_and_whitespace()
    {
        Assert.False(InstallerCacheHelpers.IsSystemFolderOrChild(string.Empty));
        Assert.False(InstallerCacheHelpers.IsSystemFolderOrChild("   "));
    }
}
