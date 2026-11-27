using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;
using NSubstitute;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The banner shown over Move and Delete while the pending-reboot check holds them,
/// driven from the enum itself rather than from a list a test author kept in step.
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

            // A reason that shares another's banner on purpose is checked against
            // that reason's banner and left out of the distinct set, so the set
            // still holds one banner per reason.
            if (SharedBanner.TryGetValue(reason, out var sharedWith))
            {
                vm.PendingRebootResult = PendingRebootResult.Block(sharedWith);
                Assert.Equal(vm.PendingRebootBannerText, text);
                continue;
            }

            seen.Add(text);
        }

        // DISTINCT, AND IT IS WHAT CATCHES A REASON WITH NO BANNER OF ITS OWN. The
        // in-progress file's banner is the fallback's text, so a reason added later
        // that falls through to the fallback duplicates it here.
        Assert.Equal(seen.Count, seen.Distinct().Count());
        Assert.Contains(Strings.Body_PendingReboot_Other, seen);
    }

    /// <summary>
    /// The reasons whose banner is another reason's, each with the reason it shares.
    /// A refused read of Windows Installer's in-progress file shows the refused-lock
    /// banner, whose sentence says Windows refused the app permission to check
    /// whether Windows Installer is busy. A Windows Installer service whose state
    /// would not read shows the banner of a lock that could not be sampled, whose
    /// sentence says the app could not check whether Windows Installer is busy.
    /// </summary>
    private static readonly Dictionary<PendingRebootReason, PendingRebootReason> SharedBanner = new()
    {
        [PendingRebootReason.InstallerInProgressMarkerAccessRefused] = PendingRebootReason.MsiExecuteMutexAccessRefused,
        [PendingRebootReason.InstallerServiceUnreadable] = PendingRebootReason.MsiExecuteMutexUnreadable,
    };

    /// <summary>
    /// A Windows Installer lock the app was refused permission to open paints its own
    /// banner, never the one saying something is using Windows Installer: nothing has
    /// been seen holding the lock, and that banner would tell the user to wait for
    /// something that may not be happening.
    /// </summary>
    [Fact]
    public void A_refused_lock_paints_its_own_banner_and_not_the_busy_one()
    {
        var vm = NewViewModel();
        vm.PendingRebootResult = PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexAccessRefused);

        Assert.True(vm.HasPendingReboot);
        Assert.Equal(Strings.Body_PendingReboot_MsiExecuteMutexAccessRefused, vm.PendingRebootBannerText);
        Assert.NotEqual(Strings.Body_PendingReboot_MsiExecuteMutex, vm.PendingRebootBannerText);
    }

    [Fact]
    public void A_reason_with_no_banner_of_its_own_still_says_something()
    {
        // Cast past the enum's members deliberately: this is the state a reason added
        // later would arrive in before anyone wrote its line, and it must reach
        // the user as a sentence rather than as the blank banner a throw out of
        // a bound property produces.
        var vm = NewViewModel();
        vm.PendingRebootResult = PendingRebootResult.Block((PendingRebootReason)99);

        Assert.Equal(Strings.Body_PendingReboot_Other, vm.PendingRebootBannerText);
        Assert.True(vm.HasPendingReboot);
    }

    // Three tests over a non-localised PendingRebootLabel stood here and went with
    // that property in schema 4, the payload having been its only reader. What
    // they were protecting, that a blocked machine is never recorded as clean, is
    // now protected by there being nothing to record: the state reaches the screen
    // through the banner above, and both action paths take their own fresh probe.

    [Fact]
    public void A_lock_whose_check_failed_on_a_Windows_error_gives_that_error_and_one_that_did_not_gives_no_cause()
    {
        var vm = NewViewModel();

        vm.PendingRebootResult = PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexUnreadable, windowsError: 8);
        Assert.Equal(
            string.Format(Strings.Body_PendingReboot_MsiExecuteMutexUnreadableWindowsError,
                WindowsErrors.MessageFor(8), "8"),
            vm.PendingRebootBannerText);

        vm.PendingRebootResult = PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexUnreadable);
        Assert.Equal(Strings.Body_PendingReboot_MsiExecuteMutexUnreadable, vm.PendingRebootBannerText);
    }

    [Fact]
    public void No_block_leaves_the_banner_empty()
    {
        var vm = NewViewModel();

        Assert.Equal(string.Empty, vm.PendingRebootBannerText);
        Assert.False(vm.HasPendingReboot);

        vm.PendingRebootResult = PendingRebootResult.Clean;

        Assert.Equal(string.Empty, vm.PendingRebootBannerText);
        Assert.False(vm.HasPendingReboot);
    }
}
