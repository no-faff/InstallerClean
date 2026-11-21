using InstallerClean.Helpers;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The Move destination gates' second comparison, of a share path's local path, which refuses
/// a destination reaching the Installer folder or a system folder through this PC's own share.
/// A scripted reader says which local folder a share path reaches.
/// </summary>
public class InstallerCacheHelpersDestinationIdentityTests
{
    private const string ThroughTheShare = @"\\localhost\C$\Moved";

    private static string InstallerRoot =>
        InstallerCacheHelpers.ResolveFinalPath(InstallerCacheHelpers.InstallerFolder).TrimEnd('\\');

    private static string WindowsRoot =>
        InstallerCacheHelpers.ResolveFinalPath(KnownFolders.GetPath(Environment.SpecialFolder.Windows)).TrimEnd('\\');

    [Fact]
    public void A_share_path_reaching_a_folder_in_the_Installer_folder_is_inside_it()
    {
        var reader = new Reaches(Path.Combine(InstallerRoot, "Moved"));

        Assert.True(InstallerCacheHelpers.ResolvesInsideInstallerFolder(ThroughTheShare, proven: true, reader.Read));
        Assert.Equal([ThroughTheShare], reader.Asked);
    }

    [Fact]
    public void A_share_path_reaching_the_Installer_folder_itself_is_inside_it()
    {
        Assert.True(InstallerCacheHelpers.ResolvesInsideInstallerFolder(
            ThroughTheShare, proven: true, new Reaches(InstallerRoot).Read));
    }

    [Fact]
    public void A_share_path_reaching_a_folder_inside_the_Windows_folder_is_inside_a_system_folder()
    {
        // A share made on a folder inside a root, as print$ is on System32\spool\drivers.
        var reader = new Reaches(Path.Combine(WindowsRoot, "System32", "spool", "drivers", "Moved"));

        Assert.True(InstallerCacheHelpers.ResolvesInsideSystemFolder(@"\\localhost\print$\Moved", proven: true, reader.Read));
    }

    [Fact]
    public void Each_gate_hands_the_reader_its_own_roots()
    {
        var reader = new Reaches(null);

        InstallerCacheHelpers.ResolvesInsideInstallerFolder(ThroughTheShare, proven: true, reader.Read);
        Assert.Equal([InstallerRoot], reader.Roots);

        InstallerCacheHelpers.ResolvesInsideSystemFolder(ThroughTheShare, proven: true, reader.Read);
        Assert.Contains(WindowsRoot, reader.Roots);
    }

    [Fact]
    public void A_share_path_reaching_another_folder_is_not_inside_either()
    {
        var reader = new Reaches(@"C:\Users\Public\Moved");

        Assert.False(InstallerCacheHelpers.ResolvesInsideInstallerFolder(ThroughTheShare, proven: true, reader.Read));
        Assert.False(InstallerCacheHelpers.ResolvesInsideSystemFolder(ThroughTheShare, proven: true, reader.Read));
    }

    [Fact]
    public void A_share_path_whose_local_path_is_not_established_is_compared_by_its_spelling_alone()
    {
        var reader = new Reaches(null);

        Assert.False(InstallerCacheHelpers.ResolvesInsideInstallerFolder(ThroughTheShare, proven: true, reader.Read));
        Assert.False(InstallerCacheHelpers.ResolvesInsideSystemFolder(ThroughTheShare, proven: true, reader.Read));
        Assert.Equal(2, reader.Asked.Count);
    }

    [Fact]
    public void A_local_path_is_not_asked_about()
    {
        // A volume root trimmed to its letter among them: "C:" alone names the current folder
        // on that drive, so asking about it would read some other folder.
        var reader = new Reaches(InstallerRoot);
        var root = Path.GetPathRoot(TestTemp.Run)!;

        Assert.False(InstallerCacheHelpers.ResolvesInsideInstallerFolder(
            Path.Combine(TestTemp.Run, "Moved"), proven: true, reader.Read));
        Assert.False(InstallerCacheHelpers.ResolvesInsideSystemFolder(root.TrimEnd('\\'), proven: true, reader.Read));
        Assert.Empty(reader.Asked);
    }

    [Fact]
    public void A_path_the_spelling_places_inside_is_not_asked_about()
    {
        var reader = new Reaches(null);

        Assert.True(InstallerCacheHelpers.ResolvesInsideInstallerFolder(
            Path.Combine(InstallerRoot, "Moved"), proven: true, reader.Read));
        Assert.True(InstallerCacheHelpers.ResolvesInsideSystemFolder(
            Path.Combine(WindowsRoot, "Moved"), proven: true, reader.Read));
        Assert.Empty(reader.Asked);
    }

    [Fact]
    public void A_share_path_that_did_not_resolve_is_not_asked_about()
    {
        var reader = new Reaches(InstallerRoot);

        Assert.False(InstallerCacheHelpers.ResolvesInsideInstallerFolder(ThroughTheShare, proven: false, reader.Read));
        Assert.False(InstallerCacheHelpers.ResolvesInsideSystemFolder(ThroughTheShare, proven: false, reader.Read));
        Assert.Empty(reader.Asked);
    }

    [Fact]
    public void Both_gates_refuse_a_destination_reached_through_this_PCs_own_admin_share()
    {
        // The whole route as a Move takes it, with nothing scripted: the destination resolves
        // to its UNC spelling, its nearest folder's id is read through the share, and that id
        // opens on the system drive as the Installer folder or the Windows folder.
        var windows = KnownFolders.GetPath(Environment.SpecialFolder.Windows);
        var throughTheShare = $@"\\localhost\{windows.Substring(0, 1)}$" + windows.Substring(2);
        Assert.True(Directory.Exists(throughTheShare),
            $"{throughTheShare} does not open on this machine, so the route through the share cannot be tested here");

        Assert.True(InstallerCacheHelpers.IsInstallerFolderOrChild(
            Path.Combine(throughTheShare, "Installer", "ic-not-there")));
        Assert.True(InstallerCacheHelpers.IsSystemFolderOrChild(
            Path.Combine(throughTheShare, "ic-not-there")));
    }

    [Fact]
    public void The_production_kernel_opens_a_folder_by_its_id_as_its_own_local_path()
    {
        // The kernel's own read and reopen, on a local folder: the id read off the folder
        // opens on its volume, named by that volume's root, as the folder itself, and the part
        // of the path not there yet goes back on. Through a share the first read is made over
        // the network and the rest is the same.
        var folder = TestTemp.NewFolder("ic-destination-");
        try
        {
            var local = InstallerCacheHelpers.LocalPathThroughThisPCsVolumes(
                Path.Combine(folder, "not-there", "Moved"), [Path.GetPathRoot(folder)!]);

            Assert.Equal(
                Path.Combine(InstallerCacheHelpers.ResolveFinalPath(folder), "not-there", "Moved"),
                local,
                ignoreCase: true);
        }
        finally
        {
            TestTemp.Delete(folder);
        }
    }

    [Fact]
    public void The_production_kernel_does_not_open_a_folder_on_no_roots_volume()
    {
        var folder = TestTemp.NewFolder("ic-destination-");
        try
        {
            Assert.Null(InstallerCacheHelpers.LocalPathThroughThisPCsVolumes(folder, []));
        }
        finally
        {
            TestTemp.Delete(folder);
        }
    }

    /// <summary>
    /// A scripted <see cref="LocalPathReader"/>: every share path it is asked about reaches
    /// <paramref name="localPath"/>, or has no established local path where that is null.
    /// </summary>
    private sealed class Reaches(string? localPath)
    {
        /// <summary>Every share path this reader was asked about, in order.</summary>
        public List<string> Asked { get; } = new();

        /// <summary>The roots it was last handed.</summary>
        public IReadOnlyList<string> Roots { get; private set; } = [];

        public string? Read(string sharePath, IReadOnlyList<string> roots)
        {
            Asked.Add(sharePath);
            Roots = roots;
            return localPath;
        }
    }
}
