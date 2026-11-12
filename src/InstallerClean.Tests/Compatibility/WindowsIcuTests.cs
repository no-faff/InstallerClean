using InstallerClean.Compatibility;

namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// <see cref="WindowsIcu"/> reads DOTNET_SYSTEM_GLOBALIZATION_USENLS as .NET 10 reads it: "1"
/// or "true" in any case asks for NLS, and anything else, or no variable, leaves ICU in use.
/// </summary>
public class WindowsIcuTests
{
    [Theory]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("True", true)]
    [InlineData("0", false)]
    [InlineData("false", false)]
    [InlineData("yes", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void The_variable_asks_for_NLS_as_NET_10_reads_it(string? value, bool nls)
    {
        Assert.Equal(nls, WindowsIcu.AsksForNls(value));
    }
}
