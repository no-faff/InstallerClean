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

// DisableRuntimeMarshalling switches off the runtime's reflection-based
// interop marshaller for the whole assembly. Every interop struct under
// Interop/Native/ is fully blittable for that reason: string fields are
// IntPtr handles to caller-allocated unmanaged buffers (not managed
// string fields the runtime would have to convert), arrays stay outside
// the struct, and Win32 BOOL returns are marshalled explicitly via
// [return: MarshalAs(UnmanagedType.Bool)] because Win32 BOOL is 32 bits
// where C# bool is 8.
[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]
