using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The scan's third source for product packages: what a cached installation
/// package says it belongs to, put to Windows.
///
/// IT IS THE ONLY THING IN THE TREE THAT STARTS AT THE FILE. Every other way the
/// scan decides a cached file is spare starts at a registration and looks for the
/// file it names, so where a product's records hold no usable path there is
/// nothing for any of them to work from. That is the class this reaches, and the
/// tests below are about the direction it fails in: only a POSITIVE answer that
/// Windows does not hold the declared product lets a file through, and every
/// inability keeps it.
///
/// AND IT REACHES THAT CLASS FOR PRODUCT PACKAGES ONLY. A patch is refused
/// outright, which two tests here pin, because Windows holds a record of every
/// registered superseded patch by construction and the keeping arm would
/// therefore be true of that whole class on every machine.
///
/// BOTH FAKES THROW ON ANYTHING NO TEST SCRIPTED, which is the point of them
/// rather than strictness. A fake answering an unscripted question with a
/// plausible default is how a test comes to pass without ever reaching its own
/// subject, and the two questions here have a permissive answer each: "Windows
/// does not hold that product" and "there was nothing to read". Either as a
/// default would let a test assert the file was offered while proving nothing
/// about why.
/// </summary>
public class DeclaredProductCheckTests
{
    private const string ProductA = "{11111111-1111-1111-1111-111111111111}";
    private const string ProductB = "{22222222-2222-2222-2222-222222222222}";

    private static OrphanedFile Package(string path) =>
        new(path, 100, IsPatch: false, IsRemovablePatch: false, IsObsoleted: false, Reason: "orphaned");

    private static OrphanedFile Patch(string path) =>
        new(path, 100, IsPatch: true, IsRemovablePatch: false, IsObsoleted: false, Reason: "orphaned");

    // ---- The keeping arm: Windows still holds the declared product ----

    [Fact]
    public void A_package_whose_declared_product_Windows_holds_is_kept_back()
    {
        // THE WHOLE POINT OF THE CHECK. Nothing registered names this file, so
        // every other mechanism in the scan has already let it through; the file
        // itself says which product it belongs to, and Windows still has that
        // product.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var outcomes = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") });

        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcomes[0]);
        Assert.True(outcomes[0].Withholds());
    }

    // ---- The one permitting arm, which is the must-miss control for the above ----

    [Fact]
    public void A_package_Windows_says_it_does_not_hold_is_left_where_it_was()
    {
        // The must-miss half. Without it the test above passes just as well
        // against a check that keeps every file back, which is a check that has
        // emptied the offer and looks identical from the outside.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.UnknownProduct);

        var outcomes = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") });

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcomes[0]);
        Assert.False(outcomes[0].Withholds());
    }

    [Fact]
    public void NoMoreItems_is_the_other_return_that_means_the_product_is_not_there()
    {
        // Two returns are allowed to mean absence and the code has to accept both,
        // so pinning only the obvious one would leave the second free to be
        // dropped: a keyed enumeration that runs out of rows has answered, and
        // reading that as an inability would keep every file on every machine.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.NoMoreItems);

        var outcomes = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") });

        Assert.Equal(DeclaredProductOutcome.DeclaredProductNotInstalled, outcomes[0]);
    }

    // ---- Every inability keeps the file, which is the half easiest to get wrong ----

    [Fact]
    public void A_return_that_is_not_on_the_absence_allowlist_keeps_the_file()
    {
        // THE ARM THAT DECIDES WHETHER THIS CHECK IS WORTH HAVING. A call that
        // could not be made has not shown the product to be absent. Treating any
        // non-success as "no product" would offer the file on the strength of a
        // question that was never really put, which is the exact collapse the
        // check exists to prevent, and it would look like a working check.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Answers(ProductA, MsiError.AccessDenied);

        var outcomes = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") });

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcomes[0]);
        Assert.True(outcomes[0].Withholds());
    }

    [Fact]
    public void A_package_that_will_not_yield_an_identity_is_kept_back()
    {
        // A file that would not open, a database with no Property table, a
        // ProductCode that is not a GUID: the reader reports all of them as
        // nothing to ask about, and none of them is evidence that the file is
        // spare. This is the outcome an earlier design of this work got backwards.
        var identities = new ScriptedPackageIdentities();
        identities.YieldsNothing(@"C:\Windows\Installer\a.msi");

        var outcomes = new DeclaredProductCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") });

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcomes[0]);
        Assert.True(outcomes[0].Withholds());
    }

    [Fact]
    public void A_reading_that_yields_an_empty_code_is_kept_back()
    {
        // Not the same shape as the test above and not redundant with it. A
        // do-nothing reader hands back an identity carrying no code rather than a
        // null, which is a value that would reach a keyed enumeration and be
        // answered about nothing.
        var identities = new ScriptedPackageIdentities();
        identities.Yields(@"C:\Windows\Installer\a.msi",
            new PackageIdentity(string.Empty, IsPatch: false, Array.Empty<string>()));

        var outcomes = new DeclaredProductCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") });

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcomes[0]);
    }

    [Fact]
    public void A_reading_that_comes_back_marked_as_a_patch_is_kept_back()
    {
        // The product reading was asked for and something else came back. A patch
        // code put to a keyed PRODUCT enumeration is a question about nothing, and
        // the answer would be an absence that means only that the wrong thing was
        // asked.
        var identities = new ScriptedPackageIdentities();
        identities.Yields(@"C:\Windows\Installer\a.msi",
            new PackageIdentity(ProductA, IsPatch: true, new[] { ProductB }));

        var outcomes = new DeclaredProductCheck(new ScriptedMsiProducts(), identities)
            .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") });

        Assert.Equal(DeclaredProductOutcome.Unestablished, outcomes[0]);
    }

    // ---- The restriction, which is the control on the whole design ----

    [Fact]
    public void A_patch_is_never_read_and_never_asked_about()
    {
        // THE CONTROL THAT PROVES THE RESTRICTION IS REAL RATHER THAN INTENDED,
        // and the fixture is built the hostile way round on purpose: the msi fake
        // would answer "installed" to any product code it were given, and the
        // identity reader would throw if asked to read this path at all. So the
        // test can only pass if the patch never reached either.
        //
        // Asked of a patch, the keeping arm is true of every registered superseded
        // patch on every machine, superseded being precisely the state of a patch
        // Windows holds a record of. That would withhold the whole class the
        // offer's other half is made of, for ever, with a green build.
        var identities = new ScriptedPackageIdentities();      // nothing scripted: any read throws
        var msi = new ScriptedMsiProducts();                   // nothing scripted: any ask throws

        var outcomes = new DeclaredProductCheck(msi, identities)
            .Screen(new[] { Patch(@"C:\Windows\Installer\p.msp") });

        Assert.Equal(DeclaredProductOutcome.NotAProductPackage, outcomes[0]);
        Assert.False(outcomes[0].Withholds());
        Assert.Empty(identities.Reads);
        Assert.Empty(msi.Asked);
    }

    [Fact]
    public void A_patch_beside_a_package_leaves_the_package_screened_and_the_patch_alone()
    {
        // The pair in one fixture, because the test above passes equally well
        // against a screen that has stopped working altogether. Same list, same
        // scan, opposite outcomes.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\a.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var outcomes = new DeclaredProductCheck(msi, identities).Screen(new[]
        {
            Patch(@"C:\Windows\Installer\p.msp"),
            Package(@"C:\Windows\Installer\a.msi"),
        });

        Assert.Equal(DeclaredProductOutcome.NotAProductPackage, outcomes[0]);
        Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, outcomes[1]);
    }

    // ---- The pass's own contract ----

    [Fact]
    public void The_verdicts_line_up_with_the_candidates_they_answer()
    {
        // Positional, so a caller reads verdict i as candidate i's. Three
        // candidates with three different answers, deliberately not in the order
        // the enum declares them, so a check that returned a fixed sequence or
        // sorted its output would fail here.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\gone.msi", ProductA);
        identities.YieldsNothing(@"C:\Windows\Installer\unreadable.msi");
        identities.Declares(@"C:\Windows\Installer\held.msi", ProductB);

        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, MsiError.UnknownProduct);
        msi.Installed(ProductB);

        var outcomes = new DeclaredProductCheck(msi, identities).Screen(new[]
        {
            Package(@"C:\Windows\Installer\gone.msi"),
            Package(@"C:\Windows\Installer\unreadable.msi"),
            Package(@"C:\Windows\Installer\held.msi"),
        });

        Assert.Equal(new[]
        {
            DeclaredProductOutcome.DeclaredProductNotInstalled,
            DeclaredProductOutcome.Unestablished,
            DeclaredProductOutcome.DeclaredProductInstalled,
        }, outcomes);
    }

    [Fact]
    public void One_product_code_is_put_to_Windows_once_however_many_files_declare_it()
    {
        // A folder holding six cached packages of one program declares one product
        // code six times. The cache is what keeps the pass proportional to the
        // number of PRODUCTS rather than to the number of files, and it must not
        // change any verdict: all three files here get the same answer.
        var identities = new ScriptedPackageIdentities();
        identities.Declares(@"C:\Windows\Installer\v1.msi", ProductA);
        identities.Declares(@"C:\Windows\Installer\v2.msi", ProductA);
        identities.Declares(@"C:\Windows\Installer\v3.msi", ProductA);

        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var outcomes = new DeclaredProductCheck(msi, identities).Screen(new[]
        {
            Package(@"C:\Windows\Installer\v1.msi"),
            Package(@"C:\Windows\Installer\v2.msi"),
            Package(@"C:\Windows\Installer\v3.msi"),
        });

        Assert.All(outcomes, o => Assert.Equal(DeclaredProductOutcome.DeclaredProductInstalled, o));
        Assert.Equal(new[] { ProductA }, msi.Asked);
        // Every file is still opened: two packages declaring one code is the
        // ordinary case, and the only way to know a file declares that code is to
        // read it.
        Assert.Equal(3, identities.Reads.Count);
    }

    [Fact]
    public void A_cancelled_scan_stops_the_pass()
    {
        // The pass opens a database per candidate, so on a folder of any size it
        // is the part of a scan a user is most likely to cancel during.
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new DeclaredProductCheck(new ScriptedMsiProducts(), new ScriptedPackageIdentities())
                .Screen(new[] { Package(@"C:\Windows\Installer\a.msi") }, cts.Token));
    }

    // ---- What the outcomes mean, pinned over the whole enum ----

    [Fact]
    public void Exactly_two_outcomes_let_a_file_through_and_an_unset_verdict_does_not()
    {
        // The rule is written as "anything but these two" so that a member added
        // later withholds rather than silently not withholding. This pins the
        // permitting set by name, so adding one that permits has to be a
        // deliberate edit here as well as there.
        var permitting = Enum.GetValues<DeclaredProductOutcome>()
            .Where(o => !o.Withholds())
            .ToArray();

        Assert.Equal(
            new[]
            {
                DeclaredProductOutcome.NotAProductPackage,
                DeclaredProductOutcome.DeclaredProductNotInstalled,
            },
            permitting);

        // And the value a verdict nobody set carries. An array of these is
        // allocated before anything fills it, so the zero has to keep the file.
        Assert.True(default(DeclaredProductOutcome).Withholds());
    }
}

/// <summary>
/// A scripted <see cref="IPackageIdentityReader"/>. Shared with
/// <see cref="FileSystemScanServiceDeclaredProductTests"/>, which drives the real
/// check through the real scan.
///
/// AN UNSCRIPTED PATH THROWS RATHER THAN YIELDING NOTHING. "Nothing to read" is
/// one of the two answers under test and it is the one that keeps a file, so a
/// fake handing it back by default would let a test assert a withholding that the
/// fixture, not the code, produced.
/// </summary>
internal sealed class ScriptedPackageIdentities : IPackageIdentityReader
{
    private readonly Dictionary<string, PackageIdentity?> _byPath = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every path this reader was asked about, in order.</summary>
    public List<string> Reads { get; } = new();

    public void Declares(string path, string productCode) =>
        _byPath[path] = new PackageIdentity(productCode, IsPatch: false, Array.Empty<string>());

    public void Yields(string path, PackageIdentity identity) => _byPath[path] = identity;

    /// <summary>The file would not give up an identity at all.</summary>
    public void YieldsNothing(string path) => _byPath[path] = null;

    public PackageIdentity? Read(string filePath, bool isPatch, out string detail)
    {
        Reads.Add(filePath);
        detail = string.Empty;
        if (!_byPath.TryGetValue(filePath, out var identity))
            throw new InvalidOperationException(
                $"the fake reader was asked to read {filePath}, which no test scripted");
        return identity;
    }
}

/// <summary>
/// A scripted <see cref="IMsiApi"/> answering the one keyed product question this
/// area asks. The other three members are not reachable from here and say so.
///
/// AN UNSCRIPTED CODE THROWS, for the reason the reader's does: "Windows does not
/// hold that product" is the single answer that lets a file through, so a fake
/// giving it by default would let a test assert an offer nothing established.
/// </summary>
internal sealed class ScriptedMsiProducts : IMsiApi
{
    private readonly Dictionary<string, uint> _answers = new(StringComparer.Ordinal);

    /// <summary>Every product code this API was asked about, in order, once each.</summary>
    public List<string> Asked { get; } = new();

    public void Installed(string productCode) => _answers[productCode] = MsiError.Success;

    /// <param name="absence">
    /// Which of the returns that mean absence to give. Named by the caller rather
    /// than picked here, because which returns are allowed to carry that meaning
    /// is the thing under test.
    /// </param>
    public void NotInstalled(string productCode, uint absence) => _answers[productCode] = absence;

    public void Answers(string productCode, uint error) => _answers[productCode] = error;

    public uint EnumProducts(string? productCode, string? userSid, MsiInstallContext context, uint index,
        char[]? installedProductCode, out MsiInstallContext installedContext, char[]? sid, ref uint sidLength)
    {
        installedContext = MsiInstallContext.Machine;

        if (productCode is null)
            throw new InvalidOperationException(
                "the fake was asked to walk every product; this area only asks keyed questions");

        Asked.Add(productCode);

        if (!_answers.TryGetValue(productCode, out var result))
            throw new InvalidOperationException(
                $"the fake was asked about {productCode}, which no test scripted");

        if (result != MsiError.Success) return result;

        // A machine-context product, so the caller reads no SID back. Written into
        // the buffer anyway, because the real API does and a fake that leaves it
        // empty on success is a fake with a shape the code has never met.
        if (installedProductCode is not null)
            for (var i = 0; i < productCode.Length && i < installedProductCode.Length - 1; i++)
                installedProductCode[i] = productCode[i];

        return MsiError.Success;
    }

    public uint EnumPatches(string? productCode, string? userSid, MsiInstallContext context, MsiPatchFilter filter,
        uint index, char[]? patchCode, char[]? targetProductCode, out MsiInstallContext targetProductContext,
        char[]? targetUserSid, ref uint targetUserSidLength) =>
        throw new InvalidOperationException("the declared-product check enumerates no patches");

    public uint GetProductInfo(string productCode, string? userSid, MsiInstallContext context, string property,
        char[]? value, ref uint valueLength) =>
        throw new InvalidOperationException("the declared-product check reads no product properties");

    public uint GetPatchInfo(string patchCode, string productCode, string? userSid, MsiInstallContext context,
        string property, char[]? value, ref uint valueLength) =>
        throw new InvalidOperationException("the declared-product check reads no patch properties");
}
