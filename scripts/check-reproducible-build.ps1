# Fails (exit 1) where a second build of this commit does not give the same bytes as the
# first, for the two merged exes the OneFile merge writes (publish/window and publish/cli),
# the .exe.config it writes beside each, and the three assemblies merged into them
# (InstallerClean.Core.dll and each project's own exe). The release notes tell a reader that
# building a tag on the same SDK with the same flags gives the same bytes and the same hashes,
# and a push that breaks that fails here.
#
# The first build is whatever the steps before this one built. The script copies it aside,
# shuts the build server down, and builds both projects again with --no-incremental and
# UseSharedCompilation=false, so every project is compiled again, each by a new compiler
# process. A compiler process seeds .NET's string hashing afresh, so anything in the build
# that emits in hash order (a source generator enumerating a dictionary, for one) can come
# out in a different order here than in the first build, where one long-lived server may
# have compiled everything with one seed.
#
# Where a pair differs, the step prints both SHA-256 values, how many bytes differ, and, for
# an assembly, its type table read from both: the same types in a different order, with each
# place the order parts, or the types only one side defines. A second build that did not
# rewrite a file, a file missing on either side, or a comparison that cannot tell two files
# apart (its control is a copy of the first Core assembly with one byte changed) fails the
# step.
#
# With -Setup it checks the setup instead, run after the setup has been compiled once through
# scripts/compile-setup.ps1. It copies that setup aside, sets every file the [Files] section of
# installer/InstallerClean.iss packs to a last-write time a day earlier, as another build of the
# same commit would leave them, compiles the setup again into a folder of its own and compares
# the two byte for byte. The control is a third compile with the commit time a day later, which
# has to come out different. A setup missing from either compile, a [Files] source that is not
# there, or a control that comes out the same fails the step.
#
# Run from the repo root after the two merged builds: pwsh scripts/check-reproducible-build.ps1
# and after the setup is compiled: pwsh scripts/check-reproducible-build.ps1 -Setup
param([string]$PublishRoot = 'publish', [switch]$Setup)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Reflection.Metadata
Add-Type -TypeDefinition @'
public static class ByteCount
{
    // How many places in the shorter length hold a different byte.
    public static int Differing(byte[] a, byte[] b)
    {
        int count = 0;
        int length = System.Math.Min(a.Length, b.Length);
        for (int i = 0; i < length; i++) { if (a[i] != b[i]) count++; }
        return count;
    }

    // Where the bytes differ, as up to max runs of "offset+length" in hex, offsets into the
    // shorter length.
    public static string Runs(byte[] a, byte[] b, int max)
    {
        var runs = new System.Collections.Generic.List<string>();
        int length = System.Math.Min(a.Length, b.Length);
        int i = 0;
        while (i < length && runs.Count < max)
        {
            if (a[i] == b[i]) { i++; continue; }
            int start = i;
            while (i < length && a[i] != b[i]) i++;
            runs.Add(string.Format("0x{0:x}+{1}", start, i - start));
        }
        return string.Join(", ", runs);
    }
}
'@

$failures = [System.Collections.Generic.List[string]]::new()
function Fail([string]$message) { $failures.Add($message); Write-Host "::error::$message" }

# The project's own Release build of $file: under bin/Release/net48/win-x64 where the SDK
# infers the win-x64 runtime identifier, as it does on Windows, and under bin/Release/net48
# where it infers none. Where both are there, the one written last is the build's.
function Built-File([string]$project, [string]$file) {
  $found = @("src/$project/bin/Release/net48/win-x64/$file", "src/$project/bin/Release/net48/$file") |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    ForEach-Object { Get-Item -LiteralPath $_ } |
    Sort-Object LastWriteTimeUtc -Descending
  if ($found) { $found[0].FullName } else { $null }
}

# Every file compared, by the label printed for it, with where the build leaves it, and
# whether it is an assembly with a type table to read. The merged exes and the .exe.config the
# merge writes beside each are read from the second build's own publish folder, given to it
# as OneFileDir.
$second = Join-Path ([System.IO.Path]::GetTempPath()) "reproducible-build-$PID"
$copies = Join-Path ([System.IO.Path]::GetTempPath()) "reproducible-build-first-$PID"
$files = @(
  [pscustomobject]@{ Label = 'merged window'; Assembly = $true; First = "$PublishRoot/window/InstallerClean.exe"; Second = { "$second/publish/window/InstallerClean.exe" } }
  [pscustomobject]@{ Label = 'merged window config'; Assembly = $false; First = "$PublishRoot/window/InstallerClean.exe.config"; Second = { "$second/publish/window/InstallerClean.exe.config" } }
  [pscustomobject]@{ Label = 'merged command line'; Assembly = $true; First = "$PublishRoot/cli/installerclean-cli.exe"; Second = { "$second/publish/cli/installerclean-cli.exe" } }
  [pscustomobject]@{ Label = 'merged command line config'; Assembly = $false; First = "$PublishRoot/cli/installerclean-cli.exe.config"; Second = { "$second/publish/cli/installerclean-cli.exe.config" } }
  [pscustomobject]@{ Label = 'InstallerClean.Core.dll'; Assembly = $true; First = (Built-File 'InstallerClean.Core' 'InstallerClean.Core.dll'); Second = { Built-File 'InstallerClean.Core' 'InstallerClean.Core.dll' } }
  [pscustomobject]@{ Label = 'InstallerClean.exe before the merge'; Assembly = $true; First = (Built-File 'InstallerClean' 'InstallerClean.exe'); Second = { Built-File 'InstallerClean' 'InstallerClean.exe' } }
  [pscustomobject]@{ Label = 'installerclean-cli.exe before the merge'; Assembly = $true; First = (Built-File 'InstallerClean.Cli' 'installerclean-cli.exe'); Second = { Built-File 'InstallerClean.Cli' 'installerclean-cli.exe' } }
)

# The assembly at $path's MVID and the full name of every type it defines, in the order of
# its TypeDef table, which is the order the compiler emitted them.
function Read-Types([string]$path) {
  $stream = [System.IO.File]::OpenRead($path)
  try {
    $pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
    try {
      $md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
      $names = foreach ($handle in $md.TypeDefinitions) {
        $type = $md.GetTypeDefinition($handle)
        $name = $md.GetString($type.Name)
        $namespace = $md.GetString($type.Namespace)
        if ($namespace) { "$namespace.$name" } else { $name }
      }
      [pscustomobject]@{ Mvid = $md.GetGuid($md.GetModuleDefinition().Mvid); Types = @($names) }
    } finally { $pe.Dispose() }
  } finally { $stream.Dispose() }
}

# Whether the two files hold the same bytes, printing both hashes and, where they differ,
# what differs: the byte count and, for an assembly, the MVIDs and the type tables.
function Same-Bytes([string]$label, [string]$first, [string]$secondPath, [bool]$assembly) {
  $a = [System.IO.File]::ReadAllBytes($first)
  $b = [System.IO.File]::ReadAllBytes($secondPath)
  $hashA = (Get-FileHash -LiteralPath $first -Algorithm SHA256).Hash
  $hashB = (Get-FileHash -LiteralPath $secondPath -Algorithm SHA256).Hash
  if ($hashA -eq $hashB -and $a.Length -eq $b.Length -and [System.Linq.Enumerable]::SequenceEqual($a, $b)) {
    Write-Host "  same: $label, $($a.Length) bytes, SHA-256 $hashA"
    return $true
  }
  $differing = [ByteCount]::Differing($a, $b)
  Write-Host "  DIFFERENT: $label"
  Write-Host "    first:  $($a.Length) bytes, SHA-256 $hashA"
  Write-Host "    second: $($b.Length) bytes, SHA-256 $hashB"
  Write-Host "    $differing of the first $([Math]::Min($a.Length, $b.Length)) bytes differ"
  if ($differing) { Write-Host "    where they differ (the first ten runs): $([ByteCount]::Runs($a, $b, 10))" }
  if (-not $assembly) { return $false }
  try {
    $typesA = Read-Types $first
    $typesB = Read-Types $secondPath
    Write-Host "    MVID: first $($typesA.Mvid), second $($typesB.Mvid)"
    $onlyA = @($typesA.Types | Where-Object { $typesB.Types -notcontains $_ })
    $onlyB = @($typesB.Types | Where-Object { $typesA.Types -notcontains $_ })
    if ($onlyA.Count -or $onlyB.Count) {
      Write-Host "    types only the first defines ($($onlyA.Count)): $($onlyA -join ', ')"
      Write-Host "    types only the second defines ($($onlyB.Count)): $($onlyB -join ', ')"
    } else {
      $moved = @(for ($i = 0; $i -lt $typesA.Types.Count; $i++) {
        if ($typesA.Types[$i] -ne $typesB.Types[$i]) { "      TypeDef $($i + 1): $($typesA.Types[$i]) | $($typesB.Types[$i])" }
      })
      if ($moved.Count) {
        Write-Host "    the same $($typesA.Types.Count) types, $($moved.Count) of them at another place in the TypeDef table (first | second):"
        $moved | Select-Object -First 60 | ForEach-Object { Write-Host $_ }
        if ($moved.Count -gt 60) { Write-Host "      and $($moved.Count - 60) more" }
      } else {
        Write-Host "    the same $($typesA.Types.Count) types in the same order: the difference is outside the type table"
      }
    }
  } catch {
    Write-Host "    the type tables could not be read: $($_.Exception.GetType().FullName)"
  }
  return $false
}

# The check itself. It returns at the first failure the rest cannot run past, and the folders
# it writes are removed whichever way it ends.
function Invoke-Check {
  Write-Host 'The first build:'
  New-Item -ItemType Directory -Force -Path $copies | Out-Null
  foreach ($file in $files) {
    if (-not $file.First -or -not (Test-Path -LiteralPath $file.First -PathType Leaf)) {
      Fail "the first build has no $($file.Label) (looked for: $($file.First)); run this after the two merged builds"
      continue
    }
    $copy = Join-Path $copies ("{0}-{1}" -f $files.IndexOf($file), (Split-Path -Leaf $file.First))
    Copy-Item -LiteralPath $file.First -Destination $copy
    Write-Host "  $($file.Label): $($file.First)"
    $file | Add-Member -NotePropertyName Copy -NotePropertyValue $copy
  }
  if ($failures.Count) { return }

  Write-Host 'The control, a copy of the first InstallerClean.Core.dll with one byte changed:'
  $core = $files | Where-Object Label -eq 'InstallerClean.Core.dll'
  $planted = Join-Path $copies 'planted-InstallerClean.Core.dll'
  $bytes = [System.IO.File]::ReadAllBytes($core.Copy)
  $bytes[[int]($bytes.Length / 2)] = $bytes[[int]($bytes.Length / 2)] -bxor 0xFF
  [System.IO.File]::WriteAllBytes($planted, $bytes)
  if (Same-Bytes 'the planted copy' $core.Copy $planted $true) {
    Fail 'the comparison found no difference between InstallerClean.Core.dll and a copy with one byte changed'
    return
  }
  Write-Host '  reported, as it should be'

  Write-Host 'The second build, every project compiled again by a new compiler process:'
  & dotnet build-server shutdown | Out-Null
  $started = [DateTime]::UtcNow
  # On Windows the SDK needs nothing more; elsewhere it builds a Windows target only when told it may.
  $targeting = @(if (-not $IsWindows) { '-p:EnableWindowsTargeting=true' })
  foreach ($build in @(
      @{ Project = 'src/InstallerClean/InstallerClean.csproj'; Dir = "$second/publish/window/" }
      @{ Project = 'src/InstallerClean.Cli/InstallerClean.Cli.csproj'; Dir = "$second/publish/cli/" })) {
    & dotnet build $build.Project -c Release -p:RestoreLockedMode=true -p:OneFile=true "-p:OneFileDir=$($build.Dir)" `
      -p:UseSharedCompilation=false -nodeReuse:false --no-incremental --nologo -v:q @targeting
    if ($LASTEXITCODE -ne 0) { Fail "the second build of $($build.Project) failed with exit code $LASTEXITCODE"; return }
  }

  Write-Host 'The two builds compared:'
  foreach ($file in $files) {
    $path = & $file.Second
    if (-not $path -or -not (Test-Path -LiteralPath $path -PathType Leaf)) {
      Fail "the second build has no $($file.Label) (looked for: $path)"
      continue
    }
    if ((Get-Item -LiteralPath $path).LastWriteTimeUtc -lt $started) {
      Fail "the second build did not write $($file.Label) again ($path), so there is no second build of it to compare"
      continue
    }
    if (-not (Same-Bytes $file.Label $file.Copy $path $file.Assembly)) {
      Fail "two builds of this commit gave different bytes for $($file.Label)"
    }
  }
}

# Every file the [Files] section of the setup script packs, by its path from the repo root.
function Setup-Sources {
  $script = 'installer/InstallerClean.iss'
  $lines = Get-Content -LiteralPath $script
  $publishDir = $lines | ForEach-Object { if ($_ -match '^#define PublishDir "(.+)"$') { $Matches[1] } } | Select-Object -First 1
  if (-not $publishDir) { Fail "$script defines no PublishDir the check can read"; return @() }
  $inFiles = $false
  $sources = foreach ($line in $lines) {
    if ($line -match '^\[(.+)\]$') { $inFiles = $Matches[1] -eq 'Files'; continue }
    if (-not $inFiles -or $line -notmatch '^Source: "([^"]+)"') { continue }
    $written = $Matches[1]
    $source = $written.Replace('{#PublishDir}', $publishDir)
    if ($source.Contains('{')) { Fail "$($script): a [Files] Source the check cannot resolve: $written"; continue }
    Join-Path 'installer' $source
  }
  if (-not $sources) { Fail "$script has no [Files] Source the check can read" }
  @($sources)
}

# The setup check, run with -Setup.
function Invoke-SetupCheck {
  $version = ([xml](Get-Content Directory.Build.props)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
  $first = "$PublishRoot/InstallerClean-$version-setup.exe"
  if (-not (Test-Path -LiteralPath $first -PathType Leaf)) { Fail "there is no first setup to compare (looked for: $first); run this after the setup is compiled"; return }
  New-Item -ItemType Directory -Force -Path $copies, "$second/again", "$second/control" | Out-Null
  $copy = Join-Path $copies (Split-Path -Leaf $first)
  Copy-Item -LiteralPath $first -Destination $copy
  Write-Host "The first setup: $first"

  $sources = Setup-Sources
  if ($failures.Count) { return }
  $other = [DateTime]::UtcNow.AddDays(-1)
  Write-Host "Every file the setup packs, set to a last-write time of $($other.ToString('u')):"
  foreach ($source in $sources) {
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { Fail "the setup packs $source, which is not there"; continue }
    (Get-Item -LiteralPath $source).LastWriteTimeUtc = $other
    Write-Host "  $source"
  }
  if ($failures.Count) { return }

  $commitTime = & ./scripts/compile-setup.ps1 -PrintCommitTime
  $controlTime = [DateTime]::ParseExact($commitTime, 'yyyy-MM-ddTHH:mm:ss', [System.Globalization.CultureInfo]::InvariantCulture).AddDays(1).ToString(
    'yyyy-MM-ddTHH:mm:ss', [System.Globalization.CultureInfo]::InvariantCulture)
  foreach ($compile in @(
      @{ Dir = "$second/again"; Time = $commitTime; Label = 'The second compile, with the commit time' }
      @{ Dir = "$second/control"; Time = $controlTime; Label = 'The control, compiled with the commit time a day later' })) {
    Write-Host "$($compile.Label):"
    & ./scripts/compile-setup.ps1 -OutputDir $compile.Dir -CommitTime $compile.Time -FailOnWarning
    if ($LASTEXITCODE -ne 0) { Fail "the compile into $($compile.Dir) failed with exit code $LASTEXITCODE"; return }
    $path = Join-Path $compile.Dir (Split-Path -Leaf $first)
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "the compile wrote no setup at $path"; return }
  }

  $name = Split-Path -Leaf $first
  Write-Host 'The control against the first setup:'
  if (Same-Bytes 'the control setup' $copy "$second/control/$name" $false) {
    Fail 'a setup compiled with a commit time a day later came out the same as the first'
    return
  }
  Write-Host '  reported, as it should be'
  Write-Host 'The two compiles with the commit time compared:'
  if (-not (Same-Bytes 'setup' $copy "$second/again/$name" $false)) {
    Fail 'two compiles of the setup for this commit gave different bytes'
  }
}

try { if ($Setup) { Invoke-SetupCheck } else { Invoke-Check } }
finally { Remove-Item -Recurse -Force -LiteralPath $copies, $second -ErrorAction SilentlyContinue }
if ($failures.Count) { exit 1 }
if ($Setup) { Write-Host 'Two compiles of the setup for this commit gave the same bytes.' }
else { Write-Host 'Two builds of this commit gave the same bytes for every file compared.' }
