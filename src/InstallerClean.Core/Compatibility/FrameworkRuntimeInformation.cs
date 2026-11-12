using System.Runtime.InteropServices;
using InstallerClean.Interop.Native;

namespace InstallerClean.Compatibility;

/// <summary>
/// <see cref="RuntimeInformation.OSArchitecture"/> as .NET 10 answers it on Windows, from
/// <c>RuntimeInformation.Windows.cs</c> (dotnet/runtime v10.0.0): the machine's own
/// architecture as IsWow64Process2 gives it, and where Windows has no IsWow64Process2, as
/// GetNativeSystemInfo gives it. .NET Framework's answer can name the architecture an
/// emulated process runs as, so an x64 copy on an Arm64 PC can read X64.
/// </summary>
internal static class FrameworkRuntimeInformation
{
    private const ushort ImageFileMachineArmNt = 0x01C4;
    private const ushort ImageFileMachineAmd64 = 0x8664;
    private const ushort ImageFileMachineArm64 = 0xAA64;
    private const ushort ImageFileMachineI386 = 0x014C;

    private const ushort ProcessorArchitectureIntel = 0;
    private const ushort ProcessorArchitectureArm = 5;
    private const ushort ProcessorArchitectureAmd64 = 9;
    private const ushort ProcessorArchitectureArm64 = 12;

    private static int s_osArchPlusOne;

    /// <summary>The architecture of the machine this process runs on, read once and kept.</summary>
    public static Architecture OSArchitecture
    {
        get
        {
            var osArch = s_osArchPlusOne - 1;
            if (osArch < 0)
            {
                osArch = (int)Read();
                s_osArchPlusOne = osArch + 1;
            }
            return (Architecture)osArch;
        }
    }

    private static Architecture Read()
    {
        try
        {
            return Kernel32.IsWow64Process2(Kernel32.GetCurrentProcess(), out _, out var nativeMachine)
                ? MapMachineConstant(nativeMachine)
                : RuntimeInformation.ProcessArchitecture;
        }
        catch (EntryPointNotFoundException)
        {
            Kernel32.GetNativeSystemInfo(out var info);
            return Map(info.wProcessorArchitecture);
        }
    }

    /// <summary>An IMAGE_FILE_MACHINE value as an architecture; one not named is the process's own.</summary>
    internal static Architecture MapMachineConstant(ushort machine) => machine switch
    {
        ImageFileMachineArmNt => Architecture.Arm,
        ImageFileMachineAmd64 => Architecture.X64,
        ImageFileMachineArm64 => Architecture.Arm64,
        ImageFileMachineI386 => Architecture.X86,
        _ => RuntimeInformation.ProcessArchitecture,
    };

    /// <summary>A PROCESSOR_ARCHITECTURE value as an architecture; one not named is X86.</summary>
    internal static Architecture Map(ushort processorArchitecture) => processorArchitecture switch
    {
        ProcessorArchitectureArm64 => Architecture.Arm64,
        ProcessorArchitectureArm => Architecture.Arm,
        ProcessorArchitectureAmd64 => Architecture.X64,
        _ => Architecture.X86,
    };
}
