using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers.Integration;

public class CrashLogTests
{
    [Fact]
    public void Write_returns_the_expected_log_path()
    {
        var path = CrashLog.Write(new InvalidOperationException("test"));

        Assert.EndsWith("crash.log", path);
        Assert.Contains("NoFaff", path);
        Assert.Contains("InstallerClean", path);
    }

    [Fact]
    public void Write_never_throws_even_when_exception_serialisation_is_unusual()
    {
        var nested = new Exception("outer", new Exception("inner", new Exception("innermost")));

        var ex = Record.Exception(() => CrashLog.Write(nested));

        Assert.Null(ex);
    }
}
