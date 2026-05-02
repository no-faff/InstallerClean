using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

public class PruneEmptySubdirectoriesTests
{
    // Both tests skip unless INSTALLERCLEAN_TEST_PRUNE=1: the helper
    // deletes real empty subdirs in C:\Windows\Installer on the host.
    private const string OptInEnvVar = "INSTALLERCLEAN_TEST_PRUNE";
    private static bool OptedIn =>
        Environment.GetEnvironmentVariable(OptInEnvVar) == "1";

    [Fact]
    public void Does_not_throw_when_invoked_with_default_token()
    {
        if (!OptedIn) return;

        var ex = Record.Exception(() => InstallerCacheHelpers.PruneEmptySubdirectories());
        Assert.Null(ex);
    }

    [Fact]
    public void Respects_already_cancelled_token()
    {
        if (!OptedIn) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = Record.Exception(() =>
            InstallerCacheHelpers.PruneEmptySubdirectories(cts.Token));

        if (ex is not null)
            Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }
}
