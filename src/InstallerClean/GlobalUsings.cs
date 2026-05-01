global using System.IO;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean.Tests")]

// Disable the legacy reflection-driven marshalling layer assembly-wide
// so the .NET 8 source-generated [LibraryImport] stubs can produce
// fully blittable, AOT-compatible P/Invoke calls. Every interop type
// in the assembly must therefore be unmanaged (use IntPtr / fixed-
// size buffers, not managed string/array fields inside structs).
[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]
