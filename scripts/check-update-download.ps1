# Fails (exit 1) where the merged command line cannot download a real release asset through
# its own UpdateDownloadService, on the .NET Framework the runner has.
#
# The copy's update downloads the newer version's exe from the GitHub release with
# UpdateDownloadService. A .NET Framework 4.8 harness built here references
# publish/cli/installerclean-cli.exe and calls the exe's UpdateDownloadService.DownloadAsync for
# the v3.7.0 release's installerclean-cli.exe, following GitHub's redirect to its download host
# as the update does. The file it writes has to start with MZ and be the size GitHub's API gives
# for the asset.
#
# Every run also checks itself: the harness downloads the same release's update.json, which is
# not an exe, and the exe's UpdateDownloadService has to refuse it as not a Windows program, as
# it refuses any body that does not start with MZ. A missing exe, a harness that does not
# build, an API answer without the asset, or a control the service saves or fails for any other
# reason fails the step.
#
# A request GitHub does not answer is tried again, three times in all, a minute apart: the API
# call, and each download that fails before the service has judged the body.
#
# Windows only, since the harness runs on .NET Framework. Run from the repo root after the
# command line's merged build: pwsh scripts/check-update-download.ps1
param([string]$PublishRoot = 'publish')

$ErrorActionPreference = 'Stop'

$exe = (Resolve-Path (Join-Path $PublishRoot 'cli/installerclean-cli.exe')).Path
$release = 'https://api.github.com/repos/no-faff/InstallerClean/releases/tags/v3.7.0'
$headers = @{ 'User-Agent' = 'InstallerClean-CI' }
if ($env:GH_TOKEN) { $headers['Authorization'] = "Bearer $env:GH_TOKEN" }
$tries = 3
$assets = $null
for ($try = 1; -not $assets; $try++) {
  try {
    $assets = (Invoke-RestMethod -Uri $release -Headers $headers).assets
  } catch {
    Write-Host "The release's API answer, try $try of ${tries}: $($_.Exception.Message)"
    if ($try -ge $tries) { Write-Host "::error::$release did not answer."; exit 1 }
    Start-Sleep -Seconds 60
  }
}
$exeAsset = $assets | Where-Object name -eq 'installerclean-cli.exe'
$jsonAsset = $assets | Where-Object name -eq 'update.json'
if (-not $exeAsset -or -not $jsonAsset) { Write-Host "::error::$release does not list installerclean-cli.exe and update.json."; exit 1 }
Write-Host "GitHub lists $($exeAsset.browser_download_url), $($exeAsset.size) bytes, and $($jsonAsset.browser_download_url)."

$work = Join-Path ([IO.Path]::GetTempPath()) ('update-download-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
  # x64, as the merged exe is AMD64 only and a 32-bit process cannot load it.
  Set-Content -Path (Join-Path $work 'UpdateDownload.csproj') -Value @"
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
  </ItemGroup>
</Project>
"@
  Set-Content -Path (Join-Path $work 'Program.cs') -Value @'
using InstallerClean.Services;

// args[0]: the address. args[1]: where to write it. args[2]: the size it has to be, or -1.
// Exit 0 where the file starts with MZ and has that size, 1 where it does not, 3 where the
// service refused the body as not a Windows program, 2 where the download failed otherwise.
static class Program
{
    static int Main(string[] args)
    {
        var service = new UpdateDownloadService();
        Console.WriteLine($"UpdateDownloadService from {typeof(UpdateDownloadService).Assembly.Location}");
        try
        {
            service.DownloadAsync(new Uri(args[0]), args[1], null, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"the download failed: {ex.GetType().FullName}: {ex.Message}");
            return ex is UpdateFailedException { Message: "The download is not a Windows program." } ? 3 : 2;
        }
        var bytes = File.ReadAllBytes(args[1]);
        var head = bytes.Length >= 2 ? $"{bytes[0]:X2} {bytes[1]:X2}" : "(fewer than two bytes)";
        Console.WriteLine($"wrote {bytes.Length} bytes, starting {head}");
        var size = long.Parse(args[2]);
        var mz = bytes.Length >= 2 && bytes[0] == (byte)'M' && bytes[1] == (byte)'Z';
        return mz && (size < 0 || bytes.Length == size) ? 0 : 1;
    }
}
'@
  $build = dotnet build (Join-Path $work 'UpdateDownload.csproj') -c Release --nologo -o (Join-Path $work 'out') 2>&1
  if ($LASTEXITCODE -ne 0) { $build | Write-Host; Write-Host '::error::The download harness did not build.'; exit 1 }
  $harness = Join-Path $work 'out/UpdateDownload.exe'

  # The harness's exit code, running it again while the download fails before the service
  # has judged the body (exit 2).
  function Download([string]$address, [string]$path, [long]$size) {
    for ($try = 1; ; $try++) {
      & $harness $address $path $size | Out-Host
      if ($LASTEXITCODE -ne 2 -or $try -ge $tries) { return $LASTEXITCODE }
      Write-Host "try $try of $tries failed; trying again in a minute"
      Start-Sleep -Seconds 60
    }
  }

  Write-Host 'The v3.7.0 command line, through the merged exe:'
  $real = Download $exeAsset.browser_download_url (Join-Path $work 'installerclean-cli.exe') $exeAsset.size
  Write-Host 'The control, the v3.7.0 update.json, which is not an exe and has to be refused:'
  $control = Download $jsonAsset.browser_download_url (Join-Path $work 'update.json') -1
  $controlSaved = Test-Path -LiteralPath (Join-Path $work 'update.json')

  $failed = $false
  if ($real -ne 0) { Write-Host "::error::The merged command line did not download the v3.7.0 exe whole (harness exit $real)."; $failed = $true }
  if ($control -ne 3 -or $controlSaved) { Write-Host "::error::The merged exe did not refuse update.json (harness exit $control, file left: $controlSaved), so the check cannot be trusted."; $failed = $true }
  if ($failed) { exit 1 }
  Write-Host 'The merged command line downloads a release exe whole.'
} finally {
  Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}

# The control's own exit code is the last one the script saw, and the runner ends a pwsh
# step with that code, so a check that passes says so explicitly.
exit 0
