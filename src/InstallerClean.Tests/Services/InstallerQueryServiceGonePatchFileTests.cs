using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;

using RegistryPackageRecord = InstallerClean.Services.InstallerQueryService.RegistryPackageRecord;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Whether the missing-files warning counts a superseded or obsoleted patch's cached file
/// that has gone while something may still need it.
///
/// THE MACHINE, IN PLAIN WORDS. <see cref="Listed"/> holds a patch superseded, or
/// obsoleted, and a second program, <see cref="Holder"/>, is listed beside it. Where a test
/// says so, the holder holds the same patch in some state, and its own patch list leaves
/// that registration out, so its claim on the file never reaches the merge; asked by the
/// patch's code, it answers. The registry fallback holds the listed program's own package
/// record of the patch, as every machine's does, and a clean patch set for both programs.
///
/// THE FILE HAS GONE. The query service does not know that: the scan stamps it later, so
/// each test marks the row missing itself and asks the warning's own predicate. A patch
/// file that has gone does not read, and the reader here reads nothing.
/// </summary>
public class InstallerQueryServiceGonePatchFileTests
{
    private const uint BadConfiguration = 1610;

    private const string Listed = "{AAAAAAAA-0000-0000-0000-00000000000A}";
    private const string Holder = "{BBBBBBBB-0000-0000-0000-00000000000B}";
    private const string Patch = "{CCCCCCCC-0000-0000-0000-00000000000C}";
    private const string SecondCode = "{DDDDDDDD-0000-0000-0000-00000000000D}";
    private const string HolderOwnPatch = "{EEEEEEEE-0000-0000-0000-00000000000E}";

    private const string PatchFile = @"C:\Windows\Installer\gone-patch.msp";
    private const string ListedFile = @"C:\Windows\Installer\gone-patch-listed.msi";
    private const string HolderFile = @"C:\Windows\Installer\gone-patch-holder.msi";
    private const string HolderOwnPatchFile = @"C:\Windows\Installer\gone-patch-holder-own.msp";

    private const string MachineAccount = "S-1-5-18";

    // ---- A superseded patch the per-pairing pass asks about, its file gone ----

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_gone_superseded_file_is_reported_where_a_program_holds_the_patch_applied(bool holdsIt)
    {
        var msi = Machine(listedState: "2", listedUninstallable: "0");
        if (holdsIt) HoldsInvisibly(msi, Patch, state: "1");

        var row = Gone(await Scan(msi));

        Assert.False(row.IsRemovable);
        Assert.Equal(holdsIt, MissingFilesReport.Affected(row));
        if (holdsIt)
        {
            // Kept on the holder's claim, with the holder's reading and name on the row.
            Assert.False(row.RemovableWithheld);
            Assert.Equal(1, row.PatchState);
            Assert.Equal("A Holder", row.ProductName);
        }
        else
        {
            // Every installation answered and none holds the patch: the unread file is the
            // one reason left, and it reads as the file having gone.
            Assert.True(row.RemovableWithheld);
            Assert.True(row.WithheldOnUnreadableFile);
        }
    }

    [Fact]
    public async Task A_gone_superseded_file_an_installation_will_not_answer_about_is_reported()
    {
        var msi = Machine(listedState: "2", listedUninstallable: "0");
        HoldsInvisibly(msi, Patch, state: "1");
        msi.PatchPropertyResult[(Patch, Holder, "State")] = BadConfiguration;

        var row = Gone(await Scan(msi));

        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.False(row.WithheldOnUnreadableFile);
        Assert.True(MissingFilesReport.Affected(row));
    }

    /// <summary>
    /// The listed program registers the file under two codes, and the holder holds only the
    /// one named by <paramref name="heldCode"/>. The work list reaches the path under one
    /// code or the other, and which one must not decide what the warning says.
    /// </summary>
    [Theory]
    [InlineData(Patch)]
    [InlineData(SecondCode)]
    public async Task A_gone_file_named_by_two_codes_is_reported_whichever_code_the_holder_holds(string heldCode)
    {
        var msi = Machine(listedState: "2", listedUninstallable: "0");
        msi.AddPatch(Listed, SecondCode, PatchFile, state: "2", uninstallable: "0");
        HoldsInvisibly(msi, heldCode, state: "1");

        var row = Gone(await Scan(msi, OwnRecords(Patch, SecondCode)));

        Assert.False(row.IsRemovable);
        Assert.Equal(1, row.PatchState);
        Assert.True(MissingFilesReport.Affected(row));
    }

    // ---- A row no per-pairing question reaches ----

    /// <summary>
    /// The listed program holds the patch obsoleted, so the row is never removable and the
    /// per-pairing pass never asks about it.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_gone_obsoleted_file_is_reported_where_a_program_holds_it_applied(bool holdsIt)
    {
        var msi = Machine(listedState: "4", listedUninstallable: "0");
        if (holdsIt) HoldsInvisibly(msi, Patch, state: "1");

        var row = Gone(await Scan(msi));

        Assert.Equal(holdsIt, MissingFilesReport.Affected(row));
        if (holdsIt) Assert.Equal("A Holder", row.ProductName);
    }

    [Fact]
    public async Task A_gone_obsoleted_file_an_installation_will_not_answer_about_is_reported()
    {
        var msi = Machine(listedState: "4", listedUninstallable: "0");
        HoldsInvisibly(msi, Patch, state: "1");
        msi.PatchPropertyResult[(Patch, Holder, "State")] = BadConfiguration;

        var row = Gone(await Scan(msi));

        Assert.True(row.OtherHoldNotRuledOut);
        Assert.True(MissingFilesReport.Affected(row));
    }

    [Fact]
    public async Task A_gone_obsoleted_file_is_reported_where_a_program_holding_it_superseded_can_uninstall_another_patch()
    {
        // The holder holds the patch superseded, which says nothing about the file on its
        // own. What reaches for it is the patch the holder can uninstall, whose rollback can
        // open this one's cached file, and the holder is in the set the verdict is taken
        // across only because it answered.
        var msi = Machine(listedState: "4", listedUninstallable: "0");
        HoldsInvisibly(msi, Patch, state: "2");
        msi.AddPatch(Holder, HolderOwnPatch, HolderOwnPatchFile, state: "1", uninstallable: "1");

        var row = Gone(await Scan(msi, OwnRecords(Patch), removablePatchOn: Holder));

        Assert.Equal(ProductPatchSet.RemovablePatchPresent, row.ProductPatchSetVerdict);
        Assert.True(MissingFilesReport.Affected(row));
    }

    // ---- A package record that is not the patch's own ----

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_gone_file_a_programs_package_record_names_is_reported_though_the_row_is_not_removable(
        bool productRecord)
    {
        // Obsoleted, so the row is never removable. The registry also holds a product's
        // package record naming the file, which a sound registration never does; the row's
        // own records are there in both cases.
        var msi = Machine(listedState: "4", listedUninstallable: "0");
        RegistryPackageRecord[] records = productRecord
            ? [Own(Patch), new(Path.GetFullPath(PatchFile), IsPatch: false, Holder, MachineAccount)]
            : [Own(Patch)];

        var row = Gone(await Scan(msi, records));

        Assert.Equal(productRecord, row.OtherHoldNotRuledOut);
        Assert.Equal(productRecord, MissingFilesReport.Affected(row));
    }

    // ---- The machine ----

    private static FakeMsiApi Machine(string listedState, string listedUninstallable)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct(Listed);
        msi.SetProductProperty(Listed, "LocalPackage", ListedFile);
        msi.SetProductProperty(Listed, "ProductName", "A Program");
        msi.AddPatch(Listed, Patch, PatchFile, state: listedState, uninstallable: listedUninstallable);
        msi.AddProduct(Holder);
        msi.SetProductProperty(Holder, "LocalPackage", HolderFile);
        msi.SetProductProperty(Holder, "ProductName", "A Holder");
        return msi;
    }

    /// <summary>
    /// The holder holds <paramref name="code"/> in <paramref name="state"/>, and its own patch
    /// list does not name it: the keyed read answers and nothing else does.
    /// </summary>
    private static void HoldsInvisibly(FakeMsiApi msi, string code, string state)
    {
        msi.SetPatchProperty(code, Holder, "State", state);
        msi.SetPatchProperty(code, Holder, "Uninstallable", "0");
    }

    private static RegistryPackageRecord Own(string code) =>
        new(Path.GetFullPath(PatchFile), IsPatch: true, code, MachineAccount);

    private static RegistryPackageRecord[] OwnRecords(params string[] codes) => codes.Select(Own).ToArray();

    private static async Task<InstallerQueryResult> Scan(
        FakeMsiApi msi, RegistryPackageRecord[]? records = null, string? removablePatchOn = null)
    {
        string[] codes = [Listed, Holder];
        var patchSets = codes.ToDictionary(
            c => c,
            c => c == removablePatchOn ? ProductPatchSet.RemovablePatchPresent : ProductPatchSet.AllNonRemovable,
            StringComparer.OrdinalIgnoreCase);
        var fallback = new InstallerQueryService.FallbackRead(
            0, codes.Length,
            RegistryProductCodes: codes,
            ProductPatchSets: patchSets,
            PackageRecords: records ?? OwnRecords(Patch));

        return await new InstallerQueryService(msi, (_, _) => fallback, crashLogSink: null,
                identityReader: new ReadsNothing())
            .GetRegisteredPackagesAsync();
    }

    /// <summary>The patch's row, with its file marked gone as the scan would stamp it.</summary>
    private static RegisteredPackage Gone(InstallerQueryResult result) =>
        Assert.Single(result.Packages, r => r.LocalPackagePath.EndsWith(
            Path.GetFileName(PatchFile), StringComparison.OrdinalIgnoreCase)) with { FileExists = false };

    /// <summary>A reader for a folder whose patch files have gone: none of them reads.</summary>
    private sealed class ReadsNothing : IPackageIdentityReader
    {
        public PackageIdentity? Read(string filePath, bool isPatch, out string detail, out PackageReadRefusal refusal)
        {
            detail = string.Empty;
            refusal = PackageReadRefusal.WouldNotRead;
            return null;
        }

        public string? PackageCodeOf(string filePath) => null;

        public bool HoldsNoRevisionNumber(string filePath) => false;
    }
}
