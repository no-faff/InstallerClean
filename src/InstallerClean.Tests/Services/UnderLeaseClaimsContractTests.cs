using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Pins the one property of the action services' signatures that no call can
/// pin: that the under-lease claims have to be supplied.
/// </summary>
/// <remarks>
/// A TEST ABOUT A SHAPE RATHER THAN ABOUT BEHAVIOUR, and it is here because what it
/// guards is invisible to every other kind. Made optional, the argument could be left
/// out at a call site, an omitted one would become <see cref="UnderLeaseClaims.None"/>,
/// and an empty batch returns at the first line of the re-read, so the check in front
/// of a permanent delete would never look for a claim that moved while the caller's
/// enumeration was running.
///
/// No behavioural test reaches that, because the omission happens at the call site and
/// at compile time. The compiler catches it while the argument is required and not
/// nullable, and this test holds it to both: one <c>= null</c> added to the signature
/// lets a call site leave it out, and a nullable type lets one pass nothing.
/// </remarks>
public class UnderLeaseClaimsContractTests
{
    [Theory]
    [InlineData(typeof(IDeleteFilesService), nameof(IDeleteFilesService.DeleteFilesAsync))]
    [InlineData(typeof(IMoveFilesService), nameof(IMoveFilesService.MoveFilesAsync))]
    public void The_under_lease_claims_cannot_be_omitted(Type service, string method)
    {
        var parameter = service.GetMethod(method)!.GetParameters()
            .Single(p => p.Name == "underLeaseClaims");

        // Optional is the whole defect: an omitted argument re-reads nothing.
        Assert.False(parameter.IsOptional);

        // And not nullable either. A caller with no claims says so with
        // UnderLeaseClaims.None, which is the difference between having looked
        // and having forgotten to; a nullable parameter puts the second back
        // within reach even with the default gone.
        Assert.Equal(typeof(UnderLeaseClaims), parameter.ParameterType);
    }
}
