using InstallerClean.Services;
using Xunit;

namespace InstallerClean.Tests.Services;

/// <summary>
/// A fake <see cref="IInstallerTransactionApi"/> that records every call and
/// answers each begin from a queue of codes. An end made while it holds no open
/// transaction of its own is recorded as a stray end, which is the one call the
/// hold must never make.
/// </summary>
public sealed class FakeInstallerTransactionApi : IInstallerTransactionApi
{
    private readonly Queue<uint> _beginCodes;

    public FakeInstallerTransactionApi(params uint[] beginCodes) =>
        _beginCodes = new Queue<uint>(beginCodes);

    public int Begins { get; private set; }
    public int Commits { get; private set; }
    public int StrayEnds { get; private set; }
    public int Releases { get; private set; }
    public bool Open { get; private set; }
    public uint CommitCode { get; set; }
    public Exception? BeginThrows { get; set; }
    public List<string> Names { get; } = new();

    /// <summary>Run inside each commit, so a test can see the order of calls.</summary>
    public Action? OnCommit { get; set; }

    public uint Begin(string name, out InstallerTransactionHandles handles)
    {
        Begins++;
        Names.Add(name);
        if (BeginThrows is not null)
            throw BeginThrows;
        var code = _beginCodes.Count > 0 ? _beginCodes.Dequeue() : 0u;
        handles = code == 0 ? new InstallerTransactionHandles(42, null) : default;
        if (code == 0)
            Open = true;
        return code;
    }

    public uint Commit()
    {
        if (!Open)
            StrayEnds++;
        Commits++;
        Open = false;
        OnCommit?.Invoke();
        return CommitCode;
    }

    public void Release(InstallerTransactionHandles handles) => Releases++;
}

public class InstallerHoldTests
{
    [Fact]
    public void A_begin_that_succeeds_gives_a_hold_and_its_dispose_ends_it_once()
    {
        var api = new FakeInstallerTransactionApi(0);

        var hold = InstallerHold.TryBegin(api, "name", out var start, out var code);

        Assert.NotNull(hold);
        Assert.Equal(InstallerHoldStart.Held, start);
        Assert.Equal(0u, code);
        Assert.True(api.Open);
        Assert.Equal(0, api.Commits);

        hold!.Dispose();
        hold.Dispose();

        Assert.Equal(1, api.Commits);
        Assert.Equal(1, api.Releases);
        Assert.Equal(0, api.StrayEnds);
        Assert.Equal(0u, hold.EndCode);
    }

    [Theory]
    [InlineData(1618u, InstallerHoldStart.InstallerBusy)]
    [InlineData(1653u, InstallerHoldStart.RollbackDisabled)]
    [InlineData(1601u, InstallerHoldStart.Failed)]
    [InlineData(87u, InstallerHoldStart.Failed)]
    public void A_begin_that_fails_gives_no_hold_and_never_ends(uint beginCode, InstallerHoldStart expected)
    {
        var api = new FakeInstallerTransactionApi(beginCode);

        var hold = InstallerHold.TryBegin(api, "name", out var start, out var code);

        Assert.Null(hold);
        Assert.Equal(expected, start);
        Assert.Equal(beginCode, code);
        Assert.Equal(1, api.Begins);
        Assert.Equal(0, api.Commits);
        Assert.Equal(0, api.Releases);
    }

    [Fact]
    public void An_exception_inside_a_using_still_ends_the_hold_once()
    {
        var api = new FakeInstallerTransactionApi(0);

        void ThrowInsideTheHold()
        {
            using var hold = InstallerHold.TryBegin(api, "name", out _, out _);
            throw new InvalidOperationException("thrown inside the hold");
        }

        Assert.Throws<InvalidOperationException>(ThrowInsideTheHold);

        Assert.Equal(1, api.Commits);
        Assert.Equal(0, api.StrayEnds);
        Assert.False(api.Open);
    }

    [Fact]
    public void An_end_that_fails_is_kept_on_the_hold_and_not_thrown()
    {
        var api = new FakeInstallerTransactionApi(0) { CommitCode = 5 };
        var hold = InstallerHold.TryBegin(api, "name", out _, out _)!;

        hold.Dispose();

        Assert.Equal(5u, hold.EndCode);
        Assert.False(hold.Ended);
        Assert.Equal(1, api.Releases);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(3010u)]
    [InlineData(1641u)]
    public void An_end_returning_a_success_code_is_ended(uint commitCode)
    {
        var api = new FakeInstallerTransactionApi(0) { CommitCode = commitCode };
        var hold = InstallerHold.TryBegin(api, "name", out _, out _)!;

        hold.Dispose();

        Assert.True(hold.Ended);
    }

    [Fact]
    public void A_begin_that_throws_is_reported_as_failed_and_never_ends()
    {
        var api = new FakeInstallerTransactionApi { BeginThrows = new EntryPointNotFoundException("no export") };

        var hold = InstallerHold.TryBegin(api, "name", out var start, out var code);

        Assert.Null(hold);
        Assert.Equal(InstallerHoldStart.Failed, start);
        Assert.Equal(InstallerHold.BeginNotMade, code);
        Assert.Equal(0, api.Commits);
    }

    [Fact]
    public void The_name_given_is_the_name_Windows_is_asked_for()
    {
        var api = new FakeInstallerTransactionApi(0);

        using var hold = InstallerHold.TryBegin(api, "the transaction's name", out _, out _);

        Assert.Equal(new[] { "the transaction's name" }, api.Names);
    }
}

public class InProcessTransactionGuardTests
{
    private static InProcessTransactionGuard Guard(Func<uint> beginCode, Func<uint> commitCode) =>
        new((string name, out uint transaction, out Microsoft.Win32.SafeHandles.SafeWaitHandle ev) =>
            {
                transaction = 1;
                ev = new Microsoft.Win32.SafeHandles.SafeWaitHandle(IntPtr.Zero, ownsHandle: false);
                return beginCode();
            },
            commitCode);

    [Fact]
    public void A_second_begin_while_one_is_open_throws_without_reaching_Windows()
    {
        var calls = 0;
        var guard = Guard(() => { calls++; return 0; }, () => 0);

        guard.Begin("first", out _);

        Assert.Throws<InvalidOperationException>(() => guard.Begin("second", out _));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void A_begin_that_fails_leaves_the_guard_open_for_the_next()
    {
        var codes = new Queue<uint>(new[] { 1618u, 0u });
        var guard = Guard(() => codes.Dequeue(), () => 0);

        Assert.Equal(1618u, guard.Begin("busy", out _));
        Assert.False(guard.Open);
        Assert.Equal(0u, guard.Begin("again", out _));
        Assert.True(guard.Open);
    }

    [Fact]
    public void A_begin_that_throws_leaves_the_guard_open_for_the_next()
    {
        var throwNext = true;
        var guard = Guard(() => throwNext ? throw new DllNotFoundException("msi") : 0u, () => 0);

        Assert.Throws<DllNotFoundException>(() => guard.Begin("first", out _));
        Assert.False(guard.Open);
        throwNext = false;
        Assert.Equal(0u, guard.Begin("second", out _));
    }

    [Fact]
    public void An_end_that_succeeds_lets_the_next_begin_through()
    {
        var guard = Guard(() => 0, () => 0);

        guard.Begin("first", out _);
        guard.Commit();

        Assert.False(guard.Open);
        Assert.Equal(0u, guard.Begin("second", out _));
    }

    [Fact]
    public void An_end_that_fails_keeps_every_later_begin_refused()
    {
        var guard = Guard(() => 0, () => 5);

        guard.Begin("first", out _);
        guard.Commit();

        Assert.True(guard.Open);
        Assert.Throws<InvalidOperationException>(() => guard.Begin("second", out _));
    }
}
