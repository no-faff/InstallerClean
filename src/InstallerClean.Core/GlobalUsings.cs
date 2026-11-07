global using System.IO;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("installerclean-cli")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean.Tests")]

// Every member inside an extension block in Compatibility is declared public, in a class
// that stays internal, so these three assemblies alone reach it. The C# compiler of the
// .NET 10 SDK binds an extension member from another assembly only where the member is
// public: an internal one is refused there, or the call binds another member of that name
// instead. scripts/check-extension-member-access.mjs refuses a member that is not public.

// The search for every library this assembly's own imports name looks in System32 alone,
// not in the folder the app runs from, where a file of the same name could otherwise be
// loaded in its place.
[assembly: System.Runtime.InteropServices.DefaultDllImportSearchPaths(
    System.Runtime.InteropServices.DllImportSearchPath.System32)]
