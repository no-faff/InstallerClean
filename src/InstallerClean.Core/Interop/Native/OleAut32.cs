using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for oleaut32.dll's BSTR allocation, declared as <see cref="Kernel32"/>'s
/// are.
/// Consumed by <see cref="Helpers.Bstrs"/>.
/// </summary>
/// <remarks>
/// A BSTR is made and freed here rather than with <see cref="Marshal.StringToBSTR"/> and
/// <see cref="Marshal.FreeBSTR"/>, through this assembly's own import of oleaut32.
/// </remarks>
internal static class OleAut32
{
    private const string Library = "oleaut32.dll";

    /// <summary>
    /// A new BSTR of <paramref name="len"/> characters, copied from <paramref name="src"/>
    /// where it is not null and left for the caller to fill where it is, with a null
    /// character after the last. Returns zero where the memory could not be allocated.
    /// </summary>
    [DllImport(Library, EntryPoint = "SysAllocStringLen", ExactSpelling = true)]
    public static extern IntPtr SysAllocStringLen(IntPtr src, uint len);

    /// <summary>Frees a BSTR made by SysAllocStringLen. A null pointer is ignored.</summary>
    [DllImport(Library, EntryPoint = "SysFreeString", ExactSpelling = true)]
    public static extern void SysFreeString(IntPtr bstrString);
}
