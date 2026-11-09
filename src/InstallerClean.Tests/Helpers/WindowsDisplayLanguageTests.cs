using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

public class WindowsDisplayLanguageTests
{
    // The country goes and the language stays. The opt-in report carries this, and
    // a value naming a country is the one outcome these rows exist to rule out.
    [Theory]
    [InlineData("en-US", "en")]
    [InlineData("en-GB", "en")]
    [InlineData("cs-CZ", "cs")]
    [InlineData("pt-BR", "pt")]
    [InlineData("pt-PT", "pt")]
    [InlineData("es-419", "es")]
    [InlineData("de", "de")]
    public void The_country_is_taken_off_and_the_language_kept(string name, string expected)
        => Assert.Equal(expected, WindowsDisplayLanguage.Neutral(name));

    // A language written in more than one script keeps the script, which is part of
    // which language it is: Traditional and Simplified Chinese are read by different
    // people, and zh-TW names no script of its own until its parent does.
    [Theory]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-TW", "zh-Hant")]
    [InlineData("zh-HK", "zh-Hant")]
    [InlineData("zh-SG", "zh-Hans")]
    [InlineData("zh-MO", "zh-Hant")]
    [InlineData("zh-Hans", "zh-Hans")]
    [InlineData("zh-CHS", "zh-Hans")]
    [InlineData("zh-CHT", "zh-Hant")]
    [InlineData("sr-Latn-RS", "sr-Latn")]
    [InlineData("sr-Cyrl-RS", "sr-Cyrl")]
    [InlineData("zh-Hant", "zh-Hant")]
    public void A_script_is_kept(string name, string expected)
        => Assert.Equal(expected, WindowsDisplayLanguage.Neutral(name));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void No_name_is_unreadable(string? name)
        => Assert.Equal(WindowsDisplayLanguage.Unreadable, WindowsDisplayLanguage.Neutral(name));

    [Fact]
    public void A_name_that_is_not_a_culture_is_unrecognised()
        => Assert.Equal(WindowsDisplayLanguage.Unrecognised, WindowsDisplayLanguage.Neutral("not a culture"));

    // The call itself, which only Windows answers. Every machine the suite runs on
    // has a display language, so unreadable here is the call failing, and a fault in
    // it would otherwise send unreadable from every machine with nothing failing.
    [Fact]
    public void The_display_language_reads_from_Windows()
    {
        var language = WindowsDisplayLanguage.Current();

        Assert.NotEqual(WindowsDisplayLanguage.Unreadable, language);
        Assert.NotEqual(WindowsDisplayLanguage.Unrecognised, language);
        Assert.Equal(language, WindowsDisplayLanguage.Neutral(language));
    }
}
