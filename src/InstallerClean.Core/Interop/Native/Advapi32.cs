using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for advapi32.dll, declared as <see cref="Kernel32"/>'s are.
/// </summary>
internal static class Advapi32
{
    private const string Library = "advapi32.dll";

    /// <summary>
    /// Builds a self-relative security descriptor from its SDDL text. The descriptor
    /// is allocated with LocalAlloc and the caller frees it with
    /// <see cref="Kernel32.LocalFree"/>.
    /// </summary>
    [DllImport(Library, EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
        string stringSecurityDescriptor,
        uint stringSdRevision,
        out IntPtr securityDescriptor,
        IntPtr securityDescriptorSize);

    /// <summary>The only revision the SDDL conversion takes.</summary>
    public const uint SDDL_REVISION_1 = 1;

    /// <summary>
    /// Reads an open object's owner, list and descriptor into memory the system
    /// allocates and the caller frees once, with <see cref="Kernel32.LocalFree"/> on
    /// the descriptor pointer. The owner and list pointers are into that same block,
    /// so they are read before it is freed and never freed on their own. Answers
    /// ERROR_SUCCESS or a Win32 error.
    ///
    /// READ OFF THE HANDLE, not a path: the owner and list are the ones on the object
    /// the handle already names, so a folder put in its place under the same name
    /// afterwards does not change what comes back.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetSecurityInfo", ExactSpelling = true)]
    public static extern uint GetSecurityInfo(
        Microsoft.Win32.SafeHandles.SafeFileHandle handle,
        int ObjectType,
        uint SecurityInfo,
        out IntPtr ppsidOwner,
        out IntPtr ppsidGroup,
        out IntPtr ppDacl,
        out IntPtr ppSacl,
        out IntPtr ppSecurityDescriptor);

    /// <summary>Reads a descriptor's control flags, for the protected-list bit.</summary>
    [DllImport(Library, EntryPoint = "GetSecurityDescriptorControl", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetSecurityDescriptorControl(
        IntPtr pSecurityDescriptor,
        out ushort pControl,
        out uint lpdwRevision);

    /// <summary>
    /// Points <paramref name="pAce"/> at the entry at <paramref name="dwAceIndex"/> in
    /// a list, into the list's own memory. The entry is read before the descriptor it
    /// belongs to is freed.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetAce", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetAce(IntPtr pAcl, uint dwAceIndex, out IntPtr pAce);

    /// <summary>
    /// Writes a SID as its canonical string form (S-1-...), into memory the caller
    /// frees with <see cref="Kernel32.LocalFree"/>. The string is a stable name for a
    /// SID, which is why two SIDs are compared through it here rather than byte by byte.
    /// </summary>
    [DllImport(Library, EntryPoint = "ConvertSidToStringSidW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ConvertSidToStringSid(IntPtr Sid, out IntPtr StringSid);

    /// <summary>OWNER_SECURITY_INFORMATION: ask for the owner.</summary>
    public const uint OWNER_SECURITY_INFORMATION = 0x00000001;

    /// <summary>DACL_SECURITY_INFORMATION: ask for the list.</summary>
    public const uint DACL_SECURITY_INFORMATION = 0x00000004;

    /// <summary>SE_FILE_OBJECT: the object type a file or directory handle names.</summary>
    public const int SE_FILE_OBJECT = 1;

    /// <summary>SE_DACL_PRESENT: the descriptor carries a list, rather than none.</summary>
    public const ushort SE_DACL_PRESENT = 0x0004;

    /// <summary>
    /// SE_DACL_PROTECTED: the list is sealed against the entries a parent would
    /// otherwise add by inheritance, which an SDDL "P" sets.
    /// </summary>
    public const ushort SE_DACL_PROTECTED = 0x1000;
}
