using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using InstallerClean.Compatibility;

namespace InstallerClean.Helpers;

/// <summary>
/// The order a list of <see cref="SortDescription"/>s gives a list view, as .NET 10's WPF
/// sorts by one (dotnet/wpf v10.0.0 <c>SortFieldComparer</c>, with the
/// <c>System.Collections.Comparer</c> it makes for the view's culture): each key in turn,
/// reversed where it is descending, until one differs. Two strings compare in the view's
/// culture through <see cref="FrameworkCollation"/>, which is .NET 10's collation on
/// Windows; .NET Framework's comparer collates through NLS alone. A null sorts ahead of
/// every other value, and any other value compares as its own type compares.
/// </summary>
/// <remarks>
/// Each row's keys are read once and kept, as WPF keeps them for a SortDescription, so a
/// key worked out from another value, a file name from its path, is worked out once per
/// row rather than at every comparison. The rows are told apart by reference: they are
/// records, and two rows with equal values are still two rows.
/// </remarks>
internal sealed class CollatedSort<T> : IComparer where T : class
{
    private readonly (Func<T, object?> Read, bool Descending)[] _keys;
    private readonly Func<string?, string?, int> _collate;
    private readonly Dictionary<T, object?[]> _read = new(ByReference.Instance);

    /// <param name="culture">
    /// The view's culture, which WPF takes from the list's language; null, as WPF reads a
    /// view with none, is the invariant culture.
    /// </param>
    /// <param name="keys">Each key's value for a row, and its direction, first key first.</param>
    public CollatedSort(CultureInfo? culture, params (Func<T, object?> Read, ListSortDirection Direction)[] keys)
    {
        _collate = FrameworkCollation.For(culture ?? CultureInfo.InvariantCulture, CompareOptions.None);
        _keys = Array.ConvertAll(keys, k => (k.Read, k.Direction == ListSortDirection.Descending));
    }

    public int Compare(object? x, object? y)
    {
        var first = Read((T)x!);
        var second = Read((T)y!);
        for (var k = 0; k < _keys.Length; k++)
        {
            var result = CompareValues(first[k], second[k]);
            if (_keys[k].Descending) result = -result;
            if (result != 0) return result;
        }
        return 0;
    }

    // System.Collections.Comparer's Compare, with strings collated as .NET 10 collates them.
    private int CompareValues(object? a, object? b)
    {
        if (a == b) return 0;
        if (a is null) return -1;
        if (b is null) return 1;
        if (a is string sa && b is string sb)
            return _collate(sa, sb);
        if (a is IComparable ia) return ia.CompareTo(b);
        if (b is IComparable ib) return -ib.CompareTo(a);
        throw new ArgumentException("At least one object must implement IComparable.");
    }

    private object?[] Read(T row)
    {
        if (_read.TryGetValue(row, out var values)) return values;
        values = new object?[_keys.Length];
        for (var k = 0; k < _keys.Length; k++) values[k] = _keys[k].Read(row);
        _read[row] = values;
        return values;
    }

    private sealed class ByReference : IEqualityComparer<T>
    {
        public static readonly ByReference Instance = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
