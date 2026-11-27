namespace InstallerClean.Services;

/// <summary>
/// What a read of the Windows Installer service's state found.
///
/// ONLY <see cref="Stopped"/> AND <see cref="NotThere"/> ARE CLEAR READINGS.
/// <see cref="NotStopped"/> IS THE ZERO, so a value nobody set, a test double's default
/// included, reads as an installation that may be under way.
/// </summary>
public enum InstallerServiceState
{
    /// <summary>The service is starting, running, stopping or paused.</summary>
    NotStopped,

    /// <summary>The service is stopped, so no installation's engine is running.</summary>
    Stopped,

    /// <summary>
    /// Windows has no such service, so there is no service for an installation's engine
    /// to run in.
    /// </summary>
    NotThere,

    /// <summary>The state could not be read; the Windows error is carried beside it.</summary>
    Unreadable,
}

/// <summary>One read of the service's state, with the Windows error where the read failed on one.</summary>
public readonly record struct InstallerServiceRead(InstallerServiceState State, int? WindowsError = null);

/// <summary>
/// Whether the Windows Installer service (<c>msiserver</c>) is stopped.
///
/// WHAT IT IS FOR. Every installation, repair, update and removal Windows Installer makes
/// runs its engine in that service, which Windows starts for it and which stops by itself
/// some minutes after the last one has finished. While the service is stopped, nothing
/// is part-way through changing Windows Installer's records. An installer window left
/// open keeps the service running for as long as the window is up, with no other sign.
/// </summary>
public interface IInstallerServiceState
{
    /// <summary>Reads the service's state once. Never throws, and starts nothing.</summary>
    InstallerServiceRead Read();
}

/// <summary>A service that always reads stopped, for a test that has no service to read.</summary>
internal sealed class StoppedInstallerService : IInstallerServiceState
{
    public static StoppedInstallerService Instance { get; } = new();

    public InstallerServiceRead Read() => new(InstallerServiceState.Stopped);
}
