using InstallerClean.Helpers;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Where the space goes for a Move through a share whose server is not this PC by name
/// (<see cref="MoveSpaceCheck.ResolveIsOnInstallerCacheDrive"/>): the volume serial number the
/// share's folder carries, against the cache root's. A scripted reader stands in for both
/// reads, so nothing here reaches a network. A share this PC serves is asked through its
/// server service, in <c>MoveSpaceCheckShareIntegrationTests</c>.
/// </summary>
public class MoveSpaceCheckShareTests
{
    private const string CacheRoot = @"C:\Windows\Installer";
    private const string OtherPC = @"\\otherpc\C$\ICProbe-moved";

    [Fact]
    public void A_matching_serial_number_on_a_server_that_is_not_this_PC_is_not_settled()
    {
        // Another computer imaged from the same disk, or this PC under an alias.
        var reader = WithRoot(new Identities(FileIdentityRead.NamesNothing));
        reader.Reads(OtherPC, serial: 7);
        var asked = new List<string>();

        Assert.Null(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(
            OtherPC, CacheRoot, reader, Share(names: server => { asked.Add(server); return false; })));
        Assert.Equal(["otherpc"], asked);
    }

    [Fact]
    public void A_share_folder_on_another_volume_is_another_drive()
    {
        var reader = WithRoot(new Identities(FileIdentityRead.NamesNothing));
        reader.Reads(OtherPC, serial: 8);

        Assert.False(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(OtherPC, CacheRoot, reader, Share()));
    }

    [Fact]
    public void A_share_that_gives_no_file_id_is_another_drive()
    {
        var reader = WithRoot(new Identities(FileIdentityRead.IdentityUnavailable));

        Assert.False(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(OtherPC, CacheRoot, reader, Share()));
    }

    [Theory]
    [InlineData(FileIdentityRead.OpenRefused)]
    [InlineData(FileIdentityRead.Faulted)]
    public void A_share_folder_that_will_not_read_is_not_settled(FileIdentityRead outcome)
    {
        var reader = WithRoot(new Identities(outcome));

        Assert.Null(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(OtherPC, CacheRoot, reader, Share()));
        Assert.Empty(_serviceAsked);
    }

    [Fact]
    public void A_cache_root_that_will_not_read_is_not_settled()
    {
        var reader = new Identities(FileIdentityRead.NamesNothing);
        reader.Answers(CacheRoot, FileIdentityRead.OpenRefused);
        reader.Reads(OtherPC, serial: 8);

        Assert.Null(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(OtherPC, CacheRoot, reader, Share()));
        Assert.Empty(_serviceAsked);
    }

    [Theory]
    [InlineData(@"\\otherpc\C$\ICProbe-moved", @"\\otherpc\C$\")]
    [InlineData(@"\\?\UNC\otherpc\C$\ICProbe-moved", @"\\?\UNC\otherpc\C$\")]
    public void A_share_with_nothing_there_up_to_its_root_is_not_settled(string destination, string shareRoot)
    {
        var reader = WithRoot(new Identities(FileIdentityRead.NamesNothing));

        Assert.Null(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(destination, CacheRoot, reader, Share()));
        Assert.Equal([CacheRoot, destination, shareRoot], reader.Asked);
    }

    [Theory]
    [InlineData(@"\\otherpc\C$", @"\\otherpc\C$\")]
    [InlineData(@"\\otherpc\C$\", @"\\otherpc\C$\")]
    [InlineData(@"//otherpc/C$", @"//otherpc\C$\")]
    public void A_share_root_given_as_the_destination_is_read_with_its_separator(string destination, string readAs)
    {
        var reader = WithRoot(new Identities(FileIdentityRead.NamesNothing));
        reader.Reads(readAs, serial: 8);

        Assert.False(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(destination, CacheRoot, reader, Share()));
        Assert.Equal([CacheRoot, readAs], reader.Asked);
    }

    [Fact]
    public void A_server_this_PC_serves_is_asked_of_its_server_service_and_not_by_file_id()
    {
        var reader = WithRoot(new Identities(FileIdentityRead.NamesNothing));
        var shares = new List<string>();

        Assert.Null(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(
            @"\\localhost\C$\ICProbe-moved", CacheRoot, reader,
            Share(names: _ => true, local: name => { shares.Add(name); return null; })));
        Assert.Equal(["C$"], shares);
        Assert.Empty(reader.Asked);
    }

    [Fact]
    public void A_destination_is_asked_by_its_share_spelling()
    {
        // As a drive mapped onto a share resolves to the share's own spelling.
        var reader = WithRoot(new Identities(FileIdentityRead.NamesNothing));
        var servers = new List<string>();
        var shares = new List<string>();

        Assert.Null(MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(
            @"\\ic-alias\backup\ICProbe-moved", CacheRoot, reader,
            Share(
                toShare: _ => @"\\localhost\D$\ICProbe-moved",
                names: name => { servers.Add(name); return true; },
                local: name => { shares.Add(name); return null; })));
        Assert.Equal(["localhost"], servers);
        Assert.Equal(["D$"], shares);
    }

    [Theory]
    [InlineData(@"\\localhost\C$\Moved", @"\\localhost\C$\Moved")]
    [InlineData(@"\\?\UNC\localhost\C$\Moved", @"\\localhost\C$\Moved")]
    [InlineData(@"\\?\unc\localhost\C$\Moved", @"\\localhost\C$\Moved")]
    [InlineData(@"\\.\UNC\localhost\C$\Moved", @"\\localhost\C$\Moved")]
    [InlineData(@"//localhost/C$/Moved", @"\\localhost\C$\Moved")]
    public void A_share_spelling_is_taken_as_written_with_backslashes(string destination, string sharePath) =>
        Assert.Equal(sharePath, ShareReads.Production.ToSharePath(destination));

    [Theory]
    [InlineData(7UL, null)]
    [InlineData(8UL, false)]
    public void A_destination_with_no_share_spelling_is_asked_by_file_id(ulong serial, bool? expected)
    {
        // As a drive mapped onto a share whose resolution the kernel would not give.
        var reader = WithRoot(new Identities(FileIdentityRead.NamesNothing));
        reader.Reads(OtherPC, serial);

        Assert.Equal(expected, MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(
            OtherPC, CacheRoot, reader, Share(toShare: _ => null, names: _ => true)));
        Assert.Empty(_serviceAsked);
    }

    [Theory]
    [InlineData(@"C:\", "", @"C:\")]
    [InlineData(@"C:\Data", @"Moved\Sub", @"C:\Data\Moved\Sub")]
    [InlineData(@"C:\Data\", "Moved", @"C:\Data\Moved")]
    [InlineData(@"C:\Data", @"..\Moved", null)]
    [InlineData(@"C:\Data", @"Moved\.\Sub", null)]
    public void The_part_below_a_share_goes_on_its_local_path_and_never_above_it(string shareFolder, string below, string? expected) =>
        Assert.Equal(expected, MoveSpaceCheck.OnShareFolder(shareFolder, below));

    [Fact]
    public void A_share_path_climbing_above_its_share_is_asked_of_the_share_itself()
    {
        var reader = WithRoot(new Identities(FileIdentityRead.NamesNothing));
        var shares = new List<string>();

        MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(
            @"\\localhost\Data\..\..\Other\Moved", CacheRoot, reader,
            Share(names: _ => true, local: name => { shares.Add(name); return null; }));
        Assert.Equal(["Data"], shares);
    }

    [Fact]
    public void A_Move_through_a_share_on_the_cache_volume_still_copies_and_is_still_measured()
    {
        // Where the space ends up and whether the Move copies are two questions, and they
        // part here: the space stays on the cache's volume, and each file is still copied
        // through the share.
        const string throughTheShare = @"\\localhost\C$\ICProbe-moved";
        Assert.False(MoveSpaceCheck.RenamesOnInstallerCacheDrive(throughTheShare));
        Assert.Equal(500L, MoveSpaceCheck.RefusalFreeSpace(throughTheShare, 1_000, 500));
    }

    /// <summary>
    /// Scripted share reads: the destination is its own share spelling, its server is not this
    /// PC, and the server service knows no share and records that it was asked, unless a test
    /// says otherwise.
    /// </summary>
    private ShareReads Share(
        Func<string, string?>? toShare = null, Func<string, bool>? names = null, Func<string, string?>? local = null) =>
        new(toShare ?? (destination => destination), names ?? (_ => false),
            local ?? (name => { _serviceAsked.Add(name); return null; }));

    /// <summary>Every share the default stand-in for the server service was asked about.</summary>
    private readonly List<string> _serviceAsked = new();

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
