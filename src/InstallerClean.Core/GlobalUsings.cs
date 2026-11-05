global using System.IO;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("installerclean-cli")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean.Tests")]

// Core has no Windows-flavoured target framework so it can in principle
// build on any host, but the registry, EventLog, and Win32 P/Invoke
// surfaces only run on Windows. The assembly-level platform attribute
// silences the platform-compatibility analyser at every callsite that
// would otherwise warn.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]
