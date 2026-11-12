using System.Security;
using InstallerClean.Helpers;
using Microsoft.Win32;

namespace InstallerClean.Services;

/// <summary>
/// Production <see cref="IFirstRunMark"/>: a DWORD under HKLM in the 64-bit view.
/// A read that fails in one of the four ways <see cref="RegistryReader"/> reports
/// answers <see cref="FirstRunMarkState.Unreadable"/>.
/// </summary>
internal sealed class FirstRunMark : IFirstRunMark
{
    internal const string KeyPath = @"SOFTWARE\NoFaff\InstallerClean";
    internal const string ValueName = "FirstRunRecorded";

    /// <summary>
    /// The machine-wide lock <see cref="TryClaim"/> and <see cref="Set"/> each hold around
    /// their read and write of the mark. It is not the single-instance lock
    /// (<see cref="InstanceLock"/>), which a window holds for its whole life; this one is
    /// held for the length of a registry read and write, so nothing waits on it for long.
    /// </summary>
    internal const string ClaimLockName = @"Global\InstallerClean_FirstReportClaim";

    /// <summary>
    /// How long <see cref="TryClaim"/> waits for the lock before it answers false, and how
    /// long <see cref="Set"/> waits before it writes the mark without the lock.
    /// </summary>
    internal static readonly TimeSpan ClaimLockWait = Durations.FromSeconds(2);

    private readonly RegistryHive _hive;
    private readonly string _keyPath;
    private readonly string _claimLockName;
    private readonly TimeSpan _claimLockWait;

    /// <summary>True once this instance has created the mark while holding the claim lock, which <see cref="TryClaim"/> reads as the mark being this run's own. A write that finds the mark already there, or that went ahead without the lock, leaves it false.</summary>
    private volatile bool _setHere;

    /// <summary>True once <see cref="TryClaim"/> has granted a claim, so a second call on this instance is refused.</summary>
    private volatile bool _claimTaken;

    public FirstRunMark() : this(RegistryHive.LocalMachine, KeyPath) { }

    /// <summary>
    /// Test seam: points the read and the write at another hive and key, so a test
    /// can work on a scratch key under HKCU, which needs no administrator rights,
    /// and delete it afterwards.
    /// </summary>
    internal FirstRunMark(RegistryHive hive, string keyPath)
        : this(hive, keyPath, ClaimLockName, ClaimLockWait) { }

    /// <summary>Test seam: also names the claim lock and bounds the wait for it.</summary>
    internal FirstRunMark(RegistryHive hive, string keyPath, string claimLockName, TimeSpan claimLockWait)
    {
        _hive = hive;
        _keyPath = keyPath;
        _claimLockName = claimLockName;
        _claimLockWait = claimLockWait;
    }

    public FirstRunMarkState Read()
    {
        try
        {
            using var hive = RegistryKey.OpenBaseKey(_hive, RegistryView.Registry64);
            using var key = hive.OpenSubKey(_keyPath);
            if (key is null) return FirstRunMarkState.NotSet;

            // Not expanded and not converted: whatever is stored under the name, of
            // any type, is enough to answer.
            var value = key.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            return value is null ? FirstRunMarkState.NotSet : FirstRunMarkState.Set;
        }
        catch (SecurityException) { return FirstRunMarkState.Unreadable; }
        catch (IOException) { return FirstRunMarkState.Unreadable; }
        catch (UnauthorizedAccessException) { return FirstRunMarkState.Unreadable; }
        catch (ObjectDisposedException) { return FirstRunMarkState.Unreadable; }
    }

    public void Set()
    {
        Mutex? claimLock = null;
        var held = false;
        try
        {
            try
            {
                claimLock = new Mutex(initiallyOwned: false, _claimLockName);
                held = WaitForClaimLock(claimLock);
            }
            catch (Exception ex)
            {
                CrashLog.TryWrite(ex);
            }

            // Without the lock the mark is still written, and the write is not this run's
            // own: whether another process created it first is not known.
            WriteMark(createdUnderLock: held);
        }
        finally
        {
            ReleaseClaimLock(claimLock, held);
        }
    }

    /// <summary>
    /// Writes the mark where it is absent, and leaves a value already under the name as it
    /// is, since <see cref="Read"/> answers set for a value of any kind. Answers whether the
    /// mark is there afterwards, read from the handle the write went through.
    /// <paramref name="createdUnderLock"/> is true where the caller holds the claim lock, so
    /// that finding the mark absent is a fact no other process can have changed between the
    /// read and the write.
    /// </summary>
    private bool WriteMark(bool createdUnderLock)
    {
        try
        {
            using var hive = RegistryKey.OpenBaseKey(_hive, RegistryView.Registry64);
            // Creates the key where it is missing and opens it for writing where it is
            // there. A refusal throws; null comes back only where Windows reports
            // success and hands back no key.
            using var key = hive.CreateSubKey(_keyPath, writable: true);
            if (key is null)
            {
                CrashLog.TryWrite(new IOException(
                    "The key for the first-run mark could not be created or opened."));
                return false;
            }
            var wrote = false;
            if (key.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) is null)
            {
                key.SetValue(ValueName, 1, RegistryValueKind.DWord);
                wrote = true;
            }
            if (key.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) is null)
                return false;
            if (wrote && createdUnderLock) _setHere = true;
            return true;
        }
        catch (Exception ex)
        {
            // Nothing is thrown from here. The command line sets the mark after its
            // files have gone and before it writes the run's summary entry, and a throw
            // would reach its catch-all and write a failure entry in that one's place.
            // It also sets it as a service's own failure passes through on its way to
            // that catch-all (Program.HandOverBatchAsync), where a throw would take that
            // failure's place in the entry. The window's start check sets it inside the
            // task its answer comes back in, which a throw would fault.
            CrashLog.TryWrite(ex);
            return false;
        }
    }

    /// <summary>
    /// True where the lock is held, which includes a lock its last holder ended without
    /// releasing. The lock is a <see cref="Mutex"/>, which belongs to the thread that
    /// waited on it and can be released only by that thread, so nothing between the wait
    /// and <see cref="ReleaseClaimLock"/> may await: a continuation on another thread
    /// would fail the release and leave the lock held until the process ends.
    /// </summary>
    /// <remarks>
    /// The wait dispatches nothing meanwhile (<see cref="HandleWait"/>): the window claims on
    /// its own thread, and no message sent to it is handled before the claim is answered.
    /// Abandoned is its last holder having ended without releasing it, and Windows hands it
    /// to this thread.
    /// </remarks>
    private bool WaitForClaimLock(Mutex claimLock) =>
        HandleWait.One(claimLock.SafeWaitHandle, _claimLockWait) is not HandleWaitResult.TimedOut;

    private static void ReleaseClaimLock(Mutex? claimLock, bool held)
    {
        if (held)
        {
            try { claimLock!.ReleaseMutex(); }
            catch (Exception ex) { CrashLog.TryWrite(ex); }
        }
        claimLock?.Dispose();
    }

    public bool TryClaim()
    {
        if (_claimTaken) return false;
        Mutex? claimLock = null;
        var held = false;
        try
        {
            claimLock = new Mutex(initiallyOwned: false, _claimLockName);
            held = WaitForClaimLock(claimLock);
            if (!held) return false;

            // The claim is looked at again now the lock is held, since another thread on
            // this instance may have been granted it while this one waited.
            if (_claimTaken) return false;

            // A mark this instance created is this run's own: a command-line Delete or
            // Move that moved or deleted a file sets it as the batch returns, ahead of the
            // claim for the report of that same run. A mark anything else created, or that
            // cannot be read, is refused.
            var state = Read();
            if (state == FirstRunMarkState.Unreadable) return false;
            if (state == FirstRunMarkState.Set && !_setHere) return false;
            if (state == FirstRunMarkState.NotSet && !WriteMark(createdUnderLock: true)) return false;
            _claimTaken = true;
            return true;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return false;
        }
        finally
        {
            ReleaseClaimLock(claimLock, held);
        }
    }
}
