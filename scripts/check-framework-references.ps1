# Fails (exit 1) where a merged exe needs what .NET Framework does not have, read from the two
# exes build-and-test merges (publish/window and publish/cli) against Microsoft's reference
# assemblies for .NET Framework 4.0, 4.5, 4.5.1, 4.5.2, 4.6, 4.6.1, 4.6.2, 4.7, 4.7.1, 4.7.2
# and 4.8.
#
# For each exe the step holds:
#
#  - Every .NET Framework type and member the exe references resolves in 4.8, through the
#    assembly its reference names and that assembly's forwarders (FrameworkReferences.cs).
#    What the exe references that 4.6.2 lacks is printed for the record.
#  - The exe reaches its start checks, for Windows 10 version 1607 and .NET Framework 4.8,
#    and their messages on 4.0, 4.5, 4.5.1, 4.5.2, 4.6, 4.6.1, 4.6.2, 4.7, 4.7.1 and 4.7.2:
#    nothing the runtime compiles or loads before the checks have answered is missing from
#    any of them. The command line's checks are Program.RefuseOnAPcThatCannotRunIt, which
#    Main calls; the window's are App.EndOnAPcThatCannotRunIt, which App's static constructor
#    calls first. Each check is
#    followed, and what runs after it (Program.Start, App.OnStartup and the rest of App's
#    static constructor) is not. Each missing member is printed once, with the chain it was
#    first met on and every method of the app's own through which the walk reaches it.
#  - No assembly merged into the exe but the exe's own carries a module initializer: the
#    merge keeps only the primary assembly's, so another's would never run. The exe's own
#    carries exactly one, and the merged exe's module constructor calls it.
#
# Every run also checks itself: an exe built here whose check path reaches what 4.0 to 4.7.1
# lack, which must report each one and nothing else, and name both routes to the one it
# reaches two ways, and one whose check path reaches nothing missing, which must report
# nothing on its path and the misses of its start in the whole-exe list. One whose check is called from its entry type's static
# constructor must report what that constructor runs before the check and nothing after
# it. A reference .NET Framework 4.6.2 defines in mscorlib but the
# exe takes from netstandard must read as missing from 4.6.2, which has no netstandard, and
# as present in 4.7.1. A library built with a module initializer must be reported, and one
# without must not. A download whose hash differs, a set with no mscorlib, an exe with no
# framework references or a walk that reads no method fails the step.
#
# Run from the repo root after the two merged builds: pwsh scripts/check-framework-references.ps1
param([string]$PublishRoot = 'publish')

$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'NativeImports.cs'), (Join-Path $PSScriptRoot 'FrameworkReferences.cs') -ReferencedAssemblies System.Reflection.Metadata,
  System.Collections, System.Linq, System.Runtime, System.Collections.Immutable, System.Security.Cryptography, System.Memory

$failures = [System.Collections.Generic.List[string]]::new()
function Fail([string]$message) { $failures.Add($message); Write-Host "::error::$message" }

# Microsoft.NETFramework.ReferenceAssemblies.<version> 1.0.3 from nuget.org, each with the
# SHA-512 of its package and the folder its reference assemblies sit in.
$packages = [ordered]@{
  'net40'  = @{ Folder = 'v4.0';   Sha512 = 'bd45e6c0fc62ee56350c49b19eb9ef7bf6b826037f6755c4e916a8cdc9a5aab7f6399c6cf6c99705bdd0f72d9d6769423ed54cc990513388d03c15f3d38227a5' }
  'net45'  = @{ Folder = 'v4.5';   Sha512 = 'ccf2793ea73af9c060e22af7dc05abc80f025312498faf02b3509fa3ca6566dd471f743407f12a5689fa2d15f0f5bf1d7d0c8b60ca9324993699a5e02e118923' }
  'net451' = @{ Folder = 'v4.5.1'; Sha512 = '7d2d90c31c7725ba09d9a50292730674cd7749a60bb20bab5de86ff8ba799676e5fe52b0b47e794bfb88dcaeca4411f90aa1b12ea05f59626f033fde952f4008' }
  'net452' = @{ Folder = 'v4.5.2'; Sha512 = '125b35cb0b1b2c3fd7a44810595d3232900e1e4d0b79ccf88d698e78bc33192149dca15707ee7bc0a8d7e2b109c6a25ff7b08816752670efa9bc8cfc2df4d7bf' }
  'net46'  = @{ Folder = 'v4.6';   Sha512 = 'd8351799a723a8d4d79f9144f79a9613a6cbdb669ed26ff4acbfd280adda5ac5baa82943cd7566a3490e2f270287a255ea2ddd414fca32e4f6c2dad3f047d988' }
  'net461' = @{ Folder = 'v4.6.1'; Sha512 = 'b717dcc15dab52be03c6bd2bda353adfb0ecc785bd5a8d4fc47239a1324f46c4ac187595d902d2ef90c69bbbc9d9eeec97d04c6599f5ef8feba374833bfa01c6' }
  'net462' = @{ Folder = 'v4.6.2'; Sha512 = '1cc9f441098f61c26c56e29eb7d580f0841b070799bdced44bcf0ddfd8ad491a9c9ffd57393fca6a8d508a2ca4c54fafc7c93bff38a6d7571598ab6e9e3bb830' }
  'net47'  = @{ Folder = 'v4.7';   Sha512 = 'c7610db70f82c80395f443b5579a2478c90f911f0b1965bc17ac46fa7771c04ab718c94d983a34083059b48ebdd0f3ffd233074f094fc32413bd80615291a264' }
  'net471' = @{ Folder = 'v4.7.1'; Sha512 = '57199cd45c492d4e210346f699c10b9f83b94ba3f701fc6d9451e38351b0ea831a04f2fc80c46df574fe20e0e4cc359e27d6d7ff260de043e224ccfd661ff685' }
  'net472' = @{ Folder = 'v4.7.2'; Sha512 = '3ef64aa456edb31b49062ea1272cedfe268aef5a8676ef66355a494f8dfe8b55a9db7ca6e7b615b9023e0449ceffdf4ada9c4f304818b73eb69fbc99144822d8' }
  'net48'  = @{ Folder = 'v4.8';   Sha512 = '5d62a0c9e35a74d71341a215bd007c06b74236b11aafa7e8fdd7b539d41167d1c5cb48dc05268cf08579abae196a6a6c4a70bc0254ec002686654ba8170e3544' }
}

# The versions below 4.8 a check path has to run on, and the one every reference resolves in.
$belowLabels = 'net40', 'net45', 'net451', 'net452', 'net46', 'net461', 'net462', 'net47', 'net471', 'net472'
$targetLabel = 'net48'

$work = Join-Path ([IO.Path]::GetTempPath()) ('framework-references-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
  $sets = [ordered]@{}
  foreach ($label in $packages.Keys) {
    $id = "microsoft.netframework.referenceassemblies.$label"
    $nupkg = Join-Path $work "$label.nupkg"
    Invoke-WebRequest -Uri "https://api.nuget.org/v3-flatcontainer/$id/1.0.3/$id.1.0.3.nupkg" -OutFile $nupkg -UseBasicParsing
    $hash = (Get-FileHash -Algorithm SHA512 -LiteralPath $nupkg).Hash.ToLowerInvariant()
    if ($hash -ne $packages[$label].Sha512) { throw "The $label reference package's SHA-512 is $hash, where the pinned one is $($packages[$label].Sha512)." }
    $dir = Join-Path $work $label
    Expand-Archive -LiteralPath $nupkg -DestinationPath $dir
    $sets[$label] = [FrameworkReferences.ReferenceSet]::Open($label, (Join-Path $dir "build/.NETFramework/$($packages[$label].Folder)"))
    Write-Host "$label`: $($sets[$label].Files) reference assemblies"
  }
  $below = [FrameworkReferences.ReferenceSet[]]@($belowLabels | ForEach-Object { $sets[$_] })

  # The whole-exe and path findings for one exe, each as a line naming the set.
  function Get-Whole($exe, $set) { @($exe.Missing($set) | ForEach-Object { "$($set.Label): $($_.ToString())" }) }
  function Get-Walk($exe, $checkCall) { [FrameworkReferences.Walk]::Run($exe, $below, $checkCall) }
  # A miss prints the chain it was first met on, and then every way the app's own code reaches
  # it, so a second route is in view before the first is cut.
  # The app's own methods are those in InstallerClean; a fixture's are its Program's.
  function Get-PathLines($walk, [string]$app = 'InstallerClean.') { @($walk.Misses | ForEach-Object { "$($_.Set): $($_.ToString()), named by $(($walk.ChainTo($_.From)) -join ' -> '); reached from $(($walk.AppRoutesTo($_.From, $app)) -join ' | ')" }) }

  # Whether the walk followed the method, by Type::Name.
  function Test-Followed($exe, $walk, [string]$member) {
    $type, $name = $member -split '::', 2
    $h = $exe.FindMethod($type, $name)
    if ($null -eq $h) { throw "$($exe.Name) has no method $member." }
    $walk.IsFollowed($h)
  }

  # The exes and the library built here for the controls.
  $fixtureMain = @'
    public static int Main(string[] args)
    {
        Before();
        int? refused = Check();
        if (refused.HasValue) return refused.Value;
        return Start(args) + Later(args);
    }
'@
  $sources = @{
    'clean' = @"
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fixture
{
    internal static class Program
    {
$fixtureMain

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Before() => Console.Out.Flush();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int? Check() => Environment.Version.Major < 4 ? 1 : (int?)null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Start(string[] args)
        {
            var pair = (args.Length, "start");
            var set = args.ToHashSet(StringComparer.Ordinal);
            return pair.Item1 + set.Count + (RuntimeInformation.OSArchitecture == Architecture.X64 ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Later(string[] args) => (args.Length, 0, 0).Item1;
    }
}
"@
    'faulty' = @"
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fixture
{
    internal static class Program
    {
$fixtureMain

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Before()
        {
            var pair = (1, 2);
            Console.Write(pair.Item1 + Shared.Tally());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int? Check()
        {
            IShape shape = new Square();
            return shape.Sides() + Counted.Total + Shared.Tally() > 0 ? (int?)null : 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Start(string[] args) => (args.Length, 0, 0, 0).Item1;

        static int Later(string[] args) => (args.Length, 0, 0).Item1;
    }

    internal interface IShape { int Sides(); }

    internal sealed class Square : IShape
    {
        public int Sides() => RuntimeInformation.ProcessArchitecture == Architecture.X64 ? 4 : 4;
    }

    internal static class Counted
    {
        internal static readonly int Total = new[] { "a" }.ToHashSet(StringComparer.Ordinal).Count;
    }

    internal static class Shared
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int Tally() => (0, 0, 0, 0, 0).Item1;
    }
}
"@
    'staticcheck' = @'
using System;
using System.Runtime.CompilerServices;

namespace Fixture
{
    internal static class Program
    {
        static Program()
        {
            Before();
            Check();
            After();
        }

        public static int Main(string[] args) => Start(args);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Before()
        {
            var pair = (1, 2);
            Console.Write(pair.Item1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check()
        {
            if (Environment.Version.Major < 4) Environment.Exit(1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void After() => Console.Write((1, 2, 3).Item1);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Start(string[] args) => (args.Length, 0, 0, 0).Item1;
    }
}
'@
    'initializer' = @'
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : Attribute { }
}

namespace Fixture
{
    internal static class Start
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Before() => System.Console.Out.Flush();
    }
}
'@
    'library' = @'
namespace Fixture
{
    public static class Library
    {
        public static int Answer() => 42;
    }
}
'@
  }
  $fixtures = @{}
  foreach ($name in $sources.Keys) {
    $dir = Join-Path $work "fixture-$name"
    New-Item -ItemType Directory -Path $dir | Out-Null
    Set-Content -Path (Join-Path $dir "$name.cs") -Value $sources[$name]
    $outputType = if ($name -in 'clean', 'faulty', 'staticcheck') { 'Exe' } else { 'Library' }
    Set-Content -Path (Join-Path $dir "$name.csproj") -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <OutputType>$outputType</OutputType>
    <LangVersion>latest</LangVersion>
    <Optimize>true</Optimize>
  </PropertyGroup>
</Project>
"@
    $build = dotnet build (Join-Path $dir "$name.csproj") -c Release --nologo -o (Join-Path $dir 'out') 2>&1
    if ($LASTEXITCODE -ne 0) { $build | Write-Host; throw "The $name fixture did not build." }
    $ext = if ($outputType -eq 'Exe') { 'exe' } else { 'dll' }
    $path = Join-Path $dir "out/$name.$ext"
    if (-not (Test-Path -LiteralPath $path)) { throw "The $name fixture's build wrote no $path." }
    $fixtures[$name] = $path
  }

  $fixtureCheck = 'Fixture.Program::Check'

  Write-Host '== Control: an exe whose check path reaches nothing missing'
  $clean = [FrameworkReferences.Exe]::Open($fixtures['clean'])
  $walk = Get-Walk $clean $fixtureCheck
  $lines = @(Get-PathLines $walk)
  if ($lines) { foreach ($line in $lines) { Fail "The clean exe's check path was reported: $line" } }
  else { Write-Host "  its check path reported nothing, as it should, over $($walk.Compiled.Count) methods" }
  if (-not (Test-Followed $clean $walk $fixtureCheck)) { Fail "The walk did not follow the clean exe's check." }
  if (Test-Followed $clean $walk 'Fixture.Program::Start') { Fail "The walk followed the clean exe's Start, which runs after the check." }
  $whole = @(Get-Whole $clean $sets['net462'])
  foreach ($want in 'System.ValueTuple`2', 'System.ValueTuple`3', 'System.Runtime.InteropServices.RuntimeInformation', 'System.Linq.Enumerable::ToHashSet') {
    if (@($whole | Where-Object { $_.Contains($want) }).Count) { Write-Host "  whole exe on net462 reports, as it should: $want" }
    else { Fail "The clean exe's whole-exe list on net462 does not report $want." }
  }
  $target = @(Get-Whole $clean $sets[$targetLabel])
  foreach ($line in $target) { Fail "The clean exe reads as needing what $targetLabel lacks: $line" }

  Write-Host '== Control: an exe whose check path reaches what 4.0 to 4.7.1 lack'
  $faulty = [FrameworkReferences.Exe]::Open($fixtures['faulty'])
  $walk = Get-Walk $faulty $fixtureCheck
  $lines = @(Get-PathLines $walk 'Fixture.Program::')
  # Before runs ahead of the check; Later is compiled into Main; the check reaches Square
  # through an interface call and Counted through its static field; Before and the check
  # both call Shared.Tally.
  $expected = @(
    foreach ($label in 'net40', 'net45', 'net451', 'net452', 'net46', 'net461', 'net462') {
      "$label`: System.ValueTuple``2  [mscorlib]", "$label`: System.ValueTuple``2::.ctor", "$label`: System.ValueTuple``2::Item1"
      "$label`: System.ValueTuple``3  [mscorlib]", "$label`: System.ValueTuple``3::.ctor", "$label`: System.ValueTuple``3::Item1"
      "$label`: System.ValueTuple``5  [mscorlib]", "$label`: System.ValueTuple``5::.ctor", "$label`: System.ValueTuple``5::Item1"
    }
    foreach ($label in 'net40', 'net45', 'net451', 'net452', 'net46', 'net461', 'net462', 'net47') {
      "$label`: System.Runtime.InteropServices.RuntimeInformation  [mscorlib]", "$label`: System.Runtime.InteropServices.Architecture  [mscorlib]"
      "$label`: System.Runtime.InteropServices.RuntimeInformation::get_ProcessArchitecture"
    }
    foreach ($label in 'net40', 'net45', 'net451', 'net452', 'net46', 'net461', 'net462', 'net47', 'net471') { "$label`: System.Linq.Enumerable::ToHashSet" }
  )
  # A member reached by two routes prints one chain and names both.
  foreach ($line in @($lines | Where-Object { $_.Contains('ValueTuple`5') })) {
    if (-not ($line.Contains('reached from Fixture.Program::Before() | Fixture.Program::Check()'))) { Fail "The faulty exe's Shared.Tally is reached from Before and the check, and the line does not name both: $line" }
  }
  if (-not @($lines | Where-Object { $_.Contains('ValueTuple`5') })) { Fail 'The faulty exe did not report what Shared.Tally names.' }
  foreach ($want in $expected) {
    $hits = @($lines | Where-Object { $_.StartsWith($want) })
    if ($hits.Count) { Write-Host "  reported, as it should be: $($hits[0])" }
    else { Fail "The faulty exe did not report: $want" }
  }
  foreach ($line in $lines) {
    if (-not @($expected | Where-Object { $line.StartsWith($_) })) { Fail "The faulty exe reported something no fault accounts for: $line" }
  }
  if (@($lines | Where-Object { $_.Contains('ValueTuple`4') })) { Fail "The faulty exe's Start, which runs after the check, was read as on its path." }

  Write-Host '== Control: an exe whose check is called from its entry type''s static constructor'
  # The constructor runs before Main, up to the check; what it calls after the check, and
  # what Main calls, runs only once the check has answered.
  $static = [FrameworkReferences.Exe]::Open($fixtures['staticcheck'])
  $walk = Get-Walk $static $fixtureCheck
  $lines = @(Get-PathLines $walk 'Fixture.Program::')
  $expected = @(foreach ($label in 'net40', 'net45', 'net451', 'net452', 'net46', 'net461', 'net462') {
    "$label`: System.ValueTuple``2  [mscorlib]", "$label`: System.ValueTuple``2::.ctor", "$label`: System.ValueTuple``2::Item1"
  })
  foreach ($want in $expected) {
    $hits = @($lines | Where-Object { $_.StartsWith($want) })
    if ($hits.Count) { Write-Host "  reported, as it should be: $($hits[0])" }
    else { Fail "The exe checking from its static constructor did not report: $want" }
  }
  foreach ($line in $lines) {
    if (-not @($expected | Where-Object { $line.StartsWith($_) })) { Fail "The exe checking from its static constructor reported what runs after its check: $line" }
  }
  if (-not (Test-Followed $static $walk $fixtureCheck)) { Fail "The walk did not follow the check called from the static constructor." }
  foreach ($after in 'Fixture.Program::After', 'Fixture.Program::Start') {
    if (Test-Followed $static $walk $after) { Fail "The walk followed $after, which runs after the check." }
  }

  Write-Host '== Control: a reference taken from netstandard'
  # The command line's merged packages take System.Object from netstandard. 4.6.2's mscorlib
  # defines it, and 4.6.2 has no netstandard, so it is missing there and present in 4.7.1.
  $cliPath = Join-Path $PublishRoot 'cli/installerclean-cli.exe'
  if (-not (Test-Path -LiteralPath $cliPath)) { throw "$cliPath is not there; the two merged builds come first." }
  $cli = [FrameworkReferences.Exe]::Open((Resolve-Path -LiteralPath $cliPath).Path)
  $object = 'System.Object  [netstandard]'
  if (@(Get-Whole $cli $sets['net462'] | Where-Object { $_.StartsWith("net462: $object ") }).Count) { Write-Host "  missing from net462, as it should be: $object" }
  else { Fail "The command line's $object does not read as missing from net462, which has no netstandard." }
  if (@(Get-Whole $cli $sets['net471'] | Where-Object { $_.StartsWith("net471: $object ") }).Count) { Fail "The command line's $object reads as missing from net471, which has netstandard." }
  else { Write-Host "  present in net471, as it should be: $object" }

  Write-Host '== Control: a library with a module initializer and one without'
  $found = @([FrameworkReferences.Meta]::ModuleInitializers($fixtures['initializer']))
  if (@($found | Where-Object { $_ -eq '[ModuleInitializer] Fixture.Start::Before' }).Count) { Write-Host "  reported, as it should be: $($found -join '; ')" }
  else { Fail "The library built with a module initializer was not reported: $($found -join '; ')" }
  $found = @([FrameworkReferences.Meta]::ModuleInitializers($fixtures['library']))
  if ($found) { Fail "The library built without a module initializer was reported: $($found -join '; ')" }
  else { Write-Host '  the one without reported nothing, as it should' }

  $exes = @(
    [pscustomobject]@{ Label = 'window'; Path = 'window/InstallerClean.exe'; Project = 'src/InstallerClean'; CheckCall = 'InstallerClean.App::EndOnAPcThatCannotRunIt'
      Check = 'InstallerClean.App::EndOnAPcThatCannotRunIt'; Start = 'InstallerClean.App::OnStartup'; Initializer = 'InstallerClean.ProcessDefaults::SetBeforeMain' }
    [pscustomobject]@{ Label = 'command line'; Path = 'cli/installerclean-cli.exe'; Project = 'src/InstallerClean.Cli'; CheckCall = 'InstallerClean.Cli.Program::RefuseOnAPcThatCannotRunIt'
      Check = 'InstallerClean.Cli.Program::RefuseOnAPcThatCannotRunIt'; Start = 'InstallerClean.Cli.Program::Start'; Initializer = 'InstallerClean.Cli.LibrarySearch::SetBeforeMain' }
  )
  foreach ($entry in $exes) {
    Write-Host ''
    $path = Join-Path $PublishRoot $entry.Path
    if (-not (Test-Path -LiteralPath $path)) { throw "$path is not there; the two merged builds come first." }
    Write-Host "== $($entry.Label): $path"
    $exe = [FrameworkReferences.Exe]::Open((Resolve-Path -LiteralPath $path).Path)
    Write-Host "  $($exe.FrameworkTypeReferences) framework type references, $($exe.FrameworkMemberReferences) framework member references"
    if (-not $exe.FrameworkTypeReferences -or -not $exe.FrameworkMemberReferences) { Fail "$path reads as referencing no .NET Framework type or member, so nothing was judged." }

    $target = @(Get-Whole $exe $sets[$targetLabel])
    foreach ($line in $target) { Fail "$($entry.Label) needs what .NET Framework 4.8 lacks: $line" }
    if (-not $target) { Write-Host "  every reference resolves in $targetLabel" }

    $old = @(Get-Whole $exe $sets['net462'])
    Write-Host "  the exe as a whole needs $($old.Count) types and members net462 lacks:"
    foreach ($line in $old) { Write-Host "    $line" }
    if (-not $old) { Fail "$($entry.Label) reads as needing nothing net462 lacks, which a set with no netstandard and no ValueTuple cannot answer." }

    $walk = Get-Walk $exe $entry.CheckCall
    Write-Host "  the check path: $($walk.Compiled.Count) methods compiled, $($walk.TypesNamed) type and $($walk.MembersNamed) member names resolved against $($belowLabels -join ', ')"
    if ($walk.Compiled.Count -lt 2) { Fail "$($entry.Label)'s walk read $($walk.Compiled.Count) methods, so its check path was not judged." }
    if (-not (Test-Followed $exe $walk $entry.Check)) { Fail "$($entry.Label)'s walk did not follow $($entry.Check), so its check was not judged." }
    if (Test-Followed $exe $walk $entry.Start) { Fail "$($entry.Label)'s walk followed $($entry.Start), which runs after the check." }
    $lines = @(Get-PathLines $walk)
    foreach ($line in $lines) { Fail "$($entry.Label)'s check path needs what a PC below 4.8 lacks: $line" }
    if (-not $lines) { Write-Host "  nothing on the check path is missing from $($belowLabels -join ', ')" }
    foreach ($a in $walk.Attributes) { Write-Host "  attribute on the path, not loaded by the runtime: $($a.Set): $($a.ToString()), on $($a.From)" }

    # The merge's inputs, from the response file Directory.Build.targets writes: every line not
    # an ILRepack option (a slash, a word, and a colon or nothing), the first being the exe's
    # own assembly.
    $rsp = Join-Path $entry.Project 'obj/Release/net48/onefile.rsp'
    if (-not (Test-Path -LiteralPath $rsp)) { throw "$rsp is not there, so the merge's inputs cannot be read." }
    $inputs = @(Get-Content -LiteralPath $rsp | Where-Object { $_ -and $_ -notmatch '^/[A-Za-z]+(:|$)' })
    if ($inputs.Count -lt 2) { Fail "$rsp names $($inputs.Count) inputs, so the merged assemblies were not judged." }
    Write-Host "  the merge's $($inputs.Count) inputs:"
    for ($i = 0; $i -lt $inputs.Count; $i++) {
      $found = @([FrameworkReferences.Meta]::ModuleInitializers($inputs[$i]))
      Write-Host "    $([IO.Path]::GetFileName($inputs[$i])): $(if ($found) { $found -join '; ' } else { 'no module initializer' })"
      if ($i -eq 0) {
        $own = @($found | Where-Object { $_.StartsWith('[ModuleInitializer] ') })
        if ($own.Count -ne 1 -or $own[0] -ne "[ModuleInitializer] $($entry.Initializer)") { Fail "$($entry.Label)'s own assembly carries $($own.Count) module initializers ($($own -join '; ')), where it should carry $($entry.Initializer) alone." }
      }
      elseif ($found) { Fail "$([IO.Path]::GetFileName($inputs[$i])), merged into the $($entry.Label), carries a module initializer the merge drops: $($found -join '; ')" }
    }
    $merged = @([FrameworkReferences.Meta]::ModuleInitializers($exe.Path))
    Write-Host "  the merged exe: $($merged -join '; ')"
    if (-not @($merged | Where-Object { $_ -eq '<Module>::.cctor' }).Count) { Fail "The merged $($entry.Label) has no module constructor, so $($entry.Initializer) never runs." }
    elseif (-not (Test-Followed $exe $walk $entry.Initializer)) { Fail "The merged $($entry.Label)'s module constructor does not reach $($entry.Initializer)." }
  }
}
finally { Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue }

Write-Host ''
if ($failures.Count) {
  Write-Host "$($failures.Count) checks failed:"
  $failures | ForEach-Object { Write-Host "  $_" }
  exit 1
}
Write-Host 'Every check passed.'
