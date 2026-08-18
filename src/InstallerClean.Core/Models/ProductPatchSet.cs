namespace InstallerClean.Models;

/// <summary>
/// What one product's registered patch set says about whether a superseded
/// patch sharing that product may be offered.
///
/// THE QUESTION IT ANSWERS IS ABOUT THE PRODUCT AND NOT ABOUT ANY ONE PATCH,
/// which is the whole reason it exists. A superseded patch B is cached once and
/// registered once per product it applies to, so a rollback on ANY of those
/// products reaches for the same file. Reading B's own removability answers a
/// different question from the one the risk turns on, which is whether anything
/// that could supersede B on a product they share can still be uninstalled.
///
/// TWO OF THE THREE VALUES WITHHOLD AND THEY ARE NOT THE SAME FINDING. One is
/// the app saying it looked and the answer was no; the other is the app saying
/// it could not tell. That distinction is carried through to
/// <c>InstallerQueryService.Downgrade(claimed, path, withheld:)</c>, which already has both causes and
/// gains no vocabulary from this.
/// </summary>
public enum ProductPatchSet
{
    /// <summary>
    /// Every patch registered to this product positively declared
    /// <c>Uninstallable</c> as a number equal to zero. The only value that
    /// permits, and deliberately the one requiring the most to have gone right.
    /// </summary>
    AllNonRemovable,

    /// <summary>
    /// At least one registered patch positively declared itself removable, so
    /// something on this product can be uninstalled and reach for a superseded
    /// patch's cached file.
    ///
    /// IT BEATS <see cref="Unestablished"/> WHEN A PRODUCT MEETS BOTH, because
    /// it is a finding where the other is an absence of one, and the surface
    /// that names a cause should name the one that was established. Same
    /// direction as <c>InstallerQueryService.MergeClaim</c>'s second rule.
    /// </summary>
    RemovablePatchPresent,

    /// <summary>
    /// The set could not be established: the key would not open, a patch
    /// carried no <c>Uninstallable</c>, or one carried a value that was not a
    /// number. Anything that is not a positive zero lands here or above.
    ///
    /// A PRODUCT WITH NO PATCHES AT ALL DOES NOT READ AS THIS, AND IT USED TO. The
    /// reason recorded for that was twofold and both halves have gone. The first was
    /// that an absent <c>Patches</c> key could not be told from a key that would not
    /// open: they are told apart, and at the caller rather than in the read, because
    /// a key that exists and refuses throws and is caught and written as this verdict
    /// with its own failure cause, while a key that is not there returns null and
    /// never reaches the exception path. The second was that it cost nothing, since
    /// the verdict was only ever consulted for a product some candidate patch was
    /// registered to. That stopped being true when the judged product set gained the
    /// patch file's own declared targets, which name products holding no patch at all
    /// and often no patch this machine has ever seen. Reading those as unestablished
    /// would have withheld the superseded class on any ordinary machine.
    ///
    /// So a product whose <c>Patches</c> key is absent now reads
    /// <see cref="AllNonRemovable"/>: it holds no registered patch, so it holds no
    /// removable one, so nothing on it can be uninstalled and reach for a superseded
    /// patch's cached file. An absent list and an empty list say the same thing about
    /// the machine and now answer the same way. What still lands HERE is a key that
    /// exists and could not be read.
    /// </summary>
    Unestablished,
}
