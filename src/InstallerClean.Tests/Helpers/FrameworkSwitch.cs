using System.Reflection;

namespace InstallerClean.Tests.Helpers;

/// <summary>Reads a .NET Framework compatibility switch as the framework itself acts on it.</summary>
internal static class FrameworkSwitch
{
    /// <summary>The target framework name both exes carry and the suite runs under.</summary>
    internal const string NetFramework48 = ".NETFramework,Version=v4.8";

    /// <summary>
    /// The value of <paramref name="property"/>, a static property of the framework's own switch
    /// class <paramref name="typeName"/> in <paramref name="assembly"/>, which reads the switch
    /// once per domain and keeps it.
    /// </summary>
    internal static bool Read(Assembly assembly, string typeName, string property)
    {
        var type = assembly.GetType(typeName, throwOnError: true)!;
        var member = type.GetProperty(property, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(typeName, property);
        return (bool)member.GetValue(null)!;
    }
}
