using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace InstallerClean.Interop.Native;

/// <summary>
/// Source-generated COM declaration of <c>IFileOperation</c>
/// (shobjidl_core.h, IID 947aab5f-...). Consumed only: the engine
/// activates a native FileOperation and calls four of these methods.
///
/// Every method is <c>[PreserveSig] int</c>, returning the raw
/// HRESULT, so the engine inspects each code itself with no exception
/// translation (the shell returns informational success codes such as
/// 0x00270008 on a normal recycle, which must not be mistaken for a
/// failure, and a COM exception in the hot path could surface a path
/// in its message).
///
/// All 20 methods are declared in exact shobjidl_core.h vtable order:
/// the source generator lays out vtable slots in declaration order, so
/// the slots the engine calls (SetOperationFlags = 3, DeleteItem = 16,
/// PerformOperations = 19, GetAnyOperationsAborted = 20) only land on
/// the right function pointers if every earlier method occupies its
/// slot. A wrong order or a wrong parameter width is an
/// AccessViolation at run time, not a compile error.
///
/// The methods the engine never calls carry blittable placeholder
/// signatures (<c>IntPtr</c> for every interface and string pointer,
/// <c>uint</c> for every DWORD). Their managed-to-unmanaged stub is
/// generated but never executed, so the placeholders only need to
/// occupy their slot, not match the real parameter types. Every
/// <c>IShellItem*</c> and <c>LPCWSTR</c> is <c>IntPtr</c>; the only
/// marshalled parameter is the per-item sink on <c>DeleteItem</c>,
/// which is a managed <c>[GeneratedComClass]</c> the generator wraps.
/// </summary>
[GeneratedComInterface]
[Guid("947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8")]
internal partial interface IFileOperation
{
    [PreserveSig] int Advise(IntPtr pfops, IntPtr pdwCookie);                                              // 1
    [PreserveSig] int Unadvise(uint dwCookie);                                                             // 2
    [PreserveSig] int SetOperationFlags(uint dwOperationFlags);                                            // 3  called
    [PreserveSig] int SetProgressMessage(IntPtr pszMessage);                                               // 4
    [PreserveSig] int SetProgressDialog(IntPtr popd);                                                      // 5
    [PreserveSig] int SetProperties(IntPtr pproparray);                                                    // 6
    [PreserveSig] int SetOwnerWindow(IntPtr hwndOwner);                                                    // 7
    [PreserveSig] int ApplyPropertiesToItem(IntPtr psiItem);                                               // 8
    [PreserveSig] int ApplyPropertiesToItems(IntPtr punkItems);                                            // 9
    [PreserveSig] int RenameItem(IntPtr psiItem, IntPtr pszNewName, IntPtr pfopsItem);                     // 10
    [PreserveSig] int RenameItems(IntPtr pUnkItems, IntPtr pszNewName);                                    // 11
    [PreserveSig] int MoveItem(IntPtr psiItem, IntPtr psiDestFolder, IntPtr pszNewName, IntPtr pfopsItem); // 12
    [PreserveSig] int MoveItems(IntPtr punkItems, IntPtr psiDestFolder);                                   // 13
    [PreserveSig] int CopyItem(IntPtr psiItem, IntPtr psiDestFolder, IntPtr pszCopyName, IntPtr pfopsItem);// 14
    [PreserveSig] int CopyItems(IntPtr punkItems, IntPtr psiDestFolder);                                   // 15
    [PreserveSig] int DeleteItem(IntPtr psiItem, IFileOperationProgressSink? pfopsItem);                   // 16 called
    [PreserveSig] int DeleteItems(IntPtr punkItems);                                                       // 17
    [PreserveSig] int NewItem(IntPtr psiDestFolder, uint dwFileAttributes, IntPtr pszName,
                              IntPtr pszTemplateName, IntPtr pfopsItem);                                    // 18
    [PreserveSig] int PerformOperations();                                                                 // 19 called
    // GetAnyOperationsAborted returns HRESULT with the BOOL via an out
    // parameter (shobjidl_core.h: HRESULT GetAnyOperationsAborted(BOOL*)).
    // The flag is `out int`, not `out bool`: Win32 BOOL is a 4-byte int,
    // and a 1-byte managed bool target would be overrun by the callee's
    // 4-byte write. Compare the out value != 0.
    [PreserveSig] int GetAnyOperationsAborted(out int pfAnyOperationsAborted);                             // 20 called
}
