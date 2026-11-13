using Microsoft.Win32;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// A registry value read as .NET's own <c>RegistryKey.GetValue</c> reads it on .NET 10
/// (dotnet/runtime v10.0.0, <c>RegistryKey.cs</c>, <c>InternalGetValue</c>), and called
/// in its place.
///
/// One RegQueryValueEx call reads the type and the data together, into a buffer that
/// grows to the length Windows asks for and is read again for as long as Windows answers
/// ERROR_MORE_DATA, so a value rewritten between two calls is read whole as it now
/// stands. Any other error answers null, as .NET 10 answers the default. .NET Framework's
/// own member asks for the length and then for the data, and takes the second call's
/// answer whatever it was.
///
/// A REG_DWORD or REG_QWORD is a number only at exactly 4 bytes, an int, or 8 bytes, a
/// long, whichever of the two types it is; at any other length it is its bytes. .NET
/// Framework's own member reads a REG_DWORD of 4 bytes or fewer as an int, a REG_DWORD
/// of 5 to 8 bytes and a REG_QWORD of 8 bytes or fewer as a long, so a REG_DWORD with no
/// data there reads as the int 0.
///
/// A REG_SZ or REG_EXPAND_SZ is its text without one terminating null, expanded for a
/// REG_EXPAND_SZ, as .NET 10 expands it, unless <c>doNotExpand</c> is set, and a REG_MULTI_SZ is its strings,
/// split at each null. REG_NONE, REG_BINARY and REG_DWORD_BIG_ENDIAN are their bytes, and
/// any other type answers null. Text is the data's UTF-16 code units as they are, an
/// unpaired surrogate included. An odd byte length takes one zero byte on the end, as on
/// .NET 10.
///
/// IT TAKES AN OPENED SUBKEY, NEVER A HIVE'S BASE KEY, and refuses one with
/// <see cref="ArgumentException"/>, for the reason <see cref="RegistryNames"/> gives
/// (<see cref="RegistryNames.SubkeyHandle"/>). A
/// disposed key throws <see cref="ObjectDisposedException"/>, as on .NET 10.
/// </summary>
internal static class RegistryData
{
    private const int ERROR_SUCCESS = 0;
    private const int ERROR_MORE_DATA = 234;

    private const int REG_NONE = 0;
    private const int REG_SZ = 1;
    private const int REG_EXPAND_SZ = 2;
    private const int REG_BINARY = 3;
    private const int REG_DWORD = 4;
    private const int REG_DWORD_BIG_ENDIAN = 5;
    private const int REG_MULTI_SZ = 7;
    private const int REG_QWORD = 11;

    // .NET 10's first buffer, on the stack there.
    private const int FirstBuffer = 512;

    /// <summary>
    /// The value <paramref name="name"/> names under <paramref name="key"/>, the default
    /// value where it is null or empty, or null where there is no such value or it could
    /// not be read.
    /// </summary>
    public static object? ValueOf(RegistryKey key, string? name, bool doNotExpand = false)
    {
        var handle = RegistryNames.SubkeyHandle(key);
        return Decode(buffer =>
        {
            var length = buffer.Length;
            var result = Advapi32.RegQueryValueEx(handle, name, IntPtr.Zero, out var type, buffer, ref length);
            return new Answer(result, type, length);
        }, doNotExpand);
    }

    /// <summary>
    /// What one RegQueryValueEx call answers: its result, the value's type and the length
    /// written or needed. A type of its own rather than a tuple: the copy's .NET Framework
    /// 4.8 check reads the registry through here, and .NET Framework 4.6.2 has no ValueTuple.
    /// </summary>
    internal readonly record struct Answer(int Result, int Type, int Length);

    /// <summary>
    /// The decoding, given the call: <paramref name="query"/> fills the buffer it is handed
    /// and answers the call's result, the value's type and the length written or needed.
    /// </summary>
    internal static object? Decode(Func<byte[], Answer> query, bool doNotExpand)
    {
        var buffer = new byte[FirstBuffer];
        while (true)
        {
            var (result, type, length) = query(buffer);
            if (length < 0)
                throw new IOException("Registry values larger than 2 GB are not supported.");

            if (result == ERROR_MORE_DATA)
            {
                buffer = new byte[length];
                continue;
            }
            if (result != ERROR_SUCCESS)
                return null;

            switch (type)
            {
                case REG_NONE:
                case REG_BINARY:
                case REG_DWORD_BIG_ENDIAN:
                    return Bytes(buffer, length);

                case REG_DWORD:
                case REG_QWORD:
                    return length switch
                    {
                        4 => BitConverter.ToInt32(buffer, 0),
                        8 => BitConverter.ToInt64(buffer, 0),
                        _ => Bytes(buffer, length),
                    };

                case REG_SZ:
                case REG_EXPAND_SZ:
                case REG_MULTI_SZ:
                    return Text(buffer, length, type, doNotExpand);

                default:
                    return null;
            }
        }
    }

    private static byte[] Bytes(byte[] buffer, int length)
    {
        var bytes = new byte[length];
        Buffer.BlockCopy(buffer, 0, bytes, 0, length);
        return bytes;
    }

    private static object Text(byte[] buffer, int length, int type, bool doNotExpand)
    {
        // An odd length takes one zero byte on the end, so the last code unit is whole.
        if (length == int.MaxValue)
            throw new IOException("Registry values larger than 2 GB are not supported.");
        var byteLength = length % 2 == 1 ? length + 1 : length;
        var chars = new char[byteLength / 2];
        Buffer.BlockCopy(buffer, 0, chars, 0, length);

        if (type == REG_MULTI_SZ)
        {
            var strings = new List<string>();
            var start = 0;
            var remaining = chars.Length;
            while (remaining > 1 || (remaining == 1 && chars[start] != '\0'))
            {
                var nullAt = Array.IndexOf(chars, '\0', start, remaining);
                if (nullAt < 0)
                {
                    strings.Add(new string(chars, start, remaining));
                    remaining = 0;
                }
                else
                {
                    strings.Add(new string(chars, start, nullAt - start));
                    remaining -= nullAt - start + 1;
                    start = nullAt + 1;
                }
            }
            return strings.ToArray();
        }

        if (chars.Length == 0)
            return string.Empty;

        var textLength = chars[chars.Length - 1] == '\0' ? chars.Length - 1 : chars.Length;
        var text = new string(chars, 0, textLength);
        return type == REG_EXPAND_SZ && !doNotExpand ? Expanded(text) : text;
    }

    /// <summary>
    /// <paramref name="text"/> with its environment variables expanded as .NET 10's
    /// Environment.ExpandEnvironmentVariables expands them (<c>Environment.Windows.cs</c>):
    /// one ExpandEnvironmentStrings call over the whole text, into a buffer grown to the
    /// length it asks for, and the error Windows gives where it answers 0. .NET
    /// Framework's own asks once for each %-delimited piece before the whole.
    /// </summary>
    private static string Expanded(string text)
    {
        if (text.Length == 0)
            return text;

        var buffer = new char[128];
        uint length;
        while ((length = Kernel32.ExpandEnvironmentStrings(text, buffer, (uint)buffer.Length)) > buffer.Length)
            buffer = new char[length];

        if (length == 0)
            throw Win32Marshal.GetExceptionForLastWin32Error();

        // The length counts the terminating null.
        return new string(buffer, 0, (int)length - 1);
    }
}
