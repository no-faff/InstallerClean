using System.Runtime.InteropServices;
using System.Text;
using InstallerClean.Interop.Native;

namespace InstallerClean.Compatibility;

/// <summary>
/// <see cref="RuntimeInformation.OSArchitecture"/> as .NET 10 answers it on Windows, from
/// <c>RuntimeInformation.Windows.cs</c> (dotnet/runtime v10.0.0): where kernel32 exports
/// IsWow64Process2, the machine's own architecture as it gives it, or the process's
/// architecture where it fails; where kernel32 does not, the architecture GetNativeSystemInfo
/// gives. .NET Framework's answer is GetNativeSystemInfo's alone, which an emulated process
/// reads as the architecture it is emulated as, so an x64 copy on an Arm64 PC reads X64.
/// </summary>
internal static class FrameworkRuntimeInformation
{
    private const ushort ImageFileMachineArmNt = 0x01C4;
    private const ushort ImageFileMachineAmd64 = 0x8664;
    private const ushort ImageFileMachineArm64 = 0xAA64;
    private const ushort ImageFileMachineI386 = 0x014C;

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
                osArch = (int)Read(HasIsWow64Process2() ? NativeMachineOfThisProcess : null, NativeProcessorArchitecture);
                s_osArchPlusOne = osArch + 1;
            }
            return (Architecture)osArch;
        }
    }

    /// <summary>IsWow64Process2 for this process: whether it succeeded, and the machine's own type.</summary>
    internal delegate bool NativeMachineReader(out ushort nativeMachine);

    /// <summary>
    /// The machine's architecture from <paramref name="isWow64Process2"/>, which is null where
    /// kernel32 does not export it, and otherwise from
    /// <paramref name="nativeProcessorArchitecture"/>, GetNativeSystemInfo's
    /// wProcessorArchitecture.
    /// </summary>
    internal static Architecture Read(NativeMachineReader? isWow64Process2, Func<ushort> nativeProcessorArchitecture)
    {
        if (isWow64Process2 is null)
            return Map(nativeProcessorArchitecture());

        return isWow64Process2(out var nativeMachine)
            ? MapMachineConstant(nativeMachine)
            : RuntimeInformation.ProcessArchitecture;
    }

    /// <summary>
    /// Whether the kernel32 this process has loaded exports IsWow64Process2. Calling the
    /// declaration where it does not would throw EntryPointNotFoundException, so the
    /// export is looked up first, as .NET 10 looks it up.
    /// </summary>
    internal static bool HasIsWow64Process2()
    {
        var kernel32 = Kernel32.GetModuleHandle("kernel32.dll");
        return kernel32 != IntPtr.Zero
            && Kernel32.GetProcAddress(kernel32, Encoding.ASCII.GetBytes("IsWow64Process2\0")) != IntPtr.Zero;
    }

    private static bool NativeMachineOfThisProcess(out ushort nativeMachine) =>
        Kernel32.IsWow64Process2(Kernel32.GetCurrentProcess(), out _, out nativeMachine);

    private static ushort NativeProcessorArchitecture()
    {
        Kernel32.GetNativeSystemInfo(out var info);
        return info.wProcessorArchitecture;
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
