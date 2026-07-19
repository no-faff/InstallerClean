using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Unit tests for InstallerCacheHelpers.PruneEmptySubdirectories against an
/// in-memory <see cref="MockFileSystem"/>. The prune takes the caller's
/// IFileSystem, which is what makes these runnable at all: a helper that
/// reached past the injection seam to the real folder could only be tested by
/// pruning the host's own cache, and would silently do the same on every move
/// and delete unit test that reached it.
///
/// The folder is not a parameter, so the tests seed the mock at
/// <see cref="InstallerCacheHelpers.InstallerFolder"/>, the same path
/// production reads.
/// </summary>
public class PruneEmptySubdirectoriesTests
{
    private static readonly string Root = InstallerCacheHelpers.InstallerFolder;

    [Fact]
    public void Deletes_an_empty_subdirectory()
    {
        var fs = new MockFileSystem();
        var empty = fs.Path.Combine(Root, "empty");
        fs.Directory.CreateDirectory(empty);

        InstallerCacheHelpers.PruneEmptySubdirectories(fs);

        Assert.False(fs.Directory.Exists(empty));
        Assert.True(fs.Directory.Exists(Root));
    }

    [Fact]
    public void Collapses_a_nested_empty_tree_in_one_pass()
    {
        var fs = new MockFileSystem();
        var outer = fs.Path.Combine(Root, "outer");
        fs.Directory.CreateDirectory(fs.Path.Combine(outer, "inner", "deepest"));

        InstallerCacheHelpers.PruneEmptySubdirectories(fs);

        // One pass collapses the whole tree because the walk is ordered by
        // descending path LENGTH, not by depth: a child's path is strictly
        // longer than its parent's, so every directory is reached after its own
        // descendants and is already empty by the time its turn comes.
        Assert.False(fs.Directory.Exists(outer));
        Assert.True(fs.Directory.Exists(Root));
    }

    [Fact]
    public void Keeps_a_subdirectory_that_still_holds_a_file()
    {
        var fs = new MockFileSystem();
        var keep = fs.Path.Combine(Root, "keep");
        var file = fs.Path.Combine(keep, "patch.msp");
        fs.AddFile(file, new MockFileData("payload"));
        fs.Directory.CreateDirectory(fs.Path.Combine(Root, "drop"));

        InstallerCacheHelpers.PruneEmptySubdirectories(fs);

        Assert.True(fs.File.Exists(file));
        Assert.False(fs.Directory.Exists(fs.Path.Combine(Root, "drop")));
    }

    [Fact]
    public void Returns_without_throwing_when_the_installer_folder_is_absent()
    {
        // The hermetic case every other MockFileSystem-backed test relies on: a
        // mock with no installer folder, so the move and delete unit tests reach
        // the prune and it does nothing at all.
        var fs = new MockFileSystem();

        var ex = Record.Exception(() => InstallerCacheHelpers.PruneEmptySubdirectories(fs));

        Assert.Null(ex);
    }

    [Fact]
    public void Respects_an_already_cancelled_token()
    {
        var fs = new MockFileSystem();
        var empty = fs.Path.Combine(Root, "empty");
        fs.Directory.CreateDirectory(empty);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() =>
            InstallerCacheHelpers.PruneEmptySubdirectories(fs, cts.Token));
        Assert.True(fs.Directory.Exists(empty));
    }
}
