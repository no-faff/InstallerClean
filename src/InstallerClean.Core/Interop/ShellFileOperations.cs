using System.Runtime.InteropServices;
using InstallerClean.Interop.Native;
using InstallerClean.Resources;

namespace InstallerClean.Interop;

/// <summary>
/// Thin wrapper over <see cref="Shell32.SHFileOperation"/> for sending
/// files to the Recycle Bin without any UI. Avoids VB's
/// FileSystem.DeleteFile which expects an STA thread when it shows
/// error dialogs.
/// </summary>
internal static class ShellFileOperations
{
    /// <summary>
    /// Sends a single file to the Recycle Bin. Returns 0 on success
    /// or a non-zero shell error code otherwise. Never shows UI, never
    /// prompts, and is safe to call from any thread or apartment state.
    /// </summary>
    internal static int SendToRecycleBin(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException(Strings.Error_MissingSourceFile, nameof(path));

        // SECURITY: refuse paths that contain an embedded null. SHFILEOPSTRUCT.pFrom
        // is a list-of-strings encoding (each entry null-terminated, list end
        // marked by a second null). An embedded \0 in path would split the input
        // into two list entries, so the shell would attempt to delete both. Real
        // Windows file paths cannot contain \0, so any caller passing one is a
        // bug; fail fast rather than over-delete.
        if (path.Contains('\0'))
            throw new ArgumentException(Strings.Error_MissingSourceFile, nameof(path));

        // SHFILEOPSTRUCT.pFrom requires a DOUBLE-null-terminated UTF-16
        // string (Windows file-list-string convention). We construct
        // it explicitly:
        //   - `path + "\0"` adds the inner null between the file path
        //     and the (zero-element) next entry.
        //   - Marshal.StringToCoTaskMemUni's documented behaviour is to
        //     append a trailing null to whatever string it receives,
        //     producing the second null on the unmanaged side.
        // This depends on StringToCoTaskMemUni's implicit termination.
        // Do NOT swap to an allocator that returns the raw UTF-16 bytes
        // of the input string (e.g. a hypothetical Marshal.StringToHGlobalUni
        // variant that doesn't terminate) without explicitly writing
        // the second null yourself, or SHFileOperationW will read past
        // the end of the buffer.
        var pFrom = Marshal.StringToCoTaskMemUni(path + "\0");
        try
        {
            var op = new Shell32.SHFILEOPSTRUCT
            {
                wFunc = Shell32.FO_DELETE,
                pFrom = pFrom,
                fFlags = (ushort)(Shell32.FOF_ALLOWUNDO
                                | Shell32.FOF_SILENT
                                | Shell32.FOF_NOCONFIRMATION
                                | Shell32.FOF_NOERRORUI
                                | Shell32.FOF_NOCONFIRMMKDIR),
            };
            return Shell32.SHFileOperation(ref op);
        }
        finally
        {
            Marshal.FreeCoTaskMem(pFrom);
        }
    }
}
