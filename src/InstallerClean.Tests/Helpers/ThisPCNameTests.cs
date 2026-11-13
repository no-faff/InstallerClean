using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

public class ThisPCNameTests
{
    [Theory]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("localhost.")]
    [InlineData("127.0.0.1")]
    [InlineData("127.1.2.3")]
    [InlineData("::1")]
    [InlineData("[::1]")]
    public void A_loopback_name_is_this_PC(string server) => Assert.True(ThisPCName.Names(server));

    [Fact]
    public void The_computer_name_is_this_PC()
    {
        Assert.True(ThisPCName.Names(Environment.MachineName));
        Assert.True(ThisPCName.Names(Environment.MachineName.ToLowerInvariant()));
    }

    [Theory]
    [InlineData("ic-not-this-pc-0f3a")]
    [InlineData("192.0.2.1")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Any_other_name_is_not_established_as_this_PC(string? server) => Assert.False(ThisPCName.Names(server));

    [Theory]
    [InlineData(@"\\localhost\C$\Moved", "localhost")]
    [InlineData(@"\\otherpc\share", "otherpc")]
    [InlineData(@"//otherpc/share/x", "otherpc")]
    [InlineData(@"\\?\UNC\otherpc\share", null)]
    [InlineData(@"\\.\C:\x", null)]
    [InlineData(@"C:\Windows", null)]
    [InlineData(@"\\", null)]
    public void The_server_is_read_from_a_share_path_alone(string path, string? server) =>
        Assert.Equal(server, ThisPCName.ServerOf(path));
}
