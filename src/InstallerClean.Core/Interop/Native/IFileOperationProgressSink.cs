using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace InstallerClean.Interop.Native;

/// <summary>
/// Source-generated COM declaration of <c>IFileOperationProgressSink</c>
/// (shobjidl_core.h, IID 04b0f1a7-...). Implemented by the engine's
/// sink and passed to <c>DeleteItem</c>; the shell calls back into it.
/// It is the only source of the per-item delete result: the
/// <c>PostDeleteItem</c> <c>hrDelete</c> and whether
/// <c>psiNewlyCreated</c> is non-null (the verified recycle signal).
///
/// Implemented interface, so the caller (the shell) decides which
/// methods to invoke, and each call runs the generated
/// unmanaged-to-managed stub. Every method is therefore declared
/// call-frame-correct: exact slot count (16), exact order, and every
/// parameter a blittable type matching the native width (<c>uint</c>
/// for DWORD/UINT, <c>int</c> for HRESULT, <c>IntPtr</c> for every
/// <c>IShellItem*</c> and <c>LPCWSTR</c>). All-blittable means no
/// marshalling runs on any callback, so a callback cannot
/// AccessViolation on a mis-marshalled argument; only the slot order
/// and widths matter, and those mirror the header exactly. The shell
/// items arrive as borrowed pointers (the shell owns them), so taking
/// them as <c>IntPtr</c> and only testing for null is exactly right:
/// no AddRef, no Release.
/// </summary>
[GeneratedComInterface]
[Guid("04b0f1a7-9490-44bc-96e1-4296a31252e2")]
internal partial interface IFileOperationProgressSink
{
    void StartOperations();                                                                          // 1
    void FinishOperations(int hrResult);                                                             // 2
    void PreRenameItem(uint dwFlags, IntPtr psiItem, IntPtr pszNewName);                             // 3
    void PostRenameItem(uint dwFlags, IntPtr psiItem, IntPtr pszNewName, int hrRename, IntPtr psiNewlyCreated); // 4
    void PreMoveItem(uint dwFlags, IntPtr psiItem, IntPtr psiDestFolder, IntPtr pszNewName);         // 5
    void PostMoveItem(uint dwFlags, IntPtr psiItem, IntPtr psiDestFolder, IntPtr pszNewName, int hrMove, IntPtr psiNewlyCreated); // 6
    void PreCopyItem(uint dwFlags, IntPtr psiItem, IntPtr psiDestFolder, IntPtr pszNewName);         // 7
    void PostCopyItem(uint dwFlags, IntPtr psiItem, IntPtr psiDestFolder, IntPtr pszNewName, int hrCopy, IntPtr psiNewlyCreated); // 8
    void PreDeleteItem(uint dwFlags, IntPtr psiItem);                                                // 9
    void PostDeleteItem(uint dwFlags, IntPtr psiItem, int hrDelete, IntPtr psiNewlyCreated);         // 10 read by the engine
    void PreNewItem(uint dwFlags, IntPtr psiDestFolder, IntPtr pszNewName);                          // 11
    void PostNewItem(uint dwFlags, IntPtr psiDestFolder, IntPtr pszNewName, IntPtr pszTemplateName,
                     uint dwFileAttributes, int hrNew, IntPtr psiNewItem);                            // 12
    void UpdateProgress(uint iWorkTotal, uint iWorkSoFar);                                           // 13
    void ResetTimer();                                                                               // 14
    void PauseTimer();                                                                               // 15
    void ResumeTimer();                                                                              // 16
}
