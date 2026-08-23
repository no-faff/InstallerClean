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
/// Installed products this enumeration did not account for. Surfaced to the user
/// as the scan summary's kept-patches notice, and the trigger for withholding the
/// removable class.
///
/// FOUR contributors, and only the first two are failures to read. A product row
/// the API skipped (identity unknowable, counted one per row); a product whose
/// rows came back but whose LocalPackage value, or one of whose patch rows, would
/// not read. Then an absence rather than a failure: a cached file the registry
/// claims and the API never mentioned, computed NET of the read failures (see the
/// subtraction at the assembly site), so by construction it counts only products
/// where nothing failed to read at all. A product meeting more than one
/// contributor counts once.
///
/// The fourth is neither a failure to read nor an absence: a product the registry
/// named and this scan could not settle either way, Windows declining to say
/// whether the code is installed or the key name yielding no code to ask with. Its
/// opposite number, a registry product confirmed installed and recovered into the
/// questions the scan asks, contributes NOTHING here, and that asymmetry is the
/// point: a product that can be asked about is asked, and only a product nobody
/// can get an answer about withholds.
///
/// IT WAS CALLED <c>UnreadableProductCount</c> AND THAT NAME WAS A CAUSE STATED
/// FOR A MIXED SET, inside the app rather than on a screen: two of its four
/// contributors are not read failures at all, one being an absence and one an
/// inability to establish anything either way. Any sentence built on this number is
/// a sentence about all four, which is why the name now says only that the
/// enumeration could not account for them. The terms themselves are in
/// <see cref="Census"/>, separately, for anything that needs to say which.
///
/// It is not an exact headcount either, and cannot be made one. The unclaimed-file
/// term is an estimate the assembly site deliberately biases low, so the number
/// can run under the truth. It can no longer run OVER it: the term that could,
/// a difference between two product totals that a stale registry key inflates, is
/// gone, and the products behind such a difference are now asked about by name
/// instead. So no surface may present this as a count of programs.
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
public record InstallerQueryResult(
    IReadOnlyList<RegisteredPackage> Packages,
    int UnaccountedProductCount = 0,
    IReadOnlyList<PatchClaim>? PatchClaims = null,
    EnumerationCensus Census = default)
{
    /// <summary>Never null: an absent list reads as no claims rather than as a fault.</summary>
    public IReadOnlyList<PatchClaim> PatchClaims { get; init; } = PatchClaims ?? Array.Empty<PatchClaim>();

    /// <summary>
    /// The enumeration did not account for at least one installed product, so the
    /// set of registrations it read may be short of one. Whether that product's
    /// records failed to read or were never reached does not enter into it: what
    /// matters is the missing claim, not the mechanism (see
    /// <see cref="UnaccountedProductCount"/> for the four).
    ///
    /// WHAT IT BEARS ON IS NOW THE MISSING-FILES REPORT rather than the offer. It
    /// withholds every superseded-patch verdict, which it has always done; and a
    /// registration this scan never saw is also one whose file, had it gone, went
    /// uncounted. Exposed for the copy that says so.
    /// </summary>
    public bool RecordsIncomplete => UnaccountedProductCount > 0;
}

/// <summary>
/// The terms behind <see cref="InstallerQueryResult.UnaccountedProductCount"/>,
/// plus the shape facts one enumeration can see, carried separately so a report
/// never has to state one cause for a quantity built from several.
///
/// EVERY FIELD HERE IS INSTRUMENTATION. Nothing decides a file's fate on any of
/// them and nothing may start: the classification is settled where it is settled,
/// and a counter that acquired a consumer would be a second, quieter copy of a
/// rule that already exists in one place.
///
/// They exist because the evidence base for every safety claim this app makes is
/// one machine, and a single machine can falsify a universal and can never confirm
/// one. Each is a count or a fixed label, never a path, a name or an identifier.
/// </summary>
/// <param name="UnreadableProducts">
/// Products whose records came back short: a skipped enumeration row, an
/// unreadable <c>LocalPackage</c> value, or a patch enumeration that did not run
/// to a clean end. One per product however many of the three it met. An exact
/// per-product tally with no floor under it, which is what separates it from the
/// estimate the withholding is otherwise computed from.
/// </param>
/// <param name="SkippedProductRows">
/// Enumeration rows the product loop could not read at all, one per row. A subset
/// of <see cref="UnreadableProducts"/>, which is seeded from it and then grows.
/// Carried separately because the two answer different questions about one
/// product: that a claim was lost, and that the row itself never arrived.
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
/// <see cref="UnresolvableProductCount"/> is the arithmetic's one other input and
/// is not one of these, being neither a headcount nor an observation of the disk:
/// it is a tally of the questions that got no answer, and it is added to the
/// derived term rather than weighed against it.
///
/// <see cref="RegistryProductKeys"/> is NOT an input to any of it and still
/// travels. Nothing is derived from its difference against
/// <see cref="ProductCount"/>, that difference having turned out unable to tell a
/// truncated enumeration from ordinary registry residue; the products behind it
/// are asked about by name instead. How large it runs across real machines is a
/// fact about machines rather than about this app, and only these reports can
/// answer it.
/// </param>
/// <param name="NonStringLocalPackageValues">
/// Registrations whose <c>LocalPackage</c> value was present and was not a string.
/// Answers whether anything in the wild writes that value under a type other than
/// <c>REG_SZ</c>, which decides whether a string cast is a safe way to read it.
/// A subset of the fallback's failure count rather than a term beside it.
/// </param>
/// <param name="UnreadablePatchStates">
/// Patch claims whose <c>State</c> or <c>Uninstallable</c> read failed, one per
/// (patch, product) pairing asked. No file turns on it any more, every
/// registration being kept; what the number sizes is how often a machine cannot
/// answer a plain question about its own installer records at all, which is the
/// one thing these reports can establish and one machine never could.
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
/// the shape of a machine's cache and the thing the owner's own machine, at two
/// patches, is least like.
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
/// Nine of nine on one machine would say the cache is not named the way that
/// machine's other measurements assumed. The comparison it is for is against
/// <see cref="ScanResult.RegisteredPackages"/>'s own count in the same report,
/// which is why this is a count rather than the boolean the question was first
/// asked as.
/// </param>
/// <param name="RecoveredProductCount">
/// Products the registry named, the enumeration never returned, and a keyed ask
/// then found installed. THE TRUNCATION, MEASURED: a count of products identified
/// individually and confirmed one at a time, where every earlier attempt at this
/// question inferred it from the difference between two totals.
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
/// Between them these three report, per machine, what proportion of its registry
/// keys really were residue. That figure used to be guessed at by a tolerance band
/// on the difference between two product totals, set from one machine and unable
/// to tell residue from a truncated enumeration; the band is gone and these are
/// the measurement that replaced the guess.
/// </param>
/// <param name="InstanceProductCount">
/// Products that answered a non-zero <c>InstanceType</c>: installed as a second
/// instance of themselves under an instance transform. PRODUCTS, not files, and
/// not a count of anything held back.
///
/// IT DECIDES, AND THIS NOTE SAID FOR A WEEK THAT IT DECIDED NOTHING. That was true
/// when it was written and stopped being true the following day, which is the more
/// dangerous half: the sentence stayed well argued and specific while its subject
/// moved. It read that the condition emptied the whole offer until 3.0.0, on the
/// reading that a keyed question about the product code written inside a cached
/// package can answer "no record" while a registration under a transform-generated
/// code still needs the file, and that "nothing reads inside a file any more", so
/// the reading had no subject. Something reads a product code out of a cached file
/// again (<see cref="Services.DeclaredProductCheck"/>), and it is the last pass
/// standing between an unclaimed candidate and the offer. The subject came back and
/// nobody re-read the sentence.
///
/// What acts on it is <see cref="SecondInstanceNotRuledOut"/>, which reads this
/// count together with the one below and never on its own. This member is still
/// carried and sent apart, because how often a machine ANSWERS the question and how
/// often it REFUSES to are different facts and nobody has measured either.
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
/// WHICH PRODUCTS WERE ASKED AT ALL, and the answer moved in 3.0.0. Both counts
/// cover the products the enumeration returned AND the products it lost that the
/// registry named and the recovery pass resolved as installed
/// (<see cref="RecoveredProductCount"/>), which are asked one keyed read each. This
/// note used to say that a product the walk never reached was never asked, and that
/// is now true only of the two states the recovery cannot settle: a code Windows
/// would not answer about (<see cref="UnansweredProductCount"/>) and a registry key
/// whose name yielded no code (<see cref="UnparseableProductKeyNames"/>). Either of
/// those non-zero, or <see cref="UnreadableProducts"/> on a run where no fallback
/// named the lost product, makes both counts a floor.
/// </param>
/// <param name="ProductPatchKeyCount">
/// Products whose registry <c>Patches</c> key opened, from the per-product patch
/// listing the superseded-patch condition rests on.
///
/// THESE FOUR ARE CARRIED AND NOT YET SENT. They land here ahead of the rule that
/// consumes the reading and ahead of the payload fields that will report them, so the
/// three can be reviewed and reverted apart. Nothing outside this record reads them
/// today, which is deliberate and is not the licence to delete them that an unread
/// counter usually is. Against
/// <see cref="ProductCount"/> it says how usual it is for a product to carry one:
/// one machine reads 138 of 139, and a product with no patches has no reason to.
/// </param>
/// <param name="ProductPatchRegistrationCount">
/// Patch subkeys under those keys, one per (product, patch) registration rather
/// than per patch. With the count above it is the shape fact the measured machine
/// is least like, holding five.
/// </param>
/// <param name="ProductsWithRemovablePatchCount">
/// Products where at least one registered patch positively declared itself
/// removable, so a rollback on that product could reach for a superseded patch's
/// cached file.
///
/// IT IS THE FIGURE THAT SAYS WHAT THE CONDITION COSTS, and nothing measured on one
/// machine can answer it. On the machine every other measurement came from, the only
/// patch declaring itself removable sits on a Visual C++ redistributable with no
/// superseded patch to withhold, so the condition costs that machine nothing at all.
/// Whether that is usual is exactly what these reports exist to find out.
/// </param>
/// <param name="ProductsWithPatchSetUnestablishedCount">
/// Products whose patch set could not be established: the key would not open, or a
/// patch carried no <c>Uninstallable</c> or one that was not a number.
///
/// THE OTHER HALF OF THE SAME QUESTION, and kept apart from it because they are
/// different findings. One is the condition finding a reason to withhold; this is
/// the condition unable to look. A machine reading high here is a machine where the
/// fix is withholding without having established anything, which is safe and is not
/// the same as safe-and-informed.
/// </param>
/// <param name="PathResolverAttemptCount">
/// Recorded paths this scan put to the final-path resolver, which from 3.0.0 is
/// EVERY value that got past the embedded-null test and the expansion. Until then it
/// was only a value carrying a long-path or NT object prefix or an 8dot3 alias, and
/// the number therefore answered two questions at once: how many were asked, and how
/// many carried such a spelling. It answers only the first now.
/// <see cref="PathFlaggedSpellingCount"/> is where the second went, and it went
/// somewhere rather than being dropped because a report that quietly stops being able
/// to answer a question reads exactly like a machine with nothing to report.
///
/// THE FIVE OUTCOME COUNTS BELOW CANNOT BE READ WITHOUT IT. Most machines flag no
/// path at all, so the resolver is never asked and its five failures all read zero:
/// identical, on the wire, to a machine that asked and got five clean answers. This
/// is what tells those two apart, and a receiver reading any of the five without it
/// is reading a number that cannot mean what it appears to.
///
/// IT IS NO LONGER THE ONLY REASON THIS EXISTS, and the sentence saying so has been
/// taken out rather than qualified. The five below were carried to size a failure
/// before anything was designed around it; from 3.0.0 they also decide something,
/// and <see cref="AnyRecordedPathUnestablished"/> is where.
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
/// THE FIVE USED TO BE SPLIT INTO TWO ORDINARY MACHINE STATES AND THREE THAT COULD
/// NOT BE PRODUCED BY AN ABSENCE OR A PERMISSION, and that split has gone from these
/// notes because nothing reads it any more. It was there to argue that a count over
/// all five together could not be acted on. All five are now acted on alike, on the
/// one thing true of every member: the resolver was asked and did not answer.
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
/// expansion it precedes. The expansion cuts such a value at the null and returns
/// without throwing, so before this count existed the condition was invisible on
/// the platform the application runs on: the report said no path failed while one
/// had.
/// </param>
/// <param name="PathFlaggedSpellingCount">
/// Recorded values carrying a spelling only the filesystem can settle: an 8dot3
/// alias, or a prefix the strip left on for want of a drive root.
///
/// THE ONLY MEMBER OF THIS GROUP THAT IS NOT AN OUTCOME. Every other count here says
/// what happened to a value; this says what the value looked like, and it decides
/// nothing. A machine reporting a figure above zero is one holding the spellings the
/// resolution was built for, which is a question this project has been trying to
/// answer from real machines rather than from one.
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
    int PathFlaggedSpellingCount = 0)
{
    /// <summary>
    /// Every recorded value this scan could not turn into a path, whatever refused
    /// it: the sum of the four counts above, and the population the withholding
    /// acts on.
    ///
    /// IT IS A PROPERTY HERE SO THAT THE SUM EXISTS ONCE. The rule that reads it
    /// lives in another service and used to add the parts itself, which meant a
    /// member added to the split was a member that rule silently did not act on:
    /// the build stays green, the counter still reports, and the withholding just
    /// does not fire for the new cause. That is the failure this release found in
    /// the embedded-null case, and a hand-rolled sum is how it would arrive again.
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
    /// That is the failure this release found once already, in the count above, and
    /// it is why the question is asked where the members are declared. Anything added
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
    /// Whether this scan failed to establish that every product it could ask about is
    /// an ordinary single-instance installation. THE ONE THING THE SECOND-INSTANCE
    /// WITHHOLDING ASKS, and it is here rather than in the service that acts on it for
    /// the reason <see cref="AnyRecordedPathUnestablished"/> is: a rule that named the
    /// members itself would be one edit away from silently not acting on a member added
    /// later, with a green build and a counter still reporting.
    ///
    /// THE TWO MEMBERS ARE OPPOSITE FINDINGS AND THE SUPERORDINATE IS EXACT.
    /// <see cref="InstanceProductCount"/> is a positive answer that a product IS a second
    /// instance of itself; <see cref="InstanceTypeUnreadableCount"/> is a question that was
    /// put and not answered. The only thing true of both is the one this property is named
    /// for: the scan cannot say that no installed product is a second instance of itself.
    /// Nothing may state a cause over the pair, here or on any surface.
    ///
    /// WHY EITHER WITHHOLDS. A product installed under an instance transform registers
    /// under a product code the transform produced, while the package cached for it
    /// declares the base code. So the one pass that reads a product code OUT OF A CACHED
    /// FILE and puts it to Windows (<see cref="Services.DeclaredProductCheck"/>) can be
    /// told there is no such record while a live registration still needs that file, and
    /// that pass is the last thing standing between a candidate nothing claimed and the
    /// offer. It cannot be made to work: the app has no way to tell WHICH cached file
    /// belongs to the second copy, which is the whole condition.
    ///
    /// AND NOT KNOWING WITHHOLDS ON THE SAME TERMS AS KNOWING. A read that failed leaves
    /// the machine in exactly the state the positive reading describes as far as this rule
    /// can tell, and a rule that acted on the positive alone would be armed by the machines
    /// that answer and disarmed by the machines that do not.
    ///
    /// A BOOL RATHER THAN A SUM. The two count different things and adding them would
    /// produce a figure that reads as a product count and is not one. The counts are
    /// carried apart for the report, which reads them apart; the rule needs only whether
    /// either is above zero.
    ///
    /// WHAT IT DOES NOT REACH, stated because the boundary is a decision and not an
    /// oversight. A product that is installed and that this scan could not name at all,
    /// neither from the enumeration nor from the registry, is never asked and cannot be.
    /// That is the limit of every question the scan puts rather than of this one, and
    /// extending the rule to it would make it fire on the possibility that any enumeration
    /// anywhere is short, which is true of every scan and leaves the rule no floor.
    /// </summary>
    public bool SecondInstanceNotRuledOut =>
        InstanceProductCount > 0 || InstanceTypeUnreadableCount > 0;
}
