using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// BSTRs, made and freed the way <see cref="System.Runtime.InteropServices.Marshal.StringToBSTR"/>
/// and <see cref="System.Runtime.InteropServices.Marshal.FreeBSTR"/> make and free them on
/// Windows, through this assembly's own oleaut32 import, which its resolver loads from System32.
/// </summary>
/// <remarks>
/// A null string gives a null pointer, and an empty one a BSTR of no characters. The
/// characters are copied into a BSTR SysAllocStringLen allocates for that length, which
/// writes the null character after them, and where it allocates nothing an
/// <see cref="OutOfMemoryException"/> is thrown. Free leaves alone any value with no bit set
/// above the low 16, which is null or a Win32 atom rather than memory.
/// </remarks>
internal static unsafe class Bstrs
{
    /// <summary>Asks Windows for a BSTR of a given length: the pointer, or zero.</summary>
    internal delegate IntPtr Allocate(IntPtr source, uint length);

    /// <summary>A new BSTR holding <paramref name="s"/>, or a null pointer for null.</summary>
    internal static IntPtr FromString(string? s) => FromString(s, OleAut32.SysAllocStringLen);

    /// <summary>Test seam: the same, with the call to Windows supplied by the caller.</summary>
    internal static IntPtr FromString(string? s, Allocate allocate)
    {
        if (s is null)
            return IntPtr.Zero;

        var bstr = allocate(IntPtr.Zero, (uint)s.Length);
        if (bstr == IntPtr.Zero)
            throw new OutOfMemoryException();

        s.AsSpan().CopyTo(new Span<char>((char*)bstr, s.Length));
        return bstr;
    }

    /// <summary>Frees a BSTR made by <see cref="FromString(string?)"/>.</summary>
    internal static void Free(IntPtr bstr) => Free(bstr, OleAut32.SysFreeString);

    /// <summary>Test seam: the same, with the call to Windows supplied by the caller.</summary>
    internal static void Free(IntPtr bstr, Action<IntPtr> free)
    {
        if (((long)bstr & unchecked((long)0xffffffffffff0000L)) != 0)
            free(bstr);
    }
}
