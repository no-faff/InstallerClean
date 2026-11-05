using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for ole32.dll's task memory, GUIDs and COM activation,
/// declared as <see cref="Kernel32"/>'s are. Every parameter is a number, a
/// pointer or a pointer to a blittable value, so each declaration is the
/// native signature exactly.
/// </summary>
/// <remarks>
/// Memory a Windows call hands back for the caller to free is freed here rather than
/// with <see cref="Marshal.FreeCoTaskMem"/>, and a new GUID is made here rather than
/// with <see cref="Guid.NewGuid"/>. Both of those call ole32 through .NET's own imports,
/// and those imports search for ole32 without going through this assembly's resolver.
/// </remarks>
internal static class Ole32
{
    private const string Library = "ole32.dll";

    /// <summary>Frees memory allocated with CoTaskMemAlloc. A null pointer is ignored.</summary>
    [DllImport(Library, EntryPoint = "CoTaskMemFree", ExactSpelling = true)]
    public static extern void CoTaskMemFree(IntPtr pv);

    /// <summary>Makes a new GUID. Returns an HRESULT, zero (S_OK) where it made one.</summary>
    [DllImport(Library, EntryPoint = "CoCreateGuid", ExactSpelling = true)]
    public static extern int CoCreateGuid(out Guid guid);

    /// <summary>
    /// Initialises the calling thread's COM apartment. Returns S_OK
    /// (first init), S_FALSE (already initialised on this thread) or an
    /// error HRESULT. Both S_OK and S_FALSE increment the apartment's
    /// init count and require one matching <see cref="CoUninitialize"/>.
    /// </summary>
    [DllImport(Library, EntryPoint = "CoInitializeEx", ExactSpelling = true)]
    public static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    /// <summary>Balances one successful <see cref="CoInitializeEx"/> (S_OK or S_FALSE) on the same thread.</summary>
    [DllImport(Library, EntryPoint = "CoUninitialize", ExactSpelling = true)]
    public static extern void CoUninitialize();

    /// <summary>
    /// Activates a coclass. <paramref name="ppv"/> comes back with a
    /// reference owned by the caller, which hands it to
    /// <see cref="Helpers.ComObjects.Wrap"/>: the wrapper takes a reference
    /// of its own and the caller's is released there.
    /// </summary>
    [DllImport(Library, EntryPoint = "CoCreateInstance", ExactSpelling = true)]
    public static extern int CoCreateInstance(
        in Guid rclsid, IntPtr pUnkOuter, uint dwClsContext, in Guid riid, out IntPtr ppv);
}
