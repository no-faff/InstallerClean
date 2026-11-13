using System.Globalization;
using System.Reflection;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Resources;

/// <summary>
/// Strings.FrameworkCheck, the door for the strings of the .NET Framework 4.8 check, hands
/// its values over without substituting the installer-folder token, so none of the keys it
/// serves may carry the token in any language.
/// </summary>
/// <remarks>
/// Each value is compared with the same key read through Get, which does substitute the
/// token: the two agree only where the value carries none, and only where the door reads
/// the key its member is named for.
/// </remarks>
public class StringsFrameworkCheckTests
{
    private static readonly PropertyInfo[] Served =
        typeof(Strings.FrameworkCheck).GetProperties(BindingFlags.Static | BindingFlags.NonPublic);

    public static TheoryData<string> Languages()
    {
        var data = new TheoryData<string>();
        foreach (var culture in SupportedLanguages.CultureNames) data.Add(culture);
        return data;
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void No_key_the_door_serves_carries_the_installer_folder_token(string culture)
    {
        var before = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Assert.NotEmpty(Served);
            foreach (var member in Served)
            {
                var key = member.Name.Replace('_', '.');
                var value = (string)member.GetValue(null)!;

                Assert.NotEqual(key, value);
                Assert.DoesNotContain(InstallerFolderToken.Token, value, StringComparison.Ordinal);
                Assert.Equal(Strings.Get(key), value);
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }
}
