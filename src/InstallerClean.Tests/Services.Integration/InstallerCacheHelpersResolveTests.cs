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
        var root = TestTemp.NewFolder("ic-resolve-");
        try
        {
            var uncreated = Path.Combine(root, "not-yet", "still-not-yet");

            var resolved = InstallerCacheHelpers.ResolveFinalPath(uncreated);

            Assert.EndsWith(Path.Combine("not-yet", "still-not-yet"), resolved);
            Assert.StartsWith(root.Substring(0, 3), resolved, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception) when (TestTemp.KeepOnFailure(root)) { }
        finally
        {
            TestTemp.Delete(root);
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
        var root = TestTemp.NewFolder("ic-tryresolve-");
        try
        {
            Assert.True(InstallerCacheHelpers.TryResolveFinalPath(
                Path.Combine(root, "not-yet.msi"), out _));
        }
        catch (Exception) when (TestTemp.KeepOnFailure(root)) { }
        finally
        {
            TestTemp.Delete(root);
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
        // without ever opening anything. What comes back is a spelling rather
        // than a location, and the answer says so.
        var unmounted = Helpers.TestHost.FirstUnmountedDriveLetter();
        if (unmounted is null)
            return; // every letter is in use on this host; nothing to pose the question with

        Assert.False(InstallerCacheHelpers.TryResolveFinalPath(
            $@"{unmounted}:\Windows\Installer\x.msi", out _));
    }

    [Fact]
    public void ResolveFinalPathOutcome_answers_OpenRefused_below_a_folder_that_refuses_the_open()
    {
        // System Volume Information on the system drive is there to be seen, and
        // the access list Windows gives it grants full control to SYSTEM and
        // nothing to anybody else, administrators included. So the open is
        // refused whether or not the test runs elevated, and the walk stops at
        // it. The path below it is handed back as GetFullPath spells it.
        var folder = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "System Volume Information");
        Assert.True(Directory.Exists(folder), $"{folder} is not there to be seen on this host");
        var path = Path.Combine(folder, "x.msi");

        var outcome = InstallerCacheHelpers.ResolveFinalPathOutcome(path, out var resolved);

        Assert.Equal(PathResolution.OpenRefused, outcome);
        Assert.Equal(Path.GetFullPath(path), resolved);
    }

    [Fact]
    public void CheckSafeToRemove_reports_Unproven_rather_than_Refused_when_it_cannot_look()
    {
        // The distinction the two action services turn their per-file error on.
        // An embedded null defeats the attribute read, so neither "it is a
        // symlink" nor "it is outside the cache" was established, and neither
        // may be reported.
        Assert.Equal(CandidateGuard.RemovalSafety.Unproven,
            CandidateGuard.CheckSafeToRemove("C:\\Windows\\Installer\\bad\0name.msi", InstallerCacheRoot.Resolve()));
    }

    [Fact]
    public void CheckSafeToRemove_reports_Unproven_when_the_ROOT_could_not_be_resolved()
    {
        // Nothing about the file is in doubt here: it resolves. The ROOT does
        // not, and a fully resolved candidate cannot match an unexpanded root
        // however legitimately it sits in the folder, so the comparison never
        // took place and "this file is outside the cache" would be a cause
        // nothing showed. The file is kept either way, which is the invariant
        // this must not touch; only the sentence the user reads changes.
        var unmounted = TestHost.FirstUnmountedDriveLetter();
        if (unmounted is null)
            return; // every letter is in use on this host

        var root = InstallerCacheRoot.Resolve($@"{unmounted}:\Windows\Installer");

        Assert.Equal(CandidateGuard.RemovalSafety.Unproven,
            CandidateGuard.CheckSafeToRemove(Path.Combine(Path.GetTempPath(), "resolves-fine.msi"), root));
    }

    [Fact]
    public void CheckSafeToRemove_reports_Refused_for_a_path_outside_the_cache()
    {
        // The other arm, so the test above is not passing merely because
        // everything answers Unproven.
        Assert.Equal(CandidateGuard.RemovalSafety.Refused,
            CandidateGuard.CheckSafeToRemove(Path.Combine(Path.GetTempPath(), "outside.msi"), InstallerCacheRoot.Resolve()));
    }

    [Fact]
    public void CheckSafeToRemove_accepts_a_file_sitting_directly_in_the_cache_root()
    {
        // The baseline for the four refusals below: every real candidate, from
        // the walk or from a LocalPackage value, is a bare filename at the root.
        WithCacheSandbox(root =>
            Assert.Equal(CandidateGuard.RemovalSafety.Safe,
                CandidateGuard.CheckSafeToRemove(Path.Combine(root, "12ab34.msi"), InstallerCacheRoot.Resolve(root))));
    }

    [Fact]
    public void CheckSafeToRemove_refuses_a_file_one_level_inside_the_cache()
    {
        // SECURITY.md tells a reporter the app never acts inside a subfolder.
        // Only a corrupt registration can name one, since the walk does not
        // descend, but "only a corrupt registration" is precisely the case the
        // source guard exists for.
        WithCacheSandbox(root =>
            Assert.Equal(CandidateGuard.RemovalSafety.Refused,
                CandidateGuard.CheckSafeToRemove(Path.Combine(root, "sub", "12ab34.msi"), InstallerCacheRoot.Resolve(root))));
    }

    [Fact]
    public void CheckSafeToRemove_refuses_a_file_in_the_PatchCache_subtree()
    {
        // The subtree this is really about: $PatchCache$ holds the patch
        // engine's baseline payload copies, which the scan puts out of scope by
        // never descending, and which nothing may then delete by another route.
        WithCacheSandbox(root =>
            Assert.Equal(CandidateGuard.RemovalSafety.Refused,
                CandidateGuard.CheckSafeToRemove(
                    Path.Combine(root, "$PatchCache$", "Managed", "0AB", "1.0.0", "patch.msp"),
                    InstallerCacheRoot.Resolve(root))));
    }

    [Fact]
    public void CheckSafeToRemove_refuses_the_cache_root_itself()
    {
        // A candidate is a file. The folder answering Safe would have made the
        // guard's own name a lie, whatever the services do with it afterwards.
        WithCacheSandbox(root =>
            Assert.Equal(CandidateGuard.RemovalSafety.Refused,
                CandidateGuard.CheckSafeToRemove(root, InstallerCacheRoot.Resolve(root))));
    }

    [Fact]
    public void CheckSafeToRemove_refuses_a_sibling_folder_whose_name_starts_with_the_root()
    {
        // The prefix trap: "...\ic-cache-<id>-notreally\x.msi" starts with the
        // root's spelling and is not under it. The parent comparison is exact,
        // so the trap cannot fire, and this is here to keep it that way.
        WithCacheSandbox(root =>
            Assert.Equal(CandidateGuard.RemovalSafety.Refused,
                CandidateGuard.CheckSafeToRemove(root + "-notreally" + Path.DirectorySeparatorChar + "x.msi",
                    InstallerCacheRoot.Resolve(root))));
    }

    // ---- Whether a source's package would be a file directly in the Installer folder ----

    [Fact]
    public void NamesAFileDirectlyInInstallerFolder_answers_true_for_the_root_spelling_without_reading_an_identity()
    {
        WithCacheSandbox(root =>
        {
            var reader = new ScriptedFileIdentities();

            Assert.True(InstallerCacheHelpers.NamesAFileDirectlyInInstallerFolder(
                Path.Combine(root, "c.msi"), InstallerCacheRoot.Resolve(root), reader));
            Assert.Empty(reader.Reads);
        });
    }

    [Fact]
    public void NamesAFileDirectlyInInstallerFolder_answers_true_for_another_spelling_of_the_root()
    {
        // The Installer folder reached through this PC's own admin share resolves to its
        // UNC spelling. The other folder here stands in for that spelling: the reader says
        // it opens as the root.
        WithTwoFolders((root, other) =>
        {
            var package = Path.Combine(other, "c.msi");
            var reader = new ScriptedFileIdentities();
            reader.Opens(FolderOf(package), 7);
            reader.Opens(InstallerCacheRoot.Resolve(root).Resolved, 7);

            Assert.True(InstallerCacheHelpers.NamesAFileDirectlyInInstallerFolder(
                package, InstallerCacheRoot.Resolve(root), reader));
        });
    }

    [Fact]
    public void NamesAFileDirectlyInInstallerFolder_answers_false_for_another_folder()
    {
        WithTwoFolders((root, other) =>
        {
            var package = Path.Combine(other, "c.msi");
            var reader = new ScriptedFileIdentities();
            reader.Opens(FolderOf(package), 7);
            reader.Opens(InstallerCacheRoot.Resolve(root).Resolved, 8);

            Assert.False(InstallerCacheHelpers.NamesAFileDirectlyInInstallerFolder(
                package, InstallerCacheRoot.Resolve(root), reader));
        });
    }

    [Fact]
    public void NamesAFileDirectlyInInstallerFolder_answers_false_for_a_folder_that_is_not_there()
    {
        // The root is there, so a folder that is not cannot be it. The root is never
        // opened for this answer.
        WithTwoFolders((root, other) =>
        {
            var package = Path.Combine(other, "gone", "c.msi");
            var reader = new ScriptedFileIdentities();
            reader.Answers(FolderOf(package), FileIdentityRead.NamesNothing);

            Assert.False(InstallerCacheHelpers.NamesAFileDirectlyInInstallerFolder(
                package, InstallerCacheRoot.Resolve(root), reader));
        });
    }

    [Theory]
    [InlineData(FileIdentityRead.OpenRefused)]
    [InlineData(FileIdentityRead.IdentityUnavailable)]
    [InlineData(FileIdentityRead.Faulted)]
    [InlineData(FileIdentityRead.NotAPath)]
    public void NamesAFileDirectlyInInstallerFolder_answers_null_where_the_folder_will_not_identify(
        FileIdentityRead outcome)
    {
        WithTwoFolders((root, other) =>
        {
            var package = Path.Combine(other, "c.msi");
            var reader = new ScriptedFileIdentities();
            reader.Answers(FolderOf(package), outcome);

            Assert.Null(InstallerCacheHelpers.NamesAFileDirectlyInInstallerFolder(
                package, InstallerCacheRoot.Resolve(root), reader));
        });
    }

    [Fact]
    public void NamesAFileDirectlyInInstallerFolder_answers_null_where_the_root_will_not_identify()
    {
        WithTwoFolders((root, other) =>
        {
            var package = Path.Combine(other, "c.msi");
            var reader = new ScriptedFileIdentities();
            reader.Opens(FolderOf(package), 7);
            reader.Answers(InstallerCacheRoot.Resolve(root).Resolved, FileIdentityRead.OpenRefused);

            Assert.Null(InstallerCacheHelpers.NamesAFileDirectlyInInstallerFolder(
                package, InstallerCacheRoot.Resolve(root), reader));
        });
    }

    [Fact]
    public void NamesAFileDirectlyInInstallerFolder_answers_false_for_a_volume_root_without_reading_an_identity()
    {
        // A volume root has no parent, so it is no file in any folder.
        WithCacheSandbox(root =>
        {
            var reader = new ScriptedFileIdentities();

            Assert.False(InstallerCacheHelpers.NamesAFileDirectlyInInstallerFolder(
                Path.GetPathRoot(root)!, InstallerCacheRoot.Resolve(root), reader));
            Assert.Empty(reader.Reads);
        });
    }

    [Fact]
    public void NamesAFileDirectlyInInstallerFolder_reads_two_real_folders_as_two_folders()
    {
        // The production reader, which opens a folder as it opens a file.
        WithTwoFolders((root, other) =>
            Assert.False(InstallerCacheHelpers.NamesAFileDirectlyInInstallerFolder(
                Path.Combine(other, "c.msi"), InstallerCacheRoot.Resolve(root))));
    }

    // ---- Whether a folder is the Installer folder itself ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IsTheInstallerFolder_answers_true_for_the_root_spelling_without_reading_an_identity(bool trailingSeparator)
    {
        WithCacheSandbox(root =>
        {
            var reader = new ScriptedFileIdentities();
            var folder = trailingSeparator ? root + Path.DirectorySeparatorChar : root;

            Assert.True(InstallerCacheHelpers.IsTheInstallerFolder(folder, InstallerCacheRoot.Resolve(root), reader));
            Assert.Empty(reader.Reads);
        });
    }

    [Fact]
    public void IsTheInstallerFolder_answers_true_for_another_spelling_of_the_root()
    {
        WithTwoFolders((root, other) =>
        {
            var reader = new ScriptedFileIdentities();
            reader.Opens(InstallerCacheHelpers.ResolveFinalPath(other), 7);
            reader.Opens(InstallerCacheRoot.Resolve(root).Resolved, 7);

            Assert.True(InstallerCacheHelpers.IsTheInstallerFolder(other, InstallerCacheRoot.Resolve(root), reader));
        });
    }

    [Fact]
    public void IsTheInstallerFolder_answers_false_for_another_folder_and_opens_nothing_inside_it()
    {
        WithTwoFolders((root, other) =>
        {
            File.WriteAllText(Path.Combine(other, "package.msi"), "");
            var reader = new ScriptedFileIdentities();
            reader.Opens(InstallerCacheHelpers.ResolveFinalPath(other), 7);
            reader.Opens(InstallerCacheRoot.Resolve(root).Resolved, 8);

            Assert.False(InstallerCacheHelpers.IsTheInstallerFolder(other, InstallerCacheRoot.Resolve(root), reader));
            Assert.Equal(
                [InstallerCacheHelpers.ResolveFinalPath(other), InstallerCacheRoot.Resolve(root).Resolved], reader.Reads);
        });
    }

    [Fact]
    public void IsTheInstallerFolder_answers_false_for_a_folder_that_is_not_there()
    {
        WithTwoFolders((root, other) =>
        {
            var folder = Path.Combine(other, "gone");
            var reader = new ScriptedFileIdentities();
            reader.Answers(InstallerCacheHelpers.ResolveFinalPath(folder), FileIdentityRead.NamesNothing);

            Assert.False(InstallerCacheHelpers.IsTheInstallerFolder(folder, InstallerCacheRoot.Resolve(root), reader));
        });
    }

    [Theory]
    [InlineData(FileIdentityRead.OpenRefused)]
    [InlineData(FileIdentityRead.IdentityUnavailable)]
    [InlineData(FileIdentityRead.Faulted)]
    [InlineData(FileIdentityRead.NotAPath)]
    public void IsTheInstallerFolder_answers_null_where_the_folder_will_not_identify(FileIdentityRead outcome)
    {
        WithTwoFolders((root, other) =>
        {
            var reader = new ScriptedFileIdentities();
            reader.Answers(InstallerCacheHelpers.ResolveFinalPath(other), outcome);

            Assert.Null(InstallerCacheHelpers.IsTheInstallerFolder(other, InstallerCacheRoot.Resolve(root), reader));
        });
    }

    [Fact]
    public void IsTheInstallerFolder_answers_null_where_the_root_will_not_identify()
    {
        WithTwoFolders((root, other) =>
        {
            var reader = new ScriptedFileIdentities();
            reader.Opens(InstallerCacheHelpers.ResolveFinalPath(other), 7);
            reader.Answers(InstallerCacheRoot.Resolve(root).Resolved, FileIdentityRead.OpenRefused);

            Assert.Null(InstallerCacheHelpers.IsTheInstallerFolder(other, InstallerCacheRoot.Resolve(root), reader));
        });
    }

    [Fact]
    public void IsTheInstallerFolder_opens_a_volume_root_with_its_separator()
    {
        // Without it the root's spelling would name the current folder on that drive.
        WithCacheSandbox(root =>
        {
            var cacheRoot = InstallerCacheRoot.Resolve(root);
            var volumeRoot = Path.GetPathRoot(cacheRoot.Resolved)!;
            var reader = new ScriptedFileIdentities();
            reader.Opens(volumeRoot, 7);
            reader.Opens(cacheRoot.Resolved, 8);

            Assert.False(InstallerCacheHelpers.IsTheInstallerFolder(volumeRoot, cacheRoot, reader));
            Assert.Equal([volumeRoot, cacheRoot.Resolved], reader.Reads);
        });
    }

    [Fact]
    public void IsTheInstallerFolder_reads_two_real_folders_as_two_folders()
    {
        // The production reader, which opens a folder as it opens a file.
        WithTwoFolders((root, other) =>
            Assert.False(InstallerCacheHelpers.IsTheInstallerFolder(other, InstallerCacheRoot.Resolve(root))));
    }

    /// <summary>
    /// The folder <see cref="InstallerCacheHelpers.NamesAFileDirectlyInInstallerFolder"/>
    /// opens for <paramref name="path"/>: the parent of its resolved spelling.
    /// </summary>
    private static string FolderOf(string path) =>
        Path.GetDirectoryName(InstallerCacheHelpers.ResolveFinalPath(path))!;

    /// <summary>
    /// <see cref="WithCacheSandbox"/> with a second real directory beside the root, which
    /// is not the root.
    /// </summary>
    private static void WithTwoFolders(Action<string, string> body) =>
        WithCacheSandbox(root =>
        {
            var other = root + "-other";
            Directory.CreateDirectory(other);
            try
            {
                body(root, other);
            }
            finally
            {
                try { Directory.Delete(other, recursive: true); } catch { }
            }
        });

    /// <summary>
    /// Runs <paramref name="body"/> against a real throwaway directory standing
    /// in for <c>C:\Windows\Installer</c>. Real, not mocked: the containment
    /// gate resolves through the kernel whatever <c>IFileSystem</c> is injected,
    /// which is the property that stops a MockFileSystem entry reaching it.
    /// </summary>
    private static void WithCacheSandbox(Action<string> body)
    {
        var root = TestTemp.NewFolder("ic-cache-");
        try
        {
            body(root);
        }
        catch (Exception) when (TestTemp.KeepOnFailure(root)) { }
        finally
        {
            TestTemp.Delete(root);
        }
    }
}
