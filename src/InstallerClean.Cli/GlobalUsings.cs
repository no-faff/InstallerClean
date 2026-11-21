global using System.IO;

// The suite reaches this host's own helpers, the held-back folds, the
// stopped-batch byte arithmetic and the held-back stdout line among them, as it
// reaches Core's and the WPF assembly's.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean.Tests")]

// The search for every library this assembly's own imports name looks in System32 alone,
// not in the folder the app runs from, where a file of the same name could otherwise be
// loaded in its place.
[assembly: System.Runtime.InteropServices.DefaultDllImportSearchPaths(
    System.Runtime.InteropServices.DllImportSearchPath.System32)]
