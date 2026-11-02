using InstallerClean.Models;
using InstallerClean.Services;
using Microsoft.Win32;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// What the registry fallback claims out of one account's subtree, for a registration
/// recording its cached package under <c>ManagedLocalPackage</c>, the name a per-user
/// managed installation uses, as well as or instead of <c>LocalPackage</c>.
///
/// AN INTEGRATION TEST FOR THE REASON <see cref="LocalPackageValueTypeTests"/> IS ONE:
/// the reader walks a real <c>RegistryKey</c>. The subtree is built under a GUID-named
/// key in HKCU standing in for <c>UserData</c> and removed in a finally, so nothing
/// needs elevation and nothing outlives the test.
///
/// THE CACHED FILES ARE CREATED, EMPTY, IN A FOLDER OF THE TEST'S OWN UNDER THE
/// TEMPORARY FOLDER, and removed with it. A recorded path that exists resolves, so a
/// claim reaches the dictionary as a location and not as a counted refusal, and the
/// fallback's own existence check on an unclaimed path has something to find.
/// Claims are matched on their leaf names, which carry a GUID each, because what the
/// resolver makes of the folder above them is a fact about the machine running the
/// suite.
/// </summary>
public class RegistryFallbackCachedPackageTests
{
    private const string Sid = "S-1-5-21-1000000000-1000000000-1000000000-1001";

    // Real packed key names and the codes they unpack to, as pinned in
    // InstallerQueryServiceProductCodeUnpackTests and used in ProductPatchSetTests.
    // Real ones, so the fallback's own unpacking names the patch the assertions
    // look up rather than an expectation composed from the same reading of the
    // format the code applies.
    private const string PackedPatch = "4D54076CED4F5BA32BBD3E5FAD1CD4C9";
    private const string PatchCode = "{C67045D4-F4DE-3AB5-B2DB-E3F5DAC14D9C}";
    private const string PackedProduct = "2D0058F6F08A743309184BE1178C95B2";

    [Fact]
    public void A_product_recording_its_package_only_under_ManagedLocalPackage_is_claimed()
    {
        WithSubtree(f =>
        {
            var managed = f.Files.Create("msi");
            using (var props = ProductValues(f))
                props.SetValue("ManagedLocalPackage", managed, RegistryValueKind.String);

            var (read, claimed) = Read(f);

            Assert.Equal(0, read.Failures);
            Assert.Single(claimed.Keys, k => EndsWithLeaf(k, managed));
            Assert.Equal(1, read.UnclaimedProductFiles);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_products_own_patch_listing_is_kept_under_its_account_only_where_its_Patches_key_is_there(
        bool patchesKey)
    {
        WithSubtree(f =>
        {
            using (var props = ProductValues(f))
                props.SetValue("LocalPackage", f.Files.Create("msi"), RegistryValueKind.String);
            if (patchesKey)
                f.Account.CreateSubKey($@"Products\{PackedProduct}\Patches\{PackedPatch}", writable: true)!.Dispose();

            var (read, _) = Read(f);

            var product = InstallerQueryService.UnpackRegistryProductCode(PackedProduct)!;
            Assert.NotNull(read.PatchListings);
            if (patchesKey)
                Assert.Equal(new[] { PatchCode },
                    read.PatchListings[new InstallerQueryService.AccountCode(Sid, product)]);
            else
                Assert.Empty(read.PatchListings);
            // The product's package record carries the account it was read under.
            Assert.Equal(Sid, Assert.Single(read.PackageRecords!, r => !r.IsPatch).Account);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_product_key_is_an_entry_under_its_account_whether_or_not_it_has_a_Patches_key(bool patchesKey)
    {
        WithSubtree(f =>
        {
            f.Account.CreateSubKey($@"Products\{PackedProduct}", writable: true)!.Dispose();
            if (patchesKey)
                f.Account.CreateSubKey($@"Products\{PackedProduct}\Patches\{PackedPatch}", writable: true)!.Dispose();

            var (read, _) = Read(f);

            var entry = Assert.Single(read.ProductEntries!);
            Assert.Equal(Sid, entry.Account);
            Assert.Equal(InstallerQueryService.UnpackRegistryProductCode(PackedProduct), entry.Code);
            Assert.Equal(patchesKey ? [PatchCode] : [], entry.PatchCodes);
            Assert.Equal(0, read.Failures);
        });
    }

    [Fact]
    public void A_listing_holding_a_name_that_is_no_code_is_an_entry_with_no_listing()
    {
        WithSubtree(f =>
        {
            f.Account.CreateSubKey($@"Products\{PackedProduct}\Patches\not-a-packed-code", writable: true)!.Dispose();

            var (read, _) = Read(f);

            Assert.Null(Assert.Single(read.ProductEntries!).PatchCodes);
        });
    }

    [Fact]
    public void A_product_recording_both_values_claims_both_and_counts_once()
    {
        // Two paths, one product. The unclaimed-file figure is weighed against a count
        // of products, so the entry adds one to it and not two.
        WithSubtree(f =>
        {
            var local = f.Files.Create("msi");
            var managed = f.Files.Create("msi");
            using var props = ProductValues(f);
            props.SetValue("LocalPackage", local, RegistryValueKind.String);
            props.SetValue("ManagedLocalPackage", managed, RegistryValueKind.String);

            var (read, claimed) = Read(f);

            Assert.Equal(0, read.Failures);
            Assert.Single(claimed.Keys, k => EndsWithLeaf(k, local));
            Assert.Single(claimed.Keys, k => EndsWithLeaf(k, managed));
            Assert.Equal(1, read.UnclaimedProductFiles);
        });
    }

    [Fact]
    public void A_product_ManagedLocalPackage_that_is_not_a_string_is_a_counted_failure()
    {
        // Present and unreadable is a read that failed, on this name as on the other.
        // It reaches the failure count the degraded-sources gate weighs and the
        // non-string count the report carries, and the LocalPackage beside it is still
        // claimed.
        WithSubtree(f =>
        {
            var local = f.Files.Create("msi");
            using var props = ProductValues(f);
            props.SetValue("LocalPackage", local, RegistryValueKind.String);
            props.SetValue("ManagedLocalPackage", Array.Empty<byte>(), RegistryValueKind.None);

            var (read, claimed) = Read(f);

            Assert.Equal(1, read.Failures);
            Assert.Equal(1, read.NonStringLocalPackageValues);
            Assert.Single(claimed.Keys, k => EndsWithLeaf(k, local));
        });
    }

    [Fact]
    public void A_patch_recording_only_LocalPackage_is_recorded_as_its_path()
    {
        // The control for the tests below: the subtree, the unpacking and the reach
        // map all work on a registration of the shape every machine has, so a result
        // below that differs from this one is about the managed value and not about
        // the fixture.
        WithSubtree(f =>
        {
            var local = f.Files.Create("msp");
            using (var patch = PatchValues(f))
                patch.SetValue("LocalPackage", local, RegistryValueKind.String);

            var (read, claimed) = Read(f);

            Assert.Equal(0, read.Failures);
            Assert.Single(claimed.Keys, k => EndsWithLeaf(k, local));
            var paths = RecordedPaths(read);
            Assert.NotNull(paths);
            Assert.Single(paths, p => EndsWithLeaf(p, local));
        });
    }

    [Fact]
    public void A_patch_recording_only_ManagedLocalPackage_is_claimed_and_recorded_as_its_path()
    {
        WithSubtree(f =>
        {
            var managed = f.Files.Create("msp");
            using (var patch = PatchValues(f))
                patch.SetValue("ManagedLocalPackage", managed, RegistryValueKind.String);

            var (read, claimed) = Read(f);

            Assert.Equal(0, read.Failures);
            Assert.Single(claimed.Keys, k => EndsWithLeaf(k, managed));
            Assert.Equal(1, read.UnclaimedPatchFiles);
            var paths = RecordedPaths(read);
            Assert.NotNull(paths);
            Assert.Single(paths, p => EndsWithLeaf(p, managed));
        });
    }

    [Fact]
    public void A_patch_recording_both_values_records_both_paths()
    {
        // A recovered product holding this patch is judged against either file, so
        // both have to be in the set: a set of one would let the other go.
        WithSubtree(f =>
        {
            var local = f.Files.Create("msp");
            var managed = f.Files.Create("msp");
            using var patch = PatchValues(f);
            patch.SetValue("LocalPackage", local, RegistryValueKind.String);
            patch.SetValue("ManagedLocalPackage", managed, RegistryValueKind.String);

            var (read, _) = Read(f);

            var paths = RecordedPaths(read);
            Assert.NotNull(paths);
            Assert.Equal(2, paths.Count);
            Assert.Single(paths, p => EndsWithLeaf(p, local));
            Assert.Single(paths, p => EndsWithLeaf(p, managed));
            Assert.Equal(1, read.UnclaimedPatchFiles);
        });
    }

    [Fact]
    public void A_patch_whose_ManagedLocalPackage_will_not_read_leaves_its_paths_unestablished()
    {
        // The readable LocalPackage is claimed, and the patch's recorded paths are
        // still nothing established: the value that would not read may be recording
        // any path, so a recovered product holding this patch is judged against every
        // path rather than against the one that did read.
        WithSubtree(f =>
        {
            var local = f.Files.Create("msp");
            using var patch = PatchValues(f);
            patch.SetValue("LocalPackage", local, RegistryValueKind.String);
            patch.SetValue("ManagedLocalPackage", Array.Empty<byte>(), RegistryValueKind.None);

            var (read, claimed) = Read(f);

            Assert.Equal(1, read.Failures);
            Assert.Equal(1, read.NonStringLocalPackageValues);
            Assert.Single(claimed.Keys, k => EndsWithLeaf(k, local));
            Assert.NotNull(read.Reach.CachedPathsByPatchCode);
            Assert.True(read.Reach.CachedPathsByPatchCode.ContainsKey(PatchCode));
            Assert.Null(RecordedPaths(read));
        });
    }

    [Theory]
    [InlineData("LocalPackage", "ManagedLocalPackage")]
    [InlineData("ManagedLocalPackage", "LocalPackage")]
    public void A_patch_with_one_value_empty_beside_a_path_leaves_its_paths_unestablished(
        string emptyName, string pathName)
    {
        // Empty is not unreadable and not absent: the value is there, reads as text,
        // and records no path. A recovered product holding this patch is judged
        // against every path, as it is for a patch recording no path at all, so the
        // path the other name gives is claimed and the patch's recorded paths are
        // still nothing established. Nothing about it is a failed read.
        WithSubtree(f =>
        {
            var named = f.Files.Create("msp");
            using var patch = PatchValues(f);
            patch.SetValue(emptyName, "", RegistryValueKind.String);
            patch.SetValue(pathName, named, RegistryValueKind.String);

            var (read, claimed) = Read(f);

            Assert.Equal(0, read.Failures);
            Assert.Single(claimed.Keys, k => EndsWithLeaf(k, named));
            Assert.NotNull(read.Reach.CachedPathsByPatchCode);
            Assert.True(read.Reach.CachedPathsByPatchCode.ContainsKey(PatchCode));
            Assert.Null(RecordedPaths(read));
        });
    }

    private static IReadOnlyCollection<string>? RecordedPaths(InstallerQueryService.FallbackRead read)
    {
        Assert.NotNull(read.Reach.CachedPathsByPatchCode);
        return read.Reach.CachedPathsByPatchCode.TryGetValue(PatchCode, out var paths) ? paths : null;
    }

    private static bool EndsWithLeaf(string path, string file) =>
        path.EndsWith(@"\" + Path.GetFileName(file), StringComparison.OrdinalIgnoreCase);

    private static RegistryKey ProductValues(Fixture f) =>
        f.Account.CreateSubKey($@"Products\{PackedProduct}\InstallProperties", writable: true);

    private static RegistryKey PatchValues(Fixture f) =>
        f.Account.CreateSubKey($@"Patches\{PackedPatch}", writable: true);

    /// <summary>
    /// The fallback's read of the one account in the fixture, into a claims dictionary
    /// built as the scan builds its own, with the failure log's sink discarded so a
    /// counted failure does not write to the real crash log.
    /// </summary>
    private static (InstallerQueryService.FallbackRead Read, Dictionary<string, RegisteredPackage> Claimed)
        Read(Fixture f)
    {
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);
        var read = InstallerQueryService.ReadFallbackSid(
            f.UserData, Sid, claimed,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None,
            new PerItemFailureLog("Registry fallback", "Test run.", _ => { }));
        return (read, claimed);
    }

    private sealed record Fixture(RegistryKey UserData, RegistryKey Account, TempFiles Files);

    private sealed class TempFiles(string folder)
    {
        public string Create(string extension)
        {
            var path = Path.Combine(folder, $"{Guid.NewGuid():N}.{extension}");
            File.WriteAllBytes(path, []);
            return path;
        }
    }

    /// <summary>
    /// Builds a throwaway stand-in for <c>UserData</c> under HKCU holding one account's
    /// subtree, and a throwaway folder for the cached files, runs the body, and removes
    /// both whatever happens. The shared parent key is removed only when it is left
    /// empty, so a concurrent run cannot delete another one's key out from under it.
    /// </summary>
    private static void WithSubtree(Action<Fixture> body)
    {
        var keyPath = $@"Software\InstallerCleanTests\{Guid.NewGuid():N}";
        var folder = Path.Combine(Path.GetTempPath(), $"ictest-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(folder);
            using var userData = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
            Assert.NotNull(userData);
            using var account = userData.CreateSubKey(Sid, writable: true);
            Assert.NotNull(account);
            body(new Fixture(userData, account, new TempFiles(folder)));
        }
        finally
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false); }
            catch (Exception) { /* the test's verdict must not turn on the tidy-up */ }

            try
            {
                using var parent = Registry.CurrentUser.OpenSubKey(@"Software\InstallerCleanTests");
                if (parent is not null && parent.SubKeyCount == 0 && parent.ValueCount == 0)
                {
                    parent.Dispose();
                    Registry.CurrentUser.DeleteSubKey(@"Software\InstallerCleanTests", throwOnMissingSubKey: false);
                }
            }
            catch (Exception) { /* as above */ }

            try { Directory.Delete(folder, recursive: true); }
            catch (Exception) { /* as above */ }
        }
    }
}
