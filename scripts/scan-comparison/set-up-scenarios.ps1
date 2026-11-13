# Builds the scan comparison's test programs and patches with WiX, installs them on the runner
# and puts files into the Installer folder, so the comparison has files of known kinds to judge
# beside the runner's own. Nothing here needs a restart, and a step Windows answers with
# "restart required" stops the script. Writes scenarios.tsv to -OutDir: one row per file in
# the Installer folder the script made or placed, with what that file is.
#
# What it sets up:
#  - A and B, per machine, at 1.0.0. Patch P1 updates both to 1.0.1, so one cached patch
#    serves two programs. Patch P2 updates A to 1.0.2 and supersedes P1 there, while P1 stays
#    the current patch on B.
#  - U, per user, installed by the account the job runs as.
#  - S, a per-user package installed by SYSTEM from a scheduled task.
#  - C, per machine, installed and removed, its cached copy kept and put back under the name
#    Windows gave it, so the folder holds a file whose program has gone.
#  - Files no program or patch registered: D, a program never installed; a second copy of A
#    under a new name; patch Q, which targets A and was never applied; a second copy of P1's
#    cached patch under a new name.
#
# Run from the repo root as an administrator, with wix.exe on PATH:
#   pwsh scripts/scan-comparison/set-up-scenarios.ps1 -OutDir <folder>
param([Parameter(Mandatory)][string]$OutDir)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$installer = Join-Path $env:SystemRoot 'Installer'
$msiexec = Join-Path $env:SystemRoot 'System32\msiexec.exe'
$build = Join-Path $OutDir 'build'
$logs = Join-Path $OutDir 'logs'
$null = New-Item -ItemType Directory -Force -Path $build, $logs

$products = [ordered]@{
    A = @{ Code = '{8DB1FAAB-ED8A-4808-93D5-329139A310F7}'; Upgrade = '{8EFEA1AC-C0C5-4F6F-A252-03C2DAD5B7E2}'; Component = '{2BDA83AA-7A45-4571-BE5A-A2325670EC7B}'; Scope = 'perMachine'; Versions = '1.0.0', '1.0.1', '1.0.2', '1.0.3' }
    B = @{ Code = '{68A9EF26-E063-49CC-8FFE-A7AC67556D03}'; Upgrade = '{6EFEC85D-4672-4AEF-B732-7A32A5491E5B}'; Component = '{1BEEF627-2544-4896-9661-B8BC522A8886}'; Scope = 'perMachine'; Versions = '1.0.0', '1.0.1' }
    C = @{ Code = '{A9983B0B-A5F2-445D-8EBB-CA30B74EBAA1}'; Upgrade = '{29D7DCD4-99F8-4D8E-BCED-64E3BA862F67}'; Component = '{9F3B5B05-C503-4524-9582-43FA15DD836A}'; Scope = 'perMachine'; Versions = , '1.0.0' }
    D = @{ Code = '{287B0E22-2F47-4025-86CF-355618D70656}'; Upgrade = '{6853BCC0-A07C-4BFE-9C8A-DF5511559940}'; Component = '{B229AF83-3668-4AF0-B6F0-7124372F1D30}'; Scope = 'perMachine'; Versions = , '1.0.0' }
    U = @{ Code = '{DEDBA091-66F9-4098-90FD-969F67E2249C}'; Upgrade = '{2704706F-C232-491B-9A25-24CFAC4DD627}'; Component = '{4EFCA862-EFC2-4830-9808-81A302F3C5FD}'; Scope = 'perUser'; Versions = , '1.0.0' }
    S = @{ Code = '{014CFBC6-35F1-4EFD-A83C-8C39D2506E1E}'; Upgrade = '{CDDF9CC9-ECF5-4AEE-8C20-C74F36A9E52D}'; Component = '{0F929006-FD3F-4460-B128-B1D35FE9BC16}'; Scope = 'perUser'; Versions = , '1.0.0' }
}
$patches = [ordered]@{
    P1 = @{ Code = '{BFAD921A-E6F1-4150-AD44-F4C695549ED4}'; Family = '1.0.1.0'; Supersede = 'no'; Targets = @{ A = '1.0.1'; B = '1.0.1' } }
    P2 = @{ Code = '{9B950B4C-AA31-4CCC-ADE3-65EA331760C0}'; Family = '1.0.2.0'; Supersede = 'yes'; Targets = @{ A = '1.0.2' } }
    Q = @{ Code = '{4A50A0D7-5AB0-45B9-80E4-6FD5B560B284}'; Family = '1.0.3.0'; Supersede = 'no'; Targets = @{ A = '1.0.3' } }
}
$rows = [System.Collections.Generic.List[string]]::new()
$rows.Add("file`twhat it is")

function Invoke-Wix([string[]]$wixArgs) {
    $out = & wix @wixArgs 2>&1
    if ($LASTEXITCODE -ne 0) { $out | ForEach-Object { "  $_" }; throw "wix $($wixArgs[0..1] -join ' ') exited $LASTEXITCODE." }
}

# Runs msiexec quietly with a verbose log and stops the script on any answer but success. A
# restart-required answer stops it too: the comparison runs on a runner that has not restarted.
function Invoke-Msiexec([string]$what, [string[]]$msiArgs) {
    $log = Join-Path $logs (($what -replace '[^A-Za-z0-9]+', '-') + '.log')
    $p = Start-Process -FilePath $msiexec -ArgumentList ($msiArgs + @('/qn', '/norestart', '/l*v', "`"$log`"")) -Wait -PassThru
    "msiexec, ${what}: exit code $($p.ExitCode)"
    if ($p.ExitCode -ne 0) { throw "msiexec, $what, exited $($p.ExitCode); its log is $log." }
}

# The cached file Windows Installer records for a product or a patch, read with
# MsiGetProductInfoEx and MsiGetPatchInfoEx. An empty user SID passes null, which names the
# account the job runs as for a per-user context and is what a machine context requires.
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class ScenarioMsi {
    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    static extern uint MsiGetProductInfoExW(string product, string sid, int context, string property, StringBuilder value, ref uint length);
    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    static extern uint MsiGetPatchInfoExW(string patch, string product, string sid, int context, string property, StringBuilder value, ref uint length);
    public static string Product(string product, string sid, int context, string property) {
        var value = new StringBuilder(1024); uint length = 1024;
        uint rc = MsiGetProductInfoExW(product, sid.Length == 0 ? null : sid, context, property, value, ref length);
        return rc == 0 ? value.ToString() : null;
    }
    public static string Patch(string patch, string product, string sid, int context, string property) {
        var value = new StringBuilder(1024); uint length = 1024;
        uint rc = MsiGetPatchInfoExW(patch, product, sid.Length == 0 ? null : sid, context, property, value, ref length);
        return rc == 0 ? value.ToString() : null;
    }
}
"@
$machine = 4
$userUnmanaged = 2

function ProductPackage([string]$code, [string]$sid, [int]$context) { [ScenarioMsi]::Product($code, $sid, $context, 'LocalPackage') }
function PatchPackage([string]$productCode, [string]$patchCode) { [ScenarioMsi]::Patch($patchCode, $productCode, '', $machine, 'LocalPackage') }
function PatchState([string]$productCode, [string]$patchCode) { [ScenarioMsi]::Patch($patchCode, $productCode, '', $machine, 'State') }

function Record([string]$path, [string]$what) {
    if (-not $path) { throw "Windows Installer lists no cached file for $what." }
    if (-not (Test-Path -LiteralPath $path)) { throw "The cached file for $what, $path, is not on disk." }
    $name = Split-Path -Leaf $path
    "  $name`: $what"
    $rows.Add("$name`t$what")
}

# A name the Installer folder does not hold yet, in the folder's own form: hexadecimal digits
# and the extension.
function NewCacheName([string]$extension) {
    do { $name = ('{0:x8}' -f (Get-Random -Maximum ([int]::MaxValue))) + $extension }
    while (Test-Path -LiteralPath (Join-Path $installer $name))
    return $name
}

function Place([string]$source, [string]$what) {
    $extension = [System.IO.Path]::GetExtension($source)
    $target = Join-Path $installer (NewCacheName $extension)
    Copy-Item -LiteralPath $source -Destination $target
    Record $target $what
}

"Building the test programs and patches"
$payloads = Join-Path $build 'payload'
$versions = @($products.Values | ForEach-Object { $_.Versions } | Sort-Object -Unique)
$i = 0
foreach ($version in $versions) {
    $folder = Join-Path $payloads $version
    $null = New-Item -ItemType Directory -Force -Path $folder
    Set-Content -LiteralPath (Join-Path $folder 'payload.txt') -Encoding ascii -Value ("InstallerClean scan comparison payload, version $version. " + ('x' * (64 + 37 * $i)))
    $i++
}
foreach ($tag in $products.Keys) {
    $p = $products[$tag]
    foreach ($version in $p.Versions) {
        Invoke-Wix @('build', '-arch', 'x64', (Join-Path $here 'product.wxs'),
            '-d', "Tag=$tag", '-d', "Version=$version", '-d', "Scope=$($p.Scope)",
            '-d', "PayloadDir=$(Join-Path $payloads $version)",
            '-d', "ProductCode=$($p.Code)", '-d', "UpgradeCode=$($p.Upgrade)", '-d', "ComponentGuid=$($p.Component)",
            '-o', (Join-Path $build "$tag-$version.msi"))
    }
}
foreach ($tag in $patches.Keys) {
    $p = $patches[$tag]
    $defines = @('-d', "PatchId=$($p.Code.Trim('{', '}'))", '-d', "Tag=$tag", '-d', "FamilyVersion=$($p.Family)", '-d', "Supersede=$($p.Supersede)", '-d', 'Ext=wixpdb')
    $updated = $null
    foreach ($target in $p.Targets.Keys) {
        $defines += @('-d', "Base$target=$target-1.0.0", '-d', "Upd$target=$target-$($p.Targets[$target])")
        $updated = $p.Targets[$target]
    }
    # Every target of one patch moves to the same version, so one pair of payload folders
    # serves them all.
    Invoke-Wix (@('build', (Join-Path $here 'patch.wxs')) + $defines + @(
            '-bindpath', $build,
            '-bindpath:target', (Join-Path $payloads '1.0.0'),
            '-bindpath:update', (Join-Path $payloads $updated),
            '-o', (Join-Path $build "$tag.msp")))
}
Get-ChildItem -LiteralPath $build -File | Where-Object Extension -in '.msi', '.msp' | ForEach-Object { "  built $($_.Name), $($_.Length) bytes" }

"Installing"
Invoke-Msiexec 'install A' @('/i', "`"$(Join-Path $build 'A-1.0.0.msi')`"")
Invoke-Msiexec 'install B' @('/i', "`"$(Join-Path $build 'B-1.0.0.msi')`"")
Invoke-Msiexec 'apply P1 to A and B' @('/p', "`"$(Join-Path $build 'P1.msp')`"")
Invoke-Msiexec 'apply P2 to A' @('/p', "`"$(Join-Path $build 'P2.msp')`"")
Invoke-Msiexec 'install U for this account' @('/i', "`"$(Join-Path $build 'U-1.0.0.msi')`"")
Invoke-Msiexec 'install C' @('/i', "`"$(Join-Path $build 'C-1.0.0.msi')`"")

# SYSTEM's own per-user install, from a scheduled task, waited on until it ends.
$taskName = 'InstallerClean scan comparison, S for SYSTEM'
$sLog = Join-Path $logs 'install-S-for-SYSTEM.log'
$action = New-ScheduledTaskAction -Execute $msiexec -Argument "/i `"$(Join-Path $build 'S-1.0.0.msi')`" /qn /norestart /l*v `"$sLog`""
$null = Register-ScheduledTask -TaskName $taskName -Action $action -User 'SYSTEM' -RunLevel Highest -Force
Start-ScheduledTask -TaskName $taskName
# 267011 is the task's result before its first run and 267009 its result while it runs.
$clock = [Diagnostics.Stopwatch]::StartNew()
do {
    Start-Sleep -Seconds 1
    $task = Get-ScheduledTask -TaskName $taskName
    $sResult = (Get-ScheduledTaskInfo -TaskName $taskName).LastTaskResult
    $ended = $task.State -ne 'Running' -and $sResult -notin 267011, 267009
} while (-not $ended -and $clock.Elapsed.TotalSeconds -lt 300)
Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
"msiexec, install S for SYSTEM: exit code $sResult"
if (-not $ended) { throw "The install of S for SYSTEM had not ended after 300 s." }
if ($sResult -ne 0) { throw "The install of S for SYSTEM exited $sResult; its log is $sLog." }

"What each test file in the Installer folder is"
Record (ProductPackage $products.A.Code '' $machine) 'A, installed per machine, patched by P1 and P2'
Record (ProductPackage $products.B.Code '' $machine) 'B, installed per machine, patched by P1'
Record (ProductPackage $products.U.Code '' $userUnmanaged) 'U, installed per user by the account the job runs as'
# Windows Installer may record a per-user install by SYSTEM in SYSTEM's own per-user context or
# in the machine context; the line says which one answered.
$sPackage = ProductPackage $products.S.Code 'S-1-5-18' $userUnmanaged
$sWhere = 'in SYSTEM''s per-user context'
if (-not $sPackage) { $sPackage = ProductPackage $products.S.Code '' $machine; $sWhere = 'in the machine context' }
Record $sPackage "S, a per-user package installed by SYSTEM, recorded $sWhere"
$p1 = PatchPackage $products.A.Code $patches.P1.Code
if ($p1 -ne (PatchPackage $products.B.Code $patches.P1.Code)) { throw "P1 is not one cached file for A and B." }
"  P1's state on A is $(PatchState $products.A.Code $patches.P1.Code) and on B is $(PatchState $products.B.Code $patches.P1.Code) (1 applied, 2 superseded)"
Record $p1 'P1, applied to A and B, superseded on A by P2 and current on B'
Record (PatchPackage $products.A.Code $patches.P2.Code) 'P2, applied to A, superseding P1 there'

$cPackage = ProductPackage $products.C.Code '' $machine
if (-not $cPackage) { throw "Windows Installer lists no cached file for C." }
$cKept = Join-Path $build 'C-cached-copy.msi'
Copy-Item -LiteralPath $cPackage -Destination $cKept
Invoke-Msiexec 'remove C' @('/x', $products.C.Code)
if (Test-Path -LiteralPath $cPackage) { throw "The removal of C left its cached file $cPackage in place." }
Copy-Item -LiteralPath $cKept -Destination $cPackage
Record $cPackage 'C, installed and removed, its cached copy put back under the name Windows gave it'

Place (Join-Path $build 'D-1.0.0.msi') 'D, never installed'
Place (Join-Path $build 'A-1.0.1.msi') 'a second copy of A at 1.0.1, the installed program''s own product code'
Place (Join-Path $build 'Q.msp') 'Q, a patch for A that was never applied'
Place $p1 'a second copy of P1''s cached patch under a new name'

$tsv = Join-Path $OutDir 'scenarios.tsv'
Set-Content -LiteralPath $tsv -Value $rows -Encoding utf8
"$($rows.Count - 1) test files set up; their list is $tsv"
