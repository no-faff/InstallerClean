global using System.IO;

// The CLI host has no Windows-flavoured target framework but only ever
// runs on Windows: the Core services it consumes wrap msi.dll, the
// Windows Registry, and the shell IFileOperation API. The assembly-level platform
// declaration silences the platform-compatibility analyser at every
// callsite that touches those APIs.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]
