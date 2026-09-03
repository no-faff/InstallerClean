using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;
using NSubstitute;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The one line on the main window that tells a reader the app could not be certain,
/// over the machines that reach it and the machines that must not.
///
/// IT IS THE THIRD OF A TRIPLE AND THE OTHER TWO ARE PINNED. The left-alone line and
/// the superseded line have tests of their own; this one is the only surface on the
/// window that says the offer beside it is short, so a reader who does not meet it
/// takes the list as the whole answer.
///
/// THREE THINGS DECIDE WHAT IT SAYS AND THEY ARE PULLED APART IN THE FIXTURES. Whether
/// anything was kept back, whether anything was offered beside it, and which of the two
/// sentences the withholding earned. Read what each test SETS UP rather than what it
/// asserts: the counts are given deliberately different values so that a gate reading
/// the wrong one lands on a figure no assertion here accepts.
/// </summary>
public class ScanViewModelNothingListedTests
{
    private static readonly string Orphaned = Strings.Reason_Orphaned;

    private static ScanViewModel Driven(ScanResult result)
    {
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var vm = new ScanViewModel(
            scan,
            Substitute.For<IPendingRebootService>(),
            Substitute.For<IDialogService>());

        vm.ScanWithProgressAsync(null).GetAwaiter().GetResult();
        return vm;
    }

    private static OrphanedFile[] Files(int n, string prefix) =>
        Enumerable.Range(0, n)
            .Select(i => new OrphanedFile(
                $@"C:\Windows\Installer\{prefix}{i}.msi", 1024, false, false, false, Orphaned))
            .ToArray();

    /// <summary>
    /// The three footnote gates, in the order the scan raises them.
    ///
    /// WHY A TEST RATHER THAN A READING. The window turns each of these property
    /// changes into a live-region announcement and queues them as they arrive, so
    /// this sequence is what a screen reader says and in what order. Nothing else
    /// decides it: the code-behind matches one property name per notification, so
    /// its own layout says nothing about sequence. Reordering the assignments in
    /// OnScanCompleted is therefore a change to the window, and this is the only
    /// place that can catch one.
    ///
    /// The fixture turns all three on at once, which is what makes an order
    /// observable at all: any run that trips fewer than three leaves a sequence
    /// that a wrong order still satisfies.
    /// </summary>
    [Fact]
    public async Task The_three_footnote_gates_are_raised_in_the_order_their_lines_are_drawn()
    {
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                RemovableFiles: Files(4, "offered"),
                RegisteredPackages: Array.Empty<RegisteredPackage>(),
                RegisteredTotalBytes: 0,
                MissingAffectedCount: 2,
                WithheldCount: 5,
                WithheldFiles: Files(3, "held"),
                WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 3)));

        var vm = new ScanViewModel(
            scan,
            Substitute.For<IPendingRebootService>(),
            Substitute.For<IDialogService>());

        var drawn = new[]
        {
            nameof(ScanViewModel.HasNothingListed),
            nameof(ScanViewModel.HasSupersededHeldBack),
            nameof(ScanViewModel.HasMissingFromDisk),
        };
        var raised = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is { } name && drawn.Contains(name))
                raised.Add(name);
        };

        await vm.ScanWithProgressAsync(null);

        // All three are on, so the sequence below is over a full set rather than a
        // subset a wrong order would also produce.
        Assert.True(vm.HasNothingListed);
        Assert.True(vm.HasSupersededHeldBack);
        Assert.True(vm.HasMissingFromDisk);
        Assert.Equal(drawn, raised);
    }

    [Fact]
    public void A_run_that_offered_something_and_kept_files_back_says_so_and_says_how_many()
    {
        var vm = Driven(new ScanResult(
            RemovableFiles: Files(4, "offered"),
            RegisteredPackages: Array.Empty<RegisteredPackage>(),
            RegisteredTotalBytes: 0,
            WithheldFiles: Files(3, "held"),
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 3)));

        Assert.True(vm.HasNothingListed);
        Assert.Equal(3, vm.NothingListedCount);
        Assert.Contains("3", vm.NothingListedText, StringComparison.Ordinal);
    }

    [Fact]
    public void The_line_is_off_where_the_scan_kept_nothing_back()
    {
        var vm = Driven(new ScanResult(
            RemovableFiles: Files(4, "offered"),
            RegisteredPackages: Array.Empty<RegisteredPackage>(),
            RegisteredTotalBytes: 0));

        Assert.False(vm.HasNothingListed);
        Assert.Equal(0, vm.NothingListedCount);
    }

    [Fact]
    public void The_line_is_off_where_nothing_was_offered_beside_it()
    {
        // That machine never reaches this window's list: the completion screen
        // replaces the surface and says the same thing in its own words. A line here
        // as well would say it twice, and the screen's own wording is the one he
        // approved for a machine with nothing on its list.
        var vm = Driven(new ScanResult(
            RemovableFiles: Array.Empty<OrphanedFile>(),
            RegisteredPackages: Array.Empty<RegisteredPackage>(),
            RegisteredTotalBytes: 0,
            WithheldFiles: Files(3, "held"),
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 3)));

        Assert.False(vm.HasNothingListed);
        Assert.Equal(0, vm.NothingListedCount);
    }

    [Fact]
    public void The_one_form_asserts_oneness_in_words_and_carries_no_numeral()
    {
        // The one-form spells no numeral, which is what makes the prefix a cardinality
        // string rather than a grammatical one. Pinned on the surface that renders it
        // as well as in CountedStringTests, because the two questions are answered by
        // different code.
        var vm = Driven(new ScanResult(
            RemovableFiles: Files(2, "offered"),
            RegisteredPackages: Array.Empty<RegisteredPackage>(),
            RegisteredTotalBytes: 0,
            WithheldFiles: Files(1, "held"),
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1)));

        Assert.Equal(1, vm.NothingListedCount);
        Assert.Equal(
            string.Format(Strings.Summary_NothingListedPerFile_Singular,
                1, DisplayHelpers.PluraliseFile(1)),
            vm.NothingListedText);
        Assert.DoesNotContain("1 files", vm.NothingListedText, StringComparison.Ordinal);
    }

    [Fact]
    public void The_count_is_every_file_kept_back_and_not_one_decision_s_share()
    {
        // THE MUST-MISS THAT SETS THE FIGURES APART. Four figures are within reach here
        // and only one of them is the answer: the wholesale arm reads 1, the superseded
        // figure reads 9, the split's own total reads 2 because it does not account for
        // every file on the list, and only the list's own length reads 3. No assertion
        // here accepts any of the other three.
        //
        // THE SPLIT IS LEFT SHORT ON PURPOSE. The line is the whole withholding, so a
        // count derived from the split would under-report against the list the Details
        // window shows on any machine the split does not account for whole.
        var vm = Driven(new ScanResult(
            RemovableFiles: Files(2, "offered"),
            RegisteredPackages: Array.Empty<RegisteredPackage>(),
            RegisteredTotalBytes: 0,
            WithheldCount: 9,
            WithheldFiles: Files(3, "held"),
            WalkOfferWithheldWholesale: true,
            WithheldBy: new WithholdingSplit(IdentityUnestablishedCount: 1, WholesaleCount: 1)));

        Assert.True(vm.HasNothingListed);
        Assert.Equal(3, vm.NothingListedCount);
    }

    [Fact]
    public void A_withholding_the_wholesale_arm_accounts_for_whole_takes_the_wholesale_sentence()
    {
        var vm = Driven(new ScanResult(
            RemovableFiles: Files(2, "offered"),
            RegisteredPackages: Array.Empty<RegisteredPackage>(),
            RegisteredTotalBytes: 0,
            WithheldFiles: Files(2, "held"),
            WalkOfferWithheldWholesale: true,
            WithheldBy: new WithholdingSplit(WholesaleCount: 2)));

        Assert.False(vm.NothingListedIsPerFile);
        Assert.Equal(
            string.Format(Strings.Summary_NothingListed_Plural,
                2, DisplayHelpers.PluraliseFile(2)),
            vm.NothingListedText);
    }

    [Fact]
    public void Anything_else_takes_the_sentence_that_is_true_of_every_file_on_the_list()
    {
        // A run that kept files back both ways. The wholesale sentence names something
        // the scan could not establish about the records, which is false of the file
        // the per-file decision took, so the machine gets the one sentence true of all
        // of them. Named rather than merely different from the wholesale one, because
        // NotEqual would be satisfied by the right key formatted with wrong arguments.
        var vm = Driven(new ScanResult(
            RemovableFiles: Files(2, "offered"),
            RegisteredPackages: Array.Empty<RegisteredPackage>(),
            RegisteredTotalBytes: 0,
            WithheldFiles: Files(3, "held"),
            WalkOfferWithheldWholesale: true,
            WithheldBy: new WithholdingSplit(IdentityUnestablishedCount: 1, WholesaleCount: 2)));

        Assert.True(vm.NothingListedIsPerFile);
        Assert.Equal(
            string.Format(Strings.Summary_NothingListedPerFile_Plural,
                3, DisplayHelpers.PluraliseFile(3)),
            vm.NothingListedText);
    }

    [Fact]
    public void The_line_and_the_superseded_line_are_two_different_counts_on_one_screen()
    {
        // They sit one above the other and count different populations: the superseded
        // figure is registrations the records call superseded, and this one is files
        // the walk found and could not clear. A machine carrying both must show both
        // figures rather than one figure twice, which is what a gate reading across
        // them would produce.
        var vm = Driven(new ScanResult(
            RemovableFiles: Files(2, "offered"),
            RegisteredPackages: Array.Empty<RegisteredPackage>(),
            RegisteredTotalBytes: 0,
            WithheldCount: 7,
            WithheldFiles: Files(3, "held"),
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 3)));

        Assert.True(vm.HasNothingListed);
        Assert.True(vm.HasSupersededHeldBack);
        Assert.Equal(3, vm.NothingListedCount);
        Assert.Equal(7, vm.SupersededHeldBackCount);
    }
}
