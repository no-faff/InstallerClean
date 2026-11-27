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

    /// <summary>
    /// Opens the subkey <paramref name="lpSubKey"/> names under <paramref name="hKey"/> with
    /// the access <paramref name="samDesired"/> asks for, writing its handle into
    /// <paramref name="phkResult"/>, which the caller closes. Answers ERROR_SUCCESS,
    /// ERROR_FILE_NOT_FOUND where there is no such key, or another Win32 error.
    /// </summary>
    [DllImport(Library, EntryPoint = "RegOpenKeyExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int RegOpenKeyEx(
        Microsoft.Win32.SafeHandles.SafeRegistryHandle hKey,
        string lpSubKey,
        int ulOptions,
        int samDesired,
        out IntPtr phkResult);

    /// <summary>
    /// Opens the service control manager of this PC with the access
    /// <paramref name="dwDesiredAccess"/> asks for. The handle is closed with
    /// <see cref="CloseServiceHandle"/>.
    /// </summary>
    [DllImport(Library, EntryPoint = "OpenSCManagerW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static extern SafeServiceHandle OpenSCManager(string? lpMachineName, string? lpDatabaseName, uint dwDesiredAccess);

    /// <summary>
    /// Opens the service <paramref name="lpServiceName"/> names. Fails with
    /// <see cref="ERROR_SERVICE_DOES_NOT_EXIST"/> where there is no such service.
    /// </summary>
    [DllImport(Library, EntryPoint = "OpenServiceW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static extern SafeServiceHandle OpenService(SafeServiceHandle hSCManager, string lpServiceName, uint dwDesiredAccess);

    /// <summary>
    /// Reads a service's present state into <paramref name="lpBuffer"/>, for the one
    /// information level there is, SC_STATUS_PROCESS_INFO. It asks the service control
    /// manager and starts nothing.
    /// </summary>
    [DllImport(Library, EntryPoint = "QueryServiceStatusEx", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryServiceStatusEx(
        SafeServiceHandle hService,
        int InfoLevel,
        out SERVICE_STATUS_PROCESS lpBuffer,
        uint cbBufSize,
        out uint pcbBytesNeeded);

    /// <summary>Closes a handle <see cref="OpenSCManager"/> or <see cref="OpenService"/> gave.</summary>
    [DllImport(Library, EntryPoint = "CloseServiceHandle", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseServiceHandle(IntPtr hSCObject);

    /// <summary>SERVICE_STATUS_PROCESS, as winsvc.h declares it: nine DWORDs.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_STATUS_PROCESS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
        public uint dwProcessId;
        public uint dwServiceFlags;
    }

    /// <summary>A handle to the service control manager or to a service.</summary>
    public sealed class SafeServiceHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeServiceHandle() : base(ownsHandle: true) { }

        protected override bool ReleaseHandle() => CloseServiceHandle(handle);
    }

    /// <summary>SC_MANAGER_CONNECT: the right to connect to the service control manager.</summary>
    public const uint SC_MANAGER_CONNECT = 0x0001;

    /// <summary>SERVICE_QUERY_STATUS: the right to ask a service's state.</summary>
    public const uint SERVICE_QUERY_STATUS = 0x0004;

    /// <summary>SC_STATUS_PROCESS_INFO, the information level <see cref="QueryServiceStatusEx"/> takes.</summary>
    public const int SC_STATUS_PROCESS_INFO = 0;

    /// <summary>SERVICE_STOPPED: the service is not running.</summary>
    public const uint SERVICE_STOPPED = 0x00000001;

    /// <summary>The service named is not installed on this PC.</summary>
    public const int ERROR_SERVICE_DOES_NOT_EXIST = 1060;

    /// <summary>KEY_QUERY_VALUE: the right <see cref="RegQueryInfoKey"/> needs on a key.</summary>
    public const int KEY_QUERY_VALUE = 0x0001;

    /// <summary>KEY_NOTIFY: the right <see cref="RegNotifyChangeKeyValue"/> needs on a key.</summary>
    public const int KEY_NOTIFY = 0x0010;

    /// <summary>KEY_WOW64_64KEY: open the key in the 64-bit view of the registry, whatever the process is.</summary>
    public const int KEY_WOW64_64KEY = 0x0100;

    /// <summary>REG_NOTIFY_CHANGE_NAME: a subkey is added or deleted.</summary>
    public const uint REG_NOTIFY_CHANGE_NAME = 0x00000001;

    /// <summary>REG_NOTIFY_CHANGE_LAST_SET: a value is added, deleted or changed.</summary>
    public const uint REG_NOTIFY_CHANGE_LAST_SET = 0x00000004;

    /// <summary>
    /// REG_NOTIFY_THREAD_AGNOSTIC: the registration is not tied to the thread that made
    /// it, so it stays in force after that thread ends. Windows 8 and later.
    /// </summary>
    public const uint REG_NOTIFY_THREAD_AGNOSTIC = 0x10000000;

    /// <summary>
    /// Asks to be told of one change to the key <paramref name="hKey"/> names, or to
    /// anything under it where <paramref name="bWatchSubtree"/> is true, of the kinds
    /// <paramref name="dwNotifyFilter"/> names. With <paramref name="fAsynchronous"/> true
    /// it returns at once and sets <paramref name="hEvent"/> when the change comes.
    /// Answers ERROR_SUCCESS or a Win32 error. Microsoft's page for it: "This function
    /// detects a single change. After the caller receives a notification event, it should
    /// call the function again to receive the next notification", and the key is to have
    /// been opened with KEY_NOTIFY and is to stay open for as long as the change is
    /// waited for.
    /// </summary>
    [DllImport(Library, EntryPoint = "RegNotifyChangeKeyValue", ExactSpelling = true)]
    public static extern int RegNotifyChangeKeyValue(
        Microsoft.Win32.SafeHandles.SafeRegistryHandle hKey,
        [MarshalAs(UnmanagedType.Bool)] bool bWatchSubtree,
        uint dwNotifyFilter,
        Microsoft.Win32.SafeHandles.SafeWaitHandle hEvent,
        [MarshalAs(UnmanagedType.Bool)] bool fAsynchronous);

    /// <summary>
    /// Reads how many subkeys and values an open key holds. Every other count, length
    /// and time it can answer is passed as null and not read. Answers ERROR_SUCCESS or
    /// a Win32 error.
    /// </summary>
    [DllImport(Library, EntryPoint = "RegQueryInfoKeyW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int RegQueryInfoKey(
        Microsoft.Win32.SafeHandles.SafeRegistryHandle hKey,
        IntPtr lpClass,
        IntPtr lpcchClass,
        IntPtr lpReserved,
        out uint lpcSubKeys,
        IntPtr lpcbMaxSubKeyLen,
        IntPtr lpcbMaxClassLen,
        out uint lpcValues,
        IntPtr lpcbMaxValueNameLen,
        IntPtr lpcbMaxValueLen,
        IntPtr lpcbSecurityDescriptor,
        IntPtr lpftLastWriteTime);

    /// <summary>
    /// Writes the name of the subkey at <paramref name="dwIndex"/> into
    /// <paramref name="lpName"/>, and its length in characters, without the terminator,
    /// into <paramref name="lpcchName"/>, which goes in as the buffer's size. Answers
    /// ERROR_SUCCESS, ERROR_NO_MORE_ITEMS past the last subkey, ERROR_MORE_DATA where the
    /// name does not fit, or another Win32 error.
    /// </summary>
    [DllImport(Library, EntryPoint = "RegEnumKeyExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int RegEnumKeyEx(
        Microsoft.Win32.SafeHandles.SafeRegistryHandle hKey,
        uint dwIndex,
        [Out] char[] lpName,
        ref uint lpcchName,
        IntPtr lpReserved,
        IntPtr lpClass,
        IntPtr lpcchClass,
        IntPtr lpftLastWriteTime);

    /// <summary>
    /// Writes the name of the value at <paramref name="dwIndex"/> into
    /// <paramref name="lpValueName"/>, as <see cref="RegEnumKeyEx"/> writes a subkey's,
    /// with the value's type and data passed as null and not read. Answers as
    /// <see cref="RegEnumKeyEx"/> does.
    /// </summary>
    [DllImport(Library, EntryPoint = "RegEnumValueW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int RegEnumValue(
        Microsoft.Win32.SafeHandles.SafeRegistryHandle hKey,
        uint dwIndex,
        [Out] char[] lpValueName,
        ref uint lpcchValueName,
        IntPtr lpReserved,
        IntPtr lpType,
        IntPtr lpData,
        IntPtr lpcbData);

    /// <summary>
    /// Writes the type and the data of the value <paramref name="lpValueName"/> names,
    /// the default value where it is null, into <paramref name="lpType"/> and
    /// <paramref name="lpData"/>, and the data's length in bytes into
    /// <paramref name="lpcbData"/>, which goes in as the buffer's size. Answers
    /// ERROR_SUCCESS, ERROR_MORE_DATA where the data does not fit, with the length it
    /// needs, ERROR_FILE_NOT_FOUND where there is no such value, or another Win32 error.
    /// </summary>
    [DllImport(Library, EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int RegQueryValueEx(
        Microsoft.Win32.SafeHandles.SafeRegistryHandle hKey,
        string? lpValueName,
        IntPtr lpReserved,
        out int lpType,
        [Out] byte[] lpData,
        ref int lpcbData);

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
