using System.Globalization;
using InstallerClean.Compatibility;

namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// <see cref="FrameworkCultureDisplayName"/> names a culture in the UI culture's language as
/// .NET 10's CultureInfo.DisplayName does on Windows, from the ICU Windows ships. The
/// expected names are what .NET 10 gave on Windows 11 for each pair.
/// </summary>
public class FrameworkCultureDisplayNameTests
{
    [Fact]
    public void Windows_here_ships_an_ICU_with_the_functions_the_lookup_calls()
    {
        Assert.NotNull(FrameworkCultureDisplayName.Icu.Load());
    }

    [Theory]
    [InlineData("en-GB", "en-GB", "English (United Kingdom)")]
    [InlineData("en-GB", "zh-Hans", "Chinese (Simplified)")]
    [InlineData("en-GB", "ru", "Russian")]
    [InlineData("en-GB", "ja", "Japanese")]
    [InlineData("en-GB", "pt-BR", "Portuguese (Brazil)")]
    [InlineData("en-GB", "fr", "French")]
    [InlineData("fr", "en-GB", "anglais (Royaume-Uni)")]
    [InlineData("fr", "zh-Hans", "chinois (simplifi\u00E9)")]
    [InlineData("fr", "ru", "russe")]
    [InlineData("fr", "ja", "japonais")]
    [InlineData("fr", "pt-BR", "portugais (Br\u00E9sil)")]
    [InlineData("fr", "fr", "fran\u00E7ais")]
    [InlineData("de", "en-GB", "Englisch (Vereinigtes K\u00F6nigreich)")]
    [InlineData("de", "zh-Hans", "Chinesisch (Vereinfacht)")]
    [InlineData("de", "ru", "Russisch")]
    [InlineData("de", "ja", "Japanisch")]
    [InlineData("de", "pt-BR", "Portugiesisch (Brasilien)")]
    [InlineData("de", "fr", "Franz\u00F6sisch")]
    [InlineData("ru", "en-GB", "\u0430\u043D\u0433\u043B\u0438\u0439\u0441\u043A\u0438\u0439 (\u0412\u0435\u043B\u0438\u043A\u043E\u0431\u0440\u0438\u0442\u0430\u043D\u0438\u044F)")]
    [InlineData("ru", "zh-Hans", "\u043A\u0438\u0442\u0430\u0439\u0441\u043A\u0438\u0439 (\u0443\u043F\u0440\u043E\u0449\u0435\u043D\u043D\u0430\u044F)")]
    [InlineData("ru", "ru", "\u0440\u0443\u0441\u0441\u043A\u0438\u0439")]
    [InlineData("ru", "ja", "\u044F\u043F\u043E\u043D\u0441\u043A\u0438\u0439")]
    [InlineData("ru", "pt-BR", "\u043F\u043E\u0440\u0442\u0443\u0433\u0430\u043B\u044C\u0441\u043A\u0438\u0439 (\u0411\u0440\u0430\u0437\u0438\u043B\u0438\u044F)")]
    [InlineData("ru", "fr", "\u0444\u0440\u0430\u043D\u0446\u0443\u0437\u0441\u043A\u0438\u0439")]
    [InlineData("ja", "en-GB", "\u82F1\u8A9E (\u30A4\u30AE\u30EA\u30B9)")]
    [InlineData("ja", "zh-Hans", "\u4E2D\u56FD\u8A9E (\u7C21\u4F53\u5B57)")]
    [InlineData("ja", "ru", "\u30ED\u30B7\u30A2\u8A9E")]
    [InlineData("ja", "ja", "\u65E5\u672C\u8A9E")]
    [InlineData("ja", "pt-BR", "\u30DD\u30EB\u30C8\u30AC\u30EB\u8A9E (\u30D6\u30E9\u30B8\u30EB)")]
    [InlineData("ja", "fr", "\u30D5\u30E9\u30F3\u30B9\u8A9E")]
    [InlineData("zh-Hans", "en-GB", "\u82F1\u8BED\uFF08\u82F1\u56FD\uFF09")]
    [InlineData("zh-Hans", "zh-Hans", "\u4E2D\u6587\uFF08\u7B80\u4F53\uFF09")]
    [InlineData("zh-Hans", "ru", "\u4FC4\u8BED")]
    [InlineData("zh-Hans", "ja", "\u65E5\u8BED")]
    [InlineData("zh-Hans", "pt-BR", "\u8461\u8404\u7259\u8BED\uFF08\u5DF4\u897F\uFF09")]
    [InlineData("zh-Hans", "fr", "\u6CD5\u8BED")]
    [InlineData("tr", "en-GB", "\u0130ngilizce (Birle\u015Fik Krall\u0131k)")]
    [InlineData("tr", "zh-Hans", "\u00C7ince (Basitle\u015Ftirilmi\u015F)")]
    [InlineData("tr", "ru", "Rus\u00E7a")]
    [InlineData("tr", "ja", "Japonca")]
    [InlineData("tr", "pt-BR", "Portekizce (Brezilya)")]
    [InlineData("tr", "fr", "Frans\u0131zca")]
    public void A_culture_is_named_in_the_UI_culture_s_language(string uiCulture, string culture, string expected)
    {
        var icu = FrameworkCultureDisplayName.Icu.Load();
        Assert.NotNull(icu);

        Assert.Equal(expected, FrameworkCultureDisplayName.Of(new CultureInfo(culture), uiCulture, icu));
    }

    // zh-CHT and zh-CHS are looked up as zh-Hant and zh-Hans, as .NET 10 looks them up.
    [Theory]
    [InlineData("zh-CHT", "zh-Hant")]
    [InlineData("zh-CHS", "zh-Hans")]
    public void A_legacy_Chinese_name_is_named_as_its_script(string legacy, string script)
    {
        var icu = FrameworkCultureDisplayName.Icu.Load();

        Assert.Equal(FrameworkCultureDisplayName.Of(new CultureInfo(script), "fr", icu),
            FrameworkCultureDisplayName.Of(new CultureInfo(legacy), "fr", icu));
    }

    // A UI locale ICU holds no names for gives the English name, as .NET 10 retries.
    [Fact]
    public void A_UI_locale_with_no_names_of_its_own_gives_the_English_name()
    {
        Assert.Equal("Russian",
            FrameworkCultureDisplayName.Of(new CultureInfo("ru"), "qaa", FrameworkCultureDisplayName.Icu.Load()));
    }

    // Where Windows has no ICU and the default UI culture is not the regional format, as
    // here (the test assembly pins en-GB, and CI's runners are set to en-US), .NET 10
    // names a culture by its own name.
    [Fact]
    public void Without_ICU_a_default_UI_culture_other_than_the_regional_format_gives_the_own_name()
    {
        Assert.Equal(new CultureInfo("ru").NativeName,
            FrameworkCultureDisplayName.NlsDisplayName(new CultureInfo("ru"), "fr"));
    }

    [Fact]
    public void The_invariant_culture_is_named_by_its_native_name()
    {
        Assert.Equal(CultureInfo.InvariantCulture.NativeName,
            FrameworkCultureDisplayName.Of(CultureInfo.InvariantCulture, "fr", FrameworkCultureDisplayName.Icu.Load()));
    }

#if NETFRAMEWORK
    // .NET Framework's own DisplayName, which the shipped projects are barred from reading,
    // answers in the display language Windows is set to: on a runner set to English, the
    // French name of Russian, "russe", is not what it gives.
    [Fact]
    public void The_framework_s_own_display_name_does_not_follow_the_UI_culture()
    {
        var before = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr");
            Assert.NotEqual("russe", new CultureInfo("ru").DisplayName);
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }
#endif
}
