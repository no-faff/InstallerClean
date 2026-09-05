using InstallerClean.Models;
using Xunit;

namespace InstallerClean.Tests.Models;

/// <summary>
/// The patch-set verdict a row carries when nothing has set one.
///
/// THE ANSWER A ROW TAKES BY DEFAULT IS WRITTEN OUT ON THE PARAMETER AND CANNOT BE
/// LEFT TO THE TYPE. <see cref="ProductPatchSet"/>'s own zero is
/// <see cref="ProductPatchSet.AllNonRemovable"/>, the one value that permits, so a
/// row constructed without a verdict would otherwise arrive carrying a clean answer
/// nobody established. The parameter default is what makes an unjudged row withhold,
/// and this holds it to that.
/// </summary>
public class RegisteredPackageTests
{
    [Fact]
    public void A_row_nobody_judged_carries_the_verdict_that_withholds()
    {
        var row = new RegisteredPackage(
            @"C:\Windows\Installer\package.msi",
            "A product",
            "{0BEEF000-0000-0000-0000-000000000000}");

        Assert.Equal(ProductPatchSet.Unestablished, row.ProductPatchSetVerdict);
    }
}
