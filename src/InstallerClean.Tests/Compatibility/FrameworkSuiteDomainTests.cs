using System.Reflection;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// The suite's own domain has the .NET Framework 4.8 compatibility defaults the shipped exes
/// have, so path code is tested under the rules it runs under. The first call into System.IO
/// in a domain fills in the runtime's defaults for the target framework the domain knows at
/// that moment, and the test assembly's module initializers make such a call before any test
/// runs. A domain that knows no target gets the defaults of .NET Framework 4.0, with legacy
/// path handling and a 260-character limit.
/// </summary>
public class FrameworkSuiteDomainTests
{
    /// <summary>
    /// The domain's reading as the tests are found. Where the runner lists a theory's rows at
    /// that point, as dotnet test does, the four values are in the test's name whether it
    /// passes or fails.
    /// </summary>
    public static TheoryData<string, string, bool, bool> Reading => new()
    {
        { SetupTarget(), RuntimeTarget(), PathSwitch("UseLegacyPathHandling"), PathSwitch("BlockLongPaths") },
    };

    [Theory]
    [MemberData(nameof(Reading))]
    public void The_suite_runs_with_the_path_handling_of_NET_Framework_4_8(
        string setupTarget, string runtimeTarget, bool useLegacyPathHandling, bool blockLongPaths)
    {
        // The row's values are there for the test's name. They are read again where the test
        // runs, which need not be where it was found.
        _ = (setupTarget, runtimeTarget, useLegacyPathHandling, blockLongPaths);
        var reading = $"setup target {SetupTarget()}, runtime target {RuntimeTarget()}, "
            + $"UseLegacyPathHandling {PathSwitch("UseLegacyPathHandling")}, BlockLongPaths {PathSwitch("BlockLongPaths")}";
        Assert.True(RuntimeTarget() == FrameworkSwitch.NetFramework48, reading);
        Assert.False(PathSwitch("UseLegacyPathHandling"), reading);
        Assert.False(PathSwitch("BlockLongPaths"), reading);
    }

    private static string SetupTarget() => AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName ?? "null";

    // The name the runtime fills its defaults in from: the domain's own, or in the process's
    // first domain the entry assembly's TargetFramework attribute.
    private static string RuntimeTarget() =>
        (string?)typeof(AppDomain)
            .GetMethod("GetTargetFrameworkName", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(AppDomain.CurrentDomain, null)
        ?? "null";

    private static bool PathSwitch(string property) =>
        FrameworkSwitch.Read(typeof(object).Assembly, "System.AppContextSwitches", property);
}
