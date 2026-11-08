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
/// <remarks>
/// NOTHING CALLED FROM A MODULE INITIALIZER MAY REACH APPCONTEXT, SYSTEM.IO OR
/// <see cref="Assembly.Location"/>. On .NET Framework the domain does not yet know its
/// target framework while the exe's initializer runs, and the first AppContext read in the
/// process, which System.IO's path code makes and Location's permission check makes, fills
/// in the runtime's compatibility defaults for that unknown target: those of .NET Framework
/// 4.0, among them legacy path handling and a 260-character limit, for the life of the
/// process. So the decision here uses only the two module handles, and a refusal is kept
/// and written to the crash log by <see cref="WriteWhatTheInitializerKept"/> once Main runs.
/// The initializers' IL is checked for this in the tests.
/// </remarks>
internal static class SystemLibraries
{
    private const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;
    private const uint LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400;

    private static readonly List<Exception> Kept = [];

    /// <summary>
    /// Sets the search Windows makes for the whole process, for a library asked for by its name
    /// alone, to System32 and any folder a component in the process adds with AddDllDirectory
    /// or SetDllDirectory, where <paramref name="exe"/> is the process's own image. The folder
    /// the exe is run from is then not part of that search. Each exe calls this from a module
    /// initializer, which runs before its Main.
    /// </summary>
    /// <remarks>
    /// The app adds no folder of its own. A folder added with AddDllDirectory or SetDllDirectory
    /// is searched before System32 for every library the process asks for by name, so adding
    /// one the user can write to would put it ahead of System32. Nothing here throws, since a
    /// module initializer that throws stops the exe before its Main.
    /// </remarks>
    internal static void SearchSystem32For(Assembly exe)
    {
        try
        {
            SearchSystem32For(IsProcessImage(exe), Kernel32.SetDefaultDllDirectories, Keep);
        }
        catch (Exception ex)
        {
            Keep(ex);
        }
    }

    /// <summary>
    /// Whether <paramref name="exe"/> is the image this process was started from: its module's
    /// handle is the process's own. A process that loads the exe as a reference (a test host,
    /// a tool reading it) maps it apart from its own image. Null where either handle cannot be
    /// read.
    /// </summary>
    internal static bool? IsProcessImage(Assembly exe)
    {
        var module = Marshal.GetHINSTANCE(exe.ManifestModule);
        var process = Kernel32.GetModuleHandle(IntPtr.Zero);
        if (module == IntPtr.Zero || module == new IntPtr(-1) || process == IntPtr.Zero) return null;
        return module == process;
    }

    /// <summary>
    /// Sets the search with <paramref name="setDefaultDllDirectories"/> where
    /// <paramref name="isProcessImage"/> is true, and answers whether it was set. A process
    /// that loaded the exe without being started from it keeps its own search. Where it could
    /// not be told which, or Windows refused the search, that goes to <paramref name="record"/>.
    /// </summary>
    internal static bool SearchSystem32For(
        bool? isProcessImage, Func<uint, bool> setDefaultDllDirectories, Action<Exception> record)
    {
        try
        {
            if (isProcessImage is null)
            {
                record(new InvalidOperationException(
                    "Whether the process was started from this exe could not be told, so its library search was left as it was."));
                return false;
            }
            if (isProcessImage == false) return false;
            if (setDefaultDllDirectories(LOAD_LIBRARY_SEARCH_SYSTEM32 | LOAD_LIBRARY_SEARCH_USER_DIRS)) return true;
            record(new Win32Exception(Marshal.GetLastWin32Error(),
                "SetDefaultDllDirectories refused System32 as the process's library search."));
        }
        catch (Exception ex)
        {
            record(ex);
        }
        return false;
    }

    /// <summary>
    /// Writes to the crash log whatever <see cref="SearchSystem32For(Assembly)"/> kept, and
    /// forgets it. Each exe calls this first thing once its Main runs: the command line's Main,
    /// the window's App type initializer. Never throws.
    /// </summary>
    internal static void WriteWhatTheInitializerKept()
    {
        Exception[] kept;
        lock (Kept)
        {
            kept = [.. Kept];
            Kept.Clear();
        }
        foreach (var ex in kept)
        {
            try
            {
                CrashLog.TryWrite(ex);
            }
            catch (Exception)
            {
                // The crash log could not be reached either; the exe runs on without the entry.
            }
        }
    }

    private static void Keep(Exception ex)
    {
        lock (Kept) Kept.Add(ex);
    }
}
