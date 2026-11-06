using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic;

/// <summary>
/// The dictionary and pair members .NET Framework 4.8 does not have, each doing what the
/// .NET 10 member of the same name does, so a call written for .NET 10 binds here unchanged.
/// </summary>
internal static class FrameworkCollectionExtensions
{
    /// <summary>
    /// Adds <paramref name="value"/> under <paramref name="key"/> where the dictionary has no
    /// entry for the key, and answers whether it did. An entry already there keeps its value.
    /// </summary>
    internal static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));

        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
            return true;
        }

        return false;
    }

    /// <summary>The value under <paramref name="key"/>, or the type's default where there is none.</summary>
    internal static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) =>
        dictionary.GetValueOrDefault(key, default!);

    /// <summary>The value under <paramref name="key"/>, or <paramref name="defaultValue"/> where there is none.</summary>
    internal static TValue GetValueOrDefault<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
    {
        if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));

        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Removes the entry under <paramref name="key"/> and hands back its value in
    /// <paramref name="value"/>, answering true; where there is none, answers false with the
    /// type's default.
    /// </summary>
    internal static bool Remove<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary, TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));

        if (dictionary.TryGetValue(key, out value))
        {
            dictionary.Remove(key);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>The pair's key and value, for a deconstruction such as <c>foreach (var (key, value) in dictionary)</c>.</summary>
    internal static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}
