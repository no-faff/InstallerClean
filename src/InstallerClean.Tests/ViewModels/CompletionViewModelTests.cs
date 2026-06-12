using InstallerClean.Models;
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
}
