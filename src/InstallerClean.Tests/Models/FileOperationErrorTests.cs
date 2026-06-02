using InstallerClean.Models;

namespace InstallerClean.Tests.Models;

/// <summary>
/// Display-formatting pins for <see cref="FileOperationError"/> subtypes.
/// </summary>
public class FileOperationErrorTests
{
    [Fact]
    public void RecycleFailed_renders_the_hresult_as_hex_not_signed_decimal()
    {
        // A COM HRESULT with the top bit set (E_FAIL) is a large negative
        // int. Rendered through the bare resx {0} it reads as gibberish
        // (-2147467259); hex keeps it recognisable as an HRESULT.
        var err = new RecycleFailed(@"C:\Windows\Installer\x.msi", unchecked((int)0x80004005));

        Assert.Contains("0x80004005", err.LocalisedMessage);
        Assert.DoesNotContain("-2147467259", err.LocalisedMessage);
    }
}
