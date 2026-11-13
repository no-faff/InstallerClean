using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace InstallerClean.Helpers;

/// <summary>
/// Whether a share's server name is this PC by one of its own names: <c>localhost</c>, a
/// loopback address, the computer name, the host name, the host name with the PC's DNS
/// domain, or one of the PC's own IP addresses. Any other name answers false, an alias of
/// this PC made in a hosts file or in DNS included, so a caller treats false as "not
/// established", never as "another computer".
/// </summary>
internal static class ThisPCName
{
    internal static bool Names(string? server)
    {
        if (string.IsNullOrWhiteSpace(server)) return false;
        var name = server!.Trim().TrimEnd('.');

        if (name.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (name.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)) return true;

        try
        {
            var host = Dns.GetHostName();
            if (name.Equals(host, StringComparison.OrdinalIgnoreCase)) return true;

            var domain = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            if (!string.IsNullOrEmpty(domain)
                && name.Equals(host + "." + domain.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
                return true;
        }
        catch (Exception ex) when (ex is SocketException or NetworkInformationException)
        {
        }

        if (!IPAddress.TryParse(name.Trim('[', ']'), out var address)) return false;
        if (IPAddress.IsLoopback(address)) return true;

        try
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                    if (unicast.Address.Equals(address)) return true;
        }
        catch (NetworkInformationException)
        {
        }

        return false;
    }

    /// <summary>
    /// The server of a share path spelled <c>\\server\share\...</c>, as the kernel's
    /// resolution of a share or a drive mapped onto one spells it; null for any other path.
    /// </summary>
    internal static string? ServerOf(string sharePath)
    {
        if (sharePath.Length < 3
            || !PathText.IsDirectorySeparator(sharePath[0]) || !PathText.IsDirectorySeparator(sharePath[1])
            || sharePath[2] is '?' or '.')
            return null;

        var end = sharePath.IndexOfAny(['\\', '/'], 2);
        var server = end < 0 ? sharePath.Substring(2) : sharePath.Substring(2, end - 2);
        return server.Length == 0 ? null : server;
    }
}
