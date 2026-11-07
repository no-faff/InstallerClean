using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The folders a Move makes on the way to its destination: which ones creating the
/// destination would make, and the removal of them again, which takes a folder only
/// while it is empty and stops at the first that is there and will not go.
/// </summary>
public class CreatedFoldersTests
{
    private readonly MockFileSystem _fs = new();

    // A full path with no short name in it: Windows resolves it as text and nothing on the
    // disk is looked at, so the folders exist only in the mock.
    private readonly string _root = @"C:\ic-created";
    private readonly IDestinationGuard _guard;

    public CreatedFoldersTests()
    {
        _fs.AddDirectory(_root);
        _guard = new ByPathDestinationGuard(_fs);
    }

    private string Folder(params string[] parts) => Path.Combine([_root, .. parts]);

    [Fact]
    public void The_destination_and_each_missing_folder_above_it_are_listed_destination_first()
    {
        var missing = CreatedFolders.MissingOnPath(_fs, Folder("a", "b", "c"));

        Assert.Equal([Folder("a", "b", "c"), Folder("a", "b"), Folder("a")], missing);
    }

    [Fact]
    public void A_missing_destination_in_a_folder_that_is_there_is_the_only_one_listed()
    {
        Assert.Equal([Folder("a")], CreatedFolders.MissingOnPath(_fs, Folder("a")));
    }

    [Fact]
    public void A_destination_that_is_there_lists_nothing()
    {
        _fs.AddDirectory(Folder("a", "b"));

        Assert.Empty(CreatedFolders.MissingOnPath(_fs, Folder("a", "b")));
    }

    [Fact]
    public void A_folder_above_the_destination_that_is_there_is_not_listed()
    {
        _fs.AddDirectory(Folder("a"));

        Assert.Equal([Folder("a", "b")], CreatedFolders.MissingOnPath(_fs, Folder("a", "b")));
    }

    [Fact]
    public void A_trailing_separator_on_the_destination_lists_each_folder_once()
    {
        var withSeparator = Folder("a", "b") + Path.DirectorySeparatorChar;

        Assert.Equal([Folder("a", "b"), Folder("a")], CreatedFolders.MissingOnPath(_fs, withSeparator));
    }

    [Fact]
    public void A_destination_spelled_with_a_parent_reference_lists_the_folders_the_create_makes()
    {
        var spelled = Path.Combine(Folder("a", "y"), "..", "z");

        Assert.Equal([Folder("a", "z"), Folder("a")], CreatedFolders.MissingOnPath(_fs, spelled));
    }

    [Fact]
    public void A_destination_that_cannot_be_spelled_as_a_full_path_lists_nothing()
    {
        Assert.Empty(CreatedFolders.MissingOnPath(_fs, Folder("a") + "\0"));
    }

    [Fact]
    public void A_drive_root_is_never_listed()
    {
        var nothingThere = Substitute.For<IFileSystem>();
        var directory = Substitute.For<IDirectory>();
        nothingThere.Directory.Returns(directory);
        var destination = Folder("a");

        var missing = CreatedFolders.MissingOnPath(nothingThere, destination);

        Assert.Equal(destination, missing[0]);
        Assert.All(missing, folder => Assert.False(string.IsNullOrEmpty(Path.GetDirectoryName(folder))));
    }

    private IDestinationGuard GuardAnswering(params (string Folder, FolderCreation Answer)[] answers)
    {
        var guard = Substitute.For<IDestinationGuard>();
        foreach (var (folder, answer) in answers)
            guard.TryCreateFolder(folder).Returns(answer);
        return guard;
    }

    [Fact]
    public void Every_folder_the_guard_made_is_listed_destination_first_and_the_destination_is_not_created_again()
    {
        var guard = GuardAnswering(
            (Folder("a"), FolderCreation.Made), (Folder("a", "b"), FolderCreation.Made),
            (Folder("a", "b", "c"), FolderCreation.Made));
        var made = new List<string>();

        CreatedFolders.Create(_fs, guard, Folder("a", "b", "c"), made);

        Assert.Equal([Folder("a", "b", "c"), Folder("a", "b"), Folder("a")], made);
        Assert.False(_fs.Directory.Exists(Folder("a", "b", "c")));
    }

    [Fact]
    public void The_folders_are_created_top_most_first()
    {
        var guard = Substitute.For<IDestinationGuard>();
        guard.TryCreateFolder(Arg.Any<string>()).Returns(FolderCreation.Made);

        CreatedFolders.Create(_fs, guard, Folder("a", "b", "c"), new List<string>());

        Received.InOrder(() =>
        {
            guard.TryCreateFolder(Folder("a"));
            guard.TryCreateFolder(Folder("a", "b"));
            guard.TryCreateFolder(Folder("a", "b", "c"));
        });
    }

    [Fact]
    public void A_folder_the_guard_found_already_there_is_not_listed_though_it_looked_absent()
    {
        var guard = GuardAnswering(
            (Folder("a"), FolderCreation.Made), (Folder("a", "b"), FolderCreation.AlreadyThere),
            (Folder("a", "b", "c"), FolderCreation.Made));
        var made = new List<string>();

        CreatedFolders.Create(_fs, guard, Folder("a", "b", "c"), made);

        Assert.Equal([Folder("a", "b", "c")], made);
    }

    [Fact]
    public void A_folder_found_already_there_at_the_top_leaves_the_folders_below_it_listed()
    {
        var guard = GuardAnswering(
            (Folder("a"), FolderCreation.AlreadyThere), (Folder("a", "b"), FolderCreation.Made),
            (Folder("a", "b", "c"), FolderCreation.Made));
        var made = new List<string>();

        CreatedFolders.Create(_fs, guard, Folder("a", "b", "c"), made);

        Assert.Equal([Folder("a", "b", "c"), Folder("a", "b")], made);
    }

    [Fact]
    public void A_destination_found_already_there_lists_nothing()
    {
        var guard = GuardAnswering(
            (Folder("a"), FolderCreation.Made), (Folder("a", "b"), FolderCreation.AlreadyThere));
        var made = new List<string>();

        CreatedFolders.Create(_fs, guard, Folder("a", "b"), made);

        Assert.Empty(made);
    }

    [Fact]
    public void A_destination_that_is_there_before_the_create_asks_nothing_of_the_guard()
    {
        _fs.AddDirectory(Folder("a", "b"));
        var guard = Substitute.For<IDestinationGuard>();
        var made = new List<string>();

        CreatedFolders.Create(_fs, guard, Folder("a", "b"), made);

        Assert.Empty(made);
        guard.DidNotReceiveWithAnyArgs().TryCreateFolder(default!);
    }

    [Fact]
    public void A_failed_create_ends_the_creating_and_lists_only_the_folders_made_above_it()
    {
        var guard = GuardAnswering(
            (Folder("a"), FolderCreation.Made), (Folder("a", "b"), FolderCreation.Failed));
        var made = new List<string>();

        CreatedFolders.Create(_fs, guard, Folder("a", "b", "c"), made);

        Assert.Equal([Folder("a")], made);
        guard.DidNotReceive().TryCreateFolder(Folder("a", "b", "c"));
    }

    [Fact]
    public void A_failed_create_of_the_top_most_folder_lists_nothing()
    {
        var guard = GuardAnswering((Folder("a"), FolderCreation.Failed));
        var made = new List<string>();

        CreatedFolders.Create(_fs, guard, Folder("a", "b"), made);

        Assert.Empty(made);
    }

    [Fact]
    public void A_failed_create_is_followed_by_the_destination_being_created_through_the_filesystem()
    {
        var guard = Substitute.For<IDestinationGuard>();
        guard.TryCreateFolder(Arg.Any<string>()).Returns(FolderCreation.Failed);

        CreatedFolders.Create(_fs, guard, Folder("a", "b"), new List<string>());

        Assert.True(_fs.Directory.Exists(Folder("a", "b")));
    }

    [Fact]
    public void A_failed_create_that_the_filesystem_also_refuses_throws_with_the_folders_made_listed()
    {
        var guard = GuardAnswering(
            (Folder("a"), FolderCreation.Made), (Folder("a", "b"), FolderCreation.Failed));
        var directory = Substitute.For<IDirectory>();
        directory.Exists(_root).Returns(true);
        directory.When(d => d.CreateDirectory(Arg.Any<string>())).Do(_ => throw new IOException("refused"));
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Returns(directory);
        fileSystem.Path.Returns(_fs.Path);
        var made = new List<string>();

        Assert.Throws<IOException>(() => CreatedFolders.Create(fileSystem, guard, Folder("a", "b"), made));

        Assert.Equal([Folder("a")], made);
    }

    [Fact]
    public void A_guard_that_throws_leaves_the_folders_made_before_it_listed()
    {
        var guard = Substitute.For<IDestinationGuard>();
        guard.TryCreateFolder(Folder("a")).Returns(FolderCreation.Made);
        guard.TryCreateFolder(Folder("a", "b")).Returns(_ => throw new InvalidOperationException("refused"));
        var made = new List<string>();

        Assert.Throws<InvalidOperationException>(() => CreatedFolders.Create(_fs, guard, Folder("a", "b"), made));

        Assert.Equal([Folder("a")], made);
    }

    [Fact]
    public void Folders_already_in_the_list_are_left_where_they_are()
    {
        var guard = GuardAnswering(
            (Folder("a"), FolderCreation.AlreadyThere), (Folder("a", "b"), FolderCreation.Made));
        var made = new List<string> { "earlier" };

        CreatedFolders.Create(_fs, guard, Folder("a", "b"), made);

        Assert.Equal(["earlier", Folder("a", "b")], made);
    }

    [Fact]
    public void Folders_that_are_empty_are_removed_in_the_order_given()
    {
        _fs.AddDirectory(Folder("a", "b", "c"));

        CreatedFolders.RemoveWhileEmpty(_fs, _guard, [Folder("a", "b", "c"), Folder("a", "b"), Folder("a")]);

        Assert.False(_fs.Directory.Exists(Folder("a")));
        Assert.True(_fs.Directory.Exists(_root));
    }

    [Fact]
    public void A_folder_holding_a_file_is_left_and_keeps_every_folder_above_it()
    {
        _fs.AddDirectory(Folder("a", "b", "c"));
        _fs.AddFile(Path.Combine(Folder("a", "b"), "kept.txt"), new MockFileData("x"));

        CreatedFolders.RemoveWhileEmpty(_fs, _guard, [Folder("a", "b", "c"), Folder("a", "b"), Folder("a")]);

        Assert.False(_fs.Directory.Exists(Folder("a", "b", "c")));
        Assert.True(_fs.File.Exists(Path.Combine(Folder("a", "b"), "kept.txt")));
        Assert.True(_fs.Directory.Exists(Folder("a")));
    }

    [Fact]
    public void A_folder_that_is_no_longer_there_is_passed_over()
    {
        _fs.AddDirectory(Folder("a", "b"));

        CreatedFolders.RemoveWhileEmpty(_fs, _guard, [Folder("a", "b", "c"), Folder("a", "b"), Folder("a")]);

        Assert.False(_fs.Directory.Exists(Folder("a")));
    }

    [Fact]
    public void A_removal_that_throws_ends_without_throwing()
    {
        var guard = Substitute.For<IDestinationGuard>();
        guard.TryRemoveEmptyFolder(Arg.Any<string>(), Arg.Any<string>()).Returns(_ => throw new InvalidOperationException("refused"));

        var thrown = Record.Exception(() => CreatedFolders.RemoveWhileEmpty(_fs, guard, [Folder("a")]));

        Assert.Null(thrown);
    }

    [Fact]
    public void A_removal_that_throws_ends_the_removal_at_that_folder()
    {
        var guard = Substitute.For<IDestinationGuard>();
        guard.TryRemoveEmptyFolder(Arg.Any<string>(), Arg.Any<string>()).Returns(_ => throw new InvalidOperationException("refused"));

        CreatedFolders.RemoveWhileEmpty(_fs, guard, [Folder("a", "z"), Folder("a")]);

        guard.Received(1).TryRemoveEmptyFolder(Arg.Any<string>(), Arg.Any<string>());
        guard.Received(1).TryRemoveEmptyFolder(Folder("a", "z"), _root);
    }

    [Fact]
    public void Each_folder_is_removed_with_the_folder_above_the_top_most_as_the_existing_ancestor()
    {
        var guard = Substitute.For<IDestinationGuard>();
        guard.TryRemoveEmptyFolder(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        CreatedFolders.RemoveWhileEmpty(_fs, guard, [Folder("a", "b", "c"), Folder("a", "b"), Folder("a")]);

        guard.Received(1).TryRemoveEmptyFolder(Folder("a", "b", "c"), _root);
        guard.Received(1).TryRemoveEmptyFolder(Folder("a", "b"), _root);
        guard.Received(1).TryRemoveEmptyFolder(Folder("a"), _root);
        guard.ReceivedWithAnyArgs(3).TryRemoveEmptyFolder(default!, default!);
    }

    [Fact]
    public void No_folders_to_remove_asks_nothing_of_the_guard()
    {
        var guard = Substitute.For<IDestinationGuard>();

        CreatedFolders.RemoveWhileEmpty(_fs, guard, []);

        guard.DidNotReceiveWithAnyArgs().TryRemoveEmptyFolder(default!, default!);
    }
}
