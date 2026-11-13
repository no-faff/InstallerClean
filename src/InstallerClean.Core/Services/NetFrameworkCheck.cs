using InstallerClean.Resources;

namespace InstallerClean.Services;

/// <summary>
/// Whether this PC has .NET Framework 4.8, read where a copy starts, and the message a copy
/// shows before it ends where it does not.
/// </summary>
/// <remarks>
/// The portable and command-line downloads ship no config naming the runtime, so .NET
/// Framework starts them on whatever 4.x the PC has. Each reads the Release value .NET
/// Framework 4.8 writes, the value <see cref="UpdateManifest.HasNetFramework48"/> reads, at
/// its start: the command line before it reads its arguments, the window before
/// Application's constructor, each after <see cref="WindowsVersionCheck"/>. Below 4.8 each
/// says what to install and ends. The setup's copy has its config, which names 4.8, and the
/// setup refuses to install where 4.8 is missing.
///
/// EVERYTHING A COPY RUNS BEFORE ITS START CHECKS ANSWER, these checks and their messages
/// included, uses only what .NET Framework 4.0 has: no ValueTuple, no netstandard type, no
/// AppContext, no TypeInfo, nothing added after 4.0. Windows 8 ships 4.5, Windows 8.1 ships
/// 4.5.1 and Windows 10 1507 ships 4.6, Windows 7 has whichever 4.x was installed on it, and
/// the Windows check that refuses them runs on that .NET Framework. A method naming anything
/// else fails to compile there, and the copy ends without a message. So the checks' strings
/// come through Strings.FrameworkCheck, and the command line's refusals record nothing in
/// crash.log. build-and-test's check-framework-references.ps1 walks that path in each merged
/// exe and fails on anything 4.0, 4.5, 4.5.1, 4.5.2, 4.6, 4.6.1, 4.6.2, 4.7, 4.7.1 or 4.7.2
/// lacks.
/// </remarks>
internal static class NetFrameworkCheck
{
    /// <summary>The Release value .NET Framework 4.8 writes, as this PC holds it.</summary>
    internal static RegistryDwordRead ReleaseOfThisPc() =>
        RegistryReader.LocalMachineDword(UpdateManifest.NetFrameworkKeyPath, UpdateManifest.NetFrameworkReleaseValueName);

    /// <summary>
    /// Whether the Release value <paramref name="release"/> says the PC does not have .NET
    /// Framework 4.8.
    /// </summary>
    /// <remarks>
    /// Only a Release value read below 4.8's, or no value at all, refuses: a value that could
    /// not be read or is not a number says nothing about the PC, and the copy starts.
    /// </remarks>
    internal static bool Refuses(RegistryDwordRead release) => release.State switch
    {
        RegistryDwordState.Read => release.Value < UpdateManifest.NetFramework48Release,
        RegistryDwordState.Absent => true,
        _ => false,
    };

    /// <summary>
    /// The message to show the user before the copy ends, in the language the strings are read
    /// in. The command line prints its sentence, which asks the user to run InstallerClean
    /// again, and on the next line the link's name and Microsoft's address
    /// (<c>Cli.UpdateDownloadLink</c>). The window's sentence asks the user to open it again,
    /// and after a blank line comes the link's name and, on a line of its own, the address.
    /// </summary>
    internal static string Message(bool commandLine)
    {
        var url = UpdateManifest.NetFramework48DownloadUrl;
        if (commandLine)
            return Strings.FrameworkCheck.Cli_NeedsNetFramework48 + Environment.NewLine
                + string.Format(Strings.FrameworkCheck.Cli_UpdateDownloadLink,
                    Strings.FrameworkCheck.UpdateNotice_DownloadNetFramework48, url);

        // The link line in the language's own punctuation with no address in it, which then
        // stands on its own line. Every language's link line ends with the address.
        var name = string.Format(Strings.FrameworkCheck.Cli_UpdateDownloadLink,
            Strings.FrameworkCheck.UpdateNotice_DownloadNetFramework48, "").TrimEnd(null);
        return Strings.FrameworkCheck.Startup_NeedsNetFramework48 + Environment.NewLine
            + Environment.NewLine + name + Environment.NewLine + url;
    }
}
