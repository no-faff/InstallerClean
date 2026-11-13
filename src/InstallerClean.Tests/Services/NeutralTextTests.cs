using System.Globalization;
using System.IO;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The English a start shows where its check failed to load what it needs: each constant in
/// NetFrameworkCheck.NeutralText is the neutral value of its key in Strings.resx, and the
/// message built from them is the one the strings give in English. Which exceptions end the
/// start that way is held here too.
/// </summary>
public class NeutralTextTests
{
    [Theory]
    [InlineData("Cli.NeedsNetFramework48", NetFrameworkCheck.NeutralText.CliNeedsNetFramework48)]
    [InlineData("Startup.NeedsNetFramework48", NetFrameworkCheck.NeutralText.StartupNeedsNetFramework48)]
    [InlineData("Cli.UpdateDownloadLink", NetFrameworkCheck.NeutralText.CliUpdateDownloadLink)]
    [InlineData("UpdateNotice.DownloadNetFramework48", NetFrameworkCheck.NeutralText.UpdateNoticeDownloadNetFramework48)]
    [InlineData("Window.Main.Title", NetFrameworkCheck.NeutralText.WindowMainTitle)]
    public void Each_constant_is_the_neutral_value_in_Strings_resx(string key, string constant)
    {
        Assert.Equal(Strings.ResourceManager.GetString(key, CultureInfo.InvariantCulture), constant);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_English_message_is_the_one_the_strings_give_in_English(bool commandLine)
    {
        var before = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");
            Assert.Equal(NetFrameworkCheck.Message(commandLine), NetFrameworkCheck.EnglishMessage(commandLine));
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }

    public static TheoryData<Exception> LoadFailures() => new()
    {
        new TypeLoadException(),
        new MissingMethodException(),
        new MissingFieldException(),
        new TypeInitializationException("InstallerClean.Resources.Strings", new TypeLoadException()),
        new FileLoadException(),
        new FileNotFoundException(),
    };

    public static TheoryData<Exception> OtherFailures() => new()
    {
        new InvalidOperationException(),
        new IOException(),
        new UnauthorizedAccessException(),
        new FormatException(),
        new NullReferenceException(),
    };

    [Theory]
    [MemberData(nameof(LoadFailures))]
    public void A_load_failure_ends_the_start(Exception exception)
    {
        Assert.True(StartChecks.IsLoadFailure(exception));
    }

    [Theory]
    [MemberData(nameof(OtherFailures))]
    public void Any_other_failure_lets_the_start_go_on(Exception exception)
    {
        Assert.False(StartChecks.IsLoadFailure(exception));
    }
}
