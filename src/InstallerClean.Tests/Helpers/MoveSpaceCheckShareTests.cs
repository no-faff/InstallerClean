using InstallerClean.Helpers;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Where the space goes for a Move through a share (<see cref="MoveSpaceCheck.ResolveIsOnInstallerCacheDrive"/>):
/// the volume serial number the share's folder carries, against the cache root's. A scripted
/// reader stands in for both reads, so nothing here reaches a network.
/// </summary>
public class MoveSpaceCheckShareTests
{
    private const string CacheRoot = @"C:\Windows\Installer";
    private const string ThroughTheShare = @"\\localhost\C$\ICProbe-moved";

    [Fact]
    public void A_share_folder_on_the_cache_volume_is_the_same_drive()
    {
        var reader = new Identities(FileIdentityRead.NamesNothing);
        reader.Reads(CacheRoot, serial: 7);
        reader.Reads(@"\\localhost\C$\", serial: 7);

        Assert.True(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(
            ThroughTheShare, CacheRoot, reader, (_, _) => @"C:\ICProbe-moved"));
        Assert.Equal([CacheRoot, ThroughTheShare, @"\\localhost\C$\"], reader.Asked);
    }

    [Fact]
    public void A_matching_serial_number_whose_folder_does_not_open_here_is_another_drive()
    {
        // A share on another computer imaged from the same disk carries the same serial.
        var reader = new Identities(FileIdentityRead.NamesNothing);
        reader.Reads(CacheRoot, serial: 7);
        reader.Reads(ThroughTheShare, serial: 7);
        var confirmedAgainst = new List<string>();

        Assert.False(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(
            ThroughTheShare, CacheRoot, reader, (share, roots) => { confirmedAgainst.AddRange(roots); return null; }));
        Assert.Equal([CacheRoot], confirmedAgainst);
    }

    [Fact]
    public void A_share_folder_on_another_volume_is_another_drive()
    {
        var reader = new Identities(FileIdentityRead.NamesNothing);
        reader.Reads(CacheRoot, serial: 7);
        reader.Reads(ThroughTheShare, serial: 8);

        Assert.False(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(
            ThroughTheShare, CacheRoot, reader, (_, _) => throw new InvalidOperationException("confirmed a serial that did not match")));
    }

    [Fact]
    public void A_share_that_gives_no_file_id_is_another_drive()
    {
        var reader = new Identities(FileIdentityRead.IdentityUnavailable);
        reader.Reads(CacheRoot, serial: 7);

        Assert.False(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(ThroughTheShare, CacheRoot, reader));
    }

    [Theory]
    [InlineData(FileIdentityRead.OpenRefused)]
    [InlineData(FileIdentityRead.Faulted)]
    public void A_share_folder_that_will_not_read_is_not_settled(FileIdentityRead outcome)
    {
        var reader = new Identities(outcome);
        reader.Reads(CacheRoot, serial: 7);

        Assert.Null(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(ThroughTheShare, CacheRoot, reader));
    }

    [Fact]
    public void A_cache_root_that_will_not_read_is_not_settled()
    {
        var reader = new Identities(FileIdentityRead.NamesNothing);
        reader.Answers(CacheRoot, FileIdentityRead.OpenRefused);
        reader.Reads(ThroughTheShare, serial: 7);

        Assert.Null(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(ThroughTheShare, CacheRoot, reader));
    }

    [Fact]
    public void A_share_with_nothing_there_up_to_its_root_is_not_settled() =>
        Assert.Null(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(
            ThroughTheShare, CacheRoot, WithRoot(new Identities(FileIdentityRead.NamesNothing))));

    [Fact]
    public void A_Move_through_a_share_on_the_cache_volume_still_copies_and_is_still_measured()
    {
        // Where the space ends up and whether the Move copies are two questions, and they
        // part here: the space stays on the cache's volume, and each file is still copied
        // through the share.
        Assert.False(MoveSpaceCheck.RenamesOnInstallerCacheDrive(ThroughTheShare));
        Assert.Equal(500L, MoveSpaceCheck.RefusalFreeSpace(ThroughTheShare, 1_000, 500));
    }

    private static Identities WithRoot(Identities reader)
    {
        reader.Reads(CacheRoot, serial: 7);
        return reader;
    }

    /// <summary>
    /// A scripted <see cref="IFileIdentityReader"/>: each scripted path answers as scripted,
    /// and every other path answers the outcome the test names, with no identity.
    /// </summary>
    private sealed class Identities(FileIdentityRead otherwise) : IFileIdentityReader
    {
        private readonly Dictionary<string, (FileIdentityRead Outcome, FileIdentity Identity)> _byPath =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Every path this reader was asked about, in order.</summary>
        public List<string> Asked { get; } = new();

        /// <summary>The path reads as a folder on the volume with serial number <paramref name="serial"/>.</summary>
        public void Reads(string path, ulong serial) =>
            _byPath[path] = (FileIdentityRead.Read, new FileIdentity(serial, 1, 0));

        public void Answers(string path, FileIdentityRead outcome) => _byPath[path] = (outcome, default);

        public FileIdentityRead ReadOutcome(string path, out FileIdentity identity)
        {
            Asked.Add(path);
            var scripted = _byPath.TryGetValue(path, out var answer) ? answer : (otherwise, default);
            identity = scripted.Identity;
            return scripted.Outcome;
        }

        public FileIdentityRead ReadOutcomeWithVolume(string path, out FileIdentity identity, out VolumeName? volume)
        {
            volume = null;
            return ReadOutcome(path, out identity);
        }

        public VolumeName? VolumeOf(string path) => null;
    }
}
