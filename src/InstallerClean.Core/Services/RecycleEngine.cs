using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// Production <see cref="IRecycleEngine"/> over the shell
/// <c>IFileOperation</c> API via source-generated COM.
///
/// <c>IFileOperation</c> is STA-only, so every COM call runs on one
/// dedicated STA worker thread owned by this instance. The thread is
/// created on first use (so constructing the engine on a non-Windows
/// host, or before any delete, does no Windows-only work), initialises
/// its apartment once with <c>CoInitializeEx(STA)</c>, then executes
/// dispatched jobs from a queue until disposal. A silent
/// <c>PerformOperations</c> needs no message pump (verified on
/// Windows), so the worker just drains the queue. Registered as a DI
/// singleton; the container disposes it at shutdown, which drains the
/// queue and joins the thread.
/// </summary>
internal sealed class RecycleEngine : IRecycleEngine, IDisposable
{
    // The marshallers generated for [GeneratedComInterface] parameters
    // use their own default ComWrappers instance; this one is only for
    // bridging the raw CoCreateInstance pointer. A single shared
    // instance is fine because UniqueInstance wrappers are not cached on
    // it.
    private static readonly StrategyBasedComWrappers s_cw = new();

    private readonly object _gate = new();
    private BlockingCollection<Action>? _work;
    private Thread? _sta;
    private bool _disposed;

    private void EnsureStarted()
    {
        if (_work is not null) return;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_work is not null) return;

            var work = new BlockingCollection<Action>();
            var sta = new Thread(() => StaLoop(work))
            {
                IsBackground = true,
                Name = "InstallerClean.Recycle",
            };
            // STA is a Windows concept: SetApartmentState throws
            // PlatformNotSupportedException off Windows. That is correct,
            // the engine only does anything on Windows, and deferring it
            // to first use keeps construction host-agnostic.
            sta.SetApartmentState(ApartmentState.STA);
            sta.Start();

            _work = work;
            _sta = sta;
        }
    }

    private static void StaLoop(BlockingCollection<Action> work)
    {
        int hr = ShellRecycleNative.CoInitializeEx(IntPtr.Zero, ShellRecycleNative.COINIT_APARTMENTTHREADED);
        try
        {
            foreach (var job in work.GetConsumingEnumerable())
                job();
        }
        finally
        {
            // CoUninitialize once for the successful init (S_OK or
            // S_FALSE, both >= 0); skip it if the apartment never
            // initialised.
            if (hr >= 0)
                ShellRecycleNative.CoUninitialize();
        }
    }

    private T Run<T>(Func<T> func)
    {
        EnsureStarted();
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _work!.Add(() =>
        {
            try { tcs.SetResult(func()); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        // The caller is a thread-pool thread inside DeleteFilesAsync's
        // Task.Run, never the STA worker, so blocking here cannot
        // deadlock the worker.
        return tcs.Task.GetAwaiter().GetResult();
    }

    public bool CanRecycleToVolume(string anyPathOnThatVolume) =>
        Run(() => ProbeRecycle(anyPathOnThatVolume));

    public RecycleFileOutcome RecycleFile(string filePath) =>
        Run(() => RecycleOne(filePath));

    /// <summary>
    /// Control delete: place a throwaway file on the target volume,
    /// attempt the recycle, and report whether it actually reached the
    /// bin. Recycle behaviour is per-volume, so the probe file must sit
    /// on the same volume as the files being deleted.
    ///
    /// On the success path the probe is genuinely recycled, so it leaves
    /// the disk but DOES leave one ~1 KB self-describing entry (named
    /// ic-recycle-probe-*.tmp) in the volume's Recycle Bin. That residue
    /// is accepted, not removed: emptying the bin (SHEmptyRecycleBin)
    /// would destroy the user's own files, and hand-deleting the specific
    /// $R entry under $Recycle.Bin is fragile. An empirical recycle is the
    /// only reliable per-volume bin test, so the tiny named residue is the
    /// cost of answering that question correctly.
    /// </summary>
    private static bool ProbeRecycle(string anyPathOnThatVolume)
    {
        string? probe = CreateProbeFile(anyPathOnThatVolume);
        if (probe is null)
            return false; // could not place a probe on the target volume: cannot confirm, fail safe

        try
        {
            return RecycleOne(probe).Outcome == RecycleOutcome.Recycled;
        }
        catch
        {
            // The probe's contract is false-on-any-doubt, and the caller
            // (DeleteFilesService.CanRecycleToVolume) does not wrap this
            // call. Any unexpected throw from RecycleOne (for example a QI
            // failure casting the activated object to IFileOperation) must
            // degrade to "cannot recycle", not fault the whole delete with
            // a generic crash in place of the tailored RecycleUnavailable
            // guidance.
            return false;
        }
        finally
        {
            // Removes only the failed-without-nuke case, where the probe is
            // still on disk. A successful recycle leaves its entry in the
            // bin (see the summary above), so File.Delete is then a no-op:
            // the path no longer exists on disk.
            try { if (File.Exists(probe)) File.Delete(probe); }
            catch { /* best-effort cleanup of our own throwaway file */ }
        }
    }

    private static string? CreateProbeFile(string anyPathOnThatVolume)
    {
        try
        {
            string targetRoot = Path.GetPathRoot(Path.GetFullPath(anyPathOnThatVolume)) ?? string.Empty;

            // %TEMP% is on the system volume in the normal case (orphans
            // live under %SystemRoot%\Installer). If it is redirected to
            // another volume the probe would test the wrong bin, so fall
            // back to %SystemRoot%\Temp, which is on the system volume
            // and writable by an elevated process. If neither sits on
            // the target volume, give up rather than test the wrong one.
            string tempDir = Path.GetTempPath();
            if (!RootsEqual(Path.GetPathRoot(tempDir), targetRoot))
            {
                string sysTemp = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                if (RootsEqual(Path.GetPathRoot(sysTemp), targetRoot) && Directory.Exists(sysTemp))
                    tempDir = sysTemp;
                else
                    return null;
            }

            string probe = Path.Combine(tempDir, $"ic-recycle-probe-{Guid.NewGuid():N}.tmp");
            // Small but non-empty, matching the verified harness probe; a
            // zero-byte file is an unnecessary edge case for the bin.
            File.WriteAllBytes(probe, new byte[1024]);
            return probe;
        }
        catch
        {
            return null;
        }
    }

    private static bool RootsEqual(string? a, string? b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// One file, one IFileOperation. Activates the coclass, sets the
    /// recycle flags, deletes the item with a result sink, then reads
    /// the per-item outcome from the sink. The aggregate HRESULTs report
    /// success even on a permanent delete, so the verdict comes from the
    /// sink: it fired, the per-item code, and whether a bin item was
    /// created.
    /// </summary>
    private static RecycleFileOutcome RecycleOne(string filePath)
    {
        int hr = ShellRecycleNative.CoCreateInstance(
            in ShellRecycleNative.CLSID_FileOperation, IntPtr.Zero,
            ShellRecycleNative.CLSCTX_INPROC_SERVER, in ShellRecycleNative.IID_IFileOperation, out IntPtr pOp);
        if (hr < 0 || pOp == IntPtr.Zero)
            return new RecycleFileOutcome(RecycleOutcome.Failed, hr); // fail closed: never fall back to a permanent delete

        // GetOrCreateObjectForComInstance takes its own reference (it
        // QIs for IUnknown); the CoCreateInstance reference is still
        // ours to release. UniqueInstance keeps the wrapper out of the
        // cache and makes FinalRelease() release deterministically.
        object rcw = s_cw.GetOrCreateObjectForComInstance(pOp, CreateObjectFlags.UniqueInstance);
        Marshal.Release(pOp);

        var sink = new RecycleProgressSink();
        IntPtr psi = IntPtr.Zero;
        try
        {
            var op = (IFileOperation)rcw;

            int shr = op.SetOperationFlags(ShellRecycleNative.RecycleFlags);
            if (shr < 0) return new RecycleFileOutcome(RecycleOutcome.Failed, shr);

            int chr = ShellRecycleNative.SHCreateItemFromParsingName(
                filePath, IntPtr.Zero, in ShellRecycleNative.IID_IShellItem, out psi);
            if (chr < 0 || psi == IntPtr.Zero)
                return new RecycleFileOutcome(RecycleOutcome.Failed, chr);

            int dhr = op.DeleteItem(psi, sink);
            if (dhr < 0) return new RecycleFileOutcome(RecycleOutcome.Failed, dhr);

            int phr = op.PerformOperations();
            // Win32 documents calling this after PerformOperations
            // regardless of its result; the per-item verdict comes from
            // the sink, not this aggregate flag.
            op.GetAnyOperationsAborted(out _);

            if (!sink.Fired)
                // The sink never fired, so the delete did not really run;
                // report the aggregate failure code (or the sentinel).
                return new RecycleFileOutcome(RecycleOutcome.Failed, phr < 0 ? phr : sink.HrDelete);
            if (sink.HrDelete < 0)
                return new RecycleFileOutcome(RecycleOutcome.Failed, sink.HrDelete);

            return sink.Recycled
                ? new RecycleFileOutcome(RecycleOutcome.Recycled, sink.HrDelete)
                : new RecycleFileOutcome(RecycleOutcome.PermanentlyDeleted, sink.HrDelete);
        }
        finally
        {
            if (psi != IntPtr.Zero) Marshal.Release(psi);
            // ComObject is not IDisposable; FinalRelease() releases the
            // wrapper's own reference and works because the wrapper was
            // created with UniqueInstance.
            if (rcw is ComObject co) co.FinalRelease();
            // The sink must outlive PerformOperations: the shell holds
            // its CCW across the call, and its fields are read above.
            GC.KeepAlive(sink);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
        }
        _work?.CompleteAdding();
        _sta?.Join();
        _work?.Dispose();
    }
}

/// <summary>
/// Result sink for one recycle. Only <see cref="PostDeleteItem"/> does
/// anything; the rest are no-ops the shell may still call. The defaults
/// cannot impersonate a real verdict: if the sink never fires
/// <see cref="HrDelete"/> stays a sentinel and <see cref="Recycled"/>
/// stays false, so the engine reads "did not run", not a plausible
/// success.
/// </summary>
[GeneratedComClass]
internal sealed partial class RecycleProgressSink : IFileOperationProgressSink
{
    public bool Fired;
    public int HrDelete = unchecked((int)0xDEADBEEF);
    public bool Recycled;

    public void PostDeleteItem(uint dwFlags, IntPtr psiItem, int hrDelete, IntPtr psiNewlyCreated)
    {
        Fired = true;
        HrDelete = hrDelete;
        // Non-null psiNewlyCreated is the only reliable "actually
        // recycled" signal: a new bin item was created. Null means the
        // file was permanently deleted.
        Recycled = psiNewlyCreated != IntPtr.Zero;
    }

    public void StartOperations() { }
    public void FinishOperations(int hrResult) { }
    public void PreRenameItem(uint dwFlags, IntPtr psiItem, IntPtr pszNewName) { }
    public void PostRenameItem(uint dwFlags, IntPtr psiItem, IntPtr pszNewName, int hrRename, IntPtr psiNewlyCreated) { }
    public void PreMoveItem(uint dwFlags, IntPtr psiItem, IntPtr psiDestFolder, IntPtr pszNewName) { }
    public void PostMoveItem(uint dwFlags, IntPtr psiItem, IntPtr psiDestFolder, IntPtr pszNewName, int hrMove, IntPtr psiNewlyCreated) { }
    public void PreCopyItem(uint dwFlags, IntPtr psiItem, IntPtr psiDestFolder, IntPtr pszNewName) { }
    public void PostCopyItem(uint dwFlags, IntPtr psiItem, IntPtr psiDestFolder, IntPtr pszNewName, int hrCopy, IntPtr psiNewlyCreated) { }
    public void PreDeleteItem(uint dwFlags, IntPtr psiItem) { }
    public void PreNewItem(uint dwFlags, IntPtr psiDestFolder, IntPtr pszNewName) { }
    public void PostNewItem(uint dwFlags, IntPtr psiDestFolder, IntPtr pszNewName, IntPtr pszTemplateName, uint dwFileAttributes, int hrNew, IntPtr psiNewItem) { }
    public void UpdateProgress(uint iWorkTotal, uint iWorkSoFar) { }
    public void ResetTimer() { }
    public void PauseTimer() { }
    public void ResumeTimer() { }
}
