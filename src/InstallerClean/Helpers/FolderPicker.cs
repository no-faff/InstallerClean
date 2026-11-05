using System.Runtime.InteropServices;
using System.Windows.Interop;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// The shell's own folder picker, the Common Item Dialog with FOS_PICKFOLDERS, opened
/// through <see cref="IFileDialog"/>.
/// </summary>
/// <remarks>
/// Every wrapper is released as <see cref="Pick"/> returns, on every path, rather than
/// when the garbage collector next runs. A call that sets the dialog up or reads its
/// answer and fails throws a <see cref="COMException"/> naming the call, which the
/// caller's crash handling reports by type name. Show failing gives no folder, as
/// cancelling does.
/// </remarks>
internal static class FolderPicker
{
    /// <summary>
    /// The whole set of options the dialog runs with: it offers folders rather than files,
    /// and the item it hands back is a file system item that exists.
    /// FOS_DEFAULTNOMINIMODE is part of the set and Windows 7 and later do not act on it.
    /// </summary>
    internal const uint Options =
        FileOpenDialogNative.FOS_PICKFOLDERS
        | FileOpenDialogNative.FOS_FORCEFILESYSTEM
        | FileOpenDialogNative.FOS_PATHMUSTEXIST
        | FileOpenDialogNative.FOS_FILEMUSTEXIST
        | FileOpenDialogNative.FOS_DEFAULTNOMINIMODE;

    /// <summary>
    /// Shows the picker and returns the full path of the folder chosen, or null when the
    /// user cancels or the dialog does not show. Called on an STA thread with a message
    /// loop, which is the dispatcher thread in the app.
    /// </summary>
    /// <param name="title">The dialog's title.</param>
    /// <param name="startFolder">
    /// The folder the dialog opens in, or null for the shell's own choice. A folder the
    /// shell will not make an item of, or will not open in, gives the shell's own choice.
    /// </param>
    /// <param name="fallbackOwner">
    /// The window the dialog is modal to when no window on this thread is active, as when
    /// the app is not in the foreground. Zero leaves the dialog without an owner.
    /// </param>
    public static string? Pick(string title, string? startFolder, IntPtr fallbackOwner)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            throw new InvalidOperationException("The folder picker is shown from an STA thread only.");

        using var com = new ComObjects();

        var hr = Ole32.CoCreateInstance(
            in FileOpenDialogNative.CLSID_FileOpenDialog, IntPtr.Zero,
            FileOpenDialogNative.CLSCTX_INPROC_SERVER,
            in FileOpenDialogNative.IID_IFileDialog, out var pDialog);
        if (hr < 0 || pDialog == IntPtr.Zero)
            throw Failed("CoCreateInstance(FileOpenDialog)", hr);
        var dialog = (IFileDialog)com.Wrap(pDialog);

        // SetOptions is given the whole set rather than one added to what GetOptions
        // reports, so the dialog runs with Options and nothing else.
        Check(dialog.SetOptions(Options), "IFileDialog.SetOptions");
        SetText(text => dialog.SetTitle(text), title, "IFileDialog.SetTitle");
        // SetFileName sets the text in the dialog's Folder box, which opens empty.
        SetText(text => dialog.SetFileName(text), string.Empty, "IFileDialog.SetFileName");
        if (!string.IsNullOrEmpty(startFolder))
            SetStartFolder(dialog, startFolder!);

        // The active window is the one of this app's windows the user was in, so the
        // picker is modal to it, as a dialog opened from it would be.
        var owner = User32.GetActiveWindow();
        if (owner == IntPtr.Zero) owner = fallbackOwner;

        // Show runs its own message loop until the dialog closes. PushModal tells WPF
        // this thread is in a modal loop meanwhile, and PopModal is in a finally so WPF
        // is told it has ended whatever Show does.
        ComponentDispatcher.PushModal();
        try
        {
            hr = dialog.Show(owner);
        }
        finally
        {
            ComponentDispatcher.PopModal();
        }

        if (hr < 0)
        {
            // Cancelling is an answer. Any other failure leaves no folder chosen too, and
            // is written to the crash log, the one place it can be seen.
            if (hr != FileOpenDialogNative.HRESULT_ERROR_CANCELLED)
                CrashLog.TryWrite(Failed("IFileDialog.Show", hr));
            return null;
        }

        return ChosenFolder(dialog, com);
    }

    /// <summary>The path of the folder the dialog hands back once Show has succeeded.</summary>
    private static string ChosenFolder(IFileDialog dialog, ComObjects com)
    {
        var hr = dialog.GetResult(out var pItem);
        if (hr < 0 || pItem == IntPtr.Zero)
            throw Failed("IFileDialog.GetResult", hr);
        var item = (IShellItem)com.Wrap(pItem);

        hr = item.GetDisplayName(FileOpenDialogNative.SIGDN_DESKTOPABSOLUTEPARSING, out var pName);
        try
        {
            if (hr < 0 || pName == IntPtr.Zero)
                throw Failed("IShellItem.GetDisplayName", hr);
            return Marshal.PtrToStringUni(pName)!;
        }
        finally
        {
            Ole32.CoTaskMemFree(pName);
        }
    }

    // SetTitle and SetFileName read a null-terminated UTF-16 string. A BSTR points at
    // the first of its characters and has a null character after the last, so one made
    // here and freed after the call is that string.
    private static void SetText(Func<IntPtr, int> set, string value, string call)
    {
        var text = Bstrs.FromString(value);
        try
        {
            Check(set(text), call);
        }
        finally
        {
            Bstrs.Free(text);
        }
    }

    /// <summary>
    /// Opens the dialog in <paramref name="folder"/>. SetFolder makes it the folder the
    /// dialog opens in, ahead of the one the user last chose in it, and SetDefaultFolder
    /// makes it the folder used where the dialog has no recently used one. Whatever the
    /// shell refuses here leaves the dialog to open where the shell would have opened it.
    /// </summary>
    private static void SetStartFolder(IFileDialog dialog, string folder)
    {
        var hr = Shell32.SHCreateItemFromParsingName(
            folder, IntPtr.Zero, in FileOpenDialogNative.IID_IShellItem, out var pFolder);
        if (hr < 0 || pFolder == IntPtr.Zero)
            return;
        try
        {
            dialog.SetDefaultFolder(pFolder);
            dialog.SetFolder(pFolder);
        }
        finally
        {
            // The dialog takes references of its own to the folder it keeps.
            Marshal.Release(pFolder);
        }
    }

    private static void Check(int hr, string call)
    {
        if (hr < 0) throw Failed(call, hr);
    }

    private static COMException Failed(string call, int hr) =>
        new($"{call} failed with HRESULT 0x{hr:X8}.", hr);
}
