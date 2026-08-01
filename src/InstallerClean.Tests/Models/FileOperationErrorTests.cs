using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Models;

/// <summary>
/// Display-formatting pins for <see cref="FileOperationError"/> subtypes.
/// </summary>
public class FileOperationErrorTests
{
    [Fact]
    public void FileInUse_reads_for_both_operations_and_names_neither()
    {
        // Move and Delete share this category, both reaching it by
        // discriminating the same two HRESULTs off an IOException. So the verb
        // has to cover the pair: "remove", not "move" or "delete", either of
        // which is wrong to half the readers who see it.
        var err = new FileInUse(@"C:\Windows\Installer\x.msi");

        Assert.Equal(Strings.Error_FileInUse_Singular, err.LocalisedMessage);
        Assert.Contains("remove it", err.LocalisedMessage);
        Assert.DoesNotContain("x.msi", err.LocalisedMessage);
    }

    /// <summary>
    /// The per-file sentence and the list heading are two different jobs. The
    /// CLI prints LocalisedMessage after "filename: " for every failed file, so
    /// it stays singular whatever the bucket size; the completion overlay puts
    /// LocalisedGroupHeading over the list of filenames beneath it, so it
    /// inflects.
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
        // reparse point, an out-of-cache candidate) were deliberately left
        // without plural variants, so the base class default has to carry them
        // rather than a lookup missing and returning empty.
        var missing = new MissingSourceFile(@"C:\Windows\Installer\x.msi");
        var reparse = new SourceIsReparsePoint(@"C:\Windows\Installer\y.msi");
        var outside = new CandidateOutsideCache(@"C:\Windows\Installer\z.msi");

        Assert.Equal(missing.LocalisedMessage, missing.LocalisedGroupHeading(5));
        Assert.Equal(reparse.LocalisedMessage, reparse.LocalisedGroupHeading(5));
        Assert.Equal(outside.LocalisedMessage, outside.LocalisedGroupHeading(5));
    }
}
