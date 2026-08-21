using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// <see cref="InstallerQueryService.CarriesFlaggedSpelling"/>, which selects a
/// registered LocalPackage value carrying a spelling only the filesystem can
/// settle. It is pure string work and needs no filesystem, which is why every case
/// below runs anywhere.
///
/// EVERY ASSERTION HERE IS UNCHANGED ACROSS 3.0.0's WIDENING, AND THAT IS WORTH
/// SAYING RATHER THAN LEAVING TO BE NOTICED. This predicate used to GATE the
/// final-path resolution: a handle was opened for a value answering true and for no
/// other. Every recorded path is resolved now, so it gates nothing and counts
/// instead. What it SELECTS did not change, only what the answer is used for, so
/// the cases that pinned the old gate pin the new count without a line of them
/// moving. A widening that had needed these rewritten would have been a widening of
/// the predicate rather than of its call site, which is a different change.
///
/// The two triggers are not symmetrical. A tilde followed by a digit is the 8.3
/// alias form, which is why the folder matters: Windows Installer names the
/// cached files itself as short hex, so the FILENAME cannot have a differing
/// short form, while <c>Installer</c> is nine characters and does. A surviving
/// <c>\\?\</c> is what StripLongPathPrefix leaves on a path with no drive root,
/// which in a LocalPackage value means a volume-GUID path.
///
/// Over-selection is deliberate and is asserted here rather than left to be read as
/// an accident. What it costs has changed with the job: a false positive used to
/// cost one handle on a path that resolved to itself and now inflates a figure
/// nothing acts on, and a false negative used to cost a file and now costs nothing,
/// the resolution no longer depending on this answer.
/// </summary>
public class InstallerQueryServiceShortNameTriggerTests
{
    [Theory]
    // The folder form, which is the one the cache path can actually take.
    [InlineData(@"C:\Windows\INSTAL~1\9f05cba.msi")]
    // Matched by character, not by case, because nothing obliges a registered
    // value to carry the alias in the case the filesystem reports it.
    [InlineData(@"C:\Windows\instal~1\9f05cba.msi")]
    // A digit other than one, which an alias takes when the first is claimed.
    [InlineData(@"C:\Windows\INSTAL~2\9f05cba.msi")]
    // Elsewhere in the path, because the trigger is not anchored to a segment.
    [InlineData(@"C:\PROGRA~1\SomeVendor\cached\9f05cba.msi")]
    // The leaf, which Windows Installer's own naming rules out and a registered
    // value does not: the test asks what the string says, not who wrote it.
    [InlineData(@"C:\Windows\Installer\ABCDEF~1.MSI")]
    // The trigger as the final two characters, the loop's boundary case.
    [InlineData(@"C:\Windows\INSTAL~1")]
    public void A_tilde_before_a_digit_is_a_flagged_spelling(string path) =>
        Assert.True(InstallerQueryService.CarriesFlaggedSpelling(path));

    [Theory]
    // What StripLongPathPrefix leaves whole, being the form with no drive root.
    [InlineData(@"\\?\Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\\?\GLOBALROOT\Device\HarddiskVolume3\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\\?\HarddiskVolume3\Windows\Installer\9f05cba.msi")]
    // Both spellings, because a registered value can carry either and they name
    // the same object. The NT form is the one that cannot be left to
    // Path.GetFullPath, which reads its leading separator as rooted on the
    // running process's drive; missing it here is how it stayed unnoticed.
    [InlineData(@"\??\Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\??\GLOBALROOT\Device\HarddiskVolume3\Windows\Installer\9f05cba.msi")]
    public void A_surviving_prefix_of_either_form_is_a_flagged_spelling(string path) =>
        Assert.True(InstallerQueryService.CarriesFlaggedSpelling(path));

    [Theory]
    // The shape of every path on a machine that has neither spelling, which is
    // every path on every machine measured so far.
    [InlineData(@"C:\Windows\Installer\9f05cba.msi")]
    [InlineData(@"C:\Windows\Installer\1e4a2f.msp")]
    [InlineData(@"\\server\share\Installer\9f05cba.msi")]
    // A tilde with no digit behind it is an ordinary character in a name.
    [InlineData(@"C:\Windows\Inst~aller\9f05cba.msi")]
    [InlineData(@"C:\Windows\Installer\backup~.msi")]
    // A trailing tilde has nothing after it to be an alias index.
    [InlineData(@"C:\Windows\Installer\9f05cba~")]
    // A digit with no tilde before it, which is most of every real path.
    [InlineData(@"C:\Windows\Installer\1.msi")]
    [InlineData("")]
    public void An_ordinary_path_carries_no_flagged_spelling(string path) =>
        Assert.False(InstallerQueryService.CarriesFlaggedSpelling(path));

    /// <summary>
    /// The prefix test is Ordinal and the alias test is not, and both are
    /// deliberate. A prefix is a fixed four-character token the kernel and the
    /// registry both spell one way; an alias is a filesystem name whose case a
    /// registered value need not preserve. Pinned because a later reader
    /// tidying one to match the other would break exactly one of them.
    /// </summary>
    [Theory]
    [InlineData(@"//?/Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\\?/Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\9f05cba.msi")]
    public void A_forward_slash_is_not_the_prefix(string path) =>
        Assert.False(InstallerQueryService.CarriesFlaggedSpelling(path));

    /// <summary>
    /// A prefix the strip already dealt with must not be selected: taking
    /// <c>\??\</c> off a drive-rooted path is exactly what StripLongPathPrefix
    /// does, so what arrives here is an ordinary path and nothing about it is
    /// unsettled. Pinned because the two prefix tests read alike and only the
    /// surviving one is meant to fire.
    /// </summary>
    [Theory]
    [InlineData(@"C:\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\\server\share\Installer\9f05cba.msi")]
    public void A_path_the_strip_already_settled_carries_nothing(string path) =>
        Assert.False(InstallerQueryService.CarriesFlaggedSpelling(path));
}
