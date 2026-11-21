using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The line naming the drives and shares a scan, or the check made before a Move or
/// Delete, carried on without. Every screen that names them takes the line from here, so
/// the words and the way each root is named are pinned once for all of them.
/// </summary>
public class SourcesGivenUpReportTests
{
    private const string Tail =
        ", so it left alone any file it couldn't check without it. Re-scan once it's responding normally.";

    private const string PathTail =
        ", so it left alone any file it couldn't check without it. Re-scan once it's responding normally.";

    private const string CommandLineTail =
        ", so it left alone any file it couldn't check without it. Once it's responding normally, run the command again.";

    private const string CommandLinePathTail =
        ", so it left alone any file it couldn't check without it. Once it's responding normally, run the command again.";

    private static SourceRootGivenUp Root(string root) => new(root, SourceRootGiveUpRoute.NoAnswer, 1);

    [Fact]
    public void Nothing_given_up_produces_no_line()
    {
        // Each host tests the line for emptiness to decide whether it appears at all.
        using var scope = new LocalisationScope(British);

        Assert.Equal(string.Empty, SourcesGivenUpReport.WindowLine([]));
        Assert.Equal(string.Empty, SourcesGivenUpReport.CommandLine([]));
    }

    [Theory]
    [InlineData("D:", "InstallerClean stopped waiting for drive D:" + Tail)]
    [InlineData("d:", "InstallerClean stopped waiting for drive D:" + Tail)]
    [InlineData(@"\\nas\installers", @"InstallerClean stopped waiting for \\nas\installers" + PathTail)]
    [InlineData(@"GLOBALROOT\Device\Mup\nas\installers", @"InstallerClean stopped waiting for GLOBALROOT\Device\Mup\nas\installers" + PathTail)]
    public void One_drive_or_share_is_named_inside_the_sentence(string root, string line)
    {
        using var scope = new LocalisationScope(British);

        Assert.Equal(line, SourcesGivenUpReport.WindowLine([Root(root)]));
    }

    [Fact]
    public void A_drive_takes_the_drive_form_and_anything_else_the_path_form()
    {
        // Against the keys rather than the English, so a language whose two forms differ
        // in more than the word for "drive" is held to the right one.
        using var scope = new LocalisationScope(British);

        Assert.Equal(string.Format(Strings.Summary_SourceGivenUp_Drive, "E:"),
            SourcesGivenUpReport.WindowLine([Root("e:")]));
        Assert.Equal(string.Format(Strings.Summary_SourceGivenUp_Path, @"\\nas\installers"),
            SourcesGivenUpReport.WindowLine([Root(@"\\nas\installers")]));
    }

    [Fact]
    public void More_than_one_are_listed_in_brackets_in_the_order_given_up()
    {
        using var scope = new LocalisationScope(British);

        Assert.Equal(
            @"InstallerClean stopped waiting for more than one drive or share (drive D:, \\nas\installers, drive E:), "
            + "so it left alone any file it couldn't check without them. "
            + "Re-scan once they're responding normally.",
            SourcesGivenUpReport.WindowLine([Root("d:"), Root(@"\\nas\installers"), Root("E:")]));
    }

    [Fact]
    public void Each_item_in_the_list_is_joined_with_the_language_s_own_separator()
    {
        using var scope = new LocalisationScope(British);

        Assert.Equal(
            string.Format(Strings.Summary_SourcesGivenUp,
                string.Format(Strings.Display_DriveName, "D:") + Strings.Display_ListSeparator + @"\\nas\installers"),
            SourcesGivenUpReport.WindowLine([Root("D:"), Root(@"\\nas\installers")]));
    }

    [Fact]
    public void A_drive_letter_in_the_list_takes_its_capital_under_Turkish_too()
    {
        var turkish = CultureInfo.GetCultureInfo("tr-TR");
        using var scope = new LocalisationScope(turkish);
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = turkish;
        try
        {
            Assert.Equal(string.Format(Strings.Summary_SourceGivenUp_Drive, "I:"),
                SourcesGivenUpReport.WindowLine([Root("i:")]));
            Assert.Contains(string.Format(Strings.Display_DriveName, "I:"),
                SourcesGivenUpReport.WindowLine([Root("i:"), Root("j:")]), StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("D:", "InstallerClean stopped waiting for drive D:" + CommandLineTail)]
    [InlineData("d:", "InstallerClean stopped waiting for drive D:" + CommandLineTail)]
    [InlineData(@"\\nas\installers", @"InstallerClean stopped waiting for \\nas\installers" + CommandLinePathTail)]
    [InlineData(@"GLOBALROOT\Device\Mup\nas\installers", @"InstallerClean stopped waiting for GLOBALROOT\Device\Mup\nas\installers" + CommandLinePathTail)]
    public void The_command_line_names_one_drive_or_share_inside_its_own_sentence(string root, string line)
    {
        using var scope = new LocalisationScope(British);

        Assert.Equal(line, SourcesGivenUpReport.CommandLine([Root(root)]));
    }

    [Fact]
    public void The_command_line_takes_its_own_drive_and_path_forms()
    {
        // Against the command line's keys, so neither host can borrow the other's words:
        // the window's lines end on Re-scan, a button the command line has not got.
        using var scope = new LocalisationScope(British);

        Assert.Equal(string.Format(Strings.Cli_SourceGivenUp_Drive, "E:"),
            SourcesGivenUpReport.CommandLine([Root("e:")]));
        Assert.Equal(string.Format(Strings.Cli_SourceGivenUp_Path, @"\\nas\installers"),
            SourcesGivenUpReport.CommandLine([Root(@"\\nas\installers")]));
    }

    [Fact]
    public void The_command_line_lists_more_than_one_in_brackets_in_the_order_given_up()
    {
        using var scope = new LocalisationScope(British);

        Assert.Equal(
            @"InstallerClean stopped waiting for more than one drive or share (drive D:, \\nas\installers, drive E:), "
            + "so it left alone any file it couldn't check without them. "
            + "Once they're responding normally, run the command again.",
            SourcesGivenUpReport.CommandLine([Root("d:"), Root(@"\\nas\installers"), Root("E:")]));
    }

    [Fact]
    public void The_list_names_each_drive_with_its_word_and_each_share_as_spelled()
    {
        // The command line's Application-log entries name the drives and shares through
        // this same list, so it is pinned on its own as well as inside the sentences.
        using var scope = new LocalisationScope(British);

        Assert.Equal(@"drive D:, \\nas\installers",
            SourcesGivenUpReport.ListOf([Root("d:"), Root(@"\\nas\installers")]));
        Assert.Equal("drive E:", SourcesGivenUpReport.ListOf([Root("e:")]));
    }

    private static readonly CultureInfo British = CultureInfo.GetCultureInfo("en-GB");

    /// <summary>
    /// Pins the app's language for one test and drops it on the way out. The pin is
    /// process-global, which the assembly's serial test run (see AssemblyInfo.cs) is what
    /// makes safe.
    /// </summary>
    private sealed class LocalisationScope : IDisposable
    {
        public LocalisationScope(CultureInfo culture) => Localisation.Set(culture, culture);

        public void Dispose() => Localisation.Reset();
    }
}
