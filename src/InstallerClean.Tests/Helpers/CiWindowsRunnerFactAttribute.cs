using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// A fact that runs only on a GitHub Actions Windows runner and is reported skipped
/// anywhere else, for a test that leaves something on the machine it runs on, or one
/// that needs what a runner is documented to have, given as the reason. GitHub
/// documents each of its hosted runners as a new virtual machine and its Windows ones as
/// running as administrators, and sets <c>GITHUB_ACTIONS</c> to <c>true</c> whenever
/// Actions runs the workflow.
/// </summary>
public sealed class CiWindowsRunnerFactAttribute : FactAttribute
{
    private const string ApplicationLog = "Leaves an entry in this machine's Application log";

    public CiWindowsRunnerFactAttribute() => Skip = SkipReason();

    /// <param name="because">What the test needs or leaves, which the skip message names.</param>
    /// <param name="evenWithoutAdministrator">
    /// Runs on a GitHub Actions Windows runner whatever its rights, for a test whose point
    /// is that the runner has them: it then fails where they are missing, rather than
    /// being skipped.
    /// </param>
    public CiWindowsRunnerFactAttribute(string because, bool evenWithoutAdministrator = false) =>
        Skip = SkipReason(because, evenWithoutAdministrator);

    /// <summary>Null on a GitHub Actions Windows runner; otherwise why a test that runs only there is skipped.</summary>
    internal static string? SkipReason(string because = ApplicationLog, bool evenWithoutAdministrator = false) =>
        !OperatingSystem.IsWindows()
        || Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true"
        || (!evenWithoutAdministrator && !AdministratorRights.Held())
            ? because + ", so it runs only on a GitHub Actions Windows runner."
            : null;
}
