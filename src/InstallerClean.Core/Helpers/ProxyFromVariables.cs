using System.Net;
using System.Net.Http;

namespace InstallerClean.Helpers;

/// <summary>
/// The proxy .NET 10 takes by default on Windows (dotnet/runtime v10.0.0,
/// <c>SystemProxyInfo.Windows.cs</c>, <c>HttpEnvironmentProxy.cs</c> and
/// <c>HttpEnvironmentProxy.Windows.cs</c>): the one HTTPS_PROXY, HTTP_PROXY or ALL_PROXY
/// names, with NO_PROXY as the hosts that go direct, and where none of the three names
/// one, the Windows proxy settings. .NET Framework's default proxy reads the Windows
/// settings only, so each of the app's handlers is given <see cref="Default"/>.
///
/// HTTP_PROXY is the proxy for an http address and is not read where GATEWAY_INTERFACE is
/// set; HTTPS_PROXY is the proxy for any other; ALL_PROXY stands in for whichever of the
/// two is not set. A value is read as .NET 10 reads it: a value with no scheme is http on
/// port 80 unless it names a port, an https one defaults to 443, and a user and password
/// before an <c>@</c> are the proxy's credentials, <c>:</c> alone being the account's own.
/// A NO_PROXY entry starting with a full stop matches that domain and every host under it,
/// and any other entry matches its host exactly, in any case. The variables are read
/// once, the first time a handler asks.
///
/// An https or SOCKS proxy is read as not set (see <see cref="HttpOnly"/>), which is where
/// this differs from .NET 10: the next variable stands in for it, so an https or SOCKS
/// HTTPS_PROXY lets ALL_PROXY be read for an https address, where .NET 10 reads ALL_PROXY
/// only for a variable that is not set. Where no variable names a usable proxy at all, the
/// handler keeps .NET Framework's own default; where one does for the other scheme only,
/// an address of this scheme goes direct, as .NET 10 sends one whose variables name none.
///
/// A handler is given a proxy only where the variables name one. Left without one,
/// .NET Framework takes its own default, which follows a proxy script or WPAD through the
/// chain of answers they give; a proxy standing in for that default would take only its
/// first answer.
/// </summary>
internal sealed class ProxyFromVariables : IWebProxy
{
    private static readonly Lazy<IWebProxy?> FromProcess =
        new(() => TryCreate(Variable), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// The proxy the variables name, or null where none does, so the handler keeps
    /// .NET Framework's own default.
    /// </summary>
    public static IWebProxy? Default => ForTests ?? FromProcess.Value;

    /// <summary>
    /// The proxy a test has every handler built on its own thread take in place of the one
    /// the variables name, so a handler another thread builds meanwhile is not given it.
    /// Null everywhere else. Nothing in the app sets it.
    /// </summary>
    [ThreadStatic]
    internal static IWebProxy? ForTests;

    /// <summary>
    /// <paramref name="handler"/> with <see cref="Default"/> as its proxy where the
    /// variables name one, and with .NET Framework's own default left in place where they do not.
    /// </summary>
    public static T Carrying<T>(T handler) where T : HttpClientHandler => Carrying(handler, Default);

    /// <summary><paramref name="handler"/> with <paramref name="proxy"/> as its proxy where it is not null.</summary>
    internal static T Carrying<T>(T handler, IWebProxy? proxy) where T : HttpClientHandler
    {
        if (proxy is not null)
            handler.Proxy = proxy;
        return handler;
    }

    private static string? Variable(string name) => Environment.GetEnvironmentVariable(name);

    private readonly Uri? _httpProxy;
    private readonly Uri? _httpsProxy;
    private readonly string[]? _bypass;

    private ProxyFromVariables(Uri? httpProxy, Uri? httpsProxy, string? bypass)
    {
        _httpProxy = httpProxy;
        _httpsProxy = httpsProxy;
        Credentials = ProxyCredentials.TryCreate(httpProxy, httpsProxy);
        _bypass = bypass?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(entry => entry.Trim())
            .Where(entry => entry.Length > 0)
            .ToArray();
    }

    /// <summary>The proxy <paramref name="variable"/> names, read as the class summary says, or null.</summary>
    internal static ProxyFromVariables? TryCreate(Func<string, string?> variable)
    {
        Uri? httpProxy = null;
        if (variable("GATEWAY_INTERFACE") is null)
            httpProxy = HttpOnly(UriFrom(variable("HTTP_PROXY")));

        var httpsProxy = HttpOnly(UriFrom(variable("HTTPS_PROXY")));

        if (httpProxy is null || httpsProxy is null)
        {
            var allProxy = HttpOnly(UriFrom(variable("ALL_PROXY")));
            httpProxy ??= allProxy;
            httpsProxy ??= allProxy;
        }

        return httpProxy is null && httpsProxy is null
            ? null
            : new ProxyFromVariables(httpProxy, httpsProxy, variable("NO_PROXY"));
    }

    /// <summary>
    /// <paramref name="proxy"/> where it is an http proxy, and null for an https or SOCKS
    /// one, which .NET 10 speaks to and .NET Framework refuses with NotSupportedException
    /// before the request leaves. A value naming one is read as not set, so the next
    /// variable stands in for it (the class summary says what follows where none does).
    /// </summary>
    private static Uri? HttpOnly(Uri? proxy) =>
        proxy is not null && string.Equals(proxy.Scheme, "http", StringComparison.OrdinalIgnoreCase) ? proxy : null;

    public ICredentials? Credentials { get; set; }

    public Uri? GetProxy(Uri destination) =>
        string.Equals(destination.Scheme, "http", StringComparison.OrdinalIgnoreCase)
        || string.Equals(destination.Scheme, "ws", StringComparison.OrdinalIgnoreCase)
            ? _httpProxy
            : _httpsProxy;

    public bool IsBypassed(Uri host) => GetProxy(host) is null || InBypassList(host);

    private bool InBypassList(Uri input)
    {
        if (_bypass is null)
            return false;

        foreach (var entry in _bypass)
        {
            if (entry[0] == '.')
            {
                // ".example.com" matches example.com itself and every host under it.
                if (entry.Length - 1 == input.Host.Length
                    && string.Compare(entry, 1, input.Host, 0, input.Host.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
                if (input.Host.EndsWith(entry, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (string.Equals(entry, input.Host, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <paramref name="value"/> as a proxy's address, read as .NET 10's
    /// <c>HttpEnvironmentProxy.GetUriFromString</c> reads it, or null where it is empty or
    /// will not read.
    /// </summary>
    internal static Uri? UriFrom(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var hostIndex = 0;
        var protocol = "http";
        ushort port = 80;
        if (value!.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            hostIndex = 7;
        }
        else if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            hostIndex = 8;
            protocol = "https";
            port = 443;
        }
        else if (value.StartsWith("socks4://", StringComparison.OrdinalIgnoreCase))
        {
            hostIndex = 9;
            protocol = "socks4";
        }
        else if (value.StartsWith("socks5://", StringComparison.OrdinalIgnoreCase))
        {
            hostIndex = 9;
            protocol = "socks5";
        }
        else if (value.StartsWith("socks4a://", StringComparison.OrdinalIgnoreCase))
        {
            hostIndex = 10;
            protocol = "socks4a";
        }

        if (hostIndex > 0)
            value = value.Substring(hostIndex);

        string? user = null;
        string? password = null;
        var separator = value.LastIndexOf('@');
        if (separator != -1)
        {
            var auth = Uri.UnescapeDataString(value.Substring(0, separator));
            value = value.Substring(separator + 1);
            separator = auth.IndexOf(':');
            if (separator == -1)
            {
                user = auth;
            }
            else
            {
                user = auth.Substring(0, separator);
                password = auth.Substring(separator + 1);
            }
        }

        string host;
        var ipv6End = value.IndexOf(']');
        separator = value.LastIndexOf(':');
        if (separator == -1 || (ipv6End != -1 && separator < ipv6End))
        {
            host = value;
        }
        else
        {
            host = value.Substring(0, separator);
            var end = separator + 1;
            while (end < value.Length && char.IsDigit(value[end]))
                end++;
            if (!ushort.TryParse(value.Substring(separator + 1, end - separator - 1), System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out port))
                return null;
        }

        try
        {
            var builder = new UriBuilder(protocol, host, port);
            if (user is not null)
                builder.UserName = Uri.EscapeDataString(user);
            if (password is not null)
                builder.Password = Uri.EscapeDataString(password);

            var uri = builder.Uri;
            // A user and a password both empty is ":" before the @, which stands for the
            // account's own credentials; UriBuilder drops it, so it is put back.
            if (user == "" && password == "")
            {
                var parts = uri.ToString().Split(new[] { '/' }, 3);
                if (parts.Length == 3)
                    uri = new Uri($"{parts[0]}//:@{parts[2]}");
            }

            return uri;
        }
        catch (Exception)
        {
            // As in .NET 10, any value the builder refuses names no proxy.
            return null;
        }
    }

    /// <summary>
    /// The credentials the proxies' addresses carry, as .NET 10's
    /// <c>HttpEnvironmentProxyCredentials</c> gives them: each proxy's own user and
    /// password, and the account's own where the address carries <c>:</c> alone.
    /// </summary>
    private sealed class ProxyCredentials : ICredentials
    {
        private readonly Uri? _httpProxy;
        private readonly NetworkCredential? _httpCredential;
        private readonly Uri? _httpsProxy;
        private readonly NetworkCredential? _httpsCredential;

        private ProxyCredentials(Uri? httpProxy, NetworkCredential? httpCredential, Uri? httpsProxy, NetworkCredential? httpsCredential)
        {
            _httpProxy = httpProxy;
            _httpCredential = httpCredential;
            _httpsProxy = httpsProxy;
            _httpsCredential = httpsCredential;
        }

        public NetworkCredential? GetCredential(Uri? uri, string authType)
        {
            if (uri is null)
                return null;
            return uri.Equals(_httpProxy) ? _httpCredential
                : uri.Equals(_httpsProxy) ? _httpsCredential
                : null;
        }

        public static ProxyCredentials? TryCreate(Uri? httpProxy, Uri? httpsProxy)
        {
            var httpCredential = httpProxy is null ? null : FromUserInfo(httpProxy.UserInfo);
            var httpsCredential = httpsProxy is null ? null : FromUserInfo(httpsProxy.UserInfo);
            return httpCredential is null && httpsCredential is null
                ? null
                : new ProxyCredentials(httpProxy, httpCredential, httpsProxy, httpsCredential);
        }

        private static NetworkCredential? FromUserInfo(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            if (value == ":")
                return CredentialCache.DefaultNetworkCredentials;

            value = Uri.UnescapeDataString(value!);
            var password = "";
            string? domain = null;
            var at = value.IndexOf(':');
            if (at != -1)
            {
                password = value.Substring(at + 1);
                value = value.Substring(0, at);
            }

            at = value.IndexOf('\\');
            if (at != -1)
            {
                domain = value.Substring(0, at);
                value = value.Substring(at + 1);
            }

            return new NetworkCredential(value, password, domain);
        }
    }
}
