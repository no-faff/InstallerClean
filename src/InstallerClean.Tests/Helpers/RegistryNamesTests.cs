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

    // ---- read twice: a listing that changes while it is read is treated as unread ----

    [Fact]
    public void A_key_nobody_writes_to_gives_the_names_one_listing_gives_from_one_pair_of_readings()
    {
        var key = new StandInKey { SubKeys = { "A", "B", "C" }, Values = { "", "x", "y" } };

        Assert.Equal(RegistryNames.SubKeys(key), RegistryNames.SubKeysUnchanged(key));
        Assert.Equal(RegistryNames.Values(key), RegistryNames.ValuesUnchanged(key));
        // One listing each for SubKeys and Values, two each for the readings that agreed.
        Assert.Equal(6, key.Counted);
    }

    [Fact]
    public void A_key_with_nothing_under_it_and_nobody_writing_gives_nothing()
    {
        var key = new StandInKey();

        Assert.Empty(RegistryNames.SubKeysUnchanged(key));
        Assert.Empty(RegistryNames.ValuesUnchanged(key));
    }

    // Each of these makes one condition hold and no other, between every two readings in
    // a row, so the key is read four times and then reported unread.

    [Fact]
    public void A_count_that_moves_between_the_readings_is_unread()
    {
        var key = new ScriptedKey((2, new[] { "A", "B" }), (3, new[] { "A", "B" }));

        AssertUnread(key, "the count Windows gives went from 2 to 3");
    }

    [Fact]
    public void A_first_listing_short_of_the_count_is_unread()
    {
        var key = new ScriptedKey((3, new[] { "A", "B" }));

        AssertUnread(key, "the first listing held 2 names against a count of 3");
    }

    [Fact]
    public void A_first_listing_past_the_count_is_unread()
    {
        var key = new ScriptedKey((1, new[] { "A", "B" }));

        AssertUnread(key, "the first listing held 2 names against a count of 1");
    }

    [Fact]
    public void Two_listings_that_differ_are_unread()
    {
        var key = new ScriptedKey((2, new[] { "A", "B" }), (2, new[] { "A", "D" }));

        AssertUnread(key, "the two listings differ");
    }

    [Fact]
    public void A_name_read_twice_in_one_listing_is_unread()
    {
        var key = new ScriptedKey((2, new[] { "A", "A" }));

        AssertUnread(key, "a name was read twice");
    }

    [Fact]
    public void Two_names_differing_only_in_case_are_two_names()
    {
        // A name read twice is the same stored string both times, so only an exact
        // repeat counts as one.
        var key = new ScriptedKey((2, new[] { "A", "a" }));

        Assert.Equal(new[] { "A", "a" }, RegistryNames.SubKeysUnchanged(key, NoPause));
    }

    [Fact]
    public void A_key_that_settles_gives_the_names_of_the_first_pair_that_agrees()
    {
        // The first and second readings disagree; the second and third agree.
        var key = new ScriptedKey(
            (2, new[] { "A", "B" }), (3, new[] { "A", "B", "C" }), (3, new[] { "A", "B", "C" }));

        Assert.Equal(new[] { "A", "B", "C" }, RegistryNames.SubKeysUnchanged(key, NoPause));
    }

    // The tears themselves, on a key that answers as Windows does.

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_name_removed_ahead_of_one_listing_gives_the_names_the_key_settles_on(bool subKeys)
    {
        // A, B and C, with A removed once the first listing has read A and B: index 2
        // then answers that there is nothing more, and that listing alone holds A and B
        // without C.
        var key = new StandInKey();
        var names = subKeys ? key.SubKeys : key.Values;
        names.AddRange(new[] { "A", "B", "C" });
        var removed = false;
        key.BeforeEnum = index =>
        {
            if (index == 2 && !removed) { names.RemoveAt(0); removed = true; }
        };

        Assert.Equal(new[] { "B", "C" }, List(key, subKeys));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_name_added_ahead_of_one_listing_gives_the_names_the_key_settles_on(bool subKeys)
    {
        // B and C, with A added once the first listing has read B: index 1 is then B
        // again, and that listing alone holds B twice and C, without A.
        var key = new StandInKey();
        var names = subKeys ? key.SubKeys : key.Values;
        names.AddRange(new[] { "B", "C" });
        var added = false;
        key.BeforeEnum = index =>
        {
            if (index == 1 && !added) { names.Insert(0, "A"); added = true; }
        };

        Assert.Equal(new[] { "A", "B", "C" }, List(key, subKeys));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_key_written_during_every_listing_is_unread(bool subKeys)
    {
        // Every listing finds a new name ahead of the one it has just read.
        var key = new StandInKey();
        var names = subKeys ? key.SubKeys : key.Values;
        names.AddRange(new[] { "M", "N" });
        var added = 0;
        key.BeforeEnum = index => { if (index == 1) names.Insert(0, $"A{added++:D2}"); };

        Assert.Throws<IOException>(() => List(key, subKeys));
    }

    [Fact]
    public void A_key_empty_at_its_first_count_and_written_before_the_second_is_read_again()
    {
        var key = new StandInKey();
        var counts = 0;
        key.BeforeCount = () => { if (++counts == 2) key.SubKeys.Add("added"); };

        Assert.Equal(new[] { "added" }, RegistryNames.SubKeysUnchanged(key, NoPause));
    }

    private static string[] List(RegistryNames.IKeyCalls key, bool subKeys) =>
        subKeys ? RegistryNames.SubKeysUnchanged(key, NoPause) : RegistryNames.ValuesUnchanged(key, NoPause);

    private static readonly Action<TimeSpan> NoPause = _ => { };

    [Fact]
    public void A_key_read_again_is_read_after_a_pause_that_lengthens()
    {
        var pauses = new List<TimeSpan>();
        var key = new ScriptedKey((2, new[] { "A", "B" }), (3, new[] { "A", "B" }));

        Assert.Throws<IOException>(() => RegistryNames.SubKeysUnchanged(key, pauses.Add));
        Assert.Equal(new[] { TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250) }, pauses);
    }

    [Fact]
    public void A_key_nobody_writes_to_is_read_without_a_pause()
    {
        var pauses = new List<TimeSpan>();

        RegistryNames.SubKeysUnchanged(new StandInKey { SubKeys = { "A" } }, pauses.Add);

        Assert.Empty(pauses);
    }

    private static void AssertUnread(ScriptedKey key, string disagreement)
    {
        foreach (var subKeys in new[] { true, false })
        {
            var thrown = Assert.Throws<IOException>(() => List(key.Again(), subKeys));
            Assert.Contains(disagreement, thrown.Message);
            Assert.Contains("4 readings", thrown.Message);
        }
    }

    /// <summary>
    /// A key whose readings are scripted and repeat in turn: the Nth count asked for
    /// answers the Nth entry's count, and the enumeration that
    /// follows answers that entry's names, so a test can make one condition hold without
    /// the others. Subkeys and values read the same script; <see cref="Again"/> starts it
    /// over.
    /// </summary>
    private sealed class ScriptedKey : RegistryNames.IKeyCalls
    {
        private readonly (int Count, string[] Names)[] _readings;
        private int _reading = -1;

        public ScriptedKey(params (int Count, string[] Names)[] readings) => _readings = readings;

        public ScriptedKey Again() => new(_readings);

        public int QueryCounts(out int subKeys, out int values)
        {
            _reading++;
            var reading = _readings[_reading % _readings.Length];
            subKeys = values = reading.Count;
            return 0;
        }

        public int EnumKey(int index, char[] name, ref int nameLength) => Enum(index, name, ref nameLength);

        public int EnumValue(int index, char[] name, ref int nameLength) => Enum(index, name, ref nameLength);

        public void MarkHandleInvalid() { }

        private int Enum(int index, char[] buffer, ref int nameLength)
        {
            var names = _readings[_reading % _readings.Length].Names;
            if (index >= names.Length) return ERROR_NO_MORE_ITEMS;
            names[index].CopyTo(0, buffer, 0, names[index].Length);
            nameLength = names[index].Length;
            return 0;
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
        public Action? BeforeCount { get; set; }
        public int? CountFailure { get; set; }
        public (int Index, int Code)? EnumFailureAtIndex { get; set; }
        public bool HandleMarkedInvalid { get; private set; }
        public List<(int Index, int BufferLength, int LengthGiven)> Asked { get; } = [];

        public int Counted { get; private set; }

        public int QueryCounts(out int subKeys, out int values)
        {
            Counted++;
            BeforeCount?.Invoke();
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
