using InstallerClean.Interop.Native;

namespace InstallerClean.Compatibility;

/// <summary>
/// The ICU Windows ships, loaded once for the process as .NET 10 loads it on Windows
/// (dotnet/runtime v10.0.0: <c>GlobalizationMode.Windows.cs</c>'s <c>UseNls</c> and
/// <c>LoadIcu</c>, and in <c>src/native/libs/System.Globalization.Native</c>,
/// <c>pal_icushim.c</c>'s <c>FindICULibs</c>): icu.dll from System32, which Windows 10
/// version 1903 and later carry and which holds the whole of ICU, or icuuc.dll with
/// icuin.dll beside it, ICU's common library and its
/// internationalisation library. <see cref="Loaded"/> is null where .NET 10 uses NLS
/// instead: neither is there, or DOTNET_SYSTEM_GLOBALIZATION_USENLS asks for NLS.
/// </summary>
/// <remarks>
/// .NET 10 ends the process where ICU loads and a function it needs is missing. Here each
/// user asks for the functions it calls, and where one is missing that user takes NLS:
/// an order or a spoken name is never worth ending the app over.
/// </remarks>
internal sealed class WindowsIcu
{
    /// <summary>The variable .NET 10 reads to use NLS where Windows has ICU.</summary>
    internal const string UseNlsVariable = "DOTNET_SYSTEM_GLOBALIZATION_USENLS";

    private static readonly Lazy<WindowsIcu?> Process = new(() =>
        AsksForNls(Environment.GetEnvironmentVariable(UseNlsVariable)) ? null : Load());

    private WindowsIcu(IntPtr common, IntPtr internationalisation)
    {
        Common = common;
        Internationalisation = internationalisation;
    }

    /// <summary>The ICU this process uses, or null where it uses NLS.</summary>
    public static WindowsIcu? Loaded => Process.Value;

    /// <summary>ICU's common library: icu.dll, or icuuc.dll.</summary>
    public IntPtr Common { get; }

    /// <summary>ICU's internationalisation library, which collates: icu.dll, or icuin.dll.</summary>
    public IntPtr Internationalisation { get; }

    /// <summary>
    /// Whether <paramref name="value"/>, the variable's value or null where it is not set,
    /// asks for NLS, as .NET 10's <c>AppContextConfigHelper.GetBooleanConfig</c> reads it:
    /// "1" or "true" in any case does, and anything else leaves ICU in use, the app
    /// setting no runtime switch for it.
    /// </summary>
    internal static bool AsksForNls(string? value) =>
        value is not null && (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The address of <paramref name="name"/> in <paramref name="library"/>, or zero where
    /// it is not exported. Windows' ICU exports its functions under their plain names.
    /// </summary>
    public static IntPtr Export(IntPtr library, string name) =>
        Kernel32.GetProcAddress(library, System.Text.Encoding.ASCII.GetBytes(name + "\0"));

    private static WindowsIcu? Load()
    {
        var whole = Kernel32.LoadLibraryEx("icu.dll", IntPtr.Zero, Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32);
        if (whole != IntPtr.Zero)
            return new WindowsIcu(whole, whole);

        var common = Kernel32.LoadLibraryEx("icuuc.dll", IntPtr.Zero, Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32);
        if (common == IntPtr.Zero)
            return null;
        var internationalisation = Kernel32.LoadLibraryEx("icuin.dll", IntPtr.Zero, Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32);
        return internationalisation == IntPtr.Zero ? null : new WindowsIcu(common, internationalisation);
    }
}
