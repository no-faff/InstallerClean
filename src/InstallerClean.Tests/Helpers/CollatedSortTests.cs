using System.Collections;
using System.Globalization;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// <see cref="CollatedSort{T}"/> orders a list view's rows as .NET 10's WPF orders them for a
/// list of SortDescriptions: key by key, each reversed where it is descending, names
/// collated in the view's culture as .NET 10 collates them, so "a_b" sorts ahead of "a)b"
/// where .NET Framework's NLS comparer puts it after.
/// </summary>
public class CollatedSortTests
{
    private sealed class Row(string? name, long size, string path)
    {
        public int NameReads { get; private set; }

        public string? Name
        {
            get { NameReads++; return name; }
        }

        public long Size => size;

        public string Path => path;
    }

    private static string[] Sorted(IComparer comparer, params Row[] rows)
    {
        var list = new ArrayList(rows);
        list.Sort(comparer);
        return list.Cast<Row>().Select(r => r.Path).ToArray();
    }

    [Fact]
    public void Names_sort_in_the_culture_as_NET_10_sorts_them_and_ties_fall_to_the_next_key()
    {
        var sort = new CollatedSort<Row>(
            CultureInfo.GetCultureInfo("en-GB"),
            (r => r.Name, false),
            (r => r.Path, false));

        var order = Sorted(sort,
            new Row("a)b", 1, "p3"), new Row("a_b", 1, "p2"), new Row("a_b", 1, "p1"), new Row("B", 1, "p4"));

        Assert.Equal(new[] { "p1", "p2", "p3", "p4" }, order);
    }

    [Fact]
    public void A_descending_key_reverses_its_own_order_only()
    {
        var sort = new CollatedSort<Row>(
            CultureInfo.GetCultureInfo("en-GB"),
            (r => r.Size, true),
            (r => r.Path, false));

        var order = Sorted(sort, new Row("x", 1, "b"), new Row("x", 5, "c"), new Row("x", 1, "a"));

        Assert.Equal(new[] { "c", "a", "b" }, order);
    }

    [Fact]
    public void A_missing_name_sorts_ahead_of_every_name()
    {
        var sort = new CollatedSort<Row>(null, (r => r.Name, false));

        var order = Sorted(sort, new Row("a", 1, "named"), new Row(null, 1, "unnamed"));

        Assert.Equal(new[] { "unnamed", "named" }, order);
    }

    [Fact]
    public void Each_row_is_read_once_however_many_comparisons_it_takes_part_in()
    {
        var rows = Enumerable.Range(0, 200).Select(i => new Row((199 - i).ToString("D3"), 1, i.ToString())).ToArray();
        var sort = new CollatedSort<Row>(CultureInfo.InvariantCulture, (r => r.Name, false));

        Sorted(sort, rows);

        Assert.All(rows, r => Assert.Equal(1, r.NameReads));
    }

    private sealed record Item(string Path);

    [Fact]
    public void Two_rows_with_equal_values_are_still_two_rows()
    {
        // The rows are records, which compare by value, so a key kept for one row must not
        // be handed to an equal one.
        var reads = 0;
        var sort = new CollatedSort<Item>(null, (i => ++reads, false));
        var first = new Item("same");
        var second = new Item("same");

        Assert.Equal(first, second);
        Assert.True(sort.Compare(first, second) < 0);
        Assert.Equal(2, reads);
    }
}
