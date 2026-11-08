using System.Runtime.CompilerServices;
using InstallerClean.Interop.Native;

namespace InstallerClean;

/// <summary>
/// Sets the library search for the window's process before any of its code runs, so a library
/// asked for by its name alone is not looked for in the folder the exe is run from. The
/// AppContext switches are pinned later, from App's type initializer: the first AppContext
/// call in the process fixes the runtime's own defaults for the target the domain knows at
/// that moment, which inside this initializer is none (see <see cref="SystemLibraries"/>).
/// </summary>
internal static class ProcessDefaults
{
    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255",
        Justification = "The search has to be set before the window's first library is loaded.")]
    internal static void SetBeforeMain()
    {
        SystemLibraries.SearchSystem32For(typeof(ProcessDefaults).Assembly);
    }
}
