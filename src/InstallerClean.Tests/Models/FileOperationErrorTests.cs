using InstallerClean.Models;
using InstallerClean.Resources;

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

    [Fact]
    public void RecycleFailed_access_denied_uses_the_access_denied_message()
    {
        // 0x80070005 (E_ACCESSDENIED) is a permissions or ownership block, a
        // distinct cause from a held-open file, so it gets its own message.
        var err = new RecycleFailed(@"C:\Windows\Installer\x.msi", unchecked((int)0x80070005));

        Assert.Equal(string.Format(Strings.Error_RecycleAccessDenied, "0x80070005"), err.LocalisedMessage);
    }

    [Fact]
    public void RecycleFailed_sharing_violation_uses_the_in_use_message()
    {
        var err = new RecycleFailed(@"C:\Windows\Installer\x.msi", unchecked((int)0x80070020));

        Assert.Equal(string.Format(Strings.Error_RecycleInUse, "0x80070020"), err.LocalisedMessage);
    }

    [Fact]
    public void RecycleFailed_lock_violation_uses_the_in_use_message()
    {
        var err = new RecycleFailed(@"C:\Windows\Installer\x.msi", unchecked((int)0x80070021));

        Assert.Equal(string.Format(Strings.Error_RecycleInUse, "0x80070021"), err.LocalisedMessage);
    }

    [Fact]
    public void RecycleFailed_unclassified_code_uses_the_generic_message()
    {
        // A shell copy-engine code (FACILITY_SHELL, 0x8027xxxx) or any other
        // code not classified takes the generic line, never a guessed cause.
        var err = new RecycleFailed(@"C:\Windows\Installer\x.msi", unchecked((int)0x80270000));

        Assert.Equal(string.Format(Strings.Error_ShellRecycleFailed, "0x80270000"), err.LocalisedMessage);
    }

    [Fact]
    public void PermanentlyDeleted_is_category_only_with_no_path_or_code()
    {
        // The shell nuked a file it was asked to recycle while reporting
        // success throughout, so the HResult this carries is a SUCCESS code
        // held for telemetry. It stays out of the sentence, where it would
        // read as a failure code with nothing to look up, and so does the
        // path, which in an elevated run can name another user's profile.
        var err = new PermanentlyDeleted(@"C:\Windows\Installer\x.msi", 0);

        Assert.Equal(Strings.Error_DeletedNotRecycled, err.LocalisedMessage);
        Assert.DoesNotContain("x.msi", err.LocalisedMessage);
        // No placeholder: nothing formats this one, so an added {0} would
        // reach the user literally.
        Assert.DoesNotContain("{0}", err.LocalisedMessage);
    }
}
