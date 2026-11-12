using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace InstallerClean.Compatibility;

/// <summary>
/// <c>CompareInfo.Compare(string, string, CompareOptions)</c> as .NET 10 gives it on
/// Windows, for the two option sets the app sorts with, <see cref="CompareOptions.None"/> and
/// <see cref="CompareOptions.IgnoreCase"/> (dotnet/runtime v10.0.0: <c>CompareInfo.cs</c>'s
/// <c>Compare</c>, <c>CompareInfo.Icu.cs</c>, <c>CultureData.Icu.cs</c>'s
/// <c>InitIcuCultureDataCore</c>, and in <c>src/native/libs/System.Globalization.Native</c>,
/// <c>pal_collation.c</c>'s <c>GlobalizationNative_GetSortHandle</c>,
/// <c>CloneCollatorWithOptions</c> and <c>GlobalizationNative_CompareString</c>, and
/// <c>pal_locale.c</c>'s <c>GlobalizationNative_GetLocaleName</c>): through the ICU collator
/// Windows ships where <see cref="WindowsIcu.Loaded"/> has one, and through NLS where it
/// does not. .NET Framework compares through NLS alone, which orders punctuation and
/// whole scripts otherwise: ICU puts '_' ahead of ')', and NLS ')' ahead of '_'.
/// </summary>
internal static unsafe class FrameworkCollation
{
    // UColAttribute and UColAttributeValue, as ICU's ucol.h numbers them.
    private const int Strength = 5;
    private const int DefaultValue = -1;
    private const int Secondary = 1;

    private static readonly Lazy<Icu?> Loaded = new(() => WindowsIcu.Loaded is { } icu ? Icu.Load(icu) : null);

    /// <summary>
    /// Compares <paramref name="first"/> with <paramref name="second"/> in
    /// <paramref name="culture"/>'s sort order: less than zero where the first sorts first,
    /// zero where they sort together, more than zero where the second does. A null string
    /// sorts ahead of every other, and two nulls together.
    /// </summary>
    /// <exception cref="ArgumentException">Options other than None and IgnoreCase.</exception>
    public static int Compare(string? first, string? second, CultureInfo culture, CompareOptions options) =>
        For(culture, options, Loaded.Value)(first, second);

    /// <summary>
    /// <see cref="Compare(string, string, CultureInfo, CompareOptions)"/> with the culture's
    /// collator found once, for a sort that compares many times in one culture.
    /// </summary>
    /// <exception cref="ArgumentException">Options other than None and IgnoreCase.</exception>
    public static Func<string?, string?, int> For(CultureInfo culture, CompareOptions options) =>
        For(culture, options, Loaded.Value);

    internal static Func<string?, string?, int> For(CultureInfo culture, CompareOptions options, Icu? icu)
    {
        if (options is not (CompareOptions.None or CompareOptions.IgnoreCase))
            throw new ArgumentException($"CompareOptions.{options} is not ported to .NET Framework: only None and IgnoreCase sort as .NET 10 does.", nameof(options));

        var collator = icu?.Collator(culture.Name, options);
        return (first, second) =>
        {
            if (first is null) return second is null ? 0 : -1;
            if (second is null) return 1;
            if (ReferenceEquals(first, second)) return 0;
            if (collator is { } c) return icu!.Compare(c, first, second);

            // NLS, as .NET 10 compares where Windows has no ICU: CompareStringEx with the
            // culture's sort and NORM_LINGUISTIC_CASING, adding NORM_IGNORECASE for
            // IgnoreCase, which is what .NET Framework's CompareInfo calls too.
#pragma warning disable RS0030
            return culture.CompareInfo.Compare(first, second, options);
#pragma warning restore RS0030
        };
    }

    /// <summary>
    /// <paramref name="cultureName"/> as the locale name .NET 10 opens its collator for: an
    /// alternative sort after '_' written as ICU's collation keyword, the whole canonicalised
    /// by ICU, and ICU's '_' between the parts written as '-'.
    /// </summary>
    internal static string? LocaleName(string cultureName, Func<string, string?> canonicalise)
    {
        var sort = cultureName.IndexOf('_');
        var name = sort > 0
            ? cultureName.Substring(0, sort) + "@collation=" + cultureName.Substring(sort + 1)
            : cultureName;
        return canonicalise(name)?.Replace('_', '-');
    }

    /// <summary>
    /// The rules .NET 10 adds to a locale's own for <see cref="CompareOptions.IgnoreCase"/>
    /// (<c>CloneCollatorWithOptions</c>, with <c>FillIgnoreKanaRules</c> and
    /// <c>FillIgnoreWidthRules</c>). Ignoring case lowers the strength to secondary, where
    /// ICU no longer tells kana types or widths apart, so the rules put each katakana after
    /// its hiragana and each full or half width form after its other form, then tie each
    /// full-width capital to its small letter, and end with a rule that changes nothing so
    /// ICU applies the one before it.
    /// </summary>
    internal static string IgnoreCaseRules(string localeRules)
    {
        var rules = new StringBuilder(localeRules, localeRules.Length + 1600);

        foreach (var hiragana in HiraganaWithoutVoicedSoundMark)
            rules.Append('&').Append(hiragana).Append('<').Append((char)(hiragana + HiraganaToKatakana));

        for (var i = 0; i < HalfFullLower.Length; i++)
        {
            rules.Append('&');
            if (NeedsEscape(HalfFullLower[i])) rules.Append('\\');
            rules.Append(HalfFullLower[i]).Append('<').Append(HalfFullHigher[i]);
        }

        for (var capital = '\uFF21'; capital <= '\uFF3A'; capital++)
            rules.Append('&').Append((char)(capital + ('\uFF41' - '\uFF21'))).Append('=').Append(capital);

        return rules.Append("&a=a").ToString();
    }

    /// <summary>The ASCII punctuation ICU's rule syntax reserves, which a rule escapes.</summary>
    private static bool NeedsEscape(char c) =>
        c is (>= '!' and <= '/') or (>= ':' and <= '@')
            or (>= '[' and <= '`') or (>= '{' and <= '~');

    private const int HiraganaToKatakana = 0x30A1 - 0x3041;

    /// <summary>
    /// The hiragana .NET 10 ties to their katakana: those without a voiced or semi-voiced
    /// sound mark, whose rules would clash with ICU's own (<c>g_HiraganaWithoutVoicedSoundMarkChars</c>).
    /// </summary>
    internal static readonly char[] HiraganaWithoutVoicedSoundMark =
    [
        '\u3041', '\u3042', '\u3043', '\u3044', '\u3045', '\u3046', '\u3047', '\u3048', '\u3049', '\u304A', '\u304B', '\u304D', '\u304F', '\u3051', '\u3053',
        '\u3055', '\u3057', '\u3059', '\u305B', '\u305D', '\u305F', '\u3061', '\u3063', '\u3064', '\u3066', '\u3068', '\u306A', '\u306B', '\u306C', '\u306D',
        '\u306E', '\u306F', '\u3072', '\u3075', '\u3078', '\u307B', '\u307E', '\u307F', '\u3080', '\u3081', '\u3082', '\u3083', '\u3084', '\u3085', '\u3086',
        '\u3087', '\u3088', '\u3089', '\u308A', '\u308B', '\u308C', '\u308D', '\u308E', '\u308F', '\u3090', '\u3091', '\u3092', '\u3093', '\u3095', '\u3096',
        '\u309D',
    ];

    /// <summary>
    /// Each character that sorts ahead of its other-width form in <see cref="HalfFullHigher"/>,
    /// at the same index (<c>g_HalfFullLowerChars</c>): ASCII and a few signs ahead of their
    /// full-width forms, then full-width kana and Hangul ahead of their half-width forms.
    /// </summary>
    internal static readonly char[] HalfFullLower =
    [
        '\u0021', '\u0022', '\u0023', '\u0024', '\u0025', '\u0026', '\u0027', '\u0028', '\u0029', '\u002A', '\u002B', '\u002C', '\u002D', '\u002E', '\u002F',
        '\u0030', '\u0031', '\u0032', '\u0033', '\u0034', '\u0035', '\u0036', '\u0037', '\u0038', '\u0039', '\u003A', '\u003B', '\u003C', '\u003D', '\u003E',
        '\u003F', '\u0040', '\u0041', '\u0042', '\u0043', '\u0044', '\u0045', '\u0046', '\u0047', '\u0048', '\u0049', '\u004A', '\u004B', '\u004C', '\u004D',
        '\u004E', '\u004F', '\u0050', '\u0051', '\u0052', '\u0053', '\u0054', '\u0055', '\u0056', '\u0057', '\u0058', '\u0059', '\u005A', '\u005B', '\u005D',
        '\u005E', '\u005F', '\u0060', '\u0061', '\u0062', '\u0063', '\u0064', '\u0065', '\u0066', '\u0067', '\u0068', '\u0069', '\u006A', '\u006B', '\u006C',
        '\u006D', '\u006E', '\u006F', '\u0070', '\u0071', '\u0072', '\u0073', '\u0074', '\u0075', '\u0076', '\u0077', '\u0078', '\u0079', '\u007A', '\u007B',
        '\u007C', '\u007D', '\u007E', '\u00A2', '\u00A3', '\u00AC', '\u00AF', '\u00A6', '\u00A5', '\u20A9',

        '\u3002', '\u300C', '\u300D', '\u3001', '\u30FB', '\u30F2', '\u30A1', '\u30A3', '\u30A5', '\u30A7', '\u30A9', '\u30E3', '\u30E5', '\u30E7', '\u30C3',
        '\u30A2', '\u30A4', '\u30A6', '\u30A8', '\u30AA', '\u30AB', '\u30AD', '\u30AF', '\u30B1', '\u30B3', '\u30B5', '\u30B7', '\u30B9', '\u30BB', '\u30BD',
        '\u30BF', '\u30C1', '\u30C4', '\u30C6', '\u30C8', '\u30CA', '\u30CB', '\u30CC', '\u30CD', '\u30CE', '\u30CF', '\u30D2', '\u30D5', '\u30D8', '\u30DB',
        '\u30DE', '\u30DF', '\u30E0', '\u30E1', '\u30E2', '\u30E4', '\u30E6', '\u30E8', '\u30E9', '\u30EA', '\u30EB', '\u30EC', '\u30ED', '\u30EF', '\u30F3',
        '\u3164', '\u3131', '\u3132', '\u3133', '\u3134', '\u3135', '\u3136', '\u3137', '\u3138', '\u3139', '\u313A', '\u313B', '\u313C', '\u313D', '\u313E',
        '\u313F', '\u3140', '\u3141', '\u3142', '\u3143', '\u3144', '\u3145', '\u3146', '\u3147', '\u3148', '\u3149', '\u314A', '\u314B', '\u314C', '\u314D',
        '\u314E', '\u314F', '\u3150', '\u3151', '\u3152', '\u3153', '\u3154', '\u3155', '\u3156', '\u3157', '\u3158', '\u3159', '\u315A', '\u315B', '\u315C',
        '\u315D', '\u315E', '\u315F', '\u3160', '\u3161', '\u3162', '\u3163',
    ];

    /// <summary>The other-width form of each character in <see cref="HalfFullLower"/> (<c>g_HalfFullHigherChars</c>).</summary>
    internal static readonly char[] HalfFullHigher =
    [
        '\uFF01', '\uFF02', '\uFF03', '\uFF04', '\uFF05', '\uFF06', '\uFF07', '\uFF08', '\uFF09', '\uFF0A', '\uFF0B', '\uFF0C', '\uFF0D', '\uFF0E', '\uFF0F',
        '\uFF10', '\uFF11', '\uFF12', '\uFF13', '\uFF14', '\uFF15', '\uFF16', '\uFF17', '\uFF18', '\uFF19', '\uFF1A', '\uFF1B', '\uFF1C', '\uFF1D', '\uFF1E',
        '\uFF1F', '\uFF20', '\uFF21', '\uFF22', '\uFF23', '\uFF24', '\uFF25', '\uFF26', '\uFF27', '\uFF28', '\uFF29', '\uFF2A', '\uFF2B', '\uFF2C', '\uFF2D',
        '\uFF2E', '\uFF2F', '\uFF30', '\uFF31', '\uFF32', '\uFF33', '\uFF34', '\uFF35', '\uFF36', '\uFF37', '\uFF38', '\uFF39', '\uFF3A', '\uFF3B', '\uFF3D',
        '\uFF3E', '\uFF3F', '\uFF40', '\uFF41', '\uFF42', '\uFF43', '\uFF44', '\uFF45', '\uFF46', '\uFF47', '\uFF48', '\uFF49', '\uFF4A', '\uFF4B', '\uFF4C',
        '\uFF4D', '\uFF4E', '\uFF4F', '\uFF50', '\uFF51', '\uFF52', '\uFF53', '\uFF54', '\uFF55', '\uFF56', '\uFF57', '\uFF58', '\uFF59', '\uFF5A', '\uFF5B',
        '\uFF5C', '\uFF5D', '\uFF5E', '\uFFE0', '\uFFE1', '\uFFE2', '\uFFE3', '\uFFE4', '\uFFE5', '\uFFE6',

        '\uFF61', '\uFF62', '\uFF63', '\uFF64', '\uFF65', '\uFF66', '\uFF67', '\uFF68', '\uFF69', '\uFF6A', '\uFF6B', '\uFF6C', '\uFF6D', '\uFF6E', '\uFF6F',
        '\uFF71', '\uFF72', '\uFF73', '\uFF74', '\uFF75', '\uFF76', '\uFF77', '\uFF78', '\uFF79', '\uFF7A', '\uFF7B', '\uFF7C', '\uFF7D', '\uFF7E', '\uFF7F',
        '\uFF80', '\uFF81', '\uFF82', '\uFF83', '\uFF84', '\uFF85', '\uFF86', '\uFF87', '\uFF88', '\uFF89', '\uFF8A', '\uFF8B', '\uFF8C', '\uFF8D', '\uFF8E',
        '\uFF8F', '\uFF90', '\uFF91', '\uFF92', '\uFF93', '\uFF94', '\uFF95', '\uFF96', '\uFF97', '\uFF98', '\uFF99', '\uFF9A', '\uFF9B', '\uFF9C', '\uFF9D',
        '\uFFA0', '\uFFA1', '\uFFA2', '\uFFA3', '\uFFA4', '\uFFA5', '\uFFA6', '\uFFA7', '\uFFA8', '\uFFA9', '\uFFAA', '\uFFAB', '\uFFAC', '\uFFAD', '\uFFAE',
        '\uFFAF', '\uFFB0', '\uFFB1', '\uFFB2', '\uFFB3', '\uFFB4', '\uFFB5', '\uFFB6', '\uFFB7', '\uFFB8', '\uFFB9', '\uFFBA', '\uFFBB', '\uFFBC', '\uFFBD',
        '\uFFBE', '\uFFC2', '\uFFC3', '\uFFC4', '\uFFC5', '\uFFC6', '\uFFC7', '\uFFCA', '\uFFCB', '\uFFCC', '\uFFCD', '\uFFCE', '\uFFCF', '\uFFD2', '\uFFD3',
        '\uFFD4', '\uFFD5', '\uFFD6', '\uFFD7', '\uFFDA', '\uFFDB', '\uFFDC',
    ];

    /// <summary>
    /// The ICU collation functions, from the library <see cref="WindowsIcu"/> loaded, and the
    /// collators opened through them: one per locale and option set, kept for the life of the
    /// process as .NET 10's sort handles are. ICU's collators compare safely from several
    /// threads at once.
    /// </summary>
    internal sealed class Icu
    {
        private readonly delegate* unmanaged[Cdecl]<byte*, byte*, int, int*, int> _canonicalize;
        private readonly delegate* unmanaged[Cdecl]<byte*, byte*, int, int*, int> _getLanguage;
        private readonly delegate* unmanaged[Cdecl]<byte*, int*, IntPtr> _open;
        private readonly delegate* unmanaged[Cdecl]<char*, int, int, int, void*, int*, IntPtr> _openRules;
        private readonly delegate* unmanaged[Cdecl]<IntPtr, int*, char*> _getRules;
        private readonly delegate* unmanaged[Cdecl]<IntPtr, int, int, int*, void> _setAttribute;
        private readonly delegate* unmanaged[Cdecl]<IntPtr, char*, int, char*, int, int> _strcoll;
        private readonly ConcurrentDictionary<(string Culture, CompareOptions Options), IntPtr> _collators = new();

        private Icu(IntPtr canonicalize, IntPtr getLanguage, IntPtr open, IntPtr openRules, IntPtr getRules, IntPtr setAttribute, IntPtr strcoll)
        {
            _canonicalize = (delegate* unmanaged[Cdecl]<byte*, byte*, int, int*, int>)canonicalize;
            _getLanguage = (delegate* unmanaged[Cdecl]<byte*, byte*, int, int*, int>)getLanguage;
            _open = (delegate* unmanaged[Cdecl]<byte*, int*, IntPtr>)open;
            _openRules = (delegate* unmanaged[Cdecl]<char*, int, int, int, void*, int*, IntPtr>)openRules;
            _getRules = (delegate* unmanaged[Cdecl]<IntPtr, int*, char*>)getRules;
            _setAttribute = (delegate* unmanaged[Cdecl]<IntPtr, int, int, int*, void>)setAttribute;
            _strcoll = (delegate* unmanaged[Cdecl]<IntPtr, char*, int, char*, int, int>)strcoll;
        }

        /// <summary>The functions from <paramref name="icu"/>, or null where one is missing.</summary>
        internal static Icu? Load(WindowsIcu icu)
        {
            var canonicalize = WindowsIcu.Export(icu.Common, "uloc_canonicalize");
            var getLanguage = WindowsIcu.Export(icu.Common, "uloc_getLanguage");
            var open = WindowsIcu.Export(icu.Internationalisation, "ucol_open");
            var openRules = WindowsIcu.Export(icu.Internationalisation, "ucol_openRules");
            var getRules = WindowsIcu.Export(icu.Internationalisation, "ucol_getRules");
            var setAttribute = WindowsIcu.Export(icu.Internationalisation, "ucol_setAttribute");
            var strcoll = WindowsIcu.Export(icu.Internationalisation, "ucol_strcoll");
            return canonicalize == IntPtr.Zero || getLanguage == IntPtr.Zero || open == IntPtr.Zero || openRules == IntPtr.Zero
                || getRules == IntPtr.Zero || setAttribute == IntPtr.Zero || strcoll == IntPtr.Zero
                ? null
                : new Icu(canonicalize, getLanguage, open, openRules, getRules, setAttribute, strcoll);
        }

        /// <summary>
        /// The collator for <paramref name="cultureName"/> and <paramref name="options"/>, or
        /// null where ICU cannot open one, which the caller answers through NLS.
        /// </summary>
        public IntPtr? Collator(string cultureName, CompareOptions options)
        {
            if (!_collators.TryGetValue((cultureName, options), out var collator))
                collator = _collators.GetOrAdd((cultureName, options), Open(cultureName, options));
            return collator == IntPtr.Zero ? null : collator;
        }

        /// <summary><c>GlobalizationNative_CompareString</c>: ucol_strcoll over the two strings.</summary>
        public int Compare(IntPtr collator, string first, string second)
        {
            fixed (char* a = first)
            fixed (char* b = second)
                return _strcoll(collator, a, first.Length, b, second.Length);
        }

        // ucol_open for the locale, and for IgnoreCase a collator built from the locale
        // collator's rules and .NET 10's, at secondary strength. Zero on any failure. A
        // collator a race opened twice is left open, as the dictionary keeps one of the two.
        private IntPtr Open(string cultureName, CompareOptions options)
        {
            var status = 0;
            if (options != CompareOptions.None)
                return Collator(cultureName, CompareOptions.None) is { } locale
                    ? IgnoringCase(locale, &status)
                    : IntPtr.Zero;

            if (LocaleName(cultureName, Canonicalise) is not { } name)
                return IntPtr.Zero;
            var bytes = Encoding.ASCII.GetBytes(name + "\0");
            IntPtr collator;
            fixed (byte* n = bytes)
                collator = _open(n, &status);
            return status > 0 ? IntPtr.Zero : collator;
        }

        private IntPtr IgnoringCase(IntPtr collator, int* status)
        {
            int length;
            var own = _getRules(collator, &length);
            var rules = IgnoreCaseRules(new string(own, 0, length));
            IntPtr ignoringCase;
            fixed (char* r = rules)
                ignoringCase = _openRules(r, rules.Length, DefaultValue, Secondary, null, status);
            if (*status > 0) return IntPtr.Zero;
            _setAttribute(ignoringCase, Strength, Secondary, status);
            return *status > 0 ? IntPtr.Zero : ignoringCase;
        }

        // GetLocale with canonicalising, from pal_locale.c: the name's first 156 characters,
        // a character outside ASCII, or a '/', refused, canonicalised by ICU, and refused
        // where its language part does not fit ICU's language capacity.
        private string? Canonicalise(string name)
        {
            const int Capacity = 157;
            const int LanguageCapacity = 12;
            const int BufferOverflowError = 15;
            const int StringNotTerminatedWarning = -124;
            var source = stackalloc byte[Capacity];
            var result = stackalloc byte[Capacity];
            var language = stackalloc byte[LanguageCapacity];
            for (var i = 0; i < Capacity - 1 && i < name.Length; i++)
            {
                if (name[i] > 0x7F || name[i] == '/') return null;
                source[i] = (byte)name[i];
            }

            var status = 0;
            var length = _canonicalize(source, result, Capacity, &status);
            if (status > 0) return null;
            _getLanguage(source, language, LanguageCapacity, &status);
            if (status > 0 || status == StringNotTerminatedWarning || status == BufferOverflowError) return null;
            return new string((sbyte*)result, 0, Math.Min(length, Capacity - 1));
        }
    }
}
