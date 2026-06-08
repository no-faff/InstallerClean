using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace InstallerClean.Interop.Native;

// Source-generated COM declarations for the chain that hands a URL to the
// running Explorer so an elevated process opens it at the desktop shell's
// integrity level:
//
//   IShellWindows.FindWindowSW(desktop)  -> the desktop IDispatch
//     -> IServiceProvider.QueryService(SID_STopLevelBrowser) -> IShellBrowser
//       -> IShellBrowser.QueryActiveShellView                -> IShellView
//         -> IShellView.GetItemObject(SVGIO_BACKGROUND)      -> IShellFolderViewDual
//           -> IShellFolderViewDual.get_Application          -> IShellDispatch2
//             -> IShellDispatch2.ShellExecute(url)           (runs in Explorer)
//
// Three rules govern every declaration here; together they are the
// difference between a working call and an AccessViolation at run time
// (a wrong vtable slot or parameter width is not a compile error):
//
//   * Every method is [PreserveSig] int returning the raw HRESULT, so
//     each step is inspected explicitly with no exception translation.
//   * Every method up to and including the one that is called must be
//     declared, in exact native vtable order, because the source
//     generator lays out vtable slots in declaration order after the
//     three IUnknown slots the runtime supplies. Methods past the called
//     one are omitted; the unused methods before it are slot-fillers.
//   * IShellWindows, IShellFolderViewDual and IShellDispatch2 derive from
//     IDispatch. Source-generated COM supports IUnknown-derived interfaces
//     only, so the four IDispatch methods are declared explicitly as the
//     first four slots to reproduce the IDispatch-derived vtable.
//
// Only the called method on each interface needs a faithful signature.
// The slot-fillers are never invoked (their generated stub never runs),
// so their parameters are blittable placeholders (IntPtr for every
// interface/string pointer, the integer widths elsewhere) that exist
// only to occupy the slot. Every interface here is consumed, never
// implemented, so no [GeneratedComClass] is involved.

/// <summary>
/// IShellWindows (exdisp.h). The chain entry point: activated from the
/// running Explorer and asked for the desktop's dispatch via
/// <c>FindWindowSW</c> (slot 13, after the four IDispatch slots and the
/// eight IShellWindows methods that precede it).
/// </summary>
[GeneratedComInterface]
[Guid("85cb6900-4d95-11cf-960c-0080c7f4ee85")]
internal partial interface IShellWindows
{
    [PreserveSig] int GetTypeInfoCount(out uint pctinfo);                                                       // 1
    [PreserveSig] int GetTypeInfo(uint iTInfo, uint lcid, out IntPtr ppTInfo);                                  // 2
    [PreserveSig] int GetIDsOfNames(in Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);   // 3
    [PreserveSig] int Invoke(int dispIdMember, in Guid riid, uint lcid, ushort wFlags,
                             IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, out uint puArgErr);      // 4
    [PreserveSig] int get_Count(out int count);                                                                // 5
    [PreserveSig] int Item(VARIANT index, out IntPtr folder);                                                  // 6
    [PreserveSig] int _NewEnum(out IntPtr ppunk);                                                              // 7
    [PreserveSig] int Register(IntPtr pid, int hwnd, int swClass, out int plCookie);                           // 8
    [PreserveSig] int RegisterPending(int lThreadId, in VARIANT pvarloc, in VARIANT pvarlocRoot,
                                      int swClass, out int plCookie);                                          // 9
    [PreserveSig] int Revoke(int lCookie);                                                                     // 10
    [PreserveSig] int OnNavigate(int lCookie, in VARIANT pvarLoc);                                             // 11
    [PreserveSig] int OnActivated(int lCookie, short fActive);                                                 // 12
    // FindWindowSW(SWC_DESKTOP, SWFO_NEEDDISPATCH) returns S_OK with the
    // desktop's IDispatch, or S_FALSE when there is no desktop window
    // (a clean fall-back-to-clipboard signal). pHWND is the IDL `long*`
    // (a 32-bit HWND that is discarded). The two VARIANT* location
    // filters are VT_EMPTY for the desktop.
    [PreserveSig] int FindWindowSW(in VARIANT pvarLoc, in VARIANT pvarLocRoot, int swClass,
                                   out int pHWND, int swfwOptions, out IntPtr ppdispOut);                      // 13 called
}

/// <summary>
/// IServiceProvider (servprov.h), the COM service locator. This is the
/// COM interface, distinct from <see cref="System.IServiceProvider"/>;
/// the desktop's dispatch is queried through it for the top-level browser.
/// Its single method sits at slot 1 (IUnknown-derived, no IDispatch).
/// </summary>
[GeneratedComInterface]
[Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
internal partial interface IServiceProvider
{
    [PreserveSig] int QueryService(in Guid guidService, in Guid riid, out IntPtr ppvObject);                  // 1 called
}

/// <summary>
/// IShellBrowser (shobjidl_core.h), derived from IOleWindow.
/// <c>QueryActiveShellView</c> is slot 13 (two IOleWindow methods plus
/// ten IShellBrowser methods precede it).
/// </summary>
[GeneratedComInterface]
[Guid("000214e2-0000-0000-c000-000000000046")]
internal partial interface IShellBrowser
{
    [PreserveSig] int GetWindow(out IntPtr phwnd);                                                            // 1  (IOleWindow)
    [PreserveSig] int ContextSensitiveHelp(int fEnterMode);                                                   // 2  (IOleWindow)
    [PreserveSig] int InsertMenusSB(IntPtr hmenuShared, IntPtr lpMenuWidths);                                 // 3
    [PreserveSig] int SetMenuSB(IntPtr hmenuShared, IntPtr holemenuRes, IntPtr hwndActiveObject);             // 4
    [PreserveSig] int RemoveMenusSB(IntPtr hmenuShared);                                                      // 5
    [PreserveSig] int SetStatusTextSB(IntPtr pszStatusText);                                                  // 6
    [PreserveSig] int EnableModelessSB(int fEnable);                                                          // 7
    [PreserveSig] int TranslateAcceleratorSB(IntPtr pmsg, ushort wID);                                        // 8
    [PreserveSig] int BrowseObject(IntPtr pidl, uint wFlags);                                                 // 9
    [PreserveSig] int GetViewStateStream(uint grfMode, out IntPtr ppStrm);                                    // 10
    [PreserveSig] int GetControlWindow(uint id, out IntPtr phwnd);                                            // 11
    [PreserveSig] int SendControlMsg(uint id, uint uMsg, IntPtr wParam, IntPtr lParam, out IntPtr pret);      // 12
    [PreserveSig] int QueryActiveShellView(out IntPtr ppshv);                                                 // 13 called
}

/// <summary>
/// IShellView (shobjidl_core.h), derived from IOleWindow.
/// <c>GetItemObject</c> is slot 13 (two IOleWindow methods plus ten
/// IShellView methods precede it).
/// </summary>
[GeneratedComInterface]
[Guid("000214e3-0000-0000-c000-000000000046")]
internal partial interface IShellView
{
    [PreserveSig] int GetWindow(out IntPtr phwnd);                                                            // 1  (IOleWindow)
    [PreserveSig] int ContextSensitiveHelp(int fEnterMode);                                                   // 2  (IOleWindow)
    [PreserveSig] int TranslateAccelerator(IntPtr pmsg);                                                      // 3
    [PreserveSig] int EnableModeless(int fEnable);                                                            // 4
    [PreserveSig] int UIActivate(uint uState);                                                                // 5
    [PreserveSig] int Refresh();                                                                              // 6
    [PreserveSig] int CreateViewWindow(IntPtr psvPrevious, IntPtr pfs, IntPtr psb,
                                       IntPtr prcView, out IntPtr phWnd);                                      // 7
    [PreserveSig] int DestroyViewWindow();                                                                    // 8
    [PreserveSig] int GetCurrentInfo(IntPtr pfs);                                                             // 9
    [PreserveSig] int AddPropertySheetPages(uint dwReserved, IntPtr pfn, IntPtr lparam);                      // 10
    [PreserveSig] int SaveViewState();                                                                        // 11
    [PreserveSig] int SelectItem(IntPtr pidlItem, uint uFlags);                                               // 12
    [PreserveSig] int GetItemObject(uint uItem, in Guid riid, out IntPtr ppv);                                // 13 called
}

/// <summary>
/// IShellFolderViewDual (shldisp.h), derived from IDispatch.
/// <c>get_Application</c> is the first dispinterface method, slot 5
/// (after the four IDispatch slots).
/// </summary>
[GeneratedComInterface]
[Guid("e7a1af80-4d96-11cf-960c-0080c7f4ee85")]
internal partial interface IShellFolderViewDual
{
    [PreserveSig] int GetTypeInfoCount(out uint pctinfo);                                                     // 1
    [PreserveSig] int GetTypeInfo(uint iTInfo, uint lcid, out IntPtr ppTInfo);                                // 2
    [PreserveSig] int GetIDsOfNames(in Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId); // 3
    [PreserveSig] int Invoke(int dispIdMember, in Guid riid, uint lcid, ushort wFlags,
                             IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, out uint puArgErr);    // 4
    [PreserveSig] int get_Application(out IntPtr ppid);                                                       // 5 called
}

/// <summary>
/// IShellDispatch2 (shldisp.h), derived from IShellDispatch : IDispatch.
/// <c>ShellExecute</c> is slot 29 (four IDispatch methods, the 23
/// IShellDispatch methods, then IShellDispatch2.IsRestricted precede it).
/// </summary>
[GeneratedComInterface]
[Guid("a4c6892c-3ba9-11d2-9dea-00c04fb16162")]
internal partial interface IShellDispatch2
{
    [PreserveSig] int GetTypeInfoCount(out uint pctinfo);                                                     // 1  (IDispatch)
    [PreserveSig] int GetTypeInfo(uint iTInfo, uint lcid, out IntPtr ppTInfo);                                // 2  (IDispatch)
    [PreserveSig] int GetIDsOfNames(in Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId); // 3  (IDispatch)
    [PreserveSig] int Invoke(int dispIdMember, in Guid riid, uint lcid, ushort wFlags,
                             IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, out uint puArgErr);    // 4  (IDispatch)
    [PreserveSig] int get_Application(out IntPtr ppid);                                                       // 5  (IShellDispatch)
    [PreserveSig] int get_Parent(out IntPtr ppid);                                                            // 6
    [PreserveSig] int NameSpace(VARIANT vDir, out IntPtr ppsdf);                                              // 7
    [PreserveSig] int BrowseForFolder(int hwnd, IntPtr title, int options, VARIANT rootFolder, out IntPtr ppsdf); // 8
    [PreserveSig] int Windows(out IntPtr ppid);                                                               // 9
    [PreserveSig] int Open(VARIANT vDir);                                                                     // 10
    [PreserveSig] int Explore(VARIANT vDir);                                                                  // 11
    [PreserveSig] int MinimizeAll();                                                                          // 12
    [PreserveSig] int UndoMinimizeALL();                                                                      // 13
    [PreserveSig] int FileRun();                                                                              // 14
    [PreserveSig] int CascadeWindows();                                                                       // 15
    [PreserveSig] int TileVertically();                                                                       // 16
    [PreserveSig] int TileHorizontally();                                                                     // 17
    [PreserveSig] int ShutdownWindows();                                                                      // 18
    [PreserveSig] int Suspend();                                                                              // 19
    [PreserveSig] int EjectPC();                                                                              // 20
    [PreserveSig] int SetTime();                                                                              // 21
    [PreserveSig] int TrayProperties();                                                                       // 22
    [PreserveSig] int Help();                                                                                 // 23
    [PreserveSig] int FindFiles();                                                                            // 24
    [PreserveSig] int FindComputer();                                                                         // 25
    [PreserveSig] int RefreshMenu();                                                                          // 26
    [PreserveSig] int ControlPanelItem(IntPtr bstrDir);                                                       // 27
    [PreserveSig] int IsRestricted(IntPtr group, IntPtr restriction, out int plRestrictValue);               // 28 (IShellDispatch2)
    // ShellExecute(File, vArgs, vDir, vOperation, vShow). File is a BSTR
    // (passed as a raw pointer the caller allocates and frees); the four
    // optional arguments are VT_EMPTY VARIANTs by value, selecting the
    // default verb and show command.
    [PreserveSig] int ShellExecute(IntPtr File, VARIANT vArgs, VARIANT vDir,
                                   VARIANT vOperation, VARIANT vShow);                                        // 29 called
}
