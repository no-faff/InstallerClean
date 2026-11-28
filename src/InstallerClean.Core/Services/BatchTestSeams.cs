using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Signals that always read clean, for the Move and Delete services' test constructors.
/// Production always gets the real <see cref="PendingRebootService"/> through DI.
/// </summary>
internal sealed class QuietInstallerSignals : IPendingRebootService
{
    internal static readonly QuietInstallerSignals Instance = new();

    public PendingRebootResult Check() => PendingRebootResult.Clean;

    public PendingRebootResult CheckBusy() => PendingRebootResult.Clean;
}

/// <summary>
/// The <see cref="IBatchCheck"/> the Move and Delete services' test constructors take
/// where a test passes none. It throws instead of answering, because there is no honest
/// answer for it to give: a check that stood would say files were confirmed that nothing
/// checked, and an ended batch would name a cause that did not occur. A test that reaches
/// it has made a look read as not clean without scripting the check that follows.
/// Production always gets the real <see cref="BatchCheck"/> through DI.
/// </summary>
internal sealed class NoLaterBatchCheck : IBatchCheck
{
    internal static readonly NoLaterBatchCheck Instance = new();

    public Task<BatchCheckResult> RunAsync(
        IReadOnlyList<string> filePaths,
        TimeSpan? waitBound,
        IProgress<BatchCheckProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "No batch check was given to this service. Something resolved this in place of BatchCheck.");
}
