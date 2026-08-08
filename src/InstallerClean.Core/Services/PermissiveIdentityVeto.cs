using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// An <see cref="IIdentityVeto"/> that reads nothing and keeps nothing back.
///
/// It exists for the test constructors of the two services that take a veto, so a
/// test written to pin the PATH classification pins the path classification and is
/// not silently also asserting what the identity pass does. The tests whose subject
/// IS the veto inject a real one or a scripted one.
///
/// It is not a production shape and cannot become one by accident: nothing
/// registers it in either composition root, it is internal, and both production
/// constructors take the veto as a parameter with no default.
/// </summary>
internal sealed class PermissiveIdentityVeto : IIdentityVeto
{
    internal static readonly PermissiveIdentityVeto Instance = new();

    public IdentityPassResult Screen(
        IReadOnlyList<IdentityCandidate> candidates,
        IProgress<ScanProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Filled rather than left at the array's default, so this keeps permitting
        // everything if the enum's members are ever reordered.
        var outcomes = new CandidateIdentityOutcome[candidates.Count];
        Array.Fill(outcomes, CandidateIdentityOutcome.Unclaimed);
        return new IdentityPassResult(outcomes, 0, 0);
    }
}
