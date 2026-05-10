using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

public class PruneEmptySubdirectoriesTests
{
    // The helper deletes real empty subdirs in C:\Windows\Installer on
    // the host, which can't be done in CI. Both tests are marked Skip
    // so xUnit reports them as Skipped (visible, audited) rather than
    // silently passing without asserting. Remove the Skip parameter
    // and run elevated on a Windows host to exercise.
    private const string SkipReason =
        "Manual: requires elevated Windows host with C:\\Windows\\Installer to prune. Remove [Fact(Skip)] to run.";

    [Fact(Skip = SkipReason)]
    public void Does_not_throw_when_invoked_with_default_token()
    {
        var ex = Record.Exception(() => InstallerCacheHelpers.PruneEmptySubdirectories());
        Assert.Null(ex);
    }

    [Fact(Skip = SkipReason)]
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
