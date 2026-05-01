global using System.IO;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("installerclean-cli")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean.Tests")]

// Core targets net8.0 (rather than net8.0-windows) so the same package
// doesn't drag the WindowsDesktop runtime into a future Linux / Mac
// consumer; in practice the Windows Installer API and the registry
// keys this library reads only exist on Windows. Declaring the
// supported platform tells the platform-compatibility analyser to
// stop flagging every Registry and EventLog call site.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]

// Disable the legacy reflection-driven marshalling layer assembly-wide
// so the .NET 8 source-generated [LibraryImport] stubs can produce
// fully blittable, AOT-compatible P/Invoke calls. Every interop type
// in the assembly must therefore be unmanaged (use IntPtr / fixed-
// size buffers, not managed string/array fields inside structs).
//
// This attribute lives on the Core assembly because every [LibraryImport]
// declaration lives there (Interop/Native/{Kernel32,Dwmapi,Msi,Shell32}.cs).
// The WPF-side InstallerClean assembly does not declare any P/Invoke;
// adding one there without the matching attribute would compile fine
// but use the legacy marshalling path. If a WPF-side P/Invoke is ever
// genuinely needed, declare it here under Interop/Native/ rather than
// in the GUI project so the marshalling rules stay uniform.
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
