namespace System;

/// <summary>
/// The two generic <see cref="Enum"/> members .NET Framework 4.8 does not have, each answering
/// as the .NET 10 member of the same name does, so a call such as
/// <c>Enum.GetValues&lt;T&gt;()</c> binds here unchanged.
/// </summary>
internal static class FrameworkEnum
{
    extension(Enum)
    {
        /// <summary>
        /// A new array of the values <typeparamref name="TEnum"/> declares, one per declared
        /// name and so including any value declared twice, in order of the value read as an
        /// unsigned number.
        /// </summary>
        internal static TEnum[] GetValues<TEnum>() where TEnum : struct, Enum =>
            (TEnum[])Enum.GetValues(typeof(TEnum));

        /// <summary>
        /// Whether <paramref name="value"/> is a value <typeparamref name="TEnum"/> declares.
        /// A combination of flags is defined only where a name declares that combination.
        /// </summary>
        internal static bool IsDefined<TEnum>(TEnum value) where TEnum : struct, Enum =>
            Enum.IsDefined(typeof(TEnum), value);
    }
}
