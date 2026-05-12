using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

public class InstallerCacheHelpersResolveTests
{
    [Fact]
    public void ResolveFinalPath_returns_existing_path_unchanged_shape()
    {
        var temp = Path.GetTempPath().TrimEnd('\\');

        var resolved = InstallerCacheHelpers.ResolveFinalPath(temp);

        Assert.False(string.IsNullOrWhiteSpace(resolved));
        Assert.True(Directory.Exists(resolved) || Directory.Exists(resolved.TrimEnd('\\')));
    }

    [Fact]
    public void ResolveFinalPath_returns_value_for_nonexistent_subpath()
    {
        var path = Path.Combine(Path.GetTempPath(),
            "installerclean-nonexistent-" + Guid.NewGuid(), "deep", "leaf");

        var resolved = InstallerCacheHelpers.ResolveFinalPath(path);

        Assert.False(string.IsNullOrWhiteSpace(resolved));
    }

    [Fact]
    public void ResolveFinalPath_walks_up_to_existing_ancestor_and_reattaches_suffix()
    {
        // Create an existing directory, then ask for a deep subpath below it
        // that does not exist. ResolveFinalPath should resolve the existing
        // ancestor (canonicalising any symlinks) and reattach the missing
        // suffix so the caller sees the expected path shape.
        var root = Path.Combine(Path.GetTempPath(), "ic-resolve-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        try
        {
            var uncreated = Path.Combine(root, "not-yet", "still-not-yet");

            var resolved = InstallerCacheHelpers.ResolveFinalPath(uncreated);

            Assert.EndsWith(Path.Combine("not-yet", "still-not-yet"), resolved);
            Assert.StartsWith(root.Substring(0, 3), resolved, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void ResolveFinalPath_walk_up_to_drive_root_keeps_separator()
    {
        // When the existing-ancestor walk lands at the drive root
        // ("C:\"), the suffix attachment must keep the trailing
        // separator. Trimming it produces drive-relative paths like
        // "C:NewFolder\Sub" once the separator-less suffix concatenates.
        var systemDrive = Path.GetPathRoot(Environment.SystemDirectory);
        if (string.IsNullOrEmpty(systemDrive)) return;
        var unborn = Path.Combine(systemDrive, "ic-resolve-nonexistent-" + Guid.NewGuid(), "leaf");

        var resolved = InstallerCacheHelpers.ResolveFinalPath(unborn);

        Assert.False(string.IsNullOrEmpty(resolved));
        Assert.StartsWith(systemDrive, resolved, StringComparison.OrdinalIgnoreCase);
        // Sanity: no drive-relative shape ("C:foo" without backslash).
        Assert.NotEqual(systemDrive[0] + ":" + Path.GetFileName(unborn), resolved);
    }

    [Fact]
    public void ResolveFinalPath_empty_input_does_not_throw()
    {
        var ex = Record.Exception(() => InstallerCacheHelpers.ResolveFinalPath(string.Empty));

        Assert.Null(ex);
    }

    [Fact]
    public void IsInstallerFolderOrChild_returns_false_for_empty()
    {
        Assert.False(InstallerCacheHelpers.IsInstallerFolderOrChild(string.Empty));
    }
}
