using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class CompletionViewModelTests
{
    [Fact]
    public void FormatErrorBreakdown_splits_same_type_with_different_messages()
    {
        // RecycleFailed tailors its sentence by HRESULT (access denied vs
        // file in use vs generic); a mixed batch must show each sentence
        // over its own files, not the first file's sentence over all.
        var errors = new List<FileOperationError>
        {
            new RecycleFailed(@"C:\Windows\Installer\a.msi", unchecked((int)0x80070005)),
            new RecycleFailed(@"C:\Windows\Installer\b.msi", unchecked((int)0x80070020)),
        };

        var text = CompletionViewModel.FormatErrorBreakdown(errors);

        Assert.Contains(errors[0].LocalisedMessage, text);
        Assert.Contains(errors[1].LocalisedMessage, text);
        Assert.Contains("a.msi", text);
        Assert.Contains("b.msi", text);
    }

    [Fact]
    public void ShowDeleteSummary_reads_cleaned_up_not_freed_and_sets_the_space_hint()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 2, deletedBytes: 3 * 1024 * 1024,
            errors: new List<FileOperationError>());

        // A Recycle-Bin delete reclaims no disk until the bin is emptied, so
        // the headline reads "cleaned up", and the space hint names the
        // emptying step.
        Assert.Contains("cleaned up", vm.Heading);
        Assert.DoesNotContain("freed", vm.Heading);
        Assert.Equal(Strings.Completion_DeleteSpaceHint, vm.SpaceHint);
    }

    [Fact]
    public void ShowMoveSummary_keeps_freed_for_a_space_freeing_move_and_carries_no_space_hint()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveSummary(movedCount: 1, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: new List<FileOperationError>(), freesSpace: true);

        Assert.Contains("freed", vm.Heading);
        Assert.Equal(string.Empty, vm.SpaceHint);
    }

    [Fact]
    public void ShowPermanentDeleteSummary_reads_cleaned_up_and_carries_no_space_hint()
    {
        var vm = new CompletionViewModel();
        vm.ShowPermanentDeleteSummary(deletedCount: 1, deletedBytes: 1024 * 1024,
            errors: new List<FileOperationError>());

        // No bin to empty: the disk is reclaimed at that instant. The headline
        // still reads "cleaned up" for consistency with the recycle path.
        Assert.Contains("cleaned up", vm.Heading);
        Assert.Equal(string.Empty, vm.SpaceHint);
    }

    [Fact]
    public void Space_hint_from_a_delete_is_cleared_by_a_following_move()
    {
        // The view-model instance is reused across operations, so a stale space
        // hint from a prior delete must not bleed into a later move.
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 1, deletedBytes: 1024 * 1024,
            errors: new List<FileOperationError>());
        Assert.NotEqual(string.Empty, vm.SpaceHint);

        vm.ShowMoveSummary(movedCount: 1, movedBytes: 1024 * 1024, destination: @"D:\b",
            errors: new List<FileOperationError>(), freesSpace: false);
        Assert.Equal(string.Empty, vm.SpaceHint);
    }
}
