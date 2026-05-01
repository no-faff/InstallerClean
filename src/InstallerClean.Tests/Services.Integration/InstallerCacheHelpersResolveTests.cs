using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

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

    [Fact]
    public void IsInstallerFolderOrChild_resolves_junction_pointing_into_installer_folder()
    {
        var installerFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");
        if (!Directory.Exists(installerFolder))
            return;

        var junctionPath = Path.Combine(Path.GetTempPath(),
            "ic-test-junction-" + Guid.NewGuid());
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe",
                $"/c mklink /J \"{junctionPath}\" \"{installerFolder}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p is null) return;
            p.WaitForExit();
            // Without elevation mklink refuses. Skip silently rather than fail
            // because CI commonly runs non-elevated.
            if (p.ExitCode != 0 || !Directory.Exists(junctionPath))
                return;

            Assert.True(InstallerCacheHelpers.IsInstallerFolderOrChild(junctionPath));
        }
        finally
        {
            try { if (Directory.Exists(junctionPath)) Directory.Delete(junctionPath); }
            catch { }
        }
    }
}
