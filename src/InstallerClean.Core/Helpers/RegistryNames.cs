using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// The names of a registry key's subkeys and values, listed as .NET's own
/// <c>RegistryKey.GetSubKeyNames</c> and <c>GetValueNames</c> list them on .NET 10
/// (dotnet/runtime v10.0.0, <c>RegistryKey.cs</c>), and called in their place.
///
/// One listing asks for one index after another until Windows answers
/// ERROR_NO_MORE_ITEMS. Within it, the count Windows gives first only sizes the array,
/// which grows when the enumeration runs past it. .NET Framework's own members ask for
/// exactly as many indexes as that first count and throw on ERROR_NO_MORE_ITEMS. A
/// count of zero gives an empty listing without enumerating, as .NET 10's does.
///
/// A failed call is thrown as .NET 10 throws it: access denied as
/// <see cref="UnauthorizedAccessException"/>, a key that no longer exists as an
/// <see cref="IOException"/> with HResult 2, and any other code as an
/// <see cref="IOException"/> carrying the text Windows gives for the code and the bare
/// code as its HResult. ERROR_INVALID_HANDLE also marks the key's handle invalid first,
/// as .NET 10 does.
///
/// A LISTING THAT CHANGES WHILE IT IS READ IS TREATED AS UNREAD. Windows numbers a
/// key's names by their place in the key, so a name added or removed ahead of the
/// place a listing has reached moves every name after it: one name can then be read
/// twice and another not at all, with no error from Windows. So <see cref="SubKeysOf"/>
/// and <see cref="ValuesOf"/> read the key at least twice, and take the names only
/// where two readings in a row agree: the same count from Windows, an earlier listing
/// holding exactly that many names, the same names in the same order and no name
/// twice. Where they do not, the key is read again after a short pause, up to
/// <see cref="Readings"/> readings in all, since any agreeing pair is as good an answer
/// as the first would have been; after that it throws an <see cref="IOException"/>
/// naming the disagreement, which every caller takes as a key it could not read. A key
/// nobody writes to gives the same names as one listing would, from two readings.
///
/// IT TAKES AN OPENED SUBKEY, NEVER A HIVE'S BASE KEY, and refuses one with
/// <see cref="ArgumentException"/>. .NET Framework's <c>RegistryKey.Handle</c> reopens a
/// base key's root on every read, asking for write access where the key was opened
/// writable, where .NET 10 lists the hive's own handle. So a base key cannot be listed
/// here as .NET 10 lists it, and every key the app lists is one opened under a hive.
/// </summary>
internal static class RegistryNames
{
    private const int ERROR_SUCCESS = 0;
    private const int ERROR_FILE_NOT_FOUND = 2;
    private const int ERROR_ACCESS_DENIED = 5;
    private const int ERROR_INVALID_HANDLE = 6;
    private const int ERROR_MORE_DATA = 234;
    private const int ERROR_NO_MORE_ITEMS = 259;

    // The longest key name Windows allows, 255 characters, and its terminator. A longer
    // name answers ERROR_MORE_DATA, which is thrown, as on .NET 10.
    private const int KeyNameBuffer = 255 + 1;

    // The value-name buffer's first size: .NET 10 rents 100 characters from the shared
    // array pool, which hands back its 128-character bucket. A value name can run to
    // 16,383 characters, and the buffer doubles each time Windows answers that the name
    // does not fit, as the pool's next bucket up does.
    private const int FirstValueNameBuffer = 128;

    /// <summary>Every subkey name under <paramref name="key"/>, in the order Windows gives them.</summary>
    public static string[] SubKeysOf(RegistryKey key) => WithCalls(key, calls => SubKeysUnchanged(calls));

    /// <summary>
    /// Every value name under <paramref name="key"/>, in the order Windows gives them, the default
    /// value's empty name among them when it is set.
    /// </summary>
    public static string[] ValuesOf(RegistryKey key) => WithCalls(key, calls => ValuesUnchanged(calls));

    // How many readings are taken before a key whose readings keep disagreeing is
    // reported as unread, and the pause before each reading after the second, which
    // lets a run of writes to the key finish before it is read again.
    private const int Readings = 4;
    private static readonly TimeSpan[] PauseBefore = [TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250)];

    /// <summary>
    /// The three Windows calls the listing makes, and the marking of the key's handle as
    /// invalid, so a test can stand in for them.
    /// </summary>
    internal interface IKeyCalls
    {
        int QueryCounts(out int subKeys, out int values);
        int EnumKey(int index, char[] name, ref int nameLength);
        int EnumValue(int index, char[] name, ref int nameLength);
        void MarkHandleInvalid();
    }

    /// <summary>One listing: the names, and the count Windows gave before it.</summary>
    private readonly record struct Reading(string[] Names, int Count);

    /// <summary>The subkey names, taken where two readings in a row agree.</summary>
    internal static string[] SubKeysUnchanged(IKeyCalls calls, Action<TimeSpan>? pause = null) =>
        Unchanged(calls, ReadSubKeys, pause ?? (wait => Thread.Sleep(wait)));

    /// <summary>The value names, taken where two readings in a row agree.</summary>
    internal static string[] ValuesUnchanged(IKeyCalls calls, Action<TimeSpan>? pause = null) =>
        Unchanged(calls, ReadValues, pause ?? (wait => Thread.Sleep(wait)));

    private static string[] Unchanged(IKeyCalls calls, Func<IKeyCalls, Reading> read, Action<TimeSpan> pause)
    {
        var earlier = read(calls);
        for (var taken = 2; ; taken++)
        {
            var later = read(calls);
            var disagreement = Disagreement(earlier, later);
            if (disagreement is null) return earlier.Names;
            if (taken == Readings)
                throw new IOException(
                    $"The registry key's names did not read the same way twice running in {Readings} readings: {disagreement}.");
            pause(PauseBefore[taken - 2]);
            earlier = later;
        }
    }

    // The later reading's count is taken after the earlier listing ends. A name read
    // twice is the same stored string both times, so names are compared exactly as
    // Windows gives them.
    private static string? Disagreement(Reading first, Reading second)
    {
        if (first.Count != second.Count)
            return $"the count Windows gives went from {first.Count} to {second.Count}";
        if (first.Names.Length != Math.Max(first.Count, 0))
            return $"the first listing held {first.Names.Length} names against a count of {first.Count}";
        if (!first.Names.SequenceEqual(second.Names, StringComparer.Ordinal))
            return "the two listings differ";
        if (first.Names.Distinct(StringComparer.Ordinal).Count() != first.Names.Length)
            return "a name was read twice";
        return null;
    }

    /// <summary>One listing of the subkey names, as .NET 10 lists them.</summary>
    internal static string[] SubKeys(IKeyCalls calls) => ReadSubKeys(calls).Names;

    /// <summary>One listing of the value names, as .NET 10 lists them.</summary>
    internal static string[] Values(IKeyCalls calls) => ReadValues(calls).Names;

    private static Reading ReadSubKeys(IKeyCalls calls)
    {
        var result = calls.QueryCounts(out var count, out _);
        if (result != ERROR_SUCCESS) Throw(calls, result);
        if (count <= 0) return new Reading([], count);

        var names = new string[count];
        var buffer = new char[KeyNameBuffer];
        var found = 0;
        var nameLength = buffer.Length;

        while ((result = calls.EnumKey(found, buffer, ref nameLength)) != ERROR_NO_MORE_ITEMS)
        {
            if (result != ERROR_SUCCESS) Throw(calls, result);

            if (found >= names.Length) Array.Resize(ref names, names.Length * 2);
            names[found++] = new string(buffer, 0, nameLength);
            nameLength = buffer.Length;
        }

        if (found < names.Length) Array.Resize(ref names, found);
        return new Reading(names, count);
    }

    private static Reading ReadValues(IKeyCalls calls)
    {
        var result = calls.QueryCounts(out _, out var count);
        if (result != ERROR_SUCCESS) Throw(calls, result);
        if (count <= 0) return new Reading([], count);

        var names = new string[count];
        var buffer = new char[FirstValueNameBuffer];
        var found = 0;
        var nameLength = buffer.Length;

        while ((result = calls.EnumValue(found, buffer, ref nameLength)) != ERROR_NO_MORE_ITEMS)
        {
            switch (result)
            {
                case ERROR_SUCCESS:
                    if (found >= names.Length) Array.Resize(ref names, names.Length * 2);
                    names[found++] = new string(buffer, 0, nameLength);
                    break;
                case ERROR_MORE_DATA:
                    // The same index is asked again with the larger buffer.
                    buffer = new char[checked(buffer.Length * 2)];
                    break;
                default:
                    Throw(calls, result);
                    break;
            }

            nameLength = buffer.Length;
        }

        if (found < names.Length) Array.Resize(ref names, found);
        return new Reading(names, count);
    }

    private static void Throw(IKeyCalls calls, int code)
    {
        switch (code)
        {
            case ERROR_ACCESS_DENIED:
                throw new UnauthorizedAccessException();
            case ERROR_INVALID_HANDLE:
                calls.MarkHandleInvalid();
                throw new IOException(WindowsErrors.SystemMessage(code), code);
            case ERROR_FILE_NOT_FOUND:
                throw new IOException("The specified registry key does not exist.", code);
            default:
                throw new IOException(WindowsErrors.SystemMessage(code), code);
        }
    }

    private static string[] WithCalls(RegistryKey key, Func<IKeyCalls, string[]> list)
    {
        // A key opened under another carries its parent's path in its Name; a hive's base
        // key is named by the hive alone. For an opened subkey, Handle is the key's own
        // handle, which the key closes.
        if (key.Name.IndexOf('\\') < 0)
            throw new ArgumentException("Only a key opened under a hive is listed here, never a hive's base key.", nameof(key));

        return list(CallsOn(key.Handle));
    }

    /// <summary>The calls made on <paramref name="handle"/> itself.</summary>
    internal static IKeyCalls CallsOn(SafeRegistryHandle handle) => new NativeCalls(handle);

    private sealed class NativeCalls(SafeRegistryHandle handle) : IKeyCalls
    {
        public int QueryCounts(out int subKeys, out int values)
        {
            var result = Advapi32.RegQueryInfoKey(
                handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out var keys, IntPtr.Zero, IntPtr.Zero,
                out var valueCount, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            subKeys = unchecked((int)keys);
            values = unchecked((int)valueCount);
            return result;
        }

        public int EnumKey(int index, char[] name, ref int nameLength)
        {
            var length = unchecked((uint)nameLength);
            var result = Advapi32.RegEnumKeyEx(
                handle, unchecked((uint)index), name, ref length, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            nameLength = unchecked((int)length);
            return result;
        }

        public int EnumValue(int index, char[] name, ref int nameLength)
        {
            var length = unchecked((uint)nameLength);
            var result = Advapi32.RegEnumValue(
                handle, unchecked((uint)index), name, ref length, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            nameLength = unchecked((int)length);
            return result;
        }

        public void MarkHandleInvalid() => handle.SetHandleAsInvalid();
    }
}
