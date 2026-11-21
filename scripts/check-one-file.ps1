# Fails (exit 1) where a merged exe is not the whole app, read from the two exes the OneFile
# merge writes (publish/window and publish/cli).
#
# A copy decides it is one file, and so may update by replacing its own exe, where the code
# of AppCopy is in the exe's own assembly (AppCopy.IsOneFileBuild). For each merged exe the
# step holds:
#
#  - The exe's metadata defines InstallerClean.Services.AppCopy, so a copy started from it
#    takes itself for one file.
#  - The exe references no assembly whose name starts with InstallerClean, so no part of the
#    app is left in a file beside it for an update to leave at the version it was.
#
# Every run also checks itself against the exe each merge started from, the project's own
# Release build, which is not one file: it has to report both faults. That build is at
# bin/Release/net48/win-x64 where the SDK infers the win-x64 runtime identifier, as it does
# on Windows, and at bin/Release/net48 where it infers none; each one there is checked. A missing
# exe on either side, or a control that reports nothing, fails the step.
#
# Run from the repo root after the two merged builds: pwsh scripts/check-one-file.ps1
param([string]$PublishRoot = 'publish')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Reflection.Metadata

$appCopy = 'InstallerClean.Services.AppCopy'
$failures = [System.Collections.Generic.List[string]]::new()
function Fail([string]$message) { $failures.Add($message); Write-Host "::error::$message" }

# What the assembly at $path defines and references: the full names of the types named
# AppCopy, and the names of every assembly it references.
function Read-Assembly([string]$path) {
  $stream = [System.IO.File]::OpenRead($path)
  try {
    $pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
    try {
      $md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
      $types = foreach ($handle in $md.TypeDefinitions) {
        $type = $md.GetTypeDefinition($handle)
        if ($md.GetString($type.Name) -eq 'AppCopy') { "$($md.GetString($type.Namespace)).AppCopy" }
      }
      $references = foreach ($handle in $md.AssemblyReferences) {
        $md.GetString($md.GetAssemblyReference($handle).Name)
      }
      [pscustomobject]@{ AppCopyTypes = @($types); References = @($references | Sort-Object -Unique) }
    } finally { $pe.Dispose() }
  } finally { $stream.Dispose() }
}

# The faults in the assembly at $path that keep it from being the whole app, printing what
# was read.
function Faults([string]$path) {
  $assembly = Read-Assembly $path
  Write-Host "  defines: $(if ($assembly.AppCopyTypes.Count) { $assembly.AppCopyTypes -join ', ' } else { 'no type named AppCopy' })"
  Write-Host "  references: $($assembly.References -join ', ')"
  $found = @()
  if ($assembly.AppCopyTypes -notcontains $appCopy) { $found += "does not define $appCopy" }
  $own = @($assembly.References | Where-Object { $_ -like 'InstallerClean*' })
  if ($own.Count) { $found += "references $($own -join ', '), which is not inside it" }
  , $found
}

$exes = [ordered]@{
  'window' = @{ Merged = Join-Path $PublishRoot 'window/InstallerClean.exe'; Project = 'src/InstallerClean'; Name = 'InstallerClean.exe' }
  'cli' = @{ Merged = Join-Path $PublishRoot 'cli/installerclean-cli.exe'; Project = 'src/InstallerClean.Cli'; Name = 'installerclean-cli.exe' }
}

foreach ($label in $exes.Keys) {
  $exe = $exes[$label]
  if (-not (Test-Path -LiteralPath $exe.Merged)) { Fail "$($exe.Merged) is not there; the two merged builds come first."; continue }
  Write-Host "$($exe.Merged):"
  foreach ($fault in (Faults $exe.Merged)) { Fail "$($exe.Merged) $fault." }

  $controls = @("bin/Release/net48/win-x64/$($exe.Name)", "bin/Release/net48/$($exe.Name)" |
    ForEach-Object { Join-Path $exe.Project $_ } | Where-Object { Test-Path -LiteralPath $_ })
  if ($controls.Count -eq 0) { Fail "No Release build of $($exe.Name) under $($exe.Project)/bin/Release/net48 to check this step against."; continue }
  foreach ($control in $controls) {
    Write-Host "$control, the exe the merge started from:"
    $faults = Faults $control
    if ($faults.Count -lt 2) { Fail "$control is not one file and the step reported $($faults.Count) of its two faults: $($faults -join '; ')" }
    else { Write-Host "  reported, as it has to be: $($faults -join '; ')" }
  }
}

if ($failures.Count) {
  Write-Host "$($failures.Count) problem(s)."
  exit 1
}
Write-Host 'Both merged exes define AppCopy and reference no part of the app outside themselves.'
