using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Pins the one property of the action services' signatures that no call can
/// pin: that the under-lease claims have to be supplied.
/// </summary>
/// <remarks>
/// A TEST ABOUT A SHAPE RATHER THAN ABOUT BEHAVIOUR, and it is here because the
/// defect it guards against is invisible to every other kind. While the argument
/// was optional a caller could leave it out, the services turned an omitted one
/// into <see cref="UnderLeaseClaims.None"/>, and an empty batch returns at the
/// first line of the re-read. So the last check standing in front of a permanent
/// delete passed without asking anything, and nothing anywhere said so: the
/// files are deleted either way, and the only difference is whether a claim that
/// moved while the caller's enumeration was running was ever looked for.
///
/// No behavioural test reaches that, because the omission happens at the call
/// site and at compile time. What catches it now is the compiler. What keeps the
/// compiler catching it is this test, because one <c>= null</c> put back here
/// would restore the whole defect in a keystroke, with the suite still green.
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
