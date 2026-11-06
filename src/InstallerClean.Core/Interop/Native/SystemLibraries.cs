using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using InstallerClean.Helpers;

namespace InstallerClean.Interop.Native;

/// <summary>
/// The search Windows makes for a library the process asks for by its name alone, which each
/// exe sets before its Main (<see cref="SearchSystem32For(Assembly)"/>). Core's own imports
/// are loaded from System32 by the <see cref="DefaultDllImportSearchPathsAttribute"/> the
/// assembly carries.
/// </summary>
internal static class SystemLibraries
{
    private const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;
    private const uint LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400;

    /// <summary>
    /// Sets the search Windows makes for the whole process, for a library asked for by its name
    /// alone, to System32 and any folder a component in the process adds with AddDllDirectory
    /// or SetDllDirectory, where <paramref name="exe"/> is the assembly the process was started
    /// for. The folder the exe is run from is then not part of that search. Each exe calls this
    /// from a module initializer, which runs before its Main.
    /// </summary>
    /// <remarks>
    /// The app adds no folder of its own. A folder added with AddDllDirectory or SetDllDirectory
    /// is searched before System32 for every library the process asks for by name, so adding
    /// one the user can write to would put it ahead of System32. Nothing here throws, since a
    /// module initializer that throws stops the exe before its Main; a refusal from Windows is
    /// written to the crash log.
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
}
