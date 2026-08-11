using InstallerClean.Helpers;
using InstallerClean.Models;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The renderer both hosts use for the missing-files line. It lives in Core so
/// the window and the command line cannot drift on what they say about one
/// machine state, and these pin what it says rather than how either host lays it
/// out.
///
/// THE CONSTRAINT THAT IS NOT A NICETY: the line names no cause. Every tool that
/// has ever deleted from that folder leaves an identical record, this one
/// included up to v2.3.0, so nothing here can say what removed a file. The line
/// fires on machines the app has never run on.
/// </summary>
public class MissingFilesReportTests
{
    private static RegisteredPackage Missing(string path, string productName) =>
        new(path, productName, "{00000000-0000-0000-0000-000000000001}", FileExists: false);

    private static RegisteredPackage Present(string path, string productName) =>
        new(path, productName, "{00000000-0000-0000-0000-000000000001}", FileExists: true);

    [Fact]
    public void Only_the_registrations_whose_file_is_gone_are_reported()
    {
        var products = MissingFilesReport.Products(new[]
        {
            Present(@"C:\Windows\Installer\here.msi", "Contoso Reader"),
            Missing(@"C:\Windows\Installer\gone.msi", "Fabrikam Suite"),
        });

        var product = Assert.Single(products);
        Assert.Equal("Fabrikam Suite", product.ProductName);
        Assert.Equal(1, product.FileCount);
    }

    [Fact]
    public void A_superseded_registration_is_reported_like_any_other()
    {
        // The 3.0.0 correction in one assertion. A patch Windows has marked
        // superseded whose file has gone is the same condition as any other
        // missing registration: Windows opens every registered patch's cached file
        // whether superseded or not, and a missing one gives error 1635.
        var products = MissingFilesReport.Products(new[]
        {
            Missing(@"C:\Windows\Installer\superseded.msp", "Contoso Reader") with { PatchState = 2 },
        });

        Assert.Equal("Contoso Reader", Assert.Single(products).ProductName);
    }

    [Fact]
    public void Files_are_grouped_by_program_and_ordered_by_how_many_each_has_lost()
    {
        var products = MissingFilesReport.Products(new[]
        {
            Missing(@"C:\Windows\Installer\a.msi", "Fabrikam Suite"),
            Missing(@"C:\Windows\Installer\b.msp", "Contoso Reader"),
            Missing(@"C:\Windows\Installer\c.msp", "Contoso Reader"),
        });

        Assert.Equal(new[] { "Contoso Reader", "Fabrikam Suite" },
            products.Select(p => p.ProductName).ToArray());
        Assert.Equal(new[] { 2, 1 }, products.Select(p => p.FileCount).ToArray());
    }

    [Fact]
    public void Two_programs_that_have_lost_the_same_number_order_alphabetically()
    {
        // So two scans of one machine agree. Without a tiebreaker the order falls
        // out of enumeration order, which is not stable and would make the line
        // change between runs on a machine that has not.
        var products = MissingFilesReport.Products(new[]
        {
            Missing(@"C:\Windows\Installer\a.msi", "Zeta Tools"),
            Missing(@"C:\Windows\Installer\b.msi", "Alpha Tools"),
        });

        Assert.Equal(new[] { "Alpha Tools", "Zeta Tools" },
            products.Select(p => p.ProductName).ToArray());
    }

    [Fact]
    public void Registrations_nothing_named_are_counted_as_one_group_and_placed_last()
    {
        // NOT AN EDGE CASE. The registry fallback claims a path without a product
        // name, so a registration only it reached has none to give, and a residue
        // key whose product has gone is exactly the shape whose file tends to be
        // absent. Counting each as its own program would invent a headcount of
        // programs the app cannot name.
        var products = MissingFilesReport.Products(new[]
        {
            Missing(@"C:\Windows\Installer\a.msi", ""),
            Missing(@"C:\Windows\Installer\b.msi", ""),
            Missing(@"C:\Windows\Installer\c.msi", "Contoso Reader"),
        });

        Assert.Equal(2, products.Count);
        Assert.Equal("Contoso Reader", products[0].ProductName);
        Assert.Equal(string.Empty, products[1].ProductName);
        Assert.Equal(2, products[1].FileCount);
    }

    [Fact]
    public void An_empty_set_renders_nothing_at_all()
    {
        // The caller's cue to say nothing rather than to print an empty clause.
        Assert.Empty(MissingFilesReport.Inline(MissingFilesReport.Products(Array.Empty<RegisteredPackage>())));
    }

    [Fact]
    public void The_line_names_the_programs_it_can()
    {
        var line = MissingFilesReport.Inline(MissingFilesReport.Products(new[]
        {
            Missing(@"C:\Windows\Installer\a.msi", "Contoso Reader"),
            Missing(@"C:\Windows\Installer\b.msi", "Fabrikam Suite"),
        }));

        Assert.Contains("Contoso Reader", line);
        Assert.Contains("Fabrikam Suite", line);
    }

    [Fact]
    public void Past_the_cap_the_rest_are_counted_as_other_programs()
    {
        var line = MissingFilesReport.Inline(MissingFilesReport.Products(new[]
        {
            Missing(@"C:\Windows\Installer\a.msi", "Aaa"),
            Missing(@"C:\Windows\Installer\b.msi", "Bbb"),
            Missing(@"C:\Windows\Installer\c.msi", "Ccc"),
            Missing(@"C:\Windows\Installer\d.msi", "Ddd"),
            Missing(@"C:\Windows\Installer\e.msi", "Eee"),
        }));

        Assert.Contains("Aaa", line);
        Assert.Contains("2 other programs", line);
        Assert.DoesNotContain("Eee", line);
    }

    [Fact]
    public void The_two_tails_are_separate_sentences_and_are_not_merged()
    {
        // A program past the cap is one the app CAN name and had no room for; a
        // registration nothing named is one it cannot name at all. One tail
        // covering both would put both under a sentence false of half of them.
        var line = MissingFilesReport.Inline(MissingFilesReport.Products(new[]
        {
            Missing(@"C:\Windows\Installer\a.msi", "Aaa"),
            Missing(@"C:\Windows\Installer\b.msi", "Bbb"),
            Missing(@"C:\Windows\Installer\c.msi", "Ccc"),
            Missing(@"C:\Windows\Installer\d.msi", "Ddd"),
            Missing(@"C:\Windows\Installer\e.msi", ""),
        }));

        Assert.Contains("1 other program", line);
        Assert.Contains("1 file with no program named in the records", line);
    }

    [Fact]
    public void A_machine_whose_missing_registrations_name_nothing_still_says_so()
    {
        // The whole set unnamed, which a registry-fallback-only population gives.
        // The line must still carry something rather than rendering an empty
        // clause after its colon.
        var line = MissingFilesReport.Inline(MissingFilesReport.Products(new[]
        {
            Missing(@"C:\Windows\Installer\a.msi", ""),
            Missing(@"C:\Windows\Installer\b.msi", ""),
        }));

        Assert.Equal("2 files with no program named in the records", line);
    }

    [Fact]
    public void Nothing_it_renders_says_what_removed_the_files()
    {
        var line = MissingFilesReport.Inline(MissingFilesReport.Products(new[]
        {
            Missing(@"C:\Windows\Installer\a.msi", "Contoso Reader"),
            Missing(@"C:\Windows\Installer\b.msi", ""),
        }));

        Assert.DoesNotContain("InstallerClean", line);
        Assert.DoesNotContain("removed", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deleted", line, StringComparison.OrdinalIgnoreCase);
    }
}
