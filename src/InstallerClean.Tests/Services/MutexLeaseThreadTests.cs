using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Runtime.CompilerServices;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The rule the whole machine-wide hold rests on: the <c>Global\_MSIExecute</c> lease
/// is released on the thread that took it.
///
/// WHY IT IS A RULE AND NOT A DETAIL. Win32 requires the owning thread to release a
/// mutex. Both action services take the lease inside a <c>Task.Run</c> whose body is
/// synchronous for exactly that reason, and both say so in a comment at the acquire
/// and again at the release; the command line's <c>static int Main</c> carries the same
/// constraint for its own single-instance mutex and states it at the sync-over-async
/// wrapper that preserves it.
///
/// WHY THE REGRESSION IS SILENT. Turning <c>Task.Run(() =&gt; ...)</c> into
/// <c>Task.Run(async () =&gt; ...)</c> binds the unwrapping overload, so the method's
/// signature and return type do not move and the build stays green with no warning.
/// Any await in the body can then resume on another pool thread, and
/// <c>MutexProbe.MutexLease.Dispose</c> catches and swallows the wrong-thread
/// <c>ApplicationException</c> by design, because a release that throws after a
/// committed batch must not take the process down. So the machine-wide installer mutex
/// would stay held until the process exits, every installer on the box that wants it
/// would wait or fail with 1618, and there would be no exception, no log line and
/// nothing red. The cost falls on the instruments rather than on the app, which is the
/// shape that reads exactly like nothing being wrong.
///
/// TWO TESTS, BECAUSE ONE OF THEM CANNOT BE TRUSTED ALONE. The behavioural test below
/// runs the real batch and compares the two thread identities, which is the invariant
/// stated directly. But an await only makes a hop LIKELY: a task that has already
/// completed by the time it is awaited resumes synchronously, so the continuation stays
/// where it was, and that test alone can therefore pass with the fault present. A test
/// that finds a planted fault half the time is not a guard.
///
/// So the structural test is the deterministic half, and it is what actually holds this
/// invariant: the regression is a compiler-generated async state machine arising from
/// the lease-holding method, and that either exists in the assembly or does not.
/// NEITHER OF THE TWO IS REDUNDANT BESIDE THE OTHER, AND THE STRUCTURAL ONE IS THE ONE
/// THAT LOOKS IT, being a reflection walk over type names sitting beside a test that
/// exercises the real batch. Remove it and the invariant is left to a comparison that
/// only fires when the scheduler happens to move the continuation.
///
/// The behavioural one stays because it says the rule in the rule's own terms, it can
/// never be a false red, and it reaches restructurings the name-keyed walk cannot see.
/// </summary>
public class MutexLeaseThreadTests
{
    private const string Cache = @"C:\Windows\Installer";
    private const string Destination = @"D:\InstallerClean backup";

    /// <summary>
    /// Drives a batch the under-lease re-read empties, which is a real completion path
    /// (it is what ShowReverifyAllSkipped exists for) and is the one route through the
    /// hold that touches no filesystem gate: both services return on it before
    /// resolving the cache root, so the whole acquire-work-release sequence runs
    /// wherever the suite runs rather than only on Windows.
    /// </summary>
    private static (MockFileSystem Fs, string Path, FakeMutexProbe Mutex, FakeReclaimingReverifier Reverifier)
        BatchTheReReadEmpties()
    {
        var fs = new MockFileSystem();
        var path = $@"{Cache}\held-back.msp";
        fs.AddFile(path, new MockFileData("payload"));
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.Acquire);
        return (fs, path, mutex, new FakeReclaimingReverifier(new[] { path }, mutex));
    }

    [Fact]
    public async Task A_delete_releases_the_installer_mutex_on_the_thread_that_took_it()
    {
        var (fs, path, mutex, reverifier) = BatchTheReReadEmpties();
        var service = new DeleteFilesService(fs, mutex, installerFolderOverride: null, reverifier);

        await service.DeleteFilesAsync(new[] { path }, UnderLeaseClaims.None);

        AssertReleasedWhereItWasTaken(mutex);
    }

    [Fact]
    public async Task A_move_releases_the_installer_mutex_on_the_thread_that_took_it()
    {
        var (fs, path, mutex, reverifier) = BatchTheReReadEmpties();
        var service = new MoveFilesService(fs, mutex, installerFolderOverride: null, reverifier);

        await service.MoveFilesAsync(new[] { path }, Destination, UnderLeaseClaims.None);

        AssertReleasedWhereItWasTaken(mutex);
    }

    private static void AssertReleasedWhereItWasTaken(FakeMutexProbe mutex)
    {
        // The counters first, so a batch that never took the lease at all cannot pass
        // this by leaving both thread readings null and equal.
        Assert.Equal(1, mutex.Acquired);
        Assert.Equal(1, mutex.Released);
        Assert.NotNull(mutex.AcquiredOnThread);

        Assert.True(mutex.AcquiredOnThread == mutex.ReleasedOnThread,
            $"The installer mutex was taken on thread {mutex.AcquiredOnThread} and released on "
            + $"{mutex.ReleasedOnThread}. Win32 requires the owning thread to release, and the "
            + "production lease swallows the exception a wrong-thread release raises, so the "
            + "machine-wide mutex would be left held until the process exits with nothing "
            + "reported. Nothing between the acquire and the release may await.");
    }

    /// <summary>
    /// Every method the two services hold the lease across. Written out rather than
    /// discovered because there is nothing in the metadata that says which method takes
    /// a mutex; <c>nameof</c> keeps a rename from leaving this pointing at nothing.
    /// </summary>
    public static TheoryData<Type, string> LeaseHolders() => new()
    {
        { typeof(DeleteFilesService), nameof(DeleteFilesService.DeleteFilesAsync) },
        { typeof(MoveFilesService), nameof(MoveFilesService.MoveFilesAsync) },
    };

    [Theory]
    [MemberData(nameof(LeaseHolders))]
    public void The_lease_holding_method_compiles_to_no_async_state_machine(Type service, string method)
    {
        // The deterministic half. An await anywhere between the acquire and the release
        // means the compiler built a state machine for the method that holds the lease,
        // and the naming derives from that method: an async lambda in the body yields
        // DeleteFilesService+<>c__DisplayClass7_0+<<DeleteFilesAsync>b__0>d, and making
        // the method itself async yields DeleteFilesService+<DeleteFilesAsync>d__7.
        // Both carry the method's name in angle brackets.
        //
        // THE WALK HAS TO RECURSE. The display class is a nested type of the service and
        // the state machine is a nested type of the DISPLAY CLASS, so a single
        // GetNestedTypes stops one level above the thing it is looking for, finds the
        // fault absent and says so.
        //
        // WHAT IT DOES NOT REACH, said here rather than left to be discovered. An await
        // moved out into a separate async helper that the body merely calls would put
        // the state machine under the HELPER's name and pass this. That is a different
        // and much more visible edit than adding one keyword to a lambda, which is the
        // regression this is aimed at.
        var machines = NestedTypes(service)
            .Where(t => typeof(IAsyncStateMachine).IsAssignableFrom(t))
            .Where(t => t.FullName!.Contains($"<{method}>", StringComparison.Ordinal))
            .Select(t => t.FullName!)
            .ToArray();

        Assert.True(machines.Length == 0,
            $"{service.Name}.{method} compiled to an async state machine ({string.Join(", ", machines)}), "
            + "so something in it awaits. It holds the machine-wide installer mutex across its body "
            + "and Win32 requires the owning thread to release, so a continuation that resumes "
            + "elsewhere leaves the mutex held for the life of the process with nothing reported.");
    }

    [Fact]
    public void The_state_machine_walk_can_see_one()
    {
        // The must-hit control. Everything above reports zero, and a walk that cannot
        // find a state machine anywhere reports zero for the same reason whether or not
        // one is there. FileSystemScanService.ScanAsync is a genuinely async method in
        // the same assembly, so its machine is a thing this walk must be able to reach.
        var found = NestedTypes(typeof(FileSystemScanService))
            .Where(t => typeof(IAsyncStateMachine).IsAssignableFrom(t))
            .Where(t => t.FullName!.Contains($"<{nameof(FileSystemScanService.ScanAsync)}>", StringComparison.Ordinal))
            .Select(t => t.FullName!)
            .ToArray();

        Assert.True(found.Length > 0,
            "The walk found no async state machine for a method that is written async, so its zero "
            + "everywhere else says nothing about the services. Either the compiler's naming has "
            + "moved or the recursion is not reaching far enough.");
    }

    /// <summary>
    /// <paramref name="type"/>'s nested types at every depth. A lambda's state machine
    /// is nested inside the display class that captures for it, so a single
    /// <c>GetNestedTypes</c> stops one level short of the thing being looked for.
    /// </summary>
    private static IEnumerable<Type> NestedTypes(Type type)
    {
        foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            yield return nested;
            foreach (var deeper in NestedTypes(nested))
                yield return deeper;
        }
    }
}
