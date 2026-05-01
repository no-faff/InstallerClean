global using System.IO;

// CLI host targets net8.0 but only ever runs on Windows: the Core
// services it consumes wrap msi.dll, the Windows Registry, and
// SHFileOperationW. Declaring the supported platform tells the
// platform-compatibility analyser to stop flagging every Core API.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]
