namespace System;

/// <summary>
/// The <see cref="Math"/> member .NET Framework 4.8 does not have, with every numeric overload
/// .NET 10 has, so a call binds the same overload it binds on .NET 10 and gives back the same
/// type.
/// </summary>
internal static class FrameworkMath
{
    extension(Math)
    {
        // Each overload: min greater than max throws ArgumentException; a value below min
        // gives min, above max gives max, and otherwise the value itself, so a NaN passes
        // through unchanged.

        internal static byte Clamp(byte value, byte min, byte max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }

        internal static decimal Clamp(decimal value, decimal min, decimal max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }

        internal static double Clamp(double value, double min, double max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }

        internal static short Clamp(short value, short min, short max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }

        internal static int Clamp(int value, int min, int max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }

        internal static long Clamp(long value, long min, long max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }

        internal static nint Clamp(nint value, nint min, nint max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }

        internal static sbyte Clamp(sbyte value, sbyte min, sbyte max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }

        internal static float Clamp(float value, float min, float max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }

        internal static ushort Clamp(ushort value, ushort min, ushort max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }

        internal static uint Clamp(uint value, uint min, uint max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }

        internal static ulong Clamp(ulong value, ulong min, ulong max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }

        internal static nuint Clamp(nuint value, nuint min, nuint max)
        {
            if (min > max) ThrowMinMaxException(min, max);

            if (value < min) return min;
            else if (value > max) return max;

            return value;
        }
    }

    private static void ThrowMinMaxException<T>(T min, T max) =>
        throw new ArgumentException(string.Format("'{0}' cannot be greater than {1}.", min, max));
}
