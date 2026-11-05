using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InstallerClean.Helpers;

namespace InstallerClean.Interop.Native;

/// <summary>
/// Loads every library this assembly imports from System32, before the runtime searches
/// anywhere for it, so a file of the same name in the folder the app runs from, such as a
/// Downloads folder holding the portable copy, or in the folder a single-file build
/// extracts itself to, is not loaded into the elevated process. Every library under
/// Interop/Native/ is part of Windows and lives in System32. It also sets the search Windows
/// makes for the whole process, for a library asked for by its name alone, to System32 and
/// the folders added with AddDllDirectory or SetDllDirectory, which each exe asks for before
/// its Main.
/// </summary>
/// <remarks>
/// Each is loaded with LOAD_LIBRARY_SEARCH_SYSTEM32, which looks in System32 for the
/// library and for every library it depends on. LoadLibraryExW is reached through
/// kernel32's own export rather than an import of this assembly, since an import would
/// come back here to be resolved. A library that cannot be loaded from System32 fails
/// the call that imports it, with the <see cref="DllNotFoundException"/> the runtime
/// gives a missing library, rather than falling back to the runtime's own search. An
/// import of a library that ships with the app needs a path of its own here.
/// </remarks>
internal static unsafe class SystemLibraries
{
    private const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;
    private const uint LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400;

    /// <summary>LoadLibraryExW, read from kernel32 the first time a library is resolved.</summary>
    private static delegate* unmanaged<char*, IntPtr, uint, IntPtr> s_loadLibraryEx;

    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255",
        Justification = "The resolver has to be in place before this assembly's first import, whichever host calls it.")]
    internal static void LoadFromSystem32() =>
        NativeLibrary.SetDllImportResolver(typeof(SystemLibraries).Assembly, Resolve);

    /// <summary>
    /// Sets the search Windows makes for the whole process, for a library asked for by its name
    /// alone, to System32 and any folder a component in the process adds with AddDllDirectory
    /// or SetDllDirectory, where <paramref name="exe"/> is the assembly the process was started
    /// for. The folder the exe is run from is then not part of that search. Each exe calls this
    /// from a module initializer, which runs before its Main.
    /// </summary>
    /// <remarks>
    /// The app adds no folder of its own. The runtime loads each of the app's own native
    /// libraries by its full path, from the folder the .NET host names for it, with
    /// LOAD_WITH_ALTERED_SEARCH_PATH, so a library one of them imports is looked for first in
    /// that folder. A folder added with AddDllDirectory or SetDllDirectory is searched before
    /// System32 for every library the process asks for by name, so adding the folder a
    /// single-file build extracts itself to, which its user can write to, would put that
    /// folder ahead of System32. Nothing here throws, since a module initializer that throws
    /// stops the exe before its Main; a refusal from Windows is written to the crash log.
    /// </remarks>
    internal static void SearchSystem32For(Assembly exe)
    {
        try
        {
            SearchSystem32For(exe, Assembly.GetEntryAssembly(), Kernel32.SetDefaultDllDirectories);
        }
        catch (Exception ex)
        {
            Record(ex);
        }
    }

    /// <summary>
    /// Sets the search with <paramref name="setDefaultDllDirectories"/> where
    /// <paramref name="exe"/> is <paramref name="entry"/>, and answers whether it was set. A
    /// test run, whose process was started for its test host, keeps its own search. Where
    /// the process names no entry assembly, that is written to the crash log.
    /// </summary>
    internal static bool SearchSystem32For(Assembly exe, Assembly? entry, Func<uint, bool> setDefaultDllDirectories)
    {
        try
        {
            if (entry == null)
            {
                Record(new InvalidOperationException(
                    "The process names no entry assembly, so its library search was left as it was."));
                return false;
            }
            if (entry != exe) return false;
            if (setDefaultDllDirectories(LOAD_LIBRARY_SEARCH_SYSTEM32 | LOAD_LIBRARY_SEARCH_USER_DIRS)) return true;
            Record(new Win32Exception(Marshal.GetLastWin32Error(),
                "SetDefaultDllDirectories refused System32 as the process's library search."));
        }
        catch (Exception ex)
        {
            Record(ex);
        }
        return false;
    }

    /// <summary>Writes <paramref name="ex"/> to the crash log, where the log can be written.</summary>
    private static void Record(Exception ex)
    {
        try
        {
            CrashLog.TryWrite(ex);
        }
        catch (Exception)
        {
            // The crash log could not be reached either; the exe starts without the entry.
        }
    }

    /// <summary>The library <paramref name="libraryName"/> names, loaded from System32.</summary>
    internal static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) =>
        LoadLibraryFromSystem32(libraryName);

    /// <summary>
    /// The library <paramref name="libraryName"/> names, loaded from System32 with
    /// LOAD_LIBRARY_SEARCH_SYSTEM32, or a <see cref="DllNotFoundException"/> where it cannot be.
    /// </summary>
    internal static IntPtr LoadLibraryFromSystem32(string libraryName)
    {
        var loadLibraryEx = s_loadLibraryEx;
        if (loadLibraryEx == null)
        {
            var kernel32 = Path.Combine(Environment.SystemDirectory, "kernel32.dll");
            if (!NativeLibrary.TryLoad(kernel32, out var module)
                || !NativeLibrary.TryGetExport(module, "LoadLibraryExW", out var export))
                throw new DllNotFoundException($"{libraryName} could not be loaded: kernel32's LoadLibraryExW is not there.");
            s_loadLibraryEx = loadLibraryEx = (delegate* unmanaged<char*, IntPtr, uint, IntPtr>)export;
        }

        IntPtr handle;
        fixed (char* name = libraryName)
            handle = loadLibraryEx(name, IntPtr.Zero, LOAD_LIBRARY_SEARCH_SYSTEM32);
        return handle != IntPtr.Zero
            ? handle
            : throw new DllNotFoundException($"{libraryName} could not be loaded from System32.");
    }
}
