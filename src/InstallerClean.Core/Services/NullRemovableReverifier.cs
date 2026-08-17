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
/// made. Nothing upstream guarantees that pairing. Both action services call
/// the re-read unconditionally, an empty claim list being short-circuited
/// inside <see cref="RemovableReverifier"/>, which is the implementation this
/// stands in for rather than this one. So the guarantee is made here, by
/// refusing the claims instead of answering them: a test that reaches this with
/// a batch's claims in hand has wired the stand-in where the real reverifier
/// belongs, and a vacuous all-clear is exactly what must not be indistinguishable
/// from a real one.
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

    public UnderLeaseRecheck RecheckUnderLease(
        IReadOnlyList<Models.PatchClaim> claims,
        IReadOnlyList<Models.PatchClaim> siblingClaims) =>
        claims.Count == 0
            ? new UnderLeaseRecheck(Array.Empty<string>())
            : throw new NotSupportedException(
                "The no-op reverifier cannot re-read patch claims. Something resolved this in place of RemovableReverifier.");
}
