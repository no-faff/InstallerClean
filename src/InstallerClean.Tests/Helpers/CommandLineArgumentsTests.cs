using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// <see cref="CommandLineArguments"/> writes a list of arguments as .NET 10 writes
/// <c>ProcessStartInfo.ArgumentList</c>, and CommandLineToArgvW, the reader Windows
/// provides, gives each argument back as it was given.
/// </summary>
public class CommandLineArgumentsTests
{
    [Theory]
    [InlineData("/s", "/s")]
    [InlineData("", "\"\"")]
    [InlineData(@"D:\My Backup", "\"D:\\My Backup\"")]
    [InlineData(@"D:\My Backup\", "\"D:\\My Backup\\\\\"")]
    [InlineData("say \"hi\"", "\"say \\\"hi\\\"\"")]
    [InlineData(@"a\\""b", "\"a\\\\\\\\\\\"b\"")]
    [InlineData(@"C:\no\spaces\", @"C:\no\spaces\")]
    [InlineData("tab\there", "\"tab\there\"")]
    [InlineData("ideographic\u3000space", "\"ideographic\u3000space\"")]
    [InlineData("zero\u200Bwidth", "zero\u200Bwidth")]
    public void Each_argument_is_written_as_dotnet_10_writes_it(string argument, string written)
    {
        Assert.Equal(written, CommandLineArguments.Join([argument]));
    }

    [Fact]
    public void Arguments_are_separated_by_one_space_with_none_before_the_first()
    {
        Assert.Equal("/m \"D:\\a b\" /noupdate", CommandLineArguments.Join(["/m", @"D:\a b", "/noupdate"]));
        Assert.Equal("", CommandLineArguments.Join([]));
    }

    [Fact]
    public void A_null_argument_is_refused_as_Process_Start_refuses_one()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => CommandLineArguments.Join(["/s", null!]));

        Assert.Equal("item", ex.ParamName);
    }

    [Fact]
    public void The_white_space_set_is_Unicodes_White_Space_property()
    {
        // The 25 code points .NET 10's char.IsWhiteSpace answers true for.
        int[] expected =
        [
            0x0009, 0x000A, 0x000B, 0x000C, 0x000D, 0x0020, 0x0085, 0x00A0, 0x1680,
            0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007, 0x2008, 0x2009, 0x200A,
            0x2028, 0x2029, 0x202F, 0x205F, 0x3000,
        ];

        var found = Enumerable.Range(0, 65536).Where(i => CommandLineArguments.IsWhiteSpace((char)i)).ToArray();

        Assert.Equal(expected, found);
    }

    [Theory]
    [InlineData("/s")]
    [InlineData("")]
    [InlineData(@"D:\My Backup\")]
    [InlineData(@"D:\My Backup\\")]
    [InlineData("say \"hi\"")]
    [InlineData("\"")]
    [InlineData("\\\"")]
    [InlineData(@"a\\""b")]
    [InlineData(@"a\\b")]
    [InlineData("tab\there")]
    [InlineData("  leading and trailing  ")]
    [InlineData("/replacecopy:C:\\Program Files\\InstallerClean\\installerclean-cli.exe")]
    public void Windows_reads_each_argument_back_as_it_was_given(string argument)
    {
        // The program's path stands first, as Process.Start writes it: CommandLineToArgvW reads
        // the first argument by other rules.
        string[] given = ["/first", argument, "/last"];

        var read = Argv("\"C:\\x.exe\" " + CommandLineArguments.Join(given));

        Assert.Equal(["C:\\x.exe", .. given], read);
    }

    private static string[] Argv(string commandLine)
    {
        var list = CommandLineToArgvW(commandLine, out var count);
        if (list == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
        try
        {
            var argv = new string[count];
            for (var i = 0; i < count; i++)
                argv[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(list, i * IntPtr.Size))!;
            return argv;
        }
        finally
        {
            Kernel32.LocalFree(list);
        }
    }

    [DllImport("shell32.dll", EntryPoint = "CommandLineToArgvW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr CommandLineToArgvW(string lpCmdLine, out int pNumArgs);
}
