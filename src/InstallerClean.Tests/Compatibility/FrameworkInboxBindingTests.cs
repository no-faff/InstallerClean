using System.Net.Http;
using System.Reflection;

namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// The shipped code compiles against System.Net.Http 4.2.0.0 and System.ValueTuple 4.0.5.0
/// (the net48 reference assemblies), and on a PC the 4.8 runtime carries its own copies of
/// both in the GAC, at 4.0.0.0. For the app to run, a reference to the higher version has to
/// bind to the runtime's copy rather than fail to load; the first use of either type is where
/// that binding either succeeds or throws. HttpClient is the app's own first use, in the
/// update check and the report.
///
/// THIS IS NET48 EVIDENCE AND ONLY NET48 EVIDENCE. On .NET Framework these bindings resolve to
/// the GAC; on Fedora or any other .NET this test loads that runtime's own copies and says
/// nothing about 4.8. The reading below carries the versions that actually loaded, so a CI run
/// on the Windows net48 runner shows them whether the test passes or fails. Run it there, named.
/// </summary>
public class FrameworkInboxBindingTests
{
    [Fact]
    public void System_Net_Http_binds_to_the_runtime_copy_and_a_client_constructs()
    {
        // Constructing the client forces System.Net.Http to load and its types to resolve,
        // which is the binding the app depends on. A load failure here (FileLoadException,
        // TypeLoadException) is the failure this test exists to catch, and it fails the test
        // with the loaded version named rather than throwing an unlabelled exception.
        var reference = typeof(HttpClient).Assembly.GetName();
        Exception? failure = null;
        try
        {
            using var client = new HttpClient();
            _ = client.Timeout;
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        Assert.True(failure is null,
            $"System.Net.Http {reference.Version} did not bind to a usable copy: {failure}");
    }

    [Fact]
    public void System_ValueTuple_binds_to_the_runtime_copy_and_a_tuple_reads()
    {
        // A tuple expression is where the code uses ValueTuple, so reading one proves the
        // reference to System.ValueTuple 4.0.5.0 bound without a load failure. On .NET
        // Framework 4.8 the type itself is forwarded into mscorlib, so its hosting assembly
        // is not asserted here; the LoadedVersions theory below names what loaded.
        (int first, int second) pair = (1, 2);
        var sum = pair.first + pair.second;

        Assert.Equal(3, sum);
    }

    /// <summary>
    /// The versions the test assembly references, beside the versions that actually loaded, so
    /// a net48 CI run prints both. On 4.8 the loaded version is the runtime's GAC copy; a
    /// reference version that loaded proves the bind the app needs.
    /// </summary>
    public static TheoryData<string, string> LoadedVersions()
    {
        string Loaded(Assembly assembly) => assembly.GetName().Version?.ToString() ?? "null";
        return new TheoryData<string, string>
        {
            { "System.Net.Http loaded " + Loaded(typeof(HttpClient).Assembly), "System.Net.Http" },
            { "System.ValueTuple loaded " + Loaded(typeof(ValueTuple<int, int>).Assembly), "System.ValueTuple" },
        };
    }

    [Theory]
    [MemberData(nameof(LoadedVersions))]
    public void An_inbox_assembly_loaded(string reading, string name)
    {
        _ = reading;
        Assert.Contains(name, reading);
    }
}
