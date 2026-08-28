using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;
using NSubstitute;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// What the window is left holding when a scan stops on its own: the installer
/// records came back empty, or unreadable, or ended in a way nothing could be
/// trusted from.
///
/// The account of it is built in Core and arrives as the exception's message. It
/// reaches a card, and a card lasts as long as the window does, so the same text
/// goes to the crash log and a closing sentence says where. The two things this
/// pins are that the account itself reaches the reader whole, and that the closing
/// sentence never names a file unless one was written.
/// </summary>
public class ScanViewModelStoppedScanTests
{
    private static ScanViewModel NewViewModel() =>
        new(Substitute.For<IFileSystemScanService>(),
            Substitute.For<IPendingRebootService>(),
            Substitute.For<IDialogService>());

    private static Exception AScanThatStopped() =>
        new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty);

    [Fact]
    public void A_scan_that_stops_reaches_the_reader_with_its_whole_account()
    {
        var failure = NewViewModel().DescribeScanFailure(AScanThatStopped());

        // StartsWith rather than Equals: the closing sentence is appended after it,
        // and this test is about the account arriving rather than about that.
        Assert.StartsWith(Strings.Error_InstallerDbEmpty, failure.Message, StringComparison.Ordinal);

        // The rung matters as much as the text. Falling through to the generic arm
        // would replace this account with a type name, which is what that arm exists
        // to do for an exception nobody anticipated.
        Assert.Equal(Strings.Error_InstallerDbUnavailableTitle, failure.Title);
        Assert.Equal(Strings.Status_ScanFailedDb, failure.StatusLine);
        Assert.True(failure.IsError);
    }

    [Fact]
    public void The_account_is_written_to_the_crash_log_and_the_message_says_where()
    {
        // Whether the log can be written is a property of the host: the write goes
        // through Win32, so it succeeds where the app runs and fails where the suite
        // is being run for convenience. Asking first is what decides which form to
        // assert; each host pins one of the two and neither pins both.
        var probe = CrashLog.TryWrite(new InvalidOperationException("probe"));

        // WHAT THE LOG ALREADY HELD, because it is appended to and kept until it
        // reaches half a megabyte. Reading the whole file would find this account
        // from any earlier run on the same machine, so a method that had stopped
        // writing would still pass here and pass for ever after the first time.
        //
        // READ AS A STRING RATHER THAN MEASURED AS A FILE, and the difference is not
        // cosmetic. A length in bytes is not an index into the text: the file opens
        // with a UTF-8 preamble and a privacy header that is translated, so on a
        // Japanese or Russian profile the byte count runs past the end of the string
        // and the slice throws, while English stays green for ever.
        string alreadyThere = probe.Written ? File.ReadAllText(probe.Path) : string.Empty;

        var failure = NewViewModel().DescribeScanFailure(AScanThatStopped());
        var added = failure.Message[Strings.Error_InstallerDbEmpty.Length..].Trim();

        if (!probe.Written)
        {
            Assert.Equal(string.Empty, added);
            return;
        }

        Assert.Contains("crash.log", added, StringComparison.OrdinalIgnoreCase);

        // The file itself, not only the sentence about it, and only what this run
        // added to it.
        var appended = File.ReadAllText(probe.Path)[alreadyThere.Length..];
        Assert.Contains(Strings.Error_InstallerDbEmpty, appended, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unanticipated_failure_still_takes_the_generic_rung()
    {
        // The must-miss control for the two above. Both assert that a deliberate stop
        // does NOT look like a crash, and a ladder that had collapsed to one rung
        // would satisfy them only if this one fails.
        var failure = NewViewModel().DescribeScanFailure(new InvalidProgramException("nothing anticipated this"));

        Assert.Equal(Strings.Error_ScanFailedTitle, failure.Title);
        Assert.DoesNotContain("nothing anticipated this", failure.Message, StringComparison.Ordinal);
    }
}
