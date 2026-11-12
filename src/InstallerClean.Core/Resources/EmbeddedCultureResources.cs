using System.Globalization;
using System.Reflection;
using System.Resources;

namespace InstallerClean.Resources;

/// <summary>
/// A <see cref="ResourceManager"/> that reads every language from the assembly it is given.
/// The neutral strings are the resource named <c>&lt;baseName&gt;.resources</c>, and each
/// other language is the resource named <c>&lt;baseName&gt;.&lt;culture&gt;.resources</c>
/// beside it in the same assembly. This manager looks for no satellite assembly, so it reads
/// no folder named for a culture beside the exe.
/// </summary>
/// <remarks>
/// The fallback is the one a satellite layout gives. A culture with no resource of its own
/// takes its parent's, and so on up to the neutral strings; a key a language's resource does
/// not hold is answered by the next resource up the same chain, which
/// <see cref="ResourceManager.GetString(string, CultureInfo)"/> walks by asking
/// <see cref="InternalGetResourceSet"/> for each culture in turn. The culture the assembly's
/// <see cref="NeutralResourcesLanguageAttribute"/> names reads the neutral strings, as the
/// invariant culture does. A culture's name is matched to its resource without regard to
/// case, as a folder name on Windows is.
/// </remarks>
internal sealed class EmbeddedCultureResources : ResourceManager
{
    private const string Extension = ".resources";

    private readonly Assembly _assembly;
    private readonly string _neutralName;
    private readonly string _neutralCulture;
    private readonly Dictionary<string, string> _nameByCulture = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, ResourceSet> _sets = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    internal EmbeddedCultureResources(string baseName, Assembly assembly)
        : base(baseName, assembly)
    {
        _assembly = assembly;
        _neutralName = baseName + Extension;
        _neutralCulture = GetNeutralResourcesLanguage(assembly).Name;
        var prefix = baseName + ".";
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (name.Length <= prefix.Length + Extension.Length) continue;
            if (!name.StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (!name.EndsWith(Extension, StringComparison.Ordinal)) continue;
            var culture = name.Substring(prefix.Length, name.Length - prefix.Length - Extension.Length);
            _nameByCulture[culture] = name;
        }
    }

    /// <summary>
    /// The names of the cultures the assembly holds a resource for, the neutral strings not
    /// among them.
    /// </summary>
    internal IReadOnlyCollection<string> Cultures => _nameByCulture.Keys;

    /// <summary>
    /// The resource for <paramref name="culture"/>, or with <paramref name="tryParents"/> the
    /// first one found going up through its parents to the neutral strings. Null where
    /// <paramref name="tryParents"/> is false and the culture has no resource of its own.
    /// </summary>
    protected override ResourceSet? InternalGetResourceSet(
        CultureInfo culture, bool createIfNotExists, bool tryParents)
    {
        for (var current = culture; ; current = current.Parent)
        {
            var neutral = current.Equals(CultureInfo.InvariantCulture)
                || string.Equals(current.Name, _neutralCulture, StringComparison.OrdinalIgnoreCase);
            if (neutral || _nameByCulture.ContainsKey(current.Name))
            {
                return SetFor(neutral ? string.Empty : current.Name, createIfNotExists);
            }
            if (!tryParents) return null;
        }
    }

    /// <summary>
    /// Closes every resource read so far; a later read opens it again. The sets are taken out
    /// of use before any of them is closed, as the base class does with its own.
    /// </summary>
    public override void ReleaseAllResources()
    {
        Dictionary<string, ResourceSet> released;
        lock (_gate)
        {
            released = _sets;
            _sets = new Dictionary<string, ResourceSet>(StringComparer.OrdinalIgnoreCase);
        }
        base.ReleaseAllResources();
        foreach (var set in released.Values) set.Close();
    }

    private ResourceSet? SetFor(string culture, bool createIfNotExists)
    {
        lock (_gate)
        {
            if (_sets.TryGetValue(culture, out var held)) return held;
            if (!createIfNotExists) return null;

            var name = culture.Length == 0 ? _neutralName : _nameByCulture[culture];
            var stream = _assembly.GetManifestResourceStream(name)
                ?? throw new MissingManifestResourceException(
                    $"The assembly {_assembly.GetName().Name} holds no resource named {name}.");
            var set = new ResourceSet(stream);
            _sets[culture] = set;
            return set;
        }
    }
}
