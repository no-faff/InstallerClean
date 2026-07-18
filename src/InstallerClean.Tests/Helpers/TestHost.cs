namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Facts about the machine the suite is running on, for tests that need to pose
/// the production code a question the real filesystem has to answer. The
/// containment and reparse gates consult the REAL filesystem whatever
/// <c>IFileSystem</c> is injected, by design, so a MockFileSystem entry cannot
/// reach them at all.
/// </summary>
internal static class TestHost
{
    /// <summary>
    /// The first drive letter with nothing mounted on it, or null if every one
    /// is in use. A path on such a letter is one the resolver can find no
    /// existing ancestor for, which is how a test reaches "this path was never
    /// expanded" without needing a junction or a permission it may not have.
    ///
    /// Computed rather than hardcoded: a test that assumed Z: was free would
    /// silently start passing for the wrong reason on a host that has a Z:
    /// drive, which is the failure mode a test cannot report on itself.
    /// </summary>
    internal static char? FirstUnmountedDriveLetter()
    {
        var used = DriveInfo.GetDrives()
            .Select(d => char.ToUpperInvariant(d.Name.FirstOrDefault()))
            .ToHashSet();
        for (var c = 'D'; c <= 'Z'; c++)
            if (!used.Contains(c)) return c;
        return null;
    }
}
