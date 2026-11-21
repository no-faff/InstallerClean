using InstallerClean.Helpers;
using InstallerClean.Interop;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// The declared-product check answers about drives and folder entries through two defaults that
/// ask Windows, and a check built without overriding them is the one the app runs.
/// </summary>
public class DeclaredProductCheckDefaultsTests
{
    private static DeclaredProductCheck Check() =>
        new(Substitute.For<IMsiApi>(), Substitute.For<IPackageIdentityReader>());

    [Fact]
    public void The_default_drive_kind_is_the_kind_Windows_reports_for_the_drive()
    {
        var root = Path.GetPathRoot(Environment.SystemDirectory)!;
        var drive = root.TrimEnd('\\');

        var kind = Check().DriveKindOf(drive);

        Assert.Equal(StorageHelpers.GetDriveKind(root), kind);
        Assert.Equal(DriveType.Fixed, kind);
    }

    [Fact]
    public void The_default_names_in_a_folder_are_the_names_Windows_holds_for_the_file()
    {
        var folder = TestTemp.NewFolder("ic-default-names-");
        var file = Path.Combine(folder, "InstallerClean default names test.msi");
        File.WriteAllBytes(file, []);

        var names = Check().NamesInFolderOf(file);

        Assert.NotNull(names);
        Assert.Contains("InstallerClean default names test.msi", names);
        Assert.Equal(StorageHelpers.GetNamesInFolder(file), names);
    }
}
