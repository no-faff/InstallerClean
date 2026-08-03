namespace InstallerClean.Services;

/// <summary>
/// No-op <see cref="IRemovableReverifier"/> for the action-service test
/// constructors that do not drive the under-lease re-read, the sibling of
/// <see cref="NullMutexProbe"/>. Production always gets the real
/// <see cref="RemovableReverifier"/> through DI.
/// </summary>
/// <remarks>
/// <see cref="RecheckUnderLease"/> answering "nothing reclaimed" is the
/// safe no-op only because it is paired with a caller that passes no claims:
/// with claims in hand it would be a safety check reporting a pass it never
/// made. The action services short-circuit on an empty claim list before
/// reaching it, so on this path the answer is a fact rather than an assumption.
///
/// <see cref="ReverifyAsync"/> throws instead of answering, and the asymmetry is
/// deliberate. There is no safe no-op for it: every honest return says either
/// that nothing was reclaimed, which it did not check, or that everything was,
/// which would silently empty a batch. Nothing in either action service calls
/// it, so reaching it means something was wired to this rather than to the real
/// one, and failing loudly is the only answer that cannot be mistaken for a
/// verified all-clear.
/// </remarks>
internal sealed class NullRemovableReverifier : IRemovableReverifier
{
    internal static readonly NullRemovableReverifier Instance = new();

    public Task<ReverifyResult> ReverifyAsync(
        IReadOnlyList<string> candidatePaths,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "The no-op reverifier cannot re-verify a batch. Something resolved this in place of RemovableReverifier.");

    public UnderLeaseRecheck RecheckUnderLease(IReadOnlyList<Models.PatchClaim> claims) =>
        new(Array.Empty<string>());
}
