using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;
using NSubstitute;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The banner shown over Move and Delete while Windows Installer is busy, driven
/// from the enum itself rather than from a list a test author kept in step.
///
/// Both properties here are read at the one moment the buttons are dead, and
/// both fail quietly rather than loudly if a reason is ever added without them:
/// the banner is a bound property, so WPF reports a throw as a binding error and
/// paints nothing, and the label goes to the diagnostic log, where a wrong value
/// is not visible at all. Enumerating the enum is what turns either into a
/// failing build.
/// </summary>
public class ScanViewModelPendingRebootTests
{
    private static ScanViewModel NewViewModel() =>
        new(Substitute.For<IFileSystemScanService>(),
            Substitute.For<IPendingRebootService>(),
            Substitute.For<IDialogService>());

    [Fact]
    public void Every_reason_has_a_banner_of_its_own()
    {
        var vm = NewViewModel();
        var seen = new List<string>();

        foreach (var reason in Enum.GetValues<PendingRebootReason>())
        {
            vm.PendingRebootResult = PendingRebootResult.Block(reason);
            var text = vm.PendingRebootBannerText;

            Assert.False(string.IsNullOrWhiteSpace(text), $"{reason} paints an empty banner");
            Assert.NotEqual(Strings.Body_PendingReboot_Other, text);
            seen.Add(text);
        }

        // Distinct, because the fallback would otherwise satisfy the assertion
        // above for every reason at once and this test would pass over the very
        // gap it exists to close.
        Assert.Equal(seen.Count, seen.Distinct().Count());
    }

    [Fact]
    public void A_reason_with_no_banner_of_its_own_still_says_something()
    {
        // Cast past the enum's members deliberately: this is the state a fourth
        // reason would arrive in before anyone wrote its line, and it must reach
        // the user as a sentence rather than as the blank banner a throw out of
        // a bound property produces.
        var vm = NewViewModel();
        vm.PendingRebootResult = PendingRebootResult.Block((PendingRebootReason)99);

        Assert.Equal(Strings.Body_PendingReboot_Other, vm.PendingRebootBannerText);
        Assert.True(vm.HasPendingReboot);
    }

    [Fact]
    public void Every_reason_has_a_diagnostic_label_of_its_own()
    {
        // The log's filter is the whole point of these: a report that says
        // "clean" about a machine that was blocked sends the reader past it.
        var vm = NewViewModel();
        var seen = new List<string>();

        foreach (var reason in Enum.GetValues<PendingRebootReason>())
        {
            vm.PendingRebootResult = PendingRebootResult.Block(reason);
            Assert.NotEqual(PendingRebootLabels.Clean, vm.PendingRebootLabel);
            seen.Add(vm.PendingRebootLabel);
        }

        Assert.Equal(seen.Count, seen.Distinct().Count());
    }

    [Fact]
    public void No_block_leaves_the_banner_empty_and_the_label_clean()
    {
        var vm = NewViewModel();

        Assert.Equal(string.Empty, vm.PendingRebootBannerText);
        Assert.Equal(PendingRebootLabels.Clean, vm.PendingRebootLabel);
        Assert.False(vm.HasPendingReboot);

        vm.PendingRebootResult = PendingRebootResult.Clean;

        Assert.Equal(string.Empty, vm.PendingRebootBannerText);
        Assert.Equal(PendingRebootLabels.Clean, vm.PendingRebootLabel);
        Assert.False(vm.HasPendingReboot);
    }
}
