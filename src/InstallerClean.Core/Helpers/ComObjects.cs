using System.Runtime.InteropServices;

namespace InstallerClean.Helpers;

/// <summary>
/// Wraps the raw COM pointers a Windows call hands back, and releases every wrapper
/// when it is disposed rather than when the garbage collector next runs.
/// </summary>
/// <remarks>
/// Each pointer gets a wrapper of its own, never one shared through the runtime's cache
/// of wrappers: two pointers to one object, such as an IDispatch and the interface it is
/// cast to, would otherwise share one wrapper, and the second release would find it
/// already released. A wrapper is used and disposed of on the thread that made it, and
/// disposed of before that thread leaves its COM apartment.
/// </remarks>
internal sealed class ComObjects : IDisposable
{
    private readonly List<object> _wrappers = new();

    /// <summary>
    /// A wrapper for <paramref name="pointer"/>, of which the caller holds one reference.
    /// The wrapper takes a reference of its own and the caller's is released here, whether
    /// or not the wrapper could be made, so the caller does not use the pointer again.
    /// Casting the wrapper to a COM interface asks the object for that interface, and the
    /// wrapper holds what the object answers until it is released.
    /// </summary>
    public object Wrap(IntPtr pointer)
    {
        object wrapper;
        try
        {
            wrapper = Marshal.GetUniqueObjectForIUnknown(pointer);
        }
        finally
        {
            Marshal.Release(pointer);
        }
        _wrappers.Add(wrapper);
        return wrapper;
    }

    /// <summary>
    /// Releases every wrapper made here, the last made first, with every reference each
    /// holds. A wrapper is not used after this. A release that throws is written to the
    /// crash log and the rest are still released, so one cannot keep the others' objects
    /// alive past the apartment, and nothing thrown here replaces an exception already on
    /// its way out of the caller.
    /// </summary>
    public void Dispose()
    {
        for (var i = _wrappers.Count - 1; i >= 0; i--)
        {
            try
            {
                Marshal.FinalReleaseComObject(_wrappers[i]);
            }
            catch (Exception ex)
            {
                CrashLog.TryWrite(ex);
            }
        }
        _wrappers.Clear();
    }
}
