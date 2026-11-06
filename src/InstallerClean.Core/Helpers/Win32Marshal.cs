using System.Diagnostics;
using System.Runtime.InteropServices;

namespace InstallerClean.Helpers;

/// <summary>
/// The exception .NET 10 raises for a Windows error number, built as .NET 10's own
/// Win32Marshal builds it: the same type, message and HResult for each number, so a caller
/// that tells one failure from another by its type or its HResult reads it the same on
/// .NET Framework 4.8.
/// </summary>
internal static class Win32Marshal
{
    private const int ERROR_FILE_NOT_FOUND = 2;
    private const int ERROR_PATH_NOT_FOUND = 3;
    private const int ERROR_ACCESS_DENIED = 5;
    private const int ERROR_SHARING_VIOLATION = 32;
    private const int ERROR_FILE_EXISTS = 80;
    private const int ERROR_ALREADY_EXISTS = 183;
    private const int ERROR_FILENAME_EXCED_RANGE = 206;
    private const int ERROR_OPERATION_ABORTED = 995;

    /// <summary>
    /// The exception for the last error a call declared with SetLastError left, naming
    /// <paramref name="path"/> in its message where that is not empty.
    /// </summary>
    internal static Exception GetExceptionForLastWin32Error(string? path = "") =>
        GetExceptionForWin32Error(Marshal.GetLastWin32Error(), path);

    /// <summary>
    /// The exception for <paramref name="errorCode"/>, naming <paramref name="path"/> in its
    /// message where that is not empty:
    /// <list type="bullet">
    /// <item>2, FileNotFoundException; 3, DirectoryNotFoundException; 5,
    /// UnauthorizedAccessException; 206, PathTooLongException; each carrying its type's own
    /// HResult.</item>
    /// <item>32, IOException with HResult 0x80070020. 80 and 183 with a path, IOException
    /// naming it as already there.</item>
    /// <item>995, a plain OperationCanceledException with no token and .NET 10's English
    /// text, which .NET Framework's own would give in the language of an installed language
    /// pack.</item>
    /// <item>Every other number, 80 and 183 without a path among them, IOException with
    /// the text Windows gives for the number, then the path and
    /// <paramref name="errorDetails"/> where given, and HResult 0x80070000 with the number
    /// in the low word.</item>
    /// </list>
    /// </summary>
    internal static Exception GetExceptionForWin32Error(int errorCode, string? path = "", string? errorDetails = null)
    {
        Debug.Assert(errorCode != 0, "The error was read after another call reset it.");

        var named = !string.IsNullOrEmpty(path);

        switch (errorCode)
        {
            case ERROR_FILE_NOT_FOUND:
                return new FileNotFoundException(
                    named ? $"Could not find file '{path}'." : "Unable to find the specified file.", path);
            case ERROR_PATH_NOT_FOUND:
                return new DirectoryNotFoundException(
                    named ? $"Could not find a part of the path '{path}'." : "Could not find a part of the path.");
            case ERROR_ACCESS_DENIED:
                return new UnauthorizedAccessException(
                    named ? $"Access to the path '{path}' is denied." : "Access to the path is denied.");
            case ERROR_ALREADY_EXISTS when named:
                return new IOException(
                    $"Cannot create '{path}' because a file or directory with the same name already exists.",
                    MakeHRFromErrorCode(errorCode));
            case ERROR_FILENAME_EXCED_RANGE:
                return new PathTooLongException(
                    named
                        ? $"The path '{path}' is too long, or a component of the specified path is too long."
                        : "The specified file name or path is too long, or a component of the specified path is too long.");
            case ERROR_SHARING_VIOLATION:
                return new IOException(
                    named
                        ? $"The process cannot access the file '{path}' because it is being used by another process."
                        : "The process cannot access the file because it is being used by another process.",
                    MakeHRFromErrorCode(errorCode));
            case ERROR_FILE_EXISTS when named:
                return new IOException($"The file '{path}' already exists.", MakeHRFromErrorCode(errorCode));
            case ERROR_OPERATION_ABORTED:
                return new OperationCanceledException("The operation was canceled.");
            default:
                var message = WindowsErrors.SystemMessage(errorCode);
                if (named) message += $" : '{path}'.";
                if (!string.IsNullOrEmpty(errorDetails)) message += $" {errorDetails}";
                return new IOException(message, MakeHRFromErrorCode(errorCode));
        }
    }

    /// <summary>
    /// <paramref name="errorCode"/> as an HRESULT in the Windows error facility, 0x8007 in
    /// the high word and the number in the low word. A value already carrying anything in
    /// its high word is an HRESULT and comes back unchanged.
    /// </summary>
    internal static int MakeHRFromErrorCode(int errorCode) =>
        (0xFFFF0000 & errorCode) != 0 ? errorCode : unchecked((int)0x80070000 | errorCode);
}
