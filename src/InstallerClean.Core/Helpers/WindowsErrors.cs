using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// A Windows error number, read off an exception .NET raised for it, and the text
/// Windows gives for that number, for a sentence that says why a check failed.
/// </summary>
internal static class WindowsErrors
{
    // The facility .NET puts in an exception's HResult when it builds the exception from
    // a Windows error number: 0x8007 in the high word, the number in the low word.
    private const int Win32Facility = unchecked((int)0x80070000);

    /// <summary>
    /// The Windows error number <paramref name="exception"/> was raised for, or null where
    /// it was raised for none.
    ///
    /// ONLY THE EXCEPTIONS .NET BUILDS FROM A WINDOWS ERROR ANSWER: a
    /// <see cref="Win32Exception"/>, and an <see cref="IOException"/> whose HResult is in the
    /// Windows error facility, which is the form a failed Windows call on a file or a
    /// synchronisation object is raised in. Several exceptions .NET raises on its own
    /// account carry a code in that facility too (an <see cref="ArgumentException"/> carries
    /// the code for an invalid parameter, an <see cref="EndOfStreamException"/> the code for
    /// the end of a file), so the facility alone does not show that Windows gave the error,
    /// and those answer null.
    /// </summary>
    internal static int? Of(Exception exception) => exception switch
    {
        Win32Exception win32 => win32.NativeErrorCode,
        EndOfStreamException => null,
        IOException when (exception.HResult & unchecked((int)0xFFFF0000)) == Win32Facility =>
            exception.HResult & 0xFFFF,
        _ => null,
    };

    /// <summary>
    /// The text Windows gives for <paramref name="code"/>, in the language Windows is shown in,
    /// without the full stop Windows ends it with, so a sentence can carry it in the middle.
    /// It names no file and no account.
    /// </summary>
    internal static string MessageFor(int code) => OneLine(SystemMessage(code));

    /// <summary>
    /// The text Windows gives for <paramref name="code"/>, from the system's own message
    /// table in the language Windows is shown in, with the white space and control
    /// characters after it taken off and its full stop left on. Where Windows has no text
    /// for the number it is <c>Unknown error (0x...)</c>, with the number in hexadecimal.
    /// Inserts are left as written: the text is read with no arguments to put in them.
    /// </summary>
    internal static string SystemMessage(int code)
    {
        const uint flags = Kernel32.FORMAT_MESSAGE_ALLOCATE_BUFFER | Kernel32.FORMAT_MESSAGE_IGNORE_INSERTS
            | Kernel32.FORMAT_MESSAGE_FROM_SYSTEM | Kernel32.FORMAT_MESSAGE_ARGUMENT_ARRAY;
        var length = Kernel32.FormatMessage(
            flags, IntPtr.Zero, unchecked((uint)code), 0, out var buffer, 0, IntPtr.Zero);
        try
        {
            if (length > 0 && buffer != IntPtr.Zero)
            {
                var text = Marshal.PtrToStringUni(buffer, (int)length);
                var end = text.Length;
                while (end > 0 && text[end - 1] <= ' ') end--;
                return text.Substring(0, end);
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero) Kernel32.LocalFree(buffer);
        }
        return "Unknown error (0x" + code.ToString("x", CultureInfo.InvariantCulture) + ")";
    }

    /// <summary>
    /// <paramref name="message"/> as one line with its full stop taken off: every run of
    /// white space, line breaks included, becomes one space. A message-table entry can break
    /// its text across lines, and a sentence carrying it is one line on both hosts.
    /// </summary>
    internal static string OneLine(string message) =>
        string.Join(" ", message.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.', '。').TrimEnd();

    /// <summary>
    /// <paramref name="code"/> as the sentences print it: decimal, the form Windows prints its
    /// own error numbers in, and never grouped by a culture's separator.
    /// </summary>
    internal static string Number(int code) => code.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// <paramref name="withError"/> with the text Windows gives for <paramref name="code"/> in
    /// <c>{0}</c> and its number in <c>{1}</c> where there is a code, and
    /// <paramref name="plain"/>, which gives no cause, where there is none.
    /// </summary>
    internal static string Sentence(int? code, string plain, string withError) =>
        code is { } number
            ? string.Format(withError, MessageFor(number), Number(number))
            : plain;
}
