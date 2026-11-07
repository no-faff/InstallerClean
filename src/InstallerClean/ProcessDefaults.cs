using System.Runtime.CompilerServices;
using InstallerClean.Interop.Native;

namespace InstallerClean;

/// <summary>
/// Sets the library search for the window's process before any of its code runs, so a library
/// asked for by its name alone is not looked for in the folder the exe is run from, and pins the
/// AppContext switches in <see cref="FrameworkSwitches"/> before WPF reads them.
/// </summary>
internal static class ProcessDefaults
{
    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255",
        Justification = "The search and the switches have to be set before the window's first library is loaded and WPF's first read.")]
    internal static void SetBeforeMain()
    {
        SystemLibraries.SearchSystem32For(typeof(ProcessDefaults).Assembly);
        FrameworkSwitches.Pin();
    }
}
