using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// Production <see cref="IInstallerInProgressMarker"/>: asks the real filesystem for
/// the attributes of <c>inprogressinstallinfo.ipi</c> in the Installer folder.
///
/// IT TAKES NO <c>IFileSystem</c>. What it answers decides whether the app acts on the
/// cache at all, and a test double is not what should be deciding whether Windows
/// Installer is part-way through something. Tests that need the answer fixed
/// substitute the interface.
/// </summary>
internal sealed class InstallerInProgressMarker : IInstallerInProgressMarker
{
    /// <summary>The marker's file name, as Windows Installer writes it.</summary>
    internal const string FileName = "inprogressinstallinfo.ipi";

    // HRESULT_FROM_WIN32 of ERROR_SHARING_VIOLATION and ERROR_LOCK_VIOLATION, which is
    // what an IOException from the framework carries for either.
    private const int SharingViolation = unchecked((int)0x80070020);
    private const int LockViolation = unchecked((int)0x80070021);

    private readonly string _path;

    /// <summary>Production constructor: the marker in the machine's Installer folder.</summary>
    public InstallerInProgressMarker()
        : this(InstallerCacheHelpers.InstallerFolder) { }

    /// <summary>Test constructor: the marker in <paramref name="folder"/>.</summary>
    internal InstallerInProgressMarker(string folder)
    {
        _path = PathText.Combine(folder, FileName);
    }

    /// <inheritdoc />
    public InstallerInProgressMarkerReading Read()
    {
        // The attributes rather than File.Exists, which answers false for a path it
        // could not read as well as for one with nothing at it. Each failure is
        // classified by what it establishes, and one the three readings do not name
        // is left to propagate.
        try
        {
            File.GetAttributes(_path);
            return InstallerInProgressMarkerReading.Present;
        }
        catch (FileNotFoundException)
        {
            return InstallerInProgressMarkerReading.Absent;
        }
        catch (DirectoryNotFoundException)
        {
            return InstallerInProgressMarkerReading.Absent;
        }
        catch (UnauthorizedAccessException)
        {
            return InstallerInProgressMarkerReading.AccessRefused;
        }
        catch (IOException ex) when (ex.HResult is SharingViolation or LockViolation)
        {
            // Another process holds the file open without sharing it, so it is there.
            return InstallerInProgressMarkerReading.Present;
        }
    }
}
