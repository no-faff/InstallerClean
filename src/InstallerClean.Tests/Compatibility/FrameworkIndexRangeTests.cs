namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// The Index and Range types InstallerClean.Core declares for .NET Framework 4.8, as .NET 10
/// has them: where an index from either end falls, the offset and length a range covers and
/// what it throws past the sequence, how each writes itself, and the C# syntax built on them.
/// </summary>
public class FrameworkIndexRangeTests
{
    [Fact]
    public void An_index_from_the_start_is_its_value_and_from_the_end_counts_back_from_the_length()
    {
        Assert.Equal(2, new Index(2).GetOffset(5));
        Assert.Equal(4, new Index(1, fromEnd: true).GetOffset(5));
        Assert.Equal(4, Index.FromEnd(1).GetOffset(5));
        Assert.Equal(3, Index.FromStart(3).GetOffset(5));
        Assert.Equal(0, Index.Start.GetOffset(5));
        Assert.Equal(5, Index.End.GetOffset(5));
    }

    [Fact]
    public void An_index_reports_its_value_and_which_end_it_counts_from()
    {
        Index fromEnd = ^3;
        Index fromStart = 3;

        Assert.Equal(3, fromEnd.Value);
        Assert.True(fromEnd.IsFromEnd);
        Assert.Equal(3, fromStart.Value);
        Assert.False(fromStart.IsFromEnd);
        Assert.True(Index.End.IsFromEnd);
        Assert.Equal(0, Index.End.Value);
    }

    [Fact]
    public void A_negative_index_value_throws_naming_value()
    {
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => new Index(-1)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => new Index(-1, fromEnd: true)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => Index.FromStart(-1)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => Index.FromEnd(-1)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => (Index)(-1)).ParamName);
    }

    [Fact]
    public void Indexes_are_equal_where_they_name_the_same_place_from_the_same_end()
    {
        Assert.True(new Index(2, fromEnd: true).Equals(^2));
        Assert.True(new Index(2).Equals((object)Index.FromStart(2)));
        Assert.False(new Index(2).Equals(^2));
        Assert.False(new Index(2).Equals((object)2));
        Assert.Equal(Index.FromEnd(2).GetHashCode(), (^2).GetHashCode());
    }

    [Fact]
    public void An_index_writes_its_value_after_a_caret_when_it_counts_from_the_end()
    {
        Assert.Equal("^3", (^3).ToString());
        Assert.Equal("^0", Index.End.ToString());
        Assert.Equal("4", ((Index)4).ToString());
    }

    [Fact]
    public void A_range_gives_the_offset_of_its_first_element_and_how_many_it_covers()
    {
        Assert.Equal((1, 3), (1..^1).GetOffsetAndLength(5));
        Assert.Equal((0, 5), Range.All.GetOffsetAndLength(5));
        Assert.Equal((2, 3), Range.StartAt(2).GetOffsetAndLength(5));
        Assert.Equal((0, 3), Range.EndAt(^2).GetOffsetAndLength(5));
        Assert.Equal((5, 0), (5..).GetOffsetAndLength(5));
        Assert.Equal((2, 0), (2..2).GetOffsetAndLength(5));
    }

    [Fact]
    public void A_range_past_the_sequence_or_starting_after_its_end_throws_naming_length()
    {
        Assert.Equal("length", Assert.Throws<ArgumentOutOfRangeException>(() => (0..6).GetOffsetAndLength(5)).ParamName);
        Assert.Equal("length", Assert.Throws<ArgumentOutOfRangeException>(() => (3..2).GetOffsetAndLength(5)).ParamName);
        Assert.Equal("length", Assert.Throws<ArgumentOutOfRangeException>(() => (^6..).GetOffsetAndLength(5)).ParamName);
    }

    [Fact]
    public void Ranges_are_equal_where_both_ends_are_and_equal_ranges_hash_alike()
    {
        Assert.True((1..^1).Equals(new Range(1, ^1)));
        Assert.True((1..^1).Equals((object)new Range(Index.FromStart(1), Index.FromEnd(1))));
        Assert.False((1..^1).Equals(1..1));
        Assert.False((1..^1).Equals((object)"1..^1"));
        Assert.Equal((1..^1).GetHashCode(), new Range(1, ^1).GetHashCode());
    }

    [Fact]
    public void A_range_writes_its_two_ends_joined_by_two_dots()
    {
        Assert.Equal("1..^1", (1..^1).ToString());
        Assert.Equal("0..^0", Range.All.ToString());
    }

    [Fact]
    public void The_index_and_range_syntax_reaches_the_elements_dotnet_10_reaches()
    {
        var letters = new List<char> { 'a', 'b', 'c', 'd', 'e' };

        Assert.Equal('e', letters[^1]);
        Assert.Equal("bcd", "abcde"[1..^1]);
        Assert.Equal("de", "abcde"[^2..]);
        Assert.Equal("abcde", "abcde"[..]);
    }
}
