using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// The check on <see cref="ArgumentException"/> that .NET Framework 4.8 does not have, throwing
/// as the .NET 10 member of the same name does, so a call such as
/// <c>ArgumentException.ThrowIfNullOrEmpty(path)</c> binds here unchanged.
/// </summary>
internal static class FrameworkArgumentException
{
    extension(ArgumentException)
    {
        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> where <paramref name="argument"/> is
        /// null and an <see cref="ArgumentException"/> where it is empty, each naming
        /// <paramref name="paramName"/>, the expression the caller passed.
        /// </summary>
        public static void ThrowIfNullOrEmpty(
            [NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null) throw new ArgumentNullException(paramName);
            if (argument.Length == 0) throw new ArgumentException("The value cannot be an empty string.", paramName);
        }
    }
}
