global using System.IO;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("installerclean-cli")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean.Tests")]

// The search for every library this assembly's own imports name looks in System32 alone,
// not in the folder the app runs from, where a file of the same name could otherwise be
// loaded in its place.
[assembly: System.Runtime.InteropServices.DefaultDllImportSearchPaths(
    System.Runtime.InteropServices.DllImportSearchPath.System32)]
