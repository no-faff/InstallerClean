namespace InstallerClean.Services;

/// <summary>What stops a copy at its start, where anything does.</summary>
internal enum StartRefusal
{
    None,

    /// <summary>Windows older than Windows 10 version 1607 (<see cref="WindowsVersionCheck"/>).</summary>
    OlderWindows,

    /// <summary>No .NET Framework 4.8 (<see cref="NetFrameworkCheck"/>).</summary>
    NoNetFramework48,
}

/// <summary>
/// The checks each copy makes before anything else it runs, and the message it shows where
/// one refuses. The window and the command line both call these first.
/// </summary>
/// <remarks>
/// Windows is checked first: Windows 10 versions 1507 and 1511 cannot take .NET Framework
/// 4.8, so on them the .NET Framework message would ask for something that does not install.
/// Deciding and wording are apart so the window can set the language picked in the app between
/// the two (<see cref="Helpers.PickedLanguageAtStart"/>). Everything here is held to what
/// <see cref="NetFrameworkCheck"/>'s remarks set out.
/// </remarks>
internal static class StartChecks
{
    /// <summary>What stops this PC running the copy, or <see cref="StartRefusal.None"/>.</summary>
    internal static StartRefusal OfThisPc() =>
        WindowsVersionCheck.Refuses(WindowsVersionCheck.CurrentBuildOfThisPc()) ? StartRefusal.OlderWindows
        : NetFrameworkCheck.Refuses(NetFrameworkCheck.ReleaseOfThisPc()) ? StartRefusal.NoNetFramework48
        : StartRefusal.None;

    /// <summary>
    /// The message for <paramref name="refusal"/>, which is not <see cref="StartRefusal.None"/>,
    /// in the language the strings are read in.
    /// </summary>
    internal static string Message(StartRefusal refusal, bool commandLine) =>
        refusal == StartRefusal.OlderWindows ? WindowsVersionCheck.Message : NetFrameworkCheck.Message(commandLine);
}
