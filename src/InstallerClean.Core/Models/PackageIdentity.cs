namespace InstallerClean.Models;

/// <summary>
/// What a cached package says it is, read out of the file itself rather than
/// taken from how a registration spelled its location.
///
/// The distinction is the whole point of the type. Every other question the scan
/// asks about a cached file is a question about a PATH, and a path is written by
/// whatever installed the product, in whatever form that installer chose. An
/// identity is written by the package author into the package, is required to be
/// there, and does not change with the spelling of anything.
///
/// A value of this type means the file yielded something the app can put to
/// Windows as a question. A file that yielded nothing askable produces no value
/// at all; see <see cref="Services.IPackageIdentityReader"/>, which will not
/// hand back a half-read identity for a caller to notice or fail to notice.
/// </summary>
/// <param name="Code">
/// The package's own code: a ProductCode for an installation package, a
/// PatchCode for a patch. Canonicalised to the braced upper-case GUID form, so
/// two readings of the same code are the same string and can key a cache.
/// </param>
/// <param name="IsPatch">
/// Which of the two <paramref name="Code"/> is, because the question Windows is
/// asked differs entirely: a product code can be asked about on its own, and a
/// patch code cannot be asked about at all except through a product that might
/// hold it.
/// </param>
/// <param name="TargetProductCodes">
/// For a patch, the product codes it declares it may be applied to, from its
/// Template. Empty for an installation package, and never empty for a patch: a
/// patch naming no target is a patch there is no way to ask about, so the reader
/// treats it as unread rather than returning an identity that cannot be used.
/// </param>
public readonly record struct PackageIdentity(
    string Code,
    bool IsPatch,
    IReadOnlyList<string> TargetProductCodes);
