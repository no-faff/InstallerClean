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
/// IT DECIDES NOTHING, WHICH IS THE POINT OF IT. Up to 3.0.0 this condition
/// emptied the whole offer, on the reading that a keyed question about the product
/// code written inside a cached package can answer "no record" while a
/// registration under a transform-generated code still needs the file. Nothing
/// reads inside a file any more, so that reading has no subject and the condition
/// picks out no risk this machine does not carry for every other product. What
/// remains is a fact about machines that nobody anywhere has measured: whether a
/// machine carrying such a product exists in the field at all. Counted and sent;
/// acted on nowhere.
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
/// uses, and it composes with them: a complete negative is a zero here AND a zero
/// in <see cref="UnreadableProducts"/>, <see cref="UnansweredProductCount"/> and
/// <see cref="UnparseableProductKeyNames"/>, because a product the walk never
/// reached was never asked this question either. Any of those non-zero makes the
/// count above a floor.
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
    int ProductsWithPatchSetUnestablishedCount = 0);
