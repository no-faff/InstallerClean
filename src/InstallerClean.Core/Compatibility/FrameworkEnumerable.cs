namespace System.Linq;

/// <summary>
/// The sequence member .NET Framework 4.8 does not have, doing what the .NET 10 member of the
/// same name does, so a call written for .NET 10 binds here unchanged.
/// </summary>
internal static class FrameworkEnumerable
{
    /// <summary>
    /// The elements of <paramref name="source"/> whose key, from <paramref name="keySelector"/>
    /// and compared with the key type's default comparer, has not come before: the first element
    /// for each key, in the order the source gives them. The arguments are checked at the call,
    /// and the source is read as the result is enumerated.
    /// </summary>
    internal static IEnumerable<TSource> DistinctBy<TSource, TKey>(
        this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

        if (source is TSource[] { Length: 0 }) return [];

        return DistinctByIterator(source, keySelector);
    }

    private static IEnumerable<TSource> DistinctByIterator<TSource, TKey>(
        IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        using var enumerator = source.GetEnumerator();

        if (enumerator.MoveNext())
        {
            var set = new HashSet<TKey>();
            do
            {
                var element = enumerator.Current;
                if (set.Add(keySelector(element)))
                    yield return element;
            }
            while (enumerator.MoveNext());
        }
    }
}
