namespace System;

/// <summary>
/// The two <see cref="Array"/> members .NET Framework 4.8 does not have, each doing what the
/// .NET 10 member of the same name does, so a call written for .NET 10 binds here unchanged.
/// </summary>
internal static class FrameworkArray
{
    extension(Array)
    {
        /// <summary>
        /// Sets every element of <paramref name="array"/> to its type's default, whatever its
        /// rank and lower bounds.
        /// </summary>
        internal static void Clear(Array array)
        {
            if (array is null) throw new ArgumentNullException(nameof(array));

            Array.Clear(array, array.GetLowerBound(0), array.Length);
        }

        /// <summary>
        /// Stores <paramref name="value"/> in every element of <paramref name="array"/>. Each
        /// element is assigned in turn, so an array of a derived type refuses a value it
        /// cannot hold with ArrayTypeMismatchException.
        /// </summary>
        internal static void Fill<T>(T[] array, T value)
        {
            if (array is null) throw new ArgumentNullException(nameof(array));

            for (var i = 0; i < array.Length; i++)
                array[i] = value;
        }
    }
}
