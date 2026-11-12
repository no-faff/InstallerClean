using System.Globalization;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The exception built for each Windows error number as .NET 10 builds it: the type, the
/// message with and without a path, and the HResult a caller reads to tell one failure from
/// another.
/// </summary>
public class Win32MarshalTests
{
    public static TheoryData<int, string, Type, string, int> Named => new()
    {
        { 2, "", typeof(FileNotFoundException), "Unable to find the specified file.", unchecked((int)0x80070002) },
        { 2, @"C:\a.msi", typeof(FileNotFoundException), @"Could not find file 'C:\a.msi'.", unchecked((int)0x80070002) },
        { 3, "", typeof(DirectoryNotFoundException), "Could not find a part of the path.", unchecked((int)0x80070003) },
        { 3, @"C:\d\a.msi", typeof(DirectoryNotFoundException), @"Could not find a part of the path 'C:\d\a.msi'.", unchecked((int)0x80070003) },
        { 5, "", typeof(UnauthorizedAccessException), "Access to the path is denied.", unchecked((int)0x80070005) },
        { 5, @"C:\a.msi", typeof(UnauthorizedAccessException), @"Access to the path 'C:\a.msi' is denied.", unchecked((int)0x80070005) },
        { 32, "", typeof(IOException), "The process cannot access the file because it is being used by another process.", unchecked((int)0x80070020) },
        { 32, @"C:\a.msi", typeof(IOException), @"The process cannot access the file 'C:\a.msi' because it is being used by another process.", unchecked((int)0x80070020) },
        { 80, @"C:\a.msi", typeof(IOException), @"The file 'C:\a.msi' already exists.", unchecked((int)0x80070050) },
        { 183, @"C:\d", typeof(IOException), @"Cannot create 'C:\d' because a file or directory with the same name already exists.", unchecked((int)0x800700B7) },
        { 206, "", typeof(PathTooLongException), "The specified file name or path is too long, or a component of the specified path is too long.", unchecked((int)0x800700CE) },
        { 206, @"C:\a.msi", typeof(PathTooLongException), @"The path 'C:\a.msi' is too long, or a component of the specified path is too long.", unchecked((int)0x800700CE) },
    };

    [Theory]
    [MemberData(nameof(Named))]
    public void Each_mapped_number_gives_its_type_message_and_HResult(
        int code, string path, Type type, string message, int hresult)
    {
        var exception = Win32Marshal.GetExceptionForWin32Error(code, path);

        Assert.IsType(type, exception);
        Assert.Equal(message, exception.Message);
        Assert.Equal(hresult, exception.HResult);
    }

    [Fact]
    public void A_missing_file_carries_the_path_as_its_file_name()
    {
        var exception = Assert.IsType<FileNotFoundException>(Win32Marshal.GetExceptionForWin32Error(2, @"C:\a.msi"));

        Assert.Equal(@"C:\a.msi", exception.FileName);
    }

    [Fact]
    public void An_aborted_operation_is_a_plain_cancellation()
    {
        var exception = Assert.IsType<OperationCanceledException>(Win32Marshal.GetExceptionForWin32Error(995, @"C:\a.msi"));

        Assert.False(exception.CancellationToken.CanBeCanceled);
        Assert.Equal("The operation was canceled.", exception.Message);
    }

    [Theory]
    [InlineData(87)]
    [InlineData(80)]
    [InlineData(183)]
    [InlineData(1224)]
    public void Any_other_number_is_an_IOException_with_the_text_Windows_gives(int code)
    {
        var exception = Assert.IsType<IOException>(Win32Marshal.GetExceptionForWin32Error(code));

        Assert.Equal(WindowsErrors.SystemMessage(code), exception.Message);
        Assert.Equal(unchecked((int)0x80070000 | code), exception.HResult);
    }

    // The text itself, as Windows gives it in English, which is what the CI runner is
    // shown in, so the message is not only checked against the call that builds it.
    // Elsewhere Windows is shown in English or the row has nothing to compare against.
    [Theory]
    [InlineData(87, "The parameter is incorrect.")]
    [InlineData(80, "The file exists.")]
    [InlineData(183, "Cannot create a file when that file already exists.")]
    public void Any_other_number_carries_the_text_Windows_gives_in_English(int code, string text)
    {
        if (!string.Equals(CultureInfo.InstalledUICulture.TwoLetterISOLanguageName, "en", StringComparison.Ordinal)
            && Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
            return;

        var exception = Assert.IsType<IOException>(Win32Marshal.GetExceptionForWin32Error(code));

        Assert.Equal(text, exception.Message);
    }

    [Fact]
    public void Any_other_number_names_the_path_and_the_details_after_the_text()
    {
        var exception = Win32Marshal.GetExceptionForWin32Error(87, @"C:\a.msi", "More.");

        Assert.Equal(WindowsErrors.SystemMessage(87) + @" : 'C:\a.msi'. More.", exception.Message);
    }

    [Theory]
    [InlineData(5, unchecked((int)0x80070005))]
    [InlineData(0xFFFF, unchecked((int)0x8007FFFF))]
    [InlineData(unchecked((int)0x80004005), unchecked((int)0x80004005))]
    [InlineData(0x10000, 0x10000)]
    public void An_error_number_becomes_a_Windows_facility_HResult_and_an_HResult_stays_as_it_is(int code, int expected)
    {
        Assert.Equal(expected, Win32Marshal.MakeHRFromErrorCode(code));
    }
}
