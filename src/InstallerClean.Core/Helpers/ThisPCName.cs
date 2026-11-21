using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using InstallerClean.Interop.Native;
using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// This PC as a share's server: whether a server name is this PC by one of its own names, and
/// where one of this PC's shares is on its own disks.
/// </summary>
internal static class ThisPCName
{
    private const string Ipv6LiteralSuffix = ".ipv6-literal.net";

    /// <summary>
    /// Whether <paramref name="server"/> is this PC by one of its own names: <c>localhost</c>,
    /// <c>127.0.0.1</c>, the IPv6 loopback as a share path writes it (<c>--1.ipv6-literal.net</c>),
    /// the computer name, the host name, or one of this PC's own addresses, written as Windows
    /// writes it, an IPv6 one only where it is not link-local. Any other name answers false: one that
    /// resolves through DNS, mDNS or a hosts file can reach another computer, and so can
    /// another loopback address forwarded by a tunnel. So a caller treats false as "not
    /// established", never as "another computer".
    /// </summary>
    internal static bool Names(string? server)
    {
        if (string.IsNullOrWhiteSpace(server)) return false;
        var name = server!.Trim();

        if (name.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (name == "127.0.0.1") return true;
        if (name.Equals("--1" + Ipv6LiteralSuffix, StringComparison.OrdinalIgnoreCase)
            || name.Equals("0--1" + Ipv6LiteralSuffix, StringComparison.OrdinalIgnoreCase))
            return true;
        if (name.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)) return true;

        try
        {
            if (name.Equals(Dns.GetHostName(), StringComparison.OrdinalIgnoreCase)) return true;
        }
        catch (SocketException)
        {
        }

        return IsOwnAddress(name);
    }

    /// <summary>
    /// Whether <paramref name="name"/> is one of the unicast addresses this PC holds on an
    /// adapter that is up: an IPv4 one as <see cref="IPAddress.ToString"/> writes it, and an
    /// IPv6 one that is not link-local as a share path writes it, its colons as dashes and with
    /// no zone. A link-local IPv6 address
    /// names a different computer on each link, so without its zone it is not this PC's alone.
    /// </summary>
    private static bool IsOwnAddress(string name)
    {
        try
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                // An address counts only where this PC holds it: its adapter up, and the address
                // not found in use by another computer on the link.
                if (adapter.OperationalStatus != OperationalStatus.Up) continue;

                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.DuplicateAddressDetectionState != DuplicateAddressDetectionState.Preferred) continue;
                    var address = unicast.Address;
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (name == address.ToString()) return true;
                    }
                    else if (address.AddressFamily == AddressFamily.InterNetworkV6
                        && !address.IsIPv6LinkLocal && address.ScopeId == 0
                        && name.Equals(address.ToString().Replace(':', '-') + Ipv6LiteralSuffix, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch (NetworkInformationException)
        {
        }

        return false;
    }

    /// <summary>
    /// The local path of <paramref name="share"/>, one of this PC's own shares, as this PC's
    /// server service records it (<c>C$</c> gives <c>C:\</c>); null where the service does not
    /// know the share or will not say, and for a DFS share, whose folders can send a path to
    /// another server.
    /// </summary>
    internal static string? LocalPathOfShare(string share)
    {
        if (!IsPlainShare(share)) return null;

        var buffer = IntPtr.Zero;
        try
        {
            if (Netapi32.NetShareGetInfo(null, share, 2, out buffer) != Netapi32.NERR_Success || buffer == IntPtr.Zero)
                return null;

            var info = (Netapi32.SHARE_INFO_2)Marshal.PtrToStructure(buffer, typeof(Netapi32.SHARE_INFO_2))!;
            var path = info.shi2_path == IntPtr.Zero ? null : Marshal.PtrToStringUni(info.shi2_path);
            return string.IsNullOrEmpty(path) ? null : path;
        }
        finally
        {
            if (buffer != IntPtr.Zero) Netapi32.NetApiBufferFree(buffer);
        }
    }

    /// <summary>
    /// Whether the server service says <paramref name="share"/> is neither a DFS namespace's
    /// root nor in one; false where it will not say.
    /// </summary>
    private static bool IsPlainShare(string share)
    {
        var buffer = IntPtr.Zero;
        try
        {
            if (Netapi32.NetShareGetInfo(null, share, 1005, out buffer) != Netapi32.NERR_Success || buffer == IntPtr.Zero)
                return false;

            var flags = (uint)Marshal.ReadInt32(buffer);
            return (flags & (Netapi32.SHI1005_FLAGS_DFS | Netapi32.SHI1005_FLAGS_DFS_ROOT)) == 0;
        }
        finally
        {
            if (buffer != IntPtr.Zero) Netapi32.NetApiBufferFree(buffer);
        }
    }

    /// <summary>
    /// The server and share of a share path spelled <c>\\server\share\...</c>, and the part
    /// below the share; false for any other path, <c>\\?\</c> and <c>\\.\</c> paths included.
    /// </summary>
    internal static bool TrySplit(string sharePath, out string server, out string share, out string below)
    {
        server = share = below = "";
        if (!InstallerCacheHelpers.IsUncPath(sharePath)) return false;

        var parts = sharePath.Substring(2).Split(['\\', '/'], 3);
        if (parts.Length < 2 || parts[0].Length == 0 || parts[1].Length == 0) return false;

        server = parts[0];
        share = parts[1];
        below = parts.Length == 3 ? parts[2].Trim('\\', '/') : "";
        return true;
    }
}
