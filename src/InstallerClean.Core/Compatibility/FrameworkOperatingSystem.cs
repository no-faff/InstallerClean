namespace System;

/// <summary>
/// The <see cref="OperatingSystem"/> check .NET Framework 4.8 does not have. .NET Framework
/// runs on Windows alone, so the answer is true, as it is in .NET 10's Windows build.
/// </summary>
internal static class FrameworkOperatingSystem
{
    extension(OperatingSystem)
    {
        /// <summary>True: this process runs on Windows.</summary>
        internal static bool IsWindows() => true;
    }
}
