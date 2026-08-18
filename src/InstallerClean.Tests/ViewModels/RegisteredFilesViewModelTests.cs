using NSubstitute;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class RegisteredFilesViewModelTests
{
    private static RegisteredPackage Pkg(string path, string name, string code) =>
        new(path, name, code);

    private static IMsiFileInfoService NullInfoService()
    {
        var mock = Substitute.For<IMsiFileInfoService>();
        mock.GetSummaryInfo(Arg.Any<string>()).Returns((MsiSummaryInfo?)null);
        return mock;
    }

    [Fact]
    public void Groups_products_by_ProductCode()
    {
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\bbb.msi", "Product B", "{BBB}"),
        };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService());

        Assert.Equal(2, vm.Products.Count);
        Assert.All(vm.Products, p => Assert.Equal(0, p.PatchCount));
    }

    [Fact]
    public void Counts_patches_per_product()
    {
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\patch1.msp", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\patch2.msp", "Product A", "{AAA}"),
        };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService());

        var product = Assert.Single(vm.Products);
        Assert.Equal(2, product.PatchCount);
        Assert.Equal(2, product.Patches.Count);
    }

    [Fact]
    public void Handles_product_with_only_patches_and_no_msi()
    {
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\patch1.msp", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\patch2.msp", "Product A", "{AAA}"),
        };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService());

        Assert.Single(vm.Products);
    }

    [Fact]
    public void Empty_product_name_becomes_unknown()
    {
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "", "{AAA}"),
        };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService());

        Assert.Equal(InstallerClean.Resources.Strings.Field_UnknownProductName, vm.Products[0].ProductName);
    }

    [Fact]
    public void Summary_shows_total_count_and_size()
    {
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\bbb.msi", "Product B", "{BBB}"),
        };

        var vm = new RegisteredFilesViewModel(packages, 1_048_576, NullInfoService());

        // "Left alone" and nothing about the files themselves: the window shows two
        // populations and only one of them is registered, so a count called
        // "registered files" would be false of the other half the moment it has one.
        Assert.Equal("2 files left alone (1.0 MB)", vm.Summary);
    }

    [Fact]
    public void Opens_on_the_first_product_missing_from_disk()
    {
        // The main window's missing-from-disk banner tells the user to open
        // Details for what to do, and the recovery note lives on the missing
        // row's details pane. Sorted by product name, the missing one is third.
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\bbb.msi", "Product B", "{BBB}"),
            new(@"C:\Windows\Installer\ccc.msi", "Product C", "{CCC}", FileExists: false),
            new(@"C:\Windows\Installer\ddd.msi", "Product D", "{DDD}", FileExists: false),
        };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService());

        Assert.Equal("Product C", vm.SelectedProduct?.ProductName);
        Assert.True(vm.SelectedProduct?.IsMissing);
        Assert.True(vm.ShowMissing);
    }

    [Fact]
    public void Opens_on_the_first_product_when_none_is_missing()
    {
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\bbb.msi", "Product B", "{BBB}"),
        };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService());

        Assert.Equal("Product A", vm.SelectedProduct?.ProductName);
        Assert.False(vm.ShowMissing);
    }
}
