using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The real <see cref="MutexProbe"/> against real Win32 named mutexes. Covering
/// it separately is what stops <c>FakeMutexProbe</c> being the only thing under
/// test: the fake defines the contract it imitates, so the real probe has to be
/// held to that contract independently. The mutex hold that stops a msiexec
/// racing a delete batch rests entirely on
/// <c>TryAcquire</c> setting <c>heldByAnother</c> correctly, since
/// DeleteFilesService and MoveFilesService refuse the whole batch and report
/// InstallerBusy on exactly that flag.
///
/// A <c>Local\</c> name with a fresh GUID per test, never
/// <c>Global\_MSIExecute</c>: the real object is machine-wide and taking it
/// would serialise every installer on whichever machine ran the suite, which
/// is the very cost the production comment warns about.
///
/// The DACL-refused arm (returns null with heldByAnother false, so the caller
/// proceeds without the hold) is not covered: reproducing it means creating a
/// named object with a deny ACE, and a test that got that setup subtly wrong
/// would pass for the wrong reason.
/// </summary>
public class MutexProbeTests
{
    private readonly string _name = $"Local\\ic-test-{Guid.NewGuid():N}";

    /// <summary>
    /// Holds <paramref name="name"/> on a thread of its own for the duration of
    /// <paramref name="whileHeld"/>. A separate thread is the whole point: a
    /// Windows mutex is re-entrant for its owning thread, so a second WaitOne(0)
    /// from the test thread would return true and prove nothing.
    /// </summary>
    private static void WithNameHeldElsewhere(string name, Action whileHeld)
    {
        using var held = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var acquired = false;
        Exception? holderFailure = null;

        var holder = new Thread(() =>
        {
            try
            {
                using var mutex = new Mutex(initiallyOwned: false, name);
                acquired = mutex.WaitOne(0);
                held.Set();
                release.Wait(TimeSpan.FromSeconds(30));
                if (acquired) mutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                holderFailure = ex;
                held.Set();
            }
        })
        { IsBackground = true };

        holder.Start();
        try
        {
            Assert.True(held.Wait(TimeSpan.FromSeconds(30)), "the holder thread never started");
            Assert.Null(holderFailure);
            // Without this the assertions below would pass against a name
            // nobody holds, which is the opposite of what they claim to test.
            Assert.True(acquired, "the holder thread did not get the mutex");
            whileHeld();
        }
        finally
        {
            release.Set();
            holder.Join(TimeSpan.FromSeconds(30));
        }
    }

    [Fact]
    public void TryAcquire_takes_a_name_nobody_holds()
    {
        var probe = new MutexProbe();

        using var lease = probe.TryAcquire(_name, out var heldByAnother);

        Assert.NotNull(lease);
        Assert.False(heldByAnother);
    }

    [Fact]
    public void TryAcquire_reports_a_name_another_thread_holds()
    {
        WithNameHeldElsewhere(_name, () =>
        {
            var probe = new MutexProbe();

            var lease = probe.TryAcquire(_name, out var heldByAnother);

            // The pair the refuse-the-batch path depends on. A null lease with
            // heldByAnother false would instead mean "carry on without the
            // hold", which on a live installer transaction is the one outcome
            // that must not happen.
            Assert.Null(lease);
            Assert.True(heldByAnother);
        });
    }

    [Fact]
    public void Disposing_a_lease_frees_the_name_for_the_next_acquire()
    {
        var probe = new MutexProbe();

        // Acquire and release both on this thread, per the Win32 owner-thread
        // rule; the test is deliberately synchronous so no await can hop it.
        var first = probe.TryAcquire(_name, out _);
        Assert.NotNull(first);
        first.Dispose();

        using var second = probe.TryAcquire(_name, out var heldByAnother);

        Assert.NotNull(second);
        Assert.False(heldByAnother);
    }

    [Fact]
    public void An_abandoned_mutex_is_acquired_rather_than_reported_as_held()
    {
        // A holder thread that exits without releasing leaves the mutex
        // abandoned, which surfaces as AbandonedMutexException on the next
        // acquire WITH ownership already transferred. Reporting that as
        // heldByAnother would refuse every batch after a crashed installer,
        // and go on refusing until the machine restarted.
        var holder = new Thread(() =>
        {
            var mutex = new Mutex(initiallyOwned: false, _name);
            mutex.WaitOne(0);
            // No release, no dispose: the thread ends holding it.
        })
        { IsBackground = true };
        holder.Start();
        Assert.True(holder.Join(TimeSpan.FromSeconds(30)));

        var probe = new MutexProbe();
        using var lease = probe.TryAcquire(_name, out var heldByAnother);

        Assert.NotNull(lease);
        Assert.False(heldByAnother);
    }

    [Fact]
    public void IsHeld_is_false_for_a_name_nobody_holds()
    {
        // A name nothing has ever created: IsHeld opens with TryOpenExisting,
        // which does not create, so this exercises the name-does-not-exist
        // miss rather than the exists-but-unheld answer.
        Assert.False(new MutexProbe().IsHeld(_name));
    }

    [Fact]
    public void IsHeld_is_true_while_another_thread_holds_the_name()
    {
        WithNameHeldElsewhere(_name, () => Assert.True(new MutexProbe().IsHeld(_name)));
    }
}
