global using System.IO;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean.Tests")]

// Disable the legacy reflection-driven marshalling layer assembly-wide
// so the .NET 8 source-generated [LibraryImport] stubs can produce
// fully blittable, AOT-compatible P/Invoke calls. Every interop type
// in the assembly must therefore be unmanaged (use IntPtr / fixed-
// size buffers, not managed string/array fields inside structs).
//
// MAINTAINER NOTE: any new P/Invoke added under Interop/Native/ must
// (a) use [LibraryImport] not [DllImport]; (b) be on a `partial static`
// method; (c) marshal `bool` returns explicitly via
// [return: MarshalAs(UnmanagedType.Bool)] (Win32 BOOL is 32-bit, not 8);
// (d) keep struct fields blittable - replace `string` with `IntPtr` and
// allocate via Marshal.StringToCoTaskMemUni (see Interop/Native/Shell32.cs
// for the pattern). Adding a [DllImport]-style declaration with managed
// string/array fields will silently miscompile under this attribute.
[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]
