using System.Reflection;
using System.Text;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Models;

/// <summary>
/// The report is written byte for byte as .NET 10 writes it. FixedReport.net10.json is
/// what .NET 10's own System.Text.Json gives for <see cref="FixedReport.Build"/> with the
/// options <see cref="ResultLogService"/> serialises with, its lines ended with a line
/// feed; both runtimes end each line with the platform's newline, so the comparison is
/// made with that newline in place.
/// </summary>
public class FixedReportBytesTests
{
    private static string Expected()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FixedReport.net10.json")
            ?? throw new InvalidOperationException("FixedReport.net10.json is not embedded.");
        using var reader = new StreamReader(stream, new UTF8Encoding(false));
        return reader.ReadToEnd().Replace("\n", Environment.NewLine);
    }

    [Fact]
    public void The_report_is_written_as_dotnet_10_writes_it()
    {
        var expected = Expected();

        var written = ResultLogService.Serialize(FixedReport.Build());

        Assert.Equal(expected, written);
        Assert.Equal(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(written));
    }

#if NETFRAMEWORK
    // The order holds in a domain whose reflection has reached a property of the report
    // by name before anything is written, which on .NET Framework can put that property
    // first in Type.GetProperties.
    [Fact]
    public void The_order_holds_after_a_property_is_reached_by_name_first()
    {
        var (_, shipped) = InFreshDomain();

        Assert.Equal(Expected(), shipped);
    }

    // The control: the same domain's own serialiser, with no order of its own, writes a
    // different order, which is what the order the report is written in is fixed against.
    [Fact]
    public void Without_the_order_a_property_reached_by_name_first_moves()
    {
        var (plain, _) = InFreshDomain();

        Assert.NotEqual(Expected(), plain);
    }

    private static (string Plain, string Shipped) InFreshDomain()
    {
        var setup = new AppDomainSetup
        {
            ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
            TargetFrameworkName = FrameworkSwitch.NetFramework48,
            ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
        };
        var domain = AppDomain.CreateDomain("report order", null, setup);
        try
        {
            var probe = (OrderProbe)domain.CreateInstanceAndUnwrap(
                typeof(OrderProbe).Assembly.FullName!, typeof(OrderProbe).FullName!);
            return (probe.Plain(), probe.Shipped());
        }
        finally
        {
            try { AppDomain.Unload(domain); }
            catch (CannotUnloadAppDomainException) { }
        }
    }

    /// <summary>Runs in the fresh domain: one property reached by name, then the writes.</summary>
    public sealed class OrderProbe : MarshalByRefObject
    {
        public OrderProbe() =>
            _ = typeof(ScanInfo).GetProperty(nameof(ScanInfo.WithheldIdentityUnestablishedCount));

        public string Plain() => System.Text.Json.JsonSerializer.Serialize(FixedReport.Build(),
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            });

        public string Shipped() => ResultLogService.Serialize(FixedReport.Build());
    }
#endif

    // The values the comparison above exists for are in the fixture: the error list,
    // the largest int, a null and a character outside the Basic Multilingual Plane.
    [Fact]
    public void The_fixed_report_holds_a_list_an_extreme_a_null_and_a_surrogate_pair()
    {
        var written = ResultLogService.Serialize(FixedReport.Build());

        Assert.Contains("\"errors\": [", written);
        Assert.Contains("2147483647", written);
        Assert.Contains("null", written);
        Assert.Contains("\\uD83D\\uDE00", written);
    }
}
