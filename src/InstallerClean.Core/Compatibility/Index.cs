using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// .NET's index into a sequence, counted from the start or from the end, as the C# compiler
/// makes it for <c>^1</c> and for an <c>int</c> used where an index is expected. .NET Framework
/// 4.8 does not define it, and the compiler binds it by its full name, so this internal copy
/// serves InstallerClean.Core and every assembly that sees Core's internals. Every member
/// behaves as .NET 10's does.
/// </summary>
internal readonly struct Index : IEquatable<Index>
{
    // A value from the start is held as itself and a value from the end as its bitwise
    // complement, so the sign says which it is and both share one field.
    private readonly int _value;

    /// <summary>
    /// An index <paramref name="value"/> places from the start, or from the end when
    /// <paramref name="fromEnd"/> is true, where 1 from the end is the last element and 0 from
    /// the end is just past it. A negative value throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Index(int value, bool fromEnd = false)
    {
        if (value < 0) ThrowValueNegative();

        _value = fromEnd ? ~value : value;
    }

    private Index(int value)
    {
        _value = value;
    }

    /// <summary>The first element.</summary>
    public static Index Start => new(0);

    /// <summary>Just past the last element.</summary>
    public static Index End => new(~0);

    /// <summary>
    /// The index <paramref name="value"/> places from the start. A negative value throws
    /// <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromStart(int value)
    {
        if (value < 0) ThrowValueNegative();

        return new Index(value);
    }

    /// <summary>
    /// The index <paramref name="value"/> places from the end. A negative value throws
    /// <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromEnd(int value)
    {
        if (value < 0) ThrowValueNegative();

        return new Index(~value);
    }

    /// <summary>The number of places, from whichever end <see cref="IsFromEnd"/> names.</summary>
    public int Value => _value < 0 ? ~_value : _value;

    /// <summary>Whether the index counts from the end.</summary>
    public bool IsFromEnd => _value < 0;

    /// <summary>
    /// The position from the start in a sequence of <paramref name="length"/> elements. Neither
    /// the length nor the result is checked: a position outside the sequence fails where it is
    /// used.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOffset(int length)
    {
        int offset = _value;

        // From the end, length - ~_value, which is length + _value + 1.
        if (IsFromEnd) offset += length + 1;

        return offset;
    }

    public override bool Equals([NotNullWhen(true)] object? value) => value is Index other && _value == other._value;

    public bool Equals(Index other) => _value == other._value;

    public override int GetHashCode() => _value;

    /// <summary>The index <paramref name="value"/> places from the start.</summary>
    public static implicit operator Index(int value) => FromStart(value);

    /// <summary>The number of places, after a '^' when the index counts from the end.</summary>
    public override string ToString() => IsFromEnd ? "^" + Value.ToString() : ((uint)Value).ToString();

    private static void ThrowValueNegative() =>
        throw new ArgumentOutOfRangeException("value", "Non-negative number required.");
}
