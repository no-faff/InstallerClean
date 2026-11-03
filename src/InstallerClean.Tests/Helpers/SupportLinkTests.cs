using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

public class SupportLinkTests
{
    [Fact]
    public void The_bare_address_carries_no_query()
        => Assert.DoesNotContain('?', SupportLink.Url);

    [Theory]
    [InlineData("en-GB", "en")]
    [InlineData("en-US", "en")]
    [InlineData("sv-SE", "en")] // unshipped language: the app shows English
    [InlineData("pt-PT", "en")] // only Brazilian Portuguese ships
    [InlineData("de-DE", "de")]
    [InlineData("it", "it")]
    [InlineData("ja-JP", "ja")]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-Hans", "zh-Hans")]
    [InlineData("zh-TW", "zh-Hant")]
    [InlineData("zh-HK", "zh-Hant")]
    public void The_tip_page_carries_the_language_the_app_shows(string culture, string lang)
        => Assert.Equal(
            SupportLink.Url + "?lang=" + lang,
            SupportLink.TipPage(CultureInfo.GetCultureInfo(culture), null));

    [Theory]
    [InlineData("GB", "GB")]
    [InlineData("kr", "KR")]
    [InlineData("Br", "BR")]
    public void A_two_letter_region_is_sent_in_upper_case(string region, string sent)
        => Assert.Equal(
            SupportLink.Url + "?lang=de&region=" + sent,
            SupportLink.TipPage(CultureInfo.GetCultureInfo("de-DE"), region));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("029")] // a UN M.49 number, which Windows gives where there is no ISO code
    [InlineData("1")]
    [InlineData("G")]
    [InlineData("GBR")]
    [InlineData("G1")]
    [InlineData("ÄB")]
    [InlineData("G ")]
    public void Any_other_region_sends_none(string? region)
        => Assert.Equal(
            SupportLink.Url + "?lang=en",
            SupportLink.TipPage(CultureInfo.GetCultureInfo("en-GB"), region));

    [Fact]
    public void The_region_is_read_at_each_open()
    {
        var region = Substitute.For<IWindowsRegion>();
        region.Read().Returns("GB", "DE");

        var first = SupportLink.TipPage(region);
        var second = SupportLink.TipPage(region);

        Assert.EndsWith("&region=GB", first, StringComparison.Ordinal);
        Assert.EndsWith("&region=DE", second, StringComparison.Ordinal);
        region.Received(2).Read();
    }

    [Fact]
    public void The_app_wide_overload_reads_the_language_the_app_is_showing()
    {
        var region = Substitute.For<IWindowsRegion>();
        region.Read().Returns("JP");
        Localisation.Set(CultureInfo.GetCultureInfo("ja-JP"), CultureInfo.GetCultureInfo("ja-JP"));
        try
        {
            Assert.Equal(SupportLink.Url + "?lang=ja&region=JP", SupportLink.TipPage(region));
        }
        finally
        {
            Localisation.Reset();
        }
    }
}
