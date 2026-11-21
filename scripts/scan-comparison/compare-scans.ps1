# Fails (exit 1) where two builds of the command line do not offer and hold the same files in
# the Installer folder, printing each file whose verdict differs with both builds' verdicts.
#
# The reference build is run, then the build under test, then the reference build again, each
# as `/s /noupdate /noquestions`, which reads and changes nothing in the folder. A file is
# offered where the build lists it under its found line, and held where it is in the folder
# and the build does not list it. The folder is the scan's own set: the files at its top level
# with the .msi or .msp extension, in any case.
#
# The step fails, whatever the two builds' lists, where:
#  - a run exits with anything but 0;
#  - a run lists a file the folder does not hold, or lists a different number of files from
#    the number on its found line;
#  - the folder's files or sizes differ between the listing before the runs and the one after;
#  - the reference build's two runs disagree, which says the folder or Windows Installer moved
#    under the runs and the comparison cannot be read;
#  - the folder holds no file to judge.
# It prints what each run read: the folder's file count, each build's offered and held counts
# and its left-alone line. A difference in a file's size text or reason, the folder's verdict
# being the same, is printed and does not fail the step.
#
# Every run then checks its own comparison on the outputs it has just read: the reference
# compared with itself reports nothing, and the build under test with one offered row taken
# out and one held file listed has those two files reported, each the right way round. A
# check that reports anything else fails the step.
#
# -Scenarios names a file of `name<TAB>what it is<TAB>this build<TAB>why` rows, as
# set-up-scenarios.ps1 writes; each of its files is printed with both verdicts. A row whose third
# column is 'holds' or 'offers' expects the build under test to do that with the file and the
# reference build the other. That difference is printed with its reason and does not fail the
# step; it not happening, either build judging the file the other way, fails it. A difference on
# any other file fails the step as above, so a file of the runner's own never carries one.
#
#   pwsh scripts/scan-comparison/compare-scans.ps1 -Reference <exe> -UnderTest <exe> -Label <text> -OutDir <folder> [-Scenarios <tsv>]
param(
    [Parameter(Mandatory)][string]$Reference,
    [Parameter(Mandatory)][string]$UnderTest,
    [Parameter(Mandatory)][string]$Label,
    [Parameter(Mandatory)][string]$OutDir,
    [string]$ReferenceName = '3.7.0',
    [string]$UnderTestName = 'this build',
    [string]$Scenarios
)

$ErrorActionPreference = 'Stop'
$installer = Join-Path $env:SystemRoot 'Installer'
$null = New-Item -ItemType Directory -Force -Path $OutDir
$failures = [System.Collections.Generic.List[string]]::new()
function Fail([string]$message) { $failures.Add($message); Write-Host "::error::$Label`: $message" }

# The folder's files the scan reads, by name, with each size in bytes.
function Read-Folder {
    $files = [ordered]@{}
    Get-ChildItem -LiteralPath $installer -File -Force |
        Where-Object { $_.Extension -in '.msi', '.msp' } |
        Sort-Object Name |
        ForEach-Object { $files[$_.Name] = $_.Length }
    return $files
}

# One run of a build: its exit code, every line it printed, and the files it offered with
# each row's text after the name.
function Invoke-Scan([string]$exe, [string]$name, [int]$number) {
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $lines = @(& $exe /s /noupdate /noquestions 2>&1 | ForEach-Object { "$_" })
    $code = $LASTEXITCODE
    $seconds = [math]::Round($clock.Elapsed.TotalSeconds, 1)
    $file = Join-Path $OutDir ("$Label-run$number-$name.txt" -replace '[^A-Za-z0-9.-]+', '-')
    Set-Content -LiteralPath $file -Value $lines -Encoding utf8
    return [pscustomobject]@{ Name = $name; Number = $number; Code = $code; Seconds = $seconds; Lines = $lines; Output = $file }
}

# What a run's output says: the files it offered (name to the rest of the row), the count on
# its found line, and its left-alone line. A row is a line of two spaces, the name, two or
# more spaces and the size and reason in brackets; the rows sit straight under the found line.
function Read-Verdicts($run) {
    $offered = [ordered]@{}
    $found = $null
    $leftAlone = @($run.Lines | Where-Object { $_ -match '^\S.* left alone$' }) -join ' | '
    $inList = $false
    foreach ($line in $run.Lines) {
        if ($line -match '^Found (\S+) unneeded ') {
            $found = [int](($Matches[1]) -replace '[^0-9]', '')
            $inList = $true
            continue
        }
        if ($inList -and $line -match '^  (\S+)\s{2,}(\(.*\))\s*$') {
            $offered[$Matches[1]] = $Matches[2]
            continue
        }
        $inList = $false
    }
    return [pscustomobject]@{ Offered = $offered; Found = $found; LeftAlone = $leftAlone }
}

# Each file whose verdict differs between two readings of one folder.
function Compare-Verdicts($folder, $one, $two) {
    foreach ($name in $folder.Keys) {
        $a = $one.Offered.Contains($name)
        $b = $two.Offered.Contains($name)
        if ($a -ne $b) {
            [pscustomobject]@{
                Name = $name
                One = if ($a) { "offered $($one.Offered[$name])" } else { 'held' }
                Two = if ($b) { "offered $($two.Offered[$name])" } else { 'held' }
            }
        }
    }
}

"== $Label"
$before = Read-Folder
"The Installer folder holds $($before.Count) .msi and .msp files before the runs."
$runs = @(
    (Invoke-Scan $Reference $ReferenceName 1),
    (Invoke-Scan $UnderTest $UnderTestName 2),
    (Invoke-Scan $Reference $ReferenceName 3)
)
$after = Read-Folder

$folderMoved = @(Compare-Object @($before.Keys | ForEach-Object { "$_ $($before[$_])" }) @($after.Keys | ForEach-Object { "$_ $($after[$_])" }))
foreach ($entry in $folderMoved) { "  folder $(if ($entry.SideIndicator -eq '<=') { 'before only' } else { 'after only' }): $($entry.InputObject)" }
if ($folderMoved.Count) { Fail "The Installer folder's files changed while the builds ran." }
if ($before.Count -eq 0) { Fail 'The Installer folder holds no .msi or .msp file, so there is nothing to compare.' }

$verdicts = @{}
foreach ($run in $runs) {
    $v = Read-Verdicts $run
    $verdicts[$run.Number] = $v
    $held = $before.Count - @($before.Keys | Where-Object { $v.Offered.Contains($_) }).Count
    "Run $($run.Number), $($run.Name): exit code $($run.Code) after $($run.Seconds) s; offered $($v.Offered.Count), held $held; found line $(if ($null -eq $v.Found) { 'not printed' } else { $v.Found }); left-alone line: $(if ($v.LeftAlone) { $v.LeftAlone } else { 'not printed' }). Output: $($run.Output)"
    if ($run.Code -ne 0) {
        $run.Lines | ForEach-Object { "    $_" }
        Fail "Run $($run.Number), $($run.Name), exited $($run.Code)."
    }
    $missing = @($v.Offered.Keys | Where-Object { -not $before.Contains($_) })
    foreach ($name in $missing) { "    listed and not in the folder: $name" }
    if ($missing.Count) { Fail "Files run $($run.Number), $($run.Name), listed that the folder does not hold: $($missing.Count)." }
    if (($v.Found ?? 0) -ne $v.Offered.Count) { Fail "Run $($run.Number), $($run.Name), listed $($v.Offered.Count) files under a found line of $($v.Found)." }
}

$referenceMoved = @(Compare-Verdicts $before $verdicts[1] $verdicts[3])
foreach ($d in $referenceMoved) { "  $ReferenceName, run 1 against run 3: $($d.Name): $($d.One) / $($d.Two)" }
if ($referenceMoved.Count) { Fail "Files $ReferenceName gave two answers for, so the comparison cannot be read: $($referenceMoved.Count)." }

# The scenario rows, with each file's expected difference where its row names one.
$scenarioRows = @()
$expectedDifferences = @{}
if ($Scenarios) {
    foreach ($row in @(Get-Content -LiteralPath $Scenarios | Select-Object -Skip 1)) {
        $name, $what, $expect, $why = $row -split "`t", 4
        $scenarioRows += [pscustomobject]@{ Name = $name; What = $what }
        if (-not $expect -and -not $why) { continue }
        if ($expect -notin 'holds', 'offers' -or -not $why) { Fail "The scenario row for $name names an expected difference '$expect' without 'holds' or 'offers' and a reason." }
        else { $expectedDifferences[$name] = [pscustomobject]@{ Holds = $expect -eq 'holds'; Why = $why } }
    }
}

$differences = @(Compare-Verdicts $before $verdicts[1] $verdicts[2])
$accepted = @($differences | Where-Object { $expectedDifferences.Contains($_.Name) -and ($_.Two -eq 'held') -eq $expectedDifferences[$_.Name].Holds })
$unexpected = @($differences | Where-Object { $_ -notin $accepted })
"Files whose verdict differs between $ReferenceName and ${UnderTestName}: $($differences.Count), $($accepted.Count) of them expected"
foreach ($d in $unexpected) { "  $($d.Name): $ReferenceName $($d.One); $UnderTestName $($d.Two)" }
foreach ($d in $accepted) { "  expected: $($d.Name): $ReferenceName $($d.One); $UnderTestName $($d.Two). $($expectedDifferences[$d.Name].Why)" }
if ($unexpected.Count) { Fail "Files offered by one build and held by the other: $($unexpected.Count)." }
foreach ($name in $expectedDifferences.Keys) {
    if (@($accepted | Where-Object Name -eq $name).Count) { continue }
    if (-not $before.Contains($name)) { continue }
    $one = if ($verdicts[1].Offered.Contains($name)) { 'offered' } else { 'held' }
    $two = if ($verdicts[2].Offered.Contains($name)) { 'offered' } else { 'held' }
    Fail "The test file $name was expected to be $(if ($expectedDifferences[$name].Holds) { 'held' } else { 'offered' }) by $UnderTestName and the other way by $ReferenceName, and $ReferenceName $one it and $UnderTestName $two it."
}

$rowsDiffer = @($verdicts[1].Offered.Keys | Where-Object { $verdicts[2].Offered.Contains($_) -and $verdicts[2].Offered[$_] -ne $verdicts[1].Offered[$_] })
"Files offered by both whose row text differs (printed, not failed): $($rowsDiffer.Count)"
foreach ($name in $rowsDiffer) { "  ${name}: $ReferenceName $($verdicts[1].Offered[$name]); $UnderTestName $($verdicts[2].Offered[$name])" }

if ($Scenarios) {
    "The test files, with each build's verdict:"
    foreach ($row in $scenarioRows) {
        $name, $what = $row.Name, $row.What
        $one = if ($verdicts[1].Offered.Contains($name)) { 'offered' } elseif ($before.Contains($name)) { 'held' } else { 'NOT IN THE FOLDER' }
        $two = if ($verdicts[2].Offered.Contains($name)) { 'offered' } elseif ($before.Contains($name)) { 'held' } else { 'NOT IN THE FOLDER' }
        "  ${name}: $ReferenceName $one, $UnderTestName $two. $what"
        if (-not $before.Contains($name)) { Fail "The test file $name ($what) is not in the folder." }
    }
}

# The comparison's own check, on the outputs just read.
$self = @(Compare-Verdicts $before $verdicts[1] $verdicts[1])
if ($self.Count) { Fail "Files the comparison of $ReferenceName's run with itself reported: $($self.Count)." }
$planted = [pscustomobject]@{ Offered = [ordered]@{}; Found = $null; LeftAlone = '' }
foreach ($name in $verdicts[2].Offered.Keys) { $planted.Offered[$name] = $verdicts[2].Offered[$name] }
$expected = @{}
$takenOut = @($planted.Offered.Keys | Select-Object -First 1)
foreach ($name in $takenOut) { $planted.Offered.Remove($name); $expected[$name] = 'held' }
$listed = @($before.Keys | Where-Object { -not $verdicts[2].Offered.Contains($_) } | Select-Object -First 1)
foreach ($name in $listed) { $planted.Offered[$name] = '(planted row)'; $expected[$name] = 'offered (planted row)' }
$caught = @(Compare-Verdicts $before $verdicts[2] $planted)
$caughtText = ($caught | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Two)" }) -join ', '
$expectedText = ($expected.Keys | Sort-Object | ForEach-Object { "$_=$($expected[$_])" }) -join ', '
"Check of the comparison: planted $(if ($expectedText) { $expectedText } else { 'nothing, the folder being empty' }); reported $(if ($caughtText) { $caughtText } else { 'nothing' })."
if ($expected.Count -eq 0) { Fail 'The comparison could not be checked: there was no file to plant a difference on.' }
elseif ($caughtText -ne $expectedText) { Fail 'The comparison did not report the planted difference exactly.' }

if ($failures.Count) {
    "$Label FAILED:"
    $failures | ForEach-Object { "  $_" }
    exit 1
}
if ($accepted.Count) {
    "$Label passed: $($before.Count) files, $($verdicts[1].Offered.Count) offered and $($before.Count - $verdicts[1].Offered.Count) held by $ReferenceName, and $UnderTestName the same but for the $($accepted.Count) expected above."
} else {
    "$Label passed: $($before.Count) files, $($verdicts[1].Offered.Count) offered and $($before.Count - $verdicts[1].Offered.Count) held by both builds."
}
