using InstallerClean.Cli;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// <see cref="RunCancellation"/> lets a Cancel on one thread meet a Dispose on another, its
/// wait handle made, as .NET 10's CancellationTokenSource does: the Cancel either runs the
/// callbacks or, where the Dispose came first, throws ObjectDisposedException having begun
/// nothing.
/// </summary>
public class CliRunCancellationTests
{
    [Fact]
    public void A_cancel_meeting_a_dispose_runs_the_callbacks_or_finds_the_source_disposed()
    {
        for (var round = 0; round < 2_000; round++)
        {
            var run = new RunCancellation();
            _ = run.Token.WaitHandle;
            var called = false;
            run.Token.Register(() => called = true);
            Exception? thrown = null;
            using var go = new Barrier(2);

            var canceller = new Thread(() =>
            {
                go.SignalAndWait();
                try
                {
                    run.Cancel();
                }
                catch (Exception ex)
                {
                    thrown = ex;
                }
            });
            canceller.Start();
            go.SignalAndWait();
            run.Dispose();
            canceller.Join();

            if (thrown is null) Assert.True(called, $"round {round}: cancelled without its callback");
            else
            {
                // Only a Dispose that came first: the source never began cancelling.
                Assert.IsType<ObjectDisposedException>(thrown);
                Assert.False(called, $"round {round}: a callback ran and the cancel still threw");
                Assert.False(run.IsCancellationRequested, $"round {round}: cancelling began and the cancel threw");
            }
        }
    }

    [Fact]
    public void A_cancel_after_the_dispose_throws_ObjectDisposedException()
    {
        var run = new RunCancellation();
        run.Dispose();
        run.Dispose();

        Assert.Throws<ObjectDisposedException>(() => run.Cancel());
    }

    [Fact]
    public void A_cancel_cancels_the_token_and_runs_its_callbacks()
    {
        using var run = new RunCancellation();
        var called = false;
        run.Token.Register(() => called = true);

        run.Cancel();

        Assert.True(run.IsCancellationRequested);
        Assert.True(run.Token.IsCancellationRequested);
        Assert.True(called);
    }
}
