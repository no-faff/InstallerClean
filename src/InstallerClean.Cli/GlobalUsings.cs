global using System.IO;

// The CLI host has no Windows-flavoured target framework but only ever
// runs on Windows: the Core services it consumes wrap msi.dll, the Windows
// Registry and the Application event log. The assembly-level platform
// declaration silences the platform-compatibility analyser at every
// callsite that touches those APIs.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]

// The suite reaches this host's own helpers, matching what Core and the WPF
// assembly already grant. Until it did, nothing in the suite could see the
// command line at all: its held-back folds, its stopped-batch byte arithmetic
// and its held-back stdout line were unreachable by any test, and the byte
// arithmetic is where the sharpest fault in that surface has actually been.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstallerClean.Tests")]
