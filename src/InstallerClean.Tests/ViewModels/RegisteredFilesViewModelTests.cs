using NSubstitute;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class RegisteredFilesViewModelTests
{
    private static RegisteredPackage Pkg(string path, string name, string code) =>
        new(path, name, code);

    // A file this scan declined to offer. The reason string is what the orphan
    // list's own Reason column would carry, and it is passed here only because the
    // record takes one: NOTHING in this window reads it, which is the point. If a
    // future change makes a withheld row carry its cause onto this screen, these
    // fixtures will not catch it and a reviewer reading the assertions will not
    // either, so the guard is the comment on the view model, not this line.
    private const string Kept = @"C:\Windows\Installer\1285c2.msi";

    private static OrphanedFile Withheld(string path, long size = 2_700_000) =>
        new(path, size, IsPatch: false, IsRemovablePatch: false, IsObsoleted: false, Reason: "orphaned");

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
    public void Summary_counts_files_and_not_rows()
    {
        // THE FIXTURE IS THE TEST HERE. Three packages collapse to ONE row and two
        // withheld files add two more, so rows are 3 and files are 5. A summary
        // built from Products.Count would say 3 and pass every other test in this
        // file, because every other fixture has one package per row.
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\patch1.msp", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\patch2.msp", "Product A", "{AAA}"),
        };
        var withheld = new List<OrphanedFile>
        {
            Withheld(Kept, 1_048_576),
            Withheld(@"C:\Windows\Installer\1285c7.msi", 1_048_576),
        };

        var vm = new RegisteredFilesViewModel(packages, 1_048_576, NullInfoService(), withheld);

        Assert.Equal(3, vm.Products.Count);
        Assert.Equal("5 files left alone (3.0 MB)", vm.Summary);
    }

    [Fact]
    public void Withheld_files_are_rows_in_the_one_list()
    {
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "Product A", "{AAA}"),
        };
        var withheld = new List<OrphanedFile> { Withheld(Kept) };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService(), withheld);

        Assert.Equal(2, vm.Products.Count);
        var row = Assert.Single(vm.Products, p => p.FullPath == Kept);

        // The path is what makes the detail pane work on this row: the pane reads
        // the FILE's own summary stream, not the registration, so a withheld row
        // needs nothing but its path to populate. Throwing the path away at
        // construction, as the second list did, is what stopped it.
        Assert.Equal(0, row.PatchCount);
        Assert.Empty(row.Patches);

        // Not missing: the flag drives a recovery note about a file WINDOWS HAS A
        // RECORD FOR, and these rows are exactly the ones it does not.
        Assert.False(row.IsMissing);
    }

    [Fact]
    public void A_withheld_row_has_no_product_name_and_no_placeholder()
    {
        // The two halves of this are one test on purpose. A withheld row's product
        // cell is EMPTY, and the "(unknown)" string stays reserved for a REGISTERED
        // product whose display name did not come back. Assert only the first half
        // and a change reusing "(unknown)" here still passes, because a placeholder
        // is not empty either way round; assert only the second and nothing pins
        // which row got it.
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "", "{AAA}"),
        };
        var withheld = new List<OrphanedFile> { Withheld(Kept) };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService(), withheld);

        var registered = Assert.Single(vm.Products, p => p.FullPath == @"C:\Windows\Installer\aaa.msi");
        var kept = Assert.Single(vm.Products, p => p.FullPath == Kept);

        Assert.Equal(InstallerClean.Resources.Strings.Field_UnknownProductName, registered.ProductName);
        Assert.Equal(string.Empty, kept.ProductName);
    }

    [Fact]
    public void A_withheld_rows_spoken_name_does_not_open_with_a_comma()
    {
        var withheld = new List<OrphanedFile> { Withheld(Kept, 1_048_576) };

        var vm = new RegisteredFilesViewModel(
            new List<RegisteredPackage>(), 0, NullInfoService(), withheld);

        var row = Assert.Single(vm.Products);

        // Composed from the cells that have something in them. The product cell is
        // blank on this row, and a plain join would put a stray comma in front of
        // the file name for a screen reader to pause on.
        //
        // The expectation opens with the row's OWN file-name cell rather than a
        // literal "1285c2.msi", and that is not the assertion going soft. What is
        // under test is the JOIN: three parts, comma-separated, nothing standing in
        // for the absent product. Spelling the base name out here would also pin
        // Path.GetFileName's answer, which is a different subject and one that
        // depends on the platform's path separator.
        //
        // THE OBVIOUS OBJECTION IS THAT THE SUBJECT APPEARS ON BOTH SIDES, SO THIS
        // COULD NOT FAIL. IT WAS CHECKED RATHER THAN ARGUED. Restoring the plain
        // string.Join to ProductRow.AccessibleName, which is the fault this test
        // exists for, fails THIS test and no other: the join then emits a leading
        // comma for the empty product name and the composition no longer matches.
        // The interpolated FileName pins the join's SHAPE while leaving the base
        // name to whichever platform is running it.
        Assert.Equal($"{row.FileName}, 1.0 MB, 0 patches", row.AccessibleName);
        Assert.DoesNotContain(", ,", row.AccessibleName);
        Assert.False(row.AccessibleName.StartsWith(","));
    }

    [Fact]
    public void Opens_on_a_missing_registration_and_never_on_a_withheld_row()
    {
        // A withheld row is never IsMissing, so it cannot take the opening selection
        // off the missing registration the main window's banner sent the reader here
        // to find. That is a property of the flag rather than of the order, and this
        // fixture would pass with no withheld row at all, so it checks the row is
        // really in the list before checking what the selection did.
        var packages = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\ccc.msi", "Product C", "{CCC}", FileExists: false),
        };
        var withheld = new List<OrphanedFile> { Withheld(Kept) };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService(), withheld);

        var kept = Assert.Single(vm.Products, p => p.FullPath == Kept);
        Assert.False(kept.IsMissing);

        Assert.Equal("Product C", vm.SelectedProduct?.ProductName);
        Assert.True(vm.ShowMissing);
    }

    [Fact]
    public void A_selected_withheld_row_shows_neither_the_missing_note_nor_a_patch_list()
    {
        var withheld = new List<OrphanedFile> { Withheld(Kept) };

        // The constructor selects the first row when none is missing, and here the
        // only row is a withheld one, so this is the state the window opens in on a
        // machine whose whole list is withheld files.
        var vm = new RegisteredFilesViewModel(
            new List<RegisteredPackage>(), 0, NullInfoService(), withheld);

        Assert.Same(vm.Products[0], vm.SelectedProduct);
        Assert.True(vm.HasSelection);
        Assert.False(vm.ShowMissing);
        Assert.False(vm.HasPatches);
        Assert.Empty(vm.SelectedPatches);
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
    [Fact]
    public void A_missing_patch_is_marked_on_its_own_row_and_the_product_row_is_not()
    {
        // WHAT THIS WINDOW SHOWED BEFORE, AND IT WAS NOT A BLANK. A product row takes
        // its missing state from the product's own cached package, so a product whose
        // package is present and one of whose patches has gone was not marked, and the
        // patch appeared in the list below carrying a formatted size built from a byte
        // count of zero. So the window painted a plausible "0 B" for a file that is not
        // there, and said the same to a screen reader.
        //
        // It matters because the missing-files notice on the main window ends "Open
        // Details for what to do", and this is where it sends people.
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\present.msi", "Test Product", "{AAA}"),
            new(@"C:\Windows\Installer\gone.msp", "Test Product", "{AAA}",
                PatchState: 2, FileExists: false),
        };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService());
        var product = Assert.Single(vm.Products);

        // THE PRODUCT ROW STAYS UNMARKED, WHICH IS THE HALF THAT IS EASY TO GET WRONG.
        // Rolling the patch's absence up to here would print "missing" where this row's
        // own size goes, and this row's own file is on the disk. That would be false of
        // it, so the flag lives on the patch row instead.
        Assert.False(product.IsMissing);

        var patch = Assert.Single(product.Patches);
        Assert.True(patch.IsMissing);
        Assert.Equal("gone.msp, missing", patch.AccessibleName);
        Assert.DoesNotContain("0 B", patch.AccessibleName);
    }

    [Fact]
    public void A_patch_that_is_on_the_disk_keeps_its_size()
    {
        // THE MUST-MISS CONTROL, and the two fixtures differ in one thing only. Without
        // it a change that marked every patch row would pass the test above, and every
        // patch in the window would read "missing" whatever the folder holds.
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\present.msi", "Test Product", "{AAA}"),
            new(@"C:\Windows\Installer\there.msp", "Test Product", "{AAA}",
                PatchState: 2, FileSizeBytes: 1_048_576),
        };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService());
        var patch = Assert.Single(Assert.Single(vm.Products).Patches);

        Assert.False(patch.IsMissing);
        Assert.Equal("there.msp, 1.0 MB", patch.AccessibleName);
    }
}
