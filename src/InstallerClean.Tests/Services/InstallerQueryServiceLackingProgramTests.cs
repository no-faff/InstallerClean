using InstallerClean.Helpers;
using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// A superseded patch's cached file is kept while a program the patch is for, which does
/// not hold it, could still be given it by an installation that opens the file. Each test
/// drives the pass that confirms a removable verdict, as the patch truncation tests do: one
/// program holds the patch superseded and declaring zero, on clean patch sets, so the file
/// is listed unless the program that lacks the patch keeps it.
/// </summary>
public class InstallerQueryServiceLackingProgramTests
{
    private const string Holder = "{AAAAAAAA-1111-0000-0000-00000000000A}";
    private const string Lacking = "{BBBBBBBB-1111-0000-0000-00000000000B}";
    private const string AlsoLacking = "{DDDDDDDD-1111-0000-0000-00000000000D}";
    private const string Patch = "{CCCCCCCC-1111-0000-0000-00000000000C}";
    private const string File = @"C:\Windows\Installer\superseded.msp";

    private const string AnAccount = "S-1-5-21-1111111111-2222222222-3333333333-1001";
    private const string AnotherAccount = "S-1-5-21-1111111111-2222222222-3333333333-1002";
    private const string LocalSystem = "S-1-5-18";

    /// <summary>
    /// A machine as the confirmation pass is handed one: the installations the product
    /// walk listed, the patch claims the enumeration read, and behind them a
    /// <see cref="FakeMsiApi"/> answering every keyed question. <see cref="Holder"/> holds
    /// the patch superseded and declaring zero, for the machine unless a test installs it
    /// under an account. A program a test names in the Template and says nothing more
    /// about is installed for the machine, off the walk, and holds no patch.
    /// </summary>
    private sealed class Machine
    {
        public FakeMsiApi Msi { get; } = new();

        private readonly List<(string ProductCode, string? UserSid, MsiInstallContext Context)> _walked = new();
        private readonly List<(string Product, string Patch, string Path, string State, string Uninstallable)> _holds = new();

        public Machine(string? holderAccount = null)
        {
            Install(Holder, holderAccount);
            Hold(Holder, state: "2", uninstallable: "0");
        }

        /// <summary>An installation the product walk lists, per user where an account is given.</summary>
        public void Install(string code, string? account = null,
            MsiInstallContext context = MsiInstallContext.UserUnmanaged)
        {
            Msi.AddProduct(code);
            if (account is null)
            {
                _walked.Add((code, null, MsiInstallContext.Machine));
                return;
            }

            Msi.WalkInstances[code] = (account, context);
            Msi.KeyedInstances[code] = [(account, context)];
            _walked.Add((code, account, context));
        }

        /// <summary>A patch the program holds, with the claim the enumeration read for it.</summary>
        public void Hold(string code, string state, string uninstallable, string patch = Patch, string path = File)
        {
            Msi.AddPatch(code, patch, path, state, uninstallable);
            _holds.Add((code, patch, path, state, uninstallable));
        }

        /// <summary>
        /// The program holds the patch and answers for it by name, and no claim of its
        /// own reached the enumeration.
        /// </summary>
        public void HoldWithNoClaim(string code, string state, string uninstallable)
        {
            Msi.SetPatchProperty(Patch, code, "State", state);
            Msi.SetPatchProperty(Patch, code, "Uninstallable", uninstallable);
        }

        /// <summary>
        /// Runs the confirmation pass over this machine with a patch file whose Template
        /// names <paramref name="named"/>, on clean patch sets, and returns the file's row.
        /// </summary>
        public RegisteredPackage TheFile(params string[] named) => Rows(named)[File];

        /// <summary>The same pass, returning every row, for a machine with more than one file.</summary>
        public Dictionary<string, RegisteredPackage> Rows(params string[] named)
        {
            var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);
            var claims = new List<PatchClaim>();
            foreach (var (product, patch, path, state, uninstallable) in _holds)
            {
                var (_, sid, context) = _walked.Single(w => w.ProductCode == product);
                claims.Add(new PatchClaim(path, patch, product, sid, (int)context));
                int.TryParse(state, out var patchState);
                InstallerQueryService.MergeClaim(
                    claimed,
                    new RegisteredPackage(path, "Product", product, patchState,
                        InstallerQueryService.IsRemovablePatch(state, uninstallable)),
                    InstallerQueryService.ClaimSource.InstallerApi);
            }

            var patchSets = named.Concat(_walked.Select(w => w.ProductCode)).Distinct()
                .ToDictionary(c => c, _ => ProductPatchSet.AllNonRemovable, StringComparer.OrdinalIgnoreCase);

            new InstallerQueryService(Msi, NoFallback, null, new TemplateReader(named))
                .ConfirmRemovableAgainstEveryProduct(
                    claimed, claims, _walked.ToList(), [], default, patchSets, patchSets, CancellationToken.None);

            return claimed;
        }
    }

    private static InstallerQueryService.FallbackRead NoFallback(
        Dictionary<string, RegisteredPackage> claimed, CancellationToken ct) => new(0, 0);

    private static void AssertListed(RegisteredPackage row)
    {
        Assert.True(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
        Assert.Equal(LackingProgramKeep.None, row.KeptForLackingProgram);
    }

    private static void AssertKept(RegisteredPackage row, LackingProgramKeep answer)
    {
        Assert.False(row.IsRemovable);
        Assert.Equal(answer, row.KeptForLackingProgram);
        // An answer Windows gave keeps the file on a finding, and one that was not had
        // keeps it as a read that established nothing.
        Assert.Equal(answer == LackingProgramKeep.AnswerNotHad, row.RemovableWithheld);
    }

    /// <summary>
    /// THE MUST-HIT CONTROL. With no program but its holder named, the file is listed
    /// and nothing is asked, so every test below that keeps it does so on the program
    /// that lacks the patch.
    /// </summary>
    [Fact]
    public void A_patch_naming_only_its_holder_is_listed_and_asks_nothing()
    {
        var machine = new Machine();
        var msi = machine.Msi;

        AssertListed(machine.TheFile(Holder));
        Assert.Empty(msi.PatchSequenceAsks);
    }

    [Fact]
    public void A_program_for_the_machine_that_would_take_the_patch_keeps_the_file()
    {
        var machine = new Machine();
        var msi = machine.Msi;
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.WouldApply;

        AssertKept(machine.TheFile(Holder, Lacking), LackingProgramKeep.WouldApply);

        // Asked in the program's own context, with no account for the machine's, and
        // with the cached file itself.
        Assert.Equal(
            [(Lacking, (string?)null, MsiInstallContext.Machine, File)],
            msi.PatchSequenceAsks);
    }

    [Fact]
    public void A_program_the_patch_is_superseded_on_keeps_the_file()
    {
        var machine = new Machine();
        var msi = machine.Msi;
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.SupersededThere;

        AssertKept(machine.TheFile(Holder, Lacking), LackingProgramKeep.SupersededThere);
    }

    [Fact]
    public void Only_the_answer_that_the_patch_cannot_apply_lets_the_file_be_listed()
    {
        var machine = new Machine();
        var msi = machine.Msi;
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.CannotApply;

        AssertListed(machine.TheFile(Holder, Lacking));
        Assert.Single(msi.PatchSequenceAsks);
    }

    [Theory]
    [InlineData(1642u)] // the call itself failing with the code that, as a status, lists the file
    [InlineData(1648u)]
    [InlineData(87u)]
    public void A_call_that_fails_keeps_the_file(uint error)
    {
        var machine = new Machine();
        var msi = machine.Msi;
        msi.PatchSequence[(Lacking, File)] = (error, 0xFFFFFFFF, 1642);

        AssertKept(machine.TheFile(Holder, Lacking), LackingProgramKeep.AnswerNotHad);
    }

    [Theory]
    // Not applied, with a status that is neither success nor that the patch cannot apply.
    [InlineData(0xFFFFFFFFu, 1643u)]
    [InlineData(0xFFFFFFFFu, 1636u)]
    // Not applied, with the status the call never wrote.
    [InlineData(0xFFFFFFFFu, 0xFFFFFFFFu)]
    // Given a place in the order, with a status that is not success.
    [InlineData(0u, 1642u)]
    [InlineData(3u, 1603u)]
    public void A_call_that_succeeds_with_any_other_order_and_status_is_no_answer_and_keeps_the_file(
        uint order, uint status)
    {
        var machine = new Machine();
        var msi = machine.Msi;
        msi.PatchSequence[(Lacking, File)] = (0, order, status);

        AssertKept(machine.TheFile(Holder, Lacking), LackingProgramKeep.AnswerNotHad);
    }

    [Fact]
    public void A_place_later_in_the_order_is_an_answer_that_the_patch_would_apply()
    {
        var machine = new Machine();
        var msi = machine.Msi;
        msi.PatchSequence[(Lacking, File)] = (0, 4, 0);

        AssertKept(machine.TheFile(Holder, Lacking), LackingProgramKeep.WouldApply);
    }

    [Theory]
    [InlineData("1")] // advertised and not installed
    [InlineData("")]
    [InlineData("2")]
    public void A_program_that_does_not_read_as_installed_keeps_the_file_and_is_not_asked(string state)
    {
        var machine = new Machine();
        var msi = machine.Msi;
        msi.SetProductProperty(Lacking, MsiInstallProperty.State, state);
        // The answer that would list the file, so the keep below is the state's.
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.CannotApply;

        AssertKept(machine.TheFile(Holder, Lacking), LackingProgramKeep.AnswerNotHad);
        Assert.Empty(msi.PatchSequenceAsks);
    }

    [Fact]
    public void A_program_whose_state_will_not_read_keeps_the_file()
    {
        var machine = new Machine();
        var msi = machine.Msi;
        msi.ProductPropertyResult[(Lacking, MsiInstallProperty.State)] = 1610;
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.CannotApply;

        AssertKept(machine.TheFile(Holder, Lacking), LackingProgramKeep.AnswerNotHad);
    }

    [Theory]
    [InlineData(MsiInstallContext.UserUnmanaged)]
    [InlineData(MsiInstallContext.UserManaged)]
    public void A_program_the_local_system_installed_per_user_never_keeps_the_file_and_is_never_asked(
        MsiInstallContext context)
    {
        var machine = new Machine();
        var msi = machine.Msi;
        machine.Install(Lacking, LocalSystem, context);
        // The answer that would keep the file, so the listing below is the rule's.
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.WouldApply;

        AssertListed(machine.TheFile(Holder, Lacking));
        Assert.Empty(msi.PatchSequenceAsks);
    }

    [Fact]
    public void A_per_user_program_does_not_keep_a_file_registered_for_the_machine()
    {
        var machine = new Machine();
        var msi = machine.Msi;
        machine.Install(Lacking, AnAccount);
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.WouldApply;

        AssertListed(machine.TheFile(Holder, Lacking));
        Assert.Empty(msi.PatchSequenceAsks);
    }

    [Fact]
    public void A_program_for_the_machine_does_not_keep_a_file_registered_under_an_account()
    {
        var machine = new Machine(holderAccount: AnAccount);
        var msi = machine.Msi;
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.WouldApply;

        AssertListed(machine.TheFile(Holder, Lacking));
        Assert.Empty(msi.PatchSequenceAsks);
    }

    [Fact]
    public void A_program_of_another_account_does_not_keep_a_file_registered_under_an_account()
    {
        var machine = new Machine(holderAccount: AnAccount);
        var msi = machine.Msi;
        machine.Install(Lacking, AnotherAccount);
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.WouldApply;

        AssertListed(machine.TheFile(Holder, Lacking));
        Assert.Empty(msi.PatchSequenceAsks);
    }

    [Theory]
    [InlineData(MsiInstallContext.UserUnmanaged)]
    [InlineData(MsiInstallContext.UserManaged)]
    public void A_program_of_the_same_account_keeps_a_file_registered_under_that_account(
        MsiInstallContext context)
    {
        var machine = new Machine(holderAccount: AnAccount);
        var msi = machine.Msi;
        machine.Install(Lacking, AnAccount, context);
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.WouldApply;

        AssertKept(machine.TheFile(Holder, Lacking), LackingProgramKeep.WouldApply);

        // Asked in that account and in the context the program is installed in.
        Assert.Equal([(Lacking, (string?)AnAccount, context, File)], msi.PatchSequenceAsks);
    }

    [Fact]
    public void A_program_of_the_same_account_the_patch_cannot_apply_to_leaves_the_file_listed()
    {
        var machine = new Machine(holderAccount: AnAccount);
        var msi = machine.Msi;
        machine.Install(Lacking, AnAccount);
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.CannotApply;

        AssertListed(machine.TheFile(Holder, Lacking));
    }

    [Fact]
    public void A_program_installed_per_user_with_no_account_keeps_the_file_and_is_not_asked()
    {
        // The keyed walk lists the program per user and names no account, so the key its
        // records are under cannot be named.
        var machine = new Machine();
        var msi = machine.Msi;
        msi.KeyedInstances[Lacking] = [(null, MsiInstallContext.UserUnmanaged)];
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.CannotApply;

        AssertKept(machine.TheFile(Holder, Lacking), LackingProgramKeep.AnswerNotHad);
        Assert.Empty(msi.PatchSequenceAsks);
    }

    [Fact]
    public void A_program_the_walk_also_lists_is_asked_once()
    {
        // The walk's installations are asked before the Template's, so the program comes
        // round twice. It is the same installation and gets one question.
        var machine = new Machine();
        var msi = machine.Msi;
        machine.Install(Lacking);
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.CannotApply;

        AssertListed(machine.TheFile(Holder, Lacking));
        Assert.Single(msi.PatchSequenceAsks);
    }

    [Fact]
    public void A_file_two_programs_keep_is_recorded_under_the_answer_not_had_before_superseded_there()
    {
        var machine = new Machine();
        var msi = machine.Msi;
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.SupersededThere;
        // AlsoLacking is not scripted, so its call fails.

        AssertKept(machine.TheFile(Holder, Lacking, AlsoLacking), LackingProgramKeep.AnswerNotHad);
    }

    [Fact]
    public void No_program_is_asked_once_one_has_answered_that_the_patch_would_apply()
    {
        var machine = new Machine();
        var msi = machine.Msi;
        machine.Install(Lacking);
        machine.Install(AlsoLacking);
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.WouldApply;
        msi.PatchSequence[(AlsoLacking, File)] = FakeMsiApi.SupersededThere;

        AssertKept(machine.TheFile(Holder, Lacking, AlsoLacking), LackingProgramKeep.WouldApply);
        Assert.Equal([Lacking], msi.PatchSequenceAsks.Select(a => a.ProductCode));
    }

    [Fact]
    public void A_file_a_program_still_holds_as_needed_carries_no_mark_for_a_program_that_lacks_it()
    {
        // The second holder has the patch applied, which keeps the file as a claim. The
        // program that lacks the patch is not what decided it, so the row is not counted
        // among the files kept for one.
        const string applied = "{EEEEEEEE-1111-0000-0000-00000000000E}";
        var machine = new Machine();
        var msi = machine.Msi;
        machine.Install(applied);
        machine.Hold(applied, state: "1", uninstallable: "0");
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.WouldApply;

        var row = machine.TheFile(Holder, applied, Lacking);

        Assert.False(row.IsRemovable);
        Assert.Equal(LackingProgramKeep.None, row.KeptForLackingProgram);
    }

    [Fact]
    public void A_named_program_that_is_not_installed_keeps_nothing()
    {
        var machine = new Machine();
        var msi = machine.Msi;
        msi.NotInstalled.Add(Lacking);

        AssertListed(machine.TheFile(Holder, Lacking));
        Assert.Empty(msi.PatchSequenceAsks);
    }

    [Fact]
    public void A_call_that_fails_once_is_asked_again()
    {
        var machine = new Machine();
        var msi = machine.Msi;
        var answers = new Queue<(uint, uint, uint)>([(1627, 0, 0), FakeMsiApi.CannotApply, FakeMsiApi.CannotApply]);
        msi.PatchSequenceFor = (_, _) => answers.Count > 1 ? answers.Dequeue() : answers.Peek();

        AssertListed(machine.TheFile(Holder, Lacking));
        Assert.Equal(3, msi.PatchSequenceAsks.Count);
    }

    [Fact]
    public void A_program_whose_call_never_answers_is_asked_again_for_one_file_only()
    {
        // Two superseded files, each naming the program. Its call fails every time: it
        // is made three times for the first file and once for the second.
        const string otherPatch = "{CCCCCCCC-1111-0000-0000-00000000000D}";
        const string otherFile = @"C:\Windows\Installer\superseded-too.msp";
        var machine = new Machine();
        machine.Hold(Holder, state: "2", uninstallable: "0", patch: otherPatch, path: otherFile);

        var rows = machine.Rows(Holder, Lacking);

        AssertKept(rows[File], LackingProgramKeep.AnswerNotHad);
        AssertKept(rows[otherFile], LackingProgramKeep.AnswerNotHad);
        Assert.Equal(WalkRestarts.Attempts + 1, machine.Msi.PatchSequenceAsks.Count);
    }

    [Fact]
    public void An_installation_found_holding_the_patch_by_asking_registers_the_file_under_its_account()
    {
        // The enumeration's one claim is for the machine. A program under an account
        // also holds the patch, and says so when asked, though no claim of its own
        // reached the enumeration. So a program of that account that lacks the patch is
        // under the key of a registration, and is asked.
        const string alsoHolds = "{EEEEEEEE-1111-0000-0000-00000000000F}";
        var machine = new Machine();
        var msi = machine.Msi;
        machine.Install(alsoHolds, AnAccount);
        machine.HoldWithNoClaim(alsoHolds, state: "2", uninstallable: "0");
        machine.Install(Lacking, AnAccount);
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.WouldApply;

        AssertKept(machine.TheFile(Holder, alsoHolds, Lacking), LackingProgramKeep.WouldApply);
        Assert.Equal([(Lacking, (string?)AnAccount, MsiInstallContext.UserUnmanaged, File)], msi.PatchSequenceAsks);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_file_two_patch_codes_name_is_kept_on_the_unanswered_one_whichever_is_reached_first(
        bool theUnansweredCodeSortsFirst)
    {
        // Two patch codes name the one file. Under one of them every installation
        // answers and a program that lacks the patch would take it. Under the other an
        // installation does not answer. The file is kept as one an installation did not
        // answer for, and carries no mark, whichever code the pass reaches first.
        var unanswered = theUnansweredCodeSortsFirst
            ? "{00000000-1111-0000-0000-000000000001}"
            : "{FFFFFFFF-1111-0000-0000-00000000000F}";
        const string silent = "{EEEEEEEE-1111-0000-0000-00000000000A}";
        var machine = new Machine();
        var msi = machine.Msi;
        machine.Hold(Holder, state: "2", uninstallable: "0", patch: unanswered);
        machine.Install(silent);
        msi.PatchPropertyResult[(unanswered, silent, "State")] = 1610;
        msi.PatchSequence[(Lacking, File)] = FakeMsiApi.WouldApply;

        var row = machine.TheFile(Holder, Lacking);

        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.Equal(LackingProgramKeep.None, row.KeptForLackingProgram);
    }

    [Fact]
    public void The_scan_counts_the_files_kept_under_each_answer_and_their_bytes()
    {
        static RegisteredPackage Kept(string name, LackingProgramKeep answer, long bytes, bool onDisk = true) =>
            new(@"C:\Windows\Installer\" + name, "Product", Holder, PatchState: 2,
                RemovableWithheld: answer == LackingProgramKeep.AnswerNotHad,
                FileSizeBytes: bytes, FileExists: onDisk, KeptForLackingProgram: answer);

        var scan = new ScanResult(
            Array.Empty<OrphanedFile>(),
            [
                Kept("a.msp", LackingProgramKeep.WouldApply, 100),
                Kept("b.msp", LackingProgramKeep.WouldApply, 20),
                Kept("c.msp", LackingProgramKeep.SupersededThere, 3),
                Kept("d.msp", LackingProgramKeep.AnswerNotHad, 4000),
                // Not on the disk, so it is no file kept.
                Kept("e.msp", LackingProgramKeep.WouldApply, 50000, onDisk: false),
                // Kept for another reason, and under no answer.
                Kept("f.msp", LackingProgramKeep.None, 600000),
            ],
            0);

        Assert.Equal(2, scan.SupersededKeptForLackingProgramCount(LackingProgramKeep.WouldApply));
        Assert.Equal(120, scan.SupersededKeptForLackingProgramBytes(LackingProgramKeep.WouldApply));
        Assert.Equal(1, scan.SupersededKeptForLackingProgramCount(LackingProgramKeep.SupersededThere));
        Assert.Equal(3, scan.SupersededKeptForLackingProgramBytes(LackingProgramKeep.SupersededThere));
        Assert.Equal(1, scan.SupersededKeptForLackingProgramCount(LackingProgramKeep.AnswerNotHad));
        Assert.Equal(4000, scan.SupersededKeptForLackingProgramBytes(LackingProgramKeep.AnswerNotHad));
        Assert.Equal(0, scan.SupersededKeptForLackingProgramCount(LackingProgramKeep.None));
        Assert.Equal(0, scan.SupersededKeptForLackingProgramBytes(LackingProgramKeep.None));
    }

    [Theory]
    [InlineData(LackingProgramKeep.WouldApply)]
    [InlineData(LackingProgramKeep.SupersededThere)]
    [InlineData(LackingProgramKeep.AnswerNotHad)]
    public void A_file_kept_for_a_program_that_lacks_its_patch_is_reported_where_it_has_gone(LackingProgramKeep answer)
    {
        // Superseded, on clean patch sets, with nothing else holding the file: the row the
        // missing-files report would otherwise pass over.
        var row = new RegisteredPackage(File, "Product", Holder, PatchState: 2,
            ProductPatchSetVerdict: ProductPatchSet.AllNonRemovable, FileExists: false);
        Assert.False(InstallerClean.Helpers.MissingFilesReport.Affected(row));

        Assert.True(InstallerClean.Helpers.MissingFilesReport.Affected(row with { KeptForLackingProgram = answer }));
    }

    /// <summary>
    /// The stronger-answer rule on its own: the answer one file is recorded under where
    /// two programs keep it.
    /// </summary>
    [Theory]
    [InlineData(LackingProgramKeep.None, LackingProgramKeep.SupersededThere, LackingProgramKeep.SupersededThere)]
    [InlineData(LackingProgramKeep.SupersededThere, LackingProgramKeep.AnswerNotHad, LackingProgramKeep.AnswerNotHad)]
    [InlineData(LackingProgramKeep.AnswerNotHad, LackingProgramKeep.WouldApply, LackingProgramKeep.WouldApply)]
    [InlineData(LackingProgramKeep.WouldApply, LackingProgramKeep.SupersededThere, LackingProgramKeep.WouldApply)]
    [InlineData(LackingProgramKeep.None, LackingProgramKeep.None, LackingProgramKeep.None)]
    public void The_stronger_of_two_answers_is_the_same_whichever_comes_first(
        LackingProgramKeep a, LackingProgramKeep b, LackingProgramKeep stronger)
    {
        Assert.Equal(stronger, LackingProgramKeeps.Stronger(a, b));
        Assert.Equal(stronger, LackingProgramKeeps.Stronger(b, a));
    }

    /// <summary>A reader for which every patch file names the given programs.</summary>
    private sealed class TemplateReader(string[] named) : IPackageIdentityReader
    {
        public PackageIdentity? Read(string filePath, bool isPatch, out string detail, out PackageReadRefusal refusal)
        {
            detail = string.Empty;
            refusal = PackageReadRefusal.WouldNotRead;
            // Every patch file here names the same programs.
            return isPatch ? new PackageIdentity(Patch, IsPatch: true, named) : null;
        }

        public string? PackageCodeOf(string filePath) => null;

        public bool HoldsNoRevisionNumber(string filePath) => false;
    }
}
