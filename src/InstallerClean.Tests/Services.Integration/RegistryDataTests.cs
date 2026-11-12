using InstallerClean.Helpers;
using InstallerClean.Tests.Helpers;
using Microsoft.Win32;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// <see cref="RegistryData.ValueOf"/> reads a value as .NET 10's RegistryKey.GetValue
/// does: a REG_DWORD or REG_QWORD is a number only at exactly 4 or 8 bytes, and a value
/// that changes while it is read is read again or answers null, never zeros.
/// </summary>
public class RegistryDataTests
{
    [Theory]
    [InlineData(RawRegistryValue.REG_DWORD, 0, "bytes 0")]
    [InlineData(RawRegistryValue.REG_DWORD, 2, "bytes 2")]
    [InlineData(RawRegistryValue.REG_DWORD, 4, "int")]
    [InlineData(RawRegistryValue.REG_DWORD, 5, "bytes 5")]
    [InlineData(RawRegistryValue.REG_DWORD, 8, "long")]
    [InlineData(RawRegistryValue.REG_QWORD, 3, "bytes 3")]
    [InlineData(RawRegistryValue.REG_QWORD, 4, "int")]
    [InlineData(RawRegistryValue.REG_QWORD, 8, "long")]
    public void A_number_is_read_only_at_exactly_4_or_8_bytes(int type, int length, string expected)
    {
        WithKey(key =>
        {
            var data = new byte[length];
            for (var i = 0; i < length; i++) data[i] = (byte)(i + 1);
            RawRegistryValue.Write(key, "v", type, data);

            var value = RegistryData.ValueOf(key, "v");

            var read = value switch
            {
                int => "int",
                long => "long",
                byte[] bytes => $"bytes {bytes.Length}",
                _ => value?.GetType().Name ?? "null",
            };
            Assert.Equal(expected, read);
            if (value is int number) Assert.Equal(BitConverter.ToInt32(data, 0), number);
            if (value is long wide) Assert.Equal(BitConverter.ToInt64(data, 0), wide);
            if (value is byte[] raw) Assert.Equal(data, raw);
        });
    }

    [Fact]
    public void Text_keeps_its_own_characters_and_a_list_its_empty_entries()
    {
        WithKey(key =>
        {
            // "ab" and an odd trailing byte: the last code unit takes a zero byte.
            RawRegistryValue.Write(key, "odd", RawRegistryValue.REG_SZ, new byte[] { (byte)'a', 0, (byte)'b', 0, (byte)'c' });
            // a, an empty entry, b, then the list's two terminators.
            RawRegistryValue.Write(key, "list", RawRegistryValue.REG_MULTI_SZ,
                new byte[] { (byte)'a', 0, 0, 0, (byte)'b', 0, 0, 0, 0, 0 });

            Assert.Equal("abc", RegistryData.ValueOf(key, "odd"));
            Assert.Equal(new[] { "a", "", "b" }, (string[])RegistryData.ValueOf(key, "list")!);
            Assert.Null(RegistryData.ValueOf(key, "missing"));
        });
    }

    // A value rewritten longer between Windows giving its length and its data is read
    // again into a buffer of the new length, and one removed in between answers null.
    [Fact]
    public void A_value_that_changes_while_it_is_read_is_read_again_or_answers_null()
    {
        var grown = new byte[600];
        grown[0] = (byte)'x';
        var calls = 0;
        var value = RegistryData.Decode(buffer =>
        {
            calls++;
            if (buffer.Length < grown.Length) return (234, RawRegistryValue.REG_SZ, grown.Length);
            Buffer.BlockCopy(grown, 0, buffer, 0, grown.Length);
            return (0, RawRegistryValue.REG_SZ, grown.Length);
        }, doNotExpand: true);
        Assert.Equal(2, calls);
        Assert.StartsWith("x", (string)value!);

        calls = 0;
        var removed = RegistryData.Decode(_ =>
            ++calls == 1 ? (234, RawRegistryValue.REG_DWORD, 4096) : (2, 0, 0), doNotExpand: true);
        Assert.Null(removed);
    }

#if NETFRAMEWORK
    // .NET Framework's own GetValue, which the shipped projects are barred from calling,
    // reads a REG_DWORD with no data as the int 0.
    [Fact]
    public void The_framework_s_own_read_takes_an_empty_dword_for_zero()
    {
        WithKey(key =>
        {
            RawRegistryValue.Write(key, "v", RawRegistryValue.REG_DWORD, new byte[0]);
            Assert.Equal(0, key.GetValue("v"));
        });
    }
#endif

    private static void WithKey(Action<RegistryKey> body)
    {
        var path = $@"Software\InstallerCleanTests\{Guid.NewGuid():N}";
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(path, writable: true);
            Assert.NotNull(key);
            body(key!);
        }
        finally
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false); }
            catch (Exception) { /* the test's verdict must not turn on the tidy-up */ }
        }
    }
}
