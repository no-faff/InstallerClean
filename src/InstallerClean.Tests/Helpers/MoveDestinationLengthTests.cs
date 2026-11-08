using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// <see cref="MoveDestinationLength"/> refuses a destination where a moved file's path would
/// be 260 characters or more, measured on the full path a Move writes. Every destination
/// here is built on <c>C:\</c> from a run of one letter, so its full path is its own
/// spelling and its length is known without reading the disk.
/// </summary>
public class MoveDestinationLengthTests
{
    private static readonly string[] OneFile = [@"C:\Windows\Installer\a.msi"];

    /// <summary>A folder whose full path is <paramref name="length"/> characters.</summary>
    private static string FolderOf(int length) => @"C:\" + new string('d', length - 3);

    [Fact]
    public void With_no_file_named_a_folder_of_257_characters_is_accepted()
    {
        Assert.False(MoveDestinationLength.TooLong(FolderOf(257)));
    }

    [Fact]
    public void With_no_file_named_a_folder_of_258_characters_is_refused()
    {
        // A separator and a one-character name take 258 to 260.
        Assert.True(MoveDestinationLength.TooLong(FolderOf(258)));
    }

    [Fact]
    public void A_file_whose_path_would_be_259_characters_is_accepted()
    {
        // 253, a separator and "a.msi" make 259.
        Assert.False(MoveDestinationLength.TooLong(FolderOf(253), OneFile));
    }

    [Fact]
    public void A_file_whose_path_would_be_260_characters_is_refused()
    {
        Assert.True(MoveDestinationLength.TooLong(FolderOf(254), OneFile));
    }

    [Fact]
    public void The_longest_name_in_the_batch_decides()
    {
        // 247, a separator and the 12 characters of "1a2b3c4d.msp" make 260; "a.msi" alone fits.
        string[] files =
        [
            @"C:\Windows\Installer\a.msi",
            @"C:\Windows\Installer\1a2b3c4d.msp",
            @"C:\Windows\Installer\b.msi",
        ];

        Assert.False(MoveDestinationLength.TooLong(FolderOf(247), OneFile));
        Assert.True(MoveDestinationLength.TooLong(FolderOf(247), files));
    }

    [Fact]
    public void A_trailing_separator_is_not_counted()
    {
        Assert.False(MoveDestinationLength.TooLong(FolderOf(253) + @"\", OneFile));
        Assert.True(MoveDestinationLength.TooLong(FolderOf(254) + @"\", OneFile));
    }

    [Fact]
    public void A_dot_dot_is_resolved_before_the_path_is_measured()
    {
        // Spelled at more than 260 characters, it names C:\short.
        Assert.False(MoveDestinationLength.TooLong(FolderOf(300) + @"\..\short", OneFile));
    }

    [Fact]
    public void An_extended_prefix_is_not_counted()
    {
        // Without the four characters of \\?\ the path is 259, then 260.
        Assert.False(MoveDestinationLength.TooLong(@"\\?\" + FolderOf(253), OneFile));
        Assert.True(MoveDestinationLength.TooLong(@"\\?\" + FolderOf(254), OneFile));
    }

    [Fact]
    public void A_share_is_measured_as_it_is_spelled_without_the_prefix()
    {
        // \\server\share\ is 15 characters, so these folders are 253 and 254.
        var fits = @"\\server\share\" + new string('d', 253 - 15);
        var over = @"\\server\share\" + new string('d', 254 - 15);

        Assert.False(MoveDestinationLength.TooLong(fits, OneFile));
        Assert.True(MoveDestinationLength.TooLong(over, OneFile));
        Assert.False(MoveDestinationLength.TooLong(@"\\?\UNC\" + fits.Substring(2), OneFile));
        Assert.True(MoveDestinationLength.TooLong(@"\\?\UNC\" + over.Substring(2), OneFile));
    }

    [Fact]
    public void A_lower_case_unc_after_the_prefix_is_not_counted_either()
    {
        var fits = @"\\server\share\" + new string('d', 253 - 15);

        Assert.False(MoveDestinationLength.TooLong(@"\\?\unc\" + fits.Substring(2), OneFile));
    }

    [Fact]
    public void A_device_prefix_is_not_counted()
    {
        Assert.False(MoveDestinationLength.TooLong(@"\\.\" + FolderOf(253), OneFile));
        Assert.True(MoveDestinationLength.TooLong(@"\\.\" + FolderOf(254), OneFile));
    }

    [Fact]
    public void A_destination_with_no_full_path_is_left_to_the_checks_after_it()
    {
        Assert.False(MoveDestinationLength.TooLong("C:\\" + new string('d', 300) + "\0b", OneFile));
    }
}
