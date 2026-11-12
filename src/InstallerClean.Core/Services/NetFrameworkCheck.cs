using InstallerClean.Resources;

namespace InstallerClean.Services;

/// <summary>
/// Whether this PC has .NET Framework 4.8, read where a copy starts, and the message a copy
/// shows before it ends where it does not.
/// </summary>
/// <remarks>
/// The portable and command-line downloads ship no config naming the runtime, so .NET
/// Framework starts them on whatever 4.x the PC has. Each reads the Release value .NET
/// Framework 4.8 writes (<see cref="UpdateManifest.HasNetFramework48"/>) as the first thing
/// its start does, and below 4.8 says what to install and ends. The setup's copy has its
/// config, which names 4.8, and the setup refuses to install where 4.8 is missing.
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
    internal static string? RefusalMessage(IRegistryReader registry, bool commandLine)
    {
        if (UpdateManifest.HasNetFramework48(registry)) return null;
        var url = UpdateManifest.NetFramework48DownloadUrl;
        if (commandLine)
            return Strings.Cli_NeedsNetFramework48 + Environment.NewLine
                + string.Format(Strings.Cli_UpdateDownloadLink, Strings.UpdateNotice_DownloadNetFramework48, url);

        // The link line in the language's own punctuation, broken where the address goes,
        // so the address stands on its own line.
        var name = string.Format(Strings.Cli_UpdateDownloadLink, Strings.UpdateNotice_DownloadNetFramework48, AddressMark);
        name = name.Substring(0, name.IndexOfOrdinal(AddressMark, StringComparison.Ordinal)).TrimEnd();
        return Strings.Startup_NeedsNetFramework48 + Environment.NewLine + Environment.NewLine
            + name + Environment.NewLine + url;
    }

    // Stands in for the address in the link line until the line is cut there. No string the
    // app shows holds it.
    private const string AddressMark = "\u0001";
}
