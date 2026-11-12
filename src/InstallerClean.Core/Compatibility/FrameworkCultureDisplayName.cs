using System.Globalization;
using InstallerClean.Interop.Native;

namespace InstallerClean.Compatibility;

/// <summary>
/// <see cref="CultureInfo.DisplayName"/> as .NET 10 gives it on Windows (dotnet/runtime
/// v10.0.0: <c>CultureData.cs</c>'s <c>DisplayName</c> and <c>LocalizedLanguageName</c>,
/// <c>CultureData.Icu.cs</c>, <c>CultureData.Nls.cs</c>, and in
/// <c>src/native/libs/System.Globalization.Native</c>, <c>pal_icushim.c</c>'s
/// <c>FindICULibs</c> and <c>pal_localeStringData.c</c>'s
/// <c>GlobalizationNative_GetLocaleInfoString</c>): a culture's name in the language of
/// a UI culture, from the ICU Windows ships, and from NLS on a Windows with no
/// ICU, as .NET 10 falls back. .NET Framework's answers from NLS alone, in the display
/// language Windows is set to whatever the UI culture is, so a French window on English
/// Windows would name Russian "Russian".
/// </summary>
internal static unsafe class FrameworkCultureDisplayName
{
    // ICU's own limits, as .NET 10 sizes them: a locale ID, and a returned name.
    private const int FullNameCapacity = 157;
    private const int ValueCapacity = 100;

    private const int LanguageCapacity = 12;

    private const int UsingDefaultWarning = -127;
    private const int StringNotTerminatedWarning = -124;
    private const int IllegalArgumentError = 1;
    private const int BufferOverflowError = 15;

    private static readonly Lazy<Icu?> Loaded = new(Icu.Load);

    /// <summary>
    /// The display name of <paramref name="culture"/> in <paramref name="uiCulture"/>'s
    /// language. .NET 10 reads the thread's UI culture for it; the window passes the
    /// culture its own strings are drawn in.
    /// </summary>
    public static string Of(CultureInfo culture, CultureInfo uiCulture) => Of(culture, uiCulture.Name, Loaded.Value);

    internal static string Of(CultureInfo culture, string uiCultureName, Icu? icu)
    {
        var name = culture.Name;
        if (name.Length == 0)
            return culture.NativeName;

        var lookedUp =
            name.Equals("zh-CHT", StringComparison.OrdinalIgnoreCase) ? "zh-Hant" :
            name.Equals("zh-CHS", StringComparison.OrdinalIgnoreCase) ? "zh-Hans" :
            name;

        var displayName = icu is null
            ? NlsDisplayName(culture, uiCultureName)
            : icu.Name(icu.DisplayName, lookedUp, uiCultureName);
        if (displayName.Length == 0 && culture.IsNeutralCulture)
        {
            displayName = icu is null
                ? NlsLanguageName(culture, uiCultureName)
                : icu.Name(icu.DisplayLanguage, lookedUp, uiCultureName);
        }
        return displayName;
    }

    /// <summary>
    /// .NET 10's answer where Windows has no ICU (<c>NlsGetLanguageDisplayName</c>): the
    /// culture's own name where the app has set a default UI culture other than the user's
    /// regional format, and otherwise the localised display name NLS gives.
    /// </summary>
    internal static string NlsDisplayName(CultureInfo culture, string uiCultureName)
    {
        var defaultUi = CultureInfo.DefaultThreadCurrentUICulture;
        if (defaultUi is not null && UserDefaultLocaleName() is { } user && !defaultUi.Name.Equals(user))
            return culture.NativeName;

        return NlsValue(culture.Name, Kernel32.LOCALE_SLOCALIZEDDISPLAYNAME);
    }

    /// <summary>
    /// .NET 10's <c>LocalizedLanguageName</c> where Windows has no ICU: NLS's localised
    /// language name where the display language Windows is set to is the UI culture, and the
    /// language's own name otherwise.
    /// </summary>
    private static string NlsLanguageName(CultureInfo culture, string uiCultureName) =>
        UserDefaultUiCultureName() == uiCultureName
            ? NlsValue(culture.Name, Kernel32.LOCALE_SLOCALIZEDLANGUAGENAME)
            : culture.NativeName;

    // The user's own display language, as .NET 10's UserDefaultUICulture reads it. A
    // language Windows names by a number no culture has gives no name.
    private static string? UserDefaultUiCultureName()
    {
        try
        {
            return CultureInfo.GetCultureInfo(Kernel32.GetUserDefaultUILanguage()).Name;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static string NlsValue(string localeName, uint type)
    {
        var buffer = new char[530];
        var written = Kernel32.GetLocaleInfoEx(localeName, type, buffer, buffer.Length);
        return written > 0 ? new string(buffer, 0, written - 1) : string.Empty;
    }

    private static string? UserDefaultLocaleName()
    {
        var buffer = new char[85];
        var written = Kernel32.GetUserDefaultLocaleName(buffer, buffer.Length);
        return written > 0 ? new string(buffer, 0, written - 1) : null;
    }

    /// <summary>The ICU functions the lookup calls, from the icu.dll Windows ships.</summary>
    internal sealed class Icu
    {
        private readonly delegate* unmanaged[Cdecl]<byte*, byte*, int, int*, int> _getName;
        private readonly delegate* unmanaged[Cdecl]<byte*, byte*, int, int*, int> _getLanguage;

        private Icu(IntPtr getName, IntPtr getLanguage, IntPtr getDisplayName, IntPtr getDisplayLanguage)
        {
            _getName = (delegate* unmanaged[Cdecl]<byte*, byte*, int, int*, int>)getName;
            _getLanguage = (delegate* unmanaged[Cdecl]<byte*, byte*, int, int*, int>)getLanguage;
            DisplayName = (delegate* unmanaged[Cdecl]<byte*, byte*, char*, int, int*, int>)getDisplayName;
            DisplayLanguage = (delegate* unmanaged[Cdecl]<byte*, byte*, char*, int, int*, int>)getDisplayLanguage;
        }

        /// <summary>uloc_getDisplayName: a locale's full name in a display locale.</summary>
        public delegate* unmanaged[Cdecl]<byte*, byte*, char*, int, int*, int> DisplayName { get; }

        /// <summary>uloc_getDisplayLanguage: a locale's language alone in a display locale.</summary>
        public delegate* unmanaged[Cdecl]<byte*, byte*, char*, int, int*, int> DisplayLanguage { get; }

        /// <summary>
        /// The ICU Windows ships, found as .NET 10 finds it: icu.dll in System32, which
        /// Windows 10 version 1903 and later carry, or icuuc.dll with icuin.dll beside it.
        /// Null where neither is there or a function is missing, which is where .NET 10
        /// uses NLS.
        /// </summary>
        internal static Icu? Load()
        {
            var common = Kernel32.LoadLibraryEx("icu.dll", IntPtr.Zero, Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32);
            if (common == IntPtr.Zero)
            {
                common = Kernel32.LoadLibraryEx("icuuc.dll", IntPtr.Zero, Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32);
                if (common == IntPtr.Zero
                    || Kernel32.LoadLibraryEx("icuin.dll", IntPtr.Zero, Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32) == IntPtr.Zero)
                    return null;
            }

            var getName = Export(common, "uloc_getName");
            var getLanguage = Export(common, "uloc_getLanguage");
            var getDisplayName = Export(common, "uloc_getDisplayName");
            var getDisplayLanguage = Export(common, "uloc_getDisplayLanguage");
            return getName == IntPtr.Zero || getLanguage == IntPtr.Zero
                || getDisplayName == IntPtr.Zero || getDisplayLanguage == IntPtr.Zero
                ? null
                : new Icu(getName, getLanguage, getDisplayName, getDisplayLanguage);
        }

        private static IntPtr Export(IntPtr library, string name) =>
            Kernel32.GetProcAddress(library, System.Text.Encoding.ASCII.GetBytes(name + "\0"));

        /// <summary>
        /// What <paramref name="lookup"/> gives for <paramref name="localeName"/> in
        /// <paramref name="uiLocaleName"/>, in English where ICU has no data for the UI
        /// locale, and empty on any failure: <c>GlobalizationNative_GetLocaleInfoString</c>
        /// with .NET 10's buffer.
        /// </summary>
        public string Name(delegate* unmanaged[Cdecl]<byte*, byte*, char*, int, int*, int> lookup, string localeName, string uiLocaleName)
        {
            var locale = stackalloc byte[FullNameCapacity];
            var uiLocale = stackalloc byte[FullNameCapacity];
            var english = stackalloc byte[] { (byte)'e', (byte)'n', 0 };
            var value = stackalloc char[ValueCapacity];
            var status = 0;

            if (!LocaleId(localeName, locale, ref status))
                return string.Empty;
            LocaleId(uiLocaleName, uiLocale, ref status);

            var length = lookup(locale, uiLocale, value, ValueCapacity, &status);
            if (status == UsingDefaultWarning)
                length = lookup(locale, english, value, ValueCapacity, &status);

            return status > 0 ? string.Empty : new string(value, 0, Math.Min(length, ValueCapacity));
        }

        /// <summary>
        /// <paramref name="name"/> as an ICU locale ID, from <c>pal_locale.c</c>'s
        /// <c>GetLocale</c> without canonicalising: a character outside ASCII, or a '/',
        /// is an illegal argument, the rest go to uloc_getName, and a language part
        /// uloc_getLanguage cannot fit in ICU's language capacity is an illegal argument too.
        /// </summary>
        private bool LocaleId(string name, byte* result, ref int status)
        {
            if (status > 0)
                return false;

            var temp = stackalloc byte[FullNameCapacity];
            for (var i = 0; i < FullNameCapacity - 1 && i < name.Length; i++)
            {
                var c = name[i];
                if (c > 0x7F || c == '/')
                {
                    status = 1; // U_ILLEGAL_ARGUMENT_ERROR
                    return false;
                }
                temp[i] = (byte)c;
            }

            fixed (int* err = &status)
            {
                _getName(temp, result, FullNameCapacity, err);
                if (*err <= 0)
                {
                    var language = stackalloc byte[LanguageCapacity];
                    _getLanguage(temp, language, LanguageCapacity, err);
                    if (*err == BufferOverflowError || *err == StringNotTerminatedWarning)
                        *err = IllegalArgumentError;
                }
            }
            return status <= 0;
        }
    }
}
