using System.ComponentModel;
using System.Runtime.InteropServices;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The real <see cref="MutexProbe"/> against real Win32 named mutexes. Covering
/// it separately is what stops <c>FakeMutexProbe</c> being the only thing under
/// test: the fake defines the contract it imitates, so the real probe has to be
/// held to that contract independently. The mutex hold that stops a msiexec
/// racing a delete batch rests entirely on
/// <c>TryAcquire</c> reporting the right <see cref="MutexAcquireOutcome"/>, since
/// DeleteFilesService and MoveFilesService refuse the whole batch and choose which
/// refusal the user is told about on exactly that value.
///
/// A <c>Local\</c> name with a fresh GUID per test, never
/// <c>Global\_MSIExecute</c>: the real object is machine-wide and taking it
/// would serialise every installer on whichever machine ran the suite, which
/// is the very cost the production comment warns about.
///
/// The access-refused answers (<see cref="MutexSample.AccessRefused"/> from the
/// sample, and a null lease with <see cref="MutexAcquireOutcome.AccessRefused"/>
/// from the acquire) are reproduced with a named object whose DACL is empty. Each
/// test using one first shows a plain open of that object being refused with the
/// very call the probe makes, so a setup that went subtly wrong fails there rather
/// than letting the assertion after it pass for the wrong reason.
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

        using var lease = probe.TryAcquire(_name, out var outcome);

        Assert.NotNull(lease);
        Assert.Equal(MutexAcquireOutcome.Acquired, outcome);
    }

    [Fact]
    public void TryAcquire_reports_a_name_another_thread_holds()
    {
        WithNameHeldElsewhere(_name, () =>
        {
            var probe = new MutexProbe();

            var lease = probe.TryAcquire(_name, out var outcome);

            // The value that decides WHICH refusal the caller reports. Every
            // outcome here stops the batch, so a null lease under the wrong one
            // would not let anything through; what it would do is send a live
            // installer transaction down a path whose sentence names a different
            // condition, and the pending-reboot banner that would have named the
            // real cause is never painted.
            Assert.Null(lease);
            Assert.Equal(MutexAcquireOutcome.HeldByAnother, outcome);
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

        using var second = probe.TryAcquire(_name, out var outcome);

        Assert.NotNull(second);
        Assert.Equal(MutexAcquireOutcome.Acquired, outcome);
    }

    [Fact]
    public void A_sample_and_an_acquire_hand_no_wait_to_the_threads_synchronisation_context()
    {
        using var existing = new Mutex(initiallyOwned: false, _name);

        var waits = WaitRecordingContext.RunOnThreadWithIt(context =>
        {
            var probe = new MutexProbe();
            Assert.Equal(MutexSample.NotHeld, probe.Sample(_name).State);
            using (var lease = probe.TryAcquire(_name, out var outcome))
                Assert.Equal(MutexAcquireOutcome.Acquired, outcome);
            return context.Waits;
        });

        Assert.Equal(0, waits);
    }

    [Fact]
    public void An_abandoned_mutex_is_acquired_rather_than_reported_as_held()
    {
        // A holder thread that exits without releasing leaves the mutex
        // abandoned, which the next acquire's wait answers as abandoned, WITH
        // ownership already transferred. Reporting that as
        // held by another would refuse every batch after a crashed installer,
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
        using var lease = probe.TryAcquire(_name, out var outcome);

        Assert.NotNull(lease);
        Assert.Equal(MutexAcquireOutcome.Acquired, outcome);
    }

    [Fact]
    public void Sample_is_not_held_for_a_name_nobody_holds()
    {
        // A name nothing has ever created: Sample opens with TryOpenExisting,
        // which does not create, so this exercises the name-does-not-exist
        // miss rather than the exists-but-unheld answer.
        Assert.Equal(MutexSample.NotHeld, new MutexProbe().Sample(_name).State);
    }

    [Fact]
    public void Sample_is_not_held_for_a_name_that_exists_but_nobody_holds()
    {
        // The case the acquire-rather-than-check design exists for, and the one
        // the name-does-not-exist test above cannot reach. A named mutex exists
        // for as long as any process holds a handle to it, owned or not, so an
        // existence test would answer "installer busy" whenever something merely
        // had the object open. An unowned handle held open is precisely that
        // state: the object exists, so TryOpenExisting succeeds, and the
        // zero-wait acquire is what tells the two apart.
        using var existsUnheld = new Mutex(initiallyOwned: false, _name);

        Assert.Equal(MutexSample.NotHeld, new MutexProbe().Sample(_name).State);
    }

    [Fact]
    public void Sample_is_held_while_another_thread_holds_the_name()
    {
        WithNameHeldElsewhere(_name,
            () => Assert.Equal(MutexSample.Held, new MutexProbe().Sample(_name).State));
    }

    [Fact]
    public void Sample_reports_a_name_whose_security_refuses_the_open_as_refused_and_not_as_held()
    {
        using var refusing = CreateRefusingMutex(_name);

        // THE CONTROL. The object is there and a plain open of it, the same call
        // Sample makes, is refused. Without this the assertion below could pass
        // against an object that refused for some other reason, or against no
        // object at all.
        Assert.Throws<UnauthorizedAccessException>(() => Mutex.TryOpenExisting(_name, out _));

        // The answer that decides which banner the window paints and which sentence
        // and exit code the command line gives. Held would tell the user something
        // is installing, which nothing here has seen.
        Assert.Equal(MutexSample.AccessRefused, new MutexProbe().Sample(_name).State);
    }

    [Fact]
    public void TryAcquire_reports_a_name_whose_security_refuses_the_open_as_refused()
    {
        using var refusing = CreateRefusingMutex(_name);

        // The control, for the call TryAcquire makes: the create-or-open
        // constructor meets the existing object and is refused.
        Assert.Throws<UnauthorizedAccessException>(() => new Mutex(initiallyOwned: false, _name));

        var probe = new MutexProbe();
        var lease = probe.TryAcquire(_name, out var outcome);

        Assert.Null(lease);
        Assert.Equal(MutexAcquireOutcome.AccessRefused, outcome);
        // And the sample of the same object agrees with it, which is what lets the
        // pending-reboot gate meet a standing refusal before the action does.
        Assert.Equal(MutexSample.AccessRefused, probe.Sample(_name).State);
    }

    /// <summary>
    /// Creates <paramref name="name"/> carrying an empty DACL, which grants nothing to
    /// anybody. SYNCHRONIZE and MUTEX_MODIFY_STATE are not rights an owner holds
    /// implicitly, so every later open asking for them is refused, this process's
    /// included, elevated or not. The create itself returns a handle whatever the new
    /// object's security says, since that security governs the opens after it, and
    /// the handle keeps the object alive until the test disposes it.
    /// </summary>
    private static SafeWaitHandle CreateRefusingMutex(string name)
    {
        if (!ConvertStringSecurityDescriptorToSecurityDescriptorW("D:", SddlRevision1, out var descriptor, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            var attributes = new SecurityAttributes
            {
                Length = Marshal.SizeOf<SecurityAttributes>(),
                SecurityDescriptor = descriptor,
                InheritHandle = 0,
            };
            var handle = CreateMutexW(ref attributes, initialOwner: false, name);
            if (handle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return handle;
        }
        finally
        {
            LocalFree(descriptor);
        }
    }

    private const uint SddlRevision1 = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityAttributes
    {
        public int Length;
        public IntPtr SecurityDescriptor;
        public int InheritHandle;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptorW(
        string stringSecurityDescriptor, uint revision, out IntPtr securityDescriptor, IntPtr size);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeWaitHandle CreateMutexW(
        ref SecurityAttributes attributes, bool initialOwner, string name);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
}
