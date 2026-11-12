# native-libraries.ps1: Process Monitor, and the functions that record a build with it
# and judge which files the build looked for to load and which images it loaded: the
# native-libraries job in ci.yml, and the update self-update.yml performs.
#
# A workflow writes native.ps1, which defines $builds and then dot-sources this file, and
# each step that records or judges a build dot-sources native.ps1. $builds holds each
# build's label, its exe, its arguments, how long it is given, what it must load and the
# kind of its first act seen. The functions are given the builds and runs they judge, and
# a run carries its build. $procmon is read from $env:PROCMON, which the step that runs
# Get-Procmon sets for the steps after it.
# Two steps that only start a build with a started event dot-source this file itself, for
# New-StartedEvent. What they call from it throws where it fails rather than calling Fail,
# since neither reads the failures Fail collects.
# Source files are read from the repository this file is in, whatever the working
# directory.

$repository = Split-Path $PSScriptRoot

$procmon = $env:PROCMON
$failures = [System.Collections.Generic.List[string]]::new()
function Fail([string]$message) { $failures.Add($message); Write-Host "::error::$message" }

# A name looked for that no file on the runner carries is planted as this
# library under that name, so the look is judged like any other.
$standIn = Join-Path $env:SystemRoot 'System32\msimg32.dll'

# Each Process Monitor call that is waited for is bounded, so one that hangs fails the
# step with a message. A record starts only once no earlier Process Monitor is left,
# since one started while another is ending can hand its arguments over to it instead
# of recording.
function Invoke-Procmon([string[]]$arguments, [int]$seconds, [string]$what) {
  $p = Start-Process $procmon -ArgumentList $arguments -PassThru
  if (-not $p.WaitForExit($seconds * 1000)) {
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    throw "Process Monitor did not finish $what within $seconds s."
  }
}

function Wait-ProcmonGone([string]$what) {
  $deadline = [DateTime]::UtcNow.AddSeconds(60)
  while (Get-Process -Name Procmon64 -ErrorAction SilentlyContinue) {
    if ([DateTime]::UtcNow -gt $deadline) { throw "Process Monitor was still running 60 s after $what." }
    Start-Sleep -Milliseconds 500
  }
}

function Start-Record([string]$name) {
  if (Get-Process -Name Procmon64 -ErrorAction SilentlyContinue) { Invoke-Procmon @('/Terminate') 60 'ending a record left running' }
  Wait-ProcmonGone 'the earlier record ended'
  $pml = Join-Path (Split-Path $procmon) "$name.pml"
  Start-Process $procmon -ArgumentList '/AcceptEula', '/Quiet', '/Minimized', '/BackingFile', "`"$pml`""
  Invoke-Procmon @('/AcceptEula', '/WaitForIdle') 120 "starting the $name record"
}

function Stop-Record([string]$name) {
  $dir = Split-Path $procmon
  Invoke-Procmon @('/Terminate') 60 "ending the $name record"
  Wait-ProcmonGone "the $name record was ended"
  $csv = Join-Path $dir "$name.csv"
  Invoke-Procmon @('/AcceptEula', '/Quiet', '/OpenLog', "`"$dir\$name.pml`"", '/SaveAs', "`"$csv`"") 300 "saving the $name record"
  Wait-ProcmonGone "the $name record was saved"
  if (-not (Test-Path $csv)) { throw "Process Monitor saved no $name record." }
  $csv
}

# One build at a time, each ended and waited for before the next starts, since
# the windows share the single-instance mutex. The command line scans and ends;
# a window is given twenty seconds and then ended.
# The addresses github.com resolved to around each run that checks for updates, by its
# process ID and label, which Test-UpdateCheck judges its connections against.
$script:GitHubAddressesByRun = @{}

# The addresses github.com resolves to now, or none where the lookup fails.
function Get-GitHubAddresses {
  try { @([System.Net.Dns]::GetHostAddresses('github.com') | ForEach-Object IPAddressToString) } catch { @() }
}

function Invoke-Build($build, [string]$label, [string]$folder) {
  $start = @{ FilePath = (Join-Path $folder (Split-Path $build.Exe -Leaf)); WorkingDirectory = $folder; PassThru = $true }
  if ($build.Arguments.Count) { $start.ArgumentList = $build.Arguments }
  $before = if ($build.UpdateCheck) { Get-GitHubAddresses } else { @() }
  $p = Start-Process @start
  # Read now, so the exit code is kept once the process ends.
  $null = $p.Handle
  if (-not $p.WaitForExit($build.Wait * 1000)) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue; $p.WaitForExit() }
  if ($build.UpdateCheck) { $script:GitHubAddressesByRun["$($p.Id)|$label"] = @(@($before) + @(Get-GitHubAddresses) | Sort-Object -Unique) }
  Write-Host "  ${label}: process $($p.Id), ended with $($p.ExitCode)"
  $p.Id
}

# The path Windows gives $path in full, from GetLongPathName, which needs it to exist,
# or nothing where Windows does not answer. Get-ShortName is the same from
# GetShortPathName.
function Get-LongName([string]$path) { Invoke-PathName 'GetLongPathNameW' $path }
function Get-ShortName([string]$path) { Invoke-PathName 'GetShortPathNameW' $path }

function Invoke-PathName([string]$function, [string]$path) {
  if (-not ('NativeLibraries.PathNames' -as [type])) {
    Add-Type -Namespace NativeLibraries -Name PathNames -MemberDefinition @'
[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern uint GetLongPathNameW(string path, System.Text.StringBuilder buffer, uint size);
[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern uint GetShortPathNameW(string path, System.Text.StringBuilder buffer, uint size);
'@
  }
  $buffer = [System.Text.StringBuilder]::new(32768)
  $length = [NativeLibraries.PathNames]::$function($path, $buffer, [uint32]$buffer.Capacity)
  if ($length -eq 0 -or $length -ge $buffer.Capacity) { return }
  $buffer.ToString()
}

# A record path on a drive written in full where it has a part holding '~'. A failed
# open is recorded as the process spelled it, short names included, as in
# C:\Users\RUNNER~1. The path up to and including its last such part is given in full
# by Get-LongName, so that part has to exist when the record is read. Answers the path
# unchanged where it has no such part, and nothing where that part is not given in
# full. Each part looked up is kept in $longParts, and each one used in $usedParts.
$longParts = @{}
$usedParts = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
function Get-FullPath([string]$path) {
  if ($path -notmatch '^[A-Za-z]:\\' -or -not $path.Contains('~')) { return $path }
  $parts = $path.Split('\')
  $last = -1
  for ($i = 0; $i -lt $parts.Count; $i++) { if ($parts[$i].Contains('~')) { $last = $i } }
  $short = $parts[0..$last] -join '\'
  if (-not $longParts.ContainsKey($short)) { $longParts[$short] = Get-LongName $short }
  if ($null -eq $longParts[$short]) { return }
  $null = $usedParts.Add($short)
  $longParts[$short] + $path.Substring($short.Length)
}

# Each run's rows, by the run's key (Get-RunKey). Process Monitor writes the record in
# event order, so a row's place among its process's rows is its place in that sequence.
# Windows hands a process ID on once a process has ended, and the stand-in old copy
# Invoke-Replace starts carries the name of the build's exe, so a process ID and a name do
# not say whose rows are whose. Each process's rows from its Process Start row to the
# next Process Start row of its ID are one lifetime: Windows closes a process's handles
# after its Process Exit row, so its CloseFile and RegCloseKey rows come after that row.
# A lifetime is a run's where it is the run's process ID and its first Load Image, which
# is the process's own image, is the run's exe in the run's folder. A run with StartedBy is
# the copy that run starts, so its lifetime also begins while that run's does. A run with
# ByProcessId, a process another started whose folder its own rows settle
# (Test-StartedProcesses), takes the first lifetime of its ID and name begun after the row
# After that no other such run has taken.
# Every row a run is given carries its row in the record as Row.
#
# With $started, a run that starts a replace (Test-StartsReplace) has that replace read in
# the same pass: a lifetime of another process whose image is the run's own exe, begun
# while the run's lifetime is, is a run of its own (New-StartedReplace), added to
# $started. Each path is written in full by Get-FullPath, and the step fails, naming the
# path, where one cannot be, so a look spelled in short names is judged with the rest.
#
# The step fails, and the run is judged on none of its rows and its key added to $dropped,
# where two runs could take one lifetime or two lifetimes are one run's. It fails too
# where a process with a run's ID and the name of its exe has rows with no Process Start
# before them, or a lifetime of a run's ID ends with no load of its image, unless the run
# is a ByProcessId run, which Test-StartedProcesses refuses itself: whether those rows are
# the run's is not known. Any other process with such rows is given to no run, and a line
# says so, since rows given to no run judge nothing: a process started before the record
# began, such as a replace still running from an earlier update.
function Read-Record([string]$csv, [object[]]$runs, [System.Collections.Generic.List[object]]$started = $null,
    [System.Collections.Generic.HashSet[string]]$dropped = $null) {
  $usedParts.Clear()
  $unwritten = [ordered]@{}
  $byId = @{}
  foreach ($run in $runs) {
    $id = [string]$run.Id
    if (-not $byId.ContainsKey($id)) { $byId[$id] = [System.Collections.Generic.List[object]]::new() }
    $byId[$id].Add($run)
    $run | Add-Member -Force -NotePropertyName RecordKey -NotePropertyValue $id
    if (-not $run.PSObject.Properties['ByProcessId']) { $run | Add-Member -Force -NotePropertyName Image -NotePropertyValue (Get-RunImage $run) }
  }
  # Runs that share a process ID, a lifetime apart, are kept apart by label.
  foreach ($list in @($byId.Values | Where-Object Count -gt 1)) { foreach ($run in $list) { $run.RecordKey = "$($run.Id)|$($run.Label)" } }
  $names = [System.Collections.Generic.HashSet[string]]::new([string[]]@($runs | ForEach-Object Exe), [StringComparer]::OrdinalIgnoreCase)
  $starting = @(if ($null -ne $started) { $runs | Where-Object { Test-StartsReplace $_ } })
  $owners = @{}
  $unjudged = [System.Collections.Generic.HashSet[string]]::new()
  $live = @{}
  $byProcess = @{}
  $index = 0

  function Get-RowPath($row) {
    $full = Get-FullPath $row.Path
    if ($null -eq $full) {
      if (-not $unwritten.Contains($row.Path)) { $unwritten[$row.Path] = "$($row.'Process Name'), process $($row.PID)" }
      return $row.Path
    }
    $full
  }

  function Add-LifetimeRow($life, $row) {
    $row.Path = Get-RowPath $row
    $byProcess[$life.Key].Add($row)
  }

  # Whether $run's lifetime began before row $at and had not ended by then.
  function Test-LiveAt($run, [int]$at) {
    $life = $owners[$run.RecordKey]
    $life -and $life.Start -lt $at -and (-not $life.Ended -or $life.Ended -gt $at)
  }

  # Settles whose $life is, once its image is known or it has ended without one.
  function Resolve-Lifetime($life, [string]$image) {
    $life.Decided = $true
    $id = $life.Id
    $candidates = @($byId[$id] | Where-Object { $_ })
    # A process another started takes the first lifetime of its ID and name begun after the
    # row that named it, and of two such processes, the one named later.
    $children = @($candidates | Where-Object { $_.PSObject.Properties['ByProcessId'] -and $_.Exe -eq $life.Name -and $life.Start -gt $_.After -and -not $owners.ContainsKey($_.RecordKey) } |
      Sort-Object After -Descending | Select-Object -First 1)
    $claim = @(foreach ($run in $candidates) {
      $mine = if ($run.PSObject.Properties['ByProcessId']) { $run -in $children }
        else { $image -and $image -eq $run.Image -and (-not $run.PSObject.Properties['StartedBy'] -or (Test-LiveAt $run.StartedBy $life.Start)) }
      if ($mine) { $run }
      elseif ($image) { Write-Host "  process $id from row $($life.Start) of the record, running $image, is not $($run.Label), so those rows are not its" }
    })
    if (-not $claim -and $image) {
      $parent = @($starting | Where-Object { $_.Image -eq $image -and (Test-LiveAt $_ $life.Start) })
      if ($parent) {
        $replace = New-StartedReplace $parent[0] ([int]$id)
        $replace | Add-Member -Force -NotePropertyName RecordKey -NotePropertyValue "$id|$($replace.Label)|$($life.Start)"
        $replace | Add-Member -Force -NotePropertyName Image -NotePropertyValue $image
        $started.Add($replace)
        $claim = @($replace)
      }
    }
    if (-not $claim) {
      $own = @($candidates | Where-Object { $_.Exe -eq $life.Name -and -not $_.PSObject.Properties['ByProcessId'] })
      if (-not $image -and $own) { Fail "Process Monitor recorded no load of the image of $($life.Name), process $id, from row $($life.Start) of the record, so whether those rows are $($own[0].Label)'s is not known." }
      elseif (-not $image) { Write-Host "  $($life.Name), process $id, from row $($life.Start) of the record, loaded no image, so its rows are no run's" }
      $life.Held = $null
      return
    }
    if ($claim.Count -gt 1) {
      Fail "Process Monitor recorded process $id from row $($life.Start) of the record as the run of each of $(($claim | ForEach-Object Label) -join ' and '), so none of them is judged."
      foreach ($run in $claim) { $null = $unjudged.Add($run.RecordKey) }
      $life.Held = $null
      return
    }
    $run = $claim[0]
    if ($owners.ContainsKey($run.RecordKey)) {
      Fail "Process Monitor recorded two lifetimes of process $id, from rows $($owners[$run.RecordKey].Start) and $($life.Start) of the record, that are both $($run.Label), so neither is judged."
      $null = $unjudged.Add($run.RecordKey)
      $life.Held = $null
      return
    }
    $owners[$run.RecordKey] = $life
    $life.Key = $run.RecordKey
    $byProcess[$life.Key] = [System.Collections.Generic.List[object]]::new()
    foreach ($held in $life.Held) { Add-LifetimeRow $life $held }
    $life.Held = $null
  }

  function New-Lifetime([string]$id, [string]$name, [int]$start, [bool]$decided) {
    [pscustomobject]@{ Id = $id; Name = $name; Start = $start; Ended = $null; Decided = $decided; Key = $null
      Held = $(if (-not $decided) { , [System.Collections.Generic.List[object]]::new() }) }
  }

  Import-Csv $csv | ForEach-Object {
    $index++
    $name = $_.'Process Name'
    if (-not $names.Contains($name)) { return }
    $id = $_.PID
    $life = $live[$id]
    if ($_.Operation -eq 'Process Start' -or -not $life -or $life.Name -ne $name) {
      # A lifetime still waiting for its image ends here without one.
      if ($life -and -not $life.Decided) { Resolve-Lifetime $life '' }
      if ($_.Operation -eq 'Process Start') { $life = New-Lifetime $id $name $index $false }
      else {
        $own = @($byId[$id] | Where-Object { $_ -and $_.Exe -eq $name -and -not $_.PSObject.Properties['ByProcessId'] })
        if ($own) { Fail "Process Monitor recorded $name, process $id, from row $index of the record with no Process Start before it, so whether those rows are $($own[0].Label)'s is not known." }
        else { Write-Host "  $name, process $id, from row $index of the record: no Process Start before it, so its rows are no run's" }
        $life = New-Lifetime $id $name $index $true
      }
      $live[$id] = $life
    }
    if ($_.Operation -eq 'Process Exit') { $life.Ended = $index }
    if (-not $life.Decided) {
      $_ | Add-Member -NotePropertyName Row -NotePropertyValue $index
      $life.Held.Add($_)
      # Written in full only to be compared: a path that cannot be is failed once its rows are a run's.
      if ($_.Operation -eq 'Load Image') { $full = Get-FullPath $_.Path; Resolve-Lifetime $life $(if ($null -eq $full) { $_.Path } else { $full }) }
    }
    elseif ($life.Key) { $_ | Add-Member -NotePropertyName Row -NotePropertyValue $index; Add-LifetimeRow $life $_ }
  }
  foreach ($life in @($live.Values | Where-Object { -not $_.Decided })) { Resolve-Lifetime $life '' }
  # A run judged on none of its rows loses those read before that was known, too.
  foreach ($key in $unjudged) { $byProcess.Remove($key); if ($null -ne $dropped) { $null = $dropped.Add($key) } }
  Write-Host "Process Monitor recorded $([int]($byProcess.Values | Measure-Object -Property Count -Sum).Sum) operations by the builds."
  foreach ($short in $usedParts) { Write-Host "  paths under $short are read in full, under $($longParts[$short])" }
  foreach ($path in $unwritten.Keys) { Fail "$($unwritten[$path]): Process Monitor recorded $path, which could not be written in full, so the looks there were not judged." }
  $byProcess
}

# The key Read-Record gives $run's rows under: its process ID, or where another run in the
# same record had that ID, the ID with its label.
function Get-RunKey($run) { if ($run.PSObject.Properties['RecordKey']) { $run.RecordKey } else { [string]$run.Id } }

# The path of $run's exe, written in full where its folder is spelled in short names.
function Get-RunImage($run) {
  $path = "$($run.Folder.TrimEnd('\'))\$($run.Exe)"
  $full = Get-FullPath $path
  if ($null -eq $full) { $path } else { $full }
}

# Looks beside a run's exe that are for data rather than for a file to load, by exact
# extension. Any other look there, whatever its extension, is one for a file the run could
# load, since Windows maps whatever file it is named.
$dataExtensions = [System.Collections.Generic.HashSet[string]]::new([string[]]@(
    # The symbols a Debug build carries beside itself.
    '.pdb',
    # .NET Framework's own configuration, as InstallerClean.exe.config.
    '.config',
    # The language files Inno Setup's setup and setup.tmp look for beside themselves,
    # as InstallerClean-<version>-setup.en, .ENU and .en-US.
    '.en', '.enu', '.en-US'
  ), [StringComparer]::OrdinalIgnoreCase)

# Every look for a file in a run's own folder, or in $workingFolder where the run was
# started in a folder of its own, and every image it loaded from anywhere but its own exe.
# A look is left out where it is for data, by $dataExtensions; where it is for the run's own
# exe or a name a replace leaves beside it (Get-ReplaceSuffixes), which a start of the command
# line or the window clears, the exclusion going by name whatever the run; and where it is at
# a path the run opens as a folder or has rows under. The working folder itself, which Windows
# opens for the run as it starts, is not in it, so it is left out too.
function Get-Libraries($rows, [string]$folder, [string]$exe, [string]$workingFolder = '') {
  $own = $folder.TrimEnd('\') + '\'
  $working = if ($workingFolder) { $workingFolder.TrimEnd('\') + '\' }
  $self = $own + $exe
  $neighbours = @($self) + @(Get-ReplaceSuffixes | ForEach-Object { "$self$_" })
  # A path the run opens as a directory, or one another of its rows lies under.
  $folders = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
  foreach ($row in $rows) {
    if ($row.Operation -eq 'CreateFile' -and $row.Detail -match '(Options: |, )Directory(,| File|$)') { $null = $folders.Add($row.Path) }
    if (-not ($row.Path.StartsWith($own, 'OrdinalIgnoreCase') -or ($working -and $row.Path.StartsWith($working, 'OrdinalIgnoreCase')))) { continue }
    for ($at = $row.Path.LastIndexOf('\'); $at -gt 0 -and $folders.Add($row.Path.Substring(0, $at)); $at = $row.Path.LastIndexOf('\', $at - 1)) { }
  }
  for ($i = 0; $i -lt $rows.Count; $i++) {
    $row = $rows[$i]
    $place = if ($row.Path.StartsWith($own, 'OrdinalIgnoreCase')) { 'own' }
      elseif ($working -and $row.Path.StartsWith($working, 'OrdinalIgnoreCase')) { 'working' } else { 'elsewhere' }
    if ($row.Operation -eq 'Load Image') { if ($row.Path -eq $self) { continue } }
    elseif ($place -eq 'elsewhere' -or $folders.Contains($row.Path) -or $dataExtensions.Contains([IO.Path]::GetExtension($row.Path)) -or
      ($place -eq 'own' -and $row.Path -in $neighbours)) { continue }
    [pscustomobject]@{
      Index = $i + 1; Place = $place; Operation = $row.Operation; Result = $row.Result
      Relative = $(if ($place -eq 'own') { $row.Path.Substring($own.Length) } elseif ($place -eq 'working') { $row.Path.Substring($working.Length) })
      Path = $row.Path; Name = (Split-Path $row.Path -Leaf).ToLowerInvariant(); Time = $row.'Time of Day'
    }
  }
}

# The first act of the build's own code that Process Monitor can see, with its kind: the look
# for the names a replace leaves beside the exe, the app's folder under %LOCALAPPDATA% or the
# NoFaff one earlier versions use, a file under C:\Windows\Installer, or the load of msi.dll.
# A build started in replace mode makes one more, the replaced copy: its first row on the exe it
# puts itself at, or a name a replace leaves beside that exe. A row after it came after the
# build's own code started. Each build names the kind it shows first, and the first pass fails
# where it shows another.
function Get-Marker($rows, $run) {
  $exe = $run.Folder.TrimEnd('\') + '\' + $run.Exe
  $replaced = if ($run.PSObject.Properties['Target'] -and $run.Target) { @($run.Target) + @(Get-ReplaceSuffixes | ForEach-Object { "$($run.Target)$_" }) } else { @() }
  $leftovers = @(Get-ReplaceSuffixes | ForEach-Object { "$exe$_" })
  for ($i = 0; $i -lt $rows.Count; $i++) {
    $row = $rows[$i]
    $kind = if ($replaced -contains $row.Path) { 'replaced copy' }
      elseif ($leftovers -contains $row.Path) { 'replace leftovers' }
      elseif ($row.Path -match '\\AppData\\Local\\((NoFaff\\)?InstallerClean(\\|$)|NoFaff$)') { 'app folder' }
      elseif ($row.Path -match '^C:\\Windows\\Installer(\\|$)') { 'Installer folder' }
      elseif ($row.Operation -eq 'Load Image' -and (Split-Path $row.Path -Leaf) -eq 'msi.dll') { 'msi.dll' }
    if (-not $kind) { continue }
    return [pscustomobject]@{ Index = $i + 1; Kind = $kind; What = "$($row.Operation) $($row.Path)" }
  }
}

function Get-When($library, $marker) {
  if ($marker -and $library.Index -gt $marker.Index) { 'after its own code began' } else { 'before any act of its own code seen' }
}

function Show-Libraries($libraries, $marker, [string]$firstTime) {
  Write-Host "  first row at $firstTime"
  if ($marker) { Write-Host "  first act of its own code seen: row $($marker.Index), $($marker.Kind), $($marker.What)" }
  else { Write-Host "  no act of its own code seen" }
  Write-Host "  every look in its own folder and in a working folder of its own (row, time, operation, result, times seen, path):"
  $libraries | Where-Object { $_.Place -ne 'elsewhere' -and $_.Operation -ne 'Load Image' } |
    Group-Object Path, Operation, Result | Sort-Object { $_.Group[0].Index } | ForEach-Object {
      $first = $_.Group[0]
      Write-Host "    row $($first.Index)  $($first.Time)  $($first.Operation)  $($first.Result)  x$($_.Count)  $($first.Path)  ($(Get-When $first $marker))"
    }
  Write-Host "  libraries loaded:"
  $libraries | Where-Object Operation -eq 'Load Image' | ForEach-Object Path | Sort-Object -Unique | ForEach-Object { Write-Host "    $_" }
}

# The same as Show-Libraries for a program that is not one of the builds: its build
# names no kind of first act, so nothing places a look against the start of its own code.
function Show-Looks($libraries, [string]$firstTime) {
  Write-Host "  first row at $firstTime"
  Write-Host "  every look in its own folder (row, time, operation, result, times seen, path):"
  $libraries | Where-Object { $_.Place -ne 'elsewhere' -and $_.Operation -ne 'Load Image' } |
    Group-Object Path, Operation, Result | Sort-Object { $_.Group[0].Index } | ForEach-Object {
      $first = $_.Group[0]
      Write-Host "    row $($first.Index)  $($first.Time)  $($first.Operation)  $($first.Result)  x$($_.Count)  $($first.Path)"
    }
  Write-Host "  libraries loaded:"
  $libraries | Where-Object Operation -eq 'Load Image' | ForEach-Object Path | Sort-Object -Unique | ForEach-Object { Write-Host "    $_" }
}

# The genuine file for a name: one the build loaded under that name from
# anywhere but its own folder, else System32's, else the stand-in.
function Get-Plant([string]$name, $loads) {
  $loaded = @($loads | Where-Object { $_.Name -eq $name -and $_.Place -ne 'own' })
  $system = Join-Path $env:SystemRoot "System32\$name"
  if ($loaded) { [pscustomobject]@{ Source = $loaded[0].Path; Kind = 'the copy it loaded' } }
  elseif (Test-Path -LiteralPath $system) { [pscustomobject]@{ Source = $system; Kind = "System32's copy" } }
  else { [pscustomobject]@{ Source = $standIn; Kind = 'a stand-in, since no file on the runner carries this name' } }
}

# Prints a run's rows and returns its libraries, its loads and its marker, or
# nothing where Process Monitor recorded nothing for it.
function Read-Run($run, $byProcess) {
  Write-Host "== $($run.Label), $($run.Exe), process $($run.Id)"
  $rows = $byProcess[(Get-RunKey $run)]
  if (-not $rows) { Fail "$($run.Label): Process Monitor recorded nothing for process $($run.Id)."; return }
  Write-Host "  $($rows.Count) operations"
  $libraries = @(Get-Libraries $rows $run.Folder $run.Exe $run.WorkingFolder)
  $marker = $null
  if ($run.Build.Marker) {
    $marker = Get-Marker $rows $run
    Show-Libraries $libraries $marker $rows[0].'Time of Day'
  }
  else { Show-Looks $libraries $rows[0].'Time of Day' }
  [pscustomobject]@{ Libraries = $libraries; Loads = @($libraries | Where-Object Operation -eq 'Load Image'); Marker = $marker }
}

function Test-MustLoad($run, $loads) {
  if (-not $run.Build.MustLoad) { Fail "$($run.Label) names nothing it must load."; return }
  foreach ($name in $run.Build.MustLoad) {
    if (-not ($loads | Where-Object Name -eq $name)) {
      Fail "$($run.Label) never loaded $name, so it did not get far enough for this check to judge it."
    }
  }
}

# A run that checks for updates reaches github.com, which the update check alone
# contacts, and finds no newer release while the build's version is at or above the
# latest. Process Monitor names the far end of a connection by its host name where
# it can, and by its address otherwise, so an address counts where github.com resolved
# to it just before the run, just after it, or as it is judged: GitHub's addresses
# change from one lookup to the next. Every /s run writes one summary entry to
# the Application log, so a run with no entry there, or a log that cannot be read,
# fails rather than reading as one that found nothing newer. Only the entries written while the run was going are
# judged, so a run of the same exe before or after it cannot answer for it
# (Read-ApplicationLog).
function Test-UpdateCheck($run, $byProcess) {
  $github = @(@($script:GitHubAddressesByRun["$($run.Id)|$($run.Label)"]) + @(Get-GitHubAddresses) | Where-Object { $_ } | Sort-Object -Unique)
  $answers = @($byProcess[(Get-RunKey $run)] | Where-Object {
    $_.Operation -eq 'TCP Receive' -and $_.Path -match '-> (.+):(https|443)$' -and
    ($Matches[1] -match '(^|\.)github\.com$' -or $Matches[1] -in $github)
  })
  if ($answers) { "  $($run.Label): the update check had an answer from $(($answers[0].Path -split ' -> ')[-1]) at $($answers[0].'Time of Day')" }
  else {
    $seen = @($byProcess[(Get-RunKey $run)] | Where-Object { $_.Operation -eq 'TCP Receive' } | ForEach-Object { ($_.Path -split ' -> ')[-1] } | Sort-Object -Unique)
    Fail ("$($run.Label): the update check got no answer from GitHub, so the run's network libraries were not judged. " +
      "github.com resolved to $(if ($github) { $github -join ', ' } else { 'nothing' }) around the run; " +
      "the run received from $(if ($seen) { $seen -join ', ' } else { 'nowhere' }).")
  }
  $read = Read-ApplicationLog $run.Started $run.Ended
  $entries = @($read.Entries)
  $unread = @($read.Unread)
  foreach ($message in $unread) { Fail "$($run.Label): the Application log could not be read, or not in full, so whether it updated itself may not have been judged: $message" }
  if (-not $entries -and -not $unread) { Fail "$($run.Label) wrote no entry to the Application log, so whether it updated itself was not judged." }
  foreach ($entry in $entries) { "  $($run.Label): Event ID $($entry.Id) at $($entry.TimeCreated)" }
  foreach ($entry in @($entries | Where-Object Id -in 3005, 3006)) {
    if ($entry.Id -eq 3005) { Fail "$($run.Label) updated itself to a newer release, Event ID 3005 at $($entry.TimeCreated), so what ran was not this build." }
    else { Fail "$($run.Label) found a newer release and the update failed or was not tried, Event ID 3006 at $($entry.TimeCreated)." }
  }
}

# The folders under %LOCALAPPDATA% the app keeps its crash log in: its own, and the
# NoFaff one earlier versions use, which it keeps using where it cannot move it.
$crashLogFolders = 'InstallerClean', 'NoFaff\InstallerClean'

# The folder a run's copy of its build is started from, named for the pass and the run.
function Get-RunFolder([string]$pass, [string]$label) {
  Join-Path $env:RUNNER_TEMP ("$pass-" + ($label -replace '[^A-Za-z0-9]+', '-'))
}

# Ends the step with every failure listed, where there was one.
function Exit-IfFailed {
  if (-not $failures.Count) { return }
  Write-Host "$($failures.Count) checks failed:"
  $failures | ForEach-Object { Write-Host "  $_" }
  exit 1
}

# Downloads Process Monitor into $folder and answers its path. It stops where the file
# is not validly signed by Microsoft.
function Get-Procmon([string]$folder) {
  New-Item -ItemType Directory -Force -Path $folder | Out-Null
  $path = Join-Path $folder 'Procmon64.exe'
  Invoke-WebRequest https://live.sysinternals.com/Procmon64.exe -OutFile $path -ConnectionTimeoutSeconds 60 -OperationTimeoutSeconds 60
  $sig = Get-AuthenticodeSignature $path
  if ($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
    throw "Procmon64.exe is not validly signed by Microsoft: $($sig.Status), $($sig.SignerCertificate.Subject)"
  }
  $path
}

# The label of the replace a run of $label starts once its command has ended
# (Invoke-Replace's 'command'), which is judged as a run of its own.
function Get-StartedReplaceLabel([string]$label) { "$label, the replace it started" }

# The folders Windows' own libraries are loaded from: the ones a file of the same name planted
# beside an exe has to lose to.
function Get-WindowsLibraryFolders {
  $root = $env:SystemRoot.TrimEnd('\')
  @("$root\System32\", "$root\WinSxS\", "$root\Microsoft.NET\Framework64\")
}

# The folders an image may be loaded from: Windows and the folders only an administrator
# writes to.
function Get-ProtectedFolders {
  @($env:SystemRoot, $env:ProgramFiles, ${env:ProgramFiles(x86)}, (Join-Path $env:ProgramData 'Microsoft\Windows Defender')) |
    Where-Object { $_ } | ForEach-Object { $_.TrimEnd('\') + '\' }
}

# The genuine files a first-pass run's looks and loads call for in its second-pass run. A
# file is planted where the run looked for one beside its exe, or in the working folder it was
# started in, whatever its name; those plants say Why 'looked', and the second pass has to
# find each in place. A copy of every library the run loaded from Windows' own folders
# (Get-WindowsLibraryFolders) is planted beside the exe too, and in the working folder where
# there is one, under the name it was loaded by; those say Why 'loaded'. A run whose library
# search is System32 alone never looks for them there, so the second pass asks only that none
# is loaded or read from where it was planted.
function Get-RunPlants($run, $read) {
  $plants = [System.Collections.Generic.List[object]]::new()
  $looks = @($read.Libraries | Where-Object { $_.Operation -ne 'Load Image' -and $_.Path -notmatch '[*?]' })
  foreach ($relative in ($looks | Where-Object Place -eq 'own' | ForEach-Object { $_.Relative.ToLowerInvariant() } | Sort-Object -Unique)) {
    $plant = Get-Plant (Split-Path $relative -Leaf) $read.Loads
    $plants.Add([pscustomobject]@{ Place = 'beside'; Path = $relative; Source = $plant.Source; Kind = $plant.Kind; Why = 'looked' })
  }
  # A look in a working folder of the run's own is planted whatever its name.
  foreach ($relative in ($looks | Where-Object Place -eq 'working' | ForEach-Object { $_.Relative.ToLowerInvariant() } | Sort-Object -Unique)) {
    $plant = Get-Plant (Split-Path $relative -Leaf) $read.Loads
    $plants.Add([pscustomobject]@{ Place = 'working'; Path = $relative; Source = $plant.Source; Kind = $plant.Kind; Why = 'looked' })
  }
  $folders = @(Get-WindowsLibraryFolders)
  $places = @('beside') + @(if ($run.WorkingFolder) { 'working' })
  $taken = @($plants | ForEach-Object { "$($_.Place)|$($_.Path)" })
  $names = @($read.Loads | Where-Object {
    $path = $_.Path
    $_.Place -eq 'elsewhere' -and $_.Name.EndsWith('.dll') -and -not $_.Name.EndsWith('.ni.dll') -and
      @($folders | Where-Object { $path.StartsWith($_, 'OrdinalIgnoreCase') }).Count
  } | ForEach-Object Name | Sort-Object -Unique)
  foreach ($name in $names) {
    $plant = Get-Plant $name $read.Loads
    foreach ($place in $places) {
      if ("$place|$name" -in $taken) { continue }
      $plants.Add([pscustomobject]@{ Place = $place; Path = $name; Source = $plant.Source; Kind = $plant.Kind; Why = 'loaded' })
    }
  }
  @($plants)
}

# Puts each of $plants in place for a run started from $folder, working in
# $workingFolder, and answers each path with its hash and the plant's place and path as
# its key.
function Add-Plants([string]$folder, $plants, [string]$workingFolder = '') {
  $placed = [System.Collections.Generic.List[object]]::new()
  foreach ($plant in @($plants | Where-Object { $_ })) {
    $path = switch ($plant.Place) {
      'beside' { Join-Path $folder $plant.Path }
      'working' { if (-not $workingFolder) { throw "A plant in the working folder, $($plant.Path), was given no working folder." }; Join-Path $workingFolder $plant.Path }
      default { $plant.Path }
    }
    New-Item -ItemType Directory -Force -Path (Split-Path $path) | Out-Null
    Copy-Item -LiteralPath $plant.Source -Destination $path
    $placed.Add([pscustomobject]@{ Path = $path; Hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash; Key = "$($plant.Place)|$($plant.Path)"; Why = $plant.Why })
  }
  $placed
}

# When a row came, against the start of the build's own code, for a message about a
# build; nothing for a program whose build names no kind of first act.
function Get-WhenSuffix($run, $row, $marker) {
  if ($run.Build.Marker) { ", $(Get-When $row $marker)" } else { '' }
}

# Judges a second-pass run against the files planted for it. For a program whose build names
# no kind of first act, a load from its own folder is left to Test-OwnLoads.
function Test-PlantedRun($run, $read) {
  # A native library is mapped and loaded; a managed one can be read instead, so
  # any of the three by the build counts as the planted file being taken.
  # A run sharing its folder with another is judged against that run's plants too.
  $placedPaths = @(@($run.Placed) + @($run.SharedPlaced) | Where-Object { $_ } | ForEach-Object Path)
  $loadedPaths = @($read.Loads | ForEach-Object Path)
  foreach ($taken in ($read.Libraries | Where-Object { $_.Operation -in 'CreateFileMapping', 'ReadFile' -and $placedPaths -contains $_.Path -and $loadedPaths -notcontains $_.Path } | Group-Object Path)) {
    $first = $taken.Group[0]
    Fail "$($run.Label) read the planted $($first.Path), $($first.Operation) at row $($first.Index)$(Get-WhenSuffix $run $first $read.Marker)."
  }
  foreach ($load in $read.Loads) {
    if ($placedPaths -contains $load.Path) {
      Fail "$($run.Label) loaded the planted $($load.Path), row $($load.Index)$(Get-WhenSuffix $run $load $read.Marker)."
    }
    elseif ($run.Build.Marker -and $load.Place -in 'own', 'working') {
      Fail "$($run.Label) loaded $($load.Path), which the first pass did not plant, row $($load.Index)$(Get-WhenSuffix $run $load $read.Marker)."
    }
  }
  foreach ($plant in $run.Placed) {
    # A file planted for a library the run loaded from Windows is never looked for beside
    # the exe by a run that searches System32 alone, so only a plant for a look has to be
    # looked at and found.
    if ($plant.Why -ne 'loaded') {
      $at = @($read.Libraries | Where-Object { $_.Path -eq $plant.Path -and $_.Operation -ne 'Load Image' })
      $found = @($at | Where-Object { $_.Result -eq 'SUCCESS' -and $_.Operation -in 'QueryOpen', 'CreateFile' })
      if (-not $at) { Fail "$($run.Label) did not look for the planted $($plant.Path)." }
      elseif (-not $found) {
        Fail "The planted $($plant.Path) was not there when $($run.Label) looked: $(($at | ForEach-Object { "$($_.Operation) $($_.Result)" } | Sort-Object -Unique) -join ', ')."
      }
    }
    if (-not (Test-Path -LiteralPath $plant.Path)) { Fail "The planted $($plant.Path) was gone once $($run.Label) ended." }
    elseif ((Get-FileHash -Algorithm SHA256 -LiteralPath $plant.Path).Hash -ne $plant.Hash) { Fail "The planted $($plant.Path) was changed while $($run.Label) ran." }
  }
}

# Fails where the run's process loaded an image from outside the folders only an administrator
# writes to (Get-ProtectedFolders). The run's own folder and working folder are judged by
# Test-PlantedRun and Test-OwnLoads; this covers every other path, such as a user's profile or
# a temp folder.
function Test-ImageFolders($run, $read) {
  $protected = @(Get-ProtectedFolders)
  foreach ($load in @($read.Loads | Where-Object Place -eq 'elsewhere')) {
    $path = $load.Path
    if (-not @($protected | Where-Object { $path.StartsWith($_, 'OrdinalIgnoreCase') })) {
      Fail "$($run.Label) loaded $path, row $($load.Index), which is outside $($protected -join ', ')."
    }
  }
}

# The is-*.tmp folder a path lies under, the first along it, or nothing. Inno Setup
# makes these in a temp folder (Get-TempFolders), new and newly named for every run.
function Get-IsTmpFolder([string]$path) {
  if ($path -match '^(.*?\\is-[^\\]+\.tmp)\\') { $Matches[1] }
}

# Fails where the run's process maps an image from its own folder, of any name but its
# own exe's, or from an is-*.tmp folder. An image in an is-*.tmp folder that this same
# process created at that path earlier in the record, a CreateFile whose result is
# Created, is printed and passes: a file a process has just written into a folder made
# new for the run is not one left there for it. Answers each is-*.tmp folder an image
# was mapped from.
function Test-OwnLoads($run, $rows) {
  $own = $run.Folder.TrimEnd('\') + '\'
  $self = $own + $run.Exe
  $tempFolders = [System.Collections.Generic.List[string]]::new()
  # The row on which this process first created each path, numbered from one.
  $createdAt = [System.Collections.Generic.Dictionary[string, int]]::new([StringComparer]::OrdinalIgnoreCase)
  for ($i = 0; $i -lt $rows.Count; $i++) {
    $r = $rows[$i]
    if ($r.Operation -eq 'CreateFile' -and $r.Detail -match 'OpenResult: Created' -and -not $createdAt.ContainsKey($r.Path)) { $createdAt[$r.Path] = $i + 1 }
  }
  for ($i = 0; $i -lt $rows.Count; $i++) {
    $row = $rows[$i]
    if ($row.Operation -ne 'Load Image' -or $row.Path -eq $self) { continue }
    $inOwn = $row.Path.StartsWith($own, 'OrdinalIgnoreCase')
    $isTmp = Get-IsTmpFolder $row.Path
    if (-not $inOwn -and -not $isTmp) { continue }
    if ($isTmp) {
      if ($tempFolders -notcontains $isTmp) { $tempFolders.Add($isTmp) }
      $made = 0
      if ($createdAt.TryGetValue($row.Path, [ref]$made) -and $made -le $i) { Write-Host "  loaded $($row.Path) at row $($i + 1), which it created at row $made"; continue }
    }
    if ($inOwn) { Fail "$($run.Exe), process $($run.Id), loaded $($row.Path) from its own folder, row $($i + 1)." }
    else { Fail "$($run.Exe), process $($run.Id), loaded $($row.Path) from the is-*.tmp folder $isTmp, which it did not create there, row $($i + 1)." }
  }
  $tempFolders
}

# Each process $parent started, other than one whose image is at $skip, and each
# process those started, read from the record $csv by the process ID in each Process
# Create row. Each is printed with its looks and judged by Test-OwnLoads. Command lines
# are not printed. Answers each process as a run, with the is-*.tmp folders it loaded
# images from.
#
# A Process Create row spells the image's path as the parent passed it, which can be
# in short names, as in C:\Users\RUNNER~1. So a process's folder is taken from its own
# load of its image, spelled as its other rows are, and a process with no such load
# fails and is not judged. A process whose image is at $skip is left out by either
# spelling.
function Test-StartedProcesses([string]$csv, $parent, $parentRows, [string]$skip) {
  $all = [System.Collections.Generic.List[object]]::new()
  $parents = @([pscustomobject]@{ Run = $parent; Rows = $parentRows })
  foreach ($depth in 1, 2) {
    $started = @(foreach ($p in $parents) {
      foreach ($row in @($p.Rows | Where-Object Operation -eq 'Process Create')) {
        if ($skip -and $row.Path -eq $skip) { continue }
        if ($row.Detail -notmatch '^PID: (\d+)') { Fail "A Process Create row of process $($p.Run.Id) names no process ID."; continue }
        [pscustomobject]@{ Label = Split-Path $row.Path -Leaf; Build = [pscustomobject]@{ Exe = $row.Path }; Folder = Split-Path $row.Path
          Exe = Split-Path $row.Path -Leaf; Id = [int]$Matches[1]; Parent = "$($p.Run.Exe), process $($p.Run.Id)"; StartedAs = $row.Path; TempFolders = @()
          ByProcessId = $true; After = $(if ($row.PSObject.Properties['Row']) { $row.Row } else { 0 }) }
      }
    })
    if (-not $started) { break }
    $byChild = Read-Record $csv $started
    $next = foreach ($child in $started) {
      $rows = $byChild[(Get-RunKey $child)]
      if (-not $rows) { $all.Add($child); Fail "Process Monitor recorded nothing for $($child.Exe), process $($child.Id)."; continue }
      $image = @($rows | Where-Object { $_.Operation -eq 'Load Image' -and (Split-Path $_.Path -Leaf) -eq $child.Exe })
      if (-not $image) { $all.Add($child); Fail "$($child.Exe), process $($child.Id), started as $($child.StartedAs): Process Monitor recorded no load of its image, so its folder is not known and its looks were not judged."; continue }
      if ($skip -and $image[0].Path -eq $skip) { continue }
      $child.Build.Exe = $image[0].Path
      $child.Folder = Split-Path $image[0].Path
      $all.Add($child)
      Write-Host "== $($child.Exe), process $($child.Id), started by $($child.Parent), from $($child.Folder)"
      if ($child.StartedAs -ne $image[0].Path) { Write-Host "  started as $($child.StartedAs)" }
      Show-Looks @(Get-Libraries $rows $child.Folder $child.Exe) $rows[0].'Time of Day'
      $child.TempFolders = @(Test-OwnLoads $child $rows)
      [pscustomobject]@{ Run = $child; Rows = $rows }
    }
    $parents = @($next)
  }
  $all
}

# The temp folders a setup can make its is-*.tmp folder in: %TEMP%, %TMP% and what
# GetTempPath answers. Each folder is answered once, in its long spelling where Windows
# gives one and as it was given where not, and two spellings Windows gives the same short
# name for are one folder.
function Get-TempFolders {
  $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
  $folders = [System.Collections.Generic.List[string]]::new()
  foreach ($folder in @($env:TEMP, $env:TMP, [IO.Path]::GetTempPath()) | Where-Object { $_ }) {
    $folder = $folder.TrimEnd('\')
    $long = Get-LongName $folder
    $short = Get-ShortName $folder
    $key = if ($short) { $short.TrimEnd('\') } else { $folder }
    if ($seen.Add($key)) { $folders.Add($(if ($long) { $long.TrimEnd('\') } else { $folder })) }
  }
  , $folders.ToArray()
}

# Starts a job writing to $out each is-*.tmp folder that appears in any of the folders
# Get-TempFolders names, with its owner and its permissions as read the moment it is
# seen, until $stop exists. A folder there before the job starts is left out. Answers
# the job once it has started.
function Start-TempWatch([string]$out, [string]$stop) {
  $job = Start-Job -ArgumentList (Get-TempFolders), $out, $stop -ScriptBlock {
    param($temps, $out, $stop)
    $seen = @{}
    foreach ($d in @($temps | ForEach-Object { Get-ChildItem -LiteralPath $_ -Directory -Filter 'is-*.tmp' -ErrorAction SilentlyContinue })) { $seen[$d.FullName] = $true }
    Set-Content -Path $out -Value ''
    while (-not (Test-Path -LiteralPath $stop)) {
      foreach ($d in @($temps | ForEach-Object { Get-ChildItem -LiteralPath $_ -Directory -Filter 'is-*.tmp' -ErrorAction SilentlyContinue })) {
        if ($seen.ContainsKey($d.FullName)) { continue }
        try {
          $acl = Get-Acl -LiteralPath $d.FullName
          "$($d.FullName)`towner $($acl.Owner)`t$($acl.GetSecurityDescriptorSddlForm('Access'))" | Add-Content -Path $out
          $seen[$d.FullName] = $true
        }
        catch { }
      }
      Start-Sleep -Milliseconds 50
    }
  }
  $deadline = [DateTime]::UtcNow.AddSeconds(60)
  while (-not (Test-Path -LiteralPath $out)) {
    if ([DateTime]::UtcNow -gt $deadline) { throw "The job watching the temp folders did not start within 60 s." }
    Start-Sleep -Milliseconds 200
  }
  $job
}

# Ends the job Start-TempWatch started. An error in ending it is printed.
function Stop-TempWatch($job, [string]$stop) {
  try {
    New-Item -ItemType File -Path $stop -Force | Out-Null
    $null = Wait-Job $job -Timeout 60
    Receive-Job $job | Out-Host
  }
  catch { Write-Host "The job watching the temp folders did not end cleanly: $($_.Exception.Message)" }
  finally { Remove-Job $job -Force -ErrorAction SilentlyContinue }
}

# Prints the folders the watch wrote to $out. Fails where none of $runs ran from an
# is-*.tmp folder, and where one ran from or loaded an image from such a folder that
# the watch did not see made, whose permissions are then not known.
function Show-TempFolders([string]$out, $runs) {
  Write-Host "== The is-*.tmp folders made in $((Get-TempFolders) -join ', ') (path, owner, permissions):"
  $made = @(Get-Content $out -ErrorAction SilentlyContinue | Where-Object { $_ })
  foreach ($line in $made) { Write-Host "  $line" }
  $madePaths = @($made | ForEach-Object { ($_ -split "`t")[0] })
  $ranFromTemp = $false
  foreach ($run in $runs) {
    $ranFrom = Get-IsTmpFolder ($run.Folder.TrimEnd('\') + '\')
    if ($ranFrom) {
      $ranFromTemp = $true
      if ($madePaths -contains $ranFrom) { Write-Host "  $($run.Exe), process $($run.Id), ran from $ranFrom, printed above" }
      else { Fail "$($run.Exe), process $($run.Id), ran from $ranFrom, which the watch on the temp folders did not see made, so its permissions are not known." }
    }
    foreach ($folder in @($run.TempFolders | Where-Object { $_ -and $_ -ne $ranFrom })) {
      if ($madePaths -contains $folder) { Write-Host "  $($run.Exe), process $($run.Id), loaded images from $folder, printed above" }
      else { Fail "$($run.Exe), process $($run.Id), loaded images from $folder, which the watch on the temp folders did not see made, so its permissions are not known." }
    }
  }
  if (-not $ranFromTemp) { Fail "No process the setup started ran from an is-*.tmp folder." }
}

# Runs $block with Fail collecting each message rather than failing the step. Answers
# what $block answered, as Output, and the messages, as Caught.
function Invoke-Caught([scriptblock]$block) {
  $caught = [System.Collections.Generic.List[string]]::new()
  $output = & {
    function Fail([string]$message) { $caught.Add($message) }
    & $block
  }
  [pscustomobject]@{ Output = $output; Caught = $caught }
}

# Fails the step unless each of $must is part of exactly one of $caught, and for each of
# $caught that none of $must is part of. $what names the control in each message.
function Assert-Caught($caught, [string[]]$must, [string]$what) {
  foreach ($message in $must) {
    $hits = @($caught | Where-Object { $_.Contains($message) })
    if ($hits.Count -eq 1) { Write-Host "  failed as it must: $($hits[0])" }
    else { Fail "The control on $what failed $($hits.Count) times where one failure names: $message" }
  }
  foreach ($message in $caught) {
    if (-not @($must | Where-Object { $message.Contains($_) })) { Fail "The control on $what failed where it must pass: $message" }
  }
}

# Runs Test-StartedProcesses and Show-TempFolders over a record made up for the
# purpose, and fails the step unless each fails exactly where it must. The setup's
# started process is created under a short-named path, as in C:\Users\RUNNER~1, with
# its own rows in full names. It loads a file nobody created from its own is-*.tmp
# folder, one it opened there without creating it, one nobody created from another
# is-*.tmp folder, and one from an is-*.tmp folder the watch did not see made, each of
# which must fail; and one it created in its own folder and one it created in another
# is-*.tmp folder, each of which must pass. A second process, created under a short
# name for $skip's path, must be left out, and a third, with no load of its image,
# must fail. The setup exe itself is judged as a workflow judges it: an image it
# created in an is-*.tmp folder the watch did not see made must fail.
function Test-SetupControls {
  Write-Host "== Control: the checks on the processes a setup starts"
  $temp = 'C:\Users\runneradmin\AppData\Local\Temp'
  $own = "$temp\is-AAAA.tmp"
  $other = "$temp\is-BBBB.tmp"
  $unseen = "$temp\is-CCCC.tmp"
  $skip = 'C:\Program Files\Control\skipped.exe'
  $created = 'Desired Access: Generic Read/Write, Disposition: OverwriteIf, OpenResult: Created'
  $opened = 'Desired Access: Generic Read, Disposition: Open, OpenResult: Opened'
  $rows = @(
    @(201, 'control.tmp', 'Process Start', '', ''),
    @(201, 'control.tmp', 'Load Image', "$own\control.tmp", ''),
    @(201, 'control.tmp', 'Load Image', "$own\planted.dll", ''),
    @(201, 'control.tmp', 'CreateFile', "$own\opened.dll", $opened),
    @(201, 'control.tmp', 'Load Image', "$own\opened.dll", ''),
    @(201, 'control.tmp', 'CreateFile', "$own\made.dll", $created),
    @(201, 'control.tmp', 'Load Image', "$own\made.dll", ''),
    @(201, 'control.tmp', 'CreateFile', "$other\_isetup\helper.dll", $created),
    @(201, 'control.tmp', 'Load Image', "$other\_isetup\helper.dll", ''),
    @(201, 'control.tmp', 'Load Image', "$other\uncreated.dll", ''),
    @(201, 'control.tmp', 'CreateFile', "$unseen\helper.dll", $created),
    @(201, 'control.tmp', 'Load Image', "$unseen\helper.dll", ''),
    @(202, 'skipped.exe', 'Process Start', '', ''),
    @(202, 'skipped.exe', 'Load Image', $skip, ''),
    @(202, 'skipped.exe', 'Load Image', 'C:\Program Files\Control\planted.dll', ''),
    @(203, 'noimage.exe', 'Process Start', '', ''),
    @(203, 'noimage.exe', 'CreateFile', 'C:\Windows\System32\kernel32.dll', $opened)
  ) | ForEach-Object {
    [pscustomobject]@{ 'Time of Day' = '12:00:00.0000000 PM'; 'Process Name' = $_[1]; PID = [string]$_[0]; Operation = $_[2]; Path = $_[3]; Result = 'SUCCESS'; Detail = $_[4] }
  }
  $parent = [pscustomobject]@{ Exe = 'control-setup.exe'; Id = 200; Folder = 'C:\Control'; TempFolders = @() }
  $parentRows = @(
    [pscustomobject]@{ Operation = 'CreateFile'; Path = "$temp\is-DDDD.tmp\loader.dll"; Detail = $created },
    [pscustomobject]@{ Operation = 'Load Image'; Path = "$temp\is-DDDD.tmp\loader.dll"; Detail = '' },
    [pscustomobject]@{ Operation = 'Process Create'; Path = 'C:\Users\RUNNER~1\AppData\Local\Temp\is-AAAA.tmp\control.tmp'; Detail = 'PID: 201' },
    [pscustomobject]@{ Operation = 'Process Create'; Path = 'C:\PROGRA~1\Control\skipped.exe'; Detail = 'PID: 202' },
    [pscustomobject]@{ Operation = 'Process Create'; Path = 'C:\Control\noimage.exe'; Detail = 'PID: 203' })
  $folder = Join-Path ([IO.Path]::GetTempPath()) ('setup-controls-' + [guid]::NewGuid().ToString('N'))
  try {
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
    $csv = Join-Path $folder 'record.csv'
    $out = Join-Path $folder 'temp.txt'
    $rows | Export-Csv -Path $csv -NoTypeInformation
    Set-Content -Path $out -Value "$own`towner control`tD:P", "$other`towner control`tD:P"
    $result = Invoke-Caught {
      $parent.TempFolders = @(Test-OwnLoads $parent $parentRows)
      $runs = @(Test-StartedProcesses $csv $parent $parentRows $skip)
      Show-TempFolders $out (@($parent) + $runs)
      $runs
    }
  }
  finally { Remove-Item -LiteralPath $folder -Recurse -Force -ErrorAction SilentlyContinue }
  Assert-Caught $result.Caught @(
    "loaded $own\planted.dll from its own folder",
    "loaded $own\opened.dll from its own folder",
    "loaded $other\uncreated.dll from the is-*.tmp folder $other, which it did not create there",
    "loaded images from $unseen, which the watch",
    "control-setup.exe, process 200, loaded images from $temp\is-DDDD.tmp, which the watch",
    'noimage.exe, process 203, started as C:\Control\noimage.exe: Process Monitor recorded no load of its image') "a setup's processes"
  $child = @($result.Output | Where-Object Id -eq 201)
  if ($child.Count -ne 1 -or $child[0].Folder -ne $own) { Fail "The control's setup.tmp was not judged from its own folder $own." }
  if (@($result.Output | Where-Object Id -eq 202)) { Fail "The control's process at $skip was judged, where it is to be left out." }
}

# Runs Get-Libraries, Get-RunPlants and Test-PlantedRun over records made up for the
# purpose. The first pass looks beside the exe for a .dll, an upper-case .OCX, a .drv, an
# .exe and a dotted name with no extension Windows knows, each of which must be planted
# for, and for each data name $dataExtensions holds, the exe itself, the names a replace
# leaves beside it, and a folder it creates, none of which may be. It loads a library from
# System32 that it never looks for beside itself, which must be planted too, one native image
# of a managed assembly and one library from outside Windows' own folders, which may not be.
# The second pass must fail where it loads the planted .drv, the planted System32 name or a
# file from beside itself that the first pass did not look for, and pass the load of its own
# exe. A second pass that loads the System32 library from System32 and nothing planted must
# pass, though it never looks for the file planted under that name.
function Test-ImageNameControls {
  Write-Host "== Control: the looks for a file to load beside a build"
  $folder = Join-Path ([IO.Path]::GetTempPath()) ('image-controls-' + [guid]::NewGuid().ToString('N'))
  $first = Join-Path $folder 'first'
  $second = Join-Path $folder 'second'
  $third = Join-Path $folder 'third'
  $source = Join-Path $folder 'genuine.bin'
  $system = Join-Path $env:SystemRoot 'System32'
  function Get-ControlRow([string]$operation, [string]$path, [string]$detail = '', [string]$result = 'NAME NOT FOUND') {
    [pscustomobject]@{ 'Time of Day' = '12:00:00.0000000 PM'; Operation = $operation; Result = $result; Path = $path; Detail = $detail }
  }
  $planted = 'control.dll', 'control.ocx', 'controlsys.dll', 'helper.exe', 'system.io.compression.native', 'winspool.drv'
  $looked = 'control.dll', 'CONTROL.OCX', 'winspool.drv', 'helper.exe', 'System.IO.Compression.Native',
    'control.pdb', 'control.dll.config', 'control.en', 'control.ENU', 'control.en-US',
    'control.exe', 'work'
  $looked = @($looked) + @(Get-ReplaceSuffixes | ForEach-Object { "control.exe$_" })
  try {
    New-Item -ItemType Directory -Force -Path $first, $second, $third | Out-Null
    Set-Content -Path $source -Value 'control'
    $rows = @($looked | ForEach-Object { Get-ControlRow 'QueryOpen' (Join-Path $first $_) }) +
      @(Get-ControlRow 'CreateFile' (Join-Path $first 'work') 'Desired Access: Read Data/List Directory, Synchronize, Disposition: Create, Options: Directory, Synchronous IO Non-Alert') +
      @("$system\controlsys.dll", "$system\controlsys.ni.dll", "$env:SystemRoot\assembly\NativeImages\control.ni.dll", "$env:ProgramFiles\Control\controlpf.dll" |
        ForEach-Object { Get-ControlRow 'Load Image' $_ '' 'SUCCESS' })
    $run = [pscustomobject]@{ Label = 'image control'; Build = [pscustomobject]@{ Exe = 'control.exe'; Marker = 'app folder' }; Folder = $first; Exe = 'control.exe' }
    $firstPass = Invoke-Caught {
      $libraries = @(Get-Libraries $rows $first 'control.exe')
      @(Get-RunPlants $run ([pscustomobject]@{ Libraries = $libraries; Loads = @($libraries | Where-Object Operation -eq 'Load Image'); Marker = $null }))
    }
    $plants = @($firstPass.Output)
    $names = @($plants | ForEach-Object Path | Sort-Object) -join ', '
    if ($firstPass.Caught.Count -or $names -ne ($planted -join ', ')) {
      foreach ($message in $firstPass.Caught) { Fail "The control on the looks beside a build failed where it must pass: $message" }
      Fail "The control on the looks beside a build planted '$names', where it should plant $($planted -join ', ')."
      return
    }
    Write-Host "  planted for the second pass: $names"
    $why = @($plants | Where-Object Why -eq 'loaded' | ForEach-Object Path) -join ', '
    if ($why -ne 'controlsys.dll') { Fail "The control on the looks beside a build planted '$why' for the libraries it loaded, where it should plant controlsys.dll."; return }

    foreach ($plant in $plants) { $plant.Source = $source }
    $placed = @(Add-Plants $second $plants)
    $rows = @($placed | Where-Object Why -ne 'loaded' | ForEach-Object { Get-ControlRow 'QueryOpen' $_.Path '' 'SUCCESS' }) +
      @('winspool.drv', 'controlsys.dll', 'unlooked.weird', 'control.exe' | ForEach-Object { Get-ControlRow 'Load Image' (Join-Path $second $_) '' 'SUCCESS' })
    $libraries = @(Get-Libraries $rows $second 'control.exe')
    $read = [pscustomobject]@{ Libraries = $libraries; Loads = @($libraries | Where-Object Operation -eq 'Load Image'); Marker = $null }
    $run = [pscustomobject]@{ Label = 'image control'; Build = $run.Build; Folder = $second; Exe = 'control.exe'; Placed = $placed }
    $secondPass = Invoke-Caught { Test-PlantedRun $run $read }

    $placedClean = @(Add-Plants $third $plants)
    $rows = @($placedClean | Where-Object Why -ne 'loaded' | ForEach-Object { Get-ControlRow 'QueryOpen' $_.Path '' 'SUCCESS' }) +
      @("$system\controlsys.dll", (Join-Path $third 'control.exe') | ForEach-Object { Get-ControlRow 'Load Image' $_ '' 'SUCCESS' })
    $libraries = @(Get-Libraries $rows $third 'control.exe')
    $read = [pscustomobject]@{ Libraries = $libraries; Loads = @($libraries | Where-Object Operation -eq 'Load Image'); Marker = $null }
    $run = [pscustomobject]@{ Label = 'image control, clean'; Build = $run.Build; Folder = $third; Exe = 'control.exe'; Placed = $placedClean }
    $thirdPass = Invoke-Caught { Test-PlantedRun $run $read }
  }
  finally { Remove-Item -LiteralPath $folder -Recurse -Force -ErrorAction SilentlyContinue }
  Assert-Caught $secondPass.Caught @(
    "image control loaded the planted $(Join-Path $second 'winspool.drv'), row ",
    "image control loaded the planted $(Join-Path $second 'controlsys.dll'), row ",
    "image control loaded $(Join-Path $second 'unlooked.weird'), which the first pass did not plant") 'the looks beside a build'
  Assert-Caught $thirdPass.Caught @() 'a run that loads each library from System32'
}

# Runs Get-Libraries, Get-RunPlants and Test-PlantedRun over records made up for a run
# started in a working folder of its own. The first pass looks there for a .dll, which
# must be planted there, and for a data name, which may not be; and it looks beside the exe
# for a .dll, which must be planted there. It opens the working folder itself as well, as Windows does for a run. The
# second pass must fail where it loads the .dll planted in the working folder and where it
# loads a file from the working folder that the first pass did not look for.
function Test-WorkingFolderControls {
  Write-Host "== Control: the looks in a working folder of a build's own"
  $folder = Join-Path ([IO.Path]::GetTempPath()) ('working-controls-' + [guid]::NewGuid().ToString('N'))
  $first = Join-Path $folder 'first'
  $firstWork = Join-Path $folder 'first-work'
  $second = Join-Path $folder 'second'
  $secondWork = Join-Path $folder 'second-work'
  $source = Join-Path $folder 'genuine.bin'
  function Get-ControlRow([string]$operation, [string]$path, [string]$detail = '', [string]$result = 'NAME NOT FOUND') {
    [pscustomobject]@{ 'Time of Day' = '12:00:00.0000000 PM'; Operation = $operation; Result = $result; Path = $path; Detail = $detail }
  }
  $build = [pscustomobject]@{ Exe = 'control.exe'; Marker = 'app folder' }
  try {
    New-Item -ItemType Directory -Force -Path $first, $firstWork, $second, $secondWork | Out-Null
    Set-Content -Path $source -Value 'control'
    $rows = @(
      Get-ControlRow 'CreateFile' $firstWork 'Desired Access: Execute/Traverse, Synchronize, Disposition: Open, Options: Directory, Synchronous IO Non-Alert' 'SUCCESS'
      Get-ControlRow 'QueryOpen' (Join-Path $firstWork 'control.dll')
      Get-ControlRow 'QueryOpen' (Join-Path $firstWork 'control.pdb')
      Get-ControlRow 'QueryOpen' (Join-Path $first 'helper.dll'))
    $run = [pscustomobject]@{ Label = 'working control'; Build = $build; Folder = $first; Exe = 'control.exe'; WorkingFolder = $firstWork }
    $firstPass = Invoke-Caught {
      @(Get-RunPlants $run ([pscustomobject]@{ Libraries = @(Get-Libraries $rows $first 'control.exe' $firstWork); Loads = @(); Marker = $null }))
    }
    $plants = @($firstPass.Output | Where-Object { $_ -is [pscustomobject] -and $_.PSObject.Properties['Place'] })
    $names = @($plants | ForEach-Object { "$($_.Place) $($_.Path)" } | Sort-Object) -join ', '
    if ($firstPass.Caught.Count -or $names -ne 'beside helper.dll, working control.dll') {
      foreach ($message in $firstPass.Caught) { Fail "The control on the looks in a working folder failed where it must pass: $message" }
      Fail "The control on the looks in a working folder planted '$names', where it should plant beside helper.dll, working control.dll."
      return
    }
    Write-Host "  planted for the second pass: $names"

    foreach ($plant in $plants) { $plant.Source = $source }
    $placed = @(Add-Plants $second $plants $secondWork)
    $rows = @($placed | ForEach-Object { Get-ControlRow 'QueryOpen' $_.Path '' 'SUCCESS' }) +
      @((Join-Path $secondWork 'control.dll'), (Join-Path $secondWork 'unlooked.dll'), (Join-Path $second 'control.exe') | ForEach-Object { Get-ControlRow 'Load Image' $_ '' 'SUCCESS' })
    $libraries = @(Get-Libraries $rows $second 'control.exe' $secondWork)
    $read = [pscustomobject]@{ Libraries = $libraries; Loads = @($libraries | Where-Object Operation -eq 'Load Image'); Marker = $null }
    $run = [pscustomobject]@{ Label = 'working control'; Build = $build; Folder = $second; Exe = 'control.exe'; WorkingFolder = $secondWork; Placed = $placed }
    $secondPass = Invoke-Caught { Test-PlantedRun $run $read }
  }
  finally { Remove-Item -LiteralPath $folder -Recurse -Force -ErrorAction SilentlyContinue }
  Assert-Caught $secondPass.Caught @(
    "working control loaded the planted $(Join-Path $secondWork 'control.dll'), row ",
    "working control loaded $(Join-Path $secondWork 'unlooked.dll'), which the first pass did not plant") 'the looks in a working folder'
}

# Runs Read-Record, Get-Libraries and Get-RunPlants over a record made up for the
# purpose. A look beside the exe spelled under the short name Windows gives the temp
# folder must be read in full and planted for, and a look under a short part that does
# not exist must fail, naming the path.
function Test-ShortNameControls {
  Write-Host "== Control: looks recorded under short names"
  $long = Get-LongName ([IO.Path]::GetTempPath().TrimEnd('\'))
  $short = if ($long) { Get-ShortName $long }
  if (-not $short -or -not $short.Contains('~')) { Fail "The control on short names could not be made: Windows gives the temp folder $long no short name holding '~'."; return }
  $folder = "$long\control-short-names"
  $missing = 'C:\NOSUCH~9\control\helper.dll'
  $run = [pscustomobject]@{ Label = 'short-name control'; Build = [pscustomobject]@{ Exe = 'control.exe' }; Folder = $folder; Exe = 'control.exe'; Id = 300 }
  $csv = Join-Path ([IO.Path]::GetTempPath()) ('short-name-control-' + [guid]::NewGuid().ToString('N') + '.csv')
  try {
    @(
      @('Process Start', 'SUCCESS', ''),
      @('Load Image', 'SUCCESS', "$folder\control.exe"),
      @('QueryOpen', 'NAME NOT FOUND', "$short\control-short-names\winspool.drv"),
      @('QueryOpen', 'NAME NOT FOUND', $missing)
    ) | ForEach-Object {
      [pscustomobject]@{ 'Time of Day' = '12:00:00.0000000 PM'; 'Process Name' = 'control.exe'; PID = '300'; Operation = $_[0]; Path = $_[2]; Result = $_[1]; Detail = '' }
    } | Export-Csv -Path $csv -NoTypeInformation
    $result = Invoke-Caught {
      $rows = (Read-Record $csv @($run))['300']
      @(Get-RunPlants $run ([pscustomobject]@{ Libraries = @(Get-Libraries $rows $folder 'control.exe'); Loads = @(); Marker = $null }))
    }
  }
  finally { Remove-Item -LiteralPath $csv -Force -ErrorAction SilentlyContinue }
  $names = @($result.Output | ForEach-Object Path) -join ', '
  if ($names -ne 'winspool.drv') { Fail "The control on short names planted '$names', where it should plant winspool.drv, looked for under $short." }
  else { Write-Host "  planted for the look under ${short}: $names" }
  Assert-Caught $result.Caught @("Process Monitor recorded $missing, which could not be written in full") 'short names'
}

# Runs Read-RecordWithReplaces over a record made up for the purpose, in which process IDs
# are handed on, stand-ins carry the builds' exe names, and every process has rows after
# its Process Exit row, as Windows closes its handles. Each run must be given the rows of
# its own lifetime, those after its exit included, and no others: a run whose ID a
# stand-in had before it, a replace whose ID a stand-in had, a replace and a run given one
# ID a lifetime apart, and the copy a replace starts at the path, whose ID and image the
# stand-in there had. A replace must follow the run that started it, a stand-in of the
# staged exe's name beside it must not, and so must a process of a run's exe name that is no
# run's, with no Process Start or no load of its image. A process another started must be given the
# lifetime begun after the row naming it, not an earlier or a later one of its ID and
# name. Two runs that could take one lifetime, two lifetimes of one run, a run's process
# with no Process Start, one with no load of its image, and two replaces of one run must
# each fail once, and the runs they leave unjudged must be left out.
function Test-LifetimeControls {
  Write-Host "== Control: whose lifetime each row in a record is"
  $command = [pscustomobject]@{ Replace = 'command' }
  $window = [pscustomobject]@{ Replace = 'replaced' }
  $other = [pscustomobject]@{ Replace = $null }
  function New-ControlRun([string]$label, [int]$id, $build, [string]$folder, [string]$exe) {
    [pscustomobject]@{ Label = $label; Build = $build; Folder = $folder; Exe = $exe; Id = $id }
  }
  $winRun = New-ControlRun 'window' 1100 $window 'C:\stage11' 'win.exe'
  $copy = New-ControlRun 'window, the copy' 1101 $other 'C:\target11' 'app.exe'
  $copy | Add-Member -NotePropertyName StartedBy -NotePropertyValue $winRun
  $runs = @(
    New-ControlRun 'read' 100 $command 'C:\stage1' 'staged.exe'
    New-ControlRun 'reused' 200 $other 'C:\run2' 'other.exe'
    New-ControlRun 'reuse replace' 300 $command 'C:\stage3' 'staged.exe'
    New-ControlRun 'handed on' 400 $command 'C:\stage4' 'staged.exe'
    New-ControlRun 'given its ID' 600 $other 'C:\run6' 'given.exe'
    New-ControlRun 'two lifetimes' 500 $other 'C:\run5' 'twice.exe'
    New-ControlRun 'no start' 700 $other 'C:\run7' 'nostart.exe'
    New-ControlRun 'no image' 800 $other 'C:\run8' 'noimage.exe'
    New-ControlRun 'twice' 1000 $command 'C:\stage10' 'staged.exe'
    $winRun
    $copy
    New-ControlRun 'one of two' 1200 $other 'C:\run12' 'same.exe'
    New-ControlRun 'other of two' 1200 $other 'C:\run12' 'same.exe')
  function Get-Lifetime([int]$id, [string]$name, [string]$image, [string[]]$looks, [switch]$Open) {
    @(, @($id, $name, 'Process Start', ''))
    @(, @($id, $name, 'Thread Create', ''))
    if ($image) { @(, @($id, $name, 'Load Image', $image)) }
    foreach ($look in $looks) { @(, @($id, $name, 'QueryOpen', $look)) }
    if (-not $Open) { Get-End $id $name $(if ($image) { Split-Path $image } else { 'C:\nowhere' }) }
  }
  # The Process Exit row, and the handle closed after it.
  function Get-End([int]$id, [string]$name, [string]$folder) {
    @(, @($id, $name, 'Process Exit', ''))
    @(, @($id, $name, 'CloseFile', "$folder\closed-after-exit.txt"))
  }
  $rows = @(
    # A stand-in had 200 and 301 before the runs that are given them.
    Get-Lifetime 200 'other.exe' 'C:\run2-target\other.exe' @('C:\run2-target\stand-in.dll')
    Get-Lifetime 301 'staged.exe' 'C:\stage3-target\staged.exe' @('C:\stage3-target\stand-in.dll')
    # And the stand-in at the window's path had the ID its copy is given, before the window started.
    Get-Lifetime 1101 'app.exe' 'C:\target11\app.exe' @('C:\target11\stand-in.dll')
    Get-Lifetime 200 'other.exe' 'C:\run2\other.exe' @('C:\run2\own.dll')
    Get-Lifetime 100 'staged.exe' 'C:\stage1\staged.exe' @() -Open
    Get-Lifetime 102 'staged.exe' 'C:\stage1-target\staged.exe' @('C:\stage1-target\stand-in.dll')
    Get-Lifetime 101 'staged.exe' 'C:\stage1\staged.exe' @('C:\stage1\helper.dll')
    Get-End 100 'staged.exe' 'C:\stage1'
    Get-Lifetime 300 'staged.exe' 'C:\stage3\staged.exe' @() -Open
    Get-Lifetime 301 'staged.exe' 'C:\stage3\staged.exe' @('C:\stage3\helper.dll')
    Get-End 300 'staged.exe' 'C:\stage3'
    # The replace of 400 ends, and a later run is given its ID.
    Get-Lifetime 400 'staged.exe' 'C:\stage4\staged.exe' @() -Open
    Get-Lifetime 600 'staged.exe' 'C:\stage4\staged.exe' @('C:\stage4\helper.dll')
    Get-End 400 'staged.exe' 'C:\stage4'
    Get-Lifetime 600 'given.exe' 'C:\run6\given.exe' @('C:\run6\own.dll')
    Get-Lifetime 500 'twice.exe' 'C:\run5\twice.exe' @()
    Get-Lifetime 500 'twice.exe' 'C:\run5\twice.exe' @()
    @(, @(700, 'nostart.exe', 'QueryOpen', 'C:\run7\helper.dll'))
    Get-Lifetime 800 'noimage.exe' '' @('C:\run8\helper.dll')
    # A process of a run's exe name that is no run's, started before the record and with no image.
    @(, @(701, 'staged.exe', 'QueryOpen', 'C:\nowhere\helper.dll'))
    Get-Lifetime 801 'staged.exe' '' @('C:\nowhere\helper.dll')
    Get-Lifetime 1000 'staged.exe' 'C:\stage10\staged.exe' @() -Open
    Get-Lifetime 1001 'staged.exe' 'C:\stage10\staged.exe' @()
    Get-Lifetime 1002 'staged.exe' 'C:\stage10\staged.exe' @()
    Get-End 1000 'staged.exe' 'C:\stage10'
    Get-Lifetime 1100 'win.exe' 'C:\stage11\win.exe' @() -Open
    Get-Lifetime 1101 'app.exe' 'C:\target11\app.exe' @('C:\target11\own.dll')
    Get-End 1100 'win.exe' 'C:\stage11'
    Get-Lifetime 1200 'same.exe' 'C:\run12\same.exe' @()
    # A process another started, whose ID an earlier process of its name had.
    Get-Lifetime 1300 'child.tmp' 'C:\old\child.tmp' @('C:\old\stale.dll')
    Get-Lifetime 1300 'child.tmp' 'C:\new\child.tmp' @('C:\new\own.dll')
    # And a later process another started, given the same ID.
    Get-Lifetime 1300 'child.tmp' 'C:\newer\child.tmp' @('C:\newer\own.dll'))
  $starts = @(for ($i = 0; $i -lt $rows.Count; $i++) { if ($rows[$i][0] -eq 1300 -and $rows[$i][2] -eq 'Process Start') { $i } })
  foreach ($child in @(@('child', 1), @('child, later', 2))) {
    $runs += [pscustomobject]@{ Label = $child[0]; Build = $other; Folder = 'C:\nowhere'; Exe = 'child.tmp'; Id = 1300; ByProcessId = $true; After = $starts[$child[1]] }
  }
  $csv = Join-Path ([IO.Path]::GetTempPath()) ('record-lifetime-control-' + [guid]::NewGuid().ToString('N') + '.csv')
  try {
    $rows | ForEach-Object {
      [pscustomobject]@{ 'Time of Day' = '12:00:00.0000000 PM'; 'Process Name' = $_[1]; PID = [string]$_[0]; Operation = $_[2]; Path = $_[3]; Result = 'SUCCESS'; Detail = '' }
    } | Export-Csv -Path $csv -NoTypeInformation
    $result = Invoke-Caught { Read-RecordWithReplaces $csv $runs }
  }
  finally { Remove-Item -LiteralPath $csv -Force -ErrorAction SilentlyContinue }
  $read = @($result.Output | Where-Object { $_ -is [pscustomobject] -and $_.PSObject.Properties['ByProcess'] })
  if ($read.Count -ne 1) { Fail "The control on whose lifetime each row is answered $($read.Count) readings, where it answers one."; return }
  $labels = @($read[0].Runs | ForEach-Object Label) -join ', '
  $expected = (@('read', (Get-StartedReplaceLabel 'read'), 'reused', 'reuse replace', (Get-StartedReplaceLabel 'reuse replace'), 'handed on',
    (Get-StartedReplaceLabel 'handed on'), 'given its ID', 'no start', 'no image', 'twice', (Get-StartedReplaceLabel 'twice'),
    (Get-StartedReplaceLabel 'twice'), 'window', 'window, the copy', 'child', 'child, later') -join ', ')
  if ($labels -ne $expected) { Fail "The control on whose lifetime each row is answered the runs '$labels', where it answers '$expected'." }
  else { Write-Host "  runs: $labels" }
  # Each run's own looks and the handle it closed after its exit, and none of a stand-in's.
  foreach ($check in @(@('read', 'C:\stage1'), @((Get-StartedReplaceLabel 'read'), 'C:\stage1\helper.dll'), @('reused', 'C:\run2\own.dll'),
      @((Get-StartedReplaceLabel 'reuse replace'), 'C:\stage3\helper.dll'), @((Get-StartedReplaceLabel 'handed on'), 'C:\stage4\helper.dll'),
      @('given its ID', 'C:\run6\own.dll'), @('window, the copy', 'C:\target11\own.dll'), @('child', 'C:\new\own.dll'), @('child, later', 'C:\newer\own.dll'))) {
    $run = @($read[0].Runs | Where-Object Label -eq $check[0])
    if ($run.Count -ne 1) { Fail "The control on whose lifetime each row is answered $($run.Count) runs labelled $($check[0]), where it answers one."; continue }
    $rowsOf = @($read[0].ByProcess[(Get-RunKey $run[0])])
    $looks = @($rowsOf | Where-Object Operation -eq 'QueryOpen' | ForEach-Object Path) -join ', '
    $want = if ($check[1].EndsWith('.dll')) { $check[1] } else { '' }
    $closed = @($rowsOf | Where-Object Operation -eq 'CloseFile' | ForEach-Object Path) -join ', '
    $wantClosed = "$(if ($want) { Split-Path $want } else { $check[1] })\closed-after-exit.txt"
    if ($looks -ne $want -or $closed -ne $wantClosed) {
      Fail "The control on whose lifetime each row is gave $($check[0]) the looks '$looks' and the closes '$closed', where it has '$want' and '$wantClosed'."
    }
    else { Write-Host "  $($check[0]) looked for '$looks' and closed $closed after its exit" }
  }
  $stray = @($read[0].ByProcess.Keys | Where-Object { ($_ -split '\|')[0] -in '500', '700', '800', '701', '801', '102', '1200' })
  if ($stray) { Fail "The control on whose lifetime each row is read rows under $($stray -join ', '), which is judged as no run." }
  Assert-Caught $result.Caught @(
    'Process Monitor recorded process 1200 from row',
    'Process Monitor recorded two lifetimes of process 500, from rows',
    'Process Monitor recorded nostart.exe, process 700, from row',
    'Process Monitor recorded no load of the image of noimage.exe, process 800',
    'twice started 2 copies of staged.exe') 'whose lifetime each row is'
}

# ---- Replace mode ----
#
# A newer version an update downloads is started from a staging folder to put itself at
# the old copy's path once that copy has ended (ExeReplacement). Each build is started
# that way too: from a folder made as UpdateStagingFolder makes one, straight under
# %ProgramData%, under the name AppCopy.DownloadFileName gives its download, with the three
# switches HandOverContract reads, against a stand-in old copy, a copy of Windows
# PowerShell named as the build's exe and started from the path, so the replace finds
# the process runs from there. The stand-in sleeps until the build sets the started
# event and is then ended, as an old copy closes once it is told. A run with nothing
# left at the path renames the stand-in's exe aside before ending it.
#
# A window's start clears the staging folders it finds of their InstallerClean-*.exe files
# and removes those left empty (UpdateStagingFolder.RemoveLeftovers), so a window started
# later in a pass can take an earlier run's staged exe. The files planted beside it stay,
# and they are what is judged.

# The source file $path, read once a step: each step runs this file in a process of its own.
$script:SourceTexts = @{}
function Get-SourceText([string]$path) {
  if (-not $script:SourceTexts.ContainsKey($path)) { $script:SourceTexts[$path] = Get-Content -Raw (Join-Path $repository $path) }
  $script:SourceTexts[$path]
}

# A string constant $name in the source file $path, read as written, once a step.
$script:SourceStrings = @{}
function Read-SourceString([string]$path, [string]$name) {
  Read-SourceMatch $path "\b$name\s*=\s*@?""([^""]*)"";" "$path has no string $name to read."
}

# A new started event, named as HandOverContract names one, and the argument that hands it
# to a build: Handle and Argument.
function New-StartedEvent {
  $id = [guid]::NewGuid().ToString('N')
  $created = $false
  $handle = [System.Threading.EventWaitHandle]::new($false, 'ManualReset', "$(Get-StartedEventPrefix)$id", [ref]$created)
  if (-not $created) { $handle.Dispose(); throw "An event named for $id exists already." }
  [pscustomobject]@{ Handle = $handle; Argument = "$(Read-SourceString 'src/InstallerClean.Core/Helpers/HandOverContract.cs' 'StartedEventSwitch')$id" }
}

# An event the run is told to watch for Ctrl+C, named as CliContract.CancelEventName
# names one, with the argument that passes it. It is never set.
function New-CancelEvent {
  $id = [guid]::NewGuid().ToString('N')
  $prefix = Read-SourceMatch 'src/InstallerClean.Core/Helpers/CliContract.cs' 'CancelEventName\(Guid id\)\s*=>\s*@?"([^"]+)"\s*\+' 'CliContract.cs names no cancel event.'
  $created = $false
  $handle = [System.Threading.EventWaitHandle]::new($false, 'ManualReset', "$prefix$id", [ref]$created)
  if (-not $created) { $handle.Dispose(); throw "An event named for $id exists already." }
  [pscustomobject]@{ Handle = $handle; Argument = "$(Read-SourceString 'src/InstallerClean.Core/Helpers/CliContract.cs' 'CancelEventSwitch')$id" }
}

# The first group $pattern matches in the source file $path, read once a step; where it
# matches nothing, the step stops with $missing.
function Read-SourceMatch([string]$path, [string]$pattern, [string]$missing) {
  $key = "$path|$pattern"
  if (-not $script:SourceStrings.ContainsKey($key)) {
    if ((Get-SourceText $path) -notmatch $pattern) { throw $missing }
    $script:SourceStrings[$key] = $Matches[1]
  }
  $script:SourceStrings[$key]
}

# What HandOverContract names a started event with, before its id.
function Get-StartedEventPrefix {
  Read-SourceMatch 'src/InstallerClean.Core/Helpers/HandOverContract.cs' 'StartedEventName\(Guid id\)\s*=>\s*@?"([^"]+)"\s*\+' "HandOverContract.cs names no started event to read."
}

# The Application-log event ID CliContract gives the notice that an update was not
# installed because nothing was left at the path.
function Get-NothingAtPathEventId {
  [int](Read-SourceMatch 'src/InstallerClean.Core/Helpers/CliContract.cs' 'CliEventClass\.UpdateNotInstalledNotice\s*=>\s*(\d+)' "CliContract.cs gives UpdateNotInstalledNotice no event ID to read.")
}

# Creates the folder $path with the descriptor $sddl in the one call, as
# UpdateStagingFolder does.
function New-FolderWithDescriptor([string]$path, [string]$sddl) {
  $security = [System.Security.AccessControl.DirectorySecurity]::new()
  $security.SetSecurityDescriptorSddlForm($sddl)
  [System.IO.FileSystemAclExtensions]::Create([System.IO.DirectoryInfo]::new($path), $security)
}

# A new staging folder, named and made as UpdateStagingFolder makes one, straight under
# %ProgramData%.
function New-StagingFolder {
  $sddl = Read-SourceString 'src/InstallerClean.Core/Services/UpdateStagingFolder.cs' 'AdministratorsOnly'
  $prefix = Read-SourceString 'src/InstallerClean.Core/Services/UpdateStagingFolder.cs' 'NamePrefix'
  $path = Join-Path $env:ProgramData ($prefix + [guid]::NewGuid().ToString('N'))
  $null = New-FolderWithDescriptor $path $sddl
  $path
}

# The name AppCopy.DownloadFileName gives the download of the build $label names, which is
# the name a newer version runs under from its staging folder.
function Get-StagedName([string]$label) {
  $source = Get-SourceText 'src/InstallerClean.Core/Services/AppCopy.cs'
  $kind = switch ($label) { 'command line' { 'CommandLineDownload' } 'window' { 'Portable' } default { throw "No download is named for the build $label." } }
  if ($source -notmatch "AppCopyKind\.$kind\s*=>\s*(""([^""]+)""|(\w+))\s*,") { throw "AppCopy.cs names no download for $kind." }
  if ($Matches[2]) { return $Matches[2] }
  Read-SourceString 'src/InstallerClean.Core/Services/AppCopy.cs' $Matches[3]
}

# A folder's or file's access list as SDDL, without its owner or group.
function Get-AccessSddl([string]$path) { (Get-Acl -LiteralPath $path).GetSecurityDescriptorSddlForm('Access') }

# The names a replace leaves beside the exe while it runs (ExeReplacement).
function Get-ReplaceSuffixes {
  $file = 'src/InstallerClean.Core/Services/ExeReplacement.cs'
  @('ReplacingSuffix', 'BackupSuffix', 'LockSuffix' | ForEach-Object { Read-SourceString $file $_ })
}

# What every staging folder's full path starts with: %ProgramData% and
# UpdateStagingFolder.NamePrefix.
function Get-StagingPrefix {
  Join-Path $env:ProgramData (Read-SourceString 'src/InstallerClean.Core/Services/UpdateStagingFolder.cs' 'NamePrefix')
}

# Waits up to $seconds for every process started since $since running from a staging
# folder, or with -SetupTemp from an is-*.tmp folder (Get-IsTmpFolder) straight inside
# a folder Get-TempFolders names, the folders Start-TempWatch watches, to end. Each is
# taken in every spelling it is given and in its long and short spelling where Windows
# gives them, since a process's path can be in any. A process whose start time cannot
# be read is counted, since it may be one the run started. A second's margin is taken off
# $since, since a process's start time is read off a coarser clock than Get-Date's.
# Answers those still running then as "<id> <path>", and ends none: nothing here can
# show that a process is one the run started rather than another given its ID.
function Wait-StagingProcesses([int]$seconds, [datetime]$since, [switch]$SetupTemp) {
  $prefix = Get-StagingPrefix
  $temps = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
  if ($SetupTemp) {
    foreach ($folder in @(Get-TempFolders) + @($env:TEMP, $env:TMP, [IO.Path]::GetTempPath()) | Where-Object { $_ }) {
      foreach ($spelling in $folder, (Get-LongName $folder), (Get-ShortName $folder)) {
        if ($spelling) { $null = $temps.Add($spelling.TrimEnd('\')) }
      }
    }
  }
  $from = $since.AddSeconds(-1)
  $clock = [System.Diagnostics.Stopwatch]::StartNew()
  while ($true) {
    $running = @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
      $path = $_.Path
      if (-not $path) { return $false }
      $isTmp = if ($temps.Count) { Get-IsTmpFolder $path }
      ($path.StartsWith($prefix, 'OrdinalIgnoreCase') -or ($isTmp -and $temps.Contains((Split-Path $isTmp)))) -and
        $(try { $_.StartTime -ge $from } catch { $true }) })
    if (-not $running) { return , @() }
    if ($clock.Elapsed.TotalSeconds -ge $seconds) { break }
    Start-Sleep -Milliseconds 500
  }
  , @($running | ForEach-Object { "$($_.Id) $($_.Path)" })
}

# The Application-log entries from InstallerClean's source (EventLogWriter.SourceName)
# written from $since to $until, those with the event ID $id alone where one is given, and
# why the log could not be read where it could not. Get-WinEvent takes its StartTime to the
# whole second, so the entries are then kept by the time each was written, and a run just
# before cannot answer for this one.
function Read-ApplicationLog([datetime]$since, [datetime]$until, $id = $null) {
  $filter = @{ LogName = 'Application'; ProviderName = (Read-SourceString 'src/InstallerClean.Core/Helpers/EventLogWriter.cs' 'SourceName'); StartTime = $since }
  if ($null -ne $id) { $filter.Id = $id }
  # No entry matching, and a source never registered or writing to another log, each
  # come back as an error that means nothing was written here.
  $noEntry = 'NoMatchingEventsFound', 'NoMatchingProvidersFound', 'LogsAndProvidersDontOverlap'
  $readErrors = $null
  try { $logged = @(Get-WinEvent -FilterHashtable $filter -ErrorAction SilentlyContinue -ErrorVariable readErrors) }
  catch { return [pscustomobject]@{ Entries = @(); Unread = @($_.Exception.Message) } }
  [pscustomobject]@{
    Entries = @($logged | Where-Object { $_.TimeCreated -ge $since -and $_.TimeCreated -le $until })
    Unread = @($readErrors | Where-Object { ($_.FullyQualifiedErrorId -split ',')[0] -notin $noEntry } | ForEach-Object { $_.Exception.Message })
  }
}

# The nothing-at-path entries written from $since to five seconds after $until, read once
# those five seconds have passed, so an entry written as the run ends, and a second one,
# are each counted, whether one is expected or not.
function Read-NothingAtPathEntries([datetime]$since, [datetime]$until) {
  $last = $until.AddSeconds(5)
  $wait = $last - (Get-Date)
  if ($wait -gt [TimeSpan]::Zero) { Start-Sleep -Milliseconds ([int][math]::Ceiling($wait.TotalMilliseconds)) }
  Read-ApplicationLog $since $last (Get-NothingAtPathEventId)
}

# Judges how a replace run ended, from $end: the run's label and mode ('replaced',
# 'nothing', or 'command' for a run of /s standing for the copy at the path, which then
# starts the replace itself), the build's exe, the target path and folder, the staging folder's access
# list as the folder was made, whether the started event was set, the replacer's exit
# code, whether a process is still running from the target path or the staging folder, the
# times it began and ended, and $readEntries, which takes those two times and answers the
# nothing-at-path entries written around the run (Entries) and why the log could not be
# read where it could not (Unread). Fails where: the Application log could not be read;
# the event was not set; the exit code is not 0 for a replace or a command and 1 for nothing
# at the path; the file at the path is not the build's exe, or is there at all where nothing was
# left at the path; a name a replace leaves beside the exe is there, or the target folder
# holds anything but the exe, the control file and the stand-in's exe set aside; for a
# replace or a command, the staging and target folders' access lists are the same, so the next check
# could not tell them apart, or the exe's is not the control file's; the entry saying the
# update was not installed is missing, more than one, or without the releases page where
# nothing was left, or there at all after a replace or a command; or a process is still running.
function Test-ReplaceEnd($end) {
  $label = $end.Label
  if (-not $end.EventSet) { Fail "$label did not set the started event within 60 s." }
  $putInPlace = $end.Mode -in 'replaced', 'command'
  $expected = if ($putInPlace) { 0 } else { 1 }
  if ($end.ExitCode -ne $expected) { Fail "$label ended with exit code $($end.ExitCode), where it should end with $expected." }
  if ($putInPlace) {
    if (-not (Test-Path -LiteralPath $end.Target)) { Fail "$label left nothing at $($end.Target)." }
    elseif ((Get-FileHash -Algorithm SHA256 -LiteralPath $end.Target).Hash -ne (Get-FileHash -Algorithm SHA256 -LiteralPath $end.BuildExe).Hash) {
      Fail "$label left a file at $($end.Target) that is not the build's exe."
    }
  }
  elseif (Test-Path -LiteralPath $end.Target) { Fail "$label, with nothing left at the path, has a file at $($end.Target)." }
  $suffixes = @(Get-ReplaceSuffixes)
  foreach ($suffix in $suffixes) {
    if (Test-Path -LiteralPath "$($end.Target)$suffix") { Fail "$label left $($end.Target)$suffix beside the exe." }
  }
  # The names a replace leaves are judged above, so each fault is named once.
  $exeName = Split-Path $end.Target -Leaf
  $allowed = @($exeName, 'control.txt', "$exeName.aside") + @($suffixes | ForEach-Object { "$exeName$_" })
  foreach ($stray in @(Get-ChildItem -LiteralPath $end.TargetFolder -Force | Where-Object Name -notin $allowed)) {
    Fail "$label left $($stray.FullName) in the folder it was put in."
  }
  if ($putInPlace -and (Test-Path -LiteralPath $end.Target)) {
    $staging = $end.StagingAccess
    $folder = Get-AccessSddl $end.TargetFolder
    Write-Host "  staging folder's access list: $staging"
    Write-Host "  target folder's access list: $folder"
    if ($staging -eq $folder) { Fail "$label's staging and target folders have the same access list, so where the exe's came from could not be told." }
    else {
      $exe = Get-AccessSddl $end.Target
      $control = Get-AccessSddl (Join-Path $end.TargetFolder 'control.txt')
      if ($exe -ne $control) { Fail "$label left the exe at the path with the access list $exe, where a file made in that folder has $control." }
      else { Write-Host "  the exe at the path carries the folder's inherited access list: $exe" }
    }
  }
  $read = & $end.ReadEntries $end.Started $end.Ended
  $entries = @($read.Entries)
  $releases = Read-SourceString 'src/InstallerClean.Core/Services/UpdateCheckService.cs' 'ReleasesPageUrl'
  if (@($read.Unread)) {
    foreach ($message in @($read.Unread)) { Fail "$label`: the Application log could not be read, so what it wrote there was not judged: $message" }
  }
  elseif ($end.Mode -eq 'nothing') {
    if ($entries.Count -ne 1) { Fail "$label, with nothing left at the path, wrote $($entries.Count) entries saying the update was not installed, where it should write one." }
    elseif (-not $entries[0].Message.Contains($releases)) { Fail "$label's entry saying the update was not installed does not name $releases`: $($entries[0].Message)" }
    else { Write-Host "  wrote entry $($entries[0].Id): $($entries[0].Message)" }
  }
  elseif ($entries.Count) { Fail "$label put the exe in place and wrote $($entries.Count) entries saying the update was not installed." }
  if ($end.StillRunning) { Fail "$label left a process running from $($end.StillRunning)." }
}

# The run of the replace $parent started as process $id (Read-Record), judged from the
# staging folder and working there (StagedRun.StartReplace), against the plants planned for
# it and against $parent's as files beside it too.
function New-StartedReplace($parent, [int]$id) {
  [pscustomobject]@{
    Label = (Get-StartedReplaceLabel $parent.Label); Build = $parent.Build; Folder = $parent.Folder; Exe = $parent.Exe; Id = $id
    Started = $null; Ended = $null; Placed = $parent.ReplacePlaced; SharedPlaced = $parent.Placed; Target = $parent.Target
    WorkingFolder = ''; StartedReplace = $true; StartedBy = $parent
  }
}

# Whether $run is one that starts a replace once its command has ended: a run of a build
# whose Replace is 'command', other than the replace such a run started.
function Test-StartsReplace($run) { $run.Build.Replace -eq 'command' -and -not $run.PSObject.Properties['StartedReplace'] }

# Reads the record $csv for $runs, and answers the record and the runs with, after each run
# that starts a replace, the replace it started (Read-Record). A run Read-Record judged on
# none of its rows is left out, the step having failed for it, and so is a replace of one.
# Fails where a run that starts a replace started other than one, unless it is judged on
# its loads alone and started none, which it says, so a replace never judged is never
# silent.
function Read-RecordWithReplaces([string]$csv, $runs) {
  $started = [System.Collections.Generic.List[object]]::new()
  $dropped = [System.Collections.Generic.HashSet[string]]::new()
  $byProcess = Read-Record $csv $runs $started $dropped
  $all = foreach ($run in $runs) {
    if ($dropped.Contains((Get-RunKey $run))) { continue }
    $run
    if (-not (Test-StartsReplace $run)) { continue }
    $replaces = @($started | Where-Object { $_.StartedBy -eq $run })
    foreach ($replace in @($replaces | Where-Object { -not $dropped.Contains((Get-RunKey $_)) })) {
      Write-Host "  $($replace.Label): process $($replace.Id), started by process $($run.Id)"
      $replace
    }
    if ($replaces.Count -ne 1) { Fail "$($run.Label) started $($replaces.Count) copies of $($run.Exe), where it starts one replace." }
  }
  [pscustomobject]@{ ByProcess = $byProcess; Runs = @($all) }
}

# Starts $build in replace mode from a new staging folder holding $plants, for the pass
# $pass, and answers the replacer's run and, with -JudgeCopy, for a window build that put
# itself in place, the run of the copy it started at the path. With -JudgeCopy that copy
# runs for the build's own Wait before it is ended; without it, it is ended at once.
# Each run's end is judged by Test-ReplaceEnd once every process has ended.
# A build whose Replace is 'command' is started with /s /noupdate beside the three
# switches, and the Ctrl+C event's switch naming an event made here, as the older copy
# starts it, working in a folder of its own outside both the staging folder and the
# target's, as a scheduled task can start the older copy, and its run carries that folder
# as WorkingFolder. It runs the scan standing for the copy at the path, so the stand-in for
# that copy is left running until it has ended, and the replace it then starts, detached
# and from the staging folder, is waited for until the stand-in has ended and the replace
# with it. That replace is judged as a run of its own once the record is read
# (Read-RecordWithReplaces), against $replacePlants, which are put in the staging folder
# beside $plants; the run carries both lists.
function Invoke-Replace($build, [string]$label, [string]$pass, $plants, [switch]$JudgeCopy, $replacePlants = @()) {
  $staging = New-StagingFolder
  # Read now: a window started at the path deletes the staged exe as it starts, and the
  # folder with it where nothing else is in it.
  $stagingAccess = Get-AccessSddl $staging
  $exeName = Split-Path $build.Exe -Leaf
  $stagedName = Get-StagedName $build.AtPath.Label
  Copy-Item $build.Exe (Join-Path $staging $stagedName)
  $command = $build.Replace -eq 'command'
  $workingFolder = $staging
  if ($command) {
    $workingFolder = Get-RunFolder $pass "$label working folder"
    if (Test-Path -LiteralPath $workingFolder) { Remove-Item -LiteralPath $workingFolder -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $workingFolder | Out-Null
    Write-Host "  ${label}: working in $workingFolder"
  }
  # A path both runs look at is planted once, as the run's, so neither copy is written
  # over the other while either runs.
  $runKeys = @($plants | Where-Object { $_ } | ForEach-Object { "$($_.Place)|$($_.Path)" })
  $replaceKeys = @($replacePlants | Where-Object { $_ } | ForEach-Object { "$($_.Place)|$($_.Path)" })
  $placedAll = @(Add-Plants $staging (@($plants) + @($replacePlants | Where-Object { $_ -and "$($_.Place)|$($_.Path)" -notin $runKeys })) $workingFolder)
  $placed = @($placedAll | Where-Object Key -in $runKeys)
  $replacePlaced = @($placedAll | Where-Object Key -in $replaceKeys)
  $targetFolder = Get-RunFolder $pass "$label target"
  if (Test-Path -LiteralPath $targetFolder) { Remove-Item -LiteralPath $targetFolder -Recurse -Force }
  New-Item -ItemType Directory -Force -Path $targetFolder | Out-Null
  $target = Join-Path $targetFolder $exeName
  Copy-Item (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe') $target
  Set-Content -LiteralPath (Join-Path $targetFolder 'control.txt') -Value 'A file made in the target folder, for its access list.'

  $standIn = Start-Process -FilePath $target -ArgumentList '-NoProfile', '-NonInteractive', '-Command', 'Start-Sleep -Seconds 600' -WindowStyle Hidden -PassThru
  $null = $standIn.Handle
  $signal = New-StartedEvent
  $event = $signal.Handle
  $contract = 'src/InstallerClean.Core/Helpers/HandOverContract.cs'
  $cancel = $null
  if ($command) { $cancel = New-CancelEvent }
  $arguments = @(if ($command) { '/s', '/noupdate', $cancel.Argument }) + @(
    "`"$(Read-SourceString $contract 'ReplaceCopySwitch')$target`"",
    "$(Read-SourceString $contract 'AfterProcessSwitch')$($standIn.Id)",
    $signal.Argument)
  $begun = Get-Date
  $replacer = Start-Process -FilePath (Join-Path $staging $stagedName) -ArgumentList $arguments -WorkingDirectory $workingFolder -PassThru
  $null = $replacer.Handle
  $eventSet = $event.WaitOne([TimeSpan]::FromSeconds(60))
  $event.Dispose()
  if ($build.Replace -eq 'nothing') { Rename-Item -LiteralPath $target -NewName "$exeName.aside" }
  if (-not $command) {
    Stop-Process -Id $standIn.Id -Force -ErrorAction SilentlyContinue
    $standIn.WaitForExit()
  }
  if (-not $replacer.WaitForExit($build.Wait * 1000)) {
    Fail "$label was still running $($build.Wait) s after it was started."
    Stop-Process -Id $replacer.Id -Force -ErrorAction SilentlyContinue
    $replacer.WaitForExit()
  }
  if ($command) {
    # The replace the run started waits for the copy it stood for, which is ended now.
    $replaces = @(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path.StartsWith("$staging", 'OrdinalIgnoreCase') })
    Write-Host "  ${label}: $($replaces.Count) replace process(es) running from the staging folder as the run ended: $(($replaces | ForEach-Object Id) -join ', ')"
    Stop-Process -Id $standIn.Id -Force -ErrorAction SilentlyContinue
    $standIn.WaitForExit()
    foreach ($replace in $replaces) {
      if (-not $replace.WaitForExit(120000)) { Fail "$label's replace, process $($replace.Id), was still running 120 s after the copy it stood for ended." }
    }
  }
  if ($cancel) { $cancel.Handle.Dispose() }
  $ended = Get-Date
  Write-Host "  ${label}: process $($replacer.Id), ended with $($replacer.ExitCode)"

  $runs = @([pscustomobject]@{
    Label = $label; Build = $build; Folder = $staging; Exe = $stagedName; Id = $replacer.Id; Started = $begun; Ended = $ended
    Placed = $placed; Target = $target; WorkingFolder = $(if ($command) { $workingFolder } else { '' })
    ReplacePlaced = $replacePlaced; SharedPlaced = $replacePlaced
  })
  $copies = @(Get-Process -Name ([IO.Path]::GetFileNameWithoutExtension($exeName)) -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $target })
  if ($build.Replace -eq 'replaced' -and $build.StartsAtPath) {
    if ($copies.Count -ne 1) { Fail "$label put itself in place and $($copies.Count) copies were running from $target, where it starts one." }
    if ($JudgeCopy -and $copies) {
      # The copy gets as far as the build's own start does before it is judged as one.
      Start-Sleep -Seconds $build.AtPath.Wait
      foreach ($copy in $copies) {
        Write-Host "  ${label}: the copy started at the path is process $($copy.Id)"
        $runs += [pscustomobject]@{
          Label = "$label, the copy started at the path"; Build = $build.AtPath; Folder = $targetFolder; Exe = $exeName; Id = $copy.Id
          Started = $begun; Ended = $null; StartedCopy = $true; StartedBy = $runs[0]
        }
      }
    }
  }
  # The replacer ends only once the copy it started has set its started event, so a copy
  # ended here has taken the mutex, read its settings and built its view model.
  foreach ($copy in $copies) { Stop-Process -Id $copy.Id -Force -ErrorAction SilentlyContinue; $copy.WaitForExit() }
  $copiesEnded = Get-Date
  foreach ($run in @($runs | Where-Object StartedCopy)) { $run.Ended = $copiesEnded }
  if (Test-Path -LiteralPath "$target.aside") { Remove-Item -LiteralPath "$target.aside" -Force -ErrorAction SilentlyContinue }

  $left = @(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.Path -and ($_.Path -eq $target -or $_.Path.StartsWith("$staging\", 'OrdinalIgnoreCase')) })
  foreach ($process in $left) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue }
  Test-ReplaceEnd ([pscustomobject]@{
    Label = $label; Mode = $build.Replace; BuildExe = $build.Exe; Target = $target; TargetFolder = $targetFolder; StagingAccess = $stagingAccess
    EventSet = $eventSet; ExitCode = $replacer.ExitCode; Started = $begun; Ended = $ended; ReadEntries = ${function:Read-NothingAtPathEntries}
    StillRunning = $(if ($left) { $left[0].Path })
  })
  $runs
}

# Runs Test-ReplaceEnd over ends made up for the purpose, each with a fault planted, and
# one with none, and fails the step unless each fails exactly where it must. Also runs
# Get-Marker over rows made up for a replace and fails unless it answers the replaced
# copy at the first row on the target.
function Test-ReplaceControls {
  Write-Host "== Control: the checks on how a replace ended"
  $root = Join-Path ([IO.Path]::GetTempPath()) ('replace-controls-' + [guid]::NewGuid().ToString('N'))
  try {
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    $build = Join-Path $root 'build.exe'
    Set-Content -LiteralPath $build -Value 'the build'
    $sddl = Read-SourceString 'src/InstallerClean.Core/Services/UpdateStagingFolder.cs' 'AdministratorsOnly'
    $staging = Join-Path $root 'staging'
    $null = New-FolderWithDescriptor $staging $sddl
    $releases = Read-SourceString 'src/InstallerClean.Core/Services/UpdateCheckService.cs' 'ReleasesPageUrl'
    $id = Get-NothingAtPathEventId
    $none = { param($since, $until) [pscustomobject]@{ Entries = @(); Unread = @() } }
    $two = { param($since, $until) [pscustomobject]@{ Entries = @([pscustomobject]@{ Id = $id; Message = 'one' }, [pscustomobject]@{ Id = $id; Message = 'two' }); Unread = @() } }
    $unreadable = { param($since, $until) [pscustomobject]@{ Entries = @(); Unread = @('the control cannot read it') } }

    function New-Target([string]$name, [string]$content) {
      $folder = Join-Path $root $name
      New-Item -ItemType Directory -Force -Path $folder | Out-Null
      Set-Content -LiteralPath (Join-Path $folder 'control.txt') -Value 'control'
      if ($content) { Set-Content -LiteralPath (Join-Path $folder 'InstallerClean.exe') -Value $content }
      $folder
    }
    function New-End($folder, $mode, $read, [string]$stagingFolder = $staging) {
      [pscustomobject]@{
        Label = "control $(Split-Path $folder -Leaf)"; Mode = $mode; BuildExe = $build; Target = (Join-Path $folder 'InstallerClean.exe'); TargetFolder = $folder
        StagingAccess = (Get-AccessSddl $stagingFolder); EventSet = $true; ExitCode = $(if ($mode -in 'replaced', 'command') { 0 } else { 1 }); Started = (Get-Date); Ended = (Get-Date); ReadEntries = $read; StillRunning = $null
      }
    }

    $good = New-End (New-Target 'good' 'the build') 'replaced' $none
    $goodNothing = New-End (New-Target 'good-nothing' $null) 'nothing' { param($since, $until) [pscustomobject]@{ Entries = @([pscustomobject]@{ Id = $id; Message = "downloaded again from $releases." }); Unread = @() } }

    $faulted = New-End (New-Target 'faulted' 'another build') 'replaced' $two
    $faulted.EventSet = $false
    $faulted.ExitCode = 1
    $faulted.StillRunning = 'C:\Control\InstallerClean.exe'
    $suffixes = @(Get-ReplaceSuffixes)
    Set-Content -LiteralPath "$($faulted.Target)$($suffixes[1])" -Value 'left'
    Set-Content -LiteralPath (Join-Path $faulted.TargetFolder 'stray.txt') -Value 'left'

    $sameAcl = New-End (New-Target 'same-acl' 'the build') 'replaced' $none -stagingFolder (Join-Path $root 'same-acl')

    $ownAcl = New-End (New-Target 'own-acl' 'the build') 'replaced' $none
    $acl = Get-Acl -LiteralPath $ownAcl.Target
    # A list of its own that the runner's account can still read, so the hash is read
    # and only the list differs.
    $acl.SetAccessRuleProtection($true, $false)
    $acl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new([System.Security.Principal.SecurityIdentifier]::new('S-1-5-18'), 'FullControl', 'Allow'))
    $acl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new([System.Security.Principal.WindowsIdentity]::GetCurrent().User, 'FullControl', 'Allow'))
    Set-Acl -LiteralPath $ownAcl.Target -AclObject $acl

    $nothingLeft = New-End (New-Target 'nothing-left' 'something') 'nothing' { param($since, $until) [pscustomobject]@{ Entries = @([pscustomobject]@{ Id = $id; Message = 'no link' }); Unread = @() } }
    $nothingLeft.ExitCode = 0

    $unread = New-End (New-Target 'unread' 'the build') 'replaced' $unreadable

    # A staging folder gone by the time the run is judged, as a window started at the
    # path leaves it, passes on the access list read as it was made.
    $stagingGone = Join-Path $root 'staging-gone'
    $null = New-FolderWithDescriptor $stagingGone $sddl
    $goneStaging = New-End (New-Target 'gone-staging' 'the build') 'replaced' $none -stagingFolder $stagingGone
    Remove-Item -LiteralPath $stagingGone -Recurse -Force

    $nothingNone = New-End (New-Target 'nothing-none' $null) 'nothing' $none
    $nothingTwo = New-End (New-Target 'nothing-two' $null) 'nothing' { param($since, $until) [pscustomobject]@{ Entries = @(1, 2 | ForEach-Object { [pscustomobject]@{ Id = $id; Message = "downloaded again from $releases." } }); Unread = @() } }

    $goodCommand = New-End (New-Target 'good-command' 'the build') 'command' $none
    $commandFaulted = New-End (New-Target 'command-faulted' 'another build') 'command' $none
    $commandFaulted.ExitCode = 2

    $result = Invoke-Caught { foreach ($end in $good, $goodNothing, $faulted, $sameAcl, $ownAcl, $nothingLeft, $unread, $goneStaging, $nothingNone, $nothingTwo, $goodCommand, $commandFaulted) { Test-ReplaceEnd $end } }
  }
  finally { Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue }
  Assert-Caught $result.Caught @(
    'control faulted did not set the started event',
    'control faulted ended with exit code 1, where it should end with 0',
    'control faulted left a file at',
    "control faulted left $($faulted.Target)$($suffixes[1]) beside the exe",
    'stray.txt in the folder it was put in',
    'control faulted put the exe in place and wrote 2 entries',
    'control faulted left a process running from C:\Control\InstallerClean.exe',
    "control same-acl's staging and target folders have the same access list",
    'control own-acl left the exe at the path with the access list',
    'control nothing-left ended with exit code 0, where it should end with 1',
    'control nothing-left, with nothing left at the path, has a file at',
    "control nothing-left's entry saying the update was not installed does not name",
    'control unread: the Application log could not be read, so what it wrote there was not judged: the control cannot read it',
    'control nothing-none, with nothing left at the path, wrote 0 entries',
    'control nothing-two, with nothing left at the path, wrote 2 entries',
    'control command-faulted ended with exit code 2, where it should end with 0',
    'control command-faulted left a file at') 'how a replace ended'

  Write-Host "== Control: the first act of a replace's own code"
  $target = 'C:\Control\target\InstallerClean.exe'
  $run = [pscustomobject]@{ Label = 'replace control'; Build = [pscustomobject]@{ Marker = 'replaced copy' }; Folder = 'C:\Control\staging'; Exe = 'InstallerClean.exe'; Target = $target }
  $rows = @(
    [pscustomobject]@{ Operation = 'CreateFile'; Path = 'C:\Control\staging\InstallerClean.exe'; Detail = '' },
    [pscustomobject]@{ Operation = 'QueryOpen'; Path = 'C:\Control\target\other.txt'; Detail = '' },
    [pscustomobject]@{ Operation = 'QueryBasicInformationFile'; Path = $target; Detail = '' },
    [pscustomobject]@{ Operation = 'CreateFile'; Path = "$target$((Get-ReplaceSuffixes)[2])"; Detail = '' })
  $marker = Get-Marker $rows $run
  if (-not $marker -or $marker.Kind -ne 'replaced copy' -or $marker.Index -ne 3) {
    Fail "The control on a replace's first act answered $(if ($marker) { "the $($marker.Kind) at row $($marker.Index)" } else { 'nothing' }), where it should answer the replaced copy at row 3."
  }
  else { Write-Host "  answered the replaced copy at row 3, as it must" }
  $run.Target = $null
  $marker = Get-Marker $rows $run
  if ($marker -and $marker.Kind -eq 'replaced copy') { Fail "The control on a replace's first act answered the replaced copy for a run with no target." }
  else { Write-Host "  answered no replaced copy for a run with no target, as it must" }

  Write-Host "== Control: the first act of a start that clears what a replace left"
  $own = "C:\Control\staging\InstallerClean.exe$((Get-ReplaceSuffixes)[0])"
  $rows = @(
    [pscustomobject]@{ Operation = 'QueryOpen'; Path = "C:\Control\other\InstallerClean.exe$((Get-ReplaceSuffixes)[0])"; Detail = '' },
    [pscustomobject]@{ Operation = 'QueryOpen'; Path = 'C:\Control\staging\InstallerClean.exe.other'; Detail = '' },
    [pscustomobject]@{ Operation = 'QueryOpen'; Path = $own; Detail = '' })
  $marker = Get-Marker $rows $run
  if (-not $marker -or $marker.Kind -ne 'replace leftovers' -or $marker.Index -ne 3) {
    Fail "The control on a start's first act answered $(if ($marker) { "the $($marker.Kind) at row $($marker.Index)" } else { 'nothing' }), where it should answer the replace leftovers at row 3."
  }
  else { Write-Host "  answered the replace leftovers at row 3, and nothing for a neighbour's name, as it must" }
}

# Runs Test-ImageFolders over a read made up for the purpose: loads from Windows' own folder,
# from Program Files and from Windows Defender's folder under %ProgramData% must pass, and a
# load from a user's profile and one from a temp folder must each fail.
function Test-ImageFolderControls {
  Write-Host "== Control: where a started exe may load an image from"
  $loads = @("$env:SystemRoot\System32\control.dll", "$env:ProgramFiles\Control\control.dll",
    "$env:ProgramData\Microsoft\Windows Defender\Platform\control\control.dll",
    'C:\Users\control\AppData\Local\Control\profile.dll', 'D:\Control\temp\temp.dll')
  $index = 0
  $read = [pscustomobject]@{ Loads = @($loads | ForEach-Object { $index++; [pscustomobject]@{ Place = 'elsewhere'; Path = $_; Index = $index } }) }
  $result = Invoke-Caught { Test-ImageFolders ([pscustomobject]@{ Label = 'control' }) $read }
  Assert-Caught $result.Caught @(
    'control loaded C:\Users\control\AppData\Local\Control\profile.dll, row 4, which is outside',
    'control loaded D:\Control\temp\temp.dll, row 5, which is outside') 'where an image is loaded from'
}
