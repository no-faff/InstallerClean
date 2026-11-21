using System.Text;
using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Resources;

// Every copy carries THIRD-PARTY-NOTICES as a resource of InstallerClean.Core, which the
// About window shows and the command line prints. This project embeds the repository's
// THIRD-PARTY-NOTICES and LICENSE as plain bytes beside it, so the app's copy is read
// against the file itself. scripts/check-notices.ps1 reads the copy in each merged exe.
public class ThirdPartyNoticesTests
{
    private static byte[] Repository(string name)
    {
        using var stream = typeof(ThirdPartyNoticesTests).Assembly.GetManifestResourceStream("Repository." + name)
            ?? throw new InvalidOperationException($"The test assembly holds no Repository.{name}.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return bytes.ToArray();
    }

    private static string Utf8(byte[] bytes) =>
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);

    [Fact]
    public void The_app_reads_the_file_whole_as_the_repository_has_it()
    {
        Assert.Equal(Utf8(Repository("THIRD-PARTY-NOTICES")), ThirdPartyNotices.Read());
    }

    // The window and the command line show the text as read, so a byte-order mark would be a
    // stray character at its start, and a CR a doubled break where the command line splits at
    // each LF and writes the host's newline.
    [Fact]
    public void The_file_has_no_byte_order_mark_and_no_carriage_return()
    {
        var bytes = Repository("THIRD-PARTY-NOTICES");
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            "THIRD-PARTY-NOTICES starts with a UTF-8 byte-order mark.");
        Assert.DoesNotContain((byte)'\r', bytes);
    }

    // The Material Icons are published under the Apache License 2.0, which asks that its text
    // go with every copy, and LICENSE is not inside the portable or the command line. So the
    // notices carry its terms, word for word as LICENSE has them.
    [Fact]
    public void The_notices_carry_the_terms_of_the_Apache_License_as_LICENSE_has_them()
    {
        const string end = "END OF TERMS AND CONDITIONS";
        var licence = Utf8(Repository("LICENSE"));
        var terms = licence.Substring(0, licence.IndexOf(end, StringComparison.Ordinal) + end.Length).Trim('\n');

        Assert.Contains(terms, ThirdPartyNotices.Read());
    }

    [Fact]
    public void The_command_line_prints_the_notices_a_line_to_each_line_of_the_file()
    {
        var console = new StringWriter();
        var original = Console.Out;
        try
        {
            Console.SetOut(console);

            Assert.Equal(CliExitCode.Ok, Program.Main(["--notices"]));
        }
        finally
        {
            Console.SetOut(original);
        }

        // Each line ends in the writer's newline, whatever the host calls one.
        Assert.Equal(ThirdPartyNotices.Read().TrimEnd('\n') + "\n",
            console.ToString().Replace(Environment.NewLine, "\n"));
    }
}
