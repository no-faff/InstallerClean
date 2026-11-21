using System.Runtime.InteropServices;
using InstallerClean.Compatibility;

namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// <see cref="FrameworkRuntimeInformation.OSArchitecture"/> reads the machine's architecture as
/// .NET 10's RuntimeInformation does: from IsWow64Process2 where kernel32 exports it, from
/// GetNativeSystemInfo where it does not, and with the machine values mapped as .NET 10 maps them.
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

    // An x64 process emulated on an Arm64 PC: IsWow64Process2 gives the machine as Arm64,
    // where GetNativeSystemInfo, read inside the emulation, gives AMD64.
    [Fact]
    public void An_x64_process_on_an_Arm64_machine_reads_Arm64()
    {
        var architecture = FrameworkRuntimeInformation.Read(
            (out ushort nativeMachine) => { nativeMachine = 0xAA64; return true; },
            NotAsked);

        Assert.Equal(Architecture.Arm64, architecture);
    }

    [Fact]
    public void A_failed_IsWow64Process2_reads_as_the_process_architecture()
    {
        var architecture = FrameworkRuntimeInformation.Read(
            (out ushort nativeMachine) => { nativeMachine = 0xAA64; return false; },
            NotAsked);

        Assert.Equal(RuntimeInformation.ProcessArchitecture, architecture);
    }

    [Fact]
    public void Without_IsWow64Process2_the_native_system_info_names_the_machine()
    {
        Assert.Equal(Architecture.Arm64, FrameworkRuntimeInformation.Read(null, () => 12));
        Assert.Equal(Architecture.X64, FrameworkRuntimeInformation.Read(null, () => 9));
    }

    // Where IsWow64Process2 answers, GetNativeSystemInfo is never asked.
    private static ushort NotAsked() => throw new InvalidOperationException("GetNativeSystemInfo was asked although IsWow64Process2 answered.");

    // CI's runners are Windows 10 version 1511 or later, whose kernel32 exports it.
    [Fact]
    public void Kernel32_here_exports_IsWow64Process2()
    {
        Assert.True(FrameworkRuntimeInformation.HasIsWow64Process2());
    }

    // The suite runs as an x64 process; the machine under it is x64 or Arm64, never x86.
    [Fact]
    public void This_machine_reads_as_x64_or_Arm64()
    {
        Assert.Contains(FrameworkRuntimeInformation.OSArchitecture, new[] { Architecture.X64, Architecture.Arm64 });
    }
}
