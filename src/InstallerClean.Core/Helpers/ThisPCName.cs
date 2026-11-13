using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace InstallerClean.Helpers;

/// <summary>
/// Whether a share's server name is this PC by one of its own names: <c>localhost</c>,
/// <c>127.0.0.1</c> or <c>::1</c>, the computer name, the host name alone or with one of the
/// PC's own DNS suffixes or <c>.local</c>, or one of the PC's own IP addresses. Any other
/// name answers false, an alias of this PC made in a hosts file or in DNS included, so a
/// caller treats false as "not established", never as "another computer".
/// </summary>
internal static class ThisPCName
{
    private const string Ipv6LiteralSuffix = ".ipv6-literal.net";

    internal static bool Names(string? server)
    {
        if (string.IsNullOrWhiteSpace(server)) return false;
        var name = server!.Trim().TrimEnd('.');

        if (name.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (name.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)) return true;
        if (NamesThisHost(name)) return true;

        var address = AddressIn(name);
        if (address is null) return false;

        // 127.0.0.1 and ::1 alone: another loopback address can be forwarded to another
        // computer, as a tunnel does.
        if (address.Equals(IPAddress.Loopback) || address.Equals(IPAddress.IPv6Loopback)) return true;

        try
        {
            var bytes = address.GetAddressBytes();
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                    if (unicast.Address.AddressFamily == address.AddressFamily
                        && Enumerable.SequenceEqual(unicast.Address.GetAddressBytes(), bytes))
                        return true;
        }
        catch (NetworkInformationException)
        {
        }

        return false;
    }

    /// <summary>
    /// Whether <paramref name="name"/> is this PC's host name, alone or with its primary DNS
    /// suffix, a suffix one of its network adapters has, or <c>.local</c>.
    /// </summary>
    private static bool NamesThisHost(string name)
    {
        try
        {
            var host = Dns.GetHostName();
            if (name.Equals(host, StringComparison.OrdinalIgnoreCase)) return true;
            if (!name.StartsWith(host + ".", StringComparison.OrdinalIgnoreCase)) return false;

            var suffix = name.Substring(host.Length + 1);
            if (suffix.Equals("local", StringComparison.OrdinalIgnoreCase)) return true;
            if (suffix.Equals(IPGlobalProperties.GetIPGlobalProperties().DomainName.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                if (suffix.Equals(adapter.GetIPProperties().DnsSuffix?.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
                    return true;
        }
        catch (Exception ex) when (ex is SocketException or NetworkInformationException)
        {
        }

        return false;
    }

    /// <summary>
    /// The IP address <paramref name="name"/> spells: a dotted IPv4 address, or an IPv6
    /// address as a share path writes one, <c>fe80--1s4.ipv6-literal.net</c>, its colons
    /// as dashes and its zone after an <c>s</c>. Null where it spells none.
    /// </summary>
    private static IPAddress? AddressIn(string name)
    {
        if (name.EndsWith(Ipv6LiteralSuffix, StringComparison.OrdinalIgnoreCase))
        {
            var literal = name.Substring(0, name.Length - Ipv6LiteralSuffix.Length).Replace('-', ':');
            var zone = literal.IndexOf('s');
            if (zone >= 0) literal = literal.Substring(0, zone);
            return IPAddress.TryParse(literal, out var v6) && v6.AddressFamily == AddressFamily.InterNetworkV6 ? v6 : null;
        }

        return IPAddress.TryParse(name, out var address) && address.AddressFamily == AddressFamily.InterNetwork
            ? address
            : null;
    }

    /// <summary>
    /// The server of a share path spelled <c>\\server\share\...</c>, as the kernel's
    /// resolution of a share or a drive mapped onto one spells it; null for any other path,
    /// <c>\\?\</c> and <c>\\.\</c> paths included.
    /// </summary>
    internal static string? ServerOf(string sharePath)
    {
        if (sharePath.Length < 3
            || !PathText.IsDirectorySeparator(sharePath[0]) || !PathText.IsDirectorySeparator(sharePath[1])
            || sharePath.StartsWith(@"\\?\", StringComparison.Ordinal)
            || sharePath.StartsWith(@"\\.\", StringComparison.Ordinal))
            return null;

        var end = sharePath.IndexOfAny(['\\', '/'], 2);
        var server = end < 0 ? sharePath.Substring(2) : sharePath.Substring(2, end - 2);
        return server.Length == 0 ? null : server;
    }
}
