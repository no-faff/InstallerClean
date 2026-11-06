namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// The dictionary, pair and sequence members the app calls as .NET 10 has them: adding only
/// where a key is absent, a default for an absent key, a removal that hands back the value,
/// a pair's two halves, and the first element per key in source order.
/// </summary>
public class FrameworkCollectionsTests
{
    [Fact]
    public void TryAdd_adds_an_absent_key_and_answers_true()
    {
        var dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        Assert.True(dictionary.TryAdd("a", 1));
        Assert.Equal(1, dictionary["a"]);
    }

    [Fact]
    public void TryAdd_leaves_an_entry_already_there_and_answers_false()
    {
        var dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["a"] = 1 };

        Assert.False(dictionary.TryAdd("A", 2));
        Assert.Equal(1, dictionary["a"]);
        Assert.Single(dictionary);
    }

    [Fact]
    public void TryAdd_refuses_a_null_key()
    {
        var dictionary = new Dictionary<string, int>();

        Assert.Throws<ArgumentNullException>(() => dictionary.TryAdd(null!, 1));
    }

    [Fact]
    public void GetValueOrDefault_gives_the_value_under_a_key()
    {
        var dictionary = new Dictionary<string, TimeSpan> { ["a"] = TimeSpan.FromSeconds(3) };

        Assert.Equal(TimeSpan.FromSeconds(3), dictionary.GetValueOrDefault("a"));
    }

    [Fact]
    public void GetValueOrDefault_gives_the_default_for_an_absent_key()
    {
        var times = new Dictionary<string, TimeSpan>();
        var kinds = new Dictionary<string, DriveType?>();

        Assert.Equal(TimeSpan.Zero, times.GetValueOrDefault("a"));
        Assert.Null(kinds.GetValueOrDefault("a"));
        Assert.Equal(7, new Dictionary<string, int>().GetValueOrDefault("a", 7));
    }

    [Fact]
    public void Remove_hands_back_the_value_it_removes()
    {
        var dictionary = new Dictionary<string, Func<bool>?> { ["a"] = () => true };

        Assert.True(dictionary.Remove("a", out var value));
        Assert.NotNull(value);
        Assert.Empty(dictionary);
    }

    [Fact]
    public void Remove_of_an_absent_key_answers_false_with_null()
    {
        var dictionary = new Dictionary<string, Func<bool>?> { ["b"] = () => true };

        Assert.False(dictionary.Remove("a", out var value));
        Assert.Null(value);
        Assert.Single(dictionary);
    }

    [Fact]
    public void A_pair_deconstructs_into_its_key_and_value()
    {
        var seen = new List<string>();
        foreach (var (key, value) in new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 })
            seen.Add(key + value);

        Assert.Equal(new[] { "a1", "b2" }, seen);
    }

    [Fact]
    public void DistinctBy_keeps_the_first_element_for_each_key_in_source_order()
    {
        var source = new[] { ("b", 1), ("a", 2), ("B", 3), ("b", 4), ("a", 5), ("c", 6) };

        Assert.Equal(new[] { ("b", 1), ("a", 2), ("B", 3), ("c", 6) }, source.DistinctBy(x => x.Item1).ToArray());
    }

    [Fact]
    public void DistinctBy_compares_tuple_keys_with_their_default_comparer()
    {
        var source = new[] { ("A", (string?)null), ("A", null), ("A", "x"), ("a", null) };

        Assert.Equal(new[] { ("A", (string?)null), ("A", "x"), ("a", null) }, source.DistinctBy(x => x).ToArray());
    }

    [Fact]
    public void DistinctBy_reads_the_source_when_the_result_is_enumerated()
    {
        var source = new List<int> { 1 };
        var distinct = source.DistinctBy(x => x);
        source.Add(2);

        Assert.Equal(new[] { 1, 2 }, distinct.ToArray());
    }

    [Fact]
    public void DistinctBy_checks_its_arguments_at_the_call()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<int>)null!).DistinctBy(x => x));
        Assert.Throws<ArgumentNullException>(() => new[] { 1 }.DistinctBy<int, int>(null!));
    }

    [Fact]
    public void DistinctBy_of_nothing_is_empty()
    {
        Assert.Empty(Array.Empty<int>().DistinctBy(x => x));
        Assert.Empty(new List<int>().DistinctBy(x => x));
    }
}
