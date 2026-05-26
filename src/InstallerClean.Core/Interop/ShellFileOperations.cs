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
        // The ArgumentException type alone names the precondition; the
        // message is overhead a caller's catch-by-type already conveys.
        // DeleteFilesService catches with catch (Exception) and buckets
        // the result as UnknownError, so the message never reaches the
        // user. A future caller adding a catch-by-type ArgumentException
        // branch would get the right shape without a misleading
        // "source file is missing" message.
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException(null, nameof(path));

        // pFrom is a list-of-strings encoding (null-terminated entries,
        // list end = second null); an embedded \0 in path would split
        // it into two delete entries.
        if (path.Contains('\0'))
            throw new ArgumentException(null, nameof(path));

        // pFrom needs double-null-terminated UTF-16. `path + "\0"`
        // gives the inner null; StringToCoTaskMemUni appends the
        // outer one.
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
