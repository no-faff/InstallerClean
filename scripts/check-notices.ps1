# Fails (exit 1) where a merged exe does not carry THIRD-PARTY-NOTICES as the repository has
# it, or merges an assembly the file does not name. Read from the two exes the OneFile merge
# writes (publish/window and publish/cli) and the input list each merge was given.
#
# For each merged exe the step holds:
#
#  - The exe holds the manifest resource InstallerClean.THIRD-PARTY-NOTICES, and its bytes
#    are the bytes of THIRD-PARTY-NOTICES at the root of the repository. The window shows
#    that resource and the command line prints it, so it is the copy every download carries.
#  - Every assembly the merge took, other than InstallerClean.Core, is named in the file's
#    table of merged packages, as the first word of a line of that table. Each of those
#    assemblies has its package's name.
#
# The merge's input list is the response file the MergeToOneFile target writes
# (Directory.Build.targets), obj/Release/net48/onefile.rsp, or obj/Release/net48/win-x64 where
# the SDK infers the win-x64 runtime identifier; each one there is read.
#
# Every run also checks itself: the resource comparison has to report a copy of the file with
# one byte changed, and the table lookup has to report a name the table does not hold. A
# missing exe, a missing input list or a control that reports nothing fails the step.
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

# The names in the table of merged packages: the first word of each line between the line
# headed "Package" and the first blank line after it.
$lines = $text -split "`n"
$head = [Array]::FindIndex($lines, [Predicate[string]] { param($l) $l -match '^\s+Package\s+Version\s+In\s*$' })
if ($head -lt 0) { Fail 'THIRD-PARTY-NOTICES has no table of merged packages headed "Package  Version  In".' }
$named = @(for ($i = $head + 1; $head -ge 0 -and $i -lt $lines.Length -and $lines[$i].Trim() -ne ''; $i++) {
  if ($lines[$i] -match '^\s{2}(\S+)(\s|$)') { $Matches[1] }
})
Write-Host "THIRD-PARTY-NOTICES names $($named.Count) merged packages: $($named -join ', ')"

# The control for the lookup: a name no package has must be reported.
if ($named -contains 'NoSuchPackage.ForTheControl') { Fail 'The table lookup found a name it was built not to find.' }

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
  foreach ($list in $lists) {
    $inputs = @(Get-Content -LiteralPath $list | Where-Object { $_ -like '*.dll' } |
      ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Trim()) } |
      Where-Object { $_ -ne 'InstallerClean.Core' })
    if ($inputs.Count -eq 0) { Fail "$list lists no assembly to merge besides InstallerClean.Core."; continue }
    Write-Host "${list}: $($inputs -join ', ')"
    foreach ($assembly in $inputs) {
      if ($named -notcontains $assembly) { Fail "The merge into $($exe.Merged) takes $assembly.dll, which THIRD-PARTY-NOTICES does not name." }
    }
  }
}

if ($failures.Count) {
  Write-Host "$($failures.Count) problem(s)."
  exit 1
}
Write-Host 'Both merged exes carry THIRD-PARTY-NOTICES as the repository has it, and it names every package merged into them.'
