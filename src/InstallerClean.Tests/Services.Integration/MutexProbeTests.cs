using System.ComponentModel;
using System.Runtime.InteropServices;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The real <see cref="MutexProbe"/> against real Win32 named mutexes. Its sample is what
/// the pending-reboot check reads Windows Installer's own mutex through, so the answers it
/// gives for a mutex held, abandoned, owned by the asking thread or closed to this process
/// are held here to what Windows does and not to a fake's answers.
///
/// A <c>Local\</c> name with a fresh GUID per test, never <c>Global\_MSIExecute</c>: the
/// real object is machine-wide and a test holding it would hold up every installer on
/// whichever machine ran the suite.
///
/// The access-refused answer (<see cref="MutexSample.AccessRefused"/>) is reproduced with a
/// named object whose DACL is empty. The test using it first shows a plain open of that
/// object being refused with the very call the probe makes, so a setup that went subtly
/// wrong fails there rather than letting the assertion after it pass for the wrong reason.
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
    public void A_sample_hands_no_wait_to_the_threads_synchronisation_context()
    {
        using var existing = new Mutex(initiallyOwned: false, _name);

        var waits = WaitRecordingContext.RunOnThreadWithIt(context =>
        {
            var probe = new MutexProbe();
            Assert.Equal(MutexSample.NotHeld, probe.Sample(_name).State);
            return context.Waits;
        });

        Assert.Equal(0, waits);
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
        // The case reading ownership rather than existence is for, and the one
        // the name-does-not-exist test above cannot reach. A named mutex exists
        // for as long as any process holds a handle to it, owned or not, so an
        // existence test would answer "installer busy" whenever something merely
        // had the object open. An unowned handle held open is precisely that
        // state: the object exists, so TryOpenExisting succeeds, and the read of
        // its state is what tells the two apart.
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
    public void Sample_is_not_held_for_an_abandoned_mutex_and_does_not_take_it()
    {
        // This handle keeps the object there after the holder ends.
        using var mine = new Mutex(initiallyOwned: false, _name);
        var holder = new Thread(() =>
        {
            var mutex = new Mutex(initiallyOwned: false, _name);
            mutex.WaitOne(0);
            // No release, no dispose: the thread ends holding it.
        })
        { IsBackground = true };
        holder.Start();
        Assert.True(holder.Join(TimeSpan.FromSeconds(30)));

        Assert.Equal(MutexSample.NotHeld, new MutexProbe().Sample(_name).State);

        // The next wait is still the one told the mutex was abandoned. A sample that
        // had taken the mutex and released it would have been told instead, and this
        // wait would return true.
        Assert.Throws<AbandonedMutexException>(() => mine.WaitOne(0));
        mine.ReleaseMutex();
    }

    [Fact]
    public void Sample_is_not_held_for_a_mutex_the_sampling_thread_owns()
    {
        // Held means another thread owns it. The thread asking is not another thread.
        using var mine = new Mutex(initiallyOwned: false, _name);
        Assert.True(mine.WaitOne(0));
        try
        {
            Assert.Equal(MutexSample.NotHeld, new MutexProbe().Sample(_name).State);
        }
        finally
        {
            mine.ReleaseMutex();
        }
    }

    [Fact]
    public void Sample_leaves_a_free_mutex_free_for_a_thread_that_asks_at_once()
    {
        // Many samples from one thread while another takes and releases the mutex with a
        // zero wait. No take fails: no sample ever owns the mutex.
        using var mine = new Mutex(initiallyOwned: false, _name);
        using var stop = new ManualResetEventSlim();
        var sampler = new Thread(() =>
        {
            var probe = new MutexProbe();
            while (!stop.IsSet) probe.Sample(_name);
        })
        { IsBackground = true };
        sampler.Start();
        try
        {
            for (var take = 0; take < 20_000; take++)
            {
                Assert.True(mine.WaitOne(0), $"take {take} found the mutex owned");
                mine.ReleaseMutex();
            }
        }
        finally
        {
            stop.Set();
            Assert.True(sampler.Join(TimeSpan.FromSeconds(30)));
        }
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
