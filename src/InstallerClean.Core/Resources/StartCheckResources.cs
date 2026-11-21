using System.Globalization;
using System.Resources;

namespace InstallerClean.Resources;

/// <summary>
/// Reads the strings <see cref="Strings.FrameworkCheck"/> serves straight from the resources
/// embedded in this assembly, with no <see cref="ResourceManager"/>. On .NET Framework 4.0
/// the ResourceManager constructor reads every attribute this assembly carries to find its
/// neutral language, and the DefaultDllImportSearchPaths attribute is a type 4.0 does not
/// have, so the constructor throws there. The start checks show their message on exactly
/// that .NET Framework, so nothing here reads an attribute.
/// </summary>
/// <remarks>
/// The search is the one <see cref="EmbeddedCultureResources"/> makes: the culture, then each
/// parent, ending at the invariant culture, whose resources are the neutral strings. A culture
/// with no resources of its own, the neutral language among them, passes on to its parent,
/// and so does a key one culture's resources lack.
/// </remarks>
internal static class StartCheckResources
{
    /// <summary>The resources Strings reads, named as its ResourceManager names them.</summary>
    internal const string BaseName = "InstallerClean.Resources.Strings";

    /// <summary>The value of <paramref name="name"/> for <paramref name="culture"/>, or null.</summary>
    internal static string? GetString(string name, CultureInfo culture)
    {
        var assembly = typeof(StartCheckResources).Assembly;
        for (var current = culture; ; current = current.Parent)
        {
            var neutral = current.Equals(CultureInfo.InvariantCulture);
            var resource = neutral ? BaseName + ".resources" : BaseName + "." + current.Name + ".resources";
            using (var stream = assembly.GetManifestResourceStream(resource))
            {
                if (stream is not null)
                {
                    using var set = new ResourceSet(stream);
                    if (set.GetString(name) is { } value) return value;
                }
            }
            if (neutral) return null;
        }
    }
}
