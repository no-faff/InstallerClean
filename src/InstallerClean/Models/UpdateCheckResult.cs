namespace InstallerClean.Models;

/// <summary>
/// Outcome of an update check. Distinguishes a successful check that found
/// no update from a failed check where we couldn't reach GitHub.
/// </summary>
public record UpdateCheckResult(string? LatestVersion, bool CheckFailed)
{
    public static UpdateCheckResult UpToDate()  => new(null, CheckFailed: false);
    public static UpdateCheckResult Failed()    => new(null, CheckFailed: true);
    public static UpdateCheckResult Available(string version) => new(version, CheckFailed: false);
}
