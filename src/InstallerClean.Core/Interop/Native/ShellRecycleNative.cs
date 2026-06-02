using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// Activation and shell-item P/Invoke for the <c>IFileOperation</c>
/// recycle path, plus the CLSID, IIDs and operation flags it needs.
///
/// Source-generated <c>[LibraryImport]</c> so it works under the
/// assembly's <c>DisableRuntimeMarshalling</c>: every signature is
/// blittable. <c>in Guid</c> / <c>out IntPtr</c> are pointers to
/// blittable values and pass straight through with no marshalling;
/// the one string (<c>pszPath</c>) is marshalled explicitly via the
/// attribute's <c>StringMarshalling.Utf16</c> because the API takes
/// <c>LPCWSTR</c>.
/// </summary>
internal static partial class ShellRecycleNative
{
    /// <summary>CLSCTX_INPROC_SERVER: the FileOperation coclass is an in-process shell server.</summary>
    internal const uint CLSCTX_INPROC_SERVER = 0x1;

    /// <summary>COINIT_APARTMENTTHREADED: IFileOperation is STA-only.</summary>
    internal const uint COINIT_APARTMENTTHREADED = 0x2;

    private const uint FOFX_RECYCLEONDELETE = 0x00080000;
    private const uint FOF_SILENT           = 0x0004;
    private const uint FOF_NOCONFIRMATION   = 0x0010;
    private const uint FOF_NOERRORUI        = 0x0400;

    /// <summary>
    /// Flags for <c>SetOperationFlags</c>. <c>FOFX_RECYCLEONDELETE</c>
    /// flips <c>DeleteItem</c> from its permanent-delete default to
    /// recycle; the three FOF_* flags keep it headless (no UI, no
    /// confirmation, no error dialog) so it runs on a console CLI and a
    /// dedicated worker thread alike.
    ///
    /// <c>FOFX_RECYCLEONDELETE</c> is recycle-OR-permanently-delete, not
    /// recycle-or-fail: when the Recycle Bin is unavailable the file is
    /// deleted permanently and every HRESULT still reports success
    /// (verified on Windows; the only reliable recycle signal is the
    /// sink's <c>psiNewlyCreated</c>). <c>FOFX_EARLYFAILURE</c> is
    /// deliberately not set: the engine runs one operation per file and
    /// keeps the app's continue-on-error model, so aborting the whole
    /// batch on the first error would be wrong.
    /// </summary>
    internal const uint RecycleFlags =
        FOFX_RECYCLEONDELETE | FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOERRORUI;

    /// <summary>CLSID_FileOperation: the coclass, not an interface. Activated via CoCreateInstance.</summary>
    internal static readonly Guid CLSID_FileOperation = new("3ad05575-8857-4850-9277-11b85bdb8e09");

    /// <summary>IID of <c>IFileOperation</c> (the interface), distinct from the coclass CLSID above.</summary>
    internal static readonly Guid IID_IFileOperation = new("947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8");

    /// <summary>IID of <c>IShellItem</c>, the riid for <c>SHCreateItemFromParsingName</c>.</summary>
    internal static readonly Guid IID_IShellItem = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");

    /// <summary>
    /// Initialises the calling thread's COM apartment. Returns S_OK
    /// (first init), S_FALSE (already initialised on this thread) or an
    /// error HRESULT. Both S_OK and S_FALSE increment the apartment's
    /// init count and require one matching <see cref="CoUninitialize"/>.
    /// </summary>
    [LibraryImport("ole32.dll")]
    internal static partial int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    /// <summary>Balances one successful <see cref="CoInitializeEx"/> (S_OK or S_FALSE) on the same thread.</summary>
    [LibraryImport("ole32.dll")]
    internal static partial void CoUninitialize();

    /// <summary>
    /// Activates a coclass. <paramref name="ppv"/> comes back with a
    /// reference owned by the caller; the caller releases it (after
    /// handing it to a ComWrappers wrapper, which takes its own
    /// reference).
    /// </summary>
    [LibraryImport("ole32.dll")]
    internal static partial int CoCreateInstance(
        in Guid rclsid, IntPtr pUnkOuter, uint dwClsContext, in Guid riid, out IntPtr ppv);

    /// <summary>
    /// Creates an <c>IShellItem</c> for a parsing-name path. Returns an
    /// HRESULT; <paramref name="ppv"/> is an <c>IShellItem*</c> with a
    /// reference owned by the caller (released with
    /// <see cref="Marshal.Release(IntPtr)"/>). Kept as a raw
    /// <see cref="IntPtr"/> because the engine never calls an
    /// <c>IShellItem</c> method, so no managed wrapper is needed.
    /// </summary>
    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int SHCreateItemFromParsingName(
        string pszPath, IntPtr pbc, in Guid riid, out IntPtr ppv);
}
