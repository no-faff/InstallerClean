using System.Reflection;
using System.Runtime.CompilerServices;
using InstallerClean.Interop.Native;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The process's library search: System32 and the folders added with AddDllDirectory or
/// SetDllDirectory, set before each exe's Main, only in the process that exe was started for, and never an
/// exception out of a module initializer.
/// </summary>
public class SystemLibrariesSearchTests
{
    private const uint System32AndAddedFolders = 0x00000800 | 0x00000400;

    private static readonly Assembly Exe = typeof(InstallerClean.Cli.LibrarySearch).Assembly;

    [Fact]
    public void The_exes_own_process_searches_System32_and_added_folders()
    {
        var calls = new List<uint>();

        var set = SystemLibraries.SearchSystem32For(Exe, Exe, flags => { calls.Add(flags); return true; });

        Assert.True(set);
        Assert.Equal(new[] { System32AndAddedFolders }, calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_process_started_for_another_assembly_or_none_keeps_its_own_search(bool entryKnown)
    {
        var calls = new List<uint>();
        var entry = entryKnown ? typeof(SystemLibrariesSearchTests).Assembly : null;

        var set = SystemLibraries.SearchSystem32For(Exe, entry, flags => { calls.Add(flags); return true; });

        Assert.False(set);
        Assert.Empty(calls);
    }

    [Fact]
    public void A_refused_search_does_not_throw()
    {
        Assert.False(SystemLibraries.SearchSystem32For(Exe, Exe, _ => false));
    }

    [Fact]
    public void A_call_that_throws_does_not_throw_out()
    {
        Assert.False(SystemLibraries.SearchSystem32For(Exe, Exe, _ => throw new DllNotFoundException("kernel32.dll")));
    }

    [Theory]
    [InlineData(typeof(InstallerClean.ProcessDefaults))]
    [InlineData(typeof(InstallerClean.Cli.LibrarySearch))]
    public void Each_exe_sets_the_search_as_it_is_loaded(Type initializer)
    {
        var setUp = initializer.GetMethod("SetBeforeMain", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(setUp);
        Assert.NotNull(setUp.GetCustomAttribute<ModuleInitializerAttribute>());
    }
}
