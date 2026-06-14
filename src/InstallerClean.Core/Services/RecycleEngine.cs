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
    // volatile: read outside _gate by EnsureStarted's lock-free fast path and
    // Run's disposed check. The release/acquire pairing publishes a
    // fully-constructed BlockingCollection on a weakly-ordered target
    // (Windows-on-ARM64) and makes a post-Dispose _disposed write visible to a
    // racing Run.
    private volatile BlockingCollection<Action>? _work;
    private volatile Thread? _sta;
    private volatile bool _disposed;

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
        // Dispose does not null _work, so EnsureStarted's fast path would return
        // on a disposed engine and the Add below would throw
        // InvalidOperationException ("marked as complete"). Fail with the expected
        // ObjectDisposedException instead; DeleteFilesService already degrades a
        // recycle fault to the RecycleUnavailable / per-file-error path.
        ObjectDisposedException.ThrowIf(_disposed, this);
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
        Run(() => RecycleOne(filePath, captureBinItem: false).Outcome);

    /// <summary>
    /// Control delete: place a throwaway file on the target volume,
    /// attempt the recycle, and report whether it actually reached the
    /// bin. Recycle behaviour is per-volume, so the probe file must sit
    /// on the same volume as the files being deleted. An empirical
    /// recycle is the only reliable per-volume bin test.
    ///
    /// On the success path the probe is genuinely recycled, so a ~1 KB
    /// entry (named ic-recycle-probe-*.tmp) appears in the volume's
    /// Recycle Bin; the sink captures the exact IShellItem the shell
    /// reports creating and the entry is then permanently deleted, so a
    /// successful probe leaves nothing behind. Identity-addressed
    /// cleanup is the safe form: emptying the bin (SHEmptyRecycleBin)
    /// would destroy the user's own binned files and guessing at $R
    /// names under $Recycle.Bin is unreliable, but deleting the very
    /// item the shell handed back can only ever remove the probe. The
    /// cleanup is best-effort; if it fails the entry stays in the bin,
    /// which is harmless.
    /// </summary>
    private static bool ProbeRecycle(string anyPathOnThatVolume)
    {
        string? probe = CreateProbeFile(anyPathOnThatVolume);
        if (probe is null)
            return false; // could not place a probe on the target volume: cannot confirm, fail safe

        IntPtr binItem = IntPtr.Zero;
        try
        {
            var (outcome, captured) = RecycleOne(probe, captureBinItem: true);
            binItem = captured;
            bool recycled = outcome.Outcome == RecycleOutcome.Recycled;
            if (recycled && binItem != IntPtr.Zero)
                DeleteBinItemPermanently(binItem);
            return recycled;
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
            if (binItem != IntPtr.Zero) Marshal.Release(binItem);
            // Removes only the failed-without-nuke case, where the probe is
            // still on disk. A successful recycle takes it off the disk, so
            // File.Delete is then a no-op: the path no longer exists.
            try { if (File.Exists(probe)) File.Delete(probe); }
            catch { /* best-effort cleanup of our own throwaway file */ }
        }
    }

    /// <summary>
    /// Permanently removes the bin entry a successful probe created,
    /// addressed by the exact IShellItem the shell reported in
    /// PostDeleteItem, never by a guessed path, so it can only ever
    /// remove the probe's own ~1 KB entry. Same flags as a recycle minus
    /// FOFX_RECYCLEONDELETE: deleting an item already in the bin without
    /// the recycle flag removes it for good. Failures are swallowed; the
    /// probe's verdict stands either way and a surviving entry is just
    /// the probe's own throwaway file left in the bin, which is harmless.
    /// </summary>
    private static void DeleteBinItemPermanently(IntPtr psiBinItem)
    {
        int hr = ShellRecycleNative.CoCreateInstance(
            in ShellRecycleNative.CLSID_FileOperation, IntPtr.Zero,
            ShellRecycleNative.CLSCTX_INPROC_SERVER, in ShellRecycleNative.IID_IFileOperation, out IntPtr pOp);
        if (hr < 0 || pOp == IntPtr.Zero)
            return;

        object rcw = s_cw.GetOrCreateObjectForComInstance(pOp, CreateObjectFlags.UniqueInstance);
        Marshal.Release(pOp);
        try
        {
            var op = (IFileOperation)rcw;
            if (op.SetOperationFlags(ShellRecycleNative.PermanentDeleteFlags) < 0) return;
            if (op.DeleteItem(psiBinItem, null) < 0) return;
            op.PerformOperations();
        }
        catch
        {
            // Best-effort by design; see the summary.
        }
        finally
        {
            if (rcw is ComObject co) co.FinalRelease();
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
    /// created. With <paramref name="captureBinItem"/> the returned
    /// BinItem is the AddRef'd IShellItem of the created bin entry
    /// (IntPtr.Zero when none was created); ownership transfers to the
    /// caller, which must Release a non-zero value.
    /// </summary>
    private static (RecycleFileOutcome Outcome, IntPtr BinItem) RecycleOne(string filePath, bool captureBinItem)
    {
        int hr = ShellRecycleNative.CoCreateInstance(
            in ShellRecycleNative.CLSID_FileOperation, IntPtr.Zero,
            ShellRecycleNative.CLSCTX_INPROC_SERVER, in ShellRecycleNative.IID_IFileOperation, out IntPtr pOp);
        if (hr < 0 || pOp == IntPtr.Zero)
            return (new RecycleFileOutcome(RecycleOutcome.Failed, hr), IntPtr.Zero); // fail closed: never fall back to a permanent delete

        // GetOrCreateObjectForComInstance takes its own reference (it
        // QIs for IUnknown); the CoCreateInstance reference is still
        // ours to release. UniqueInstance keeps the wrapper out of the
        // cache and makes FinalRelease() release deterministically.
        object rcw = s_cw.GetOrCreateObjectForComInstance(pOp, CreateObjectFlags.UniqueInstance);
        Marshal.Release(pOp);

        var sink = new RecycleProgressSink(captureBinItem);
        IntPtr psi = IntPtr.Zero;
        try
        {
            var op = (IFileOperation)rcw;

            int shr = op.SetOperationFlags(ShellRecycleNative.RecycleFlags);
            if (shr < 0) return (new RecycleFileOutcome(RecycleOutcome.Failed, shr), sink.NewItem);

            int chr = ShellRecycleNative.SHCreateItemFromParsingName(
                filePath, IntPtr.Zero, in ShellRecycleNative.IID_IShellItem, out psi);
            if (chr < 0 || psi == IntPtr.Zero)
                return (new RecycleFileOutcome(RecycleOutcome.Failed, chr), sink.NewItem);

            int dhr = op.DeleteItem(psi, sink);
            if (dhr < 0) return (new RecycleFileOutcome(RecycleOutcome.Failed, dhr), sink.NewItem);

            int phr = op.PerformOperations();
            // Win32 documents calling this after PerformOperations
            // regardless of its result; the per-item verdict comes from
            // the sink, not this aggregate flag.
            op.GetAnyOperationsAborted(out _);

            if (!sink.Fired)
                // The sink never fired, so the delete did not really run;
                // report the aggregate failure code (or the sentinel).
                return (new RecycleFileOutcome(RecycleOutcome.Failed, phr < 0 ? phr : sink.HrDelete), sink.NewItem);
            if (sink.HrDelete < 0)
                return (new RecycleFileOutcome(RecycleOutcome.Failed, sink.HrDelete), sink.NewItem);

            return (sink.Recycled
                ? new RecycleFileOutcome(RecycleOutcome.Recycled, sink.HrDelete)
                : new RecycleFileOutcome(RecycleOutcome.PermanentlyDeleted, sink.HrDelete), sink.NewItem);
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
    private readonly bool _captureNewItem;

    public RecycleProgressSink(bool captureNewItem = false) => _captureNewItem = captureNewItem;

    public bool Fired;
    public int HrDelete = unchecked((int)0xDEADBEEF);
    public bool Recycled;

    /// <summary>
    /// AddRef'd IShellItem of the bin entry the delete created, when
    /// capture was requested and an entry was created; IntPtr.Zero
    /// otherwise. The holder owns the release.
    /// </summary>
    public IntPtr NewItem;

    public void PostDeleteItem(uint dwFlags, IntPtr psiItem, int hrDelete, IntPtr psiNewlyCreated)
    {
        Fired = true;
        HrDelete = hrDelete;
        // Non-null psiNewlyCreated is the only reliable "actually
        // recycled" signal: a new bin item was created. Null means the
        // file was permanently deleted.
        Recycled = psiNewlyCreated != IntPtr.Zero;
        if (_captureNewItem && psiNewlyCreated != IntPtr.Zero)
        {
            // The pointer is borrowed for the duration of this callback;
            // AddRef keeps the item alive past PerformOperations so the
            // probe can remove the exact bin entry it just created.
            Marshal.AddRef(psiNewlyCreated);
            NewItem = psiNewlyCreated;
        }
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
