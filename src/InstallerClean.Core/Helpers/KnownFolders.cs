using System.Runtime.InteropServices;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// The Windows folders the app builds its paths from: the Windows folder,
/// <c>%LOCALAPPDATA%</c>, <c>%ProgramData%</c> and the two Program Files folders.
/// Each answer is the one <see cref="Environment.GetFolderPath(Environment.SpecialFolder)"/>
/// gives for the same folder, asked through this assembly's own shell32 import, which
/// loads from System32.
/// </summary>
/// <remarks>
/// The call is the one .NET makes: SHGetKnownFolderPath for the same known-folder GUID,
/// with no flags and no token. Where it returns anything but S_OK, the answer is the
/// environment variable .NET falls back to for that folder, or an empty string where the
/// variable is not set. The string Windows hands back is freed whether the call succeeded
/// or not. A folder not listed here throws, so a new caller adds its folder with the GUID
/// and the variable .NET uses for it.
/// </remarks>
internal static class KnownFolders
{
    /// <summary>Asks Windows for a known folder: the HRESULT, and the string it allocated.</summary>
    internal delegate int Query(in Guid folderId, uint flags, IntPtr token, out IntPtr path);

    private static readonly Guid FOLDERID_Windows = new("F38BF404-1D43-42F2-9305-67DE0B28FC23");
    private static readonly Guid FOLDERID_LocalAppData = new("F1B32785-6FBA-4FCF-9D55-7B8E7F157091");
    private static readonly Guid FOLDERID_ProgramData = new("62AB5D82-FDC1-4DC3-A9DD-070D1D495D97");
    private static readonly Guid FOLDERID_ProgramFiles = new("905E63B6-C1BF-494E-B29C-65B732D3D21A");
    private static readonly Guid FOLDERID_ProgramFilesX86 = new("7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E");

    /// <summary>
    /// The path of <paramref name="folder"/>, which is one of Windows,
    /// LocalApplicationData, CommonApplicationData, ProgramFiles and ProgramFilesX86.
    /// Throws <see cref="ArgumentOutOfRangeException"/> for any other folder.
    /// </summary>
    internal static string GetPath(Environment.SpecialFolder folder) =>
        GetPath(folder, Shell32.SHGetKnownFolderPath, Ole32.CoTaskMemFree, Environment.GetEnvironmentVariable);

    /// <summary>
    /// Test seam: the same, with the call to Windows, the free and the variable read
    /// supplied by the caller.
    /// </summary>
    internal static string GetPath(
        Environment.SpecialFolder folder, Query query, Action<IntPtr> free, Func<string, string?> variable)
    {
        // Two variables rather than a tuple: the window's start message reads the language picked
        // in the app through here (PickedLanguageAtStart), on a PC whose .NET Framework may be
        // older than 4.7, which brought ValueTuple.
        Guid folderId;
        string fallbackVariable;
        switch (folder)
        {
            case Environment.SpecialFolder.Windows:
                folderId = FOLDERID_Windows; fallbackVariable = "windir"; break;
            case Environment.SpecialFolder.LocalApplicationData:
                folderId = FOLDERID_LocalAppData; fallbackVariable = "LOCALAPPDATA"; break;
            case Environment.SpecialFolder.CommonApplicationData:
                folderId = FOLDERID_ProgramData; fallbackVariable = "ProgramData"; break;
            case Environment.SpecialFolder.ProgramFiles:
                folderId = FOLDERID_ProgramFiles; fallbackVariable = "ProgramFiles"; break;
            case Environment.SpecialFolder.ProgramFilesX86:
                folderId = FOLDERID_ProgramFilesX86; fallbackVariable = "ProgramFiles(x86)"; break;
            default:
                throw new ArgumentOutOfRangeException(nameof(folder), folder, "Not a folder the app asks Windows for.");
        }

        var path = IntPtr.Zero;
        try
        {
            // S_OK only: S_FALSE and every error take the variable, as .NET does.
            if (query(in folderId, 0, IntPtr.Zero, out path) == 0)
                return Marshal.PtrToStringUni(path)!;
        }
        finally
        {
            if (path != IntPtr.Zero)
                free(path);
        }

        return variable(fallbackVariable) ?? string.Empty;
    }
}
