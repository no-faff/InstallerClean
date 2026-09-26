namespace InstallerClean.Models;

/// <summary>
/// The output of one <c>InstallerQueryService</c> enumeration: every
/// <c>LocalPackage</c> path the Windows Installer API and the registry fallback
/// between them claim, plus how much of the enumeration failed to read.
/// </summary>
/// <param name="Packages">
/// One row per claimed path. A row carries
/// <see cref="RegisteredPackage.IsRemovable"/> only where Windows reported the patch
/// SUPERSEDED, the patch positively declared itself non-removable, and every product
/// it is registered under was established to hold no patch that could be uninstalled
/// and roll back onto its file. An obsoleted patch never carries it, being off the
/// offer for policy rather than for safety, and neither does a row whose State or
/// Uninstallable read failed.
/// </param>
/// <param name="UnaccountedProductCount">
/// Installed products this enumeration did not account for. The trigger for
/// withholding the removable class, and the figure the command line's
/// Application-log notice about that withholding carries.
///
/// THREE contributors, and only the first is a failure to read: a product whose
/// row came back but whose LocalPackage value, or one of whose patch rows, would
/// not read. A product row that would not read refuses the scan instead, so no
/// result carries one. Then an absence rather than a failure: a cached file the
/// registry claims and the API never mentioned, computed NET of the read failures
/// (see the subtraction at the assembly site), so by construction it counts only
/// products where nothing failed to read at all. A product meeting more than one
/// contributor counts once.
///
/// The third is neither a failure to read nor an absence: a product the registry
/// named and this scan could not settle either way, Windows declining to say
/// whether the code is installed or the key name yielding no code to ask with. Its
/// opposite number, a registry product confirmed installed and recovered into the
/// questions the scan asks, contributes NOTHING here, and that asymmetry is the
/// point: a product that can be asked about is asked, and only a product nobody
/// can get an answer about withholds.
///
/// ITS NAME SAYS ONLY THAT THE ENUMERATION COULD NOT ACCOUNT FOR THEM, because two
/// of its three contributors are not read failures at all, one being an absence and
/// one an inability to establish anything either way. Any sentence built on this
/// number is a sentence about all three. The terms themselves are in
/// <see cref="Census"/>, separately, for anything that needs to say which.
///
/// It is not an exact headcount either, and cannot be made one. It can run under the
/// truth, the unclaimed-file term being an estimate the assembly site deliberately
/// biases low. It can run over it as well: a product subkey whose name is not a
/// packed GUID counts once where nothing could be asked about it, and the file that
/// key records counts again where the enumeration never claimed it and it is on the
/// disk, two terms that are added rather than netted against each other. A difference
/// between two product totals, which a stale registry key inflates, is not a term:
/// the products behind such a difference are asked about by name instead.
///
/// READING HIGH IS THE SAFE DIRECTION, a higher count withholding the removable class
/// on more machines and never on fewer. So no surface may present this as a count of
/// programs, nor say the true figure is at least this one.
/// </param>
/// <param name="PatchClaims">
/// Every product-to-patch claim this enumeration read, one entry per claim
/// rather than per path. <see cref="Packages"/> answers what a path's verdict is;
/// this answers who to ask about it, which the merge behind
/// <see cref="Packages"/> cannot keep (see <see cref="PatchClaim"/>). Empty on a
/// machine with no registered patches, and empty on a result built by anything
/// that does not enumerate patches.
/// </param>
/// <param name="Census">
/// What the enumeration measured about itself and about the machine, for the
/// opt-in report and for nothing else. Default on a result built by anything that
/// does not enumerate, which reads as a census nobody took rather than as a
/// machine where every term was zero; only a real enumeration's result should be
/// read for these.
/// </param>
/// <param name="Installations">
/// Every installation of a product this enumeration established: each row the product
/// walk listed, whether or not its records read, and each installation the recovery by
/// name found for a product the walk did not return. The declared-product screen is
/// handed it, and an answer about a product that leaves out an installation listed here
/// contradicts this enumeration (see <see cref="Services.IDeclaredProductCheck"/>). Empty
/// on a result built by anything that does not enumerate.
/// </param>
public record InstallerQueryResult(
    IReadOnlyList<RegisteredPackage> Packages,
    int UnaccountedProductCount = 0,
    IReadOnlyList<PatchClaim>? PatchClaims = null,
    EnumerationCensus Census = default,
    IReadOnlyList<ListedInstallation>? Installations = null)
{
    /// <summary>Never null: an absent list reads as no claims rather than as a fault.</summary>
    public IReadOnlyList<PatchClaim> PatchClaims { get; init; } = PatchClaims ?? Array.Empty<PatchClaim>();

    /// <summary>Never null: an absent list reads as an enumeration that listed nothing.</summary>
    public IReadOnlyList<ListedInstallation> Installations { get; init; } =
        Installations ?? Array.Empty<ListedInstallation>();

    /// <summary>
    /// The enumeration did not account for at least one installed product, so the
    /// set of registrations it read may be short of one. Whether that product's
    /// records failed to read or were never reached does not enter into it: what
    /// matters is the missing claim, not the mechanism (see
    /// <see cref="UnaccountedProductCount"/> for the three).
    ///
    /// IT BEARS ON THE OFFER AND ON THE MISSING-FILES REPORT. It withholds every
    /// superseded-patch verdict, which is the whole of the superseded offer on a run
    /// where it fires; and a registration this scan never saw is also one whose file,
    /// had it gone, went uncounted.
    /// </summary>
    public bool RecordsIncomplete => UnaccountedProductCount > 0;
}

/// <summary>
/// The terms behind <see cref="InstallerQueryResult.UnaccountedProductCount"/>,
/// plus the shape facts one enumeration can see, carried separately so a report
/// never has to state one cause for a quantity built from several.
///
/// THE FIELDS THAT DECIDE ANYTHING ARE READ THROUGH THE TWO PROPERTIES AT THE END,
/// <see cref="AnyRecordedPathUnestablished"/> and
/// <see cref="SecondInstanceNotRuledOut"/>, and the rest decide nothing. A count
/// that gained a consumer anywhere else would be a second, quieter copy of a rule
/// that already exists in one place.
///
/// They travel in the opt-in report because how each one runs across machines is a
/// fact only reports from many machines can establish. Each is a count or a fixed
/// label, never a path, a name or an identifier.
/// </summary>
/// <param name="UnreadableProducts">
/// Products whose records came back short: an unreadable <c>LocalPackage</c> value,
/// an unreadable <c>LocalPackage</c> under one of its patches, or a patch
/// enumeration that did not run to a clean end. One per product however many it
/// met. An exact per-product tally with no floor under it, which is what separates
/// it from the estimate the withholding is otherwise computed from.
/// </param>
/// <param name="SkippedProductRows">
/// Rows the product walk passed without reading, one per row. It is zero on every
/// scan that produces a result: the walk refuses the scan on a row it cannot read.
/// <see cref="UnreadableProducts"/> is seeded from it, so a row the walk ever passed
/// would count there as well.
/// </param>
/// <param name="RegistryProductKeys">
/// Product subkeys the registry fallback walked under <c>UserData</c>. The only
/// independent count of how many products a machine has. Nothing is derived from
/// it: the products it disagrees with the enumeration about are asked after by
/// name instead.
/// </param>
/// <param name="UnclaimedProductFiles">
/// Product registry entries whose cached path the API's own loop never claimed
/// AND whose file is really on the disk. Both halves are load-bearing and both
/// are observed rather than inferred.
/// </param>
/// <param name="UnclaimedPatchFiles">
/// The same for patch entries. A patch entry names no product, so it can
/// establish only that at least one product went unreached.
///
/// THESE ARE TALLIES AND NOT THE ARITHMETIC, WHICH IS DELIBERATELY NOT CARRIED.
/// Its one derived term is a product estimate floored at one by patch evidence and
/// biased low by a generous subtraction, so it is not the count its name would
/// claim, and it is reproducible from these plus
/// <see cref="UnreadableProducts"/>.
///
/// <see cref="UnansweredProductCount"/> and <see cref="UnparseableProductKeyNames"/>
/// are the arithmetic's other inputs and are not among these, being neither a
/// headcount nor an observation of the disk: they tally the products that could not
/// be settled, and they are added to the derived term rather than weighed against
/// it.
///
/// <see cref="RegistryProductKeys"/> is NOT an input to any of it and still
/// travels. Nothing is derived from its difference against
/// <see cref="ProductCount"/>, which cannot tell a truncated enumeration from
/// ordinary registry residue; the products behind it are asked about by name
/// instead. How large it runs across real machines is a
/// fact about machines rather than about this app, and only these reports can
/// answer it.
/// </param>
/// <param name="NonStringLocalPackageValues">
/// Cached-package values, <c>LocalPackage</c> or <c>ManagedLocalPackage</c>, that
/// were present and were not a string, one per value. Answers whether anything in the
/// wild writes those values under a type other than <c>REG_SZ</c>, which decides
/// whether a string cast is a safe way to read them.
/// A subset of the fallback's failure count rather than a term beside it.
/// </param>
/// <param name="UnreadablePatchStates">
/// Patch claims whose <c>State</c> or <c>Uninstallable</c> read failed, one per
/// (patch, product) pairing asked. No file turns on it, a registration whose read
/// failed being kept; the number says how often a machine cannot answer a plain
/// question about its own installer records at all.
/// </param>
/// <param name="UnreadableVerdictPaths">
/// Cached paths whose patch state no read established, one per merged row where
/// the count above is one per pairing. The pair is the interesting reading:
/// a machine where several products' reads failed on one shared patch reports a
/// high pairing count against a single path, and a machine where the failures are
/// spread reports the two close together, which are different faults wearing one
/// number.
///
/// Existence is not tested, unlike the two unclaimed-file counts above, so this
/// counts registrations rather than files on the disk. Nothing downstream may
/// read it as a count of space or of files a user could see.
/// </param>
/// <param name="ProductCount">
/// Product rows the API enumeration returned. With
/// <see cref="PatchClaimCount"/> it gives the patch-to-product ratio, which is
/// the shape of a machine's cache.
/// </param>
/// <param name="PatchClaimCount">
/// Product-to-patch claims read, one per claim rather than per patch: a patch
/// applied to three products counts three times, which is the figure that
/// describes the enumeration's work.
/// </param>
/// <param name="LongLeafStemCount">
/// Claimed paths whose leaf name has more than eight characters before the
/// extension, so the name cannot itself be an 8dot3 short name. Counted across
/// every claimed path, removable rows included, because the question is about
/// what the records hold and not about what this run is offering.
///
/// It is read against <see cref="ScanResult.RegisteredPackages"/>'s own count in the
/// same report: the two equal says no name the records hold for a cached file can be
/// an 8dot3 short name.
/// </param>
/// <param name="RecoveredProductCount">
/// Products the registry named, the enumeration never returned, and a keyed ask
/// then found installed: a count of products identified individually and confirmed
/// one at a time, not inferred from the difference between two totals.
///
/// Zero is the answer on a machine whose enumeration was whole, and it is also
/// the answer on a machine whose registry holds nothing but residue, so a
/// non-zero reading is the interesting one. It withholds nothing, the products
/// behind it having been asked rather than guessed at, which is why it is only
/// here and nowhere in the arithmetic.
/// </param>
/// <param name="UnansweredProductCount">
/// Product codes the registry named, the enumeration never returned, and Windows
/// would then not say were installed or not. A question that was put and got no
/// answer. Unlike the count above it does withhold, because nothing about the
/// enumeration's completeness follows from an unanswered question.
///
/// IT WITHHOLDS TWICE. The superseded class is withheld, on the same count as every
/// other product this scan could not account for. And the walk-derived offer is
/// withheld, through <see cref="SecondInstanceNotRuledOut"/>: a product Windows would
/// not answer about was never asked its <c>InstanceType</c>, so nothing shows it is
/// not a second instance of itself.
/// </param>
/// <param name="UnparseableProductKeyNames">
/// Registry product key names that yielded no product code, so there was nothing
/// to ask about. The registry says the machine has a product and nothing here can
/// turn its name into a question.
///
/// IT IS NOT THE COUNT ABOVE AND MUST NEVER BE MERGED INTO IT UNDER ONE NAME. A
/// single figure over both can only be described by a sentence false of half its
/// members: "Windows would not answer" is false of every member of this term,
/// because Windows was never asked. The withholding arithmetic may add them,
/// needing only the total of what could not be settled, and that superordinate is
/// true of both; every sentence narrower than it has to keep them apart, the
/// opt-in report included.
///
/// Counted while walking every product key, not only the ones the enumeration
/// missed, so it is a property of the registry's contents rather than of a run.
///
/// It withholds on the same two terms as the count above, the walk-derived offer
/// through <see cref="SecondInstanceNotRuledOut"/> included: a key whose name yields
/// no code cannot be matched to any product that was asked, so nothing shows the
/// product it belongs to was asked its <c>InstanceType</c>.
///
/// Between them these three report, per machine, what proportion of its registry
/// keys really were residue. The difference between the two product totals cannot
/// answer that, since it cannot tell residue from a truncated enumeration.
/// </param>
/// <param name="InstanceProductCount">
/// Products that answered a non-zero <c>InstanceType</c>: installed as a second
/// instance of themselves under an instance transform. PRODUCTS, not files, and
/// not a count of anything held back.
///
/// IT DECIDES. A keyed question about the product code written inside a cached
/// package can answer "no record" while a registration under a transform-generated
/// code still needs the file, and <see cref="Services.DeclaredProductCheck"/> reads
/// that code out of the file and asks exactly that question.
///
/// What acts on it is <see cref="SecondInstanceNotRuledOut"/>, which reads this
/// count together with the one below, and with the two counts of products the
/// registry names that nothing shows were asked, and never on its own. This member
/// is carried and sent apart, because how often a machine ANSWERS the question and
/// how often it REFUSES to are different facts.
///
/// A POSITIVE READING IS THE ONLY THING COUNTED. A value that will not parse is
/// not a positive, and neither is an absent property, which Microsoft documents as
/// meaning an ordinary installation. The value is compared as a NUMBER rather than
/// against the string "1", because nothing documents the spelling the API returns
/// and a machine answering "01" or "1 " would read as ordinary on a string test.
/// </param>
/// <param name="InstanceTypeUnreadableCount">
/// Products whose <c>InstanceType</c> read failed, so they were neither counted
/// above nor shown to be ordinary. THIS IS WHAT STOPS A ZERO ABOVE BEING READ AS
/// "NO SUCH PRODUCT ON THIS MACHINE".
///
/// IT IS THE HALF THAT MAKES THE COUNT HONEST AND IT IS NOT A TRI-STATE. A single
/// three-valued verdict could say complete, incomplete or unreadable and could not
/// say how many products it failed on, which is the number a receiver comparing
/// machines needs. A count beside a count is the shape every other member here
/// uses.
///
/// WHICH PRODUCTS WERE ASKED AT ALL. Both counts
/// cover the products the enumeration returned AND the products it lost that the
/// registry named and the recovery pass resolved as installed
/// (<see cref="RecoveredProductCount"/>), which are asked one keyed read each. A
/// product the walk never reached goes unasked only in the two states the recovery
/// cannot settle: a code Windows
/// would not answer about (<see cref="UnansweredProductCount"/>) and a registry key
/// whose name yielded no code (<see cref="UnparseableProductKeyNames"/>). Either of
/// those non-zero, or <see cref="UnreadableProducts"/> on a run where no fallback
/// named the lost product, makes both counts a floor.
/// </param>
/// <param name="ProductPatchKeyCount">
/// Products whose registry <c>Patches</c> key opened, from the per-product patch
/// listing the superseded-patch condition rests on.
///
/// THESE FOUR ARE READ AND THEY ARE SENT.
/// <see cref="ResultLogEntry"/> takes all four off this census, declares them as
/// parameters of its scan record, and that record is the file the Send-result button
/// POSTs.
///
/// DO NOT DROP, RENAME OR QUIETLY REDEFINE THEM. An unread counter is ordinarily fair
/// game for all three, and these four are the only instrument this project has for
/// sizing what the per-product condition withholds on any machine but the one it was
/// written on. Something is receiving them, so a change of meaning here is a schema
/// decision and not a tidy-up.
///
/// Against <see cref="ProductCount"/> it says how usual it is for a product to carry a
/// Patches key at all, a product holding no registered patch having no reason to.
/// </param>
/// <param name="ProductPatchRegistrationCount">
/// Patch subkeys under those keys, one per (product, patch) registration rather
/// than per patch. With the count above it gives how many patches a machine's
/// products carry.
/// </param>
/// <param name="ProductsWithRemovablePatchCount">
/// Products where at least one registered patch positively declared itself
/// removable, so a rollback on that product could reach for a superseded patch's
/// cached file.
///
/// IT IS THE FIGURE THAT SAYS ON HOW MANY PRODUCTS THE CONDITION IS ARMED, and only
/// reports from many machines can say how it runs. It is not a count of anything held
/// back: that is decided per cached path, over the products holding it.
/// </param>
/// <param name="ProductsWithPatchSetUnestablishedCount">
/// Products whose patch set could not be established: the key would not open, or a
/// patch carried no <c>Uninstallable</c> or one that was not a number.
///
/// THE OTHER HALF OF THE SAME QUESTION, and kept apart from it because they are
/// different findings. One is the condition finding a reason to withhold; this is
/// the condition unable to look. A machine reading high here is one where the
/// condition holds patches back without having established anything about them.
/// </param>
/// <param name="PathResolverAttemptCount">
/// Recorded paths this scan put to the final-path resolver: EVERY value that got past
/// the embedded-null test and the expansion. How many of them carried a spelling only
/// the filesystem can settle is <see cref="PathFlaggedSpellingCount"/>.
///
/// THE FIVE OUTCOME COUNTS BELOW CANNOT BE READ WITHOUT IT. A scan that asked the
/// resolver nothing reports all five as zero, identical on the wire to a scan that
/// asked and got clean answers. This is what tells those two apart, and a receiver
/// reading any of the five without it is reading a number that cannot mean what it
/// appears to. The five decide something as well, through
/// <see cref="AnyRecordedPathUnestablished"/>.
/// </param>
/// <param name="PathResolverNotAPathCount">
/// Of those, the ones the resolver refused outright as not a path.
/// </param>
/// <param name="PathResolverNoAncestorCount">
/// Of those, the ones with no existing component anywhere up to the root: an
/// unattached drive, an unmapped share, a detached virtual disk.
/// </param>
/// <param name="PathResolverOpenRefusedCount">
/// Of those, the ones an ancestor existed for and no handle could be opened on.
/// Most often an ACL.
/// </param>
/// <param name="PathResolverNoFinalNameCount">
/// Of those, the ones whose final name came back empty from an opened handle.
/// </param>
/// <param name="PathResolverFaultedCount">
/// Of those, the ones where the attempt threw rather than answering.
///
/// THE RESOLVED COUNT IS NOT CARRIED. It is the attempts less these five, and a
/// stored copy could disagree with its own parts.
///
/// All five are acted on alike, on the one thing true of every member: the resolver
/// was asked and did not answer.
/// </param>
/// <param name="PathNormalisationRefusedAtExpansionCount">
/// Recorded values refused while expanding an environment variable.
///
/// THESE FOUR ARE ONE POPULATION SPLIT BY CAUSE, AND THE SPLIT IS THE POINT. A
/// value carrying a character no path can carry, and one refused by the expansion,
/// by the prefix work and by <c>GetFullPath</c>, are four different facts about a
/// machine, so a single count named for any one of them would be false of the other
/// three. The only thing true of all four, and the only thing any sentence may say
/// over their sum, is that the recorded path could not be turned into a path at all.
///
/// WHAT THE SUM MEANS, since it is the one that matters: such a claim is kept in
/// the raw spelling Windows gave, so it matches nothing the folder walk produces and
/// the cached file it names sits in the folder unclaimed. It cannot be caused by a
/// missing file, a missing drive or a permission, which is what keeps it a separate
/// population from the resolver's five, where two of the causes can.
/// </param>
/// <param name="PathNormalisationRefusedAtPrefixStripCount">
/// The same, refused while taking a prefix off or preparing the resolver's ask.
/// </param>
/// <param name="PathNormalisationRefusedAtFullPathCount">
/// The same, refused by <c>GetFullPath</c>: a device name, a length past the API's
/// limit.
/// </param>
/// <param name="PathNormalisationRefusedAtEmbeddedNullCount">
/// The same, refused for carrying an embedded null, which no path can carry.
///
/// LAST IN THE LIST AND FIRST IN THE METHOD, and the order here is the safe one
/// rather than the tidy one. These are positional parameters and every one of them
/// is an <c>int</c>, so inserting a member among the others would re-point each
/// argument after it at its neighbour's value with nothing in the build to say so.
/// Appending cannot do that.
///
/// IT IS THE MEMBER THAT FIRES ON WINDOWS, which is why it is not folded into the
/// expansion it precedes. On Windows the expansion cuts such a value at the null and
/// returns without throwing, so this count, taken before it, is where the condition
/// shows.
/// </param>
/// <param name="PathFlaggedSpellingCount">
/// Recorded values carrying a spelling only the filesystem can settle: an 8dot3
/// alias, or a prefix the strip left on for want of a drive root.
///
/// THE ONLY MEMBER OF THIS GROUP THAT IS NOT AN OUTCOME. Every other count here says
/// what happened to a value; this says what the value looked like, and it decides
/// nothing. A machine reporting a figure above zero is one holding the spellings the
/// resolution exists for.
///
/// IT IS NOT A COUNT OF ANYTHING GOING WRONG and must never be reported as one. A
/// flagged spelling that resolves is a claim correctly settled: the mechanism
/// working, not a fault. The five resolver outcomes are where a failure would appear.
/// </param>
public readonly record struct EnumerationCensus(
    int UnreadableProducts = 0,
    int SkippedProductRows = 0,
    int RegistryProductKeys = 0,
    int UnclaimedProductFiles = 0,
    int UnclaimedPatchFiles = 0,
    int NonStringLocalPackageValues = 0,
    int UnreadablePatchStates = 0,
    int ProductCount = 0,
    int PatchClaimCount = 0,
    int LongLeafStemCount = 0,
    int RecoveredProductCount = 0,
    int UnansweredProductCount = 0,
    int UnparseableProductKeyNames = 0,
    int UnreadableVerdictPaths = 0,
    int InstanceProductCount = 0,
    int InstanceTypeUnreadableCount = 0,
    int ProductPatchKeyCount = 0,
    int ProductPatchRegistrationCount = 0,
    int ProductsWithRemovablePatchCount = 0,
    int ProductsWithPatchSetUnestablishedCount = 0,
    int PathResolverAttemptCount = 0,
    int PathResolverNotAPathCount = 0,
    int PathResolverNoAncestorCount = 0,
    int PathResolverOpenRefusedCount = 0,
    int PathResolverNoFinalNameCount = 0,
    int PathResolverFaultedCount = 0,
    int PathNormalisationRefusedAtExpansionCount = 0,
    int PathNormalisationRefusedAtPrefixStripCount = 0,
    int PathNormalisationRefusedAtFullPathCount = 0,
    int PathNormalisationRefusedAtEmbeddedNullCount = 0,
    int PathFlaggedSpellingCount = 0,
    // The registry side's own tally of key reads that threw, appended rather
    // than filed beside the products it is about: every member here is a
    // positional int, so one inserted among them re-points every argument after
    // it with nothing in the build to say so, and appending is the arrangement
    // that cannot do that.
    //
    // It counts a different thing from UnreadableProducts, which is what the
    // enumeration said about ITSELF: products it returned whose records came
    // back short inside the loop. This one is the fallback failing to read a key
    // at all. The refusal that weighs both needs each to be above zero, so a
    // machine whose enumeration answers cleanly while the registry side fails
    // reads leaves that refusal unarmed, and the count is how the opt-in report
    // records that the reads failed, as registryKeyReadFailureCount. The crash log
    // writes the first twenty in full, then each cause not yet seen, and counts
    // the rest in a closing entry.
    int RegistryKeyReadFailures = 0)
{
    /// <summary>
    /// Every recorded value this scan could not turn into a path, whatever refused
    /// it: the sum of the four counts above, and the population the withholding
    /// acts on.
    ///
    /// IT IS A PROPERTY HERE SO THAT THE SUM EXISTS ONCE. The rule that reads it lives
    /// in another service, and a rule adding the parts itself would not act on a member
    /// added to the split: the build stays green, the counter still reports, and the
    /// withholding does not fire for the new cause.
    ///
    /// A MIXED SET, SO NOTHING MAY STATE A CAUSE FOR IT. The four are four different
    /// facts about a machine and the only thing true of every member is that the
    /// recorded path could not be turned into a path at all.
    /// </summary>
    public int PathNormalisationRefusedTotal =>
        PathNormalisationRefusedAtExpansionCount
        + PathNormalisationRefusedAtPrefixStripCount
        + PathNormalisationRefusedAtFullPathCount
        + PathNormalisationRefusedAtEmbeddedNullCount;

    /// <summary>
    /// Every recorded value the final-path resolver was asked about and did not
    /// resolve, whichever way it failed: the sum of the five outcome counts above.
    ///
    /// A SECOND POPULATION BESIDE THE ONE ABOVE, NOT A PART OF IT. The four above
    /// are values that could not be turned into a path at all; these are values that
    /// ARE paths and whose spelling the filesystem would not settle. A value can
    /// appear in both, the resolver refusing it and the closing
    /// <c>GetFullPath</c> then refusing it too, so the two totals must never be added
    /// and called a count of anything. <see cref="AnyRecordedPathUnestablished"/> is
    /// the only thing that reads them together, and it asks a question a double count
    /// cannot distort.
    ///
    /// A MIXED SET, SO NOTHING MAY STATE A CAUSE FOR IT, on the same rule as the
    /// total above. The only thing true of all five is that the resolver was asked
    /// and did not answer.
    /// </summary>
    public int PathResolverRefusedTotal =>
        PathResolverNotAPathCount
        + PathResolverNoAncestorCount
        + PathResolverOpenRefusedCount
        + PathResolverNoFinalNameCount
        + PathResolverFaultedCount;

    /// <summary>
    /// Whether this scan met any recorded path it could not settle, over every
    /// population above. THE ONE THING THE WITHHOLDING ASKS, and the reason it is
    /// here rather than in the service that acts on it.
    ///
    /// A rule that named the populations itself would be one edit away from silently
    /// not acting on a population added later: the build stays green, the new counter
    /// still reports, and the withholding simply does not fire for the new cause.
    /// That is why the question is asked where the members are declared. Anything added
    /// to this record that means "a recorded path this scan could not settle" belongs
    /// in this expression in the same edit.
    ///
    /// A BOOL RATHER THAN A SUM, deliberately. The populations can double-count one
    /// value between them, so their sum is a count of refusals and not of paths, and a
    /// figure that reads as a file count and is not one would be quoted as one. The
    /// counts are carried apart for the report, which reads them apart; the rule needs
    /// only whether anything failed.
    /// </summary>
    public bool AnyRecordedPathUnestablished =>
        PathNormalisationRefusedTotal > 0 || PathResolverRefusedTotal > 0;

    /// <summary>
    /// Whether this scan failed to establish that every product it knows of is an
    /// ordinary single-instance installation. THE ONE THING THE SECOND-INSTANCE
    /// WITHHOLDING ASKS, and it is here rather than in the service that acts on it for
    /// the reason <see cref="AnyRecordedPathUnestablished"/> is: a rule that named the
    /// members itself would be one edit away from silently not acting on a member added
    /// later, with a green build and a counter still reporting.
    ///
    /// FOUR MEMBERS, AND THE SUPERORDINATE IS EXACT. <see cref="InstanceProductCount"/>
    /// is a positive answer that a product IS a second instance of itself.
    /// <see cref="InstanceTypeUnreadableCount"/> is a question that was put and not
    /// answered. <see cref="UnansweredProductCount"/> is a product the registry names
    /// that Windows would not say was installed, so it was never put the question.
    /// <see cref="UnparseableProductKeyNames"/> is a registry product key whose name
    /// yields no code, so it cannot be matched to any product that was asked. The only
    /// thing true of all four is the one this property is named for: the scan cannot say
    /// that no installed product is a second instance of itself. Nothing may state a
    /// cause over them, here or on any surface.
    ///
    /// WHY ANY OF THEM WITHHOLDS. A product installed under an instance transform registers
    /// under a product code the transform produced, while the package cached for it
    /// declares the base code. So <see cref="Services.DeclaredProductCheck"/>, which
    /// reads a product code OUT OF A CACHED FILE and puts it to Windows, can be told
    /// there is no such record while a live registration still needs that file. The app
    /// has no way to tell WHICH cached file belongs to the second copy, which is the
    /// whole condition, so the walk-derived offer is withheld.
    ///
    /// AND NOT KNOWING WITHHOLDS ON THE SAME TERMS AS KNOWING. A read that failed leaves
    /// the machine in exactly the state the positive reading describes as far as this rule
    /// can tell, and so does a product the registry names that nothing shows was asked.
    /// A code Windows would not say was installed is never recovered, so the pass over
    /// the products the enumeration lost does not ask it; and a key whose name yields no
    /// code cannot be matched to any product that was asked. A rule that acted on the
    /// positive alone would be armed by the machines that answer and disarmed by the
    /// machines that do not.
    ///
    /// A BOOL RATHER THAN A SUM. The four count different things and adding them would
    /// produce a figure that reads as a product count and is not one. The counts are
    /// carried apart for the report, which reads them apart; the rule needs only whether
    /// any of them is above zero.
    ///
    /// WHAT IT READS. Every product the enumeration returned, asked one keyed read each;
    /// every product it lost that a key under <c>UserData</c> names and Windows confirms
    /// installed, asked the same way; and, through the two counts, every such key that
    /// names a code Windows would not answer about or carries a name yielding no code.
    /// Do not widen it to the chance that an enumeration is short: that is true of every
    /// scan, so the rule would then fire on all of them.
    /// </summary>
    public bool SecondInstanceNotRuledOut =>
        InstanceProductCount > 0
        || InstanceTypeUnreadableCount > 0
        || UnansweredProductCount > 0
        || UnparseableProductKeyNames > 0;
}
