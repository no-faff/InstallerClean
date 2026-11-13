using System;
using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// Substitutes the real installer cache folder into any resx value that names
/// it. A string writes <c>{InstallerFolder}</c> and every consumer, C# and XAML
/// alike, receives the resolved path, because the substitution happens in
/// <c>Strings.Get</c>, the one door every resource lookup comes through.
/// </summary>
/// <remarks>
/// The folder is <c>%SystemRoot%\Installer</c>, resolved once for the whole app
/// by <see cref="InstallerCacheHelpers.InstallerFolder"/>. A literal C: path in a
/// string names a folder the app is not touching on any machine whose Windows
/// lives elsewhere.
///
/// A named token rather than a <c>{0}</c>: several of these strings already
/// carry positional arguments, and the token is spent before
/// <c>string.Format</c> ever sees the value, so the two systems never meet.
///
/// Ordering is load-bearing on the drawn side. <c>InstallerPathText.KeepWhole</c>
/// binds the path's seams against a line break and can only do that to a real
/// path, so substituting here, ahead of it, is what keeps that working.
///
/// <c>Cli.EventLogMoveDestinationInsideInstaller</c> takes no token. It is
/// machine-read and held English at the emit site, and it names the folder in
/// words rather than spelling a path, so it reads the same on every machine
/// without resolving one; its resx comment carries the rest.
/// </remarks>
internal static class InstallerFolderToken
{
    internal const string Token = "{InstallerFolder}";

    /// <summary>
    /// Returns <paramref name="value"/> with the token replaced by the resolved
    /// folder, or unchanged when it carries none, which is nearly every string
    /// and the reason for the test rather than an unconditional Replace.
    /// </summary>
    /// <remarks>
    /// string's own Contains and Replace, which compare ordinally on .NET Framework as on
    /// .NET 10. Every string of a copy's .NET Framework 4.8 message comes through here, so
    /// what this names keeps to the .NET Framework NetFrameworkCheck's remarks name. The
    /// overloads taking a StringComparison are FrameworkStringExtensions' and search through
    /// System.Memory's spans, whose own code needs more.
    /// </remarks>
    internal static string Resolve(string value) =>
        value.Contains(Token)
            ? value.Replace(Token, InstallerCacheHelpers.InstallerFolder)
            : value;
}
