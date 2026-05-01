using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

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
}
