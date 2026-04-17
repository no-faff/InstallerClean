using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

public class PruneEmptySubdirectoriesTests
{
    [Fact]
    public void Does_not_throw_when_invoked_with_default_token()
    {
        var ex = Record.Exception(() => InstallerCacheHelpers.PruneEmptySubdirectories());
        Assert.Null(ex);
    }

    [Fact]
    public void Respects_already_cancelled_token()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = Record.Exception(() =>
            InstallerCacheHelpers.PruneEmptySubdirectories(cts.Token));

        if (ex is not null)
            Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }
}
