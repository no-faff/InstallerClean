using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

public class LanguagePreferenceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_returns_null_for_automatic(string? setting)
        => Assert.Null(LanguagePreference.Resolve(setting));

    [Fact]
    public void Resolve_returns_culture_for_a_supported_name()
        => Assert.Equal("it", LanguagePreference.Resolve("it")!.Name);

    [Fact]
    public void Resolve_is_case_insensitive()
        => Assert.Equal("en-GB", LanguagePreference.Resolve("EN-gb")!.Name);

    [Fact]
    public void Resolve_returns_null_for_a_real_but_unsupported_culture()
        => Assert.Null(LanguagePreference.Resolve("fr-FR"));

    [Fact]
    public void Resolve_returns_null_for_junk()
        => Assert.Null(LanguagePreference.Resolve("not-a-culture"));
}
