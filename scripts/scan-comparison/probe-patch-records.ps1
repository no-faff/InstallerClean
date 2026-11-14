# Asks Windows Installer about the patches of test programs it installs for the purpose, and
# prints each answer beside the registry values it could have come from. It answers, for a
# program installed per user and for one installed per machine:
#  - which registry value MsiGetPatchInfoEx answers a patch's Uninstallable property from;
#  - which registry keys MsiEnumPatchesEx lists a program's patches from.
# Each question is put three ways. The answer is read beside every candidate value. The calls
# are made under Process Monitor, and the registry events between two marker lookups are
# printed. And a candidate value is changed, the question asked again in a new process, and the
# value put back, which shows whether the answer follows the value.
#
# What it installs, from product.wxs and patch.wxs beside it:
#  - PU, per user, installed by the account the script runs as.
#  - PG, per user, advertised (msiexec /ju) and then installed by the same account. Where
#    Windows Installer records that as a managed installation, PG is read as one; the result
#    says which registry keys it made either way.
#  - PM, per machine.
#  - PO, with -SecondAccount: per user, installed and patched by a scheduled task run as a
#    local administrator account the script creates, and read once that account's registry
#    hive is no longer loaded. PO's records in HKLM are also copied under an account that
#    does not exist, which has no hive, and read there.
# Each program takes two patches of one family, the second superseding the first: R1 and R2
# on PU and PM (one cached patch for a per-user and a per-machine program), G1 and G2 on PG,
# O1 and O2 on PO.
#
# Everything it changes is on those programs' own registry keys. Before a change, the key as
# it stands is written to a file under -OutDir\journal, and the key is put back and read
# against that file afterwards. The script ends by removing the programs and reading every
# place they wrote to: each line of that section starts CLEAN or NOT CLEAN.
# -TeardownOnly does only that: it puts back any key a run that was cut short left changed,
# removes the programs and reads the same places.
#
# A step of its own set-up that fails stops it with the reason, after the teardown. A
# comparison with one side unread prints UNREAD and is counted apart from SAME and DIFFERENT.
#
# Run as an administrator in 64-bit PowerShell 7:
#   pwsh scripts/scan-comparison/probe-patch-records.ps1 -OutDir <folder>
# -Packages names a folder of packages a run elsewhere built, to install the same files;
# without it the script builds them and needs wix.exe on PATH. -Procmon names Procmon64.exe;
# without it the script downloads Process Monitor from Sysinternals and runs it only where
# its signature is valid and Microsoft's. The result is patch-records.txt in -OutDir.
param(
    [Parameter(Mandatory)][string]$OutDir,
    [string]$Packages = '',
    [string]$Procmon = '',
    [switch]$SecondAccount,
    [switch]$TeardownOnly,
    # Set by the script itself, on the child process that answers one installation's questions.
    [string]$Ask = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class PatchRecordsMsi
{
    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    static extern uint MsiGetPatchInfoExW(string patch, string product, string sid, int context,
        string property, StringBuilder value, ref uint length);

    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    static extern uint MsiGetProductInfoExW(string product, string sid, int context,
        string property, StringBuilder value, ref uint length);

    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    static extern uint MsiEnumPatchesExW(string product, string sid, int context, int filter,
        uint index, StringBuilder patch, StringBuilder target, out int targetContext,
        StringBuilder targetSid, ref uint targetSidLength);

    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    static extern uint MsiEnumProductsExW(string product, string sid, int context, uint index,
        StringBuilder installed, out int installedContext, StringBuilder installedSid,
        ref uint installedSidLength);

    // An empty string from PowerShell stands for no value: PowerShell passes its own null to a
    // string parameter as an empty string.
    static string OrNull(string text) { return string.IsNullOrEmpty(text) ? null : text; }

    public static string PatchInfo(string patch, string product, string sid, int context, string property)
    {
        var value = new StringBuilder(1024);
        uint length = 1024;
        uint rc = MsiGetPatchInfoExW(patch, product, OrNull(sid), context, property, value, ref length);
        if (rc == 234)
        {
            length++;
            value = new StringBuilder((int)length);
            rc = MsiGetPatchInfoExW(patch, product, OrNull(sid), context, property, value, ref length);
        }
        return rc == 0 ? "rc=0 '" + value.ToString() + "'" : "rc=" + rc;
    }

    public static string ProductInfo(string product, string sid, int context, string property)
    {
        var value = new StringBuilder(1024);
        uint length = 1024;
        uint rc = MsiGetProductInfoExW(product, OrNull(sid), context, property, value, ref length);
        return rc == 0 ? "rc=0 '" + value.ToString() + "'" : "rc=" + rc;
    }

    // One row for each patch the walk returns: patch|target product|context|sid. A walk that ends
    // on anything but "no more items" ends with a row that starts ERROR. withSidBuffer false
    // passes no buffer for the target account, and the rows carry no sid.
    public static string[] Patches(string product, string sid, int context, int filter, bool withSidBuffer)
    {
        var rows = new List<string>();
        for (uint index = 0; index < 10000; index++)
        {
            var patch = new StringBuilder(39);
            var target = new StringBuilder(39);
            var targetSid = withSidBuffer ? new StringBuilder(256) : null;
            uint length = withSidBuffer ? 256u : 0u;
            int targetContext;
            uint rc = MsiEnumPatchesExW(OrNull(product), OrNull(sid), context, filter, index, patch, target,
                out targetContext, targetSid, ref length);
            if (rc == 259) break;
            if (rc != 0 && !(rc == 234 && !withSidBuffer)) { rows.Add("ERROR rc=" + rc + " at index " + index); break; }
            rows.Add(patch.ToString().ToUpperInvariant() + "|" + target.ToString().ToUpperInvariant() + "|" + targetContext
                + "|" + (withSidBuffer ? targetSid.ToString() : ""));
        }
        return rows.ToArray();
    }

    // One row for each installation the walk returns: product|context|sid.
    public static string[] Products(string product, string sid, int context)
    {
        var rows = new List<string>();
        for (uint index = 0; index < 100000; index++)
        {
            var installed = new StringBuilder(39);
            var installedSid = new StringBuilder(256);
            uint length = 256;
            int installedContext;
            uint rc = MsiEnumProductsExW(OrNull(product), OrNull(sid), context, index, installed,
                out installedContext, installedSid, ref length);
            if (rc == 259) break;
            if (rc != 0) { rows.Add("ERROR rc=" + rc + " at index " + index); break; }
            rows.Add(installed.ToString().ToUpperInvariant() + "|" + installedContext + "|" + installedSid.ToString());
        }
        return rows.ToArray();
    }
}
'@

$everyone = 's-1-1-0'
$contextNames = @{ 1 = 'per user, managed'; 2 = 'per user, unmanaged'; 4 = 'per machine' }

# What Windows Installer answers about one installation's patches: the walk with every filter
# bit set, the walk of applied patches alone, the walk as the scan makes it (no buffer for the
# target account), and each named patch's State and Uninstallable. Asked with the
# installation's own sid and context; a per-machine installation has no sid.
function Read-Answers([string]$productCode, [string]$sid, [int]$context, [string[]]$patchCodes) {
    $answers = [ordered]@{
        WalkAll     = @([PatchRecordsMsi]::Patches($productCode, $sid, $context, 15, $true))
        WalkApplied = @([PatchRecordsMsi]::Patches($productCode, $sid, $context, 1, $true))
        WalkAsScan  = @([PatchRecordsMsi]::Patches($productCode, $sid, $context, 15, $false))
        Patches     = [ordered]@{}
    }
    foreach ($patchCode in $patchCodes) {
        $answers.Patches[$patchCode] = [ordered]@{
            State         = [PatchRecordsMsi]::PatchInfo($patchCode, $productCode, $sid, $context, 'State')
            Uninstallable = [PatchRecordsMsi]::PatchInfo($patchCode, $productCode, $sid, $context, 'Uninstallable')
        }
    }
    return $answers
}

# The child process: answers for one installation, as JSON on the output stream.
if ($Ask) {
    $parts = $Ask -split ';'
    Read-Answers $parts[0] $parts[1] ([int]$parts[2]) @($parts[3] -split ',') | ConvertTo-Json -Depth 6 -Compress
    return
}

$here = $PSScriptRoot
$null = New-Item -ItemType Directory -Force -Path $OutDir
$OutDir = (Resolve-Path -LiteralPath $OutDir).Path
$result = Join-Path $OutDir 'patch-records.txt'
$build = Join-Path $OutDir 'build'
$logs = Join-Path $OutDir 'logs'
$journal = Join-Path $OutDir 'journal'
$capture = Join-Path $OutDir 'capture'
$null = New-Item -ItemType Directory -Force -Path $logs, $journal, $capture
$installer = Join-Path $env:SystemRoot 'Installer'
$msiexec = Join-Path $env:SystemRoot 'System32\msiexec.exe'
$pwshExe = (Get-Process -Id $PID).Path
$mySid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$otherAccountName = 'icprobe'
$fast = 'MSIFASTINSTALL=1'
$script:procmonExe = ''

if ((Test-Path -LiteralPath $result) -and -not $TeardownOnly) {
    Rename-Item -LiteralPath $result -NewName ('patch-records.earlier-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.txt')
}
function Say([string]$text = '') {
    Write-Host $text
    Add-Content -LiteralPath $result -Value $text -Encoding utf8
}

$programs = [ordered]@{
    PU = @{ Code = '{498D5826-70DF-4F4C-8DFA-33873443CB55}'; Upgrade = '{16CF7A23-51D6-4F04-A6EE-22CD5420F715}'; Component = '{7B15281B-3674-49C0-BE5F-C822A2C48AE3}'; Scope = 'perUser'; What = 'per user, installed by the account the script runs as' }
    PG = @{ Code = '{50C91825-B2FE-4ABF-AF77-91C92C3BA954}'; Upgrade = '{567E8A93-8C60-4CBC-AB3B-383C588647C8}'; Component = '{2567896B-3842-4C5B-BD46-3812A7EBE260}'; Scope = 'perUser'; What = 'per user, advertised and then installed by the account the script runs as' }
    PM = @{ Code = '{F2B7AE1E-1056-4466-B10E-99DA09FAC0F6}'; Upgrade = '{5BBEA170-7CB6-439E-A950-617940986246}'; Component = '{23F08365-0F04-4CC5-963C-14E7EF191BB2}'; Scope = 'perMachine'; What = 'per machine' }
    PO = @{ Code = '{23A18B4B-53EA-434A-8521-F00FBFD6AFC5}'; Upgrade = '{ECBD7F7A-AEFE-4F63-9EF7-CC7C3F60094D}'; Component = '{6615BD33-B70A-4EF5-B5F6-8BD40604C981}'; Scope = 'perUser'; What = 'per user, installed by a second account whose registry hive is not loaded' }
}
# Each family's first patch is the one its second supersedes.
$patches = [ordered]@{
    R1 = @{ Code = '{2C8488FB-C233-4FBC-9B7C-4A3C35A9724B}'; Name = 'ICPatchRecordsFamily'; Family = '1.0.1.0'; Supersede = 'no'; Version = '1.0.1'; Targets = 'PU', 'PM' }
    R2 = @{ Code = '{2ECCDAC6-283D-4DF8-99E9-819D4746370A}'; Name = 'ICPatchRecordsFamily'; Family = '1.0.2.0'; Supersede = 'yes'; Version = '1.0.2'; Targets = 'PU', 'PM' }
    G1 = @{ Code = '{23B64E33-8147-49DA-BCB4-4402D981E682}'; Name = 'ICPatchRecordsAdvertisedFamily'; Family = '1.0.1.0'; Supersede = 'no'; Version = '1.0.1'; Targets = , 'PG' }
    G2 = @{ Code = '{C7B30F36-6AB9-4B2E-B43A-3D767F7FBFF1}'; Name = 'ICPatchRecordsAdvertisedFamily'; Family = '1.0.2.0'; Supersede = 'yes'; Version = '1.0.2'; Targets = , 'PG' }
    O1 = @{ Code = '{4888EF62-E840-438E-B16D-AEE4A96D43B7}'; Name = 'ICPatchRecordsOtherFamily'; Family = '1.0.1.0'; Supersede = 'no'; Version = '1.0.1'; Targets = , 'PO' }
    O2 = @{ Code = '{0AF9BE29-6A1F-498F-9400-BBD608D53A0D}'; Name = 'ICPatchRecordsOtherFamily'; Family = '1.0.2.0'; Supersede = 'yes'; Version = '1.0.2'; Targets = , 'PO' }
}
$patchesOf = @{ PU = 'R1', 'R2'; PM = 'R1', 'R2'; PG = 'G1', 'G2'; PO = 'O1', 'O2' }
# A patch code no patch has.
$madeUp = '{EFEC9638-9904-4D1B-8733-DEF1C576919E}'
# The scan comparison's X, read where it is installed.
$scenarioX = '{E6704E1A-DF47-478B-9FAD-3AA7FA64FDCD}'

# A product, patch, component or upgrade code in the packed form Windows Installer names its
# registry keys with.
function Packed([string]$code) {
    $h = $code.Trim('{', '}').Replace('-', '').ToUpperInvariant()
    $text = (-join $h.Substring(0, 8).ToCharArray()[7..0]) + (-join $h.Substring(8, 4).ToCharArray()[3..0]) + (-join $h.Substring(12, 4).ToCharArray()[3..0])
    for ($i = 16; $i -lt 32; $i += 2) { $text += $h[$i + 1]; $text += $h[$i] }
    return $text
}

# The name the result gives a code: its tag where it is one of the script's own.
function NameOf([string]$code) {
    foreach ($tag in $patches.Keys) { if ($patches[$tag].Code -eq $code) { return "$tag $code" } }
    foreach ($tag in $programs.Keys) { if ($programs[$tag].Code -eq $code) { return "$tag $code" } }
    if ($code -eq $madeUp) { return "the made-up code $code" }
    return $code
}
$codeOfPacked = @{}
foreach ($code in @($patches.Values | ForEach-Object { $_.Code }) + @($programs.Values | ForEach-Object { $_.Code }) + $madeUp) { $codeOfPacked[(Packed $code)] = $code }
function NameOfPacked([string]$packedName) {
    if ($codeOfPacked.ContainsKey($packedName)) { return NameOf $codeOfPacked[$packedName] }
    return $packedName
}

# ---- The registry, read and written through the 64-bit view.
$hklm = [Microsoft.Win32.RegistryKey]::OpenBaseKey('LocalMachine', 'Registry64')
$hku = [Microsoft.Win32.RegistryKey]::OpenBaseKey('Users', 'Registry64')
$installerKey = 'HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer'

# A location is HKLM\... or HKU\...; this answers its hive and the path under it.
function Split-Location([string]$location) {
    $cut = $location.IndexOf('\')
    $hive = if ($location.Substring(0, $cut) -eq 'HKLM') { $hklm } elseif ($location.Substring(0, $cut) -eq 'HKU') { $hku } else { throw "A registry location starts HKLM or HKU, not as $location does." }
    return $hive, $location.Substring($cut + 1)
}
function Open-Location([string]$location, [bool]$writable = $false) {
    $hive, $path = Split-Location $location
    return $hive.OpenSubKey($path, $writable)
}
function Test-Location([string]$location) {
    $key = Open-Location $location
    if ($null -eq $key) { return $false }
    $key.Dispose()
    return $true
}

# A key with its values and subkeys, as text that reads the same for the same contents. Null
# where the key is not there. A key or value that will not read throws.
function Read-Tree([string]$location) {
    $key = Open-Location $location
    if ($null -eq $key) { return $null }
    try { return (Get-TreeOf $key | ConvertTo-Json -Depth 30 -Compress) } finally { $key.Dispose() }
}
function Get-TreeOf($key) {
    $values = @(foreach ($name in ($key.GetValueNames() | Sort-Object)) {
            $kind = $key.GetValueKind($name)
            $data = $key.GetValue($name, $null, 'DoNotExpandEnvironmentNames')
            $text = switch ("$kind") {
                'Binary' { [Convert]::ToBase64String([byte[]]$data) }
                'MultiString' { , @($data) }
                default { if ($data -is [byte[]]) { [Convert]::ToBase64String($data) } else { $data } }
            }
            [ordered]@{ Name = $name; Kind = "$kind"; Data = $text }
        })
    $keys = [ordered]@{}
    foreach ($name in ($key.GetSubKeyNames() | Sort-Object)) {
        $sub = $key.OpenSubKey($name)
        try { $keys[$name] = Get-TreeOf $sub } finally { $sub.Dispose() }
    }
    return [ordered]@{ Entries = $values; Subkeys = $keys }
}

# Writes a key from Read-Tree's text, replacing whatever is there.
function Write-Tree([string]$location, [string]$treeText) {
    $hive, $path = Split-Location $location
    $hive.DeleteSubKeyTree($path, $false)
    $key = $hive.CreateSubKey($path)
    try { Set-TreeOf $key ($treeText | ConvertFrom-Json -AsHashtable) } finally { $key.Dispose() }
}
function Set-TreeOf($key, $tree) {
    foreach ($value in @($tree.Entries)) {
        $data = switch ($value.Kind) {
            'Binary' { , [Convert]::FromBase64String($value.Data) }
            'MultiString' { , [string[]]@($value.Data) }
            'DWord' { [int]$value.Data }
            'QWord' { [long]$value.Data }
            'None' { , [Convert]::FromBase64String("$($value.Data)") }
            default { "$($value.Data)" }
        }
        $key.SetValue($value.Name, $data, [Microsoft.Win32.RegistryValueKind]$value.Kind)
    }
    foreach ($name in @($tree.Subkeys.get_Keys())) {
        $sub = $key.CreateSubKey($name)
        try { Set-TreeOf $sub $tree.Subkeys[$name] } finally { $sub.Dispose() }
    }
}

# One value as the result prints it: its data and kind, 'no such value', 'no such key', or
# UNREAD with the reason.
function Show-Value([string]$location, [string]$name) {
    try {
        $key = Open-Location $location
        if ($null -eq $key) { return 'no such key' }
        try {
            if ($key.GetValueNames() -notcontains $name) { return 'no such value' }
            $data = $key.GetValue($name, $null, 'DoNotExpandEnvironmentNames')
            return "$(@($data) -join ' | ') ($($key.GetValueKind($name)))"
        }
        finally { $key.Dispose() }
    }
    catch { return "UNREAD: $($_.Exception.Message)" }
}
# The names of a key's values, or of its subkeys; null where the key is not there. Throws where
# it will not read.
function Get-Names([string]$location, [bool]$subkeys) {
    $key = Open-Location $location
    if ($null -eq $key) { return $null }
    try { if ($subkeys) { return , @($key.GetSubKeyNames()) } else { return , @($key.GetValueNames()) } } finally { $key.Dispose() }
}

# The access part of a key's security descriptor.
function Get-Access([string]$location) {
    $hive, $path = Split-Location $location
    $key = $hive.OpenSubKey($path, [Microsoft.Win32.RegistryKeyPermissionCheck]::ReadWriteSubTree, [System.Security.AccessControl.RegistryRights]'ReadPermissions')
    try { return $key.GetAccessControl('Access').GetSecurityDescriptorSddlForm('Access') } finally { $key.Dispose() }
}
# Adds or removes the one rule the script uses: everyone is denied reading the key's values and
# listing its subkeys, on that key alone.
function Set-ReadDenied([string]$location, [bool]$denied) {
    $hive, $path = Split-Location $location
    $key = $hive.OpenSubKey($path, [Microsoft.Win32.RegistryKeyPermissionCheck]::ReadWriteSubTree, [System.Security.AccessControl.RegistryRights]'ReadPermissions, ChangePermissions')
    try {
        $acl = $key.GetAccessControl('Access')
        $rule = [System.Security.AccessControl.RegistryAccessRule]::new(
            [Security.Principal.SecurityIdentifier]'S-1-1-0',
            [System.Security.AccessControl.RegistryRights]'QueryValues, EnumerateSubKeys',
            [System.Security.AccessControl.InheritanceFlags]::None,
            [System.Security.AccessControl.PropagationFlags]::None,
            [System.Security.AccessControl.AccessControlType]::Deny)
        if ($denied) { $acl.AddAccessRule($rule) } else { $null = $acl.RemoveAccessRuleSpecific($rule) }
        $key.SetAccessControl($acl)
    }
    finally { $key.Dispose() }
}

# ---- The journal: what a key held before the script changed it, on disk before the change.
$journalCount = 0
# Records $location as it stands and answers the entry. $kind is 'tree' for a key's contents and
# 'access' for its security descriptor.
function Start-Change([string]$what, [string]$location, [string]$kind) {
    $script:journalCount++
    $entry = [ordered]@{
        What = $what; Location = $location; Kind = $kind; State = 'changed'
        Before = if ($kind -eq 'tree') { Read-Tree $location } else { Get-Access $location }
        File = Join-Path $journal ('{0:d3}.json' -f $script:journalCount)
    }
    $entry | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $entry.File -Encoding utf8
    return $entry
}
# Puts an entry's key back and reads it against what the journal holds. Answers a line that
# starts CLEAN or NOT CLEAN and names the key.
function Undo-Change($entry) {
    $location = $entry.Location
    try {
        if ($entry.Kind -eq 'tree') {
            if ($null -eq $entry.Before) { $hive, $path = Split-Location $location; $hive.DeleteSubKeyTree($path, $false) }
            else { Write-Tree $location $entry.Before }
            $now = Read-Tree $location
        }
        else {
            Set-ReadDenied $location $false
            $now = Get-Access $location
        }
    }
    catch { return "NOT CLEAN: $location could not be put back as it was before '$($entry.What)': $($_.Exception.Message)" }
    if ($now -cne $entry.Before) { return "NOT CLEAN: $location does not read as it did before '$($entry.What)'. Before: $($entry.Before) Now: $now" }
    $entry.State = 'put back'
    $entry | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $entry.File -Encoding utf8
    return "CLEAN: $location reads as it did before '$($entry.What)'."
}

# ---- Running msiexec.
# Answers msiexec's exit code. The log is in the logs folder under the step's name. Every
# install, patch and removal passes MSIFASTINSTALL=1, with which Windows Installer makes no
# restore point.
function Invoke-Msiexec([string]$what, [string[]]$msiArgs) {
    $log = Join-Path $logs (($what -replace '[^A-Za-z0-9]+', '-') + '.log')
    $p = Start-Process -FilePath $msiexec -ArgumentList ($msiArgs + @('/qn', '/norestart', '/l*v', "`"$log`"")) -Wait -PassThru
    Say "msiexec, ${what}: exit code $($p.ExitCode)"
    return $p.ExitCode
}
function Assert-Msiexec([string]$what, [string[]]$msiArgs) {
    $code = Invoke-Msiexec $what $msiArgs
    if ($code -ne 0) { throw "msiexec, $what, exited $code; its log is in $logs." }
}
# One msiexec run as another account, from a scheduled task, waited on until it ends. Answers
# the task's result, or -1 where it had not ended after 300 s.
function Invoke-MsiexecAs([string]$what, [string]$msiArgs, [string]$account, [string]$password) {
    $taskName = 'InstallerClean patch records, ' + ($what -replace '[^A-Za-z0-9 ]+', '')
    $log = Join-Path $logs (($what -replace '[^A-Za-z0-9]+', '-') + '.log')
    $action = New-ScheduledTaskAction -Execute $msiexec -Argument "$msiArgs /qn /norestart /l*v `"$log`""
    $null = Register-ScheduledTask -TaskName $taskName -Action $action -User $account -Password $password -RunLevel Highest -Force
    Start-ScheduledTask -TaskName $taskName
    # 267011 is the task's result before its first run and 267009 its result while it runs.
    $clock = [Diagnostics.Stopwatch]::StartNew()
    do {
        Start-Sleep -Seconds 1
        $task = Get-ScheduledTask -TaskName $taskName
        $taskResult = (Get-ScheduledTaskInfo -TaskName $taskName).LastTaskResult
        $ended = $task.State -ne 'Running' -and $taskResult -notin 267011, 267009
    } while (-not $ended -and $clock.Elapsed.TotalSeconds -lt 300)
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    Say "msiexec as $account, ${what}: $(if ($ended) { "exit code $taskResult" } else { 'had not ended after 300 s' })"
    if ($ended) { return $taskResult } else { return -1 }
}

# ---- What the programs can leave behind, read by what is there.
$ownCodes = @($programs.Values | ForEach-Object { $_.Code; $_.Upgrade; $_.Component }) + @($patches.Values | ForEach-Object { $_.Code }) + $madeUp
$ownNames = [regex]::new(((@($ownCodes | ForEach-Object { Packed $_ }) + @($ownCodes | ForEach-Object { [regex]::Escape($_) }) + 'ICScanComparisonP') -join '|'), 'IgnoreCase')

# Every key and value under $location whose name holds one of the script's own codes, in either
# form, or the name its programs write under. $unread takes each key that would not open.
function Find-Own([string]$location, [System.Collections.Generic.List[string]]$found, [System.Collections.Generic.List[string]]$unread) {
    try { $key = Open-Location $location } catch { $unread.Add("$location ($($_.Exception.Message))"); return }
    if ($null -eq $key) { return }
    try {
        foreach ($name in $key.GetValueNames()) {
            if ($ownNames.IsMatch($name)) { $found.Add("the value $name of $location") }
        }
        foreach ($name in $key.GetSubKeyNames()) {
            if ($ownNames.IsMatch($name)) { $found.Add("the key $location\$name") }
            else { Find-Own "$location\$name" $found $unread }
        }
    }
    catch { $unread.Add("$location ($($_.Exception.Message))") }
    finally { $key.Dispose() }
}
# The places Windows Installer and the programs write to, for the machine and for each account
# whose hive is loaded. An account's classes hive is read through its Software\Classes key.
function Get-OwnPlaces {
    $places = @(
        $installerKey,
        'HKLM\SOFTWARE\Classes\Installer',
        'HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
        'HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall',
        'HKLM\SOFTWARE\NoFaffTest')
    foreach ($sid in $hku.GetSubKeyNames() | Where-Object { $_ -notlike '*_Classes' }) {
        $places += "HKU\$sid\Software\Microsoft\Installer", "HKU\$sid\Software\Classes\Installer",
        "HKU\$sid\Software\Microsoft\Windows\CurrentVersion\Uninstall", "HKU\$sid\Software\NoFaffTest"
    }
    return $places
}
function Find-OwnEverywhere([System.Collections.Generic.List[string]]$found, [System.Collections.Generic.List[string]]$unread) {
    foreach ($place in Get-OwnPlaces) { Find-Own $place $found $unread }
}
function Get-CacheNames { @(Get-ChildItem -LiteralPath $installer -File -Force | Where-Object Extension -in '.msi', '.msp' | ForEach-Object Name) }
function Get-OtherAccountSid {
    try { return ([Security.Principal.NTAccount]::new("$env:COMPUTERNAME\$otherAccountName")).Translate([Security.Principal.SecurityIdentifier]).Value }
    catch { return '' }
}
# Waits for an account's hive to leave HKU, and says what it took. A hive still loaded after
# $seconds is waited on again once the Windows Installer service is stopped, and after that is
# unloaded with reg.exe. Answers whether the hive is loaded at the end.
function Wait-HiveUnloaded([string]$sid, [int]$seconds) {
    $isLoaded = { $hku.GetSubKeyNames() -contains $sid }
    $wait = {
        $clock = [Diagnostics.Stopwatch]::StartNew()
        while ((& $isLoaded) -and $clock.Elapsed.TotalSeconds -lt $seconds) { Start-Sleep -Seconds 2 }
        [int]$clock.Elapsed.TotalSeconds
    }
    $waited = & $wait
    if (-not (& $isLoaded)) { Say "The hive HKU\$sid left by itself, $waited s after the account's last task ended."; return $false }
    Say "The hive HKU\$sid was still loaded $waited s after the account's last task ended."
    $service = Get-Service -Name msiserver
    if ($service.Status -ne 'Stopped') {
        try { Stop-Service -Name msiserver -Force; Say 'The Windows Installer service was running and is stopped.' }
        catch { Say "The Windows Installer service was not stopped: $($_.Exception.Message)" }
    }
    else { Say 'The Windows Installer service was not running.' }
    $waited = & $wait
    if (-not (& $isLoaded)) { Say "The hive HKU\$sid left $waited s after that."; return $false }
    $owned = @(Get-Process -IncludeUserName | Where-Object { $_.UserName -like "*\$otherAccountName" })
    Say "Processes running as the account ($($owned.Count)): $(($owned | ForEach-Object { "$($_.Name) $($_.Id)" }) -join ', ')"
    if ($owned.Count) {
        $owned | Stop-Process -Force -ErrorAction SilentlyContinue
        $waited = & $wait
        if (-not (& $isLoaded)) { Say "The hive HKU\$sid left $waited s after those processes were stopped."; return $false }
    }
    foreach ($name in "${sid}_Classes", $sid) {
        $out = & reg.exe unload "HKU\$name" 2>&1
        Say "reg unload HKU\${name}: exit code $LASTEXITCODE ($(($out | Out-String).Trim()))"
    }
    $loaded = & $isLoaded
    Say "After reg unload the hive HKU\$sid is loaded: $loaded."
    return $loaded
}
function New-Password { return (-join (1..10 | ForEach-Object { [char](Get-Random -Minimum 97 -Maximum 123) })) + 'A9!x' }

# The keys Windows Installer or a program makes for an account the first time it needs them. The
# probe records which were there at its start, and its teardown removes one that was not and
# now holds no value at any depth.
function Get-FirstUseKeys {
    return @("$installerKey\UserData\$mySid", "$installerKey\Managed\$mySid", "HKU\$mySid\Software\Microsoft\Installer",
        "HKU\$mySid\Software\Classes\Installer", "HKU\$mySid\Software\NoFaffTest", 'HKLM\SOFTWARE\NoFaffTest')
}
# Removes $location where it holds no value at any depth, and says so. Answers 1 where it
# holds a value and is left, and 0 otherwise.
function Remove-EmptyKey([string]$location, [string]$what) {
    try {
        $tree = Read-Tree $location
        if ($null -eq $tree) { Say "CLEAN: $location is not there."; return 0 }
        if ($tree -match '"Name":') { Say "NOT CLEAN: $location, $what, holds values and is left: $tree"; return 1 }
        $hive, $path = Split-Location $location
        $hive.DeleteSubKeyTree($path, $false)
        if (Test-Location $location) { Say "NOT CLEAN: $location, $what, was not removed."; return 1 }
        Say "CLEAN: $location, $what, held no value at any depth and is removed. It held the keys: $tree"
        return 0
    }
    catch { Say "NOT CLEAN: $location, $what, was not read or not removed: $($_.Exception.Message)"; return 1 }
}

# ---- The teardown: puts back what the journal says is changed, removes the programs, and
# reads every place they wrote to. Answers the number of NOT CLEAN lines.
function Invoke-Teardown {
    Say ''
    Say '==== TEARDOWN: WHAT WAS PUT BACK, WHAT WAS REMOVED AND WHAT IS LEFT'
    $notClean = 0
    foreach ($file in Get-ChildItem -LiteralPath $journal -Filter '*.json' | Sort-Object Name -Descending) {
        $entry = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json -AsHashtable
        if ($entry.State -eq 'put back') { continue }
        $line = Undo-Change $entry
        if ($line -like 'NOT CLEAN*') { $notClean++ }
        Say $line
    }

    $otherSid = Get-OtherAccountSid
    if ($otherSid) {
        # The account's password is set again here, so that a run cut short can still remove PO.
        $password = New-Password
        & net.exe user $otherAccountName $password | Out-Null
        $code = Invoke-MsiexecAs 'remove PO as the second account' "/x $($programs.PO.Code) $fast" "$env:COMPUTERNAME\$otherAccountName" $password
        if ($code -notin 0, 1605) { $notClean++; Say "NOT CLEAN: removing PO as the second account ended with $code." }
        $null = Wait-HiveUnloaded $otherSid 30
        & net.exe user $otherAccountName /delete | Out-Null
        Get-CimInstance -ClassName Win32_UserProfile -Filter "SID='$otherSid'" | ForEach-Object {
            $profilePath = $_.LocalPath
            try { $_ | Remove-CimInstance; Say "CLEAN: the second account's profile $profilePath is removed." }
            catch { $notClean++; Say "NOT CLEAN: the second account's profile $profilePath was not removed: $($_.Exception.Message)" }
        }
        if (Get-OtherAccountSid) { $notClean++; Say "NOT CLEAN: the account $otherAccountName is still there." }
        else { Say "CLEAN: the account $otherAccountName is not there." }
        foreach ($left in "$installerKey\UserData\$otherSid", "$installerKey\Managed\$otherSid") {
            $notClean += Remove-EmptyKey $left 'the removed account''s key'
        }
    }
    foreach ($tag in 'PG', 'PU', 'PM') {
        $code = Invoke-Msiexec "remove $tag" @('/x', $programs[$tag].Code, $fast)
        if ($code -notin 0, 1605) { $notClean++; Say "NOT CLEAN: removing $tag ended with $code (0 is removed, 1605 is not installed)." }
    }

    $found = [System.Collections.Generic.List[string]]::new()
    $unread = [System.Collections.Generic.List[string]]::new()
    Find-OwnEverywhere $found $unread
    Say "Read for anything named by one of the probe's codes: $((Get-OwnPlaces) -join '; ')"
    if ($found.Count) { $notClean += $found.Count; foreach ($item in $found) { Say "NOT CLEAN: $item is still there." } }
    else { Say "CLEAN: no key or value there is named by any of the probe's product, patch, component or upgrade codes." }
    if ($unread.Count) { $notClean += $unread.Count; foreach ($item in $unread) { Say "NOT CLEAN: not read, so not known to be clean: $item" } }

    foreach ($folder in @(
            (Join-Path $env:LOCALAPPDATA 'ICScanComparisonPU'), (Join-Path $env:LOCALAPPDATA 'ICScanComparisonPG'),
            (Join-Path $env:ProgramFiles 'ICScanComparisonPM'))) {
        if (Test-Path -LiteralPath $folder) { $notClean++; Say "NOT CLEAN: the folder $folder is still there." }
        else { Say "CLEAN: the folder $folder is not there." }
    }
    $firstUseList = Join-Path $OutDir 'keys-at-start.txt'
    if (Test-Path -LiteralPath $firstUseList) {
        $thereAtStart = @(Get-Content -LiteralPath $firstUseList)
        foreach ($location in Get-FirstUseKeys) {
            if ($location -in $thereAtStart) { Say "AS IT WAS: $location was there before the probe started and is left alone." }
            else { $notClean += Remove-EmptyKey $location 'a key made for the probe''s programs' }
        }
    }
    else { $notClean++; Say "NOT CLEAN: the list of keys that were there at the start, $firstUseList, is not there, so a key made for the probe's programs and left empty is not known from one that was there before." }
    $startList = Join-Path $OutDir 'installer-folder-at-start.txt'
    if (Test-Path -LiteralPath $startList) {
        $atStart = @(Get-Content -LiteralPath $startList)
        $added = @(Get-CacheNames | Where-Object { $_ -notin $atStart })
        if ($added.Count) { $notClean += $added.Count; foreach ($name in $added) { Say "NOT CLEAN: the Installer folder holds $name, which it did not hold when the probe started." } }
        else { Say "CLEAN: the Installer folder holds no .msi or .msp file it did not hold when the probe started ($($atStart.Count) then)." }
    }
    else { $notClean++; Say "NOT CLEAN: the list of the Installer folder's files at the start, $startList, is not there, so what the probe added to the folder is not known." }
    $running = @(Get-Process -Name 'Procmon*' -ErrorAction SilentlyContinue)
    if ($running.Count) { $notClean++; Say "NOT CLEAN: Process Monitor is still running ($(($running | ForEach-Object { "$($_.Name) $($_.Id)" }) -join ', '))." }
    else { Say 'CLEAN: Process Monitor is not running.' }
    Say "TEARDOWN: $(if ($notClean) { "NOT CLEAN, $notClean lines above say what is left" } else { 'CLEAN' })"
    return $notClean
}

# ---- Process Monitor.
# Runs Process Monitor with $procmonArgs and waits for that run to end, for up to $seconds.
function Invoke-Procmon([string[]]$procmonArgs, [int]$seconds) {
    $p = Start-Process -FilePath $script:procmonExe -ArgumentList $procmonArgs -PassThru
    if (-not $p.WaitForExit($seconds * 1000)) { throw "Process Monitor ($($procmonArgs -join ' ')) had not ended after $seconds s." }
}
function Stop-Procmon {
    if (-not $script:procmonExe) { return }
    try { Invoke-Procmon @('/Terminate') 60 } catch { }
    Start-Sleep -Seconds 3
    Get-Process -Name 'Procmon*' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}
# Answers the Procmon64.exe to run, or throws with the reason there is none.
function Resolve-Procmon {
    if ($Procmon) {
        if (-not (Test-Path -LiteralPath $Procmon)) { throw "Process Monitor is not at $Procmon." }
        $exe = $Procmon
    }
    else {
        $zip = Join-Path $capture 'ProcessMonitor.zip'
        Invoke-WebRequest -Uri 'https://download.sysinternals.com/files/ProcessMonitor.zip' -OutFile $zip
        Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $capture 'procmon') -Force
        $exe = Join-Path $capture 'procmon\Procmon64.exe'
    }
    $signature = Get-AuthenticodeSignature -LiteralPath $exe
    $signer = if ($signature.SignerCertificate) { $signature.SignerCertificate.Subject } else { 'nobody' }
    Say "Process Monitor: $exe, version $((Get-Item -LiteralPath $exe).VersionInfo.FileVersion), SHA-256 $((Get-FileHash -Algorithm SHA256 -LiteralPath $exe).Hash), signature $($signature.Status), signed by $signer"
    if ($signature.Status -ne 'Valid' -or $signer -notmatch 'O=Microsoft Corporation') { throw 'Process Monitor is not run: its signature is not a valid one of Microsoft''s.' }
    return $exe
}
# A lookup of a key that is not there, which the capture shows as one event naming $name.
function Set-Marker([string]$name) { $null = $hku.OpenSubKey("$mySid\Software\ICPatchRecordsMarker-$name") }

# ---- An installation and its registry keys.
# Each installation Windows Installer lists for a program by its code, across every account and
# context, as the scan asks: objects with Sid and Context. $expected is added, marked as not
# listed, where the walk does not return it.
function Get-Installations([string]$tag, [string]$expectedSid, [int]$expectedContext) {
    $code = $programs[$tag].Code
    $rows = @([PatchRecordsMsi]::Products($code, $everyone, 7))
    $list = @(foreach ($row in $rows | Where-Object { $_ -notlike 'ERROR*' }) {
            $cells = $row -split '\|'
            [pscustomobject]@{ Tag = $tag; Code = $code; Sid = $cells[2]; Context = [int]$cells[1]; Listed = $true }
        })
    if (-not ($list | Where-Object { $_.Context -eq $expectedContext -and $_.Sid -eq $expectedSid })) {
        $list += [pscustomobject]@{ Tag = $tag; Code = $code; Sid = $expectedSid; Context = $expectedContext; Listed = $false }
    }
    return , $list
}
function Get-Label($installation) {
    $who = if ($installation.Context -eq 4) { 'no account' } elseif ($installation.Sid -eq $mySid) { "this account, $($installation.Sid)" } else { "the account $($installation.Sid)" }
    $note = if ($installation.PSObject.Properties['Note']) { "; $($installation.Note)" } else { '' }
    return "$($installation.Tag) ($($programs[$installation.Tag].What); asked as $($contextNames[$installation.Context]), $who$note$(if (-not $installation.Listed) { '; Windows Installer does not list this installation' }))"
}
# The key Windows Installer keeps an installation's patch records under.
function Get-RecordsKey($installation) {
    $account = if ($installation.Context -eq 4) { 'S-1-5-18' } else { $installation.Sid }
    return "$installerKey\UserData\$account\Products\$(Packed $installation.Code)\Patches"
}
# The Patches key under the product key that advertises an installation, by its context.
function Get-AdvertisedKey([string]$code, [string]$sid, [int]$context) {
    switch ($context) {
        4 { return "HKLM\SOFTWARE\Classes\Installer\Products\$(Packed $code)\Patches" }
        2 { return "HKU\$sid\Software\Microsoft\Installer\Products\$(Packed $code)\Patches" }
        1 { return "$installerKey\Managed\$sid\Installer\Products\$(Packed $code)\Patches" }
    }
}

# An installation's answers on one line, each code under the name the result gives it.
function Format-Answers($answers) {
    $walk = { param($rows) if (-not @($rows).Count) { 'no patch' } else { (@($rows) | ForEach-Object { if ($_ -like 'ERROR*') { $_ } else { $cells = $_ -split '\|'; (NameOf $cells[0]) -replace ' \{.*$', '' } }) -join ', ' } }
    $text = "the walk lists $(& $walk $answers.WalkAll); the walk of applied patches lists $(& $walk $answers.WalkApplied); the walk as the scan makes it lists $(& $walk $answers.WalkAsScan)"
    foreach ($patchCode in @($answers.Patches.get_Keys())) {
        $text += "; $((NameOf $patchCode) -replace ' \{.*$', '') State $($answers.Patches[$patchCode].State), Uninstallable $($answers.Patches[$patchCode].Uninstallable)"
    }
    return $text
}
function Get-PatchCodes([string]$tag) { return @($patchesOf[$tag] | ForEach-Object { $patches[$_].Code }) + $madeUp }
function Read-Here($installation) { return Format-Answers (Read-Answers $installation.Code $installation.Sid $installation.Context (Get-PatchCodes $installation.Tag)) }
# The same answers from a new PowerShell process, which has read nothing before.
function Read-Fresh($installation) {
    $spec = "$($installation.Code);$($installation.Sid);$($installation.Context);$((Get-PatchCodes $installation.Tag) -join ',')"
    $json = & $pwshExe -NoProfile -File $PSCommandPath -OutDir $OutDir -Ask $spec
    if ($LASTEXITCODE -ne 0 -or -not $json) { throw "The child process that asks about $($installation.Tag) exited $LASTEXITCODE and answered '$json'." }
    return Format-Answers (($json | Select-Object -Last 1) | ConvertFrom-Json -AsHashtable)
}

# ==== The run.
$failures = [System.Collections.Generic.List[string]]::new()
$notClean = 0
$stopped = $null

Say "probe-patch-records.ps1, $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$windows = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion'
$msiDll = Get-Item -LiteralPath (Join-Path $env:SystemRoot 'System32\msi.dll')
Say "Windows build $($windows.CurrentBuild).$($windows.UBR), $($windows.EditionID); msi.dll $($msiDll.VersionInfo.FileVersion); PowerShell $($PSVersionTable.PSVersion); 64-bit process: $([Environment]::Is64BitProcess)"
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Say "Run as $([Security.Principal.WindowsIdentity]::GetCurrent().Name), $mySid; administrator: $isAdmin"
if (-not $isAdmin -or -not [Environment]::Is64BitProcess) { Say 'STOPPED: the probe needs an administrator and a 64-bit process. Nothing was done.'; throw 'Not an administrator in a 64-bit process.' }

if ($TeardownOnly) {
    $script:procmonExe = if ($Procmon) { $Procmon } else { Join-Path $capture 'procmon\Procmon64.exe' }
    if (Test-Path -LiteralPath $script:procmonExe) { Stop-Procmon }
    $left = Invoke-Teardown
    if ($left) { throw "The teardown left $left things; $result names each." }
    return
}

$found = [System.Collections.Generic.List[string]]::new()
$unread = [System.Collections.Generic.List[string]]::new()
Find-OwnEverywhere $found $unread
if ($found.Count -or (Get-OtherAccountSid)) {
    Say 'STOPPED: the probe''s programs, or what an earlier run left of them, are already here. Nothing was done. Run it with -TeardownOnly first.'
    foreach ($item in $found) { Say "  $item" }
    throw 'An earlier run''s programs are still here.'
}
$pending = @(Get-ChildItem -LiteralPath $journal -Filter '*.json' | Where-Object { (Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json).State -ne 'put back' })
if ($pending.Count) {
    Say "STOPPED: the journal in $journal holds $($pending.Count) changes an earlier run did not put back. Nothing was done. Run it with -TeardownOnly first."
    throw 'An earlier run''s changes are not put back.'
}
Get-ChildItem -LiteralPath $journal -Filter '*.json' | Remove-Item
Set-Content -LiteralPath (Join-Path $OutDir 'installer-folder-at-start.txt') -Value (Get-CacheNames) -Encoding utf8
Set-Content -LiteralPath (Join-Path $OutDir 'keys-at-start.txt') -Value @(Get-FirstUseKeys | Where-Object { Test-Location $_ }) -Encoding utf8
$sysinternalsAtStart = Test-Location "HKU\$mySid\Software\Sysinternals\Process Monitor"

$installations = @()
try {
    # ---- The packages.
    Say ''
    Say '==== SET-UP: THE PACKAGES, THE INSTALLS AND THE PATCHES'
    if ($Packages) {
        $build = (Resolve-Path -LiteralPath $Packages).Path
        Say "Packages taken from ${build}:"
    }
    else {
        $null = New-Item -ItemType Directory -Force -Path $build
        $payloads = Join-Path $build 'payload'
        $n = 0
        foreach ($version in '1.0.0', '1.0.1', '1.0.2') {
            $folder = Join-Path $payloads $version
            $null = New-Item -ItemType Directory -Force -Path $folder
            Set-Content -LiteralPath (Join-Path $folder 'payload.txt') -Encoding ascii -Value ("InstallerClean patch records payload, version $version. " + ('x' * (64 + 37 * $n)))
            $n++
        }
        foreach ($tag in $programs.Keys) {
            $p = $programs[$tag]
            foreach ($version in '1.0.0', '1.0.1', '1.0.2') {
                $out = & wix build -arch x64 (Join-Path $here 'product.wxs') -d "Tag=$tag" -d "Version=$version" -d "Scope=$($p.Scope)" `
                    -d "PayloadDir=$(Join-Path $payloads $version)" -d "ProductCode=$($p.Code)" -d "UpgradeCode=$($p.Upgrade)" -d "ComponentGuid=$($p.Component)" `
                    -o (Join-Path $build "$tag-$version.msi") 2>&1
                if ($LASTEXITCODE -ne 0) { $out | ForEach-Object { Say "  $_" }; throw "wix build of $tag $version exited $LASTEXITCODE." }
            }
        }
        foreach ($tag in $patches.Keys) {
            $p = $patches[$tag]
            $defines = @('-d', "PatchId=$($p.Code.Trim('{', '}'))", '-d', "Tag=$tag", '-d', "Family=$($p.Name)", '-d', "FamilyVersion=$($p.Family)", '-d', "Supersede=$($p.Supersede)", '-d', 'Ext=wixpdb')
            foreach ($target in $p.Targets) { $defines += @('-d', "Base$target=$target-1.0.0", '-d', "Upd$target=$target-$($p.Version)") }
            $out = & wix build (Join-Path $here 'patch.wxs') @defines -bindpath $build -bindpath:target (Join-Path $payloads '1.0.0') `
                -bindpath:update (Join-Path $payloads $p.Version) -o (Join-Path $build "$tag.msp") 2>&1
            if ($LASTEXITCODE -ne 0) { $out | ForEach-Object { Say "  $_" }; throw "wix build of patch $tag exited $LASTEXITCODE." }
        }
        Say "Packages built in ${build}:"
    }
    foreach ($name in @($programs.Keys | ForEach-Object { "$_-1.0.0.msi" }) + @($patches.Keys | ForEach-Object { "$_.msp" })) {
        $file = Join-Path $build $name
        if (-not (Test-Path -LiteralPath $file)) { throw "The package $file is not there." }
        Say "  $name, $((Get-Item -LiteralPath $file).Length) bytes, SHA-256 $((Get-FileHash -Algorithm SHA256 -LiteralPath $file).Hash)"
    }
    function Package([string]$name) { return "`"$(Join-Path $build $name)`"" }

    # ---- PM and PU, which every later section needs.
    Assert-Msiexec 'install PM' @('/i', (Package 'PM-1.0.0.msi'), $fast)
    Assert-Msiexec 'install PU for this account' @('/i', (Package 'PU-1.0.0.msi'), $fast)
    Assert-Msiexec 'apply R1 to PU and PM' @('/p', (Package 'R1.msp'), $fast)
    Assert-Msiexec 'apply R2 to PU and PM' @('/p', (Package 'R2.msp'), $fast)

    # Whether a program's two patches are in the states they were built for: the first
    # superseded (2) and the second applied (1). Answers the reason they are not, or ''.
    function Test-Shape($installation) {
        $first, $second = $patchesOf[$installation.Tag]
        $firstState = [PatchRecordsMsi]::PatchInfo($patches[$first].Code, $installation.Code, $installation.Sid, $installation.Context, 'State')
        $secondState = [PatchRecordsMsi]::PatchInfo($patches[$second].Code, $installation.Code, $installation.Sid, $installation.Context, 'State')
        if ($firstState -eq "rc=0 '2'" -and $secondState -eq "rc=0 '1'") { return '' }
        return "$first State is $firstState and $second State is $secondState, where the probe needs 2 (superseded) and 1 (applied)"
    }
    # The programs each later section reads: an installation with its patches in shape.
    $inShape = @()
    foreach ($pair in @(@('PM', '', 4), @('PU', $mySid, 2))) {
        $list = Get-Installations $pair[0] $pair[1] $pair[2]
        $installations += $list
        $own = @($list | Where-Object { $_.Context -eq $pair[2] -and $_.Sid -eq $pair[1] })[0]
        $why = if (-not $own.Listed) { 'Windows Installer does not list it in the context it was installed for' } else { Test-Shape $own }
        if ($why) { throw "$($pair[0]) is not in the shape the probe needs: $why." }
        $inShape += $own
    }

    # ---- PG: an attempt at a per-user managed installation.
    $advertised = Invoke-Msiexec 'advertise PG to this account' @('/ju', (Package 'PG-1.0.0.msi'))
    Say "After advertising PG, its product key is there: managed (under $installerKey\Managed\$mySid) $(Test-Location "$installerKey\Managed\$mySid\Installer\Products\$(Packed $programs.PG.Code)"); unmanaged (under HKU\$mySid\Software\Microsoft\Installer) $(Test-Location "HKU\$mySid\Software\Microsoft\Installer\Products\$(Packed $programs.PG.Code)")"
    $pgSteps = @($advertised,
        (Invoke-Msiexec 'install PG for this account' @('/i', (Package 'PG-1.0.0.msi'), $fast)),
        (Invoke-Msiexec 'apply G1 to PG' @('/p', (Package 'G1.msp'), $fast)),
        (Invoke-Msiexec 'apply G2 to PG' @('/p', (Package 'G2.msp'), $fast)))
    $pgList = Get-Installations 'PG' $mySid 1
    $installations += $pgList
    $pgListed = @($pgList | Where-Object Listed)
    if ($pgSteps | Where-Object { $_ -ne 0 }) { $failures.Add("PG: a step of its install ended with other than 0 ($($pgSteps -join ', ')), so PG is read as it stands and is not changed.") }
    elseif ($pgListed.Count -ne 1) { $failures.Add("PG: Windows Installer lists $($pgListed.Count) installations of it, so PG is read as it stands and is not changed.") }
    else {
        $why = Test-Shape $pgListed[0]
        if ($why) { $failures.Add("PG: $why, so PG is read as it stands and is not changed.") }
        else {
            $inShape += $pgListed[0]
            Say "PG IS RECORDED AS $($contextNames[$pgListed[0].Context].ToUpperInvariant())."
        }
    }

    # ---- PO: a per-user program of an account whose hive is not loaded.
    $otherSid = ''
    if ($SecondAccount) {
        $password = New-Password
        & net.exe user $otherAccountName $password /add | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "The account $otherAccountName was not created (net user exited $LASTEXITCODE)." }
        & net.exe localgroup Administrators $otherAccountName /add | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "The account $otherAccountName was not made an administrator (net localgroup exited $LASTEXITCODE)." }
        $otherSid = Get-OtherAccountSid
        Say "The second account is $env:COMPUTERNAME\$otherAccountName, $otherSid."
        $account = "$env:COMPUTERNAME\$otherAccountName"
        $poSteps = @(
            (Invoke-MsiexecAs 'install PO as the second account' "/i $(Package 'PO-1.0.0.msi') $fast" $account $password),
            (Invoke-MsiexecAs 'apply O1 as the second account' "/p $(Package 'O1.msp') $fast" $account $password),
            (Invoke-MsiexecAs 'apply O2 as the second account' "/p $(Package 'O2.msp') $fast" $account $password))
        $loaded = Wait-HiveUnloaded $otherSid 30
        $poList = Get-Installations 'PO' $otherSid 2
        $installations += $poList
        if ($poSteps | Where-Object { $_ -ne 0 }) { $failures.Add("PO: a step of its install ended with other than 0 ($($poSteps -join ', ')), so PO is read as it stands and is not changed.") }
        elseif ($loaded) { $failures.Add('PO: the second account''s hive is still loaded, so what follows about PO is not about an account whose hive is not loaded.') }
        # The second account's records copied under an account that does not exist, so has no
        # hive to load: what Windows Installer holds for PO in HKLM, with nothing in HKU.
        $absentSid = $otherSid -replace '-\d+$', '-64999'
        $absentKey = "$installerKey\UserData\$absentSid"
        $known = try { $null = ([Security.Principal.SecurityIdentifier]$absentSid).Translate([Security.Principal.NTAccount]); $true } catch { $false }
        if ($known -or (Test-Location $absentKey) -or ($hku.GetSubKeyNames() -contains $absentSid)) {
            $failures.Add("PO: $absentSid names an account or holds keys already, so no copy of PO's records is made under it.")
        }
        elseif (-not ($poSteps | Where-Object { $_ -ne 0 })) {
            $null = Start-Change "a copy of the second account's UserData key, $installerKey\UserData\$otherSid, made under an account that does not exist" $absentKey 'tree'
            Write-Tree $absentKey (Read-Tree "$installerKey\UserData\$otherSid")
            Say "A copy of $installerKey\UserData\$otherSid is made at $absentKey; $absentSid is no account and has no hive under HKU."
            $keyedRows = @([PatchRecordsMsi]::Products($programs.PO.Code, $everyone, 7))
            $installations += [pscustomobject]@{
                Tag = 'PO'; Code = $programs.PO.Code; Sid = $absentSid; Context = 2
                Listed = [bool]($keyedRows | Where-Object { $_ -like "*|2|$absentSid" })
                Note = 'a copy of the second account''s records under an account that does not exist'
            }
            $inShape += $installations[-1]
        }
        $poRecords = "$installerKey\UserData\$otherSid\Products\$(Packed $programs.PO.Code)\Patches"
        if (-not ($poSteps | Where-Object { $_ -ne 0 }) -and (Test-Location $poRecords)) { $inShape += @($poList | Where-Object { $_.Context -eq 2 -and $_.Sid -eq $otherSid })[0] }
    }

    # ---- Section 1.
    Say ''
    Say '==== 1. WHAT WINDOWS INSTALLER LISTS FOR EACH PROGRAM, AND WHICH REGISTRY KEYS HOLD IT'
    $everyRow = @([PatchRecordsMsi]::Products('', $everyone, 7))
    Say "MsiEnumProductsEx with no product code, every account and every context returns $($everyRow.Count) rows$(if ($everyRow | Where-Object { $_ -like 'ERROR*' }) { ', the last an error: ' + $everyRow[-1] })."
    $accounts = @((Get-Names "$installerKey\UserData" $true) | Where-Object { $_ })
    $managedAccounts = @((Get-Names "$installerKey\Managed" $true) | Where-Object { $_ })
    $loadedHives = @($hku.GetSubKeyNames() | Where-Object { $_ -notlike '*_Classes' })
    Say "Accounts with a key under UserData: $($accounts -join ', '). Accounts with a key under Managed: $(if ($managedAccounts) { $managedAccounts -join ', ' } else { 'none' }). Hives loaded under HKU: $($loadedHives -join ', ')."
    foreach ($tag in $programs.Keys) {
        if ($tag -eq 'PO' -and -not $SecondAccount) { continue }
        $code = $programs[$tag].Code
        $packedCode = Packed $code
        $inWalk = @($everyRow | Where-Object { $_ -like "$code|*" })
        Say "$tag ($($programs[$tag].What)), $code, packed $packedCode"
        Say "    the walk with no product code lists it $($inWalk.Count) times$(if ($inWalk) { ': ' + (($inWalk | ForEach-Object { $c = $_ -split '\|'; "context $($c[1]) ($($contextNames[[int]$c[1]])) sid '$($c[2])'" }) -join '; ') })"
        $keyed = @([PatchRecordsMsi]::Products($code, $everyone, 7))
        Say "    MsiEnumProductsEx asked for its code, every account and every context, lists it $($keyed.Count) times$(if ($keyed) { ': ' + (($keyed | ForEach-Object { if ($_ -like 'ERROR*') { $_ } else { $c = $_ -split '\|'; "context $($c[1]) ($($contextNames[[int]$c[1]])) sid '$($c[2])'" } }) -join '; ') })"
        $holders = @()
        foreach ($account in $accounts) { if (Test-Location "$installerKey\UserData\$account\Products\$packedCode") { $holders += "$installerKey\UserData\$account\Products\$packedCode" } }
        foreach ($account in $managedAccounts) { if (Test-Location "$installerKey\Managed\$account\Installer\Products\$packedCode") { $holders += "$installerKey\Managed\$account\Installer\Products\$packedCode" } }
        foreach ($sid in $loadedHives) { if (Test-Location "HKU\$sid\Software\Microsoft\Installer\Products\$packedCode") { $holders += "HKU\$sid\Software\Microsoft\Installer\Products\$packedCode" } }
        if (Test-Location "HKLM\SOFTWARE\Classes\Installer\Products\$packedCode") { $holders += "HKLM\SOFTWARE\Classes\Installer\Products\$packedCode" }
        Say "    product keys that are there ($($holders.Count)):"
        foreach ($holder in $holders) { Say "        $holder" }
        foreach ($installation in @($installations | Where-Object { $_.Tag -eq $tag })) {
            Say "    $(Get-Label $installation): MsiGetProductInfoEx LocalPackage $([PatchRecordsMsi]::ProductInfo($code, $installation.Sid, $installation.Context, 'LocalPackage')), State $([PatchRecordsMsi]::ProductInfo($code, $installation.Sid, $installation.Context, 'State'))"
        }
    }

    # ---- Section 2.
    Say ''
    Say '==== 2. EVERY PAIRING OF A PROGRAM AND A PATCH: WINDOWS INSTALLER''S ANSWERS BESIDE THE REGISTRY''S VALUES'
    $same = 0
    $different = [System.Collections.Generic.List[string]]::new()
    $unreadPairs = [System.Collections.Generic.List[string]]::new()
    $patchRows = @([PatchRecordsMsi]::Patches('', $everyone, 7, 15, $true))
    $ownPatchCodes = @($patches.Values | ForEach-Object { $_.Code })
    Say "MsiEnumPatchesEx with no product code, every account, every context and every filter bit returns $($patchRows.Count) rows; those naming one of the probe's patches:"
    foreach ($row in $patchRows) {
        $cells = $row -split '\|'
        if ($row -like 'ERROR*') { Say "    $row" }
        elseif ($cells[0] -in $ownPatchCodes) { Say "    $(NameOf $cells[0]) on $(NameOf $cells[1]), context $($cells[2]) ($($contextNames[[int]$cells[2]])), sid '$($cells[3])'" }
    }
    foreach ($installation in $installations) {
        $who = Get-Label $installation
        Say ''
        Say $who
        foreach ($filter in 15, 1, 2, 4, 8) {
            $rows = @([PatchRecordsMsi]::Patches($installation.Code, $installation.Sid, $installation.Context, $filter, $true))
            $filterName = @{ 15 = 'every bit'; 1 = 'applied'; 2 = 'superseded'; 4 = 'obsoleted'; 8 = 'registered' }[$filter]
            Say "    MsiEnumPatchesEx, filter $filter ($filterName), lists $($rows.Count): $(($rows | ForEach-Object { if ($_ -like 'ERROR*') { $_ } else { $c = $_ -split '\|'; "$((NameOf $c[0]) -replace ' \{.*$', '') [context $($c[2]) sid '$($c[3])']" } }) -join ', ')"
        }
        $asScan = @([PatchRecordsMsi]::Patches($installation.Code, $installation.Sid, $installation.Context, 15, $false))
        Say "    MsiEnumPatchesEx as the scan calls it (every bit, no buffer for the account) lists $($asScan.Count): $(($asScan | ForEach-Object { if ($_ -like 'ERROR*') { $_ } else { (NameOf (($_ -split '\|')[0])) -replace ' \{.*$', '' } }) -join ', ')"
        $walked = @([PatchRecordsMsi]::Patches($installation.Code, $installation.Sid, $installation.Context, 15, $true) | Where-Object { $_ -notlike 'ERROR*' } | ForEach-Object { ($_ -split '\|')[0] })

        # Every key that could hold this installation's patch list, whatever its context.
        $recordsKey = Get-RecordsKey $installation
        $candidates = [ordered]@{ "the installation's records key" = $recordsKey }
        if ($installation.Context -ne 4) {
            $candidates["the account's own hive's product key"] = Get-AdvertisedKey $installation.Code $installation.Sid 2
            $candidates["the account's managed product key"] = Get-AdvertisedKey $installation.Code $installation.Sid 1
        }
        $candidates["the machine's product key"] = Get-AdvertisedKey $installation.Code '' 4
        foreach ($label in $candidates.Keys) {
            $location = $candidates[$label]
            $subkeys = $label -eq "the installation's records key"
            try {
                $names = Get-Names $location $subkeys
                if ($null -eq $names) {
                    $hiveLoaded = $location -notlike 'HKU\*' -or ($hku.GetSubKeyNames() -contains ($location -split '\\')[1])
                    Say "    ${label}: $location $(if ($hiveLoaded) { 'is not there' } else { 'is in a hive that is not loaded' })"
                    continue
                }
                $codes = @($names | ForEach-Object { NameOfPacked $_ })
                Say "    ${label}: $location has $($names.Count) $(if ($subkeys) { 'subkeys' } else { 'values' }): $($codes -join ', ')"
                foreach ($name in $names) {
                    if ($subkeys) { Say "        $(NameOfPacked $name): $(Read-Tree "$location\$name")" }
                    else { Say "        value $(NameOfPacked $name) = $(Show-Value $location $name)" }
                }
                $listedHere = @($names | Where-Object { $codeOfPacked.ContainsKey($_) } | ForEach-Object { $codeOfPacked[$_] })
                $onlyWalk = @($walked | Where-Object { $_ -notin $listedHere })
                $onlyKey = @($listedHere | Where-Object { $_ -notin $walked })
                Say "        the walk against this key: only in the walk: $(if ($onlyWalk) { ($onlyWalk | ForEach-Object { NameOf $_ }) -join ', ' } else { 'none' }); only in the key: $(if ($onlyKey) { ($onlyKey | ForEach-Object { NameOf $_ }) -join ', ' } else { 'none' })"
            }
            catch { Say "    ${label}: $location UNREAD: $($_.Exception.Message)" }
        }

        foreach ($patchTag in $patchesOf[$installation.Tag]) {
            $patchCode = $patches[$patchTag].Code
            $patchKey = "$recordsKey\$(Packed $patchCode)"
            foreach ($property in 'Uninstallable', 'State') {
                $answer = [PatchRecordsMsi]::PatchInfo($patchCode, $installation.Code, $installation.Sid, $installation.Context, $property)
                $value = Show-Value $patchKey $property
                $pairing = "$who, $patchTag, $property"
                if ($value -like 'UNREAD*' -or $answer -notlike 'rc=0*') { $verdict = 'UNREAD'; $unreadPairs.Add("$pairing (Windows Installer $answer; registry $value)") }
                elseif ($value -match '^(\S+) \(DWord\)$' -and $answer -eq "rc=0 '$($Matches[1])'") { $verdict = 'SAME'; $same++ }
                else { $verdict = 'DIFFERENT'; $different.Add("$pairing (Windows Installer $answer; registry $value)") }
                Say "    $patchTag ${property}: MsiGetPatchInfoEx $answer | the value $property of $patchKey is $value | $verdict"
            }
            Say "    $patchTag LocalPackage: MsiGetPatchInfoEx $([PatchRecordsMsi]::PatchInfo($patchCode, $installation.Code, $installation.Sid, $installation.Context, 'LocalPackage'))"
        }
    }
    Say ''
    Say "Pairings where Windows Installer's answer and the records key's value are the same: $same. Different: $($different.Count). With one side unread: $($unreadPairs.Count)."
    foreach ($item in $different) { Say "    DIFFERENT: $item" }
    foreach ($item in $unreadPairs) { Say "    UNREAD: $item" }

    # ---- Section 3.
    Say ''
    Say '==== 3. THE REGISTRY EVENTS OF EACH CALL, UNDER PROCESS MONITOR'
    try {
        $script:procmonExe = Resolve-Procmon
        $pml = Join-Path $capture 'calls.pml'
        $csv = Join-Path $capture 'calls.csv'
        Remove-Item -LiteralPath $pml, $csv -ErrorAction SilentlyContinue
        $calls = @()
        try {
            $null = Start-Process -FilePath $script:procmonExe -ArgumentList '/AcceptEula', '/Quiet', '/Minimized', '/NoFilter', '/BackingFile', "`"$pml`"" -PassThru
            Invoke-Procmon @('/WaitForIdle') 120
            Start-Sleep -Seconds 3
            $n = 0
            foreach ($installation in $installations) {
                foreach ($patchTag in $patchesOf[$installation.Tag]) {
                    $n++
                    Set-Marker "$n-begin"
                    $answer = [PatchRecordsMsi]::PatchInfo($patches[$patchTag].Code, $installation.Code, $installation.Sid, $installation.Context, 'Uninstallable')
                    Set-Marker "$n-end"
                    $calls += [pscustomobject]@{ N = $n; What = "MsiGetPatchInfoEx Uninstallable of $patchTag on $(Get-Label $installation) answered $answer"; Words = @((Packed $installation.Code), (Packed $patches[$patchTag].Code), 'Uninstallable') }
                }
                $n++
                Set-Marker "$n-begin"
                $rows = @([PatchRecordsMsi]::Patches($installation.Code, $installation.Sid, $installation.Context, 15, $false))
                Set-Marker "$n-end"
                $calls += [pscustomobject]@{ N = $n; What = "MsiEnumPatchesEx as the scan calls it on $(Get-Label $installation) listed $($rows.Count): $($rows -join ', ')"; Words = @((Packed $installation.Code), '\Patches') }
            }
            foreach ($tag in @($installations | ForEach-Object { $_.Tag } | Select-Object -Unique)) {
                $n++
                Set-Marker "$n-begin"
                $rows = @([PatchRecordsMsi]::Products($programs[$tag].Code, $everyone, 7))
                Set-Marker "$n-end"
                $calls += [pscustomobject]@{ N = $n; What = "MsiEnumProductsEx asked for the code of $tag, every account and every context, listed $($rows.Count): $($rows -join ', ')"; Words = @((Packed $programs[$tag].Code)) }
            }
            Start-Sleep -Seconds 2
        }
        finally { Stop-Procmon }
        if (-not (Test-Path -LiteralPath $pml)) { throw 'Process Monitor wrote no capture file.' }
        Say "Capture: $([math]::Round((Get-Item -LiteralPath $pml).Length / 1MB, 1)) MB"
        Invoke-Procmon @('/OpenLog', "`"$pml`"", '/SaveAs', "`"$csv`"") 600
        if (-not (Test-Path -LiteralPath $csv)) { throw 'Process Monitor did not write the capture as CSV.' }

        # One pass over the capture: each call's events are those between its two markers.
        $byCall = @{}
        foreach ($call in $calls) { $byCall[$call.N] = [pscustomobject]@{ Call = $call; Lines = [System.Collections.Generic.List[string]]::new(); Others = [System.Collections.Generic.List[string]]::new(); Processes = @{}; Began = $false; Ended = $false } }
        $open = $null
        $rowCount = 0
        Import-Csv -LiteralPath $csv | ForEach-Object {
            $rowCount++
            if ($_.Path -match 'ICPatchRecordsMarker-(\d+)-(begin|end)$') {
                $slot = $byCall[[int]$Matches[1]]
                if ($Matches[2] -eq 'begin') { $slot.Began = $true; $open = $slot } else { $slot.Ended = $true; $open = $null }
            }
            elseif ($null -ne $open -and $_.Operation -like 'Reg*') {
                $process = $_.'Process Name'
                $open.Processes[$process] = 1 + $(if ($open.Processes.ContainsKey($process)) { $open.Processes[$process] } else { 0 })
                $path = $_.Path
                if ($open.Call.Words | Where-Object { $path.IndexOf($_, [StringComparison]::OrdinalIgnoreCase) -ge 0 }) {
                    $open.Lines.Add("    [$process $($_.PID)] $($_.Operation) $path -> $($_.Result) $($_.Detail)")
                }
                elseif ("$($_.PID)" -eq "$PID" -and $_.Operation -notin 'RegCloseKey', 'RegQueryKey') {
                    $other = "$($_.Operation) $path -> $($_.Result)"
                    if (-not $open.Others.Contains($other)) { $open.Others.Add($other) }
                }
            }
        }
        Say "Rows in the capture: $rowCount"
        foreach ($call in $calls) {
            $slot = $byCall[$call.N]
            Say ''
            Say "Call $($call.N): $($call.What)"
            if (-not $slot.Began -or -not $slot.Ended) { Say '    UNREAD: the capture does not hold both of this call''s markers.'; $failures.Add("Process Monitor: call $($call.N) has no markers in the capture."); continue }
            foreach ($line in ($slot.Lines | Select-Object -First 300)) { Say $line }
            if ($slot.Lines.Count -gt 300) { Say "    ... and $($slot.Lines.Count - 300) more such events, not printed." }
            Say "    Every other key the probe's own process opened or read between the markers ($($slot.Others.Count)):"
            foreach ($line in ($slot.Others | Select-Object -First 80)) { Say "        $line" }
            if ($slot.Others.Count -gt 80) { Say "        ... and $($slot.Others.Count - 80) more, not printed." }
            Say "    $($slot.Lines.Count) registry events naming the program, the patch or the property are printed, of these registry events between the markers: $(($slot.Processes.Keys | Sort-Object | ForEach-Object { "$_ $($slot.Processes[$_])" }) -join ', ')."
        }
    }
    catch {
        Say "REFUSED: this section has no capture to read: $($_.Exception.Message)"
        $failures.Add("Process Monitor: $($_.Exception.Message)")
    }

    # ---- Section 4.
    Say ''
    Say '==== 4. A REGISTRY VALUE CHANGED, THE QUESTIONS ASKED AGAIN, AND THE VALUE PUT BACK'
    Say 'Each change is made to one of the probe''s own programs. "A new process" is a PowerShell process started for that one reading; "this process" is the probe itself, which has asked the same questions before.'

    # Makes one change and prints the answers before it, with it and after it is put back.
    # $locations are the keys the change touches, journalled first; $change is run with them.
    function Invoke-Change([string]$what, $installation, [string[]]$locations, [string]$kind, [scriptblock]$change) {
        Say ''
        Say "CHANGE: $what, on $(Get-Label $installation)"
        foreach ($location in $locations) { Say "    key: $location" }
        $before = Read-Fresh $installation
        Say "    before, a new process: $before"
        $entries = @()
        $with = $null
        try {
            foreach ($location in $locations) { $entries += , (Start-Change $what $location $kind) }
            & $change $locations
            $with = Read-Fresh $installation
            Say "    changed, a new process: $with"
            Say "    changed, this process: $(Read-Here $installation)"
        }
        catch { Say "    REFUSED: the change was not made or not read: $($_.Exception.Message)"; $failures.Add("Change '$what' on $($installation.Tag): $($_.Exception.Message)") }
        finally {
            [array]::Reverse($entries)
            foreach ($entry in $entries) {
                $line = Undo-Change $entry
                Say "    $line"
                if ($line -like 'NOT CLEAN*') { $failures.Add("Change '$what' on $($installation.Tag): $line") }
            }
        }
        $after = Read-Fresh $installation
        Say "    put back, a new process: $after"
        if ($after -cne $before) { Say '    THE ANSWERS AFTER THE KEY WAS PUT BACK ARE NOT THE ANSWERS BEFORE THE CHANGE.'; $failures.Add("Change '$what' on $($installation.Tag): the answers did not return.") }
        elseif ($null -ne $with) { Say "    RESULT: $(if ($with -cne $before) { 'the answers changed with the key and returned with it.' } else { 'the answers did not change.' })" }
    }

    foreach ($installation in $inShape) {
        $superseded, $applied = $patchesOf[$installation.Tag] | ForEach-Object { Packed $patches[$_].Code }
        $supersededTag, $appliedTag = $patchesOf[$installation.Tag]
        $recordsKey = Get-RecordsKey $installation
        $advertisedKey = Get-AdvertisedKey $installation.Code $installation.Sid $installation.Context
        $hasAdvertised = $advertisedKey -notlike 'HKU\*' -or ($hku.GetSubKeyNames() -contains $installation.Sid)
        $fake = Packed $madeUp

        Invoke-Change "the Uninstallable value of $appliedTag's record set to the other of 0 and 1" $installation @("$recordsKey\$applied") 'tree' {
            param($keys)
            $key = Open-Location $keys[0] $true
            try { $key.SetValue('Uninstallable', $(if ([int]$key.GetValue('Uninstallable') -eq 0) { 1 } else { 0 }), 'DWord') } finally { $key.Dispose() }
        }
        Invoke-Change "the Uninstallable value of $appliedTag's record deleted" $installation @("$recordsKey\$applied") 'tree' {
            param($keys)
            $key = Open-Location $keys[0] $true
            try { $key.DeleteValue('Uninstallable') } finally { $key.Dispose() }
        }
        Invoke-Change "$appliedTag's record (an applied patch) deleted from the records key" $installation @($recordsKey) 'tree' {
            param($keys)
            $key = Open-Location $keys[0] $true
            try { $key.DeleteSubKeyTree($applied) } finally { $key.Dispose() }
        }
        Invoke-Change "$supersededTag's record (a superseded patch) deleted from the records key" $installation @($recordsKey) 'tree' {
            param($keys)
            $key = Open-Location $keys[0] $true
            try { $key.DeleteSubKeyTree($superseded) } finally { $key.Dispose() }
        }
        Invoke-Change "a record for the made-up code added to the records key, a copy of $appliedTag's" $installation @($recordsKey) 'tree' {
            param($keys)
            Write-Tree "$($keys[0])\$fake" (Read-Tree "$($keys[0])\$applied")
        }
        if ($hasAdvertised -and (Test-Location $advertisedKey)) {
            Invoke-Change "$appliedTag's value deleted from the product key's Patches key" $installation @($advertisedKey) 'tree' {
                param($keys)
                $key = Open-Location $keys[0] $true
                try { $key.DeleteValue($applied) } finally { $key.Dispose() }
            }
            Invoke-Change "$appliedTag's value deleted from the product key's Patches key, and its code taken out of the value named Patches there" $installation @($advertisedKey) 'tree' {
                param($keys)
                $key = Open-Location $keys[0] $true
                try {
                    $key.DeleteValue($applied)
                    if ($key.GetValueNames() -contains 'Patches') {
                        $kind = $key.GetValueKind('Patches')
                        $kept = @(@($key.GetValue('Patches')) | Where-Object { $_ -ne $applied })
                        if ($kind -eq 'MultiString') { $key.SetValue('Patches', [string[]]$kept, $kind) } else { $key.SetValue('Patches', ($kept -join ''), $kind) }
                    }
                }
                finally { $key.Dispose() }
            }
            Invoke-Change "a value named by the made-up code added to the product key's Patches key, a copy of $appliedTag's" $installation @($advertisedKey) 'tree' {
                param($keys)
                $key = Open-Location $keys[0] $true
                try { $key.SetValue($fake, "$($key.GetValue($applied))", 'String') } finally { $key.Dispose() }
            }
            Invoke-Change "the made-up code added to both keys: a value in the product key's Patches key and a record in the records key, copies of $appliedTag's" $installation @($advertisedKey, $recordsKey) 'tree' {
                param($keys)
                $key = Open-Location $keys[0] $true
                try { $key.SetValue($fake, "$($key.GetValue($applied))", 'String') } finally { $key.Dispose() }
                Write-Tree "$($keys[1])\$fake" (Read-Tree "$($keys[1])\$applied")
            }
        }
        else { Say ''; Say "NOT CHANGED: $(Get-Label $installation) has no product key the probe can open at $advertisedKey, so no change is made there." }
        if ($installation.Context -eq 4) {
            # The first key on the way to the product key under this account's classes that is
            # not there yet, so that putting it back removes everything the change made.
            $classes = "HKU\$mySid\Software\Classes"
            $missing = $classes
            foreach ($part in 'Installer', 'Products', (Packed $installation.Code)) { $missing = "$missing\$part"; if (-not (Test-Location $missing)) { break } }
            $target = "$classes\Installer\Products\$(Packed $installation.Code)\Patches"
            Invoke-Change "a value named by the made-up code written under this account's own classes, at $target, a copy of $appliedTag's in the machine's" $installation @($missing) 'tree' {
                param($keys)
                $machineKey = Open-Location $advertisedKey
                try { $copied = "$($machineKey.GetValue($applied))" } finally { $machineKey.Dispose() }
                $hive, $path = Split-Location $target
                $key = $hive.CreateSubKey($path)
                try { $key.SetValue($fake, $copied, 'String') } finally { $key.Dispose() }
            }
        }
        Invoke-Change "everyone denied reading the records key's values and listing its subkeys" $installation @($recordsKey) 'access' {
            param($keys)
            Set-ReadDenied $keys[0] $true
        }
    }

    # ---- Section 5.
    Say ''
    Say '==== 5. THE SCAN COMPARISON''S X: ONE PRODUCT CODE INSTALLED PER USER BY SYSTEM AND PER MACHINE'
    $xRows = @([PatchRecordsMsi]::Products($scenarioX, $everyone, 7))
    $xKeys = @($accounts | Where-Object { Test-Location "$installerKey\UserData\$_\Products\$(Packed $scenarioX)" })
    if (-not $xRows.Count -and -not $xKeys.Count) { Say "X, $scenarioX, is not installed here, so nothing is asked about it." }
    else {
        Say "MsiEnumProductsEx asked for X's code, $scenarioX, every account and every context, lists it $($xRows.Count) times: $(($xRows | ForEach-Object { if ($_ -like 'ERROR*') { $_ } else { $c = $_ -split '\|'; "context $($c[1]) ($($contextNames[[int]$c[1]])) sid '$($c[2])'" } }) -join '; ')"
        Say "Accounts whose UserData key holds a product key for X ($($xKeys.Count)): $($xKeys -join ', ')"
        foreach ($sid in $loadedHives) { Say "X's product key in the hive of ${sid}: $(Test-Location "HKU\$sid\Software\Microsoft\Installer\Products\$(Packed $scenarioX)")" }
        Say "X's product key in the machine's classes: $(Test-Location "HKLM\SOFTWARE\Classes\Installer\Products\$(Packed $scenarioX)")"
    }
}
catch {
    $stopped = $_
    Say ''
    Say "STOPPED: $($_.Exception.Message)"
    Say "    at $($_.ScriptStackTrace -replace "`n", ' < ')"
}
finally {
    Stop-Procmon
    $notClean = Invoke-Teardown
    Say "Process Monitor's own settings key, HKU\$mySid\Software\Sysinternals\Process Monitor, was there before the run: $sysinternalsAtStart; is there now: $(Test-Location "HKU\$mySid\Software\Sysinternals\Process Monitor")."
}

Say ''
if ($failures.Count) {
    Say "==== WHAT THE PROBE COULD NOT SET UP OR READ ($($failures.Count))"
    foreach ($failure in $failures) { Say "    $failure" }
}
if ($stopped) { throw "The probe stopped: $($stopped.Exception.Message)" }
if ($notClean) { throw "The teardown left $notClean things; $result names each." }
if ($failures.Count) { throw "The probe could not set up or read $($failures.Count) things; $result names each." }
Say 'Done: every section was set up and read, and the teardown is clean.'
