using System.Text;
using InstallerClean.Cli;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The command line's output encoding (<see cref="Program.TrySetUtf8Output(Func{Encoding}, Action{Encoding}, Action)"/>):
/// the console's code page set to UTF-8 where there is a console, and UTF-8 writers put over
/// stdout and stderr where setting it fails because there is none. The failure is the
/// IOException .NET Framework's setter throws for ERROR_INVALID_HANDLE and
/// ERROR_INVALID_ACCESS, made here with the HRESULT it carries.
/// </summary>
public class CliOutputEncodingTests
{
    private static IOException SetFailedWith(int error) =>
        new("The handle is invalid.", unchecked((int)0x80070000) | error);

    [Fact]
    public void Where_the_code_page_is_set_the_one_it_replaced_comes_back_and_no_writer_is_put_over_stdout()
    {
        var oem = Encoding.GetEncoding(437);
        Encoding? set = null;
        var writersPut = 0;

        var previous = Program.TrySetUtf8Output(() => oem, e => set = e, () => writersPut++);

        Assert.Same(oem, previous);
        Assert.Equal(65001, set?.CodePage);
        Assert.Equal(0, writersPut);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(12)]
    public void Where_there_is_no_console_UTF8_writers_are_put_over_stdout_and_nothing_is_given_back(int error)
    {
        var writersPut = 0;

        var previous = Program.TrySetUtf8Output(
            () => Encoding.GetEncoding(1252), _ => throw SetFailedWith(error), () => writersPut++);

        Assert.Null(previous);
        Assert.Equal(1, writersPut);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(87)]
    public void Any_other_failure_to_set_the_code_page_leaves_the_writers_as_they_are(int error)
    {
        var writersPut = 0;

        var previous = Program.TrySetUtf8Output(
            () => Encoding.GetEncoding(1252), _ => throw SetFailedWith(error), () => writersPut++);

        Assert.Null(previous);
        Assert.Equal(0, writersPut);
    }

    [Fact]
    public void A_failure_putting_the_writers_over_stdout_never_escapes()
    {
        var thrown = Record.Exception(() => Program.TrySetUtf8Output(
            () => Encoding.GetEncoding(1252), _ => throw SetFailedWith(6), () => throw new IOException("no stream")));

        Assert.Null(thrown);
    }

    [Fact]
    public void A_failure_reading_the_code_page_changes_nothing()
    {
        var set = 0;

        var previous = Program.TrySetUtf8Output(
            () => throw SetFailedWith(6), _ => set++, () => set++);

        Assert.Null(previous);
        Assert.Equal(0, set);
    }

    [Fact]
    public void The_writer_put_over_stdout_writes_UTF8_with_no_byte_order_mark_on_every_write()
    {
        using var stream = new MemoryStream();
        var writer = Program.Utf8ConsoleWriter(stream);

        writer.Write("é ü 日");

        Assert.Equal(
            new byte[] { 0xC3, 0xA9, 0x20, 0xC3, 0xBC, 0x20, 0xE6, 0x97, 0xA5 },
            stream.ToArray());
    }

    [Fact]
    public void Where_a_standard_handle_is_missing_the_writer_takes_nothing()
    {
        Assert.Same(TextWriter.Null, Program.Utf8ConsoleWriter(Stream.Null));
    }
}
