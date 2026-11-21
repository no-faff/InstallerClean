using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// The tests' own File.CreateSymbolicLink and Directory.CreateSymbolicLink make a link
/// on a GitHub Actions Windows runner, which runs as an administrator. Every test that
/// takes a link it cannot make as a host that makes none, and passes, rests on these
/// two making one where the suite runs on CI. A runner without the rights fails here
/// rather than skipping it.
/// </summary>
public class TestFrameworkLinkTests
{
    [CiWindowsRunnerFact("Needs the rights to make a symbolic link", evenWithoutAdministrator: true)]
    public void A_link_to_a_file_and_a_link_to_a_folder_are_made_and_read_through()
    {
        var folder = TestTemp.NewFolder("ic-links-");
        var fileLink = Path.Combine(folder, "file-link.txt");
        var folderLink = Path.Combine(folder, "folder-link");
        try
        {
            var target = Path.Combine(folder, "target");
            Directory.CreateDirectory(target);
            var targetFile = Path.Combine(target, "kept.txt");
            File.WriteAllText(targetFile, "through the link");

            var madeFile = File.CreateSymbolicLink(fileLink, targetFile);
            var madeFolder = Directory.CreateSymbolicLink(folderLink, target);

            Assert.True(madeFile.Exists, fileLink);
            Assert.True(madeFolder.Exists, folderLink);
            Assert.True((File.GetAttributes(fileLink) & FileAttributes.ReparsePoint) != 0, fileLink);
            Assert.True((File.GetAttributes(folderLink) & FileAttributes.ReparsePoint) != 0, folderLink);
            Assert.Equal("through the link", File.ReadAllText(fileLink));
            Assert.Equal(new[] { "kept.txt" },
                Directory.GetFiles(folderLink).Select(Path.GetFileName).ToArray());
        }
        finally
        {
            // Each link is removed as a link before the folder, so nothing is deleted through one.
            try { File.Delete(fileLink); } catch (Exception) { }
            try { if (Directory.Exists(folderLink)) Directory.Delete(folderLink); } catch (Exception) { }
            try { Directory.Delete(folder, recursive: true); } catch (Exception) { }
        }
    }
}
