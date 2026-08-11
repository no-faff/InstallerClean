using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Unit tests for <see cref="IdentityVeto"/>, the check that reads a candidate's
/// own declared identity and puts it to Windows before the candidate can be
/// offered.
///
/// Both seams are faked. <see cref="IPackageIdentityReader"/> is faked because
/// the real one opens the file through msi.dll, and <see cref="IMsiApi"/> because
/// the answers that decide a file's fate are properties of the machine the suite
/// happens to run on. What is under test is the composition rule, and the whole
/// point of that rule is which combinations of answers permit an offer and which
/// keep the file, so every test here is about a combination no real machine would
/// reliably produce on demand.
///
/// THE DIRECTION IS THE INVARIANT. Only one outcome permits an offer, and it
/// requires every source to have answered and none of them to have claimed the
/// file. Each test that drives a failure asserts the file was KEPT, and asserts
/// WHICH cause it was kept for, because the three causes are reported separately
/// and a right outcome under a wrong cause is a defect here.
/// </summary>
public class IdentityVetoTests
{
    private const uint Success = 0, MoreData = 234, NoMoreItems = 259;
    private const uint UnknownProduct = 1605, UnknownProperty = 1608, UnknownPatch = 1647;

    /// <summary>
    /// ERROR_BAD_CONFIGURATION, standing for the whole class of return this code
    /// has no reading for. Nothing branches on its value: it is on neither
    /// allowlist, which is the whole of what makes it withhold.
    /// </summary>
    private const uint BadConfiguration = 1610;

    /// <summary>
    /// ERROR_INVALID_PARAMETER. What both keyed entry points return for the
    /// machine account in a per-user context, measured on one machine in every
    /// context and documented by Microsoft as a call that may not be made.
    /// </summary>
    private const uint InvalidParameter = 87;

    /// <summary>The registry subtree the veto reads, for the account ladder.</summary>
    private const string UserData = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData";

    private const string MachineSid = "S-1-5-18";
    private const string OtherAccountSid = "S-1-5-21-1-2-3-1001";

    private const string ProductA = "{AAAAAAAA-0000-0000-0000-000000000001}";
    private const string ProductB = "{BBBBBBBB-0000-0000-0000-000000000002}";
    private const string PatchP = "{CCCCCCCC-0000-0000-0000-000000000003}";

    private const string CacheFolder = @"C:\Windows\Installer";
    private const string FileA = @"C:\Windows\Installer\a.msi";
    private const string FileB = @"C:\Windows\Installer\b.msi";
    private const string PatchFile = @"C:\Windows\Installer\p.msp";

    // ---- Product candidates ----

    [Fact]
    public void Keeps_a_candidate_a_registered_product_answers_for()
    {
        var api = new FakeApi();
        api.Install(ProductA, sid: null, MsiInstallContext.Machine);

        var outcome = Screen(api, Reader((FileA, Product(ProductA))), FileA);

        Assert.Equal(CandidateIdentityOutcome.Claimed, outcome);
    }

    [Fact]
    public void Offers_a_candidate_no_source_knows()
    {
        // Nothing installed at all: every keyed ask answers "no such product"
        // and the enumeration ends immediately.
        var outcome = Screen(new FakeApi(), Reader((FileA, Product(ProductA))), FileA);

        Assert.Equal(CandidateIdentityOutcome.Unclaimed, outcome);
    }

    [Fact]
    public void Keeps_a_candidate_whose_record_exists_without_the_property_asked_for()
    {
        // ERROR_UNKNOWN_PROPERTY is the load-bearing case and the easiest to get
        // backwards. It is a record that was REACHED and does not carry the
        // property, so it proves the product exists. It is also what another
        // account's per-user product returns for most properties, measured on one
        // machine in 2026-08, so reading it as an absence would offer that
        // product's cached file.
        var api = new FakeApi();
        api.ProductAskResult[ProductA] = UnknownProperty;

        var outcome = Screen(api, Reader((FileA, Product(ProductA))), FileA);

        Assert.Equal(CandidateIdentityOutcome.Claimed, outcome);
    }

    [Fact]
    public void Keeps_a_candidate_belonging_to_another_accounts_per_user_product()
    {
        // The whole reason the ladder names every account rather than passing
        // null: a keyed read with no account named cannot see this product, so a
        // check that asked once would offer its cached file.
        var api = new FakeApi();
        api.Install(ProductA, OtherAccountSid, MsiInstallContext.UserUnmanaged);

        var outcome = Screen(api, Reader((FileA, Product(ProductA))), FileA,
            sids: new[] { MachineSid, OtherAccountSid });

        Assert.Equal(CandidateIdentityOutcome.Claimed, outcome);
    }

    [Fact]
    public void Keeps_a_candidate_only_the_filtered_enumeration_knows()
    {
        // The belt. Every keyed ask says no such product, and the enumeration
        // filtered to this one code still finds it, which is what covers a
        // registration under an account the ladder did not name.
        var api = new FakeApi();
        api.ProductEnumResult[ProductA] = Success;

        var outcome = Screen(api, Reader((FileA, Product(ProductA))), FileA);

        Assert.Equal(CandidateIdentityOutcome.Claimed, outcome);
    }

    [Fact]
    public void Keeps_a_candidate_whose_keyed_ask_returns_something_unreadable()
    {
        var api = new FakeApi();
        api.ProductAskResult[ProductA] = BadConfiguration;

        var outcome = Screen(api, Reader((FileA, Product(ProductA))), FileA);

        Assert.Equal(CandidateIdentityOutcome.RecordsUnaskable, outcome);
    }

    [Fact]
    public void Keeps_a_candidate_whose_enumeration_returns_something_unreadable()
    {
        // Every keyed ask answered cleanly that it does not know the product, and
        // the belt then failed. The file is kept rather than offered, because the
        // belt exists to catch what the ladder cannot see and a belt that did not
        // run has ruled nothing out.
        var api = new FakeApi();
        api.ProductEnumResult[ProductA] = BadConfiguration;

        var outcome = Screen(api, Reader((FileA, Product(ProductA))), FileA);

        Assert.Equal(CandidateIdentityOutcome.RecordsUnaskable, outcome);
    }

    // ---- The file's own identity ----

    [Fact]
    public void Keeps_a_candidate_whose_identity_could_not_be_read()
    {
        var outcome = Screen(new FakeApi(), Reader((FileA, null)), FileA);

        Assert.Equal(CandidateIdentityOutcome.IdentityUnreadable, outcome);
    }

    [Fact]
    public void An_unreadable_file_is_reported_as_unreadable_even_when_nothing_could_be_asked()
    {
        // Both inabilities present at once. The cause reported is the one that
        // actually occurred for this file: the read is what failed, and the
        // question was never reached, so naming the records would be false of it.
        var registry = Substitute.For<IRegistryReader>();
        registry.LocalMachineSubKeyNames(Arg.Any<string>()).Returns((string[]?)null);

        var result = new IdentityVeto(Reader((FileA, null)), new FakeApi(), registry, _ => { })
            .Screen(new[] { new IdentityCandidate(FileA, false) });

        Assert.Equal(CandidateIdentityOutcome.IdentityUnreadable, result.Outcomes[0]);
        Assert.Equal(1, result.IdentityUnreadableCount);
        Assert.Equal(0, result.RecordsUnaskableCount);
    }

    [Fact]
    public void Keeps_every_candidate_when_the_account_list_could_not_be_read()
    {
        // A list of accounts that could not be read is not a list of no accounts.
        // Without it the keyed asks cannot be made at all, so nothing has been
        // established about any candidate.
        var registry = Substitute.For<IRegistryReader>();
        registry.LocalMachineSubKeyNames(Arg.Any<string>()).Returns((string[]?)null);

        var result = new IdentityVeto(Reader((FileA, Product(ProductA))), new FakeApi(), registry, _ => { })
            .Screen(new[] { new IdentityCandidate(FileA, false) });

        Assert.Equal(CandidateIdentityOutcome.RecordsUnaskable, result.Outcomes[0]);
    }

    [Fact]
    public void An_empty_account_list_is_an_answer_and_the_machine_context_is_still_asked()
    {
        // Distinct from the test above: the key was read and holds nothing, so
        // there are no per-user registrations to ask about and the per-machine
        // ask stands on its own.
        var api = new FakeApi();
        api.Install(ProductA, sid: null, MsiInstallContext.Machine);

        var outcome = Screen(api, Reader((FileA, Product(ProductA))), FileA,
            sids: Array.Empty<string>());

        Assert.Equal(CandidateIdentityOutcome.Claimed, outcome);
    }

    [Fact]
    public void The_machine_account_is_never_asked_about_per_user_products()
    {
        // S-1-5-18 IS ALWAYS A UserData SUBKEY, being where per-machine
        // registrations live, so it reaches any ladder built from that key list.
        // Microsoft documents that it cannot be used that way and one machine
        // measured ERROR_INVALID_PARAMETER for it in every context, which is on
        // no allowlist and so reads as "the records could not be asked".
        //
        // Left in, it fires on the FIRST rung after the per-machine ask, so every
        // candidate whose product Windows genuinely does not know is withheld and
        // the offer is empty on every machine. This is the test that says the
        // ladder does not ask it.
        var api = new FakeApi();
        api.RejectsPerUserSid = MachineSid;

        var outcome = Screen(api, Reader((FileA, Product(ProductA))), FileA,
            sids: new[] { MachineSid, OtherAccountSid });

        Assert.Equal(CandidateIdentityOutcome.Unclaimed, outcome);
    }

    [Fact]
    public void A_real_account_answering_invalid_parameter_still_withholds()
    {
        // The other half, and it is what stops the fix above becoming a licence.
        // Only the machine account is left out of the per-user rungs. Any OTHER
        // account answering something outside the documented set is a question
        // that could not be put, and withholds.
        var api = new FakeApi();
        api.RejectsPerUserSid = OtherAccountSid;

        var outcome = Screen(api, Reader((FileA, Product(ProductA))), FileA,
            sids: new[] { MachineSid, OtherAccountSid });

        Assert.Equal(CandidateIdentityOutcome.RecordsUnaskable, outcome);
    }

    // ---- Patch candidates ----

    [Fact]
    public void Keeps_a_patch_a_target_product_still_holds()
    {
        var api = new FakeApi();
        api.Install(ProductA, sid: null, MsiInstallContext.Machine);
        api.Apply(PatchP, ProductA);

        var outcome = Screen(api, Reader((PatchFile, Patch(PatchP, ProductA))), PatchFile, isPatch: true);

        Assert.Equal(CandidateIdentityOutcome.Claimed, outcome);
    }

    [Fact]
    public void Offers_a_patch_no_target_product_holds()
    {
        // Both targets answer cleanly. An uninstalled target is a clean negative
        // rather than a failure: a product that is not there holds no patches, so
        // it does not hold this one.
        var api = new FakeApi();
        api.Install(ProductA, sid: null, MsiInstallContext.Machine);

        var outcome = Screen(api, Reader((PatchFile, Patch(PatchP, ProductA, ProductB))), PatchFile, isPatch: true);

        Assert.Equal(CandidateIdentityOutcome.Unclaimed, outcome);
    }

    [Fact]
    public void Keeps_a_patch_when_any_one_target_cannot_answer()
    {
        // The first target answers cleanly and the second cannot. The patch may
        // be held by exactly the one that did not answer, so one silent target is
        // enough to make the whole question unanswerable.
        var api = new FakeApi();
        api.Install(ProductA, sid: null, MsiInstallContext.Machine);
        api.PatchAskResult[(PatchP, ProductB)] = BadConfiguration;

        var outcome = Screen(api, Reader((PatchFile, Patch(PatchP, ProductA, ProductB))), PatchFile, isPatch: true);

        Assert.Equal(CandidateIdentityOutcome.RecordsUnaskable, outcome);
    }

    [Fact]
    public void Keeps_a_patch_only_the_target_enumeration_knows()
    {
        var api = new FakeApi();
        api.Install(ProductA, sid: null, MsiInstallContext.Machine);
        api.EnumeratedPatches[ProductA] = new List<string> { PatchP };

        var outcome = Screen(api, Reader((PatchFile, Patch(PatchP, ProductA))), PatchFile, isPatch: true);

        Assert.Equal(CandidateIdentityOutcome.Claimed, outcome);
    }

    [Fact]
    public void Keeps_a_patch_whose_target_enumeration_could_not_run_to_the_end()
    {
        // A short enumeration is a veto set that is short by an unknown amount,
        // which is a veto that does not fire. It answers the same way as no
        // enumeration at all.
        var api = new FakeApi();
        api.Install(ProductA, sid: null, MsiInstallContext.Machine);
        api.PatchEnumResult[ProductA] = BadConfiguration;

        var outcome = Screen(api, Reader((PatchFile, Patch(PatchP, ProductA))), PatchFile, isPatch: true);

        Assert.Equal(CandidateIdentityOutcome.RecordsUnaskable, outcome);
    }

    [Fact]
    public void Keeps_a_patch_that_names_no_target_product()
    {
        // The reader refuses to hand back an identity there is no way to ask
        // about, so this arrives as an unreadable identity rather than as a patch
        // with an empty target list. The cause reported is the file's.
        var outcome = Screen(new FakeApi(), Reader((PatchFile, null)), PatchFile, isPatch: true);

        Assert.Equal(CandidateIdentityOutcome.IdentityUnreadable, outcome);
    }

    // ---- Caching, which must not change any answer ----

    [Fact]
    public void Asks_once_about_a_product_code_two_candidates_share()
    {
        // The case that makes the pass affordable on a folder holding twenty
        // cached versions of one product.
        var api = new FakeApi();
        var reader = Reader((FileA, Product(ProductA)), (FileB, Product(ProductA)));

        var result = Veto(api, reader).Screen(new[]
        {
            new IdentityCandidate(FileA, false),
            new IdentityCandidate(FileB, false),
        });

        Assert.Equal(1, result.DistinctIdentitiesAsked);
        Assert.Equal(1, result.IdentityCacheHits);
        Assert.Equal(2, result.Outcomes.Count(o => o == CandidateIdentityOutcome.Unclaimed));
        // Counted independently of the result's own figures, so the cache is
        // shown to be real rather than merely reported. The default ladder is the
        // per-machine ask plus one real account's two contexts, the machine
        // account being no part of it, so three asks is one code asked once and
        // six would be one code asked twice.
        Assert.Equal(3, api.ProductAsks.Count);
    }

    [Fact]
    public void Asks_again_for_one_patch_code_declaring_different_targets()
    {
        // Two files declaring one patch code and different target lists are two
        // different questions, and the answer to the shorter one is the weaker.
        // Keying the cache on the code alone would reuse it.
        var api = new FakeApi();
        api.Install(ProductB, sid: null, MsiInstallContext.Machine);
        api.Apply(PatchP, ProductB);
        var reader = Reader(
            (PatchFile, Patch(PatchP, ProductA)),
            (@"C:\Windows\Installer\q.msp", Patch(PatchP, ProductA, ProductB)));

        var result = Veto(api, reader).Screen(new[]
        {
            new IdentityCandidate(PatchFile, true),
            new IdentityCandidate(@"C:\Windows\Installer\q.msp", true),
        });

        Assert.Equal(2, result.DistinctIdentitiesAsked);
        Assert.Equal(CandidateIdentityOutcome.Unclaimed, result.Outcomes[0]);
        Assert.Equal(CandidateIdentityOutcome.Claimed, result.Outcomes[1]);
    }

    // ---- The pass itself ----

    [Fact]
    public void An_empty_pass_asks_nothing_and_reads_no_accounts()
    {
        var registry = Substitute.For<IRegistryReader>();

        var result = new IdentityVeto(Reader(), new FakeApi(), registry, _ => { })
            .Screen(Array.Empty<IdentityCandidate>());

        Assert.Empty(result.Outcomes);
        Assert.Equal(0, result.ClaimedCount);
        Assert.Equal(0, result.IdentityUnreadableCount);
        Assert.Equal(0, result.RecordsUnaskableCount);
        registry.DidNotReceive().LocalMachineSubKeyNames(Arg.Any<string>());
    }

    [Fact]
    public void Reports_progress_before_the_first_candidate_is_read()
    {
        // The pass opens every candidate, so on a large folder it is the longest
        // part of a scan. A first report that waited for the interval would leave
        // the overlay static across the slowest single file there is.
        var reports = new List<ScanProgressUpdate>();
        // Assembled with the running platform's separator rather than written as
        // a literal, which every other path here is. The ticker names the file
        // through Path.GetFileName, and that does not treat a backslash as a
        // separator off Windows, so a literal Windows path would make this
        // assertion mean one thing on the CI host and another anywhere the logic
        // is exercised from. The app is a Windows app and production is
        // unaffected; this is about the test saying the same thing twice.
        var path = Path.Combine(CacheFolder, "a.msi");

        Veto(new FakeApi(), Reader((path, Product(ProductA))))
            .Screen(new[] { new IdentityCandidate(path, false) },
                new ImmediateProgress(reports.Add));

        var report = Assert.Single(reports);
        Assert.Equal("a.msi", report.Message);
        // Never a milestone: a screen reader must not be made to announce one
        // update per file over a folder of any size.
        Assert.False(report.IsMilestone);
    }

    [Fact]
    public void Stops_when_cancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            Veto(new FakeApi(), Reader((FileA, Product(ProductA))))
                .Screen(new[] { new IdentityCandidate(FileA, false) }, null, cts.Token));
    }

    [Fact]
    public void The_counts_account_for_every_candidate()
    {
        // The counts are derived from the verdicts rather than tallied alongside
        // them, so this pins the arithmetic the report will be built on: three
        // causes plus the offered ones, summing to the whole set and nothing over.
        var api = new FakeApi();
        api.Install(ProductA, sid: null, MsiInstallContext.Machine);
        api.ProductAskResult[ProductB] = BadConfiguration;
        var reader = Reader(
            (FileA, Product(ProductA)),                              // claimed
            (FileB, Product(ProductB)),                              // unaskable
            (@"C:\Windows\Installer\c.msi", null),                   // unreadable
            (@"C:\Windows\Installer\d.msi", Product("{DDDDDDDD-0000-0000-0000-000000000004}")));

        var result = Veto(api, reader).Screen(new[]
        {
            new IdentityCandidate(FileA, false),
            new IdentityCandidate(FileB, false),
            new IdentityCandidate(@"C:\Windows\Installer\c.msi", false),
            new IdentityCandidate(@"C:\Windows\Installer\d.msi", false),
        });

        Assert.Equal(1, result.ClaimedCount);
        Assert.Equal(1, result.RecordsUnaskableCount);
        Assert.Equal(1, result.IdentityUnreadableCount);
        // The three counts and the outcome list, never a total over them: the
        // three are three different findings and the pass exposes no sum.
        Assert.Equal(4, result.Outcomes.Count);
        Assert.Equal(1, result.Outcomes.Count(o => o == CandidateIdentityOutcome.Unclaimed));
    }

    // ---- The machine reading: products installed under instance transforms ----

    [Fact]
    public void Withholds_where_the_machine_installs_products_under_instance_transforms()
    {
        // Windows knows nothing about the code this file declares, which on an
        // ordinary machine is the one answer that permits an offer. Here it does
        // not, because a product installed with an instance transform is
        // registered under a code the transform produced while the package cached
        // for it declares the base code, so "no record of the base code" and "no
        // registration needs this file" have come apart.
        var outcome = Screen(new FakeApi(), Reader((FileA, Product(ProductA))), FileA,
            instanceType: "1");

        Assert.Equal(CandidateIdentityOutcome.InstanceTransformsInUse, outcome);
    }

    [Fact]
    public void An_ordinary_machine_still_offers_what_Windows_answered_for()
    {
        // The must-fail control. Without it the test above passes equally against
        // a veto that withholds everything unconditionally, and the two are
        // indistinguishable from the outside: both report an empty offer.
        var outcome = Screen(new FakeApi(), Reader((FileA, Product(ProductA))), FileA,
            instanceType: "0");

        Assert.Equal(CandidateIdentityOutcome.Unclaimed, outcome);
    }

    [Theory]
    // Zero is how a product installed normally reads, and an empty or unparseable
    // value is a machine nothing here anticipated. None of them says this machine
    // does instance installs, and reading any as though it did would empty the
    // offer on every machine that answers in a shape nobody expected.
    [InlineData("0")]
    [InlineData("")]
    [InlineData("not a number")]
    [InlineData("   ")]
    public void Only_a_positive_reading_withholds(string value)
    {
        var outcome = Screen(new FakeApi(), Reader((FileA, Product(ProductA))), FileA,
            instanceType: value);

        Assert.Equal(CandidateIdentityOutcome.Unclaimed, outcome);
    }

    [Theory]
    // Compared as a number, not against the string "1", because nothing documents
    // the spelling the API returns it in and a machine answering with padding
    // would read as an ordinary product on a string test.
    [InlineData("1")]
    [InlineData(" 1 ")]
    [InlineData("2")]
    public void Any_non_zero_instance_type_withholds(string value)
    {
        var outcome = Screen(new FakeApi(), Reader((FileA, Product(ProductA))), FileA,
            instanceType: value);

        Assert.Equal(CandidateIdentityOutcome.InstanceTransformsInUse, outcome);
    }

    [Fact]
    public void An_enumeration_that_fails_leaves_the_scan_where_it_was()
    {
        // This is a withholding TRIGGER and not a release condition, so its failing
        // to fire costs nothing that was not already being spent: the scan behaves
        // exactly as it did before the reading existed. ERROR_ACCESS_DENIED is the
        // documented return for a call made without the privileges to enumerate
        // across accounts, and it is on no allowlist here for the same reason.
        var api = new FakeApi { UnfilteredEnumResult = 5 };   // ERROR_ACCESS_DENIED

        var outcome = Screen(api, Reader((FileA, Product(ProductA))), FileA, instanceType: "1");

        Assert.Equal(CandidateIdentityOutcome.Unclaimed, outcome);
    }

    [Fact]
    public void A_machine_whose_walk_returns_nothing_leaves_the_scan_where_it_was()
    {
        var outcome = Screen(new FakeApi(), Reader((FileA, Product(ProductA))), FileA);

        Assert.Equal(CandidateIdentityOutcome.Unclaimed, outcome);
    }

    [Fact]
    public void The_machine_reading_cannot_turn_a_claim_into_an_offer()
    {
        // The invariant, from the direction that would actually hurt. The reading
        // touches one arm of the verdict and only ever moves it towards keeping
        // the file; a product Windows positively answers for stays claimed
        // whatever the machine does with instances.
        var api = new FakeApi();
        api.Install(ProductA, sid: null, MsiInstallContext.Machine);

        var outcome = Screen(api, Reader((FileA, Product(ProductA))), FileA,
            instanceType: "1");

        Assert.Equal(CandidateIdentityOutcome.Claimed, outcome);
    }

    [Fact]
    public void The_machine_reading_cannot_turn_an_unanswerable_question_into_an_offer()
    {
        // The same invariant on the other keeping state, so a future change that
        // reordered the switch arms could not quietly promote one.
        var api = new FakeApi();
        api.ProductAskResult[ProductA] = BadConfiguration;

        var outcome = Screen(api, Reader((FileA, Product(ProductA))), FileA,
            instanceType: "1");

        Assert.Equal(CandidateIdentityOutcome.RecordsUnaskable, outcome);
    }

    [Fact]
    public void The_withheld_candidates_are_counted_apart_from_the_other_three()
    {
        // Four causes, four counts, no total. A candidate every source answered
        // for is not an unreadable file and is not an unanswerable question, and
        // a surface reaching for one sentence over all four would say something
        // false of three of them.
        var result = Veto(new FakeApi(), Reader((FileA, Product(ProductA))),
                instanceType: "1")
            .Screen(new[] { new IdentityCandidate(FileA, false) });

        Assert.Equal(1, result.InstanceTransformsInUseCount);
        Assert.Equal(0, result.ClaimedCount);
        Assert.Equal(0, result.IdentityUnreadableCount);
        Assert.Equal(0, result.RecordsUnaskableCount);
    }

    // ---- Helpers ----

    /// <param name="instanceType">
    /// What this machine's one enumerable product answers to <c>InstanceType</c>,
    /// or null for a machine whose walk returns no products at all. Null is the
    /// ordinary machine and is what every test that is not about this gets.
    /// </param>
    private static IdentityVeto Veto(FakeApi api, IPackageIdentityReader reader, string[]? sids = null,
        string? instanceType = null)
    {
        if (instanceType is not null)
        {
            // A DIFFERENT PRODUCT FROM THE CANDIDATE'S. The instance product is by
            // definition one whose code the candidate file does not carry, so a
            // test that gave them the same code would be exercising a case that
            // cannot occur.
            api.Enumerable.Add(ProductB);
            api.InstanceTypes[ProductB] = instanceType;
        }

        var registry = Substitute.For<IRegistryReader>();
        registry.LocalMachineSubKeyNames(UserData).Returns(
            sids ?? new[] { MachineSid, OtherAccountSid });
        // The crash-log sink is bound to a no-op: several of these drive the
        // unreadable branch deliberately, and the real sink would append to the
        // log of whatever machine ran the suite.
        return new IdentityVeto(reader, api, registry, _ => { });
    }

    private static CandidateIdentityOutcome Screen(
        FakeApi api, IPackageIdentityReader reader, string path, bool isPatch = false, string[]? sids = null,
        string? instanceType = null) =>
        Veto(api, reader, sids, instanceType)
            .Screen(new[] { new IdentityCandidate(path, isPatch) })
            .Outcomes[0];

    private static PackageIdentity Product(string code) =>
        new(code, IsPatch: false, Array.Empty<string>());

    private static PackageIdentity Patch(string code, params string[] targets) =>
        new(code, IsPatch: true, targets);

    private static IPackageIdentityReader Reader(params (string Path, PackageIdentity? Identity)[] entries)
    {
        var reader = new FakeReader();
        foreach (var (path, identity) in entries) reader.Identities[path] = identity;
        return reader;
    }

    /// <summary>
    /// An <see cref="IProgress{T}"/> that calls back on the reporting thread.
    /// <see cref="Progress{T}"/> posts to a synchronisation context or the thread
    /// pool, so a test using it races its own assertion.
    /// </summary>
    private sealed class ImmediateProgress(Action<ScanProgressUpdate> onReport) : IProgress<ScanProgressUpdate>
    {
        public void Report(ScanProgressUpdate value) => onReport(value);
    }

    private sealed class FakeReader : IPackageIdentityReader
    {
        public Dictionary<string, PackageIdentity?> Identities { get; } = new(StringComparer.OrdinalIgnoreCase);

        public PackageIdentity? Read(string filePath, bool isPatch, out string detail)
        {
            var identity = Identities.GetValueOrDefault(filePath);
            detail = identity is null ? "test: no identity" : string.Empty;
            return identity;
        }
    }

    /// <summary>
    /// Answers the four keyed and filtered questions the veto asks, off a
    /// declared machine state. Deliberately not the enumeration-walking fake the
    /// query service's tests use: the veto never walks the machine's product
    /// list, and a fake that modelled one would be answering a question this code
    /// does not ask.
    /// </summary>
    private sealed class FakeApi : IMsiApi
    {
        public List<(string Code, string? Sid, MsiInstallContext Context)> Installed { get; } = new();
        public List<(string PatchCode, string ProductCode)> AppliedPatches { get; } = new();

        /// <summary>Forces a return out of every keyed product read for one code.</summary>
        public Dictionary<string, uint> ProductAskResult { get; } = new();

        /// <summary>Forces a return out of the keyed patch read for one pairing.</summary>
        public Dictionary<(string PatchCode, string ProductCode), uint> PatchAskResult { get; } = new();

        /// <summary>Forces a return out of the filtered product enumeration for one code.</summary>
        public Dictionary<string, uint> ProductEnumResult { get; } = new();

        /// <summary>Forces a return out of one product's patch enumeration.</summary>
        public Dictionary<string, uint> PatchEnumResult { get; } = new();

        /// <summary>
        /// Patch codes the enumeration reports for a product WITHOUT the keyed
        /// read answering for them, which is the only way to exercise the belt on
        /// its own.
        /// </summary>
        public Dictionary<string, List<string>> EnumeratedPatches { get; } = new();

        /// <summary>
        /// What the machine-wide walk returns, in order, and what each of those
        /// products answers to InstanceType. Only the instance reading uses either.
        /// </summary>
        public List<string> Enumerable { get; } = new();

        public Dictionary<string, string> InstanceTypes { get; } = new();

        /// <summary>Forces a return out of the unfiltered walk, for the failure cases.</summary>
        public uint? UnfilteredEnumResult { get; set; }

        /// <summary>Every product code a keyed read was made for, for the caching tests.</summary>
        public List<string> ProductAsks { get; } = new();

        /// <summary>
        /// A SID that refuses every per-user question, whatever the product. It
        /// models the machine account, which the API rejects in that position.
        /// </summary>
        public string? RejectsPerUserSid { get; set; }

        public void Install(string code, string? sid, MsiInstallContext context) =>
            Installed.Add((code, sid, context));

        public void Apply(string patchCode, string productCode) =>
            AppliedPatches.Add((patchCode, productCode));

        public uint GetProductInfo(string productCode, string? userSid, MsiInstallContext context,
            string property, char[]? value, ref uint valueLength)
        {
            // Answered before the ask is recorded, because the instance reading is
            // not one of the asks the caching tests count.
            if (property == MsiInstallProperty.InstanceType)
            {
                if (!InstanceTypes.TryGetValue(productCode, out var it))
                {
                    valueLength = 0;
                    return UnknownProperty;
                }
                if (value is null)
                {
                    valueLength = (uint)it.Length;
                    return it.Length == 0 ? Success : MoreData;
                }
                it.AsSpan().CopyTo(value);
                valueLength = (uint)it.Length;
                return Success;
            }

            ProductAsks.Add(productCode);
            valueLength = 0;
            if (RejectsPerUserSid is not null && userSid == RejectsPerUserSid) return InvalidParameter;
            if (ProductAskResult.TryGetValue(productCode, out var forced)) return forced;

            var known = Installed.Any(p =>
                p.Code == productCode && p.Sid == userSid && p.Context == context);
            if (!known) return UnknownProduct;

            // A product with a name: the sizing call reports the length and asks
            // to be called again, which is one of the two returns that establish
            // the record is there.
            valueLength = 7;
            return MoreData;
        }

        public uint GetPatchInfo(string patchCode, string productCode, string? userSid,
            MsiInstallContext context, string property, char[]? value, ref uint valueLength)
        {
            valueLength = 0;
            if (PatchAskResult.TryGetValue((patchCode, productCode), out var forced)) return forced;

            var productKnown = Installed.Any(p =>
                p.Code == productCode && p.Sid == userSid && p.Context == context);
            if (!productKnown) return UnknownProduct;

            if (!AppliedPatches.Contains((patchCode, productCode))) return UnknownPatch;

            // State is present and empty: a zero-length success, which is the
            // other return that establishes the record is there.
            return Success;
        }

        public uint EnumProducts(string? productCode, string? userSid, MsiInstallContext context,
            uint index, char[]? installedProductCode, out MsiInstallContext installedContext,
            char[]? sid, ref uint sidLength)
        {
            installedContext = MsiInstallContext.Machine;
            sidLength = 0;

            // The UNFILTERED form, which is a different question from the filtered
            // one below: "what does this machine have" rather than "does it have
            // this". The instance reading is the only caller, walking by index.
            if (productCode is null)
            {
                if (UnfilteredEnumResult is { } forcedAll) return forcedAll;
                if (index >= Enumerable.Count) return NoMoreItems;
                Enumerable[(int)index].AsSpan().CopyTo(installedProductCode!);
                return Success;
            }

            if (ProductEnumResult.TryGetValue(productCode, out var forced)) return forced;
            if (index > 0) return NoMoreItems;
            return Installed.Any(p => p.Code == productCode) ? Success : NoMoreItems;
        }

        public uint EnumPatches(string? productCode, string? userSid, MsiInstallContext context,
            MsiPatchFilter filter, uint index, char[]? patchCode, char[]? targetProductCode,
            out MsiInstallContext targetProductContext, char[]? targetUserSid, ref uint targetUserSidLength)
        {
            targetProductContext = MsiInstallContext.Machine;
            targetUserSidLength = 0;
            if (productCode is null) return NoMoreItems;
            if (PatchEnumResult.TryGetValue(productCode, out var forced)) return forced;

            var codes = EnumeratedPatches.GetValueOrDefault(productCode)
                ?? AppliedPatches.Where(p => p.ProductCode == productCode).Select(p => p.PatchCode).ToList();
            if (index >= codes.Count) return NoMoreItems;

            var code = codes[(int)index];
            if (patchCode is not null)
                for (var i = 0; i < code.Length && i < patchCode.Length - 1; i++) patchCode[i] = code[i];
            return Success;
        }
    }
}
