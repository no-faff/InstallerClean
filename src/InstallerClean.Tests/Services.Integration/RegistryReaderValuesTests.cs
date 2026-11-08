using InstallerClean.Services;
using Microsoft.Win32;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// <see cref="RegistryReader.LocalMachineValues"/> against the real registry, on keys
/// every Windows installation carries.
/// </summary>
public class RegistryReaderValuesTests
{
    [Fact]
    public void A_key_s_values_are_read_with_their_types_and_the_strings_with_their_text()
    {
        var read = new RegistryReader().LocalMachineValues(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", CancellationToken.None);

        Assert.Equal(RegistryKeyPresence.Present, read.Presence);
        Assert.NotNull(read.Values);
        var name = Assert.Single(read.Values, v => v.Name == "ProductName");
        Assert.Equal(RegistryValueKind.String, name.Kind);
        Assert.False(string.IsNullOrEmpty(name.Text));
        var number = Assert.Single(read.Values, v => v.Name == "CurrentMajorVersionNumber");
        Assert.Equal(RegistryValueKind.DWord, number.Kind);
        Assert.Null(number.Text);
    }

    [Fact]
    public void An_expandable_value_is_read_as_the_text_it_holds_and_its_type()
    {
        // windir is a REG_EXPAND_SZ holding a variable, which a default read expands.
        var read = new RegistryReader().LocalMachineValues(
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", CancellationToken.None);

        Assert.Equal(RegistryKeyPresence.Present, read.Presence);
        var windir = Assert.Single(read.Values!, v => v.Name == "windir");
        Assert.Equal(RegistryValueKind.ExpandString, windir.Kind);
        Assert.Equal("%SystemRoot%", windir.Text);
    }

    [Fact]
    public void A_key_s_default_value_is_read_under_the_empty_name()
    {
        // The .exe key's default value names the file type an executable is.
        var read = new RegistryReader().LocalMachineValues(@"SOFTWARE\Classes\.exe", CancellationToken.None);

        Assert.Equal(RegistryKeyPresence.Present, read.Presence);
        var defaultValue = Assert.Single(read.Values!, v => v.Name.Length == 0);
        Assert.Equal(RegistryValueKind.String, defaultValue.Kind);
        Assert.Equal("exefile", defaultValue.Text);
    }

    [Fact]
    public void A_key_that_is_not_there_reads_as_absent_with_no_values()
    {
        var read = new RegistryReader().LocalMachineValues(@"SOFTWARE\InstallerClean-test-" + Guid.NewGuid().ToString("N"), CancellationToken.None);

        Assert.Equal(RegistryKeyPresence.Absent, read.Presence);
        Assert.Null(read.Values);
    }
}
