using System.Diagnostics;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// The libraries Core's imports load are the copies in System32. Windows only: the libraries
/// are part of Windows.
/// </summary>
public class SystemLibrariesTests
{
    public static TheoryData<string, Action> CoreImports => new()
    {
        { "msi.dll", () => Msi.MsiCloseHandle(0) },
        { "ole32.dll", () => Ole32.CoTaskMemFree(IntPtr.Zero) },
        { "oleaut32.dll", () => OleAut32.SysFreeString(IntPtr.Zero) },
        { "shell32.dll", () => KnownFolders.GetPath(Environment.SpecialFolder.CommonApplicationData) },
    };

    [Theory]
    [MemberData(nameof(CoreImports))]
    public void A_library_Core_imports_is_loaded_from_System32(string name, Action call)
    {
        call();

        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var loaded = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
            .Where(m => string.Equals(Path.GetFileName(m.FileName), name, StringComparison.OrdinalIgnoreCase))
            .Select(m => m.FileName)
            .ToList();
        Assert.NotEmpty(loaded);
        Assert.All(loaded, path =>
            Assert.Equal(system32, Path.GetDirectoryName(path), StringComparer.OrdinalIgnoreCase));
    }
}
