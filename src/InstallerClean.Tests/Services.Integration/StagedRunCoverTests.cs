using System.Runtime.InteropServices;
using InstallerClean.Cli;
using InstallerClean.Services;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// <see cref="StagedRunCover"/> against real named mutexes, with an event standing for the
/// older copy's process: unsignalled while it runs, signalled once it has ended. Where the
/// older copy ends holding the mutex, its stand-in is the holding thread's own handle instead
/// (see <see cref="Holder.ThreadHandle"/>). Each test
/// names a fresh <c>Local\</c> mutex, never <see cref="InstanceLock.Name"/>, so a copy of
/// InstallerClean running on the machine neither fails a test nor is kept out by one.
/// </summary>
public class StagedRunCoverTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    private readonly string _name = $@"Local\InstallerClean_CoverTest_{Guid.NewGuid():N}";

    [Fact]
    public void A_run_is_covered_while_the_older_copy_runs_holding_the_mutex()
    {
        using var olderCopy = new ManualResetEvent(false);
        using var holder = new Holder(_name);

        using var cover = new StagedRunCover(_name, olderCopy.SafeWaitHandle);

        Assert.True(cover.Covers());
        Assert.False(cover.Holds);
        Assert.False(cover.Lost.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_cover_takes_the_mutex_as_the_older_copy_lets_go_of_it_or_ends_holding_it(bool abandoned)
    {
        using var ended = new ManualResetEvent(false);
        var holder = new Holder(_name);
        using var threadHandle = holder.ThreadHandle;
        // Ending holding the mutex, the older copy is the holder's thread, whose handle Windows
        // signals only once the thread has ended and the mutex is abandoned, as a process's handle
        // is signalled only once its mutexes are. An event set after the thread is joined can be
        // seen before the abandonment, and the cover then finds the mutex still held and is lost.
        var olderCopy = abandoned ? threadHandle : ended.SafeWaitHandle;
        using var cover = new StagedRunCover(_name, olderCopy);

        holder.End(release: !abandoned);
        if (!abandoned) ended.Set();

        Assert.True(SpinWait.SpinUntil(() => cover.Holds, Bound),
            $"The cover did not take the mutex within {Bound.TotalSeconds} s; Lost is {cover.Lost.IsCancellationRequested}.");
        Assert.True(cover.Covers());
        Assert.False(cover.Lost.IsCancellationRequested);
    }

    [Fact]
    public void A_run_whose_older_copy_ended_with_the_mutex_held_elsewhere_is_not_covered()
    {
        using var olderCopy = new ManualResetEvent(false);
        using var elsewhere = new Holder(_name);
        using var cover = new StagedRunCover(_name, olderCopy.SafeWaitHandle);

        olderCopy.Set();

        // Not covered from the moment the older copy has ended, before the cover's thread has
        // found the mutex taken as well as after.
        Assert.False(cover.Covers());
        Assert.True(SpinWait.SpinUntil(() => cover.Lost.IsCancellationRequested, Bound));
        Assert.False(cover.Covers());
        Assert.False(cover.Holds);
    }

    [Fact]
    public void A_cover_whose_wait_fails_does_not_cover_the_run()
    {
        var olderCopy = new ManualResetEvent(false);
        var handle = olderCopy.SafeWaitHandle;
        olderCopy.Dispose();
        using var cover = new StagedRunCover(_name, handle);

        Assert.False(cover.Covers());
        Assert.True(SpinWait.SpinUntil(() => cover.Lost.IsCancellationRequested, Bound));
        Assert.False(cover.Holds);
    }

    [Fact]
    public void Dispose_releases_the_mutex_the_cover_holds()
    {
        using var olderCopy = new ManualResetEvent(true);
        var cover = new StagedRunCover(_name, olderCopy.SafeWaitHandle);
        Assert.True(SpinWait.SpinUntil(() => cover.Holds, Bound));

        cover.Dispose();

        Assert.True(OnOtherThread(() =>
        {
            using var next = new InstanceLock(_name);
            var took = next.TryTake();
            if (took) next.Release();
            return took;
        }));
    }

    [Fact]
    public async Task Dispose_ends_a_cover_still_waiting_for_the_mutex()
    {
        using var olderCopy = new ManualResetEvent(false);
        using var holder = new Holder(_name);
        var cover = new StagedRunCover(_name, olderCopy.SafeWaitHandle);

        var disposed = Task.Run(cover.Dispose);

        Assert.Same(disposed, await Task.WhenAny(disposed, Task.Delay(Bound)));
        Assert.False(cover.Holds);
    }

    /// <summary>A thread of its own holding the mutex until it is told to end.</summary>
    private sealed class Holder : IDisposable
    {
        private readonly ManualResetEventSlim _end = new();
        private readonly Thread _thread;
        private bool _release = true;
        private int _openError;

        /// <summary>
        /// A handle to the holding thread, signalled once the thread has ended. Disposed with
        /// the holder, or by a caller that keeps it past the holder.
        /// </summary>
        internal SafeWaitHandle ThreadHandle { get; private set; } = null!;

        internal Holder(string name)
        {
            using var holding = new ManualResetEventSlim();
            _thread = new Thread(() =>
            {
                ThreadHandle = OpenThread(Synchronize, false, GetCurrentThreadId());
                if (ThreadHandle.IsInvalid) _openError = Marshal.GetLastWin32Error();
                var mutex = new Mutex(initiallyOwned: false, name);
                mutex.WaitOne();
                holding.Set();
                _end.Wait();
                if (_release) mutex.ReleaseMutex();
                // Not disposed where it is left abandoned, so the mutex lives on past the
                // thread and the cover finds the abandoned one, not a fresh one.
                if (_release) mutex.Dispose();
                else GC.KeepAlive(s_abandoned = mutex);
            }) { IsBackground = true };
            _thread.Start();
            Assert.True(holding.Wait(Bound));
            Assert.False(ThreadHandle.IsInvalid, $"OpenThread failed with {_openError}.");
        }

        private const uint Synchronize = 0x00100000;

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern SafeWaitHandle OpenThread(uint desiredAccess, bool inheritHandle, uint threadId);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern uint GetCurrentThreadId();

        private static Mutex? s_abandoned;

        internal void End(bool release)
        {
            _release = release;
            _end.Set();
            _thread.Join();
        }

        public void Dispose()
        {
            if (_thread.IsAlive) End(release: true);
            ThreadHandle.Dispose();
        }
    }

    private static bool OnOtherThread(Func<bool> work)
    {
        var result = false;
        var thread = new Thread(() => result = work()) { IsBackground = true };
        thread.Start();
        thread.Join();
        return result;
    }
}
