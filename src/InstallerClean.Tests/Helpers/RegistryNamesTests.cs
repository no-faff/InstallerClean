using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// <see cref="RegistryNames"/> lists a key's subkeys and values as .NET 10's
/// <c>RegistryKey</c> lists them, driven here through a stand-in for the three Windows
/// calls that answers as Windows does: ERROR_NO_MORE_ITEMS past the last name,
/// ERROR_MORE_DATA where a name does not fit the buffer, and the length written back
/// without the terminator.
///
/// The cases a counted loop gets wrong come first: a name added after the count is
/// taken, and a name removed after it ends the listing with no error.
/// </summary>
public class RegistryNamesTests
{
    private const int ERROR_FILE_NOT_FOUND = 2;
    private const int ERROR_ACCESS_DENIED = 5;
    private const int ERROR_INVALID_HANDLE = 6;
    private const int ERROR_MORE_DATA = 234;
    private const int ERROR_NO_MORE_ITEMS = 259;
    private const int ERROR_BADDB = 1009;

    [Fact]
    public void A_subkey_added_after_the_count_is_listed()
    {
        var key = new StandInKey { SubKeys = { "A", "B" } };
        key.BeforeEnum = index => { if (index == 1) key.SubKeys.Add("C"); };

        Assert.Equal(new[] { "A", "B", "C" }, RegistryNames.SubKeys(key));
    }

    [Fact]
    public void A_value_added_after_the_count_is_listed()
    {
        var key = new StandInKey { Values = { "A", "B" } };
        key.BeforeEnum = index => { if (index == 1) key.Values.Add("C"); };

        Assert.Equal(new[] { "A", "B", "C" }, RegistryNames.Values(key));
    }

    [Fact]
    public void A_subkey_removed_after_the_count_ends_the_listing_without_an_error()
    {
        var key = new StandInKey { SubKeys = { "A", "B", "C" } };
        key.BeforeEnum = index => { if (index == 1) key.SubKeys.RemoveAt(2); };

        Assert.Equal(new[] { "A", "B" }, RegistryNames.SubKeys(key));
    }

    [Fact]
    public void A_value_removed_after_the_count_ends_the_listing_without_an_error()
    {
        var key = new StandInKey { Values = { "A", "B", "C" } };
        key.BeforeEnum = index => { if (index == 1) key.Values.RemoveAt(2); };

        Assert.Equal(new[] { "A", "B" }, RegistryNames.Values(key));
    }

    [Fact]
    public void The_listing_grows_past_twice_the_count()
    {
        var key = new StandInKey { SubKeys = { "A" } };
        key.BeforeEnum = index => { if (index == 0) key.SubKeys.AddRange(new[] { "B", "C", "D", "E" }); };

        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, RegistryNames.SubKeys(key));
    }

    [Fact]
    public void A_count_of_zero_lists_nothing_and_enumerates_nothing()
    {
        var key = new StandInKey();
        key.BeforeEnum = _ => key.SubKeys.Add("added after the count");

        Assert.Empty(RegistryNames.SubKeys(key));
        Assert.Empty(RegistryNames.Values(key));
        Assert.Empty(key.Asked);
    }

    [Fact]
    public void A_name_is_taken_at_the_length_Windows_gives_not_at_a_terminator()
    {
        var key = new StandInKey { SubKeys = { "ab\0cd" }, Values = { "ef\0gh" } };

        Assert.Equal("ab\0cd", Assert.Single(RegistryNames.SubKeys(key)));
        Assert.Equal("ef\0gh", Assert.Single(RegistryNames.Values(key)));
    }

    [Fact]
    public void Every_call_is_given_the_whole_buffer_as_its_length()
    {
        var key = new StandInKey { SubKeys = { "a", "bbbb", "cc" }, Values = { "a", "bbbb", "cc" } };

        RegistryNames.SubKeys(key);
        RegistryNames.Values(key);

        Assert.All(key.Asked, call => Assert.Equal(call.BufferLength, call.LengthGiven));
    }

    [Fact]
    public void A_value_name_longer_than_the_buffer_is_asked_again_at_the_same_index_with_a_larger_one()
    {
        var longName = new string('v', 450);
        var key = new StandInKey { Values = { "first", longName, "last" } };

        Assert.Equal(new[] { "first", longName, "last" }, RegistryNames.Values(key));

        // The sizes .NET 10's pool hands back: 128 and 256 do not hold 450 characters and
        // their terminator; 512 does.
        var asked = key.Asked.Select(c => (c.Index, c.BufferLength)).ToArray();
        Assert.Equal(new[] { (0, 128), (1, 128), (1, 256), (1, 512), (2, 512), (3, 512) }, asked);
    }

    [Fact]
    public void A_subkey_name_longer_than_255_characters_is_thrown_as_Windows_answers_it()
    {
        var longest = new string('k', 255);
        Assert.Equal(longest, Assert.Single(RegistryNames.SubKeys(new StandInKey { SubKeys = { longest } })));

        var key = new StandInKey { SubKeys = { new string('k', 256) } };

        var thrown = Assert.Throws<IOException>(() => RegistryNames.SubKeys(key));
        Assert.Equal(ERROR_MORE_DATA, thrown.HResult);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Access_denied_is_UnauthorizedAccessException(bool atTheCount)
    {
        var key = Failing(ERROR_ACCESS_DENIED, atTheCount);

        Assert.Throws<UnauthorizedAccessException>(() => RegistryNames.SubKeys(key));
        Assert.Throws<UnauthorizedAccessException>(() => RegistryNames.Values(key));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_key_that_no_longer_exists_is_an_IOException_with_HResult_2(bool atTheCount)
    {
        var key = Failing(ERROR_FILE_NOT_FOUND, atTheCount);

        foreach (var list in Listings)
        {
            var thrown = Assert.Throws<IOException>(() => list(key));
            Assert.Equal(ERROR_FILE_NOT_FOUND, thrown.HResult);
            Assert.Equal("The specified registry key does not exist.", thrown.Message);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Any_other_code_is_an_IOException_with_the_bare_code_as_HResult(bool atTheCount)
    {
        var key = Failing(ERROR_BADDB, atTheCount);

        foreach (var list in Listings)
        {
            var thrown = Assert.Throws<IOException>(() => list(key));
            Assert.Equal(ERROR_BADDB, thrown.HResult);
            Assert.Null(WindowsErrors.Of(thrown));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void An_invalid_handle_marks_the_handle_invalid_and_is_an_IOException(bool atTheCount)
    {
        foreach (var list in Listings)
        {
            var key = Failing(ERROR_INVALID_HANDLE, atTheCount);
            var thrown = Assert.Throws<IOException>(() => list(key));
            Assert.Equal(ERROR_INVALID_HANDLE, thrown.HResult);
            Assert.True(key.HandleMarkedInvalid);
        }
    }

    private static readonly Func<RegistryNames.IKeyCalls, string[]>[] Listings =
        [RegistryNames.SubKeys, RegistryNames.Values];

    private static StandInKey Failing(int code, bool atTheCount)
    {
        var key = new StandInKey { SubKeys = { "A", "B" }, Values = { "A", "B" } };
        if (atTheCount) key.CountFailure = code;
        else key.EnumFailureAtIndex = (1, code);
        return key;
    }

    /// <summary>
    /// A key whose names are two lists, answering the three calls as Windows does. The
    /// count is the lists' sizes when it is asked; <see cref="BeforeEnum"/> runs before
    /// each enumeration call, so a test can change the key while it is listed.
    /// </summary>
    private sealed class StandInKey : RegistryNames.IKeyCalls
    {
        public List<string> SubKeys { get; } = [];
        public List<string> Values { get; } = [];
        public Action<int>? BeforeEnum { get; set; }
        public int? CountFailure { get; set; }
        public (int Index, int Code)? EnumFailureAtIndex { get; set; }
        public bool HandleMarkedInvalid { get; private set; }
        public List<(int Index, int BufferLength, int LengthGiven)> Asked { get; } = [];

        public int QueryCounts(out int subKeys, out int values)
        {
            subKeys = SubKeys.Count;
            values = Values.Count;
            return CountFailure ?? 0;
        }

        public int EnumKey(int index, char[] name, ref int nameLength) => Enum(SubKeys, index, name, ref nameLength);

        public int EnumValue(int index, char[] name, ref int nameLength) => Enum(Values, index, name, ref nameLength);

        public void MarkHandleInvalid() => HandleMarkedInvalid = true;

        private int Enum(List<string> names, int index, char[] buffer, ref int nameLength)
        {
            Asked.Add((index, buffer.Length, nameLength));
            BeforeEnum?.Invoke(index);

            if (EnumFailureAtIndex is { } failure && failure.Index == index) return failure.Code;
            if (index >= names.Count) return ERROR_NO_MORE_ITEMS;

            var found = names[index];
            if (found.Length + 1 > nameLength) return ERROR_MORE_DATA;

            found.CopyTo(0, buffer, 0, found.Length);
            buffer[found.Length] = '\0';
            nameLength = found.Length;
            return 0;
        }
    }
}
