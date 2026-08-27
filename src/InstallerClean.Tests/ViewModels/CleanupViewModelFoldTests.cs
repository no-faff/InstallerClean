using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// What the fold does to the two claim lists it carries, which is a different
/// question from what it does to the two path lists.
/// </summary>
/// <remarks>
/// THE FOLD IS THE ONE PLACE IN THE APP THAT BUILDS A ReverifyResult OUT OF
/// ANOTHER ONE, so it is the one place a field can be dropped in the making. The
/// two claim lists are not symmetric and the tests here say so in both
/// directions: the surviving claims are keyed by path and follow their paths out
/// of the batch, while the sibling claims are keyed by PRODUCT and must not be
/// filtered by path at all, because the row that answers for a product can name
/// a path this fold has just condemned while another surviving path is still
/// registered to that same product.
///
/// Nothing reads the sibling half of a FOLDED result today: UnderLeaseClaims.From
/// is its only reader in the app and every fold happens after the action service
/// has returned. That is what makes these tests worth having rather than what
/// makes them unnecessary. The type exists so that a caller cannot hold one half
/// of the pair without the other, and a rebuild that drops a half puts that state
/// back inside the process whether or not anything reads it this month.
/// </remarks>
public class CleanupViewModelFoldTests
{
    private const string Held = @"C:\Windows\Installer\held.msp";
    private const string Kept = @"C:\Windows\Installer\kept.msp";
    private const string ProductOne = "{1111FFFF-0000-0000-0000-000000000001}";

    private static PatchClaim Claim(string path, string product = ProductOne) =>
        new(path, "{AAAA0000-0000-0000-0000-000000000001}", product, null, 4);

    [Fact]
    public void The_fold_carries_the_sibling_claims_through_whole()
    {
        // The sibling naming the condemned path is the interesting one: it
        // answers for a product the kept path is still registered to, so
        // filtering the list the way the surviving claims are filtered would
        // throw away the row that condemns the rest of that product.
        var siblings = new[] { Claim(Held), Claim(Kept) };
        var before = new ReverifyResult(
            new[] { Held, Kept }, Array.Empty<string>(),
            SurvivingPatchClaims: new[] { Claim(Held), Claim(Kept) },
            SiblingPatchClaims: siblings);

        var after = CleanupViewModel.FoldHeldBack(
            before, new[] { Held }, new HeldBackReasons(Reclaimed: 1));

        Assert.Equal(siblings, after.SiblingPatchClaims);
    }

    [Fact]
    public void The_fold_leaves_a_surviving_claim_that_names_a_held_back_path()
    {
        // The other direction, so a fix to the test above cannot be to stop
        // filtering both lists. These are keyed by path and go with their path.
        var before = new ReverifyResult(
            new[] { Held, Kept }, Array.Empty<string>(),
            SurvivingPatchClaims: new[] { Claim(Held), Claim(Kept) },
            SiblingPatchClaims: new[] { Claim(Held), Claim(Kept) });

        var after = CleanupViewModel.FoldHeldBack(
            before, new[] { Held }, new HeldBackReasons(Reclaimed: 1));

        Assert.Equal(new[] { Claim(Kept) }, after.SurvivingPatchClaims);
        Assert.Equal(new[] { Kept }, after.Surviving);
        Assert.Equal(new[] { Held }, after.Dropped);
    }

    [Fact]
    public void A_fold_of_a_result_with_no_claims_at_all_leaves_both_lists_empty()
    {
        // ReverifyResult takes both claim lists as nullable and reads a null back
        // as empty through a property initialiser. A with-expression copies the
        // already-coalesced values rather than re-running that initialiser, so no
        // null can arrive through the fold. Asserted rather than reasoned, because
        // the whole of the fix rests on what the copy does.
        var before = new ReverifyResult(new[] { Kept }, Array.Empty<string>());

        var after = CleanupViewModel.FoldHeldBack(
            before, new[] { Kept }, new HeldBackReasons(Reclaimed: 1));

        Assert.Empty(after.SurvivingPatchClaims);
        Assert.Empty(after.SiblingPatchClaims);
    }

    [Fact]
    public void A_fold_that_holds_nothing_back_returns_the_result_it_was_given()
    {
        var before = new ReverifyResult(
            new[] { Kept }, Array.Empty<string>(),
            SurvivingPatchClaims: new[] { Claim(Kept) },
            SiblingPatchClaims: new[] { Claim(Kept) });

        Assert.Same(before, CleanupViewModel.FoldHeldBack(
            before, Array.Empty<string>(), default));
    }
}
