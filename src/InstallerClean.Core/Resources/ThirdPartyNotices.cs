using System.Text;

namespace InstallerClean.Resources;

/// <summary>
/// The text of THIRD-PARTY-NOTICES at the root of the repository: the notices and licences
/// of the third-party work inside the app. The build embeds the file in this assembly
/// unchanged, so every copy carries the text: the window shows it from its About window and
/// the command line prints it on its notices switch. The OneFile merge copies the resource
/// into each exe with the rest of this assembly.
/// </summary>
internal static class ThirdPartyNotices
{
    /// <summary>
    /// The resource's name, set as its LogicalName in InstallerClean.Core.csproj.
    /// scripts/check-notices.ps1 reads each merged exe's resource by this name.
    /// </summary>
    internal const string ResourceName = "InstallerClean.THIRD-PARTY-NOTICES";

    /// <summary>
    /// The notices, decoded as UTF-8. A byte that is not UTF-8 throws rather than reading as
    /// a replacement character, so a file saved in another encoding cannot show a mangled
    /// copyright line. Line breaks are as the file has them, a bare LF.
    /// </summary>
    internal static string Read()
    {
        using var stream = typeof(ThirdPartyNotices).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"{typeof(ThirdPartyNotices).Assembly.GetName().Name} holds no resource named {ResourceName}.");
        using var reader = new StreamReader(stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: false);
        return reader.ReadToEnd();
    }
}
