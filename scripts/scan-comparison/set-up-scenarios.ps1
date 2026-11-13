# Builds the scan comparison's test programs and patches with WiX, installs them on the runner
# and puts files into the Installer folder, so the comparison has files of known kinds to judge
# beside the runner's own. Nothing here needs a restart, and a step Windows answers with
# "restart required" stops the script. Writes scenarios.tsv to -OutDir: one row per file in
# the Installer folder the script made or placed, with what that file is, and, for a file the
# build under test is meant to judge otherwise than 3.7.0, whether it holds or offers the file
# and why (compare-scans.ps1).
#
# What it sets up:
#  - A and B, per machine, at 1.0.0. Patch P1 updates both to 1.0.1, so one cached patch
#    serves two programs. Patch P2 updates A to 1.0.2 and supersedes P1 there, while P1 stays
#    the current patch on B.
#  - U, per user, installed by the account the job runs as.
#  - S, a per-user package installed by SYSTEM from a scheduled task, and M, per machine.
#    Patch P3 updates both and is applied by SYSTEM, so one cached patch serves SYSTEM's
#    program and a machine program.
#  - V, a per-user package SYSTEM advertises (msiexec /ju) and never installs.
#  - X, installed per user by SYSTEM from a copy of its package placed in the Installer folder,
#    then per machine from the build folder. Windows Installer keeps one record and one cached
#    package for both, and SYSTEM's source list still names the copy, which this build holds
#    and 3.7.0 offers.
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
    S = @{ Code = '{014CFBC6-35F1-4EFD-A83C-8C39D2506E1E}'; Upgrade = '{CDDF9CC9-ECF5-4AEE-8C20-C74F36A9E52D}'; Component = '{0F929006-FD3F-4460-B128-B1D35FE9BC16}'; Scope = 'perUser'; Versions = '1.0.0', '1.0.1' }
    M = @{ Code = '{B4790A2F-EF1F-4CB7-847D-CAEABD85F56A}'; Upgrade = '{0EF225CD-3A41-427C-83BE-E16A18ECE231}'; Component = '{D2C16BA7-8268-4CCE-8832-864AE9C2E31E}'; Scope = 'perMachine'; Versions = '1.0.0', '1.0.1' }
    V = @{ Code = '{99DC2DBB-7BFE-4B55-A347-4B354CE3BEFC}'; Upgrade = '{E95AFCC4-6E05-472D-ADA3-5117C5D37118}'; Component = '{7EC7F849-EDA1-4223-9424-821AE4B07130}'; Scope = 'perUser'; Versions = , '1.0.0' }
    X = @{ Code = '{E6704E1A-DF47-478B-9FAD-3AA7FA64FDCD}'; Upgrade = '{C18FE168-72F6-40F6-B6DA-CE51E12B337F}'; Component = '{76F88265-1A25-4465-A78D-80CCEBA2A187}'; Scope = 'perUserOrMachine'; Versions = , '1.0.0' }
}
$patches = [ordered]@{
    P1 = @{ Code = '{BFAD921A-E6F1-4150-AD44-F4C695549ED4}'; Name = 'ICScanComparisonFamily'; Family = '1.0.1.0'; Supersede = 'no'; Targets = @{ A = '1.0.1'; B = '1.0.1' } }
    P2 = @{ Code = '{9B950B4C-AA31-4CCC-ADE3-65EA331760C0}'; Name = 'ICScanComparisonFamily'; Family = '1.0.2.0'; Supersede = 'yes'; Targets = @{ A = '1.0.2' } }
    Q = @{ Code = '{4A50A0D7-5AB0-45B9-80E4-6FD5B560B284}'; Name = 'ICScanComparisonFamily'; Family = '1.0.3.0'; Supersede = 'no'; Targets = @{ A = '1.0.3' } }
    P3 = @{ Code = '{C3A29953-BE83-4409-A590-1C8886D20F7B}'; Name = 'ICScanComparisonSystemFamily'; Family = '1.0.1.0'; Supersede = 'no'; Targets = @{ M = '1.0.1'; S = '1.0.1' } }
}
$rows = [System.Collections.Generic.List[string]]::new()
$rows.Add("file`twhat it is`tthis build`twhy")

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

# A file's row. $expect, where it is given, is 'holds' or 'offers': what the build under test
# does with the file where 3.7.0 does the other, with $why the reason.
function Record([string]$path, [string]$what, [string]$expect = '', [string]$why = '') {
    if (-not $path) { throw "Windows Installer lists no cached file for $what." }
    if (-not (Test-Path -LiteralPath $path)) { throw "The cached file for $what, $path, is not on disk." }
    if ($expect -notin '', 'holds', 'offers') { throw "An expected difference is 'holds' or 'offers', not '$expect'." }
    if (($expect -eq '') -ne ($why -eq '')) { throw "An expected difference for $what needs both what this build does and why." }
    $name = Split-Path -Leaf $path
    "  $name`: $what$(if ($expect) { " (this build $expect it: $why)" })"
    $rows.Add("$name`t$what`t$expect`t$why")
}

# A name the Installer folder does not hold yet, in the folder's own form: hexadecimal digits
# and the extension.
function NewCacheName([string]$extension) {
    do { $name = ('{0:x8}' -f (Get-Random -Maximum ([int]::MaxValue))) + $extension }
    while (Test-Path -LiteralPath (Join-Path $installer $name))
    return $name
}

# Copies $source into the Installer folder under a new name, records it, and answers its path.
function Place([string]$source, [string]$what, [string]$expect = '', [string]$why = '') {
    $extension = [System.IO.Path]::GetExtension($source)
    $target = Join-Path $installer (NewCacheName $extension)
    Copy-Item -LiteralPath $source -Destination $target
    Record $target $what $expect $why | Out-Host
    return $target
}

# A product code in the packed form Windows Installer names its registry keys with.
function Packed([string]$code) {
    $h = $code.Trim('{', '}').Replace('-', '').ToUpperInvariant()
    $r = (-join $h.Substring(0, 8).ToCharArray()[7..0]) + (-join $h.Substring(8, 4).ToCharArray()[3..0]) + (-join $h.Substring(12, 4).ToCharArray()[3..0])
    for ($i = 16; $i -lt 32; $i += 2) { $r += $h[$i + 1]; $r += $h[$i] }
    return $r
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
    $defines = @('-d', "PatchId=$($p.Code.Trim('{', '}'))", '-d', "Tag=$tag", '-d', "Family=$($p.Name)", '-d', "FamilyVersion=$($p.Family)", '-d', "Supersede=$($p.Supersede)", '-d', 'Ext=wixpdb')
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

# One msiexec run as SYSTEM, from a scheduled task, waited on until it ends. Stops the script on
# any answer but success, as Invoke-Msiexec does.
function Invoke-MsiexecAsSystem([string]$what, [string]$msiArgs) {
    $taskName = 'InstallerClean scan comparison, ' + ($what -replace '[^A-Za-z0-9 ]+', '')
    $log = Join-Path $logs (($what -replace '[^A-Za-z0-9]+', '-') + '.log')
    $action = New-ScheduledTaskAction -Execute $msiexec -Argument "$msiArgs /qn /norestart /l*v `"$log`""
    $null = Register-ScheduledTask -TaskName $taskName -Action $action -User 'SYSTEM' -RunLevel Highest -Force
    Start-ScheduledTask -TaskName $taskName
    # 267011 is the task's result before its first run and 267009 its result while it runs.
    $clock = [Diagnostics.Stopwatch]::StartNew()
    do {
        Start-Sleep -Seconds 1
        $task = Get-ScheduledTask -TaskName $taskName
        $result = (Get-ScheduledTaskInfo -TaskName $taskName).LastTaskResult
        $ended = $task.State -ne 'Running' -and $result -notin 267011, 267009
    } while (-not $ended -and $clock.Elapsed.TotalSeconds -lt 300)
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    "msiexec as SYSTEM, ${what}: exit code $result"
    if (-not $ended) { throw "msiexec as SYSTEM, $what, had not ended after 300 s." }
    if ($result -ne 0) { throw "msiexec as SYSTEM, $what, exited $result; its log is $log." }
}

# The .msi and .msp names at the top of the Installer folder.
function CacheNames { @(Get-ChildItem -LiteralPath $installer -File -Force | Where-Object Extension -in '.msi', '.msp' | ForEach-Object Name) }

# Runs one step and records each file it added to the Installer folder as what.
function RecordWhatItAdds([string]$what, [scriptblock]$step) {
    $before = CacheNames
    & $step
    $added = @(CacheNames | Where-Object { $_ -notin $before })
    if (-not $added.Count) { "  ${what}: added no file to the Installer folder" }
    foreach ($name in $added) { Record (Join-Path $installer $name) $what }
}

Invoke-Msiexec 'install M' @('/i', "`"$(Join-Path $build 'M-1.0.0.msi')`"")
Invoke-MsiexecAsSystem 'install S for SYSTEM' "/i `"$(Join-Path $build 'S-1.0.0.msi')`""

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
Record (ProductPackage $products.M.Code '' $machine) 'M, installed per machine, sharing patch P3 with S'
RecordWhatItAdds 'P3, a patch shared by M and SYSTEM''s S, applied by SYSTEM' {
    Invoke-MsiexecAsSystem 'apply P3 as SYSTEM' "/p `"$(Join-Path $build 'P3.msp')`""
}
RecordWhatItAdds 'V, a per-user package advertised by SYSTEM and not installed' {
    Invoke-MsiexecAsSystem 'advertise V per user as SYSTEM' "/ju `"$(Join-Path $build 'V-1.0.0.msi')`""
}

# X per user by SYSTEM from a copy in the Installer folder, then per machine from the build
# folder. The per-machine install replaces the one cached package the two share, and SYSTEM's own
# per-user source list goes on naming the copy.
$xMsi = Join-Path $build 'X-1.0.0.msi'
$beforeX = CacheNames
$xCopy = Place $xMsi 'a copy of X''s package in the Installer folder, the source of SYSTEM''s per-user installation of X, which is also installed per machine from elsewhere' 'holds' 'SYSTEM''s per-user installation of X names it on its source list'
Invoke-MsiexecAsSystem 'install X per user for SYSTEM from its copy in the Installer folder' "/i `"$xCopy`" ALLUSERS=`"`""
$xPacked = Packed $products.X.Code
$xUnmanaged = "Registry::HKEY_USERS\S-1-5-18\Software\Microsoft\Installer\Products\$xPacked"
$xManaged = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Managed\S-1-5-18\Installer\Products\$xPacked"
"  SYSTEM's per-user product key for X: unmanaged $(Test-Path -LiteralPath $xUnmanaged), managed $(Test-Path -LiteralPath $xManaged)"
foreach ($context in 1, $userUnmanaged, $machine) {
    "  X's cached package as SYSTEM's install left it, in context ${context}: $([ScenarioMsi]::Product($products.X.Code, $(if ($context -eq $machine) { '' } else { 'S-1-5-18' }), $context, 'LocalPackage'))"
}
if (-not ((Test-Path -LiteralPath $xUnmanaged) -or (Test-Path -LiteralPath $xManaged))) {
    throw "SYSTEM's install of X made no per-user product key, so the runner does not hold the shape X is for."
}
Invoke-Msiexec 'install X per machine' @('/i', "`"$xMsi`"", 'ALLUSERS=1')
$xSourceList = Join-Path $xUnmanaged 'SourceList'
if (Test-Path -LiteralPath $xSourceList) { "  SYSTEM's per-user source list for X names $((Get-ItemProperty -LiteralPath $xSourceList).PackageName)" }
if (-not (Test-Path -LiteralPath $xCopy)) { throw "X's copy $xCopy is no longer in the Installer folder after X's two installs." }
"  X's cached package per machine: $(ProductPackage $products.X.Code '' $machine)"
# Every file X's two installs left in the folder, the copy apart, which has its row.
$xLeft = @(CacheNames | Where-Object { $_ -notin $beforeX -and $_ -ne (Split-Path -Leaf $xCopy) })
if (-not $xLeft.Count) { throw "X's two installs left no cached package in the Installer folder." }
foreach ($name in $xLeft) { Record (Join-Path $installer $name) 'a file X''s installs, per user by SYSTEM and then per machine, left in the Installer folder' }

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

$null = Place (Join-Path $build 'D-1.0.0.msi') 'D, never installed'
$null = Place (Join-Path $build 'A-1.0.1.msi') 'a second copy of A at 1.0.1, the installed program''s own product code'
$null = Place (Join-Path $build 'Q.msp') 'Q, a patch for A that was never applied'
$null = Place $p1 'a second copy of P1''s cached patch under a new name'

$tsv = Join-Path $OutDir 'scenarios.tsv'
Set-Content -LiteralPath $tsv -Value $rows -Encoding utf8
"$($rows.Count - 1) test files set up; their list is $tsv"
