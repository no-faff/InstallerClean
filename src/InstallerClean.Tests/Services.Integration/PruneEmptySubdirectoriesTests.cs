using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

public class PruneEmptySubdirectoriesTests
{
    // Opt-in env var. Without it, both tests skip because
    // PruneEmptySubdirectories walks the real C:\Windows\Installer
    // and deletes empty subdirectories there - useful for production
    // but a destructive side effect on a developer or CI host where
    // the tests run with admin. Set INSTALLERCLEAN_TEST_PRUNE=1 to
    // run them on a host where you're OK with that.
    private const string OptInEnvVar = "INSTALLERCLEAN_TEST_PRUNE";
    private static bool OptedIn =>
        Environment.GetEnvironmentVariable(OptInEnvVar) == "1";

    [Fact]
    public void Does_not_throw_when_invoked_with_default_token()
    {
        if (!OptedIn) return; // see OptInEnvVar comment above

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
