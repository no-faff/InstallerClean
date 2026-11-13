# Fails (exit 1) where the merged command line does not write the report byte for byte as
# src/InstallerClean.Tests/Models/FixedReport.net10.json holds it, which is what .NET 10's
# own System.Text.Json writes for the same report.
#
# FixedReportBytesTests makes that comparison against the Core assembly the tests build. This
# makes it against the merged exe, where ILRepack has rewritten every type's metadata, and the
# report's property order is read from the metadata (ResultLogService.InDeclarationOrder). A
# .NET Framework 4.8 harness built here references publish/cli/installerclean-cli.exe, fills
# the report with the tests' own FixedReport.cs, and writes it through the exe's
# ResultLogService.Serialize, on the .NET Framework the runner has.
#
# Every run also checks itself: the harness compares the same text against the pinned file
# with two of its lines swapped, and has to report that difference. A missing exe or file, a
# harness that does not build, or a control that passes fails the step.
#
# Windows only, since the harness runs on .NET Framework. Run from the repo root after the
# command line's merged build: pwsh scripts/check-report-bytes.ps1
param([string]$PublishRoot = 'publish')

$ErrorActionPreference = 'Stop'

$exe = (Resolve-Path (Join-Path $PublishRoot 'cli/installerclean-cli.exe')).Path
$fixedReport = (Resolve-Path 'src/InstallerClean.Tests/Models/FixedReport.cs').Path
$pinned = (Resolve-Path 'src/InstallerClean.Tests/Models/FixedReport.net10.json').Path

$work = Join-Path ([IO.Path]::GetTempPath()) ('report-bytes-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
  # x64, as the merged exe is AMD64 only and a 32-bit process cannot load it.
  Set-Content -Path (Join-Path $work 'ReportBytes.csproj') -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="installerclean-cli">
      <HintPath>$exe</HintPath>
      <Private>true</Private>
    </Reference>
    <Compile Include="$fixedReport" Link="FixedReport.cs" />
  </ItemGroup>
</Project>
"@
  Set-Content -Path (Join-Path $work 'Program.cs') -Value @'
using System.Reflection;
using System.Text;
using InstallerClean.Models;
using InstallerClean.Tests.Models;

// args[0]: the pinned file. args[1]: "control" to compare against it with two lines swapped.
static class Program
{
    static int Main(string[] args)
    {
        var assembly = typeof(ResultLogEntry).Assembly;
        Console.WriteLine($"ResultLogEntry from {assembly.Location}");
        var serialize = assembly.GetType("InstallerClean.Services.ResultLogService", throwOnError: true)!
            .GetMethod("Serialize", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("ResultLogService.Serialize is not in the exe.");
        var written = (string)serialize.Invoke(null, new object[] { FixedReport.Build() })!;

        var lines = File.ReadAllText(args[0], new UTF8Encoding(false)).Replace("\r\n", "\n").Split('\n');
        if (args.Length > 1 && args[1] == "control")
            (lines[2], lines[3]) = (lines[3], lines[2]);
        var expected = string.Join(Environment.NewLine, lines);

        var writtenBytes = Encoding.UTF8.GetBytes(written);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        Console.WriteLine($"written {writtenBytes.Length} bytes, expected {expectedBytes.Length} bytes");
        if (writtenBytes.SequenceEqual(expectedBytes)) { Console.WriteLine("same bytes"); return 0; }

        var got = written.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var want = expected.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        for (var i = 0; i < Math.Max(got.Length, want.Length); i++)
        {
            var g = i < got.Length ? got[i] : "(end)";
            var w = i < want.Length ? want[i] : "(end)";
            if (g == w) continue;
            Console.WriteLine($"first difference at line {i + 1}:");
            Console.WriteLine($"  written:  {g}");
            Console.WriteLine($"  expected: {w}");
            break;
        }
        return 1;
    }
}
'@
  $build = dotnet build (Join-Path $work 'ReportBytes.csproj') -c Release --nologo -o (Join-Path $work 'out') 2>&1
  if ($LASTEXITCODE -ne 0) { $build | Write-Host; Write-Host '::error::The report harness did not build.'; exit 1 }
  $harness = Join-Path $work 'out/ReportBytes.exe'

  Write-Host 'The report through the merged command line, against the pinned bytes:'
  & $harness $pinned
  $real = $LASTEXITCODE
  Write-Host 'The control, against the pinned bytes with two lines swapped:'
  & $harness $pinned control
  $control = $LASTEXITCODE

  $failed = $false
  if ($real -ne 0) { Write-Host "::error::The merged command line does not write the report as .NET 10 does (harness exit $real)."; $failed = $true }
  if ($control -ne 1) { Write-Host "::error::The control was not reported (harness exit $control), so the comparison cannot be trusted."; $failed = $true }
  if ($failed) { exit 1 }
  Write-Host 'The merged command line writes the report byte for byte as .NET 10 does.'
} finally {
  Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
