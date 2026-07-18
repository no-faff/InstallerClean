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

    [Fact]
    public void FileInUse_reads_as_a_move_failure_not_a_removal_failure()
    {
        // Move's counterpart to Error.RecycleInUse. It must say "move": the
        // Delete-side sentence says "remove", and a user reading this one has
        // pressed Move.
        var err = new FileInUse(@"C:\Windows\Installer\x.msi");

        Assert.Equal(Strings.Error_FileInUse_Singular, err.LocalisedMessage);
        Assert.Contains("move it", err.LocalisedMessage);
        Assert.DoesNotContain("x.msi", err.LocalisedMessage);
    }

    /// <summary>
    /// The per-file sentence and the list heading are two different jobs. The
    /// CLI prints LocalisedMessage after "filename: " for every failed file, so
    /// it stays singular whatever the bucket size; the completion overlay puts
    /// LocalisedGroupHeading over the indented list, so it inflects.
    /// </summary>
    [Fact]
    public void LocalisedMessage_stays_singular_while_the_heading_inflects()
    {
        var err = new FileInUse(@"C:\Windows\Installer\x.msi");

        Assert.Equal(Strings.Error_FileInUse_Singular, err.LocalisedMessage);
        Assert.Equal(Strings.Error_FileInUse_Singular, err.LocalisedGroupHeading(1));
        Assert.Equal(Strings.Error_FileInUse_Plural, err.LocalisedGroupHeading(4));
    }

    [Fact]
    public void The_four_pluralising_categories_pick_their_plural_heading_for_a_bucket()
    {
        FileOperationError[] errors =
        [
            new FileInUse(@"C:\Windows\Installer\a.msi"),
            new IOFailure(@"C:\Windows\Installer\b.msi"),
            new AccessDenied(@"C:\Windows\Installer\c.msi"),
            new UnknownError(@"C:\Windows\Installer\d.msi"),
        ];
        string[] plurals =
        [
            Strings.Error_FileInUse_Plural,
            Strings.Error_IOFailure_Plural,
            Strings.Error_AccessDenied_Plural,
            Strings.Error_UnknownError_Plural,
        ];

        for (int i = 0; i < errors.Length; i++)
            Assert.Equal(plurals[i], errors[i].LocalisedGroupHeading(3));
    }

    [Fact]
    public void A_category_with_no_plural_form_heads_its_bucket_with_its_own_sentence()
    {
        // The rare and the already-complete sentences (a missing source, a
        // reparse point, an out-of-cache candidate, the recycle family) were
        // deliberately left without plural variants, so the base class default
        // has to carry them rather than a lookup missing and returning empty.
        var missing = new MissingSourceFile(@"C:\Windows\Installer\x.msi");
        var recycle = new RecycleFailed(@"C:\Windows\Installer\y.msi", unchecked((int)0x80070020));

        Assert.Equal(missing.LocalisedMessage, missing.LocalisedGroupHeading(5));
        Assert.Equal(recycle.LocalisedMessage, recycle.LocalisedGroupHeading(5));
    }
}
