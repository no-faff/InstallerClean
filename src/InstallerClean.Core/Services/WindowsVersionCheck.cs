using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using Microsoft.Win32;

namespace InstallerClean.Services;

/// <summary>
/// Whether this PC runs Windows 10 version 1607 or later, read where a copy starts, and the
/// message a copy shows before it ends where it does not.
/// </summary>
/// <remarks>
/// The setup refuses to install below Windows 10 version 1607 (MinVersion 10.0.14393 in
/// InstallerClean.iss). The portable and the command line are run without it, so each checks
/// the build itself at its start, before <see cref="NetFrameworkCheck"/>: Windows 10 versions
/// 1507 and 1511 cannot take .NET Framework 4.8, so the message naming it would send the user
/// after something that does not install there. Windows 7 and 8.1 can take 4.8 and are
/// refused here as the setup refuses them.
///
/// The build is the CurrentBuild value Windows keeps under
/// HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion, a string such as "7601" or "14393",
/// or CurrentBuildNumber beside it where CurrentBuild does not read as a number.
/// A program's compatibility settings change what the version APIs report to it and leave
/// this value alone. Everything here runs before the .NET Framework 4.8 check, so it is held
/// to what <see cref="NetFrameworkCheck"/>'s remarks set out: only what .NET Framework 4.0
/// has. It reads the value through <see cref="RegistryData"/>, as the Release read does.
/// </remarks>
internal static class WindowsVersionCheck
{
    /// <summary>The build of Windows 10 version 1607, the oldest Windows a copy runs on.</summary>
    internal const int MinimumBuild = 14393;

    internal const string CurrentVersionKeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    internal const string CurrentBuildValueName = "CurrentBuild";
    internal const string CurrentBuildNumberValueName = "CurrentBuildNumber";

    /// <summary>
    /// Whether the CurrentBuild value <paramref name="currentBuild"/>, null where it could not
    /// be read, says the PC runs Windows older than Windows 10 version 1607.
    /// </summary>
    /// <remarks>
    /// Only a build read as a whole number below <see cref="MinimumBuild"/> refuses. A value
    /// that could not be read, is missing or is not a number says nothing about the PC, and
    /// the copy starts.
    /// </remarks>
    internal static bool Refuses(string? currentBuild) =>
        int.TryParse(currentBuild, NumberStyles.None, CultureInfo.InvariantCulture, out var build)
            && build < MinimumBuild;

    /// <summary>
    /// The message to show the user before the copy ends, in the language the strings are read
    /// in. Both copies show the same sentence.
    /// </summary>
    internal static string Message => Strings.FrameworkCheck.Startup_NeedsWindows10Version1607;

    /// <summary>
    /// The CurrentBuild value as the string it is, or where that is not a whole number the
    /// CurrentBuildNumber value beside it, which Windows writes with the same build. Null where
    /// the key is missing or the read fails, and whatever CurrentBuildNumber holds where
    /// CurrentBuild does not read.
    /// </summary>
    internal static string? CurrentBuildOfThisPc()
    {
        // Any failure here answers null, so the copy goes on to the .NET Framework 4.8 check
        // rather than past it.
        try
        {
            using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hive.OpenSubKeyEarned(CurrentVersionKeyPath);
            if (key is null) return null;
            var build = RegistryData.ValueOf(key, CurrentBuildValueName, doNotExpand: true) as string;
            return int.TryParse(build, NumberStyles.None, CultureInfo.InvariantCulture, out _)
                ? build
                : RegistryData.ValueOf(key, CurrentBuildNumberValueName, doNotExpand: true) as string;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
