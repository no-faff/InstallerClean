global using System.IO;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("installerclean-cli")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean.Tests")]

// Core targets net8.0 (not net8.0-windows) but the registry, EventLog
// and Win32 calls only run on Windows; this attribute silences the
// platform-compatibility analyser at every call site.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]

// Source-generated [LibraryImport] stubs produce fully-blittable
// P/Invoke calls. Every interop struct under Interop/Native/ uses
// IntPtr / fixed-size buffers (no managed strings or arrays inside
// structs); Win32 BOOL returns are marshalled explicitly via
// [return: MarshalAs(UnmanagedType.Bool)] because Win32 BOOL is
// 32-bit, not 8.
[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]
