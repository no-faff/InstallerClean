using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Interop.Native;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The by-path destination guard over a substituted filesystem, which the Move fixtures
/// use in place of the handle-based guard. These hold what each of its three methods
/// answers, the failure code it reports and the filesystem calls it makes. The folder
/// removal is the one-argument <c>Directory.Delete(path)</c>, which NSubstitute treats as
/// a different call from the two-argument form.
/// </summary>
public class ByPathDestinationGuardTests
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IDirectory _directory = Substitute.For<IDirectory>();
    private readonly IFile _file = Substitute.For<IFile>();
    private readonly ByPathDestinationGuard _guard;

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ic-guard-test");
    private readonly string _copy = Path.Combine(Path.GetTempPath(), "ic-guard-test", "a.msi");

    public ByPathDestinationGuardTests()
    {
        // MockFileSystem's Path is a working implementation; a bare substitute returns an
        // empty string from GetRandomFileName.
        _fileSystem.Path.Returns(new MockFileSystem().Path);
        _fileSystem.Directory.Returns(_directory);
        _fileSystem.File.Returns(_file);
        _guard = new ByPathDestinationGuard(_fileSystem);
    }

    /// <summary>
    /// The refusals a filesystem gives for a folder or a file it will not take, by exception
    /// name, each with the HResult .NET raises it with for a failed Windows call. A theory row
    /// carries the name, which the test framework can serialise, so each refusal is its own
    /// test case.
    /// </summary>
    private static readonly Dictionary<string, Func<Exception>> RefusalFactories = new()
    {
        [nameof(IOException)] = () => new IOException("refused", Win32HResult(Kernel32.ERROR_SHARING_VIOLATION)),
        [nameof(UnauthorizedAccessException)] = () => new UnauthorizedAccessException("refused"),
        [nameof(DirectoryNotFoundException)] = () => new DirectoryNotFoundException("refused"),
        [nameof(PathTooLongException)] = () => new PathTooLongException("refused"),
        [nameof(NotSupportedException)] = () => new NotSupportedException("refused"),
        [nameof(ArgumentException)] = () => new ArgumentException("refused"),
    };

    /// <summary>
    /// The Windows error number each refusal is recorded as: the number the exception stands
    /// for, and the code for an unidentified failure where .NET raised it on its own account.
    /// </summary>
    private static readonly Dictionary<string, int> RecordedCodes = new()
    {
        [nameof(IOException)] = Kernel32.ERROR_SHARING_VIOLATION,
        [nameof(UnauthorizedAccessException)] = Kernel32.ERROR_ACCESS_DENIED,
        [nameof(DirectoryNotFoundException)] = Kernel32.ERROR_PATH_NOT_FOUND,
        [nameof(PathTooLongException)] = ErrorFilenameExcedRange,
        [nameof(NotSupportedException)] = Kernel32.ERROR_UNIDENTIFIED_ERROR,
        [nameof(ArgumentException)] = Kernel32.ERROR_UNIDENTIFIED_ERROR,
    };

    /// <summary>"The filename or extension is too long": what a PathTooLongException stands for.</summary>
    private const int ErrorFilenameExcedRange = 206;

    private static int Win32HResult(int code) => unchecked((int)0x80070000) | code;

    public static TheoryData<string> RefusalNames => new(RefusalFactories.Keys);

    private static Exception NewRefusal(string name) => RefusalFactories[name]();

    [Theory]
    [MemberData(nameof(RefusalNames))]
    public void Each_refusal_name_builds_the_exception_it_names(string refusalName)
    {
        Assert.Equal(refusalName, NewRefusal(refusalName).GetType().Name);
    }

    [Fact]
    public void A_folder_that_is_not_there_is_created_and_answers_made()
    {
        Assert.Equal(FolderCreation.Made, _guard.TryCreateFolder("X"));

        _directory.Received(1).CreateDirectory("X");
    }

    [Fact]
    public void A_folder_that_is_there_answers_already_there_and_is_not_created()
    {
        _directory.Exists("X").Returns(true);

        Assert.Equal(FolderCreation.AlreadyThere, _guard.TryCreateFolder("X"));

        _directory.DidNotReceive().CreateDirectory(Arg.Any<string>());
    }

    [Theory]
    [MemberData(nameof(RefusalNames))]
    public void A_create_the_filesystem_refuses_answers_failed(string refusalName)
    {
        var refusal = NewRefusal(refusalName);
        _directory.When(d => d.CreateDirectory("X")).Do(_ => throw refusal);

        Assert.Equal(FolderCreation.Failed, _guard.TryCreateFolder("X"));
    }

    [Fact]
    public void An_emptied_folder_is_removed_with_the_one_argument_Delete()
    {
        Assert.True(_guard.TryRemoveEmptyFolder("X", "Y"));

        _directory.Received(1).Delete("X");
        _directory.DidNotReceive().Delete(Arg.Any<string>(), Arg.Any<bool>());
    }

    [Theory]
    [MemberData(nameof(RefusalNames))]
    public void A_folder_the_filesystem_will_not_remove_answers_false(string refusalName)
    {
        var refusal = NewRefusal(refusalName);
        _directory.When(d => d.Delete("X")).Do(_ => throw refusal);

        Assert.False(_guard.TryRemoveEmptyFolder("X", "Y"));
    }

    [Fact]
    public void A_folder_that_takes_a_file_and_lets_it_be_read_back_passes_the_probe()
    {
        string? written = null;
        byte[]? payload = null;
        _file.When(f => f.WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(call =>
            {
                written = call.ArgAt<string>(0);
                payload = call.ArgAt<byte[]>(1);
            });

        Assert.True(_guard.TryProbeWritable(_folder, out var failureCode, out var probeRefusal));

        Assert.Equal(0, failureCode);
        Assert.Equal(WriteProbeRefusal.None, probeRefusal);
        Assert.NotNull(written);
        Assert.Equal(_folder, Path.GetDirectoryName(written));
        Assert.NotNull(payload);
        Assert.Empty(payload);
        Received.InOrder(() =>
        {
            _file.WriteAllBytes(written!, Arg.Any<byte[]>());
            _file.ReadAllBytes(written!);
            _file.Delete(written!);
        });
    }

    [Theory]
    [MemberData(nameof(RefusalNames))]
    public void A_folder_that_refuses_the_probe_file_fails_the_probe_with_the_refusals_Win32_code(string refusalName)
    {
        var refusal = NewRefusal(refusalName);
        _file.When(f => f.WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>())).Do(_ => throw refusal);

        Assert.False(_guard.TryProbeWritable(_folder, out var failureCode, out var probeRefusal));

        // Only a write refused access is the folder refusing the account the right to add
        // the file; any other failure of the write is named as a failure of the create.
        Assert.Equal(RecordedCodes[refusalName], failureCode);
        Assert.Equal(
            refusal is UnauthorizedAccessException ? WriteProbeRefusal.CreateRefusedAccess : WriteProbeRefusal.CreateFailed,
            probeRefusal);
        _file.DidNotReceive().ReadAllBytes(Arg.Any<string>());
        _file.DidNotReceive().Delete(Arg.Any<string>());
    }

    [Theory]
    [MemberData(nameof(RefusalNames))]
    public void A_probe_file_that_cannot_be_read_back_fails_the_probe_and_is_still_deleted(string refusalName)
    {
        var refusal = NewRefusal(refusalName);
        _file.ReadAllBytes(Arg.Any<string>()).Returns(_ => throw refusal);

        Assert.False(_guard.TryProbeWritable(_folder, out var failureCode, out var probeRefusal));

        // Only a read refused access is the folder declining to give the file back; any
        // other failure of the read is named as a failure of the read-back.
        Assert.Equal(RecordedCodes[refusalName], failureCode);
        Assert.Equal(
            refusal is UnauthorizedAccessException ? WriteProbeRefusal.ReadBackRefusedAccess : WriteProbeRefusal.ReadBackFailed,
            probeRefusal);
        _file.Received(1).Delete(Arg.Any<string>());
    }

    [Theory]
    [MemberData(nameof(RefusalNames))]
    public void A_folder_whose_probe_file_cannot_be_deleted_passes_the_probe(string refusalName)
    {
        var refusal = NewRefusal(refusalName);
        _file.When(f => f.Delete(Arg.Any<string>())).Do(_ => throw refusal);

        Assert.True(_guard.TryProbeWritable(_folder, out var failureCode, out var probeRefusal));

        Assert.Equal(0, failureCode);
        Assert.Equal(WriteProbeRefusal.None, probeRefusal);
        _file.Received(1).Delete(Arg.Any<string>());
    }

    [Fact]
    public void An_exception_standing_for_no_error_is_recorded_as_unidentified_never_as_zero()
    {
        _file.When(f => f.WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(_ => throw new IOException("refused", Win32HResult(0)));

        Assert.False(_guard.TryProbeWritable(_folder, out var failureCode, out _));

        Assert.Equal(Kernel32.ERROR_UNIDENTIFIED_ERROR, failureCode);
    }

    [Fact]
    public void A_Win32Exception_from_the_filesystem_is_recorded_as_its_own_error_number()
    {
        _file.When(f => f.WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(_ => throw new System.ComponentModel.Win32Exception(Kernel32.ERROR_NOT_READY));

        Assert.False(_guard.TryProbeWritable(_folder, out var failureCode, out _));

        Assert.Equal(Kernel32.ERROR_NOT_READY, failureCode);
    }

    [Fact]
    public void A_copy_that_is_not_there_is_reported_gone_and_nothing_is_deleted()
    {
        _file.Exists(_copy).Returns(false);
        _directory.Exists(_copy).Returns(false);

        Assert.True(_guard.TryDiscardCopy(_copy, out var failureCode));

        Assert.Equal(0, failureCode);
        _file.Received(1).Exists(_copy);
        _directory.Received(1).Exists(_copy);
        _file.DidNotReceive().Delete(Arg.Any<string>());
        _file.DidNotReceive().SetAttributes(Arg.Any<string>(), Arg.Any<FileAttributes>());
    }

    [Fact]
    public void A_folder_at_the_copy_s_name_is_left_and_recorded_as_a_folder()
    {
        _file.Exists(_copy).Returns(false);
        _directory.Exists(_copy).Returns(true);

        Assert.False(_guard.TryDiscardCopy(_copy, out var failureCode));

        Assert.Equal(Kernel32.ERROR_DIRECTORY_NOT_SUPPORTED, failureCode);
        _file.DidNotReceive().Delete(Arg.Any<string>());
        _directory.DidNotReceive().Delete(Arg.Any<string>());
    }

    [Fact]
    public void A_copy_that_is_there_is_deleted_without_touching_its_attributes()
    {
        _file.Exists(_copy).Returns(true);
        _file.GetAttributes(_copy).Returns(FileAttributes.Archive);

        Assert.True(_guard.TryDiscardCopy(_copy, out var failureCode));

        Assert.Equal(0, failureCode);
        _file.Received(1).Delete(_copy);
        _file.DidNotReceive().SetAttributes(Arg.Any<string>(), Arg.Any<FileAttributes>());
    }

    [Fact]
    public void A_read_only_copy_has_the_attribute_cleared_and_then_is_deleted()
    {
        _file.Exists(_copy).Returns(true);
        _file.GetAttributes(_copy).Returns(FileAttributes.ReadOnly | FileAttributes.Archive);

        Assert.True(_guard.TryDiscardCopy(_copy, out _));

        Received.InOrder(() =>
        {
            _file.SetAttributes(_copy, FileAttributes.Archive);
            _file.Delete(_copy);
        });
    }

    [Theory]
    [MemberData(nameof(RefusalNames))]
    public void A_read_only_copy_whose_attribute_will_not_clear_fails_the_discard_and_is_not_deleted(string refusalName)
    {
        var refusal = NewRefusal(refusalName);
        _file.Exists(_copy).Returns(true);
        _file.GetAttributes(_copy).Returns(FileAttributes.ReadOnly);
        _file.When(f => f.SetAttributes(_copy, Arg.Any<FileAttributes>())).Do(_ => throw refusal);

        Assert.False(_guard.TryDiscardCopy(_copy, out var failureCode));

        Assert.Equal(RecordedCodes[refusalName], failureCode);
        _file.DidNotReceive().Delete(Arg.Any<string>());
    }

    [Fact]
    public void A_copy_whose_attributes_cannot_be_read_fails_the_discard_and_is_not_deleted()
    {
        var refusal = new UnauthorizedAccessException("refused");
        _file.Exists(_copy).Returns(true);
        _file.GetAttributes(_copy).Returns(_ => throw refusal);

        Assert.False(_guard.TryDiscardCopy(_copy, out var failureCode));

        Assert.Equal(Kernel32.ERROR_ACCESS_DENIED, failureCode);
        _file.DidNotReceive().Delete(Arg.Any<string>());
    }

    [Fact]
    public void A_copy_whose_presence_cannot_be_asked_fails_the_discard_and_is_not_deleted()
    {
        var refusal = new IOException("refused", Win32HResult(Kernel32.ERROR_SHARING_VIOLATION));
        _file.Exists(_copy).Returns(_ => throw refusal);

        Assert.False(_guard.TryDiscardCopy(_copy, out var failureCode));

        Assert.Equal(Kernel32.ERROR_SHARING_VIOLATION, failureCode);
        _file.DidNotReceive().Delete(Arg.Any<string>());
    }

    [Theory]
    [MemberData(nameof(RefusalNames))]
    public void A_copy_the_filesystem_will_not_delete_fails_the_discard_with_the_refusals_Win32_code(string refusalName)
    {
        var refusal = NewRefusal(refusalName);
        _file.Exists(_copy).Returns(true);
        _file.GetAttributes(_copy).Returns(FileAttributes.Archive);
        _file.When(f => f.Delete(_copy)).Do(_ => throw refusal);

        Assert.False(_guard.TryDiscardCopy(_copy, out var failureCode));

        Assert.Equal(RecordedCodes[refusalName], failureCode);
        _file.DidNotReceive().SetAttributes(Arg.Any<string>(), Arg.Any<FileAttributes>());
    }

    [Fact]
    public void A_read_only_copy_the_filesystem_will_not_delete_has_its_attribute_put_back()
    {
        var refusal = new IOException("refused", Win32HResult(Kernel32.ERROR_SHARING_VIOLATION));
        _file.Exists(_copy).Returns(true);
        _file.GetAttributes(_copy).Returns(FileAttributes.ReadOnly | FileAttributes.Archive);
        _file.When(f => f.Delete(_copy)).Do(_ => throw refusal);

        Assert.False(_guard.TryDiscardCopy(_copy, out var failureCode));

        Assert.Equal(Kernel32.ERROR_SHARING_VIOLATION, failureCode);
        Received.InOrder(() =>
        {
            _file.SetAttributes(_copy, FileAttributes.Archive);
            _file.Delete(_copy);
            _file.SetAttributes(_copy, FileAttributes.ReadOnly | FileAttributes.Archive);
        });
    }

    [Fact]
    public void A_refused_delete_is_the_cause_recorded_when_the_attribute_cannot_be_put_back()
    {
        var refusal = new IOException("delete refused", Win32HResult(Kernel32.ERROR_SHARING_VIOLATION));
        _file.Exists(_copy).Returns(true);
        _file.GetAttributes(_copy).Returns(FileAttributes.ReadOnly);
        _file.When(f => f.Delete(_copy)).Do(_ => throw refusal);
        _file.When(f => f.SetAttributes(_copy, FileAttributes.ReadOnly))
            .Do(_ => throw new UnauthorizedAccessException("restore refused"));

        Assert.False(_guard.TryDiscardCopy(_copy, out var failureCode));

        Assert.Equal(Kernel32.ERROR_SHARING_VIOLATION, failureCode);
    }
}
