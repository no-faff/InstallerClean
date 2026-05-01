using InstallerClean.Interop;

namespace InstallerClean.Tests.Services.Integration;

public class ShellFileOperationsTests
{
    [Fact]
    public void Empty_path_throws_argument_exception()
    {
        Assert.Throws<ArgumentException>(() => ShellFileOperations.SendToRecycleBin(string.Empty));
    }

    [Fact]
    public void Null_path_throws()
    {
        Assert.ThrowsAny<Exception>(() => ShellFileOperations.SendToRecycleBin(null!));
    }

    [Fact]
    public void Nonexistent_file_returns_nonzero_without_crashing()
    {
        var fake = Path.Combine(Path.GetTempPath(), "installerclean-absolutely-missing-" + Guid.NewGuid() + ".msi");

        var result = ShellFileOperations.SendToRecycleBin(fake);

        // Exact code varies by Windows version; we only assert it's a
        // non-zero failure that came back cleanly without throwing.
        Assert.NotEqual(0, result);
    }
}
