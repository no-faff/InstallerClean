using System.Runtime.InteropServices;
using InstallerClean.Interop;
using InstallerClean.Interop.Native;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The real reader against small databases built here with msi.dll: what it answers for an
/// installation package whose Property table gives no usable ProductCode, the package code it
/// reads from an installation package's summary stream, what it answers for a patch whose
/// summary stream gives no usable code or target, and for a file it cannot read. Each database
/// is built by the calls below, every one of which must succeed, so a fixture that did not
/// build fails the test rather than reading as a file that would not open.
/// </summary>
public sealed class PackageIdentityReaderDatabaseTests : IDisposable
{
    private const string WellFormedCode = "{12345678-1234-1234-1234-123456789ABC}";
    private const string WellFormedPatchCode = "{87654321-4321-4321-4321-CBA987654321}";

    /// <summary>The real reader, asked through the interface, as the declared-product check asks it.</summary>
    private static readonly IPackageIdentityReader Reader = new PackageIdentityReader();

    private readonly string _folder = TestTemp.NewFolder("ic-package-identity-");


    public void Dispose() => TestTemp.Delete(_folder);

    [Fact]
    public void A_package_declaring_a_well_formed_ProductCode_reads_as_that_code()
    {
        // The building APackage gives the packages below, with a ProductCode the reader accepts.
        var path = APackage(("ProductCode", WellFormedCode), ("ProductName", "Something"));

        var identity = Reader.Read(path, isPatch: false, out var detail, out _);

        Assert.NotNull(identity);
        Assert.Equal(WellFormedCode, identity.Value.Code);
        Assert.False(identity.Value.IsPatch);
        Assert.Equal(string.Empty, detail);
    }

    [Fact]
    public void A_package_with_no_ProductCode_row_declares_no_code()
    {
        var path = APackage(("ProductName", "Something"));

        var identity = Reader.Read(path, isPatch: false, out var detail, out var refusal);

        Assert.Null(identity);
        Assert.Equal(PackageReadRefusal.DeclaresNoCode, refusal);
        Assert.Equal("package declares no ProductCode", detail);
    }

    [Fact]
    public void A_package_whose_ProductCode_is_not_a_GUID_declares_no_code()
    {
        var path = APackage(("ProductCode", "not-a-guid"), ("ProductName", "Something"));

        var identity = Reader.Read(path, isPatch: false, out var detail, out var refusal);

        Assert.Null(identity);
        Assert.Equal(PackageReadRefusal.DeclaresNoCode, refusal);
        Assert.Equal("ProductCode is not a well-formed GUID", detail);
    }

    [Fact]
    public void A_package_whose_ProductCode_is_empty_declares_no_code()
    {
        // The Value column here takes a null, which is how Windows Installer holds an empty
        // string, so the ProductCode row is there and states nothing.
        var path = ADatabase(
            "CREATE TABLE `Property` (`Property` CHAR(72) NOT NULL, `Value` LONGCHAR LOCALIZABLE PRIMARY KEY `Property`)",
            "INSERT INTO `Property` (`Property`) VALUES ('ProductCode')");

        var identity = Reader.Read(path, isPatch: false, out var detail, out var refusal);

        Assert.Null(identity);
        Assert.Equal(PackageReadRefusal.DeclaresNoCode, refusal);
        Assert.Equal("ProductCode is empty", detail);
    }

    [Fact]
    public void A_database_with_no_Property_table_would_not_read()
    {
        // The query naming the table does not open, which the reader cannot tell from a query
        // this build of msi.dll would not prepare, so it claims nothing about what the file
        // declares.
        var path = ADatabase(
            "CREATE TABLE `Other` (`Key` CHAR(72) NOT NULL PRIMARY KEY `Key`)",
            "INSERT INTO `Other` (`Key`) VALUES ('Something')");

        var identity = Reader.Read(path, isPatch: false, out var detail, out var refusal);

        Assert.Null(identity);
        Assert.Equal(PackageReadRefusal.WouldNotRead, refusal);
        Assert.StartsWith("Property table query would not open", detail);
    }

    [Fact]
    public void A_file_that_is_not_a_database_would_not_read()
    {
        var path = Path.Combine(_folder, "text.msi");
        File.WriteAllText(path, "not a database");

        var identity = Reader.Read(path, isPatch: false, out var detail, out var refusal);

        Assert.Null(identity);
        Assert.Equal(PackageReadRefusal.WouldNotRead, refusal);
        Assert.StartsWith("package would not open as a database", detail);
    }

    [Theory]
    [InlineData(WellFormedCode)]
    [InlineData("not-a-guid")]
    public void The_plain_read_answers_as_the_read_that_says_why(string productCode)
    {
        var path = APackage(("ProductCode", productCode));

        var plain = Reader.Read(path, isPatch: false, out var plainDetail);
        var withRefusal = Reader.Read(path, isPatch: false, out var detail, out _);

        Assert.Equal(withRefusal, plain);
        Assert.Equal(detail, plainDetail);
    }

    [Fact]
    public void A_patch_declaring_its_code_and_a_target_reads_as_both()
    {
        // The building APatch gives the patches below, with both values set and well formed.
        var path = APatch(revisionNumber: WellFormedPatchCode, template: WellFormedCode);

        var identity = Reader.Read(path, isPatch: true, out var detail, out _);

        Assert.NotNull(identity);
        Assert.Equal(WellFormedPatchCode, identity.Value.Code);
        Assert.True(identity.Value.IsPatch);
        Assert.Equal(new[] { WellFormedCode }, identity.Value.TargetProductCodes);
        Assert.Equal(string.Empty, detail);
    }

    [Fact]
    public void A_patch_with_no_revision_number_declares_no_code()
    {
        var path = APatch(revisionNumber: null, template: WellFormedCode);

        var identity = Reader.Read(path, isPatch: true, out var detail, out var refusal);

        Assert.Null(identity);
        Assert.Equal(PackageReadRefusal.DeclaresNoCode, refusal);
        Assert.Equal("patch declares no revision number", detail);
    }

    [Fact]
    public void A_patch_with_no_template_declares_no_code()
    {
        var path = APatch(revisionNumber: WellFormedPatchCode, template: null);

        var identity = Reader.Read(path, isPatch: true, out var detail, out var refusal);

        Assert.Null(identity);
        Assert.Equal(PackageReadRefusal.DeclaresNoCode, refusal);
        Assert.Equal("patch declares no target products", detail);
    }

    [Theory]
    [InlineData("")]
    [InlineData(";")]
    public void A_patch_whose_template_names_no_product_declares_no_code(string template)
    {
        var path = APatch(revisionNumber: WellFormedPatchCode, template: template);

        var identity = Reader.Read(path, isPatch: true, out var detail, out var refusal);

        Assert.Null(identity);
        Assert.Equal(PackageReadRefusal.DeclaresNoCode, refusal);
        Assert.Equal("patch names no target product", detail);
    }

    [Fact]
    public void A_package_s_package_code_is_its_revision_number_in_upper_case()
    {
        var path = APackageRevised(
            "{abcdef01-2345-6789-abcd-ef0123456789}", ("ProductCode", WellFormedCode));

        Assert.Equal("{ABCDEF01-2345-6789-ABCD-EF0123456789}", Reader.PackageCodeOf(path));
        // The product code reads as it does without the package code.
        Assert.Equal(WellFormedCode, Reader.Read(path, isPatch: false, out _, out _)?.Code);
    }

    [Fact]
    public void A_package_with_no_revision_number_has_no_package_code()
    {
        var path = APackageRevised(null, ("ProductCode", WellFormedCode));

        Assert.Null(Reader.PackageCodeOf(path));
    }

    [Fact]
    public void A_file_that_will_not_open_has_no_package_code()
    {
        var path = Path.Combine(_folder, "not-a-package.msi");
        File.WriteAllText(path, "not a database");

        Assert.Null(Reader.PackageCodeOf(path));
        Assert.Null(Reader.PackageCodeOf(Path.Combine(_folder, "absent.msi")));
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("{ABCDEF01-2345-6789-ABCD-EF0123456789}{ABCDEF01-2345-6789-ABCD-EF0123456789}")]
    [InlineData(" {ABCDEF01-2345-6789-ABCD-EF0123456789}")]
    public void A_package_whose_revision_number_is_not_one_braced_GUID_has_no_package_code(string revision)
    {
        var path = APackageRevised(revision, ("ProductCode", WellFormedCode));

        Assert.Null(Reader.PackageCodeOf(path));
    }

    [Fact]
    public void A_package_with_no_summary_information_has_no_revision_number()
    {
        var path = APackage(("ProductCode", WellFormedCode));

        Assert.True(Reader.HoldsNoRevisionNumber(path));
        Assert.Null(Reader.PackageCodeOf(path));
    }

    [Fact]
    public void A_package_whose_summary_holds_other_properties_and_whose_revision_number_was_taken_out_has_no_revision_number()
    {
        var path = ADatabase(PropertyTable([("ProductCode", WellFormedCode)]), summary =>
        {
            SetText(summary, MsiSummaryProperty.Subject, "Something");
            SetText(summary, MsiSummaryProperty.Template, "Intel;1033");
            SetText(summary, MsiSummaryProperty.RevisionNumber, WellFormedCode);
        });
        Assert.False(Reader.HoldsNoRevisionNumber(path));
        TakeOutTheRevisionNumber(path);

        Assert.True(Reader.HoldsNoRevisionNumber(path));
        Assert.Null(Reader.PackageCodeOf(path));
    }

    [Fact]
    public void A_package_whose_summary_holds_other_properties_and_never_had_a_revision_number_has_no_revision_number()
    {
        var path = ADatabase(PropertyTable([("ProductCode", WellFormedCode)]), summary =>
        {
            SetText(summary, MsiSummaryProperty.Subject, "Something");
            SetText(summary, MsiSummaryProperty.Template, "Intel;1033");
        });

        Assert.True(Reader.HoldsNoRevisionNumber(path));
        Assert.Null(Reader.PackageCodeOf(path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("{ABCDEF01-2345-6789-ABCD-EF0123456789}{ABCDEF01-2345-6789-ABCD-EF0123456789}")]
    [InlineData("{ABCDEF01-2345-6789-ABCD-EF0123456789}")]
    public void A_package_whose_revision_number_is_set_is_not_one_with_no_revision_number(string revision)
    {
        // Empty text and text that is not one braced GUID give no package code, and are still a
        // Revision Number that is set.
        var path = APackageRevised(revision, ("ProductCode", WellFormedCode));

        Assert.False(Reader.HoldsNoRevisionNumber(path));
    }

    [Fact]
    public void A_file_that_will_not_open_is_not_one_with_no_revision_number()
    {
        var path = Path.Combine(_folder, "not-a-package.msi");
        File.WriteAllText(path, "not a database");

        Assert.False(Reader.HoldsNoRevisionNumber(path));
        Assert.False(Reader.HoldsNoRevisionNumber(Path.Combine(_folder, "absent.msi")));
    }

    /// <summary>
    /// Takes the Revision Number out of the summary stream of the package at the path, leaving
    /// its other properties, by setting it to VT_EMPTY and saving the stream.
    /// </summary>
    private static void TakeOutTheRevisionNumber(string path)
    {
        Assert.Equal(MsiError.Success, Msi.MsiGetSummaryInformation(0, path, SummaryUpdateCount, out var summary));
        try
        {
            Assert.Equal(MsiError.Success,
                MsiSummaryInfoSetPropertyW(summary, MsiSummaryProperty.RevisionNumber, VtType.Empty, 0, IntPtr.Zero, null));
            Assert.Equal(MsiError.Success, MsiSummaryInfoPersist(summary));
        }
        finally
        {
            Msi.MsiCloseHandle(summary);
        }
    }

    /// <summary>A database whose Property table holds the rows given.</summary>
    private string APackage(params (string Property, string Value)[] rows) =>
        ADatabase(PropertyTable(rows));

    /// <summary>
    /// A database whose Property table holds the rows given and whose summary stream holds the
    /// revision number given as text, or leaves it unset where it is null.
    /// </summary>
    private string APackageRevised(string? revisionNumber, params (string Property, string Value)[] rows) =>
        ADatabase(PropertyTable(rows), summary =>
        {
            if (revisionNumber is not null) SetText(summary, MsiSummaryProperty.RevisionNumber, revisionNumber);
        });

    private static string[] PropertyTable((string Property, string Value)[] rows) =>
    [
        "CREATE TABLE `Property` (`Property` CHAR(72) NOT NULL, `Value` LONGCHAR NOT NULL LOCALIZABLE PRIMARY KEY `Property`)",
        .. rows.Select(row => $"INSERT INTO `Property` (`Property`, `Value`) VALUES ('{row.Property}', '{row.Value}')"),
    ];

    /// <summary>
    /// A database whose summary stream holds the revision number and Template given as text,
    /// and leaves unset each one given as null. The reader takes the patch reading of it by
    /// being asked to.
    /// </summary>
    private string APatch(string? revisionNumber, string? template) =>
        ADatabase([], summary =>
        {
            if (revisionNumber is not null) SetText(summary, MsiSummaryProperty.RevisionNumber, revisionNumber);
            if (template is not null) SetText(summary, MsiSummaryProperty.Template, template);
        });

    private static void SetText(uint summary, uint property, string value) =>
        Assert.Equal(MsiError.Success, MsiSummaryInfoSetPropertyW(summary, property, VtType.String, 0, IntPtr.Zero, value));

    /// <summary>A new database in the test's folder, built by running each query and committed.</summary>
    private string ADatabase(params string[] queries) => ADatabase(queries, writeSummary: null);

    /// <summary>
    /// A new database in the test's folder, built by running each query, then writing its
    /// summary stream where <paramref name="writeSummary"/> is given, and committed.
    /// </summary>
    private string ADatabase(string[] queries, Action<uint>? writeSummary)
    {
        var path = Path.Combine(_folder, Guid.NewGuid() + ".msi");
        Assert.Equal(MsiError.Success, Msi.MsiOpenDatabase(path, CreateMode, out var database));
        try
        {
            foreach (var query in queries)
            {
                Assert.Equal(MsiError.Success, Msi.MsiDatabaseOpenView(database, query, out var view));
                try
                {
                    Assert.Equal(MsiError.Success, Msi.MsiViewExecute(view, 0));
                }
                finally
                {
                    Msi.MsiCloseHandle(view);
                }
            }

            if (writeSummary is not null)
            {
                Assert.Equal(MsiError.Success,
                    Msi.MsiGetSummaryInformation(database, null, SummaryUpdateCount, out var summary));
                try
                {
                    writeSummary(summary);
                    Assert.Equal(MsiError.Success, MsiSummaryInfoPersist(summary));
                }
                finally
                {
                    Msi.MsiCloseHandle(summary);
                }
            }

            Assert.Equal(MsiError.Success, MsiDatabaseCommit(database));
        }
        finally
        {
            Msi.MsiCloseHandle(database);
        }

        return path;
    }

    /// <summary>MSIDBOPEN_CREATE in msiquery.h, passed as the pointer value it is declared as.</summary>
    private static readonly IntPtr CreateMode = (IntPtr)3;

    /// <summary>How many properties the summary stream may be given; more than any test writes.</summary>
    private const uint SummaryUpdateCount = 20;

    [DllImport("msi.dll")]
    private static extern uint MsiDatabaseCommit(uint hDatabase);

    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    private static extern uint MsiSummaryInfoSetPropertyW(
        uint hSummaryInfo, uint uiProperty, uint uiDataType, int iValue, IntPtr pftValue, string? szValue);

    [DllImport("msi.dll")]
    private static extern uint MsiSummaryInfoPersist(uint hSummaryInfo);
}
