using System.Reflection;
using System.Runtime.CompilerServices;
using InstallerClean.Interop.Native;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The process's library search: System32 and the folders added with AddDllDirectory or
/// SetDllDirectory, set before each exe's Main, only in the process started from that exe, and
/// never an exception out of a module initializer.
/// </summary>
public class SystemLibrariesSearchTests
{
    private const uint System32AndAddedFolders = 0x00000800 | 0x00000400;


    [Fact]
    public void The_exes_own_process_searches_System32_and_added_folders()
    {
        var calls = new List<uint>();
        var kept = new List<Exception>();

        var set = SystemLibraries.SearchSystem32For(true, flags => { calls.Add(flags); return true; }, kept.Add);

        Assert.True(set);
        Assert.Equal(new[] { System32AndAddedFolders }, calls);
        Assert.Empty(kept);
    }

    [Fact]
    public void A_process_that_loaded_the_exe_without_being_started_from_it_keeps_its_own_search_and_says_nothing()
    {
        var calls = new List<uint>();
        var kept = new List<Exception>();

        var set = SystemLibraries.SearchSystem32For(false, flags => { calls.Add(flags); return true; }, kept.Add);

        Assert.False(set);
        Assert.Empty(calls);
        Assert.Empty(kept);
    }

    [Fact]
    public void A_process_that_cannot_be_told_keeps_its_own_search_and_records_why()
    {
        var calls = new List<uint>();
        var kept = new List<Exception>();

        var set = SystemLibraries.SearchSystem32For(null, flags => { calls.Add(flags); return true; }, kept.Add);

        Assert.False(set);
        Assert.Empty(calls);
        Assert.Contains("could not be told", Assert.Single(kept).Message);
    }

    [Fact]
    public void A_refused_search_is_recorded_and_does_not_throw()
    {
        var kept = new List<Exception>();

        Assert.False(SystemLibraries.SearchSystem32For(true, _ => false, kept.Add));
        Assert.IsType<System.ComponentModel.Win32Exception>(Assert.Single(kept));
    }

    [Fact]
    public void A_call_that_throws_is_recorded_and_does_not_throw_out()
    {
        var kept = new List<Exception>();

        Assert.False(SystemLibraries.SearchSystem32For(true, _ => throw new DllNotFoundException("kernel32.dll"), kept.Add));
        Assert.IsType<DllNotFoundException>(Assert.Single(kept));
    }

    [Theory]
    [InlineData(typeof(InstallerClean.ProcessDefaults))]
    [InlineData(typeof(InstallerClean.Cli.LibrarySearch))]
    public void Neither_exe_is_this_test_process_s_own_image(Type inExe)
    {
        // The test host loads both exes as references; its own image is the host's.
        Assert.False(SystemLibraries.IsProcessImage(inExe.Assembly));
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
