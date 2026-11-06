using System.Runtime.CompilerServices;
using InstallerClean.Interop.Native;

namespace InstallerClean.Cli;

/// <summary>
/// Sets the library search for the command line's process before any of its code runs, so a library
/// asked for by its name alone is not looked for in the folder the exe is run from.
/// </summary>
internal static class LibrarySearch
{
    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255",
        Justification = "The search has to be set before the command line's first library is loaded.")]
    internal static void SetBeforeMain()
    {
        SystemLibraries.SearchSystem32For(typeof(LibrarySearch).Assembly);
    }
}
