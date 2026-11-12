# Fails (exit 1) where a merged exe can load a library from anywhere but System32 or the
# framework's own folders, read from the two exes build-and-test merges (publish/window and
# publish/cli). Each exe is one assembly: ILRepack has folded Core and every package into
# it, so its own metadata is the whole of the code that can ask Windows for a library.
#
# For each exe the step holds:
#
#  - The assembly carries DefaultDllImportSearchPaths(System32), so every import in the
#    merged code searches System32 alone, and no declaration sets a search of its own.
#  - Every P/Invoke is declared in InstallerClean.Interop.Native and names a library on the
#    list below. A package that brings an import of its own, or a new library, fails.
#  - The imports that load a library by name are the two listed, from the callers listed:
#    SetDefaultDllDirectories, which only SystemLibraries.SearchSystem32For takes the address
#    of, and LoadLibraryExW, whose every call passes LOAD_LIBRARY_SEARCH_SYSTEM32 and no other
#    folder flag. A declaration of SetDllDirectory, AddDllDirectory, LoadLibrary, LdrLoadDll or
#    any other way in fails.
#  - No code calls Assembly.LoadFrom, LoadFile, UnsafeLoadFrom or LoadWithPartialName, Load
#    by name or AssemblyName, Activator.CreateInstanceFrom, or an AppDomain method that
#    loads from a file or a name, each of which can take an assembly from the exe's folder.
#  - The exe references .NET Framework's own assemblies and nothing else. A reference to
#    anything else is an assembly the merge left out, which the runtime would look for in the
#    folder the exe runs from.
#  - The merged module's static constructor, which runs before Main, makes as its first call
#    one that reaches SystemLibraries.SearchSystem32For, the call that sets the process's
#    library search to System32.
#
# Every run also checks itself: an assembly built here with each of those faults, which must
# report every one, and one built without them, which must report none. A missing exe, an
# exe that reads no imports, or a control that reports nothing fails the step.
#
# What a started exe loads, and from where, is the native-libraries job's: it runs each exe
# with Process Monitor recording and plants a file at every name the exe loads or looks for.
#
# Run from the repo root after the two merged builds: pwsh scripts/check-native-imports.ps1
param([string]$PublishRoot = 'publish')

$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'NativeImports.cs') -ReferencedAssemblies System.Reflection.Metadata, System.Collections,
  System.Linq, System.Runtime, System.Collections.Immutable, System.Security.Cryptography, System.Memory

$failures = [System.Collections.Generic.List[string]]::new()
function Fail([string]$message) { $failures.Add($message); Write-Host "::error::$message" }

# DllImportSearchPath.System32, and the flags of LoadLibraryEx that choose a folder to search.
$system32 = 0x800
$libraryFolderFlags = 0x1F00

$searchCall = 'InstallerClean.Interop.Native.SystemLibraries::SearchSystem32For(System.Reflection.Assembly)'

# The libraries the exes' own P/Invokes name.
$libraries = 'advapi32.dll', 'dwmapi.dll', 'kernel32.dll', 'msi.dll', 'ntdll.dll', 'ole32.dll', 'oleaut32.dll', 'shell32.dll', 'user32.dll'

# The imports that load a library by name or set where one is searched for, by entry point,
# each with the members that may call it. SetDefaultDllDirectories is taken by address.
$loaders = [ordered]@{
  'SetDefaultDllDirectories' = @($searchCall)
  'LoadLibraryExW' = @('InstallerClean.Compatibility.FrameworkCultureDisplayName+Icu::Load()')
}
$loaderPattern = '^(LoadLibrary|LoadPackagedLibrary|LdrLoadDll|SetDllDirectory|AddDllDirectory|RemoveDllDirectory|SetDefaultDllDirectories)'

# Calls that take an assembly from a file or by a name the runtime searches the exe's folder for.
$managedLoadPattern = '^(System\.Reflection\.Assembly::(LoadFrom|LoadFile|UnsafeLoadFrom|LoadWithPartialName)\(|' +
  'System\.Reflection\.Assembly::Load\((String|System\.Reflection\.AssemblyName)|' +
  'System\.Activator::CreateInstanceFrom\(|' +
  'System\.AppDomain::(ExecuteAssembly|ExecuteAssemblyByName|CreateInstanceFrom|CreateInstanceFromAndUnwrap)\(|' +
  'System\.AppDomain::Load\((String|System\.Reflection\.AssemblyName))'

# The .NET Framework 4.8 assemblies each exe binds to.
$frameworkReferences = 'mscorlib', 'netstandard', 'System', 'System.Core', 'System.Net.Http', 'System.Numerics', 'System.ValueTuple'
$exes = @(
  [pscustomobject]@{ Label = 'window'; Path = 'window/InstallerClean.exe'
    References = $frameworkReferences + 'PresentationCore', 'PresentationFramework', 'System.ComponentModel.DataAnnotations', 'System.Xaml', 'UIAutomationProvider', 'UIAutomationTypes', 'WindowsBase' }
  [pscustomobject]@{ Label = 'command line'; Path = 'cli/installerclean-cli.exe'; References = $frameworkReferences }
)

# What an assembly is judged against: the libraries its imports may name, the assemblies it may
# reference and the members that may call each loader.
function New-Policy($references, $callers) {
  [pscustomobject]@{ Libraries = $libraries; References = $references; Callers = $callers }
}

# Every fault the assembly shows against the policy, each as a sentence. Answers an empty list
# for an assembly with none.
function Get-Findings($assembly, $policy) {
  $found = [System.Collections.Generic.List[string]]::new()

  if ($null -eq $assembly.SearchPaths) { $found.Add("$($assembly.Name) carries no DefaultDllImportSearchPaths, so its imports search the folder it runs from first.") }
  elseif ($assembly.SearchPaths -ne $system32) { $found.Add("$($assembly.Name) carries DefaultDllImportSearchPaths $($assembly.SearchPaths), where it should carry System32 ($system32).") }

  foreach ($import in $assembly.Imports) {
    $name = "$($import.Type) $($import.Library)!$($import.EntryPoint)"
    if (($import.Type -split '\+')[0] -notmatch '^InstallerClean\.Interop\.Native\.[^.]+$') { $found.Add("P/Invoke outside InstallerClean.Interop.Native: $name.") }
    if ($import.Library.ToLowerInvariant() -notin $policy.Libraries) { $found.Add("P/Invoke of a library not on the list: $name.") }
    if ($null -ne $import.SearchPaths -and $import.SearchPaths -ne $system32) { $found.Add("P/Invoke with a library search of its own, $($import.SearchPaths): $name.") }
    if ($import.EntryPoint -match $loaderPattern -and -not $loaders.Contains($import.EntryPoint)) { $found.Add("P/Invoke that loads a library or sets where one is searched for, not on the list: $name.") }
  }

  foreach ($entry in $loaders.Keys) {
    $declared = @($assembly.Imports | Where-Object { $_.EntryPoint -eq $entry })
    if ($declared.Count -gt 1) { $found.Add("$entry is declared $($declared.Count) times.") }
    foreach ($import in $declared) {
      foreach ($call in @($assembly.Calls | Where-Object { $_.Callee -eq $import.Key })) {
        if ($call.Caller -notin $policy.Callers[$entry]) { $found.Add("$($call.Caller) calls $entry, which only $($policy.Callers[$entry] -join ', ') may.") }
        if ($entry -eq 'LoadLibraryExW') {
          if ($null -eq $call.LastConstant) { $found.Add("$($call.Caller) calls LoadLibraryEx with flags that are not a constant.") }
          elseif (($call.LastConstant -band $libraryFolderFlags) -ne $system32) { $found.Add("$($call.Caller) calls LoadLibraryEx with flags 0x$($call.LastConstant.ToString('X')), where the folder flags should be LOAD_LIBRARY_SEARCH_SYSTEM32 alone.") }
        }
      }
    }
  }

  foreach ($call in @($assembly.Calls | Where-Object { $_.Callee -match $managedLoadPattern } | Sort-Object Caller, Callee -Unique)) {
    $found.Add("$($call.Caller) calls $($call.Callee), which can load an assembly from the exe's own folder.")
  }

  foreach ($reference in $assembly.AssemblyReferences | Sort-Object -Unique) {
    if ($reference -notin $policy.References) { $found.Add("$($assembly.Name) references $reference, which is not a .NET Framework assembly the exe is held to.") }
  }

  $initializer = @($assembly.CallsOf('<Module>::.cctor()'))
  if (-not $initializer.Count) { $found.Add("$($assembly.Name)'s module static constructor makes no call, so nothing sets the library search before Main.") }
  elseif (-not $assembly.Reach($initializer[0].Callee, $searchCall)) {
    $found.Add("$($assembly.Name)'s module static constructor makes as its first call $($initializer[0].Callee), which does not reach $searchCall.")
  }
  $found
}

# The assemblies built here for the controls: one with none of the faults and one with all.
$sources = @{
  'good' = @'
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace InstallerClean.Interop.Native
{
    internal static class SystemLibraries
    {
        internal static void SearchSystem32For(Assembly exe) { }
    }

    internal static class Kernel32
    {
        [DllImport("kernel32.dll", EntryPoint = "SetDefaultDllDirectories", ExactSpelling = true)]
        internal static extern bool SetDefaultDllDirectories(uint flags);

        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr LoadLibraryEx(string name, IntPtr file, uint flags);
    }
}

namespace Fixture
{
    internal static class Start
    {
        [ModuleInitializer]
        internal static void Before() => InstallerClean.Interop.Native.SystemLibraries.SearchSystem32For(typeof(Start).Assembly);

        internal static IntPtr Icu() => InstallerClean.Interop.Native.Kernel32.LoadLibraryEx("icu.dll", IntPtr.Zero, 0x800);

        internal static Func<uint, bool> Set() => InstallerClean.Interop.Native.Kernel32.SetDefaultDllDirectories;
    }
}
'@
  'faulty' = @'
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Faulty
{
    internal static class Faults
    {
        [ModuleInitializer]
        internal static void Before() => Console.Write(string.Empty);

        [DllImport("planted.dll")]
        internal static extern int Outside();

        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr LoadLibrary(string name);

        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        internal static extern IntPtr LoadLibraryEx(string name, IntPtr file, uint flags);

        [DllImport("kernel32.dll", EntryPoint = "AddDllDirectory", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr AddDllDirectory(string folder);

        internal static IntPtr WrongFlags(string name) => LoadLibraryEx(name, IntPtr.Zero, 0x200);

        internal static IntPtr NotAConstant(string name, uint flags) => LoadLibraryEx(name, IntPtr.Zero, flags);

        internal static IntPtr Unlisted(string name) => LoadLibraryEx(name, IntPtr.Zero, 0x800);

        internal static Assembly FromFile(string path) => Assembly.LoadFrom(path);

        internal static Assembly ByName(string name) => Assembly.Load(name);

        internal static object Referenced() => new System.Xml.XmlDocument();
    }
}
'@
}
$fixtureDir = Join-Path ([IO.Path]::GetTempPath()) ('planted-native-imports-' + [guid]::NewGuid().ToString('N'))
$fixtures = @{}
try {
  foreach ($name in $sources.Keys) {
    $dir = Join-Path $fixtureDir $name
    New-Item -ItemType Directory -Path $dir | Out-Null
    Set-Content -Path (Join-Path $dir "$name.cs") -Value $sources[$name]
    Set-Content -Path (Join-Path $dir "$name.csproj") -Value @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <NoWarn>CA1416;CS0618</NoWarn>
  </PropertyGroup>
</Project>
'@
    $build = dotnet build (Join-Path $dir "$name.csproj") -c Release --nologo -o (Join-Path $dir 'out') 2>&1
    if ($LASTEXITCODE -ne 0) { $build | Write-Host; throw "The $name assembly did not build." }
    $path = Join-Path $dir "out/$name.dll"
    if (-not (Test-Path -LiteralPath $path)) { throw "The $name assembly's build wrote no $path." }
    $fixtures[$name] = [NativeImports.Assembly]::Open([IO.File]::ReadAllBytes($path), "$name.dll")
    $fixtures[$name].ReadMethods()
  }
}
finally { Remove-Item -LiteralPath $fixtureDir -Recurse -Force -ErrorAction SilentlyContinue }

# What each exe is judged against is what the controls are judged against too, apart from the
# members that may call a loader, which name the fixtures' own.
$fixturePolicy = [pscustomobject]@{
  Libraries = $libraries
  References = @('System.Runtime', 'System.Console', 'System.Runtime.InteropServices', 'System.Runtime.Loader', 'System.Collections')
  Callers = @{ 'SetDefaultDllDirectories' = @('Fixture.Start::Set()'); 'LoadLibraryExW' = @('Fixture.Start::Icu()', 'Faulty.Faults::WrongFlags(String)', 'Faulty.Faults::NotAConstant(String,UInt32)') }
}

Write-Host '== Control: an assembly with none of the faults'
$clean = @(Get-Findings $fixtures['good'] $fixturePolicy)
if ($clean) { foreach ($line in $clean) { Fail "The assembly built without faults was reported: $line" } }
else { Write-Host '  reported nothing, as it should' }

Write-Host '== Control: an assembly with every fault'
$faulty = @(Get-Findings $fixtures['faulty'] $fixturePolicy)
$expected = @(
  'carries no DefaultDllImportSearchPaths'
  'P/Invoke outside InstallerClean.Interop.Native: Faulty.Faults planted.dll!Outside'
  'P/Invoke outside InstallerClean.Interop.Native: Faulty.Faults kernel32.dll!LoadLibraryW'
  'P/Invoke outside InstallerClean.Interop.Native: Faulty.Faults kernel32.dll!LoadLibraryExW'
  'P/Invoke outside InstallerClean.Interop.Native: Faulty.Faults kernel32.dll!AddDllDirectory'
  'P/Invoke of a library not on the list: Faulty.Faults planted.dll!Outside'
  'P/Invoke with a library search of its own, 2: Faulty.Faults kernel32.dll!LoadLibraryExW'
  'P/Invoke that loads a library or sets where one is searched for, not on the list: Faulty.Faults kernel32.dll!LoadLibraryW'
  'P/Invoke that loads a library or sets where one is searched for, not on the list: Faulty.Faults kernel32.dll!AddDllDirectory'
  'Faulty.Faults::WrongFlags(String) calls LoadLibraryEx with flags 0x200'
  'Faulty.Faults::NotAConstant(String,UInt32) calls LoadLibraryEx with flags that are not a constant'
  'Faulty.Faults::Unlisted(String) calls LoadLibraryExW, which only'
  'Faulty.Faults::FromFile(String) calls System.Reflection.Assembly::LoadFrom(String)'
  'Faulty.Faults::ByName(String) calls System.Reflection.Assembly::Load(String)'
  'references System.Xml.ReaderWriter'
  "module static constructor makes as its first call Faulty.Faults::Before(), which does not reach $searchCall"
)
foreach ($want in $expected) {
  $hits = @($faulty | Where-Object { $_.Contains($want) })
  if ($hits.Count) { Write-Host "  reported, as it should be: $($hits[0])" }
  else { Fail "The assembly built with every fault did not report: $want" }
}
foreach ($line in $faulty) {
  if (-not @($expected | Where-Object { $line.Contains($_) })) { Fail "The assembly built with every fault reported something no fault accounts for: $line" }
}

foreach ($exe in $exes) {
  Write-Host ''
  $path = Join-Path $PublishRoot $exe.Path
  if (-not (Test-Path -LiteralPath $path)) { throw "$path is not there; the two merged builds come first." }
  Write-Host "== $($exe.Label): $path"
  $assembly = [NativeImports.Assembly]::Open([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $path).Path), $path)
  $assembly.ReadMethods()
  Write-Host "  $($assembly.Name) $($assembly.Version), $($assembly.Sha256)"
  Write-Host "  library search: $($assembly.SearchPaths)"
  Write-Host "  $($assembly.Imports.Count) P/Invokes, in $((@($assembly.Imports | ForEach-Object { ($_.Type -split '\+')[0] } | Sort-Object -Unique)).Count) types, naming $((@($assembly.Imports | ForEach-Object Library | Sort-Object -Unique)) -join ', ')"
  Write-Host "  references: $((@($assembly.AssemblyReferences | Sort-Object -Unique)) -join ', ')"
  # An exe that reads as having no imports, no calls or no initializer has been read wrongly,
  # and every check above would pass over it.
  if (-not $assembly.Imports.Count) { Fail "$path reads as holding no P/Invoke, so what it imports was not judged." }
  if (-not $assembly.Calls.Count) { Fail "$path reads as making no call, so what it calls was not judged." }
  # A listed caller that makes no call is a list gone stale, and a loader nothing calls leaves
  # the checks on its callers with nothing to judge.
  foreach ($entry in $loaders.Keys) {
    $declared = @($assembly.Imports | Where-Object { $_.EntryPoint -eq $entry })
    if (-not $declared) { Fail "$path declares no $entry, which the library search needs."; continue }
    foreach ($caller in $loaders[$entry]) {
      if (-not @($assembly.Calls | Where-Object { $_.Caller -eq $caller -and $_.Callee -eq $declared[0].Key })) { Fail "$caller is listed as calling $entry, and $path has no such call." }
      else { Write-Host "  $entry called by $caller" }
    }
  }
  $chain = $assembly.Reach('<Module>::.cctor()', $searchCall)
  if ($chain) { Write-Host "  before Main: $($chain -join ' -> ')" }
  foreach ($line in @(Get-Findings $assembly (New-Policy $exe.References $loaders))) { Fail "$($exe.Label): $line" }
}

Write-Host ''
if ($failures.Count) {
  Write-Host "$($failures.Count) checks failed:"
  $failures | ForEach-Object { Write-Host "  $_" }
  exit 1
}
Write-Host 'Every check passed.'
