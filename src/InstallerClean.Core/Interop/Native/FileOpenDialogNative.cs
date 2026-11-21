using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

// COM declarations for the shell's Common Item Dialog, opened as a folder picker
// for Move's destination:
//
//   CoCreateInstance(CLSID_FileOpenDialog)                  -> IFileDialog
//     SetOptions, SetTitle, SetFileName, SetDefaultFolder, SetFolder
//     Show(owner)                                           (the user picks or cancels)
//     GetResult                                             -> IShellItem
//       GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING)        -> the folder's path
//
// The rules are those of ShellDispatchInterfaces.cs: every method is
// [PreserveSig] int, every interface is InterfaceIsIUnknown and derives from
// nothing in C#, and every method up to the last one called is declared in the
// order of the Windows SDK's vtable, its base interfaces' methods included
// (IModalWindow's Show, then IFileDialog's own).
//
// Every shell item and every string is an IntPtr. COM interop marshals a string
// parameter as a BSTR, and SetTitle and SetFileName read an LPCWSTR; the caller
// passes a pointer to null-terminated UTF-16 text it owns for the call, and frees
// the text Windows hands back with CoTaskMemFree. A shell item pointer the caller
// receives carries one reference the caller owns.

/// <summary>
/// IFileDialog (shobjidl_core.h), derived from IModalWindow; the FileOpenDialog object
/// answers for it. <c>GetResult</c> is slot 18 (IModalWindow's one method and sixteen
/// of IFileDialog's precede it).
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
internal interface IFileDialog
{
    // Shows the dialog modal to hwndOwner and returns once it closes: S_OK when a
    // folder was chosen, HRESULT_FROM_WIN32(ERROR_CANCELLED) when the user cancelled.
    [PreserveSig] int Show(IntPtr hwndOwner);                                          // 1  called (IModalWindow)
    [PreserveSig] int SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);              // 2  (IFileDialog)
    [PreserveSig] int SetFileTypeIndex(uint iFileType);                                // 3
    [PreserveSig] int GetFileTypeIndex(out uint piFileType);                           // 4
    [PreserveSig] int Advise(IntPtr pfde, out uint pdwCookie);                         // 5
    [PreserveSig] int Unadvise(uint dwCookie);                                         // 6
    // FILEOPENDIALOGOPTIONS is a DWORD of flags.
    [PreserveSig] int SetOptions(uint fos);                                            // 7  called
    [PreserveSig] int GetOptions(out uint pfos);                                       // 8
    [PreserveSig] int SetDefaultFolder(IntPtr psi);                                    // 9  called
    [PreserveSig] int SetFolder(IntPtr psi);                                           // 10 called
    [PreserveSig] int GetFolder(out IntPtr ppsi);                                      // 11
    [PreserveSig] int GetCurrentSelection(out IntPtr ppsi);                            // 12
    [PreserveSig] int SetFileName(IntPtr pszName);                                     // 13 called
    [PreserveSig] int GetFileName(out IntPtr pszName);                                 // 14
    [PreserveSig] int SetTitle(IntPtr pszTitle);                                       // 15 called
    [PreserveSig] int SetOkButtonLabel(IntPtr pszText);                                // 16
    [PreserveSig] int SetFileNameLabel(IntPtr pszLabel);                               // 17
    // The item chosen, readable once Show has returned S_OK. It fails where more
    // than one item was chosen, which a dialog without FOS_ALLOWMULTISELECT never has.
    [PreserveSig] int GetResult(out IntPtr ppsi);                                      // 18 called
}

/// <summary>
/// IShellItem (shobjidl_core.h): one item in the shell's namespace, here a folder.
/// <c>GetDisplayName</c> is slot 3.
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
internal interface IShellItem
{
    [PreserveSig] int BindToHandler(IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv); // 1
    [PreserveSig] int GetParent(out IntPtr ppsi);                                                        // 2
    // ppszName is allocated with CoTaskMemAlloc and freed by the caller; SIGDN is a
    // 32-bit enum whose values set the top bit, hence uint.
    [PreserveSig] int GetDisplayName(uint sigdnName, out IntPtr ppszName);                               // 3 called
}

/// <summary>
/// Constants and GUIDs for opening the shell's folder picker. The interfaces are
/// declared above, and the activation call is <see cref="Ole32.CoCreateInstance"/>.
/// </summary>
internal static class FileOpenDialogNative
{
    /// <summary>CLSCTX_INPROC_SERVER: the Common Item Dialog is an in-process server.</summary>
    internal const uint CLSCTX_INPROC_SERVER = 0x1;

    /// <summary>FOS_NOCHANGEDIR: the dialog leaves the process's working directory as it is.</summary>
    internal const uint FOS_NOCHANGEDIR = 0x00000008;

    /// <summary>FOS_PICKFOLDERS: the dialog chooses a folder rather than a file.</summary>
    internal const uint FOS_PICKFOLDERS = 0x00000020;

    /// <summary>FOS_FORCEFILESYSTEM: every item the dialog hands back is a file system item.</summary>
    internal const uint FOS_FORCEFILESYSTEM = 0x00000040;

    /// <summary>FOS_PATHMUSTEXIST: the item handed back is in a folder that exists.</summary>
    internal const uint FOS_PATHMUSTEXIST = 0x00000800;

    /// <summary>FOS_FILEMUSTEXIST: the item handed back exists.</summary>
    internal const uint FOS_FILEMUSTEXIST = 0x00001000;

    /// <summary>
    /// FOS_DEFAULTNOMINIMODE: a Save As dialog opens expanded. Windows 7 and later do not
    /// act on it.
    /// </summary>
    internal const uint FOS_DEFAULTNOMINIMODE = 0x20000000;

    /// <summary>
    /// SIGDN_DESKTOPABSOLUTEPARSING: the item's parsing name from the desktop, which
    /// for a file system folder is its full path.
    /// </summary>
    internal const uint SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000;

    /// <summary>HRESULT_FROM_WIN32(ERROR_CANCELLED): what Show returns when the user cancels.</summary>
    internal const int HRESULT_ERROR_CANCELLED = unchecked((int)0x800704C7);

    internal static readonly Guid CLSID_FileOpenDialog = new("dc1c5a9c-e88a-4dde-a5a1-60f82a20aef7");
    internal static readonly Guid IID_IFileDialog      = new("42f85136-db7e-439c-85f1-e4075d135fc8");
    internal static readonly Guid IID_IShellItem       = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
}
