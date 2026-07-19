using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

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

    [Fact]
    public void TryResolveFinalPath_reports_success_for_a_real_folder()
    {
        // The baseline the source-side gate depends on: an ordinary existing
        // path IS proven, so requiring proof does not refuse everything.
        Assert.True(InstallerCacheHelpers.TryResolveFinalPath(
            Path.GetTempPath().TrimEnd('\\'), out var resolved));
        Assert.False(string.IsNullOrWhiteSpace(resolved));
    }

    [Fact]
    public void TryResolveFinalPath_reports_success_for_a_path_below_a_real_folder()
    {
        // The walk-up case, which is most of what the source gate meets: the
        // leaf need not exist, because an ancestor being resolved is what shows
        // no junction stands between the cache root and the file.
        var root = Path.Combine(Path.GetTempPath(), "ic-tryresolve-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        try
        {
            Assert.True(InstallerCacheHelpers.TryResolveFinalPath(
                Path.Combine(root, "not-yet.msi"), out _));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void TryResolveFinalPath_reports_failure_for_a_path_it_cannot_normalise()
    {
        // An embedded null: GetFullPath refuses it, so nothing was expanded and
        // the caller is told so rather than handed the raw string as if it had
        // been. The string still comes back, because the destination gates want
        // the best effort and only the source gate wants the proof.
        const string bad = "C:\\Windows\\Installer\\bad\0name.msi";

        Assert.False(InstallerCacheHelpers.TryResolveFinalPath(bad, out var resolved));
        Assert.Equal(bad, resolved);
    }

    [Fact]
    public void TryResolveFinalPath_reports_failure_when_no_ancestor_exists()
    {
        // Nothing mounted on the letter, so the walk runs out of ancestors
        // without ever opening anything. Degraded to a plain GetFullPath, this
        // is the shape that made a spelling look like a location.
        var unmounted = Helpers.TestHost.FirstUnmountedDriveLetter();
        if (unmounted is null)
            return; // every letter is in use on this host; nothing to pose the question with

        Assert.False(InstallerCacheHelpers.TryResolveFinalPath(
            $@"{unmounted}:\Windows\Installer\x.msi", out _));
    }

    [Fact]
    public void CheckSafeToRemove_reports_Unproven_rather_than_Refused_when_it_cannot_look()
    {
        // The distinction the two action services turn their per-file error on.
        // An embedded null defeats the attribute read, so neither "it is a
        // symlink" nor "it is outside the cache" was established, and neither
        // may be reported.
        Assert.Equal(CandidateGuard.RemovalSafety.Unproven,
            CandidateGuard.CheckSafeToRemove("C:\\Windows\\Installer\\bad\0name.msi"));
    }

    [Fact]
    public void CheckSafeToRemove_reports_Refused_for_a_path_outside_the_cache()
    {
        // The other arm, so the test above is not passing merely because
        // everything answers Unproven.
        Assert.Equal(CandidateGuard.RemovalSafety.Refused,
            CandidateGuard.CheckSafeToRemove(Path.Combine(Path.GetTempPath(), "outside.msi")));
    }
}
