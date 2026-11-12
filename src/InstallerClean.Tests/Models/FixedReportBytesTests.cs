using System.Reflection;
using System.Text;
using InstallerClean.Services;

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
    [Fact]
    public void The_report_is_written_as_dotnet_10_writes_it()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FixedReport.net10.json")
            ?? throw new InvalidOperationException("FixedReport.net10.json is not embedded.");
        using var reader = new StreamReader(stream, new UTF8Encoding(false));
        var expected = reader.ReadToEnd().Replace("\n", Environment.NewLine);

        var written = ResultLogService.Serialize(FixedReport.Build());

        Assert.Equal(expected, written);
        Assert.Equal(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(written));
    }

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
