using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// Where the space goes for a Move through this PC's own admin share, with nothing scripted:
/// the destination's nearest folder is read through the share, its volume serial number is
/// compared with the cache root's, and the match is confirmed by reopening the same id on the
/// cache's volume.
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
    public void A_share_destination_whose_folders_are_not_there_is_read_at_the_share_root()
    {
        // Neither folder is there, so the walk ends on the share's root, asked with its
        // trailing separator. That root is the cache drive's own root through the share.
        var destination = Path.Combine(CacheDriveShare(), $"ic-not-there-{Guid.NewGuid():N}", "Moved");

        Assert.True(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(destination));
        Assert.Equal(MoveDestinationKinds.SameDrive, MoveDestinationClassifier.Classify(destination));
    }

    [Fact]
    public void A_share_destination_below_a_folder_that_is_there_is_read_at_that_folder()
    {
        var windows = KnownFolders.GetPath(Environment.SpecialFolder.Windows);
        var throughTheShare = CacheDriveShare() + windows.Substring(3);
        var destination = Path.Combine(throughTheShare, $"ic-not-there-{Guid.NewGuid():N}");

        Assert.True(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(destination));
        Assert.Equal(MoveDestinationKinds.SameDrive, MoveDestinationClassifier.Classify(destination));
    }
}
