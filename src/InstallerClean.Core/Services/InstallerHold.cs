using InstallerClean.Interop.Native;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Services;

/// <summary>
/// The two calls a hold makes on Windows Installer, behind an interface so the action
/// services can be tested without opening a real transaction on the machine running
/// the suite.
/// </summary>
public interface IInstallerTransactionApi
{
    /// <summary><c>MsiBeginTransaction</c>: the Windows Installer return code, and the handles on success.</summary>
    uint Begin(string name, out InstallerTransactionHandles handles);

    /// <summary><c>MsiEndTransaction</c> with the commit state. Takes no handle; see <see cref="InstallerHold"/>.</summary>
    uint Commit();

    /// <summary>Closes the two handles a successful <see cref="Begin"/> returned.</summary>
    void Release(InstallerTransactionHandles handles);
}

/// <summary>The transaction handle and the change-of-owner event a successful begin returns.</summary>
public readonly record struct InstallerTransactionHandles(uint Transaction, SafeWaitHandle? ChangeOfOwnerEvent);

/// <summary>
/// The production <see cref="IInstallerTransactionApi"/>, straight onto msi.dll.
///
/// One transaction at a time in the process. A begin while this process's own
/// transaction may still be open throws rather than reaching Windows: Windows would
/// refuse it as busy, and the caller would read its own transaction as somebody
/// else's installation. The guard opens again only after an end that succeeded, so a
/// failed end leaves every later begin in the process refused, and the transaction is
/// ended by Windows when the process exits.
/// </summary>
internal sealed class InstallerTransactionApi : IInstallerTransactionApi
{
    /// <summary>msi.h's MSITRANSACTIONSTATE_COMMIT.</summary>
    private const uint CommitState = 1;

    private static readonly InProcessTransactionGuard Guard = new(
        (string name, out uint transaction, out SafeWaitHandle changeOfOwnerEvent) =>
            Msi.MsiBeginTransaction(name, 0, out transaction, out changeOfOwnerEvent),
        () => Msi.MsiEndTransaction(CommitState));

    public uint Begin(string name, out InstallerTransactionHandles handles) => Guard.Begin(name, out handles);

    public uint Commit() => Guard.Commit();

    public void Release(InstallerTransactionHandles handles)
    {
        if (handles.Transaction != 0)
            Msi.MsiCloseHandle(handles.Transaction);
        handles.ChangeOfOwnerEvent?.Dispose();
    }
}

/// <summary>
/// The one-transaction-per-process rule of <see cref="InstallerTransactionApi"/>, over
/// two delegates so it can be tested without msi.dll.
/// </summary>
internal sealed class InProcessTransactionGuard
{
    internal delegate uint BeginCall(string name, out uint transaction, out SafeWaitHandle changeOfOwnerEvent);

    private readonly BeginCall _begin;
    private readonly Func<uint> _commit;
    private int _open;

    internal InProcessTransactionGuard(BeginCall begin, Func<uint> commit)
    {
        _begin = begin;
        _commit = commit;
    }

    internal bool Open => Volatile.Read(ref _open) != 0;

    internal uint Begin(string name, out InstallerTransactionHandles handles)
    {
        if (Interlocked.CompareExchange(ref _open, 1, 0) != 0)
            throw new InvalidOperationException(
                "A Windows Installer transaction of this process may still be open.");
        try
        {
            var code = _begin(name, out var transaction, out var changeOfOwnerEvent);
            handles = new InstallerTransactionHandles(transaction, changeOfOwnerEvent);
            if (code != 0)
                Interlocked.Exchange(ref _open, 0);
            return code;
        }
        catch
        {
            Interlocked.Exchange(ref _open, 0);
            throw;
        }
    }

    internal uint Commit()
    {
        var code = _commit();
        if (InstallerHold.EndSucceeded(code))
            Interlocked.Exchange(ref _open, 0);
        return code;
    }
}

/// <summary>How an attempt to take the hold ended.</summary>
public enum InstallerHoldStart
{
    /// <summary>The transaction is open and this process owns it.</summary>
    Held,

    /// <summary>
    /// ERROR_INSTALL_ALREADY_RUNNING: an installation or another transaction is in
    /// progress. Nothing was opened, and the caller may try again later.
    /// </summary>
    InstallerBusy,

    /// <summary>
    /// ERROR_ROLLBACK_DISABLED: the DisableRollback policy is set on this machine, and
    /// Windows Installer opens no transaction while it is.
    /// </summary>
    RollbackDisabled,

    /// <summary>Any other code, or a begin that could not be made at all. Nothing was opened.</summary>
    Failed,
}

/// <summary>
/// An empty Windows Installer transaction owned by this process. While one is open
/// the Windows Installer service starts no installation, patch application or patch
/// removal on the machine, so Windows Installer's records cannot change; that is the
/// window the action services act in.
///
/// END IS CALLED EXACTLY ONCE, AND ONLY AFTER A BEGIN THAT RETURNED SUCCESS.
/// <c>MsiEndTransaction</c> takes no handle, so an end with no transaction of this
/// process's own open reaches the operation the service is running instead.
/// <see cref="TryBegin"/> returns a hold only for a begin that succeeded, and
/// <see cref="Dispose"/> is the only place the end is made. Use it in a <c>using</c>
/// on one thread, with no await inside, so every exit path reaches the end and the
/// end comes from the thread that began.
///
/// While a hold is open, every installer on the machine is turned away, so nothing
/// inside one may wait on a person, the console, the network or a share. A begin can
/// itself take many seconds while Windows makes a System Restore point, so it is never
/// made on a thread a window paints from.
/// </summary>
public sealed class InstallerHold : IDisposable
{
    /// <summary>ERROR_INSTALL_ALREADY_RUNNING.</summary>
    public const uint ErrorInstallAlreadyRunning = 1618;

    /// <summary>ERROR_ROLLBACK_DISABLED.</summary>
    public const uint ErrorRollbackDisabled = 1653;

    /// <summary>The code <see cref="TryBegin"/> reports for a begin that threw before Windows answered.</summary>
    public const uint BeginNotMade = uint.MaxValue;

    private readonly IInstallerTransactionApi _api;
    private readonly InstallerTransactionHandles _handles;
    private int _ended;

    private InstallerHold(IInstallerTransactionApi api, InstallerTransactionHandles handles)
    {
        _api = api;
        _handles = handles;
    }

    /// <summary>
    /// The code <c>MsiEndTransaction</c> returned, or null where the end has not been
    /// made or did not return.
    /// </summary>
    public uint? EndCode { get; private set; }

    /// <summary>
    /// True once the end has returned success. False before the end, and after one that
    /// failed or threw, after which this process starts no further hold.
    /// </summary>
    public bool Ended { get; private set; }

    /// <summary>
    /// Success, and the two success codes that also ask for a restart, which an empty
    /// transaction has nothing to restart for but which Windows Installer reports as success.
    /// </summary>
    internal static bool EndSucceeded(uint code) => code is 0 or 3010 or 1641;

    /// <summary>
    /// Begins a transaction named <paramref name="name"/>. Returns the hold when the
    /// begin succeeded, and null with <paramref name="start"/> saying why otherwise;
    /// <paramref name="code"/> is the Windows Installer return code, or
    /// <see cref="BeginNotMade"/> where the call threw. Never throws.
    /// </summary>
    public static InstallerHold? TryBegin(
        IInstallerTransactionApi api, string name, out InstallerHoldStart start, out uint code)
    {
        InstallerTransactionHandles handles;
        try
        {
            code = api.Begin(name, out handles);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            Helpers.CrashLog.TryWrite(ex);
            code = BeginNotMade;
            start = InstallerHoldStart.Failed;
            return null;
        }

        if (code == 0)
        {
            start = InstallerHoldStart.Held;
            return new InstallerHold(api, handles);
        }

        start = code switch
        {
            ErrorInstallAlreadyRunning => InstallerHoldStart.InstallerBusy,
            ErrorRollbackDisabled => InstallerHoldStart.RollbackDisabled,
            _ => InstallerHoldStart.Failed,
        };
        return null;
    }

    /// <summary>
    /// Ends the transaction (commit) and closes its handles. A second call does nothing.
    /// A failing end is recorded in the crash log and not thrown: by the time it ends,
    /// the files acted on inside it are already moved or deleted. The caller reads
    /// <see cref="Ended"/> and starts no further hold where it is false.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _ended, 1) != 0)
            return;
        try
        {
            var code = _api.Commit();
            EndCode = code;
            Ended = EndSucceeded(code);
            if (!Ended)
                Helpers.CrashLog.TryWrite(new InvalidOperationException(
                    $"MsiEndTransaction returned {code}."));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            Helpers.CrashLog.TryWrite(ex);
        }
        finally
        {
            try { _api.Release(_handles); }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Helpers.CrashLog.TryWrite(ex);
            }
        }
    }
}
