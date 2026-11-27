using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services;

/// <summary>
/// A program still lists a patch, as superseded, and the patch's own registration has
/// gone: Windows Installer answers that it knows no such patch when asked for the patch's
/// cached file, and the registry holds no key for the patch under the program's account.
/// Windows can leave a machine in this state itself, where it removes one of two
/// programs that shared a patch's registration.
/// </summary>
public class InstallerQueryServiceGoneRegistrationTests
{
    private const string Lister = "{AAAAAAAA-2222-0000-0000-00000000000A}";
    private const string ListerFile = @"C:\Windows\Installer\gone-lister.msi";
    private const string GonePatch = "{CCCCCCCC-2222-0000-0000-00000000000C}";

    private const string Other = "{BBBBBBBB-2222-0000-0000-00000000000B}";
    private const string OtherFile = @"C:\Windows\Installer\gone-other.msi";
    private const string OtherPatch = "{DDDDDDDD-2222-0000-0000-00000000000D}";
    private const string OtherPatchFile = @"C:\Windows\Installer\gone-other.msp";

    private const uint UnknownPatch = 1647;

    /// <summary>
    /// <see cref="Lister"/> lists <see cref="GonePatch"/> and answers 1647 for its cached
    /// file. <see cref="Other"/>, an unrelated program, holds a superseded patch of its own
    /// that is listed on a machine with nothing else to say.
    /// </summary>
    private static FakeMsiApi AMachine()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct(Lister);
        msi.SetProductProperty(Lister, "LocalPackage", ListerFile);
        msi.AddPatch(Lister, GonePatch, localPackage: "", state: "2", uninstallable: "0");
        msi.PatchPropertyResult[(GonePatch, Lister, "LocalPackage")] = UnknownPatch;

        msi.AddProduct(Other);
        msi.SetProductProperty(Other, "LocalPackage", OtherFile);
        msi.AddPatch(Other, OtherPatch, OtherPatchFile, state: "2", uninstallable: "0");
        return msi;
    }

    private static Task<InstallerQueryResult> Scan(FakeMsiApi msi, InstallerQueryService.FallbackRead fallback) =>
        new InstallerQueryService(msi, (_, _) => fallback, crashLogSink: null, identityReader: new NamingNobody())
            .GetRegisteredPackagesAsync();

    /// <summary>A reader for which every patch file reads and names no program.</summary>
    private sealed class NamingNobody : IPackageIdentityReader
    {
        public PackageIdentity? Read(string filePath, bool isPatch, out string detail, out PackageReadRefusal refusal)
        {
            detail = string.Empty;
            refusal = PackageReadRefusal.WouldNotRead;
            return new PackageIdentity(string.Empty, isPatch, Array.Empty<string>());
        }

        public string? PackageCodeOf(string filePath) => null;

        public bool HoldsNoRevisionNumber(string filePath) => false;
    }

    private const string LocalSystem = "S-1-5-18";
    private const string AnAccount = "S-1-5-21-1111111111-2222222222-3333333333-1001";

    /// <summary>
    /// A registry read that failed nowhere, with the patch keys it listed under each
    /// account in <paramref name="patchKeys"/>: an account with an entry is one whose
    /// <c>Patches</c> key was listed to its end, and an account with none is one whose
    /// listing was not established. Null is a result with no listing at all.
    /// </summary>
    private static InstallerQueryService.FallbackRead ARegistry(
        Dictionary<string, IReadOnlyCollection<string>>? patchKeys, int failures = 0) =>
        new(Failures: failures, ProductKeys: 2,
            ProductPatchSets: new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase)
            {
                [Lister] = ProductPatchSet.AllNonRemovable,
                [Other] = ProductPatchSet.AllNonRemovable,
            },
            PackageRecords: [],
            PatchKeys: patchKeys);

    /// <summary>The local system's <c>Patches</c> key listed to its end, holding the other program's patch alone.</summary>
    private static Dictionary<string, IReadOnlyCollection<string>> NoKeyForThePatch(string account = LocalSystem) =>
        new(StringComparer.OrdinalIgnoreCase) { [account] = new[] { OtherPatch } };

    private static RegisteredPackage TheOtherPatch(InstallerQueryResult result) =>
        result.Packages.Single(p => p.PatchState == 2 && p.ProductCode == Other);

    /// <summary>
    /// THE CONTROL. Without the patch that has no registration, nothing is unsettled and
    /// nothing is counted, so each test below turns on that one pairing.
    /// </summary>
    [Fact]
    public async Task A_machine_without_such_a_patch_settles_every_program_and_counts_none()
    {
        var msi = AMachine();
        msi.PatchPropertyResult.Remove((GonePatch, Lister, "LocalPackage"));
        msi.PatchCodes.Remove(Lister);

        var result = await Scan(msi, ARegistry(NoKeyForThePatch()));

        Assert.Equal(0, result.Census.UnsettledEnumeratedProductCount);
        Assert.Equal(0, result.Census.PatchesListedWithoutRegistrationCount);
        Assert.True(TheOtherPatch(result).IsRemovable);
    }

    [Fact]
    public async Task A_patch_a_program_lists_with_no_registration_leaves_nothing_unsettled_and_is_counted()
    {
        var result = await Scan(AMachine(), ARegistry(NoKeyForThePatch()));

        // Windows Installer answered that it knows no such patch, and the registry holds no
        // key for it under the program's account, so no record of a cached file was lost.
        Assert.Equal(0, result.Census.UnsettledEnumeratedProductCount);
        Assert.Equal(1, result.Census.PatchesListedWithoutRegistrationCount);
        // And its program is not one whose records came back short.
        Assert.Equal(0, result.Census.UnreadableProducts);

        // The other program's superseded patch stays listed.
        var row = TheOtherPatch(result);
        Assert.False(row.WithheldScanWide);
        Assert.True(row.IsRemovable);
    }

    [Fact]
    public async Task A_registry_listing_that_was_not_established_for_the_account_leaves_the_program_unsettled()
    {
        // Another account's Patches key was listed, and the local system's was not.
        var result = await Scan(AMachine(), ARegistry(NoKeyForThePatch(AnAccount)));

        Assert.Equal(1, result.Census.UnsettledEnumeratedProductCount);
        Assert.Equal(0, result.Census.PatchesListedWithoutRegistrationCount);
        Assert.Equal(1, result.Census.UnreadableProducts);
        AssertEverySupersededPatchIsWithheld(result);
    }

    [Fact]
    public async Task A_result_with_no_registry_listing_leaves_the_program_unsettled()
    {
        var result = await Scan(AMachine(), ARegistry(patchKeys: null));

        Assert.Equal(1, result.Census.UnsettledEnumeratedProductCount);
        Assert.Equal(0, result.Census.PatchesListedWithoutRegistrationCount);
        AssertEverySupersededPatchIsWithheld(result);
    }

    [Fact]
    public async Task A_patch_whose_key_is_there_and_names_no_file_leaves_the_program_unsettled()
    {
        // The key is there, so the registration has not gone: it records no path this scan
        // can read, which is a lost record.
        var keys = NoKeyForThePatch();
        keys[LocalSystem] = new[] { OtherPatch, GonePatch };

        var result = await Scan(AMachine(), ARegistry(keys));

        Assert.Equal(1, result.Census.UnsettledEnumeratedProductCount);
        Assert.Equal(0, result.Census.PatchesListedWithoutRegistrationCount);
        AssertEverySupersededPatchIsWithheld(result);
    }

    [Theory]
    [InlineData(1610u)] // the configuration data is corrupt
    [InlineData(5u)]    // access denied
    [InlineData(1605u)] // the product is not installed, of a product the scan has just listed
    public async Task Any_other_failed_read_of_the_patchs_file_leaves_the_program_unsettled(uint error)
    {
        var msi = AMachine();
        msi.PatchPropertyResult[(GonePatch, Lister, "LocalPackage")] = error;

        var result = await Scan(msi, ARegistry(NoKeyForThePatch()));

        Assert.Equal(1, result.Census.UnsettledEnumeratedProductCount);
        Assert.Equal(0, result.Census.PatchesListedWithoutRegistrationCount);
        AssertEverySupersededPatchIsWithheld(result);
    }

    [Fact]
    public async Task An_answer_of_no_record_that_a_read_made_again_does_not_repeat_leaves_the_program_unsettled()
    {
        // The first read answers that the installation holds no record of the patch, and
        // each read made again is refused. The answer the scan takes is the first, and
        // it is not the answer every time, so the record counts as lost.
        var msi = AMachine();
        msi.PatchPropertyResult[(GonePatch, Lister, "LocalPackage")] = 5;
        msi.PassingReadError(GonePatch, Lister, "LocalPackage", UnknownPatch, calls: 1);

        var result = await Scan(msi, ARegistry(NoKeyForThePatch()));

        Assert.Equal(1, result.Census.UnsettledEnumeratedProductCount);
        Assert.Equal(0, result.Census.PatchesListedWithoutRegistrationCount);
        AssertEverySupersededPatchIsWithheld(result);
    }

    [Theory]
    [InlineData(AnAccount, 0, 1)]
    // The key is looked for under the account the program's records are under, and the
    // local system's listing does not answer for another account's.
    [InlineData(LocalSystem, 1, 0)]
    public async Task A_per_user_programs_patch_is_looked_for_under_that_programs_own_account(
        string listedAccount, int unsettled, int counted)
    {
        var msi = AMachine();
        msi.WalkInstances[Lister] = (AnAccount, MsiInstallContext.UserUnmanaged);
        msi.KeyedInstances[Lister] = [(AnAccount, MsiInstallContext.UserUnmanaged)];

        var result = await Scan(msi, ARegistry(NoKeyForThePatch(listedAccount)));

        Assert.Equal(unsettled, result.Census.UnsettledEnumeratedProductCount);
        Assert.Equal(counted, result.Census.PatchesListedWithoutRegistrationCount);
    }

    [Fact]
    public async Task Such_a_patch_does_not_refuse_the_scan_where_another_registry_read_failed()
    {
        // A read of some other cached-file record failed. A program whose own records came
        // back short refuses the scan beside that; this program lost no record.
        var result = await Scan(AMachine(), ARegistry(NoKeyForThePatch(), failures: 1));

        Assert.Equal(1, result.Census.PatchesListedWithoutRegistrationCount);
    }

    [Fact]
    public async Task A_program_that_did_lose_a_record_still_refuses_the_scan_where_another_registry_read_failed()
    {
        var msi = AMachine();
        msi.PatchPropertyResult[(GonePatch, Lister, "LocalPackage")] = 1610;

        var refusal = await Assert.ThrowsAnyAsync<Exception>(
            () => Scan(msi, ARegistry(NoKeyForThePatch(), failures: 1)));

        Assert.Equal(StopMessages.NothingRemoved(Strings.Error_ScanRecordsUnreadable), refusal.Message);
    }

    private const string OwnPatch = "{EEEEEEEE-2222-0000-0000-00000000000E}";
    private const string OwnPatchFile = @"C:\Windows\Installer\gone-lister-own.msp";

    /// <summary>
    /// The patch whose registration has gone is still one of its program's patches, so
    /// whether it could be uninstalled still decides that program's other superseded
    /// patches. The registry's own reading says nothing here can be uninstalled, so the
    /// verdict below is the answer Windows Installer gave for the patch.
    /// </summary>
    [Theory]
    [InlineData("0", ProductPatchSet.AllNonRemovable)]
    [InlineData("1", ProductPatchSet.RemovablePatchPresent)]
    public async Task The_programs_own_superseded_patch_is_judged_on_what_Windows_answers_for_the_gone_patch(
        string uninstallable, ProductPatchSet verdict)
    {
        var msi = AMachine();
        msi.SetPatchProperty(GonePatch, Lister, "Uninstallable", uninstallable);
        msi.AddPatch(Lister, OwnPatch, OwnPatchFile, state: "2", uninstallable: "0");

        var result = await Scan(msi, ARegistry(NoKeyForThePatch()));

        Assert.Equal(1, result.Census.PatchesListedWithoutRegistrationCount);
        var own = result.Packages.Single(p => p.PatchState == 2 && p.ProductCode == Lister);
        Assert.Equal(verdict, own.ProductPatchSetVerdict);
    }

    [Fact]
    public async Task A_gone_patch_whose_Uninstallable_will_not_read_leaves_its_programs_patch_set_unestablished()
    {
        var msi = AMachine();
        msi.PatchPropertyResult[(GonePatch, Lister, "Uninstallable")] = 1610;
        msi.AddPatch(Lister, OwnPatch, OwnPatchFile, state: "2", uninstallable: "0");

        var result = await Scan(msi, ARegistry(NoKeyForThePatch()));

        var own = result.Packages.Single(p => p.PatchState == 2 && p.ProductCode == Lister);
        Assert.NotEqual(ProductPatchSet.AllNonRemovable, own.ProductPatchSetVerdict);
        Assert.False(own.IsRemovable);
    }

    private const string Spender = "{99999999-2222-0000-0000-000000000009}";

    /// <summary>
    /// The same machine with a program walked first whose six patches each fail their
    /// path read at every attempt, which spends the pauses the pass shares. The read of
    /// the gone patch's path after that is made once, with no read made again.
    /// </summary>
    private static FakeMsiApi AMachineWhosePausesAreSpent()
    {
        var msi = AMachine();
        msi.Products.Insert(0, (Spender, 0));
        for (var i = 1; i <= 6; i++)
        {
            var patch = $"{{99999999-2222-0000-0000-00000000010{i}}}";
            msi.AddPatch(Spender, patch, localPackage: "", state: "1", uninstallable: "0");
            msi.PatchPropertyResult[(patch, Spender, "LocalPackage")] = 1610;
        }

        return msi;
    }

    [Fact]
    public async Task Where_the_pauses_are_spent_the_path_is_read_once_more_and_the_same_answer_counts_the_patch()
    {
        var msi = AMachineWhosePausesAreSpent();

        var result = await Scan(msi, ARegistry(NoKeyForThePatch()));

        // Read once by the pass, with no pause left for another, and once more on the spot.
        Assert.Equal(2, msi.Reads(GonePatch, Lister, "LocalPackage"));
        Assert.Equal(1, result.Census.PatchesListedWithoutRegistrationCount);
    }

    [Fact]
    public async Task Where_the_pauses_are_spent_a_second_read_that_answers_otherwise_leaves_the_record_lost()
    {
        var msi = AMachineWhosePausesAreSpent();
        msi.PatchPropertyResult[(GonePatch, Lister, "LocalPackage")] = 5;
        msi.PassingReadError(GonePatch, Lister, "LocalPackage", UnknownPatch, calls: 1);

        var result = await Scan(msi, ARegistry(NoKeyForThePatch()));

        Assert.Equal(2, msi.Reads(GonePatch, Lister, "LocalPackage"));
        Assert.Equal(0, result.Census.PatchesListedWithoutRegistrationCount);
        // The spender and the lister are both programs the scan could not settle.
        Assert.Equal(2, result.Census.UnsettledEnumeratedProductCount);
    }

    private static void AssertEverySupersededPatchIsWithheld(InstallerQueryResult result)
    {
        var row = TheOtherPatch(result);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.True(row.WithheldScanWide);
    }
}
