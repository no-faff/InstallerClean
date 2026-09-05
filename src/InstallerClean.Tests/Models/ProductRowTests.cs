using InstallerClean.Models;
using InstallerClean.Resources;
using Xunit;

namespace InstallerClean.Tests.Models;

/// <summary>
/// The two properties that read a row's stored product name:
/// <see cref="ProductRow.HasNoNamedProduct"/>, which the window applies ahead of the
/// product name and always ascending, so a true row sits at the foot of the list
/// whichever way the header points; and <see cref="ProductRow.ProductNameDisplay"/>,
/// which is what the product cell paints and what the spoken line opens with.
///
/// TWO DIFFERENT FACTS REACH THEM AND EACH IS PINNED ON ITS OWN. An empty name is a
/// file no registration names at all; <c>Field.UnknownProductName</c> is a registered
/// product whose display name did not come back. They meet in the ordering and part
/// again in the cell, where each says its own thing rather than one standing in for
/// both.
///
/// AND THE ORDERING IS PINNED AGAINST THE DISPLAY, which is the pairing that matters:
/// the cell shows words for a row whose stored name is empty, and the row still has to
/// rank as having no named product. Reading the two on one row is what would catch a
/// placeholder that had been stored instead of composed.
///
/// THE PLACEHOLDER IS MATCHED WHOLE, which is what separates this from a substring
/// test: a product genuinely named something that contains the placeholder keeps its
/// place among the named rows.
/// </summary>
public class ProductRowTests
{
    private static ProductRow Row(string productName) =>
        new(productName,
            "package.msi",
            @"C:\Windows\Installer\package.msi",
            "1.0 MB",
            1_048_576,
            0,
            Array.Empty<PatchRow>());

    [Fact]
    public void A_file_no_registration_names_has_no_named_product()
    {
        Assert.True(Row(string.Empty).HasNoNamedProduct);
    }

    [Fact]
    public void A_registration_whose_display_name_did_not_come_back_has_no_named_product()
    {
        Assert.True(Row(Strings.Field_UnknownProductName).HasNoNamedProduct);
    }

    [Fact]
    public void A_named_product_has_a_named_product()
    {
        Assert.False(Row("Adobe Acrobat Reader").HasNoNamedProduct);
    }

    [Fact]
    public void A_name_that_merely_contains_the_placeholder_is_a_named_product()
    {
        Assert.False(Row($"Driver {Strings.Field_UnknownProductName} Toolkit").HasNoNamedProduct);
    }

    [Fact]
    public void A_file_no_registration_names_shows_that_there_is_no_program_to_name()
    {
        var row = Row(string.Empty);

        Assert.Equal(Strings.Field_NoNamedProduct, row.ProductNameDisplay);

        // The stored name is untouched, and that is the half worth asserting: the
        // ordering reads the stored name, so a placeholder written into the record
        // rather than composed here would leave this row ranking as a named product
        // and send it up among them.
        Assert.Equal(string.Empty, row.ProductName);
        Assert.True(row.HasNoNamedProduct);
    }

    [Fact]
    public void A_registration_whose_display_name_did_not_come_back_keeps_its_own_words()
    {
        var row = Row(Strings.Field_UnknownProductName);

        Assert.Equal(Strings.Field_UnknownProductName, row.ProductNameDisplay);
        Assert.NotEqual(Strings.Field_NoNamedProduct, row.ProductNameDisplay);
    }

    [Fact]
    public void A_named_product_is_shown_under_its_own_name()
    {
        Assert.Equal("Adobe Acrobat Reader", Row("Adobe Acrobat Reader").ProductNameDisplay);
    }

    [Fact]
    public void The_spoken_line_opens_with_what_the_product_cell_shows()
    {
        // Both rows, because the point is that the spoken line carries the cell
        // whatever the cell says. A blank opening is what this pins against: the
        // listener is told the same thing the reader can see.
        Assert.StartsWith(
            $"{Strings.Field_NoNamedProduct}, package.msi",
            Row(string.Empty).AccessibleName);
        Assert.StartsWith(
            "Adobe Acrobat Reader, package.msi",
            Row("Adobe Acrobat Reader").AccessibleName);
    }
}
