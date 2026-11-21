# Fails (exit 1) where a merged exe is not a 64-bit x64 image, read from the two exes the
# OneFile merge writes (publish/window and publish/cli).
#
# Each copy ships as a 64-bit x64 exe, built for x64 by Directory.Build.props. For each merged
# exe the step holds:
#
#  - The PE header names the machine AMD64 and the optional header is PE32+.
#  - The CLI header marks the exe as IL only and asks for neither a 32-bit process
#    (COMIMAGE_FLAGS_32BITREQUIRED) nor a preference for one (COMIMAGE_FLAGS_32BITPREFERRED).
#
# Every run also checks itself against an exe built here as AnyCPU with Prefer32Bit, which
# .NET Framework starts as a 32-bit process on 64-bit Windows: it has to report its machine,
# its header and both flags. A missing exe, or a control reporting fewer than its four faults,
# fails the step.
#
# Run from the repo root after the two merged builds: pwsh scripts/check-architecture.ps1
param([string]$PublishRoot = 'publish')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Reflection.Metadata

$failures = [System.Collections.Generic.List[string]]::new()
function Fail([string]$message) { $failures.Add($message); Write-Host "::error::$message" }

# The faults in the exe at $path that keep it from being a 64-bit x64 image, printing what was
# read.
function Faults([string]$path) {
  $stream = [System.IO.File]::OpenRead($path)
  try {
    $pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
    try {
      $headers = $pe.PEHeaders
      $machine = $headers.CoffHeader.Machine
      $magic = $headers.PEHeader.Magic
      $cor = $headers.CorHeader
      $flags = if ($cor) { $cor.Flags } else { $null }
      Write-Host "  machine $machine, $magic, CLI flags $(if ($cor) { $flags } else { 'none: no CLI header' })"
      $found = @()
      if ($machine -ne [System.Reflection.PortableExecutable.Machine]::Amd64) { $found += "names the machine $machine, not Amd64" }
      if ($magic -ne [System.Reflection.PortableExecutable.PEMagic]::PE32Plus) { $found += "has a $magic header, not PE32Plus" }
      if (-not $cor) { $found += 'has no CLI header' }
      else {
        $corFlags = [System.Reflection.PortableExecutable.CorFlags]
        if (-not ($flags -band $corFlags::ILOnly)) { $found += 'is not marked IL only' }
        if ($flags -band $corFlags::Requires32Bit) { $found += 'asks for a 32-bit process' }
        if ($flags -band $corFlags::Prefers32Bit) { $found += 'prefers a 32-bit process' }
      }
      , $found
    } finally { $pe.Dispose() }
  } finally { $stream.Dispose() }
}

foreach ($exe in (Join-Path $PublishRoot 'window/InstallerClean.exe'), (Join-Path $PublishRoot 'cli/installerclean-cli.exe')) {
  if (-not (Test-Path -LiteralPath $exe)) { Fail "$exe is not there; the two merged builds come first."; continue }
  Write-Host "${exe}:"
  foreach ($fault in (Faults $exe)) { Fail "$exe $fault." }
}

# The control, built outside the repo so Directory.Build.props, which sets x64, does not reach it.
$work = Join-Path ([IO.Path]::GetTempPath()) ('architecture-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
  Set-Content -Path (Join-Path $work 'Prefers32.csproj') -Value @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <PlatformTarget>AnyCPU</PlatformTarget>
    <Prefer32Bit>true</Prefer32Bit>
  </PropertyGroup>
</Project>
'@
  Set-Content -Path (Join-Path $work 'Program.cs') -Value 'static class Program { static void Main() { } }'
  $build = dotnet build (Join-Path $work 'Prefers32.csproj') -c Release --nologo -o (Join-Path $work 'out') 2>&1
  if ($LASTEXITCODE -ne 0) { $build | Write-Host; Fail 'The 32-bit control did not build.' }
  else {
    $control = Join-Path $work 'out/Prefers32.exe'
    if (-not (Test-Path -LiteralPath $control)) { Fail "The 32-bit control's build wrote no $control." }
    else {
      Write-Host 'An AnyCPU exe preferring 32-bit, built here as the control:'
      $faults = Faults $control
      if ($faults.Count -lt 4) { Fail "The control reported $($faults.Count) of its four faults: $($faults -join '; ')" }
      else { Write-Host "  reported, as it has to be: $($faults -join '; ')" }
    }
  }
} finally {
  Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count) {
  Write-Host "$($failures.Count) problem(s)."
  exit 1
}
Write-Host 'Both merged exes are 64-bit x64 images that ask for no 32-bit process.'
