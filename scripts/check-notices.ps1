# Fails (exit 1) where a merged exe does not carry THIRD-PARTY-NOTICES as the repository has
# it, or merges an assembly the file does not name. Read from the two exes the OneFile merge
# writes (publish/window and publish/cli) and the input list each merge was given.
#
# For each merged exe the step holds:
#
#  - The exe holds the manifest resource InstallerClean.THIRD-PARTY-NOTICES, and its bytes
#    are the bytes of THIRD-PARTY-NOTICES at the root of the repository. The window shows
#    that resource and the command line prints it, so it is the copy every download carries.
#  - The file's table of merged packages and the merge agree, both ways: every assembly the
#    merge took, other than InstallerClean.Core, is a row of the table; every row whose In
#    column names this exe ("both", or "window" for InstallerClean.exe) is an assembly the
#    merge took; and the row's Version is the version the project's packages.lock.json
#    resolves that package to. Each of those assemblies has its package's name.
#
# The merge's input list is the response file the MergeToOneFile target writes
# (Directory.Build.targets), at obj/Release/net48/onefile.rsp, or under obj/Release/net48/win-x64
# where the SDK infers the win-x64 runtime identifier. Where both are there, the newer is the
# list of the merge that wrote the exe, and it is the one read.
#
# Every run also checks itself: the resource comparison has to report a copy of the file with
# one byte changed, and the table comparison, run on this run's own inputs with an assembly
# added, one taken away and one version changed, has to report all three. A missing exe, a
# missing input list or a control that reports less fails the step.
#
# Run from the repo root after the two merged builds: pwsh scripts/check-notices.ps1
param([string]$PublishRoot = 'publish')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Reflection.Metadata

$resourceName = 'InstallerClean.THIRD-PARTY-NOTICES'
$failures = [System.Collections.Generic.List[string]]::new()
function Fail([string]$message) { $failures.Add($message); Write-Host "::error::$message" }

# The bytes of the manifest resource $name in the assembly at $path, or $null where it holds
# none of that name. An embedded resource's data is a 4-byte length and then the bytes, at
# its offset into the image's resources directory.
function Read-Resource([string]$path, [string]$name) {
  $stream = [System.IO.File]::OpenRead($path)
  try {
    $pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
    try {
      $md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
      foreach ($handle in $md.ManifestResources) {
        $resource = $md.GetManifestResource($handle)
        if ($md.GetString($resource.Name) -ne $name) { continue }
        if (-not $resource.Implementation.IsNil) { throw "$path names $name as held in another file." }
        $directory = $pe.PEHeaders.CorHeader.ResourcesDirectory
        $section = $pe.GetSectionData($directory.RelativeVirtualAddress)
        $reader = $section.GetReader([int]$resource.Offset, $directory.Size - [int]$resource.Offset)
        $length = $reader.ReadInt32()
        return , $reader.ReadBytes($length)
      }
      return $null
    } finally { $pe.Dispose() }
  } finally { $stream.Dispose() }
}

function Same-Bytes([byte[]]$a, [byte[]]$b) {
  if ($a.Length -ne $b.Length) { return $false }
  for ($i = 0; $i -lt $a.Length; $i++) { if ($a[$i] -ne $b[$i]) { return $false } }
  $true
}

$file = [System.IO.File]::ReadAllBytes((Resolve-Path 'THIRD-PARTY-NOTICES'))
$text = [System.Text.UTF8Encoding]::new($false, $true).GetString($file)

# The rows of the table of merged packages, between the line headed "Package  Version  In" and
# the first blank line after it: each row's package, version and the exes it is merged into.
$lines = $text -split "`n"
$head = [Array]::FindIndex($lines, [Predicate[string]] { param($l) $l -match '^\s+Package\s+Version\s+In\s*$' })
if ($head -lt 0) { Fail 'THIRD-PARTY-NOTICES has no table of merged packages headed "Package  Version  In".' }
$rows = @(for ($i = $head + 1; $head -ge 0 -and $i -lt $lines.Length -and $lines[$i].Trim() -ne ''; $i++) {
  if ($lines[$i] -match '^\s{2}(\S+)\s+(\S+)\s+(both|window)\s*$') {
    [pscustomobject]@{ Name = $Matches[1]; Version = $Matches[2]; In = $Matches[3] }
  } else { Fail "THIRD-PARTY-NOTICES has a line in its table of merged packages that is not a package, a version and both or window: '$($lines[$i])'" }
})
Write-Host "THIRD-PARTY-NOTICES lists $($rows.Count) merged packages: $(($rows | ForEach-Object { "$($_.Name) $($_.Version) ($($_.In))" }) -join ', ')"

# Where the table and one merge disagree: $label is window or cli, $inputs the assemblies the
# merge took, and $versions the project's lock file, package name to resolved version.
function Disagreements([string]$label, $rows, [string[]]$inputs, [hashtable]$versions) {
  $found = [System.Collections.Generic.List[string]]::new()
  foreach ($assembly in $inputs) {
    $row = $rows | Where-Object { $_.Name -eq $assembly } | Select-Object -First 1
    if (-not $row) { $found.Add("the merge takes $assembly.dll, which the table does not list"); continue }
    if ($row.In -eq 'window' -and $label -ne 'window') { $found.Add("the merge takes $assembly.dll, which the table lists as merged into InstallerClean.exe alone") }
    if (-not $versions.ContainsKey($assembly)) { $found.Add("the lock file resolves no package $assembly") }
    elseif ($versions[$assembly] -ne $row.Version) { $found.Add("the table lists $assembly at $($row.Version), and the lock file resolves it to $($versions[$assembly])") }
  }
  foreach ($row in $rows) {
    if (($row.In -eq 'both' -or $label -eq 'window') -and $inputs -notcontains $row.Name) {
      $found.Add("the table lists $($row.Name) as merged in, and the merge does not take it")
    }
  }
  , $found
}

# The control for the comparison: the file with its last byte changed must differ.
$changed = [byte[]]$file.Clone()
$changed[$changed.Length - 1] = $changed[$changed.Length - 1] -bxor 1
if (Same-Bytes $file $changed) { Fail 'The comparison read a changed copy of the file as the same.' }

$exes = [ordered]@{
  'window' = @{ Merged = Join-Path $PublishRoot 'window/InstallerClean.exe'; Project = 'src/InstallerClean' }
  'cli' = @{ Merged = Join-Path $PublishRoot 'cli/installerclean-cli.exe'; Project = 'src/InstallerClean.Cli' }
}

foreach ($label in $exes.Keys) {
  $exe = $exes[$label]
  if (-not (Test-Path -LiteralPath $exe.Merged)) { Fail "$($exe.Merged) is not there; the two merged builds come first."; continue }

  $carried = Read-Resource $exe.Merged $resourceName
  if ($null -eq $carried) { Fail "$($exe.Merged) holds no resource named $resourceName." }
  elseif (-not (Same-Bytes $carried $file)) { Fail "$($exe.Merged) carries $resourceName as $($carried.Length) bytes, which are not the $($file.Length) bytes of THIRD-PARTY-NOTICES." }
  else { Write-Host "$($exe.Merged) carries $resourceName, $($carried.Length) bytes, the same as THIRD-PARTY-NOTICES." }

  $lists = @('obj/Release/net48/onefile.rsp', 'obj/Release/net48/win-x64/onefile.rsp' |
    ForEach-Object { Join-Path $exe.Project $_ } | Where-Object { Test-Path -LiteralPath $_ })
  if ($lists.Count -eq 0) { Fail "No onefile.rsp under $($exe.Project)/obj/Release/net48 to read the merge's inputs from."; continue }
  $list = $lists | Sort-Object { (Get-Item -LiteralPath $_).LastWriteTimeUtc } | Select-Object -Last 1
  $inputs = @(Get-Content -LiteralPath $list | Where-Object { $_ -like '*.dll' } |
    ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Trim()) } |
    Where-Object { $_ -ne 'InstallerClean.Core' })
  if ($inputs.Count -eq 0) { Fail "$list lists no assembly to merge besides InstallerClean.Core."; continue }
  Write-Host "${list}: $($inputs -join ', ')"

  $lock = Get-Content -Raw -LiteralPath (Join-Path $exe.Project 'packages.lock.json') | ConvertFrom-Json
  $versions = @{}
  foreach ($package in $lock.dependencies.'.NETFramework,Version=v4.8'.PSObject.Properties) { $versions[$package.Name] = $package.Value.resolved }

  foreach ($fault in (Disagreements $label $rows $inputs $versions)) { Fail "$($exe.Merged): $fault." }

  # The control: this merge's own inputs with an assembly added and the first one taken away,
  # and the second one's version changed in the lock file's answer. All three have to be
  # reported.
  $planted = @($inputs | Select-Object -Skip 1) + 'NoSuchPackage.ForTheControl'
  $plantedVersions = $versions.Clone()
  $plantedVersions[$inputs[1]] = '0.0.0-control'
  $caught = Disagreements $label $rows $planted $plantedVersions
  $kinds = @(
    ($caught | Where-Object { $_ -like '*NoSuchPackage.ForTheControl.dll, which the table does not list' }).Count,
    ($caught | Where-Object { $_ -like "*lists $($inputs[0]) as merged in, and the merge does not take it" }).Count,
    ($caught | Where-Object { $_ -like "*resolves it to 0.0.0-control" }).Count)
  if ($kinds -contains 0) { Fail "The table comparison for $label was given an added assembly, a missing one and a changed version, and reported: $($caught -join '; ')" }
  else { Write-Host "  the control was reported, as it has to be: $($caught -join '; ')" }
}

if ($failures.Count) {
  Write-Host "$($failures.Count) problem(s)."
  exit 1
}
Write-Host 'Both merged exes carry THIRD-PARTY-NOTICES as the repository has it, and its table of merged packages matches both merges and their lock files.'
