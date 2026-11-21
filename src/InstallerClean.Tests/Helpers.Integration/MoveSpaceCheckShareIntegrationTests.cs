using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// Where the space goes for a Move through this PC's own admin share, with nothing scripted:
/// this PC's server service gives the share's local path, and that path is asked by its
/// volume.
/// </summary>
public class MoveSpaceCheckShareIntegrationTests
{
    private static string CacheDriveShare()
    {
        var root = Path.GetPathRoot(InstallerCacheHelpers.InstallerFolder)!;
        var share = $@"\\localhost\{root.Substring(0, 1)}$\";
        Assert.True(Directory.Exists(share),
            $"{share} does not open on this machine, so the route through the share cannot be tested here");
        return share;
    }

    [Fact]
    public void A_share_destination_whose_folders_are_not_there_is_the_cache_drive()
    {
        var destination = Path.Combine(CacheDriveShare(), $"ic-not-there-{Guid.NewGuid():N}", "Moved");

        Assert.True(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(destination));
        Assert.Equal(MoveDestinationKinds.SameDrive, MoveDestinationClassifier.Classify(destination));
    }

    [Fact]
    public void A_share_destination_below_a_folder_that_is_there_is_the_cache_drive()
    {
        var windows = KnownFolders.GetPath(Environment.SpecialFolder.Windows);
        var throughTheShare = CacheDriveShare() + windows.Substring(3);
        var destination = Path.Combine(throughTheShare, $"ic-not-there-{Guid.NewGuid():N}");

        Assert.True(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(destination));
        Assert.Equal(MoveDestinationKinds.SameDrive, MoveDestinationClassifier.Classify(destination));
    }

    [Fact]
    public void This_PCs_server_service_gives_the_admin_share_its_drive_root()
    {
        var root = Path.GetPathRoot(InstallerCacheHelpers.InstallerFolder)!;

        Assert.Equal(root, ThisPCName.LocalPathOfShare(root.Substring(0, 1) + "$"), ignoreCase: true);
        Assert.Null(ThisPCName.LocalPathOfShare("ic-not-a-share-0f3a"));
    }

    [Fact]
    public void A_share_this_PC_serves_whose_local_path_is_on_no_volume_is_not_settled()
    {
        // The service gives the share's local path, and that path is asked by its volume.
        var reads = new ShareReads(d => d, _ => true, _ => @"\\?\Volume{00000000-0000-0000-0000-000000000000}\");

        Assert.Null(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(@"\\localhost\ic-share\Moved", share: reads));
    }

    [Fact]
    public void The_production_reads_give_a_mapped_spelling_nothing_where_it_is_no_share()
    {
        Assert.Null(ShareReads.Production.ToSharePath(KnownFolders.GetPath(Environment.SpecialFolder.Windows)));
    }
}
