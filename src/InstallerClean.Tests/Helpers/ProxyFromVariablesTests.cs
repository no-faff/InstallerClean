using System.Net;
using System.Net.Http;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// <see cref="ProxyFromVariables"/> reads HTTP_PROXY, HTTPS_PROXY, ALL_PROXY and NO_PROXY
/// as .NET 10 reads them on Windows. The rows and expectations are those of .NET 10's own
/// tests (dotnet/runtime v10.0.0, <c>HttpEnvironmentProxyTest.cs</c>), bar the https and
/// SOCKS proxies, which .NET Framework cannot use: they are read as not set, and the rows
/// for them, with the next variable standing in, are this class's own.
/// </summary>
public class ProxyFromVariablesTests
{
    private static readonly Uri FooHttp = new("http://foo.com/");
    private static readonly Uri FooHttps = new("https://foo.com/");
    private static readonly Uri FooWs = new("ws://foo.com/");
    private static readonly Uri FooWss = new("wss://foo.com/");

    private static ProxyFromVariables? From(params (string Name, string Value)[] set)
    {
        var values = set.ToDictionary(v => v.Name, v => v.Value, StringComparer.OrdinalIgnoreCase);
        return ProxyFromVariables.TryCreate(name => values.TryGetValue(name, out var value) ? value : null);
    }

    [Fact]
    public void Nothing_set_gives_no_proxy()
    {
        Assert.Null(From());
        Assert.Null(From(("NO_PROXY", "foo.com")));
    }

    [Fact]
    public void Each_scheme_takes_its_own_variable_and_ALL_PROXY_fills_the_other()
    {
        var all = From(("ALL_PROXY", "http://1.1.1.1:3000"))!;
        Assert.Null(all.Credentials);
        foreach (var target in new[] { FooHttp, FooHttps, FooWs, FooWss })
            Assert.Equal("1.1.1.1", all.GetProxy(target)!.Host);

        var both = From(("ALL_PROXY", "http://1.1.1.1:3000"), ("HTTP_PROXY", "http://1.1.1.2:3001"))!;
        Assert.Equal(new Uri("http://1.1.1.2:3001/"), both.GetProxy(FooHttp));
        Assert.Equal(new Uri("http://1.1.1.2:3001/"), both.GetProxy(FooWs));
        Assert.Equal(new Uri("http://1.1.1.1:3000/"), both.GetProxy(FooHttps));
        Assert.Equal(new Uri("http://1.1.1.1:3000/"), both.GetProxy(FooWss));

        // A value that will not read is not set, and ALL_PROXY stands in for it.
        var unreadable = From(("ALL_PROXY", "http://1.1.1.1:3000"), ("HTTP_PROXY", "1.1.1.3:3003"), ("HTTPS_PROXY", "ab!cd"))!;
        Assert.Equal(new Uri("http://1.1.1.3:3003/"), unreadable.GetProxy(FooHttp));
        Assert.Equal(new Uri("http://1.1.1.1:3000/"), unreadable.GetProxy(FooHttps));

        var https = From(("HTTP_PROXY", "1.1.1.3:3003"), ("HTTPS_PROXY", "http://1.1.1.5:3005"))!;
        Assert.Equal(new Uri("http://1.1.1.5:3005/"), https.GetProxy(FooHttps));
    }

    [Fact]
    public void Only_HTTPS_PROXY_set_sends_an_http_address_direct()
    {
        var proxy = From(("HTTPS_PROXY", "http://1.1.1.5:3005"))!;

        Assert.Null(proxy.GetProxy(FooHttp));
        Assert.True(proxy.IsBypassed(FooHttp));
        Assert.False(proxy.IsBypassed(FooHttps));
    }

    [Fact]
    public void HTTP_PROXY_is_not_read_where_GATEWAY_INTERFACE_is_set()
    {
        Assert.Null(From(("HTTP_PROXY", "http://1.1.1.2:3001"), ("GATEWAY_INTERFACE", "CGI/1.1")));
        Assert.Equal(new Uri("http://1.1.1.2:3001/"),
            From(("HTTP_PROXY", "http://1.1.1.2:3001"))!.GetProxy(FooHttp));
    }

    [Theory]
    [InlineData("1.1.1.5", "1.1.1.5", 80, null, null)]
    [InlineData("http://1.1.1.5:3005", "1.1.1.5", 3005, null, null)]
    [InlineData("http://foo@1.1.1.5", "1.1.1.5", 80, "foo", "")]
    [InlineData("http://[::1]:80", "[::1]", 80, null, null)]
    [InlineData("foo:PLACEHOLDER@[::1]:3128", "[::1]", 3128, "foo", "PLACEHOLDER")]
    [InlineData("foo:Pass$!#\\.$@127.0.0.1:3128", "127.0.0.1", 3128, "foo", "Pass$!#\\.$")]
    [InlineData("[::1]", "[::1]", 80, null, null)]
    [InlineData("domain\\foo:PLACEHOLDER@1.1.1.1", "1.1.1.1", 80, "foo", "PLACEHOLDER")]
    [InlineData("domain%5Cfoo:PLACEHOLDER@1.1.1.1", "1.1.1.1", 80, "foo", "PLACEHOLDER")]
    [InlineData("HTTP://ABC.COM/", "abc.com", 80, null, null)]
    [InlineData("http://10.30.62.64:7890/", "10.30.62.64", 7890, null, null)]
    [InlineData("http://1.2.3.4:8888/foo", "1.2.3.4", 8888, null, null)]
    public void A_value_is_read_as_dotnet_10_reads_it(string value, string host, int port, string? user, string? password)
    {
        var proxy = From(("ALL_PROXY", value))!;

        var address = proxy.GetProxy(FooHttp)!;
        Assert.Equal(host, address.Host);
        Assert.Equal(port, address.Port);
        if (user is null)
        {
            Assert.Null(proxy.Credentials);
        }
        else
        {
            var credential = proxy.Credentials!.GetCredential(address, "Basic")!;
            Assert.Equal(user, credential.UserName);
            Assert.Equal(password, credential.Password);
        }
    }

    [Theory]
    [InlineData("socks4://1.2.3.4:8888/foo", "socks4", 8888)]
    [InlineData("socks4a://1.2.3.4:8888/foo", "socks4a", 8888)]
    [InlineData("socks5://1.2.3.4:8888/foo", "socks5", 8888)]
    [InlineData("https://1.1.1.5:3005", "https", 3005)]
    [InlineData("https://1.1.1.5", "https", 443)]
    public void An_https_or_SOCKS_proxy_is_read_and_then_not_used(string value, string scheme, int port)
    {
        var address = ProxyFromVariables.UriFrom(value)!;
        Assert.Equal(scheme, address.Scheme);
        Assert.Equal(port, address.Port);

        Assert.Null(From(("ALL_PROXY", value)));
        // The next variable stands in for it.
        Assert.Equal(new Uri("http://1.1.1.1:3000/"),
            From(("HTTPS_PROXY", value), ("ALL_PROXY", "http://1.1.1.1:3000"))!.GetProxy(FooHttps));
    }

    [Fact]
    public void Each_proxy_answers_for_its_own_credentials_only()
    {
        var proxy = From(("ALL_PROXY", "http://foo:PLACEHOLDER@1.1.1.1:3000"), ("HTTPS_PROXY", "http://foo1:PLACEHOLDER1@1.1.1.1:3001"))!;

        Assert.Equal("foo", proxy.Credentials!.GetCredential(proxy.GetProxy(FooHttp), "Basic")!.UserName);
        Assert.Equal("foo1", proxy.Credentials.GetCredential(proxy.GetProxy(FooHttps), "Basic")!.UserName);
        Assert.Null(proxy.Credentials.GetCredential(FooHttp, "Basic"));
        // The address .NET Framework asks with is its own, built apart from this one.
        Assert.Equal("foo", proxy.Credentials.GetCredential(new Uri("http://1.1.1.1:3000"), "Basic")!.UserName);
        Assert.Null(proxy.Credentials.GetCredential(null, "Basic"));
    }

    [Fact]
    public void A_colon_alone_before_the_at_is_the_accounts_own_credentials()
    {
        var proxy = From(("ALL_PROXY", "http://:@1.1.1.1:3000"))!;

        Assert.Same(CredentialCache.DefaultNetworkCredentials, proxy.Credentials!.GetCredential(proxy.GetProxy(FooHttp), ""));
        Assert.Same(CredentialCache.DefaultNetworkCredentials, proxy.Credentials.GetCredential(proxy.GetProxy(FooHttps), ""));
    }

    [Fact]
    public void NO_PROXY_names_a_host_or_a_domain_and_everything_under_it()
    {
        var proxy = From(("NO_PROXY", ".test.com,, foo.com"), ("ALL_PROXY", "http://foo:PLACEHOLDER@1.1.1.1:3000"))!;

        Assert.True(proxy.IsBypassed(FooHttp));
        Assert.True(proxy.IsBypassed(FooHttps));
        Assert.True(proxy.IsBypassed(new Uri("http://test.com")));
        Assert.False(proxy.IsBypassed(new Uri("http://1test.com")));
        Assert.True(proxy.IsBypassed(new Uri("http://www.test.com")));
        Assert.False(proxy.IsBypassed(new Uri("https://github.com/")));
    }

    [Fact]
    public void A_handler_takes_a_proxy_only_where_the_variables_name_one()
    {
        var proxy = From(("HTTPS_PROXY", "http://1.1.1.5:3005"))!;

        using var given = ProxyFromVariables.Carrying(new HttpClientHandler(), proxy);
        using var left = ProxyFromVariables.Carrying(new HttpClientHandler(), null);

        Assert.Same(proxy, given.Proxy);
        Assert.Null(left.Proxy);
    }

    // The update check's and the download's handler, and the report's, each take the proxy.
    [Fact]
    public void The_update_and_report_handlers_carry_the_proxy()
    {
        var proxy = From(("HTTPS_PROXY", "http://1.1.1.5:3005"))!;
        ProxyFromVariables.ForTests = proxy;
        try
        {
            using var shipping = InstallerClean.Services.UpdateCheckService.CreateShippingHandler();
            using var report = InstallerClean.Services.ResultLogService.CreateHandler();

            Assert.Same(proxy, shipping.Proxy);
            Assert.Same(proxy, report.Proxy);
        }
        finally
        {
            ProxyFromVariables.ForTests = null;
        }
    }
}
