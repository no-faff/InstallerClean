using System.Runtime.InteropServices;
using InstallerClean.Compatibility;

namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// <see cref="FrameworkRuntimeInformation.OSArchitecture"/> maps the machine values Windows gives as
/// .NET 10's RuntimeInformation does, and reads this machine's architecture.
/// </summary>
public class FrameworkRuntimeInformationTests
{
    [Theory]
    [InlineData((ushort)0x01C4, Architecture.Arm)]
    [InlineData((ushort)0x8664, Architecture.X64)]
    [InlineData((ushort)0xAA64, Architecture.Arm64)]
    [InlineData((ushort)0x014C, Architecture.X86)]
    public void A_machine_value_names_its_architecture(ushort machine, Architecture expected)
    {
        Assert.Equal(expected, FrameworkRuntimeInformation.MapMachineConstant(machine));
    }

    [Fact]
    public void An_unknown_machine_value_is_the_process_architecture()
    {
        Assert.Equal(RuntimeInformation.ProcessArchitecture, FrameworkRuntimeInformation.MapMachineConstant(0));
    }

    [Theory]
    [InlineData((ushort)12, Architecture.Arm64)]
    [InlineData((ushort)5, Architecture.Arm)]
    [InlineData((ushort)9, Architecture.X64)]
    [InlineData((ushort)0, Architecture.X86)]
    [InlineData((ushort)0xFFFF, Architecture.X86)]
    public void A_processor_architecture_names_its_architecture(ushort value, Architecture expected)
    {
        Assert.Equal(expected, FrameworkRuntimeInformation.Map(value));
    }

    // The suite runs as an x64 process; the machine under it is x64 or Arm64, never x86.
    [Fact]
    public void This_machine_reads_as_x64_or_Arm64()
    {
        Assert.Contains(FrameworkRuntimeInformation.OSArchitecture, new[] { Architecture.X64, Architecture.Arm64 });
    }
}
