using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// Opens a URL at the desktop shell's integrity level from this elevated
/// process. A plain <c>Process.Start</c> with <c>UseShellExecute=true</c>
/// spawns the browser elevated, which strips the user's normal cookies
/// and turns any address opened from the elevated session into a
/// privilege-amplification path. Instead the URL is handed to the
/// already-running Explorer through its shell-view automation chain
/// (IShellWindows, IServiceProvider, IShellBrowser, IShellView,
/// IShellFolderViewDual, IShellDispatch2.ShellExecute), so ShellExecute
/// runs inside Explorer's medium-integrity context and the browser opens
/// unelevated.
///
/// The route needs no privilege, unlike duplicating the shell's primary
/// token and calling <c>CreateProcessWithTokenW</c>: that API requires
/// SeImpersonatePrivilege on the caller and is refused with
/// ERROR_ACCESS_DENIED on elevated tokens that do not grant it. Driving
/// the running shell over COM avoids the privilege entirely and works on
/// locked-down machines.
///
/// If any link in the chain is unavailable (no running shell, a session
/// without a desktop, a COM activation refused across integrity levels)
/// the attempt fails and the caller falls back to copying the URL to the
/// clipboard. The chain opens the browser unelevated or it does not open
/// it at all; it never opens it elevated.
/// </summary>
internal static class UnelevatedLauncher
{
    // Bridges the raw COM pointers from CoCreateInstance and the chain's
    // out-parameters into managed wrappers. UniqueInstance keeps each
    // wrapper out of the cache so FinalRelease() releases it
    // deterministically. The [GeneratedComInterface] marshallers use
    // their own default ComWrappers instance, which is fine: every
    // pointer here is wrapped exactly once and there is no cross-instance
    // identity question.
    private static readonly StrategyBasedComWrappers s_cw = new();

    /// <summary>
    /// Result of an <see cref="OpenUrl"/> attempt. <see cref="Launched"/>
    /// is true when the unelevated browser was launched; otherwise
    /// <see cref="FailureReason"/> describes the step that failed.
    /// Callers fall back to a copy-to-clipboard prompt rather than
    /// launching elevated.
    /// </summary>
    public readonly record struct OpenUrlResult(bool Launched, string FailureReason);

    /// <summary>
    /// Opens <paramref name="url"/> in the user's default browser at
    /// medium integrity by driving the running shell. Returns a result
    /// rather than throwing or falling back to an elevated launch: the
    /// calling host decides what to do when the unelevated route is
    /// unavailable.
    /// </summary>
    public static OpenUrlResult OpenUrl(string url)
    {
        // ShellExecute runs whatever it is handed, including a local path
        // or an executable, so the launch is restricted to an absolute
        // http/https address before it reaches the shell. Every in-app
        // link is https; anything else copies to the clipboard instead.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return new OpenUrlResult(false, "not an absolute http/https URL");

        var safeUrl = uri.AbsoluteUri;

        try
        {
            var launched = false;
            var failureReason = "shell dispatch chain did not complete";

            // The shell automation objects are STA. Run the whole chain on
            // a dedicated STA thread with its own CoInitializeEx /
            // CoUninitialize so the launch does not depend on the caller's
            // apartment (the WPF UI thread is STA, but Core asserts
            // nothing about its callers). The chain is a handful of
            // cross-process COM calls and returns quickly; Join keeps the
            // call synchronous for the caller, as the old token chain was.
            var sta = new Thread(() =>
            {
                var co = ShellDispatchNative.CoInitializeEx(IntPtr.Zero, ShellDispatchNative.COINIT_APARTMENTTHREADED);
                try
                {
                    launched = TryShellExecuteViaExplorer(safeUrl, out failureReason);
                }
                catch (Exception ex)
                {
                    failureReason = ex.GetType().Name;
                    CrashLog.Write(ex);
                }
                finally
                {
                    // One CoUninitialize for the successful init (S_OK or
                    // S_FALSE, both >= 0); skip it if init failed.
                    if (co >= 0) ShellDispatchNative.CoUninitialize();
                }
            });
            sta.SetApartmentState(ApartmentState.STA);
            sta.IsBackground = true;
            sta.Start();
            sta.Join();

            if (launched) return new OpenUrlResult(true, string.Empty);

            CrashLog.Write(new InvalidOperationException(
                "UnelevatedLauncher could not open an unelevated browser via Explorer: " + failureReason));
            return new OpenUrlResult(false, failureReason);
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
            return new OpenUrlResult(false, ex.GetType().Name);
        }
    }

    private static bool TryShellExecuteViaExplorer(string url, out string failureReason)
    {
        failureReason = string.Empty;
        var wrappers = new List<object>(6);
        var bstrUrl = IntPtr.Zero;
        try
        {
            // 1. Connect to the running desktop shell's ShellWindows object.
            var hr = ShellDispatchNative.CoCreateInstance(
                in ShellDispatchNative.CLSID_ShellWindows, IntPtr.Zero,
                ShellDispatchNative.CLSCTX_LOCAL_SERVER,
                in ShellDispatchNative.IID_IShellWindows, out var pShellWindows);
            if (hr < 0 || pShellWindows == IntPtr.Zero)
            {
                failureReason = $"CoCreateInstance(ShellWindows) hr=0x{hr:X8}";
                return false;
            }
            var shellWindows = (IShellWindows)Wrap(pShellWindows, wrappers);

            // 2. FindWindowSW(SWC_DESKTOP) -> the desktop's IDispatch.
            //    Only S_OK (0) with a non-null dispatch is success;
            //    S_FALSE (1) means no desktop window.
            var empty = default(VARIANT);
            hr = shellWindows.FindWindowSW(
                in empty, in empty, ShellDispatchNative.SWC_DESKTOP,
                out _, ShellDispatchNative.SWFO_NEEDDISPATCH, out var pDesktop);
            if (hr != 0 || pDesktop == IntPtr.Zero)
            {
                failureReason = $"FindWindowSW hr=0x{hr:X8}";
                return false;
            }
            // The COM IServiceProvider (servprov.h), fully qualified to
            // distinguish it from System.IServiceProvider.
            var serviceProvider = (InstallerClean.Interop.Native.IServiceProvider)Wrap(pDesktop, wrappers);

            // 3. QueryService(SID_STopLevelBrowser) -> IShellBrowser.
            hr = serviceProvider.QueryService(
                in ShellDispatchNative.SID_STopLevelBrowser,
                in ShellDispatchNative.IID_IShellBrowser, out var pBrowser);
            if (hr < 0 || pBrowser == IntPtr.Zero)
            {
                failureReason = $"QueryService(STopLevelBrowser) hr=0x{hr:X8}";
                return false;
            }
            var browser = (IShellBrowser)Wrap(pBrowser, wrappers);

            // 4. QueryActiveShellView -> IShellView.
            hr = browser.QueryActiveShellView(out var pView);
            if (hr < 0 || pView == IntPtr.Zero)
            {
                failureReason = $"QueryActiveShellView hr=0x{hr:X8}";
                return false;
            }
            var view = (IShellView)Wrap(pView, wrappers);

            // 5. GetItemObject(SVGIO_BACKGROUND) -> the desktop folder view's IDispatch.
            hr = view.GetItemObject(
                ShellDispatchNative.SVGIO_BACKGROUND,
                in ShellDispatchNative.IID_IDispatch, out var pBackground);
            if (hr < 0 || pBackground == IntPtr.Zero)
            {
                failureReason = $"GetItemObject(BACKGROUND) hr=0x{hr:X8}";
                return false;
            }
            var folderView = (IShellFolderViewDual)Wrap(pBackground, wrappers);

            // 6. get_Application -> the Shell.Application dispatch (IShellDispatch2).
            hr = folderView.get_Application(out var pApp);
            if (hr < 0 || pApp == IntPtr.Zero)
            {
                failureReason = $"get_Application hr=0x{hr:X8}";
                return false;
            }
            var shellDispatch = (IShellDispatch2)Wrap(pApp, wrappers);

            // 7. ShellExecute runs in Explorer's medium-integrity context.
            //    The four optional VARIANT arguments are VT_EMPTY (default),
            //    selecting no extra args, no working directory, the default
            //    "open" verb and the default show command.
            bstrUrl = Marshal.StringToBSTR(url);
            hr = shellDispatch.ShellExecute(bstrUrl, default, default, default, default);
            if (hr < 0)
            {
                failureReason = $"IShellDispatch2.ShellExecute hr=0x{hr:X8}";
                return false;
            }
            return true;
        }
        finally
        {
            if (bstrUrl != IntPtr.Zero) Marshal.FreeBSTR(bstrUrl);
            // ComObject is not IDisposable; FinalRelease() releases the
            // wrapper's own reference and works because each wrapper was
            // created with UniqueInstance.
            foreach (var rcw in wrappers)
                if (rcw is ComObject co) co.FinalRelease();
        }
    }

    // Wraps a raw COM pointer the caller owns one reference to.
    // GetOrCreateObjectForComInstance QIs for IUnknown and so takes its
    // own reference; the caller's reference is released here, and the
    // wrapper's own reference is released by FinalRelease() in the
    // caller's finally. The wrapper is returned as object so the call
    // sites can cast to the generated interface (the cast triggers the QI
    // through IDynamicInterfaceCastable); a ComObject-typed return would
    // not compile that cast because ComObject is sealed.
    private static object Wrap(IntPtr punk, List<object> wrappers)
    {
        var rcw = s_cw.GetOrCreateObjectForComInstance(punk, CreateObjectFlags.UniqueInstance);
        Marshal.Release(punk);
        wrappers.Add(rcw);
        return rcw;
    }
}
