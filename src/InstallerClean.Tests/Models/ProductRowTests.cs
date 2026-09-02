using InstallerClean.Models;
using InstallerClean.Resources;
using Xunit;

namespace InstallerClean.Tests.Models;

/// <summary>
/// <see cref="ProductRow.HasNoNamedProduct"/>, which is the one place the registered
/// list treats a row with nothing in its product cell differently. The window applies
/// it ahead of the product name and always ascending, so a true row sits at the foot
/// of the list whichever way the header points.
///
/// TWO DIFFERENT FACTS REACH IT AND EACH IS PINNED ON ITS OWN. An empty name is a file
/// no registration names at all; <c>Field.UnknownProductName</c> is a registered
/// product whose display name did not come back. They meet in this property and
/// nowhere else, so each gets its own case rather than one standing in for both.
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
}
