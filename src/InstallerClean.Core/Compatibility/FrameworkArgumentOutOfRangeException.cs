using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// The two range checks on <see cref="ArgumentOutOfRangeException"/> that .NET Framework 4.8
/// does not have, each throwing as the .NET 10 member of the same name does, so a call such as
/// <c>ArgumentOutOfRangeException.ThrowIfLessThan(value, 0)</c> binds here unchanged.
/// </summary>
/// <remarks>
/// The exception carries the parameter name, the value and .NET 10's English message. Its
/// <see cref="Exception.Message"/> is composed from those by .NET Framework, which adds the
/// parameter name and the value on lines of their own in its own words.
/// </remarks>
internal static class FrameworkArgumentOutOfRangeException
{
    extension(ArgumentOutOfRangeException)
    {
        /// <summary>
        /// Throws where <paramref name="value"/> is less than <paramref name="other"/>; a
        /// value equal to it passes.
        /// </summary>
        internal static void ThrowIfLessThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) < 0)
                throw new ArgumentOutOfRangeException(
                    paramName, value, string.Format("{0} ('{1}') must be greater than or equal to '{2}'.", paramName, value, other));
        }

        /// <summary>
        /// Throws where <paramref name="value"/> is greater than <paramref name="other"/>; a
        /// value equal to it passes.
        /// </summary>
        internal static void ThrowIfGreaterThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) > 0)
                throw new ArgumentOutOfRangeException(
                    paramName, value, string.Format("{0} ('{1}') must be less than or equal to '{2}'.", paramName, value, other));
        }
    }
}
