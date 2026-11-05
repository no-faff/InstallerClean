using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// Constants and GUIDs for opening a URL through the already-running
/// desktop shell. An elevated process that wants a URL opened at the
/// user's normal integrity level hands the launch to Explorer rather than
/// spawning the browser itself; these are the values that chain passes.
/// The COM activation calls are in <see cref="Ole32"/> and the interfaces
/// in ShellDispatchInterfaces.cs. Consumed by
/// <see cref="Helpers.UnelevatedLauncher"/>.
/// </summary>
internal static class ShellDispatchNative
{
    /// <summary>COINIT_APARTMENTTHREADED: the shell automation objects in this chain are STA.</summary>
    internal const uint COINIT_APARTMENTTHREADED = 0x2;

    /// <summary>
    /// CLSCTX_LOCAL_SERVER: CLSID_ShellWindows must resolve to the
    /// running Explorer's automation object (an out-of-process local
    /// server), never a fresh in-process instance. Connecting to the
    /// out-of-process shell is the whole mechanism: ShellExecute then
    /// runs in Explorer's medium-integrity context. An in-process
    /// instance would run inside this elevated process and open the
    /// browser elevated, which is exactly what the chain exists to avoid.
    /// </summary>
    internal const uint CLSCTX_LOCAL_SERVER = 0x4;

    /// <summary>SWC_DESKTOP: ask FindWindowSW for the desktop window.</summary>
    internal const int SWC_DESKTOP = 0x08;

    /// <summary>SWFO_NEEDDISPATCH: FindWindowSW must return the window's IDispatch, not only its HWND.</summary>
    internal const int SWFO_NEEDDISPATCH = 0x01;

    /// <summary>
    /// SVGIO_BACKGROUND: ask IShellView.GetItemObject for the
    /// folder-background object (the desktop's IShellFolderViewDual)
    /// rather than a selected item.
    /// </summary>
    internal const uint SVGIO_BACKGROUND = 0x00000000;

    // Only the GUIDs the chain passes as a riid argument are declared. The rest
    // of the interfaces it walks (IServiceProvider, IShellView,
    // IShellFolderViewDual, IShellDispatch2) are reached by casting a wrapped
    // pointer, and the cast asks the object for the GUID in each interface's own
    // [Guid] attribute, so a constant for them would be dead weight. This is not
    // the vtable-slot rule next door in ShellDispatchInterfaces: an unused method
    // there is load-bearing because slots are positional, and an unused GUID here
    // is not.
    internal static readonly Guid CLSID_ShellWindows       = new("9ba05972-f6a8-11cf-a442-00a0c90a8f39");
    internal static readonly Guid IID_IShellWindows        = new("85cb6900-4d95-11cf-960c-0080c7f4ee85");
    internal static readonly Guid SID_STopLevelBrowser     = new("4c96be40-915c-11cf-99d3-00aa004ae837");
    internal static readonly Guid IID_IShellBrowser        = new("000214e2-0000-0000-c000-000000000046");
    internal static readonly Guid IID_IDispatch            = new("00020400-0000-0000-c000-000000000046");
}

/// <summary>
/// Minimal blittable VARIANT, sized to the native union so it can be
/// passed by value across the COM ABI. Only VT_EMPTY is ever used here:
/// the shell-dispatch calls take optional VARIANT arguments, and a
/// zeroed (VT_EMPTY) VARIANT selects every default. The layout is the
/// 8-byte header (vt plus three reserved words) followed by two
/// pointer-sized slots; that is 24 bytes on x64 and 16 on x86, each
/// matching sizeof(VARIANT), so a by-value parameter occupies the exact
/// footprint the callee reads. A wrong size corrupts the call frame at
/// run time rather than failing to compile.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VARIANT
{
    public ushort vt;
    public ushort wReserved1;
    public ushort wReserved2;
    public ushort wReserved3;
    public IntPtr dataPart1;
    public IntPtr dataPart2;
}
