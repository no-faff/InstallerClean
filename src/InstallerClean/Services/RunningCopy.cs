namespace InstallerClean.Services;

/// <summary>
/// The kind of copy of the app that is running, from what
/// <see cref="AppCopy.DetectRunning"/> reads off this build and the PC.
/// </summary>
internal static class RunningCopy
{
    /// <summary>
    /// The kind of copy running. A process whose own path Windows does not give is
    /// taken for a build from source, which offers the release page alone.
    /// </summary>
    public static AppCopyKind Kind(IRegistryReader registry) =>
        Environment.ProcessPath is { Length: > 0 } exePath
            ? AppCopy.DetectRunning(exePath, registry)
            : AppCopyKind.FromSource;
}
