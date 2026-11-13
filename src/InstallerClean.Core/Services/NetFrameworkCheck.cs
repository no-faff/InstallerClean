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
/// Application's constructor. Below 4.8 each says what to install and ends. The setup's copy has its
/// config, which names 4.8, and the setup refuses to install where 4.8 is missing.
///
/// EVERYTHING A COPY RUNS BEFORE THIS CHECK ANSWERS, this check and its message included, uses
/// only what .NET Framework 4.6 has: no ValueTuple, no netstandard type, nothing added after
/// 4.6. Windows 10 1507 ships 4.6, and the portable and the command line check no Windows
/// version of their own. A method naming anything else fails to compile there, and the copy
/// ends without the message. build-and-test's check-framework-references.ps1 walks that path
/// in each merged exe and fails on anything 4.6, 4.6.1, 4.6.2, 4.7, 4.7.1 or 4.7.2 lacks.
/// </remarks>
internal static class NetFrameworkCheck
{
    /// <summary>
    /// The message to show the user before the copy ends, or null where the PC has .NET
    /// Framework 4.8. The command line prints its sentence, which asks the user to run
    /// InstallerClean again, and on the next line the link's name and Microsoft's address
    /// (<c>Cli.UpdateDownloadLink</c>). The window's sentence asks the user to open it
    /// again, and after a blank line comes the link's name and, on a line of its own, the
    /// address.
    /// </summary>
    /// <remarks>
    /// Only a Release value read below 4.8's, or no value at all, refuses: a value that could
    /// not be read or is not a number says nothing about the PC, and the copy starts.
    /// </remarks>
    internal static string? RefusalMessage(bool commandLine) =>
        RefusalMessage(RegistryReader.LocalMachineDword(
            UpdateManifest.NetFrameworkKeyPath, UpdateManifest.NetFrameworkReleaseValueName), commandLine);

    /// <summary>
    /// <see cref="RefusalMessage(bool)"/> for the Release value <paramref name="release"/>.
    /// </summary>
    internal static string? RefusalMessage(RegistryDwordRead release, bool commandLine)
    {
        var below48 = release.State switch
        {
            RegistryDwordState.Read => release.Value < UpdateManifest.NetFramework48Release,
            RegistryDwordState.Absent => true,
            _ => false,
        };
        if (!below48) return null;
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
