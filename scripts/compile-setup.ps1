# Compiles installer/InstallerClean.iss with Inno Setup's ISCC, giving it the two values the
# script requires: AppVersion, read from Directory.Build.props, and CommitTime, the UTC time
# of the commit checked out, read from git. CommitTime is every packed file's date and time
# and the copyright year, so two compiles of one commit around the same files give the same
# setup, whenever and wherever they run.
#
#   -UninstallerSignatureDir  passed to ISCC as UninstallerSignatureDir, for the compile
#                             that embeds a signed uninstaller
#   -OutputDir                where the setup is written, in place of the script's OutputDir
#   -CommitTime               a time to compile with in place of the commit's, as
#                             yyyy-mm-ddThh:mm:ss; the reproducibility check's control uses it
#   -FailOnWarning            fails where ISCC prints a line starting "Warning: ", which it
#                             does for a message missing from a language's .isl
#   -PrintCommitTime          writes the commit time in the form ISCC is given it, and
#                             compiles nothing, for a step that runs ISCC itself
#   -Commit                   the commit whose time is read, HEAD where it is not given
#
# ISCC is looked for under Program Files (x86), where the Inno Setup package puts it.
#
# Run from anywhere in the repository: pwsh scripts/compile-setup.ps1
param(
  [string]$UninstallerSignatureDir,
  [string]$OutputDir,
  [string]$CommitTime,
  [switch]$FailOnWarning,
  [switch]$PrintCommitTime,
  [string]$Commit = 'HEAD'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

if (-not $CommitTime) {
  # %ct is the committer time in seconds since 1970, which carries no time zone, so the
  # conversion to UTC below is the only one made.
  $seconds = & git -C $root log -1 --format=%ct $Commit
  if ($LASTEXITCODE -ne 0 -or $seconds -notmatch '^\d+$') { throw "Could not read the time of $Commit from git (got: $seconds)." }
  $CommitTime = [DateTimeOffset]::FromUnixTimeSeconds([long]$seconds).UtcDateTime.ToString(
    'yyyy-MM-ddTHH:mm:ss', [System.Globalization.CultureInfo]::InvariantCulture)
}
if ($PrintCommitTime) { Write-Output $CommitTime; exit 0 }

$iscc = Get-ChildItem "${env:ProgramFiles(x86)}" -Filter ISCC.exe -Recurse -Depth 1 -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName
if (-not $iscc) { throw "ISCC.exe not found under ${env:ProgramFiles(x86)}; install Inno Setup first." }

$version = ([xml](Get-Content (Join-Path $root 'Directory.Build.props'))).Project.PropertyGroup.Version |
           Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw "Could not read <Version> from Directory.Build.props." }

$arguments = @("/DAppVersion=$version", "/DCommitTime=$CommitTime")
if ($UninstallerSignatureDir) { $arguments += "/DUninstallerSignatureDir=$UninstallerSignatureDir" }
if ($OutputDir) { $arguments += "/O$OutputDir" }
$arguments += (Join-Path $root 'installer\InstallerClean.iss')

Write-Host "Compiling the setup: version $version, commit time $CommitTime (UTC)"
& $iscc @arguments | Tee-Object -Variable compileOutput
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed with exit code $LASTEXITCODE." }

if ($FailOnWarning) {
  $warnings = @($compileOutput | Where-Object { $_ -like 'Warning: *' })
  if ($warnings.Count -gt 0) {
    ''
    'ISCC printed these warnings, and any warning fails this compile:'
    $warnings
    exit 1
  }
}
