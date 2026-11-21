using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

public class ThisPCNameTests
{
    [Theory]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("127.0.0.1")]
    [InlineData("--1.ipv6-literal.net")]
    [InlineData("0--1.IPV6-LITERAL.NET")]
    public void A_loopback_name_is_this_PC(string server) => Assert.True(ThisPCName.Names(server));

    [Fact]
    public void The_computer_name_and_the_host_name_are_this_PC()
    {
        Assert.True(ThisPCName.Names(Environment.MachineName));
        Assert.True(ThisPCName.Names(Environment.MachineName.ToLowerInvariant()));
        Assert.True(ThisPCName.Names(System.Net.Dns.GetHostName()));
    }

    [Theory]
    [InlineData("ic-not-this-pc-0f3a")]
    [InlineData("192.0.2.1")]
    [InlineData("127.0.0.2")]
    [InlineData("127.1")]
    [InlineData("2130706433")]
    [InlineData("localhost.")]
    [InlineData("::1")]
    [InlineData("fe80--1s4.ipv6-literal.net")]
    [InlineData("2001-db8--1.ipv6-literal.net")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Any_other_name_is_not_established_as_this_PC(string? server) => Assert.False(ThisPCName.Names(server));

    [Fact]
    public void A_name_resolved_by_a_suffix_or_mDNS_is_not_established_as_this_PC() =>
        Assert.False(ThisPCName.Names(System.Net.Dns.GetHostName() + ".local"));

    [Theory]
    [InlineData(@"\\localhost\C$\Moved\Sub", "localhost", "C$", @"Moved\Sub")]
    [InlineData(@"\\otherpc\share", "otherpc", "share", "")]
    [InlineData(@"\\otherpc\share\", "otherpc", "share", "")]
    public void A_share_path_splits_into_its_server_share_and_the_rest(string path, string server, string share, string below)
    {
        Assert.True(ThisPCName.TrySplit(path, out var s, out var n, out var b));
        Assert.Equal((server, share, below), (s, n, b));
    }

    [Theory]
    [InlineData(@"\\?\UNC\otherpc\share")]
    [InlineData(@"\\.\C:\x")]
    [InlineData(@"C:\Windows")]
    [InlineData(@"\\otherpc")]
    [InlineData(@"\\")]
    public void Any_other_path_does_not_split(string path) => Assert.False(ThisPCName.TrySplit(path, out _, out _, out _));
}
