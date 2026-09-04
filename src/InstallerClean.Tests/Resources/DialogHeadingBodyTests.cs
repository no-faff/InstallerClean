using InstallerClean.Resources;

namespace InstallerClean.Tests.Resources;

/// <summary>
/// A message dialog paints its heading and its body one above the other, and
/// announces them joined, so a body opening with its own heading says the same
/// words twice on the screen and twice to a screen reader.
/// </summary>
/// <remarks>
/// THE PAIRS BELOW ARE EVERY DIALOG WHOSE HEADING AND BODY ARE BOTH FIXED
/// STRINGS, not a sample of them. The rest of the app's dialogs compose one side
/// or both at the call site (a ScanFailure's own two fields, an exception's
/// message, a description built from a crash-log result), so there is no pair to
/// read here; those are covered where they are built.
///
/// A body may of course say the same THING as its heading in other words, which
/// is what a body is for. What this pins is the literal repetition.
/// </remarks>
public class DialogHeadingBodyTests
{
    public static TheoryData<string, string> HeadingAndBody() => new()
    {
        { Strings.Error_InvalidDestinationTitle, Strings.Error_DestinationNotFullyQualified },
        { Strings.Error_InvalidDestinationTitle, Strings.Error_DestinationInsideInstaller },
        { Strings.Error_InvalidDestinationTitle, Strings.Error_DestinationInSystemFolder },
        { Strings.Error_NotEnoughSpaceTitle, Strings.Error_NotEnoughSpaceBody },
        { Strings.Error_MoveInstallerLockUnavailableTitle, Strings.Error_MoveInstallerLockUnavailable },
        { Strings.Error_InstallerLockUnavailableTitle, Strings.Error_InstallerLockUnavailable },
        { Strings.Error_MoveFailedTitle, Strings.Status_MoveFailed },
        { Strings.Error_MoveFailedTitle, Strings.Status_MoveFailed_NoLog },
        { Strings.Error_DeleteFailedTitle, Strings.Status_DeleteFailed },
        { Strings.Error_DeleteFailedTitle, Strings.Status_DeleteFailed_NoLog },
        { Strings.Error_MoveStoppedTitle, Strings.Error_DestinationChangedMidBatch },
        { Strings.Error_SettingNotSavedTitle, Strings.Error_SettingNotSavedBody },
    };

    [Theory]
    [MemberData(nameof(HeadingAndBody))]
    public void No_dialog_body_opens_by_repeating_its_own_heading(string heading, string body)
    {
        Assert.False(
            body.StartsWith(heading, StringComparison.OrdinalIgnoreCase),
            $"body opens with the heading \"{heading}\": {body}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_scan_failure_body_does_repeat_its_heading_and_has_to(bool noLog)
    {
        // The one exemption, pinned rather than left out, so a later pass that
        // tidies it to match the others has to come and read this first. The
        // string is the dialog body AND the main window's error line AND what the
        // scan announcer speaks, and on those last two nothing is drawn above it,
        // so it names its own subject or the reader is told a bare exception type
        // with no clue what it was doing.
        var body = noLog ? Strings.Status_ScanFailedDetails_NoLog : Strings.Status_ScanFailedDetails;

        Assert.StartsWith(Strings.Error_ScanFailedTitle, body, StringComparison.Ordinal);
    }
}
