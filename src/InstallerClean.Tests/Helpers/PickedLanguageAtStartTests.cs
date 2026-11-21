using System.Text.Json;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The language a copy's start message is shown in is the one the window would apply from
/// the same settings.json.
/// </summary>
public class PickedLanguageAtStartTests
{
    public static TheoryData<string> Samples() => new()
    {
        "{\n  \"language\": \"fr\"\n}",
        "{\"language\":\"zh-Hant\",\"autoUpdateCheck\":false}",
        "{\n  \"moveDestination\": \"D:\\\\Old\",\n  \"language\": \"pt-BR\"\n}",
        "{\n  \"language\": null\n}",
        "{\n  \"language\": \"sv\"\n}",
        "{\n  \"language\": \"\"\n}",
        "{\n  \"Language\": \"de\"\n}",
        "{\n  \"moveDestination\": \"D:\\\\\\\"language\\\": \\\"fr\\\"\"\n}",
        "{}",
    };

    [Theory]
    [MemberData(nameof(Samples))]
    public void The_start_message_takes_the_language_the_window_would(string json)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(json, SettingsService.JsonOptions)!;

        Assert.Equal(LanguagePreference.Resolve(settings.Language), PickedLanguageAtStart.FromSettings(json));
    }

    [Fact]
    public void A_language_the_app_saved_is_read_back()
    {
        var json = JsonSerializer.Serialize(new AppSettings { Language = "de" }, SettingsService.JsonOptions);

        Assert.Equal("de", PickedLanguageAtStart.FromSettings(json)?.Name);
    }

    [Fact]
    public void No_settings_file_picks_nothing()
    {
        Assert.Null(PickedLanguageAtStart.FromSettings(null));
    }
}
