using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// .NET's range of a sequence, from a start index up to but not including an end index, as the
/// C# compiler makes it for <c>a..b</c>. .NET Framework 4.8 does not define it, and the compiler
/// binds it by its full name, so this internal copy serves InstallerClean.Core and every
/// assembly that sees Core's internals. Every member behaves as .NET 10's does, except that
/// <see cref="GetHashCode"/> gives the same value in every process.
/// </summary>
internal readonly struct Range : IEquatable<Range>
{
    public Range(Index start, Index end)
    {
        Start = start;
        End = end;
    }

    /// <summary>The first element in the range.</summary>
    public Index Start { get; }

    /// <summary>The element just past the range.</summary>
    public Index End { get; }

    /// <summary>From <paramref name="start"/> to the end of the sequence.</summary>
    public static Range StartAt(Index start) => new(start, Index.End);

    /// <summary>From the start of the sequence up to <paramref name="end"/>.</summary>
    public static Range EndAt(Index end) => new(Index.Start, end);

    /// <summary>The whole sequence.</summary>
    public static Range All => new(Index.Start, Index.End);

    /// <summary>
    /// The position of the range's first element and the number of elements it covers, in a
    /// sequence of <paramref name="length"/> elements. A range ending past the sequence, or
    /// starting after its own end, throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int Offset, int Length) GetOffsetAndLength(int length)
    {
        int start = Start.IsFromEnd ? length - Start.Value : Start.Value;
        int end = End.IsFromEnd ? length - End.Value : End.Value;

        // Compared unsigned, a negative position counts as past the end.
        if ((uint)end > (uint)length || (uint)start > (uint)end)
        {
            ThrowLengthOutOfRange();
        }

        return (start, end - start);
    }

    public override bool Equals([NotNullWhen(true)] object? value) =>
        value is Range other && other.Start.Equals(Start) && other.End.Equals(End);

    public bool Equals(Range other) => other.Start.Equals(Start) && other.End.Equals(End);

    // Equal ranges give equal hashes: the start's hash rotated left five bits, added to itself,
    // and exclusive-or'd with the end's.
    public override int GetHashCode()
    {
        int h1 = Start.GetHashCode();
        uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);

        return ((int)rol5 + h1) ^ End.GetHashCode();
    }

    /// <summary>The start and the end, as <see cref="Index.ToString"/> writes them, joined by "..".</summary>
    public override string ToString() => Start.ToString() + ".." + End.ToString();

    private static void ThrowLengthOutOfRange() => throw new ArgumentOutOfRangeException("length");
}
